# Release pipeline worker 2026-10-05

请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\release-pipeline-worker-20261005.request.md`
唯一可写报告: 本文件。可写产品文件限定为 6 个 (Build-Spire1Release.ps1, refresh-workshop-payloads.ps1, 4 个 csproj)。

状态: **CODE_COMPLETE** (本 worker 的两个脚本切片; 四个 csproj 已按协调者指令移交 Tesla, 不在本 worker 交付面)。

## 已确认

### W1. 范围与编码事实

- 已读请求文件、协调契约 `G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md` 与前一轮 scout 报告。
- 范围变更 (协调者指令, 2026-10-05): 四个 csproj 移交 Tesla (同批另一 agent) 负责; 本 worker 只完成两个脚本编辑。不 build, 不 lint, 不 test, 不 pack, 不部署, 不运行游戏, 不接触 Steam/共享配置, 不写 C:。
- 编辑前编码/换行实测:
  - `Build-Spire1Release.ps1`: 无 BOM, CRLF, 纯 ASCII, 315 行 / 15,459 bytes。
  - `refresh-workshop-payloads.ps1`: 无 BOM, 纯 LF, 纯 ASCII, 1494 行 / 80,864 bytes。

### W2. [CODE_COMPLETE] Build-Spire1Release.ps1

文件: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`
编辑后: 438 行 / 24,710 bytes; 无 BOM, 纯 CRLF (crlf=438, lfOnly=0), 纯 ASCII (nonAscii=0); Parser::ParseFile parseErrors=0。

变更 1: 新增函数 `Assert-SafeWriteTarget` (L42-67), 位于 `Assert-NoReparse` 之后。语义: `Assert-Under` 目标必须在根内; 拒绝 C: 前缀; 拒绝 `\steamapps(\)` 与 `\mod_configs(\)`; 从目标逐级向上到盘根检查每个已存在层级的 reparse 位与 `steam_appid.txt` 标记; 对尚不存在的文件目标只检查已存在的祖先 (Get-Item 仅在 Test-Path 为真时调用)。

变更 2: 重写 `-Promote` 块 (L316-432, 原 L289-309)。顺序与语义:
- L317-318: `-PlanOnly` 与 `Configuration -ne 'Release'` 在任何发布写入前拒绝 (原代码只拒绝 PlanOnly)。
- L319-322: Workshop 根/内容根边界与 reparse 检查保持原样, ShouldProcess 确认保持原样。
- L324-332: 注释明示 canonical 同步只在显式 `-Promote`、PCK 结构门禁与 DLL 三项门禁之后、任何 Workshop payload 字节之前执行; 明示两文件安装 (PCK 先, digest 后) 不是成对原子事务, 中断留下不完整对时由现有 provenance gate 拒绝; 不伪造时间戳。
- L333-347: canonical 路径显式为 `mod\.godot\mono\temp\bin\Release\Spire1.dll|pck|pck.sha256`; 目录存在性 + 四个目标的 `Assert-SafeWriteTarget`; canonical DLL 必须存在; `$dll` 必须等于 canonical (bin/Release 与 publish fallback 一律拒绝 promotion)。
- L348-354: canonical DLL SHA256 与已 staged payload DLL SHA256 逐字节相等才继续 (否则 PROMOTE-DENY)。
- L356-358: 重算 payload PCK SHA256 并与结构门禁时的 `$pckHash` 相等 (防门禁后被换)。
- L360-364: 唯一临时名 `Spire1.pck.new-<guid>` / `Spire1.pck.sha256.new-<guid>`, 均在 canonical 目录内并过 `Assert-SafeWriteTarget`。
- L365-375: 复制到临时 PCK; 回读长度与 SHA256; 实际 mtime 必须 >= canonical DLL mtime (不满足即 abort, 不改时间); 写临时 digest (ASCII), 回读校验 `^[0-9A-F]{64}$` 且等于临时 PCK hash。
- L376-384: `[System.IO.File]::Copy` 安装 PCK, 再安装 digest; 回读最终 PCK 非空/SHA256 等于临时 hash/mtime >= DLL mtime; 回读最终 digest 等于最终 PCK hash。
- L385-398: 写 `evidence\canonical-sync.json` (Schema=Spire1CanonicalSync.v1, PairAtomic=$false, InstalledOrder=[pck,digest], DLL/PCK hash, PCK mtime)。
- L399-405: finally 中仅用 LiteralPath 删除本函数自己创建的已知临时路径 ($tempPck/$tempDigest), 绝不动其它文件。
- L407-410: 写 payload 前对 Workshop 三目标做 `Assert-SafeWriteTarget` (存在时)。
- L411-423: 三文件复制 + stale 清理 + 精确三文件 allowlist 检查 (与原逻辑一致)。
- L424-430: 新增 fail-closed post-promote 回读: 三文件逐个 SHA256 与 payload 对应文件相等, 不等即 PROMOTE-READBACK-DENY。
- 普通非 promote 运行: 同步代码全部位于 `if ($Promote)` 内, 默认行为不变。

### W3. [CODE_COMPLETE] refresh-workshop-payloads.ps1

文件: `G:\omp works\.tooling\refresh-workshop-payloads.ps1`
编辑后: 1523 行 / 83,057 bytes; 无 BOM, 纯 LF (crlf=0, lfOnly=1522), 纯 ASCII (nonAscii=0); Parser::ParseFile parseErrors=0。

变更 1 (R1): Spire1 row (原 L134) 追加 `PublishPdb = $false`; 其余 6 行未改 (默认 optional PDB 行为不变)。

变更 2 (R2): `Get-RowAllowlist` (现 L553-571) 改为 policy-aware: 新增 `$publishPdb` 默认 `$true`; 行内存在 `PublishPdb` 键时取其 bool; 仅当为 true 才添加 pdb 条目。Spire1 的 allowlist 因此完全不含 `Spire1.pdb`:
- copy direction 1: staged `Spire1.pdb` 的 `$entry.Count -eq 0` -> `STAGED_FILE_NOT_ALLOWLISTED` finding (不删除)。
- copy direction 2: 只按 `$allow` 逐项补文件, pdb 不在其中, 永不复制回 staging。
- pre-copy artifact gate 与 Invoke-AllowlistReconciliation 同用该函数, 已有 staged pdb 时在任何写入前 exit 1。
- 其它行无 `PublishPdb` 键 -> 行为与改动前完全一致。

变更 3 (R3): copy direction 1 的同 hash 分支 (现 L1339-1362) 修复同 hash 不同 mtime 的 PCK bug:
- 非 pck role: 行为不变 (`ALREADY_CURRENT` + continue)。
- pck role: 读取 build 与 staged 的真实 `LastWriteTimeUtc`; 不同则 `$sameHashNeedsCopy=$true` 落到后续 WhatIf/复制路径; 相同则维持 `ALREADY_CURRENT`。
- 读取失败 -> `PATH_UNREADABLE` finding + continue (fail closed)。
- 只重写 mtime 的伪装路径不存在; 复制走原有 `Copy-Item` 逻辑并回读 hash。
- `-VerifyOnly` 根本不进入 copy phase (原 `if (-not $VerifyOnly)` 包裹未动), 仍由 Invoke-Verification 的 staged-vs-build mtime 比较拒绝 mismatch; `-WhatIf` 在复制前短路为 WOULD_COPY, 保持只读。
- 未改动任何 SHA、digest-required、mtime、freshness、path、held-back、ITEM inventory、`-Only` 语义。

变更 4 (R4): 头部注释 L26 更新, 说明行可通过 `PublishPdb=$false` 省略 pdb。

变更 5 (R5): 新增的 mtime 读取失败状态 `PATH_UNREADABLE` 同时加入 `$badStatuses` (L1436) 与 `$resultFailures` (L1508) 两个失败清单, 使该分支在任何模式下都会触发 exit 1 (fail-closed), 而不是仅打印结果表。

静态不变量复核 (编辑后文本, 未执行脚本): 10/10 通过 - Spire1 row 含 `PublishPdb = $false`; Get-RowAllowlist 读取该键; pdb 条目仅在 `$publishPdb` 为真时添加; 同 hash 分支对 pck role 读 mtime; 不同 mtime 置 `$sameHashNeedsCopy=$true`; `if (-not $VerifyOnly)` 包裹未动; WhatIf 短路在 Copy-Item 之前; `PATH_UNREADABLE` 出现在两个失败清单; 无 BOM 且纯 ASCII; pdb 条目构造仅剩一处。

与中央快照的差异核对 (只读): `Build-Spire1Release.ps1` 对 `.original` 为 123 行纯插入 0 行删除 (Compare-Object 全部为 '=>'); `refresh-workshop-payloads.ps1` 对 `.original` 的 8 处删除均为预期替换 (头部 pdb 注释 1 行, Spire1 row 1 行, pdb 条目 1 行, 同 hash 早退 2 行, 两个失败清单 2 行), 无未预期删除。

### W4. 编辑方法学与可复核性

- 两个脚本均先读原始文本, 以唯一锚点做精确 `Replace` (每处替换前校验出现次数必须为 1), 再以 `UTF8Encoding($false)` 写回, 保持原编码与换行。
- 插入代码块先落盘到 `G:\omp works\.tmp\spire1-worker-20261005\blockA.txt` / `blockB.txt` 再拼接; 未触碰任何未授权文件。
- 编辑后只做了 parse 检查 (Parser::ParseFile) 与编码/换行/ASCII 检查; 未 build, 未执行脚本任何分支。

### W5. 变更文件绝对路径清单 (本轮本 worker)

1. `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`
2. `G:\omp works\.tooling\refresh-workshop-payloads.ps1`

## 进行中

- 本 worker 两个脚本切片已 CODE_COMPLETE; 等待中央会话集中 build/verify (parse, -SkipBuild -Promote 演练, refresh VerifyOnly 等)。
- 四个 csproj (Perfect / MpConfigSync / HeartShake / QuriousCraftingRelics) 属 Tesla 切片; 本 worker 将按 `quick-pck-supervisor-20261005.request.md` 在其报告与 wait 证据齐备后执行监督 (尚未开始)。

## 未知

- 未验证 (按规格本轮不做): 编辑后脚本的真实 build 行为、-Promote 端到端演练、refresh 修复后的端到端结果、digest producer 在真实 PackPck 流程中的效果。这些属中央会话的 build/verify 职责。
- [P2] `[System.IO.File]::Copy` 在同卷覆盖是否原子未在本机实测 (与 scout 报告 U6 同源); 语义上中断后由 provenance gate 拒绝, 不依赖原子性。
- [P2] PowerShell 5.1 兼容性: 两个脚本新增代码仅用 PS5.1 已有 API (Get-Item/Get-FileHash/Set-Content/Copy-Item/System.IO.File), parse 检查在 PS7 下通过; PS5.1 下的 parse 由中央会话验证。
- [P2] `Assert-SafeWriteTarget` 对不存在文件目标只检查已存在祖先; 若未来在更深的新建层级引入 reparse, 由写入后 provenance gate 兜底 (本流程所有目标目录均已存在)。
