# docs/ — 内部工程文档索引 / Internal engineering documentation index

本目录是 Spire1 的开发档案，**不是玩家文档**。里面是设计契约、机制考证、
审查报告与交接记录，用来回答「这个行为为什么是这样」「当时验证到哪一步」。
Most files are working records written during development; they may contain
absolute paths from the maintainer's machine and conclusions that were later
revised. When a document and the code disagree, **the code wins** — then fix
the document.

> 玩家请看仓库根目录的 `README.md`；安装、依赖与功能列表以那里和
> Workshop 页面为准。本目录面向贡献者与未来的维护者。

---

## 1. 建议阅读顺序 / Reading order

1. `../DEVELOP.md` — 权威设计与契约（角色、卡池、架构、发布约束）。
2. `../CONTRIBUTING.md` — 环境、构建、代码约定。
3. `../DEVLOG.md` — 按时间顺序的完整开发日志（最新状态在文件尾部）。
4. 本目录的 `CORE-DEVLOG.md` — 蒸馏后的知识层，比 `DEVLOG.md` 短。
5. 需要查 API 时看 `BaseLib-API.md`；查历史决策看对应的 `DEVELOP-*.md`。

命名约定 / naming:

| Pattern | Meaning |
|---|---|
| `DEVELOP-<topic>-<YYYYMMDD>.md` | 设计或行为契约，日期是定稿日 |
| `HANDOFF-*.md` / `CHECKPOINT-*.md` | 跨会话交接与断点，含“下一步做什么” |
| `RELEASE-*` / `WORKSHOP-*` | 发布门禁、打包与推送边界 |
| `DIVERGENCE-*` / `MECH-*` | 联机分歧、机制判据的专项分析 |
| `reports/<batch>/` | 某一轮工作的逐 agent 报告（`.request.md` 是任务书，同名无后缀是结果） |

---

## 2. API 参考 / API references

| File | Subject |
|---|---|
| `BaseLib-API.md` | BaseLib 的 shipped-binary 接口参考（含 SHIPPED/SOURCE-ONLY 标记与版本偏差表）。写代码先查这里。 |
| `RitsuLib-API.md` | RitsuLib v0.5.13 接口参考；该库经评估后**未采用**，保留作决策证据。 |
| `JmcModLib-API.md` | JmcModLib 1.9.0 接口参考；同样**未采用**，保留作决策证据。 |

## 3. 设计契约 / Design contracts

| File | Subject |
|---|---|
| `DEVELOP-form-mod-20260926.md` | 形态（Form）系统规格：六形态、姿态分组、双形态效果。 |
| `DEVELOP-form-playable-20260928.md` | 把形态做到“可从游戏入口游玩”的验收契约与边界。 |
| `DEVELOP-forms-independent-20261005.md` | 把 Forms 拆成独立 mod（`sts2-forms`）的硬目标与反射桥契约。 |
| `DEVELOP-events-20260923.md` | 一代事件的移除边界、`EnableSts1Events` 默认关闭的原因。 |
| `DEVELOP-hook-scan-20260924.md` | 知识库 hook 扫描器的行为契约与证据边界。 |
| `DEVELOP-menu-performance-20260923.md` | 选人背景掉帧实验层；仅由 `SPIRE1_MENU_PERFORMANCE_PROBE` 编译符号启用，Release 不含。 |
| `RELEASE-PACKAGE-CONTRACT-20261003.md` | 发布包必须从源码 allowlist 重建，不盲删 `dist`/`workshop`。 |
| `RELEASE-GATES-CHECKLIST.md` | 三道发布门禁（AssemblyRef / manifest / TypeDef），读 ECMA-335 元数据而非字符串。 |
| `WORKSHOP-PREPARATION-CONTRACT-20261005.md` | Workshop 推送前准备的授权边界（不调用 SteamCMD、不读凭据等）。 |
| `chaosbridge-design.md` | ChaosBridge 设计稿：让任意自定义角色被 AutoAnthony 自动兼容（独立 mod）。 |

## 4. 机制与专项分析 / Mechanics and investigations

| File | Subject |
|---|---|
| `DIVERGENCE-AAW-mp-20261008.md` | 联机 `StateDivergence` 全案：AutoAnthonyWatcher 生成池在两端激活状态不一致，含已确认/待验证/未复现三段。 |
| `MECH-reaperform-doom-20260926.md` | 死神形态（ReaperForm）与灾厄（DoomPower）的逐行触发判据。 |
| `pandora-watcher-pool-leak.md` | Pandora's Box 把观者原生卡泄漏进 chaos run 的类别级分析。 |
| `AFTP1-BINARY-VERIFICATION.md` | 对 Workshop 上 AFTP 真实二进制（sha256 固定）的定点验证。 |
| `KNOWLEDGE-RECHECK-20260923.md` | 对旧知识断言的重验：Havoc 的 CardType、EnergyManager.recharge 字节码实验。 |

## 5. 审查报告 / Reviews

| File | Subject |
|---|---|
| `CODE-REVIEW-20260904.md` | 批判审阅：304 张卡三方数值对比（一代 jar ↔ 引擎 ↔ 实现）、66 只怪 HP 审计、发现均带 `文件:行`。 |

逐轮的实现/审查/门禁报告在 `reports/`，共三批：

| Directory | Scope |
|---|---|
| `reports/form-playable-20260928/` | 形态可玩化的 worker/supervisor 报告、发布门禁、隔离启动矩阵。 |
| `reports/workshop-prep-20261004/` | Workshop 推送前的重建、打包、负面门禁与证据 JSON。 |
| `reports/forms-independent-20261005/` | Forms 独立化的逐轮（r4…r40）实施、审查、失败恢复记录。 |

这些是**过程证据**，不是结论摘要；单个文件可能只覆盖一轮、且可能被后续轮次推翻。

## 6. 开发日志 / Development logs

| File | Subject |
|---|---|
| `CORE-DEVLOG.md` | 蒸馏后的“服务性”知识：项目地图、引擎事实、契约，读这一份最快。 |
| `FULL-DEVLOG.md` | 全部 omp 会话的历史索引（每条：日期、标题、产出位置）。 |
| `../DEVLOG.md` | 原始时间线，信息最全；查找“某次为什么这么改”用这里。 |
| `../DEVLOG-archive.md` | 更早（2026-08 之前/初期）的日志归档。 |

## 7. 交接与断点 / Handoffs and checkpoints

| File | Subject |
|---|---|
| `HANDOFF-forms-current-20261005.md` | 形态系统在某一天的已验证状态与未验收边界（源码 HEAD 固定）。 |
| `HANDOFF-forms-next-20261006.md` | 后续会话的接续点：独立仓 commit、Beta 包哈希、已知 warning。 |
| `HANDOFF-forms-source-snapshot-20261005.json` | 上述交接引用的源码快照（机器可读）。 |
| `CHECKPOINT-workshop-prep-20261005.md` | Workshop 推送前准备的断点记录（授权范围、门禁位置）。 |
| `historical/` | 2026-08 至 09 的旧交接、工作计划与最终报告，**已冻结**，仅作历史。 |

## 8. 证据目录 / Evidence

| Directory | Contents |
|---|---|
| `evidence/` | 探针源码与原始输出：`plated-armor-*`（覆甲结算）、`aftp-shape-probe-*`（AFTP 类型形状）。用于复核文档里的断言。 |
| `reports/**/evidence-*` | 某一轮发布/门禁运行的机器可读结果（JSON、日志、哈希）。 |

---

## 9. 维护规则 / Maintenance rules

- 新契约用 `DEVELOP-<topic>-<YYYYMMDD>.md`；不要把新结论直接改写出旧日期的文件，
  除非只是修正笔误——历史结论要保留可追溯性。
- 行为变更必须同时更新 `../DEVELOP.md` 或对应契约，并在 `../DEVLOG.md` 追加一条。
- 本目录的文件不删除：过期内容移入 `historical/`，不要直接抹掉。
- 不要把玩家向说明写进这里；玩家文档在仓库根 `README.md` 与 `workshop/`。
