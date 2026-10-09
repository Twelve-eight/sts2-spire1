# DSV4.1F Round3 Spire1 Supervisor (独立只读监督)

- 范围: `spire1-worker.request.md` 全项, 最终四文件与 Forms 协议.
- 唯一指定模型: global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh (记录; 本轮执行 harness = Codex 原生子代理设施).
- 约束: 未委派, 未启动其它模型/fallback/外部 harness, 未写产品代码, 未构建/lint/测试/部署/运行游戏, 未改 Steam/共享 mod_configs/发布脚本, 未写 C:.
- 结论: NEEDS_REWORK (四文件主体正确, 但存在 1 项 P1 契约宣称缺口与 2 项 P2 项; 详见下).

## 已确认

### 门禁与同批身份
- [已确认][Gate] `coordination.md` 记录 Spire1 worker `01a108dc-af6a-7233-af1c-900de3d29274`, 对应监督 `01a108dc-affb-7f00-a63b-5e8f569bf9cb`; Spire1 round3 门禁段记录 `NativeTool=multi_agent_v1.wait_agent`, `Target=01a108dc-af6a-7233-af1c-900de3d29274`, `ReturnedStatus=completed`, `timed_out=false`, `GateCheckedAt=2026-10-05T05:55:30.1046280+08:00`. 本监督在真实原生门禁后激活.
- [已确认][范围] 实现报告 `spire1-worker.md` 声明 CODE_COMPLETE; 产品改动仅 `FormsCompatibilityBridge.cs` 与 `StanceCmd.cs`; `Spire1.csproj` 与 `Spire1PowersGatePatch.cs` 复核无改动. 只读核对最终四文件与 Forms 协议.

### 已修 P0 (逐条核对通过)
- [已确认][P0 已修] 签名/运行可用分离: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs:92-112` 每次调用 `GetUsableBridge()` 后再执行 `bridge.IsAvailable()`; `:438-474` `IsBridgeUsable` 复用前重新枚举当前 Forms 程序集、`ReferenceEquals` 身份比对, 并对 `IsAvailable=false` 返回 false. 签名缓存不再掩盖 Retryable/Terminal/ShuttingDown.
- [已确认][P0 已修] 已选模式 fail-closed: `FormsCompatibilityBridge.cs:131-164` 命中本局精确 modifier 身份而桥不可用时抛 `InvalidOperationException`; `:308-356` Enter/Exit 桥为 null 时抛出, 不再 `return` 吞掉切换. `StanceCmd.cs:20-33` `KindNone` 抛 `NotSupportedException`; `:49-59` Enter 与 `:105-111` Exit 走桥.
- [已确认][P0 已修] 精确身份: `FormsCompatibilityBridge.cs:195-215` 只接受 `type.FullName == "Forms.FormsCode.FormStanceModifier"` 或 `CustomIDAttribute.ID == "SPIRE1-FORM_STANCE_MODIFIER"`. 与 `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:13-14` 一致; `CustomIDAttribute.ID` 见 `G:\omp works\Sts\sts2-spire1\.tmp\baselib-dll\BaseLib.Utils.Attributes\CustomIDAttribute.cs:6-9`. 不再把 Forms 程序集任意 modifier 当形态选择.
- [已确认][P1 已修] 重探测/身份: `FormsCompatibilityBridge.cs:358-394` 同一 revision 内只重读运行可用; `:496-525` 同 simple name 多程序集抛错; `:71-85` AssemblyLoad 回调只递增 revision, 不触碰 Godot/Harmony; 明确不宣称进程内热替换.
- [已确认][契约一致] Forms 入口签名逐项通过: `G:\omp works\Sts\sts2-forms\mod\FormsCode\Interop\FormsRuntimeEntryPoint.cs:21,29,32,35,43,54,66` 与 `FormsCompatibilityBridge.cs:625-642` 的反射校验、契约第 2.3 节一致.
- [已确认][无需改动] `Spire1.csproj:71-75` 无条件排除 `Spire1Code/Forms/**` 与两个 smoke 文件. 实测 380 个非排除 .cs 无 `Spire1.Spire1Code.Forms` 类型引用(仅 `FormsCompatibilityBridge.cs:41-42` 字符串常量); `Spire1.json` 依赖仍只有 BaseLib.
- [已确认][P1 无需改动] `Spire1PowersGatePatch.cs:701` 按程序集身份识别 Forms power; `:711-716` Apply 与 `:782-786` Modify 直接放行; `:1015-1031, :1093-1100` 后备层仍只按 Spire1 命名空间判定.
- [已确认][只读通知] `FormsCompatibilityBridge.cs:745-759` `FormsCompatibilityEntryPoint.Dispatch` 仅做 kind 映射 + `StanceCmd.Dispatch`; `:761-769` `LogicalStance` 不 Apply/Remove/发资源/改形态.

### 发现 (NEEDS_REWORK 依据)
- [P1][契约宣称缺口] 已选 Forms 旧档在 Forms 程序集缺失时仍会静默降为普通规则, 与请求第 3 条意图及 worker 报告 "Forms assembly缺失时也检查本局已选身份" 的宣称不符. 证据链: `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Saves\SaveUtil.cs:75-78` `ModifierOrDeprecated` 在 ModelDb 查不到 id 时返回 `DeprecatedModifier`; `...\MegaCrit.Sts2.Core.Runs\RunState.cs:296` 加载存档时即调用 `ModifierModel.FromSerializable` 替换; `...\MegaCrit.Sts2.Core.Models.Modifiers\DeprecatedModifier.cs:3` 该类型无 CustomID 且全名不匹配. 因此 `FormsCompatibilityBridge.cs:171-193 HasFormsDeclaredModifier` / `:195-215 IsFormStanceModifier` 在 Forms 缺失时永远看不到原始 `SPIRE1-FORM_STANCE_MODIFIER` 身份, `:140-143` 返回 false, 走普通规则. 触发条件: Forms 未加载/被卸载时读取旧形态档. 最小修复: 在 Spire1 侧对 `ModifierModel.FromSerializable` 加 prefix, 用 `SerializableModifier.Id` 精确比对 `SPIRE1-FORM_STANCE_MODIFIER`, 命中则显式失败或标记待失败; 或在报告/契约中明确该边界为不可在四文件写集内实现, 不得宣称已覆盖. 尚缺实机证据: 真实旧档在 Forms 缺失时的加载行为.
- [P2][选择判定分歧] `FormsCompatibilityBridge.cs:131-164` 在 `HasFormsDeclaredModifier==true` 后直接 `return bridge.IsSelected(player)` (`:155`); Forms 侧 `FormStanceMode.IsSelected` 使用 `modifier is FormStanceModifier` (`G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs:19-20`), 与 Spire1 的 "全名或旧 CustomID" 谓词不完全等价. 当 Spire1 因旧 CustomID 判定为形态局而 Forms 类型谓词返回 false 时, `IsSelected` 返回 false, 静默回落普通规则, 与 `:124-129` 注释宣称 "A selected run ... never returns false" 不符. 触发条件: 运行中存在带该 CustomID 但非 `Forms.FormsCode.FormStanceModifier` 的 modifier. 最小修复: 在 `bridge.IsSelected(player)==false` 而 `HasFormsDeclaredModifier(player)==true` 时显式失败, 或对身份分歧记录并 fail closed. 尚缺实机证据: 冲突 modifier 场景.
- [P2][契约重复注册, hub 项] 契约增量明确 "Forms专属modifier/power本地化不得重复注册到Spire1生产包", 但 Spire1 生产本地化仍包含全部 Forms 专属 key: `mod\Spire1\localization\eng\powers.json:173-199`, `zhs\powers.json:173-199`, `eng\modifiers.json:2-3`, `zhs\modifiers.json:2-3`; 且 `Spire1.csproj:51` 以 `AdditionalFiles` 纳入. Forms 侧同名 key 见 `G:\omp works\Sts\sts2-forms\mod\Forms\localization\eng\powers.json:2-28` 与 `zhs\powers.json:2-28`. 触发条件: 两包同时加载时重复注册同名 key. 注: 该路径不在 worker 四文件写集内, 属主 hub/本地化清理项, 非本 worker 可修. 最小修复: 从 Spire1 生产本地化移除 Forms 专属 key, 保留普通 Spire1 仍用的共享姿态图标/普通键. 尚缺实机证据: 两包重复 key 的实际加载优先级.
- [P3][注释/宣称不符] `Spire1PowersGatePatch.cs:697-700` 注释称 "Forms 缺失或签名校验失败时返回 false", 但 `FormsCompatibilityBridge.cs:223-249 IsFormsType` 只按程序集 simple name + collectible 判定, 与 entry point 签名校验无关; 签名失败时仍返回 true. 最小修复: 更正注释或按需收紧谓词. 同 `FormsCompatibilityBridge.cs:209-214` 注释称属性反射失败不得把已选档降为普通局, 但该 catch 实际 `return false`; 仅当类型全名不匹配且 CustomID 反射失败时降级, 注释与行为不符. 属文档级, 不影响主路径.

## 进行中
- 无. 独立只读审查已完成; 剩余为修复后复核与主会话集中构建/实机门禁, 不在本监督本轮范围.

## 未知
- 未构建, 未 lint, 未测试, 未部署, 未运行游戏. 上述全部为源码级只读证据, 不构成实机验证.
- 未实机验证: Forms `IsAvailable` 在 Retryable/Terminal/ShuttingDown 之间的真实时序; 已选 Forms 局中 bridge 失效时 Enter/Exit 的实机异常面; 新 Forms 程序集晚加载/同名冲突/collectible 卸载; 旧档 modifier 在独立程序集下与 Forms 缺失时的实际 ModelDb 解析; 多人/UI/长战斗.
- 未核对: Spire1/Forms Release 构建产物的 AssemblyRef/TypeDef/manifest 与两 PCK 资源集合(需主会话集中构建门禁).

## 监督结论
- NEEDS_REWORK.
- Worker 四文件主体修复方向正确, 请求第 1/2/4/5/6 条与 Forms 签名核对均通过; 但 P1 项为请求第 3 条意图与报告宣称的实质缺口, 必须修复或按诚实性要求明确标注为四文件写集内不可实现, 不得保留 "已检查本局已选身份" 的过度宣称.
- P2 本地化重复注册属 hub 项; P2 选择判定分歧建议在 Spire1 侧收紧为 fail-closed.