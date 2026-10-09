# r21 静态监督报告

## 已确认

### 门禁和授权
- 审查开始: 2026-10-05T11:11:52+08:00. 最迟结束: 2026-10-05T11:21:52+08:00.
- gate-notice.json 记录 worker 01a109f7-d41f-7a93-822f-53fee6f9f8bd 经 multi_agent_v1.wait_agent 返回 completed, TimedOut=false, ArtifactState=PARTIAL_CODE_COMPLETE; CallId=Unavailable. 用户已明确授权门禁后静态审核. 本监督未自行调用 peer 工具, 未把门禁视作代码完整或实机通过证据.
- 只写本报告; 不委派, 不启动其它 harness, 不构建/运行/测试/git, 不改代码/安装/共享配置, 不写 C:.
- 请求中模型为 global:deepseek-v4.1-flash, 路由 wb2api; 本监督未取得实际会话模型/路由元数据, 不将请求文字冒充实际路由证据.
### 原始等待记录
- 时间: 2026-10-05 (Asia/Shanghai)
- 状态: 已落盘等待; 尚未审代码, 尚未调用 peer/thread 工具, 尚未构建/运行/测试/git.
- worker id: 01a109f7-d41f-7a93-822f-53fee6f9f8bd
- 门禁: 仅 hub 原生 wait completed 且 gate-notice 落盘后, 再由 hub send_input 唤醒审核.
- 审核范围: 按 supervisor.request.md 执行, 不提前审代码.

### 独立字节/hash核对
- 首次门禁后检查使用 PowerShell Get-Item/Get-FileHash SHA256 独立读取, 非采用 worker 口述. 精确结束快照时间见收尾记录.
- G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs: 43134 bytes, SHA256 1F57F1FD31C2F069EBF0BC87C38EB408159B6B3151A6BCD34E04B00E04C8CB43, 与 gate-notice.json 一致.
- G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md: 4037 bytes, SHA256 AEC2DE98781D6A5E3296F0068149120B3C03D07B3BBFEF83D42BBE33C5D97FA4, 与 gate-notice.json 一致.
- worker.md:26 自认 productionAssemblySHA256/location 未显式写入, PARTIAL_CODE_COMPLETE. 该缺口已登记, 待直接核对代码后形成阻断项, 不把未编译或报告落盘叫作通过.
### 风险 1 [P1]: 新增源码使用两个 file-scoped namespace, 无法编译
- 证据: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:26 与 :38 同一文件分别声明 namespace FormsNativeSmoke.Patches; 与 namespace FormsNativeSmoke.Run;. C# 一个编译单元只允许一个 file-scoped namespace, 本结构违反语言约束, 预期诊断 CS8954. 这是静态确定的语法阻断, 本监督没有构建来获取真实诊断输出.
- 触发条件: hub 将此新增文件纳入 FormsNativeSmoke.csproj 的常规 Compile 输入.
- 宣称/契约: worker.request.md:44,47 要求同程序集可执行的新增 patch 与 partial runner, 不能 flag 以外使用 stub; worker 只可称 PARTIAL_CODE_COMPLETE.
- 当前控制流: patch 在第一个 namespace, runner 试图在第二个 namespace, 编译器在进入运行时之前即拒绝; 所有真实 probe 均未成为可运行路径.
- 静态核对命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs' -Pattern '^namespace .*;'. 本轮不执行 build/test.
- 最小修复范围: 仅新增 RuntimeSafetySmokeRunner.cs, 改为两个 block-scoped namespace 并保持原有类的 namespace 和可访问性; 其它 API/私有 helper 的准确性继续核对.
- 尚缺实机证据: 修复后的真实编译输出与两场景 JSON, 当前无任何实机通过证据.
### 中央诊断更新
- 用户新增证据: core r21 已真实诊断编译失败, 0 warnings / 8 errors. 权威日志 G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r19-phase3-r21-diag.log. RuntimeSafetySmokeRunner.cs:38 为 CS8954, 其余 private partial helper 类型不可见为 CS0246. 原始日志待立即只读核对.
- 当前阻断: NEEDS_REWORK, 不得通过或部署. hub 显式 Compile exclude 此 PARTIAL 文件后的 r19 phase3 构建不包含 core r21, 不得被称作 core 构建通过. 本监督没有自行构建/测试或修改源码.

### 风险 2 [P1]: 生产身份/强制证据不参与成功门禁, 无报告仍可 exit 0
- 证据: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:654-662 的基础 result 无 productionAssemblySHA256/location; 全文件未见此字段赋值. :289-293 与 :408-414 仅从局部 probe 布尔值设 passed. :178-179,237-238,322-323,372-373 仅记录 guard/owner metadata, 未要求前后精确 prefix=1 或旧 owner=0. :117-118 在报告写入前已定 exitCode, :664-694 的 writer 在缺环境/非 G: 路径/写入异常时只记录 reportWriteFailure, 不改 passed/exitCode, :135-160 仍可主线程 Quit(0) 并标 completed.
- 触发条件: 局部 probe 表面满足, 但生产 assembly 身份缺失, guard metadata 不符, 或 SPIRE1_FORM_SMOKE_REPORT 缺失/非法/不可写.
- 权威契约: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-safety-probes-r21\worker.request.md:51,59,62 要求生产 hash/location, owner 精确数量, 原始证据完整, 无报告必须失败; RUNTIME-SAFETY.md:54-61 同样宣称.
- 当前控制流: 身份字段完全未建立; metadata 是旁路数据, WriteRuntimeSafetyJson 为 void 且吞掉失败, 不提供可用于拒绝成功的写入结果. 不能将该 PARTIAL 实现判为证据完整.
- 静态核对命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs' -Pattern 'productionAssembly|reportWriteFailure|prefixCount|ownerCounts|exitCode'. 只读源码证明, 不是实机复现.
- 最小修复范围: 仅新增 runner/doc, 读取实际已载入 Forms assembly 的 Location/字节 SHA256 并校验为必填证据; 将精确 guard/owner, 每步完整证据, JSON 成功写入纳入统一失败关闭门禁, 缺失和异常只允许 failed/exit 1. 暂未执行任何修复.
- 尚缺实机证据: 两场景真实 JSON 含匹配的生产 location/hash; 非法或不可写报告目录确实失败退出; 稳定 guard 与旧 owner 数量的实际校验.
### 权威诊断日志和真实 API 已核对
- G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r19-phase3-r21-diag.log:3-10 为 8 条真实错误, :22-23 为 0 warnings / 8 errors. CS8954 在 RuntimeSafetySmokeRunner.cs:38; CS0246 分别在 :165,:310,:431,:433,:477,:491,:524. :14-21 是同一批诊断的摘要重复, 不能计作 16 errors. 这是中央编译证据, 非本监督运行.
- 原 partial helpers 确实存在于 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1758,3570 和 BindingLossSmokeRunner.cs:671-703,1058-1120,1196-1201. CS0246 与错误的 namespace/partial 归属相符, 不据此捏造 helper 缺失. 修正 namespace 后仍需中央重编译确认其它诊断.
- 引擎权威 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:1137 确认 StartNewSingleplayerRun 返回 Task<RunState>, 新代码 :193/:337 类型匹配.
- G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:105,:219,:291 确认 Apply/ModifyAmount/Remove 签名与新代码 :226,:272,:241 匹配; :231 先 Hook.BeforePowerAmountChanged, :241 才 SetAmount; :295 才 RemoveInternal.
- G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\CreatureCmd.cs:180 确认真实 enemy dealer 的 Damage overload; :285 先 BeforeDamageReceived, :287/:293 才 block/HP mutation. 新代码 :257 为真实引擎调用, 未直接调用 guard prefix 或 override/mock. 这些是源码契约证据, 不等于实际 patch 安装或运行成功.
### 风险 3 [P1]: command 未 settled 时继续复用战斗, drain/cleanup 失败未彻底失败关闭
- 证据: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:488-551 两个 CaptureRuntimeCommandAsync 在 AwaitOperationWithTimeoutAsync(... terminalOnFailure:false ...) 超时后 catch 并返回 settled=false 的 evidence; :241-257/:272 之后无 settled 检查, 仍取 after 快照并派发下一个真实 command. 权威复用 helper G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:3629-3642 说明此超时只观察/登记 DetachedOperation, 没有取消底层 command.
- 触发条件: 任一 Remove/Damage/ModifyAmount 真实 Task 或 main-thread submission 超过 30 秒/主线程 gate 期限仍未完成. 下一探针在同一 combat 上运行, 前一底层命令仍可能延迟落地.
- 权威契约: worker.request.md:56,62 要求 timeout/pending/cancelled 失败, 任一 command 未 settled 不得后续复用被污染战斗; 不得未 settled 就 cleanup 或宣称 passed.
- 当前控制流: :562-568 的 expected 判断仅匹配 exception 文本, 不要求 faulted/settled, 同步 prefix 拒绝也始终留下 settled=false; 必须据实区分同步 submission 已结束但无 Task 与真正 pending. :508/:516/:541/:549 在分类前将全部异常树放入 expectedRejections, 不是仅允许已确认的 Forms unavailable/restart 拒绝.
- drain/cleanup 同面证据: :438-467 drain 不 settled 只将 cleanup 标 skipped, cleanup 异常只写 failure; :469-474 只有 unobservedFaultGate 的 false 会改 passed. 初次 drain 后新增的 final-cleanup detached operation 未再次 drain. :130 在 Quit 和 post-quit drain 之前退订 fault 事件; :156-159 最终失败仅改 status, 未统一回写 passed=false/exitCode=1, quitOperations 的 settled 也不能证明先前 combat operations settled.
- 静态核对命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs' -Pattern 'terminalOnFailure: false|settled|cleanupAllowed|UnobservedFault -=|expectedRejections|quitDrainSettled'. 本轮不通过运行复现 pending.
- 最小修复范围: 仅新增 runner, 分类并保留已确认拒绝的原始异常; 若 command/submission pending 则终止后续 probe, 有界 drain 后决定仅安全 cleanup/隔离退出; cleanup 新操作必须二次 drain; 监听保留至最终 drain 完成, 任何 drain/cleanup/报告失败统一 passed=false/非零退出. 不改变生产语义来变绿.
- 尚缺实机证据: 真实 pending/late fault/cleanup gate 失败时不派发下一 command, 不清空污染战斗且非零退出, 最终 report 保留所有 raw fault 和两阶段 settled 证据.
### 普通局控制与原始证据检查面完成
- 真实控制: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:337-339 使用 Array.Empty<ModifierModel>() 创建普通局, :351 真实 Crescendo, :362-365 依据 raw marker 判 Wrath 且 carrier/effect 空. 生产 FormStanceMode.cs:20-21 由 RunState.Modifiers 决定选中; :52-79 的 fail-closed 只拦当前 live 的选中 Forms combat. FormStanceSafetyGuard.cs:112-119 对非 Forms 局通过该 gate 后允许真实 Remove. 未使用 mock 或直接 prefix 代替 ordinary control.
- Watcher 权威: G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherStrike_P.cs:17,20,24-29 确认基础 DamageVar 6, 1 费, 真实 DamageCmd.Attack; WatcherTranquility.cs:24-31 确认 1 费与真实 EnterCalm; Wrath.cs:39-52 确认满足其 props/dealer 条件时 2m 倍率. 新代码读取 DynamicVars 而非写死 12, 但 :379 只 base*2, 没有证明力量/其它倍率状态满足 expected 的前提.
- 风险 2 的普通局接续: 新代码 :361 仅记录 selectedBefore, 未要求 false; :377/:392 调真实 PlayBindingLossCardAsync, 该 helper G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:718-736 在每次调用中 CreateCard/Add, 因此 Strike/Tranquility 的 card fixture 发生在 :368 Shutdown 之后, 不满足失效前预备全部 card fixture 的契约. 未见失效后 GainEnergy, 能量 fixture :367 在 Shutdown 前, 此面不能误报为失效后补能.
- history/payment 接续: Snapshot 的 watcherStrikeHistory 确有真实 started/finished/EnergySpent 证据 (FormNativeSmokeRunner.cs:1852,1943-1965), 不是无 history 输出. 但新代码 :408-410 成功门禁未比较 history finished delta 或支付, Tranquility 也未采自身 card history. helper :828-832 的 Passed 不要求 :823 的 actionExecutedAndSettled, 仅 success/非 Hand pile/no fault. 当前确为真实 PlayCardAction enqueue, 不能说它只等 CompletionTask; 也不能说新门禁已满足完整真实出牌与支付证明.
- full-name amounts 接续: Snapshot 的 power 类型全名存在, 但 FormNativeSmokeRunner.cs:1790-1794 的数值字典按 shortName 键, 不符合逐 power full-name amounts 契约; raw stance marker 自有 full-name amounts (BindingLossSmokeRunner.cs:1074-1076). 新代码 Remove 仅 marker presence, Damage 仅 HP/block, ModifyAmount 仅 Strength equality, 未统一核对完整 HP/block/energy/所有 powers/marker 无变更; 缺数值 reader :1921-1929 返回 0 可能形成未知=未知的伪相等.
- 最小接续仍属风险 2: 预备真实卡牌且失效后只 enqueue 已备卡; 以每张真实 history/energy/marker 和精确 full-name amount 对比为门禁, expected 依据当时真实状态或明确排除其它 modifier. 不改变生产支付语义, 不把只测核心 command 当作已证明选中局外层出牌支付前防线.
### 主线程/入口/G:和成本检查面完成
- 风险 3 的线程接续: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:174-175 与 :318-319 在 await startup 后直接调用 FormStanceWatcherBridge.TryBind, 未通过已知主线程 gate. 生产 FormStanceWatcherBridge.cs:180-200 会 ValidateBinding/装 Harmony 并 StartPumpLocked; :411-435 会访问 Engine.GetMainLoop/SceneTree.Root/AddChild. 原 InvokeOnMainThreadAsync helper (FormNativeSmokeRunner.cs:3445-3472) 明确检查 NGame.IsMainThread 并使用 RunContinuationsAsynchronously 的 TCS. await 本身不提供已知主线程证明; 重试/首次绑定的 Godot 路径未受新 runner gate 保护. :230 直接读 strength.Amount, :376 直接读 DynamicVars 也未从 gate 返回值快照. 这是源码边界缺口, 没有实测断言当前线程一定是工作线程. 最小接续: 对 TryBind 和需读取的真实对象状态也走主线程 gate, 并记录 thread 证据.
- 正向证据: :181-231/:325-350 的主要 run/ModelDb/encounter/命令提交和 snapshot, :233/:368 的生产 Shutdown, 以及复用 QuitOnMainThreadAsync 的 SceneTree.Quit 均走主线程 helper; 主线程保障不是完全缺失. WaitWithTimeoutAsync 确实等 GameStartupComplete 的真实 Task.
- 真实 Cubex 来源 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Encounters\CubexConstructNormal.cs:5-16, 全名与 Monster room 相符. 固定种子来自原 runner :31, shouldSave=false. 无 Spire1/Watcher 编译类型引用, Watcher 查找按完整类型名, 未采用 fake scheduler.
- 无参数边界: 新入口 :59-65 在两开关均缺失时直接 return, 尚未置 started 或订阅 fault/建 run/Shutdown/Quit/写 JSON. 原 test MainFile.cs:74-89 扫本测试程序集所有 HarmonyPatch 类, 新 patch 会被纳入, 但当前编译失败使运行时边界尚不可验收. 新文件未改旧 runner 源码; 本监督只确认本次读到的路径, 不把未做 Git diff 的检查称作全部旧文件无变更证明.
- G:边界: writer :675-680 做 Path.GetFullPath 后只接受 G: 前缀, 无 C:/Steam/config 写入代码. 但 reportWriteFailure 没有失败关闭, 已归风险 2; 是否存在目录 junction 和实机 APPDATA/GSE 隔离由 hub 的独立部署/运行验收负责, 本轮未执行.
- 字节/线程成本: 新 .cs 43134 bytes/696 lines, 文档 4037 bytes/68 lines. 每场景记录 2 次 guard prefix 元数据和 2 次全局 Harmony.GetAllPatchedMethods owner 扫描 (O(全局 patched methods + patches)), 非逐帧循环; JSON 同步写两次且每次完整序列化 result. capture 保留 exception 树/raw 文本, snapshots/history 反复拷贝但探针步骤固定; 未测主线程耗时、内存峰值或性能, 不作性能通过结论.
### 收尾与监督结论
- 结论: NEEDS_REWORK. 已确认 3 项 P1 风险, 不增加无证据第四项. PARTIAL_CODE_COMPLETE 不是 CODE_COMPLETE, 更不是构建/实机通过; 当前不得部署.
- 收尾时间: 2026-10-05T11:20:12+08:00; 自 2026-10-05T11:11:52+08:00 起用时 500.3 秒, 未超过 10 分钟.
- 结束快照: 两份新增文件再次 Get-FileHash 核对与门禁一致=True, .cs SHA256 1F57F1FD31C2F069EBF0BC87C38EB408159B6B3151A6BCD34E04B00E04C8CB43, .md SHA256 AEC2DE98781D6A5E3296F0068149120B3C03D07B3BBFEF83D42BBE33C5D97FA4. 这是源码/文档 hash, 绝不替代缺失的实际 production Forms.dll SHA256/location.
- 中央诊断原件 SHA256: 8C566B013AC3D355E3A9CCBA0DEE1DC540B33DE8C6B0132CF045E4A80F349F74; 权威计数以 log:22-23 的 0 warnings / 8 errors 为准. 本监督只读取, 没有运行任何编译.
- 唯一写入文件: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-safety-probes-r21\supervisor.md. 未修改源码/工程/生产/配置, 未使用 peer 工具/委派/其它 harness, 未构建/运行/测试/lint/git.
- r19 phase3 在显式排除 r21 PARTIAL 文件后的构建或验收是另一集合; 不作为 core r21 通过证据. core 需上述最小返工、中央重新纳入编译及两独立实机证据门禁才能重新送审.
## 进行中
- 本次有界监督已结束, 无后台任务或主动 peer 等待. 待 hub 收割风险 1-3 后决定返工, 此报告不授权自动修改或运行.

## 未知
- core 新代码尚未成功编译或运行; 中央实际编译失败已明确记录, 只作诊断证据. 未取得两场景真实 JSON/退出码/主线程记录/原始 faults/全量 no-mutation/最终 settled drain, 不宣称任何实机通过.
- 缺 actual production assembly SHA256/location, 当前代码本身没有身份字段. 本监督没有以磁盘候选 DLL 或源码 hash 补造实际已加载身份.
- 选中局外层真实 card/action 的支付前拒绝, 真实 end-turn, UI, 长战斗, 读档, 多人, 真热替换和性能未覆盖, 不能由三条核心 command 推定通过.
- 真实模型/路由会话元数据, 目标 binary 与本地权威源码的逐字对应, APPDATA/GSE 隔离和物理写入路径/junction 边界均未验证. 请求所写模型/路由不作为实际元数据证明.