# 当前 Beta r2 部分 Mod 交叉启动矩阵 - r31 - 2026-10-03

## 结论

本轮使用 Beta r2 当前字节,而不是旧 r30 字节,对 `BaseLib`、`Watcher`、`Spire1`、`AutoAnthony`、`AutoAnthonyWatcher` 的 9 种挂载组合完成真实隔离 headless 启动。所有 case 退出码为 `0`,无超时、无窗口句柄、日志排空、无嵌套 manifest;共享 `mod_configs` before/after 相同,测试 mods 清理完成,Steam settings 恢复。

矩阵只证明启动、依赖拒绝和桥接状态边界,不证明形态战斗、可见 UI 或长时行为。形态战斗由 `form-native-smoke-r25-current-20261003.md` 单独证明。

## 已确认

### 1. 运行身份

- 运行目录: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r31-current-beta-20261003\`
- 启动脚本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r31-current-beta-20261003.ps1`
- 隔离游戏: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\`
- Spire1 来源: `G:\omp works\.tmp\Spire1-beta-20261003-r2\mods\Spire1\`
- m5 staging 明确记录:
  - Spire1.dll `781312` bytes, SHA256 `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`
  - Spire1.pck `28866294` bytes, SHA256 `CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E`
  - Spire1.json `548` bytes, SHA256 `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305`
  - staging 不含 `Spire1.pdb`.

### 2. 总体门禁

权威汇总:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r31-current-beta-20261003\matrix-summary.json`

- `sharedConfigSha256Unchanged=true`.
- `steamSafe=true`.
- 9/9 case `exitCode=0`.
- 9/9 `timedOut=false`.
- 9/9 `nonzeroWindowHandleObserved=false`.
- 9/9 `logDrainCompleted=true`.
- 9/9 `nestedManifestCount=0`.
- 运行后目标 `game\mods` 不存在。
- 运行后 `data_sts2_windows_x86_64\steam_settings` 恢复为原有顶层文件,无嵌套目录。
- 运行后没有残留 `SlayTheSpire2` 进程。

### 3. 基础组合和明确前置

| Case | 挂载 | 结果 | 关键证据 |
|---|---|---|---|
| m1 | `BaseLib + Spire1` | 通过 | `Loaded 2 mods`; Spire1 initializer 成功; Watcher bridge 明确 disabled,不抛硬依赖错误 |
| m2 | `BaseLib + Watcher` | 通过 | BaseLib/Watcher initializer 成功,无 Spire1 |
| m3 | `BaseLib` | 通过 | `Loaded 1 mods`,BaseLib initializer 成功 |
| m4 | `Spire1` | 按预期拒绝 | `Loaded 0 mods (1 total)`;未调用 Spire1 initializer;缺少 `BaseLib` 是唯一声明前置 |
| m5 | `BaseLib + Watcher + Spire1` | 通过 | `Loaded 3 mods`;三 initializer 成功;`Watcher bridge bound` |

### 4. 可选 AutoAnthony 组合

| Case | 挂载 | 结果 | 关键证据 |
|---|---|---|---|
| m6 | `BaseLib + AutoAnthony + Spire1` | 通过 | `Loaded 3 mods`;Spire1 initializer 成功;`core=True, third-party=Pending`;Watcher 缺失时 Forms bridge disabled |
| m7 | `BaseLib + AutoAnthony + Watcher + Spire1` | 通过 | `Loaded 4 mods`;`core=True, third-party=LegacyBridge`;Watcher bridge bound |
| m8 | `BaseLib + AutoAnthony + Watcher + AutoAnthonyWatcher + Spire1` | 通过 | `Loaded 5 mods`;日志明确官方 addon takeover,`legacy bridge disabled`;状态 `third-party=OfficialAddon, settled=True`;Watcher bridge 仍绑定 |
| m9 | `BaseLib + Watcher + AutoAnthonyWatcher + Spire1` | 按预期部分拒绝 | `AutoAnthonyWatcher` 因缺少 `AutoAnthony` 被 ModLoader 拒绝;其余 `BaseLib/Watcher/Spire1` 仍加载,`Loaded 3 mods (4 total)`;Spire1 initializer 和 Watcher bridge 成功 |

### 5. 依赖结论

- 当前 r2 二进制的三形态运行仍只声明 BaseLib 作为 manifest 前置。
- Watcher 缺失不会阻塞 Spire1 初始化;它只使 Forms bridge fail-closed/disabled。
- AutoAnthony 和 AutoAnthonyWatcher 缺失或不完整不会把它们变成 Spire1 硬前置。
- ModLoader 对 `AutoAnthonyWatcher` 自身缺少 `AutoAnthony` 的组合执行明确拒绝,但不连带拒绝 Spire1。
- m8 官方桥接接管时 legacy bridge 被关闭,未观察到双重安装状态。

## 进行中

- AutoAnthony 自身可能输出 `Expected 65 complete v111 Colorless cards, found 76` 的第三方内容版本自检错误;本轮确认它不阻塞进程退出,但未修复第三方内容。
- 需要朋友在可见 UI 中按实际 mod 列表验证安装顺序和可选项显示;headless 矩阵不替代该步骤。

## 未知

- 真实用户环境中的其它 Mod、加载顺序、不同配置和多人客户端差异。
- AutoAnthony 极晚加载与 ProcessExit/in-flight Apply 并发压力。
- 可见 UI、视觉资源、长战斗、存档重载、重连、多人同步、性能和平衡。

## 复核入口

```powershell
Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r31-current-beta-20261003\matrix-summary.json' -Raw
Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r31-current-beta-20261003\m8-baselib-spire1-official-addon\appdata\SlayTheSpire2\logs\godot.log' | Select-String -Pattern 'OfficialAddon|legacy bridge disabled|Watcher bridge bound'
Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r31-current-beta-20261003\m9-baselib-spire1-addon-missing-core\appdata\SlayTheSpire2\logs\godot.log' | Select-String -Pattern 'depends on mods which have not been loaded|Loaded 3 mods'
```