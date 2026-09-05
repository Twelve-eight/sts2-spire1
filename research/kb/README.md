# sts2-spire1 知识库索引（research/kb/）

| 文件 | 内容 | 维护 |
|---|---|---|
| engine-facts.md | 引擎事实速查表：卡牌模型/联机/部署/日志取证四域 | 新事实结案即追加 |
| sts2-combat-semantics.md | StS2 战斗语义卷 S01-S14（EA 反编译 C#）：AttackCommand 逐击管线、CreatureCmd.Damage 全序（Osty 双相位）、Kill/ShouldDie 免死、PowerCmd 三态叠层、StS1→StS2 仲裁速查表 | 源码重导出或新结案时追加 |
| sts2-card-play.md | StS2 出牌管线 C01-C06：PlayCardAction 门序、资源先行（星抵超支）、OnPlayWrapper 主循环（playCount/附魔/affliction/归堆分流）、AutoPlay 免费语义 | 源码重导出或新结案时追加 |
| sts2-monster-ai.md | StS2 怪物 AI 卷 A01-A07：MoveStateMachine/ConditionalBranch、进场即 RollMove、敌人回合循环、AmountOnTurnStart 快照、ShouldClearBlock preventer、空手检查设计注记 | 源码重导出或新结案时追加 |
| sts2-combat-turn-machine.md | StS2 回合状态机卷 T01-T08：PlayerTurnPhase、EndTurnSignal、回合尾两段式（回合尾卡→Ethereal→Flush）、胜负插桩 | 源码重导出或新结案时追加 |
| sts2-orbs-enchantments.md | StS2 宝珠/附魔卷 O01-O07：Channel/Evoke 家族、TriggerPassive 触发次数钩子（Cables 泛化）、ModifyOrbValue（Focus 泛化）、一卡一附魔规则 | 源码重导出或新结案时追加 |
| sts2-hook-matrix.md | StS2 钩子实现矩阵（62/71 钩子，扫描器 scan-sts2-hooks.mjs + JSON）：早/晚相位变体体系、出牌/回合/伤害各族名单、StS1 顺序依赖的移植警示 | 源码重导出或新结案时追加 |
| research-methods.md | KB 研究方法与实录坑 M1-M17：javap/unzip/MSYS/ugrep/Node 坑、常量池扫描法、签名正则、时序推导、StS2 C# 工作流、自检纪律 | 每遇新坑/新方法即追加 |
| pitfalls.md | 陷阱模式库 P-01..P-10：症状/根因/修复/预防 四段式 | 每个结案 bug 追加一条 |
| debug-protocols.md | 取证 SOP：冻结协议、divergence 对拍、drain 管线、控制台、良性噪音清单 | 协议变化时更新 |
| aftp-interop.md | AFTP 许可证结论/fork 拓扑/问题清单/验证阻塞 | 上游或 fork 状态变化时更新 |
| loc-drift-report.md | 本地化 vs 官方原文相似度对账（318 条，A/B/C 分级） | loc_drift.js 可重跑刷新 |

关联卷：
- 一代数据与机制语义：`../sts1-kb/`（cards/relics/potions/events + mechanics/ 202 规则）
- 审计报告：`../audits/`（critique / devlog-audit / morning-summary / upstream-issue-draft）

原则：**skill 放方法，KB 放事实**。任何"下次还会用到的事实"进本目录；
任何"下次该怎么做"进 skill。
