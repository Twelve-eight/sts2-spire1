你是实现者,范围:
G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1

用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`,思考层级 `max`.只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时,不得再委派.

## 唯一可写报告路径
报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-dll-identity-worker-r14-20261004.md`
产品代码只可修改上述脚本.不构建,不测试,不部署,不启动游戏,不修改 Steam 安装,不修改共享 `mod_configs`,不写 C:.

## 增量落盘硬要求
1. 先读脚本和现有发布报告.
2. 拿到第一条可用结论后先追加到报告 `## 已确认`,再编辑脚本.
3. 每完成一个检查面追加一次.报告必须有 `## 已确认`,`## 进行中`,`## 未知`.

## 任务
当前 run-release-gates.ps1 在 `-Json` 输出中只保存 DLL 路径与门禁结果,不直接保存 DLL SHA256 和字节长度.为避免发布记录把错误字节误绑定到 PASS,对指定 `-Dll` 计算并写入顶层字段:
- `dllSha256`: 大写 SHA256 字符串.
- `dllLength`: Int64 字节长度.

## 验收契约
1. `-Json` 未指定时原有 stdout,退出码和门禁行为不变.
2. `-Json` 指定且门禁工具成功或失败后,若工具已写出 JSON,文件保留原字段并新增两个字段.不要计算 JSON 自身哈希.
3. 使用 PowerShell 5.1 兼容语法和现有 G: 缓存约束;不引入第三方模块.
4. 文件路径必须安全,使用已验证的 `$Dll`;哈希必须来自实际 DLL,长度必须来自同一文件快照.
5. 不改 gate tool 二进制,不加 stub.不吞掉门禁工具原始退出码;若追加身份字段失败,应明确失败而不是伪造 PASS.
6. 产品脚本改动最小,注释说明输出契约.

## 检查和报告
检查 `-Dll`,`-Json`,门禁工具退出码和 JSON 写回顺序.报告每项包含优先级,绝对路径和行号,触发条件,契约,控制流,可复现命令,最小修复范围,尚缺中央验证.
只做源码实现,不构建和运行.

## 语言
只允许中文,英文,法文,德文,俄文与 ASCII punctuation.未知多语言原文只引用本地路径和行号.
