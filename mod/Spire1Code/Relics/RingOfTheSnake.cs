using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Silent — Ring of the Snake (Starter). At the start of each combat, draw 2 additional cards.
/// ID = SPIRE1-RING_OF_THE_SNAKE. Sits in the Silent relic pool (overrides the base Spire1Relic pool).
/// </summary>
[Pool(typeof(SilentRelicPool))]
public class RingOfTheSnake : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Ring of the Snake",
            "#At the start of each combat, draw 2 additional cards.",
            "A fanged ring.");

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 2m, Owner);
    }
}
