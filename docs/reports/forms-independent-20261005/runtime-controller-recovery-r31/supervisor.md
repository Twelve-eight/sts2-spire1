GATE_OPEN - POST-GATE STATIC SUPERVISION - FINAL

## 已确认
- 门禁真实开启: central-gate-notice.request.md 声明本批 worker 已真实 native completed/CODE_COMPLETE, gate-notice.json 已落盘. 本轮按 supervisor.request.md 做门禁后窄静态审核.
- 模型/路由固定: global:deepseek-v4.1-flash / wb2api / xhigh; 不委派, 不用 peer/harness/模型切换, 不 close/resume, 不改代码/构建/测试/游戏/Git.
- 冻结源 hash 独立核对: `G:\omp works\.tmp\forms-independent-20261005\RuntimeSafetySmokeRunner-r29-frozen-for-r31.cs` SHA256=48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C, 1385 行.
- r31 脚本 hash 独立核对: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r31.ps1` SHA256=25E049A0E228861736F6CEA2569A49926AF71B6A485666CEED69D23574EF14EE, 746 行.
- 权威契约: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md` 与冻结源同目录; .tmp 根目录无该文件.
- [裁决1 - ownerCounts 矛盾] 冻结 source 的真实调用顺序为: `:272-275` Shutdown 前采 `guardBefore` + `ownerCountsBefore`; `:329-336` 先 `Forms.FormsCode.MainFile.Shutdown()`, 再采 `guardAfterShutdown` + `ownerCountsAfterShutdown`; `:338-345` 断言. 真实期望是两端均为 Forms=0, Forms.FormStanceMode.Watcher=0, Forms.FormStanceSafety=1. r31 `:579-585` 与此完全一致. worker.md 把 Safety 写成 0 的表述是文字错误, 不是 source 契约, 也不是 r31 解析缺陷. 不判 P1.
- [裁决2 - 3 command 原始证据] r31 `:596-606` 对 runtime-safety 精确取 removeProbe/damageProbe/powerAmountProbe 三项, 要求恰 3 且 label 唯一; 每项经 `Test-RuntimeCommandProbe` `:461-484` 校验 command 字符串非空, before/after 完整快照严格 unchanged, submitted 或 synchronousSubmissionFailure 至少一项, settled=true, timedOut=false, cancelled=false, expectedFormsUnavailableRestart=true, faulted 或 sync 为真, exceptionType/exception 非空且文本同时含 Forms unavailable 与 restart. 与 source `:347-385,699-846,1255-1291` 字段一致. 不判 P1.
- [裁决3 - 2 action 原始证据] r31 `:608-649` 对 runtime-unselected 精确取 strikeProbe/tranquilityProbe, 校验 label 与 card entry, 每项经 `Test-RuntimeCardRun` `:485-523` 校验 state=Finished, status=completed, completionTaskOutcome=completed, cancelled=false, 无 exception/failure, cardLeftHand=true, cardPileAfter 非空, observedByCardReference=true, Started/Finished 计数真实增量, 且 before/after formModeSelected=false 与 carrier/effect 数组为空; 再校验 Strike 真实能量支付 (after<before) 与 damage 等于本次计算期望, Tranquility raw stance Wrath->Calm. 与 source `:408-536,959-1114,1361-1382` 字段一致. 不判 P1.
- [裁决4 - typed 数组保形状] r31 `:84-87` 的 `Test-NonNullArray` 要求值为 `System.Array`; `Evidence-Property` `:78-83` 用 `return ,$property.Value` 避免展开; `:610-614` 对 entryCarrierTypes/entryEffectTypes 要求非 null 数组且 Count=0; `:629-631` 对 unexpectedPowerTypes 同; `:403-406` 对 fault 四数组同; `:440-444` 对 map/string 数组同. 与 source `:461-465,1295-1298` 的 string[]/object[] 输出一致. 不判 P1.
- [裁决5 - scenario/final raw fault 分流] source `:187-209` 先保存 scenario 四数组, 再以 faultsAtStart=0 重算 final; r31 `:589-595` 要求 scenario 与 final 各自 raw==expected 内部一致, finalRaw 含全部 scenarioRaw 且逐对象相同. 因 final 文档本身经 Test-RuntimeFaultFields 强制 raw 集合==expected 集合, scenarioRaw 子集关系已传递到 final expected, 不存在 expected 集合错配放行窗口. 不判 P1.
- [裁决6 - final drain/cleanup/phase/exit fail closed] r31 `:551-557` 要求 finalEvidencePhase=post-quit, quitDrainSettled=true, quitDrainOutcome=settled, cleanup=completed, cleanupDrainSettled 若存在必须 true, cleanupFailure 必须不存在; `:548-550` 要求 exitCode 整数 0; `:546-547` 要求 status=completed 且 passed=true. source `:146-251` 与 `:614-686` 在 drain/cleanup/report write 失败时设置 false/失败字段或 status failed/exit 1, 因此无法同时满足 r31 的上述断言. 不判 P1.
- [裁决7 - metadata] r31 `:558-585` 校验 productionIdentity.loaded/passed=true, location 必须在隔离根内且精确等于当前 staged `mods\Forms\Forms.dll`, sha256 精确等于本次 `$FormsPayload\Forms.dll` 的 hash; guardBefore/guardAfterShutdown 逐字段校验 owner=Forms.FormStanceSafety, prefixCount=1, prefixOwner, prefixPatchType=Forms.FormsCode.FormStanceSafetyGuard, prefixMethodName=RemovePrefix, removeMethodIdentity 非空且前后同一, failure 不存在; ownerCountsBefore/AfterShutdown 均 Forms=0/Bridge=0/Safety=1. 与 source `:569-612,1123-1203,1309-1348` 字段一致. 不判 P1.
- [裁决8 - 旧 5 模式/隔离/共享/无窗口/日志/保留 mod] r28 与 r31 逐行对比: r28 `:1-383` 与 r31 `:1-383` 完全相同; 仅 r28 `:384` 的 `Evidence-Property $e 'failure'` 在 r31 改为 `Property $e 'failure'` (正确处理成功时无 failure 键); r28 `:625-718` 与 r31 `:653-746` 偏移 28 后逐字节相同, 覆盖 Test-Evidence 旧分支与主循环. r31 `:13-28` 保留 Assert-Under/reparse deny, `:697-711` 保留隔离目录/共享 config 快照/staging, `:712-715` 保留无窗口/日志重定向/SPIRE1_FORM_SMOKE_REPORT 隔离目录, `:738-741` 保留 staged-mods-retained 与 reparse 检查, `:745-746` 保留共享快照与汇总. 旧 5 模式主体与保留 mod 语义未变. 不判 P1.
- 最终静态结论: SUPERVISION_PASS (static). 本结论仅指 r31 parser 相对 r28 冻结实现与 r29 冻结 source/RUNTIME-SAFETY.md 的静态一致性; 不覆盖实机 JSON, 构建, 退出码, 主线程记录, 真实 faults, 真实 drain/cleanup.

## 进行中
- 无. 四个检查面均已落盘并完成静态裁决.

## 未知
- 未运行游戏, 未构建, 未 lint, 未测试, 未部署; 本批明确禁止, 实机验证归 hub.
- 未用真实 `forms-runtime-safety.json` / `forms-runtime-unselected.json` 复核运行值; 静态结论不替代实机证据.
- `removeMethodIdentity` 精确字符串格式, `productionIdentity.location` 实际加载路径, 真实 fault sequence/expected 集合, 真实 cleanup/quit 字段仍待实机报告确认.
