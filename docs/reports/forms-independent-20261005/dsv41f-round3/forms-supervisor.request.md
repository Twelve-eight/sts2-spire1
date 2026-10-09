你是本批只读监督审查员. 唯一可写路径是下述报告, 不写产品代码. 此任务先登记 WAITING_GATE 然后结束等待回合, 不轮询不审查实现者正在写入的源码. 待主会话实际 multi_agent_v1.wait_agent 精确实现者 returned completed, 并发送带目标id/时间/coordination门禁的消息后才开始独立审查. 不能以CODE_COMPLETE或文件存在代替原生门禁. 完成监督明确 SUPERVISION_PASS 或 NEEDS_REWORK, 每项准确路径行号, 证据风险最小修复和未实机面. 不把源码推理称实机.
报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round3\forms-supervisor.md. 实现者和监督同批, id随后由hub给出. 审查范围同目录 forms-worker.request.md 的全项, 独立检查源码而非复述报告. 可读原生SDK/Godot本机API和已反编译Watcher. 特别核对关闭前状态/续体代数/主线程泵/部分安装回滚/资源/CustomID. 
用户本轮唯一指定 global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh. 禁止再委派, 禁止任何其它模型或 fallback, 禁止启动 codex exec / omp / 外部 agent harness. 不写 C:, 不构建 lint 测试部署, 不运行游戏, 不改 Steam / 共享 mod_configs, 不操作 canonical Release / Workshop staging, 不执行 git add -A/reset/clean. 主会话集中构建验证. 不改其它会话的发布脚本.
增量落盘: 第一条可用结论立即写唯一报告, 每检查或完成一面再次追加. 按 ## 已确认 / ## 进行中 / ## 未知 分段. 代码完成记录 CODE_COMPLETE 和实际修改文件列表, 未验证面明确标识. 只允许中文 English French German Russian 和 ASCII 标点.
旧 round2 两个 supervisor 的元数据在恢复后曾解析为未授权的 ovoapi:6.1sol. 旧审查不作为本轮验收证据, 只能作待重新核实的线索. 两个旧 worker 的元数据确认一直是指定 DeepSeek. 所有新产物必须经过本批新监督.
契约: G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md.
## 模板原件
# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 监督审查员, 范围: G:\omp works\Sts\sts2-forms.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round3\forms-supervisor.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 10 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

---

## 主会话收割方法

```powershell
Get-ChildItem -LiteralPath '<报告目录>' -File | Sort-Object LastWriteTime |
  Select-Object Name,Length,LastWriteTime
```

- 子代理超时/中断/额度耗尽时: 先读报告文件, 把 `已确认` 当作可用证据, `进行中` 当作半成品复核, 不得当作无产出.
- 收到第一批落盘结果后即向用户汇报一次, 后续增量补充.