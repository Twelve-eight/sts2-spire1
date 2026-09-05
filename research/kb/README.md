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
| sts2-afflictions.md | StS2 负面附灵卷 F01-F04：施加门序（ShouldAfflict 钩子/类型白名单/Unplayable 门）、一卡一附灵、OnPlay 位置（附魔后）、逻辑旁挂 Power 的双件套模式、7 个 vanilla 附灵清单 | 源码重导出或新结案时追加 |
| pool-architecture.md | 卡池架构与玩法语义不变量卷 I0-I3/G1-G4：两代池架构、可调用集合≠池对象（Splash 案）、池注册契约与引擎注册表可见性（AutoAnthony/GA 案）、颜色池不相交假设、开发模式提升（KB 先行规格/语义评审门/静态 lint/冒烟边界） | 每次池/注册表相关事故或特性后追加 |
| sts2-cross-pool-cards.md | StS2 用池卡普查 C01-C06：26 张四分类（唯一原生跨池=Splash）、Splash 原生池对象排除模式精读（含 Count>1 护栏与文本差异）、VisualCardPool 纯显示、移植五规则 | 源码重导出或新结案时追加 |
| invariants.md | 引擎/mod 契约与不变量目录 I4-I10（池架构卷之外）：联机状态一致性、canonical/mutable 生命周期、资产存在≠内容、本地化变量名权威、标识符静态解析坑、事件池隐式数量要求、注册时序 | 每次实机事故结案后追加 |
| semantics-review-checklist.md | 语义评审门清单（G2 固化）：P1-P8 池/注册表 + M1-M4 模型/联机提问表 + 可整段派发的 reviewer 提示词 | 不变量目录每新增一条同步加问 |
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
