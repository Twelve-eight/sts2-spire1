using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Blood for Blood (Uncommon Attack). Deal 18 damage. Costs 1 less for each time you lost HP this combat.
/// (Upgraded: base cost 3, 22 damage.)
/// </summary>
public class BloodForBlood() : Spire1Card(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(18, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    /// <summary>
    /// Runs after any creature's HP changes for any reason (damage, heal, HP loss, ...). Every card in a combat
    /// pile is a hook listener, so each copy of this card sees every time the player's HP actually dropped
    /// (blocked damage never changes HP, so it never counts) and discounts itself by 1 for the rest of the combat.
    /// </summary>
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner.Creature || delta >= 0 || CombatState == null)
            return Task.CompletedTask;
        EnergyCost.AddThisCombat(-1, reduceOnly: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a card enters combat with no previous pile (fresh generation mid-combat, e.g. via Infernal
    /// Blade). Combat-scoped cost modifiers from earlier are gone, so catch this copy up on HP losses that
    /// happened before it existed by recounting the combat history (mirrors the game's Midnight card pattern).
    /// </summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this)
            return Task.CompletedTask;
        int lost = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Count(e => e.Receiver == Owner.Creature && e.Result.UnblockedDamage > 0);
        if (lost > 0)
            EnergyCost.AddThisCombat(-lost, reduceOnly: true);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
