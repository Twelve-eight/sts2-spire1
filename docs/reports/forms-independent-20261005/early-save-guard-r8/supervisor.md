# supervisor - early-save-guard-r8

状态: SUPERVISION_PASS

## 门禁确认 (2026-10-05)

- 已读取 `gate-notice.txt` 与 `coordination.md`: 主会话真实 `multi_agent_v1.wait_agent` 对精确同批 worker `01a10953-b6d4-7022-ae18-13e8559c23f7` returned completed, `timed_out=false`; coordination 落盘时点 2026-10-05T07:56:27.4258582+08:00, 与 gate-notice 一致。门禁成立, 开始独立只读审查。
- 已读取 `worker.request.md` / `worker.md` / `api-type-warning.txt` / r5 `runtime-rework-r5\spire1-supervisor.md` / `docs\DEVELOP-forms-independent-20261005.md`。
- 本轮只读, 未改产品代码, 未构建/lint/测试/部署/运行游戏, 未执行 git, 未写 C:, 未委派, 未启动其它代理运行时。
- 模型/路由保持: `global:deepseek-v4.1-flash` / `wb2api` / `xhigh`。

## 写集核对 (独立)

- 最终三个白名单文件修改时间: `MainFile.cs` 2026-10-05 07:52:33, `Spire1PowersGatePatch.cs` 2026-10-05 07:52:33, `FormsMissingModifierSaveGuardPatch.cs` 2026-10-05 07:53:30。
- `mod/` 树中 2026-10-05 07:45 之后被修改的非 obj/bin 文件仅这三个; `Spire1.csproj` 最后修改 2026-10-05 05:24:22 (本轮未动)。写集与请求白名单一致, 无越界改动。
- 独立重算 SHA-256 (与 worker 报告一致):
  - `MainFile.cs` 18053 bytes `fd4fb20bad5c1634ef31eb5c9087968af5f48aaaa62fdaad2d117dc44ceb90df`
  - `Patches\FormsMissingModifierSaveGuardPatch.cs` 8978 bytes `f4f0fa5b8167e431a640f70b415e6913c521106269674fcf9fd4a38c4a3673ea`
  - `Patches\Spire1PowersGatePatch.cs` 97659 bytes `af12c81524d2f37c99d95994340ac4dc0bd276a56f271a793f018b1720e0f6e6`
- 三文件均无 BOM, 无 CRLF。

## 已确认

### P1 - guard 最早安全阶段显式安装 + Harmony 精确证明
- 优先级: P1。
- 绝对路径: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:69-87`; `...\Patches\FormsMissingModifierSaveGuardPatch.cs:59-145`。
- 触发条件: `MainFile.Initialize()` 每次进入; 安装点位于注册熔断面 (`:59-67`) 之后、`Phase1AssetReadinessAndConfig()` (`:89-97`) 之前。
- 宣称或权威契约: guard 是独立 save 安全面, 不受 `ContentUnavailableActive` 支配; 必须用 `Harmony.GetPatchInfo` 精确证明 owner+prefix+priority。
- 当前控制流: `MainFile.cs:75` 调 `EnsureInstalled(Phase3Harmony())`; guard `:74` 读 `HarmonyLib.Patches? info = Harmony.GetPatchInfo(TargetMethod)`, `:75-76` 仅当本 owner 且 `PatchMethod` 等于本 guard `Prefix` 时视为已装, 未装时 `:80` `CreateClassProcessor(...).Patch()`, `:83` 调 `VerifyInstalled` (`:116-137`) 复核 `patch.priority == (int)Priority.First`; 解析/读取/安装/证明失败一律 `:63-70` / `:85-93` 返回 false (fail closed); `MainFile.cs:77-86` 只记 Error 并明确 "save protection is incomplete", 不把内容熔断当保护生效。
- 可复现命令: `Select-String -LiteralPath '<MainFile.cs>' -Pattern 'EnsureInstalled'`; `Select-String -LiteralPath '<guard.cs>' -Pattern 'PatchInfo|ownerPrefixExists'`。
- 本机程序集实测 (`...\.nuget\packages\lib.harmony\2.4.2\lib\net9.0\0Harmony.dll`): `Harmony.GetPatchInfo(MethodBase)` 返回 `HarmonyLib.Patches`; `Patches.Prefixes` 为 public `ReadOnlyCollection<Patch>`; `Patch.priority` 为 `System.Int32`; `Priority.First` 为 literal int 800。`api-type-warning.txt` 勘误已被正确采纳, 三白名单文件无 `HarmonyLib.PatchInfo` 类型声明。
- 最小修复范围: 已满足; 无需修改。
- 尚缺的实机证据: 真实运行期 `GetPatchInfo` 的 owner/prefix 快照 (静态类型与语义已核对)。

### P1 - 不可用与 Phase1 故障路径仍保留 guard
- 优先级: P1。
- 绝对路径: `MainFile.cs:69-87`; `:223-242`; `:249-265`; `:276-282`。
- 触发条件: `ContentUnavailableActive` 置位, 或 Phase1/Phase2/Phase3 抛异常。
- 宣称或权威契约: 契约第 3 节 "FormStanceModifier 在选择新局和加载旧局时必须在桥不可用时显式失败"; 请求第 1 条要求不可用分支下 guard 仍已安装。
- 当前控制流: guard 在 `:75` 早于 Phase1 安装; 两个不可用分支 (`:223`, `:249`) 的提前 `return` 只影响 Phase3 通用扫描, 不再导致 guard 漏装。r5 的 P2 缺口 (不可用路径 guard 不安装) 已闭合。
- 可复现命令: `Select-String -LiteralPath '<MainFile.cs>' -Pattern 'ContentUnavailableActive|EnsureInstalled'`。
- 最小修复范围: 已满足; 无需修改。
- 尚缺的实机证据: 确定性不可用状态真实触发下加载旧 Forms 档的实际表现 (需主会话实机)。

### P2 - 重复 Initialize/扫描不叠加 patch
- 优先级: P2。
- 绝对路径: `FormsMissingModifierSaveGuardPatch.cs:46-47`, `:61`, `:72-83`; `MainFile.cs:267-298`。
- 触发条件: 重复 `Initialize()`, 或 Phase3 属性扫描。
- 宣称或权威契约: 重复初始化不得叠加相同 owner+prefix 条目; 保留 `_phase3Completed`/`_phase3ScanCompleted` 语义。
- 当前控制流: `:75-76` 先探测本 owner+prefix, 存在则只 `VerifyInstalled` 不重复 `Patch`; `InstallLock` (`:61`) 串行化; Phase3 扫描 `MainFile.cs:278` 用 `type == typeof(FormsMissingModifierSaveGuardPatch)` 精确跳过本 guard, 不与显式路径重复; `_phase3Completed`/`_phase3ScanCompleted` 语义未改。
- 可复现命令: `Select-String -LiteralPath '<guard.cs>' -Pattern 'ownerPrefixExists|InstallLock'`; `Select-String -LiteralPath '<MainFile.cs>' -Pattern 'FormsMissingModifierSaveGuardPatch'`。
- 最小修复范围: 已满足; 无需修改。
- 尚缺的实机证据: 真实重复 Initialize 下的条目计数 (静态幂等路径已核对)。

### P2 - 普通 modifier 不误拦, 无 Forms/Watcher 硬引用
- 优先级: P2。
- 绝对路径: `FormsMissingModifierSaveGuardPatch.cs:179-212`; `Spire1.csproj:21-30`, `:65-75`。
- 触发条件: 非 `SPIRE1-FORM_STANCE_MODIFIER` 的 modifier, 以及 Spire1 编译。
- 宣称或权威契约: 非 Forms raw modifier 保持原引擎解析; 不新增可选 mod 硬引用。
- 当前控制流: `:183-187` 未命中直接 `return true` 放行; `:207-212` 仅以 `StringComparison.Ordinal` 比对 `ModelId.Entry`; guard `using` 仅 BCL/sts2/BaseLib/Spire1, 无 `using Forms`/`typeof(Forms...)`/Watcher 类型; csproj `Reference Include` 仍只有 `0Harmony` 与 `sts2`, Forms 目录 `Compile Remove` 保留。
- 可复现命令: `Select-String -LiteralPath '<guard.cs>' -Pattern 'using '`; `Select-String -LiteralPath '<Spire1.csproj>' -Pattern 'Reference Include|Forms'`。
- 最小修复范围: 已满足; 无需修改。
- 尚缺的实机证据: 真实旧档中非 Forms modifier 的加载结果 (源码放行路径已核对)。

### P3 - PowersGate 第 48 行误导注释已更正
- 优先级: P3。
- 绝对路径: `...\Patches\Spire1PowersGatePatch.cs:44-49`。
- 触发条件: 阅读类级文档推断 `IsFormsPower` 行为。
- 宣称或权威契约: 请求第 3 条: 只看程序集 simple name+noncollectible 身份, 不看 Forms bridge `IsAvailable` 或签名。
- 当前控制流: `:48` 现表述为 "Forms 程序集缺失 (或类型为 null) 时该判定为 false; 签名校验失败/Retryable/Terminal/ShuttingDown 期间仍按程序集身份识别为 Forms 类型", 与 `:696-704` 方法级注释及 `FormsCompatibilityBridge.cs:246-272` 实际行为一致; 方法体未改, 仅该行注释。r5 残留项闭合。
- 可复现命令: `Select-String -LiteralPath '<Spire1PowersGatePatch.cs>' -Pattern '签名校验失败'` (现仅剩更正后的正向表述)。
- 最小修复范围: 已满足; 无需修改。
- 尚缺的实机证据: 无 (纯注释一致性)。

### P3 - 最小 diff 与写集边界
- 优先级: P3。
- 绝对路径: 上述三个白名单文件。
- 触发条件: 审查本轮写集。
- 宣称或权威契约: 只改白名单三文件, 不改其它生产/共享/测试/csproj。
- 当前控制流: 独立核对 `mod/` 树 2026-10-05 07:45 之后被修改的非 obj/bin 文件仅这三个; `Spire1.csproj` 未动; 三文件 SHA-256 与 worker 报告一致, 均无 BOM/CRLF。
- 可复现命令: `Get-ChildItem -LiteralPath '<mod>' -Recurse -File | Where-Object { $_.LastWriteTime -ge [datetime]'2026-10-05T07:45:00' -and $_.FullName -notmatch '\\(obj|bin|\.godot)\\' }`。
- 最小修复范围: 已满足; 无需修改。
- 尚缺的实机证据: 无。

## 进行中

- 无。独立只读审查已完成, 六项全部闭合。

## 未知

- 未构建, 未 lint, 未测试, 未部署, 未运行游戏, 未执行 git (按任务约束)。上述全部为源码级与本地程序集类型级证据, 不构成实机验证。
- 未实机验证: 确定性不可用状态下加载旧 Forms 存档时 guard 抛 `InvalidOperationException` 的真实表现; 重复 Initialize 在真实运行时的条目计数; Forms 桥 `IsAvailable` 运行期时序。
- 两 mod 均缺失时本保护代码不可执行, 不在覆盖范围 (未变)。

## 监督结论

- **SUPERVISION_PASS**。
- r5 的两项 NEEDS_REWORK 已闭合: P3 第 48 行注释更正; P2 不可用路径 guard 漏装改为最早安全阶段显式安装+精确证明。
- guard 最早安装、显式 Harmony owner+prefix+priority 证明、不可用与 Phase1 故障路径保留 guard、重复初始化不叠 patch、普通修正不误拦、无 Forms/Watcher 硬引用、最小 diff 均通过静态核对。
- `api-type-warning.txt` 勘误已被正确采纳 (`HarmonyLib.Patches`, 非 `HarmonyLib.PatchInfo`)。
- 实机与中央构建/PE/PCK 门禁仍由主 hub 集中执行, 本监督不代替。