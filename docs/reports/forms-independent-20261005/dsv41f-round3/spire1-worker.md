# Spire1 worker 报告 - dsv41f round3

- 范围: 仅 Spire1 optional bridge 返工
- 产品写集: 仅请求列出的四文件
- 报告写集: 仅本文件
- 约束: Native Codex only, 不委派, 不启动其它 agent runtime, 不构建/测试/部署, 不写 C:, 不改 Steam/共享 mod_configs
- 用户指定模型: global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh (记录; 本轮实际执行 harness = Codex)

## 已确认

- [已确认][P0] `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs:431-432` 仅调用 `RequireStaticPropertyGetter(entryPoint, "IsAvailable", typeof(bool))` 后丢弃 getter (`_ = isAvailableGetter;`), 注释自认 "Spire1 reads its own cached availability". 因此 Forms `IsAvailable == false` (Retryable/Terminal/ShuttingDown) 时, 只要签名存在, `ProbeLocked()` 仍会在 `:329-331` 发布 delegate 并置 `_available=true`. 触发条件: Forms 程序集已加载且签名匹配但运行态不可用; 后果: 桥被当作可用, 已选 Forms 局可能进入无有效后端的转发路径.
- [已确认][P0] `FormsCompatibilityBridge.cs` (修复前 :238-244 与 :263-269): `Enter`/`Exit` 在 `GetBridge()` 返回 null 时 `return`, 调用方 `StanceCmd.Enter/Exit` 因此观察到成功返回. 若本局已选 Forms 而探测失败 (Forms 晚加载、签名漂移、assembly 冲突、运行态不可用), 姿态切换被静默吞掉. 需在已选模式路径显式 fail-closed.
- [已确认][P1] round2 报告 `docs\reports\forms-independent-20261005\dsv41f-round2\spire1-worker.md` 自认 Forms 侧真实签名未核对 (工作区无 `sts2-forms`), 且 `IsFormsType` 在桥 disabled 时返回 false. 本轮需逐项核对 Forms 入口当前签名; 若 Forms 源仍缺失, 报告中必须标记为未覆盖而不是宣称一致.

## 进行中

### 第二批源码核对

- 已确定最小修复范围: FormsCompatibilityBridge.cs 重构可用性/身份/重探测; StanceCmd.cs 的 IsIn 已选路径显式失败; Spire1.csproj 与 Spire1PowersGatePatch.cs 复核后确认无需改动.

- [已确认][P0] `StanceCmd.cs` (修复前 :20-38) 的 `IsIn<TStance>` 在 `IsSelected==true` 但 `KindOf` 返回 `KindNone` 时会继续落到普通 `player.Creature.GetPower<TStance>()` 路径。若桥在两次调用之间失效, 这会静默退回普通规则。已选 Forms 模式必须显式失败.
- [已确认][P0] `FormsCompatibilityBridge.cs` (修复前 :154-177) 的 `HasFormsDeclaredModifier` 只按 `modifier.GetType().Assembly.GetName().Name == "Forms"` 判定; 任意未来 Forms 程序集 modifier 都会被当成形态选择. 需改为精确类型全名 `Forms.FormsCode.FormStanceModifier` 或旧 `CustomID` 值 `SPIRE1-FORM_STANCE_MODIFIER`.
- [已确认][P0] `FormsCompatibilityBridge.cs` (修复前 :139-152) 的 `IsFormsAssemblyLoaded` 在枚举失败时返回 false, 且 `IsSelected` 先依赖它早退; 若本局确已选 Forms 但程序集缺失/枚举失败, 会被当成普通局. 需先按本局 modifier 精确身份判定, 不依赖 Forms 程序集是否已加载.
- [已确认][P1] `FormsCompatibilityBridge.cs` (修复前 :284-306) 的 fastpath 只比较 `_available` 与 `_probedRevision`; `IsAvailable` getter 的运行态变化不会使缓存失效, 旧 delegate 会被复用. 需每次发布/继续使用 binding 前读取运行可用 getter, 并在 collectible 程序集不再加载时清空旧 delegate.
- [已确认][P1] `FormsCompatibilityBridge.cs` (修复前 :59-72) 的 AssemblyLoad 回调只递增 revision, 不访问 Godot/Harmony, 符合约束; 但 `FindFormsAssembly` 在同 simple name 多程序集时抛错并 fail closed, 需保留该行为并记录“不支持真正热替换, 需重启”的边界.
- [已确认][契约一致] `G:\omp works\Sts\sts2-forms\mod\FormsCode\Interop\FormsRuntimeEntryPoint.cs:21-70` 当前真实签名与契约一致: `public static bool IsAvailable`, `IsSelected(Player)->bool`, `KindOf(Type)->int`, `CurrentKind(Player)->int`, `Enter(PlayerChoiceContext,Player,Type,CardModel?)->Task`, `Exit(PlayerChoiceContext,Player,CardModel?)->Task`. 不改协议, 不硬引用 Forms.
- [已确认][契约一致] `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:13` 仍标注 `[CustomID("SPIRE1-FORM_STANCE_MODIFIER")]`; `BaseLib.Utils.Attributes.CustomIDAttribute` 的 `ID` 属性见 `G:\omp works\Sts\sts2-spire1\.tmp\baselib-dll\BaseLib.Utils.Attributes\CustomIDAttribute.cs:6-9`. 精确身份判定可同时覆盖类型全名与旧 CustomID.
- [已确认][无需改动] `Spire1.csproj:71-75` 已无条件排除 `Spire1Code/Forms/**`, `FormNativeSmokeRunner.cs`, `FormNativeSmokePatch.cs`; 非排除 377 个 .cs 中对 Forms namespace/类型的命中为 0, 仅剩反射字符串常量. `Spire1.json` 依赖仍只有 BaseLib. 本轮不新增 Manifest 依赖.
- [已确认][无需改动] `Spire1PowersGatePatch.cs:711-716, 782-786` 对独立 Forms power 的分支是“放行/不进入 Spire1 门控”, 不是过滤 Forms types; Forms 类型 namespace 为 `Forms.FormsCode`, 本就不命中 `Spire1.` 前缀. 保持该语义, 不新增 Forms namespace/type 例外.
- [已确认][无需改动] `FormsCompatibilityBridge.cs:543-572` 的 `FormsCompatibilityEntryPoint.Dispatch` 只把 kind 映射为 Spire1 canonical `CalmPower/WrathPower/DivinityPower` 作为只读通知参数, 再调用 `StanceCmd.Dispatch`; 不 Apply/Remove power, 不发资源, 不改形态.
- [已确认][无需改动] 卡牌 `DemonFormPower` 属于游戏原生 `MegaCrit.Sts2.Core.Models.Powers.DemonFormPower`, 不随 Forms 迁移; 本轮不改卡牌与发布脚本.

### 第三批: 修复后复核

- [已确认][P0 已修] `FormsCompatibilityBridge.cs:92-112, 438-474` 现把“签名有效”与“运行可用”分离: `_signatureValid` 只表示签名/身份绑定有效, `IsBridgeUsable` 在每次发布或继续使用 binding 前读取 Forms `IsAvailable` getter; `IsAvailable=false` (Retryable/Terminal/ShuttingDown) 不再被签名缓存掩盖.
- [已确认][P0 已修] `FormsCompatibilityBridge.cs:358-394` 的缓存策略: 同一程序集 revision 内只重读运行可用, 不再因 `IsAvailable=false` 反复反射; 新 Forms 程序集加载递增 revision 后重新校验签名与身份. 因此不存在“Retryable 每次调用重建 delegate”的浪费, 也不复用旧 delegate.
- [已确认][P0 已修] `FormsCompatibilityBridge.cs:131-164` 的 `IsSelected`: 先按本局 modifier 精确身份判定, 不依赖 Forms 程序集是否已加载; 命中已选身份而桥不可用时抛 `InvalidOperationException`, 不再返回 false 静默降级.
- [已确认][P0 已修] `FormsCompatibilityBridge.cs:195-215` 的 `IsFormStanceModifier`: 只接受精确类型全名 `Forms.FormsCode.FormStanceModifier` 或旧 `CustomID("SPIRE1-FORM_STANCE_MODIFIER")`; 不再把 Forms 程序集任意未来 modifier 当作形态选择.
- [已确认][P0 已修] `FormsCompatibilityBridge.cs:308-356` 的 `Enter`/`Exit`: 桥为 null 时显式抛 `InvalidOperationException`, 不再 return 吞掉切换. `StanceCmd.cs:20-33` 的 `IsIn` 在已选 Forms 且 `KindOf` 返回 `KindNone` 时显式抛 `NotSupportedException`, 不再落回普通规则.
- [已确认][P1 已修] `FormsCompatibilityBridge.cs:438-525`: 复用前重新枚举当前 Forms 程序集并做 `ReferenceEquals` 身份比对; collectible 程序集若已不在 `AppDomain.CurrentDomain.GetAssemblies()` 中则 fail closed; 同 simple name 多程序集显式抛错并提示“不支持进程内热替换, 需重启”.
- [已确认][P1 已修] `FormsCompatibilityBridge.cs:223-249` 的 `IsFormsType` 现独立于签名校验与运行可用, 只按程序集身份 + collectible 加载性判定. PowersGate 仍按“放行/不进入 Spire1 门控”处理独立 Forms power, 不新增 Forms namespace/type 例外.
- [已确认][无改动] `Spire1.csproj:71-75` 仍无条件排除 `Spire1Code/Forms/**` 与两个 smoke 文件; 非排除源码对 Forms namespace/类型的引用仅为反射字符串常量. `Spire1.json` 依赖仍只有 BaseLib.
- [已确认][无改动] `FormsCompatibilityBridge.cs:730-770` 的 `FormsCompatibilityEntryPoint.Dispatch` 仍只做只读 kind 映射 + `StanceCmd.Dispatch` 通知; 不 Apply/Remove power, 不发资源, 不改形态.

- Forms `IsAvailable` 在 Retryable/Terminal/ShuttingDown 之间的真实时序未实机验证; 本轮只保证读取该 getter 且不缓存其 false 结果.

## 未知

## CODE_COMPLETE

- 状态: CODE_COMPLETE
- 实际修改的产品文件 (2):
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs` (重写: 签名/运行可用分离, 精确 modifier 身份/CustomID, 每次使用前身份与可用性复核, collectible/同名冲突 fail-closed, 桥缺失显式失败)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs` (IsIn 已选路径显式失败; Enter 注释纠正)
- 复核后未修改的产品文件 (2):
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` (round2 既有排除项已满足第 5 条; 本轮无改动)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` (round2 既有程序集身份判定已满足第 5 条; 本轮无改动)
- 唯一报告: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round3\spire1-worker.md`
- 未构建, 未 lint, 未测试, 未部署, 未运行游戏.
- 未委派, 未启动其它 agent runtime.
