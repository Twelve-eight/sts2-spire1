# Forms 同批监督报告

- 角色: 同批只读监督
- 模型: global:deepseek-v4.1-flash
- 路由: wb2api
- reasoning: xhigh
- 状态: WAITING_GATE
- 记录时间: 2026-10-05 06:21:31 +08:00
- 请求文件: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-supervisor.request.md

## 已确认

- 已读取请求文件, 并登记 WAITING_GATE.
- 本回合未读取实现者正在写入的最终产物, 未自行轮询, 未再委派.
- 门禁条件: 必须等待 hub 对精确同批 worker 的真实 multi_agent_v1.wait_agent returned completed, 并提供 id/time/coordination 证据.

## 进行中

- 等待 hub 提供上述真实 completed 门禁证据; 收到前不进入最终源码审查.

## 未知

- 同批 worker 的最终产物状态.
- 最终监督结论 SUPERVISION_PASS 或 NEEDS_REWORK.
- 实机验证面与未覆盖边界.

## 2026-10-05 门禁与首条证据 (SUPERVISION_IN_PROGRESS)

### 门禁核验 (已确认)
- coordination.md 记录主会话真实 multi_agent_v1.wait_agent, targets 为 01a10901-e1f3-7f52-8d72-0ef5d3cf705f 与 01a10901-e2c3-79d1-9fa3-8c90ef1f5310, 两目标 completed, timed_out=false, 收割窗口 2026-10-05 07:29-07:33 +08:00. 证据路径: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\coordination.md 第 14-17 行.
- 本角色对应 worker 为 01a10901-e1f3-7f52-8d72-0ef5d3cf705f, 门禁满足. 未再委派, 未构建/测试/部署/运行游戏/改产品代码.

### S-HASH-01 [P1] worker 报告声明的最终 SHA256 与 live 文件不一致 (已确认)
- 绝对路径: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs
- worker 声明: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-worker.md 第 68 行与第 86 行, 声称最终 SHA256 为 6813383ADEAA1764A90A8AE5839EC8C95AB56F49C15EB06174F835F0754912DF.
- 实测: Get-FileHash -LiteralPath '<上述绝对路径>' -Algorithm SHA256 得到 140A85DD310D74C284E01E73B73E89D3C0CEF71F920193CAD9308E442126AAB0, 文件长度 59006, LastWriteTime 2026-10-05 06:36:44 +08:00.
- 排除项: raw_noBom 仍为 140A85DD...; LF 归一化 B5BF5170...; CRLF 归一化 F4737DD7...; 均不等于声明的 6813383A.... 该文件含 CRLF.
- 触发条件: 任何以 worker 声明 SHA 作为"交付版本"的复现/比对/后续收割.
- 当前控制流: 报告 mtime 2026-10-05 06:38:19 晚于产品文件 mtime 06:36:44, 说明不是"报告后文件又被改"; 声明值无法对应磁盘上的任何归一化形态.
- 最小修复范围: worker 报告勘误, 以 live 实测 SHA256 重写第 68/86 行; 或者若声明值对应另一临时副本, 必须给出该副本绝对路径并重新冻结.
- 尚缺证据: 无 (哈希与长度已实测). 影响: "以第三批最终 SHA 与结构自检为准"的交付锚点失效, 监督不能以该 SHA 认定被审版本.

### S-03/S-04/S-05/S-07 复核 (已确认, 但受 S-HASH-01 阻断)
- 绝对路径: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs
- S-03 [P1]: 现行第 251-280 行 TryBindOnMainThreadEntry 在 Bound 分支先 StartPumpLocked() (第 260 行), 再 Volatile.Read(_assemblyLoadPending) (第 263 行); 无 pending 才第 264 行 return true, 有 pending 走出锁区第 276 行 TryBind(). TryBind() 第 147-161 行消费 pending (第 153 行) 并 BoundIdentityStillValid() (第 154 行), 不一致 EnterTerminalLocked (第 158-159 行). 绑定成功第 191 行 EnsureAssemblyLoadSubscription(), 第 195 行 StartPumpLocked(), 不再 StopPump. 触发条件: Bound 后 Watcher AssemblyLoad; 旧 native delegate 门禁在第 576-591 行 TryAcquireLease 与第 593-603 行 IsLeaseCurrent, 均拒绝 pending.
- S-04 [P2]: 第 232-243 行 catch 中 terminal 走 EnterTerminalLocked(reason) (第 236 行), 仅 Retryable 第 242 行 EnsureAssemblyLoadSubscription(); EnterTerminalLocked 第 658-688 行清 pending (663), 清 binding/boundAssembly (664-667), StopPumpLocked (672), RemoveAssemblyLoadSubscription (673), Spire1StanceNotification.Reset (675). 回滚失败第 225-230 行强制 terminal, 满足"不重试叠 patch".
- S-05 [P2]: 第 675 行确认 EnterTerminalLocked 调 Spire1StanceNotification.Reset(); MarkOwnerPatchesFailed 第 344-352 行复用 EnterTerminalLocked (第 350 行).
- S-07 [P3]: pending 读全部 Volatile.Read (第 94, 151, 263, 505, 583, 599, 619 行), 写 Volatile.Write (第 153, 166, 300, 381, 449, 465, 663 行); OnAssemblyLoad 第 375-383 行只置 pending/epoch, 不触 Godot/Harmony.
- 尚缺证据: 这些是源码静态证据; 未构建/未实机, 且本报告不把静态推理称为实机通过. S-03 的真实替换时序仍未知.

### S-06 复核 (已确认)
- 绝对路径同上, 第 928-951 行 AfterMarkerRemoved. 第 933 行 await 前 TryAcquireLease 捕获 BindingLease; 第 940 行 await 后 IsLeaseCurrent 门禁; 第 942-948 行每轮 PowerCmd.Remove 前第 945 行再次 IsLeaseCurrent. 触发条件: await original 期间 Shutdown/Terminal/pending. 关闭即 finally 内不产生 Remove 副作用.
- 尚缺证据: 退出/回滚真实路径未跑; 仅静态.

### 第 4 项主线程边界复核 (已确认的源码/API 证据)
- ModelDb.Init 主线程证据: G:\omp works\.tmp\mpcs-sts2\src\MegaCrit.Sts2.Core.Helpers\OneTimeInitialization.cs 第 68 行 ExecuteEssential(), 第 81 行 ModelDb.Init(); G:\omp works\.tmp\mpcs-sts2\src\MegaCrit.Sts2.Core.Nodes\NGame.cs 第 554 行 _mainThreadId = Environment.CurrentManagedThreadId, 第 556 行 TaskHelper.RunSafely(GameStartupWrapper()), 第 649 行 GameStartup(), 第 661 行 OneTimeInitialization.ExecuteEssential(). 因此 ModelDb.Init postfix 可视为已知 Godot 主线程入口.
- ProcessExit 无主线程保证: G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs 第 147 行 AppDomain.CurrentDomain.ProcessExit += OnProcessExit; 第 202 行 OnProcessExit -> Shutdown. 现行 StopPumpLocked 第 440-477 行: 无节点任意线程安全 (443-451); 有节点仅当第 456-457 行当前托管线程 == 创建线程才 IsInstanceValid/QueueFree (470-471), 否则第 459 行只置 _pumpStopRequested.
- Godot API 证据: G:\omp works\.nuget-packages\godotsharp\4.5.1\lib\net8.0\GodotSharp.xml 第 5660-5665 行只说明 QueueFree 延迟到帧末且可重复调用, 未声明线程安全; 本报告不宣称其跨线程安全.

### 主会话勘误与冻结锚点 (已确认, 收敛 S-HASH-01)
- 勘误来源: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-hash-errata-notice.txt 与 coordination.md 末段 (第 19-22 行). 主会话声明 worker 摘要 6813383A... 不作验收身份, 以 live 实测 140A85DD... 为准, 并冻结快照.
- 冻结核验: G:\omp works\.tmp\forms-independent-20261005\forms-r5-review-snapshot\FormStanceWatcherBridge.cs 与 live G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs 均 SHA256=140A85DD310D74C284E01E73B73E89D3C0CEF71F920193CAD9308E442126AAB0, 长度 59006, 逐字节 SequenceEqual=True.
- 结论: S-HASH-01 由报告摘要错误 + 主会话勘误构成, 不再阻断源码验收; 但保留为报告诚实性记录 (worker 报告第 68/86 行摘要仍与 live 不符, 未修正). 源码问题与报告锚点问题已分离.
- 门禁记录: G:\omp works\.tmp\forms-independent-20261005\r5-completion-gate.json 记录 targets 01a10901-e1f3-7f52-8d72-0ef5d3cf705f / 01a10901-e2c3-79d1-9fa3-8c90ef1f5310 均 completed, TimedOut=false, ToolCallIdentifier=Not provided, ReportedWorkerSha256 与 LiveSha256 不同且 HashDisagreement=true. 与 coordination.md 一致, 不伪造调用标识.

### 结构自检 (已确认)
- FormStanceWatcherBridge.cs: 1272 行; 花括号 161/161, 圆括号 719/719, 方括号 65/65 配平.
- 已移除符号检查: 全文无 WatcherAssemblyCount / _pumpInitialAttempt / _nativeNotification 残留.
- 同名 AssemblyIdentity x2 分别位于 FormStanceWatcherBridge (第 626 行) 与 Spire1StanceNotification (第 1261 行), 属两个类各自私有方法, 非冲突.
- 配套未改文件哈希: FormStanceBridgePump.cs B7075F4A5966A70E75ACB2D8F9DB002E9146D43E1020520A64BB4DE622ADB78A (688 bytes), MainFile.cs 112A5E94E35254140CDF5BF780A72102FF9743C621CE57B4835BC5A1B4FB3E75 (7133 bytes), 与 worker 报告声明的"本批未改"一致.

### 新增观察 O-01 [P3, 不构成返工阻断] NotifyEndTurnDivinity 单次 native 调用未在 await 后复核代数
- 绝对路径: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs 第 1130-1144 行.
- 触发条件: 第 1143 行 await lease.Binding.OnStanceChanged 期间发生 Shutdown/Terminal/pending.
- 当前控制流: 第 1138 行 TryAcquireLease + IsLeaseCurrent 门禁在调用前成立, 调用期间旧 binding 可能失效, 但这是单次 native 通知, await 后无后续副作用 (无 Remove/Apply/静态写).
- 与 S-06 的区别: S-06 在 await 后仍有 PowerCmd.Remove 副作用循环, 已逐轮核对; 本项无 await 后副作用, 风险等级低于 S-06.
- 最小修复范围 (如后续收紧): await 后追加 IsLeaseCurrent 仅用于日志/诊断, 不改 native 语义.
- 尚缺证据: 退出真实路径未跑; 不要求本轮返工.

## 最终判定 (r5, live/冻结 140A85DD...)

- 判定: SUPERVISION_PASS (静态监督通过; 不构成编译或实机验收).
- S-03/S-04/S-05/S-06/S-07 与请求第 4 项均已按 live 源码找到对应实现证据, 行号见上; 未发现新增 P0/P1/P2 源码缺陷.
- 唯一 P1 发现 S-HASH-01 属 worker 报告摘要锚点错误, 已由主会话 coordination.md 末段与 r5-completion-gate.json 勘误, 冻结快照与 live 字节一致, 不作为源码返工项; 报告第 68/86 行摘要仍未修正, 留作诚实性记录.
- O-01 为 P3 观察, 不阻断.
- 本监督未构建/未 lint/未测试/未部署/未运行游戏/未改产品代码/未 git; 编译与实机结论以主会话集中门禁为准.

## 未知

- 编译是否通过 (本角色禁止构建), 以主会话 Release 日志为准.
- 真实 Watcher 晚加载 / 同 simple name 多程序集 / 程序集替换时序.
- ProcessExit 实际线程模型与 Godot QueueFree 跨线程安全保证.
- 旧档解析, 联机/多人, UI/PCK 视觉, 长战斗, 战中存档.
- 上述均无实机证据; 不得把本静态结论当作实机通过.
