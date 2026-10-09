# supervisor.md

状态: 等待门禁
记录时间: 2026-10-05 11:46:19 +08:00

## 已确认
- 已读取 supervisor.request.md 与 worker-id.txt。
- worker-id: 01a10a2a-1f5f-7a42-851b-98a69e20f230。
- 尚未收到主 hub 在 native wait 最新 worker completed 后落盘的 gate-notice.json 与 send_input 通知。
- 因此未开始独立审核，未读取任何在写源码、产物或候选差异。

## 进行中
- 无。等待主 hub 完成 native wait 并发出门禁通知。

## 未知
- 最终冻结字节与 hash。
- 权威契约独立重审结果。
- 错误模型候选 r23 重新审修是否完整。
- 旧主体区域及文件 hash 是否未变。

## 等待规则
- 收到 gate-notice.json 与 send_input 通知后才开始审核。
- 不使用 peer 工具或文件轮询冒充 native wait。
- 不提前审代码，不继承任何旧 PASS，不构建/测试/再委派。

---

## 门禁启动记录 (2026-10-05 11:54:53 +08:00)
- 已读取 gate-notice.json 与 gate-message.txt。
- native wait 返回 worker=01a10a2a-1f5f-7a42-851b-98a69e20f230, status=completed, TimedOut=false, WorkerResult=CODE_COMPLETE。
- gate-notice 记录 r24 SHA256=E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1。
- 现在开始冻结解析器静态监督；不构建/测试/peer/委派/其它harness；不继承旧PASS。

---

## 检查面 A: 冻结字节与写集边界 (2026-10-05 12:01:28 +08:00)

- 冻结哈希独立重算: r24 SHA256=E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1, 与 gate-notice.json 完全一致。字节数 38596。
- 基线未改: r22 SHA256=1B8865E6F1AD826B250923727B639807E3383493C2CB04A2C4CC7A2130C454E1, r23 SHA256=AF007F9F785AA4877377B3A94374F33D6B9CDEACBDB9271D709B99E18272B049, r20 SHA256=AFBE52076154176890F6291574B63CFEF315D76729F20706AB8C176AC3F83EA2, 三者均与 worker.md 记录一致。
- 静态解析: PS 7.6.5 下 Parser.ParseFile r24 -> ParseErrors=0。仅解析, 未执行脚本主体。
- 写集边界: 逐行比对确认 r23->r24 新增仅 33 行 (L92-96 Test-NonNegativeInt; L135-154 Test-ActualCardHistory; L160 types 数组; L191 调用; L309-314 双 action actualCardHistory 循环), 修改 4 处 (L331, L345, L360, 及 L133 区域的 types 行), 其余 425 行与 r23 逐字相等 (HEAD 1-89 identical=True; TAIL r23 L372-END vs r24 L405-END identical=True)。
- 独立核对: 未改 r20/r22/r23/矩阵/测试/生产; 未扩 runtime core 模式; effects/saveguard/矩阵输入原语义未改。


---

## 检查面 B: typed 数组保真 (2026-10-05 12:01:37 +08:00)

- r24:82-85 Test-NonNullArray 要求 `-is [System.Array]` 且非 null; 由 r23 引入的 Evidence-Property (r24:76-81) 用 `return ,$property.Value` 保持数组/标量原形状, 不走管道枚举。
- r24:154,172,197-201,318-319,349-352 所有证据数组均经 Evidence-Property + Test-NonNullArray; empty/single/multi 保真; null/missing/scalar 被拒 (missing 由 Evidence-Property 抛 Missing evidence property)。
- 独立复算的中央夹具结果 (只读, 非本次执行): controller-r23-central-fixture-results.json 记录 array-empty/single/multi/null/scalar/missing 六项 Match=true, ScriptSHA256 与 r23 冻结值一致。r24 未改这些代码路径 (逐字相等), 但本面仅静态继承判断, 未对新 r24 字节重跑夹具。
- 结论: 静态满足任务 1。尚缺 hub 对新 r24 字节的中央重跑证据。

---

## 检查面 C: 两卡 actualCardHistory 门禁 (2026-10-05 12:10:42 +08:00)

- 契约来源: 权威 schema `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:83-104` 定义 BindingLossCardHistoryEvidence 八字段; 输出注入在 `:1121` (result["actualCardHistory"] = run.ActualCardHistory?.ToJson())。
- r24:133-152 Test-ActualCardHistory 要求: 对象; 八字段全部存在(缺字段抛 missing); card 精确 Ordinal 等于对应 run.card; cardTypeFullName 精确等于真正类型; cardInstanceIdentity 为非空 string; observedByCardReference 为 bool true; 四个计数为整数且 >=0; beforeStarted==afterStarted 且 beforeFinished==afterFinished。
- 双卡绑定: r24:309-313 对 nextStrike 期望 `WatcherMod.WatcherStrike_P`、stanceChangeProbe 期望 `WatcherMod.WatcherTranquility`; 另 r24:190 在 Test-ActionRejectionEvidence 内按 $types[$index] 再次绑定。不再使用固定 Strike 单读取器, Tranquility 独立出牌不会再被固定 Strike history 漏掉。
- 真正类型 FullName 已对照本地 Watcher 权威 source: `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherStrike_P.cs:11` namespace WatcherMod + `:13` class WatcherStrike_P => `WatcherMod.WatcherStrike_P`; `WatcherTranquility.cs:8,10` => `WatcherMod.WatcherTranquility`。与 r24 常量一致, 无差异, 无需 flag 字符串猜测。
- 结论: 静态满足任务 3。r25 源码未读(按约定实现), r25 实机输出仍待 hub。

## 检查面 D: 恰双 action / raw expected final / roots (2026-10-05 12:10:42 +08:00)

- 恰双 action: r24:156 proof 数==2; :157-160 labels/cards/types/runs 四个固定映射; :167 label 必须属于集合且 Ordinal HashSet 去重; :177 每 action 内序号唯一, 且 $sequenceOwners 跨 action 去重(重复/跨 action 认领即抛)。
- BeforeExecuted 执行: r24:118 要求 action.executionObserved==true; 权威对应 `BindingLossSmokeRunner.cs:804-807` action.BeforeExecuted 回调置位, `:855-862` 输出 executionObserved。
- settled/非取消/故障: r24:119 actionExecutedAndSettled==true; :120 state 精确 Finished; :121 cancelled 必须 bool false; :123 status 仅 faulted/failed (timeout/pending/cancelled 拒绝); :124-126 completionTaskOutcome 必须 completed 或 `faulted: ` 前缀。
- 明确重启分量: r24:170 proof.actionExecutedAndSettled==true 且 proof.explicitFormsRestart==true; :171 actionFaultRootType 非空。权威 `:1104-1106` explicitFormsRestart = 有显式拒绝且故障文本提及 Forms restart。
- roots 计数=action 数: r24:315-317 expectedRejectionsExpected 必须整数恰 2; :318-320 expectedRejectionRoots 为保真数组且恰 2; :321-325 两个 root 与两个 action proof 的 actionFaultRootType 逐位精确对应。不是 events 数。
- raw/expected/final 逐 sequence 相等: r24:111-115 Test-SameFaultEvidence 要求 sequence 相等且 observedUtc/exceptionType/exception 逐项 Ordinal 相等; :218 raw 文本必须等于对应 exception; :226 expected 必须命中 raw 且同证据; :227-230 expected 序号必须有唯一 action 认领, 且所有 owner 序号必须出现在 expected。
- 无遗漏/重复/伪对象/未认领: r24:204 三段长度必须相等; :217 raw 序号不得重复; :225 expected 序号不得重复; :201 四数组均为保真数组且非 null; :215/223 每个元素必须是 JSON 对象。因 raw.Count==expected.Count 且双向包含, raw 与 expected 集合相等, 无持久化 proof 的 late 事件无法混入。
- final 对照: r24:334-338 对 final 独立重跑同一分类器, 并要求 final 与 scenario 的 raw 对象逐 sequence 完全相等。
- 无支付/HP/block/power/marker/伤害/历史变化: r24:249-274 Test-NoNativeMutation 覆盖 energy(双向)、player CurrentHp/MaxHp/Block、IsDead、ownerPowerAmounts、PowerTypes、markerAmounts、markerFullNames、raw stance、watcherStrikeHistory; :303-307 额外要求 8 个 mutation bool 为 false 且 nativeDamage/probeNativeDamage 恰为 0。
- 结论: 静态满足任务 2。

## 检查面 E: lifecycle 精确稳定 (2026-10-05 12:10:42 +08:00)

- 权威引擎: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291` 单参数 `Remove(PowerModel? power)`。
- 生产 guard: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs:22` HarmonyId=Forms.FormStanceSafety; `:57-63` 反射取单参数 Remove; `:101-104` PrefixMethod=RemovePrefix。
- r24:345 owner 精确 Forms.FormStanceSafety; :346 removeMethodFound 与 removePatchedBySafetyOwner 均 true; :347-348 prefixCount 整数恰 1; :349-353 targets/prefixPatchTypes/prefixDeclaringMethods 均为保真数组且各恰 1。
- r24:355 精确 target 串 `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power)`; :357 前缀类型 `Forms.FormsCode.FormStanceSafetyGuard`; :358 方法 `RemovePrefix`。权威编码 `LifecycleSmokeRunner.cs:376-380` (FullName + 参数 FullName + 参数名)。
- r24:359 removeMethodIdentity 非空; :362-364 三快照 Target/PatchType/Declaring/Owner/RemoveMethodIdentity 逐项 Ordinal 相等且 prefixCount 恰 1(前后稳定)。
- r24:390-394 ownerPatchCountAfterShutdown 的 Forms 与 Forms.FormStanceMode.Watcher 必须整数恰 0; 顶层 bool 不能替代。
- 结论: 静态满足任务 4。

## 检查面 F: 旧主体区域与文件 hash 保持 (2026-10-05 12:10:42 +08:00)

- HEAD 逐行: r23 L1-89 与 r24 L1-89 逐字相等=True。
- TAIL 逐行: r23 L372-END 与 r24 L405-END 逐字相等=True (54 行)。
- 写集核对: r24 相对 r23 新增 33 行、修改 4 处(L331 PrefixCount 加 [long]、L345 lifecycle 加 -DateKind String、L360 count 加 [long]、L158/160 types 行); 未触及 r20/r22/r23/矩阵/测试/生产/隔离/路径 reparse deny/无窗口/G: 环境/共享配置 hash/日志排空/Move 主体/effects/saveguard 原语义。
- 基线 hash 未变: r20=AFBE5207..., r22=1B8865E6..., r23=AF007F9F... 与 worker.md 一致。
- 结论: 静态满足任务 5。

## 旁证: hub 新字节中央夹具 (非本次执行, 只读复核)

- `controller-r24-central-fixture-results-r2.json`: ScriptSHA256=E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1(与本轮冻结字节一致), Results=64, AllMatch=True。
- `controller-r24-central-fixture-results.json`(首轮): 同一冻结 hash, Results=64, 1 项不匹配 binding-positive, detail=`Test-NonNegativeInt is not recognized`。经核对这是夹具宿主自身缺陷: `test-controller-r24-fixtures.ps1:12` 的 $allowed 名单漏登记 r24 新增的 Test-NonNegativeInt; r2 在 `:12` 补入该名后全部 64 项通过。属 hub 夹具工具问题, 不是 r24 脚本缺陷, 不构成 NEEDS_REWORK。

## 未完成/未知

- 未执行 r24 脚本主体、未跑游戏、未做真机/授权实机验证 (按请求由 hub 集中执行)。
- 未读取 r25 源码; actualCardHistory 仅按约定与已冻结权威 schema 核对, r25 实机输出待 hub 验证。
- removeMethodIdentity 字符串编码格式仅要求非空且前后稳定, 未额外约束编码形态。
- 未使用 peer/委派/其它 harness; 仅静态只读审查。

## 最终结论 (2026-10-05 12:10:42 +08:00)

- SUPERVISION_PASS
- 冻结字节: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1 SHA256=E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1
- 范围: 冻结解析器静态监督 + 权威契约逐项对照 + 旧主体保持; 新字节中央夹具 r2 全通过(旁证)。实机证据仍由 hub 重建, 本 PASS 不声称 AST 或实机通过。
