using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Deadly Poison (Common). Apply 5 Poison (7 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class DeadlyPoison() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.Apply<PoisonPower>(choiceContext, play.Target!, this);

    protected override void OnUpgrade() => DynamicVars.Power<PoisonPower>().UpgradeValueBy(2m);
}
