# StS2 联机测试监视 — 最终总结

监视窗口：2026-08-27 15:41 – 17:07（游戏已于 17:02 退出，测试结束）
共捕获并分析 5 个分歧包（另 1 个 08-24 旧包按已知跳过），全部落简报于本目录（1-142407.md ~ 5-160224.md）。
环境：RitsuLib 0.5.15 / BaseLib v3.4.5 / Host 76561199033460852 ↔ Remote 76561199466878739；两端 mod 集合不同（34 vs 29）但差异均为 non-gameplay（皮肤/UI），非分歧来源。涉及 gameplay 的共同 mod：Acts from the Past 1.0.5 + ActsFromThePastMultiplayerBalance 0.0.1、typing 0.0.6、MultiplayerLimitBreak 0.2.7 等。

## 三大 bug 家族

### 家族 A：Slimed 打出后 Hand/Draw 归属错位（已知 ClassicSlimed bug 的实测形态）
- 实例：#28（14:24，简报1）、#29（紧随 #28，日志内连续分歧）、#286（14:36，简报3）——3 次打 Slimed 3 次分歧。
- 触发：铁甲打出 CARD.SLIMED（PlayCardAction finished execution）。
- 真实差异（dump diff）：仅 2 行——一张牌在 Host 端留 Draw 堆顶 / Remote 端进 Hand（牌堆总数两端相同，只错位一张）；其余 HP/力量/遗物全一致。#286 后断线。
- 根因：与已知 AFTP ClassicSlimed 联机标记丢失（IsClassicSlimed 只在创建端打、网络重建端丢失）直接吻合——Slimed 的 Exhaust→抽牌路径双端不同序。
- 定位：AFTP mod 的 ClassicSlimed 补丁。

### 家族 B：ActsFromThePastMultiplayerBalance 平衡补丁只在 Host 端生效（本次新发现，决定性证据）
- 实例：#55（16:02，简报5）DUPLICATOR 事件；#35（14:26，简报2）SHINING_LIGHT 事件强烈疑似同源。
- 决定性证据（两端 debug log 对比）：DUPLICATOR 事件对同一玩家，Host 端选项页为 `INITIAL_REBALANCED`（选项1=KNEEL 跪下：失去生命+复制2张升级卡），Remote 端为原版 `INITIAL`（选项1=LEAVE 离开：无效果）。同一选项索引在两端执行不同动作。
- 真实差异：BOOK_OF_FIVE_RINGS CardsAdded=4(host) vs 2(remote)；Niche RNG Counter 57 vs 53（差4次）。#35 SHINING_LIGHT 同样 Niche 差 4 次（32 vs 28）——模式吻合，建议将 #35 主嫌修正为本家族。
- 两端 mod 版本完全一致（AFTP 1.0.5 + Balance 0.0.1），排除版本不同步；是补丁应用条件/加载在客户端失效。
- 结果：两次均断线。
- 规避：联机时两端同时禁用 ActsFromThePastMultiplayerBalance 即可验证并绕过。

### 家族 C：DARV 事件 × 尘封魔典(DustyTome) 奖励错位（已知问题的实测形态）
- 实例：#558（15:00，简报4）。
- 触发：Exiting event room EVENT.DARV。
- 真实差异：储君 floor 32 事件奖励——Host=[天鹅绒颈圈+贤者之石]，Remote=[贤者之石+**第二个尘封魔典**(AncientCard=封印王座)]。Remote 端 DustyTome 被复制/再次发放并顶掉 VELVET_CHOKER。铁甲的同层奖励两端一致。
- 根因：DARV 奖励结算与储君已有 DustyTome 的先古卡逻辑交互，导致 relic grab bag 消费顺序双端错位（池内容本身一致）。
- 结果：断线。
- 附注：DARV_EPOCH obtainDate 两端相差 23 天（各自玩家进度），若 DARV 逻辑读取该日期分支，也可能是不对称来源之一。

## 其他观察（非分歧）
- #286、#558、#55 三个分歧包对应日志中均有 `Disconnecting peer, reason: StateDivergence`——每次分歧均以断线告终。
- 常驻无害错误：SpineClickInteractor.cs 类找不到（角色选择界面）、necrobindertexiao.tscn 加载失败（皮肤）、Invalid Task ID（Godot worker 池，退出期）。均与分歧无关。
- 16:41–17:02 期间仅 RitsuLib Workshop 自动检查日志；17:02 游戏重启一次（回主菜单）后 17:05 左右退出。

## 汇总表

| # | 时间 | checksum | 触发 | 家族 | 断线 |
|---|------|----------|------|------|------|
| 1 | 14:24:07 | 28 | 打出 Slimed | A | (随后#29) |
| 2 | 14:26:27 | 35 | 退出 SHINING_LIGHT | B(疑) | 否(未及) |
| 3 | 14:36:56 | 286 | 打出 Slimed | A | 是 |
| 4 | 15:00:02 | 558 | 退出 DARV | C | 是 |
| 5 | 16:02:24 | 55 | 退出 DUPLICATOR | B(确证) | 是 |

解包档案：G:/tmp/watch-div-1 ~ watch-div-5（勿删，含两端 debug log 全量）。
