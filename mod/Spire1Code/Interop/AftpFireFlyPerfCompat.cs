using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// AFTP(创意工坊 3746969593 "Acts from the Past",mod manifest version 1.0.5)FireFly
/// 拖尾分配/抖动兼容层(AFTP-2,docs/STS-PERFORMANCE-PLAN-2026-09-15.md).
///
/// 缺口(反编译实锤,工坊 DLL 1.0.5 与两份源码树一致):<c>FireFlyEffect.Update</c> 每
/// 0.04 秒(<c>TrailTime</c>)采一个拖尾样本 -- new Sprite2D + 逐属性赋值 + AddChild +
/// <c>_trailSprites.Add</c>;样本数超过 30(<c>TrailMaxAmt</c>)后每次采样都对最旧样本
/// <c>QueueFree</c> 并 <c>RemoveAt(0)</c>.即每个存活萤火虫每秒约 25 次 Sprite2D 构造
/// 加 25 次节点销毁;单个特效一生(<c>StartingDuration</c> 6-14 秒)约 150-350 次.
/// <c>TheCityBackground.UpdateFireFlies</c> 同时最多 9 只(decompile 28594:
/// <c>_fireFlies.Count &lt; 9</c>).同因:<c>CreateAdditiveMaterial</c> 每次调用都 new 一个
/// <c>CanvasItemMaterial</c> 且只设 <c>BlendMode = Add</c>,头精灵(<c>Initialize</c>)与
/// 每个拖尾样本各一个,同样是每秒约 25 次分配.本层不量化任何帧时间或 GC 收益(未做
/// 原生 A/B),只消除分配与销毁动作本身;上面的次数是按代码结构推导,不是测量值.
///
/// 桥接面(单一 capability group,全有或全无 -- 半套绑定比不绑定更糟):
/// 1. Transpiler <c>FireFlyEffect.Update</c>:两处机械 1:1 指令替换,其余指令逐字节
///    不变,因此索引,alpha 衰减,scale,0.04 秒节奏,RNG 次序,位置与 duration 全部
///    按构造不受影响:
///    a) <c>IL_0068: newobj instance void [GodotSharp]Godot.Sprite2D::.ctor()</c>
///       -&gt; <c>call class [GodotSharp]Godot.Sprite2D AcquireTrailSprite()</c>
///       (270 条指令体中的第 37 条;栈效果同为 pop 0 / push Sprite2D,其后仍是
///       stloc 到 Sprite2D 局部变量,故后续赋值序列无需任何改动)
///    b) <c>IL_0115: callvirt instance void [GodotSharp]Godot.Node::QueueFree()</c>
///       -&gt; <c>call void ReleaseTrailSprite(class [GodotSharp]Godot.Sprite2D)</c>
///       (第 103 条;栈效果同为 pop 1 / push 0,被弹出值静态类型即 Sprite2D -- 它来自
///       <c>List&lt;Sprite2D&gt;::get_Item(0)</c>,故不需要任何转换指令)
///    两处替换都只改 opcode 与 operand,不增删指令,labels/blocks 随指令对象保留,
///    因此所有分支目标与异常块边界不变.
/// 2. Prefix <c>FireFlyEffect.CreateAdditiveMaterial</c>:返回一个进程级共享,创建后
///    永不修改的 <c>CanvasItemMaterial</c>(<c>BlendMode = Add</c>).该方法只有两个调用点
///    (Initialize 头精灵 <c>IL_00b3</c>,Update 拖尾样本 <c>IL_00ab</c>),两点都紧跟
///    <c>CanvasItem::set_Material</c>,因此共享材质覆盖全部使用点.共享面之所以成立,
///    已核验的是:AFTP 全库对材质的写入只有构造期那一处 <c>BlendMode</c>(逐处 grep),
///    构造之后没有任何代码再写材质属性,也没有把材质存进字段留待后改.
///    未被核验的是:引擎或第三方 mod 若自行改写这个共享实例,不在本层的保证范围内.
///
/// 生命周期(producer/owner/first-consumer/cleanup):
/// - 拖尾精灵池:producer = <c>FireFlyEffect.Update</c> 的采样块(经替换后的
///   <see cref="AcquireTrailSprite"/>);owner = 本类;first consumer = 同一采样块的逐属性
///   赋值与 AddChild;cleanup = 淘汰路径的 <see cref="ReleaseTrailSprite"/>(父检查
///   RemoveChild 后入池).
/// - 共享材质:producer = 第一次 <c>CreateAdditiveMaterial</c> 调用(懒建,与 AFTP 原本
///   首次创建的时机相同,避免在 mod 初始化阶段触碰引擎资源系统);owner = 本类(静态
///   引用,进程级);first consumer = 同一次调用的 <c>__result</c>;cleanup = 无(不可变,
///   进程退出时随引擎释放).
/// - 池内精灵的收尾:本层不注册任何退出清理.进程退出时池里(至多 <see cref="TrailPoolCapacity"/>
///   个)无父精灵的 native 对象随引擎一并释放;它们不属于任何场景树,故房间或场景切换
///   不会回收它们,这一点是有意为之(否则复用面会在切场景时失效).池本身只在主线程访问,
///   不存在并发归还的竞态.
///
/// 池的存放/上限/释放语义:
/// - 存放:本类的静态 <c>Stack&lt;Sprite2D&gt;</c>(LIFO,最近释放者优先复用,保持工作集
///   小).池只持有"已被 RemoveChild 摘除父节点"的精灵,故池中节点不在任何场景树里,
///   不会被房间或场景切换连带释放.
/// - 上限:<see cref="TrailPoolCapacity"/> = 64.推导:同一个采样块内先取后还(见下),稳态
///   下每个存活特效最多占用 1 个空闲精灵,同时最多 9 只萤火虫,故稳态占用约 9;上限
///   64 给出约 7 倍余量,同时硬性封顶保留的节点数.达到上限时超出的精灵不入池,而是照
///   原语义 QueueFree 销毁,并只记一条 Info(有界日志).
/// - 释放 vs 真正销毁:<see cref="ReleaseTrailSprite"/> 把精灵摘出场景树并放回池(不销毁,
///   留着复用);只有不入池的路径才 QueueFree 销毁.仍在 <c>_trailSprites</c> 里的活精灵
///   (至多 30)与头精灵从不入池,它们在特效节点被释放时随父节点一起由引擎销毁.
/// - 仍带父节点时的语义:<see cref="ReleaseTrailSprite"/> 被调用的那一刻,精灵仍是特效节点
///   的子节点(原实现是 QueueFree,延迟到帧末才真正删除),所以释放路径必须做父检查
///   RemoveChild:若 <c>GetParent() != null</c> 则先 RemoveChild 再入池;若摘除后仍带父
///   (理论上不可能)则不入池并照原语义销毁 -- "池只存无父精灵"的不变量优先于复用率,
///   且不留下游离节点.这也是复用可行的前提:带父节点的精灵再次 AddChild 会失败.
///
/// 依赖的节点身份/次序保证:
/// - 身份:<c>_trailSprites</c> 里存的就是 AddChild 的同一个实例,淘汰路径
///   <c>get_Item(0)</c> 取回的仍是该实例,故 Release 收到的实例必是 Acquire 曾经返回
///   的那个(池按引用复用,不做任何身份映射);
/// - 复用不残留状态的依据(逐条核验,不是推断):对 FireFlyEffect 整个类做过一次
///   全量 setter 扫描,该类写过的属性只有 9 个 --
///   Sprite2D::Texture / RegionEnabled / RegionRect / Centered,CanvasItem::Material
///   (这 5 个由构造块写,而构造块被 Transpiler 原样保留,每次取样都重写),
///   Node2D::GlobalPosition,CanvasItem::Modulate,Node2D::Scale
///   (这 3 个由 <c>UpdateSprite</c> 写,而它在同一次 Update 内,新精灵入列之后无条件
///   执行,见下),以及材质工厂里的 CanvasItemMaterial::BlendMode.
///   也就是说:池中精灵身上"AFTP 会写的属性"被完整重写,不存在需要本层额外重置的项.
///   AFTP 从不写的那些属性(Rotation, Skew, Offset, FlipH/V, Visible, SelfModulate,
///   TopLevel, YSortEnabled, ZAsRelative 等)保持引擎默认值,复用与否都一样;
///   ZIndex 是设在特效节点上的(<c>TheCityBackground</c> 的 <c>fly.ZIndex = -15</c>),
///   不是设在精灵上.
/// - 帧内次序(为什么 Modulate/GlobalPosition/Scale 不需要在 acquire 时重置):
///   同一次 <c>Update</c> 内,采样块先执行(构造 -> AddChild -> 入列),<c>UpdateSprite</c>
///   在方法末尾无条件执行(IL 里是最后一条 call),其循环对 <c>_trailSprites</c> 里每一个
///   活精灵重写这三个属性 -- 新取的精灵在当帧就被写成正确值,不存在"带着上一世状态
///   渲染一帧"的窗口.所以本层刻意不在 acquire 里重置任何属性:那既无必要,也会白白
///   增加每样本的开销.
/// - 池中精灵不被本层之外的代码触碰:它已被 RemoveChild 摘出场景树,不在任何场景树里,
///   故引擎的节点遍历与处理流程都到不了它;AFTP 自己也只在把它放回池之后就不再持有引用.
/// - 次序:同一个采样块内先 acquire(构造)后 release(淘汰),故刚释放的精灵最早也要到
///   下一个采样块才会被再次取出,取出时它已完全脱离父节点;
/// - 与 AFTP-1 正交:AFTP-1 只接管 ProcessFrame 订阅与退树清理,不改变本类的 Update
///   调用次序,两者可同时生效.
///
/// 失败矩阵(可选性契约,缺席 = 零影响):
/// - AFTP 程序集缺席:只记一条 Info,不装补丁;挂 AssemblyLoad 兜底(与 AFTP-1 同).
/// - 程序集在而类型或成员解析失败,或 Update/Initialize 的 IL 形状与上述锚点不符(版本
///   漂移):终局裁决一次(<see cref="_terminal"/>),带原因记 Error,摘除兜底,本会话不再
///   重试;先验证后安装,故此时不安装任何补丁.
/// - 安装中途失败:按本层 Harmony ID 撤销已装的那一个后再终局,绝不留半套能力.
/// - 运行期材质创建失败:该次调用回退原体(按调用分配,原语义),全进程只记一次 Error.
///
/// 线程假设:与 AFTP-1 相同 -- 全部池与材质访问都发生在 Godot 主线程(producer 是
/// <c>SceneTree.ProcessFrame</c> -&gt; <c>NSts1Effect.OnProcessFrame</c> -&gt;
/// <c>FireFlyEffect.Update</c>,以及 TreeEntered -&gt; Initialize).本类不加锁,也不承诺
/// 跨线程安全;若未来 AFTP 版本把 Update 挪到别的线程,本层必须重新审计(此处显式
/// 标记,不假装已覆盖).
///
/// 明确不做(各归其主):不改 AFTP 的 DLL 与 PCK;不动头精灵的 new Sprite2D(在
/// <c>Initialize</c> 内,每特效一次,不构成抖动);不动 <c>NSts1Effect</c> 自身节点的
/// QueueFree(AFTP-1 的生命周期领地);不做原生 A/B 性能测量与收益声明(属 GATE-1
/// 测量流程);除下列已知差异外不做任何行为变更.已知且被接受的差异:被淘汰的最旧
/// 样本原本会多渲染一帧(QueueFree 到帧末才生效),现在在淘汰当刻即被摘出场景树,
/// 故最旧(alpha 最低)的样本提前一帧消失;其余样本的位置,alpha,scale 不受影响.
/// 纯反射绑定,本程序集不引用 AFTP 编译期类型,无 SPIRE1_AFTP 条件编译需要.
/// </summary>
internal static class AftpFireFlyPerfCompat
{
    private const string TargetAssembly = "ActsFromThePast";
    private const string EffectTypeName = "ActsFromThePast.FireFlyEffect";
    private const string UpdateMethodName = "Update";
    private const string InitializeMethodName = "Initialize";
    private const string MaterialFactoryName = "CreateAdditiveMaterial";
    private const string TrailListFieldName = "_trailSprites";

    /// <summary>空闲拖尾精灵的硬上限(见类注释的推导).到顶后超出者丢弃而非入池,
    /// 故本层保留的 native 节点数有确定上界.</summary>
    private const int TrailPoolCapacity = 64;

    // 终态锁存:_applied = 补丁已装(幂等,重复初始化不再挂);_terminal = 判定不兼容,
    // 带原因退场,永不重试.两者都不置位 = 程序集尚未出现,兜底等待中.
    private static bool _applied;
    private static bool _terminal;
    private static bool _hooked;
    private static bool _materialFallbackLogged;
    private static bool _saturationLogged;
    private static Harmony? _harmony;

    // 注入用的两个方法(补丁安装前解析就绪;安装后不可为 null).Transpiler 每帧零反射:
    // 直接吃这里的 MethodInfo 作为 operand.
    private static MethodInfo? _acquireTrailSprite;
    private static MethodInfo? _releaseTrailSprite;

    // 共享材质与空闲池.两者都是本类的静态状态,进程级;池只在主线程访问,故不加锁.
    private static CanvasItemMaterial? _sharedAdditiveMaterial;
    private static readonly Stack<Sprite2D> IdleTrailSprites = new();

    /// <summary>
    /// 挂载入口.MainFile 集成点:Phase3PatchAndInteropRegistration 内,
    /// <c>Interop.AftpEffectLifecycleCompat.TryApply(harmony);</c> 之后一行
    /// <c>Interop.AftpFireFlyPerfCompat.TryApply(harmony);</c>(SpireCore 负责接入,
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
            MainFile.Logger.Info("[Spire1] AFTP absent - FireFly trail churn untouched (AFTP's own per-sample allocation).");
            HookAssemblyLoad();
            return;
        }

        try
        {
            InstallCapability(harmony, aftp);
            _applied = true;
            UnhookAssemblyLoad();
            MainFile.Logger.Info($"[Spire1] AFTP FireFly trail-churn compat installed - trail sprites pooled (cap {TrailPoolCapacity}), one shared additive material ({TargetAssembly} {aftp.GetName().Version}).");
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
        MainFile.Logger.Info("[Spire1] AFTP FireFly trail-churn compat deferred - waiting for the ActsFromThePast assembly to load.");
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

    /// <summary>终局裁决:本会话内不再重试,AFTP 基础行为不受影响.</summary>
    private static void FailTerminal(string reason)
    {
        if (_terminal)
        {
            return;
        }
        _terminal = true;
        UnhookAssemblyLoad();
        MainFile.Logger.Error($"[Spire1] AFTP FireFly trail-churn compat: terminal, not retrying this session - {reason}.");
    }

    /// <summary>
    /// 单一 capability group(全有或全无):解析类型与成员 -&gt; 读原体 IL 做模式校验
    /// -&gt; 装 Transpiler 与 Prefix.任何一步失败都以异常上行,由 <see cref="TryApply"/>
    /// 记终局原因.模式校验先于任何补丁安装,故校验失败时不存在半套能力.
    /// </summary>
    private static void InstallCapability(Harmony harmony, Assembly aftp)
    {
        Type? effectType = aftp.GetType(EffectTypeName, throwOnError: false);
        if (effectType == null)
        {
            throw new InvalidOperationException($"type '{EffectTypeName}' not found - unsupported AFTP version.");
        }

        MethodInfo? update = AccessTools.DeclaredMethod(effectType, UpdateMethodName, [typeof(float)]);
        if (update == null || update.IsStatic || update.IsAbstract || update.ReturnType != typeof(void))
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName}(float) not found or shape changed - unsupported AFTP version.");
        }

        MethodInfo? initialize = AccessTools.DeclaredMethod(effectType, InitializeMethodName, Type.EmptyTypes);
        if (initialize == null || initialize.IsStatic || initialize.ReturnType != typeof(void))
        {
            throw new InvalidOperationException($"FireFlyEffect.{InitializeMethodName}() not found or shape changed - unsupported AFTP version.");
        }

        MethodInfo? materialFactory = AccessTools.DeclaredMethod(effectType, MaterialFactoryName, Type.EmptyTypes);
        if (materialFactory == null || !materialFactory.IsStatic || materialFactory.ReturnType != typeof(CanvasItemMaterial))
        {
            throw new InvalidOperationException($"FireFlyEffect.{MaterialFactoryName}() not found or shape changed - unsupported AFTP version.");
        }

        FieldInfo? trailSprites = AccessTools.DeclaredField(effectType, TrailListFieldName);
        if (trailSprites == null || trailSprites.IsStatic || trailSprites.FieldType != typeof(List<Sprite2D>))
        {
            throw new InvalidOperationException($"FireFlyEffect.{TrailListFieldName} not found or shape changed - unsupported AFTP version.");
        }

        _acquireTrailSprite = AccessTools.Method(typeof(AftpFireFlyPerfCompat), nameof(AcquireTrailSprite))
            ?? throw new InvalidOperationException($"injected helper {nameof(AcquireTrailSprite)} not resolvable.");
        _releaseTrailSprite = AccessTools.Method(typeof(AftpFireFlyPerfCompat), nameof(ReleaseTrailSprite), [typeof(Sprite2D)])
            ?? throw new InvalidOperationException($"injected helper {nameof(ReleaseTrailSprite)} not resolvable.");

        // 模式校验:先读原体 IL 逐锚点确认,再谈装补丁.这里读的是原体(未被任何
        // transpiler 处理过),与实际运行时输入同源--AFTP 自身对 Update 无 transpiler.
        List<CodeInstruction> updateBody = PatchProcessor.GetOriginalInstructions(update);
        (int spriteCtor, int materialCall, int materialSet, int addChild, int queueFree) =
            ValidateUpdateBody(updateBody, trailSprites, materialFactory);

        // 共享材质覆盖范围校验:CreateAdditiveMaterial 在 Initialize 与 Update 里
        // 各只有一个调用点,且都直接喂给 CanvasItem::set_Material.多出或少掉调用点
        // 都意味着共享面变了 -> 终局,而不是把材质发给未审计的使用点.
        List<CodeInstruction> initializeBody = PatchProcessor.GetOriginalInstructions(initialize);
        int initializeMaterialSites = CountMaterialCallSites(initializeBody, materialFactory);
        if (initializeMaterialSites != 1)
        {
            throw new InvalidOperationException($"FireFlyEffect.{InitializeMethodName} has {initializeMaterialSites} {MaterialFactoryName} call site(s), expected exactly 1 - unsupported AFTP version.");
        }

        MethodInfo transpiler = AccessTools.Method(typeof(AftpFireFlyPerfCompat), nameof(TrailChurnTranspiler))
            ?? throw new InvalidOperationException($"transpiler {nameof(TrailChurnTranspiler)} not resolvable.");
        MethodInfo materialPrefix = AccessTools.Method(typeof(AftpFireFlyPerfCompat), nameof(AdditiveMaterialPrefix))
            ?? throw new InvalidOperationException($"prefix {nameof(AdditiveMaterialPrefix)} not resolvable.");

        try
        {
            harmony.Patch(update, transpiler: new HarmonyMethod(transpiler));
            harmony.Patch(materialFactory, prefix: new HarmonyMethod(materialPrefix));
        }
        catch
        {
            Rollback(harmony, update, materialFactory);
            throw;
        }

        MainFile.Logger.Info($"[Spire1] AFTP FireFly anchors validated: sprite-ctor #{spriteCtor}, material #{materialCall}->#{materialSet}, addchild #{addChild}, evict queuefree #{queueFree}, body {updateBody.Count} instructions.");
    }

    /// <summary>按本层 Harmony ID 撤销已装补丁(安装中途失败的收尾).撤销自身失败只记
    /// Error,不再抛出--终局原因保留最初的那个.</summary>
    private static void Rollback(Harmony harmony, MethodInfo update, MethodInfo materialFactory)
    {
        try
        {
            harmony.Unpatch(update, HarmonyPatchType.Transpiler, harmony.Id);
            harmony.Unpatch(materialFactory, HarmonyPatchType.Prefix, harmony.Id);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AFTP FireFly compat rollback failed ({e.GetType().Name}: {e.Message}) - a patch may be partially installed.");
        }
    }

    // ---- 模式校验(装补丁之前;失败即终局) ----

    /// <summary>
    /// 校验 <c>FireFlyEffect.Update</c> 原体与本层假设的指令形状一致,返回各锚点下标.
    /// 判据全部是类型导向的,不做计数式猜测:
    /// - Sprite2D 构造:<c>newobj</c> 且 operand 是 0 参数构造器且
    ///   <c>DeclaringType == typeof(Sprite2D)</c>.该体内另有两条 newobj(Vector2 与 Color),
    ///   它们的 DeclaringType 不同,故不可能被选中;再要求紧随其后的指令把结果存入
    ///   Sprite2D 局部变量,双重确认它就是拖尾精灵的构造.
    /// - 淘汰:<c>callvirt</c> 且 operand 是 <c>Godot.Node::QueueFree()</c>(0 参数),
    ///   且其前一条指令是对 <c>_trailSprites</c> 字段类型(<c>List&lt;Sprite2D&gt;</c>)的
    ///   <c>get_Item(int)</c> 调用 -- 即 <c>_trailSprites[0]</c> 的淘汰路径,而不是
    ///   方法体内任何别的 QueueFree.
    /// - AddChild 与 set_Material:各自恰好一条,用于确认构造块形状.
    /// - 材质工厂调用恰好一条且紧跟 <c>CanvasItem::set_Material</c>.
    /// 任何一项不符都抛异常,由调用方记终局;绝不产出半替换的方法体.
    /// </summary>
    private static (int SpriteCtor, int MaterialCall, int MaterialSet, int AddChild, int QueueFree) ValidateUpdateBody(
        List<CodeInstruction> codes, FieldInfo trailSprites, MethodInfo materialFactory)
    {
        List<int> spriteCtors = [];
        List<int> evictions = [];
        List<int> addChilds = [];
        List<int> materialSets = [];
        List<int> materialCalls = [];

        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction instruction = codes[i];
            if (IsTrailSpriteCtor(instruction))
            {
                spriteCtors.Add(i);
            }
            if (IsEvictionQueueFree(instruction))
            {
                evictions.Add(i);
            }
            if (IsNodeAddChild(instruction))
            {
                addChilds.Add(i);
            }
            if (IsCanvasItemSetMaterial(instruction))
            {
                materialSets.Add(i);
            }
            if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo called && called.Equals(materialFactory))
            {
                materialCalls.Add(i);
            }
        }

        if (spriteCtors.Count != 1)
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} has {spriteCtors.Count} Sprite2D constructor(s), expected exactly 1 - unsupported AFTP version.");
        }
        if (evictions.Count != 1)
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} has {evictions.Count} Node.QueueFree() call(s), expected exactly 1 - unsupported AFTP version.");
        }
        if (addChilds.Count != 1)
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} has {addChilds.Count} Node.AddChild call(s), expected exactly 1 - unsupported AFTP version.");
        }
        if (materialSets.Count != 1)
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} has {materialSets.Count} CanvasItem.set_Material call(s), expected exactly 1 - unsupported AFTP version.");
        }
        if (materialCalls.Count != 1)
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} has {materialCalls.Count} {MaterialFactoryName} call(s), expected exactly 1 - unsupported AFTP version.");
        }

        int spriteCtor = spriteCtors[0];
        if (spriteCtor + 1 >= codes.Count || !StoresToLocal(codes[spriteCtor + 1], typeof(Sprite2D)))
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} Sprite2D construction is not stored into a Sprite2D local - unsupported AFTP version.");
        }

        int queueFree = evictions[0];
        if (queueFree == 0 || !IsTrailListItemGetter(codes[queueFree - 1], trailSprites))
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} QueueFree is not the {TrailListFieldName}[0] eviction - unsupported AFTP version.");
        }

        int materialCall = materialCalls[0];
        if (materialCall + 1 != materialSets[0])
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} {MaterialFactoryName} call is not immediately followed by CanvasItem.set_Material - unsupported AFTP version.");
        }

        return (spriteCtor, materialCall, materialSets[0], addChilds[0], queueFree);
    }

    /// <summary>统计"工厂调用紧跟 set_Material"的调用点数(共享材质的实际覆盖面).</summary>
    private static int CountMaterialCallSites(List<CodeInstruction> codes, MethodInfo materialFactory)
    {
        int sites = 0;
        for (int i = 0; i + 1 < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo called && called.Equals(materialFactory)
                && IsCanvasItemSetMaterial(codes[i + 1]))
            {
                sites++;
            }
        }
        return sites;
    }

    private static bool IsTrailSpriteCtor(CodeInstruction instruction) =>
        instruction.opcode == OpCodes.Newobj
        && instruction.operand is ConstructorInfo ctor
        && ctor.DeclaringType == typeof(Sprite2D)
        && ctor.GetParameters().Length == 0;

    private static bool IsEvictionQueueFree(CodeInstruction instruction) =>
        instruction.opcode == OpCodes.Callvirt
        && instruction.operand is MethodInfo method
        && method.DeclaringType == typeof(Node)
        && method.Name == nameof(Node.QueueFree)
        && method.GetParameters().Length == 0;

    private static bool IsTrailListItemGetter(CodeInstruction instruction, FieldInfo trailSprites) =>
        instruction.opcode == OpCodes.Callvirt
        && instruction.operand is MethodInfo method
        && method.Name == "get_Item"
        && method.DeclaringType == trailSprites.FieldType
        && method.GetParameters().Length == 1
        && method.GetParameters()[0].ParameterType == typeof(int);

    private static bool IsNodeAddChild(CodeInstruction instruction) =>
        instruction.opcode == OpCodes.Call
        && instruction.operand is MethodInfo method
        && method.DeclaringType == typeof(Node)
        && method.Name == nameof(Node.AddChild)
        && method.GetParameters().Length == 3;

    private static bool IsCanvasItemSetMaterial(CodeInstruction instruction) =>
        instruction.opcode == OpCodes.Callvirt
        && instruction.operand is MethodInfo method
        && method.DeclaringType == typeof(CanvasItem)
        && method.Name == "set_Material"
        && method.GetParameters().Length == 1;

    /// <summary>
    /// 判断指令是否为"存入指定类型的局部变量".三种 operand 形态都兼容:stloc_0..3 无
    /// operand;stloc/stloc.s 的 operand 是 LocalBuilder(带 LocalType);个别读取器只
    /// 暴露整数下标.只有拿到 LocalBuilder 且类型不符时才否证 -- 类型取不到时不否证,
    /// 判别力由 <see cref="ValidateUpdateBody"/> 的主判据承担:被存入的指令本身就是
    /// <c>newobj Sprite2D::.ctor()</c>,其类型已由构造器 operand 确认.
    /// </summary>
    private static bool StoresToLocal(CodeInstruction instruction, Type localType)
    {
        if (instruction.opcode != OpCodes.Stloc
            && instruction.opcode != OpCodes.Stloc_S
            && instruction.opcode != OpCodes.Stloc_0
            && instruction.opcode != OpCodes.Stloc_1
            && instruction.opcode != OpCodes.Stloc_2
            && instruction.opcode != OpCodes.Stloc_3)
        {
            return false;
        }
        return instruction.operand is not LocalBuilder local || local.LocalType == localType;
    }

    // ---- 注入的两个方法(Transpiler 的替换目标;owner:本类) ----

    /// <summary>
    /// Harmony Transpiler:两处机械 1:1 替换(锚点见类注释).逐指令原地改写 opcode 与
    /// operand,不增删指令,labels/blocks 随指令对象保留,故分支目标与异常块边界不变;
    /// 每一条未被替换的指令原样通过.替换数不为 2 时抛异常 -&gt; Patch() 失败 -&gt; 终局
    /// 裁决,绝不产出半替换的方法体.
    /// </summary>
    private static IEnumerable<CodeInstruction> TrailChurnTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo acquire = _acquireTrailSprite
            ?? throw new InvalidOperationException($"{nameof(AcquireTrailSprite)} not bound.");
        MethodInfo release = _releaseTrailSprite
            ?? throw new InvalidOperationException($"{nameof(ReleaseTrailSprite)} not bound.");

        List<CodeInstruction> codes = [.. instructions];
        int substituted = 0;
        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction instruction = codes[i];
            if (IsTrailSpriteCtor(instruction))
            {
                instruction.opcode = OpCodes.Call; // newobj Sprite2D::.ctor -> call AcquireTrailSprite()
                instruction.operand = acquire;
                substituted++;
                continue;
            }
            if (IsEvictionQueueFree(instruction))
            {
                instruction.opcode = OpCodes.Call; // callvirt Node::QueueFree -> call ReleaseTrailSprite(Sprite2D)
                instruction.operand = release;
                substituted++;
            }
        }
        if (substituted != 2)
        {
            throw new InvalidOperationException($"FireFlyEffect.{UpdateMethodName} no longer matches the AFTP-2 anchors ({substituted}/2 substitutions) - refusing a partial rewrite.");
        }
        return codes;
    }

    /// <summary>
    /// Harmony Prefix,整体替换 <c>FireFlyEffect.CreateAdditiveMaterial</c> 原体
    /// (原体 = new CanvasItemMaterial + BlendMode = Add).返回共享实例并跳过原体,
    /// 消除每个拖尾样本与每个头精灵各一次的资源分配.共享实例创建后永不修改,故所有
    /// 使用点看到的行为与各自持有一份 Add 混合材质完全一致.创建失败时返回 true 走
    /// 原体(按调用分配,原语义),全进程只记一次 Error.
    /// </summary>
    private static bool AdditiveMaterialPrefix(ref CanvasItemMaterial __result)
    {
        CanvasItemMaterial? shared = _sharedAdditiveMaterial;
        if (shared != null && GodotObject.IsInstanceValid(shared))
        {
            __result = shared;
            return false;
        }

        try
        {
            shared = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            _sharedAdditiveMaterial = shared; // 先发布后返回:此引用之后只读
            __result = shared;
            return false;
        }
        catch (Exception e)
        {
            if (!_materialFallbackLogged)
            {
                _materialFallbackLogged = true;
                MainFile.Logger.Error($"[Spire1] AFTP FireFly shared material unavailable ({e.GetType().Name}: {e.Message}) - {MaterialFactoryName} falls back to AFTP's per-call allocation (original semantics).");
            }
            return true;
        }
    }

    /// <summary>
    /// 取一个拖尾精灵:池中有存活实例则 LIFO 取出,否则新建.契约:返回的实例必定无父
    /// 节点且未被排队删除,故调用方随后的 AddChild 必定成功;返回实例的属性由调用方在
    /// 构造块内全量重写,故复用不残留上一生命的状态.池中实例若已失效(引擎收尾等)或
    /// 已被外部排队删除,则丢弃并继续取下一个.
    /// </summary>
    private static Sprite2D AcquireTrailSprite()
    {
        while (IdleTrailSprites.Count > 0)
        {
            Sprite2D candidate = IdleTrailSprites.Pop();
            if (GodotObject.IsInstanceValid(candidate) && !candidate.IsQueuedForDeletion())
            {
                return candidate;
            }
        }
        return new Sprite2D();
    }

    /// <summary>
    /// 归还一个拖尾精灵(淘汰路径).父检查 RemoveChild 后入池,不销毁:调用当刻精灵仍是
    /// 特效节点的子节点(原实现 QueueFree 到帧末才真删),不摘除就再次 AddChild 会失败.
    /// 不入池的三种情形都按原语义 QueueFree 销毁,绝不留下游离节点:
    /// 摘除后仍带父(理论上不可能),或池已满(超出保留上限).实例已失效或已被排队删除
    /// 时不重复销毁(前者已无 native 对象,后者已在删除队列里).
    /// </summary>
    private static void ReleaseTrailSprite(Sprite2D? sprite)
    {
        if (sprite == null || !GodotObject.IsInstanceValid(sprite))
        {
            return; // 进树前或释放后已被销毁:无可归还之物(防御分支)
        }
        if (sprite.IsQueuedForDeletion())
        {
            return; // 已排队删除的实例不能复用,也无需再销毁
        }

        Node? parent = sprite.GetParent();
        if (parent != null)
        {
            parent.RemoveChild(sprite);
        }
        if (sprite.GetParent() != null)
        {
            sprite.QueueFree(); // 摘除未生效:不入池(不变量优先于复用率),照原语义销毁
            return;
        }

        if (IdleTrailSprites.Count >= TrailPoolCapacity)
        {
            if (!_saturationLogged)
            {
                _saturationLogged = true;
                MainFile.Logger.Info($"[Spire1] AFTP FireFly trail pool at its cap ({TrailPoolCapacity}) - surplus sprites are destroyed instead of pooled (retention stays bounded).");
            }
            sprite.QueueFree(); // 池满:保留量有硬上界,超出的照原语义销毁而非入池
            return;
        }
        IdleTrailSprites.Push(sprite);
    }
}
