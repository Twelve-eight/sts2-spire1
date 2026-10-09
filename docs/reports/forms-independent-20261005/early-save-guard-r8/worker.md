# worker - early-save-guard-r8

状态: CODE_COMPLETE

## 已确认

### 修复 1 - guard 在最早安全阶段显式安装/精确证明
- 文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormsMissingModifierSaveGuardPatch.cs`
- 新增幂等入口 `EnsureInstalled(Harmony)` (`:59-110`): 不读 `Spire1Config`, 不受 `ContentUnavailableActive` 支配.
- 精确证明 (`:116-137`): 读取 `Harmony.GetPatchInfo(target)`, 要求条目 `owner == harmony.Id`, `PatchMethod` 等于本 guard `Prefix`, 且 `patch.priority == (int)Priority.First`; 任一漂移或读取异常返回 false (fail closed).
- 幂等 (`:72-83`): 安装前先探测同 owner+prefix 条目; 已存在则只复核不重复 `Patch`, 避免叠加相同条目. 目标/prefix 解析失败 (`:63-70`) 与安装/探测异常 (`:85-93`) 都显式记 Error 并返回 false, 不伪称已保护.
- 反射目标 (`:147-176`): `AccessTools.DeclaredMethod(ModifierModel, FromSerializable, SerializableModifier)`; prefix 反射带 `HarmonyPrefix` 属性校验.
- guard prefix 本体与 raw id 失败逻辑保持不变: `:179-205` 仍为 `Priority.First` + 命中 `SPIRE1-FORM_STANCE_MODIFIER` 且桥不可用即抛 `InvalidOperationException`.

### 修复 2 - MainFile 早调用 + Phase3 精确跳过
- 文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs`
- 最早安全阶段调用 (`:69-87`): 位于注册熔断面之后, `Phase1AssetReadinessAndConfig()` (`:89-97`) 之前; 安装失败仅记 Error, 明确 "save protection is incomplete", 不把内容熔断当保护生效.
- Phase3 扫描跳过 (`:276-282`): `type == typeof(FormsMissingModifierSaveGuardPatch) || Spire1PowersGate.IsGateManagedPatchType(type)` 时 continue, 避免通用扫描重复挂载.
- 既有 `_phase3Completed` / `_phase3ScanCompleted` 语义与 `ContentUnavailableActive` 分支 (`:223`, `:249`) 未改; 因 guard 已在最早阶段安装, 不可用分支提前 return 不再导致 guard 漏装.
- 未改 powers gate 本体, 未放宽 guard 自己的 rawID/Forms unavailable 失败逻辑, 未触碰非 Forms raw modifier 的原引擎解析.

### 修复 3 - PowersGate 第 48 行误导注释
- 文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:48`
- 现表述: 只看程序集身份; "签名校验失败/Retryable/Terminal/ShuttingDown 期间仍按程序集身份识别为 Forms 类型", 与 `:696-704` 方法级注释及 `FormsCompatibilityBridge.IsFormsType` 实际行为一致.
- 方法体未改; 仅该行注释.

### 静态核对
- `grep FormsMissingModifierSaveGuardPatch` 仅命中: guard 自身 (`FormsMissingModifierSaveGuardPatch.cs:36`, `:80`, `:161`), MainFile 早调用 (`MainFile.cs:75`) 与 Phase3 跳过 (`MainFile.cs:278`).
- 三文件均无 BOM, LF 行尾.
- SHA-256 (写入后):
  - `MainFile.cs`: `FD4FB20BAD5C1634EF31EB5C9087968AF5F48AAAA62FDAAD2D117DC44CEB90DF` (18053 bytes)
  - `Patches\FormsMissingModifierSaveGuardPatch.cs`: `F4F0FA5B8167E431A640F70B415E6913C521106269674FCF9FD4A38C4A3673EA` (8978 bytes)
  - `Patches\Spire1PowersGatePatch.cs`: `AF12C81524D2F37C99D95994340AC4DC0BD276A56F271A793F018B1720E0F6E6` (97659 bytes)

## 进行中

- 无.

## 未知

- 未构建, 未 lint, 未测试, 未部署, 未运行游戏, 未执行 git (按任务约束).
- 未实机验证: 确定性不可用状态下加载旧 Forms 存档时, guard 抛 `InvalidOperationException` 的真实表现.
- 未实机验证: 重复 Initialize / 正常扫描 / 不可用分支在真实运行时不叠加条目 (仅静态控制流与 `GetPatchInfo` 精确证明已核对).
- 未实机验证: Forms 桥 `IsAvailable` 运行期时序.
- 两 mod 均缺失时本保护代码不可执行, 不在覆盖范围 (未变).
## API 类型复核 (api-type-warning.txt)

- 已读 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\early-save-guard-r8\api-type-warning.txt`: 中央编译发现独立 test helper 误记 `Harmony.GetPatchInfo` 返回 `HarmonyLib.PatchInfo`.
- 本机程序集实测 (`G:\omp works\Sts\sts2-spire1\.nuget\packages\lib.harmony\2.4.2\lib\net9.0\0Harmony.dll`): `Harmony.GetPatchInfo(MethodBase)` 返回 `HarmonyLib.Patches`; `HarmonyLib.Patches` 为 public 类型; `HarmonyLib.PatchInfo` 不是该 API 返回类型.
- 本轮 `EnsureInstalled` 证明路径 (`FormsMissingModifierSaveGuardPatch.cs:74`, `:127`) 已使用 `HarmonyLib.Patches? info`, 未复制错误声明.
- 三白名单文件全量 grep `PatchInfo` 未发现 `HarmonyLib.PatchInfo` 类型声明; 既有 `Spire1PowersGatePatch.cs:568`, `:602`, `:643`, `:2132` 也均为 `HarmonyLib.Patches`.
- 结论: 无需改产品代码; 原白名单与静态修复内容保持不变.