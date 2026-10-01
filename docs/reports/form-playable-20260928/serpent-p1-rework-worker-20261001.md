# Serpent P1 rework worker report

日期: 2026-10-01
范围: 仅修改 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs` 与本报告.
状态: completed. 按请求不运行 build/lint/test/probe/deploy/game.

## 已确认

- 已读取返工请求,实现者报告 `serpent-p1-worker-20261001.md` 与监督报告 `serpent-p1-review-20261001.md`.
- 监督报告确认正常路径的 pending 转移可覆盖旧 P1,但现实现对 `PowerCmd.Apply` 的拒绝或异常不是事务性的: 未挂载时会清掉唯一 pending,抛异常时可能跳过旧实例清理和退出能量.
- 监督报告还记录了旧 listener 快照与转移 await 窗口的跨实例双副本风险;该条件尚未被实机复现,本轮只按最小证据处理.
- 真实 `PowerCmd.Apply` 会经过异步 hook 后才可能 `ApplyInternal`; `Instanced` 不做同类型合并. 因此返工必须区分正常挂载,正常战斗中的拒绝,挂载后移除,异常和 combat end.

## 进行中

- 本 worker 的返工代码已完成,无待实现项.
- 按请求未运行 build/lint/test/probe/deploy/game;运行时验证保留在下方未知项.

[实现增量 2026-10-01]

- 已将实例本地 `HashSet<CardPlay>` 改为单文件内的可合并 `PendingTracker`. 合并使用 parent/root,旧 listener,普通新群蛇与 bridge 即使在 handoff await 窗口仍解析到同一领取集合,避免旧实现的双副本各自 Remove.
- 普通实例移除时不再无条件清空 pending. 成功转移后保留共享 tracker;正常战斗中 Apply 异常或活动战斗无接收者时保留 tracker并抛出明确异常,不把失败静默当成完成. `FinishRemoval` 位于 finally,因此不会因 handoff 异常跳过一次性退出能量处理.
- Apply 正常返回但 bridge 未挂载时,先复查同一 tracker 的 receiver,再复查新出现的普通群蛇或 bridge. 活跃战斗仍无 receiver 时抛出 `InvalidOperationException`;若已进入 combat end,清空 combat-local tracker 并移除同 tracker 的 bridge,不重新挂载.
- 已处理 bridge 挂载后被移除: bridge 的 `AfterRemoved` 在活跃战斗内尝试把同一 tracker 转给现有 receiver或新 bridge; `recoveryInProgress` 防止 Apply 内移除造成无限递归. combat end 或无 pending 时只清理不重挂.
- 保持 `BeforeCardPlayed` 门槛,bridge 不记录新 Before,伤害前按 CardPlay 引用 Remove 去重,目标与 RNG 逻辑不变,bridge 不支付退出能量.

## 未知

- 未运行 build/lint/test/probe/deploy/game;未宣称编译,探针或实机通过.
- 活跃战斗中若所有合法 receiver 都被外部 hook 拒绝,实现会显式抛错而不是静默丢单;单文件且不绕过 `PowerCmd` hook 时,该失败后的实际游戏恢复行为仍未知.
- Apply 或其 hook 在挂载后抛异常时,代码保留已挂载 bridge并重新抛出;若外部调用链随后中止 CardPlay,真实引擎是否仍会给该 CardPlay 一个可消费的 After 回调尚未验证.
- 当前 tracker 仍是对象级 parent/root 结构,没有引入 combat identity 字段;在真实战斗切换与异步 hook 同时发生时的所有迟到清理顺序尚未由实机证据确认.
- 现有 probe 仍未覆盖真实 listener 快照内转姿态,Apply 拒绝,Apply 异常,挂载后移除和 combat end 交错;本任务未修改探针.

实际修改文件:
1. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs`
2. `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\serpent-p1-rework-worker-20261001.md`

状态: completed. 代码已落盘,报告已更新,未验证边界已明确记录.