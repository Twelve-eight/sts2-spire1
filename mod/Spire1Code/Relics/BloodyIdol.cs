using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 — Bloody Idol (Event). Whenever you gain Gold, heal 5 HP.</summary>
public class BloodyIdol : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>StS1 <c>BloodyIdol.HEAL_AMOUNT = 5</c>.</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Bloody Idol",
            "#Whenever you gain *Gold*, heal !H! HP.",
            "The idol now weeps a constant stream of blood.");

    // StS1: onGainGold() { flash(); addToTop(new RelicAboveCreatureAction(player, this)); player.heal(5, true); }
    // AfterGoldGained (AbstractModel.cs:767) is the matching StS2 notification hook: async, so a heal can
    // simply be awaited, and it runs only after the gold has landed (PlayerCmd.cs:168-169), past the
    // `!(amount > 0m)` early return at PlayerCmd.cs:146. StS1's AbstractPlayer.gainGold behaves
    // identically — its bytecode returns before the relic loop both when amount <= 0 and when the player
    // holds Ectoplasm — so a zero-gold gain, and an Ectoplasm run, heal nothing in either game (StS2
    // reaches the same outcome because Ectoplasm.cs:18 zeroes the amount in ModifyGoldGained).
    // Shipped precedent for this exact shape is DragonFruit.cs:22.
    // ModifyGoldGained is not used: it is a synchronous modifier chain (AbstractModel.cs:1635) with no
    // place to await a heal, and it runs even when the gain is then discarded.
    public override async Task AfterGoldGained(Player player)
    {
        if (player != Owner)
            return;

        // CreatureCmd.Heal does not skip dead creatures — it plays a revive animation instead
        // (CreatureCmd.cs:744,772-775) — so the guard is load-bearing, as in BurningBlood.
        if (Owner.Creature.IsDead)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}
