# AutoAnthony incompatible latch supervisor r13 retry - 2026-10-04

监督范围:
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs
实现者报告: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-incompatible-latch-worker-r13-20261004.md
本轮指定模型/路由: global:deepseek-v4.1-flash / gateway/wb2api / max (未从会话元数据核验实际解析路由)
本轮为只读监督: 未修改产品代码, 未构建, 未测试, 未部署, 未启动游戏, 未触碰 Steam 安装或共享 mod_configs, 未写 C:
证据级别: 源码静态证据 (git diff + 全文逐行复核 + rg 扫描 + 只读文件元数据); 无实机证据

## 已确认

### A1 (P3, 门禁 1) AutoAnthony 缺席仍可等待晚加载 - PASS(源码静态)
- 优先级: P3.
- 绝对路径与准确行号:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:236-240 (AaAssembly == null 时记 Info 并提前返回, _reflectionState 保持默认 NotResolved, 不进入 MarkIncompatible)
  - 同文件:305-308 (AaAssembly == null 时 NeedsRetryWithoutAssemblyLoad 返回 false, 不创建周期源)
  - 同文件:57-62, :132 (ReflectionState 默认值 0 = NotResolved)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:456-475 (settled=false 分支始终 HookAssemblyLoad; NeedsPeriodicRetry=false 时仅 StopRetryTimer(permanent:false))
  - 同文件:588-607 (OnAssemblyLoad 对能力程序集请求 Apply), :867-870 (IsCapabilityAssembly 含 AutoAnthony/Watcher/AutoAnthonyWatcher)
  - 同文件:507-556 (HookAssemblyLoad 订阅 AppDomain.AssemblyLoad), :558-586 (UnhookAssemblyLoad 仅在 settled=true 分支 :453 调用)
- 触发条件: 进程启动时 AppDomain 尚无 AutoAnthony 程序集.
- 契约: 缺席不得被判定为 Incompatible; AssemblyLoad 观察必须保留; 晚加载后必须有一次完整解析机会.
- 控制流: Apply :236 提前返回 false -> ExecuteApplyCore :463 挂 AssemblyLoad -> NeedsPeriodicRetry :464 false -> StopRetryTimer(permanent:false) :474 -> 晚加载 AutoAnthony 触发 OnAssemblyLoad :588-607 -> RequestApply -> Apply 再次进入 ResolveReflection(状态 NotResolved, :360/:364 两个短路都不成立).
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "AaAssembly == null|HookAssemblyLoad|UnhookAssemblyLoad|IsCapabilityAssembly" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
  - rg -n "ReflectionState|_reflectionState" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs
- 最小修复范围: 无(通过).
- 尚缺实机证据: 未在运行时注入 "启动缺席 -> 后加载 AutoAnthony" 时序; 本项仅为源码静态证据.
### A2 (P3, 门禁 2) 已加载但不兼容时单次主要 Error,无每秒解析/刷日志,周期 retry 停止 - PASS(源码静态)
- 优先级: P3.
- 绝对路径与准确行号:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:358-368 (ResolveReflection 入口: Ready 直接 true; Incompatible 直接 false, 不再反射)
  - 同文件:375, :382, :401, :423 (MarkIncompatible 仅 4 个失败返回点, 单次调用只经过其中一个)
  - 同文件:429-434 (MarkIncompatible: :431 先置 Incompatible, :432 记录唯一主要 Error)
  - 同文件:242-247 (Apply 失败分支不再重复日志)
  - 同文件:310-314 (Incompatible 时 NeedsRetryWithoutAssemblyLoad=false)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:431-440 (Apply 调用点), :456-475 (settled=false: HookAssemblyLoad, NeedsPeriodicRetry=false -> StopRetryTimer(permanent:false)), :764-797 (StopRetryTimer 语义), :799-838 (timer 回调只做通知), :893-899 (EnsureRetryWakeSourceIfNeeded)
- 触发条件: AutoAnthony 程序集已加载, 但 ChaosCardGenerator.GeneratedCharacter / AutoAnthony.ChaosRunDefinitions / AutoAnthony.ChaosCardRegistry 或 ResolveReflection 中任一必需 getter/method 无法解析, 或解析过程抛异常.
- 契约: 同一不兼容字节生命周期内只记录一次主要 Error; 不再每秒重复反射解析与刷日志; 周期 retry 停止; AssemblyLoad 观察保留.
- 控制流(首次失败): Apply :236 通过(程序集在场) -> ResolveReflection :372-401 失败 -> MarkIncompatible :431 置终态 -> :432 单条 Error -> Apply :242-246 静默返回 false -> ExecuteApplyCore :463 HookAssemblyLoad -> :464 NeedsPeriodicRetry -> NeedsRetryWithoutAssemblyLoad :310-314 false -> :474 StopRetryTimer(permanent:false). 之后任何 Apply 在 :364-368 静默 false, :242-246 不记日志, :464 仍 false, 不再创建周期源.
- 并发/串行化: 全仓 Apply 唯一调用点 LoadHook:434; ApplyOnMainThread:346-367 以 _applyInProgress 串行化, 嵌套 AssemblyLoad 请求合并为 _applyPending(:348-354) 并在 :396-407 单次重排, 不存在两个线程同时进入 MarkIncompatible.
- 附加证据(异常安全): :431 在 :432 日志之前置位; 即使 MainFile.Logger.Error 抛异常, 异常经 Apply -> ExecuteApplyCore :436-440 捕获后 settled=false, NeedsPeriodicRetry 仍为 false, 周期源不会恢复.
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "MarkIncompatible|ReflectionState.Incompatible|StopRetryTimer|NeedsPeriodicRetry" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
  - rg -n "AutoAnthonyCompatBridge.Apply" mod/
- 最小修复范围: 无(通过).
- 尚缺实机证据: 未在运行时用真实不兼容 AutoAnthony 版本复现 "单次 Error + timer 停止", 未统计实机日志行数; 本项为源码静态证据.
### A3 (P3, 门禁 3) 普通 Pending / partial rollback / OfficialAddonPending 仍可重试 - PASS(源码静态)
- 优先级: P3.
- 绝对路径与准确行号:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:45-52 (ThirdPartyCapabilityState 五态), :74-76 (能力状态与补丁列表), :310-314 (Incompatible 短路在 core 状态之前, 只在该 latch 下生效), :316-323 (未完成 core / 任一 partial 列表非空 -> true), :330-333 (OfficialAddonPending -> true), :338-341 (official addon 已加载且未 settled -> true), :343-348 (Pending/Unsupported 且可选程序集在场 -> true)
  - 同文件:454-496 (TryApplyCoreGroup 对 partial 先回滚, 失败保持 pending), :498-514 (RollbackPatchedMethods), :534-563 (PatchThirdPartyEntries 异常回滚与状态保持), :565-662 (PatchThirdPartyEntriesCore: OfficialAddonPending :580-583 保持 retryable; Unsupported :584-590; LegacyBridge :591-596 保持观察; 部分安装 :642-653 回滚并保持 Pending), :664-690 (SetOfficialWatcherCapability: 回滚失败 -> OfficialAddonPending :679-681, 成功 -> OfficialAddon :687-689)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:464-475 (NeedsPeriodicRetry=true 时 EnsureRetryWakeSource, false 时 StopRetryTimer(permanent:false)), :893-899 (EnsureRetryWakeSourceIfNeeded)
- 触发条件: core 反射已 Ready 或尚未解析, 但 core group 未完成 / 有 partial 补丁待回滚 / 官方 addon 处于 OfficialAddonPending / 可选 Watcher 程序集在场但尚未 settled.
- 契约: Incompatible latch 之外的原有可前进状态必须保持周期重试能力; 新 latch 不得吞掉这些路径.
- 控制流: NeedsRetryWithoutAssemblyLoad 仅在 :310 命中 Incompatible 时提前 false; :316-323 的 core/partial 判定与 :330-348 的 optional 判定保持原逻辑, 与 git 基线逐行一致(git diff 仅新增 :310-314 一个短路块, 未改动后续分支).
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "OfficialAddonPending|ThirdPartyPartialPatchedMethods|TryApplyCoreGroup|SetOfficialWatcherCapability" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs
  - git diff -U3 -- mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs | Select-String -Pattern "NeedsRetryWithoutAssemblyLoad" -Context 0,30
- 最小修复范围: 无(通过).
- 尚缺实机证据: 未在运行时注入 partial rollback 失败 / OfficialAddonPending 时序; 本项为源码静态证据.
### A4 (P3, 门禁 4) AutoAnthonyWatcher 晚加载 / 官方接管 / 旧 patch 清理 - PASS(源码静态)
- 优先级: P3.
- 绝对路径与准确行号:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:242-247 (Incompatible 下 Apply 在 PatchThirdPartyEntries 之前返回, :275 的 PatchThirdPartyEntries 不可达)
  - 同文件:249-270 (core group 安装仅在 ResolveReflection 成功后执行), :275-283 (third-party capability 仅在此后调用)
  - 同文件:534-563, :565-662 (PatchThirdPartyEntries/Core 状态机), :664-690 (SetOfficialWatcherCapability 接管与回滚)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:456-475 (settled=false 时 HookAssemblyLoad 保留, 周期源按 NeedsPeriodicRetry 决定), :588-607 (AssemblyLoad 通知仅对能力程序集), :867-870 (含 AutoAnthonyWatcher)
- 触发条件: core 反射已判定 Incompatible 后, AutoAnthonyWatcher(或 Watcher)程序集晚加载.
- 契约: 官方 addon 晚加载不得被 latch 误阻断; 必须保留 AssemblyLoad 观察; 不得产生半套 patch 或无限周期热循环.
- 控制流: 晚加载 -> OnAssemblyLoad :597-607 -> RequestApply -> 主线程 Apply -> :236 程序集在场 -> :242 ResolveReflection -> :364-368 静默 false -> Apply :244-246 返回 false -> ExecuteApplyCore :463 HookAssemblyLoad(已订阅则 no-op) -> :464 NeedsPeriodicRetry -> :310-314 false -> :474 StopRetryTimer(permanent:false). 该路径不进入 :275 PatchThirdPartyEntries, 因此不改变 _thirdPartyCapabilityState, 也不执行任何 unpatch.
- 半套 patch 分析: 不兼容态只能从 NotResolved 进入(Ready 在 :360-363 永久短路, 不可再变 Incompatible); 而 third-party patch 与 core group patch 都要求先通过 ResolveReflection 并执行到 :249-275. 因此 Incompatible 状态下 FromPartialPatchedMethods / PoolDeckPartialPatchedMethods / ThirdPartyPatchedMethods / ThirdPartyPartialPatchedMethods 必然全空, 不存在需要官方接管清理的旧 patch. 官方 addon 自身经 AutoAnthony 官方扩展 API 注册, 与本桥无编译期/运行期强耦合(见 :519-522 注释).
- 周期热循环分析: latch 后 NeedsRetryWithoutAssemblyLoad 恒 false; 每次 AssemblyLoad 通知最多触发一次主线程 Apply(串行化, :346-367), 返回后不再排周期源; StopRetryTimer(permanent:false) 不会置 _retrySourcesStopped, 但只有 NeedsPeriodicRetry=true 才会重新创建源, latch 下不成立.
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "PatchThirdPartyEntries|SetOfficialWatcherCapability|OfficialAddonPending|NeedsPeriodicRetry|HookAssemblyLoad" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
  - git diff -U3 -- mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs | Select-String -Pattern "MarkIncompatible" -Context 2,8
- 最小修复范围: 无(通过). 说明: 若未来要求 latch 后仍执行旧 Watcher 清理, 需先证明存在"Ready 后变 Incompatible"的路径; 当前源码不存在该路径.
- 尚缺实机证据: 未在运行时注入 "不兼容 AutoAnthony + 晚加载 AutoAnthonyWatcher" 时序; 本项为源码静态证据.
### A5 (P3, 门禁 5) 无 AutoAnthony/Watcher/AutoAnthonyWatcher 编译期硬引用; 主线程/Harmony 边界与 fail-closed 语义未回退 - PASS(源码静态)
- 优先级: P3.
- 绝对路径与准确行号:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:32-38 (注释声明无编译期引用; 无 <Reference Include="AutoAnthony">, 无 SPIRE1_AUTOANTHONY DefineConstants)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:1-6 (using 仅 System.Reflection/HarmonyLib/MegaCrit 核心/Spire1 自身), :114-117 (程序集名与类型名为 const string), :142-146 (AaAssembly/AaType 反射入口), :372-394 (全部成员经 AaType/AccessTools 解析)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:1-6 (using 无 AA/Watcher 类型), :867-870 (IsCapabilityAssembly 字符串比较)
  - 同文件:322-410 (ApplyOnMainThread 主线程检查 :329-337, _applyInProgress 串行化 :346-367, 嵌套请求合并 :396-407), :417-440 (ExecuteApplyCore 防御性主线程复查 :424-429), :507-556 (HookAssemblyLoad 订阅), :625-640 (EnsureRetryWakeSource 不抛)
  - 同文件:63-72 (TryApplyBridge 唯一入口), MainFile.cs:277 (调用点)
- 触发条件: 任意运行环境(含未安装 AutoAnthony/Watcher 的用户).
- 契约: 不得出现 AutoAnthony/Watcher/AutoAnthonyWatcher 的编译期硬引用; 所有 Harmony 操作只在 Godot 主线程执行; 失败不得逃逸到加载器/初始化器.
- 证据:
  - 源码层: 两文件无 using/typeof/强类型成员引用 AA 程序集; csproj 无 AA 引用与条件符号.
  - 编译产物佐证(注意: 二进制为 2026-09-30 旧构建, 不包含本轮 latch 改动, 仅佐证"无硬引用"这一既有性质): workshop/content/Spire1/Spire1.dll 元数据 AssemblyRef 列表为 0Harmony/BaseLib/GodotSharp/sts2/System.* 共 12 项, 不含 AutoAnthony/Watcher/AutoAnthonyWatcher(命令见下).
  - 本轮改动(worker 报告 + git diff 核对): AutoAnthonyLoadHook.cs 仅 +9 行注释(无控制流改动); AutoAnthonyCompatBridge.cs 的改动全在反射解析/状态/latch 与注释, 未触碰 Harmony 调用边界.
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n -i "AutoAnthony|Watcher" mod/Spire1.csproj
  - rg -n "^using" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
  - 只读 AssemblyRef 扫描: 见本报告 A5 证据说明使用的 System.Reflection.Metadata 只读脚本; 结果 12 项不含目标程序集.
- 最小修复范围: 无(通过).
- 尚缺实机证据: 旧构建 AssemblyRef 结论已由只读元数据扫描证实, 但本轮源码未构建, 无法对"当前源码编译产物"重复该扫描; 实机缺失 AA 启动路径未在本轮运行.
### A6 (P3, 门禁 6) 反射缓存仅在完整成功后提交; 失败状态清晰且异常安全; 改动最小无 stub - PASS(源码静态)
- 优先级: P3.
- 绝对路径与准确行号:
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:352-425 (ResolveReflection 事务式重写)
  - 同文件:372-383 (局部解析枚举与两个类型; 任一 null -> MarkIncompatible 直接返回, 静态缓存零写入)
  - 同文件:385-402 (9 个成员全部解析到局部变量后统一判空; 失败同样零写入)
  - 同文件:405 (VerifyEnumConstants 接收局部 Type, 不触发 GeneratedCharacterType 属性的惰性缓存)
  - 同文件:407-418 (所有必需成员确认非空后才逐个赋值静态缓存; 最后 :418 置 Ready; _originalCardsForPreservedPool 可空为既有语义 :403)
  - 同文件:421-424 (catch 全量兜底 -> MarkIncompatible; 无部分提交)
  - 同文件:429-434 (MarkIncompatible 只置状态与日志; 不写任何成员缓存)
  - 同文件:148-152 (GeneratedCharacterType 仍保留惰性属性供补丁体使用; 在 Ready 后才会被补丁体调用, 不在解析阶段触发)
  - G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:1-917 (git diff 仅 +9 行注释, 无 stub/无测试替身; 见 git diff --numstat 9/0)
  - 改动最小性: git diff --numstat 为 AutoAnthonyCompatBridge.cs 88+/33-, AutoAnthonyLoadHook.cs 9+/0-; 两文件之外无本轮产品代码改动(时间戳窗口内 mod/ 仅这两文件被修改, 见 git diff --stat -- mod/Spire1Code/Interop/ 与只读文件时间戳证据)
- 触发条件: 任何一次 ResolveReflection 调用(AutoAnthony 在场); 或解析过程抛异常.
- 契约: 缓存只在完整解析成功后写入; 失败不留下部分成功状态; 异常安全; 无 stub/假测试.
- 控制流: 解析全部局部 -> 判空 -> VerifyEnumConstants(纯日志) -> 提交 -> Ready; 任一失败分支在提交前 return MarkIncompatible. 由于 Apply 仅在 Ready 后才执行 PatchFrom/PatchPoolsAndDecks(:249-270), 失败时不会安装任何 core 补丁, 也不会出现 "补丁已挂但缓存半套" 的组合.
- 边界(仅记录, 不构成本轮失败): :393 的 `new[] { generatedCharacterType, typeof(int) }` 在编译器角度为 Type?[]; 运行时实际元素非空(AccessTools.Method 只做签名匹配), 且项目未开启 TreatWarningsAsErrors(Spire1.csproj:18 仅 NoWarn MSB3270), 预期为 nullable 提示级而非编译错误; 本轮未构建, 无法出具编译结论, 见 ## 未知.
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1):
  - rg -n "Type\? generatedCharacterType|MarkIncompatible|_reflectionState = ReflectionState.Ready" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs
  - git diff --numstat -- mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
- 最小修复范围: 无(通过).
- 尚缺实机证据: 未构建/未测试; 未注入解析中途抛异常路径; 本项为源码静态证据.
## 进行中

- 无. 六个检查面(A1-A6)已全部完成并落盘; 本报告已收尾.

## 未知

- 未构建 / 未编译: 本轮按请求只读, 未运行 dotnet build 或任何编译器; 当前源码能否通过编译未验证. 已知待观察点: AutoAnthonyCompatBridge.cs:393 的 `new[] { generatedCharacterType, typeof(int) }` 为 Type?[] 元素类型, 预期仅 nullable 提示(项目未开 TreatWarningsAsErrors), 但需构建确认.
- 未测试 / 未部署 / 未启动游戏: 无任何运行时证据; 未统计实机日志行数.
- 未实机复现: 真实不兼容 AutoAnthony 版本下的 "单次主要 Error + 周期 retry 停止"; 未注入 AutoAnthony 缺席 -> 晚加载时序; 未注入 AutoAnthonyWatcher 晚加载/官方接管时序; 未注入 partial rollback 失败与 OfficialAddonPending 时序.
- 未验证边界: 同一进程内 AutoAnthony 以不同字节二次加载(假设 .NET 程序集身份下不发生, latch 依赖该进程级生命周期假设); Timer/fallback 线程与 ProcessExit 并发竞态; MainFile.cs:277 的初始化调用时序本身未在本轮运行验证.
- AssemblyRef 佐证来自 2026-09-30 旧构建产物(不含本轮 latch 改动), 只证明 "无 AA/Watcher 硬引用" 这一既有性质; 当前源码未构建, 无新二进制可扫描.
- 本会话实际解析模型/路由未从会话元数据核验; 仅记录请求文件指定的 `global:deepseek-v4.1-flash`, `gateway/wb2api`, `max`.

## 最终结论

SUPERVISION_PASS

- 判定依据: 门禁 1-6 全部通过(源码静态证据), 见 A1-A6; 无 REWORK_REQUIRED 级问题.
- 实现与实现者报告一致: AutoAnthonyCompatBridge.cs 88+/33-, AutoAnthonyLoadHook.cs 9+/0-(全部注释); 报告所列行号与当前源码逐项核对一致(含 :57-62, :132, :236-247, :310-314, :352-434, LoadHook:456-475, :872-891).
- 证据等级: 源码静态复核(git diff + 逐行阅读 + rg + 只读文件元数据 + 只读 AssemblyRef 扫描); 未构建, 未测试, 未部署, 未启动游戏, 无实机证据.
- 本报告是监督唯一写入产物; 未修改产品代码, 未触碰 Steam 安装与共享 mod_configs, 未写 C:.
- 残留风险(不改变 PASS, 需后续流程覆盖): 编译验证与不兼容 AutoAnthony 实机复现尚未执行, 详见 ## 未知.