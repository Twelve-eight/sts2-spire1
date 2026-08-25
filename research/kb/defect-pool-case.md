# 案例档案：故障机器人卡牌池贫瘠 Bug（已修复 8781855）

> 结案时间：2026-08-25。本档案为全案报告存档，供未来同类"池异常"问题参照。
> 关联：审计五步法 skill、coverage.js、PoolCensus 探针。

## 一、症状（2026-08-23/24 实机暴露）

SPIRE1-DEFECT 卡牌奖励池异常贫瘠：
- 一局仅遇寥寥数张自有卡；57 张一代同款官方卡从未出现在奖励中
- 12 张应服役的一代实现卡被错误退役
- 事件 ROOM_FULL_OF_CHEESE（要求 8 张不同普通卡）无法满足

## 二、根因（三层叠加）

| 层 | 机制 | 出处 |
|---|---|---|
| 冻结时机 | `ModHelper.AddModelToPool` 在池首次生成时冻结全部 modded 内容，之后追加抛异常。注入必须先于引擎初始化完成 | SharedCardReuse.cs 类注释；DEVELOP.md 7a |
| 复用缺失 | LEAN-CODE 规则要求同名同数值卡复用官方模型，但初版 DefectReuse 数组为空——机器人池一条复用都没有 | 8781855 前 SharedCardReuse.cs |
| 退役误标 | 早期整批移入 Spire1LegacyPool 的 12 张卡实际仍在役语义，白白退出循环 | 8781855 diff：12 类各 -2 行旧标记 |

放大器：`GetPossibleCards` 无拥有去重（贫池观感更糟）；ROOM_FULL_OF_CHEESE 硬性 8 张普通卡需求。

## 三、修复内容（8781855）

1. DefectReuse 注入 57 张官方孪生（逐字段核对 cost/升级差/关键词）
2. 12 张误退役卡恢复现役（Caltrops/Clash/ConserveBattery/Corruption/Distraction/DualWield/Entrench/HelloWorld/Outmaneuver/Rebound/RipAndTear/Stack）
3. ColorlessReuse 通道新增
4. PoolCensus 探针（四池终态稀有度分布打印）防回归
5. 顺带清理被替代的 ShopPurchaseGuardPatch 96 行死代码

## 四、前后对比

| 指标 | 前 | 后（154 局 drain 终态） |
|---|---|---|
| 自有池 C/U/R | 3 / 18 / 6 | 13 / 43 / 12 |
| 覆盖矩阵 | 51/58 停滞 | **63/63 ✅** |

覆盖口径：coverage.js 双 id 记账（我方 id 与复用通道原版 id 均计命中）。

## 五、关联与辨析

- **GA 池归属是独立 bug**：漏挂 `[Pool]` 继承铁甲池，af6d1d7 修复；勿与本案混淆。
- **已知残留**：(官方)ThunderClap 154 局 0 次出现=RNG 缺口，queue 文件持续追踪。
- **未解之谜（低危）**：PoolCensus 启动探针行未见于任何归档日志（初始化期文件 sink 未挂载之疑）；功能不受影响——池内容已由 play 日志独立证实。后续若需启动期池证据，改用 GD.Print 直写或落盘文件探针。

## 六、预防机制沉淀

三道闸：PoolCensus 启动探针 → coverage.js 终态矩阵（双 id 记账）→ RitsuLib divergence 对拍。
方法论全文见 `.omp/skills/sts2-spire1-card-audit/SKILL.md`。
