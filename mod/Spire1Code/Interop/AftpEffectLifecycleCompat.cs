using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
// bare Expression = LINQ; Godot.Expression is unrelated and must never be picked up
using Expression = System.Linq.Expressions.Expression;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// AFTP（创意工坊 3746969593 "Acts from the Past"，mod manifest version 1.0.5）特效节点
/// 生命周期兼容层（AFTP-1，docs/STS-PERFORMANCE-PLAN-2026-09-15.md）。
///
/// 缺口（反编译实锤，工坊 DLL 1.0.5 与两份源码树一致）：<c>NSts1Effect.OnTreeEntered</c>
/// 在节点进树时调 <c>Initialize()</c> 并订阅 <c>SceneTree.ProcessFrame</c>；而
/// <c>OnProcessFrame</c> 的退树分支在 <c>IsInsideTree()==false</c> 之后再调
/// <c>GetTree()</c>——Godot 中节点不在树内时 <c>GetTree()</c> 返回 null，随后的
/// <c>null.ProcessFrame -=</c> 按 C# 语义抛 NullReferenceException，且异常发生在退订
/// 完成之前，handler 会留在 SceneTree 上逐帧重蹈（astra AFTP-R4-03：源码级风险，
/// 本层不量化任何泄漏率/性能数字）。同因：TreeEntered 每次进树都触发，节点
/// 退树再进树会重复 Initialize（FireFlyEffect 等会在 Initialize 里 AddChild 新
/// Sprite2D，即重复子节点）并二次订阅 ProcessFrame（每帧双份 Update/delta）。
///
/// 桥接面（单一 capability group，全有或全无——半套绑定比不绑定更糟）：
/// Prefix 替换 <c>NSts1Effect.OnTreeEntered</c>（private instance void；已确认它是
/// 46 个特效子类唯一的 ProcessFrame 订阅点）。替换后的订阅由本类全权拥有：
/// 1. Initialize 每实例至多一次（进树重入不重建子节点、不重掷 RNG）；
/// 2. 订阅瞬间缓存 <c>SceneTree</c> 引用；此后一切退订只走缓存引用，
///    退树之后绝无 <c>GetTree()</c> 调用；
/// 3. 对称清理：退树（≤1 帧窗口，窗口内 handler 先做 no-op 检查）、
///    IsDone 完结、节点被外部释放三条路径都退订同一 delegate 并清空状态；
/// 4. ProcessMode.Always / SetProcess 由原 <c>Setup()</c> 原样保留（本层不碰），
///    帧驱动仍是 SceneTree.ProcessFrame，delta 仍取自同一节点的
///    <c>Node.GetProcessDeltaTime()</c>，调用次序与原 handler 一致
///    （进树检查 -> Update(delta) -> IsDone -> 退订 -> QueueFree），
///    暂停/菜单/战斗中的表现语义不变。
///
/// 生命周期（每个订阅都在注释里落 producer/owner/first-consumer/cleanup）：
/// - producer：AFTP <c>NSts1Effect.Setup()</c> 连接的 TreeEntered 信号（每特效实例一次）；
/// - owner：本类（Spire1 互操作层）；补丁只装一次（<see cref="_applied"/> 锁存）；
/// - first consumer：AFTP 特效的 <c>Update(float)</c> override（经编译委托虚分派）；
/// - cleanup：<see cref="EffectBinding.Detach"/>（退树/完结/外部释放/树消亡），
///   委托从缓存的 SceneTree 上按同一实例摘除，无静态节点/handler 账本可累积。
///
/// 失败矩阵（可选性契约，缺席 = 零影响）：
/// - AFTP 程序集缺席：只记一条 Info，不装补丁；按 AutoAnthonyLoadHook 先例挂
///   AssemblyLoad 兜底（Spire1 与 AFTP 无依赖边，加载序由用户 mod 列表决定）。
/// - 程序集在而成员解析失败（版本漂移）：终局裁决一次（<see cref="_terminal"/>），
///   带原因记 Error，摘除 AssemblyLoad 兜底，本会话不再重试（无重试风暴）。
/// - 绑定后个别节点异常：该节点回退到 AFTP 原始订阅路径（保底原语义），
///   全进程只记一次 Error（有界日志）。
///
/// 线程假设：与 AutoAnthonyCompatBridge 相同——ModManager initializer 与 AssemblyLoad
/// 都发生在主线程加载阶段，本类不加锁。
///
/// 明确不做（各归其主）：FireFly trail 分配优化（AFTP-2）；Match-and-Keep 奖励完成
/// 语义（独立正确性任务）；Burn+/Classic Slimed 序列化（独立正确性任务）；
/// InteractableTorchEffect / 三张战斗背景 / HexaghostVisuals 各自的成对订阅或
/// await 生命周期（它们有 _ExitTree 配对退订或 ToSignal，不在本缺口内）；
/// 不构建/替换 AFTP 主 DLL 与 PCK。纯反射绑定，本程序集不引用 AFTP 编译期类型，
/// 无 SPIRE1_AFTP 条件编译需要。
/// </summary>
internal static class AftpEffectLifecycleCompat
{
    private const string TargetAssembly = "ActsFromThePast";
    private const string EffectBaseType = "ActsFromThePast.NSts1Effect";
    private const string SubscribeMethodName = "OnTreeEntered";
    private const string InitializeMethodName = "Initialize";
    private const string UpdateMethodName = "Update";
    private const string IsDoneFieldName = "IsDone";

    // 终态锁存：_applied = 补丁已装（幂等，重复初始化不再挂）；_terminal = 判定不兼容，
    // 带原因退场，永不重试。两者都不置位 = 程序集尚未出现，兜底等待中。
    private static bool _applied;
    private static bool _terminal;
    private static bool _hooked;
    private static bool _fallbackLogged;
    private static Harmony? _harmony;

    // 一次性编译的成员访问器（补丁安装前全部就绪；安装后不可为 null）。
    // 每帧零反射零装箱：委托为开实例（open-instance）编译，直接吃 object + float。
    private static Action<object>? _initialize;
    private static Action<object, float>? _update;
    private static Func<object, bool>? _isDone;

    // 每实例绑定状态。ConditionalWeakTable：键（特效节点）存活期间状态存活，
    // 节点消亡即随之可回收——房 cycling/重复初始化不会留下静态累积。
    private static readonly ConditionalWeakTable<object, EffectBinding> Bindings = new();

    /// <summary>
    /// 挂载入口。MainFile 集成点：Phase3PatchAndInteropRegistration 内、
    /// <c>AutoAnthonyLoadHook.TryApplyBridge(harmony)</c> 之后一行
    /// <c>AftpEffectLifecycleCompat.TryApply(harmony);</c>（SpireCore 负责接入，
    /// 本文件不触碰 MainFile）。
    /// </summary>
    internal static void TryApply(Harmony harmony)
    {
        if (_applied || _terminal)
        {
            return; // 幂等：每进程至多一次安装；终局裁决不复审
        }
        _harmony = harmony;

        Assembly? aftp = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == TargetAssembly);
        if (aftp == null)
        {
            // 缺席 != 终局：加载序由用户 mod 列表决定，AFTP 可能晚于本 mod 装载。
            MainFile.Logger.Info("[Spire1] AFTP absent - effect lifecycle untouched (Spire1 base behavior).");
            HookAssemblyLoad();
            return;
        }

        try
        {
            InstallCapability(harmony, aftp);
            _applied = true;
            UnhookAssemblyLoad();
            MainFile.Logger.Info($"[Spire1] AFTP effect-lifecycle compat installed - NSts1Effect frame subscription now owned by Spire1 ({TargetAssembly} {aftp.GetName().Version}).");
        }
        catch (Exception e)
        {
            // 程序集一旦装载，成员解析与补丁安装是确定性操作：失败即终局，
            // 记一次带原因的 Error，摘除兜底，绝不重试（无重试风暴）。
            FailTerminal($"{e.GetType().Name}: {e.Message}");
        }
    }

    private static void HookAssemblyLoad()
    {
        if (_hooked)
        {
            return; // 幂等：多次初始化复用同一兜底 handler
        }
        _hooked = true;
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        MainFile.Logger.Info("[Spire1] AFTP effect-lifecycle compat deferred - waiting for the ActsFromThePast assembly to load.");
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

    /// <summary>终局裁决：本会话内不再重试，Spire1 基础行为不受影响。</summary>
    private static void FailTerminal(string reason)
    {
        if (_terminal)
        {
            return;
        }
        _terminal = true;
        UnhookAssemblyLoad();
        MainFile.Logger.Error($"[Spire1] AFTP effect-lifecycle compat: terminal, not retrying this session - {reason}.");
    }

    /// <summary>
    /// 单一 capability group（全有或全无）：解析类型/成员 -> 编译访问器 -> 装 Prefix。
    /// 任何一步失败都以异常上行，由 <see cref="TryApply"/> 记终局原因。
    /// </summary>
    private static void InstallCapability(Harmony harmony, Assembly aftp)
    {
        Type? effectType = aftp.GetType(EffectBaseType, throwOnError: false);
        if (effectType == null)
        {
            throw new InvalidOperationException($"type '{EffectBaseType}' not found - unsupported AFTP version.");
        }

        // v1.0.5 咽喉点：OnTreeEntered 是特效家族唯一的 ProcessFrame 订阅点
        // （对工坊 DLL 全量反编译核验；背景/火把/HexaghostVisuals 的订阅不在本类内）。
        MethodInfo? subscribe = AccessTools.Method(effectType, SubscribeMethodName);
        if (subscribe == null || subscribe.IsStatic || subscribe.IsAbstract
            || subscribe.ReturnType != typeof(void) || subscribe.GetParameters().Length != 0)
        {
            throw new InvalidOperationException($"NSts1Effect.{SubscribeMethodName} not found or shape changed - unsupported AFTP version.");
        }

        MethodInfo? initialize = AccessTools.Method(effectType, InitializeMethodName);
        if (initialize == null || initialize.IsStatic || initialize.ReturnType != typeof(void)
            || initialize.GetParameters().Length != 0)
        {
            throw new InvalidOperationException($"NSts1Effect.{InitializeMethodName} not found or shape changed - unsupported AFTP version.");
        }

        MethodInfo? update = AccessTools.Method(effectType, UpdateMethodName);
        if (update == null || update.IsStatic || update.ReturnType != typeof(void)
            || update.GetParameters().Length != 1 || update.GetParameters()[0].ParameterType != typeof(float))
        {
            throw new InvalidOperationException($"NSts1Effect.{UpdateMethodName}(float) not found or shape changed - unsupported AFTP version.");
        }

        FieldInfo? isDone = AccessTools.Field(effectType, IsDoneFieldName);
        if (isDone == null || isDone.IsStatic || isDone.FieldType != typeof(bool))
        {
            throw new InvalidOperationException($"NSts1Effect.{IsDoneFieldName} not found or shape changed - unsupported AFTP version.");
        }

        // 先编译访问器、后装补丁：补丁在位时访问器必定可用（半套绑定不可能发生）。
        _initialize = CompileVoidCall(initialize);
        _update = CompileUpdateCall(update);
        _isDone = CompileFieldReader(isDone);

        harmony.Patch(subscribe, prefix: new HarmonyMethod(typeof(AftpEffectLifecycleCompat), nameof(SubscribePrefix)));
    }

    // ---- 每实例订阅管理（owner：本类） ----

    /// <summary>
    /// Harmony Prefix，整体替换 <c>NSts1Effect.OnTreeEntered</c> 原体
    /// （原体 = Initialize() + GetTree().ProcessFrame += OnProcessFrame，即缺口所在）。
    /// 返回 false 跳过原体；意外异常时回退原体（保底 AFTP 原语义），有界日志一次。
    /// </summary>
    private static bool SubscribePrefix(object __instance)
    {
        try
        {
            BindNode(__instance);
        }
        catch (Exception e)
        {
            // A1 fix (P2, 2026-09-15 review): hand the instance to AFTP's original body PERMANENTLY.
            // The original OnTreeEntered calls Initialize() again and installs its own
            // OnProcessFrame, so this class must never subscribe the same instance afterwards -
            // otherwise both handlers run and the effect advances twice per frame. Marking the
            // binding sticky is what makes that impossible; a throw here can happen AFTER the
            // Initialize latch was set (the latch is set before the call on purpose, to keep
            // Initialize at-most-once), so without this flag a later re-entry would subscribe.
            GetOrCreateBinding(__instance).OriginalOwned = true;
            if (!_fallbackLogged)
            {
                _fallbackLogged = true;
                MainFile.Logger.Error($"[Spire1] AFTP effect-lifecycle: managed subscribe failed ({e.GetType().Name}: {e.Message}) - affected nodes fall back to AFTP's original subscription (original lifecycle semantics) for the rest of their lifetime.");
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 受管订阅绑定（producer：TreeEntered 信号；owner：本类；first consumer：
    /// <see cref="EffectBinding.Tick"/>；cleanup：<see cref="EffectBinding.Detach"/>）。
    /// 订阅是本方法最后一步副作用:即使后续出现异常也不存在半挂状态.
    ///
    /// 回退路径(A1 修正,2026-09-15 复核):原体 <c>OnTreeEntered</c> 会再调一次 Initialize 并挂上
    /// 它自己的 <c>OnProcessFrame</c>.因此回退必须把该实例**永久**交给原体:
    /// <see cref="EffectBinding.OriginalOwned"/> 置位后本类不再订阅它,否则同一节点会同时挂着
    /// 原体的处理器与本类的 Tick,每帧各推进一次 Update.原实现声称回退不会造成双重订阅,那只在
    /// Initialize **确定性**失败时成立(原体自己的 Initialize 同样抛,什么都没挂上);瞬时失败会
    /// 破坏该前提,所以现在靠标志位而非推理来保证.
    /// </summary>
    private static void BindNode(object instance)
    {
        var node = (Godot.Node)instance;
        if (!GodotObject.IsInstanceValid(node))
        {
            return; // 进树前已被释放：无可托管之物（防御分支）
        }

        EffectBinding binding = Bindings.GetOrCreateValue(instance);

        if (binding.OriginalOwned)
        {
            // A1 fix: this instance was handed to AFTP's original body on an earlier failure, so
            // the original already called Initialize() and installed its own OnProcessFrame.
            // Subscribing here as well would double-advance the effect and give QueueFree two
            // reachable paths. Return WITHOUT touching the node: the original owns it now.
            return;
        }

        SceneTree tree = node.GetTree(); // 合法时刻:TreeEntered 触发时节点已在树内(原体同样依赖这一点)

        if (!binding.Initialized)
        {
            binding.Initialized = true; // 先锁存：Initialize 至多一次（重入不重建子节点/不重掷 RNG）
            _initialize!(instance);
        }

        if (binding.FrameHandler != null)
        {
            if (ReferenceEquals(binding.Tree, tree))
            {
                return; // 同帧内退树又进树：仍挂在本树上，不重复订阅
            }
            binding.Detach(); // 换树重绑：先对称摘除旧挂载
        }

        binding.Node = node;
        binding.Tree = tree;
        binding.FrameHandler = binding.Tick; // 每实例 Action，target=binding，无闭包分配
        tree.ProcessFrame += binding.FrameHandler;
    }

    /// <summary>取(或建)某实例的绑定状态.与 <c>Bindings.GetOrCreateValue</c> 同一语义,
    /// 单独包一层是为了让回退路径也能拿到绑定(它可能在 BindNode 抛点之后才执行).</summary>
    private static EffectBinding GetOrCreateBinding(object instance)
        => Bindings.GetOrCreateValue(instance);

    /// <summary>每实例帧回调(事件类型为游戏 GodotSharp 4.5.1 的
    /// <c>SceneTree.ProcessFrame : Action</c>）。次序与原 <c>OnProcessFrame</c> 严格一致；
    /// 唯一差别是退树分支不再调 <c>GetTree()</c>，改用订阅时缓存的树引用退订。</summary>
    private sealed class EffectBinding
    {
        // 仅在已挂载期间持强引用（树 -> delegate -> binding -> node，与原体同形）；
        // Detach 后三者清空，不保留任何已脱离节点的引用图。
        public Godot.Node? Node;
        public SceneTree? Tree; // 订阅瞬间缓存;Detach 即清空--此后不存在 GetTree 调用
        public Action? FrameHandler; // 挂在 Tree.ProcessFrame 上的精确 delegate 实例
        public bool Initialized; // Initialize 每实例至多一次
        /// <summary>
        /// 该实例已交回 AFTP 原体接管(回退路径).置位后本类永不再为该实例订阅,
        /// 否则会出现双重订阅:A1(P2,2026-09-15 复核发现).
        ///
        /// 缺口机制:原体 <c>OnTreeEntered</c> = <c>Initialize(); GetTree().ProcessFrame += OnProcessFrame;</c>
        /// (反编译 :15737-15741).若 <c>Initialize</c> 抛瞬时异常(非确定性失败),前缀的 catch
        /// 会 <c>return true</c> 放行原体:原体再调一次 Initialize 并挂上自己的 OnProcessFrame,
        /// 而本实例的 <c>Initialized</c> 锁存已在抛点之前置位、Node/Tree/FrameHandler 三者仍为 null.
        /// 此后该节点若再次进树,BindNode 会跳过 Initialize(锁存生效)却走到尾部订阅,于是同一节点
        /// 同时挂着原体的 OnProcessFrame 与本类的 Tick--两者每帧各调一次 Update、各自测 IsDone,
        /// 特效推进速度翻倍且 QueueFree 有两条路径可达.
        ///
        /// 因此回退必须是"粘性"的:一旦放行给原体,该实例的整个生命周期都归原体所有.
        /// 确定性失败(Initialize 每次都抛)本来就不会走到订阅,与本标志一致,所以这不改变
        /// 文档里描述的那种情形,只堵住瞬时失败这一条.
        /// </summary>
        public bool OriginalOwned;

        public void Tick()
        {
            Godot.Node? node = Node;
            SceneTree? tree = Tree;
            if (node == null || tree == null || FrameHandler == null || _update == null || _isDone == null)
            {
                Detach(); // 已脱离/访问器缺失：纯 no-op 退场（不触碰引擎对象）
                return;
            }
            if (!GodotObject.IsInstanceValid(node))
            {
                Detach(); // 节点被外部释放（未经 IsDone 路径）：对称清理
                return;
            }
            if (!node.IsInsideTree())
            {
                Detach(); // 退树：≤1 帧窗口内的对称清理；此处绝不调用 GetTree()
                return;
            }

            float delta = (float)node.GetProcessDeltaTime(); // 与原体相同的 delta 来源
            _update(node, delta); // 虚分派到各特效 override
            if (_isDone(node))
            {
                Detach();     // 完结：先退订（与原体次序一致）
                node.QueueFree();
            }
        }

        /// <summary>对称退订：按缓存的树引用摘除同一 delegate 实例并清空全部状态。
        /// 树已消亡时无需（也无法）退订，直接清空本端引用。</summary>
        public void Detach()
        {
            Action? handler = FrameHandler;
            SceneTree? tree = Tree;
            FrameHandler = null;
            Tree = null;
            Node = null;
            if (handler == null || tree == null)
            {
                return;
            }
            if (!GodotObject.IsInstanceValid(tree))
            {
                return; // 树已消亡（如进程收尾）：事件源不复存在
            }
            tree.ProcessFrame -= handler;
        }
    }

    // ---- 开实例访问器编译（一次性；每帧调用零反射、零装箱） ----

    private static Action<object> CompileVoidCall(MethodInfo method)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var call = Expression.Call(Expression.Convert(instance, method.DeclaringType!), method);
        return Expression.Lambda<Action<object>>(call, instance).Compile();
    }

    private static Action<object, float> CompileUpdateCall(MethodInfo method)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var delta = Expression.Parameter(typeof(float), "delta");
        var call = Expression.Call(Expression.Convert(instance, method.DeclaringType!), method, delta);
        return Expression.Lambda<Action<object, float>>(call, instance, delta).Compile();
    }

    private static Func<object, bool> CompileFieldReader(FieldInfo field)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var read = Expression.Field(Expression.Convert(instance, field.DeclaringType!), field);
        return Expression.Lambda<Func<object, bool>>(read, instance).Compile();
    }
}
