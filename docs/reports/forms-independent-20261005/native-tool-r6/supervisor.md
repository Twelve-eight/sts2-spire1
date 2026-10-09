# Supervisor Report - native-tool-r6

SUPERVISION_IN_PROGRESS

- 门禁: 同目录 gate-notice.txt 声明主会话真实 multi_agent_v1.wait_agent 对 worker 01a10944-3b64-70e1-a27b-4be20351158d returned completed, timed_out=false; coordination.md 第 20-22 行落盘同证据 (2026-10-05T07:43:03.5050217+08:00, 工具未提供准确事件时间/调用标识)。
- 唯一指定模型: global:deepseek-v4.1-flash / wb2api / xhigh。未委派, 未构建, 未测试, 未运行游戏, 未执行 git, 未改产品代码。
- 审查范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke (12 文件)。

## 已确认

- [P0] 迁移语义保持 (证据级): `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs` (3615 行) 与 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs` (3615 行) 归一化 namespace/usings 后逐行比对, 46 条差异全部是 `MainFile.Logger` -> `FormsNativeSmoke.MainFile.Logger` 的全限定改写 (worker.md 进行中 (b) 所述)。旧载体对 Spire1 的引用仅 4 处 (第 24 行 using, 第 26 行 namespace, 第 30 与 3584 行环境变量名), 新载体已无 Spire1 类型/命名空间引用。语义 token 计数一致: Blasphemy 6/6, strength 11/11, Death 2/2, FORMNATIVE20261001 1/1, --form-native-smoke 7/7, turns 28/28。
  复现: `Compare-Object` 归一化行比对; `Select-String -Pattern 'Spire1'`。命令见本报告末节。
- [P0] 无 Spire1 引用 (证据级): 对测试目录 12 文件全量 grep `Spire1` 只命中 4 类: 注释/README 声明不引用 Spire1, 以及 `FormNativeSmokeRunner.cs:30` 与 `:3584`、`LifecycleSmokeRunner.cs:42` 的 `SPIRE1_FORM_SMOKE_REPORT` 环境变量名 (规格第 4 条要求兼容旧控制器, 属有意保留)。无 `ProjectReference`, 无 Spire1 类型使用。
- [P0] 无生产测试泄漏 / 无自动部署 (证据级): `FormsNativeSmoke.csproj` 无 `<ProjectReference>`; 唯一 `<Target>` 是 `CheckDependencyPaths` (第 82-89 行), 只做 `Error` 校验, 无 `Copy`/`Exec`/Publish/auto-deploy。`CopyToModsFolderOnBuild`/`CopyQuickPck`/`GodotPublish`/`PublishReadyToRun` 均显式 false (第 33-36 行)。工程树向上无 `Directory.Build.targets`; 仓库根无 `Directory.Build.props/targets/Packages.props`。tests 目录下无 obj/bin 残留。`Sts2PathDiscovery.props` 无 Steam 回退 (第 28-34 行), 且测试工程不设 `GodotPath`。
- [P0] 依赖与引用面 (证据级): `FormsDllPath` 为必填 `Error` 校验 (第 86-88 行), Reference `Forms` `Private=false` (第 60-65 行); `0Harmony`/`sts2` 取 `$(Sts2DataDir)` 且 `Private=false` (第 43-52 行); Publicizer 只 `Include="sts2"` (第 54-57 行); `Alchyr.Sts2.BaseLib 3.4.5` (第 68 行); `ModAnalyzers` 已移除 (第 69 行注释)。
- [P0] manifest 契约 (证据级): `FormsNativeSmoke.json` 第 8-9 行 `has_pck=false` / `has_dll=true`; 第 10-19 行 dependencies = BaseLib(3.4.5) + Forms(0.1.0); 第 20 行 `affects_gameplay=false`; 第 5 行 description 明确 TEST-ONLY。与规格第 2 条一致。
- [P0] 载体 Initializer 失败不吞 (证据级): `MainFile.cs` 第 51-68 行 Initialize 捕获异常后记录 `LastPatchFailure` + `Logger.Error`, 置 `_patchesInstalled=false`/`_patchesHealthy=false`; 第 74-147 行只扫 `typeof(MainFile).Assembly` (不扫 Forms/Spire1), 失败 rollback `UnpatchAll(ModId)`; 第 117-140 行用 `Harmony.GetPatchInfo(NGame._Ready)` 验证本 owner postfix 确已安装, 未装则回滚并置失败。第 149-167 行 Shutdown 幂等。
- [P0] 报告路径边界 (证据级): `FormNativeSmokeRunner.cs` 第 3591-3604 行与 `LifecycleSmokeRunner.cs` 第 364-373 行都先 `Path.GetFullPath`, 再要求以 `G:\` 或 `G:/` 开头, 否则拒绝写入; 只写 `form-native-smoke-*.json` / `forms-lifecycle-smoke.json`。测试目录全量 grep 无 `AppData`/`mod_configs`/`Steam`/`steamapps`/`Roaming`/`SpecialFolder` 写入路径; `C:`/`Steam` 命中仅出现在 `Sts2PathDiscovery.props` 的"拒绝回退 Steam"注释中。无共享存档写入路径。
- [P0] LifecycleSmokeRunner 的声明边界诚实 (证据级): `LifecycleSmokeRunner.cs` 第 33-36、70 行明示 `Assembly.Load(byte[])` 是"同名多程序集隔离复现, NOT a true unload/hot-swap"; 第 222-224 行如实记录 `distinctAssemblyInstance` 与 `sameMvidByteCopy` (字节副本 MVID/FullName 必然相同), 未谎称 differentIdentity; 第 205-210 行 `Location` 为空时标 `loaded=false` + failure, 第 143-148 行把不可尝试写成 `not-attempted` + `duplicateBlockedReason`, 不伪造通过。第 38 行注释明示不第二次 `ModelDb.Init`。
- [P0] 生产 Forms 契约与测试断言对齐 (证据级, 静态): `FormStanceWatcherBridge.cs` 第 643-655 行 `BoundIdentityStillValid` 要求恰好 1 个 `Watcher` 程序集且 `ReferenceEquals(current, bound)` 且 identity 字符串相等, 否则第 158-159 行 `EnterTerminalLocked("Watcher assembly identity changed after binding; hot reload is not supported, restart the process")`。测试第 133-139 行等待 `!IsAvailable && UnavailableReason.Contains("identity changed")` 并把 `terminalRequiresRestart` 设为同一结果, 断言与生产控制流一致。第 251-280 行 `TryBindOnMainThreadEntry` 是 pump 消费 AssemblyLoad 的真实入口, 测试靠真实主线程 pump 帧消费而非手动调 TryBind, 与规格第 3 条一致。
- [P0] 双 owner 重复 Initialize 不叠 patch 的断言口径 (证据级): `LifecycleSmokeRunner.cs` 第 236-268 行按 "patch 条目数" 统计两个 owner (`Forms` 与 `Forms.FormStanceMode.Watcher`), 遍历 `Harmony.GetAllPatchedMethods()` 的 Prefixes/Postfixes/Transpilers 累加 owner 命中数 (第 250-261 行), 而非按方法数, 能发现同方法重复叠加。生产侧 `Forms.FormsCode.MainFile.Initialize` 第 76-80 行 `_patchesInstalled` 早退, `FormStanceWatcherBridge.TryBind` 第 147-153 行 Bound 早退, 与"重复调用不叠 patch"契约一致。

## 进行中

- 仍在核对: 生产 Forms.dll 字节中测试所引用的全部公开成员是否可解析 (已确认关键成员名字符串存在, 未做完整 API 面比对); sts2 helper API (`TaskHelper.UnobservedFault`/`NGame.IsMainThread`) 已在 sts2.dll 字符串面确认存在。
- 仍在核对: 编译未知项 (本写集按请求不构建, 不代跑编译)。

## 未知

- 编译: 未跑 `dotnet build`, 编译结果未知 (本写集与监督均被要求不构建)。
- 实机: Forms+Watcher 已加载时 bridge Bound / 三姿态 / turns / 退出码 / JSON 证据未运行验证。
- 生命周期实机: 重复 Initialize/TryBind 不叠 patch, 同名多程序集隔离, 双 Shutdown 无残留 patch 未实机验证。
- 实机证据: `FormsDllPath` 指向 r4 生产字节时的运行时兼容性未验证。
- wb2api 细路由: coordination.md 记为 Unknown, 无法从本会话独立复核 provider 细路由。

## 复现命令

- 归一化差异比对 (PowerShell): 见本会话执行的 `Norm` 函数 + `Compare-Object -SyncWindow 5`。
- Spire1 grep: `Select-String -LiteralPath (Get-ChildItem 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke' -Recurse -File) -Pattern 'Spire1'`
- 路径边界 grep: `Select-String -Pattern 'AppData|mod_configs|Steam|steamapps|Roaming|SpecialFolder'`
- 工程面 grep: `Select-String -Pattern '<Target|<Import|Copy |Exec '`

## 门禁与范围复核 (第二轮, 增量)

- 门禁证据独立复核: `gate-notice.txt` 与 `coordination.md` 第 20-22 行一致, 记录 `multi_agent_v1.wait_agent targets=[01a10944-3b64-70e1-a27b-4be20351158d] returned completed, timed_out=false`, 落盘时点 `2026-10-05T07:43:03.5050217+08:00`; 工具未提供准确事件时间/调用标识 (协调文件已如实标注)。CODE_COMPLETE 未被当作门禁, 门禁来自真实工具结果。通过。
- 写集范围: 12 个文件全部位于 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke`, 时间戳集中在 2026-10-04 23:37-23:41 UTC; `sts2-forms` 仓库为初始未提交状态 (无 HEAD), 无法用 git diff 归因, 但测试目录外无本批新增文件迹象, 且写集不含 `mod/` 下任何文件。未改产品代码/共享配置。

## 剩余检查面结论 (增量)

- [P0] 生产 `Forms.dll` 公开面与测试引用对齐: 测试引用的全部 Forms 类型在 `mod/FormsCode` 均为 `public` (`FormStanceWatcherBridge`, `FormStanceMode`, `FormStanceKind`, `FormStanceModifier`, `WatcherFormStancePower`, 六个具名 power 类)。`WatcherFormStancePower` 仅作为 `OfType<>` 过滤使用 (runner 第 1689/1693 行), 其 `NativeMarker` 为 `internal` 未被测试直接访问, 无跨程序集可见性问题。`FormsDllPath` 指向的 r4 字节 (sha256 `6CCB8B21...`) 中已含 `CurrentKind`/`BoundTargets`/`UnavailableReason`/`TryBindOnMainThreadEntry`/`MarkOwnerPatchesFailed`/`PatchesHealthy`/`FormStanceWatcherBridge`/`WatcherFormStancePower`/`FormStanceModifier`/`FormStanceKind` 全部符号字符串。
- [P1] `GodotPath` 缺失不阻断纯 build: 生产 `mod/Directory.Build.props` 第 5 行把 `GodotPath` 指向 `C:/megadot/...`, 但该属性只在 PckPacker/export-pack 路径使用; 测试工程自带 `Directory.Build.props` (无 GodotPath) 且不引 PckPacker/ModAnalyzers, 无 publish/export target, 因此纯 `dotnet build` 不需要该可执行文件, 也不触碰 C:。测试 `NuGet.config` 第 5-6 行把 globalPackagesFolder/repositoryPath 钉在 `G:\omp works\Sts\sts2-forms\.nuget\packages`, 缓存不落 C:。
- [P1] sts2 helper API 存在: `sts2.dll` 含 `TaskHelper.UnobservedFault`、`TaskHelper.RunSafely`、`NGame.IsMainThread`、`Harmony.GetAllPatchedMethods` 相关符号; 0Harmony.dll 存在。未做 IL 级签名比对 (不构建约束下无法用编译器验证), 记为静态面确认。
- [P1] 线程边界 (源码级): 测试的 `NGame.IsMainThread()` 只经 `InvokeOnMainThreadAsync` 在 runner 已运行后的主线程入口调用 (runner 第 3255 行, lifecycle 第 302 行), 不在进程早期非主线程调用; 对照 Spire1 的 `AutoAnthonyLoadHook.cs` 第 20-21 行明确警告该 API "首次调用可能把非主线程当作主线程", 迁移载体调用点均在 `NGame._Ready` postfix 之后, 风险低于该警告场景, 但属未实机验证的边界。
- [P1] 测试独立性: 测试工程只以 `Reference` 方式消费既有 `Forms.dll` 字节, 无 `ProjectReference`; 生产 Forms 工程本身无 auto-copy 目标 (mod/Forms.csproj 第 68-72 行注释确认已移除)。二者构建相互独立, 测试构建不可能重建/替换生产字节。`Directory.Build.props` 只在 `mod/` 与 `tests/FormsNativeSmoke/` 各自存在, 无跨目录继承。

## 未覆盖 / 不构成本轮证据

- 编译与实机运行均未执行 (任务硬约束), 因此以上均为源码/字节静态证据, 不等于行为验证。
- `wb2api` 细路由未能独立复核 (coordination.md 标注 Unknown)。
- r4 `Forms.dll` 与当前 `mod/FormsCode` 源码的一致性未逐成员核对, 仅核对了名字面与生产契约控制流。

## 最终裁定

SUPERVISION_PASS

- 依据: 门禁证据真实成立 (wait_agent returned completed, timed_out=false); 迁移语义逐行归一化比对无行为差异; 无 Spire1 代码引用 (仅保留规格要求的环境变量名); 无生产/测试泄漏与无自动部署; 报告路径限定 G:; LifecycleSmokeRunner 边界声明诚实且断言与生产控制流一致; 测试独立性成立。
- 限制: 以上为静态审查结论, 不替代编译与实机验证; 本写集按请求未构建、未测试、未运行游戏。后续 hub 若编译/实机发现问题, 应按新证据重新裁定。
- 唯一报告: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-tool-r6\supervisor.md`
