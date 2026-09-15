using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Feed (Rare Attack). Deal 10 damage; if this kills the enemy, permanently raise your Max HP by 3 (12 damage / 4 HP upgraded). Exhaust.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Feed() : Spire1Card(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
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

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new MaxHpVar(3),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target ?? throw new ArgumentNullException(nameof(play.Target));

        // (2026-08-26 reverify fix) The predicate must match the engine's own Feed exactly:
        // All(p => p.ShouldOwnerDeathTriggerFatal()) - e.g. MinionPower overrides it to false
        // so minions are excluded from the max-HP reward. The previous !p.... inversion was the
        // same bug fixed in LessonLearned (3cfbcf1) but missed here.
        bool shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);

        if (shouldTriggerFatal && attack.Results.SelectMany(hit => hit).Any(r => r.WasTargetKilled))
            await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.MaxHp.UpgradeValueBy(1m);
    }
}
