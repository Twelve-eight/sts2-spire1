# sts2-spire1 增量审阅报告(2026-08-30, HEAD 34947f9)

范围 = critic 审计(079281b, 2026-08-28)之后的全部增量.四路审阅:
AftpForkReview(reviewer),Spire1IncrementReview(reviewer),KbVolumeAudit(scout,
超预算停机后经 transcript 打捞由主会话续完),HandoffHonestyAudit(scout).
全部 P1 级发现经主会话独立复核.

## 结论总览

| 对象 | 结论 | 严重度 |
|---|---|---|
| AFTP fork 三提交(22e83d3/f166f11/9b4c4fb) | A/B/C 家族修复正确; family-D 有极性反转缺陷(F1) | P1(实害受部署形态屏蔽) |
| CombatSyncStallWatchPatch(观察哨) | 通过, 零行为变化, 注册路径确认 | - |
| 语言防火墙(tools + hooks + rules) | 功能面通过, hook wrapper 有 cwd 缺陷(F2) | P2 |
| KB 卷四 | 10 条核心引擎声明全部核实, 行号基本零漂移 | 通过 |
| KB 卷五 | 骨架正确, 4 处实质错误(F3) | P2(文档纠错) |
| SESSION-HANDOFF + friends-pack v4 | 诚实性通过, 1 个过期指针(F4) | P3 |

打包链事实(主会话直接验证): 部署目录与 zip 内四件二进制 md5 完全一致
(8d510cee/aae4930e/317ad034/ba60133a), character.txt=all, 无 PDB 残留.

## F1 (P1) fork 9b4c4fb family-D 修复极性反转

发现: AftpForkReview; 独立复核: 主会话.

机制: 两个 allow-flag 包装器(ActsFromThePastConfig.cs L42-46)MP 下恒返回
false, 调用点(ShrinePatches.cs L60-64)在逻辑非下消费----MP 下过滤器恒触发:
一代幕总移除全部 base-game shared 事件, 官方幕总移除全部 mod shared 事件.
与提交信息声称的"MP 保持原版未过滤 concat"相反.

对称性: 双端同值同过滤, 池一致, 无 desync----是 MP 内容回归, 非分歧.

实害评估: 实际部署为 Spire1+AFTP 双 mod 同装(friends-pack v4).Spire1 侧
LegacyActSharedEventFilterPatch 无条件删一代幕全部官方 shared 事件
(STALLW1 日志 L2195-2196 实锤), 两 postfix 交集 = 全删, SP/MP 最终态一致,
无玩家可见差异.实害仅限 fork 单独安装(不带 Spire1)的 MP 局.

修复(fork 上游正确性): 两包装器 MP 下改返 true, 或调用点 Phase-1 过滤块
整体加 SP 判定.另记: SecretPortal.IsAllowed 以 RunTime 墙钟为门
(SecretPortal.cs L31), 双端独立求值可翻转资格----超范围待查.

其余核验: 裸配置读零残留(5 键全 Effective 化, 调用点仅在声明与包装器体内);
判别式与引擎同构, NetService 在 GenerateRooms 前定型(RunManager.cs L470
vs L343); CleanUp 不置空 NetService; 单机无行为回归(Replay 调试屏除外,
无生产影响).

## F2 (P2) hook wrapper 的 cwd 相对路径依赖

发现: Spire1IncrementReview.

.cursor/hooks/check-agent-text.mjs 以 process.cwd() 调 tools/ 相对路径.
若 hook 以非项目根为 cwd 启动, checker 不可达, 走 catch 分支 deny----
fail-closed 不破安全性, 但防火墙整体瘫痪(所有 Task/MCP 调用永久拒绝).
修复: wrapper 以 import.meta.url 递推定位 checker, 不信任 cwd.

reviewer 同时确认: matcher "Task|MCP:.*" 覆盖正确; 字母表与策略一致;
观察哨 Postfix 包装正确(async 语义/异常传播/单机不误报/属性扫描必达),
STALLW1 日志复核零 Spire1 ERROR/WARN,零 stall 告警,Victory.

## F3 (P2) KB 卷五 4 处实质错误(打捞自 KbVolumeAudit transcript)

KbVolumeAudit 完成 95% 核查后被停; 主会话从其 transcript 提取 10 个
思考块(151KB, ASCII 清洗后存 .tmp/kbaudit-salvage-ascii.txt), 逐条采信
并复核.卷四 10 条高风险声明全部 MATCH(V4-R6 无界 await/事件 RNG 派生式
EventModel.cs L234/Player.cs L330/RewardsSet 无限期缓冲 L256 L281/
GenerateRooms 对称 RunManager.cs L328-344/IsShared=true 恰 8 个/
checksum 次数一致 ChecksumTracker.cs L83-84/RNG 三层表/M1-M7).

卷五实质错误:
1. FlavorSynchronizer 描述错: 卷称管"遗物风味文本每端独立 roll"; 实际
   处理 EndTurnPingMessage/MapPingMessage(游戏手感类消息), 遗物风味是
   RelicModel.Flavor 纯 LocString 查表, 无 roll.
2. ReactionSynchronizer 描述错: 卷称管"遗物触发动画 flash 同步"; 实际
   处理 ReactionMessage(光标处表情/反应轮).
3. 11-syncer 家族表不完整且归属错: 表称"全目录"但漏 RewardSynchronizer/
   EventCombatSynchronizer/ActChangeSynchronizer 三个真实 syncer;
   ActionQueueSynchronizer 实际在 MegaCrit.Sts2.Core.GameActions.Multiplayer/,
   不在表头声称的 Multiplayer.Game/ 目录; CombatStateSynchronizer 也在
   父目录.
4. VoteForMapCoordMessage 不存在: 实为 VoteForMapCoordAction 经
   ActionQueueSynchronizer 入队(NMapScreen.cs L947-948), 非直接消息.

细微修正(卷四): client 完成 sync 除收齐 SyncPlayerDataMessage 外还需
host 的 SyncRngMessage(CheckSyncCompleted 要求 _rngSet != null)----卷四
简化表述漏了此附加挂起路径; 宝箱房断线无 OnPeerDisconnected 释放,
"三姐妹唯一释放=断线"的表述对 treasure 情形不准确; M8 不存在(卷内实际
M1-M6 表+M7 补记); ChooseLocalOption 引用行号 L246-254 有小漂移
(实际 L230-263).OneOffSynchronizer 行数 210 与实际(~232)小漂移;
"互斥"定性不准(实际是跨端执行一次性动作: 商店删卡/开箱金币/水晶球奖励).

处置: 卷五 4 处改正 + 卷四 3 处细化, 一次性小修.

## F4 (P3) handoff 过期指针

发现: HandoffHonestyAudit.

- SESSION-HANDOFF L37"最新黑屏日志 godot.log"已过期: 证据已轮转至
  godot2026-08-29T15.51.05.log; 当前 godot.log 是 STALLW1 冒烟.
- README v4 已落实 critic #1(未定态表述+验证清单)与 #15(轻量安装);
  zip 内与 dist/friends-pack/ 暂存副本字节一致.
- handoff 无虚假验证时态; 未清项与 WORKPLAN P1/DEVLOG session 22 一致.
- DEVELOP.md 仍带旧计数(305 卡/33 遗物, 实际 306/25), 一并同步.

## 处置清单(按优先级)

1. F1 fork 修极性(不阻塞当前 friends-pack; 修后重建 dll 重打包)
2. F2 wrapper 定位修复(import.meta.url)
3. F3 卷五 4 处纠错 + 卷四 3 处细化
4. F4 handoff 指针改轮转文件名; DEVELOP.md 计数同步
5. 派发惯例: 任务书加硬规则----禁止逐字引用源文本, 引用一律 file:line +
   英文转述; 全角标点注释的源码只读不回显.本次三起供应商掐断(Spire1
   reviewer 1 起,KbVolumeAudit 2 起)均死于回显, 非读取.

## 审阅过程记录

- AftpForkReview: 完成 1h15m, P1 发现已独立复核坐实.
- Spire1IncrementReview: 生成中回显 em-dash/箭头被掐(finish_reason
  sensitive), 唤醒后按英文 ASCII 转述纪律交付, F2 坐实.
- KbVolumeAudit: 155 请求超预算停机, 恢复两次均因上下文含 KB 全角标点
  再触过滤; 处决后经 transcript 打捞(151KB 思考块, ASCII 清洗), 核查
  结论完整采信, F3 坐实.
- HandoffHonestyAudit: 完成 19m, 无虚假声明.
- 主会话独立验证: 打包哈希四件套+character.txt; 观察哨引擎源三个
  WaitForSync 调用点; Effective 判别式时序(RunManager L335/L343/L470);
  IsShared=true 8 个清单; EventModel L234/Player L330/RewardsSet L256;
  EpochModel/Act4 错误为 DEVLOG L701 已知良性项(非新发现).
