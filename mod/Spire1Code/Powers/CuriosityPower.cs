using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.CuriosityPower</c>, as carried by the Awakened One:
/// whenever the player plays a Power card, this creature gains Amount Strength. (The shipped
/// StS2 <c>CuriousPower</c> is a card-cost reduction instead, so the StS1 monster-side behaviour
/// is reimplemented here.)
/// </summary>
public sealed class CuriosityPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Power)
            return;
        if (cardPlay.Player.Creature.CombatState != base.Owner.CombatState)
            return;
        if (base.Owner.IsDead || Amount <= 0)
            return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, Amount, base.Owner, null);
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Curiosity",
            "Whenever you play a Power card, this enemy gains {Amount} Strength.",
            "Whenever you play a Power card, this enemy gains {Amount} Strength.");
}
