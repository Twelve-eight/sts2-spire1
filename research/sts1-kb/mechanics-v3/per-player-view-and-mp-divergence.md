# StS2 多人独立视图机制卷（per-player-view）——引擎拆解卷四

> 生成：2026-08-29。来源：sts2.dll v0.111.0 反编译源（research/engine-dllsrc/）逐类精读。
> 动机：2026-08-28 联机黑屏（家族C）暴露了"每个玩家可以合法看到不同选项"这一**游戏机制**与 mod 侵入之间的系统性风险。本卷回答：引擎哪里**故意**允许双端不一致、哪里**要求**双端一致、mod 从哪里越界。
> 阅读原则：**"合法不一致"由引擎白名单机制承载；绕过白名单自造的不一致=死等/黑屏。**

---

## 1. 引擎的两种事件模型（一切的根）

**每个玩家一份 EventModel 实例**。`EventSynchronizer.BeginEvent`（EventSynchronizer.cs L98-126）对每个 Player 调 `canonicalEvent.ToMutable()` 克隆一份，`BeginEvent(player, ...)` 各自启动。**本地 UI 只显示自己那份**（EventModel.cs L66-73 注释原文）。这一设计是"每人看到不同选项"的合法基础。

### 1.1 非共享事件（IsShared=false，默认）
- **选项生成**：每份实例独立跑 `SetInitialEventState` → `GenerateInitialOptionsWrapper`（AncientEventModel.cs L193-207）。DARV/Neow 等先古事件全部非共享。
- **选择同步**：本地选完发 `OptionIndexChosenMessage`（只带 **optionIndex 数字**，ChooseLocalOption L246-254）；对端收到后 `ChooseOptionForEvent(player, index)` 在**对方玩家那份实例**上执行**同索引**选项。
- **RNG 派生**（EventModel.cs L234）：`Rng = new Rng(RunState.Rng.Seed + (IsShared ? 0 : playerSlotIndex) + hash(事件Id.Entry))`——非共享事件**每个 slot 有专属 RNG 流**，同一事件不同玩家掷出不同结果是**设计意图**。
- **硬边界**（ChooseOptionForEvent L284-287）：`optionIndex >= CurrentOptions.Count` 直接抛 `InvalidOperationException`。

> **规则 V4-R1**：非共享事件的选项数/选项语义由**各端本地**生成；引擎只同步"第几个"。**任何 mod 改动 GenerateInitialOptions 的可见行为（重roll、增删选项、换顺序）必须双端同时同条件生效**——单端生效=对端索引落在完全不同的选项上（家族C实锤：房主3选项vs朋友3选项，index 2 = 星盘 vs 尘封魔典）。

### 1.2 共享事件（IsShared=true，仅 8 个）
BattlewornDummy / DenseVegetation / FakeMerchant / JungleMazeAdventure / MorphicGrove / PunchOff / TheLanternKey / WarHistorianRepy。
- 投票制：`VotedForSharedEventOptionMessage` 收票 → host 齐票后 `_multiplayerOptionSelectionRng.NextItem(_playerVotes)` 定胜（**只在 host roll，明确注释"不要依赖它跨端确定"** L53-57）→ `SharedEventOptionChosenMessage` 广播统一 index。
- 注释明文（EventModel.cs L76-80）："**会转场到其它房间的事件必须 shared**"。

> **规则 V4-R2**：转场类事件（进战斗/开商店/进小游戏）做不成 shared 的，不能进 MP 池。

## 2. 奖励同步链（RewardsSetSynchronizer）

- **per-player 奖励栈**：每人一个 `PlayerRewardState`（rewardsStack 栈 + bufferedMessages 缓冲 + completedRewards 台账），`RewardsSet.Id` 由**接收端本地计数器** `nextId++` 分配（BeginRewardsSet L167-171）。
- **消息只带 (setId, rewardIndex)**：选择方发 `RewardSelectedMessage`；接收端若 `nextId <= setId`（该 set 还没在本地生成）→ **无限期缓冲**（L254-262，日志 "Buffering ... hasn't been created yet"）。缓冲的消化时机只有一个：同 id 的 set 后来真的 `BeginRewardsSet`（L180-196）。
- **跨端 Id 对齐机制**：`GetNextRewardIds`/`FastForwardRewardIds`（L437-452）用于重连快进——常规对局中 set 是否生成由**各端本地逻辑**决定，引擎不补发"set 应该生成"的信号。

> **规则 V4-R3**：奖励的**生成路径**（哪些代码路径会 spawn RewardsSet）必须双端一致；生成什么内容（卡牌/遗物的具体 roll）可以不同（走 PlayerRng.Rewards per-player 流）。**只改一端的生成路径 = 对端永久缓冲**。家族C实锤：朋友的 DUSTY_TOME onChosen → RelicCmd.Obtain → DustyTome 奖励 set（id 12）在朋友端生成；房主端选项列表根本没有尘封魔典，同一事件房主走星盘分支，set 12 永不生成 → 5 条消息永久 buffer。

> **规则 V4-R4**：mod 拿奖励不要绕过 `Reward/RewardsSet` 体系直发 `CardCmd.Add`——直发不经同步器，双端各自改牌库=checksum 分歧（除非该路径双端从同一消息驱动，见 §4）。

## 3. 位置定向消息缓冲（RunLocationTargetedMessageBuffer）

- 所有 `IRunLocationTargetedMessage`（含 RewardSelected/OptionIndexChosen/VoteForMapCoord）带 `location`（act/coord/room）投递；本地 `CurrentLocation` 不同 → 入队等 `OnLocationChanged`（L70-90）。
- **死等条件 A**：消息的 location **永远不会再被访问**（`_visitedLocations` 不含且不会再含）→ 永久滞留；下一次位置变更会打 `Error: still N messages for other locations`（L88）——**这条 Error 是"有消息永远等不到地方"的官方信号**。
- RunLocation 用 act+coord+room 标识；同一坐标重进（如 back-to-back 同房间）不重置缓冲。

> **规则 V4-R5**：跨房间延迟效果（"离开事件后再给奖励"类）必须确保两端的 room 序列一致；mod 自定义转场/自定义房间会改变一端的 location 序列 → 对端定向消息永久滞留。

## 4. 房间过渡三重屏障与卡死语义（EventRoom.Exit → MoveToMapCoord）

退房链（EventRoom.cs L90-104）：`AwaitPendingOptionTasks`（等所有 onChosen 任务完成）→ `ChecksumTracker.GenerateChecksum("Exiting event room ...")`（仅 IsDeterministic=true 即非共享事件）→ 清理。随后 `RunManager` L892/L1097/L1285 三处 `StartSync()` → `WaitForSync()`。

`CombatStateSynchronizer.WaitForSync`（CombatStateSynchronizer.cs L152-163）：`await _syncCompletionSource.Task`——**无超时、无取消**。完成条件只有两个：收齐所有 peer 的 `SyncPlayerDataMessage`（CheckSyncCompleted），或 **peer 断线**（OnPeerDisconnected L103-111 会 CheckSyncCompleted）。

> **规则 V4-R6（卡死的充要条件）**：只要"对端永远不发 sync 消息且不断线"，本端就永久黑屏等待。触发路径：对端先死在另一个等待点（如索引越界抛异常把任务链炸断）、或对端的过渡路径被 mod 改得不经过 StartSync。**唯一自然解除=断线**——这就是家族C黑屏"只能强退"的机制解释。
>
> 排查口诀：**日志冻在 "Waiting to receive all sync messages" = 对端没走到它自己的 sync 点**；往上翻对端日志找它卡在哪（通常是异常或另一个死等）。

> **规则 V4-R7（checksum 语义）**：`GenerateChecksum` 注释明文（L83-84）："**每个 peer 必须精确相同次数调用**，否则产生假阳性 mismatch"。mod 在退房路径上做单端条件跳过（如某配置只在房主生效的额外奖励）会造成**调用次数差**→ 立即 StateDivergence 弹窗（而非黑屏）。IsDeterministic=IsShared 反转（L56）：共享事件不参与事件退房 checksum。

## 5. RNG 三层体系（哪个能不同、哪个必须同）

| 层 | 实例 | 双端一致? | 依据 |
|---|---|---|---|
| Run 层 | `RunState.Rng`（地图/遭遇等） | **必须**（host 经 SyncRngMessage 下发快照，WaitForSync L177-186 强制对齐） | CombatStateSynchronizer L136-143 |
| Player 层 | `PlayerRngSet{Rewards,Shops,Transformations}`，种子=hash(runSeed)+slotIndex（Player.cs L330） | **按玩家各一份**（每人自己的奖励 roll 独立） | PlayerRngSet.cs L14-29 |
| Event 层 | `EventModel.Rng`，种子=runSeed+slotIndex+hash(事件Id)（EventModel.cs L234） | **按玩家各一份**（非共享事件） | 同上 |

> **规则 V4-R8**：mod 随机性必须选对层：内容 roll（给什么卡）→ PlayerRng.Rewards 或 EventModel.Rng；结构性 roll（房间内容/遭遇）→ 必须走双端同步的 RunState.Rng 或干脆 host-roll 后广播。**绝不用本地 new Random()/Guid**——直接制造不可复现分歧。

## 6. mod 侵入风险模式清单（对照已有三家族+潜在第四类）

| # | 模式 | 实例 | 后果 |
|---|---|---|---|
| M1 | Prefix/Postfix 改 `GenerateInitialOptions` 输出且门控条件单端成立 | AFTP DarvUniqueOffersPatch（家族C） | 索引错位→对端奖励 set 永不生成→黑屏 |
| M2 | 本地配置直读（不加 Effective 守卫）改变事件分支 | AFTP RebalancedMode 75 处（家族B）、DarvOnlyInLegacyActs、LegacyEnemiesGiveClassicSlimed | 同一 index 双端执行不同 onChosen→checksum 分歧弹窗 |
| M3 | 替换实体类型（如 Slimed→ClassicSlimed）单端生效 | AFTP 粘液族（家族A） | 牌实体 ModelID 不同→checksum 分歧 |
| M4 | 事件 onChosen 内直调 `SetupForPlayer` 等 per-player 副作用 | Darv DustyTome 路径（配合 M1） | 对端无对应选项→set 缺失（见 R3） |
| M5 | mod 转场/自定义房间改变 location 序列单端差异 | （未观察到，理论） | 定向消息永久滞留（R5） |
| M6 | 退房路径单端额外奖励生成 | （未观察到，理论） | checksum 调用次数差→假阳性 divergence（R7） |

## 7. 预防 checklist（mod 写手落地版）

1. **改任何事件选项/分支前问一句：这个改动 MP 下双端会同时发生吗？**门控条件只能是：`RunManager` 权威状态（如 NetService.Type、act 类型、双方一致的 run 状态）或 `XxxEffective`（SP-only 合取）——**永远不是本地 cfg 裸读**。
2. **新增选项/重排选项**：非共享事件中选项是各端独立生成的——mod 新增选项必须注册于双端一致的模型层（ModelDb 注册池），且生成条件不含单端状态。
3. **奖励生成走 RewardsSet**（引擎自同步）；确认生成路径的触发条件双端同源（同一 OptionIndexChosenMessage 驱动）。
4. **随机 roll 分层**：内容→PlayerRng/EventRng；结构→RunState.Rng 或 host 广播。
5. **退房钩子里不做条件性状态变更**（AfterRoomEntered/Exit 必须无条件双端同路径执行）。
6. **每轮 MP 测试看两类信号**：`Buffering ... hasn't been created yet`（>0 且局末仍在=生成路径分歧）；`still N messages for other locations`（location 序列分歧）。
7. **部署纪律**：同 mod 双端 dll 逐字节一致（md5 清单）；单端新 dll=单端新行为=M1-M6 任一模式复活。

## 8. 已知家族与本卷规则的映射

| 家族 | 现象 | 违反规则 | 修复 |
|---|---|---|---|
| A 粘液 | 打 Slimed 卡断线 | M3/R7 | ClassicSlimed MP 标记网络重建（22e83d3） |
| B 复制机/Rebalanced | 事件选择后断线 | M2/R1 | RebalancedModeEffective 75 处（22e83d3） |
| C DARV 黑屏 | 尘封魔典选择后黑屏 | M1+M4/R1+R3+R6 | DarvOnlyInLegacyActsEffective 等（f166f11） |
| （防）未来家族 | — | M5/M6/R5/R8 | 本卷 checklist 预防 |

## 附：证据锚点索引

- EventSynchronizer.cs：L57（option-selection rng host-only 注释）、L98-126（per-player 克隆）、L178（NextItem 齐票）、L206-222（OptionIndexChosen 处理）、L284-287（索引越界抛异常）
- EventModel.cs：L56（IsDeterministic=!IsShared）、L66-73（Owner 每人一份注释）、L76-81（IsShared 语义）、L234（事件 RNG 派生式）
- AncientEventModel.cs：L193-207（GenerateInitialOptionsWrapper）、L268-288（RelicOption onChosen→Obtain+Done）
- Darv.cs：L147-168（GenerateInitialOptions 原版）、L191-197（CurrentActIndex 过滤器——**先古选项本身含 act 位置条件**）
- DustyTome.cs：L50-56（SetupForPlayer=PlayerRng.Rewards.NextItem 掷先古牌）
- RewardsSetSynchronizer.cs：L167-199（BeginRewardsSet/缓冲消化）、L249-267（Buffering 判定）、L437-452（Id 快进）
- RunLocationTargetedMessageBuffer.cs：L70-90（location 变更消化+残留 Error）
- CombatStateSynchronizer.cs：L103-111（断线才解锁）、L119-147（StartSync/Host 下发 Rng 快照）、L152-163（WaitForSync 无超时 await）
- EventRoom.cs：L90-104（Exit 链）、L127-142（Ancient 全员完成→MarkPreFinished）
- ChecksumTracker.cs：L79-103（GenerateChecksum 次数一致要求）、L131-153（mismatch→DisconnectClient）
- PlayerRngSet.cs：L14-29（三层用途注释）、Player.cs L330（slot 掺盐）
