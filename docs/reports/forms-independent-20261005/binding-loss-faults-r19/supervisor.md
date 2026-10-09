# binding-loss-faults-r19 监督报告 (两阶段: FIXTURE + typed fault / Lifecycle)

范围: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke`
监督请求: `supervisor.request.md`
worker: `01a109cb-2810-7463-b8c0-eccd6b48ff4f`
模型/路由: `global:deepseek-v4.1-flash` / `wb2api` (用户本轮唯一指定), reasoning xhigh
边界: 仅静态审查; 不构建 / 不测试 / 不运行游戏 / 不 git; 不再委派, 不轮询 peer, 不启动其它 harness.

## 门禁证据

### 第一阶段 (FIXTURE_ONLY)
- `fixture-gate-notice.json`: hub 原生 `multi_agent_v1.wait_agent`, worker `01a109cb-2810-7463-b8c0-eccd6b48ff4f`, `Status=completed`, `TimedOut=false`, `ObservedAt=2026-10-05T10:08:02.6121414+08:00`, sha256 `C266FD6B503EFA5B4F8D7A085DB23F47BA4C290D052BB0C4FE000530B8999696`.

### 第二阶段 (typed fault + Lifecycle)
- `phase2-gate-notice.json`: `Status=NATIVE_WORKER_COMPLETED`, `NativeTool=multi_agent_v1.wait_agent`, `Target=01a109cb-2810-7463-b8c0-eccd6b48ff4f`, `ReturnedStatus=completed`, `TimedOut=false`, `RecordedAt=2026-10-05T10:35:07.6794895+08:00`.
- 4 文件 hash 与磁盘逐字一致 (见下 S6).

## 已确认

### S1-S5 第一阶段 (FIXTURE_ONLY): PASS
- S1 hash/写集: `BindingLossSmokeRunner.cs` 51275 `DF1C08...6063`, `README.md` 6304 `FCE44D...3083`; 当时仅 2 文件 M, 另 2 文件未改.
- S2 合法 target: `BindingLossEffectiveTarget` 对 AnyEnemy 传敌方 target, 其余 null; 与 `WatcherCrescendo/Tranquility`(None)、`WatcherStrike_P`(AnyEnemy)、`CardModel.IsValidTarget`(1762-1785) 一致.
- S3 入场诊断: 仅入场 `recordPreflightDiagnostics:true` 调 CanPlay; 桥失效后只写 `canPlayDeferred`.
- S4 手牌离开判据: `cardLeftHand` 区分 Cancel 假完成与真实出牌.
- S5 README 与实际行为一致.
- 第一阶段结论: FIXTURE_SUPERVISION_PASS (仅覆盖 FIXTURE_ONLY).

### S6 第二阶段 hash 门禁核对 (通过)
- `FormNativeSmokeRunner.cs` 161533 `0543ABC68E4A3CBD7A85276C21AA5969D692B9CC52F0AA8F721EF4098E1C3DB3`
- `BindingLossSmokeRunner.cs` 58477 `E496F79DCFE15579ED705ECB2C3395F13F544C8579FF4933850B59D3E99BCD0F`
- `LifecycleSmokeRunner.cs` 23901 `3457BCDED4BED87C2815822D002F57E9BFE74655550726218CA0FF33269B8CCA`
- `README.md` 7492 `D6F404EC0868E9EE7AB4CC2B84BE122DC2598F16804FAA5A07143DE039CA9DC1`
- 均与 `phase2-gate-notice.json` 一致.

### S7 编译面 (权威失败, 决定性阻断)
- 权威日志 `G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r19-final.log`: 生成失败, **2 errors / 6 warnings**.
- `BindingLossSmokeRunner.cs(789,60): error CS0103: 当前上下文中不存在名称"GameActionState"`.
  - 根因 (已核实): `GameActionState` 位于 `MegaCrit.Sts2.Core.Entities.Actions` (engine `...\MegaCrit.Sts2.Core.Entities.Actions\GameActionState.cs`); `FormNativeSmokeRunner.cs:12` 有该 using, 但 `BindingLossSmokeRunner.cs` 的 using 列表 (1-22 行) 缺 `using MegaCrit.Sts2.Core.Entities.Actions;`. 故 partial 文件的另一个部分能编译, 本文件不能.
- `BindingLossSmokeRunner.cs(849,17): error CS1739: "BindingLossCardRun"的最佳重载没有名为"actionExecutedAndSettled"的参数`.
  - 根因 (已核实): record 第 12 位形参为 `ActionExecutedAndSettled` (大写 A, 行 77), catch 路径命名实参写作 `actionExecutedAndSettled` (小写 a, 行 849); C# 命名实参区分大小写.
- 结论: 测试载体当前 **不可编译**, 一切运行期断言 (typed fault 关联 / Lifecycle / 实机) 均无法成立.

### S8 typed fault 对象级关联 (逻辑审查: 结构合理, 但被 S7 阻断)
- `UnobservedFaultEvidence` (FormNativeSmokeRunner.cs:1731-1744) 保留原始 `Exception` 对象 + Text + Sequence + ObservedUtc; 订阅点 165-169 保留对象.
- `FaultMatchesExceptionIdentity` (1296-1309) 只按 `ReferenceEquals` + `AggregateException.InnerExceptions` + `InnerException` 递归, 无字符串比较.
- `ApplyUnobservedFaultGate` (1250-1289): `faults = Skip(faultsAtStart)`; expected 仅由调用方传入的 `expectedRejections` 经身份匹配得出; `unexpected.Count>0` → `status=failed` + `terminalFailure=true` + `passed=false`. 无 expectedRejections 的调用 (scenario 987, turns-divinity 1586/1617, turns-wrath 1714, turns 2473, turns-calm 2788, effect 2855/3002) 一律全量 strict.
- BindingLoss expected 关联 (BindingLossSmokeRunner.cs:469-501): 仅当 `ActionExecutedAndSettled && strikeFaultAssociated && strikeExplicitFormsRestart` 才 `expectedRejections.Add(strikeActionRoot)`; `strikeFaultAssociated` 是身份匹配; `BindingLossFaultMentionsFormsRestart` 只作文本附加条件, 不是身份判据.
- 时序核对: `TaskHelper.LogTaskExceptions` 在 catch 内先 `UnobservedFault?.Invoke(ex)` 再 `throw`, 故 event 先于 `_executionTask` fault; `GameAction.Execute` 在 `_executionTask.IsCompleted` 后才 `TrySetResult` CompletionTask. 因此 `await action.CompletionTask` 返回时 event 已入队且 `action.Exception` 已可用; 代码在 event 到达时不删除, 延后到 scenario 级关联 — 与 TaskHelper 契约一致.
- 判定: "final raw 故障不会被误判为预期拒绝" 这一不变量在源码层面成立 (身份关联为唯一放行依据, 文本仅附加). 但 **因 S7 编译失败无法运行验证**.

### S9 副作用/安全门 (逻辑审查: 结构合理)
- 双向能量 `EnergyChanged` (FormNativeSmokeRunner.cs:2028-2035, 未知=mutated); HP/Block `PlayerHpOrBlockChanged` (2047-2059); owner power 集合 `OwnerPowersChanged` (2062-2079).
- `regression` (BindingLossSmokeRunner.cs:462-466) 含 strikeExecuted / damage / 双向能量 / HP-Block / owner powers / probe 侧 / stanceProbe.Passed; `safetyPassed = explicitRejectionBoth && noNativeMutation && !regression && stanceEvidenceDeterminate` (493).
- `cardLeftHand` (775-776) 现要求非 None 且非 Hand; 另 `strikeExecuted` 仍要求 card-play history FinishedCount 增长.
- 原始 marker 证据 (ReadBindingLossRawStanceFromPlayer 1030-1062) 仅取 owner Power 实例 FullName, 不依赖失效桥; 不确定即 `safetyPassed=false`.

### S10 Lifecycle owner proof (逻辑审查: 结构合理)
- `SafetyOwner = "Forms.FormStanceSafety"` (LifecycleSmokeRunner.cs:46) 纳入 `CountOwnerPatches` (269-297, 计 entry 而非 method).
- `SafetyGuardEvidence` (313-372) 用 `Harmony.GetAllPatchedMethods` + `GetPatchInfo` + 反射 `Patch.owner`/`PatchMethod` 读真实元数据; `FindPowerCmdRemoveMethod` (391-410) 精确定位 `PowerCmd.Remove(PowerModel)`; `SafetyGuardProof` (412-418) 要求 removeFound && removePatchedBySafetyOwner && prefixCount==1; `SafetyGuardSameIdentity` (420-432) 比对 count/targets/declaring.
- 流程 (154-195): 初始 proof → 重复 `MainFile.Initialize()` 后身份不变 → `MainFile.Shutdown()`x2 + `FormStanceWatcherBridge.Shutdown()`x2 后两原 owner=0 且 SafetyOwner 仍 1. 不直接调用 prefix 冒充安装.
- 生产侧 `FormStanceSafetyGuard.cs` 使用独立 HarmonyId `Forms.FormStanceSafety`, `EnsureInstalled` 幂等且自证 `ProofHoldsLocked`; `MainFile.Initialize:62` 显式安装, `Shutdown` 不移除 (181-196 注释+实现).

## 进行中

- 无.

## 未知

- 编译修复后能否真正构建通过: 未验证 (本会话不构建).
- 实机运行期断言 (r5 terminal/shutdown 是否真进入失效阶段并抛出 Forms-restart): 未验证; worker 亦报 JSON 未落盘.
- `final` JSON (FormNativeSmokeRunner.cs:185-193 构造) 只含 status/exitCode/quitStatus/quitDrain*, 不含 `unobservedFaults`/`expectedRejections` 数组; 原始 fault 证据落在 per-scenario JSON (BindingLoss 605-606/638-644). 是否满足"scenario/final JSON"双落盘要求需用户确认口径.
- 生产 r18 中央编译 1 warning/0 errors、结构门禁通过, 但未实机 — 不构成本测试载体的通过证据.

## 第二阶段结论

SUPERVISION_NEEDS_REWORK

- 阻断项 (P0): 测试载体编译失败 2 errors — `BindingLossSmokeRunner.cs:789` 缺 `using MegaCrit.Sts2.Core.Entities.Actions;` (CS0103); `BindingLossSmokeRunner.cs:849` 命名实参大小写不匹配 `actionExecutedAndSettled` vs 形参 `ActionExecutedAndSettled` (CS1739). 最小修复范围即这两处 (加 using / 改大小写), 不改其它逻辑.
- 非阻断观察 (待修后复验): final JSON 未携带 fault 数组 (口径待确认); 6 个 nullable 警告 (其中 BindingLossSmokeRunner.cs:155 为 CS8602).
- 已通过面 (源码级, 待编译+实机复验): 合法 target/诊断/手牌离开判据 (第一阶段 PASS); typed fault 身份关联不误判; 副作用 fail-closed 判据; Lifecycle Harmony 元数据 owner proof.
- 未声称实机通过; 第二阶段不得以任何形式引用为整体通过.

---

# 第三阶段 (phase3 窄返工) 监督: 等待门禁

## 已确认 (仅记录, 未审)

- phase3 请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\phase3.request.md` (已读取).
- 写集: 仅 `BindingLossSmokeRunner.cs` 与 `FormNativeSmokeRunner.cs`; 唯一 worker 报告 `worker-phase3.md`.
- 本轮要求: (1) 修中央编译两处错误; (2) 新增 `actionRejectionEvidence` 恰两项 (Strike/Tranquility) 含真实 reference-identity 匹配 sequence; (3) final 落盘 fault 分类字段与同源单调 sequence, 保持订阅至 post-quit 排空.
- 监督边界: 只等 hub 真实 `multi_agent_v1.wait_agent` 对 worker `01a109cb-2810-7463-b8c0-eccd6b48ff4f` 返回 completed 后才审 2 文件窄返工与 final/双 action 持久化 proof.
- 本阶段不再委派; 不 peer 轮询; 不启动其它 harness; 模型/路由仍 `global:deepseek-v4.1-flash` / `wb2api` / xhigh.

## 进行中

- 等待 hub 真实 wait completed 通知. 门禁前不审任何新改动.

## 未知

- phase3 改动内容与 hash; 编译是否通过; final/双 action proof 是否实现.

---

# phase3 窄返工监督, 已进入门禁后审查

## 已确认

### P3-S0 门禁和 hash 核对
- 检查时间: 2026-10-05T11:11:52+08:00.
- hub 门禁文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\phase3-gate-notice.json`.
- 门禁字段: `NativeTool=multi_agent_v1.wait_agent`, `Target=01a109cb-2810-7463-b8c0-eccd6b48ff4f`, `ReturnedStatus=completed`, `TimedOut=false`, `RecordedAt=2026-10-05T11:10:32.4198009+08:00`, `ArtifactState=CODE_COMPLETE`. 此证据来自 hub 通知及落盘文件, 本监督未调用 peer 工具.
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`: 61778 bytes, SHA256 `DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A`, 与门禁逐字一致.
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs`: 163311 bytes, SHA256 `4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D`, 与门禁逐字一致.
- `LifecycleSmokeRunner.cs` 和 `README.md` hash 仍与 phase2 门禁相同. 本阶段不审这两文件, 不扩大产品写集.
- 用户请求指定 `global:deepseek-v4.1-flash / wb2api / xhigh`; 当前工具未证实本会话实际模型或 provider route, 不将请求文字冒充实测路由. 本监督没有发起模型调用或回退.

## 进行中
- 仅审两文件窄返工, 双 action 身份 proof 和 final 分类及排空. 每完成一面追加本报告.

## 未知
- 新 hash 字节的中央构建和实机结果尚未由本监督验证. CODE_COMPLETE 不表示构建或游戏通过.
## 已确认

### P3-S1 两处确定编译错误的源码返工
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:12` 已加入 `using MegaCrit.Sts2.Core.Entities.Actions;`.
- 同文件 `:78` 的 record 参数是 `ActionExecutedAndSettled`, `:880` 的 catch 构造命名实参已改为 `ActionExecutedAndSettled: false`.
- `:818` 仍引用真实 `GameActionState.Finished`, 未新增或猜造枚举. phase2 的 CS0103 和 CS1739 触发点已在源码层面修正.
- 证据类型: hash 锁定源码静态核对. 本监督未重跑编译, 不将此结论写成构建通过.

## 进行中
- 双 action proof 的来源和硬门禁, final roots 传递和 late fault 分类仍在审查.

## 未知
- phase3 新字节是否还有其它编译错误, 需 hub 中央构建确认.
## 已确认

### P3-S2 双 action 身份 proof 已持久化且纳入硬门禁
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:139-144` 初始化 `actionRejectionEvidence` 恰两项, 仅 Strike 和 Tranquility, 早退保留 false 和空 sequence, 进入牌不计入.
- `:405` 和 `:423` 分别替换实际 action 的 proof; `:1037-1056` 输出 label, cardEntry, actionExecutedAndSettled, explicitFormsRestart, actionFaultRootType, identityMatchedFaultSequences, action.
- `:1043-1045` 的 sequence 仅取原始 `UnobservedFaultEvidence` 经现有 `FaultMatchesExceptionIdentity` 匹配后的 Sequence, 没有文本匹配或构造序号.
- `:766-771` 在同一真实 action 的 `BeforeExecuted` 上记录执行, 使用 ReferenceEquals, Interlocked.Exchange; `:817-825` 要求真实执行事件, Finished, CompletionTask 已完成和非 actionFailure. timeout/cancel 路径不能凭 drain 后状态取得有效 settled proof.
- `:490-510` 双 action 分别满足已执行和 settled, 真实身份关联及明确 Forms 重启拒绝, 才得到 `dualActionProofPassed` 和 `safetyPassed`. `:655-658` 仅将对应已批准 roots 传给本次局部 final 列表.
- 编译修复, 两项 proof 结构和硬条件这一面在源码层面满足 phase3 请求. 身份 helper 对混合异常的完整覆盖仍另行核对, 不据此声称所有 raw fault 都已安全分类.

## 进行中
- 审核 final pre/post-quit 分类, 订阅生命周期, late fault 边界和 proof 原始副作用证据.

## 未知
- 两 action 的真实执行, settled 和匹配序号尚无本阶段实机证据; 上述结论仅为 hash 锁定源码核对.
## 已确认

### P3-S3 final 持久化和订阅生命周期, 结构已补齐
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:165-171` 保留本次进程 raw Exception queue, 同源 Interlocked Sequence 和局部 approved roots 列表; `:176`, `:301-305`, `:430-438` 显式传到 binding-loss, 未用持久全局吞错列表.
- `:202` 和 `:262-263` 用同一队列及 `faultsAtStart=0` 分类 pre/post-quit, `:1294-1297` 和 `:1298-1307` 输出五项 fault 字段. 普通 smoke 未加入 roots, 仍全 strict.
- `:191` 初始化 failure=null; `:250-275` 累积 quit/drain/fault failure, completed 必须同时满足 gate, failure 为空, evidence write 成功, exitCode=0, quit executed, drain settled. 不用第二次成功 gate 清除早期 failure.
- `:224` 保留 quitExitCodeRequested, `:266-267` 分离 evidence exitCode 与已请求退出码. 后发现故障不能追改已请求的实际退出码, 此边界已明示.
- `:294-297` 在最外 finalization 的 finally 退订, 而非 quit 前退订. cleanup 和 quit/drain 中发布的 event 能进入该 queue.
- 这一面的字段和生命周期结构满足请求. 但下面 R1 证明分类并非对所有 late fault fail closed, 因此不能给 final 安全分类 PASS.

### R1 [P1] 混合 AggregateException 的部分命中会吞掉无关 late fault
- 文件: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1323-1335`, 调用点 `:1289-1297`, final post-quit 调用 `:262-263`.
- 权威契约: phase3.request.md 第 3 项要求 late fault 不匹配既有 real root 时必须失败, 普通或其它异常不能被 expected 标签掩盖; AggregateException 展开也必须按实际 Exception reference identity 归属.
- 触发条件: 一张真实已执行并 settled 的 action 已批准异常对象 E 和真实 root R=AggregateException(E); 后续 cleanup/quit 的一个 raw event L=AggregateException(E,U), 其中 U 是未批准的无关 Exception 对象. 两个 event/root 可共享 E, 但 L 的 U 绝非该 action 的批准异常.
- 当前控制流: `FaultMatchesExceptionIdentity(L,R)` 的 observedAggregate.InnerExceptions.Any 只需 E 命中便返回 true; E 命中 R 的 actionAggregate.InnerExceptions.Any. `ApplyUnobservedFaultGate` 把整条 L 移到 expected, 不再检查 U, unexpectedUnobservedFaults 可仍为空, finalFaultGatePassed 可为 true. wrapper 中只要夹带 E 就能吞掉 U.
- 证据类型: 源码分支逐步推导, 非已执行的隔离测试, 非实机复现. 按文本相同的独立 E2 仍不能命中, 问题是共享真实对象时未覆盖所有叶子.
- 只读复核命令: `$s=Get-Content -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs'; $s[1283..1334]`. 本监督未运行被审方法或构造测试事件.
- 最小修复范围: 限两文件既有 fault 分类/proof helper, 建立真实 action root 的批准对象闭包; observed AggregateException 只有全部 fault 叶子按 reference identity 属于批准 root 才能整条 expected, 或在不丢 raw event 的前提下把 unmatched 叶子另列 unexpected 并 fail. 不把 Any 改成文本判据, 不制造 sequence, 不降低两 action gate.
- 尚缺证据: hub 对 same-root wrapper, 同文异对象, 混合 E+U, 完全无关 late fault 的隔离验证和 terminal/shutdown 实机证据. 本监督不构建或测试.

## 进行中
- 核对新加 Tranquility actualCardPlay 证据是否引用正确牌的 history, 然后有界收敛.

## 未知
- cleanup/quit late fault 的真实发布时序和新字节编译结果未验证.
## 已确认

### R2 [P2] 新增 Tranquility actualCardPlay 证据仍读取 Strike history
- 文件: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:430-431`, `:476-480`; 来源文件: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1852`, `:1943-1954`, `:1968-1971`.
- 宣称: worker-phase3.md 的 P3-05 宣称 Tranquility 的真实 FinishedCount 增量已纳入 regression, 两张牌已有实际 CardPlay 不能被误判为无副作用.
- 触发条件: 真实 Tranquility 产生 CardPlaysFinished 记录, 而 probe 前后没有新的 WATCHER_STRIKE_P 记录.
- 当前控制流: `probeActualCardPlay` 调用 `ReadCardPlayHistory(stanceProbe.After)` 和同样的 before reader; reader 只取 watcherStrikeHistory, Snapshot 只为 WATCHER_STRIKE_P 构建该字段, DescribeCardPlayHistory 明确按 entry.CardPlay.Card.Id.Entry 过滤. 因此 Tranquility 的真实 FinishedCount 增长不改变这个 counter, probeActualCardPlay 仍为 false. faulted action 的 Passed 本来也是 false, `regression` 对 probe 的 actual-play 检测没有实现宣称.
- 证据类型: 源码数据流核对. 未声称此误记已在实机造成安全假阳性, 能量/HP/block/raw marker 变化仍会使其它 gate 失败.
- 只读复核命令: `$b=Get-Content -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs'; $b[427..479]; $f=Get-Content -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs'; $f[1942..1970]`.
- 最小修复范围: 限两文件 binding-loss 证据路径, 按真实 probe 卡的 Card.Id.Entry 记录其自己的 started/finished history 和资源支付证据, 不猜译名或注册 ID; 原 Strike history 和普通三姿态/turns 的门禁保持不变.
- 尚缺证据: hub 对真实 Tranquility CardPlayFinished 及 fault-after-play 负例的隔离验证和实机证据. 本监督未测试或运行游戏.

### P3-S4 安全门未因 phase3 标签放松的已核面
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:803-804` 已用 OrdinalIgnoreCase 排除 null pile 的 none/None, 不把 null 当 cardLeftHand.
- `:428-429` 的 Strike FinishedCount 增长已与 Passed 解耦, fault 后真实出牌仍会进入 regression.
- 双向 energy, HP/block, owner power 集合及失效桥之外的 raw marker 判据保持. R2 指的是新增 probe history 来源错误, 不是要求撤销现有门禁.
- 没有重新审 Lifecycle 或 README, 没有调用 peer 或 git, 没有构建/测试/部署/游戏动作.

## 进行中
- 审查已收敛, 最后核对 hash 未漂移并落最终结论.

## 未知
- 新字节中央编译结果, 双 action sequence/proof 的 terminal/shutdown 实机证据, cleanup/quit late fault 分类的真实时序都未验证.
## 已确认

### P3-S5 收尾 hash 复核
- 收尾检查时间: 2026-10-05T11:17:22+08:00. 两文件 bytes/SHA256 与 phase3-gate-notice.json 仍逐字一致, 审查期间未漂移.
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`: 61778 bytes, `DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A`.
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs`: 163311 bytes, `4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D`.
- 门禁文件 SHA256: `D6A16E3489C0771134232703A694D497D333609D1E9EA1B2CAB3558DBF7EA852`.
- worker-phase3.md SHA256: `E87801C3ABD2E2B15C05778807F09B660C04C395D6897395AD537C3FD93B1BDD`.
- 本阶段开始于 11:11:52, 收尾于 11:17, 在 10 分钟界内. 只追加唯一 supervisor.md, 未改产品源码或其它报告.

## 进行中
- 无. phase3 窄监督已停止, 不自主轮询或继续审未过新门禁字节.

## 未知
- 本阶段未编译, 未运行被审方法或隔离测试, 未启动游戏. 两处原编译错误已在源码修复, 但不等于中央构建通过.
- 双真实 action 的执行和 settled, 身份 sequence 的实际 JSON, cleanup/quit late fault 以及实际进程退出码仍需 hub 中央证据.
- 当前工具未提供实际会话模型/provider route 元数据. 请求指定模型和路由不等于实测 route.

## phase3 最终结论

SUPERVISION_NEEDS_REWORK

- 判定针对上述两文件的 phase3 门禁 hash, 不将第一阶段 FIXTURE_SUPERVISION_PASS 当整体通过.
- 已修面: 原 CS0103/CS1739 触发点, 恰两项 actionRejectionEvidence, BeforeExecuted 真实执行观测, 双 proof 硬条件, 局部 approved roots 传递, final 五项 fault 字段, post-quit 排空后退订, failure 保留和请求退出码与证据退出码分离.
- 待返工 R1 [P1]: FormNativeSmokeRunner.cs:1323-1335 的 Any 仅检查部分身份命中, 带批准 E 和无关 U 的混合 AggregateException 会被整条标 expected, 违反其它/late fault 必须失败的门禁.
- 待返工 R2 [P2]: BindingLossSmokeRunner.cs:430-431 的 Tranquility actualCardPlay 读取固定 Strike history, 未实现两卡各自真实 CardPlayFinished 的持久化副作用证据.
- 两项均为源码证据, 不冒充已执行复现. 最小修复限本批两文件相关 helper/证据路径; 完成后需要新 hub 真实 completed/hash 门禁和中央构建及实机验收.