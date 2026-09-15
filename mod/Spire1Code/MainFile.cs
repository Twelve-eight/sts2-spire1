using System.Reflection;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Interop;
using BaseLib.Patches.Localization;

namespace Spire1.Spire1Code;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Spire1"; // used for resource filepath (res://Spire1) and ID prefix (SPIRE1-)
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    /// <summary>
    /// SP1-1 (2026-09-15) phase map. Statement order is UNCHANGED from the pre-SP1-1
    /// initializer - the phases only name the boundaries so registration, patching and
    /// observation stay separable for measurement (producer / owner / first consumer
    /// named per phase):
    ///   Phase 1 asset readiness + config: no pool is touched; nothing below can freeze one.
    ///   Phase 2 semantic content registration: the ONLY code that feeds pools; must stay
    ///     before the first pool consumer (the game's first pool generation freezes each
    ///     pool via ModHelper.ConcatModelsFromMods and then AddModelToPool throws).
    ///   Phase 3 patch + interop registration: registers the Harmony triggers, including
    ///     the on-demand census trigger; registering a trigger touches no pool.
    ///   Phase 4 diagnostics: INTENTIONALLY EMPTY at initializer time - observation runs
    ///     post-registration only (see Phase4Diagnostics).
    /// </summary>
    public static void Initialize()
    {
        Phase1AssetReadinessAndConfig();
        Phase2SemanticContentRegistration();
        Phase3PatchAndInteropRegistration();
        Phase4Diagnostics();
    }

    // ------------------------------------------------------------------
    // PHASE 1 - asset readiness + config.
    // Producer: Spire1 initializer. Owner: Spire1. First consumer: the Godot scene
    // loader (script lookup) and BaseLib's loc token conversion at content load.
    // ------------------------------------------------------------------
    private static void Phase1AssetReadinessAndConfig()
    {
        // Enable BaseLib SimpleLoc so cards.json !D!/!B!/*word* tokens are converted at load.
        SimpleLoc.EnableSimpleLoc(ModId);
        // Register C# scripts used by any Godot scenes shipped in the .pck.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Runtime content toggles (Settings -> Mod Settings).
        ModConfigRegistry.Register(ModId, new Spire1Config());
    }

    // ------------------------------------------------------------------
    // PHASE 2 - semantic content registration (model/pool feeding).
    // Producer: Spire1 initializer. Owner: Spire1. First consumer: the game's FIRST
    // pool generation - ModHelper freezes each pool's modded content the first time the
    // pool is generated and then throws on further AddModelToPool calls, so everything
    // that feeds pools MUST stay here, before any consumer and before Phase 3/4.
    // ------------------------------------------------------------------
    private static void Phase2SemanticContentRegistration()
    {
        // Events gate (user request 2026-09-13): gen-1 story events must be
        // toggleable out of base-game event pools. CustomEventModel ctors run
        // at ModelDb init (before config load), so the gate is applied HERE by
        // removing them from BaseLib's registration lists; RegisterType's
        // once-guard prevents re-adds. SpireHeart was already autoAdd:false.
        if (!Spire1Config.EventsEnabled)
        {
            BaseLib.Patches.Content.CustomContentDictionary.ActCustomEvents
                .RemoveAll(e => e is Spire1.Spire1Code.Events.Spire1Event);
            BaseLib.Patches.Content.CustomContentDictionary.SharedCustomEvents
                .RemoveAll(e => e is Spire1.Spire1Code.Events.Spire1Event);
            Logger.Info("[Spire1] StS1 events removed from shared event pools (EnableSts1Events=false)");
        }

        // LEAN-CODE RULE (DEVELOP.md 7a): shipped StS2 cards that are identical to their StS1
        // counterparts are added to our pools instead of being reimplemented. Must run before the
        // game generates any pool, because ModHelper freezes modded pool content on first use.
        // SP1-1: registration only - the old startup LogPoolCensus pool reads were moved to
        // Diagnostics/PoolCensus.cs (post-registration, opt-in; a diagnostic AllCards read
        // freezes pools and would lock later-loaded mods out of them).
        SharedCardReuse.Register();
    }

    // ------------------------------------------------------------------
    // PHASE 3 - patch + interop registration.
    // Producer: Spire1 initializer. Owner: Spire1. First consumer: every patched engine
    // call site from here on. One-line failure note: the Harmony loop keeps one try/catch
    // PER TYPE (see below), and the interop bridges keep per-capability boundaries.
    // This phase also registers the on-demand diagnostic TRIGGER (PoolCensusMenuPatch,
    // picked up by the attribute scan below) - registering a trigger performs no pool read.
    // ------------------------------------------------------------------
    private static void Phase3PatchAndInteropRegistration()
    {
        // Apply Harmony patches declared in this assembly - one try/catch PER TYPE so a single
        // bad patch can never abort the whole set (PatchAll aborts on first failure, which
        // silently stripped every other patch for an entire night run on 2026-08-24).
        // SP1-1 evidence note: this reflection discovery runs at startup but is NOT proven to
        // be a frame bottleneck - do not optimize or cache it without measurement.
        Harmony harmony = new(ModId);
        int failed = 0;
        foreach (var type in typeof(MainFile).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
            {
                continue;
            }
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception e)
            {
                failed++;
                Logger.Error($"Harmony patch {type.Name} failed: {e.Message}");
            }
        }
        if (failed > 0)
        {
            Logger.Error($"Harmony: {failed} patch class(es) failed to apply");
        }

        // AutoAnthony 桥接：必须在 ModManager 已加载 AutoAnthony 之后应用（本 initializer
        // 的调用时机--ModManager.Initialize 逐 mod 依拓扑序调 initializer--取决于加载
        // 顺序；AutoAnthony 无依赖、按用户 mod 列表序可能在本 mod 之前或之后。若此刻
        // 尚未加载，由 AutoAnthonyLoadHook 的 AssemblyLoad 事件兜底重试）。
        AutoAnthonyLoadHook.TryApplyBridge(harmony);

        // 第三方（RitsuLib）弹窗抑制不能进上面的属性扫描--目标类型缺失时 AccessTools
        // 解析会抛异常，会让注册循环每次启动都记一条失败。显式调用、内部自兜底。
        if (Spire1Config.IgnoreMpModDifferences)
        {
            Logger.Info(RitsuLibPopupSuppressionPatch.Apply(harmony)
                ? "[Spire1] MP ignore-mod-diff: RitsuLib divergence popup suppressed"
                : "[Spire1] MP ignore-mod-diff: RitsuLib popup type not found (mod absent?) - skipped");
        }
    }

    // ------------------------------------------------------------------
    // PHASE 4 - diagnostics: post-registration only, INTENTIONALLY EMPTY here.
    // Producer (on demand): PoolCensusMenuPatch (first menu entry, config
    // Spire1Config.PoolCensusOnMenuEnter, default OFF) or the "poolcensus" console
    // command. Owner: Spire1 diagnostics. First consumer: the log / console output.
    // DEFECT FIXED HERE (SP1-1, astra-advice.md SP1-5): the old LogPoolCensus read
    // pool AllCards at initializer time, and a diagnostic AllCards read freezes the
    // pool (ModHelper.ConcatModelsFromMods) - a later-loaded mod's AddModelToPool for
    // that pool then throws. Never read a pool in this phase; Diagnostics/PoolCensus.cs
    // documents the freeze semantics and is the only sanctioned observation path.
    // ------------------------------------------------------------------
    private static void Phase4Diagnostics()
    {
        // Intentionally empty: no diagnostic pool materialization at initializer time.
        // The census is triggered post-registration only (see phase comment above).
    }
}
