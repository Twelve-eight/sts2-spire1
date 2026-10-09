# Forms 独立只读审查: Watcher 桥接重加载/状态机 (dsv41f-round1)

## 已确认

### R01 [P0] 缺 Watcher 时 TryBind 一次性封死, 进程内无重试

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:58-63` (`TryBind` 入口, 先把 `_attempted` 置 true), `:106-111` (`ValidateBinding` 只在当前 AppDomain 程序集列表中查找 `Watcher`), `:80-103` (任何异常后仅回滚并返回 false).
- 触发条件: Watcher 程序集在 `ModelDb.Init` postfix 执行时尚未加载, 或 Watcher 版本/签名不匹配, 或任一 Harmony 目标安装失败.
- 权威契约: `FormStanceWatcherBridge.cs:57` 注释声明由 ModelDb.Init 后同步调用, 不是 assembly-load 事件; `FormStanceMode.cs:23-35` 要求已选中 modifier 的存档在依赖不可绑定时显式失败; `FormStanceModifier.cs:14-21` 在 run created/loaded/combat start 调用 `RequireAvailable`.
- 当前控制流: `FormStanceModePatch.cs:8-13` 只在 `ModelDb.Init` postfix 调用一次 `TryBind`; `TryBind` 首次进入即设置 `_attempted=true`; 后续任何调用在 `:60-61` 直接返回 `IsAvailable`; 没有 `AssemblyLoad`/`AssemblyResolve` 兜底, 没有定时重试, 没有"依赖晚到后重新尝试绑定"的状态.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_attempted|AssemblyLoad|AssemblyResolve|AppDomain' -Encoding UTF8`
- 最小修复范围: 只改 `FormStanceWatcherBridge.cs` 的绑定状态机 (区分"尚未加载/可重试"与"版本不兼容/确定性失败"), 以及 `FormStanceModePatch.cs` 的触发源; 不触碰产品语义代码.
- 尚缺的实机证据: 在真实启动序列中把 Watcher 延后到 `ModelDb.Init` 之后加载, 观察是否出现重试, 以及旧存档是否按契约显式失败而不是静默降级.

### R02 [P0] 同进程重加载会复用进程级静态绑定状态, 无法重新评估 Watcher 可用性

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:33-39` (`_binding`, `_attempted`, `UnavailableReason`, `BoundTargets` 全是 static), `:58-62` (第二次调用直接返回), `FormStanceModePatch.cs:8-13` (仅在 ModelDb.Init 后调用一次).
- 触发条件: 同一进程内二次进入 `ModelDb.Init` (例如 mod 列表重载/场景重载), 或者在同一进程里先无 Watcher 后补装 Watcher.
- 权威契约: `DEVELOP.md:325` 要求可选程序集缺失时不得硬失败, 应保持 disabled/pending/fail-closed; `DEVELOP.md:326-331` 要求晚加载唤醒与退出期间 fail-closed; 本请求要求推导同进程重加载状态机.
- 当前控制流: `TryBind` 只在首次调用时真正尝试; 之后 `_attempted=true` 使其成为永久 no-op. 进程级 static 不会随 `ModelDb.Init` 重置. 没有 `AppDomain.Unload`/`AssemblyLoadContext.Unloading` 清理, 也没有把 `_attempted` 重置为可重试的路径.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_binding|_attempted|UnavailableReason|BoundTargets' -Encoding UTF8`
- 最小修复范围: 给 `FormStanceWatcherBridge` 增加明确的 `Reset()`/代际 token 或把"已加载但签名不匹配"的终态与"尚未加载"的可重试态分开; 触发源仍可保留 ModelDb.Init, 但需允许晚加载后再次进入.
- 尚缺的实机证据: 无真实同进程重载/热重载运行日志; 不能证明引擎会在何时二次调用 ModelDb.Init, 也不能证明 static 状态不会被新 AssemblyLoadContext 隔离.

### R03 [P1] AssemblyLoad 事件缺失, Watcher 晚加载没有唤醒源

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:108-111` (每次 `ValidateBinding` 都重新枚举 `AppDomain.CurrentDomain.GetAssemblies()`), `:57` 注释明确 "not by an assembly-load event"; `FormStanceModePatch.cs:6-7` 注释明确 "No AssemblyLoad event".
- 触发条件: `ModelDb.Init` 时刻 Watcher 尚未进入 AppDomain; 之后 Watcher 才被 ModManager 加载.
- 权威契约: `DEVELOP.md:325` (缺少可选程序集时继续完成 initializer 并记录 disabled/pending/fail-closed), `DEVELOP.md:326` (AssemblyLoad 是晚加载的廉价唤醒源), `DEVELOP.md:330` (AssemblyLoad 订阅必须与 ProcessExit 共享串行 gate).
- 当前控制流: 绑定判断只发生在 `ModelDb.Init` postfix 一次; 没有 `AppDomain.CurrentDomain.AssemblyLoad +=` 订阅, 没有 `AssemblyResolve`, 也没有 Timer/fallback thread. 因此 Watcher 晚加载时不会自动重试.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'AssemblyLoad|AssemblyResolve|Timer|ProcessExit' -Encoding UTF8`
- 最小修复范围: 在 `FormStanceWatcherBridge` 增加按 `DEVELOP.md:326-331` 约束的 AssemblyLoad 通知源 (仅通知, 串行 gate, 退出 fail-closed), 或在 ModelDb.Init 后由既有 AutoAnthonyLoadHook 的统一唤醒路径驱动一次重试; 不改绑定校验语义.
- 尚缺的实机证据: 没有观察到 Watcher 晚加载场景的真实日志顺序; 也没有压力验证 AssemblyLoad 回调与 Godot 主线程/退出清理的交互.

### R04 [P1] Harmony 安装失败后的回滚只覆盖本次 specs 的目标, 未覆盖已提前发布的 native delegate

- 绝对路径与准确行号: `FormStanceWatcherBridge.cs:64-70` (创建 Harmony, 校验, 先设置 `_nativeNotification`, 再逐个 `harmony.Patch`), `:80-103` (catch 中置空 `_binding`, 回滚 `specs` 目标并检查 owner 残留), `:99-100` (回滚失败只拼进 `UnavailableReason`), `:37-39` (`IsAvailable` 只看 `_binding`).
- 触发条件: `ValidateBinding` 通过并设置 `_nativeNotification` 后, 某个 `harmony.Patch` 或 `VerifyInstalled` 抛异常.
- 权威契约: 请求要求推导 "Harmony 安装回滚" 状态机要求; `DEVELOP.md:256` 要求目标漂移时 fail closed, 不把失败记成成功; `DEVELOP.md:330-331` 要求订阅/通知在退出期间 fail-closed.
- 当前控制流: catch 会清 `_binding`, 因此 `IsAvailable` 为 false, 这是 fail-closed 的; 但 `_nativeNotification` 未被清空, 仍是已校验的 Watcher delegate. 更关键的是回滚只对 `specs` 里已经加入的目标做 `Unpatch`; `:70` 的 `harmony.Patch` 若在异常前已经对某个目标完成部分安装, 该目标在 specs 中, 会被回滚; 但 `:71-72` 的 `VerifyInstalled` 失败路径不会移除同一目标上可能存在的其它 owner patch (按 Harmony 语义 Unpatch 只按 HarmonyId, 这点是符合最小权限的). 回滚失败只记录, 不阻断后续进程继续运行.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_nativeNotification|Unpatch|rollbackFailures|IsAvailable' -Encoding UTF8`
- 最小修复范围: 在 catch 中把 `_nativeNotification` 一并清空, 并把 "rollback failures" 作为硬 fail-closed 状态暴露给 `UnavailableReason` 之外的显式布尔/枚举, 避免后续 `TryBind` 重试时复用半成品状态.
- 尚缺的实机证据: 没有注入 "Patch 中途失败" 或 "Unpatch 抛异常" 的真实运行证据; 回滚失败后的 Harmony 实际方法状态未验证.

### R05 [P1] 退出清理缺失: FormStanceWatcherBridge 无 ProcessExit/Unload 退订路径

- 绝对路径与准确行号: `FormStanceWatcherBridge.cs:28-39` (static class, 无任何退出事件字段), `:64-70` (每次 TryBind 新建 Harmony 并安装), `FormStanceModePatch.cs:8-13` (仅 ModelDb.Init 后触发). 对比 `AutoAnthonyLoadHook.cs:82-104` (有 ProcessExit 与 `_processExitHooked`) 和 `:507-584` (AssemblyLoadGate 串行订阅/退订).
- 触发条件: 游戏进程退出, 或同进程卸载/重载 mod 程序集时, 已安装的 Harmony patch 仍挂在 Watcher/引擎方法上.
- 权威契约: `DEVELOP.md:326-331` 明确要求退出期间所有通知必须静默退出, AssemblyLoad 订阅必须与 ProcessExit 共享串行 gate, 退出回调 fail-closed; 本请求要求推导 "退出清理" 状态机.
- 当前控制流: `FormStanceWatcherBridge` 从未注册 `AppDomain.CurrentDomain.ProcessExit`, 也没有 `AssemblyLoadContext.Unloading` 或 `AppDomain.DomainUnload`; 静态 `_binding` 不会在退出时清空. 已安装的 patch 依赖 Harmony/进程本身终止, 没有显式撤下.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'ProcessExit|DomainUnload|Unloading|Unpatch' -Encoding UTF8`
- 最小修复范围: 给 `FormStanceWatcherBridge` 增加与 `AutoAnthonyLoadHook` 同构的退出 gate, 至少清空 `_binding`/delegate 并阻止晚到回调继续安装; 是否 `Unpatch` 取决于引擎退出期是否能安全调用 Harmony.
- 尚缺的实机证据: 没有退出期间 AssemblyLoad 与 TryBind 同时发生的压力复现; 无法确认 Godot 退出时 Harmony patch 是否仍被调用.

### R06 [P1] 旧存档载入只检查 modifier 是否存在, 不校验持久化的姿态载体与 native marker 是否仍匹配

- 绝对路径与准确行号: `FormStanceModifier.cs:14-16` (`AfterRunCreated`/`AfterRunLoaded` 只调用 `RequireAvailable`), `FormStanceMode.cs:20-30` (`IsSelected` 只按 `RunState.Modifiers` 里是否存在 `FormStanceModifier`), `WatcherFormStancePower.cs:22-31` (`Data` 含 `NativeMarker`, `Applied`, `FirstEffect`, `SecondEffect` 等运行态字段), `:50` (`InitInternalData` 返回新 `Data`, 没有从存档恢复 `NativeMarker` 的代码), `:52-58` (`BindNativeMarker` 只在首次应用时允许一次), `:61-69` (`BeforeApplied` 要求 `NativeMarker` 非空且 `target.Powers.Contains(marker)`).
- 触发条件: 旧存档在带 `FormStanceModifier` 的情况下载入, 其中可能保存了 `WatcherFormStancePower` 或其它形态 power; 或者存档载入发生在 Watcher 缺失/版本不匹配时.
- 权威契约: `DEVELOP.md:325` 要求缺少可选程序集时 disabled/pending/fail-closed; `FormStanceMode.cs:23` 注释要求已选中存档在依赖不可绑定时显式失败; `DEVELOP-form-playable-20260928.md:152` 明确 "存档重载" 仍是未关闭边界.
- 当前控制流: `AfterRunLoaded` 只验证 bridge 可用性, 不验证存档里的 carrier 是否仍能绑定到 native marker; `InitInternalData` 每次返回全新 `Data`, `NativeMarker` 默认 null; `BeforeApplied` 因此会对任何由存档反序列化出来的 carrier 抛 `InvalidOperationException` ("Form stances must enter through the real Watcher stance command"). 这是一个显式失败, 但没有恢复/迁移路径, 也没有在载入时给出结构化诊断.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs' -Pattern 'NativeMarker|InitInternalData|BindNativeMarker|BeforeApplied' -Encoding UTF8`
- 最小修复范围: 在 `FormStanceModifier.AfterRunLoaded` 或 `WatcherFormStancePower` 的载入路径中增加一致性校验/恢复策略; 最小改动是明确拒绝不可恢复的 carrier 并记录原因, 不改原生 marker 语义.
- 尚缺的实机证据: 没有战中/战外真实存档重载日志, 没有证明 carrier 在存档里是否被序列化,是否重新绑定, 也没有验证旧档在 Watcher 缺失时的实际报错文本.

### R07 [P1] 重复补丁防护只靠 `_attempted` 与 Harmony owner, 无显式幂等/重复安装测试

- 绝对路径与准确行号: `FormStanceWatcherBridge.cs:58-62` (`_attempted` 是唯一重复进入保护), `:64-70` (每次新建 `new Harmony(HarmonyId)` 再 Patch), `:85-96` (回滚按 `specs` 去重后 Unpatch), `FormStanceModePatch.cs:8-13` (HarmonyPatch on ModelDb.Init, 可能被多次触发).
- 触发条件: `ModelDb.Init` 被多次触发, 或同一进程内多个调用者 (例如 `FormNativeSmokeRunner.cs:321` 与 ModInitializer) 先后调用 `TryBind`.
- 权威契约: `MainFile.cs:184-186` 注释声明 "Harmony 安装本身幂等, 不会重复挂载"; `DEVELOP.md:330` 要求订阅/安装串行 gate.
- 当前控制流: `_attempted` 在首次调用即置 true, 所以正常情况下不会重复 Patch; 但这是无锁的非原子 bool. 若两个线程同时进入 `TryBind` 且都读到 false, 可能各自 `new Harmony` 并 Patch. Harmony 同 id 重复安装会按 owner/method 去重或叠加, 这里没有显式 gate 或锁来保证 "恰好一次". 另一个调用者 `FormNativeSmokeRunner.cs:321` 也直接调 `TryBind`, 说明存在多个入口.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_attempted|new Harmony|harmony.Patch|lock' -Encoding UTF8`
- 最小修复范围: 用 `Interlocked.CompareExchange` 或锁把 "首次尝试" 与安装状态串行化, 并增加重复进入的自检; 不改 HarmonyId.
- 尚缺的实机证据: 没有并发触发 ModelDb.Init 或并发调用 TryBind 的真实日志; 未验证 Harmony 对同 owner 重复 Patch 的实际行为.

### R08 [P1] AssemblyLoad/版本不匹配失败是永久终态, 缺 "可重试" 与 "确定性不可用" 区分

- 绝对路径与准确行号: `FormStanceWatcherBridge.cs:60-62` (`_attempted` 一旦置位不再重试), `:108-136` (版本/签名检查失败抛异常), `:80-103` (异常统一走同一个 catch, 只写 `UnavailableReason`), `FormStanceMode.cs:23-35` (选中存档一律抛 `InvalidOperationException`).
- 触发条件: Watcher 尚未加载 (可重试) 与 Watcher 已加载但版本/签名不兼容 (确定性不可用) 走同一异常路径, 状态被合并.
- 权威契约: `DEVELOP.md:325` 要求 disabled/pending/fail-closed 可区分; 请求要求区分 fail closed 与晚加载重试.
- 当前控制流: 两种失败都设置 `_attempted=true`, 都令 `IsAvailable=false`, 都用 `UnavailableReason` 表达; 没有任何状态枚举区分 "等待依赖" 与 "确定不兼容". 这使后续无法安全决定是否重试, 也无法向用户解释原因.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'InvalidOperationException|UnavailableReason|_attempted' -Encoding UTF8`
- 最小修复范围: 引入 `BindingState` (Unbound/Retryable/Terminal) 或等价字段, 让晚加载只推进 Retryable, 版本不匹配直接 Terminal; `UnavailableReason` 仍保留人类可读文本.
- 尚缺的实机证据: 没有真实 Watcher 版本漂移/签名不匹配的运行日志; 未验证失败后再次加载是否会触发任何现有唤醒源.

### R09 [P1] 退出期 Harmony patch 的调用语义未定义: postfix 仍可能改写 `__result`

- 绝对路径与准确行号: `FormStanceWatcherBridge.cs:283-309` (`MarkerAppliedPostfix`/`AfterMarkerApplied` 在退出时仍可能继续执行), `:311-329` (`MarkerRemovedPostfix`/`AfterMarkerRemoved`), `:356-373` (`StanceChangedPostfix`/`AfterStanceChanged`), `:451-456` (`GainDivinityEntryEnergy` 仍会访问 `PlayerCmd`), `:493-507` (`RemoveEndTurnDivinity`/`NotifyEndTurnDivinity`).
- 触发条件: 进程退出或 Godot teardown 期间, 引擎方法仍被调用, Harmony patch 未撤下.
- 权威契约: `DEVELOP.md:326-331` 要求退出期间所有通知静默退出, 不得访问已释放原生对象; 本请求要求推导 "退出清理" 状态机.
- 当前控制流: 所有 postfix/transpiler 钩子只检查 `FormStanceMode.IsEnabled(...)` 和 `CombatManager.Instance.IsEnding`, 没有统一的 `_shutdownRequested` 检查; `IsEnabled` 只要求 `IsSelected` 且 `IsAvailable`, 退出期仍可能为 true. 因此理论上退出期仍会进入 `PowerCmd.Remove`/`PowerCmd.Apply`/`PlayerCmd.GainEnergy` 等路径.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'IsEnabled|IsEnding|IsOverOrEnding|GainEnergy|PowerCmd' -Encoding UTF8`
- 最小修复范围: 在 bridge 层增加统一的 shutdown guard, 所有钩子入口先 fail-closed 返回; 或在退出 gate 中先移除补丁再清理静态状态.
- 尚缺的实机证据: 没有 Godot teardown 与战斗方法同时调用的真实复现; 当前风险是源码级可达性, 不是已证明的 crash.

### R10 [P2] 旧存档载入时 `RequireAvailable` 抛出的异常可能打断整个 run 载入

- 绝对路径与准确行号: `FormStanceModifier.cs:14-16` (`AfterRunCreated`/`AfterRunLoaded` 直接调用 `FormStanceMode.RequireAvailable`), `FormStanceMode.cs:32-35` (`RequireAvailable` 在不可用时抛 `InvalidOperationException`).
- 触发条件: 存档带 `FormStanceModifier`, 但 `FormStanceWatcherBridge` 不可用 (Watcher 缺失/版本不匹配/晚加载尚未完成).
- 权威契约: `FormStanceMode.cs:23` 注释要求 "selected save must fail explicitly if its optional dependency can no longer be bound"; `DEVELOP.md:325` 要求缺少可选程序集时 initializer 继续完成并以 disabled/pending/fail-closed 记录能力状态.
- 当前控制流: `RequireAvailable` 是抛异常, 没有返回值/状态; `AfterRunLoaded` 不捕获异常. 因此异常会向引擎的 run 载入路径传播. 这符合 "显式失败" 的意图, 但没有证明引擎会把它呈现为可恢复的 UI 错误, 也没有在抛之前写结构化诊断.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs' -Pattern 'RequireAvailable|AfterRunLoaded|AfterRunCreated' -Encoding UTF8`
- 最小修复范围: 保留显式失败语义, 但在 `AfterRunLoaded` 增加一次 Error 日志并确保异常类型/消息稳定; 不改 bridge 可用性判断.
- 尚缺的实机证据: 没有在真实载入 UI 上观察异常呈现; 未证明引擎是否捕获并继续, 还是直接把玩家送回主菜单.

### 最小状态机建议 (只读推导, 不改代码)

- `Unbound`: 进程启动, 尚未调用 `TryBind`; 允许首次绑定.
- `Retryable`: Watcher 程序集尚未出现, 或已出现但签名/目标暂不可用; 允许 AssemblyLoad/Timer 再次触发绑定, 不发布 `IsAvailable=true`.
- `Installing`: 已选定 specs, 正在 `Harmony.Patch` + `VerifyInstalled`; 任何失败先回滚, 再按失败类型进入 `Retryable` 或 `Terminal`.
- `Bound`: 全部补丁安装并验证成功, 才发布 `_binding`; 允许进入玩法路径.
- `Terminal`: 已加载 Watcher 但签名/版本/目标确定性不兼容, 或回滚失败; 禁止再自动重试, 但保留显式诊断与用户可见的 fail-closed.
- `ShuttingDown`: 已收到退出信号; 禁止新绑定/新订阅, 清空 delegate/静态引用, 所有钩子 fail-closed.
- 关键不变量: `IsAvailable` 只在 `Bound` 为 true; `UnavailableReason` 只描述当前状态; `_nativeNotification` 不得在非 `Bound` 状态被使用; 所有状态转换在同一个 gate 内串行.

### 测试矩阵建议 (只读推导, 不执行)

| 场景 | 前置 | 期望 |
| --- | --- | --- |
| T1 Watcher 缺失 | 启动时无 Watcher | Spire1 正常初始化; bridge Retryable/Unavailable; 普通 run 可用 |
| T2 Watcher 晚加载 | ModelDb.Init 后加载 Watcher | 通过 AssemblyLoad 唤醒; 最终 Bound 或明确 Terminal; 不重复安装 |
| T3 签名漂移 | 加载不兼容 Watcher | Terminal; 选中存档显式失败; 不自动重试 |
| T4 Patch 中途失败 | 注入第 N 个 Patch 异常 | 回滚本 owner 全部目标; delegate 清空; 状态可诊断 |
| T5 Unpatch 失败 | 注入 Unpatch 抛异常 | 状态为 Terminal/不一致; 不宣称已回滚 |
| T6 同进程重载 | 二次 ModelDb.Init / 重载 | 不复用陈旧 static; 能重新评估或明确保持 Bound |
| T7 旧存档载入 | 带 FormStanceModifier 的旧档 | 可恢复则恢复; 不可恢复则显式 fail-closed + 诊断 |
| T8 退出期回调 | ProcessExit 与 AssemblyLoad/TryBind 竞争 | 不再订阅/安装; 钩子 fail-closed; 无 native 访问 |
| T9 重复补丁 | 并发或重复调用 TryBind | 恰好一次安装; 无重复 owner/patch |
| T10 战中存档重载 | 战斗中存档并重载 | carrier/marker/effect 一致性可验证; 无半替换 |

## 进行中

- 无. 只读审查已完成; 未改产品代码, 未构建, 未启动游戏, 未委派.

## 未知

- 尚无实机运行证据; 本报告目前只包含只读源码推导.

Request: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round1\reload.request.md
Requested model: global:deepseek-v4.1-flash
Requested route: wb2api
Actual resolved model/route: unknown from inside this session (not independently verified)
