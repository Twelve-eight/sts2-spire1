---
name: sts2-spire1-card-audit
description: sts2-spire1（StS1→StS2 BaseLib 移植）卡牌/遗物一致性审计五步法。涉及数值仲裁、本地化占位符校验、覆盖审计、联机失同步归责时使用。事实性内容（引擎速查表、部署协议、取证 SOP、AFTP 档案）已剥离至 research/kb/。
---

# 卡牌一致性审计五步法

> 事实速查表已迁至 `research/kb/engine-facts.md`；取证/部署/管线协议在 `research/kb/debug-protocols.md`；
> AFTP 互操作与许可证档案在 `research/kb/aftp-interop.md`；一代官方数据在 `research/sts1-kb/`。

## 第一步：loc ↔ 实现双向扫描
- 占位符分域：cards/powers 用 `!X!`，events 用 `{X}`——分开扫。
- loc 键规则：`SPIRE1-<类名蛇形>` × `title/description/smartDescription` × `zhs/eng`；
  类内字符串正则必须含连字符，否则空转假绿（踩坑两次）。
- 脚本：`.tmp/audit-event-vars.js`。

## 第二步：jar 字节码仲裁
- 权威源 = `desktop-1.0.jar` 的 class 构造实参 + `localization/{eng,zhs}/*.json` 原文（单数 localization）。
- 类名≠游戏 ID（GeneticAlgorithm.class ↔ ID "Genetic Algorithm"）；大小写敏感（ThunderClap）。
- 全量双语权威数据已在 `research/sts1-kb/`——优先查 KB，KB 缺的才翻 jar。

## 第三步：引擎源码链路验证
- 快照 `.tmp/dllsrc/`；高频事实查 `research/kb/engine-facts.md`，不在表内的先读源码再下结论。

## 第四步：运行时探针日志
- `[Spire1]` 前缀探针打 OnPlay 等关键路径；日志位置与轮转陷阱见 `research/kb/debug-protocols.md`。

## 第五步：RitsuLib 转储对拍
- 先数 differ 标记再定责（假阳性案例 #563）；流程见 `research/kb/debug-protocols.md`。
