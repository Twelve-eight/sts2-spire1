# 卡牌奖励生成管线（Card Reward Pipeline）— StS1 战斗语义知识库

## 本卷范围
战斗胜利后卡牌奖励的完整生成算法：池构建（initializeCardPools，含角色覆写点——chaosbridge 相关）、数量修正（relic/daily）、稀有度滚动（rollRarity + Blizzard 保底计数器的方向与钳制）、去重重试、升级掷骰、预览钩子。与 `../relics.json` 数据层互补。
**图例**：置信度 **高**=字节码直接可证 / **中**=推断（注明）。基准 jar：desktop-1.0.jar v2.x。出处 `AbstractDungeon` / `AbstractRoom` javap 偏移。

---

## 1. 池构建（initializeCardPools）

**R01 池清单与角色覆写点** — 出处 `AbstractDungeon#initializeCardPools`（方法头 offset 0-115）。置信度：**高**（结构）/ **中**（去重细节未逐行）
五个池先 clear：`commonCardPool / uncommonCardPool / rareCardPool / colorlessCardPool / curseCardPool`（另有 `srcUncommonCardPool` 备份池）。随后：
```
CardLibrary.addColorlessCards（"Colorless Cards" 日替再放宽）；"Diverse" 日替 → addRed/Green/BlueCards 全色；
"Watcher" 未解锁则跳过 addPurpleCards；
★ player.getCardPool(list) —— AbstractPlayer 虚方法 = 角色卡池的引擎级覆写点
  （chaosbridge 的 WatcherCardPool patch 与此处语义对应）；
addColorlessCards() 再并入 colorlessCardPool / curseCardPool，最后按 rarity 字段
分发进 common/uncommon/rare 池。
```

## 2. 奖励数量与逐槽生成（getRewardCards）

**R02 数量决定链** — 出处 `AbstractDungeon#getRewardCards` offset 8-59。置信度：**高**
`numCards = 3` → 遗物按**容器顺序**逐个 `changeNumberOfCardsInReward(n)`（可累乘/累加，实现自定）→ "Binary" 日替 -1。

**R03 Blizzard 保底计数器（方向与钳制）** — 出处 offset 108-145 + 静态初始化（AbstractDungeon javap 行 610-626）：置信度：**高**
```
cardBlizzStartOffset=5, cardBlizzGrowth=1, cardBlizzMaxOffset=-40；战斗/初始化时 cardBlizzRandomizer=5
本槽 roll 出 RARE → cardBlizzRandomizer = 5（重置）
本槽 roll 出 UNCOMMON → 不变
本槽 roll 出 COMMON → cardBlizzRandomizer -= 1；若 <= -40 → 钳为 -40
```
**R04 稀有度判定** — 出处 `AbstractDungeon#rollRarity(Random)` offset 0-33 + `AbstractRoom#getCardRarity(int,boolean)` offset 0-124 + `AbstractRoom#<init>` offset 108-116 + `MonsterRoomElite#<init>` offset 30-38。置信度：**高**
```
roll = cardRng.random(99) + cardBlizzRandomizer
if (roll < rareCardChance)     → RARE    （遗物 changeRareCardRewardChance 可调；较 base 提高时 flash）
else if (roll < uncommonCardChance) → UNCOMMON（changeUncommonCardRewardChance）
else                            → COMMON
阈值：普通战斗 baseRare=3 / baseUncommon=37；精英 10 / 40（alterCardRarityProbabilities 为日替钩子）
```
推论：初始 blizz=+5 时 roll ∈ [5,104] ⇒ **开战首张不可能 RARE**（roll<3 需 blizz 先降）；连续 COMMON 把 blizz 推向 -40，roll 下探进入稀有区间——保底方向与 wiki 口传一致且现在有精确系数。

**R05 逐槽抽取与去重重试** — 出处 `getRewardCards` offset 159-250。置信度：**高**
```
do { retry=false;
     card = hasRelic("PrismaticShard") ? CardLibrary.getAnyColorCard(rarity)
                                       : getCard(rarity)      // 按池 getRandomCard(useRng=true)
     若本批已有相同 cardID → retry=true 重抽
   } while (retry);
   card != null → 加入本批
```

**R06 复制/升级/预览段** — 出处 offset 271-427。置信度：**高**
```
逐个 makeCopy() 为返回列表；
if (copy.rarity != RARE && cardRng.randomBoolean(cardUpgradedChance) && canUpgrade()) → copy.upgrade()
   ⇒ RARE 奖励卡永不走此自动升级；
逐 copy 调 relics.onPreviewObtainCard(copy)（遗物梯，容器序）
```
`cardUpgradedChance` 为 per-dungeon 静态字段（各幕构造器赋值，本卷未枚举具体值——开放问题 2）。

**R07 无 RNG 变体** — 出处 `getCardWithoutRng` offset 0-84。置信度：**高**
同分派但 `getRandomCard(false)`（不消耗 RNG），RARE 槽在无 RNG 场景改从 `returnRandomCurse()` 取诅咒池。

---

## 3. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| 战斗开局第一张奖励（无任何影响） | RARE 不可能（blizz=+5），UNCOMMON/COMMON 分界 37 | R04 推论 |
| 连续多张 COMMON | blizz 递减 → 后续 roll 整体下移 → RARE/UNCOMMON 概率上升 | R03/R04 |
| 精英战后 | rare/unc 阈值 10/40，且 blizz 计数器延续全局 | R04 |
| 问号牌屋/Prismatic Shard | 抽取改走全色库，稀有度流程不变 | R05 |
| 重复 cardID | 同槽重抽（去重循环），池空则可返回 null → 该槽被跳过（列表少一张） | R05 |
| RARE 卡 + 高升级概率遗物 | 不自动升级（RARE 被显式排除） | R06 |
| 你的影响奖励数量遗物 | 按遗物容器顺序链式修改 | R02 |

## 4. 事件赠卡普查（StS1 侧）

**R08 事件用池 API 全量（56 事件类，工具 tools/stS1-event-pool-usage.js）** — 出处 javap 常量方法引用扫描。置信度：**高**
```
AbstractDungeon.getCard(rarity)：GremlinMatchGame、TheLibrary（池随机奖品型）
returnRandomCurse：GremlinMatchGame
CardLibrary.getCopy(指定卡)：AccursedBlacksmith、BigFish、DrugDealer、ForgottenAltar、
  MindBloom、Mushrooms、Sssserpent、TheMausoleum、WindingHalls（事件按卡 ID 精确赠卡）
returnRandomRelic（遗物侧）：Addict、BigFish、DeadAdventurer、GremlinWheelGame、
  ScrapOoze、TheMausoleum、WeMeetAgain
CardLibrary.getCard：NoteForYourself
```
**R09 两代范式对照** — 结论。置信度：**高**
StS1 事件赠卡以**指定卡 ID（CardLibrary.getCopy）为主、池随机为辅**；StS2 事件几乎全部走 `CreateForReward + 池`（sts2-event-pool-usage.md E02，仅 ColorfulPhilosophers 硬编码色序）。⇒ 移植 StS1 事件到 StS2 时"指定卡"要改写为 FromCard 池外注入或专用 options；移植 StS2 事件到 StS1 时要建池或 getCopy 化。跨代移植事件不共享同一赠卡 API 范式。

## 6. 开放问题 / 低置信项

1. `initializeCardPools` 中 srcUncommonCardPool 备份的消费方（transform/去重辅助）未穷举。置信度：**中**。
2. ~~各幕 cardUpgradedChance 具体值~~ **已结案**（2026-09-05 各幕构造器直证）：Exordium=0.0、TheCity=0.125、TheBeyond=0.25、TheEnding=飞升<12 为 0.5 / ≥12 为 0.25（TheEnding ctor 条件分支）。置信度：**高**。
3. `changeNumberOfCardsInReward` 的 vanilla 实现者清单未枚举（Question Card? 类）。
4. Boss/商店/事件的奖励生成走 `RewardItem` 其他分支，本卷只覆盖战斗胜利卡池路径。置信度：**范围声明**。
