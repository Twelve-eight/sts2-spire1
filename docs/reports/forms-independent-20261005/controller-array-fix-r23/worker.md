# r23 控制器窄修增量报告

写集仅本报告与 `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1`. 不改 r20/r22/矩阵/生产/测试源码. 不构建/lint/测试/运行/部署/git/委派/peer/其它 harness.
请求指定: `global:deepseek-v4.1-flash` / `wb2api`. 未自行选择模型/provider/fallback; 当前真实路由元数据本实现者未独立核验, 由 hub 核实, 不用请求文字冒充实证.

## 已确认

### 面 1, P1: 数组形状丢失, 首条结论已立即落盘
- 路径及行号: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1:40,76-79,121-124,130-139,197-206,227-229`.
- 触发: 权威 JSON 的合法 empty/single 数组通过 `Property` 函数管道时被枚举, empty 变 null, single 变 scalar, 随后 `Test-NonNullArray` 拒绝.
- 契约: null/missing/empty/single/multi/scalar 必须保持区别, 不以数组包装伪造证据.
- 证据: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-evidence-r22\supervisor.md:33-41`; 中央已有纯函数复现 `G:\omp works\.tmp\forms-independent-20261005\controller-r22-array-shape-repro.json` 中 empty/single 的 Match=false, EmptyAndSingleArrayBugConfirmed=true. 此为读取已有中央证据, 本会话未重跑.
- 最小修复: r23 添加保留属性对象、非枚举返回 helper, 新 typed-array 读取改走该边界; 保持原 Property 和其它模式不变.
- 复核命令(仅源码, 未运行夹具): `Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1' | Select-Object -Skip 39 -First 1`.
- 尚缺实机证据: hub 修后正反夹具与授权实机; 两者不可混称.

### 面 1 编码完成, 已增量落盘
- r23 已从 r22 新建, 添加 `Evidence-Property`: 缺失属性单独抛错, 实际 Value 以 unary comma 不枚举返回. empty/single/multi 保持原数组对象, scalar 不包装为数组, null 仍由原类型门禁拒绝.
- 仅将新增 binding/final/lifecycle 数组读取转到 helper; 未修改原 `Property`, saveguard/effects 与脚本主体.
- phase3 完成门禁已核对: `phase3-gate-notice.json` 记录 native wait completed/TimedOut=false/CODE_COMPLETE. 当前 BindingLossSmokeRunner.cs 的 DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A 与 FormNativeSmokeRunner.cs 的 4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D 逐一匹配; LifecycleSmokeRunner.cs 的 3457BCDED4BED87C2815822D002F57E9BFE74655550726218CA0FF33269B8CCA 与已完成 phase2 相符. 这只允许读取完成 schema, 不表示编译/实机通过.
- 未执行函数或夹具. 面 2/3 继续编码.
### 面 2, P1: action/raw/expected/final 编码已增量落盘
- 权威字段: 完成的 BindingLossSmokeRunner.cs:389-423,483-518,817-825,1037-1067; FormNativeSmokeRunner.cs:202,262-296,1277-1315,1758-1770. 全程使用 phase3 native 门禁相符字节, 未造字段.
- 根数与事件数分离: expectedRejectionsExpected 必須整数2, expectedRejectionRoots 恰2并按两个实际 action 的 root type 对照; 每个 action 允许多个唯一正整数事件序号, 不把 roots 数量当 raw events 数量.
- 两个 proof 必须对应真实 label/cardEntry/nextStrike/stanceChangeProbe; proof.action 与真实 run.action 都须 executionObserved=true/actionExecutedAndSettled=true/state=Finished/cancelled=false, status 为 faulted/failed 且 CompletionTask settled. timeout/pending/cancelled/unknown 完成时间不放行. 不以顶层 bool 替代分量.
- 每条 proof 内与跨 action 的 sequence 重复均失败. raw 与 expected 逐对象核 sequence/exceptionType/exception/observedUtc, raw 字符串按索引与同条 evidence.exception Ordinal 相等. raw/expected 中重复、非对象、无效序号、无 action 认领或遗漏均失败.
- final 复用同一个严格分类函数, 与场景逐 sequence 比对完整对象, 不只比集合/数量. 缺任一分类字段失败. 对 cleanup/quit 新事件缺持久化 action 序号映射的情况保守拒绝, 不伪造归属.
- 支付/HP/MaxHp/block/死亡/power amounts/full names/raw markers/stance/真实 history 已按两个 run 的 before/after 对照. top-level 各 mutation/实际出牌/伤害分量亦必须 false/0. Probe 的原始 watcherStrikeHistory 仍是 Strike 过滤历史, 不冒充 Tranquility 专属历史; probeActualCardPlay 分量按真实完成载体读取.
- 源码复核命令: `Select-String -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1' -Pattern 'sequenceOwners|Test-SameFaultEvidence|expectedRejectionsExpected|Test-RejectedAction|Test-NoNativeMutation'`. 未运行解析函数/夹具.
- 尚缺: hub 正反夹具及实机 action identity/drain. 面3继续.
### 面 3, P1: lifecycle 精确身份与两个旧 owner0 已增量落盘
- 契约源码: 完成的 LifecycleSmokeRunner.cs:299-300,313-365,374-378; 生产 FormStanceSafetyGuard.cs:57-67,101-112; 引擎 `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291-295` 确认 `Remove(PowerModel? power)`.
- r23 三快照的 targets 恰1并 Ordinal 等于 `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(MegaCrit.Sts2.Core.Models.PowerModel power)`, prefixPatchTypes 恰1并等于 `Forms.FormsCode.FormStanceSafetyGuard`, prefixDeclaringMethods 恰1并等于 `RemovePrefix`. owner 精确 Forms.FormStanceSafety, prefixCount 整数1, removeMethodFound/removePatchedBySafetyOwner 必须 true. removeMethodIdentity 必须非空且三个快照相等.
- ownerPatchCountAfterShutdown 必须存在, Forms 与 Forms.FormStanceMode.Watcher 两个精确属性必须整数0. 顶层 noResidualFormsOwnerPatches=true 无法代替该分量. 仍保留原不叠补丁/Terminal/两次 Shutdown/guardSurvivesShutdown 门禁.
- 只读复核命令: `Select-String -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1' -Pattern 'RemovePrefix|ownerPatchCountAfterShutdown|exact Remove'`.
- 时间字符串保真: 仅 binding-loss 两个 JSON 读取增加 `ConvertFrom-Json -DateKind String`, 防止宿主自动解析 observedUtc/completedUtc 改变原证据类型. 要求 PowerShell 7.5+; 本机前轮记录为 7.6.5. 不改其它模式 JSON 读取.
- 未执行解析/测试; 尚缺 hub 正反夹具与真实 Harmony 元数据/Shutdown 验收.
### 最终交付与源码差异收割
- 状态: CODE_COMPLETE. 三个窄修面已逐面写代码并立即增量写本报告; 未扩 core probes/开关, 不等 peer, 无后台任务.
- 准确源码行号: r23:76-105 属性/数组/对象/整数保真; :111-166 双真实 action 拒绝与归属; :168-205 raw/expected/final 公用逐对象分类; :222-247 两 action 无副作用快照; :275-293 两实际 roots 与分量; :301-305 final/scenario 精确对象对照; :310-332 精确安全 prefix; :355-367 两旧 owner0 与三快照稳定.
- 只读差异复核: r22/r23 解析器以前区域(包含原 Property/saveguard/参数/模式)全文 Ordinal 全等; Test-Evidence 到 lifecycle 分支以前区域(包含 effects/分发)全文 Ordinal 全等; `}if($CaseIds.Count)` 到文件末全文 Ordinal 全等. 两新文件都无 BOM, 保持 r22 行尾. 因而这些区域 UTF-8 字节保持, 无窗口/路径/G:环境/日志排空/共享配置 hash/Move 主体未改. 不把文本核对称为运行验收.
- r23 普通 $error/$pid 匹配数=0. 未执行 Parser/AST/纯函数/夹具, 不声称语法或运行已通过; hub 按本轮禁令负责集中验证.
- 收割时旧件哈希不变:
  - r20: AFBE52076154176890F6291574B63CFEF315D76729F20706AB8C176AC3F83EA2
  - r22: 1B8865E6F1AD826B250923727B639807E3383493C2CB04A2C4CC7A2130C454E1
  - 原矩阵: FE4B1DAFE2032F24E1E921EBD01C7AE312FBE2B3785A4E1FF7DA900B78D7092C
  - r22 矩阵: BAF24B6C270F6EEE9329713F538070AFDE55E1316009AC81207229291A477D95
- phase3 双源与 Lifecycle 文件收割哈希仍与前述 native 完成门禁相符; 未改测试/生产源码.
- 交付脚本: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1`.
- 字节数: 35948.
- SHA256: `AF007F9F785AA4877377B3A94374F33D6B9CDEACBDB9271D709B99E18272B049`.
- 收割时点: 2026-10-05T11:20:42.1522693+08:00.

## 进行中

- 无. 本实现者有界停止; 等 hub 最新 native completed 门禁后唤醒同批监督, 本会话不自行轮询或派发.

## 未知

- 本轮未构建/lint/测试/运行/游戏/部署/git, 无新字节语法验证、正反夹具或实机证据; 不声称已验收.
- JSON 持久化 sequence/action/typed 对照不等于重新证明运行时 Exception reference identity, 需结合完成载体源码及授权实机 raw 事件.
- phase3 final 允许 late 事件按旧 roots 分类, 但没有新增 late 事件的 per-action 持久化序号映射. r23 对未在双 action proof 认领的 final 事件 fail closed, 不按异常文本补归属; 若 hub 实机出现该合法边界, 需测试载体提供可复核 proof 后另行窄调.
- 当前快照 watcherStrikeHistory 专用于 Strike; 不声称提供 Tranquility 独立逐条历史. probeActualCardPlay/伤害与 probe before/after 分量仍来自完成 schema 并严格核查.
- ConvertFrom-Json -DateKind String 要求 PowerShell 7.5+; 未自行安装或修改工具链. hub 需使用满足条件的宿主.
- 当前真实模型/provider route 未独立核验, 不以请求的 wb2api 字样当元数据证据, 本会话没有切换/委派/fallback.