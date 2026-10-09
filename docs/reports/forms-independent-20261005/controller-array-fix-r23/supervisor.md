# r23 监督报告 - 本轮门禁后静态监督

- 状态: REVIEW_IN_PROGRESS
- 记录时间: 2026-10-05 11:12 +08:00
- worker id: 01a109ed-ebd7-7df2-812b-28c22e7b4d8e
- 请求文件: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-array-fix-r23\supervisor.request.md
- 待监督脚本: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1
- 唯一可写文件: 本 supervisor.md
- 请求指定模型/路由: global:deepseek-v4.1-flash / wb2api. 未自行选模型/切换路由/fallback/委派; 实际路由仍须主 hub 会话元数据证明, 不以请求文字冒充实证.

## 已确认
### 2026-10-05 11:30 本轮门禁核对

- 已读取本目录 gate-message.txt 与 gate-notice.json; 用户明确通知开始 r23 静态监督.
- gate-notice.json:2-7 记录 CheckedAt=2026-10-05T11:29:48.7807759+08:00, NativeTool=multi_agent_v1.wait_agent, Worker=01a109ed-ebd7-7df2-812b-28c22e7b4d8e, Status=completed, TimedOut=false, WorkerResult=CODE_COMPLETE. 不沿用 r22 门禁, 不自行调 peer/thread 工具.
- 仅审该 gate 冻结的 r23 控制器与 phase3 schema; 不读取进行中的 phase4. 下方等待条目保留为 11:12 历史快照, 不代表当前进度.

- 已读取本轮 supervisor.request.md, 仅记录等待与监督契约. 末尾三项窄修范围替代早先仅数组范围; 门禁后监督限 10 分钟.
- 用户说明 r22 已由主 hub 真实 wait 收割 SUPERVISION_NEEDS_REWORK, 数组拒真已中央纯函数复现. 这是上一轮记录, 不是 r23 最新完成门禁; 本会话本轮没有重跑复现.
- r23 开审必须由主 hub 真实 multi_agent_v1.wait_agent 返回上述 worker 最新一轮 completed, 并落盘本轮 gate-notice, 随后 send_input 通知. 不沿用 r22 completed 或 peer 状态观察.
- 本轮尚未读取 r23 产物/差异/hash, 未作代码审核或通过结论. 没有检查或轮询门禁文件, 等待主 hub 明确通知.
- 本轮只写本文件, 不再写 r22 原报告; 未使用 peer/thread 工具, 未委派/其它 harness/构建/lint/测试/运行/git/部署/共享配置写入/C: 写入.


### 检查面 1: 数组边界窄修 - 静态通过

- 冻结字节核对: r23 SHA256=AF007F9F785AA4877377B3A94374F33D6B9CDEACBDB9271D709B99E18272B049; BindingLossSmokeRunner.cs=DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A; FormNativeSmokeRunner.cs=4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D; LifecycleSmokeRunner.cs=3457BCDED4BED87C2815822D002F57E9BFE74655550726218CA0FF33269B8CCA. 四者均与本轮 gate-notice.json 相符, 未读取 phase4.
- 代码证据: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1:76-84. Evidence-Property 明确拒绝对象/属性缺失, 使用 return ,$property.Value 保留原数组对象, 不以 @() 将 null/scalar 伪装为数组; Test-NonNullArray 随后拒绝 null/scalar. empty/single/multi 保真, 空 unexpected 与单 target 不再被原 Property 的输出枚举破坏.
- proof/raw/expected/final/lifecycle 新数组均经该 helper 获取 (r23:129-130,146-147,170-174,285-286,316-319). Test-ActionRejectionEvidence:166 也以非枚举方式返回 proof 数组. 原 Property 未修改, 不扩改其它模式.
- 中央历史证据: G:\omp works\.tmp\forms-independent-20261005\controller-r22-array-shape-repro.json 记录 2026-10-05T11:03:39.6158612+08:00 的旧 r22 哈希 1B8865E6F1AD826B250923727B639807E3383493C2CB04A2C4CC7A2130C454E1, empty/single Match=false, EmptyAndSingleArrayBugConfirmed=true. 仅确认旧回归; 不冒充 r23 修后夹具通过.
- 只读复核: Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1' | Select-Object -Skip 75 -First 30. 本会话未执行函数/Parser/夹具/主体.
- 边界: r23:256,260 为时间字符串保真使用 ConvertFrom-Json -DateKind String, 需要 PowerShell 7.5+. 本轮不安装或调整宿主.
- 尚缺证据: r23 中央正反夹具 (array empty/single/multi/null/missing/scalar) 与授权实机. 本面仅静态通过, 总结论未定.


### 检查面 3: lifecycle 精确身份与两个旧 owner 0 - 静态通过

- r23:310-327 三份 safety evidence 必须为对象; owner 精确 Forms.FormStanceSafety, bool proof true, prefixCount 为整数 1, targets/types/methods 为保真数组且各恰 1.
- r23:322-325 使用 Ordinal 精确匹配 target= MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power), prefix 类=Forms.FormsCode.FormStanceSafetyGuard, method=RemovePrefix. 没有继续使用子串/大小写不敏感正则或仅比较三个错误快照的办法.
- 权威格式已逐行对照: 冻结 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs:328-351,374-378 使用参数 FullName 与真实参数名编码, :350-351 分别输出 prefix 类型与方法名称, 不是拼接声明. 引擎 G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291 确认 Remove(PowerModel? power); 生产 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs:7,57-67,101-112 确认命名空间、target 与 prefix.
- r23:329-331,363-367 比较三快照的 Target/PatchType/Declaring/Owner/RemoveMethodIdentity 全等且恰一 prefix. removeMethodIdentity 必须非空; 原门禁 bridgeBound/不叠补丁/Terminal/两次 Shutdown/survivesShutdown 继续保留 (:347-354).
- r23:355-360 读取 ownerPatchCountAfterShutdown 的 Forms 与 Forms.FormStanceMode.Watcher, 各要求实际整数 0; missing/null/scalar/type错误/非0 均不通过. 与冻结 LifecycleSmokeRunner.cs:181-185,299-300 的真实输出相符, 不只信总数或顶层 noResidual bool.
- 只读复核: Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1' | Select-Object -Skip 309 -First 58. 未调用任何 prefix/解析函数/游戏.
- 尚缺: 中央正反夹具与授权真实 Harmony/Shutdown 证据; 此面静态通过不代表实机通过.

### 写集边界独立核对 - 已确认

- 单字节映射逐字节比对 r22 -> r23: 第一段 (r22/r23:1-75) 全等; Test-Evidence 到 lifecycle 分支前 (r22:251-259 / r23:333-341) 全等; 主体从 }if($CaseIds.Count) 到末尾 (r22:283-336 / r23:372-425) 全等. 三处比较均 True, 两文件均无 UTF-8 BOM.
- 因而本轮未改变原 Property/saveguard/effects/分发/模式/启动隔离/路径与 reparse 拒绝/无窗口/G:环境/共享配置只读hash/日志排空/安全 Move 逻辑, 未扩 core probes 或增加运行开关.
- 本轮命令宿主只读版本信息为 PowerShell 7.6.5, 满足 -DateKind String 所需 7.5+; 未执行 ConvertFrom-Json 解析函数正反测试或安装/调整工具链.


### 冻结 schema 的读取边界更新

- 前两次 schema 读取均先对同一份 ReadAllBytes 缓冲做 gate SHA256 核对, 相符后才输出源码; 上面的检查结论只针对已核对的冻结字节, 不代表后续工作树状态.
- 补读 FormNativeSmokeRunner.cs 的剩余状态 helper 前, 相同保护检查发现其字节已不再匹配本轮 gate 的 4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D. 命令抛出 SCHEMA_HASH_CHANGED_SKIP_PHASE4, 没有解码/输出/审核这些变化内容; 没有读取 phase4 请求/报告或调用 peer 工具.
- 不把该在途变化当 r23 缺陷, 不据它改写冻结 schema 的已确认部分. 只可使用已读取的相符片段或已完成 phase3 的冻结快照补足剩余核对.

## 进行中

- 无主动等待进程或轮询. 落盘后直接返回, 等待主 hub 最新完成门禁及后续审核通知.
- 门禁后的三项核对清单:
  1. 数组保真: empty/singleton 保留数组类型, null/missing/scalar 仍拒绝; 结合中央 array-shape 证据与源码, 不当作实机.
  2. 关联分类: proof 内及跨 action sequence 不重复; raw/expected 同 sequence 的 exceptionType/exception/observedUtc 全等并与 raw 文本关联; final 同强度逐项校验, 拒绝重复 raw/无效 expected. 两 action 使用真实 label/cardEntry, 逐分量验证 settled/无变化. roots 数量精确对应两个 action, 不当 raw event 数; 一 action 可按真实 identity 派生多个唯一认领 event.
  3. lifecycle: 以完成的真实输出格式核对精确 Remove/单参数/Forms.FormsCode.FormStanceSafetyGuard.RemovePrefix 及生产类身份; ownerPatchCountAfterShutdown 中 Forms 与 Forms.FormStanceMode.Watcher 都必须为整数 0.
- 其它模式/隔离/路径/配置/hash/Move 不改, 不扩 core probes 模式或运行开关.

## 未知

- r23 实际修复与指纹尚未覆盖; 等本轮门禁后才核对 r22 -> r23 窄 diff/hash.
- 请求声明 phase3 已 CODE_COMPLETE 且有 phase3-gate-notice.json, 并给出 BindingLossSmokeRunner.cs SHA256=DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A, FormNativeSmokeRunner.cs SHA256=4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D. 这些本轮只作为请求记录, 尚未独立读取/核对完成的 schema 或门禁文件.
- 本轮没有解析夹具或实机证据. 最终监督结论未定, 不提前给出 SUPERVISION_PASS / SUPERVISION_NEEDS_REWORK.