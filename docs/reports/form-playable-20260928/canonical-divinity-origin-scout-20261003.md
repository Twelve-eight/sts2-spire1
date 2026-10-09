# canonical Divinity 异常来源只读审查

范围: `native-form-smoke-turns-current-20261003-r4` 的历史实机证据, 与相邻 `native-form-smoke-r4-current-20261003`, `native-form-smoke-r5-current-20261003` 对比. 本报告只写只读审查结论, 不构成产品修复. 固定分段: 已确认 / 进行中 / 未知.

## 已确认

1. 异常只已在 turns r4 的实机运行中出现; 单场景 r4/r5, turns r2/r3 均未出现 canonical 异常.
   - turns r4: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\run-final.json` 顶层 `status=failed`, `exitCode=1`.
   - turns r4 JSON: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\form-native-smoke-turns.json:5` 与 `:858`, `:1184`, `:1187`, `:1193`.
   - turns r4 godot log: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\appdata\SlayTheSpire2\logs\godot.log:495` 首次记录 `Combat #1 turn loop died ... CanonicalModelException: Canonical model of type Spire1.Spire1Code.Cards.Strike used in incorrect place.`
   - turns r4 实机异常栈: `godot.log:496-503` -> `HookPlayerChoiceContext.GetOwner` -> `HookPlayerChoiceContext..ctor` -> `Hook.AfterDeath` -> `CreatureCmd.Kill` -> `WatcherMod.EndTurnDeathPower.AfterPlayerTurnStart`.
   - 单场景 r4/r5: 各自 `form-native-smoke-final.json` 为 `status=completed`, `exitCode=0`; 对应 `form-native-smoke-divinity.json` 为 `status=passed`, 且没有 `CanonicalModelException`.
   - 触发条件: turns r4 divinity 边界进入 `state=player-dead, before=3, after=4, combatRound=3->4, playerDead=True`; 单场景同日 divinity 走的是 `status=passed` 的独立场景, 没有进入该 turn-boundary 路径.
   - 当前控制流 (实机证据): `WatcherMod.EndTurnDeathPower.AfterPlayerTurnStart` 杀死玩家 -> `Hook.AfterDeath` -> `new HookPlayerChoiceContext(Strike, ...)` -> `CardModel.Owner` getter -> `AssertMutable()` -> `CanonicalModelException`.

2. 订阅日志本身不是 turns 独有差异; 但 turns r4 与 r2/r3 的订阅确认状态不同.
   - 单场景 r4: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r4-current-20261003\appdata\SlayTheSpire2\logs\godot.log:293` 出现 `deck-grant guard subscribed`.
   - 单场景 r5: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r5-current-20261003\appdata\SlayTheSpire2\logs\godot.log:293` 出现 `deck-grant guard subscribed`.
   - turns r2: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r2\appdata\SlayTheSpire2\logs\godot.log:293` 出现 `deck-grant guard confirmed: canonical Spire1 card is present in ModHelper run-state hook subscribers.`
   - turns r3: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r3\appdata\SlayTheSpire2\logs\godot.log:293` 出现 `deck-grant guard confirmed`.
   - turns r4: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\appdata\SlayTheSpire2\logs\godot.log:293` 仅出现 `deck-grant guard subscribed`, 没有 `confirmed`.
   - 限制: 这条证据只说明 r4 的订阅确认分支与其他运行不同, 不能单凭日志文本证明 canonical `Strike` 必定来自该订阅.

3. 产物身份不是同一构建; 当前不能把异常归因于 turns runner 单独引入.
   - turns r4 staging: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\staging-current.json`, `mods\Spire1\Spire1.dll` `sha256=D2B1FA40C4DED8AD251AC193624E4E71DABD79DDF83E573C0F2EB70913B66675`, `length=845824`.
   - 单场景 r4/r5 staging: 同一路径 DLL `sha256=4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`, `length=788480`.
   - turns r2/r3 staging: DLL 分别为 `sha256=8891BF71486F0847A34D212F926DD5E791F75839E094C309EF33947CC9842B01` (`871424` 字节) 与 `sha256=684E8B597C980228B449AFCFDF3108B7F8970539165480735963F34D9F4F2FA0` (`873984` 字节).
   - turns r4 runner 源目录: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-turns-current-20261003-r4.ps1:7` -> `G:\omp works\.tmp\spire1-beta-r4-build-20261003-1046\payload\mods\Spire1`.
   - 单场景 r4/r5 runner 源目录: 对应 runner 第7行 -> `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\release-r4-current-20261003\payload\mods\Spire1`.
   - 结论: "由 turns runner 引入" 与 "由 Spire1 r4 DLL 构建引入" 尚未分离; 见"进行中"未闭合项.

4. 首次出现时间点 (只读检索范围: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad` 下全部 `*.log` 与 `*.json`).
   - 命中文件只有 3 个, 均为 turns r4: `form-native-smoke-turns.json` (2026-10-03 10:50:39), `appdata\SlayTheSpire2\logs\godot.log` (10:50:39), `stderr-final.log` (10:50:39).
   - turns r2/r3 均无 canonical `Spire1.Spire1Code.Cards.Strike` 异常.
   - 更早历史目录没有该异常的命中; 唯一更早相关的 `native-form-smoke-r17-retry-20261001` 与 `native-form-smoke-r17-ftue-20261001` 也没有该异常.

5. 源码控制流 (源码推理, 非实机证据; 路径与行号为绝对路径 `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs`).
   - `:68901-68906`: `AbstractModel.AssertMutable()` 在 `!IsMutable` 时抛 `CanonicalModelException`.
   - `:72761-72767`: `CardModel.Owner` getter 首先调用 `AssertMutable()`.
   - `:164421-164424`: `Hook.AfterDeath` 遍历 `runState.IterateHookListeners(combatState)` 并直接为每个 model 构造 `HookPlayerChoiceContext(model, ...)`.
   - `:172357-172368`: `HookPlayerChoiceContext` 构造函数调用 `GetOwner(source, combatState)`; `GetOwner` 对 `CardModel` 读取 `cardModel.Owner`.
   - `:41933-41975`: `RunState.IterateHookListeners` 在 `ModHelper.IterateAllRunStateSubscribers(this)` 的返回值后继续枚举 (`:41972-41975`).
   - `:155353-155409`: `ModHelper.SubscribeForRunStateHooks` / `IterateAllRunStateSubscribers` 的订阅与枚举实现.
   - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\Spire1Card.cs:367-395`: `ResolveRepresentatives()` 返回 `ModelDb.Card<Strike>()`, 即 canonical 实例 (源码推理).
   - `:164414-164428`: canonical 实例在 `AfterDeath` 中作为 `model` 传给 `HookPlayerChoiceContext`, 触发 `Owner` -> `AssertMutable` 的链条 (源码推理).
   - 抑制条件: 只有当 `Hook.AfterDeath` 实际遍历到该 canonical `Strike` 订阅者, 且 `LocalContext.NetId` 存在, 才会走到 `HookPlayerChoiceContext` 构造函数. turns r4 的异常栈满足"遍历到该 model"这一条件.

6. 触发条件 (当前证据能确定的范围): 需要同时满足 (a) `ending-turn death` 路径在下一己方回合开始处杀死玩家, 由 `WatcherMod.EndTurnDeathPower.AfterPlayerTurnStart` 执行; (b) `AfterDeath` 遍历的 listener 序列包含 canonical `Spire1.Spire1Code.Cards.Strike`.
   - (a) 的实机证据: `godot.log:503` 与 `form-native-smoke-turns.json:858` 的 `state=player-dead`.
   - (b) 只有 turns r4 的订阅日志退化为 `subscribed`; 其余运行没有同样的 canonical 异常, 但这不是唯一差别 (构建 hash 也不同).

7. 最小修复范围 (建议, 未实施; 不修改产品代码).
   - 方向 A: 改变 `Spire1DeckGrantGuard` 的订阅对象, 使其不返回 canonical `Strike`, 或不再把 canonical model 放进 run-state hook listener.
   - 方向 B: 避免让该 canonical model 参与 `AfterDeath`/`GetOwner` 读取 `Owner` 的路径.
   - 需先确认 r4 构建对应的 `Spire1Card.cs` 版本与订阅序列, 否则修复范围会过宽.

8. 可复现命令 (只读, 不构建, 不运行游戏):
   - `rg -n -S "Canonical model of type Spire1.Spire1Code.Cards.Strike" "G:\omp works\.tmp\form-playable-20260928-01a0e7ad" -g "*.log" -g "*.json"`
   - `Select-String -LiteralPath "G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\appdata\SlayTheSpire2\logs\godot.log" -Pattern "deck-grant guard|Canonical model|EndTurnDeath"`
   - `Get-Content -LiteralPath "G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r4\staging-current.json" -Raw`
   - `Get-FileHash -Algorithm SHA256 -LiteralPath "G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-turns-current-20261003-r4.ps1","G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r4-current-20261003.ps1","G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r5-current-20261003.ps1"`

## 进行中

- runner 字节身份尚未完成闭合: r4 turns runner `sha256=A9E282B6CAB6CB5701B3C49890287065677901A72CDBC13878DFFB5860477ECB`; 单场景 r4 `sha256=160A54A4377E43B333A69C08337C5126BFDA46197AB09013EE7049E8860231F2`; 单场景 r5 `sha256=6B5F3A5C9CFBB1ADB169104A09AD2192220AC71CA874EF6330FB61B62ACB3C4C`. 这些 runner 也不相同, 仍需按"runner 差异 vs DLL 差异"做交叉验证.
- turns r4 构建的 `Spire1Card.cs` 对应源码版本尚未明确; 当前工作树 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\Spire1Card.cs:164-395` 是带 `IsSubscriberConfirmed` 探针的版本, 而 turns r4 日志只出现 `subscribed`, 未出现 `confirmed`; 需要只读反编译该 DLL 才能确认两者是否一致.
- `Spire1DeckGrantGuard` 与 canonical `Strike` 的因果关系只由源码推理支持, 尚未用实机证据把异常模型直接定位为该订阅返回的实例.

## 未知

- turns r4 的同一 DLL + 同一 runner 重复运行是否稳定复现.
- 若把 turns r4 的 DLL 换成 r2/r3 的 DLL, 或把 turns runner 换成单场景 runner, 异常是否仍出现; 这需要受控对照, 不能由当前历史目录推断.
- `ModHelper.SubscribeForRunStateHooks` 的订阅顺序在不同 DLL/运行时是否变化, 以及该顺序是否决定 `AfterDeath` 是否先遍历到 canonical `Strike`.
- 单场景 r4/r5 是否会在相同 turn 序列下复现; 当前没有实机证据.
- 是否存在比 turns r4 更早、但未被只读检索范围覆盖的实机复现目录.