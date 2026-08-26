# 引擎 API 事实重验报告（adversarial re-verification · engine domain）

- 日期：2026-08-26　验证域：DEVLOG.md Sessions 4-7「Engine facts」段 + §6.3 + §7.4 + Phase3-4 + §9.3
- 方法：不信任任何已写下结论，逐条对照当前磁盘上的硬证据——`.tmp/dllsrc/`（StS2 v0.111.0 反编译）与 `.tmp/baselib-dll/`（BaseLib 3.4.5 反编译），并抽查 `mod/Spire1Code/` 现状代码。只读，未修改任何代码文件。
- 裁定口径：✅ 验证成立 / ❌ 验证失败（附反证）/ ⚠️ 无法验证或部分成立（说明缺口）。所有行号为本次实测行号。

---

## 一、Session 4 Engine facts（DEVLOG L33、L36-46）

### A1 [L33] `RelicRarity.Event` 是池排除机制 —— ✅
**声明**：RelicFactory.RollRarity 只会返回 Common/Uncommon/Rare，Event 遗物永远不会从宝箱/商店随机池抽出。
**证据**：`.tmp/dllsrc/MegaCrit.Sts2.Core.Factories/RelicFactory.cs:85-94`
```csharp
float num = rng.NextFloat();
return (num < 0.5f) ? RelicRarity.Common : ((!(num < 0.83f)) ? RelicRarity.Rare : RelicRarity.Uncommon);
```
无 Event 分支；:80-83 `PullNextRelicFromBack(Player)` 默认即调 `RollRarity(player)`。（唯一旁路是 TestRngInjector 测试覆盖，不影响运行时结论。）

### A2 [L37] TryModifyRewards 签名与语义 —— ✅
**证据**：
- `AbstractModel.cs:2140`（行号精确）：`public virtual bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)`；doc-comment 明确 room 为 null 表示非房间完成来源（事件/拾取）→「同时拿到奖励表与所属房间」成立。
- `PlayerCmd.cs:144`（精确）：`amount = Hook.ModifyGoldGained(runState, ..., amount, player, ...)` 位于 GainGold 内 → 「ModifyGoldGained 对每一次 GainGold 都触发」成立。
- 配对钩子过滤链完整：`Hook.ModifyRewards`（Hook.cs:1990-2007）只把返回 true 的模型收进 modifiers → `RewardsSet.cs:144` `await Hook.AfterModifyingRewards(Player.RunState, modifiers)` → `Hook.cs:841-850` 只对 modifiers 列表成员回调 `AfterModifyingRewards()`。`AbstractModel.cs:966`（精确）。
- Shipped 先例存在：`.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Modifiers/Midas.cs:12-19` 确以 `new GoldReward(goldReward.Amount * 2, player)` 重写金额（注：它挂在 TryModifyRewardsLate 上，「先例」成立，挂哪个变体是使用者选择）。

### A3 [L38] AfterGoldGained 零收益不触发 —— ✅
**证据**：`PlayerCmd.cs:146-149` `if (!(amount > 0m)) { return; }` 早退先于 :169 `await Hook.AfterGoldGained(runState, player);`；钩子声明 `AbstractModel.cs:767`（精确）。两处行号均命中。

### A4 [L39] ModifyCardPlayCount / AfterModifyingCardPlayCount —— ✅
**证据**：`AbstractModel.cs:1495` `(CardModel card, Creature? target, int playCount)`、`:851` `(CardModel card)` —— 签名与两个行号全部精确命中。

### A5 [L40] GetResolved 的 X-cost 语义与时机 —— ✅
**证据**：
- `CardEnergyCost.cs:155-162`（精确）：`public int GetResolved() { if (CostsX) { return CapturedXValue; } return Math.Max(0, GetWithModifiers(CostModifiers.All)); }`
- `CardEnergyCost.cs:105-108`（精确）：GetWithModifiers 对 CostsX 早退返回原始 `_base`（此前还有 IsCanonical 与 `_base < 0` 两个早退）→「GetWithModifiers(All) 单独用于 X 费卡得到裸 _base」成立。
- 时机：CapturedXValue 于 `CardModel.cs:1826`（SpendEnergy 内，精确）写入；GeneratePlayCount 于 `CardModel.cs:1887`（OnPlayWrapper 内，精确）运行；`PlayCardAction.ExecuteAction` 先 `await _card.SpendResources();`（:92）再 `OnPlayWrapper`（:107）→ 手动打出路径上捕获严格先行，时机安全成立。

### A6 [L41] 战斗中生成卡的 API 面 —— ✅
**证据**：
- `CardFactory.cs:119`（精确）：`GetDistinctForCombat(Player, IEnumerable<CardModel>, int count, Rng rng)`；内部 FilterForCombat = `c.CanBeGeneratedInCombat && c.Rarity != CardRarity.Basic && != CardRarity.Ancient && != CardRarity.Event` + `.Distinct()`（:167）。
- `CardModel.cs:1267` SetToFreeThisTurn（=SetThisTurnOrUntilPlayed(0)+星费清零）；:1272 SetToFreeThisCombat（额外 SetStarCostThisCombat(0)）。
- `CardPileCmd.cs:267`（精确）：`AddGeneratedCardToCombat(CardModel card, PileType newPileType, Player? creator, CardPilePosition position = Bottom)`。doc 原文 "Card must have just been generated... We do this, instead of a regular add, because this adds the generated card entry to the combat history."；实现还校验：已有 pile 抛异常、非战斗 pile 抛异常、写 History.CardGenerated 并派发 AfterCardGeneratedForCombat —— plain `Add` 均无 → 「生成卡必须用 AddGeneratedCardToCombat」有充分依据。
- CardEnergyCost 全公共面核对（:94-414）：只有 Get*/Set*/Add*/UpgradeBy/Cleanup/Clone 族，**不存在** SetCostForCombat/ModifyCost/CostForTurn → 「整个费用表面就是 CardEnergyCost」成立。

### A7 [L42] FromChooseACardScreen —— ✅
**证据**：`CardSelectCmd.cs:252`（精确）签名含默认参 `bool canSkip = false`，返回 `Task<CardModel?>`；:254-257 `cards.Count > 3` 抛 ArgumentException；:264 调 `UndoEndTurnIfNecessary(player);`；签名无 prompt 参数。四项子声明全中。

### A8 [L43] CardPilePosition 与 Random 语义 —— ✅
**证据**：枚举恰为 {None, Bottom, Top, Random}（CardPilePosition.cs:3-9）；Random 分支 `CardPileCmd.cs:510`（声称 508-511，精确覆盖）：`CardPilePosition.Random => card.Owner.RunState.Rng.Shuffle.NextInt(cardPile.Cards.Count + 1)`。
附注：同一 switch 的 `_` 臂抛 ArgumentOutOfRangeException —— None 在入堆路径不可用（枚举有值但该消费点拒绝），与 DEVLOG 记录不冲突，补充备忘。

### A9 [L44] 遗物接收伤害钩子 —— ✅（附归因细化）
**证据**：
- `RunState.cs:567-572`（IterateHookListeners 内 childCombatState==null 分支）：`list.AddRange(player2.Relics.Where((RelicModel r) => !r.IsMelted));`，与 deck 卡、potions、Modifiers、BadgeModels 并列 —— 字面成立。
- `Hook.ModifyDamageInternal`（Hook.cs:2520-2557）：Multiplicative 段对每个监听器 `num *= item2.ModifyDamageMultiplicative(...)`（:2540-2541）→「乘进同一个 running product」成立。
- **细化**：战斗伤害钩子的监听器集合来自 `runState.IterateHookListeners(combatState)`；此时 RunState 版本跳过 relics（仅 childCombatState==null 才加），遗物实际由 `CombatState.IterateHookListeners`（CombatState.cs:411-473，同样 !IsMelted 过滤）补入。实质结论（遗物收伤害钩子、IsMelted 过滤、乘法合成）不受影响；归因建议改写为两条路径并存。

### A10 [L45] 伤害截断不舍入 —— ✅
**证据**：`Creature.cs:450`：`int num = (int)Math.Clamp(amount, 0m, 999999999m); CurrentHp = Math.Max(CurrentHp - num, 0);`（声明引 :449，实为紧邻下一行，同一语句块；内容逐字符吻合）。

### A11 [L46] GetRelic<T> / Relics / RelicCmd 表面 —— ✅
**证据**：`Player.cs:532-535` GetRelic<T>（FirstOrDefault(r => r is T)）；`Player.cs:118` `public IReadOnlyList<RelicModel> Relics => _relics;`；RelicCmd 五方法 `Obtain<T>(Player):22` / `Obtain(RelicModel, Player, int index = -1):35` / `Remove(RelicModel):61` / `Replace(RelicModel, RelicModel):74` / `Melt(RelicModel):89` —— 与 DEVLOG 所列逐一对应。

---

## 二、Session 6.3（DEVLOG L232-236）

### B1a [L233] ModelLocPatch 机制链 —— ✅
**证据**：`BaseLib.Patches.Localization/ModelLocPatch.cs`：`[HarmonyPatch(typeof(ModelDb), "Init")]` + `[HarmonyPostfix] AddModelLoc`；:81 只处理 `is ILocalizationProvider`；:49-50 类别映射 `SlugifyCategory("MonsterModel") -> "monsters"`；:90 `LocTable ?? CategoryToLocTable.GetValueOrDefault(...) ?? throw` → Spire1Monster 保持 LocTable=null 走类别回退成立；键以 `entry + "." + key` 写入表内 `_translations` 字典。

### B1b [L233] 「CustomMonsterModel 是 BaseLib 唯一无 ILocalizationProvider 的内容基类」 —— ❌（量词；机制不受累）
**反证**（均在 .tmp/baselib-dll/BaseLib.Abstracts/）：
- `CustomEncounterModel.cs:14` `public abstract class CustomEncounterModel : EncounterModel, ICustomModel` —— 无 ILocalizationProvider；
- `CustomActModel.cs:23`、`CustomEnchantmentModel.cs:6`、`CustomModifierModel.cs:7`、`CustomSingletonModel.cs:10` 同样没有。
本仓库自己的实践也早已越过该表述：`mod/Spire1Code/Monsters/Spire1Encounter.cs:24` `public abstract class Spire1Encounter : CustomEncounterModel, ILocalizationProvider` —— 遭遇侧同样需要自行补接口。
**影响评估**：零。「Spire1Monster 必须自行实现该接口」「LocTable 可留空走 monsters 映射」两个承重结论全部成立（见 B1a）；错的只是排他性措辞。建议 DEVLOG 改为「CustomMonsterModel 与 CustomEncounterModel 等均不带 ILocalizationProvider，凡需表本地化的自建基类都自行补实现」。

### B2 [L234] DonorId 视觉替换 —— ✅（末句为判断性陈述，静态无法完全证实）
**证据**：`mod/Spire1Code/Monsters/Spire1Monster.cs:34` `protected override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + DonorId);`；baselib `VisualsPath.cs` HarmonyPrefix 拦截 MonsterModel 视觉路径 getter：CustomVisualPath 非 null 即短路返回（与 RoomIconPathPatch 同型）。
末句「引擎默认动画机免逐怪工作」属设计判断：SetupAnimationState 静态助手存在（idle/dead/hit/attack/cast 全参，CustomMonsterModel.cs），CreatureAnimator 对缺失动画仅 Log.Warn（CreatureAnimator.cs:92-95）——静态层面无反证，但不能由反编译源单独证实，记为不可完全验证成分。

### B3 [L235] GenerateAllEncounters postfix —— ✅
**证据**：引擎 `ActModel.cs:285` `public abstract IEnumerable<EncounterModel> GenerateAllEncounters();`（abstract 属实）；baselib `Baselib.Patches.Content/AddActContent.cs`：Patch() 对每个 ActModel 子类以 `AccessTools.DeclaredMethod(item, "GenerateAllEncounters", null, null)` 检测（**必须 declared**，:22），命中则挂 Postfix `AddCustomEncounters`；追加条件 `!origResult.Any(id相同) && encounter.IsValidForAct(__instance)`（:51-53）。三个子声明全部精确。

### B4 [L236] act.Index=-2 语义 —— ✅
**证据**：
- `CustomActModel.cs:277` ctor：`Index = actNumber - 1;` → CustomActModel(-1) 得 Index=-2。
- 唯一读取点：全 dllsrc 正则扫 act 相关 `.Index` 仅命中 `ModelDb.cs:334` `if (act.Index >= 0)`（及同方法内 :336、:340 的使用）——ActsByIndex getter 之外引擎无处读 act.Index →「负数幕自然轮换安全隔离」成立。
- `CustomActModel.cs:195-201` AllAncients 对非 0/1/2 索引 `throw new Exception("Override AllAncients for acts with a non-basegame act number.")`。
- `CustomActModel.cs:205-211` BaseNumberOfRooms：0=>15, 1=>14, 2=>13, **_ => 15** —— 回退 15 属实。

---

## 三、Session 7.4（DEVLOG L278-284）

### C1 [L279] AddBranch 重载绑定 —— ✅
**证据**：`RandomBranchState.cs:46-113` 十个重载全列：(state,cooldown,repeatType,Func<float>):46 / **(state,cooldown,maxRepeats,Func<float>):62** / (state,maxRepeats,Func<float>):75 / (state,cooldown,repeatType,float):80 / (state,repeatType,float):85 / (state,repeatType,Func<float>):90 / (state,maxRepeats,float):95 / (state,cooldown,repeatType):100 / (state,maxRepeats):105 / (state,repeatType):110。
- 四参形态 (state,int,int,**float**) 不存在 → `AddBranch(s, 0, N, 裸float)` 无适用重载，编译失败，「必须 () => Wf」成立。
- maxRepeats=0：:62-73 置 StateWeight{maxTimes=0, repeatType=CanRepeatXTimes}；GetStateWeight（:150-173）中 num2=maxTimes=0 → `num = StateLog.Count < 0 ? 1 : 0` 恒 0，恢复循环被 `num3 < num2`（0<0）挡死 → 权重永久归零 = never repeat，精确成立。
- 边界备注：**三参** `(state, int maxRepeats, float weight)`（:95）是合法的——「裸 float 不转换」仅对四参形态成立，DEVLOG 表述与之一致。

### C2 [L280] 分支状态必须注册 —— ✅
**证据**：`MonsterMoveStateMachine.cs` FindNextMoveState：`if (!string.IsNullOrEmpty(nextState) && !States.ContainsKey(nextState)) throw new InvalidOperationException("no valid state found: " + nextState);` —— States 字典由状态机构造器的注册列表构建；RandomBranchState.RegisterStates 把自身加入字典。未注册分支一旦被 nextState 引用即抛同款消息，机制与文案双验证。

### C3 [L281] RollMove 用 RunRng.MonsterAi；MonsterModel.Rng 可作子滚 —— ✅
**证据**：`MonsterModel.cs:416-419` `NextMove = MoveStateMachine.RollMove(targets, Creature, RunRng.MonsterAi);`；`RunRngSet.cs:77` `public Rng MonsterAi => GetRng(RunRngType.MonsterAi);`。MonsterModel.Rng（:101-117）doc 原文 "It should ONLY be set in CombatState.CreateCreature" 且不可变态回退 Rng.Chaotic → per-combat 种子化属实。

### C4 [L282] 动画事实 —— ⚠️（API 半部 ✅，rig 清单半部不可验证）
- ✅ `SetupAnimationState(MegaSprite controller, string idleName, string? deadName, bool deadLoop, string? hitName, bool hitLoop, string? attackName, bool attackLoop, string? castName, bool castLoop)` 存在于 CustomMonsterModel.cs 静态方法，签名形状与 DEVLOG 一致。
- ✅ 未知触发 no-op + Log.Warn：`CreatureAnimator.cs:92-95` `if (!_spineController.HasAnimation(_currentState.Id)) { ... Log.Warn($"could not find '{_currentState.Id}' animation on '{value}'"); }`（排队路径 :125-128 同款）。
- ⚠️ fat_gremlin/lagavulin_matriarch/torch_head_amalgam/slimed_berserker 各自的轨道清单是资产级断言（.scn/.tres spine 数据），不在反编译 C# 可达范围内；本次不裁真伪，如需闭环须解包 pck 核对场景资源。

### C5 [L283] SetMaxAndCurrentHp 存在性 —— ✅
**证据**：`CreatureCmd.cs:911` `public static async Task SetMaxAndCurrentHp(Creature creature, decimal amount)`（= SetMaxHp + SetCurrentHp）；引擎自用点多处（DecimillipedeSegment.cs:142、ToughEgg.cs:173、WaterfallGiant.cs:306）。

### C6 [L284] 遭遇/怪物 loc 键形 —— ✅
**证据**：`EncounterLoc.cs` record(string Title, string LossText, ...) 隐转 ("title",..),("loss",..)；`MonsterLoc.cs` record(string Name, MoveTitles, ...) 隐转 ("name",..) + "moves."+stateId+".title"。与「encounter 用 .title/.loss；monster 用 .name + moves.<STATE_ID>.title」逐项吻合。

---

## 四、Session 4 Phase3-4（DEVLOG L74-81）

### D1 [L78] CanonicalVars/Keywords/Tags 每实例单次读 —— ✅
**证据**：
- `CardModel.cs:538-550` DynamicVars getter：`if (_dynamicVars != null) return ...; _dynamicVars = new DynamicVarSet(CanonicalVars); _dynamicVars.InitializeWithOwner(this);`（赋值行 :546 精确命中声称区间 538-549）。
- CanonicalKeywords 经 LocalKeywords 懒初始化：`_keywords = new HashSet<CardKeyword>(); _keywords.UnionWith(CanonicalKeywords);`（getter ~509-520，声称区间 507-518 命中）；Tags：`_tags ?? (_tags = CanonicalTags)`（~528）。
- `RelicModel.cs:296`（精确）同构 DynamicVars 懒缓存。
- Localization 每模型一次：ModelLocPatch 为 ModelDb.Init 的 HarmonyPostfix（见 B1a），每个模型仅在 Init 后写一次表。

### D2 [L79] 缓存 CanonicalVars/DynamicVars 会成 bug —— ✅
**证据**：`DynamicVarSet.cs` 构造器直接 `_vars[var.Name] = var;` 存引用、零克隆；InitializeWithOwner 对每个 value 调 `value.SetOwner(model)`。引擎各模型类（CardModel/PowerModel/RelicModel/EventModel/PotionModel/EnchantmentModel）均为每实例各自 `new DynamicVarSet(CanonicalVars)`，而 CanonicalVars 表达式体（=> [new DamageVar(6)] 风格）每次求值新建 var 对象；跨实例共享缓存即共享同一批活体 var（UpgradeValueBy/BaseValue/ResetToBase 互染）。Clone(model) 存在但仅在 DeepCloneFields 显式走 `DynamicVars.Clone(this)`。DEVLOG 的 bug 推演成立。

### D3 [L80] Clash/SignatureMove 性能修复的行为等价性 —— ✅
**证据**：
- 引擎 `CardPile.cs:64-67`：`public static IEnumerable<CardModel> GetCards(Player player, params PileType[] piles) => piles.SelectMany(p => p.GetPile(player).Cards);` —— params 数组 + SelectMany，与 DEVLOG 描述逐字一致。
- `CardPile.cs:49-62` Get(PileType, Player) 为纯 switch 直取 `player.PlayerCombatState?.Hand` 等缓存堆属性 → O(1)。
- mod 现状：`Clash.cs:21` `protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.All(c => c.Type == CardType.Attack);`；`SignatureMove.cs:21-22` 同型 Any 查询。单 pile 查询下两者枚举同一底层 List，出战斗时双方都经 GetPile 抛 InvalidOperationException —— 结果集与失败模式全同，行为等价成立。

### D4 [L81] 类型化访问器 = 同一字典查找 —— ✅
**证据**：`DynamicVarSet.cs`：`public DamageVar Damage => (DamageVar)_vars["Damage"];`（Block/Heal/Strength 等二十余个同型）——与索引器 `this[string key] => _vars[key]` 同一次字典查找加一次强转，「转换字符串键站点无收益」的非发现结论成立。

---

## 五、Session 9.3（DEVLOG L398-407）

### E1 文本路径 LocManager.GetTable / LocTable.GetRawText —— ✅
**证据**：`LocManager.cs:483` `public LocTable GetTable(string name)`；`LocTable.cs:44-55` GetRawText（miss → fallback 表 → LocException）；`LocString.cs:85` `return LocManager.Instance.GetTable(LocTable).GetRawText(LocEntryKey);` —— 即 THE text path。mod 合并机制：ModelLocPatch 把每个模型的键以 `entry + "." + key` 写入既有表字典，entry 即 SPIRE1-* 的 ModelId.Entry → 「并入同名表、SPIRE1- 前缀」成立。

### E2 EncounterModel.MapNodeAssetPaths —— ✅
**证据**：`EncounterModel.cs:212-225`：BossNodeSpineResource != null 时返回单元素 [BossNodePath(.tres)]，否则返回 [BossNodePath + ".png", BossNodePath + "_outline.png"]；BossNodePath virtual（:193，指向 animations/map/<id>/<id>_node_skel_data.tres）；BossNodeSpineResource 内 ResourceLoader.Exists 检查（:199）。与 DEVLOG 描述一致。

### E3 BaseLib RoomIconPathPatch 短路 —— ✅
**证据**：`BaseLib.Patches.UI/RoomIconPathPatch.cs:12-31`：`[HarmonyPatch(typeof(ImageHelper), "GetRoomIconPath")]` Prefix 对 CustomAncientModel/CustomEncounterModel 取 `CustomRunHistoryIconPath` 置 `__result` 并 `return __result == null;` → 非 null 即短路 vanilla 路径，属实。

### E4 AssetCache 投毒 —— ✅
**证据**：`AssetCache.cs:49`：`throw new AssetLoadException("Asset previously failed to load: " + path + ". The game installation may be corrupted.");` —— 消息逐字命中；后续加载走缓存即抛而非返 null，「装饰性缺图升级为启动崩溃」的机理成立。

---

## 六、统计与总评

| 组 | 条目 | ✅ | ❌ | ⚠️ |
|---|---|---|---|---|
| Session 4 facts（A1-A11） | 11 | 11 | 0 | 0 |
| Session 6.3（B1a/B1b/B2/B3/B4） | 5 | 4 | 1 | 0 |
| Session 7.4（C1-C6） | 6 | 5 | 0 | 1 |
| Phase3-4（D1-D4） | 4 | 4 | 0 | 0 |
| Session 9.3（E1-E4） | 4 | 4 | 0 | 0 |
| **合计** | **30** | **28** | **1** | **1** |

- **通过率：28/30 = 93.3%**；❌ 1 条（B1b 排他量词，零下游影响）；⚠️ 1 条（C4 rig 轨道清单，资产级超出反编译可达域）。
- **行号精度**：约 40 个 file:line 锚点中，除 Creature.cs「:449 实为 :450」一行之差外全部精确命中。该精度水平支撑了「do NOT re-derive」的使用方式。
- **需修订的 DEVLOG 文本（共 2 处，均为措辞级）**：
  1. L233：「CustomMonsterModel is BaseLib's ONLY content base without ILocalizationProvider」→ 改为「CustomMonsterModel（及 CustomEncounterModel/CustomActModel/CustomEnchantmentModel/CustomModifierModel）不带 ILocalizationProvider；凡需表本地化的自建基类自行补实现」。我方 Spire1Encounter 已按后者实践。
  2. L44：遗物伤害钩子归因可细化为「run 态经 RunState.IterateHookListeners（childCombatState==null 分支），战斗态经 CombatState.IterateHookListeners」，两者均有 !IsMelted 过滤。
- **未发现任何会导致行为错误的引擎事实错误**。昨夜 freeze-review 未涉及本域条目的推翻需求，本次独立重推亦未产生新的 ❌ 级反证。
