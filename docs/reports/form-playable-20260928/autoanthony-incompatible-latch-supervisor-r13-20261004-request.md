你是监督审查员,范围:
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs
实现者报告:
G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-incompatible-latch-worker-r13-20261004.md

用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`,思考层级 `max`.只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时,不得再委派.

## 唯一可写报告路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-incompatible-latch-supervisor-r13-20261004.md`

先等待实现者报告存在且显示代码修改已完成,再开始审查.不得在实现者完成前把半成品当结论.只读审查,不得修改产品代码,不得构建,不得测试,不得部署,不得启动游戏,不得修改 Steam 安装或共享 `mod_configs`,不得写 C:.

## 增量落盘硬要求

1. 先读取实现者报告和当前源码.拿到第一条可用结论后立即追加到 `## 已确认`,再继续.
2. 每完成一个检查面追加一次.报告固定包含 `## 已确认`,`## 进行中`,`## 未知`.
3. 最终结论必须为 `SUPERVISION_PASS` 或 `REWORK_REQUIRED`,并给出准确绝对路径和行号.

## 验收门禁

1. AutoAnthony 程序集缺席时仍可等待晚加载.
2. AutoAnthony 已加载但必需反射类型或成员不兼容时,同一进程内只记录一次主要 Error,不再每秒解析或刷日志;周期 retry 必须停止.
3. 普通 Pending,partial rollback,OfficialAddonPending 仍可重试.
4. AutoAnthonyWatcher 晚加载不能被 core incompatible latch 误阻断.必须保留必要的 AssemblyLoad 观察,并保证官方接管和旧 patch 清理不会产生半套 patch或无限周期热循环.
5. 仍无 AutoAnthony,Watcher,AutoAnthonyWatcher 的硬 AssemblyRef;主线程/Harmony边界和 fail-closed 语义不回退.
6. 反射缓存仅在完整成功后写入;失败状态清晰且异常安全;实现最小且无 stub.

## 审查方法

逐行复核 `Apply`, `ResolveReflection`, `NeedsRetryWithoutAssemblyLoad`, `ExecuteApplyCore`, `NeedsPeriodicRetry`, `HookAssemblyLoad`, `OnAssemblyLoad`,以及官方 addon 分支.检查重复日志/重试,状态转换,late-load和partial rollback.只把源码证据称为源码证据,不把它称为实机通过.未构建/未测试必须明确.

每项包含优先级,绝对路径和准确行号,触发条件,契约,控制流,可复现命令,最小修复范围,尚缺实机证据.

## 语言

只允许中文,英文,法文,德文,俄文与 ASCII punctuation.未知多语言原文只引用本地路径和行号.

