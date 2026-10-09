# 姿态形态长回合边界烟测实现任务 - 2026-10-03

## 目标

在现有真实游戏 smoke runner 的基础上补一个可选的长回合边界场景, 用于关闭当前未完成的下一回合刷新/下一回合退出证据缺口. 这不是产品玩法改动, 只扩展测试入口和证据.

## 唯一可写文件

- 产品测试入口: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`
- 增量报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\turn-boundary-smoke-worker-20261003.md`

不得修改其它产品代码, 项目文件, 发布包, 游戏目录, Steam 安装, 共享 mod_configs, C: 或其它代理.

## 现有入口与约束

- 先读 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md` 相关契约.
- 当前入口是 `--form-native-smoke`, 可扩展为 `--form-native-smoke-turns` 或 `--form-native-smoke=turns`, 不得破坏 calm/wrath/divinity 三个现有场景.
- 真实链路必须复用当前 runner 的 `StartNewSingleplayerRun`,`FormStanceModifier`,`EnterRoomDebug`,真实 `PlayCardAction`,真实 Watcher 卡和真实回合命令. 不得用直接 `PowerCmd.Apply` 注入形态或伪造 hook.
- worker 只写代码和增量报告, 不构建,不部署,不启动游戏,不运行测试. 主会话集中构建和运行.
- 所有新的第一批结论,每个检查面完成后立即追加到报告. 报告分为已确认 / 进行中 / 未知. 如果无法实现某个断言, 必须报告原因, 不留空 stub.

## 必须覆盖的证据

至少实现一个真实 `turns` 场景, 并在 JSON 中明确记录每项的 before/after 与通过条件:

1. Calm: 真实 `WATCHER_VIGILANCE` 进入 Calm 后, 在首回合用真实 `WATCHER_STRIKE_P` 消耗一次免费额度; 通过真实结束玩家回合并回到下一次自己的 Play 阶段, 再用第二张真实 Strike 验证免费额度刷新. 记录两次能量快照,两次出牌状态和本场未观察异常.
2. Divinity: 真实 `WATCHER_BLASPHEMY` 进入 Divinity 后, 通过真实结束当前玩家回合并进入下一次自己的回合边界, 验证原生 stance,carrier 和两个 effect 已按契约清理, 且没有在入场同一回合立即清掉. 如果当前引擎无法安全驱动该边界, 记录具体 API/状态阻塞而不是假通过.
3. Wrath: 若能在同一真实 runner 场景安全驱动下一回合, 记录 Demon strength 的 round 1/round 2 增量; 不能安全驱动时可列为未知, 但不能删掉 Calm/Divinity 两项.

## 线程与证据

- 保持主线程边界,总 deadline,detached drain,cleanup gate 和 unobserved-fault gate.
- 新场景失败必须写 failure evidence, 不能继续伪造 passed.
- 保持现有脚本的 `--form-native-smoke` 三场景兼容; parser 只增加显式 turns 入口.
- 先静态阅读并在报告落盘, 再编辑. 完成实现后再次追加自洽报告, 列出改动行范围和未覆盖项.

## 模型与路由

本任务只使用当前用户指定的 `global:deepseek-v4.1-flash`, Codex 原生子代理设施, reasoning `xhigh`. 不启动 omp 或 codex exec, 不再委派子代理.
