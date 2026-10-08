# 2026-10-08 联机 StateDivergence 分析：AutoAnthonyWatcher 生成池在两端激活状态不一致

来源：`G:\appdata\C-Users-o_Obl\Roaming\SlayTheSpire2\logs\ritsulib_state_divergence_20261008_*`（5 个包，已解包到
`G:\tmp\div-20261008\ritsulib_state_divergence_*`）。全部结论只读得出，未改 Steam 副本、未改共享
`mod_configs`、未重启游戏。

结论分三段：**已确认（有直接证据）/ 待验证（推断，未取到运行日志）/ 未复现**。

---

## 0. 一句话结论

**同一局里，房主（Watcher 玩家）按"原版观者牌池"跑，客机（SteamID 76561199033460852）按"生成（chaos）观者牌池"跑。**
两端对同一张卡（同一 `NetCombatCard`/choice 索引）解析出**不同卡身份**，校验和因此不同。
不是种子问题，不是版本号不同，也不是 RitsuLib 误报。

进一步（§1.6/§2.1）：客机启用的**唯一可能来源是它自己的本地 AA 设置**
（`AutoAnthony/settings.json` 的 `Enabled=true`）——房主 carrier 明确携带 `MultiplayerModEnabled=false`，
已排除"客机误读房主 carrier"。AAW 0.3.3 的读档路径 `options with { Enabled = true }`
会把"关"强制改写成"开"，使本地开关形同虚设。

**第二个独立症状（§1.4）**：`CombatCardGeneration` 的 RNG 计数器两端差 3
（`host = client + 3`，`advance(client_state, 3) == host_state` 逐位相等）。
`NetFullCombatState` 包含 RNG，所以**仅此一项**就足以触发断开——
包 #2/#3/#5 正是"卡身份零差异、纯 RNG 不一致"而断的。
⇒ 修好"池不同"**不一定**能消除分歧，RNG 差 3 必须单独处理。

修复方向见 §4（未改动任何文件）。

---

## 1. 已确认（直接证据）

### 1.1 分歧的物理表现：同一索引 → 不同卡

| 包 | 触发动作 | 分歧槽位 | 房主视角 | 客机视角 |
|---|---|---|---|---|
| `...221012` (checksum 336) | `UsePotionAction ... index: 2`（Power Potion，选牌索引 0） | Watcher Hand[04] | `CARD.WATCHER_MASTER_REALITY` | `CARD.CHAOS_WATCHER_CARD051` |
| `...221302` (checksum 34) | `SPIRE1-DISCOVERY`（Regent 打出，选牌索引 0） | Regent Hand[03] | `CARD.SPIRE1-DUAL_WIELD` | `CARD.SPIRE1-GENETIC_ALGORITHM` |

- 包 #1 两端 dump 卡总数均 59，**只有这 1 张不同**；其余 58 张（含升级、附魔、楼层）逐字节一致。
- 包 #4 两端卡总数均 61，只有这 1 张不同。
- 两处都是"**同一条选择载荷（`indexes 0`）在两端指向不同卡**"，这是"候选池内容不同"的判别性证据——
  不是随机数不同（若只是 RNG 不同，抽出的卡会整体偏移，而非仅一处）。

### 1.2 客机日志直接显示它在生成 chaos 卡，房主没有

客机 debug-log（`remote-debug-log.records.json`，日志源含 `Watcher`、`Spire1`，**不含 `AutoAnthony`**）：

```
14:07:33  Player 76561199466878739 chose cards [CHAOS_WATCHER_CARD038]
14:07:33  [DebugCompat] Missing localization key 'CHAOS_WATCHER_CARD038.title' ...
14:09:11  Player 76561199466878739 obtained CARD.CHAOS_WATCHER_CARD041 from card reward
14:10:31  Player 76561199466878739 chose cards [CHAOS_WATCHER_CARD051]
```

房主 debug-log 同一时段（14:07–14:10）**没有任何 CHAOS 卡**，只有：

```
14:07:09  Player 76561199466878739 chose cards [WATCHER_FASTING2]
14:08:46  Player 76561199466878739 obtained CARD.WATCHER_MENTAL_FORTRESS from card reward
14:10:07  Player 76561199466878739 chose cards [WATCHER_MASTER_REALITY]
```

两端是**同一场战斗、同一 turn 1**，同一个 Watcher 玩家、同一个 seed，选同一索引 → 一端出原版卡、一端出 chaos 卡。

### 1.3 房主侧 AA 明确"把这一局当作原版 run"

房主日志出现 9 次（每次自动存档时）：

```
AutoAnthony | Treated RunState.FromSerializable as a vanilla run because it contains
neither an AutoAnthony pool snapshot nor pool-managed generated card IDs.
```

对应 AA 反编译 `AutoAnthony.decompiled.cs:66875`（`SeedBeforeLoadPatch.PrepareSavedRun`）：
存档里既无 `PoolSnapshot`、牌组里也无 `IsGeneratedCardId` 的卡 → `ChaosRunDefinitions.DeactivateRun()`。
即**房主的存档里根本没有 AA 生成池**，观者牌池从未被 chaos 池替换。

### 1.4 RNG：唯一不同的流恰好是"战斗内造卡"，恒定差 3

| 包 | `CombatCardGeneration.counter` (host / client) | 差 |
|---|---|---|
| `...221012` | 379 / 376 | +3 |
| `...221040` | 379 / 376 | +3 |
| `...221115` | 379 / 376 | +3 |
| `...221302` | 1080 / 1077 | +3 |
| `...221545` | 487 / 484 | +3 |

- `rng.run.seed` 两端相同（`0HR24G6AS5PS`）；13 个 run 级 RNG 流里**只有 `CombatCardGeneration` 不同**，
  其余（`UpFront`/`Shuffle`/`MonsterAi`/`TreasureRoomRelics` …）逐位一致。
- `CombatCardGeneration` 只被 `CardFactory.GetDistinctForCombat` / `GetForCombat` 这类"战斗内随机造卡"消费
  （`AttackPotion`、`PowerPotion`、`Discovery`、`CARD.SPIRE1-DISCOVERY` 等）。
- `GetDistinctForCombat` → `FilterForCombat(cards).TakeRandom(count, rng)` → `UnstableShuffle`，
  Fisher–Yates 消耗 `n-1` 次 `NextInt`（`ListExtensions.cs:45-60`）。**候选集大小每差 1，RNG 消耗就每差 1**，
  且会一路错位下去。差 3 ⇒ 两端候选池大小相差 3（或消耗路径相差 3 次）。
- **严格验证（`q8_rng_offset.py` → `Q8_RNG_OFFSET.txt`）**：`Rng.NextInt(max)` 每次只消耗一次
  `MegaRandom.NextULongInner()`（`Rng.cs`：`_counter++; _random.Next(max)`），
  所以状态只取决于**抽取次数**、与 `max` 无关。用 xoshiro256\*\* 逐步推进客机状态 3 次：
  **5 个包里 `advance(client_state, 3) == host_state` 全部成立**（逐位相等，非近似）。
  ⇒ 两端不是"用了不同的随机源"，而是**同一条流上相差恰好 3 次抽取**。
- **方向**：`host.counter > client.counter`（379>376 等），且 `host = advance(client, 3)`，
  即**房主多抽了 3 次**。
  ⚠️ 但这**不能**顺势推出"因为房主候选池更大"：`advance` 逐位相等只证明"同一条流相差 3 次抽取"，
  不证明这 3 次的来源。池差与调用次数差两种模型都与观测相容（见 §2.2）。
- **差值恒为 3 不能用来排除累积性池差**：本作在**第一次**校验和不一致时房主就
  `DisconnectClient(..., StateDivergence)`（`ChecksumTracker.cs:150-152`），客机随即被踢、本局结束，
  ⇒ 差值**没有机会累积**，我们只能看到"首次不一致"那一刻的快照。见 §2.2。
- **计数器不是单调的**：`...221302` = 1080/1077，而更晚的 `...221545` = 487/484（变小）。
  这是 `TurnRewind`（`sts2.piyixiajiuhenfen.rewind`）回退 + `CombatStateSynchronizer.cs:181`
  `_runState.Rng.LoadFromSerializable` 的结果：**本局发生过多次状态回退**（房主日志 14:10–14:15
  有 5 次 `combat started`，间隔远短于正常战斗时长）。
  ⇒ 跨包差值不能当作"累积消耗"来用；但**每个快照自身都是 +3**，这一点不受回退影响。
- 因此**"哪一次调用造成这 3 次差"包内材料判定不了**。两种可能都自洽：
  (i) 某次走**整池/大子池**的调用两端候选集大小差 3（如一次 73 vs 70 的调用）；
  (ii) 回退/读档时机差异导致某一端少补了 3 次抽取。
  要定论需在测试副本上打印每次 `FilterForCombat` 后的实际列表大小。
- **新证据（校验和 ID 与计数器的对照，指向"差值在加载时就已存在"）**：

  | 包 | 校验和 ID | 触发动作 | `CombatCardGeneration` | 卡身份差异 |
  |---|---|---|---|---|
  | `...221012` | 336 | `UsePotionAction`（Power Potion） | 379 / 376 | **1 张** |
  | `...221040` | **1** | `GenericHookGameAction id 0` | 379 / 376 | 0 |
  | `...221115` | **1** | `GenericHookGameAction id 0` | 379 / 376 | 0 |
  | `...221302` | 34 | `PlayCardAction CARD.SPIRE1-DISCOVERY` | 1080 / 1077 | **1 张** |
  | `...221545` | 82 | `PlayCardAction CARD.FALLING_STAR` | 487 / 484 | 0 |

  包 #2/#3 在**校验和 ID = 1**（会话内第 1 个校验和，`lastExecutedActionId=2`）就触发断开，
  计数器却已是 `379 / 376` —— 与包 #1 在会话中段（ID 336，`lastExecutedActionId=462`）
  观测到的**同一对数值**。可得的结论：
  1. 差值在**该会话最早的比较点**就已存在（这是可观测的最早点），即它**不是**在该会话里
     经过多个动作逐步累积出来的 —— 要么在加载/开局时就带进来，要么由该会话第一个动作一次造成。
     （**注**：计数器是随存档累积的序列化值，所以"ID=1 时计数 379"本身并不能区分这两者；
     真正的判据是"没有中间状态可观测"。）
  2. 包 #2/#3/#5 的**卡身份零差异**、却依然因 RNG 字段不一致而断开 —— 说明"RNG 差 3"本身
     就足以触发 `StateDivergence`（`NetFullCombatState` 含 RNG，`NetFullCombatState.cs:473`），
     **不需要**先出现卡身份分歧。即卡身份差异是"池不同"的**表现**，而 RNG 差是**独立**的第二个症状。
  ⇒ 修好"池不同"不一定能消除 RNG 差 3；两者都要处理。

- **注意：5 个包不是 5 个独立样本**。包 #1/#2/#3 的 `CombatCardGeneration` **四个 state 字与计数器完全相同**
  （`379/376`，`state0..3` 逐字相同），只有 `checksum.value`、校验和 ID、触发动作不同
  ⇒ 三者是**同一份 RNG 状态**在三次重连后被反复比对。真正独立的样本只有 3 个：
  `379/376`（#1/#2/#3）、`1080/1077`（#4）、`487/484`（#5）。
  统计上只能说"三次独立观测都恰好 +3"，**不足以**据此断定差值不会随调用累积
  （每次重连都会用 `LoadFromSerializable` 把客机 RNG 重置回房主值，累积会被清零）。

### 1.5 两端装的 mod 清单不同（但三个相关 mod 版本串相同）

`loadedMods.count: local=49; remote=32`。房主独有 20 个（全是 `non-gameplay`：皮肤、UI、统计类），
客机独有 3 个（`JmcModLib`、`MaxHpSizeMod`、`东尼算法 (AutoAnthony)`）。

**关键**：`AutoAnthonyWatcher` 两端都报 `version=0.3.3 / workshop=3794876718`，
`AutoAnthony` 两端都报 `version=0.3.139 / workshop=3786611028`，`Watcher` 两端都报 `0.10.0`。
→ **"版本号不一致"不是本次原因**（此前会话的假设可排除）。

> 注：本机 `E:\Slay the Spire 2\mods\AutoAnthonyWatcher` 是 **0.3.2** 的陈旧本地副本，
> 与本次两端实际运行的 0.3.3 不是同一个构建。之前会话用 E: 副本做"host vs client"程序集 diff 得到的
> "两端结构不同"结论**不适用于本次事故**，请勿引用。

### 1.6 机制：0.3.3 的"读档强制开启"路径

AAW 0.3.3 反编译（`G:\tmp\div-20261008\decomp\host\`，来自 workshop 3794876718）：

`AutoAnthonyWatcher.Patches/WatcherSeedBeforeLoadPatch.cs`（Harmony prefix on `RunState.FromSerializable`）：

```csharp
if (!save.Players.SelectMany(p => p.Deck).Any(c => ChaosWatcherCardRegistry.IsGeneratedCardId(c.Id))
    && !ChaosSettingsBridge.Enabled)
{
    WatcherChaosRun.Deactivate();
    return;
}
bool takeOverColorless = !save.Players.Any(p => WatcherColorlessPool.IsBaseCharacterId(p.CharacterId));
WatcherGenerationOptions options = WatcherRunOptionsStore.TryGet(seed, out var stored) ? stored
                               : ChaosSettingsBridge.FromLocalSettings();
WatcherRunActivation.ActivateFor(hasWatcher: true, seed, options with { Enabled = true }, "读档", takeOverColorless);
```

两个要点：

1. **`options with { Enabled = true }` 无条件强制开启**——只要走到这一行，`Enabled=false` 也会被改写成 `true`。
   `WatcherChaosRun.Activate` 里 `if (!options.Enabled) Deactivate();` 这一"关闭开关"因此**形同虚设**。
2. `Enabled` 的守卫只看**两件事**：存档牌组里有没有 chaos 卡 ID、或 `ChaosSettingsBridge.Enabled`（本地 AA 设置）。
   它**不看** `WatcherRunOptionsStore`（`user://AutoAnthonyWatcher/run_options.json`）里记录的 `Enabled`。

房主的 `run_options.json`（`G:\appdata\...\SlayTheSpire2\AutoAnthonyWatcher\run_options.json`）中
本局 seed `0HR24G6AS5PS` 记录为：

```json
{"Enabled": false, "UltimateChaos": false, "NumericBalanceOptimization": false, "NumericRandomMode": false,
 "ReplaceStartingCards": true, "RandomCardArt": false, "PreserveOriginalCards": false, "SavedAt": 1791467498}
```

`SavedAt=1791467498` = 2026-10-08 13:51:38 UTC = **本机 21:51:38（+08）**，本局开打（22:10 分歧）前约 19 分钟。
即：**本局开局时 `Enabled=false` 已落盘**（房主视角：本局不开 chaos）。

`Enabled` 的真实来源是 AA 的本地设置 `G:\appdata\...\SlayTheSpire2\AutoAnthony\settings.json`
（`"Enabled": false, "AddGeneratedCards": true`，文件 mtime 2026-10-07 06:26，本局前未改动）：

```csharp
// ChaosSettingsBridge (0.3.3)
internal static bool Enabled => ComponentRunSettingsApi.Local is { Enabled: true, AddGeneratedCards: true };
```

### 1.7 两端日志时间戳差 ~24-25 秒，事件一一对应

把两端日志按"同一次选择"对齐（客机时间戳系统性晚 24–25 秒）：

| 房主时刻 | 房主解析 | 客机时刻 | 客机解析 |
|---|---|---|---|
| 14:06:51 | `chose cards [WATCHER_DEFEND_P]` | 14:07:16 | `chose cards [WATCHER_DEFEND_P]` ✅ 同卡 |
| 14:07:09 | `chose cards [WATCHER_FASTING2]` | 14:07:33 | `chose cards [CHAOS_WATCHER_CARD038]` ❌ |
| 14:08:46 | `obtained CARD.WATCHER_MENTAL_FORTRESS` | 14:09:11 | `obtained CARD.CHAOS_WATCHER_CARD041` ❌ |
| 14:10:07 | `chose cards [WATCHER_MASTER_REALITY]` | 14:10:31 | `chose cards [CHAOS_WATCHER_CARD051]` ❌ |

**同一条选择载荷（`NetPlayerChoiceResult indexes 0`）在两端解析成不同卡**——这是"索引打进了一份不同的候选列表"，
即两端 `FilterForCombat` 后的候选池内容不同。第一行（同卡）说明载荷本身同步正常，排除网络丢包/乱序。

### 1.8 判定：客机侧是偏离方（房主是权威）

- 房主 `run_options.json` 里本 seed `0HR24G6AS5PS` 的 `SavedAt`=1791467498 = 2026-10-08 21:51:38 (+08)，
  而本局分歧发生在 22:10；`Record` 只在 `WatcherSeedBeforeMultiplayerPatch`（联机开局）里调用，
  写入的是**开局解析结果** ⇒ 房主开局解析结果就是 `Enabled=false`。
  加上房主自己的 dump 里 **0 张 chaos 卡**，可判定房主**本局没有启用 chaos 池**
  （也因此排除了"房主中途被读档路径关掉"这一假说——若开局是 true，`Record` 会写 true）。
- 客机侧：窗口（客机 14:04:52 起）内**没有任何 `AutoAnthonyWatcher` 源日志**，但 14:07:33 已有 chaos 卡。
  生成池只在"联机开局"（`WatcherSeedBeforeMultiplayerPatch`）或"读档"（`WatcherSeedBeforeLoadPatch`）
  两条路径安装，两者都发生在战斗之前 ⇒ **客机的 chaos 池在 14:07:33 之前就已装好**，
  不是被窗口内某次 `FromSerializable` 中途翻转的。
- AA 的设计意图是房主权威（AA 源码会打印 `Using host multiplayer ... setting instead of the local setting`）。
  ⇒ **本局权威设置是"不开生成池"，客机偏离。**

### 1.9 客机侧 AA/AAW 确实在运行（早期版本报告里"客机无 AA 日志"的说法不准确）

**修正**：早期只看了包 #1（`...221012`）就断言"客机日志源里没有 `AutoAnthony`"。
按 5 个包全量统计，客机在**后 4 个包**里都有 `AutoAnthony` 源日志（`source=AutoAnthony`）：

| 包 | 房主 `AutoAnthony` 行 | 客机 `AutoAnthony` 行 | 客机窗口 |
|---|---|---|---|
| `...221012` | 10 | **0** | 14:04:52–14:10:33 |
| `...221040` | 16 | 4 | 14:05:22–14:11:02 |
| `...221115` | 22 | 8 | 14:05:44–14:11:36 |
| `...221302` | 24 | 14 | 14:08:29–14:13:24 |
| `...221545` | 27 | 18 | 14:10:29–14:16:07 |

客机那些行的内容是（`...221545` 为例）：

```
14:10:54  AutoAnthony | Treated multiplayer load lobby as a vanilla run because it contains neither
                        an AutoAnthony pool snapshot nor pool-managed generated card IDs.
14:10:55  AutoAnthony | Treated RunState.FromSerializable as a vanilla run because it contains neither
                        an AutoAnthony pool snapshot nor pool-managed generated card IDs.
```

⇒ **客机同样把这一局当作"原版 run"**（AA 本体的 `ChaosRunDefinitions` 在客机也是 deactivate 的）。
这与 §1.11 的观察一致（客机 AA 认不出 Watcher 角色）。

**因此"客机跑 chaos 观者池"这件事只能由 AAW（`AutoAnthonyWatcher`，观者专用扩展）负责**——
它不走 AA 本体的 `ChaosRunDefinitions`，而是走 `ExternalComponentCharacterApi` +
`WatcherChaosRun`。这也说明**不能**用"客机 AA 没启用"去否定"客机装了 chaos 池"：两者是不同子系统。

> 早期结论"客机日志源里没有 `AutoAnthony`"**作废**。窗口截断的解释也不再需要——
> 客机日志里 AA 行本来就存在，只是包 #1 的窗口（截至 14:10:33）刚好在客机首次打印之前。

### 1.10 客机独有：本仓 Spire1 的 `AutoAnthony bridge` 在客机被触发，房主没有

客机日志里反复出现**本仓 sts2-spire1 自己的**日志行（`source=Spire1`）：

```
14:10:55  Spire1 | AutoAnthony bridge: Watcher -> generated pool #0.
14:11:27  Spire1 | AutoAnthony bridge: Watcher -> generated pool #0.
14:12:29  Spire1 | AutoAnthony bridge: Watcher -> generated pool #0.
14:14:08  Spire1 | AutoAnthony bridge: Watcher -> generated pool #0.
```

房主日志里**一条都没有**（房主的 `Spire1` 源有 120/104 行，但全是贴图缺失类）。

该行出自 `mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs:868`（`FromCharacterPostfix`）——
它是对 `AutoAnthony.Patches.ChaosCharacterMapping.From(CharacterModel)` 的 Harmony **Postfix**，
`__result == null` 时才写入 `Watcher -> GcIronclad`（0）。

含义与后果：

1. 客机上 `ChaosCharacterMapping.From(Watcher)` **返回了 null**，被本桥补成 `Ironclad(0)`。
   ⇒ 客机的 AA **认不出** Watcher 角色。
2. 该映射在 AA 里是"这局要不要生成池"的判据：`SeedBeforeMultiplayerPatch` 用
   `lobby.Players.Select(p => ChaosCharacterMapping.From(p.character))` 得到 `GeneratedCharacter[]`，
   **空数组就整段跳过**（`AutoAnthony.decompiled.cs:66641` + `array.Length != 0` 守卫）。
3. 但**这不足以解释客机装了 chaos 观者池**：本桥映射的是 AA 本体（`AutoAnthony.Chaos*`），
   AAW 的观者池走 `ExternalComponentCharacterApi` + `WatcherChaosRun`，不经过本桥。
   ⇒ 本桥是**客机环境异常的一个可靠指示器**（说明客机的 AA/Watcher 装配与房主不同），
   但它与 `CHAOS_WATCHER_CARD051` 的出现之间**没有已证实的因果链**。
4. 触发时机也值得注意：4 次全部落在**重连后约 1 秒**（14:10:54 连上 → 14:10:55 打印），
   与 §1.10 的"重连即复现"完全同步。

> 判定：**已确认的观测**是"本桥只在客机触发、且每次重连都触发"；
> **未确认**的是它是否参与本次分歧。要定论需在测试副本上复现并打印
> `ChaosCharacterMapping.From(Watcher)` 的返回值与 AAW `WatcherChaosRun.IsRunActive`。

### 1.11 本局经历过多次"分歧 → 断开 → 重连"，5 个包是同一次故障的连续快照

房主日志（`local-debug-log.records.json`）显示本局是**反复重连**的过程，而不是一次性的分歧：

```
14:10:07  State divergence detected! Checksum ID 336 for client 76561199033460852 doesn't match host's!
14:10:12  Disconnecting peer 76561199033460852, reason: StateDivergence
14:10:19  SteamHost | Initializing Steam host. Our player id: 76561199466878739   ← 重建房主会话
14:10:23  Player connected: 76561199367055891
14:10:29  Player connected: 76561199033460852
14:10:35  State divergence detected! Checksum ID 1 ...                              ← 重连后约 6 秒再次分歧
14:10:40  Disconnecting peer 76561199033460852, reason: StateDivergence
14:10:48  SteamHost | Initializing Steam host ...                                    ← 再重建
14:11:10  State divergence detected! Checksum ID 1 ...
14:11:15  Disconnecting peer ... ; 14:12:57 checksum 34 ; 14:15:40 checksum 82
```

关键含义：

- 每次重连都走 `NMultiplayerLoadGameScreen.StartRun()` → `RunState.FromSerializable(_runLobby.Run)`
  → **`WatcherSeedBeforeLoadPatch`（读档路径）被触发**。这正是 §1.6 里"`Enabled` 被强制改写为 `true`"的路径。
- 重连后 **6–30 秒内**就再次分歧，且分歧点都在**战斗内造卡**（`UsePotionAction`、
  `PlayCardAction CARD.SPIRE1-DISCOVERY`、`PlayCardAction CARD.FALLING_STAR`）——
  说明客机的 chaos 池在每次重连后**依然生效**（池状态是进程级静态量，不随重连重置）。
- **重要修正（勿过度解读）**：两端窗口内都**没有** `[AutoAnthonyWatcher] Installed the generated Watcher pool`
  这类安装日志。这不等于"客机没装"，最可能是 `WatcherChaosRun.Install` 的**短路分支**
  （`WatcherChaosRun.cs:192-204`，`Install` 方法开头）：`if (_installed && _installedSeed == seed && _installedOptions.Equals(options))`
  直接 `return true` 且**不打日志**。客机首装发生在窗口开始前，此后每次重连都命中短路。
  （此为代码层解释，客机首装日志本身不在窗口内，无法直接验证。）
- 客机的 chaos 池在窗口内**确实生效**，这有直接证据而非靠日志缺席推断：
  dump 里客机 Watcher 手牌含 `CARD.CHAOS_WATCHER_CARD051`（§1.1），
  且 `CombatCardGeneration` 状态与房主相差 3 次抽取（§1.4）。
  注意**不能**用"没有 `Deactivated the generated Watcher pool` 行"来论证它一直激活——
  该行只在 `_runActive` 由 true 变 false 时打印，从未激活同样不会打印。
- `...221040`/`...221115` 两个包的 `Last executed action ID` 都是 `2`，`Choice IDs` 都是 `1,1`，
  即**重连后刚起步就分歧**；而 `...221012`（`action 462`）是重连前那次。
- ⇒ 5 个包不是 5 个独立事故，而是**同一次"池不一致"在每次重连后立刻复现**的连续快照。
  这也解释了为什么 5 个包的 `CombatCardGeneration` 差值恒为 +3（每次重连都从同一个分叉点重放）。

---

## 2. 待验证（推断，缺运行日志）

### 2.1 客机为何启用了 chaos 池（已缩小到"客机本地 AA 设置为开"）

方向已由 §1.8 定为"客机偏离、房主权威"。**进一步：现有代码路径已排除"客机误读房主 carrier"，
只剩"客机本地 AA 设置就是开的"。**

**(B) 已被排除——房主 carrier 的内容可证伪"误读"**：

- 房主 `ChaosModSettings.EffectiveGeneratedCardsEnabled` = `Enabled && AddGeneratedCards` = `false && true` = **false**，
  所以房主在 `MultiplayerGenerationModePatch.Prefix` 里走的是**提前返回分支**
  （`AutoAnthony.decompiled.cs:63962-63965`）：
  `AddGenerationMarker(modifiers, null, null, ChaosModSettings.Enabled /*false*/, ...)`。
  ⇒ carrier 的 `MultiplayerModEnabled` **必然是 false**（房主 `settings.json` 的 `"Enabled": false` 亦为佐证）。
- 客机侧 `ChaosSettingsBridge.From(settings)` = `new WatcherGenerationOptions(settings.Enabled && settings.AddGeneratedCards, ...)`，
  其中 `settings.Enabled` 直接来自 `MultiplayerModEnabled`。**`MultiplayerAddGeneratedCards` 无论取何值，
  都不可能把 `false` 变成 `true`。** ⇒ "客机把房主 carrier 解析成启用"这一假说**不成立**。
- 补强：两端 patch 优先级也有利于 carrier 正常传递——AAW `WatcherCarrierCapturePatch` 是
  `[HarmonyPriority(800)]`，AA `MultiplayerGenerationMarkerTransportPatch` 无优先级（默认 400），
  Harmony 前缀按优先级**降序**执行 ⇒ AAW 先读到 marker，之后 AA 才把它摘走转存
  （`Pending`）并在 `NGame.StartNewMultiplayerRun` 重新挂回。

**(A) 收敛后的结论**：客机要装上 chaos 池，**只能**靠它自己的本地 AA 设置为开。三条安装路径逐条排除如下：

| 路径 | 触发条件 | 若客机本地 `Enabled=false` 的结果 |
|---|---|---|
| `WatcherSeedBeforeMultiplayerPatch`（联机开局） | carrier → `TryFromMultiplayer` → fallback `FromLocalSettings()` | carrier 给出 `Enabled=false` → `WatcherChaosRun.Activate` 里 `if (!options.Enabled) Deactivate()` → **不装** |
| `WatcherSeedBeforeLoadPatch`（读档） | 守卫：牌组有 chaos 卡 **或** `ChaosSettingsBridge.Enabled` | 开局时牌组无 chaos 卡（房主 dump 可证：Watcher 牌组 22 张全是原版 `WATCHER_*`），`ChaosSettingsBridge.Enabled` 又需 `Enabled && AddGeneratedCards` → **不装** |
| `WatcherSeedBeforeSingleplayerPatch` | 单机 | 本局是联机 → 不走 |

⇒ **客机的 `AutoAnthony/settings.json` 必然满足 `Enabled=true`（且 `AddGeneratedCards=true`）**，
`ChaosSettingsBridge.Enabled` 因此为真，读档路径的守卫被放行，再叠加 §1.6 的
`options with { Enabled = true }` 强制改写，chaos 池就被反复装回。

仍需下一局开局日志确认**具体是哪一条**在起作用（carrier 未到达 → fallback，还是 carrier 到达后
又被读档路径翻回开）。判据（在**开局**时刻搜）：
`[AutoAnthonyWatcher] Installed the generated Watcher pool`、
`[AutoAnthonyWatcher] 联机开局没拿到房主的生成模式载体`（出现即说明 fallback 生效）、
`[AutoAnthonyWatcher] Deactivated the generated Watcher pool for this run`、
`[AutoAnthony] Captured the host generation carrier`、
`[AutoAnthony] Host selected multiplayer generation mode`。
这些行**都不在本次 5000 条窗口内**（窗口只覆盖 run 后段；`metadata.json` 的 `droppedOldRecordCount=0`，
说明截断发生在采集上限而非丢弃）。

**最快的验证**：直接向客机玩家索取 `%APPDATA%\SlayTheSpire2\AutoAnthony\settings.json`
（或 Godot 的 `user://AutoAnthony/settings.json`）——若其 `Enabled` 为 `true`，(A) 即被证实。

### 2.2 池大小与计数器差 3 的关系（两个症状分开归因）

两侧池大小（反编译静态计数）：

| 池 | 成员数 | 战斗内可生成（`FilterForCombat` 后，排除 Basic/Ancient/Event + `CanBeGeneratedInCombat=false`） |
|---|---|---|
| 原版 `WatcherMod.WatcherCardPool` | 83 | **73**（实测计数：Basic 4 + Ancient 4 排除，另 `WatcherLessonLearned`/`WatcherWish_P` 两张 `CanBeGeneratedInCombat => false`） |
| AAW `ChaosWatcherCardPool`（`ChaosWatcherCardRegistry.Types`，Slot 0..81） | 82 | 生成器按 `Rarities` = 10 Basic/20 Common/32 Uncommon/18 Rare/2 Ancient |

`WatcherChaosRun.SlotCount = 82`，`Rarities` = 10+20+32+18+2。`ChaosWatcherCardBase` 的 rarity 由
`Generated.Rarity` 映射（`AutoAnthony.decompiled.cs:39580`）：Basic→1、Ancient→5，
`FilterForCombat` 排除这两档 ⇒ chaos 池战斗内可生成 `82-10-2 = 70`（AA 自己的造卡路径还会再叠一层
`RandomCombatGenerationCandidates`，即 `CanBeRandomlyGeneratedInCombat`；原版路径没有这一层，
所以 73 与 70 都是**静态上界估计**，精确值需运行时打印）。

> 附带观测（支持"池内容不同"）：原版观者 **Power** 卡战斗内可生成 12 张
> （`WatcherBattleHymn`/`Fasting2`/`Foresight`/`LikeWater`/`MentalFortress`/`Nirvana`/`Rushdown`/`Study`/`DevaForm`/`Devotion`/`Establishment`/`MasterReality`）；
> 包 #1 的分歧正是 **Power Potion** 选牌，房主抽到 `WATCHER_MASTER_REALITY`（原版 Power 池成员），
> 客机抽到 `CHAOS_WATCHER_CARD051`（chaos 池成员）——两端候选集**内容**不同，与"池不同"一致。

#### 池大小差与计数器差 3 的关系（**不能**用"恒定 3"排除池差）

一条看起来很强、实则无效的推理是："若两端候选表长度持续相差 3，差值应随抽取累积增长；
但观测到的差值恒为 3，所以池差不成立。"**该推理作废**，原因是本作的分歧检测方式：

1. 校验和在**每个动作结束后**产生（`RunManager.cs:572`
   `GenerateChecksum($"finished action execution {action}", action)`），并且 `NetFullCombatState`
   **包含 RNG**（`NetFullCombatState.cs:473` `writer.Write(Rng)`；`FromRun` 里 `Rng = runState.Rng.ToSerializable()`）。
   ⇒ 抽取次数差 3 本身**就足以**让校验和不一致。
2. 一旦不一致，房主立即 `DisconnectClient(senderId, NetError.StateDivergence)`
   （`ChecksumTracker.cs:150-152`），**客机被踢出，本局结束**。
   ⇒ 差值**没有机会累积**：我们只能观测到"第一次出现不一致"的那一瞬间的快照，
   而那一刻的差值取决于"从上一次成功比较到首次不一致之间发生了多少次两端消耗不同的调用"。
3. 因此 `+3` 恒定**与**"每次 shuffle 都差 3"**完全相容**（第一次出现时就已被踢，看不到第二次）；
   同样也与"只在某一次调用上差 3"相容。**观测无法区分这两种模型**——这正是 §1.4 里
   "哪一次调用造成这 3 次差，包内材料判定不了"的根据。
4. 另一条路（用 `advance(client_state, 3) == host_state` 逐位相等）证明的是
   "同一条流、相差 3 次抽取"，**不能**证明"这 3 次来自池大小"。

**⇒ 两个症状必须分开归因**：

| 症状 | 归因 | 状态 |
|---|---|---|
| 同一 choice 索引两端解析成**不同卡** | 两端牌池**内容**不同（原版 vs chaos） | 已确认（§1.1/§1.2） |
| `CombatCardGeneration` 计数器差 3 | 房主多消耗 3 次抽取（池大小差或调用次数差） | **机制未定** |

**判别方法**（下一步，测试副本）：按调用点打印候选表长度与 `_counter` 增量，
对比两端每次 `GetDistinctForCombat`/`GetForCombat` 前后的计数器差值；
若每次 shuffle 的 `Δcounter` 都相差 3 ⇒ 池差模型成立；若只有一次相差 3 ⇒ 调用次数差模型成立。

### 2.3 客机那份 `run_options.json` 不可见

`user://AutoAnthonyWatcher/run_options.json` 是**每台机器各一份**的本地文件，客机那份本机无法读取。
若要区分 (A)/(B)，除了下一局开局日志，另一条路是让客机玩家提供该文件。

---

## 3. 未复现

- **未在测试副本同 seed 隔离复现**：需要两端各自跑 AAW，抓 `CombatCardGeneration` 消耗序列与
  `WatcherChaosRun.IsRunActive`/候选池大小。本轮全程只读，未启动游戏。
- **未取得开局阶段的完整日志**：本次 5 个包的 debug-log 均被截断在 run 后段
  （5000 条上限，覆盖 14:04–14:10）。开局行（`Installed the generated Watcher pool`、
  `Captured the host generation carrier`、`Host selected multiplayer generation mode`）**不在窗口内**。
- **未验证 `netIdMap` 那行 `118 SavedProperty net-id slot(s) differ.`**：该段 local/remote 两侧 118 行
  文本**逐行完全相同**，且 `savedProperties.mapHash` 两端都是 `0x0FBB1978`、`count` 两端 118。
  疑为 RitsuLib 报告口径问题（把"被比较"写成"differ"），**不作为结论**。

---

## 4. 修复方向（供后续实现，未改动任何文件）

按证据强度排序：

1. **AAW 读档路径的 `with { Enabled = true }` 应改为"尊重已记录/已协商的开关"**。
   当前写法让 `WatcherRunOptionsStore` 与联机 carrier 的 `Enabled=false` 失效，
   是"房主本局明明关了 chaos、却在某条路径上被重新打开（或被反复开关）"的根源。
   至少要：`Enabled` 仍为 false 时**不要**激活；把 `run_options.json` 的 `Enabled` 纳入守卫。
2. **联机时以房主的 carrier 为唯一权威**，两端都不得用本地设置兜底激活生成池。
   现有 `WatcherSeedBeforeMultiplayerPatch` 已有 carrier 分支，但 fallback 到 `FromLocalSettings()`
   会静默造成两端不一致；建议 fallback 时**直接不激活**（fail-closed），并把 fallback 记为显式告警。
3. **开局把"本局是否启用生成池"写进存档**（AA 的 `ChaosPoolSnapshotModifier.PoolSnapshot` 已有此机制），
   使 `RunState.FromSerializable` 的读档路径能据此恢复，而不是靠"牌组里有没有 chaos 卡"反推。
4. **RNG 差 3 必须单独修**（§1.4：包 #2/#3/#5 卡身份零差异仍断开）。
   在两端**对齐 `CombatCardGeneration` 的消耗序列**：所有走 `GetDistinctForCombat`/`GetForCombat`
   的调用必须两端候选表长度一致（即先修"池内容/池大小"，因为 `TakeRandom` → `UnstableShuffle`
   的消耗 = `list.Count − 1`，候选表长度直接决定抽取次数）。
   若池必须不同（设计如此），则**不要让生成池走 `run.Rng.CombatCardGeneration`**，
   改用不参与校验和的本地流，或把生成结果作为确定性输入同步。
5. 复现与验收：测试副本同 seed 双端开局，断言两端
   `rng.run.rngs.CombatCardGeneration.counter` 与 `FilterForCombat` 候选集大小一致；
   并按调用点打印每次 `GetDistinctForCombat`/`GetForCombat` 的 `Δcounter`
   （用于判别"池差"还是"调用次数差"，见 §2.2）。
6. **本仓 `AutoAnthonyCompatBridge` 待排查（§1.10）**：客机 4 次打印
   `AutoAnthony bridge: Watcher -> generated pool #0.`，房主 0 次。这说明客机上
   `ChaosCharacterMapping.From(Watcher)` 返回 null 被本桥补成 `Ironclad(0)`，即**客机的 AA 认不出观者角色**。
   桥本身对 AAW 观者池无因果（AAW 不走该映射），但**它证明客机与房主的 AA/Watcher 装配状态不同**，
   是排查"为什么只有客机开了 chaos 池"的第一现场。建议下一步在测试副本上打印
   `ChaosCharacterMapping.From(Watcher)` 返回值 + `WatcherChaosRun.IsRunActive`。

---

## 5. 证据索引

| 内容 | 路径 |
|---|---|
| 5 个分歧包（原始） | `G:\appdata\C-Users-o_Obl\Roaming\SlayTheSpire2\logs\ritsulib_state_divergence_20261008_*` |
| 解包 + 分析脚本/输出 | `G:\tmp\div-20261008\`（`q7_final.py`→`Q7_FINAL.txt`、`q6_hands.py`→`Q6_HANDS.txt`、`q8_rng_offset.py`→`Q8_RNG_OFFSET.txt`、`q9_draw_windows.py`→`Q9_DRAW_WINDOWS.txt`、`LOGSCAN.txt`） |
| AAW 0.3.3 反编译（workshop） | `G:\tmp\div-20261008\decomp\host\`（`WatcherSeedBeforeLoadPatch.cs`、`WatcherChaosRun.cs`、`ChaosSettingsBridge.cs`、`WatcherRunOptionsStore.cs`、`WatcherCarrierCapturePatch.cs`、`WatcherCardPoolPatch.cs`） |
| AA 0.3.139 反编译 | `G:\tmp\div-20261008\decomp\aa-host\AutoAnthony.decompiled.cs`（`SeedBeforeLoadPatch` @66849、`SeedBeforeMultiplayerPatch` @66585、`AddGenerationMarker` @64685、`ChaosPoolSnapshotModifier` @53455、`ComponentRunSettingsApi` @59312） |
| Watcher 0.10.0 反编译 | `G:\tmp\div-20261008\decomp\watcher-host2\WatcherMod\WatcherCardPool.cs`（83 张） |
| 引擎源码 | `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\`（`CardFactory.cs`、`ListExtensions.cs`、`StartRunLobby.cs`、`NGame.cs`） |
| 本机 AA 设置 | `G:\appdata\...\SlayTheSpire2\AutoAnthony\settings.json`、`AutoAnthonyWatcher\run_options.json` |

**只读声明**：本轮未写入 Steam 副本（`G:\steam\steamapps\common\Slay the Spire 2`）、
未写入共享 `mod_configs`、未改 mod 顺序或存档、未重启游戏。全部分析产物在 `G:\tmp\div-20261008`。
