# r26 监督审查 (bounded stop 2026-10-05)

范围: 冻结 r26 产物 RuntimeSafetySmokeRunner.cs + RUNTIME-SAFETY.md.
门禁: gate-notice.json Worker 01a10a33-e88b-7c02-9cda-ab5181bd3b0d Status=completed, TimedOut=false, WorkerResult=CODE_COMPLETE.
模型/路由: 请求文本 global:deepseek-v4.1-flash / wb2api / xhigh; 未取得独立会话元数据, 不把请求文字冒充已解析路由证据.
方法: 只读源码静态核对. 未构建/lint/Parser/测试/peer/委派/其它 harness.
本轮因超过 10 分钟有界窗口被 bounded-stop-1221.txt 叫停, 未覆盖面一律记 Unknown, 不判 PASS.

## 已确认

### 0. 冻结 hash 与 namespace (有证据)
- RuntimeSafetySmokeRunner.cs 实际 SHA256 F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919 (bytes 82442) 与 gate-notice.json 一致.
- RUNTIME-SAFETY.md 实际 SHA256 891FE834C0ECC6D4C5A0FA746531153BC05D74472C165E914973490B649B08FC (bytes 5952) 与 gate-notice.json 一致.
- baseline-gate.json 四个 r25 文件 hash 与当前磁盘全部一致, r26 未改 r25 冻结写集.
- 两 file-scoped namespace 已改 block: :31 namespace FormsNativeSmoke.Patches (RuntimeSafetySmokePatch :34), :44 namespace FormsNativeSmoke.Run (partial FormNativeSmokeRunner :46); 全名/归属不变, 无重复类型或方法签名冲突.
- patch 由 test carrier 扫描安装: test MainFile 扫描本 assembly 全部 [HarmonyPatch] 类型 (MainFile.cs:80-119), csproj 无 Compile Remove; 与 FormNativeSmokePatch 共存于 NGame._Ready.

### 1. 主线程 gate / settled / 副作用比较 (静态符合, 有证据)
- 所有启动/绑定/读取/命令提交经 InvokeOnMainThreadWithTimeoutAsync (r25 FormNativeSmokeRunner.cs:3534-3555); TryBind 只在 :528-531 gate 内调用, await 后无直接调用.
- 三命令 submit 在 gate 内, observe 在调用线程等待 Task (:669-757); 未 settled 立即 throw 终止后续 probe (:322-323/:336-337/:349-350).
- 未 settled 时 FinalizeProbe 置 passed=false 且不读 after (:792-799); RuntimeSafetySnapshotsUnchanged 对 HP/Block/Energy/全名 power amounts/raw marker amounts 全比, 缺值 false (:826-850), 不会 0=0 通过.
- expected 身份: RuntimeSafetyRecordExpectedTree 保留原始 Exception 对象 (:759-767), ApplyUnobservedFaultGate 按 ReferenceEqualityComparer 叶子身份批准 (r25 FormNativeSmokeRunner.cs:1327-1366), 非文本匹配.
- 收尾: fault 订阅到 post-quit 最后快照后退订 (:195-206); pre-quit 写失败强制 passed=false+exitCode=1 (:149-158); post-quit 写失败置 failed/passed=false/exitCode=1 并重试 (:208-222); Quit 在 gate 内 (r25 FormNativeSmokeRunner.cs:3556-3578); quitDrainSettled 参与 completed (:184-193).

### 2. 更正: 未选局 Strike 2x 来源不是 bridge patch (撤回上一版 P0)
- 上一版把 WatcherMod.Wrath 的 2x 归因于 bridge 的 NeutralizeDamagePrefix 是错的. 该 prefix 实际是"中和"补丁: 当且仅当 IsSelectedAndBound 时令 __result=1m 并 skip original (FormStanceWatcherBridge.cs:962-968), 即选中 Forms 局里把 Wrath 原生 2x 压成 1x.
- 未选局没有 FormStanceModifier, IsSelectedAndBound=false, 该 prefix 直接 return true 不介入. 因此未选局的 2x 来自 Watcher 自身 ModifyDamageMultiplicative (原生 PowerModel hook, 非 Harmony patch), Shutdown 不会移除它.
- 结论: 未选局 expected=(base+Strength)*2 的 2x 来源与 Shutdown 无冲突, 原 P0 不成立, 撤回.

## 未知 (未覆盖, 不得视为通过)

### 3. 未选局 Strike 期望值细节 (P1, Unknown)
- 待确认: StrengthPower.ModifyDamageAdditive 要求 props.IsPoweredAttack() (StrengthPower.cs), 而 Strike 经 AttackCommand 默认 DamageProps=ValueProp.Move (AttackCommand.cs:127); ValueProp.Move 是否满足 IsPoweredAttack 及 Wrath 的 (ValueProp)8/(ValueProp)4 判定, 本轮未读完 ValueProp 常量定义, 未确认.
- 待确认: RuntimeSafetyComputeMultiplier (:904-931) 在 Shutdown 前采样 powers, 未验证失效后 Strength/Wrath 集合与采样时相同 (中间仅 Shutdown + 两次快照读取, 静态看无写, 但未实机).
- 待确认: 选中局 guard ownerCounts 语义. RuntimeSafetyOwnerCounts (:1149-1177) 只统计 Harmony patch owner; 契约里 fail-closed 的 FormStanceModifier 是 RunState model hook (FormStanceModifier.cs:43-72), 不是 Harmony patch. 故 ownersBefore.Forms==0 不等于"生产 fail-closed guard 已生效"; 该证明实际靠 :324/:338/:351 的 ExpectedRejection 文本含 Forms unavailable+restart (RuntimeSafetyCommandEvidence.HasFormsUnavailableRestartText :1244-1249), 属文本而非身份级. 计数异常会 fail closed (:1171-1175 / :1313-1321), 不构成静默通过, 但语义弱于 request 第 2 条文字.

### 4. 实机面 (Unknown)
- 未构建/未测试/未运行; 无真实 Harmony.GetPatchInfo 输出, 未确认 Shutdown 后 Forms/Forms.FormStanceMode.Watcher 两 owner 真实为 0, 未确认 remove identity 前后一致.
- 无真实线程门日志、真实 settled 结果、原始 fault 队列、drain/cleanup 输出、两场景 JSON、退出码.
- post-quit 写失败边界: SceneTree.Quit 已由 :165 用旧 exitCode 发出, :214 新 exitCode=1 无法改变已发出 code (代码注释 :215-217 自认不可达); 是否可接受由主 hub 判定.

## 结论

SUPERVISION_INCOMPLETE (不得判 PASS)

- 已确认面: 冻结 hash、namespace 修复、patch 注册、主线程 gate/settled/副作用比较/收尾 fail-closed 静态路径.
- 上一版 P0 (未选局 2x 来自被撤 bridge patch) 经复核为误判, 已撤回.
- 因 bounded-stop 超时叫停, 未完成实机与 ValueProp/ownerCounts 复核, 故本轮不出 PASS, 亦不把未覆盖面当失败定论. 建议主 hub 在冻结 hash 下补做第 3/4 面复核后再判.
---

# 补充静态面 (supplement.request.md, 2026-10-05)

冻结 hash 复核: RuntimeSafetySmokeRunner.cs 仍为 F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919, 未改.

## 面 1: ValueProp.Move / IsPoweredAttack / Wrath 判据 -> PASS

- 权威文件:
  - research\engine-dllsrc\MegaCrit.Sts2.Core.ValueProps\ValueProp.cs:12-18: Unpowered=4, Move=8, Unblockable=2; 注释明确 Move = Attack damage from Attack cards and Enemy creatures attacking.
  - research\engine-dllsrc\MegaCrit.Sts2.Core.ValueProps\ValuePropExtensions.cs:5-14: IsPoweredAttack() = props.HasFlag(Move) && !props.HasFlag(Unpowered); Move-only -> true.
  - research\engine-dllsrc\MegaCrit.Sts2.Core.Commands.Builders\AttackCommand.cs:127: DamageProps 默认 = ValueProp.Move; :214-235 FromCard 只设 Attacker/ModelSource/CardPlay, 不改 DamageProps.
  - research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\StrengthPower.cs:19-27: owner==dealer 且 IsPoweredAttack() -> 返回 Amount (加算).
  - .tmp\watchermod\WatcherMod\Wrath.cs ModifyDamageMultiplicative: flag = props.HasFlag(8) && !props.HasFlag(4) (即 Move && !Unpowered); dealer==Owner 或 target==Owner 时返回 2m, 否则 1m. 该 2x 是 Watcher 自身 PowerModel hook, 不是 Harmony patch.
  - research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:2524-2540: ModifyDamageInternal 先 Additive 后 Multiplicative.
- 静态判定: 未选局 Strike 由 DamageCmd.Attack(6).FromCard(...).Targeting(...).Execute 发出 (WatcherStrike_P.cs:27-30), DamageProps=Move, 不额外调用 Unpowered() -> IsPoweredAttack=true -> StrengthPower 加算生效, Wrath flag=true 且 dealer==owner -> 2x 生效. 引擎顺序先加后乘, 故 expected=(DynamicVars.Damage.BaseValue + Strength) * 2 合法.
- 桥 patch 不构成冲突: FormStanceWatcherBridge.cs:962-968 NeutralizeDamagePrefix 仅在 IsSelectedAndBound 时把 __result 中和为 1m 并 skip original; 未选局 IsSelected=false -> 返回 true, 原始 Wrath 2x 正常执行. 与 Shutdown 时序无关.
- 面 1 结论: PASS. 未选局两牌期望值静态合法 (Tranquility 无 Damage var, 代码用 -1 哨兵且不参与 Strike 期望, :955-957).

## 进行中

面 2 与面 3 正在补, 见下.

## 未知

暂无新增; 原面 3/4 实机证据仍由 hub 集中采集, 不作为静态阻断.
## 面 2: ownerCounts 语义边界 vs 实际 false-pass -> PASS (是语义边界, 非 false-pass)

- 实际控制流 (selected): :310-317 guardPassed 要求 guardBefore/After 的 Remove 精确 guard (prefixCount==1, owner=Forms.FormStanceSafety, PatchMethod=Forms.FormsCode.FormStanceSafetyGuard.RemovePrefix) 且 remove identity 前后相同; :320-330 Remove 探针要求 settled && ExpectedRejection && 严格 before/after 无副作用; :334-344 Damage 同; :347-357 ModifyAmount 同. passed=guardPassed && 三个 probe.Passed (:359-360).
- 三条命令的 fail-closed 实际来源不同, 但每条都被真实调用验证:
  - PowerCmd.Remove: FormStanceSafetyGuard.RemovePrefix (FormStanceSafetyGuard.cs:112-120) 是驻留 Harmony prefix, Shutdown 不撤 (MainFile.cs:185-225), 由 guardPassed 的 prefixCount/identity 精确证明.
  - CreatureCmd.Damage: FormStanceModifier.BeforeDamageReceived (FormStanceModifier.cs:50-60) 经 Hook.BeforeDamageReceived 在 DamageBlockInternal/LoseHp 之前派发 (CreatureCmd.cs:283-287); FormStanceModifier 是 RunState model hook, 不是 Harmony patch.
  - PowerCmd.ModifyAmount: FormStanceModifier.BeforePowerAmountChanged (FormStanceModifier.cs:63-72) 经 Hook.BeforePowerAmountChanged 在 power.SetAmount 之前派发 (PowerCmd.cs:231-241).
- 判定: ownerCounts 只统计 Harmony owner 是"证明手段边界", 不是 false-pass. 因为契约要求的不是"model hook 出现在 Harmony 计数", 而是三条真实命令被拒且状态无变化; 该要求由 :320-357 的 settled + ExpectedRejection + 严格 before/after 共同强制. FormStanceModifier 无法被 Harmony 计数覆盖是设计事实 (model hook), request 亦说明依赖真实 model hook 异常即可证明所测路径, 不要求出现在 Harmony 计数.
- 仍需注意 (不改变 PASS 定级): ExpectedRejection 的判定是文本 (HasFormsUnavailableRestartText 要求含 Forms unavailable + restart, :1244-1249), 不是身份级; 但异常 root 保留为原始 Exception 对象并由 ReferenceEqualityComparer 叶子身份批准 (:759-767, r25 FormNativeSmokeRunner.cs:1327-1366), 文本只用于"是否明确重启". 因此不构成可伪造的 false-pass.
- 面 2 结论: PASS.

## 进行中

面 3 正在补, 见下.

## 未知

实机证据 (真实 Harmony 计数输出/真实命令 settled/退出码) 仍由 hub 集中采集, 不作为静态阻断.
## 面 3: RuntimeSafetySmokeRunner.cs:149-222 post-quit 收尾 -> P1 (报告/退出码一致性缺口, 非安全探针 false-pass)

- 关键顺序: :119 exitCode=1 -> :134-135 场景通过则 exitCode=0 -> :160 result["exitCode"]=exitCode -> :165 QuitOnMainThreadAsync(game, exitCode) 已提交 SceneTree.Quit -> :184-193 由 passed/quitStatus/quitDrainSettled 重算 completed, 失败则 result["exitCode"]=1 且 exitCode=1 -> :197-206 才做最终 fault gate -> :208-222 post-quit 报告写入.
- 晚 fault 行为: :203 调 ApplyUnobservedFaultGate, 该 helper (r25 FormNativeSmokeRunner.cs:1277-1317) 在出现 unexpected 时设 result["passed"]=false / status="failed" / terminalFailure=true / failure, 但**不改 result["exitCode"]**, 也不改局部 exitCode. 此时 :165 的 Quit 已用旧值 (completed 时为 0) 提交. 故 post-quit 晚 fault 会得到 status=failed + passed=false + exitCode=0 的报告, 且进程退出码仍为 0. raw fault 会被完整保留 (unobservedFaults/unobservedFaultEvidence/expectedRejections/unexpectedUnobservedFaults, :1295-1298), 这一半满足.
- post-quit 写失败行为: :209-218 设 result["exitCode"]=1 并重试 WriteRuntimeSafetyJson, 但 Quit 已用旧 exitCode 提交, 进程仍可能 exit 0. 若重试也失败, 磁盘上只剩 :149 的 pre-quit 报告 (当时 status 可能为 completed/passed=true/exitCode=0, 且 finalEvidencePhase 尚未写为 post-quit, :160-163 在 pre-quit 写之后才设置), 该文件会被误当 final. 这与 RUNTIME-SAFETY.md "post-quit 写失败也记录 failed 并尽最大可实现方式非零退出" 的"尽最大可实现方式"文字可辩, 但当前实现没有把"实际进程退出码"与"报告声明 exitCode"区分开, 构成可误导的 final 证据.
- 判定: 这不是选中局/未选局安全探针的 false-pass (那些 passed 由 :359-360 / :502-506 决定, 且晚 fault 会置 passed=false), 而是最终收尾的 P1 报告完整性/退出码一致性问题. 依赖 controller 严格 final 校验可 fail closed, 但 RUNTIME-SAFETY.md 的契约文字未规定 controller 必须校验 finalEvidencePhase/exitCode 一致, 因此静态面不能直接判为已满足.
- 最小修复建议 (不自行改代码):
  1. :203 最终 fault gate 之后重新做一次 exitCode 收敛: 若 result 已 failed, 则 result["exitCode"]=1 并另记 processExitCode 实际值, 避免 status=failed/exitCode=0.
  2. 在 :208 post-quit 写失败分支, 除置 failed 外, 用尽最大可实现方式强制非零退出 (例如 Environment.Exit(1) 或等效), 或在报告中显式写 exitCodeAuthoritative=false, 并让 controller 对 pre-quit-only/exitCode 不一致一律 fail.
  3. controller 严格 final 校验: 要求 report.finalEvidencePhase=="post-quit" && status=="completed" && passed==true && exitCode==0 且进程退出码==0; 任一不满足即整体 fail.
- 面 3 结论: P1. 契约文字有"尽最大可实现"余地, 但实现存在 status=failed 与 exitCode=0 并存、pre-quit 报告可能被误当 final 的静态缺口, 需最小修复或 controller 严格校验后才可判整体 PASS.

## 补充静态面结论

NEEDS_REWORK

- 面 1 PASS: ValueProp.Move=8 / IsPoweredAttack / Strength 加算 / Wrath 原生 2x 的静态合法性成立, 未选局 expected=(base+Strength)*2 合法; bridge NeutralizeDamagePrefix 只在选中局中和, 未选局不介入.
- 面 2 PASS: ownerCounts 仅统计 Harmony 是语义边界而非 false-pass; 三条真实命令的 settled + 完整 Forms unavailable/restart 异常 + 严格 before/after 无副作用 + 精确 resident Remove guard 共同构成所测路径的证明, model hook 无需出现在 Harmony 计数.
- 面 3 P1: post-quit 晚 fault 不更新 result["exitCode"], 且 post-quit 写失败后 Quit 已提交, 可能 status=failed 与 exitCode=0 并存、pre-quit 报告被误当 final. 需最小修复或 controller 严格 final 校验.
- 未构建/未测试/未实机; 实机证据由 hub 集中采集, 不作为静态阻断.