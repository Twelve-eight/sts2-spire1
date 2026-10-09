# supervisor (r22) - 门禁后增量监督

- 状态: SUPERVISION_NEEDS_REWORK (MATRIX_SUPERVISION_PASS; phase3 schema 门禁后对照待办)
- worker id: 01a109ed-ebd7-7df2-812b-28c22e7b4d8e
- 监督范围: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1 与 G:\omp works\.tmp\forms-independent-20261005\run-independent-matrix-r22.ps1
- 指定模型/路由: global:deepseek-v4.1-flash / wb2api (请求指定值; 本会话未自行切换/启用 fallback/委派; 实际模型与 provider 元数据未独立核验)
- 唯一可写路径: 本文件

## 已确认
### 2026-10-05 10:57 门禁已核对

- 主 hub 新指令已授权开始审核; gate-notice.json:2-6 记录 NativeTool=multi_agent_v1.wait_agent, Target=01a109ed-ebd7-7df2-812b-28c22e7b4d8e, ReturnedStatus=completed, TimedOut=false, RecordedAt=2026-10-05T10:56:22.1032537+08:00. CallId=Unavailable, 本会话只引用主 hub 通知及落盘记录, 不把此前 peer 状态观察作为监督门禁.
- 门禁授权仅覆盖两个控制器; phase3 schema 未 CODE_COMPLETE 前不得当作完成的权威契约.
- 以下旧的等待阶段条目只代表 10:43:59 的历史快照, 不代表当前状态.

- 已读取 supervisor.request.md, 监督契约已记录: 门禁前不审代码, 不自行 peer 轮询, 不再委派.
- 等待阶段历史记录 (10:43:59): 当时未收到主 hub 真实 multi_agent_v1.wait_agent 返回的 worker completed, 也未落盘 gate-notice.
- 等待阶段历史记录 (10:43:59): 因此当时尚未读取 r22 新代码, 尚未做任何 r20/r22 diff/hash 核对, 尚无监督结论.
- 本文件为唯一落盘产物; 未修改产品代码/构建/部署/游戏/共享配置, 未写 C:.


### 矩阵单面结论: MATRIX_SUPERVISION_PASS

- 核对时间: 2026-10-05 10:58:26 +08:00. 两文件用单字节编码逐字节映射到内存, 对原件做大小写敏感的字面替换后与 r22 全文 Ordinal 相等. 这是独立源码/字节比对, 未运行控制器或合成测试.
- 原件: G:\omp works\.tmp\forms-independent-20261005\run-independent-matrix.ps1, 9314 bytes, SHA256 FE4B1DAFE2032F24E1E921EBD01C7AE312FBE2B3785A4E1FF7DA900B78D7092C.
- 新件: G:\omp works\.tmp\forms-independent-20261005\run-independent-matrix-r22.ps1, 9338 bytes, SHA256 BAF24B6C270F6EEE9329713F538070AFDE55E1316009AC81207229291A477D95, 与 gate-notice.json 记录一致.
- 原件字面变量出现恰 4 次, 新件旧变量出现 0 次, 新变量出现 4 次; 变化行号: 64, 74, 81, 82. 每处只把 $error 改为 $launchError, 总字节增加 24, 不存在其它字节差异.
- 既有路径/共享配置/进程/无窗口/模式控制流未因本次矩阵修改改变; 不据此声称既有行为实机通过.
- 复核方法: 只读 ReadAllBytes, 对原件 ASCII 字节序列 $error 做精确替换后逐字节比对新件; Get-FileHash -Algorithm SHA256. 未执行任何脚本主体.
- 尚缺: 矩阵实际运行证据; 本轮禁运行, 该单面通过只证明指定窄写集.


### 检查面 1: typed array 取值存在确定的拒真风险 (源码证据)

- [P1] 路径: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1:40,76-79,121-124,130-139,197-206,227-229.
- 触发条件: 权威 JSON 的 unexpectedUnobservedFaults 为合法空数组, 或 identityMatchedFaultSequences / lifecycle targets 为合法单元素数组.
- 契约: 数组必须区分缺失/null/空数组/单元素数组; unexpected 必须为非 null 空数组, target 恰 1.
- 当前控制流: Property 的 return $p.Value 经 PowerShell 函数输出管道枚举数组. 空数组没有输出, 调用方拿到 $null; 单元素数组被拆成标量. 新增 Test-NonNullArray 随后要求 System.Array 并 throw. 原 r20:102-105 用 PSObject.Properties 的 .Value 直接保留 raw 数组, 新 binding 分类改用 Property 后暴露此错误. 此判断不依赖 phase3 未完成字段, 也没有执行脚本或夹具.
- 静态复核命令: Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1' | Select-Object -Skip 39 -First 1; 同文件 76-79 与各调用点核对. 本轮未运行解析器, 未称实机复现.
- 最小修复范围: 新增不枚举的属性取值/直接取 PSObject.Properties[name].Value, 新 array 检查点先保留原对象再检查; 不把 @() 强行包装 null 来掩盖缺字段, 不扩改其它模式的语义.
- 尚缺实机证据: 修正后由 hub 合成正反夹具验证 empty/singleton/multi/null/scalar, 再做授权实机; 合成夹具不等同实机.
- 本面结论: SUPERVISION_NEEDS_REWORK. MATRIX_SUPERVISION_PASS 不受影响.


### 检查面 2: action/raw/expected/final 关联仅做部分集合检查 (源码证据)

- [P1] 路径: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1:142-150,197-214.
- 触发条件: 在修正数组保真后, 同一 sequence 在一条 proof 内重复或被两个不同实际 action 同时认领; expected 与 raw 虽同 sequence 但 type/exception 不一致; finalExpected 含无效元素或 finalRawEvidence 重复已有 sequence.
- 契约: 请求要求 raw/expected/unexpected/action reference 关联严格匹配, 数量/sequence/raw 对应不允许丢弃或重复, 最终 drain 分类不能弱化.
- 当前控制流: proofSeqSet:142-143 将所有 action 的 sequence 压成 set, 没有重复/多 action 所属检查. raw 与 expected:145-150 仅验证两边 sequence 集合, 没有核对同一 sequence 对应的 exceptionType/exception/observedUtc 或 raw 文本关联. finalExpected:201-202 只要求 array, :208 只使用 Count, 未逐项验证对象/sequence/类型/所属 action. finalRawEvidence:211-214 未拒绝重复, 双向 set 包含仍可能掩盖数量差异.
- 只读复核命令: Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1' | Select-Object -Skip 128 -First 32; 同文件 197-214.
- 静态反例设计 (未执行): 两条 action 的 identityMatchedFaultSequences 都含 [1,2], 两个 raw/expected sequence 仍为 1/2, 当前 set 逻辑无法区分错误双重认领. final expected 给 [null,null] 而 final raw 给有效的 sequence 1/2 时, 当前代码只看到 Count=2; final 追加重复 sequence 后配相同长度 expected 时, set 双向包含也无法发现重复. 这些不是实机复现, 且当前先会被检查面 1 拒绝, 是修复该阻断后仍需处理的独立潜在误放行.
- 最小修复范围: 在分类函数内验证每条 proof 序列唯一与跨 action 所属关系; 为 raw/expected 建立带完整 typed evidence 的 sequence 字典并核对持久化关联; final 复用同强度校验并拒绝重复/无效 expected. 类型和 exception reference 派生关联的最终精确 schema 等 phase3 CODE_COMPLETE 门禁后确认, 不从在途源码造字段.
- 尚缺证据: phase3 完成后的真正 action proof/final 分类结构及 no-mutation 各分量; hub 正反夹具与授权实机 drain. 本轮未运行任何夹具.
- 本面结论: SUPERVISION_NEEDS_REWORK, 不宣称 phase3 在途字段已经完成.


### 检查面 3: lifecycle 只验证三个快照相互一致, 未绑定正确的生产 prefix 身份 (源码证据)

- [P1] 路径: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1:230-248,265-278.
- 权威契约: G:\omp works\Sts\sts2-forms\DEVELOP.md:141-144 要求 Harmony 精确目标/owner/prefix 身份, 两原 owner 为 0. 生产 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs:57-67,101-112 明确以单 PowerModel 参数的 PowerCmd.Remove 为 target, 以 FormStanceSafetyGuard.RemovePrefix 为 prefix.
- 触发条件: 修复检查面 1 后, 三份快照一致地给出错误 overload/错误 prefix 类/错误 declaring method, 或两原 owner 的残留数与顶层 bool 不一致.
- 当前控制流: target:231-232 只做默认大小写不敏感的正则开头与 PowerModel 子串匹配, 没核完整 target/参数结构. prefixPatchTypes 与 prefixDeclaringMethods:239-240 只需非空, 允许 unknown 或任意错误名称. SameIdentity:244-248 仅比较三快照彼此相等, 因此稳定的错误身份仍会被接纳. :270 只信 noResidualFormsOwnerPatches=true, 不读取 ownerPatchCountAfterShutdown 中两个精确 owner 的整数 0.
- 本轮已对照完成的 lifecycle 源码: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs SHA256=3457BCDED4BED87C2815822D002F57E9BFE74655550726218CA0FF33269B8CCA, 与 r19/phase2-gate-notice.json 指纹一致. :44-46 明确 owner 常量, :181-184 持久化 residual 计数, :346-351 持久化完整 target 与 prefix 类型/方法, 不是需要等 phase3 的新字段.
- 只读复核命令: Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1' | Select-Object -Skip 218 -First 31; 同文件 265-278; 对照上述生产声明与完成的 lifecycle 源码.
- 静态反例设计 (未执行): 三快照都写相同的 prefixPatchTypes=['unknown'], prefixDeclaringMethods=['unknown'], prefixCount=1, 正确 owner 与布尔 true; 当前身份条件不拒绝. 单元数与布尔为 0 以外的残留如果 noResidualFormsOwnerPatches 仍为 true, 也无交叉拒绝. 此为源码级潜在误放行, 当前先被数组取值错误阻断, 不称实机复现.
- 最小修复范围: 精确绑定已确认生产类型/RemovePrefix 与真实 Remove target 的声明/单参数类型, 三快照再做 Ordinal 相等; ownerPatchCountAfterShutdown 精确验证 Forms 与 Forms.FormStanceMode.Watcher 为整数 0, 缺字段/null/类型错误失败.
- 尚缺实机证据: 修正后的 hub 正反解析夹具与授权真实 Harmony 元数据/Shutdown 实机. 未使用总 patch 数替代目标证明, 未调用 prefix 或运行测试.
- 本面结论: SUPERVISION_NEEDS_REWORK. 至此已列 3 项, 停止扩展审查.


### 监督收束与写集边界 (2026-10-05 11:04:51 +08:00)

- 总结论: SUPERVISION_NEEDS_REWORK. 单面结论 MATRIX_SUPERVISION_PASS 已成立; 控制器有上述 3 项, 不签整体通过, 不继续扩面.
- 最后一次只读指纹检查: 四文件仍与本轮已记录指纹相同=True. 此为检查时点快照, 不覆盖将来的 hub 修订.
- r20 原件: AFBE52076154176890F6291574B63CFEF315D76729F20706AB8C176AC3F83EA2; r22 控制器: 1B8865E6F1AD826B250923727B639807E3383493C2CB04A2C4CC7A2130C454E1. r22 控制器与 gate-notice.json 的指纹一致.
- 外围隔离独立核对: r20:1-43 与 r22:1-43、r20:131-184 与 r22:283-336 在仅规范 CRLF/LF 后 Ordinal 全等. 原件混合 3 CRLF/180 LF, 新件 335 CRLF/0 LF, 因此不能宣称外围字节完全未变. Saveguard 原44-75/新44-75 规范换行后全等; effects 原122-125/新256-259 在规范换行并忽略块末缩进后全等 (末尾多 1 空格).
- 上述差异界限证明未改既有无窗口 CreateNoWindow/Hidden、G: 独立环境、路径/reparse 拒绝、共享配置只读 Snapshot/hash、安全 Move、退出/日志排空等逻辑; 不等于这些路径已实机验证.
- bindingloss 最终 status=completed、finalEvidencePhase=post-quit、quitDrainSettled=bool true、quitDrainOutcome=settled、exitCode=整数 0 的原门禁在 r22:190-196 保留; 新增分类/identity 门禁缺口见检查面 1-2.
- hub 静态证据: G:\omp works\.tmp\forms-independent-20261005\controller-r22-static-gates.json (RecordedAt=2026-10-05T10:56:22.1441614+08:00) 记录两脚本 ParseErrorCount=0 与矩阵精确替换. 本会话没有重跑 AST/解析夹具, AST 0 errors 不构成运行通过.
- 本轮仅写本 supervisor.md, 仅使用本 harness 文件读写命令; 未 peer/thread 轮询, 未委派/其它 harness/fallback/构建/lint/测试/git/游戏/部署/共享配置写入/C: 写入. 指定模型/provider 不以请求文字冒充实测 metadata, 路由实证仍由 hub 元数据负责.

## 进行中

- 无代理/进程在等待, 不轮询, 不继续本轮扩面. 本轮有界监督已停止; 需要 hub 修复并重新提供明确门禁后再安排核对.
- phase3 源码/schema 对照尚未授权为 CODE_COMPLETE: actionRejectionEvidence 的真实 action settle/fault/reference proof, expectedRejectionRoots 是否必填, final 分类是全量还是增量等, 只列待办, 不读在途修改当作既定契约.
- 待 phase3 真正完成并有门禁后, 逐字段确认两实际 action 确已执行并 settled, timeout/pending/cancelled 不通过, 并核实支付/HP/block/power/marker/history 各分量无变化. 当前不以顶层 noNativeMutation 布尔代替未完成的明细验证.

## 未知

- 尚无本轮控制器/矩阵真实运行证据, 无授权实机路径/退出 drain/共享配置无变化/Harmony 元数据/动作拒绝验收; 无合成解析器执行结果.
- phase3 在途字段与 final fault 分类/持久化 identity 关联的最终结构未覆盖, 不能据此判其实现通过或失败.
- JSON sequence/type/action proof 是持久化关联, 不等于在 JSON 进程里重新证明 C# Exception reference identity. 必须结合完成后的载体源契约与实机输出.
- 当前会话实际模型/provider route 元数据未独立核验; 未自行选择其它模型/路由或启动 fallback.
- 最终状态: SUPERVISION_NEEDS_REWORK; MATRIX_SUPERVISION_PASS 仅为独立窄写集的源码/字节监督, 非实机.