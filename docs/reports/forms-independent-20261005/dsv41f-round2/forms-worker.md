# Forms worker 报告 - dsv41f round2

- 范围: 仅 G:\omp works\Sts\sts2-forms 与报告文件
- 姿态: 只改源码, 不构建, 不测试, 不部署, 不改共享配置, 不写 C:
- 契约: DEVELOP-forms-independent-20261005.md / HANDOFF-forms-current-20261005.md / build-split-audit.md / coupling-audit.md
- 本轮模型: global:deepseek-v4.1-flash, 路由 wb2api (请求文件指定); 仅用当前 harness 原生设施, 未再委派

## 已确认

### 契约/现状 (源码证据)
- 已读取请求文件与四份契约/审查文档; 当前 Spire1 Forms 目录共 17 个 .cs 文件 (build-split-audit F-01 计数一致).
- Spire1 侧对 Forms 的编译期耦合点: `StanceCmd.cs` (using + 4 处调用), `Spire1PowersGatePatch.cs` (using + FormsNamespace 常量 + FormStanceMode 访问), `MainFile.cs` Phase3 程序集属性扫描, `FormNativeSmokeRunner/Patch` 仍在编译项内 (coupling-audit C-01..C-08). 这些属于 Spire1 worker 的写集, 本轮未改其文件.
- Forms 侧对 Spire1 的编译期耦合点: `WatcherFormStancePower : StancePower`, `FormStanceMode` 比较 CalmPower/WrathPower/DivinityPower, `FormStanceCmd` 调 StanceCmd, `FormStanceWatcherBridge` 调 StanceCmd.Dispatch 与 OfType<StancePower>.
- `CustomIDAttribute` 位于 `BaseLib.Utils.Attributes`; `BaseLib.Patches.Content.PrefixIdPatch` 在 `ModelDb.GetEntry` 后置改写 ID, 因此独立 namespace/程序集不影响旧档 ID.
- 引擎 API 已核对: `Log`/`LogType` 在 `MegaCrit.Sts2.Core.Logging`; `ModInitializerAttribute` 在 `MegaCrit.Sts2.Core.Modding`; `NCustomRunModifiersList.GetAllModifiers` 在 `...Nodes.Screens.MainMenu`; `ModelDb.Contains(Type)` / `ModelDb.Power<T>` / `ModelDb.Modifier<T>` 存在; `CombatManager.IsInProgress/IsEnding/IsOverOrEnding` 存在; `PlayerCombatState.TurnNumber` 存在.
- Watcher 0.9.28 契约与源实现一致 (WatcherMod.{Calm,Wrath,Divinity}, WatcherCombatHelper.{EnterCalm,EnterWrath,EnterDivinity,ExitStance,GetCurrentStance,OnStanceChanged,RunWithHookContext,EndTurnSafely,ChangeStance<T>}), 反射校验逐签名保留.

### 已落盘 (源码已改)
- 项目骨架 `G:\omp works\Sts\sts2-forms`:
  `NuGet.config` (缓存指 G:), `.omp/hooks/pre/backup.ts` (AGENTS Sec 7),
  `mod/Forms.csproj` (Godot.NET.Sdk 4.5.1 / net9.0 / sts2 / 0Harmony / BaseLib 3.4.5 / ModAnalyzers / PckPacker; 不引用 Watcher, 不引用 Spire1; `Compile Remove="Forms/**"`),
  `mod/Forms.json` (id=Forms, 依赖仅 BaseLib>=3.4.5, has_pck=true, 无 Watcher/Spire1 依赖),
  `mod/project.godot` (assembly_name=Forms, icon=res://Forms/mod_image.png),
  `mod/Sts2PathDiscovery.props` (测试副本优先, 无 Steam 回退), `mod/Directory.Build.props`, `mod/GlobalUsings.cs`, `mod/export_presets.cfg`, `.gitignore`/`.gitattributes`.
- FormsCode 迁移 (原 17 个 -> 独立 namespace `Forms.FormsCode`, 全部落盘):
  `FormsStancePower` (自有 carrier 基类, 取代 Spire1 StancePower), `WatcherFormStancePower`, `FormStanceMode`,
  `FormStanceModifier`, 3 个 carrier, 6 个效果 power, `DemonFormStrengthTransactionPatch`, `VoidFormPlayTransactionPatch`,
  `FormStanceCmd`, `FormStanceModePatch`, `FormStanceMainMenuRetryPatch`, `MainFile`, `Interop/FormsRuntimeEntryPoint`.
- 命名空间/引用清理: 全部 `namespace Forms.FormsCode`; 无 `using Spire1`; 无 Spire1 编译期类型引用. 仅存的 "Spire1" 出现是 (a) 文档注释, (b) 类型全名字符串 (用于反射兼容), (c) `Spire1.Spire1Code.Interop.FormsCompatibilityEntryPoint` 通知常量. 无 `res://Spire1` 残留.
- 10 个类型显式 `[CustomID("SPIRE1-*")]`, ID 与契约一致 (VOID_SERPENT_STANCE_POWER / DEMON_REAPER_STANCE_POWER / ECHO_CELESTIAL_STANCE_POWER / VOID_FORM_EFFECT_POWER / SERPENT_FORM_POWER / DEMON_FORM_POWER / REAPER_FORM_EFFECT_POWER / ECHO_FORM_EFFECT_POWER / CELESTIAL_FORM_POWER / FORM_STANCE_MODIFIER).
- `FormStanceWatcherBridge` 完整重写: 状态机 Unbound/Retryable/Installing/Bound/Terminal/ShuttingDown; `IsAvailable` 仅 Bound 为 true; ValidateBinding 逐签名校验 (不兼容 -> Terminal); patch 安装后逐条 `Harmony.GetPatchInfo` 验证; 失败先 Unpatch 本 owner, 部分回滚失败收敛 Terminal; `Shutdown()` 撤销本 owner patch 并清空 delegate/引用.
- AssemblyLoad 回调 `OnAssemblyLoad` **只** `Volatile.Write(ref _assemblyLoadPending, true)`; 真正绑定只发生在已知主线程入口: `FormStanceModePatch` (ModelDb.Init postfix) 与 `FormStanceMainMenuRetryPatch` (NMainMenu._Ready postfix -> `TryBindOnMainThreadEntry()`).
- 稳定反射协议 `Forms.FormsCode.Interop.FormsRuntimeEntryPoint`: `IsAvailable`, `IsSelected(Player)`, `KindOf(Type)` (0/1/2/3), `CurrentKind(Player)`, `Enter(PlayerChoiceContext, Player, Type, CardModel?)`, `Exit(PlayerChoiceContext, Player, CardModel?)` 全部落盘, 签名与契约一致.
- Forms 可选探测 `Spire1.Spire1Code.Interop.FormsCompatibilityEntryPoint.Dispatch(Player, PlayerChoiceContext, int, int)` (纯反射, `Spire1StanceNotification`); 缺失/签名不符只跳过通知.
- `MainFile` 自持 `[ModInitializer]` + `SimpleLoc.EnableSimpleLoc("Forms")` + 本程序集 `[HarmonyPatch]` 属性扫描 + ProcessExit -> `Shutdown()`; 不依赖 Spire1 MainFile.
- 资源/本地化: `Forms/images/powers/{calm,wrath,divinity}_power.png` + `big/` 6 图标 + `mod_image.png`;
  `Forms/localization/{eng,zhs}/powers.json` (9 键 x3) 与 `modifiers.json` (2 键, smartDescription 在源中本就不存在, 未伪造); key 沿用 `SPIRE1-*` 身份; 代码引用全部 `res://Forms/...`.
- 文档: `DEVELOP.md` (设计/契约 + 未验证边界), `DEVLOG.md` (本轮记录).
- `FormNativeSmokeRunner`/`FormNativeSmokePatch` 未复制进生产 Forms 项目 (契约 Sec 2.2/4); 未新建任何测试 stub; 后续测试 runner 应独立项目或测试编译常量.

## 进行中

- 无. 实现范围内项目已全部落盘; 未构建/未测试属请求明确约束, 不是未完成项.

## 未知 (未验证边界)

- 独立 Forms.dll 被 ModelDb 发现与实例化的实际时序 (契约 Sec 6).
- Watcher 晚加载后 AssemblyLoad -> 主线程绑定的真实路径 (静态实现存在, 未实机).
- 独立 carrier 与 Spire1 原 `IOnStanceChanged` 消费者的兼容完整性 (通知走反射, 未实机).
- 旧档/联机身份在独立程序集下的真实解析 (CustomID 静态正确, 未实机).
- 新 Forms PCK 视觉资源在实际 UI 中显示.
- 可能的编译风险 (未构建, 按要求不构建): 未发现已知的确定编译错误, 但未由编译器确认. 重点人工核对项已通过: 无 Spire1 类型引用, namespace 一致, 10 个 CustomID 齐全, res:// 路径一致, 桥接 hook 方法名与 `nameof` 引用全部存在.
## 修改文件清单 (本轮新建/写入)

新建项目根 `G:\omp works\Sts\sts2-forms`:

- `NuGet.config`
- `DEVELOP.md`, `DEVLOG.md`
- `.omp/hooks/pre/backup.ts`
- `mod/Forms.csproj`, `mod/Forms.json`, `mod/project.godot`, `mod/Sts2PathDiscovery.props`,
  `mod/Directory.Build.props`, `mod/GlobalUsings.cs`, `mod/export_presets.cfg`, `mod/.gitignore`, `mod/.gitattributes`
- `mod/Forms/mod_image.png`
- `mod/Forms/images/powers/{calm,wrath,divinity}_power.png`
- `mod/Forms/images/powers/big/{calm,wrath,divinity}_power.png`
- `mod/Forms/localization/eng/powers.json`, `mod/Forms/localization/eng/modifiers.json`
- `mod/Forms/localization/zhs/powers.json`, `mod/Forms/localization/zhs/modifiers.json`
- `mod/FormsCode/FormsStancePower.cs`
- `mod/FormsCode/FormStanceMode.cs`
- `mod/FormsCode/FormStanceModifier.cs`
- `mod/FormsCode/WatcherFormStancePower.cs`
- `mod/FormsCode/VoidSerpentStancePower.cs`
- `mod/FormsCode/DemonReaperStancePower.cs`
- `mod/FormsCode/EchoCelestialStancePower.cs`
- `mod/FormsCode/VoidFormEffectPower.cs`
- `mod/FormsCode/SerpentFormPower.cs`
- `mod/FormsCode/DemonFormPower.cs`
- `mod/FormsCode/ReaperFormEffectPower.cs`
- `mod/FormsCode/EchoFormEffectPower.cs`
- `mod/FormsCode/CelestialFormPower.cs`
- `mod/FormsCode/DemonFormStrengthTransactionPatch.cs`
- `mod/FormsCode/VoidFormPlayTransactionPatch.cs`
- `mod/FormsCode/FormStanceCmd.cs`
- `mod/FormsCode/FormStanceWatcherBridge.cs`
- `mod/FormsCode/FormStanceModePatch.cs`
- `mod/FormsCode/FormStanceMainMenuRetryPatch.cs`
- `mod/FormsCode/MainFile.cs`
- `mod/FormsCode/Interop/FormsRuntimeEntryPoint.cs`

写入的报告:

- `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\forms-worker.md`

未写入 (遵守边界): 未改 `G:\omp works\Sts\sts2-spire1\mod` 任何产品源码/项目文件; 未构建; 未测试; 未部署; 未改共享配置; 未写 C:.