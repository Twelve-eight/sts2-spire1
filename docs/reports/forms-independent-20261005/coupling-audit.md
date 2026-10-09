# Forms 与 Spire1 解耦审查报告 (coupling-audit)

- 审查时点: 2026-10-05, Asia/Shanghai
- 审查姿态: 只读静态审查, 未构建, 未运行游戏, 未改产品代码
- 项目根: `G:\omp works\Sts\sts2-spire1`
- 交接文档: `G:\omp works\Sts\sts2-spire1\docs\HANDOFF-forms-current-20261005.md`
- 独立化目标: Forms 不引用 Spire1; Spire1 不硬引用 Forms; 缺 Watcher 时 fail-closed; Watcher 晚加载可重试; 同进程重加载/退出幂等; 不覆盖其他会话 dirty 修改
- 边界声明: 本报告全部为源码静态证据, 不含实机证据; 任何"实机未验证"项均显式标注

## 已确认

### C-01 [P1] Forms 全部文件位于 Spire1 命名空间, 且直接编译期引用 Spire1 的 Extensions/Powers

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:5` `using Spire1.Spire1Code.Extensions;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:6` `using Spire1.Spire1Code.Powers;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:5` `using Spire1.Spire1Code.Powers;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:19` `using Spire1.Spire1Code.Extensions;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:20` `using Spire1.Spire1Code.Powers;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:11` `using Spire1.Spire1Code.Extensions;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:12` `using Spire1.Spire1Code.Powers;`
  - 全部 17 个 Forms .cs 文件的命名空间声明均为 `namespace Spire1.Spire1Code.Forms;`
- 触发条件: 任何把 `mod\Spire1Code\Forms` 目录整体移动到独立 mod 工程的构建/拆包动作
- 契约或宣称: 独立化目标要求 "Forms 不引用 Spire1"; 当前 Forms 处于 Spire1 的命名空间树与编译单元内
- 当前控制流: 编译期命名空间 + using 解析, 无反射兜底; 移动目录即产生 CS0246/CS0234 编译错误, 而非运行时降级
- 可复现命令:
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs' -Pattern '^namespace |^using '`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs' -Pattern 'Spire1Code\.'`
- 最小修复范围: 重命名 Forms 命名空间为独立根 (例如 `Forms`), 并把对 `Spire1.Spire1Code.Extensions` / `Spire1.Spire1Code.Powers` 的编译期引用替换为独立实现或反射; 具体清单见后续检查面
- 尚缺实机证据: 独立工程构建与游戏内加载均未验证; 本项只证明编译期耦合存在

### C-02 [P1] Spire1 侧 `StanceCmd` 硬引用 Forms 类型 (反向耦合, 与 C-01 互为闭环)

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:10` `using Spire1.Spire1Code.Forms;`
  - `:22` `FormStanceMode.IsEnabled(player)`
  - `:24-26` `FormStanceMode.KindOf(typeof(TStance))` / `FormStanceWatcherBridge.CurrentKind(player)`
  - `:46-54` `FormStanceMode.IsEnabled` / `FormStanceWatcherBridge.Enter` / `typeof(WatcherFormStancePower).IsAssignableFrom(typeof(TStance))`
  - `:102-104` `FormStanceMode.IsEnabled` / `FormStanceWatcherBridge.Exit`
- 触发条件: 从 Spire1 中移除 Forms 目录 (即把 Forms 拆成独立 mod) 的任何构建
- 契约或宣称: 独立化目标要求 "Spire1 不硬引用 Forms"; 现状为 `StanceCmd` 是 Watcher 卡/能力进姿态的统一入口, 直接按类型调用 Forms
- 当前控制流: `StanceCmd.IsIn/Enter/Exit` 在读取本局 Modifier 后, 若为形态局则把控制流整体交给 `FormStanceWatcherBridge`; 无反射隔离层
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs' -Pattern 'Form'`
- 最小修复范围: 在 Spire1 侧为形态模式定义接口/委托桥 (或改用反射探测), 使 `StanceCmd` 只依赖抽象契约; Forms 侧实现该契约并在可用时注册
- 尚缺实机证据: 拆分后 Watcher 卡进姿态的实际行为未验证; 本项只证明编译期反向依赖

### C-03 [P1] `Spire1PowersGatePatch.cs` 硬引用 Forms 类型 + 以字符串常量 `"Spire1.Spire1Code.Forms"` 判定命名空间

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:17` `using Spire1.Spire1Code.Forms;`
  - `:111` `private const string FormsNamespace = "Spire1.Spire1Code.Forms";`
  - `:700-705` `IsFormsPower` 按命名空间前缀判定
  - `:718` `IsFormsPower(power!) && FormStanceMode.IsSelected(target?.Player)` (Apply 门控例外)
  - `:789` 同型判定 (ModifyAmount 门控例外)
  - `:50-52` 注释明确说明 Forms 例外是必须的, 否则形态子系统会硬故障
- 触发条件: 拆分后 Forms 命名空间改名或独立程序集加载时, `IsFormsPower` 判定失效; 或 Forms 缺失时 `FormStanceMode` 类型解析失败导致 powers gate 类加载失败
- 契约或宣称: 独立化后 powers 门控仍需对 Forms 力量保持例外语义 (形态子系统自持生命周期)
- 当前控制流: powers gate 用编译期类型引用 + 命名空间字符串双重绑定 Forms; 拆分后二者都会失配
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs' -Pattern 'Forms'`
- 最小修复范围: 把 Forms 例外判定改为配置/契约式 (例如接口标记或注册表), 命名空间前缀改为独立 mod 的稳定标识; 同时移除对 `FormStanceMode` 的直接类型引用
- 尚缺实机证据: 拆分后 powers 开关与形态局共存的实际门控行为未验证
### C-04 [P1] Forms 与 Spire1 之间还存在反向硬依赖: Forms 引 Spire1 的 `StancePower`/`Spire1Config`/`IOnStanceChanged`, Spire1 引 Forms 的桥

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:20` `public abstract class WatcherFormStancePower : StancePower` (继承 Spire1 基类)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\StancePower.cs:6` `public abstract class StancePower : Spire1Power`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:5` `using Spire1.Spire1Code.Powers;` 并在 `:38-46` 直接比较 `CalmPower/WrathPower/DivinityPower`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:10` 反向 `using ...Forms;`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:5-6` 反向引用 `StanceCmd` 与 `Powers`
- 触发条件: 拆分或复制 Forms 目录到独立 mod 工程, 或单独发布 Forms 补丁包
- 契约或宣称: 独立化目标双向不硬引用; 当前是双向编译期引用 (循环), 只有同一次编译才能满足
- 当前控制流: 拆包后两个工程都缺少对方类型, 编译期失败; 若仅复制文件而不改 using/namespace, 编译产物仍会把两个 mod 合并为一个程序集
- 可复现命令:
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs' -Pattern 'using Spire1'`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs','G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs' -Pattern 'Forms'`
- 最小修复范围: 见 C-01/C-02/C-03; 核心是定义稳定的桥接口 (由 Spire1 侧提供, Forms 侧可选实现) 或全部走反射/属性注册, 并把 Forms 所需的力量基类改为可迁移/独立的基类
- 尚缺实机证据: 拆分后两 mod 同时加载/仅加载一个的行为未验证

### C-05 [P1] `Spire1PowersGate` 以字符串常量匹配 Forms 命名空间, 且 Forms 例外直接调用 `FormStanceMode.IsSelected`

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:111` `private const string FormsNamespace = "Spire1.Spire1Code.Forms";`
  - `:700-705` `IsFormsPower(PowerModel power)` 使用 `ns.Equals(FormsNamespace, ...) || ns.StartsWith(FormsNamespace + ".", ...)`
  - `:718` `if (IsFormsPower(power!) && FormStanceMode.IsSelected(target?.Player))` (Apply 例外)
  - `:789` `if (IsFormsPower(power!) && FormStanceMode.IsSelected(power!.Owner?.Player))` (ModifyAmount 例外)
- 触发条件: 拆分后 Forms 命名空间改为独立根 (例如 `Forms.Spire1Code...`) 或 Forms mod 未加载
- 契约或宣称: powers 门控必须只对 Forms 形态局放行, 普通局仍拦截
- 当前控制流: 先按硬编码字符串判断是否 Forms power, 再调用 Forms 类型判断本局是否选中形态; 拆包后两者都会误判/抛 `TypeLoadException`
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs' -Pattern 'FormsNamespace|IsFormsPower|FormStanceMode'`
- 最小修复范围: 用独立契约 (例如 `IFormStancePower` 接口或注册标记) 替代命名空间字符串; `FormStanceMode.IsSelected` 改为桥接口/反射查询, 避免 Spire1 编译期依赖 Forms
- 尚缺实机证据: 拆分后普通局与形态局的门控差异未实机验证

### C-06 [P1] `FormStanceModifier` 注册与菜单注入完全依赖 Spire1 的 Harmony 属性扫描 (MainFile Phase3)

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs:9-13` `[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]` + `TryBind()`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:164-167` 目标 `NCustomRunModifiersList.GetAllModifiers` (Spire1 之外的游戏 API)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:375-390` `CustomModifiersPostfix` 注入 `FormStanceModifier`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:247-274` Phase3 用 `typeof(MainFile).Assembly.GetTypes()` 扫描 `[HarmonyPatch]` 并统一安装; 扫描范围是 Spire1 程序集
- 触发条件: 把 Forms 拆成独立 mod 程序集后, 该补丁类不在 Spire1 程序集内, 不会被 MainFile 扫描; 独立 mod 必须自行安装
- 契约或宣称: 独立化后 Forms 必须能自注册 (或由宿主 mod 显式加载), 不能依赖 Spire1 的初始化器
- 当前控制流: Spire1 初始化时统一扫描程序集内所有 `[HarmonyPatch]` 类型; Forms 桥的补丁安装发生在 `ModelDb.Init` 之后 (由属性 patch 触发), 独立 mod 缺少等价入口
- 可复现命令:
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs' -Pattern 'HarmonyPatch|CreateClassProcessor'`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'HarmonyPatch|TryBind|GetAllModifiers'`
- 最小修复范围: 独立 mod 提供自己的 `[ModInitializer]` 与 Harmony 安装路径 (含 `ModelDb.Init` 后绑定时机), 或在 Spire1 侧保留显式加载契约
- 尚缺实机证据: 独立 mod 在真实加载器下的初始化顺序与补丁安装结果未验证
### C-07 [P2] 资源与本地化全部挂在 Spire1 的 `res://Spire1/...` 与 `SPIRE1-...` 键下

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:42-44` `"calm_power.png" / "wrath_power.png" / "divinity_power.png"`
  - `:46-47` `CustomPackedIconPath => "res://Spire1/images/powers/" + IconFile` 与 `CustomBigIconPath => "res://Spire1/images/powers/big/" + IconFile`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs:10` `IconPath => "res://Spire1/images/powers/divinity_power.png"`
  - 实际资源文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1\images\powers\{calm,wrath,divinity}_power.png` 与 `...\powers\big\{calm,wrath,divinity}_power.png` (6 个文件)
  - 本地化 JSON: `G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json:173-199`, `...\zhs\powers.json:173-199` (9 个 Forms 相关键, 每个 3 行)
  - 修正器本地化: `...\eng\modifiers.json:2-3`, `...\zhs\modifiers.json:2-3` (`SPIRE1-FORM_STANCE_MODIFIER.*`)
  - 代码内嵌 `PowerLoc` 回退: 6 个 Forms 效果文件各 1 处 (`CelestialFormPower.cs:33`, `DemonFormPower.cs:54`, `EchoFormEffectPower.cs:32`, `ReaperFormEffectPower.cs:30`, `SerpentFormPower.cs:79`, `VoidFormEffectPower.cs:43`)
- 触发条件: 拆分 Forms 到独立 mod (独立 `res://` 根与独立 localization 目录) 时
- 契约或宣称: 独立化后 Forms 必须自持资源与本地化, 不能读取 Spire1.pck
- 当前控制流: 图标按 `res://Spire1/...` 硬编码; 本地化由 Spire1 manifest/SimpleLoc 注册 (`MainFile.cs:134` `SimpleLoc.EnableSimpleLoc(ModId)`, `ModId = "Spire1"`); 拆包后资源缺失, 图标回退/报错行为未验证
- 可复现命令:
  - `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1\images\powers' -Recurse -File | Where-Object { $_.Name -match 'calm|wrath|divinity' }`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json','G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\zhs\powers.json' -Pattern 'VOID_FORM_EFFECT_POWER|SERPENT_FORM_POWER|DEMON_FORM_POWER|REAPER_FORM_EFFECT_POWER|ECHO_FORM_EFFECT_POWER|CELESTIAL_FORM_POWER'`
- 最小修复范围: 独立 mod 复制/重命名 6 个图标到自己的 `res://` 根; 迁移 27+6 条本地化键 (eng/zhs) 或改为独立键名; 修改 `CustomPackedIconPath`/`CustomBigIconPath`/`IconPath` 的根
- 尚缺实机证据: 独立 mod 下图标与中英文文案的实际显示未验证
### C-08 [P2] 冒烟验证器 `FormNativeSmokeRunner` 与 Spire1 同程序集, 直接引用 Forms

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:24` `using Spire1.Spire1Code.Forms;`
  - `:321` `FormStanceWatcherBridge.TryBind() && FormStanceWatcherBridge.IsAvailable`
  - `:500` 与 `:1996` `ModelDb.Modifier<FormStanceModifier>().ToMutable()`
  - `:1682` `FormStanceWatcherBridge.CurrentKind(player)`
  - `:1689-1693` `OfType<WatcherFormStancePower>()`
  - `:1709-1710` `FormStanceMode.IsSelected(player)` / `FormStanceWatcherBridge.IsAvailable`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs:7-13` 在 `NGame._Ready` postfix 中调用
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:55-95` 未将该文件列为 held-back; 它是生产 DLL 的一部分
- 触发条件: Forms 拆分后, 这些直接引用编译失败; 或者留在 Spire1 里的 runner 找不到 Forms 类型
- 契约或宣称: 独立化后测试载体应与被测模块解耦, 或至少通过契约/反射访问
- 当前控制流: 该 runner 仅由 `--form-native-smoke*` 命令行触发, 但代码路径与 Forms 类型在同一编译单元; 拆包后 Spire1 侧 runner 无法编译
- 可复现命令:
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs' -Pattern 'FormStance|Forms|WatcherFormStancePower'`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' -Pattern 'FormNativeSmoke'` (无匹配 = 未排除)
- 最小修复范围: runner 随 Forms 一起迁移到独立 mod; 或改为按契约/反射访问 Forms 入口, 并保持 Spire1 侧无 Forms 类型引用
- 尚缺实机证据: 拆分后 runner 是否仍能驱动真实战斗未验证

### C-09 [P2] Watcher 晚加载重试: `TryBind` 以 `_attempted` 一次性短路, 绑定失败后本进程不再重试

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:34` `private static bool _attempted;`
  - `:58-62` `TryBind()` 首行 `if (_attempted) return IsAvailable; _attempted = true;`
  - `:80-103` 失败路径清空 `_binding`/`BoundTargets`, 记录 `UnavailableReason`, 返回 false; 不重置 `_attempted`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs:8-13` 唯一自动调用点: `ModelDb.Init` postfix (`Priority.Last`)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:321` 另一次调用, 但同样被 `_attempted` 短路
- 触发条件: `ModelDb.Init` 时刻 Watcher 程序集尚未加载, 或 Watcher 版本不匹配导致 `ValidateBinding` 抛错; 之后 Watcher 才加载 (晚加载)
- 契约或宣称: 交接目标要求 "Watcher 晚加载可重试"
- 当前控制流: 第一次失败后 `_attempted=true` 永久保留; 后续任何 `TryBind` 都直接返回 `false`, 不会再尝试 `ValidateBinding` 或安装 Harmony 补丁
- 可复现命令:
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_attempted'`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs','G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs' -Pattern 'TryBind'`
- 最小修复范围: 区分 "已成功绑定" 与 "已尝试" 两个状态; 失败路径允许后续重试 (例如 AssemblyLoad 事件、菜单进入前钩子或有限次重试), 并保持重复成功绑定幂等
- 尚缺实机证据: Watcher 晚加载时本进程的实际加载顺序与重试行为未验证 (r15 矩阵只覆盖同批启动)
### C-10 [P1] 普通局卡牌 `Cards\DemonForm.cs` 与形态子系统共用 `Forms\DemonFormPower`

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\DemonForm.cs:14` `new PowerVar<DemonFormPower>(2)`
  - `:22` `CommonActions.ApplySelf<DemonFormPower>(choiceContext, this)`
  - `:25` `DynamicVars.Power<DemonFormPower>().UpgradeValueBy(1m)`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:26` `public sealed class DemonFormPower : CustomPowerModel` (定义于 Forms 目录)
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:52-53` 注释明确: 普通局经 `CommonActions.ApplySelf<DemonFormPower>` 授予
- 触发条件: 把 Forms 拆成独立 mod 后, 普通局 (无 FormStanceModifier) 的 StS1 恶魔形态卡仍需要该 power 类型
- 契约或宣称: 独立化后普通局行为不能因 Forms mod 缺失而回归; 需要明确该 power 的归属 (Spire1 保留 vs Forms 提供)
- 当前控制流: 一个类型同时服务两条路径 -- 普通局由卡牌直接 Apply, 形态局由 `WatcherFormStancePower.CreateEffects` 通过 `ModelDb.Power<DemonFormPower>()` 实例化; 拆包必须二选一或做接口化
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\DemonForm.cs' -Pattern 'DemonFormPower'`
- 最小修复范围: 将普通局所需部分保留在 Spire1 (或复制为独立类型), Forms 侧只保留形态专用逻辑; 同时更新本地化键与 ModelDb 注册归属
- 尚缺实机证据: 拆包后普通局恶魔形态卡的授予/升级/力量行为未验证

## 完整直接依赖清单 (区分可保留 / 必须迁移或反射化)

### A. 可保留: BaseLib / 游戏 API (Forms 对它们的引用不构成 Spire1 耦合)

- BaseLib: `BaseLib.Abstracts.CustomPowerModel` (7 个 Forms power/modifier), `CustomModifierModel` (`FormStanceModifier.cs:8`), `PowerLoc` (6 个效果文件); BaseLib 已是 Spire1 声明的唯一依赖 (`mod\Spire1.json:10-15`)
- 游戏 API: `MegaCrit.Sts2.Core.*` 的 Combat/Commands/Entities/Cards/Creatures/Players/Powers/Models/Rooms/ValueProps/GameActions.Multiplayer/Logging/Nodes.Screens.MainMenu/Runs/Combat.History
- HarmonyLib: 全部 `[HarmonyPatch]` 与 IL transpiler 目标 (游戏/BaseLib 类型, 不含 Spire1 业务类型)
- 结论: 这些引用在独立 mod 中保留即可, 只需复制相应的 csproj Reference/PackageReference

### B. 必须迁移或反射化的 Spire1 依赖 (Forms 目录内直接引用)

- `Spire1.Spire1Code.Powers.StancePower` (基类, `WatcherFormStancePower.cs:20`)
- `Spire1.Spire1Code.Powers.CalmPower / WrathPower / DivinityPower` (`FormStanceMode.cs:40,42,44`; `FormStanceWatcherBridge.cs:279`; `FormStanceCmd.cs:17,20,23`)
- `Spire1.Spire1Code.Extensions.StanceCmd` (`FormStanceCmd.cs:17,20,23,26`; `WatcherFormStancePower.cs:161`; `FormStanceWatcherBridge.cs:372`)
- `Spire1.Spire1Code.Extensions.IOnStanceChanged` (由 `StanceCmd.Dispatch` 消费者列表间接使用)
- `Spire1.Spire1Code.Config.Spire1Config` (Forms 目录内无直接引用, 但 `StanceCmd`/`Spire1PowersGate` 有; 拆分后需由 Spire1 侧保留)
- 命名空间字符串 `"Spire1.Spire1Code.Forms"` (`Spire1PowersGatePatch.cs:111`; 拆分后必须改为独立标识)

### C. 必须迁移或反射化的 Spire1 依赖 (Spire1 目录内直接引用 Forms)

- `Spire1.Spire1Code.Forms.FormStanceMode` (`StanceCmd.cs:22,24,46,48,102`; `Spire1PowersGatePatch.cs:50,718,789`)
- `Spire1.Spire1Code.Forms.FormStanceWatcherBridge` (`StanceCmd.cs:26,51,104`; `FormNativeSmokeRunner.cs:321,330,445,1193,1682,1710,1947`)
- `Spire1.Spire1Code.Forms.FormStanceKind` (`StanceCmd.cs:24,48`; `FormNativeSmokeRunner.cs:1264,1274,1279,1284,1289,1519`)
- `Spire1.Spire1Code.Forms.WatcherFormStancePower` (`StanceCmd.cs:54`; `FormNativeSmokeRunner.cs:1689,1693`)
- `Spire1.Spire1Code.Forms.FormStanceModifier` (`FormNativeSmokeRunner.cs:500,1996`; `Spire1PowersGatePatch.cs:50` 注释; 桥接内部注入)
- `Spire1.Spire1Code.Forms.{VoidSerpentStancePower, DemonReaperStancePower, EchoCelestialStancePower, VoidFormEffectPower, SerpentFormPower, DemonFormPower, ReaperFormEffectPower, EchoFormEffectPower, CelestialFormPower}` (`FormNativeSmokeRunner.cs:1275-1292` 的 typeof 断言)

### D. 注册与调用依赖

- 属性扫描安装: `MainFile.cs:247-274` 扫描 Spire1 程序集全部 `[HarmonyPatch]`; Forms 补丁依赖该扫描
- 绑定时机: `FormStanceModePatch.cs:8-13` (`ModelDb.Init` postfix) -> `FormStanceWatcherBridge.TryBind()`
- 菜单注入: `FormStanceWatcherBridge.cs:164-167` 目标 `NCustomRunModifiersList.GetAllModifiers`; `:375-390` 注入修正器
- 运行时门控: `Spire1PowersGate` 对 Forms 命名空间 power 的 Apply/ModifyAmount 例外 (`Spire1PowersGatePatch.cs:718,789`)
- 冒烟载体: `FormNativeSmokePatch.cs:7-13` -> `FormNativeSmokeRunner.TryStart`
- 游戏 API 契约: Watcher 0.9.28 程序集名 `"Watcher"`, 类型 `WatcherMod.{Calm,Wrath,Divinity,WatcherCombatHelper}` (`FormStanceWatcherBridge.cs:108-136`)

### E. 资源与本地化依赖

- 图标: `res://Spire1/images/powers/{calm,wrath,divinity}_power.png` 与 `.../big/...` (6 文件)
- 本地化键: `SPIRE1-{VOID_SERPENT_STANCE_POWER,DEMON_REAPER_STANCE_POWER,ECHO_CELESTIAL_STANCE_POWER,VOID_FORM_EFFECT_POWER,SERPENT_FORM_POWER,DEMON_FORM_POWER,REAPER_FORM_EFFECT_POWER,ECHO_FORM_EFFECT_POWER,CELESTIAL_FORM_POWER}.*` (eng+zhs, 27+6 行)
- 内嵌回退: 6 个 `PowerLoc` 构造 (Forms 效果文件)
### C-11 [P2] `StanceCmd` 是普通局与形态局的共享入口, 拆分后需要保留稳定的双路径契约

- 绝对路径与行号:
  - 普通局调用者: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\LikeWaterPower.cs:35` `StanceCmd.IsIn<CalmPower>`; `SimmeringFuryPower.cs:40` `StanceCmd.Enter<WrathPower>`; `DevotionPower.cs:41` `StanceCmd.GainMantra`; `MantraPower.cs:23` 注释; `MentalFortressPower.cs:13` 注释
  - 形态局调用者: `Forms\FormStanceCmd.cs:17,20,23,26`; `Forms\WatcherFormStancePower.cs:161`; `Forms\FormStanceWatcherBridge.cs:372`
  - 共享入口实现: `Extensions\StanceCmd.cs:22-54,102-104,185-215`
- 触发条件: 拆分后 Spire1 侧 `StanceCmd` 不再能直接引用 `FormStanceMode`/`FormStanceWatcherBridge`
- 契约或宣称: 普通局行为不变; 形态局由独立 mod 接管进入/退出/通知
- 当前控制流: `StanceCmd` 先判断 `FormStanceMode.IsEnabled(player)`, 命中则整段委托给 Forms 桥; 未命中则走普通 `PowerCmd.Apply/Remove` 与 `Dispatch`
- 可复现命令: `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' -Recurse -File -Filter *.cs | Select-String -Pattern 'StanceCmd\.'`
- 最小修复范围: 把 `StanceCmd` 中的 Forms 分支改为接口/委托 (例如 `IStanceFormBridge` 或反射), 由独立 mod 在加载时注册; 普通局路径保持不变
- 尚缺实机证据: 拆分后 LikeWater/SimmeringFury/Devotion/MentalFortress 与形态局的交互未验证

### C-12 [P2] `FormStanceCmd` 当前在仓库内无调用者, 拆分时属于死入口或外部契约

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:14-26` 定义 4 个公开方法
  - 全仓扫描 `FormStanceCmd.` 无匹配 (除定义文件本身)
- 触发条件: 拆分 Forms 时, 该 API 是否被外部 mod/脚本依赖未知
- 契约或宣称: 交接文档把它列为 "可玩入口" 之一; 但仓库内没有调用点
- 当前控制流: 静态类仅供外部调用; 拆分后若保留在独立 mod, 需要保持命名空间/签名或提供新入口
- 可复现命令: `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code' -Recurse -File -Filter *.cs | Select-String -Pattern 'FormStanceCmd\.'`
- 最小修复范围: 拆分前确认外部契约; 若无人调用, 可随 Forms 迁移; 若被依赖, 在独立 mod 中保持同名 API
- 尚缺实机证据: 无 (本项为静态事实, 是否被外部使用未知)

### C-13 [P2] 同进程重载/退出幂等的静态状态集中在 Forms 静态字段, 拆分后需一并迁移

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:30` `HarmonyId = "Spire1.FormStanceMode.Watcher"`
  - `:33-35` `_binding`, `_attempted`, `_nativeNotification` 三个静态字段
  - `:37-39` `IsAvailable`, `UnavailableReason`, `BoundTargets` 静态属性
  - `:58-104` `TryBind` 的安装/回滚流程; `:225-233` `VerifyInstalled` 以 `Harmony.GetPatchInfo` 收束
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:291-299` `Phase3Lock`/`_phase3Completed`/`_phase3ScanCompleted`/`_phase3Harmony` 静态状态
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs:14-16` `AfterRunCreated`/`AfterRunLoaded` 都调用 `RequireAvailable()`
- 触发条件: 同进程重载/退出后重新进入菜单或重新加载 mod; 拆分后 Harmony id、静态 binding、回滚状态需由独立 mod 自持
- 契约或宣称: 交接目标要求 "同进程重加载/退出幂等"
- 当前控制流: 绑定只尝试一次 (`_attempted`), 成功绑定后重复进入 `TryBind` 返回已可用; 失败后不重试 (见 C-09); 退出路径没有显式 `UnpatchAll`/静态重置
- 可复现命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_binding|_attempted|_nativeNotification|HarmonyId'`
- 最小修复范围: 独立 mod 自持 Harmony id/静态状态与卸载路径; 明确重载时是保留绑定还是先 unpatch 再重建; 避免与 Spire1 的 `Phase3Lock` 状态混用
- 尚缺实机证据: 同进程重载/退出的真实行为未验证 (本项只证明静态状态位置)
### C-14 [P1] 缺 Watcher 时的 fail-closed 静态面: 菜单注入被抑制, 但已选存档会在运行期抛异常

- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:20-22` `IsSelected` 读取本局 Modifiers
  - `:24-29` `IsEnabled` 先 `IsSelected`, 再 `RequireAvailable()`; `:31-36` 不可用时抛 `InvalidOperationException`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:375-390` `CustomModifiersPostfix`: `IsAvailable` 为假时不注入修正器, 且已存在的修正器会被 `continue` 丢弃
  - `:246-273` `CurrentKind`/`Enter`/`Exit` 都先 `RequireAvailable()` 或 `IsEnabled`; 不可用时抛异常
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs:14-16` `AfterRunCreated`/`AfterRunLoaded` 直接 `RequireAvailable()`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:57-104` `TryBind` 失败时 `_binding=null` + `UnavailableReason`
- 触发条件: Watcher 未加载或版本不匹配, 且玩家加载了已选形态修正的旧存档/继续对局
- 契约或宣称: 交接目标要求 "缺 Watcher 时 fail-closed"
- 当前控制流: 菜单层已 fail-closed (不显示修正器); 但旧存档 `AfterRunLoaded` 会抛异常, `StanceCmd`/桥入口也会抛; 异常传播路径 (是否被引擎捕获、是否阻断加载) 未在源码中封闭
- 可复现命令:
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs' -Pattern 'RequireAvailable|IsEnabled|IsSelected'`
  - `Select-String -Path 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'IsAvailable|RequireAvailable'`
- 最小修复范围: 明确旧存档在缺 Watcher 时的用户可见行为 (禁用修正器 + 提示/降级) 与异常边界; 保证不会把异常泄漏到引擎其它 mod 路径
- 尚缺实机证据: 缺 Watcher 时加载已选形态存档的真实行为未验证 (r15 矩阵只验证了缺 Watcher 可启动)
## 进行中

- 扫描重点文件: `Extensions\StanceCmd.cs`, `Patches\Spire1PowersGatePatch.cs`, `Run\FormNativeSmokeRunner.cs`, `MainFile.cs`, localization, 资源路径
- 汇总完整直接 Assembly/type/namespace/资源/本地化/注册调用依赖清单

## 未知

- 独立工程在缺 Watcher 时是否 fail-closed (仅静态推断, 待证据)
- Watcher 晚加载重试与同进程重加载幂等的实际行为
- 其他会话 dirty 修改是否会被拆包动作覆盖