# FormsSaveGuardSmoke 实现者报告

## 已确认

- [P0] 请求中的权威源码根 `G:\omp works\.tmp\mpcs-sts2\src` 存在, 但文件在命名空间子目录而非平铺: 实际为 `src\MegaCrit.Sts2.Core.Models\ModifierModel.cs`、`src\MegaCrit.Sts2.Core.Saves.Runs\SerializableModifier.cs`、`src\MegaCrit.Sts2.Core.Saves\SaveUtil.cs`、`src\MegaCrit.Sts2.Core.Models\ModelId.cs`、`src\MegaCrit.Sts2.Core.Models\ModelDb.cs`。证据命令: `Get-ChildItem -Path 'G:\omp works\.tmp\mpcs-sts2\src' -Recurse -Filter 'ModifierModel.cs' -File` 返回 `...\src\MegaCrit.Sts2.Core.Models\ModifierModel.cs` (7040 bytes)。
- [P0] 权威 `FromSerializable` 契约: `ModifierModel.cs:155-160` 为 `SaveUtil.ModifierOrDeprecated(serializable.Id).ToMutable()`, 然后 `Props?.Fill(...)`; `SaveUtil.cs:75-78` 为 `ModelDb.GetByIdOrNull<ModifierModel>(id) ?? ModelDb.Modifier<DeprecatedModifier>()`。即未注册 id 会静默变 `DeprecatedModifier`。
- [P0] 身份生成真实规则 (非字面量): `ModelDb.cs:530-538` 的 `GetCategory` = `ModelId.SlugifyCategory(GetCategoryType(type).Name)`, `GetCategoryType` 沿 BaseType 走到直接子类 `ModifierModel`; `GetEntry` = `StringHelper.Slugify(type.Name)`, 且 `ModelId.SlugifyCategory` 会剥掉 `_MODEL` 后缀 (`ModelId.cs:53-68`)。因此 `ModifierModel` 家族的引擎 category 可由 `ModelDb.GetCategory(typeof(ModifierModel))` 在运行时真实求得。
- [P0] CustomID 覆盖链路: BaseLib `PrefixIdPatch.cs:12-39` 是 `[HarmonyPatch(typeof(ModelDb), "GetEntry")]` postfix, 命中 `CustomIDAttribute` 时把 `__result` 改为该 ID。`CustomIDAttribute.cs` 为 `[AttributeUsage(Class, Inherited=false)]`, `ID` 属性只读。`Forms\...\FormStanceModifier.cs:13-14` 标注 `[CustomID("SPIRE1-FORM_STANCE_MODIFIER")]`, 类为 `sealed class FormStanceModifier : CustomModifierModel`; category 不变, 仍为 `MODIFIER`。
- [P0] Forms 严格签名已核实: `Forms\...\Interop\FormsRuntimeEntryPoint.cs:21-32` 是 `public static class FormsRuntimeEntryPoint`, `public static bool IsAvailable => FormStanceWatcherBridge.IsAvailable;` (属性 getter, 非方法)。
- [P0] 生产保护 prefix 契约: `Spire1Code\Patches\FormsMissingModifierSaveGuardPatch.cs` 为 `[HarmonyPatch(typeof(ModifierModel), nameof(ModifierModel.FromSerializable), new[]{typeof(SerializableModifier)})]` + `[HarmonyPrefix]` + `[HarmonyPriority(Priority.First)]`, 私有静态 `bool Prefix(SerializableModifier)`; 命中 `Entry=="SPIRE1-FORM_STANCE_MODIFIER"` 且 `FormsCompatibilityBridge.IsAvailable==false` 时抛 `InvalidOperationException`, 否则放行。Spire1 的 Harmony owner = `Spire1.Spire1Code.MainFile.ModId` = `"Spire1"` (`MainFile.cs:17`, `:298-299`)。
- [P1] 参考工程结构已读取: `FormsNativeSmoke.csproj` (Godot.NET.Sdk/4.5.1 + net9.0 + Publicize sts2 + BaseLib 3.4.5 + Private=false 引擎引用 + 无 Publish/auto-deploy), `FormsNativeSmoke.json` (has_pck=false, has_dll=true), `MainFile.cs` (自有 assembly-only Harmony 扫描 + `NGame._Ready` postfix owner 校验, 失败写 LastPatchFailure 不吞), `FormNativeSmokePatch.cs` (NGame._Ready postfix 调 `TryStart`), runner 用 `SPIRE1_FORM_SMOKE_REPORT` + G: 路径白名单 + `game.GetTree().Quit(exitCode)`。
- [P1] 目标测试目录 `G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke` 在写入前不存在; 本轮只在该目录创建测试工程/manifest/README 与唯一报告文件。

- [P0] `HarmonyLib.Patch.priority` 字段名由既有源码证实 (`BaseLib.Diagnostics\HarmonyPatchDumpWriter.cs:117` 使用 `p.priority`), 故 runner 读取 `patch.priority` 成立.
- [P0] `TaskHelper.RunSafely` (`TaskHelper.cs:21-42`) 不吞异常 (重新抛出并触发 UnobservedFault); runner 另加 `RunGuardedAsync` 兜底: 未处理异常仍写失败 JSON 且 `Quit(1)`.
- [P1] 已在唯一白名单目录 `G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke` 创建 test-only 工程 (11 文件): `FormsSaveGuardSmoke.csproj` (Godot.NET.Sdk/4.5.1 + net9.0; Publicize 仅 sts2; 引用 0Harmony/sts2 Private=false + BaseLib 3.4.5; 无 FormsDllPath / ProjectReference / Publish / auto-deploy), `FormsSaveGuardSmoke.json` (has_pck=false, has_dll=true, 仅 BaseLib), `MainFile.cs` (本程序集-only Harmony 扫描 + NGame._Ready 实际 owner 命中校验, 失败不吞), `FormsSaveGuardSmokePatch.cs` (NGame._Ready postfix), `SaveGuardSmokeRunner.cs`, `README.md`, `GlobalUsings.cs`, `project.godot`, `Directory.Build.props`, `Sts2PathDiscovery.props` (无 Steam 回退), `NuGet.config`.
- [P1] 检查面实现映射 (`SaveGuardSmokeRunner.cs`):
  1. `CheckGuardOwner`: `AccessTools.Method` 取引擎 `FromSerializable(SerializableModifier)`; `Harmony.GetPatchInfo` 枚举 prefix; 必须存在 owner "Spire1" 且签名为 `static bool Prefix(SerializableModifier)`; 记录 priority/参数类型; 无该 owner 时该场景直接判失败, 不作验收.
  2. `CheckFormStanceIdentity`: category 由 `ModelDb.GetCategory(typeof(ModifierModel))` 运行时求得; raw `SerializableModifier` entry 精确为 `SPIRE1-FORM_STANCE_MODIFIER`; 反射 `Forms.FormsCode.Interop.FormsRuntimeEntryPoint` 的 `public static bool IsAvailable` 属性签名; 实际调用 `ModifierModel.FromSerializable(raw)`. 不可用时要求 `InvalidOperationException` (fail-closed); 若反射 `IsAvailable==true` 却抛异常则判失败; 返回 `DeprecatedModifier` 判失败; 可用时要求返回类型精确等于 `Forms.FormsCode.FormStanceModifier` 且 `Id.Entry` 精确. 不宣称跨进程旧档已通过.
  3. `CheckVanillaModifierRoundTrip`: 候选从 `ModelDb.GoodModifiers`/`BadModifiers` 真实列表选择且限定 sts2 程序集类型; `ToMutable` -> `ToSerializable` -> `FromSerializable`; 要求身份字符串与具体 Type 保留.
  4. `CheckUnknownModifierStillDeprecated`: 生成精确非 Forms 未知 entry, 确认 `ModelDb.GetByIdOrNull<ModifierModel>` 为 null 后 `FromSerializable`, 要求返回 `DeprecatedModifier`; 证明保护未封锁所有未知修正.
- [P1] NuGet.config 对齐参考工程 FormsNativeSmoke: globalPackagesFolder/repositoryPath 指向 `G:\omp works\Sts\sts2-forms\.nuget\packages`, 清空 fallbackPackageFolders, 仅 nuget.org 源; 不写 C: 缓存.
- [P1] 只读自检: 白名单目录 11 文件齐备; `FormsDllPath`/`ProjectReference`/`using Forms.`/`Spire1.Spire1Code` 在测试目录 grep 无命中; `SaveGuardSmokeRunner.cs` 花括号 76/76 平衡; `RunCheckOnMainThreadAsync` 5 处引用; `RunGuardedAsync`/`RunAndQuitAsync`/`QuitOnMainThreadAsync` 定义与调用成对.



## 勘误 (compile-hotfix, 2026-10-05)

- [P0] 中央构建 native-smoke-build-r6.log 6 errors/5 warnings 的真实证据: 显式 `HarmonyLib.PatchInfo` 变量声明与 `Harmony.GetPatchInfo` 的真实返回类型 `HarmonyLib.Patches` 不符; 旧 r6 静态 PASS 不是编译验收.
- [P0] 窄修复 (仅改三处变量声明, 业务/owner 条目验证/runner/project 不动):
  - `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\MainFile.cs:120`: `HarmonyLib.PatchInfo? info = ...` -> `var info = ...`
  - `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs:252`: `HarmonyLib.PatchInfo? info = ...` -> `var info = ...`
  - `G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs:190` (本 r7 写集自查发现的同类错误): `HarmonyLib.PatchInfo? info = ...` -> `var info = ...`
- [P1] 只读复查: 三个文件 grep `PatchInfo` 仅剩 `var info = Harmony.GetPatchInfo(...)` 与 `hasPatchInfo` 键; 无残留 `HarmonyLib.PatchInfo` 声明. 修改后文件 SHA256:
  - MainFile.cs: `000679911BB564C6C6D30549336F64048D72F6B5FF86394443C556441E7EAFD6`
  - LifecycleSmokeRunner.cs: `0DAB1681DAAEABBDFBA967EC85A9F77851FE16356885DBAD7F434311ACC1620B`
  - SaveGuardSmokeRunner.cs: `C0AC16FB68A47E1323545AA2614B6AD189BC28D0DA66D437745DCAE8E82323ED`

## 进行中

- 无. 本轮实现范围已收敛 (未构建, 未实机).

## 未知

- 编译结果 (本轮禁止构建): 未执行 dotnet build; 无法确认 API 名称 / Publicizer 结果 / 包还原.
- 实机证据 (本轮禁止运行游戏): 未确认 Harmony owner "Spire1" 实际命中, 未确认 forms-save-guard-smoke.json 与退出码.
- 目标环境实际是否存在 Forms/Watcher 程序集; runner 按运行时反射分支如实记录. 缺失时检查面 2 以明确 InvalidOperationException 通过 fail-closed 断言; 存在且可用时要求精确类型与 Id.
- 未验证边界: 未启动对局/战斗/读写真实 save/改 mod 选择或共享 config/第二次 ModelDb.Init; 不宣称跨进程旧档已通过.

## CODE_COMPLETE

- 本 r7 写集文件:
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\FormsSaveGuardSmoke.csproj
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\FormsSaveGuardSmoke.json
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\MainFile.cs
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\FormsSaveGuardSmokePatch.cs
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\README.md
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\GlobalUsings.cs
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\project.godot
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\Directory.Build.props
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\Sts2PathDiscovery.props
  - G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\NuGet.config
- compile-hotfix 窄改文件:
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\MainFile.cs (仅第 120 行变量声明)
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs (仅第 252 行变量声明)
- 唯一报告: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\save-guard-tool-r7\worker.md
- 未编译: 是 (本轮禁止构建).
- 未实机: 是 (本轮禁止运行游戏).
- 未再委派: 是.
