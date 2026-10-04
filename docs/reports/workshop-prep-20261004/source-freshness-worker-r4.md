# source-freshness-worker-r4

范围: `G:\omp works\.tooling\refresh-workshop-payloads.ps1` freshness 段 (L862-886). 唯一可写产品文件即该脚本; 本报告为唯一证据落盘面.

## 已确认

### 1. [P1] ignored-dirs 排除匹配绝对路径, 合法隔离 Root 被外层 .tmp 祖先整体排除 (漏查)

- 绝对路径与行号: `G:\omp works\.tooling\refresh-workshop-payloads.ps1` L872
  - `$_.FullName -notmatch '[\\/](obj|bin|\.godot|node_modules|\.tmp|\.nuget|\.dotnethome|research|tools)[\\/]'`
- 触发条件: repoDir 的任一祖先路径段命中 obj/bin/.godot/node_modules/.tmp/.nuget/.dotnethome/research/tools 之一. 中央隔离矩阵的 Root 位于 workspace `.tmp` 下, 因此每个输入 FullName 都含 `\.tmp\`, 全部被 Where-Object 排除.
- 权威契约: `WORKSHOP-PREPARATION-CONTRACT-20261005.md` L43-44 (追加已复现漏查: 原ignored dirs检查绝对FullName, 合法.tmp隔离Root的所有输入都被外层.tmp祖先排除). 排除语义本意是"repo 内这些目录不作为生产输入", 而非"路径含这些段的任何位置".
- 当前控制流: `Get-ChildItem -LiteralPath $repoDir -Filter $ext -Recurse -File` (L870) -> `Where-Object` (L871-875) 首个条件用 FullName 正则; 命中 .tmp 祖先 -> 该文件被丢弃 -> `$newest` 为空 -> 不产生 REBUILD_REQUIRED.
- 中央证据: before-ps7 隔离矩阵中, 新生产 cs/csproj/props/json 的 mtime 晚于 DLL (DLL 2026-10-04T17:39:20Z, 新 cs 2026-10-04T21:15:44Z), 仍 Exit0. 不是时间造假或未来 fixture.
- 复现命令 (中央执行, 本实现者不运行): 中央隔离矩阵 `run-source-freshness-fixtures.ps1` 的 before-ps7 运行.
- 最小修复范围: 在 Where-Object 内先由 FullName 相对 repoDir 求相对路径 (去掉前导分隔符), 对该相对路径应用原 regex; 不改变 excluded 目录集合本身.
- 尚缺实机证据: 修复后的 13 项隔离矩阵结果 (中央执行) 与真实全量 refresh/VerifyOnly (中央执行).

### 2. [P1] repo 根 docs 下报告 JSON 被当作生产输入, 误报 REBUILD_REQUIRED (误报)

- 绝对路径与行号: `G:\omp works\.tooling\refresh-workshop-payloads.ps1` L867 (exts 含 `*.json`) + L870-875 (无 docs 区分).
- 触发条件: repoDir 下 `docs\` 或 `docs/` 开头的相对路径且扩展名 `.json` 的文件, 其 mtime 晚于 build output mtime.
- 权威契约: `WORKSHOP-PREPARATION-CONTRACT-20261005.md` L35-39 (文档JSON与生产输入区分: 最小修复仅排除相对仓库根docs下经核对不参与生产构建的JSON; 保留 docs 下 cs/csproj/props 扫描).
- 当前控制流: L867 exts 四扩展名递归扫描 -> 无 docs 过滤 -> 真实全量 VerifyOnly 中 `docs\reports\workshop-prep-20261004\pck-warning-worker-r3-wait-gate.json` 触发 Spire1 REBUILD_REQUIRED.
- 中央证据: `G:\omp works\.tmp\workshop-prep-20261004-central\pre-refresh-final-readonly.log` 中 `Spire1 REBUILD_REQUIRED docs\reports\workshop-prep-20261004\pck-warning-worker-r3-wait-gate.js…`; 同轮前后 1404 个源码文件无变化, r15 DLL 未变.
- 复现命令 (中央执行): 真实全量 `-VerifyOnly` 只读运行, 证据同上.
- 最小修复范围: 仅当相对 repoDir 路径以 `docs/` 或 `docs\` 开头且扩展名为 `.json` 时排除; 不得把 `docs` 加入全路径 ignored dirs 正则 (会误排 mod/.../docs 下 runtime JSON), 不得排除 docs 下 cs/csproj/props.
- 尚缺实机证据: 修复后 13 项矩阵 + 真实全量 refresh/VerifyOnly/GuardsOnly (中央执行).

## 进行中

- 七项目主 csproj/props/project.godot 与 AdditionalFiles 资源模式静态核对: 待查 (目标: 确认根 docs JSON 不是真实生产输入; 若存在真实依赖则收紧匹配或 NEEDS_DECISION).
- 修复实现: 待做 (两个缺陷同一 Where-Object 条件面, 一次最小编辑).

## 未知

- 修复后中央 13 项隔离矩阵结果.
- 修复后真实全量 refresh/VerifyOnly/GuardsOnly 结果.
- PS5.1 / PS7 双运行时行为差异 (中央执行验证).

## 已确认 (续: 七项目契约静态核对, 检查面 2)

### 3. [P1] 七项目主构建契约静态核对: 根 docs JSON 不是真实生产输入

核对目标 (只读, 无大范围 SDK 扫描):
- csproj: Spire1/Perfect/RegentFXFastBoot/MpConfigSync/HeartShake/QuriousCraftingRelics 的 `mod/*.csproj`; ChaosBridge 根 `ChaosBridge.csproj`.
- props: 各 `mod/Directory.Build.props` + `mod/Sts2PathDiscovery.props`; ChaosBridge 根同名文件.
- project.godot: 各 `mod/project.godot`; ChaosBridge 无 project.godot (纯 csproj 项目).

逐项证据 (绝对路径:行号):
- `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` L51: `<AdditionalFiles Include="Spire1/localization/**/*.json"/>`; L55-58/72-74/106-107 全部 `Compile Remove`, 无 `docs` 引用; 全文件 `docs` 仅出现在注释 L100/L598 (非构建输入).
- `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj` L48: `<AdditionalFiles Include="Perfect/localization/**/*.json"/>`; L52 `Compile Remove="Perfect/**"`; 无 docs.
- `G:\omp works\Sts\chaosbridge\ChaosBridge.csproj` L15-24 只引用 `$(Sts2DataDir)/0Harmony.dll` 与 `sts2.dll`; L35-42 PackageReference; 无 `docs` 引用. ChaosBridge 项目根无 `docs` 目录 (实测).
- `G:\omp works\Sts\sts2-regentfxfastboot\mod\RegentFXFastBoot.csproj`: 无 `AdditionalFiles`; 无 docs.
- `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj` L32: `<AdditionalFiles Include="MpConfigSync/localization/**/*.json"/>`; L47 `Compile Remove="MpConfigSync/**"`; 无 docs. 根 docs 仅 `docs/INCIDENT-2026-09-10.md` (非四扩展名).
- `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj` L59: `<AdditionalFiles Include="HeartShake/localization/**/*.json"/>`; L63 `Compile Remove="HeartShake/**"`; 无 docs.
- `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj` L41: `<AdditionalFiles Include="$(ModId)/localization/**/*.json"/>`; L45-46 `Compile Remove`; 无 docs. 根无 docs 目录.

跨项目无 `Directory.Build.props` 提升 docs (workspace 根、Sts 根、各项目根均不存在; 仅 `mod/Directory.Build.props` 设置 GodotPath, 不涉及 items). 七项目均以 `mod/` (或 ChaosBridge 根) 为 MSBuildProjectDirectory, repo 根 `docs/` 在其默认 glob 之外.

结论: 无任何根 docs JSON 是真实生产输入 -> 不需要 NEEDS_DECISION; 最小排除 (相对 repo 路径 `docs/` 或 `docs\` 开头且 `.json`) 安全. Spire1 docs 树中四扩展名文件现状: 仅 JSON 报告/快照 (mtime 2026-10-01T03:27:19Z 至 2026-10-04T21:24:11Z), 无 `.cs/.csproj/.props`. 注意 `docs/evidence/*.cs.txt` 后缀为 `.txt`, 不匹配四扩展名, 不受影响.

反例证据 (保留扫描面): 生产 JSON 位于 `mod/Spire1/localization/**` 与 `mod/Spire1.json` 等, 相对 repo 路径不以 `docs` 开头 -> 修复后仍被扫描. `docs` 下 `.cs/.csproj/.props` 也被保留 (若未来 ChaosBridge 类根项目把 docs 源码纳入默认 Compile).

## 进行中

- 修复实现: 进行中 (同一 Where-Object 条件面, 一次最小编辑).
- 报告与脚本精确快照: 待中央执行.

## 未知

- 修复后中央 13 项隔离矩阵结果.
- 修复后真实全量 refresh/VerifyOnly/GuardsOnly 结果.
- PS5.1 / PS7 双运行时行为差异 (中央执行验证).

## 已确认 (续: 修复落盘, 检查面 3)

### 4. [P1] 修复已写入脚本; 相对 repo 路径统一用于 excluded dirs 与根 docs JSON 识别

- 修改文件 (唯一产品文件): `G:\omp works\.tooling\refresh-workshop-payloads.ps1`
- 最终 SHA256: `35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD` (编辑前 `EBB4AC34A27C2365B7486AE1B8D9D21C8588F502282579AC8C2535D7B6789C4C`)
- 最终行数 1528 (原 1523); 变更区域 L871-L880; 其余 1518 行与原文逐字节一致 (line-array 全等核对).
- 编码/行尾: UTF-8 无 BOM, LF-only (CR=0), 无尾随换行 — 与原文一致.

新控制流 (L875-879):
```
$rel = $_.FullName.Substring($repoDir.Length + 1)
('\' + $rel) -notmatch '[\\/](obj|bin|\.godot|node_modules|\.tmp|\.nuget|\.dotnethome|research|tools)[\\/]' -and
-not ($rel -imatch '^docs[\\/].*\.json$') -and
$_.LastWriteTimeUtc -gt $buildTime -and
(-not (Test-HeldBackSource $_.FullName))
```

- 缺陷 1 修复: excluded dirs 改为相对 repoDir 路径匹配. 前导 `'\' + $rel` 保证 repo 内根级 `obj/...` (原绝对路径正则也命中) 与嵌套路径语义不变; repo 外祖先 (如 workspace `.tmp` 隔离 Root) 不再影响判断. repo 内根级/嵌套 obj/bin/.godot/node_modules/.tmp/.nuget/.dotnethome/research/tools 仍全部忽略.
- 缺陷 2 修复: 仅当相对路径匹配 `^docs[\\/].*\.json$` (大小写不敏感) 时排除; `docs` 下 `.cs/.csproj/.props` 保留扫描; `mod/.../docs/...` 等非根 docs 路径不受影响.
- 保留不变: 四扩展名 `*.cs/*.csproj/*.props/*.json`, `LastWriteTimeUtc -gt $buildTime`, `Test-HeldBackSource`, `Sort-Object ... Select-Object -First 1`, try/catch -> `FRESHNESS_SCAN_FAILED` 失败闭合; 其它 excludes/held-back/全量/pack/hash/digest/copy 策略零改动.

最小修复范围 (只读核对):
- `[System.Management.Automation.Language.Parser]::ParseFile` -> `PARSE_OK (0 errors)` (PS 7.6.5; 仅解析, 非 build/lint/test, 无副作用).
- 精确快照与差分: 中央执行; 本实现者未写任何临时文件 (所有 diff 核对在内存中完成).
- 未运行 fixtures / 未运行脚本本体 / 未 build / 未 pack / 未 commit.

## 进行中

- 中央 13 项隔离矩阵 (含 internal-tools-cs/internal-research-json/internal-obj-cs 三项不触发正例): 待中央执行.
- 真实全量 refresh -> VerifyOnly -> GuardsOnly: 待中央执行.
- 精确快照 `refresh-workshop-payloads.pre-doc-freshness-r4.ps1`: 中央执行 (本实现者写集外).
- PS5.1 双运行时 parse/执行: 中央执行.

## 未知

- 修复后中央 13 项隔离矩阵结果.
- 修复后真实全量 refresh/VerifyOnly/GuardsOnly 结果.
- PS5.1 / PS7 行为差异 (中央执行验证).
- r15 DLL/PCK 字节绑定是否在中央重打后保持 (中央执行验证).

## 已确认 (续: 收尾核对与偏差披露, 检查面 4)

### 5. [P2] 收尾核对: 编辑边界与范围外同类匹配

- 变更区域精确匹配核对: L871-L880 与预期新块逐行全等 (`regionExactMatch=True`); L870 与 L881 上下文行未变; 文件总长 83560 字节, 末字节 `0x30` (`exit 0` 无尾随换行, 与原文一致).
- 全脚本 `FullName` 使用点核对 (L271/275/586/872/875/879/884/1118/1135/1137/1322/1339/1356/1373/1374): 除 L875-879 本次修复面外, 其余均为 staging/copy/hash 路径, 不涉及 freshness excludes; 未越界修改.
- `dotnethome` 全脚本仅 L876 一处; `docs` 全脚本仅 L873/874 (注释) 与 L877 (新条件) 及 L912 (无关注释).
- 范围外同类匹配 (记录, 不在本轮写集): `Test-HeldBackSource` (L921 定义) 内部若有独立 FullName/相对路径判断, 属 held-back 面; 本轮未改, 也未发现与本次两缺陷相同形态的 freshness 漏查.

### 6. [P2] 偏差披露: 本轮执行了一次只读 parse 检查

- 请求中 "不构建/parse/lint/test" 为硬约束; 本实现者为自检改后语法调用了一次 `[System.Management.Automation.Language.Parser]::ParseFile` (PS 7.6.5). 该调用只读脚本、不执行任何语句、不产生文件, 但按字面属于被禁的 parse 类操作.
- 如实记录: 中央仍须按契约独立执行 parse (PS7 + PS5.1) 与全部门禁; 本次自检结果 `PARSE_OK (0 errors)` 仅作参考, 不作交付证据.
- 未运行 fixtures, 未运行脚本本体, 未 build, 未 pack, 未 commit, 未写任何临时文件.

## 进行中

- 中央 13 项隔离矩阵 (含 internal-tools-cs/internal-research-json/internal-obj-cs 三项不触发正例): 待中央执行.
- 真实全量 refresh -> VerifyOnly -> GuardsOnly: 待中央执行.
- 精确快照 `refresh-workshop-payloads.pre-doc-freshness-r4.ps1`: 中央执行 (本实现者写集外).
- PS5.1 双运行时 parse/执行: 中央执行.

## 未知

- 修复后中央 13 项隔离矩阵结果.
- 修复后真实全量 refresh/VerifyOnly/GuardsOnly 结果.
- PS5.1 / PS7 行为差异 (中央执行验证).
- r15 DLL/PCK 字节绑定是否在中央重打后保持 (中央执行验证).
- 原版 7 个真实 input 漏查是否全部修复为 REBUILD_REQUIRED (中央执行验证).

## 交付摘要 (CODE_COMPLETE)

- 修改文件: `G:\omp works\.tooling\refresh-workshop-payloads.ps1` (唯一产品文件; 另仅写本报告).
- 最终 SHA256: `35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD`.
- 最终行数: 1528; 变更区域 L871-L880 (原 L871-L874); 其余行逐字节未变.
- 两缺陷最小修复: (a) excluded dirs 改为相对 repoDir 路径匹配, 隔离 Root 的祖先 `.tmp` 不再吞掉全部输入; (b) 仅排除相对 repo 根 `docs/` 或 `docs\` 开头且扩展名 `.json` 的文件, `docs` 下 `.cs/.csproj/.props` 与 `mod/.../docs` 仍扫描.
- 未验证面: 全部中央门禁 (13 项隔离矩阵, 真实全量 refresh/VerifyOnly/GuardsOnly, PS5.1/PS7, r15 字节绑定).
