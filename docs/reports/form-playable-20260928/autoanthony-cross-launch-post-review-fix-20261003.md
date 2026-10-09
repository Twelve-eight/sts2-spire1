# AutoAnthony 交叉启动审查后的退出竞态修复记录

## 已确认

- 独立只读审查报告 `autoanthony-cross-launch-final-review-20261003.md` 在源码层定位了一个 P2 竞态: `ProcessExit` 已标记 shutdown 后,此前进入 `ExecuteApplyCore` 的 unsettled Apply 仍可能无条件调用 `HookAssemblyLoad`,重新留下 AssemblyLoad 事件引用。
- 修复文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs`。
- 修复位置:
  - 第 38 行: 新增 `AssemblyLoadGate`。
  - 第 112-123 行: `OnProcessExit` 在该 gate 内退订并清除 `_hooked`。
  - 第 503-541 行: `HookAssemblyLoad` 增加入口和 gate 内二次 `ShouldStopNotifications()` 检查;订阅失败后的 retry bookkeeping 移到 gate 外。
  - 第 554-578 行: `UnhookAssemblyLoad` 与订阅共享同一 gate,错误日志在释放 gate 后执行。
- 设计目的: 关闭 shutdown 检查与实际 `AssemblyLoad +=` 之间的 TOCTOU 间隙,同时避免 `AssemblyLoadGate -> RetryGate` 的锁反转。没有改变 AutoAnthony/Watcher/AutoAnthonyWatcher 的功能契约,没有增加任何硬引用。
- 中央 r2 构建曾因字段声明写入脚本失误产生 3 个 CS0103;该失败立即修正,没有部署或作为验收证据。r3 最终构建日志 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-build-autoretimer-r3-20261003.log` 为 `0 errors / 60 warnings`。
- r3 门禁 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-autoretimer-r3-20261003.json` 全部 PASS: forbidden AssemblyRef、manifest consistency、forbidden TypeDef。发布 DLL 的 mod AssemblyRef 只有 `BaseLib`。
- r30 真实隔离矩阵 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\matrix-summary.json` 的 9 个场景全部 `exitCode=0`,无超时、无窗口句柄、日志排空、共享配置未变化、无嵌套 manifest。
- r30 的 m6、m7、m8、m9 均未出现 `Cannot access a disposed object` 或 `ObjectDisposedException`;m7 legacy bridge、m8 official addon takeover、m9 缺核心 addon 的 loader fail-closed 均保持。

## 进行中

- 该修复由主会话按独立审查意见完成;审查代理的报告只覆盖修复前源码。r3 构建和 r30 真实隔离回归覆盖了修复后的编译、结构和启动行为,但没有形式化替代并发证明。

## 未知

- 未执行人工注入的 Godot teardown 与 AssemblyLoad 同时发生压力测试。
- 未覆盖可见 UI、视觉、长战斗、战中存档、重连、多人同步、性能和完整平衡。
- AutoAnthony 自身的 `Expected 65 complete v111 Colorless cards, found 76` 仍是第三方内容版本问题;本修复没有宣称其 chaos gameplay 已通过。
- 独立审查安全会话未暴露实际解析模型和 provider route,报告按要求记录 Unknown,没有从请求文字推断。
