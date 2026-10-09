
# 部分模组交叉启动中央矩阵 - r26 - 2026-10-03

## 范围

本报告使用最新 Release DLL 重新执行部分 mod 组合的真实 headless loader 启动。它只验证 manifest 依赖、顶层 staging、initializer 和可选 bridge 的启动边界,不验证 UI、存档、多人或形态战斗。

运行脚本:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r26-20261003.ps1`

汇总:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r26-20261003\matrix-summary.json`

测试游戏目录:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-r21-20261002\game`

## 五个场景

| 场景 | 选择的 mod | loader / initializer 结果 | 结论 |
|---|---|---|---|
| m1 | `BaseLib` + `Spire1` | `Loaded 2 mods (2 total)`; BaseLib 和 Spire1 initializer=true; Watcher=false | Watcher 不是 Spire1 启动硬前置; bridge 缺失时按预期 disabled/fail-closed |
| m2 | `BaseLib` + `Watcher` | `Loaded 2 mods (2 total)`; BaseLib 和 Watcher initializer=true; Spire1=false | Watcher 不依赖 Spire1 的 staging 或副作用 |
| m3 | `BaseLib` | `Loaded 1 mods (1 total)`; 仅 BaseLib initializer=true | BaseLib 不依赖 Spire1 或 Watcher |
| m4 | `Spire1` | `Loaded 0 mods (1 total)`; 三个 initializer=false; stderr 明确 `depends on mods which have not been loaded: BaseLib!` | 这是预期的已声明 BaseLib 缺失拒绝,不是意外前置项或半初始化 |
| m5 | `BaseLib` + `Watcher` + `Spire1` | `Loaded 3 mods (3 total)`; 三个 initializer=true; `Watcher bridge bound` | 完整集合顺序和桥接对照成立 |

每个 case 的 `run.json` 都记录 `exitCode=0`, `timedOut=false`, `nonzeroWindowHandleObserved=false`, `logDrainCompleted=true`, `sharedConfigSha256Unchanged=true`, `nestedManifestCount=0`。m4 的进程退出码 0 只表示游戏正常退出;其 mod 结果以 loader 的 `Loaded 0 mods` 和明确依赖错误判定,不能解读为 Spire1 成功加载。

## 安全与负向证据

- `matrix-summary.json` 的 `sharedConfigSha256Unchanged=true`、`steamSafe=true`。
- 运行结束后测试游戏 `mods` 目录已清理, Steam settings 已恢复, Steam marker `steam_api64.dll` 和 `steam_appid.txt` 均不存在。
- 各 case 的 staging 顶层只有选中的 mod 目录,所有 `nestedManifestCount=0`,没有嵌套 `BaseLib`/`Watcher` staging 残留。
- m1 stderr 的 `Watcher bridge disabled` 是 Spire1 在缺 Watcher 时的可选反射桥接拒绝,不是 `FileNotFoundException`、`TypeLoadException` 或 loader 硬引用。
- m5 stdout 出现 `Spire1 Forms: Watcher bridge bound`,证明 Watcher 存在时桥接进入已加载分支。

结构性 AssemblyRef/manifest/type 门禁另见:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json`
