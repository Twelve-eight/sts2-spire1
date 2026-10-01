# Recent code audit r4 - 2026-10-01

- 审查类型: 独立只读源码审查。
- 审查范围: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms` 与 `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe`；只读核对 `research\engine-dllsrc` 与 `docs\DEVELOP-form-playable-20260928.md`。
- 禁止项: 未改产品代码；未构建、测试、部署或启动游戏；未操作窗口、键鼠或共享配置；未写入 `C:`。
- 模型/路由: 按请求指定 `gpt-6-astra-ar`，`gateway -> agentrouter -> gpt-6-astra`；本报告不把请求文字当作路由实测证据。
- 证据边界: 本报告只把源码控制流和只读静态交叉核对写为证据；探针、已有 Release 产物、真实 Harmony 绑定、实机调度和游戏行为均未在本轮执行。

## 已确认

### [INFO] 正常手动打牌路径严格在支付完成后进入 OnPlayWrapper

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs:92-103`：手动路径先 `await _card.SpendResources()`，再构造 `ResourceInfo`，最后 `await _card.OnPlayWrapper(..., isAutoPlay: false, resources)`。
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1807-1819`：`SpendResources` 内部依次 `await SpendEnergy`、`await SpendStars`，两项完成后才返回资源结果；能量/星星 hook 也在各自 await 链内。
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1858-1867`：`OnPlayWrapper` 的手动/自动分支由 `isAutoPlay` 明确区分。
- 触发条件: 普通玩家手动牌经 `PlayCardAction` 执行。
- 当前控制流: 校验可打 -> await 完整 `SpendResources` -> 生成支付信息 -> await 手动 `OnPlayWrapper`。
- 其它 `OnPlayWrapper` 真实调用者交叉核对:
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\CardCmd.cs:51-131`：`AutoPlay` 不调用 `SpendResources`，直接生成零实际支付的 `ResourceInfo` 后 `await OnPlayWrapper(..., isAutoPlay: true, ...)`；这是另一条合法自动打牌路径，不是手动支付乱序。
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Relics\WhisperingEarring.cs:79-80`：先 await `SpendResources`，再调用 `CardCmd.AutoPlay`；最终仍走自动分支，Void 交易应取消临时 token 而不消费手动额度。
  - 全量只读搜索只找到上述 `PlayCardAction`、`CardCmd.AutoPlay` 和文档/探针桩的调用，未发现产品代码中另一个手动绕过支付完成的 `OnPlayWrapper` 调用者。
- 契约或权威依据: 引擎上述源码；设计契约 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:34-35` 要求 Void 只给下一张手动牌、自动打出不消费。
- 可复核命令（只读）: `rg -n --glob '*.cs' 'OnPlayWrapper\s*\(' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe'`；`Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs' | Select-Object -Skip 91 -First 13`。
- 最小修复范围: 无；这是正常引擎顺序的确认，不是缺陷。
- 尚缺实机证据: 未运行真实游戏、未执行 Harmony 绑定，也未用真实 hook 造成支付期交错；因此只确认源码调度，不确认所有外部扩展运行时行为。

### [INFO] Void 事务的异步观察器确实等待 Task 完成才标记支付完成

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:68-86`：`ObserveSpendCompletionAsync` 对原支付 Task 执行 `await result`，成功后才设置 `SpendCompleted = true`；异常/取消进入 `Cancel`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:240-263`：Prefix 建 token，Postfix 用包装 Task 替换返回值，Finalizer 只处理同步抛异常。
- 当前静态结论: 代码具备“不要把 Harmony Postfix 返回 Task 当成支付已完成”的观察器；但 `BeginPlay` 目前没有读取 `SpendCompleted`，该缺口是否可由真实引擎调度触发须单独判断，不能直接等同于已确认 P1。
- 可复核命令（只读）: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs' | Select-Object -Skip 67 -First 55`。

### [P1] 群蛇在牌内切换姿态时丢失已经开始的 CardPlay

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:49-54`：当前群蛇在 `BeforeCardPlayed` 将自己的 `CardPlay` 放入 `startedPlays`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:59-65`：完成回调只有从当前实例的 `startedPlays` 成功移除该 `CardPlay` 才造成伤害。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:90-94`：群蛇被移除时无条件清空 `startedPlays`。
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1926-1934`、`:1959-1965`：引擎先调用 `BeforeCardPlayed`，执行并等待 `OnPlay` 及附加效果，之后才调用 `AfterCardPlayed`。
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:278-293`：`AfterCardPlayed` 开始时重新遍历当前 hook listeners，而不是复用 `BeforeCardPlayed` 的 listener 快照。
  - `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatState.cs:411-418`：每次 `IterateHookListeners()` 都从当前各 Creature 的 `Powers` 重新构造列表。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-WatcherCombatHelper-current.cs:537-547`、`:573-579`：Watcher 切换姿态先 `RemoveAllStances`，再应用目标姿态；移除 Calm/旧载体会触发自定义载体清理。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:164-176`：载体移除会清理其两个自定义效果。
- 触发条件: 玩家当前处于包含群蛇效果的自定义平静形态，随后打出一张在自身 `OnPlay` 中切换到另一 Watcher 姿态的正常卡；该卡已经经过群蛇的 `BeforeCardPlayed`，但在 `OnPlay` 完成前旧载体被移除。
- 契约或权威依据: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:36` 明确要求每次实际完成的 `CardPlay` 都触发群蛇，且效果在该次 `BeforeCardPlayed` 时已经存在；`:27` 要求切换先完整退出旧形态再进入新形态。上述引擎源码是 hook 时序和 listener 重建的权威依据。
- 当前控制流: `BeforeCardPlayed` 记录到旧 `SerpentFormPower.Data.startedPlays` -> 卡牌 `OnPlay` 调用 Watcher `ChangeStance` -> `RemoveAllStances` 移除旧载体并触发 `SerpentFormPower.AfterRemoved` 清空集合 -> 卡牌完成后引擎重新枚举 listeners，旧群蛇实例不再收到 `AfterCardPlayed`；即使收到同一实例的延迟回调，集合也已没有该 `CardPlay`，因此不会造成 3 点伤害。
- 可复核命令（只读）: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs'`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs' | Select-Object -Skip 1925 -First 42`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs' | Select-Object -Skip 277 -First 17`; `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-WatcherCombatHelper-current.cs' | Select-Object -Skip 536 -First 44`; `rg -n 'startedPlays|ChangeStance|RemoveAllStances|AfterRemoved|AfterCardPlayed' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-WatcherCombatHelper-current.cs'`。
- 最小修复范围: 不在 `AfterRemoved` 无条件清空已开始但尚未完成的 `CardPlay`，或把已开始播放的事务移到不会随姿态载体销毁的战斗级状态，并在 `AfterCardPlayed` 依据开始时的效果身份完成结算；同时保留重复回调去重和正常退出清理语义。不得以延迟清理把已完成卡牌的结算泄漏到下一回合。
- 尚缺实机证据: 本轮未启动游戏、未构建或绑定真实 Harmony；尚未在真实卡牌中确认存在“`OnPlay` 内切换 Watcher 姿态”的可用入口，也未观察伤害日志。因此这是由引擎源码和生产代码闭环证明的可达源码缺陷，不是实机复现声明。
## 进行中

- 正在继续只读核对 Void 的 reservation/block 清理、Demon 的异步力量事务、其它四个效果、Watcher 互斥/入口/回合边界以及 probe 覆盖边界。
- 尚未执行构建、测试、探针、部署或游戏；不把现有二进制/历史报告当作本轮行为通过。

## 未知

### [未知/不可操作风险] VoidForm 的“支付未完成即同卡手动进入”目前没有真实引擎路径证据

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:97-120`：`BeginPlay` 领取 token 后只检查 `Reserved`、`PowerAtStart` 和 `IsReservationFor(card)`，没有检查 `SpendCompleted`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:190-218`（路径中的重复片段仅用于说明，实际文件为上一行路径）：`CompletePlay`/`AbortForPatch` 在有 `state.Spend` 时不会进入 `BlockedCurrentPower` 清理分支。
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs:136-156`：探针只合成“支付前无 power、支付期间获得 power、随后 wrapper”的场景，且没有真实 `PlayCardAction` 调度。
- 触发条件: 必须存在同一张卡在真实 `SpendResources` Task 尚未完成期间又进入手动 `OnPlayWrapper` 的重入/交错；或必须存在支付前无 Void、支付期间获得 Void 后仍沿用 token 的真实路径。普通 `PlayCardAction` 源码明确 await 支付后才调用 wrapper，因此当前仅凭这段代码不能证明该触发条件可达。
- 契约或权威依据: `PlayCardAction.cs:92-103` 的严格 await 顺序；`CardCmd.AutoPlay.cs:122-130` 的自动打牌分支；设计契约 `DEVELOP-form-playable-20260928.md:34-35`。
- 当前控制流: 静态上确有门槛未使用和 token 有 reservation 即放行；但当前研究引擎调用图未找到产品级手动调用者绕过 `PlayCardAction`。因此降级为未知/不可操作风险，不把纯理论同卡支付期重入写成已确认 P1。
- 可复核命令（只读）: `rg -n --glob '*.cs' 'OnPlayWrapper\s*\(' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe'`；`Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs' | Select-Object -Skip 96 -First 42`。
- 最小修复范围: 若后续真实调度证明可达，再在 `BeginPlay` 或 token 领取处增加成功支付门槛，并定义支付未完成/支付失败时的回滚语义；在没有可达路径和回归证据前不建议把它作为可操作 P1 修复。
- 尚缺实机证据: 未证实支付 hook 会调用同卡手动 wrapper；未证实失败 Task 与已完成播放能交错；未证实真实游戏中 `blockedCard` 分支会被进入或残留。
