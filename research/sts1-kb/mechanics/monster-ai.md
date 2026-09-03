# 怪物 AI 框架（Monster AI Framework）— StS1 战斗语义知识库

## 本卷范围
AbstractMonster 的通用 AI 骨架（不含逐怪 move 表）：意图滚动（rollMove/getMove/aiRng）、move 历史与读取器（lastMove 族）、setMove 家族与 moveHistory 写入规则、意图数字管线（createIntent/calculateDamage）、回合调度（monsterQueue）、逃跑（escape/escapeNext）、复活重滚动。逐怪行为表不在本卷（数据层归 `../cards-*.json` 同级的怪物数据，未采集）。
依赖：回合调度位置 → `turn-phase.md` R05/R12；实伤管线 → `damage-pipeline.md` R07/R08。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 意图滚动

**R01 基础 rollMove = `getMove(aiRng.random(99))`** — 出处 `AbstractMonster#rollMove` offset 0-12 + `#getMove(int)`（protected abstract）。置信度：**高**
随机源是**独立的 `AbstractDungeon.aiRng`**（与洗牌 shuffleRng、卡选 cardRandomRng 并列的另一条流）；rollMove 基类实现即"掷 0-99 传给抽象 getMove(int)"——vanilla 怪物在 `getMove` 内按 roll 段选择 move，或直接覆写 `rollMove()` 完全自定义（AwakenedOne 等复活怪）。

**R02 setMove 家族：byte 进历史、组装 EnemyMoveInfo** — 出处 `AbstractMonster#setMove(String,byte,Intent,int,int,boolean)`（主构造 offset 0-41）。置信度：**高**
```
moveName = 参数;
if (move != -1) moveHistory.add(move);          ← ★ -1 表示"不计入历史"
this.move = new EnemyMoveInfo(move, intent, baseDamage, multiplier?, isDefined?);
```
字节重载族（offset 1487-1602）只是省略 moveName/History 的便捷版本。`moveHistory` 是 `ArrayList<Byte>`，按时间正序 append。

**R03 历史读取器：lastMove / lastMoveBefore / lastTwoMoves** — 出处 `#lastMove` offset 0-41、`#lastMoveBefore` offset 0-47+、`#lastTwoMoves`（1680 起，同构）。置信度：**高**
- `lastMove(b)`：历史空 → false；比较**最后一项**。
- `lastMoveBefore(b)`：历史 <2 → false；比较**倒数第二项**。
- `lastTwoMoves(b)`：最后两项都为 b。
⇒ "不连击/不三连"类模式全靠这套读取器在 getMove 里手写，无框架级去重。

**R04 首回合与后续回合的滚动时机** — 出处 `turn-phase.md` R05（首回合 roll 在 `usePreBattleAction/useUniversalPreBattleAction`）+ 调用点扫描：`RollMoveAction#update`（单帧 `m.rollMove(); isDone`）。置信度：**高**
vanilla 中 rollMove 的队列化包装只有一个（RollMoveAction）；`ReviveMonsterAction` 复活怪物后调用它重掷。多数怪物在自身 `takeTurn()` 尾部自调 rollMove 预掷下一意图（推断自子类方法面普遍引用 rollMove——中置信，未逐怪穷举）。

---

## 2. 意图数字

**R05 createIntent：显示数字与实伤同源不同算** — 出处 `AbstractMonster#createIntent`（1391 起，offset 55 调 `calculateDamage`）+ `#calculateDamage(int)`（private，3591-3706）。置信度：**高**
意图显示 = `calculateDamage(move.baseDamage)`：怪物 powers.give(NORMAL) → 玩家 powers.receive（易伤 ×1.5/Paper Frog ×1.75）→ 玩家 stance.receive（Wrath ×2）→ BackAttack ×1.5 → finalGive → finalReceive → floor（damage-pipeline.md R07/R08 同构）。**实伤**在 `applyPowers()`（3708 起）对 `move.damage` 列表逐个 `DamageInfo.applyPowers(this, player)`，存进预演算的 `DamageInfo.output`（R08）。
⇒ 仲裁要点：意图数字会随玩家状态（力/易伤/Wrath）**刷新时机**（R06）变化，但实伤取"applyPowers 执行时刻"的快照。

**R06 意图刷新时机** — 出处 `AbstractMonster#applyPowers` offset 97-131（`move.baseDamage > -1 → calculateDamage(base)` → `intentImg` 重取 + `updateIntentTip`）+ `AbstractDungeon#onModifyPower`（energy-cost.md R03 引：逐怪调 `applyPowers()`）+ `PlayerTurnEffect#<init>` 尾部 `MonsterGroup.showIntent()`。置信度：**高**
**任何 power 增删都会重算意图**：`onModifyPower` → 逐怪 `applyPowers()` → ① `this.damage` 列表逐个 `DamageInfo.applyPowers` 重演算（实伤快照，BackAttack ×1.5 在此，offset 77-92）② `calculateDamage(base)` 重算意图显示数字 ③ `intentImg`/tip 刷新。`showIntent()`（每回合横幅构造时）只负责**重绘**，不负责重算。⇒ 仲裁：玩家拿到力量/给怪挂易伤的瞬间，怪的意图数字立即变化；实伤取的是怪物上一次 `applyPowers` 时刻的快照（R08 damage-pipeline），**实伤快照与意图显示可能不一致**（如出牌过程中玩家丢失易伤，onModifyPower 已重算，实伤随后按新值）。

---

## 3. 回合调度

**R07 monsterQueue 组队与消费** — 出处 `MonsterGroup#queueMonsters` offset 0-55 + `turn-phase.md` R12。置信度：**高**
`!isDeadOrEscaped() || halfDead` 的怪按 monsters 列表序入 monsterQueue；每帧队头一只：先入队 `ShowMoveNameAction + IntentFlashAction`（下一帧才 takeTurn），再 `m.takeTurn(); m.applyTurnPowers()`；队列空后 `Wait(1.5s)` 垫尾。意图≠NONE 才有动作展示前奏。

---

## 4. 逃跑与死亡状态机

**R08 escape / escapeNext** — 出处 `AbstractMonster#escapeNext`（3405）/ `#escape`（3416）/ `#updateEscapeAnimation`。置信度：**高**
`escapeNext()` 只置标志（下回合开始逃跑）；`escape()` 立即进入 isEscaping → 动画走完 → isDead/escaped 态 → 经 `updateDeathAnimation` 的 `areMonstersDead` 检查触发 endBattle（triggers.md §5.2）。`escaped` 不清金币（对比 SuicideAction 清金币，death-arbitration.md R08——**两种"消失"对奖励不同**）。

**R09 heal(int)** — 出处 `AbstractMonster#heal`（1313）+ `AbstractCreature#heal`（isPlayer=false ⇒ 无 relic 链，仅 powers.onHeal）。置信度：**高**

---

## 5. StS2 对照

**R10** StS2 的对应物是 `MonsterModel` + `MultiAttackMoveMonster/SingleAttackMoveMonster` 基类与 intent 命令族（`Entities.Intents/`），power 变化会即时 `UpdateIntent`（见 `../../kb/sts2-combat-semantics.md` §8 开放问题 3——StS2 AI 框架专卷待做）。置信度：**中**（跨卷引用）。

---

## 6. 开放问题 / 低置信项

1. 逐怪 takeTurn 尾部自调 rollMove 的比例未穷举（R04）。置信度：**中**。
2. `moveHistory` 上限（是否有裁剪）未取证。置信度：**低**。
3. `EnemyMoveInfo` 第 4/5/6 参的准确语义（multiplier/isDefined）未逐字段核。置信度：**中**。
4. ~~意图在 onModifyPower 是否刷新~~ **已结案**（R06）：刷新（经 applyPowers 内 calculateDamage）。"实伤快照 vs 意图显示可能短暂不一致"的具体场景未做运行时验证。置信度：**中**。
