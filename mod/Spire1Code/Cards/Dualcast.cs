using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Defect — Dualcast (Basic). Evoke your next Orb twice. (0 cost upgraded).</summary>
[Pool(typeof(DefectCardPool))]
public class Dualcast() : Spire1Card(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.Front;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.PlayerCombatState.OrbQueue.Orbs.Count > 0)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
            await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: false);
            await Cmd.CustomScaledWait(0.1f, 0.25f);
            await OrbCmd.EvokeNext(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
