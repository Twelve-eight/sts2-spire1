你是只读启动矩阵审查员, 范围: G:\omp works\Sts\sts2-spire1. 用户本轮指定模型 `6.1sol`, 路由 `agentrouter`; 只准使用当前 harness 的原生子代理设施, 思考级别 xhigh. 不得更换模型, 不得启动其它代理运行时, 不得再委派.

唯一可写路径: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-launch-matrix-audit-20261002.md
不得修改产品代码, 不构建, 不测试, 不部署, 不写 Steam install, shared mod_configs, or C drive.

目标: 审核交叉启动时只挂载部分 mod 的实际启动矩阵, 确保不会因 staging 或加载器错误引入未声明的硬前置. 只做静态和已有证据审计, 不启动游戏.

范围:
- G:\omp works\Sts\sts2-spire1\mod\Spire1.json
- G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs
- G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001
- G:\omp works\Sts\_runtime\sts2-test-client-B\mods
- G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928

检查矩阵:
1. BaseLib + Spire1, no Watcher.
2. BaseLib + Watcher, no Spire1.
3. BaseLib only.
4. Spire1 without BaseLib, expected explicit dependency failure.
5. Staging with only declared top-level mod folders, no nested duplicate BaseLib or Watcher.

重点: 只描述已有证据和可复现命令; 区分源码证据, 隔离 staging 证据, 真实运行证据, 未验证边界. 不要把历史旧 staging 的 Loaded 2 mods 结果当作当前证据.

拿到第一条有证据的结论后立即追加报告, 然后每完成一个矩阵面再写一次. 报告固定三段: ## 已确认, ## 进行中, ## 未知. 每项包含 priority, absolute path and line, trigger, current control flow, contract, reproducible command, minimum fix, and missing runtime evidence. 最多 8 项, 有证据就停, 不凑数量.

先运行项目文本检查器检查本请求文本. 若发现不允许字符就停止并说明.
