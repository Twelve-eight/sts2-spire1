# Spire1 当前发布包只读审查 (2026-10-04)

审查员: 只读发布包审查. 未修改产品代码, 未构建, 未部署, 未启动游戏, 未写 Steam, 未写共享 mod_configs, 未写 C:.
指定模型: `global:deepseek-v4.1-flash`, 路由 `gateway/wb2api`, 思考层级 `max`.
请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\release-package-current-deepseek-review-request-20261004.md`.

## 已确认

### C1. payload 恰有三件文件, hash 与 evidence 对应 (目标 1)

- 目录: `G:\omp works\.tmp\spire1-release-r12-20261004-central\payload\mods\Spire1`.
- 实测重算 SHA256:
  - `Spire1.dll` 900608 bytes `E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB` (LastWriteTime 2026-10-04 11:18:36)
  - `Spire1.json` 548 bytes `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305` (LastWriteTime 2026-09-30 02:01:48)
  - `Spire1.pck` 19669354 bytes `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79` (LastWriteTime 2026-10-04 11:19:03)
- `evidence\release-manifest.json` 中 Payload 三项的 Path/Bytes/Sha256 与上述重算逐项一致.
- 复现命令: `Get-ChildItem -LiteralPath 'G:\omp works\.tmp\spire1-release-r12-20261004-central\payload' -Recurse -File | ForEach-Object { Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256 }`

### C2. asset-manifest 计数与 DEVLOG plan 一致 (目标 2, 初步)

- `evidence\asset-manifest.json` Counts: SourceFiles=1010, Kept=744, Excluded=266, KeptBytes=33803542, ExcludedBytes=17127986.
- 与 `DEVLOG.md` 2026-10-03 发布包重构增量记录的 1010/744/266, 33,803,542 / 17,127,986 一致.
- Excluded 按 Reason 分组: card portraits 206, relics 42, potions 12, charui template 4, omega power 2.
- 复现命令: `$j = Get-Content ...asset-manifest.json -Raw | ConvertFrom-Json; $j.Counts`

### C3. pck-structure.json 声称通过 (目标 3, 待独立复算)

- `evidence\pck-structure.json`: PckSha256 与 payload PCK 一致; Format 3 / Engine 4.5.1 / Flags 2 / FileBase 112 / HeaderBytes 112; SourceFiles=744, ExpectedEntries=1464, ActualEntries=1464, CtexEntries=720, ImportEntries=720, JsonEntries=24, Missing=[] / Unexpected=[] / Md5Verified=true.
- 尚未独立复算, 列入进行中.

### C4. release-gates.json 三项全 PASS (目标 4, 待绑定 DLL hash)

- `evidence\release-gates.json`: passed=true; assemblyref-forbidden PASS (检查 5 个 AssemblyRef); manifest-consistency PASS (二进制引用 BaseLib, manifest 声明 BaseLib); typedef-forbidden PASS (精确 6 项 + 1 个禁含命名空间).
- gates JSON 记录的 dll 路径: `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`; 该路径与 payload DLL 的 hash 绑定待验证, 列入进行中.

### C5. 隔离 smoke staging 绑定当前 r12 字节 (目标 6)

- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r12-current-20261004-rerun\staging-current.json` 中 Spire1 三项:
  - DLL 900608 `E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB`
  - JSON 548 `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305`
  - PCK 19669354 `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79`
- `run-final.json` 记录 stagedSpire1DllSha256 / stagedSpire1PckSha256 与上述一致, exitCode=0, sharedConfigSha256Unchanged=true, steamSettingsRestored=true, cleanupCompleted=true.
- 边界: 这是 headless 隔离 smoke 的运行记录; 不构成本审查对玩家可见实机全路径的验证.

### C6. 历史包字节与当前 r12 不同 (目标 6, 初步)

- 历史 r4 包 DLL `883CD438435A651DF8BF0A1DE5D9C28467E5A7CF8B14B561015C436E70FFF519` (DEVLOG 2026-10-03), 历史 r2 包 DLL `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`, 历史 r3 包 DLL `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`.
- 当前 r12 payload DLL `E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB` 与上述历史字节均不同; 历史报告不直接采信.

## 进行中

- 目标 1 收尾: payload 目录 `-Force` 检查是否含隐藏文件/目录.
- 目标 2: 逐路径核对 kept 白名单与 excluded 源码驱动理由 (charui/omega/portrait 可达性).
- 目标 3: 独立在内存中复算 PCK 头, entries 集合, excluded 资产无 ctex/import/json.
- 目标 4: 复算 gates DLL 路径当前 hash, 确认与 payload DLL 绑定; 检查 SourceCommit 与当前 HEAD.
- 目标 5: 只读盘点 `workshop\content\Spire1` 是否仍为历史字节或含 stale PDB/deps/旧 zip.

## 未知

- 未取得本会话实际解析的模型/路由 session metadata (待只读查证或标注未知).
- 未运行 PCK verifier (其输出会写文件, 超出唯一可写路径); 改用内存内只读复算.
- 未做玩家可见实机全路径验证; headless smoke 证据只按 headless 边界采信.

---

## 第二批已确认 (目标 3, 4, 5, 6)

### C7. PCK verifier 输出经独立内存复算一致 (目标 3) - 已独立复算

- 复算对象: `payload\mods\Spire1\Spire1.pck` + `evidence\asset-manifest.json`.
- 复算结果 (只读, 未运行 verifier, 未写任何输出文件):
  - magic=GDPC; Format=3; Engine=4.5.1; Flags=2; FileBase=112; HeaderBytes=112; DirectoryOffset=19516958; Length=19669354.
  - ExpectedEntries=1464; ActualEntries=1464; Missing=0; Unexpected=0.
  - CtexEntries=720; ImportEntries=720; JsonEntries=24.
  - 32 字节对齐: 0 个不对齐.
  - 每个目录项 MD5 与实际数据: 0 mismatch.
  - 每个 `.ctex` 头: 0 个非 GST2.
- 与 `evidence\pck-structure.json` 记录逐项一致 (Schema Spire1ReleasePckManifest.v1, PckSha256 `70CCBB4D...`, Md5Verified=true).
- 排除资产无对应 ctex/import/json: 由 "Unexpected=0 + expected 集合严格由 Kept 744 行推导" 直接证明. 排除的 266 项不在 Kept, 未产生任何 PCK 条目.
- 复现命令: 见本报告末尾"复算脚本"段 (内联 PowerShell, 不落盘).

### C8. gates DLL hash 与 payload DLL hash 完全绑定 (目标 4)

- `evidence\release-gates.json` 的 `dll` 字段: `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`.
- 实测该路径: Size=900608, LastWriteTime=2026-10-04T11:18:36.5966235+08:00, SHA256=`E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB`.
- payload DLL 实测: Size=900608, LastWriteTime=2026-10-04T11:18:36, SHA256 同上. **两者同字节**.
- 结论: release-gates.json 的三项 PASS 与当前 payload DLL hash 绑定成立.
- 边界: 门禁工具的 json 不记录 DLL hash 字段, 绑定由"路径当前字节 = payload 字节"间接成立; 若在 11:18:36 之后重新编译该路径, 绑定会失效.

### C9. SourceCommit 与当前 HEAD 一致 (目标 4, 补充)

- `release-manifest.json` 的 `SourceCommit` = `a6e46e53ae891e4faa7b640a64c1000a9e566c9a`.
- 当前仓库 HEAD 实测相同.
- 边界: 工作树 dirty (312 项 porcelain), 所以 SourceCommit 只标识基线, 不完整代表构建字节的源码身份; 与 DEVLOG 2026-10-03 记录的 "HEAD 不能单独代表完整源码身份" 一致.
- 但 11:18:36 之后无 `mod\Spire1Code` 或 `mod\Spire1` 资产文件更新 (实测 0 个晚于构建时间的文件), csproj 时间 2026-10-02 13:08:49, 所以构建后未被源码改写.

### C10. asset allowlist 源码驱动理由逐项复核 (目标 2)

- 保留集合 (Kept) 分类: PNG 720 + JSON 24 = 744, 与 Counts.Kept 一致.
  - `images/card_portraits` 432 (216 小图 + 216 大图), `images/run_history` 110, `images/powers` 100, `images/relics` 69, `localization/eng` 12, `localization/zhs` 12, `images/potions` 6, `images/charui` 2, `mod_image.png` 1.
- 排除集合 (Excluded) 分类: card_portraits 206, relics 42, potions 12, charui 4, powers/omega_power 2 = 266.
- 源码驱动理由核对:
  - 卡/遗物/药水路径由 `Spire1Card.cs:29-30` (`CardImagePath`/`BigCardImagePath`), `Spire1Relic.cs:30-32` (`RelicImagePath`/`BigRelicImagePath`), `Spire1Potion.cs:26-28` (`PotionImagePath`/`PotionOutlineImagePath`) 按 `Id.Entry.RemovePrefix().ToLowerInvariant()` 拼接; 构建脚本用正则从当前具体类 (Cards 220 stems, Relics 24 stems, Potions 2 stems) 生成 allowlist.
  - 交叉验证: Excluded 中 card portraits/relics/potions 与 ModelStems 的 stem 冲突数为 0/0/0, 即没有排除当前具体类的图.
  - charui 4 项 (char_select_char_name / _locked / character_icon_char_name / map_marker_char_name): `Ironclad.cs:12-26`, `Silent.cs:12-26`, `Defect.cs:12-26` 全部走 `PlaceholderCharacterModel` + `PlaceholderID`, `Spire1CardPool.cs:18-21` 注释明确 "reuse StS2 Ironclad energy icon instead of the mod's gray placeholder charui/*.png"; 全仓 394 个 .cs 文件中 0 个引用这 4 个文件名. 排除有源码理由.
  - powers/omega_power 2 项: 全仓 394 个 .cs 文件中 `omega` 0 命中; 契约中 "已确认无产品类/引用" 成立.
  - charui 保留 2 项 (big_energy/text_energy): `Spire1RelicPool.cs:15-16` 显式引用, 与契约 "显式引用" 一致.
- 边界: "源码驱动" 的证明面是"当前具体类 + 显式路径引用"; 动态拼接路径 (如 encounter id) 由 run_history 全量保留覆盖; 反射式加载路径不在本次静态证明范围内.

### C11. workshop payload 仍为历史字节且含 stale PDB (目标 5) - 只读记录

- 目录: `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\` (mtime 2026-09-22 11:00:51).
- 现存 4 个文件:
  - `Spire1.dll` 502784 B, mtime 2026-09-30 01:44:25, SHA256 `FE7935928273A313DD565E87E80C793CDA3E499ACBBC034603FDFFB3432F93B9` -- 与 DEVLOG 2026-09-30 记录的 1.2.3 实机冒烟字节一致, 即历史 1.2.3 字节, 与当前 r12 payload 不同.
  - `Spire1.json` 548 B, SHA256 `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305` -- 与当前 payload manifest 同字节 (manifest 自 2026-09-30 未变).
  - `Spire1.pck` 28865174 B, mtime 2026-09-30 01:45:45, SHA256 `C8F718AB73F3C054FC3434F1CE0277E6C7E7E76DBF96A23104CF940231EC63BD` -- 历史未精简 PCK (含被排除资产), 与当前 r12 精简 PCK 19669354 B 不同.
  - `Spire1.pdb` 179332 B, mtime 2026-09-30 01:44:24, SHA256 `62703EF8EB45D03F3F018338D7192186513F50974B73133C812A74F8D369251A` -- stale PDB, 违反正式 payload "只允许三件" 契约.
- `workshop\` 树内其它文件 (DESCRIPTION-BBCODE.txt / description-*.txt / preview.png / workshop_upload.vdf / workshop-push.ps1) 是上传脚手架, 不在 `content\Spire1` payload 内.
- 未发现 `content\Spire1` 内旧 zip / deps.json / pck.sha256 / 日志.
- 本轮未清理, 未 Promote, 未写 workshop. 状态如实记录: 当前 workshop payload 是历史 1.2.3 字节 + stale PDB, 不是 r12 精简包.

### C12. 当前 r12 证据与旧包字节区分 (目标 6)

- 当前 r12 隔离 smoke (`native-form-smoke-r12-current-20261004-rerun`) 的 staging/run 记录绑定 DLL `E31D10C1...` + PCK `70CCBB4D...`, 与当前 r12 payload 逐字节一致.
- 历史包字节 (不采信为本轮证据):
  - Beta r2 包 DLL `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7` / PCK `CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E`.
  - Beta r3 包 DLL `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`.
  - Beta r4 包 DLL `883CD438435A651DF8BF0A1DE5D9C28467E5A7CF8B14B561015C436E70FFF519` / PCK `3AA0BD39173C491301BCF42F3FB5ADA25A4762794F0C86B119239824A8CFC545`.
  - workshop 当前 DLL `FE7935928273A313DD565E87E80C793CDA3E499ACBBC034603FDFFB3432F93B9`.
- 以上 4 个历史 DLL hash 均不等于当前 r12 `E31D10C1...`; 历史报告 (beta-package-audit / beta-package-r2 / release-beta-r4) 仅作上下文, 未采信其 PASS 为当前证据.
- 当前 r12 与 r4 的 PCK 不同 (19669354 vs 19666890 bytes, 不同 hash), 但两者结构门禁计数相同 (1464/744/720/720/24); 说明 r12 是"结构相同, 资产字节更新"的重建.

## 进行中

- 目标 4 收尾: 独立复跑 release gates 工具 (只读方式, 避免写 JSON) 直接验证三项门禁对当前 DLL 的结论.
- 目标 1/5 收尾: 核对 `release-manifest.json` 的 HistoricalInputsExcluded 与 dist 现状一致.

## 未知

- 未取得本会话实际解析的模型/路由 session metadata; 唯一可见的 `gateway/wb2api` 说法来自请求文件与历史 DEVLOG, 本轮未独立核验.
- 未做玩家可见实机全路径验证 (UI/视觉/长战斗/多人/存档).
- workshop payload 的历史字节是否已在 Steam 端发布为同一版本, 未核验 (只读本地目录, 未查 Steam 端).
- `release-gates.json` 未内嵌 DLL hash 字段, 若未来同名路径被重编译, 绑定需重跑门禁 (已列入风险).

---

## 第三批已确认 (目标 4 独立复跑, 目标 1/2 收尾)

### C13. 独立复跑 release gates: 对当前 payload DLL 三项全 PASS, 退出码 0 (目标 4)

- 复跑命令 (只读, 未写 JSON):
  `& dotnet 'G:\omp works\Sts\sts2-spire1\tools\build-gates\bin\Release\net9.0\Spire1ReleaseGate.dll' --dll 'G:\omp works\.tmp\spire1-release-r12-20261004-central\payload\mods\Spire1\Spire1.dll' --config 'G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json' --manifest 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json' --gates 'assemblyref,manifest,typedef'`
- 输出: AssemblyRef 15 个, TypeDef 1014 个; assemblyref-forbidden PASS (检查 5 个); manifest-consistency PASS (二进制引用 BaseLib = manifest 声明 BaseLib); typedef-forbidden PASS; 结果 PASS; EXIT=0.
- 与 `evidence\release-gates.json` 的三项名称/通过状态一致. 注意: gates JSON 未记录 TypeDef 数量; 本次复跑读到 1014 个, JSON 的 Summary 只写"精确 6 项 + 1 个禁含命名空间", 无矛盾.
- 结论: 目标 4 对当前 payload DLL 独立成立 (不再依赖 evidence JSON 自述).

### C14. payload 目录精确三件, 无隐藏文件/目录 (目标 1 收尾)

- `Get-ChildItem -Force` 结果: `payload\mods\Spire1\` 下仅 `Spire1.dll` / `Spire1.json` / `Spire1.pck` 三个文件, 无 PDB / deps.json / pck.sha256 / 日志 / zip / 隐藏项.
- 复现命令: `Get-ChildItem -LiteralPath '...\payload' -Recurse -Force | Select-Object FullName, PSIsContainer, Length`
- 注: `evidence\Spire1.pck.sha256` 位于 evidence 目录 (构建证据), 不在 payload; 符合契约.

### C15. asset-manifest 与源目录/Stage 三方一致 (目标 2 收尾)

- 源目录 `mod\Spire1` 共 1010 个文件; Kept 744 + Excluded 266 = 1010, UNCLASSIFIED=0, KEPT_MISSING_IN_SOURCE=0, EXCL_MISSING_IN_SOURCE=0.
- Kept/Excluded 无重复 (KEPT_DUP=0 / EXCL_DUP=0 / OVERLAP=0).
- Stage 目录逐文件 SHA256 与 asset-manifest.Kept 逐行一致: STAGE_MISSING=0, STAGE_HASH_MISMATCH=0, STAGE_FILE_COUNT=744, STAGE_EXTRA=0.
- 分类计数与契约一致: localization 24/24 全保留; run_history 110/110 全保留; powers 源 102 = Kept 100 + Excluded 2 (仅 omega_power 大小图); mod_image.png 保留.
- 复现命令见"复算脚本"段.

### C16. release-manifest 的 HistoricalInputsExcluded 与 dist 现状 (目标 1/5 收尾)

- `release-manifest.json` 声明排除: `dist/deprecated`, `dist/friends-pack`, `dist/Spire1-Forms-Beta-20261003.zip`.
- 实测: `dist/deprecated` 存在 (历史目录), `dist/friends-pack` 存在 (历史目录), `dist/Spire1-Forms-Beta-20261003.zip` 不存在 (实测为 False).
- 说明: 该字段只是"历史输入排除声明"; 三个路径均未被构建脚本读取 (Build-Spire1Release.ps1 只读 `mod\Spire1` 与 `mod\Spire1Code`). 其中 zip 已被后续 r3/r4 zip 取代, 路径不存在属正常.
- `dist` 当前文件: `friends-pack.zip` (137513408 B), `Spire1-Forms-Beta-20261003-r3.zip` (19372105 B), `Spire1-Forms-Beta-20261003-r4.zip` (19382956 B), 及 r4 sidecar. 这些是历史交付物, 本轮未采信为当前证据.

### C17. 当前 r12 evidence 内不含任何旧包 hash (目标 6 收尾)

- 在 `evidence\` 5 个文件中检索历史 hash: `51224C20` / `4F49BA0D` / `883CD438` / `FE793592` / `62703EF8` / `C8F718AB` / `CF37054F` / `3AA0BD39` 全部 0 命中.
- 当前 hash 命中: `E31D10C1` 1 处 (release-manifest.json), `70CCBB4D` 3 处 (release-manifest.json / pck-structure.json / Spire1.pck.sha256).
- 结论: evidence 目录自洽地只描述当前 r12 字节; 无历史字节混入.

### C18. 隔离 smoke 的当前 r12 绑定与边界 (目标 6)

- `native-form-smoke-r12-current-20261004-rerun` 的 `staging-current.json` / `run-final.json` 绑定 DLL `E31D10C1...` + PCK `70CCBB4D...`, 与当前 payload 逐字节一致.
- 隔离游戏目录 `native-isolated-20260930\game\mods\Spire1\` 当前 3 文件为 900608 / 548 / 19669354, DLL SHA256 `E31D10C1...` 与 payload 一致.
- 三场景 JSON (calm/wrath/divinity) 均 status=passed, formGateAfter.passed=True, effectVerification.passed=True, unobservedFaults=[].
- `run-final.json`: exitCode=0, timedOut=False, logDrainCompleted=True, scenarioFresh=True, sharedConfigSha256Unchanged=True, steamSettingsRestored=True, cleanupCompleted=True.
- 共享配置快照: `shared-config-before.json` 与 `shared-config-after.json` SHA256 相同 (`046119F4...`); 真实共享目录 `G:\appdata\...\mod_configs` 现存 19 个 cfg, 本轮未写.
- 边界: 这是 headless 隔离冒烟; 不构成玩家可见 UI/视觉/长战斗/多人/存档实机全路径验证. 本审查未启动游戏, 未重新运行 smoke, 只读取其落盘证据.

### C19. 遗留风险 (P2/P3, 有证据, 不阻塞本轮结构结论)

- P2: `release-gates.json` 不内嵌 DLL SHA256, 只记录路径. 当前绑定靠"路径当前字节 = payload 字节"成立 (实测 E31D10C1 双向一致); 若同名路径将来被重编译, 该 JSON 的 PASS 会自动指向新字节而失效. 最小修复范围: 在门禁工具 JSON 中增加 `dllSha256` 字段 (不改本轮产物).
- P3: `release-manifest.json` 的 `SourceCommit` = a6e46e5 只是 HEAD 基线; 工作树 dirty (312 项 porcelain), 构建字节的完整源码身份不由此字段单独承担. 现有缓解: 构建后无 `mod\Spire1Code`/`mod\Spire1` 文件更新 (实测 0 个晚于 11:18:36), csproj mtime 2026-10-02.
- P3: `workshop\content\Spire1\` 仍含 stale `Spire1.pdb` (179332 B) 与历史 1.2.3 DLL/PCK; 本轮按请求只读记录, 未清理, 未 Promote. 若误将该目录当作 r12 发布件, 会发布历史字节.

## 进行中

- 无 (所有 6 个审查目标均已给出当前证据结论).

## 未知

- 本会话实际解析的模型/路由 session metadata 未独立核验 (请求文件与历史 DEVLOG 记为 `global:deepseek-v4.1-flash` / `gateway/wb2api`, 本报告不将其写成实测事实).
- 玩家可见实机全路径 (UI/视觉/长战斗/多人/存档读写/性能) 未验证, 不属本轮只读发布包审查范围.
- Steam 端已发布内容与本地 workshop 目录是否一致未核验 (未查 Steam, 未写 Steam).
- 动态/反射式资产加载路径未被静态证明完全覆盖; run_history 采用全量保留作为覆盖手段.

## 复算脚本 (内联, 不落盘)

```powershell
# 1) payload 三件 hash
Get-ChildItem -LiteralPath 'G:\omp works\.tmp\spire1-release-r12-20261004-central\payload' -Recurse -File |
  ForEach-Object { $h = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256; [pscustomobject]@{ Path=$_.FullName; Size=$_.Length; SHA256=$h.Hash } }

# 2) PCK 结构复算 (只读, 内存中完成): 见本轮执行的 EXPECTED_COUNT=1464 / MISSING=0 / UNEXPECTED=0 / MD5_MISMATCH=0 / CTEX_BAD=0 脚本
# 3) release gates 复跑
& dotnet 'G:\omp works\Sts\sts2-spire1\tools\build-gates\bin\Release\net9.0\Spire1ReleaseGate.dll' `
  --dll 'G:\omp works\.tmp\spire1-release-r12-20261004-central\payload\mods\Spire1\Spire1.dll' `
  --config 'G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json' `
  --manifest 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json' --gates 'assemblyref,manifest,typedef'
```

## 结论

- 目标 1 (payload 三件 + hash 对应 evidence): PASS.
- 目标 2 (asset-manifest kept/excluded 数量与路径遵守契约, 排除有源码理由): PASS.
- 目标 3 (PCK verifier 输出通过, entries/sourceFiles/digest 一致, 排除资产无 ctex/import/json): PASS (独立内存复算).
- 目标 4 (AssemblyRef / manifest / TypeDef 三项 PASS 且与当前 DLL hash 绑定): PASS (独立复跑 + hash 实测).
- 目标 5 (workshop payload 只读记录): 已记录 -- 仍为历史 1.2.3 字节 + stale PDB; 未清理, 未 Promote.
- 目标 6 (当前 r12 证据 vs 旧包字节): PASS -- 当前证据绑定 E31D10C1/70CCBB4D; 4 个历史 DLL hash 均不匹配, 未采信历史报告.

SUPERVISION_PASS

说明: 该 PASS 仅覆盖只读静态发布包审查 (payload 结构/hash/PCK/门禁/workshop 现状). 不构成玩家可见实机全路径通过; headless smoke 证据按 headless 边界采信.
