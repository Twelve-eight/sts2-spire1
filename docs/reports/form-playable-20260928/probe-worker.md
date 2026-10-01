# form-effects-probe 增量交付记录

状态: IN_PROGRESS. 本文件仅记录源码实现与静态阅读, 不代表构建或运行通过.

## 已确认

- [2026-09-28 19:33:18 +08:00] 已读取本轮请求与工作区入口. 写集严格限定为 G:\omp works\Sts\sts2-spire1\tools\form-effects-probe 下新增源文件和 csproj, 以及本报告. 不改六个生产 Power, 不运行 git, 构建, lint, 测试, 部署或游戏, 不再委派, 不写 C:.
- [2026-09-28 19:33:18 +08:00] 请求要求同一 net9.0 探针通过 FormsSourceRoot 与 Compile Link 分别消费 baseline 和当前生产源码, 均定义 SPIRE1_FORM_MOD. baseline 缺少 GrantExitEnergy 必须形成运行断言失败, 不能造成静态成员编译失败.
- [2026-09-28 19:33:18 +08:00] harness 为当前 Codex 会话. 请求指定模型 gpt-6-astra-ar, 请求路由 gateway -> agentrouter -> gpt-6-astra. 当前工具未提供可核实的模型与 provider 会话元数据, 不将请求文字作为实际路由证明. 本会话未启动其它代理或模型调用.

### 首次源码读取 - 2026-09-28 19:36:05 +08:00

- P1, 编译链接契约: G:\omp works\Sts\sts2-spire1\tools\event-removal-probe\RemovalProbe.csproj:7-18 使用显式 Compile Include/Link. 新探针将只链接指定六文件, 不引入生产项目引用, 外部 NuGet 或 Godot. 依据 G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:30-59 编写行为断言, 不把现有实现当期望来源.
- P1, Void: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:76-112 以 pendingFreePlay 引用串联 Before/After 并按 participants 重置. 触发面为实例在进入牌 OnPlay 后出现, 以及同 CardModel 后续重打. 最小探针范围是直接 hook 与费用查询, 不模拟资源扣费全链.
- P1, Serpent: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:49-106 先从 startedPlays 移除再 await, 无目标先返回, 退出由 GrantExitEnergy/exitHandled 门控. 用实际 CardPlay 身份, 受控 CombatTargets 与命令记录器检查次数和目标序列. 新属性仅反射访问, 保证 baseline 仍可编译.
- P1, Demon 力量: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:76-172 区分 previousTarget 与 grantedStrength, 初次 Apply 监听 DisplayAmountChanged; :191-229 使用 SetAmount/ApplyInternal 撤回. 探针必须按引擎返回值, 钳制与事件时序实现协作者, 不把 delta 自动当实际接受量.
- P1, Demon 伤害: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:174-179 同时限定 target, dealer 与阵营, 不排除 Unpowered. 直接调用 additive hook, 对敌方普通/Unpowered 及自伤/友方/null/其它目标分别断言.
- P1, Echo: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs:42-65 保留已有 playCount 并由 AfterModifying 消耗. 探针必须保持 GeneratePlayCount 先于 OnPlay 的事实, 不伪造进入牌已持有新效果的顺序.
- P1, Celestial: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs:38-53 在支付前设置 granted, RoundNumber 与 3 取最大值. 探针记录两种命令的实际入参, 不预填实际结果.
- P1, Reaper: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs:35-44 检查本人/宠物, IsPoweredAttack 与 TotalDamage. 协作者要真实区分 TotalDamage 与 UnblockedDamage, 并记录 Doom 目标和数值.

以上为 2026-09-28 19:33:53 +08:00 读取快照, 不是构建/隔离复现/实机证据. 复现命令与最终源文件行号将在实现后记录. 尚缺所有实际执行结果及真实桥接, 视觉, 联机和存档证据.
### 协作者契约与链接工程已落盘 - 2026-09-28 19:42:12 +08:00

- 已新增 G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj. 明确 net9.0, 六个 Compile Link, FormsSourceRoot 可切换, SPIRE1_FORM_MOD, 无 PackageReference, 关闭共享编译器与 NuGet 审计, restore 源限定探针本地目录. 未执行 restore 或 MSBuild.
- baseline 六文件已只读: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms. Void:94-99 在缺失 Before 时仍按末次手动 After 消费; Serpent:42-65 只处理末次非自动, 没有 Before 身份门; Demon:81-92 记录请求 delta, :111-117 用负 Apply 撤回. 这些仅为源码差异, 还没有任何运行复现.
- 引擎依据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:542-597 明确 SetAmount 钳制并先触发 DisplayAmountChanged, ApplyInternal 在非零时绑定 owner/写数值/加入集合, RemoveInternal 保留 Owner, clone 重新调用 InitInternalData 并清空 owner/事件. 替身不会用构造一个全新形态来伪造 clone 通过.
- 引擎依据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:105-161 与 :219-274 明确授予前修改 hook, 先存储后通知, ModifyAmount 返回存储前算出的未钳制 newAmount 而不是下游 hook 后数值. 被修改为 0 的新实例不会 ApplyInternal, generic Apply 的实际实现仍可能返回未附着实例; 不用注释中的可能 null 推断替身行为.
- 引擎依据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Relics\RuinedHelmet.cs:32-59 首次正力量增量乘 2, 于 AfterModifying 标记已用. 拒绝授予与阻止负增量将由独立接收修改 hook 注入, 不是把期望力量直接塞进 Power.
- 引擎依据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1887-1965 与 :2029-2034 为先 GeneratePlayCount/AfterModifying, 后按每次新 CardPlay 依次 Before/OnPlay/After. CardPlay.Player 与当前 Card.Owner 可以不同, 依据同目录上级 MegaCrit.Sts2.Core.Entities.Cards\CardPlay.cs:18-23.
- 引擎依据: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:2520-2535 的 additive 分支不按 Unpowered 跳过. DamageResult.cs:63 的 TotalDamage 是 BlockedDamage + UnblockedDamage. RNG 只模拟确定性输入与消费序, 不宣称复刻 MegaRandom 种子算法或多人同步.


- [2026-09-28 19:44:16 +08:00] 已落盘 ContractStubs.cs. 实现受控 CardPlay 身份, DamageResult 总伤害, PowerModel 的 MemberwiseClone/InitInternalData 和数值生命周期, 接收修改 hook, 命令调用记录与脚本 RNG. 能量/抽牌/伤害仅记录生产调用入参, 不伪造玩家资源, 牌堆或 HP. 目前尚未编译, 正继续实现驱动和各场景.


- [2026-09-28 19:46:32 +08:00] 已落盘 ProbeSupport.cs 与 Program.cs. 场景独立重置记录器, 从执行结果动态累计 PASS/FAIL/TOTAL, 任一失败返回非零, 超时中止后续场景且标明未执行. --filter/--list 支持主会话分面运行; 每次打印编译时 FormsSourceRoot. RuinedHelmet 规则在 AfterModifying 才消费, 拒绝正/负力量为独立规则. 未运行程序或任何检查器.


- [2026-09-28 19:47:47 +08:00] VoidScenarios.cs 已落盘. 覆盖仅进入牌 After, 进入牌剩余重放, 同 CardModel 后续手动系列, 本人/他人回合, 自动/他人打牌, 两类费用及牌堆边界, pending 期间嵌套自动打牌, CardPlay.Player 与 Card.Owner 分离, 重复 After. 依据契约 :34-35, 不尝试模拟 X 费用扣除与引擎手动调度; 当前只有源代码, 未运行.


- [2026-09-28 19:49:51 +08:00] SerpentScenarios.cs 已落盘. 覆盖缺 Before, 手动/自动各重放索引, 相同对象重复 Before/After, 同 CardModel 新对象, await 期间去重, CardPlay.Player 所有权快照, 空敌人零 RNG 调用, 固定序列的目标与调用轨迹, 战斗边界与退出 2 能量一次. GrantExitEnergy 仅反射验证 public bool getter/setter 和默认值, false 时禁止命令调用; baseline 缺属性会成为运行断言失败而非编译失败. 未执行任何场景.


- [2026-09-28 19:53:13 +08:00] DemonScenarios.cs 已落盘. 力量覆盖 n=1/2/3 入场与增量, 原有/后加永久力量, 首次正增量乘 2 的新建和既有力量路径, 拒绝授予及下一回合, 退出不可被新 debuff 门拦截, 零聚合恢复, 非零清除, 存储钳制, 下游独立加成, 授予 hook 内移除. 伤害直接检查敌方普通/Unpowered/组合与无标志路径 +n, 自伤/友方/本人宠物/null dealer/其它或空目标不加. 这些是可执行源码设计, 尚未证明 baseline 残留/负漂移已实际复现或新代码已通过.


- [2026-09-28 19:56:35 +08:00] EchoScenarios.cs 已落盘. 检查已有 playCount 的 +1, 查询不消费, AfterModifying 单次消费, 他人不消费, 非正数/上溢边界. 进入牌场景采用明确标注的窄顺序驱动, 在 OnPlay 新建 Echo 前生成次数与修改者列表, 不反向伪造原生调度. 真神格承载和 Watcher 桥接仍未覆盖, 未运行.

### 2026-09-30 恢复接收 - 2026-09-30 19:41:44 +08:00

- P1, 已读取 G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\probe-worker-resume-20260930.md:44-46. 白名单与禁令不变, 不再从头研究, 不构建/lint/测试/git/部署/游戏, 不再委派.
- 19:40:36 +08:00 文件快照确认现有 8 个探针源码/工程文件仍在, Program 所列 CelestialReaperScenarios.cs 与 CloneScenarios.cs 尚未落盘. 优先补齐这两个实现面.
- 当前生产 VoidFormEffectPower.cs 与 DemonFormPower.cs 的 mtime 为 2026-09-30, 不再把 2026-09-28 报告中的控制流当当前事实. 新事务依赖必须链接真实实现或明确列为未覆盖, 禁止空桩或复制算法后宣称验证.
- 当前缺口仍是实际编译和执行证据. 本会话只完成源码收束, 集中运行由主会话负责.

- [2026-09-30 19:44:28 +08:00] CelestialReaperScenarios.cs 已补齐. 天人覆盖 1/3/5 回合, 重复与异步入场, 无玩家/无战斗/结束边界. 死神覆盖本人/宠物, 全格挡和部分格挡 TotalDamage, 非有源/他人/null dealer/零伤害门, 非 Attack 牌来源和多段各目标. 没有运行.
- [2026-09-30 19:44:28 +08:00] 新生产快照已读: VoidFormEffectPower.cs:119-143 仍可仅凭 Before 将当下未消费额度绑定到已付费牌; VoidFormPlayTransactionPatch.cs:79-95 目前在 Task 返回的 Postfix 中立即 End, 不是等待异步完成. DemonFormPower.cs:114-119 依赖真实 DemonFormStrengthTransaction; 对应补丁 :49-176 在修改深度/History/SetAmount 边界记录实际写入. 上述为源码控制流, 不称已复现.
- [2026-09-30 19:44:28 +08:00] 收束方案: 保留六 Power 直接 hook 场景, 条件 Compile Link 两个现有事务源, 在最小引擎协作者边界直接调用生产补丁入口. Harmony 属性只保留编译元数据, 不安装或模拟 Harmony 注册器; 不写空的 Void/Demon 事务实现. 单独标注这类受控边界场景不证明真实 Harmony 注入, 引擎异步调度全链或任意第三方并发.

- [2026-09-30 19:48:35 +08:00] CloneScenarios.cs 已补齐, 当前 Program 原先引用的场景文件不再缺失. 每个 Power 都以 MemberwiseClone 和生产 InitInternalData 检查实例隔离, 特别检查 Void pending, Serpent startedPlays, Demon 实际贡献账本, Echo/Celestial 消费状态. Reaper 按无内部数据的真实实现验证 owner 重绑定, 不凭空补集合. 不覆盖原生 DynamicVars, 存档或网络序列化; 未运行.


- [2026-09-30 19:53:34 +08:00] 事务适配首次写入命令在 PowerShell 解析阶段因字符串转义失败, 命令未执行, 该次未写入任何源文件. 这是编辑命令失败, 不是编译/测试结果. 改为分块 here-string 写入继续收束.


- [2026-09-30 19:53:34 +08:00] TransactionPatchAdapter.cs 与两个真实事务文件的条件 Compile Link 已落盘. 适配器只反射执行生产补丁方法, baseline 缺类型时按无补丁运行, 不复制账本或预留算法. Harmony 属性仅保存编译元数据, 没有真实补丁安装行为. 正将协作者边界接入, 未运行.

## 进行中

- 正在读取本轮行为契约, 六个生产 Power, event-removal-probe 链接方式及必要引擎实现. 每完成一个实现面即追加记录, 最终交主会话集中编译与运行.

## 未知

- 构建结果, PASS/FAIL 与实际场景总数均未产生. 所有后续运行命令仅供主会话执行.
- 最小替身不能证明真实神格桥接, 引擎调度全链, 视觉, 联机, 存档或实机行为.
- 六个生产 Power 正由其它工作者修改. 本报告会记录读取时点, 主会话须基于整合后的源码统一适配编译问题.
