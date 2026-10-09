# r33 worker 增量报告

## 已确认

- 2026-10-05 13:28（Asia/Shanghai）：已读取 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\query-action-separation-r33\worker.request.md`，本会话按请求作为 r33 实现者直接执行；不构建、不 lint、不测试、不部署、不操作游戏。
- 唯一可写代码白名单（4 文件）：
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs`
- 唯一可写报告：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\query-action-separation-r33\worker.md`。
- 修改前基线 SHA256：
  - `FormStanceMode.cs` = `3A4832F3CE1F654A3EEC77072E07536A0C4D5CC30B1E67F0E057E0AD99EFD080`
  - `FormStanceModifier.cs` = `8167F71AE9E602A599B4F9E5BE36FC0BA4F827AAF7872FE9C7C2A94D1F2E3F25`
  - `FormStanceSafetyGuard.cs` = `C4851CC3129C4334A088904FA70D0A976C18A649F836F92024126B5213CBEE84`
  - `MainFile.cs` = `1645DA4C48F7C88B61E13519AD234AE470903B06004E08CE170C7102BEEE5A1A`
- 同批监督报告 `supervisor.md` 当前为 `WAITING_GATE`；本会话未注入原生 `spawn_agent`/`wait_agent`，因此不派发、不通知、不触碰监督报告；门禁由外部 hub 的真实 native wait 处理。

## 进行中

- 无。代码实现与静态自检已完成，等待同批监督与主会话集中构建/实机验证。

## 已确认（续）

- 2026-10-05 13:40（Asia/Shanghai）：4 个白名单文件实现完成，未构建/lint/test。修改后 SHA256：
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs` = `15271CF355E6303C09D0FCE56286CF5137385AD788ABEB48D1871CCE490F5034`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs` = `CDA047A214E93CBDBFB21C524A2DEC79204ABCC4157CB0F778F8600DE824F5E2`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs` = `B722AD5770D8D4DA02EB51EC5D0DA395157A728AD38873B71DA2A330B41AFAB7`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs` = `156227F421F446480930E1C184182C86C641746D8DAED9908A6A8BF254A14F8C`
- 未改动 `pump`、`effects`、测试、ID、PCK、Spire1 或其它源文件；`FormsCode` 目录最近修改文件恰好为本批 4 个白名单文件。
- 静态交叉检查：查询路径仅使用 `IsSelectedFormsCombatUnavailable`（`FormStanceModifier.ShouldPlay` 第 46-49 行）；实际动作入口仅使用 `ThrowIfSelectedFormsActionUnavailable`（`FormStanceSafetyGuard.PlayPrefix` 第 185-189 行）；其它 modifier hooks 与 `RemovePrefix` 仍使用原 throw 版本。
- `PowerCmd.Remove(PowerModel? power)` 权威签名已由 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs` 第 291 行确认。
- 安装原子性：`EnsureInstalled` 两个 `harmony.Patch` 与 `ProofHoldsLocked` 包在同一 try/catch；任一 patch 或证明失败即 `harmony.UnpatchAll(HarmonyId)` 回滚并清空两个 target/prefix，`_installed=false` 后重抛；`MainFile.PatchesHealthy` 因 `FormStanceSafetyGuard.IsInstalled` 为 false 而无法发布 Bound。
- `Shutdown`/Terminal 不撤 `FormStanceSafetyGuard`（`MainFile.Shutdown` 只 `UnpatchAll(ModId)`），两个 safety prefix 按契约驻留至进程退出。
- 代码括号配平自检：4 文件 `{`/`}` 数量分别相等（14/14、8/8、18/18、36/36）。此仅为静态文本检查，不是编译。

## CODE_COMPLETE

- 状态：CODE_COMPLETE（仅代码写入完成；未构建、未 lint、未测试、未部署、未实机验证）。
- 唯一可写报告：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\query-action-separation-r33\worker.md`。
- 请求内 4 个白名单文件全部修改，无 stub、无其它文件改动。
- 尚缺证据（由 hub/r34 集中验证）：Release 构建、结构门禁、真实 Terminal/Shutdown 双实际动作拒绝、无能量/HP/marker 变化、UI 无额外 fault、正常三形态与普通局控制。
## 未知

- 编译是否通过：本会话按请求不构建，未验证。
- 真实 Terminal/Shutdown 下 `PlayCardAction.ExecuteAction` prefix 是否在实机形成显式 fault 且无副作用：未实机验证。
- UI 查询路径是否彻底无额外 fault：未实机验证。
- 两 prefix 在真实运行时的 Harmony owner 计数（应为 owner 2、单 target 各 1）：未运行时验证。
- 普通 Bound/非 Forms 局是否完全不受影响：未实机验证。