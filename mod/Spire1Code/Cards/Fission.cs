using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Fission() : Spire1Card(0, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int count = Owner.PlayerCombatState.OrbQueue.Orbs.Count;
        if (IsUpgraded)
        {
            for (int i = 0; i < count; i++)
                await OrbCmd.EvokeNext(choiceContext, Owner);
        }
        else
        {
            foreach (var orb in Owner.PlayerCombatState.OrbQueue.Orbs.ToList())
            {
                Owner.PlayerCombatState.OrbQueue.Remove(orb);
                orb.RemoveInternal();
            }
        }

        if (count > 0)
        {
            await PlayerCmd.GainEnergy(1, Owner);
            await CardPileCmd.Draw(choiceContext, count, Owner);
        }
    }
}
