# Forms 返工 - round3 实现者报告

范围: 仅产品写集 `G:\omp works\Sts\sts2-forms` 与本报告文件. 未构建, 未 lint, 未测试, 未部署,
未运行游戏, 未改 Steam / 共享 mod_configs / canonical Release / Workshop staging, 未写 C:,
未执行 git add -A / reset / clean, 未再委派, 未启动其它 agent 运行时.

模型/路由: 请求文件指定 `global:deepseek-v4.1-flash`, 路由 wb2api, reasoning xhigh; 仅用当前 harness 原生设施.

## 已确认

### W-01 [P1] 中央诊断构建失败原因核实为 Logger 歧义
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs:4,6,24` (修复前).
- 证据: 主会话日志 `G:\omp works\.tmp\forms-independent-20261005\forms-build-r1.log` 为 CS0104,
  `Logger` 在 `Godot.Logger` 与 `MegaCrit.Sts2.Core.Logging.Logger` 之间不明确; 0 警告 1 错误.
  本实现者只读核对源码, 与日志一致; 未重跑构建.
- 已修复: 属性类型完全限定为 `MegaCrit.Sts2.Core.Logging.Logger`.
- 尚缺实机证据: 中央集中构建.

### W-02 [P1] Forms.csproj 默认自动复制目标会写 ModsPath
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\Forms.csproj` (修复前 `CopyToModsFolderOnBuild`).
- 证据: 目标仅在 `CopyToModsFolderOnBuild == false` 时跳过, 默认运行, 直接把 DLL/manifest/PDB
  复制到 `$(ModsPath)$(MSBuildProjectName)/`; 无最终绝对路径身份门禁. `Sts2PathDiscovery.props`
  的测试副本优先不防止 `/p:ModsPath=` 或 `/p:Sts2Path=` 显式覆盖.
- 已修复: 删除该目标, 不保留 opt-in 开关; 部署改由主会话隔离打包执行.
- 验证: `Select-String CopyToModsFolderOnBuild|DestinationFolder` 计数为 0.
- 尚缺实机证据: 无 (静态产品边界).

### W-03 [P1] AssemblyLoad 订阅签名矛盾
- 路径与行号: `FormStanceWatcherBridge.cs:53,236-258` (修复前).
- 证据: 字段声明为 `Action?`, 但 `OnAssemblyLoad(object?, AssemblyLoadEventArgs)` 两参数,
  赋给 Action 并用于 `AppDomain.AssemblyLoad` 订阅/退订; 该事件需要 `AssemblyLoadEventHandler`.
- 已修复: 字段改为 `AssemblyLoadEventHandler?`, 保持具名回调与成对订阅/退订.
- 尚缺实机证据: 中央集中构建.

### W-04 [P1] 菜单已 ready 后晚加载 Watcher 无主线程消费者
- 路径与行号: `FormStanceMainMenuRetryPatch.cs`, `FormStanceModePatch.cs`, `FormStanceWatcherBridge.cs`.
- 证据: 修复前真正绑定只由 ModelDb.Init postfix 与 NMainMenu._Ready postfix 触发; OnAssemblyLoad
  只写 `_assemblyLoadPending=true`, 无常驻帧回调/队列消费者. 菜单 ready 之后加载 Watcher 时 pending
  不会被再次消费, 桥可无限停在 Retryable.
- 已修复: 新增 `FormStanceBridgePump` (Godot 主线程 Node), `EnsurePumpForRetry`/`PumpTick`;
  ModelDb.Init postfix 与 NMainMenu._Ready postfix 在 Retryable 时启动/保持 pump. pump 仅在 Retryable 存活,
  每帧 O(1); 仅 AssemblyLoad (或首次尝试) 才进入有界重试突发 (`PumpMaxAttempts=120`, `PumpSkipFrames=3`).
  无每帧 AppDomain 全扫描, 无工作线程 Godot.
- 尚缺实机证据: Watcher 晚加载真实路径 (中央实机).

### W-05 [P1] Shutdown 在撤 patch 后才发布不可运行状态, 回调/helper 无统一关闭门禁
- 路径与行号: `FormStanceWatcherBridge.cs` 回调/helper 区, `FormStanceMode.cs`.
- 证据: 修复前 Shutdown 先逐条 Unpatch, 之后才置 ShuttingDown; Marker/stance/damage 回调与三个注入
  原生 async MoveNext 的 helper 直接调 `IsEnabled`, 关闭后对 Forms 对局抛异常而非立即返回;
  `NotifyEndTurnDivinity` 会访问已清空的 `_nativeNotification` 并抛异常.
- 已修复: Shutdown 先发布 `ShuttingDown` 再撤 patch; 新增绑定代数 `_bindingGeneration` +
  `BindingLease`/`TryAcquireLease`/`IsLeaseCurrent`, async 回调与 await 后续按代数核对;
  新增 `CallbacksAllowed` 非抛异常门禁; `IsSelectedAndBound` 用于 patch 入口;
  `NotifyEndTurnDivinity` 在 delegate 为空时记录并返回而不抛.
- 保留: 非 Forms 对局与关闭后仍走原生分支 (GainEnergy/Remove/原生通知).
- 尚缺实机证据: 退出/回滚真实路径 (中央实机).

### W-06 [P1] Bound 早返回绕过 AssemblyLoad pending 与身份/代数核对
- 路径与行号: `FormStanceWatcherBridge.cs` `TryBind`/`TryBindOnMainThreadEntry`/`BoundIdentityStillValid`.
- 证据: 修复前 Bound 时直接 return true, 不消费 pending, 不核对 Watcher 程序集身份.
- 已修复: Bound 早返回消费 `_assemblyLoadPending` 并核对 Watcher 程序集实例与 MVID
  (`BoundIdentityStillValid`); 不一致立即 `EnterTerminalLocked` -> Terminal, 要求完整重启进程.
  契约不宣称真正热替换成功.
- 尚缺实机证据: 真实程序集替换 (中央实机, 未知).

### W-07 [P1] MainFile 部分 patch 失败仍置 _patchesInstalled=true 并发布桥
- 路径与行号: `MainFile.cs`.
- 证据: 修复前 failed>0 只记日志, 仍置 `_patchesInstalled=true`; 部分失败不回滚本 owner, 也不阻止桥 Bound.
- 已修复: 全部成功才 `PatchesHealthy`; 部分失败 `harmony.UnpatchAll(ModId)` 回滚本 owner 并调用
  `MarkOwnerPatchesFailed` -> Terminal, 桥拒绝发布 Bound; 新增显式 `MainFile.Shutdown()` 覆盖自身 patches
  与 ProcessExit 订阅.
- 尚缺实机证据: 部分 patch 失败路径 (中央实机, 未知).

### W-08 [P1] Spire1 反射通知缓存旧静态 MethodInfo, 无身份替换检测
- 路径与行号: `FormStanceWatcherBridge.cs` 末尾 `Spire1StanceNotification`.
- 证据: 修复前 `_resolved`/`_dispatch` 一旦解析永久缓存, 无程序集实例或 MVID 核对.
- 已修复: 缓存处记录声明程序集与 MVID, 每次解析核对; 身份替换或同 simple name 多程序集时丢弃旧
  MethodInfo 并禁用通知; 新增 `Reset()`, Shutdown 调用.
- 尚缺实机证据: 真实替换 (未知).

### W-09 [P2] 仓根缺少 .gitignore
- 路径与行号: `G:\omp works\Sts\sts2-forms\.gitignore` 之前不存在; `mod/.gitignore` 只覆盖 mod/ 子树.
- 证据: 仓根 `Get-ChildItem -Force` 只有 `.omp/`, `mod/`, `DEVELOP.md`, `DEVLOG.md`, `NuGet.config`;
  `Test-Path .gitignore` = False.
- 已修复: 新增仓根 .gitignore, 排除 `.nuget/bin/obj/build/temp/.godot` 等生成物. 未执行 git add/commit.

### W-10 [P3] 接续增量契约已读取, 收窄返工保证层级
- 路径与行号: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md` 末节.
- 证据: 契约明确 (a) 显式关闭并移除默认自动部署; (b) 已选模式而运行桥不可用时 IsSelected/Enter/Exit
  均须明确失败; (c) 重复初始化/重复 ModelDb.Init/一次身份晚加载幂等, 已就绪菜单后的 AssemblyLoad
  必须由持续且有界的主线程消费者唤醒; (d) 真正程序集替换或同 simple name 多程序集 fail closed 并要求
  重启, 不宣称热替换; (e) Shutdown/Terminal 撤 patch 前发布关闭状态, async callback/await 后续以绑定
  代数核对; 初始化自身 Harmony 安装失败必须回滚本 owner 且不发布 Bound; (f) 正常非 Forms 对局原生语义
  不能被生命周期门禁误吞.
- 已修复: 按 (a)-(f) 实施; 文档与代码均不宣称真正热替换成功.

### C-01 [P3] 契约身份与公开签名未变
- 10 个 `SPIRE1-*` CustomID 全部保留 (逐条 grep 确认).
- `Forms.FormsCode.Interop.FormsRuntimeEntryPoint` 公开签名未改 (IsAvailable/IsSelected/KindOf/
  CurrentKind/Enter/Exit); SHA256=`244F13EE0E131BF818D7ACA303D0B51DB3814913AC90E887DA89E935BCDFA2EC`.
- 六形态效果语义未重设计; 无 smoke runner/stub 进入生产.

## 进行中

- 本批源码修改已完成并落盘 (见 CODE_COMPLETE). 未构建; 等主会话集中构建验收.

## 未知

- 独立 Forms.dll 被 ModelDb 发现/实例化的真实时序.
- Watcher 晚加载后 AssemblyLoad 到主线程绑定的真实路径.
- 独立 carrier 与 Spire1 原 IOnStanceChanged 消费者的兼容完整性.
- 旧档/联机身份在独立程序集下的真实解析.
- 新 Forms PCK 视觉资源在实际 UI 中显示.
- 真正程序集替换 / 热卸载是否可行: 未证明, 本批只做 fail closed + 要求重启.
- 以上均未实机验证; 本批不构建, 由主会话集中构建验收.

## CODE_COMPLETE

本轮唯一产品写集内已完成的修改 (未构建; 由主会话集中构建验收):

| 文件 (绝对路径) | 修改 | SHA256 |
| --- | --- | --- |
| `G:\omp works\Sts\sts2-forms\.gitignore` | 新增仓根忽略 | 344B001C65C221CB5B46633D3746A28F53ABF6EAE38E5D67475DD4A9A4E7FDC6 |
| `G:\omp works\Sts\sts2-forms\mod\Forms.csproj` | 删除 CopyToModsFolderOnBuild | 1396ACED456BBAF941E312040304427C056A827A9B4577BD6FB980EBA66F4698 |
| `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs` | Logger 限定 + patch 全成功门禁 + Shutdown | E6C7291E6379F4B97EA6D57B9B559CA62BE34A2BF9B493C86C80E5233A742CED |
| `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs` | W-03..W-06/W-08 生命周期 | 5F11A32D089EA03DFBA72008C5867783678C336B56FC2A44F6F53A4CA177A38B |
| `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs` | IsSelectedAndBound | 3C3445164E7BF3146118BB82FCF772EFAD35AB6992DF07E9C4F50649D0B5B918 |
| `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceBridgePump.cs` | 新增有界主线程 pump | B7075F4A5966A70E75ACB2D8F9DB002E9146D43E1020520A64BB4DE622ADB78A |
| `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMainMenuRetryPatch.cs` | ready 后启动/保持 pump | A922E636116604D7B982BA363BF7071EA2C35D58C4340B2B2A82057DBBD97E31 |
| `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModePatch.cs` | Init postfix 启动 pump | BC52C399C2B390B073E96EB0D384E666D8D587A431A38DD862C61A87C3D51173 |
| `G:\omp works\Sts\sts2-forms\DEVELOP.md` | 生命周期/部署/替换保证 | B876876886040D34DB4649D35456199A2B657D46CE27603D0A9B73FD4C05919D |
| `G:\omp works\Sts\sts2-forms\DEVLOG.md` | round3 记录 | E91A6B253A07310322F2C8CC7B223B8BBAD9D833E4C40C8CB6AEDCE8C1E8494C |

未改动 (刻意保留): `FormsRuntimeEntryPoint.cs` 公开签名; 10 个 `SPIRE1-*` CustomID; 六形态效果语义;
无 smoke runner/stub 进入生产.

静态自检仅做了括号平衡与符号唯一性核对 (无构建). 未验证: 全部编译, AssemblyRef/TypeDef/PCK 门禁,
实机启动, 旧档/联机, 真实程序集替换/热卸载, UI 视觉. 这些仍由主会话集中验收, 本报告不宣称通过.

待主会话核对的精确接口:
`FormStanceWatcherBridge.TryBind()` / `TryBindOnMainThreadEntry()` / `PumpTick()` / `EnsurePumpForRetry()` /
`Shutdown()` / `MarkOwnerPatchesFailed(string)`; `FormStanceMode.IsSelectedAndBound(Player?)`;
`FormStanceBridgePump` (Node, `_Process(double)`); `MainFile.PatchesHealthy` / `MainFile.Shutdown()`.
