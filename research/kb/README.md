# sts2-spire1 知识库索引（research/kb/）

| 文件 | 内容 | 维护 |
|---|---|---|
| engine-facts.md | 引擎事实速查表：卡牌模型/联机/部署/日志取证四域 | 新事实结案即追加 |
| sts2-combat-semantics.md | StS2 战斗语义卷 S01-S14（EA 反编译 C#）：AttackCommand 逐击管线、CreatureCmd.Damage 全序（Osty 双相位）、Kill/ShouldDie 免死、PowerCmd 三态叠层、StS1→StS2 仲裁速查表 | 源码重导出或新结案时追加 |
| pitfalls.md | 陷阱模式库 P-01..P-10：症状/根因/修复/预防 四段式 | 每个结案 bug 追加一条 |
| debug-protocols.md | 取证 SOP：冻结协议、divergence 对拍、drain 管线、控制台、良性噪音清单 | 协议变化时更新 |
| aftp-interop.md | AFTP 许可证结论/fork 拓扑/问题清单/验证阻塞 | 上游或 fork 状态变化时更新 |
| loc-drift-report.md | 本地化 vs 官方原文相似度对账（318 条，A/B/C 分级） | loc_drift.js 可重跑刷新 |

关联卷：
- 一代数据与机制语义：`../sts1-kb/`（cards/relics/potions/events + mechanics/ 202 规则）
- 审计报告：`../audits/`（critique / devlog-audit / morning-summary / upstream-issue-draft）

原则：**skill 放方法，KB 放事实**。任何"下次还会用到的事实"进本目录；
任何"下次该怎么做"进 skill。
