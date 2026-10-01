
---

## 监督审查开始 - 2026-10-02

- 范围: 最新真实单战斗 headless 载体的只读静态监督审查.
- 审查对象: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs`, `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`, 以及 r12 worker/review 报告.
- 唯一路由声明: `6.1sol` via `agentrouter`. 本轮不切换模型或 provider.
- 门禁: 实现代理已 completed; 本轮不修改产品代码, 不构建, 不测试, 不部署, 不启动游戏, 不修改共享配置或 Steam 安装.
- 证据规则: 仅记录源码与报告的静态证据; 不把静态推理写成真实运行证据. 最终结论必须明确为 `PASS` 或 `REWORK`.

## 已确认

- 审查已开始; 详细检查尚未完成.

## 进行中

- 待读取最新代码与 r12 证据, 按六个审查维度逐面核对.

## 未知

- 普通启动 gate, 原生战斗控制流, 逐帧等待边界, timeout/cancel/exception 不可逆状态, Godot 主线程门禁, 失败记录与最终退出状态均尚未在本轮确认.

---

### 检查面 1: 普通启动 gate 与禁用旁路

#### 已确认

- 静态结论: 该检查面 `PASS`.
- `FormNativeSmokePatch.cs:7-14` 只在 `NGame._Ready` postfix 中调用 `FormNativeSmokeRunner.TryStart`; `FormNativeSmokeRunner.cs:47-57` 先解析参数并检查 `SmokeRequest.Requested`, 普通启动在 `:50-52` 直接返回, 不创建 runner task, 不订阅 `TaskHelper.UnobservedFault`, 不写 smoke JSON.
- `FormNativeSmokeRunner.cs:62-121` 只接受 `--form-native-smoke`、三个场景后缀以及 `=`/`:` 场景形式; `_started` 的 `Interlocked.Exchange` 在 `:54-56` 防止重复入口.
- 当前两个产品文件的静态扫描未发现 `autoslay`, `TestMode`, `NGame.Quit`, `PowerCmd.Apply`, `Task.Run` 或条件等待的 `Task.FromResult(condition)`. Smoke runner 使用的 `Task.FromResult(operation())` 仅位于 `:849`, 是主线程直接执行结果的任务封装, 不是条件等待.
- 该载体没有直接应用 form power 或伪造 Hook; 形态身份由后续真实 `FormStanceModifier` run modifier 路径承载, 详见检查面 2.

#### 进行中

- 无源码检查面遗留项. 真实普通启动与带参数启动仍未执行.

#### 未知

- 未构建、未启动游戏, 因而未知目标二进制是否实际扫描并执行该 Harmony patch, 以及普通启动的窗口和进程生命周期行为. 本静态 `PASS` 不等于实机通过.

---

### 检查面 2: 真实 ModelDb、run、房间、卡牌与 action 链路

#### 已确认

- 静态结论: 该检查面 `PASS`.
- 角色与卡牌均来自真实运行时 `ModelDb`: `FormNativeSmokeRunner.cs:723-750` 从 `ModelDb.AllCharacters` 精确查找 `WatcherMod.Watcher`, 从 `ModelDb.AllCards` 精确查找 `WATCHER_VIGILANCE`, `WATCHER_ERUPTION_P`, `WATCHER_BLASPHEMY`; 找不到即抛错, 没有复制 Watcher 类型或构造伪造卡模型.
- Act 和 encounter 走真实模型: `:222-227` 调用 `ActModel.GetDefaultList` 并从默认 Act 的 `AllRegularEncounters` 或 `AllEliteEncounters` 选择 encounter; `:345-351` 使用 `RunManager.EnterRoomDebug(..., encounter.ToMutable(), showTransition:false)`.
- run 入口使用真实单人 API 和真实 modifier: `:313-322` 通过 `NGame.Instance.StartNewSingleplayerRun`, 传入真实 Watcher `CharacterModel`, `shouldSave:false`, `GameMode.Custom`, 固定 seed 以及 `ModelDb.Modifier<FormStanceModifier>().ToMutable()`.
- 战斗卡链路没有测试替身: `:379-397` 使用 `player.Creature.CombatState.CreateCard` 创建战斗卡并通过 `CardPileCmd.Add(..., PileType.Hand, skipVisuals:true)` 注入手牌; `:400-414` 创建真实 `PlayCardAction`, 通过 `runManager.ActionQueueSynchronizer.RequestEnqueue` 排队; `:417-420` 等待该 action 的真实 `CompletionTask`.
- 研究源码与 worker 报告一致: `NGame.StartNewSingleplayerRun`, `RunManager.EnterRoomDebug`, `CombatState.CreateCard`, `CardPileCmd.Add`, `PlayCardAction`, `ActionQueueSynchronizer.RequestEnqueue` 和 `GameAction.CompletionTask` 均有对应发行版源码证据; 本轮未把这些源码证据升级为运行证据.

#### 进行中

- 无静态 API 替代项遗留. 仍需后续检查 action 失败、超时和线程收束的控制流.

#### 未知

- 未构建, 未启动游戏, 未进入真实战斗; 因此未知当前目标二进制的 Watcher bridge 绑定、真实 modifier 持久化、encounter 创建、三张卡的 `CreateCard`/`Add`/`PlayCardAction` 执行是否全部成功.
- `FormStanceWatcherBridge.TryBind()` 在 `:192-200` 的真实进程结果以及每个场景实际 native stance/form carrier/effect 状态仍未知.

---
