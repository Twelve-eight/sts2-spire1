# 群蛇 P1 修复监督审查

日期: 2026-10-01
范围: 只读核对指定实现文件与实现者报告及必要的权威契约和引擎调用链. 本会话只写本报告, 不再委派, 不运行 build/lint/test/probe, 不部署或操作游戏和共享配置.

## 已确认

### 增量审查 1：维度一，Before 门槛与进入姿态不追溯

- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:52-60` 只在 `BeforeCardPlayed` 中记录，并以 `cardPlay.Player.Creature == Owner` 固定实际打牌者；不会按 `CardModel` 身份追溯，也不会把他人 CardPlay 收入集合。
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:54-58` 对 `completionBridge` 禁止新记录。旧群蛇移除后新进入的普通群蛇不会因已经开始的外层 CardPlay 重新记录；该外层事务只能由转移集合或桥接实例承接。这与契约 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:36` 的“Before 时已存在、进入牌不追溯”一致。
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:63-70` 先从本实例集合移除再等待伤害，单实例重复 `AfterCardPlayed` 不会再次结算。此项为源码审查结论，不是编译、探针或实机通过。

### 增量审查 2: 维度二, 正常移除链的 pending 转移

- 已完整重读实现者最终源码和最终报告. 实现者 `01a0f51e-b0ba-7502-8691-c7ed048a5b8f` 的 completed 状态沿用已经满足的监督门禁, 本轮不再等待; 代码和报告的完成标记确已落盘. 请求指定的模型与路由不等于本审查已实测的路由元数据.
- 正常 await 路径具备修复原 P1 的必要时序: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291-297` 先从 Powers 移除旧实例, 再 await `AfterRemoved`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:179-195` 等待精确实例清理; `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-WatcherCombatHelper-current.cs:537-547` 在旧姿态移除完成后才进入新姿态.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:149-174` 只在同一 `owner.Powers` 找承接实例, 优先普通群蛇, 其次现存完成桥接, 最后从 canonical 创建新 mutable 桥接. `:115-120` 等待转移完成后清除旧集合. 若桥接确已成功挂载, 卡牌 OnPlay 的 await 链返回后, `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:278-290` 的新 listener 快照能包含桥接, `:63-101` 消费一次记录并在最后一条 pending 完成时移除桥接.
- 因此原审查的正常 OnPlay 切姿态丢单路径在成功挂载、未再被移除的前提下得到源码层覆盖; 不能把这个条件性结论扩大为所有 hook、异常、调度和实机路径通过.

### [P2] 问题 1: 承接桥接被拒绝时静默丢单, 抛异常时跳过旧实例清理和退出路径

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:115-133`, `:170-187`; 权威 API 为 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:105-163`, `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:564-571`.
- 触发条件: 活跃战斗内旧群蛇已有 pending 且没有现存承接者, 新桥接 Apply 被数量 hook 修饰为零、目标暂不可接收, 或被 hook 再次移除; 另一个分支是 Apply 或其异步 hook 抛异常. 战斗已结束时停止伤害是正确门槛, 不计为丢单缺陷.
- 当前控制流: 新桥接先复制 pending -> await 普通 PowerCmd.Apply, `silent: true` 不绕过 hook -> 未挂载时 `:184-186` 清掉桥接集合 -> 调用方 `:120` 仍清掉旧集合, 卡牌随后即使正常完成也无记录可消费. 若 Apply 抛异常, `:120` 及 `:126-133` 均不会执行; 若异常发生在挂载之后, 还可能留下已经挂载的桥接. 源码确认此失败处理缺口, 未确认具体实机 hook 会触发.
- 最小修复建议: 将承接做成显式成功/失败的事务; 活跃战斗的必需桥接未挂载时不得静默清空唯一 pending, 应选择经契约批准的不中断完成机制或明确失败上报. 为 Apply 的挂载前/后异常补精确实例回滚和旧集合 finally 清理, 将退出支付的一次性处理与承接异常隔离, 不得简单 catch 后吞错或直接调用内部 Apply 绕过 hook.
- 尚缺证据: 主会话后续需构建并覆盖零数量、拒绝、挂载后移除及挂载前/后异常的隔离场景, 再核对真实 Harmony 和退出能量日志. 本轮未执行构建、测试、探针或实机.
### 增量审查 3: 维度三, 自动、重复、嵌套及异步完成

- `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1915-1926` 每次实际播放构造独立 CardPlay; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Cards\CardPlay.cs:11-23`, `:46-73` 明确区分实际打牌者与系列次数. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:28`, `:52-69` 不过滤 IsAutoPlay 或 PlayIndex, 按对象引用记录及领取, 因而自动打出和重复次数各有独立记录, 同一张 CardModel 不会把另一条实际播放覆盖掉.
- 正常嵌套 OnPlay 下, 外层和内层未完成记录可一并 UnionWith 到承接者, 内层先完成只移除自己的记录, 其余记录使桥接 `:98` 不会提前自移除. `:67` 在第一条 await 前领取, 单实例伤害等待期间的同对象重复 After 不会二次请求伤害. 本文件没有新增 Task.Run、静态 pending 或后台线程, 但这不是所有外部线程调用的线程安全保证.
- 跨实例转移不是共享领取事务: `:174` 复制, `:176-183` 等待, `:120` 才清除原集合. 严格 await 的原 OnPlay 切姿态链会在此窗口结束后才发外层 After; 未见这个正常链会同时发旧实例和桥接的同一回调, 不把双副本本身认定为已发生重复伤害.
## 进行中

- 已确认实现者报告与产品代码均已落盘；维度二、三已落盘, 继续维度四至六。
- 正在核对跨实例 pending 转移的完成窗口、异常与异步战斗边界、自动/重复/嵌套 CardPlay、RNG/伤害/退出能量，以及真实引擎 API 和 clone 语义。
- 本轮严格不运行 build/lint/test/probe，不部署、不启动游戏、不改探针或共享配置。

## 未知

### [INFO] 问题 2: listener 快照内转移及跨实例重复回调仍未形成恰好一次的完整证明

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:67-101`, `:115-120`, `:149-187`; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:278-293`; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatState.cs:411-417`.
- 触发条件与控制流 A: 若外层 AfterCardPlayed 已建立包含旧群蛇的 List 快照, 排在群蛇之前的 listener 再触发切姿态或嵌套牌切姿态, 移除逻辑会将外层 pending 转移并清空旧集合. 新桥接不在这轮早期快照, 旧实例到号时 Remove 失败; 下一轮晚期快照只调用 AfterCardPlayedLate, 本文件无晚期消费, 所以该条件下外层 pending 可能一直留到战斗清理而不结算. 引擎已有在 After 内 autoplay 的合法入口 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\ImitationLearningPower.cs:82-92`, 但它只复制 Power 牌, 本轮未找到并核实具体切姿态牌与 listener 顺序组成这条完整实机路径. 因此不把此项升级为已复现 P1.
- 触发条件与控制流 B: 若外部重入在 Apply 的等待窗口给已移除旧实例及新桥接各发同一 CardPlay 的 After, 两套 HashSet 各自 Remove 可成功; 普通多实例也没有跨实例领取标记. 标准顺序链不满足该额外调度条件, 本轮未证明正常 Watcher 会产生多个普通群蛇, 不以 Instanced 的理论多实例能力单独报 bug.
- 最小修复建议: 先集中验证完整 listener 快照与嵌套移除顺序. 若条件 A 可达, 为已开始事务增加完成尾段的可靠领取入口; 若条件 B 可达, 以同一战斗、同一 Owner 的共享一次性领取令牌作 handoff, 不增无界静态集合, 不靠保留两份待结算记录来兜底.
- 尚缺证据: 对真实 Hook 的早期/晚期快照、桥接 Apply 等待窗口、旧实例与承接者重复 After、多个普通实例的可达性做隔离与实机调度验证. `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\SerpentScenarios.cs:38-87` 只直接验证同实例去重和伤害等待, 没有上述真实快照转移情景; 本轮未执行任何场景.


- 修复正确性尚未审查. 本报告中的等待状态不是修复通过或失败的结论.
- 本轮没有构建, 探针或实机验证证据.

等待记录时间: 2026-10-01 09:58:43 +08:00
