using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// The authored delayed consumer for Spire1 Nightmare. It keeps the selected snapshot and
/// the combat that created it together, so a listener removal or combat replacement cannot
/// make the continuation grant into a different live combat.
/// </summary>
public sealed class LegacyNightmarePower : Spire1Power
{
    private sealed class Data
    {
        internal CardModel? Selected;
        internal CombatId? Combat;
        internal ICombatState? State;
        internal Player? Player;
        internal bool Consumed;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Card")];

    protected override object InitInternalData() => new Data();

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        CombatManager manager = CombatManager.Instance;
        ICombatState? state = target.CombatState;
        CombatId? combat = manager.CurrentCombatId;
        if (state == null || !ReferenceEquals(state, manager.DebugOnlyGetState())
            || !manager.IsCurrentLiveCombat(combat) || manager.IsEnding)
        {
            throw new InvalidOperationException("Nightmare target is not in the current combat.");
        }

        Data data = GetInternalData<Data>();
        data.Combat = combat;
        data.State = state;
        data.Player = target.Player;
        return Task.CompletedTask;
    }

    public void SetSelectedCard(CardModel card)
    {
        AssertMutable();
        Data data = GetInternalData<Data>();
        if (data.Selected != null || data.Consumed)
        {
            throw new InvalidOperationException("Nightmare selection is already bound.");
        }
        if (data.State == null || data.Player == null || data.Combat == null
            || !Owner.Powers.Contains(this)
            || !ReferenceEquals(card.Owner, data.Player)
            || !ReferenceEquals(Owner.Player, data.Player)
            || !ReferenceEquals(Owner.CombatState, data.State)
            || !ReferenceEquals(data.State, CombatManager.Instance.DebugOnlyGetState())
            || !CombatManager.Instance.IsCurrentLiveCombat(data.Combat))
        {
            throw new InvalidOperationException("Nightmare selection belongs to a different player or current combat.");
        }

        CardModel snapshot = card.CreateClone();
        CardCmd.ClearAffliction(snapshot);
        snapshot.EndOfTurnCleanup();
        // StS1 resetAttributes resets the turn cost to the persistent cost. In StS2 this
        // additionally clears only WhenPlayed modifiers; EndOfCombat modifiers remain.
        if (snapshot.EnergyCost.AfterCardPlayedCleanup())
        {
            snapshot.InvokeEnergyCostChanged();
        }
        data.Selected = snapshot;
        ((StringVar)DynamicVars["Card"]).StringValue = snapshot.Title;
    }

    private bool IsCurrent(Data data, Player player, ICombatState state) =>
        ReferenceEquals(player, data.Player) && ReferenceEquals(Owner.Player, player)
        && player.IsActiveForHooks && player.Creature.IsAlive
        && ReferenceEquals(state, data.State) && ReferenceEquals(Owner.CombatState, data.State)
        && ReferenceEquals(data.State, CombatManager.Instance.DebugOnlyGetState())
        && Owner.Powers.Contains(this) && CombatManager.Instance.IsCurrentLiveCombat(data.Combat)
        && !CombatManager.Instance.IsEnding;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        Data data = GetInternalData<Data>();
        if (data.Consumed || !IsCurrent(data, player, combatState))
        {
            return;
        }

        if (data.Selected == null)
        {
            data.Consumed = true;
            MainFile.Logger.Warn("[Spire1] Nightmare delayed copy grant skipped: selected snapshot is missing");
            await PowerCmd.Remove(this);
            return;
        }

        // Claim the one-shot before the first clone or await. A partial grant is terminal;
        // already-added cards remain committed and a later dispatch cannot restart it.
        data.Consumed = true;
        Exception? primary = null;
        try
        {
            CardModel selected = data.Selected
                ?? throw new InvalidOperationException("Nightmare consumed before selection completed.");
            int amount = Amount;
            for (int i = 0; i < amount; i++)
            {
                if (!IsCurrent(data, player, combatState))
                {
                    return;
                }

                CardModel copy = selected.CreateClone();
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);

                if (!IsCurrent(data, player, combatState))
                {
                    return;
                }
            }
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            // Remove only this exact instance while its original combat still owns it. A stale
            // continuation must not remove a new combat's or another selection's power.
            try
            {
                if (Owner.Powers.Contains(this) && ReferenceEquals(Owner.Player, data.Player)
                    && ReferenceEquals(Owner.CombatState, data.State)
                    && ReferenceEquals(data.State, CombatManager.Instance.DebugOnlyGetState())
                    && CombatManager.Instance.IsCurrentLiveCombat(data.Combat))
                {
                    await PowerCmd.Remove(this);
                }
            }
            catch (Exception cleanupFailure) when (primary != null)
            {
                const string baseKey = "Spire1.Nightmare.CleanupFailure";
                string key = baseKey;
                int suffix = 2;
                while (primary.Data.Contains(key))
                {
                    key = baseKey + "." + suffix++;
                }
                primary.Data.Add(key, cleanupFailure);
            }
        }
    }
}
