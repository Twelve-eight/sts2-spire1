# Void Form transaction worker report - 2026-10-02

## 已确认

- 当前源码基线仅涉及两个目标产品文件；本轮尚未构建、未运行游戏、未写入其它产品文件。
- `VoidFormPlayTransactionPatch.cs:68-86` 已把 `SpendResources` 的返回 `Task<(int, int)>` 包装为观察 Task，并在观察到成功完成时写入 `SpendToken.SpendCompleted`；异常路径调用 `Cancel`。
- `VoidFormPlayTransactionPatch.cs:168-187` 的 `ClaimForPlay` 当前只检查 `OnPlayStarted`，没有检查 `SpendCompleted`。因此付款 Task 尚未完成时，`OnPlayWrapper` 仍可能认领同一张卡的 token；这直接对应目标 1 的 P1 风险。
- `VoidFormPlayTransactionPatch.cs:190-218` 的 play 完成/失败路径只按 `PlayState.Spend` 或 `BlockedCurrentPower` 清理 reservation、token 和 blocked card；没有一个按同一 `CardModel` 清理 `VoidFormEffectPower.Data.pendingFreePlay` 的统一幂等入口。目标 2 需要补上卡身份守卫，并保留 `consumed=false` 的失败语义。
- `VoidFormPlayEffectPower.cs:21-27` 的 `Data` 只有 `pendingFreePlay`、`reservedCard`、`blockedCard` 三个卡状态字段；`VoidFormPlayTransactionPatch.cs:38-43` 的 token 只按 `CardModel` 放在 `ConditionalWeakTable` 中，没有按 `VoidFormEffectPower` 可枚举的 bucket。故当前 `BeforeSideTurnStart`（`VoidFormEffectPower.cs:175-187`）和 `AfterRemoved`（`VoidFormEffectPower.cs:189-196`）只能清字段，不能回收仍滞留在 card bucket 的 token；这直接对应目标 3 的 stale-token 风险。
- 已确认正常成功路径仍依赖真实 `CardPlay` 身份：`VoidFormEffectPower.cs:140-170` 以 `ReferenceEquals(data.pendingFreePlay, cardPlay)` 认领并标记 consumed。本轮设计不能改为伪造 engine callback，也不能破坏该身份比较。

## 进行中

- 正在核对本仓其它形态/事务实现、`CardPlay` 生命周期调用点及可用 power 生命周期钩子，以确定只在这两个白名单文件内实现：
  1. 只有成功完成的 payment Task 才能被 `ClaimForPlay` 认领；
  2. wrapper/observer 失败时按 `CardModel` 做幂等清理，且不设置 consumed；
  3. token 同时登记到 card bucket 与 power bucket，并由 power 生命周期回收。
- 设计取舍暂定：保留 card bucket 作为按卡匹配的首消费者索引，新增 power bucket 作为可枚举 owner 索引；所有移除操作都必须同时从两侧删除，并以 token/card/power 的引用身份做守卫，避免误清理另一张卡。
- 本轮不执行构建、测试、部署或游戏运行；最终报告会明确这些验证边界。

## 未知

- 需要继续从源码与已有研究确认 `OnPlayWrapper` 的同步异常、返回 Task 异常、以及 `BeforeCardPlayed`/`AfterCardPlayed` 的实际先后关系；在此之前不能把现有 hook 顺序当作运行时事实。
- 需要确认 `CardModel.Owner`、`PowerModel.Owner` 在 `AfterRemoved` 和异常清理窗口中的可用性，避免生命周期末尾访问失效 owner。
- 尚未确认现有项目 API 是否允许在不新增 engine callback 的前提下，从 `VoidFormEffectPower` 调用 transaction 清理入口；若 API/生命周期无法安全满足某个目标，必须在最终报告标记 flag，而不是写 stub。

## 已确认（第二批证据：引擎时序与生命周期）

- 研究源码 `research/_decomp/CardModel_full.cs:1807-1819` 与 `.tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1807-1819` 一致：`SpendResources` 先 `await SpendEnergy`、再 `await SpendStars`，成功后才返回资源 tuple；任一 awaited hook 抛错都会使返回 Task 失败。
- `.tmp/dllsrc/MegaCrit.Sts2.Core.GameActions/PlayCardAction.cs:92-103` 显示正常手动路径先 `await _card.SpendResources()`，再调用 `await _card.OnPlayWrapper(...)`。因此修复会保留该真实路径，只把 token 认领条件收紧为“观察到 payment Task 成功且 token 仍在 bucket”。
- `.tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1915-1969` 显示真实 `CardPlay` 在 `Hook.BeforeCardPlayed` 前创建，并在 `Hook.AfterCardPlayed` 后完成；`VoidFormEffectPower.cs:140-170` 的 `ReferenceEquals(data.pendingFreePlay, cardPlay)` 可以继续作为成功消费的身份依据，不需要伪造 callback。
- `.tmp/dllsrc/MegaCrit.Sts2.Core.Hooks/Hook.cs:263-294` 显示 `BeforeCardPlayed` 和 `AfterCardPlayed` 都是 awaited hook；任何 hook 异常会沿 `OnPlayWrapper` Task 传播，故 wrapper observer 的 catch 是合法的失败清理边界。
- `.tmp/dllsrc/MegaCrit.Sts2.Core.Commands/PowerCmd.cs:291-298` 显示 `PowerCmd.Remove` 先 `RemoveInternal()`，然后仍以 `power.Owner` 调用 `AfterRemoved(power.Owner)`；`.tmp/dllsrc/MegaCrit.Sts2.Core.Models/PowerModel.cs:624-631` 说明 `AfterRemoved(Creature oldOwner)` 是真实引擎生命周期钩子。清理接口只需使用 power/token 引用，不依赖已经从 owner 的 powers 列表中查回本 power。
- 设计补充：失败回滚需要区分“本次 free allowance”与“同卡的后续非 free wrapper”。仅按 `CardModel` 把 `consumed` 复位会误回滚先前成功的同卡交易；因此会在 `VoidFormEffectPower.Data` 记录 `consumedCard`，并只由持有本次成功 payment reservation 的 `PlayState` 请求 rollback。

## 进行中

- 已完成引擎源码核对，正在编辑两个白名单文件：payment completion gate、按卡幂等失败清理、按 power token bucket 以及 turn/removal 回收。
- 编辑后将只做文本级审查：重新读取两个源文件、检查报告与 `git status` 的白名单状态；不构建、不运行游戏、不部署。

## 未知

- 未执行编译，因此当前不能确认目标工程实际引用的 BaseLib/game DLL 与研究源码版本完全一致；报告只把研究源码当作设计证据，不把它写成运行验收。
- 未运行故障注入或游戏回归，因此成功手动路径、取消 payment、wrapper 异常及 power removal 的最终行为仍需后续授权下验证。

## 已确认（实现后静态复核）

- **目标 1 已实现（源码证据）**：`VoidFormPlayTransactionPatch.cs:212-240` 的 `ClaimForPlay` 在 card bucket 锁内只认领 `!Cancelled && SpendCompleted` 的最新未启动 token；若最新 token 仍在支付中，直接返回 null，不回退认领旧 token。`VoidFormPlayTransactionPatch.cs:288-302` 只有 payment Task 成功返回且 token 仍在 card bucket 时才写入 `SpendCompleted=true`。取消会在 `:99-118` 标记 token、清理同卡状态并从 bucket 移除。
- **目标 1 的无效手动 wrapper 防线保留**：`VoidFormPlayTransactionPatch.cs:143-160` 对没有有效 reservation 的手动 wrapper 记录同一 power 的 `BlockedPower`；因此未完成/已取消 token 不会让 `BeforeCardPlayed` 认领免费额度。AutoPlay 仍在 `:134-140` 释放 token，不消费 Void 免费次数。
- **目标 2 已实现（同卡、幂等、不消费）**：`VoidFormEffectPower.cs:138-158` 的 `ClearFailedPlay` 分别以 `ReferenceEquals(..., card)` 清理 `pendingFreePlay`、`reservedCard`、`blockedCard`，重复调用无副作用，另一张卡的状态不会被清理。`VoidFormPlayTransactionPatch.cs:268-286` 在 wrapper observer 失败或同步 finalizer 失败时调用该入口；`Cancel` 也复用同一清理入口。
- **失败后的 consumed 语义已显式隔离**：`VoidFormEffectPower.cs:24` 增加 `consumedCard`；正常路径 `:184-197` 仍必须匹配真实 `CardPlay` 引用才记录 consumed。只有持有本次有效 free reservation 的 `PlayState.FreePower`（`VoidFormPlayTransactionPatch.cs:36, :123-131`）才允许失败回滚 `consumedCard`，避免同卡后续非 free wrapper 误回滚先前成功交易。
- **目标 3 已实现（双索引、按 power 回收）**：`VoidFormPlayTransactionPatch.cs:45-46` 增加 card bucket 之外的 `PowerTokens`；`BeginSpend` 在 `:63-75` 同时登记；`Remove` 在 `:304-324` 同时从 card bucket 和 power bucket 删除；`CancelForPower` 在 `:193-210` 枚举该 power 的 token 快照并逐个幂等取消。`VoidFormEffectPower.cs:199-224` 在每次 `BeforeSideTurnStart`（包括 owner 不在 multiplayer extra-turn participant list 的情况）和 `AfterRemoved` 回收残留 token；只在 owner 参与当前 turn 时刷新 allowance 字段。
- **真实成功路径未伪造 callback**：`VoidFormEffectPower.cs:184-197` 仍以 engine 提供的同一 `CardPlay` 对象做 `ReferenceEquals`，没有新增或伪造 engine callback。`CompletePlay`（`VoidFormPlayTransactionPatch.cs:243-266`）只做 token/reservation/blocked 收尾，不替代真实 `AfterCardPlayed` 消费。
- 静态检查结果：对两个产品文件执行 `git diff --check` 无错误；文本断言检查通过：`completed_gate=True`、`completion_mark_is_bucket_guarded=True`、`cancel_clears_card_state=True`、`dual_bucket_removal=True`、`power_turn_cleanup=True`、`power_remove_cleanup=True`、`same_card_failure_guards=True`、`real_cardplay_identity=True`。
- 本轮未构建、未运行游戏、未部署。除用户指定的两个产品文件和本报告外，本轮没有写入其它产品文件；仓库其余脏项/未跟踪项属于既有工作区状态，未纳入本轮修改。

## 进行中

- 本请求范围内的两个源文件实现与静态审查已完成；没有留下 stub，也没有发现必须以不存在的 engine callback 填补的实现缺口。
- 等待后续明确授权后，才可进行编译、故障注入或游戏回归；这些不属于本轮交付，且本轮按用户要求不执行。

## 未知

- **FLAG-UNVERIFIED-BUILD-RUNTIME**：按用户要求未构建，不能在本报告中声称 C# 编译通过、Harmony 绑定通过或运行时行为通过。
- **FLAG-UNVERIFIED-FAULT-INJECTION**：未运行 payment cancellation、payment completion without wrapper、wrapper synchronous throw、wrapper awaited failure、AfterRemoved 和多人 extra-turn 的故障注入；静态控制流已覆盖目标，但实机/探针证据仍缺。
- 研究源码和实际工程引用 DLL 的版本一致性未在本轮通过构建确认；研究路径只作为设计时序证据。
