# r17 Environment 类型歧义监督审查报告

## 审查范围

- 项目：`G:\omp works\Sts\sts2-spire1`
- 实现报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-envfix-worker-r17-20261001.md`
- 审查时点：2026-10-02
- 监督顺序：已先通过 Codex hub wait 等待实现代理 `01a0f8c3-35c2-7b33-8d91-49a56d304fab` 完成，再读取实现报告和最终源码。
- 本轮限制：只读审查；未修改产品代码，未构建、未测试、未部署、未启动游戏。

## 已确认

### 结论：PASS（仅限本轮静态审查）

1. 实现代理报告的修复范围与请求一致：
   - `FormNativeSmokeRunner.cs:49` 使用 `System.Environment.GetCommandLineArgs()`。
   - `FormNativeSmokeRunner.cs:1592` 使用 `System.Environment.GetEnvironmentVariable(ReportEnvironmentVariable)`。
   - 未修改 `using`、参数、控制流、Godot API、smoke 逻辑或其它产品文件。

2. 当前源码回读确认两处真实调用均已显式绑定 `System.Environment`；对目标文件检索未发现其它未限定的 `Environment` 调用。文件仍同时包含 `using System;` 与 `using Godot;`，因此显式限定正好消除了中央构建报告中的类型歧义，未引入额外别名或 API。

3. `FormNativeSmokePatch.cs` 仍保持原有的 `NGame._Ready` postfix 入口，仅调用 `FormNativeSmokeRunner.TryStart(__instance)`；本轮没有新增入口、旁路入口、Power 注入或伪造 Hook。

4. 对 `FormNativeSmokeRunner.cs` 的关键结构回读未发现 r17 触碰既有语义的静态迹象：
   - 主线程 gate 仍使用 `CallDeferred` 与本地 completion 机制。
   - `terminalOperations` 仍贯穿 startup、action/evidence、cleanup 及最终 drain。
   - `FormStanceMode.IsSelected(player)` 仍保留在形态状态证据路径。
   - JSON 写入仍经过 `WriteJsonIfConfigured`，普通启动仍在无 smoke 参数时提前返回。
   - 未发现 `Task.Run`、`PowerCmd.Apply`、`TestMode`、`--autoslay` 或 `NGame.Quit` 旁路被本轮引入。

5. 本轮未触及共享配置、Steam 安装或测试副本部署路径；实现代理报告也明确记录未执行这些操作。

## 进行中 / 后续必须由主会话完成

- 中央主会话必须重新执行 Release 构建，以确认两处 `Environment` 编译错误实际消失。
- 构建通过后，仍需按既定隔离流程执行测试副本部署与真实三场景战斗 smoke；静态审查不能证明形态状态、卡牌结算、JSON 时序或最终可玩性。

## 未知

- r17 修复后的中央构建结果未知。
- 真实运行、三场景形态状态、视觉资源、存档和其它运行边界均未验证。
- 当前两个 smoke 产品文件在工作区表现为未跟踪文件，无法以 `HEAD` 差异单独提供基线；本结论依据实现代理的增量报告与最终源码逐行静态回读，不把未执行的构建或实机运行写成证据。
