# effects 批次监督等待报告

## 已确认

- 记录时间: 2026-09-28 19:15:09 +08:00.
- 初始状态: WAITING_FOR_REAL_WAIT_GATE. 初次响应仅建立等待记录, 当时尚未开始源码监督.
- 已读取监督请求: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\effects-review.md`.
- 等待目标: effects 批次实现者. 其请求路径为 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\effects-worker.md`; 其实现报告路径为 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\effects-worker.md`. 门禁前这两份文件均未读取.
- 初始等待记录: 当时尚未收到主会话绑定的实际实现者 id, 也未收到针对该实现者的 `multi_agent_v1.wait_agent` 返回完成的真实证据. 文件存在, `CODE_COMPLETE` 和普通状态快照均不能替代该证据.
- 门禁前未读取或审查实现代码及实现报告, 未构建, 未测试, 未部署, 未操作游戏或共享配置, 未运行 git, 未再委派. 唯一写入文件为本报告.
- 请求指定模型为 `gpt-6-astra-ar`, 请求指定路由为 `gateway -> agentrouter -> gpt-6-astra`. 这些只是请求中的声明, 不是已核验的会话模型或路由元数据.

- 身份绑定更新(2026-09-28 19:19:15 +08:00): 主会话已明确绑定 `effects-worker=01a0e7b8-88c5-7781-8b77-1e884eef2e54`, `effects-review=01a0e7b8-8a66-70a2-a9aa-023412a5720c`. 该消息仅绑定身份, 不通过审查门禁.
- 身份绑定时门禁状态仍为 WAITING_FOR_REAL_WAIT_GATE. 尚未收到主会话提供的针对上述指定实现者的 `multi_agent_v1.wait_agent` 真实完成结果. 本次仅更新本报告, 未读取实现产物, 未审查, 未主动轮询, 未再委派.

- 门禁核对更新(2026-09-28 19:42:54 +08:00): 依据主会话本次明确通知及 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\effects-gate.json`, 已核对 `tool=multi_agent_v1.wait_agent`, `target=01a0e7b8-88c5-7781-8b77-1e884eef2e54`, `returned_status=completed`, `timed_out=false`, `reviewer=01a0e7b8-8a66-70a2-a9aa-023412a5720c`. 证据记录时间为 `2026-09-28T19:38:49.8835845+08:00`; `tool_call_id=not-exposed`, 不虚构调用编号.
- 当前门禁已通过, 状态为 SOURCE_REVIEW_IN_PROGRESS. 从此更新之后才读取最终生产代码及实现报告. 本轮只作源码监督, 不审查已移交探针目录, 不构建或测试, 不运行 git, 不改代码, 不再委派.

### 增量 A - 实现报告与监督范围

- 已读取 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\effects-worker.md:27-44` 及 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\effects-worker.md:1-110`. 当前生产审查范围确认为六个指定效果文件, 可选辅助文件未创建; 已移交探针不在本次审查范围.
- 报告证据口径: 实现报告 :3, :69, :87, :108 明确没有构建, 测试或游戏运行, :89-100 只是给主会话的后续命令. 此处没有把 CODE_COMPLETE 冒称为运行通过. :26, :56 声称只读反编译当前测试 DLL, 本监督尚未独立核验该静态取证过程.
- 实现报告 :62, :109-110 主动标明授予前 hook 重入, 直接 SetAmount 来源清理, 自动/重放嵌套, X 费用, 异常和取消等边界未验证. 这些声明不是实现正确性的证据, 后续按生产控制流检查.
- 探针是否真实链接生产代码不在本次已确认结论内. 未读取, 未执行任何已移交探针; 实现报告 :54, :69 明确本实现者没有探针产出.

### 增量 B - 虚空与费用支付链

- 已完成 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:1-121` 的源码检查. 正常 OnPlay 内入场的进入牌没有本实例的 Before, 不会由它的 After 消费; 重放后续次数被 IsFirstInSeries 拦住, 自动出牌被 IsAutoPlay 拦住. After 用同一个 CardPlay 引用结算, 不依赖被转移后的 Card.Owner. 这些是源码结论, 没有运行证据.
- X 费用保留引擎语义: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Cards\CardEnergyCost.cs:134-140` 与 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1556-1566` 对 X 费用直接取资源量; 本实现没有另造 X 算法. 不能从普通免费 hook 推断 X 牌不支付资源.

#### F1 - P2 - 费用已支付但尚未预留时仍可重入使用免费额度

- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:76-86`, 关联 :43-49, :92-99.
- 触发条件: 同一所有者在第一张手动牌 A 的费用支付后, BeforeCardPlayed 前的 awaited hook 中重入另一条手动费用支付链; 或在这个窗口中新获得虚空. 本轮未找到或运行一个当前原生消费者来证明该窗口在正常牌组中必然出现, 不扩大为所有正常手动出牌都失败.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:34-35` 只允许下一张手动系列使用一次额度, 进入牌不能追溯消费新额度.
- 当前控制流: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs:92-103` 先 await SpendResources 再 OnPlayWrapper; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1807-1833` 在读取免费能量和星星费用后仍 await AfterEnergySpent, :1866, :1887 又有入牌堆和重放计数 hook, 到 :1926 才调用 BeforeCardPlayed. Void 在这之前始终 pendingFreePlay=null. 因此 A 和重入 B 可各自读取免费费用; 若虚空在支付后才入场, 已付费的 A 又会在 Before 预留并在 After 消费新额度. :85 的注释只覆盖预留之后的嵌套, 不覆盖完整支付事务.
- 可复现命令与边界: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs','G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs','G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs' -Pattern 'pendingFreePlay|SpendResources|AfterEnergySpent|BeforeCardPlayed'` 可重新定位本源码证据, 不是行为测试. 行为反例需要中央验证在 AfterEnergySpent 中插入一次受控同所有者重入或入场, 本监督未执行且未检查探针是否已有该用例.
- 最小修复范围: 给免费额度增加与真实手动支付事务绑定的预留及完成/取消身份, 时点须早于支付链第一个可重入 await, 不能在反复调用的费用预览 getter 内盲目消费. 保留实际 CardPlay 首份/完成配对. 若需要窄支付入口适配, 由 effects 和 integration 明确接口, 不用卡牌永久黑名单代替.
- 尚缺实机证据: 支付 hook 内入场或重入, 双资源牌, 费用预览, 支付异常/取消, 自动和重放嵌套的当前 DLL 行为. 本项为条件明确的源码控制流缺口, 不是实机复现.

### 增量 C - 恶魔授予账本

- 已读取 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:1-231` 及 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:71-163, :219-298`. 目标 previousTarget 与实收 grantedStrength 分离, 普通阻断和数值倍增不再盲记请求 delta. 但现有 Strength 分支仍不能把授予前重入的来源分开.

#### F2 - P1 - 授予前 hook 的其它力量被记入形态账本并在退出时吞掉

- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:107-113`, 关联 :198-224.
- 触发条件: 所有者已有 Strength; 本形态授予时, 一次性 BeforePowerAmountChanged hook 通过正常 PowerCmd 对同一个 Strength 额外授予其它来源力量. 不需要直接改字段或绕过 PowerCmd.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:41-42` 要求账本只记录本形态实际接受的增量, 退出不吞其它力量. 实现报告 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\effects-worker.md:62, :110` 已承认此边界未解决, 不能把未知声明视为契约豁免.
- 当前控制流: Demon 在 await ModifyAmount 前读取 before. `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:231` 先 await BeforePowerAmountChanged, :240 才用 hook 返回后的 power.Amount 加 modifiedOffset, :274 返回 newAmount. 因而返回值减旧 before 包含前置 hook 的其它来源改动. 示例: 原有力量 10, n=1 的本形态申请 +1, 前置 hook 一次性额外 +5. 外层返回 16, 形态账本记 6 而非 1; 退出直接减 6 后回到 10, 应保留的其它来源 +5 丢失. refreshing 只防止 RefreshStrength 自身递归, 不隔离第三方正常 Strength 命令.
- 可复现命令与边界: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs','G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs' -Pattern 'int before|resultingAmount|grantedStrength|BeforePowerAmountChanged|int newAmount|return newAmount'` 可重新定位源码链. 行为复现应由中央以一次性前置 hook 构造 10,+5,+1,退出序列并断言最终 15; 本监督没有运行此用例, 没有检查已移交探针.
- 最小修复范围: 只给本次授予事务记账, 捕获其真正 SetAmount 边界的前后差或等价的已归因 accepted offset; 不能再用跨 await 的聚合 before/after 估算. 如需窄引擎适配, 接入目标是 `PowerCmd.ModifyAmount(PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?, bool)` 内实际写入边界, 同时区分前置/后置及嵌套命令. 不用退出时再裁剪账本掩盖归因错误.
- 尚缺实机证据: 当前 DLL 的一次性前置重入, 倍增与阻断组合, 0 值移除再加回, 授予中退出及异常路径. 本项是确定控制流下的源码反例, 不是实机复现.

### 增量 D - 直接回滚 API 与重入边界

- 已核对 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:478-488, :542-579` 与 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Creatures\Creature.cs:606-655`. SetAmount 确实限幅, 修改存储量并同步触发 DisplayAmountChanged 和 Owner 的数量/UI 事件; 它不调用 Before/AfterPowerAmountChanged, 授予/接受倍率 hook 或历史 PowerReceived. ApplyInternal 在 SetAmount 后挂载实例, 不是完整 PowerCmd.Apply 生命周期.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:191-228` 的清理是有意绕过授予类 hook 的来源撤回. 这能避免把撤回再次交给 Artifact 阻断或倍率放大; 原生 Artifact 的阻断路径在 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\ArtifactPower.cs:17-40`. 不能将直接 SetAmount 简单描述为完全不发通知, 也不能描述为与正常 PowerCmd 生命周期等价.
- 正常同步清理下, cleanupComplete 和账本清零先于任何 await, 0 值后还复核实例引用才调用 Remove. 因而重复 AfterRemoved 不会重复扣减, 同步回调若把同一实例改回非零或替换为另一实例, 最后的 0 值移除检查不会主动移除新实例. 没有把这些源码守卫称为实机通过.
- 仍有失败事务缺口待中央确认: 现有 Strength 分支只有 await ModifyAmount 正常返回后才记账 (:108-112). 若实际 SetAmount 已完成, 但 `PowerCmd.cs:244-272` 的后置 hook 或等待抛异常, 本次真实增量不会入账; 外层 finally 也只能撤回此前账本. 与首次 Strength 分支 :151-158 的 finally 记账不同. 此项目前作为异常路径边界单列, 未运行故障注入, 不冒称已触发的原生游戏故障.
- SetAmount 的 DisplayAmountChanged 和 Owner 数量事件是同步回调, 并非无重入点. 未穷举回调内再次移除, 重新挂载或抛异常, 也未将绕过 Hook 的所有第三方观察者兼容性视为通过. F2 的授予前重入归因错误已具确定源码反例, 不能因清理一次门正确而消除.
- 当前测试 DLL 的只读 SHA256 已由本监督独立取得, 为 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`, 与实现报告一致. 对象是 `E:\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`. 这只核对二进制身份, 不等于本监督已独立证明所有 research 源码与该 DLL 逐方法一致.

### 2026-09-30 恢复记录 - 仅接收后增量审查

- 接收时间: 2026-09-30 19:40:32 +08:00. 已读取恢复请求 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\effects-review-resume-20260930.md:1-34`.
- 原 `effects-gate.json` 只作为原 effects-worker 批次的完成证据. 2026-09-30 其它用户授权会话修改的 VoidFormEffectPower.cs, DemonFormPower.cs 及新增的 VoidFormPlayTransactionPatch.cs, DemonFormStrengthTransactionPatch.cs 仅按恢复请求授权作接收后只读增量审查, 不宣称它们已通过原实现者完成门禁.
- 原 F1/F2 及增量 B/C/D 中的实现行号与结论是 2026-09-28 检查时点证据, 不是当前四文件的自动结论. 本次将读取当前版本后重新给出准确路径, 行号及是否仍有反例.
- 当前状态: INCREMENTAL_SOURCE_REVIEW_IN_PROGRESS. 有界完成 Serpent/Echo/Celestial/Reaper 与 clone 检查, 复核当前费用支付事务与力量事务. 不读取 probe 批次测试源码, 不构建或测试, 不改代码, 不运行 git, 不再委派. 唯一写入路径仍是本报告.
- 最终门禁仍待后续返工由原实现者接收并真实 wait 完成后再作配对监督. 本增量不能替代该监督; Release 编译成功也不是行为通过证据.

### 2026-09-30 增量 E - 当前虚空支付补丁, F1 仍未关闭

- 检查时点: 2026-09-30 19:42:10 +08:00. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs` SHA256=`6915D151BCD36A521B77789955D7BDC9BE62BB32CF79C246F775BA2E0AD86B1F`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs` SHA256=`E623C63A791802A40023F2D4F33724D7C51B50F45BEDEBCDECE908FBBDACC748`. 契约仍为原 :34-35. 以下为当前版本证据, 不沿用旧实现行号.

#### F1 当前复核 - P2 - 预留在 Task 返回时释放, 且嵌套退出没有所有权

- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:37-67, :79-95`; 关联 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:114-130`.
- 触发条件 A: 手动牌 A 的 SpendResources 在 awaited hook 上异步挂起, 稍后该 hook 的继续执行重入另一条支付链 B. Prefix 预留 A, 但 Postfix 不查看或包装返回的 Task, 它在原方法返回未完成 Task 时就 End 并清空共享 Data.reservedCard. B 因此可再次拿到额度. 即使支付 Task 同步完成, 释放到 OnPlayWrapper 的 BeforeCardPlayed 之间仍有可等待的入牌堆和重放计数调用, 不是已经有 pendingFreePlay 保护.
- 触发条件 B: A 的同步支付 hook 内先进入 B. B 的 Begin 因 ReservedPower 已存在而直接返回 (:39-41), 但 B 的 Postfix/Finalizer 仍无条件 End (:85-95), 会释放属于 A 的预留. hook 随后再进入 C 时 C 又能免费. B 不需要自己拥有额度, 也不需要发生异常.
- 触发条件 C: 支付开始时没有该 Void 实例, 在支付 hook 中新入场. Begin 没有任何历史资格记录, 当前 Void.BeforeCardPlayed :127 又允许 reservedCard==null, 因而已付费的进入牌仍可预留并在 After 消费新额度. 当前新增字段没有消除旧 F1 的支付期间入场反例.
- 契约/当前控制流: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:34-35` 要求一次手动系列一次免费且进入牌不追溯消费. 引擎链仍是 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs:92-103` 先 await 支付, 再进入包装器; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1817-1833, :1866-1887, :1926` 表明实际完成和 Before 均晚于同步 Task 返回. 当前补丁只包裹同步调用, 没有 Task 完成观察, 没有逐调用 __state 所有权, 没有支付资格向 CardPlay 的身份转移.
- 可复现命令与边界: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs','G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs' -Pattern 'Begin\(|End\(|HarmonyPostfix|HarmonyFinalizer|ReservedPower|ReleaseReservation|reservedCard == null'` 可定位当前证据. 行为反例应由中央分别控制一次真实异步暂停, 一个 A/B/C 同步嵌套序列, 以及支付中入场; 本监督未运行测试, 未读取 probe 源码.
- 最小返工范围: 限于这两个文件和明确必要的真实手动系列接入. Begin 必须返回逐调用所有权令牌, 未预留的嵌套调用不能 End 外层令牌; 必须区分 Task 返回, Task 完成, CardPlay 开始/完成并处理异常/取消, 将支付资格转交给真实系列. 新入场实例不能凭 reservedCard==null 获得已经支付的系列资格. 不在费用预览 getter 内消费, 不用永久 CardModel 排除表替代.
- 尚缺实机证据: 当前 Harmony Task 绑定, 实际异步/同步嵌套时序, 支付中入场, 支付后自动出牌的真实调用者. 此为接收后源码反例, 没有通过后续返工门禁, 不声称实机复现或行为通过.

### 2026-09-30 增量 F - 当前力量事务首次核对

- 检查时点: 2026-09-30 19:47:18 +08:00. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs` SHA256=`586711A7B85904CDB3D6E67A6757BB0EE3DE5BE9601D36EA542CA8EA36D8C660`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs` SHA256=`286BDBDBA865A18A16EFD4DEE67D2044F23AC127C39FBB51610D4B648396C945`.
- 当前 DemonFormPower :113-119 已移除旧的跨 await before/resultingAmount 减法, 改经 ModifyAmountAsync 回调记账. 因而不能直接把旧 F2 的 :107-113 当作当前错误行号.
- 当前补丁 :49-70 建立 AsyncLocal Scope 并在 finally 恢复上层身份; :77-110 对同一 Power 的嵌套命令计深度, :193-197 将返回 Task<int> 包装为等待真实完成后减深度, 不是像当前 Void 一样在同步 Task 返回时释放. :118-144 在同一实例的 History 后置点设 Armed, 然后在 SetAmount 前取 Before; :155-175 记录该写入请求的限幅差值.
- 这些是已读取代码的结构事实. 正在对照真实生产 API 顺序核算旧 F2 的一次性前置 +5 与授予 +1 反例, 以及写入后异常; 尚未宣布 F2 通过, 未执行任何 Harmony 绑定或运行验证. 当前两文件仍只属于接收后增量审查, 后续原实现者返工门禁不因此省略.

### 2026-09-30 增量 G - F2 原反例复核与写入后异常

- 已重新读取 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:219-275` 及 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:542-571`. 外层 History 后确实紧接 newAmount 计算和 SetAmount; 普通后置 hook 在 SetAmount 返回之后.
- F2 原有的一次性前置 +5, 本形态 +1 源码反例不再成立, 前提是新增三个 Harmony patch 均真实绑定: 内层 ModifyAmount 的 Depth=2, 不会 Arm; 内层 Task 完成后深度回到 1; 外层 History 后 Arm, SetAmount 前取到 15, 请求 16, 只记 +1. 这不是运行通过, 也不等于所有来源事务已经验证.
- 旧增量 D 中普通 AfterPowerAmountChanged 等后置 hook 在 SetAmount 返回后抛异常而漏记的问题, 在上述补丁绑定前提下也已由提前 Record 改进. 但 SetAmount 自身的同步事件仍有下面的明确反例.

#### F3 - P2 - 写入事件重入改值后抛异常时, 已完成的形态写入仍被丢弃

- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs:155-175`, 尤其 :163-168; 关联 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:113-119, :204-216`.
- 触发条件: 本次 SetAmount 已写入形态增量, 其同步 DisplayAmountChanged 或 Owner 数量事件中发生一次受控的其它来源数值变更, 然后该回调抛异常. 这是针对当前补丁自己声明支持的写入后异常/同步回调边界的最小故障注入场景, 不宣称某个现有第三方 mod 已经触发它.
- 宣称/契约: 当前补丁 :14-22, :148-153, :218-220 声称已完成写入在后续失败时仍入账; `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:42` 要求准确记录实际接受的形态增量, 退出不留负漂移或吞其它来源.
- 当前控制流: 引擎 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:549-551` 先写 _amount, 再同步通知. 以原力量 10, 形态申请 +1 为例: 外层确实写到 11, 一次性通知回调再将其它来源 +5 写成 16 并抛异常. Finalizer 得到 exception!=null 且 power.Amount=16 不等于 requested=11, :165 wrote=false, 但 :163 已设 Captured=true, 随即返回不记录 +1. 后续退出账本为 0, 残留总量 16 而不是应保留的 15. 这不是跨 await 旧 F2, 而是以异常后的聚合数值倒推是否已写入导致漏账.
- 可复现命令与边界: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs','G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs' -Pattern 'Captured = true|bool wrote|power.Amount == requested|_amount = amount|DisplayAmountChanged'` 可重新定位源码证据. 中央故障注入可用一次性同步数量回调执行 +5 后抛出, 再退出形态断言 15. 本监督没有运行此代码, 不读取 probe 源码, 不把该推演称为真实运行.
- 最小返工范围: 该事务补丁的实际写入收据. 在真实存储修改完成且尚未通知可重入回调的边界记录已写入及其独立差值, 保持 Scope/Target/深度身份; 不把异常后的最终聚合值是否等于请求值当成已写入的充分必要条件. 对尚未执行写入的失败与写入后失败分别处理.
- 尚缺实机证据: 当前 Harmony 绑定及 finalizer 异常传播, 实际写入点捕获, 回调内重入和故障注入, 后续退出是否保留其它来源 +5. 当前仅为接收后源码缺口, 仍须原实现者接收返工并经新 wait 后最终监督.

## 进行中

- 2026-09-30 按恢复请求继续接收后只读增量审查, 原六效果未完成的四文件与 clone 有界收束.
- 当前四个外部修改文件的 F1/F2 复核进行中, 不把旧行号当当前事实, 不把原门禁扩大为外部修改已获最终监督.
- 重点: SpendResources 返回 Task 与异步完成的区别, 嵌套预留释放, 支付期间入场; Demon AsyncLocal 事务身份, 命令深度, History/SetAmount 捕获及写入后异常.

## 未知

- 实际实现者 id 及真实等待完成结果已确认; 最终实现的正确性尚未审查.
- 截至恢复时, Serpent/Echo/Celestial/Reaper 与整体 clone 审查尚未完成; 09-30 四个外部修改文件尚待增量复核.
- 测试是否链接生产代码, 实现报告的运行声明是否有真实证据均未核验.
- 当前会话实际解析的模型 id 及 provider 路由未从会话元数据核验.
- 没有源码监督通过结论, 没有实机验收结论. 本等待报告不代表实现正确或通过验收.
