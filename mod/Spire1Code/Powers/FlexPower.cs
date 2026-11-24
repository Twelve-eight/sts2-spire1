using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>Standalone Flex's removable Strength-loss debuff when Spire1Events is not installed.</summary>
public class FlexPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc("Lose Strength", "At the end of your turn, lose {Amount} Strength.",
            "At the end of your turn, lose Strength.");

    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}
