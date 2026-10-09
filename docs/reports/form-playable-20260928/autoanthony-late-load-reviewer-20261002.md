# AutoAnthony late load supervisor review - 2026-10-02

复核前置已满足: 实现者 `01a0fcdb-9547-7ca1-acff-dfec6c0fb5d7` 已完成本轮最终汇报, 报告已落盘, 工作树已包含本轮修改. 本轮监督审查只读, 不构建, 不测试, 不部署, 不启动游戏.

## 已确认

### 旧轮次前置

- 旧轮次已确认 core patch 与 Watcher optional capability 不再共用单一 `_applied` 状态.
- 旧轮次已确认 AutoAnthony, Watcher 和 AutoAnthonyWatcher 通过程序集名与反射字符串处理,没有源码层硬类型前置.
- `Spire1.csproj` 和 `Spire1.json` 当前文本没有 AutoAnthony, Watcher 或 AutoAnthonyWatcher 的编译引用或 manifest 前置声明. 这是源码和配置静态证据,不是重新生成 DLL 的证据.

### 本轮从头复核的五个失败边界

1. **core group 完整计数和回滚: 部分 PASS,不能作为整体 PASS.**
   - `ExpectedFromPatchCount = 3`, `ExpectedPoolDeckPatchCount = 6`.
   - `TryApplyCoreGroup` 只有在完整计数时才置 `_fromPatchesApplied` 或 `_poolDeckPatchesApplied`.
   - 部分成功或异常会记录已安装方法,尝试按当前 Harmony ID 回滚;回滚失败则保留 pending 方法,后续先清理再重试.
   - 顺序执行路径静态上闭合. 但它仍受下面的 Timer 与 AssemblyLoad 并发风险影响,因此不能提升为最终 PASS.

2. **Watcher 局部安装回滚: 顺序路径 PASS,整体仍被并发风险阻断.**
   - `CardPool`, `AllCards`, `AllCardIds` 使用完整计数.
   - 局部安装失败会检查 `RollbackPatchedMethods` 返回值;回滚失败保留 `ThirdPartyPartialPatchedMethods`,不返回 settled.
   - 外层异常会合并本次安装清单、已安装清单和 partial 清单,尝试统一回滚,并清理映射与池缓存.
   - 旧回调在非 `LegacyBridge` 状态下直接放行或不改结果,具备顺序执行的 fail-closed 保护.

3. **WatcherCardPool 缺失: REWORK.**
   - 当前 `AutoAnthonyCompatBridge.cs:527-534` 在 `watcherType == null || watcherPoolType == null` 时清理状态并置 `Unsupported`,但仍返回 `true`.
   - `Apply` 会把它视为 optional capability settled,随后 `AutoAnthonyLoadHook` 摘除 AssemblyLoad 事件.
   - 如果之后才加载 `AutoAnthonyWatcher`,将没有事件触发官方接管. 该分支必须返回非 settled 状态,或建立等价的可重试终态.

4. **optional 外层异常: REWORK.**
   - `PatchThirdPartyEntries` 的异常回滚和 partial 清单处理本身已补上.
   - 但在回滚成功、没有残留 partial 方法时,代码在 `AutoAnthonyCompatBridge.cs:467-472` 将状态设为 `Pending` 并返回 false.
   - `NeedsRetryWithoutAssemblyLoad` 在 `:281-293` 只对 `OfficialAddonPending` 或 partial 方法返回 true. 若 AutoAnthony 与 Watcher 已经加载且之后没有新的目标 AssemblyLoad,该异常没有再次尝试路径.
   - 因此“异常后安全重试”尚未对所有路径闭合.

5. **AutoAnthonyWatcher 官方 addon 晚加载: REWORK.**
   - `LegacyBridge` 状态会保持 AssemblyLoad 监听,官方 addon 后加载时可尝试接管.
   - `OfficialAddonPending` 会保留旧 patch 方法清单,旧回调通过状态守卫 fail-closed,并由 Timer 尝试再次卸载.
   - 但缺少 `WatcherCardPool` 的分支提前返回 settled,会让上述晚加载路径失效.
   - 此外 `AutoAnthonyLoadHook.cs:68-77` 的 AssemblyLoad 回调和 `:129-145` 的 Timer 回调都能调用 `TryApplyOnce`/`Apply`;当前 `RetryGate` 只保护 `_retryInFlight`,没有覆盖 AssemblyLoad 回调、初始化调用、状态列表和 Harmony 操作. 存在并发重复安装、交错回滚和状态撕裂风险.
   - Timer 在 ThreadPool 上执行 Harmony patch/unpatch;当前没有证明这些操作满足游戏主线程或 Harmony 线程亲和性要求.

### 最终判定

- **REWORK**.
- 当前不能给 PASS. 必须至少修复: 1) 缺少 Watcher 类型时不得返回 settled; 2) Pending 异常在依赖已加载且无新 AssemblyLoad 时必须有可靠重试路径; 3) AssemblyLoad、初始化和 Timer 必须共享 Apply 的串行化边界,并明确 Timer/Harmony 的线程安全策略.

## 进行中

- 本轮五个失败边界的静态复核已完成.
- 监督审查没有继续修改产品代码,没有构建,测试,部署,启动游戏或操作共享配置.

## 未知

- 未构建确认当前 C# 源码和 Harmony API 的编译兼容性.
- 未确认实际 Harmony patch 数量、`Unpatch` 运行期行为、AssemblyLoad 顺序、Watcher pool 注册时序和 Timer 调度.
- 未确认 Harmony patch/unpatch 是否允许从 ThreadPool 执行,也未确认与游戏主线程的交互安全性.
- 未进行任何实机游戏验证.
