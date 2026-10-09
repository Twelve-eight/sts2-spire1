# SaveGuard API Fix R12 Supervisor Report

- 状态: SUPERVISION_PASS (门禁于 2026-10-05 08:24:46 通过; 最终结论见文末)
- 监督者: 同批监督审查员
- 指定路由: global:deepseek-v4.1-flash / wb2api / xhigh
- 精确 worker id: 01a1096f-7faf-7f51-831f-ccb572f1e5de
- 唯一可写报告: 本文件
- 门禁条件: 主会话原生 wait_agent 对精确 worker 实际返回 completed, 且 gate-notice.txt 已记录
- 门禁前动作: 不读取最终产物, 不审代码, 不构建, 不测试, 不运行游戏, 不部署, 不执行 git

## 门禁前初始状态 - 已确认

- 已读取 supervisor.request.md.
- 已读取 worker.request.md.
- 白名单代码文件: G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs.
- 监督阶段唯一可写文件: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\saveguard-api-fix-r12\supervisor.md.
- 未委派任何子代理, 未启动其它 harness 或 codex exec/omp.
- 门禁前时点尚未读取 worker.md 最终产物内容, 尚未核对 diff; 门禁后审查结果见增量记录 3/4 与最终结论.

## 门禁前初始状态 - 进行中

- 等待主会话对精确 worker id 01a1096f-7faf-7f51-831f-ccb572f1e5de 的原生 wait completed 通知.
- 等待 gate-notice.txt 落盘.

## 门禁前初始状态 - 未知

- worker 最终 diff, 源码 hash 与报告诚实性.
- 本机权威 API 对 Task 等待修正是否完全匹配.
- fault/cancel/timeout 与主线程边界是否满足 worker.request.md.
- 最终 SUPERVISION_PASS 或 NEEDS_REWORK 结论.
- 编译与实机验证边界尚未由本监督者确认.
## 增量记录 1

- 时间: 2026-10-05
- 状态: WAITING_GATE (记录时点)
- 动作: 对精确 worker id 01a1096f-7faf-7f51-831f-ccb572f1e5de 执行原生 wait 快照.
- 结果: timedOut=true; worker latest turn status=inProgress; 未完成.
- 处理: 不进入监督, 不读取最终产物.
- 未知: gate-notice.txt 尚未确认存在.

## 增量记录 2

- 时间: 2026-10-05
- 动作: 对精确 worker id 01a1096f-7faf-7f51-831f-ccb572f1e5de 执行原生 wait.
- 结果: 返回 completed; worker 最终回复摘要已收到.
- 门禁检查: gate-notice.txt 仍为 MISSING.
- 处理: 按 supervisor.request.md, 无 gate-notice.txt 不进入最终产物审查; 不读取代码 diff, 不核对 worker.md 内容.
- 状态: WAITING_GATE (记录时点, gate-notice.txt 尚未落盘)
## 门禁记录

- 时间: 2026-10-05
- 门禁证据: gate-notice.txt 与 coordination.md 已落盘, 内容声明 multi_agent_v1.wait_agent 对精确 worker 01a1096f-7faf-7f51-831f-ccb572f1e5de 返回 completed, timed_out=false.
- 本监督者独立复核: 原生 wait_threads 对该精确 worker 返回 wake.reason=turnCompleted, status=completed; 之后 gate-notice.txt 出现.
- 处理: 门禁满足, 进入只读监督.

## 门禁后已确认 (源码证据)

- 唯一代码文件存在且与 worker 报告 hash 一致: G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs, SHA256=D8DC47BCDE80650352E681661A73C86B0F213553CB778CCE9F5C21E9B2EEBD82, 25147 bytes, UTF-8 无 BOM, LF-only.
- 调用点与签名匹配: 第139-142行 WaitWithTimeoutAsync(() => game.GameStartupComplete, 120s); 第501行签名为 Func<Task>.
- 本机权威 API: NGame.GameStartupComplete 为 Task.
  - G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:211714 public Task GameStartupComplete => _gameStartupComplete.Task;
  - G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:420 与 481 一致.
  - 发行版 XML G:\omp works\Sts\_runtime\sts2-test-client-B\data_sts2_windows_x86_64\sts2.xml:21474 成员存在.
- 主线程边界: 第506行通过既有 InvokeOnMainThreadAsync(operationFactory) 在主线程取得 Task; 第513-536行主线程分派与同线程短路逻辑未被改写.
- 超时与 fault/cancel: 第507行 Task.WhenAny(startup, Task.Delay(timeout)); 第508-509行仅超时抛 TimeoutException; 第510行 await startup 使 fault/cancel 真实抛出; 未使用 IsCompleted 判定.
- 白名单: 全盘查找 SaveGuardSmokeRunner.cs 仅此一份; 本轮未发现其它副本.
- 未改产品代码: 该文件位于 tests/FormsSaveGuardSmoke 下, 本次审查仅发现此一个文件的逻辑改动.

## 门禁后推进记录 (均已完成, 详见增量记录 3/4)

- 已完成: 四项检查语义核对, 位置与内容未因本次窄修改变.
- 已完成: finalPassed/exitCode/status 与 WriteJson 边界核对, 语义未改.

## 门禁后遗留未知 (不得声称通过)

- 本监督者未构建/lint/测试/运行游戏/部署/git; 编译与实机验证由中央 r10 及后续阶段负责.
- GameStartupComplete 在 GameStartupWrapper 成功与失败分支均 TrySetResult (NGame.cs:578,586,590), 因此 await 完成不等于启动成功; worker 已在报告中诚实标注该边界.
- 真实进程中 startup task 超时/fault 路径行为未验证.

## 增量记录 3 - 四项检查语义与 JSON 诚实性核对

- 四项检查位置与语义未被本次窄修触碰:
  - guard owner: 182-239 (Harmony.GetPatchInfo -> 要求 owner=Spire1 -> Prefix 签名 bool Prefix(SerializableModifier)).
  - FormStance identity: 243-342 (ModelDb.GetCategory + SPIRE1-FORM_STANCE_MODIFIER -> FromSerializable -> 精确类型或 fail-closed InvalidOperationException, 禁止降级 DeprecatedModifier).
  - vanilla roundtrip: 346-382 (同程序集 vanilla modifier -> ToMutable/ToSerializable/FromSerializable -> type 与 Id 保持).
  - unknown DeprecatedModifier: 386-417 (随机未知 id -> 必须 DeprecatedModifier, 不得拦截全部 unknown).
- finalPassed/exitCode/status: 101-135 与 174-177; 仅 exitCode==0 时 finalPassed=true/status=completed; 证据写失败会回退 exitCode=1/finalPassed=false/status=failed 并重写 (117-132).
- WriteJson 边界: 554-576, 要求 SPIRE1_FORM_SMOKE_REPORT 且路径必须位于 AllowedReportRoot=G:\omp works\.tmp\forms-independent-20261005 之下, 否则拒绝写.
- TryStart 闩锁: 43-50 Interlocked.Exchange, 重复调用直接返回; 异常经 RunGuardedAsync 落盘失败报告并非零退出 (57-84).
- 静态自检复核: Func<bool>=0, Func<Task>=1, IsCompleted=1 (仅第504行注释), 调用点 1 处, 与 worker 报告一致.
- 修改前 hash 可逆复算: 以 worker 记录的旧代码块对当前文件做逆向替换, 复算 SHA256=C0AC16FB68A47E1323545AA2614B6AD189BC28D0DA66D437745DCAE8E82323ED, 25124 bytes, 与 worker 记录及 session 落盘时实测值一致.
- 类型推导核对: InvokeOnMainThreadAsync<T>(Func<T>) 以 T=Task 匹配, await Task<Task> 解包一层得到真实 startup Task; 同线程分支 Task.FromResult 与跨线程分支 TrySetResult 行为一致, 无编译形态歧义.

## 增量记录 4 - 白名单与工具边界核对

- worker session 复核 (C:\Users\o_Obl\.codex\sessions\2026\10\05\rollout-2026-10-05T08-21-04-01a1096f-7faf-7f51-831f-ccb572f1e5de.jsonl):
  - function_call 共 32 次, 全部为 exec_command; 无 spawn_agent / create_thread / subagent 调用.
  - 写操作仅 3 处: SaveGuardSmokeRunner.cs (第131行, 唯一代码写入) 与 worker.md (第38/160/167行); 无 Copy-Item / Move-Item / Remove-Item / robocopy / git 写操作.
  - 无 codex exec / omp 等其它 harness 启动痕迹.
- 全盘仅一份 SaveGuardSmokeRunner.cs, 未发现标准 Release / Workshop / 共享配置 / Steam 路径写入.
- sts2-forms 近 1 小时变更中, SaveGuardSmokeRunner.cs 时间戳 8:23:05, 25147 bytes, 与报告一致; 其余文件变更属其它批次, 非本 worker 写入.
- 路由核对 (round8-to12-routes.json): RequestedModel=global:deepseek-v4.1-flash, RequestedRoute=wb2api, ResolvedModels=[global:deepseek-v4.1-flash], ResolvedEfforts=[xhigh], Providers=[gateway], SubagentSpawnToolCalls=0, PeerRuntimeLaunchFindings=0, 与用户指定一致.
- 诚实性保留: 该记录 ActualWb2apiSubroute=Unknown, 只能证明 gateway 路由, 不能证明 wb2api 子路由; 按诚实性要求记为未知, 不作通过依据.

## 最终结论: SUPERVISION_PASS

依据 (全部只读核对):
- 门禁: gate-notice.txt 与 coordination.md (均 416 bytes, 2026-10-05 08:24:46) 声明精确 worker 01a1096f-7faf-7f51-831f-ccb572f1e5de 返回 completed/timed_out=false; 本监督者用原生 wait 独立复核该 worker turn status=completed, 身份相符.
- 代码: G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs, SHA256=D8DC47BCDE80650352E681661A73C86B0F213553CB778CCE9F5C21E9B2EEBD82, 25147 bytes, UTF-8 无 BOM, LF-only.
- 改动最小且精确: 仅 WaitWithTimeoutAsync 签名 Func<bool>->Func<Task> 与其实现体; 逆向复算得到 C0AC16FB... 25124 bytes, 与修改前实测值一致.
- 权威 API 匹配: decompile 211714 与 engine-dllsrc NGame.cs:420,481 均为 public Task GameStartupComplete; 发行版 sts2.xml:21474 成员存在.
- 契约满足: 主线程取 Task (506), WhenAny 超时 (507), 超时抛 TimeoutException (508-509), await startup 使 fault/cancel 真实抛出 (510); 无 IsCompleted 判定.
- 四项 engine 检查 / finalPassed / exitCode / status / WriteJson / TryStart 语义与边界未改.
- 白名单与路由: 唯一代码写入文件为白名单文件; 无子代理, 无其它 harness; 模型/路由/xhigh 与用户指定一致.

## 未构建实机边界 (不得声称通过)

- 本监督者与 worker 均未构建 / lint / 测试 / 运行游戏 / 部署 / git; 编译验证留给中央 r10.
- 未验证: 该 Func<Task> 改动在真实 MSBuild + Godot.NET.Sdk 4.5.1 下的编译结果.
- 未验证: 真实进程中 startup task 的 timeout / fault / cancel 运行行为.
- 未验证: 四项 engine FromSerializable 检查在实机运行中的实际结果与 forms-save-guard-smoke.json 落盘内容.
- 已知语义边界 (worker 已诚实标注): GameStartupComplete 在 GameStartupWrapper 成功与失败分支均 TrySetResult (NGame.cs:578,586,590), 完成不等于启动成功; 本次窄修只保证 await 真实 Task 且带超时, 未增加启动成功判定.
