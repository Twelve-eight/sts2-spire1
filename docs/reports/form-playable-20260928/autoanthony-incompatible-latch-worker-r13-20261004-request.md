你是实现者,范围:
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs

用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`,思考层级 `max`.只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时,不得再委派.

## 唯一可写报告路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-incompatible-latch-worker-r13-20261004.md`

产品代码仅可修改上述两个 Interop 文件.不构建,不运行测试,不部署,不启动游戏,不修改 Steam 安装,不修改共享 `mod_configs`,不写 C:.

## 增量落盘硬要求

1. 先读当前源码和已有报告.
2. 拿到第一条可用结论后,必须先追加到报告的 `## 已确认` 段,再修改产品代码.
3. 每完成一个检查面再追加一次.报告固定包含 `## 已确认`,`## 进行中`,`## 未知` 三段.
4. 最终回复只需摘要并给出报告绝对路径;报告必须自洽,包含实际修改文件和准确行号.

## 背景问题

当前 `AutoAnthonyCompatBridge.ResolveReflection()` 在 AutoAnthony 程序集已加载但版本不兼容时,每次 Apply 都重新反射解析并记录 Error.由于 core patch bits 永远未完成,`NeedsRetryWithoutAssemblyLoad` 长期为 true,`AutoAnthonyLoadHook` 每秒创建 retry wake source并重复调用 Apply,造成无效 CPU 和日志空转.这是当前静态复核唯一待修复的 P3.

触发条件是 AutoAnthony 程序集确实已加载,但必需类型或成员缺失或解析抛异常,例如 `ChaosCardGenerator.GeneratedCharacter`,`AutoAnthony.ChaosRunDefinitions`,`AutoAnthony.ChaosCardRegistry` 或现有必需 getter/method 无法解析.不要把 AutoAnthony 缺席误判为不兼容;缺席必须仍能等待晚加载.

## 实现验收契约

1. 为 core reflection failure 添加明确的终态 latch 或等价状态.同一不兼容字节生命周期内只记录一次主要 Error,不再每秒重复解析和刷日志.
2. `NeedsRetryWithoutAssemblyLoad` 在该终态下不得维持无意义的周期重试.同时不能破坏普通 Pending, partial patch rollback, `OfficialAddonPending` 等仍可前进的路径.
3. 不得把"程序集尚未出现"变成 terminal; AutoAnthony 后加载时仍须第一次尝试.
4. 不得误阻断 `AutoAnthonyWatcher` 晚加载/官方接管路径.即使 core 已确定 incompatible,也要保留必要的 AssemblyLoad 观察或明确,可解释的 fail-closed 状态;不能让终态 latch 让 official addon 的现有接管清理逻辑产生半套 patch.如果官方 addon 在 core incompatible 后到达,路径必须是安全 no-op 或安全 settled,且不能恢复周期热循环.
5. 不得引入 AutoAnthony, Watcher 或 AutoAnthonyWatcher 的编译期硬引用.不得改变现有主线程/Harmony边界和 fail-closed 语义.
6. 保持错误恢复/异常安全:缓存只在完整解析成功后写入;失败后不能留下部分成功状态导致重复 patch.现有静态字段生命周期为进程级,不需要跨进程重置.
7. 仅做最小必要改动,更新相关注释,不要添加 stub 或虚假测试.

## 检查和报告要求

至少检查:
- `Apply`,`ResolveReflection`,`NeedsRetryWithoutAssemblyLoad` 的状态转换.
- `AutoAnthonyLoadHook.ExecuteApplyCore`,`NeedsPeriodicRetry`,AssemblyLoad hook 在 terminal core failure 与 official addon late-load 时的交互.
- 日志是否只在首次终态转换时记录,是否仍能诊断失败原因.
- 是否保留原有可选 Watcher 缺席和 `OfficialAddonPending` 重试.

每个结论包含优先级,绝对路径和准确行号,触发条件,契约,当前控制流,可复现命令,最小修复范围,尚缺实机证据.源码推理不得称为实机复现.最终说明未构建/未测试.

## 语言

只允许中文,英文,法文,德文,俄文与 ASCII 标点.未知多语言原文只引用本地路径和行号.

