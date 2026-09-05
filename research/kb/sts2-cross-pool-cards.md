# StS2 跨池/用池卡牌普查（Cross-Pool Cards Census, EA build）— sts2-spire1 知识库

## 本卷范围
对 `Models.Cards/` 全部 26 个引用池 API 的卡逐一分类，并精读唯一的原生跨池卡 **Splash** 的集合运算（它就是 `pool-architecture.md` I1 不变量的"原生带菌样本"——我们的移植 bug 复刻了它的模式）。产出：移植/改写任何用池卡时的对照表与规则。
来源：`research/engine-dllsrc/MegaCrit.Sts2.Core.Models.Cards/`。置信度 **高**=源码直接可证。

---

## 1. 普查分类（26 张）

**C01 四类消费模式** — 出处 全目录扫描（方法：`/CardPool|AllCharacter/` 命中 → 逐卡读池 API 形态）。置信度：**高**

| 类别 | 卡 | 池 API 形态 |
|---|---|---|
| A. 跨池/多池（真正按"角色集合"取卡） | **Splash**（唯一） | `UnlockState.CharacterCardPools` + `list.Remove(owner.Character.CardPool)` |
| B. 自己池 | Abundance, Discovery, Distraction, InfernalBlade, Jackpot, MadScience, Metamorphosis, Stoke, WhiteNoise, Fasten | `Owner.Character.CardPool.GetUnlockedCards(...)` |
| C. 显式共享池（无色） | BundleOfJoy, JackOfAllTrades, Largesse, ManifestAuthority, Quasar | `ModelDb.CardPool<ColorlessCardPool>()` |
| D. 仅显示用池（`VisualCardPool` 覆写，不参与候选集） | Caltrops, Clash, DualWield, Entrench, HeirloomHammer, HelloWorld, Outmaneuver, Rebound, RipAndTear, Stack | `override CardPoolModel VisualCardPool => ModelDb.CardPool<X>()` |

要点：**D 类的 VisualCardPool 是纯显示**（生成卡的颜色/卡背视觉），与候选集无关——移植时勿把"某卡 Visual 指向某池"误读为"它从那池取卡"。

## 2. Splash：原生集合运算精读

**C02 候选集构造** — 出处 `Models.Cards/Splash.cs#OnPlay`。置信度：**高**
```
list = Owner.UnlockState.CharacterCardPools.ToList();      // 解锁态下的"角色池"集合
if (list.Count > 1) list.Remove(Owner.Character.CardPool); // ★ 池对象排除 + 单池护栏
cards = list.SelectMany(p => p.GetUnlockedCards(Owner.UnlockState, RunState.CardMultiplayerConstraint))
            .Where(c => c.Type == CardType.Attack);
choices = CardFactory.GetDistinctForCombat(owner, cards, 3, RunState.Rng.CombatCardGeneration);
IsUpgraded → 逐张 CardCmd.Upgrade(choices)
cardModel = await CardSelectCmd.FromChooseACardScreen(ctx, choices, owner, canSkip: true);
```
**C03 与 I1 的关系（vanilla 为何"能跑"、mod 为何炸）** — 出处 C02 + `pool-architecture.md` I1 + DEVLOG 修复 #10。置信度：**高**
vanilla 安全依赖两个隐含条件：①CharacterCardPools 只含**角色专属池**（共享卡在无色池，天然不在候选）；②`owner.Character.CardPool` 对象与该角色可调用专属卡集合相等。我方 SharedCardReuse 打破② ⇒ 复刻此模式即复现事故（一代 Defect 从"其他角色"选出自己已有的官方 Defect 卡）。另注意 **`Count > 1` 护栏**：只有一个角色池时**不做排除**——vanilla 自己就接受"此时候选含自己的卡"，移植时必须显式决定我方策略（保持原样 or 恒差集）。
**C04 文本 vs 代码细微差** — 出处 C02（`SetToFreeThisCombat()`）+ 官方 zhs 文案"该张牌在本回合免费打出"。置信度：**高**（实现是**本战斗**免费；文案写"本回合"）。移植文案照录官方原文，机制实现需自知差异。

## 3. B/C 类的可复用语义

**C05 解锁过滤与去重工具** — 出处各 B/C 类卡 OnPlay。置信度：**高**
- `GetUnlockedCards(unlockState, cardMultiplayerConstraint)`：候选一律先过**解锁态 + 联机约束**过滤——移植卡若直接拿 `AllCards` 就会漏掉这两层。
- `CardFactory.GetDistinctForCombat(owner, cards, n, Rng.CombatCardGeneration)`：n 张**互不重复**（按 Id）的战斗用实例；`GetForCombat`（Metamorphosis 用）允许重复。RNG 流固定为 `CombatCardGeneration`（M12 分账）。
- JackOfAllTrades（C 类）同样走 GetDistinctForCombat —— "无色池抽 3"与"跨池抽 3"的工具链相同，差异只在输入集合。

## 4. 移植规则（对照我方 SplashOwnSetSubtractPatch）

**C06** — 出处 `pool-architecture.md` I1 + 本卷 C02。置信度：**高**
1. 候选全集 = 角色池并集（StS1 侧为四色+mod 池）+ 是否含无色池按卡面文案定（StS2 Splash 不含无色池）。
2. 排除集 = **按卡牌 Id 的"当前角色可调用集合"**（我方实现含 SharedCardReuse/官方复用卡），绝不用池对象排除。
3. 保留 `Count > 1` 类护栏时要重新审视：我方多角色生态下护栏语义需显式定义（建议：恒差集，护栏仅防空集）。
4. 去重用 GetDistinctForCombat 语义（按 Id 不重复）；RNG 流对齐 CombatCardGeneration。
5. 升级联动（IsUpgraded → 候选逐张 Upgrade）与 canSkip 选择屏按卡面忠实保留。

## 5. 开放问题 / 低置信项

1. `UnlockState.CharacterCardPools` 的集合来源（随解锁进度变化？联机合并规则？）未逐行展开。
2. `Count > 1` 护栏在 EA 实际可否触达（五角色是否恒 ≥2 池）未验证。
3. D 类 VisualCardPool 的全部消费方（卡面渲染/图鉴）未穷举。
