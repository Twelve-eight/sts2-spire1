using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(Spire1LegacyPool))]
public class Claw() : Spire1Card(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private decimal _extraDamageFromClawPlays;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3, ValueProp.Move), new IntVar("Increase", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = CommonActions.CardAttack(this, play);
        // Claw.use @74-87 captures GashAction's amount before the queued attack executes.
        decimal increase = DynamicVars["Increase"].BaseValue;
        await attack.Execute(choiceContext);
        // GashAction.update @0-205: current card, then discard/draw/hand; no deduplication.
        IncreaseDamage(increase);
        var state = Owner.PlayerCombatState;
        IncreasePileDamage(state.DiscardPile, increase);
        IncreasePileDamage(state.DrawPile, increase);
        IncreasePileDamage(state.Hand, increase);
    }

    private void IncreaseDamage(decimal amount)
    {
        DynamicVars.Damage.BaseValue += amount;
        _extraDamageFromClawPlays += amount;
    }

    private static void IncreasePileDamage(CardPile pile, decimal amount)
    {
        var cards = pile.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] is Claw claw)
                claw.IncreaseDamage(amount);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += _extraDamageFromClawPlays;
    }
}
