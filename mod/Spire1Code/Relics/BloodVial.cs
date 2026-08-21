using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Blood Vial (Common). At the start of each combat, heal 2 HP.</summary>
public class BloodVial : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(2m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Blood Vial",
            "#At the start of each combat, heal !H! HP.",
            "A vial containing the blood of a pure and elder vampire.");

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState.TurnNumber > 1)
            return;
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue);
    }
}
