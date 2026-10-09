# binding-loss-hotfix-r14 worker 报告

## 已确认
- 已读取唯一请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-hotfix-r14\worker.request.md`.
- 已读取 `G:\omp works\AGENTS.md` 与同目录 `protocol.md`, 以及 r11 报告 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-repro-r11\worker.md`.
- 唯一代码白名单: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`; 唯一报告: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-hotfix-r14\worker.md`.
- 改动前 hash 与请求一致: SHA256 `A7BADDB66C49868B3005E0A9820ABE5BD8061D4A9D146709B2401ACD8D89A51B` (788 行).
- 未改主 runner / README / csproj / 产品 / patch; 未构建, 未 lint, 未测试, 未运行游戏, 未部署, 未 git; 未委派, 未换模型/路由/fallback.

### 窄修 1: AwaitProcessFrame 编译错误 (已确认并修复)
- r11 日志 `G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r11.log` 仅 23 行; 真实错误行为 `BindingLossSmokeRunner.cs(711,32): error CS1061 NGame 未包含 AwaitProcessFrame 的定义`.
- 权威 API 位置: `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Nodes.GodotExtensions\NodeUtil.cs:16` — `public static async Task<float> AwaitProcessFrame(this Node node, CancellationToken ct = default)`.
- 结论: `AwaitProcessFrame` 是 `MegaCrit.Sts2.Core.Nodes.GodotExtensions.NodeUtil` 上的扩展方法, 不是 NGame 成员; 主 runner 能编译是因为它有 `using MegaCrit.Sts2.Core.Nodes.GodotExtensions;` (`FormNativeSmokeRunner.cs:21`).
- 修复: 新文件补入同一 using (当前第 19 行). 无 stub, 无忙等替代, 调用形态 `() => game.AwaitProcessFrame()` 不变.

### 窄修 2: 能量 fixture 必须全部在绑定失效前 (已确认并修复)
- 原实现两处 `PlayerCmd.GainEnergy` 位于桥移除之后, 违反"失效后只能做两个被测 native 动作及只读快照".
- 权威签名: `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Commands\PlayerCmd.cs:29` — `public static async Task GainEnergy(decimal amount, Player player)`.
- 修复: 把补能移到真实 Crescendo 入愤怒且仍 Bound 之后、桥移除之前 (当前第 243-292 行). 先断言 `FormStanceWatcherBridge.IsAvailable` 为 true, 再记录补能前能量, 计算 `fixtureEnergyGain = max(0, minimum - before)`, 把 `PlayerCmd.GainEnergy` 的真实 `Task` 取回并在真实主线程提交后用 `AwaitOperationWithTimeoutAsync` 等待, 再记录补能后能量并断言达到最小值; 否则 failed 返回.
- 确定 fixture 数值: `BindingLossProbeEnergyMinimum = 2` (当前第 61 行). 依据: 探针 A `WATCHER_STRIKE_P` 费用 1 (`WatcherStrike_P.cs` 基类构造 `base(1, ...)`), 探针 B `WatcherMod.WatcherTranquility` 费用 1 (`WatcherTranquility.cs` 基类构造 `base(1, ...)`), 合计 2; 未伪造支付, 未跳过支付.
- 新增证据字段: `formsBoundBeforeFixture`, `energyFixtureMinimum`, `energyFixtureBefore`, `energyFixtureAfter`, `energyFixtureGain`, `energyFixtureActualTask`, `energyFixtureTaskStatus`, `energyFixtureTaskCompletedSuccessfully`; 保留 `energyFixtureBeforeStrike` (取自失效后的只读 before 快照 `ReadEnergy(before)`).
- 桥移除之后已无任何 `GainEnergy` 调用; 静态核对: 两处 `GainEnergy(` 引用 (描述字符串 + 真实调用) 均在第 261/265 行, 早于第 294 行 `bool bridgeRemoved;`.

### 窄修 3: explicitRejection 收紧 (已确认并修复)
- 原 `BindingLossHasRestartReason` 仅匹配泛化 `restart`/`not supported`/`unavailable`/`hot reload`, 任意失败都可能误判为显式拒绝.
- 修复为 `BindingLossHasExplicitRejection` (当前第 757-796 行):
  - `run.Passed == true` 直接 false;
  - 读取 `status` / `completionTaskOutcome` / `exception`: `timeout`/`pending`/无故障的 `cancelled` 一律 false (超时/未执行/取消不等于显式拒绝);
  - `observedFault` 要求 `status` 为 `faulted` 或 `failed`, 或 completionTaskOutcome 以 `faulted` 开头, 或存在 action exception;
  - 文本必须同时满足"明确 Forms 失效"与"需重启": `forms unavailable` / `forms is unavailable` / `forms invalid` / `forms lost` / 中文 `失效`/`不可用`/`丢失`, 且 `restart` / 中文 `重启`/`重新启动`.
- 未扩大场景: 字段/文件名/双探针/原 scheduler/排空/退出语义均保持.

### 静态自查 (非构建)
- 花括号 66/66, 圆括号 331/331 配平 (node 自测计数).
- 无 `BindingLossHasRestartReason` 残留; 无 TODO / NotImplementedException / NotSupported 占位.
- 文件为 UTF-8 无 BOM, LF 行尾; 非 ASCII 仅中文 `重启/重新启动/失效/不可用/丢失` 字面量, 均在 AGENTS.md Sec 5 允许范围内.
- 改动后文件: 39063 bytes, 851 行, SHA256 `26CBCBC1D32855F197DDD2ABE3297F1FC98C55F5C72F0D64A090A134582C2500`.

## 进行中
- 无. 三处窄修均已完成并静态自查.

## 未知
- 修复后能否通过主会话集中构建 (本批明确不构建, 不宣称).
- 两个场景在 r5 字节下的真实运行结果: 是否出现 `Forms unavailable` + `restart` 显式拒绝, 是否无原生副作用, 均未运行, 不得预先宣称.
- terminal 场景真实 pump 消费通知所需帧数是否落在 30 秒有界等待内, 未运行验证.
- `PlayerCmd.GainEnergy` 的真实引擎行为 (Hook 修改后是否仍达到最小值) 未在运行中验证; 代码在未达最小值时如实 failed.

## CODE_COMPLETE

### 唯一代码文件与 hash
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`
  - 改动前: `A7BADDB66C49868B3005E0A9820ABE5BD8061D4A9D146709B2401ACD8D89A51B` (788 行)
  - 改动后: `26CBCBC1D32855F197DDD2ABE3297F1FC98C55F5C72F0D64A090A134582C2500` (851 行, 39063 bytes)

### 精确 diff (逻辑摘要, 行号为改动后)
1. 第 19 行新增 `using MegaCrit.Sts2.Core.Nodes.GodotExtensions;` (修复 `AwaitProcessFrame` CS1061).
2. 第 61 行新增 `private const int BindingLossProbeEnergyMinimum = 2;`.
3. 第 243-292 行新增 bound fixture 断言 + 真实补能 Task + 补能前后与 actualTask/status 记录 (移动自失效后).
4. 删除失效后的 strike 前与 stance probe 前两段 `PlayerCmd.GainEnergy`.
5. 保留 `energyFixtureBeforeStrike = ReadEnergy(before)` (失效后只读快照).
6. 第 368-369 行改用 `BindingLossHasExplicitRejection`.
7. 第 757-796 行以严格 helper 替换原泛化 `BindingLossHasRestartReason` (含 status/timeout/pending/cancel/fault 与 Forms+restart 双重条件).
8. 新增 JSON 字段: `formsBoundBeforeFixture`, `energyFixtureBefore`, `energyFixtureAfter`, `energyFixtureGain`, `energyFixtureActualTask`, `energyFixtureTaskStatus`, `energyFixtureTaskCompletedSuccessfully`.

### 白名单外未改动
- `FormNativeSmokeRunner.cs` = `D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC` (未变).
- `README.md` = `E8F4D199A3132761DD6A228EE2D888C975EA70C4E6CCA0F44B5C1B2816FA1683` (未变).
- `FormsNativeSmoke.csproj` / `FormNativeSmokePatch.cs` / `MainFile.cs` / 生产源码 / 其它载体均未改动.


## 最终实测勘误 (本机当前, 非构建)
- 实际读取: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`
- 字节数: 39063
- 行数: 851
- SHA256: `26CBCBC1D32855F197DDD2ABE3297F1FC98C55F5C72F0D64A090A134582C2500`
- 先前 37416 bytes / 850 行摘要是中间版本; 本节为当前落盘最终值. 用户提到的 37693 bytes 与本机当前实测不一致, 本报告不据此改写.
