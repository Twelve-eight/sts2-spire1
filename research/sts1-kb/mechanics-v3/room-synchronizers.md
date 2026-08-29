# StS2 多人同步机制卷五——房间级同步器族谱（火堆/宝箱/商店/地图投票/杂项）

> 生成：2026-08-29（同批拆解，接卷四）。来源：sts2.dll v0.111.0 反编译源逐类精读。
> 卷四回答"事件/奖励为什么能不同"；本卷回答"**哪些房间/系统有自己的专用同步器**、各自的合法分歧面在哪、死等点在哪"。
> 已知死等三姐妹：卷四 WaitForSync（无超时）、本卷 AfterAllRestSitesCompleted（无超时）、宝箱投票齐票（无超时）——**共同解除条件只有 peer 断线**。

---

## 1. 同步器族谱总表（MegaCrit.Sts2.Core.Multiplayer.Game/ 全目录）

| 同步器 | 管什么 | 分歧面（各端独立） | 一致面（消息驱动） | 死等点 |
|---|---|---|---|---|
| EventSynchronizer | 事件选项（卷四详） | 非共享事件选项内容/事件 RNG | OptionIndex/投票消息 | —（异常即抛） |
| RewardsSetSynchronizer | 奖励集（卷四详） | 奖励内容 roll | Selected/Skipped 消息 | set 缺失→无限缓冲 |
| RestSiteSynchronizer | 火堆选项 | 选项列表生成（RestSiteOption.Generate 各端自跑） | OptionIndexChosen(type=RestSite)/Hovered/Skipped | AfterAllRestSitesCompleted await 每人 completionTaskSource（L249-255）——断线才补发（OnPeerDisconnected L144-162） |
| TreasureRoomRelicSynchronizer | 宝箱房遗物二选一 | 无（生成用共享 _rng+RelicGrabBag） | 投票+ActionQueue 广播 | 齐票等待 |
| MapSelectionSynchronizer | 地图节点投票 | — | VoteForMapCoord（host 齐票择 coord） | 齐票等待 |
| CombatStateSynchronizer | 房间过渡 combat sync | — | SyncPlayerData/SyncRng（host 下发 Rng 快照） | WaitForSync（卷四 V4-R6） |
| ActionQueueSynchronizer | 游戏动作队列 | — | 每动作双端排队执行 | — |
| ReactionSynchronizer | 遗物触发动画标记 | 动画表现 | — | —（45 行小类） |
| FlavorSynchronizer | 遗物 flavor 文本生成 | flavor roll（纯表现层，注释明示） | — | — |
| OneOffSynchronizer | 一次性动作去重 | — | — | —（210 行，联机锁/一次性事件） |
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

- 每步投票（VoteForMapCoordMessage）；host 齐票选坐标（同 EventSynchronizer 的 host-only rng 模式）→ MoveToMapCoordAction 入 ActionQueue 广播。
- **地图本身双端各生成**（卷四家族D：GenerateRooms 对称执行+同种子）——投票只是推进，不传地图。**地图分歧的检测点=进房时的 room 类型/内容对拍**（checksum 与 combat sync），不是投票环节。

## 6. OneOffSynchronizer / ReactionSynchronizer / FlavorSynchronizer（速记）

- OneOff（210 行）：联机"一次性动作"互斥（如双端同时开同一宝箱）——mod 若绕过它做一次性副作用，双端可重复执行。
- Reaction：遗物触发时的**动画标记同步**（远端也播放 flash）——表现层，mod 遗物 Flash() 自动同步。
- Flavor：遗物 flavor 文本（如 "聚宝盆里装着 37 金币"）各端独立 roll——**引擎明示纯表现层可分歧**，与卷四"内容必须一致"边界互补：**玩家可见文本≠状态**。

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
