using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Burst (Rare Skill). This turn, your next Skill is played twice (next 2 upgraded). Reuses the game's BurstPower.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Burst() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BurstPower>(1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<BurstPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<BurstPower>().UpgradeValueBy(1m);
}
