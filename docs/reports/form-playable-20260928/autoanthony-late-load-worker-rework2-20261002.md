# AutoAnthony late-load worker rework2 - 2026-10-02

## 已确认

### 编辑前第一条证据（已先于产品代码编辑落盘）

- `AutoAnthonyCompatBridge.cs:527-534` 在 `Watcher` 或 `WatcherCardPool` 任一类型缺失时清理瞬态状态、置为 `Unsupported`，但仍 `return true`；这会让 `Apply` 将可选能力视为 settled，并允许加载钩子摘除，晚加载 `AutoAnthonyWatcher` 可能失去观察路径。
- `AutoAnthonyCompatBridge.cs:283-293` 的 `NeedsRetryWithoutAssemblyLoad` 只覆盖 `OfficialAddonPending` 或残留 partial patch，不覆盖 optional patch 异常后回滚成功、状态为普通 `Pending` 的重试请求。
- `AutoAnthonyLoadHook.cs:68-97` 的 AssemblyLoad 回调直接调用 `TryApplyOnce`；`AutoAnthonyLoadHook.cs:129-154` 的 `System.Threading.Timer` ThreadPool 回调也直接调用 `TryApplyOnce`，而 `TryApplyOnce` 直接进入 `AutoAnthonyCompatBridge.Apply`，当前 `RetryGate` 只保护 `_retryInFlight`，没有覆盖 AssemblyLoad、initializer、retry 的共同 Apply/Harmony 串行边界。
- `FormNativeSmokeRunner.cs:2075-2108` 提供可复用的主线程门禁证据：主线程直接执行；非主线程通过 `Callable.From(...).CallDeferred()` 投递，并以 `TaskCompletionSource` 传递结果。该模式将作为本轮 retry/AssemblyLoad/initializer 的统一执行路径参考。

本报告在上述证据落盘后才开始修改产品文件。按请求本轮只允许修改两个 Interop 源文件和本报告；不构建、不测试、不部署、不启动游戏。

## 进行中

- 正在闭合监督报告中的五项 REWORK，具体实现与最终证据将在后续小节追加。

## 未知

- 当前尚未构建或运行，C# API 兼容性、Godot 主线程 deferred 调度时序和 Harmony 运行期行为仍未验证。
## 已确认

### 本轮实现结果

1. **Watcher / WatcherCardPool 缺失不再 settled，且保持官方 addon 观察路径。**
   - `AutoAnthonyCompatBridge.cs:535-543` 将缺少任一类型的分支改为 `Pending` + `return false`。
   - 该分支先清理第三方瞬态映射和池缓存，不安装局部 patch；因此能力是 fail-closed，`Apply` 不会把它报告为 settled。
   - `AutoAnthonyLoadHook.cs:135-139` 在非 settled 结果下保留 AssemblyLoad hook，并调用 retry source；官方 `AutoAnthonyWatcher` 后续加载仍会进入 `OnAssemblyLoad`。

2. **Pending 和异常回滚后的状态具有无 AssemblyLoad 的重试源。**
   - `AutoAnthonyCompatBridge.cs:283-300` 的 `NeedsRetryWithoutAssemblyLoad` 现在覆盖 core 未完成、普通 `Pending`、`OfficialAddonPending`、`Unsupported` 以及三组 partial patch 列表。
   - `AutoAnthonyLoadHook.cs:195-222` 只在 Godot 主线程检查该状态并创建低频 `System.Threading.Timer`。
   - 如果 `CallDeferred` 提交本身失败，`AutoAnthonyLoadHook.cs:79-87` 通过 `EnsureRetryWakeSource` 保留后续唤醒源；该备用源不读取 bridge capability，也不执行 Harmony。

3. **initializer、AssemblyLoad 和 retry 共用串行化 Apply 边界，并统一主线程执行。**
   - 三个入口分别在 `AutoAnthonyLoadHook.cs:28-35`、`:164-175`、`:236-244` 进入 `RequestApply`。
   - `RequestApply` 使用 `ApplyGate` 合并重复请求（`:46-71`），非主线程只提交 `Callable.From(...).CallDeferred()`（`:73-77`）。
   - 真正的 `AutoAnthonyCompatBridge.Apply` 只出现在 `ExecuteApplyLocked`（`:106-125`），该方法先以 `NGame.IsMainThread()` fail-closed 门禁，再在同一 `ApplyGate` 保护下执行；Harmony patch/unpatch 以及 capability 状态转换不会由 Timer ThreadPool 回调直接执行。
   - `FormNativeSmokeRunner.cs:2075-2108` 的 `Callable.From(...).CallDeferred()` 模式已在编辑前读取并作为本轮实现依据。

4. **OfficialAddonPending 在无下一次 AssemblyLoad 时仍会重试，settled 前不摘除 sources。**
   - `NeedsRetryWithoutAssemblyLoad` 显式保留 `OfficialAddonPending`（`:293-298`）。
   - 非 settled 路径保持 `HookAssemblyLoad` 与 `EnsureRetryTimer`（`:127-139`）；只有 `settled == true` 才执行 `UnhookAssemblyLoad` 和 `StopRetryTimer`（`:127-134`）。
   - Timer 回调仅读取已保存的 Harmony 并请求 deferred Apply（`:236-244`），因此官方 addon 已加载但没有新的 AssemblyLoad 时仍能再次进入 `SetOfficialWatcherCapability`。
   - capability 状态与 patch method lists 的现有 fail-closed 逻辑未被清空式成功捷径绕过；本轮仅扩大 retry 判定并保留 pending 状态。

5. **未引入第三方硬类型引用。**
   - 本轮 `AutoAnthonyCompatBridge.cs` 仍只通过程序集名、类型名字符串和反射解析 `AutoAnthony` / `Watcher`；`AutoAnthonyLoadHook.cs` 仅比较字符串程序集名（`:247-250`）。
   - 未添加 `using Watcher`、`using AutoAnthony`、第三方具体类型字段或方法签名。本项是源码静态证据；最终 DLL AssemblyRef 仍需中央构建后门禁确认。

### 范围与执行纪律

- 首条编辑前证据已先写入本报告，随后才编辑产品源码。
- 本轮只修改了 `mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs`、`mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs` 和本报告。
- 未构建、未测试、未部署、未启动游戏，未修改 Steam 安装或共享 `mod_configs`。
- `git diff --check` 对本轮两个已跟踪源码文件无空白错误。

## 进行中

- 等待监督代理对本轮 rework 实现进行第二轮静态复核；在监督 PASS 前，本实现不作为最终通过证据。
- 中央构建、AssemblyRef 门禁、部分 mod 启动矩阵和实机验证尚未开始，必须由主会话统一执行。

## 未知

- 未构建确认 `Godot.Callable.From(...).CallDeferred()`、`NGame.IsMainThread()`、`System.Threading.Timer` 类型别名与当前 Godot/.NET API 的编译兼容性。
- 未运行确认 AssemblyLoad 事件是否可能在目标游戏的非主线程触发、deferred callback 的实际调度时序、以及加载期间重复请求的最终合并行为。
- 未运行确认 Harmony 在主线程执行 patch/unpatch 时的真实方法计数、异常回滚和官方 addon 接管结果。
- 未实测 Watcher 类型缺失、Watcher 晚注册、optional patch 异常后回滚成功、回滚失败、以及 `AutoAnthonyWatcher` 已加载但没有后续 AssemblyLoad 的完整路径。
- 最终 Spire1.dll 对 `AutoAnthony`、`Watcher`、`AutoAnthonyWatcher` 的 AssemblyRef 结果仍待中央构建后的门禁输出确认。
