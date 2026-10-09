# r24 控制器脚本 (worker)

范围: 只新增 `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1` 与唯一本报告。
本轮未构建 / 未 lint / 未测试 / 未部署 / 未运行游戏 / 未 git / 未 peer / 未再委派 / 未启动其它 harness。
未读取 r25 源码; r25 的 `actualCardHistory` 只按本约定实现解析。

## 已确认

1. [P0] r22 的 typed 数组形状缺陷已由 r23 修复并被 r24 继承。
   - 证据: `G:\omp works\.tmp\forms-independent-20261005\controller-r22-array-shape-repro.json` 记录 `empty`/`single` 的 `ActualArray=false` (`EmptyAndSingleArrayBugConfirmed=true`)。
   - r24 采用 `Test-NonNullArray` 直接要求 `[System.Array]` 且非 null, 因此 `@()` 与单元素 `@(x)` 都保持数组形状; `null`/缺失/标量拒绝。
   - 复现命令(只读解析, 不执行主体): `powershell -NoProfile -Command "$t=$null;$e=$null;[Management.Automation.Language.Parser]::ParseFile('G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1',[ref]$t,[ref]$e)|Out-Null;$e.Count"`。
   - 尚缺实机证据: 由主 hub 对新字节重跑中央夹具与实机。

2. [P0] r24 独立补齐 r25 约定的 `actualCardHistory` 门禁。
   - 新增 `Test-ActualCardHistory`: 要求 `card`、`cardTypeFullName`、非空 `cardInstanceIdentity`、`observedByCardReference` 为 true、四个非负整数计数, 且 denied 卡前后 started/finished 计数相等。
   - `cardTypeFullName` 按本地 Watcher 权威 source 精确核对: `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherStrike_P.cs:11,13` 为 `WatcherMod.WatcherStrike_P`; `WatcherTranquility.cs:8,10` 为 `WatcherMod.WatcherTranquility`。无差异, 未猜字符串。
   - `Test-ActionRejectionEvidence` 与 `Test-BindingLossEvidence` 都要求 `nextStrike`/`stanceChangeProbe` 携带对应 run 的 `actualCardHistory`, 并拒绝缺字段/未观察/计数变化。
   - 最小修复范围: 仅新增 `Test-ActualCardHistory` 与其两处调用, 不改脚本主体。
   - 尚缺实机证据: 需要 r25 生产输出后由 hub 重跑确认。

3. [P1] lifecycle 解析器按当前权威引擎契约收紧, 未继承 r23 结论。
   - 权威引擎: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291-299` 为单参数 `Remove(PowerModel? power)`, 先 `RemoveInternal()` 再 `AfterRemoved`。
   - 生产 guard: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs:57-67,101-112` owner 为 `Forms.FormStanceSafety`、prefix 为 `FormStanceSafetyGuard.RemovePrefix`。
   - r24 要求三个 safety 快照的 owner/target/type/method 精确 Ordinal 相等、`prefixCount` 整数 1、`RemoveMethodIdentity` 前后稳定; `ownerPatchCountAfterShutdown` 的 `Forms` 与 `Forms.FormStanceMode.Watcher` 必须整数 0, 顶层 bool 不能替代。
   - 尚缺实机证据: lifecycle 实机输出仍待 hub 重跑。

## 进行中

- r24 相对 r23 只改动解析函数区域, 差异 35 行新增 / 2 行删除; 旧主体区域 head(1-86) 与 tail(最后30行) 逐字相等 (行数 425 -> 458)。r22/r23/r24 的 SHA256 已记录于下。
- 尚未做中央正反夹具重跑与实机重跑; 这些按请求留给主 hub。

## 未知

- 未验证 r24 在真实游戏进程中的最终行为。
- 未读取 r25 源码, `actualCardHistory` 仅按本约定解析; 若 r25 实际 schema 与约定不一致, 需 hub 复核后再改解析器。
- `removeMethodIdentity` 的字符串格式来自现有生产/测试输出, r24 只要求非空且前后相等, 未额外规定编码格式。

## 新增文件与 hash

- `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1`
  - SHA256: `E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1`
  - bytes: 38596
- `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-recovery-r24\worker.md` (本文件)

参考基线 hash(未改): r22 `1B8865E6F1AD826B250923727B639807E3383493C2CB04A2C4CC7A2130C454E1`; r23 `AF007F9F785AA4877377B3A94374F33D6B9CDEACBDB9271D709B99E18272B049`。

CODE_COMPLETE
