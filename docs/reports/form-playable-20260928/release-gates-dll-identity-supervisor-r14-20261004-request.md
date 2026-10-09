你是监督审查员,范围:
G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1
实现者报告:
G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-worker-r14-20261004.md

用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`,思考层级 `max`.只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时,不得再委派.

## 唯一可写报告路径
报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-supervisor-r14-20261004.md`
实现者完成后再审查.只读,不得修改脚本,不得构建,不得测试,不得部署,不得启动游戏,不得修改 Steam 安装或共享 `mod_configs`,不得写 C:.

## 增量落盘硬要求
读取实现者报告和当前脚本后,第一条结论立即写入 `## 已确认`.每完成一个检查面追加一次.固定包含 `## 已确认`,`## 进行中`,`## 未知`.最终结论必须为 `SUPERVISION_PASS` 或 `REWORK_REQUIRED`.

## 审查门禁
1. `-Json` 输出新增顶层 `dllSha256` 大写 SHA256 和 `dllLength` Int64,二者来自同一已验证 DLL.
2. 原 gate tool stdout,门禁失败退出码和 `-Json` 未指定行为不变.
3. gate JSON 已生成时,门禁工具成功或失败均应保留原 JSON 并追加身份字段;写回失败不得伪造 PASS.
4. PowerShell 5.1 兼容,无第三方依赖,无硬编码发布路径,不修改 gate tool 二进制.
5. 改动范围最小,无隐性吞错或安全路径绕过.

每项给出优先级,绝对路径和准确行号,触发条件,契约,控制流,可复现命令,最小修复范围,尚缺中央验证.明确未构建/未测试.

## 语言
只允许中文,英文,法文,德文,俄文与 ASCII punctuation.未知多语言原文只引用本地路径和行号.
