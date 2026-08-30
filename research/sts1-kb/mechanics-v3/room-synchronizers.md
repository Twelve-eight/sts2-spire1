# StS2 多人同步机制卷五——房间级同步器族谱（火堆/宝箱/商店/地图投票/杂项）

> 生成：2026-08-29（同批拆解，接卷四）。来源：sts2.dll v0.111.0 反编译源逐类精读。
> 卷四回答"事件/奖励为什么能不同"；本卷回答"**哪些房间/系统有自己的专用同步器**、各自的合法分歧面在哪、死等点在哪"。
> 已知死等三姐妹：卷四 WaitForSync（无超时）、本卷 AfterAllRestSitesCompleted（无超时）、宝箱投票齐票（无超时）。【2026-08-30 订正: 前两者的解除=断线兜底(RestSite OnPeerDisconnected L144-162/CombatState 同款);但 TreasureRoomRelicSynchronizer **无 OnPeerDisconnected 处理器**——宝箱投票在 peer 断线后无既定释放路径,断线能否解除取决于 ActionQueue 层的善后,引擎源内未见面级兜底。三姐妹"共同解除条件只有断线"对宝箱一员不成立,排查时勿假设断线必解锁宝箱房。】

---

## 1. 同步器族谱总表（MegaCrit.Sts2.Core.Multiplayer.Game/ 为主,另含 GameActions.Multiplayer/ 的 ActionQueueSynchronizer 与父目录的 CombatStateSynchronizer;目录内另有 RewardSynchronizer/EventCombatSynchronizer/ActChangeSynchronizer 未列详行）

| 同步器 | 管什么 | 分歧面（各端独立） | 一致面（消息驱动） | 死等点 |
|---|---|---|---|---|
| EventSynchronizer | 事件选项（卷四详） | 非共享事件选项内容/事件 RNG | OptionIndex/投票消息 | —（异常即抛） |
| RewardsSetSynchronizer | 奖励集（卷四详） | 奖励内容 roll | Selected/Skipped 消息 | set 缺失→无限缓冲 |
| RestSiteSynchronizer | 火堆选项 | 选项列表生成（RestSiteOption.Generate 各端自跑） | OptionIndexChosen(type=RestSite)/Hovered/Skipped | AfterAllRestSitesCompleted await 每人 completionTaskSource（L249-255）——断线才补发（OnPeerDisconnected L144-162） |
| TreasureRoomRelicSynchronizer | 宝箱房遗物二选一 | 无（生成用共享 _rng+RelicGrabBag） | 投票+ActionQueue 广播 | 齐票等待 |
| MapSelectionSynchronizer | 地图节点投票 | — | VoteForMapCoord（host 齐票择 coord） | 齐票等待 |
| CombatStateSynchronizer | 房间过渡 combat sync | — | SyncPlayerData/SyncRng（host 下发 Rng 快照） | WaitForSync（卷四 V4-R6） |
| ActionQueueSynchronizer | 游戏动作队列 | — | 每动作双端排队执行 | — |
| ReactionSynchronizer | 表情反应轮(光标处 ReactionMessage,如 EndTurnPing 同类的手感消息) | 动画表现 | ReactionMessage | —(45 行小类) |
| FlavorSynchronizer | 手感类杂项消息(EndTurnPing/MapPing) | 表现层 | Ping 消息 | — |
| OneOffSynchronizer | 一次性场景同步(商店删卡/开箱金币/水晶球奖励) | — | 各自消息 | —(约 232 行) |
| ChecksumTracker | 对拍与断线执法 | — | ChecksumData/StateDivergence | —（mismatch→DisconnectClient） |

**结构规律**：凡"选项/内容生成"都可各端独立（走 per-player RNG 或共享但已同步的 RNG）；凡"选择/推进"必走消息。**mod 的安全侵入点=内容生成钩子（Hook 层），危险侵入点=消息处理与齐票逻辑**。

## 2. 火堆（RestSiteSynchronizer + RestSiteOption.Generate）

- **选项生成各端自跑**（BeginRestSite L90-103 对每个 Player 调 `RestSiteOption.Generate(player)`）：基础=治疗+锻造；**MP 加 MendRestSiteOption（L68-71 `Players.Count > 1` 条件——人数是双端一致的 run 状态，安全）**；再走 `Hook.ModifyRestSiteOptions`（L72——**mod 加火堆选项的官方入口**，Girya 举铁若实现就该挂这里）。
- **选择同步**：`OptionIndexChosenMessage(type=RestSite)` → 对端在**该玩家**的选项列表上执行同索引（ChooseOption L177-220）；索引越界抛异常（L185-188）——与非共享事件同款 R1 风险：**Hook.ModifyRestSiteOptions 若单端加/删选项=索引错位**。
- **成功语义**：OnSelect 返回 false（如锻造无可升卡）**不算选择完成**、不消耗——对端同样重放 OnSelect，失败必须双端同因，否则一端在等一个永不来的下一条消息。
- **死等点**：`AfterAllRestSitesCompleted`（L249-255）await 全员 completionTaskSource；**断线是唯一兜底**（L144-162 注释原文："otherwise a room exit … would hang forever"）——引擎自己承认这类死等。卷四观察哨模式同样适用。

> **规则 V5-R1（火堆 mod）**：加/删/重排火堆选项的 Hook 必须双端同 dll 且条件只依赖 run 状态（人数/act/遗物持有——遗物本身经同步）。AFTP BloodBank（AfterRestSiteHeal 遗物钩子，数值性）安全；任何"本地配置多一个火堆选项"都是索引炸弹。

## 3. 宝箱房（TreasureRoomRelicSynchronizer）

- **生成一致**：BeginRelicPicking（L89-132）用**共享 `_rng`**（构造注入的 run 级 Rng）+ **RelicGrabBag 共享抓包**（RunState 级，CombatStateSynchronizer 每次 sync 时 host 下发快照强制对齐，卷四 §5）→ 双端 roll 出**同一组**遗物。**宝箱遗物双端一致是引擎强保证，不是巧合**。
- **人各一票**（PickRelicLocally→投票消息），齐票后 host 用 ActionQueue 广播发放。
- `Hook.ShouldGenerateTreasure`（L105）决定**谁**有宝箱——人数/epoch 类 run 状态，双端一致。
- **伪多人**（IsSingleplayerOrFakeMultiplayer L114-125）：AI 队友投票由本地 _rng 代打——**FakeMultiplayer 是单机模式，mod 测试时别拿它当 MP 证据**。

> **规则 V5-R2（遗物发放）**：mod 给遗物奖励走 RelicGrabBag/RelicCmd.Obtain（进共享抓包=双端同步）；绝不在单端直接 `ModelDb.Relic<T>().ToMutable()` 塞玩家背包——绕过抓包=对端不知道这个遗物被抽走了，后续宝箱 roll 分歧。

## 4. RelicGrabBag（共享遗物抓包）——所有遗物奖励的单点

- 构造：按稀有度分桶，**每桶 UnstableShuffle(rng)**（run 级 Rng，双端同种子同洗牌）。
- **PullFromFront**（大多数奖励源）/ **PullFromBack**（商店专用）——**商店从桶尾、奖励从桶头，互不抢位**（引擎用方向差异保证商店和奖励不重复出同一件）。
- 抓包状态序列化进 run 存档；MP 每次房间过渡 host 下发快照（CombatStateSynchronizer._sharedRelicGrabBag，卷四 §5 表格 Run 层）。
- **mod 注册新遗物自动进桶**（按 Rarity 归桶）——池注入在桶构造前完成即可（冻结时机见 kb/engine-facts）。

> **规则 V5-R3**：商店 mod 改货架必须走 PullFromBack 同款抓包路径；`GetAvailableDeque` 内含"该遗物对当前玩家是否允许"过滤（runState）——mod 遗物的 CanSpawn 若依赖单端状态（本地 cfg），抓包两端拉出不同遗物=宝箱/奖励分歧。

## 5. MapSelectionSynchronizer（地图投票）

- 每步投票（VoteForMapCoordAction 经 ActionQueueSynchronizer 入队,NMapScreen.cs L947-948；原记"VoteForMapCoordMessage"名称有误,2026-08-30 订正）；host 齐票选坐标（同 EventSynchronizer 的 host-only rng 模式）→ MoveToMapCoordAction 入 ActionQueue 广播。
- **地图本身双端各生成**（卷四家族D：GenerateRooms 对称执行+同种子）——投票只是推进，不传地图。**地图分歧的检测点=进房时的 room 类型/内容对拍**（checksum 与 combat sync），不是投票环节。

## 6. OneOffSynchronizer / ReactionSynchronizer / FlavorSynchronizer（速记,2026-08-30 审阅订正）

- OneOff(约 232 行): 跨端执行一次性场景动作 - 商店删卡(MerchantCardRemoval)/开箱金币(TreasureChestOpened)/水晶球奖励(CrystalSphereRewards). 类注释自称"装不进其它同步器的一次性场景杂物箱". mod 做一次性副作用若绕过它,双端可重复执行(原"互斥"定性不准,实为跨端执行).
- Reaction: ReactionMessage - 光标处表情/反应轮(NReactionContainer.DoRemoteReaction),非遗物动画. 遗物 flash 若有同步走别的通道(未定位,存疑).
- Flavor: EndTurnPingMessage/MapPingMessage - 手感类杂项消息. **原卷称"遗物 flavor 文本独立 roll"是错的**: 遗物风味是 RelicModel.Flavor 纯 LocString 查表,无 roll 无同步问题. 玩家可见动态文本若含随机数,来源是遗物 DynamicVars(确定性),非本类.

> 2026-08-30 增量审阅(increment-review-20260830 F3)订正: 本节原三条功能描述中 Reaction/Flavor 两条与引擎源不符,OneOff 定性不准,已按反编译源重写. VoteForMapCoordMessage 名称亦有误 - 实为 VoteForMapCoordAction 经 ActionQueueSynchronizer 入队(NMapScreen.cs L947-948),非直接消息.

## 7. 新增预防规则（并入卷四 checklist 用）

- V5-R1 火堆选项 Hook：双端同 dll+run 状态条件（防索引错位）。
- V5-R2 遗物发放走共享抓包（防宝箱分歧）。
- V5-R3 mod 遗物 CanSpawn 不得依赖单端状态。
- V5-R4 一次性副作用走 OneOffSynchronizer（防双端重复执行）。
- V5-R5 FakeMultiplayer 不是 MP 证据（伪多人本地代打投票）。

## 附：证据锚点

- RestSiteSynchronizer.cs：L90-103（各端 Generate）、L105-117（RestSite 型 OptionIndexChosen）、L144-162（断线兜底注释）、L177-220（ChooseOption 索引/成功语义）、L249-255（AfterAllRestSitesCompleted 死等）
- RestSiteOption.cs：L53-74（Generate：Mend 的 MP 条件、Hook.ModifyRestSiteOptions 官方入口）
- TreasureRoomRelicSynchronizer.cs：L89-132（共享 rng+抓包生成、FakeMultiplayer L114-125）
- RelicGrabBag.cs：L100-113（分桶洗牌）、L116-147（Front=奖励/Back=商店）
- Hook.cs L2323-2325（ShouldGenerateTreasure 钩子）
