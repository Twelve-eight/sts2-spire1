# Spire1 历史问题报告——升级文案差异缺失(2026-09-01)

> 触发:用户报告"1代铁甲战士的 武装 和 武装+ 看起来没有区别。这是已出现过的bug,却没有被历史推广发现。"
> 方法:根因分析 → 推广扫描(全 306 张自研卡)→ 逐卡人工终审(对照 research/sts1-kb 官方数据)。
> 扫描工具:`.tmp/upgrade-diff-audit.mjs`(可回归;输出 `.tmp/upgrade-diff-audit.json`)。

## 1. 根因:武装 / 武装+(用户报告)

- **实现**(`mod/Spire1Code/Cards/Armaments.cs`):升级只切 `_all = true`(升级一张手牌→升级全部),Block 恒 5——这是 2026-08-26 freeze-review M-5 的正确回滚结果。
- **本地化**(`mod/Spire1/localization/{eng,zhs}/cards.json`):只有一条 `SPIRE1-ARMAMENTS.description`:"获得 !B! 点 格挡 。在本场战斗中 升级 手牌中的**一张**牌。"
- **机制**:StS2 引擎按升级状态渲染同一条描述,差异只能由文案内表达承载:
  - `{IfUpgraded:show:升级后|升级前}`(ShowIfUpgradedFormatter,升级预览还会自动加绿色)
  - SimpleLoc 简写 `-升级前文本-+升级后文本+`(Simplify → UpgradeSwapRegex → 同上)
  - 数值差用 diff 变量 `!X!`(`{X:diff()}`,升级预览高亮增量)
- **缺陷**:三种表达一个都没有 → 升级前后卡面完全一致(卡名 "+" 徽章除外)→ 用户所见"武装和武装+没区别"。
- **为何历史审计没发现**:历史审计(freeze-review/reverify)盯的是**数值与行为保真**(M-5 回滚 +3 Block 正是这类),文案升级差异表达从未进入任何审计维度。扫描器本次补上该维度。

## 2. 推广验证:全卡扫描结果

**扫描范围**:306 张自研卡中 276 张带 OnUpgrade;按升级形态分四类,前三类为合法无文案差异(费用徽章/关键字行由引擎自动渲染),第四类为本缺陷族:

| 形态 | 判定规则 | 数量 | 文案要求 |
|---|---|---|---|
| costOnly | `EnergyCost.UpgradeBy(-1)` | 18 | 无(费用徽章自渲染) |
| keyword | `AddKeyword(Innate/Retain)` | 7 | 无(关键字行自渲染,CardModel beforeDescription 注入) |
| numeric + desc 已含 diff 变量 | `UpgradeValueBy` + `!X!` 在文案 | 246 | 已满足 |
| **behavior/numeric 无差异表达** | **行为切换无 swap / 数值升级无变量** | **5** | **缺失=缺陷** |

**缺陷清单(终版,全部经人工对照官方 KB 复核)**:

| # | 卡 | 升级效果(实现实锤) | eng 现状 | zhs 现状 | 官方升级文案基准(research/sts1-kb) |
|---|---|---|---|---|---|
| 1 | Armaments 武装 | 一张→全部(`_all=true`) | 缺差异 | 缺差异 | "Upgrade **all** cards in your hand" / "升级手牌中的**所有**牌" |
| 2 | Trip 绊倒 | 单体→全体易伤(TargetType override) | **畸形 swap 残骸**:`...*Vulnerable*.+ to ALL enemies.+`(裸 `+` 无 `-` 前段,渲染出字面加号) | 缺差异 | "Apply !M! Vulnerable **to ALL enemies**" |
| 3 | Blind 致盲 | 单体→全体虚弱 | 同 Trip 畸形残骸 | 缺差异 | "Apply !M! Weak **to ALL enemies**" |
| 4 | Burst 迸发 | 下 1 张→下 N 张技能牌(PowerVar +1) | 已有 `!BurstPower!` ✓ | **缺变量** + **误译**:"非攻击牌"应为"**技能牌**"(官方:这个回合,你打出的下一张**技能牌**会打出两次) | "your next !M! **Skills** are played twice" |
| 5 | Stack 蓄力 | 弃牌数格挡 +3(CalculationBase +3) | 缺 "+3" 表达 | 缺 "+3" 表达 | "Gain Block equal to ... discard pile **+3**" |

**附带发现(Burst zhs 语义错误,升级文案之外的独立缺陷)**:zhs 描述"你打出的下一张非攻击牌会被打出两次"——StS1 官方语义是"技能牌"(Skill),非攻击牌(Non-attack)范围更宽(含能力牌)。同卡实现正确(OnPlay 查 Skill),纯翻译错误。

## 3. 修复方案

统一用 SimpleLoc swap 简写(管线最短、升级预览自动绿显):

| 卡 | 修复文案(zhs / eng) |
|---|---|
| Armaments | `在本场战斗中 -升级手牌中的一张牌-+升级手牌中的所有牌+。` / `... -Upgrade a card in your hand-+Upgrade all cards in your hand+ for the rest of combat.` |
| Trip | `给予 !VulnerablePower! 层 -易伤-+易伤。给予所有敌人 !VulnerablePower! 层 易伤+`——更简洁:`-给予 !VulnerablePower! 层 易伤-+给予所有敌人 !VulnerablePower! 层 易伤+` / eng 同构 |
| Blind | 同 Trip(虚弱) |
| Burst | zhs 改 `这回合你打出的下 !BurstPower! 张技能牌会被打出两次`(修变量+修误译);eng 已合规保持 |
| Stack | `获得与你当前弃牌堆中牌数 +3 相等的 格挡 值`——官方基准是后缀式:`获得与你当前弃牌堆中牌数-+3- 相等的...` 需用 diff 变量才能绿显;实现为 CalculationBase,文案加 `!CalculationBase!`?——见下方"Stack 特殊处理" |

**Stack 特殊处理**:升级的是 CalculationBase(计算基数),非独立 DynamicVar。核对 Stack 实现的 var 定义后决定:若 `!CB!`(CalculatedBlock)已在文案,升级预览自动带 +3;若无则改写文案引用该变量。(修复实现时逐项核。)

## 4. 历史推广验证结论(用户要求"推广验证"的完整答复)

1. **同类缺陷族**:升级**行为/目标/倍率**变化但文案无差异表达 → 共 3 张(Armaments/Trip/Blind),全部锁定。
2. **数值类缺陷族**:升级改数值但文案无对应 diff 变量 → 2 张(Burst zhs/Stack)。
3. **无缺陷的合法形态**:18 纯降费(徽章自渲染)+ 7 关键字(关键字行自渲染)+ 246 数值+变量已配 → 共 271 张清白。
4. **审计盲区根因**:历史三波审计(freeze-review-20260826、reverify-20260826、increment-review-20260830)的维度是"数值保真/行为保真/双语键齐备",从未有"升级差异表达完备性"维度;本地化审计只对齐 eng/zhs **键集**,不对齐**升级语义**。本报告补入该维度,扫描器可回归(建议纳入冒烟前置)。
5. **Trip/Blind eng 的畸形残骸是独立线索**:`.+ to ALL enemies.+` 说明曾有 swap 语法被部分删除/误编辑——历史上有人开始修这个,只删了 `-旧-` 半边留下了 `+新+` 半边,且 zhs 从未跟进。这解释了"已出现过的bug":半途修复痕迹。

## 5. 修复状态

- [x] 根因定位 + 官方基准核对(本报告 §1-§2)
- [x] 推广扫描工具 + 三轮迭代消假阳性(表达式体解析、costOnly/keyword 分类、diff 变量名通配)
- [ ] 5 卡文案修复(cards.json eng+zhs)
- [ ] 构建冒烟(STS001 分析器过 localization)
- [ ] 玩家实机验证(武装+ 卡面差异)——需用户下次单人局

## 附:今日联机分歧(独立事件,2026-09-01 16:47,与本报告无关)

- **现象**:四人联机(AutoAnthony 全员局)进入精英战(骇鳗)后 client 被踢,`StateDivergence` checksum 161。
- **实锤**(godot.log + RitsuLib 诊断包 `.tmp/divergence-1647/`):骇鳗 host 端独有 `POWER.METALLICIZE_POWER_A4H (金属化):17`,client 端无。
- **根因**:Act4Heart 1.1.7 "冒火精英"(GreenKeyHooks)。地图标记(SuperEliteQuest)在每端本地由 `ModifyGeneratedMapLate` 生成,门是**本地配置** `keys_enable`;进战 buff(`DoSuperEliteBuff`,seed+act 派生 RNG 四选一:力量/金属化/再生/最大生命)同样查本地标记。**用户本地 `dolso.act4_heart.config` 的 `keys_enable=false`,host=true** → 你端地图无火标记(你未见火特效,自述吻合)、host 端进战加 17 层金属化 → checksum 分歧。Act4Heart 的 ConfigSynchronizer 只做 host→client 广播,无双端一致性校验(ValidateConfigMessage 仅对版本号,且版本号是 host 单方计数)。
- **即时规避**:用户端把 `C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/steam/76561199466878739/mod_configs/dolso.act4_heart.config` 的 `keys_enable` 改为 `true`(与 host 一致);该文件被 FileSystemWatcher 监听,热生效,无需重启。**队友四方建议全体对齐此开关**。
- **结构性缺口记录**:mod 用本地配置门控地图钩子 + 本地 quest 标记(不跨网序列化)是结构性分歧源——与卷四 KB 的"合法分歧面"清单同类,已记录;Spire1 自身无此模式(我方事件全部走选项消息重放)。
