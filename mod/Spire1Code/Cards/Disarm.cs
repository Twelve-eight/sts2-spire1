using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Disarm (Uncommon). Enemy loses 2 Strength, Exhaust (3 upgraded).</summary>
public class Disarm() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // (2026-08-27 fix) 渲染双负号修复：PowerVar<StrengthPower>(-2) 会把 !StrengthPower!
    // 渲染成 "-2"，而文案再写一遍"失去"→"失去 -2 点力量"。官方同类卡（PiercingWail）
    // 的惯例是正向 DynamicVar("StrengthLoss") + 文案"失去 X 力量"，应用时再取负。
    // 对齐官方惯例：var 存正数，OnPlay 手动 Apply 负值。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("StrengthLoss", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var loss = DynamicVars["StrengthLoss"].IntValue;
        await PowerCmd.Apply<StrengthPower>(choiceContext, play.Target!, -loss, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthLoss"].UpgradeValueBy(1m);
}
