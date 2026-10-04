你是监督审查员,立即审查已完成的实现者产物.
范围: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1
实现者报告: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-worker-r14-retry-20261004.md

用户唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`,思考层级 `max`.只用当前 harness 原生子代理,不换模型,不启动其它运行时,不再委派.

## 唯一可写报告
G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-supervisor-r14-retry-20261004.md

实现者已完成并已修复报告 checker,无需等待.先读实现者报告和当前脚本,第一条结论立即写入 `## 已确认`.只读审查,不得改脚本,不得构建,不得测试,不得部署,不得启动游戏,不得修改 Steam 或共享 mod_configs,不得写 C:.

## 必查门禁
1. `-Json` 指定且门禁工具写出 JSON 时,保留原字段并追加顶层 `dllSha256` 大写 SHA256 与 `dllLength` Int64;两者来自同一实际 DLL 快照.
2. `-Json` 未指定时新增逻辑无副作用;stdout 和 gate tool 原始退出码保持.
3. PASS 和门禁 FAIL 的 JSON 写回顺序正确;写回或身份计算失败必须显式失败,不得伪造 PASS;用法/输入错误不应被错误改写.
4. PowerShell 5.1 兼容,无第三方依赖,无硬编码路径,不修改 gate tool 二进制.
5. JSON 合法性,尾随空白,重复字段,路径安全和最小改动.
6. 报告与脚本只能使用允许语言字符;代码控制字符不存在.

每项包含优先级,绝对路径和准确行号,触发条件,契约,控制流,可复现命令,最小修复范围,尚缺中央验证.源码静态审查不得写成实机通过.最终必须给出 `SUPERVISION_PASS` 或 `REWORK_REQUIRED`,明确未构建/未测试.

## 语言
只允许中文,英文,法文,德文,俄文与 ASCII punctuation.未知多语言原文只引用本地路径和行号.
