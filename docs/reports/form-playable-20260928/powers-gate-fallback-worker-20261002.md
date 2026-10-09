# powers gate fallback worker report - 2026-10-02

## 已确认

- 检查时间: 2026-10-02; 本轮仅读取源码与文档，未构建、未运行游戏。
- 目标文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs`。
- 修复前源码第 375-376 行使用 `FallbackPendingMarker` 单对象和 `ConditionalWeakTable<PowerModel, object> FallbackPending`，每个 `PowerModel` 同时只能保存一个待消费状态。
- 修复前源码第 391-402 行的 Before 路径先判定门控；命中后第 400 行 `FallbackPending.Remove(power)`，再由第 401 行 `FallbackPending.Add(power, FallbackPendingMarker)` 写入单 marker。
- 修复前源码第 408-410 行的 Received 路径只调用一次 `FallbackPending.Remove(power)` 作为消费判断。若同一 `PowerModel` 在前一个 Received 尚未到达前再次命中 Before，第二次写入会替换而不是累积第一次状态；第一次 Received 消费后，第二次 Received 可能观察不到待消费状态并放行。这正是本任务要求修复的静态 P1 风险。
- 现有语义边界可从第 80-85 行、第 378-381 行和第 405-410 行确认：Before 不跳过原 hook，Received 命中时归零并提供非空 modifiers，未命中时放行；安装状态与日志由其它路径维护，本次不应改动。
- 并发不变量（修复目标）：同一 `PowerModel` 的每一次被阻断 Before 必须产生一个独立待消费单元；每一次对应的 Received 最多消费一个单元；不同 `PowerModel` 之间不能互相影响；容器仍必须由 `ConditionalWeakTable` 持有以保持弱引用生命周期；Before/Received 的增量与消费必须在同一同步边界内完成。


### 2026-10-02 修复与静态核对

- 已修改目标文件第 375-405 行：新增 `FallbackPendingState`，以每个 `PowerModel` 对应的状态保存 `_count`；`Add` 与 `TryConsume` 都在该状态自己的 `lock (_sync)` 内完成。
- 已修改目标文件第 429 行：每次被阻断的 Before 通过 `ConditionalWeakTable.GetValue` 取得该 `PowerModel` 的状态并增加一次计数；不再删除并覆盖已有状态。
- 已修改目标文件第 436-444 行：Received 通过 `TryGetValue` 找到同一状态后只消费一个计数；计数为零或没有状态时返回 false，因此多次 in-flight Before 可由多次 Received 一一消费。
- 第 403-405 行明确保留 CWT 项而不在计数归零时 `Remove`：这样不会因 Before/Received 并发而把相邻增量分裂到不同状态；状态值不引用 key，生命周期仍由 `ConditionalWeakTable` 的弱 key 语义控制。
- 未修改中央 gate、fallback 安装状态属性、安装日志、阻断日志、补丁调用点或 Received 的归零与 modifiers 语义。
- 非构建静态检查结果：旧的 `FallbackPendingMarker`、`ConditionalWeakTable<PowerModel, object>`、`FallbackPending.Remove(power)` 和 `FallbackPending.Add(power, ...)` 均不存在；新的 typed CWT、per-key `GetValue`、per-key `TryGetValue`、双处 `lock (_sync)`、加一和减一均存在于目标文件。
- 最终静态核对全部通过：旧单 marker 与旧 Remove/Add 路径不存在；typed CWT、per-key 增量、per-key 消费、两个 per-key 锁站点、弱引用生命周期注释和报告三段结构均存在。请求路径状态显示本轮写入的报告与目标源码文件，未执行构建或游戏运行。
## 进行中

- 本轮源码修改与静态检查已完成；没有剩余的代码检查面。构建和游戏运行仍按用户明确要求不执行。

## 未知

- 当前源码与本轮只读材料不能证明 engine callback 在所有异常、取消、重入和跨线程路径上严格成对；修复不会添加未经证实的超时、跨调用回收或猜测性清理。若存在不成对边界，仅在最终报告中记录。
- 按用户要求，本轮不会通过构建或游戏运行验证编译与实机行为；最终验证范围限于源码差异、静态检查和报告中的明确未验证项。
