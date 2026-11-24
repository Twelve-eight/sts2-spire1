using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// An awaited continuation of one native card request, not another action queue. Native count
/// listeners still run exactly once. An admitted Echo request expresses their additions as native
/// purge dupes, so they cannot recursively trigger one another or mutate the paid physical card.
/// </summary>
internal static class Spire1EchoReplay
{
    private static readonly System.Threading.AsyncLocal<WrapperScope?> CurrentScope = new();
    private static readonly ConditionalWeakTable<CardModel, CopyState> Copies = new();
    private static readonly ConditionalWeakTable<HookPlayerChoiceContext, WrapperScope> Branches = new();
    private static readonly System.Threading.AsyncLocal<Admission?> CurrentAdmission = new();
    private static readonly ConditionalWeakTable<PlayerCombatState, List<Admission>> Admissions = new();
    private static bool _installed;

    private sealed class CopyState(Frame frame, int index)
    {
        internal readonly Frame Frame = frame;
        internal readonly int Index = index;
        internal bool Started;
    }

    internal sealed class Admission(CardModel card, Player player, PlayerCombatState state, ICombatState combat)
    {
        internal readonly CardModel Card = card;
        internal readonly Player Player = player;
        internal readonly PlayerCombatState State = state;
        internal readonly ICombatState Combat = combat;
        internal readonly CombatId? Id = CombatManager.Instance.CurrentCombatId;
        internal readonly int Round = combat.RoundNumber;
        internal readonly CombatSide Side = combat.CurrentSide;
        internal readonly int Turn = state.TurnNumber;
        internal bool Registered;
    }

    internal static Admission? AmbientAdmission => CurrentAdmission.Value;

    internal static Admission? EnterAdmission(CardModel card)
    {
        var player = card.Owner;
        var state = player.PlayerCombatState;
        var combat = player.Creature.CombatState;
        var admission = state == null || combat == null ? null : new Admission(card, player, state, combat);
        CurrentAdmission.Value = admission;
        return admission;
    }

    internal static void RestoreAdmission(Admission? previous) => CurrentAdmission.Value = previous;

    private static void RegisterAdmission(CardModel card)
    {
        var admission = CurrentAdmission.Value;
        if (admission == null || admission.Registered || !ReferenceEquals(admission.Card, card))
            return;
        Admissions.GetValue(admission.State, static _ => new List<Admission>()).Add(admission);
        admission.Registered = true;
    }

    internal static void PublishStart(CardPlay play)
    {
        var admission = CurrentAdmission.Value;
        if (admission != null && ReferenceEquals(play.Card, admission.Card))
            RemoveAdmission(admission);
    }

    private static void RemoveAdmission(Admission admission)
    {
        if (!admission.Registered)
            return;
        admission.Registered = false;
        if (Admissions.TryGetValue(admission.State, out var active))
        {
            active.Remove(admission);
            if (active.Count == 0)
                Admissions.Remove(admission.State);
        }
    }

    internal static int UnpublishedStarts(Player owner, ICombatState combat, PlayerCombatState state)
    {
        int count = 0;
        if (!Admissions.TryGetValue(state, out var active))
            return count;
        foreach (var admission in active)
        {
            if (!ReferenceEquals(admission, CurrentAdmission.Value) && admission.Registered
                && ReferenceEquals(admission.Player, owner) && ReferenceEquals(admission.Card.Owner, owner)
                && ReferenceEquals(admission.Combat, combat) && admission.Id == CombatManager.Instance.CurrentCombatId
                && admission.Round == combat.RoundNumber && admission.Side == combat.CurrentSide
                && admission.Turn == state.TurnNumber)
                count++;
        }
        return count;
    }

    internal static async Task CompleteAdmission(Task nativeWrapper, Admission admission)
    {
        try { await nativeWrapper; }
        finally { RemoveAdmission(admission); }
    }

    internal sealed class WrapperScope(Frame frame, CardModel card, PlayerChoiceContext context, int next)
    {
        internal readonly Frame Frame = frame;
        internal readonly CardModel Card = card;
        internal readonly PlayerChoiceContext Context = context;
        internal readonly int Next = next;
        internal readonly TaskCompletionSource<Exception?> WrapperFinished = new();
        internal BranchingPlayerChoiceContext? DirectBranch;
        internal Action<HookPlayerChoiceContext>? BranchHandler;
        internal bool BranchAssigned;
        internal bool Ended;
    }

    internal sealed class Contribution(AbstractModel? source, int count)
    {
        internal readonly AbstractModel? Source = source;
        internal int Count = count;
    }

    internal sealed class Frame(CardModel original, Spire1EchoFormPower power, ICombatState combat, bool skipVisuals)
    {
        internal readonly CardModel Original = original;
        internal readonly Spire1EchoFormPower Power = power;
        internal readonly Player Player = original.Owner;
        internal readonly PlayerCombatState PlayerState = original.Owner.PlayerCombatState!;
        internal readonly ICombatState Combat = combat;
        internal readonly CombatId? CombatId = CombatManager.Instance.CurrentCombatId;
        internal readonly int Round = combat.RoundNumber;
        internal readonly CombatSide Side = combat.CurrentSide;
        internal readonly int Turn = original.Owner.PlayerCombatState!.TurnNumber;
        internal readonly bool SkipVisuals = skipVisuals;
        internal readonly List<Contribution> Contributions = new();
        internal readonly List<CardModel> Replays = new();
        internal Creature? Target;
        internal bool Closed;
    }

    // The token is captured before a real card command awaits. Never consult a replacement PCS
    // or yield the played card before validating the old invocation's combat/owner identity.
    internal readonly struct MutationScope(CardModel card)
    {
        internal readonly Player Player = card.Owner;
        private readonly PlayerCombatState? _state = card.Owner.PlayerCombatState;
        private readonly ICombatState? _combat = card.Owner.Creature.CombatState;
        private readonly CombatId? _id = CombatManager.Instance.CurrentCombatId;
        private readonly int _round = card.Owner.Creature.CombatState?.RoundNumber ?? 0;
        private readonly CombatSide? _side = card.Owner.Creature.CombatState?.CurrentSide;
        private readonly int _turn = card.Owner.PlayerCombatState?.TurnNumber ?? 0;

        internal bool Includes(CardModel current) => _state != null && _combat != null
            && ReferenceEquals(current.Owner, Player) && ReferenceEquals(Player.PlayerCombatState, _state)
            && ReferenceEquals(Player.Creature.CombatState, _combat) && Player.Creature.IsAlive
            && CombatManager.Instance.IsCurrentLiveCombat(_id) && !CombatManager.Instance.IsOverOrEnding
            && _combat.RoundNumber == _round && _combat.CurrentSide == _side && _state.TurnNumber == _turn;

        internal IEnumerable<T> Members<T>(T played) where T : CardModel
        {
            if (!Includes(played))
                yield break;
            yield return played;
            var identity = ReplayIdentity(played);
            foreach (var pile in _state!.AllPiles)
            {
                foreach (var card in pile.Cards)
                {
                    if (card is T member && !ReferenceEquals(member, played)
                        && ReferenceEquals(member.Owner, Player) && ReferenceEquals(member.CombatState, _combat)
                        && ReferenceEquals(ReplayIdentity(member), identity))
                        yield return member;
                }
            }
        }
    }

    internal static MutationScope CaptureMutation(CardModel card) => new(card);

    internal static CardModel ReplayIdentity(CardModel card)
    {
        // DupeOf is makeSameInstanceOf's UUID link; ordinary CloneOf remains independent.
        while (card.IsDupe && card.DupeOf is { } original)
            card = original;
        return card;
    }

    internal static void RequireInstalled()
    {
        if (_installed)
            return;
        Spire1EchoFormPower.RequireHistoryLayout();
        RequirePatch(AccessTools.DeclaredMethod(typeof(CardModel), nameof(CardModel.OnPlayWrapper)),
            typeof(Spire1EchoWrapperPatch), postfix: true);
        RequirePatch(AccessTools.DeclaredMethod(typeof(Hook), nameof(Hook.ModifyCardPlayCount)),
            typeof(Spire1EchoCountPatch), postfix: true, transpiler: true);
        RequirePatch(AccessTools.DeclaredMethod(typeof(Hook), nameof(Hook.ModifyCardPlayResultLocation)),
            typeof(Spire1EchoPurgePatch));
        RequirePatch(AccessTools.DeclaredConstructor(typeof(BranchingPlayerChoiceContext),
            [typeof(AbstractModel), typeof(ulong), typeof(PlayerChoiceContext)]), typeof(Spire1EchoBranchPatch), postfix: true, prefix: false);
        RequirePatch(AccessTools.DeclaredMethod(typeof(HookPlayerChoiceContext),
            nameof(HookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion)), typeof(Spire1EchoBranchTaskPatch));
        RequirePatch(AccessTools.DeclaredMethod(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted)),
            typeof(Spire1EchoHistoryStartPatch), postfix: true, prefix: false);
        _installed = true;
    }

    private static void RequirePatch(MethodBase target, Type patchType, bool postfix = false, bool transpiler = false, bool prefix = true)
    {
        var info = Harmony.GetPatchInfo(target);
        if (info == null || (prefix && !info.Prefixes.Any(p => p.PatchMethod.DeclaringType == patchType))
            || (postfix && !info.Postfixes.Any(p => p.PatchMethod.DeclaringType == patchType))
            || (transpiler && info.Transpilers.Count(p => p.PatchMethod.DeclaringType == patchType) != 1))
            throw new InvalidOperationException("Echo Form native replay bridge is not installed: " + target.Name);
    }

    internal static WrapperScope? Enter(CardModel card, PlayerChoiceContext context, bool skipVisuals)
    {
        Frame? frame = null;
        int next = 0;
        if (Copies.TryGetValue(card, out var copy))
        {
            if (copy.Started)
                throw new InvalidOperationException("Echo purge copy entered twice.");
            copy.Started = true;
            frame = copy.Frame;
            next = copy.Index + 1;
        }
        else if (!card.IsDupe && !Spire1PowersGate.ContentUnavailableActive)
        {
            var power = card.Owner.Creature.GetPower<Spire1EchoFormPower>();
            if (power != null && power.Amount > 0 && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
            {
                RequireInstalled();
                frame = new Frame(card, power, card.Owner.Creature.CombatState
                    ?? throw new InvalidOperationException("Echo request has no combat state."), skipVisuals);
            }
        }
        // Shadow a parent's invocation even when this nested card has no Echo.
        var scope = frame == null ? null : new WrapperScope(frame, card, context, next);
        CurrentScope.Value = scope;
        return scope;
    }

    internal static WrapperScope? Ambient => CurrentScope.Value;
    internal static void Restore(WrapperScope? previous) => CurrentScope.Value = previous;
    private static Frame? Current(CardModel card) => CurrentScope.Value is { } scope
        && ReferenceEquals(scope.Card, card) && ReferenceEquals(scope.Frame.Original, card) ? scope.Frame : null;
    internal static bool IsCopy(CardModel card) => Copies.TryGetValue(card, out _);

    internal static void BindBranch(BranchingPlayerChoiceContext branch, AbstractModel source, PlayerChoiceContext existing)
    {
        var scope = CurrentScope.Value;
        if (scope == null || scope.Frame.Replays.Count == 0
            || !ReferenceEquals(source, scope.Card) || !ReferenceEquals(existing, scope.Context))
            return;
        if (scope.DirectBranch != null)
            throw new InvalidOperationException("Echo wrapper created two direct play branches.");
        scope.DirectBranch = branch;
        scope.BranchHandler = created => Branches.Add(created, scope);
        branch.AfterBranched += scope.BranchHandler;
    }

    internal static void BindTask(HookPlayerChoiceContext context, ref Task task)
    {
        if (!Branches.TryGetValue(context, out var scope))
            return;
        Branches.Remove(context);
        scope.BranchAssigned = true;
        // The host assigns THIS task to its already-created GenericHookGameAction. The original
        // player's queue is free to return at a cross-owner pause; no detached continuation exists.
        task = CompleteBranch(task, context, scope);
    }

    private static async Task CompleteBranch(Task business, HookPlayerChoiceContext context, WrapperScope scope)
    {
        try
        {
            await business;
        }
        catch (Exception error)
        {
            // Native Hook assignment can still be waiting to publish its pause when the
            // business task fails (e.g. enqueue failure). Do not await a wrapper that is waiting
            // on this very assigned task: terminate its barrier and fault the native task now.
            EndScope(scope, error);
            await Cleanup(scope.Frame, error);
            throw;
        }
        Exception? wrapperError = await scope.WrapperFinished.Task;
        if (wrapperError != null)
        {
            await Cleanup(scope.Frame, wrapperError);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(wrapperError).Throw();
        }
        await Continue(scope.Frame, scope.Next, context);
    }

    internal static async Task CompleteWrapper(Task original, WrapperScope scope)
    {
        try
        {
            await original;
            EndScope(scope, null);
            if (!scope.BranchAssigned)
                await Continue(scope.Frame, scope.Next, scope.Context);
        }
        catch (Exception error)
        {
            EndScope(scope, error);
            await Cleanup(scope.Frame, error);
            throw;
        }
    }

    private static void EndScope(WrapperScope scope, Exception? error)
    {
        if (scope.Ended)
            return;
        scope.Ended = true;
        if (scope.DirectBranch != null && scope.BranchHandler != null)
            scope.DirectBranch.AfterBranched -= scope.BranchHandler;
        scope.WrapperFinished.TrySetResult(error);
    }

    internal static Task SynchronousFailure(WrapperScope scope, Exception error)
    {
        EndScope(scope, error);
        return Cleanup(scope.Frame, error);
    }

    internal static void BeginCount(CardModel card, int playCount, Creature? target)
    {
        RegisterAdmission(card);
        var frame = Current(card);
        if (frame == null)
            return;
        frame.Target = target;
        frame.Contributions.Clear();
        if (playCount > 1)
            frame.Contributions.Add(new Contribution(null, playCount - 1));
    }

    private static int ModifyOne(AbstractModel listener, CardModel card, Creature? target, int playCount)
    {
        int result = listener.ModifyCardPlayCount(card, target, playCount);
        var frame = Current(card);
        if (frame == null)
            return result;
        int delta = result - playCount;
        if (delta > 0)
            frame.Contributions.Add(new Contribution(listener, delta));
        else if (delta < 0)
        {
            int remove = -delta;
            for (int i = frame.Contributions.Count - 1; i >= 0 && remove > 0; i--)
            {
                var contribution = frame.Contributions[i];
                int removed = Math.Min(remove, contribution.Count);
                contribution.Count -= removed;
                remove -= removed;
            }
        }
        return result;
    }


    internal static void FinishCount(CardModel card, ref int playCount)
    {
        var frame = Current(card);
        if (frame == null || !frame.Contributions.Any(c => ReferenceEquals(c.Source, frame.Power) && c.Count > 0))
            return;
        // StS1 inserts at cardQueue index 1: reverse hook-listener order, intrinsic replay last.
        for (int i = frame.Contributions.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < frame.Contributions[i].Count; j++)
            {
                var copy = card.CreateDupe(frame.Player);
                Copies.Add(copy, new CopyState(frame, frame.Replays.Count));
                frame.Replays.Add(copy);
                frame.PlayerState.PlayPile.AddInternal(copy, silent: true);
            }
        }
        frame.Power.CommitDouble();
        playCount = 1;
    }

    private static bool CanContinue(Frame frame) => !frame.Closed
        && CombatManager.Instance.IsCurrentLiveCombat(frame.CombatId) && !CombatManager.Instance.IsOverOrEnding
        && frame.Player.Creature.IsAlive && ReferenceEquals(frame.Original.Owner, frame.Player)
        && ReferenceEquals(frame.Player.Creature.CombatState, frame.Combat)
        && ReferenceEquals(frame.Player.PlayerCombatState, frame.PlayerState)
        && frame.Combat.RoundNumber == frame.Round && frame.Combat.CurrentSide == frame.Side
        && frame.PlayerState.TurnNumber == frame.Turn
        && (frame.Target == null || (frame.Target.IsAlive && ReferenceEquals(frame.Target.CombatState, frame.Combat)));

    private static async Task Continue(Frame frame, int next, PlayerChoiceContext context)
    {
        if (next >= frame.Replays.Count || !CanContinue(frame))
        {
            await Cleanup(frame);
            return;
        }
        var copy = frame.Replays[next];
        try
        {
            await CardCmd.AutoPlay(context, copy, frame.Target, AutoPlayType.Default,
                skipXCapture: true, skipCardPileVisuals: frame.SkipVisuals);
            // A skipped/unplayable copy never entered OnPlayWrapper, so it has not handed off
            // its tail. An entered copy owns its own tail, including any native cross-owner branch.
            if (Copies.TryGetValue(copy, out var state) && !state.Started)
                await Continue(frame, next + 1, context);
        }
        catch (Exception error)
        {
            await Cleanup(frame, error);
            throw;
        }
    }

    private static async Task Cleanup(Frame frame, Exception? primary = null)
    {
        if (frame.Closed)
            return;
        frame.Closed = true;
        List<Exception>? errors = null;
        foreach (var copy in frame.Replays)
        {
            try
            {
                // Inspect only the captured player's old piles. Replacement PCS/owner state is
                // not a cleanup target, and a silently prepared card has no published UI node.
                foreach (var pile in frame.PlayerState.AllPiles)
                {
                    if (!pile.Cards.Contains(copy))
                        continue;
                    if (ReferenceEquals(frame.Player.PlayerCombatState, frame.PlayerState)
                        && ReferenceEquals(frame.Player.Creature.CombatState, frame.Combat))
                        await CardPileCmd.RemoveFromCombat(copy, skipVisuals: true);
                    else
                        pile.RemoveInternal(copy, silent: true);
                    break;
                }
            }
            catch (Exception error) { (errors ??= new()).Add(error); }
            finally { Copies.Remove(copy); }
        }
        if (errors != null)
        {
            if (primary != null)
                errors.Insert(0, primary);
            throw new AggregateException("Echo replay cleanup failed.", errors);
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class Spire1EchoWrapperPatch
{
    internal sealed class State
    {
        internal Spire1EchoReplay.WrapperScope? Previous;
        internal Spire1EchoReplay.WrapperScope? Scope;
        internal Spire1EchoReplay.Admission? PreviousAdmission;
        internal Spire1EchoReplay.Admission? Admission;
    }

    [HarmonyPrefix]
    private static void Enter(CardModel __instance, PlayerChoiceContext choiceContext, bool skipCardPileVisuals, out State __state)
    {
        __state = new State
        {
            Previous = Spire1EchoReplay.Ambient,
            PreviousAdmission = Spire1EchoReplay.AmbientAdmission
        };
        __state.Admission = Spire1EchoReplay.EnterAdmission(__instance);
        __state.Scope = Spire1EchoReplay.Enter(__instance, choiceContext, skipCardPileVisuals);
    }

    [HarmonyPostfix]
    private static void Complete(ref Task __result, State __state)
    {
        if (__state.Admission != null)
            __result = Spire1EchoReplay.CompleteAdmission(__result, __state.Admission);
        if (__state.Scope != null)
            __result = Spire1EchoReplay.CompleteWrapper(__result, __state.Scope);
    }

    [HarmonyFinalizer]
    private static Exception? Exit(Exception? __exception, ref Task __result, State? __state)
    {
        if (__state == null)
            return __exception;
        try
        {
            if (__exception != null)
            {
                __result = __state.Scope == null ? Task.FromException(__exception)
                    : FailAfterCleanup(__state.Scope, __exception);
                if (__state.Admission != null)
                    __result = Spire1EchoReplay.CompleteAdmission(__result, __state.Admission);
                return null;
            }
            return __exception;
        }
        finally
        {
            // Only the native async invocation retains its scope; another request of the same
            // physical card gets a new scope and can finish in any order after a branch pause.
            Spire1EchoReplay.Restore(__state.Previous);
            Spire1EchoReplay.RestoreAdmission(__state.PreviousAdmission);
        }
    }

    private static async Task FailAfterCleanup(Spire1EchoReplay.WrapperScope scope, Exception error)
    {
        await Spire1EchoReplay.SynchronousFailure(scope, error);
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayCount))]
internal static class Spire1EchoCountPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Begin(CardModel card, int playCount, Creature? target,
        ref List<AbstractModel> modifyingModels, ref int __result)
    {
        Spire1EchoReplay.BeginCount(card, playCount, target);
        if (Spire1EchoReplay.IsCopy(card))
        {
            // No inherited Replay enchantment, Burst/Duplication charge or Echo recursion on a
            // purge copy. All those sources already decided exactly once on the paid original.
            modifyingModels = new List<AbstractModel>();
            __result = 1;
            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Record(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        if (__originalMethod is not MethodInfo entry)
            throw new InvalidOperationException("Echo count target is not a method.");
        var body = Spire1ContinuationIl.ForKickoff(entry);
        return Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Echo, body.Move,
            instructions as IReadOnlyList<CodeInstruction> ?? instructions.ToArray());
    }

    [HarmonyPostfix]
    private static void End(CardModel card, ref int __result) => Spire1EchoReplay.FinishCount(card, ref __result);
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayResultLocation))]
internal static class Spire1EchoPurgePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Purge(CardModel card, ref IEnumerable<AbstractModel> modifiers, ref CardLocation __result)
    {
        if (!Spire1EchoReplay.IsCopy(card))
            return true;
        // Purge precedes exhaust/return-to-hand/rebound in UseCardAction.update. Do not run
        // destination modifiers and then try to undo their consumption or trigger exhaust.
        modifiers = Array.Empty<AbstractModel>();
        __result = new CardLocation(card.Owner, PileType.None, CardPilePosition.Bottom);
        return false;
    }
}

[HarmonyPatch(typeof(BranchingPlayerChoiceContext), MethodType.Constructor,
    new[] { typeof(AbstractModel), typeof(ulong), typeof(PlayerChoiceContext) })]
internal static class Spire1EchoBranchPatch
{
    [HarmonyPostfix]
    private static void Capture(BranchingPlayerChoiceContext __instance, AbstractModel source, PlayerChoiceContext existing) =>
        Spire1EchoReplay.BindBranch(__instance, source, existing);
}

[HarmonyPatch(typeof(HookPlayerChoiceContext), nameof(HookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion))]
internal static class Spire1EchoBranchTaskPatch
{
    [HarmonyPrefix]
    private static void Bind(HookPlayerChoiceContext __instance, ref Task task) =>
        Spire1EchoReplay.BindTask(__instance, ref task);
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
internal static class Spire1EchoHistoryStartPatch
{
    [HarmonyPostfix]
    private static void Published(CardPlay cardPlay) => Spire1EchoReplay.PublishStart(cardPlay);
}
