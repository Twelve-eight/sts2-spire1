# Keys & the Final Act（钥匙与第四层）— StS1 原版机制

> 权威来源：`desktop-1.0.jar` 反编译（javap）——`AbstractChest.open`、`AbstractRoom.addSapphireKey`、`AbstractDungeon`（finalize 前置检查）、`Settings.hasRubyKey/hasEmeraldKey/hasSapphireKey`。字节码锚点见 `research/sts1-javap/AbstractRoom.txt:1459-1469`、`AbstractDungeon.txt:1150-1158`。

## 三钥匙与第四层入口

- 第四层（Act 4 / The Ending）需要同时持有 **Ruby / Emerald / Sapphire** 三把钥匙（`AbstractDungeon`：`hasRubyKey && hasEmeraldKey && hasSapphireKey` 全真才放行，任一为假走普通三幕结局）。
- 钥匙不是遗物，是 `Settings` 上的三个布尔位；对应 `RewardItem.RewardType.SAPPHIRE_KEY` 等奖励类型。

## 蓝宝石钥匙：宝箱二选一（本页核心知识）

字节码（`AbstractChest.open(boolean)`，栈序）：

```
261: getstatic  Settings.isFinalActAvailable
264: ifeq  302                      // 未解锁终局 → 不加钥匙
267: getstatic  Settings.hasSapphireKey
270: ifne  302                      // 已有钥匙 → 不加
273-293: currRoom.rewards.get(size-1)   // 取最后一个奖励项
299: addSapphireKey(rewardItem)         // 以它为模板构造 SAPPHIRE_KEY 奖励并追加
```

**规则**（打开**非 Boss**宝箱时，若终局可用且尚无蓝宝石钥匙）：
- 奖励列表会**同时显示**宝箱遗物与蓝宝石钥匙两个选项；
- 两者**互斥**：选择其中一个会移除另一个——拿钥匙 = 放弃该宝箱的遗物，反之亦然；
- `isFinalActAvailable` 为假（未满足解锁条件）或已持有时不再出现。

## 其余两把

- **Ruby（红宝石）/ Emerald（翡翠）**：分别来自固定位置的事件奖励（Boss 宝箱不提供蓝宝石钥匙）。
- 三者持有状态只属于 `Settings`，与遗物栏完全独立。

## 测试基建含义（autoslay）

autoslay 的奖励拾取策略总是选宝箱**遗物**，因此它永远不会获得蓝宝石钥匙 → 三幕打完无法进入第四层 → "Act transition did not complete (VisitedMapCoords not cleared)" 超时。这是测试器策略局限，不是游戏 bug（历史 244 局中 4 局同款）。要打通第四层需让 autoslay 在"终局可用且无蓝宝石钥匙"的宝箱奖励里优先选钥匙项。

## StS2 现状（对照）

StS2 引擎（v0.111.0 sts2.dll）**没有**任何 RubyKey/EmeraldKey/SapphireKey 类型（ilspycmd 全类型清单核实）；玩家存档中的 `RELIC.RUBY_KEY` 来自第三方 mod（如 Act 4 Heart），以遗物形态模拟钥匙。两代机制不同源，移植第四层时不可直接按 StS1 字节码照搬。
