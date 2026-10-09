# spire1-supervisor

状态: NEEDS_REWORK

## 已确认

### 门禁与身份
- 已读取 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\spire1-supervisor-gate-notice.txt`。门禁记录: 主会话真实 `multi_agent_v1.wait_agent` 于 2026-10-05 07:29 至 07:33 +08:00 收割, 精确 worker `01a10901-e2c3-79d1-9fa3-8c90ef1f5310` returned completed, `timed_out=false`。
- 已读取同目录 `coordination.md`。其同批身份段记录 Spire1 worker `01a10901-e2c3-79d1-9fa3-8c90ef1f5310` / supervisor `01a10901-e383-7150-b944-892fb3ac8832`; 2026-10-05 原生完成门禁补记记录两目标 completed, `timed_out=false`。
- 已读取同目录 `spire1-worker.md`, 状态 CODE_COMPLETE; 本监督在其最终写集落盘后开始只读复核。
- 本角色未委派, 未启动其它模型或 fallback, 未写产品代码, 未构建/lint/测试/部署/运行游戏, 未写 C:/Steam/共享 mod_configs/canonical/Workshop, 未执行 git。

### 写集范围
- 产品改动与请求白名单一致: 新 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormsMissingModifierSaveGuardPatch.cs`; `...\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs`; `...\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs`; `...\mod\Spire1\localization\eng\powers.json`; `...\zhs\powers.json`; `...\eng\modifiers.json`; `...\zhs\modifiers.json`。
- 未发现白名单外产品文件在本轮被改动。`mod\Spire1.csproj:71-75` 仍只 Remove `Spire1Code/Forms/**` 与两个 smoke 文件; 新 guard 文件未被排除, 会进入 `Spire1.dll`。
- `mod\Spire1Code\MainFile.cs:247-269` 的属性扫描会安装本程序集内所有 `[HarmonyPatch]` 类型; `Spire1PowersGate.IsGateManagedPatchType` (`...\Patches\Spire1PowersGatePatch.cs:868-875`) 不包含新 guard, 故正常路径下由 Phase3 属性扫描安装。

### P1 rawID 前置保护 (已确认)
- 权威引擎链: `...\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs:155-160` `FromSerializable(SerializableModifier)` 先 `SaveUtil.ModifierOrDeprecated(serializable.Id)`; `...\.tmp\dllsrc\MegaCrit.Sts2.Core.Saves\SaveUtil.cs:75-78` 查不到 id 时返回 `DeprecatedModifier`; `...\.tmp\dllsrc\MegaCrit.Sts2.Core.Runs\RunState.cs:291-296` 读档即 `save.Modifiers.Select(ModifierModel.FromSerializable)`; `...\.tmp\dllsrc\MegaCrit.Sts2.Core.Saves.Runs\SerializableModifier.cs:8-10` `Id` 为 `ModelId?`; `...\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModelId.cs:8-25` `Entry` 为 `string`。
- 新 guard `...\Patches\FormsMissingModifierSaveGuardPatch.cs:30` 的 Harmony 目标签名与 `ModifierModel.cs:155` 精确一致; `:39-41` 为 Prefix, `:40` 为 `Priority.First`; `:67-72` 只读 `serializable?.Id` 与 `ModelId.Entry`, 以 `StringComparison.Ordinal` 比对 `SPIRE1-FORM_STANCE_MODIFIER`。
- 控制流: `:43-47` 非命中直接 `return true` 保持原路径; `:49-60` 命中后要求 `FormsCompatibilityBridge.IsAvailable`; `:52-57` 不可用则抛 `InvalidOperationException`, 不返回 true 让其降为 `DeprecatedModifier`, 也不返回 false 吞掉原方法; `IsAvailable` 自身抛异常时异常直接向上传播 (fail closed)。
- 身份等价性: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:13-14` 为 `[CustomID("SPIRE1-FORM_STANCE_MODIFIER")]`; `...\.tmp\baselib-dll\Baselib.Patches.Content\PrefixIdPatch.cs:17-40` 将 `CustomIDAttribute.ID` 写为 `ModelId.Entry`, 与 guard 的 raw Entry 比对一致。
- 保证范围: `FormsMissingModifierSaveGuardPatch.cs:22-24` 明确 Spire1 与 Forms 两 mod 都缺失时本 prefix 不存在, 不宣称覆盖该环境。此点与请求第 1 条一致。

### P2 选择判定 fail-closed (已确认)
- `...\Interop\FormsCompatibilityBridge.cs:133-181` `IsSelected`: `:142-145` 无精确身份直接 false; `:147-153` 有身份而桥不可用抛错; `:158` 调 Forms `IsSelected`; `:172-178` Forms 返回 false 时显式抛 `InvalidOperationException`, 不再静默回普通规则。
- `:188-210` `HasFormsDeclaredModifier` 只认全名或旧 CustomID; `:212-238` `IsFormStanceModifier` 的全名路径无反射, CustomID 反射失败在 `:226-237` 显式抛错, 不以 false 吞掉可能已选形态。
- Forms 侧 `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs:19-20` 用 `modifier is FormStanceModifier` 类型谓词; 两端合法 Forms 局同为 true, 身份分歧时 Spire1 侧 fail closed。此点与请求第 2 条一致。

### P3 注释更正 (部分完成, 有残留)
- 方法级注释 `...\Patches\Spire1PowersGatePatch.cs:696-702` 已改为: 判定只依据程序集 simple name 与 collectible 载入状态, 与 entry point 签名校验/运行期 `IsAvailable` 无关; 签名失败/Retryable/Terminal/ShuttingDown 期间仍识别为 Forms 类型。与 `FormsCompatibilityBridge.cs:246-272` `IsFormsType` 的实际行为一致。
- 但同类级注释 `...\Patches\Spire1PowersGatePatch.cs:48` 仍写 "Forms 缺失或签名校验失败时该判定为 false"。这与 `:699` 的更正表述及 `FormsCompatibilityBridge.cs:246-272` 的实际行为矛盾; 见下方 NEEDS_REWORK 项 P3。

### 本地化去重 (已确认)
- 去除精确键集合: 9 个 Forms 专属 power 前缀 (`SPIRE1-VOID_SERPENT_STANCE_POWER` / `SPIRE1-DEMON_REAPER_STANCE_POWER` / `SPIRE1-ECHO_CELESTIAL_STANCE_POWER` / `SPIRE1-VOID_FORM_EFFECT_POWER` / `SPIRE1-SERPENT_FORM_POWER` / `SPIRE1-DEMON_FORM_POWER` / `SPIRE1-REAPER_FORM_EFFECT_POWER` / `SPIRE1-ECHO_FORM_EFFECT_POWER` / `SPIRE1-CELESTIAL_FORM_POWER`) 各 `.title` / `.description` / `.smartDescription`, 共 27 键; 加 `SPIRE1-FORM_STANCE_MODIFIER.title` / `.description`, 共 2 键。
- 与 r15 stage 基线 `G:\omp works\.tmp\spire1-release-r15-promote-20261004\stage\Spire1\localization` 对比: `eng\powers.json` / `zhs\powers.json` 均 oldKeys=198 -> newKeys=171, removed=27, added=0, changedValues=0; `eng\modifiers.json` / `zhs\modifiers.json` 均 oldKeys=2 -> newKeys=0, removed=2, added=0, changedValues=0。
- powers 非 Forms 键值保持的强证据: 用旧文件按 Forms 前缀删除行后重建的 SHA-256 与新文件逐字相等: `eng\powers.json` new `c1671f2d52471c0c57a38c1e794a826c0d2f775316b6e53eae87a7c0e8b23428` = old-minus-Forms-lines; `zhs\powers.json` new `def941c6752eaa414dfb81d7a9d7fa9b072452e0c9b1c277cca29010b7120ddb` = old-minus-Forms-lines。modifiers 旧文件只含被删的 2 键, 新文件为 `{\n}\n` (4 bytes), 无其它键值可改。
- 键集合一致性: Spire1 剩余 171/171 keys, eng/zhs 无 key 漂移; Forms 侧 `G:\omp works\Sts\sts2-forms\mod\Forms\localization` 恰好含 27 power 键与 2 modifier 键, 与 Spire1 删除集合完全相同。Spire1 两 locale 中 Forms 前缀残留=0。
- JSON 均可 `ConvertFrom-Json` 解析, 无重复键, 无 BOM; `eng\powers.json` 15022 bytes, `zhs\powers.json` 15692 bytes, `eng\modifiers.json` / `zhs\modifiers.json` 各 4 bytes。共享姿态键 (`SPIRE1-CALM_POWER.*` / `SPIRE1-WRATH_POWER.*` / `SPIRE1-DIVINITY_POWER.*` / `SPIRE1-DEVA_FORM_POWER.*` / `SPIRE1-WRAITH_FORM_POWER.*` / `SPIRE1-RUSHDOWN_POWER.*` / `SPIRE1-MENTAL_FORTRESS_POWER.*` / `SPIRE1-LIKE_WATER_POWER.*` / `SPIRE1-SIMMERING_FURY_POWER.*` / `SPIRE1-MANTRA_POWER.*` / `SPIRE1-TALK_TO_THE_HAND_POWER.*` / `SPIRE1-WAVE_OF_THE_HAND_POWER.*`) 均保留, 无缺失。
- 未新增 zhs 译名; 未改其它 assets; Spire1 编译源中除 `Spire1Code/Forms/**` 与两个 smoke 文件外, 未发现对 9 个 Forms power 类型或键前缀的引用。

### 无硬引用静态核对 (已确认)
- `FormsCompatibilityBridge.cs:41-42` 对 Forms 类型只使用字符串常量, 未见 `using Forms`、`typeof(Forms...)` 或 Forms 编译期类型引用; `FormsMissingModifierSaveGuardPatch.cs` 只引用 BCL/sts2/BaseLib/Spire1 命名空间。
- 两侧 PE/AssemblyRef 门禁在 r4 已通过; 本轮新源码的中央 Release 构建、AssemblyRef/TypeDef/manifest、PCK 资源集合尚未由本监督重新验证 (见未知)。

## 进行中
- 无。独立只读审查已完成; 剩余为 NEEDS_REWORK 项修复后的复核, 以及主会话集中构建/PE/PCK/实机门禁。

## 未知
- 未构建, 未 lint, 未测试, 未部署, 未运行游戏。上述全部为源码级、静态 JSON 级与既有构建产物报告级证据, 不构成实机验证。
- 未实机验证: Forms 缺失时真实旧档经本 prefix 抛错的实际表现; Forms 程序集存在但 entry point 签名漂移/Retryable/Terminal/ShuttingDown 的时序; 已选身份而 Forms `IsSelected=false` 的真实触发面; 多人/UI/长战斗/战中读档。
- 未由本监督重新核对: 本轮新源码的 `Spire1.dll` / `Forms.dll` Release 构建产物, AssemblyRef/TypeDef/manifest, 两 PCK 资源集合。r4 产物报告 (`...\.tmp\forms-independent-20261005\spire1-structural-r3.json`, `forms-structural-r4.json`, `forms-pck-r4.json`) 仅证明 r4 字节, 不能替代 r5 中央构建门禁。
- 全模组缺失场景: Spire1 与 Forms 两 mod 都缺失时本 prefix 不存在, 该环境不受本 mod 保护, 已在源码注释与 worker 报告中明确。
- ContentUnavailableActive 路径: `MainFile.cs:203-222` 与 `:229-245` 在 Spire1 powers gate 确定性不可用状态下提前 return, 只安装 registration fuse 与 unavailable safety filters, 跳过 `:247-269` 的属性扫描; 因此新 guard 在该状态下不会安装。该边界未写入 worker 报告的 "明确保证范围"; 见下方 NEEDS_REWORK 项 P2。是否在真实运行中触发该状态需主会话构建/实机确认。

## 发现 (NEEDS_REWORK 依据)

### P3 - 同类级注释仍与已更正行为矛盾
- 路径: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:48`。
- 触发条件: 阅读类级文档并据此推断 `IsFormsPower` 在 Forms entry point 签名校验失败时的行为。
- 宣称或权威契约: 请求第 2 条要求 "修改相关误导注释, PowersGate注释只修程序集身份/签名无关的已知表述"; 同文件 `:696-702` 已明确 "签名校验失败/Retryable/Terminal/ShuttingDown 期间仍被识别为 Forms 类型"。
- 当前控制流: `IsFormsPower` 走 `FormsCompatibilityBridge.IsFormsType`; `FormsCompatibilityBridge.cs:246-272` 只检查程序集 simple name 与 collectible 载入状态, 不读取 entry point 签名校验结果或 `IsAvailable`。因此 Forms 程序集已载入但签名校验失败时 `IsFormsType` 返回 true, 不是注释所称 false。
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs' -Pattern '签名校验失败'` 可同时看到 `:48` 与 `:699` 两个相反表述。
- 最小修复范围: 将 `:48` 改为与 `:696-702` 一致的表述, 例如 "Forms 程序集缺失 (或类型为 null) 时该判定为 false; 签名校验失败/Retryable/Terminal/ShuttingDown 期间仍识别为 Forms 类型, 不按普通外部 mod power 重新纳入前缀门控"。方法体不动。
- 尚缺的实机证据: 无, 该项为纯静态注释一致性; 不影响运行行为, 但属于请求明确要求修改的误导注释, 未完全闭合。

### P2 - 确定性不可用路径下 guard 不安装, 报告保证范围未记录该边界
- 路径: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:203-222`, `:229-245`, `:247-269`; 新 guard `...\Patches\FormsMissingModifierSaveGuardPatch.cs:30-72`。
- 触发条件: Spire1 powers gate 覆盖证明失败或进入 `ContentUnavailableActive` 确定性不可用状态后, 玩家加载携带 `SPIRE1-FORM_STANCE_MODIFIER` 的旧档。
- 宣称或权威契约: 契约 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md` 第 3 节要求 "FormStanceModifier 在选择新局和加载旧局时必须在桥不可用时显式失败"; 请求第 1 条要求 rawID 命中后 Forms 不存在/签名或运行不可用明确抛错; 请求同时要求明确保证范围。
- 当前控制流: `MainFile.cs:203-222` 和 `:229-245` 在不可用状态安装 registration fuse 与 safety filters 后直接 `_phase3Completed = true; return;`, 不执行 `:247-269` 的 `[HarmonyPatch]` 属性扫描。guard 没有其它显式安装入口 (全仓库只有其自身类型声明, 无 `FormsMissingModifierSaveGuardPatch` 引用)。因此该状态下 guard prefix 不存在, `ModifierModel.FromSerializable` 会走 `SaveUtil.ModifierOrDeprecated` (`...\SaveUtil.cs:75-78`) 并可能降为 `DeprecatedModifier`, 静默回普通规则。
- 可复现命令: 只读检查 `MainFile.cs:203-222` / `:229-245` 的 return 与 `:247-269` 的扫描位置; 再 grep `FormsMissingModifierSaveGuardPatch` 确认无其它安装点。
- 最小修复范围: 两个选项。选项 A (不改产品行为, 最小): 在 worker 报告和契约中把该边界列入 "未知/未覆盖", 明确 `ContentUnavailableActive` 时本 guard 不安装, 不宣称该状态也 fail closed。选项 B (闭合行为, 超出本 worker 写集): 主会话在 `MainFile.cs` 不可用分支的 registration fuse 安装处显式安装/证明 guard, 或在进入不可用状态前完成 guard 安装; 需主会话集中构建与实机验证。
- 尚缺的实机证据: `ContentUnavailableActive` 的真实触发频率; 该状态下加载旧 Forms 档的实际引擎行为; 选项 B 的构建与实机结果。

## 监督结论
- NEEDS_REWORK。
- P1 rawID 前置保护、P2 选择判定 fail-closed、本地化去重、无硬引用静态核对均通过; 代码主体方向正确。
- 未闭合项: P3 `Spire1PowersGatePatch.cs:48` 残留误导注释; P2 `ContentUnavailableActive` 提前 return 导致 guard 不安装的边界未在 worker 报告/契约中明确记录。P3 最小修复为纯注释; P2 若只做文档明确, 可在本 worker 写集内完成, 若要求行为闭合则需主会话改 `MainFile.cs` 并重新走构建/实机门禁。
- 实机与中央构建仍由主会话集中执行; 本监督不代替构建、PE/PCK 门禁或实机验收。
---

## 增量 1 - 门禁 JSON 与 r5 集中构建门禁 (2026-10-05)

### 已确认
- 门禁文件实际位于 `G:\omp works\.tmp\forms-independent-20261005\r5-completion-gate.json` (通知中的 `runtime-rework-r5\r5-completion-gate.json` 路径不存在; 已在同目录 r5 报告目录外找到真实文件)。
- `r5-completion-gate.json`: `WaitTool=multi_agent_v1.wait_agent`; Targets 为 Forms worker `01a10901-e1f3-7f52-8d72-0ef5d3cf705f` 与 Spire1 worker `01a10901-e2c3-79d1-9fa3-8c90ef1f5310`, 两者 `ReturnedStatus=completed`; `TimedOut=false`; `ToolCallIdentifier=Not provided` (工具未提供, 与 coordination.md 一致, 未伪造)。此证据与本监督已采用的 coordination.md 门禁段一致。
- `G:\omp works\.tmp\forms-independent-20261005\forms-build-r5-status.json`: Forms r5 Release 构建 `ExitCode=0`, `ApprovedSourceHash=SourceHashAfter=140A85DD...AAB0` (构建期间源未漂移)。
- `G:\omp works\.tmp\forms-independent-20261005\forms-independent-r5-gates.json`: Forms r5 `Passed=true`; `ForbiddenReferences=[]`; `ProductionSmokeTypes=[]`; `EmbeddedFormsTypes=[]`; `ExactFormsIdentitySet=true`; `ManifestOnlyBaseLib=true`; PCK 18 entries, `Missing=[]`, `Unexpected=[]`, `EntryMd5Verified=true`; 本地化 4 文件各 2/27/2/27 键, 全部为 Forms keys; **`FormsKeysInSpire1=[]`**。
- 对 Spire1 侧的直接意义: `FormsKeysInSpire1=[]` 与本次监督对 Spire1 四 JSON 的静态核对一致 (Forms 前缀残留=0), 属独立第二来源佐证, 但不替代对 Spire1 四 JSON 的逐键/哈希核对 (后者已在本报告前述章节给出)。

### 进行中
- 无新增进行中面; 上述为集中构建/门禁证据收割, 不是本监督自己执行的构建。

### 未知
- `r5-completion-gate.json` 记录 `HashDisagreement=true`: `LiveSha256=140A85DD...AAB0` vs `ReportedWorkerSha256=6813383A...12DF`, 对象是 Forms 侧 `FormsCode\FormStanceWatcherBridge.cs`。这是 **Forms 侧** 的源哈希分歧, 不在 Spire1 监督写集/审查范围内; 本监督不据此对 Spire1 写集下结论, 但作为相邻未知上报主 hub。
- `forms-independent-r5-gates.json` 只覆盖 Forms 侧 DLL/PCK/本地化; 未在本文件中看到与 r5 Spire1 新源码对应的 `Spire1.dll` AssemblyRef/TypeDef/manifest 门禁 JSON。Spire1 侧 r5 构建产物门禁仍属未知, 需主 hub 确认是否另有文件或补做。
- 实机行为仍未验证 (无运行游戏证据)。

## 收敛 - 当前检查面结论 (2026-10-05)

- 行为核心面无阻断: P1 rawID 前置保护控制流/异常边界成立; P2 选择判定分歧 fail-closed 成立; 本地化去重键集合/非 Forms 键值保持成立; 无硬引用静态核对成立。请求第 1/2/4 条主体与静态可验证面通过。
- 仍未闭合的两项均为文档/边界声明级, 不改变上述行为核心:
  - **P3 (worker 写集内, 可最小修复)**: `...\Patches\Spire1PowersGatePatch.cs:48` 仍写 "Forms 缺失或签名校验失败时该判定为 false", 与同文件 `:696-702` 的更正及 `FormsCompatibilityBridge.IsFormsType` 实际行为相反。请求要求修改相关误导注释, 该项未完全闭合。最小修复为只改该行注释, 不动方法体。
  - **P2 (超出 worker 白名单, hub 决策项)**: `MainFile.cs:203-222` / `:229-245` 在 `ContentUnavailableActive` 时提前 return, 跳过 `:247-269` 属性扫描; 新 guard 无其它显式安装入口, 该状态下 guard 不安装。选项 A 为在 worker 报告/契约中把该边界列入未覆盖 (不改产品); 选项 B 为主 hub 在 `MainFile.cs` 不可用分支显式安装/证明 guard, 需重新构建与实机验证。
- 结论维持 **NEEDS_REWORK**, 但性质为文档/边界声明级; 若主 hub 判定 P2 仅需文档声明, 则剩余为 P3 一行注释。
- 本监督不构建、不测试、不部署、不运行游戏、不再委派; Spire1 侧 r5 Release 构建产物与实机验收仍由主 hub 集中执行。