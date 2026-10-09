# Forms 独立身份与资产审查 (identity-assets)

Request: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round1\identity-assets.request.md
Model: global:deepseek-v4.1-flash
Route: wb2api
Scope: mod\Spire1Code\Forms, mod\Spire1, mod\Spire1.json, ModelDb 调用, 本地化文件
约束: 只读审查, 未修改产品代码, 未构建, 未启动游戏, 未委派.

## 已确认

### C1 [P0] Forms 的 10 个模型必须在 ModelDb.Init 之后存在, 否则整桥回滚
- 绝对路径: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:168-177
- 触发条件: 任意一个 Forms 模型未被 ModelDb 注册 (例如命名/程序集扫描变化, 或某模型被删除/改名) 时, ValidateBinding 抛 InvalidOperationException, TryBind 进入 catch, _binding 置 null.
- 权威契约: FormStanceModePatch.cs:6-13 声明 "ModelDb.Init runs after all mod DLLs have loaded", 用 HarmonyPostfix 在 ModelDb.Init 之后绑定; 桥只在全部 patch 安装并验证成功后发布能力 (FormStanceWatcherBridge.cs:73-78).
- 当前控制流: ValidateBinding -> 对 10 个模型逐个 ModelDb.Contains 检查 (FormStanceWatcherBridge.cs:168-177) -> 任一失败 -> catch -> UnavailableReason 设置 -> IsAvailable 保持 false.
- 可复现命令: Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'ModelDb.Contains' -Context 0,3
- 最小修复范围: 若确实缺模型, 只需补齐对应模型类型; 不要在此处吞掉异常改为静默降级 (会破坏 FormStanceMode.RequireAvailable 的显式失败契约, FormStanceMode.cs:23-36).
- 尚缺的实机证据: 需要一次真实启动日志确认 "Spire1 Forms: Watcher bridge bound" 出现, 且 10 个模型均被 ModelDb 注册; 本次未启动游戏, 未取日志.

### C2 [P0] 普通局与形态局共享同一批类型身份, 规则开关只由 run modifier 决定
- 绝对路径: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:17-36, 38-73
- 触发条件: 任何 player 在任何 run 中调用 FormStanceMode.IsEnabled/RequireAvailable; 普通局没有 FormStanceModifier 时 IsSelected 为 false, IsEnabled 直接返回 false, RequireAvailable 才会抛异常.
- 权威契约: FormStanceMode.cs:17 注释 "Run-local selection. Bridge availability is capability, never the run's rules switch."; FormStanceModifier.cs:7-16 只在自定义 run modifier 列表出现, AfterRunCreated/AfterRunLoaded/BeforeCombatStart 均调用 RequireAvailable.
- 当前控制流: IsSelected(player) = player.RunState.Modifiers.Any(m => m is FormStanceModifier) -> IsEnabled -> IsSelected 否则 false -> RequireAvailable -> FormStanceWatcherBridge.IsAvailable 否则抛 InvalidOperationException.
- 可复现命令: Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs' -Pattern 'IsSelected|IsEnabled|RequireAvailable' -Context 0,2
- 最小修复范围: 不需要改类型身份; 若要新增形态局入口, 必须继续走 FormStanceModifier 的 run-local 选择, 不要把桥可用性当作规则开关.
- 尚缺的实机证据: 需要在普通局与形态局各跑一次, 确认普通局进入普通 Calm/Wrath/Divinity 而不触发 Forms 载体 (FormStanceCmd.cs:10-13 注释声明), 形态局才启用载体.

### C3 [P1] Forms 载体通过 ModelDb.Power<T>().ToMutable() 获取, 身份依赖类型注册
- 绝对路径: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:58-73, WatcherFormStancePower.cs:100-116
- 触发条件: 任何一次形态进入 (Calm/Wrath/Divinity) 都会调用 LogicalStance/CreateEffects, 从 ModelDb 取 canonical 实例并 ToMutable.
- 权威契约: WatcherFormStancePower.cs:16-19 注释 "Internal data is recreated by PowerModel cloning."; FormStanceMode.cs:57 注释这些 canonical 值是只读语义通知参数, 不是被施加的 power.
- 当前控制流: LogicalStance -> ModelDb.Power<VoidSerpentStancePower/DemonReaperStancePower/EchoCelestialStancePower>() -> CreateTracking -> canonical.ToMutable() -> BindNativeMarker; CreateEffects -> ModelDb.Power<SerpentFormPower/VoidFormEffectPower/DemonFormPower/ReaperFormEffectPower/EchoFormEffectPower/CelestialFormPower>().ToMutable().
- 可复现命令: Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs' -Pattern 'ModelDb\.Power<' -Context 0,1
- 最小修复范围: 若 ModelDb 注册键与类型不一致, 需在模型注册处修复, 不要在调用点加字符串 ID 兜底 (会绕过类型身份契约).
- 尚缺的实机证据: 需确认每个 ModelDb.Power<T>() 在实机 ModelDb.Init 后返回非 null, 且 ToMutable 克隆出的内部数据独立 (WatcherFormStancePower.cs:50-59).

### C4 [P1] 资源根与图标路径在源码与 PCK 中都存在
- 绝对路径: FormStanceModifier.cs:12 (res://Spire1/images/powers/divinity_power.png); WatcherFormStancePower.cs:40-49 (res://Spire1/images/powers/{calm,wrath,divinity}_power.png 与 big/ 同名); MainFile.cs:17-18 (ModId="Spire1", ResPath="res://Spire1").
- 触发条件: 任意形态载体显示小/大图标, 或自定义修正列表显示 IconPath.
- 权威契约: MainFile.cs:17 注释 "used for resource filepath (res://Spire1)"; Godot 资源根由 PCK 内相对路径 Spire1/... 提供.
- 当前控制流: 图标为字符串常量, 由引擎在 res://Spire1/images/powers/ 下解析; 不经过 ModDb 身份映射.
- 可复现命令: 本地文件存在性: Get-ChildItem 'G:\omp works\Sts\sts2-spire1\mod\Spire1\images\powers' | Where Name -match 'calm_power.png|wrath_power.png|divinity_power.png'; PCK 字节检索: python -c "d=open(r'...Spire1.pck','rb').read(); print(b'Spire1/images/powers/calm_power.png' in d)".
- 证据: 目录内 calm_power.png=8684, wrath_power.png=8665, divinity_power.png=11485; big/ 下 calm=67180, wrath=71590, divinity=117511. PCK (G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.pck, 19669354 bytes, Godot 4.5.1, 2026-10-05 4:47:46) 字节检索 6 条路径均为 True.
- 最小修复范围: 无需修复; 若改名必须同步两处常量与 PCK 导出.
- 尚缺的实机证据: 未验证实际渲染 (headless 不渲染); 需可见运行确认图标显示.

### C5 [P1] eng 与 zhs 本地化键对 10 个 Forms 模型与修正均存在且非空
- 绝对路径: mod\Spire1\localization\eng\powers.json, zhs\powers.json; eng\modifiers.json, zhs\modifiers.json
- 触发条件: 显示姿态载体或内部形态效果 tooltip; 显示自定义修正 "姿态形态" 标题与说明.
- 权威契约: Forms 模型各自 Localization 属性 (如 WatcherFormStancePower.cs:33-34, VoidFormEffectPower.cs:42-46, SerpentFormPower.cs:78-82, DemonFormPower.cs:53-57, ReaperFormEffectPower.cs:29-33, EchoFormEffectPower.cs:31-35, CelestialFormPower.cs:32-36); 命名约定 ModId + "-" + 类型大写 (MainFile.cs:17).
- 当前控制流: 引擎按 SPIRE1-<CLASS_UPPER> 键读取 .title/.description/.smartDescription; 修正用 SPIRE1-FORM_STANCE_MODIFIER.title/.description.
- 可复现命令: ConvertFrom-Json 后按 9 个 SPIRE1-*_POWER 键取 title/description/smartDescription; 修正键见 eng/zhs modifiers.json.
- 证据: eng 与 zhs powers.json 均含 SPIRE1-VOID_SERPENT_STANCE_POWER, SPIRE1-DEMON_REAPER_STANCE_POWER, SPIRE1-ECHO_CELESTIAL_STANCE_POWER, SPIRE1-VOID_FORM_EFFECT_POWER, SPIRE1-SERPENT_FORM_POWER, SPIRE1-DEMON_FORM_POWER, SPIRE1-REAPER_FORM_EFFECT_POWER, SPIRE1-ECHO_FORM_EFFECT_POWER, SPIRE1-CELESTIAL_FORM_POWER 的 title/description/smartDescription, 且读取结果非空; eng/zhs modifiers.json 均含 SPIRE1-FORM_STANCE_MODIFIER.title/.description 且非空. PCK 内也含 Spire1/localization/eng/powers.json 与 zhs/powers.json 及两者 modifiers.json.
- 最小修复范围: 无需修复; 新增形态时必须同时补两份 powers.json.
- 尚缺的实机证据: 未在可见 UI 打开 tooltip 确认渲染与换行; 未确认非 eng/zhs 语言回落行为.

### C6 [P1] manifest 只声明 BaseLib, 与 Forms 代码实际使用面一致
- 绝对路径: mod\Spire1.json (dependencies: [{"id":"BaseLib","min_version":"3.4.5"}]); Forms 代码 using BaseLib.Abstracts (CustomPowerModel, CustomModifierModel).
- 触发条件: 游戏加载 Spire1 时解析 manifest 依赖.
- 权威契约: mod\Spire1.json 顶层 id/version/min_game_version/has_pck/has_dll; DEVELOP.md:17 记录 "Single dependency, deliberately" 与 BaseLib 3.4.5 字节一致结论.
- 当前控制流: 引擎按 manifest 加载 BaseLib; Forms 桥对 Watcher 使用反射 (FormStanceWatcherBridge.cs:108-126), 不产生 AssemblyRef, 因此 Watcher 不进 manifest.
- 可复现命令: Get-Content mod\Spire1.json; Select-String -Path mod\Spire1Code\Forms\*.cs -Pattern 'using BaseLib'; 检查 DLL AssemblyRef (需反编译, 见未知 U5).
- 证据: Spire1.json 548 bytes, 2026-09-30 2:01:48, version 1.2.3, min_game_version 0.111.0, has_pck/has_dll 均 true, affects_gameplay true, 仅 BaseLib 一条依赖. FormStanceWatcherBridge.cs:24-27 注释 "Optional reflection-only bridge ... No Watcher AssemblyRef.".
- 最小修复范围: 若 Forms 新增非 BaseLib 直接依赖, 必须在 Spire1.json 增加对应项; 当前无需修复.
- 尚缺的实机证据: 需反编译/反射确认发布 DLL 的 AssemblyRef 仅 BaseLib (见 U5).

### C7 [P0] ModelDb 身份 = (Category, Entry) 字符串对; Entry 由类型名 Slugify 得到, mod 类型再由 BaseLib 加前缀
- 绝对路径: .tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModelId.cs:8-41, ModelDb.cs:515-538; .tmp\baselib-dll\BaseLib.Extensions\TypePrefix.cs:7-21; .tmp\baselib-dll\Baselib.Patches.Content\PrefixIdPatch.cs:12-40; .tmp\dllsrc\MegaCrit.Sts2.Core.Helpers\StringHelper.cs:95-100.
- 触发条件: 每次 ModelDb.Init/Inject/GetId 计算身份; 每次存档 ToSerializable/FromSerializable 使用 Id.
- 权威契约: ModelId.Deserialize 要求恰好 "Category.Entry" 两段 (ModelId.cs:28-36); ModelDb.GetEntry = StringHelper.Slugify(type.Name) (ModelDb.cs:535-538); StringHelper.Slugify = CamelCase 转下划线 + ToUpperInvariant + 去非 [A-Z0-9_] (StringHelper.cs:95-100).
- 当前控制流: BaseLib PrefixIdPatch 是 ModelDb.GetEntry 的 HarmonyPostfix; 对实现 ICustomModel 的类型, Entry = TypePrefix.GetPrefix(type) + Slugify(TypeName), 前缀 = 命名空间首段大写 + "-" (TypePrefix.cs:9-21, PrefixIdPatch.cs:31-35). Spire1.Spire1Code.* 的首段是 "Spire1", 故 Entry 形如 SPIRE1-<SLUGIFIED_NAME>; Category 由继承树的 AbstractModel 直接子类名 Slugify 得到 (ModelDb.cs:520-533).
- 关键推论: 修改类型名会改变 Entry, 从而改变存档/联机身份; 单纯重命名文件不影响, 重命名类型或命名空间首段会破坏旧存档解析与联机 net id 对齐.
- 可复现命令: Get-Content .tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModelId.cs; Get-Content .tmp\baselib-dll\BaseLib.Extensions\TypePrefix.cs; Get-Content .tmp\baselib-dll\Baselib.Patches.Content\PrefixIdPatch.cs
- 最小修复范围: 不要为已有 Forms 类型改名; 若必须改名, 需保留旧 Entry 的别名映射或提供迁移 (仓库内当前没有 Forms 的旧 ID 迁移表).
- 尚缺的实机证据: 需实机确认 SPIRE1-VOID_SERPENT_STANCE_POWER 等 9 个 Entry 的实际字符串 (类型名推导, 未在运行中读取 ModelDb 输出).

### C8 [P0] 联机身份使用 ModelId 到 net id 的静态映射表 + 全局 Hash; 两端 mod 集合必须一致
- 绝对路径: .tmp\dllsrc\MegaCrit.Sts2.Core.Multiplayer.Serialization\ModelIdSerializationCache.cs:68-132, 194-262.
- 触发条件: 联机建局/序列化任意 ModelId 时, 引擎把 Category/Entry 字符串映射为整数 net id.
- 权威契约: Init 用 ContentSorter<ModelId> 对 ModelDb.All 排序后按序遍历, 为每个新 Category/Entry 分配递增 net id (ModelIdSerializationCache.cs:72-87); 对 manifest.affectsGameplay 为 true 的 mod, 同时把 Category/Entry/SavedProperty 名喂入 XxHash32 得到 Hash (ModelIdSerializationCache.cs:88-105, 125-126).
- 当前控制流: net id 顺序取决于 ContentSorter 的排序与完整 mod 集合; 任一端多/少一个 affectsGameplay 模型, net id 与 Hash 都会变化. Spire1.json 声明 affects_gameplay=true, 所以 Spire1 的全部模型参与 Hash.
- 关键推论: Forms 的 10 个模型被加入/移除/改名都会改变 net id 分配与 Hash; 联机双方必须加载相同版本的 Spire1 (以及相同 affectsGameplay 的其它 mod 集合).
- 可复现命令: Get-Content .tmp\dllsrc\MegaCrit.Sts2.Core.Multiplayer.Serialization\ModelIdSerializationCache.cs
- 最小修复范围: 无代码修复; 需要联机验收时, 用相同 mod 版本双端建局, 并核对日志 "ModelIdSerializationCache initialized ... Hash".
- 尚缺的实机证据: 未做双端联机; 未取两端 Hash 对比.

### C9 [P1] FormStanceModifier 通过 ModifierModel 的 SavedProperty 通道持久化; 身份是 ModelId, 不是类型引用
- 绝对路径: .tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs:139-160; mod\Spire1Code\Forms\FormStanceModifier.cs:8-22.
- 触发条件: 存档写入 FormStanceModifier 时, ModifierModel.ToSerializable 写 Id + SavedProperties.From(this) (ModifierModel.cs:145-153); 读档时 SaveUtil.ModifierOrDeprecated(serializable.Id).ToMutable() 按 Id 解析 (ModifierModel.cs:155-160).
- 权威契约: ModifierModel.ToSerializable 实现表明 Id 是存档身份; ModifierModel.IsEquivalent 默认按 GetType() 比较 (ModifierModel.cs:130-137).
- 当前控制流: FormStanceModifier 无 [SavedProperty], 故只持久化身份; AfterRunLoaded 会再次调用 RequireAvailable (FormStanceModifier.cs:16), 所以旧存档在缺少 Watcher 桥时会显式抛错而不是静默降级.
- 可复现命令: Get-Content .tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs; Get-Content mod\Spire1Code\Forms\FormStanceModifier.cs
- 最小修复范围: 不需要新增 SavedProperty; 若要让旧存档在缺桥时可加载, 需显式设计降级策略, 不要静默改 RequireAvailable.
- 尚缺的实机证据: 未做战中存档/读档与重连, 未确认 FormStanceModifier 在 SerializableRun.Modifiers 中的实际 JSON 形态.


### C10 [P1] Spire1ContentSnapshotModifier 展示了本仓已有的 ModelId 存档快照范式, 与 Forms 的 run modifier 是两个独立身份
- 绝对路径: mod\Spire1Code\Run\Spire1ContentSnapshotModifier.cs:6-25; mod\Spire1Code\Patches\Spire1ContentSnapshotPatch.cs:351-360, 472-501.
- 触发条件: 新局锁存内容登记值, 存档时按 ModelDb.Modifier<Spire1ContentSnapshotModifier>().Id 去重后追加 SerializableModifier (Patch.cs:351-360); 读档按同一 Id 查找并 Fill (Patch.cs:472-488).
- 权威契约: 该类文档明确真正的引擎 ModifierModel 子类经引擎 SerializableRun.Modifiers 的 [SavedProperty] 通道随存档持久化 (Spire1ContentSnapshotModifier.cs:6-15).
- 当前控制流: 快照在 RunState.CreateShared 构造前被过滤出活动 Modifiers (Patch.cs:169-184), 只在存档里保留; FormStanceModifier 则相反, 必须留在活动 Modifiers 才能被 FormStanceMode.IsSelected 看到.
- 可复现命令: Get-Content mod\Spire1Code\Run\Spire1ContentSnapshotModifier.cs; Get-Content mod\Spire1Code\Patches\Spire1ContentSnapshotPatch.cs
- 最小修复范围: 无需修复; 若将来给 Forms 加存档级开关, 可复用该范式, 但不要把它与 FormStanceModifier 的身份混用.
- 尚缺的实机证据: 未做真实存档读写验证该范式在 Forms 场景下的行为.

## 进行中

(无 - 检查面已收束)

## 未知

- U1: 未取得实机日志, 无法确认桥绑定成功与 10 个模型全部注册 (本次未启动游戏, 符合只读约束).
- U2: 已确认 PCK 含资源与本地化路径; 仍未确认运行时资源加载成功 (headless 未渲染).
- U3: 已确认 eng/zhs 键完整非空; 仍未确认可见 UI tooltip 渲染.
- U4: 已从引擎源码确认存档/联机身份是 ModelId 字符串 + net id 映射; 仍未做真实存档读写与双端联机验收.
- U5: 未反编译发布 DLL 确认 AssemblyRef 仅 BaseLib.
- U6: 未确认 Forms 10 个 Entry 的运行时实际字符串 (源码推导为 SPIRE1-<SLUGIFIED_NAME>, 未在运行中读取).
