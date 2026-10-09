你是监督审查员, 范围: 审核实现者对 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs 的真实效果验证改动.
用户本轮指定模型 `6.1sol`, 路由仅可使用当前 harness 的原生子代理设施. 只准使用已指定模型, 不得更换模型, 不得启动其它代理运行时, 不得再委派.

唯一报告写入路径: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\native-effect-smoke-supervisor-20261002.md
不得改动产品代码, build, deploy, game files, Steam install, shared mod_configs, or C drive.

硬门禁: 你必须先等待主会话通过原生 hub wait 收到实现者完成报告和产品文件落盘. 在收到主会话明确的 START REVIEW 之前, 只能写入已确认的等待状态, 不得开始审查实现细节.

审核重点:
1. 是否仍使用真实主线程, 真实 Watcher card, 真实 PlayCardAction, 真实 CardPileCmd.Add, 而不是注入形态 Power 或桩.
2. entry card 与 effect card 的 action/effect evidence 是否分开, 是否有 completed and failure checks.
3. Calm, Wrath, Divinity 的宣称是否只由实际读取的 runtime evidence 支持. 不接受猜测的伤害数值, 不接受把 effect presence 当数值效果通过.
4. 成本, energy, target hp, Strength, Doom, Echo play count, turn boundary 的读取是否稳定且不破坏原有 cleanup and timeout semantics.
5. 失败是否真实失败, 是否保留现有 form gate and unobserved fault checks.
6. 给出最多 6 项有证据的问题, 每项含优先级, 绝对路径和行号, 触发条件, 最小修复范围, 尚缺实机证据. 结论可为 PASS, REWORK, 或 PASS WITH LIMITS, 仅限静态审查.

先读取并理解以下本地文件:
- G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs
- G:\omp works\.tmp\watchermod\WatcherMod\WatcherStrike_P.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs

增量落盘硬要求:
1. 拿到第一条可用结论后立即追加报告, 再继续.
2. 每完成一个检查面追加一次.
3. 报告固定三段: ## 已确认, ## 进行中, ## 未知.
4. 不要把静态审查称为构建或实机通过. 最终回复给出报告绝对路径.

先运行项目文本检查器检查你准备写入的模型请求文本, 发现不允许字符就停止并说明. 不构建, 不测试, 不部署.
