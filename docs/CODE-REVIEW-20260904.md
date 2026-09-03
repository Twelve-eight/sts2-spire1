# CODE-REVIEW-20260904 — sts2-spire1 批判审阅 (in progress)

> 审阅方式:主会话单线(平台并发限制禁用并行子代理)+ 两片由子代理完成
> (interop-patches、cards A-F),主会话已独立复核其 P0 级发现。
> HEAD 9453ebc。审阅进行中,本文件随审阅推进持续追加,完成后作为最终报告。

## 发现索引

### R1 [P1] AFTP 生态:三幕 boss 零奖励,与一代体验不符(用户指定审查项)
**结论**:装 Act4Heart 时,第三幕 boss 战后不发放金币/卡牌奖励(标准 boss 奖励
= 金币 + 药水 roll + 3 张卡),与 StS1"三幕 boss 掉金币+卡+boss 箱"不符。
**因果链(全部代码实证)**:
1. 引擎发奖口 `.tmp/dllsrc/MegaCrit.Sts2.Core.Rewards/RewardsSet.cs:88-91`:
   boss 房间且 `CurrentActIndex >= Acts.Count - 1`(终幕)→ 直接 `return this`
   (空奖励集),不进金/卡/药水生成。
2. 标准 boss 奖励构成(非终幕时)同文件 :238-241:`GoldReward + RollForPotion + CardReward(3)`。
3. Act4Heart(workshop 3747537811)`Act4Hooks.FixAct3Boss_IL_`
   (.tmp/act4heart/Act4Heart.Hooks/Act4Hooks.cs:118-142)对三个方法做同一 IL 改写:
   匹配 `get_Acts; get_Count; ldc.i4.1; sub` 把常量 1 改成 2,即 `Count-1` → `Count-2`。
   三个目标:`RunManager.GenerateRooms`、`RewardsSet.WithRewardsFromRoom`、
   `AmethystAubergine.TryModifyRewards`。
   - MoveType 语义实证:游戏自带 0Harmony.dll 内嵌 MonoMod.Cil,
     `enum MoveType { Before=0, AfterLabel=1, After=2 }` → `(MoveType)2` = After,
     游标落在 `Sub` 之后,`val.Prev` = `Ldc_I4_1`,改写精确无误伤。
4. Act4Heart 的 `InsertAct4_IL_ModelDb_acts`(同文件 :221-249)把 TheEnding 无条件
   追加进全局幕表;Toggler2 对"已注册但未配置的槽位"均匀随机(本仓 DEVELOP.md §7e),
   第 4 槽只有 TheEnding → 生态栈开局 `RunState.Acts.Count = 4`。
   旁证:GeneralHooks.CheckKeysBeforeAdvanceAct 读 `state.Acts[CurrentActIndex+1] is
   TheEnding`(三幕门禁时第四幕已在 run 幕表);EnsureAct4_After_FromSerializable
   只为旧存档补插(:262-277)。
5. 于是三幕 boss(index 2)落入 `2 >= 4-2=2` → 空奖励。**与玩家是否集齐钥匙无关**
   (钥匙只在 EnterNextAct 门禁检查,KeyDoor/GeneralHooks.cs:56-88)。
6. Act4Heart 全栈无任何补发钩子(全量 grep 证实:唯一 WithRewardsFromRoom 触点即
   该 flip;AmethystAubergine.TryModifyRewards 的 flip 只是放宽了该圣遗物金币加成的
   终幕豁免,需玩家持有)。
**根因定性**:作者对 `GenerateRooms`/`TryModifyRewards` 的改写方向正确
(让三幕按"非终幕"处理,生成通往四幕的门);但同一改写套在发奖口上方向相反——
原条件"终幕 boss 不发奖",改写后"倒数第二幕 boss 也不发奖"。本意"三幕不再是终幕",
实际"三幕也算终幕"。
**修复设计(待审阅完成后实施,进 Spire1 dll,遵守"生态补丁进本仓"指令 §0)**:
`RewardsSet.WithRewardsFromRoom` postfix:当 `room.RoomType == RoomType.Boss` 且
`CurrentActIndex == Acts.Count - 2`(倒数第二幕)且 `Acts[CurrentActIndex+1]` 是第四幕
(AFTP 生态 = Act4Heart 的 TheEnding;本仓 fallback = 自研 TheEnding)时,
按引擎原始构成补发 `GoldReward + RollForPotionAndAddTo + CardReward(3)`
(私有成员经 AccessTools 调用;与 :238-241 逐项一致)。
- 纯原版/AFTP-only(Count=3)零影响(条件不触发)。
- 四幕 boss(index 3 = Count-1)不触发,心脏保持无奖励。
- 逐玩家 RewardsSet,MP 侧天然按玩家实例;发奖走引擎标准 Reward 类型,同步管线不变。
- 待办:实机冒烟需覆盖(a)有钥匙三幕 boss 后有奖励屏→进四幕;(b)无钥匙三幕 boss
  后有奖励屏→胜利结算;(c)纯 AFTP 三幕回归不变。
**备注**:AFTP-only 三幕局(不装 Act4Heart)三幕 boss 同样空奖励(引擎终幕语义),
一代玩家会感知差异,但该场景 run 即结束,影响小;本修复按用户需求范围
("可进第四层时")只处理四幕在列情形。

### R2 [P0] Interop/Pandora 修复:池内容补丁落到基类 getter,变全局补丁
来源:子代理 interop-patches 切片;主会话已独立复核关键证据,成立。
- `AutoAnthonyCompatBridge.cs:185-188` 对 `WatcherCardPool` 类型 patch `AllCards`
  /`AllCardIds`;工坊 Watcher 反编译源 `.tmp/watchermod/WatcherMod/WatcherCardPool.cs`
  中 `public sealed class WatcherCardPool : CardPoolModel`,**未重声明**这两个属性
  → Harmony `AccessTools.Property` 经基类解析,补丁实际落在
  `CardPoolModel.get_AllCards/get_AllCardIds`,对**所有卡池**全局生效。
- 两个补丁方法(`ThirdPartyPoolContentsPrefix/ThirdPartyPoolIdsPostfix`,
  :212-253)均无 `__instance` 参数与类型守卫;混沌局激活时全部池的 AllCards 被换成
  混沌无色卡、AllCardIds 被并集污染 → 非混沌卡 `CardModel.Pool` 反查失配
  → `InvalidProgramException` 或池身份错配;并与 AA 在同一基类 getter 双写,
  结果取决于加载顺序。
- 对照 AA 自身 `ColorlessPoolContentsPatch`(带 `CardPoolModel __instance` +
  `is ColorlessCardPool` 分派),修复方向:补丁加 `__instance` 守卫,或改 patch
  `WatcherCardPool.GenerateAllCards`(工坊池真实重写的成员)。
- 该修复(e40db70)的"结构性验证"不可能发现此问题——冒烟只覆盖了混沌卡路径。
  PDB2805 冒烟"0 异常"与"直接命中 Pandora 种子未取得"一致:未实测到非混沌卡
  经 transform 的路径。

### R3 [P0] Interop/存档:FromSave/FromHistoryPostfix 用错字典,工坊观者存档加载必抛
来源:子代理 interop-patches 切片;行号证据成立(91-101 TryMap 查两字典;
170 WATCHER 只注册进 ThirdPartyEntryMap;323/333 回查 EntryMap)。
`AutoAnthonyCompatBridge.cs:323`(`FromSavePostfix`)与 `:333`(`FromHistoryPostfix`):
`.Where(e => TryMap(e, out _))` 过滤通过后 `.Select(e => (GeneratedCharacter)EntryMap[e!])`
——"WATCHER" 仅存在于 `ThirdPartyEntryMap`,回查 `EntryMap` 抛
KeyNotFoundException → 存档/历史加载路径直接失败。
修复:用 TryMap 的 out 值,或两字典依序取值。

### R4 [P1] 卡牌 A-F 切片(子代理,主会话抽验后并入)
- 无色共享池四张稀有度错标(StS1=Uncommon,代码=Common):
  `BandageUp.cs:12`、`Blind.cs:16`、`Finesse.cs:12`、`FlashOfSteel.cs:12`。
  根因:duplicate-cards-report.md 只对比了 mod vs StS2(同名卡),无色池四张
  StS2 不发行,漏比 StS1 数据 → 稀有度沿用默认 Common。
- `Brutality.cs:10`:Uncommon 应为 Rare(红卡)。
- `Evaluate.cs:22`:Evaluate+ 应塞入 Insight+;引擎 `CombatState.CreateCard<T>`
  不继承升级态 → 实际塞未升级 Insight(升级文案失真)。
- `Fission.cs:24-45`:整卡错译——给了 Focus、漏抽牌、升级"每球翻倍"系臆造
  (权威数据:逐球 1 能量+抽 1;升级仅 Remove→Evoke);且 :32-37 绕过命令 API
  直接改球队列(违反契约 §5.3),本地化文案与代码互相矛盾。
- P2:Burst 多 Exhaust 关键词(legacy 池)、Burn 未实现 StS1 升级(+2/4 伤)、
  Discipline 标题字面 "DEPRECATED Discipline" 对玩家可见且弃用原因无记载。
- P3:7 张自建同名卡 doc comment 缺差异字段说明(违反 §7a 记录要求);
  FTL/Finisher 的"本回合已打出自身与否"口径 UNCONFIRMED 待 jar 核对;
  DarkShackles 稀有度(legacy);BecomeAlmighty/FameAndFortune 选项卡 type 调整已注释;
  Spire1LegacyPool 注释与 Strike/Defend 变体实际仍在活跃池矛盾。
- 总体:140 卡全量读完,128 张数值全对;§7a 已知陷阱(Expertise/Claw/BiasedCognition/
  CalculatedGamble/Chill/Darkness)全部处理正确;X 费/空堆/0 层守卫到位;无 NRE。

## 已知未决项(历史,本次未重复计)
H-4 死开关、M-3 PureWater、M-4 MarkOfPain、L-1 反射大小写、Girya 休息选项、
Pandora 直接命中种子验证、NoBlockFromCards 频率分析。

## 切片进度
- [x] interop-patches(子代理)→ R2/R3 + P2 异常隔离 + P3×5
- [x] cards A-F(子代理)→ R4
- [x] act-transition 奖励维度(主会话)→ R1
- [ ] infra(MainFile/Config/Character/池)— 主会话
- [ ] cards G-Z — 主会话(脚本化保真对比+抽样)
- [ ] powers + relics — 主会话
- [ ] monsters ×2 — 主会话(脚本化+状态机精读)
- [ ] encounters + acts — 主会话
- [ ] events — 主会话
