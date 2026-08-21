using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Silent — Masterful Stab (Uncommon Attack). Deal 12 damage. Costs 1 additional Energy for each time you lose HP
/// this combat (16 upgraded).
/// </summary>
[Pool(typeof(SilentCardPool))]
public class MasterfulStab() : Spire1Card(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    /// <summary>
    /// Runs after any creature's HP changes. Every card in a combat pile is a hook listener, so each copy of this
    /// card sees every time the player's HP actually dropped (blocked damage never changes HP, so it never counts)
    /// and taxes itself by 1 for the rest of the combat. Mirrors the mod's Blood for Blood pattern, inverted.
    /// </summary>
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner.Creature || delta >= 0 || CombatState == null)
            return Task.CompletedTask;
        EnergyCost.AddThisCombat(1);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a card enters combat with no previous pile (fresh generation mid-combat). Combat-scoped cost
    /// modifiers from earlier are gone, so catch this copy up on HP losses that happened before it existed by
    /// recounting the combat history.
    /// </summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this || IsClone)
            return Task.CompletedTask;
        int lost = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Count(e => e.Receiver == Owner.Creature && e.Result.UnblockedDamage > 0);
        if (lost > 0)
            EnergyCost.AddThisCombat(lost);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
