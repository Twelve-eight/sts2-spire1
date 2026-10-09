# Lifecycle safety cardinality r39 监督审查报告

审查对象: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs`
审查时 SHA256: `A5DE9B1C28AA2487016E3F2DF4D68FE6DCD00106AB5AF3EDA29A8E86A79DDFA7`
审查方式: 只读代码/证据核对 + 用 r38 真实报告数据做判定逻辑复算; 未构建, 未 lint, 未运行游戏, 未部署, 未修改任何代码或配置。

## 已确认

### 1. 门禁与交付面一致性

- `gate-notice.json` 声明 `Status=completed`, `WorkerResult=CODE_COMPLETE`, `NotBuilt/NotTested/NotRun=true`, `SHA256=A5DE9B1C...DFA7`。
- 当前源文件实测 SHA256 为 `A5DE9B1C28AA2487016E3F2DF4D68FE6DCD00106AB5AF3EDA29A8E86A79DDFA7`, 与门禁声明逐字符一致。
- `worker.md` 记录的修改后 SHA256 同为 `A5DE9B1C...DFA7`, 记录修改前为 `0224CFEB...65DE9`。
- `worker.md` 三段结构齐全: `## 已确认` / `## 进行中` / `## 未知`; `## 未知` 明确写出未构建、未 lint、未运行、未部署, 未把未验证项写成已通过。

### 2. 写集边界

- 14:59-15:20 时间窗内, `sts2-forms` 仓库中唯一被写入的文件是 `tests/FormsNativeSmoke/LifecycleSmokeRunner.cs`(mtime 2026-10-05 15:01:51)。
- 该窗口内 `mod/FormsCode/*`、中央 PowerShell 控制器、`tests/FormsNativeSmoke` 其它测试文件均无写入。工作区中其它 modified 文件属 r35-r38 既有未提交改动, 不落在本轮窗口。
- 本轮未写共享 `mod_configs`, 未写 C:。

### 3. 常量定义(第 47-51 行)

- `RemoveTargetIdentity` = `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power)`(第 47-48 行)。
- `PlayTargetIdentity` = `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction()`(第 49-50 行)。
- `SafetyPrefixType` = `Forms.FormsCode.FormStanceSafetyGuard`(第 51 行)。
- 三个常量与请求文件要求的字面量逐字符一致。

### 4. `SafetyGuardProof` 精确 cardinality(第 421-439 行)

- `prefixCount == 2` 为硬门禁(第 437 行), 不再是旧的一个 prefix。
- targets: `SameSet(targets, { RemoveTargetIdentity, PlayTargetIdentity })`(第 432 行), `SameSet` 为排序后 `SequenceEqual`, 同时约束元素个数与元素取值, 即"恰好这两个"。
- 额外保留 Remove 精确身份: `removeCanonical == RemoveTargetIdentity`(第 433 行)。
- patchTypes: `patchTypes.Length == 2` 且每一项都 `Ordinal` 等于 `Forms.FormsCode.FormStanceSafetyGuard`(第 434-435 行)。
- declaringMethods: `SameSet(declaring, { "RemovePrefix", "PlayPrefix" })`(第 436 行)。
- `removeMethodFound`、`removePatchedBySafetyOwner` 仍必须为 true(第 423-424、437 行)。

### 5. `SafetyGuardSameIdentity` 顺序无关(第 441-466 行)

- 计数门禁为 `beforeCount == afterCount && beforeCount == 2`(第 457-458 行)。
- targets / prefixDeclaringMethods / prefixPatchTypes 三组均改用 `SameSet` 集合比较(第 459-461 行), 允许 Harmony metadata 顺序变化, 不允许集合变化。
- Remove 的 `removeMethodIdentity` 与 `removeMethodCanonicalIdentity` 仍要求前后 `Ordinal` 精确相等且非空(第 462-465 行)。
- `SameSet`(第 468-470 行)先按 `StringComparer.Ordinal` 排序再 `SequenceEqual`, 语义为多重集相等, 重复元素会因长度/元素不匹配被拒。

### 6. 其它生命周期门禁未被弱化

- `noStacking` 仍为 `SameCounts(before, after)`(第 131 行), 判定逻辑未改; 且 `CountOwnerPatches` 新增 `SafetyOwner` 计数(第 281 行)使该门禁额外覆盖安全 owner, 属增强。
- `noResidual` 仍只检查 `BridgeOwner == 0 && FormsOwner == 0`(第 189 行), 未把 SafetyOwner 混入残留判定。
- duplicate assembly fail-closed 判定未改, `bridgeFailedClosedAfterDuplicate` 仍参与最终 `ok`(第 200-201 行)。
- 最终 `ok` 由 `noStacking && noResidual && safetyBeforeOk && safetyStable && safetyResidentAfterShutdown && closed is true` 组成(第 199-201 行), 三个 safety 结论都参与且都是硬门禁, 没有新增短路或旁路。
- JSON 字段形状保留: `targets`、`prefixPatchTypes`、`prefixDeclaringMethods`、`removeMethodIdentity`、`removeMethodCanonicalIdentity` 等字段仍完整写入(第 326-332、371-374 行)。

### 7. 用 r38 真实报告数据复算判定

以 `G:\omp works\.tmp\forms-independent-20261005\native-r35-lifecycle-r38\l1-independent\forms-lifecycle-smoke.json` 的三段 safety 证据为输入, 按新逻辑离线复算:

- `safetyGuardBefore` -> Proof=True。
- `safetyGuardAfterReinit` -> Proof=True, SameIdentity=True。
- `safetyGuardAfterShutdown` -> Proof=True, SameIdentity=True, 即新逻辑会把 `safetyGuardSurvivesShutdown` 判为 True, 而 r38 报告实际为 False, 与 worker 的"旧 cardinality 导致测试载体误报"诊断一致。
- 反向对照(全部按新逻辑判为 False): `prefixCount=1`、targets 缺 Play、declaringMethods 缺 Play、混入第三方 patch type、`removePatchedBySafetyOwner=false`、targets 重复 Remove。

### 8. 与生产源码契约一致

- `mod/FormsCode/FormStanceSafetyGuard.cs` 第 63-98 行确认对 `PowerCmd.Remove(PowerModel)` 与 `PlayCardAction.ExecuteAction()` 各装一个 prefix。
- 同文件第 123-160 行 `ProofHoldsLocked` 要求两个 target 各自恰好一个本 owner 的 prefix, 与本测试载体新的 2-prefix 契约一致。

## 进行中

- 无。本轮监督审查已完成, 未留下未决检查面。

## 未知

- 未构建、未 lint、未运行测试、未运行游戏、未部署: 修复后的编译通过与真实生命周期烟测 `safetyGuardSurvivesShutdown=true` 均未在本轮验证, 须由主会话集中构建与运行确认。
- 修改前版本 `0224CFEB...65DE9` 在本机未找到可读副本(不在 git 历史、备份或 .tmp 中), 因此"最小修改"是按 `worker.md` 的 delta 说明与当前代码阅读判定的, 未做修改前/后的逐字节 diff。
- 门禁存在两次写入: 首次 gate(15:01:13)声明 `310CE13D...269E0`, 源文件在 15:01:51 继续被写, worker.md(15:02:06)与 gate-notice.json(15:02:07)随后以最终 `A5DE9B1C...DFA7` 重写。最终门禁、worker.md 与磁盘文件三者一致, 故不影响本次结论; 但"门禁先于 worker 收尾落盘"属流程瑕疵, 记录备查。
- `SafetyGuardProof` 依赖 `MethodIdentity` 输出的参数名 `power`, 若目标程序集参数名被剥离会退化为不匹配; 该精确性由请求文件明确要求, 本次不视为缺陷, 但属对程序集元数据的隐含依赖, 未在运行时验证。

SUPERVISION_PASS