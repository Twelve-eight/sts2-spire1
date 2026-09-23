# sts1-kb - SlayTheSpire 一代数据知识库

StS2 BaseLib 移植项目的 StS1 数据基线. 生成器从输入 JAR 的字节码提取数值字段, 从 eng/zhs 本地化提取文本. **当前历史快照不保证文本逐字符原样一致, 同源重提取也不等于独立数值核验或完整官方版本认证.**

本轮输入指纹, 13 份重提取与 14 份文件清点的区别, 标点有损映射及来源缺口见 [2026-09-23 重验说明](G:/omp%20works/Sts/sts2-spire1/research/sts1-kb/REVALIDATION-2026-09-23.md).

## 来源

下表描述生成器的提取渠道及原有置信度分级, 不是逐条数值或官方版本身份已经独立核验的声明. 当前证据等级以重验说明为准.

| 内容 | 权威源 | 置信度 |
|---|---|---|
| 卡牌 id / 费用 / 类型 / 颜色 / 稀有度 / 目标 | 反编译 `com/megacrit/cardcrawl/cards/**` 构造器字节码 | 最高（直接读 super(...) 实参常量） |
| 升级后费用 | 各卡 `upgrade()` 方法中的 `upgradeBaseCost(I)` / `upgradeCost(I)` 字节码 | 最高 |
| 遗物 tier | `com/megacrit/cardcrawl/relics/*` 构造器 `RelicTier` 枚举实参 | 最高 |
| 药水 rarity | `com/megacrit/cardcrawl/potions/*` 构造器 `PotionRarity` 枚举实参 | 最高 |
| 名称 / 描述 / 风味文本 | jar 内 `localization/{eng,zhs}/*.json`（官方简中=zhs） | 本地化提取;历史快照存在标点有损差异 |
| 关键词标注 | `localization/{eng,zhs}/keywords.json` 官方 Game Dictionary 词表扫描 | 中（启发式匹配，见下） |

注意：本地化 JSON **不在** `localizations/`（复数）路径--本版本 jar 内实际路径为单数 `localization/<lang>/`，共 17 个文件/语言。磁盘上的 `SlayTheSpire/localization/eng/events.json` 是残留的用户文件，未采用。

## 重新生成 / 对账

下列是原地生成入口, 会覆盖脚本所在目录的 13 份 JSON, 不生成 monsters-scan.json. 审计重提取应先把原样脚本复制到经授权的全新 G: 隔离目录再运行, 不覆盖当前快照或既有证据. 本次文档修订未执行这些命令.

```bash
cd "G:/omp works/Sts/sts2-spire1/research/sts1-kb"
node "G:/omp works/Sts/sts2-spire1/research/sts1-kb/build_kb.mjs" "G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar"
```

- build_kb.mjs 只读输入 JAR, 输出写到脚本所在目录;脚本使用 Node 标准库(自带 ZIP/JVM class 解析器,无第三方依赖). 本轮已有隔离运行的输入前后哈希, 运行时版本和输出清单见重验说明, 不外推为任意运行环境的无 C: 写入保证.
- 抽查某张卡的字节码（人工对账用）：

```bash
unzip -o -q "G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar" "com/megacrit/cardcrawl/cards/red/Barricade.class" -d "G:/omp works/Sts/sts2-spire1/research/sts1-kb/.tmp/jcls"
"C:/Program Files/Zulu/zulu-21/bin/javap.exe" -p -c "G:/omp works/Sts/sts2-spire1/research/sts1-kb/.tmp/jcls/com/megacrit/cardcrawl/cards/red/Barricade.class"   # 看 <init> 与 upgrade()
```

- 与其它资料不一致时, 应核对已固定身份的原版制品与实际调用链, 不因本库标题或旧置信度分级而跳过独立复核.

## 条目统计(当前快照清点,非完整性或可获得性认证)

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
| cards-deprecated.json | 64 | 生成器 deprecated 分组,含原包记录与缺本地化转入记录;可达性未核验 |
| relics.json | 186 | 类扫描记录,含测试条目和空名称;正式可获得范围未核验 |
| potions.json | 43 | 含 PotionSlot（rarity=PLACEHOLDER） |
| events.json | 54 | 事件文本键 |

十份卡牌文件合计 438 条. 上表 13 份 JSON 均在本轮重提取清单内, 数量相符不证明全部类覆盖, 逐条数值正确或双语字段齐全. 根目录另有 monsters-scan.json 的 73 条类扫描记录, 含 AbstractMonster, 不等于 73 个实际可遇见怪物;该文件未被本轮生成链覆盖, 来源关系待补齐.

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
  "description_en": "...",            // 本地化文本字段,含 !D!/!B!/!M!/NL/#y/[E] 等占位符
  "description_zh": "...",
  "upgraded_description_diff": {"en":"...","zh":"..."} | null,  // UPGRADE_DESCRIPTION 提取文本;null=未提取到升级描述字段
  "keywords": ["EXHAUST","ETHEREAL"]  // 官方关键词词典命中项（大写枚举键）
}
```

扩展字段（契约外补充）：`class`、`color`、`cost_upgraded_source`、`keywords`；遗物/药水另含 `flavor_*`，事件含 `options_*`。

## 已知边界情况

1. **deprecated 分组与 Impulse**:生成器将双语名称均为空的非 deprecated 包记录转入 cards-deprecated.json 并加 `note`. 这只能证明提取时未取得名称, 不能证明原版不可获得. 输出未保留原包和完整 class 路径, Impulse 的包来源与运行时可达性须另核注册或调用链;不以分类结果替代该证据.
2. **Blood for Blood**：upgrade() 为分支逻辑（cost<4 时 cost-1，否则设 3）；基础费 4 -> 取 else 支 3。
3. **Searing Blow**：无限升级卡，费用恒为 2。
4. **测试遗物条目**:部分 eng NAME 为空, 此处只描述当前快照;测试类存在不等于正式游戏可获得, 也不单独证明官方版本身份.
5. **Proceed Screen**：events.json 中的伪事件键（无 NAME），保留原样。
6. **keywords**：按官方 keywords.json 的 NAMES 词表对双语文本做词界匹配（EN 词界正则 / ZH 子串），属检索启发式，非游戏运行时判定；BLOCK 等泛词命中不代表机制相关。
7. 费用语义：`-1`=X 费（Whirlwind/Skewer/Tempest 等），`-2`=不可打出（诅咒/状态等）。升级后费用仅统计 upgrade() 内的字面量操作；运行时动态减费（如 Streamline 效果）不属于 base cost。

## 语言卫生

生成器仅读取 eng 与 zhs 本地化, JSON 输出为 UTF-8. 当前 events.json 与 relics.json 相对本次重提取存在解析后的字符串差异, 并非仅 JSON 转义写法不同. 主会话已验证限定标点映射后对象相等, 但映射有损, 不称为原文照录或原始数据一致, 不据此覆盖或反向修复任何 JSON. 本说明只引用路径, 字段名和码点编号, 不复制多语言原文.
