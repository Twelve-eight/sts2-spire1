
## 监督等待状态
- 时间: 2026-10-05 09:37:59
- 精确 worker id: 01a109b5-993b-7ea1-88b9-ab5c3a9b8cdd
- 状态: 等待 hub 对上述 worker 返回 completed 并给出落盘 gate-notice
- 当前动作: 未读取或审查进行中的代码
- 门禁后动作: 只读审查 worker 报告与最终两文件, 独立核对 hash, 重点核对 R14-01 与 R14-02, 保留超时/fault/explicitRejection/无失效后 fixture 语义
- 禁止项: 不自行委派, 不替换 wait 工具, 不启动 codex/omp/模型调用, 不构建/lint/测试/git/游戏

## 已确认
- 已收到监督请求文件, 并确认唯一可写路径为本文件.
- 尚未收到 hub 的真实 wait completed 门禁通知.

## 进行中
- 等待精确 worker id 01a109b5-993b-7ea1-88b9-ab5c3a9b8cdd 完成.

## 未知
- worker 最终两文件内容与 hash.
- worker 报告中的已确认/进行中/未知结论.
- R14-01 与 R14-02 的最终实现证据.

## R16 监督审查 (真实门禁已成立)

- 审查时间: 2026-10-05 09:52:46
- 角色: 同批监督审查员, 只读静态面
- 精确 worker id: 01a109b5-993b-7ea1-88b9-ab5c3a9b8cdd
- 模型路由: global:deepseek-v4.1-flash / wb2api / xhigh (worker 报告声明)
- 边界: 未构建, 未 lint, 未测试, 未部署, 未运行游戏, 未 git, 未改任何代码

### 已确认 (有证据)

#### 门禁与 hash
- 门禁证据: gate-notice.json 记录 multi_agent_v1.wait_agent 对 Worker 01a109b5-993b-7ea1-88b9-ab5c3a9b8cdd 返回 Status=completed, TimedOut=false, ObservedAt=2026-10-05T09:46:23.4296922+08:00.
- 独立重算 SHA256:
  - BindingLossSmokeRunner.cs = BF461E204E0B088CDECDD122C31E353C3B98CB2884C9F1BD8C73DACE5A176A40, 46357 bytes, 984 行, UTF-8 无 BOM.
  - README.md = 4C5994A7CCC1F67173E0513D5FD25509FE813655F29AA2756F216B3024AA8E6A, 5570 bytes.
  - 两个 hash 与 gate-notice.json 的 Files 声明一致.
- 未改动文件 hash 独立重算:
  - FormNativeSmokeRunner.cs = D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC.
  - FormStanceWatcherBridge.cs = 140A85DD310D74C284E01E73B73E89D3C0CEF71F920193CAD9308E442126AAB0.
  - FormStanceMode.cs = 3C3445164E7BF3146118BB82FCF772EFAD35AB6992DF07E9C4F50649D0B5B918.
  - 与 worker.md 边界核对声明一致.

#### R14-01 通过: unknown 恒回归缺陷真实消失
- 原始 marker 证据只由 owner Power 实例 FullName 判定:
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:77-99 定义 BindingLossRawStanceEvidence 与 BindingLossNativeMarkerFullNames.
  - 同文件:833-843 BindingLossSnapshot 先调既有 Snapshot, 再追加 ownerPowerRawMarkerFullNames / ownerPowerRawMarkerAmounts / ownerPowerRawStance / ownerPowerRawStanceDeterminate / ownerPowerRawStanceConflict.
  - 同文件:845-877 ReadBindingLossRawStanceFromPlayer 只遍历 player.Creature.Powers, 取 power.GetType().FullName, 匹配三个 marker, 多 marker 判 conflict, 0 个判 None, 唯一判 Wrath/Calm/Divinity, 并累计 amount.
  - 同文件:879-894 ReadBindingLossRawStance 从同一快照回读原始证据.
- 枚举不依赖失效桥:
  - 同文件:845-877 无 FormStanceWatcherBridge, 无 MethodInfo, 无 Assembly, 无 delegate 引用; 注释 75-76 与实现一致.
- 键集合匹配当前权威源码:
  - G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Wrath.cs:10-12 namespace WatcherMod; public sealed class Wrath : PowerModel.
  - G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Calm.cs:9-11 同形 Calm.
  - G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Divinity.cs:14-16 同形 Divinity.
  - FullName 集合 = WatcherMod.Wrath / WatcherMod.Calm / WatcherMod.Divinity, 与 BindingLossSmokeRunner.cs:94-99 一致; 也与 FormStanceWatcherBridge.cs:700-702 的 RequireMarker 集合一致.
- ownerPowerAmounts 真实键格式已核对并规避:
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1697-1704 第 1703 行 powerAmounts[shortName] = power.Amount; 键是短类型名 (Wrath/Calm/Divinity), 不是 FullName.
  - R16 没有复用 ownerPowerAmounts 做姿态判定, 而是新建 FullName 键字典, 与 R14-01 最小修复方向一致.
- 恒回归控制流已改:
  - BindingLossSmokeRunner.cs:417-420 明确 bridge-derived stance 仅作诊断, unknown 不得与失效前 Wrath 比较.
  - 同文件:421-427 只以 rawStanceBeforeStrike / rawStanceAfterStrike 判 strikeStanceChanged; strikeStanceChangeDeterminate 要求 before/after 均 determinate; unknown 不参与比较.
  - 同文件:429-435 probe 同样只以 raw 证据判定.
  - 同文件:437 stanceEvidenceDeterminate = strikeStanceChangeDeterminate && probeStanceChangeDeterminate.
  - 同文件:444 regression 不再包含 unknown-vs-Wrath; 448 safetyPassed 增加 stanceEvidenceDeterminate; 未知或冲突时保持 false (fail-closed).
  - 同文件:576-584 finally 仍对任何新增 UnobservedFault 强制 status=failed 与 safetyPassed=false.
- 入愤怒 gate 也改用原始 marker:
  - 同文件:250-267 enteredRawStance 必须 determinate 且 Classification=Wrath; bridge nativeStance 仅写入 nativeStanceBeforeDiagnostic (260, 263, 271).
  - 因此 R14-01 中 unknown 被当成变化的问题在 entry, strike, probe 三处均已消失.
- 结论: R14-01 的 P1 缺陷 (桥失效后 stance 读值恒 unknown 导致 regressionObserved 恒真, safetyPassed 不可达) 在静态控制流上已真实消失, 且保留 fail-closed 语义.

#### R14-02 通过: README 与失效前 energy>=2 一致
- G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md:62 写桥失效前用真实 PlayerCmd.GainEnergy 补到至少 2.
- 同文件:65 写桥失效后不再补能, 依次打 WATCHER_STRIKE_P 与 WatcherMod.WatcherTranquility.
- 同文件:67-69 写姿态判定只取原始 owner Power FullName marker, 不可确定时单独记未知且 safetyPassed 保持 false.
- 同文件:74-76 补诊断字段说明.
- 同文件已无 "至少 3" / "至少3" / "补到 3" / "补到3" 残留.
- 代码侧一致:
  - BindingLossSmokeRunner.cs:61 BindingLossProbeEnergyMinimum = 2.
  - 同文件:287-324 在 bridgeRemoved 之前读能量, 计算 fixtureEnergyGain = max(0, 2 - before), 取真实 PlayerCmd.GainEnergy Task, await, 再读能量并断言 >= 2.
  - 全文只有 293 与 297 两处 GainEnergy 文本, 均在 326 行 bridgeRemoved 分支之前; 326 行之后无任何补能调用.
- 结论: README 与代码一致, 失效前补到至少 2 然后桥失效再 probe 两张牌的语义成立.

#### 保留语义静态核对通过
- 真实动作 / Crescendo fixture: BindingLossSmokeRunner.cs:52, 233-242, 395-404; 入口用真实 WatcherMod.WatcherCrescendo, 后续用真实 WATCHER_STRIKE_P 与 WatcherMod.WatcherTranquility.
- 失效前补能, 失效后无补能: 见 R14-02 证据.
- explicitRejection 双条件: 同文件:407-409 要求 nextRun 与 stanceProbe 都被 BindingLossHasExplicitRejection 判真; 897-927 要求 Forms 失效词与 restart/重启词同时出现, 且 timeout/pending/cancelled-without-fault/no-observed-fault 直接 false.
- 真实动作完成 / fault / 排空: 同文件:676-704 等待 action.CompletionTask, 记录 action evidence, passed 需 runtime.IsSuccessful 且 !newFault; 502-584 finally 对 detached operations 做有界排空并写 unobservedFaults.
- regressionObserved / safetyPassed 语义: 同文件:444-453; 无 mutation 且 raw 证据 determinate 且两探针都显式拒绝才 passed.
- 不给 timeout/cancelled/pending 伪通过: passed 路径需 runtime.IsSuccessful; explicitRejection 拒绝 timeout/pending/无 fault 的 cancelled; 584 之后任何新增 UnobservedFault 强制 failed.
- 以上均为源码证据, 不声称实机通过.

### 进行中 (需复核的半成品)
- 无. R14-01 与 R14-02 窄修在静态面收敛.

### 未知 (未覆盖)
- 编译与实机均未由本监督执行; hub 同步进行的 compile-only 不作为实机证据. 原始 marker 在真实战斗中的键集合与姿态转移未实机验证; 修复后 safetyPassed 是否可在 r5 生产字节下达到未验证.
- 后续安全复测风险 (只读, 本批不改, 不扩大返修):
  - 引擎证据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Helpers\TaskHelper.cs:21-42. 第 21-24 行 RunSafely(Task) 调 LogTaskExceptions; 第 26-42 行 LogTaskExceptions await task, 捕获非 OperationCanceledException 后在第 38 行 TaskHelper.UnobservedFault?.Invoke(ex), 然后 rethrow.
  - 引擎证据: G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:169519-169568 GameAction.Execute. 第 169528 行 _executionTask = TaskHelper.RunSafely(ExecuteAction()); 第 169540 行 await TaskHelper.WhenAny(_executionTask, _pauseForPlayerChoiceTaskSource.Task); 第 169550 行 _completionSource.TrySetResult(). 即 RunSafely 包裹先于外层等待, ExecuteAction 的任何 fault 都会在 RunSafely 内发布 UnobservedFault, 与调用方是否 await CompletionTask 无关; 同时 CompletionTask 仍可能以 TrySetResult 正常完成, 而异常只留在 action.Exception.
  - runner 证据: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:165-167 订阅 TaskHelper.UnobservedFault 并入队; BindingLossSmokeRunner.cs:693 newFault = unobservedFaults.Count > faultsAtStart; 695-698 passed 需 !newFault; 576-584 finally 若有新增 fault 则 status=failed 且 safetyPassed=false.
  - 风险: 若预期的 Forms 重启拒绝以 ExecuteAction 异常形式出现, 同一次异常会同时提供 explicitRejection 文本并把 newFault 置真, 于是 safetyPassed 被强制 false. 该交互目前只有源码控制流证据, 拒绝的实际抛出路径与实机表现未验证; 本批未修改该面.
- 另一未覆盖边界 (非本批要求): BindingLossSmokeRunner.cs:443 energyMutated 只判 energyAfter > energyBefore; 若拒绝路径在 SpendResources 之后抛异常导致能量下降, 该下降不被 energyMutated 捕获. 未实机验证, 本批不返修.

### 结论
- SUPERVISION_PASS
- 范围: R14-01 与 R14-02 已按请求修复, 保留语义静态核对通过.
- 边界: 未构建, 未实机; 上述 TaskHelper 风险与能量下降边界留待后续安全复测, 不作为本批返修项.
- 只把源码证据当源码证据, 不声称实机通过.
