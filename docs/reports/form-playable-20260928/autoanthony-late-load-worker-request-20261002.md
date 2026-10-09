你是 实现者, 范围: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs; G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs.
用户本轮唯一指定模型 `6.1sol`, 路由 `agentrouter / ovoapi:6.1sol`. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-late-load-worker-20261002.md` (产品代码可按范围编辑; 不构建/不运行测试/不部署/不启动游戏, 不写 Steam install,shared mod_configs 或 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论后立即追加报告, 然后才做下一步.
2. 每完成一个检查面就落盘;结论未定也写证据.
3. 报告固定三段: `## 已确认`, `## 进行中`, `## 未知`.

## 实现范围与验收契约

当前风险: AutoAnthonyCompatBridge.Apply 使用一个整体 `_applied` 状态. 如果 AutoAnthony 先于 Watcher 加载, core bridge 成功后 `_applied=true`; Watcher 后加载时再次 Apply 会提前返回,导致 Watcher card pool bridge 永远不重试. 修复必须满足:

1. core AutoAnthony patches 与 optional Watcher/AutoAnthonyWatcher capability 分开跟踪,重复调用不得重复 Harmony patch.
2. AutoAnthony 未装时 Spire1 必须保持可加载且不产生硬引用; AutoAnthony 装后 core bridge 能应用.
3. Watcher 后加载时 bridge 必须重新尝试; AutoAnthonyWatcher 官方 addon 存在时必须视为 capability settled,不再旧 bridge.
4. AutoAnthonyLoadHook 必须监听会影响 capability 的 assembly load,并在成功/settled 后安全摘除;不得因为无 Watcher 导致 Spire1 initializer 失败.
5. 失败边界 fail-closed:某一 patch group 失败不能让 Spire1 初始化抛错,也不能把已成功 group 重复安装.
6. 只改上述两个产品文件,不发明第三方 API,不写 stub.

要求: 先读当前源码与既有报告,首条证据先落盘,再编辑. 不构建,不测试,不部署. 无法确认的项写 flag.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点.

