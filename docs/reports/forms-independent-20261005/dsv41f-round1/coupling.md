# Forms 耦合只读审查

Request: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round1\coupling.request.md
Model: global:deepseek-v4.1-flash
Route: wb2api
Scope: 只读审查. 未改产品代码, 未构建, 未部署, 未操作游戏, 未改共享配置, 未写 C:, 未委派.

## 已确认

### F1 [P0] StanceCmd 与 Forms 是同一程序集内的双向编译期依赖, 不能只搬目录

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs` L10 `using Spire1.Spire1Code.Forms;`; L22-L26 `FormStanceMode.IsEnabled` / `FormStanceMode.KindOf` / `FormStanceWatcherBridge.CurrentKind`; L46-L55 `FormStanceWatcherBridge.Enter` 与 `WatcherFormStancePower`; L102-L106 `FormStanceWatcherBridge.Exit`; L177-L219 `Dispatch` 被 Forms 反向调用.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs` L5 `using Spire1.Spire1Code.Extensions;`; L17/L20/L23/L26 直接调用 `StanceCmd.Enter<CalmPower|WrathPower|DivinityPower>` 与 `StanceCmd.Exit`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` L19-L20 反向 `using Spire1.Spire1Code.Extensions` / `.Powers`; L372 `StanceCmd.Dispatch`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs` L11-L12 反向 `using Spire1.Spire1Code.Extensions` / `.Powers`; L161 `StanceCmd.Exit`.
- 触发条件: 任何"只把 `Spire1Code\Forms\**` 移出"或"只把 `StanceCmd.cs` 留在 Spire1"的拆分, 两侧都会立即 CS0246/CS0234.
- 权威契约: `StanceCmd` 是普通姿态与 Forms 共用的唯一入口; Forms 通过 `FormStanceMode.IsEnabled` 选择分支, 普通路径继续使用 `PowerCmd` 与 `Powers` 命名空间的 `CalmPower/WrathPower/DivinityPower`.
- 当前控制流: 同一程序集内 Extensions/Powers 与 Forms 形成环: `StanceCmd` -> `Forms` -> `StanceCmd.Dispatch` / `StanceCmd.Exit` -> `Forms`.
- 可复现命令: `rg -n "Spire1\.Spire1Code\.Forms|StanceCmd\." 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' --glob '*.cs'`
- 最小修复范围: 拆分前先定义跨程序集契约 (公共接口或反射契约), 覆盖 IsIn/Enter/Exit/Dispatch 四个入口; 不能只搬目录.
- 尚缺实机证据: 跨程序集后的 Harmony 安装顺序、类型解析与运行期分派未验证.

### F2 [P0] 普通局 DemonFormPower 实际归属引擎类型, 不是 Forms 的 DemonFormPower; 门控注释与事实不符

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\DemonForm.cs` L6 `using MegaCrit.Sts2.Core.Models.Powers;`; L14 `new PowerVar<DemonFormPower>(2)`; L22 `CommonActions.ApplySelf<DemonFormPower>`; L25 `DynamicVars.Power<DemonFormPower>()`.
  - 该文件没有 `using Spire1.Spire1Code.Forms;`, 其命名空间为 `Spire1.Spire1Code.Cards`; 因此 `DemonFormPower` 解析到 L6 导入的引擎类型 `MegaCrit.Sts2.Core.Models.Powers.DemonFormPower`, 而不是 `Spire1.Spire1Code.Forms.DemonFormPower`.
  - `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Models.Powers\DemonFormPower.cs` L15 `public sealed class DemonFormPower : PowerModel`; L58-L65 每方回合开始 `PowerCmd.Apply<StrengthPower>(..., base.Amount, ...)`, 即每回合固定 Amount 力量.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs` L26 才是递增三角力量版本; 它只被 `Forms\WatcherFormStancePower.cs` L110 在形态局创建.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` L52-L53 注释声称普通局经 `CommonActions.ApplySelf<DemonFormPower>` 授予 Forms 命名空间 power, 与上述解析结果冲突.
- 触发条件: 普通局打出 `Spire1.Spire1Code.Cards.DemonForm` 时, 实际授予引擎 `DemonFormPower`; Forms 的递增版 `DemonFormPower` 不会进入普通局.
- 权威契约: 引擎 `DemonFormPower` 是 `MegaCrit.*` 命名空间; 本 mod 的 Forms 版是 `Spire1.*` 命名空间. powers gate 的 `IsGatedPower` 只按 `Spire1.` 前缀识别, 但 `ShouldBlockApply` L723 仍可用 `IsSpire1CardSource(cardSource)` 在 powers 关闭时拦截引擎 power 的这次授予.
- 当前控制流: `Cards\DemonForm.OnPlay` L16-L23 先检查 `Spire1Config.Powers`; 打开时调用 `CommonActions.ApplySelf<引擎 DemonFormPower>`; 关闭时 L18-L21 直接返回. 门控层仍会因 cardSource 是 Spire1 卡而拦截.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\DemonForm.cs' -Pattern 'using|DemonFormPower'`; `rg -n "class DemonFormPower" 'G:\omp works\Sts\sts2-spire1'`
- 最小修复范围: 明确普通局 DemonForm 的归属: 若要使用 Forms 的递增版, 必须显式 `using Spire1.Spire1Code.Forms;` 或全限定类型; 若继续使用引擎版, 必须修正门控注释与任何"Forms 版覆盖普通局"的假设.
- 尚缺实机证据: 未在普通局实测引擎 DemonFormPower 的力量数值与 Forms 版差异.

### F3 [P0] powers gate 的 Forms 例外是命名空间字符串判定, 且对整局 Forms power 放行

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` L111 `private const string FormsNamespace = "Spire1.Spire1Code.Forms"`; L699-L706 `IsFormsPower` 按 Namespace 前缀判定; L716-L721 Apply 例外 `IsFormsPower(power) && FormStanceMode.IsSelected(target?.Player)`; L787-L792 Modify 例外 `IsFormsPower(power) && FormStanceMode.IsSelected(power.Owner?.Player)`.
  - 同文件 L45-L53 注释明确: 形态子系统自持生命周期, Apply 被拦截时 `WatcherFormStancePower.ApplyEffect` 会抛 `A power hook rejected a required form effect`.
- 触发条件: 该局带有 `FormStanceModifier` (即 `FormStanceMode.IsSelected` 为真) 时, 任何 Namespace 以 `Spire1.Spire1Code.Forms` 开头的 power 都不受 powers 组与硬熔断门控.
- 权威契约: 例外是为了避免 Forms 生命周期硬故障; 代价是命名空间一旦改名或拆到新程序集/新命名空间, 例外静默失效, Forms 的必需挂载会再次被拦截.
- 当前控制流: `ShouldBlockApply` 先判例外再判 `IsGatedPower`/`IsSpire1CardSource`; `ShouldBlockModify` 在 offset 分支之后判例外. 硬熔断 `HardFailClosed` 也排在例外之后.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs' -Pattern 'FormsNamespace|IsFormsPower|FormStanceMode.IsSelected'`
- 最小修复范围: 若 Forms 独立, 把例外从命名空间字符串改为显式契约标识 (公共接口/标记类型), 或在独立程序集中重新实现等价门控; 不要只改字符串常量.
- 尚缺实机证据: 未运行硬熔断加形态局的组合场景, 未证明例外在目标二进制中始终先于拦截生效.

### F4 [P0] FormNativeSmokeRunner 与 FormNativeSmokePatch 生产编译且无条件挂载, 仅靠命令行开关

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs` L24 `using Spire1.Spire1Code.Forms;`; L28 `internal static class FormNativeSmokeRunner` (3615 行); L30 `SPIRE1_FORM_SMOKE_REPORT`; L48-L61 `TryStart` 解析命令行, 只有 `--form-native-smoke*` 才进入异步.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs` L7-L13 `[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]` postfix 无条件调用 `FormNativeSmokeRunner.TryStart(__instance)`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` L54-L63 只排除资产目录; L93-L96 只 `Compile Remove` 两个 AFTP held-back 文件; 没有对 `Run\FormNativeSmokeRunner.cs` 或 `Patches\FormNativeSmokePatch.cs` 的条件排除或 `#if`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs` L247-L275 Phase3 属性扫描会安装所有非 gate-managed 的 `[HarmonyPatch]` 类型, 因此正常可用路径会安装 FormNativeSmokePatch.
- 触发条件: 生产 DLL 内含 runner 与 patch; 只要启动参数带 `--form-native-smoke*` 且环境变量指向报告路径, 就会在真实 `NGame._Ready` 后执行跑局、CleanUp、StartNewSingleplayerRun、必要时 RemoveArtifactPowerForSmoke 与 Quit.
- 权威契约: `FormNativeSmokeRunner` 自述为隔离 smoke 工具; 当前没有任何编译期或运行期产品开关把它排除在发布 DLL 外.
- 当前控制流: 普通启动时 `ParseRequest` L63-L130 无匹配参数即 L50-L54 返回; 请求命中时 `Interlocked.Exchange` L55 保证单次, 随后 `TaskHelper.RunSafely` L60.
- 可复现命令: `rg -n "FormNativeSmokeRunner|FormNativeSmokePatch" 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' --glob '*.cs'`
- 最小修复范围: 拆分 Forms 时把 runner 与 patch 一并移出产品程序集, 或加独立构建开关/程序集隔离; 不要只依赖命令行参数.
- 尚缺实机证据: 未启动目标二进制验证 patch 安装与命令行门控; 本轮未运行游戏.

### F5 [P0] 独立 mod 注册补丁边界: bridge 要求加载名为 "Watcher" 的程序集与精确 WatcherMod 私有 API

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` L30 `HarmonyId = "Spire1.FormStanceMode.Watcher"`; L58-L104 `TryBind` 安装与回滚; L106-L191 `ValidateBinding`.
  - 同文件 L108-L112 `AppDomain.CurrentDomain.GetAssemblies()` 要求恰好一个 `GetName().Name == "Watcher"`; L113-L115 要求 `WatcherMod.Calm/Wrath/Divinity`; L116-L136 要求 `WatcherMod.WatcherCombatHelper` 的 EnterCalm/EnterWrath/EnterDivinity/ExitStance/GetCurrentStance/OnStanceChanged/RunWithHookContext 精确签名, 并校验 `ChangeStance<T>` 泛型契约; L140-L167 安装 marker/IL/菜单补丁; L168-L177 要求 10 个 Forms 模型已在 `ModelDb`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs` L6-L13 `[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]` postfix 调用 `TryBind`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs` L7-L21 modifier 通过 L375-L390 `CustomModifiersPostfix` 注入 `NCustomRunModifiersList.GetAllModifiers`.
- 触发条件: Forms 若拆为独立 mod, 它必须自己拥有一个名为 "Watcher" 的程序集并提供上述精确私有 API, 或者改写 bridge 的绑定契约; 否则 `TryBind` 在 L110-L112 直接失败.
- 权威契约: 当前 bridge 是 Spire1 对第三方 Watcher 0.9.28 的反射适配层, 不是通用姿态接口; 它用独立 HarmonyId 安装/回滚, 并要求 `ModelDb.Init` 后所有 Forms 模型已注册.
- 当前控制流: MainFile Phase3 属性扫描安装 `FormStanceModePatch` -> `ModelDb.Init` postfix -> `TryBind` -> 校验 Watcher 程序集/类型/方法 -> 安装 marker/IL/菜单补丁 -> 发布 `_binding`.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'HarmonyId|GetName\(\).Name == "Watcher"|RequireMarker|RequireMethod|ModelDb.Contains'`
- 最小修复范围: 拆分前先定义独立 mod 的程序集名与公共契约; 若继续复用 Watcher 反射契约, 必须保证程序集名与私有 API 完全一致, 否则改为公共接口并同步改写 bridge.
- 尚缺实机证据: 未验证独立程序集下 Watcher 加载顺序、`ModelDb.Contains` 时点与菜单注入.

### F6 [P0] StanceCmd 的双路径契约在形态局下与原生 Watcher 姿态系统竞争同一 StancePower 语义

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs` L20-L36 `IsIn<TStance>`; L38-L41 `Current` 只取 `player.Creature.Powers.OfType<StancePower>().FirstOrDefault()`; L43-L98 `Enter`; L100-L122 `Exit`; L124-L175 `GainMantra`; L177-L219 `Dispatch`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` L252-L273 `Enter/Exit` 调用 Watcher `EnterCalm/EnterWrath/EnterDivinity/ExitStance`; L275-L281 `RemoveLegacyInternalStances` 删除 `CalmPower/WrathPower/DivinityPower`; L347-L373 `StanceChangedPostfix` -> `StanceCmd.Dispatch`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs` L152-L162 Divinity carrier 在下一回合调用 `StanceCmd.Exit`; L164-L177 `AfterRemoved` 在 native marker 仍存在时调用 `FormStanceWatcherBridge.Exit`.
- 触发条件: 形态局中 `FormStanceMode.IsEnabled` 为真, `StanceCmd.Enter/Exit/IsIn` 走 Forms 分支; 普通局中走原生 `PowerCmd.Apply<StancePower>` 与 `PowerCmd.Remove`.
- 权威契约: `StanceCmd.Current` 只认 `StancePower` 实例; Forms 分支下 carrier 是 `WatcherFormStancePower : StancePower`, 但真正的姿态源是 Watcher marker; `IsIn` Forms 分支用 `FormStanceWatcherBridge.CurrentKind` 而不是 `Current`.
- 当前控制流: Forms 分支下 `StanceCmd.Current` 仍可能返回 Forms carrier, 而 `Enter/Exit` 不再使用它; 普通分支下 `Dispatch` 的 from/to 是原生 `CalmPower/WrathPower/DivinityPower`; Forms 分支的 from/to 由 `FormStanceMode.LogicalStance` 映射为 Forms 命名空间的 `VoidSerpent/DemonReaper/EchoCelestial` carrier 类型.
- 可复现命令: `rg -n "StanceCmd\.Current|FormStanceMode\.LogicalStance|RemoveLegacyInternalStances" 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' --glob '*.cs'`
- 最小修复范围: 明确 Forms 分支下 `StanceCmd.Current` 的语义 (返回 carrier 还是 null), 并让所有 `Current` 消费者显式区分两套姿态身份; 不要只改 `IsIn`.
- 尚缺实机证据: 未实测形态局中 `Current` 消费者 (若有) 与原生 marker/carrier 同时存在的顺序.

### F7 [P0] 普通姿态卡与 Watcher 内容在 Spire1 内只靠 Powers 组开关, 且与 Forms 共享同一个 powers gate

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Config\Spire1Config.cs` L314 `PowersEnabled => EnableSts1Content && EnableSts1Powers && ContentActiveThisRun`; L347-L356 `IsEnabled(Powers)`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\SimmeringFuryPower.cs` L31-L37 关闭时移除自身; L40 `StanceCmd.Enter<WrathPower>`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\LikeWaterPower.cs` L31-L38 关闭时直接 return; L35 `StanceCmd.IsIn<CalmPower>`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\DevotionPower.cs` L33-L41 关闭时移除自身; L41 `StanceCmd.GainMantra`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` L64-L71 `Enter` 关闭时移除旧 stance 并 return; L114-L121 `Exit` 关闭时移除但不再 Dispatch; L126-L138 `GainMantra` 关闭时移除 Mantra; L183-L191 `Dispatch` 关闭时直接 return.
- 触发条件: `EnableSts1Powers` 关闭或 per-run latch 关闭时, 普通姿态卡/力量与 Forms 共享的 stance 入口全部关闭.
- 权威契约: 普通姿态与 Forms 都使用同一个 `Spire1ContentGroup.Powers` 开关; 目前没有独立 Forms 开关.
- 当前控制流: 普通局关闭 powers -> `StanceCmd.Enter` L64-L71 移除旧 stance 并 return; Forms 形态局关闭 powers -> Forms 分支在 L46-L53 先返回, 不受 L64 的关闭路径影响; Forms power 例外在门控层放行.
- 可复现命令: `rg -n "Spire1ContentGroup.Powers|StanceCmd\." 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers' --glob '*.cs'`
- 最小修复范围: 若 Forms 独立, 需要把普通姿态/力量与 Forms 的开关边界拆开, 或在独立 mod 中重新定义自己的 gate; 不要继续复用 Spire1 的 Powers 开关语义.
- 尚缺实机证据: 未验证关闭 powers 时形态局与普通局的真实收尾差异.

### F8 [P1] FormStanceWatcherBridge 用自身 HarmonyId 安装/回滚, 但与 Spire1 的 Phase3 扫描/门控状态没有统一清单

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` L30 `HarmonyId = "Spire1.FormStanceMode.Watcher"`; L58-L104 `TryBind` 内 `new Harmony(HarmonyId)` 与失败时 `Unpatch(target, All, HarmonyId)`; L225-L233 `VerifyInstalled` 用 `Harmony.GetPatchInfo` 逐目标复核.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs` L6-L13 `[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]` 由 MainFile Phase3 属性扫描安装; `FormStanceWatcherBridge.TryBind` 只在这之后运行.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs` L247-L275 属性扫描跳过 `Spire1PowersGate.IsGateManagedPatchType` 的 gate 类; `FormStanceModePatch` 不在该跳过集合内.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` L349-L423 注册熔断面与 L220-L233 覆盖证明都不感知 `FormStanceWatcherBridge.HarmonyId` 的补丁状态.
- 触发条件: bridge 部分安装失败时, 它只回滚自己的 HarmonyId; Spire1 powers gate 的覆盖证明仍可能通过, 因为二者状态互不可见.
- 权威契约: bridge 自述 "only this Harmony owner's patches are removed if any target fails"; powers gate 只证明自己的 central/fallback/fuse 目标.
- 当前控制流: Phase3 安装 `FormStanceModePatch` -> `ModelDb.Init` -> `TryBind` -> 成功则 `_binding` 发布; 失败则 `_binding=null`, 但 Spire1 其他补丁保持安装.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'HarmonyId|Unpatch|VerifyInstalled'`
- 最小修复范围: 若 Forms 独立, 让新程序集自己的 gate 统一管理 bridge 补丁; 若留在 Spire1, 把 bridge 的安装状态纳入 Spire1 的覆盖/失败清单.
- 尚缺实机证据: 未构造 bridge 部分安装失败场景, 未验证回滚后的进程状态.

### F9 [P1] FormNativeSmokeRunner 反向依赖 Watcher mod 的字符/卡牌/私有类型名, 是跨 mod 测试耦合

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs` L321 `FormStanceWatcherBridge.TryBind`; L1227-L1231 要求 `ModelDb.AllCharacters` 中存在 `WatcherMod.Watcher`; L1243-L1253 要求 `WATCHER_VIGILANCE`/`WATCHER_ERUPTION_P`/`WATCHER_BLASPHEMY`; L1263-L1294 断言 Forms carrier/effect 类型.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` L108-L112 要求程序集名 `"Watcher"`; L113-L115 要求 `WatcherMod.Calm/Wrath/Divinity`; L116-L136 要求 `WatcherMod.WatcherCombatHelper` 的私有方法签名.
  - `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\Watcher.csproj` L3 `AssemblyName=Watcher`; `WatcherMod\Watcher.cs` L14 `public class Watcher : CharacterModel`.
- 触发条件: 任何 Forms smoke 运行都要求 Watcher mod 以精确程序集名/类型名/卡牌 entry 存在; Watcher 版本漂移会先让 `TryBind` 失败, 再让 runner 在 L321-L341 写 terminal failure.
- 权威契约: 当前 Forms 不是独立姿态系统, 而是对 Watcher mod 的适配层; smoke 也按这一事实断言.
- 当前控制流: runner -> TryBind -> 校验 Watcher -> 查 Watcher 字符/卡牌 -> StartNewSingleplayerRun -> 断言 Forms carrier/effect.
- 可复现命令: `rg -n "WatcherMod|WATCHER_" 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'`
- 最小修复范围: 拆分 Forms 时把 runner 与 bridge 的 Watcher 契约一起移出, 并定义 Watcher 版本/程序集名验收; 不要把 runner 留在 Spire1 发布 DLL.
- 尚缺实机证据: 未运行目标 Watcher 版本, 未验证这些精确类型名在当前二进制中仍存在.

### F10 [P1] Forms 的 Harmony 补丁直接改引擎全局方法, 且不检查形态局是否真的存在

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs` L739-L778 `[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]`; L783-L821 `OnPlayWrapper`; L829-L843 `PlayCardAction.ExecuteAction` 动态目标; L849-L859 `CancelAction` 动态目标.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs` L203-L217 `[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount))]`; L223-L232 `CombatHistory.PowerReceived`; L238-L296 `PowerModel.SetAmount` 含 IL transpiler.
  - 同文件 L44 `AsyncLocal<Scope?>`; L85-L91 `ConditionalWeakTable` 系列; L153-L168 `CaptureStoredAmount`; L261-L296 transpiler 要求 `_amount` 恰好一处写入.
- 触发条件: 这些补丁在普通局也会被安装, 前缀/后缀先判 `CurrentScope`/`CurrentSpend` 等 AsyncLocal 是否命中; 未命中时多数直接返回.
- 权威契约: VoidForm/DemonForm 事务补丁依赖引擎方法内部调用顺序与 IL 形状; 引擎升级会导致 `HarmonyTargetMethod` 解析失败或 transpiler 抛异常.
- 当前控制流: MainFile Phase3 属性扫描安装这些补丁; 形态局外它们保持被动, 但仍在全局方法上.
- 可复现命令: `rg -n "HarmonyPatch|HarmonyTargetMethod|HarmonyTranspiler" 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms' --glob '*.cs'`
- 最小修复范围: 拆分时把这批全局补丁移入独立 Forms 程序集, 或给它们独立安装门控; 不要把它们留在 Spire1 的普通局补丁面.
- 尚缺实机证据: 未验证当前引擎 IL 与 `ResolveNativePlayCardMethodForPatch` 的实际解析结果.

### F11 [P1] FormStanceMode.IsEnabled 对 "选中但 bridge 不可用" 直接抛异常, 是普通局与存档加载的硬边界

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs` L20-L30 `IsSelected` / `IsEnabled`; L32-L36 `RequireAvailable`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs` L14-L21 `AfterRunCreated`/`AfterRunLoaded`/`BeforeCombatStart` 都调用 `RequireAvailable`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs` L22-L27 `IsIn` 在 `IsEnabled` 为真后调用 `CurrentKind`; L46-L53 `Enter` 同理.
- 触发条件: 旧存档带 `FormStanceModifier` 但当前进程 Watcher bridge 不可用时, `IsEnabled` 抛 `InvalidOperationException("Spire1 Forms unavailable: ...")`.
- 权威契约: 注释明确 "A selected save must fail explicitly if its optional dependency can no longer be bound"; 这是有意 fail-loud, 不是降级.
- 当前控制流: `FormStanceModifier.AfterRunLoaded` -> `RequireAvailable` -> 抛异常; `StanceCmd` 的 Forms 分支也依赖 `IsEnabled` 先通过.
- 可复现命令: `rg -n "RequireAvailable|IsEnabled\(" 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs'`
- 最小修复范围: 独立 Forms mod 必须保留这一显式失败契约, 或在加载时给出可恢复的用户提示; 不要静默把形态存档降级为普通姿态.
- 尚缺实机证据: 未加载带 modifier 且 Watcher 缺失的存档, 未观察异常在引擎加载路径的最终表现.

## 进行中

- 已完成 F1-F11, 继续 F12-F16.

## 未知

- 独立 Forms mod 的最终程序集名、命名空间与是否继续依赖 Watcher 未定.
- 普通局 DemonForm 的预期归属 (引擎版还是 Forms 递增版) 未由请求方确认.
- 未运行任何目标二进制, 所有结论均为源码/配置/反编译文本证据.
