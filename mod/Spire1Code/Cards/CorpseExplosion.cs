using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Corpse Explosion (Rare Skill). Apply 6 Poison (9 upgraded); when the enemy dies, deal damage equal to its Max HP to ALL enemies.</summary>
[Pool(typeof(SilentCardPool))]
public class CorpseExplosion() : Spire1Card(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(6), new PowerVar<CorpseExplosionPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
        await CommonActions.Apply<CorpseExplosionPower>(choiceContext, this, play);
    }

    protected override void OnUpgrade() => DynamicVars.Power<PoisonPower>().UpgradeValueBy(3m);
}
