# 火堆前进本房断线分析（2026-08-27 16:02，checksum #55）

## 现象
用户报告"进火堆掉线"。实际断点=火堆前的 AFTP 复制机事件房退出时刻。

## 触发链（godot.log 逐行取证）
1. 16:01:58 两人进入 EVENT.ACTSFROMTHEPAST-DUPLICATOR
2. 朋友(76561199466878739)选 option 0 = Pray：手动选 STARDUST 复制 1 张（走 PlayerChoiceSynchronizer 网络同步选择）
3. Host 选 option index 1 = Kneel（RebalancedMode）：受 5 伤 + StableShuffle(Rng.Niche) 取 2 张已升级牌自动复制
4. 16:02:24 Exiting event room → checksum #55 分歧 → 断线

## 真实差异（divergence zip dump diff，仅 2 行）
- RELIC.BOOK_OF_FIVE_RINGS CardsAdded: local=4 vs remote=2（Host 的五环书"获得卡计数"差 2）
- RNG Niche counter: local=57 vs remote=53（Host 多消耗 4 次 Niche RNG）

## 根因
AFTP Duplicator 的 Kneel 分支联机双端不对称：
- Host 端执行 Kneel：StableShuffle 消耗本地 Niche RNG + CloneCard 2 张入组（触发五环书 +2）
- 客户端不执行该逻辑：它的 Niche 计数与五环书计数停留在 Pray/ALIGNMENT 的 2
- Pray 能同步是因为卡选择走 PlayerChoiceSynchronizer；Kneel 的自动复制不走任何网络同步原语
- RNG 错位后，后续所有 Niche 消耗永久错拍 → checksum 永久分歧 → 断线

## 归属
AFTP fork 的 MP 缺陷（SharedEvents/Duplicator.cs Kneel）。与 Spire1 无关。

## 修复方向（fork 内）
Kneel 的卡选择改为 FromDeckGeneric 同步选择原语（与 Pray 一致），或把 Kneel 的 RNG 消耗改用同步 RNG（player.RunRng.Niche 在 MP 有同步语义的话）。需对照引擎 MP RNG 同步规范。
