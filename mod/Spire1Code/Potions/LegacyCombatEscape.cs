using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Potions;

/// <summary>
/// StS1 Smoke Bomb's rewardless escape transaction.
///
/// The host has one combat-end path (<c>CombatManager.IsCombatEnding</c> to
/// <c>EndCombatInternal</c>) and it always fires <c>CombatWon</c>, victory progress, achievements and,
/// when the encounter wants rewards, the reward screen. Smoke Bomb must instead leave the room without
/// rewards, so this helper owns a separate pending-escape transaction:
/// <list type="bullet">
/// <item>the real <see cref="UsePotionAction"/> admission path validates and leases the potion before
/// the wrapper's <c>RemoveBeforeUse</c>;</item>
/// <item><see cref="SmokeBomb.OnUse"/> only binds the pending escape, it never ends combat immediately;</item>
/// <item>the private <c>CheckWinCondition(CombatTurnState)</c> is the single safe point that consumes the
/// pending escape (a real pending loss still wins, and a stale turn state is a no-op);</item>
/// <item>the completion mirrors the parts of <c>EndCombatInternal</c> that are combat lifecycle, keeps
/// the legacy <c>AfterCombatVictory</c> hook, and omits <c>CombatWon</c>, rewards, win progress and
/// achievements;</item>
/// <item>a serialization marker makes the pre-finished room continue to skip rewards after a reload.</item>
/// </list>
///
/// Patch installation is carried by the nested attribute classes below and performed by MainFile's
/// Phase3 attribute scan. <see cref="EnsureInstalled"/> is the idempotent verifier; it never installs,
/// never enumerates ModelDb, and never runs in a constructor or property initializer. A live use with an
/// incomplete installation is rejected with a real exception instead of silently falling back to the
/// normal victory path.
/// </summary>
internal static class LegacyCombatEscape
{
    internal const string EncounterStateKey = "SPIRE1_SMOKE_ESCAPED";
    private const string MarkerValue = "1";
    private const string WrapperFailure = "SmokeBomb escape helper patches are not completely installed; refusing to use the potion.";
    private const string EssenceFailure = "Essence of Steel needs the Powers content group; its Plated Armor would be inert without it.";

    private static readonly object Gate = new();
    private static readonly object PendingGate = new();
    private static readonly ConditionalWeakTable<PotionModel, EscapeLease> Leases = new();
    private static readonly ConditionalWeakTable<CombatRoom, EscapeMarker> EscapedRooms = new();
    private static readonly AsyncLocal<PendingEscape?> Completing = new();

    private static bool _installed;
    private static bool _failureLogged;
    private static Func<CombatManager, CombatId?, CombatTurnState?>? _liveTurnState;
    private static Action<CombatState, PlayerTurnPhase>? _setPhaseForAllPlayers;
    private static Action<CombatManager, bool>? _setPlayerActionsDisabled;
    private static FieldInfo? _combatEndedField;
    private static FieldInfo? _turnStateField;
    private static PendingEscape? _pending;

    /// <summary>
    /// Verifies every live-dependent patch target, every Harmony attachment made by the nested patch
    /// classes, the two completion delegates and the private event backing field. Verification is
    /// repeated until it succeeds; a failed attempt is not cached because Phase3 may still be installing
    /// the attribute classes when the first card/potion usability query runs.
    /// </summary>
    internal static bool EnsureInstalled()
    {
        if (Volatile.Read(ref _installed))
        {
            return true;
        }

        lock (Gate)
        {
            if (_installed)
            {
                return true;
            }

            try
            {
                MethodInfo? wrapper = AccessTools.DeclaredMethod(
                    typeof(PotionModel), nameof(PotionModel.OnUseWrapper),
                    new[] { typeof(PlayerChoiceContext), typeof(Creature) });
                MethodInfo? execute = AccessTools.DeclaredMethod(typeof(UsePotionAction), "ExecuteAction");
                MethodInfo? checkWin = AccessTools.DeclaredMethod(
                    typeof(CombatManager), "CheckWinCondition", new[] { typeof(CombatTurnState) });
                MethodInfo? toSerializable = AccessTools.DeclaredMethod(
                    typeof(CombatRoom), nameof(CombatRoom.ToSerializable));
                MethodInfo? fromSerializable = AccessTools.DeclaredMethod(
                    typeof(CombatRoom), "FromSerializable",
                    new[] { typeof(SerializableRoom), typeof(IRunState) });
                MethodInfo? offerRewards = AccessTools.DeclaredMethod(
                    typeof(CombatRoom), nameof(CombatRoom.OfferRoomEndRewards));

                if (!IsInstanceTask(wrapper, typeof(PlayerChoiceContext), typeof(Creature))
                    || !IsInstanceTask(execute)
                    || !IsInstanceTask(checkWin, typeof(CombatTurnState), returnsBool: true)
                    || !IsInstanceResult(toSerializable, typeof(SerializableRoom))
                    || !IsStaticResult(fromSerializable, typeof(CombatRoom), typeof(SerializableRoom), typeof(IRunState))
                    || !IsInstanceTask(offerRewards))
                {
                    return Fail("one or more patch target signatures changed");
                }

                MethodInfo? liveTurn = AccessTools.DeclaredMethod(
                    typeof(CombatManager), "LiveTurnStateFor", new[] { typeof(CombatId?) });
                MethodInfo? phase = AccessTools.DeclaredMethod(
                    typeof(CombatManager), "SetPhaseForAllPlayers",
                    new[] { typeof(CombatState), typeof(PlayerTurnPhase) });
                PropertyInfo? actionsDisabled = AccessTools.Property(
                    typeof(CombatManager), nameof(CombatManager.PlayerActionsDisabled));
                MethodInfo? actionsSetter = actionsDisabled?.GetSetMethod(nonPublic: true);
                FieldInfo? combatEnded = AccessTools.Field(typeof(CombatManager), "CombatEnded");
                FieldInfo? turnStateField = AccessTools.Field(typeof(CombatManager), "_turnState");

                if (!IsInstanceResult(liveTurn, typeof(CombatTurnState), typeof(CombatId?))
                    || phase is not { IsStatic: true }
                    || phase.ReturnType != typeof(void)
                    || !HasParameters(phase, typeof(CombatState), typeof(PlayerTurnPhase))
                    || actionsSetter is not { IsStatic: false }
                    || actionsSetter.ReturnType != typeof(void)
                    || !HasParameters(actionsSetter, typeof(bool))
                    || combatEnded is null
                    || combatEnded.FieldType != typeof(Action<CombatRoom>)
                    || turnStateField is null
                    || turnStateField.FieldType != typeof(CombatTurnState))
                {
                    return Fail("the combat completion API changed");
                }

                // A containing type cannot name a nested type's private patch methods (CS0122), so
                // resolve them by exact string name instead of nameof.
                MethodInfo? wrapperPrefix = AccessTools.DeclaredMethod(
                    typeof(SmokeOnUseWrapperPatch), "Prefix");
                MethodInfo? wrapperCompletion = AccessTools.DeclaredMethod(
                    typeof(SmokeOnUseWrapperCompletionPatch), "Postfix");
                MethodInfo? executePrefix = AccessTools.DeclaredMethod(
                    typeof(UsePotionAdmissionPatch), "Prefix");
                MethodInfo? checkWinPrefix = AccessTools.DeclaredMethod(
                    typeof(CheckWinConditionPatch), "Prefix");
                MethodInfo? serializationPostfix = AccessTools.DeclaredMethod(
                    typeof(CombatRoomToSerializablePatch), "Postfix");
                MethodInfo? deserializationPostfix = AccessTools.DeclaredMethod(
                    typeof(CombatRoomFromSerializablePatch), "Postfix");
                MethodInfo? offerPrefix = AccessTools.DeclaredMethod(
                    typeof(CombatRoomOfferRoomEndRewardsPatch), "Prefix");

                if (wrapperPrefix is null || wrapperCompletion is null || executePrefix is null
                    || checkWinPrefix is null || serializationPostfix is null
                    || deserializationPostfix is null || offerPrefix is null
                    || !HasPatch(wrapper, wrapperPrefix)
                    || !HasPatch(wrapper, wrapperCompletion)
                    || !HasPatch(execute, executePrefix)
                    || !HasPatch(checkWin, checkWinPrefix)
                    || !HasPatch(toSerializable, serializationPostfix)
                    || !HasPatch(fromSerializable, deserializationPostfix)
                    || !HasPatch(offerRewards, offerPrefix))
                {
                    return Fail("the required Harmony patches are not all attached");
                }

                _liveTurnState = liveTurn!.CreateDelegate<Func<CombatManager, CombatId?, CombatTurnState?>>();
                _setPhaseForAllPlayers = phase!.CreateDelegate<Action<CombatState, PlayerTurnPhase>>();
                _setPlayerActionsDisabled = actionsSetter!.CreateDelegate<Action<CombatManager, bool>>();
                _combatEndedField = combatEnded;
                _turnStateField = turnStateField;
                Volatile.Write(ref _installed, true);
                return true;
            }
            catch (Exception exception)
            {
                return Fail("verification threw " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }

    /// <summary>Usability predicate used by <see cref="SmokeBomb.PassesCustomUsabilityCheck"/>.</summary>
    internal static bool CanUseSmokeBomb(SmokeBomb potion, out string reason) =>
        TryCaptureSmokeBomb(potion, out _, out reason);

    /// <summary>
    /// Binds a pending escape to the exact turn state captured at admission. Called from
    /// <see cref="SmokeBomb.OnUse"/> after the wrapper has already validated and leased the use. It does
    /// not end combat and does not reset the pending escape; the turn loop's CheckWinCondition is the
    /// only consumer.
    /// </summary>
    internal static void BindSmokeBomb(SmokeBomb potion, Creature? target)
    {
        if (!EnsureInstalled())
        {
            throw new InvalidOperationException(WrapperFailure);
        }
        if (target is not null && !ReferenceEquals(target, potion.Owner?.Creature))
        {
            throw new InvalidOperationException("SmokeBomb may only be used on its owner.");
        }
        if (!Leases.TryGetValue(potion, out EscapeLease lease) || lease is null)
        {
            throw new InvalidOperationException("SmokeBomb reached OnUse without the real UsePotionAction admission lease.");
        }
        if (!IsLeaseLive(lease))
        {
            // The potion has already passed real admission and RemoveBeforeUse. A dead owner,
            // disappeared combat, changing room or replaced room generation is an explicit cancel,
            // not a broken-install fault; preserve the captured lease context and let the original
            // cancellation propagate through the wrapper observer.
            throw new OperationCanceledException(
                $"SmokeBomb escape canceled before OnUse: combat {lease.CombatId.Value}, "
                + $"room {lease.Room.RoomType}, owner {lease.Owner.NetId}.");
        }

        lock (PendingGate)
        {
            if (_pending is not null && !_pending.IsFinished)
            {
                throw new InvalidOperationException("SmokeBomb reached OnUse while another escape was still pending.");
            }
            _pending = new PendingEscape(lease);
        }

        // OnUse is the last point that needs the lease. Removing it here also covers a later
        // AfterPotionUsed exception; the action path removes it in a finally as a second layer.
        Leases.Remove(potion);
    }

    /// <summary>
    /// Awaits the original wrapper Task and only then marks the pending escape Ready. The returned Task is
    /// the Task the caller actually awaits: success commits, and a fault/cancel clears the still-Requested
    /// pending while the original exception continues. There is no background continuation.
    /// </summary>
    private static async Task ObserveWrapperAsync(PotionModel potion, Task wrapperTask)
    {
        try
        {
            await wrapperTask;
            MarkReady(potion);
        }
        catch
        {
            DiscardUnreadyPending(potion);
            throw;
        }
    }

    internal static void MarkReady(PotionModel potion)
    {
        lock (PendingGate)
        {
            PendingEscape? pending = _pending;
            if (pending is not null && ReferenceEquals(pending.Potion, potion) && !pending.Discarded)
            {
                pending.Ready = true;
            }
        }
    }

    internal static void DiscardUnreadyPending(PotionModel potion)
    {
        lock (PendingGate)
        {
            PendingEscape? pending = _pending;
            if (pending is not null && ReferenceEquals(pending.Potion, potion) && !pending.Ready)
            {
                pending.Discarded = true;
                _pending = null;
            }
        }
    }

    private static bool TryCaptureSmokeBomb(
        SmokeBomb potion, out EscapeCapture capture, out string reason)
    {
        capture = default;
        reason = string.Empty;

        if (!EnsureInstalled())
        {
            reason = "helper-incomplete";
            return false;
        }

        CombatManager manager = CombatManager.Instance;
        if (RunManager.Instance.IsChangingRooms)
        {
            reason = "changing-rooms";
            return false;
        }
        if (!manager.IsInProgress || manager.IsOverOrEnding)
        {
            reason = "combat-not-live";
            return false;
        }

        CombatId? combatId = manager.CurrentCombatId;
        if (combatId is null)
        {
            reason = "no-combat-id";
            return false;
        }

        CombatTurnState? turnState = _liveTurnState?.Invoke(manager, combatId);
        if (turnState is null || !turnState.IsLive || !turnState.IsInProgress)
        {
            reason = "turn-state-stale";
            return false;
        }

        CombatState combatState = turnState.State;
        IRunState runState = combatState.RunState;
        if (runState.CurrentRoom is not CombatRoom room || !ReferenceEquals(room.CombatState, combatState))
        {
            reason = "room-mismatch";
            return false;
        }
        if (room.RoomType == RoomType.Boss)
        {
            reason = "boss-room";
            return false;
        }

        Player? owner = potion.Owner;
        if (owner is null)
        {
            reason = "no-owner";
            return false;
        }
        if (owner.Creature.IsDead
            || !combatState.ContainsCreature(owner.Creature)
            || !ReferenceEquals(owner.Creature.CombatState, combatState))
        {
            reason = "owner-not-live";
            return false;
        }

        PlayerCombatState? playerCombatState = owner.PlayerCombatState;
        if (playerCombatState is null || !ReferenceEquals(owner.RunState, runState))
        {
            reason = "owner-state-mismatch";
            return false;
        }

        if (combatState.Enemies.Any(enemy =>
                enemy.HasPower<BackAttackLeftPower>() || enemy.HasPower<BackAttackRightPower>()))
        {
            reason = "back-attack";
            return false;
        }

        lock (PendingGate)
        {
            PendingEscape? pending = _pending;
            if (pending is not null)
            {
                if (pending.IsFinished || !pending.TurnState.IsLive
                    || !manager.IsCurrentLiveCombat(pending.CombatId))
                {
                    _pending = null;
                }
                else
                {
                    reason = "escape-already-pending";
                    return false;
                }
            }
        }

        capture = new EscapeCapture(
            potion, owner, playerCombatState, combatState, turnState, runState, room, turnState.Id);
        return true;
    }

    private static bool TryBuildLease(SmokeBomb potion, out EscapeLease lease, out string reason)
    {
        if (!TryCaptureSmokeBomb(potion, out EscapeCapture capture, out reason))
        {
            lease = null!;
            return false;
        }
        lease = new EscapeLease(
            capture.Potion, capture.Owner, capture.Pcs, capture.CombatState,
            capture.TurnState, capture.RunState, capture.Room, capture.CombatId);
        return true;
    }

    private static bool IsLeaseLive(EscapeLease lease)
    {
        CombatManager manager = CombatManager.Instance;
        if (RunManager.Instance.IsChangingRooms || !manager.IsInProgress)
        {
            return false;
        }
        CombatId? currentId = manager.CurrentCombatId;
        if (currentId is null || currentId != lease.CombatId)
        {
            return false;
        }
        CombatTurnState? current = _liveTurnState?.Invoke(manager, currentId);
        return current is not null
            && ReferenceEquals(current, lease.TurnState)
            && ReferenceEquals(lease.RunState.CurrentRoom, lease.Room)
            && ReferenceEquals(lease.CombatState.RunState, lease.RunState)
            && ReferenceEquals(lease.Owner.RunState, lease.RunState)
            && ReferenceEquals(lease.Owner.PlayerCombatState, lease.Pcs)
            && ReferenceEquals(lease.Owner.Creature.CombatState, lease.CombatState)
            && lease.CombatState.ContainsCreature(lease.Owner.Creature)
            && !lease.Owner.Creature.IsDead;
    }

    private static void SetLease(PotionModel potion, EscapeLease lease)
    {
        Leases.Remove(potion);
        Leases.Add(potion, lease);
    }

    private static bool IsPendingAdmissible(PendingEscape pending, CombatTurnState turnState)
    {
        if (!turnState.IsInProgress || !turnState.IsLive || turnState.PendingLoss is not null)
        {
            return false;
        }
        CombatManager manager = CombatManager.Instance;
        if (!manager.IsCurrentLiveCombat(pending.CombatId))
        {
            return false;
        }
        CombatTurnState? current = _liveTurnState?.Invoke(manager, pending.CombatId);
        return current is not null
            && ReferenceEquals(current, turnState)
            && ReferenceEquals(pending.CombatState, turnState.State)
            && ReferenceEquals(pending.CombatState.RunState, pending.RunState)
            && ReferenceEquals(pending.RunState.CurrentRoom, pending.Room)
            && ReferenceEquals(pending.Room.CombatState, pending.CombatState)
            && ReferenceEquals(pending.Owner.RunState, pending.RunState)
            && ReferenceEquals(pending.Owner.PlayerCombatState, pending.Pcs)
            && ReferenceEquals(pending.Owner.Creature.CombatState, pending.CombatState)
            && pending.CombatState.ContainsCreature(pending.Owner.Creature)
            && !pending.Owner.Creature.IsDead;
    }

    private static bool CheckWinConditionPrefix(
        CombatManager instance, CombatTurnState turnState, ref Task<bool> result)
    {
        PendingEscape? pending = Volatile.Read(ref _pending);
        if (pending is null || !ReferenceEquals(pending.TurnState, turnState))
        {
            return true;
        }

        lock (PendingGate)
        {
            pending = _pending;
            if (pending is null || !ReferenceEquals(pending.TurnState, turnState))
            {
                return true;
            }
            if (ReferenceEquals(Completing.Value, pending))
            {
                // A completion hook re-entered CheckWinCondition on the same logical scope.
                // The combat is already ending; return the terminal true instead of awaiting the
                // completion Task that this same flow is still executing.
                result = Task.FromResult(true);
                return false;
            }
            if (pending.Completion is { } existingCompletion)
            {
                result = existingCompletion;
                return false;
            }
            if (pending.Discarded)
            {
                _pending = null;
                return true;
            }
            if (!pending.Ready)
            {
                // The wrapper is still running. Its AfterPotionUsed/history/empty-hand segment can
                // still fault or re-enter here; an uncommitted use must never escape. Success marks
                // Ready from ObserveWrapperAsync, and a fault/cancel clears the pending there.
                return true;
            }
            if (turnState.PendingLoss is not null)
            {
                pending.Discarded = true;
                _pending = null;
                return true;
            }
            if (!IsPendingAdmissible(pending, turnState))
            {
                pending.Discarded = true;
                _pending = null;
                return true;
            }

            TaskCompletionSource<bool> completionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.CompletionSource = completionSource;
            pending.Completion = completionSource.Task;
            result = completionSource.Task;
            _ = RunCompletionAsync(pending);
            return false;
        }
    }

    private static async Task RunCompletionAsync(PendingEscape pending)
    {
        PendingEscape? previous = Completing.Value;
        Completing.Value = pending;
        try
        {
            bool completed = await CompleteEscapeAsync(pending);
            pending.CompletionSource!.TrySetResult(completed);
        }
        catch (Exception exception)
        {
            pending.CompletionSource!.TrySetException(exception);
        }
        finally
        {
            Completing.Value = previous;
        }
    }

    /// <summary>
    /// Ending-state liveness check for the completion's awaits. IsCurrentLiveCombat is deliberately not
    /// used: the completion has already set IsInProgress=false, so the captured turn state, id, run and
    /// room are compared directly. If any await resumes after a new combat/run/room has taken over, the
    /// remaining cleanup must not touch it.
    /// </summary>
    private static bool IsSameEndingScope(PendingEscape pending)
    {
        FieldInfo field = _turnStateField
            ?? throw new InvalidOperationException("CombatManager._turnState was not verified at installation.");
        CombatManager manager = CombatManager.Instance;
        return ReferenceEquals(field.GetValue(manager), pending.TurnState)
            && manager.CurrentCombatId == pending.CombatId
            && ReferenceEquals(RunManager.Instance.State, pending.RunState)
            && ReferenceEquals(pending.RunState.CurrentRoom, pending.Room)
            && ReferenceEquals(pending.Room.CombatState, pending.CombatState)
            && ReferenceEquals(pending.Owner.RunState, pending.RunState)
            && ReferenceEquals(pending.Owner.PlayerCombatState, pending.Pcs)
            && ReferenceEquals(pending.Owner.Creature.CombatState, pending.CombatState);
    }

    private static bool ContinueAfterAwait(PendingEscape pending, string stage)
    {
        if (IsSameEndingScope(pending))
        {
            return true;
        }
        MainFile.Logger.Error(
            "[Spire1] SmokeBomb escape completion stopped after " + stage
            + ": its captured turn state/run/room is no longer current.");
        return false;
    }

    /// <summary>
    /// The escape completion. It deliberately does not call EndCombatInternal or any victory/reward
    /// helper. IsInProgress flips false before the first await; every later step uses the captured
    /// turn state/room/run/player identities. A failure propagates through the returned task and leaves
    /// the pending completion faulted rather than re-running hooks or granting rewards.
    /// </summary>
    private static async Task<bool> CompleteEscapeAsync(PendingEscape pending)
    {
        CombatTurnState turnState = pending.TurnState;
        CombatState combatState = pending.CombatState;
        IRunState runState = pending.RunState;
        CombatRoom room = pending.Room;
        Player localPlayer = LocalContext.GetMe(combatState);
        int turnsTaken = localPlayer.PlayerCombatState!.TurnNumber;

        MarkEscaped(room);
        turnState.IsInProgress = false;
        using (turnState.ReadyLock.EnterScope())
        {
            turnState.PlayersTakingExtraTurn.Clear();
        }
        SetPhaseForAllPlayers(combatState, PlayerTurnPhase.None);
        SetPlayerActionsDisabled(CombatManager.Instance, false);

        foreach (Player player in combatState.Players)
        {
            await player.ReviveBeforeCombatEnd();
            if (!ContinueAfterAwait(pending, "ReviveBeforeCombatEnd"))
            {
                return true;
            }
        }
        await Hook.AfterCombatEnd(runState, combatState, room);
        if (!ContinueAfterAwait(pending, "Hook.AfterCombatEnd"))
        {
            return true;
        }
        CombatManager.Instance.History.Clear();
        room.OnCombatEnded();
        if (RunManager.Instance.NetService.Type != NetGameType.Replay)
        {
            RunManager.Instance.WriteReplay(stopRecording: true);
        }
        foreach (Player player in combatState.Players)
        {
            player.AfterCombatEnd();
        }
        await Hook.AfterCombatVictory(runState, combatState, room);
        if (!ContinueAfterAwait(pending, "Hook.AfterCombatVictory"))
        {
            return true;
        }
        NHoverTipSet.Clear();

        if (runState.CurrentMapPointHistoryEntry is not null)
        {
            runState.CurrentMapPointHistoryEntry.Rooms.Last().TurnsTaken = turnsTaken;
        }
        foreach (RunModel runModel in combatState.RunModels)
        {
            runModel.OnCombatFinished();
        }
        room.MarkPreFinished();
        await SaveManager.Instance.SaveRun(room, saveProgress: false);
        if (!ContinueAfterAwait(pending, "SaveRun"))
        {
            return true;
        }

        FireCombatEnded(room);
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotInCombat);
        RunManager.Instance.ActionExecutor.Unpause();
        NRunMusicController.Instance?.UpdateTrack();
        NMapScreen.Instance?.SetTravelEnabled(enabled: true);

        await ProceedWithoutRewardsAsync(runState, room);
        _ = ContinueAfterAwait(pending, "ProceedWithoutRewards");
        return true;
    }

    private static void SetPhaseForAllPlayers(CombatState combatState, PlayerTurnPhase phase)
    {
        Action<CombatState, PlayerTurnPhase> setter = _setPhaseForAllPlayers
            ?? throw new InvalidOperationException("SetPhaseForAllPlayers was not verified at installation.");
        setter(combatState, phase);
    }

    private static void SetPlayerActionsDisabled(CombatManager manager, bool disabled)
    {
        Action<CombatManager, bool> setter = _setPlayerActionsDisabled
            ?? throw new InvalidOperationException("PlayerActionsDisabled setter was not verified at installation.");
        setter(manager, disabled);
    }

    private static void FireCombatEnded(CombatRoom room)
    {
        FieldInfo field = _combatEndedField
            ?? throw new InvalidOperationException("CombatEnded backing field was not verified at installation.");
        if (field.GetValue(CombatManager.Instance) is Action<CombatRoom> combatEnded)
        {
            combatEnded(room);
        }
    }

    private static Task ProceedWithoutRewardsAsync(IRunState runState, CombatRoom room)
    {
        if (!TestMode.IsOn && NCombatRoom.Instance is { } screen
            && ReferenceEquals(runState.CurrentRoom, room))
        {
            return screen.Ui.ProceedWithoutRewards();
        }
        return RunManager.Instance.ProceedFromTerminalRewardsScreenAsync();
    }

    private static void MarkEscaped(CombatRoom room) =>
        EscapedRooms.AddOrUpdate(room, new EscapeMarker());

    private static bool IsEscaped(CombatRoom room) => EscapedRooms.TryGetValue(room, out _);

    private static bool HasPatch(MethodInfo target, MethodInfo patch)
    {
        HarmonyLib.Patches? info = Harmony.GetPatchInfo(target);
        if (info is null)
        {
            return false;
        }
        return info.Prefixes.Any(entry => entry.owner == MainFile.ModId && entry.PatchMethod == patch)
            || info.Postfixes.Any(entry => entry.owner == MainFile.ModId && entry.PatchMethod == patch);
    }

    private static bool IsInstanceTask(MethodInfo? method, params Type[] parameters) =>
        method is { IsStatic: false } && method.ReturnType == typeof(Task) && HasParameters(method, parameters);

    private static bool IsInstanceTask(MethodInfo? method, Type parameter, bool returnsBool) =>
        method is { IsStatic: false }
        && method.ReturnType == (returnsBool ? typeof(Task<bool>) : typeof(Task))
        && HasParameters(method, parameter);

    private static bool IsInstanceResult(MethodInfo? method, Type returnType, params Type[] parameters) =>
        method is { IsStatic: false } && method.ReturnType == returnType && HasParameters(method, parameters);

    private static bool IsStaticResult(MethodInfo? method, Type returnType, params Type[] parameters) =>
        method is { IsStatic: true } && method.ReturnType == returnType && HasParameters(method, parameters);

    private static bool HasParameters(MethodInfo method, params Type[] parameters)
    {
        ParameterInfo[] actual = method.GetParameters();
        if (actual.Length != parameters.Length)
        {
            return false;
        }
        for (int index = 0; index < actual.Length; index++)
        {
            if (actual[index].ParameterType != parameters[index])
            {
                return false;
            }
        }
        return true;
    }

    private static bool Fail(string reason)
    {
        if (!_failureLogged)
        {
            _failureLogged = true;
            MainFile.Logger.Error("[Spire1] SmokeBomb escape helper is NOT installed: " + reason);
        }
        return false;
    }

    private static Task Fault(string reason) => Task.FromException(new InvalidOperationException(reason));

    private static bool OnUseWrapperPrefix(PotionModel instance, Creature? target, ref Task result)
    {
        if (instance is SmokeBomb smoke)
        {
            if (!EnsureInstalled())
            {
                result = Fault(WrapperFailure);
                return false;
            }
            if (target is not null && !ReferenceEquals(target, smoke.Owner?.Creature))
            {
                result = Fault("SmokeBomb may only be used on its owner.");
                return false;
            }
            if (Leases.TryGetValue(smoke, out EscapeLease lease) && lease is not null)
            {
                if (!IsLeaseLive(lease))
                {
                    Leases.Remove(smoke);
                    result = Fault("SmokeBomb's captured combat changed before OnUseWrapper.");
                    return false;
                }
                return true;
            }
            if (!TryBuildLease(smoke, out EscapeLease directLease, out string reason))
            {
                result = Fault("SmokeBomb was rejected at OnUseWrapper: " + reason + ".");
                return false;
            }
            SetLease(smoke, directLease);
            return true;
        }

        if (instance is EssenceOfSteel && !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            result = Fault(EssenceFailure);
            return false;
        }
        return true;
    }

    private static bool ExecuteActionPrefix(UsePotionAction action, ref Task result)
    {
        PotionModel? potion = action.Player.GetPotionAtSlotIndex((int)action.PotionIndex);
        if (potion is null)
        {
            return true;
        }

        if (potion is EssenceOfSteel && !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            action.Cancel();
            result = Task.CompletedTask;
            return false;
        }

        if (potion is not SmokeBomb smoke)
        {
            return true;
        }

        if (!EnsureInstalled())
        {
            result = Fault(WrapperFailure);
            return false;
        }
        if (!ReferenceEquals(action.Player, smoke.Owner))
        {
            action.Cancel();
            result = Task.CompletedTask;
            return false;
        }
        if (!TryBuildLease(smoke, out EscapeLease lease, out _))
        {
            action.Cancel();
            result = Task.CompletedTask;
            return false;
        }

        SetLease(smoke, lease);
        result = RunSmokeActionAsync(action, smoke, lease);
        return false;
    }

    private static async Task RunSmokeActionAsync(UsePotionAction action, SmokeBomb potion, EscapeLease lease)
    {
        try
        {
            if (!IsLeaseLive(lease))
            {
                action.Cancel();
                return;
            }
            PlayerChoiceContext choiceContext = new GameActionPlayerChoiceContext(action);
            action.PlayerChoiceContext = choiceContext;
            await potion.OnUseWrapper(choiceContext, lease.Owner.Creature);
            MarkReady(potion);
        }
        catch
        {
            DiscardUnreadyPending(potion);
            throw;
        }
        finally
        {
            Leases.Remove(potion);
        }
    }

    private static void AddEscapeMarker(SerializableRoom serializableRoom)
    {
        serializableRoom.EncounterState ??= new Dictionary<string, string>();
        serializableRoom.EncounterState[EncounterStateKey] = MarkerValue;
    }

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
    private static class SmokeOnUseWrapperPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PotionModel __instance, Creature? target, ref Task __result) =>
            OnUseWrapperPrefix(__instance, target, ref __result);
    }

    // The same async method also needs its returned Task observed: the prefix cannot know whether the
    // wrapper's later AfterPotionUsed/history/empty-hand segment succeeded. The postfix only attaches
    // completion observation; it never consumes or replaces the original Task or its exception.
    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
    private static class SmokeOnUseWrapperCompletionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PotionModel __instance, ref Task __result)
        {
            if (__instance is SmokeBomb)
            {
                __result = ObserveWrapperAsync(__instance, __result);
            }
        }
    }

    [HarmonyPatch(typeof(UsePotionAction), "ExecuteAction")]
    private static class UsePotionAdmissionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(UsePotionAction __instance, ref Task __result) =>
            ExecuteActionPrefix(__instance, ref __result);
    }

    [HarmonyPatch(typeof(CombatManager), "CheckWinCondition", new[] { typeof(CombatTurnState) })]
    private static class CheckWinConditionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(CombatManager __instance, CombatTurnState turnState, ref Task<bool> __result) =>
            CheckWinConditionPrefix(__instance, turnState, ref __result);
    }

    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.ToSerializable))]
    private static class CombatRoomToSerializablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CombatRoom __instance, SerializableRoom __result)
        {
            if (IsEscaped(__instance))
            {
                AddEscapeMarker(__result);
            }
        }
    }

    [HarmonyPatch(typeof(CombatRoom), "FromSerializable",
        new[] { typeof(SerializableRoom), typeof(IRunState) })]
    private static class CombatRoomFromSerializablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(SerializableRoom serializableRoom, ref CombatRoom __result)
        {
            if (serializableRoom.EncounterState is null
                || !serializableRoom.EncounterState.TryGetValue(EncounterStateKey, out string? value))
            {
                return;
            }
            if (value != MarkerValue)
            {
                throw new InvalidOperationException(
                    "The saved SmokeBomb escape marker is invalid; refusing to load the room as a normal reward victory.");
            }
            MarkEscaped(__result);
        }
    }

    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OfferRoomEndRewards))]
    private static class CombatRoomOfferRoomEndRewardsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(CombatRoom __instance, ref Task __result)
        {
            if (!IsEscaped(__instance))
            {
                return true;
            }
            __result = ProceedWithoutRewardsAsync(__instance.CombatState.RunState, __instance);
            return false;
        }
    }

    private sealed class EscapeMarker
    {
    }

    private readonly struct EscapeCapture(
        SmokeBomb potion,
        Player owner,
        PlayerCombatState pcs,
        CombatState combatState,
        CombatTurnState turnState,
        IRunState runState,
        CombatRoom room,
        CombatId combatId)
    {
        internal readonly SmokeBomb Potion = potion;
        internal readonly Player Owner = owner;
        internal readonly PlayerCombatState Pcs = pcs;
        internal readonly CombatState CombatState = combatState;
        internal readonly CombatTurnState TurnState = turnState;
        internal readonly IRunState RunState = runState;
        internal readonly CombatRoom Room = room;
        internal readonly CombatId CombatId = combatId;
    }

    private sealed class EscapeLease(
        SmokeBomb potion,
        Player owner,
        PlayerCombatState pcs,
        CombatState combatState,
        CombatTurnState turnState,
        IRunState runState,
        CombatRoom room,
        CombatId combatId)
    {
        internal readonly SmokeBomb Potion = potion;
        internal readonly Player Owner = owner;
        internal readonly PlayerCombatState Pcs = pcs;
        internal readonly CombatState CombatState = combatState;
        internal readonly CombatTurnState TurnState = turnState;
        internal readonly IRunState RunState = runState;
        internal readonly CombatRoom Room = room;
        internal readonly CombatId CombatId = combatId;
    }

    private sealed class PendingEscape(EscapeLease lease)
    {
        internal readonly SmokeBomb Potion = lease.Potion;
        internal readonly Player Owner = lease.Owner;
        internal readonly PlayerCombatState Pcs = lease.Pcs;
        internal readonly CombatState CombatState = lease.CombatState;
        internal readonly CombatTurnState TurnState = lease.TurnState;
        internal readonly IRunState RunState = lease.RunState;
        internal readonly CombatRoom Room = lease.Room;
        internal readonly CombatId CombatId = lease.CombatId;
        internal TaskCompletionSource<bool>? CompletionSource;
        internal Task<bool>? Completion;
        internal bool Ready;
        internal bool Discarded;

        internal bool IsFinished => Discarded || Completion is { IsCompleted: true };
    }
}
