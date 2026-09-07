# 战利品奖励管线：药水/宝箱/商店（Loot Rewards）- StS1 战斗语义知识库

## 本卷范围
卡牌奖励（`card-rewards.md`）之外的战利品：药水掉落稀有度滚动、宝箱类型与钥匙、商店定价公式与 RNG 分账、金币已录内容的交叉引用。
**图例**：**高**=字节码直接可证 / **中**=推断（注明）。出处 `AbstractDungeon` / `PotionHelper` / `TreasureRoom` / `ShopScreen` / `AbstractCard` javap 偏移。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 药水掉落（PotionHelper / returnRandomPotion）

**L01 稀有度滚动** - 出处 `AbstractDungeon#returnRandomPotion()` offset 0-51 + `PotionHelper` 静态初始化（`POTION_COMMON_CHANCE=65`、`POTION_UNCOMMON_CHANCE=25`，bipush 直证）。置信度：**高**
```
roll = potionRng.random(0, 99)
roll < 65        -> COMMON   （getRandomPotion 内按稀有度池抽取）
roll < 65+25=90  -> UNCOMMON
else             -> RARE     （隐含 10%）
```
独立 RNG：`potionRng`（与 cardRng/treasureRng/merchantRng 并列，M12 分账）。`returnRandomPotion(boolean useRng)` 与恒定 `Fruit Juice` 排除分支（offset 20-40 常量池）并存。

## 2. 宝箱（TreasureRoom / getRandomChest）

**L02 宝箱类型滚动** - 出处 `AbstractDungeon#getRandomChest`（`treasureRng.random(99)` 与 `smallChestChance` 比较 -> SmallChest，后续分支到其余箱型）。置信度：**高**（结构+RNG）/ **中**（三档箱型各自概率字段值未枚举--per-dungeon 静态字段，开放问题 2）
**L03 翡翠钥匙** - 出处 `TreasureRoom` 构造/入场段（`0.2f` 常量：宝箱为翡翠钥匙携带者的掷骰）。置信度：**高**（常量存在）/ **中**（该常量语义结合 keys-and-final-act.md 的蓝宝石/翡翠二选一推断为 20% 携带率）

## 3. 商店（ShopScreen）

**L04 卡牌定价公式** - 出处 `ShopScreen#initCards` offset 57-79。置信度：**高**
```
price = AbstractCard.getPrice(rarity) x merchantRng.random(0.9, 1.1)
```
**L05 基础价表** - 出处 `AbstractCard#getPrice`（tableswitch 1-6：50 / 75 / 150 / 9999 / 9999 常量直证）。置信度：**高**（数值）/ **中**（枚举位映射按标准序推断：COMMON=50、UNCOMMON=75、RARE=150、SPECIAL/CURSE=9999；BASIC 位未单独取证）
推论：普通卡商店价 ~ 45-55，罕见 ~ 67-82，稀有 ~ 135-165--与社区经验一致且公式实证。
**L06 定价变体位** - 出处 `ShopScreen` 其余 `merchantRng` 调用点（offset 205/329/1082/1126 区域）：遗物/药水牌位同构 x(0.9-1.1) 浮动 + OnSaleTag（五折标签）与 VIP 特价位的存在性。置信度：**中**（结构）/ 各位精确公式未逐行展开（开放问题 3）。

**L07 金币奖励交叉引用** - 普通战斗 10-20（treasureRng）、精英 25-35、Boss 100+/-5（asc13+ x0.75）：见 triggers.md R10（本卷不重复）。

---

## 4. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| 检查连续药水掉落 | 稀有度独立三段 65/25/10，potionRng 独立流 | L01 |
| 宝箱开出 | treasureRng 决定箱型 -> 箱内 relic 走 returnRandomRelic(tier)（relicRng 相关，另见 relics 数据层） | L02 |
| 商店普通卡价格 | 50 x U(0.9,1.1) = 45-55 | L04/L05 |
| 打折标签 | 存在独立 OnSaleTag 机制位 | L06 |
| 多 RNG 流移植 | card/treasure/merchant/potion 四流独立种子，禁止共用 | L01/L02/L04 + M12 |

## 5. 遗物掉落池（returnRandomRelic 族）

**L12 五池弹头式发放** - 出处 `AbstractDungeon#returnRandomRelicKey(RelicTier)`（javap 行 2247-2450 一带）。置信度：**高**
```
池 = common/uncommon/rare/shop/bossRelicPool（ArrayList<String>，初始化时按 relicRng 洗好）
COMMON:  commonRelicPool.isEmpty() -> 递归转 UNCOMMON
UNCOMMON: uncommonRelicPool.isEmpty() -> 递归转 RARE
RARE:     rareRelicPool.isEmpty() -> key = "Circlet"（占位遗物兜底）
取卡 = pool.remove(0)      <- * 恒弹头部（iconst_0 直证）
```
要点：**洗牌一次、按序弹头** => 同一局内遗物不重复（remove 即出池）、且发放顺序由初始化洗牌完全确定（relicRng 流）。空池三级降级链 common->uncommon->rare->Circlet 与社区认知一致且有字节码实锚。返回侧 `returnRandomRelic(tier)` = key -> CardLibrary.getRelic 包装。

## 5.1 池初始化（L13，2026-09-07 补）

**L13 五池初始化全链** - 出处 `AbstractDungeon#initializeRelicList`（javap 行 3687-3900 一带，由各幕 ctor 顺序调用，Exordium ctor offset 12 直证）+ `RelicLibrary#populateRelicPool`（offset 0-480）。置信度：**高**
```
1. 清空五池（common/uncommon/rare/shop/bossRelicPool）
2. 逐池 populateRelicPool(pool, tier, chosenClass)：
   - sharedRelics 全表扫：tier 匹配 && (!UnlockTracker.isRelicLocked(key)
     || Settings.treatEverythingAsUnlocked) -> pool.add(key)
   - tableswitch(chosenClass)：red/green/blue/purpleRelics 色池同规则并入
     （STARTER/SPECIAL 不入池；色遗物只对对应角色可见）
3. floorNum >= 1（续局）时：player.relics 的 relicId 全部加入 relicsToRemoveOnStart
4. 自定义模式互斥遗物追加到 relicsToRemoveOnStart：
   Flight|Uncertain Future -> WingedGreaves；Diverse -> PrismaticShard；
   DeadlyEvents -> Juzu Bracelet；Hoarder -> Smiling Mask；
   Draft|SealedDeck|Shiny|Insanity 任一 -> Pandora's Box
5. 五池各自 Collections.shuffle(pool, new Random(relicRng.randomLong()))
   -> 洗牌消费 5 次 randomLong，顺序固定：common -> uncommon -> rare -> shop -> boss
6. 对每个 toRemove 条目：五池内 equals 匹配则 iterator.remove()（"removed." 日志）
```
要点：**已拥有/互斥遗物在洗牌后剔除**（步骤 6 在步骤 5 之后），即剔除不消耗 relicRng；returnRandomRelicKey 的弹头顺序（L12）只由步骤 5 的洗牌决定。ENDLESS 模式下 Exordium ctor 会在 floorNum<=1 时重置 blightPool（Exordium offset 21-44）。复活/续局（floorNum>=1 分支）把已持有遗物全部移出候选，与"同局不重复"双保险。

## 5.2 商店库存全解（L14，2026-09-07 补；开放问题 2/3 结案）

**L14 商店库存与商户 Rng 消费全链** - 出处 `ShopScreen#initCards/initRelics/initPotions/rollRelicTier` + `StoreRelic/StorePotion` ctor + `OnSaleTag`。置信度：**高**
```
牌位（initCards）：
  彩色卡 5 张（ctor 传入的 coloredCards）：price = getPrice(rarity) x merchantRng.random(0.9f,1.1f) 取整
  无色卡 2 张（colorlessCards）：同式后再 x1.2f（offset 216-223 无色加价 20%）
  OnSaleTag：彩色卡组 merchantRng.random(0,4) 随机 1 张 price/2（offset 325-363），
             daily run 不豁免（该 roll 无 isDailyRun 门）
遗物位（initRelics，3 槽）：
  槽 0/1：rollRelicTier() = merchantRng.random(99)：<48 COMMON，48-81 UNCOMMON，>=82 RARE
          -> returnRandomRelicEnd(tier)（走 shopRelicPool/bossRelicPool 之外的常规池体系）
  槽 2：恒 RelicTier.SHOP -> returnRandomRelicEnd(SHOP)
  价格：StoreRelic ctor price = relic.getPrice()；非 daily 再 x merchantRng.random(0.95f,1.05f) 取整
药水位（initPotions，3 槽）：
  returnRandomPotion()（potionRng 65/25/10，见 L01）
  价格：StorePotion ctor price = potion.getPrice()；非 daily 再 x U(0.95,1.05) 取整
重定价（getNewPrice，玩家获折扣遗物后触发刷新）：
  relic/potion 各自重掷 xU(0.95,1.05)，再叠 Courier x0.8 -> Membership Card x0.5
```
要点：卡位/遗物位/药水位的价格浮动区间**不同**（卡 0.9-1.1，遗物/药水 0.95-1.05，均 merchantRng 流）。无色卡固定 +20% 底价乘数。A16 进阶折扣（ascension.md A16）发生在 ctor 最前（applyDiscount(1.1f,false)），先于上述所有定价位。折扣链完整顺序：A16 x1.1 -> 位浮动 -> OnSaleTag/2（卡）或 Courier/Membership（遗物重定价时）。

## 6. 开放问题 / 低置信项

1. ~~三档宝箱概率字段~~ **全量结案**（2026-09-05 四幕构造器直证）：Exordium/TheCity/TheBeyond = small 50 / medium 33 / large 17；**TheEnding = 0 / 100 / 0**（只出中箱；同 ctor：treasureRoomChance=0、eventRoomChance=0.22、eliteRoomChance=0.08）。置信度：**高**。
2. ~~商店遗物/药水位与 OnSaleTag 的完整公式~~ **已结案**（2026-09-07，L14）：遗物/药水位 xU(0.95,1.05) 取整；OnSaleTag = 彩色卡组内 merchantRng.random(0,4) 一张半价；无色卡底价 x1.2；遗物槽 tier roll 48/34/18（槽 3 恒 SHOP）。
3. ~~returnRandomRelic 族~~ **已结案**（L12）。
4. BASIC 牌 getPrice 位（普通卡组牌不入店，理论 9999 或 50 未终验）。
