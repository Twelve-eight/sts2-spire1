你是实现者,只修改这个产品脚本:
G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1

用户唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`,思考层级 `max`.只用当前 harness 原生子代理,不换模型,不启动其它运行时,不再委派.

唯一可写报告:
G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-worker-r14-retry-20261004.md

立即读取当前脚本和报告协议.首条基线先写报告 `## 已确认`,然后编辑脚本.只做源码实现,不要构建,不要测试,不要部署,不要启动游戏,不要改 Steam,不要改共享 mod_configs,不要写 C:.

任务: 当 `-Json` 指定且 gate tool 已写出结果 JSON 时,在不破坏原字段的前提下追加顶层 `dllSha256` 和 `dllLength`.哈希必须是指定 DLL 的大写 SHA256,长度必须是同一 DLL 的 Int64 字节长度.使用 PowerShell 5.1 兼容语法和现有 G: 约束. gate tool 原始 stdout 和退出码必须保留. `-Json` 未指定时不新增副作用.如果 JSON 写回或身份计算失败,必须显式失败且不能伪造 PASS.修改最小,不要改 gate tool 二进制.

报告固定三段 `## 已确认`,`## 进行中`,`## 未知`,每项含优先级,绝对路径与行号,触发条件,契约,控制流,可复现命令,最小修复范围,尚缺中央验证.只允许中文,英文,法文,德文,俄文和 ASCII punctuation.
