using System.Threading;
using BaseLib.Config;

namespace Spire1.Spire1Code.Config;

/// <summary>
/// Runtime toggles for this mod, shown in Settings -> Mod Settings (auto-generated UI).
/// Event injection defaults OFF. Read at run / act / pool generation time to gate content, so a run can
/// use none of this mod's content while it stays installed (no uninstall needed).
/// </summary>
[ConfigHoverTipsByDefault]
internal class Spire1Config : SimpleModConfig
{
    // C14 r10 (2026-10-03): the master and cards switches are custom properties so every write
    // republishes one atomic combined Cards gate decision (master && cards && per-run latch).
    // Writers serialize on CardsGateSync and publish the new combined value BEFORE storing the
    // individual switch, so a reader can never observe a gate that ignores a switch that has
    // already been written. Readers (including the Spire1Card.Tags total fuse) take exactly one
    // atomic read instead of re-deriving the AND chain from three mutable sources at once.
    private static readonly object CardsGateSync = new();

    /// <summary>
    /// C14 r13 (2026-10-04): run-latch publish version. Bumped only by
    /// SetRunContentLatch/SetRunContentLatchFromLoad and by a lifecycle gate rewind, always under
    /// CardsGateSync. A lifecycle capture/restore pair uses it to rewind only the run-latch write
    /// it made itself while that write is still the latest one, so a failed load can never roll
    /// back a newer latch publish from another thread. A concurrent settings toggle does not bump
    /// it, so a rewind still picks up the newest switch values.
    /// </summary>
    private static long _cardsLatchVersion;

    /// <summary>
    /// C14 r11 (2026-10-03): subscribe the deck-grant guard while this type is initialized.
    /// Spire1Config is constructed from MainFile Phase 1, before ModelDb.Init, any run or any pool
    /// consumer; the guard itself resolves its representative lazily on first hook use. Failure is
    /// logged by the guard and never reported as protection.
    /// </summary>
    static Spire1Config()
    {
        Spire1.Spire1Code.Cards.Spire1DeckGrantGuard.EnsureSubscribed();
    }

    private static bool _enableSts1Content = true;
    private static bool _enableSts1Cards = true;

    /// <summary>Master switch. When false, all StS1 content is gated off.</summary>
    public static bool EnableSts1Content
    {
        get => Volatile.Read(ref _enableSts1Content);
        set
        {
            lock (CardsGateSync)
            {
                if (Volatile.Read(ref _enableSts1Content) == value)
                {
                    return;
                }
                PublishCardsGate(value, Volatile.Read(ref _enableSts1Cards), Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun);
                Volatile.Write(ref _enableSts1Content, value);
            }
        }
    }

    /// <summary>Show the StS1 characters ("StS1 - X") in character select.</summary>
    public static bool EnableSts1Characters { get; set; } = true;

    /// <summary>Inject StS1 colorless cards into the shared reward pool.</summary>
    public static bool EnableSts1Cards
    {
        get => Volatile.Read(ref _enableSts1Cards);
        set
        {
            lock (CardsGateSync)
            {
                if (Volatile.Read(ref _enableSts1Cards) == value)
                {
                    return;
                }
                PublishCardsGate(Volatile.Read(ref _enableSts1Content), value, Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun);
                Volatile.Write(ref _enableSts1Cards, value);
            }
        }
    }

    /// <summary>Inject StS1 relics into the shared reward pools.</summary>
    public static bool EnableSts1Relics { get; set; } = true;

    /// <summary>
    /// Allow StS1 custom power models (types under the Spire1 namespace) to be applied
    /// during a run. Default ON (matches the pre-toggle behavior). When OFF, this gate
    /// blocks StS1 custom powers and PowerCmd effects whose cardSource is a Spire1 card,
    /// including vanilla power effects triggered by Spire1 cards (for example
    /// StrengthPower or PoisonPower). Unrelated vanilla powers and effects from other
    /// mods' cards are not blocked. Read at runtime effect application entry points; see
    /// <see cref="Spire1ContentGroup"/> for the run snapshot.
    /// </summary>
    public static bool EnableSts1Powers { get; set; } = true;

    /// <summary>
    /// Inject StS1 potions into the shared reward pools. Default ON
    /// (matches the pre-toggle behavior). Read at pool/query time;
    /// see <see cref="Spire1ContentGroup"/> for the run snapshot.
    /// </summary>
    public static bool EnableSts1Potions { get; set; } = true;

    /// <summary>
    /// Inject StS1 events into the shared event pools (Act2 story events).
    /// Default OFF; explicit user values are preserved.
    /// When false, they are stripped at runtime from every act's event pool as
    /// rooms are generated / the next event is pulled, so they never roll in
    /// base-game runs (user request 2026-09-13: gen-1 events should be
    /// toggleable out of gen-2 dungeons). This is a runtime filter, not a
    /// startup removal; see Sts1EventToggleFilterPatch.
    /// </summary>
    public static bool EnableSts1Events { get; set; } = false;

    /// <summary>
    /// 是否把本 mod 的一代奖励内容(卡牌/遗物/事件/药水/自定义力量的池注入与运行期应用)登记进"下一局"新对局.默认开.
    /// <para>
    /// 与上面几个即时开关不同,本项在<b>新对局创建时被快照锁存进该局存档</b>(经
    /// <see cref="Spire1.Spire1Code.Run.Spire1ContentSnapshotModifier"/>,复用引擎
    /// SerializableRun.Modifiers 的 [SavedProperty] 通道)。因此:
    /// 改本设置只影响之后新开的对局,不动进行中的存档;加载任意存档时以该存档创建时
    /// 锁存的值运行,而非当前全局值。缺席快照的旧存档(本功能上线前创建)按 true 处理,
    /// 保持既有可见行为不变。角色可见性属选人期(建局之前)决策,不受本项影响。
    /// </para>
    /// </summary>
    public static bool RegisterContentNextRun { get; set; } = true;

    // 2026-09-10: 一代地牢(4 幕自建内容)已按用户指令整体移除;EnableSts1Dungeon/
    // UseSts1Dungeon 配置项与幕选择器一并删除.
    // 2026-10-02 (C01): the matching eng/zhs settings_ui keys were dead strings with no
    // consumer; they are deleted here so the localization surface only lists live switches.
    // The enum slots Monsters/Encounters/Acts/Scenes remain reserved for a future wave.

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
    /// SP1-1 (2026-09-15) pool-materialization diagnostics: OPT-IN, default OFF.
    /// When true, the ordered pool census (Diagnostics/PoolCensus.cs) runs ONCE at the
    /// first main menu entry after this setting is on. It must never run at mod-initializer
    /// time: reading a pool's AllCards freezes that pool (ModHelper.ConcatModelsFromMods)
    /// and would lock any later-loaded mod out of ModHelper.AddModelToPool for it. Menu
    /// entry is post-registration by construction (every mod initializer has finished).
    /// Toggling it ON mid-session fires on the NEXT main menu entry (e.g. after leaving a run).
    /// The "poolcensus" console command runs the same report on demand without this flag.
    /// </summary>
    public static bool PoolCensusOnMenuEnter { get; set; } = false;

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
    // Semantics (single source of truth for every content agent):
    //   * characters: select-time decision, deliberately NOT bound to the per-run snapshot.
    //   * cards/relics/events/powers/potions: require the master switch, the per-group switch,
    //     and the per-run snapshot latched by Run.Spire1RunContent at run creation / save load.
    //   * monsters/encounters/acts/scenes: no such content exists in the current product tree
    //     (see DEVELOP.md); the enum reserves the slots so a future wave cannot invent a
    //     second gate API.
    // All of these are read-only computed properties: they are not serialized and never
    // mutate a deterministic pool mid-run.
    [ConfigIgnore] public static bool CharactersEnabled => EnableSts1Content && EnableSts1Characters;
    [ConfigIgnore] public static bool CardsEnabled => !CardsGateClosedThisRun;

    /// <summary>
    /// C14 r10 (2026-10-03): single atomic Cards decision. Writers publish this value BEFORE
    /// storing the switch/latch they just changed, so a reader that takes one snapshot cannot
    /// observe a torn master/cards/run AND chain and cannot see a value that ignores an
    /// already-published close. The Spire1Card.Tags total fuse reads this at the engine point
    /// of use; no Harmony prefix-local boolean is part of the protection.
    /// </summary>
    private static bool _cardsGateClosedThisRun =
        !(_enableSts1Content && _enableSts1Cards && Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun);

    /// <summary>
    /// C14 r11 (2026-10-03): the single Cards decision. When a lifecycle operation scope is
    /// active (Spire1CardsGateSnapshot), every read inside that operation returns the one
    /// decision captured at its entry; otherwise the live atomic value is returned.
    /// </summary>
    [ConfigIgnore] public static bool CardsGateClosedThisRun =>
        Spire1.Spire1Code.Run.Spire1CardsGateSnapshot.Closed;

    /// <summary>
    /// C14 r11: the live (non-snapshot) atomic value, for writers that must publish a fresh
    /// combined decision without re-entering the operation snapshot. Never used by readers.
    /// </summary>
    [ConfigIgnore] internal static bool LiveCardsGateClosed => Volatile.Read(ref _cardsGateClosedThisRun);

    /// <summary>
    /// C14 r10: every Spire1RunContent latch/restore/reset routes through here. The combined gate
    /// value is published before the per-run field is stored, so a concurrent engine read either
    /// sees the previous consistent decision or the new one.
    /// </summary>
    internal static void SetRunContentLatch(bool value)
    {
        lock (CardsGateSync)
        {
            PublishCardsGate(
                Volatile.Read(ref _enableSts1Content),
                Volatile.Read(ref _enableSts1Cards),
                value);
            _cardsLatchVersion++;
            Spire1.Spire1Code.Run.Spire1RunContent.WriteContentActiveThisRun(
                value, _cardsLatchVersion, ownedByActiveCapture: false);
        }
    }

    /// <summary>
    /// C14 r13: load-path latch used by RunState.FromSerializable. Identical to
    /// SetRunContentLatch, but the combined gate and fallback writes are attributed to the
    /// active lifecycle capture, so a failed load (or the canonicalize wrapper) can rewind
    /// exactly this temporary write while it is still the latest publish.
    /// </summary>
    internal static void SetRunContentLatchFromLoad(bool value)
    {
        lock (CardsGateSync)
        {
            PublishCardsGate(
                Volatile.Read(ref _enableSts1Content),
                Volatile.Read(ref _enableSts1Cards),
                value);
            _cardsLatchVersion++;
            Spire1.Spire1Code.Run.Spire1RunContent.WriteContentActiveThisRun(
                value, _cardsLatchVersion, ownedByActiveCapture: true);
        }
    }

    /// <summary>
    /// C14 r13: read the current run-latch publish version for a lifecycle capture/restore pair.
    /// Read under CardsGateSync so it is consistent with any concurrent latch publish; the
    /// fallback carrier is written by latch publishers inside the same lock.
    /// </summary>
    internal static long CaptureRunLatchVersion()
    {
        lock (CardsGateSync)
        {
            return _cardsLatchVersion;
        }
    }

    /// <summary>
    /// C14 r13: rewind the combined Cards gate to the captured process-wide latch. The rewind
    /// only happens while the captured latch is still the latest published one (nothing
    /// published since the capture, or the latest publish is this operation's own write); a
    /// newer latch publish from another thread wins and is left untouched. Current switch values
    /// are read fresh, so a concurrent settings toggle is preserved, and the gate is recomputed
    /// as !(master &amp;&amp; cards &amp;&amp; capturedRunLatch) instead of restoring a stale raw
    /// value. This keeps the Cards gate consistent with the other gates, which read the restored
    /// Spire1RunContent latch directly.
    /// </summary>
    internal static void RestoreCardsGateFromRunLatch(
        bool capturedRunLatch, long capturedVersion, long ownVersion)
    {
        lock (CardsGateSync)
        {
            long current = _cardsLatchVersion;
            if (current != capturedVersion && current != ownVersion)
            {
                return;
            }
            bool closed = !(Volatile.Read(ref _enableSts1Content)
                && Volatile.Read(ref _enableSts1Cards)
                && capturedRunLatch);
            if (Volatile.Read(ref _cardsGateClosedThisRun) == closed)
            {
                return;
            }
            Volatile.Write(ref _cardsGateClosedThisRun, closed);
            _cardsLatchVersion++;
        }
    }

    /// <summary>
    /// C14 r10: the single atomic publish point. Writers hold CardsGateSync; readers only take
    /// one Volatile.Read of the published value.
    /// </summary>
    private static void PublishCardsGate(bool master, bool cards, bool runActive) =>
        Volatile.Write(ref _cardsGateClosedThisRun, !(master && cards && runActive));

    [ConfigIgnore] public static bool RelicsEnabled => EnableSts1Content && EnableSts1Relics && Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun;
    [ConfigIgnore] public static bool EventsEnabled => EnableSts1Content && EnableSts1Events && Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun;
    [ConfigIgnore] public static bool PowersEnabled => EnableSts1Content && EnableSts1Powers && Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun;
    [ConfigIgnore] public static bool PotionsEnabled => EnableSts1Content && EnableSts1Potions && Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun;
    [ConfigIgnore] public static bool LocDebug => EnableSts1Content && DebugShowLocKeys;

    /// <summary>
    /// Stable content-group identifiers for the single gate API. Consumers (character pools,
    /// card/relic/potion/event/power filters, run-snapshot code) must call
    /// <see cref="Spire1Config.IsEnabled(Spire1ContentGroup)"/> instead of re-deriving the AND chain.
    /// The enum is deliberately not a config property: it is not serialized and does not
    /// appear in the settings UI.
    /// </summary>
    public enum Spire1ContentGroup
    {
        Characters,
        Cards,
        Relics,
        Powers,
        Potions,
        Events,
        Monsters,
        Encounters,
        Acts,
        Scenes,
    }

    /// <summary>
    /// Single gate API for every content agent. Fail-closed by construction: an unknown
    /// group returns false rather than silently enabling content.
    /// <para>
    /// Monsters/Encounters/Acts/Scenes have no registered product content today; they
    /// return false so a future implementation cannot accidentally treat them as on.
    /// </para>
    /// </summary>
    public static bool IsEnabled(Spire1ContentGroup group) => group switch
    {
        Spire1ContentGroup.Characters => CharactersEnabled,
        Spire1ContentGroup.Cards => CardsEnabled,
        Spire1ContentGroup.Relics => RelicsEnabled,
        Spire1ContentGroup.Powers => PowersEnabled,
        Spire1ContentGroup.Potions => PotionsEnabled,
        Spire1ContentGroup.Events => EventsEnabled,
        _ => false,
    };
}
