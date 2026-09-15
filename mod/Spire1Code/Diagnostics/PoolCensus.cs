using System.Diagnostics;
using System.Globalization;
using System.Text;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;
// Namespace-qualified pool references (no blanket usings): the engine and this mod both
// define same-named pools (engine MegaCrit.Sts2.Core.Models.CardPools.SilentCardPool vs
// our Spire1.Spire1Code.Character.SilentCardPool); SharedCardReuse injects into OUR pools.
using EngineCardPools = MegaCrit.Sts2.Core.Models.CardPools;

namespace Spire1.Spire1Code.Diagnostics;

/// <summary>
/// SP1-1 (2026-09-15): ordered pool census - the tool the later pool-census correctness
/// task runs before any deduplication/removal decision (plan change 3: distinguish
/// code-class duplication, resource duplication, and duplicate reward weight). Enumerates
/// the FINAL pool contents under the active pool mode, covering both shared and
/// PureSts1Pools modes: the pool set is identical, the contents differ because
/// SharedCardReuse.Register behaved differently at initializer time.
///
/// FREEZE SAFETY - why this tool is post-load only (this replaces the old startup
/// LogPoolCensus, the SP1-1 defect; see astra-advice.md SP1-5):
/// CardPoolModel.AllCards lazily runs GenerateAllCards() and then
/// ModHelper.ConcatModelsFromMods, which sets that pool type's ModHelper "isFrozen" flag
/// THE FIRST TIME the pool is read. After that, ModHelper.AddModelToPool throws for the
/// pool - so a diagnostic AllCards read is write-equivalent: at mod-initializer time it
/// can freeze a LATER-LOADED mod's content out of the pool. Both triggers below therefore
/// fire strictly AFTER every mod initializer has finished:
///   - first main menu entry, only when Spire1Config.PoolCensusOnMenuEnter is true
///     (default OFF; trigger: Patches/PoolCensusMenuPatch.cs), or
///   - the "poolcensus" console command (Diagnostics/PoolCensusConsoleCmd.cs).
/// Re-running after the first materialization is read-only (AllCards is cached in the
/// pool instance; the freeze flag is already set and setting it again is a no-op).
///
/// OUTPUT, per pool (Colorless / Spire1 / Silent / Defect / Spire1Legacy):
///   - one ordered line per member: idx, model id entry + category, rarity, concrete
///     class name, best-effort registration origin;
///   - DUP summary: a model id appearing more than once in one pool = duplicate reward
///     weight (ConcatModelsFromMods concatenates blindly without deduplication);
///   - ID-MIX summary: one model id served by multiple code classes = code-class
///     duplication candidate (duplicate class names are NOT proof of dead content - the
///     census task decides; Spire1LegacyPool retired ids are expected to coexist with
///     shipped names).
///
/// ORIGIN ATTRIBUTION is best-effort by design. Our own SharedCardReuse injections are
/// known exactly via SharedCardReuse.InjectionLedger (recorded at Register() time, no
/// pool reads). Because ConcatModelsFromMods APPENDS modded content after the pool's own
/// generated content, the census walks the member list BACKWARDS and consumes matching
/// ledger entries (type-name multiset); those are reported as "spire1:&lt;kind&gt;".
/// Everything else is reported as "not-sharedcardreuse" - i.e. NOT injected through
/// SharedCardReuse: our own [Pool]-attributed classes (registered at ModelDb init),
/// engine/base content for the engine pools, or another mod's injection. Known
/// ambiguity: an engine twin of a "shipped-twin" injection resolves in our favor only
/// at the tail - which is exactly where ConcatModelsFromMods places modded content.
///
/// NOT derivable from CardPoolModel/CardModel and therefore NOT emitted here (FLAG for
/// the census task, not silently skipped): resource duplication via shared art paths
/// (CardModel exposes no art-path member), old-save id coverage, and transform/reward
/// candidate derivation - those need CardFactory-side evidence in addition to this tool.
/// </summary>
internal static class PoolCensus
{
    private static int _menuRunStarted; // one-shot latch for the menu trigger

    /// <summary>Menu-enter trigger entry: runs the census at most once per session.</summary>
    internal static void TryRunOnceFromMenu()
    {
        if (Interlocked.Exchange(ref _menuRunStarted, 1) != 0)
        {
            return;
        }
        Run("menu-enter");
    }

    /// <summary>
    /// Builds and logs the census. Returns the full report (the console command shows it).
    /// Never throws: each pool is an independent failure boundary - a failed pool is
    /// reported and the census continues with the others.
    /// </summary>
    internal static string Run(string trigger)
    {
        long t0 = Stopwatch.GetTimestamp();
        var sb = new StringBuilder(4096);
        string mode = Spire1Config.PureSts1Pools ? "pure" : "shared";
        sb.AppendLine("[Spire1] PoolCensus BEGIN trigger=" + trigger + " mode=" + mode
            + " (mode label read live; pool CONTENTS were fixed at initializer time by SharedCardReuse.Register)");
        sb.AppendLine("[Spire1] PoolCensus NOTE freeze: this run READ each pool's AllCards, which freezes that pool"
            + " (ModHelper.ConcatModelsFromMods). Post-load diagnostic only - never call during mod initialization.");

        // Engine colorless pool (was materialized at initializer time by the old
        // LogPoolCensus - the SP1-1 defect) + our three character pools (the injection
        // targets of SharedCardReuse) + the retired-ids sink pool.
        CensusPool<EngineCardPools.ColorlessCardPool>(sb);
        CensusPool<Character.Spire1CardPool>(sb);
        CensusPool<Character.SilentCardPool>(sb);
        CensusPool<Character.DefectCardPool>(sb);
        CensusPool<Character.Spire1LegacyPool>(sb);

        double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
        sb.AppendLine("[Spire1] PoolCensus END trigger=" + trigger
            + " census_ms=" + ms.ToString("F3", CultureInfo.InvariantCulture));

        string report = sb.ToString();
        foreach (string line in report.Split('\n'))
        {
            if (line.Length == 0)
            {
                continue; // trailing element after the final newline
            }
            MainFile.Logger.Info(line.TrimEnd('\r'));
        }
        return report;
    }

    /// <summary>
    /// Enumerates one pool's final content. THE FREEZE POINT is the single AllCards read
    /// below - intentional and post-load only (see class doc). Per-pool try/catch keeps the
    /// capability-level failure boundary: one unresolvable pool cannot kill the census or
    /// the game.
    /// </summary>
    private static void CensusPool<TPool>(StringBuilder sb) where TPool : CardPoolModel
    {
        string name = typeof(TPool).Name;
        try
        {
            CardPoolModel pool = ModelDb.CardPool<TPool>();
            // NOTE: CardPool<T> THROWS (KeyNotFound/ModelNotFound) when absent — it never
            // returns null; the per-pool catch below surfaces that as a FAILED line.

            // THE FREEZE POINT (documented, intentional, post-load only): the first AllCards
            // read generates + concatenates modded content + caches, and freezes the pool.
            var cards = pool.AllCards.ToList();

            // Origin lookup: our injections for THIS pool, as a type-name multiset with the
            // first-recorded origin per type name (a twin name appears once per pool list).
            var originByTypeName = new Dictionary<string, string>(StringComparer.Ordinal);
            var remainingByTypeName = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in Character.SharedCardReuse.InjectionLedger)
            {
                if (entry.PoolName != name)
                {
                    continue;
                }
                originByTypeName.TryAdd(entry.ModelTypeName, entry.Origin);
                remainingByTypeName[entry.ModelTypeName] = remainingByTypeName.GetValueOrDefault(entry.ModelTypeName) + 1;
            }

            sb.AppendLine("[Spire1] PoolCensus " + name + ": total=" + cards.Count + " begin");
            var multiplicity = new Dictionary<string, int>(cards.Count, StringComparer.Ordinal);
            var classesPerId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            // Origin attribution: BACKWARD tail-matching (doc contract). ConcatModelsFromMods
            // APPENDS our injections after generated content, so matching must consume the
            // ledger's remaining counts from the TAIL of the member list; forward matching
            // would mislabel the first same-name occurrence in generated content instead of
            // the real tail injection (reviewer SP1-1 REQUIRED fix).
            var originByIndex = new string[cards.Count];
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                originByIndex[i] = "not-sharedcardreuse";
                string typeName = cards[i].GetType().Name;
                if (remainingByTypeName.TryGetValue(typeName, out int remaining) && remaining > 0)
                {
                    remainingByTypeName[typeName] = remaining - 1;
                    originByIndex[i] = "spire1:" + originByTypeName[typeName];
                }
            }

            int idx = 0;
            foreach (var c in cards)
            {
                string origin = originByIndex[idx];
                string typeName = c.GetType().Name;
                string id = c.Id.Entry;
                multiplicity[id] = multiplicity.GetValueOrDefault(id) + 1;
                if (!classesPerId.TryGetValue(id, out var classSet))
                {
                    classSet = new HashSet<string>(StringComparer.Ordinal);
                    classesPerId[id] = classSet;
                }
                classSet.Add(typeName);
                sb.AppendLine("[Spire1] PoolCensus " + name + " idx=" + idx + " id=" + id
                    + " cat=" + c.Id.Category + " rarity=" + c.Rarity
                    + " class=" + typeName + " origin=" + origin);
                idx++;
            }

            foreach (var kv in multiplicity.Where(kv => kv.Value > 1).OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                sb.AppendLine("[Spire1] PoolCensus " + name + " DUP id=" + kv.Key + " count=" + kv.Value
                    + " classes=" + FormatClasses(classesPerId[kv.Key])
                    + " (same model id twice in one pool = duplicate reward weight; ConcatModelsFromMods does not deduplicate)");
            }
            foreach (var kv in classesPerId.Where(kv => kv.Value.Count > 1).OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                sb.AppendLine("[Spire1] PoolCensus " + name + " ID-MIX id=" + kv.Key
                    + " classes=" + FormatClasses(kv.Value)
                    + " (one model id served by multiple code classes = code-class duplication candidate)");
            }
            sb.AppendLine("[Spire1] PoolCensus " + name + ": end");
        }
        catch (Exception e)
        {
            sb.AppendLine("[Spire1] PoolCensus " + name + " FAILED: " + e.GetType().Name + ": " + e.Message);
        }
    }

    private static string FormatClasses(HashSet<string> classes)
        => string.Join("|", classes.OrderBy(n => n, StringComparer.Ordinal));
}
