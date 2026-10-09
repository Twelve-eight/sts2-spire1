# powers gate fallback supervisor review - 2026-10-02

## 已确认

- 2026-10-02：实现代理 `01a0fc4c-b0c8-7c91-a4ea-6687cc68220a` 已通过原生 `wait_threads` 等待至 `turnCompleted`，状态为 `idle`；其修改已落盘。主会话本轮只读审查，不修改产品代码、不构建、不运行游戏。
- 唯一审查目标：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs`。
- 第一批源码证据：当前第 375-405 行已将单 marker 替换为 `FallbackPendingState`；第 380-400 行以 `_count` 表示同一 `PowerModel` 的待消费数量，并在 `Add` / `TryConsume` 内各自使用 `lock (_sync)`；第 405 行仍由 `ConditionalWeakTable<PowerModel, FallbackPendingState>` 持有状态。
- 第一批配对证据：当前第 429 行每次被阻断的 Before 通过 `GetValue` 取得该 key 的状态后 `Add()`；当前第 437-443 行的 Received 通过 `TryGetValue` 找到同一状态后只消费一个计数。初步结论是多次 in-flight Before 不再覆盖同一 key 的既有 pending 状态。

## 进行中

- 继续只读核对：多次 Before/Received 的并发线性化、ConditionalWeakTable 弱引用边界、未命中语义、中央 gate 与安装日志是否保持不变。

## 未知

- 尚未完成本轮全部静态检查；未对 engine 的异常、取消、重入或跨线程 callback 是否严格成对作实机验证。
- 按用户要求不进行构建和游戏运行，因此编译可接受性及真实运行时行为仍需单独验证。
### 检查面 1：多次 Before/Received 配对

## 已确认

- 文件第 375-400 行：`FallbackPendingState` 只保存 `_count`；`Add()` 在第 380-386 行把每个被阻断的 Before 计数加一，`TryConsume()` 在第 388-400 行只在 `_count > 0` 时减一并返回 `true`。
- 文件第 429 行：每次第 424-426 行判定为 `block` 的 Before，都对该 `PowerModel` 的 CWT 值执行一次 `Add()`，没有旧 marker 的 `Remove`/覆盖操作。
- 文件第 437-443 行：每次 Received 只从同一 `PowerModel` 的 CWT 值消费一个计数；无 key 或计数为零时返回 `false`。因此在通常的回调顺序下，N 次已完成的 block Before 可由 N 次 Received 一一消费，第 N+1 次 Received 不会误消费新的 token。
- 文件第 404-405 行：CWT 的 value 不持有 `PowerModel` 字段；不同 `PowerModel` 通过不同 CWT key 隔离计数。

## 进行中

- 本检查面的静态审查已完成；下一面核对弱引用和锁边界。

## 未知

- 计数只记录数量，不记录某一次 Before 与某一次 Received 的身份，也不证明乱序 callback 的业务配对；这是引擎回调契约的实机边界，不是计数覆盖问题。

### 静态风险与实机边界

- 静态风险：`GetValue(...).Add()` 是 CWT 查找后再进入状态锁；`TryGetValue(...).TryConsume()` 也是查找后再进入状态锁。若引擎允许 Received 在对应 Before 完成 `Add()` 之前并发进入，静态上存在先 miss 后留下 pending 的窗口。源码不能证明该交错不可能。
- 实机边界：本轮未构建、未启动游戏、未驱动真实 Hook，因此不能把“通常回调顺序”升级为运行时保证。
### 检查面 2：ConditionalWeakTable 弱引用

## 已确认

- 文件第 405 行仍使用 `ConditionalWeakTable<PowerModel, FallbackPendingState>`，key 类型仍为 `PowerModel`，没有改成普通 `Dictionary`、静态强引用集合或按对象 ID 的表。
- 文件第 375-378 行的 `FallbackPendingState` 只有 `_sync` 与 `_count`；没有 `PowerModel` 字段、闭包字段或其它反向引用 key 的成员。文件第 429 行的 factory 也只创建无 key 成员的 state。
- 文件第 403-405 行明确没有在计数归零时 `Remove` CWT 项。对弱 key 而言，这不会把 `PowerModel` 变成由 value 强持有的对象；key 的回收仍交给 `ConditionalWeakTable` 的弱 key 机制，value 不会反向延长 key 生命周期。

## 进行中

- 本检查面的源码证据已完成；下一面核对 per-key 锁、CWT API 调用和计数读写的线程安全组合。

## 未知

- 没有构建或运行时对象存活探针，无法在本轮证明 GC 时点、回收延迟或引擎仍持有其它 `PowerModel` 引用时的实际生命周期；这些不属于源码中弱引用结构是否保留的结论。

### 静态风险与实机边界

- 静态风险：计数归零后 state 仍留在 CWT value 侧，可能保留少量空 state，直到弱 key 被清除；但当前 state 不持有 key，源码上未发现把 key 重新强引用的路径。
- 实机边界：未运行 GC/Hook 场景验证，因此不报告具体回收时间或内存表现。
### 检查面 3：锁与线程安全

## 已确认

- 文件第 382-386 行的 `_count++` 在 `lock (_sync)` 内；文件第 390-400 行对 `_count` 的零检查、减一和成功返回也在同一个 `lock (_sync)` 内。当前源码没有发现对 `_count` 的其它直接读写。
- `FallbackPendingState` 是私有 sealed 类型，第 377 行的 `_sync` 不向类外暴露；同一 `PowerModel` 的 Add/Consume 共用同一 state lock，不依赖全局可变 marker，也不需要跨 key 的锁顺序。
- 文件第 429、438 行使用 `ConditionalWeakTable` 的 `GetValue`/`TryGetValue`，没有把 CWT 替换成无同步的普通映射。不同 `PowerModel` 使用不同 state lock，不会因为一个 key 的计数而串行化所有 power。
- lock 的释放/获取覆盖了 `_count` 的增减和判断，静态上满足同一 state 内的基本可见性与互斥要求；没有在 state lock 内调用日志、Hook 或其它外部回调。

## 进行中

- 本检查面的锁结构审查已完成；下一面核对 Received 未命中、Before 未命中及归零/非空 modifiers 的原语义。

## 未知

- 本轮未对 .NET 运行时的 CWT 高并发实现、调度顺序或引擎是否允许 callback 跨线程作实机/构建验证。

### 静态风险与实机边界

- 静态风险：CWT 查找与 state lock 不是一个跨方法的原子事务。`GetValue` 返回后到 `Add()` 之前，另一线程的 `TryGetValue` 可能看到 `_count == 0`；这与检查面 1 的先 miss 窗口相同。当前改动只保证计数读写互斥，不保证“查表 + 计数操作”相对于另一条完整 callback 链的整体线性化。
- 静态风险：`_count` 使用 `int`，源码没有溢出保护；只有达到不现实的单 key 超大 in-flight 数量才相关，本轮不把它扩大为当前任务的主要缺陷。
- 实机边界：未构建、未运行游戏，不能证明真实 callback 调度恰好满足该实现所需的 Before 先完成再进入 Received。
### 检查面 4：未命中路径与 Received 原语义

## 已确认

- 文件第 415-418 行：`power is null` 仍直接返回 `false`，不创建 pending state；第 420-426 行：判定结果为 `block == false` 时也直接返回 `false`，不写入 CWT。
- 文件第 791-794 行：Before prefix 在 `ShouldBlockFallbackBefore` 未命中时仍返回 `true`，让原始 `BeforePowerAmountChanged` 继续执行；没有把未命中路径改成跳过原 hook。
- 文件第 910-914 行：Received 未找到 state 或计数为零时，仍把 `modifiers` 写成 `Array.Empty<AbstractModel>()` 并返回 `true`，让原始 Received hook 继续执行。这保持了旧单 marker 路径的 miss 语义。
- 文件第 916-918 行：命中 pending 时才把 `__result` 设为 `0m`、把 `modifiers` 设为非 null 空集合并返回 `false`；没有把归零动作扩展到未命中调用。
- 文件第 450-452 行的 `ApplyInternal` fallback 不使用 pending state；本次计数修改没有把类型级兜底错误地接入 Received 计数。

## 进行中

- 本检查面的未命中与归零语义已完成；下一面核对中央 gate、安装状态和安装日志代码是否仍保持原结构与调用关系。

## 未知

- 不构建、不运行游戏，无法确认真实 `Hook.ModifyPowerAmountReceived` 在 miss 时对空 modifiers 的后续处理，或真实引擎是否在所有路径都提供与 canonical power 对应的 Received。

### 静态风险与实机边界

- 静态风险：如果 callback 不成对，旧 pending 仍可能被后续同一 `PowerModel` 的 Received 消费；计数修复解决的是“多次 Before 覆盖”，没有凭空建立引擎级 callback 身份关联。
- 实机边界：以上是当前源码控制流结论，不是游戏运行验证；未声明未命中路径已通过实机验收。
### 检查面 5：中央 gate、安装状态与安装日志

## 已确认

- 文件第 141 行仍以 `ApplyGateInstalled && ModifyGateInstalled` 定义 `CentralGateOperational`；文件第 1033-1047 行仍在中央 gate 完整时直接返回、否则记录 central gate incomplete 并安装三个 fallback patch。
- 文件第 143-229 行的中央 gate/fallback 安装状态属性、`Record*Installed`、`Report*Unavailable`、`RecordFallbackDegraded` 及其 `MainFile.Logger` 文本仍存在；文件第 1040-1061 行的 fallback 安装日志、降级日志和成功日志仍存在。
- 文件第 477-690 行的两个中央 gate patch 仍分别解析非泛型 `PowerCmd.Apply` 与 `PowerCmd.ModifyAmount`，其 prefix 仍调用 `ShouldBlockApply` / `ShouldBlockModify`；本次 pending 计数没有接入中央 gate。
- 文件第 695-919 行的 fallback Before/Received 安装解析、`Prepare`/`Cleanup` 状态记录、Harmony priority、调用点和 Received 的归零/`modifiers` 处理仍保留。文件第 927-1024 行的 `ApplyInternal` fallback 也未接入 pending 计数。
- 实现代理的可见写入轨迹只对 `using`、第 375-405 行状态块、Before 的计数写入和 Received 的消费块作替换，随后只改了相关注释；没有写入中央 gate 方法或安装日志方法。结合当前源码仍存在的上述方法/日志，未发现中央 gate 或安装日志被改动。

## 进行中

- 本检查面的源码结构与写入轨迹已完成；准备追加最终汇总，明确静态结论、剩余风险和未验证边界。

## 未知

- 目标文件在当前 Git 工作树中显示为未跟踪文件，无法使用 `git diff HEAD -- <path>` 对本次修改生成可靠的基线差异；因此“未改动”结论基于当前结构、精确写入轨迹和符号/调用点核对，不声称已完成 byte-level 基线 diff。
- 未构建，不能确认 Harmony 反射签名、C# 编译或运行时安装结果；未运行游戏，不能确认日志实际输出次数或中央/fallback 分支的真实选择。

### 静态风险与实机边界

- 静态结论：从可见的精确替换范围和当前文件内容看，中央 gate 的判定、安装状态、安装日志和 fallback 安装调用关系没有被本次计数修改触碰。
- 实机边界：真实安装顺序、目标解析、日志输出和 central/fallback 分支仍未实机验证，不能报告为运行时已确认。
## 最终汇总

## 已确认

- 结论（静态）：本次把单 marker 覆盖问题改为按 `PowerModel` 计数；在每个 Before 已完成 `Add()` 后，多个 block Before 可由多个 Received 逐次消费，不再出现第二次 Before 覆盖第一次 pending 的旧路径。
- 结论（静态）：`ConditionalWeakTable<PowerModel, FallbackPendingState>` 仍保留弱 key；state 不反向持有 `PowerModel`。
- 结论（静态）：`_count` 的增量、零检查和减量由同一 per-key `lock (_sync)` 保护；不同 key 不共享该锁。
- 结论（静态）：Before 未命中、Received 未命中和 Received 命中后的归零/非空 modifiers 控制流保持原语义。
- 结论（静态）：依据实现代理的精确写入轨迹、当前中央 gate/安装器/日志区域及调用点核对，没有发现中央 gate、安装状态、安装日志或 fallback 安装接线被本次改动触碰。

## 进行中

- 无。本轮只读审查已完成；未修改产品代码、未构建、未运行游戏。

## 未知

- 仍有一个静态并发边界：CWT 查找与 `Add()`/`TryConsume()` 不是同一个跨 callback 的原子事务；若真实引擎允许 Received 在对应 Before 完成 `Add()` 前进入，仍可能先 miss。源码和本轮证据不能排除该调度。
- 未验证异常、取消、重入、跨线程及不成对 callback 的引擎契约；未验证 GC 时点、Harmony 安装、编译和游戏内日志。
- 因目标文件当前为 Git 未跟踪文件，未能执行可靠的基线 `git diff`；中央 gate/安装日志“未改动”结论不是 byte-level diff 结论。

### 最终静态风险与实机边界

- 静态风险等级：原单 marker 的多次 Before 覆盖风险已由计数结构消除；剩余风险是查表与计数操作之间的 callback 调度窗口，以及 callback 不成对时 pending 的语义边界。
- 实机边界：本结论仅来自源码和实现代理写入轨迹；没有任何构建或游戏运行证据。