# 姿态形态长回合边界烟测监督审查任务 - 2026-10-03

## 监督门禁

实现 worker session id: `01a0ff7a-94be-7790-93c1-713150880ea3`.

你必须先使用原生 `wait_agent` 等待该 worker 完成,并先读取其增量报告与实际 diff,然后才开始审查. 不得把并行读取或独立静态检查冒充监督.

## 审查范围

- 请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\turn-boundary-smoke-worker-request-20261003.md`
- 实现报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\turn-boundary-smoke-worker-20261003.md`
- 唯一产品文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`
- 你的唯一写入文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\turn-boundary-smoke-review-20261003.md`

只读审查, 不构建, 不测试, 不部署, 不启动游戏, 不修改产品代码, 不再委派.

## 必查项

1. parser 新入口是否保持现有 calm/wrath/divinity 行为和错误入口边界.
2. 新 turns 场景是否只使用真实 StartNewSingleplayerRun, FormStanceModifier, EnterRoomDebug, PlayCardAction 和真实回合命令, 没有直接 PowerCmd 注入或伪造 hook.
3. Calm 的下一回合免费额度, Divinity 的下一回合退出, Wrath 的 round 2 证据是否真的有明确 before/after 和 failure gate; 无法覆盖的项必须标 Unknown, 不得伪造 PASS.
4. 主线程 gate, 总 deadline, detached drain, cleanup gate, unobserved-fault gate 是否保持.
5. JSON 证据是否在失败时 honest, 不会以旧文件或未完成 action 声称通过.
6. 是否超出唯一写集或引入非 ASCII 不准脚本文本.

## 报告协议

先写进度, 完成一个检查面就追加一次. 报告必须分为已确认, 进行中, 未知, 并给出文件和行号. 最终结论只能是 PASS, REWORK 或 BLOCKED, 且注明仅限静态监督.

## 模型与路由

只使用当前用户指定的 `global:deepseek-v4.1-flash`, Codex 原生子代理设施, reasoning `xhigh`. 不启动 omp 或 codex exec.
