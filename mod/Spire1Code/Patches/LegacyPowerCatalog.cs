using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using HostCharacters = MegaCrit.Sts2.Core.Models.Characters;
using Sts2Cards = MegaCrit.Sts2.Core.Models.Cards;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// One row of the historical StS1 Power card table: the legacy class key (identity of the
/// constructor), the exact legacy cardID (the lookup key, spaces and case preserved), the
/// legacy rarity, the HEALING marker, the compile-time entity when this mod can reference it,
/// and the exact runtime type name.
/// </summary>
internal sealed record LegacyPowerSpec(
    string LegacyClassKey,
    string LegacyCardId,
    CardRarity Rarity,
    bool Healing,
    Type? Entity,
    string CurrentType);

/// <summary>One base-character source profile: exact owner types plus the complete Power inventory.</summary>
internal sealed record LegacyPowerProfile(
    string Character,
    IReadOnlyList<string> ExactOwnerTypes,
    IReadOnlyList<LegacyPowerSpec> Inventory,
    string Evidence);

/// <summary>
/// Complete StS1 combat Power source domain for White Noise, resolved to actual canonical
/// <see cref="CardModel"/> instances. Four base-character profiles, 51 declared rows:
/// IRONCLAD 14, SILENT 11, DEFECT 14 (Self Repair is HEALING), WATCHER 12 (third-party
/// Watcher 0.10.11 cards, resolved by type name because this mod must not reference that
/// assembly at compile time).
///
/// <para>
/// Authority (jar bytecode, not the wiki):
/// <c>.tmp/sts1-jar-dump/cards/WhiteNoise.txt::use @0-28</c> draws
/// <c>AbstractDungeon.returnTrulyRandomCardInCombat(CardType.POWER)</c>, copies it, calls
/// <c>AbstractCard.setCostForTurn(0)</c> and queues <c>MakeTempCardInHandAction(card, true)</c>.
/// <c>.tmp/sts1-jar-dump/dungeons/AbstractDungeon.txt:2793-2878</c> builds that list from
/// <c>srcCommonCardPool</c> + <c>srcUncommonCardPool</c> + <c>srcRareCardPool</c>, keeps rows
/// whose <c>type</c> equals POWER, skips rows carrying <c>CardTags.HEALING</c>, and samples
/// with <c>cardRandomRng</c>. <c>AbstractDungeon.txt:3361-3598</c> (initializeCardPools,
/// Diverse mod off) fills those src pools from <c>player.getCardPool(...)</c>. A character's
/// getCardPool adds its own colour first and appends other colours only when the "Red Cards" /
/// "Green Cards" / "Purple Cards" modifiers are enabled (Defect.javap.txt:228-248 @0-41:
/// addBlue, then the three modifier branches); the "Diverse" modifier bypasses getCardPool and
/// adds every colour. Normal runs are therefore the OWNED character's single colour, never
/// another character's pool. The four frozen profiles below are exactly that per-color Power
/// inventory. (<c>extra/Defect.txt</c> is a declaration-only dump and is not used as
/// implementation proof.)
/// </para>
///
/// <para>
/// Declared identity comes from the reviewed frozen sources
/// (<c>R13/spire1-official/whitenoise-fidelity-probe/LegacyPowerCatalog.cs</c> Defect 14 and
/// <c>R13/spire1-official/whitenoise-character-profiles/LegacyCharacterPowerProfiles.cs</c>
/// Ironclad 14 / Silent 11 / Watcher 12, contract <c>candidateCounts</c> 14/11/12).
/// One intentional production deviation: the DEFECT EchoForm row now maps to
/// <c>Spire1.Spire1Code.Cards.EchoForm</c> after the 2026-11-16 fidelity cutover. The frozen probe
/// declared the host <c>MegaCrit.Sts2.Core.Models.Cards.EchoForm</c> because no own class existed;
/// LegacyClassKey, LegacyCardId, Rarity and Healing are unchanged.
/// Runtime row certification boundary: <c>whitenoise-canonical-consumer run-wncanonical-06</c>
/// certified exactly IRONCLAD 14 + SILENT 11 + WATCHER 12 = 37 rows (its Program.cs:78-83 runs
/// only those three profiles; gate certifiedRows=37, judgedOn=full; the joined tsv carries the
/// 25 Spire1 rows). The DEFECT 14 rows are frozen declarations only - their per-row runtime
/// resolution was NOT run. Electrodynamics has one restricted registration-id receipt
/// (<c>CARD.SPIRE1-ELECTRODYNAMICS</c>, electro-zero-offset/positive.json); the other 8 mod
/// Defect rows have no runtime per-row evidence. Claiming more than STATIC_ONLY for the DEFECT
/// profile requires a DEFECT profile resolution gate added to the consumer.
/// </para>
///
/// <para>
/// Unlock: StS1's src pools only contain unlocked cards — CardLibrary.addRed/Green/Blue/PurpleCards
/// filter <c>rarity != BASIC &amp;&amp; (!UnlockTracker.isCardLocked(cardID) ||
/// Settings.treatEverythingAsUnlocked())</c> (<c>CardLibrary.txt:2903-3078</c>) — but this mod ships no
/// legacy UnlockTracker and authors every declared legacy card unconditionally (the equivalent of
/// <c>Settings.treatEverythingAsUnlocked</c>). The declared non-HEALING inventory is therefore
/// the offered set, with no live per-play unlock query. The owner's live <c>CardPool.GetUnlockedCards</c>
/// is deliberately NOT used
/// here: this mod's reward-retirement policy (cards re-implemented by StS2 are parked in
/// Spire1LegacyPool) and the host's own Defect/Ironclad/Silent pools would silently shrink the
/// legacy domain. Any canonical row that cannot be resolved is a HOLD and refuses generation;
/// rows are never dropped silently.
/// </para>
///
/// <para>
/// Scope boundary: the StS1 "Diverse" modifier adds every colour to the src pools, and the
/// "Red Cards" / "Green Cards" / "Purple Cards" modifiers append their colour inside the
/// character's own getCardPool (Defect.javap.txt:228-248 @0-41). Both kinds of mixed-colour
/// source pool stay unsupported (the frozen consumer contract lists them under
/// unsupportedUntilExplicitlyResolved); White Noise always uses the owned character's
/// single-colour profile.
/// </para>
/// </summary>
internal static class LegacyPowerCatalog
{
    /// <summary>StS1 Defect (BLUE) Power inventory; Self Repair carries the legacy HEALING tag.</summary>
    internal static readonly LegacyPowerProfile Defect = new(
        "DEFECT",
        ["MegaCrit.Sts2.Core.Models.Characters.Defect", "Spire1.Spire1Code.Character.Defect"],
        [
            Row<BiasedCognition>("BiasedCognition", "Biased Cognition", CardRarity.Rare),
            Row<Sts2Cards.Buffer>("Buffer", "Buffer", CardRarity.Rare),
            Row<Sts2Cards.Capacitor>("Capacitor", "Capacitor", CardRarity.Uncommon),
            Row<CreativeAI>("CreativeAI", "Creative AI", CardRarity.Rare),
            Row<Defragment>("Defragment", "Defragment", CardRarity.Uncommon),
            Row<EchoForm>("EchoForm", "Echo Form", CardRarity.Rare),
            Row<Electrodynamics>("Electrodynamics", "Electrodynamics", CardRarity.Rare),
            Row<Heatsinks>("Heatsinks", "Heatsinks", CardRarity.Uncommon),
            Row<HelloWorld>("HelloWorld", "Hello World", CardRarity.Uncommon),
            Row<Sts2Cards.Loop>("Loop", "Loop", CardRarity.Uncommon),
            Row<Sts2Cards.MachineLearning>("MachineLearning", "Machine Learning", CardRarity.Rare),
            Row<SelfRepair>("SelfRepair", "Self Repair", CardRarity.Uncommon, healing: true),
            Row<StaticDischarge>("StaticDischarge", "Static Discharge", CardRarity.Uncommon),
            Row<Storm>("Storm", "Storm", CardRarity.Uncommon),
        ],
        "pool-evidence.json:complete-75-blue-card-constructor-census");

    /// <summary>StS1 Ironclad (RED) Power inventory.</summary>
    internal static readonly LegacyPowerProfile Ironclad = new(
        "IRONCLAD",
        ["MegaCrit.Sts2.Core.Models.Characters.Ironclad", "Spire1.Spire1Code.Character.Ironclad"],
        [
            Row<Barricade>("Barricade", "Barricade", CardRarity.Rare),
            Row<Berserk>("Berserk", "Berserk", CardRarity.Rare),
            Row<Brutality>("Brutality", "Brutality", CardRarity.Rare),
            Row<Combust>("Combust", "Combust", CardRarity.Uncommon),
            Row<Corruption>("Corruption", "Corruption", CardRarity.Rare),
            Row<DarkEmbrace>("DarkEmbrace", "Dark Embrace", CardRarity.Uncommon),
            Row<DemonForm>("DemonForm", "Demon Form", CardRarity.Rare),
            Row<Evolve>("Evolve", "Evolve", CardRarity.Uncommon),
            Row<FeelNoPain>("FeelNoPain", "Feel No Pain", CardRarity.Uncommon),
            Row<FireBreathing>("FireBreathing", "Fire Breathing", CardRarity.Uncommon),
            Row<Inflame>("Inflame", "Inflame", CardRarity.Uncommon),
            Row<Juggernaut>("Juggernaut", "Juggernaut", CardRarity.Rare),
            Row<Metallicize>("Metallicize", "Metallicize", CardRarity.Uncommon),
            Row<Rupture>("Rupture", "Rupture", CardRarity.Uncommon),
        ],
        "legacy-constructors.json:addRedCards();map-evidence.json:IRONCLAD");

    /// <summary>StS1 Silent (GREEN) Power inventory.</summary>
    internal static readonly LegacyPowerProfile Silent = new(
        "SILENT",
        ["MegaCrit.Sts2.Core.Models.Characters.Silent", "Spire1.Spire1Code.Character.Silent"],
        [
            Row<Accuracy>("Accuracy", "Accuracy", CardRarity.Uncommon),
            Row<AfterImage>("AfterImage", "After Image", CardRarity.Rare),
            Row<AThousandCuts>("AThousandCuts", "A Thousand Cuts", CardRarity.Rare),
            Row<Caltrops>("Caltrops", "Caltrops", CardRarity.Uncommon),
            Row<Envenom>("Envenom", "Envenom", CardRarity.Rare),
            Row<Footwork>("Footwork", "Footwork", CardRarity.Uncommon),
            Row<InfiniteBlades>("InfiniteBlades", "Infinite Blades", CardRarity.Uncommon),
            Row<NoxiousFumes>("NoxiousFumes", "Noxious Fumes", CardRarity.Uncommon),
            Row<ToolsOfTheTrade>("ToolsOfTheTrade", "Tools of the Trade", CardRarity.Rare),
            Row<WellLaidPlans>("WellLaidPlans", "Well Laid Plans", CardRarity.Uncommon),
            Row<WraithForm>("WraithForm", "Wraith Form v2", CardRarity.Rare),
        ],
        "legacy-constructors.json:addGreenCards();map-evidence.json:SILENT");

    /// <summary>
    /// StS1 Watcher (PURPLE) Power inventory. The cards live in the third-party Watcher 0.10.11
    /// assembly, which this mod does not reference; they resolve by exact type name and are a HOLD
    /// when that assembly is absent. Watcher 0.10.11 was certified 12/12 MAPPED in full mode.
    /// </summary>
    internal static readonly LegacyPowerProfile Watcher = new(
        "WATCHER",
        ["WatcherMod.Watcher"],
        [
            External("BattleHymn", "BattleHymn", CardRarity.Uncommon, "WatcherMod.WatcherBattleHymn"),
            External("DevaForm", "DevaForm", CardRarity.Rare, "WatcherMod.WatcherDevaForm"),
            External("Devotion", "Devotion", CardRarity.Rare, "WatcherMod.WatcherDevotion"),
            External("Establishment", "Establishment", CardRarity.Rare, "WatcherMod.WatcherEstablishment"),
            External("Fasting", "Fasting2", CardRarity.Uncommon, "WatcherMod.WatcherFasting2"),
            External("Foresight", "Wireheading", CardRarity.Uncommon, "WatcherMod.WatcherForesight"),
            External("LikeWater", "LikeWater", CardRarity.Uncommon, "WatcherMod.WatcherLikeWater"),
            External("MasterReality", "MasterReality", CardRarity.Rare, "WatcherMod.WatcherMasterReality"),
            External("MentalFortress", "MentalFortress", CardRarity.Uncommon, "WatcherMod.WatcherMentalFortress"),
            External("Nirvana", "Nirvana", CardRarity.Uncommon, "WatcherMod.WatcherNirvana"),
            External("Rushdown", "Adaptation", CardRarity.Uncommon, "WatcherMod.WatcherRushdown"),
            External("Study", "Study", CardRarity.Uncommon, "WatcherMod.WatcherStudy"),
        ],
        "legacy-constructors.json:addPurpleCards();map-evidence.json:WATCHER;Watcher.dll 0.10.11 runtime identity certified by whitenoise-canonical-consumer run-wncanonical-06");

    internal static readonly IReadOnlyList<LegacyPowerProfile> Profiles = [Ironclad, Silent, Defect, Watcher];

    private static readonly Dictionary<string, Lazy<Snapshot>> Resolved = BuildResolved();

    private readonly record struct Snapshot(CardModel[] Offered, string[] Holds);

    private static Dictionary<string, Lazy<Snapshot>> BuildResolved()
    {
        Dictionary<string, Lazy<Snapshot>> resolved = new(Profiles.Count, StringComparer.Ordinal);
        foreach (LegacyPowerProfile profile in Profiles)
        {
            resolved.Add(profile.Character,
                new Lazy<Snapshot>(() => Resolve(profile), LazyThreadSafetyMode.ExecutionAndPublication));
        }
        return resolved;
    }

    /// <summary>
    /// Exact owner-profile selection, mirroring the certified consumer rule: the owner's runtime
    /// type must be one of the profile's exact types. Another character's certified source pool
    /// is never borrowed, and an unprofiled character (any character without an StS1 color
    /// inventory) is a HOLD instead of an implicit host-pool fallback.
    /// </summary>
    internal static LegacyPowerProfile? ForOwner(CharacterModel? owner)
    {
        if (owner is null)
        {
            return null;
        }
        string ownerType = owner.GetType().FullName ?? string.Empty;
        foreach (LegacyPowerProfile profile in Profiles)
        {
            if (profile.ExactOwnerTypes.Contains(ownerType, StringComparer.Ordinal))
            {
                return profile;
            }
        }
        return null;
    }

    /// <summary>
    /// Exact supported native host characters for the R1 fallback (1131 completion contract):
    /// Regent, Necrobinder and Fool only, by CLR type equality. The four legacy profiles keep
    /// priority; this method never turns a native owner into a <see cref="LegacyPowerProfile"/>.
    /// No Owner access, no type-name/namespace/assignability guess.
    /// </summary>
    internal static bool UsesNativeHostPool(CharacterModel? character)
    {
        if (character is null)
        {
            return false;
        }
        Type type = character.GetType();
        return type == typeof(HostCharacters.Regent)
            || type == typeof(HostCharacters.Necrobinder)
            || type == typeof(HostCharacters.Fool);
    }

    /// <summary>
    /// Fresh native host Power pool for a supported native owner: the owner's real CardPool query
    /// (actual epoch/unlock state and current multiplayer constraint included), then Type == Power,
    /// then IsAllowedInRun, then the factory's own combat filter. Native exclusions from those host
    /// filters are legal - there is no legacy no-shrink comparison against a raw host table, no
    /// synthesized/union pool and no invented HEALING rule. The result is a fresh lazy enumerable;
    /// this helper does no materialization, caching, RNG, card creation, logging or mutation.
    /// </summary>
    internal static IEnumerable<CardModel> GetNativeHostPowerPool(Player owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!UsesNativeHostPool(owner.Character))
        {
            throw new InvalidOperationException(
                "Native host Power pool requested for an unsupported owner type '"
                + (owner.Character is null ? "null" : owner.Character.GetType().FullName)
                + "'.");
        }
        return CardFactory.FilterForCombat(
            owner.Character.CardPool
                .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.Type == CardType.Power)
                .Where(card => card.IsAllowedInRun(owner.RunState)));
    }

    /// <summary>
    /// Resolves a profile's complete source domain. Returns false when any declared row is a
    /// HOLD (type not loaded, not registered, type/rarity drift, mutable instance, aliased id)
    /// or when the offered set is empty; the caller must then grant nothing rather than retry
    /// or silently drop a row.
    /// <para>
    /// Precondition (R4): the caller must already have passed the independent unavailable fuse and
    /// both the powers and cards content gates. The cached snapshot evaluated
    /// <c>CanBeGeneratedInCombat</c> under those gates and is not gate-aware; a later gate change
    /// is caught per play by the factory's FilterForCombat shrink check, not by this cache.
    /// </para>
    /// </summary>
    internal static bool TryGetOfferedPool(LegacyPowerProfile profile, out CardModel[] offered, out string[] holds)
    {
        Snapshot snapshot = Resolved[profile.Character].Value;
        offered = snapshot.Offered;
        holds = snapshot.Holds;
        return holds.Length == 0 && offered.Length > 0;
    }

    private static Snapshot Resolve(LegacyPowerProfile profile)
    {
        List<CardModel> offered = new(profile.Inventory.Count);
        List<string> holds = new();
        HashSet<string> legacyIds = new(StringComparer.Ordinal);
        HashSet<ModelId> canonicalIds = new();

        foreach (LegacyPowerSpec spec in profile.Inventory)
        {
            if (!legacyIds.Add(spec.LegacyCardId))
            {
                holds.Add(spec.LegacyCardId + ":duplicate-legacy-id");
                continue;
            }
            // AbstractDungeon.returnTrulyRandomCardInCombat(CardType) skips hasTag(HEALING)
            // before any type match; Self Repair is the only HEALING Power in these inventories.
            if (spec.Healing)
            {
                continue;
            }

            Type? entity = spec.Entity;
            if (entity is null)
            {
                entity = ResolveLoadedType(spec.CurrentType);
                if (entity is null)
                {
                    holds.Add(spec.LegacyCardId + ":type-not-loaded");
                    continue;
                }
            }

            CardModel? card = ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(entity));
            if (card is null)
            {
                holds.Add(spec.LegacyCardId + ":not-registered");
                continue;
            }
            if (!ReferenceEquals(card.GetType(), entity)
                || !string.Equals(card.GetType().FullName, spec.CurrentType, StringComparison.Ordinal))
            {
                holds.Add(spec.LegacyCardId + ":type-drift");
                continue;
            }
            if (!card.IsCanonical)
            {
                holds.Add(spec.LegacyCardId + ":mutable-instance");
                continue;
            }
            if (card.Type != CardType.Power)
            {
                holds.Add(spec.LegacyCardId + ":not-power");
                continue;
            }
            if (card.Rarity != spec.Rarity)
            {
                holds.Add(spec.LegacyCardId + ":rarity-drift");
                continue;
            }
            if (!card.CanBeGeneratedInCombat)
            {
                holds.Add(spec.LegacyCardId + ":not-combat-generatable");
                continue;
            }
            if (!canonicalIds.Add(card.Id))
            {
                holds.Add(spec.LegacyCardId + ":duplicate-canonical-id");
                continue;
            }
            offered.Add(card);
        }

        return new Snapshot(offered.ToArray(), holds.ToArray());
    }

    private static Type? ResolveLoadedType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? type;
            try
            {
                type = assembly.GetType(fullName, throwOnError: false);
            }
            catch (Exception)
            {
                continue;
            }
            if (type is not null)
            {
                return type;
            }
        }
        return null;
    }

    private static LegacyPowerSpec Row<T>(string legacyClassKey, string legacyCardId, CardRarity rarity, bool healing = false)
        where T : CardModel
        => new(legacyClassKey, legacyCardId, rarity, healing, typeof(T), typeof(T).FullName ?? string.Empty);

    private static LegacyPowerSpec External(string legacyClassKey, string legacyCardId, CardRarity rarity, string currentType)
        => new(legacyClassKey, legacyCardId, rarity, false, null, currentType);
}
