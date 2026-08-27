using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using BaseLib.Utils;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher token - Beta. Shuffle an Omega into your draw pile. Exhaust.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Beta() : Spire1Card(2, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CardPileCmd.AddToCombatAndPreview<Omega>(Owner.Creature, PileType.Draw, 1, Owner, CardPilePosition.Random);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
