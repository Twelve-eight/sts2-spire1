# DemonFormPower 监督审查报告

## 元数据

- 审查日期: 2026-10-02。
- 等待纪律: 已使用 Codex 原生 `wait_threads` 等待实现代理 `01a0fc4c-22f7-71a2-b7b2-f5b8cb741746`，收到 `turnCompleted` 后才开始读取目标源码。
- 审查范围: 只读审查 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs`；不修改产品代码，不构建，不运行游戏。
- 目标文件状态证据: `git diff` 仅显示该目标文件的修改；本报告是监督审查的唯一落盘报告。

## 已确认

### 证据面 1: StrengthPower 实例身份与代际保护

- 文件 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:175-199`：`TrackStrength` 保存精确的 `StrengthPower` 引用，并为该引用挂接 `Removed` 回调；回调把同一引用保存到 `removedStrength`，同时记录当时 `Amount == 0`。这不是只保存 power id 或当前查询结果，能够区分旧实例与后续 replacement 实例。
- 文件 `...\DemonFormPower.cs:220-241`：`ForgetPurgedStrength` 先比较 `owner.GetPower<StrengthPower>()` 与 `data.strength` 的引用；当前存在不同引用时归类为 replacement，先解除旧追踪并清零旧 `grantedStrength`，不对当前 replacement 做扣减。
- 文件 `...\DemonFormPower.cs:302-309`：形态退出再次要求当前 `StrengthPower` 与退出前保存的 `tracked` 引用 `ReferenceEquals`，否则只清理账本并返回。静态上满足“旧世代账本不得套到新世代”的核心不变量。

### 证据面 2: 首批静态结论

- 文件 `...\DemonFormPower.cs:271-321`：`AfterRemoved` 设置 `removed`，非刷新中才调用清理；`cleanupComplete` 在 `RemoveGrantedStrength` 开头形成一次性闩锁，避免重复实际撤回。刷新中移除则由 `RefreshStrength` 的 `finally` 在异步授予结束后接管清理（`:131-140`）。
- 文件 `...\DemonFormPower.cs:312-320`：同一 `StrengthPower` 仍存在时使用 `SetAmount` 撤回已记账 delta；归零后仅在 owner 仍指向该实例时调用 `PowerCmd.Remove`。这保持了同实例正常恢复路径，并避免通过新的 `Apply` 触发 Artifact 或授予倍率。

## 进行中

- 正在逐面检查归零 `Removed` 与显式 purge 的区分、同实例归零恢复的数值方向、异步重入的时序，以及目标文件之外已通过的 Wrath 行为是否未被本次差异触碰。

## 未知

- 尚未对当前目标文件执行实机或探针运行；以下结论仅是静态源码结论，不能替代运行时验证。

## 已确认（增量检查面 2）

### 归零 Removed 与显式 purge

- 引擎参考 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:247-253`：`ModifyAmount` 在 `SetAmount` 及后置 hook 后，仅按 `ShouldRemoveDueToAmount()` 调用 `Remove`；`PowerCmd.cs:291-298` 的显式 `Remove` 也最终调用 `PowerModel.RemoveInternal`。
- 引擎参考 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:575-580`：`Removed` 回调只有无参数事件，且在 owner 列表移除前触发；事件没有 removal cause 或调用来源字段。
- 目标文件 `DemonFormPower.cs:186-195` 只保存精确实例和 `strength.Amount == 0`。因此非零显式 purge 可静态区分并在 `:235-241` 清空旧账本、不恢复；但“自动归零移除”和“显式移除零值实例”在当前引擎回调契约下不可完全区分。
- 残余静态风险: 若显式 purge 的对象本身为零值、无 replacement 且 `grantedStrength != 0`，`DemonFormPower.cs:226-261` 会把它归类为 `zeroAggregate` 并直接恢复 `-granted`。这不是已证明的实机失败，但属于当前代码无法闭合的 cause ambiguity，必须保留为未知边界。

### 同实例正常恢复与新实例保护

- 目标文件 `DemonFormPower.cs:281-320`：退出清理先等待 `ForgetPurgedStrength`，保存 `tracked`，设置 `cleanupComplete`，再要求 `owner.GetPower<StrengthPower>()` 与 `tracked` 为同一引用后才 `SetAmount(strength.Amount - granted)`；归零时还要求 owner 仍持有同一实例才调用 `PowerCmd.Remove`。
- 目标文件 `DemonFormPower.cs:220-241`：旧实例已移除且当前为不同 replacement 时，`replacement` 分支先解除追踪并将旧 `grantedStrength` 置零，不会对 replacement 撤回旧账本；`DemonFormPower.cs:302-309` 在形态退出路径再次拒绝跨代扣减。
- 现有探针合同 `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\DemonScenarios.cs:163-189` 与上述静态控制流一致：同一零聚合场景要求恢复未归属的负 Strength，非零 purge 后的 replacement 要保持不被二次撤回。探针本轮未运行，因此这里只确认源码与合同的匹配，不宣称运行通过。

## 进行中（增量）

- 已完成实例身份、归零/purge、同实例恢复和 replacement 保护的静态检查。
- 正在完成异步重入/AfterRemoved 幂等与已通过 Wrath 行为的非干扰检查。

## 已确认（增量检查面 3）

### 异步重入与 AfterRemoved 幂等

- 目标文件 `DemonFormPower.cs:83-140`：刷新入口以 `data.refreshing` 拦截并发刷新；授予前后均显式 `await ForgetPurgedStrength`；`finally` 先复位 `refreshing`，再在形态已移除时统一调用 `RemoveGrantedStrength`。授予 hook 内移除形态时，`AfterRemoved` 不会递归等待当前授予，而是由 `finally` 收尾。
- 目标文件 `DemonFormPower.cs:271-295`：`AfterRemoved` 将 `removed` 置位；刷新中只延期处理，非刷新中进入清理；`cleanupComplete` 在实际撤回前设置，随后清零账本、解除事件订阅并清理实例证据。重复 `AfterRemoved` 静态上会被一次性闩锁拦截。
- 目标文件 `DemonFormPower.cs:302-320`：清理只对仍为同一 tracked 实例的 Strength 做 `SetAmount`，避免异步期间 replacement 进入后误撤回；归零后的 `PowerCmd.Remove` 也再次检查 owner 当前实例身份。
- `DemonFormStrengthTransactionPatch.cs:50-71,101-110,170-195` 的只读辅助证据表明 accepted delta 在真实 `SetAmount` 写入边界记录，异步 transaction 结束时恢复 `AsyncLocal` scope；本目标文件对该回调仅累加已接受值，不把 awaited hook 窗口内的其它写入计入本账本。

### 已通过的 Wrath 行为的非干扰检查

- 本次目标差异只修改 Strength 账本、移除收尾和相关注释；`DemonFormPower.cs:264-269` 的敌方伤害 additive 逻辑未改变，目标文件外的 `DemonReaperStancePower`、Watcher bridge、Reaper effect 也不在本次目标 diff 中。
- 历史运行证据 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:96-98` 与 `DEVLOG.md:2722-2724` 记录过 Wrath 场景 `status=passed`、真实出牌完成且 `unobservedFaults=[]`。本次静态差异没有触碰该姿态进入链或 Reaper 伤害路径，因此没有发现代码级回归入口。

## 未知

### 静态风险

- 当前引擎的 `Removed` 无 removal-cause 参数；目标文件只能以实例引用和零值标志推断来源。显式移除零值 Strength 与其它来源把聚合值归零后的自动移除仍会合并到同一 `zeroAggregate` 分支，见 `DemonFormPower.cs:186-195,226-261`。若产品契约要求严格区分这两种来源，当前写集无法从现有回调证据中证明完全闭合。
- `ForgetPurgedStrength` 的无 replacement 恢复使用 `ApplyInternal`（`:257-260`），发生在 `RemoveGrantedStrength` 设置 `cleanupComplete`（`:291`）之前；正常引擎路径没有证据显示该同步调用会再次触发同一 Demon 的 `AfterRemoved`，但恶意或非标准嵌套回调的重入边界未能由静态目标文件单独证明。

### 实机边界

- 按请求未构建、未运行探针、未运行游戏；因此 replacement 时序、零值显式 purge、同实例归零恢复、重复 `AfterRemoved` 和本次修改后的 Wrath 行为均未取得新的实机证据。
- 历史 Wrath `passed` 证据早于本次目标文件修改，只能证明此前版本的运行结果和本次静态非干扰关系，不能宣称本次修改后的运行时回归测试已通过。

## 结论

- 实例身份/代际保护、同实例撤回、新实例不继承旧账本、核心异步收尾和 `AfterRemoved` 一次性清理：静态审查通过。
- 归零自动移除与显式零值 purge 的严格区分：当前引擎证据不足，保留为明确未知边界和静态风险。
- Wrath 已通过路径：本次目标差异未触碰其代码路径；修改后未重新运行，结论限于静态非干扰。
