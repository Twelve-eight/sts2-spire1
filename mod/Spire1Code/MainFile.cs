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
    /// <remarks>
    /// <para>
    /// CONTENT GATE CONTRACT (C01): model instantiation is engine-driven and happens
    /// after every mod initializer (ModelDb.Init -> Activator.CreateInstance over
    /// AllAbstractModelSubtypes, which includes mod types). Therefore "content group off"
    /// can never mean "do not instantiate the class": it means the group is filtered at
    /// the runtime query/grant entries and by the per-run snapshot. Phase 2 still runs
    /// unconditionally so that old saves keep resolving their model ids; the switches are
    /// read by <see cref="Spire1Config.IsEnabled(Spire1ContentGroup)"/>
    /// consumers (pools, event filter, character pools, effect application).
    /// </para>
    /// </remarks>
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
        // Events gate (user request 2026-09-13): gen-1 story events must be toggleable out of
        // base-game event pools.
        // FIXED 2026-09-27: the old removal here was a no-op. It ran in this INITIALIZER, but
        // Spire1Event instances are only constructed (and auto-added to ActCustomEvents /
        // SharedCustomEvents) later, at ModelDb.Init, which the engine's ModManager runs AFTER
        // every mod initializer. So RemoveAll matched 0 items and the events were added anyway -
        // the toggle had no effect regardless of its value. The gate is now enforced at RUNTIME
        // by Sts1EventToggleFilterPatch (ActModel.GenerateRooms postfix), which reads the config
        // after ModelDb.Init and strips Spire1Event from each act's event pool when disabled.
        // SpireHeart stays out via compile-time autoAdd:false, unchanged.

        // LEAN-CODE RULE (DEVELOP.md 7a): shipped StS2 cards that are identical to their StS1
        // counterparts are added to our pools instead of being reimplemented. Must run before the
        // game generates any pool, because ModHelper freezes modded pool content on first use.
        // SP1-1: registration only - the old startup LogPoolCensus pool reads were moved to
        // Diagnostics/PoolCensus.cs (post-registration, opt-in; a diagnostic AllCards read
        // freezes pools and would lock later-loaded mods out of them).
        // C01: this is append-only registration for save-compatibility and never reads a pool;
        // the content-group switches are enforced at the query/grant entries instead (see the
        // gate contract on the class doc).
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
        // C01: every content filter registered here must read
        // Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup) (or the matching computed
        // property) and fail closed when its target cannot be resolved.
        // C12 r5-B: the Spire1PowersFallback* classes are intentionally excluded from this scan;
        // Spire1PowersFallbackInstaller.InstallIfNeeded below installs them only when the two
        // central PowerCmd targets are not both verifiably installed, and records P1-C12-01
        // NOT closed when the fallback layer itself is incomplete. The fallback layer holds no
        // cross-invocation state (no token/count/CWT), so a blocked invocation can never consume
        // an allowed invocation's state.
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
            // C12 r5-B (P1-C12-01): fallback 类必须由 InstallIfNeeded 按中央 gate 的真实安装
            // 状态显式安装, 不能随属性扫描无条件挂载 (否则主漏斗可用时也会增加运行期路径).
            if (Spire1PowersGate.IsFallbackPatchType(type))
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

        // C12 r5-B (P1-C12-01): fallback 类已被上面的属性扫描排除, 这里按两个中央目标的真实安装
        // 状态决定是否安装 fail-closed fallback. 中央漏斗完整时该调用是 no-op; 任一 fallback
        // 目标缺失/安装失败时置 FailClosedDegraded 并记录 P1-C12-01 NOT closed.
        Spire1PowersFallbackInstaller.InstallIfNeeded(harmony);

        // AutoAnthony 桥接:必须在 ModManager 已加载 AutoAnthony 之后应用(本 initializer
        // 的调用时机--ModManager.Initialize 逐 mod 依拓扑序调 initializer--取决于加载
        // 顺序;AutoAnthony 无依赖,按用户 mod 列表序可能在本 mod 之前或之后.若此刻
        // 尚未加载,由 AutoAnthonyLoadHook 的 AssemblyLoad 事件兜底重试).
        AutoAnthonyLoadHook.TryApplyBridge(harmony);
        // AFTP-1 (SpireAftpCompat): optional AFTP effect-lifecycle compat - replaces the
        // NSts1Effect family's ProcessFrame subscription with a symmetric detach/reentry
        // binding (astra AFTP-R4-03: GetTree outside the tree). Absent AFTP = no-op;
        // all outcomes logged by the compat layer itself.
        Interop.AftpEffectLifecycleCompat.TryApply(harmony);

        // 第三方(RitsuLib)弹窗抑制不能进上面的属性扫描--目标类型缺失时 AccessTools
        // 解析会抛异常,会让注册循环每次启动都记一条失败.显式调用,内部自兜底.
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
