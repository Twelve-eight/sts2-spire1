你是只读审查员. 审查当前 AutoAnthony late-load 兼容实现和当前 Release 字节边界, 不修改产品代码.

用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `gateway/wb2api`, 思考层级 `max`. 只准使用当前 Codex harness 原生子代理设施, 不得换模型, 不得启动其它代理运行时, 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-current-deepseek-review-20261004.md`.
不得修改产品代码, 构建, 测试, 部署, 启动游戏, 写 Steam, 写 shared mod_configs 或 C:.

## 输入

当前源文件:
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs`
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs`
- `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj`
- `G:\omp works\Sts\sts2-spire1\mod\Spire1.json`
当前 Release 证据:
- `G:\omp works\.tmp\spire1-release-r12-20261004-central\evidence\release-gates.json`
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r12-current-20261004-rerun\run-final.json`
历史报告仅作上下文, 不直接采信:
- `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-late-load-reviewer-rework2-20261002.md`
- `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-cross-launch-post-fix-review-20261003.md`
协议: `G:\omp works\.tooling\subagent-report-protocol.md`.

## 审查目标

1. 缺失 Watcher 类型时是否保持 Pending 且不形成 partial settled capability.
2. Pending, 异常回滚, OfficialAddonPending 是否有 AssemblyLoad 和 timer/deferred 重试源.
3. initializer, AssemblyLoad, timer, fallback thread 是否统一进入主线程 deferred Apply, 是否存在锁反转或旁路 Harmony Apply.
4. 官方 AutoAnthonyWatcher 晚加载接管是否先回滚 legacy bridge, 回滚失败是否 fail-closed 且可重试.
5. AutoAnthony, Watcher, AutoAnthonyWatcher 是否仅字符串和反射, 无硬 AssemblyRef, 并核对当前 Release 门禁证据.
6. 只把当前源文件和当前 r12 release evidence 作为当前状态; 旧跨启动日志若 DLL hash 不匹配必须标为不适用.

每条结论给绝对路径, 准确行号, 触发条件, 当前控制流, 最小修复范围和未验证实机边界. 不发现阻断时明确 PASS, 不凑数量. 首条证据立即落盘, 每完成一个检查面追加一次. 报告固定包含 `## 已确认`, `## 进行中`, `## 未知`. 最终写 `SUPERVISION_PASS` 或 `SUPERVISION_REWORK`.