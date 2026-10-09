# 部分模组交叉启动中央实机矩阵 — 2026-10-02

范围：`G:\omp works\Sts\sts2-spire1`

本报告只记录隔离的非 Steam 测试副本启动证据，不把它外推为 UI、存档、多人或形态战斗验收。

## 已确认

### 1. 修复并确认了上一轮矩阵夹具的真正阻断点

- 旧夹具：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r24-20261002.ps1`。
- 反编译契约：`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModSettings.cs:8-15` 把 `mod_settings.mods_enabled` 映射为 `PlayerAgreedToModLoading`；`ModManager.cs:124-133` 在同意后才调用 mod initializer；`ModManager.cs:677` 对未同意状态输出 `user has not yet seen the mods warning`。
- 旧夹具把预置 `settings.save` 写到矩阵根 `...\partial-mod-matrix-r24-20261002\appdata\...`，但启动时却为每个 case 设置了独立的 `APPDATA=...\<case>\appdata`，因此游戏读取的是 case 内新建的默认设置。r24 的全部 initializer=false 只能证明夹具未给游戏提供同意状态，不能证明 mod 启动失败。
- 新夹具：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r25-20261002.ps1`。
- 修复内容：`Prepare-Settings` 改为接收 case 路径，并把设置写入对应的 `...\<case>\appdata\SlayTheSpire2\steam\76561199520000001\settings.save`；启动前实测该文件包含 `mod_settings.mods_enabled=true` 与 `seen_ea_disclaimer=true`。
- 可复现检查命令：

```powershell
Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModSettings.cs'
Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModManager.cs' -Pattern 'PlayerAgreedToModLoading|user has not yet seen the mods warning|Calling initializer'
Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r25-20261002.ps1' -Pattern 'function Prepare-Settings|settingsPath =|Prepare-Settings -Case'
```

### 2. 矩阵 1：BaseLib + Spire1，无 Watcher，实际加载通过

证据目录：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r25-20261002\m1-baselib-spire1-no-watcher`

- `staging.json`：顶层只有 `BaseLib`、`Spire1`，`nestedManifestPaths=[]`。
- `stdout.log`：发现两个 manifest；调用 `BaseLib.BaseLibMain` 与 `Spire1.Spire1Code.MainFile`；输出 `Loaded 2 mods (2 total)`。
- `run.json`：`exitCode=0`、`timedOut=false`、`nonzeroWindowHandleObserved=false`、`spire1Initializer=true`、`watcherInitializer=false`、`baselibInitializer=true`、`typeLoadOrFileNotFound=[]`。
- 结论：Watcher 没有被当作 Spire1 的启动硬前置；Spire1 在 BaseLib 满足时完成 initializer。

### 3. 矩阵 2：BaseLib + Watcher，无 Spire1，实际加载通过

证据目录：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r25-20261002\m2-baselib-watcher-no-spire1`

- `staging.json`：顶层只有 `BaseLib`、`Watcher`，`nestedManifestPaths=[]`。
- `stdout.log`：调用 `BaseLib.BaseLibMain` 与 `WatcherMod.WatcherBootstrap`；没有 Spire1 initializer；输出 `Loaded 2 mods (2 total)`。
- `run.json`：`exitCode=0`、`spire1Initializer=false`、`watcherInitializer=true`、`baselibInitializer=true`、`typeLoadOrFileNotFound=[]`。
- 结论：Watcher 不会通过 Spire1 的 staging 或 bridge 获得隐式前置；没有 Spire1 时 Watcher + BaseLib 可独立启动。

### 4. 矩阵 3：BaseLib-only，实际加载通过

证据目录：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r25-20261002\m3-baselib-only`

- `staging.json`：顶层只有 `BaseLib`，`nestedManifestPaths=[]`。
- `stdout.log`：只调用 `BaseLib.BaseLibMain`；输出 `Loaded 1 mods (1 total)`。
- `run.json`：`exitCode=0`、`spire1Initializer=false`、`watcherInitializer=false`、`baselibInitializer=true`、`typeLoadOrFileNotFound=[]`。
- 结论：BaseLib 不依赖 Spire1 或 Watcher 的初始化副作用。

### 5. 矩阵 4：Spire1 无 BaseLib，显式依赖失败且未进入 initializer

证据目录：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r25-20261002\m4-spire1-no-baselib`

- `staging.json`：顶层只有 `Spire1`，`nestedManifestPaths=[]`。
- `stdout.log`：发现 `Spire1.json`，输出 `Loaded 0 mods (1 total)`。
- `stderr.log`：实际输出 `Tried to load mod Spire1, but it depends on mods which have not been loaded: BaseLib!`。
- `run.json`：`exitCode=0`、`spire1Initializer=false`、`watcherInitializer=false`、`baselibInitializer=false`、`typeLoadOrFileNotFound=[]`。
- 结论：缺少 BaseLib 时由 loader 按 manifest 依赖阻断 Spire1；没有进入半初始化或 TypeLoad/FileNotFound 路径。这里的进程退出码 0 是游戏正常退出码，不能解释为该 mod 集合“成功加载”；成功/失败信号来自 loader 的 `Loaded 0 mods` 与错误文本。

### 6. 矩阵 5：完整三 mod 对照，顺序与桥接仍然成立

证据目录：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r25-20261002\m5-full-control`

- 顶层只有 `BaseLib`、`Watcher`、`Spire1`，`nestedManifestPaths=[]`。
- `stdout.log` 中 initializer 顺序为 `BaseLib.BaseLibMain`、`WatcherMod.WatcherBootstrap`、`Spire1.Spire1Code.MainFile`；输出 `Loaded 3 mods (3 total)`。
- 同一日志出现 `Spire1 Forms: Watcher bridge bound`，说明该对照集合仍能进入 Watcher bridge 的已加载分支。
- `run.json`：三个 initializer 均为 true，`typeLoadOrFileNotFound=[]`，`exitCode=0`。
- 结论：部分集合的正/负向结果与完整集合的桥接对照互相一致。

### 7. 隔离与 staging 安全条件通过

- 汇总：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r25-20261002\matrix-summary.json`。
- 五个 case 均 `exitCode=0`、无超时、无窗口句柄、`nestedManifestCount=0`、`sharedConfigSha256Unchanged=true`。
- 汇总 `steamSafe=true`；运行目标为 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-r21-20261002\game`，不是 Steam 安装目录。
- 五个 case 的实际 settings.save 均在各自隔离 APPDATA 下被读取并保存为 `mod_settings.mods_enabled=true`、`seen_ea_disclaimer=true`，没有触碰共享 `mod_configs`。

## 进行中

- 已完成部分 mod 交叉启动矩阵；r25 是有效运行证据，r21-r24 的 `initializer=false` 结果仅作为夹具失败的历史诊断，不作为产品结论。
- 已完成当前 Release DLL 的 AssemblyRef/manifest/type 门禁，见 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-final-20261002.json`。
- 已完成 AutoAnthony 晚加载桥接静态监督，见 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-late-load-reviewer-rework2-20261002.md`。
- 待完成：使用当前 Release 产物重新执行 Wrath、Calm、Divinity 三路径 native smoke，并把形态行为与部分 mod loader 证据分开记录。

## 未知

- 该矩阵没有验证 UI 交互、存档恢复、多人握手或形态卡牌在真实战斗中的行为。
- m1 证明 Spire1 无 Watcher 可以启动，但没有证明 Watcher bridge 的每一个缺失能力调用点都被业务路径触发后正确 fail-closed；这仍由源码门禁与后续形态 smoke 覆盖。
- AutoAnthony、AutoAnthonyWatcher 在当前 m1-m5 中未挂载；它们的“可选”结论依靠源码/AssemblyRef/manifest 门禁，尚无它们分别晚加载或缺失时的真实运行日志。
- 运行 JSON 的时间字段由本机进程写入，原样为 `2026-10-03T04:50:42+08:00` 至 `2026-10-03T04:51:32+08:00`；本报告不把该时间字段当作当前日期判断，只保留为证据时间戳。
