# Forms 交接 2026-10-06（r40 之后）

## 当前状态
- 独立仓 `G:\omp works\Sts\sts2-forms`，commit `0893330` 已推送，工作树干净；无游戏进程残留。
- r40 Beta：`G:\omp works\.tmp\forms-independent-20261005\Forms-Beta-20261005-r40.zip`
  SHA256 `187BAE55A0B8F7E31E0C9FE5775CDBBA45EA65364AE70E57FCAC0B59E116228B`
- 构建 1 warning：`DemonFormPower.cs:214` CS1998。
- 旧交接与证据：`docs/HANDOFF-forms-current-20261005.md`（末尾 r40 段），
  `docs/reports/forms-independent-20261005/central-progress-20261005.md`，
  门禁 `.tmp\forms-independent-20261005\forms-independent-r40-gates.json`，
  路由 `.tmp\forms-independent-20261005\agent-route-r40-current.json`。

## 本会话情况
- 本会话无原生子代理工具，未派发任何子代理，未改代码，未启动游戏。
- 用户已授权：可抢前台、启动破解版测试副本做实机烟测。

## 用户最新指令（下个会话执行）
- 主会话只做规划统筹 + 集中构建/部署/实机验证。
- 开发子代理：仅 deepseek（按 `global:deepseek-v4.1-flash` via wb2api，xhigh；如有歧义先问用户）。
- 首遍开发完成后：dsv4.1f(wb) max 审核子代理。
- 每项功能：opus5.5 (ki) high 验收子代理。派发前须从会话元数据核实实际解析的模型/提供方；无法核实则不派并上报。
- 并发 <= 4；子代理禁止再委派、禁止 codex exec/omp；每个子代理须有唯一报告路径（AGENTS.md 8b 模板 `.tooling\subagent-report-protocol.md`）。

## 计划
1. 主会话部署 r40 + BaseLib(>=3.4.5) + Watcher 到 `E:\Slay the Spire 2`（仅测试副本），前台实机：三形态入口、图标、tooltip、中文文案、首回合效果，截图落盘。
2. 问题按功能拆片给 deepseek 开发子代理；顺带修 CS1998。
3. dsv4.1f max 审核 -> opus5.5 验收（逐项）。
4. 集中构建、重跑 r40 回归套件（effects/lifecycle/runtime-safety/unselected/shutdown/terminal/r8-startup-matrix）+ 实机复测，出 r41 Beta，DEVLOG 记录真实路由。

## 未验证（不得称为发行版）
可见 UI/手动入口、视觉、长战斗、战中读档、多人同步、性能平衡、热卸载、Steam 可见性、Workshop。

## 硬约束
不写 Steam 安装、不改共享 mod_configs、不写 C:、不覆盖 Spire1 Release/Workshop、不恢复旧子代理会话、用户可见文本中文。