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

/// <summary>StS1 Ironclad — Akabeko (Common). Your first Attack each combat deals 8 additional damage.</summary>
public class Akabeko : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(8m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Akabeko",
            "#Your first Attack each combat deals !VigorPower! additional damage.",
            "\"Muuu~\"");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState.TurnNumber > 1)
            return;
        Flash();
        await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.Power<VigorPower>().IntValue, Owner.Creature, null);
    }
}
