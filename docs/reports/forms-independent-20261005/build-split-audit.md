# Forms 独立构建拆分审查报告 (build-split-audit)

- 审查时点: 2026-10-05, Asia/Shanghai
- 审查姿态: 只读静态审查, 未构建, 未运行游戏, 未改产品代码
- 项目根: `G:\omp works\Sts\sts2-spire1`
- 源码核对基线: HEAD `5c35a49b6ba3b0f2187e4ad59df8ec2bf68a3f09` (交接文档记录 HEAD 为 `1149129`, 其后有文档提交; 全仓有 89 项既有 dirty, 本报告不改动它们)
- 交接文档: `G:\omp works\Sts\sts2-spire1\docs\HANDOFF-forms-current-20261005.md`
- 独立化目标: Forms 不引用 Spire1; Spire1 不硬引用 Forms; 缺 Watcher 时 fail-closed; Watcher 晚加载可重试; 同进程重加载/退出幂等; 不覆盖其他会话 dirty 修改
- 边界声明: 本报告全部为源码静态证据, 不含实机证据; 任何"实机未验证"项均显式标注

## 已确认

### F-01 [P1] 当前没有项目边界: Forms 全部编译进 Spire1.dll 同一程序集

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:1` 唯一项目为 `Godot.NET.Sdk/4.5.1`, 未设置独立 Forms 项目
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:54-63` 仅 `Compile Remove` 了 `Spire1/**`, `materials/**`, `shaders/**`, `images/**`; 没有 `Spire1Code/Forms/**` 的 Remove
  - `G:\omp works\Sts\sts2-spire1\mod\project.godot:16-18` `project/assembly_name="Spire1"`; 全仓没有第二个 mod csproj
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms` 共 17 个 .cs 文件 (例如 `FormStanceWatcherBridge.cs` 28667 bytes, `VoidFormPlayTransactionPatch.cs` 26715 bytes)
- 触发条件: 任何把 Forms 当作独立 mod 构建/发布的动作
- 契约或宣称: 独立化目标要求 Forms 与 Spire1 不互相硬引用; 当前两者是同一声明的程序集
- 当前控制流: SDK 默认 glob 收集 `mod/Spire1Code/**/*.cs`, Forms 类型与 Spire1 类型一起编译进 `Spire1.dll`; 不存在跨程序集边界可供反射/版本隔离
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' -Pattern 'Forms|Compile Remove'`
- 最小修复范围: 新建 Forms 项目与目录, 并把 `Spire1Code/Forms/**` 从 Spire1 编译项移除; 不改变 Spire1 其他代码
- 尚缺实机证据: 独立 Forms.dll 的游戏加载与 ModelDb 发现未验证

### F-02 [P1] Spire1 属性扫描只扫描自身程序集, Forms 拆出后补丁不会自动安装

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:250-274` `typeof(MainFile).Assembly.GetTypes()` 后按 `HarmonyPatch` 属性逐类安装
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs:8-13` `[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]` 的 Postfix 调用 `FormStanceWatcherBridge.TryBind()`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:57-62` 绑定只在第一次 `ModelDb.Init` 后尝试一次
- 触发条件: Forms 类型移动到另一个程序集后, 由 Spire1 的 Phase3 扫描安装补丁
- 契约或宣称: 拆分后 Forms 必须自带初始化与补丁安装路径; 当前依赖 Spire1 的全局扫描
- 当前控制流: Spire1 初始化时扫描 Spire1.dll 的 TypeDef; Forms 拆出后该循环看不到 Forms 类型, `FormStanceModePatch` 不会安装, `TryBind` 不会运行, 自定义修正不会出现在列表中
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs' -Pattern 'GetTypes|HarmonyPatch|CreateClassProcessor'`
- 最小修复范围: 新增 Forms 自己的 `[ModInitializer]` 与补丁安装入口 (显式安装 `ModelDb.Init` postfix 或做同等的按程序集属性扫描)
- 尚缺实机证据: 两个 mod 初始化顺序与 ModelDb.Init 时序未在实机验证

### F-03 [P1] Spire1 对 Forms 有 5 处编译期硬引用, 拆分前必须逐一改掉

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:10` `using Spire1.Spire1Code.Forms;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:22-26` `FormStanceMode.IsEnabled` / `FormStanceMode.KindOf` / `FormStanceWatcherBridge.CurrentKind`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:46-54` `FormStanceWatcherBridge.Enter` / `typeof(WatcherFormStancePower)`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:102-104` `FormStanceWatcherBridge.Exit`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:17` `using Spire1.Spire1Code.Forms;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:111` `private const string FormsNamespace = "Spire1.Spire1Code.Forms";`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:700-705` 按命名空间判定 Forms power
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:718` 和 `:789` `FormStanceMode.IsSelected(...)`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:24` `using Spire1.Spire1Code.Forms;` (测试运行器, 仍在 Spire1 编译项内)
- 触发条件: 从 Spire1 构建中排除 Forms 目录, 或把 Forms 类型移到另一个程序集
- 契约或宣称: 独立化目标要求 Spire1 不硬引用 Forms; 当前这些 using 和类型引用会直接产生 CS0246/CS0234 或 AssemblyRef
- 当前控制流: `StanceCmd` 在进入/退出/查询姿态时直接调用 Forms 静态类; powers gate 直接读取 Forms 修正状态; 无反射或注册表隔离层
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\**\*.cs' -Pattern 'FormStanceMode|FormStanceWatcherBridge|WatcherFormStancePower|Spire1Code.Forms'`
- 最小修复范围: 在 Spire1 侧保留一个只依赖 sts2/BCL 类型的注册表或反射协议, 由 Forms 在加载时注册; `StanceCmd` 和 powers gate 只调用该协议
- 尚缺实机证据: 反射协议下形态进入/退出和 powers gate 例外的实际行为未验证

### F-04 [P1] Forms 对 Spire1 也有编译期硬引用, 且引用的是 Spire1 的姿态基类与分发入口

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:5-6` `using Spire1.Spire1Code.Extensions;` / `using Spire1.Spire1Code.Powers;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:17-26` 调用 `StanceCmd.Enter<CalmPower|WrathPower|DivinityPower>` 与 `StanceCmd.Exit`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:11-14` 引用 `Spire1.Spire1Code.Extensions` / `Spire1.Spire1Code.Powers`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:20` `WatcherFormStancePower : StancePower` (Spire1 的 `Spire1.Spire1Code.Powers.StancePower`)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:161` `await StanceCmd.Exit(choiceContext, player, null);`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:5` `using Spire1.Spire1Code.Powers;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:40-45` 比较 `CalmPower` / `WrathPower` / `DivinityPower`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:19-20` `using Spire1.Spire1Code.Extensions;` / `using Spire1.Spire1Code.Powers;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:278-279` `OfType<StancePower>()` 过滤 `CalmPower or WrathPower or DivinityPower`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:367-372` `FormStanceMode.LogicalStance` + `StanceCmd.Dispatch`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\StancePower.cs:6-11` `StancePower : Spire1Power` (Forms 载体继承的 Spire1 基类)
- 触发条件: 把 Forms 移动到不引用 Spire1.dll 的新程序集
- 契约或宣称: 独立化目标要求 Forms 不引用 Spire1; 当前 Forms 载体继承 Spire1 的 `StancePower`, 桥接调用 Spire1 的 `StanceCmd`, 逻辑姿态映射引用 Spire1 的三个原生姿态类型
- 当前控制流: Forms 的姿态载体是 Spire1 姿态类型体系的子类; 外部查询 `OfType<StancePower>()` 能看到 Forms 载体; 拆开后若 Forms 复制一份自己的 `StancePower`, 两边类型身份不同, Spire1 的查询与分发将看不到 Forms 载体
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs' -Pattern 'using Spire1|StanceCmd|StancePower|CalmPower|WrathPower|DivinityPower'`
- 最小修复范围: 二选一. 方案 A (推荐, 严格无互引用): Spire1 侧定义只含 BCL/sts2 类型的反射注册协议, Forms 侧实现该协议并自持载体基类; 由 Spire1 通过协议查询/分发, 不共享 `StancePower` 类型. 方案 B (不满足 Forms 不引用 Spire1 的严格目标): Forms 声明对 Spire1 的硬依赖, 保留继承; 只避免 Spire1 对 Forms 的 AssemblyRef
- 尚缺实机证据: 方案 A 下第三方卡牌/能力对 Spire1 `StancePower` 的 `OfType` 查询是否仍能覆盖 Forms 载体, 未实机验证

### F-05 [P2] 测试专用 FormNativeSmokeRunner 与补丁仍编进 Spire1 发布 DLL, 拆分时应一并移出

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:28` `internal static class FormNativeSmokeRunner` (3615 行)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs:7-14` `[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]` 的 Postfix 无条件调用 `FormNativeSmokeRunner.TryStart(__instance)`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:30-31` 只有环境变量 `SPIRE1_FORM_SMOKE_REPORT` 作为开关
- 触发条件: Spire1 正式发布构建
- 契约或宣称: 该运行器是隔离烟测工具, 不是玩家功能; 它现在依赖 Forms 类型并随 Spire1.dll 发布
- 当前控制流: 每次 `NGame._Ready` 都安装并调用一次 `TryStart`; 未设置环境变量时提前返回, 但类型和代码仍在发布字节中
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\**\*.cs' -Pattern 'FormNativeSmokeRunner'`
- 最小修复范围: 把运行器与补丁移入独立烟测项目或从 Spire1/Forms 发布编译项显式 Remove; 保留其隔离测试用途
- 尚缺实机证据: 移出后原 r15 烟测入口的重建方式未验证

### F-06 [P1] 发布管线是单 mod 单项目结构, 无 Forms 独立产物与独立门禁

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1:107-115` 输入固定为 `mod\Spire1`, `mod\Spire1Code`, `mod\Spire1.csproj`, `mod\Spire1.json`
  - `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1:245-273` 单次 `dotnet build` 与单次 PckPacker 打包
  - `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1:316-431` 只向 `workshop\content\Spire1` 提升三文件
  - `G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json:5-15` `forbiddenAssemblyRefs.names` 目前不含 `Forms`
  - `G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json:41-51` `forbiddenTypeDefs` / `forbiddenNamespaces` 目前不含 Forms 命名空间
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:2` id 为 `Spire1`; 只有 BaseLib 一个依赖
- 触发条件: 拆分后分别发布 Spire1 与 Forms
- 契约或宣称: 当前门禁只证明 Spire1.dll 不含 Watcher/AutoAnthony 等 AssemblyRef, 不证明 Spire1.dll 不含 Forms 类型; 也不存在 Forms.dll 的门禁
- 当前控制流: 发布脚本按 MSBuildProjectName 和固定 id 推导所有路径; 新 mod 需要独立脚本或参数化
- 可复现命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1' -Pattern 'Spire1Code|Spire1.csproj|workshop.content.Spire1'`
- 最小修复范围: 新增 `Build-FormsRelease.ps1` (或参数化同一脚本); 新增 `forms-gate-config.json`; 在 Spire1 门禁中加入 `Forms` 到 forbiddenAssemblyRefs, 在 Forms 门禁中加入 `Spire1` (若要求严格双向独立)
- 尚缺实机证据: 新脚本与新门禁未运行

## 进行中

- 核对 ModelDb 对独立程序集模型类型的发现契约
- 核对资源路径 `res://Spire1/...` 与本地化键 `SPIRE1-*` 的迁移范围
- 核对 Watcher 晚加载重试与同进程重载幂等的当前实现边界
- 汇总最小安全的目录/编译项/发布结构方案

## 未知

- 独立 Forms 程序集能否被 ModelDb 自动发现 (仅静态推断, 待证据)
- 两个 mod 的初始化顺序与 ModelDb.Init 时序对绑定成功率的影响
- 拆包后第三方对 Spire1 `StancePower` 的查询是否覆盖 Forms 载体
- 其他会话 dirty 修改是否会被拆包动作覆盖