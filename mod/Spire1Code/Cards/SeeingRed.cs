using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Seeing Red (Uncommon). Gain 2 Energy. Exhaust.</summary>
public class SeeingRed() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await PlayerCmd.GainEnergy(2, Owner);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
