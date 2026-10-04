# Resume finalizer r2 - 六文件最终交付确认

请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\resume-finalizer-r2.request.md`
角色: 接续实现者 (finalizer)。范围: 六个绝对路径。不重做旧实现, 不构建/测试/parse/lint/预处理/pack/deploy, 不再委派。
状态: **CODE_COMPLETE**。本轮无代码改动; 唯一写入为本报告。
生成时间: 2026-10-05 03:52:01 +08:00。

## 已确认

### C0. 范围与只读边界

- 已读请求文件, 契约 `G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md`, 原 `release-pipeline-worker-20261005.md` 与 `quick-pck-worker-20261005.md`。
- 本轮只读检查 (Get-Content/Select-String/Compare-Object/Get-FileHash/git status); 未构建, 未执行任何脚本分支, 未改产品文件, 未写 C:, 未接触 Steam/共享配置/游戏。

### C1. 六文件实测哈希 (与两份 worker 报告记录一致)

| # | 绝对路径 | Size | SHA256 | 编码事实 |
|---|---|---|---|---|
| 1 | `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` | 24710 | 5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279 | 无 BOM, CRLF 438, 纯 ASCII |
| 2 | `G:\omp works\.tooling\refresh-workshop-payloads.ps1` | 83057 | EBB4AC34A27C2365B7486AE1B8D9D21C8588F502282579AC8C2535D7B6789C4C | 无 BOM, LF 1522, 纯 ASCII |
| 3 | `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj` | 12887 | 769E2D0FE6B183B1B48625056D32DE104F283431B43A23D5B7BFF4A1912C98D5 | UTF-8 BOM, LF 204 |
| 4 | `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj` | 11367 | C3B273F5E6398844FCD61BD44F3EF10DF92BE8FD7D2FAEB5D71B3DBD7123EEB0 | UTF-8 BOM, LF 186 |
| 5 | `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj` | 12051 | 2D5266D5A0121E36EF81C04462C9BD95976443BA8F8BFE8A9FC701B6BE7E80E9 | UTF-8 BOM, LF 203 |
| 6 | `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj` | 10256 | 962D9EAF7EA3C208084E8C5B587D165B3F36ECC5F474C727C7B25663BE488900 | 无 BOM, LF 166 |

### C2. 四份 csproj producer 静态核对 - 通过

- 四份结构一致: `UsingTask ReadPckMetadata` (RoslynCodeTaskFactory) + `BeginQuickPckDigest` (BeforeTargets=PackPck) + `WriteQuickPckDigest` (AfterTargets=PackPck); 目标行: Perfect L155-203, MpConfigSync L138-185, HeartShake L154-201, QuriousCraftingRelics L117-164。
- 与 PckPacker 0.1.1 targets (`G:\omp works\Sts\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\build\BSchneppe.StS2.PckPacker.targets` L12-37) 对照: exit 0=成功, 2=skip (`PckPackerSkipped=true`), 其它=包内 Error。
- 门禁事实:
  - disabled: `'$(PckPackerEnabled)' == 'true'` 不满足 -> Begin/Write 都不运行, 不写 digest。
  - inner-export: `'$(IsInnerGodotExport)' != 'true'` 不满足 -> 不运行。
  - skipped: `'$(PckPackerSkipped)' != 'true'` 不满足 -> 不写 digest; Begin 若已运行先删除旧 digest (Perfect L160 / MpConfigSync L143 / HeartShake L159 / Qurious L122)。
  - failure: `'$(_PckPackerExitCode)' == '0'` 不满足 -> 不写 digest (包内已 Error 中断)。
  - 成功 pack: 断言 PCK 存在 (Perfect L171-172), 非空 (L183-184), mtime >= pack start (L187-188), mtime >= DLL (L191-192); SHA256 由 `ReadPckMetadata` 算 (L173-177), 写 ASCII (L193) 并 `ReadAllText().Trim()` 回读比对 (L196-201)。
- `CopyToModsFolderOnBuild` 未出现在 producer 条件中 (Perfect L125-127 注释明示独立; 条件串无该属性) -> 与部署独立。
- 四份 producer 块逐字符相等 (正则提取 6630 字符, 四份一致); 各 csproj 1 个 `</Project>`, 1 个 UsingTask, 1 个 Begin + 1 个 Write, 无重复定义。
- csproj 均引用 `BSchneppe.StS2.PckPacker 0.1.1`; 未覆盖 `PckPackerEnabled/PckPackerOutputPath/PckPackerSourceDir` -> 默认输出 `$(OutputPath)$(MSBuildProjectName).pck`, 与 refresh 期望的 `mod\.godot\mono\temp\bin\Release\<Id>.pck` 一致。

### C3. Build-Spire1Release.ps1 静态核对 - 通过

- Promote 顺序 (L316-432): PlanOnly 拒绝 (L317) -> Configuration 必须 Release (L318) -> Workshop 根 Assert-Under + Assert-NoReparse (L320-321) -> ShouldProcess (L322) -> canonical 目录/目标 SafeWriteTarget (L333-341) -> canonical DLL 存在 (L342-344) -> `` 必须是 canonical 路径, 拒绝 bin/publish fallback (L345-347) -> canonical DLL hash == staged payload DLL hash (L348-354) -> 重算 payload PCK hash 等于结构门禁时的 `` (L356-358) -> 临时 PCK 复制 + 回读长度/hash/mtime>=DLL (L366-371) -> 临时 digest 写 + 回读 64hex 且等于 temp hash (L372-375) -> 安装 canonical PCK 再 digest (L376-377) -> 最终回读非空/hash/mtime/digest (L378-384) -> `evidence\canonical-sync.json` (L385-398, PairAtomic=false) -> finally 仅删两个已知临时路径 (L399-405) -> Workshop 三目标 SafeWriteTarget (L407-410) -> 复制三文件 + 清理 pdb/deps.json/pck.sha256 (L411-418) -> 精确三文件 allowlist (L419-423) -> post-promote 回读 hash 相等 (L424-430)。
- PDB: 正式 payload 恰为 `Spire1.dll/Spire1.json/Spire1.pck` (L407/L412/L415/L420), 不含 PDB。
- canonical 输出路径固定 `mod\.godot\mono\temp\bin\Release\Spire1.{dll,pck,pck.sha256}` (L333-336), 无错误路径。
- `Assert-SafeWriteTarget` (L42-67): 目标必须在根内, 拒绝 C:, `\steamapps(\)`, `\mod_configs(\)`, 逐级 reparse 与 `steam_appid.txt` 标记。
- 非 promote 行为不变: canonical 同步代码全部位于 `if ($Promote)` 内。
- 与中央基线 `Build-Spire1Release.ps1.original` (15459 B) 只读 Compare-Object: 0 行删除, 123 行纯插入。

### C4. refresh-workshop-payloads.ps1 静态核对 - 通过

- Spire1 row 含 `PublishPdb = $false` (L134); `Get-RowAllowlist` (L553-571) 仅在 `PublishPdb` 为真时加入 pdb 条目 (L564-568); 其它 6 行无该键, 保持 optional PDB。
- 同 hash 分支 (L1339-1362): pck role 读 build 与 staged 真实 mtime (L1348-1356), 不同则 `sameHashNeedsCopy=true` (L1352) 继续走复制路径; 读取失败 -> `PATH_UNREADABLE` + continue (L1353-1355), 且该状态在 `` (L1436) 与 `` (L1508) 两处均 fail-closed。
- 保留的门禁: PCK_EMPTY (L642/L702/L1191), PCK_MTIME_MISMATCH (L648), PCK_STALE (L653/L1198), SHA_MISMATCH (L662), DIGEST_MALFORMED (L690/L1223), DIGEST_STALE (L705/L1239), PCK_DIGEST_MISSING (L714/L1208), 全量预检失败即停 (L1045-1050, L1060-1067, L1252-1256), held-back 门禁 (L1053-1067, L1443-1457)。
- `-VerifyOnly` 不进入 copy phase; WhatIf 短路在 Copy-Item 之前 (L1363-1366); `Hash-Of` 用 .NET SHA256 (L167-187) 无 PSModulePath 依赖。
- 与中央基线 `refresh-workshop-payloads.ps1.original` (80864 B) 只读 Compare-Object: 7 行删除全部为规格内替换 (pdb 注释 1, Spire1 row 1, pdb 条目 1, ALREADY_CURRENT 早退 2, 两个失败清单 2), 36 行插入。

### C5. 既有只读证据交叉引用 (非本轮产生)

- `G:\omp works\.tmp\workshop-prep-20261004-central\central-script-parse-ps7.json` / `central-script-parse-ps51.json`: 两脚本 parse 0 错误, SHA256 与本次实测一致 (PS 7.6.5 与 5.1.19041.6328)。
- `negative-gates-run.log` / `negative-gates-ps51-run.log`: 11/11 夹具通过 (staged Spire1 pdb 失败、mtime 同 hash 复制、digest missing/malformed/mismatch/stale 全失败、VerifyOnly/WhatIf 只读)。
- csproj 改前基线 `*.csproj.before` 的 SHA256 与 worker 报告记录一致; Perfect 剥离 producer 块后 SHA256 与改前基线完全一致 (26954607...FD2F)。
- canonical 目录 `mod\.godot\mono\temp\bin\Release` 存在, 路径链到盘根无 reparse, 链上无 `steam_appid.txt`。

### C6. 非本轮事实 (需知晓, 非六文件缺口)

- `Perfect.csproj` 相对其 git HEAD 还有一段 AFTP 集成替换 (删除 AftpDll Reference/Publicize, 改为始终编译无 AFTP 类型引用的 patch)。证据: 中央基线 `Perfect.initial-csproj-diff.patch` (2026-10-05 01:34 落盘, 早于 producer 工作), 且当前文件剥离 producer 块后 SHA 等于 worker 记录改前基线 -> 该改动在本轮 producer 工作之前已存在, 不属于六文件本轮交付, 本轮未改动。
- 四份 csproj 相对各自 git HEAD 均为未提交工作区改动 (`git status` 显示 M), 由主会话按契约 commit + push。

## 进行中

- 无。六文件最终交付确认完成; 无剩余实现动作, 零代码改动。

## 未知

- 动态证据 (主会话验证面, 本轮按要求未做): 四项目真实 Release Rebuild + producer digest 端到端 (`four-mods-rebuild-results.json` 与各 `*-release-rebuild.log` 本轮读取时尚不存在); Spire1 `-SkipBuild -Promote` 演练; 真实全量 refresh 后 `VerifyOnly` 7/7; `-GuardsOnly` VDF/held-back/文字/大小/description 门禁; r15 字节覆盖确认; 实机游戏验收。
- 未构建/未执行任何动态路径 -> 不宣称已发布、已部署或已实机验收。

## 六文件最终清单 (绝对路径 + SHA256)

| # | 绝对路径 | SHA256 | 本轮改动 |
|---|---|---|---|
| 1 | `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` | 5CBD6F25138767D57BB75D492DC07E10145D490CC20AD96C70E4D6E68ACAC279 | 无 |
| 2 | `G:\omp works\.tooling\refresh-workshop-payloads.ps1` | EBB4AC34A27C2365B7486AE1B8D9D21C8588F502282579AC8C2535D7B6789C4C | 无 |
| 3 | `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj` | 769E2D0FE6B183B1B48625056D32DE104F283431B43A23D5B7BFF4A1912C98D5 | 无 |
| 4 | `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj` | C3B273F5E6398844FCD61BD44F3EF10DF92BE8FD7D2FAEB5D71B3DBD7123EEB0 | 无 |
| 5 | `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj` | 2D5266D5A0121E36EF81C04462C9BD95976443BA8F8BFE8A9FC701B6BE7E80E9 | 无 |
| 6 | `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj` | 962D9EAF7EA3C208084E8C5B587D165B3F36ECC5F474C727C7B25663BE488900 | 无 |

## CODE_COMPLETE

- 六文件最终交付确认完成; 无缺口, 零代码改动。接续交付责任结束。
