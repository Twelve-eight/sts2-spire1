using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.TimeWarpPower</c>. Hung on the Time Eater at combat
/// start; every card the player plays (no type filter — statuses and curses count too)
/// decrements the counter, and when it reaches 0 the player's turn is forcibly ended, the
/// counter resets to 12, and EVERY monster gains 2 Strength.
/// </summary>
public sealed class TimeWarpPower : CustomPowerModel
{
    public const int ResetAmount = 12;

    public const int TimeWarpStrength = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature.CombatState != base.Owner.CombatState)
            return;
        Flash();
        Amount -= 1;
        if (Amount <= 0)
        {
            Amount = ResetAmount;
            // Vanilla onAfterUseCard: StrengthPower(+2) on every monster in the fight.
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (Creature m in base.Owner.CombatState.Enemies)
            {
                await PowerCmd.Apply<StrengthPower>(ctx, m, TimeWarpStrength, base.Owner, null);
            }
            PlayerCmd.EndTurn(cardPlay.Player, canBackOut: false);
        }
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Time Warp",
            "Whenever you play a card, this enemy gains {Amount} Time.",
            "Whenever you play a card, this enemy gains {Amount} Time.");
}
