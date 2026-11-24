using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using Spire1.Spire1Code.Potions;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// One row of the StS1 Silent potion table: the legacy id (the jar's potion ID string,
/// which is also what the reviewed oracle uses), the legacy rarity, and the real entity that
/// is granted for that row. Ancient Potion, EssenceOfSteel, CultistPotion and SmokeBomb all
/// resolve to the mod's own production potions; there is no missing/nullable row, no reroll,
/// no substitute and no fake victory.
/// </summary>
internal readonly record struct LegacyAlchemizeEntry(
    string Id, PotionRarity Rarity, Type Entity);

/// <summary>
/// StS1 Silent potion domain and draw sequence used by <see cref="Alchemize"/>.
///
/// Authority (jar bytecode, not the wiki):
/// <list type="bullet">
/// <item><c>cards/Alchemize.txt::use @0-15</c> calls <c>AbstractDungeon.returnRandomPotion(true)</c>
/// and hands the result to <c>ObtainPotionAction</c>: the potion is rolled at play time from
/// the potion RNG and only then offered to the obtain command.</item>
/// <item><c>dungeons/AbstractDungeon.txt::returnRandomPotion(Z) @0-51</c> rolls
/// <c>potionRng.random(0,99)</c>; rolls below <c>PotionHelper.POTION_COMMON_CHANCE</c> (65) are
/// Common, below 65+<c>POTION_UNCOMMON_CHANCE</c> (25, i.e. 90) Uncommon, the rest Rare
/// (<c>helpers/PotionHelper.txt:1113-1116</c>).</item>
/// <item><c>returnRandomPotion(rarity, boolean) @0-40</c> samples once, then always enters the
/// retry loop; it returns only a draw whose rarity matches and whose ID is not
/// <c>Fruit Juice</c>. That is the same exclusion the reviewed fidelity probe proved: Fruit
/// Juice is skipped even when its rarity matches.</item>
/// <item>The table order is <c>PotionHelper.getPotions(class, false)</c>: the three Silent
/// potions first (<c>helpers/PotionHelper.txt:92-102</c>), then the shared list
/// (<c>:180-299</c>, Block Potion through EntropicBrew).</item>
/// </list>
///
/// The host's own <c>PotionFactory.CreateRandomPotionInCombat</c> rolls a different rarity
/// distribution (float thresholds 0.10/0.35) and draws from the character/shared potion pools,
/// so it cannot reproduce this table or these weights; this class is the legacy seam.
/// The RNG stream is the host's <c>RunState.Rng.CombatPotionGeneration</c>, the stream the
/// engine documents for combat potion generation (RunRngSet.cs:54-57).
/// </summary>
internal static class LegacyAlchemizePool
{
    private const string FruitJuiceId = "Fruit Juice";

    /// <summary>Exact legacy Silent order; index is part of the draw contract.</summary>
    internal static IReadOnlyList<LegacyAlchemizeEntry> Silent { get; } = Array.AsReadOnly(new LegacyAlchemizeEntry[]
    {
        Host<PoisonPotion>("Poison Potion", PotionRarity.Common),
        Host<CunningPotion>("CunningPotion", PotionRarity.Uncommon),
        Host<GhostInAJar>("GhostInAJar", PotionRarity.Rare),
        Host<BlockPotion>("Block Potion", PotionRarity.Common),
        Host<DexterityPotion>("Dexterity Potion", PotionRarity.Common),
        Host<EnergyPotion>("Energy Potion", PotionRarity.Common),
        Own<ExplosivePotion>("Explosive Potion", PotionRarity.Common),
        Host<FirePotion>("Fire Potion", PotionRarity.Common),
        Host<StrengthPotion>("Strength Potion", PotionRarity.Common),
        Host<SwiftPotion>("Swift Potion", PotionRarity.Common),
        Host<WeakPotion>("Weak Potion", PotionRarity.Common),
        Own<FearPotion>("FearPotion", PotionRarity.Common),
        Host<AttackPotion>("AttackPotion", PotionRarity.Common),
        Host<SkillPotion>("SkillPotion", PotionRarity.Common),
        Host<PowerPotion>("PowerPotion", PotionRarity.Common),
        Host<ColorlessPotion>("ColorlessPotion", PotionRarity.Common),
        Host<FlexPotion>("SteroidPotion", PotionRarity.Common),
        Host<SpeedPotion>("SpeedPotion", PotionRarity.Common),
        Host<BlessingOfTheForge>("BlessingOfTheForge", PotionRarity.Common),
        Host<RegenPotion>("Regen Potion", PotionRarity.Uncommon),
        Own<AncientPotion>("Ancient Potion", PotionRarity.Uncommon),
        Host<LiquidBronze>("LiquidBronze", PotionRarity.Uncommon),
        Host<GamblersBrew>("GamblersBrew", PotionRarity.Uncommon),
        Own<EssenceOfSteel>("EssenceOfSteel", PotionRarity.Uncommon),
        Host<Duplicator>("DuplicationPotion", PotionRarity.Uncommon),
        Host<DistilledChaos>("DistilledChaos", PotionRarity.Uncommon),
        Host<LiquidMemories>("LiquidMemories", PotionRarity.Uncommon),
        Own<CultistPotion>("CultistPotion", PotionRarity.Rare),
        Host<FruitJuice>("Fruit Juice", PotionRarity.Rare),
        Host<SneckoOil>("SneckoOil", PotionRarity.Rare),
        Host<FairyInABottle>("FairyPotion", PotionRarity.Rare),
        Own<SmokeBomb>("SmokeBomb", PotionRarity.Rare),
        Host<EntropicBrew>("EntropicBrew", PotionRarity.Rare),
    });

    private static LegacyAlchemizeEntry Host<T>(string id, PotionRarity rarity) where T : PotionModel
        => new(id, rarity, typeof(T));

    private static LegacyAlchemizeEntry Own<T>(string id, PotionRarity rarity) where T : Spire1Potion
        => new(id, rarity, typeof(T));

    /// <summary>
    /// Draws exactly like <c>AbstractDungeon.returnRandomPotion(true)</c>: one rarity roll,
    /// one initial table draw that can never be returned, then retries until a non-Fruit-Juice
    /// row of the target rarity is drawn. No allocation per play.
    /// </summary>
    internal static LegacyAlchemizeEntry Select(Rng rng)
    {
        int roll = rng.NextInt(0, 100);
        PotionRarity rarity = roll < 65
            ? PotionRarity.Common
            : roll < 90
                ? PotionRarity.Uncommon
                : PotionRarity.Rare;

        // The legacy helper samples the table once before its forced retry loop.
        _ = Silent[rng.NextInt(Silent.Count)];

        LegacyAlchemizeEntry item;
        do
        {
            item = Silent[rng.NextInt(Silent.Count)];
        }
        while (item.Rarity != rarity || item.Id == FruitJuiceId);

        return item;
    }

    /// <summary>
    /// Resolves the selected row to a mutable host potion. Every row now names a real entity; a
    /// missing ModelDb entry, a non-canonical instance or a failed clone throws instead of turning
    /// into a silent no-grant. The canonical instance is verified before cloning so a stale or
    /// incorrect ModelDb entry can never masquerade as the selected potion.
    /// </summary>
    internal static PotionModel Resolve(LegacyAlchemizeEntry entry)
    {
        PotionModel canonical = ModelDb.GetById<PotionModel>(ModelDb.GetId(entry.Entity));
        canonical.AssertCanonical();
        return canonical.ToMutable();
    }
}
