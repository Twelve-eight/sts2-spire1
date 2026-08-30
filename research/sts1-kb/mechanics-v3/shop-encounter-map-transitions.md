# StS2 多人同步机制卷六——商店/遭遇/地图幕过渡（2026-08-30 拆解）

> 生成：2026-08-30（接卷四卷五）。来源：sts2.dll v0.111.0 反编译源逐类精读 + AFTP fork 源码审计。
> 卷四回答"事件/奖励为什么能不同"；卷五回答"房间级专用同步器"；本卷回答"**商店怎么卖、遭遇怎么选、地图怎么走、幕怎么切**——每条链的 RNG 归属、消息通道、死等点、mod 安全侵入面"。
> 拆解方法：四路 scout 并行 + 主会话逐条复核；三份被打断的 scout 报告经 transcript 打捞恢复（.tmp/MapActTeardown-salvage-ascii.txt、AftpRiskAudit-salvage-ascii.txt）。所有 file:line 均经主会话或对应 scout 直接读源核实。

---

## 0. 核心结论（先读）

1. **遭遇选择零 per-player RNG**：全部走 run 级流（UpFront 开局一次性滚完 + Unknown 流按点滚类型），PlayerRngSet 只管 Rewards/Shops/Transformations 三件事。mod 想"让某玩家遭遇不同"没有合法钩子——除非破坏同步。
2. **可变态遭遇不跨网序列化**：双端各自本地构造 mutable EncounterModel，一致性靠种子公式 `runSeed + TotalFloor + hash(encounterId)`（EncounterModel.cs L263-264）。
3. **商店货架是 per-player 本地生成**（`Player.PlayerRng.Shops` 各自 roll，双端内容可不同），**购买效果经 RewardSynchronizer 广播**（GoldLostMessage/RewardObtainedMessage/CardRemovedMessage，location 定向）。
4. **地图拓扑零消息**：`new Rng(runSeed, "act_N_map")` 种子派生，双端天然一致（StandardActMap.cs L112-114）；地图投票只有 host 用 `map_point_selection` RNG 破平票（MapSelectionSynchronizer.cs L52-90）。
5. **幕切换也是"齐票 AND"**：ActChangeSynchronizer 全玩家 VoteToMoveToNextActAction 齐了才 EnterNextAct；**无超时、无断线兜底**——与卷五"三姐妹"同族，是第四个无兜底死等点。
6. **新实锤两个无断线兜底点**：MapSelectionSynchronizer（断线玩家票位永空→host 永不 MoveToMapCoord）与 ActChangeSynchronizer（断线未投票玩家→永久等待）。对照 CombatStateSynchronizer/RestSiteSynchronizer 都有 OnPeerDisconnected 兜底——**这两处是引擎自身的断线死等缺口**。
7. **AFTP 候选风险**：MatchAndKeep 小游戏在 UI 回调里直接加卡（仅 Owner 端执行）——结构性单端副作用，待双端实测定级。

---

## 1. 商店（Merchant + RewardSynchronizer）

### 1.1 货架生成（per-player RNG，双端可不同）

- `MerchantInventory.CreateForNormalMerchant(player)`（MegaCrit.Sts2.Core.Entities.Merchant/MerchantInventory.cs L78+）：卡牌用 `player.PlayerRng.Shops.NextInt`（L100），药水用 `Player.PlayerRng.Shops` 滚 3 瓶（L143）。
- 结论：**每个玩家的商店货架按他自己的 Shops 流 roll**。双端对同一玩家的同一货架内容一致（同 seed 同流）；不同玩家之间货架天然不同——这是设计，不是 bug。
- 遗物货架走 RelicGrabBag `PullFromBack`（卷五 §4：商店专用从桶尾拉，不抢奖励位）。

### 1.2 购买同步（RewardSynchronizer 广播）

- `RewardSynchronizer`（MegaCrit.Sts2.Core.Multiplayer.Game/RewardSynchronizer.cs）自述"临时同步器，通知其他玩家在非确定性场景获得的奖励——目前仅商店用"（L23-26）。
- 购买路径（MerchantCardEntry.cs L131-143 / MerchantRelicEntry.cs L55-64 / MerchantPotionEntry.cs）：
  - `SyncLocalGoldLost(Cost)` → 广播 **GoldLostMessage**（L172-178，带 location）
  - `SyncLocalObtainedCard/Relic/Potion(...)` → 广播 **RewardObtainedMessage**（L88-96/L114-121/L140-147）
  - 删卡服务：`DoLocalCardRemoval` 发 **CardRemovedMessage** 后本地删（L180-188），对端收消息后 `DoUnsyncedCardRemoval`（L281-299，`CardRemovedMessage should not be sent to the player removing the card` 防自收）
- 接收端（HandleRewardObtainedMessage L190-262）：按 senderId 找玩家 → 应用金币/卡/药水/遗物 + 写历史（CardChoices/PotionChoices/RelicChoices）。**对端不 roll 内容，直接用消息里的 model**——所以货架内容分歧无害，购买结果必须一致。
- **战斗内缓冲**：RewardSynchronizer 三消息在 `CombatManager.Instance.IsInProgress` 时先入 `_bufferedMessages`（L192-201/L266-274/L283-291），`OnCombatEnded` 时补放（L301-319）——商店战斗外买不受影响，但事件战斗内触发奖励会排队。
- **错误面**：`SyncLocalCardEvent` 等在战斗内调用直接抛 `InvalidOperationException`（L84-87）——mod 在战斗内偷调会崩，不是分歧。

### 1.3 mod 安全侵入面

- 改货架内容（卡/药水/遗物/价格）是**本地生成阶段**，安全（每端各自 roll 自己的）。
- 改购买效果（加额外遗物等）必须走 RewardSynchronizer 广播或确定性命令，不能只改本地玩家对象——对端不知道。
- 商店新增"一次性服务"（如删卡）需走 CardRemovedMessage 同款模式。

---

## 2. 遭遇选择与战斗入口（Encounter + EventCombatSynchronizer）

### 2.1 遭遇在哪选、用什么 RNG

**开局一次性（run 级 UpFront 流）**：
- `RunManager.GenerateRooms`（RunManager.cs L743-766）遍历全部幕调 `ActModel.GenerateRooms`（L756 传 `State.Rng.UpFront`）；Ancient 池同流洗牌（L745-751）；双 Boss 进阶滚第二 Boss（L762-764）。
- `ActModel.GenerateRooms`（ActModel.cs L331-386）单流做完全部：事件洗牌（L347）、3 弱遭遇（L348-354）、常规遭遇填满（L355-365）、15 精英（L366-383）、boss 用 `NextItem(AllBossEncounters)`（L384）、Ancient 用 `NextItem`（L385）。`AddWithoutRepeatingTags`（L416-426）避免同 tag 连出。
- **幕列表也是种子派生**：host 只广播 seed 字符串（StartRunLobby.cs L451-468 LobbyBeginRunMessage），双端各自 `act_selection` RNG 滚 `ActModel.GetRandomList`（L469-513/L538-562）。

**按点进入（确定性状态 + run 流）**：
- `EnterMapPointInternal`（RunManager.cs L876-936）用 `RollRoomTypeFor`（L976-1005）滚房间类型：固定点直映，? 点走 `State.Odds.UnknownMapPoint`（UnknownMapPointOdds.cs L127-165，从 UnknownMapPoint 流抽 + 滚后改 odds）；首次? 强制 Event（L135-144）。
- 遭遇内容**进入时不重 roll**：`PullNextEncounter`（ActModel.cs L445-456）纯查 RoomSet 位置队列（visited 计数取模，RoomSet.cs L72-84）；`MarkVisited` 只在房间栈深 1 时推进（RunManager.cs L1230-1232）——**事件嵌套战斗不消耗遭遇槽位**。

### 2.2 可变态遭遇的种子公式（双端零消息一致的核心）

- `GenerateMonstersWithSlots`（EncounterModel.cs L254-271）：per-encounter RNG 种子 = `runSeed + TotalFloor + hash(encounterId)`（L263-264）。TotalFloor = 全部幕地图点历史总数（RunState.cs L158）。
- **遭遇从不跨网序列化**：每端本地 `ToMutable()`（L315-321），一致性全靠同 seed 同公式。
- 生成防重：`HaveMonstersBeenGenerated`（L116）+ 二次生成抛异常（L257-259）；Combat-layout 事件在 `EventCombatSynchronizer.InitializeForEvent` 就生成（L64-88），`StartCombat` 对 `ShouldCreateCombat=false` 跳过重建（CombatRoom.cs L210-216）。

### 2.3 怪物生成与 AI RNG

- `CombatState.CreateCreature`（CombatState.cs L233-248）：怪物 `RunRng = RunState.Rng`（共享集，L236）、同名牌 HP 用 `Niche` 滚（L241）、多人 HP 缩放（L242）、顺序 CombatId（L244）、per-monster 外观 RNG = runSeed+列+行+幕+CombatId（L245）。
- **AI 单一共享流**：`MonsterModel.RollMove`（L416-419）→ `MonsterMoveStateMachine.RollMove`（L34-40）用 `RunRng.MonsterAi`；`RandomBranchState.GetNextState`（L115-129）`NextFloat(totalWeight)` 走权重分支，重复/冷却动态清零权重（L131-165）。**没有 per-monster/per-combat AI 流**——抽序必须双端一致（ActionQueue 同步保证）。
- 外观混沌：`Rng.Chaotic`（Rng.cs L25 墙钟种子）用于 scale/hue——表现层，故意非确定性。

### 2.4 战斗前对齐（CombatStateSynchronizer 屏障）

- `EnterMapPointInternal`（L892/L897）与 `EnterRoomWithoutExitingCurrentRoom`（L1285/L1293）在房间创建前跑 StartSync/WaitForSync：每端广播 SyncPlayerDataMessage，host 额外广播完整 RunRngSet + 共享遗物抓包（SyncRngMessage）→ client 覆盖本地 RNG（WaitForSync L171-183）。
- **已知引擎 TODO 风险**：SyncRngMessage.cs L12-15 注释——client 若 Niche 流已超前，回滚可能**同一值 roll 两次**。引擎自认的坑。

### 2.5 事件→战斗过渡（EventCombatSynchronizer，无消息的本地齐票）

- 事件实例各自调 `ReadyToEnterCombat(canonicalEncounter, owner, extraRewards, shouldResumeAfterCombat)`（EventModel.cs L586 → EventCombatSynchronizer.cs L103-121），**每玩家槽位存一份**，全齐才 `EnterCombat()`（L122）。
- `EnterCombat` 校验所有玩家实例的 canonicalEncounter 与 resume 标志一致（L137-149，不一致抛异常）→ 建 CombatRoom（L149-153）→ 挂每人 extraRewards（L154-158）→ `EnterRoomWithoutExitingCurrentRoom`（L160）。
- **自述"不是 synchronizer，不发消息"**（L17-20）：一致性全靠前面的选项消息把每端事件实例推到同一决定。
- 失败模式：选项消息丢 → 该端事件实例走不到战斗选项 → 槽位永 null → **该端永久等待（无超时无断线处理）**。可靠传输+缓冲就是防这个的。
- 选项消息三件套（全 reliable/buffered/location-targeted）：非共享 `OptionIndexChosenMessage`（EventSynchronizer.cs L230-258）；共享 `VotedForSharedEventOptionMessage` + host `_multiplayerOptionSelectionRng`（seed+`event_synchronizer` 名，L77）破平票 → `SharedEventOptionChosenMessage`（L171-197）。

### 2.6 mod 单端倾斜面（实锤清单）

1. `RandomBranchState` 权重：改任一 weightLambda 就改所有端的 NextFloat 分布——**仅全端同 mod 才安全**（PeerVersionInfo 握手核对 mod 清单）。
2. 遭遇构成：`GenerateMonsters` override 只在单端 → 双端怪物集不同 → 立即 checksum 分歧（PunchOffEventEncounter.cs L17-19 等用 EncounterModel.Rng）。
3. `Hook.ModifyNextEvent`（Hook.cs L1839-1847）：改事件 id → 级联改遭遇种子与内容。
4. 生物创建顺序：CombatId 喂 per-monster 种子（CombatState.cs L245）——增删/重排生物即分歧。
5. `DebugRandomizeRng`（EncounterModel.cs L345-349）：唯一合法单端重种子，但仅 dev console，且联网命令经 ActionQueue 全端广播。

---

## 3. 地图生成、投票与幕过渡

### 3.1 地图拓扑（种子派生，零消息）

- `StandardActMap.CreateFor`（StandardActMap.cs L112-114）：`new Rng(runState.Rng.Seed, $"act_{CurrentActIndex+1}_map")`——与可变 RNG 流状态无关，纯 (seed, actIndex) 函数。**同幕重生成出同一地图**（Golden Compass 再生成也同，靠 MapGenerationCount 使旧票失效）。
- 存档恢复：SavedActMap 原样恢复（RunManager.cs L807-816），只允许 Late 钩子跑。
- **分歧风险**：任何 `ModifyGeneratedMap` 钩子若消费 RNG/本地状态（如 SecretPortal 墙钟门）即双端可分歧。

### 3.2 地图投票（host 唯一决策）

- 每端 `VoteForMapCoordAction`（ActionType.Any，L37）→ 经 ActionQueueSynchronizer 全端执行 → `MapSelectionSynchronizer.PlayerVotedForMapCoord`（source 校验丢弃旧源票）。
- **host 齐票才 MoveToMapCoord**：`_votes.All(v => v.HasValue && v.mapGenerationCount == 当前)` && Type != Client（MapSelectionSynchronizer.cs L80-88）；host 用 `_multiplayerMapPointSelection`（`new Rng(runSeed, "map_point_selection")`，构造一次，**不在 RunRngSet、不同步**）从全部票里 `NextItem` 破平（注意：均匀抽一票，不是多数派）→ 广播 MoveToMapCoordAction（L52-90）。
- 客户端自己算 map_point_selection 流但永不推进、永不使用——host 迁移不存在，安全。
- **断线死等（新实锤）**：`_votes` 按 `RunState.Players`（含已断线玩家）定长，**无 RemotePlayerDisconnected 订阅**——断线玩家票位永空 → host 永不 MoveToMapCoord → 全队卡地图屏。对照 CombatState/RestSite 都有断线兜底，**这是引擎缺口**。
- 防重入：`MoveToMapCoordAction.ExecuteAction` → `EnterMapCoord` 先 `AddVisitedMapCoord` 判重（RunManager.cs L837-843），重复执行 no-op。

### 3.3 幕过渡（ActChangeSynchronizer，分布式 AND）

- 触发：终端奖励屏 Proceed（NRewardsScreen.cs L570-586，boss/胜利房）→ `SetLocalPlayerReady()` → `VoteToMoveToNextActAction`（NonCombat）经 ActionQueue 全端执行 → 每端 `OnPlayerReady`（L62-85）。
- 防重入：`actIndex < CurrentActIndex || actIndex <= _lastTransitioningActIndex` 忽略旧幕票（L65-72），胜利房豁免。
- 齐票 → `MoveToNextAct`（L87-95）：复位 flags → `_lastTransitioningActIndex` → `ActFloor++` → `TaskHelper.RunSafely(EnterNextAct())` → 隐藏等待 overlay。**每端独立执行 EnterNextAct，不是 host 广播**——靠 ActionQueue 全序保证同刻齐票。
- `EnterNextAct`（RunManager.cs L1308-1332）→ `EnterAct` → `SetActInternal`（L1378-1396：清 visited coords、重置 Unknown odds、`AfterMapLocationChanged`、`GenerateMap`、进新 MapRoom）。**幕过渡不做新遭遇 roll**（全幕内容开局已滚完），只重生成种子地图。
- **断线死等（新实锤）**：`_readyPlayers` 定长且**无 RemotePlayerDisconnected 订阅**；断线玩家未投票 → 永久等待 overlay。**引擎缺口二**。
- 注意：`MoveToNextAct` 的 `ActFloor++` 先于 `EnterNextAct`，SetActInternal 会重置——瞬态值，非分歧源。

### 3.4 RunLocation 与位置定向缓冲（卷四 §3 补充）

- `RunLocationTargetedMessageBuffer`：`_visitedLocations` HashSet **只增不减、跨幕不清**（每房间入口加一项，几百项封顶，非泄漏但无界）；消息按 location 延迟投递，`OnLocationChanged` 若仍有滞留打 Error（L76-79）——**滞留消息永不过期**，是内存+功能双重滞留源。
- 构造器把 `default(RunLocation)`（act0/null/null）预加进 visited（L63）——首个 MapRoom 实际 location 是 (act0,null,0)，**初始 location 未被预访问**，早期消息会被缓冲到首次 OnLocationChanged。
- roomId：每房间 Enter 时 `GetAndIncrementNextRoomId`（AbstractRoom.cs L59），`AddVisitedMapCoord` 重置为 0（RunState.cs L460-468）——roomId 是局部计数，双端一致性靠房间进入顺序（ActionQueue 保证）。
- 重新进入：`AddVisitedMapCoord` 判重阻止重复进入（L837-843）；`RemoveStaleVisitedMapCoords`（RunState.cs L649-656）在 Golden Compass 重生成地图后清理失效坐标。

---

## 4. 新死等点与引擎缺口汇总（卷四 V4-R6 家族扩展）

| 死等点 | 同步器 | 解除条件 | 断线兜底 |
|---|---|---|---|
| WaitForSync（卷四 V4-R6） | CombatStateSynchronizer | 收齐玩家数据+RNG 快照 | ✅ OnPeerDisconnected L103-111（+RNG 快照缺失则 client 仍挂，卷四已订正） |
| AfterAllRestSitesCompleted（卷五） | RestSiteSynchronizer | 全员 completionTaskSource | ✅ OnPeerDisconnected L144-162 |
| 宝箱投票齐票（卷五） | TreasureRoomRelicSynchronizer | 全员投票 | ❌ 无（卷五已订正"三姐妹"表述） |
| **地图投票齐票（本卷）** | MapSelectionSynchronizer | host 收齐全员有效票 | ❌ **无——断线玩家票位永空** |
| **幕过渡齐票（本卷）** | ActChangeSynchronizer | 全员 VoteToMoveToNextActAction | ❌ **无——断线玩家 ready 位永 false** |
| **事件→战斗齐票（本卷）** | EventCombatSynchronizer | 全员 ReadyToEnterCombat | ❌ 无（选项消息可靠传输兜底，但消息丢则单端挂） |

**排查口诀补充**：日志停在地图屏无推进 = 查对端是否发了票/断了线；停在"等待其他玩家进入下一幕" = 查对端奖励屏是否点了 Proceed。

---

## 5. AFTP 风险审计结果（fork 源码 + 引擎契约对照）

### 5.1 已修复面（本轮确认干净）

- 配置类分歧（RebalancedMode/DarvOnlyInLegacyActs/ClassicSlimed/双 shared 过滤）：Effective 包装器全覆盖，裸读零残留（increment-review-20260830 F1 修复后）。
- RewardsSet.WithRewardsFromRoom Postfix（Colosseum/MindBloom/DeadAdventurer/MaskedBandits/MysteriousSphere 删奖）：双端对称（encounter 类型 + ExtraRewards 来自游戏状态），安全。
- 事件内直接加卡（BigFish/MindBloom/WindingHalls/Augmenter/ForgottenAltar/KnowingSkull/MaskedBandits/Nloth/OldBeggar/TheMausoleum/TheNest/PleadingVagrant/Duplicator/Necronomicon 等）：全部在**选项处理器内**执行 → 经 OptionIndexChosenMessage/共享投票消息双端重放 → 安全（引擎同款模式）。
- 事件 Rng 使用（LivingWall/Augmenter/Transmogrifier 等用 `RunState.Rng.Niche` 或事件 Rng）：共享/非共享事件的 RNG 消费均对称（卷四公式），安全。

### 5.2 候选风险（待双端实测定级）

1. **MatchAndKeep 小游戏加卡（结构性单端副作用）**：
   - `MatchAndKeep.Play()`（SharedEvents/MatchAndKeep.cs L40）→ `MatchAndKeepMinigame.PlayMinigame()`（L121-127：`if (!LocalContext.IsMe(Owner)) return;`）→ `NMatchAndKeepScreen.HandleMatch` 配对回调里 `CreateCard` + `CardPileCmd.Add`（NMatchAndKeepScreen.cs L517-518，UI 驱动、仅 Owner 端执行）。
   - 机制链：非共享事件，玩家 A 端玩 UI 加卡；B 端 A 的实例 `!IsMe(A)` 直接 return → `SetEventFinished` → **B 端 A 牌堆无卡**。`CardPileCmd.Add` 是纯本地命令（CardPileCmd.cs L324+，无网络消息）。
   - 定级待实测：若下一次战斗 sync 的 `SyncWithSerializedPlayer` 覆盖修复 → 无害漂移；若 checksum 检测牌堆 → 立即分歧。**建议双人实测 MatchAndKeep 一次**。
2. **SecretPortal 墙钟门（已知，确认机制）**：`IsAllowed` 用 `RunManager.Instance.RunTime <= MinRunTimeSeconds`（SecretPortal.cs L31）——RunTime 是墙钟，双端加载/动画速度不同 → 资格判定可分歧 → 事件池/地图分歧。RebalancedModeEffective 门只挡了 rebalanced 分支，墙钟判定本身未修。**规避：联机时双端同速（关 SpeedX 类变速）或接受偶发差异**。
3. **Minigame 非本地端立即完成**：MatchAndKeep/WheelSpin/PortalMapBuilder 的非 Owner 实例直接 return + SetEventFinished——只要加卡/发奖都在 Owner 端 UI 回调里（MatchAndKeep）就有 1 的风险；WheelSpin 结果在进 minigame 前已从事件 Rng 滚定（ApplyResult 确定性重放），安全。

### 5.3 审计方法说明

- 引擎契约来源：事件双端重放（EventSynchronizer.cs L98-258）、事件 RNG 公式（EventModel.cs L234）、RewardsSet 缓冲（RewardsSetSynchronizer.cs L254-262）、CardPileCmd 纯本地（CardPileCmd.cs L324+）。
- 打捞恢复：AftpRiskAudit 被预算停机，其 11 个思考块（35KB）经 ASCII 清洗提取，关键机制链（MatchAndKeep/SecretPortal/共享事件 RNG 语义）由主会话逐条对照源码复核后采信。

---

## 6. 附：证据锚点（主会话直接核实）

- RunManager.cs：L743-766（GenerateRooms/UpFront）、L799-838（GenerateMap）、L876-936（EnterMapPointInternal+同步屏障）、L941-967（CreateRoom/PullNextEncounter）、L976-1033（RollRoomTypeFor/Unknown）、L1230-1232（MarkVisited 栈深门）、L1276-1316（EnterRoomWithoutExitingCurrentRoom）、L1308-1396（EnterNextAct/EnterAct/SetActInternal）、L837-843（AddVisitedMapCoord 防重入）
- EncounterModel.cs：L254-271（种子公式）、L315-321（ToMutable）、L345-349（DebugRandomizeRng）、L396-413（SpawnedEnemies）
- ActModel.cs：L331-386（GenerateRooms）、L416-426（AddWithoutRepeatingTags）、L434-456（PullNextEvent/PullNextEncounter）、L538-562（GetRandomList）
- CombatState.cs：L233-248（CreateCreature：RunRng/Niche HP/CombatId/per-monster seed）
- CombatStateSynchronizer.cs：L119-186（StartSync/WaitForSync）、L103-111（OnPeerDisconnected）
- EventCombatSynchronizer.cs：L64-88（layout 生成）、L103-121（ReadyToEnterCombat 齐票）、L127-165（EnterCombat 校验+建房）、L168-180（ResetState）
- EventSynchronizer.cs：L98-121（per-player 实例）、L171-258（共享/非共享选项消息）、L372-383（AwaitPendingOptionTasks）
- RewardSynchronizer.cs：L88-178（三类消息构造）、L180-188（DoLocalCardRemoval）、L190-299（三消息接收+战斗缓冲）、L301-319（OnCombatEnded 补放）
- MerchantInventory.cs：L78+（CreateForNormalMerchant，Shops 流滚货架）
- MerchantCardEntry.cs L131-143 / MerchantRelicEntry.cs L55-64（购买→SyncLocalGoldLost+Obtained）
- MapSelectionSynchronizer.cs：L38-39（map_point_selection RNG）、L52-90（host 齐票+NextItem+MoveToMapCoordAction）、L80-88（quorum 条件）
- ActChangeSynchronizer.cs：全文件（SetLocalPlayerReady/OnPlayerReady/MoveToNextAct，无断线订阅）
- StandardActMap.cs：L112-114（seed+act 名派生）
- RunLocationTargetedMessageBuffer.cs：L63（default location 预访问）、L70-90（OnLocationChanged+滞留 Error）
- SyncRngMessage.cs：L12-15（Niche 回滚 TODO）
- RandomBranchState.cs：L115-165（权重分支+冷却清零）
- Rng.cs：L25（Chaotic 墙钟）、L54-56（named seed）
- PlayerRngSet.cs：L19/24/29（Rewards/Shops/Transformations）
- MatchAndKeep.cs L40 / MatchAndKeepMinigame.cs L121-127（IsMe 门） / NMatchAndKeepScreen.cs L517-518（UI 加卡）
- SecretPortal.cs L31（RunTime 墙钟门）
