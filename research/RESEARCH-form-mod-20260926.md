# RESEARCH form-mod 2026-09-26 (read-only scout report)

## 路径与身份说明

- 派发文本给出的报告路径含未解析占位符 `<host>`: `G:\omp works\<host>\sts2-spire1\<host>\RESEARCH-form-mod-20260926.md`.
- 本工作区实际布局 (AGENTS.md Sec 0, docs/WORKSPACE-PROJECTS.md:138-145): 项目根为 `G:\omp works\Sts\sts2-spire1`, 分类目录为 `Sts`. 占位符 `<host>` 在本机不存在任何同名目录 (已核对 `G:\omp works\LAY1NN`, `G:\omp works\Lay1nn`, `G:\omp works\Sts\sts2-spire1\<host>` 均不存在).
- 按本工作区既有先例 (同批报告 `gateway-followup-audit-r1.md:111` 记录: `<host>` 是模板残留, 不得新建 host 目录, 应落到真实路径), 本报告落盘于 `G:\omp works\Sts\sts2-spire1\research\RESEARCH-form-mod-20260926.md`. 未创建任何 host 目录, 未写入 C:, 未 git, 未构建, 未运行游戏, 未改配置.
- 角色: 只读研究员 (scout). 唯一写入 = 本文件.
- 模型与路由: 用户指定值 `global:deepseek-v4.1-flash` (首选), 路由未在派发文本中给出 provider; 本会话实际解析模型/路由 = 未知 (未取得可信会话元数据, 不以请求文字代替元数据证据).

## 需求修正记录 (用户 2026-09-26 追加)

1. 每种姿态同时挂载两种形态效果 (平静=虚空+群蛇; 愤怒=恶魔+死神; 神格=回响+天人), 需确认一个姿态对象可挂多组 hook.
2. 恶魔形态: 加力量与 "受伤+n" 随退出姿态消失, 需定位退出 hook 能否回滚本姿态施加的 Strength 与受伤修正.
3. 死神形态 = 每次出牌对怪物施加等同该次伤害的灾厄类减益层数 (不是回血). 需定位 "出牌伤害数值获取 + 给敌人施加 debuff 层数" 的 API.

## 已确认

### A. StS2 引擎侧: 不存在内置 Stance 系统 (实测反编译)

- 证据: 对 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc` (3538 个 .cs, 引擎反编译转储) 做大小写敏感检索 `Stance`, 命中文件数为 0. 引擎内不存在 `StanceModel`/`StanceType`/`onEnterStance` 之类类型或方法.
- 结论: 姿态在 StS2 是 mod 层概念, 用 `PowerModel` 子类实现. 本项目已有两套 mod 层实现 (见 D/E 段), 新 mod 应沿用该模式, 不要等待引擎 API.
- 证据路径: `research/engine-dllsrc/MegaCrit.Sts2.Core.Models/AbstractModel.cs` (2271 行), `.../PowerModel.cs` (566 行), `.../Hooks/Hook.cs` (2417 行).

### B. PowerModel 基类关键成员 (StS2 实测反编译, 行号为当前转储)

文件: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs`

- `public virtual PowerInstanceType InstanceType => PowerInstanceType.None;` (:144)
- `public bool IsVisible` (:150); `protected virtual bool IsVisibleInternal` (隐藏用)
- `public int Amount` (:187); `public int AmountOnTurnStart` (:205); `public virtual int DisplayAmount => Amount;` (:218)
- `public virtual bool AllowNegative => false;` (:238) — 需要允许负层数 (力量) 时须覆写
- `public Creature Owner` (:264); `public ICombatState CombatState => Owner.CombatState;` (:287); `public Creature? Applier` (:289)
- `public virtual bool ShouldScaleInMultiplayer => false;` (:342)
- `public void SetAmount(int amount, bool silent = false)` (:542) — 直接同步层数显示, 不触发 PowerCmd 流程
- `public PowerModel ToMutable(int initialAmount = 0)` (:555)
- `public void ApplyInternal(Creature owner, decimal amount, bool silent = false)` (:564)
- `public virtual Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)` (:608)
- `public virtual Task AfterApplied(Creature? applier, CardModel? cardSource)` (:619) — 姿态进入时的挂 VFX/一次性结算入口
- `public virtual Task AfterRemoved(Creature oldOwner)` (:628) — 姿态退出时的回滚入口 (关键)
- `public virtual bool ShouldPowerBeRemovedAfterOwnerDeath()` (:637); `public virtual bool ShouldOwnerDeathTriggerFatal()` (:646)
- `protected override object? InitInternalData()` + `GetInternalData<T>()` — 每实例私有状态容器 (SerpentForm/VoidForm/Calamity 都用它存字典/计数器)

枚举:
- `PowerStackType { None, Counter, Single }` — `research/engine-dllsrc/MegaCrit.Sts2.Core.Entities.Powers/PowerStackType.cs`
- `PowerType { None, Buff, Debuff }` — `.../PowerType.cs`
- `PowerInstanceType` (Instanced / InstancedPerApplier / None) — `.../PowerInstanceType.cs`; 决定 `PowerCmd.FindExistingInstanceForStacking` 的合并策略 (`PowerCmd.cs:169-178`)

### C. 施加 / 移除 / 层数 API (StS2 实测反编译)

文件: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs`

- `public static async Task<T?> Apply<T>(PlayerChoiceContext choiceContext, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel` (:71) — 返回 null 的三种情况: 战斗结束 / 被 Artifact 之类拦截 / 层数被改成 0 (:66-69 注释)
- `public static async Task Apply(PlayerChoiceContext choiceContext, PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)` (:105)
- `public static async Task Decrement(PowerModel power)` (:184) — 内部走 `ModifyAmount(..., -1m, null, null)`
- `public static async Task<int> ModifyAmount(PlayerChoiceContext choiceContext, PowerModel power, decimal offset, Creature? applier, CardModel? cardSource, bool silent = false)` (:219)
- `public static async Task Remove<T>(Creature creature) where T : PowerModel` (:282)
- `public static async Task Remove(PowerModel? power)` (:291) — 调用 `power.RemoveInternal()`, 等待 0.2-0.4s, 再调 `power.AfterRemoved(power.Owner)` (:293-298)
- 关键: `Apply` 会按顺序触发 `Hook.BeforePowerAmountChanged` (:124) -> `Hook.ModifyPowerAmountGiven` (:129) -> `Hook.ModifyPowerAmountReceived` (:131) -> `power.BeforeApplied` (:136) -> `ApplyInternal` (:139) -> `power.AfterApplied` (:159) -> `Hook.AfterPowerAmountChanged` (:160). 玩家侧 Debuff 会设 `SkipNextDurationTick = true` (:148-151).

其它命令:
- `PlayerCmd.GainEnergy(decimal amount, Player player)` — `...\Commands\PlayerCmd.cs:29`
- `CardPileCmd.Draw(PlayerChoiceContext, decimal count, Player player, bool fromHandDraw = false)` — `...\Commands\CardPileCmd.cs:982`; 单张版 `Draw(ctx, Player)` (:969); `DrawWithoutBlockingOnOtherPlayers(ctx, count, player, source, fromHandDraw)` (:1000)
- `CreatureCmd.Damage(...)` 多重重载 (:99/:118/:133/:147/:164/:180/:201/:221/:240/:258); `CreatureCmd.GainBlock(Creature, decimal, ValueProp, CardPlay?, bool fast = false)` (:668); `CreatureCmd.Kill(Creature, bool force = false)` (:446)
### D. 姿态系统现状 A: 主 mod (Spire1) 已有可复用实现 — 建议直接作为新 mod 的脚手架

这是本工作区**已有的**、正在维护的 StS2 观者姿态实现 (不是 .tmp 里的历史反编译件). 新 "形态" mod 应复用它, 不要从零写.

#### D.1 姿态基类与四姿态

| 类型 | 文件 (绝对路径) | 关键成员 |
|---|---|---|
| `Spire1.Spire1Code.Powers.StancePower` (abstract) | `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\StancePower.cs:6` | `override PowerStackType StackType => PowerStackType.Single;` (:8); `public abstract string StanceName { get; }` (:10); 继承 `Spire1Power` -> `BaseLib.Abstracts.CustomPowerModel` -> `MegaCrit.Sts2.Core.Models.PowerModel` |
| `CalmPower` | `...\Powers\CalmPower.cs:9` | `StanceName => "Calm"` (:13); `override async Task AfterRemoved(Creature oldOwner)` (:21) -> `PlayerCmd.GainEnergy(2m, oldOwner.Player)` |
| `WrathPower` | `...\Powers\WrathPower.cs:10` | `StanceName => "Wrath"` (:14); `override decimal ModifyDamageMultiplicative(...)` (:22) -> `props.IsPoweredAttack()` 门 + `dealer == Owner` 或 `target == Owner` 时 x2 |
| `DivinityPower` | `...\Powers\DivinityPower.cs:14` | `StanceName => "Divinity"` (:18); `ModifyDamageMultiplicative` (:26) -> 仅 `dealer == Owner` 时 x3; `override async Task AfterPlayerTurnStart(...)` (:46) -> `PowerCmd.Remove(this)` (回合开始自动退) |
| `MantraPower` | `...\Powers\MantraPower.cs:6` | `PowerType.Buff`, `PowerStackType.Counter` |
| (无姿态) | 由 `StanceCmd.Current(player)` 返回 null 表示 | 无 NeutralPower 类型 |

注意: 该实现用 **一个 Power = 一个姿态** 的模型, 姿态之间互斥由 `StanceCmd` 保证, 引擎侧没有约束.

#### D.2 StanceCmd — 姿态切换的唯一入口 (关键脚手架)

文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs`

- `private const int _mantraThreshold = 10;` (:16)
- `public static bool IsIn<TStance>(Player player) where TStance : StancePower` (:18) — `player.Creature.GetPower<TStance>() != null`
- `public static StancePower? Current(Player player)` (:23) — `player.Creature.Powers.OfType<StancePower>().FirstOrDefault()`
- `public static async Task Enter<TStance>(PlayerChoiceContext ctx, Player player, CardModel? source) where TStance : StancePower` (:28)
  - 已是同姿态 -> 直接 return (同姿态幂等, 与 StS1 `ChangeStanceAction` 一致)
  - `current != null` -> `await PowerCmd.Remove(current)` (:42) — 触发旧姿态 `AfterRemoved`
  - `TStance? entered = await PowerCmd.Apply<TStance>(ctx, player.Creature, 1m, player.Creature, source);` (:45)
  - `entered == null` -> return (:47)
  - `if (entered is DivinityPower) await PlayerCmd.GainEnergy(3m, player);` (:51)
  - `await Dispatch(player, ctx, current, entered);` (:54)
- `public static async Task Exit(PlayerChoiceContext ctx, Player player, CardModel? source)` (:56) — `PowerCmd.Remove(current)` 后 `Dispatch(player, ctx, current, null)`
- `public static async Task GainMantra(PlayerChoiceContext ctx, Player player, decimal amount, CardModel? source)` (:68) — `PowerCmd.Apply<MantraPower>`; `while (mantra.Amount >= 10)` 扣 10 后 `Enter<DivinityPower>`, 带防死循环护栏 (注释 :82-86, 代码 :87-95)
- `private static async Task Dispatch(Player player, PlayerChoiceContext ctx, StancePower? from, StancePower? to)` (:99)
  - 收集 `player.Creature.Powers.OfType<IOnStanceChanged>()` (:104-112)
  - 再收集 `player.PlayerCombatState.AllPiles` 里所有卡上实现的 `IOnStanceChanged` (:114-126)
  - 逐个 `await listener.OnStanceChanged(ctx, from, to);` (:128-131)

**这是本次需求 1 与 2 的核心答案**: 姿态切换事件由 mod 层自己 dispatch, 不是引擎 hook; 一个姿态 Power 上可挂任意多组 hook (`PowerModel` 的虚方法都可覆写, 无数量限制), 也可让多个 power 同时实现 `IOnStanceChanged`.

#### D.3 IOnStanceChanged 订阅接口

文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\IOnStanceChanged.cs:7-10`

```csharp
public interface IOnStanceChanged
{
    Task OnStanceChanged(PlayerChoiceContext ctx, StancePower? from, StancePower? to);
}
```

现有实现者 (同仓):
- `RushdownPower` (`...\Powers\RushdownPower.cs:14`) — `if (to is not WrathPower || Amount <= 0) return;` 然后 `CardPileCmd.Draw(ctx, Amount, Owner.Player)` (:25-31)
- `MentalFortressPower` (`...\Powers\MentalFortressPower.cs:15`) — `Amount <= 0` 直接返回, 否则 `CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null)` (:26-32) — 注意它**没有**比较 from/to, 因此退出姿态 (to == null) 也会触发
- `SimmeringFuryPower` (`...\Powers\SimmeringFuryPower.cs:15`) — 不走订阅, 用 `AfterPlayerTurnStart` 调 `StanceCmd.Enter<WrathPower>` 后抽牌并 `PowerCmd.Remove(this)` (:26-36)
- `DevotionPower` (`...\Powers\DevotionPower.cs:16`) — `AfterSideTurnStart` -> `StanceCmd.GainMantra(new ThrowingPlayerChoiceContext(), Owner.Player, Amount, null)` (:27-33)
- `LikeWaterPower` (`...\Powers\LikeWaterPower.cs:14`) — `AfterSideTurnEnd` 里 `StanceCmd.IsIn<CalmPower>(owner)` 后加格挡 (:25-35)
- `BlasphemyPower` (`...\Powers\BlasphemyPower.cs:16`) — `AfterSideTurnStart` -> `CreatureCmd.Kill(Owner)` (:27-34), 故意不 Remove (死亡被防住时下回合再触发, 注释 :11-14)

调用点 (`StanceCmd.` 使用者, 用 grep 得到): `DevotionPower.cs`, `LikeWaterPower.cs`, `MentalFortressPower.cs`, `SimmeringFuryPower.cs`.

#### D.4 历史实现 (WatcherMod, 仅供对照, 不是当前维护代码)

`G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\` 是早期独立 Watcher mod 的反编译/源码快照 (含 `Calm.cs` / `Wrath.cs` / `Divinity.cs` / `WatcherCombatHelper.cs` / `WatcherStatePower.cs` 等, 约 300 个文件, 最后写入 2026-09-02). 它的姿态模型与 Spire1 版**不同**: 每个姿态是独立 `PowerModel`, 切换逻辑在 `WatcherCombatHelper.ChangeStance<T>` (`WatcherCombatHelper.cs:537-550`), 用 `RemoveAllStances` (:573-579) + `GetCurrentStance` (:552-571) + `OnStanceChanged` (:594-627) 手写 dispatch, 而不是接口订阅. 两种模式都可参考; 新 mod 建议对齐 Spire1 的 `StanceCmd` 模式 (更近, 且在维护中).

---

### E. 需求 1 结论: 一个姿态能否挂多组 hook

- **能, 无数量限制.** 证据: `PowerModel` 是普通类, 所有 hook 都是可覆写的虚方法 (见 B 段清单); 一个实例覆写多少虚方法都行. 参照 `VoidFormPower` 同时覆写 `AfterApplied` / `AfterRemoved` / `BeforePowerAmountChanged` / `BeforeApplied` / `TryModifyEnergyCostInCombatLate` / `TryModifyStarCost` / `AfterCardPlayed` / `BeforeSideTurnStart` 共 8 个 (`research/engine-dllsrc/MegaCrit.Sts2.Core.Models.Powers\VoidFormPower.cs`).
- **组合多种持续效果的正确做法**: 姿态 Power 只做"标记 + 转发", 具体形态效果做成独立 Power (如 `DemonFormPower`, `ReaperFormPower`), 在姿态 `AfterApplied` 里 `PowerCmd.Apply<X>` 它们, 在 `AfterRemoved` 里 `PowerCmd.Remove` 它们. 这正是需求 2 需要的"可追踪 + 可回滚"结构.
- **注意**: 形态 power 若设 `PowerInstanceType.Instanced` 可避免与玩家已拥有的同名原版 power 合并 (引擎默认 `None` 会走 `FindExistingInstanceForStacking` 直接叠加, `PowerCmd.cs:169-178`).

### F. 需求 2 结论: 退出姿态时的回滚机制

可用的两个钩子:
- `PowerModel.AfterRemoved(Creature oldOwner)` — `PowerModel.cs:628`. `PowerCmd.Remove(PowerModel)` 会先 `RemoveInternal()` 再把 power 从 creature 的 `_powers` 移除, 然后 `await power.AfterRemoved(power.Owner)` (`PowerCmd.cs:291-299`). **注意**: `RemoveInternal()` (`PowerModel.cs:575-580`) 内部调 `Owner.RemovePowerInternal(this)` (`Creature.cs:648-656`), 但 **不** 清空 `_owner` 字段, 所以 `AfterRemoved` 里 `Owner` 仍然有效 (形参 `oldOwner` 也给了). `_owner` 只在 `AfterCloned` 里被清 (`PowerModel.cs:597`).
- `PowerModel.AfterSideTurnEnd` / `BeforeSideTurnEnd` — 若姿态因回合结束而退, 也可以在这些钩子里做回滚.

力量回滚的现成范例 (引擎内, 与需求完全同构):
- `TemporaryStrengthPower` — `research/engine-dllsrc/MegaCrit.Sts2.Core.Models.Powers\TemporaryStrengthPower.cs`
  - `BeforeApplied`: `PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), target, Sign * amount, applier, cardSource, silent: true)` (:112-115 区域)
  - `AfterSideTurnEnd`: `PowerCmd.Remove(this)` 然后 `PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, -Sign * base.Amount, base.Owner, null)` — 即"移除自己时反向抵消等量力量"
  - 具体子类: `FlexPotionPower` (`...\Powers\FlexPotionPower.cs`), 另有 `CoordinatePower` / `CrushUnderPower` / `DarkShacklesPower` / `DyingStarPower` / `EnfeeblingTouchPower` / `FeedingFrenzyPower` / `ManglePower` / `MonarchsGazeStrengthDownPower` / `PiercingWailPower` / `ReptileTrinketPower` / `SetupStrikePower` / `ShacklingPotionPower` 共 13 个 (grep `: TemporaryStrengthPower`).
- `StrengthPower` 本身: `PowerType.Buff`, `PowerStackType.Counter`, `public override bool AllowNegative => true;` (必需, 否则扣力量会被夹到 0), `ModifyDamageAdditive` 返回 `base.Amount` 仅当 `base.Owner == dealer && props.IsPoweredAttack()`.

**可追踪回滚的最小实现建议** (基于以上事实的推断, 需实现后验证): 在姿态 power 内部用 `InitInternalData` 存一个 `int appliedStrength`; `AfterApplied` 里记下本次施加量并 `PowerCmd.Apply<StrengthPower>`; `AfterRemoved` 里 `PowerCmd.Apply<StrengthPower>(..., -appliedStrength, ...)`. 与 `TemporaryStrengthPower` 同构, 风险点是需要区分"本姿态施加的"与"玩家自带的"力量, 直接扣减会污染玩家原有力量; 更安全的做法是给形态效果单独建一个 `TemporaryStrengthPower` 子类 (它自身记录 Amount 并做反向抵消), 姿态退出时 `PowerCmd.Remove` 该子类即可.

### G. 需求 3 结论: 出牌伤害数值 + 施加灾厄类减益

**灾厄在 StS2 里对应 `DoomPower`** (与用户描述 "Malaise/Corruption 类减益" 接近的是"层数型减益"; StS1 的 Malaise 在 StS2 里是 `Malaise.cs`, 施加 `StrengthPower` 负值 + `WeakPower`, 见下). 两个候选:

1. `DoomPower` — `research/engine-dllsrc/MegaCrit.Sts2.Core.Models.Powers\DoomPower.cs`
   - `PowerType.Debuff`, `PowerStackType.Counter`
   - 机制: 拥有者 `CurrentHp <= Amount` 时, 在其回合结束被 `DoomKill` 处死 (`IsOwnerDoomed()`); 触发点 `BeforeSideTurnEnd` (:60-67) 与 `AfterSideTurnEnd` (:69-76)
   - `public static async Task DoomKill(IReadOnlyList<Creature> creatures)`; `public static IReadOnlyList<Creature> GetDoomedCreatures(IReadOnlyList<Creature> creatures)`
   - **这正是 `ReaperFormPower` 用的减益** (见下).
2. `Malaise` (卡, 不是 power) — `research/engine-dllsrc\MegaCrit.Sts2.Core.Models.Cards\Malaise.cs`
   - `TargetType.AnyEnemy`, `HasEnergyCostX => true`, 带 `CardKeyword.Exhaust`
   - `OnPlay`: `PowerCmd.Apply<StrengthPower>(ctx, cardPlay.Target, -powerAmount, Owner.Creature, this)` + `PowerCmd.Apply<WeakPower>(ctx, cardPlay.Target, powerAmount, Owner.Creature, this)`

**"出牌造成伤害的数值获取"** — 引擎已经给出标准实现, 直接抄 `ReaperFormPower`:
- `research/engine-dllsrc/MegaCrit.Sts2.Core.Models.Powers\ReaperFormPower.cs`
  ```csharp
  public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
                                               ValueProp props, Creature target, CardModel? cardSource)
  {
      if (dealer != null && (dealer == base.Owner || dealer.PetOwner?.Creature == base.Owner)
          && props.IsPoweredAttack() && result.TotalDamage > 0)
      {
          Vfx?.OnEffectTriggered();
          await PowerCmd.Apply<DoomPower>(choiceContext, target, result.TotalDamage * base.Amount, base.Owner, null);
      }
  }
  ```
- `DamageResult` 字段 (`research/engine-dllsrc\MegaCrit.Sts2.Core.Entities.Creatures\DamageResult.cs`): `BlockedDamage`, `UnblockedDamage`, `OverkillDamage`, `TotalDamage = BlockedDamage + UnblockedDamage`, `WasBlockBroken`, `WasFullyBlocked`, `WasTargetKilled`, `Receiver`, `Props`.
- `Hook.AfterDamageGiven` 签名: `Hook.cs:401` — `(PlayerChoiceContext choiceContext, ICombatState combatState, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)`
- `Hook.BeforeDamageReceived`: `Hook.cs:415`; `Hook.AfterDamageReceived`: `Hook.cs:429`
- 施加减益: `PowerCmd.Apply<DoomPower>(choiceContext, target, amount, applier, cardSource)` (见 C 段)

**注意 (重要, 与需求 3 的表述有偏差)**: `ReaperFormPower` 是"**每次造成有源攻击伤害就施加等于该次伤害的 Doom**", 不是"每次出牌" — 判定用 `props.IsPoweredAttack()`, 所以: (a) 一张牌多次命中会触发多次; (b) 非攻击牌 (Skill/Power) 即使造成伤害也不触发; (c) 无源伤害 (`ValueProp.Unpowered`) 不触发. 若新 mod 要严格"每次出牌一次", 需要额外用 `cardPlay` 判重或用 `BeforeCardPlayed`/`AfterCardPlayed` 记账.

---

### H. 出牌 hook 与每回合开始 hook (StS2 实测反编译)

`MegaCrit.Sts2.Core.Models.AbstractModel` (`research/engine-dllsrc/MegaCrit.Sts2.Core.Models\AbstractModel.cs`, 2271 行) 里与出牌/回合相关的虚方法 (行号):

| 方法 | 行号 | 说明 (摘录自 XML 注释) |
|---|---|---|
| `BeforeAttack(AttackCommand command)` | :228 | 多次攻击: 所有命中前运行一次 (对应 StS1 `onAttack`) |
| `AfterAttack(PlayerChoiceContext, AttackCommand)` | :240 | 所有命中后运行一次 |
| `BeforeCardPlayed(CardPlay cardPlay)` | :468 | 出牌前 |
| `AfterCardPlayed(PlayerChoiceContext, CardPlay)` | :477 | 出牌后 |
| `AfterCardPlayedLate(PlayerChoiceContext, CardPlay)` | :488 | 注释: "CAREFUL! You should usually use AfterCardPlayed instead of this." |
| `AfterDamageGiven(PlayerChoiceContext, Creature? dealer, DamageResult, ValueProp, Creature target, CardModel? cardSource)` | :593 | 注释: 即使实际伤害为 0 (被格挡等) 也会调用 |
| `BeforeDamageReceived(PlayerChoiceContext, Creature target, decimal amount, ValueProp, Creature? dealer, CardModel? cardSource)` | :608 | 受伤前 |
| `AfterDamageReceived(...)` | :623 | 受伤后 |
| `AfterDamageReceivedLate(...)` | :640 | 注释: "CAREFUL! You should usually use AfterDamageReceived instead of this." |
| `AfterEnergyReset(Player player)` | :689 | 能量重置后 (Deva Form 类效果挂点) |
| `AfterEnergyResetLate(Player)` | :701 | |
| `AfterModifyingCardPlayCount(CardModel card)` | :851 | 出牌次数被改写后 |
| `AfterModifyingDamageAmount(CardModel? cardSource)` | :886 | 伤害数值被改写后 |
| `BeforePowerAmountChanged(PowerModel, decimal, Creature, Creature?, CardModel?)` | :1057 | power 层数变更前 |
| `AfterPowerAmountChanged(PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?)` | :1077 | power 层数变更后 (Mantra 计数挂点) |
| `BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` | :1247 | |
| `AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` | :1268 | 注释: "start of combat" 效果应在第 1 回合用此钩子 + `RoundNumber` 检查 |
| `AfterSideTurnStartLate(...)` | :1290 | |
| `AfterPlayerTurnStartEarly(PlayerChoiceContext, Player)` | :1306 | |
| `AfterPlayerTurnStart(PlayerChoiceContext, Player)` | :1320 | 需要玩家选择的 start-of-turn 效果用此钩子 |
| `AfterPlayerTurnStartLate(...)` | :1336 | |
| `BeforeSideTurnEndVeryEarly(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` | :1354 | |
| `BeforeSideTurnEndEarly(...)` | :1372 | |
| `BeforeSideTurnEnd(...)` | :1388 | 注释: 敌人伤害类效果 (Bedlam Beacon / The Bomb) **不** 应放这里 |
| `AfterSideTurnEnd(...)` | :1406 | 同上注释 |
| `AfterSideTurnEndLate(...)` | :1425 | |
| `ModifyCardPlayCount(CardModel card, Creature? target, int playCount)` | :1495 | |
| `ModifyDamageAdditive(Creature? target, decimal amount, ValueProp, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)` | :1579 | 加算伤害 (Strength/Vigor 走这里) |
| `ModifyDamageMultiplicative(...)` | :1613 | 乘算伤害 (Vulnerable/Weak/Wrath/Divinity 走这里) |
| `ModifyDamageCap(Creature? target, ValueProp, Creature? dealer, CardModel? cardSource, CardPlay?)` | :1595 | 单次伤害上限 |
| `ModifyEnergyGain(Player, decimal)` | :1624 | 能量获取修正 |
| `ModifyMaxEnergy(Player, decimal)` | :1771 | 最大能量修正 |
| `ModifyHpLostBeforeOsty(Creature, decimal, ValueProp, Creature?, CardModel?)` | :1702 | 扣血修正, Osty 重定向之前 |
| `ModifyHpLostBeforeOstyLate(...)` | :1722 | |
| `ModifyHpLostAfterOsty(...)` | :1741 | Osty 重定向之后 |
| `ModifyHpLostAfterOstyLate(...)` | :1761 | |
| `ModifyUnblockedDamageTarget(Creature, decimal, ValueProp, Creature?)` | :1915 | 改受伤目标 |
| `ModifyPowerAmountGivenAdditive(PowerModel, Creature giver, decimal, Creature?, CardModel?)` | :1845 | |
| `ModifyPowerAmountGivenMultiplicative(...)` | :1861 | |
| `TryModifyEnergyCostInCombat(CardModel, decimal originalCost, out decimal modifiedCost)` | :2034 | 战斗内费用改写 (第一遍) |
| `TryModifyEnergyCostInCombatLate(CardModel, decimal, out decimal)` | :2049 | 战斗内费用改写 (第二遍) |
| `TryModifyKeywordsInCombat(CardModel, ISet<CardKeyword>)` | :2066 | 战斗内关键词改写 |
| `TryModifyStarCost(CardModel, decimal, out decimal)` | :2078 | 星费用改写 |
| `TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal, Creature?, out decimal)` | :2093 | 施加 power 层数改写 |

Hook 分发器: `MegaCrit.Sts2.Core.Hooks.Hook` (`...\Hooks\Hook.cs`, 2417 行). 关键静态方法 (行号):
- `BeforeCardPlayed(ICombatState, CardPlay)` :263; `AfterCardPlayed(ICombatState, PlayerChoiceContext, CardPlay)` :278
- `AfterDamageGiven(...)` :401; `BeforeDamageReceived(...)` :415; `AfterDamageReceived(...)` :429
- `AfterModifyingBlockAmount` :661; `AfterModifyingCardPlayCount` :676; `AfterModifyingDamageAmount` :706
- `AfterModifyingPowerAmountGiven` :811; `AfterModifyingPowerAmountReceived` :826
- `AfterPlayerTurnStart(ICombatState, PlayerChoiceContext, Player)` :894
- `BeforePowerAmountChanged` :1020; `AfterPowerAmountChanged` :1032
- `BeforeSideTurnStart` :1156; `AfterSideTurnStart` :1175
- `BeforeSideTurnEnd` :1244; `AfterSideTurnEnd` :1279
- `ModifyEnergyCostInCombat(ICombatState, CardModel, decimal)` :1583 (内部跑两遍: 先 `TryModifyEnergyCostInCombat` 再 `TryModifyEnergyCostInCombatLate`, :1592/:1596)
- `ModifyDamage(IRunState, ICombatState?, Creature? target, Creature? dealer, decimal damage, ValueProp, CardModel?, CardPlay?, ModifyDamageHookType, CardPreviewMode, out IEnumerable<AbstractModel> modifiers)` :1495 — 内部加算循环在 :2528, 乘算循环在 :2538

监听者收集: `CombatState.IterateHookListeners()` — `...\Combat\CombatState.cs:411-436+` — 顺序为: 每个 creature 的 `Powers` -> (怪物) `Monster` / (玩家) 其 `Relics`, 即**先 powers 后 relics**, 按 creature 在 `_allies` / `_enemies` 里的顺序.
`Hook.IterateCombatHookListeners` (`Hook.cs:49-60`) 会在 `CombatManager.Instance.IsOverOrEnding && !IsStarting` 时返回空 — 但 `IsStarting` 时例外 (战斗 setup 的 hook 仍需运行).

### I. 免费打出牌 (free-to-play) 机制 (StS2 实测反编译)

**没有 "freeToPlayOnce" 字段** (那是 StS1 的, 见 `research/sts1-kb/.tmp-javap/cls/.../cards/AbstractCard.class` 的 javap: `public boolean freeToPlayOnce;` / `public boolean freeToPlay();`). StS2 用 **费用改写 hook**:

- 接口点: `AbstractModel.TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)` (:2034) / `TryModifyEnergyCostInCombatLate(...)` (:2049), 返回 `true` 表示已修改.
- 分发: `Hook.ModifyEnergyCostInCombat(ICombatState, CardModel, decimal)` (`Hook.cs:1583-1600`), 两遍.
- 显示路径: `CardCostHelper.TryModifyEnergyCostWithHooks` (`research/engine-dllsrc\MegaCrit.Sts2.Core.Helpers.Models\CardCostHelper.cs`) — 同样先跑 `TryModifyEnergyCostInCombat` 再跑 `...Late`, 用于费用颜色判定.
- 现成实现 (可抄):
  - `FreeAttackPower` (`...\Powers\FreeAttackPower.cs`) — `card.Owner.Creature == Owner && card.Type == CardType.Attack && card.Pile?.Type in { Hand, Play }` 时 `modifiedCost = 0`, 返回 true; `BeforeCardPlayed` 里满足条件就 `await PowerCmd.Decrement(this)` (消耗一层)
  - `FreePowerPower` (`...\Powers\FreePowerPower.cs`) — 同上, 条件 `CardType.Power`
  - `FreeSkillPower` (`...\Powers\FreeSkillPower.cs`) — 同上, 条件 `CardType.Skill`
  - `VoidFormPower` (`...\Powers\VoidFormPower.cs`) — 更复杂: 用 `InitInternalData<Data>().cardsPlayedThisTurn` 计数, `AfterCardPlayed` 里 `cardPlay.Card.Owner.Creature == Owner && !cardPlay.IsAutoPlay && cardPlay.IsLastInSeries` 时 +1; `BeforeSideTurnStart` 清零; `TryModifyEnergyCostInCombatLate` 与 `TryModifyStarCost` 都在 `cardsPlayedThisTurn < Amount` 时把费用改成 0; 还有个 `HideTemporaryZeroCostVisual()` hack 处理"本回合第一张牌打出瞬间的 0 费闪烁" (:120-127 注释 + 实现)
  - `CorruptionPower` / `BorrowedTimePower` / `CuriousPower` / `TangledPower` / `VeilpiercerPower` / `BrilliantScarf` (遗物) / `SpikedGauntlets` (遗物) 也用同一 hook (grep `TryModifyEnergyCostInCombatLate|TryModifyEnergyCost`)
- `CardPlay` 字段参考: `...\Entities\Cards\CardPlay.cs` — 有 `Card`, `Target`, `IsAutoPlay`, `IsFirstInSeries`, `IsLastInSeries` (从 `VoidFormPower` / `EchoFormPower` 用法反推)