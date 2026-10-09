# Partial mod dependency audit - 2026-10-02

## 已确认

## 进行中

## 未知


### 审查启动记录（2026-10-02）
- 已读取请求文件：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-dependency-audit-request-20261002.md`。
- 项目文本检查器 `G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs --file <request>` 返回 `agent text accepted`。
- 当前限制已锁定：只读证据审查；唯一写入目标为本报告；不修改产品代码、不构建、不测试、不部署、不运行游戏、不写 Steam install、shared `mod_configs` 或 C:。
- 当前阶段：读取工作区/项目规范并建立源码证据索引。

### 检查面 1：源码声明依赖与 Watcher/AutoAnthony AssemblyRef 风险（静态）
- priority: P1（已确认的安全面，仍需区分当前二进制与源码时间点）。
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:32-38,47-52`; `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-15`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:24-27,108-126`。
- trigger: 在缺少 Watcher 或 AutoAnthony 的部分 mod 启动中，若 Spire1 的元数据含外部程序集硬引用，CLR 可能在类型加载/JIT 时失败；本面检查当前源码是否产生该硬引用。
- current control flow: csproj 明确删除旧 AutoAnthony `<Reference>`/条件编译块，仅保留 BaseLib 包引用；manifest 仅声明 `BaseLib >= 3.4.5`。Watcher 桥只按程序集名 `Watcher` 扫描，并在 `ValidateBinding` 中通过字符串类型名、`MethodInfo` 和委托创建解析；源码中 Forms 目录没有 `using Watcher...` 或外部 Watcher 类型签名。
- contract: Spire1 可在 Watcher/AutoAnthony 缺失时保留可加载的 BaseLib 单依赖；可选桥接不得转化为 AssemblyRef 或直接类型引用。
- reproducible command: `rg -n --glob '*.cs' '(using\s+Watcher|WatcherMod\.|Assembly.Load|GetAssemblies|GetType\(|FormStanceWatcherBridge)' 'G:\\omp works\\Sts\\sts2-spire1\\mod'`; `Get-Content -LiteralPath 'G:\\omp works\\Sts\\sts2-spire1\\mod\\Spire1.csproj'`; `Get-Content -LiteralPath 'G:\\omp works\\Sts\\sts2-spire1\\mod\\Spire1.json'`。
- minimum fix: 当前源码层无需为 Watcher/AutoAnthony 增加声明；若后续改回强类型引用，必须同时增加真实 manifest 依赖或恢复纯反射设计，不能只改 manifest。
- missing runtime evidence: 本轮未构建、未加载缺 Watcher/AutoAnthony 的实例、未进行当前源码对应 DLL 的 AssemblyRef 表复核；历史门禁只能作为历史静态证据，不能证明当前源码版本实机通过。

### 检查面 2: BaseLib 声明依赖与缺失时 fail-closed（静态已确认）
- priority: P1.
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:21-29,46-50`; `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-14`; `G:\omp works\Sts\sts2-spire1\research\BaseLib-StS2\BaseLib.json:6-10`; `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModManager.cs:719-784`.
- trigger: 启动时缺少 BaseLib, BaseLib 未成功加载, 或 BaseLib 低于 `3.4.5`.
- current control flow: Spire1 的编译期 `BaseLib` 用法来自 package/reference 体系, manifest 明确声明 `BaseLib` 最低版本 `3.4.5`. ModManager 在 DLL 加载前检查声明依赖;依赖不存在或状态不是 `Loaded` 时记录 `MISSING_DEPENDENCY` 并把 Spire1 状态置为 `Failed`,不会进入 `LoadFromAssemblyPath` 和 initializer 路径. 已读取的 BaseLib manifest 声明 `dependencies: []`,未发现 BaseLib 还要求本审查范围内其它 mod.
- contract: BaseLib 是 Spire1 的必需前置项;缺失时应在 manifest gate 失败,而不是让 Spire1 以半初始化状态运行. 当前源码声明与该契约一致.
- reproducible command: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json'`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\BaseLib-StS2\BaseLib.json'`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModManager.cs' | Select-Object -Index (718..783)`.
- minimum fix: 本面无需改产品代码. 若升级 BaseLib,必须同步 package 版本和 `Spire1.json` 的 `min_version`;若将新必需 mod 写进代码,必须同时增加真实 manifest dependency,不能只依靠反射或文档.
- missing runtime evidence: 本轮未启动缺 BaseLib 的部分 mod,未验证游戏实际日志字符串和当前 DLL 的加载状态;本结论是 manifest 和加载器源码证据,不是实机通过.

### 增量收束说明
以下新增条目使用 `[已确认]`、`[进行中]`、`[未知]` 标记归类,保留前文的增量记录,不改写已有内容.

### [已确认] 检查面 3: Watcher 反射桥的严格绑定与缺失时 fail-closed
- priority: P1.
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:24-27,57-103,106-190,193-232,375-390,401-479`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:17-35`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs:7-21`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs:6-13`; `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Helpers\OneTimeInitialization.cs:59-61,64-83`.
- trigger: Watcher 缺失,加载了重复的 `Watcher` 程序集,目标类型/方法签名变化,async state machine 变化,Divinity 能量 IL 形状变化,或 Harmony 安装后核验失败.
- current control flow: `ModelDb.Init` 的 Harmony postfix 调用 `TryBind`. ModManager 初始化先完成 mod 列表的逐项 `TryLoadMod`,随后才进入 `OneTimeInitialization.ExecuteEssential` 的 `ModelDb.Init`,因此当前生命周期下所有已成功加载的 mod DLL 应已在绑定检查前进入 AppDomain. `TryBind` 只在全部 `ValidateBinding`、Harmony patch 和 `VerifyInstalled` 成功后发布 `_binding`;任何异常都清空 capability,按自己的 Harmony owner 反向 unpatch,记录不可用原因. `CustomModifiersPostfix` 只在 bridge available 时返回 Forms modifier;已选择该 modifier 的新建、载入和战斗开始路径均调用 `RequireAvailable` 并显式抛错.
- contract: Watcher 是 Forms 功能的可选运行时能力,不是 Spire1 的编译期前置项. 缺失或不兼容时普通 Spire1 仍应可加载,Forms modifier 应隐藏;已有带 modifier 的存档不得按错误规则继续运行,而应 fail-closed. 当前源码控制流满足该契约,并且严格检查目标签名、可见性、`ChangeStance<T>` 的 `PowerModel` 约束、async `MoveNext`,关键 IL 字面值和 Harmony owner.
- reproducible command: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern '_attempted|ValidateBinding|RequireMethod|RequireMoveNext|VerifyInstalled|ValidateDivinityEnergySite|ValidateEndTurnSites'`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Helpers\OneTimeInitialization.cs' | Select-Object -Index (58..82)`.
- minimum fix: 本面无需改产品代码. 若要支持新的 Watcher ABI,应新增显式签名和 IL 契约分支并保留全量预检、发布后核验和按 owner 回滚;不得把 Watcher 类型直接写入 Spire1 公共签名或只放宽反射匹配.
- missing runtime evidence: 本轮未运行无 Watcher、错误签名 Watcher、重复同名程序集或旧存档加载场景;未把源码结论称为实机通过.

### [已确认] 检查面 4: AutoAnthony 可选桥的 Watcher 后加载窗口
- priority: P1（仅影响 AutoAnthony 与 Watcher 的交叉能力,不构成 Spire1 加载硬依赖）.
- absolute path and line: `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModManager.cs:123-127`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:161-165`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:20-49,52-70`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:199-229,334-381`.
- trigger: 用户 mod 顺序让 AutoAnthony 先于 Spire1,而 Watcher 在 Spire1 之后才被 `ModManager` 逐项加载. 这条顺序合法,因为当前 manifest 未声明 AutoAnthony 或 Watcher 依赖边.
- current control flow: Spire1 initializer 调 `TryApplyBridge`. AutoAnthony 已在 AppDomain 时,`Apply` 直接执行. `PatchThirdPartyEntries` 在此刻找不到 `Watcher` 时返回 `0`;但 `PatchFrom` 和 `PatchPoolsAndDecks` 的成功计数会使 `patched > 0`,从而在 `AutoAnthonyCompatBridge:227` 设置 `_applied = true` 并在 `AutoAnthonyLoadHook:37-40` 直接返回,不会注册 `AssemblyLoad` 兜底. 后续 Watcher 加载事件不会触发重新尝试,因为现有 hook 只在 AutoAnthony 初次缺席时订阅,且 `OnAssemblyLoad` 只匹配程序集名 `AutoAnthony`.
- contract: AutoAnthony 对 Spire1 自有角色的桥接可以在 AutoAnthony 单独存在时成功;Watcher 兼容映射应在 Watcher 后加载时仍可补挂,或应明确声明真实加载顺序. 当前实现的 `_applied` 是整体状态,把核心 AutoAnthony 补丁和可选 Watcher 能力错误地绑定为一次性成功.
- reproducible command: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModManager.cs' | Select-Object -Index (122..126)`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs' | Select-Object -Index (19..69)`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs' | Select-Object -Index (198..228),(333..380)`.
- minimum fix: 将 AutoAnthony 核心补丁与 Watcher third-party 补丁拆成独立 capability 状态;在 Watcher 或 `AutoAnthonyWatcher` 程序集加载事件到达后只重试未完成的第三-party 组,并保持每组幂等和 Harmony owner 核验. 不要把可选 Watcher 直接加入 Spire1 manifest 依赖来掩盖该时序问题.
- missing runtime evidence: 本轮未用隔离启动复现 `AutoAnthony -> Spire1 -> Watcher` 与 `AutoAnthony -> Spire1` 两种顺序,未观察 third-party pool Harmony owner 或实际卡池. 这是源码控制流确认的顺序缺口,不是运行验收.

### [已确认] 检查面 5: 历史 partial staging 存在嵌套目录导致 manifest 不可见的风险
- priority: P1（历史夹具已出现,可直接造成 `Loaded 2 mods (4 total)` 一类假失败）.
- absolute path and line: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-form-smoke-r10.ps1:81-88,99-106`; `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-final-20261001\staged-mods\BaseLib\BaseLib\BaseLib.json`; `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-final-20261001\staged-mods\Watcher\Watcher\Watcher.json`; `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staging-latest.json`.
- trigger: staging 先创建 `stage\BaseLib`, `stage\Watcher`, `stage\Spire1`,随后把源 mod 目录的所有子项复制到对应 stage 目录;如果源目录本身已经带同名子目录,再把 stage 子目录复制到目标 `mods`,就会形成 `BaseLib\BaseLib` 或 `Watcher\Watcher`.
- current control flow: 历史脚本的复制方向由 `run-form-smoke-r10.ps1:81-88` 和 `:105-106` 固定;历史产物实际留下 `staged-mods\BaseLib\BaseLib\BaseLib.json` 与 `staged-mods\Watcher\Watcher\Watcher.json`,说明 manifest 不在 ModManager 预期的 `mods\<id>\<id>.json` 顶层. 较新的 `staging-latest.json` 记录了扁平目标 `mods\BaseLib\BaseLib.json`, `mods\Watcher\Watcher.json`, `mods\Spire1\Spire1.json`,表明至少该次 staging 结果已被整理,但该 JSON 不是生成逻辑本身.
- contract: partial staging 必须把每个 mod 的 manifest、DLL 和 PCK 放在一个且仅一个 `mods\<manifest.id>\` 顶层目录;不得依靠目录名猜测 manifest 位置,不得把一个 mod 的目录再次嵌套进同名目录.
- reproducible command: `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-form-smoke-r10.ps1' | Select-Object -Index (80..105)`; `Get-ChildItem -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-final-20261001\staged-mods' -Recurse -Filter '*.json' | Select-Object -ExpandProperty FullName`; `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staging-latest.json'`.
- minimum fix: staging 前对每个源目录先解析 manifest 的 `id`,再把 manifest 所在的直接文件集合复制到唯一目标 `stage\<id>\`;复制完成后强制断言 `stage\<id>\<id>.json` 存在且 `stage\<id>\<id>\` 不存在,并让 launcher 在发现嵌套或缺 manifest 时 fail-fast.
- missing runtime evidence: 本轮未运行历史脚本,未修改 staging,未重新启动游戏;当前 r18/r19 记录显示扁平目标,但没有把当前生成器的源代码和 partial mod 缺项矩阵全部核对,因此不能宣称所有后续 staging 已无回归.

### [进行中] 检查面 6: 当前已有 Release DLL 的目标外部类型引用静态抽查
- priority: P1.
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`（文件时间 `2026-10-02T21:02:39+08:00`, SHA256 `5BC0BD72728BA791539ED4E1A5D6D1DD9E48833A7516E02CA857D852219519B9`）; 对应源码声明位置仍为 `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:32-38,46-50`.
- trigger: 需要确认当前已有 payload 没有把 Watcher 或 AutoAnthony 的类型签名重新编译进 Spire1,否则源码层的纯反射设计可能被旧构建产物或其它文件回归抵消.
- current control flow: 使用现有 `ilspycmd 9.1.0.7988 --ilcode` 只读反编译该 DLL,针对 IL 外部类型签名搜索 `\[(Watcher|AutoAnthony)\]`;本次没有命中. IL 中出现的 `WatcherMod.Watcher`、`AutoAnthonyLoadHook` 和相关文本均是字符串或 Spire1 自有类型调用,不是命名为 `[Watcher]` 或 `[AutoAnthony]` 的外部类型签名.
- contract: 当前 payload 至少通过了目标外部类型签名的静态抽查;完整 AssemblyRef 表仍需独立元数据枚举,不能把该抽查升级为完整二进制门禁.
- reproducible command: `$dll='G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll'; & 'C:\Users\o_Obl\.dotnet\tools\ilspycmd.exe' --disable-updatecheck --ilcode $dll 2>$null | Select-String -Pattern '\[(Watcher|AutoAnthony)\]'`.
- minimum fix: 若完整 AssemblyRef 检查命中 `Watcher` 或 `AutoAnthony`,先定位引入该引用的源码类型签名或构建项,恢复纯反射并从 payload 中移除引用;不能只修改 manifest.
- missing runtime evidence: 未构建,未重新生成 DLL,未完成完整 AssemblyRef 表枚举,未进行缺 Watcher/AutoAnthony 的实际加载. 该条保持进行中.

- 触发条件校正: 上述窗口不只限于 `AutoAnthony -> Spire1 -> Watcher`;任何使 `AutoAnthonyCompatBridge.Apply` 在 `Watcher` 程序集进入 AppDomain 之前完成的顺序都命中,例如 `Spire1 -> AutoAnthony -> Watcher`. 关键不变量是 Watcher 后加载,而不是两个前置 mod 的绝对相对顺序.

### [未知] 检查面 7: 本轮禁止运行导致的 partial mod 交叉启动验收缺口
- priority: P1.
- absolute path and line: 请求边界 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-dependency-audit-request-20261002.md`; 相关静态入口 `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-14`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs:6-13`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:42-70`; 隔离证据索引 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staging-latest.json`.
- trigger: 需要对 BaseLib 缺失、Watcher 缺失、AutoAnthony 缺失、Watcher ABI 不兼容,以及 Watcher 晚于 AutoAnthony 桥加载的部分组合逐一观察实际 ModManager 状态和日志.
- current control flow: 静态上可推导 BaseLib 缺失应在 manifest gate 失败,Watcher 缺失应保留普通 Spire1 加载并隐藏 Forms modifier,AutoAnthony 缺失应跳过可选桥;但本轮硬性禁止构建、测试、启动游戏和修改 staging,因此没有取得这些组合的当前运行证据. 已有 r17/r18/r19 记录只覆盖包含 `BaseLib`,`Watcher`,`Spire1` 的隔离路径,且它们不是本轮新运行.
- contract: 只有在每个部分 mod 组合的真实加载状态、manifest 解析结果、initializer 结果和关键日志均与静态契约一致后,才能关闭该交叉启动门禁. 静态推理、历史运行记录和扁平 staging JSON 不能互相替代.
- reproducible command: `Get-ChildItem -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad' -Recurse -File -Filter 'run*.json' | Select-String -Pattern 'mods|Loaded|exitCode|cleanupCompleted'`; `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staging-latest.json'`.
- minimum fix: 在获得单独运行授权后,建立不写 Steam/shared config 的 G: 隔离矩阵,每个 case 只改变 mod 目录和 manifest 集合,并记录 ModManager 的 loaded/failed 状态;在此之前不宣称 partial mod 交叉启动已通过.
- missing runtime evidence: 缺 BaseLib,缺 Watcher,缺 AutoAnthony,错误 Watcher ABI,重复 Watcher Assembly,以及 Watcher 晚于 AutoAnthony bridge 的实际启动日志和结果全部缺失. 本轮没有运行游戏,没有构建,没有测试,没有部署.

### [已确认] 检查面 8: Harmony target discovery 不会把 Watcher 变成扫描期硬前置
- priority: P1.
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:113-159`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:161-179`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs:6-13`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs:474-476,592,698,814,926`.
- trigger: 缺少 Watcher 时,MainFile 的程序集级 Harmony 类型发现可能因为某个 Watcher target 无法解析而中止,进而连带丢失其它 Spire1 patch.
- current control flow: MainFile 逐类型扫描本程序集的 `HarmonyPatch` 属性,每个类型在独立 `try/catch` 内调用 `CreateClassProcessor(type).Patch`,失败只计数并继续. Forms 只由 `FormStanceModePatch` 进入该扫描,其静态 target 是引擎 `ModelDb.Init`;真正的 Watcher targets 由 `FormStanceWatcherBridge.TryBind` 在运行时发现后显式 `harmony.Patch`,不经过属性扫描. `Spire1PowersGatePatch` 的 target resolver 也只针对引擎 `PowerCmd`/相关方法,未形成 Watcher 类型签名.
- contract: Watcher 缺失或 ABI 不兼容时,普通 Spire1 patch 集合不得因一个可选桥失败而整体中断;动态桥应独立记录不可用并保持 fail-closed. 当前 MainFile 和 Forms target discovery 的边界符合该契约.
- reproducible command: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs' | Select-Object -Index (112..178)`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModePatch.cs'`; `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs' -Pattern 'WatcherMod\\.|using Watcher|AutoAnthony\\.'`.
- minimum fix: 本面无需改产品代码. 新增可选 mod 兼容层时继续使用显式能力边界和每类型隔离异常;不得把缺失可选程序集的 target 放进无条件属性扫描或共享的静态类型签名.
- missing runtime evidence: 未在缺 Watcher 的真实 ModManager 会话中测量普通 patch 的最终安装集合;本结论只来自源码控制流.
