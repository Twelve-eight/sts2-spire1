using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher token - Miracle. Gain 1 Energy (2 upgraded). Retain and Exhaust.</summary>
public class Miracle() : Spire1Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
