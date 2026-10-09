# DemonFormPower 2026-10-02 worker report

## 已确认

- 检查时间: 2026-10-02 (Asia/Shanghai).
- 工作边界: 产品代码目标仅为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs`; 按用户要求不构建、不运行游戏、不写其它产品文件。报告本身是用户明确指定的增量交付文件。
- 初始 Git 状态证据: `git status --short -- mod/Spire1Code/Forms/DemonFormPower.cs docs/reports/form-playable-20260928/demon-form-ledger-worker-20261002.md` 仅报告 `M mod/Spire1Code/Forms/DemonFormPower.cs`; 目标源码在本次修改前已有未提交差异，后续判断必须以当前工作树快照和 `HEAD` 对照，不能把既有差异归因于本次。
- 当前工作树证据: `DemonFormPower.cs:31-42` 的 `Data` 已保存 `StrengthPower? strength`、移除处理器和 `strengthRemovedAtZero`；`TrackStrength` 在 `:174-193` 为当前实例订阅 `Removed`，回调只记录 `strength.Amount == 0`；`ForgetPurgedStrength` 在 `:206-253` 会在 owner 当前 `StrengthPower` 与被跟踪实例不同时清理账本。
- 当前风险证据: `ForgetPurgedStrength` 的 `zeroAggregate` 仅由 `strengthRemovedAtZero && data.grantedStrength != 0m` 决定 (`:214`)，随后从 owner 读取当前 `StrengthPower` (`:239`) 并在 `:247-251` 对其减去旧 `granted`。因此“旧实例已归零移除 + owner 已经出现不同的新 StrengthPower 实例”时，静态路径仍可能把旧世代账本扣到新实例；这正违反本任务要求的实例身份/世代不变量。
- 当前正常清理证据: `AfterRemoved` (`:262-270`) 只在非 refreshing 时进入 `RemoveGrantedStrength`; `RemoveGrantedStrength` (`:272-309`) 通过 `cleanupComplete` 做一次性门闩，并在完成前调用 `ForgetPurgedStrength`，因此修复必须保持重复回调、refreshing finally 和显式形态移除的幂等性。

## 进行中

- 正在只分析 `DemonFormPower.cs` 及其已存在的工作树差异，确定最小实例身份/世代证据改法；不扩写其它产品文件。
- 需要保持的不变量: 同一 `StrengthPower` 实例被外部修改到零并触发 `Removed` 时，下一次 Demon grant 或形态退出仍可恢复/扣减正确的形态账本；旧实例归零后若当前 owner 已换成不同 `StrengthPower` 实例，绝不能把旧 `grantedStrength` 施加到新实例；显式 purge 仍不得凭空重建被清除的力量；所有清理入口重复执行结果相同。

## 未知

- 当前源码快照尚未证明 `StrengthPower.Removed` 回调执行时 owner 的替换实例是否已经挂载，也尚未证明显式 purge 与归零移除在回调可观察字段上的完整区分能力；后续只依据本地源码/API证据保守实现，不能把未验证的运行时时序当成事实。

## 已确认（增量检查面 2）

- 本地引擎源码证据: `research/engine-dllsrc/MegaCrit.Sts2.Core.Models/PowerModel.cs:542-553` 先写入 `_amount` 再触发 `DisplayAmountChanged`；`:575-580` 的 `RemoveInternal` 先触发 `Removed`，再调用 `Owner.RemovePowerInternal`。因此移除回调同步执行时，原 `StrengthPower` 仍可作为该 owner 当前列表中的同一实例身份证据。
- 本地引擎源码证据: `research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/PowerCmd.cs:239-253` 在 `SetAmount`、后置 hook 后，若 `ShouldRemoveDueToAmount()` 才调用 `PowerCmd.Remove`; `:291-298` 随后才 await 形态 power 的 `AfterRemoved`。这证明“回调捕获旧实例身份”与“清理阶段检查 replacement 当前实例”是两个不同时间点，不能只保存最终 `Amount == 0`。
- 既有探针契约证据: `tools/form-effects-probe/DemonScenarios.cs:163-177` 明确要求无 replacement 时，归零的 Demon aggregate 恢复未归属的负 Strength；`:179-189` 要求显式移除非零旧 aggregate 后，新 replacement Strength 不被二次撤回；`:223-236` 要求形态在授予 hook 内被移除时，in-flight ledger 仍精确清理。修复不能删除这些语义。
- 风险闭环: 旧实例归零回调应保存该旧 `StrengthPower` 的引用和该次追踪代际；清理阶段若当前 owner 的 `StrengthPower` 是不同引用，应清空旧 ledger 并直接返回，不能对 replacement 做 `current.Amount - granted`。若当前为空，则只有在旧实例确实收到归零 `Removed` 证据且未观察到 replacement 时，才保留既有的负 Strength 恢复路径；非零显式 purge 仍清账不重建。

## 进行中（增量）

- 已选定最小修改方向: 在 `Data` 中增加显式 removed-instance / generation 证据；回调保存捕获实例，不再只留下布尔值；`ForgetPurgedStrength` 与 `RemoveGrantedStrength` 统一使用该证据并保持一次性清理。
- 不采用任何未经引擎证实的 removal-cause API。若非零实例被显式 purge 可由最终 amount 区分；若某路径在 `Removed` 前先把 amount 写为零，仍只能保守按未知处理，不凭空恢复或触碰 replacement。

## 已确认（增量检查面 3：修复落盘）

- `DemonFormPower.cs:31-43` 新增 `removedStrength` 实例引用；`TrackStrength` (`:175-199`) 为每次当前追踪绑定 exact `StrengthPower`，`Removed` 回调在引擎移除前保存触发回调的实例身份，并继续保存最终是否为零。
- `ForgetPurgedStrength` (`:212-261`) 现在先读取 `tracked` 与 owner 当前 `current`：当前仍是 tracked 时不提前清理；当前存在但不是 tracked 时分类为 replacement，清零旧 `grantedStrength` 并直接返回；只有旧 tracked 实例已确认以零值触发 `Removed`、且当前没有 replacement 时，才保留既有外部负 Strength 恢复路径。旧账本不再对 replacement `current` 做减法。
- `RemoveGrantedStrength` (`:287-319`) 在清理前保存 exact tracked 引用；清账后仅当 owner 当前 `StrengthPower` 仍与该 tracked 引用相同才做 `SetAmount(strength.Amount - granted)`。当前为空或已换代时直接返回，不会凭旧账本创建或修改新世代。
- 幂等性保持: `cleanupComplete` 仍在第一次清理中置位；`UntrackStrength`、`removedStrength` 和零值标记在各清理出口一起清除；refreshing finally、重复 `AfterRemoved` 和显式清理不会重复消费同一账本。
- 目标文件行尾检查: LF，无 BOM；未运行构建、lint、测试或游戏。`git diff --check -- mod/Spire1Code/Forms/DemonFormPower.cs` 为通过（无 whitespace 错误）。

## 进行中

- 代码修改已完成；正在做最终静态闭环和写集核对，不执行用户禁止的构建或运行。

## 未知

- `Removed` 事件只提供最终 `Amount`，本文件没有权威 removal-cause/source token；因此“显式 purge 先把 amount 写成 0 再移除”与“外部 amount 修改归零后自动移除”仍无法完全区分。当前保守边界是：非零 purge 清账；零值回调只有在 exact tracked identity 且检查时没有 replacement 时保留既有恢复语义；若该路径的真实时序不同，需后续运行时证据确认。
- 未构建、未运行探针、未运行游戏，因此本轮只声明源码控制流和差异证据，不声明编译或实机通过。

## 已确认（最终静态闭环）

- 写集核对: 本次产品代码写入仅落在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs`; 用户指定的唯一报告写入落在本文件。目标产品文件在本次开始前已有未提交修改，报告中的结论按当前工作树相对快照记录，未覆盖或重置既有改动。
- 实现使用的是 `StrengthPower` exact object identity（`ReferenceEquals`），不是凭空生成的数值或未经证实的 numeric generation；这已经满足“实例身份或世代证据”中的实例身份分支。未对数值、回合目标或力量规则作改动。
- 最终静态不变量: replacement 当前实例永远不会进入旧 ledger 的 `SetAmount(current.Amount - granted)`；形态退出只对仍为 exact tracked 实例的当前 Strength 做撤回；无当前实例时只消费旧 ledger 并保持既有 zero-removal 外部余量恢复分支；重复清理在 `cleanupComplete` 后无副作用。
- 最终检查未发现目标文件 whitespace 错误；未构建、未运行游戏、未运行探针，未写其它产品文件。

## 进行中

- 无。

## 未知

- 显式 zero purge 与外部归零自动移除仍没有独立 removal-cause/source token；该边界按上文保守标注，不能由本次静态检查宣称完全闭合。
- 编译、探针行为、真实战斗时序、多人同步和存档/重连均未验证，因为本次请求明确禁止构建和运行。
