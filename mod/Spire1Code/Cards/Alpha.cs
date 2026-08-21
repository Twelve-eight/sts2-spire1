using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Alpha (Rare Skill). Shuffle a Beta into your draw pile. Exhaust. Upgrade adds Innate.</summary>
[Pool(typeof(WatcherCardPool))]
public class Alpha() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CardPileCmd.AddToCombatAndPreview<Beta>(Owner.Creature, PileType.Draw, 1, Owner, CardPilePosition.Random);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
