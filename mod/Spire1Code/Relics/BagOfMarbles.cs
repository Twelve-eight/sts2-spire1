using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Bag of Marbles (Common). At the start of each combat, apply 1 Vulnerable to ALL enemies.</summary>
public class BagOfMarbles : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Bag of Marbles",
            "#At the start of each combat, apply !VulnerablePower! Vulnerable to ALL enemies.",
            "A once popular toy in the City. Useful for throwing enemies off balance.");

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState.TurnNumber > 1)
            return;
        Flash();
        await PowerCmd.Apply<VulnerablePower>(choiceContext, combatState.HittableEnemies, DynamicVars.Power<VulnerablePower>().BaseValue, Owner.Creature, null);
    }
}
