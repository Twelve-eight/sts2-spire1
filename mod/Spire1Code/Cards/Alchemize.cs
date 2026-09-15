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
    /// 原版排除机制(jar 权威):`green.Alchemize` 构造器写入 `CardTags.HEALING`
    /// (javap: getfield tags + getstatic AbstractCard$CardTags.HEALING + ArrayList.add),
    /// 而 StS1 的战斗内随机生成(`AbstractDungeon.returnTrulyRandomCardInCombat`)对
    /// common/uncommon/rare 三池一律 `hasTag(HEALING)` 过滤 —— 带该 tag 的卡不会被
    /// 战斗内随机生成.引擎侧对应物是 CardModel.CanBeGeneratedInCombat(默认 true),
    /// 引擎自带 Alchemize 亦覆写为 false.两者结论一致,故此处显式关闭.
    /// 注:本卡移植的是 green(Silent)Alchemize;blue(Defect)同名类不带该 tag.
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
