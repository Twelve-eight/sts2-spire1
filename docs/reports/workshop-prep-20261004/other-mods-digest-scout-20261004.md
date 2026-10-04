# 其它三个模组 + QuriousCraftingRelics 的 PCK digest 缺失只读审查 (2026-10-04)

请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\other-mods-digest-scout-20261004.request.md`
唯一可写文件: 本文件。审查期间未修改产品代码, 未构建, 未部署, 未操作游戏或共享配置, 未写 C:。

## 已确认

### C1. 四个项目的 Release 输出目录都缺 `<mod>.pck.sha256` (第一条可用结论)

| Row | build dir | DLL | PCK | `.pck.sha256` |
|---|---|---|---|---|
| Perfect | `G:\omp works\Sts\sts2-perfect\mod\.godot\mono\temp\bin\Release` | yes 54272 B | yes 346341 B | no |
| MpConfigSync | `G:\omp works\Sts\sts2-mpconfigsync\mod\.godot\mono\temp\bin\Release` | yes 52224 B | yes 59143 B | no |
| HeartShake | `G:\omp works\Sts\sts2-heartshake\mod\.godot\mono\temp\bin\Release` | yes 20992 B | yes 30010 B | no |
| QuriousCraftingRelics | `G:\omp works\Sts\AutoAnthonyRelics\mod\.godot\mono\temp\bin\Release` | yes 214016 B | yes 576005 B | no |

可复现命令:

```powershell
Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-perfect\mod\.godot\mono\temp\bin\Release' -Filter '*.pck*'
Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-mpconfigsync\mod\.godot\mono\temp\bin\Release' -Filter '*.pck*'
Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-heartshake\mod\.godot\mono\temp\bin\Release' -Filter '*.pck*'
Get-ChildItem -LiteralPath 'G:\omp works\Sts\AutoAnthonyRelics\mod\.godot\mono\temp\bin\Release' -Filter '*.pck*'
```

### C2. 真实原因: PckPacker 只写 PCK, 从不写 digest; 这四个项目也没有任何 digest 生产者

- `G:\omp works\Sts\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\build\BSchneppe.StS2.PckPacker.targets` L12-39 只有 `PackPck` 目标: 调用 `StS2PckPacker.dll` 产出 `$(PckPackerOutputPath)`, 全文无 `sha256`/`hash`/`digest` 写入 (实测 grep 0 命中)。
- 四个 csproj 全文无 `sha256`/`ReadPckMetadata`/`WritePckDigest`/`UsingTask` 命中:
  - `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj` (118 行, L101-105 `CopyQuickPck` 只复制 `$(PckPackerOutputPath)`)
  - `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj` (101 行, L96-100 同上)
  - `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj` (117 行, L112-116 同上)
  - `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj` (80 行, L73-79 `CopyToModsFolder` 只复制 DLL/JSON/PDB/PCK, 无 digest)
- 对照 Spire1: `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` L366-382 (`WritePckDigestForQuickPck`) 与 L588-640 (`GodotPublish`) 才是 digest 生产者, 两个 `WriteLinesToFile` (L380/L630) 明确写出 `<mod>.pck.sha256`。

结论: 缺 digest 不是构建失败或文件被删, 而是这四个项目从未实现 digest 生产者; refresh 的 `has_pck=true` 硬契约 (L658-660, L1171-1240) 现在必然报 `PCK_DIGEST_MISSING`。

### C3. 触发条件与当前控制流 (只读实跑)

- refresh `-Only <row> -VerifyOnly` 实测 (2026-10-04, 只读): 五条命令都 exit 1 (四个目标行报 `PCK_DIGEST_MISSING`, Spire1 报 `PCK_STALE`), 且都在 PRE-COPY ARTIFACT GATE 停下, 每次只列被选行:
  - Perfect: `G:\omp works\Sts\sts2-perfect\mod\.godot\mono\temp\bin\Release\Perfect.pck.sha256: manifest has_pck=true but the build digest is absent`
  - MpConfigSync / HeartShake / QuriousCraftingRelics 同型。
- 控制流: `refresh-workshop-payloads.ps1` L1171-1200 (pre-copy gate 读 `has_pck=true` -> 查 digest -> 缺则 `Add-Finding PCK_DIGEST_MISSING` + `$preCopyArtifactFailed=$true`) -> L1243-1246 (`exit 1`, 零写入)。
- `-Only` 过滤 `$rows` (L143-150), 而 pre-write/pre-copy gate 都迭代 `$rows` (L952, L1067, L1255), 因此 `-Only X` 只评估 X 这一行 (本轮四次实测都只输出被选行的 finding)。**但是**上传入口 `workshop-push-all.ps1` L215-219 调用 refresh 时不转发 `-Only` (`& $refreshScript -VerifyOnly` / `& $refreshScript`), L267 的 pre-flight 亦然; 所以任何一次上传或 `-GuardsOnly` 都要求全部 7 行通过, 修好单行不足以解锁上传。
- 复制阶段 (L1254-1400) 现在不可达: gate 已在任何 Copy 之前退出, 因此本次审查未观察到 staging 被改写; 实测四个 staged DLL mtime 与审查前一致。

### C4. 当前 staged PCK 与 build PCK 字节不同 (修复后 refresh 会覆盖)

| Row | build PCK bytes / mtimeUtc | staged PCK bytes / mtimeUtc | 同字节? |
|---|---|---|---|
| Perfect | 346341 / 2026-10-02T07:53:07Z | 342844 / 2026-09-12T16:05:46Z | no |
| MpConfigSync | 59143 / 2026-10-02T07:53:05Z | 55630 / 2026-09-27T14:06:29Z | no |
| HeartShake | 30010 / 2026-10-02T07:49:20Z | 30010 / 2026-09-12T16:05:47Z | 同长度同 SHA256 (3043D131...) |
| QuriousCraftingRelics | 576005 / 2026-10-02T07:53:07Z | 572965 / 2026-09-23T02:45:48Z | no |

SHA256 实测: build/staged 分别为 Perfect `1B6E3989...` vs `5E1283DC...`; MpConfigSync `27D698D2...` vs `06FDE2AE...`; HeartShake 两侧同为 `3043D131...`; Qurious `5693D849...` vs `FDC1FB9B...`。

注意: refresh 的 verify 阶段要求 staged PCK mtime 等于 build PCK mtime (L638-639 `PCK_MTIME_MISMATCH`), 而当前四行 mtime 全不相同; 所以即使补上 digest, 也还需要一次真正的 refresh 复制 (Copy-Item 保留源 mtime) 才能通过。只写 digest 不会让 `-VerifyOnly` 通过。

### C5. 当前 staged DLL 也是旧字节 (与 build DLL 不同)

| Row | build DLL bytes / sha256 | staged DLL bytes / sha256 |
|---|---|---|
| Perfect | 54272 / `46E3CC52...` | 47104 / `25F342AB...` |
| MpConfigSync | 52224 / `56FCF7F6...` | 38400 / `2EB2738D...` |
| HeartShake | 20992 / `3738C328...` | 16384 / `1271C1EB...` |
| QuriousCraftingRelics | 214016 / `FC1A0E13...` | 207872 / `347AF24D...` |

含义: 补齐 digest 后的正常 refresh 会同时刷新 DLL 和 PCK, 这是预期行为; 若只想补 digest 而不动 staging, 则 staged 与 build 的 SHA_MISMATCH/PCK_MTIME_MISMATCH 仍会失败 (即"只生成 digest"不能替代 refresh 的字节同步)。

### C6. 四种修复选项的风险区分

- 选项 A (重新构建 PCK): 对这四个项目而言 PCK 由 PckPacker 从 `mod\<mod>\` 资产目录生成; 重新构建会得到与当前 build PCK 相同输入的新字节, 只有当资产源确实需要更新时才必要。它不是 digest 缺失的根因修复, 成本最高。
- 选项 B (只生成由当前 PCK 字节计算的 digest): 这是允许的最小修复方向, 但必须满足 L695-696 `DIGEST_STALE` (digest mtime 不得早于 PCK mtime) 与 L1213-1231 同型检查; 由 SHA256 计算后立即写 `<buildDir>\<mod>.pck.sha256` 即满足。该选项只补证据记录, 不伪造字节, 不改 refresh 脚本。它不足以让 gate 通过 (见 C4/C5), 必须再跑一次 refresh。
- 选项 C (改 refresh gate 契约, 把 digest 从必需降为可选): 会让 `has_pck=true` 的发布链失去"PCK 由受控生产者产出"的证明; `G:\omp works\.tmp\sts2-fix-20261002\reports\A08-workshop-provenance.md` L196-197 记录 digest 目前无消费者、仅声明性, 降级后只剩 SHA256 对账, 无法区分"本次构建产物"与"手工/旧字节"。不推荐, 且与 Spire1 的 A09 契约 (`Spire1.csproj` L327-351) 不一致。
- 选项 D (改 `-Only` 行为, 让上传只评估被选行): 属于放宽发布安全边界: `workshop-push-all.ps1` L468-480 的 `-Only` 只收窄上传集, 其 pre-flight 仍对全部 `$items` 跑 refresh (L215-219, L267); refresh 侧单独放宽会让"某一行坏掉时仍能上传另一行"成为默认行为, 与 refresh L36-38 的 INVARIANT (覆盖 push-all 每一项) 及 L184-196 的 fail-closed 设计相悖。

### C7. 最小安全修复范围 (不改 refresh 脚本, 不改 -Only)

推荐路径 = 选项 B (每个项目各自的 csproj 内加一个 digest 生产者) + 中央 refresh 一次。参考 Spire1 的最小实现形状:

- `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` L366-382 (`WritePckDigestForQuickPck`): `ReadPckMetadata` 读 `$(OutputPath)$(MSBuildProjectName).pck` -> 断言长度非零 -> `WriteLinesToFile File="$(OutputPath)$(MSBuildProjectName).pck.sha256" Lines="$(_QuickPckSha256)" Encoding="ASCII"`。
- 同样的 `UsingTask ReadPckMetadata` (L549-573) 可以按项目复制到四个 csproj, 或抽到一个共享 `.props` 里 import; 需要新增/修改的文件仅:
  - `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj`
  - `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj`
  - `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj`
  - `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj`
- digest 记录格式: Spire1 当前记录是 66 字节 (64 hex + CRLF), refresh 只做 `Trim()` 后正则 `^[0-9A-Fa-f]{64}$` (L1204/L1213), 两种格式都能过; 若用 `Set-Content -NoNewline` 得 64 字节也可。
- 中央验证顺序 (每一步都有可复现命令):
  1. 重建四个项目 (强制不部署, 只产 build 目录): `dotnet build mod\<Mod>.csproj -c Release -p:CopyToModsFolderOnBuild=false`
  2. 逐项确认 digest 存在且与 PCK 自洽: 对每个 build dir 比较 `Get-FileHash <mod>.pck` 与 digest 文本。
  3. 逐项只读 gate: `powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only <Mod>` (当前会从 `PCK_DIGEST_MISSING` 变为 `PCK_MTIME_MISMATCH`/`SHA_MISMATCH`, 因为 staging 仍是旧字节)。
  4. 中央 refresh (会写 staging): `powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1"` -> 期望 exit 0, 四个 row 的 DLL/PCK 从 build 复制到 staging。
  5. 复跑只读 gate 确认稳定: `... -VerifyOnly` -> 期望 `VERIFY RESULT: OK`。
  6. 上传入口预检: `powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\workshop-push-all.ps1" -GuardsOnly` (不联系 Steam)。

### C8. 发布契约原文位置 (供对照)

- `refresh-workshop-payloads.ps1` L21-34 (PUBLISH ALLOWLIST 契约: digest 是 record-only, has_pck=true 时必需)
- 同文件 L550-562 (`Get-RowAllowlist`: digest `Required=$false` 但在 L658-705 被 has_pck=true 提升为硬性)
- 同文件 L1171-1240 (pre-copy gate 的 PCK 与 digest 检查)
- 同文件 L1406-1458 (final gate 与 exit code)
- `G:\omp works\.tooling\workshop-push-all.ps1` L184-268 (payload provenance gate 的调用与配对断言)

### C9. `-Only` 实测结论: refresh 侧只过滤行; 上传入口不转发 `-Only`, 仍要求全量通过 (权威复现)

2026-10-04 实测五条只读命令 (全部 exit 1, 且 gate 输出中只列被选行):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only Perfect
# exit 1: Perfect PCK_DIGEST_MISSING
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only MpConfigSync
# exit 1: MpConfigSync PCK_DIGEST_MISSING
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only HeartShake
# exit 1: HeartShake PCK_DIGEST_MISSING
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only Qurious
# exit 1: QuriousCraftingRelics PCK_DIGEST_MISSING
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only Spire1
# exit 1: Spire1 PCK_STALE
```

行号级原因 (refresh 侧): `$rows` 在 L143-150 被 `-Only` 过滤, 而 pre-write gate (L952)、pre-copy artifact gate (L1067) 与 copy phase (L1255) 都迭代 `$rows`; 任一被评估行失败即 L1036-1041 / L1243-1246 exit 1。所以 refresh `-Only` 的语义是"只检查这一行", 不是"只检查这一行并允许其它行失败"。

上传侧原因 (真正阻塞单行上传): `G:\omp works\.tooling\workshop-push-all.ps1` L215-219 调用 refresh 时**不转发** `-Only` (`& $refreshScript -VerifyOnly *> $gateLog` / `& $refreshScript *> $gateLog`), L267 `Invoke-PayloadProvenanceGate $guardsOnly "pre-flight"` 同样全量执行; 因此 `-Only <Mod>` 只能收窄 L468-480 的 upload 命令列表, 无法绕过全量 gate。修好一个 row 不足以解锁任何上传。

### C10. 四个 build PCK 的当前 mtime 全部晚于各自 DLL (PCK_STALE 对它们不成立)

实测 (2026-10-04):

| Row | build PCK mtimeUtc | build DLL mtimeUtc | PCK >= DLL? |
|---|---|---|---|
| Perfect | 2026-10-02T07:53:07.1494430Z | 2026-10-02T07:53:05.4759448Z | yes |
| MpConfigSync | 2026-10-02T07:53:05.6349944Z | 2026-10-02T07:53:05.3569442Z | yes |
| HeartShake | 2026-10-02T07:49:20.1159985Z | 2026-10-02T07:49:20.0040025Z | yes |
| QuriousCraftingRelics | 2026-10-02T07:53:07.7356186Z | 2026-10-02T07:53:06.0157746Z | yes |

因此这四个 row 当前唯一的 pre-copy 硬失败是 `PCK_DIGEST_MISSING`; `PCK_STALE` 只命中 Spire1 (28MB build PCK mtime 2026-10-02T22:28:21Z, 早于 2026-10-04 DLL)。

### C11. Perfect 的 freshness scan 前置阻塞 (补强 C7 步骤 1)

- 文件: `G:\omp works\Sts\sts2-perfect\mod\Perfect\localization\eng\settings_ui.json` 与 `zhs\settings_ui.json`, mtimeUtc = 2026-10-03T01:41:09Z。
- 同一扫描面内更新的还有 `mod\PerfectCode\MainFile.cs` (2026-10-03T01:40:45Z), `mod\PerfectCode\PerfectConfig.cs` (2026-10-03T01:40:04Z), `mod\PerfectCode\Patches\TimeWarpPlayerOwnedPatch.cs` (2026-10-03T01:38:38Z)。
- 对照: Perfect build DLL mtimeUtc = 2026-10-02T07:53:05Z。
- 触发点: refresh L853-877 (`REBUILD_REQUIRED`), 触发条件为 repo 内 `*.cs`/`*.csproj`/`*.props`/`*.json` (排除 obj/bin/.godot/.tmp/.nuget/.dotnethome/research/tools) 的 mtime 晚于 build DLL mtime。
- 结论: 即使 Perfect 补上 digest, 也需先重新构建, 否则 refresh 仍会以 `REBUILD_REQUIRED` 拒绝。MpConfigSync / HeartShake / QuriousCraftingRelics 当前无同型 newer-source 命中 (实测 0)。

### C12. staged 目录 mtime 与只读 gate 语义 (反证"gate 已改写 staging")

- 四个 staging 目录 mtimeUtc: Perfect 2026-09-08T19:17:55Z, MpConfigSync 2026-09-10T17:06:37Z, HeartShake 2026-09-13T22:28:27Z, QuriousCraftingRelics 2026-09-11T22:06:20Z。
- 四个 staged DLL mtimeUtc 与审查开始时一致 (未因 `-VerifyOnly` 变化), 佐证 L1243-1246 在任何 Copy 之前退出, 只读语义成立。

### C13. staging 里还有 PDB (allowlist 允许, 但会随 refresh 一起刷新)

- 四个 staging 目录当前文件 (2026-10-04 实测):
  - Perfect: `Perfect.dll` 47104, `Perfect.json` 416, `Perfect.pck` 342844, `Perfect.pdb` 22816
  - MpConfigSync: `MpConfigSync.dll` 38400, `MpConfigSync.json` 464, `MpConfigSync.pck` 55630, `MpConfigSync.pdb` 24084
  - HeartShake: `HeartShake.dll` 16384, `HeartShake.json` 495, `HeartShake.pck` 30010, `HeartShake.pdb` 22640
  - QuriousCraftingRelics: `QuriousCraftingRelics.dll` 207872, `QuriousCraftingRelics.json` 598, `QuriousCraftingRelics.pck` 572965, `QuriousCraftingRelics.pdb` 83764
- refresh allowlist (L550-562) 把 PDB 列为可选 payload (`Required=$false`, `Payload=$true`); 若 build 目录存在 PDB (四个项目都存在), refresh 会把它一并复制到 staging。这是当前契约的预期行为, 不属于本次修复范围, 但会影响上传内容的字节数。
- Spire1 的发布契约 (`G:\omp works\Sts\sts2-spire1\docs\RELEASE-PACKAGE-CONTRACT-20261003.md` L13-20) 只把 DLL/JSON/PCK 列为正式 payload 并明确排除 PDB; 该契约目前只适用于 Spire1, 四个其它项目的 Workshop staging 仍按 refresh 的 PDB 可选规则工作。

## 进行中

- 已实跑五个 `-Only` 只读 gate (Perfect/MpConfigSync/HeartShake/Qurious/Spire1) 并落盘上面的输出摘要; 未改动任何文件 (四个 build 目录复查仍无 `*.sha256`)。
- 已排除的可能原因: (a) digest 被误删 -> 四个项目 csproj 无生产者, 且 `.godot` 下无任何 `*.sha256`; (b) refresh 脚本误报 -> 四个 build dir 实测确无文件; (c) manifest 缺字段 -> 四份 manifest 都明确 `"has_pck": true`。
- 待补充 (半成品): 选项 B 的具体 csproj 补丁未实施, 未构建验证; 上述最小修复范围只是行号级方案。

## 未知

- [P1] 未做实机 (游戏进程) 验证: 四个项目的 staging 刷新后 DLL/PCK 是否能被测试副本加载、设置页/战斗行为是否符合预期, 均未验证。
- [P1] 未验证四个项目重新构建后的 PCK 与当前 build PCK 是否字节一致 (PckPacker 的跨输入确定性未验证; Spire1 只对同输入有两个同 SHA256 样本)。
- [P2] Perfect 的 `mod\Perfect\localization\{eng,zhs}\settings_ui.json` (mtime 2026-10-03T01:41Z) 晚于 build DLL (2026-10-02T07:53Z); refresh L853-877 的 freshness scan 会报 `REBUILD_REQUIRED`。即使补上 digest, Perfect 仍需先重新构建。
- [P2] 未验证四个项目重构建是否会写 C: 或部署到 Steam: 构建命令必须显式带 `-p:CopyToModsFolderOnBuild=false`, 本轮未执行构建。
- [P2] 未验证 `-Only` 放宽方案 (选项 D) 是否还有其它消费者依赖全量评估; 本审查只确认 `workshop-push-all.ps1` 的配对断言按 VDF/contentfolder 全集工作。
- [P3] 未验证四个项目的 `.gitignore` 是否会把 `*.pck.sha256` 当作可提交产物; 若加生产者, 需要确认 build 目录仍在忽略范围内且无需提交 digest 文件。

---

边界声明: 本报告所有结论基于 2026-10-04 的只读文件读取与只读 gate 实跑; 未修改产品代码, 未构建, 未部署, 未启动游戏, 未联系 Steam, 未改共享 mod_configs, 未写 C:。
