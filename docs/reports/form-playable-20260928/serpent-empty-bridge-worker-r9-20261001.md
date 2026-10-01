# serpent-empty-bridge-worker-r9-20261001

## 已确认
- 优先级: P1
- 绝对路径与准确行号: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:101-106。
- 触发条件: AfterCardPlayed 在共享 tracker 已消费后再次收到 source/bridge 重复回调。
- 权威契约: 请求文件要求重复回调允许空 completion bridge 清理, 但不得重复伤害、RNG 或退出能量。
- 当前控制流: pending.plays.Remove(cardPlay) 返回 false 后直接 return, 因而跳过第 132-139 行 finally 清理逻辑;这与 central-probe-r8 的失败描述一致。
- 可复现命令: 中央隔离探针已提供证据, G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-probe-r8-20261001-01\serpent-before.log;本轮按要求不重新运行。
- 最小修复范围: 仅调整该早退分支,在 tracker 已消费且当前实例为 completion bridge、tracker 为空且仍挂载时执行一次 PowerCmd.Remove(this);不触发伤害或 RNG。
- 尚缺的实机证据: 中央会话构建与运行验证。

## 进行中
- 正在修改唯一生产文件并随后追加变更行号。

## 未知
- 未覆盖构建、测试、部署与游戏实机行为。
- 已完成检查面: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:103-113。
- 实际修改: 在 tracker 移除失败分支中,仅当 completion bridge 仍挂载且共享 tracker 为空时移除当前 bridge;不执行伤害、RNG 或退出能量。
- 控制流保持: 消费仍先于 await;正常 listener 路径与原 finally 语义未改;不修改 PendingTracker, scheduler 或外部 hook 排序。
- 尚缺实机证据: 未构建、未测试、未部署、未启动游戏,由主会话集中验证。

## 进行中
- 无。

## 未知
- 中央构建与隔离探针回归结果尚未知。

CODE_COMPLETE
- 实际修改文件: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs
- 报告文件: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\serpent-empty-bridge-worker-r9-20261001.md
