# AutoAnthony incompatible latch worker r13 - 2026-10-04

## 已确认

### 结论 1(编辑前基线, P3): core 反射失败无终态,导致 1s 周期重试与重复 Error

- 优先级: P3(请求文件背景段定级;静态复核唯一待修复项).
- 文件与准确行号(编辑前):
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:230-234 Apply: ResolveReflection() 为 false 时记 Error 并返回 false.
  - 同文件 :332-379 ResolveReflection: 失败(必需类型/成员缺失或抛异常)只返回 false, _reflectionReady 保持 false, 无终态标记.
  - 同文件 :285-301 NeedsRetryWithoutAssemblyLoad: 程序集在场且 !_fromPatchesApplied / !_poolDeckPatchesApplied 时恒为 true.
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:430: 全仓唯一 AutoAnthonyCompatBridge.Apply 调用点(rg 扫描确认).
  - 同文件 :452-469 ExecuteApplyCore: settled=false 时 HookAssemblyLoad() + NeedsPeriodicRetry() -> EnsureRetryWakeSource(); :677-681 创建 1s ThreadingTimer; :866-882 NeedsPeriodicRetry 直接读上述属性.
- 触发条件: AutoAnthony 程序集已加载, 但 ChaosCardGenerator.GeneratedCharacter, AutoAnthony.ChaosRunDefinitions, AutoAnthony.ChaosCardRegistry 或任一现有必需 getter/method 无法解析(版本不兼容). 程序集缺席不满足触发条件.
- 契约: 同一不兼容字节生命周期内只记录一次主要 Error, 不再每秒重复解析和刷日志; 终态下 NeedsRetryWithoutAssemblyLoad 不得维持无意义周期重试; 程序集缺席必须仍可等待晚加载.
- 当前控制流(编辑前): Apply -> ResolveReflection() false -> Error + false; core patch bits 恒 false; NeedsRetryWithoutAssemblyLoad 恒 true; 1s Timer 每 tick 重复 Apply 与 Error; AssemblyLoad 观察同时存在.
- 可复现命令(静态, 工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "ResolveReflection|NeedsRetryWithoutAssemblyLoad|_reflectionReady" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs
  - rg -n "NeedsPeriodicRetry|EnsureRetryWakeSource|AutoAnthonyCompatBridge.Apply" mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
- 最小修复范围: AutoAnthonyCompatBridge.cs 增加终态 latch, ResolveReflection 改为完整成功后才提交静态缓存, NeedsRetryWithoutAssemblyLoad 在 latch 下返回 false; AutoAnthonyLoadHook.cs 只更新相关注释, 不改控制流.
- 尚缺实机证据: 本轮不构建/不测试/不部署, 没有 "AutoAnthony 版本不兼容" 的实机复现; 以上为源码静态证据. 现有实机日志仅覆盖 AutoAnthony 缺席路径(见 G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-current-deepseek-review-20261004.md:117-122 及其引用的 stdout-final.log:317).

### 结论 2(实现落盘, P3): core 反射失败改为终态 latch, 周期重试关闭, AssemblyLoad 观察保留

- 优先级: P3(请求文件指定;本轮唯一实现项).
- 修改文件与准确行号(编辑后):
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs
    - :54-62 新增私有 ReflectionState { NotResolved, Ready, Incompatible };
    - :132 `_reflectionState` 取代原 `_reflectionReady` bool;
    - :226-232 Apply 文档注释说明终态语义; :242-246 ResolveReflection 失败不再重复记 Error(唯一一次主要 Error 由 MarkIncompatible 记录);
    - :301-350 NeedsRetryWithoutAssemblyLoad 新增 Incompatible 短路, 返回 false(判定在 :310-314);原有 Pending/partial/OfficialAddonPending 判定保持在 :316-348;
    - :352-425 ResolveReflection 改为事务式: 枚举/类型/全部成员先解析到局部变量, 必需成员齐备后才在 :407-418 原子提交静态缓存并置 Ready;
    - :427-434 新增 MarkIncompatible: 置 Incompatible 并记录唯一一次主要 Error;
    - :436-448 VerifyEnumConstants 改为接收局部解析出的 Type 参数, 不再经缓存属性取类型.
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs
    - :31-35 类文档补充终态语义; :461-462 ExecuteApplyCore 分支注释; :872-874 NeedsPeriodicRetry 文档注释. 该文件仅注释变更(git diff 为 +9 行, 全部为注释), 控制流未改.
- 触发条件: 与结论 1 相同(AutoAnthony 程序集已加载但必需类型/成员缺失或解析抛异常).
- 契约逐条核对:
  1. 同一不兼容字节生命周期内只记录一次主要 Error: MarkIncompatible(:429-434) 是唯一失败日志入口; 后续 Apply 在 :242-246 静默返回, ResolveReflection 在 :364-368 静默短路. VerifyEnumConstants 的 drift 日志保持原有行为, 只在完整解析成功后执行一次(:405).
  2. NeedsRetryWithoutAssemblyLoad 终态返回 false(:310-314), 不再维持无意义周期重试; Pending/partial/OfficialAddonPending 路径(:316-348)未改.
  3. 程序集缺席不终态: Apply 在 :236-240 提前返回, 不进入 ResolveReflection, _reflectionState 保持 NotResolved; 晚加载后第一次 Apply 仍会完整尝试解析.
  4. AutoAnthonyWatcher 晚加载路径保留: AutoAnthonyLoadHook.cs:463 HookAssemblyLoad() 仍在 settled=false 时挂载; OnAssemblyLoad(:588)对 AutoAnthony/Watcher/AutoAnthonyWatcher(IsCapabilityAssembly :867-870)触发一次性 Apply. core 终态下 Apply 在 ResolveReflection 短路处静默返回 false, 补丁从未安装, 无半套 patch 可回滚; settled 仍为 false, NeedsPeriodicRetry(:875-884)为 false, :474 停周期源, 不恢复热循环.
  5. 无新增编译期硬引用: 仅新增私有枚举 ReflectionState 与字符串/反射调用; AutoAnthony/Watcher/AutoAnthonyWatcher 仍只以字符串程序集名和 GetType 反射出现.
  6. 异常安全: 所有解析结果先落局部变量, 失败(含 catch :421-424)不写任何静态字段; 部分成功状态不可能残留; Harmony 部分补丁回滚逻辑未改.
  7. 最小改动: 仅上述两个 Interop 文件; 静态字段为进程级, 无需跨进程重置.
- 可复现命令(静态, 工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "ReflectionState|MarkIncompatible|_reflectionState" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs
  - rg -n "terminal core reflection failure|terminal incompatible-core latch|Consults the bridge state" mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
  - git diff -- mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
- 最小修复范围: 本报告所列两文件即全部产品代码改动, 未触碰其它文件.
- 尚缺实机证据: 本轮按请求不构建/不测试/不部署, 未在运行时用不兼容 AutoAnthony 版本复现终态转换; 上述为源码静态核对结果.

### 结论 3(静态核对, P3): 终态与既有路径的交互矩阵

- 优先级: P3.
- 文件与准确行号(编辑后): AutoAnthonyCompatBridge.cs:236-246(缺席/失败), :301-350(重试判定), :352-425(解析), :429-434(latch); AutoAnthonyLoadHook.cs:458-474(settled=false 记账), :588-607(AssemblyLoad 通知), :867-899(能力程序集/周期判定).
- 交互结论:
  - 程序集缺席: Apply Info 一次并返回 false; NeedsRetryWithoutAssemblyLoad 返回 false; AssemblyLoad 钩子保留 -> 晚加载仍触发第一次解析(不是终态).
  - 程序集在场且兼容: 解析成功 -> Ready; 后续 Apply 直接复用缓存, 行为与改动前一致.
  - 程序集在场且不兼容: 第一次失败即 Incompatible + 单次 Error; 之后每次 Apply 静默 false; ExecuteApplyCore 走 settled=false 分支, NeedsPeriodicRetry=false -> StopRetryTimer(permanent:false)(:474), 仅保留 AssemblyLoad 观察.
  - 不兼容后官方 addon 晚加载: AssemblyLoad 通知 -> 主线程一次性 Apply -> 在 ResolveReflection 终态短路处返回 false(补丁从未安装, 因而没有半套 patch 可回滚); ExecuteApplyCore 重新挂 AssemblyLoad 观察并因 NeedsPeriodicRetry=false 停掉周期源 -> 安全 no-op, 不恢复热循环. 该路径不再调用 PatchThirdPartyEntries, 不改变 _thirdPartyCapabilityState(官方 addon 自身的接管行为独立于本桥).
  - 不兼容后普通 Watcher 晚加载: 同路径安全 no-op; 由于 core 补丁位未置, 旧 Watcher 桥不会安装, 保持 fail-closed.
- 可复现命令: 同上结论 2 的 rg/git diff 命令; 另可 rg -n "NeedsPeriodicRetry|EnsureRetryWakeSource|StopRetryTimer" mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs.
- 尚缺实机证据: 未注入 AssemblyLoad 竞态/官方 addon 晚加载时序; 未运行游戏.

## 进行中

- 无. 本轮范围内(读取请求与源码 -> 首条证据落盘 -> 两文件最小实现 -> 静态契约核对)已完成.
- 按请求未构建, 未测试, 未部署, 未启动游戏, 未修改 Steam 安装与共享 mod_configs, 未写 C:.

## 未知

- 未构建/未编译: 源码仅做静态阅读与括号启发式检查(hook 的 -1 深度与 HEAD 基线一致, 属既有启发式噪声, 不构成编译结论);编译是否通过需后续会话按项目流程验证.
- 无实机证据: 未在运行时用不兼容 AutoAnthony 版本复现 "单次 Error + 无周期重试" 行为; 未注入 AssemblyLoad 晚加载/官方 addon 接管时序.
- 未验证边界: AutoAnthony 在同一进程内以不同字节二次加载(假定 .NET 程序集身份下不会发生); 终态 latch 依赖该进程级生命周期假设, 与请求描述一致.
- 未验证边界: Timer/fallback 线程与 ProcessExit 并发竞态本轮未注入.
- 本会话实际解析模型/路由未从会话元数据核验; 仅记录请求文件指定的 `global:deepseek-v4.1-flash`, `gateway/wb2api`, `max`.
