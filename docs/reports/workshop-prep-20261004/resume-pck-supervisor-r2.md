# resume-pck-supervisor-r2 报告 (四份 csproj 监督, 最终)

- 监督范围: `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj`, `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj`, `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj`, `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj`
- 基线: 同目录 `<Id>.csproj.before` 与 `G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md`
- 门禁: `resume-finalizer-r2-wait-gate.json` (精确 id `01a10873-550a-7690-8e8a-e17bdd04f80e`, `multi_agent_v1.wait_agent`=completed, ZeroCodeChanges=true) + `resume-supervision-r2-activation.txt`
- 本报告为静态审查. 本人未执行 parse/build/lint/test/pack/预处理/部署/游戏操作. 下文引用的 24/24 夹具与 11/11 隔离结果均为中央既有证据 (交叉引用, 非本人运行).
- 结论: **SUPERVISION_PASS** (无未解决问题; 边界见"未知")

## 已确认

### 1. [P0] 最终 hash 冻结一致; 四份均为纯插入, 旧代码/依赖/部署 0 改动
- 路径与行号 (新增块): Perfect L119-203, MpConfigSync L102-186, HeartShake L118-202, Qurious L81-165.
- 冻结 hash (本机复算 == wait-gate json): Perfect `769E2D0FE6B183B1B48625056D32DE104F283431B43A23D5B7BFF4A1912C98D5` (12887B); MpConfigSync `C3B273F5E6398844FCD61BD44F3EF10DF92BE8FD7D2FAEB5D71B3DBD7123EEB0` (11367B); HeartShake `2D5266D5A0121E36EF81C04462C9BD95976443BA8F8BFE8A9FC701B6BE7E80E9` (12051B); Qurious `962D9EAF7EA3C208084E8C5B587D165B3F36ECC5F474C727C7B25663BE488900` (10256B).
- 字节结构: 对每份 new-vs-.before 逐字节比较: prefixEqual=True (6240/4721/5404/3609 字节), 旧尾部 `</Project>` 逐字节保留 (11/10/11/11 字节), 长度等式成立; 首个分歧字节即插入点. 四份插入块字节完全一致: 6636B, SHA256 `BC3C983D22FBBB2841B165A345FB33EBF370555B0C1723C7224F8CD5579CE90C`.
- 编码: 中央 `central-csproj-xml-encoding.json` (XmlParsed=true, 四份 hash 同冻结值) 记录 BOM/LF 与改前一致 (Perfect/MpConfigSync/HeartShake 有 BOM, Qurious 无; MpConfigSync 改前改后均不以 LF 结尾). 本人独立复核一致.
- 复现: `Get-FileHash -LiteralPath <path> -Algorithm SHA256`; 逐字节比较脚本见 `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\resume-pck-supervisor-r2.request.md` 所述基线.
- 最小修复范围: 无 (PASS). 尚缺实机: 无 (结构面已闭合).

### 2. [P0] PckPacker 0.1.1 契约与 producer 挂点/退出码/顺序一致
- 权威包: `G:\omp works\Sts\sts2-spire1\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\build\BSchneppe.StS2.PckPacker.targets` (SHA256 `03B20AD3DE975F1A38301450A9AFDAB232544A5E585E8EA320B8C66B5D7C1921`; 四项目解析同版本: 各 `*.csproj.nuget.g.targets` L5; AutoAnthonyRelics 本地 `.nuget` 副本同 SHA).
- 权威语义: targets L12-14 (`PackPck` AfterTargets=Build, 条件 Enabled==true 且 SourceDir 存在); L16-21 (Exec 捕获 `_PckPackerExitCode`); L23-26 (exit 2 -> `PckPackerSkipped=true`); L36-37 (其它非 0 -> 包内 Error, 构建失败).
- producer 控制流 (四份一致): `BeginQuickPckDigest` BeforeTargets=PackPck (Perfect L155-162 / Mp L138-145 / HS L154-161 / Q L117-124), 条件含 Enabled==true 且 inner!=true 且 Exists(SourceDir); `WriteQuickPckDigest` AfterTargets=PackPck (Perfect L164-203 / Mp L147-186 / HS L163-202 / Q L126-165), 条件含 Enabled==true 且 inner!=true 且 Skipped!=true 且 `_PckPackerExitCode`==0.
- 顺序: Begin 在 pack 前记录起点并删旧 digest; Write 在 pack 后 (exit 0 且非 skip) 才写 digest; 失败 (exit 非 0/2) 时包内 Error 中止, Write 不运行; skip (exit 2) 时 Write 条件不满足. 与契约第 5 条逐项一致.
- 复现: 阅读上述 targets L12-37 与四份 csproj 对应行; 夹具证据 `G:\omp works\.tmp\workshop-prep-20261004-central\producer-fixtures-20261005-040018\results.json` 24/24 Passed, 每项 `SourceSha256` == 上述冻结 hash.
- 最小修复范围: 无. 尚缺实机: 真实四项目 Rebuild (见未知 1).

### 3. [P0] 旧 digest 作废: 失败/跳过/缺失 DLL 后不得残留旧记录 (夹具实证)
- 行号: Begin 删除动作 Perfect L160 / Mp L143 / HS L159 / Q L122 (`Delete Files="$(PckPackerOutputPath).sha256" TreatErrorsAsWarnings="false"`).
- 夹具断言 (`run-producer-fixtures.ps1` L34-38): skipped -> exit 0 且 digest 不存在且旧 PCK 字节未变; pack-failure -> exit != 0 且 digest 不存在且旧 PCK 未变; missing-dll -> exit != 0 且 digest 不存在. results.json 三者 4 项目全部 Passed=true (24/24 日志 `producer-fixtures-run.log`).
- pack-failure 日志 (`HeartShake-pack-failure\msbuild.log`) 显示包内 Error (exit -532462766) 后构建中止; 该 run 中旧 digest 已被 Begin 删除且未被重标.
- disabled/inner-export 夹具断言 (L36-37): 旧 digest 的 hash 与 mtime 均保持不变 -> producer 不活动, 不误删也不重标.
- 复现: `G:\omp works\.tmp\workshop-prep-20261004-central\producer-fixtures-20261005-040018\<Id>-<case>\msbuild.log` + `results.json`.
- 最小修复范围: 无. 尚缺实机: 真实 Rebuild 下的 skip/failure 触发 (未出现, 见未知 1).

### 4. [P0] 真 pack 起点 + PCK/DLL freshness 断言与写入顺序正确
- 起点: Begin 记录 `_QuickPckPackStartUtcTicks` (Perfect L158 / Mp L141 / HS L157 / Q L120), 严格早于 pack CLI.
- 断言 (Write): PCK 存在 (Perfect L171-172 等), 非空 (L183-184 等), PCK mtime >= pack start (L187-188 等), PCK mtime >= 构建 DLL mtime (L191-192 等), DLL mtime 可读 (L189-190 等).
- 夹具成功用例日志含 `QUICK-PCK-DIGEST-ALLOW`, 且 runner L33 断言 digest 内容 == 对 PCK 现算的 SHA256 -> 真实 pack 产物绑定通过.
- 附加证据: pack-failure 堆栈显示工具以 `System.IO.File.Create` 打开输出路径 (每次 pack 都重建 PCK 文件), 支持"成功 pack 必然刷新 PCK mtime"这一前提; 该证据来自夹具异常路径, 仍以真实 Rebuild 复核为准.
- 复现: 四份 csproj 上述行 + `producer-fixtures-20261005-040018\<Id>-success\msbuild.log`.
- 最小修复范围: 无. 尚缺实机: 真实 Rebuild 中 mtime 断言不误报 (见未知 1).

### 5. [P0] 实际 hash 与 ASCII 64hex 回读核对, 与消费端格式兼容
- 生产端: `ReadPckMetadata` 对真实字节算 SHA256, 大写无连字符 (SHA 计算行: Perfect L147 / Mp L130 / HS L146 / Q L109); `WriteLinesToFile ... Encoding="ASCII"` (Perfect L193 / Mp L176 / HS L192 / Q L155); 回读 `ReadAllText().Trim()` 与实算值比较, 空/不一致 -> Error (Perfect L195-201 / Mp L178-184 / HS L194-200 / Q L157-163). 夹具成功产物为 66 字节 (64 hex + 换行), 内容等于 PCK SHA256.
- 消费端 (refresh, 只读核对): 路径 `<buildDir>\<base>.pck.sha256` (L1183, L569); 读取后 `.Trim()` (L1213), 正则 `^[0-9A-Fa-f]{64}$` (L1222), 与实算 hash `ToUpperInvariant()` 比较 (L1230), digest mtime < PCK mtime -> DIGEST_STALE (L1238-1240); has_pck=true 时缺失即 PCK_DIGEST_MISSING (L1207-1209).
- 四份 manifest 均 has_pck=true (Perfect/MpConfigSync/HeartShake/QuriousCraftingRelics.json), 因此该 digest 在真实 refresh 中为必需项.
- 复现: 上述行号 + `refresh-workshop-payloads.ps1` L167-187 (`Hash-Of` .NET SHA256 大写).
- 最小修复范围: 无. 尚缺实机: 真实 Rebuild 后 refresh 实读该 digest (中央执行).

### 6. [P0] canonical 输出路径正确 (build 目录内, 与 refresh 期望一致)
- 默认输出: 包 targets L8 `PckPackerOutputPath = $(OutputPath)$(MSBuildProjectName).pck`; 四份 csproj 未覆盖 `PckPackerOutputPath`/`PckPackerSourceDir`/`PckPackerEnabled` (全库检索仅命中新增块自身与注释).
- OutputPath 权威: Godot SDK 4.5.1 `Sdk.props` L19-21: `BaseOutputPath=$(GodotProjectDir).godot\mono\temp\bin\`, `OutputPath=...\$(Configuration)\`; Release -> `mod\.godot\mono\temp\bin\Release\`. 现存产物佐证: 四仓 build 目录内已有 `<Id>.dll`/`<Id>.pck` 同目录 (2026-10-02 mtime).
- 与 refresh 行定义一致: L135-140 四行 `Build = "mod\.godot\mono\temp\bin\Release\<Id>.dll"`; digest 叶名 `{base}.pck.sha256` (L569).
- 复现: 上述行号 + `Get-ChildItem G:\omp works\Sts\<repo>\mod\.godot\mono\temp\bin\Release`.
- 最小修复范围: 无. 尚缺实机: 无 (路径面静态闭合; 实机写入待中央).

### 7. [P1] 部署 false 时仍产 digest; disabled/inner-export 不活动
- 独立性: producer 条件串 (Begin/Write) 不含 `CopyToModsFolderOnBuild` (Perfect L156/L165 等); 块注释明示独立 (Perfect L125-127 等). 即 `-p:CopyToModsFolderOnBuild=false` 时 PCK 与 digest 仍产在 build 目录, 不依赖部署开关.
- disabled: 条件 Enabled==true 不满足 -> Begin/Write 都不运行 (夹具 disabled: exit 0, digest 原样保留).
- inner-export: 条件 inner!=true 不满足 -> producer 不运行 (夹具 inner-export: digest hash+mtime 不变, 无 ALLOW 行).
- 复现: 四份 csproj 条件行 + 夹具 `results.json` 的 disabled/inner-export 项 (4 项目各 2 项全部 Passed).
- 最小修复范围: 无. 尚缺实机: 真实 Rebuild 用 `-p:CopyToModsFolderOnBuild=false` 产 digest (中央计划项).

### 8. [P1] 声明与代码一致; 四份块字节一致且夹具保真
- 块注释 (Perfect L119-128 等) 宣称: 真 pack 拥有 digest / Begin 先删旧记录 / 仅 exit 0 非 skip 才 stamp / 拒绝缺失/空/过期 (相对 pack 起点与 DLL) / 与部署开关独立 / disabled 与 inner 不活动. 逐条与条件串一致 (见 2-7).
- 四份插入块 SHA256 一致 (见 1); 夹具工程复制生产块逐字符一致 (忽略空白后 1569 字符相等, `equalIgnoringWhitespace=True`), 且夹具 `SourceSha256` 与冻结 hash 逐字符一致 -> 夹具结果可绑定到最终产物.
- 复现: `producer-fixtures-20261005-040018\Perfect-success\Perfect.proj` 与生产文件比对; `run-producer-fixtures.ps1` L11-13 (从生产 csproj 提取 UsingTask+两 Target).
- 最小修复范围: 无. 尚缺实机: 无 (该面为静态+夹具面).

## 进行中

- 无未决检查项. 本写集 (四份 csproj) 的静态监督面已全部闭合; 真实构建/刷新验收由中央执行, 本报告不将其称为已通过.

## 未知

1. 真实四项目 Release Rebuild + 全量 refresh/VerifyOnly/GuardsOnly 尚未运行 (中央职责): 夹具用隔离工程 + 合成 DLL 覆盖 producer 逻辑; 真实 csproj (Godot SDK/分析器/真实 PCK 工具) 下 mtime、digest 与 refresh 实读待中央验证. 证据文件 `rebuild-four-mods.ps1` 已存在, 结果 JSON 尚未落盘.
2. inner-export 残余边界 (范围外): 这四份 csproj 未像 Spire1 (Spire1.csproj L355-357) 那样在 `IsInnerGodotExport=true` 时强制 `PckPackerEnabled=false`; 若有人显式以 `-p:IsInnerGodotExport=true` 构建这四项目, 包内 PackPck 仍会重写 PCK, 而 producer 不活动 -> 可能留下与 PCK 不匹配的旧 digest (refresh 会 fail-closed 拒绝, 不会静默发布). 仓库内检索显示没有脚本对这四个项目传该属性 (仅 Spire1 自身 Exec 设置). 建议记入 DEVLOG 作为已知边界; 如需最小补丁: 在各 csproj 加 `<PropertyGroup Condition="'$(IsInnerGodotExport)' == 'true'"><PckPackerEnabled>false</PckPackerEnabled></PropertyGroup>` (本次未改, 不阻塞).
3. 打包工具"内容未变时是否跳过重写"仅由 pack-failure 异常堆栈 (File.Create) 间接支持; 真实 Rebuild 若出现 `stale-vs-pack` 拒绝, 即该前提不成立, 需回看 (fail-closed, 不会静默错标).
4. 本报告未覆盖: 两脚本写集 (由 resume-script-supervisor-r2 负责), Spire1 Promote 演练, Steam/游戏/共享配置未触碰.
