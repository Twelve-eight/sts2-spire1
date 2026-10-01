# 姿态形态真实战斗 smoke runner r15 监督审查报告

- 审查日期: 2026-10-02
- 角色: Gauss 实现者监督审查员
- 实现代理: 01a0f8a5-725b-74b1-b6c3-ad77896a1598
- 用户指定模型/路由: 6.1sol via agentrouter
- 监督前置: 已通过 Codex 原生 hub wait 等待实现代理完成；其 turn 已 completed，随后才开始本审查。
- 范围: 只读静态审查；未构建、未测试、未部署、未启动游戏。

## 已确认

### 检查面 0：实现报告与审查前置

已读取 r15 实现报告与当前两个白名单源文件。实现代理报告确认本轮只修改 `FormNativeSmokeRunner.cs`，`FormNativeSmokePatch.cs` 未改变，并明确未构建、未测试、未部署、未启动游戏。以下结论均为源码静态证据，不是实机证据。

### 检查面 1：r13 形态 gate 仍存在

当前 `FormNativeSmokeRunner.cs:1068-1102` 仍按 calm/wrath/divinity 绑定 Watcher 卡牌与三组预期类型；`FormNativeSmokeRunner.cs:1116-1172` 读取 native stance、carrier 和两个 effect 的快照，并在 `FormNativeSmokeRunner.cs:1134-1141` 将四项条件合取为 gate。`FormNativeSmokeRunner.cs:1189-1204` 从真实玩家 Power 集合读取类型并记录 `FormStanceMode.IsSelected(player)`。未发现 r13 gate 被移除或退化为只写快照。

### 检查面 2：普通入口与真实链路的初步静态证据

`FormNativeSmokePatch.cs` 仍只有 `NGame._Ready` postfix 入口，未见新增直接应用 Power、`TestMode`、`--autoslay`、`PowerCmd.Apply`、`Task.Run` 或 `NGame.Quit`。`FormNativeSmokeRunner.cs:1068-1070` 使用真实 Watcher 卡牌 key；后续需继续核对 ModelDb、run、room、card、action 和最终状态控制流。

## 进行中

- 正在逐项审查 action failure/status、final status 合取、JSON 写入失败传播、所有 deferred/main-thread/process-frame/startup/action/quit detached operation 的 bounded drain 与场景阻断。

## 未知

- 真实目标二进制、Godot deferred 调度、真实 Watcher 卡牌自然进入形态、三场景出牌、退出码与 JSON 时序均未验证。

## 已确认

### 检查面 3：action failure latch 与 cardPlay.status 一致性

静态结论：PASS。

- action completion timeout、取消、异常路径在 `FormNativeSmokeRunner.cs:575-623` 均先设置 `actionFailureLatched=true`，并通过 `TryCancelActionAsync` 或 evidence 写入 `currentActionResult["failure"]`。
- `actionPassed` 的主线程评估在 `FormNativeSmokeRunner.cs:635-643` 同时要求未锁存、无 failure、`State == Finished`、无 exception、`CompletionTask` 正常完成且本轮无新增 `UnobservedFault`；评估调用自身失败时 `FormNativeSmokeRunner.cs:645-653` 也不可逆锁存并写 failure。
- `actionPassed == false` 的统一分支在 `FormNativeSmokeRunner.cs:677-694` 再次锁存、补齐 failure、调用 `RecordActionEvidenceAsync`，并把外层 `result.status/failure` 设为 failed；最终 `finally` 在 `FormNativeSmokeRunner.cs:730-756` 再做一次 evidence 收口，evidence 失败也将 action 和外层结果改为 failed。
- action 失败、超时或 terminal failure 会在 `FormNativeSmokeRunner.cs:375-379` 阻断后续场景；不是只记录日志后继续。

### 检查面 4：cardPlay.status 由真实 action 终态派生

静态结论：PASS。

- `CreateActionResult` 在 `FormNativeSmokeRunner.cs:863-876` 的初始状态是 `pending`，未发现固定 `enqueued`。
- `RecordActionEvidence` 在 `FormNativeSmokeRunner.cs:931-955` 每次读取真实 `State`、`CompletionTask`、取消状态、exception 和 failure，并调用 `DeriveActionStatus`。
- `DeriveActionStatus` 在 `FormNativeSmokeRunner.cs:976-1010` 区分 timeout、cancelled、faulted、pending、completed 和 failed；成功还要求 `Finished + RanToCompletion + failure == null`。因此嵌套 action evidence 不再与外层场景状态脱节。

### 检查面 5：final status 与业务 exitCode、quit outcome 的合取

静态结论：PASS，保留实机退出时序未知边界。

- `RunAsync` 在 `FormNativeSmokeRunner.cs:311-382` 以 scenario status 和 terminal failure 形成 `sharedFailure`，业务失败返回非零。
- `RunAndQuitAsync` 在 `FormNativeSmokeRunner.cs:172-192` 明确要求 `exitCode == 0`、`quitStatus` 以 `executed` 开头、`quitDrainSettled == true` 三者同时满足才写 `finalPayload["status"] = "completed"`；业务失败不会因 `SceneTree.Quit` 调用成功而变成 completed。
- quit gate 与 quit detached drain 的证据在 `FormNativeSmokeRunner.cs:144-170`、`1345-1367`。

### 检查面 6：JSON 写入失败传播

静态结论：PASS（静态传播路径）；文件系统和进程退出时序仍未知。

- `WriteJsonIfConfigured` 在 `FormNativeSmokeRunner.cs:1588-1623` 对未配置、非 G:、目录创建、序列化和写入异常均返回失败结果并记录日志。
- scenario 写入由 `TryWriteScenarioEvidence` 在 `FormNativeSmokeRunner.cs:1559-1581` 改写失败状态、设置 `terminalFailure/evidenceWriteFailure/failure`，重试后仍返回 false；`RunAsync` 在 `FormNativeSmokeRunner.cs:366-382` 将其转为 `sharedFailure` 和非零返回并阻止后续场景。
- startup、bridge、invalid request 分支也检查 scenario evidence 写入结果并返回非零（`FormNativeSmokeRunner.cs:215-232`、`238-282`、`285-308`）。
- final 写入失败在 `FormNativeSmokeRunner.cs:194-206` 改写 final payload 为 failed、记录 failure、记录日志并执行一次重试。由于 final write 位于已调用 `SceneTree.Quit` 之后，源码不能把随后发现的文件写入失败回写到已传入 Godot 的 OS exit code；该限制已被显式保留，不能视为实机通过。

### 检查面 7：deferred main-thread gate、AwaitProcessFrame、startup/action/cancel/evidence、bounded drain 与 cleanup/场景阻断

静态结论：PASS（仅控制流证据）。

- `InvokeOnMainThreadWithTimeoutAsync` 在 `FormNativeSmokeRunner.cs:1326-1343` 对 deferred completion timeout 创建并登记 `DetachedOperation`，调用 `ObserveDetachedTask`，没有声称可取消底层 callback/task。
- `AwaitOperationWithTimeoutAsync` 在 `FormNativeSmokeRunner.cs:1424-1472` 对 startup、run、room、card injection、action completion 的 timeout/fault 路径登记 detached operation；证据字段 `underlyingCancellationRequested=false` 在 `FormNativeSmokeRunner.cs:1545-1552` 明确写出。
- `WaitForConditionWithTimeoutAsync` 在 `FormNativeSmokeRunner.cs:1244-1288` 将 process-frame timeout 纳入传入的 `terminalOperations`；所有三个场景条件等待在 `FormNativeSmokeRunner.cs:465-509` 传入该列表。
- startup bounded drain 在 `FormNativeSmokeRunner.cs:235-282`；scenario setup drain 在 `FormNativeSmokeRunner.cs:315-365`；scenario terminal drain、cleanup skipped 和 final cleanup gate drain 在 `FormNativeSmokeRunner.cs:759-848`；quit drain 在 `FormNativeSmokeRunner.cs:152-170`。未收束时写入隔离结果并禁止 cleanup，terminal failure 也阻断后续场景。
- 逐个回读结果：`InvokeOnMainThreadWithTimeoutAsync` 定义及全部调用均带对应 detached list；`WaitWithTimeoutAsync` 的 startup 调用带 `startupOperations`；三个 `WaitForConditionWithTimeoutAsync` 场景调用带 `terminalOperations`；`WriteJsonIfConfigured` 的五个业务调用均读取 `JsonWriteResult`。未发现 r14 所述漏传签名残留。

### 检查面 8：普通启动、真实 Watcher 链与禁止注入回归

静态结论：PASS。

- 普通启动在 `FormNativeSmokeRunner.cs:47-60` 无 smoke 参数时直接返回；入口 `FormNativeSmokePatch.cs:7-14` 仍只有 `NGame._Ready` postfix 调用 runner。
- 真实链在 `FormNativeSmokeRunner.cs:1048-1075` 从 `ModelDb.AllCharacters`、`ModelDb.AllCards` 查找 Watcher 与三张真实卡牌；`FormNativeSmokeRunner.cs:447-496` 使用 `StartNewSingleplayerRun`、`FormStanceModifier`、`EnterRoomDebug`；`FormNativeSmokeRunner.cs:524-573` 使用真实 `CreateCard`、`CardPileCmd.Add`、`PlayCardAction`、`RequestEnqueue`、`CompletionTask`。
- r13 精确 gate 未回退：`FormNativeSmokeRunner.cs:1084-1104` 保留每场景 native kind、carrier、两个 effect 类型；`1111-1174` 要求 `FormStanceMode.IsSelected`、native stance 匹配、carrier 与两个 effect 全部存在。
- 对两个白名单文件静态检索未发现 `--autoslay`、`TestMode`、`NGame.Quit`、`PowerCmd.Apply`、`ApplyForm`、`Task.Run` 或直接构造/应用形态 Power。

## 进行中

- 指定源码审查面已完成；没有待继续的静态检查面。
- 等待后续中央流程执行 Release 构建、测试副本隔离部署和真实三场景 smoke；本轮明确不执行这些操作。

## 未知

- 未构建，不能确认当前源码可编译、依赖引用和生成物完整。
- 未启动真实游戏，未知 `Callable.CallDeferred`、`AwaitProcessFrame`、run/room/card/action completion、取消、cleanup、quit 和 JSON 文件落盘的真实时序。
- 未验证三张真实 Watcher 卡牌在目标版本中实际进入预期 native stance、carrier 和两个 effect；r13 gate 仅通过源码静态核对。
- 未验证 final JSON 写入失败发生在 `SceneTree.Quit` 前后的具体进程时序；源码已传播到内存 payload 和日志，但不能声称文件已落盘。

## 结论

状态：**PASS（仅限本轮静态监督审查）**。

r15 已静态满足请求的七项检查：r13 精确形态 gate 未回退；真实 Watcher ModelDb/run/room/card/action 链未被注入替代；action failure latch 与 cardPlay 终态同源；final status 合取业务 exitCode、quit outcome 和 quit drain；JSON 写入失败可传播到场景/RunAsync/final payload/log；deferred gate、process-frame、startup/action/evidence/cancel 和 quit 均有 detached registration 与 bounded drain；普通启动路径保持不变。

该 PASS 不是构建或实机 PASS。姿态形态 Mod 仍不能仅凭本报告宣称最终可玩，必须由主会话继续做中央构建、测试副本部署和真实战斗 smoke。
