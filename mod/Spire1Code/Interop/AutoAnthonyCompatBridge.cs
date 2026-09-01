using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Spire1.Spire1Code.Character;
#if SPIRE1_AUTOANTHONY
using AutoAnthony;
using AutoAnthony.Patches;
using ChaosCardGenerator;
#endif

namespace Spire1.Spire1Code.Interop;

#if SPIRE1_AUTOANTHONY
/// <summary>
/// AutoAnthony（创意工坊 3786611028，"Auto-Anthonyology"）桥接层：让它的每局随机卡池
/// 覆盖本 mod 的 StS1 角色（SPIRE1-IRONCLAD / SPIRE1-SILENT / SPIRE1-DEFECT）。
///
/// 缺口（2026-09-01 反编译实锤）：AutoAnthony 的激活链
/// <c>ChaosCharacterMapping.From(CharacterModel)</c> 用 <c>is Ironclad</c> 等引擎类型
/// 检查识别角色；本 mod 角色是 <c>PlaceholderCharacterModel</c> 子类，永远不被识别 →
/// <c>DeactivateRun()</c> 放行 → StS1 角色开局没有任何随机卡。
///
/// 桥接面（全部挂进 <see cref="Apply"/>，由 ModManager 加载完 AutoAnthony 后调用）：
/// 1. Postfix <c>ChaosCharacterMapping.From(CharacterModel)</c>（单人/多人激活共用）：
///    原返回 null 且入参是本 mod 角色时补 GeneratedCharacter 映射 → AutoAnthony 自己的
///    激活、快照、起手替换链对我们角色全量生效。原返回非 null（引擎角色）绝不干涉。
/// 2. Postfix <c>ChaosCharacterMapping.From(SerializableRun)/(RunHistory)</c>：按
///    CharacterId/ModelId 补映射 → 存档恢复与历史记录页同样生效。
/// 3. Prefix 本 mod 三角色的 <c>CardPool</c> getter：Chaos run 激活时返回对应
///    <c>ChaosXxxCardPool</c>（AutoAnthony 只 patch 了五个引擎角色类的 getter，够不到我们的）。
/// 4. Prefix 本 mod 三角色的 <c>StartingDeck</c> getter：<c>ReplaceStartingCards</c>
///    开启时返回 <c>ChaosCardRegistry.Canonical(character, slot)</c> 起手。
///
/// 多人：AutoAnthony 多人路径（SeedBeforeMultiplayerPatch）同样经
/// <c>From(player.character)</c>，对桥接透明；host 快照经 ChaosPoolSnapshotModifier 分发。
/// 双端都装这两个 mod 即可，MP 一致性是 AutoAnthony 自身契约（快照 authoritative、
/// 需重生成即抛），不属本层职责。
///
/// 版本耦合：本文件直接引用 AutoAnthony 公开 API（ChaosRunDefinitions / ChaosCardRegistry /
/// ChaosCharacterMapping 均为 public/internal——internal 经 Publicizer 不可用，只用 public 面）。
/// AutoAnthony 大版本更新若改这些 API，构建会当场失败（好事：强制重新审计），
/// 运行时缺席则 Apply 返回 false、全部补丁不挂、StS1 角色用原版池。
/// </summary>
internal static class AutoAnthonyCompatBridge
{
    private static bool _applied;

    // (2026-09-01 CodeQualityCritic blocker fix) 类型初始化器绝不能引用 AutoAnthony 类型:
    // beforefieldinit 下首次触碰本类任何静态字段即运行 cctor,若 Map/EntryMap 直接以
    // GeneratedCharacter 为值类型,会在 Apply 的程序集探测(以及一切更早的触碰)之前强制
    // 解析 AutoAnthony.dll —— 未装该 mod 或加载顺序靠后时抛 FileNotFoundException,
    // .NET 永久缓存 TypeInitializationException,整个 Spire1 initializer 被 ModManager
    // 标记失败(经离仓 CLR 双程序集复现实锤)。枚举常量在编译期折叠为 int,故以 int 为
    // 值不产生任何外部类型引用;使用点在方法体内再强转。
    private static readonly Dictionary<Type, int> Map = new()
    {
        [typeof(Ironclad)] = (int)GeneratedCharacter.Ironclad,
        [typeof(Silent)] = (int)GeneratedCharacter.Silent,
        [typeof(Defect)] = (int)GeneratedCharacter.Defect,
        // 本仓的 Watcher（观者）已归档且无 StS2 同名原型，不参与。
    };

    /// <summary>
    /// 第三方角色 → 映射（工坊 Boninall 观者 v0.9.24，用户 2026-09-01 裁定走无色池）。
    /// 类型/ID 经反射在 Apply 期解析（见 ThirdPartyEntries），避免编译期/加载期
    /// 硬依赖 Watcher mod；该 mod 缺席时条目静默不注册。
    /// CharacterId.Entry 为 "WATCHER"（纯 ModelDb 注册，无 BaseLib 前缀）。
    /// </summary>
    private static readonly Dictionary<Type, int> ThirdPartyMap = new();
    private static readonly Dictionary<string, int> ThirdPartyEntryMap = new(StringComparer.Ordinal);

    private const string WatcherModAssembly = "Watcher";
    private const string WatcherCharacterType = "WatcherMod.Watcher";
    private const string WatcherEntry = "WATCHER";

    internal static bool TryMap(Type spire1Character, out GeneratedCharacter generated)
    {
        if (Map.TryGetValue(spire1Character, out int value)
            || ThirdPartyMap.TryGetValue(spire1Character, out value))
        {
            generated = (GeneratedCharacter)value;
            return true;
        }
        generated = default;
        return false;
    }

    internal static bool TryMap(string characterIdEntry, out GeneratedCharacter generated)
    {
        if (EntryMap.TryGetValue(characterIdEntry, out int value)
            || ThirdPartyEntryMap.TryGetValue(characterIdEntry, out value))
        {
            generated = (GeneratedCharacter)value;
            return true;
        }
        generated = default;
        return false;
    }
    private static readonly Dictionary<string, int> EntryMap = new(StringComparer.Ordinal)
    {
        ["SPIRE1-IRONCLAD"] = (int)GeneratedCharacter.Ironclad,
        ["SPIRE1-SILENT"] = (int)GeneratedCharacter.Silent,
        ["SPIRE1-DEFECT"] = (int)GeneratedCharacter.Defect,
    };


    /// <summary>
    /// 挂全部桥接补丁。必须在 ModManager 加载完 AutoAnthony 之后调用（晚于其 initializer），
    /// 否则 patch 目标方法虽可解析（类型在引用程序集里），但 AutoAnthony 自己的 Harmony
    /// 补丁尚未挂上——先后顺序对本层无影响（我们 patch 的是它的静态方法本体，不与其补丁交互）。
    /// 返回 false = AutoAnthony 未加载，静默跳过。
    /// </summary>
    internal static bool Apply(Harmony harmony)
    {
        if (_applied)
        {
            return true;
        }

        // 探测：AutoAnthony 程序集必须已在 AppDomain（ModManager 装载）。
        bool present = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "AutoAnthony");
        if (!present)
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony absent — StS1 characters keep their normal card pools.");
            return false;
        }

        int patched = 0;
        patched += PatchFrom(harmony);
        patched += PatchPoolsAndDecks(harmony);
        patched += PatchThirdPartyEntries(harmony);
        _applied = patched > 0;
        MainFile.Logger.Info($"[Spire1] AutoAnthony bridge applied ({patched} patch groups).");
        return _applied;
    }

    /// <summary>
    /// 第三方角色注册：工坊观者（Boninall）→ 无色池。Watcher mod 缺席时静默跳过。
    ///
    /// 激活映射故意返回 Ironclad 而非 Colorless：AA 的 NormalizeCharacters 会剥掉
    /// Colorless（ChaosRunDefinitions.cs:1262），From→Colorless 会让激活链直接
    /// DeactivateRun()。伪 Ironclad 让激活/快照/MP 契约全通；观者的实际卡池由
    /// ThirdPartyPoolPrefix 单独指向 ColorlessCardPool——其内容被 AA 的
    /// ColorlessPoolContentsPatch 替换为 GetCards(Colorless) 的混沌卡
    /// （GetCards 按需 Build,Chaos run 激活时用 ActiveSeed——种子一致,MP 确定）。
    /// 起手保留观者原生 10 张（BasicCountFor(Colorless)=0,无伪造槽位）。
    /// </summary>
    private static int PatchThirdPartyEntries(Harmony harmony)
    {
        Assembly? watcherAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == WatcherModAssembly);
        if (watcherAssembly == null)
        {
            return 0; // 工坊观者未安装——不注册
        }

        Type? watcherType = watcherAssembly.GetType(WatcherCharacterType);
        if (watcherType == null)
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony bridge: Watcher mod present but WatcherMod.Watcher type not found — skipped.");
            return 0;
        }

        // 激活身份 = Ironclad（见方法注释）；池身份 = Colorless（ThirdPartyPoolPrefix）。
        ThirdPartyMap[watcherType] = (int)GeneratedCharacter.Ironclad;
        ThirdPartyEntryMap[WatcherEntry] = (int)GeneratedCharacter.Ironclad;

        int count = PatchGetter(harmony, watcherType, "CardPool",
            new HarmonyMethod(typeof(AutoAnthonyCompatBridge), nameof(ThirdPartyPoolPrefix)));
        if (count > 0)
        {
            MainFile.Logger.Info("[Spire1] AutoAnthony bridge: workshop Watcher -> Colorless generated pool (Ironclad activation carrier, native starting deck kept).");
        }
        return count;
    }

    private static bool ThirdPartyPoolPrefix(ref CardPoolModel __result)
    {
        if (!ChaosRunDefinitions.IsRunActive)
        {
            return true; // 非混沌局:观者原版紫色池
        }
        // ColorlessCardPool 的内容已被 AA 的 ColorlessPoolContentsPatch 在 AllCards 层
        // 替换为混沌卡;这里把池身份指过去(奖励/商店/PrismaticGem 枚举随之全通)。
        __result = ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.ColorlessCardPool>();
        return false;
    }

    // ---- 1+2. ChaosCharacterMapping.From 三个重载的 Postfix ----

    private static int PatchFrom(Harmony harmony)
    {
        int count = 0;
        Type? mappingType = Type.GetType("AutoAnthony.Patches.ChaosCharacterMapping, AutoAnthony");
        if (mappingType == null)
        {
            MainFile.Logger.Error("[Spire1] AutoAnthony bridge: ChaosCharacterMapping type not found — From overloads not patched.");
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

    // Postfix 签名与目标严格对齐（Harmony 要求 __result 与目标返回类型一致）。
    private static void FromCharacterPostfix(CharacterModel character, ref GeneratedCharacter? __result)
    {
        if (__result != null || character == null)
        {
            return; // AutoAnthony 已认出（引擎角色）或入参为空（原方法对 null 也返回 null）——不干涉
        }
        if (TryMap(character.GetType(), out GeneratedCharacter generated))
        {
            __result = generated;
            MainFile.Logger.Info($"[Spire1] AutoAnthony bridge: {character.GetType().Name} -> {generated} generated pool.");
        }
    }

    private static void FromSavePostfix(SerializableRun save, ref GeneratedCharacter[] __result)
    {
        List<GeneratedCharacter> extra = save.Players
            .Select(p => p.CharacterId?.Entry)
            .Where(e => e != null && TryMap(e, out _))
            .Select(e => (GeneratedCharacter)EntryMap[e!])
            .ToList();
        MergeInto(ref __result, extra);
    }

    private static void FromHistoryPostfix(RunHistory history, ref GeneratedCharacter[] __result)
    {
        List<GeneratedCharacter> extra = history.Players
            .Select(p => p.Character?.Entry)
            .Where(e => e != null && TryMap(e, out _))
            .Select(e => (GeneratedCharacter)EntryMap[e!])
            .ToList();
        MergeInto(ref __result, extra);
    }

    private static void MergeInto(ref GeneratedCharacter[] result, List<GeneratedCharacter> extra)
    {
        if (extra.Count == 0)
        {
            return;
        }
        result = result.Concat(extra).Distinct().OrderBy(c => c).ToArray();
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

    // Prefix 签名：CardPoolModel / IEnumerable<CardModel> 与 CharacterModel 对应 getter 的
    // 返回类型严格一致（public virtual，Spire1 角色以 override 属性重写）。

    private static bool IroncladPoolPrefix(ref CardPoolModel __result)
        => ReplacePool(typeof(Ironclad), GeneratedCharacter.Ironclad, ref __result);

    private static bool SilentPoolPrefix(ref CardPoolModel __result)
        => ReplacePool(typeof(Silent), GeneratedCharacter.Silent, ref __result);

    private static bool DefectPoolPrefix(ref CardPoolModel __result)
        => ReplacePool(typeof(Defect), GeneratedCharacter.Defect, ref __result);

    private static bool ReplacePool(Type spire1Character, GeneratedCharacter generated, ref CardPoolModel __result)
    {
        // 只查 IsRunActive，不查 IsCharacterRunActive——与 AutoAnthony 对引擎角色的
        // ReplacePool 语义对齐：池替换必须是全局的。全池枚举者（PrismaticGem、
        // ColorfulPhilosophers、UnlockState.CharacterCardPools）会把所有角色的池拉进来，
        // 若只替换本局角色的池，未游玩的 Spire1 角色会漏出原版一代卡（对照：引擎
        // 五角色的池在 Chaos run 中全部无条件替换）。起手替换保持 per-character
        // （见 ReplaceDeck），与它的 ReplaceStartingDeck 同构。
        if (!ChaosRunDefinitions.IsRunActive)
        {
            return true; // 原版池
        }

        CardPoolModel? pool = generated switch
        {
            GeneratedCharacter.Ironclad => ModelDb.CardPool<ChaosIroncladCardPool>(),
            GeneratedCharacter.Silent => ModelDb.CardPool<ChaosSilentCardPool>(),
            GeneratedCharacter.Defect => ModelDb.CardPool<ChaosDefectCardPool>(),
            _ => null,
        };
        if (pool == null)
        {
            return true;
        }
        __result = pool;
        return false;
    }

    private static bool IroncladDeckPrefix(ref IEnumerable<CardModel> __result)
        => ReplaceDeck(GeneratedCharacter.Ironclad, ref __result);

    private static bool SilentDeckPrefix(ref IEnumerable<CardModel> __result)
        => ReplaceDeck(GeneratedCharacter.Silent, ref __result);

    private static bool DefectDeckPrefix(ref IEnumerable<CardModel> __result)
        => ReplaceDeck(GeneratedCharacter.Defect, ref __result);

    private static bool ReplaceDeck(GeneratedCharacter generated, ref IEnumerable<CardModel> __result)
    {
        // 与 AutoAnthony 的 CharacterPoolPatchRouting.ReplaceStartingDeck 同构：
        // 仅当 Chaos run 激活、该角色在激活集、且 ReplaceStartingCards 开启时替换。
        if (!ChaosRunDefinitions.IsRunActive || !ChaosRunDefinitions.IsCharacterRunActive(generated)
            || !ChaosRunDefinitions.ActiveReplaceStartingCards)
        {
            return true; // 我方 StS1 起手牌组
        }

        int count = ChaosRunDefinitions.BasicCountFor(generated);
        CardModel[] deck = new CardModel[count];
        for (int slot = 0; slot < count; slot++)
        {
            deck[slot] = ChaosCardRegistry.Canonical(generated, slot);
        }
        __result = deck;
        return false;
    }
}
#else
/// <summary>
/// 编译机无 AutoAnthony 引用（.tmp/interop-refs 缺失）时的空壳：保持 MainFile 调用点
/// 无条件编译。构建环境缺 dll 不应改变源码，只应降级桥接能力。
/// </summary>
internal static class AutoAnthonyCompatBridge
{
    internal static bool Apply(Harmony _)
    {
        MainFile.Logger.Info("[Spire1] AutoAnthony interop not compiled in (reference dll absent) — bridge disabled.");
        return false;
    }
}
#endif
