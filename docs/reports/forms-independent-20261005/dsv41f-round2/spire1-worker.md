# Spire1 worker 报告 - dsv41f round2 (CODE_COMPLETE)

- 范围: 仅 Spire1 mod 与必要的同仓文档报告
- 姿态: 只改源码, 不构建, 不测试, 不部署, 不改共享配置, 不委派
- 源码已改文件:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs` (新建)
- `MainFile.cs` 未改 (核对后确认无必要).

## 已确认

### 源码已改

- [已确认] `Spire1.csproj:72-74` 新增三个默认排除: `Spire1Code/Forms/**`, `Spire1Code/Run/FormNativeSmokeRunner.cs`, `Spire1Code/Patches/FormNativeSmokePatch.cs`. Forms 目录整体不再编进 Spire1.dll, smoke runner/patch 不进入生产 DLL.
- [已确认] `StanceCmd.cs` 删除 `using Spire1.Spire1Code.Forms;` 与全部 Forms 类型引用; `IsIn/Enter/Exit` 改为调用 `FormsCompatibilityBridge` 的 `IsSelected/KindOf/CurrentKind/Enter/Exit`. 普通 Calm/Wrath/Divinity 逻辑 (`Current`, `PowerCmd.Apply/Remove`, Divinity 加能量, `GainMantra`, `Dispatch`) 原样保留.
- [已确认] `StanceCmd.cs:10` 新增 `using Spire1.Spire1Code.Interop;`; 删除的 `typeof(WatcherFormStancePower).IsAssignableFrom(...)` 分支不再需要 (Forms 类型已不在 Spire1 编译单元, 泛型约束只接受 Spire1 `StancePower`).
- [已确认] 新建 `FormsCompatibilityBridge.cs`: 只按字符串 `"Forms"` 程序集名 + 全名 `"Forms.FormsCode.Interop.FormsRuntimeEntryPoint"` + 显式签名校验做反射; 无任何 `typeof(Forms...)`, 无 Forms using, 无 Forms AssemblyRef.
- [已确认] 桥校验的签名与契约一致: `public static bool IsAvailable` 属性, `IsSelected(Player)->bool`, `KindOf(Type)->int`, `CurrentKind(Player)->int`, `Enter(PlayerChoiceContext,Player,Type,CardModel)->Task`, `Exit(PlayerChoiceContext,Player,CardModel)->Task`. 任一缺失/重载歧义/返回类型或参数漂移 → 校验抛错 → 桥 disabled, 不发布任何 delegate.
- [已确认] 线程安全: 静态 `ProbeLock` 保护探测与发布; `_available`/`_assemblyLoadRevision` 用 `Volatile`; 通过 `AppDomain.AssemblyLoad` 递增 revision, Forms 晚加载后下一次调用会重新探测; 同一 revision 只探测一次.
- [已确认] `Spire1PowersGatePatch.cs` 删除 `using Spire1.Spire1Code.Forms;` 与 `private const string FormsNamespace = "Spire1.Spire1Code.Forms";`; 原按命名空间判定的 `IsFormsPower` 改为 `FormsCompatibilityBridge.IsFormsType(power.GetType())` (按已绑定 Forms 程序集身份), 无 Forms 类型/命名空间硬编码, 也不再调用 `FormStanceMode.IsSelected`.
- [已确认] powers gate 的独立 Forms 语义: `ShouldBlockApply`/`ShouldBlockModify` 命中独立 Forms 程序集类型时直接放行, 使独立 Forms power 不进入 Spire1 powers gate; Spire1 自身 `Spire1.*` 前缀 gate 行为未改.
- [已确认] 编译文件集核对: 排除 Forms 目录与 smoke 文件后, 377 个 .cs 中匹配 `FormStanceMode|FormStanceWatcherBridge|FormStanceKind|WatcherFormStancePower|FormStanceModifier|VoidSerpentStancePower|DemonReaperStancePower|EchoCelestialStancePower|VoidFormEffectPower|SerpentFormPower|ReaperFormEffectPower|EchoFormEffectPower|CelestialFormPower|Spire1.Spire1Code.Forms` 的命中为 0.
- [已确认] `MainFile.cs` 未改, 且 Phase3 只扫描 `typeof(MainFile).Assembly.GetTypes()`, 不扫描其它程序集; 移除 smoke 文件后无需为 smoke 或跨程序集扫描改 MainFile.
- [已确认] `Spire1.json` 未改: 依赖仍只有 BaseLib, 无 Forms manifest 依赖, 未引入新前置.
- [已确认] 契约补充已实现: `FormsCompatibilityBridge.cs` 新增公开 `Spire1.Spire1Code.Interop.FormsCompatibilityEntryPoint`, 签名 `public static Task Dispatch(Player, PlayerChoiceContext, int previousKind, int nextKind)`; 0/1/2/3 映射为 null/`CalmPower`/`WrathPower`/`DivinityPower` 的 canonical 实例 (仅作只读通知参数, 不 Apply/不挂载/不改资源), 再 `await StanceCmd.Dispatch`.
- [已确认] 契约补充的显式失败语义: `FormsCompatibilityBridge.IsSelected` 先判断 Forms 程序集是否已加载 (未加载 → 返回 false, 普通路径可用); 若 Forms 已加载且本局 modifier 列表中存在 Forms 程序集声明的 modifier, 但桥不可用 → 抛 `InvalidOperationException`, 不静默退回普通规则; 转发调用 (IsSelected/KindOf/CurrentKind/Enter/Exit) 抛错时标记桥不可用并把异常包装上抛.

### 与审查报告的差异 (已核实)

- [已确认] `coupling-audit.md` C-10 判定有误: `Cards/DemonForm.cs:14,22,25` 的 `DemonFormPower` 解析到原版 `MegaCrit.Sts2.Core.Models.Powers.DemonFormPower` (该文件 `using MegaCrit.Sts2.Core.Models.Powers;`, 无 Forms using; 原版类型见 `.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Powers/DemonFormPower.cs:15`). 移除 Forms 目录不会破坏普通局恶魔形态卡, 因此本任务无需改该卡或新增 Forms 前置.

## 进行中

- 无. 本任务写集内源码改动已全部落盘并复核, 剩余工作已收束到当前反射桥与通知协议; 不再扩展其它文件.

## 未知

- [未知] Forms 侧 `Forms.FormsCode.Interop.FormsRuntimeEntryPoint` 的真实签名未在本机核对: 工作区当前不存在 `G:\omp works\Sts\sts2-forms` 目录, 该类型未落盘. 本轮只按请求契约的签名做反射校验; 若 Forms 侧实际签名不同, 桥会 disabled, 本局已选 modifier 时会显式失败而不是静默降级.
- [未知] 未构建, 未测试: 上述结论全部为源码静态核对, 不代表编译通过或游戏内加载通过.
- [未知] `IsFormsType` 依赖桥已成功绑定; 若 Forms 程序集已加载但 entrypoint 签名不匹配, 该判定为 false. 由于独立 Forms power 命名空间不以 `Spire1.` 开头, `IsGatedPower` 本就不会命中, 因此 powers gate 结果仍正确; 该分支只在 Forms 使用 `Spire1.*` 命名空间时才有额外意义.
- [未知] `FormsCompatibilityEntryPoint` 由 Forms 侧反射调用; Forms 是否会在自己进程退出/重载时清理对 Spire1 的调用未验证 (超出本轮写集).