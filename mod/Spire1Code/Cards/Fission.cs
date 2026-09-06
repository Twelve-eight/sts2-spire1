using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect — Fission (Rare Skill)。jar 逐字段对应（desktop-1.0.jar，
/// com/megacrit/cardcrawl/cards/blue/Fission.class + actions/defect/FissionAction.class）：
/// <list type="bullet">
/// <item>ctor：cost=iconst_0（0 费）、type=SKILL、color=BLUE、rarity=RARE、target=NONE；
///   magicNumber=1、baseMagicNumber=1（仅文案 !M! 显示用，无逻辑消费）、exhaust=true。</item>
/// <item>use()：addToBot(new FissionAction(upgraded))——效果全部在 FissionAction 内。</item>
/// <item>FissionAction.update()：n = player.filledOrbCount()（清球前快照，偏移 11-17）；
///   addToTop 依次压入 Draw(n)、GainEnergy(n)、[EvokeAllOrbs(升级)/RemoveAllOrbs(基础)]，
///   后进先出 → 实际执行序 = 清球 → 获得 n 能量 → 抽 n 张牌。</item>
/// <item>基础版 RemoveAllOrbsAction：循环 removeNextOrb()——AbstractPlayer.removeNextOrb 只把
///   首球换成 EmptyOrbSlot，不调用 onEvoke（对照 evokeOrb() 偏移 32-35 有 onEvoke），
///   即纯移除：不触发激发效果、不触发被动。</item>
/// <item>升级版 EvokeAllOrbsAction：逐球 EvokeOrbAction（触发激发效果）。</item>
/// <item>upgrade()：仅 upgradeName() + rawDescription 换 UPGRADE_DESCRIPTION
///   （Remove→Evoke 文案），无数值变化——升级语义是"移除改激发"，不是数值翻倍。</item>
/// </list>
/// 引擎命令映射（全命令 API，无手写 queue.Remove/RemoveInternal）：
/// <list type="bullet">
/// <item>激发（升级）：OrbCmd.EvokeNext 逐球（等价 EvokeOrbAction 队首激发；
///   引擎 Shatter/Quadcast 同款循环）。</item>
/// <item>纯移除（基础）：引擎无单球移除命令（RemoveAllOrbsAction 无直接等价物）；
///   命令级等价组合为 OrbCmd.RemoveSlots(capacity) + OrbCmd.AddSlots(capacity)——
///   RemoveCapacity 从队尾逐球出队且不触发任何球效果/钩子，AddSlots 原样恢复槽位
///   （净效果 = 清球保槽，动画上球环收拢再展开，与引擎 BulkUp 的 RemoveSlots 同源）。
///   出队即脱离 CombatState.IterateHookListeners 的活队列
///   （CombatState.cs:449 直接遍历 OrbQueue.Orbs），无需 RemoveInternal。</item>
/// <item>能量：PlayerCmd.GainEnergy(n)（= GainEnergyAction(n)）；
///   抽牌：CardPileCmd.Draw(n)（= DrawCardAction(n)）。</item>
/// </list>
/// 文案：loc description 用 SimpleLoc `-x-+y+` 升级交换标记表达 Remove→Evoke
/// （纯文案升级，OnUpgrade 无需重写，Catalyst 同款）。jar 文案尾部的 " NL Exhaust."
/// 不入 description——StS2 引擎自动渲染 Exhaust 关键字（SHATTER/APOTHEOSIS 等官方
/// description 均无 Exhaust 字样，本 mod 63 张 Exhaust 卡中 56 张同此约定）。
/// </summary>
[Pool(typeof(DefectCardPool))]
public class Fission() : Spire1Card(0, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>升级后为激发全部球：悬停卡时高亮全部球位；基础版是移除，不高亮。</summary>
    public override OrbEvokeType OrbEvokeType => IsUpgraded ? OrbEvokeType.All : OrbEvokeType.None;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        // jar：n = filledOrbCount() 在清球前快照（FissionAction.update 偏移 11-17）。
        int count = queue.Orbs.Count;
        if (count == 0)
            return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (IsUpgraded)
        {
            // EvokeAllOrbsAction：逐球激发（触发激发效果与 AfterOrbEvoked 钩子）。
            for (int i = 0; i < count; i++)
            {
                await OrbCmd.EvokeNext(choiceContext, Owner);
                if (i != count - 1)
                    await Cmd.CustomScaledWait(0.15f, 0.25f);
            }
        }
        else
        {
            // RemoveAllOrbsAction：纯移除，不激发。RemoveCapacity 连容量一起清且不触发
            // 任何球效果；随后 AddSlots 原样恢复槽位（净效果 = 清球保槽）。
            int capacity = queue.Capacity;
            OrbCmd.RemoveSlots(Owner, capacity);
            await OrbCmd.AddSlots(Owner, capacity);
        }

        // jar 执行序：清球 → GainEnergyAction(n) → DrawCardAction(n)。
        await PlayerCmd.GainEnergy(count, Owner);
        await CardPileCmd.Draw(choiceContext, count, Owner);
    }
}
