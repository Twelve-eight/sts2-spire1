> **判读指南**（2026-08-25 夜间抽查定级）：低相似度≠错误。
> - A类·机制适配（保留）：StS2 无对应机制的刻意改写。已证：MEDITATE（无姿态API）、ZAP 等 Defect 牌的措辞本地化。
> - B类·排版差异（忽略）：占位符写法、空格、NL 换行差异；本脚本已尽量归一化仍会残留。
> - C类·真漂移（待修）：确认样例——ZAP 硬编码"1 道"未用 !M! 占位符；INJURY/CLUMSY/ASCENDERS_BANE 我方为空描述（官方有"不能被打出"等），建议补齐保图鉴完整。
> 逐条人工裁决以上队列后再动 loc 文件。

# 本地化漂移报告（vs 官方 StS1 原文）

生成：2026-08-24T18:08:46.229Z

- 我方卡描述条目：318
- 对上官方 KB：274
- KB 未命中：44（StS2 原生复用项/衍生牌属正常）

## 相似度最低 30 条（人工复核队列）

| 卡 | zhs | eng |
|---|---|---|
| INJURY | 0% | 0% |
| CLUMSY | 0% | 0% |
| ASCENDERS_BANE | 0% | 0% |
| MEDITATE | 15% | 92% |
| ZAP | 25% | 85% |
| PARASITE | 26% | 79% |
| ENVENOM | 29% | 88% |
| TERROR | 33% | 62% |
| EXPUNGER | 41% | 91% |
| DOUBT | 41% | 52% |
| DEVA_FORM | 42% | 72% |
| OUTMANEUVER | 47% | 60% |
| BURN | 48% | 93% |
| SEEING_RED | 50% | 58% |
| DECAY | 50% | 56% |
| SHAME | 50% | 53% |
| STORM | 50% | 93% |
| ADRENALINE | 53% | 55% |
| BOUNCING_FLASK | 53% | 87% |
| BLOODLETTING | 56% | 58% |
| CONSERVE_BATTERY | 58% | 68% |
| CONCENTRATE | 58% | 69% |
| CRIPPLING_POISON | 60% | 79% |
| MIRACLE | 60% | 26% |
| DISCIPLINE | 62% | 89% |
| JUDGEMENT | 66% | 100% |
| VOID | 67% | 73% |
| FISSION | 69% | 78% |
| HEEL_HOOK | 70% | 73% |
| SUNDER | 70% | 81% |

## zhs 相似度 <85% 明细

### INJURY（0%）
- 我方：
- 官方： 不能被打出 。

### CLUMSY（0%）
- 我方：
- 官方： 不能被打出 。 NL 虚无 。

### ASCENDERS_BANE（0%）
- 我方：
- 官方：不能被打出 。 NL 虚无 。 NL 不能从牌组中移除。

### MEDITATE（15%）
- 我方： 保留 你的手牌最多 !C! 张。
- 官方：将弃牌堆中的一张牌放入你的手牌，并将其 保留 。 NL 进入 平静 。 NL 结束你的回合。

### ZAP（25%）
- 我方： 引导 1 道 闪电 。
- 官方：生成 !M! 个 闪电 充能球。

### PARASITE（26%）
- 我方：如果这张牌被消除,受到 !MaxHp! 点伤害。
- 官方： 不能被打出 。 NL 如果这张牌在你的牌组中被转化或移除，你失去3点最大生命。

### ENVENOM（29%）
- 我方：你的攻击牌会造成 中毒 。
- 官方：每有一次攻击造成未被格挡的伤害，就给予1层 中毒 。

### TERROR（33%）
- 我方：使敌人获得 !VulnerablePower! 层 易伤 。
- 官方：给予99层 易伤 。 NL 消耗 。

### EXPUNGER（41%）
- 我方：造成 !D! 点伤害 !Repeat! 次,从所有敌人身上吸收 3 层 中毒 。
- 官方：造成 !D! 点伤害 X 次。

### DOUBT（41%）
- 我方：每回合开始获得 1 层 虚弱 。
- 官方： 不能被打出 。 NL 在你的回合结束时，获得1层 虚弱 。

### DEVA_FORM（42%）
- 我方： 每个回合开始时,获得 !DevaFormPower! 层 空间 。
- 官方： 虚无 。 NL 在你的回合开始时获得 [W] ，每回合增加 !M! 。

### OUTMANEUVER（47%）
- 我方：下一回合
获得 !E! 点 *能量* 。
- 官方：下一回合 NL 获得 [G] [G] 。

### BURN（48%）
- 我方：回合结束时受到 !D! 点伤害。
- 官方： 不能被打出 。 NL 在你的回合结束时，你受到2点伤害。

### SEEING_RED（50%）
- 我方：获得 2 点 *能量* 。
消耗 。
- 官方：获得 [R] [R] 。 NL 消耗 。

### DECAY（50%）
- 我方：回合结束时受到 !D! 点伤害。
- 官方： 不能被打出 。 NL 在你的回合结束时，受到2点伤害。

### SHAME（50%）
- 我方：回合结束时获得 1 层 柔弱 。
- 官方：不能被打出 。 NL 在你的回合结束时，获得1层 脆弱 。

### STORM（50%）
- 我方： 每当你打出一张 势 牌, 引导 1 道 闪电 。
- 官方：你每打出一张能力牌， 生成 1 个 闪电 充能球。

### ADRENALINE（53%）
- 我方：获得 !E! 点 能量 。抽 !C! 张牌。
 消耗 。
- 官方：获得 [G] 。 NL 抽2张牌。 NL 消耗 。

### BOUNCING_FLASK（53%）
- 我方：施加 !PoisonPower! 层 中毒 ,共 !Repeat! 次。
- 官方：随机给予敌人3层 中毒  !M! 次。

### BLOODLETTING（56%）
- 我方：获得 !E! 点 *能量* 。
失去 3 点生命。
- 官方：获得 [R] [R] 。 NL 失去 3 点生命。

### CONSERVE_BATTERY（58%）
- 我方：获得 !B! 点 *格挡*。下回合，获得 !E! 点 *能量*。
- 官方：获得 !B! 点 格挡 。 NL 在下一回合获得 [B] 。

### CONCENTRATE（58%）
- 我方：丢弃 !C! 张牌。
获得 !E! 点 *能量* 。
- 官方：丢弃 !M! 张牌。 NL 获得 [G] [G] 。

### CRIPPLING_POISON（60%）
- 我方：对所有敌人给予 !PoisonPower! 层 *中毒* 和 !WeakPower! 层 *虚弱*。
- 官方：给予所有敌人 !M! 层 中毒 和2层 虚弱 。 NL 消耗 。

### MIRACLE（60%）
- 我方： 保留 。
 获得 !E! 点 能量 。
 消耗 。
- 官方： 保留 。 NL 获得 [W] 。 NL 消耗 。

### DISCIPLINE（62%）
- 我方：若你的回合结束时仍有未使用的 *能量* ，下回合额外抽等量的牌。
- 官方：如果你在回合结束时有未使用的 [W] ，则在下一回合额外抽相应张数的牌。

### JUDGEMENT（66%）
- 我方：若敌人的生命值不高于 !Threshold! ，将其生命值设为 0。
- 官方：如果目标敌人的生命值小于等于 !M! 点， NL 则将其生命值变为0。

### VOID（67%）
- 我方： 不能被打出 。
当你抽到这张牌时,获得 !E! 点 能量 。
- 官方：不能被打出 。 NL 抽到这张牌时失去1点能量。 NL 虚无 。

### FISSION（69%）
- 我方：移除你的所有 充能球 ,每移除一个便获得 1 点 能量 并抽 1 张牌。
 消耗 。
- 官方：移除所有 充能球 ，每移除一个充能球获得 [B] 并抽 !M! 张牌 。 NL 消耗 。

### HEEL_HOOK（70%）
- 我方：造成 !D! 点伤害。如果敌人有 虚弱 ,获得 !E! 点 能量 并抽 !C! 张牌。
- 官方：造成 !D! 点伤害。 NL 如果敌人有 虚弱 状态 ， NL 获得 [G] 并且 NL 抽1张牌。

### SUNDER（70%）
- 我方：造成 !D! 点伤害。如果这张牌杀死了敌人,则获得 !E! 点 能量 。
- 官方：造成 !D! 点伤害。 NL 如果这张牌杀死了敌人，则获得 [B] [B] [B] 。

### BURST（71%）
- 我方：这回合你打出的下一张非攻击牌会被打出两次。
 消耗 。
- 官方：在这个回合，你打出的下一张技能牌会打出两次。

### SENTINEL（72%）
- 我方：获得 !B! 点 格挡 。
如果这张牌被 消耗 ，
获得 !E! 点 *能量* 。
- 官方：获得 !B! 点 格挡 。 NL 如果这张牌被 消耗 ， NL 获得 [R] [R] 。

### FLYING_KNEE（73%）
- 我方：造成 !D! 点伤害。下一回合获得 !E! 点 能量 。
- 官方：造成 !D! 点伤害。 NL 在下一回合获得 [G] 。

### AGGREGATE（73%）
- 我方：抽牌堆中每有 !MagicNumber! 张牌，获得 1 点能量。
- 官方：你的抽牌堆中每有 !M! 张牌，获得一点 [B] 。

### SECOND_WIND（74%）
- 我方： 消耗 手牌中任意张非攻击牌,每张获得 5 点 格挡 。
- 官方：消耗 手牌中所有非攻击牌，每张获得 !B! 点 格挡 。

### DAGGER_THROW（74%）
- 我方：造成 !D! 点伤害。抽 !C! 张牌。弃 !C! 张牌。
- 官方：造成 !D! 点伤害。 NL 抽一张牌。 NL 丢弃一张牌。

### TACTICIAN（77%）
- 我方： 不能被打出 。
如果这张牌从你的手牌中被丢弃,获得 !E! 点 能量 。
- 官方： 不能被打出 。 NL 如果这张牌从你的手牌中被丢弃，获得 [G] 。

### SHOCKWAVE（80%）
- 我方：给予所有敌人 !WeakPower! 层 虚弱 和 !VulnerablePower! 层 易伤 。
- 官方：给予所有敌人 !M! 层 虚弱 和 易伤 。 NL 消耗 。

### RECYCLE（80%）
- 我方：消耗 一张牌。
获得与其耗能相等的 *能量* 。
- 官方：消耗 一张牌。 NL 获得与其耗能相等的 [B] 。

### DOPPELGANGER（81%）
- 我方：下一回合，抽X张牌，获得X *能量* 。
消耗 。
- 官方：下一回合，抽X张牌，获得X [G] 。 NL 消耗 。

### FINESSE（81%）
- 我方：获得 !B! 点 格挡 。抽 !C! 张牌。
- 官方：获得 !B! 点 格挡 。 NL 抽1张牌。

### QUICK_SLASH（81%）
- 我方：造成 !D! 点伤害。抽 !C! 张牌。
- 官方：造成 !D! 点伤害。 NL 抽 1 张牌。

### BACKFLIP（81%）
- 我方：获得 !B! 点 格挡 。抽 !C! 张牌。
- 官方：获得 !B! 点 格挡 。 NL 抽2张牌。

### AFTER_IMAGE（81%）
- 我方： 每当你打出一张牌,获得 1 点 格挡 。
- 官方：你每打出一张牌，都获得1点 格挡 。

### BERSERK（83%）
- 我方：获得 !VulnerablePower! 层 易伤 。
在每回合开始时获得 *能量* 。
- 官方：获得 !M! 层 易伤 。 NL 在每回合开始时获得 [R] 。

## KB 未命中清单
- STRIKE
- DEFEND
- BLOOD_FOR_BLOOD
- FLASH_OF_STEEL
- BLOCK_POTION
- STRENGTH_POTION
- DEXTERITY_POTION
- ENERGY_POTION
- FIRE_POTION
- EXPLOSIVE_POTION
- WEAK_POTION
- FEAR_POTION
- SHURIKEN
- ORNAMENTAL_FAN
- LETTER_OPENER
- BIRD_FACED_URN
- BLACK_BLOOD
- RUNIC_CUBE
- STRIKE_SILENT
- DEFEND_SILENT
- STRIKE_DEFECT
- DEFEND_DEFECT
- STRIKE_WATCHER
- DEFEND_WATCHER
- DODGE_AND_ROLL
- SNEAKY_STRIKE
- ALCHEMIZE
- NIGHTMARE
- STORM_OF_STEEL
- TOOLS_OF_THE_TRADE
- WRAITH_FORM
- CLAW
- RECURSION
- STEAM_BARRIER
- DOOM_AND_GLOOM
- RIP_AND_TEAR
- MULTI_CAST
- PRESSURE_POINTS
- TRANQUILITY
- RUSHDOWN
- SIMMERING_FURY
- FASTING
- FORESIGHT
- J_A_X
