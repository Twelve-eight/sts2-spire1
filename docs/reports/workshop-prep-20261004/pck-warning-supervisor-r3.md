# PCK warning supervisor r3

状态: COMPLETE (独立监督审查已完成)
门禁: 主会话原生 multi_agent_v1.wait_agent 对精确 worker `01a108a8-ed70-7420-8436-76f1d7aad3a3` 返回 completed, timed_out=false; 证据 `G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004/pck-warning-worker-r3-wait-gate.json` (CheckedAt 2026-10-05T04:53:18.1870959+08:00).
结论: **SUPERVISION_PASS** (仅限下述边界; 中央复验为补充证据, 非本审查执行).

## 已确认

### A. 门禁与冻结 hash

- 门禁 json 中四个冻结 hash 与四份当前文件实测 SHA256 完全一致 (最终复查 2026-10-05 05:06):
  - `G:/omp works/Sts/sts2-perfect/mod/Perfect.csproj` = `A4495FA79D1250A7475B236AAD37B9E2322C75E101996CA9F85871C0D47EE5C3` (12862 bytes, mtime 04:50:49.275).
  - `G:/omp works/Sts/sts2-mpconfigsync/mod/MpConfigSync.csproj` = `F4CE5EC5A4B0177A57115E62124457509C284C19AC79BD25FCFCE6831B256C94` (11342 bytes, mtime 04:50:49.309).
  - `G:/omp works/Sts/sts2-heartshake/mod/HeartShake.csproj` = `8F3C6C4B5FA8D72E8CAE576336BE7CD20B4532B10D9D1B86E91B7F382696995A` (12026 bytes, mtime 04:50:49.316).
  - `G:/omp works/Sts/AutoAnthonyRelics/mod/QuriousCraftingRelics.csproj` = `F44992193E9859A1C530C91645F215660673A6AE6D1C0891A67A3FBCFE1C5931` (10231 bytes, mtime 04:50:49.320).

### B. 最小修复独立复现 (字节级)

- 对中央 `*.csproj.r2-supervised` 快照逐字节比对: 四份当前文件均等于快照减去恰好一个 25 字节序列 `20x12 + "return true;" + 0A`; 该序列在每份快照中恰好出现 1 次.
  - Perfect: offset 8408, 删除前第 148 行.
  - MpConfigSync: offset 6889, 删除前第 131 行.
  - HeartShake: offset 7572, 删除前第 147 行.
  - QuriousCraftingRelics: offset 5777, 删除前第 110 行.
- 由快照重建 (减该序列) 与当前文件逐字节相等 = True; 除该行外无任何差异. 即无其它改动, 无 suppress.
- 修改后片段: try=1, catch=1, return-true=0, return-false=2; 行数各减 1 (LF 计数 204→203 / 186→185 / 203→202 / 166→165).

### C. 不 suppress 与语义保持

- 四份文件全文无 `CS0162` / `pragma warning` / `TreatWarningsAsErrors`; 唯一 NoWarn 为 `<NoWarn>$(NoWarn);MSB3270</NoWarn>` (Perfect/MpConfigSync/HeartShake 第 17 行, Qurious 第 18 行), 与 r2 快照完全相同 = 既有项, 非本轮新增.
- ParameterGroup 与 r2 快照逐字符相同 (PckPath 必填输入; LastWriteUtcTicks/Length/Sha256 输出), UsingTask 属性未变.
- 控制流: 成功 -> try 自然结束 -> 工厂自动尾部 `return Success;` (Success 默认 true, 片段内从不赋值 false) -> 返回 true, 且三个输出在尾部之前已赋值; missing -> `Log.LogError` + return false; 异常 -> `Log.LogErrorFromException` + return false.
- 错误路径不生成 digest: 两个 ReadPckMetadata 调用均未设 ContinueOnError, 返回 false 即中止 WriteQuickPckDigest 目标; 所有 `<Error>` 检查位于 `<WriteLinesToFile>` 之前; 目标 Condition 要求 PckPackerEnabled=true 且非 inner export 且非 skipped 且 `_PckPackerExitCode == 0`; BeginQuickPckDigest 在 pack 前先删除旧 digest (fail closed).
- 未采用 Code Type=Method 备选路径 (保持 Fragment), 该分支不适用.

### D. 工厂尾部契约 (独立主要证据, 非推测)

- 本机 `dotnet msbuild -version` = `17.14.51.32402`, `dotnet --info` MSBuild version = `17.14.51+25f168cee`.
- 官方源码 (dotnet/msbuild) 在 tag `v17.14.51` 与 commit `25f168cee` 两个引用下文件逐字节相同 (35664 bytes, sha256 `7B8FF6531F49A050A93C2BDE99CB6E43064CA1305057B9EC9EEE41A7CDE18204`), 即本地 MSBuild 构建提交与 tag 一致.
- 该文件: 第 226-239 行 Fragment 分支; 第 228 行 `CreateProperty(codeTypeDeclaration, "Success", typeof(bool), true)`; 第 237 行追加片段源码; 第 238 行追加 `return <Success>;`; 第 618-655 行 `CreateProperty` 将 default=true 写入字段初始化 -> 生成 `private bool _Success = true;`.
- 该官方源码中 `HasLoggedErrors` 出现次数 = 0. 即请求文本里 "按 Log.HasLoggedErrors 返回" 的设想不成立; worker 报告的更正 (实际尾部为 `return Success;`, 默认 true) 与官方源码一致.
- 既有生成文件 `G:/tmp/MSBuildTemp/tmp64eee4c6c4bf4509a99e573b926cebf8.tmp` (95 行, mtime 2026-10-02 09:46): 第 68 行 `private bool _Success = true;`, 第 70-77 行 Success 属性, 第 88 行修复前 `return true;`, 第 92 行 `return Success;` (即 CS0162 点). 边界: 该文件用户片段段使用 `Convert.ToHexString`, 与当前文件的 `BitConverter.ToString(...)` 略有不同, 仅作为工厂脚手架形状证据; 契约主要证据是上述官方源码精确提交.
- 修复前中央日志中 CS0162 均在生成文件第 92 行第 13 列 = 工厂尾部; 删除显式 return true 后成功路径可到达尾部, 警告点消失. 这与中央修复后日志 0 warning 一致 (补充证据).

### E. 四份块一致性 / 编码 / 换行 / Perfect dirty

- 四份 `<UsingTask TaskName="ReadPckMetadata" ...>...</UsingTask>` 整块逐字节相同 (1391 bytes, sha256 `1880DECCD347EC5AFAD2F542892A2428FAB3CBCF71E2E24E51F7AA7F2172D032`); 四份 CDATA 片段逐字节相同 (654 bytes, sha256 `DF0335934FCFADEC1A2F30101319D465EDA1E74C659D442C790EEC2FA256FC18`; 归一化空白后也相同).
- 编码/换行/末字节保持: Perfect/MpConfigSync/HeartShake UTF8-BOM, Qurious 无 BOM (与 r2 相同); 全部 LF-only; 末字节不变 (Perfect/HeartShake/Qurious 0x0A, MpConfigSync 0x3E).
- Perfect 既有 dirty 保留: 相对 r2 快照该 csproj 仅新增该 1 行删除; worker 窗口 (04:43:38-04:53:18) 内四个仓库非 bin/obj/.godot 写入仅四份 csproj, 其它既有 dirty 文件 mtime 均早于窗口.

### F. 补充证据 (中央执行, 非本审查运行; 仅读取)

- `G:/omp works/.tmp/workshop-prep-20261004-central/four-mods-rebuild-results.json`: 四项目 Exit=0, ProjectSha256 与冻结 hash 相同, DigestMatches=true, NoDeployProperty=true, RuntimeVerified=false; 各 `*-release-rebuild.log` 显示 "0 个警告 / 0 个错误" 及 QUICK-PCK-DIGEST-ALLOW.
- `producer-fixtures-20261005-045406/results.json`: 24 cases 全部 Passed=true, SourceSha256 与冻结 hash 相同, GameOrDeploy=false.
- 上述均为中央产物; 本报告不将它们预称为本审查的运行结果.

### G. 临时写入 / 范围偏差 (单独记录, 不改变 PASS 结论)

1. 范围偏差 (worker): 取证临时文件 `G:/tmp/RoslynCodeTaskFactory-*.cs` 属报告+四 csproj 之外写入; worker 报告称已删除, 当前 `G:/tmp` 下确无 RoslynCodeTaskFactory* 文件. 无产品影响, 已记录.
2. 过程异常 (worker): 首轮写入未截断 (残留 25 字节旧尾部), 随后 SetLength+重写修正; 最终四份文件已验证为精确最小删除, 无残留.
3. 报告笔误 (worker): 报告第 11 行版本串写作 `17.14.51+25f168ce3`, 实际为 `17.14.51+25f168cee`; 属报告文字笔误, 底层证据正确.
4. 本审查自身: 独立取证时创建并在用后删除 `G:/tmp/RoslynCodeTaskFactory-v17.14.51.cs` (reviewer-created, 已清理), 一并记录.
5. worker 窗口内 `G:/tmp/MSBuildTemp` 无任何文件被写 = 与 "worker 不构建" 一致; 中央 rebuild 在 04:58 (worker 完成之后), 不归因于 worker.

## 进行中

- 无. 审查面已全部完成.

## 未知

- 游戏内运行期行为: 未验证 (中央 results.json 中 RuntimeVerified=false; 不属本报告范围).
- 中央 24 路径夹具与四项目真实 Rebuild 的原始执行: 本报告仅读取中央产物, 未自行运行 build/parse/lint/test/pack.
- 其它 MSBuild / Visual Studio 版本的生成模板差异: 未覆盖 (本轮证据仅覆盖本机 MSBuild 17.14.51+25f168cee).
- worker 报告笔误 `25f168ce3` 未在 worker 侧更正 (本报告已记录实际值).
