using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using System.Linq;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent - Well-Laid Plans. At the end of your turn, Retain up to Amount cards.</summary>
public class WellLaidPlansPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Well-Laid Plans",
            "#At the end of your turn, Retain up to {Amount:plural:card|cards}.",
            "At the end of your turn, Retain up to {Amount:plural:card|cards}.");

    /// <summary>
    /// Runs in turn-end phase one, while the hand is still intact (the hand flush that discards unretained cards
    /// happens in phase two and checks ShouldRetainThisTurn). Select up to Amount cards and give each a single-turn
    /// Retain so the flush keeps them in hand; the retain flag is cleared by the game's end-of-turn cleanup.
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;
        Flash();
        var prefs = new CardSelectorPrefs(new LocString("cards", "SPIRE1-WELL_LAID_PLANS.selectionScreenPrompt"), 0, Amount);
        var picked = (await CardSelectCmd.FromHand(choiceContext, Owner.Player, prefs, null, this)).ToList();
        foreach (var card in picked)
        {
            CardCmd.ApplySingleTurnRetain(card);
        }
    }
}
