# Serpent P1 worker report

日期: 2026-10-01
范围: 仅修改 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs` 与本报告.

## 已确认

- 生产代码 `SerpentFormPower.BeforeCardPlayed` 将当前 `CardPlay` 写入实例本地 `Data.startedPlays`.
- `AfterCardPlayed` 只在同一实例的 `startedPlays.Remove(cardPlay)` 成功时结算 3 点伤害.
- `AfterRemoved` 当前无条件清空 `startedPlays`.
- 引擎时序源码显示 `BeforeCardPlayed` 之后等待卡牌 `OnPlay`, 再重新枚举当前 listeners 并调用 `AfterCardPlayed`.
- 因此卡牌 `OnPlay` 内切换 Watcher 姿态时, 旧载体移除会清掉已开始但未完成的 `CardPlay`; 该次播放可能不再结算. 这是与契约 `DEVELOP-form-playable-20260928.md:36` 一致的已确认源码缺陷, 不是实机复现声明.
- 既有 `SerpentScenarios.cs` 已覆盖进入牌不追溯, 自动/重复播放, 重复回调去重, 嵌套/异步完成语义的相关基线; 本任务不修改探针.

## 进行中

- 本 worker 的单文件实现和只读自检均已完成,无待实现项.
- 按请求未运行 build/lint/test/probe/deploy/game;运行时验证保留在下方未知项.

## 未知

- 尚未在真实卡牌与真实 Harmony 运行中观察该姿态切换路径.
- 尚未完成修复后的编译与探针验证,不能宣称行为已通过.

[增量结论 2026-10-01]

- 单文件内可完成修复: 在旧群蛇实例移除时, 将未完成的 `CardPlay` 转移给同一玩家仍在场的活动群蛇实例; 若没有活动群蛇实例, 附加一个只处理已开始播放的隐藏完成桥接实例.
- 桥接实例不在 `BeforeCardPlayed` 记录新的 CardPlay, 只在 `AfterCardPlayed` 消费转移集合; 结算后立即移除, 战斗结束钩子清空并移除残留桥接.
- 以实际 `CardPlay` 对象引用去重, 所有者仍固定为旧群蛇的同一 `Creature`; 不使用全局跨战斗集合, 因此不会把记录匹配到不相关 CardPlay, 玩家或下一场战斗.
- 该方案不要求修改 Watcher 载体或引擎 API, 能覆盖旧载体移除后没有新群蛇监听器的姿态切换路径.

[只读自检增量 2026-10-01 09:58 +08:00]

- 已确认真实 Remove 顺序: `PowerCmd.cs:291-297` 先 `RemoveInternal`, 再等待, 最后 await `AfterRemoved`; 旧实例的 Owner 不被移除操作清空. 正常 await 的 Watcher 切换路径会等待 pending 桥接附加后再完成 OnPlay.
- 已确认真实 Apply 顺序: `PowerCmd.cs:105-160` 会经过异步 power hooks 和数量修饰, 然后 `ApplyInternal`; `Instanced` 不走同类型合并. `silent: true` 不绕过 hooks, 不是保证成功附加.
- 已确认失败边界: 如果 Apply 返回但桥接未在 `owner.Powers` 中, 当前代码清空桥接记录, 调用方也清空旧记录, 因而不能保证该 pending 后续仍结算. Apply 抛异常也没有 catch/finally 恢复, 会跳过本次旧实例退出能量路径. 这些是已确认控制流边界, 不是已实测的失败场景.
- 已确认正常战斗结束清理: `CombatManager.cs:1307-1327` 先关闭 IsInProgress, 调用 AfterCombatEnd, 然后 Player.AfterCombatEnd 清掉全部 powers. 群蛇新 AfterCombatEnd 清空记录并移除桥接, 桥接 AfterRemoved 不重挂且不支付能量.
- 未知异步边界: Apply 前后的 await 期间若战斗结束或已结束的战斗状态被替换, 当前代码未在附加后重新检查战斗身份/结束状态. 尚无该交错的实机证据, 不能宣称所有迟到附加都已清理.
- 已确认转移窗口: 新桥接先复制记录, await Apply 返回后旧实例才清空集合; 在此窗口存在两个集合副本. 单实例重复 After 回调仍在伤害前 Remove 去重, 但跨旧实例/桥接的重复回调没有共享领取标记. 实际引擎能否在此窗口交错该同一 CardPlay 回调尚未知.
- 已确认现有隔离探针 API 缺口: `ContractStubs.cs:219-322` 的 PowerModel 没有 AfterCombatEnd, 当前探针源文件也没有 CombatRoom/Rooms 定义; csproj 直接编译新增 override 的生产源码且没有真实引擎引用. 需要主会话另行补齐探针桩再集中验证. 本任务不改探针, 未运行编译.

[最终收尾 2026-10-01]

- 最终未验证边界: `PowerCmd.Apply` 可能因 `IsEnding`, `CanReceivePowers`, combat state 缺失,或异步 hook 后目标不再可接收而不挂载 bridge;当前代码会检测 `owner.Powers.Contains(bridge)` 并清掉 bridge 本地 pending.若 Apply 或其 hook 抛异常,本文件没有 catch/finally 恢复;旧实例的后续清理和退出能量路径可能不会继续.以上是源码控制流边界,未做运行时失败注入.
- 最终未验证边界: `AfterRemoved` 只在检查时确认 `IsInProgress && !IsOverOrEnding`,Apply 等待期间未保存或复核 combat identity.战斗结束与 Apply 异步交错时,迟到挂载或清理顺序尚未由实机证据确认;正常 `AfterCombatEnd` 会清空 pending,移除已挂载 bridge,且 bridge 的 `AfterRemoved` 不重挂.
- 最终未验证边界: 单实例以 `CardPlay` 引用移除后去重;极端的旧实例,替换实例与 bridge 跨实例交错回调是否可能重复结算,尚无真实引擎证据.
- 未运行 build,lint,test,probe,deploy 或游戏;未宣称编译,探针或实机通过.

实际修改文件:
1. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs`
2. `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\serpent-p1-worker-20261001.md`

状态: 已完成;代码已落盘;报告已更新;未验证项已明确记录.