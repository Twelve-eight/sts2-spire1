# 今日修复批复核报告（2026-08-27）

基线：sts2-spire1 @ 5a914ac（工作区未提交 33 文件）；aftp-ActsFromThePast @ 7416aef（工作区 38 文件）。
复核方式：只读 diff 审查 + `.tmp/dllsrc`（官方反编译）与 `.tmp/baselib-dll`（BaseLib 3.4.5 反编译）交叉验证。

---

## 1. 删类联动：Vampires 引用官方 BloodVial — **正确 ✓**

- 官方 `MegaCrit.Sts2.Core.Models.Relics.BloodVial`（dllsrc）与我方删掉版（`git show HEAD:mod/Spire1Code/Relics/BloodVial.cs`）逐字段一致：Common 稀有度、`HealVar(2m)`、`AfterPlayerTurnStartLate` 钩子、`TurnNumber <= 1` 守卫、治疗 2 HP。语义零漂移。
- `mod/Spire1Code/Events/Vampires.cs:67,86` 的 `r is BloodVial` 现解析到官方类（新增 `using MegaCrit.Sts2.Core.Models.Relics;`，本地 `Spire1.Spire1Code.Relics` 命名空间已无同名类，无歧义）。AFTP 上游 Vampires 同样引用官方 BloodVial，行为一致。
- 事件链路成立：官方 BloodVial 在 `SharedRelicPool`（dllsrc SharedRelicPool.cs:26），`RelicGrabBag` 合并 Shared 池 + 角色池，SPIRE1 角色可获得血瓶 → `LoseVial` 选项可触发 → `RelicCmd.Remove` 正常。全仓 grep 确认无其他 BloodVial 引用残留。

## 2. Token 卡归档 Spire1LegacyPool（11 张）— **正确 ✓**

- 11/11 张 Token 卡（BecomeAlmighty/Beta/Expunger/FameAndFortune/Insight/LiveForever/Miracle/Omega/Safety/Smite/ThroughViolence）均带 `[Pool(typeof(Spire1LegacyPool))]`，逐一 grep 验证通过。这是全仓仅有的 11 张 `CardRarity.Token` 卡，无遗漏。
- 属性遮蔽正确：显式 `[Pool]` 覆盖基类 `Spire1Card` 上的 `[Pool(typeof(Spire1CardPool))]`（`CustomContentDictionary.AddModel` 用 `GetCustomAttribute` 取最近声明），Token 卡不再落入 Spire1CardPool。
- 发放链路不受池归属影响：`CreateCard<T>`/`AddToCombatAndPreview<T>` 走 `ModelDb.Card<T>()`，与池无关。逐一确认：Alpha→Beta（Alpha.cs:16）、Beta→Omega（Beta.cs:17）、CollectPower→Miracle（CollectPower.cs:31）、DeusExMachina→Miracle、ConjureBlade→Expunger、Wish→三选项卡、CarveReality→Smite、DeceiveReality→Safety、Evaluate/Pray/StudyPower→Insight、ReachHeaven→ThroughViolence、BattleHymnPower→Smite，全部经由 ModelDb 直取。
- Spire1LegacyPool 无角色引用、`IsShared=false`，不出现在奖励/商店/总览（`PrismaticGem`/`DingyRug` 均不会拉入它）。
- 副产品（正确方向）：DustyTome 新过滤同时把 `CardRarity.Ancient` 的 mod 卡 Bite 排除出先古奖励候选，修复了潜在的"先古书发 Bite"渗漏。

## 3. InjectTwin 稀有度漂移三卡 — **正确 ✓**

- `RarityDriftTwins = {"Bludgeon","Acrobatics","Predator"}`（SharedCardReuse.cs:235-240）与列表中 `typeof(Sts2Cards.X).Name` 精确匹配（大小写一致，三处 typeof 行 85/124/145）。
- 漂移主张经 dllsrc 反证：官方 Bludgeon=Uncommon（StS1 Rare✓漂移）、Acrobatics=Uncommon（StS1 Common✓）、Predator=Common（StS1 Uncommon✓）。三条全部属实。
- 自研类存在且直接继承 Spire1Card：Bludgeon.cs:12（Rare, 32dmg+10）、Acrobatics.cs:14（Common, 抽3弃1）、Predator.cs:15（Uncommon, 15dmg+5, 下回合抽2）。`ResolveOwnImplementation` 的 `"Spire1.Spire1Code.Cards."+name` 反射与 `BaseType?.Name == "Spire1Card"` 检查均可命中。
- 注入逻辑：漂移卡注入自研版并 `return`（shipped 版不注入，无双卡共存）；非漂移卡维持 shipped 注入；`own == null` 时回退 shipped，安全。pure 分支早退走 `AddOwnImplementations`，不受影响。数值本就与 StS1 一致，差异仅在稀有度，注入自研版即恢复 StS1 稀有度。

## 4. DustyTome 回退池过滤自研同名卡 — **正确 ✓**

- `DustyTomeAncientFallbackPatch.cs:58-63`：`Assembly.GetType("Spire1.Spire1Code.Cards." + c.GetType().Name, throwOnError: false) == null`，程序集取自本 patch 类型（Spire1 mod 程序集）✓，`throwOnError:false` ✓。
- 官方 19 张 Ancient 卡逐一比对：与自研 Cards 命名空间同名的只有 BiasedCognition / Corruption / Wish / WraithForm 四张，全部是同名同义的 StS1 忠实移植（Rare，各角色池在册），过滤即目的本身。**无误过滤**——不存在"同名但语义不同"的自研类。
- 过滤同时作用于角色池首查与回退池查询两个调用点，逻辑闭合。

## 5. Disarm DynamicVar 化 — **正确 ✓**

- `Disarm.cs`：`new DynamicVar("StrengthLoss", 2m)` 正向存储；OnPlay `PowerCmd.Apply<StrengthPower>(…, -loss, …)` 取负应用（`StrengthPower.AllowNegative=true`，dllsrc 证实负值合法）；`OnUpgrade => UpgradeValueBy(1m)` → 2→3 → 应用 -3 ✓。
- 对照官方 PiercingWail（dllsrc）：同样的 `DynamicVar("StrengthLoss", 正值)` + 文案"失去 X"惯例，模式对齐。
- loc 双语占位符 eng/zhs cards.json:41 均改为 `!StrengthLoss!`，与 var 名精确一致。渲染从"失去 -2 点力量"修正为"失去 2 点力量"。
- PowerCmd.Apply 形参（context, target, amount, applier, cardSource）与调用点匹配；`IntValue` 与官方 `BaseValue` 对整数值等价。

## 6. AFTP fork：RebalancedModeEffective + ClassicSlimedOnPlayPatch — **正确 ✓（含 2 条 P3 观察）**

- `ActsFromThePastConfig.cs:16-17`：`RunManager.Instance is { } rm && rm.NetService.Type == NetGameType.Singleplayer && RebalancedMode`。枚举值经 dllsrc 证实（None/Singleplayer/Host/Client/Replay；Host/Client/Replay 服务的 Type 分别为对应值）。
- 替换完备性：全仓 grep `RebalancedMode` 排除 `RebalancedModeEffective` 后仅剩 Config.cs 内 3 处（定义+注释+自身读取），**零裸引用残留**；76 处 Effective 调用分布 37 文件（含 Minigames/CardPatches）。
- ClassicSlimedOnPlayPatch（CardPatches.cs:100-108）：MP 守卫位于 `IsClassicSlimed.Get` 标记检查**之前**——位置正确，因为标记本身（`SpireField`→`ConditionalWeakTable`，仅创建端 set）就是分叉源；MP 下 `return true` 走原版 OnPlay，两端一致。
- P3-1（观察）：`ClassicSlimedDescriptionPatch` 未加同款守卫——远端显示原版描述、本地显示经典描述，纯 UI 文案分歧，不改游戏状态、不会 desync。可不改。
- P3-2（观察）：Replay 类型（`NetGameType.Replay`）下 Effective 恒 false——回放用 RebalancedMode 录制的局会按原版分支渲染选项文本。仅影响回放显示，不影响联机对局。
- 边界提示：`rm.NetService` 在无进行中对局时为 null。当前全部调用点（事件选项生成、minigame 设置、IsAllowed）都在对局内执行——`SecretPortal.IsAllowed` 修复前就已直接解引用 `RunManager.Instance.RunTime`，周边代码本就假设活跃对局。风险仅存在于"对局外求值事件 DynamicVars"的假想路径（如 SensoryStone.CanonicalVars 被库预览触碰），未发现实际触发点。

## 7. MP 守卫边界 — **正确 ✓**

- 单机局：`NetSingleplayerGameService.Type == Singleplayer` → `Effective == RebalancedMode` → 本地配置完全生效 ✓。
- 联机局（Host/Client）：Effective 恒 false → 任何本地配置都走原版分支 → 两端无条件一致 ✓（这正是 divergence #55/#35 的修复目标）。
- ClassicSlimedOnPlayPatch 对称：`Type != Singleplayer` → 原版；Singleplayer → 保留经典变体 ✓。

## 8. 药水删除 — **正确 ✓（1 条 P3 洁净度备注）**

- `mod/Spire1Code/Potions/` 仅剩 ExplosivePotion.cs / FearPotion.cs / Spire1Potion.cs。全仓 grep 无任何代码引用被删的 6 个药水类 ✓。
- 删除安全性：被删 6 药水（Block/Strength/Dexterity/Energy/Fire/Weak）与官方 SharedPotionPool 版本**数值逐字段一致**（12 格挡 / +2 力量 / +2 敏捷 / 2 能量 / 20 伤害 / 3 虚弱，稀有度均 Common），且全部在官方 SharedPotionPool 在册——SPIRE1 角色经 `PotionFactory.GetPotionOptions`（角色池 + Shared 池）仍可获得等价药水，功能零损失。
- 保留正确性：ExplosivePotion/FearPotion 在 dllsrc 全量搜索**无官方对应类**（mod 独有语义），必须保留 ✓。
- 孤儿键风险：eng/zhs cards.json:194-207 仍存 6 药水的 `SPIRE1-*` 键。`LocTable.MergeWith` 纯字典合并、`LocException` 只在 **missing** key 的 Get 上抛出——多余键是惰性死数据，**无任何报错路径** ✓。relics.json 同理（~40 个被删遗物键）。属可留可清的洁净度项（P3）。

## 9. 附带改动（loc 文本）— **正确 ✓**

- `characters.json` 四角色 `cardsModifierTitle/Description`：旧值是关键词表误植（"Strike/Deal damage."、"Shiv/Add a Shiv…"），新值正确描述"向卡牌奖励与商店加入 X 卡牌"。消费链路 `CharacterModel.CardsModifierTitle → CharacterCards.Title/Description → NCustomRunModifiersList/NDailyRunScreen`，语义修正成立。
- `cards.json` DualWield 双语描述与实现（手牌中选攻击/能力牌，复制入手，升级复制两份）吻合。

---

## 总评

**整体裁定：正确（无阻断性缺陷，无必须返工项）。**

8 项修复中 8 项主逻辑验证通过；三条关键主张（稀有度漂移、官方 BloodVial 等价、被删药水与官方等价）全部经反编译源逐字段证实，无臆断。

### 非阻断观察（P3，均不要求返工）
1. **旧存档兼容不对称（△ 需产品决策）**：Token 卡与同名退役卡进 Spire1LegacyPool 的明示理由是"saved runs reference their SPIRE1-* ids"（Spire1LegacyPool.cs 文档），但被删的 8 遗物 + 6 药水是硬删除——持有 `SPIRE1-ANCHOR`/`SPIRE1-BLOCK_POTION` 等的旧存档载入时模型解析会失败。若当前阶段存档可弃则无碍；若要保存档兼容，需按 LegacyPool 同法归档而非删除。
2. **Akabeko 稀有度漂移（△ 同族问题）**：StS1 Akabeko=Common，StS2 官方=Uncommon。删除自研版后 SPIRE1 角色从 Shared 池拿到的是 Uncommon 版——与本次修复的 Bludgeon 卡稀有度渗漏同族，但遗物稀有度仅影响奖励档位分布，影响很小。
3. ClassicSlimedDescriptionPatch 无 MP 守卫（纯显示分歧）；Replay 分支下 Effective 恒 false（仅回放显示）。
4. 孤儿 loc 键（药水 12 + 遗物 ~40，双语）：惰性数据，无报错路径，可择机清理。
5. Spire1LegacyPool 文档注释已不完全覆盖新成员（Token 卡并非"StS2 已同名 shipped"），纯注释滞后。

### 必须返工项清单
**（无）**
