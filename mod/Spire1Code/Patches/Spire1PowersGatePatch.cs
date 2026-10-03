using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Forms;

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
/// Forms 例外 (必须, 否则形成硬故障): <c>Spire1.Spire1Code.Forms</c> 的力量由形态子系统自持生命周期.
/// <c>WatcherFormStancePower.ApplyEffect</c> 在 Apply 被拦截时会因 <c>!owner.Powers.Contains(effect)</c>
/// 抛 <c>InvalidOperationException("A power hook rejected a required form effect")</c>,
/// <c>FormStanceWatcherBridge.AfterMarkerApplied</c> 同样对 carrier 抛
/// "A power hook rejected the required Watcher form carrier". Forms/ 不在本任务写集内, 无法在那里
/// 补优雅降级. 因此: 当该局带有 <c>FormStanceModifier</c> (即 <see cref="FormStanceMode.IsSelected"/>
/// 为真, 形态子系统正在运行) 时, Forms 命名空间的 power 不受本门控拦截; 普通局 (无该修正) 的
/// Forms 命名空间 power 仍然被门控 -- 覆盖 <c>Cards/DemonForm.cs</c> 经
/// <c>CommonActions.ApplySelf&lt;DemonFormPower&gt;</c> 的普通局授予路径.
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
/// fail closed (P1-C12-01, r3/r5-B): 两个中央目标各自解析并各自安装. 安装结果由
/// <see cref="ApplyGateInstalled"/> / <see cref="ModifyGateInstalled"/> 记录; 目标缺失, 参数漂移,
/// 歧义或 Harmony 安装失败时对应标志保持 false, 日志明确写 "NOT installed". r5-B 接线:
/// MainFile Phase3 的属性扫描显式排除五个 fallback 类, 并在扫描结束后按真实安装状态调用
/// <see cref="Spire1PowersFallbackInstaller.InstallIfNeeded"/>: 中央漏斗完整时不安装 fallback
/// (正常路径零开销); 不完整时才安装五个非泛型 fallback 目标.
/// </para>
    /// <para>
    /// fallback 层工作方式 (r6-B, 无跨调用状态, 命中即硬阻断):
    /// * BeforePowerAmountChanged prefix 只做本地判定; 命中时直接以 faulted Task 硬阻断本次调用,
    ///   不放行任何 "后续层会归零" 的调用. 依据: 引擎没有跨方法调用身份, applier/combatState 条件
    ///   在 await Before 之后重新求值 (PowerCmd.cs:124-131, :231-238), 异步重入会让预测失效.
    /// * ModifyPowerAmountGiven prefix 是本层唯一带 cardSource 的同步入口: 命中时把 amount 归零并写
    ///   非 null out modifiers, 覆盖 "Spire1 卡源触发 vanilla power" 路径; canonical 预览调用
    ///   (PowerVar.UpdateCardPreview) 早退, 不归零.
    /// * ModifyPowerAmountReceived prefix 只按本次调用的实例/目标/amount 做本地判定,
    ///   不消费 Before 阶段保存的任何状态; 无论命中与否都写非 null out modifiers
    ///   (PowerCmd 会把该值继续传给 Hook.AfterModifyingPowerAmountReceived, 该 hook 对 modifiers 直接
    ///   Contains, null 会在有监听者时抛 NullReferenceException).
    /// * ApplyInternal prefix 是类型级最后兜底 (仅 Spire1 命名空间 power, Forms 形态局例外),
    ///   把 amount 置 0, 让引擎原生 amount == 0 早退, 不挂载, 不写 History/AfterApplied.
    /// * 实例分类用 HasOwner (Owner != null) 而不是 Owner.Powers.Contains: RemoveInternal 不清空
    ///   Owner, 因此旧实例被异步移除后, 负 offset 收尾仍按 Modify 规则放行, 不会被重分类成新挂载.
    /// </para>
    /// <para>
    /// fallback 层降级边界 (必须明示, 不是主漏斗的字节级等价):
    /// * 归零发生在 Given 或 Received 阶段而不是跳过整个 PowerCmd 方法: 命中 Given 的调用会跳过
    ///   Hook.ModifyPowerAmountGiven 的其它监听者 (如 UnsettlingLamp), 命中 Received 的调用会跳过
    ///   Hook.ModifyPowerAmountReceived 的其它监听者 (如 ArtifactPower); 主漏斗拦截则连 Before 都不调用.
    ///   这条路径的监听者可见性与主漏斗不同, 但 Spire1 power 不会挂载/增量, 旧实例的负 offset 收尾
    ///   (Decrement/TickDownDuration) 仍按 Modify 规则放行.
    /// * 主漏斗可用时不安装 fallback 层, 因此正常路径零运行期开销, 仅在目标缺失/漂移/歧义/安装失败时启用.
    /// </para>
    /// <para>
    /// NotClosed 边界 (不得伪称已闭合): fallback 五个目标任一解析/安装失败时,
    /// <see cref="FailClosedDegraded"/> 置位, 已安装的门控对 Spire1 内容不再读配置而直接拦截
    /// (vanilla/其它 mod 不受影响), Before prefix 命中即 faulted Task 硬阻断; 但若 Before 目标本身缺失,
    /// 则 "Spire1 cardSource -> vanilla power" 且 applier 不可达的调用没有任何带 cardSource 的必经入口
    /// (Received/ApplyInternal 无 cardSource 参数, Given 被引擎条件跳过), 该状态只记录 Error + NOT closed,
    /// 不能声称 fail-closed. 第三方直接 <c>SetAmount</c> 或非 Spire1 路径的 <c>ApplyInternal</c>
    /// 同样不在本写集可证明范围内.
    /// </para>
/// </summary>
internal static class Spire1PowersGate
{
    private const string ModNamespacePrefix = "Spire1.";
    private const string CardsNamespace = "Spire1.Spire1Code.Cards";
    private const string FormsNamespace = "Spire1.Spire1Code.Forms";

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
    /// 后备层 PreApplyBridge (4 个引擎 BeforeApplied override) 是否已安装 (仅在中央漏斗不完整时).
    /// 该层在 Before 目标缺失时提供第二道硬阻断, 避免 apply 进入 ApplyInternal 挂载.
    /// </summary>
    internal static bool FallbackPreApplyBridgeInstalled { get; private set; }

    /// <summary>后备层 ModifyPowerAmountGiven 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackGivenInstalled { get; private set; }

    /// <summary>后备层 ModifyPowerAmountReceived 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackReceivedInstalled { get; private set; }

    /// <summary>后备层 ApplyInternal 是否已安装 (仅在中央漏斗不完整时).</summary>
    internal static bool FallbackApplyInternalInstalled { get; private set; }

    /// <summary>
    /// True = 中央漏斗与 fallback 层均不完整 (P1-C12-01 无法在本写集内证明闭合).
    /// 置位后已安装的 Spire1 门控不再读配置而直接拦截 Spire1 内容; 已安装的 fallback Before
    /// 命中即以 faulted Task 硬阻断. 若连可用的非泛型拦截层都不存在 (全部目标漂移), 该标志
    /// 只是显式的 NOT closed 状态, 不构成阻断 - 该边界在报告中作为 FLAG 保留, 不伪称 fail-closed.
    /// </summary>
    internal static bool FailClosedDegraded { get; private set; }

    /// <summary>True = 两个主漏斗目标都真实安装; false = 需要启用后备层.</summary>
    internal static bool CentralGateOperational => ApplyGateInstalled && ModifyGateInstalled;

    internal static void RecordApplyGateInstalled()
    {
        ApplyGateInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: PowerCmd.Apply(non-generic) target resolved; apply gate installed.");
    }

    internal static void RecordModifyGateInstalled()
    {
        ModifyGateInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: PowerCmd.ModifyAmount target resolved; modify gate installed.");
    }

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

    internal static void RecordFallbackPreApplyBridgeInstalled()
    {
        FallbackPreApplyBridgeInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: fallback PreApplyBridge installed (PowerModel.BeforeApplied base + 4 engine overrides; central gate incomplete).");
    }

    internal static void ReportFallbackPreApplyBridgeUnavailable(string reason)
    {
        FallbackPreApplyBridgeInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback PreApplyBridge " + reason +
            " - fallback NOT installed; PreApplyBridge zeroing is NOT active.");
    }

    internal static void RecordFallbackHookInstalled()
    {
        FallbackHookInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: fallback BeforePowerAmountChanged installed (central gate incomplete).");
    }

    internal static void ReportFallbackHookUnavailable(string reason)
    {
        FallbackHookInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback BeforePowerAmountChanged " + reason +
            " - fallback NOT installed; Spire1 power application is NOT intercepted.");
    }

    internal static void RecordFallbackGivenInstalled()
    {
        FallbackGivenInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: fallback ModifyPowerAmountGiven installed (central gate incomplete).");
    }

    internal static void ReportFallbackGivenUnavailable(string reason)
    {
        FallbackGivenInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback ModifyPowerAmountGiven " + reason +
            " - fallback NOT installed; Spire1 card power amount given is NOT intercepted.");
    }

    internal static void RecordFallbackReceivedInstalled()
    {
        FallbackReceivedInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: fallback ModifyPowerAmountReceived installed (central gate incomplete).");
    }

    internal static void ReportFallbackReceivedUnavailable(string reason)
    {
        FallbackReceivedInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] powers gate: fallback ModifyPowerAmountReceived " + reason +
            " - fallback NOT installed; Spire1 power modification is NOT intercepted.");
    }

    internal static void RecordFallbackApplyInternalInstalled()
    {
        FallbackApplyInternalInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] powers gate: fallback ApplyInternal installed (central gate incomplete).");
    }

    /// <summary>
    /// fallback 层不完整: 记录 P1-C12-01 NOT closed 并让已安装的门控进入无配置的 fail-closed 模式.
    /// 调用方 (Installer) 保证只在 fallback 目标缺失/安装失败时调用一次.
    /// 注意: 该状态本身不是硬阻断; 硬阻断来自已安装的 Before prefix (命中即 faulted Task).
    /// </summary>
    internal static void RecordFallbackDegraded(string detail)
    {
        FailClosedDegraded = true;
        MainFile.Logger.Error(
            "[Spire1] powers gate: P1-C12-01 NOT closed - " + detail +
            " (before=" + FallbackHookInstalled + ", given=" + FallbackGivenInstalled +
            ", received=" + FallbackReceivedInstalled +
            ", applyInternal=" + FallbackApplyInternalInstalled + ", applyGate=" + ApplyGateInstalled +
            ", modifyGate=" + ModifyGateInstalled + ").");
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

    /// <summary>True = 该 power 属于 Forms 形态子系统命名空间.</summary>
    private static bool IsFormsPower(PowerModel power)
    {
        string? ns = power.GetType().Namespace;
        return ns is not null
            && (ns.Equals(FormsNamespace, StringComparison.Ordinal)
                || ns.StartsWith(FormsNamespace + ".", StringComparison.Ordinal));
    }

    /// <summary>新挂载门控: powers 组关闭时拦截 Spire1 power 的 Apply.</summary>
    internal static bool ShouldBlockApply(PowerModel? power, Creature? target, CardModel? cardSource)
    {
        if (power is null)
        {
            return false; // 无 power 实例: 交给原方法自身的空值语义
        }

        if (!IsGatedPower(power) && !IsSpire1CardSource(cardSource))
        {
            return false; // 原版/其它 mod power 且非 Spire1 卡触发: 永不拦截
        }

        if (IsFormsPower(power!) && FormStanceMode.IsSelected(target?.Player))
        {
            return false; // 形态子系统自持生命周期, 见类型注释
        }

        if (!FailClosedDegraded && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
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

        // Forms 例外与 Apply 相同; Owner 在 ModifyAmount 的原方法体首句即被读取,
        // 这里直接读取不改变失败语义 (原方法对无 Owner 的实例同样会抛).
        if (IsFormsPower(power!) && FormStanceMode.IsSelected(power!.Owner?.Player))
        {
            return false;
        }

        if (!FailClosedDegraded && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
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
    /// True = 该类型是 fail-closed fallback 补丁类之一 (Before/Given/Received/ApplyInternal).
    /// MainFile Phase3 的属性扫描必须跳过它们,
    /// 只允许 <see cref="Spire1PowersFallbackInstaller.InstallIfNeeded"/> 在中央漏斗不完整时安装.
    /// </summary>
    internal static bool IsFallbackPatchType(Type type)
    {
        return type == typeof(Spire1PowersFallbackBeforePatch)
            || type == typeof(Spire1PowersFallbackPreApplyBridgePatch)
            || type == typeof(Spire1PowersFallbackGivenPatch)
            || type == typeof(Spire1PowersFallbackReceivedPatch)
            || type == typeof(Spire1PowersFallbackApplyInternalPatch);
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
        if (!FailClosedDegraded && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
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

    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            // Prepare() 已失败; Harmony 仍可能以无参形式调用 Cleanup, 这里绝不能把缺失目标记成已安装.
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordApplyGateInstalled();
            return;
        }
        Spire1PowersGate.ReportApplyGateUnavailable("installation threw (" + __exception.GetType().Name + ")");
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

    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            // Prepare() 已失败; Harmony 仍可能以无参形式调用 Cleanup, 这里绝不能把缺失目标记成已安装.
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordModifyGateInstalled();
            return;
        }
        Spire1PowersGate.ReportModifyGateUnavailable("installation threw (" + __exception.GetType().Name + ")");
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

    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordFallbackHookInstalled();
            return;
        }
        Spire1PowersGate.ReportFallbackHookUnavailable(
            "installation threw (" + __exception.GetType().Name + ")");
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

    private static bool _prepared;

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
        _prepared = false;
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

        _prepared = true;
        return true;
    }

    private static void Cleanup(Exception? __exception)
    {
        if (!_prepared)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordFallbackPreApplyBridgeInstalled();
            return;
        }
        Spire1PowersGate.ReportFallbackPreApplyBridgeUnavailable(
            "installation threw (" + __exception.GetType().Name + ")");
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

    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordFallbackGivenInstalled();
            return;
        }
        Spire1PowersGate.ReportFallbackGivenUnavailable(
            "installation threw (" + __exception.GetType().Name + ")");
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

    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordFallbackReceivedInstalled();
            return;
        }
        Spire1PowersGate.ReportFallbackReceivedUnavailable(
            "installation threw (" + __exception.GetType().Name + ")");
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

    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1PowersGate.RecordFallbackApplyInternalInstalled();
            return;
        }
        Spire1PowersGate.ReportFallbackApplyInternalUnavailable(
            "installation threw (" + __exception.GetType().Name + ")");
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
/// C12 r3 安装入口: 主漏斗任一目标未安装时启用后备层. 每个后备补丁独立 try/catch,
/// 单个失败不阻止其它后备补丁安装; 任一失败时置 <see cref="Spire1PowersGate.FailClosedDegraded"/>
/// 并记录 P1-C12-01 NOT closed (不得只记 Error 后放行, 也不得伪称已闭合).
/// </summary>
internal static class Spire1PowersFallbackInstaller
{
    internal static void InstallIfNeeded(Harmony harmony)
    {
        if (Spire1PowersGate.CentralGateOperational)
        {
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] powers gate: central gate incomplete (apply=" +
            Spire1PowersGate.ApplyGateInstalled + ", modify=" + Spire1PowersGate.ModifyGateInstalled +
            ") - installing fail-closed fallback patches.");

        TryInstall(harmony, typeof(Spire1PowersFallbackBeforePatch));
        TryInstall(harmony, typeof(Spire1PowersFallbackPreApplyBridgePatch));
        TryInstall(harmony, typeof(Spire1PowersFallbackGivenPatch));
        TryInstall(harmony, typeof(Spire1PowersFallbackReceivedPatch));
        TryInstall(harmony, typeof(Spire1PowersFallbackApplyInternalPatch));

        if (!Spire1PowersGate.FallbackHookInstalled
            || !Spire1PowersGate.FallbackPreApplyBridgeInstalled
            || !Spire1PowersGate.FallbackGivenInstalled
            || !Spire1PowersGate.FallbackReceivedInstalled
            || !Spire1PowersGate.FallbackApplyInternalInstalled)
        {
            Spire1PowersGate.RecordFallbackDegraded(
                "fallback layer incomplete (before=" + Spire1PowersGate.FallbackHookInstalled +
                ", preApplyBridge=" + Spire1PowersGate.FallbackPreApplyBridgeInstalled +
                ", given=" + Spire1PowersGate.FallbackGivenInstalled +
                ", received=" + Spire1PowersGate.FallbackReceivedInstalled +
                ", applyInternal=" + Spire1PowersGate.FallbackApplyInternalInstalled + ")");
            return;
        }

        MainFile.Logger.Info(
            "[Spire1] powers gate: fail-closed fallback layer installed (before+preApplyBridge+given+received+applyInternal).");
    }

    private static void TryInstall(Harmony harmony, Type type)
    {
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
}
