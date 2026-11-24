using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Ironclad - Mark of Pain (Boss). Gain 1 Energy. At the start of combat, shuffle 2 Wounds into your draw pile.
/// </summary>
public class MarkOfPain : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    // StS1 Mark of Pain is a permanent +1 max Energy, not a per-turn energy gain.
    // Same shape as the shipped Sozu (Sozu.cs:22-28): only the owner's amount changes.
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner)
        {
            return amount;
        }
        return amount + (decimal)DynamicVars.Energy.IntValue;
    }

    // StS1 atBattleStart queues Wounds after the opening draw. The host awaits its
    // initial draw before AfterPlayerTurnStart (CombatManager.cs:946-948).
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState?.TurnNumber != 1)
            return;

        // Cards content group gate (C18 r2, 2026-10-03): Wound is a Spire1 status card created directly by
        // AddToCombatAndPreview, which bypasses the pool filters that carry the cards gate. Fail closed before
        // any card is constructed, following the relic grants in Enchiridion.cs:40-44 and Toolbox.cs:25-29;
        // the max-Energy bonus above is not card content and is unaffected.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Mark of Pain Wound grant skipped: cards content group is off");
            return;
        }

        Flash();
        await CardPileCmd.AddToCombatAndPreview<Wound>(Owner.Creature, PileType.Draw, 2, Owner, CardPilePosition.Random);
    }
}
