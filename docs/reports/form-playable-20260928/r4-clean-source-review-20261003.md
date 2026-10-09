# R4 clean source review 2026-10-03

范围: 只读审查 clean worktree 的 `FormNativeSmokeRunner.cs` 与 `Sts1EventToggleFilterPatch.cs`, 对照当前工作树与 a6e46e5 基线. 不构建, 不运行游戏, 不改产品代码, 不再委派.

取证目录:
- Clean: `G:\omp works\.tmp\spire1-beta-r4-clean`
- Current: `G:\omp works\Sts\sts2-spire1`
- 本次 diff 摘录 (非产品文件): `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\r4-review-20261003`

唯一可写路径即本文件. 未写产品代码, 未构建, 未运行游戏, 未改共享 mod_configs.

## 已确认

1. [P3] 审查基线与输入文件身份已固定.

- clean worktree HEAD: `a6e46e53ae891e4faa7b640a64c1000a9e566c9a`.
- a6e46e5 中两个目标相对路径均存在, 可逐文件对比.
- clean/current 两个目标文件逐字节一致:
  - `FormNativeSmokeRunner.cs`: 152416 bytes; SHA256 `3001C04C0225BA0EE44A865EF542A50F8D1315836BBE4D086BF31EBCB834BF88`.
  - `Sts1EventToggleFilterPatch.cs`: 14350 bytes; SHA256 `0FCCA3D3AC809B5E7D6A6C0DC6AE01178059B0EA1C61EAA4C36F11DA1FDD5E83`.
- 更正: clean worktree 并非干净. `git -C clean status` 显示两个目标文件均 `modified`; 首次误读已按实际状态更正.
- 与 a6e46e5 差异:
  - `FormNativeSmokeRunner.cs`: 1253 insertions, 116 deletions.
  - `Sts1EventToggleFilterPatch.cs`: 8 insertions, 8 deletions.
- 复现:
  - `git -C 'G:\omp works\.tmp\spire1-beta-r4-clean' status --porcelain=v1`
  - `git -C 'G:\omp works\.tmp\spire1-beta-r4-clean' diff --numstat a6e46e5 -- 'mod/Spire1Code/Run/FormNativeSmokeRunner.cs' 'mod/Spire1Code/Patches/Sts1EventToggleFilterPatch.cs'`
  - `Get-FileHash -LiteralPath <四个路径> -Algorithm SHA256`
- 尚缺: 无 (文件身份为已完成项).

2. [P2] Divinity 入口已改为真实无目标出牌, 与 Blasphemy 的 `TargetType.None` 一致.

- 证据: `FormNativeSmokeRunner.cs:1417-1420` 注释声明 Blasphemy 构造传 `TargetType.None`; `:1432-1440` 实参为 `null`; `:1249` 卡 id 为 `WATCHER_BLASPHEMY`; `:1256-1261` 从 `ModelDb.AllCards` 按 `Id.Entry` 查真实卡.
- 独立源码对照: `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherBlasphemy.cs` 的构造调用为 `base(1, (CardType)2, (CardRarity)4, (TargetType)0)`, `OnPlay` 调 `WatcherCombatHelper.EnterDivinity`; 即 `TargetType.None` 的真实第三方源码证据.
- 当前控制流: `FindFirstHittableEnemy(..., preferNoArtifact: true)` 仅取观察目标 `entryTarget` (`:1421-1430`), 该 target 不传入出牌; 无敌人则直接 failed.
- 尚缺: 未实机确认 Blasphemy 在 r4 隔离包中进入 Divinity 后的下一回合行为.

3. [P2] `blocked` 语义只在回合边界未达下一 Play 阶段时产生, 且不会伪装成 pass.

- 触发: `boundary.Passed == false` (`FormNativeSmokeRunner.cs:1470`), 子因区分 `PlayerDead` / `CombatEnded` / 其它未达下一回合 (`:1473-1477`).
- 输出: `passed=false`, `status=blocked`, `blockedReason`, 并尽力写 `afterBoundary` 证据 (`:1478-1503`).
- 该分支仍调用 `ApplyUnobservedFaultGate` (`:1502`); 若有 `TaskHelper.UnobservedFault`, gate 会把状态覆盖为 `failed` 并置 `terminalFailure=true` (`:1199-1224`), 因此 blocked 不掩盖崩溃级故障.
- 尚缺: 未实机确认该分支在 r4 是否触发, 以及 `afterBoundary` 证据内容.

4. [P2] 顶层 `partial` 的接受范围严格限于 `turns` 场景.

- 只有 `turns` 且 `divinity.status == "blocked"` 时写 `status=partial` (`FormNativeSmokeRunner.cs:411-421`, `:2109-2126`).
- 其它场景的 `blocked` / `failed` / 缺失 status 都会令 `sharedFailure` 非空, `RunAsync` 返回 1 (`:429`).
- `turns` 写 partial 时 `failure` 为 blocked 原因, 不置 `passed=true` (`:2116-2125`).
- 尚缺: 未构建, 未运行; r4 实机 JSON 必须复核 partial 是否被 drain/gate 升级为 failed.

5. [P2] 主线程 gate 与 detached 操作清理边界有界且 fail-closed.

- 所有游戏状态访问经 `InvokeOnMainThreadWithTimeoutAsync` (`FormNativeSmokeRunner.cs:3312-3333`), 默认 10 秒 gate; 超时会登记未完成 invocation 并抛 `MainThreadGateException`.
- 同线程直接执行; 异线程经 `Callable.CallDeferred` (`:3253-3287`).
- `turns` 场景的 `TerminalOperationException`/`MainThreadGateException`/`TimeoutException` 会置 `terminalFailure=true` (`:2145-2162`).
- `finally` 先 drain detached operation; 只有全部 settle 才 `runManager.CleanUp(graceful:false)`, 未 settle 则 `cleanup=skipped` 并置 failed (`:2207-2242`).
- `runManager == null` 时跳过 cleanup 并记录原因 (`:2298-2305`); 最后无条件再跑 `ApplyUnobservedFaultGate` (`:2309`).
- 尚缺: 未实机验证 Blasphemy/EndTurnDeathPower 路径下 detached operation 能否在 10 秒内 settle.

6. [P3] `Sts1EventToggleFilterPatch.cs` 的 fallback 重命名只解决编译遮蔽, 无行为变化.

- diff 仅两处局部变量:
  - `fallback` -> `emptyPoolFallback` (`:138-156`), 引用同步替换, 逻辑不变.
  - `fallback` -> `allGen1Fallback` (`:174-188`), 引用同步替换, 逻辑不变.
- 除这 8 处增删外, 唯一其它差异是文件末尾 newline 状态.
- 复现: `git -C '<clean>' diff --unified=8 a6e46e5 -- 'mod/Spire1Code/Patches/Sts1EventToggleFilterPatch.cs'`.
- 尚缺: 未编译, 无法以构建错误列表验证 `CS0136` 确已消失; 可确认该差异不改变运行语义.

7. [P3] `WATCHER_BLASPHEMY` 拼写修复已落盘.

- clean 全树 `*.cs` 中 `WATCHER_BLASPHEMY` 出现 3 次, 旧错串 `WATCHER_BLASHPHEMY` 0 次.
- 行: `FormNativeSmokeRunner.cs:1249`, `:1417`, `:1437`.
- 复现: `rg -n 'WATCHER_BLASPHEMY|WATCHER_BLASHPHEMY' '<clean>' -g '*.cs'`.
- 尚缺: 未运行 turns 场景验证 Division 分支确实可到达; 仅静态确认字符串与真实卡 id 相同.

8. [P2] 可选 mod 硬引用与 manifest 前置风险未在本次改动中放大.

- `Spire1.csproj:22-29` 仅有 `0Harmony` 与 `sts2` 的 Reference; `:32-37` 说明 AutoAnthony Reference 已删除; `:42-50` 为 PrivateAssets 构建包.
- `Spire1.csproj` 内无 `SPIRE1_FORM_MOD` DefineConstants; `VoidFormPlayTransactionPatch.cs:14,78,691,709` 的 `#if SPIRE1_FORM_MOD && !GODOT` 仅剩探针分支.
- `Spire1.json` dependencies 只有 `BaseLib >= 3.4.5`.
- Watcher 只有运行期反射桥: `FormStanceWatcherBridge.cs:25-26` 明确 optional reflection-only; `:108-116` 仅在已加载程序集名为 `Watcher` 时解析 `WatcherMod.*`.
- 缺 Watcher 时 `TryBind` 失败并设 `UnavailableReason`, 由 `FormStanceMode.RequireAvailable` 显式失败 (`FormStanceMode.cs:27-36`), 不静默半启用.
- AutoAnthony/AFTP 兼容层沿用反射/缺席 no-op (`AutoAnthonyCompatBridge.cs:31-38`; `AutoAnthonyLoadHook.cs:862-864`).
- 尚缺: 未构建, 未对最终 DLL 跑 AssemblyRef/manifest 门禁; 本次只做源码级扫描.

## 进行中

- 请求列出的检查面均已完成静态审查; 没有待收尾的代码面.
- 若后续收到 r4 实机 JSON, 可继续复核: Divinity blocked/partial、detached operation settle、`afterBoundary` 内容.

## 未知

- 未构建, 未运行游戏, 未读 r4 实机 JSON.
- 未在真实 Watcher 0.9.28 运行时验证反射签名与 Hook 绑定.
- 未验证最终产物 AssemblyRef 与 manifest 一致性门禁.
- 未验证 Blasphemy/EndTurnDeathPower 在隔离实机中的实际回合行为.