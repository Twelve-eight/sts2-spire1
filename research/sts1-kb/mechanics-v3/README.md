# mechanics-v3 — StS2 多人机制卷（per-player-view 系列）

> 本目录承载从 sts2.dll v0.111.0 反编译源拆解出的**多人同步机制**知识。
> 与 mechanics/（一代机制，编号 R 规则）区分：这里只放二代引擎多人行为的拆解。

## 卷目

| 文件 | 主题 |
|---|---|
| [per-player-view-and-mp-divergence.md](per-player-view-and-mp-divergence.md) | 引擎如何合法地让每个玩家看到不同选项/奖励；mod 侵入模式 M1-M8；预防 checklist；卡死（黑屏）充要条件 V4-R6；断线家族 A-D 根因映射 |
| [room-synchronizers.md](room-synchronizers.md) | 同步器族谱总表（11 个）；火堆/宝箱/地图投票各自的合法分歧面；死等三姐妹；RelicGrabBag 共享抓包 Front/Back 方向语义；规则 V5-R1..R5 |

## 使用方式

- 写任何触碰事件/奖励/RNG 的 mod 代码前：过一遍卷内 §7 checklist。
- 联机出现黑屏/断线：先查 §4 卡死语义与 §6 模式表对号入座。
- 证据锚点在文末索引，全部指向 research/engine-dllsrc/ 可复核。
