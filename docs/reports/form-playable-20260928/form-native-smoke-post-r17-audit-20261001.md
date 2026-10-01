# Form Native Smoke post-r17 静态审查

- 检查时间: 2026-10-02T03:26:29.5929847+08:00
- 实际模型: 6.1sol（按本轮用户指定的当前继承模型解析）
- provider 路由: agentrouter（未回退、未换路由）
- harness: Codex 原生当前会话；本轮未委派子代理、未启动其它代理运行时
- 检查范围:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs
  - 相对最近 r17 实现批次的 git 状态/diff 可得性及对应报告
- 已读取的约束入口: G:\omp works\AGENTS.md, G:\omp works\START-HERE.md, G:\omp works\docs\WORKSPACE-PROJECTS.md, G:\omp works\Sts\sts2-spire1\DEVELOP.md, G:\omp works\Sts\sts2-spire1\DEVLOG.md。未找到独立的 G:\omp works\Sts\AGENTS.md 或 G:\omp works\Sts\sts2-spire1\AGENTS.md。
- 未执行的运行命令: 未构建、未运行测试、未部署、未启动或停止游戏、未修改产品代码/构建产物/共享配置；以下结论均为静态源码、git 元数据和本地报告回读。

## 已确认(有证据)

### 初始检查面：入口与当前工作树证据

1. 两个目标文件均存在。G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs:7-14 只有 NGame._Ready 的 Harmony postfix，并调用 FormNativeSmokeRunner.TryStart(__instance)；G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:47-60 先解析命令行，未请求 smoke 时直接返回，已请求时用 TaskHelper.RunSafely(RunAndQuitAsync(...)) 启动流程。当前静态证据未显示普通启动路径被强制改为 smoke。
2. 当前仓库 git status --short --branch 显示目标路径仍是未跟踪批次：FormNativeSmokePatch.cs 为 ??，FormNativeSmokeRunner.cs 隶属于未跟踪的 mod\Spire1Code\Run\。因此 HEAD 与该批次之间没有可直接读取的普通 git diff；r17 的变更边界必须由当前文件、对应 r17 报告和可取得的历史基线共同复核，不能把当前源码自动称为已提交 r17 diff。

### 检查面：返回 Task 的主线程 gate 超时只 drain 外层 completion task

- **[P1] 已确认的静态缺陷。** 触发条件是某个 Func<Task> 主线程操作在 deferred callback 执行前超过 MainThreadGateSeconds：G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1304-1317 用 TaskCompletionSource<T> 保存 operation() 的结果；当 T 为 Task 时，保存的是“返回的底层 Task”作为外层 completion 的结果。随后 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1331-1340 在 gate 超时后登记的是 invocation 外层任务，而不是 callback 迟到后返回的底层 Task。G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1519-1525 的 drain 以 Task 基类等待该登记项，只能等待外层 completion task，不会自动等待嵌套的底层 run/room/card/process-frame task。
- 受影响调用点包括：startup 的 WaitWithTimeoutAsync（G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1232-1241）、StartNewSingleplayerRun（:447-463）、EnterRoomDebug（:483-496）、CardPileCmd.Add（:529-538）以及 AwaitProcessFrame 提交（:1267-1286）。若 callback 迟到后返回的底层任务仍 pending，外层 TCS 会先以“成功返回一个 Task”收束，DrainDetachedOperationsAsync 可能据此允许 RunManager.CleanUp 或最终退出；实际底层 operation 仍未被观察或收束。
- 这与 G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-review-r15-20261002.md:68-76 和 G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-final-review-20261002.md:68-76 所称“所有 deferred/main-thread/process-frame 调用均有 detached registration 与 bounded drain”不完全相符：当前确实登记了 gate completion，但没有登记/等待 gate callback 产生的嵌套 operation。
- **可复核命令（本轮未执行）:** $env:SPIRE1_FORM_SMOKE_REPORT='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\audit-repro-p1'; & 'E:\Slay the Spire 2\SlayTheSpire2.exe' --rendering-driver opengl3 --form-native-smoke=calm；要实际触发该缺陷，还需在目标运行中制造超过 10 秒的主线程 deferred gate 延迟，然后核对 scenario JSON 的 detachedOperations、cleanup 与迟到 callback 的日志时序。该命令和诱发步骤均未执行。
- **最小修复范围:** 对 Func<Task>/Func<Task<T>> 的 gate 单独保留并展开 callback 返回的底层任务；drain 必须同时等待 callback completion 与其返回的 operation，或在 callback 尚未执行时把返回任务纳入后续 drain；在二者均未收束前保持 terminalFailure 并禁止 cleanup/后续场景/最终退出。
- **尚缺实机证据:** 本轮未构建、未启动游戏、未人为制造主线程停顿；以上是由泛型嵌套任务和 Task 基类等待直接推出的源码控制流缺陷，不是实机复现。
### 检查面：AwaitProcessFrame 取消/异常被降级为可继续场景失败

- **[P1] 已确认的静态缺陷。** 权威本地发行版源码 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes.GodotExtensions\NodeUtil.cs:11-30 明确说明 AwaitProcessFrame 在 token 取消或节点离开 scene tree 时抛出 OperationCanceledException/TaskCanceledException。当前 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1267-1286 在 await frameTask 时没有把该异常包装成 TerminalOperationException，也没有把已完成但取消/故障的 frame task 写入 terminalOperations。
- 该异常落到 RunScenarioAsync 的通用 catch（G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:707-725）。这里只有 MainThreadGateException 或 TimeoutException 会设置 result["terminalFailure"] = true；OperationCanceledException、TaskCanceledException 或其它 frame 异常只设置 status=failed。随后 RunAsync（:371-379）虽然记录 sharedFailure，但只有看到 terminalFailure=true 才阻断后续场景。因此在节点退出/进程帧任务取消时，代码可先清理当前 run，再继续下一个场景；这不满足终止性 frame failure 的隔离语义。
- **静态复核命令（已执行，只读）:** `$p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; Get-Content -LiteralPath $p | Select-Object -Index (1266..1286); Get-Content -LiteralPath $p | Select-Object -Index (706..714)`。**运行时触发条件（未执行）:** AwaitProcessFrame 等待期间 NGame/其节点离开 scene tree，随后出现 TaskCanceledException；按 NodeUtil.cs:16-30 这是明确支持的异常路径，不是臆测。
- **最小修复范围:** 将 frame task 的取消/异常转为带 terminalFailure 的终止性异常并写入 detached/frame evidence；若 task 尚 pending，继续纳入 bounded drain；在任何 frame wait failure 后跳过 cleanup 或至少阻断后续场景，直到原 task 和主线程 gate 都达到可记录终态。
- **尚缺实机证据:** 未构造节点退出的真实运行时序，未构建、未启动游戏；但异常来源和当前 catch/loop 分支均有本地源码证据。
### 检查面：final JSON 在 SceneTree.Quit 之后写入，存在退出竞态

- **[P1] 已确认的证据完整性缺陷。** RunAndQuitAsync 在 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:141-145 先调用 QuitOnMainThreadAsync；该方法 :1352-1359 在主线程 gate 内执行 game.GetTree().Quit(exitCode)。随后才在 :145-194 组装 finalPayload 并首次调用 WriteJsonIfConfigured("final", finalPayload)。本地 GodotSharp 权威 XML G:\omp works\Sts\sts2-spire1\.nuget\godotsharp\4.5.1\lib\net8.0\GodotSharp.xml:208590-208593 说明 SceneTree.Quit 会在当前 iteration 结束时退出，而源码没有等待最终 JSON 写入完成的 barrier。
- 触发条件是 final quit gate 成功后主线程完成当前 iteration、进程退出速度先于 worker continuation 的目录创建/序列化/写文件完成，或写入中途进程被退出。结果可能是 final.json 缺失/截断，或者首次写入失败后的 retry（:194-206）根本没有运行；同时 exitCode 已传给 Godot，后续发现的 evidence failure 不能回写进程退出码。既有报告把该时序列为“未知”，但当前顺序本身已足以证明没有证据落盘先于退出的静态保证。
- **可复核命令（未执行）:** $env:SPIRE1_FORM_SMOKE_REPORT='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\audit-final-race'; & 'E:\Slay the Spire 2\SlayTheSpire2.exe' --rendering-driver opengl3 --form-native-smoke=calm；运行后需在进程退出瞬间核对 final.json 是否存在、可解析、包含 quitStatus/quitDrainSettled，并与日志时间戳比较。本轮按请求未启动目标游戏。
- **最小修复范围:** 将最终 evidence 的序列化、写入和必要的持久化确认放到 SceneTree.Quit 调用之前，或把 quit 请求延后到最终 JSON 写入完成之后；若仍需记录 quit outcome，至少先写一个不依赖 post-quit 的最终判定/意图文件，并明确把 post-quit 退出状态作为未观测字段，而不是依赖退出后 retry。
- **尚缺实机证据:** 未执行进程退出竞态复现，不能把“已丢失 final.json”称为实机事实；本项结论是 Godot API 时序与当前调用顺序的静态证据。
### 检查面：condition timeout 没有覆盖每次主线程 gate 的剩余预算

- **[P2] 已确认的静态缺陷。** WaitForConditionWithTimeoutAsync 在 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1251-1255 只把总 deadline 用于循环条件；InvokeOnMainThreadWithTimeoutAsync 在 :1331-1334 每次固定等待 MainThreadGateSeconds（10 秒），没有接收调用方的 remaining。G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1257-1265 只在 condition gate 返回后检查 deadline；若 condition gate 在 deadline 之后才返回 true，while (!true) 会直接退出并报告成功，代码没有再验证总预算。
- 同一问题出现在 :1267-1286：remaining 在 process-frame gate 之前计算，但 :1268-1271 的 gate 仍可独立等待 10 秒，之后才用旧的 remaining 创建 Task.Delay。因此单次条件等待可超过声明的 120 秒，且在迟到 frame 恰好完成时存在越过 deadline 仍返回成功的控制流。
- 该缺口在既有 G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-flow-review-r12-20261002.md:17 已被指出为“每次 gate 独立延长 10 秒”，但 r15/r17 代码仍保留同一结构；相对 r17 的两行 Environment 修复没有触及它。
- **静态复核命令（已执行，只读）:** `$p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; Get-Content -LiteralPath $p | Select-Object -Index (1243..1287); Get-Content -LiteralPath $p | Select-Object -Index (1325..1342)`。**运行时触发条件（未执行）:** 在某次 condition/frame gate 开始前让剩余预算小于 10 秒，并让 deferred callback 延迟到 deadline 之后。
- **最小修复范围:** 让 gate 接收本次调用的剩余时间并使用 min(remaining, MainThreadGateSeconds)；每次 gate 返回后再次检查 deadline，禁止在总预算过期后接受条件成功；超时/迟到操作继续进入同一 detached drain。
- **尚缺实机证据:** 未人为制造主线程延迟，未构建或运行游戏；本项是时间预算算术和调用参数的静态结论。
### 已检查且未发现新增旁路的检查面

- **action failure latch / cardPlay.status:** G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:575-623 的 timeout、取消、异常路径均锁存 actionFailureLatched；:635-653 的成功判定要求无锁存、无 failure、Finished、无 exception、CompletionTask 为 RanToCompletion 且无新增 UnobservedFault；:677-694 对普通不变量失败再次锁存并补齐 failure；:931-1010 从真实 action 状态派生 status。未发现 action 失败后可恢复为 passed 的静态路径。
- **cleanup gate / 场景阻断（不含前述嵌套 Task 缺陷）:** G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:759-789 在 terminal drain 未收束时跳过 cleanup 并写 cleanupSkipped；:793-848 通过主线程 gate 执行最终 cleanup，gate 失败后对新增 detached 项执行独立 drain。该结构本身覆盖 cleanup gate，但其安全性仍受“返回 Task 的 gate 只等待外层 completion”问题影响。
- **normal startup 与禁止注入:** G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs:7-14 只有 _Ready postfix；G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:47-60 在没有支持参数时返回，不创建 smoke task。当前两个白名单文件未发现 Task.Run、--autoslay、TestMode、PowerCmd.Apply、直接构造/应用形态 Power 或 NGame.Quit 旁路。
- **真实链路:** G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1048-1075 从 ModelDb 查真实 Watcher 与三张卡；:447-496 使用真实单人 run、FormStanceModifier、EnterRoomDebug；:524-573 使用真实 CreateCard、CardPileCmd.Add、PlayCardAction、RequestEnqueue 和 CompletionTask。当前源码没有把形态 gate 建立在直接注入 Power 或伪造 Hook 上。

### 本轮结论

- **状态: REWORK（仅限静态审查）。** r17 的 `System.Environment` 两处显式限定本身与对应报告一致，但当前控制流仍有 3 项 P1 和 1 项 P2：返回 `Task` 的主线程 gate 超时只收束外层 completion、`AwaitProcessFrame` 取消/异常未被标记为终止性失败、final JSON 在 `SceneTree.Quit` 之后写入存在退出竞态，以及 condition timeout 未扣除每次 gate 的固定预算。
- 以上结论全部来自 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`、`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs`、本地发行版源码/XML和对应报告；未把静态推理称为实机复现。

## 进行中(半成品, 需复核)

- 静态检查面已完成；无更多静态面待复核，剩余仅为运行时验证。产品代码没有继续修改；本轮请求明确禁止构建、测试、部署和启动游戏，因此不把运行时验证作为本报告的通过条件。

## 未知(未覆盖)

- 未构建、未运行测试、未部署、未启动或停止游戏；未知当前未跟踪源码是否可编译、目标二进制的 deferred 调度、真实三场景 action/form 状态、cleanup/quit 时序和 JSON 实际落盘结果。
- 未人为制造主线程 gate 延迟、节点离树、进程退出竞态或复用报告目录；上述触发条件均为静态可达路径，不是实机复现。
- 目标文件在当前工作树仍为未跟踪文件，`HEAD` 没有可直接读取的普通 `git diff`；r17 两行变更边界依据 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-envfix-worker-r17-20261001.md` 与监督报告核对，不能替代提交历史基线。
