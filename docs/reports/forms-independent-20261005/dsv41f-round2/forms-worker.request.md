你是实现者,范围: G:\omp works\Sts\sts2-forms. 用户本轮唯一指定模型 global:deepseek-v4.1-flash, 路由 wb2api. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时, 不得再委派.

## 唯一可写路径

只可写 G:\omp works\Sts\sts2-forms 以及报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\forms-worker.md. 不改 G:\omp works\Sts\sts2-spire1 的产品源码. 不构建, 不部署, 不启动游戏, 不改共享配置, 不写 C:.

## 增量落盘硬要求

拿到第一条可用结论后立即追加报告, 每完成一个检查面再追加一次. 报告固定为 ## 已确认, ## 进行中, ## 未知. 最终回复给出报告绝对路径和修改文件清单.

## 输入和契约

先读:

- G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md
- G:\omp works\Sts\sts2-spire1\docs\HANDOFF-forms-current-20261005.md
- G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\build-split-audit.md
- G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\coupling-audit.md
- 当前源文件 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs

## 实现范围

在 G:\omp works\Sts\sts2-forms 创建独立 Forms mod 项目:

1. 复制并改造 17 个 Forms 源文件到 FormsCode, 命名空间改为 Forms.FormsCode, 不保留任何 using Spire1 或类型名 Spire1 的编译期引用.
2. 独立项目提供 Forms.csproj, Forms.json, project.godot, Sts2PathDiscovery.props, Forms 资源根和 localization/eng,zhs. 项目基础参照当前 Spire1 和现有独立 mod, 但不要复制 Spire1 的大门禁或发布脚本.
3. 提供 Forms.FormsCode.MainFile, 使用自己的 ModInitializer, SimpleLoc 和自己程序集的 Harmony patch scan. 不依赖 Spire1 MainFile.
4. Watcher 只通过严格反射桥接. 缺 Watcher 时初始化完成且状态为 disabled 或 retryable, 不让类型初始化异常传播到普通加载.
5. 将 FormStanceMode, FormStanceCmd, FormStanceModifier, 9 个 power model 和 Watcher bridge 自持. FormStanceModifier 使用 CustomIDAttribute 保留契约中的 10 个 SPIRE1-* ID. 若某个旧类型没有稳定身份证据, 记录未知, 不伪造行为.
6. 资源路径从 res://Spire1 改为 res://Forms, 只迁移 Forms 需要的图标和本地化. 不复制无关 Spire1 资产.
7. 暴露给 Spire1 可选反射协议 Forms.FormsCode.Interop.FormsRuntimeEntryPoint, 至少包括 IsAvailable, IsSelected(Player), KindOf(Type), CurrentKind(Player), Enter(PlayerChoiceContext,Player,Type,CardModel), Exit(PlayerChoiceContext,Player,CardModel). 参数和返回签名以契约文件为准. 缺 Forms 时不影响独立 Forms 自身.
8. Forms 不得直接引用 Spire1 的 StancePower, StanceCmd, Spire1Config, IOnStanceChanged, MainFile, Spire1Power 或 Spire1 namespace. 如果某旧逻辑需要通知 Spire1, 只做运行时反射兼容; Spire1 缺失时跳过通知.
9. 原 FormNativeSmokeRunner 不复制到生产 Forms 项目. 如需保留测试入口, 只写说明到报告, 不做测试 stub.

## 允许的处理原则

- 可以重命名 namespace 和文件夹.
- 可以用反射访问 Watcher 和可选 Spire1.
- 可以把原有 Spire1 StancePower 语义改成 Forms 自有 carrier 基类, 但不能改变六形态效果契约.
- 不能通过复制一个假的 Spire1 类型或空方法让编译变绿.
- 不能修改 Spire1 项目来适应 Forms.
- 不构建和不测试;发现可能编译失败就记录准确文件和原因,继续完成可恢复实现或明确阻塞.

## 输出要求

报告必须区分源码已改, 进行中和未知. 最终回复只摘要, 给出报告路径和修改文件清单.
