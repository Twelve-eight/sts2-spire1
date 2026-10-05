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
    ///     C12 r7: after the scan, the central gate installation is reconciled by Harmony
    ///     probe and Spire1PowersGate.EvaluateAndEnforceCoverage runs; when the Spire1
    ///     power-path coverage cannot be proven, it hard-closes the Spire1 content master
    ///     switch (runtime only, not saved) before any run or pool consumer exists.
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
        // r8c: 注册熔断面必须最先安装: 它不读 Spire1Config, 不依赖 powers central/fallback prefix,
        // 只过滤 Spire1 程序集的模型类型 (vanilla/其它 mod 放行). 先于 Phase1/Phase2, 保证
        // 之后任何确定性不可用状态 (含 Phase1 异常) 都能在 ModelDb.Init 之前生效.
        try
        {
            Spire1PowersGate.EnsureRegistrationFuseInstalled(Phase3Harmony());
        }
        catch (Exception e)
        {
            Spire1PowersGate.MarkDeterministicUnavailable(
                "registration fuse installation exception (" + e.GetType().Name + ": " + e.Message + ")");
        }

        // r8: Forms 旧档 raw modifier guard 是独立 save 安全面, 不受 powers gate 的
        // ContentUnavailableActive 支配. 必须早于 Phase1/Phase2 显式安装, 并由 guard 自身用
        // Harmony.GetPatchInfo 精确证明 owner+prefix+priority; 证明失败只如实记录未闭合, 不把
        // 内容熔断当成 save 保护已生效.
        try
        {
            if (!FormsMissingModifierSaveGuardPatch.EnsureInstalled(Phase3Harmony()))
            {
                Logger.Error(
                    "[Spire1] Forms save guard NOT proven installed at earliest safe stage; raw " +
                    "SPIRE1-FORM_STANCE_MODIFIER save protection is incomplete.");
            }
        }
        catch (Exception e)
        {
            Logger.Error(
                "[Spire1] Forms save guard installation threw (" + e.GetType().Name + ": " + e.Message +
                "); raw SPIRE1-FORM_STANCE_MODIFIER save protection is incomplete.");
        }

        try
        {
            Phase1AssetReadinessAndConfig();
        }
        catch (Exception e)
        {
            Spire1PowersGate.MarkDeterministicUnavailable(
                "Phase1 exception (" + e.GetType().Name + ": " + e.Message + ")");
        }

        // r8b (P1-C12-01): powers gate 安装/覆盖证明必须收束在 Phase2 内容注册之前. 覆盖证明
        // 失败或任何确定性不可用状态置位时, 跳过 Phase2 内容注册 (硬失败状态已独立于
        // Spire1Config setter 与未安装 prefix). Phase3 只安装其余补丁并复核, 不会重复挂载.
        bool contentRegistrationAllowed;
        try
        {
            contentRegistrationAllowed = Spire1PowersGate.PreflightBeforeContentRegistration(
                Phase3Harmony());
        }
        catch (Exception e)
        {
            Spire1PowersGate.MarkDeterministicUnavailable(
                "Phase3 harmony creation exception (" + e.GetType().Name + ": " + e.Message + ")");
            contentRegistrationAllowed = false;
        }
        if (contentRegistrationAllowed)
        {
            try
            {
                Phase2SemanticContentRegistration();
            }
            catch (Exception e)
            {
                Spire1PowersGate.MarkDeterministicUnavailable(
                    "Phase2 content registration exception (" + e.GetType().Name + ": " + e.Message + ")");
            }
        }
        else
        {
            Logger.Error(
                "[Spire1] Phase2 semantic content registration skipped: powers gate reported the " +
                "deterministic unavailable state (coverage not proven or hard fail-closed).");
        }

        try
        {
            Phase3PatchAndInteropRegistration();
        }
        catch (Exception e)
        {
            Spire1PowersGate.MarkDeterministicUnavailable(
                "Phase3 global exception (" + e.GetType().Name + ": " + e.Message + ")");
        }

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
        // r8b (P1-C12-01): Phase3 重复进入保护改为 "完整成功收束后置位". 首个调用者执行;
        // 中途异常由 Initialize 的全局 catch 进入确定性不可用路径, 且不会把半成品标记为已完成,
        // 后续重入会重新尝试 (Harmony 安装本身幂等, 不会重复挂载).
        lock (Phase3Lock)
        {
            if (_phase3Completed)
            {
                Logger.Error("[Spire1] Phase3 re-entry ignored (already completed this process).");
                return;
            }

            // r8b: powers gate 类 (central Apply/ModifyAmount + 五个 fallback 类) 已由
            // Spire1PowersGate.PreflightBeforeContentRegistration 在 Phase2 之前显式安装/收束;
            // 属性扫描跳过它们, 避免重复挂载. 其余补丁保持每个类型独立 try/catch.
            Harmony harmony = Phase3Harmony();

            // r8c (P1-C12-01): 独立不可用状态 (ContentUnavailable/HardFailClosed) 下不得再安装任何
            // 普通补丁或 interop: 它们会读取 Spire1Config, 类型初始化失败时会把异常传播到 vanilla/其它
            // mod 路径. 只保留已实测安装的注册熔断面, 由 EnsureRegistrationFuseInstalled 复核 (幂等).
            if (Spire1PowersGate.ContentUnavailableActive)
            {
                // r8d (P1 D1): 不可用状态下不能只保留 ModelDb 熔断面. 引擎仍会构造 Spire1 模型并
                // 经 BaseLib [Pool] 注入池; 因此这里显式安装 cards/relics/potions/events 的独立安全
                // 过滤器, 并逐目标用 Harmony.GetPatchInfo 精确证明. 白名单安装失败时保持 fail closed,
                // 由 EnsureUnavailableSafetyFiltersInstalled 返回 false 并如实记录未闭合边界.
                Logger.Error(
                    "[Spire1] Phase3 patch/interop installation skipped: deterministic unavailable state is set; " +
                    "installing only the independent unavailable content safety filters (cards/relics/potions/events).");
                Spire1PowersGate.EnsureRegistrationFuseInstalled(harmony);
                bool safetyFiltersProven = Spire1PowersGate.EnsureUnavailableSafetyFiltersInstalled(harmony);
                if (!safetyFiltersProven)
                {
                    Logger.Error(
                        "[Spire1] r8d unavailable content safety filters NOT fully proven; Spire1 content " +
                        "closure is incomplete and remains fail-closed pending investigation.");
                }
                _phase3Completed = true;
                return;
            }

            // r8c: powers gate 幂等复核提前到属性扫描之前. 若 coverage 失败/独立不可用状态置位,
            // 本次 Phase3 不安装任何普通补丁/interop (见下方第二次检查), 避免读取 Spire1Config 的
            // 补丁在类型初始化失败时把异常传播到 vanilla/其它 mod 路径.
            Spire1PowersGate.EnsureInstalled(harmony);

            if (Spire1PowersGate.ContentUnavailableActive)
            {
                // r8d: powers gate 中途进入不可用状态时, 同样显式安装并证明安全过滤器.
                Logger.Error(
                    "[Spire1] Phase3 patch/interop installation skipped: powers gate reported the deterministic " +
                    "unavailable state; installing only the independent unavailable content safety filters.");
                Spire1PowersGate.EnsureRegistrationFuseInstalled(harmony);
                bool safetyFiltersProven = Spire1PowersGate.EnsureUnavailableSafetyFiltersInstalled(harmony);
                if (!safetyFiltersProven)
                {
                    Logger.Error(
                        "[Spire1] r8d unavailable content safety filters NOT fully proven; Spire1 content " +
                        "closure is incomplete and remains fail-closed pending investigation.");
                }
                _phase3Completed = true;
                return;
            }

            if (!_phase3ScanCompleted)
            {
                int failed = 0;
                foreach (var type in typeof(MainFile).Assembly.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                    {
                        continue;
                    }
                    // r8: guard 已由 Initialize 最早安全阶段显式安装/证明; 通用扫描精确跳过它,
                    // 避免重复挂载相同 owner+prefix+priority, 也避免不可用分支的漏装回退.
                    if (type == typeof(FormsMissingModifierSaveGuardPatch)
                        || Spire1PowersGate.IsGateManagedPatchType(type))
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
                _phase3ScanCompleted = true;
            }

            AutoAnthonyLoadHook.TryApplyBridge(harmony);
            Interop.AftpEffectLifecycleCompat.TryApply(harmony);

            if (Spire1Config.IgnoreMpModDifferences)
            {
                Logger.Info(RitsuLibPopupSuppressionPatch.Apply(harmony)
                    ? "[Spire1] MP ignore-mod-diff: RitsuLib divergence popup suppressed"
                    : "[Spire1] MP ignore-mod-diff: RitsuLib popup type not found (mod absent?) - skipped");
            }

            _phase3Completed = true;
        }
    }

    /// <summary>r8b: Phase3 一次性 completed 状态 (完整收束后才置位).</summary>
    private static readonly object Phase3Lock = new();
    private static bool _phase3Completed;
    private static bool _phase3ScanCompleted;
    private static Harmony? _phase3Harmony;

    /// <summary>r8b: Phase3 使用的进程级 Harmony 实例 (preflight 与 Phase3 共用同一 id/实例).</summary>
    private static Harmony Phase3Harmony() =>
        _phase3Harmony ??= new Harmony(ModId);

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
