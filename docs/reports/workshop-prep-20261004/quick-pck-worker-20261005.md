# 四项目 quick PCK digest producer 实现报告 (2026-10-05)

请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\quick-pck-worker-20261005.request.md`
角色: implementation worker Tesla (mechanical four-csproj slice)。唯一可写代码文件为四份 csproj; 唯一可写报告为本文件。
边界: 不改脚本 (Cicero 的切片), 不构建, 不 lint, 不测试, 不打包, 不部署, 不联系 Steam, 不改共享配置, 不写 C:。Cicero 的脚本审查须等 coordinator 的真实 wait 门禁; 本文件不审脚本。

## 已确认

- 基线: 四份 csproj 均无 digest 生产者 (全文无 `ReadPckMetadata`/`pck.sha256`/`WritePckDigest` 命中; 与 2026-10-04 scout 报告 C2 一致)。
- 基线 hash 与编码 (2026-10-05 读取):
  - `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj` 6251 B sha256=26954607C5C82D3EBDF1137FACF3D5AB470DE7E73638E4DBE38AEDCA7CB3FD2F, UTF-8 BOM, LF, 尾字节 0x0A。
  - `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj` 4731 B sha256=41A80D4B2D947A558799DD25565FC74D6E7948DC6288EFEF5585BA4694FB7161, UTF-8 BOM, LF, 尾字节 0x3E (无末尾换行)。
  - `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj` 5415 B sha256=6F6189A9E337A7B39967FCB132F61627BC7F11C784EFFE0A32A70D61E23D388B, UTF-8 BOM, LF, 尾字节 0x0A。
  - `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj` 3620 B sha256=837B1EFCA34CE49C3C34A2E89A5FFAF289BC4D9712DADCF7F04AABBB80090E90, UTF-8 无 BOM, LF, 尾字节 0x0A。
- PckPacker 0.1.1 行为 (G:\omp works\Sts\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\build\BSchneppe.StS2.PckPacker.targets L5-14, L16-37): `PackPck` 目标条件 `PckPackerEnabled == true And Exists(PckPackerSourceDir)`; Exec 输出 `_PckPackerExitCode` (0=成功, 2=跳过) 与 `_PckPackerOutput`; 非 0/2 由包内 Error 直接失败; 无任何 digest 写入。默认 `PckPackerOutputPath=$(OutputPath)$(MSBuildProjectName).pck`。
- 四份 csproj 均未覆盖 `PckPackerEnabled`/`PckPackerOutputPath`/`PckPackerSourceDir`, 且 `CopyQuickPck`/`CopyToModsFolder` 与 digest 无关 (不提供可复用 producer)。

### F1. Perfect.csproj producer added (2026-10-05)

- File: `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj` (6251 B -> 12887 B, sha256 769E2D0FE6B183B1B48625056D32DE104F283431B43A23D5B7BFF4A1912C98D5).
- Added before `</Project>`: narrow `ReadPckMetadata` RoslynCodeTaskFactory inline task (copy of Spire1.csproj L549-573), `BeginQuickPckDigest` (BeforeTargets=PackPck; captures `$([System.DateTime]::UtcNow.Ticks)` pack start, deletes old `$(PckPackerOutputPath).sha256`), `WriteQuickPckDigest` (AfterTargets=PackPck; condition requires PckPackerEnabled=true, IsInnerGodotExport!=true, PckPackerSkipped!=true, _PckPackerExitCode=0; asserts PCK exists, non-empty, mtime >= pack start and >= `$(TargetPath)` DLL; hashes real bytes; WriteLinesToFile ASCII + ReadAllText readback equality).
- Independent of CopyToModsFolderOnBuild; PckPackerEnabled=false / inner export produce no activity.
- Preserved: UTF-8 BOM, LF-only, single `</Project>`, trailing newline; only-insertion invariant verified by removing the inserted block and comparing to the pre-edit bytes.
- Not verified by me: MSBuild execution (no build allowed in this slice).
### F2. MpConfigSync.csproj producer added (2026-10-05)

- File: `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj` (4731 B -> 11367 B, sha256 C3B273F5E6398844FCD61BD44F3EF10DF92BE8FD7D2FAEB5D71B3DBD7123EEB0).
- Same producer block as F1: `ReadPckMetadata` inline task, `BeginQuickPckDigest` (BeforeTargets=PackPck; pack-start ticks + old digest deletion), `WriteQuickPckDigest` (AfterTargets=PackPck; exit-code 0 + not skipped; exists/non-empty/mtime-vs-pack-start/mtime-vs-DLL assertions; real-byte SHA256; ASCII write + readback equality).
- Preserved: UTF-8 BOM, LF-only, single `</Project>`, and the original file's no-trailing-newline ending (verified: text still ends exactly with `</Project>`).
- Not verified by me: MSBuild execution (no build allowed in this slice).
### F3. HeartShake.csproj producer added (2026-10-05)

- File: `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj` (5415 B -> 12051 B, sha256 2D5266D5A0121E36EF81C04462C9BD95976443BA8F8BFE8A9FC701B6BE7E80E9).
- Same producer block as F1/F2.
- Preserved: UTF-8 BOM, LF-only, single `</Project>`, trailing newline. Independent reversal check: removing the inserted block and re-encoding reproduces the pre-edit SHA256 `6F6189A9E337A7B39967FCB132F61627BC7F11C784EFFE0A32A70D61E23D388B` exactly.
- Not verified by me: MSBuild execution (no build allowed in this slice).
### F4. QuriousCraftingRelics.csproj producer added (2026-10-05)

- File: `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj` (3620 B -> 10256 B, sha256 962D9EAF7EA3C208084E8C5B587D165B3F36ECC5F474C727C7B25663BE488900).
- Same producer block as F1/F2/F3.
- Preserved: no-BOM UTF-8, LF-only, single `</Project>`, trailing newline; only-insertion invariant verified against the pre-edit bytes.
- Not verified by me: MSBuild execution (no build allowed in this slice).
## 进行中

- 状态: 四份 csproj 实现完成并落盘; 无剩余实现动作。中央构建/测试由主会话执行。

## 已完成的只读静态校验 (非构建, 非 lint)

- MSBuild 预处理 (read-only evaluation, `dotnet msbuild <proj> -pp:<tmp>`): 四份 csproj 全部 exit=0, 无输出错误。这是真实 MSBuild 求值, 但不编译、不打包、不执行任何 target。
- 预处理输出证实: 四份中 `PackPck` 来自 PckPacker 包 (`AfterTargets="Build"`, `Condition="'$(PckPackerEnabled)' == 'true' And Exists('$(PckPackerSourceDir)')"`); 我方 `BeginQuickPckDigest` (BeforeTargets=PackPck) 与 `WriteQuickPckDigest` (AfterTargets=PackPck) 均被求值并保留条件原文; `ReadPckMetadata` 在每份中出现 2 次使用 (PckPackerOutputPath 与 TargetPath), 且都位于 `WriteQuickPckDigest` 内; `WriteLinesToFile` 与 ReadAllText 回读也都在该 target 内。
- 只读 XML 解析: 四份均 well-formed; 目标集合 = 原有目标 + BeginQuickPckDigest + WriteQuickPckDigest (Perfect 8 个, MpConfigSync/HeartShake 7 个, Qurious 5 个), UsingTask=1。
- 字节级回归 (反向剥离插入块并重新编码后比对原始 SHA256): 四份全部 match original (Perfect 26954607..., MpConfigSync 41A80D4B..., HeartShake 6F6189A9..., Qurious 837B1EFC...), 证明除插入块外零改动。
- 编码保持: Perfect/MpConfigSync/HeartShake 仍为 UTF-8 BOM; Qurious 仍为 UTF-8 无 BOM; 四份 CR 字节数=0 (LF-only); 末尾换行语义与改前一致 (MpConfigSync 保持无末尾换行)。
- 临时预处理文件写到 `G:\tmp` (非 C:, 非仓内), 不构成产品改动。
- 未做: 构建, 运行 MSBuild 任何 target, 打包, 部署, 测试, Steam 接触, 共享配置改动。
## 未知

- 未实际执行构建: MSBuild 求值通过不等于编译/打包通过; 具体 target 执行顺序 (BeginQuickPckDigest -> PackPck -> WriteQuickPckDigest) 在真实构建中的行为未由本轮验证, 由中央构建验证。
- 未验证真实 `_PckPackerExitCode`/`PckPackerSkipped` 取值路径 (成功 0 / 跳过 2) 在本机 PckPacker 0.1.1 下的实际表现; 条件依据包 targets L12-37 的静态阅读。
- 未验证四项目重建后 PCK 与当前 build PCK 是否字节一致; 未验证 digest 与 refresh 门禁 (PCK_MTIME_MISMATCH / SHA_MISMATCH) 的端到端配合。
- 未做实机/运行时验证 (游戏加载、设置页、战斗行为)。
- 未审查 Cicero 的两个脚本 (待 coordinator 的真实 wait 门禁; 本文件不构成脚本监督)。

## CODE_COMPLETE

- 已完成四份 csproj 的 quick PackPck digest producer 实现, 绝对路径:
  - `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj`
  - `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj`
  - `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj`
  - `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj`
- 无其它产品文件改动; 未构建/测试/打包/部署。