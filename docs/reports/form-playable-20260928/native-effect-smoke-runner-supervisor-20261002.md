# native-effect-smoke-runner-supervisor-20261002

## 已确认
- 第一批增量证据（2026-10-02）：`git diff HEAD -- mod/Spire1Code/Run/FormNativeSmokeRunner.cs` 显示当前文件相对 `HEAD` 新增 380 行、删除 16 行；审查仅针对该工作树差异，未修改产品代码，未构建，未运行游戏。
- 当前新增真实效果入口位于 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:691-707`：现有姿态入口卡成功且 `formGateAfter.passed` 后，才调用 `RunEffectVerificationAsync`，并将其结果单独写入 `result["effectVerification"]`；效果失败会把场景状态置为 `failed`。
- 真实 Watcher 入口查找位于 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1118-1123`，按 `ModelDb.AllCards` 的精确 `Id.Entry` 查找；效果卡入口在 `:1495-1503` 固定使用 `WATCHER_STRIKE_P`，不是通过形态 Power 注入替代卡牌。
- 当前效果卡路径在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1504-1536` 依次执行真实 `CardPileCmd.Add(..., PileType.Hand, ...)`、手牌归属检查、真实 `PlayCardAction(card, target)` 创建及 `ActionQueueSynchronizer.RequestEnqueue(action)`；这些 Godot/战斗对象访问均经 `InvokeOnMainThreadWithTimeoutAsync` 包装。
- 初步风险边界：效果判断确实与 entry action 分离，但最终的 Calm/Wrath/Divinity 数值和能量断言仍需结合 `WatcherStrike_P` 及六个形态效果的准确实现逐项核对；当前尚不能把静态逻辑称为实机证据。

## 进行中
- 正在检查效果 action 的完成、异常、取消、超时和 detached-operation 生命周期，以及失败后是否保留原有清理与报告落盘语义。
- 正在核对 `Snapshot` 的敌人、力量、Doom、能量和回合证据是否足以支撑新增数值断言，尤其是多目标/目标排序和 Echo 重放边界。

## 未知
- 尚未完成对全部新增分支的静态结论；尚未确定是否存在会把错误运行时行为误判为通过的断言缺口。
- 无本轮实机运行证据；用户明确禁止构建和运行游戏，因此任何真实卡牌效果、伤害、能量或报告 JSON 的最终运行值均未知。
## 已确认（增量检查面：action 生命周期与主线程边界）
- [P1 静态风险] `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1560-1566` 在 `PlayEffectCardAsync` 的异步 continuation 中直接读取 `action.State`、`action.Exception` 和 `action.CompletionTask.Status`，而不是通过 `InvokeOnMainThreadWithTimeoutAsync` 读取。当前仓库的引擎参考实现 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\GameAction.cs:46-63,104-148,203-209` 显示 `State`/`Exception` 是由动作执行与取消路径写入的运行时状态；这与同文件 entry action 在 `:661-669` 经主线程门读取的做法不一致。触发条件是 effect action 完成后继续在后台 continuation 执行判定；最小修复是把这些字段的最终读取统一放入一个主线程快照，或只消费已由 `RecordActionEvidenceAsync` 主线程写入的状态；当前无实机证据证明跨线程读取安全。
- [P1 静态风险] 新 effect action 的异常兜底 `:1588-1611` 只记录失败，不保证已创建但尚未稳定入队的 `PlayCardAction` 被取消；尤其 `:1529-1536` 的主线程入队门超时会把 deferred submission 放入 `detachedOperations`，随后直接进入该兜底，未注册/等待对应 `action.CompletionTask`，也未调用 `TryCancelActionAsync`。触发条件是入队调用的主线程门超时或其后任一非 `TerminalOperationException` 异常；最小修复是对已创建 action 统一执行主线程取消并把其完成任务纳入 drain，避免最终 cleanup 与迟到的入队/执行竞态；当前只有静态证据。
- `:1491-1559` 的真实卡牌注入、手牌检查、真实 `PlayCardAction` 入队和完成任务等待顺序已确认；`TerminalOperationException` 路径会调用 `TryCancelActionAsync`，正常完成会调用 `RecordActionEvidenceAsync`，因此已保留 entry action 使用的主要完成/失败证据模式，但上述非终止异常分支仍不完整。

## 进行中（增量检查面：效果数值与运行时证据）
- 正在逐项核对 Calm、Wrath、Divinity 的期望数值是否与 `WatcherStrike_P` 及六个形态 Power 的真实语义一致，并检查 HP 差分是否会受目标死亡、Block、随机目标和 Replay 影响。
- 正在检查报告 JSON 的序列化形状、scenario failure 传播、cleanup gate 和最终 quit 证据是否在 effect action 失败时仍保持诚实。

## 未知
- 目前没有构建、游戏运行或真实 JSON 输出证据；不能确认跨线程读取、动作取消或任何伤害/能量结果在实际运行时的表现。
## 已确认（增量检查面：能量、伤害与效果证据）
- [P1 静态风险，问题 3/6] Wrath 的通过条件在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:1435-1447` 只有 `firstLoss == 7 && strength > 0`；读取到的 `doom` 只写入报告，未参与 `effectPassed`，也没有 Wrath 的能量消耗断言。结合 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs:35-44` 的契约，若 Reaper 未施加 Doom 或施加错误数值，只要伤害和力量仍正确，场景仍会被标为通过；若支付错误，也不会被该场景捕获。触发条件是 Reaper hook 失效、Doom 数值错误或资源支付异常；最小修复是要求 target Doom 等于该次实际 powered damage，并明确要求本次 1 费卡的能量差/`EnergySpent` 证据符合预期；当前没有实机证据。
- [P1 静态风险，问题 4/6] Divinity 的 `echoExtraPlayEvidence` 在 `:1449-1459` 只是把 `firstLoss == 12` 改名为 Echo 证据，没有读取 `CardPlay.PlayCount`/`PlayIndex` 或战斗历史中的真实 WatcherStrike_P 播放次数；`expectedManualPlayCount`（`:1362-1365`）同样只是输出字段，不参与断言。引擎参考 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Cards\CardPlay.cs:49-59` 明确存在 `PlayIndex`/`PlayCount`，而 `CombatHistory` 在 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat.History\CombatHistory.cs:24-28,38-46` 提供了卡牌播放记录。触发条件是其它伤害来源、卡牌数值变化或错误重复机制恰好造成 12 点损失；最小修复是加入同一 action 的真实 `PlayCount == 2`（及必要的每次 `CardPlay` 身份）证据，并让该证据参与通过条件；当前没有实机证据。
- [P2 静态风险，问题 5/6] `TotalEnemyHpLoss` 在 `:1334-1348` 以 before/after 的 `HittableEnemies` 数组下标配对，且只遍历 `Math.Min` 长度；`Snapshot` 在 `:1286-1302` 每次重新取 `HittableEnemies`。目标死亡会使 after 数组缩短，多个敌人被重新排序或随机 Serpent 伤害改变可命中集合时，下标不再代表同一 Creature，可能漏算或错配生命损失。触发条件是 6/7/12 点伤害击杀目标、Encounter 有多个敌人或敌人可命中状态变化；最小修复是按稳定 `CombatId`/对象身份在完整 `CombatState.Creatures` 上做差分，并单独保留目标证据；当前没有实机证据。
- [P2 静态风险，问题 6/6] effect action 的 unobserved-fault 门禁在 `:1560-1566` 只在 action 完成后、`after` 快照前检查一次；`RunEffectVerificationAsync` 在 `:1467-1470` 仅把 `unobservedFaultsSinceEffectStart` 写入结果，不要求该数组为空。若 fault 在 after 快照、场景 switch 或返回前到达，数值条件仍可把 `effectPassed` 置为 true。触发条件是异步 hook 在完成判定后才把异常推入 `TaskHelper.UnobservedFault`；最小修复是在最终通过前重新取基线并将新增 fault 作为硬失败，同时保留 fault evidence；当前没有实机证据。
- 已确认报告传播路径仍存在：`RunScenarioAsync` 在 `:691-717` 将效果失败写入顶层 `status/failure`，`TryWriteScenarioEvidence` 在 `:2052-2073` 对场景 JSON 写入失败改写为失败并重试，`RunAndQuitAsync` 的最终 pending/quit 证据路径未被本轮删除；但这些结论仅来自静态源码。

## 进行中（增量检查面：报告落盘、清理与最终结论）
- 正在确认 effect failure、detached drain、run cleanup 和 final quit 之间是否存在会把未收束动作误报为已清理的边界；目前未发现新增路径绕过 `cleanupSkipped`/`terminalFailure` 的明确证据，但尚未完成最终复核。

## 未知
- `WatcherStrike_P` 的真实 base 6/1 费契约已由本地反编译源确认，但未在本轮构建或实际运行中确认 ModelDb 注册、资源支付、实际总伤害、Doom、Echo `PlayCount` 或 JSON 序列化结果。
- 由于用户禁止构建与运行游戏，本轮不能提供实机证据；上述问题均为静态风险，不能用既有历史 smoke JSON 替代。
## 已确认（最终结论）
- 静态结论：`REWORK`，不是 `PASS`。真实 `WATCHER_STRIKE_P` 查找、`CardPileCmd.Add`、真实 `PlayCardAction`、完成任务等待、entry/effect 分离和场景 JSON 失败传播均已存在；但当前新增效果证据仍有 6 项需修正/收窄：
  1. [P1] `:1560-1566` effect action 状态在异步 continuation 中越过主线程边界读取。
  2. [P1] `:1529-1536,1588-1611` 入队门或后续非终止异常时，已创建 action 的取消/完成任务追踪不完整。
  3. [P1] `:1435-1447` Wrath 通过条件不要求 Doom 正确，也不要求 1 费资源支付正确。
  4. [P1] `:1449-1459` Divinity 用 12 点生命损失代理 Echo 重放，没有真实 `PlayCount`/播放历史断言。
  5. [P2] `:1334-1348` 以 `HittableEnemies` 数组下标差分生命，目标死亡或敌人重排时不稳定。
  6. [P2] `:1467-1470` 最终 effect 通过条件没有把结束前新增的 unobserved fault 作为硬失败。
- 静态风险与实机证据已分开：本报告没有任何本轮构建、游戏运行、真实伤害、能量、Doom、Echo 播放次数或 JSON 输出证据；历史 smoke 记录不覆盖本次 `git diff HEAD`。

## 进行中（最终结论）
- 本轮审查已完成；未修改产品代码，未构建，未运行游戏，未部署，未触碰 Steam 安装或共享 `mod_configs`。

## 未知（最终边界）
- 未知项保留为：上述跨线程与失败路径风险是否在具体运行时触发，以及真实 Watcher 注册、支付、伤害、Doom、Echo `PlayCount` 和报告序列化结果；这些必须由后续修正后的隔离实机冒烟确认，不能由本次静态审查代替。
