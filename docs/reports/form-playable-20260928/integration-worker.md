# integration 实现增量报告

## 已确认

- 2026-09-28 接收任务. 证据: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\integration-worker.md:1. 仅编辑精确白名单与本报告, 不构建/lint/测试/部署/git, 不运行游戏, 不修改共享配置, 不再委派. 六效果 Power 和 MainFile.cs 属其它并发任务, 不写入.

- 本轮契约已读取: G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:19-28,47-58. 模式按 run modifier 隔离, 必须保留外部 Watcher 的真实姿态 Power 和通知链, 默认编译形态且不动 AFTP 门. 术语依据已读取 G:\omp works\docs\terminology-glossary.md, 使用平静/愤怒/神格/力量/格挡, 六形态名与灾厄依据本轮契约. 这些是契约证据, 不是运行证据.

### 1. P1 - 默认构建缺少可达且隔离的形态入口

- 源码证据(修改前): G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:9-29 由 SPIRE1_FORM_MOD 包围; G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:18-53 只查询和进入内部姿态, 不识别每局修正; G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\RushdownPower.cs:25-30 只接受 WrathPower 精确继承身份. G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:54-96 已由默认 glob 编译代码, 本任务无需修改构建文件即可纳入去门后的 Forms, AFTP 排除原样保留.
- 触发条件: 默认构建, 或外部 Watcher 正常姿态卡进入姿态. 契约: 本轮 DEVELOP:19-28 要求自定义对局手选项, 普通局不变且同局无两套叠加.
- 最小修复: 去除本人四文件条件门, 新增可选桥接和自定义修正, StanceCmd 按逻辑姿态路由, Rushdown 按逻辑愤怒判定.
- 复查命令(只读, 非运行复现): Select-String -Path "G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\*.cs" -Pattern "SPIRE1_FORM_MOD". 尚缺编译, 手选 UI, 普通局/模式局实际对照证据.

### 2. P1 - 姿态身份与神格退出通知不满足契约

- 源码证据(修改前): G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidSerpentStancePower.cs:22-40, DemonReaperStancePower.cs:21-39, EchoCelestialStancePower.cs:22-47. 三者 StanceName 不是 Calm/Wrath/Divinity; 神格 AfterPlayerTurnStart 无入场回合保护且直接 Remove(this), 绕过 StanceCmd.Dispatch.
- 触发条件: 内部形态路径或在回合开始 hook 内进入神格. 契约: 本轮 DEVELOP:47-57 要求下一次自己的回合开始经真实姿态通知退出, 状态按 owner 隔离.
- 最小修复: 承载层使用逻辑姿态身份, 记录入场回合, 经 StanceCmd 退出; 外部真实标记由追踪 Power 成对管理, 不复制六效果实现.
- 复查命令(只读, 非运行复现): Get-Content -LiteralPath "G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoCelestialStancePower.cs". 尚缺引擎 hook 阶段与联机/战中重连实机证据.

### 3. P1 - 外部 Watcher 原行为必须在模式内窄替换

- 当前 DLL 反编译证据: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-Calm-current.cs:31-38 退出自己支付 2 能量; watcher-Wrath-current.cs:39-52 攻防原始倍伤为 2; watcher-divinity-current.cs:28-38 异步入场支付 3 能量且附 VFX, :49-68 攻击倍伤为 3 且原本在回合结束退出.
- 触发条件: 模式局通过外部 Watcher 原卡牌进入平静/愤怒/神格. 契约: 不双发能量, 不叠倍伤, 不破坏 VFX, 神格延至下一自己的回合开始.
- 最小修复: 保留 Calm.AfterRemoved; 外部群蛇 mutable 实例设置 GrantExitEnergy=false. 私有 bridge Harmony id 统一校验/安装/回滚, 仅改 Divinity.AfterApplied 状态机唯一 GainEnergy 调用, 模式外保留原调用.
- 复查命令(只读, 非实机复现): Select-String -LiteralPath "G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-divinity-current.cs" -Pattern "GainEnergy|AfterSideTurnEnd|ModifyDamageMultiplicative". 尚缺实际 Harmony 绑定和 VFX/能量通知实机证据.

- 自定义修正研究面: G:\omp works\Sts\sts2-spire1\research\BaseLib-StS2\Abstracts\CustomModifierModel.cs:10-50 支持 ModifierAlignment.None, 可以避免 GoodModifiers/BadModifiers 随机池. 后续用原生手选列表窄后缀加入.

- 外部通知链已核对: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-WatcherCombatHelper-current.cs:537-579 先移除旧姿态再挂真实 Power, :594-627 用真实 Type 分发 Rushdown/MentalFortress/Flurry/VioletLotus. 将包装原异步返回 Task, 不替换 Type 或复制其分发. 另发现 :529-533 的 EndTurnSafely 多人非出牌阶段后备分支直接移除 Divinity, 正在纳入窄绑定审查, 不把回合结束钩子作为唯一退出面.

- 补充授权已记录: 允许修改 G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json 中本次 Forms 的精确类型/命名空间禁止项, 保留 Experimental/Debug/AFTP held-back 和 Watcher/AutoAnthony 硬引用门. 同批监督 id=01a0e7bd-0ff1-76f3-995d-417856020237, 本实现者不再委派也不执行验收. 模式默认关闭, 仅手选 modifier 启用.

- 生命周期 API 已核对: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:105-161 支持预配置 mutable Power 的非泛型 Apply; :291-298 先从 owner 移除再 await AfterRemoved. PowerModel.cs:582-598 在 clone 时重置内部数据与 owner, 可用独立 InitInternalData 存放本次入场标记与效果引用. NCustomRunModifiersList.cs:176-194 为 private IEnumerable<ModifierModel>, :104-117 真实建 tickbox. ModifierModel.cs:73-95 可在新建/读档拒绝缺失 bridge, :139-159 提供原生序列化.

- 首批代码已落盘: FormStanceMode.cs, FormStanceModifier.cs, FormStanceModePatch.cs, FormStanceCmd.cs 与三个形态承载类. 去除了本人四个文件的条件编译门, 模式由当前 player.RunState.Modifiers 判断, 修正采用 Alignment.None 且新建/读档检查 bridge. FormStanceCmd 在普通局走原姿态规则, 没有全局开关或共享配置. 这些文件暂依赖接下来写入的公共追踪基类和 bridge, 未构建/测试.

- 追踪生命周期和本仓路由已落盘: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs. Kind/EnteredRound/EnteredTurnNumber 可供中央探针读取; 每个 mutable carrier 的 InitInternalData 独立存储原标记和两效果引用, 仅卸载自身持有的实例. 群蛇 GrantExitEnergy=false 在非泛型 Apply 前设置. 神格退出经 StanceCmd -> 原 Watcher ExitStance, 同次自己的回合开始不会清除刚进入的神格. StanceCmd 的 Enter/Exit/IsIn 已按当前 run modifier 路由, 普通局原代码保持; Rushdown 采用逻辑 Wrath 身份. 未运行编译或测试.

- Watcher 桥接代码已落盘: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs. 使用私有 HarmonyId=Spire1.FormStanceMode.Watcher, ModelDb.Init 后同步校验所有目标再统一发布 IsAvailable. 目标包括三标记 AfterApplied/AfterRemoved, 两倍伤方法, Divinity.AfterSideTurnEnd, 原 OnStanceChanged, 两个异步 MoveNext, 原生手选列表. 任一签名/IL 唯一匹配/安装检查失败则只回滚本 id 并隐藏入口, 新建/读档拒绝缺失 bridge. 额外覆盖 EndTurnSafely 后备退出分支, 保留正常 EndTurn 调用. Divinity 状态机只替换唯一且参数为 3 的 GainEnergy 调用, 原 VFX 和异步控制流保留. 本轮尚未执行任何绑定或 IL 实测.

- 异步生命周期面继续收紧: ApplyEffect 在 await 后发现退出/战斗结束时, 直接卸载局部持有的准确实例, 即使 AfterRemoved 已清空追踪引用也不会遗留迟到效果. bridge 单独保留原通知委托供极端回滚失败时的非模式旁路, 不把半绑定的能力标为可用. 修订来自源码检查, 未运行并发复现.

- 本地化接入面: G:\omp works\Sts\sts2-spire1\research\BaseLib-StS2\Patches\Localization\ModelLocPatch.cs:39-54 会以代码 Localization 覆盖当前语言文本. 本批三个可见承载已不提供代码 Localization, 直接使用 eng/zhs 文件, 从而避免英语覆盖中文 tooltip. 六个隐藏效果仍由 effects-worker 独占; 本批不修改其实现. 图标文件已只读确认存在于 mod\Spire1\images\powers 与 big 下的 calm_power.png/wrath_power.png/divinity_power.png.

- 本地化已增量落盘: eng/zhs powers.json 各只在文件末尾追加本任务 9 个 Power 的 title/description/smartDescription, 不重排旧键; 新增两个 modifiers.json. 三个可见承载的说明完整包含六效果, 敌方伤害边界, 格挡计入灾厄, 神格下次自己回合退出与不叠原始资源/倍伤. 六效果中文名和灾厄来自用户契约, 其它姿态/力量/格挡术语已对照工作区术语表. 本次未触碰旧译名.

### 4. P1 - 默认编译 Forms 与旧发布门禁相冲突

- 修改前源码证据: G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json:48-57 明确禁止本次十个 Forms 类型, :59-62 额外禁止整个 Forms 命名空间. 契约要求默认编译而非默认启用.
- 触发条件: 中央完成构建后执行发布门禁, 即使自定义入口正确也会被旧规则拒绝. 最小修复: 按本轮追加授权仅删除这十个精确条目与 Forms 命名空间条目, 保留 Experimental/Debug/AFTP 和全部可选程序集硬引用禁止项.
- 复查命令(只读, 未运行门禁): Get-Content -LiteralPath "G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json". 本任务不宣称已验收; 完整产物门禁由主会话执行.

- gate-config.json 已按追加授权落盘: 只解除十个本轮 Forms 精确 TypeDef 和 Forms 命名空间禁止项, 并更新对应说明. Experimental 两个精确项及命名空间, 两个 Debug 精确项, 两个 AFTP held-back 精确项, Watcher/AutoAnthony/其它可选程序集 AssemblyRef 禁令原样保留. mod\Spire1.csproj 不需要修改, 其 AFTP 门与部署保护均未写入.

- 最新调度要求已执行: 不创建 tools\form-integration-probe, 验证探针由其它独立实现负责. 本地化与 gate-config 收尾完成, 原配对监督 01a0e7bd-0ff1-76f3-995d-417856020237 保留. bootstrap 使用 Priority.Last 的 ModelDb.Init 后缀, 不改 MainFile.cs.

### CODE_COMPLETE

- 完成时间: 2026-09-28T11:52:10.043Z. 本批源码写入完成, 不代表编译/运行/发布验收通过. 原配对监督 id=01a0e7bd-0ff1-76f3-995d-417856020237.
- 没有执行构建, lint, 测试, 游戏, 部署或 git; 没有再委派; 没有写入 tools/form-integration-probe, MainFile.cs, 六效果 Power, Steam 或共享配置. mod/Spire1.csproj 无需变动且未写入.

#### 实际修改文件

- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceMode.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceModifier.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceModePatch.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceCmd.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/VoidSerpentStancePower.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/DemonReaperStancePower.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/EchoCelestialStancePower.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/WatcherFormStancePower.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Extensions/StanceCmd.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Powers/RushdownPower.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceWatcherBridge.cs
- G:/omp works/Sts/sts2-spire1/mod/Spire1/localization/eng/powers.json
- G:/omp works/Sts/sts2-spire1/mod/Spire1/localization/eng/modifiers.json
- G:/omp works/Sts/sts2-spire1/mod/Spire1/localization/zhs/powers.json
- G:/omp works/Sts/sts2-spire1/mod/Spire1/localization/zhs/modifiers.json
- G:/omp works/Sts/sts2-spire1/tools/build-gates/gate-config.json
- G:/omp works/Sts/sts2-spire1/docs/reports/form-playable-20260928/integration-worker.md

#### 公开接口与中央验证入口

- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceMode.cs:20 - FormStanceMode.IsSelected(Player?): 只读本局 modifier; IsEnabled(Player?) 在已选择但 bridge 不可用时明确抛错, 不静默退回普通规则.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceModifier.cs:8 - FormStanceModifier: ModelDb.Modifier<FormStanceModifier>().ToMutable() 为原生 mutable 修正, Alignment.None, 由原生 Modifiers 序列化/建局传递. 未实测往返.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceWatcherBridge.cs:37 - FormStanceWatcherBridge.IsAvailable, UnavailableReason, BoundTargets, HarmonyId: 公开绑定诊断. TryBind() 只在首次调用执行同步完整绑定; 对绑定失败/缺少 Watcher 的不同用例应使用独立进程, 不靠污染静态状态重试.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceWatcherBridge.cs:235 - KindOfMarker(Type?), CurrentKind(Player): 读取真实 Watcher 标记映射, None/Calm/Wrath/Divinity. Foreseen 不属于本次三姿态替换.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceWatcherBridge.cs:252 - Enter(Player, FormStanceKind, CardModel?) / Exit(Player): 仅模式局可调用, 返回 Task, 调用方必须 await.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/FormStanceCmd.cs:16 - FormStanceCmd.EnterVoidSerpent/EnterDemonReaper/EnterEchoCelestial/ExitForm(PlayerChoiceContext, Player, CardModel?): 与本仓原卡牌共用 StanceCmd 路由. 普通局进入原姿态, 不启用形态.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Forms/WatcherFormStancePower.cs:33 - player.Creature.Powers.OfType<WatcherFormStancePower>() 可读取每个可见 carrier 的 Kind, StanceName, EnteredRound, EnteredTurnNumber. 三个具体 carrier 对应三种真实姿态, 神格退出在 :152 的 AfterPlayerTurnStart.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Extensions/StanceCmd.cs:19 - StanceCmd.IsIn<TStance> 在模式局按真实 Watcher 标记判定; Current 返回可见 carrier. Enter/Exit 均 await 外部原入口.
- G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Powers/RushdownPower.cs:27 - 本仓 Rushdown 使用逻辑 Wrath, 外部 Rushdown 的原 Type 比较保持原样.

#### 绑定目标清单

- 启动后缀: MegaCrit.Sts2.Core.Models.ModelDb.Init, 类级 HarmonyPatch, Priority.Last; 不使用 AssemblyLoad.
- WatcherMod.Calm.AfterApplied(Creature, CardModel) 与 AfterRemoved(Creature).
- WatcherMod.Wrath.AfterApplied(Creature, CardModel), AfterRemoved(Creature), ModifyDamageMultiplicative(Creature, decimal, ValueProp, Creature, CardModel, CardPlay).
- WatcherMod.Divinity.AfterApplied(Creature, CardModel), AfterRemoved(Creature), 同签名 ModifyDamageMultiplicative, AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>).
- WatcherMod.WatcherCombatHelper.OnStanceChanged(Player, Type, Type): 原通知保留, 后缀只补本仓消费者; 战斗结束不分发奖励.
- Divinity.AfterApplied 的 AsyncStateMachineAttribute.StateMachineType.MoveNext: 严格校验唯一 GainEnergy(decimal, Player), 原始常量3与 this.Owner.Player 收件者形状; 只改该调用, 保留 VFX.
- WatcherCombatHelper.EndTurnSafely 的对应 MoveNext: 保留两个原 EndTurn 调用, 只替换已确认的唯一后备 Remove 和通知调用, 模式内神格不在该分支早退.
- MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NCustomRunModifiersList.GetAllModifiers(): 仅手选列表追加可用修正, 不写 GoodModifiers/BadModifiers.
- 同步签名校验但不打补丁: WatcherCombatHelper.EnterCalm/EnterWrath/EnterDivinity(Player, CardModel), ExitStance(Player), GetCurrentStance(Creature), ChangeStance<T>(Player, CardModel), RunWithHookContext(Player, Func<PlayerChoiceContext, Task>).

#### 中央建议验证顺序

1. 默认 Release 编译与结构门禁: Forms 可用且不存在 Watcher/AutoAnthony AssemblyRef, AFTP/Experimental/Debug 仍禁止.
2. 使用本轮实际 Watcher DLL 检查 TryBind 与 BoundTargets; 改变签名或 IL 匹配数时确认整个 bridge 回滚, 手选项隐藏, 已选存档明确拒绝.
3. 原生自定义列表含一次 mutable FormStanceModifier, Good/Bad 随机池不含它; Modifiers 新建/保存/载入/多人建局往返.
4. 正常 Watcher 卡牌与本仓 StanceCmd 两路分别验证普通局不变, 模式局真实标记加一份 carrier, 同姿态不重复给资源, 切换先完整卸载旧两效果.
5. 平静退出只有原2能量并保留 Violet Lotus, 愤怒/神格不叠原倍伤, 神格原3能量未发放而非扣回; VFX 与外部 Rushdown/MentalFortress/Flurry 通知不丢.
6. 神格在回合结束和 EndTurnSafely 后备分支仍保留, 下次自己的 TurnNumber 增加后经原通知退出; 同一次回合开始内进入不立即清除; 额外回合与不同玩家隔离.
7. eng/zhs 可见三个 carrier tooltip 完整覆盖六效果, 现有图标可加载; 战斗结束无退出能量, 异步挂载中退出不留迟到效果.

## 进行中

- 正在读取本轮契约与实际 Watcher 0.9.28 反编译证据, 随每个检查面增量记录, 然后直接实施白名单接入.

- 接入设计已收敛: 三个原形态类作为可见追踪 Power, 共享 WatcherFormStancePower 基类管理本 owner 的真实 Watcher 标记与两效果引用. StanceCmd/FormStanceCmd 在模式局统一调用外部原入口, 外部标记 AfterApplied/AfterRemoved 的 awaited 包装负责成对挂卸; 普通局不路由. 通知继续调用原 OnStanceChanged, 完成后仅补发本仓 IOnStanceChanged, 不重复外部消费者. 神格按 PlayerCombatState.TurnNumber 而非全局回合计数判断下一次自己的开始, 同次开始内进入不会立刻退出.

- 本实现批次 CODE_COMPLETE, 已移交中央构建/真实验证与原配对监督. 上文正在读取/接下来写入等措辞为保留的过程记录, 当前没有继续派发或实现项. 若监督提出修订, 只在后续明确请求后继续本白名单.

## 未知

- 本任务文字指定 gpt-6-astra-ar, gateway -> agentrouter -> gpt-6-astra. 尚无实际 session metadata 证据, 不将文字指定冒充已核实路由. 本会话使用 Codex 当前运行环境, 未启动任何其它代理或选择替代模型.
- 构建与真实运行由主会话集中执行. 本报告的源码结论不等于编译成功或实机通过.

- 本批未执行任何构建或测试, 因而实际 Watcher IL 形状是否通过新增严格 validator, Harmony 安装/回滚, 原生 tickbox/序列化, 实机 VFX/多人/额外回合均未验证. 不以源码接口检查替代这些证据.
- 六效果 Power 由并发 effects-worker 独占, 本批仅使用已约定的公开类与 GrantExitEnergy 接口, 不为其行为单独背书. 隐藏效果的代码 Localization 仍可能覆盖其独立条目的语言; 可见 carrier 不提供该覆盖, 使用本批 eng/zhs 文件完整描述六效果.
- 战中 clone/重连对 carrier 的 marker/effect 引用与入场计数恢复没有完整证据. 内部数据按引擎 clone 重新初始化可避免跨 owner 共享, 不等于已证明战中状态恢复正确.
- 本任务实际模型/路由元数据未读取到证据, 继续只记录用户指定值, 不把指定值当成已核验事实.
