using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Nightmare (Rare Skill). Choose a card; next turn add 3 copies of it into your hand (2 cost upgraded). Exhaust. Reuses the game's NightmarePower.</summary>
[Pool(typeof(SilentCardPool))]
public class Nightmare() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<NightmarePower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var selected = await CommonActions.SelectSingleCard(this, SelectionScreenPrompt, choiceContext, PileType.Hand);
        if (selected != null)
        {
            var power = await CommonActions.ApplySelf<NightmarePower>(choiceContext, this);
            power?.SetSelectedCard(selected);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
