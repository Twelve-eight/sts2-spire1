你是监督审查员, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\unselected-card-id-r38\supervisor.md`.
只可写该报告. 不改产品代码, 不改测试代码, 不构建, 不运行游戏, 不部署, 不写共享配置, 不写 C:.

## 增量落盘

1. 在门禁文件出现后立即追加第一批审核结论.
2. 每完成一个检查面写一次盘.
3. 报告文件固定三段: `## 已确认`, `## 进行中`, `## 未知`.
4. 审核必须等待并读取 worker 报告和 gate-notice, 不得提前审代码.

## 任务

worker 完成后, 审核其对 `RuntimeSafetySmokeRunner.cs` 的最小 delta:
- 真实 CardModel 先进入 Hand, 且等待 CardPileCmd.Add 完成并验证 PileType.Hand.
- `PlayCardAction` 只在卡牌已被 NetCombatCardDb 通过 pile change 注册后构造.
- Shutdown 前两张真实卡和两份真实 action 都已准备, Shutdown 后不新建 action.
- 不伪造卡牌 ID, 不写生产 Forms, 不改 controller.
- 保留完整 runtime/action/history/damage/stance evidence, 不重现 r37 wrapper 覆盖问题.
- 检查代码风格,异常路径,重复调用和未选局范围; 记录精确文件与行号.

从本地请求文件读取任务和约束:
`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\unselected-card-id-r38\worker-request.md`.
worker 门禁文件将由主会话写入:
`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\unselected-card-id-r38\gate-notice.json`.
只有完成真实审核后, 报告最后才写 `SUPERVISION_PASS` 或 `SUPERVISION_NEEDS_REWORK`, 并列出未验证的构建/实机边界.
