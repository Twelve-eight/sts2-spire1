using BaseLib.Utils;
using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Recursion() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var orbs = Owner.PlayerCombatState.OrbQueue.Orbs;
        if (orbs.Count == 0)
            return;

        // v0.111 新增 GlassOrb 等非经典球种：未知类型只 evoke、不再同型重铸，
        // 绝不抛异常打断出牌（P1SMOKE3-r3 实测 "Unsupported orb type" 崩局）。
        OrbModel? replacement = orbs[0] switch
        {
            LightningOrb => ModelDb.Orb<LightningOrb>().ToMutable(),
            FrostOrb => ModelDb.Orb<FrostOrb>().ToMutable(),
            DarkOrb => ModelDb.Orb<DarkOrb>().ToMutable(),
            PlasmaOrb => ModelDb.Orb<PlasmaOrb>().ToMutable(),
            GlassOrb => ModelDb.Orb<GlassOrb>().ToMutable(),
            _ => null
        };
        await OrbCmd.EvokeNext(choiceContext, Owner);
        if (replacement != null)
            await OrbCmd.Channel(choiceContext, replacement, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
