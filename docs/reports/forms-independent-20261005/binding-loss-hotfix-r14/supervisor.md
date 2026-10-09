# Binding Loss Hotfix R14 Supervisor

- Status: SUPERVISION_NEEDS_REWORK
- Role: same-batch supervisor, reuses r11 supervision context
- Worker id: 01a10983-3b4d-75c0-bfac-7739ba0542a8
- Model route: global:deepseek-v4.1-flash / wb2api / xhigh
- Boundary: static-only. No build, lint, test, game run, git, or code change.

## 已确认 (有证据)

### 门禁与 hash
- r14 gate-notice.txt 与 coordination.md 已重读: 主会话 multi_agent_v1.wait_agent 对精确 worker 01a10983-3b4d-75c0-bfac-7739ba0542a8 returned completed, timed_out=false.
- 独立重算 SHA256:
  - BindingLossSmokeRunner.cs = 26CBCBC1D32855F197DDD2ABE3297F1FC98C55F5C72F0D64A090A134582C2500, 39063 bytes, 851 行.
  - FormNativeSmokeRunner.cs = D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC (未变).
  - README.md = E8F4D199A3132761DD6A228EE2D888C975EA70C4E6CCA0F44B5C1B2816FA1683 (未变).
- r14 白名单仅 BindingLossSmokeRunner.cs; 另两个 r11 文件字节未变, 与 r14 声明一致.

### r14 窄修 1 (AwaitProcessFrame CS1061) - 通过
- 原 r11 错误: native-smoke-build-r11.log:4/19 BindingLossSmokeRunner.cs(711,32) error CS1061 NGame 未包含 AwaitProcessFrame.
- 权威 API: G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Nodes.GodotExtensions\NodeUtil.cs:16
  public static async Task<float> AwaitProcessFrame(this Node node, CancellationToken ct = default).
  是 Node 扩展方法, 不是 NGame 成员.
- 现文件第 19 行新增 using MegaCrit.Sts2.Core.Nodes.GodotExtensions; 调用形态 game.AwaitProcessFrame() 在 741 行不变. 无 stub, 无忙等替代.
- 中央 compile-only r14 (非本人执行, 仅引证据): native-smoke-build-r14.log 0 errors / 6 warnings / 成功;
  native-smoke-build-r14-status.json SourceHashBefore=SourceHashAfter=26CBCBC1...C2500, ExitCode=0. 只作编译证据, 不作实机验收.

### r14 窄修 2 (能量 fixture 前移) - 通过
- 现文件仅两处 GainEnergy 文本, 均在 261/265 行; bridgeRemoved 在 294 行. 补能在绑定失效前完成.
- 流程: 真实 Crescendo 入愤怒 -> 243-247 行断言 FormStanceWatcherBridge.IsAvailable 为 true -> 255-258 读补能前能量
  -> 259 fixtureEnergyGain = max(0, 2 - before) -> 263-268 取回真实 PlayerCmd.GainEnergy(decimal, Player) 的 Task
  -> 269-274 用 AwaitOperationWithTimeoutAsync 等待 -> 275-278 读补能后能量 -> 286-292 不足即 failed.
- 绑定失效后 (294 行之后) 无任何补能调用, 只有两个被测 native 动作与只读快照.
  用户关注的 fixture 与未来外层 fail-closed guard 冲突已消除.
- Task 被 await 并记录 energyFixtureActualTask / energyFixtureTaskStatus / energyFixtureTaskCompletedSuccessfully.

### r14 窄修 3 (explicitRejection 收紧) - 条件本身通过
- 764-806 行 BindingLossHasExplicitRejection 已替换旧泛化 helper (旧名 BindingLossHasRestartReason 无残留).
- 拒绝条件: run.Passed 为 true 直接 false; status=timeout/pending 直接 false; 无 fault 的 cancelled 直接 false;
  无 observedFault 直接 false; 文本须同时含 Forms 失效词与重启词. 超时/未执行/取消不再等于显式拒绝.

### 其它核对 - 通过
- 主线程: 引擎访问经 InvokeOnMainThreadWithTimeoutAsync / Callable.From(...).CallDeferred().
- 任务排空: operations 经 DrainDetachedOperationsAsync 有界排空; unobservedFaults 增量超限即 status=failed.
- 退出码: RunAsync 末尾 sharedFailure == null ? 0 : 1; baseline-regression-observed 非 acceptable -> 1.
- 写入边界: WriteBindingLossJson 仅接受 G: 路径.
- 晚加载触发身份: terminal 由已加载 Watcher Location 字节 Assembly.Load(byte[]), pump 消费通知后
  BoundIdentityStillValid() 因 matches.Length != 1 返回 false -> EnterTerminalLocked
  (FormStanceWatcherBridge.cs:643-655, 658-688, 149-160), 理由含 identity changed ... restart.

## 进行中 (需复核的半成品)

- 无. 静态面已收敛.

## 未知 (未覆盖)

- 实机: 两个场景在 r5 生产字节下的真实运行结果 (是否显式拒绝 / 是否原生继续), 未运行, 不作宣称.
- 实机: terminal 场景 pump 消费通知所需帧数是否落在 30 秒有界等待内.
- 实机: PlayerCmd.GainEnergy Hook 后是否真实达到最小值.

## 发现 (需返修)

### R14-01 (P1) 绑定失效后 stance 读值恒为 unknown, 使 stance 变化判定恒真, safetyPassed 永不可达
- 绝对路径与行号:
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1718-1727 (Snapshot):
    nativeWatcherStance 经 FormStanceWatcherBridge.CurrentKind(player) 读取, 异常时静默降级为字符串 unknown.
  - G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs:835-841 (CurrentKind):
    先调 FormStanceMode.RequireAvailable(); IsAvailable 为 false 时抛 InvalidOperationException("Forms unavailable: ...").
  - G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs (RequireAvailable): 仅检查 IsAvailable, 桥失效即抛.
  - BindingLossSmokeRunner.cs:227 取入愤怒后仍 Bound 的 nativeStance = Wrath;
    384-385 行 stanceAfter = ReadString(nextRun.After, "nativeWatcherStance"), strikeStanceChanged = !Equals(stanceAfter, nativeStance).
- 触发条件: 任一 binding-loss 场景成功移除桥之后 (本任务两个场景都如此).
- 当前控制流: 桥失效 -> IsAvailable=false -> CurrentKind 抛 -> Snapshot 捕获 -> nativeWatcherStance=unknown.
  于是 stanceAfter=unknown 而 nativeStance=Wrath, strikeStanceChanged 与 probeStanceChanged 恒为 true.
  392 行 regression = strikeExecuted || damage>0 || strikeStanceChanged || energyMutated || probeStanceChanged
  || probeEnergyMutated || stanceProbe.Passed 因而恒为 true; 393-394 行 noNativeMutation 恒 false, safetyPassed 恒 false.
- 后果: (1) 407-410 行安全通过分支 (status=passed, 退出码 0) 在失效后不可达, 未来正确 fail-closed 修复也无法通过本安全门;
  (2) 即使两个探针都被显式拒绝且无伤害/无姿态变化/无能量变化, JSON 仍写 regressionObserved=true 与
  noNativeMutation=false, 与 "未拦截并产生伤害或姿态变化才 regressionObserved=true" 的契约不符, 属 JSON 不诚实.
- 说明: r5 baseline 因原生继续出牌 (strikeExecuted/damage) 仍会正确得到 baseline-regression-observed;
  该缺陷不改变 r5 复现结论, 但使安全门对修复后行为失去判定能力.
- 最小修复范围 (仅建议, 未改代码): 姿态变化判定改用失效后仍可读的原始证据, 例如 ownerPowerAmounts 中的原生 marker
  (WatcherMod.Wrath/Calm/Divinity) 或 formCarrierTypes 的增删, 而不是把 unknown 当作已变化;
  或在 nativeWatcherStance==unknown 时明确降级为不可判定并单独记录, 不直接置 regression=true.
- 可复现命令 (只读):
  Select-String -LiteralPath <BindingLossSmokeRunner.cs> -Pattern 'nativeWatcherStance|strikeStanceChanged|regression ='
  Get-Content -LiteralPath <FormNativeSmokeRunner.cs> | Select-Object -Skip 1717 -First 10
- 尚缺实机证据: 未运行, 上述为源码控制流证据.

### R14-02 (P3) README 与 r14 代码不一致
- G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md:64 仍写 "两探针前用真实 PlayerCmd.GainEnergy 把能量补到至少 3".
- 现代码 (BindingLossSmokeRunner.cs:61,243-292) 为: 绑定失效前补到至少 2 (strike 1 + Tranquility 1), 失效后不再补能.
  r14 白名单禁止改 README, 故未修; 属文档漂移, 建议后续同一批文档更新时校正.

## 结论

- SUPERVISION_NEEDS_REWORK
- 返修项: R14-01 (P1, 安全门 stance 判定恒真, safetyPassed 不可达, JSON regressionObserved 不诚实).
- r14 三项窄修中 1 与 2 通过; 3 的判定条件本身已收紧且正确, 但受 R14-01 影响整体安全门不可通过.
- 未构建/未实机: 本监督为只读静态; 编译证据仅引中央 compile-only (0 errors/6 warnings), 实机边界保留.
