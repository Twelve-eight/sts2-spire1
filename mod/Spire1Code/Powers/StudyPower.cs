using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Study. At the end of the owner's turn, shuffle Insight cards (one per stack) into the draw pile.
/// </summary>
public class StudyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Study",
            "#At the end of your turn, shuffle {Amount} Insight into your draw pile.",
            "At the end of your turn, shuffle an Insight into your draw pile.");

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Amount <= 0 || !participants.Contains(Owner))
            return;
        Flash();
        await CardPileCmd.AddToCombatAndPreview<Insight>(
            Owner,
            PileType.Draw,
            Amount,
            Owner.Player,
            CardPilePosition.Random);
    }
}
