# 语义评审门清单（Semantics Review Gate）— sts2-spire1 知识库

> 目的：把 `pool-architecture.md` G2 固化为**可直接派发的评审提示**。适用对象：任何涉及卡池/注册表/跨角色取牌/资产管线/本地化变量的改动（新卡、新角色、池操作、兼容桥、事件奖励）。使用方式：主会话在合入前逐条自检，或把"评审提示词"原样派给 reviewer 角色子代理（提示词在 §3，允许整段复制）。每条都给出"问题→要看的证据→放行标准"。
> 维护：不变量目录（`invariants.md`）每新增一条，本清单同步加一问。

---

## 1. 池与注册表（对照 pool-architecture.md I1-I3）

| # | 提问 | 证据要求 | 放行标准 |
|---|---|---|---|
| P1 | 候选集是**按 Id 集合差**定义的吗？ | 新改动的 OnPlay/生成逻辑中不得出现 `Remove(...CardPool)` / `Where(p != owner.CardPool)` 类池对象排除；应有"全集 − 可调用集合(按 Id)"的字面实现 | 有集合差实现，或改动不涉及候选集 |
| P2 | "当前角色可调用集合"包含 SharedCardReuse/chaos 注入的卡吗？ | 排除集来源是运行时归属查询（AllCardIds/实测池内容），不是硬编码卡表 | 注入卡被正确排除 |
| P3 | 池数量护栏（如 `Count > 1`）在多角色生态下的语义被显式定义了吗？ | 规格文本或注释写明单池/多池/全 mod 环境的行为 | 有显式决策，非复制 vanilla |
| P4 | 每张新卡类有可解析的 `[Pool]` 归属吗？ | `node tools/pool-audit.mjs` 退出码 0；新增类不得依赖"基类链恰好指向正确池" | lint 绿 + 孤儿数为 0 |
| P5 | 池容量契约满足吗？ | 新角色/新池统计各稀有度数量（PoolCensus 日志或 coverage.js）；Common ≥ 8 | 事件类契约可满足 |
| P6 | 第三方角色可见性：改动是否行走 AllCharacters/AllCards/池枚举？ | 枚举点列出；对未被 patch 的注册表（vanilla 硬编码 5 数组）是否足够 | 明确回答"能看到谁、看不到谁" |
| P7 | 注册时序：AddModelToPool/池首次枚举的先后有保证吗？ | 注册发生在 mod 初始化/内容装载期，不晚于任何 AllCards 访问；冻结后调用会抛异常（I0b+） | 时序有显式保证 |
| P8 | 跨池重复卡：本次改动是否增加"同 Id 多池"或消费"池颜色=独占"假设？ | duplicate-cards 报告更新；消费方审计 | 无新增隐性依赖 |

## 2. 模型生命周期与联机（对照 invariants.md I4/I5）

| # | 提问 | 证据要求 | 放行标准 |
|---|---|---|---|
| M1 | 是否直接使用 canonical 模型做战斗操作？ | Obtain/CloneCard/入堆前必须 `ToMutable()`；canonical 直传 = "used in incorrect place" | 无 canonical 直用 |
| M2 | 联机状态：改动是否引入"单侧生效"的牌堆/状态变更？ | 抽牌/弃牌/入堆走 Cmd 层（同步分发），不是本地直改 pile；对照粘液失同步验尸的教训 | 无绕过同步层的写入 |
| M3 | RNG 流分账正确吗？ | 新随机使用明确命名流（CombatCardGeneration/CombatTargets/MonsterAi/…），不借用他流 | 流名在规格中出现 |
| M4 | 异步 task 被丢弃时异常可见吗？ | fire-and-forget 必须 ContinueWith/RunSafely 记录 | 无静默吞异常 |

## 3. 可直接派发的评审提示词（reviewer 角色用）

```
你在做语义评审（不是代码风格评审）。仓库：G:\omp works\sts2-spire1。
先读 research/kb/pool-architecture.md（不变量 I1-I3）、research/kb/invariants.md、
research/kb/semantics-review-checklist.md（本文件）。
然后对改动 <文件列表或 diff 范围> 逐条回答上面 §1 P1-P8 与 §2 M1-M4 的"提问"，
每条给出：结论（通过/不通过/不适用）+ 指向具体代码行/KB 规则号的证据。
特别规则：
- 你要扮演"读懂游戏怎么玩的人"，不是编译器：任何"机制对第三方角色/复用卡/多角色
  环境的行为"与卡面文案或常识玩法预期不符的，都算不通过（参考案例：DEVLOG 修复 #10
  Splash 候选集、Session 25 AutoAnthony 失效——两案在机械测试下全绿）。
- 冒烟测试全绿不能作为放行依据（G4 边界声明）。
- 输出格式：逐条编号结论 + 一个总体 verdict（approve / request-changes + 最小修复清单）。
```

## 4. 维护规则

- 每次实机事故结案后：DEVLOG 记录 → `invariants.md` 增加不变量条目 → 本清单加对应提问（三处缺一不可）。
- 本清单是评审提示的**唯一权威副本**；子代理提示只引用路径，不复制内容（防漂移）。
