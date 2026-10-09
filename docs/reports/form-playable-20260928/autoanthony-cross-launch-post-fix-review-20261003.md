# AutoAnthony 交叉启动修复后只读复核 - 2026-10-03

范围: `G:\omp works\Sts\sts2-spire1` 的 AutoAnthony 可选桥接与部分 Mod 交叉启动生命周期。
本轮只读;未构建、未测试、未部署、未启动游戏、未修改共享配置、未调用其它代理运行时、未再委派。
模型/路由: 请求指定 `global:deepseek-v4.1-flash` / `wb2api via local gateway`; 本会话未暴露可核验的解析元数据, 故不作已验证声明。

## 已确认

### [P1] 字节身份链: r30 实机覆盖旧字节, 当前 beta 包内 DLL 是新字节且 manifest 只声明 BaseLib
- 绝对路径与准确行号:
  - r30 部署记录 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\m1-baselib-spire1-no-watcher\staging.json`: Spire1.dll `sha256=DD935F68241F0060D1DDED62DE72E0D1B92CA0D1A32F4D656A745E3D58E13F8B`, `length=776704`; `matrix-summary.json` 的 `started=2026-10-03T06:59:06+08:00`。
  - 当前 Release DLL `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`: `sha256=51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`, `length=781312`, `LastWriteTime=2026-10-03 07:55:18`。
  - 当前 beta ZIP `G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003.zip` (`LastWriteTime=2026-10-03 07:59:45`) 内 `mods/Spire1/Spire1.dll` 与当前 Release 同字节: `781312` / `51224C20...`; `Spire1.pck`: `28866294` / `CF37054F...`; `Spire1.pdb`: `237176` / `731AE9E9...`。
  - 同 ZIP 内 `mods/Spire1/Spire1.json`: `548` / `CDBD57D5...`; 内容 `version=1.2.3`, `min_game_version=0.111.0`, `dependencies=[BaseLib >= 3.4.5]`, `has_dll=true`, `has_pck=true`, `affects_gameplay=true`。
  - 包内 `README-安装说明.txt` 声明 Watcher/AutoAnthony/AutoAnthonyWatcher 均为可选, 且只要求 BaseLib 3.4.5+。
  - 修复源码 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:38,100-129,501-580` 的 `LastWriteTime=2026-10-03 06:56:13`, 早于 r30; 但 r30 部署字节仍是 776704 旧版本。
- 触发条件: 把 r30 的九场景启动结果、或 `docs\reports\form-playable-20260928\release-gates-current-20261003.md:6-16` 引用的 05:32 门禁 JSON, 当作当前 07:55:18 beta DLL 的实机/结构证据。
- 权威契约: 请求要求 “当前 beta Release DLL 和 manifest 的结构结论不得用旧 DLL 证据替代; 如路径报告是旧字节, 明确标记”。
- 当前控制流: r30 的 `dllSource` 只记录复制来源路径, 实际启动字节由 `artifacts[].sha256` 决定, 即 776704 旧字节; 当前 Release 与 beta ZIP 均为 781312 新字节, 二者相差 4608 字节且哈希不同。manifest 侧静态结论明确: 无 Watcher/AutoAnthony/AutoAnthonyWatcher 硬前置。
- 可复现命令:
  `Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll' -Algorithm SHA256`
  `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\m1-baselib-spire1-no-watcher\staging.json' -Raw`
  `Add-Type -AssemblyName System.IO.Compression.FileSystem; $z=[IO.Compression.ZipFile]::OpenRead('G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003.zip'); $z.Entries | Select-Object FullName,Length`
- 最小修复范围: 在 beta 发布记录中明确 “r30 = 旧字节 `DD935F68...`; beta ZIP = 新字节 `51224C20...`”; 若要正式声明, 对新字节重跑 release gates 与至少 m1/m6/m7/m8/m9 交叉启动。
- 尚缺的实机证据: 对 `51224C20...` 字节的启动/退出/可选桥接矩阵; r30 只覆盖 `DD935F68...`。

### [P1] 修复后的 shutdown gate 已闭合 ProcessExit 与订阅之间的 TOCTOU; 但仍存在一条可静态指出的复位/重建路径需要标注
- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:38` 定义 `AssemblyLoadGate`。
  - `:100-129` `OnProcessExit` 先设 `_shutdownRequested=true`, 停周期源, 清 deferred 状态, 再在 `AssemblyLoadGate` 内退订并置 `_hooked=false`。
  - `:501-550` `HookAssemblyLoad` 入口 `:503-506` 与 gate 内 `:514-517` 二次 `ShouldStopNotifications()`; 订阅成功后 `:523` 才置 `_hooked=true`。
  - `:552-580` `UnhookAssemblyLoad` 与订阅共用 `AssemblyLoadGate`。
  - `:149-154` `ShouldStopNotifications` 在 `_retrySourcesStopped || _shutdownRequested` 时返回 true。
  - `:758-791` `StopRetryTimer(permanent:true)` 在 `RetryGate` 内设 `_retrySourcesStopped=true`, 并把 `_harmony=null`; `_retrySourcesStopped` 没有任何复位点。
- 触发条件: 进入 `ExecuteApplyCore` 的 unsettled Apply 在 ProcessExit 之后返回; 旧控制流会再调用 `HookAssemblyLoad`, 新 gate 会因 `_shutdownRequested` 拒绝订阅。
- 权威契约: 请求要求 “shutdown gate 必须阻止 ProcessExit 与 in-flight Apply 重新订阅 AssemblyLoad”。
- 当前控制流: `OnProcessExit` 先 `_shutdownRequested=true`; `HookAssemblyLoad` 在 gate 内二次检查, 因此即使 ProcessExit 与订阅竞争, 也最多二选一; 若 ProcessExit 先进入 gate, 订阅被拒绝; 若订阅先进入 gate, ProcessExit 随后进入 gate 并退订。静态上该 TOCTOU 已闭合。
- 可复现命令:
  `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs' -Pattern 'AssemblyLoadGate|_shutdownRequested|_retrySourcesStopped|StopRetryTimer'`
- 最小修复范围: 无必须修改; 建议在报告/DEVLOG 明确 `_retrySourcesStopped` 是进程生命周期内单向锁存, 不存在退出后重新订阅的路径。
- 尚缺的实机证据: 没有可控的 ProcessExit 与 in-flight Apply 并发注入/teardown 压力测试; 该结论是源码级, 不是新实机通过。

### [P1] Timer 与 fallback thread 只提交主线程 deferred Apply, 静态上满足契约
- 绝对路径与准确行号:
  - `:13-17` 类注释声明 AssemblyLoad/Timer 仅为通知源, 全部经 `ApplyGate/_applyInProgress` 串行化并在 Godot 主线程执行。
  - `:677-681` Timer 回调 `OnRetryTimer`。
  - `:715-750` fallback thread 循环只调用 `OnRetryTimer`。
  - `:793-818` `OnRetryTimer` 只做 `WatchDeferredApply` 与 `RequestApply`。
  - `:191-205` `RequestApply`: 主线程直接 `ApplyOnMainThread`; 非主线程走 `QueueDeferredApply`。
  - `:214-239` `QueueDeferredApply` 仅做 `Callable.From(...).CallDeferred()`。
  - `:273-310` `ExecuteDeferredApply` 做 generation/queued 校验后调用 `ApplyOnMainThread`。
  - `:318-333` `ApplyOnMainThread` 有主线程 fail-closed 检查; `:413-425` `ExecuteApplyCore` 在桥边界再次检查。
  - `:582-612` `OnAssemblyLoad` 也只 `RequestApply`, 不直接调 Harmony。
- 触发条件: AutoAnthony/Watcher 晚加载, 或 deferred callback 被取消时。
- 权威契约: 请求要求 “AssemblyLoad,Timer,fallback thread 只能提交主线程 deferred Apply”。
- 当前控制流: 三条通知路径均不直接改 Harmony/桥状态; 非主线程一律 deferred, 非主线程 Apply 一律 fail-closed 并保留重试源。
- 可复现命令:
  `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs' -Pattern 'OnRetryTimer|RequestApply|QueueDeferredApply|CallDeferred|IsMainThread'`
- 最小修复范围: 无。
- 尚缺的实机证据: 没有线程注入测试证明 Timer/fallback 线程从不进入桥边界; 只有源码路径闭合证据与 r30 无 disposed 错误的运行日志。

### [P1] Watcher/AutoAnthony/AutoAnthonyWatcher 缺失时 Spire1 初始化不形成硬前置; 但 r30 实测字节非当前 DLL
- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:10-15` 运行时依赖只有 `BaseLib >= 3.4.5`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs:163-167` 在 Phase3 无条件调用 `AutoAnthonyLoadHook.TryApplyBridge(harmony)`; 失败只影响可选桥。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:223-228` AutoAnthony 缺席时记录 absent 并返回 false。
  - `:285-328` `NeedsRetryWithoutAssemblyLoad`: AutoAnthony 缺席直接 false; 可选程序集都缺席时不创建周期源。
  - `:479-607` 第三方 Watcher capability 全程反射解析, 缺失时 fail-closed/pending。
  - r30 `m1` 记录 `Spire1 initializer=true`, `Watcher initializer=false`, `Loaded 2 mods`; `m6` 记录 `core=True, third-party=Pending`; `m9` 记录缺 AutoAnthony 的 addon 被 loader 拒绝而 Spire1 仍初始化。
- 触发条件: 只装 BaseLib+Spire1, 或 AutoAnthony 在场但 Watcher/AutoAnthonyWatcher 缺席。
- 权威契约: “Watcher,AutoAnthony,AutoAnthonyWatcher 缺失时 Spire1 仍可初始化, 不形成 manifest 或 AssemblyRef 硬前置”。
- 当前控制流: manifest 只有 BaseLib; 可选桥的解析/补丁都在 try/catch 内, 缺席返回 false, 不向 initializer 抛异常。静态结论成立。
- 可复现命令:
  `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json'`
  `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\m1-baselib-spire1-no-watcher\run.json' -Raw`
- 最小修复范围: 无; 但必须重新绑定当前 DLL 字节后才能把 r30 的启动结论用于当前 beta。
- 尚缺的实机证据: 当前 `51224C20...` DLL 的 m1/m6/m9 启动证据; AssemblyRef 硬前置需要对该字节重跑 release gate。

### [P1] legacy Watcher bridge 与官方 AutoAnthonyWatcher takeover 静态互斥, 且 r30 m7/m8 日志给出旧字节实测边界
- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:510-519` `PatchThirdPartyEntriesCore` 在检测到 `AutoAnthonyWatcher` 程序集时无条件走 `SetOfficialWatcherCapability`, 即官方 addon 优先。
  - `:609-635` `SetOfficialWatcherCapability` 先回滚 `ThirdPartyPatchedMethods` 与 `ThirdPartyPartialPatchedMethods`; 只有全部移除成功才清空列表并置 `OfficialAddon`; 失败则清空映射、置 `OfficialAddonPending` 并保留重试列表。
  - `:671-744` legacy 三个补丁体 (`ThirdPartyPoolPrefix`/`ThirdPartyPoolContentsPrefix`/`ThirdPartyPoolIdsPostfix`) 均在入口检查 `_thirdPartyCapabilityState != LegacyBridge` 时直接放行, 因此即使 Unpatch 失败, 旧回调也 fail-closed, 不会与官方 addon 重复安装。
  - `:536-541` `LegacyBridge` 状态下 `PatchThirdPartyEntriesCore` 返回 false, 保持 AssemblyLoad/周期重试存活, 以便晚到的官方 addon 触发确定性 unpatch。
  - r30 `m8` 日志 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\m8-baselib-spire1-official-addon\appdata\SlayTheSpire2\logs\godot.log:340-341`: `AutoAnthonyWatcher addon present - Watcher handed over to the official extension API, legacy bridge disabled.` 与 `core=True, third-party=OfficialAddon, settled=True.`
  - r30 `m7` 日志 `...\m7-baselib-spire1-autoanthony-watcher\appdata\SlayTheSpire2\logs\godot.log:332-333`: `third-party=LegacyBridge, settled=False`。
- 触发条件: 先装 Watcher 建立 legacy bridge, 随后晚加载 AutoAnthonyWatcher; 或两者同时在场。
- 权威契约: 请求要求 “legacy Watcher bridge 与官方 AutoAnthonyWatcher takeover 不互相重复安装”。
- 当前控制流: 官方 addon 存在时优先接管; legacy 补丁被回滚, 且即使回滚失败, 补丁体也因状态机变为 `OfficialAddonPending` 而全部放行, 不产生双份安装。
- 可复现命令:
  `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs' -Pattern 'SetOfficialWatcherCapability|OfficialAddonPending|LegacyBridge'`
  `Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\m8-baselib-spire1-official-addon\appdata\SlayTheSpire2\logs\godot.log' -Pattern 'OfficialAddon|legacy bridge disabled'`
- 最小修复范围: 无静态缺口; 但 r30 的 m7/m8 证据绑定的是 `DD935F68...` 旧字节, 当前 `51224C20...` 需要重跑才能外推。
- 尚缺的实机证据: 当前 beta DLL 的 m7/m8 takeover 运行日志; 以及 “legacy 先装、官方 addon 极晚加载” 的真实竞态注入。

### [P1] 当前 beta ZIP 附带 Spire1.pdb, 内含本机绝对路径 `G:\omp works\...`
- 绝对路径与准确行号:
  - ZIP `G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003.zip` 内存在 `mods/Spire1/Spire1.pdb`, `Length=237176`。
  - 包内 `README-安装说明.txt` 的 `Contents` 段也把 `mods/Spire1/Spire1.pdb` 列为交付内容。
  - 本轮对 ZIP 内该 PDB 做只读字符串扫描: 发现 1 条不同绝对路径形态, 含 `G:\omp works\Sts\sts2-spire1\...` 与 GitHub 源链接前缀 `https://raw.githubusercontent.com/Twelve-eight/sts2-spire1/f8be5c26...`。
- 触发条件: 将 beta 包分发给朋友或上传工坊/公开渠道。
- 权威契约: 发布包不应携带开发机绝对路径; 早前 beta 结构审查的最小安全包明确 “不应包含 `Spire1.pdb`”。
- 当前控制流: ZIP 由当前 Release 目录 + README 打包, Release 目录含 `Spire1.pdb`, 打包脚本未把它排除。
- 可复现命令:
  `Add-Type -AssemblyName System.IO.Compression.FileSystem; $z=[IO.Compression.ZipFile]::OpenRead('G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003.zip'); $z.Entries | Where-Object { $_.FullName -like '*.pdb' } | Select-Object FullName,Length`
  `$n=$z.GetEntry('mods/Spire1/Spire1.pdb'); [Text.Encoding]::ASCII.GetString(...)` (只读扫描)
- 最小修复范围: 从 beta ZIP 与 README 的 Contents 中移除 `Spire1.pdb`, 或确认可接受后显式记录 PDB 会暴露本机路径; 若保留, 不应作为公开/朋友包。
- 尚缺的实机证据: 无; 这是包内字节的静态字符串结论, 与游戏运行无关。

### [P1] 当前 Release DLL 的 release gates 实际已在 07:58:51 对 07:55:18 字节重跑并 PASS; 但该 JSON 未记录 DLL SHA256
- 绝对路径与准确行号:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\beta-audit-gate-current-20261003.json`: `LastWriteTime=2026-10-03 07:58:51`, `passed=true`; 三项 `assemblyref-forbidden` / `manifest-consistency` / `typedef-forbidden` 均 `Passed=true`。
  - 该 JSON 的 `dll` 字段为 `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`, 与当前 07:55:18 DLL 同路径; 时间晚于 DLL, 因此对当前字节有效。
  - `docs\reports\form-playable-20260928\release-gates-current-20261003.md:6-16` 引用的是 05:32:45 的旧 JSON, 该旧 JSON 未覆盖 07:55:18 字节。
  - `docs\reports\form-playable-20260928\beta-package-audit-20261003.md:32-42` 记录的 07:58:51 门禁运行正是这一组 JSON。
  - 局限: 该 JSON 只记录路径, 不记录 DLL SHA256/长度; 因此 “PASS 对应 51224C20” 依赖文件时间顺序与同路径推断, 不是字节级绑定。
- 触发条件: 将 07:58:51 JSON 作为当前 beta 的权威结构门禁证据。
- 权威契约: 门禁报告应绑定被检查产物的字节身份; 请求明确禁止用旧 DLL 证据替代当前结论。
- 当前控制流: 当前 `51224C20...` DLL 的 AssemblyRef/TypeDef/manifest 结构已有一次路径级 PASS; 但 JSON 未自证 SHA256。
- 可复现命令:
  `Get-Item -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\beta-audit-gate-current-20261003.json','G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll' | Select-Object FullName,Length,LastWriteTime`
  `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\beta-audit-gate-current-20261003.json' -Raw`
- 最小修复范围: 在门禁 JSON/报告中记录 `dllSha256` 与 `dllLength`; 对 ZIP 内 `51224C20...` 字节重跑一次并写 SHA256。
- 尚缺的实机证据: 无新增运行证据; 这是静态结构证据的身份绑定问题。

### [P1] 退出竞态修复与 beta 包均未提交/未推送, 当前 git HEAD 不含 AssemblyLoadGate
- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1` 工作区 `git status --short` 显示 `MM mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs`、`MM mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs`、`M mod/Spire1.json`、`?? dist/Spire1-Forms-Beta-20261003.zip`。
  - `git log -1` 的 HEAD 为 `f8be5c2 2026-10-03 04:06:01 +0800 Close native race probe cancel-first ordering`。
  - `git show HEAD:mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs` 中只有 `_hooked` 的简单置位/清除 (HEAD 版本第 18,43,47,63 行), 没有 `AssemblyLoadGate`, 也没有 `_shutdownRequested` 或 `StopRetryTimer`。
- 触发条件: 从 git HEAD 或远端重建, 或认为仓库状态已包含本轮修复。
- 权威契约: 工作区规范要求每个代码变更提交并推送, 验收为干净 `git status` 与无未推送提交。
- 当前控制流: 修复后的源码与 beta ZIP 只存在于工作区/未跟踪文件; HEAD 仍是修复前版本。
- 可复现命令:
  `Set-Location -LiteralPath 'G:\omp works\Sts\sts2-spire1'; git status --short; git log -1 --pretty='%h %ad %s'; git show HEAD:mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs | Select-String -Pattern 'AssemblyLoadGate|_shutdownRequested|StopRetryTimer'`
- 最小修复范围: 将修复源码、manifest、beta ZIP 与相关报告提交并推送; 提交前确认不夹带未预期的路径清理或临时产物。
- 尚缺的实机证据: 无; 这是仓库状态静态结论。

## 进行中

- 静态结论已覆盖: 字节身份链与 manifest 结构、主线程 deferred 边界、ProcessExit/AssemblyLoad gate、可选程序集缺失、legacy/official takeover、beta ZIP 结构与 PDB、当前字节门禁、git 提交状态。
- 正在收尾: 确认报告三段结构与条目数量, 不再新增检查面。

## 未知

- 当前 beta ZIP (`51224C20...`) 的 m1/m6/m7/m8/m9 实机交叉启动矩阵; r30 只覆盖旧字节 `DD935F68...`。
- 当前 DLL 的 teardown 压力与 ProcessExit/in-flight Apply 并发注入测试。
- 本会话实际解析模型/provider route 的可核验元数据。