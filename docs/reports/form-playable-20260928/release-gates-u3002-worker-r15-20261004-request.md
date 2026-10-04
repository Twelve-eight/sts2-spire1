你是实现者,范围: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `gateway/wb2api`. 只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时(omp/codex 等),不得再委派.

唯一可写报告路径:
G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-gates-u3002-worker-r15-20261004.md

代码写入范围:
只允许修改 G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1. 不改其它产品代码,不构建,不测试,不部署,不启动游戏,不修改 Steam 安装,不修改共享 mod_configs,不写 C:.

任务:
1. 先读取当前脚本并把编辑前基线立即追加到唯一报告文件,报告必须有 `## 已确认`,`## 进行中`,`## 未知` 三段. 基线至少记录绝对路径,当前 U+3002 计数与行号,脚本字节长度,是否 BOM,CRLF 情况,以及当前身份回写实现未改动的事实.
2. 仅把脚本中的全部 U+3002 替换成 ASCII `.` 或删除. 当前已知位置为 L1,L5,L15,L17,L64,L66,L67,L71,L78. 不改运行语义,不改身份回写控制流.
3. 保持 no BOM 和 CRLF. 不要用会把文件转成 LF 或写入 BOM 的编辑方式.
4. 完成后立即把修复事实,准确行号,字节检查命令,最小差异和未验证边界追加到报告. 不构建,不运行门禁,不部署.
5. 如果发现超出本范围的问题,只在报告中标记,不要顺手修改.

每项报告包含优先级,绝对路径与准确行号,触发条件,契约,当前控制流,可复现命令,最小修复范围,尚缺的实机证据. 语言只允许中文,英文,法文,德文,俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

先落盘后继续是硬要求. 最终回复只给摘要和报告绝对路径.