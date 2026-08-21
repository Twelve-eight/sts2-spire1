using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Ironclad - Fire Breathing. Whenever you draw a Status or Curse card, deal 6 damage to ALL enemies.</summary>
public class FireBreathingPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Fire Breathing",
            "#Whenever you draw a Status or Curse card, deal {Amount} damage to ALL enemies.",
            "Whenever you draw a Status or Curse card, deal damage to ALL enemies.");

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner || (card.Type != CardType.Status && card.Type != CardType.Curse))
            return;
        Flash();
        await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner, null, null);
    }
}
