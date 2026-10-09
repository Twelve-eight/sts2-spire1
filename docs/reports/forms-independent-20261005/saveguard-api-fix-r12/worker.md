# SaveGuard API 窄修报告 (r12)

## 已确认
- 中央 r9 编译日志 `G:\omp works\.tmp\forms-independent-20261005\saveguard-smoke-build-r9.log` 第 140 行报错:
  - CS0029: 无法将 `System.Threading.Tasks.Task` 隐式转换为 `bool`
  - CS1662: lambda 无法转换为预期委托类型
- 出错表达式为 `() => game.GameStartupComplete`, 传给 `WaitWithTimeoutAsync(Func<bool>...)`.
- 本机权威证据确认 `GameStartupComplete` 是 `Task` 而非 `bool`:
  - 发行版文档 XML `G:\omp works\Sts\_runtime\sts2-test-client-B\data_sts2_windows_x86_64\sts2.xml:21474` 存在成员 `P:MegaCrit.Sts2.Core.Nodes.NGame.GameStartupComplete`.
  - decompile `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:211714`: `public Task GameStartupComplete => _gameStartupComplete.Task;`
  - 引擎源码 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:420,481`: `TaskCompletionSource _gameStartupComplete` 与 `public Task GameStartupComplete => _gameStartupComplete.Task;`
- 语义边界 (已确认): 该 Task 在 `GameStartup()` 成功与失败分支都会 `TrySetResult()` (`NGame.cs:586,590`), 即完成不等于启动成功; 因此只 `await` 不检查状态也不足以证明启动成功. 但本窄修范围只要求: await 实际 Task 及超时, 不能仅 `IsCompleted` 把 fault/cancel 当成功; 并且启动状态等待不得不经 Godot 主线程直接访问 engine 节点.
- 修改前的文件 hash (SHA256): `C0AC16FB68A47E1323545AA2614B6AD189BC28D0DA66D437745DCAE8E82323ED`, 25124 bytes, LF-only, 无 BOM.

## 已完成的精确改动 (唯一代码文件)
- 文件: `G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs`
- 唯一逻辑改动: 把 `WaitWithTimeoutAsync` 的签名从 `Func<bool> condition` 改为 `Func<Task> operationFactory`, 并在实现内:
  - 先在 Godot 主线程经既有 `InvokeOnMainThreadAsync(operationFactory)` 取得真实 `Task` (因此不绕过主线程直接访问 engine 节点);
  - 用既有 timeout 语义 `Task.WhenAny(startup, Task.Delay(timeout))` 加超时;
  - 超时抛既有 `TimeoutException` 文案; 否则 `await startup`, 让 fault/cancel 真实抛出而非被 `IsCompleted` 吞掉.
- 调用点 `() => game.GameStartupComplete` 保持不变 (现在 lambda 类型为 `Func<Task>`, 与真实 API 匹配).
- 删除旧的 `Func<bool>` 轮询循环 (其中 `InvokeOnMainThreadAsync(condition)` + `Task.Delay(50)` 轮询), 未改动其它 helper.
- 未改动: 四项 engine `FromSerializable` 检查 (patch owner / FormStance identity / vanilla roundtrip / unknown DeprecatedModifier)、`finalPassed`/`exitCode`/`status` 语义、`TryStart` 闩锁、失败报告路径、主线程检查封装、`WriteJson` 边界. 未做任何产品代码修改.
- 修改后文件 hash (SHA256): `D8DC47BCDE80650352E681661A73C86B0F213553CB778CCE9F5C21E9B2EEBD82`, 25147 bytes, UTF-8 无 BOM, LF-only.
- 静态自检 (非构建): `Func<bool>` 剩余 0 处; `Func<Task>` 1 处; 调用点 `() => game.GameStartupComplete` 保留 1 处; 代码中不再有 `IsCompleted` 判断 (仅注释文字提及).

## 进行中
- 无. 本窄修已完成.

## 未知 / 未验证边界
- 按派发约束, 本轮未构建/lint/测试/运行游戏/部署/git, 因此该改动未做编译验证; 编译由中央 r10 进行.
- `GameStartupComplete` 在启动失败时仍会 `TrySetResult` 而不是 fault, 所以本 Task 本身不会以异常表达启动失败; 现有 carrier 由 `MainFile.PatchesHealthy` 与后续 engine 检查兜底. 若需要"启动成功而非仅完成"的额外判定 (例如 `RootSceneContainer.CurrentScene is NMainMenu`), 超出本次"窄修 API"范围, 未添加.
- 未验证真实进程中的 startup task 超时/fault 路径行为 (需运行游戏, 本轮禁止).

CODE_COMPLETE