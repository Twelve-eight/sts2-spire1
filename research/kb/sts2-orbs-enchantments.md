# StS2 宝珠与附魔（Orbs & Enchantments, EA build）— sts2-spire1 知识库

## 本卷范围
OrbCmd/OrbQueue/OrbModel 的宝珠管线（通道/激发/被动/回合触发/触发次数钩子）与 EnchantmentModel 附魔系统（附加/叠层/参与管线的位置）。
来源：`research/engine-dllsrc/`。关联：`sts2-combat-semantics.md` S04/S13、`sts2-card-play.md` C03；StS1 对照 `../sts1-kb/mechanics/orbs.md`。

**图例**：**高**=源码直接可证。

---

## 1. 宝珠管线

**O01 OrbQueue 数据模型** — 出处 `Entities.Orbs/OrbQueue.cs`（行 50-100）。置信度：**高**
`_orbs` 列表 + `Capacity`；`TryEnqueue`（容量 0 拒绝、满则抛异常——**满槽检查在 OrbCmd 侧先行**）；`Remove(orb)` 按对象移除；`Insert(idx, orb)`（索引插入，供特定效果插队）；回合触发器 `BeforeTurnEnd`（快照列表逐个 `BeforeTurnEndOrbTrigger`）/ `AfterTurnStart`（同构 `AfterTurnStartOrbTrigger`），循环中每项先复查 `CombatState != null`。

**O02 Channel 全序** — 出处 `Commands/OrbCmd.cs#Channel`（行 68-92）。置信度：**高**
```
战斗未结束 →
① 角色基础槽位 0 且 Capacity==0 → 先 AddSlots(1)（无槽也能通道）
② orb.Owner = player
③ 若 Orbs.Count >= Capacity → await EvokeNext(player)   ← 满槽先激发最左（无 StS1 三连重排队，直接 await 串行）
④ TryEnqueue 成功 → History.OrbChanneled → PlayChannelSfx → 动画 → Hook.AfterOrbChanneled
```
**StS1 对照**（orbs.md R04）：StS1 满槽通道 = `addToTop 三连`（Animate→Evoke→Channel）异步展开；StS2 是 `await EvokeNext()` 后再入队——**语义等价（先逐出最左再通道）、实现形态不同**。

**O03 激发家族** — 出处 `OrbCmd.cs#EvokeNext/#EvokeLast/#Evoke`（行 94-153）。置信度：**高**
- `EvokeNext` = `Orbs.First()`（最左/最早）——对应 StS1 `evokeOrb()`。
- `EvokeLast` = `Orbs.Last()`（最新）——对应 StS1 `evokeNewestOrb()`。
- `dequeue` 参数：**false = 不从队列移除只结算**——对应 StS1 `evokeWithoutLosingOrb`（Multi-Cast 基础，orbs.md R07）。
- `Evoke` 私有体：先（可选）Remove+动画 → `await orb.Evoke(ctx)`（返回目标集）→ 结算；战斗结束守卫。

**O04 被动：Passive vs TriggerPassive（Cables 泛化）** — 出处 `OrbCmd.cs#Passive`（行 155-171）+ `Models/OrbModel.cs#TriggerPassive`（行 243-262）。置信度：**高**
```
OrbCmd.Passive(countAffectedByHooks=false) → orb.Passive(ctx, target)（原始一次）
OrbCmd.Passive(countAffectedByHooks=true)  → orb.TriggerPassive(ctx, target)：
    triggerCount = Hook.ModifyOrbPassiveTriggerCount(state, orb, 1, out modifiers)
    Hook.AfterModifyingOrbPassiveTriggerCount(...)
    for i < triggerCount: Passive + wait   ← ★ 触发次数可被钩子改成 N（Cables/类似物 = +1 的钩子实现）
```
数值本体：`OrbModel.PassiveVal/EvokeVal`（abstract，decimal）经 `ModifyOrbValue` 钩子（行 294）——**StS1 Focus 的泛化等价物**（orbs.md R11/R12 的"焦点即值修正钩子"），加成对象是宝珠而非全局 power。

**O05 回合触发点** — 出处 `OrbQueue.cs#AfterTurnStart(92)/#BeforeTurnEnd(80)` + 调用方：玩家侧回合开始（sts2-monster-ai.md A05⑥ `OrbQueue.AfterTurnStart`）与 `DoTurnEnd` 第一步（sts2-combat-turn-machine.md T04，先于回合尾卡/Ethereal/Flush）。置信度：**高**
基类 `BeforeTurnEndOrbTrigger/AfterTurnStartOrbTrigger` 为空（OrbModel.cs 行 233-242）——**具体宝珠自行选择挂哪一侧**（StS1 的 Plasma 挂回合开始、Frost 挂回合尾的分工，在 StS2 由子类覆写决定，无引擎级固定）。

---

## 2. 附魔系统（Enchantment）

**O06 附魔规则** — 出处 `Commands/CardCmd.cs#Enchant ×2 / #ClearEnchantment`（行 520-578）。置信度：**高**
```
① CanEnchant(card) 门（不通过抛异常）
② 卡无附魔 → EnchantInternal(enchantment, amount) + enchantment.ModifyCard()（改卡面/数值）
③ 已有附魔：同类型 → Enchantment.Amount += amount（叠加）；不同类型 → 抛 InvalidOperationException
   ⇒ ★ 一卡一附魔，异类型互斥、同类型数值叠
④ FinalizeUpgradeInternal；在牌组堆时写 CardsEnchanted 历史
```
**O07 附魔参与管线的位置** — 出处 `Models/EnchantmentModel.cs`（行 21-217）+ 管线交叉。置信度：**高**
- 伤害计算：`ModifyDamage` 的**最外层**（附魔 Additive → Multiplicative 先于一切模型层，sts2-combat-semantics.md S04）。
- 出牌：卡效果 OnPlay 之后、AfterCardPlayed 之前执行 `Enchantment.OnPlay(cardPlay)`（sts2-card-play.md C03 步骤 f），随后 `InvokeExecutionFinished`。
- 钩子资格：`ShouldReceiveCombatHooks = Card?.ShouldReceiveCombatHooks`（跟随宿主卡）。
- 显示/状态：`EnchantmentStatus`、`ShouldStartAtBottomOfDrawPile`（附魔卡的洗牌落底选项）、`ShouldGlowGold/Red`、`IsStackable`、`DisplayAmount`。
- 移除：`CardCmd.ClearEnchantment`；affliction（负面附灵）是平行系统（`Afflict` 族，行 580-676，本卷未展开）。

---

## 3. StS1 → StS2 宝珠仲裁速查

| 语义 | StS1（orbs.md） | StS2（本卷） | 仲裁建议 |
|---|---|---|---|
| 满槽通道 | addToTop 三连（R04） | await EvokeNext 后入队（O02） | 语义等价；注意 StS2 Channel 会在 0 槽时自动加 1 槽 |
| 激发最左/最新 | evokeOrb / evokeNewestOrb（R06/R07） | EvokeNext / EvokeLast（O03） | 直接映射 |
| 激发不移除 | evokeWithoutLosingOrb（R07） | Evoke(dequeue:false)（O03） | 直接映射 |
| Focus | power + applyFocus 刷新（R11/R12 不对称） | ModifyOrbValue 钩子（O04） | StS2 无"冻结不回落"问题（每次数值即时钩子计算） |
| Cables 双触发 | 写死 orbs[0] 二次调用（R10） | ModifyOrbPassiveTriggerCount 钩子（O04） | StS2 是泛化；移植 StS1 Cables = +1 触发钩子 |
| 被动时点 | Plasma 回合开始/Frost·Dark 回合尾（引擎固定） | 子类覆写 AfterTurnStart/BeforeTurnEndOrbTrigger（O05） | 移植时逐珠选择挂点 |
| 空槽占位 | EmptyOrbSlot 占位对象（R01） | OrbQueue.Count < Capacity（无占位对象） | 数据模型差异，遍历逻辑别照抄 |
| 被动触发计数 | 固定 1 | triggerCount 循环（O04） | StS1"多段被动"类卡移植时用 countAffectedByHooks=true |

## 4. 开放问题 / 低置信项

1. 具体 OrbModel 子类清单与各自 Passive/Evoke 数值（EA 版本内容层）未枚举——数据层任务。
2. `OrbModel.Evoke` 返回目标集的消费方（Evoke 内 targets 用途）未逐行展开。
3. Affliction 系统（Afflict 族/负面附灵与卡的交互）未展开，平行于附魔。
4. OrbQueue.Insert 的调用方（插队类效果）未枚举。
