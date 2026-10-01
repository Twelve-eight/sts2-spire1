using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Every completed CardPlay that began with this effect deals 3 unpowered damage.
/// The Watcher bridge can suppress the exit payment when native Calm owns that payment.
/// </summary>
public sealed class SerpentFormPower : CustomPowerModel
{
    private const decimal DamagePerCard = 3m;
    private const decimal EnergyOnExit = 2m;

    private sealed class PendingTracker
    {
        public readonly HashSet<CardPlay> plays = new(ReferenceEqualityComparer.Instance);
        public bool recoveryInProgress;
        private PendingTracker? parent;

        public PendingTracker Root
        {
            get
            {
                if (parent == null)
                {
                    return this;
                }

                parent = parent.Root;
                return parent;
            }
        }

        public void MergeInto(PendingTracker target)
        {
            PendingTracker sourceRoot = Root;
            PendingTracker targetRoot = target.Root;
            if (ReferenceEquals(sourceRoot, targetRoot))
            {
                return;
            }

            targetRoot.plays.UnionWith(sourceRoot.plays);
            targetRoot.recoveryInProgress |= sourceRoot.recoveryInProgress;
            sourceRoot.parent = targetRoot;
        }
    }

    private sealed class Data
    {
        public PendingTracker pending = new();
        public bool exitHandled;
        public bool completionBridge;
    }

    public bool GrantExitEnergy { get; set; } = true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Serpent Form",
            "#Each card play deals 3 damage to a random enemy. Leaving Calm grants 2 *Energy*.",
            "Every card play hits a random enemy.");

    // The engine reinitializes internal data when cloning; no canonical/shared play set.
    protected override object InitInternalData() => new Data();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (!data.completionBridge && cardPlay.Player.Creature == Owner)
        {
            data.pending.Root.plays.Add(cardPlay);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        PendingTracker pending = data.pending.Root;
        // Remove before awaiting damage: a repeated callback or a nested card cannot double-trigger.
        if (!pending.plays.Remove(cardPlay))
        {
            // A duplicate callback cannot deal or consume RNG, but it may be the
            // completion bridge's last callback after the shared tracker was consumed.
            if (data.completionBridge && pending.plays.Count == 0 && Owner.Powers.Contains(this))
            {
                data.exitHandled = true;
                await PowerCmd.Remove(this);
            }

            return;
        }

        try
        {
            ICombatState? combatState = Owner.CombatState;
            if (combatState == null || CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }

            List<Creature> hittable = combatState.HittableEnemies.ToList();
            if (hittable.Count == 0)
            {
                return;
            }

            // Only the synchronized run RNG selects targets. An empty list must not advance it.
            Creature? target = combatState.RunState.Rng.CombatTargets.NextItem(hittable);
            if (target == null)
            {
                return;
            }

            Flash();
            await CreatureCmd.Damage(choiceContext, target, DamagePerCard, ValueProp.Unpowered, Owner, null, null);
        }
        finally
        {
            if (data.completionBridge && pending.Root.plays.Count == 0 && Owner.Powers.Contains(this))
            {
                data.exitHandled = true;
                await PowerCmd.Remove(this);
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        Data data = GetInternalData<Data>();
        if (data.completionBridge)
        {
            await RecoverRemovedBridge(oldOwner, data);
            return;
        }

        bool clearPending = true;
        try
        {
            if (data.pending.Root.plays.Count > 0 && IsCombatActive(oldOwner))
            {
                // Keep the source tracker alive on every failure so an exception is never
                // converted into a silent loss before the current listener snapshot can finish.
                clearPending = false;
                clearPending = !await PreservePendingPlays(oldOwner, data);
            }
        }
        finally
        {
            if (clearPending)
            {
                data.pending.Root.plays.Clear();
            }

            // Handoff failures must not skip the original one-shot exit-energy semantics.
            await FinishRemoval(oldOwner, data);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        Data data = GetInternalData<Data>();
        PendingTracker pending = data.pending.Root;
        pending.plays.Clear();
        if (!data.completionBridge || !Owner.Powers.Contains(this))
        {
            return;
        }

        data.exitHandled = true;
        await PowerCmd.Remove(this);
    }

    private async Task FinishRemoval(Creature oldOwner, Data data)
    {
        if (data.exitHandled)
        {
            return;
        }

        data.exitHandled = true;
        if (!GrantExitEnergy || oldOwner.Player == null || oldOwner.IsDead
            || oldOwner.CombatState == null || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        await PlayerCmd.GainEnergy(EnergyOnExit, oldOwner.Player);
    }

    private async Task RecoverRemovedBridge(Creature owner, Data data)
    {
        PendingTracker pending = data.pending.Root;
        if (pending.plays.Count == 0)
        {
            pending.plays.Clear();
            return;
        }

        if (!IsCombatActive(owner))
        {
            await DiscardPendingAtCombatEnd(owner, pending);
            return;
        }

        if (pending.recoveryInProgress)
        {
            // The enclosing Apply transaction is already looking for a replacement receiver.
            // Do not recurse while a hook removes a bridge during its own Apply.
            return;
        }

        pending.recoveryInProgress = true;
        bool clearPending = false;
        try
        {
            clearPending = !await PreservePendingPlays(owner, data);
        }
        finally
        {
            pending.Root.recoveryInProgress = false;
            if (clearPending)
            {
                pending.Root.plays.Clear();
            }
        }
    }

    private async Task<bool> PreservePendingPlays(Creature owner, Data data)
    {
        PendingTracker pending = data.pending.Root;
        if (pending.plays.Count == 0)
        {
            return true;
        }

        if (!IsCombatActive(owner))
        {
            await DiscardPendingAtCombatEnd(owner, pending);
            return false;
        }

        SerpentFormPower? replacement = FindNormalReceiver(owner);
        if (replacement != null)
        {
            MergePending(data, replacement.GetInternalData<Data>());
            return true;
        }

        SerpentFormPower? bridge = FindBridgeReceiver(owner);
        if (bridge != null)
        {
            MergePending(data, bridge.GetInternalData<Data>());
            return true;
        }

        bridge = (SerpentFormPower)ModelDb.Power<SerpentFormPower>().ToMutable();
        bridge.GrantExitEnergy = false;
        Data bridgeData = bridge.GetInternalData<Data>();
        bridgeData.completionBridge = true;
        bridgeData.pending = pending;

        try
        {
            await PowerCmd.Apply(
                new ThrowingPlayerChoiceContext(),
                bridge,
                owner,
                1m,
                owner,
                null,
                silent: true);
        }
        catch
        {
            // Do not swallow Apply or hook failures. During teardown, clear only the
            // combat-local tracker before rethrowing so it cannot cross the combat boundary.
            if (!IsCombatActive(owner))
            {
                await DiscardPendingAtCombatEnd(owner, pending);
            }

            throw;
        }

        if (!IsCombatActive(owner))
        {
            await DiscardPendingAtCombatEnd(owner, pending);
            return false;
        }

        // A hook may have removed the exact bridge after mounting, or may have created a
        // replacement while Apply was suspended. Re-home the same tracker if any receiver exists.
        if (FindAttachedReceiver(owner, pending) != null)
        {
            return true;
        }

        replacement = FindNormalReceiver(owner);
        if (replacement != null)
        {
            MergePending(data, replacement.GetInternalData<Data>());
            return true;
        }

        bridge = FindBridgeReceiver(owner);
        if (bridge != null)
        {
            MergePending(data, bridge.GetInternalData<Data>());
            return true;
        }

        // An active-combat rejection is a contract failure, not a successful handoff. Keep
        // the tracker and surface the error instead of silently dropping the only pending play.
        throw new InvalidOperationException("A power hook rejected the required Serpent completion bridge");
    }

    private static void MergePending(Data source, Data target)
    {
        source.pending.Root.MergeInto(target.pending.Root);
    }

    private static SerpentFormPower? FindNormalReceiver(Creature owner)
    {
        return owner.Powers
            .OfType<SerpentFormPower>()
            .FirstOrDefault(power => !power.GetInternalData<Data>().completionBridge);
    }

    private static SerpentFormPower? FindBridgeReceiver(Creature owner)
    {
        return owner.Powers
            .OfType<SerpentFormPower>()
            .FirstOrDefault(power => power.GetInternalData<Data>().completionBridge);
    }

    private static SerpentFormPower? FindAttachedReceiver(Creature owner, PendingTracker pending)
    {
        PendingTracker root = pending.Root;
        return owner.Powers
            .OfType<SerpentFormPower>()
            .FirstOrDefault(power => ReferenceEquals(power.GetInternalData<Data>().pending.Root, root));
    }

    private static async Task DiscardPendingAtCombatEnd(Creature owner, PendingTracker pending)
    {
        PendingTracker root = pending.Root;
        root.plays.Clear();
        SerpentFormPower[] bridges = owner.Powers
            .OfType<SerpentFormPower>()
            .Where(power => power.GetInternalData<Data>().completionBridge
                && ReferenceEquals(power.GetInternalData<Data>().pending.Root, root))
            .ToArray();
        foreach (SerpentFormPower bridge in bridges)
        {
            if (owner.Powers.Contains(bridge))
            {
                await PowerCmd.Remove(bridge);
            }
        }
    }

    private static bool IsCombatActive(Creature owner)
    {
        return owner.CombatState != null
            && CombatManager.Instance.IsInProgress
            && !CombatManager.Instance.IsOverOrEnding;
    }
}