# Spire1 canonical build artifact 只读审查 (2026-10-04)

请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\spire1-canonical-build-scout-20261004.request.md`
唯一可写文件: 本文件。审查期间未修改产品代码/构建/部署/游戏/共享配置; 未启动游戏, 未接触 Steam/SteamCMD/共享 mod_configs。

## 已确认

### C1. 当前 PCK/DLL 文件证据 (2026-10-04 读取)

| 路径 | Bytes | LastWriteTimeUtc | SHA256 |
|---|---|---|---|
| `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck` | 19,669,354 | 2026-10-04T06:32:59.5959673Z | 70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79 |
| `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.dll` | 900,608 | 2026-10-04T06:12:02.4436245Z | 8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06 |
| `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.json` | 548 | 2026-09-29T18:01:48.7059491Z | CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305 |
| `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.pck` | 28,866,294 | 2026-10-02T22:28:21.2991350Z | CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E |
| `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.pck.sha256` | 66 (64 hex + CRLF) | 2026-10-02T22:28:22.5361534Z | 内容 = CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E |
| `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll` | 900,608 | 2026-10-04T06:12:02.4436245Z | 8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06 |

结论: staging 的 clean 19MB PCK 与 build 目录的 28MB PCK 不是同一份字节; build 目录 digest 记录只与 28MB PCK 自洽。staging DLL 与 build 目录 DLL 字节相同 (同一 SHA256)。

### C2. clean 19MB PCK 有两次独立产出, 字节一致 (PckPacker 0.1.1 本机可复现)

| 产物 | mtimeUtc | Bytes | SHA256 |
|---|---|---|---|
| `G:\omp works\.tmp\spire1-release-r15-20261004-central\payload\mods\Spire1\Spire1.pck` | 2026-10-04T06:12:29.3975657Z | 19,669,354 | 70CCBB4D...51C79 |
| `G:\omp works\.tmp\spire1-release-r15-promote-20261004\payload\mods\Spire1\Spire1.pck` | 2026-10-04T06:32:59.5959673Z | 19,669,354 | 70CCBB4D...51C79 |
| `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck` | 2026-10-04T06:32:59.5959673Z | 19,669,354 | 70CCBB4D...51C79 |

同一 source asset tree 两次独立打包得到同一 SHA256 (两次 staging 目录不同、mtime 不同)。支持"PckPacker 对同一输入字节确定", 但不等于对所有输入都确定; 见 U1。

### C3. Build-Spire1Release.ps1 的 clean PCK 只写入 .tmp payload, 不写 build 目录

文件: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` (315 行)

- L218-231: `dotnet build` 参数含 `-p:CopyToModsFolderOnBuild=false`、`-p:IncludeHeldBackLayers=false`、`-p:PckPackerEnabled=false`; 本次构建明确不产生/不部署 PCK 副作用。
- L233-240: DLL 候选第一项即 `mod\.godot\mono\temp\bin\Release\Spire1.dll`; 命中后复制到 `.tmp` payload (只读源, 不写 build 目录)。
- L242-246: 只把 staging 资产树 `$stageAssetRoot` 交给 packer, 输出 `$payloadModsRoot\Spire1.pck`; 该 clean PCK 只存在于 `.tmp` 下。
- L248-256: 对 payload PCK 计算 SHA256, 写入 `.tmp\...\evidence\Spire1.pck.sha256`。
- L258-265: 对 payload PCK 运行 `tools\release\Verify-Spire1Pck.ps1`, 输出 `evidence\pck-structure.json`。
- L267-281: 生成 `release-manifest.json` (Payload 三文件 + hash, PayloadRoot 指向 .tmp)。
- L283-287: 对 DLL 运行 assemblyref/manifest/typedef 门禁。
- L289-309: `-Promote` 只把 payload 三文件复制到 `workshop\content\Spire1`, 并删除 stale `Spire1.pdb`/`Spire1.deps.json`/`Spire1.pck.sha256`; 没有任何一步把 clean PCK/digest 写回 `mod\.godot\mono\temp\bin\Release\`。

### C4. refresh-workshop-payloads.ps1 的 provenance 契约以 build 目录为唯一 canonical 来源

文件: `G:\omp works\.tooling\refresh-workshop-payloads.ps1` (1494 行)

- L134: Spire1 row `Build = "mod\.godot\mono\temp\bin\Release\Spire1.dll"`, `Stage = "workshop\content\Spire1\Spire1.dll"`, `Manifest = "mod\Spire1.json"`, `Vdf = "workshop\workshop_upload.vdf"`。
- L550-562: 发布 allowlist 由 manifest 决定: manifest/dll/pck 必选 (has_dll/has_pck=true), pdb 可选 payload, `.pck.sha256` 为 record-only 不发布。
- L626-651: PCK reconciliation: build PCK 必须非空、长度与 staged 相同、mtime 与 staged 相同、build PCK mtime 不得早于 build DLL mtime (PCK_STALE)、staged 与 build SHA256 相同。
- L658-706: has_pck=true 时 build 目录 `<mod>.pck.sha256` 为硬性要求; 缺失 (L704-705)、空、非 64-hex、与 PCK 不匹配、mtime 早于 PCK 均 fail closed。
- L1060-1065 + L1171-1246: PRE-COPY ARTIFACT GATE 在任何复制前验证所有源; 任一失败即 `exit 1`, 并打印 "no file was written"。
- L1301-1349 (copy direction 1): staging 每个 payload 文件以 build 目录同名文件为源; SHA 不同即 `Copy-Item -LiteralPath $source -Destination $staged.FullName -Force` (L1339) 覆盖 staging。
- L1383-1399 (copy direction 2): staging 缺失的 required payload 从 build 目录补。

结论: refresh 的 canonical 是 build 目录; staging 只是 build 的镜像。让 refresh 通过而不降门禁, 必须让 build 目录里就是 clean PCK + 其 digest。

### C5. 当前 refresh 实际状态: PCK_STALE, fail-closed (实跑证据)

命令 (只读, 未复制任何文件):
```
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only Spire1
```
输出 (2026-10-04 21:5x 本地时间):
```
PRE-COPY ARTIFACT GATE: refusing to refresh staging (no file was written):

Item   Status    Detail
----   ------    ------
Spire1 PCK_STALE G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.pck: build PCK mtime 2026-10-0...
```
exit code = 1。

`-Only Spire1` 只评估 Spire1 一行 (L143-150 把 `$rows` 过滤为匹配行; 实测输出只有 Spire1 `PCK_STALE`, 见下)。

**重要区分**: 直接跑 `refresh-workshop-payloads.ps1 -Only Spire1` 是单行检查; 但上传入口 `workshop-push-all.ps1` 在 L215-219 调用 refresh 时**不传 `-Only`**, 因此 push 路径上的 provenance gate 会评估全部 7 行。实测 (2026-10-04, 只读):

```
# refresh -Only Spire1  -> 只有 Spire1 PCK_STALE, exit 1
# refresh (无 -Only)     -> Spire1 PCK_STALE + Perfect/MpConfigSync/HeartShake/QuriousCraftingRelics PCK_DIGEST_MISSING, exit 1
```

`G:\omp works\.tmp\workshop-payload-gate.log` (mtime 2026-10-04T12:55:43.8582319Z, 798 bytes) 记录的是后一种全量结果。结论: Spire1 单项修复只解锁 `refresh -Only Spire1`; 解锁实际上传还须让 push 路径的全量 gate 通过, 见 P1。

### C6. PCK_STALE 的成因是 mtime 语义, 不是字节损坏

- build 目录 DLL mtime = 2026-10-04T06:12:02.4436245Z (本次 Release 编译)。
- build 目录 PCK mtime = 2026-10-02T22:28:21.2991350Z (更早的 PackPck 产物, 即 28MB 完整资产 PCK)。
- clean 19MB PCK 在 .tmp 的 mtime 均晚于本次 DLL (06:12:29 与 06:32:59)。
- 当前 PCK_STALE 只是"build 目录里躺着一份旧 PCK"的直接后果, 与 clean PCK 的字节无关。

### C7. 发布契约与脚本现状的缺口

文件: `G:\omp works\Sts\sts2-spire1\docs\RELEASE-PACKAGE-CONTRACT-20261003.md` (95 行)

- L11-15: 正式 payload 只允许 `Spire1.dll`、`Spire1.json`、`Spire1.pck`。
- L20: `Spire1.pck.sha256` 明确"仅构建证据, 保留在 staging/evidence", 不是正式 payload。
- L73: "结构门禁失败时禁止 `-Promote`"。
- L75-88: 重建流程 1-10 步; 第 7 步生成 payload manifest/文件清单/SHA256/PCK digest; 第 10 步验证通过后才显式 `-Promote`。
- L90-94: 未验证边界: 本机无 MegaDot, 未做 Godot export-pack 实机验证, 需在测试副本确认加载/旧存档运行历史。

缺口: 该契约定义了"payload 不含 digest"与"脚本默认只写 .tmp", 但没有规定 clean PCK/digest 如何进入 refresh 所消费的 build 目录。当前脚本因此无法既满足契约、又让 provenance gate 通过。

### C8. push-all 的 provenance gate 只看 staging 目录配对, 不看 PCK 大小/资产清单

文件: `G:\omp works\.tooling\workshop-push-all.ps1` (全文 `pck` 大小写不敏感匹配 0 次)

- L184-199: refresh 是 mandatory、无 bypass。
- L206-265: `Invoke-PayloadProvenanceGate` 调 refresh (L215-219), 要求 exit 0 (L224-228), 解析 ITEM 行 (L231-238), 把每个 VDF 的 contentfolder 与该次 verified staging 目录配对 (L245-261); 不匹配即 exit 12 (L268)。
- L270-296: 独立的 held-back 层字节扫描 (读 staging DLL, 不读 PCK)。
- L173: Spire1 上传行指向 `workshop\workshop_upload.vdf`。

结论: 一旦 refresh 通过, push-all 不会发现"PCK 是 28MB 完整资产版"这类回归; 唯一的 PCK 内容门禁是 Build-Spire1Release.ps1 L258-265 对 .tmp payload 调用的 `Verify-Spire1Pck.ps1`, 它不检查 build 目录的那份 PCK。

### C9. refresh 的 ITEM 输出字段顺序 (修复方案需要)

- L1470-1475: 每行 `ITEM` + Name + stageDir + contentFolder + vdfPath。
- push-all L234-237 用 `parts[3]` 作为 contentfolder 键, 与 L1464-1469 注释一致。

### C10. 测试副本现状 (只读, 未修改)

| 路径 | DLL Bytes | DLL mtime | PCK Bytes | PCK mtime | 与 r15 clean 关系 |
|---|---|---|---|---|---|
| `E:\Slay the Spire 2\mods\Spire1\Spire1.dll` | 770,048 | 2026-10-03 06:26 | 28,865,174 | 2026-09-30 01:45 | 旧, 非 r15 |
| `G:\omp works\Sts\_runtime\sts2-test-client-B\mods\Spire1\Spire1.dll` | 616,448 | 2026-10-02 04:39 | 28,865,174 | 2026-10-02 04:40 | 旧, 非 r15 |

两处都仍有 `Spire1.pdb`。当前 r15 clean payload 未部署到这两个测试副本; 本次审查未部署。

### C11. freshness 扫描当前 0 命中

按 refresh L853-877 的同一规则 (排除 `obj|bin|.godot|node_modules|.tmp|.nuget|.dotnethome|research|tools`, 排除 held-back 源) 扫描 `.cs/.csproj/.props/.json`: 没有文件 mtime 晚于 build DLL 的 2026-10-04T06:12:02.4436245Z。当前 build DLL 与工作树源码之间不存在 REBUILD_REQUIRED 级差异 (注意工作树 dirty, HEAD 不等于工作树内容, 见 DEVLOG L2948)。

### C12. build-gates 对当前 DLL 三项全 PASS (已有证据)

`G:\omp works\.tmp\spire1-release-r15-promote-20261004\evidence\release-gates.json`: `passed=true`; `assemblyref-forbidden` PASS; `manifest-consistency` PASS (二进制引用 mod 程序集: BaseLib; manifest 依赖: BaseLib); `typedef-forbidden` PASS; `dllSha256=8C7CA3DB...2F06`, `dllLength=900608`。该 JSON 与本次读到的 build DLL 同一 SHA256。

### C13. Verify-Spire1Pck.ps1 的门禁强度 (修复方案复用点)

`G:\omp works\Sts\sts2-spire1\tools\release\Verify-Spire1Pck.ps1` (189 行):
- L96-102: 强制 Godot 4.5.1 / format v3 / PACK_REL_FILEBASE / 112 字节头与 fileBase。
- L104-106: directory offset 必须在文件内。
- L112-127: 条目路径安全 (无 `..`、无绝对路径、无冒号)、无重复、32 字节对齐、数据区在文件内。
- L141-145: PCK 路径集合必须恰好等于 asset-manifest 推导集合。
- L147-159: 每项 MD5 与数据一致; 所有 `.godot/imported/*.ctex` 必须 `GST2` 开头。
- L162-181: 输出 `Spire1ReleasePckManifest.v1` JSON; 当前 `pck-structure.json` = `SourceFiles:744, ExpectedEntries:1464, ActualEntries:1464, CtexEntries:720, ImportEntries:720, JsonEntries:24, Md5Verified:true`。

### C14. asset allowlist 规模 (修复方案的输入身份)

`.tmp\spire1-release-r15-promote-20261004\evidence\asset-manifest.json`: `SourceFiles=1010, Kept=744, Excluded=266, KeptBytes=33,803,542, ExcludedBytes=17,127,986`; ModelStems: cards=220, relics=24, potions=2。clean PCK 由 744 个保留资产 (720 PNG + 24 JSON) 打包; 266 个被排除资产 (约 17.1MB) 是 28MB -> 19MB 差距的主要来源。排除依据是静态源码审计 (契约 L42-56)。

### C15. 其它 row 的 build 目录现状 (只读; 属另一 request 范围, 此处只登记对 Spire1 上传的阻塞)

| Row | build dir | DLL | PCK | .pck.sha256 |
|---|---|---|---|---|
| Spire1 | `sts2-spire1\mod\.godot\mono\temp\bin\Release` | yes | yes (28MB, stale) | yes (与 28MB 自洽) |
| Perfect | `sts2-perfect\mod\.godot\mono\temp\bin\Release` | yes | yes | no |
| MpConfigSync | `sts2-mpconfigsync\mod\.godot\mono\temp\bin\Release` | yes | yes | no |
| HeartShake | `sts2-heartshake\mod\.godot\mono\temp\bin\Release` | yes | yes | no |
| QuriousCraftingRelics | `AutoAnthonyRelics\mod\.godot\mono\temp\bin\Release` | yes | yes | no |
| ChaosBridge | `chaosbridge\.godot\mono\temp\bin\Release` | yes | no | no |
| RegentFXFastBoot | `sts2-regentfxfastboot\mod\.godot\mono\temp\bin\Release` | yes | no | no |

与 gate log 一致: Spire1 `PCK_STALE`; Perfect/MpConfigSync/HeartShake/QuriousCraftingRelics `PCK_DIGEST_MISSING`。ChaosBridge/RegentFXFastBoot 未在该日志中列为 finding 的原因已确认: 两者 manifest `has_pck=false` (`G:\omp works\Sts\chaosbridge\ChaosBridge.json`、`G:\omp works\Sts\sts2-regentfxfastboot\mod\RegentFXFastBoot.json`), 按 refresh L550-562 allowlist 其 PCK 非 required, 故缺 PCK/digest 不报错。这 4 个 `PCK_DIGEST_MISSING` 只在 push 路径的全量 gate (C5) 上阻塞 Spire1 上传; 单跑 `refresh -Only Spire1` 时不出现。

### C16. 路径门禁现状 (修复方案不得降低的部分)

- refresh L61-75: `-Root` 只允许 `G:\omp works\Sts` 与 `G:\omp works\.tmp` 两棵树。
- L79-108: root 及其祖先的 reparse 检查、`steam_appid.txt` Steam 标记检查。
- L945-1041: PRE-WRITE PATH GATE, 覆盖 repo/manifest/build/stage/vdf 与 VDF contentfolder。
- L773-798: verify 阶段对 stage/build/manifest/vdf 再做 dot-dot/inside-repo/reparse/Steam 检查。
- 实测 `mod\.godot` 到 `Release`、`workshop\content` 到 `Spire1` 各级均为 `reparse=False` (只读属性检查)。

## 进行中

### P0. 最小安全实现方案 A (推荐, 未实施): 在 Build-Spire1Release.ps1 里把 clean PCK + digest 原子写入 build 目录

触发条件: 需要让 Spire1 的 refresh provenance gate 通过 (当前被 PCK_STALE 阻塞), 且不得把 28MB 完整资产 PCK 重新带入发布、不得降低路径与字节门禁。

发布契约依据: 契约 L11-15 (三件 payload)、L20 (digest 非 payload)、L73 (结构门禁失败禁止 Promote)、L75-88 (重建流程)。

当前控制流: 见 C3 + C4。clean PCK 只到 .tmp payload/evidence; refresh 只信 build 目录; 两者不接。

需要修改的绝对路径与行号:
1. `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`
   - 在 L246 之后 (packer 成功、PCK 已在 `$payloadModsRoot`)、L248 之前插入 "sync-to-build-dir" 段 (伪代码见下)。
   - 目标: `Join-Path $RepoRoot "mod\.godot\mono\temp\bin\$Configuration\Spire1.pck"` 与同目录 `Spire1.pck.sha256`。
   - 顺序: 先跑 L258-265 的 `Verify-Spire1Pck.ps1` 确认字节, 再做同步; 同步必须先写临时名 (`*.new-<guid>`), 回读校验后 `Move-Item -Force` 覆盖正式名 (PCK 先, digest 后)。
   - 断言: 目标目录在 `$RepoRoot` 内且非 reparse; 最终 PCK mtime 晚于 build DLL mtime; digest 内容等于最终 PCK SHA256。
   - 建议做成显式开关 (例如 `-SyncBuildDirPck`, 默认关闭), 或仅非 `-PlanOnly` 且 `-SkipBuild` 为假时执行, 保持既有默认行为。
2. `G:\omp works\Sts\sts2-spire1\docs\RELEASE-PACKAGE-CONTRACT-20261003.md`
   - L25 之后或 L75-88 流程中加一条: "脚本必须把 clean PCK + digest 同步到 Release build 目录, 使 refresh 的 canonical 来源成立; digest 仍不是 payload。"
   - L17-23 的"不是正式 payload"列表保留 `Spire1.pck.sha256`。

插入点伪代码 (仓库风格, 含 Assert-Under/Assert-NoReparse/失败回滚):
```
$buildPckDir = Join-Path $RepoRoot "mod\.godot\mono\temp\bin\$Configuration"
Assert-Under $buildPckDir $RepoRoot 'build-pck-dir'
Assert-NoReparse $buildPckDir 'build-pck-dir'
$buildPck = Join-Path $buildPckDir 'Spire1.pck'
$buildDigest = Join-Path $buildPckDir 'Spire1.pck.sha256'
# 1) Verify-Spire1Pck 已通过 (L258-265)
# 2) Copy-Item $pck -> "$buildPck.new-<guid>"; WriteLinesToFile digest -> "$buildDigest.new-<guid>"
# 3) 回读临时 digest == 临时 PCK 的 SHA256, 否则 throw
# 4) Move-Item -Force 临时 PCK -> $buildPck; 临时 digest -> $buildDigest
# 5) 断言 $buildPck mtime > build DLL mtime, 且 $buildDigest 内容 == $buildPck SHA256
```

需要运行的命令 (修复后, 由主会话执行):
```
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1" -OutputRoot "G:\omp works\.tmp\spire1-release-<stamp>" -SkipBuild -SyncBuildDirPck -Promote
powershell -NoProfile -ExecutionPolicy Bypass -File "G:\omp works\.tooling\refresh-workshop-payloads.ps1" -VerifyOnly -Only Spire1
```
第二条必须 exit 0 且输出 `VERIFY RESULT: OK (1 row(s) verified; read-only, nothing copied)`。

最小修复范围: 只动 `Build-Spire1Release.ps1` (新增同步段 + 可选开关) 与契约文档一句; 不改 refresh 契约、不改 push-all、不改 csproj、不改游戏目录。

尚缺实机证据: 见 U2/U3/U4; 修复本身可用 SHA256/门禁输出自动验证, 但 clean PCK 的运行时加载/旧存档/运行历史仍需在测试副本启动游戏验证 (契约 L90-94)。

风险与边界:
- `-SkipBuild` 复用当前 build DLL; 若源码在编译后又被改, refresh freshness 扫描 (L853-877) 会报 REBUILD_REQUIRED。本次扫描 0 命中 (C11)。
- 覆盖 build 目录 PCK 会移除 28MB 旧件; 这是 build 输出, 不属用户游戏/Steam, 符合 AGENTS.md 测试副本规则。
- 该方案要求 refresh 全量 gate 通过才能上传, 因此还需其它 row 的 digest 修复 (C15)。

### P1. Spire1 单项上传的连带阻塞 (需并行处置)

上传入口 `workshop-push-all.ps1` L215-219 调用 refresh 时不传 `-Only`, 所以 push 路径的 provenance gate 覆盖全部 7 行。Perfect/MpConfigSync/HeartShake/QuriousCraftingRelics 的 `PCK_DIGEST_MISSING` (C15) 因此是实际上传的阻塞项; ChaosBridge/RegentFXFastBoot 因 `has_pck=false` 不报 finding (C15)。其处置范围属于 `other-mods-digest-scout-20261004.request.md`, 本报告只登记依赖关系。

直接跑 `refresh-workshop-payloads.ps1 -Only Spire1` 时只有 Spire1 被评估, 所以 Spire1 单项修复可以用该命令独立验证, 不必等其它项目修完。

### P2. 方案 B (不推荐, 仅备选): 在 refresh 里为 Spire1 增加 payload PCK 旁路

需改 `G:\omp works\.tooling\refresh-workshop-payloads.ps1` L134 (row 定义) 与 L564-707 (reconciliation), 让某类 row 从 .tmp payload 取 PCK。代价: 引入第二套 canonical 来源; `-Root` 隔离测试、pre-write path gate、copy phase 都要分支; 与 L27-28 "no source-tree fallback" 原则冲突。会降低门禁单一性, 不建议。

## 未知

- U1 [P2] PckPacker 0.1.1 的字节确定性只有两个同输入样本支持 (C2); 未做不同目录顺序/文件系统顺序的更强实验, 未反汇编确认。
- U2 [P2] clean 19MB PCK 的真实游戏加载未由本审查验证 (未启动游戏)。已有独立证据 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-current-r15-20261004.md` L7-L9: 隔离 non-Steam headless 运行使用 `PCK sha256=70CCBB4D...51C79` 且 exitCode=0; 那是三形态 smoke, 不等于完整资产/运行历史/旧存档验收。
- U3 [P2] clean PCK 与 28MB PCK 的运行时差异未验证; 被排除 266 个资产 (C14) 是否真无消费者只有静态源码审计依据 (契约 L42-56)。
- U4 [P1] 其它 4 个项目的 `PCK_DIGEST_MISSING` (C15) 未在本任务修复或验证; Spire1 单项修复后全量 gate 仍会失败。
- U5 [P3] 未检查 vdf contentfolder 下除 `Spire1` 之外的其它 Spire1 副本; 本次只读 vdf 指向的 `Spire1` 子目录。
- U6 [P2] 未验证 `Move-Item -Force` 在同卷是否原子替换 (NTFS 通常等价 rename, 未实测); 若需更强保证可用 `File.Replace` 并保留备份。
- U7 [P2] 未验证 `-SkipBuild` + 同步开关组合下 mtime 断言是否总成立 (若 DLL 比新 PCK 新, 应显式失败而非静默)。
- U8 [P1] 未验证方案 A 对 `refresh-workshop-payloads.ps1 -Root G:\omp works\.tmp\...` 隔离测试的影响: 方案 A 写真实 build 目录, 隔离测试指向 .tmp 副本时不会被同步; 需主会话决定是否同时支持 build 目录参数。

### 明确不能声称的实机证据 (汇总)

1. 未运行游戏; clean PCK 的实际加载、设置页、三角色、旧存档、运行历史均未由本审查验证。
2. 未部署到 `E:\Slay the Spire 2\` 或 test-client-B; 两处仍是旧 DLL/PCK (C10)。
3. 未上传 Workshop; 未启动 SteamCMD; 未接触共享 `mod_configs`。
4. 未做 Godot export-pack / MegaDot 验证 (本机无 MegaDot, 契约 L92)。
5. 未验证 PckPacker 跨不同输入/环境的确定性 (仅两个同输入样本)。
