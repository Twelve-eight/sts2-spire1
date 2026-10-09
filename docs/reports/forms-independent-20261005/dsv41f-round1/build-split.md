# Forms 独立化构建边界审查

Request: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round1\build-split.request.md
Model: global:deepseek-v4.1-flash
Route: wb2api
Scope: 只读审查. 未改产品代码, 未构建, 未部署, 未操作游戏, 未改共享配置, 未写 C:, 未委派.

## 已确认

### C1 [P0] SDK 与目标框架是 Forms 独立工程的第一道硬门禁

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` L1 `Godot.NET.Sdk/4.5.1`, L4 `net9.0`, L10 `AppOutputBase=$(MSBuildProjectDirectory)\`, L11 `PathMap`, L2 `Import .\Sts2PathDiscovery.props`.
- 触发条件: 新 csproj 若不继承同一 Godot SDK/TFM/PathMap/路径解析入口, 编译与 PDB 路径映射即与现状分叉.
- 权威契约: L1 是唯一 SDK 声明; L4 是唯一 TFM; L2 是唯一路径解析入口.
- 当前控制流: Forms 源码目前作为同一 csproj 的 `Spire1Code/Forms/*.cs` 参与编译 (实测 17 个文件), 无独立 SDK/TFM 覆盖.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' | Select-Object -First 12`
- 最小修复范围: 新 csproj 首行保持 `Godot.NET.Sdk/4.5.1`, `TargetFramework` 保持 `net9.0`, 显式声明 `AppOutputBase`/`PathMap`, 并复制或链接 `Sts2PathDiscovery.props`.
- 尚缺实机证据: 新工程实际编译产物与 Godot 导出链未验证 (只读审查, 未构建).

### C2 [P0] 游戏与 Harmony 引用来自 Sts2DataDir, 必须整体迁移

- 绝对路径与准确行号: `Spire1.csproj` L21-L30: `Reference Include="0Harmony"` (HintPath `$(Sts2DataDir)/0Harmony.dll`, Private=false) 与 `Reference Include="sts2"` (HintPath `$(Sts2DataDir)/sts2.dll`, Private=false).
- 触发条件: Forms 代码大量引用 `MegaCrit.Sts2.*` (实测每个 Forms 文件均有 `using MegaCrit.Sts2.*`) 与 `HarmonyLib`; 独立工程缺少 L2 或 L21-L30 即编译失败.
- 权威契约: `Sts2PathDiscovery.props` L28-L34 解析 `Sts2Path` -> `ModsPath`/`Sts2DataDir`, 且 L6-L15 明确拒绝回退到 Steam 库; L21-L30 是唯一游戏/Harmony 引用块.
- 当前控制流: 同一 csproj 内所有 `Spire1Code/**.cs` 共享这两个引用; Forms 无独立引用声明.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Sts2PathDiscovery.props' | Select-Object -Index (27..34)`
- 最小修复范围: 新工程复制 `Sts2PathDiscovery.props` 的解析链与 0Harmony/sts2 两个 Reference 及 Private=false; 不得改成 Steam 回退.
- 尚缺实机证据: 未对 Forms 文件逐个做符号解析, 引用清单尚未闭合.

### C3 [P0] PackageReference 集合决定 analyzer/PCK/publicizer 行为

- 绝对路径与准确行号: `Spire1.csproj` L41-L44 `Krafs.Publicizer 2.3.0` + `Publicize Include="sts2"`; L47 `Alchyr.Sts2.BaseLib 3.4.5` (PrivateAssets=All); L48 `Alchyr.Sts2.ModAnalyzers` Version `*`; L50 `BSchneppe.StS2.PckPacker 0.1.1` (PrivateAssets=All); L51 `AdditionalFiles Include="Spire1/localization/**/*.json"`.
- 触发条件: 新工程缺少 ModAnalyzers 则 analyzer 诊断与 AdditionalFiles 检查消失; 缺少 PckPacker 则简单资产打包链消失; 缺少 Publicizer 则对 sts2 私有成员访问失败; `Version="*"` 使依赖版本不可复现.
- 权威契约: 上述四条 PackageReference 与一条 AdditionalFiles 是当前唯一声明; `BaseLib 3.4.5` 与 `Spire1.json` L13 `min_version 3.4.5` 由 L118-L140 `UpdateDependencyVersions` 保持同步.
- 当前控制流: 四条包引用同时作用于 Spire1 与 Forms 全部源码; Forms 无独立包集合.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' | Select-Object -Index (40..51)`
- 最小修复范围: 新工程按需复制 L41-L51; 若 Forms 不访问 sts2 私有成员可评估去掉 Publicizer, 但须以 Forms 源码证据为准; 建议把 `*` 固定为实测版本.
- 尚缺实机证据: 未确认 Forms 是否使用 publicized 成员, 也未确认 ModAnalyzers 对 Forms 的必需性.

### C4 [P0] Compile Remove 把 Spire1 资产目录挡在编译外, 但不区分 Forms

- 绝对路径与准确行号: `Spire1.csproj` L54-L63: `Compile Remove="Spire1/**"`, `materials/**`, `shaders/**`, `images/**`, 以及对应 `EmbeddedResource Remove`.
- 触发条件: 独立 Forms 工程若沿用父目录相对 Include, 可能把 `Spire1/**` 资产再次卷入; 若不复制这些 Remove, 会把父工程资产误带入 Forms.
- 权威契约: L55-L62 是当前唯一资源排除清单.
- 当前控制流: 排除只针对 `Spire1/`, `materials/`, `shaders/`, `images/`; `Spire1Code/Forms/**` 不在排除列表, 因此当前被编译. 同时 L99-L103 `<None Include="Spire1/**">` 与 L107-L109 `<Folder Include="Spire1\images\...">` 把 Spire1 资产登记为工程项.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' | Select-Object -Index (53..62)`
- 最小修复范围: 新工程以显式 `Compile Include` 或独立目录树限定 Forms, 并显式排除 Spire1 资产目录与 `None/Folder` 登记项.
- 尚缺实机证据: 未核对新工程目录布局, 无法断言实际 glob 结果.

### C5 [P0] manifest 是 Spire1 身份, 不能直接复用

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1.json` L2 `"id": "Spire1"`, L8 `"has_pck": true`, L9 `"has_dll": true`, L10-L15 `dependencies` 仅 `BaseLib min_version 3.4.5`, L16 `affects_gameplay true`.
- 触发条件: Forms 独立 mod 若沿用 `Spire1.json`, 会在运行时与 Spire1 争用同一 mod id, 且 `Spire1.csproj` L323 会以 `$(AssemblyName).json` 覆盖同一目标文件.
- 权威契约: 当前 manifest 唯一 id 为 Spire1; has_pck/has_dll 均为 true. 消费点: `Spire1.csproj` L101 `None Include="Spire1.json"`, L120 `ManifestPath=$(MSBuildProjectName).json`, L389 `_PckProducerManifestPath=$(MSBuildProjectDirectory)/$(MSBuildProjectName).json`, L597 同一路径, L323 `Copy SourceFiles="$(AssemblyName).json"`.
- 当前控制流: PCK 生产者门禁 (L384-L418) 与 Godot 发布门禁 (L588-L640) 均以 `$(MSBuildProjectName).json` 为唯一 manifest, 项目名即 mod id.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json'`
- 最小修复范围: 新工程必须有独立 manifest (独立 id/name/version/dependencies), 文件名与 `$(MSBuildProjectName)` 一致, 并让 `CopyToModsFolderOnBuild` (L319-L325) 复制它.
- 尚缺实机证据: 新 id 的实际加载与冲突行为未验证.

### C6 [P0] localization 目前锚定 Spire1 目录, 且 Forms 文案混在共享 JSON 内

- 绝对路径与准确行号: `Spire1.csproj` L51 `AdditionalFiles Include="Spire1/localization/**/*.json"`; L103 `None Include="Spire1/**"`.
- 触发条件: Forms 独立后若 localization 仍留在 `Spire1/localization`, 新工程 analyzer 看不到; 若整目录复制, 会把 Spire1 的 222 张卡/24 遗物文案一并带入 Forms.
- 权威契约: 当前 analyzer 输入只有 `Spire1/localization/**/*.json`. Forms 文案实测混在共享文件内: `G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json` L173-L199 与 `...\zhs\powers.json` L173-L199 含 `SPIRE1-VOID_SERPENT_STANCE_POWER` / `DEMON_REAPER_STANCE_POWER` / `ECHO_CELESTIAL_STANCE_POWER` / `VOID_FORM_EFFECT_POWER` / `SERPENT_FORM_POWER` / `DEMON_FORM_POWER` / `REAPER_FORM_EFFECT_POWER` / `ECHO_FORM_EFFECT_POWER` / `CELESTIAL_FORM_POWER`; `...\eng\modifiers.json` L2-L3 与 `...\zhs\modifiers.json` L2-L3 含 `SPIRE1-FORM_STANCE_MODIFIER`.
- 当前控制流: localization 与 Spire1 资产同树, 无 Forms 专属过滤; Forms 的 key 前缀仍是 `SPIRE1-`.
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json' -Pattern 'FORM|STANCE'`
- 最小修复范围: 先按 key 归属把 Forms 文案切分到独立 localization 根, 再让新 csproj 指向该根; key 前缀是否随 mod id 变更需单独决策.
- 尚缺实机证据: 尚未核对全部 24 个 localization 文件中 Forms key 的完整清单与重叠面.

### C7 [P0] Forms 命名空间被三个非 Forms 文件反向引用, 拆分会撕裂 Spire1 编译

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs` L10 `using Spire1.Spire1Code.Forms;`; L22-L26/L46-L55/L102-L104 调用 `FormStanceMode`/`FormStanceWatcherBridge`/`WatcherFormStancePower`; L185-L187 注释明示 Forms 经该桥进入.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` L17 `using Spire1.Spire1Code.Forms;`; L111 `private const string FormsNamespace = "Spire1.Spire1Code.Forms"`; L700-L705 `IsFormsPower`; L718/L789 `FormStanceMode.IsSelected`.
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs` L24 `using Spire1.Spire1Code.Forms;`; L321/L330/L445/L500/L1264-L1292/L1682-L1710/L1947/L2075 引用 `FormStanceWatcherBridge`/`FormStanceMode`/`FormStanceModifier` 与各 FormPower 类型.
- 触发条件: 任何"只把 `Spire1Code/Forms/**` 移出"的拆分都会让这三处失去类型而编译失败.
- 权威契约: 递归 grep `Spire1Code\.Forms` 在非 Forms 目录仅命中上述三个文件, 即 Forms 被外部引用的全部实测点.
- 当前控制流: 同一程序集内双向引用 (Forms 内 L5/L6/L11/L12/L19/L20 反向 `using Spire1.Spire1Code.Extensions` 与 `.Powers`), 形成 Extensions/Powers <-> Forms 环.
- 可复现命令: `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' -Recurse -File -Filter *.cs | Where-Object { $_.FullName -notmatch '\\Forms\\' } | Select-String -Pattern 'Spire1Code\.Forms'`
- 最小修复范围: 拆分前先定义跨 mod 契约 (公共接口/事件/反射契约), 覆盖 StanceCmd 的 stance 分派, Spire1PowersGatePatch 的 Forms 例外判定, 与 FormNativeSmokeRunner 的整条 smoke 依赖; 不能只搬目录.
- 尚缺实机证据: 跨 mod 契约下的运行期分派与 Harmony 补丁顺序未验证.

### C8 [P0] Forms 直接依赖 Powers 命名空间的具体类型 (Calm/Wrath/Divinity/StancePower)

- 绝对路径与准确行号: `Spire1Code\Forms\FormStanceMode.cs` L40-L44 (`typeof(CalmPower)`, `typeof(WrathPower)`, `typeof(DivinityPower)`), L58-L70 `LogicalStance` 返回 `StancePower`; `Forms\FormStanceWatcherBridge.cs` L278-L279 `.Where(power => power is CalmPower or WrathPower or DivinityPower)`; `Forms\FormStanceCmd.cs` L5 `using Spire1.Spire1Code.Extensions;` 与 L17/L20/L23 `StanceCmd.Enter<CalmPower|WrathPower|DivinityPower>`; `Forms\WatcherFormStancePower.cs` L20 `: StancePower`.
- 触发条件: Forms 独立后若不同时带走 (或依赖) `Spire1Code/Powers/{CalmPower,WrathPower,DivinityPower,StancePower}.cs` 与 `Spire1Code/Extensions/{StanceCmd,IOnStanceChanged}.cs`, 编译失败.
- 权威契约: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\StancePower.cs` L1-L11 是 Forms 基类链 (`: Spire1Power`, 需 `BaseLib.Abstracts`); `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\IOnStanceChanged.cs` L1-L10 是 stance 变更监听契约.
- 当前控制流: Forms 的 carrier/effect 类型挂在 Spire1 的 Power 继承树上, 由 `PowerCmd` 生命周期管理.
- 可复现命令: `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms' -File -Filter *.cs | Select-String -Pattern 'CalmPower|WrathPower|DivinityPower|StancePower|StanceCmd'`
- 最小修复范围: 新工程必须自带这 4 个 Power 基类与 `StanceCmd`/`IOnStanceChanged`, 或把 Spire1 设为强制依赖并显式声明契约; 二选一需先定契约.
- 尚缺实机证据: 未验证拆出后 `ModelDb` 能否同时解析 Spire1 与 Forms 两侧的 Power 模型.

### C9 [P0] Powers 基类 Spire1Power 与 Extensions 也被 Forms 链路间接需要

- 绝对路径与准确行号: `Spire1Code\Powers\Spire1Power.cs` L1-L4 `using BaseLib.Abstracts; using BaseLib.Extensions; using Spire1.Spire1Code.Extensions;` L14 `: CustomPowerModel`, L17-L18 `CustomPackedIconPath`/`CustomBigIconPath` 由 Id 推导图片路径; `Forms\FormStanceWatcherBridge.cs` L19 `using Spire1.Spire1Code.Extensions;`; `Forms\WatcherFormStancePower.cs` L11 同一 using.
- 触发条件: Forms 独立工程若只搬 `Forms/**`, 会在 `Spire1Power`/`Spire1.Spire1Code.Powers`/`Spire1.Spire1Code.Extensions` 命名空间上出现未解析符号.
- 权威契约: `Spire1Power` L17-L18 的图片路径契约决定每个 Power 的贴图必须位于 `Spire1/images/powers/<id>.png`; 该前缀是硬编码 mod id, 与 Forms 新 id 不兼容.
- 当前控制流: 同一程序集内类型可见, 无跨程序集边界, 因此没有 `internal`/`public` 可见性隔离.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\Spire1Power.cs' | Select-Object -Index (13..18)`
- 最小修复范围: 明确 Spire1Power 等基础类型归属 (随 Forms 复制, 或把 Spire1 提升为强制 mod 依赖并公开这些类型); 同时决定 Forms 贴图根前缀.
- 尚缺实机证据: 跨程序集的类型可见性、加载顺序与贴图解析未验证.

### C10 [P1] project.godot 的 assembly_name / config.name / 资源根是 Spire1 身份

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\project.godot` L13 `config/name="Spire1"`, L15 `config/icon="res://Spire1/mod_image.png"`, L27 `project/assembly_name="Spire1"`.
- 触发条件: Forms 独立工程若复用同一 `project.godot`, 输出程序集与资源根仍叫 Spire1, 与独立 mod id 冲突; 若不复用则新工程需自建 Godot 工程文件.
- 权威契约: L27 与 `Spire1.csproj` L595 `_PckPath=$(OutputPath)$(MSBuildProjectName).pck` 共同决定产物命名; L15 资源根与 `Spire1Code\MainFile.cs` L17-L18 `ModId="Spire1"` / `ResPath="res://Spire1"` 一致; `Forms\FormStanceModifier.cs` L12 `IconPath => "res://Spire1/images/powers/divinity_power.png"` 也硬编码该根.
- 当前控制流: Godot 导出 (`Spire1.csproj` L613 `--export-pack "BasicExport"`) 以本 `project.godot` 为工程根.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\project.godot'`
- 最小修复范围: 新工程建立独立 `project.godot` (独立 name/assembly_name/资源根), 并修正所有硬编码 `res://Spire1/...` 引用; 或在同一 Godot 工程下用独立 export preset.
- 尚缺实机证据: 未验证游戏对两个独立 PCK/程序集的加载与资源根解析.

### C11 [P0] export preset `all_resources` 会把 Spire1 资产整体打进 Forms PCK

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\export_presets.cfg` L3 `name="BasicExport"`, L9 `export_filter="all_resources"`, L10 `include_filter=""`, L11 `exclude_filter="ModTemplate.json"`, L19 `script_export_mode=2`, L69 `dotnet/include_debug_symbols=true`; 调用点 `Spire1.csproj` L613.
- 触发条件: Forms 若复用同一 `mod/project.godot` 与 `BasicExport`, 导出过滤为 `all_resources` 且无 include/exclude 限定, PCK 会包含工程根下全部资源; 当前 `mod/Spire1/` 实测 986 个 `.png` 与 24 个 `.json`, 均不在排除列表内, 且工程根的 `Spire1.json` 同样会被打包.
- 权威契约: L9-L11 是当前唯一导出过滤配置, 只排除 `ModTemplate.json`. 快路径等价默认见 `G:\omp works\Sts\sts2-spire1\.nuget\bschneppe.sts2.pckpacker\0.1.1\build\BSchneppe.StS2.PckPacker.targets` L6 `PckPackerSourceDir=$(MSBuildProjectDirectory)/$(MSBuildProjectName)/`, L7 `PckPackerResPrefix=$(MSBuildProjectName)`, L8 `PckPackerOutputPath=$(OutputPath)$(MSBuildProjectName).pck`.
- 当前控制流: 快路径按项目名取 `mod/<项目名>/` 作为唯一资源根, 项目名一变即换根; Godot 路径无项目名过滤, 扫描整个 Godot 工程.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\export_presets.cfg' | Select-Object -Index (2,8,9,10)` 与 `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1' -Recurse -File | Group-Object Extension`
- 最小修复范围: 新工程建立独立 Godot 工程根, 或为 Forms 增加 `include_filter="Forms/**"` 式显式过滤并把 `Spire1/**` 放入 `exclude_filter`; 不能沿用现预设直出.
- 尚缺实机证据: 未执行 Godot 导出, 未核对实际 PCK 文件清单; 该结论是配置级证据, 不是实测清单证据.

### C12 [P0] 发布/输出目标全部按项目名推导, 直接决定 Forms 的最终落地路径

- 绝对路径与准确行号: `Spire1.csproj` L162-L164 `AssertModsPathIdentity` (BeforeTargets 覆盖 `WritePckDigestForQuickPck;CopyToModsFolderOnBuild;CopyQuickPck;GodotPublish;CopyGodotPckToModsFolder`); L183-L185 与 L231-L233 允许根仅 `E:\Slay the Spire 2\mods`, `G:\omp works\Sts\_runtime\sts2-test-client-B\mods`, `G:\omp works\.tmp`; L229 `_ModsPathGuardDestNorm` 由 `$(ModsPath)/$(MSBuildProjectName)/` 构造; L287 与 L319-L325 复制目标同为 `$(ModsPath)/$(MSBuildProjectName)/`.
- 触发条件: 新工程沿用 L162-L316 的守卫时, `$(_ModsPathGuardDestAllowed)` 的第三条 (`g:\omp works\.tmp\...` 前缀) 仍可通过, 但前两条显式 Spire1 路径不再匹配; 若新工程名产生 `mods\<新名>` 且未更新允许根, 构建会在首次写入前 DENY.
- 权威契约: L162-L316 是唯一写入门禁, L183-L185/L231-L233 是唯一允许根清单; 默认部署目标为 `<允许根>/<项目名>/`.
- 当前控制流: 构建默认开启复制 (`CopyToModsFolderOnBuild` 非 false), 因此新工程不显式传 `/p:CopyToModsFolderOnBuild=false` 时会尝试向 `mods/<新名>` 写入.
- 可复现命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' | Select-Object -Index (161..184)`
- 最小修复范围: 新工程保留守卫骨架, 但把允许根清单改为 Forms 自身的部署目录, 并在隔离构建时显式传 `/p:CopyToModsFolderOnBuild=false`; 不得复用 Spire1 的目标目录.
- 尚缺实机证据: 未实际执行该守卫的 DENY/ALLOW 路径, 未验证新工程名下的解析结果.

## 进行中

- 已完成 12 项检查面 (C1-C12) 的静态核对, 达到请求上限, 停止扩展.
- 本轮全部结论均为源码/配置/依赖包文本证据, 无构建、无运行、无游戏内证据.

## 未知

- Forms 独立工程的实际目录布局与项目名未定, 因此 C11 的过滤修法与 C12 的允许根清单无法进一步收窄.
- Forms 与 Spire1 之间最终采用 "复制基础类型" 还是 "Spire1 强制依赖" 的契约未定 (C8/C9 的二选一).
- 跨程序集场景下 `ModelDb` 对两侧 Power/Modifier 模型的解析顺序, 以及 `FormStanceWatcherBridge` 的 Harmony 绑定时机, 均未在运行期验证.
- 未确认 `Spire1PowersGatePatch` 的 Forms 例外 (C7, L111/L700-L705) 在拆成两个程序集后是否仍按同一命名空间字符串命中; 若 Forms 改名命名空间, 该例外会静默失效.
- 未确认 17 个 Forms 源文件之外是否还有未纳入版本库的 Forms 专属资产 (本轮实测 `Spire1Code/Forms` 下无 `.cs` 以外文件, Forms 贴图疑似复用 `Spire1/images/powers/divinity_power.png`).