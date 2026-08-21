using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Piercing Wail (Common). ALL enemies lose 6 Strength this turn, Exhaust (8 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class PiercingWail() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PiercingWailPower>(6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.Apply<PiercingWailPower>(choiceContext, this, play);

    protected override void OnUpgrade() => DynamicVars.Power<PiercingWailPower>().UpgradeValueBy(2m);
}
