你是实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\unselected-card-id-r38\worker.md`.
代码写集只允许上述 RuntimeSafetySmokeRunner.cs. 不改生产 Forms, 不改控制器, 不构建, 不 lint, 不运行游戏, 不部署, 不写共享配置, 不写 C:.

## 增量落盘

1. 拿到第一条可用结论后立即追加到报告文件, 然后才做下一步.
2. 每完成一个检查面写一次盘.
3. 报告文件固定三段: `## 已确认`, `## 进行中`, `## 未知`.
4. 最终回复只需摘要并给出报告绝对路径.

## 任务

修复普通未选形态局的测试载体准备顺序. 当前证据在 `G:\omp works\.tmp\forms-independent-20261005\native-r35-unselected-r37\r2-runtime-unselected` 显示, `RuntimeSafetyPrepareCardAsync` 在卡牌加入 Hand 前构造 `PlayCardAction`, 因而 `NetCombatCardDb.GetCardId` 找不到该 mutable card. 权威引擎反编译与已有成功载体示例在:
- `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs` 的 `NetCombatCardDb.StartCombat`, `OnPileContentsChanged`, `IdCardIfNecessary`.
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs` 约 661-688 行.
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs` 约 751-789 行.

只做最小,真实的修复: 先 `CombatState.CreateCard`, 再 `CardPileCmd.Add(card, PileType.Hand, skipVisuals: true)` 并等待完成和验证 hand, 最后才构造 `PlayCardAction`. 保持 Shutdown 前准备全部真实 CardModel 和真实 action 的契约. 不伪造卡牌 ID, 不直接反射写 NetCombatCardDb, 不在 Shutdown 后重建 action. 只改这个 runner 内为实现顺序所需的最小类型/方法调整, 保留现有 action/history/target/damage 证据形状. 若发现需要修改生产代码或控制器, 只在报告标记为阻塞, 不越界修改.

完成后在报告中写入修改前后 SHA256, 精确行号和 delta, 并明确未构建/未运行.
