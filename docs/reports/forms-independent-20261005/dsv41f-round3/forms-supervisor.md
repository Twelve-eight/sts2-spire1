# forms-supervisor 监督报告

状态: NEEDS_REWORK (r3 冻结快照)

## 已确认

### 门禁与审查基线
- 激活门禁: `forms-supervisor.activate.request.md`; 同目录 `coordination.md` 记录 native `multi_agent_v1.wait_agent` target `01a108dc-ad9d-7463-8914-e25181a633f9`, returned `completed`, timed_out=false, 时点 2026-10-05T06:03:31.2068548+08:00. 仅许可监督, 不代表构建或实机通过.
- 中央 r3 构建失败证据: `G:\omp works\.tmp\forms-independent-20261005\forms-build-r3.log`; 2 errors, 0 warnings. 错误原文: `FormStanceWatcherBridge.cs(75,20): error CS0246: 未能找到类型或命名空间名"Node"`; `MainFile.cs(25,35): error CS0133: 指派给"MainFile.ResPath"的表达式必须是常量`.
- 冻结审查基线: `G:\omp works\.tmp\forms-independent-20261005\forms-r3-review-snapshot\`. 已逐文件 SHA256 比对, 快照 23 个文件与当时的 live `G:\omp works\Sts\sts2-forms\mod\` 全部一致 (Same=True). r4 窄修只允许改 MainFile.cs / FormStanceWatcherBridge.cs, 本报告结论只针对 r3 快照.

### 快照到产品路径映射
| 快照路径 | 产品路径 |
| --- | --- |
| `.tmp\forms-independent-20261005\forms-r3-review-snapshot\Forms.csproj` | `Sts\sts2-forms\mod\Forms.csproj` |
| `.tmp\forms-independent-20261005\forms-r3-review-snapshot\FormsCode\<file>.cs` | `Sts\sts2-forms\mod\FormsCode\<file>.cs` |
| `.tmp\forms-independent-20261005\forms-r3-review-snapshot\FormsCode\Interop\FormsRuntimeEntryPoint.cs` | `Sts\sts2-forms\mod\FormsCode\Interop\FormsRuntimeEntryPoint.cs` |

### S-01 [P1] 中央编译阻断 CS0246: 桥文件缺少 `using Godot;`
- 快照路径与行号: `forms-r3-review-snapshot\FormsCode\FormStanceWatcherBridge.cs:75` (`private static Node? _pumpNode;`), 另见 383 (`Node? root`), 386 (`((SceneTree)Engine.GetMainLoop()).Root`), 398 (`new FormStanceBridgePump()`), 424 (`GodotObject.IsInstanceValid`).
- 产品路径映射: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs`.
- 触发条件: 任意 Release/Debug 编译. 该文件 using 列表 1-19 行只有 System/Harmony/MegaCrit, 无 `using Godot;`; `Node`/`SceneTree`/`Engine`/`GodotObject` 全部无法解析.
- 宣称或权威契约: 主会话集中构建门禁要求 Forms.dll 编译成功; 中央日志 0 warning 2 error 已复现.
- 当前控制流: 编译期直接失败, 无运行期控制流.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\forms-build-r3.log'`.
- 最小修复范围: 在 FormStanceWatcherBridge.cs 顶部加 `using Godot;` (或把这 5 处完全限定). 只改该文件.
- 尚缺的实机证据: 无 (编译门禁).

### S-02 [P1] 中央编译阻断 CS0133: `MainFile.ResPath` 不是编译期常量
- 快照路径与行号: `forms-r3-review-snapshot\FormsCode\MainFile.cs:25` (`public const string ResPath = string.Concat("res://", ModId);`).
- 产品路径映射: `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs`.
- 触发条件: 任意编译. `string.Concat` 不是编译期常量表达式, 不能用于 `const` 初始化.
- 宣称或权威契约: 同上, 编译门禁.
- 当前控制流: 编译期直接失败.
- 最小修复范围: 改为 `public const string ResPath = "res://" + ModId;` 或字面量 `"res://Forms"`. 只改该文件.
- 尚缺的实机证据: 无 (编译门禁).

### S-03 [P1] Bound 后 AssemblyLoad pending 与身份核对不可达 (round3 W-06 未真正生效)
- 快照路径与行号: `forms-r3-review-snapshot\FormsCode\FormStanceWatcherBridge.cs:239-240` (`if (_state == BridgeState.Bound) return true;` 位于 `TryBindOnMainThreadEntry`).
- 产品路径映射: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs`.
- 触发条件: 桥已 Bound 后发生 Watcher AssemblyLoad 通知.
- 宣称或权威契约: 接续增量契约第 3 条 "已绑定的早返回不能绕过 AssemblyLoad pending 和身份检测"; `forms-worker.md` W-06 宣称 "Bound 早返回消费 `_assemblyLoadPending` 并核对 Watcher 程序集实例与 MVID".
- 当前控制流: `OnAssemblyLoad` (350-356) 只置 `_assemblyLoadPending`/`_assemblyLoadEpoch`; 绑定成功时 185-186 行 `EnsureAssemblyLoadSubscription` + `StopPumpLocked`; `TryBindOnMainThreadEntry` 在 Bound 时于 239-240 行直接 `return true`; 唯一调用 `TryBind()` 的位置是 247 行, 而 247 行在 239-240 行之后不可达. 因此 `TryBind()` 141-154 行的 Bound 身份核对分支在运行期不可达.
- 可复现命令: `Select-String -Path '<snapshot>\FormsCode\FormStanceWatcherBridge.cs' -Pattern 'TryBindOnMainThreadEntry|TryBind\(\)'`.
- 最小修复范围: `TryBindOnMainThreadEntry` 的 Bound 分支先消费 `_assemblyLoadPending` 并调用 `BoundIdentityStillValid()`, 不一致时 `EnterTerminalLocked`; 或让 Bound 分支委托 `TryBind()`.
- 尚缺的实机证据: 真实程序集替换/同 simple name 多程序集时序.

### S-04 [P2] Terminal 路径遗留 AssemblyLoad 订阅
- 快照路径与行号: `FormStanceWatcherBridge.cs:215-226` (catch 中 `_state = terminal ? Terminal : Retryable;` 后无条件 `EnsureAssemblyLoadSubscription()`).
- 触发条件: 安装失败被判定为 Terminal (契约签名不匹配或回滚失败).
- 宣称或权威契约: 契约第 3 节 "ShuttingDown 后禁止新绑定, 取消 AssemblyLoad 订阅"; Terminal 应同样停止消费通知.
- 当前控制流: Terminal 后订阅仍存在, `OnAssemblyLoad` 继续置 pending/epoch, 但 `PumpTick` 无 pump, `TryBindOnMainThreadEntry` 对 Terminal 直接 false, pending 无人消费. 功能上 fail-closed, 但订阅与 pending 状态泄漏.
- 最小修复范围: Terminal 分支调用 `RemoveAssemblyLoadSubscription()` 并清 `_assemblyLoadPending` (与 `EnterTerminalLocked` 595 行一致).
- 尚缺的实机证据: 部分 patch 失败路径.

### S-05 [P2] Terminal 不清 Spire1 反射通知缓存
- 快照路径与行号: `FormStanceWatcherBridge.cs:579-608` (`EnterTerminalLocked` 清 `_binding`/`_nativeNotification`/`_harmony` 并退订 AssemblyLoad, 但未调用 `Spire1StanceNotification.Reset()`); 对比 `Shutdown()` 306 行调用 `Spire1StanceNotification.Reset()`.
- 触发条件: Bound 后检测到 Watcher 身份变化进入 Terminal, 或绑定/回滚失败进入 Terminal, 而 `Spire1StanceNotification` 已缓存 Spire1 的 MethodInfo.
- 宣称或权威契约: 契约第 3 节 "清空 pending/native markers/兼容通知缓存/泵"; worker W-08 宣称 Shutdown 调用 `Reset()`.
- 当前控制流: Terminal 后 bridge patches 已撤, `AfterStanceChanged` 不再运行, 所以正常路径不会调用 Dispatch; 但缓存旧静态 MethodInfo 仍驻留, 若未来有代码直接调用该通知入口则可能复用旧身份.
- 最小修复范围: `EnterTerminalLocked` 末尾调用 `Spire1StanceNotification.Reset()`.
- 尚缺的实机证据: 真实替换/同 simple name 多程序集.

### S-06 [P3] `AfterMarkerRemoved` 关闭竞态检查无代数绑定
- 快照路径与行号: `FormStanceWatcherBridge.cs:848-863` (`finally { if (CallbacksAllowed) { foreach ... await PowerCmd.Remove(carrier); } }`).
- 触发条件: await 原生 `original` 期间发生 Shutdown/Terminal.
- 宣称或权威契约: 接续增量契约 "已进入的 async callback 及 await 后续以绑定代数核对".
- 当前控制流: `CallbacksAllowed` 只检查当次 `_state == Bound`, 未捕获 `BindingLease`; 检查通过后循环内 `await PowerCmd.Remove` 期间仍可能关闭. 只调用 sts2 原生 `PowerCmd.Remove`, 不触碰已清空的 bridge delegate, 因此风险低于 S-03.
- 最小修复范围: 进入 finally 时 `TryAcquireLease` 并每轮 `IsLeaseCurrent`, 或在关闭后跳过.
- 尚缺的实机证据: 退出/回滚真实路径.

### S-07 [P3] `_assemblyLoadPending` 非易失读
- 快照路径与行号: `FormStanceWatcherBridge.cs:354` (`Volatile.Write(ref _assemblyLoadPending, true)`), 145 行与 244 行普通读.
- 触发条件: AssemblyLoad 线程写 pending, 主线程 Bound/主线程入口读 pending.
- 宣称或权威契约: 生命周期契约要求跨线程通知可被主线程可靠消费.
- 当前控制流: 写入用 `Volatile.Write`, 读取在 `lock (Gate)` 内但未用 `Volatile.Read`; 实际 x86/ARM 上 bool 撕裂风险低, 但内存序语义未显式.
- 最小修复范围: 读取处改 `Volatile.Read(ref _assemblyLoadPending)`, 或在 OnAssemblyLoad 内用 `lock (Gate)`.
- 尚缺的实机证据: 高并发/弱内存序平台未测.

### C-01 [P3] 10 个 CustomID 与公开签名保留
- 快照路径: `FormsCode\*.cs` 逐个 grep 命中 10 个 `SPIRE1-*` CustomID (与契约第 2.4 节一致).
- 快照路径与行号: `FormsCode\Interop\FormsRuntimeEntryPoint.cs:21-70` 公开签名 IsAvailable/IsSelected/KindOf/CurrentKind/Enter/Exit 与契约 2.3 一致; SHA256 `244F13EE0E13...` 与 `forms-worker.md` C-01 一致.
- 资源: `mod\Forms\` 下 calm/wrath/divinity + big 三套 png 与 eng/zhs localization 存在; `Forms.csproj:68-72` 已删除默认自动复制目标, 无 ModsPath 写入路径 (静态).

## 进行中

- 继续只读核对: 主线程 pump 生命周期与有界重试细节, MainFile Harmony 全成功门禁与 Shutdown 覆盖, await 续体代数其它点, 非 Forms 原生语义, 资源/CustomID 交叉一致性.
- 以上均针对 r3 冻结快照; r4 两个窄修文件的未来改动不在本报告结论内.

## 未知

- 独立 Forms.dll 被 ModelDb 发现/实例化的真实时序.
- Watcher 晚加载到主线程绑定的端到端实机路径; 真实程序集替换/同 simple name 多程序集.
- 旧档/联机身份, UI/PCK 实机显示, 长战斗, 热替换.
- 本轮监督不构建, 不测试, 不部署, 不运行游戏; 上述实机面均未验证.

## 结论 (r3 冻结快照)

- 判定: NEEDS_REWORK.
- 阻断性: S-01 与 S-02 为中央构建实测的两个编译错误, 任一存在则 Forms.dll 无法产出, r3 交付不成立.
- 契约未达成: S-03 表明 round3 声称修复的 "Bound 早返回绕过身份检测" (worker W-06) 在运行期不可达, 属实现与自述不一致.
- 次级风险: S-04 Terminal 订阅泄漏, S-05 Terminal 未清 Spire1 通知缓存, S-06 await 后续未用代数核对, S-07 pending 读未显式 volatile. 均不影响编译, 但应在 r4 或后续一并收敛.
- 已保留: 10 个 CustomID, 公开入口签名, 六效果数值, 资源路径, 默认自动部署删除.
- r4 注意: 本次日志只暴露 2 个错误; 修掉后编译器可能暴露更多错误或警告, 不得把 "2 errors 清零" 当作构建通过, 必须以 r4 完整构建日志为准.
- 本判定不构成实机验收; 全部实机面见 "未知".
