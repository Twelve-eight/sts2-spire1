
你是 实现者, 范围: G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `gateway, 用户指定 wb2api, 更细实际路由需元数据证据`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004/promote-path-worker-r3.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 1 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

## 实现者特例与精确任务
本节替代模板中的只读产品限制. 除唯一报告外, 仅允许修改 G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1. 不得修改其他文件, 不提交, 不构建或执行任何测试/parse/lint/pack. 所有验证由主会话集中执行.
修复同批监督报告 G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\resume-script-supervisor-r2.md 的 P2: workshop\content\Spire1 是空 junction 时, 条件 leaf 检查漏过. 主会话已在 G: 隔离 AST 夹具确认 empty-junction: Passed=False Exit=0 TargetUnchanged=False, 证据位于 G:\omp works\.tmp\workshop-prep-20261004-central 下 promote-path-before-ps7-*\results.json 与 run.log.
先读 G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md. 最小修复为对 $workshopRoot 无条件调用 Assert-SafeWriteTarget, Root 为 Join-Path $RepoRoot 'workshop\content', Name 为 'workshop-root'. 推荐放在现有 workshop-content-root 检查后, 任何 canonical/Workshop 写入之前, 使违规路径早拒绝. 不重构, 不更改其余门禁/mtime/非原子顺序/默认行为. 保留编码和换行.
第一条结论立即写报告. 完成后写 CODE_COMPLETE, 精确行号与最终 SHA256, 记录修改文件和未验证边界. 监督者同批派发, 由主会话真实 wait_agent 等到你完成并留证后激活. 不得自称监督通过. 用户只使用 DeepSeek 子代理, reasoning max, 不再委派.
