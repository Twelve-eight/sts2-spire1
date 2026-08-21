using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Defect — Multi-Cast (Rare Skill). X-cost: Evoke your next Orb X times (X+1 upgraded).</summary>
[Pool(typeof(DefectCardPool))]
public class MultiCast() : Spire1Card(-1, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.All;
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int count = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        for (int i = 0; i < count; i++)
        {
            await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: i == count - 1);
            await Cmd.CustomScaledWait(0.25f, 0.25f);
        }
    }
}
