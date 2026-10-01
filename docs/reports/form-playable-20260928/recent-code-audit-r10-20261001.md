# recent-code-audit-r10-20261001

## 已确认
- 审查模式：独立只读；模型：`ovoapi:6.1sol`。
- 范围严格限定为请求文件指定的三个源码文件，并核对了 `AbstractModel.cs:520-522`、`SerpentFormPower.cs` 与 `TransactionPatchAdapter.cs` 的控制流证据。
- 未构建、未测试、未改代码、未部署、未启动游戏。

## 进行中
- 已完成三项静态控制流审查，以下记录问题与已排除面。

## 未知
- 未验证真实 AutoAnthony 运行时的实际 getter `DeclaringType`、Harmony 版本行为及第三方池 ModelDb 注册时序。
- 未验证真实游戏中多人、存档恢复、卡池反查和战斗调度器的实机行为。

## 审查结果

### 1. AutoAnthony getter 重绑定、AllCardIds postfix 与跨模式污染

**优先级：P1（已修复项仍需实机边界验证）**

- **证据/路径/行号：**
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:620-631`：`PatchGetter` 先取得 getter，再读取真实 `getter.DeclaringType`，随后用 `AccessTools.DeclaredPropertyGetter(declaringType, propertyName)` 重绑定；没有改用 `GetBaseDefinition()`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:637-643`：仅接受具体、无参数、实例 getter，并同时传递可选 `prefix` 与 `postfix`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:372-375`：`AllCardIds` 现在明确以 `postfix:` 命名参数传入 `ThirdPartyPoolIdsPostfix`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:454-469`：postfix 先用 `ReferenceEquals(__instance, ResolveThirdPartyPoolInstance())` 守卫，再检查 `IsRunActive()`，最后将原结果与 Colorless 类型 ID 合并并去重。
- **触发条件：** AutoAnthony 已加载且反射解析成功；Watcher mod 在场且 AutoAnthonyWatcher 扩展不在场；Watcher 池类型可解析；进入 AutoAnthony run 后访问继承来的 `AllCardIds` getter。
- **当前控制流：** `PatchThirdPartyEntries` 在 372-375 行分别为 `AllCards` 挂 prefix、为 `AllCardIds` 挂 postfix。修复前同一调用把 `HarmonyMethod` 作为 `PatchGetter` 的第四个位置参数传入，实际落入 `prefix`；当前 diff 已改为 `postfix:`，因此 AllCardIds 注册方向正确。继承 getter 的 Harmony 目标被重新绑定到实际声明类型；运行时又以实例身份收回作用域。
- **最小修复：** 当前工作树中的最小修复已经存在：`AllCardIds` 使用 `postfix:`；`PatchGetter` 用真实 `DeclaringType` 重绑定并支持 prefix/postfix。无需追加源码修改。
- **跨模式污染结论：** 在审查范围内**没有可证实的 `_allCardIds` 字段或缓存污染链**：全工作区检索未发现 `_allCardIds` 定义；当前实现只复制 getter 原结果到局部 `List<ModelId> merged`（464 行），并写回本次调用的 `__result`。非混沌模式在 460-463 行直接返回，其他池实例在 456-458 行直接返回。因此不能把跨模式污染列为已确认问题。
- **未验证实机边界：** 未确认真实 AutoAnthony `WatcherCardPool` 是否确实继承而未重声明属性，未确认 Harmony 对该运行时 getter 目标的接受情况，也未确认 `ResolveThirdPartyPoolInstance` 在真实 ModelDb 注册完成后的时序。上述是未知边界，不是静态已证实缺陷。

### 2. `ContractStubs.cs` 的 `CombatRoom` 与 `PowerModel.AfterCombatEnd(CombatRoom)`

**优先级：已排除（无问题）**

- **证据/路径/行号：**
  - 权威参考：`G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\AbstractModel.cs:520-522`，签名为 `public virtual Task AfterCombatEnd(CombatRoom room)`，默认返回 `Task.CompletedTask`。
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:33-37`：定义最小 `public sealed class CombatRoom { }`。
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:226-320`，尤其 319 行：定义同名同参虚方法并返回 `Task.CompletedTask`。
- **触发条件：** 仅在 probe 编译链接需要 `AfterCombatEnd` 的类型与签名时触发；该 stub 不模拟房间生命周期，也没有额外调度行为。
- **当前控制流：** `CombatRoom` 只作为参数类型存在；`AfterCombatEnd` 是最小虚协作者，默认完成任务。与参考源码的签名、可覆写性和默认返回值一致。
- **最小修复：** 无需修复。保持当前最小 stub；不要向其中加入房间状态或调度器模拟。
- **未验证实机边界：** probe 不覆盖真实 combat-end 发布顺序、非战斗模型订阅者、网络同步或原生历史订阅；文件头 1-3 行也明确排除这些范围。这不构成签名不一致。

### 3. `SerpentScenarios.cs` 新增场景、生产 hook 与共享 tracker 去重

**优先级：已排除（无问题）**

- **证据/路径/行号：**
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\SerpentScenarios.cs:176-191`：`serpent.removed_source_and_bridge_duplicates_hit_once_and_clean_up` 使用同一 `CardPlay` 对象，按 `source, bridge, source, bridge` 重复投递 `AfterCardPlayed`，断言一次伤害、一次退出能量、tracker 清空及 bridge 移除。
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\SerpentScenarios.cs:203-216`：`GrantExitEnergy` 公共属性场景通过反射验证公开 bool getter/setter，并验证关闭后不重复发放退出能量。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:98-147`：生产 `AfterCardPlayed` 先从共享 root tracker 移除 play，再选择目标、调用 `CreatureCmd.Damage`，并在 finally 中处理 completion bridge。
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionPatchAdapter.cs:82-108`：adapter 明确只反射调用真实生产 patch 私有方法；没有复制 ledger、reservation 或 AsyncLocal 算法。
- **触发条件：** 同一实际 `CardPlay` 被多个 live listener 或重复 callback 投递；生产 hook 仍由场景直接调用 `SerpentFormPower.BeforeCardPlayed/AfterCardPlayed`，命令边界由 probe 的生产适配层承接。
- **当前控制流：** 场景没有伪造生产 scheduler，也没有声称覆盖完整战斗。它直接驱动生产 power hook；`PowerCmd.Remove`、`CreatureCmd.Damage` 等进入 probe collaborator，涉及真实 production patch 的部分经 `ProductionPatchCalls` 反射调用。断言范围限于重复投递、共享 tracker 消费、一次性伤害/能量和清理。
- **最小修复：** 无需修复。若未来要宣称真实调度器或完整战斗覆盖，应另增实机/集成场景，而不是扩大这两个 probe 场景的结论。
- **未验证实机边界：** 未验证真实引擎 callback 调度、并发交错、多人同步、真实敌方 HP/block 变化或 Harmony 注入现场；现有场景没有夸大这些覆盖。

## 结论
- 发现 1 个已在工作树中体现的 P1 风险修复点：`AllCardIds` postfix 关键字注册，以及按真实 `DeclaringType` 重绑定 getter；静态代码显示当前方向正确。
- 没有证据支持 `_allCardIds` 跨模式污染链。
- `CombatRoom`/`AfterCombatEnd` 是与权威源码一致的最小协作者。
- Serpent 新增场景直接驱动生产 power hook，验证共享 tracker 的重复投递去重，不伪造 scheduler，也未夸大真实战斗覆盖。
- 本报告仅为静态只读审查；未作构建、测试或实机验证。
