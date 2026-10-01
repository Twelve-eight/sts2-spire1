# 姿态形态六效果独立静态审查报告

- 审查日期: 2026-10-02
- 范围: Void/Demon 事务补丁、六效果契约、clone、bridge 与 MainFile 的独立源码审查。
- 限制: 只读静态审查；未构建、未测试、未部署、未启动游戏；不修改产品代码。
- 唯一审查报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\final-forms-effects-review-independent-20261002.md`

## 已确认

### 首条证据：Void 当前仍以支付事务状态与真实 CardPlay 身份共同决定免费额度

当前 `VoidFormEffectPower.cs:47-70` 的费用预览会跳过已有 `reservedCard` 之外的其它牌；`VoidFormEffectPower.cs:114-141` 的 `BeforeCardPlayed` 仍要求手动、首系列、未消费，并通过 `ownsReservation || !managedBySpend` 决定是否建立 `pendingFreePlay`。`VoidFormEffectPower.cs:143-155` 在匹配的 `AfterCardPlayed` 中清理 reservation/pending/blocked 并消费额度。

这证明最新代码已存在支付期卡牌身份守卫，但尚不能证明 F1 已关闭：必须继续核对 `VoidFormPlayTransactionPatch.cs` 是否等待 `SpendResources` 返回的真实 `Task` 完成、是否为每个支付调用保有独立 token、以及支付异常/取消和支付中入场是否能安全清理与阻断追溯消费。

## 进行中

- 正在读取最新 Void/Demon 事务补丁与六效果实现，并对照旧 `effects-review.md` 中 F1、F2、F3 结论重新定位当前行号。

## 未知

- F1 是否已关闭尚未知。
- F3 的 `SetAmount` 真实写入后捕获是否已闭合尚未知。
- 六效果契约、clone 状态、主线程/异步边界和同步阻塞旁路尚未完成核对。
- 尚无任何构建、测试或真实运行证据。

## 已确认

### F1 当前复核：REWORK，旧问题仍未闭合

静态结论：**REWORK（P1/P2 混合）**。当前补丁比旧版前进了真实 `Task` 观察、逐卡 token 和异常包装，但仍存在可由源码直接构造的控制流缺口。

#### 1. `SpendResources` 的真实完成被观察但没有成为出牌准入条件

- `VoidFormPlayTransactionPatch.cs:68-86` 的 `ObserveSpendCompletionAsync` 确实 `await result`，成功后设置 `token.SpendCompleted=true`，失败则 `Cancel(token)`。
- 但 `SpendToken.SpendCompleted` 在当前十文件中没有任何读取点；`ClaimForPlay`（`:168-187`）只检查 `OnPlayStarted`，`BeginPlay`（`:97-135`）只检查 `token.Reserved` 与 `PowerAtStart.IsReservationFor(card)`，没有要求 `SpendCompleted`。
- 因而在支付 Task 尚未完成、但重入路径提前进入 `OnPlayWrapper` 时，未完成支付 token 仍可被领取并进入免费额度转移。源码不能证明“真实完成后才能交付给 CardPlay”。

#### 2. 嵌套支付 token 与 power 的单一 `CardModel` reservation 没有一一所有权

- `BeginSpend`（`:45-65`）为每次调用建立独立 `SpendToken`，但 `VoidFormEffectPower` 的 reservation 仍只有一个 `CardModel? reservedCard`（`VoidFormEffectPower.cs:7-14,80-108`）。
- `ClaimForPlay`（`:168-187`）按同一 `CardModel` 的 token bucket 取“最新未开始” token；`BeforeCardPlayed`（`VoidFormEffectPower.cs:114-141`）又只用 `ReferenceEquals(data.reservedCard, cardPlay.Card)` 判定 `ownsReservation`，没有核对被领取的具体 `SpendToken`。
- 两次嵌套支付若复用同一 `CardModel`，后进入的 token 可以成为 `PlayState.Spend`，而 `BeforeCardPlayed` 仍会按 card 身份认领先前 token 的 reservation；随后 `AfterCardPlayed` 清掉单一 reservation，先前 token 仍在 bucket 中。这是 token/card 身份错配，不能称为所有权闭合。

#### 3. 支付中入场的 Void 会留下永久 `blockedCard`

- 若 `BeginSpend` 时没有 Void，token 的 `PowerAtStart` 为 null；支付 await 期间新入场的 Void 会在 `BeginPlay`（`:123-135`）看到当前 power，并调用 `current.BlockCard(card)`，同时将 `state.BlockedCurrentPower=true`。
- 该卡因 `managedBySpend` 且没有 reservation，`VoidFormEffectPower.BeforeCardPlayed`（`:114-141`）不会建立 `pendingFreePlay`，这部分“不能追溯消费新额度”的阻断方向是正确的。
- 但 `CompletePlay`（`:190-206`）和 `AbortForPatch`（`:208-218`）只在 `state.Spend == null` 时才处理 `state.BlockedCurrentPower`；本例 `state.Spend != null`，因此新入场 power 的 `blockedCard` 不会被清除。支付中入场会把该 CardModel 留在阻断状态，直到其它生命周期清理，异常和成功路径都不完整。

#### 4. 出牌异常不能清除已建立的 `pendingFreePlay`

- `VoidFormEffectPower.BeforeCardPlayed`（`:114-141`）建立精确 `CardPlay` pending 后，若 `OnPlayWrapper` 的异步 Task 失败，`ObservePlayCompletionAsync`（`VoidFormPlayTransactionPatch.cs:138-153`）调用 `AbortForPatch`。
- `AbortForPatch`（`:208-218`）对有 token 的路径只调用 `Cancel`；`Cancel`（`:88-95`）只释放 reservation 并删除 token，没有清除 `VoidFormEffectPower` 的 `pendingFreePlay`。当前效果也没有提供面向该异常路径的 `AbortPending(CardPlay)` 方法。
- 因而失败的 CardPlay 可能留下 `pendingFreePlay != null`，后续牌持续被 `ShouldSkip`（`VoidFormEffectPower.cs:47-70`）阻断；这直接违反“支付/出牌异常清理”。

#### F1 最小返工范围

只需继续收窄到 `VoidFormEffectPower.cs` 与 `VoidFormPlayTransactionPatch.cs`：

1. 将 `SpendCompleted` 纳入 PlayState/BeforeCardPlayed 的准入，未完成 Task 不得转移 allowance。
2. 令 BeginSpend 返回逐调用 ownership token，并将 token 身份传递到真实 CardPlay；不能仅以 `CardModel` 单值 reservation 匹配嵌套调用。
3. 为支付中入场的 token 记录并在 Complete/Abort 两条路径清除 `blockedCard`。
4. 增加按精确 `CardPlay` 清理 pending/reservation 的异常路径，并覆盖同步 Finalizer、异步 Task fault/cancel。

以上为源码反例，不是行为测试结论；未执行游戏或 probe。

### F3 当前复核：写入后捕获的源码结构已闭合，但安装与目标 IL 仍未证实

静态结论：**条件性 PASS，实机/安装状态未知**。

- 引擎研究源 `PowerModel.cs:542-553` 明确顺序为 `_amount = amount`（`:549`）之后才触发 `DisplayAmountChanged`（`:550`）与 `Owner.InvokePowerModified`（`:551`）。
- `DemonFormStrengthTransactionPatch.cs:261-295` 的 transpiler 查找 `_amount` 的唯一 `stfld`，在其后插入 `Ldarg_0; CaptureStoredAmount`；不再依赖事件之后的聚合 amount 值。
- `BeginSetAmount`（`:133-145`）保存写入前 amount；`CaptureStoredAmount`（`:153-168`）在真实 stfld 后保存已写入值并标记 `Wrote`；`RecordSetAmount`（`:174-195`）即使 `SetAmount` 后续同步事件抛异常，也会从独立的 `Stored-Before` 差值记账。因此旧 F3 的“写入完成后异常导致完全漏记”在这段源码中已被修复。
- `DemonFormStrengthTransaction.ModifyAmountAsync`（`:50-71`）等待真实 `PowerCmd.ModifyAmount` Task；嵌套 depth 的回收（`:90-110,203-216`）也绑定到 Task 完成而不是同步返回。

但不能写成无条件通过：

- `MainFile.cs:107-126` 对每个 Harmony patch type 单独捕获失败并继续初始化；如果 `DemonFormSetAmountCapturePatch` 或其 transpiler 因目标 IL 不匹配而失败，模式不会自动 fail-closed，`FormStanceWatcherBridge` 也没有验证该事务 patch 是否安装。
- `DemonFormStrengthTransactionPatch.cs:267-293` 的“恰好一个 `_amount` stfld”是运行时目标 IL 前提；本轮禁止构建/启动，因此未证明当前目标二进制实际满足并安装该 transpiler。

## 进行中

- F1 已找到四个当前源码级缺口，暂不能判通过。
- F3 的写入边界逻辑已完成静态核对，但仍需把 Harmony 安装成功作为独立未知边界记录。
- 下一步继续核对六效果契约、clone 状态、主线程/异步边界和同步阻塞旁路。

## 未知

- 未构建、未测试、未部署、未启动游戏；没有当前二进制的 Harmony patch 安装证据。
- 没有运行支付 Task 异步挂起、同 CardModel 嵌套支付、支付中入场、OnPlayWrapper fault/cancel 或 SetAmount 事件抛错场景。
