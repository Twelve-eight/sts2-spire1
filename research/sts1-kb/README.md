# sts1-kb — SlayTheSpire 一代权威知识库

StS2 BaseLib 移植项目的 StS1 数据基线。**所有数值来自游戏字节码，所有文本来自官方本地化原文**（双语 en+zh），未做任何人工翻译或臆造。

## 来源

| 内容 | 权威源 | 置信度 |
|---|---|---|
| 卡牌 id / 费用 / 类型 / 颜色 / 稀有度 / 目标 | 反编译 `com/megacrit/cardcrawl/cards/**` 构造器字节码 | 最高（直接读 super(...) 实参常量） |
| 升级后费用 | 各卡 `upgrade()` 方法中的 `upgradeBaseCost(I)` / `upgradeCost(I)` 字节码 | 最高 |
| 遗物 tier | `com/megacrit/cardcrawl/relics/*` 构造器 `RelicTier` 枚举实参 | 最高 |
| 药水 rarity | `com/megacrit/cardcrawl/potions/*` 构造器 `PotionRarity` 枚举实参 | 最高 |
| 名称 / 描述 / 风味文本 | jar 内 `localization/{eng,zhs}/*.json`（官方简中=zhs） | 最高（原文照录） |
| 关键词标注 | `localization/{eng,zhs}/keywords.json` 官方 Game Dictionary 词表扫描 | 中（启发式匹配，见下） |

注意：本地化 JSON **不在** `localizations/`（复数）路径——本版本 jar 内实际路径为单数 `localization/<lang>/`，共 17 个文件/语言。磁盘上的 `SlayTheSpire/localization/eng/events.json` 是残留的用户文件，未采用。

## 重新生成 / 对账

```bash
cd "G:/omp works/sts2-spire1/research/sts1-kb"
node build_kb.mjs "G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar"
```

- 全程只读 jar；无 C 盘临时文件；脚本纯 Node 标准库（自带 ZIP/JVM class 解析器，无第三方依赖）。
- 抽查某张卡的字节码（人工对账用）：

```bash
unzip -o -q desktop-1.0.jar "com/megacrit/cardcrawl/cards/red/Barricade.class" -d .tmp/jcls
javap -p -c .tmp/jcls/com/megacrit/cardcrawl/cards/red/Barricade.class   # 看 <init> 与 upgrade()
```

- 与 wiki 交叉校验时以本库为准；发现不一致先跑上面 javap 复核字节码。

## 条目统计（与本 jar 完全一致）

| 文件 | 条目 | 说明 |
|---|---|---|
| cards-red.json | 75 | 战士（含基础牌） |
| cards-green.json | 75 | 猎手 |
| cards-blue.json | 75 | 机器人 |
| cards-purple.json | 77 | 观者（契约外补充：本 jar 为 v2.x，含观者） |
| cards-colorless.json | 39 | 无色 |
| cards-curses.json | 14 | 诅咒 |
| cards-status.json | 5 | 状态牌 |
| cards-tempCards.json | 9 | 临时牌（Shiv/Miracle 等） |
| cards-optionCards.json | 5 | 观者愿望选项牌 |
| cards-deprecated.json | 64 | 废弃/不可获得（63 个 jar 类 + 1 个无本地化的 Impulse，见下） |
| relics.json | 186 | 含 6 个 Beta 测试遗物（Test 1/3/4/5/6，官方原文 eng 名称即为空串） |
| potions.json | 43 | 含 PotionSlot（rarity=PLACEHOLDER） |
| events.json | 54 | 事件文本键 |

卡牌合计 438 条 = jar 内 438 个非抽象卡牌类。relics/potions 每条均与字节码类一一对应且在 eng+zhs 双语齐全。

## 卡牌 schema

```jsonc
{
  "id": "Barricade",                  // 游戏内 card id（super 第 1 参）
  "class": "Barricade",               // 类名（对账用）
  "color": "RED",                     // CardColor 枚举
  "name_en": "...", "name_zh": "...",
  "type": "POWER",                    // ATTACK/SKILL/POWER/STATUS/CURSE
  "rarity": "RARE",                   // BASIC/COMMON/UNCOMMON/RARE/SPECIAL/CURSE
  "cost": 3,                          // 基础费用；-1=X 费；-2=不可打出
  "cost_upgraded": 2,
  "cost_upgraded_source": "upgradeBaseCost(2)",  // 费用变化来源；"unchanged"=升级不变
  "target": "SELF",                   // CardTarget 枚举
  "description_en": "...",            // 原文，保留 !D!/!B!/!M!/NL/#y/[E] 等占位符
  "description_zh": "...",
  "upgraded_description_diff": {"en":"...","zh":"..."} | null,  // UPGRADE_DESCRIPTION 原文；null=升级不改描述
  "keywords": ["EXHAUST","ETHEREAL"]  // 官方关键词词典命中项（大写枚举键）
}
```

扩展字段（契约外补充）：`class`、`color`、`cost_upgraded_source`、`keywords`；遗物/药水另含 `flavor_*`，事件含 `options_*`。

## 已知边界情况

1. **Impulse**（blue 包）：类存在但本版本无任何本地化条目，不可获得 → 移入 cards-deprecated.json 并加 `note`。
2. **Blood for Blood**：upgrade() 为分支逻辑（cost<4 时 cost-1，否则设 3）；基础费 4 → 取 else 支 3。
3. **Searing Blow**：无限升级卡，费用恒为 2。
4. **Beta 测试遗物 Test 1/3/4/5/6**：eng NAME 为空字符串系官方数据原样。
5. **Proceed Screen**：events.json 中的伪事件键（无 NAME），保留原样。
6. **keywords**：按官方 keywords.json 的 NAMES 词表对双语文本做词界匹配（EN 词界正则 / ZH 子串），属检索启发式，非游戏运行时判定；BLOCK 等泛词命中不代表机制相关。
7. 费用语义：`-1`=X 费（Whirlwind/Skewer/Tempest 等），`-2`=不可打出（诅咒/状态等）。升级后费用仅统计 upgrade() 内的字面量操作；运行时动态减费（如 Streamline 效果）不属于 base cost。

## 语言卫生

全部内容仅为官方英文（eng）与官方简体中文（zhs）原文照录，JSON 输出 UTF-8、非 ASCII 不转义。
