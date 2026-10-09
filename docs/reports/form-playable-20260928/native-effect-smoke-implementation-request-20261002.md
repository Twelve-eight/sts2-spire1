你是实现者, 范围: 为 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs 增加真实效果验证场景.
用户本轮指定模型 `6.1sol`, 路由仅可使用当前 harness 的原生子代理设施. 只准使用已指定模型, 不得更换模型, 不得启动其它代理运行时, 不得再委派.

唯一产品代码写入路径: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs
唯一报告写入路径: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\native-effect-smoke-implementation-20261002.md
不得改动其它产品代码, csproj, docs, build scripts, game files, Steam install, shared mod_configs, or C drive.

目标:
1. 保持真实主线程, 真实 RunManager, 真实 custom modifier, 真实 Watcher card path, 真实 PlayCardAction and CardPileCmd.Add.
2. 在现有三场景中, 进入 Calm, Wrath, Divinity 后再注入真实 WatcherStrike_P, 基础费用 1, 基础伤害 6. 不得直接注入形态 Power.
3. 将 entry card action 和 effect card action 的证据分开. 至少记录 action status, energy before/after, target hp before/after, relevant form state and expected effect evidence.
4. Calm: 证明后续 WatcherStrike_P 可完成, energy cost is free exactly once, and Serpent damage is visible in target hp change or an explicit effect observation. Entry card may leave energy 1, so use snapshots and avoid inventing exact draw counts.
5. Wrath: 证明 DemonFormPower is present and Strength is nonzero after entry, WatcherStrike_P completes, target hp delta is consistent with a powered attack under Wrath plus Reaper Doom if observable. If exact numeric decomposition is not stable, record exact observed hp and Doom/Strength evidence instead of guessing.
6. Divinity: 证明 entry resources are present, WatcherStrike_P completes, Echo extra play is observable through target hp delta or a bounded evidence field, and the carrier/effects remain or exit only according to observed turn boundary. Do not claim a value unless read from the runtime.
7. Fail honestly when any required evidence is unavailable. Do not weaken existing form gate.
8. Reuse existing timeout, cleanup, detached operation, and JSON write patterns. Do not run build or tests yourself.

先读取并理解以下本地文件, 不要把原始多语言日志复制进请求:
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
4. 报告中给绝对路径和准确行号, 说明实现边界. 最终回复只需摘要和报告路径.

先运行项目文本检查器检查你准备写入的模型请求文本, 发现不允许字符就停止并说明. 只写代码和报告, 不构建, 不测试, 不部署.
