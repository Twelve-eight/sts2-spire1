using System.Collections;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// One Fiend Fire / Sever Soul play, not an action queue. Only native Feel No Pain / Dark Embrace
/// payments are captured, at their actual place in the exhaust hook. Every other listener still
/// runs inside the real CardCmd.Exhaust before the next card is selected. The caller then executes
/// the pre-existing DamageActions and drains the captured bottom-of-queue payments once.
/// </summary>
internal sealed class Spire1ExhaustTransaction : IDisposable
{
    internal enum Phase { Exhausting, Attacking, PrimaryComplete, Paying, Completed, Canceled, Faulted }
    private enum RewardKind { Block, Draw }
    private readonly record struct Reward(RewardKind Kind, PowerModel Source, Creature Owner, int Amount);

    private readonly CardModel _source;
    private readonly CardPlay _play;
    private readonly Player _owner;
    private readonly Creature _creature;
    private readonly PlayerCombatState _piles;
    private readonly IRunState _run;
    private readonly RunRngSet _rngs;
    private readonly Rng _selection;
    private readonly ICombatState _combat;
    private readonly CombatTurnState _turnState;
    private readonly CancellationToken _cancellation;
    private readonly int _turnNumber;
    private readonly CombatId _combatId;
    private readonly PlayerChoiceContext _context;
    private readonly Spire1DamageSnapshot _damage;
    private List<Reward>? _rewards;
    private ExhaustEvent? _exhausting;
    private int _nextReward;
    private bool _disposed;

    // The synchronous ticket belongs to one actual CardCmd.Exhaust invocation. The native
    // async method captures its event, then its postfix restores the caller immediately.
    private static readonly AsyncLocal<ExhaustEvent?> CurrentExhaust = new();
    private static readonly AsyncLocal<ExhaustDispatch?> CurrentDispatch = new();
    [ThreadStatic] private static ExhaustEvent? _pendingExhaust;
    [ThreadStatic] private static PowerModel? _pendingPower;
    [ThreadStatic] private static Spire1ExhaustTransaction? _pendingReward;
    private static readonly AsyncLocal<Spire1ExhaustTransaction?> CurrentReward = new();
    private static readonly AsyncLocal<NativeOperation?> CurrentNative = new();
    [ThreadStatic] private static NativeOperation? _pendingNative;
    [ThreadStatic] private static MethodBase? _pendingNativeTarget;
    private static readonly Func<CombatManager, CombatId?, CombatTurnState?> StateFor = BindStateFor();

    internal Phase State { get; private set; } = Phase.Exhausting;
    internal CardPile Hand => _piles.Hand;
    internal Creature Target { get; }

    internal Spire1ExhaustTransaction(CardModel source, CardPlay play, PlayerChoiceContext context)
    {
        if (!ReferenceEquals(play.Card, source) || !ReferenceEquals(play.Player, source.Owner))
            throw new InvalidOperationException("Exhaust transaction requires the source's actual CardPlay.");
        Spire1ExhaustPatches.RequireInstalled();
        _source = source;
        _play = play;
        _owner = play.Player;
        _creature = _owner.Creature;
        _context = context;
        _piles = _owner.PlayerCombatState ?? throw new InvalidOperationException("Missing combat piles.");
        _combat = _owner.Creature.CombatState ?? throw new InvalidOperationException("Missing combat state.");
        _run = _owner.RunState;
        _rngs = _run.Rng;
        _selection = _rngs.CombatCardSelection;
        _combatId = CombatManager.Instance.CurrentCombatId ?? throw new InvalidOperationException("Missing combat id.");
        _turnState = StateFor(CombatManager.Instance, _combatId) ?? throw new InvalidOperationException("Missing owned live turn state.");
        _cancellation = _turnState.Ct;
        _turnNumber = _piles.TurnNumber;
        Target = play.Target ?? throw new InvalidOperationException("Missing attack target.");
        ValidateIdentity();
        if (!CanExhaust() || !ReferenceEquals(Target.CombatState, _combat) || !_combat.ContainsCreature(Target))
            throw new InvalidOperationException("Exhaust transaction requires its live combat and target.");

        _damage = Spire1DamageSnapshot.CaptureLegacyUse(source, play);
        ValidateIdentity();
    }

    internal bool CanExhaust()
    {
        ValidateIdentity();
        return !CombatManager.Instance.IsOverOrEnding && !_owner.Creature.IsDead;
    }

    internal CardModel? PickFromLiveHand()
    {
        RequirePhase(Phase.Exhausting);
        if (!CanExhaust()) return null;
        var cards = Hand.Cards;
        // ExhaustAction's empty-hand branch spends no RNG; even a singleton consumes one draw.
        return cards.Count == 0 ? null : cards[_selection.NextInt(cards.Count)];
    }

    internal async Task Exhaust(CardModel card)
    {
        RequirePhase(Phase.Exhausting);
        ValidateIdentity();
        if (_exhausting != null) throw new InvalidOperationException("An exhaust is already in progress.");
        if (!ReferenceEquals(card.Owner, _owner)) throw new InvalidOperationException("Foreign exhausted card owner.");
        // ExhaustSpecificCardAction keeps its originating group; never pull a departed card back.
        if (!Hand.Cards.Contains(card) || !CanExhaust()) return;
        var previous = _pendingExhaust;
        var exhausted = new ExhaustEvent(this, card, _context, _rewards?.Count ?? 0);
        int modelDepth = _context.ModelStack?.Count() ?? 0;
        _exhausting = exhausted;
        _pendingExhaust = exhausted;
        try
        {
            // Keep the host's movement, history, visuals, hook order and result semantics intact.
            // A listener may return this card to hand; final-pile checks would wrongly forbid that.
            Task<CardPileAddResult?> task;
            try { task = CardCmd.Exhaust(_context, card); }
            finally { _pendingExhaust = previous; }
            await task;
            ValidateIdentity();
        }
        catch
        {
            Fault();
            throw;
        }
        finally
        {
            exhausted.Active = false;
            _pendingExhaust = previous;
            _exhausting = null;
            RestoreModels(modelDepth);
        }
    }

    internal async Task Attack(int count, Func<Task>? beforeDamage = null)
    {
        RequirePhase(Phase.Exhausting);
        if (_exhausting != null) throw new InvalidOperationException("Exhaust listeners have not completed.");
        State = Phase.Attacking;
        int modelDepth = _context.ModelStack?.Count() ?? 0;
        try
        {
            // FiendFireAction queues N distinct DamageActions, not one N-hit attack. Keep an
            // independent BeforeAttack/AfterAttack boundary per action, fixed target and no RNG.
            for (int i = 0; i < count; i++)
            {
                ValidateIdentity();
                if (CombatManager.Instance.IsOverOrEnding || _owner.Creature.IsDead || !Target.IsAlive
                    || !_combat.ContainsCreature(Target)) break;
                AttackCommand attack = _damage.CreateExhaustAttack(this);
                if (beforeDamage != null) attack.BeforeDamage(beforeDamage);
                await attack.Execute(_context);
                ValidateIdentity();
            }
            State = Phase.PrimaryComplete;
        }
        catch
        {
            Fault();
            throw;
        }
        finally { RestoreModels(modelDepth); }
    }

    internal async Task Complete()
    {
        RequirePhase(Phase.PrimaryComplete);
        State = Phase.Paying;
        try
        {
            // Legacy UseCardAction removes the initial Vigor once, after queued primary damage,
            // before the bottom rewards. Late-only Vigor has no initial remove intent.
            ValidateIdentity();
            if (_damage.HadVigor && _creature.IsAlive && _creature.GetPower<VigorPower>() is { } vigor)
            {
                await PowerCmd.Remove(vigor);
                ValidateIdentity();
            }
            while (_rewards != null && _nextReward < _rewards.Count)
            {
                ValidateIdentity();
                Reward reward = _rewards[_nextReward++]; // claim before await; never replay a partial command
                int modelDepth = _context.ModelStack?.Count() ?? 0;
                _context.PushModel(reward.Source);
                try
                {
                    // Owner/amount were captured at the original power callback, not looked up now.
                    // Do not require Source still to be attached: the legacy action survives removal.
                    // The real host commands retain their own ending guards; no postcombat whitelist.
                    var previous = _pendingReward;
                    _pendingReward = this;
                    Task task;
                    try
                    {
                        task = reward.Kind == RewardKind.Block
                            ? CreatureCmd.GainBlock(reward.Owner, reward.Amount, ValueProp.Unpowered, null)
                            : CardPileCmd.Draw(_context, reward.Amount, _owner);
                    }
                    finally { _pendingReward = previous; }
                    await task;
                    ValidateIdentity();
                }
                finally { RestoreModels(modelDepth); }
            }
            State = Phase.Completed;
            _rewards?.Clear();
        }
        catch
        {
            Fault();
            throw;
        }
    }

    private void RequirePhase(Phase expected)
    {
        ValidateIdentity();
        if (State != expected) throw new InvalidOperationException($"Exhaust transaction is {State}, not {expected}.");
    }

    private bool HasIdentity()
        => !_disposed && State is not (Phase.Canceled or Phase.Faulted or Phase.Completed)
            && Equals(CombatManager.Instance.CurrentCombatId, _combatId)
            && ReferenceEquals(_source.Owner, _owner) && ReferenceEquals(_play.Card, _source)
            && ReferenceEquals(_play.Player, _owner) && ReferenceEquals(_owner.PlayerCombatState, _piles)
            && !_cancellation.IsCancellationRequested && _piles.TurnNumber == _turnNumber
            && ReferenceEquals(_turnState.State, _combat)
            && ReferenceEquals(_owner.Creature, _creature)
            && ReferenceEquals(_creature.CombatState, _combat) && ReferenceEquals(_owner.RunState, _run)
            && ReferenceEquals(_combat.RunState, _run) && ReferenceEquals(_run.Rng, _rngs)
            && ReferenceEquals(_rngs.CombatCardSelection, _selection)
            && (CombatManager.Instance.IsOverOrEnding || ReferenceEquals(StateFor(CombatManager.Instance, _combatId)?.State, _combat));

    private void ValidateIdentity()
    {
        if (HasIdentity()) return;
        Cancel();
        throw new OperationCanceledException("Exhaust transaction's combat/owner/play domain expired.");
    }

    private static Func<CombatManager, CombatId?, CombatTurnState?> BindStateFor()
    {
        var method = typeof(CombatManager).GetMethod("LiveTurnStateFor", BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(CombatId?)], null) ?? throw new MissingMethodException(typeof(CombatManager).FullName, "LiveTurnStateFor");
        if (method.ReturnType != typeof(CombatTurnState) || method.IsStatic)
            throw new InvalidOperationException("Combat lease lookup signature changed.");
        return method.CreateDelegate<Func<CombatManager, CombatId?, CombatTurnState?>>();
    }

    internal static CombatTurnState CaptureTurn()
        => StateFor(CombatManager.Instance, CombatManager.Instance.CurrentCombatId)
            ?? throw new InvalidOperationException("Missing live combat turn state.");

    private void RestoreModels(int depth)
    {
        // Hook.AfterCardExhausted currently does not pop its model when a listener throws.
        // Restore only pushes made inside our command, never another caller's earlier stack.
        while ((_context.ModelStack?.Count() ?? 0) > depth && _context.LastInvolvedModel is { } model)
            _context.PopModel(model);
    }

    private void Cancel()
    {
        if (State is not (Phase.Faulted or Phase.Completed)) State = Phase.Canceled;
        _rewards?.Clear();
    }

    private void Fault()
    {
        if (State != Phase.Canceled) State = Phase.Faulted;
        _rewards?.Clear();
    }

    public void Dispose()
    {
        Cancel();
        _disposed = true;
    }

    internal readonly struct CommandScope
    {
        private readonly ExhaustEvent? _previous;
        private readonly NativeOperation? _nativePrevious;
        internal CommandScope(PlayerChoiceContext context, CardModel card)
        {
            _previous = CurrentExhaust.Value;
            var pending = _pendingExhaust;
            _pendingExhaust = null;
            CurrentExhaust.Value = pending is { Active: true } && ReferenceEquals(pending.Context, context)
                && ReferenceEquals(pending.Card, card) ? pending : null;
            _nativePrevious = CurrentNative.Value;
            CurrentNative.Value = CurrentExhaust.Value is { } captured ? new NativeOperation(captured.Frame) : NativeOperation.Mask;
        }
        internal void Restore()
        {
            CurrentExhaust.Value = _previous;
            CurrentNative.Value = _nativePrevious;
        }
    }

    private sealed class NativeOperation(Spire1ExhaustTransaction? frame)
    {
        internal static readonly NativeOperation Mask = new(null);
        internal readonly Spire1ExhaustTransaction? Frame = frame;
        internal CardModel? Card;
        internal CardModel[]? Cards;
        internal CardPile? Pile;
        internal Creature[]? Creatures;
        internal Creature? Creature;
        internal void Validate()
        {
            if (Frame == null) return;
            Frame.ValidateIdentity();
            if (Card != null) Frame.ValidateNativeCard(Card);
            if (Cards != null) foreach (var card in Cards) Frame.ValidateNativeCard(card);
            if (Pile != null && !Frame.OwnsPile(Pile))
                throw new OperationCanceledException("Captured native destination pile changed.");
            if (Creature != null && !ReferenceEquals(Creature.CombatState, Frame._combat)
                && !(Creature.IsDead && Creature.CombatState == null))
                throw new OperationCanceledException("Captured native target left combat.");
            if (Creatures != null)
                foreach (var creature in Creatures)
                    if (!ReferenceEquals(creature.CombatState, Frame._combat)
                        && !(creature.IsDead && creature.CombatState == null))
                        throw new OperationCanceledException("Captured native death target left combat.");
        }
    }

    private static Spire1ExhaustTransaction? CausalFrame()
        => CurrentNative.Value is { } operation ? operation.Frame : CurrentReward.Value ?? CurrentExhaust.Value?.Frame;

    // A fresh native kickoff is explicitly masked by its prefix. A bridge reached from an
    // already-selected engine body must reject an expired frame, never downgrade to native.
    private static Spire1ExhaustTransaction? OpenCausalFrame()
    {
        var frame = CausalFrame();
        frame?.ValidateIdentity();
        return frame;
    }

    private readonly struct NativeCall : IDisposable
    {
        private readonly NativeOperation? _previous;
        private readonly MethodBase? _previousTarget;
        internal NativeCall(MethodBase target, NativeOperation? operation)
        {
            _previous = _pendingNative; _previousTarget = _pendingNativeTarget;
            _pendingNative = operation; _pendingNativeTarget = target;
        }
        public void Dispose()
        { _pendingNative = _previous; _pendingNativeTarget = _previousTarget; }
    }

    internal readonly struct NativeScope
    {
        private readonly NativeOperation? _previous;
        private NativeScope(NativeOperation? selected)
        {
            _previous = CurrentNative.Value;
            CurrentNative.Value = selected ?? NativeOperation.Mask;
        }
        internal void Restore() => CurrentNative.Value = _previous;
        internal static NativeScope? Enter(MethodBase method)
        {
            var selected = _pendingNativeTarget == method ? _pendingNative : null;
            if (method == Spire1ExhaustPatches.DamageMethod && Spire1DamageSnapshot.PendingExhaust is { } damage)
                selected = new NativeOperation(damage);
            _pendingNative = null; _pendingNativeTarget = null;
            selected?.Validate();
            return selected != null || CurrentNative.Value != null || CausalFrame() != null ? new NativeScope(selected) : null;
        }
    }

    internal static NativeScope? EnterNative(MethodBase method) => NativeScope.Enter(method);
    internal static void GuardNative() => CurrentNative.Value?.Validate();

    private bool OwnsPile(CardPile pile)
        => ReferenceEquals(pile, _piles.Hand) || ReferenceEquals(pile, _piles.DrawPile)
            || ReferenceEquals(pile, _piles.DiscardPile) || ReferenceEquals(pile, _piles.ExhaustPile)
            || ReferenceEquals(pile, _piles.PlayPile);

    private void ValidateNativeCard(CardModel card)
    {
        ValidateIdentity();
        if (!ReferenceEquals(card.Owner, _owner) || !ReferenceEquals(card.CombatState, _combat))
            throw new OperationCanceledException("Captured native card changed owner/combat.");
    }

    private static NativeOperation? SelectCard(CardModel card, CardPile pile)
    {
        var frame = OpenCausalFrame();
        if (frame == null) return null;
        frame.ValidateNativeCard(card);
        if (!frame.OwnsPile(pile)) throw new InvalidOperationException("Unexpected selected native card destination.");
        return new NativeOperation(frame) { Card = card, Pile = pile };
    }

    internal static Task<CardPileAddResult> ExhaustAdd(CardModel card, PileType pile, CardPilePosition position, AbstractModel? clonedBy, bool skipVisuals)
    {
        NativeOperation? operation = null;
        if (OpenCausalFrame() is { } frame)
        {
            if (pile != PileType.Exhaust || CurrentExhaust.Value is not { Active: true } exhausted
                || !ReferenceEquals(exhausted.Card, card) || !ReferenceEquals(exhausted.Frame, frame))
                throw new InvalidOperationException("Selected exhaust movement call changed.");
            operation = SelectCard(card, frame._piles.ExhaustPile);
        }
        using var call = new NativeCall(Spire1ExhaustPatches.AddCommand, operation);
        return CardPileCmd.Add(card, pile, position, clonedBy, skipVisuals);
    }

    // FTL's one final Draw.Add bridge calls this exact native layer in both selected modes.
    internal static Task<CardPileAddResult> DrawAdd(CardModel card, CardPile pile, CardPilePosition position, AbstractModel? clonedBy, bool skipVisuals)
    {
        var operation = SelectCard(card, pile);
        using var call = new NativeCall(Spire1ExhaustPatches.AddCommand, operation);
        return CardPileCmd.Add(card, pile, position, clonedBy, skipVisuals);
    }

    private static NativeOperation? SelectPlayer(PlayerChoiceContext context, Player player)
    {
        var frame = OpenCausalFrame();
        if (frame == null) return null;
        if (!ReferenceEquals(context, frame._context) || !ReferenceEquals(player, frame._owner))
            throw new InvalidOperationException("Selected native shuffle domain changed.");
        return new NativeOperation(frame);
    }

    internal static Task DrawShuffle(PlayerChoiceContext context, Player player)
    {
        using var call = new NativeCall(Spire1ExhaustPatches.ShuffleIfNecessaryCommand, SelectPlayer(context, player));
        return Spire1ExhaustPatches.ShuffleIfNecessary(context, player);
    }

    internal static Task ShuffleNow(PlayerChoiceContext context, Player player)
    {
        using var call = new NativeCall(Spire1ExhaustPatches.ShuffleCommand, SelectPlayer(context, player));
        return CardPileCmd.Shuffle(context, player);
    }

    internal static Task<IReadOnlyList<CardPileAddResult>> ShuffleAdd(IEnumerable<CardModel> cards, CardPile pile,
        CardPilePosition position, AbstractModel? clonedBy, bool skipVisuals, bool skipHandLayout)
    {
        NativeOperation? operation = null;
        if (OpenCausalFrame() is { } frame)
        {
            var snapshot = cards.ToArray();
            operation = new NativeOperation(frame) { Cards = snapshot, Pile = pile };
            operation.Validate(); cards = snapshot;
        }
        using var call = new NativeCall(Spire1ExhaustPatches.AddCommand, operation);
        return CardPileCmd.Add(cards, pile, position, clonedBy, skipVisuals, skipHandLayout);
    }

    private static NativeOperation? SelectDeath(IReadOnlyCollection<Creature> creatures, CardModel? source)
    {
        var frame = OpenCausalFrame();
        if (frame == null) return null;
        if (source != null && !ReferenceEquals(source, frame._source))
            throw new InvalidOperationException("Selected death source changed.");
        var snapshot = creatures.ToArray();
        foreach (var creature in snapshot)
            if (!ReferenceEquals(creature.CombatState, frame._combat)
                && !(creature.IsDead && CurrentNative.Value?.Creatures?.Contains(creature) == true))
                throw new OperationCanceledException("Selected native death target is foreign.");
        return new NativeOperation(frame) { Creatures = snapshot };
    }

    internal static Task DamageKill(IReadOnlyCollection<Creature> creatures, CardModel? source, bool force)
    {
        var operation = SelectDeath(creatures, source);
        using var call = new NativeCall(Spire1ExhaustPatches.KillCommand, operation);
        return CreatureCmd.Kill(operation?.Creatures ?? creatures, source, force);
    }

    internal static Task KillOne(Creature creature, CardModel? source, bool force, int recursion)
    {
        NativeOperation? operation = null;
        if (OpenCausalFrame() is { } frame)
        {
            if (source != null && !ReferenceEquals(source, frame._source))
                throw new InvalidOperationException("Selected death source changed.");
            if (!ReferenceEquals(creature.CombatState, frame._combat)
                && !(creature.IsDead && (ReferenceEquals(CurrentNative.Value?.Creature, creature)
                    || CurrentNative.Value?.Creatures?.Contains(creature) == true)))
                throw new OperationCanceledException("Selected native death target is foreign.");
            operation = new NativeOperation(frame) { Creature = creature };
        }
        using var call = new NativeCall(Spire1ExhaustPatches.KillOneCommand, operation);
        return Spire1ExhaustPatches.KillWithoutCheckingWinCondition(creature, source, force, recursion);
    }

    internal static Task KillCreature(Creature creature, bool force)
    {
        if (OpenCausalFrame() == null) return CreatureCmd.Kill(creature, force);
        return DamageKill([creature], null, force);
    }

    private static NativeOperation? SelectHook(PlayerChoiceContext context, IRunState? run, ICombatState? combat, Creature? dealer, CardModel? source, Creature target)
    {
        var frame = OpenCausalFrame();
        if (frame == null) return null;
        if (!ReferenceEquals(context, frame._context) || !ReferenceEquals(combat, frame._combat)
            || (run != null && !ReferenceEquals(run, frame._run))
            || !ReferenceEquals(dealer, frame._creature) || !ReferenceEquals(source, frame._source))
            throw new InvalidOperationException("Selected damage dispatcher domain changed.");
        return new NativeOperation(frame) { Creature = target };
    }

    internal static Task BeforeDamageReceived(PlayerChoiceContext context, IRunState run, ICombatState? combat, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? source)
    {
        using var call = new NativeCall(Spire1ExhaustPatches.BeforeDamageCommand, SelectHook(context, run, combat, dealer, source, target));
        return Hook.BeforeDamageReceived(context, run, combat, target, amount, props, dealer, source);
    }

    internal static Task AfterDamageGiven(PlayerChoiceContext context, ICombatState combat, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? source)
    {
        using var call = new NativeCall(Spire1ExhaustPatches.AfterGivenCommand, SelectHook(context, null, combat, dealer, source, target));
        return Hook.AfterDamageGiven(context, combat, dealer, result, props, target, source);
    }

    internal static Task AfterDamageReceived(PlayerChoiceContext context, IRunState run, ICombatState? combat, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? source)
    {
        using var call = new NativeCall(Spire1ExhaustPatches.AfterReceivedCommand, SelectHook(context, run, combat, dealer, source, target));
        return Hook.AfterDamageReceived(context, run, combat, target, result, props, dealer, source);
    }

    internal static Task AfterCardDrawn(ICombatState combat, PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        var frame = OpenCausalFrame();
        NativeOperation? operation = null;
        if (frame != null)
        {
            if (!ReferenceEquals(context, frame._context) || !ReferenceEquals(combat, frame._combat))
                throw new InvalidOperationException("Selected draw dispatcher domain changed.");
            frame.ValidateNativeCard(card);
            operation = new NativeOperation(frame) { Card = card };
        }
        using var call = new NativeCall(Spire1ExhaustPatches.DrawnCommand, operation);
        return Hook.AfterCardDrawn(combat, context, card, fromHandDraw);
    }


    internal readonly struct RewardScope
    {
        private readonly Spire1ExhaustTransaction? _previous;
        private readonly NativeOperation? _nativePrevious;
        public RewardScope()
        {
            _previous = CurrentReward.Value;
            CurrentReward.Value = _pendingReward;
            _pendingReward = null;
            _nativePrevious = CurrentNative.Value;
            CurrentNative.Value = CurrentReward.Value is { } captured ? new NativeOperation(captured) : NativeOperation.Mask;
        }
        internal void Restore()
        {
            CurrentReward.Value = _previous;
            CurrentNative.Value = _nativePrevious;
        }
    }

    internal static RewardScope? EnterRewardCommand()
        => _pendingReward != null || CurrentReward.Value != null || CurrentNative.Value != null
            || CausalFrame() != null ? new RewardScope() : null;

    internal static void GuardReward()
    {
        if (CurrentReward.Value is { } frame) frame.RequirePhase(Phase.Paying);
    }

    internal static Task DispatchExhaust(ICombatState combat, PlayerChoiceContext context, CardModel card, bool ethereal)
    {
        var exhausted = CurrentExhaust.Value;
        if (exhausted == null || !exhausted.Active) return Hook.AfterCardExhausted(combat, context, card, ethereal);
        exhausted.Frame.ValidateIdentity();
        exhausted.AtHostDispatch = true;
        try { return Hook.AfterCardExhausted(combat, context, card, ethereal); }
        finally { exhausted.AtHostDispatch = false; }
    }

    internal static CommandScope? EnterCommand(PlayerChoiceContext context, CardModel card)
        => _pendingExhaust != null || CurrentExhaust.Value != null || CurrentNative.Value != null
            || CausalFrame() != null ? new CommandScope(context, card) : null;

    internal static void GuardExhaust()
    {
        if (CurrentExhaust.Value is { } exhausted) exhausted.Frame.RequirePhase(Phase.Exhausting);
    }

    internal static Task InvokeExhaustListener(AbstractModel model, PlayerChoiceContext context, CardModel card, bool ethereal)
    {
        ExhaustDispatch.Guard();
        var previous = _pendingPower;
        var exhausted = CurrentExhaust.Value;
        _pendingPower = exhausted != null && ExhaustDispatch.Matches(exhausted) ? model as PowerModel : null;
        try { return model.AfterCardExhausted(context, card, ethereal); }
        finally { _pendingPower = previous; }
    }

    internal sealed class ExhaustEvent(Spire1ExhaustTransaction frame, CardModel card, PlayerChoiceContext context, int firstReward)
    {
        internal readonly Spire1ExhaustTransaction Frame = frame;
        internal readonly CardModel Card = card;
        internal readonly PlayerChoiceContext Context = context;
        internal readonly int FirstReward = firstReward;
        internal bool Active = true;
        internal bool Entered;
        internal bool AtHostDispatch;
    }

    internal sealed class ExhaustDispatch
    {
        private readonly ExhaustEvent? _event;
        private readonly ExhaustDispatch? _previous;
        private readonly bool _captures;
        private bool _active = true;

        internal ExhaustDispatch(ICombatState combat, PlayerChoiceContext context, CardModel card, bool ethereal)
        {
            _previous = CurrentDispatch.Value;
            var exhausted = CurrentExhaust.Value;
            _event = exhausted;
            _captures = exhausted is { Active: true, AtHostDispatch: true, Entered: false } && !ethereal
                && ReferenceEquals(context, exhausted.Context) && ReferenceEquals(card, exhausted.Card)
                && ReferenceEquals(combat, exhausted.Frame._combat);
            if (_captures)
            {
                exhausted!.Frame.ValidateIdentity();
                exhausted.Entered = true;
                exhausted.AtHostDispatch = false; // consume before the first listener can recurse
            }
            CurrentDispatch.Value = this;
        }

        internal static ExhaustDispatch? Enter(ICombatState combat, PlayerChoiceContext context, CardModel card, bool ethereal)
            => CurrentExhaust.Value != null || CurrentDispatch.Value != null
                ? new ExhaustDispatch(combat, context, card, ethereal) : null;

        internal void Restore() => CurrentDispatch.Value = _previous;

        internal async Task Finish(Task task)
        {
            try { await task; }
            catch { if (_captures) _event!.Frame.Fault(); throw; }
            finally { _active = false; }
        }

        internal static void Guard()
        {
            if (CurrentDispatch.Value is { _active: true, _captures: true } dispatch)
                dispatch._event!.Frame.RequirePhase(Phase.Exhausting);
        }

        internal static bool Matches(ExhaustEvent exhausted)
            => CurrentDispatch.Value is { _active: true, _captures: true } dispatch
                && ReferenceEquals(dispatch._event, exhausted);
    }

    internal static bool Capture(PowerModel power, PlayerChoiceContext context, CardModel card, bool ethereal, bool draw)
    {
        var exhausted = CurrentExhaust.Value;
        bool authorized = ReferenceEquals(_pendingPower, power);
        _pendingPower = null; // one synchronous callback, never inherited by nested/direct native calls
        if (!authorized || ethereal || exhausted == null || !exhausted.Active || !ExhaustDispatch.Matches(exhausted)
            || !ReferenceEquals(exhausted.Context, context) || !ReferenceEquals(exhausted.Card, card)
            || !ReferenceEquals(card.Owner, exhausted.Frame._owner)
            || !ReferenceEquals(power.Owner, exhausted.Frame._owner.Creature)) return false;
        var frame = exhausted.Frame;
        frame.RequirePhase(Phase.Exhausting);
        frame._rewards ??= new List<Reward>();
        for (int i = exhausted.FirstReward; i < frame._rewards.Count; i++)
            if (ReferenceEquals(frame._rewards[i].Source, power))
                throw new InvalidOperationException("Duplicate native power callback for one exhaust event.");
        frame._rewards.Add(new Reward(draw ? RewardKind.Draw : RewardKind.Block, power, power.Owner, power.Amount));
        return true;
    }

    internal void ValidateSnapshotAttack(PlayerChoiceContext? context = null)
    {
        RequirePhase(Phase.Attacking);
        if (context != null && !ReferenceEquals(context, _context))
            throw new InvalidOperationException("Captured attack crossed its choice context.");
    }

    internal static Task<IEnumerable<DamageResult>> DamageFromAttack(PlayerChoiceContext context,
        IEnumerable<DamageTargetData>? targets, decimal amount, ValueProp props, Creature? dealer,
        CardModel? source, CardPlay? play, ModifyDamageHookType hooks, AttackCommand command)
        => Spire1DamageSnapshot.DamageFromAttack(context, targets, amount, props, dealer, source, play, hooks, command);

    internal static Task<IEnumerable<DamageResult>> DamageFromAttackWithRitsu(PlayerChoiceContext context,
        IEnumerable<DamageTargetData>? targets, decimal amount, ValueProp props, Creature? dealer,
        CardModel? source, CardPlay? play, ModifyDamageHookType hooks, AttackCommand command,
        int hitIndex, decimal totalHitCount)
        => Spire1DamageSnapshot.DamageFromAttackWithRitsu(context, targets, amount, props, dealer, source, play, hooks,
            command, hitIndex, totalHitCount);

    internal static void RegisterRitsuAttackDamage(MethodInfo method)
        => Spire1DamageSnapshot.RegisterRitsuAttackDamage(method);

    internal static object? BeginDamage(IRunState run, ICombatState? combat, Creature? target, Creature? dealer,
        decimal amount, ValueProp props, CardModel? source, CardPlay? play, ModifyDamageHookType hooks, CardPreviewMode preview)
        => Spire1DamageSnapshot.BeginDamage(run, combat, target, dealer, amount, props, source, play, hooks, preview);

    internal static void EndDamage(object? state, ref IEnumerable<AbstractModel> modifiers)
        => Spire1DamageSnapshot.EndDamage(state, ref modifiers);

    internal static void GuardAttack(AttackCommand command) => Spire1DamageSnapshot.GuardAttack(command);
    internal static void GuardDamage(IEnumerable<DamageTargetData>? targets) => Spire1DamageSnapshot.GuardDamage(targets);
}

/// <summary>Immutable legacy-use damage, shared only by the three named production callers.</summary>
internal sealed class Spire1DamageSnapshot
{
    private readonly CardModel _card;
    private readonly CardPlay _play;
    private readonly Player _owner;
    private readonly Creature _dealer;
    private readonly Creature _target;
    private readonly PlayerCombatState _piles;
    private readonly IRunState _run;
    private readonly RunRngSet _rngs;
    private readonly ICombatState _combat;
    private readonly CombatTurnState _state;
    private readonly CancellationToken _ct;
    private readonly int _turn;
    private readonly decimal _amount;
    private readonly AbstractModel[] _modifiers;
    internal bool HadVigor { get; }
    [ThreadStatic] private static Ticket? _pending;
    internal static Spire1ExhaustTransaction? PendingExhaust => _pending?.Attack.Exhaust;

    private Spire1DamageSnapshot(CardModel card, CardPlay play)
    {
        _card = card; _play = play; _owner = play.Player; _dealer = _owner.Creature;
        _target = play.Target ?? throw new InvalidOperationException("Snapshot requires an actual target.");
        _piles = _owner.PlayerCombatState ?? throw new InvalidOperationException("Snapshot requires combat piles.");
        _combat = _dealer.CombatState ?? throw new InvalidOperationException("Snapshot requires combat.");
        _run = _owner.RunState; _rngs = _run.Rng; _turn = _piles.TurnNumber;
        _state = Spire1ExhaustTransaction.CaptureTurn(); _ct = _state.Ct;
        ValidateIdentity(); ValidateTargetBeforeAttack();
        var vigor = _dealer.GetPower<VigorPower>();
        HadVigor = vigor != null;
        decimal vigorAmount = vigor?.Amount ?? 0;
        decimal amount = card.DynamicVars.Damage.BaseValue;
        if (card.Enchantment is { } enchantment)
        {
            amount += enchantment.EnchantDamageAdditive(amount, ValueProp.Move);
            amount *= enchantment.EnchantDamageMultiplicative(amount, ValueProp.Move);
        }
        List<AbstractModel>? modifiers = null;
        foreach (var listener in _run.IterateHookListeners(_combat))
        {
            decimal delta = listener is VigorPower power
                ? ReferenceEquals(power, vigor) ? vigorAmount : 0m
                : listener.ModifyDamageAdditive(_target, amount, ValueProp.Move, _dealer, card, play);
            amount += delta;
            if (delta != 0m) (modifiers ??= []).Add(listener);
        }
        foreach (var listener in _run.IterateHookListeners(_combat))
        {
            decimal factor = listener.ModifyDamageMultiplicative(_target, amount, ValueProp.Move, _dealer, card, play);
            amount *= factor;
            if (factor != 1m) (modifiers ??= []).Add(listener);
        }
        foreach (var listener in _run.IterateHookListeners(_combat))
        {
            decimal factor = listener.ModifyDamageMultiplicativeLate(_target, amount, ValueProp.Move, _dealer, card, play);
            amount *= factor;
            if (factor != 1m) (modifiers ??= []).Add(listener);
        }
        // Intangible is an S1 final-receive snapshot modifier; generic host caps remain live.
        if (_target.GetPower<IntangiblePower>() is { } intangible && amount > 1m)
        {
            amount = 1m; (modifiers ??= []).Add(intangible);
        }
        _amount = decimal.Floor(Math.Max(0m, amount));
        _modifiers = modifiers?.ToArray() ?? [];
        ValidateIdentity();
    }

    internal static Spire1DamageSnapshot CaptureLegacyUse(CardModel card, CardPlay play)
    {
        if (card.GetType() != typeof(Spire1.Spire1Code.Cards.FiendFire) && card.GetType() != typeof(Spire1.Spire1Code.Cards.SeverSoul)
            && card.GetType() != typeof(Spire1.Spire1Code.Cards.FTL)) throw new InvalidOperationException("Unadmitted snapshot producer.");
        if (!ReferenceEquals(play.Card, card) || !ReferenceEquals(play.Player, card.Owner))
            throw new InvalidOperationException("Snapshot requires the exact owned CardPlay.");
        Spire1ExhaustPatches.RequireInstalled();
        return new Spire1DamageSnapshot(card, play);
    }

    internal void ValidateIdentity()
    {
        _ct.ThrowIfCancellationRequested();
        if (!Equals(CombatManager.Instance.CurrentCombatId, _state.Id) || !ReferenceEquals(_state.State, _combat)
            || !ReferenceEquals(_play.Card, _card) || !ReferenceEquals(_play.Player, _owner)
            || !ReferenceEquals(_card.Owner, _owner) || !ReferenceEquals(_owner.Creature, _dealer)
            || !ReferenceEquals(_dealer.CombatState, _combat) || !ReferenceEquals(_owner.PlayerCombatState, _piles)
            || _piles.TurnNumber != _turn || !ReferenceEquals(_owner.RunState, _run)
            || !ReferenceEquals(_combat.RunState, _run) || !ReferenceEquals(_run.Rng, _rngs)
            || (!ReferenceEquals(_target.CombatState, _combat) && !(_target.IsDead && _target.CombatState == null)))
            throw new OperationCanceledException("Captured legacy damage domain expired.");
    }

    internal bool CanAttackTarget()
    {
        ValidateIdentity();
        return _target.IsAlive && _combat.ContainsCreature(_target);
    }

    internal void ValidateTargetBeforeAttack()
    {
        ValidateIdentity();
        if (!_target.IsAlive || !_combat.ContainsCreature(_target))
            throw new InvalidOperationException("Captured attack target is no longer hittable.");
    }

    internal AttackCommand CreateAttack() => Create(null);
    internal AttackCommand CreateExhaustAttack(Spire1ExhaustTransaction exhaust) => Create(exhaust);
    private AttackCommand Create(Spire1ExhaustTransaction? exhaust)
    {
        ValidateTargetBeforeAttack();
        return new CapturedAttack(this, exhaust).FromCard(_card, _play).Targeting(_target);
    }

    private sealed class CapturedAttack(Spire1DamageSnapshot snapshot, Spire1ExhaustTransaction? exhaust) : AttackCommand(snapshot._amount)
    {
        internal readonly Spire1DamageSnapshot Snapshot = snapshot;
        internal readonly Spire1ExhaustTransaction? Exhaust = exhaust;
        internal void Validate(PlayerChoiceContext? context = null)
        {
            Snapshot.ValidateIdentity(); Exhaust?.ValidateSnapshotAttack(context);
        }
    }

    private sealed class Ticket(CapturedAttack attack) : IEnumerable<AbstractModel>
    {
        internal readonly CapturedAttack Attack = attack;
        internal IEnumerable<AbstractModel> Live = [];
        public IEnumerator<AbstractModel> GetEnumerator()
        {
            foreach (var modifier in Attack.Snapshot._modifiers) yield return modifier;
            foreach (var modifier in Live) yield return modifier;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class Targets(IReadOnlyList<DamageTargetData> source, CapturedAttack attack) : IReadOnlyList<DamageTargetData>
    {
        internal readonly CapturedAttack Attack = attack;
        public int Count => source.Count;
        public DamageTargetData this[int index] => source[index];
        public IEnumerator<DamageTargetData> GetEnumerator() => source.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private delegate Task<IEnumerable<DamageResult>> RitsuDamageWithAttackHitHooksDelegate(
        PlayerChoiceContext choiceContext, IEnumerable<DamageTargetData>? targets, decimal amount,
        ValueProp props, Creature? dealer, CardModel? card, CardPlay? play,
        ModifyDamageHookType hooks, AttackCommand attack, int hitIndex, decimal totalHitCount);

    private static RitsuDamageWithAttackHitHooksDelegate? _ritsuAttackDamage;

    internal static void RegisterRitsuAttackDamage(MethodInfo method)
    {
        var created = method.CreateDelegate<RitsuDamageWithAttackHitHooksDelegate>();
        Volatile.Write(ref _ritsuAttackDamage, created);
    }

    internal static Task<IEnumerable<DamageResult>> DamageFromAttack(PlayerChoiceContext context,
        IEnumerable<DamageTargetData>? targets, decimal amount, ValueProp props, Creature? dealer,
        CardModel? card, CardPlay? play, ModifyDamageHookType hooks, AttackCommand command)
    {
        if (command is not CapturedAttack captured) return CreatureCmd.Damage(context, targets, amount, props, dealer, card, play, hooks);
        captured.Validate(context);
        var snapshot = captured.Snapshot;
        if (!ReferenceEquals(dealer, snapshot._dealer) || !ReferenceEquals(card, snapshot._card)
            || !ReferenceEquals(play, snapshot._play) || amount != snapshot._amount || props != ValueProp.Move
            || hooks != ModifyDamageHookType.All || targets is not IReadOnlyList<DamageTargetData> list
            || list.Count != 1 || !ReferenceEquals(list[0].target, snapshot._target))
            throw new InvalidOperationException("Snapshot damage consumer identity changed.");
        var previous = _pending;
        _pending = new Ticket(captured);
        try { return Finish(CreatureCmd.Damage(context, new Targets(list, captured), amount, props, dealer, card, play, ModifyDamageHookType.Cap), captured); }
        finally { _pending = previous; }
    }

    internal static Task<IEnumerable<DamageResult>> DamageFromAttackWithRitsu(PlayerChoiceContext context,
        IEnumerable<DamageTargetData>? targets, decimal amount, ValueProp props, Creature? dealer,
        CardModel? card, CardPlay? play, ModifyDamageHookType hooks, AttackCommand command,
        int hitIndex, decimal totalHitCount)
    {
        if (command is CapturedAttack captured)
            return DamageFromAttack(context, targets, amount, props, dealer, card, play, hooks, captured);
        RitsuDamageWithAttackHitHooksDelegate? ritsu = Volatile.Read(ref _ritsuAttackDamage);
        if (ritsu != null)
            return ritsu(context, targets, amount, props, dealer, card, play, hooks, command, hitIndex, totalHitCount);
        return CreatureCmd.Damage(context, targets, amount, props, dealer, card, play, hooks);
    }

    private static async Task<IEnumerable<DamageResult>> Finish(Task<IEnumerable<DamageResult>> task, CapturedAttack attack)
    {
        var result = await task; attack.Validate(); return result;
    }

    internal static object? BeginDamage(IRunState run, ICombatState? combat, Creature? target, Creature? dealer,
        decimal amount, ValueProp props, CardModel? card, CardPlay? play, ModifyDamageHookType hooks, CardPreviewMode preview)
    {
        var ticket = _pending; if (ticket == null) return null;
        _pending = null; // consume before executing ANY live cap listener or nested damage
        var snapshot = ticket.Attack.Snapshot; ticket.Attack.Validate();
        if (!ReferenceEquals(run, snapshot._run) || !ReferenceEquals(combat, snapshot._combat)
            || !ReferenceEquals(target, snapshot._target) || !ReferenceEquals(dealer, snapshot._dealer)
            || !ReferenceEquals(card, snapshot._card) || !ReferenceEquals(play, snapshot._play)
            || amount != snapshot._amount || props != ValueProp.Move || hooks != ModifyDamageHookType.Cap || preview != CardPreviewMode.None)
            throw new InvalidOperationException("Snapshot synchronous cap call changed.");
        return ticket;
    }

    internal static void EndDamage(object? state, ref IEnumerable<AbstractModel> modifiers)
    {
        if (state is not Ticket ticket) return;
        ticket.Attack.Validate();
        if (ticket.Attack.Snapshot._modifiers.Length == 0) return;
        ticket.Live = modifiers; modifiers = ticket;
    }

    internal static void GuardAttack(AttackCommand command)
    { if (command is CapturedAttack captured) captured.Validate(); }
    internal static void GuardDamage(IEnumerable<DamageTargetData>? targets)
    { if (targets is Targets captured) captured.Attack.Validate(); }
    internal static bool IsCaptured(AttackCommand command) => command is CapturedAttack;
}

internal static class Spire1ExhaustPatches
{
    private const BindingFlags Methods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static MethodInfo Exact(Type type, string name, Type result, params Type[] arguments)
    {
        var method = AccessTools.DeclaredMethod(type, name, arguments)
            ?? throw new MissingMethodException(type.FullName, name);
        if (!method.IsStatic || method.ReturnType != result)
            throw new InvalidOperationException("Exhaust native entry signature changed: " + method);
        return method;
    }
    private static MethodInfo Move(MethodInfo method) => Spire1ContinuationIl.ForKickoff(method).Move;
    internal static readonly MethodInfo AttackMove = Move(AccessTools.DeclaredMethod(typeof(AttackCommand), nameof(AttackCommand.Execute), [typeof(PlayerChoiceContext)])!);
    internal static readonly MethodInfo DamageMethod = Exact(typeof(CreatureCmd), nameof(CreatureCmd.Damage), typeof(Task<IEnumerable<DamageResult>>),
        typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp),
        typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType));
    internal static readonly MethodInfo DamageMove = Move(DamageMethod);
    internal static readonly MethodInfo ExhaustCommand = Exact(typeof(CardCmd), nameof(CardCmd.Exhaust), typeof(Task<CardPileAddResult?>),
        typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool));
    internal static readonly MethodInfo ExhaustMove = Move(ExhaustCommand);
    private static readonly MethodInfo ExhaustMethod = Exact(typeof(Hook), nameof(Hook.AfterCardExhausted), typeof(Task),
        typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool));
    internal static readonly MethodInfo DispatchMove = Move(ExhaustMethod);
    internal static readonly MethodInfo BlockCommand = Exact(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), typeof(Task<decimal>),
        typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool));
    internal static readonly MethodInfo DrawCommand = Exact(typeof(CardPileCmd), "DrawInternal", typeof(Task<IEnumerable<CardModel>>),
        typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool));
    internal static readonly MethodInfo BlockMove = Move(BlockCommand);
    internal static readonly MethodInfo DrawMove = Move(DrawCommand);
    internal static readonly MethodInfo AddCommand = Exact(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(Task<IReadOnlyList<CardPileAddResult>>),
        typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool));
    internal static readonly MethodInfo ShuffleIfNecessaryCommand = Exact(typeof(CardPileCmd), "ShuffleIfNecessary", typeof(Task), typeof(PlayerChoiceContext), typeof(Player));
    internal static readonly Func<PlayerChoiceContext, Player, Task> ShuffleIfNecessary = ShuffleIfNecessaryCommand.CreateDelegate<Func<PlayerChoiceContext, Player, Task>>();
    internal static readonly MethodInfo ShuffleCommand = Exact(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle), typeof(Task), typeof(PlayerChoiceContext), typeof(Player));
    internal static readonly MethodInfo KillCommand = Exact(typeof(CreatureCmd), nameof(CreatureCmd.Kill), typeof(Task), typeof(IReadOnlyCollection<Creature>), typeof(CardModel), typeof(bool));
    internal static readonly MethodInfo KillOneCommand = Exact(typeof(CreatureCmd), "KillWithoutCheckingWinCondition", typeof(Task), typeof(Creature), typeof(CardModel), typeof(bool), typeof(int));
    internal static readonly Func<Creature, CardModel?, bool, int, Task> KillWithoutCheckingWinCondition = KillOneCommand.CreateDelegate<Func<Creature, CardModel?, bool, int, Task>>();
    internal static readonly MethodInfo BeforeDamageCommand = Exact(typeof(Hook), nameof(Hook.BeforeDamageReceived), typeof(Task),
        typeof(PlayerChoiceContext), typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel));
    internal static readonly MethodInfo AfterGivenCommand = Exact(typeof(Hook), nameof(Hook.AfterDamageGiven), typeof(Task),
        typeof(PlayerChoiceContext), typeof(ICombatState), typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel));
    internal static readonly MethodInfo AfterReceivedCommand = Exact(typeof(Hook), nameof(Hook.AfterDamageReceived), typeof(Task),
        typeof(PlayerChoiceContext), typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel));
    internal static readonly MethodInfo DrawnCommand = Exact(typeof(Hook), nameof(Hook.AfterCardDrawn), typeof(Task),
        typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool));
    internal static readonly MethodInfo[] NativeCommands = [DamageMethod, AddCommand, ShuffleIfNecessaryCommand, ShuffleCommand,
        KillCommand, KillOneCommand, BeforeDamageCommand, AfterGivenCommand, AfterReceivedCommand, DrawnCommand];
    internal static readonly MethodInfo[] CausalMoves = [Move(AddCommand), Move(ShuffleCommand), Move(ShuffleIfNecessaryCommand),
        Move(KillCommand), Move(KillOneCommand), Move(BeforeDamageCommand), Move(AfterGivenCommand), Move(AfterReceivedCommand), Move(DrawnCommand)];
    internal static readonly MethodInfo[] VigorCommands = [AccessTools.DeclaredMethod(typeof(VigorPower), nameof(VigorPower.BeforeAttack), [typeof(AttackCommand)])!,
        AccessTools.DeclaredMethod(typeof(VigorPower), nameof(VigorPower.AfterAttack), [typeof(PlayerChoiceContext), typeof(AttackCommand)])!];
    private static readonly MethodInfo ModifyMethod = typeof(Hook).GetMethod(nameof(Hook.ModifyDamage))!;

    internal static void RequireInstalled()
    {
        Require(AttackMove, typeof(Spire1ExhaustAttackPatch), "Transpile", HarmonyPatchType.Transpiler);
        Require(DamageMove, typeof(Spire1ExhaustDamageAwaitPatch), "Transpile", HarmonyPatchType.Transpiler);
        Require(ExhaustMove, typeof(Spire1ExhaustCommandPatch), "Transpile", HarmonyPatchType.Transpiler);
        Require(ExhaustCommand, typeof(Spire1ExhaustCommandScopePatch), "Prefix", HarmonyPatchType.Prefix);
        Require(ExhaustCommand, typeof(Spire1ExhaustCommandScopePatch), "Postfix", HarmonyPatchType.Postfix);
        Require(ExhaustCommand, typeof(Spire1ExhaustCommandScopePatch), "Finalizer", HarmonyPatchType.Finalizer);
        Require(DispatchMove, typeof(Spire1ExhaustDispatchAwaitPatch), "Transpile", HarmonyPatchType.Transpiler);
        foreach (var target in new[] { BlockCommand, DrawCommand })
        {
            Require(target, typeof(Spire1ExhaustRewardScopePatch), "Prefix", HarmonyPatchType.Prefix);
            Require(target, typeof(Spire1ExhaustRewardScopePatch), "Postfix", HarmonyPatchType.Postfix);
            Require(target, typeof(Spire1ExhaustRewardScopePatch), "Finalizer", HarmonyPatchType.Finalizer);
        }
        Require(BlockMove, typeof(Spire1ExhaustRewardAwaitPatch), "Transpile", HarmonyPatchType.Transpiler);
        Require(DrawMove, typeof(Spire1ExhaustRewardAwaitPatch), "Transpile", HarmonyPatchType.Transpiler);
        Require(ModifyMethod, typeof(Spire1ExhaustDamagePatch), "Prefix", HarmonyPatchType.Prefix);
        Require(ModifyMethod, typeof(Spire1ExhaustDamagePatch), "Postfix", HarmonyPatchType.Postfix);
        Require(ExhaustMethod, typeof(Spire1ExhaustDispatchPatch), "Prefix", HarmonyPatchType.Prefix);
        Require(ExhaustMethod, typeof(Spire1ExhaustDispatchPatch), "Postfix", HarmonyPatchType.Postfix);
        Require(ExhaustMethod, typeof(Spire1ExhaustDispatchPatch), "Finalizer", HarmonyPatchType.Finalizer);
        Require(typeof(FeelNoPainPower).GetMethod(nameof(PowerModel.AfterCardExhausted))!, typeof(Spire1ExhaustFeelNoPainPatch), "Prefix", HarmonyPatchType.Prefix);
        Require(typeof(DarkEmbracePower).GetMethod(nameof(PowerModel.AfterCardExhausted))!, typeof(Spire1ExhaustDarkEmbracePatch), "Prefix", HarmonyPatchType.Prefix);
        foreach (var target in VigorCommands)
            Require(target, typeof(Spire1ExhaustVigorPatch), "Prefix", HarmonyPatchType.Prefix);
        foreach (var target in NativeCommands)
        {
            Require(target, typeof(Spire1ExhaustNativeScopePatch), "Prefix", HarmonyPatchType.Prefix);
            Require(target, typeof(Spire1ExhaustNativeScopePatch), "Postfix", HarmonyPatchType.Postfix);
            Require(target, typeof(Spire1ExhaustNativeScopePatch), "Finalizer", HarmonyPatchType.Finalizer);
        }
        foreach (var target in CausalMoves)
            Require(target, typeof(Spire1ExhaustCausalPatch), "Transpile", HarmonyPatchType.Transpiler);
    }

    private static void Require(MethodInfo target, Type patch, string name, HarmonyPatchType kind)
    {
        MethodInfo expected = patch.GetMethod(name, Methods)!;
        var info = Harmony.GetPatchInfo(target);
        var installed = kind switch
        {
            HarmonyPatchType.Prefix => info?.Prefixes,
            HarmonyPatchType.Postfix => info?.Postfixes,
            HarmonyPatchType.Finalizer => info?.Finalizers,
            _ => info?.Transpilers
        };
        if (installed == null || installed.Count(p => p.PatchMethod == expected) != 1)
            throw new InvalidOperationException("Exhaust transaction capability unavailable: " + patch.Name + "." + name);
    }

}

[HarmonyPatch]
internal static class Spire1ExhaustRewardScopePatch
{
    private static IEnumerable<MethodBase> TargetMethods() => [Spire1ExhaustPatches.BlockCommand, Spire1ExhaustPatches.DrawCommand];
    [HarmonyPrefix]
    private static void Prefix(out Spire1ExhaustTransaction.RewardScope? __state)
        => __state = Spire1ExhaustTransaction.EnterRewardCommand();
    [HarmonyPostfix]
    private static void Postfix(Spire1ExhaustTransaction.RewardScope? __state) => __state?.Restore();
    [HarmonyFinalizer]
    private static void Finalizer(Exception? __exception, Spire1ExhaustTransaction.RewardScope? __state)
    {
        if (__exception != null) __state?.Restore();
    }
}

[HarmonyPatch]
internal static class Spire1ExhaustRewardAwaitPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => [Spire1ExhaustPatches.BlockMove, Spire1ExhaustPatches.DrawMove];
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Exhaust, __originalMethod, instructions.ToList());
}


[HarmonyPatch]
internal static class Spire1ExhaustAttackPatch
{
    private static MethodBase TargetMethod() => Spire1ExhaustPatches.AttackMove;
    // Harmony treats -1 as unset (falls back to Priority.Normal); -2 sorts strictly after Priority.Last (0).
    [HarmonyTranspiler, HarmonyAfter("com.ritsukage.sts2-RitsuLib.framework-core"), HarmonyPriority(-2)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Exhaust, __originalMethod, instructions.ToList());
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class Spire1ExhaustDamagePatch
{
    [HarmonyPrefix]
    private static void Prefix(IRunState __0, ICombatState? __1, Creature? __2, Creature? __3, decimal __4,
        ValueProp __5, CardModel? __6, CardPlay? __7, ModifyDamageHookType __8, CardPreviewMode __9, out object? __state)
        => __state = Spire1ExhaustTransaction.BeginDamage(__0, __1, __2, __3, __4, __5, __6, __7, __8, __9);
    [HarmonyPostfix]
    private static void Postfix(ref IEnumerable<AbstractModel> __10, object? __state)
        => Spire1ExhaustTransaction.EndDamage(__state, ref __10);
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardExhausted))]
internal static class Spire1ExhaustDispatchPatch
{
    [HarmonyPrefix]
    private static void Prefix(ICombatState __0, PlayerChoiceContext __1, CardModel __2, bool __3,
        out Spire1ExhaustTransaction.ExhaustDispatch? __state)
        => __state = Spire1ExhaustTransaction.ExhaustDispatch.Enter(__0, __1, __2, __3);
    [HarmonyPostfix]
    private static void Postfix(ref Task __result, Spire1ExhaustTransaction.ExhaustDispatch? __state)
    {
        if (__state != null)
        {
            __state.Restore();
            __result = __state.Finish(__result);
        }
    }
    [HarmonyFinalizer]
    private static void Finalizer(Exception? __exception, Spire1ExhaustTransaction.ExhaustDispatch? __state)
    {
        if (__exception != null) __state?.Restore();
    }
}

[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust))]
internal static class Spire1ExhaustCommandScopePatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerChoiceContext __0, CardModel __1, out Spire1ExhaustTransaction.CommandScope? __state)
        => __state = Spire1ExhaustTransaction.EnterCommand(__0, __1);
    [HarmonyPostfix]
    private static void Postfix(Spire1ExhaustTransaction.CommandScope? __state) => __state?.Restore();
    [HarmonyFinalizer]
    private static void Finalizer(Exception? __exception, Spire1ExhaustTransaction.CommandScope? __state)
    {
        if (__exception != null) __state?.Restore();
    }
}

[HarmonyPatch]
internal static class Spire1ExhaustCommandPatch
{
    private static MethodBase TargetMethod() => Spire1ExhaustPatches.ExhaustMove;
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Exhaust, __originalMethod, instructions.ToList());
}

[HarmonyPatch]
internal static class Spire1ExhaustDispatchAwaitPatch
{
    private static MethodBase TargetMethod() => Spire1ExhaustPatches.DispatchMove;
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Exhaust, __originalMethod, instructions.ToList());
}

[HarmonyPatch]
internal static class Spire1ExhaustDamageAwaitPatch
{
    private static MethodBase TargetMethod() => Spire1ExhaustPatches.DamageMove;
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Exhaust, __originalMethod, instructions.ToList());
}


[HarmonyPatch(typeof(FeelNoPainPower), nameof(FeelNoPainPower.AfterCardExhausted))]
internal static class Spire1ExhaustFeelNoPainPatch
{
    [HarmonyPrefix]
    private static bool Prefix(FeelNoPainPower __instance, PlayerChoiceContext __0, CardModel __1, bool __2, ref Task __result)
    {
        if (!Spire1ExhaustTransaction.Capture(__instance, __0, __1, __2, false)) return true;
        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(DarkEmbracePower), nameof(DarkEmbracePower.AfterCardExhausted))]
internal static class Spire1ExhaustDarkEmbracePatch
{
    [HarmonyPrefix]
    private static bool Prefix(DarkEmbracePower __instance, PlayerChoiceContext __0, CardModel __1, bool __2, ref Task __result)
    {
        if (!Spire1ExhaustTransaction.Capture(__instance, __0, __1, __2, true)) return true;
        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch]
internal static class Spire1ExhaustNativeScopePatch
{
    private static IEnumerable<MethodBase> TargetMethods() => Spire1ExhaustPatches.NativeCommands;
    [HarmonyPrefix]
    private static void Prefix(MethodBase __originalMethod, out Spire1ExhaustTransaction.NativeScope? __state)
        => __state = Spire1ExhaustTransaction.EnterNative(__originalMethod);
    [HarmonyPostfix]
    private static void Postfix(Spire1ExhaustTransaction.NativeScope? __state) => __state?.Restore();
    [HarmonyFinalizer]
    private static void Finalizer(Exception? __exception, Spire1ExhaustTransaction.NativeScope? __state)
    { if (__exception != null) __state?.Restore(); }
}

[HarmonyPatch]
internal static class Spire1ExhaustCausalPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => Spire1ExhaustPatches.CausalMoves;
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Exhaust, __originalMethod, instructions.ToList());
}

[HarmonyPatch]
internal static class Spire1ExhaustVigorPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => Spire1ExhaustPatches.VigorCommands;
    [HarmonyPrefix]
    private static bool Prefix(AttackCommand command, ref Task __result)
    {
        if (!Spire1DamageSnapshot.IsCaptured(command)) return true;
        Spire1DamageSnapshot.GuardAttack(command);
        __result = Task.CompletedTask;
        return false;
    }
}
