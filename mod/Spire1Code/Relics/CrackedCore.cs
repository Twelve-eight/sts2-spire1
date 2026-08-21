using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Defect — Cracked Core (Starter). At the start of combat, Channel 1 Lightning.
/// ID = SPIRE1-CRACKED_CORE. Sits in the Defect relic pool (overrides the base Spire1Relic pool).
/// Logic mirrors the native StS2 CrackedCore exactly.
/// </summary>
[Pool(typeof(DefectRelicPool))]
public class CrackedCore : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Lightning", 1m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Cracked Core",
            "#At the start of combat, Channel 1 Lightning.",
            "The remains of the Defect's heart.");

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature) && Owner.PlayerCombatState.TurnNumber <= 1)
        {
            for (int i = 0; (decimal)i < DynamicVars["Lightning"].BaseValue; i++)
            {
                await OrbCmd.Channel<LightningOrb>(new BlockingPlayerChoiceContext(), Owner);
            }
        }
    }
}
