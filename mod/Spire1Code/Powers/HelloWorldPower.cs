using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using System.Linq;

namespace Spire1.Spire1Code.Powers;

public class HelloWorldPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Hello World",
            "#At the start of your turn, add {Amount} random Common card(s) into your hand.",
            "#At the start of your turn, add random Common cards into your hand.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
            return;
        Flash();
        var cards = CardFactory.GetDistinctForCombat(
            Owner.Player,
            Owner.Player.Character.CardPool
                .GetUnlockedCards(Owner.Player.UnlockState, Owner.Player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == CardRarity.Common),
            Amount,
            Owner.Player.RunState.Rng.CombatCardGeneration);
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner.Player);
    }
}
