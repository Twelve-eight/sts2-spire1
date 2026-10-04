# Source Freshness Supervisor R4

- 状态: WAITING
- 监督范围: G:\omp works\.tooling\refresh-workshop-payloads.ps1 文档JSON过滤
- 同批实现者精确目标: 01a108cd-b5c4-7b72-ada4-051ee3c15cea
- 激活条件: 主会话原生 hub wait 到该精确目标 completed 并冻结 hash 后激活. 激活前保持 WAITING.
- 冻结前纪律: 不审代码, 不 build/parse/lint/test/pack, 不委派, 不写临时文件, 只写本报告.

## 已确认

- 2026-10-05: 已读取监督请求文件 `G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004/source-freshness-supervisor-r4.request.md`.
- 已按要求先写入 WAITING; 本轮在激活前不进行任何代码审查.
- 唯一可写路径确认: 本文件. 不改产品代码/构建/部署/游戏/共享配置, 不写 C:.
- 同批实现者精确 id 已由主会话给出: `01a108cd-b5c4-7b72-ada4-051ee3c15cea` (仅记录, 未激活).

## 进行中

- 等待主会话原生 hub wait 对该精确实现者完成 completion gate, 并冻结 hash 后激活监督.
- 激活前不启动任何审查动作, 不预读实现 diff.

## 未知

- 冻结 hash 与最小补丁内容.
- 中央隔离矩阵 13 项最终结果.

---

## 激活面 0: 门禁与 hash 核对 (已确认)

- 状态: ACTIVE (主会话原生 wait 精确实现者 `01a108cd-b5c4-7b72-ada4-051ee3c15cea` 返回 completed, timed_out=false; 门禁 JSON: `G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004/source-freshness-worker-r4-wait-gate.json`).
- 独立 hash 核对 (只读): `Get-FileHash -LiteralPath 'G:\omp works\.tooling\refresh-workshop-payloads.ps1' -Algorithm SHA256` -> `35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD`, 与门禁冻结值及主会话宣称一致.
- 基线路径存在性: `G:\omp works\.tmp\workshop-prep-20261004-central\refresh-workshop-payloads.pre-doc-freshness-r4.ps1` 待读取核对.
- 本轮纪律: 只读审查; 不 build/parse/lint/test/pack; 不写临时文件; 唯一写路径为本报告.

## 已确认 (累积)

- 激活门禁: 精确实现者 completed (timed_out=false), 产品 SHA256 = `35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD`, 独立复算一致.

---

## 面 1: 补丁差分与相对路径语义 (已确认)

### 1.1 补丁最小性与逐字节等价 (P1 面, PASS)

- 独立复算基线: `G:\omp works\.tmp\workshop-prep-20261004-central\refresh-workshop-payloads.pre-doc-freshness-r4.ps1` SHA256 = `EBB4AC34A27C2365B7486AE1B8D9D21C8588F502282579AC8C2535D7B6789C4C`, 83057 字节, 1523 行.
- 产品文件: `G:\omp works\.tooling\refresh-workshop-payloads.ps1` SHA256 = `35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD`, 83560 字节, 1528 行, 与门禁冻结值一致.
- 差分命令 (只读): `git --no-pager diff --no-index --unified=12 -- <base> <prod>` -> 单一 hunk, `@@ -860,25 +860,30 @@`, 仅 freshness Where-Object 条件面.
- 逐字节等价证明 (内存构造, 无临时文件): 取基线唯一匹配旧块 (base L872-L874, 3 行) 替换为新块 (prod L872-L880, 8 行), 得到 `expected`; `expected -ceq prod` -> `True`; expected/prod 长度均 83560. 即产品文件 = 基线 + 仅此一处替换, 无任何其它字节改动.
- 编码面: 两文件均 UTF-8 无 BOM (首字节 35,32,114 = `# r`), CR=0 (LF-only), 末字节 0x30 (`exit 0` 无尾随换行). 净 +5 行 = 3 行注释 + `$rel` 赋值 + 根 docs JSON 条件.
- `$rel` 名冲突核对: 产品文件内 `$rel` 出现于 L586-592 (staging allowlist 面, 独立 foreach 作用域) 与 L1118-1131/L1322-1381 (copy 面), 均非 freshness Where-Object 所在作用域; L875 的脚本块内赋值与 L884 的重算互不依赖. 未发现遮蔽/串扰.

### 1.2 相对路径语义 (P1 面, PASS)

- `$repoDir` 来源 L769: `Full-Path (Join-Path $root $it.Repo)`; L195-197 `Full-Path` = `[System.IO.Path]::GetFullPath`; L69 已 TrimEnd 分隔符. `Get-ChildItem -LiteralPath $repoDir -Recurse` 返回的 `FullName` 以 `$repoDir` 为前缀, 故 `Substring($repoDir.Length + 1)` 恰好去掉 `repoDir + '\'`, 得 repo 相对路径 (L875).
- 原绝对路径正则 vs 新相对路径正则语义等价性核对 (内存正则求值, 只读):
  - repo 内根级 `\obj\a.cs`, `\bin\x\a.cs` -> 命中; 嵌套 `\sub\obj\a.cs`, `\src\obj\a.cs`, `\mod\tools\x.cs`, `\foo\research\bar.json` -> 命中 (前导 `'\' + $rel` 补齐分隔符后与原绝对路径同样可命中).
  - 近似名 `\objish\a.cs` -> 不命中 (目录名边界未被放宽).
  - repo 外祖先 (workspace `.tmp` 隔离 Root) 不再参与匹配: 相对路径首段不含祖先段, 原"每个输入都因外层 .tmp 被排除"的缺陷在该式下不可能复现 (仅源码推理; 实机矩阵由中央执行).
- 根 docs JSON 条件核对: `-not ($rel -imatch '^docs[\\/].*\.json$')` (L877, 大小写不敏感):
  - `docs\reports\a.json`, `docs/a.json`, `docs\deep\x\y\z.json`, `Docs\Reports\A.JSON` -> 排除 (预期).
  - `docs\a.cs`, `docs\a.csproj`, `docs\a.props` -> 保留 (预期).
  - `mod\Spire1\docs\a.json`, `other\docs\a.json`, `mod\Spire1\localization\a.json` -> 保留 (预期, 任意段 docs 未被排除).

### 1.3 保留不变量核对 (P1 面, PASS)

- 四扩展名 L867 `*.cs/*.csproj/*.props/*.json` 未变.
- mtime 条件 L878 `$_.LastWriteTimeUtc -gt $buildTime` 未变.
- held-back 过滤 L879 `Test-HeldBackSource $_.FullName` 未变 (定义 L926-928, 绝对路径 contains 语义未受相对化影响).
- 排序选择 L880 `Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1` 未变.
- 失败闭合 L887-889 `catch -> FRESHNESS_SCAN_FAILED` 未变; `-ErrorAction Stop` (L870) 未变.
- excludes/held-back/mtime/digest/字节/复制检查等其它面 (L586-592, L1118+, L1290+) 在 diff 中零改动 (由 1.1 逐字节等价保证).

- 尚缺证据 (中央执行): 13 项隔离矩阵, 真实全量 refresh/VerifyOnly/GuardsOnly, PS5.1/PS7 行为, r15 字节绑定. 以上均未由监督者执行, 不预称通过.
- 边界记录 (不构成 NEEDS_REWORK): `Substring` 假定子项 FullName 以 `$repoDir` 前缀开头; 该假定与既有 L884 (原版同样写法) 相同, 且 A10 pre-write 路径门禁拒绝 junction/symlink 链, 非本次引入的新风险.

---

## 面 2: 七项目生产依赖独立核对 (已确认)

核对对象 (脚本 L133-141 行表 + L769 `$repoDir`): Spire1 / Perfect / ChaosBridge / RegentFXFastBoot / MpConfigSync / HeartShake / QuriousCraftingRelics.

### 2.1 项目文件与上游 props 链

- 七项目主构建文件确认: `mod\Spire1.csproj`, `mod\Perfect.csproj`, `ChaosBridge.csproj` (根), `mod\RegentFXFastBoot.csproj`, `mod\MpConfigSync.csproj`, `mod\HeartShake.csproj`, `mod\QuriousCraftingRelics.csproj`; 均存在.
- 上游 `Directory.Build.props/targets`/`Directory.Packages.props`: workspace 根, `Sts` 根, 七项目根均**不存在** (已逐一 Test-Path 确认). 仅有各项目内一层: `mod\Directory.Build.props` (或 ChaosBridge 根) + `mod\Sts2PathDiscovery.props`.
- `mod\Directory.Build.props` (七份内容同构): 仅 `<GodotPath>` 属性; 无任何 Item/Import 项 -> 不能把 repo 根 docs 引入构建.
- `<Import>` 仅一处/项目: `.\Sts2PathDiscovery.props` (L2), 同目录, 不向上越级.
- `Sts2PathDiscovery.props`: 仅设置 `Sts2Path`/`ModsPath`/`Sts2DataDir` 属性 (macOS 分支 L49-50), 无 `docs`/Item Include -> 不引入 docs.
- `project.godot` 存在于 6 个 mod 项目 (Spire1/Perfect/RegentFXFastBoot/MpConfigSync/HeartShake/QuriousCraftingRelics), ChaosBridge 无 (与 worker 报告一致); 六份 `project.godot` 内均无 `docs` 引用.

### 2.2 Item 级核对 (Compile/EmbeddedResource/AdditionalFiles/CopyToOutput/Pack/None/Content)

- `mod\Spire1.csproj`: `AdditionalFiles Include="Spire1/localization/**/*.json"` (L51); `Compile Remove` L55-58/72-74/106-107; `EmbeddedResource Remove` L59-62; `None Include` L112-115 (`.gitignore`/`Spire1.json`/`project.godot`/`Spire1/**`). 全部相对 `mod\`; 无 `docs`. 文件内 `docs` 仅 L100/L598 注释.
- `mod\Perfect.csproj`: `AdditionalFiles` L48 (相对); `Compile Remove` L52; `EmbeddedResource Remove` L53; `None Include` L58-60. 无 docs.
- `ChaosBridge.csproj` (57 行, 全文核对): L15-24 `<Reference>` 指向 `$(Sts2DataDir)` 外部 DLL; L35-38/40-42 PackageReference; L44-48 路径检查; L50-56 CopyToModsFolder (只复制 TargetPath / `$(AssemblyName).json` / pdb). **无 Item Remove/Include 覆盖默认 Compile glob** -> 根目录默认 `**/*.cs` (SDK 默认含 `docs/**/*.cs`) 仍会参与编译, 这正是 docs 下 `.cs/.csproj/.props` 必须保留扫描的原因 (契约要求).
- `mod\RegentFXFastBoot.csproj`: 无 `AdditionalFiles`; 无 Compile/EmbeddedResource Remove; 无 docs.
- `mod\MpConfigSync.csproj`: `AdditionalFiles` L32; `Compile Remove` L47; `EmbeddedResource Remove` L48; `None Include` L53-55. 无 docs.
- `mod\HeartShake.csproj`: `AdditionalFiles` L59; `Compile Remove` L63; `EmbeddedResource Remove` L64; `None Include` L69-71. 无 docs.
- `mod\QuriousCraftingRelics.csproj`: `AdditionalFiles` L41; `Compile Remove` L45-47; `EmbeddedResource Remove` L47-48; `None Include` L53-56. 无 docs.
- 全七份文件 grep `Include=|Import ` 且含 `..` 或 `docs` -> 零命中 (无越目录 docs 引用).
- 全七份文件 grep `docs` -> 仅 Spire1 L100/L598 注释; 其余零命中.

### 2.3 根 docs 实际内容 (排除面现状)

- `sts2-spire1\docs` 四扩展名文件 19 个, **全部为 `.json`** (报告/快照/wait-gate); 无 `.cs/.csproj/.props`. 修复后这 19 个全部被新条件排除 (不再触发误报 REBUILD_REQUIRED).
- 其余六项目根 `docs`: sts2-perfect/chaosbridge/sts2-regentfxfastboot/sts2-heartshake/AutoAnthonyRelics **无 docs 目录**; sts2-mpconfigsync 有 `docs` 但四扩展名文件数 = 0 (仅 `.md` 等) -> 修复对其无影响, 也不掩盖任何四扩展名文件.

### 2.4 结论 (P1 面, PASS)

- 七项目均无任何"根 docs JSON 是真实生产输入"的路径: 无 `docs` 的 `Compile/EmbeddedResource/AdditionalFiles/CopyToOutput/Pack/None/Content` 项, 无上游 props/targets 提升, 无越目录引用.
- 反例 (保留扫描面) 确认: Spire1 生产 JSON 在 `mod\Spire1/localization/**` 与 `mod\Spire1.json` 等, 相对 repo 路径不以 `docs` 开头 -> 修复后仍被扫描; `docs` 下 `.cs/.csproj/.props` (若未来 ChaosBridge 式默认 Compile 需要) 仍保留.
- 边界: 本结论为静态源码/配置证据 (源码推理), 不是构建产物复现; 构建级验证由中央执行 (未由监督者执行, 不预称通过).

## 进行中

- 待汇总: 中央门禁项清单与监督最终裁决 (SUPERVISION_PASS / NEEDS_REWORK).

## 未知

- 13 项隔离矩阵结果 (中央执行).
- 真实全量 refresh/VerifyOnly/GuardsOnly 结果 (中央执行).
- PS5.1/PS7 运行时行为 (中央执行).
- r15 DLL/PCK 字节绑定 (中央执行).

---

## 面 3: 最终汇总与裁决 (已确认)

### 3.1 核对结论汇总 (监督者独立证据)

| 面 | 结论 | 关键证据 |
|---|---|---|
| 冻结门禁 | PASS | worker `01a108cd-b5c4-7b72-ada4-051ee3c15cea` completed, timed_out=false (wait-gate JSON); 产品 hash 独立复算 = 冻结值 |
| 补丁最小性 | PASS | 逐字节等价: 基线 + 唯一 3->8 行替换 = 产品全文 (83560 字节全等); diff 单一 hunk `@@ -860,25 +860,30 @@` |
| 相对路径 excludes | PASS (源码推理) | L876 `'\' + $rel` 匹配; repo 内根级/嵌套 9 类目录名仍命中; repo 外祖先 (含 `.tmp` 隔离 Root) 不再影响 |
| 根 docs JSON 过滤 | PASS (源码推理) | L877 `^docs[\\/].*\.json$` 仅根 docs 的 JSON; docs 下 cs/csproj/props 保留; `mod/.../docs` JSON 保留 |
| 四扩展名/mtime/held-back/排序/失败闭合 | PASS | L867-880 与基线逐行一致 (由 3->8 替换块外全等保证) |
| 七项目生产依赖 | PASS (静态) | 无 docs 的 Compile/EmbeddedResource/AdditionalFiles/CopyToOutput/Pack/None/Content 项; 无上游 props/targets; 无越目录引用; ChaosBridge 无 Compile Remove (默认 glob 保留 docs 源码扫描的理由成立) |
| 根 docs 实际内容 | 事实 | Spire1 根 docs 四扩展名 19 个全为 .json; MpConfigSync docs 四扩展名 0 个; 其余 5 项目无 docs 目录 |

### 3.2 未由监督者执行、必须由中央门禁覆盖的项 (边界, 不预称通过)

1. PS7 + PS5.1 parse (本次监督未执行 parse; 实现者自述曾执行一次只读 parse, 按请求该结果仅参考、不作交付证据).
2. 13 项隔离矩阵 (原 7 个真实/保守 input 须 REBUILD_REQUIRED; 新增 internal-tools-cs / internal-research-json / internal-obj-cs 不触发; docs JSON 正例不触发; 全部须在 workspace `.tmp` 隔离 Root 下运行以证明祖先排除缺陷修复).
3. 真实全量 refresh -> VerifyOnly -> GuardsOnly (7/7 OK; VDF 配对; held-back 字节; 无 Steam contact).
4. r15 DLL/PCK/manifest 字节绑定保持 (若 hash 变化则 r15 实机证据失去覆盖).
5. 修改脚本精确快照备份与部署 hash 一致性 (契约 L29).
6. 旧版误报证据面 (契约要求: 真实 Sts 全量 pre-refresh-final-readonly.log 的 docs JSON 误报), 监督者未重跑该全量.

### 3.3 裁决

**SUPERVISION_PASS (源码/配置面; 中央门禁未执行, 待中央验证确认)**

- 产品: `G:\omp works\.tooling\refresh-workshop-payloads.ps1`
- 冻结 SHA256: `35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD` (监督独立复算一致)
- 基线 SHA256: `EBB4AC34A27C2365B7486AE1B8D9D21C8588F502282579AC8C2535D7B6789C4C`; 变更仅 L871-L880 区域, 净 +5 行 (1523 -> 1528), UTF-8 无 BOM / LF-only / 无尾随换行保持.
- 门禁判定依据: (a) 最小补丁逐字节等价; (b) 两缺陷均按契约最小面修复且未弱化 excludes/held-back/mtime/四扩展名/失败闭合; (c) 七项目静态依赖核对未发现根 docs JSON 生产依赖 (若有依赖则不写 PASS 的条件未触发).
- PASS 不覆盖: 3.2 全部中央项; 任何构建/运行/实机结论均由中央与主会话执行后给出.
- 若中央矩阵或全量门禁出现与上述源码结论矛盾的证据 (例如 docs 正例在非 .tmp Root 下误报、真实 input 仍漏查、parse 失败), 本 PASS 应被撤销并转为 NEEDS_REWORK.
