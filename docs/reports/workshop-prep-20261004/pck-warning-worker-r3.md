# PCK warning fix - worker r3 report

范围: 四项目 ReadPckMetadata Fragment CS0162 消除. 只写本报告与四个 csproj 的 ReadPckMetadata 块. 不构建, 不测试, 不提交, 不委派.

## 已确认

### 条目 1 (唯一项) - 已完成实现

- 优先级: P2 (构建 Exit=0 但 warning 计数非零, 阻塞本轮 0 warning 验收).
- 触发条件: 任一四项目 Release Rebuild, 编译 RoslynCodeTaskFactory 的 Code Type=Fragment 内联任务.
- 宣称或权威契约: 消除本轮引入的 CS0162, 不 suppress warning. 真实工厂契约 (MSBuild 17.14.51): Fragment 生成类含 `_Success = true` 字段与 `Success` 属性, Execute() 尾部追加 `return Success;`. 证据: (a) 同源生成文件 `G:\tmp\MSBuildTemp\tmp64eee4c6c4bf4509a99e573b926cebf8.tmp` (95 行) 第 88 行 `return true;`, 第 92 行 `return Success;` 即警告点, 且含 `private bool _Success = true;`; (b) 官方源码 tag v17.14.51 `src/Tasks/RoslynCodeTaskFactory/RoslynCodeTaskFactory.cs` 第 228 行 `CreateProperty(codeTypeDeclaration, "Success", typeof(bool), true)`, 第 237-238 行 源片段后追加 `return Success;`, 第 618-655 行 默认值写入字段初始化; 本机 `dotnet --info` MSBuild version 17.14.51+25f168ce3 与该 tag 一致. 注意: 工厂尾部实际是 `return Success;` (Success 默认 true), 不是 `Log.HasLoggedErrors`; 成功路径语义与显式 `return true;` 等价, 故采用首选最小方案.
- 当前控制流 (修改后): try 成功 -> 自然落入工厂自动尾部 `return Success;` (true); missing/异常 -> `return false;`; 尾部可达, CS0162 消除.
- 构建日志证据 (修改前, 中央 r2): `G:\omp works\.tmp\workshop-prep-20261004-central\` 下 `Perfect-release-rebuild-r2-before-warning-fix.log`, `MpConfigSync-release-rebuild-r2-before-warning-fix.log`, `HeartShake-release-rebuild-r2-before-warning-fix.log`, `QuriousCraftingRelics-release-rebuild-r2-before-warning-fix.log` 各含 1 条 CS0162; `four-mods-rebuild-results-r2-before-warning-fix.json` 记录四项目 Exit=0.
- 可复现命令 (中央复验): `powershell -File "G:\omp works\.tmp\workshop-prep-20261004-central\rebuild-four-mods.ps1"` (本 worker 未运行, 遵守不构建约束).
- 最小修复范围: 四个 csproj 的 ReadPckMetadata CDATA 内仅删除 Fragment 成功路径一行 `            return true;`, 保留 missing/error 的 `return false;`, 未改 ParameterGroup/TaskName/targets/其它任何行.
- 修改方法: 字节级删除目标行 (25 字节 = 12 空格 + `return true;` + LF), 每文件先断言目标字节序列恰好出现 1 次; 写后回读并与 `r2-supervised` 快照逐字节比较, 结果 = 快照减去该 25 字节, 完全一致 (exactMinimalRemoval=True). 首轮写入后立即发现文件长度未截断 (残留 25 字节旧尾部), 已用 SetLength+重写修正; 最终四文件均与预期最小删除结果逐字节一致.
- 修改后核验 (非构建): 四文件 try=1, catch=1, return true=0, return false=2; BOM 保留 (Perfect/MpConfigSync/HeartShake 为 UTF8-BOM, QuriousCraftingRelics 无 BOM), 全部 LF-only, 末字节不变 (Perfect/HeartShake/Qurious=0x0A, MpConfigSync=0x3E).
- 相对 r2 快照的 git diff: 每文件恰好 `1 file changed, 1 deletion(-)`, 删除内容即 `return true;`.

### 修改前行号 (1-based, 修改前) / 修改后行号

- `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj`: 删除前第 148 行; 现 try 块为 141-148, catch 第 149 行.
- `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj`: 删除前第 131 行; 现 try 块为 124-131, catch 第 132 行.
- `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj`: 删除前第 147 行; 现 try 块为 140-147, catch 第 148 行.
- `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj`: 删除前第 110 行; 现 try 块为 103-110, catch 第 111 行.

### 最终 SHA256 与字节数 (修改后)

- `G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj`: SHA256 `A4495FA79D1250A7475B236AAD37B9E2322C75E101996CA9F85871C0D47EE5C3`, 12862 字节 (原 12887).
- `G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj`: SHA256 `F4CE5EC5A4B0177A57115E62124457509C284C19AC79BD25FCFCE6831B256C94`, 11342 字节 (原 11367).
- `G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj`: SHA256 `8F3C6C4B5FA8D72E8CAE576336BE7CD20B4532B10D9D1B86E91B7F382696995A`, 12026 字节 (原 12051).
- `G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj`: SHA256 `F44992193E9859A1C530C91645F215660673A6AE6D1C0891A67A3FBCFE1C5931`, 10231 字节 (原 10256).

### 边界遵守

- 仅写本报告与四个 csproj 的 ReadPckMetadata 块; 未提交, 未 build/parse/lint/test/pack, 未委派, 未写 C:, 未改 Steam/共享配置/游戏.
- Perfect 原有 dirty 未动: 相对 r2 快照仅新增该 1 行删除.
- 取证用临时文件 `G:\tmp\RoslynCodeTaskFactory-*.cs` 已删除.

## 进行中

- 无. 实现面已完成并落盘.

## 未知

- 修改后真实 Release Rebuild 是否 0 warning: 待中央执行, 本 worker 不构建.
- 修改后四项目 PCK/DLL 产物字节与运行期行为: 未覆盖, 属中央复验范围.
- 其它 MSBuild/Visual Studio 版本下的生成模板差异: 未覆盖 (本轮证据仅覆盖本机 MSBuild 17.14.51).