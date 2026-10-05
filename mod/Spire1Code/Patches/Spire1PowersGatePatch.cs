using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Potions;
using Spire1.Spire1Code.Relics;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Interop;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Spire1 powers content-group gate (C08, 2026-10-02).
/// <para>
/// 背景: <see cref="Spire1Config.PowersEnabled"/> 此前只有定义没有消费者, powers 开关是 dead gate
/// (监督报告 SC-spire1 已确认). 本文件为它接上真实运行期消费者.
/// </para>
/// <para>
/// 引擎调用面 (源码证据, .tmp/dllsrc/MegaCrit.Sts2.Core.Commands/PowerCmd.cs):
/// 1) <c>PowerCmd.Apply&lt;T&gt;</c> 无已有实例 -> 新建 <c>powerModel.ToMutable()</c> 后调用非泛型
///    <c>Apply(PlayerChoiceContext, PowerModel, Creature, decimal, Creature?, CardModel?, bool)</c>;
/// 2) <c>PowerCmd.Apply&lt;T&gt;</c> 已有实例 -> 直接调用 <c>ModifyAmount</c>;
/// 3) 直接调用非泛型 <c>Apply</c> 的调用方 (Forms/桥接/控制台/Mock 卡等) 同样走 (1) 的入口;
/// 4) <c>Decrement</c> / <c>TickDownDuration</c> 内部也走 <c>ModifyAmount</c>.
/// 因此非泛型 <c>Apply</c> + <c>ModifyAmount</c> 是覆盖"直接应用 + 已有实例修改"的两条必经漏斗.
/// 泛型开放方法不作为补丁目标: BaseLib 文档明确 "Harmony can't patch Instantiate&lt;T&gt;() directly
/// because it's a generic method and .NET shares native code across reference-type instantiations"
/// (research/BaseLib-StS2/docs/auto_conversion.md:9); 本仓库无任何成功 patch 泛型方法的先例.
/// </para>
/// <para>
/// 判定规则: 只拦截 <c>Spire1.*</c> 命名空间的 PowerModel (与既有
/// <see cref="Spire1PowerIconFallbackPatch"/> 同款前缀判定), 原版 <c>MegaCrit.*</c>,BaseLib,
/// 其它 mod 一律不碰.
/// </para>
/// <para>
/// Forms 例外 (独立程序集, 自持生命周期): 独立 Forms 程序集声明的 power 由 Forms 自己管理挂载/移除,
/// 不属于 Spire1 powers gate. 本文件只通过 <see cref="Spire1.Spire1Code.Interop.FormsCompatibilityBridge"/>
/// 的运行时程序集身份判定识别它们, 不使用命名空间字符串, 也不做任何 Forms 编译期类型引用.
/// Forms 程序集缺失 (或类型为 null) 时该判定为 false; 签名校验失败/Retryable/Terminal/ShuttingDown 期间仍按程序集身份识别为 Forms 类型, 不按普通外部 mod power 重新纳入前缀门控.
/// </para>
    /// <para>
    /// 语义边界 (已在报告中记录):
    /// * <c>Apply</c> 拦截所有新挂载; 泛型 <c>Apply&lt;T&gt;</c> 的新实例路径在拦截后仍会返回未挂载的
    ///   mutable 实例 (phantom). 本仓库内该返回值只被用于 null 检查/对未挂载实例写字段
    ///   (Combust/Nightmare/TheBomb), 以及 <c>StanceCmd.Enter</c> 的 Dispatch (监听者本身也是 Spire1
    ///   power, powers 关闭时不存在). 该 phantom 无法在本层消除, 已标记为残余边界.
    /// * <c>ModifyAmount</c> 的拦截规则 (C12 细化, 判定谓词 <c>ShouldBlockModify</c>):
    ///   - offset == 0: 放行 (保留引擎自身的清理语义);
    ///   - offset &gt; 0: 仅拦截 Spire1 power 实例的叠加, 或 Spire1 卡触发的 vanilla power 叠加;
    ///   - offset &lt; 0 且目标是 Spire1 power 实例 (cardSource 为 null, 即倒计时/自减/移除):
    ///     放行, 避免把已存在实例冻结成死锁 (旧存档与局中切开关的收尾语义);
    ///   - offset &lt; 0 且由 Spire1 卡发起/目标是 vanilla power: 拦截 (属于 Spire1 内容触发的新效果).
    ///   移除 (<c>PowerCmd.Remove</c>) 不设门控, 保证切开关后旧实例能正常收尾.
    /// * fallback 的 PreApplyBridge/Given/Received/ApplyInternal 层不重新解释该谓词, 而是复用同一
    ///   <c>ShouldBlockApply/ShouldBlockModify</c>, 并只用本次调用的参数与实例的稳定状态
    ///   (<c>HasOwner</c>/<c>IsCanonical</c>) 做本地判定; 不保存任何跨调用状态.
    /// * 直接 <c>SetAmount</c> 的第三方路径仍不在本门控覆盖内; <c>ApplyInternal</c> 仅在 fallback
    ///   层成功安装时覆盖 Spire1 命名空间 power (Forms 形态局例外); Spire1 卡源触发的 vanilla power
    ///   在该层没有 cardSource 可判定, 属明确记录的 NotClosed 边界.
    /// </para>
    /// <para>
    /// fail closed (P1-C12-01, r3/r5-B/r7/r8b): 两个中央目标各自解析并各自安装. 安装结果不再以 Cleanup
    /// 回调为准, 而是由 <see cref="ReconcileCentralGateInstallation"/> 用 Harmony.GetPatchInfo 实测收束;
    /// 目标缺失, 参数漂移, 歧义或 Harmony 安装失败时对应标志为 false, 日志明确写 "NOT installed".
    /// r8b 接线: MainFile 在 Phase2 内容注册之前调用 <see cref="PreflightBeforeContentRegistration"/>,
    /// 它通过唯一入口 <see cref="EnsureInstalled"/> 执行确定性序列: 显式安装并收束中央 gate 的真实状态,
    /// 再调用 <see cref="Spire1PowersFallbackInstaller.InstallIfNeeded"/> (中央漏斗完整时不安装 fallback,
    /// 正常路径零开销; 不完整时才安装 fallback 目标), 最后做覆盖证明. Phase3 属性扫描显式跳过
    /// 中央 gate 类与 fallback 类, 只安装其余补丁. fallback 的安装状态同样由 GetPatchInfo 实测收束,
    /// 重复进入只复核不重复挂载 (EnsureInstalled / installer 各持一次性 guard).
    /// </para>
    /// <para>
    /// r8b 覆盖证明 (r7 P1-1 闭合): 对最坏情况 (applier == null 或不在 combat state, Given 被引擎跳过),
    /// Spire1 cardSource -> vanilla/其它 mod power 的新挂载只认可能真实安装且覆盖完整派发目标的层:
    /// central Apply 或 fallback Before 任一层真实安装 (FallbackPreApplyBridge 仅 best-effort, 不作为
    /// coverage 证明); 修改要求 (ModifyGateInstalled || FallbackHookInstalled); Spire1 命名空间 power
    /// 的新挂载/修改分别由 Apply/Before/Received/ApplyInternal 与 Modify/Before/Received 覆盖.
    /// Given 刻意不计入 (它在 applier 不可达时被引擎跳过, 且 fallback Given 无法识别
    /// Received/ApplyInternal 的无 cardSource 路径).
    /// 覆盖证明失败时 <see cref="EvaluateAndEnforceCoverage"/> 执行真实硬阻断: 先置独立
    /// ContentUnavailable/HardFailClosed 状态 (不依赖任何未安装 prefix), 再尝试运行期关闭
    /// Spire1Config.EnableSts1Content (不写档); setter 失败时保持独立不可用状态, 由 MainFile
    /// preflight 在 Phase2 内容注册前跳过后续注册.
    /// </para>
    /// <para>
    /// 残余边界 (r8b 明确保留, 不得伪称已闭合): 第三方 mod 的 Harmony prefix 先返回 false 时本 mod
    /// fallback 不会执行; 直接 <c>SetAmount</c> 或第三方直接 <c>ApplyInternal</c> 的 vanilla/其它 mod
    /// power 没有 cardSource 可判定; Forms 形态局例外保持 r6-B 语义 (形态子系统自持生命周期, 见上文);
    /// 旧存档中已挂载/已持有的 Spire1 内容不会因硬熔断被清除. 后续 mod 加载的 BeforeApplied override
    /// 在 Phase3 无法完整枚举, PreApplyBridge 固定目标集合只作 best-effort, 不计入 coverage 证明.
    /// </para>
    /// </summary>
internal static class Spire1PowersGate
{
    private const string ModNamespacePrefix = "Spire1.";
    private const string CardsNamespace = "Spire1.Spire1Code.Cards";
    // Forms is a separate assembly with no stable namespace contract from Spire1's side. The
    // powers gate keeps independent Forms powers out of its scope by assembly identity, never by
    // a hardcoded namespace string or a compile-time Forms type reference.

    private static bool _blockedApplyLogged;
    private static bool _blockedModifyLogged;

    /// <summary>
    /// True 仅当非泛型 <c>PowerCmd.Apply</c> 目标已解析且对应 Harmony 补丁类成功安装.
    /// 目标缺失/安装失败时保持 false; 该标志不会让缺失目标被当作已安装.
    /// </summary>
    internal static bool ApplyGateInstalled { get; private set; }

    /// <summary>
    /// True 仅当 <c>PowerCmd.ModifyAmount</c> 目标已解析且对应 Harmony 补丁类成功安装.
    /// 目标缺失/安装失败时保持 false; 该标志不会让缺失目标被当作已安装.
    /// </summary>
    internal static bool ModifyGateInstalled { get; private set; }

    /// <summary>后备层 BeforePowerAmountChanged 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackHookInstalled { get; private set; }

    /// <summary>
    /// 后备层 PreApplyBridge 是否已安装 (仅在中央漏斗不完整时). r8b: 该层只作为 best-effort
    /// 第二道阻断, 不再计入 cardSource coverage 证明: 后续 mod 加载的 BeforeApplied override
    /// 在 Phase3 无法完整枚举, 固定目标集合不能证明覆盖全部虚派发目标.
    /// </summary>
    internal static bool FallbackPreApplyBridgeInstalled { get; private set; }

    /// <summary>后备层 ModifyPowerAmountGiven 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackGivenInstalled { get; private set; }

    /// <summary>后备层 ModifyPowerAmountReceived 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackReceivedInstalled { get; private set; }

    /// <summary>后备层 ApplyInternal 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackApplyInternalInstalled { get; private set; }

    /// <summary>
    /// r7: True = 覆盖证明失败, Spire1 内容主开关已在运行期被强制关闭, 且所有已安装门控对 Spire1
    /// 路径无条件硬阻断 (不再读 Powers 配置). 这是 r6 P1 要求的真实 fail-closed: 不允许无法证明
    /// 完整门控时继续进入可触发运行态. 只影响 Spire1 内容入口; vanilla/其它 mod 路径不变.
    /// </summary>
    private static volatile bool _hardFailClosed;

    /// <summary>r7: 一次性 engage guard, 保证日志与主开关写入只执行一次 (多线程安全).</summary>
    private static int _hardFailClosedEngaged;

    /// <summary>r8b: 独立于 Spire1Config setter 的确定性不可用状态; 置位后 Phase2 内容注册必须跳过.</summary>
    private static volatile bool _contentUnavailable;

    /// <summary>r8b: 主开关 setter 关闭失败时置位 (独立状态仍为不可用, 不依赖该写入成功).</summary>
    private static volatile bool _configMasterCloseFailed;

    /// <summary>r8b: 覆盖证明是否通过 (供 preflight/Phase3 复用; 不代表当前内容可用).</summary>
    private static volatile bool _coverageProven;
    /// <summary>
    /// r8c: 注册熔断面是否已由 Harmony.GetPatchInfo 实测证明安装 (ModelDb.Init prefix + getter postfix).
    /// 目标缺失/安装失败/证明失败时保持 false; 覆盖证明会把 false 当作 fail closed 处理.
    /// </summary>
    private static volatile bool _fuseInstalled;

    internal static bool HardFailClosed => _hardFailClosed;

    /// <summary>r8b: True = 已进入独立确定性不可用状态; MainFile 必须跳过 Phase2 内容注册.</summary>
    internal static bool ContentUnavailable => _contentUnavailable;
    /// <summary>r8c: 熔断总判定; 已安装补丁的配置读取前早退必须用本属性.</summary>
    internal static bool ContentUnavailableActive => _contentUnavailable || _hardFailClosed;

    /// <summary>r8c: True = 注册熔断面 (ModelDb.Init prefix + getter postfix) 已由 GetPatchInfo 实测证明安装.</summary>
    internal static bool FuseInstalled => _fuseInstalled;

    /// <summary>r8b: True = 主开关 setter 关闭失败; 独立不可用状态仍然生效.</summary>
    internal static bool ConfigMasterCloseFailed => _configMasterCloseFailed;

    /// <summary>
    /// r8b 覆盖证明 (Spire1 cardSource -> vanilla/其它 mod power, 最坏情况 applier 不可达):
    /// 新挂载只认可 central Apply 或 fallback Before 中至少一层真实安装 (两者都能在 Phase3 用
    /// Harmony.GetPatchInfo 证明安装, 且能看到 cardSource); PreApplyBridge 固定目标集合无法证明
    /// 覆盖后续 mod 加载的 BeforeApplied override, 因此只作为 best-effort, 不计入 coverage.
    /// 已有实例修改必须被能看见 cardSource 的层覆盖 (central ModifyAmount / fallback Before).
    /// Given 被引擎在 applier == null 或 applier 不在 combat state 时跳过, 且 fallback Given 无法
    /// 识别无 cardSource 的 Received/ApplyInternal, 因此这里刻意不把 Given 计入覆盖证明.
    /// </summary>
    internal static bool CardSourcePathsCovered =>
        FuseInstalled
        && (ApplyGateInstalled || FallbackHookInstalled)
        && (ModifyGateInstalled || FallbackHookInstalled);

    /// <summary>
    /// r6 P1-1 覆盖证明 (Spire1 命名空间 power 自身): 新挂载由 Apply/Before/Received/ApplyInternal
    /// 覆盖, 已有实例修改由 ModifyAmount/Before/Received 覆盖. canonical 预览早退与 Forms 例外
    /// 保持 r6-B 语义不变.
    /// </summary>
    internal static bool Spire1PowerPathsCovered =>
        (ApplyGateInstalled || FallbackHookInstalled || FallbackReceivedInstalled || FallbackApplyInternalInstalled)
        && (ModifyGateInstalled || FallbackHookInstalled || FallbackReceivedInstalled);

    internal static string DescribeCoverage() =>
        "applyGate=" + ApplyGateInstalled + ", modifyGate=" + ModifyGateInstalled +
        ", before=" + FallbackHookInstalled + ", preApplyBridge=" + FallbackPreApplyBridgeInstalled +
        ", given=" + FallbackGivenInstalled + ", received=" + FallbackReceivedInstalled +
        ", applyInternal=" + FallbackApplyInternalInstalled + ", fuse=" + FuseInstalled +
        ", contentUnavailable=" + ContentUnavailable + ", configMasterCloseFailed=" + ConfigMasterCloseFailed;

    /// <summary>
    /// r8b 收束点: 只有覆盖证明成立且独立不可用状态未置位才允许 Spire1 内容进入可触发的运行态.
    /// 证明失败时先置独立 ContentUnavailable/HardFailClosed 状态 (不依赖 Spire1Config setter 或
    /// 任何未安装 prefix), 再尝试运行期关闭 Spire1 内容主开关 (Spire1Config.EnableSts1Content=false,
    /// 不写档: BaseLib 只在用户 UI 操作时 Save). 返回 true = coverage 已证明.
    /// </summary>
    internal static bool EvaluateAndEnforceCoverage(string source)
    {
        bool cardSourceCovered = CardSourcePathsCovered;
        bool spire1PowerCovered = Spire1PowerPathsCovered;
        if (cardSourceCovered && spire1PowerCovered && FuseInstalled)
        {
            return true;
        }

        EngageHardFailClosed(
            "coverage NOT proven (" + source + "; cardSource=" + cardSourceCovered +
            ", spire1Power=" + spire1PowerCovered + ", fuse=" + FuseInstalled + "; " + DescribeCoverage() + ")");
        return false;
    }

    private static void EngageHardFailClosed(string reason)
    {
        if (Interlocked.Exchange(ref _hardFailClosedEngaged, 1) != 0)
        {
            return;
        }

        // r8b: 独立硬失败状态先于任何 Spire1Config 访问置位. 即使 setter 抛异常 (含静态构造
        // 失败导致的 TypeInitializationException), 该状态仍然生效, MainFile 会在 Phase2 内容
        // 注册之前读取它并跳过后续注册; 已安装/未安装的 prefix 都不是该状态的依赖.
        _hardFailClosed = true;
        _contentUnavailable = true;

        bool masterClosed = false;
        try
        {
            // 运行期关闭 Spire1 内容主开关 (唯一已证明的内容门 API 的单一事实来源).
            // 不调用 Save: 不写 mod_configs, 不持久化, 不影响下次启动的用户设置.
            Spire1Config.EnableSts1Content = false;
            masterClosed = true;
        }
        catch (Exception e)
        {
            _configMasterCloseFailed = true;
            MainFile.Logger.Error(
                "[Spire1] powers gate: r8b hard fail-closed could not close the content master switch (" +
                e.GetType().Name + ": " + e.Message + "); independent ContentUnavailable state is set, " +
                "Phase2 content registration will be skipped.");
        }

        MainFile.Logger.Error(
            "[Spire1] powers gate: r8b HARD FAIL-CLOSED engaged - " + reason +
            ". independent ContentUnavailable=" + ContentUnavailable +
            ", masterSwitchClosed=" + masterClosed +
            "; Spire1 power entry is blocked and Phase2 content registration is skipped. " +
            "vanilla/other-mod paths are not gated.");
    }

    /// <summary>r8b: powers gate 一次性入口 guard (整段序列完成后才置位; 中途异常保留可重试状态).</summary>
    private static readonly object EnsureLock = new();
    private static bool _installEnsured;

    /// <summary>
    /// r8b powers gate 唯一入口: 显式安装/收束中央 gate -> fallback 幂等安装/复核 -> 覆盖证明与
    /// 硬熔断. 重复进入 (preflight/Phase3 各调一次) 直接短路, 不会重复挂载 Harmony; 幂等细节由
    /// <see cref="InstallOrVerifyCentralLayer"/>,<see cref="Spire1PowersFallbackInstaller.InstallIfNeeded"/>
    /// 与 Reconcile* 保证. 只有整段序列完成后才置 _installEnsured; 中途异常保持未完成, 由调用方
    /// 进入确定性不可用路径 (不会把半成品标记为已完成).
    /// </summary>
    internal static bool EnsureInstalled(Harmony harmony)
    {
        lock (EnsureLock)
        {
            if (_installEnsured)
            {
                MainFile.Logger.Info(
                    "[Spire1] powers gate: EnsureInstalled re-entry ignored (already reconciled this process).");
                return _coverageProven;
            }

            // r8c: 注册熔断面必须最先安装并收束: 它不读 Spire1Config, 不依赖 powers prefix, 只过滤 Spire1
            // 程序集的模型类型, vanilla/其它 mod 放行. 目标缺失/安装失败时 FuseInstalled=false, 覆盖证明
            // 随之失败并进入独立不可用状态 (fail closed).
            InstallOrVerifyFuse(harmony);

            // r8b: 中央 gate 由本入口显式安装, 不再依赖 Phase3 属性扫描; 属性扫描会跳过这些类型,
            // 避免重复挂载. 安装后一律用 GetPatchInfo 收束真实状态.
            InstallOrVerifyCentralLayer(
                harmony, typeof(Spire1PowersApplyGatePatch),
                Spire1PowersApplyGatePatch.TargetMethod, Spire1PowersApplyGatePatch.PrefixMethod,
                "PowerCmd.Apply(non-generic)");
            InstallOrVerifyCentralLayer(
                harmony, typeof(Spire1PowersModifyAmountGatePatch),
                Spire1PowersModifyAmountGatePatch.TargetMethod, Spire1PowersModifyAmountGatePatch.PrefixMethod,
                "PowerCmd.ModifyAmount");

            ReconcileCentralGateInstallation(harmony);
            Spire1PowersFallbackInstaller.InstallIfNeeded(harmony);
            _coverageProven = EvaluateAndEnforceCoverage("EnsureInstalled");
            _installEnsured = true;
            return _coverageProven;
        }
    }

    /// <summary>
    /// r8b: Phase2 内容注册之前的唯一 powers gate 收束点. 先安装/收束 central gate 与 fallback
    /// 层, 再做覆盖证明. 返回 true 仅当 coverage 已证明且独立不可用状态未置位; false 时调用方
    /// 必须跳过 Phase2 内容注册 (硬失败状态已置位, 不依赖 Spire1Config setter 或任何 prefix).
    /// </summary>
    internal static bool PreflightBeforeContentRegistration(Harmony harmony)
    {
        bool covered;
        try
        {
            covered = EnsureInstalled(harmony);
        }
        catch (Exception e)
        {
            EngageHardFailClosed(
                "preflight exception (" + e.GetType().Name + ": " + e.Message + ")");
            covered = false;
        }

        bool allowed = covered && !_contentUnavailable;
        MainFile.Logger.Info(
            "[Spire1] powers gate: r8b preflight before Phase2 - coverageProven=" + covered +
            ", contentUnavailable=" + _contentUnavailable + ", contentRegistrationAllowed=" + allowed + ".");
        return allowed;
    }

    /// <summary>
    /// r8c: 注册熔断面的独立安装入口 (MainFile.Initialize 最早期与 Phase3 不可用分支调用). 幂等;
    /// 不安装 central/fallback powers gate, 不读取 Spire1Config, 只保证 ModelDb 注册熔断面.
    /// </summary>
    internal static void EnsureRegistrationFuseInstalled(Harmony harmony)
    {
        InstallOrVerifyFuse(harmony);
    }

    /// <summary>
    /// r8c: 注册熔断面安装/复核. 目标解析失败/prefix 解析失败/GetPatchInfo 证明失败时保持 FuseInstalled=false,
    /// 由覆盖证明 fail closed. 已证明安装时只复核, 不重复挂载.
    /// </summary>
    private static void InstallOrVerifyFuse(Harmony harmony)
    {
        try
        {
            MethodInfo? initTarget = Spire1ModelDbInitFusePatch.TargetMethod;
            MethodInfo? initPrefix = Spire1ModelDbInitFusePatch.PrefixMethod;
            MethodInfo? subtypesTarget = Spire1ModelDbSubtypesFusePatch.TargetMethod;
            MethodInfo? subtypesPostfix = Spire1ModelDbSubtypesFusePatch.PostfixMethod;

            if (initTarget is null || initPrefix is null || subtypesTarget is null || subtypesPostfix is null)
            {
                MainFile.Logger.Error(
                    "[Spire1] powers gate: r8c ModelDb registration fuse target/prefix reflection unresolved - fuse NOT installed.");
                _fuseInstalled = false;
                return;
            }

            bool initInstalled = VerifyLayerInstalled(harmony, initTarget, initPrefix, "ModelDb.Init fuse prefix");
            bool subtypesInstalled = VerifyPostfixLayerInstalled(harmony, subtypesTarget, subtypesPostfix, "ModelDb.AllAbstractModelSubtypes fuse postfix");

            if (!initInstalled)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(Spire1ModelDbInitFusePatch)).Patch();
                }
                catch (Exception e)
                {
                    MainFile.Logger.Error(
                        "[Spire1] powers gate: r8c ModelDb.Init fuse patch failed: " + e.Message);
                }
            }

            if (!subtypesInstalled)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(Spire1ModelDbSubtypesFusePatch)).Patch();
                }
                catch (Exception e)
                {
                    MainFile.Logger.Error(
                        "[Spire1] powers gate: r8c ModelDb getter fuse patch failed: " + e.Message);
                }
            }

            _fuseInstalled = VerifyLayerInstalled(harmony, initTarget, initPrefix, "ModelDb.Init fuse prefix")
                && VerifyPostfixLayerInstalled(harmony, subtypesTarget, subtypesPostfix, "ModelDb.AllAbstractModelSubtypes fuse postfix");

            if (_fuseInstalled)
            {
                MainFile.Logger.Info("[Spire1] powers gate: r8c ModelDb registration fuse verified installed.");
            }
            else
            {
                MainFile.Logger.Error(
                    "[Spire1] powers gate: r8c ModelDb registration fuse NOT proven installed - coverage will fail closed.");
            }
        }
        catch (Exception e)
        {
            _fuseInstalled = false;
            MainFile.Logger.Error(
                "[Spire1] powers gate: r8c ModelDb registration fuse threw (" + e.GetType().Name + ": " + e.Message + ") - treated as NOT installed.");
        }
    }

    /// <summary>
    /// r8b: 单层中央 gate 安装/复核. 已由 GetPatchInfo 证明时只复核不重复挂载; 未安装时尝试
    /// CreateClassProcessor(type).Patch(), 失败只记录, 由随后的 ReconcileCentralGateInstallation
    /// 以实测结果收束 (不把 Patch() 未抛异常当作成功).
    /// </summary>
    private static void InstallOrVerifyCentralLayer(
        Harmony harmony, Type type, MethodInfo? target, MethodInfo? prefix, string layer)
    {
        if (target is null || prefix is null)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: " + layer +
                " target/prefix reflection unresolved - central gate NOT installed.");
            return;
        }

        if (VerifyLayerInstalled(harmony, target, prefix, layer))
        {
            return;
        }

        try
        {
            harmony.CreateClassProcessor(type).Patch();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: central patch " + type.Name + " failed: " + e.Message);
        }
    }

    /// <summary>
    /// r8b: 独立确定性不可用入口 (Phase1/Phase3 全局异常等). 与覆盖证明失败走同一状态机:
    /// 先置独立状态, 再尝试关闭主开关; setter 失败不影响不可用状态.
    /// </summary>
    internal static void MarkDeterministicUnavailable(string reason)
    {
        EngageHardFailClosed(reason);
    }

    /// <summary>True = 两个主漏斗目标都真实安装; false = 需要启用后备层.</summary>
    internal static bool CentralGateOperational => ApplyGateInstalled && ModifyGateInstalled;

    internal static void ReportApplyGateUnavailable(string reason)
    {
        ApplyGateInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: PowerCmd.Apply(non-generic) " + reason + " - apply gate NOT installed; " +
            "Spire1 power application is NOT intercepted by the central gate.");
    }

    internal static void ReportModifyGateUnavailable(string reason)
    {
        ModifyGateInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: PowerCmd.ModifyAmount " + reason + " - modify gate NOT installed; " +
            "Spire1 power modification is NOT intercepted by the central gate.");
    }

    internal static void ReportFallbackPreApplyBridgeUnavailable(string reason)
    {
        FallbackPreApplyBridgeInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback PreApplyBridge " + reason +
            " - fallback NOT installed; PreApplyBridge zeroing is NOT active.");
    }

    internal static void ReportFallbackHookUnavailable(string reason)
    {
        FallbackHookInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback BeforePowerAmountChanged " + reason +
            " - fallback NOT installed; Spire1 power application is NOT intercepted.");
    }

    internal static void ReportFallbackGivenUnavailable(string reason)
    {
        FallbackGivenInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback ModifyPowerAmountGiven " + reason +
            " - fallback NOT installed; Spire1 card power amount given is NOT intercepted.");
    }

    internal static void ReportFallbackReceivedUnavailable(string reason)
    {
        FallbackReceivedInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback ModifyPowerAmountReceived " + reason +
            " - fallback NOT installed; Spire1 power modification is NOT intercepted.");
    }


    /// <summary>
    /// r7 安装收束 (central): 用 Harmony.GetPatchInfo 实测 PowerCmd.Apply / PowerCmd.ModifyAmount
    /// 目标上属于本 Harmony id 的 prefix 是否真实存在, 并据此设置 ApplyGateInstalled /
    /// ModifyGateInstalled. 这是安装成功不变量的唯一判定点: Cleanup 的 null-exception 不再作为成功证据.
    /// 目标字段为空 (解析失败) 时保持 NOT installed.
    /// </summary>
    internal static void ReconcileCentralGateInstallation(Harmony harmony)
    {
        ApplyGateInstalled = VerifyLayerInstalled(
            harmony, Spire1PowersApplyGatePatch.TargetMethod,
            Spire1PowersApplyGatePatch.PrefixMethod, "PowerCmd.Apply(non-generic)");
        ModifyGateInstalled = VerifyLayerInstalled(
            harmony, Spire1PowersModifyAmountGatePatch.TargetMethod,
            Spire1PowersModifyAmountGatePatch.PrefixMethod, "PowerCmd.ModifyAmount");
    }

    /// <summary>
    /// r7 安装收束 (fallback): 逐层用 GetPatchInfo 实测 prefix 是否真实安装. 目标缺失/依赖层缺失/
    /// prefix 未绑定一律为 false, 不允许把 "CreateClassProcessor 没抛异常" 当成安装成功.
    /// </summary>
    internal static void ReconcileFallbackInstallation(Harmony harmony)
    {
        FallbackHookInstalled = VerifyLayerInstalled(
            harmony, Spire1PowersFallbackBeforePatch.TargetMethod,
            Spire1PowersFallbackBeforePatch.PrefixMethod, "fallback BeforePowerAmountChanged");
        FallbackPreApplyBridgeInstalled = VerifyPreApplyBridgeInstalled(harmony);
        FallbackGivenInstalled = VerifyLayerInstalled(
            harmony, Spire1PowersFallbackGivenPatch.TargetMethod,
            Spire1PowersFallbackGivenPatch.PrefixMethod, "fallback ModifyPowerAmountGiven");
        FallbackReceivedInstalled = VerifyLayerInstalled(
            harmony, Spire1PowersFallbackReceivedPatch.TargetMethod,
            Spire1PowersFallbackReceivedPatch.PrefixMethod, "fallback ModifyPowerAmountReceived");
        FallbackApplyInternalInstalled = VerifyLayerInstalled(
            harmony, Spire1PowersFallbackApplyInternalPatch.TargetMethod,
            Spire1PowersFallbackApplyInternalPatch.PrefixMethod, "fallback ApplyInternal");
    }

    /// <summary>
    /// 实测单目标单 prefix 层是否安装: 返回 true 仅当 GetPatchInfo 能解析, Owners 含本 Harmony id,
    /// 且 Prefixes 中存在 owner 与 PatchMethod 都精确匹配的条目. 任何异常/形状漂移都返回 false
    /// (fail closed), 不抛出.
    /// </summary>
    private static bool VerifyLayerInstalled(Harmony harmony, MethodInfo? target, MethodInfo? prefix, string layer)
    {
        if (target is null || prefix is null)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: r7 cannot verify " + layer + " (target/prefix reflection unresolved).");
            return false;
        }

        try
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(target);
            if (info is null
                || !info.Owners.Contains(harmony.Id)
                || !info.Prefixes.Any(patch =>
                    patch.owner == harmony.Id && patch.PatchMethod is { } patchMethod && (patchMethod == prefix || patchMethod.Equals(prefix))))
            {
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: r7 verification of " + layer + " threw (" +
                e.GetType().Name + ": " + e.Message + "); treated as NOT installed.");
            return false;
        }
    }

    /// <summary>
    /// r8c 实测单目标单 postfix 层是否安装: 返回 true 仅当 GetPatchInfo 能解析, Owners 含本 Harmony id,
    /// 且 Postfixes 中存在 owner 与 PatchMethod 都精确匹配的条目. 任何异常/形状漂移都返回 false (fail closed).
    /// </summary>
    private static bool VerifyPostfixLayerInstalled(Harmony harmony, MethodInfo? target, MethodInfo? postfix, string layer)
    {
        if (target is null || postfix is null)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: r8c cannot verify " + layer + " (target/postfix reflection unresolved).");
            return false;
        }

        try
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(target);
            if (info is null
                || !info.Owners.Contains(harmony.Id)
                || !info.Postfixes.Any(patch =>
                    patch.owner == harmony.Id && patch.PatchMethod is { } patchMethod && (patchMethod == postfix || patchMethod.Equals(postfix))))
            {
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: r8c verification of " + layer + " threw (" +
                e.GetType().Name + ": " + e.Message + "); treated as NOT installed.");
            return false;
        }
    }

    /// <summary>
    /// r7 安装收束 (fallback PreApplyBridge): 5 个目标方法每个都必须解析到精确的 prefix 绑定;
    /// 任一缺失即整层 false (与 Prepare 的 5/5 语义一致).
    /// </summary>
    private static bool VerifyPreApplyBridgeInstalled(Harmony harmony)
    {
        try
        {
            MethodInfo[] targets = Spire1PowersFallbackPreApplyBridgePatch.TargetMethodsResolved();
            if (targets.Length != Spire1PowersFallbackPreApplyBridgePatch.ExpectedTargetCount)
            {
                return false;
            }

            MethodInfo? prefix = Spire1PowersFallbackPreApplyBridgePatch.PrefixMethod;
            if (prefix is null)
            {
                return false;
            }

            foreach (MethodInfo target in targets)
            {
                HarmonyLib.Patches? info = Harmony.GetPatchInfo(target);
                if (info is null
                    || !info.Owners.Contains(harmony.Id)
                    || !info.Prefixes.Any(patch =>
                        patch.owner == harmony.Id && patch.PatchMethod is { } patchMethod && (patchMethod == prefix || patchMethod.Equals(prefix))))
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: r7 verification of fallback PreApplyBridge threw (" +
                e.GetType().Name + ": " + e.Message + "); treated as NOT installed.");
            return false;
        }
    }
    internal static void ReportFallbackApplyInternalUnavailable(string reason)
    {
        FallbackApplyInternalInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback ApplyInternal " + reason +
            " - fallback NOT installed; direct ApplyInternal mount is NOT intercepted.");
    }

    /// <summary>True = 本 mod 拥有,且受 powers 组门控管辖的 PowerModel 类型.</summary>
    internal static bool IsGatedPower(PowerModel? power)
    {
        string? ns = power?.GetType().Namespace;
        return ns is not null && ns.StartsWith(ModNamespacePrefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// True = 这次 PowerCmd 调用由 Spire1 卡牌发起 (cardSource 是 Spire1 卡/诅咒).
    /// 用于覆盖 "Spire1 卡直接应用 vanilla power" 的路径 (Inflame/Footwork/PoisonedStab 等):
    /// 这些调用的最终 power 是 MegaCrit.* 类型, 无法按 power 类型识别.
    /// 只按 cardSource 的声明命名空间判定: vanilla 卡/其它 mod 卡永不命中.
    /// </summary>
    private static bool IsSpire1CardSource(CardModel? card)
    {
        if (card is null)
        {
            return false;
        }

        string? ns = card.GetType().Namespace;
        return ns is not null
            && (ns.Equals(CardsNamespace, StringComparison.Ordinal)
                || ns.StartsWith(CardsNamespace + ".", StringComparison.Ordinal));
    }

    /// <summary>
    /// True = 该 power 实例由已加载的独立 Forms 程序集声明. 判定只依据程序集 simple name (以及
    /// collectible 程序集是否仍在载), 与 Forms entry point 的签名校验或运行期 IsAvailable 无关:
    /// 签名校验失败/Retryable/Terminal/ShuttingDown 期间, 独立 Forms power 仍被识别为 Forms 类型.
    /// 独立 Forms power 不属于 Spire1 powers gate: 普通局也不拦截它 (它由 Forms 自持生命周期).
    /// Forms 程序集缺失 (或类型为 null) 时返回 false, 该 power 就按普通外部 mod power 处理
    /// (Spire1 前缀不命中).
    /// </summary>
    private static bool IsFormsPower(PowerModel power) => FormsCompatibilityBridge.IsFormsType(power.GetType());

    /// <summary>新挂载门控: powers 组关闭时拦截 Spire1 power 的 Apply.</summary>
    internal static bool ShouldBlockApply(PowerModel? power, Creature? target, CardModel? cardSource)
    {
        if (power is null)
        {
            return false; // 无 power 实例: 交给原方法自身的空值语义
        }

        // 独立 Forms 程序集的 power 不属于 Spire1 powers gate: 它由 Forms 自持生命周期,
        // 即使是普通局 (未选形态修正) 也不应被 Spire1 的前缀门控拦截.
        if (IsFormsPower(power!))
        {
            return false; // 独立 Forms 程序集, 见类型注释
        }

        if (!IsGatedPower(power) && !IsSpire1CardSource(cardSource))
        {
            return false; // 原版/其它 mod power 且非 Spire1 卡触发: 永不拦截
        }

        // r7 硬熔断优先: 覆盖证明失败后, 凡能归入 Spire1 门控面的新挂载一律无条件阻断,
        // 不再依赖配置读取或 cardSource 分类 (vanilla/其它 mod 路径仍由下方分类返回 false).
        if (HardFailClosed && IsGatedPower(power))
        {
            return true;
        }

        if (!HardFailClosed && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return false;
        }

        if (!_blockedApplyLogged)
        {
            _blockedApplyLogged = true;
            MainFile.Logger.Info(
                "[Spire1] powers gate: blocked PowerCmd.Apply for " + power!.GetType().Name +
                " (powers content group off)");
        }
        return true;
    }

    /// <summary>
    /// 已有实例门控: powers 组关闭时拦截对 Spire1 power 的正向叠加 (offset &gt; 0);
    /// 削减/自减/移除保持原逻辑, 让已存在实例能正常收尾.
    /// </summary>
    internal static bool ShouldBlockModify(PowerModel? power, decimal offset, CardModel? cardSource)
    {
        if (power is null)
        {
            return false;
        }

        bool spire1Power = IsGatedPower(power);
        bool spire1Source = IsSpire1CardSource(cardSource);

        if (offset == 0m)
        {
            return false; // 无数量变化: 保留引擎自身的清理语义 (ShouldRemoveDueToAmount 等)
        }

        if (offset > 0m)
        {
            if (!spire1Power && !spire1Source)
            {
                return false; // 原版/其它 mod 的正向叠加: 永不拦截
            }
        }
        else
        {
            // offset < 0: Spire1 power 实例自身的倒计时/自减/移除 (cardSource 为 null) 必须放行,
            // 这是旧存档与局中切开关的收尾语义 (C08 规则). 但 Spire1 卡新施加到 vanilla power 上的
            // 减益仍属 Spire1 内容触发的效果, 必须拦截; 原版/其它 mod 的 cardSource 永不命中.
            if (spire1Power || !spire1Source)
            {
                return false;
            }
        }

        // 独立 Forms 程序集的 power 与 Apply 相同: 不属于 Spire1 powers gate, 直接放行.
        if (IsFormsPower(power!))
        {
            return false;
        }

        if (!HardFailClosed && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return false;
        }

        if (!_blockedModifyLogged)
        {
            _blockedModifyLogged = true;
            MainFile.Logger.Info(
                "[Spire1] powers gate: blocked PowerCmd.ModifyAmount(+) for " + power!.GetType().Name +
                " (powers content group off)");
        }
        return true;
    }

    /// <summary>
    /// r7 严格校验: 只有静态 + 带 [HarmonyPrefix] 的成员才被接受为安装收束用的 prefix 反射句柄;
    /// 名称漂移/签名漂移/属性缺失一律返回 null (fail closed), 绝不猜测.
    /// </summary>
    internal static bool IsDeclaredPatchMethod(MethodInfo? method)
    {
        if (method is null || !method.IsStatic)
        {
            return false;
        }

        try
        {
            return method.GetCustomAttributes(typeof(HarmonyPrefix), false).Length == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// r8c 严格校验 (postfix 版本): 只有静态 + 带 [HarmonyPostfix] 的成员才被接受为安装收束用的 postfix 反射句柄;
    /// 名称漂移/签名漂移/属性缺失一律返回 null (fail closed).
    /// </summary>
    internal static bool IsDeclaredPostfixMethod(MethodInfo? method)
    {
        if (method is null || !method.IsStatic)
        {
            return false;
        }

        try
        {
            return method.GetCustomAttributes(typeof(HarmonyPostfix), false).Length == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }
    /// <summary>
    /// True = 该类型是 fail-closed fallback 补丁类之一 (Before/PreApplyBridge/Given/Received/ApplyInternal).
    /// MainFile Phase3 的属性扫描必须跳过它们, 只允许
    /// <see cref="Spire1PowersFallbackInstaller.InstallIfNeeded"/> 在中央漏斗不完整时安装.
    /// </summary>
    internal static bool IsFallbackPatchType(Type type)
    {
        return type == typeof(Spire1PowersFallbackBeforePatch)
            || type == typeof(Spire1PowersFallbackPreApplyBridgePatch)
            || type == typeof(Spire1PowersFallbackGivenPatch)
            || type == typeof(Spire1PowersFallbackReceivedPatch)
            || type == typeof(Spire1PowersFallbackApplyInternalPatch);
    }

    /// <summary>
    /// r8b: True = 该类型由 powers gate preflight 显式安装 (central Apply/ModifyAmount 或 fallback 类);
    /// MainFile Phase3 属性扫描必须跳过它们, 避免与 preflight 重复挂载.
    /// r8e: 独立不可用安全过滤器 (cards/relics/potions/events) 不在此列: 正常可用路径由 Phase3
    /// 属性扫描安装, 不可用路径由 <see cref="EnsureUnavailableSafetyFiltersInstalled"/> 显式安装;
    /// 两条控制流互斥 (不可用分支在扫描前 return), 不存在重复挂载.
    /// </summary>
    internal static bool IsGateManagedPatchType(Type type)
    {
        return type == typeof(Spire1PowersApplyGatePatch)
            || type == typeof(Spire1PowersModifyAmountGatePatch)
            || type == typeof(Spire1ModelDbInitFusePatch)
            || type == typeof(Spire1ModelDbSubtypesFusePatch)
            || IsFallbackPatchType(type);
    }

    /// <summary>
    /// r8e: True = 该类型是独立不可用安全过滤器之一 (cards/relics/potions/events 的注册后运行期提供路径).
    /// 不可用状态 (熔断面安装失败/部分安装/覆盖证明失败) 下由
    /// <see cref="EnsureUnavailableSafetyFiltersInstalled"/> 的显式白名单安装并逐目标证明.
    /// 本判定只服务不可用路径; 不参与 MainFile Phase3 的 gate-managed 跳过集合.
    /// </summary>
    internal static bool IsUnavailableSafetyFilterType(Type type)
    {
        return type == typeof(Spire1SharedPoolGatePatch)
            || type == typeof(Spire1PotionPoolGatePatch)
            || type == typeof(Spire1RelicPoolGatePatch)
            || type == typeof(Sts1EventToggleGenerateRoomsPatch)
            || type == typeof(Sts1EventTogglePullNextEventPrefixPatch)
            || type == typeof(Sts1EventTogglePullNextEventPostfixPatch);
    }

    /// <summary>
    /// r8d: 不可用状态的独立安全过滤器白名单. 每个过滤器在读取 Spire1Config 之前先判
    /// <see cref="ContentUnavailableActive"/> 并强制过滤本 mod 类型, 保留 vanilla/其它 mod.
    /// 安装后必须逐目标用 Harmony.GetPatchInfo 精确证明; 任一目标无法证明时返回 false,
    /// 调用方必须保持 fail closed 并如实记录未闭合边界.
    /// </summary>
    private static readonly (Type Type, MethodInfo? Target, MethodInfo? Patch, string Layer)[] UnavailableSafetyFilters =
    {
        (typeof(Spire1SharedPoolGatePatch), Spire1SharedPoolGatePatch.TargetMethod, Spire1SharedPoolGatePatch.PostfixMethod, "ColorlessCardPool.GetUnlockedCards"),
        (typeof(Spire1PotionPoolGatePatch), Spire1PotionPoolGatePatch.TargetMethod, Spire1PotionPoolGatePatch.PostfixMethod, "PotionPoolModel.GetUnlockedPotions"),
        (typeof(Spire1RelicPoolGatePatch), Spire1RelicPoolGatePatch.TargetMethod, Spire1RelicPoolGatePatch.PostfixMethod, "RelicPoolModel.GetUnlockedRelics"),
        (typeof(Sts1EventToggleGenerateRoomsPatch), Sts1EventToggleGenerateRoomsPatch.TargetMethod, Sts1EventToggleGenerateRoomsPatch.PostfixMethod, "ActModel.GenerateRooms"),
        (typeof(Sts1EventTogglePullNextEventPrefixPatch), Sts1EventTogglePullNextEventPrefixPatch.TargetMethod, Sts1EventTogglePullNextEventPrefixPatch.PrefixMethod, "ActModel.PullNextEvent(prefix)"),
        (typeof(Sts1EventTogglePullNextEventPostfixPatch), Sts1EventTogglePullNextEventPostfixPatch.TargetMethod, Sts1EventTogglePullNextEventPostfixPatch.PostfixMethod, "ActModel.PullNextEvent(postfix)")
    };

    /// <summary>
    /// r8d: 不可用状态下显式安装 cards/relics/potions/events 安全过滤器, 逐目标用 GetPatchInfo 精确证明.
    /// 返回 true 仅当白名单全部证明安装; 任一目标缺失/部分安装/异常时返回 false (fail closed).
    /// 本方法不读 Spire1Config.
    /// </summary>
    internal static bool EnsureUnavailableSafetyFiltersInstalled(Harmony harmony)
    {
        if (!ContentUnavailableActive)
        {
            return true;
        }

        lock (EnsureLock)
        {
            foreach (var (type, target, patch, layer) in UnavailableSafetyFilters)
            {
                if (!IsUnavailableSafetyFilterType(type))
                {
                    MainFile.Logger.Error(
                        "[Spire1] r8e unavailable safety filter " + layer +
                        " type is outside the unavailable whitelist predicate - NOT proven installed.");
                    return false;
                }

                if (patch is null || target is null)
                {
                    MainFile.Logger.Error(
                        "[Spire1] r8d unavailable safety filter " + layer +
                        " target/patch reflection unresolved - NOT proven installed.");
                    return false;
                }

                bool isPrefix = patch.GetCustomAttributes(typeof(HarmonyPrefix), false).Length == 1;
                bool installed = isPrefix
                    ? VerifyLayerInstalled(harmony, target, patch, layer)
                    : VerifyPostfixLayerInstalled(harmony, target, patch, layer);

                if (!installed)
                {
                    try
                    {
                        harmony.CreateClassProcessor(type).Patch();
                    }
                    catch (Exception e)
                    {
                        MainFile.Logger.Error(
                            "[Spire1] r8d unavailable safety filter " + layer + " patch failed: " + e.Message);
                    }

                    installed = isPrefix
                        ? VerifyLayerInstalled(harmony, target, patch, layer)
                        : VerifyPostfixLayerInstalled(harmony, target, patch, layer);
                }

                if (!installed)
                {
                    MainFile.Logger.Error(
                        "[Spire1] r8d unavailable safety filter " + layer +
                        " NOT proven installed - unavailable content closure is incomplete.");
                    return false;
                }
            }

            MainFile.Logger.Error(
                "[Spire1] r8d unavailable safety filters proven installed: cards/relics/potions/events " +
                "runtime offer paths are closed for Spire1 types (vanilla/other-mod types preserved).");
            return true;
        }
    }
    // ------------------------------------------------------------------
    // C12 r6-B fallback support. 仅当中央漏斗 (非泛型 PowerCmd.Apply +
    // PowerCmd.ModifyAmount) 任一目标未安装时, MainFile.Phase3 才安装后备层.
    // 本层不保存跨调用状态 (无 token/计数/CWT); Before 命中即 faulted Task 硬阻断,
    // Given/Received/ApplyInternal 各自只用本次调用参数与 HasOwner/IsCanonical 判定.
    // ------------------------------------------------------------------

    /// <summary>
    /// 后备层 BeforePowerAmountChanged 判定: 只用本次调用的参数 (power/amount/target/cardSource)
    /// 与实例的稳定状态 (HasOwner/IsCanonical) 做本地判定, 不保存任何跨调用状态.
    /// 命中时调用方直接 faulted Task 硬阻断: 本层不再预测后续 Given/Received 是否可达,
    /// 因为引擎没有跨方法调用身份, applier/combatState 条件在 await 之后重新求值
    /// (PowerCmd.cs:124-131, :231-238), 预测放行会在异步重入下失去归零层.
    /// </summary>
    internal static bool ShouldBlockFallbackBefore(
        PowerModel? power, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (power is null || power.IsCanonical)
        {
            // canonical 实例只出现在预览路径 (PowerVar.UpdateCardPreview), 不是安装调用.
            return false;
        }

        // HasOwner 是稳定分类: ApplyInternal 设置 Owner 后, RemoveInternal 不会清空它
        // (PowerModel.cs:278, :575-580; PowerCmd.cs:297 移除后仍读 power.Owner).
        // 因此 await 期间实例被移除也不会把旧实例负 offset 收尾重分类成新挂载.
        return HasOwner(power)
            ? ShouldBlockModify(power, amount, cardSource)
            : ShouldBlockApply(power, target, cardSource);
    }

    /// <summary>
    /// 后备层 PreApplyBridge 判定: 只拦截 "Spire1 cardSource -> 非 Spire1 power" 且 powers 组关闭/
    /// 降级时的新挂载调用. Spire1 自有 power 的新挂载由 Received/ApplyInternal 覆盖, 不走本层;
    /// 非 Spire1 cardSource 永不命中. 该层不需要 Before/Given/Received 的调用身份, 只用
    /// 本次 BeforeApplied 的实例/目标/cardSource 做本地判定, 因此没有跨异步调用错配.
    /// </summary>
    internal static bool ShouldBlockFallbackPreApplyBridge(
        PowerModel? power, Creature? target, CardModel? cardSource)
    {
        if (power is null || power.IsCanonical)
        {
            return false;
        }

        if (IsGatedPower(power) || !IsSpire1CardSource(cardSource))
        {
            return false;
        }

        // Spire1/Forms power 已在上方 IsGatedPower 分支返回 false, 由 Received/ApplyInternal 覆盖.
        if (!HardFailClosed && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 后备层 Given 判定 (Hook.ModifyPowerAmountGiven, 同步无 await): 命中时把 amount 归零并让调用方
    /// 写非空 out modifiers. 只用本次调用的 cardSource/power 判定, 不消费任何跨调用状态.
    /// canonical 预览调用 (PowerVar.cs:25) 不携带安装身份, 直接放行, 不归零.
    /// </summary>
    internal static bool ShouldBlockFallbackGiven(
        PowerModel? power, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (power is null || power.IsCanonical)
        {
            return false;
        }

        return HasOwner(power)
            ? ShouldBlockModify(power, amount, cardSource)
            : ShouldBlockApply(power, target, cardSource);
    }

    /// <summary>
    /// 后备层 ModifyPowerAmountReceived 判定: 只用本次调用的实例/目标/amount 做本地判定,
    /// 不消费任何跨调用状态. Spire1 卡源触发 vanilla power 的路径由 Given 层按 cardSource 覆盖.
    /// HasOwner = 已有实例 -> Modify 规则 (负 offset 是旧实例收尾, 必须放行);
    /// 无 Owner = 新挂载 -> Apply 规则 (Spire1 power 拦截; vanilla power 无 cardSource 不可判定).
    /// </summary>
    internal static bool ShouldBlockFallbackReceived(PowerModel? power, Creature? target, decimal amount)
    {
        if (power is null || power.IsCanonical)
        {
            return false;
        }

        return HasOwner(power)
            ? ShouldBlockModify(power, amount, null)
            : ShouldBlockApply(power, target, null);
    }

    /// <summary>
    /// True = 该实例已被 ApplyInternal 设置过 Owner (含已从 Powers 列表移除的旧实例).
    /// 与旧 IsAttached (Owner.Powers.Contains) 不同: 本判定在异步移除后保持稳定, 不会把
    /// 旧实例的负 offset 收尾重新分类成新挂载; canonical 实例读 Owner 会抛, 调用方已先按
    /// IsCanonical 早退. 异常实例按未附加处理, 不把 hook 前缀异常升级成崩溃.
    /// </summary>
    private static bool HasOwner(PowerModel power)
    {
        try
        {
            return power.Owner is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 后备层 ApplyInternal 类型级兜底: 只覆盖 Spire1 命名空间 power (Forms 形态局例外);
    /// 直接 ApplyInternal 的 vanilla power 没有 cardSource 可判定, 属明确记录的残余边界.
    /// canonical 实例不是安装调用, 直接放行.
    /// </summary>
    internal static bool ShouldBlockFallbackApplyInternal(PowerModel? power, Creature? owner)
    {
        if (power is null || power.IsCanonical)
        {
            return false;
        }

        return ShouldBlockApply(power, owner, null);
    }
}

/// <summary>
/// 单目标补丁类: 非泛型 <c>PowerCmd.Apply</c> (新挂载必经漏斗).按"非泛型 + 7 参数"精确解析,
/// 与 AutoAnthony 的 PowerCmd 反射选择同款写法; 目标缺失/歧义时不安装并记录 Error.
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersApplyGatePatch
{
    private static readonly MethodInfo? Target = Resolve();

    /// <summary>r7 安装收束用: 目标方法 (解析失败为 null).</summary>
    internal static MethodInfo? TargetMethod => Target;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersApplyGatePatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo? match = null;
            foreach (MethodInfo candidate in typeof(PowerCmd)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != nameof(PowerCmd.Apply) || candidate.IsGenericMethod)
                {
                    continue;
                }

                if (!IsExactSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    Spire1PowersGate.ReportApplyGateUnavailable(
                        "target is ambiguous (multiple exact matches)");
                    return null;
                }

                match = candidate;
            }

            return match;
        }
        catch (Exception e)
        {
            Spire1PowersGate.ReportApplyGateUnavailable(
                "resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    /// <summary>
    /// 精确版本合同: 返回类型, 参数类型与参数名全部匹配才接受; 任何漂移都判为 NOT installed,
    /// 促使 MainFile 的 fail-closed 接线启用 fallback 层.
    /// </summary>
    private static bool IsExactSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(Task))
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 7
            && p[0].ParameterType == typeof(PlayerChoiceContext) && p[0].Name == "choiceContext"
            && p[1].ParameterType == typeof(PowerModel) && p[1].Name == "power"
            && p[2].ParameterType == typeof(Creature) && p[2].Name == "target"
            && p[3].ParameterType == typeof(decimal) && p[3].Name == "amount"
            && p[4].ParameterType == typeof(Creature) && p[4].Name == "applier"
            && p[5].ParameterType == typeof(CardModel) && p[5].Name == "cardSource"
            && p[6].ParameterType == typeof(bool) && p[6].Name == "silent";
    }

    private static bool Prepare()
    {
        if (Target is null)
        {
            Spire1PowersGate.ReportApplyGateUnavailable("target not found");
            return false;
        }
        return true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    [HarmonyPrefix]
    private static bool Prefix(PowerModel power, Creature target, CardModel? cardSource, ref Task __result)
    {
        if (!Spire1PowersGate.ShouldBlockApply(power, target, cardSource))
        {
            return true;
        }

        // async Task 方法: 必须显式给出已完成的 Task, 不能让调用方 await null.
        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// 单目标补丁类: <c>PowerCmd.ModifyAmount</c> (已有实例修改与 Decrement/TickDownDuration 必经漏斗).
/// 被拦截时返回 Task&lt;int&gt;(0): 泛型 Apply&lt;T&gt; 据此把结果置 null, 与引擎"数量归零"语义一致.
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersModifyAmountGatePatch
{
    private static readonly MethodInfo? Target = Resolve();

    /// <summary>r7 安装收束用: 目标方法 (解析失败为 null).</summary>
    internal static MethodInfo? TargetMethod => Target;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersModifyAmountGatePatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo? match = null;
            foreach (MethodInfo candidate in typeof(PowerCmd)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != nameof(PowerCmd.ModifyAmount) || !IsExactSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    Spire1PowersGate.ReportModifyGateUnavailable(
                        "target is ambiguous (multiple exact matches)");
                    return null;
                }

                match = candidate;
            }

            return match;
        }
        catch (Exception e)
        {
            Spire1PowersGate.ReportModifyGateUnavailable(
                "resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    /// <summary>精确版本合同: 返回 Task&lt;int&gt;, 6 个参数的类型与名称全部匹配.</summary>
    private static bool IsExactSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(Task<int>))
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 6
            && p[0].ParameterType == typeof(PlayerChoiceContext) && p[0].Name == "choiceContext"
            && p[1].ParameterType == typeof(PowerModel) && p[1].Name == "power"
            && p[2].ParameterType == typeof(decimal) && p[2].Name == "offset"
            && p[3].ParameterType == typeof(Creature) && p[3].Name == "applier"
            && p[4].ParameterType == typeof(CardModel) && p[4].Name == "cardSource"
            && p[5].ParameterType == typeof(bool) && p[5].Name == "silent";
    }

    private static bool Prepare()
    {
        if (Target is null)
        {
            Spire1PowersGate.ReportModifyGateUnavailable("target not found");
            return false;
        }
        return true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    [HarmonyPrefix]
    private static bool Prefix(PowerModel power, decimal offset, CardModel? cardSource, ref Task<int> __result)
    {
        if (!Spire1PowersGate.ShouldBlockModify(power, offset, cardSource))
        {
            return true;
        }

        __result = Task.FromResult(0);
        return false;
    }
}

/// <summary>
/// C12 r6-B fallback 层 A: Hook.BeforePowerAmountChanged 只做本地判定, 不保存任何跨调用状态.
/// 命中时直接以 faulted Task 硬阻断本次调用, 不再预测 Given/Received 是否可达:
/// 引擎没有跨方法调用身份, 异步重入会改变 applier/combatState 与实例挂载状态.
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersFallbackBeforePatch
{
    private static readonly MethodInfo? Target = Resolve();

    /// <summary>r7 安装收束用: 目标方法 (解析失败为 null).</summary>
    internal static MethodInfo? TargetMethod => Target;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersFallbackBeforePatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo? match = null;
            foreach (MethodInfo candidate in typeof(Hook)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != nameof(Hook.BeforePowerAmountChanged)
                    || !IsExactSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    Spire1PowersGate.ReportFallbackHookUnavailable(
                        "target is ambiguous (multiple exact matches)");
                    return null;
                }

                match = candidate;
            }

            return match;
        }
        catch (Exception e)
        {
            Spire1PowersGate.ReportFallbackHookUnavailable(
                "resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    private static bool IsExactSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(Task))
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 6
            && p[0].ParameterType == typeof(ICombatState) && p[0].Name == "combatState"
            && p[1].ParameterType == typeof(PowerModel) && p[1].Name == "power"
            && p[2].ParameterType == typeof(decimal) && p[2].Name == "amount"
            && p[3].ParameterType == typeof(Creature) && p[3].Name == "target"
            && p[4].ParameterType == typeof(Creature) && p[4].Name == "applier"
            && p[5].ParameterType == typeof(CardModel) && p[5].Name == "cardSource";
    }

    private static bool Prepare()
    {
        if (Target is null)
        {
            return false;
        }
        return true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    private static bool Prefix(
        PowerModel power,
        decimal amount,
        Creature target,
        CardModel? cardSource,
        ref Task __result)
    {
        if (!Spire1PowersGate.ShouldBlockFallbackBefore(power, amount, target, cardSource))
        {
            return true;
        }

        // r6-B: 命中即硬阻断. 不再预测后续 Given/Received 是否可达, 也不放行任何
        // "已安装归零层会覆盖该调用" 的判断 -- 引擎没有跨方法调用身份, 预测在异步重入下不可靠.
        __result = Task.FromException(new InvalidOperationException(
            "Spire1 powers gate: fallback BeforePowerAmountChanged blocked a Spire1 power " +
            "application (powers content group off or gate degraded; P1-C12-01)."));
        return false;
    }
}


/// <summary>
/// C12 r6-B fallback 层 A2: PreApplyBridge. 目标集合是 <c>PowerModel.BeforeApplied</c> 的
/// 基类虚方法本体 (覆盖所有不覆写它的 power, 如 Strength/Poison/Dexterity) 加上引擎程序集内
/// 4 个覆写 (TemporaryStrength/Dexterity/Focus/VoidForm), 共 5 个非泛型方法.
/// 该层只覆盖 "Spire1 cardSource -> 非 Spire1 power" 的新挂载路径 (Spire1 自有 power 由
/// Received/ApplyInternal 覆盖); 命中时返回 faulted Task 并跳过原 BeforeApplied, 使调用方在
/// <c>PowerCmd.Apply</c> 的 ApplyInternal 之前抛出, 从而不挂载.
/// 不使用 amount=0 归零: BeforeApplied 的 amount 是按值传递, 归零无法阻止外层 ApplyInternal.
/// 5 个目标均为非泛型独立方法, 不做泛型 patch; 任一目标缺失则整层报告 NOT installed.
/// r8b: 后续 mod 加载的 BeforeApplied override 在 Phase3 无法完整枚举, 该固定目标集合只作
/// best-effort 第二道阻断, 不计入 cardSource coverage 证明 (见 Spire1PowersGate.CardSourcePathsCovered).
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersFallbackPreApplyBridgePatch
{
    private static readonly Type[] TargetTypes =
    {
        typeof(PowerModel),
        typeof(MegaCrit.Sts2.Core.Models.Powers.TemporaryStrengthPower),
        typeof(MegaCrit.Sts2.Core.Models.Powers.TemporaryDexterityPower),
        typeof(MegaCrit.Sts2.Core.Models.Powers.TemporaryFocusPower),
        typeof(MegaCrit.Sts2.Core.Models.Powers.VoidFormPower),
    };
    /// <summary>r7 安装收束用: 本层要求的目标总数 (5/5 才算安装).</summary>
    internal static int ExpectedTargetCount => TargetTypes.Length;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersFallbackPreApplyBridgePatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>r7 安装收束用: 实际解析出的目标方法数组 (只含成功解析项).</summary>
    internal static MethodInfo[] TargetMethodsResolved()
    {
        var resolved = new List<MethodInfo>(TargetTypes.Length);
        foreach (Type type in TargetTypes)
        {
            MethodInfo? method = ResolveOverride(type);
            if (method is not null)
            {
                resolved.Add(method);
            }
        }
        return resolved.ToArray();
    }


    private static MethodInfo? ResolveOverride(Type type)
    {
        MethodInfo? method = type.GetMethod(
            nameof(PowerModel.BeforeApplied),
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel) },
            null);
        if (method is null || method.ReturnType != typeof(Task))
        {
            return null;
        }

        // 必须由目标类型本身声明: 若某个 override 在未来版本消失, GetMethod 会回落到继承的基类方法;
        // DeclaringType 校验让该情形计为 "目标缺失", 触发整层 NOT installed, 而不是把基类方法重复 patch.
        if (method.DeclaringType != type)
        {
            return null;
        }

        ParameterInfo[] p = method.GetParameters();
        if (p.Length != 4
            || p[0].ParameterType != typeof(Creature) || p[0].Name != "target"
            || p[1].ParameterType != typeof(decimal) || p[1].Name != "amount"
            || p[2].ParameterType != typeof(Creature) || p[2].Name != "applier"
            || p[3].ParameterType != typeof(CardModel) || p[3].Name != "cardSource")
        {
            return null;
        }

        return method;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in TargetTypes)
        {
            MethodInfo? method = ResolveOverride(type);
            if (method is not null)
            {
                yield return method;
            }
        }
    }

    private static bool Prepare()
    {
        int found = 0;
        foreach (Type type in TargetTypes)
        {
            if (ResolveOverride(type) is not null)
            {
                found++;
            }
        }

        if (found != TargetTypes.Length)
        {
            Spire1PowersGate.ReportFallbackPreApplyBridgeUnavailable(
                "target override set drift (found " + found + " of " + TargetTypes.Length + ")");
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    private static bool Prefix(
        PowerModel __instance,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        ref Task __result)
    {
        if (amount == 0m)
        {
            // Given/Received 可能已把 amount 归零; ApplyInternal 会原生早退, 无需再阻断.
            return true;
        }

        // 只覆盖 "Spire1 cardSource -> vanilla/其它 mod power" 且 Before 目标缺失的路径:
        // Spire1 自有 power 由 Received/ApplyInternal 覆盖; 非 Spire1 cardSource 不属于本门控.
        // 基类方法与 4 个 override 的第四个参数名都按位置绑定 (Harmony 允许按位置匹配).
        if (!Spire1PowersGate.ShouldBlockFallbackPreApplyBridge(__instance, target, cardSource))
        {
            return true;
        }

        // BeforeApplied 的返回值会被 PowerCmd.Apply await (PowerCmd.cs:136); faulted Task 会在
        // ApplyInternal (PowerCmd.cs:139) 之前传播, 阻止外层 power 挂载.
        __result = Task.FromException(new InvalidOperationException(
            "Spire1 powers gate: fallback PreApplyBridge blocked a Spire1-sourced power before ApplyInternal (P1-C12-01)."));
        return false;
    }
}

/// <summary>
/// C12 r5-B fallback 层 B: Hook.ModifyPowerAmountGiven (同步, 带 cardSource) 的本地判定归零.
/// 命中时把 amount 置 0 并写非 null out modifiers; 不读取任何 Before 阶段保存的状态.
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersFallbackGivenPatch
{
    private static readonly MethodInfo? Target = Resolve();

    /// <summary>r7 安装收束用: 目标方法 (解析失败为 null).</summary>
    internal static MethodInfo? TargetMethod => Target;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersFallbackGivenPatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo? match = null;
            foreach (MethodInfo candidate in typeof(Hook)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != nameof(Hook.ModifyPowerAmountGiven)
                    || !IsExactSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    Spire1PowersGate.ReportFallbackGivenUnavailable(
                        "target is ambiguous (multiple exact matches)");
                    return null;
                }

                match = candidate;
            }

            return match;
        }
        catch (Exception e)
        {
            Spire1PowersGate.ReportFallbackGivenUnavailable(
                "resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    private static bool IsExactSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(decimal))
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 7
            && p[0].ParameterType == typeof(ICombatState) && p[0].Name == "combatState"
            && p[1].ParameterType == typeof(PowerModel) && p[1].Name == "power"
            && p[2].ParameterType == typeof(Creature) && p[2].Name == "giver"
            && p[3].ParameterType == typeof(decimal) && p[3].Name == "amount"
            && p[4].ParameterType == typeof(Creature) && p[4].Name == "target"
            && p[5].ParameterType == typeof(CardModel) && p[5].Name == "cardSource"
            && p[6].ParameterType == typeof(IEnumerable<AbstractModel>).MakeByRefType()
            && p[6].Name == "modifiers";
    }

    private static bool Prepare()
    {
        if (Target is null)
        {
            return false;
        }
        return true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    private static bool Prefix(
        PowerModel power,
        decimal amount,
        Creature? target,
        CardModel? cardSource,
        out IEnumerable<AbstractModel> modifiers,
        ref decimal __result)
    {
        // out 参数必须始终写值: 调用方会把该值传给 Hook.AfterModifyingPowerAmountGiven,
        // 该 hook 对集合执行 Contains; 空集合保持与引擎默认行为一致.
        modifiers = Array.Empty<AbstractModel>();
        if (!Spire1PowersGate.ShouldBlockFallbackGiven(power, amount, target, cardSource))
        {
            return true;
        }

        __result = 0m;
        return false;
    }
}

/// <summary>
/// C12 r5-B fallback 层 C: Hook.ModifyPowerAmountReceived 本地判定归零.
/// 只按本次调用的 canonicalPower 判定 (Spire1 命名空间, Forms 形态局例外), 不消费任何 Before 状态.
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersFallbackReceivedPatch
{
    private static readonly MethodInfo? Target = Resolve();

    /// <summary>r7 安装收束用: 目标方法 (解析失败为 null).</summary>
    internal static MethodInfo? TargetMethod => Target;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersFallbackReceivedPatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo? match = null;
            foreach (MethodInfo candidate in typeof(Hook)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name != nameof(Hook.ModifyPowerAmountReceived)
                    || !IsExactSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    Spire1PowersGate.ReportFallbackReceivedUnavailable(
                        "target is ambiguous (multiple exact matches)");
                    return null;
                }

                match = candidate;
            }

            return match;
        }
        catch (Exception e)
        {
            Spire1PowersGate.ReportFallbackReceivedUnavailable(
                "resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    private static bool IsExactSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(decimal))
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 6
            && p[0].ParameterType == typeof(ICombatState) && p[0].Name == "combatState"
            && p[1].ParameterType == typeof(PowerModel) && p[1].Name == "canonicalPower"
            && p[2].ParameterType == typeof(Creature) && p[2].Name == "target"
            && p[3].ParameterType == typeof(decimal) && p[3].Name == "amount"
            && p[4].ParameterType == typeof(Creature) && p[4].Name == "giver"
            && p[5].ParameterType == typeof(IEnumerable<AbstractModel>).MakeByRefType()
            && p[5].Name == "modifiers";
    }

    private static bool Prepare()
    {
        if (Target is null)
        {
            return false;
        }
        return true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    private static bool Prefix(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        out IEnumerable<AbstractModel> modifiers,
        ref decimal __result)
    {
        // out 参数必须始终写值 (调用方会把该值传给 Hook.AfterModifyingPowerAmountReceived,
        // 该 hook 对集合执行 Contains). 命中判定只使用本次调用的参数, 无跨调用状态.
        modifiers = Array.Empty<AbstractModel>();
        if (!Spire1PowersGate.ShouldBlockFallbackReceived(canonicalPower, target, amount))
        {
            return true;
        }

        __result = 0m;
        return false;
    }
}

/// <summary>
/// C12 r5-B fallback 层 D: 类型级最后兜底. 只对 Spire1 命名空间 power (Forms 形态局例外) 生效;
/// 直接调用 ApplyInternal 的 vanilla power 无 cardSource 可判定, 属明确记录的残余边界.
/// </summary>
[HarmonyPatch]
internal static class Spire1PowersFallbackApplyInternalPatch
{
    private static readonly MethodInfo? Target = Resolve();

    /// <summary>r7 安装收束用: 目标方法 (解析失败为 null).</summary>
    internal static MethodInfo? TargetMethod => Target;

    /// <summary>r7 安装收束用: 本补丁类的 prefix 方法 (解析失败为 null = fail closed).</summary>
    internal static MethodInfo? PrefixMethod { get; } = ResolveSelfPatchMethod(nameof(Prefix));

    private static MethodInfo? ResolveSelfPatchMethod(string name)
    {
        try
        {
            MethodInfo? method = typeof(Spire1PowersFallbackApplyInternalPatch).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo? match = null;
            foreach (MethodInfo candidate in typeof(PowerModel)
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (candidate.Name != nameof(PowerModel.ApplyInternal)
                    || !IsExactSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    Spire1PowersGate.ReportFallbackApplyInternalUnavailable(
                        "target is ambiguous (multiple exact matches)");
                    return null;
                }

                match = candidate;
            }

            return match;
        }
        catch (Exception e)
        {
            Spire1PowersGate.ReportFallbackApplyInternalUnavailable(
                "resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    private static bool IsExactSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(void))
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 3
            && p[0].ParameterType == typeof(Creature) && p[0].Name == "owner"
            && p[1].ParameterType == typeof(decimal) && p[1].Name == "amount"
            && p[2].ParameterType == typeof(bool) && p[2].Name == "silent";
    }

    private static bool Prepare()
    {
        if (Target is null)
        {
            return false;
        }
        return true;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    private static void Prefix(PowerModel __instance, Creature owner, ref decimal amount)
    {
        if (amount == 0m)
        {
            return;
        }
        if (Spire1PowersGate.ShouldBlockFallbackApplyInternal(__instance, owner))
        {
            amount = 0m;
        }
    }
}

/// <summary>
/// r8b 安装入口: 主漏斗任一目标未安装时启用后备层. 每个后备补丁独立 try/catch,
/// 单个失败不阻止其它后备补丁安装; 每层安装状态一律由 Harmony.GetPatchInfo 实测收束.
/// PreApplyBridge 只作 best-effort, 不再单独作为 coverage 证明. 覆盖证明失败时由
/// <see cref="Spire1PowersGate.EvaluateAndEnforceCoverage"/> 执行硬熔断 (先置独立
/// ContentUnavailable/HardFailClosed, 再尝试关闭内容主开关), 不再只记 Error 后放行.
/// </summary>
internal static class Spire1PowersFallbackInstaller
{
    /// <summary>
    /// r7: 一次性安装/复核入口. Phase3 多次进入时: 第一次完成后由 _completed 短路; 并发的第二次
    /// 由 _lock 串行化, 且 <see cref="InstallOrVerifyLayer"/> 对已安装层只复核不重复挂载.
    /// 每层的安装状态一律来自 <see cref="Spire1PowersGate.ReconcileFallbackInstallation"/> 的
    /// GetPatchInfo 实测, 不再依赖 CreateClassProcessor 是否抛异常.
    /// </summary>
    private static readonly object InstallLock = new();
    private static bool _completed;

    internal static void InstallIfNeeded(Harmony harmony)
    {
        lock (InstallLock)
        {
            if (_completed)
            {
                return;
            }

            if (Spire1PowersGate.CentralGateOperational)
            {
                _completed = true;
                return;
            }

            MainFile.Logger.Error(
                "[Spire1] powers gate: central gate incomplete (apply=" +
                Spire1PowersGate.ApplyGateInstalled + ", modify=" + Spire1PowersGate.ModifyGateInstalled +
                ") - installing fail-closed fallback patches.");

            InstallOrVerifyLayer(harmony, typeof(Spire1PowersFallbackBeforePatch),
                Spire1PowersFallbackBeforePatch.TargetMethod, Spire1PowersFallbackBeforePatch.PrefixMethod,
                "fallback BeforePowerAmountChanged");
            InstallOrVerifyBridge(harmony);
            InstallOrVerifyLayer(harmony, typeof(Spire1PowersFallbackGivenPatch),
                Spire1PowersFallbackGivenPatch.TargetMethod, Spire1PowersFallbackGivenPatch.PrefixMethod,
                "fallback ModifyPowerAmountGiven");
            InstallOrVerifyLayer(harmony, typeof(Spire1PowersFallbackReceivedPatch),
                Spire1PowersFallbackReceivedPatch.TargetMethod, Spire1PowersFallbackReceivedPatch.PrefixMethod,
                "fallback ModifyPowerAmountReceived");
            InstallOrVerifyLayer(harmony, typeof(Spire1PowersFallbackApplyInternalPatch),
                Spire1PowersFallbackApplyInternalPatch.TargetMethod,
                Spire1PowersFallbackApplyInternalPatch.PrefixMethod,
                "fallback ApplyInternal");

            Spire1PowersGate.ReconcileFallbackInstallation(harmony);
            _completed = true;

            if (Spire1PowersGate.FallbackHookInstalled
                && Spire1PowersGate.FallbackPreApplyBridgeInstalled
                && Spire1PowersGate.FallbackGivenInstalled
                && Spire1PowersGate.FallbackReceivedInstalled
                && Spire1PowersGate.FallbackApplyInternalInstalled)
            {
                MainFile.Logger.Info(
                    "[Spire1] powers gate: fail-closed fallback layer verified installed " +
                    "(before+preApplyBridge+given+received+applyInternal).");
                return;
            }

            MainFile.Logger.Error(
                "[Spire1] powers gate: fallback layer incomplete after verification (" +
                Spire1PowersGate.DescribeCoverage() + ").");
        }
    }

    /// <summary>
    /// r8b 多目标层 (PreApplyBridge) 安装/复核: 5/5 全部已绑定 -> 只复核 (幂等, 不重复 Patch);
    /// 0/5 已绑定 -> 安装; 部分绑定的历史残留状态不再二次 Patch (避免对已挂载方法重复挂载),
    /// 直接记为 NOT installed 交给收束与覆盖证明决定是否硬熔断. 该层只作 best-effort,
    /// 不再单独计入 cardSource coverage 证明.
    /// </summary>
    private static void InstallOrVerifyBridge(Harmony harmony)
    {
        MethodInfo? prefix = Spire1PowersFallbackPreApplyBridgePatch.PrefixMethod;
        if (prefix is null)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: fallback PreApplyBridge prefix reflection unresolved - layer NOT installed.");
            return;
        }

        MethodInfo[] resolved = Spire1PowersFallbackPreApplyBridgePatch.TargetMethodsResolved();
        if (resolved.Length != Spire1PowersFallbackPreApplyBridgePatch.ExpectedTargetCount)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: fallback PreApplyBridge target set drift (" +
                resolved.Length + "/" + Spire1PowersFallbackPreApplyBridgePatch.ExpectedTargetCount +
                ") - layer NOT installed.");
            return;
        }

        int installed = 0;
        foreach (MethodInfo target in resolved)
        {
            if (IsLayerInstalled(harmony, target, prefix))
            {
                installed++;
            }
        }

        if (installed == resolved.Length)
        {
            return; // 5/5 已安装: 幂等只复核
        }

        if (installed != 0)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: fallback PreApplyBridge partially installed (" +
                installed + "/" + resolved.Length + "); refusing a duplicate patch pass - layer " +
                "treated as NOT installed until coverage proof decides hard fail-closed.");
            return;
        }

        try
        {
            harmony.CreateClassProcessor(typeof(Spire1PowersFallbackPreApplyBridgePatch)).Patch();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: fallback patch Spire1PowersFallbackPreApplyBridgePatch failed: " + e.Message);
        }
    }

    /// <summary>
    /// 单层安装: 目标或 prefix 无法解析时不安装并记录 (fail closed); 已由 GetPatchInfo 证明安装时
    /// 只记录复核通过, 不再重复 Patch. 安装后由调用方统一 ReconcileFallbackInstallation 收束.
    /// </summary>
    private static void InstallOrVerifyLayer(
        Harmony harmony, Type type, MethodInfo? target, MethodInfo? prefix, string layer)
    {
        if (prefix is null)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: " + layer + " prefix reflection unresolved - layer NOT installed.");
            return;
        }

        if (target is not null && IsLayerInstalled(harmony, target, prefix))
        {
            return; // 已安装: 幂等, 不重复挂载
        }

        try
        {
            harmony.CreateClassProcessor(type).Patch();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: fallback patch " + type.Name + " failed: " + e.Message);
        }
    }

    /// <summary>实测单目标层是否已由本 Harmony id 的指定 prefix 安装.</summary>
    private static bool IsLayerInstalled(Harmony harmony, MethodBase target, MethodInfo prefix)
    {
        try
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(target);
            return info is not null
                && info.Owners.Contains(harmony.Id)
                && info.Prefixes.Any(patch =>
                    patch.owner == harmony.Id && patch.PatchMethod is { } patchMethod && (patchMethod == prefix || patchMethod.Equals(prefix)));
        }
        catch (Exception)
        {
            return false; // 任何探测异常都按未安装处理, 交给 Patch + 收束复核
        }
    }
}

/// <summary>
/// r8c 注册熔断面 A: <c>ModelDb.Init(Type[]?)</c> 前缀. 当独立不可用状态
/// (<see cref="Spire1PowersGate.ContentUnavailable"/> 或 <see cref="Spire1PowersGate.HardFailClosed"/>) 置位时,
/// 在引擎逐个 <c>Activator.CreateInstance</c> 之前把 Spire1 程序集的模型类型从数组剔除; 非 Spire1 类型一律保留.
/// <para>
/// 该前缀不读 <c>Spire1Config</c>, 不依赖 powers central/fallback prefix, 只按程序集判定 (产品代码无其它
/// ModelDb 注入/移除通道). null 输入 (injectedModelTypes == null) 走 AllAbstractModelSubtypes getter, 由
/// <see cref="Spire1ModelDbSubtypesFusePatch"/> 过滤, 两条路径都覆盖.
/// </para>
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init), new[] { typeof(Type[]) })]
internal static class Spire1ModelDbInitFusePatch
{
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();
    internal static MethodInfo? PrefixMethod { get; } = ResolvePrefix();

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(typeof(ModelDb), nameof(ModelDb.Init), new[] { typeof(Type[]) });
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolvePrefix()
    {
        try
        {
            MethodInfo? method = typeof(Spire1ModelDbInitFusePatch).GetMethod(
                nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref Type[]? injectedModelTypes)
    {
        if (!Spire1PowersGate.ContentUnavailableActive || injectedModelTypes is null)
        {
            return;
        }

        injectedModelTypes = Spire1ModelDbFuseFilter.FilterSpire1Types(injectedModelTypes);
    }
}

/// <summary>
/// r8c 注册熔断面 B: <c>ModelDb.AllAbstractModelSubtypes</c> getter 后缀. 该 getter 被 <c>ModelDb.Init</c> 的
/// null 输入路径以及 <c>ModelDb.AllPowers</c>/<c>Preload</c> 的 <c>ModelDb.Get(t)</c> 读取; 若只过滤 Init 输入,
/// getter 仍含未构造的 Spire1 类型, Preload 会对缺失 ModelId 抛 ModelNotFoundException. 因此熔断必须同时过滤 getter.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllAbstractModelSubtypes), MethodType.Getter)]
internal static class Spire1ModelDbSubtypesFusePatch
{
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();
    internal static MethodInfo? PostfixMethod { get; } = ResolvePostfix();

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredPropertyGetter(typeof(ModelDb), nameof(ModelDb.AllAbstractModelSubtypes));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolvePostfix()
    {
        try
        {
            MethodInfo? method = typeof(Spire1ModelDbSubtypesFusePatch).GetMethod(
                nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPostfixMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [HarmonyPostfix]
    private static void Postfix(ref Type[] __result)
    {
        if (!Spire1PowersGate.ContentUnavailableActive || __result is null)
        {
            return;
        }

        __result = Spire1ModelDbFuseFilter.FilterSpire1Types(__result);
    }
}

/// <summary>
/// r8c 熔断过滤器: 只剔除 Spire1 程序集类型, 其它程序集类型 (vanilla/其它 mod) 原样保留. 输入为 null 或全部放行时
/// 返回原数组 (不复制); 只按程序集判定, 不读配置, 不解析 powers prefix.
/// </summary>
internal static class Spire1ModelDbFuseFilter
{
    internal static Type[] FilterSpire1Types(Type[] types)
    {
        try
        {
            Assembly spire1Assembly = typeof(MainFile).Assembly;
            int removed = 0;
            for (int i = 0; i < types.Length; i++)
            {
                Type? type = types[i];
                if (type is not null && type.Assembly == spire1Assembly)
                {
                    removed++;
                }
            }

            if (removed == 0)
            {
                return types;
            }

            Type[] filtered = new Type[types.Length - removed];
            int j = 0;
            for (int i = 0; i < types.Length; i++)
            {
                Type? type = types[i];
                if (type is null || type.Assembly != spire1Assembly)
                {
                    filtered[j++] = type!;
                }
            }

            MainFile.Logger.Info(
                "[Spire1] powers gate: r8c registration fuse removed " + removed +
                " Spire1 model type(s) from ModelDb input (vanilla/other-mod types preserved).");
            return filtered;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] powers gate: r8c registration fuse filter threw (" + e.GetType().Name + ": " +
                e.Message + ") - returning the unfiltered input (fuse not applied for this call).");
            return types;
        }
    }
}
