# 工作区资源盘点 — 总索引（workspace inventory）

> 生成：2026-08-28。三卷结构：本索引 + 分卷详录。盘点对象 = G:\omp works 全部本地资产。

## 卷目

| 卷 | 覆盖 | 文件 |
|---|---|---|
| 游戏侧 mod | 本地 mods/ 7 项 + 工坊 28 订阅 + BaseLib 3.4.5 深查 + 双源优先级规则 | [inventory-mods.md](inventory-mods.md) |
| 研究资产 | research/ 全部 12 子目录 + localization/eng 姊妹目录（含决策树与数据流图） | [inventory-research.md](inventory-research.md) |

## 快速事实

- 运行态基线：godot.log 实测 35 注册 / 30 在载（RegentFX 用户禁用 + 双源去重禁 4）
- 双源规则：本地 mods/ 与工坊同名时本地胜（日志 Disabling the Steam workshop version）；例外 Mesugaki（工坊 0.1.2 胜出）与 RegentFemPortraits（工坊 v1.0 版本串解析失败）——易变状态，重盘以日志 L80-83 为准
- research/ 阅读原则：数值/引擎行为查反编译卷，项目踩坑查 kb/，历史决策查 audits/，StS1 原始真值查 sts1-kb/
- 未入卷资产：代码仓（aftp fork/upstream/stage/omp-upstream/github-opt 等）——P6-23 剩余范围，待续卷

## 维护

- mod 安装/订阅变化 → 刷新 inventory-mods
- research/ 新增子目录 → inventory-research 补节
- 判定'某资产是否还存在'勿凭记忆，直接重盘（本卷即快照）
