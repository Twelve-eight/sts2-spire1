# 部分模组启动矩阵审查 — 2026-10-02

审查范围：`G:\omp works\Sts\sts2-spire1`
唯一报告路径：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-launch-matrix-audit-20261002.md`
限制：只读静态/已有证据审计；不修改产品代码，不构建，不测试，不部署，不启动游戏，不写 Steam install、shared `mod_configs` 或 `C:`。

## 已确认

### 1. 审查请求文本检查通过
- priority: P0（流程门禁）
- absolute path and line: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-launch-matrix-audit-request-20261002.md:1`
- trigger: 对本轮请求文本运行项目文本检查器。
- current control flow: `node G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-launch-matrix-audit-request-20261002.md` 返回 `agent text accepted`。
- contract: 请求文本满足项目允许的模型提示文字字符约束，可继续进行本轮只读审查。
- reproducible command: `node G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-launch-matrix-audit-request-20261002.md`
- minimum fix: 无；检查已通过。
- missing runtime evidence: 此项不需要运行时证据；它不是产品行为结论。

### 2. 指定 staging 快照含有不符合契约的嵌套目录（矩阵 5）
- priority: P1
- absolute path and line: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods\BaseLib\BaseLib\BaseLib.json`（目录层级证据；无文本行号）；`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods\Watcher\Watcher\Watcher.json`（目录层级证据；无文本行号）；对应清单 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staging-latest.json:1-45`。
- trigger: 要求 staging 只含声明的顶层 mod 文件夹，并且不得出现 `BaseLib\BaseLib` 或 `Watcher\Watcher` 这类重复嵌套。
- current control flow: 当前快照的 `staged-mods\BaseLib` 下面还有 `BaseLib\BaseLib.dll/.json/.pck`，`staged-mods\Watcher` 下面还有 `Watcher\Watcher.dll/.json/.pck`；只有 `staged-mods\Spire1` 是直接放置 `Spire1.dll/.json/.pck/.pdb`。这是实际目录树证据，不是把历史 `Loaded 2 mods` 当作当前证据。
- contract: loader 交付目录应为 `staged-mods\BaseLib\BaseLib.json`、`staged-mods\Watcher\Watcher.json`、`staged-mods\Spire1\Spire1.json` 及同层 payload；当前可读 staging 快照不满足该目录契约。既有最终报告 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-r17-final-20261002.md:38-41` 记载“没有嵌套 manifest”，与本次对指定 `staged-mods` 快照的直接目录读取不一致；该报告不能替代当前目录证据，且其所述隔离运行目标不是本快照目录本身。
- reproducible command: `$p='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods'; Get-ChildItem -LiteralPath $p -Directory -Force | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File -Force -Recurse | Select-Object FullName } | Sort-Object FullName`
- minimum fix: staging 生成器必须把 `BaseLib` 和 `Watcher` 的 manifest/DLL/PCK 展平到各自顶层 mod 文件夹，并在交付前拒绝任何 `BaseLib\BaseLib`、`Watcher\Watcher` 或更深重复层级；此处不修改产品代码。
- missing runtime evidence: 没有使用该 `staged-mods` 快照原样启动的当前真实运行证据；`stdout-final.log:11-17,279-290` 只证明另一个隔离目标被 loader 识别为 `BaseLib`、`Watcher`、`Spire1` 三个 mod，不能证明该嵌套快照原样可加载。

### 3. 源码依赖契约：BaseLib 是硬前置，Watcher 只被声明为可选反射能力
- priority: P0（依赖契约）
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-14`；`G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:46-48`；`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:2,64-70`；`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:25-26,108-111`。
- trigger: 对照 manifest、编译引用、initializer 使用点和 Watcher bridge 的查找方式，判断是否存在未声明硬前置。
- current control flow: `Spire1.json` 唯一声明 `BaseLib`，最低版本 `3.4.5`；csproj 直接引用 `Alchyr.Sts2.BaseLib`；`MainFile` initializer 调用 `SimpleLoc.EnableSimpleLoc`、`ModConfigRegistry.Register` 等 BaseLib API。Watcher bridge 不使用类型化 Watcher 引用，而是在 `AppDomain.CurrentDomain.GetAssemblies()` 中按程序集名查找；匹配数不是 1 时抛出，`TryBind` 的 catch 清空 binding、执行本 Harmony owner 的回滚并记录 bridge disabled。请求范围内未包含 bridge 调用者，因此这里只确认 bridge 自身的 fail-closed 分支，不外推完整调用时序。
- contract: `BaseLib` 必须在 `Spire1` 前置集合中且版本至少 `3.4.5`；`Watcher` 不应因 Spire1 源码/manifest 被默认为硬前置，缺失时只能使 Watcher bridge 能力不可用。若产品设计改为强制 Watcher，则必须同步修改 manifest，而不是依赖 staging 偶然带入。
- reproducible command: `$p='G:\omp works\Sts\sts2-spire1\mod\Spire1.json'; (Get-Content -LiteralPath $p -Raw | ConvertFrom-Json).dependencies | ConvertTo-Json; Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj','G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs','G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'BaseLib|Watcher|GetAssemblies|matches.Length|bridge disabled'`
- minimum fix: 当前源码未显示需要立即修改的未声明 Watcher 硬前置；发布/交叉启动流程应在 staging 阶段独立校验 `Spire1 -> BaseLib >= 3.4.5`，并把 Watcher 作为可选集合，不得由嵌套目录或旧文件隐式满足。
- missing runtime evidence: 尚无无 Watcher 进程日志证明 bridge 在实际启动中确实禁用且 Spire1 仍能完成 initializer；也尚无 AssemblyRef 元数据读取结果可替代源码注释，因此该项不宣称二进制层面的完整证明。

### 8. 仅有的真实正向运行证据是完整三 mod 集合，不覆盖部分矩阵
- priority: P1（证据边界）
- absolute path and line: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\run-final.json:1-34`；`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log:11-20,44-46,279-290`；三个场景 JSON 的 `status`/`formGateAfter` 记录分别见 `form-native-smoke-calm.json:3,88-106`、`form-native-smoke-wrath.json:3,106-124`、`form-native-smoke-divinity.json:3,88-106`。
- trigger: 区分“完整 BaseLib + Watcher + Spire1 真实运行通过”与请求要求的四个部分集合，避免把 full-run 证据外推为 partial matrix 通过。
- current control flow: stdout 先发现三个 manifest，排序为 `BaseLib -> Watcher -> Spire1`，加载三套 DLL/PCK，记录 `Loaded 3 mods (3 total)`，随后记录 `Spire1 Forms: Watcher bridge bound`；`run-final.json` 记录 `exitCode=0`、`sharedConfigSha256Unchanged=true`、`cleanupCompleted=true`。三份场景 JSON 的 `status=passed`、`formGateAfter.passed=true`，证明的是这组三 mod 的 Watcher 卡链路。
- contract: 该证据只能作为 full-set 正向对照；它不能证明矩阵 1 的无 Watcher路径、矩阵 2 的无 Spire1路径、矩阵 3 的 BaseLib-only路径、矩阵 4 的缺 BaseLib失败路径，也不能消除指定 `staged-mods` 的嵌套目录冲突。
- reproducible command: `Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log' -Pattern 'Found mod manifest|New sorting order|Loaded 3 mods|Watcher bridge bound'; Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\run-final.json' -Raw`
- minimum fix: 无需修改产品代码来重述证据；验收报告必须保留 full-set 标签，并在 partial staging 展平且隔离后分别补齐矩阵 1-4。
- missing runtime evidence: 矩阵 1-4 各自的当前 loader 顺序、退出码、Spire1/Watcher initializer 是否执行及缺依赖错误文本均缺失；不得使用旧 `Loaded 2 mods` staging 结果补足。

## 进行中

- 已完成：请求文本门禁、指定 staging 目录树面、源码依赖契约面、full-set 真实运行证据边界，以及矩阵 1-4 的证据缺口分类。
- 未执行：任何构建、测试、部署、游戏启动或其它代理运行时。

## 未知

### 4. 矩阵 1：BaseLib + Spire1，无 Watcher
- priority: P1（缺少正向/负向隔离运行证据）
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:2-14`；`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs:108-111`；正向三 mod 对照日志 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log:11-17,279-290`。
- trigger: 只挂载 `BaseLib` 与 `Spire1`，确认 Spire1 不把 Watcher 误当硬前置，并确认 Watcher bridge 在缺失时不制造启动失败。
- current control flow: manifest 只要求 BaseLib；bridge 若被调用，会因已加载名为 `Watcher` 的程序集数量为 0 而进入 catch，清空 binding、回滚本 owner 的 patch 并记录 disabled。现有真实日志只显示完整三 mod 顺序 `BaseLib -> Watcher -> Spire1`，没有该部分集合。
- contract: 该矩阵应允许 Spire1 在 BaseLib 满足时启动；Watcher 相关能力应不可用而非成为未声明硬失败。
- reproducible command: `Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log' -Pattern 'Loaded 2 mods|bridge disabled|Expected one loaded Watcher assembly' -CaseSensitive:$false`（仅复核已有日志；本轮未执行新的启动）。
- minimum fix: 无静态产品修复结论；验收所需最小动作是生成只含 `BaseLib`、`Spire1` 的隔离 staging，运行一次并记录 loader 顺序、Spire1 initializer、bridge disabled/未触发及退出码。
- missing runtime evidence: 缺少当前、仅含 `BaseLib` + `Spire1` 的真实启动日志和退出结果；不能用历史 `Loaded 2 mods` 结果替代。

### 5. 矩阵 2：BaseLib + Watcher，无 Spire1
- priority: P1（缺少部分集合正向运行证据）
- absolute path and line: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods\BaseLib\BaseLib\BaseLib.json:1-12`；`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods\Watcher\Watcher\Watcher.json:1-12`；目标三 mod 日志 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log:11-17,44-46`。
- trigger: 只挂载 `BaseLib` 与 `Watcher`，确认 Watcher 不会通过 Spire1 的 staging 或 bridge 获得隐式前置，也确认没有 Spire1 代码被加载。
- current control flow: BaseLib manifest 明确 `dependencies: []`；Watcher manifest 未声明依赖字段，当前指定 staging 仍把其 payload 放在 `Watcher\Watcher\` 嵌套目录；已有真实日志只证明 Watcher 在完整三 mod运行中被加载，不能隔离出无 Spire1 的路径。
- contract: 该矩阵的交付集合只能包含 BaseLib 与 Watcher 顶层目录；Spire1 不得出现，Spire1 的 Watcher bridge 不应参与启动。
- reproducible command: `$p='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods'; Get-ChildItem -LiteralPath $p -Directory -Force | Where-Object Name -in @('BaseLib','Watcher') | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File -Force -Recurse | Select-Object FullName }; Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log' -Pattern 'Spire1|Loaded 2 mods' -CaseSensitive:$false`（仅复核已有目录/日志；本轮未执行新的启动）。
- minimum fix: 先让 partial staging 生成器输出两个扁平顶层 mod 目录，再用仅含两 mod 的隔离启动证明；不修改 Spire1 产品代码。
- missing runtime evidence: 缺少仅含 `BaseLib` + `Watcher` 的当前 loader 顺序、退出码及“无 Spire1 initializer”证据；当前日志明确包含 Spire1，不能替代。

### 6. 矩阵 3：仅 BaseLib
- priority: P1（缺少最小基线运行证据）
- absolute path and line: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staged-mods\BaseLib\BaseLib\BaseLib.json:1-12`；测试客户端现存目录 `G:\omp works\Sts\_runtime\sts2-test-client-B\mods\BaseLib\BaseLib.json:1-12`；对照日志 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\stdout-final.log:11-20`。
- trigger: 只挂载 BaseLib，确认基础库自身的空依赖 manifest 不会因 Spire1、Watcher 或其它 mod 的残留而改变启动矩阵。
- current control flow: BaseLib manifest 声明 `dependencies: []`，已有完整运行日志显示 BaseLib 先于 Watcher、Spire1 加载并完成 initializer；但该日志包含其它两个 mod，不能证明 BaseLib-only 启动路径。
- contract: BaseLib-only staging 应只含 `BaseLib` 顶层目录及其同层 manifest/DLL/PCK，不应加载 Spire1、Watcher，也不应依赖它们的初始化副作用。
- reproducible command: `$p='G:\omp works\Sts\_runtime\sts2-test-client-B\mods'; Get-ChildItem -LiteralPath $p -Directory -Force | Where-Object Name -eq 'BaseLib' | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File -Force -Recurse | Select-Object FullName }; Get-Content -LiteralPath 'G:\omp works\Sts\_runtime\sts2-test-client-B\mods\BaseLib\BaseLib.json' -Raw | ConvertFrom-Json | Select-Object id,version,dependencies`
- minimum fix: 无源码修复结论；验收最小动作是从干净隔离目录生成 BaseLib-only staging 并记录 loader 发现/排序/退出码。
- missing runtime evidence: 没有仅含 BaseLib 的当前真实启动日志；当前测试客户端 `mods` 目录还包含大量其它 mod，不能作为 BaseLib-only 证据。

### 7. 矩阵 4：Spire1 无 BaseLib，预期显式依赖失败
- priority: P0（硬前置缺失的 fail-closed 契约）
- absolute path and line: `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-14`；`G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:46-48`；`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:14-15,49-70`。
- trigger: 只挂载 Spire1，移除 BaseLib，确认 loader 在调用 Spire1 initializer 前报告明确的缺依赖失败，而不是把未声明的 BaseLib 当作已满足或继续半初始化。
- current control flow: manifest 明确要求 `BaseLib >= 3.4.5`；csproj 存在 BaseLib package reference；`MainFile` 是带 `ModInitializer` 的 Node，初始化阶段直接调用 BaseLib 的 `SimpleLoc` 与 `ModConfigRegistry` API。现有所有真实启动证据都包含 BaseLib，因此没有观察到缺失路径。
- contract: `Spire1` 无 BaseLib 时应显式失败并阻断 Spire1 初始化；失败文本和退出码应来自 loader/启动器实际输出，不能用源码推测的异常文本冒充运行证据。
- reproducible command: `$p='G:\omp works\Sts\sts2-spire1\mod\Spire1.json'; $m=Get-Content -LiteralPath $p -Raw | ConvertFrom-Json; [pscustomobject]@{ id=$m.id; dependencies=$m.dependencies } | ConvertTo-Json -Depth 5; Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj','G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs' -Pattern 'Alchyr.Sts2.BaseLib|using BaseLib|SimpleLoc|ModConfigRegistry|ModInitializer'`（只复核静态契约；本轮未执行缺依赖启动）。
- minimum fix: staging/启动器必须在交付前拒绝缺失 `BaseLib >= 3.4.5` 的 Spire1 集合；若 loader 已提供该显式失败，则无需产品代码修复，只需补齐当前隔离负向运行记录。
- missing runtime evidence: 缺少仅含 Spire1 的当前启动命令、loader 缺依赖错误文本、Spire1 initializer 未执行证据和退出码；不能宣称“已实测失败”。

