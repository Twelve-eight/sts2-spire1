using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect - Fission (Rare Skill): 移除全部充能球，每移除一球获得 1 层集中与 1 点能量（升级 2）。
/// 注意：移除不触发球的激发效果（一代语义）；走 EvokeOrbAnim 仅做视觉弹出，
/// 队列模型同步 Remove+RemoveInternal —— 兼顾"真移除+有动画+联机确定"。
/// </summary>
[Pool(typeof(DefectCardPool))]
public class Fission() : Spire1Card(0, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    private int PerOrb => IsUpgraded ? 2 : 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        int count = queue.Orbs.Count;
        var manager = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.OrbManager;

        foreach (var orb in queue.Orbs.ToList())
        {
            choiceContext.PushModel(orb);
            queue.Remove(orb);
            manager?.EvokeOrbAnim(orb); // 纯视觉：弹出节点并重排版
            orb.RemoveInternal();
            choiceContext.PopModel(orb);
        }

        if (count > 0)
        {
            int focus = count * PerOrb;
            await PowerCmd.Apply<FocusPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, focus, Owner.Creature, this);
            await PlayerCmd.GainEnergy(count * PerOrb, Owner);
        }
    }
}
