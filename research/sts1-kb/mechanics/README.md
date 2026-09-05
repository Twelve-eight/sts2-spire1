# mechanics/ — StS1 战斗语义知识库 · 第二卷：动作时序与触发优先级

回答"多个机制同时触发谁先谁后"类问题，为 StS2 BaseLib 移植仲裁服务。
第一卷（卡表/遗物/药水数值基线）见上级目录 [README.md](../README.md)。

## 来源与置信度图例

| 置信度 | 含义 |
|---|---|
| **高** | javap 字节码直接可证（desktop-1.0.jar v2.x，含观者），每条附 `类名#方法` 出处，多数附字节码偏移 |
| **中** | 字节码 + 调用链推断，文中注明推断环节 |
| **低** | 仅 wiki 口传或间接证据，明确标注待证 |

wiki（slay-the-spire.fandom.com）仅作交叉佐证；HTML 页 403，经 `api.php` 抽样核对（如 Vulnerable ×1.5/向下取整/时长叠加、Sundial 计洗牌勘误）。**凡 wiki 与字节码冲突，以字节码为准并标注勘误**。

## 文件索引

| 文件 | 规则数 | 范围 |
|---|---|---|
| [action-manager.md](action-manager.md) | 20 | GameActionManager 帧循环、五级队列优先级、addToBottom/addToTop/addCardQueueItem 插入语义、shouldCancelAction 取消语义、ActionType 真实用途（无逐类型节拍） |
| [turn-phase.md](turn-phase.md) | 18 | 战斗开局初始化块、能量发放点、出牌微时序、结束回合三段链（哨兵→自动结算牌→弃牌阶段）、敌方回合、新回合块、首回合特殊点 |
| [draw-exhaust.md](draw-exhaust.md) | 25 | DrawCardAction 逐帧逻辑、10 张手牌上限钳制、洗牌时机与 onShuffle 触发、triggerWhenDrawn 全量清单、消耗区七步通知链、Retain/Ethereal 结算顺序、**用户三连问裁决（§6）** |
| [keys-and-final-act.md](keys-and-final-act.md) | 6 | 三钥匙与第四层入口、蓝宝石钥匙宝箱二选一（互斥）、autoslay 不取钥匙的幕切换卡死、StS2 无原生钥匙的对照 |
| [triggers.md](triggers.md) | 18 | Power/Relic/Card/Monster/Stance 五基类钩子总表、onPlayCard/onUseCard/onAfterUseCard 四时刻对照、死亡链/胜利链、计数遗物取证、同容器获得顺序结论、同步直调 vs 队列化对照 |
| [damage-pipeline.md](damage-pipeline.md) | 17 | DamageType 三型语义、攻防两端乘区次序（Vulnerable 在攻方侧）、atDamageGive/final 修改器链、多重打击单快照结论、格挡吸收点、LoseHP 分流 |
| [status-stacking.md](status-stacking.md) | 21 | ApplyPowerAction 完整分支（Artifact 拦截/合并 vs 新建/Night Terror 特例）、负值下限与 999 钳制的真实位置、debuff 时长递减唯一发生地、justApplied 双条件 |
| [death-arbitration.md](death-arbitration.md) | 22 | 玩家死亡总闸与免死拦截链（MotB→妖精→蜥蜴尾）、致死来源分类学、渎神 EndTurnDeathPower 全解、**渎神 vs 无实体旗舰仲裁**（1 层不救/≥2 层救/回合开始新施加可救的时序推导）、MotB 治疗封死、SuicideAction/cannotLose |
| [defense-powers.md](defense-powers.md) | 10 | 防御干预五层挂点图（入口钳制/格挡/④层钩子/onLoseHpLast）、无实体三实现差异、Buffer 逐源消费、Invincible 回合预算、钨杆平减、格挡保留中央门控、仲裁案例表 |
| [orbs.md](orbs.md) | 13 | 宝珠数据模型与槽位增删、通道全序（含满槽逐出三连/静默失败）、激发家族表（激发最新不移除=Multi-Cast）、四珠被动/激发时点、Cables 双触发、**Focus 不对称刷新**（增=全员刷新/减=逐次/移除=冻结不回落） |
| [stances.md](stances.md) | 12 | ChangeStanceAction 全序（CannotChangeStance 门/同姿态幂等/订阅者先于 Calm 退场能量）、四姿态钩子表、stance.atStartOfTurn 调用点结案、Divinity 自退 vs 渎神死时序、uniqueStancesThisCombat 无消费者 |
| [energy-cost.md](energy-cost.md) | 10 | 能量三变量模型、**每回合重置点=PlayerTurnEffect 构造器**（勘误 turn-phase R02）、开局发放链、hasEnoughEnergy 七道门、费用生命周期与 Confusion/Madness、freeToPlay/X 费 |
| [potions-combat.md](potions-combat.md) | 6 | 药水点击即同步 use()（非队列化）、PotionPopUp 结算序、SmokeBomb 同步置位、妖精自动使用不触发 onUsePotion、药水槽销毁 |
| [monster-ai.md](monster-ai.md) | 10 | rollMove/getMove/aiRng、moveHistory 写入与 lastMove 族读取、setMove 家族、意图数字管线与**onModifyPower 即时重算意图**（实伤快照可能与显示不一致）、monsterQueue 调度、escape 语义 |
| [power-lifecycle.md](power-lifecycle.md) | 12 | 全量 161 power 类按钩子归档：回合四时点成员清单、justApplied 9 家族、双钩子 power（Equilibrium/Ritual/Malleable）、伤害/格挡钩子使用者、叠层定制 |
| [relic-triggers.md](relic-triggers.md) | 16 | 全量 190 遗物类按钩子归档：开场两段/回合两段/计数族/受击族/胜利链；规则位 boss 遗物零钩子=引擎查询建模 |
| [turn-control.md](turn-control.md) | 6 | 窥视 ScryAction 全序（triggerOnScry 扫尾）、skipMonsterTurn 消费者全集（Vault 连玩家 debuff 递减一起跳过）、callEndTurnEarlySequence 绕过哨兵链 |

合计 **236 条编号规则**，每条独立可引用（`文件名 Rnn`），可直接用于移植仲裁。

## 用户示例问题的确定性答案（验收项）

**Q：战斗开局抽牌、消耗、"抽到时自动打出"同时涉及时谁先谁后？**
→ 见 [draw-exhaust.md §6 用户问题裁决](draw-exhaust.md)。要点：三者不交错——初始抽牌是原子块（开局队列中的单个 `DrawCardAction`，逐帧每次一张）；每张牌在抽取瞬间先同步触发 `triggerWhenDrawn()`（此时牌尚未进手牌组），其产生的动作按 addToTop/addToBot 入队但**不打断本批抽牌**；整批抽完后才轮到入队的消耗/副作用动作。注意本版本 Havoc/Mayhem 并非 triggerWhenDrawn（见同文件 R24/R25 反例勘误）。

## 对任务书假设的勘误（移植前必读）

1. **不存在 `actionTypePhase` 枚举 / BEFORE-DEBUFF-DRAW 排序**。真实排序 = 五级容器优先级 + FIFO（action-manager.md R02/R13）。若设计文档源自旧 mod 文档需校正。
2. **`AbstractCreature.damage` 是抽象方法**，实际逻辑分置于 `AbstractMonster#damage` 与 `AbstractPlayer#damage`，两侧钩子次序不同（damage-pipeline.md R03/R04）。
3. **Havoc 是打出即结算的 POWER**（`use() → PlayTopCardAction`），Mayhem 走 power 的 `atStartOfTurn`；真正的 `triggerWhenDrawn` 只有 Doubt/EndlessAgony/Eviscerate/DeusExMachina/Void 五类（draw-exhaust.md §3）。
4. **能量跨回合保留**，基础能量只在战斗开始发一次；不存在"每回合重置为 3"（turn-phase.md R02）。
5. **PostDraw 钩子名不符实**：`atTurnStartPostDraw`/`atStartOfTurnPostDraw` 在抽牌动作入队后立即直调，卡尚未到手（turn-phase.md R04）。
6. **玩家与怪物掉格挡时机不对称**：玩家在新回合块内、怪物在其自身回合开始前（turn-phase.md R11）。
7. `applyPreCombatLogic`（relic.atPreBattle 分发器）、`ReApplyPowersAction`、`RemoveAllPowersAction` 为死代码（全量调用者扫描为零）；相关遗物实际走其他钩子。
8. InstantKillAction 仅 Judgement 使用；常规胜利由 `monster.updateDeathAnimation` 延迟触发 `room.endBattle`（triggers.md §5）。

## 与 StS2 引擎差异速记（我方移植已知约束）

| StS1 v2.x 机制 | 我方 StS2 移植现状 | 仲裁建议 |
|---|---|---|
| `AbstractStance` API（观者姿态）贯穿多处钩子：`stance.atStartOfTurn`（applyStartOfTurnRelics 首位）、`stance.onEndOfTurn`（callEndOfTurnActions 末位）、`stance.onPlayCard`（出牌流水 relic 之后 blight 之前）、`stance.atDamageGive`（Wrath×2/Divinity×3，伤害链 give 层之后） | **无姿态 API** | 以上四个挂点需映射到等价自定义机制或显式省略；涉及 Wrath 类倍率仲裁时参照 damage-pipeline.md R06 的层序插入位置 |
| 单线程单活动动作模型（currentAction 串行 + duration 跨帧） | 待对齐 | 移植不得引入并行结算；"同一时刻只有一个动作在推进"是所有次序结论的前提 |
| cardQueue/actions/preTurnActions/monsterQueue 四容器固定优先级 | 待对齐 | action-manager.md R02 即仲裁总纲 |

## 重新生成 / 对账

```bash
# 抽查任一结论的字节码（只读 jar）
cd "G:/steam/steamapps/common/SlayTheSpire"
unzip -o -q desktop-1.0.jar "com/megacrit/cardcrawl/actions/GameActionManager.class" -d .tmp/jcls
javap -c -p .tmp/jcls/com/megacrit/cardcrawl/actions/GameActionManager.class
```

- 本卷全部结论产自该 jar（v2.x）；发现与本文冲突先跑上述命令复核字节码。
- 临时解包目录 `.tmp/mechtmp` 已在使用后删除；重建不影响任何结论。
