using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Factories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spire1.Spire1Code.Powers;

public class CreativeAIPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Creative AI",
            "#At the start of your turn, add a random Power card into your hand.",
            "At the start of your turn, add a random Power card into your hand.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
            return;
        Flash();
        CardModel? card = CardFactory.GetDistinctForCombat(
            Owner.Player,
            Owner.Player.Character.CardPool
                .GetUnlockedCards(Owner.Player.UnlockState, Owner.Player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Type == CardType.Power),
            1,
            Owner.Player.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (card != null)
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner.Player);
    }
}
