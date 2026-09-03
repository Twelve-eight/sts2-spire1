# StS2 战斗语义卷（Combat Semantics, EA build）— sts2-spire1 知识库

## 本卷范围
以 `research/engine-dllsrc/` 反编译 C# 源（Godot EA 版，`MegaCrit.Sts2.Core.*`）为唯一权威，回答 StS2 战斗中"谁先谁后"类问题：攻击命令管线、单次伤害结算全序、死亡/免死（ShouldDie/preventer）、格挡/治疗管线、power 施加与叠层（InstancedPerApplier!）、时长递减（SkipNextDurationTick）。与 StS1 的仲裁差异表收尾，供移植对齐。
**来源置信度**：高 = C# 源码直接可证，标注 `文件:行` 或 `文件#方法`；中 = 源码+调用链推断（注明）；本卷不收录 wiki/口传内容。行号为反编译快照行号，重反编译会漂移，以方法名定位为准。

---

## 1. 攻击管线（DamageCmd.Attack → AttackCommand.Execute）

**S01 攻击是 builder 命令，Execute 时才结算** — 出处 `Commands/DamageCmd.cs`（`Attack(decimal)` / `Attack(CalculatedDamageVar)`）+ `Commands.Builders/AttackCommand.cs#Execute`。置信度：**高**
`Execute(choiceContext)` 全序：
```
① 守卫：战斗 IsOverOrEnding 且 state 为 live → 直接返回；Attacker 已死 → 返回
② await Hook.BeforeAttack(combatState, this)
③ attackCount = Hook.ModifyAttackHitCount(combatState, this, _hitCount)   ← 命中数可被钩子改写（decimal）
④ for i < attackCount（每击一循环）:
     - Attacker 中途死亡 → break
     - validTargets = 当前存活目标（每击重新过滤！）
     - 攻击方动画/VFX/SFX（_playOnEveryHit 或仅 i==0）
     - 随机目标：每击单独掷（RunState.Rng.CombatTargets 流；allowDuplicates 可关）
     - _beforeDamage 回调（每击）
     - CreatureCmd.Damage(amount: _damagePerHit 或 _calculatedDamageVar.Calculate(target)——
       ★ 动态伤害每击重算, targets: 单体或全体存活者, dealer: Attacker, cardSource/cardPlay)
⑤ History.CreatureAttacked + await Hook.AfterAttack
```
与 StS1 的关键差异：StS1 多段打击单快照（damage-pipeline.md R09）；StS2 `CalculatedDamageVar` 型攻击**逐击重算**，AOE 目标列表**逐击刷新**（新加入战场的敌人会被后续命中打中）。

**S02 ValueProp 位旗** — 出处 `AttackCommand#Unpowered` + `ValueProp`（`Move` 默认 / `Unpowered` / `Unblockable` / `SkipHurtAnim` 等）。置信度：**高**
`Unpowered` = "真攻击但跳过 power 钩子"（Omnislice 类专用）；`Unblockable` 在格挡吸收层消费（S04）。

---

## 2. 单次伤害结算全序（CreatureCmd.Damage）

**S03 每个目标的完整处理序** — 出处 `Commands/CreatureCmd.cs#Damage(…, IEnumerable<Creature>?, decimal, ValueProp, Creature?, CardModel?, CardPlay?)`（反编译行 258-434）。置信度：**高**
```
0. 守卫：targets 空 → 空；dealer 已死 → 生成全空结果返回
   目标列表逐个（已死者 continue）：
① modifiedAmount = Hook.ModifyDamage(…, ModifyDamageHookType.All, …, out modifiers)
   ← 全部改伤层统一入口，见 S04 分层
② await Hook.AfterModifyingDamageAmount(cardSource, modifiers)
③ await Hook.BeforeDamageReceived(target, modifiedAmount, props, dealer)   ← 在格挡之前
④ blockedDamage = target.DamageBlockInternal(modifiedAmount, props)
   （Unblockable 旗 → 吸收 0；Block += 吸收量）
⑤ unblocked = Hook.ModifyHpLost(…, HpLossHookPhase.BeforeOsty, …)        ← 掉血修正相位一
⑥ redirectedTarget = Hook.ModifyUnblockedDamageTarget(…)                  ← Osty/Necrobinder 重定向
   unblocked = Hook.ModifyHpLost(…, AfterOsty, …)（对重定向后再修正）      ← 相位二
⑦ result = target.LoseHpInternal(unblocked, props)
   → {UnblockedDamage, WasTargetKilled(amount>=hp 且 hp>0 时), OverkillDamage}，HP 钳 0
⑧ WasBlockBroken = Block<=0 且 blocked>0；WasFullyBlocked = 未穿盾且溢出 0
⑨ 视效/震屏/受伤动画（非逻辑）
全部目标处理完 → 逐 result 后置钩子：
⑩ AfterBlockBroken → AfterCurrentHpChanged(-dmg) → DamageDealt 统计
   → AfterDamageGiven(dealer 侧) →（击杀？收集 : AfterDamageReceived(target 侧)）
⑪ Kill(killedCreatures)（死亡统一批处理，见 S05）
⑫ Cmd.CustomScaledWait(0.1f, 0.2f)
```

**S04 ModifyDamage 分层与卡附魔（Enchantment）位置** — 出处 `Hooks/Hook.cs#ModifyDamage`（行 1495 起）+ `Hooks/ModifyDamageHookType.cs`。置信度：**高**
`ModifyDamageHookType` = 位旗 `{None=0, Additive=2, Multiplicative=4, Cap=8, All=0xE}`，同一入口按层调度：
```
① cardSource.Enchantment 存在 → EnchantDamageAdditive(+) → EnchantDamageMultiplicative(×)
   ★ 附魔先于一切模型钩子
② 模型层 Additive → Multiplicative → Cap（cap 层做上限钳制）
```
与 StS1 对照：StS1 的 give/receive/finalGive/finalReceive 四层 + 站姿态层，在 StS2 统一为**单一 ModifyDamage 入口的加/乘/帽三层** + 附魔前置层；攻守双方都在同一 models 集合里（靠实现接口区分，遍历顺序 = models 集合序，无 relics/powers 容器之分）。

**S05 掉血修正双相位（Osty 重定向）** — 出处 `Hooks/HpLossHookPhase.cs` 注释 + `CreatureCmd.cs` 步骤⑤⑥。置信度：**高**
`BeforeOsty` 钩子作用于重定向前（Necrobinder 本体），`AfterOsty` 作用于被 Osty（宠物）顶替后的目标。两个相位都可能改写数值；重定向只改"谁掉血"，不改格挡吸收（格挡在原目标身上，步骤④已扣）。

---

## 3. 死亡与免死（Kill / ShouldDie）

**S06 死亡管线** — 出处 `CreatureCmd.cs#Kill / #KillWithoutCheckingWinCondition`（行 446-606）。置信度：**高**
```
Kill(creatures, force=false):
  逐个 KillWithoutCheckingWinCondition →
  胜负判定：全体玩家死 → LoseCombat + 结算失败画面；
  否则：玩家死且 combatState.CurrentSide==Player → PlayerCmd.EndTurn(canBackOut:false)

KillWithoutCheckingWinCondition(creature, force, recursion):
  ① live-combat 守卫；联机非战斗内击杀玩家 → 报错并 Heal(1) 兜底
  ② CurrentHp>0 → 先 Unblockable|Unpowered 排空至 0 + AfterCurrentHpChanged
  ③ await Hook.BeforeDeath
  ④ force || MaxHp<=0 || Hook.ShouldDie(..., out preventer) 为真 → 正死：
       Died 事件 → ShouldCreatureBeRemovedFromCombatAfterDeath 钩子（决定是否移除尸体）
       → 死亡动画 → AfterDeath → 从 combatState 移除 → 全部 power 逐个 AfterRemoved
       → 主怪死则处决全副怪（teammates 全是 secondary 时级联 Kill）
       → 玩家死：OrbQueue.Clear → Osty 同死 → DeactivateHooks → HandlePlayerDeath
     为假（被 preventer 拦下，如 Fairy in a Bottle 类）：
       AfterDeath(wasRemovalPrevented:true) → AfterPreventingDeath(preventer)
       → 若依然 IsDead → 递归重走（上限 10 层，防死循环）
```
要点：**免死是"死亡提交前的 ShouldDie 裁决"**（先排血、发 BeforeDeath、再问 ShouldDie），与 StS1 "扣血后查 <1 再拦截" 不同——StS2 的免死可以由 power/relic 模型实现并报告 preventer；`force` 跳过裁决（弃局等内置场景）。

---

## 4. 格挡与治疗

**S07 GainBlock 管线** — 出处 `CreatureCmd.cs#GainBlock`（行 668-701）。置信度：**高**
```
战斗结束/目标死 → 0
① Hook.BeforeBlockGained
② modifiedAmount = Hook.ModifyBlock(...) → Math.Max(0)（负修正最多归零，不倒扣）
③ Hook.AfterModifyingBlockAmount
④ modifiedAmount>0 才：音效/视效 → GainBlockInternal（上限 999999999）→ History.BlockGained → wait
⑤ Hook.AfterBlockGained
```
`LoseBlock`（行 713-725）：直接减，`Block` 从正跨到 ≤0 时触发 `AfterBlockBroken`（与伤害破盾同钩子）。
与 StS1 差异：StS1 的 Dex/Frail 挂卡牌侧 `applyPowersToBlock`、非卡牌格挡不吃（damage-pipeline.md R11）；StS2 一切 GainBlock 都过同一 ModifyBlock 钩子链。

**S08 Heal** — 出处 `CreatureCmd.cs#Heal`（行 738-765）+ `Creature.cs#HealInternal`。置信度：**高**
`CombatManager.IsEnding && 非玩家` → 拒绝；`amountHealed = min(amount, MaxHp-CurrentHp)`；`HealInternal` 死者复生时发 `Revived` 事件（StS1 是 isDying 早退禁止治疗）。治疗**不走伤害钩子链**（同 StS1）。

---

## 5. Power 施加与叠层（PowerCmd）

**S09 三种叠层语义（InstanceType）** — 出处 `Commands/PowerCmd.cs#Apply<T>/FindExistingInstanceForStacking`（行 71-178）。置信度：**高**
```
FindExistingInstanceForStacking:
  Instanced            → 永不合并，每次 Apply 都新建实例
  InstancedPerApplier  → 同一 Applier 的实例合并，不同 Applier 各自独立实例   ★ StS1 无此概念
  None                 → 单实例，直接叠 amount（= StS1 默认）
Apply 返回 null 的三种情形：战斗结束 / 被拦截（如 Artifact，在 Received 钩子里实现）/ 修正后 amount==0
```

**S10 Apply 全序与玩家侧 debuff 保护** — 出处 `PowerCmd.cs#Apply(PowerModel 版)` 行 105-163。置信度：**高**
```
战斗结束/amount==0/目标 CanReceivePowers=false → 拒
① BeforePowerAmountChanged(power, amount, target, applier)
② ModifyPowerAmountGiven(applier 侧，applier 在场时) → ModifyPowerAmountReceived(target 侧)
③ 联机：主怪/副怪且 ShouldScaleInMultiplayer → 数值联机缩放
④ power.BeforeApplied(target, modifiedAmount, …)
⑤ ApplyInternal（真正落到 target）→ History.PowerReceived
⑥ ★ target 是玩家侧 且 power.Type==Debuff → power.SkipNextDurationTick = true
⑦ AfterModifyingPowerAmountGiven/Received → AfterApplied + AfterPowerAmountChanged
```
`SkipNextDurationTick` 是 StS1 `justApplied` 的显式化：**施加当回合不吃时长递减**，但仅对玩家侧 debuff 置位（StS1 的 justApplied 双条件见 status-stacking.md）。

**S11 递减/移除** — 出处 `PowerCmd.cs#Decrement/TickDownDuration/ModifyAmount/Remove`（行 184-299）。置信度：**高**
`TickDownDuration`：`SkipNextDurationTick` 为真则消费掉该旗不递减，否则 `Decrement(=ModifyAmount(-1))`。`ModifyAmount`：同 S10② 的 Given/Received 双侧修正 → `SetAmount` → `ShouldRemoveDueToAmount()` 为真自动 `Remove`（RemoveInternal → wait → AfterRemoved）→ 存活怪 `UpdateIntent`（power 变化刷新意图数字）。**没有 StS1 的 999 钳制/负值下限的通用规则**（下限由各 power 的 ShouldRemoveDueToAmount/实现自管，见 status-stacking.md R 对比）。

---

## 6. 其他管线要点

**S12 能量钩子** — 出处 `Hook.cs`：`AfterEnergyReset(combatState, player)`、`AfterEnergySpent(card, amount)`、`AfterModifyingEnergyGain(modifiers)`。置信度：**高**（签名）/ 消费方枚举未穷举。
**S13 宝珠钩子** — 出处 `Hook.AfterModifyingOrbPassiveTriggerCount(combatState, orb, modifiers)`：被动触发次数可被钩子修正（StS1 的 Cables 是写死二次调用，orbs.md R10）。置信度：**高**（签名）。
**S14 抽牌/弃牌/消耗钩子族** — 出处 `Hook.cs`：`BeforeHandDraw` / `AfterCardDrawn(fromHandDraw)` / `AfterCardDiscarded` / `AfterCardExhausted(causedByEthereal)` / `AfterCardChangedPiles(oldPile)` / `BeforeCardAutoPlayed(type)`。StS1 的"抽到时 triggerWhenDrawn"对应 `AfterCardDrawn`（进入手牌后，时点不同——StS1 在进手牌前，draw-exhaust.md R05）。置信度：**高**（签名与调用点存在性）。

---

## 7. StS1 → StS2 移植仲裁速查

| 语义 | StS1（mechanics/ 卷） | StS2（本卷） | 仲裁建议 |
|---|---|---|---|
| 改伤层 | give→stance→receive→finalGive→finalReceive 四层（R06/R07） | 单入口 ModifyDamage：附魔→Additive→Multiplicative→Cap（S04） | 层序映射表需按"旧层→新层"一对一落位；乘区合并次序变化要逐卡回归 |
| 掉血修正 | onLoseHpLast/onLoseHp（death-arbitration R20） | BeforeOsty/AfterOsty 双相位（S05） | 无宠物重定向的移植场景可把两相位视作一相位 |
| 免死 | damage() 尾部拦截链 MotB→Fairy→Lizard（R02） | Kill 内 ShouldDie 裁决 + preventer + 递归 10（S06） | 渎神类"队列化巨额 HP_LOSS"在 StS2 需走 Damage/kill 管线才能被 ShouldDie 类救回 |
| 死亡通知 | die() 同步：onDeath→onMonsterDeath（triggers R09） | Died 事件 + BeforeDeath/AfterDeath + powers 逐个 AfterRemoved（S06） | 注意 StS2 尸体移除与否是钩子（复活怪） |
| 多段打击 | 单快照（R09） | CalculatedDamageVar 逐击重算 + 目标逐击刷新（S01） | 移植卡牌要显式选择快照语义，不能沿用 StS1 惯性 |
| 状态施加 | ApplyPowerAction 分支（status-stacking） | PowerCmd.Apply + InstanceType 三态 + Given/Received 双侧修正（S09/S10） | InstancedPerApplier 需在移植层显式降级或保留 |
| justApplied | 隐式双条件（status-stacking） | SkipNextDurationTick，仅玩家侧 debuff（S10/S11） | 怪物侧 debuff 在 StS2 施加当回合即会递减——移植对拍点 |
| 格挡加成 | 卡牌侧 Dex/Frail，非卡牌不吃（R11） | 全部 GainBlock 过统一 ModifyBlock（S07） | 移植力量式格挡 buff 直接进钩子，无需 StS1 的两套路径 |
| 动作队列 | 五级容器 FIFO（action-manager R02） | async/await 顺序 await，无队列容器（S01/S03） | "插队/优先级"语义需用 await 次序显式表达 |

---

## 8. 开放问题 / 低置信项

1. `Hook.ModifyDamage` 的 MultiCreatureTargeting 预览分支（target==null 时逐敌循环）仅结构确认，预览与实伤一致性未验证。置信度：**中**。
2. Enchantments 系统本体（`Entities.Enchantments/` 仅 EnchantmentOption/EnchantmentStatus 两个文件，核心类在 Models 侧）未展开；附魔施加/覆盖仲裁待专卷。置信度：**未定**。
3. `MonsterModel` AI 框架（rollMove 等价物、intent 流程）未取证，StS1 侧对照卷亦未完成。置信度：**未定**。
4. `CombatManager` 回合状态机（PlayerTurnPhase/EndTurnSignal/PendingLossState）未逐状态展开。置信度：**未定**。
5. OrbCmd/ForgeCmd/ThinkCmd 等命令族与宝珠/锻造管线未展开。置信度：**未定**。
