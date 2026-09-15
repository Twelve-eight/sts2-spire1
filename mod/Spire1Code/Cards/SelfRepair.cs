using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class SelfRepair() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    /// <summary>
    /// 原版排除机制(jar 权威):本卡在 StS1 构造器写入 `CardTags.HEALING`
    /// (javap: getfield tags + getstatic AbstractCard$CardTags.HEALING),而
    /// `AbstractDungeon.returnTrulyRandomCardInCombat` 对 common/uncommon/rare
    /// 三池一律按 `hasTag(HEALING)` 过滤 -- 带该 tag 的卡不会在战斗中被随机生成
    /// (Discovery / 尼尔的法典 / 白噪声 等生成器的原版行为).
    /// 引擎侧对应物是 CardModel.CanBeGeneratedInCombat(默认 true),引擎自带 Feed
    /// 同样覆写为 false.故此处显式关闭.全库共 8 张 HEALING 卡:
    /// Alchemize / BandageUp / Bite / Feed / LessonLearned / Reaper / SelfRepair / Wish.
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SelfRepairPower>(7)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<SelfRepairPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<SelfRepairPower>().UpgradeValueBy(3m);
}
