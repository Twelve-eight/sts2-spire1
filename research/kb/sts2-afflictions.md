# StS2 负面附灵系统（Afflictions, EA build）— sts2-spire1 知识库

## 本卷范围
`AfflictionModel` 与 `CardCmd.Afflict` 族：施加门序、一卡一附灵规则、与附魔（Enchantment，`sts2-orbs-enchantments.md` O06/O07）的平行结构、出牌管线中的执行位（`sts2-card-play.md` C03 步骤 g）、vanilla 附灵清单与实现模式。
来源：`research/engine-dllsrc/`。置信度 **高**=源码直接可证。

---

## 1. 施加管线

**F01 Afflict 门序与合并规则** — 出处 `Commands/CardCmd.cs#Afflict(AfflictionModel, CardModel, decimal)`（行 627-668）。置信度：**高**
```
① 战斗结束/收尾中且卡在战斗内堆 → null（不放）
② !Hook.ShouldAfflict(combatState, card, affliction) → null     ← 引擎级豁免钩子
③ !affliction.CanAfflict(card) → null
     CanAfflict 基类链（AfflictionModel.cs 行 190-200）：卡类型白名单（CanAfflictCardType，
     基类默认全放？行 175 虚方法）→ Unplayable 关键词默认拒（CanAfflictUnplayableCards=false 可放）
④ 卡无附灵 → AfflictInternal(affliction, amount) + affliction.AfterApplied()
   已有附灵：同类型 → Affliction.Amount += amount（数值叠）
             不同类型 → 抛 InvalidOperationException    ⇒ ★ 一卡一附灵，与附魔同构
⑤ History.CardAfflicted
```
`AfflictAndPreview<T>`（行 580-603）：批量施加逐卡独立判定（失败的卡跳过），同主校验（跨主抛异常）后统一 Preview 动画 1.2s+wait。**警告注释同 Discard 族**：多卡效果勿在循环里逐卡调单卡版。

**F02 施加清单** — 出处 `Models.Afflictions/` 目录：`Bound, Entangled, Galvanized, Hexed, Ringing, Smog, Tainted`（7 个 vanilla 附灵）。置信度：**高**

## 2. 执行位与实现模式

**F03 OnPlay 位置** — 出处 `Models/CardModel.cs#OnPlayWrapper` 行 1949-1957（`sts2-card-play.md` C03）：卡效果 → 附魔 OnPlay → **附灵 OnPlay(choiceContext, target)** → AfterCardPlayed；owner 死亡同样短路。`AfflictionModel#OnPlay`（行 215）基类空，附灵自带逻辑。置信度：**高**

**F04 实现模式：逻辑旁挂 Power + 自清条件** — 出处 `Models.Afflictions/Hexed.cs` 全文。置信度：**高**
Hexed 的注释直言"Most of this Affliction's logic lives in HexPower"；附灵本体只做：
- 展示层（ExtraHoverTips 绑 CardKeyword、Overlay 贴图路径）；
- 生命周期钩子（如 `AfterCardEnteredCombat`：同卡进入战斗且 owner 无 HexPower 时 `ClearAffliction` 自清）。
⇒ 移植 StS1 诅咒/状态牌逻辑时：**卡的负面效果 → 附灵（卡面层）+ Power（结算层）双件套**是 StS2 的惯用拆分；`CanAfflictUnplayableCards` 决定能否挂在不可打出卡上（StS1 的 Status 牌语义对照）。

## 3. 与附魔的平行结构对照

| 维度 | Enchantment（O06/O07） | Affliction（本卷） |
|---|---|---|
| 每卡数量 | 1 枚，同类型叠 Amount | 1 枚，同类型叠 Amount |
| 类型互斥 | 异类型抛异常 | 异类型抛异常 |
| 施加门 | CanEnchant | ShouldAfflict（引擎钩子）+ CanAfflict（类型/Unplayable） |
| 出牌位 | 卡效果后（C03 f） | 附魔后（C03 g） |
| 伤害计算 | ModifyDamage 最外层 | 无（走 CardModel/Affliction 各自实现） |
| 语义 | 正面强化 | 负面/中性（Galvanized 等可被利用者除外） |

## 4. 开放问题 / 低置信项

1. 7 个附灵各自效果细节（Bound/Entangled/Galvanized 等的 Power 旁挂体）未逐个展开——数据层任务。
2. `CanAfflictCardType` 的默认白名单（行 175-183 未读全）。
3. `CardCmd.ClearAffliction` 的调用方清单未枚举（清除类效果）。
