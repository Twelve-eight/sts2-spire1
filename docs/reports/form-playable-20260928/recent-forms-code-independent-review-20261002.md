# Recent forms code independent review - 2026-10-02

## 已确认

- [P1] 首条源码证据: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:92-125` 先计算本轮目标与 `delta`, 再调用 `DemonFormStrengthTransaction.ModifyAmountAsync`; 只有事务回调报告的 `accepted` 才累加到 `data.grantedStrength`. `:174-193` 同时把 `Removed` 监听绑定到实际的 `StrengthPower` 实例.这证明当前代码没有把请求中的 `delta` 直接当作已接受力量, 但尚未证明外部力量事务重入下的最终账本正确.
  - trigger: 外部力量修改或 hook 在 `PowerCmd.ModifyAmount` 的等待窗口内介入, 或同一 `StrengthPower` 被归零并移除.
  - current control flow: Demon 记录目标进度 -> 事务尝试写入 -> 只按 `accepted` 入账 -> 按实例监听移除.
  - minimum fix: 暂不提出代码修改; 先完成同一实例,归零移除,显式 purge 和 reentry 的静态闭环审计.
  - missing runtime evidence: 没有运行故障注入, 没有真实战斗或原生调度器证据.

- [P2] Demon 事务补丁的静态边界是清楚的.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs:44-71` 用 `AsyncLocal` 保存当前 grant scope; `:117-195` 在 `PowerReceived` 后只捕获目标实例的真实 `SetAmount` 写入, 并在 finalizer 中记录写入前后的实际差值; `:203-215` 将嵌套 `ModifyAmount` 的深度在其返回 Task 完成时回收.权威引擎 `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:231-246` 确认 `PowerReceived` 与 `SetAmount` 的顺序.
  - trigger: 同一力量在 Before hook 或写后事件中触发嵌套修改.
  - current control flow: 外层 scope depth 1 -> 嵌套目标修改 depth 2 -> nested write 不 arm -> 外层真实 SetAmount 才入账 -> Task 完成后恢复 depth.
  - minimum fix: 当前静态证据下不提出修改; 保留对直接 `SetAmount`,跨线程 continuation 和 Harmony 安装结果的运行核验要求.
  - missing runtime evidence: 未注入嵌套力量 hook, 未验证 transpiler 在目标运行时恰好匹配一个 `_amount` 写入, 未验证线程亲和性.

- [P1] Void 的成功支付 token 没有被 `OnPlayWrapper` 前置成功条件约束.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:68-86` 只在支付 Task 完成后写入 `SpendCompleted=true`; `:168-187` 的 `ClaimForPlay` 却只检查 `OnPlayStarted`, 从未读取 `SpendCompleted`. token 也只按 `CardModel` 进入 `ConditionalWeakTable` bucket, 没有 payment Task 或 `CardPlay` 身份配对.
  - trigger: `SpendResources` 仍在等待 `SpendEnergy` 或 `SpendStars` 的 hook 时, 同一卡的 `OnPlayWrapper` 提前或交错开始; 多个同一卡支付同时在飞时也会发生 token 误配.
  - current control flow: `BeginSpend` 保留 token -> `ClaimForPlay` 反向取第一个未启动 token -> `BeginPlay` 允许 manual play -> 支付失败时才由异步观察器 `Cancel`.
  - minimum fix: 认领必须以同一支付 token 的成功完成为前置, 并把 token 与真实 `CardPlay` 或支付序号一对一绑定; 未完成支付的 wrapper 必须进入失败清理, 不能先消费 Void allowance.
  - missing runtime evidence: 未运行嵌套支付,支付 hook 重入或同一卡并行支付; 正常引擎调用是否严格串行仍未在本审查中宣称.

- [P1] Void 的 play 失败路径会留下 `pendingFreePlay`.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:140-172` 只在 `AfterCardPlayed` 收到同一 `CardPlay` 时清理 pending; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:208-218` 的 `AbortForPatch` 对 token 只调用 `Cancel`, 而 `Cancel` `:88-95` 只释放 reservation 和移除 token, 不清理该卡的 pending.权威引擎反编译 `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1926-1965` 显示 `BeforeCardPlayed` 与 `AfterCardPlayed` 分离; `OnPlay` 或其等待链异常时不会自动到达 `AfterCardPlayed`.
  - trigger: 资源支付已成功, `BeforeCardPlayed` 已登记, 随后的 `OnPlay` 或效果等待抛异常或任务被取消.
  - current control flow: pending 绑定 exact `CardPlay` -> wrapper Task fault -> `AbortForPatch` 只处理 token/block -> `pendingFreePlay` 非空; 后续 `ShouldSkip` 在 `VoidFormEffectPower.cs:46-62` 持续跳过.
  - minimum fix: 为失败清理传递并校验 exact `CardPlay` 身份, 增加只清理该 pending 的 abort 分支; 不能用仅 `CardModel` 的粗粒度清理覆盖嵌套 play.
  - missing runtime evidence: 未执行 OnPlay 抛异常,owner 死亡中断和重复 AfterCardPlayed 的隔离复现.

- [P1] Void 成功支付但没有进入 wrapper 时, token 没有终态清理.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:71-80` 的成功分支只置 `SpendCompleted`; 后续唯一删除路径是 `CompletePlay` 或 `Cancel` (`:190-231`), 没有"支付完成但 play 被取消或丢失"的回收分支.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:175-187` 会清掉 power 内的 reservation, 但不会清 `ConditionalWeakTable<CardModel, TokenBucket>` 中的 token; stale token 仍使 `IsManaged` (`VoidFormPlayTransactionPatch.cs:155-166`) 返回 true.
  - trigger: `SpendResources` 成功返回后, 调度取消,卡离手或其它控制流使对应 `OnPlayWrapper` 不执行.
  - current control flow: token 保存在按卡的弱表 -> 成功只标记 -> 无 wrapper 则不 `Remove` -> 下一次同卡 play 仍可能从旧 bucket 认领旧 token, 或旧 token 长期保留对旧 power 的引用.
  - minimum fix: 建立成功支付到 wrapper 的可验证一对一 handoff 和取消/回合结束回收; 回收必须从 bucket 删除 token, 不能只重置 power 字段.
  - missing runtime evidence: 未构造"支付完成后取消 wrapper"场景, 因而不把条件性路径写成已实机复现.

- [P1] powers fallback 的待归零标记不是按调用计数, 同一 `PowerModel` 的重入可使门控放行.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:375-410` 用 `ConditionalWeakTable<PowerModel, object>` 每个实例只存一个 marker; `ShouldBlockFallbackBefore` 的第二次命中只是覆盖同一 marker, `TryConsumeFallbackBlock` 的第一次 Received 会把它移除, 后续同实例 Received 找不到 marker 就继续执行.权威引擎 `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:231-246` 确认 Before hook 是 await 边界, 所以同一 power 的嵌套或并行调用不是该数据结构能区分的.
  - trigger: fallback 层已安装且 `BeforePowerAmountChanged` 内发生同一 power 的嵌套或并发修改.
  - current control flow: call A 写一个 marker -> call B 覆盖同一 marker -> A 的 Received 消费 marker -> B 的 Received 放行修改.
  - minimum fix: 用每次调用的 token/计数或 AsyncLocal 事务栈配对 Before 与 Received, 不能以 `PowerModel` 单 key 作为多次 in-flight 调用的状态.
  - missing runtime evidence: 未在 fallback 安装状态下注入同实例嵌套 `ModifyAmount`; 当前正常中央门控路径是否启用 fallback 也未在本审查中重新验证.

- [P1] Demon 归零恢复会把旧实例的形态账本套到任意后续新 `StrengthPower` 实例.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:206-252` 只用旧实例的 `Removed` 时 `Amount == 0` 标志判定 `zeroAggregate`; 一旦判定成立便 `UntrackStrength`, 清零账本, 再对当前 `owner.GetPower<StrengthPower>()` 执行 `current.Amount - granted` (`:239-249`). 如果旧 aggregate 归零后, 在 `ForgetPurgedStrength` 前已有其它来源新建了一个不同实例, 当前代码没有实例世代或来源身份校验, 仍会从新实例扣掉旧形态增量.
  - trigger: 形态力量与外部力量合计归零并移除旧实例, 随后另一个来源在 Demon 清理前重新施加 Strength.
  - current control flow: old instance Removed-at-zero -> old ledger marked -> new current instance found by type only -> subtract old `grantedStrength` from new amount.
  - minimum fix: 记录并核对被归零实例的身份/世代和外部剩余来源; 只有证明当前实例承接同一 aggregate 时才恢复, 否则把旧形态账本标记为丢失而不触碰新实例.
  - missing runtime evidence: 未做"归零移除后立即重新施加 Strength"的 hook 重入复现; 当前 engine 的具体来源时序未由本审查假定.

- [P2] Demon 当前信号不能在所有路径上严格区分"显式 purge"和"归零后移除".`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:174-193` 的 `Removed` handler 只保存 `strength.Amount == 0`; `:214-225` 以该布尔值决定是否恢复外部剩余.显式 `Remove` 在非零 amount 时可以被区分, 但若其它路径先把 amount 写为零再移除, 当前代码没有 removal cause 或 transaction identity 可供判定.
  - trigger: 外部清理路径在触发 `Removed` 前已把 `StrengthPower` 变为零, 或通过非标准直接移除路径跳过了正常 amount transaction.
  - current control flow: `Removed` 只观察最终 amount -> zero 被解释为 external cancellation -> 可能新建负 Strength 或扣减后续实例.
  - minimum fix: 为显式 purge 设置独立 removal marker, 或把归零恢复绑定到同一 amount transaction/source token; 不要只依赖最终 amount.
  - missing runtime evidence: 未覆盖显式 purge,直接 `RemoveInternal` 和归零自动移除的逐路径故障注入.

- [P2] 群蛇 completion bridge 的去重控制在源码层是成对的.`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:87-147` 先从共享 root tracker 移除 exact `CardPlay`, 再 await 伤害; 重复 callback 在 `:102-113` 直接返回且不重掷 RNG/不重复伤害.`PreservePendingPlays` `:251-343` 通过 root tracker 合并, `AfterCombatEnd` `:182-194` 清空 pending 并只对 bridge 走移除路径.
  - trigger: 同一 CardPlay 重复回调, 旧 receiver 与 bridge 同时收到 completion, 或 nested play 交错完成.
  - current control flow: exact reference set 去重 -> root tracker 共享 pending -> bridge 在 tracker 归零后退出 -> duplicate callback 不再消费.
  - minimum fix: 当前静态证据下不提出修改; 需要 runtime 注入确认 callback 顺序和桥接期间的 owner 生命周期.
  - missing runtime evidence: 未运行重复 callback,nested play,bridge 被 hook 中途移除,退出能量和战斗结束的真实组合场景.

- [P2] 形态实例的可变战斗数据放在 `InitInternalData` 返回的新对象中, 没有看到 canonical 与 clone 共享 `HashSet` 或 token 容器.证据为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:31-58`, `VoidFormEffectPower.cs:21-44`, `SerpentFormPower.cs:26-85`, `WatcherFormStancePower.cs:22-50`; 权威引擎 `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:582-586` 在 clone 时重新调用 `InitInternalData`.`GrantExitEnergy` 是 scalar field, 由 `MemberwiseClone` 保留, 而桥接对象在应用前显式设为 false (`SerpentFormPower.cs:279-283`).
  - trigger: power canonical 被转为多个 mutable 实例, 或 carrier/effect 在切换期间被 clone.
  - current control flow: mutable clone -> engine 重新初始化 internal Data -> 每个实例持有独立 tracker/pending/账本; Serpent bridge 另设 exit-energy policy.
  - minimum fix: 当前静态证据下不提出修改; 仍需存档/重连与实际 clone 生命周期验收.
  - missing runtime evidence: 未验证战中存档,重载,多人 clone 和跨 combat 的 state identity.

## 进行中

- 源码审查已覆盖请求列出的 7 个文件及为确认异步顺序而读取的本地引擎反编译;未读取或审查 `FormNativeSmokeRunner.cs` 的新效果场景.
- 本轮未构建, 未测试, 未部署, 未启动游戏, 未写 Steam install, 未改 shared `mod_configs`, 未写 C:;唯一写入路径是本报告.
- 已运行请求文件指定的文本检查器: `G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs --help` 返回 `agent text accepted`.未发现请求文本的禁止字符.

## 未知

- [P1] `SpendResources` 到 `OnPlayWrapper` 是否由所有原生入口严格串行, 以及支付 Task 成功后是否存在 engine-level cancellation callback;当前源码和本次只读审查没有该保证.
  - trigger: 同一卡的重复,自动,nested 或网络重放入口.
  - current control flow: 当前补丁只以 card identity 和 bucket 顺序关联两个 async API.
  - minimum fix: 获取原生调度契约或增加可观察的 handoff/cancel 证据后再决定修复形状.
  - missing runtime evidence: 没有原生 scheduler trace, 没有支付失败/取消/重入实机证据.

- [P2] 群蛇在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:182-194` 的 combat-end 分支对 normal receiver 只清空 tracker, 不显式移除自身; 当前审查没有证明引擎一定会在 combat end 另外移除该 power.若 engine 不自动清理, 可能把 `SerpentFormPower` 带过 combat 边界, 但这不是本审查可直接归因的已复现 bug.
  - trigger: combat-end hook 到达时 normal Serpent receiver 仍在 `Owner.Powers`.
  - current control flow: `pending.plays.Clear()` -> normal receiver return; 只有 `completionBridge` 执行 `PowerCmd.Remove`.
  - minimum fix: 先确认 native combat-power lifecycle; 若无自动移除, 在 combat-end 显式移除 normal receiver并保持 `GrantExitEnergy` 不发放.
  - missing runtime evidence: 未运行 combat end, 下一场 combat 和跨房间 power 列表核验.

- [P2] `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:118-195` 的 effect attach/remove 都 await 了 `PowerCmd` 命令, 但代码本身没有声明或验证 Godot main-thread affinity;同样, `SerpentFormPower` 的 `HashSet` 和 `VoidFormPlayTransaction` 的锁只保证部分数据结构安全, 不证明游戏 hook 可以在任意线程并发调用.
  - trigger: hook continuation 在非主线程恢复, 或多个 combat task 并发访问同一 power.
  - current control flow: async hook -> awaited game command -> continuation 修改 internal Data/HashSet/token bucket.
  - minimum fix: 需要原生 scheduler/thread contract 或隔离运行 trace; 不应仅凭 `await` 宣称主线程正确.
  - missing runtime evidence: 未做线程 ID trace,并发 callback 故障注入,多人联机或重连验证.

- [P2] `Spire1PowersGatePatch.cs` 的反射目标解析包含返回类型,参数类型和参数名的精确合同, fallback 只在中央目标不完整时安装.源码能证明"失败时记录 NOT installed/NOT closed"的意图, 不能证明当前运行时目标解析,Harmony transpiler 安装和 `FormStanceMode.IsSelected` 例外已经实际命中.
  - trigger: engine 签名,参数名,patch order 或运行时类型发生漂移.
  - current control flow: static Resolve -> Prepare/Cleanup 记录安装状态 -> central gate operational 时不装 fallback -> fallback incomplete 时按三层安装.
  - minimum fix: 保留 fail-closed 日志并补实际安装证据; 不在本只读审查中改目标合同.
  - missing runtime evidence: 本轮禁止构建,测试和部署, 没有新的 Harmony patch report.

- [P2] 本报告没有把源码控制流外推为视觉,保存,多人,完整 native scheduler 或完整数值平衡结论.
  - trigger: 将当前静态审查或既有主菜单/三卡日志当作完整验收.
  - current control flow: 本报告只记录源码和本地引擎反编译证据.
  - minimum fix: 为每个边界建立独立授权,独立证据和独立报告.
  - missing runtime evidence: 可见 UI,存档重载,重连,多人同步,完整长战斗与性能证据均缺失.

