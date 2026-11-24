using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;
using OwnReboot = Spire1.Spire1Code.Cards.Reboot;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Only an own Reboot's effect sequence: legacy onGainedBlock appends Juggernaut damage
/// behind that play's already-queued draw. Ordinary native consumers stay inline.
/// </summary>
internal static class Spire1RebootTransaction
{
    private const string OwnerId = "Spire1.RebootTransaction";
    private const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly object InstallationLock = new();
    private static readonly AsyncLocal<Frame?> Current = new();
    private static readonly AsyncLocal<Operation?> ActiveOperation = new();
    private static readonly Dictionary<MethodBase, Site> Sites = new();
    private static readonly List<InstalledPatch> Installation = new();
    private static Func<CombatManager, CombatId?, CombatTurnState?>? _liveTurn;
    private static Func<CombatManager, CombatTurnState, bool>? _isEnding;
    private static int _status;
    private static string _reason = "Reboot has not been initialized.";

    internal enum CapabilityStatus { Uninitialized, Ready, CleanUnavailable, ResidualGlobal }
    internal static CapabilityStatus Status => (CapabilityStatus)Volatile.Read(ref _status);
    internal static bool Available => Status == CapabilityStatus.Ready;
    internal static string Reason => _reason;

    private enum Kind { Draw, Add, Shuffle, Block, Damage, Kill, Hook }
    private sealed record InstalledPatch(MethodInfo Target, MethodInfo Handler, HarmonyPatchType Kind);
    private sealed record Site(int Id, MethodInfo Entry, MethodInfo Move, Kind Kind, string Prefix);

    internal static bool EnsureInstalled()
    {
        lock (InstallationLock)
        {
            if (Status is CapabilityStatus.CleanUnavailable or CapabilityStatus.ResidualGlobal)
                return false;
            var harmony = new Harmony(OwnerId);
            try
            {
                if (Sites.Count == 0)
                    ResolveSites();
                if (Installation.Count == 0)
                    PlanInstallation();
                if (!Available)
                    foreach (InstalledPatch patch in Installation)
                        Apply(harmony, patch);
                foreach (InstalledPatch patch in Installation)
                    Verify(patch, present: true);
                _reason = "Exact Reboot consumer and selected continuation closure installed.";
                Volatile.Write(ref _status, (int)CapabilityStatus.Ready);
                return true;
            }
            catch (Exception error)
            {
                bool clean = true;
                string reason = "Reboot unavailable: " + error.Message;
                foreach (InstalledPatch patch in Installation)
                {
                    try { harmony.Unpatch(patch.Target, patch.Handler); }
                    catch (Exception cleanup) { reason += "; rollback attempt: " + cleanup.Message; }
                    try { Verify(patch, present: false); }
                    catch (Exception residual) { clean = false; reason += "; " + residual.Message; }
                }
                _reason = reason;
                Volatile.Write(ref _status, (int)(clean ? CapabilityStatus.CleanUnavailable : CapabilityStatus.ResidualGlobal));
                if (!clean)
                    Spire1PowersGate.MarkDeterministicUnavailable(reason);
                // r8e: diagnostic only. Behaviour above (rollback, clean/status, gate marking) is unchanged; this line
                // records the real exception and the final reason so the next run does not lose the inner cause.
                MainFile.Logger.Error(
                    "[Spire1] Reboot transaction unavailable (status " +
                    (clean ? nameof(CapabilityStatus.CleanUnavailable) : nameof(CapabilityStatus.ResidualGlobal)) +
                    "): " + error + " | final reason: " + reason);
                return false;
            }
        }
    }

    private static MethodInfo Own(string name) => typeof(Spire1RebootTransaction).GetMethod(name, Methods)!;
    private static MethodInfo Exact(Type owner, string name, Type result, params Type[] args)
    {
        MethodInfo method = AccessTools.DeclaredMethod(owner, name, args)
            ?? throw new InvalidOperationException("Missing Reboot target " + owner.Name + "." + name);
        if (method.ReturnType != result)
            throw new InvalidOperationException("Changed Reboot return type: " + method);
        return method;
    }

    // Only business admission is owned here. The shared helper resolves/pins/emits every native body.
    private static void ResolveSites()
    {
        MethodInfo live = Exact(typeof(CombatManager), "LiveTurnStateFor", typeof(CombatTurnState), typeof(CombatId?));
        MethodInfo ending = Exact(typeof(CombatManager), "IsCombatEnding", typeof(bool), typeof(CombatTurnState));
        if (live.IsStatic || ending.IsStatic)
            throw new InvalidOperationException("Reboot combat lifetime signature changed");
        _liveTurn = live.CreateDelegate<Func<CombatManager, CombatId?, CombatTurnState?>>();
        _isEnding = ending.CreateDelegate<Func<CombatManager, CombatTurnState, bool>>();
        AddSite(Exact(typeof(CardPileCmd), "DrawInternal", typeof(Task<IEnumerable<CardModel>>),
            typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)), Kind.Draw, nameof(EnterDraw));
        AddSite(Exact(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(Task<IReadOnlyList<CardPileAddResult>>),
            typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool)),
            Kind.Add, nameof(EnterAdd));
        AddSite(Exact(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle), typeof(Task), typeof(PlayerChoiceContext), typeof(Player)),
            Kind.Shuffle, nameof(EnterShuffle));
        AddSite(Exact(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), typeof(Task<decimal>), typeof(Creature), typeof(decimal),
            typeof(ValueProp), typeof(CardPlay), typeof(bool)), Kind.Block, nameof(EnterGainBlock));
        AddSite(Exact(typeof(CreatureCmd), nameof(CreatureCmd.Damage), typeof(Task<IEnumerable<DamageResult>>),
            typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp), typeof(Creature),
            typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType)), Kind.Damage, nameof(EnterDamage));
        AddSite(Exact(typeof(CreatureCmd), nameof(CreatureCmd.Kill), typeof(Task), typeof(IReadOnlyCollection<Creature>),
            typeof(CardModel), typeof(bool)), Kind.Kill, nameof(EnterKill));
        AddSite(Exact(typeof(CreatureCmd), "KillWithoutCheckingWinCondition", typeof(Task), typeof(Creature), typeof(CardModel),
            typeof(bool), typeof(int)), Kind.Kill, nameof(EnterKillOne));
        HookSite(nameof(Hook.AfterShuffle), nameof(EnterShuffleHook));
        HookSite(nameof(Hook.AfterCardDrawn), nameof(EnterCardHook));
        HookSite(nameof(Hook.AfterCardChangedPiles), nameof(EnterChangedHook));
        HookSite(nameof(Hook.BeforeBlockGained), nameof(EnterBlockHook));
        HookSite(nameof(Hook.AfterModifyingBlockAmount), nameof(EnterBlockModifierHook));
        HookSite(nameof(Hook.AfterBlockGained), nameof(EnterBlockHook));
        HookSite(nameof(Hook.AfterModifyingDamageAmount), nameof(EnterDamageModifierHook));
        HookSite(nameof(Hook.BeforeDamageReceived), nameof(EnterDamageHook));
        HookSite(nameof(Hook.AfterModifyingHpLostBeforeOsty), nameof(EnterHook));
        HookSite(nameof(Hook.AfterModifyingHpLostAfterOsty), nameof(EnterHook));
        HookSite(nameof(Hook.AfterBlockBroken), nameof(EnterBrokenHook));
        HookSite(nameof(Hook.AfterCurrentHpChanged), nameof(EnterCreatureHook));
        HookSite(nameof(Hook.AfterDamageGiven), nameof(EnterDamageHook));
        HookSite(nameof(Hook.AfterDamageReceived), nameof(EnterDamageHook));
        HookSite(nameof(Hook.BeforeDeath), nameof(EnterCreatureHook));
        HookSite(nameof(Hook.AfterDeath), nameof(EnterDeathHook));
    }

    private static void HookSite(string name, string prefix)
    {
        MethodInfo[] matches = typeof(Hook).GetMethods(Methods).Where(m => m.Name == name && m.IsStatic && m.ReturnType == typeof(Task)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Ambiguous Reboot hook " + name);
        AddSite(matches[0], Kind.Hook, prefix);
    }

    private static void AddSite(MethodInfo entry, Kind kind, string prefix)
    {
        Spire1ContinuationIl.Body body = Spire1ContinuationIl.ForKickoff(entry);
        if (body.Id is < 0 or > 22)
            throw new InvalidOperationException("Reboot requested an unapproved continuation body");
        Sites.Add(entry, new Site(body.Id, entry, body.Move, kind, prefix));
    }

    private static void PlanInstallation()
    {
        MethodInfo consumer = Exact(typeof(JuggernautPower), nameof(JuggernautPower.AfterBlockGained), typeof(Task),
            typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel));
        MethodInfo wrapper = Exact(typeof(CardModel), nameof(CardModel.OnPlayWrapper), typeof(Task), typeof(PlayerChoiceContext),
            typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool));
        Installation.Add(new(consumer, Own(nameof(CaptureJuggernaut)), HarmonyPatchType.Prefix));
        Installation.Add(new(wrapper, Own(nameof(ShadowParent)), HarmonyPatchType.Prefix));
        Installation.Add(new(wrapper, Own(nameof(RestoreParent)), HarmonyPatchType.Finalizer));
        foreach (Site site in Sites.Values)
        {
            Installation.Add(new(site.Entry, Own(site.Prefix), HarmonyPatchType.Prefix));
            MethodInfo completion = site.Entry.ReturnType == typeof(Task) ? Own(nameof(Complete)) : site.Kind switch
            {
                Kind.Draw => Own(nameof(CompleteDraw)),
                Kind.Add => Own(nameof(CompleteAdd)),
                Kind.Block => Own(nameof(CompleteBlock)),
                Kind.Damage => Own(nameof(CompleteDamage)),
                _ => throw new InvalidOperationException("Unsupported Reboot completion target: " + site.Entry)
            };
            Installation.Add(new(site.Entry, completion, HarmonyPatchType.Postfix));
            Installation.Add(new(site.Entry, Own(nameof(RestoreOperation)), HarmonyPatchType.Finalizer));
            Installation.Add(new(site.Move, Own(nameof(GuardContinuations)), HarmonyPatchType.Transpiler));
        }
    }

    private static void Apply(Harmony harmony, InstalledPatch patch)
    {
        var method = new HarmonyMethod(patch.Handler) { priority = Priority.First };
        switch (patch.Kind)
        {
            case HarmonyPatchType.Prefix: harmony.Patch(patch.Target, prefix: method); break;
            case HarmonyPatchType.Postfix: harmony.Patch(patch.Target, postfix: method); break;
            case HarmonyPatchType.Finalizer: harmony.Patch(patch.Target, finalizer: method); break;
            case HarmonyPatchType.Transpiler: harmony.Patch(patch.Target, transpiler: method); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static void Verify(InstalledPatch patch, bool present)
    {
        var info = Harmony.GetPatchInfo(patch.Target);
        IEnumerable<Patch> candidates = info == null ? Array.Empty<Patch>() : patch.Kind switch {
            HarmonyPatchType.Prefix => info.Prefixes, HarmonyPatchType.Postfix => info.Postfixes,
            HarmonyPatchType.Finalizer => info.Finalizers, HarmonyPatchType.Transpiler => info.Transpilers,
            _ => throw new ArgumentOutOfRangeException()
        };
        Patch[] matches = candidates.Where(p => p.owner == OwnerId && p.PatchMethod == patch.Handler).ToArray();
        if (present ? matches.Length != 1 || matches[0].priority != Priority.First : matches.Length != 0)
            throw new InvalidOperationException((present ? "Missing exact " : "Residual exact ") + patch.Kind + ": " + patch.Handler);
    }

    private static IEnumerable<CodeInstruction> GuardContinuations(IEnumerable<CodeInstruction> source, MethodBase __originalMethod)
        => Spire1ContinuationIl.Rewrite(Spire1ContinuationIl.Owner.Reboot, __originalMethod, source.ToList());

    private readonly struct Scope(Operation? previous, Frame? previousFrame, Operation? operation)
    {
        internal readonly Operation? Previous = previous;
        internal readonly Frame? PreviousFrame = previousFrame;
        internal readonly Operation? Operation = operation;
    }

    private static void Enter(MethodBase method, object selector, bool selected, out Scope scope)
    {
        Frame? frame = Current.Value;
        Operation? previous = ActiveOperation.Value;
        Operation? operation = selected && frame != null ? new Operation(Sites[method], frame, selector) : null;
        scope = new Scope(previous, frame, operation);
        ActiveOperation.Value = operation;
        if (!selected)
            Current.Value = null; // Foreign/nested native kickoff masks both selected operation and own frame.
    }

    private static bool Select(Frame? frame) => frame is { IsOpen: true };
    private static bool ContainsReference(IReadOnlyList<CardModel> cards, CardModel card)
    {
        for (int i = 0; i < cards.Count; i++)
            if (ReferenceEquals(cards[i], card)) return true;
        return false;
    }
    private static void EnterDraw(MethodBase __originalMethod, Player player, out Scope __state)
        => Enter(__originalMethod, player, Select(Current.Value) && Current.Value!.Owns(player), out __state);
    private static void EnterShuffle(MethodBase __originalMethod, Player player, out Scope __state)
        => Enter(__originalMethod, player, Parent(Kind.Draw, Current.Value?.State.State) is { } parent
            && ReferenceEquals(parent.Selector, player), out __state);
    private static void EnterAdd(MethodBase __originalMethod, IEnumerable<CardModel> cards, CardPile newPile,
        AbstractModel? clonedBy, out Scope __state)
    {
        Frame? frame = Current.Value;
        bool selected = Select(frame) && frame!.Owns(newPile) && cards is IReadOnlyList<CardModel>;
        if (selected)
        {
            var list = (IReadOnlyList<CardModel>)cards;
            for (int i = 0; i < list.Count; i++)
                if (!frame!.Owns(list[i]) || list[i].Pile == null || !frame.Owns(list[i].Pile!)) { selected = false; break; }
        }
        Enter(__originalMethod, newPile, selected, out __state);
        if (__state.Operation is { } operation)
        {
            operation.Cards = (IReadOnlyList<CardModel>)cards;
            operation.ClonedBy = clonedBy;
        }
    }
    private static void EnterGainBlock(MethodBase __originalMethod, Creature creature, CardPlay? cardPlay, out Scope __state)
    {
        Enter(__originalMethod, creature, Select(Current.Value) && ReferenceEquals(creature, Current.Value!.Dealer), out __state);
        if (__state.Operation is { } operation)
        {
            operation.SourceCard = cardPlay?.Card;
            operation.Play = cardPlay;
        }
    }
    private static void EnterDamage(MethodBase __originalMethod, IEnumerable<DamageTargetData>? targetData, Creature? dealer,
        CardModel? cardSource, out Scope __state)
    {
        Frame? frame = Current.Value;
        bool selected = Select(frame) && ReferenceEquals(dealer, frame!.Dealer) && targetData is IReadOnlyList<DamageTargetData>;
        if (selected)
        {
            var list = (IReadOnlyList<DamageTargetData>)targetData!;
            for (int i = 0; i < list.Count; i++)
                if (!ReferenceEquals(list[i].target.CombatState, frame!.State.State)) { selected = false; break; }
        }
        Enter(__originalMethod, dealer!, selected, out __state);
        if (__state.Operation is { } operation)
        {
            operation.DamageTargets = (IReadOnlyList<DamageTargetData>)targetData!;
            operation.SourceCard = cardSource;
        }
    }
    private static bool SelectedDeath(Frame? frame, Creature creature, CardModel? source)
    {
        Operation? parent = ActiveOperation.Value;
        return Select(frame) && parent is { Active: true } && ReferenceEquals(parent.Frame, frame)
            && ReferenceEquals(creature.CombatState, frame!.State.State) && ReferenceEquals(source, parent.SourceCard)
            && (parent.Site.Kind == Kind.Damage && parent.Committed && parent.HasDamageTarget(creature)
                || parent.Site.Kind == Kind.Kill && parent.HasDeathTarget(creature));
    }
    private static void EnterKill(MethodBase __originalMethod, IReadOnlyCollection<Creature> creatures, CardModel? cardSource, out Scope __state)
    {
        bool selected = creatures is IReadOnlyList<Creature> list && list.Count > 0;
        if (selected)
        {
            var targets = (IReadOnlyList<Creature>)creatures;
            for (int i = 0; i < targets.Count; i++)
                if (!SelectedDeath(Current.Value, targets[i], cardSource)) { selected = false; break; }
        }
        Enter(__originalMethod, creatures, selected, out __state);
        if (__state.Operation is { } operation)
        {
            operation.DeathTargets = (IReadOnlyList<Creature>)creatures;
            operation.SourceCard = cardSource;
        }
    }
    private static void EnterKillOne(MethodBase __originalMethod, Creature creature, CardModel? cardSource, out Scope __state)
    {
        Enter(__originalMethod, creature, SelectedDeath(Current.Value, creature, cardSource), out __state);
        if (__state.Operation is { } operation)
            operation.SourceCard = cardSource;
    }

    private static Operation? Parent(Kind kind, ICombatState? combat)
    {
        Operation? parent = ActiveOperation.Value;
        return Select(Current.Value) && parent is { Active: true } && parent.Site.Kind == kind
            && ReferenceEquals(parent.Frame, Current.Value) && ReferenceEquals(combat, parent.Frame.State.State) ? parent : null;
    }
    private static void EnterShuffleHook(MethodBase __originalMethod, ICombatState combatState, Player shuffler, out Scope __state)
    {
        Frame? frame = Current.Value;
        bool selected = Select(frame) && frame!.Owns(shuffler) && ReferenceEquals(combatState, frame.State.State)
            && (ActiveOperation.Value == null || Parent(Kind.Shuffle, combatState) is { } parent
                && ReferenceEquals(parent.Selector, shuffler));
        Enter(__originalMethod, combatState, selected, out __state);
    }
    private static void EnterCardHook(MethodBase __originalMethod, ICombatState? combatState, CardModel card, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Draw, combatState) is { } parent
            && parent.Frame.Owns(card) && parent.Frame.InHand(card), out __state);
    private static void EnterChangedHook(MethodBase __originalMethod, ICombatState? combatState, CardModel card,
        AbstractModel? clonedBy, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Add, combatState) is { } parent && parent.HasCard(card)
            && ReferenceEquals(parent.ClonedBy, clonedBy), out __state);
    private static void EnterBlockHook(MethodBase __originalMethod, ICombatState combatState, Creature creature,
        CardModel? cardSource, out Scope __state)
    {
        Operation? parent = Parent(Kind.Block, combatState);
        Enter(__originalMethod, combatState, parent != null && ReferenceEquals(parent.Selector, creature)
            && ReferenceEquals(parent.SourceCard, cardSource), out __state);
        if (__state.Operation is { } operation)
            operation.SourceCard = cardSource;
    }
    private static void EnterBlockModifierHook(MethodBase __originalMethod, ICombatState combatState, CardModel? cardSource,
        CardPlay? cardPlay, out Scope __state)
        => Enter(__originalMethod, combatState, Parent(Kind.Block, combatState) is { } parent
            && ReferenceEquals(parent.SourceCard, cardSource) && ReferenceEquals(parent.Play, cardPlay), out __state);
    private static void EnterDamageModifierHook(MethodBase __originalMethod, ICombatState? combatState, CardModel? cardSource, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Damage, combatState) is { } parent
            && ReferenceEquals(parent.SourceCard, cardSource), out __state);
    private static void EnterHook(MethodBase __originalMethod, ICombatState? combatState, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Damage, combatState) != null, out __state);
    private static void EnterDamageHook(MethodBase __originalMethod, ICombatState? combatState, Creature? dealer, Creature target,
        CardModel? cardSource, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Damage, combatState) is { } parent
            && ReferenceEquals(parent.Selector, dealer) && ReferenceEquals(parent.SourceCard, cardSource)
            && parent.HasDamageTarget(target), out __state);
    private static void EnterBrokenHook(MethodBase __originalMethod, ICombatState? combatState, Creature? breaker, Creature target, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Damage, combatState) is { } parent
            && ReferenceEquals(parent.Selector, breaker) && parent.HasDamageTarget(target), out __state);
    private static void EnterCreatureHook(MethodBase __originalMethod, ICombatState? combatState, Creature creature, out Scope __state)
    {
        bool selected = Parent(Kind.Kill, combatState) is { } death && death.HasDeathTarget(creature);
        if (__originalMethod.Name == nameof(Hook.AfterCurrentHpChanged))
            selected |= Parent(Kind.Damage, combatState) is { } damage && damage.HasDamageTarget(creature);
        Enter(__originalMethod, combatState!, selected, out __state);
    }
    private static void EnterDeathHook(MethodBase __originalMethod, ICombatState? combatState, Creature creature,
        CardModel? source, out Scope __state)
        => Enter(__originalMethod, combatState!, Parent(Kind.Kill, combatState) is { } parent
            && parent.HasDeathTarget(creature) && ReferenceEquals(parent.SourceCard, source), out __state);

    private static void Complete(ref Task __result, Scope __state)
    {
        if (__state.Operation != null)
            __result = Track(__result, __state.Operation);
    }
    private static async Task Track(Task task, Operation operation)
    {
        try { await task; }
        finally { operation.Active = false; }
    }
    private static void CompleteDraw(ref Task<IEnumerable<CardModel>> __result, Scope __state)
    {
        if (__state.Operation != null)
            __result = TrackValue(__result, __state.Operation);
    }
    private static void CompleteAdd(ref Task<IReadOnlyList<CardPileAddResult>> __result, Scope __state)
    {
        if (__state.Operation != null)
            __result = TrackValue(__result, __state.Operation);
    }
    private static void CompleteBlock(ref Task<decimal> __result, Scope __state)
    {
        if (__state.Operation != null)
            __result = TrackValue(__result, __state.Operation);
    }
    private static void CompleteDamage(ref Task<IEnumerable<DamageResult>> __result, Scope __state)
    {
        if (__state.Operation != null)
            __result = TrackValue(__result, __state.Operation);
    }
    private static async Task<T> TrackValue<T>(Task<T> task, Operation operation)
    {
        try { return await task; }
        finally { operation.Active = false; }
    }
    private static Exception? RestoreOperation(Exception? __exception, Scope __state)
    {
        if (__exception != null && __state.Operation != null)
            __state.Operation.Active = false;
        ActiveOperation.Value = __state.Previous;
        Current.Value = __state.PreviousFrame;
        return __exception;
    }

    private static void CheckDrawContinuation(Player player)
    {
        Operation? op = ActiveOperation.Value;
        if (op?.Site.Kind == Kind.Draw && ReferenceEquals(op.Selector, player))
            op.Check(full: true);
    }

    private static void CheckInitial(object selector, int siteId, int state)
    {
        // A resumed branch must first execute the original GetResult (including its own fault/cancel).
        if (state >= 0)
            return;
        Operation? op = ActiveOperation.Value;
        if (op != null && op.Site.Id == siteId && ReferenceEquals(selector, op.Selector))
            op.Check(full: op.Site.Kind is not Kind.Hook and not Kind.Kill);
    }

    private static void CheckContinuation(object selector, int siteId, PlayerChoiceContext? context, AbstractModel? model)
    {
        Operation? op = ActiveOperation.Value;
        if (op == null || op.Site.Id != siteId || !ReferenceEquals(selector, op.Selector))
            return;
        try { op.Check(full: op.Site.Kind == Kind.Draw || op.Site.Kind is Kind.Add or Kind.Block or Kind.Damage && !op.Committed); }
        catch
        {
            if (context != null && model != null && ReferenceEquals(context.LastInvolvedModel, model))
                context.PopModel(model);
            throw;
        }
    }

    private static void BeforeCommit(int siteId, bool startsCardMove)
    {
        Operation? op = ActiveOperation.Value;
        if (op == null || op.Site.Id != siteId)
            return;
        op.Check(full: startsCardMove || !op.Committed);
        op.Committed = true;
    }

    private sealed class Operation(Site site, Frame frame, object selector)
    {
        internal readonly Site Site = site;
        internal readonly Frame Frame = frame;
        internal readonly object Selector = selector;
        internal volatile bool Active = true;
        internal bool Committed;
        internal IReadOnlyList<CardModel>? Cards;
        internal IReadOnlyList<DamageTargetData>? DamageTargets;
        internal IReadOnlyList<Creature>? DeathTargets;
        internal CardModel? SourceCard;
        internal CardPlay? Play;
        internal AbstractModel? ClonedBy;
        internal bool HasCard(CardModel card)
        {
            if (Cards != null)
                for (int i = 0; i < Cards.Count; i++)
                    if (ReferenceEquals(Cards[i], card)) return true;
            return false;
        }
        internal bool HasDamageTarget(Creature target)
        {
            if (DamageTargets != null)
                for (int i = 0; i < DamageTargets.Count; i++)
                    if (ReferenceEquals(DamageTargets[i].target, target)) return true;
            return false;
        }
        internal bool HasDeathTarget(Creature target)
        {
            if (Site.Kind != Kind.Kill)
                return false;
            if (ReferenceEquals(Selector, target))
                return true;
            if (DeathTargets != null)
                for (int i = 0; i < DeathTargets.Count; i++)
                    if (ReferenceEquals(DeathTargets[i], target)) return true;
            return false;
        }
        internal void Check(bool full)
        {
            if (!Active)
                throw new Stopped(Frame);
            Frame.Require(full);
        }
    }

    internal sealed class Stopped(Frame frame) : Exception { internal readonly Frame Frame = frame; }
    internal static void RequireAvailable()
    {
        if (!Available || Spire1PowersGate.ContentUnavailableActive)
            throw new InvalidOperationException(Reason);
    }
    internal static Frame Begin(OwnReboot card, CardPlay play)
    {
        RequireAvailable();
        if (card.GetType() != typeof(OwnReboot) || !ReferenceEquals(play.Card, card) || !ReferenceEquals(play.Player, card.Owner))
            throw new InvalidOperationException("Reboot transaction requires its exact own card play.");
        if (ExecutionContext.IsFlowSuppressed())
            throw new InvalidOperationException("Reboot requires ExecutionContext flow.");
        var manager = CombatManager.Instance;
        var state = _liveTurn!(manager, manager.CurrentCombatId) ?? throw new InvalidOperationException("Reboot has no live combat.");
        var pcs = card.Owner.PlayerCombatState;
        if (!ReferenceEquals(state.State, card.Owner.Creature.CombatState) || pcs == null
            || !state.State.ContainsCard(card) || !ReferenceEquals(card.Pile, pcs.PlayPile)
            || !ContainsReference(pcs.PlayPile.Cards, card))
            throw new InvalidOperationException("Reboot is not the current combat's actual played card.");
        var frame = new Frame(card, state, Current.Value);
        Current.Value = frame;
        ActiveOperation.Value = null;
        return frame;
    }

    private readonly struct ParentScope(Frame? frame, Operation? operation)
    {
        internal readonly Frame? Frame = frame;
        internal readonly Operation? Operation = operation;
    }
    private static void ShadowParent(CardModel __instance, out ParentScope __state)
    {
        __state = new ParentScope(Current.Value, ActiveOperation.Value);
        if (__instance.GetType() == typeof(OwnReboot) && !Available)
            throw new InvalidOperationException(Reason);
        Current.Value = null;
        ActiveOperation.Value = null;
    }
    private static Exception? RestoreParent(Exception? __exception, ParentScope __state)
    {
        Current.Value = __state.Frame;
        ActiveOperation.Value = __state.Operation;
        return __exception;
    }

    private static bool CaptureJuggernaut(JuggernautPower __instance, Creature creature, decimal amount,
        CardModel? cardSource, ref Task __result)
    {
        Operation? op = ActiveOperation.Value;
        if (op == null || op.Site.Entry.DeclaringType != typeof(Hook) || op.Site.Entry.Name != nameof(Hook.AfterBlockGained)
            || !ReferenceEquals(creature, op.Frame.Dealer) || !ReferenceEquals(__instance.Owner, creature)
            || !ReferenceEquals(cardSource, op.SourceCard) || amount <= 0)
            return true;
        try { op.Check(full: false); op.Frame.Capture(__instance); __result = Task.CompletedTask; }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested) { __result = Task.FromCanceled(e.CancellationToken); }
        catch (Exception e) { __result = Task.FromException(e); }
        return false;
    }

    internal sealed class Frame : IDisposable
    {
        private readonly OwnReboot _card;
        private readonly Player _player;
        private readonly PlayerCombatState _pcs;
        private readonly IRunState _run;
        private readonly int _turn;
        private readonly Frame? _previous;
        private readonly Operation? _previousOperation;
        private Queue<int>? _damage;
        private bool _draining;
        private bool _drained;
        private int _closed;
        internal readonly CombatTurnState State;
        internal readonly Creature Dealer;
        internal bool IsOpen => Volatile.Read(ref _closed) == 0;

        internal Frame(OwnReboot card, CombatTurnState state, Frame? previous)
        {
            _card = card; _player = card.Owner; _pcs = card.Owner.PlayerCombatState!; _run = card.Owner.RunState;
            _turn = _pcs.TurnNumber; State = state; Dealer = card.Owner.Creature;
            _previous = previous; _previousOperation = ActiveOperation.Value;
        }
        internal bool Owns(Player player) => ReferenceEquals(player, _player);
        internal bool Owns(Stopped stopped) => ReferenceEquals(stopped.Frame, this);
        internal bool Owns(CardPile pile) => ReferenceEquals(pile, _pcs.Hand) || ReferenceEquals(pile, _pcs.DrawPile)
            || ReferenceEquals(pile, _pcs.DiscardPile) || ReferenceEquals(pile, _pcs.PlayPile) || ReferenceEquals(pile, _pcs.ExhaustPile);
        internal bool Owns(CardModel card) => ReferenceEquals(card.Owner, _player) && ReferenceEquals(card.CombatState, State.State);
        internal bool InHand(CardModel card) => ReferenceEquals(card.Pile, _pcs.Hand)
            && ContainsReference(_pcs.Hand.Cards, card);

        private bool HardValid()
        {
            State.Ct.ThrowIfCancellationRequested();
            // An ended-but-not-torn-down combat still owns death/dispatch cleanup. Do not use IsLive here.
            return IsOpen && CombatManager.Instance.CurrentCombatId == State.Id
                && ReferenceEquals(_card.Owner, _player) && ReferenceEquals(_player.Creature, Dealer)
                && ReferenceEquals(_player.PlayerCombatState, _pcs) && _pcs.TurnNumber == _turn
                && ReferenceEquals(_player.RunState, _run) && ReferenceEquals(Dealer.CombatState, State.State)
                && Available && !Spire1PowersGate.ContentUnavailableActive
                && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);
        }
        internal bool CanContinue() => HardValid() && State.IsLive && !_isEnding!(CombatManager.Instance, State)
            && Dealer.IsAlive && _player.IsActiveForHooks;
        internal void Require(bool full)
        {
            if (!(full ? CanContinue() : HardValid()))
                throw new Stopped(this);
        }
        internal void Capture(JuggernautPower source)
        {
            lock (this)
            {
                Require(full: false);
                if (_drained)
                    throw new Stopped(this);
                (_damage ??= new Queue<int>()).Enqueue(source.Amount);
            }
            source.Flash();
        }
        internal async Task Drain()
        {
            lock (this)
            {
                if (_draining || _drained)
                    throw new InvalidOperationException("Reboot continuation drained twice.");
                _draining = true;
            }
            while (CanContinue())
            {
                int amount;
                lock (this)
                {
                    if (_damage == null || !_damage.TryDequeue(out amount)) { _drained = true; return; }
                }
                var targets = State.State.HittableEnemies;
                if (targets.Count == 0)
                    continue;
                var target = targets[_run.Rng.CombatTargets.NextInt(targets.Count)];
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),
                    (IEnumerable<DamageTargetData>)[new DamageTargetData(target)], amount, ValueProp.Unpowered,
                    Dealer, null, null, ModifyDamageHookType.All);
            }
        }
        public void Dispose()
        {
            lock (this) { Interlocked.Exchange(ref _closed, 1); _damage?.Clear(); _damage = null; }
            Current.Value = _previous;
            ActiveOperation.Value = _previousOperation;
        }
    }
}
