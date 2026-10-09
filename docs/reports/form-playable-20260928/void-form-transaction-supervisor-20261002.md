# Void Form transaction supervisor review - 2026-10-02

## 审查边界

- 原生 wait 已等待实现代理 `01a0fc4b-dd4f-7543-bb24-2892d14d7703` 完成；其 turn 于 2026-10-02 完成并已将修改落盘。
- 本审查只读以下两个产品文件：
  - `mod/Spire1Code/Forms/VoidFormPlayTransactionPatch.cs`
  - `mod/Spire1Code/Forms/VoidFormEffectPower.cs`
- 本监督会话不修改产品代码，不构建，不运行游戏，不部署。
- 以下第一批结论均为静态源码结论，不是实机通过结论。

## 已确认

- `VoidFormPlayTransactionPatch.cs:79-97,212-237,288-301`：Spend completion gate 已存在。观察 Task 只有在 `await result` 成功且 token 仍在同一 card bucket 中时，才由 `MarkSpendCompleted` 写入 `SpendCompleted=true`；`ClaimForPlay` 对最新未启动 token 遇到 `Cancelled` 或 `!SpendCompleted` 会拒绝认领。静态上覆盖了“付款未完成/取消不得消耗免费次数”的前置。
- `VoidFormPlayTransactionPatch.cs:45-75,304-323`：每个 token 在创建时登记到 `CardModel` bucket，并在存在 `PowerAtStart` 时登记到对应 `VoidFormEffectPower` bucket；`Remove` 会分别从两个 bucket 移除，`CancelForPower` 通过 power bucket 枚举并调用 `Cancel`。在无并发竞态的单 token 生命周期中，card/power bucket 的配对关系是闭合的。
- `VoidFormEffectPower.cs:163-179,184-196`：正常 free-card 语义仍以真实 `CardPlay` 对象身份认领。`BeforeCardPlayed` 保存传入的 `cardPlay`，`AfterCardPlayed` 只在 `ReferenceEquals(data.pendingFreePlay, cardPlay)` 成立时清理并设置 `consumed=true`；没有伪造 engine callback。
- `VoidFormPlayTransactionPatch.cs:138-160,243-286` 与 `VoidFormEffectPower.cs:138-158`：已加入按 `CardModel` 身份守卫的失败清理入口，覆盖 `pendingFreePlay`、`reservedCard`、`blockedCard`，并通过 `Cancel`/`Remove` 进行重复清理。`VoidFormEffectPower.cs:153-157` 仅在 `consumedCard` 与失败 card 同一引用时回滚 consumed 状态。

## 进行中

- 继续检查 Spend token 与 free-play state 在同一 `CardModel` 的嵌套/并发失败下是否会互相清理。
- 继续检查不同 card 共用一个 `VoidFormEffectPower` 时，reservation、blocked 与 power bucket 的隔离是否保持。
- 继续核对 `BeforeCardPlayed` / `AfterCardPlayed` 的 exact `CardPlay` 语义在 wrapper 成功、异常、重复 callback 与 observer failure 路径中的一致性。
- 继续检查异步 observer、Harmony finalizer、turn-start/removal cleanup 是否具有明确主线程边界，以及失败路径是否可能在成功 callback 之后错误回滚或遗漏。

## 未知

- 尚未运行游戏、故障注入或任何动态测试；不能把上述静态 gate、清理或 bucket 结论写成运行时通过。
- 当前两个文件没有显式的主线程切换/断言；`ObserveSpendCompletionAsync` 与 `ObservePlayCompletionAsync` 的 continuation 是否始终回到引擎主线程，仍需由运行时调度契约或实机验证确认。

## 已确认（第二批：同卡嵌套、异卡并发与清理隔离）

- `VoidFormPlayTransactionPatch.cs:221-237`：同一 card bucket 中，如果最新 token 尚未完成或已取消，`ClaimForPlay` 会直接返回 null，不会回退认领更旧 token；这避免了把旧 payment 与新 wrapper 错配，但只覆盖 transaction token 入口。
- `VoidFormEffectPower.cs:163-179`：SpendCompleted gate 没有完全传递到真实 `BeforeCardPlayed` 认领条件。`BeforeCardPlayed` 只用 `ReferenceEquals(data.reservedCard, cardPlay.Card)` 计算 `ownsReservation`，并用 `IsManaged(cardPlay.Card)` 判断是否有 token。因而同一 `CardModel` 上旧 token 持有 reservation、而新 token 仍在飞行或新 wrapper 没有成功 claim 时，仍可能满足 `ownsReservation`，写入 `pendingFreePlay` 并让错误的同卡 CardPlay 进入免费认领路径。该问题是静态 P1 风险，位置为 `VoidFormEffectPower.cs:166-178`。
- `VoidFormPlayTransactionPatch.cs:99-117` 与 `VoidFormEffectPower.cs:138-158`：`Cancel` 不区分 token 是否拥有 reservation，始终按 token 的 `Card` 调用 `ClearFailedPlay(..., rollbackConsumed:false)`。同一 `CardModel` 的嵌套/并发第二 token 失败时，可清掉第一 token 所属的 `pendingFreePlay`、`reservedCard` 或 `blockedCard`；这些字段守卫只隔离不同 card，不能隔离同一 card 的不同 transaction。该问题是静态 P1 风险。
- `VoidFormEffectPower.cs:141-152`：异卡失败的字段清理使用 `ReferenceEquals(..., card)`，因此 card B 的失败不会直接清掉 card A 的 pending、reservation 或 blocked 状态。`VoidFormPlayTransactionPatch.cs:304-323` 也按 token 从 card bucket 和其 `PowerAtStart` 对应的 power bucket 移除；在单线程、无交错窗口的异卡并发模型中，隔离关系成立。
- `VoidFormPlayTransactionPatch.cs:63-75,101-117,193-209,304-323`：card bucket 与 power bucket 的添加、移除、power 级回收分别加锁，但没有跨两个 bucket 的原子提交/删除。若异步 continuation 与 power 生命周期钩子交错，可能短暂观察到一侧已有 token、另一侧尚未登记或已移除；这不是已证实的运行时故障，但属于并发边界上的未闭合证明。

## 进行中

- 正在逐行核对真实 `CardPlay` callback 的成功/异常边界，重点确认 `AfterCardPlayed` 已经写入 consumed 后，wrapper observer 失败是否还会进入 `AbortForPatch`，以及该路径是否会错误保留或回滚 allowance。
- 正在检查 `ObserveSpendCompletion`、`ObservePlayCompletion`、Harmony finalizer 与 `BeforeSideTurnStart`/`AfterRemoved` 的重复调用组合，确认清理幂等性是否只对同一 transaction 成立。
- 正在完成主线程边界审查；当前已确认目标文件内没有显式 dispatcher、线程断言或同步上下文保证。

## 未知

- 尚未动态复现同卡双 token、同卡嵌套 wrapper 或异卡交错 cleanup；以上 P1 项是基于控制流和引用身份的静态结论。
- 尚未证明两个 bucket 的独立锁在引擎实际调度中是否被单一主线程串行化；因此不能将“异卡并发安全”写成通过。

## 已确认（第三批：CardPlay 精确身份、wrapper 失败边界与线程边界）

- 引擎参考源码 `.tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1915-1926,1963-1969` 显示：每个 play loop 先创建新的 `CardPlay`，随后 await `Hook.BeforeCardPlayed`；卡牌逻辑和附加逻辑完成后才 await `Hook.AfterCardPlayed`。目标文件 `VoidFormEffectPower.cs:163-179,184-196` 保留了这个真实对象身份，成功消费条件不是按 card 名称、索引或新建对象匹配。
- `VoidFormPlayTransactionPatch.cs:163-177` 的 observer 包裹的是整个 `OnPlayWrapper` Task，而不是只包裹 `BeforeCardPlayed` 到 `AfterCardPlayed` 的区间。引擎参考源码显示 `AfterCardPlayed` 之后仍有 `OnPlayWrapper` 的收尾 await；因此若收尾阶段失败，仍会进入 `AbortForPatch`。
- `VoidFormPlayTransactionPatch.cs:268-285` 与 `VoidFormEffectPower.cs:153-157` 的失败回滚不是完全按 `CardPlay` 对象身份进行：它按 `CardModel` 判断 pending/reservation/blocked，并在 `FreePower` 为真且 `consumedCard` 是同一 card 时回滚 consumed。静态上，这可能回滚一个已经成功经过 `AfterCardPlayed` 的 free play；同时，对没有 `FreePower` 的失败状态不回滚 consumed，形成不同失败入口的语义不一致。该项需要按产品契约定级，当前列为 P1 候选而非动态确认。
- `VoidFormEffectPower.cs:184-194` 的 `AfterCardPlayed` 在 exact `ReferenceEquals` 成功后立即设置 `consumedCard` 和 `consumed=true`，所以“callback 成功但 wrapper 后续收尾失败”的状态转换确实存在；这不是假设的不存在 callback。
- `VoidFormPlayTransactionPatch.cs:88-95,166-177,347-356,383-392` 覆盖了 payment observer、play observer 以及两个 Harmony finalizer 的异常入口。重复调用大多依赖 `ReferenceEquals`、集合 Remove 的幂等性和空状态判断；但同一 card 的不同 transaction 仍会共享 `ClearFailedPlay` 的按 card 清理，因此重复清理的幂等性不等于 transaction 隔离。
- `VoidFormPlayTransactionPatch.cs:88-95,166-177` 没有显式 `ConfigureAwait`、主线程 dispatcher 或线程断言；`VoidFormEffectPower.Data` 的读写也没有锁。静态上只能确认 bucket 层有局部锁，不能确认 observer continuation 一定在引擎主线程执行。该项保持为未验证边界，不能写成主线程安全。

## 进行中

- 复核 turn-start 与 power removal 的 cleanup 顺序，尤其是 `CancelForPower` 的 snapshot、逐 token Cancel、以及之后 Data 字段重置之间的交错。
- 复核最终报告是否把静态已确认、静态 P1 风险、设计候选风险和未运行实机边界分开表达，并记录实现代理已落盘但未由本监督会话修改代码。

## 未知

- `CardModel.OnPlayWrapper` 的实际运行是否保证所有 continuation 都在 Godot/引擎主线程，当前未由本审查动态证明。
- `AfterCardPlayed` 之后的任意异常是否在产品契约上应回滚本次 free allowance，当前仅能依据源码指出两条失败路径的回滚策略不一致。

## 已确认（第四批：turn-start、power removal 与最终静态结论）

- `VoidFormEffectPower.cs:199-215`：`BeforeSideTurnStart` 会先调用 `CancelForPower(this)`，因此正常 snapshot 中的残留 token 会走 `Cancel`，并从 card/power bucket 移除；只有 `participants.Contains(Owner)` 时才刷新本 power 的 allowance 字段。该顺序在静态上保留了“先回收 token，再刷新 owner-turn 状态”的意图。
- `VoidFormEffectPower.cs:218-226`：`AfterRemoved` 也调用 `CancelForPower(this)`，随后清空 `pendingFreePlay`、`reservedCard`、`blockedCard`、`consumedCard`。在没有并发新 token 的前提下，power removal 的 token 回收路径是存在且双 bucket 对称的。
- `VoidFormPlayTransactionPatch.cs:193-209`：power bucket 使用快照后逐个调用 `Cancel`，避免在遍历原列表时直接修改列表；`Remove` 的两侧 Remove 操作可重复执行而不抛错。这证明了集合操作层面的幂等性，但没有证明状态层面的 transaction 隔离。
- 最终静态判定：
  1. SpendCompleted：对“单 token、正常 payment observer 顺序”已加 gate；但 `VoidFormEffectPower.cs:166-178` 的同卡 `CardPlay` 条件仍可绕开该 gate，不能判定为完整闭合。
  2. token card/power bucket：正常单线程生命周期的登记/移除配对已闭合；跨 bucket 原子性和异步交错仍未证明。
  3. 清理幂等性：同一 transaction 的字段清理和集合移除基本幂等；同一 `CardModel` 的不同 transaction 不能隔离，`VoidFormPlayTransactionPatch.cs:112` 调用的按 card 清理是静态 P1 风险。
  4. 同卡嵌套：未通过静态审查；reservation 与 pending 都只按 card 引用而非 token/generation 认领，见 `VoidFormEffectPower.cs:166-178`、`VoidFormEffectPower.cs:138-158`。
  5. 异卡并发：按 card 引用的清理守卫能阻止常规 B 清理 A；但这只是在单线程或外部串行化成立时的静态结论。
  6. BeforeCardPlayed/AfterCardPlayed：成功路径保留 exact `CardPlay` identity；失败路径包裹的是整个 wrapper，且 callback 后仍可能失败，导致 `FreePower` 分支回滚而非 `FreePower` 分支不回滚的语义差异。
  7. 主线程边界：目标文件没有显式主线程 dispatcher/断言；只能标记未验证，不能声称通过。

## 进行中

- 本轮源码审查的所有指定检查面已完成；此节保留为工作流记录，不代表还有未完成的代码修改。
- 监督会话未修改两个产品文件；只追加了本报告。

## 未知

- 未构建，未运行游戏，未做故障注入，未做同卡嵌套或异卡并发实机复现；因此所有“通过”措辞均限于上述静态范围。
- 未确认引擎/运行时是否提供未写在这两个文件中的主线程串行化保证；如果不存在该保证，`Task` continuation 与 power 生命周期 hook 的交错风险需要后续修复和动态验证。
- 未确认产品契约是否要求 `AfterCardPlayed` 已成功后、wrapper 收尾失败时回滚 allowance；当前代码在 `FreePower` 与非 `FreePower` 状态上的不同处理已记录为静态风险，不将任一策略写成实机事实。
