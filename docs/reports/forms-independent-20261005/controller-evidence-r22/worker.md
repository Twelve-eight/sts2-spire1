# r22 窄解析门禁修复 - 实现者报告

范围: 仅新增 `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1`(从 r20 复制).
唯一可写报告路径: 本文件. 不改生产/测试代码, 不构建/不运行/不部署.

## 已确认

- 结论1(门禁冲突, 已确认): r20 `Test-BindingLossEvidence` 在 `run-isolated-smoke-r20.ps1:105` 强制 `unobservedFaults` 为空(`Count -ne 0` 即 throw). 新 schema 在 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1267-1274` 将 `unobservedFaults` 写成"本场景全部 raw fault 文本", 允许非空; 真正的通过条件是 `unexpectedUnobservedFaults` 为非 null 空数组且 `unobservedFaultGatePassed` 为 bool true. 证据: 同文件 `1250-1289` 的 `ApplyUnobservedFaultGate`.
- 结论1补充(已确认): `BindingLossSmokeRunner.cs:494-501` 先写 `expectedRejectionRoots`/`expectedRejectionsExpected`, 随后 `:638` 调 `ApplyUnobservedFaultGate`, 在 `FormNativeSmokeRunner.cs:1269` 用按 reference identity 归类的 `expected` 子集覆盖 `expectedRejections`. 最终 JSON 暴露: `expectedRejectionRoots`(异常类型名数组), `expectedRejectionsExpected`(数量), `expectedRejections`(fault evidence 对象数组, 含 sequence/observedUtc/exceptionType/exception), `unobservedFaultEvidence`(同结构 raw 数组), `unexpectedUnobservedFaults`(文本数组), `unobservedFaultGatePassed`(bool).
- 结论2(门禁缺失, 已确认): r20 `Test-Evidence` lifecycle 分支在 `run-isolated-smoke-r20.ps1:127` 只检查 `status/bridgeBound/repeatedInitializeDidNotStackPatches/bridgeFailedClosedAfterDuplicate/terminalRequiresRestart/noResidualFormsOwnerPatches/shutdownIdempotent`, 完全没有读取 `safetyGuardBefore`/`safetyGuardAfterReinit`/`safetyGuardAfterShutdown`/`safetyGuardSurvivesShutdown`. 新 schema 在 `LifecycleSmokeRunner.cs:154-191` 明确写出这些字段, `:412-432` 定义 `SafetyGuardProof`(owner=Forms.FormStanceSafety, removeMethodFound, removePatchedBySafetyOwner, prefixCount==1)与 `SafetyGuardSameIdentity`(prefixCount/targets/prefixDeclaringMethods 全等).

## 进行中

- 正在核对 binding-loss 最终报告 `form-native-smoke-final.json` 的 settled/quit 字段与 action evidence 的 settled/timeout 字段, 以确定解析器逐项门禁.

## 未知

- 尚未在实机验证 r22(本轮明确不运行).
- JSON 仅暴露异常类型与文本, 不暴露跨进程可复核的 reference identity; 解析器只能核对 sequence/raw/type 对应, 无法从落盘 JSON 独立重放 C# 内部的对象同一性判定. 这一点将在报告末尾如实标注为需测试载体补齐的分类字段.
## 增量 2 (binding-loss 解析器已改, 未运行)

- 已把 r22 的 `Test-BindingLossEvidence` 从"raw fault 必须为空"改为新 schema 门禁:
  - 保留原入场/失效/双卡拒绝/无变化/无回归/安全/最终 quit settled 全部门禁.
  - 新增 `Test-ActionRejectionEvidence`: `actionRejectionEvidence` 必须存在且恰 2 项, label 恰为 `binding-loss-next-watcher-strike` 与 `binding-loss-stance-change-probe`, cardEntry 分别恰为 `WATCHER_STRIKE_P` 与 `WatcherTranquility`, `actionExecutedAndSettled`/`explicitFormsRestart` 必须布尔 true, `actionFaultRootType` 必须非空字符串, `identityMatchedFaultSequences` 必须非 null 数组且至少 1 项整数.
  - 新增 `Test-BindingLossFaultClassification`: `unobservedFaults`/`unobservedFaultEvidence`/`expectedRejections`/`unexpectedUnobservedFaults` 必须非 null 数组; `unobservedFaultGatePassed` 必须布尔 true; `unexpectedUnobservedFaults` 必须为空; raw 文本数=raw evidence 数=expected+unexpected; expected sequence 必须落在 raw evidence; 每条 action proof 的 sequence 必须落在 expected 集合; 每个 expected sequence 必须被某个 action proof 认领(不丢/不重); `expectedRejectionsExpected` 必须为整数且等于 expected 条数; `expectedRejectionRoots` 若存在必须恰 2 项且覆盖两个 proof 的 root type.
  - final 报告新增: `unobservedFaults`/`unobservedFaultEvidence`/`expectedRejections`/`unexpectedUnobservedFaults` 非 null 数组, `unobservedFaultGatePassed` 布尔 true, final unexpected 为空, final raw 数与场景 raw sequence 集合双向相等(同源单调 sequence).
- 边界: `expectedRejectionRoots` 为可选(存在才校验), 因为 phase3 未把它列为必须字段; 若 phase3 CODE_COMPLETE 后确认为必填, 需再收紧.
- 未运行: 本轮不构建/不测试; 上述仅为源码级解析门禁, 不是实机证据.

## 增量 3 (进行中)

- Lifecycle 解析器待改: 需在原有不叠补丁/Terminal/两次 Shutdown 旧 owner0 之上, 增加 `safetyGuardBefore`/`safetyGuardAfterReinit`/`safetyGuardAfterShutdown` 精确 owner=Forms.FormStanceSafety, Remove 目标与 prefix 声明身份, prefix 恰 1 且身份相等, `safetyGuardSurvivesShutdown` true.
## 增量 4 (Lifecycle 解析器已改, 未运行)

- r22 `Test-Evidence` 的 lifecycle 分支已改为 fail-closed 精确校验:
  - 保留原 `status=completed`/`bridgeBound`/`repeatedInitializeDidNotStackPatches`/`bridgeFailedClosedAfterDuplicate`/`terminalRequiresRestart`/`noResidualFormsOwnerPatches`/`shutdownIdempotent` 门禁.
  - 新增 `safetyGuardSurvivesShutdown` 必须布尔 true.
  - 新增 `Test-SafetyGuardEvidence`: `safetyGuardBefore`/`safetyGuardAfterReinit`/`safetyGuardAfterShutdown` 必须存在且非 null; `owner` 必须严格等于 `Forms.FormStanceSafety`; `removeMethodFound`/`removePatchedBySafetyOwner` 必须布尔 true; `prefixCount` 必须整数且恰为 1; `targets` 必须非 null 数组恰 1 项且形如 `MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(...PowerModel...)`; `prefixPatchTypes`/`prefixDeclaringMethods` 各恰 1 项且为非空字符串.
  - 新增 `Test-SafetyGuardSameIdentity`: 三份证据的 owner/target/prefixCount/patchType/declaring 必须 Ordinal 全等 (reinit 与 shutdown 均与 before 相等).
- 边界: 不引用无关总 patch 数; 不把异常文本包含 Forms 当独立凭据.

## 增量 5 (独立机械写集: 矩阵 r22)

- 新增 `G:\omp works\.tmp\forms-independent-20261005\run-independent-matrix-r22.ps1`, 从 `run-independent-matrix.ps1` 复制, 仅把 4 处普通变量 `$error` 改为 `$launchError`, 其它字节语义未动; 原件保留未改.
- 复核: 原文 `$error` 出现 4 次 (index 6072/6975/7922/8458), 新文件 `$error` 0 次 / `$launchError` 4 次; 新文件 9338 字节, 与原件 9314 字节仅差 4 个字符 (`$error`->`$launchError` 每处 +4 字节).
- 静态解析: 两文件均 0 parse errors (仅静态解析, 非运行验证).

## 已确认 (最终)

- 静态语法: 两文件 `[Management.Automation.Language.Parser]::ParseFile` 均 0 parse errors.
- r22 与原 r20 差异隔离已复核: r20 第 1..43 行与 r22 第 1..43 行逐字相等; r20 自 `}if($CaseIds.Count)` 起的 54 行尾部与 r22 同尾逐字相等. 全部改动落在解析器区, 未触碰 effects/saveguard/启动隔离/路径保护/无窗口/G: 环境/日志排空/共享 config 只读 hash/安全 Move 语义.
- r22 无普通 `$error`/`$pid` 变量 (0/0); 矩阵 r22 无 `$error` (0).
- 新增门禁不依赖 `run-isolated-smoke-r20.ps1` 的 raw 必须为空旧规则, 与 `FormNativeSmokeRunner.cs:1250-1289` 的 `ApplyUnobservedFaultGate` schema 对齐.

## 进行中 (待 phase3 CODE_COMPLETE 后核对, 不空等不造字段)

- `actionRejectionEvidence` 的 `identityMatchedFaultSequences` 与 final 报告 `unobservedFaults`/`unobservedFaultEvidence`/`expectedRejections`/`unexpectedUnobservedFaults`/`unobservedFaultGatePassed` 均由 phase3 计划新增, 当前测试源码尚未落地; 本控制器已按 phase3 请求的字段名/类型/数量门禁编码, 未读取未完成产物当既定 schema.
- `expectedRejectionRoots` 暂按可选处理(存在才校验恰 2 且覆盖两个 proof root type); 若 phase3 确认必填需再收紧.
- 最终 raw 与场景 raw 的 sequence 集合双向相等校验: 若 phase3 final 只写 post-quit 增量而非全量, 该门禁会拒绝; 届时按 phase3 实际语义窄调.

## 未知

- 未构建/未 lint/未运行/未部署/未 git; 无实机证据, 不声称实机验收.
- phase3 两处编译错误修复与 final 持久化 identity proof 由 phase3 实现者负责, 本会话未改测试源码.
- 跨进程 JSON 无法独立重放 C# 内部 Exception reference identity; 控制器只校验持久化 sequence/type/分类与 action proof 的交叉一致性.

## 交付文件与 SHA256

- `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1`
  - bytes 29863, SHA256 `1B8865E6F1AD826B250923727B639807E3383493C2CB04A2C4CC7A2130C454E1`
- `G:\omp works\.tmp\forms-independent-20261005\run-independent-matrix-r22.ps1`
  - bytes 9338, SHA256 `BAF24B6C270F6EEE9329713F538070AFDE55E1316009AC81207229291A477D95`
- 原 r20 SHA256 `AFBE52076154176890F6291574B63CFEF315D76729F20706AB8C176AC3F83EA2` (未改).
- 原矩阵 SHA256 `FE4B1DAFE2032F24E1E921EBD01C7AE312FBE2B3785A4E1FF7DA900B78D7092C` (未改).

## 状态

CODE_COMPLETE (控制器与矩阵已落盘; 未运行; phase3 schema 面待 hub 后续唤醒核对).