using System.Linq.Expressions;
using System.Reflection;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// AFTP(创意工坊 3746969593 "Acts from the Past",mod manifest version 1.0.5)卡牌状态
/// 兼容层(AFTP-R4,docs/STS-PERFORMANCE-PLAN-2026-09-15.md).
///
/// 缺口一 R4-01(已复现的崩溃,本层的存在理由):AFTP 把 <c>Burn.MaxUpgradeLevel</c>
/// 后置改成 <c>(AllowBurnUpgrade ? 1 : 0)</c>(反编译 :22783-22791,类级
/// <c>[HarmonyPatch(typeof(Burn))]</c>,实测该补丁恰好 2 处,均在 Burn 上);引擎原本
/// <c>Burn.MaxUpgradeLevel =&gt; 0</c>(Burn.cs:16).而 <c>AllowBurnUpgrade</c> 只在
/// Hexaghost 的两个方法里置位(<c>UpgradeAllBurnsAndAddMore</c> :3251/:3282,
/// <c>AddBurnsToDiscard</c> :3293/:3316),所以升级过的 Burn 一旦离开那段窗口,
/// 任何重建都会炸:<c>CardModel.FromSerializable</c> 的升级循环
/// (CardModel.cs:2263-2266)调 <c>UpgradeInternal</c> (:2129)自增
/// <c>CurrentUpgradeLevel</c> (:2132),其 setter 在 <c>value &gt; MaxUpgradeLevel</c> 时
/// 抛 <c>InvalidOperationException: CARD.BURN cannot be upgraded past its
/// MaxUpgradeLevel.</c> (CardModel.cs:765-779).复现证据:
/// docs/evidence/aftp-r4-control-reproduction.txt(读档即崩,非理论风险).
///
/// 缺口二 R4-02(静默状态丢失,opt-in 路径):AFTP 用一个**无名**的
/// <c>SpireField&lt;CardModel,bool&gt;</c> 记录"经典史莱姆"
/// (<c>ClassicSlimedTracker.IsClassicSlimed</c>,反编译 :17868-17872),只在
/// <c>TagClassicSlimedPatch</c> 里对 <c>ToMutable</c> 的结果 Set (:22806-22812),
/// 行为消费者是 <c>ClassicSlimedOnPlayPatch</c> 的 <c>IsClassicSlimed.Get</c>
/// (:22845).该字段既没有 <c>CopyOnClone</c>,也没有注册进 BaseLib 的扩展存档,
/// 于是克隆与存读档双双丢失标记 -- 一张经典史莱姆变回普通史莱姆.只在
/// <c>LegacyEnemiesGiveClassicSlimed</c> 打开时才有标记可丢(默认 false),故为
/// 静默错误而非崩溃.
///
/// 桥接面(单一 capability group,全有或全无--半套绑定比不绑定更糟):
/// 1. R4-01:<c>CardModel.FromSerializable</c> 的 Prefix + **Finalizer**.前缀先把
///    **捕获到的**当前 <c>AllowBurnUpgrade</c> 写进 <c>__state</c>,再按门控放行;
///    终结器把 <c>__state</c> 原样写回.用 Finalizer 而不是 Postfix 是实测决定的:
///    Postfix 在抛出的那次重建里根本不会执行,留下 <c>AllowBurnUpgrade = true</c>,
///    比不修更糟--此后整个会话里**每一张** Burn 的 <c>MaxUpgradeLevel</c> 都是 1
///    (P2 复核复现:{outcome: threw, flagAfter: true, leaked: true}).
///    还原"捕获值"而不是常量 false 同样是实测决定的:常量还原会在嵌套调用时关掉
///    AFTP 自己打开的窗口(P2 复核复现:{flagAfter: false, outerWindowPreserved: false}),
///    那会打断 AFTP 自己的 <c>UpgradeAllBurnsAndAddMore</c>.
/// 2. R4-02 克隆半:安装时对 AFTP 那个既有字段调 <c>CopyOnClone()</c>.必须**在
///    AFTP 第一次 Set 之前**完成,因为 <c>SpireField.Set</c> 只在
///    <c>ShouldClone</c> 已为真时才把该模型登记进克隆表(SpireField.cs:126-134),
///    而 <c>Get</c> 对值类型(<c>bool</c>)永不登记.时序上成立:六个打标记的位置
///    全是怪物招式(反编译 :1114/:1377/:5181/:5475/:5712/:35099),只可能在战斗里
///    执行,而本层在 Spire1 initializer 里安装,严格早于任何战斗
///    (r4-02-ordering-analysis.txt).
/// 3. R4-02 存档半:调 <c>ExtendedSaveTypes.RegisterSavedValue&lt;CardModel,bool&gt;</c>,
///    getter/setter **委托给 AFTP 那个既有字段**.这里**不能**用
///    <c>SavedSpireField</c>:它自带一张新的 ConditionalWeakTable,与 AFTP 触碰的
///    不是同一张表,值会各走各的;而且行为消费者读的是 AFTP 的表,就算存档字段还原了
///    也不会还原行为.委托给同一张表才是"一处存储,零漂移".
/// 4. **不重复安装 BaseLib 自己的补丁**.扩展存档的四个嵌套补丁类
///    (<c>PrepExtendedCardData</c>/<c>LoadExtendedCardData</c>/
///    <c>SerializeExtendedCardData</c>/<c>DeserializeExtendedCardData</c>)与克隆补丁
///    <c>ICloneableField.CloneSpireFields</c> 由 BaseLib 自己的
///    <c>BaseLibMain.Initialize</c> -&gt; <c>MainHarmony.TryPatchAll</c> 安装:实测
///    <c>TryPatchAll</c> 的筛选 lambda 对嵌套补丁类返回 true
///    (probe8-results.json:五个目标全 true,<c>plain_string</c> false).重复安装会在
///    <c>SerializableCard.Serialize</c> 上叠两个 Postfix,把扩展数据写两遍并让其后
///    每个包字段错位.本层只调 <c>CopyOnClone</c> 与 <c>RegisterSavedValue</c>.
///
/// 为什么不用"跳过原方法 + 反向补丁"那条路(probe3 方案 C,已否决):它会绕过
/// BaseLib 打在 <c>CardModel.FromSerializable</c> 上的 Transpiler,而那正是 R4-02
/// 存档半赖以为生的挂点--修好 R4-01 的同时把 R4-02 弄坏,不是可接受的交换.
///
/// 存档键契约(本层**新建**,双向都由本层拥有):AFTP 那个字段是匿名的,反编译里
/// 没有任何存档名可继承,所以 <c>"ClassicSlimedCompat"</c> 是本层自定的键,写与读
/// 都经本层注册的同一对 getter/setter.它只影响 Spire1 自己写出的存档;AFTP 将来
/// 若自己实现存档,两边会各写一份键,需按当时的实现重新对齐.
///
/// 生命周期(producer/owner/first-consumer/cleanup):
/// - R4-01 窗口:producer = 本层 Prefix(每次 <c>FromSerializable</c> 调用一次);
///   owner = 本层;first consumer = 同一次调用内的 <c>UpgradeInternal</c> 读
///   <c>MaxUpgradeLevel</c>;cleanup = 本层 Finalizer(正常返回与异常退出都会跑,
///   实测两条路径都还原).
/// - R4-02 克隆半:producer = AFTP 六个招式里的 <c>CreatingClassicSlimed</c> 标记块;
///   owner = AFTP(值),本层只负责把该字段接进克隆表;first consumer = 克隆体的
///   <c>ClassicSlimedOnPlayPatch</c>;cleanup = 无(随模型生命周期,ConditionalWeakTable).
/// - R4-02 存档半:producer = 本层的 setter 委托(读档时由 BaseLib 的 Load 调用);
///   owner = AFTP 的同一张表;first consumer = 同上;cleanup = 无.
///
/// 失败矩阵(可选性契约,缺席 = 零影响):
/// - AFTP 程序集缺席:只记一条 Info,不装补丁;按 AutoAnthonyLoadHook 先例挂
///   AssemblyLoad 兜底(Spire1 与 AFTP 无依赖边,加载序由用户 mod 列表决定).
/// - 程序集在而成员解析失败(版本漂移):终局裁决一次(<see cref="_terminal"/>),
///   带原因记 Error,摘除 AssemblyLoad 兜底,本会话不再重试(无重试风暴).
/// - 门控本身抛异常:前缀的 <c>__state</c> **先赋值再门控**,所以终结器仍能还原正确
///   的先前值;门控失败退化为"不放行",即 R4-01 维持未修的现状,不引入新故障.
///   全进程只记一次 Error(有界日志).
/// - 部分安装:R4-01 的补丁可撤(<c>Harmony.Unpatch</c>),R4-02 的注册不可撤,所以
///   安装顺序是先补丁后注册,任何一步失败都撤销已装补丁再终局 -- 不会留下"存档半
///   生效而崩溃没修"的半套状态.
///
/// 线程假设:与 AutoAnthonyCompatBridge 相同--ModManager initializer 与 AssemblyLoad
/// 都发生在主线程加载阶段,本类不加锁.<c>FromSerializable</c> 本身可能在网络同步
/// 路径被调用,但那个调用也在主线程.
///
/// 明确不做(各归其主):<c>DeprecatedCard</c> 等其它 <c>MaxUpgradeLevel = 0</c> 卡牌
/// 的同类问题(引擎自身行为,与 AFTP 无关,不在本缺口内);AFTP 特效节点生命周期
/// (AFTP-1,<see cref="AftpEffectLifecycleCompat"/>);FireFly 拖尾分配(AFTP-2,
/// <see cref="AftpFireFlyPerfCompat"/>);不改 AFTP 的配置项语义;不构建/替换 AFTP
/// 主 DLL 与 PCK.程序集对 AFTP 纯反射绑定(本程序集不引用 AFTP 编译期类型),
/// 对 BaseLib 是编译期引用(Spire1 已依赖 3.4.5).
/// </summary>
internal static class AftpCardStateCompat
{
    private const string TargetAssembly = "ActsFromThePast";
    private const string BurnPatchType = "ActsFromThePast.Patches.Cards.BurnUpgradePatch";
    private const string AllowBurnUpgradeField = "AllowBurnUpgrade";
    private const string ClassicSlimedTrackerType = "ActsFromThePast.ClassicSlimedTracker";
    private const string IsClassicSlimedField = "IsClassicSlimed";

    /// <summary>本层自定的存档键(AFTP 的字段无名,见类注释的"存档键契约").</summary>
    private const string ClassicSlimedSaveKey = "ClassicSlimedCompat";

    // 终态锁存:_applied = 补丁已装(幂等,重复初始化不再挂);_terminal = 判定不兼容,
    // 带原因退场,永不重试.两者都不置位 = 程序集尚未出现,兜底等待中.
    private static bool _applied;
    private static bool _terminal;
    private static bool _hooked;
    private static bool _gateFallbackLogged;
    private static Harmony? _harmony;

    // 一次性编译的静态字段访问器(安装前全部就绪;安装后不可为 null).
    // FromSerializable 不是逐帧路径,但保持与 AFTP-1 相同的"零反射进热路径"纪律.
    private static Func<bool>? _readAllowBurnUpgrade;
    private static Action<bool>? _writeAllowBurnUpgrade;

    /// <summary>AFTP 的经典史莱姆标记(AFTP 侧字段,本层只接管线,不换存储).</summary>
    private static SpireField<CardModel, bool>? _classicSlimed;

    /// <summary>Burn 的模型 id,门控用.<c>ModelDb.GetId(Type)</c> 是纯函数(不查库).</summary>
    private static ModelId? _burnId;

    /// <summary>
    /// 挂载入口.MainFile 集成点:Phase3PatchAndInteropRegistration 内,
    /// <c>Interop.AftpEffectLifecycleCompat.TryApply(harmony)</c> 之后一行
    /// <c>Interop.AftpCardStateCompat.TryApply(harmony);</c>(SpireCore 负责接入,
    /// 本文件不触碰 MainFile).
    /// </summary>
    internal static void TryApply(Harmony harmony)
    {
        if (_applied || _terminal)
        {
            return; // 幂等:每进程至多一次安装;终局裁决不复审
        }
        _harmony = harmony;

        Assembly? aftp = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == TargetAssembly);
        if (aftp == null)
        {
            // 缺席 != 终局:加载序由用户 mod 列表决定,AFTP 可能晚于本 mod 装载.
            MainFile.Logger.Info("[Spire1] AFTP absent - card-state compat untouched (Spire1 base behavior).");
            HookAssemblyLoad();
            return;
        }

        try
        {
            InstallCapability(harmony, aftp);
            _applied = true;
            UnhookAssemblyLoad();
            MainFile.Logger.Info($"[Spire1] AFTP card-state compat installed - Burn upgrade window restored on both exits, classic-Slimed marker wired for clone and save ({TargetAssembly} {aftp.GetName().Version}).");
        }
        catch (Exception e)
        {
            // 程序集一旦装载,成员解析与补丁安装是确定性操作:失败即终局,
            // 记一次带原因的 Error,摘除兜底,绝不重试(无重试风暴).
            FailTerminal($"{e.GetType().Name}: {e.Message}");
        }
    }

    private static void HookAssemblyLoad()
    {
        if (_hooked)
        {
            return; // 幂等:多次初始化复用同一兜底 handler
        }
        _hooked = true;
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        MainFile.Logger.Info("[Spire1] AFTP card-state compat deferred - waiting for the ActsFromThePast assembly to load.");
    }

    private static void UnhookAssemblyLoad()
    {
        if (!_hooked)
        {
            return;
        }
        _hooked = false;
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        if (_applied || _terminal || _harmony == null)
        {
            UnhookAssemblyLoad();
            return;
        }
        if (args.LoadedAssembly.GetName().Name != TargetAssembly)
        {
            return;
        }
        try
        {
            TryApply(_harmony); // 成功或终局都会在内部摘除兜底
        }
        catch (Exception e)
        {
            FailTerminal($"bind failed on assembly load - {e.GetType().Name}: {e.Message}");
        }
    }

    /// <summary>终局裁决:本会话内不再重试,Spire1 基础行为不受影响.</summary>
    private static void FailTerminal(string reason)
    {
        if (_terminal)
        {
            return;
        }
        _terminal = true;
        UnhookAssemblyLoad();
        MainFile.Logger.Error($"[Spire1] AFTP card-state compat: terminal, not retrying this session - {reason}.");
    }

    /// <summary>
    /// 单一 capability group(全有或全无):先解析并校验全部成员(无副作用),
    /// 再装可撤的 R4-01 补丁,最后做不可撤的 R4-02 注册.任何一步失败都撤销已装
    /// 补丁后以异常上行,由 <see cref="TryApply"/> 记终局原因.
    /// </summary>
    private static void InstallCapability(Harmony harmony, Assembly aftp)
    {
        // ---- 1. 解析并校验(此段不改任何状态) ----

        Type? burnPatch = aftp.GetType(BurnPatchType, throwOnError: false);
        if (burnPatch == null)
        {
            throw new InvalidOperationException($"type '{BurnPatchType}' not found - unsupported AFTP version.");
        }
        FieldInfo? allow = AccessTools.DeclaredField(burnPatch, AllowBurnUpgradeField);
        if (allow == null || !allow.IsStatic || allow.FieldType != typeof(bool))
        {
            throw new InvalidOperationException($"{BurnPatchType}.{AllowBurnUpgradeField} not found or shape changed - unsupported AFTP version.");
        }

        Type? tracker = aftp.GetType(ClassicSlimedTrackerType, throwOnError: false);
        if (tracker == null)
        {
            throw new InvalidOperationException($"type '{ClassicSlimedTrackerType}' not found - unsupported AFTP version.");
        }
        FieldInfo? markerField = AccessTools.DeclaredField(tracker, IsClassicSlimedField);
        if (markerField == null || !markerField.IsStatic
            || markerField.GetValue(null) is not SpireField<CardModel, bool> marker)
        {
            throw new InvalidOperationException($"{ClassicSlimedTrackerType}.{IsClassicSlimedField} not found or shape changed - unsupported AFTP version.");
        }

        MethodInfo? fromSerializable = AccessTools.Method(typeof(CardModel), nameof(CardModel.FromSerializable));
        if (fromSerializable == null || !fromSerializable.IsStatic
            || fromSerializable.ReturnType != typeof(CardModel)
            || fromSerializable.GetParameters().Length != 1
            || fromSerializable.GetParameters()[0].ParameterType != typeof(SerializableCard))
        {
            throw new InvalidOperationException($"CardModel.{nameof(CardModel.FromSerializable)}(SerializableCard) not found or shape changed - unsupported engine version.");
        }

        MethodInfo? prefix = AccessTools.Method(typeof(AftpCardStateCompat), nameof(FromSerializablePrefix));
        MethodInfo? finalizer = AccessTools.Method(typeof(AftpCardStateCompat), nameof(FromSerializableFinalizer));
        if (prefix == null || finalizer == null)
        {
            throw new InvalidOperationException("own patch methods not resolvable.");
        }

        // 访问器先编译,补丁后装:补丁在位时访问器必定可用(半套绑定不可能发生).
        _readAllowBurnUpgrade = CompileStaticFieldReader(allow);
        _writeAllowBurnUpgrade = CompileStaticFieldWriter(allow);
        _classicSlimed = marker;
        _burnId = ModelDb.GetId(typeof(Burn));

        // ---- 2. R4-01:可撤的补丁先装 ----
        try
        {
            harmony.Patch(fromSerializable,
                prefix: new HarmonyMethod(prefix),
                finalizer: new HarmonyMethod(finalizer));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"patching CardModel.{nameof(CardModel.FromSerializable)} failed - {e.GetType().Name}: {e.Message}");
        }

        // ---- 3. R4-02:不可撤的注册后做;失败则撤销第 2 步,保持全有或全无 ----
        try
        {
            // 克隆半.必须在 AFTP 第一次 Set 之前生效(见类注释第 2 点).
            marker.CopyOnClone();

            // 存档半.委托给 AFTP 的同一张表:一处存储,零漂移.
            bool registered = ExtendedSaveTypes.RegisterSavedValue<CardModel, bool>(
                ClassicSlimedSaveKey,
                static card => _classicSlimed!.Get(card),
                static (card, value) => _classicSlimed!.Set(card, value),
                static (value, writer) => writer.WriteBool(value),
                static reader => reader.ReadBool());
            if (!registered)
            {
                throw new InvalidOperationException($"ExtendedSaveTypes.RegisterSavedValue<CardModel,bool> refused the registration for key '{ClassicSlimedSaveKey}'.");
            }
        }
        catch
        {
            Rollback(harmony, fromSerializable);
            throw;
        }
    }

    /// <summary>按本层 Harmony ID 撤销已装补丁(安装中途失败的收尾).撤销自身失败只记
    /// Error,不再抛出--终局原因保留最初的那个.
    ///
    /// 次序是有意的:**终结器先撤,前缀后撤**(P3 复核发现,2026-09-15).两次 Unpatch 里
    /// 第一次成功,第二次抛,会留下半套状态,两种半套的后果不对称:
    /// - 先撤前缀(原实现):终留终结器,而 <c>__state</c> 已随前缀消失退化为默认
    ///   <c>false</c>,终结器于是**无条件写 false**--正好在嵌套调用里关掉 AFTP 自己
    ///   打开的窗口(复核实测 finalizerOnly_outerWindowPreserved=false),即 F3 故障模式,
    ///   会打断 AFTP 自己的 UpgradeAllBurnsAndAddMore.
    /// - 先撤终结器(现实现):终留前缀,门控继续放行且无人还原,退化为"泄漏 true".
    ///   而 AFTP 自己的 finally 块在窗口结束时显式写回 false(:3282/:3316),
    ///   下一次 Hexaghost 窗口关闭即自愈.
    /// 两者都不好,但泄漏可自愈,关掉别人的窗口不可.触发需要 Unpatch 抛异常,复核与
    /// 本层都未能复现,故为纵深防御而非已观测缺陷.</summary>
    private static void Rollback(Harmony harmony, MethodInfo fromSerializable)
    {
        try
        {
            harmony.Unpatch(fromSerializable, HarmonyPatchType.Finalizer, harmony.Id);
            harmony.Unpatch(fromSerializable, HarmonyPatchType.Prefix, harmony.Id);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AFTP card-state compat rollback failed ({e.GetType().Name}: {e.Message}) - a patch may be partially installed.");
        }
    }

    // ---- R4-01:窗口前缀 + 窗口终结器(producer/cleanup:本类) ----

    /// <summary>
    /// 放行一次升级过的 Burn 的重建,并把**捕获到的**先前值交给终结器.
    ///
    /// 次序是有意的:<c>__state</c> 先赋值,之后才做门控.若门控抛异常,终结器拿到的
    /// 仍是正确的先前值,还原不会把 AFTP 自己打开的窗口关掉;门控失败的后果只是
    /// "不放行",即 R4-01 维持未修的现状,不引入新故障.
    ///
    /// 门控只对<b>升级过的 Burn</b>放行:AFTP 只给 Burn 打了那个后置补丁
    /// (<c>burnPatchCount = 2</c>,均在 Burn 上),Burn 又是 sealed,所以没有别的卡
    /// 需要这个放宽;不放宽其它卡,就避免了"任意卡重建期间 Burn 被临时视为可升级"
    /// 这个可观测副作用(实测:不放宽时 <c>MaxUpgradeLevel</c> 全程为 0).
    /// </summary>
    private static void FromSerializablePrefix(SerializableCard save, out bool __state)
    {
        Func<bool>? read = _readAllowBurnUpgrade;
        Action<bool>? write = _writeAllowBurnUpgrade;
        __state = read != null && read(); // 先捕获,后门控:门控失败也能正确还原

        if (write == null || _burnId == null)
        {
            return; // 安装后不该发生;退化为不放行
        }

        try
        {
            if (save.Id == _burnId && save.CurrentUpgradeLevel > 0)
            {
                write(true);
            }
        }
        catch (Exception e)
        {
            if (!_gateFallbackLogged)
            {
                _gateFallbackLogged = true;
                MainFile.Logger.Error($"[Spire1] AFTP card-state compat: upgrade-window gate failed ({e.GetType().Name}: {e.Message}) - affected reconstructions keep the unfixed behavior (R4-01 remains), no new failure introduced.");
            }
        }
    }

    /// <summary>
    /// 还原捕获到的先前值.<c>__exception</c> 原样返回,不吞任何异常.
    ///
    /// 用 Finalizer 而非 Postfix 是实测结论:Postfix 在抛出的那次重建里不执行,会留下
    /// <c>AllowBurnUpgrade = true</c>,此后整个会话每张 Burn 的 <c>MaxUpgradeLevel</c>
    /// 都是 1,比不修更糟.实测本终结器在正常返回与异常退出两条路径上都还原,且嵌套
    /// 调用时外层已开的窗口保持打开.
    /// </summary>
    private static Exception? FromSerializableFinalizer(Exception? __exception, bool __state)
    {
        try
        {
            _writeAllowBurnUpgrade?.Invoke(__state);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AFTP card-state compat: restoring AllowBurnUpgrade failed ({e.GetType().Name}: {e.Message}).");
        }
        return __exception;
    }

    // ---- 静态字段访问器编译(一次性;补丁在位期间零反射) ----

    private static Func<bool> CompileStaticFieldReader(FieldInfo field)
    {
        var read = Expression.Field(null, field);
        return Expression.Lambda<Func<bool>>(read).Compile();
    }

    private static Action<bool> CompileStaticFieldWriter(FieldInfo field)
    {
        var value = Expression.Parameter(typeof(bool), "value");
        var assign = Expression.Assign(Expression.Field(null, field), value);
        return Expression.Lambda<Action<bool>>(assign, value).Compile();
    }
}
