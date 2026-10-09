# 独立原生测试载体实现报告 (r6 worker)

CODE_COMPLETE

## 已确认
- [P0] 旧载体迁移的真实依赖面很小: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs` 共 3615 行, 对 Spire1 的引用只有 4 处 (第24行 `using Spire1.Spire1Code.Forms;`, 第26行 `namespace Spire1.Spire1Code.Run;`, 第30行与第3584行 env 名 `SPIRE1_FORM_SMOKE_REPORT`). 其余 `FormStanceWatcherBridge` / `FormStanceMode` / `FormStanceModifier` / `WatcherFormStancePower` / `FormStanceKind` 全部来自 Forms.dll 的 `Forms.FormsCode` 命名空间 (已逐个核对 namespace 声明). 因此只需 namespace/usings 适配, 业务控制流零改动.
- [P0] 旧 patch 仅 15 行: class-level `[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]` + postfix 调 `FormNativeSmokeRunner.TryStart(__instance)`.
- [P0] 生产 Forms.dll 公开生命周期面已核对: `Forms.FormsCode.MainFile` (public, `ModId="Forms"`, Initialize/Shutdown/PatchesHealthy, MainFile.cs 第83行 `new Harmony("Forms")`), `FormStanceWatcherBridge` (public, `HarmonyId="Forms.FormStanceMode.Watcher"` 第38行, 第168行 `new Harmony(HarmonyId)`, TryBind/TryBindOnMainThreadEntry/IsAvailable/UnavailableReason/BoundTargets/Shutdown/CurrentKind), `FormStanceMode` (public), `FormStanceKind` (public enum None/Calm/Wrath/Divinity, FormStanceMode.cs 第8-14行), `FormStanceModifier` (public sealed), `WatcherFormStancePower` (public abstract). 测试侧 owner 常量与生产逐字一致.
- [P0] 生产 Forms 工程已无自动部署目标 (Forms.csproj 末尾明确移除 auto-copy), `Sts2PathDiscovery.props` 不回退 Steam. 测试工程照抄该 discovery.
- [P0] 可用的既有生产字节 (供 hub 作 `FormsDllPath`): `G:\omp works\.tmp\forms-independent-20261005\forms-build-r4\Forms.dll` 与 `...\forms-r4-intermediate-startup-payload\Forms.dll`, 均 116736 字节, 2026-10-05 06:11:52.
- [P0] 迁移后 `FormNativeSmokeRunner.cs` 已无任何 Spire1 类型/命名空间引用, 仅保留环境变量名 `SPIRE1_FORM_SMOKE_REPORT` 两处 (有意兼容旧控制器), 不构成 Spire1 代码引用.
- [P0] 静态复核: 无 `ProjectReference`; 无 Publish/auto-deploy/copy-to-mods target; 括号平衡 (runner 390/390, lifecycle 51/51, MainFile 25/25); `0Harmony`/`sts2`/`Forms` 三个 Reference 均 `Private=false`; Publicizer 仅 `sts2`.

## 已写文件 (唯一写集 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke`)
- `FormsNativeSmoke.csproj`: Godot.NET.Sdk/4.5.1 + net9.0; 0Harmony/sts2 取 `$(Sts2DataDir)` Private=false; Publicizer 仅 sts2; `FormsDllPath` 必填, Private=false, 无 ProjectReference; BaseLib 3.4.5; 显式把 `CopyToModsFolderOnBuild`/`CopyQuickPck`/`GodotPublish`/`PublishReadyToRun` 钉为 false; 无任何部署 target.
- `FormsNativeSmoke.json`: has_pck=false, has_dll=true, dependencies BaseLib+Forms, test-only, affects_gameplay=false.
- `Sts2PathDiscovery.props` / `Directory.Build.props` (无 GodotPath, 不触 C:) / `NuGet.config` / `project.godot` / `GlobalUsings.cs`.
- `MainFile.cs`: 测试载体自有 Initializer, 只扫本程序集, 装 NGame._Ready 补丁后**验证补丁确已安装**, 失败则回滚并 Error 日志 + `PatchesHealthy=false` + `LastPatchFailure` (不吞).
- `FormNativeSmokePatch.cs`: NGame._Ready postfix, 分别调用两个 opt-in runner.
- `FormNativeSmokeRunner.cs`: 迁移版, 仅 namespace/usings 适配; 保留原三姿态场景 (calm/wrath/divinity/turns), 固定种子 FORMNATIVE20261001, 真实 Watcher 牌, 真实 actions, 全部超时/退出/证据机制. 未删除 Blasphemy 即死或力量费用规则.
- `LifecycleSmokeRunner.cs`: `--forms-lifecycle-smoke`, 见下.
- `README.md`: 中文测试契约/边界/开关/证据路径/不可为变绿删除的语义.

## LifecycleSmokeRunner 覆盖 (第3条规格)
1. 等 Godot 主线程 ModelDb 就绪后核对 `FormStanceWatcherBridge.TryBind() && IsAvailable` (bounded 120s).
2. 重复 3 次**生产** `Forms.FormsCode.MainFile.Initialize()` + `FormStanceWatcherBridge.TryBind()`, 统计 `Forms` 与 `Forms.FormStanceMode.Watcher` 两 owner 的 **patch 条目数** (非方法数), 断言不叠加.
3. 从已加载 Watcher 的 `Location` 读字节, `Assembly.Load(byte[])` 造同 simple-name 不同实例; 等真实主线程 pump 帧消费 `AssemblyLoad` 通知, 核对 bridge 不再 Available 且进入 Terminal (需重启).
4. `Forms.FormsCode.MainFile.Shutdown()` 两次 + `FormStanceWatcherBridge.Shutdown()` 两次, 核对两 owner 零残留 patch.
- 不执行第二次 `ModelDb.Init` (避免改变引擎缓存).
- 诚实边界 (已写进 JSON `claim` 与 README): 只称"同名多程序集隔离复现", **不冒充真正卸载/热替换**; 字节副本 MVID 与 FullName 必然相同, 已如实记 `sameMvidByteCopy=true` + `distinctAssemblyInstance=true`, 不谎称 differentIdentity. Watcher 若从内存加载 (Location 空) 则如实记 `not-attempted` + `duplicateBlockedReason`, 不写 stub, 不伪造通过.

## 进行中 (已修正的实现缺陷, 供监督复核)
- (a) LifecycleSmokeRunner 中裸 `MainFile` 曾可能解析到测试载体自身而非生产 Forms, 已改为全限定 `Forms.FormsCode.MainFile`.
- (b) 两个 runner 中 30 处 `MainFile.Logger` 在 `using Forms.FormsCode;` 存在时存在解析歧义, 已全部改为全限定 `FormsNativeSmoke.MainFile.Logger` (runner 23 处, lifecycle 7 处).
- (c) owner patch 计数由"方法数"改为"patch 条目数", 使叠加可被发现.
- (d) `ModAnalyzers` 已从测试工程移除 (test-only 载体无 localization 表, 避免无关 analyzer 失败).
- (e) 移除测试工程对 `C:/megadot/...` GodotPath 的引用.

## 未知 (未验证, 未声称通过)
- 编译: 本写集按请求**不构建**, 未跑 `dotnet build`; 编译结果未知.
- 实机: Forms+Watcher 已加载时 bridge Bound / 三姿态场景 / turns 的 partial 语义 / 退出码 / JSON 证据.
- 生命周期实机: 重复 Initialize/TryBind 不叠 patch, 同名多程序集隔离, 双 Shutdown 无残留 patch.
- `FormsDllPath` 指向 r4 字节时的运行时兼容性 (测试工程按 r4 生产字节编译, 未运行).

## 编译/实机未知项汇总 (交付面)
- 修改文件: 上述 12 个文件, 全部位于 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke`.
- 未修改任何生产代码/共享文档/游戏/共享配置, 未执行 git, 未写 C:.
- 下一步 (hub): 用 `dotnet build FormsNativeSmoke.csproj -c Release -p:FormsDllPath=<r4 Forms.dll>` 编译, 输出到 G: 隔离目录, 再按显式参数运行.