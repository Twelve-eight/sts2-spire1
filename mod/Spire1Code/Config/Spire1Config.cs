using BaseLib.Config;

namespace Spire1.Spire1Code.Config;

/// <summary>
/// Runtime toggles for this mod, shown in Settings -> Mod Settings (auto-generated UI).
/// All default ON. Read at run / act / pool generation time to gate content, so a run can
/// use none of this mod's content while it stays installed (no uninstall needed).
/// </summary>
[ConfigHoverTipsByDefault]
internal class Spire1Config : SimpleModConfig
{
    /// <summary>Master switch. When false, all StS1 content is gated off.</summary>
    public static bool EnableSts1Content { get; set; } = true;

    /// <summary>Show the StS1 characters ("StS1 - X") in character select.</summary>
    public static bool EnableSts1Characters { get; set; } = true;

    /// <summary>Inject StS1 colorless cards into the shared reward pool.</summary>
    public static bool EnableSts1Cards { get; set; } = true;

    /// <summary>Inject StS1 relics into the shared reward pools.</summary>
    public static bool EnableSts1Relics { get; set; } = true;

    // 2026-09-10: 一代地牢(4 幕自建内容)已按用户指令整体移除;EnableSts1Dungeon/
    // UseSts1Dungeon 配置项与幕选择器一并删除.

    /// <summary>
    /// 纯一代池模式：自定义角色池不注入任何二代官方卡（SharedCardReuse 复用项全部跳过），
    /// 改以我们自己的 StS1 忠实实现类填充（Ironclad/Silent 各 +10，Defect +ConserveBattery）。
    /// 默认关。开启时 RewardClampPatch 将奖励类抽牌数量钳制到池内实际可行数，
    /// 避免 ROOM_FULL_OF_CHEESE 等"要求 N 张不重复"的事件在小池上抛异常。
    /// </summary>
    public static bool PureSts1Pools { get; set; } = false;

    /// <summary>
    /// Debug mode: append the localization key to every mod string shown in-game,
    /// e.g. "打击 (SPIRE1-STRIKE_SILENT.title)". Makes console spawning and testing easier.
    /// </summary>
    public static bool DebugShowLocKeys { get; set; } = false;

    /// <summary>
    /// 联机容错（清单级）：握手时忽略双方 mod 清单差异强制放行。
    /// 今晚实测（divergence zip #563/#249）清单差异几乎全是"本地目录 vs 工坊来源"
    /// 假阳性；玩法安全仍依赖相同玩法 mod 二进制（哈希级另见下方开关）。
    /// 仅当双方都装了含此补丁的构建时才完整生效。
    /// </summary>
    public static bool IgnoreMpModDifferences { get; set; } = true;

    /// <summary>
    /// 联机容错（哈希级，R14 2026-09-06）：游戏版本相同但 ModelID 哈希不符时
    /// 放行。哈希 = 玩法内容二进制指纹，清单一致但哈希不符意味着至少一侧的
    /// 玩法 mod 二进制漂移（版本更新不同步/本地补丁），Serialization 安全无保证。
    /// 因此与清单级放行分离、默认关--确有跨版本联机需求时手动开。
    /// </summary>
    public static bool IgnoreMpHashMismatch { get; set; } = false;

    /// <summary>
    /// 地图页显示"跳过当前节点"救援按钮：卡死在火堆等房间时打开地图，
    /// 点按钮解锁选点后直接点下一个节点（走原生投票管线，无失同步风险）。
    /// </summary>
    public static bool EnableSkipNodeButton { get; set; } = true;

    // --- gate helpers (computed; getter-only, not surfaced as settings) ---
    [ConfigIgnore] public static bool CharactersEnabled => EnableSts1Content && EnableSts1Characters;
    [ConfigIgnore] public static bool CardsEnabled => EnableSts1Content && EnableSts1Cards;
    [ConfigIgnore] public static bool RelicsEnabled => EnableSts1Content && EnableSts1Relics;
    [ConfigIgnore] public static bool LocDebug => EnableSts1Content && DebugShowLocKeys;
}
