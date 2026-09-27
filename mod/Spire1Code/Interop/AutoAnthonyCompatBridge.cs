using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// AutoAnthony（创意工坊 3786611028，"Auto-Anthonyology"）桥接层：让它的每局随机卡池
/// 覆盖本 mod 的 StS1 角色（SPIRE1-IRONCLAD / SPIRE1-SILENT / SPIRE1-DEFECT）。
///
/// 缺口（2026-09-01 反编译实锤）：AutoAnthony 的激活链
/// <c>ChaosCharacterMapping.From(CharacterModel)</c> 用 <c>is Ironclad</c> 等引擎类型
/// 检查识别角色；本 mod 角色是 <c>PlaceholderCharacterModel</c> 子类，永远不被识别 ->
/// <c>DeactivateRun()</c> 放行 -> StS1 角色开局没有任何随机卡。
///
/// 桥接面（全部挂进 <see cref="Apply"/>，由 ModManager 加载完 AutoAnthony 后调用）：
/// 1. Postfix <c>ChaosCharacterMapping.From(CharacterModel)</c>（单人/多人激活共用）：
///    原返回 null 且入参是本 mod 角色时补 GeneratedCharacter 映射 -> AutoAnthony 自己的
///    激活、快照、起手替换链对我们角色全量生效。原返回非 null（引擎角色）绝不干涉。
/// 2. Postfix <c>ChaosCharacterMapping.From(SerializableRun)/(RunHistory)</c>：按
///    CharacterId/ModelId 补映射 -> 存档恢复与历史记录页同样生效。
/// 3. Prefix 本 mod 三角色的 <c>CardPool</c> getter：Chaos run 激活时返回对应
///    <c>ChaosXxxCardPool</c>（AutoAnthony 只 patch 了五个引擎角色类的 getter，够不到我们的）。
/// 4. Prefix 本 mod 三角色的 <c>StartingDeck</c> getter：<c>ReplaceStartingCards</c>
///    开启时返回 <c>ChaosCardRegistry.Canonical(character, slot)</c> 起手。
///
/// (2026-09-27 纯反射化) 本层过去经条件编译符号 SPIRE1_AUTOANTHONY 强类型引用
/// AutoAnthony / ChaosCardGenerator 的公开类型（GeneratedCharacter 枚举、ChaosXxxCardPool、
/// ChaosRunDefinitions、ChaosCardRegistry）。那会在 Spire1.dll 的 CLI 元数据 AssemblyRef
/// 表里写入对 AutoAnthony 的编译期硬引用，令未装 AutoAnthony 的用户在桥接方法被 JIT 时
/// 抛 TypeLoad/FileNotFound——与 Spire1.json 只声明 BaseLib 依赖的设计意图相反。
/// 现改为单一实现、永远编译，对 AutoAnthony 的一切引用走反射：类型经 <see cref="AaType"/>
/// 从已加载程序集解析，枚举值以 int 存储、用点再 <c>Enum.ToObject</c> 还原，方法/属性经
/// MethodInfo/PropertyInfo 调用。AutoAnthony 缺席时 Apply 返回 false、补丁不挂、StS1 角色
/// 用原版池，且 Spire1.dll 不再引用 AutoAnthony 程序集。
///
/// 多人：AutoAnthony 多人路径（SeedBeforeMultiplayerPatch）同样经
/// <c>From(player.character)</c>，对桥接透明；host 快照经 ChaosPoolSnapshotModifier 分发。
/// </summary>
internal static class AutoAnthonyCompatBridge
{
    private static bool _applied;

    // GeneratedCharacter 枚举的 int 常量（2026-09-27 ilspycmd 反编译实锤枚举定义顺序：
    // Ironclad=0, Silent=1, Defect=2, Necrobinder=3, Regent=4, Colorless=5）。
    // 以 int 常量存储是 cctor 安全的关键：类型初始化器绝不能触碰 AutoAnthony 类型，否则
    // beforefieldinit 下首次访问本类任何静态字段就会强制解析 AutoAnthony.dll，未装该 mod
    // 时抛 FileNotFoundException 并被 .NET 永久缓存为 TypeInitializationException，整个
    // Spire1 initializer 被 ModManager 标记失败。Apply 期用 VerifyEnumConstants 做名对齐
    // 校验（不抛，仅日志），防 AutoAnthony 改枚举顺序时静默错位。
    private const int GcIronclad = 0;
    private const int GcSilent = 1;
    private const int GcDefect = 2;
    private const int GcColorless = 5;

    private static readonly Dictionary<Type, int> Map = new()
    {
        [typeof(Ironclad)] = GcIronclad,
        [typeof(Silent)] = GcSilent,
        [typeof(Defect)] = GcDefect,
        // 本仓的 Watcher（观者）已归档且无 StS2 同名原型，不参与。
    };

    private static readonly Dictionary<string, int> EntryMap = new(StringComparer.Ordinal)
    {
        ["SPIRE1-IRONCLAD"] = GcIronclad,
        ["SPIRE1-SILENT"] = GcSilent,
        ["SPIRE1-DEFECT"] = GcDefect,
    };

    /// <summary>
    /// 第三方角色 -> 映射（工坊 Boninall 观者 v0.9.24，用户 2026-09-01 裁定走无色池）。
    /// 类型/ID 经反射在 Apply 期解析（见 ThirdPartyEntries），避免编译期/加载期
    /// 硬依赖 Watcher mod；该 mod 缺席时条目静默不注册。
    /// CharacterId.Entry 为 "WATCHER"（纯 ModelDb 注册，无 BaseLib 前缀）。
    /// </summary>
    private static readonly Dictionary<Type, int> ThirdPartyMap = new();
    private static readonly Dictionary<string, int> ThirdPartyEntryMap = new(StringComparer.Ordinal);

    private const string WatcherModAssembly = "Watcher";
    private const string WatcherCharacterType = "WatcherMod.Watcher";
    private const string WatcherPoolType = "WatcherMod.WatcherCardPool";
    private const string WatcherEntry = "WATCHER";

    // ---- AutoAnthony 反射解析缓存（Apply 期一次性解析；补丁体只读） ----

    private static Assembly? _aaAssembly;
    private static Type? _generatedCharacterType;
    private static MethodInfo? _isRunActiveGetter;
    private static MethodInfo? _isCharacterRunActive;
    private static MethodInfo? _activeReplaceStartingGetter;
    private static MethodInfo? _activePreserveOriginalGetter;
    private static MethodInfo? _basicCountFor;
    private static MethodInfo? _originalCardsForPreservedPool;
    private static MethodInfo? _canonicalCharacterSlot;
    private static MethodInfo? _colorlessTypesGetter;
    private static MethodInfo? _modelDbCardPoolGeneric;
    private static bool _reflectionReady;

    /// <summary>工坊观者池的 canonical 类型（Apply 期记住），实例惰性解析（见
    /// <see cref="ResolveThirdPartyPoolInstance"/>）。R2 守卫用：WatcherCardPool 未重声明
    /// AllCards/AllCardIds，我们的补丁经继承链落在基类 getter 上（声明域=全部卡池），
    /// 必须用实例比对把作用域收回观者池本身。</summary>
    private static Type? _thirdPartyPoolType;
    private static CardPoolModel? _thirdPartyPoolInstance;
    private static bool _thirdPartyPoolResolved;

    private static Assembly? AaAssembly
        => _aaAssembly ??= AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "AutoAnthony");

    private static Type? AaType(string fullName) => AaAssembly?.GetType(fullName);

    private static Type GeneratedCharacterType
        => _generatedCharacterType ??= AaType("ChaosCardGenerator.GeneratedCharacter")
           ?? throw new InvalidOperationException("ChaosCardGenerator.GeneratedCharacter not resolvable");

    private static object ToGenerated(int value) => Enum.ToObject(GeneratedCharacterType, value);

    // ---- 反射基元（补丁体调用；假定 ResolveReflection 已成功） ----

    private static bool IsRunActive()
        => _isRunActiveGetter != null && (bool)_isRunActiveGetter.Invoke(null, null)!;

    private static bool IsCharacterRunActive(int generated)
        => _isCharacterRunActive != null
           && (bool)_isCharacterRunActive.Invoke(null, new[] { ToGenerated(generated) })!;

    private static bool ActiveReplaceStartingCards()
        => _activeReplaceStartingGetter != null && (bool)_activeReplaceStartingGetter.Invoke(null, null)!;

    private static bool ActivePreserveOriginalCards()
        => _activePreserveOriginalGetter != null && (bool)_activePreserveOriginalGetter.Invoke(null, null)!;

    private static int BasicCountFor(int generated)
        => _basicCountFor != null ? (int)_basicCountFor.Invoke(null, new[] { ToGenerated(generated) })! : 0;

    private static CardModel Canonical(int generated, int slot)
        => (CardModel)_canonicalCharacterSlot!.Invoke(null, new object[] { ToGenerated(generated), slot })!;

    private static IEnumerable<Type> ColorlessTypes()
    {
        object? raw = _colorlessTypesGetter?.Invoke(null, null);
        return raw is IEnumerable<Type> types ? types : Enumerable.Empty<Type>();
    }

    private static IEnumerable<CardModel>? OriginalCardsForPreservedPool(int generated)
        => _originalCardsForPreservedPool?.Invoke(null, new[] { ToGenerated(generated) })
            as IEnumerable<CardModel>;

    /// <summary>反射调用 ModelDb.CardPool&lt;T&gt;()（泛型方法，MakeGenericMethod）。</summary>
    private static CardPoolModel? ChaosPoolByTypeName(string chaosPoolTypeName)
    {
        Type? poolType = AaType(chaosPoolTypeName);
        if (poolType == null || _modelDbCardPoolGeneric == null)
        {
            return null;
        }
        return _modelDbCardPoolGeneric.MakeGenericMethod(poolType).Invoke(null, null) as CardPoolModel;
    }

    private static CardPoolModel? ChaosPoolFor(int generated) => generated switch
    {
        GcIronclad => ChaosPoolByTypeName("AutoAnthony.ChaosIroncladCardPool"),
        GcSilent => ChaosPoolByTypeName("AutoAnthony.ChaosSilentCardPool"),
        GcDefect => ChaosPoolByTypeName("AutoAnthony.ChaosDefectCardPool"),
        _ => null,
    };

    internal static bool TryMap(Type spire1Character, out int generated)
    {
        if (Map.TryGetValue(spire1Character, out generated)
            || ThirdPartyMap.TryGetValue(spire1Character, out generated))
        {
            return true;
        }
        generated = default;
        return false;
    }

    internal static bool TryMap(string characterIdEntry, out int generated)
    {
        if (EntryMap.TryGetValue(characterIdEntry, out generated)
            || ThirdPartyEntryMap.TryGetValue(characterIdEntry, out generated))
        {
            return true;
        }
        generated = default;
        return false;
    }

    /// <summary>
    /// 挂全部桥接补丁。必须在 ModManager 加载完 AutoAnthony 之后调用（晚于其 initializer）。
    /// 返回 false = AutoAnthony 未加载或反射解析失败，静默跳过。
    /// </summary>
    internal static bool Apply(Harmony harmony)
    {
        if (_applied)
        {
            return true;
        }

        // 探测：AutoAnthony 程序集必须已在 AppDomain（ModManager 装载）。
        if (AaAssembly == null)
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony absent - StS1 characters keep their normal card pools.");
            return false;
        }

        if (!ResolveReflection())
        {
            MainFile.Logger.Error("[Spire1] AutoAnthony bridge: reflection resolution failed - bridge disabled (no patches applied).");
            return false;
        }

        int patched = 0;
        // SP1-1 (2026-09-15) capability-level failure boundary: each patch group runs in
        // its own try/catch so one group throwing cannot abort the others - and cannot leave
        // _applied=false after some groups already patched, which would let the
        // AutoAnthonyLoadHook retry re-apply the successful groups as duplicate Harmony patches.
        patched += ApplyPatchGroup("From overloads", () => PatchFrom(harmony));
        patched += ApplyPatchGroup("pool/deck getters", () => PatchPoolsAndDecks(harmony));
        patched += ApplyPatchGroup("third-party entries", () => PatchThirdPartyEntries(harmony));
        _applied = patched > 0;
        MainFile.Logger.Info($"[Spire1] AutoAnthony bridge applied ({patched} patch groups).");
        return _applied;
    }

    /// <summary>Apply 期一次性解析 AutoAnthony 的类型与成员到静态缓存；任一必需成员缺失
    /// 返回 false（Apply 据此整体放弃并禁用桥接，绝不半挂）。</summary>
    private static bool ResolveReflection()
    {
        if (_reflectionReady)
        {
            return true;
        }
        try
        {
            _ = GeneratedCharacterType; // 解析枚举类型（失败即抛）
            VerifyEnumConstants();

            Type? crd = AaType("AutoAnthony.ChaosRunDefinitions");
            Type? registry = AaType("AutoAnthony.ChaosCardRegistry");
            if (crd == null || registry == null)
            {
                MainFile.Logger.Error("[Spire1] AutoAnthony bridge: ChaosRunDefinitions/ChaosCardRegistry not resolvable.");
                return false;
            }

            _isRunActiveGetter = AccessTools.PropertyGetter(crd, "IsRunActive");
            _activeReplaceStartingGetter = AccessTools.PropertyGetter(crd, "ActiveReplaceStartingCards");
            _activePreserveOriginalGetter = AccessTools.PropertyGetter(crd, "ActivePreserveOriginalCards");
            _isCharacterRunActive = AccessTools.Method(crd, "IsCharacterRunActive", new[] { GeneratedCharacterType });
            _basicCountFor = AccessTools.Method(crd, "BasicCountFor", new[] { GeneratedCharacterType });
            _originalCardsForPreservedPool = AccessTools.Method(crd, "OriginalCardsForPreservedPool", new[] { GeneratedCharacterType });
            _colorlessTypesGetter = AccessTools.PropertyGetter(registry, "ColorlessTypes");
            _canonicalCharacterSlot = AccessTools.Method(registry, "Canonical", new[] { GeneratedCharacterType, typeof(int) });
            _modelDbCardPoolGeneric = AccessTools.Method(typeof(ModelDb), "CardPool");

            if (_isRunActiveGetter == null || _activeReplaceStartingGetter == null
                || _activePreserveOriginalGetter == null || _isCharacterRunActive == null
                || _basicCountFor == null || _colorlessTypesGetter == null
                || _canonicalCharacterSlot == null || _modelDbCardPoolGeneric == null)
            {
                MainFile.Logger.Error("[Spire1] AutoAnthony bridge: one or more AutoAnthony members not resolvable - bridge disabled.");
                return false;
            }
            // _originalCardsForPreservedPool 为可选（PreserveOriginalCards 附加用），缺失只降级不禁用。

            _reflectionReady = true;
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: reflection resolution threw: {e.Message}");
            return false;
        }
    }

    /// <summary>校验硬编码枚举 int 常量与 AutoAnthony 当前枚举定义名对齐；不抛，仅在错位时
    /// 记 Error（防 AutoAnthony 大版本改枚举顺序时静默把 Ironclad 当成别的角色）。</summary>
    private static void VerifyEnumConstants()
    {
        void Check(int value, string expected)
        {
            string? actual = Enum.GetName(GeneratedCharacterType, value);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: GeneratedCharacter constant drift - value {value} expected '{expected}' but AutoAnthony defines '{actual}'. Mapping may be wrong.");
            }
        }
        Check(GcIronclad, "Ironclad");
        Check(GcSilent, "Silent");
        Check(GcDefect, "Defect");
        Check(GcColorless, "Colorless");
    }

    /// <summary>Runs one interop patch group in isolation: a thrown group is logged and
    /// counted as 0, the remaining groups still apply.</summary>
    private static int ApplyPatchGroup(string name, Func<int> group)
    {
        try
        {
            return group();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: patch group '{name}' failed: {e.Message} (other groups unaffected)");
            return 0;
        }
    }

    /// <summary>
    /// 第三方角色注册:工坊观者(Boninall).观者 mod 缺席时静默跳过.
    ///
    /// 2026-09-10 让位:AutoAnthony 核心 v0.3.7x 起提供官方扩展 API,同作者的工坊扩展
    /// "Auto-Anthonyology: Watcher"(AutoAnthonyWatcher, 3794876718)用它注册了完整的观者
    /// 生成体系.本桥的观者部分在该扩展在场时冗余且冲突,检测到该程序集时本方法返回 0,
    /// 观者完全交给扩展;StS1 自有角色(SPIRE1-*)的桥接不受影响.
    ///
    /// 扩展缺席时的旧行为保留:激活映射故意返回 Ironclad 而非 Colorless(AA 的
    /// NormalizeCharacters 会剥掉 Colorless);观者的实际卡池由 ThirdPartyPoolPrefix 指向
    /// ColorlessCardPool(其内容被 AA 的 ColorlessPoolContentsPatch 替换为混沌卡);起手保留
    /// 观者原生 10 张(BasicCountFor(Colorless)=0,无伪造槽位).
    ///
    /// (2026-09-27 时序修复) 过去这里在 AssemblyLoad 早期即 ModelDb.GetById 解析观者池实例,
    /// 而此刻 ModelDb 尚未注册该池 -> GetById 抛 ModelNotFoundException
    /// (Model id=CARD_POOL.WATCHER_CARD_POOL not found).现只记住 poolType,实例改由三个补丁
    /// 守卫首次真正需要时经 ResolveThirdPartyPoolInstance 惰性 GetByIdOrNull 解析并缓存.
    /// </summary>
    private static int PatchThirdPartyEntries(Harmony harmony)
    {
        if (AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "AutoAnthonyWatcher"))
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony bridge: AutoAnthonyWatcher addon present - Watcher handed over to the official extension API, no third-party bridging.");
            return 0;
        }

        Assembly? watcherAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == WatcherModAssembly);
        if (watcherAssembly == null)
        {
            return 0; // 工坊观者未安装--不注册
        }

        Type? watcherType = watcherAssembly.GetType(WatcherCharacterType);
        if (watcherType == null)
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony bridge: Watcher mod present but WatcherMod.Watcher type not found - skipped.");
            return 0;
        }

        // 激活身份 = Ironclad（见方法注释）；池身份 = Colorless（ThirdPartyPoolPrefix）。
        ThirdPartyMap[watcherType] = GcIronclad;
        ThirdPartyEntryMap[WatcherEntry] = GcIronclad;

        int count = PatchGetter(harmony, watcherType, "CardPool",
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(ThirdPartyPoolPrefix)));

        // 潘多拉魔盒类转换补丁修复：混沌局把 WatcherCardPool.AllCards 也换成混沌无色内容；
        // AllCardIds 用并集保住原生卡的池身份解析（CardModel.Pool 经 AllCardIds 反查）。
        // R2（2026-09-06 审阅）：WatcherCardPool 未重声明这两个属性，PatchGetter 经继承链把
        // 补丁钉在基类 CardPoolModel 的 getter 上（声明域=全部卡池），守卫靠实例比对收回作用域。
        // (2026-09-27) 池实例改惰性解析，这里只记住 poolType。
        _thirdPartyPoolType = watcherAssembly.GetType(WatcherPoolType);
        if (_thirdPartyPoolType != null)
        {
            count += PatchGetter(harmony, _thirdPartyPoolType, "AllCards",
                new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(ThirdPartyPoolContentsPrefix)));
            count += PatchGetter(harmony, _thirdPartyPoolType, "AllCardIds",
                new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(ThirdPartyPoolIdsPostfix)));
        }
        if (count > 0)
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony bridge: workshop Watcher -> Colorless generated pool (Ironclad activation carrier, native starting deck kept, watcher pool contents chaos-swapped).");
        }
        return count;
    }

    /// <summary>惰性解析工坊观者池实例：首次在补丁守卫里用到时才 ModelDb.GetByIdOrNull
    /// （GetId 是纯函数永不抛；GetByIdOrNull 池未注册返回 null）。解析到非空才缓存并停止
    /// 重试，因此 AssemblyLoad 早期（池未注册）返回 null、守卫放行，池注册后自动解析成功。</summary>
    private static CardPoolModel? ResolveThirdPartyPoolInstance()
    {
        if (_thirdPartyPoolResolved || _thirdPartyPoolType == null)
        {
            return _thirdPartyPoolInstance;
        }
        try
        {
            CardPoolModel? pool = ModelDb.GetByIdOrNull<CardPoolModel>(ModelDb.GetId(_thirdPartyPoolType));
            if (pool != null)
            {
                _thirdPartyPoolInstance = pool;
                _thirdPartyPoolResolved = true;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: third-party pool instance resolution failed: {e.Message} (guard passes through).");
        }
        return _thirdPartyPoolInstance;
    }

    private static bool ThirdPartyPoolPrefix(ref CardPoolModel __result)
    {
        if (!IsRunActive())
        {
            return true; // 非混沌局:观者原版紫色池
        }
        // ColorlessCardPool 的内容已被 AA 的 ColorlessPoolContentsPatch 在 AllCards 层
        // 替换为混沌卡;这里把池身份指过去(奖励/商店/PrismaticGem 枚举随之全通)。
        CardPoolModel? colorless = ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.ColorlessCardPool>();
        if (colorless == null)
        {
            return true;
        }
        __result = colorless;
        return false;
    }

    /// <summary>WatcherCardPool.AllCards 前缀：混沌局返回混沌无色内容。
    /// R2 守卫（2026-09-06 审阅）：基类 getter 全局解析，只对观者池实例生效。</summary>
    private static bool ThirdPartyPoolContentsPrefix(CardPoolModel __instance, ref IEnumerable<CardModel> __result)
    {
        if (!ReferenceEquals(__instance, ResolveThirdPartyPoolInstance()))
        {
            return true; // R2：基类 getter 全局解析下的其他池--不干涉
        }
        if (!IsRunActive())
        {
            return true; // 非混沌局:观者原版 83 张
        }
        IEnumerable<CardModel> chaosCards = ColorlessTypes()
            .Select(t => ModelDb.GetById<CardModel>(ModelDb.GetId(t)));
        if (ActivePreserveOriginalCards())
        {
            IEnumerable<CardModel>? original = OriginalCardsForPreservedPool(GcColorless);
            if (original != null)
            {
                chaosCards = chaosCards.Concat(original);
            }
        }
        __result = chaosCards.ToArray();
        return false;
    }

    /// <summary>WatcherCardPool.AllCardIds 后缀：并上混沌无色卡 ID。
    /// R2 守卫同上：基类 getter 全局解析，只对观者池实例生效。</summary>
    private static void ThirdPartyPoolIdsPostfix(CardPoolModel __instance, ref IEnumerable<ModelId> __result)
    {
        if (!ReferenceEquals(__instance, ResolveThirdPartyPoolInstance()))
        {
            return; // R2：其他池--不干涉
        }
        if (!IsRunActive())
        {
            return;
        }
        List<ModelId> merged = new(__result);
        foreach (Type chaosType in ColorlessTypes())
        {
            merged.Add(ModelDb.GetId(chaosType));
        }
        __result = merged.Distinct().ToList();
    }

    // ---- 1+2. ChaosCharacterMapping.From 三个重载的 Postfix ----

    private static int PatchFrom(Harmony harmony)
    {
        int count = 0;
        Type? mappingType = AaType("AutoAnthony.Patches.ChaosCharacterMapping");
        if (mappingType == null)
        {
            MainFile.Logger.Error("[Spire1] AutoAnthony bridge: ChaosCharacterMapping type not found - From overloads not patched.");
            return 0;
        }
        foreach (MethodInfo from in mappingType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                     .Where(m => m.Name == "From"))
        {
            ParameterInfo[] ps = from.GetParameters();
            HarmonyMethod? postfix = ps.Length switch
            {
                1 when ps[0].ParameterType == typeof(CharacterModel) => PostfixFromCharacter(),
                1 when ps[0].ParameterType == typeof(SerializableRun) => PostfixFromSave(),
                1 when ps[0].ParameterType == typeof(RunHistory) => PostfixFromHistory(),
                _ => null,
            };
            if (postfix == null)
            {
                continue;
            }
            try
            {
                harmony.Patch(from, postfix: postfix);
                count++;
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: patch From({ps[0].ParameterType.Name}) failed: {e.Message}");
            }
        }
        return count;
    }

    internal static HarmonyMethod PostfixFromCharacter()
        => new(typeof(AutoAnthonyCompatBridge), nameof(FromCharacterPostfix));

    internal static HarmonyMethod PostfixFromSave()
        => new(typeof(AutoAnthonyCompatBridge), nameof(FromSavePostfix));

    internal static HarmonyMethod PostfixFromHistory()
        => new(typeof(AutoAnthonyCompatBridge), nameof(FromHistoryPostfix));

    // (2026-09-27 纯反射) Postfix 不能再写出 GeneratedCharacter?/GeneratedCharacter[] 的
    // 强类型 __result 签名。改用 Harmony 2.4.2 支持的弱类型 ref object __result：
    // - 目标返回 Nullable<GeneratedCharacter>（值类型）：Harmony 生成 Box[returnType] 入参、
    //   写回 Unbox_Any(returnType)，装箱/拆箱往返无损（EmitCallParameter/MethodCreator 反编译实锤）。
    //   AA 返回 null -> boxed 为 null 引用；我们写入 boxed GeneratedCharacter -> Unbox_Any 到
    //   Nullable HasValue=true；我们不写(留 null) -> 空 Nullable。
    // - 目标返回 GeneratedCharacter[]（引用类型）：object 可赋值，写回真正的 GeneratedCharacter[]
    //   （Array.CreateInstance(枚举类型,n) 构造），引用存储合法。

    private static void FromCharacterPostfix(CharacterModel character, ref object? __result)
    {
        if (__result != null || character == null)
        {
            return; // AutoAnthony 已认出（引擎角色，boxed 非空）或入参为空--不干涉
        }
        if (TryMap(character.GetType(), out int generated))
        {
            __result = ToGenerated(generated); // boxed GeneratedCharacter -> Unbox_Any 到 Nullable<GeneratedCharacter>
            MainFile.Logger.Info($"[Spire1] AutoAnthony bridge: {character.GetType().Name} -> generated pool #{generated}.");
        }
    }

    private static void FromSavePostfix(SerializableRun save, ref object __result)
    {
        // R3（2026-09-06 审阅）：用 TryMap 的 out 值，映射来源与其一致（"WATCHER" 只在
        // ThirdPartyEntryMap，回查单一字典会 KeyNotFound）。
        List<int> extra = new();
        foreach (string? entry in save.Players.Select(p => p.CharacterId?.Entry))
        {
            if (entry != null && TryMap(entry, out int generated))
            {
                extra.Add(generated);
            }
        }
        MergeInto(ref __result, extra);
    }

    private static void FromHistoryPostfix(RunHistory history, ref object __result)
    {
        List<int> extra = new();
        foreach (string? entry in history.Players.Select(p => p.Character?.Entry))
        {
            if (entry != null && TryMap(entry, out int generated))
            {
                extra.Add(generated);
            }
        }
        MergeInto(ref __result, extra);
    }

    /// <summary>合并混沌角色数组（弱类型）：读现有 GeneratedCharacter[]（boxed 元素转 int），
    /// 并入 extra，去重、按 int 升序（与原 OrderBy(c=>c) 同序，枚举底层即 int），
    /// 用 Array.CreateInstance(枚举类型,n) 构造回真正的 GeneratedCharacter[]。</summary>
    private static void MergeInto(ref object result, List<int> extra)
    {
        if (extra.Count == 0)
        {
            return;
        }
        List<int> values = new();
        if (result is Array existing)
        {
            foreach (object? item in existing)
            {
                if (item != null)
                {
                    values.Add(Convert.ToInt32(item));
                }
            }
        }
        values.AddRange(extra);
        int[] ordered = values.Distinct().OrderBy(v => v).ToArray();
        Array merged = Array.CreateInstance(GeneratedCharacterType, ordered.Length);
        for (int i = 0; i < ordered.Length; i++)
        {
            merged.SetValue(ToGenerated(ordered[i]), i);
        }
        result = merged;
    }

    // ---- 3+4. 本 mod 角色 CardPool / StartingDeck getter 的 Prefix ----

    private static int PatchPoolsAndDecks(Harmony harmony)
    {
        int count = 0;
        count += PatchGetter(harmony, typeof(Ironclad), nameof(Ironclad.CardPool),
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(IroncladPoolPrefix)));
        count += PatchGetter(harmony, typeof(Silent), nameof(Silent.CardPool),
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(SilentPoolPrefix)));
        count += PatchGetter(harmony, typeof(Defect), nameof(Defect.CardPool),
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(DefectPoolPrefix)));
        count += PatchGetter(harmony, typeof(Ironclad), nameof(Ironclad.StartingDeck),
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(IroncladDeckPrefix)));
        count += PatchGetter(harmony, typeof(Silent), nameof(Silent.StartingDeck),
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(SilentDeckPrefix)));
        count += PatchGetter(harmony, typeof(Defect), nameof(Defect.StartingDeck),
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(DefectDeckPrefix)));
        return count;
    }

    private static int PatchGetter(Harmony harmony, Type type, string propertyName, HarmonyMethod prefix)
    {
        try
        {
            MethodInfo? getter = AccessTools.PropertyGetter(type, propertyName);
            if (getter == null)
            {
                MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: {type.Name}.{propertyName} getter not found.");
                return 0;
            }
            harmony.Patch(getter, prefix: prefix);
            return 1;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AutoAnthony bridge: patch {type.Name}.{propertyName} failed: {e.Message}");
            return 0;
        }
    }

    private static bool IroncladPoolPrefix(ref CardPoolModel __result)
        => ReplacePool(GcIronclad, ref __result);

    private static bool SilentPoolPrefix(ref CardPoolModel __result)
        => ReplacePool(GcSilent, ref __result);

    private static bool DefectPoolPrefix(ref CardPoolModel __result)
        => ReplacePool(GcDefect, ref __result);

    private static bool ReplacePool(int generated, ref CardPoolModel __result)
    {
        // 只查 IsRunActive，不查 IsCharacterRunActive--与 AutoAnthony 对引擎角色的
        // ReplacePool 语义对齐：池替换必须是全局的。起手替换保持 per-character（见 ReplaceDeck）。
        if (!IsRunActive())
        {
            return true; // 原版池
        }
        CardPoolModel? pool = ChaosPoolFor(generated);
        if (pool == null)
        {
            return true;
        }
        __result = pool;
        return false;
    }

    private static bool IroncladDeckPrefix(ref IEnumerable<CardModel> __result)
        => ReplaceDeck(GcIronclad, ref __result);

    private static bool SilentDeckPrefix(ref IEnumerable<CardModel> __result)
        => ReplaceDeck(GcSilent, ref __result);

    private static bool DefectDeckPrefix(ref IEnumerable<CardModel> __result)
        => ReplaceDeck(GcDefect, ref __result);

    private static bool ReplaceDeck(int generated, ref IEnumerable<CardModel> __result)
    {
        // 与 AutoAnthony 的 CharacterPoolPatchRouting.ReplaceStartingDeck 同构：
        // 仅当 Chaos run 激活、该角色在激活集、且 ReplaceStartingCards 开启时替换。
        if (!IsRunActive() || !IsCharacterRunActive(generated) || !ActiveReplaceStartingCards())
        {
            return true; // 我方 StS1 起手牌组
        }
        int count = BasicCountFor(generated);
        CardModel[] deck = new CardModel[count];
        for (int slot = 0; slot < count; slot++)
        {
            deck[slot] = Canonical(generated, slot);
        }
        __result = deck;
        return false;
    }
}
