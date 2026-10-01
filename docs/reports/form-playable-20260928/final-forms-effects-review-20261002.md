## 已确认

- 本轮以当前工作树源码为准，按用户指定使用 `6.1sol` / `agentrouter`；只做静态审查，不构建、不运行测试、不部署、不启动游戏。产品代码不修改，本文件是唯一写入报告路径。
- 当前虚空支付链的实际文件位于 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs`。初步源码证据：`BeginSpend` 在 `:45-65` 可为当前卡预留 `VoidFormEffectPower`，Harmony `SpendResources` postfix 在 `:246-252` 将原始返回 Task 包装为 `ObserveSpendCompletion`；真正的支付 Task 失败时 `:71-85` 调用 `Cancel`，同步 prefix/finalizer 失败路径在 `:237-264` 另行清理。该事实已落盘，F1 的异步完成、嵌套支付和支付中入场反例仍需以当前全部源码重新核对。

## 进行中

- 正在读取当前六形态效果、支付/力量事务补丁，以及 `MainFile`/`WatcherFormStanceBridge` 的注册绑定；旧 `effects-review.md` 仅作问题索引，不沿用其中行号或结论。
- 正在核对六形态契约、F1、F3、线程和异步边界、禁止同步阻塞及 clone 状态。

## 未知

- 当前尚未完成全量静态审查；未构建、未运行测试、未部署、未启动游戏。

### 增量 B - F1 当前支付事务复核（当前源码行号）

- 已确认正向修复：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:45-65` 在 `SpendResources` 的同步入口立即创建每卡 `SpendToken`，并在可用时先写入 `VoidFormEffectPower` 的 `reservedCard`；`:68-85` 只在真实支付 Task 完成后标记成功，取消或异常才释放 token/reservation。当前 `VoidFormEffectPower.cs:58-62` 对另一张卡拒绝读取进行中的免费额度，因此旧报告中“仅在异步 postfix 看到 Task 就释放、嵌套支付可趁窗口取额度”的旧行号/旧控制流已不再成立。
- 已确认正向修复：`VoidFormPlayTransactionPatch.cs:97-135` 对自动播放先取消 token，不消耗免费额度；没有匹配 reservation 的手动 wrapper 会调用当前 power 的 `BlockCard`，所以“支付已开始、随后才进入 Void”的卡不会追溯消费新额度。`VoidFormEffectPower.cs:140-170` 仍只用相同 `CardPlay` 引用、首次系列和完成回调消费一次。
- **仍成立的明确缺口（F1 清理边界，需 REWORK）：** `BeginPlay` 在 `VoidFormPlayTransactionPatch.cs:123-135` 只要 `ClaimForPlay` 得到任意 `SpendToken`，就可能对当前 Void 设置 `BlockCard(card)` 并置 `state.BlockedCurrentPower=true`；这正覆盖“支付开始时没有 Void、支付中进入 Void”的反例，因为 `BeginSpend` 即使未预留也在 `:54-64` 创建 token，且 `PowerAtStart` 可为 `null`。但 `CompletePlay` `:190-205` 在 `state.Spend != null` 分支只处理 `state.Spend.PowerAtStart`，不会清除 `state.BlockedCurrentPower` 所对应的当前 power；`AbortForPatch` `:208-217` 同样只 `Cancel` token，也不会清除该当前 power 的 block。结果是支付中入场的卡在成功或失败后都可能留下 `blockedCard`，随后 `VoidFormEffectPower.cs:46-62` 会继续跳过该卡，且 `BeforeSideTurnStart` 前新的支付也可能被旧状态卡住。该缺口不是旧报告 stale 行号的复述，而是按当前 `:123-135` -> `:190-217` 的控制流重新得到的结论。
- F1 当前小结：异步完成、嵌套防重入和支付中入场“不消费新额度”已确认；支付中入场后的 block 释放未闭合，不能判 PASS。

### 增量 C - F3 当前 SetAmount 事务复核

- 已确认旧 F3 “SetAmount 已写入后同步事件重入/抛异常，导致 Demon grant 全部漏记”的核心反例已被当前实现关闭。`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs:44-72` 用 `AsyncLocal<Scope?>` 包住真实 `PowerCmd.ModifyAmount`，`:90-111` 将嵌套深度的减少延迟到真实 Task 完成；`:117-127` 只在外层事务收到 `PowerReceived` 后 armed；`:133-168` 在 `_amount` 实际 `stfld` 后捕获存储值并锁住二次 arm；`:170-195` 的 SetAmount finalizer 在写入边界已跨过时按 `Stored-Before` 记账，即使后续 Display/UI/owner 同步事件异常。
- `DemonFormStrengthTransactionPatch.cs:203-231`、`:238-296` 的当前 Harmony 注册覆盖 `PowerCmd.ModifyAmount`、`CombatHistory.PowerReceived`、`PowerModel.SetAmount`，transpiler 要求 `_amount` 写入恰好一次并在写入后插入捕获；因此 F3 旧“只在事后读 Amount、同步事件异常漏记”的实现证据不再适用。
- 仍需区别于已关闭 F3 的独立边界：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:168-177` 的 `ForgetPurgedStrength` 只在已脱离 owner 且 `data.strength.Amount != 0` 时清账。若外部变更把同一个 `StrengthPower` 降到 0 并由引擎移除，`Amount == 0` 会跳过清账；当前静态源码没有把 `data.grantedStrength` 与“该 Strength 实例已被移除但最终 amount 为零”区分开。该边界需要继续列为未知/风险，不把它冒称为已由 F3 SetAmount 修复覆盖。
