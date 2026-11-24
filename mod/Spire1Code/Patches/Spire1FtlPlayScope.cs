using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;
using OwnAgony = Spire1.Spire1Code.Cards.EndlessAgony;
using OwnVoid = Spire1.Spire1Code.Cards.Void;
using OwnEvolve = Spire1.Spire1Code.Powers.EvolvePower;
using OwnFire = Spire1.Spire1Code.Powers.FireBreathingPower;
using OwnMasterReality = Spire1.Spire1Code.Powers.MasterRealityPower;
using OwnFtl = Spire1.Spire1Code.Cards.FTL;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// A lease on one real FTL OnPlayWrapper request. Native replay keeps its own CardPlay
/// objects and loop. Only admitted effect-local actions use this deque; native Tasks and
/// their real draw results, use hooks and attack commands remain authoritative.
/// </summary>
internal static class Spire1FtlPlayScope
{
    private const string PatchOwner = "Spire1.FtlPlayScope";
    private const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly object InstallationLock = new();
    private static readonly AsyncLocal<Frame?> Current = new();
    private static readonly AsyncLocal<Request?> Pending = new();
    private static readonly AsyncLocal<Operation?> CurrentDraw = new();
    private static readonly AsyncLocal<Operation?> CurrentAttack = new();
    private static readonly AsyncLocal<EffectContext?> CurrentUse = new();
    private static readonly ConditionalWeakTable<HookPlayerChoiceContext, EffectContext> Branches = new();
    private static bool _installed;
    private static (MethodInfo Target, MethodInfo Handler, HarmonyPatchType Kind)[]? _handlers;

    // The attribute patch below installs the wrapper carrier. These additional handlers are
    // installed before that carrier can be used, and checked again on the real card path.
    internal static void EnsureInstalled()
    {
        lock (InstallationLock)
        {
            if (_installed)
            {
                foreach (var item in _handlers!)
                    RequirePatch(item.Target, item.Handler, item.Kind);
                return;
            }
            MethodInfo draw = Exact(typeof(CardPileCmd), "DrawInternal", typeof(Task<IEnumerable<CardModel>>),
                typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool));
            MethodInfo attack = Exact(typeof(AttackCommand), nameof(AttackCommand.Execute), typeof(Task<AttackCommand>), typeof(PlayerChoiceContext));
            MethodInfo drawMove = MoveNext(draw);
            MethodInfo attackMove = MoveNext(attack);
            Spire1ContinuationIl.ForKickoff(draw);
            Spire1ContinuationIl.ForKickoff(attack);
            MethodInfo wrapper = Exact(typeof(CardModel), nameof(CardModel.OnPlayWrapper), typeof(Task),
                typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool));
            MethodInfo wrapperMove = MoveNext(wrapper);
            Spire1ContinuationIl.ForKickoff(wrapper);
            MethodInfo assign = Exact(typeof(HookPlayerChoiceContext), nameof(HookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion),
                typeof(Task<bool>), typeof(Task));
            MethodInfo ironClub = Exact(typeof(IronClub), nameof(AbstractModel.AfterCardPlayed), typeof(Task),
                typeof(PlayerChoiceContext), typeof(CardPlay));
            MethodInfo ironClubMove = MoveNext(ironClub);
            Spire1ContinuationIl.ForKickoff(ironClub);
            var harmony = new Harmony(PatchOwner);
            var handlers = new (MethodInfo Target, MethodInfo Handler, HarmonyPatchType Kind)[] {
                (draw, Own(nameof(EnterDraw)), HarmonyPatchType.Prefix),
                (draw, Own(nameof(CompleteDraw)), HarmonyPatchType.Postfix),
                (draw, Own(nameof(RestoreDraw)), HarmonyPatchType.Finalizer),
                (drawMove, Own(nameof(GuardDrawContinuations)), HarmonyPatchType.Transpiler),
                (attack, Own(nameof(EnterAttack)), HarmonyPatchType.Prefix),
                (attack, Own(nameof(CompleteAttack)), HarmonyPatchType.Postfix),
                (attack, Own(nameof(RestoreAttack)), HarmonyPatchType.Finalizer),
                (attackMove, Own(nameof(GuardAttackContinuations)), HarmonyPatchType.Transpiler),
                (wrapperMove, Own(nameof(BridgeUseBoundary)), HarmonyPatchType.Transpiler),
                (ironClubMove, Own(nameof(BridgeIronClubDraw)), HarmonyPatchType.Transpiler),
                (assign, Own(nameof(BindTask)), HarmonyPatchType.Prefix),
                (Exact(typeof(OwnEvolve), nameof(AbstractModel.AfterCardDrawn), typeof(Task), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)), Own(nameof(CaptureEvolve)), HarmonyPatchType.Prefix),
                (Exact(typeof(OwnFire), nameof(AbstractModel.AfterCardDrawn), typeof(Task), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)), Own(nameof(CaptureFire)), HarmonyPatchType.Prefix),
                (Exact(typeof(OwnVoid), nameof(AbstractModel.AfterCardDrawn), typeof(Task), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)), Own(nameof(CaptureVoid)), HarmonyPatchType.Prefix),
                (Exact(typeof(OwnAgony), nameof(AbstractModel.AfterCardDrawn), typeof(Task), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)), Own(nameof(CaptureAgony)), HarmonyPatchType.Prefix)
            };
            try
            {
                foreach (var item in handlers)
                {
                    var method = new HarmonyMethod(item.Handler) { priority = PatchPriority(item.Handler) };
                    if (item.Handler == Own(nameof(GuardAttackContinuations)))
                        method.after = ["com.ritsukage.sts2-RitsuLib.framework-core"];
                    switch (item.Kind)
                    {
                        case HarmonyPatchType.Prefix: harmony.Patch(item.Target, prefix: method); break;
                        case HarmonyPatchType.Postfix: harmony.Patch(item.Target, postfix: method); break;
                        case HarmonyPatchType.Finalizer: harmony.Patch(item.Target, finalizer: method); break;
                        case HarmonyPatchType.Transpiler: harmony.Patch(item.Target, transpiler: method); break;
                    }
                }
                foreach (var item in handlers)
                    RequirePatch(item.Target, item.Handler, item.Kind);
                _handlers = handlers;
                _installed = true;
            }
            catch
            {
                // Exact handlers only: a failed FTL install must never remove Reboot or another mod.
                foreach (var item in handlers)
                    harmony.Unpatch(item.Target, item.Handler);
                throw;
            }
        }
    }

    private static MethodInfo Exact(Type owner, string name, Type result, params Type[] parameters)
    {
        MethodInfo method = owner.GetMethod(name, Methods, null, parameters, null)
            ?? throw new MissingMethodException(owner.FullName, name);
        if (method.ReturnType != result)
            throw new InvalidOperationException("FTL native signature changed: " + owner.Name + "." + name);
        return method;
    }

    private static MethodInfo MoveNext(MethodInfo kickoff)
    {
        Type state = kickoff.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
            ?? throw new InvalidOperationException("FTL native consumer is no longer async: " + kickoff.Name);
        if (state.DeclaringType != kickoff.DeclaringType || !typeof(IAsyncStateMachine).IsAssignableFrom(state))
            throw new InvalidOperationException("FTL native async consumer owner changed.");
        return Exact(state, "MoveNext", typeof(void));
    }

    private static MethodInfo Own(string name) => typeof(Spire1FtlPlayScope).GetMethod(name, Methods)!;

    private static int PatchPriority(MethodInfo method) => method == Own(nameof(BindTask))
        || method.DeclaringType == typeof(Spire1FtlWrapperPatch) && method.Name == "Complete" ? Priority.First : Priority.Last;

    private static void RequirePatch(MethodInfo target, MethodInfo method, HarmonyPatchType kind)
    {
        var info = Harmony.GetPatchInfo(target) ?? throw new InvalidOperationException("Missing FTL patch: " + target.Name);
        IEnumerable<Patch> patches = kind switch {
            HarmonyPatchType.Prefix => info.Prefixes,
            HarmonyPatchType.Postfix => info.Postfixes,
            HarmonyPatchType.Finalizer => info.Finalizers,
            HarmonyPatchType.Transpiler => info.Transpilers,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (patches.Count(p => p.PatchMethod == method && p.priority == PatchPriority(method)) != 1)
            throw new InvalidOperationException("Missing or duplicate FTL handler: " + method.Name);
    }

    private static void RequireInstalled()
    {
        if (!_installed)
            throw new InvalidOperationException("FTL play scope is unavailable; restart with compatible game and mod versions.");
        MethodInfo wrapper = Exact(typeof(CardModel), nameof(CardModel.OnPlayWrapper), typeof(Task),
            typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool));
        foreach (var (name, kind) in new[] {
            ("Enter", HarmonyPatchType.Prefix), ("Complete", HarmonyPatchType.Postfix), ("Restore", HarmonyPatchType.Finalizer) })
            RequirePatch(wrapper, typeof(Spire1FtlWrapperPatch).GetMethod(name, Methods)!, kind);
        foreach (var item in _handlers!)
            RequirePatch(item.Target, item.Handler, item.Kind);
    }

    internal sealed class Frame
    {
        internal readonly OwnFtl Card;
        internal readonly Player Player;
        internal readonly PlayerCombatState PlayerState;
        internal readonly Creature Dealer;
        internal readonly ICombatState Combat;
        internal readonly CombatId? CombatId;
        internal readonly CombatHistory History;
        private int _leases = 1;
        private int _admissionClosed;
        private int _aborted;
        private int _nextPlay;
        private int _playCount;
        private readonly List<EffectContext> _effects = new();

        internal Frame(OwnFtl card)
        {
            Card = card;
            Player = card.Owner;
            PlayerState = Player.PlayerCombatState ?? throw new InvalidOperationException("FTL has no player combat state.");
            Dealer = Player.Creature;
            Combat = Dealer.CombatState ?? throw new InvalidOperationException("FTL has no combat state.");
            var manager = CombatManager.Instance;
            CombatId = manager.CurrentCombatId;
            if (!CombatId.HasValue)
                throw new InvalidOperationException("FTL has no current combat identity.");
            History = manager.History;
            RequireIdentity();
        }

        internal bool Accepting => Volatile.Read(ref _admissionClosed) == 0;

        internal void RequireIdentity()
        {
            var manager = CombatManager.Instance;
            if (Volatile.Read(ref _aborted) != 0 || Volatile.Read(ref _leases) <= 0
                || !ReferenceEquals(Card.Owner, Player) || !ReferenceEquals(Player.PlayerCombatState, PlayerState)
                || !ReferenceEquals(Player.Creature, Dealer) || !ReferenceEquals(Dealer.CombatState, Combat)
                || manager.CurrentCombatId != CombatId || !ReferenceEquals(manager.DebugOnlyGetState(), Combat)
                || !ReferenceEquals(manager.History, History))
                throw new InvalidOperationException("FTL continuation lost its original physical play, owner or combat state.");
        }

        internal bool CanResolve => !CombatManager.Instance.IsOverOrEnding && Dealer.IsAlive;

        internal EffectContext Admit(OwnFtl card, CardPlay play, PlayerChoiceContext context)
        {
            lock (this)
            {
                RequireIdentity();
                if (!Accepting || !ReferenceEquals(card, Card) || !ReferenceEquals(play.Card, Card)
                    || !ReferenceEquals(play.Player, Player) || play.PlayCount <= 0 || play.PlayIndex != _nextPlay
                    || (_nextPlay != 0 && play.PlayCount != _playCount) || play.PlayIndex >= play.PlayCount)
                    throw new InvalidOperationException("FTL effect does not belong to this native physical play series.");
                var effect = new EffectContext(this, play, context);
                _effects.Add(effect);
                _playCount = play.PlayCount;
                _nextPlay++;
                Interlocked.Increment(ref _leases);
                return effect;
            }
        }

        internal EffectContext Find(CardPlay play) => _effects.Single(e => ReferenceEquals(e.Play, play));

        internal void FinishWrapper(Exception? failure)
        {
            lock (this)
            {
                if (failure != null)
                    Volatile.Write(ref _aborted, 1);
                if (Interlocked.Exchange(ref _admissionClosed, 1) == 0)
                {
                    foreach (var effect in _effects)
                        effect.FinishWithoutUse(failure);
                    Release();
                }
            }
        }

        internal void Release() => Interlocked.Decrement(ref _leases);
    }

    internal sealed class EffectContext : IDisposable
    {
        internal readonly Frame Frame;
        internal readonly CardPlay Play;
        internal readonly PlayerChoiceContext Choice;
        private readonly TaskCompletionSource<UseHandoff> _useReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private LinkedList<Intent>? _head;
        private Queue<Intent>? _tail;
        private Spire1DamageSnapshot? _snapshot;
        private BranchingPlayerChoiceContext? _branch;
        private Action<HookPlayerChoiceContext>? _branchHandler;
        private int _closed;
        private int _useClaimed;
        private int _usePublished;
        internal HookPlayerChoiceContext? BranchContext;
        internal Task? BranchTask;
        internal PlayerChoiceContext? UseChoice;
        private readonly HashSet<AbstractModel> _useDrawProducers = new(ReferenceEqualityComparer.Instance);

        internal bool ClaimUseDraw(AbstractModel producer, PlayerChoiceContext context, CardPlay play)
        {
            RequireIdentity();
            if (!ReferenceEquals(CurrentUse.Value, this) || !ReferenceEquals(Current.Value, Frame)
                || !ReferenceEquals(UseChoice, context) || !ReferenceEquals(Play, play)
                || !ReferenceEquals(context.LastInvolvedModel, producer) || CurrentDraw.Value != null || CurrentAttack.Value != null)
                return false;
            if (!_useDrawProducers.Add(producer))
                throw new InvalidOperationException("FTL exact IronClub use draw was claimed twice for one CardPlay.");
            return true;
        }
        internal bool Closed => Volatile.Read(ref _closed) != 0;

        internal EffectContext(Frame frame, CardPlay play, PlayerChoiceContext choice)
        {
            (Frame, Play, Choice) = (frame, play, choice);
            if (choice is BranchingPlayerChoiceContext branch)
            {
                _branch = branch;
                _branchHandler = hook =>
                {
                    if (BranchContext != null)
                        throw new InvalidOperationException("FTL effect branched more than once.");
                    BranchContext = hook;
                    Branches.Add(hook, this);
                };
                branch.AfterBranched += _branchHandler;
            }
        }

        internal void RequireIdentity()
        {
            if (Volatile.Read(ref _closed) != 0 || !ReferenceEquals(Play.Card, Frame.Card) || !ReferenceEquals(Play.Player, Frame.Player))
                throw new InvalidOperationException("FTL effect lease is closed or belongs to another card play.");
            Frame.RequireIdentity();
        }

        internal bool CanResolve
        {
            get { RequireIdentity(); return Frame.CanResolve; }
        }


        // This Task is the actual native OnPlay front. It is not allowed to wait for U on
        // the ordinary path, since the wrapper reaches U only after this front returns.
        internal async Task Front(int threshold)
        {
            try
            {
                RequireIdentity();
                if (!CanResolve)
                    return;
                _snapshot = Spire1DamageSnapshot.CaptureLegacyUse(Frame.Card, Play);
                int current = 0, prior = 0;
                foreach (var entry in Frame.History.CardPlaysStarted)
                {
                    if (!entry.HappenedThisTurn(Frame.Combat) || !ReferenceEquals(entry.CardPlay.Player, Frame.Player))
                        continue;
                    if (ReferenceEquals(entry.CardPlay, Play)) current++;
                    else prior++;
                }
                if (current != 1)
                    throw new InvalidOperationException("FTL requires exactly one native start for the actual CardPlay.");
                if (prior < threshold)
                    Head(new Intent(IntentKind.Draw, 1));
                await DrainHeads(Choice);
                RequireIdentity();
                // Old onUseCard captured only the presence of this ID. A power gained by
                // the front is not consumed if there was no original remove intent.
                if (_snapshot.HadVigor && Frame.Dealer.IsAlive)
                {
                    var vigor = Frame.Dealer.GetPower<VigorPower>();
                    if (vigor != null)
                        await PowerCmd.Remove(vigor);
                    RequireIdentity();
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Task BindBranch(Task front, HookPlayerChoiceContext context)
        {
            if (!ReferenceEquals(context, BranchContext) || BranchTask != null)
                throw new InvalidOperationException("FTL native Hook task was assigned twice or to another branch.");
            return BranchTask = CompleteBranch(front, context);
        }

        private async Task CompleteBranch(Task front, HookPlayerChoiceContext context)
        {
            try
            {
                await front;
                UseHandoff use = await _useReached.Task;
                if (use.Failure != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(use.Failure).Throw();
                if (use.Reached)
                    await ResolveUse(context);
            }
            finally { Dispose(); }
        }

        internal async Task ReachUse(ICombatState combat, PlayerChoiceContext originalContext)
        {
            RequireIdentity();
            if (!ReferenceEquals(combat, Frame.Combat) || Interlocked.Exchange(ref _usePublished, 1) != 0)
                throw new InvalidOperationException("FTL use boundary was repeated or belongs to another combat.");
            if (BranchContext == null)
            {
                try { await ResolveUse(originalContext); }
                finally { Dispose(); }
                return;
            }
            Task actual = BranchTask ?? throw new InvalidOperationException("FTL branch has no native-owned composite Task.");
            _useReached.TrySetResult(new UseHandoff(true, originalContext, null));
            // This is the native pause acknowledgement, not a fake completed U. If the
            // real composite is already terminal, observe its actual fault/cancellation.
            await BranchContext.WaitForPauseOrCompletionWithoutAssigningTask(actual);
            if (actual.IsCompleted)
                await actual;
        }

        private async Task ResolveUse(PlayerChoiceContext context)
        {
            RequireIdentity();
            if (Interlocked.Exchange(ref _useClaimed, 1) != 0)
                throw new InvalidOperationException("FTL native AfterCardPlayed was invoked twice.");
            var previousUse = CurrentUse.Value;
            UseChoice = context;
            CurrentUse.Value = this;
            try
            {
                // The original native U is always invoked and awaited exactly once. Its
                // direct dispatch deliberately completes cards that ended their combat.
                await Hook.AfterCardPlayed(Frame.Combat, context, Play);
            }
            finally { CurrentUse.Value = previousUse; }
            RequireIdentity();
            await DrainHeads(context);
            if (CanResolve && _snapshot?.CanAttackTarget() == true)
                await Attack(_snapshot.CreateAttack(), context);
            RequireIdentity();
            while (_tail != null && _tail.TryDequeue(out var intent))
            {
                if (!CanResolve)
                {
                    intent.Discard();
                    break;
                }
                await Consume(intent, context);
                await DrainHeads(context);
            }
        }

        private async Task Attack(AttackCommand command, PlayerChoiceContext context)
        {
            RequireIdentity();
            if (!ReferenceEquals(command.CardPlay, Play) || !ReferenceEquals(command.ModelSource, Frame.Card)
                || !ReferenceEquals(command.Attacker, Frame.Dealer))
                throw new InvalidOperationException("FTL attack command lost the actual native CardPlay.");
            var previous = Pending.Value;
            Pending.Value = new Request(this, command, context);
            try { await command.Execute(context); }
            finally { Pending.Value = previous; }
        }

        internal void Head(Intent intent) => (_head ??= new()).AddFirst(intent);
        internal void Tail(Intent intent) => (_tail ??= new()).Enqueue(intent);

        private async Task DrainHeads(PlayerChoiceContext context)
        {
            while (_head?.First is { } first)
            {
                _head.RemoveFirst();
                if (!CanResolve)
                {
                    first.Value.Discard();
                    while (_head?.First is { } stopped) { _head.RemoveFirst(); stopped.Value.Discard(); }
                    return;
                }
                await Consume(first.Value, context);
                RequireIdentity();
            }
        }

        private async Task Consume(Intent intent, PlayerChoiceContext context)
        {
            try
            {
                RequireIdentity();
                switch (intent.Kind)
                {
                    case IntentKind.Draw:
                        await DrawPacket(intent.Amount, context, intent.Returned, intent.FromHandDraw);
                        break;
                    case IntentKind.Shuffle:
                        await CardPileCmd.Shuffle(context, Frame.Player);
                        break;
                    case IntentKind.Energy:
                        await PlayerCmd.LoseEnergy(intent.Amount, Frame.Player);
                        break;
                    case IntentKind.Clone:
                        await CardPileCmd.AddGeneratedCardToCombat(intent.Card!, PileType.Hand, Frame.Player);
                        intent.Committed = intent.Card!.Pile != null;
                        break;
                    case IntentKind.Fire:
                        await Fire(intent, context);
                        break;
                }
                RequireIdentity();
            }
            finally { intent.Discard(); }
        }

        private async Task DrawPacket(int requested, PlayerChoiceContext context, List<CardModel>? returned = null,
            bool fromHandDraw = false)
        {
            DrawAdmission? admission = null;
            try
            {
                RequireIdentity();
                if (!CanResolve)
                    return;
                admission = new DrawAdmission(Frame.Combat, Frame.Player, fromHandDraw);
                if (!admission.Allowed || requested <= 0)
                {
                    foreach (var card in await NativeDraw(requested, context, fromHandDraw, admission)) returned?.Add(card);
                    return;
                }
                int remaining = Math.Min(requested, Math.Max(0, CardPile.MaxCardsInHand - Frame.PlayerState.Hand.Cards.Count));
                int available = Frame.PlayerState.DrawPile.Cards.Count;
                if (remaining == 0 || available == 0 && Frame.PlayerState.DiscardPile.Cards.Count == 0)
                {
                    foreach (var card in await NativeDraw(requested, context, fromHandDraw, admission)) returned?.Add(card);
                    return;
                }
                if (remaining > available && Frame.PlayerState.DiscardPile.Cards.Count != 0)
                {
                    Head(new Intent(IntentKind.Draw, remaining - available) { Returned = returned, FromHandDraw = fromHandDraw });
                    Head(new Intent(IntentKind.Shuffle, 0));
                    if (available > 0) Head(new Intent(IntentKind.Draw, available) { Returned = returned, FromHandDraw = fromHandDraw });
                    return;
                }
                for (int i = 0; i < remaining && Frame.PlayerState.DrawPile.Cards.Count > 0; i++)
                {
                    if (!CanResolve || Frame.PlayerState.Hand.Cards.Count >= CardPile.MaxCardsInHand)
                        return;
                    IEnumerable<CardModel> drawn = await NativeDraw(1, context, fromHandDraw, i == 0 ? admission : null);
                    RequireIdentity();
                    bool any = false;
                    foreach (CardModel card in drawn) { any = true; returned?.Add(card); }
                    if (!any) return;
                }
            }
            finally { admission?.Expire(); }
        }

        private async Task<IEnumerable<CardModel>> NativeDraw(decimal count, PlayerChoiceContext context, bool fromHandDraw, DrawAdmission? admission)
        {
            var previous = Pending.Value;
            Pending.Value = new Request(this, null, context) { Admission = admission };
            try { return await CardPileCmd.Draw(context, count, Frame.Player, fromHandDraw); }
            finally { Pending.Value = previous; admission?.Expire(); }
        }

        internal async Task<IEnumerable<CardModel>> DrawFromUse(decimal requested, PlayerChoiceContext context, bool fromHandDraw)
        {
            RequireIdentity();
            int count = requested > 0m ? (int)decimal.Ceiling(requested) : 0;
            var cards = new List<CardModel>();
            // This is an actual awaited draw operation. The native caller receives the
            // physical cards returned by its completed native Draw tasks, never an empty
            // enqueue acknowledgement. The same deque supplies shuffle packet boundaries.
            Head(new Intent(IntentKind.Draw, count) { Returned = cards, FromHandDraw = fromHandDraw });
            await DrainHeads(context);
            return cards;
        }

        private async Task Fire(Intent intent, PlayerChoiceContext context)
        {
            var targets = Frame.Combat.Enemies.ToArray();
            if (targets.Length != intent.Matrix!.Length)
                throw new InvalidOperationException("FTL Fire Breathing matrix/current enemy ordinal length changed.");
            var expected = targets.ToList();
            for (int i = 0; i < targets.Length; i++)
            {
                RequireIdentity();
                if (!Frame.Combat.Enemies.SequenceEqual(expected, ReferenceEqualityComparer.Instance))
                    throw new InvalidOperationException("FTL Fire Breathing enemies changed during its actual consumer.");
                if (targets[i].IsDead)
                    continue;
                if (!ReferenceEquals(targets[i].CombatState, Frame.Combat) || !Frame.Combat.ContainsCreature(targets[i]))
                    throw new InvalidOperationException("FTL Fire Breathing live target left its original combat.");
                await CreatureCmd.Damage(context, new[] { new DamageTargetData(targets[i]) }, intent.Matrix[i],
                    ValueProp.Unpowered, null, null, null, ModifyDamageHookType.Cap);
                RequireIdentity();
                if (!Frame.Combat.ContainsCreature(targets[i]) && targets[i].IsDead)
                    expected.Remove(targets[i]);
                if (!Frame.Combat.Enemies.SequenceEqual(expected, ReferenceEqualityComparer.Instance))
                    throw new InvalidOperationException("FTL Fire Breathing enemy ordinals changed after actual damage.");
            }
        }

        internal void FinishWithoutUse(Exception? error)
        {
            if (Volatile.Read(ref _usePublished) != 0)
                return;
            if (BranchTask != null)
                _useReached.TrySetResult(new UseHandoff(false, null, error));
            else
                Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0)
                return;
            if (_branch != null && _branchHandler != null)
                _branch.AfterBranched -= _branchHandler;
            if (BranchContext != null)
                Branches.Remove(BranchContext);
            while (_head?.First is { } first) { _head.RemoveFirst(); first.Value.Discard(); }
            while (_tail != null && _tail.TryDequeue(out var intent)) intent.Discard();
            Frame.Release();
        }
    }

    private sealed record UseHandoff(bool Reached, PlayerChoiceContext? OriginalChoice, Exception? Failure);

    private static void BindTask(HookPlayerChoiceContext __instance, ref Task task)
    {
        if (!Branches.TryGetValue(__instance, out var effect))
            return;
        Branches.Remove(__instance);
        task = effect.BindBranch(task, __instance);
    }

    internal static Task AfterCardPlayed(ICombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not OwnFtl)
            return Hook.AfterCardPlayed(combatState, choiceContext, cardPlay);
        Frame frame = Current.Value ?? throw new InvalidOperationException("FTL use boundary lost its physical wrapper.");
        if (!ReferenceEquals(frame.Card, cardPlay.Card))
            throw new InvalidOperationException("FTL use boundary belongs to another wrapper.");
        return frame.Find(cardPlay).ReachUse(combatState, choiceContext);
    }

    internal enum IntentKind { Draw, Shuffle, Clone, Energy, Fire }

    internal sealed class Intent(IntentKind kind, int amount)
    {
        internal readonly IntentKind Kind = kind;
        internal readonly int Amount = amount;
        internal CardModel? Card;
        internal CombatState? CreationCombat;
        internal int[]? Matrix;
        internal bool Committed;
        internal List<CardModel>? Returned;
        internal bool FromHandDraw;
        internal void Discard()
        {
            if (!Committed && Card is { Pile: null } card && CreationCombat?.ContainsCard(card) == true)
                CreationCombat.RemoveCard(card);
        }
    }

    internal sealed class DrawAdmission
    {
        private readonly ICombatState _combat;
        private readonly Player _player;
        private readonly bool _fromHandDraw;
        private readonly AbstractModel? _preventer;
        private int _state;
        internal bool Allowed { get; }

        internal DrawAdmission(ICombatState combat, Player player, bool fromHandDraw)
        {
            (_combat, _player, _fromHandDraw) = (combat, player, fromHandDraw);
            Allowed = Hook.ShouldDraw(combat, player, fromHandDraw, out _preventer);
        }

        internal bool Consume(ICombatState combat, Player player, bool fromHandDraw, out AbstractModel? preventer)
        {
            if (!ReferenceEquals(combat, _combat) || !ReferenceEquals(player, _player) || fromHandDraw != _fromHandDraw
                || Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                throw new InvalidOperationException("FTL native draw admission ticket changed or was consumed twice.");
            preventer = _preventer;
            return Allowed;
        }

        internal void Expire() => Interlocked.Exchange(ref _state, 2);
    }

    internal static bool ShouldDraw(ICombatState combatState, Player player, bool fromHandDraw, out AbstractModel? modifier)
    {
        Operation? operation = CurrentDraw.Value;
        EffectContext? effect = Selected(operation);
        if (effect == null || operation!.Request!.Admission == null)
            return Hook.ShouldDraw(combatState, player, fromHandDraw, out modifier);
        return operation.Request.Admission.Consume(combatState, player, fromHandDraw, out modifier);
    }

    internal static Task<IEnumerable<CardModel>> DrawFromUse(PlayerChoiceContext choiceContext, decimal count,
        Player player, bool fromHandDraw, CardPlay cardPlay, IronClub producer)
    {
        EffectContext? effect = CurrentUse.Value;
        if (effect == null || !ReferenceEquals(producer.Owner, player) || !ReferenceEquals(effect.Frame.Player, player)
            || !effect.ClaimUseDraw(producer, choiceContext, cardPlay))
            return CardPileCmd.Draw(choiceContext, count, player, fromHandDraw);
        if (count != 1m || fromHandDraw)
            throw new InvalidOperationException("FTL approved IronClub callsite no longer requests its exact native Draw1.");
        return effect.DrawFromUse(count, choiceContext, fromHandDraw);
    }

    internal sealed class DrawTicket : IDisposable
    {
        internal readonly EffectContext Effect;
        internal readonly CardModel Card;
        internal readonly PlayerChoiceContext Choice;
        private readonly List<(PowerModel Power, Intent Intent)> _powers = new();
        private Intent? _cardIntent;
        private bool _cardSeen;
        private bool _complete;

        internal DrawTicket(EffectContext effect, CardModel card, PlayerChoiceContext choice)
        {
            (Effect, Card, Choice) = (effect, card, choice);
            if (card is OwnVoid)
                _cardIntent = new Intent(IntentKind.Energy, 1);
            else if (card is OwnAgony && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
            {
                CombatState scope = card.CardScope as CombatState
                    ?? throw new InvalidOperationException("FTL's early Endless Agony has no combat creation scope.");
                if (!ReferenceEquals(scope, effect.Frame.Combat))
                    throw new InvalidOperationException("FTL's early copy belongs to another combat.");
                CardModel clone = card.CreateCloneForPlayer(effect.Frame.Player);
                _cardIntent = new Intent(IntentKind.Clone, 0) { Card = clone, CreationCombat = scope };
                try
                {
                    if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)
                        && effect.Frame.Dealer.GetPower<OwnMasterReality>() != null && clone.IsUpgradable)
                    {
                        clone.UpgradeInternal();
                        clone.FinalizeUpgradeInternal();
                    }
                }
                catch { _cardIntent.Discard(); throw; }
            }
        }

        internal void CardCallback(CardModel card)
        {
            if (!ReferenceEquals(card, Card) || _cardSeen)
                throw new InvalidOperationException("FTL drawn-card callback changed its exact physical card.");
            _cardSeen = true;
        }

        internal void PowerCallback(PowerModel power, Intent intent)
        {
            Effect.RequireIdentity();
            if (_complete || !ReferenceEquals(power.Owner, Effect.Frame.Dealer)
                || !ReferenceEquals(CurrentDraw.Value?.Ticket, this) || !ReferenceEquals(Choice.LastInvolvedModel, power)
                || _powers.Any(p => ReferenceEquals(p.Power, power)))
                throw new InvalidOperationException("FTL drawn-card power callback lost its native ticket, owner, context or uniqueness.");
            _powers.Add((power, intent));
        }

        internal void Complete()
        {
            Effect.RequireIdentity();
            bool nativeMaySkip = CombatManager.Instance.IsOverOrEnding && !CombatManager.Instance.IsStarting;
            if (_complete || Card is OwnVoid or OwnAgony && !_cardSeen && !nativeMaySkip)
                throw new InvalidOperationException("FTL native draw did not complete its original card callback exactly once.");
            if (!_cardSeen && _cardIntent != null)
            {
                _cardIntent.Discard();
                _cardIntent = null;
            }
            _complete = true;
            // Only real callbacks commit intents. Card intents precede the power intents,
            // which retain their actual native notification order, including new instances.
            if (_cardIntent != null)
            {
                if (_cardIntent.Kind == IntentKind.Clone) Effect.Head(_cardIntent);
                else Effect.Tail(_cardIntent);
            }
            foreach (var item in _powers)
                Effect.Tail(item.Intent);
        }

        public void Dispose()
        {
            if (!_complete)
            {
                _cardIntent?.Discard();
                foreach (var item in _powers) item.Intent.Discard();
            }
        }
    }

    internal static async Task<CardPileAddResult> AddDrawn(CardModel card, CardPile pile, CardPilePosition position,
        AbstractModel? clonedBy, bool skipVisuals, PlayerChoiceContext choiceContext)
    {
        Operation? operation = CurrentDraw.Value;
        EffectContext? effect = Selected(operation);
        if (effect == null)
            return await Spire1ExhaustTransaction.DrawAdd(card, pile, position, clonedBy, skipVisuals);
        if (!ReferenceEquals(operation!.Request!.Choice, choiceContext) || !ReferenceEquals(card.Owner, effect.Frame.Player)
            || !ReferenceEquals(pile, effect.Frame.PlayerState.Hand) || !ReferenceEquals(card.Pile, effect.Frame.PlayerState.DrawPile)
            || operation.Ticket != null)
            throw new InvalidOperationException("FTL early draw ticket lost its exact native Add consumer.");
        var ticket = new DrawTicket(effect, card, choiceContext);
        operation.Ticket = ticket;
        try
        {
            var result = await Spire1ExhaustTransaction.DrawAdd(card, pile, position, clonedBy, skipVisuals);
            effect.RequireIdentity();
            return result;
        }
        catch { ticket.Dispose(); throw; }
    }

    internal static void InvokeDrawn(CardModel card, PlayerChoiceContext choiceContext)
    {
        Operation? operation = CurrentDraw.Value;
        EffectContext? effect = Selected(operation);
        if (effect == null)
        {
            card.InvokeDrawn();
            return;
        }
        DrawTicket ticket = operation!.Ticket ?? throw new InvalidOperationException("FTL draw has no early physical ticket.");
        if (!ReferenceEquals(ticket.Card, card) || !ReferenceEquals(ticket.Choice, choiceContext))
            throw new InvalidOperationException("FTL native drawn notification belongs to another card or context.");
        card.InvokeDrawn();
        effect.RequireIdentity();
        ticket.Complete();
        ticket.Dispose();
        operation.Ticket = null;
    }

    private static DrawTicket? Ticket(PlayerChoiceContext context, CardModel card)
    {
        Operation? operation = CurrentDraw.Value;
        EffectContext? effect = Selected(operation);
        if (effect == null)
            return null;
        DrawTicket ticket = operation!.Ticket ?? throw new InvalidOperationException("FTL selected draw callback has no physical ticket.");
        return ReferenceEquals(ticket.Choice, context) && ReferenceEquals(ticket.Card, card) ? ticket : null;
    }

    private static bool CaptureVoid(OwnVoid __instance, PlayerChoiceContext choiceContext, CardModel card, ref Task __result)
    {
        if (!ReferenceEquals(__instance, card)) return true;
        DrawTicket? ticket = Ticket(choiceContext, card);
        if (ticket == null) return true;
        ticket.CardCallback(card);
        __result = Task.CompletedTask;
        return false;
    }

    private static bool CaptureAgony(OwnAgony __instance, PlayerChoiceContext choiceContext, CardModel card, ref Task __result)
    {
        if (!ReferenceEquals(__instance, card)) return true;
        DrawTicket? ticket = Ticket(choiceContext, card);
        if (ticket == null) return true;
        ticket.CardCallback(card);
        __result = Task.CompletedTask;
        return false;
    }

    private static bool CaptureEvolve(OwnEvolve __instance, PlayerChoiceContext choiceContext, CardModel card, ref Task __result)
    {
        if (!ReferenceEquals(card.Owner.Creature, __instance.Owner) || card.Type != CardType.Status
            || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)) return true;
        DrawTicket? ticket = Ticket(choiceContext, card);
        if (ticket == null) return true;
        if (card.Owner.Creature.GetPower<NoDrawPower>() == null)
        {
            __instance.Flash();
            ticket.PowerCallback(__instance, new Intent(IntentKind.Draw, __instance.Amount));
        }
        __result = Task.CompletedTask;
        return false;
    }

    private static bool CaptureFire(OwnFire __instance, PlayerChoiceContext choiceContext, CardModel card, ref Task __result)
    {
        if (!ReferenceEquals(card.Owner.Creature, __instance.Owner) || card.Type is not (CardType.Status or CardType.Curse)
            || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers)) return true;
        DrawTicket? ticket = Ticket(choiceContext, card);
        if (ticket == null) return true;
        __instance.Flash();
        int amount = __instance.Amount;
        ticket.PowerCallback(__instance, new Intent(IntentKind.Fire, amount)
        {
            Matrix = Enumerable.Repeat(amount, ticket.Effect.Frame.Combat.Enemies.Count).ToArray()
        });
        __result = Task.CompletedTask;
        return false;
    }


    internal static EffectContext Begin(OwnFtl card, CardPlay play, PlayerChoiceContext context)
    {
        return (Current.Value ?? throw new InvalidOperationException("FTL requires its actual native OnPlayWrapper request."))
            .Admit(card, play, context);
    }

    internal sealed class WrapperCall
    {
        internal readonly Frame? PreviousFrame = Current.Value;
        internal readonly Request? PreviousPending = Pending.Value;
        internal readonly Operation? PreviousDraw = CurrentDraw.Value;
        internal readonly Operation? PreviousAttack = CurrentAttack.Value;
        internal readonly EffectContext? PreviousUse = CurrentUse.Value;
        internal Frame? Frame;

        internal static WrapperCall? Capture(CardModel card) => card is OwnFtl || Current.Value != null
            || Pending.Value != null || CurrentDraw.Value != null || CurrentAttack.Value != null || CurrentUse.Value != null ? new WrapperCall() : null;
    }

    internal static void EnterWrapperInto(CardModel card, WrapperCall call)
    {
        // Every nested wrapper shadows the parent, including ordinary cards with the same owner.
        Current.Value = null;
        Pending.Value = null;
        CurrentDraw.Value = null;
        CurrentAttack.Value = null;
        CurrentUse.Value = null;
        if (card is OwnFtl ftl)
        {
            RequireInstalled();
            if (ExecutionContext.IsFlowSuppressed())
                throw new InvalidOperationException("FTL requires native ExecutionContext flow.");
            call.Frame = new Frame(ftl);
            Current.Value = call.Frame;
        }
    }

    internal static async Task CompleteWrapper(Task original, WrapperCall call)
    {
        Exception? failure = null;
        try { await original; }
        catch (Exception error) { failure = error; throw; }
        finally { call.Frame?.FinishWrapper(failure); }
    }

    internal static void RestoreWrapper(WrapperCall call, Exception? failure)
    {
        if (failure != null)
            call.Frame?.FinishWrapper(failure);
        Current.Value = call.PreviousFrame;
        Pending.Value = call.PreviousPending;
        CurrentDraw.Value = call.PreviousDraw;
        CurrentAttack.Value = call.PreviousAttack;
        CurrentUse.Value = call.PreviousUse;
    }

    internal sealed class Request(EffectContext effect, AttackCommand? command, PlayerChoiceContext choice)
    {
        internal readonly EffectContext Effect = effect;
        internal readonly AttackCommand? Command = command;
        internal readonly PlayerChoiceContext Choice = choice;
        internal DrawAdmission? Admission;
    }

    internal sealed class Operation(Request? request, Request? pending, Operation? previous)
    {
        internal readonly Request? Request = request;
        internal readonly Request? PreviousPending = pending;
        internal readonly Operation? Previous = previous;
        internal int Closed;
        internal DrawTicket? Ticket;
    }

    private static void EnterDraw(PlayerChoiceContext choiceContext, Player player, out Operation? __state)
    {
        Request? request = Pending.Value;
        bool selected = request is { Command: null } && ReferenceEquals(request.Choice, choiceContext)
            && ReferenceEquals(request.Effect.Frame.Player, player) && ReferenceEquals(Current.Value, request.Effect.Frame);
        __state = selected || CurrentDraw.Value != null || CurrentUse.Value != null || request != null
            ? new Operation(selected ? request : null, request, CurrentDraw.Value) : null;
        Pending.Value = null;
        CurrentDraw.Value = __state;
        if (selected)
            request!.Effect.RequireIdentity();
    }

    private static void EnterAttack(AttackCommand __instance, PlayerChoiceContext? choiceContext, out Operation? __state)
    {
        Request? request = Pending.Value;
        bool selected = request?.Command != null && ReferenceEquals(request.Command, __instance)
            && ReferenceEquals(request.Choice, choiceContext) && ReferenceEquals(Current.Value, request.Effect.Frame);
        __state = selected || CurrentAttack.Value != null || CurrentUse.Value != null || request != null
            ? new Operation(selected ? request : null, request, CurrentAttack.Value) : null;
        Pending.Value = null;
        CurrentAttack.Value = __state;
        if (selected)
            request!.Effect.RequireIdentity();
    }

    private static void CompleteDraw(ref Task<IEnumerable<CardModel>> __result, Operation? __state)
    {
        if (__state != null)
            __result = TrackDraw(__result, __state);
    }

    private static async Task<IEnumerable<CardModel>> TrackDraw(Task<IEnumerable<CardModel>> original, Operation operation)
    {
        try { return await original; }
        finally
        {
            operation.Ticket?.Dispose();
            Volatile.Write(ref operation.Closed, 1);
        }
    }

    private static void CompleteAttack(ref Task<AttackCommand> __result, Operation? __state)
    {
        if (__state != null)
            __result = TrackAttack(__result, __state);
    }

    private static async Task<AttackCommand> TrackAttack(Task<AttackCommand> original, Operation operation)
    {
        try { return await original; }
        finally { Volatile.Write(ref operation.Closed, 1); }
    }

    private static Exception? RestoreDraw(Exception? __exception, Operation? __state)
    {
        if (__state != null)
        {
            if (__exception != null)
                Volatile.Write(ref __state.Closed, 1);
            Pending.Value = __state.PreviousPending;
            CurrentDraw.Value = __state.Previous;
        }
        return __exception;
    }

    private static Exception? RestoreAttack(Exception? __exception, Operation? __state)
    {
        if (__state != null)
        {
            if (__exception != null)
                Volatile.Write(ref __state.Closed, 1);
            Pending.Value = __state.PreviousPending;
            CurrentAttack.Value = __state.Previous;
        }
        return __exception;
    }

    private static EffectContext? Selected(Operation? operation)
    {
        if (operation?.Request == null)
            return null;
        if (Volatile.Read(ref operation.Closed) != 0)
            throw new InvalidOperationException("FTL native operation resumed after it completed.");
        operation.Request.Effect.RequireIdentity();
        return operation.Request.Effect;
    }

    private static void CheckDrawContinuation(Player player)
    {
        EffectContext? effect = Selected(CurrentDraw.Value);
        if (effect == null)
            return;
        if (!ReferenceEquals(effect.Frame.Player, player))
            throw new InvalidOperationException("FTL draw resumed for another player.");
    }

    private static bool ShouldStopDrawLoop(Player player)
    {
        EffectContext? effect = Selected(CurrentDraw.Value);
        if (effect == null)
            return false;
        if (!ReferenceEquals(effect.Frame.Player, player))
            throw new InvalidOperationException("FTL draw resumed for another player.");
        return !effect.Frame.CanResolve;
    }

    private static bool ShouldStopAttackLoop(AttackCommand command)
    {
        EffectContext? effect = Selected(CurrentAttack.Value);
        if (effect == null)
            return false;
        if (!ReferenceEquals(CurrentAttack.Value!.Request!.Command, command))
            throw new InvalidOperationException("FTL attack resumed with another command.");
        return !effect.Frame.CanResolve;
    }

    private static void CheckAttackIdentity(AttackCommand command)
    {
        _ = ShouldStopAttackLoop(command); // The original post-damage history/AfterAttack cleanup is retained.
    }

    private static IEnumerable<CodeInstruction> GuardDrawContinuations(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Ftl, __originalMethod, instructions.ToList());

    private static IEnumerable<CodeInstruction> GuardAttackContinuations(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Ftl, __originalMethod, instructions.ToList());

    private static IEnumerable<CodeInstruction> BridgeIronClubDraw(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Ftl, __originalMethod, instructions.ToList());

    private static IEnumerable<CodeInstruction> BridgeUseBoundary(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Ftl, __originalMethod, instructions.ToList());
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class Spire1FtlWrapperPatch
{
    [HarmonyPrepare]
    private static void Prepare() => Spire1FtlPlayScope.EnsureInstalled();

    [HarmonyPrefix, HarmonyPriority(Priority.Last)]
    private static void Enter(CardModel __instance, out Spire1FtlPlayScope.WrapperCall? __state)
    {
        __state = Spire1FtlPlayScope.WrapperCall.Capture(__instance);
        if (__state != null)
            Spire1FtlPlayScope.EnterWrapperInto(__instance, __state);
    }

    [HarmonyPostfix, HarmonyPriority(Priority.First)]
    private static void Complete(ref Task __result, Spire1FtlPlayScope.WrapperCall? __state)
    {
        if (__state?.Frame != null)
            __result = Spire1FtlPlayScope.CompleteWrapper(__result, __state);
    }

    [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
    private static Exception? Restore(Exception? __exception, Spire1FtlPlayScope.WrapperCall? __state)
    {
        if (__state != null)
            Spire1FtlPlayScope.RestoreWrapper(__state, __exception);
        return __exception;
    }
}
