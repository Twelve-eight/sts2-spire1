# AutoAnthony incompatible latch supervisor r13 - 2026-10-04

监督范围:
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs (1042 行)
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs (917 行)
实现者报告: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-incompatible-latch-worker-r13-20261004.md (已显示完成: 结论 1/2/3, 进行中=无)
本轮指定模型/路由: global:deepseek-v4.1-flash / gateway/wb2api / max (未从会话元数据核验实际解析路由)
本轮为只读监督: 未修改产品代码, 未构建, 未测试, 未部署, 未启动游戏, 未触碰 Steam 安装或共享 mod_configs, 未写 C:
证据级别: 全部为源码静态证据 (git diff + 全文逐行复核 + rg 扫描); 无实机证据

## 已确认

### A1 (P3, 门禁 1) AutoAnthony 缺席仍可等待晚加载 - PASS(源码静态)
- 绝对路径与行号: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:236-240 (缺席提前返回, _reflectionState 保持 NotResolved, :238 记 Info); 同文件:305-308 (AaAssembly==null -> NeedsRetryWithoutAssemblyLoad=false, 不建周期源); G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:463 (settled=false 分支仍 HookAssemblyLoad); :588-607 (OnAssemblyLoad 仅对能力程序集请求 Apply); :867-870 (IsCapabilityAssembly 含 "AutoAnthony"/"Watcher"/"AutoAnthonyWatcher")
- 触发条件: 进程启动时 AppDomain 无 AutoAnthony 程序集
- 契约: 缺席不得被新 latch 误判为 Incompatible; AssemblyLoad 观察保留; 晚加载后仍有完整解析机会
- 控制流: Apply:236 提前返回 -> ExecuteApplyCore:463 挂 AssemblyLoad -> :464 NeedsPeriodicRetry=false -> :474 停周期源(非永久) -> 晚加载 AssemblyLoad 事件 -> OnAssemblyLoad -> RequestApply -> Apply 再次进入 ResolveReflection(状态仍为 NotResolved)
- 可复现命令(工作目录 G:\omp works\Sts\sts2-spire1): rg -n "AaAssembly == null|HookAssemblyLoad|IsCapabilityAssembly" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
- 最小修复范围: 无(通过)
- 尚缺实机证据: 未在运行时注入 "先缺席, 后晚加载" 时序

### A2 (P3, 门禁 2) 不兼容时单次主要 Error 且周期重试停止 - PASS(源码静态)
- 绝对路径与行号: AutoAnthonyCompatBridge.cs:364-368 (Incompatible 静默短路); :375, :382, :401, :423 (MarkIncompatible 唯一 4 个入口); :429-434 (latch 在 :431 先置位, 唯一主要 Error 在 :432); :242-247 (Apply 失败不再重复日志); :310-314 (Incompatible -> NeedsRetryWithoutAssemblyLoad=false); AutoAnthonyLoadHook.cs:464-474 (NeedsPeriodicRetry=false -> StopRetryTimer(permanent:false))
- 触发条件: AutoAnthony 程序集已加载, 但 ChaosCardGenerator.GeneratedCharacter / AutoAnthony.ChaosRunDefinitions / AutoAnthony.ChaosCardRegistry 或任一必需成员无法解析, 或解析抛异常
- 契约: 同一进程内只记录一次主要 Error; 不再每秒解析; 周期 retry 停止; AssemblyLoad 观察保留
- 控制流: 首次失败 -> Incompatible 置位 + 1 条 Error -> Apply 返回 false -> hook:463 保留 AssemblyLoad -> :464 为 false -> :474 停 timer/fallback -> 之后任何 Apply 在 :364-367 静默返回 false; 并发保证: Apply 全仓唯一调用点 hook:434, 且 ApplyOnMainThread:346-367 用 _applyInProgress 串行化, 不会双次进入 MarkIncompatible
- 可复现命令: rg -n "MarkIncompatible|ReflectionState.Incompatible|StopRetryTimer" mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs
- 最小修复范围: 无(通过)
- 尚缺实机证据: 未用真实不兼容 AutoAnthony 版本实机复现 "单次 Error + timer 停止", 未统计运行日志行数

## 进行中

- A3-A7 门禁复核进行中(partial/OfficialAddonPending 重试, Watcher 晚加载, 硬引用, 缓存事务, 报告一致性)
- 本报告将增量更新至收尾结论

## 未知

- 未构建 / 未测试 / 未部署 / 未启动游戏; 暂无实机证据
- 本会话实际解析模型 / 路由未从会话元数据核验