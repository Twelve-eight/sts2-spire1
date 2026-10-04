using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.TimeWarpPower</c>. Hung on the Time Eater at combat
/// start; every card the player plays (no type filter - statuses and curses count too)
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
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: do not keep a frozen Time counter on the enemy; wind down.
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        Amount -= 1;
        if (Amount <= 0)
        {
            Amount = ResetAmount;
            // Vanilla onAfterUseCard: StrengthPower(+2) on every monster in the fight.
            // C12: gate the new Strength application, but always complete the turn-end reset below so
            // a disabled powers group cannot leave the counter stuck or the turn un-ended.
            if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
            {
                var ctx = new ThrowingPlayerChoiceContext();
                foreach (Creature m in base.Owner.CombatState.Enemies)
                {
                    await PowerCmd.Apply<StrengthPower>(ctx, m, TimeWarpStrength, base.Owner, null);
                }
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
