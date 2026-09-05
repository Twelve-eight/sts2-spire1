# StS2 出牌管线（Card Play Pipeline, EA build）— sts2-spire1 知识库

## 本卷范围
StS2 一张牌从点击/自动触发到归堆的完整时序：手动出牌（PlayCardAction）、自动出牌（CardCmd.AutoPlay）、资源结算（能量/星星、X 费捕获）、OnPlayWrapper 主循环（playCount/附魔/affliction/结果堆）、与 StS1 的仲裁差异。
来源：`research/engine-dllsrc/` 反编译 C#（Godot EA）。置信度 **高**=源码直接可证；引用格式 `文件#方法`。StS1 对照卷：`../sts1-kb/mechanics/`（turn-phase R16/R17、triggers.md §2、energy-cost.md）。

---

## 1. 手动出牌（PlayCardAction）

**C01 执行门序** — 出处 `GameActions/PlayCardAction.cs#ExecuteAction`（行 62-104）。置信度：**高**
```
① 卡不在手牌堆（pile.Type != Hand）→ 取消出队
② 卡.CanPlay(out reason, out preventer) || !IsValidTarget(target) → Cancel()
③ tuple = await _card.SpendResources()          ← ★ 先扣资源
④ ResourceInfo{EnergySpent/EnergyValue=实扣, StarsSpent/StarValue=实扣}
⑤ await _card.OnPlayWrapper(ctx, target, isAutoPlay:false, resources)
```

**C02 SpendResources：能量/星星双资源 + 星星抵超支** — 出处 `Models/CardModel.cs#SpendResources/#SpendEnergy/#SpendStars`（行 1807-1856）。置信度：**高**
```
energyToSpend = EnergyCost.GetAmountToSpend(); starsToSpend = max(0, GetStarCostWithModifiers())
若 energyToSpend > 现有能量 且 Hook.ShouldPayExcessEnergyCostWithStars(...)：
    starsToSpend += (缺口) × 2;  energyToSpend = 现有能量      ← ★ 超额能量用星星按 1:2 抵
SpendEnergy: CostsX → EnergyCost.CapturedXValue = 实扣额；History.EnergySpent → LoseEnergy → Hook.AfterEnergySpent
SpendStars: LastStarsSpent 记录 → LoseStars → Hook.AfterStarsSpent
```
**StS1 重大差异**：StS1 扣能在 `card.use()` 效果全部入队**之后**（turn-phase.md R16 步骤⑧）；StS2 在 OnPlay（效果执行）**之前**先扣——"扣能瞬间与效果瞬间"的仲裁结果不同（例如 X 费/0 费联动、能量相关 power 的时点）。

## 2. OnPlayWrapper 主循环

**C03 完整序** — 出处 `Models/CardModel.cs#OnPlayWrapper`（行 1858-2019）。置信度：**高**
```
0. 压 context；WaitForUnpause；记 target
1. 进 Play 堆：手动=AddDuringManualCardPlay；自动=Add(Play, Bottom)+0.25s wait
2. resultLocation = GetResultLocationForCardPlay()
   → Hook.ModifyCardPlayResultLocation（可改归堆去向，含联机 GiveToAnotherPlayer 位）
3. playCount = await GeneratePlayCount(combatState, target)   ← ★ 打 N 次，钩子可改
4. BeginCardOrPotionEffect（作用域 combatId，结束后 finally 收口）
5. for i < playCount（每次）:
     a. Power 牌飞行动画 / 多次打的桌牌动画
     b. new CardPlay{Card, Player, Target, ResultPile, Resources, IsAutoPlay,
                     PlayIndex=i, PlayCount}（IsFirstInSeries/IsLastInSeries 可查）
     c. await Hook.BeforeCardPlayed → History.CardPlayStarted
     d. await OnPlay(branchingContext, cardPlay)                ← 卡牌效果本体
        owner 死亡 → 立即 return
     e. InvokeExecutionFinished
     f. ★ Enchantment != null → await Enchantment.OnPlay(cardPlay)（在卡效果之后！）
     g. ★ Affliction != null → await affliction.OnPlay(target)
     h. History.CardPlayFinished → await Hook.AfterCardPlayed
6. finally EndCardOrPotionEffect；补足最短演出 wait
7. 归堆：resultLocation.pileType 分流
     None → RemoveFromCombat（消耗性"用后消失"）；
     Exhaust → CardCmd.Exhaust；其余 → CardPileCmd.Add(pile,pos)；
     联机异主 → GiveToAnotherPlayer
8. CheckForEmptyHand
9. EnergyCost.AfterCardPlayedCleanup()（临时费用清理）→ EnergyCostChanged 事件
   临时星星费用（ClearsWhenCardIsPlayed）清理
10. Played 事件 → 弹 context
```
与 StS1 对照：StS1 的"四时刻"（onPlayCard/onUseCard/triggerOnCardPlayed/onAfterUseCard，triggers.md §3）在 StS2 压缩为 **BeforeCardPlayed → OnPlay → 附魔.OnPlay → affliction.OnPlay → AfterCardPlayed** 五段；StS1 的 POWER 牌"用后消失"对应 ResultPile=None 分流，消耗对应 Exhaust 分流，堆去向可被钩子改写（≈StS1 的 rebound/shuffleBack/returnToHand 字段机制）。

**C04 playCount 循环语义** — 出处 C03 步骤 3/5 + `CardPlay.IsFirstInSeries/IsLastInSeries`（`Entities.Cards/CardPlay.cs` 行 66-73）。置信度：**高**
"打出 N 次"（复制类效果）= OnPlay 效果体整体重跑 N 遍（每次新的 CardPlay 与新的 BranchingContext），附魔/affliction 同样每遍执行；StS1 的 Burst/Echo 走 `queueExtraCard` 生成副本卡入 cardQueue（action-manager.md R19）——**副本卡 vs 重跑循环**是两种模型，移植时 StS1 复制类效果在 StS2 侧应映射为 playCount 钩子或显式多次 AutoPlay，行为差异要逐一回归（目标选择、快照时机均不同）。

## 3. 自动出牌（CardCmd.AutoPlay）

**C05 门序** — 出处 `Commands/CardCmd.cs#AutoPlay`（行 51-142）。置信度：**高**
```
① 战斗结束/owner 死 → return
② Unplayable 关键词 → MoveToResultPileWithoutPlaying（进 Play 堆后直接走归堆，不执行效果）
③ !Hook.ShouldPlay(combatState, card, out preventer, type) → 同上 + 思考气泡（BlockedByHook）
④ AnyEnemy/AnyAlly 无目标 → 从 RunState.Rng.CombatTargets 随机选；仍无 → 放弃执行直接归堆
⑤ X 费：CapturedXValue = 当前能量（skipXCapture 可关）；星星费记录 LastStarsSpent
⑥ 不在手牌堆 → CardPileCmd.Add(card, Play)
⑦ await Hook.BeforeCardAutoPlayed(combatState, card, target, type)
⑧ ResourceInfo{EnergySpent=0, EnergyValue=GetAmountToSpend(), …}   ← ★ 自动出牌不扣能量
⑨ await OnPlayWrapper(isAutoPlay:true, resources)
```
**C06 自动出牌免费 + X 费=当前全部能量** — 出处 C05 步骤⑤⑧。置信度：**高**
StS1 的自动打出（autoplay 卡牌如悔恨/Doubt）免能量经 `isInAutoplay → freeToPlayOnce=true`（turn-phase.md R16 步骤3），X 费钳制为当前能量（同步骤③）；StS2 直接在 ResourceInfo 置 EnergySpent=0（不进 SpendResources），X 捕获在 AutoPlay 里完成。语义一致但机制不同：**StS2 的 X 自动打出捕获的是"当时的全部能量"且不实际扣除**（Mayhem 类效果移植对拍点：StS1 自动打出会真的扣 X 点能量吗？——不会，isInAutoplay 免费但 X 值仍取当前能量。两侧一致）。

## 4. 仲裁案例表

| 场景 | StS2 行为 | 依据 |
|---|---|---|
| 能量 2 打 3 费牌 + "星星抵超支" power | 扣 2 能量 + 2 星星（缺口 1×2） | C02 |
| Burst 类复制 | playCount 循环重跑 OnPlay（每遍独立 CardPlay），非副本卡 | C03/C04 |
| 附魔牌打出 | 卡效果 → 附魔 OnPlay → AfterCardPlayed（附魔在卡效果之后） | C03 步骤 f |
| Mayhem 自动打出 X 费 | 捕获当前全部能量、不扣能 | C05/C06 |
| Unplayable 牌被强制自动打出 | 不执行效果，直接走归堆（等价 StS1 dontTriggerOnUseCard 路径的强反） | C05② |
| "用后从游戏中消失"的牌 | ResultPile=None → RemoveFromCombat | C03 步骤 7 |
| 出牌中途 owner 死亡 | OnPlay 循环各阶段检查后立即 return（不再触发后续钩子） | C03 步骤 5 |

## 5. 开放问题 / 低置信项

1. `GetResultLocationForCardPlay` 的默认分流规则（Exhaust/Ethereal/Retain→保留位）未展开。置信度：**中**。
2. `GeneratePlayCount` 的钩子名与默认值（1）未逐行读（行 2029-2039）。置信度：**中**。
3. `CardPileCmd.AddDuringManualCardPlay` 与净同步（NetCombatCard 双构造路径）的联机时序未展开——联机卷（mechanics-v3）延伸。置信度：**未定**。
4. Affliction（负面附灵）系统本体未取证（与 Enchantment 平行，见 CardCmd.Afflict 族）。置信度：**未定**。
