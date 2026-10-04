你是实现者,范围: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`. 只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时(omp/codex 等),不得再委派.

唯一可写报告路径:
G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-code3-worker-r16-20261004.md

代码写入范围:
只允许修改上述 run-release-gates.ps1. 不改其它产品代码,不构建,不测试,不部署,不启动游戏,不修改 Steam 安装,不修改共享 mod_configs,不写 C:.

任务背景:
中央真实执行发现一个与脚本声明不一致的错误路径. 脚本 L15 声明缺失 DLL 应返回 3. 但 L26 的 `$ErrorActionPreference = "Stop"` 与 L35 的 `Write-Error` 组合会在 `exit 3` 到达前终止 powershell.exe,实测进程退出码为 1. 复现路径和输出已由主会话记录,不要把这个行为当成理论猜测.

任务:
1. 先只读读取脚本,把编辑前基线立即追加到唯一报告文件. 报告必须有 `## 已确认`,`## 进行中`,`## 未知` 三段. 基线至少记录 L26-L37,当前字节信息,no BOM,CRLF,U+3002=0,以及中央复现命令和实际 exit 1.
2. 最小修复: 让缺失 DLL 的错误路径真正返回 3,同时保留错误文本写到 stderr. 推荐只把 L35 的 `Write-Error ...` 改为不会受 `$ErrorActionPreference=Stop` 影响的 stderr 输出,例如 `[Console]::Error.WriteLine(...)`; 保留 L36 `exit 3`. 不改其它分支,不改身份字段回写逻辑.
3. 修复后追加准确行号,控制流,字节检查和最小 diff 到报告. 保持 no BOM,CRLF,不引入非允许字符.
4. 不构建,不运行门禁,不部署. 超出范围问题只记录,不要顺手修改.

每项报告包含优先级,绝对路径与准确行号,触发条件,契约,当前控制流,可复现命令,最小修复范围,尚缺的实机证据. 语言只允许中文,英文,法文,德文,俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

先落盘后继续是硬要求. 最终回复只给摘要和报告绝对路径.