# 姿态形态部分 Mod 交叉启动矩阵 r29

检查时间: 2026-10-03 06:38-06:40 +08:00

## 已确认

### 1. 运行边界

- 脚本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r29-20261003.ps1`
- 结果目录: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003`
- 游戏副本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-r21-20261002\game`
- 这是非 Steam 隔离副本: `steam_api64.dll` 与 `steam_appid.txt` 均不存在.
- `matrix-summary.json` 的 `steamSafe=true`, `sharedConfigSha256Unchanged=true`.
- 所有场景 `nestedManifestCount=0`;运行后测试副本 mods 与 steam settings 均由脚本清理/恢复.
- r29 只修正了 r28 测试脚本在进程退出与窗口句柄检查之间的竞态;没有修改产品源码.

### 2. 九个场景的真实启动结果

以下结果均来自各场景的 `run.json`、`stdout.log`、`stderr.log`,不是源码推理:

| 场景 | 挂载 | 结果 | 关键事实 |
| --- | --- | --- | --- |
| m1 | BaseLib + Spire1 | 通过 | `Loaded 2 mods (2 total)`; Spire1 initializer 成功; Watcher 缺失时 Forms bridge 明确 disabled;无 `FileNotFoundException`/`TypeLoadException`. |
| m2 | BaseLib + Watcher | 通过 | `Loaded 2 mods (2 total)`; BaseLib 与 Watcher initializer 成功;无 Spire1. |
| m3 | BaseLib | 通过 | `Loaded 1 mods (1 total)`;仅 BaseLib initializer 成功. |
| m4 | Spire1 | 通过 | `Loaded 0 mods (1 total)`;未调用 Spire1 initializer;缺少 BaseLib 的前置项由 ModLoader 拒绝. |
| m5 | BaseLib + Watcher + Spire1 | 通过 | 三个 initializer 成功;Forms Watcher bridge 绑定;`Targets` 列表完整. |
| m6 | BaseLib + AutoAnthony + Spire1 | 通过 | `Loaded 3 mods (3 total)`;Spire1 AutoAnthony core 为 `core=True, third-party=Pending`;没有 Watcher 硬前置;没有 disposed timer 错误. |
| m7 | BaseLib + AutoAnthony + Watcher + Spire1 | 通过 | Legacy bridge 绑定为 `third-party=LegacyBridge`;Forms Watcher bridge 绑定;没有 disposed timer 错误. |
| m8 | BaseLib + AutoAnthony + Watcher + AutoAnthonyWatcher + Spire1 | 通过 | 官方扩展接管为 `third-party=OfficialAddon, settled=True`;日志明确 legacy bridge disabled;没有 disposed timer 错误. |
| m9 | BaseLib + Watcher + AutoAnthonyWatcher + Spire1 | 通过 | `AutoAnthonyWatcher` 因缺少 `AutoAnthony` 被 ModLoader 拒绝;其余 BaseLib/Watcher/Spire1 仍加载;Spire1 initializer 成功. |

九个场景的 `exitCode=0`, `timedOut=false`, `nonzeroWindowHandleObserved=false`, `logDrainCompleted=true`,且各场景共享配置前后哈希不变.

### 3. 本轮重点回归结论

- r27 中 m6、m9 曾出现 `Cannot access a disposed object` 的 timer 竞态. r29 的 m6、m9 `stdout.log`/`stderr.log` 均没有 `disposed`、`ObjectDisposedException` 或同类 AutoAnthony timer 错误.
- m7 仍能安装 legacy Watcher bridge; m8 仍能由 `AutoAnthonyWatcher` 接管; m9 仍能在核心 AutoAnthony 缺失时由 ModLoader fail-closed,而不是让 Spire1 产生硬前置失败.
- m1 在没有 Watcher 时只报告 Forms bridge disabled,Spire1 主 Mod 仍完成 initializer;这证明 Watcher 是可选运行时能力而不是 Spire1 的硬 AssemblyRef.
- m6 的 `AutoAnthony` 自身仍记录 `Expected 65 complete v111 Colorless cards, found 76`,该错误来自 AutoAnthony 启动自检且日志明确不阻塞游戏启动;它不是 Spire1 的硬引用或本轮 timer 修复失败.
- m9 的依赖错误是预期的 ModLoader 拒绝:`Tried to load mod AutoAnthonyWatcher, but it depends on mods which have not been loaded: AutoAnthony!`;它没有传播为 Spire1 初始化失败.

### 4. 与发布二进制门禁的交叉证据

中央 Release 构建日志:
`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-build-autoretimer-r1-20261002.log`

- `0 errors`, `63 warnings`.
- DLL: `CE90BB1DBC6E3C15A061E625F3463B5F38AF49246FAFD06C06B8E52ECCA06980`.
- PCK: `CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E`.
- PDB: `52E678EB3546EAB612615AB0A5B2A28572FA17CC0113FD1B00F520348A998278`.

门禁 JSON:
`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-autoretimer-r1-20261002.json`

- `assemblyref-forbidden=PASS`.
- `manifest-consistency=PASS`;二进制 mod AssemblyRef 只有 `BaseLib`,manifest 只声明 `BaseLib`.
- `typedef-forbidden=PASS`.
- 没有 `Watcher`、`AutoAnthony`、`AutoAnthonyWatcher`、`DirectConnectIP`、`ActsFromThePast` 硬引用.

## 进行中

- 当前只完成 ModLoader 交叉挂载与可选桥接的启动证据. Forms 卡牌出牌闭环由 r23 单独覆盖;本报告不重复宣称完整战斗验收.
- 已启动一个 Codex 原生只读独立审查代理,报告路径为:
  `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-cross-launch-final-review-20261003.md`
  该报告的最终状态仍等待原生 `wait_agent` 完成返回;现有增量内容只能作为源码初步证据.

## 未知

- 未验证可见 UI、视觉资源、完整长战斗、存档重载、重连、多人同步、性能和数值平衡.
- 未证明 AutoAnthony 自身 `65 vs 76` 资源版本错误;本轮只确认它不阻塞这九个隔离启动场景.
- 未把 headless 的 Sentry crashpad 缺失与 Godot Dummy renderer/RID 泄漏当作业务通过;这些是已知测试运行时噪声.
- 未写入 Steam 安装 `G:\steam\steamapps\common\Slay the Spire 2\`,未修改共享 `G:\appdata\C-Users-o_Obl\Roaming\SlayTheSpire2\mod_configs`.

## 证据索引

- 总结: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r29-20261003\matrix-summary.json`
- 场景运行报告: `...\m1-baselib-spire1-no-watcher\run.json` 至 `...\m9-baselib-spire1-addon-missing-core\run.json`
- 源码: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`、`AutoAnthonyLoadHook.cs`
- 交叉启动脚本的 m4 竞态修正仅属于测试夹具,不是产品行为修复.
