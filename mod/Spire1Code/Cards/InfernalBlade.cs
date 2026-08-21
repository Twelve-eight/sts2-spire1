using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Infernal Blade (Uncommon Skill). Add a random Attack to your hand; it costs 0 this turn. Exhaust (0 cost upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class InfernalBlade() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = CommonActions.GenerateSingleCard(this, c => c.Type == CardType.Attack);
        if (attack != null)
        {
            var result = await CardPileCmd.AddGeneratedCardToCombat(attack, PileType.Hand, Owner);
            result.cardAdded.EnergyCost.SetThisTurn(0);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
