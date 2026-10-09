# Lifecycle safety cardinality r39 实施报告

## 已确认

- 修改前 SHA256(`LifecycleSmokeRunner.cs`): `0224CFEBE15147F6F0E9C8CC0E4C5034BA651EEDBC67FF0549C981765C065DE9`
- 修改后 SHA256(`LifecycleSmokeRunner.cs`): `A5DE9B1C28AA2487016E3F2DF4D68FE6DCD00106AB5AF3EDA29A8E86A79DDFA7`
- 任务只允许写: 本报告、`gate-notice.json` 与 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs`。
- r38 真实报告证明生产安全 owner 已安装 2 个 prefix，但测试契约仍按 1 个 prefix 判定，所以 `safetyGuardSurvivesShutdown=false` 是旧 cardinality 造成的测试载体失败，不是生产保护失败。
- 真实报告中的两个 targets 精确为:
  - `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power)`
  - `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction()`
- 真实报告中的 prefixPatchTypes 两项均为 `Forms.FormsCode.FormStanceSafetyGuard`，prefixDeclaringMethods 为 `RemovePrefix` 与 `PlayPrefix`。
- 生产源码 `FormStanceSafetyGuard.cs` 第 63-98 行确认两个 target 都安装 prefix；第 123-160 行确认 `ProofHoldsLocked` 要求两个 target 各自恰好一个本 owner 的 prefix。
- 未改生产 Forms，未改中央 PowerShell 控制器，未改共享配置，未写 C:。
- 本轮未派发子代理；执行者为当前 Codex harness 主会话，任务请求指定模型 `global:deepseek-v4.1-flash`，路由 `wb2api`，未做额外模型或路由替换。

### 精确 delta(相对修改前本文件状态)

1. 第 46 行后新增常量，原第 47 行前插入 5 行(新第 47-51 行):
   - `RemoveTargetIdentity` = `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power)`
   - `PlayTargetIdentity` = `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction()`
   - `SafetyPrefixType` = `Forms.FormsCode.FormStanceSafetyGuard`
2. `SafetyGuardProof`: 原第 414-434 行 -> 新第 421-439 行。
   - `prefixCount == 1` -> `prefixCount == 2`。
   - 旧单 target 字符串比较(`targets.Length == 1` + 只匹配 Remove) -> `SameSet(targets, {RemoveTargetIdentity, PlayTargetIdentity})`，并保留 `removeCanonical == RemoveTargetIdentity` 的精确 Remove identity 校验。
   - 旧单 prefixType 校验 -> `patchTypes.Length == 2` 且每一项都等于 `Forms.FormsCode.FormStanceSafetyGuard`。
   - 旧只接受 `RemovePrefix` -> `SameSet(declaring, {"RemovePrefix", "PlayPrefix"})`。
   - `removeMethodFound`、`removePatchedBySafetyOwner` 仍为必须 true。
3. `SafetyGuardSameIdentity`: 原第 436-459 行 -> 新第 441-470 行。
   - 计数门禁 `beforeCount == 1` -> `beforeCount == 2`。
   - targets/prefixPatchTypes/prefixDeclaringMethods 由拼接字符串顺序比较改为集合比较，允许 Harmony metadata 顺序变化。
   - Remove method identity 与 canonical identity 仍要求前后精确相等且非空。
4. 新增 `SameSet` 辅助方法(新第 468-470 行): 对两个字符串数组按序数排序后做 `SequenceEqual`，即集合相等(允许顺序变化、拒绝集合变化)。

### 未降低的门禁

- `SafetyGuardProof` 仍要求 `removeMethodFound`、`removePatchedBySafetyOwner` 为 true，且 prefix 数量精确为 2。
- 额外第三方 prefix 不会被算入 `Forms.FormStanceSafety` owner: `SafetyGuardEvidence` 仍按 `patch.owner == SafetyOwner` 过滤。
- `noStacking`、`noResidual`、duplicate fail-closed、`bridgeFailedClosedAfterDuplicate` 等其它生命周期门禁未改。
- JSON 字段形状未改，`targets`、`prefixPatchTypes`、`prefixDeclaringMethods` 仍完整输出。

## 进行中

- 无。最小测试载体修复已完成并落盘。

## 未知

- 未构建、未 lint、未运行测试、未运行游戏、未部署；修复后的编译与真实烟测结果未知。
- 修复后真实生命周期烟测是否把 `safetyGuardSurvivesShutdown` 变为 true 尚未在本轮验证，需由主会话集中构建/运行后确认。