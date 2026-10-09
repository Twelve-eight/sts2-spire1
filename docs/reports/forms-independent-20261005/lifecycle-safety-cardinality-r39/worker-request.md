你是实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\lifecycle-safety-cardinality-r39\worker.md`.
代码写集只允许上述 LifecycleSmokeRunner.cs. 不改生产 Forms, 不改中央 PowerShell 控制器, 不构建, 不 lint, 不运行游戏, 不部署, 不写共享配置, 不写 C:.

## 增量落盘

1. 拿到第一条可用结论后立即追加到报告文件, 然后才做下一步.
2. 每完成一个检查面写一次盘.
3. 报告文件固定三段: `## 已确认`, `## 进行中`, `## 未知`.
4. 最终回复只需摘要并给出报告绝对路径.

## 任务

当前 r35 生产 Forms 已有两个长期驻留安全前缀:
- PowerCmd.Remove(PowerModel) -> Forms.FormsCode.FormStanceSafetyGuard.RemovePrefix
- PlayCardAction.ExecuteAction() -> Forms.FormsCode.FormStanceSafetyGuard.PlayPrefix

当前 LifecycleSmokeRunner.cs 仍把 Forms.FormStanceSafety owner 当成只有一个 Remove prefix, 因此真实生命周期烟测报告 `safetyGuardAfterShutdown=false`, 不是生产保护失败, 而是测试契约仍是旧 cardinality. 真实失败证据:
`G:\omp works\.tmp\forms-independent-20261005\native-r35-lifecycle-r38\l1-independent\forms-lifecycle-smoke.json`.
生产源码证据:
`G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs` 与 `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs`.

请只做最小测试载体修复:
- SafetyGuardProof 必须精确接受两个且仅两个安全 prefix.
- targets 必须恰好包含:
  `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power)`
  `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction()`
- prefixPatchTypes 两项都必须是 `Forms.FormsCode.FormStanceSafetyGuard`.
- prefixDeclaringMethods 必须恰好包含 `RemovePrefix` 和 `PlayPrefix`.
- removeMethodFound, removePatchedBySafetyOwner 保持必须为 true.
- SafetyGuardSameIdentity 必须比较两个 target/type/method 集合与 Remove method identity, 允许 Harmony metadata 顺序改变但不允许集合改变.
- 不降低其它生命周期门禁, 不把额外第三方 prefix 算入 Forms.FormStanceSafety owner, 不修改生产代码.
- 保留现有 JSON 字段形状, 让真实 report 继续输出完整 targets/prefixPatchTypes/prefixDeclaringMethods.

完成后在报告写入修改前后 SHA256, 精确行号与 delta, 并明确未构建/未运行.
