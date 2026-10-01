# form-native-smoke nested task fix worker report

- 检查时间: 2026-10-01
- 模型: 6.1sol
- provider 路由: agentrouter
- harness: 当前会话原生设施；未委派、未启动其它代理运行时
- 唯一产品文件: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs
- 本轮限制: 只做源码修改和静态回读；不构建、不运行测试、不部署、不启动游戏、不修改共享配置

## 已确认

### P1: gate 超时登记的是 nested task 外层

- 触发条件: `InvokeOnMainThreadWithTimeoutAsync<T>` 的 `T` 为 `Task`，且 deferred callback 在 `MainThreadGateSeconds` 内没有完成。
- 证据文件与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1304-1323` 的 `TaskCompletionSource<T>` 将 `operation()` 的结果写入外层 completion；`T == Task` 时形成 `Task<Task>`。
- 当前控制流: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1331-1340` 在 gate 超时后把 `invocation` 外层任务直接放入 `DetachedOperation`；`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1519-1525` 只等待登记任务。
- 结论: deferred callback 迟到后，即使 callback 返回的底层游戏 `Task` 仍未完成，外层 `Task<Task>` 也可能先完成；现有 drain 会错误地将 detached operation 记为收束，可能提前允许 cleanup、后续场景或最终通过。
- 受影响调用证据: `:447-463` start run、`:483-496` enter room、`:529-538` card injection、`:1267-1286` process frame submission 均通过该泛型 gate。
- 最小修复方向: gate 超时登记时将 `Task<Task>` 展开为等待外层 callback completion 以及其返回的底层 `Task` 的单一 drain task；普通成功路径仍返回原有 `T`，不改变既有 bounded timeout、failure latch、cleanup gate 和最终 quit 控制流。
- 尚缺证据: 按请求不构建、不测试、不运行游戏；本结论为源码控制流证据，不是实机复现。

### 检查面：detached drain 与 cleanup 语义

- 已核对全部 `DetachedOperation` 创建点: `:1277-1279` 仅登记已经获得的 process-frame task；`:1337-1339` 登记 main-thread gate completion；`:1450-1453` 登记普通底层 operation timeout。只有 `:1337` 的输入可能是 `Task<Task>`，因此修复可局限在 gate 超时登记路径或 `DetachedOperation` 的统一 drain-task 边界。
- `:759-847` 的 terminal cleanup gate、`:152-171` 的 final quit drain，以及 `:1519-1525` 的统一 drain 都只依赖 `DetachedOperation.Task`；只要该属性改为等待 outer callback 和 inner operation 的展开任务，现有 failure latch、cleanup skip、JSON evidence 和 quit status 分支无需改动。
- 现有 `using System.Threading.Tasks;` 和目标框架 `net9.0` 提供 `Task<Task>.Unwrap()`；静态契约上可用最小展开修复，不需要新测试旁路、Power 注入或入口修改。
- 未发现其它产品文件或调用路径需要写入；唯一代码白名单仍为 `FormNativeSmokeRunner.cs`。
### 修改与静态回读

- 已修改唯一产品文件 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`。
- `:1391-1395` 的 `DetachedOperation` 构造函数现在把运行时类型为 `Task<Task>` 的输入转换为 `nestedTask.Unwrap()`；因此 drain task 只有在 deferred callback 的 outer completion 和 callback 返回的 inner operation 均完成、取消或失败后才收束。
- `:1337-1339` 的 gate 超时登记、`:1519-1525` 的 bounded drain、`:759-847` 的 cleanup gate、`:152-171` 的 final quit drain 均保持原控制流，只消费已规范化的 `DetachedOperation.Task`。
- 静态回读确认源文件仍为 UTF-8 无 BOM、LF 行尾；`Task<Task>` 与 `.Unwrap()` 各出现 1 处；限定路径的 git 状态只显示白名单产品文件和本报告文件。
- 未构建、未运行测试、未部署、未启动游戏；没有把静态回读写成运行通过。
## 进行中

- 已完成源码修改与静态回读；没有其它待执行源码面。
- 产品代码修改已完成；没有新增测试旁路、Power 注入或游戏入口改动。

## 未知

- 修改后的静态回读已完成；目标代码路径与报告路径均在本轮白名单内。
- 无本轮构建、测试、部署或游戏运行证据；不能据此宣称运行通过。
