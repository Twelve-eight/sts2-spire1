using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Alchemize (Rare Skill). Obtain a random potion (0 cost upgraded). Exhaust.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Alchemize() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    /// <summary>
    /// 与引擎自带 Alchemize 一致(engine-dllsrc/.../Cards/Alchemize.cs):基类默认
    /// CanBeGeneratedInCombat=true,而本卡在战斗中被随机生成会诱导"刷药水"最优解,
    /// 因此显式关闭.原版同卡同样不进入战斗内生成池.
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var potion = PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
