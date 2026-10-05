# R16 Worker Report - 原始 marker 姿态证据返修

范围: 仅返修 R14-01 / R14-02. 唯一写集:
- G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs
- G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md
模型: global:deepseek-v4.1-flash / wb2api / xhigh. 未构建, 未 lint, 未测试, 未部署, 未运行游戏, 未 git, 未再委派.

## 已确认

### C1. ownerPowerAmounts 真实键格式 = 短类型名
- 证据: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1694-1715 `DescribeCreature`.
- 第 1700 行 `string fullName = power.GetType().FullName ?? power.GetType().Name;` 写入 `powerTypes`.
- 第 1701 行 `string shortName = power.GetType().Name;` 第 1703 行 `powerAmounts[shortName] = power.Amount;`
- 结论: `ownerPowerAmounts` 的键是短类型名 (Wrath / Calm / Divinity), 不是 FullName. 因此 R16 不复用该字典做姿态判定, 改为遍历 `player.Creature.Powers` 取 `power.GetType().FullName`.

### C2. 三个原生 marker 的 FullName
- 证据: G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Wrath.cs:10-12 `namespace WatcherMod; public sealed class Wrath : PowerModel`.
- 证据: G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Calm.cs:9-11 同形 `Calm`.
- 证据: G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Divinity.cs:14-16 同形 `Divinity`.
- 结论: FullName 分别为 `WatcherMod.Wrath` / `WatcherMod.Calm` / `WatcherMod.Divinity`.

### C3. R14-01 成立 (桥失效后 stance 读值恒 unknown)
- 证据: FormNativeSmokeRunner.cs:1718-1727 `Snapshot` 捕获 `FormStanceWatcherBridge.CurrentKind` 异常并静默写 `unknown`.
- 证据: FormStanceWatcherBridge.cs:835-841 `CurrentKind` 首行 `FormStanceMode.RequireAvailable()`, 桥失效即抛.
- 结论: 旧 `stanceAfter != nativeStance` 恒真, `safetyPassed` 不可达.

### C4. R16 窄修已应用 (BindingLossSmokeRunner.cs)
- 新增 `BindingLossRawStanceEvidence` (第 77-92 行) 与 `BindingLossNativeMarkerFullNames` (第 94-99 行): 仅由 owner Power 实例 FullName 判定, 不接触 Watcher 桥 / MethodInfo / Assembly / delegate.
- 新增 `BindingLossSnapshot` 包装 (第 833-843 行): 调用既有 `Snapshot` 后追加 `ownerPowerRawMarkerFullNames` / `ownerPowerRawMarkerAmounts` / `ownerPowerRawStance` / `ownerPowerRawStanceDeterminate` / `ownerPowerRawStanceConflict`.
- 新增 `ReadBindingLossRawStanceFromPlayer` (第 845-877 行): 遍历 `player.Creature.Powers`, 以 FullName 匹配三 marker, 多 marker 判 conflict, 0 个判 None, 唯一判 Wrath/Calm/Divinity; 同时累计 amount.
- 新增 `ReadBindingLossRawStance` (第 879-894 行): 从快照回读原始证据.
- 场景判定 (第 417-448 行): before/after 均用同一原始 marker 证据; `unknown` 不再与 Wrath 比较; `safetyPassed` 追加 `stanceEvidenceDeterminate` 条件, 原始证据不可确定或冲突时保持 false (fail-closed).
- 入愤怒 fixture (第 254-274 行): 姿态 gate 改为原始 marker 证据 (Wrath 且 determinate); 桥 `nativeWatcherStance` 降级为诊断字段 `nativeStanceBeforeDiagnostic`.
- 4 处 binding-loss 快照调用改为 `BindingLossSnapshot` (第 363, 392, 631, 700 行); FormNativeSmokeRunner.cs 未改动.
- 保留: 真实动作/Crescendo fixture, 失效前补到至少 2 (第 61, 287-317 行), 失效后无补能 (第 324 行之后无 GainEnergy), explicitRejection 双条件, 真实动作完成/fault/排空, regressionObserved 与 safetyPassed 语义.

### C5. R14-02 README 已同步
- 证据: README.md:62 改为 "桥失效前 ... 补到至少 2"; 第 65 行 "桥失效后不再补能"; 第 67-69 行写明原始 marker 判定与未知保持 false; 第 74-76 行补诊断字段说明.
- 证据: README.md 已无 "至少 3" 残留.

### C6. 静态自检
- BindingLossSmokeRunner.cs: UTF-8 无 BOM, 984 行, 46357 bytes, SHA256 BF461E204E0B088CDECDD122C31E353C3B98CB2884C9F1BD8C73DACE5A176A40 (最终).
- README.md: 5570 bytes, SHA256 4C5994A7CCC1F67173E0513D5FD25509FE813655F29AA2756F216B3024AA8E6A.
- 括号计数平衡: braces 76/76, parens 372/372, brackets 152/152.
- 旧符号 `nativeStanceAfter` / `stanceProbeAfter` / `stanceAfter` 已无残留.

## 进行中

- 无. 窄修与文档已收敛, 待 hub 中央 compile-only 与同批监督.

## 未知

- 编译: 本写集不构建; 仅静态自检, 未获编译证据.
- 实机: 未运行; 原始 marker 在真实战斗中的键集合/姿态转移仅由源码与静态证据支撑.
- 实机: 修复后 safetyPassed 是否可在 r5 生产字节下达到, 未验证.

## 边界核对 (最终)

- 未改动: FormNativeSmokeRunner.cs D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC (与 r14 相同).
- 未改动生产: FormStanceWatcherBridge.cs 140A85DD310D74C284E01E73B73E89D3C0CEF71F920193CAD9308E442126AAB0; FormStanceMode.cs 3C3445164E7BF3146118BB82FCF772EFAD35AB6992DF07E9C4F50649D0B5B918.
- 未改生产 r5 DLL, 未构建/部署/运行/委派.