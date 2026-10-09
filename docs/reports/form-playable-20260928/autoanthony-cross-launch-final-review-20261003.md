# AutoAnthony cross-launch final review

## 已确认

- [P0, PASS] AutoAnthony 缺席和反射契约失败均为 fail-closed.
  - 路径与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:221-235,330-378`.
  - 触发条件: AutoAnthony 程序集不在 AppDomain, 或 `GeneratedCharacter`, `ChaosRunDefinitions`, `ChaosCardRegistry` 或必需成员无法解析.
  - 契约: 可选 mod 缺席不得阻断 Spire1 initializer, 不得安装半套 core patch.
  - 当前控制流: `Apply` 先检查 `AaAssembly`, 再执行 `ResolveReflection`; 任一失败直接返回 false. core patch 安装只在后续路径发生.
  - 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs' | Select-Object -Index (220..234)`.
  - 最小修复范围: 无. 保持 guard 在任何 Harmony patch 之前.
  - 尚缺实机证据: 无用户游玩安装上的退出行为证据. 本项是源码推理; r29 隔离启动的 AutoAnthony 缺席路径见 m1 和 m5.

- [P0, PASS] Watcher 和 AutoAnthonyWatcher 是可选能力, 缺少时不成为 Spire1 前置项; 官方 addon 可接管旧 bridge.
  - 路径与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:479-606,609-635`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:828-857`.
  - 触发条件: Watcher 缺席, Watcher 类型缺席, legacy bridge 已安装后 AutoAnthonyWatcher 晚加载, 或官方 addon 已在初始化前加载.
  - 契约: Watcher 缺席必须保持 Pending 或跳过; 类型不完整不得 settled; 官方 addon 出现时应移除 legacy patch 并进入 OfficialAddon.
  - 当前控制流: `PatchThirdPartyEntriesCore` 对缺席返回 false; 类型缺失清 transient state 后保持 Pending; `SetOfficialWatcherCapability` 回滚旧 patch, 回滚失败进入 OfficialAddonPending, 成功进入 OfficialAddon. `NeedsRetryWithoutAssemblyLoad` 只在可由已加载可选程序集推进时要求周期唤醒.
  - 隔离启动证据: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\m6-baselib-spire1-autoanthony\stdout.log:84` 为 `core=True, third-party=Pending, settled=False`; `m7-baselib-spire1-autoanthony-watcher\stdout.log:321-322` 为 LegacyBridge; `m8-baselib-spire1-official-addon\stdout.log:289-293,329-330` 为 addon 初始化且 `third-party=OfficialAddon, settled=True`; `m9-baselib-spire1-addon-missing-core\stderr.log:12` 拒绝缺少 AutoAnthony, 同时 `m9...\stdout.log:283,316` 显示 Spire1 初始化并保持普通池.
  - 可复现命令: `Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\m8-baselib-spire1-official-addon\stdout.log' -Pattern 'AutoAnthonyWatcher|third-party'`.
  - 最小修复范围: 无. 若未来修改, 不把 Watcher 或 AutoAnthonyWatcher 加入 `Spire1.json`.
  - 尚缺实机证据: 上述是隔离 headless loader 启动证据, 不是 UI, combat, save 或 multiplayer 实机行为.

- [P1, PASS] Apply 的主线程边界, deferred coalescing, 重入串行化和按需周期唤醒控制完整.
  - 路径与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:185-399,407-480,603-680,719-831`.
  - 触发条件: initializer, AssemblyLoad, retry timer 或 Godot deferred callback 请求 Apply.
  - 契约: AssemblyLoad 和 timer 只能通知; Harmony 和 bridge state 只在 Godot main thread; 可选程序集都未出现且 core 已完成时不得保留周期 timer.
  - 当前控制流: 非主线程请求走 `Callable.CallDeferred`; `_applyInProgress` 和 `_applyPending` 合并同步重入; `ExecuteApplyCore` 在 settled=false 时保留 AssemblyLoad, 仅 `NeedsPeriodicRetry` 为 true 时创建 timer. `AutoAnthony present + optional mods absent` 分支显式停止非永久 timer.
  - 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs' | Select-Object -Index (184..398,406..479)`.
  - 最小修复范围: 无静态必改项.
  - 尚缺实机证据: 没有执行 shutdown race, deferred callback cancel, timer construction failure 或 Godot teardown 压力测试.

- [P0, PASS] 项目依赖声明和 Release DLL 结构门禁没有发现可选 mod 的硬引用.
  - 路径与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:21-38`; `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-16`; `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json:2-29`; `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-current-20261003.md:6-16`.
  - 触发条件: 发布前检查 `Spire1.dll` 的 AssemblyRef, manifest consistency 和 TypeDef forbidden list.
  - 契约: 禁止 `AutoAnthony`, `AutoAnthonyWatcher`, `Watcher`, `DirectConnectIP`, `ActsFromThePast` AssemblyRef; manifest 只声明实际硬依赖 BaseLib; 禁止实验, bridge, held-back 和 Debug TypeDef.
  - 当前控制流: csproj 的实际 Reference 只有 `0Harmony` 和 `sts2`; AutoAnthony 区块只有说明注释. `Spire1.json` 只有 `BaseLib` dependency. 门禁 JSON 为 `passed=true`, AssemblyRef 只列 `BaseLib`, 三道 gate 全 PASS.
  - 可复现命令: `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json' -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 12`.
  - 最小修复范围: 无. 以后若引入新软依赖, 必须保持反射或字符串解析并让结构门禁继续覆盖.
  - 尚缺实机证据: 门禁是已构建 DLL 的离线元数据证据, 不是用户安装加载证据; 本轮未重新构建或重新运行门禁.

- [P1, PASS] r29 交叉启动矩阵的 m1,m5,m6,m7,m8,m9 均完成隔离 headless 启动, 没有把 optional mod 变成 loader 前置.
  - 路径与行号: 脚本 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r29-20261003.ps1:1-16,234-301`; 汇总 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\matrix-summary.json:2-6,225-336,339-456,459-518`.
  - 触发条件: 每个 case 复制选定 mod 到隔离 non-Steam game, 设置独立 APPDATA/TEMP, 启动 `SlayTheSpire2.exe --headless`, 读取 stdout/stderr 后清理.
  - 契约: m1 无 Watcher 仍启动; m5 full control 启动; m6 无 Watcher 的 AutoAnthony 启动; m7 legacy bridge 启动; m8 official addon 接管; m9 缺 AutoAnthony 时 addon 被拒绝但 Spire1 启动.
  - 当前控制流和结果: r29 的 m1,m5,m6,m7,m8,m9 `exitCode=0`, `launchError=null`, `timedOut=false`, `nonzeroWindowHandleObserved=false`, `logDrainCompleted=true`, `sharedConfigSha256Unchanged=true`. 汇总 `steamSafe=true`, boundary 明确为 isolated headless loader startup, 不含 form behavior acceptance. m1 的 `stdout.log:44,77,82` 显示 Spire1 initializer, AutoAnthony absent, 2 mods loaded; m5 的 `stdout.log:46,281,314,319` 显示 Watcher 和 Spire1 initializer, AutoAnthony absent, 3 mods loaded; m6,m7,m8,m9 的 bridge 和依赖结论见上一项及下一项.
  - 可复现命令: `$s=Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\matrix-summary.json' -Raw | ConvertFrom-Json; $s.results | Select-Object id,exitCode,launchError,timedOut,nonzeroWindowHandleObserved,logDrainCompleted,sharedConfigSha256Unchanged`.
  - 最小修复范围: 无矩阵 loader 修复项.
  - 尚缺实机证据: 没有 UI, form custom-run entry, card pool contents, starting deck, save restore, multiplayer 或用户 Steam 安装证据.

## 进行中

- [P2, 源码级风险] ProcessExit 与 in-flight unsettled Apply 的竞态可能在退出标记后重新挂回 AssemblyLoad.
  - 路径与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:99-123,432-464,495-525`.
  - 触发条件: `OnProcessExit` 将 `_shutdownRequested=true` 并把 `_hooked=false` 后, 一个此前已进入 `ExecuteApplyCore` 且最终 `settled=false` 的 Apply 才返回.
  - 契约: Godot/process teardown 后不应重新注册通知源, 不应让任何后续回调接触已释放对象.
  - 当前控制流: `ExecuteApplyCore` 的 `settled=false` 分支无 shutdown guard, 直接调用 `HookAssemblyLoad`; `HookAssemblyLoad` 自身也无 `ShouldStopNotifications` 检查, 因此理论上可再次订阅. 后续 `OnAssemblyLoad` 首行会因 `ShouldStopNotifications` 直接返回, 所以当前证据更像残留事件引用风险, 不是已证明的 native use-after-free.
  - 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs' -Pattern 'OnProcessExit|HookAssemblyLoad|ExecuteApplyCore|ShouldStopNotifications'`.
  - 最小修复范围: 在 `HookAssemblyLoad` 入口增加 `ShouldStopNotifications` guard, 并在 `ExecuteApplyCore` 的 retry-source bookkeeping 前再次检查 shutdown; 不改 bridge contract.
  - 尚缺实机证据: 没有在 Godot exit 同时触发 AssemblyLoad/retry 的压力复现, 不能把该竞态称为实机 crash.

- [P1, 隔离启动证据中的外部问题] AutoAnthony 自身在 m6,m7,m8 报告 `Expected 65 complete v111 Colorless cards, found 76`.
  - 路径与行号: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\m6-baselib-spire1-autoanthony\stderr.log:29-33`; 同路径 m7 `stderr.log:12-16`; 同路径 m8 `stderr.log:12-16`.
  - 触发条件: AutoAnthony 在该隔离 game 和现有 mod 内容上执行 `CaptureOriginalColorlessCards`.
  - 契约: 本轮只要求 loader 交叉启动和 optional bridge fail-closed; 若要宣称 AutoAnthony chaos gameplay 可用, 还必须先消除它自己的 startup initialization failure.
  - 当前控制流: m6/m7/m8 的 `Spire1` initializer 和 loader 都成功, m6 记录 `core=True, third-party=Pending`, m7 记录 `LegacyBridge`, m8 记录 `OfficialAddon`; 但 error stack 位于 `AutoAnthony.ChaosRunDefinitions`, 没有 `TypeLoadException` 或 `FileNotFoundException`, 因此不能归因于 Spire1 bridge.
  - 可复现命令: `Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\m6-baselib-spire1-autoanthony\stderr.log' -Pattern 'Expected 65|AutoAnthony'`.
  - 最小修复范围: 先核对 AutoAnthony 与游戏 card registry 的版本/内容契约; 当前证据不足以要求修改四个审查目标文件.
  - 尚缺实机证据: 没有在兼容版本组合中实际进入 chaos run, 验证随机卡池, 起手牌或 Watcher bridge.

## 未知

- 请求/工件标称日期为 2026-10-03, 会话适用日期为 2026-10-02. r29 目录,脚本,Release DLL 和日志都带 2026-10-03 时间戳, 属于相对于本会话的未来标识. 本报告将其作为已存在的本地工件读取, 没有由本会话重新启动游戏, 构建, 部署或修改共享配置. 未来标识的生成来源和可审计执行者 Unknown.
- 当前安全会话未暴露可验证的解析模型和 provider route. 实际解析模型: Unknown. provider route: Unknown. 未从请求文字推断, 也没有再委派子代理.
- 用户所称的实机行为证据: Unknown. 已确认的 r29 只是 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-r21-20261002\game` 的 headless non-Steam 隔离启动, `matrix-summary.json:518` 明确排除 UI, save, multiplayer 和 form combat acceptance.
- 本轮没有重新构建或部署, 没有写入产品代码, 没有操作游戏进程, 没有修改共享 `mod_configs`. 唯一写入路径为本报告文件.