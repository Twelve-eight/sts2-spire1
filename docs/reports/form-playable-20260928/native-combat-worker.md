# native-combat-worker 增量报告

## 已确认

### 1. P1 授权边界与证据分层
- 时间: 2026-09-30. 首条结论来自任务文件, 当前没有构建, 测试或实机证据.
- 证据: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\native-combat-worker.md:28-35. 本批仅允许六个验证载体文件和本报告, 禁止产品源码, git, 构建, 测试, 部署, 窗口与键鼠操作.
- 触发条件: 本次实现任务. 权威契约: 同文件第一阶段要求, 主会话集中执行与监督.
- 当前控制流: 已读取任务与工作区入口, 尚未写验证代码. 最小修复范围严格限定给定白名单.
- 可复现命令: 主会话后续按 COMBAT-README.md 的隔离命令执行, 本子代理不运行.
- 尚缺证据: 编译, 隔离原生目录预检, 实际加载和战斗断言全部未验证.
- 路由: 用户指定并声明已核验 gpt-6-astra-ar / gateway -> agentrouter -> gpt-6-astra, 本子代理未另启模型或运行时; 实际路由元数据由主会话绑定, 不以此文字替代元数据.

### 2. P1 真实运行 API 与依赖身份已定位
- 证据: E:\Slay the Spire 2\mods\BaseLib\BaseLib.json:2,7, E:\Slay the Spire 2\mods\Watcher\Watcher.json:3,7, E:\Slay the Spire 2\mods\Spire1\Spire1.json:2,11-17. 实际 id 为 BaseLib, Watcher, Spire1, 安装版分别 v3.4.7, 0.9.28, 1.2.3.
- 触发条件: 独立验证模组加载. 契约: 独立 net9.0, Godot 4.5.1, BaseLib 编译版本 3.4.5, 不引用 Watcher 编译依赖.
- 当前控制流证据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs:42-52,62-103 为真实手动支付入口; GameAction.cs:61-63,135-148 表明完成信号必须额外核对 Exception 与实际历史.
- 可复核命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs'. 仅源码读取, 不构成实机复现.
- 最小修复范围: 白名单中的独立 csproj, manifest, 启动器, 驱动及断言文件. 尚缺实机证据: 实际 DLL 绑定, Action 完成和支付结果.
### 3. P1 事务探针阻塞已精确定位
- 证据: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj:24-26 显式包含 TransactionPatchAdapter.cs 与缺失的 TransactionScenarios.cs; G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionPatchAdapter.cs:43-121 已提供生产私有 patch 的反射适配入口; 当前目录中不存在 TransactionScenarios.cs.
- 触发条件: 主会话集中构建 FormEffectsProbe.csproj. 权威契约: 事务场景必须调用真实生产 patch 方法, 不复制 ledger/reservation/AsyncLocal 算法.
- 当前控制流: 现有 Program.cs 会注册 Void/Demon 等场景, 但没有事务场景源文件可供 csproj 编译; 缺失文件不能由现有链接项自动生成.
- 可复现命令: `Test-Path -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs'`; `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj'`. 仅文件核对, 未构建或测试.
- 最小修复范围: 当前用户已明确要求优先补齐 TransactionScenarios.cs; 需要新增该文件并继续只更新本报告. 尚缺证据: 补齐后的编译与场景执行由主会话完成, 本子代理不运行.
### 4. P1 当前生产链接编译阻断已精确定位
- 时间: 2026-09-30. 主会话已反馈中央直接链接构建到达生产 VoidFormPlayTransactionPatch.cs 后失败。
- 证据: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:76 使用 `nameof(CardModel.SpendResources)`; G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:190-195 的 CardModel 当前缺少该成员; 权威反编译 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1803-1820 给出真实签名 `public async Task<(int, int)> SpendResources()`.
- 触发条件: FormEffectsProbe.csproj 链接生产 VoidFormPlayTransactionPatch.cs 时解析 HarmonyPatch 的目标成员。
- 当前控制流: 生产 patch 仅把该成员用于 Harmony 元数据目标, 事务适配器直接反射调用私有 Prefix/Postfix/Finalizer; 探针尚未运行该 stub 的资源扣除逻辑.
- 最小修复范围: 在 ContractStubs.cs 的 CardModel 中加入参数为空、返回 `Task<(int, int)>` 的最小占位签名, 明确不伪造资源扣除; 同批补齐 csproj 已显式包含的 TransactionScenarios.cs.
- 可复现命令: 仅文件读取与中央构建反馈, 本批不重复运行构建; 静态复核命令为 `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs'`.
- 尚缺的实机证据: stub 签名补齐后的构建、事务场景执行和真实游戏资源支付均由主会话后续验证, 本批不运行.
### 5. P1 已补齐 CardModel.SpendResources 最小真实签名替身
- 时间: 2026-09-30. 已直接写入探针协作者, 未构建、未测试、未运行。
- 证据: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:196-198 现在声明 `public Task<(int, int)> SpendResources()`; 返回 faulted Task 并明确提示资源扣除未在探针中模拟, 不把零值冒充真实支付。
- 触发条件: 生产 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:76 的 `nameof(CardModel.SpendResources)` 目标解析。
- 当前控制流: HarmonyPatch 元数据可解析该成员; 事务场景应通过 TransactionPatchAdapter 直接调用生产私有 patch 方法, 不调用此资源 stub.
- 最小修复范围: 只改 ContractStubs.cs 的 CardModel 协作者, 未修改生产源码。
- 可复核命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs'` 后查看 190-199 行; 本批不执行构建或测试。
- 尚缺的实机证据: 中央构建需确认其余链接协作者签名; 该签名本身尚未由本批编译验证.
### 6. P1 TransactionScenarios 接入前的剩余协作者缺口已核对
- 时间: 2026-09-30. 静态核对未运行构建。
- 证据: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionPatchAdapter.cs:37 引用 `Journal.HistoryRequests` 与 `HistoryRequest`; G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs:202-206 通过 `CombatManager.History.PowerReceived` 绑定生产边界; G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:50-57 当前 CombatManager 没有 History; G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ProbeSupport.cs:19-41 当前 Journal 没有 history 记录。
- 触发条件: FormEffectsProbe.csproj 链接 DemonFormStrengthTransactionPatch.cs 或编译 TransactionPatchAdapter.cs。
- 当前控制流: 生产 Demon 事务要求深度 -> CombatHistory.PowerReceived -> PowerModel.SetAmount 的顺序; 纯属性补齐仍不足以把真实私有 patch 入口接到最小协作者。
- 最小修复范围: 在探针协作者加入 History 记录与 CombatManager.History, 在 PowerCmd/PowerModel 的对应边界调用现有反射适配器, 新建并注册 TransactionScenarios.cs; 不复制生产 ledger/reservation 算法, 不改生产 Forms。
- 可复核命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\*.cs' -Pattern 'HistoryRequests|HistoryRequest|CombatManager\.Instance\.History'`; 本批不执行构建、测试或运行。
- 尚缺的实机证据: 协作者边界接入后的中央构建与场景 PASS/FAIL 由主会话验证; 事务探针仍不证明真实 Harmony 注入。
- 增量: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ProbeSupport.cs:15,23,34,47 已补 HistoryRequest, Journal.HistoryRequests, 每场景 Reset 清空和失败快照计数. 只记录实际协作者调用, 不实现生产 history 订阅或持久化. 首次编辑命令在 PowerShell 解析时失败且未写文件, 随后的锚点替换成功; 不是构建或测试结果.
- 增量: ContractStubs.cs 已接入 CombatManager.History、PowerCmd.ModifyAmount 的真实生产深度 patch 调用、History -> SetAmount 连续顺序和 SetAmount 的生产 Prefix/Finalizer. 实际金额存储和事件先后仍沿用原窄协作者, 没有复制生产贡献账本; 新增边界不代表安装 Harmony 或完整引擎调度. 文件静态读取与中央编译尚待主会话复核.
### 7. P1 TransactionScenarios.cs 已补齐最短事务闭环
- 时间: 2026-09-30. 已写入 G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs:10-93, 未构建、未测试、未运行。
- 覆盖: :14-32 异步 SpendResources 保留 owner reservation 并在 BeforeCardPlayed 转移; :34-55 嵌套不同卡不得释放外层; :57-76 失败路径释放; :78-91 Demon 通过真实 `DemonFormPower.AfterApplied`、History 和 SetAmount 边界记录实际 round-one delta 并退出回滚。
- 权威契约: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1803-1820 与 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:231-241; 场景调用 TransactionPatchAdapter, 未复制生产 reservation 或 strength ledger 算法。
- 当前控制流: 场景只在探针最小协作者中直接调用链接生产私有 patch; 它不安装 Harmony, 不代表真实游戏 async 调度或实机支付。
- 尚缺实机证据: 需要主会话集中构建并运行 `FormEffectsProbe --filter transaction`, 结果可能暴露当前 Void patch 的异步/owner-token 反例; 本批按要求不执行。
- 增量: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\Program.cs:33 已注册 `TransactionScenarios.Register(suite)`, 因而 csproj 中的新增场景不是悬空源码. 本步仅写文件, 未构建或运行.
### CODE_COMPLETE
- 时间: 2026-09-30. 最短闭环已落盘, 当前没有构建、测试、运行、部署、git 或前台操作。
- 已修改文件:
  - G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:53,197-198,259-271,442-464. 补真实 `CardModel.SpendResources()` 签名, `CombatManager.History`, 生产 Demon depth/history/SetAmount 边界接线。
  - G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ProbeSupport.cs:15,23,34,47. 补 `HistoryRequest` 与 Journal 记录清理/快照。
  - G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs:1-93. 补四个事务场景, 直接走现有 TransactionPatchAdapter 与链接生产 patch。
  - G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\Program.cs:33. 注册 TransactionScenarios。
- 工程引用保留: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj:25 现在有对应源文件, 未删除引用。
- 主会话后续验证命令: `dotnet build G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj`; `dotnet run --project G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj -- --filter transaction`. 以上命令仅交主会话, 本批不执行。
- 未验证边界: 当前 Void 生产 patch 的 async completion 与 owner token 反例、Demon 写入后异常反例、真实 Harmony 安装、游戏运行、视觉、联机和存档仍待主会话验证; 本报告不宣称通过。
## 进行中

- 代码面已完成并落盘; 后续仅待主会话按上方命令集中构建和运行, 本批不执行.

## 未知

- 本轮没有编译或运行结果, 也没有视觉或交互验收.
- 回合, 多段, 异常, 多人和完整 RunState 往返不在当前已覆盖范围.