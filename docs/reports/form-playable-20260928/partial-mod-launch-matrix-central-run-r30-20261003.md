# 姿态形态部分 Mod 交叉启动矩阵 r30

检查范围: 退出竞态修复后的 Release DLL + 非 Steam 隔离副本
脚本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r30-20261003.ps1`
结果: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r30-20261003\matrix-summary.json`
游戏副本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-r21-20261002\game`

## 已确认

### Release 与结构门禁

- 中央构建日志: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-build-autoretimer-r3-20261003.log`。
- 结果: `0 errors`, `60 warnings`。警告为既有 nullable/async/duplicate using 等项目警告,本次没有新增编译错误。
- 新 DLL SHA256: `DD935F68241F0060D1DDED62DE72E0D1B92CA0D1A32F4D656A745E3D58E13F8B`。
- 新 PDB SHA256: `1D4B3D5F03CD3DFF2CDC36A0DD068B07B88AB831ECE95884B81A17986714A956`。
- PCK SHA256: `CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E`。
- 门禁 JSON: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-autoretimer-r3-20261003.json`。
- AssemblyRef 数量 15, TypeDef 数量 971; `assemblyref-forbidden`, `manifest-consistency`, `typedef-forbidden` 全部 PASS。
- 发布 DLL 的 mod AssemblyRef 只有 `BaseLib`; `Spire1.json` 只声明 `BaseLib`. 没有 `Watcher`, `AutoAnthony`, `AutoAnthonyWatcher`, `DirectConnectIP`, `ActsFromThePast` 硬引用。

### 退出竞态修复

独立审查报告 `autoanthony-cross-launch-final-review-20261003.md` 的 P2 结论指出: `ProcessExit` 与 in-flight unsettled Apply 之间,原控制流可能在退出标记后再次调用 `HookAssemblyLoad`。

主会话随后在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs` 做了最小并发边界修复:

- 第 38 行新增 `AssemblyLoadGate`。
- 第 112-123 行让 `OnProcessExit` 与事件卸载共享该 gate。
- 第 503-541 行让 `HookAssemblyLoad` 在入口和 gate 内二次检查 shutdown,并把失败后的 retry bookkeeping 移到 gate 外,避免锁反转。
- 第 554-578 行让 `UnhookAssemblyLoad` 与订阅共享同一 gate。

第一次 r2 构建曾因字段声明写入脚本失误产生 3 个 `CS0103`,立即修正后未部署、未进入验收;最终 r3 构建为 0 errors。

### r30 九场景真实隔离启动

`matrix-summary.json` 显示九个场景全部:

- `exitCode=0`
- `launchError=null`
- `timedOut=false`
- `nonzeroWindowHandleObserved=false`
- `logDrainCompleted=true`
- `sharedConfigSha256Unchanged=true`
- `nestedManifestCount=0`
- 总结 `steamSafe=true`

关键场景:

- m1 `BaseLib + Spire1`: Spire1 initializer 成功;AutoAnthony absent;Watcher 缺失时 Forms bridge disabled,不阻塞 Spire1。
- m5 `BaseLib + Watcher + Spire1`: BaseLib、Watcher、Spire1 均完成初始化,Watcher bridge 绑定。
- m6 `BaseLib + AutoAnthony + Spire1`: Spire1 initializer 成功,`core=True, third-party=Pending`;没有 Watcher 硬前置,没有 disposed timer/`ObjectDisposedException`。
- m7 `BaseLib + AutoAnthony + Watcher + Spire1`: legacy bridge 仍绑定,`third-party=LegacyBridge`;没有 disposed timer/`ObjectDisposedException`。
- m8 `BaseLib + AutoAnthony + Watcher + AutoAnthonyWatcher + Spire1`: 官方 addon 接管,`third-party=OfficialAddon, settled=True`,legacy bridge disabled;没有 disposed timer/`ObjectDisposedException`。
- m9 `BaseLib + Watcher + AutoAnthonyWatcher + Spire1`: ModLoader 明确拒绝缺少 AutoAnthony 的 addon,但 BaseLib/Watcher/Spire1 仍加载,Spire1 initializer 成功。
- m4 `Spire1` 单独挂载: `Loaded 0 mods (1 total)`,未调用 Spire1 initializer,缺少 BaseLib 的硬前置由 ModLoader 正确拒绝。

r30 未再出现 r27 的 `Cannot access a disposed object`。m6/m7/m8 仍可能记录 AutoAnthony 自身的 `Expected 65 complete v111 Colorless cards, found 76`,它是第三方资源版本自检问题,不属于 Spire1 硬引用；本轮不宣称 AutoAnthony chaos gameplay 已通过。

### 运行隔离

- 没有写入 `G:\steam\steamapps\common\Slay the Spire 2\`。
- 没有修改共享 `G:\appdata\C-Users-o_Obl\Roaming\SlayTheSpire2\mod_configs`。
- r30 使用隔离 APPDATA/LOCALAPPDATA/TEMP/GSE,结束后清理测试 mods/settings。

## 未知

- 没有执行可控的 Godot teardown 压力测试、deferred callback 取消测试或 Timer 构造失败注入;退出竞态修复由代码审查、Release 构建和真实 r30 启动回归覆盖,不是形式化并发证明。
- 没有覆盖可见 UI、视觉、长战斗、战中存档、重连、多人同步、性能和完整平衡。
- AutoAnthony 的 `65 vs 76` 内容版本不匹配仍待单独处理;它不影响本轮“无意外 Spire1 前置项”的结论。
