# StS2 玩家回合启动（Player Turn Setup, EA build）— sts2-spire1 知识库

## 本卷范围
`CombatManager.cs#SetupPlayerTurn`（行 880-933）逐行语义与它在 `StartTurn` 中的位置：能量重置/叠加的门（ShouldPlayerResetEnergy）、默认手牌数 5 与抽牌数钩子、首回合 Innate/附魔沉底保证、抽牌与回合开始钩子的顺序。关闭 `sts2-combat-turn-machine.md` 开放问题 3 的一部分。
来源：`research/engine-dllsrc/`。置信度 **高**=源码直接可证（行号会漂移）。

---

## 1. SetupPlayerTurn 全序

**U01 主链** — 出处 `CombatManager.cs#SetupPlayerTurn`。置信度：**高**
```
① 守卫：creature 死 / PlayerCombatState null → return
② if (Hook.ShouldPlayerResetEnergy(state, player)) → ResetEnergy()（+gain_energy 音效）
   else                            → AddMaxEnergyToCurrent()   ← ★ 额外回合分支：叠加不重置
   → Hook.AfterEnergyReset
③ Hook.BeforeHandDraw(state, player)
④ handDraw = Hook.ModifyHandDraw(state, player, 5m, out modifiers)   ← ★ 默认抽 5（常量直证）
   → Hook.AfterModifyingHandDraw
⑤ TurnNumber == 1（仅首回合）：
     a. 抽牌堆中 Enchantment.ShouldStartAtBottomOfDrawPile 的卡 → MoveToBottomInternal
     b. Innate 关键词卡（排除 a 中已沉底的）→ MoveToTopInternal
     c. handDraw = max(handDraw, innate 数量)   ← ★ 保证 Innate 全部抽到
     d. handDraw = min(handDraw, CardPile.MaxCardsInHand)   ← 手牌上限在抽牌数上先钳
⑥ await CardPileCmd.Draw(ctx, handDraw, player, fromHandDraw: true)
⑦ Hook.AfterPlayerTurnStart(state, ctx, player)
```

## 2. 与 StartTurn 其他段的相对顺序

**U02 一个玩家回合的完整段序** — 出处 `CombatManager.cs#StartTurn`（sts2-monster-ai.md A05 已录）+ 本卷 U01。置信度：**高**
```
BeforeTurnStart（power 数量快照）→ Hook.BeforeSideTurnStart
→ 逐生物 AfterTurnStart（玩家首回合跳过）→ ClearBlock（ShouldClearBlock 门）
→ 逐生物 Hook.AfterBlockCleared
→ 逐玩家 SetupPlayerTurn【本卷 U01：能量 → 抽牌】
→ Hook.AfterSideTurnStart
→ 逐玩家 OrbQueue.AfterTurnStart（宝珠回合开始触发）
→ AutoPrePlay 阶段 → Phase=Play（可操作）
```
仲裁要点：
- **能量处理先于抽牌**，且都晚于格挡清除（StS1 侧对照：能量重置在 PlayerTurnEffect 构造器、格挡掉落在新回合块、抽牌在最后——三者相对次序两侧一致，见 energy-cost.md R03 与 turn-phase.md R13）。
- **额外回合不重置能量**（AddMaxEnergyToCurrent）⇒ StS2 的"再动一回合"类效果能量滚存；StS1 对应场景（TimeWarp 额外回合）的能量行为未逐字节终验（开放问题迁移至 turn-control.md）。
- Innate 抽满保证是**引擎级**（c 项 max 修正），StS1 同语义在 `CardGroup#initializeDeck`（Innate/瓶装置顶 + 超出部分转 preTurnActions 补抽，draw-exhaust.md §1.1）——实现路径不同（StS1 靠牌堆排序+补抽，StS2 靠加大抽牌数）。
- 附魔沉底（ShouldStartAtBottomOfDrawPile）**优先于** Innate 置顶：一张卡同时有两者时沉底赢（b 项 Except(list)）。

## 3. 开放问题 / 低置信项

1. `ShouldPlayerResetEnergy` 的 vanilla 实现者（首回合 true、额外回合 false 的判定位置）未枚举——归钩子矩阵卷的 JSON 可查（AfterEnergyReset 族在侧）。
2. `CardPile.MaxCardsInHand` 的值（预期 10，对齐 StS1）未逐常量验证。
3. 多玩家场景 SetupPlayerTurn 的并行调度（setupPlayerTurnContext 列表 + WaitForCompletion）细节归联机卷。
