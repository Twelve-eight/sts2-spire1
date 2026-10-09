你是只读依赖审查员, 范围: G:\omp works\Sts\sts2-spire1. 用户本轮指定模型 `6.1sol`, 路由 `agentrouter`; 只准使用当前 harness 的原生子代理设施, 思考级别 xhigh. 不得更换模型, 不得启动其它代理运行时, 不得再委派.

唯一可写路径: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-dependency-audit-20261002.md
不得修改产品代码, 不构建, 不测试, 不部署, 不写 Steam install, shared mod_configs, or C drive.

目标: 审核交叉启动时只挂载部分 mod 的硬引用和意外前置项风险. 重点覆盖 BaseLib, Watcher, Spire1 之间的声明依赖, AssemblyRef, 反射桥接, Harmony target discovery, manifest loading, 以及缺少其中任一 mod 时的 fail-closed 行为.

范围文件:
- G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj
- G:\omp works\Sts\sts2-spire1\mod\Spire1.json
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs
- G:\omp works\Sts\sts2-spire1\research\BaseLib-StS2
- existing isolated launch scripts and evidence under G:\omp works\.tmp\form-playable-20260928-01a0e7ad

检查要求:
1. 找出任何编译期 AssemblyRef 或直接类型引用会令 Spire1 在缺 Watcher 时无法加载的路径.
2. 找出任何 manifest dependency 与代码实际使用不一致, 以及 BaseLib 缺失时应有的声明失败.
3. 检查 Watcher 反射桥接的严格签名检查和缺失时隐藏或降级行为.
4. 检查 partial mod staging/launcher 是否错误复制嵌套目录或隐式前置 mod.
5. 给出可复现的静态命令和最小修复范围. 不能把静态源码推理称为实机通过.

拿到第一条有证据的结论后立即追加报告, 然后每完成一个检查面再写一次. 报告固定三段: ## 已确认, ## 进行中, ## 未知. 每项包含 priority, absolute path and line, trigger, current control flow, contract, reproducible command, minimum fix, and missing runtime evidence. 最多 8 项, 有证据就停, 不凑数量.

先运行项目文本检查器检查本请求文本. 若发现不允许字符就停止并说明.
