# 姿态形态真实战斗 smoke gate 监督审查报告 - r13 - 2026-10-01

## 元数据

- 角色: 监督审查员
- 实现代理: `01a0f7ca-5741-7d20-a179-483daf549a56`
- 用户指定模型: `6.1sol`
- 用户指定路由: `agentrouter`
- 项目根目录: `G:\omp works\Sts\sts2-spire1`
- 实现报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-state-gate-worker-r13-20261001.md`

## 已确认

- 2026-10-01: 已读取并执行请求文件 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\review-form-state-gate-r13-request.txt`。
- 2026-10-01: 已落盘本轮审查范围、禁止操作和完成门禁。

## 进行中

- 等待实现代理通过 hub wait 返回 `completed`。

## 未知

- 实现代理是否已完成。
- `FormNativeSmokeRunner.cs` 最终场景 gate、action failure latch、outer status 和 JSON evidence 的最终状态。

## 已确认

- 已通过 hub `wait_threads` 收到实现代理 `01a0f7ca-5741-7d20-a179-483daf549a56` 的 `completed`; 返回状态为 `idle`, turn 状态为 `completed`。
- 等待门禁已解除; 下一步先读取实现报告, 然后才读取最终代码和落盘差异。

## 进行中

- 正在读取实现代理报告, 尚未开始最终代码审查。

## 未知

- 实现代理报告中的具体源码证据、gate 控制流和未验证边界。

## 已确认

- 已读取实现报告 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-state-gate-worker-r13-20261001.md`。
- 报告明确将本轮唯一产品变更限定为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`; `FormNativeSmokePatch.cs` 未修改。

## 进行中

- 开始读取 `FormNativeSmokeRunner.cs` 最终代码、产品 diff 和报告所引用的真实类型定义。

## 未知

- 最终代码中的精确 gate、锁存、outer status 和 JSON evidence 是否与报告一致, 尚待源码核对。

## 已确认

### 检查面 1: 真实形态成功 gate

- 优先级: P1; 本检查面静态结论: PASS。
- 证据: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:773-800` 为 calm/wrath/divinity 建立 `FormStanceKind`、精确 carrier 类型和两个精确 effect 类型映射。
- `:873-900` 的 `Snapshot` 读取 `FormStanceMode.IsSelected(player)`, `FormStanceWatcherBridge.CurrentKind(player)`, 精确 carrier 类型集合和 effect 类型集合。
- `:802-869` 的 `BuildFormGateEvidence` 使用完整类型名和 `StringComparison.Ordinal` 精确比较, gate 同时要求 `formModeSelected`, 预期 native stance, 预期 carrier, 第一 effect 和第二 effect 全部存在; 不再依赖宽泛的 `Form` 字符串命中作为成功条件。
- `:378-382` 写入 `before` 与 `formGateBefore`; `:481-501` 在 action 成功后重新读取 `after` 并只在 form gate 通过时写入场景 `status=passed`。形态 gate 失败会写 `formGateFailureLatched=true` 和失败原因。
- 映射契约交叉证据: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:100-113` 的 `CreateEffects()` 与 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:20-21,38-45` 的真实选择/native kind 语义与 runner 映射一致。

### 检查面 2: 真实 action 链、普通启动路径和禁止注入

- 优先级: P1; 本检查面静态结论: PASS。
- `FormNativeSmokeRunner.cs:384-419` 仍使用真实 `CreateCard`, `CardPileCmd.Add`, `PlayCardAction` 和 `ActionQueueSynchronizer.RequestEnqueue`; 未发现直接 `PowerCmd.Apply`、`ApplyForm`、`TestMode`、`autoslay` 或伪造 Hook 的代码路径。
- `FormNativeSmokePatch.cs:7-13` 只在 `NGame._Ready` postfix 调用现有 runner; `FormNativeSmokeRunner.cs:47-60` 先检查命令行请求, 普通启动无 `--form-native-smoke*` 时在 `:50-53` 直接返回, 未改变普通启动路径。
- 本检查面静态扫描命令: 对上述两个文件检索 `PowerCmd\.Apply|ApplyForm|TestMode|autoslay|HarmonyPatch|Hook|new\s+.*Power`; 唯一匹配为既有 `FormNativeSmokePatch.cs:7` 的 Harmony patch 声明, 未发现 Power 直接注入或伪造 Hook。

## 进行中

- 形态 gate 和真实入口检查已完成。
- 正在审查 action failure latch、scenario/outer status 和 JSON evidence 一致性。

## 未知

- 上述 PASS 仅为源码静态证据;未构建、未测试、未启动目标游戏,未证明目标发行版真实 Watcher 卡 action 后的 native stance/carrier/effect 时序。

## 已确认

### 检查面 3: action failure latch 与 cardPlay evidence

- 结论: REWORK。
- 优先级: P1。
- 触发条件: action 没有抛出异常,但 `currentAction.State != GameActionState.Finished`, 或新增 `TaskHelper.UnobservedFault` 使 `actionPassed=false`。
- 证据: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:463-470` 将这些条件判为 action 失败; `:503-508` 只把场景 `result` 写为 failed, 没有设置 `actionFailureLatched=true`, 也没有把失败原因写入 `currentActionResult["failure"]`。
- 当前后果: `:570-588` 最终可写出 `cardPlay.failureLatched=false` 且 `cardPlay.failure=null`, 同时外层场景 `status=failed`; 已记录的 timeout/cancel/exception 分支虽在 `:427-451,529-533` 设置锁存, 但非异常失败分支不具备同样的不可逆证据。
- 最小修复范围: `:463-508,570-588`; 所有 `actionPassed=false` 分支统一锁存并写入同一份 action failure/evidence。未自行修改。
- 缺失证据: 未运行目标游戏, 未生成真实非异常 action failure JSON;上述缺陷由源码控制流直接确认。

### 检查面 4: action JSON status 与实际 action outcome

- 结论: REWORK。
- 优先级: P1。
- 触发条件: 任一 action 完成、取消、抛错或超时并进入 JSON evidence 写回。
- 证据: `FormNativeSmokeRunner.cs:628-641` 初始化 `cardPlay["status"]` 为 `"enqueued"`; `:692-708` 的 `RecordActionEvidence` 更新 state、completionTaskOutcome、cancelled、exception、completedUtc 和 failure, 但从未更新 `status`。
- 当前后果: 即使 `:463-469` 判定 action 完成并且 `:489-493` 把场景写为 `status=passed`, 嵌套 `cardPlay.status` 仍为 `enqueued`;失败路径同样无法由该字段表达终态, JSON evidence 与场景状态不一致。
- 最小修复范围: `:628-708`; 让 action status 与同一份 state/completion evidence 派生并在每次记录时更新。未自行修改。
- 缺失证据: 未运行目标游戏或读取真实 JSON;字段未写回是当前源码事实。

## 进行中

- 已确认 action failure latch 和嵌套 action JSON 存在静态一致性缺陷。
- 正在检查 scenario outer status、final status 和 JSON 写入失败传播。

## 未知

- 未知仅限目标二进制中的实际 action 时序和序列化落盘时序;不影响上述源码级 REWORK 结论。

## 已确认

### 检查面 5: scenario outer status 与 final status

- 结论: REWORK。
- 优先级: P1。
- 已确认的局部行为: `FormNativeSmokeRunner.cs:243-254` 会把任一 scenario 的非 `passed` 结果保存在 `sharedFailure`, 并以 `return sharedFailure == null ? 0 : 1` 传播业务 exit code; 这一层没有因本轮 form gate 增加而丢失失败。
- 缺陷触发条件: 任一 startup、bridge、scenario 或 invalid request 业务失败, 但最终 `SceneTree.Quit` 调用成功。
- 证据: `:130-152` 的 `RunAndQuitAsync` 将 `exitCode` 传入 `QuitOnMainThreadAsync`, 但 `final` JSON 的 `status` 只依据 `quitStatus.StartsWith("executed")` 写成 `"completed"`;它不检查 `exitCode`。
- 当前后果: `final.status="completed"` 可以与 `final.exitCode=1` 或 `2` 同时出现, 外层退出成功被错误表达为业务 smoke 成功。
- 最小修复范围: `:145-152`; final status 必须同时反映业务 exitCode 和 quit outcome, 并保留失败摘要。未自行修改。
- 缺失证据: 未启动目标游戏;上述冲突由源码控制流直接确认,不依赖实机时序。

### 检查面 6: JSON evidence 写入失败传播

- 结论: REWORK。
- 优先级: P1。
- 触发条件: `SPIRE1_FORM_SMOKE_REPORT` 未配置、解析后不在 G:、目录创建失败、序列化失败或文件写入失败。
- 证据: `FormNativeSmokeRunner.cs:1218-1245` 的 `WriteJsonIfConfigured` 在未配置、路径拒绝和 catch 分支只 return 或记录日志, 返回类型为 `void`; `RunAsync:242` 和 `RunAndQuitAsync:145-152` 没有获得写入结果, 也不会据此改变 scenario/final status 或 exitCode。
- 当前后果: 可能在没有 scenario/final JSON evidence 的情况下保留业务 exitCode=0,并由 `:147-149` 写出 `final.status="completed"`;这使成功结论缺少其规定的证据面。
- 最小修复范围: `:1218-1245` 及其调用点 `:242,145-152`; 将 evidence 写入结果纳入不可逆失败和 outer status。未自行修改。
- 缺失证据: 未运行目标游戏,未操作报告目录权限;源码已足以确认错误传播缺口。

## 进行中

- 已完成指定的 form gate、禁止注入、action latch、outer status 和 JSON evidence 静态检查。
- 正在汇总最终 REWORK 结论、最小修复范围和未验证边界。

## 未知

- 目标游戏中的真实 action、退出和文件系统时序未验证;不把这些未运行路径写成实机 PASS。

## 已确认

### 检查面 7: 变更归属证据边界

- 实现报告 `form-native-state-gate-worker-r13-20261001.md:实现完成` 声明唯一产品修改为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`, `FormNativeSmokePatch.cs` 保持不变。
- 当前工作树对这两个路径均显示 `??`; 目标文件未被 Git 跟踪, 因而 `git diff` 不能提供可独立归因的前后差异。监督者未修改任何产品文件。
- 结论: 代码审查依据为实现报告、最终落盘源码和真实行号;不把当前脏工作树当作完整 baseline,不声称已完成全仓变更归因。

## 进行中

- 无待检查的指定源码面。

## 未知

- 当前未跟踪工作树缺少本轮开始时的全仓快照;实现者报告是唯一的变更归属记录。


## 已确认

### 最终结论

- 状态: `REWORK`。
- 静态 PASS 面: `FormStanceMode.IsSelected`、预期 native `FormStanceKind`、精确 carrier、两个精确 effect 类型已共同进入 after-form gate;真实 card/action API 链、普通启动命令行 gate、无直接 Power 注入和无伪造 Hook 均保持。
- 必须返工的 P1 面:
  1. `FormNativeSmokeRunner.cs:463-508,570-588`: 非异常 action 失败或新增 unobserved fault 未统一设置 `actionFailureLatched` 和 `cardPlay.failure`。
  2. `:628-708`: `cardPlay.status` 固定为 `enqueued`, 与 action 终态和 scenario 状态不一致。
  3. `:145-152`: final JSON status 忽略业务 `exitCode`, 业务失败但退出调用成功时仍可写 `completed`。
  4. `:1218-1245` 及调用点 `:242,145-152`: JSON evidence 写入失败不传播到 scenario、final status 或 exitCode。
- 最小修复只涉及上述 `FormNativeSmokeRunner.cs` 范围;本监督没有自行修复。

## 进行中

- 无。

## 未知

- 未构建、未测试、未部署、未启动游戏;未验证目标发行版中真实 Watcher 卡、native stance、carrier/effect 时序、退出和 JSON 落盘。
- 本报告不把任何源码静态 PASS 写成实机 PASS;实际模型/provider session 元数据未在本轮独立读取,仅记录用户指定的 `6.1sol via agentrouter`。
