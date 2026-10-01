# effects 实现增量报告

状态: CODE_COMPLETE, 仅生产写集完成, 未运行验证.

## 已确认

- 2026-09-28 首次增量: 已读取 G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\effects-worker.md 及工作区入口. 本轮只写六个指定效果源文件, 可选 FormEffectRules.cs, tools/form-effects-probe 内的探针和本报告.
- 执行边界: 不构建, 不测试, 不 lint, 不 git, 不部署, 不操作游戏或共享配置, 不写 C:, 不再委派.
- 证据分层: 源码控制流, 隔离探针和真实引擎实机证据分别记录. 请求里的 gpt-6-astra-ar 与 gateway -> agentrouter -> gpt-6-astra 是指定路线, 当前没有可核验的会话元数据, 不宣称已验证真实路由.

### 2026-09-28 首批源码证据

1. P1, 虚空进入牌误消费. 基线 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:94-99 在 AfterCardPlayed 直接计数, 没有 BeforeCardPlayed 配对. 触发条件: 一张手动牌执行期间才获得该效果. 契约要求该进入牌不消费新额度, 当前后置 hook 会消费. 只读定位命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs' -Pattern 'AfterCardPlayed|cardsPlayedThisTurn'. 最小修复: 该文件及必要的配对辅助逻辑. 尚缺: 真实引擎进入牌和同牌再次出牌证据, 未运行复现.
2. P1, 群蛇遗漏自动与重放且追溯触发. 基线 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:42-65 在 After 中排除 IsAutoPlay 和非 IsLastInSeries, 且没有 Before 配对. 触发条件: 自动出牌, 重放, 或出牌中进入平静. 契约要求每个实际 CardPlay 一次, 且开始时效果已经存在. :68-73 无战斗结束和外部 Calm 去重门. 只读定位命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs' -Pattern 'AfterCardPlayed|IsAutoPlay|AfterRemoved'. 最小修复: 该文件, 提供实例级 GrantExitEnergy. 尚缺: 引擎 RNG 与离开平静的实机证据.
3. P1, 恶魔力量账本混用目标和实际接受量. 基线 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:81-92 以 appliedStrength 计算目标差, 忽略 PowerCmd.Apply 返回值, 直接加请求 delta; :111-117 退出再盲撤同量. 触发条件: 力量 hook 阻断或修改. 契约要求目标账本独立于实际授予账本, 撤回不污染其它来源. 只读定位命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs' -Pattern 'appliedStrength|PowerCmd.Apply'. 最小修复: 该文件及窄辅助逻辑. 尚缺: Apply/SetAmount 引擎链和实机 hook 交互.
4. P1, 恶魔敌方伤害判定错误. 基线 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:95-108 只检查 target 和 IsPoweredAttack, 不检查 dealer 阵营. 触发条件: 自己或友方的 powered 伤害, 或敌方无源伤害. 契约要求非空敌方 dealer 的每次伤害加 n. 只读定位命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs' -Pattern 'ModifyDamageAdditive|IsPoweredAttack'. 最小修复: 效果判定, 必要时交接给 integration 的无源分支窄 hook. 尚缺: 引擎无源实际分支和实机证据.
5. P2, 回响消费身份需约束. 基线 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs:43-63 只检查所有者和 consumed, AfterModifyingCardPlayCount 不核对是否由本实例为该牌贡献次数. 契约要求仅下一次合法出牌系列增加 1, 不改其它重放来源. 只读定位命令: Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs' -Pattern 'ModifyCardPlayCount|consumed'. 目前仅为源码风险, 具体进入牌触发条件待核对引擎调用顺序. 最小修复: 该文件. 尚缺: 修改次数 hook 的真实顺序与运行证据.
### 2026-09-28 引擎线索增量 A

- CardPlay 的身份应取实际对象和 Player, 不能取会被牌效果改变的 Card.Owner. 线索: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Cards\CardPlay.cs:18-23, :46-73. 后续将按每次 Before/After 对象配对, 免费额度仅允许手动系列的第一份 CardPlay 发起消费.
- 死神现有判定和官方 ReaperFormPower 同构, 不需要改成回血或 CardType 判定. 线索: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\ReaperFormPower.cs:57-62; G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Creatures\DamageResult.cs:63 明确 TotalDamage 包含格挡. 此文件最小修改为移除编译门, 探针覆盖其保留行为.
- 生命周期线索: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:582-586 会在 DeepCloneFields 重新 InitInternalData. 可变追踪集合放入每实例 Data, 避免 field 浅拷贝共享; 是否应保留 mutable clone 的战中账本仍需实现时明确处理.
- 以上均为本地反编译源码线索, 尚未以当前运行二进制或实机验证替代.
### 2026-09-28 当前测试 DLL 静态证据

- 已只读反编译 E:\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll 的 Hook 类型, SHA256 为 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9. 命令: & 'C:\Users\o_Obl\.dotnet\tools\ilspycmd.exe' -t 'MegaCrit.Sts2.Core.Hooks.Hook' 'E:\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'. 这是静态读取, 不是构建, 测试或启动游戏.
- 当前 DLL 的 ModifyDamageInternal 与线索 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:2520-2566 一致: additive 分支没有 Unpowered 跳过. 待补核对 CreatureCmd 当前 DLL 入口后, 预计无需 integration 另加无源 hook, 避免重复加 n.
- 回响风险收窄: 当前 Hook.AfterModifyingCardPlayCount 只通知 modifyingModels 中的实例, 对应 :676-683; 新获得的回响不在原快照中. CardModel.GeneratePlayCount 在线索 :1887 先于 BeforeCardPlayed :1926 和 OnPlay :1933, 故正常进入牌不会追溯得到新回响. 不将此前 P2 风险称为已经复现的 bug; 修复仅收紧合法计数和消费守卫, 不凭空增加排除卡名单.
### 2026-09-28 虚空实现已落盘

- 已修改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs. 移除编译门, 用实际 CardPlay 的首份手动 Before 预留额度, 仅同对象 After 确认消费. 进入牌后置和进入系列余下重放均无法消费新额度. 判定采用 CardPlay.Player, 不受 OnPlay 转移卡牌所有者干扰.
- 入场与自己的回合开始提供一个额度, 自动出牌不预留. 保留官方能量和星星费用 hook, 不修改 X 费用算法. Data 的待完成引用随 PowerModel clone 重新初始化. 尚未构建或执行; 后续探针覆盖源码链接行为.
### 2026-09-28 群蛇实现已落盘

- 已修改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs. 移除编译门, Data 持有引用身份 HashSet<CardPlay>, Before 记实际 Player, After 先移除再 await 伤害. 不排除自动或重放, 不回溯处理缺少 Before 的进入牌, 重复 After 不多触发.
- 目标只取 HittableEnemies 和 RunState.Rng.CombatTargets, 空敌人列表在调用 RNG 前返回. 保持 ValueProp.Unpowered, dealer 为 Owner, cardSource/cardPlay 为空, 与引擎无源属性含义一致, 不将 Unpowered 误等同于没有 dealer.
- 提供 public bool GrantExitEnergy { get; set; } = true. integration 对外部 Calm 的可变实例在非泛型 Apply 前设 false. 退出支付有实例级一次门, 并检查战斗进行中且未结束及玩家存活; 战斗结束不发能量. 尚未构建或运行.

### 2026-09-28 死神实现已落盘

- 已修改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs, 仅移除 SPIRE1_FORM_MOD 门. 保留官方 dealer 为本人或宠物, IsPoweredAttack, TotalDamage > 0 的联合条件, 每个 DamageResult 施加等量 DoomPower. 未加入 CardType.Attack 过滤或治疗行为. 尚未构建或运行.

### 2026-09-28 天人实现已落盘

- 已修改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs. 移除编译门, 入场一次门先于 await, 发满 max(RoundNumber, 3) 能量和抽牌, 战斗结束时不发. 不实现自动退出, 不扣回 Watcher 的原 3 能量, 由 integration 抑制原支付并持有退出时序. 尚未构建或运行.

### 2026-09-28 中央验证补充已记录

- 用户新增要求: tools/form-effects-probe 的 FormsSourceRoot 属性可切换 G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms 与本轮生产目录, 同一 Program 和协作者替身复现改前及改后.
- 探针定义 SPIRE1_FORM_MOD, 不直接引用旧版没有的新增接口. GrantExitEnergy 等新增 API 使用反射断言, 缺失属于运行时失败证据, 不造成基线编译失败. 不因此扩大生产改动.
- 同批监督实际会话 id 已由用户提供: 01a0e7b8-8a66-70a2-a9aa-023412a5720c. 不再委派, 不发送其它会话消息, 不运行探针.
### 2026-09-28 探针写集已移交

- 按用户最新指令, 立即停止承担 tools/form-effects-probe, 不再写该目录. 截至本次移交, 本实现者没有创建或修改任何探针文件, 没有需要删除或移交的既有探针文件. FormsSourceRoot 等补充要求由新独立实现者承接.
- 当前范围仅六个生产效果及必要 FormEffectRules.cs, 配对监督仍为 effects-review, id 01a0e7b8-8a66-70a2-a9aa-023412a5720c.
- 已补读当前测试 DLL 的 CreatureCmd.Damage(IEnumerable<Creature>?, decimal, ValueProp, Creature?, CardModel?, CardPlay?) 静态控制流: :258-283 不因 ValueProp.Unpowered 旁路 Hook.ModifyDamage, :283 始终传 ModifyDamageHookType.All. 当前 Hook.ModifyDamageInternal :2520-2534 的 additive 分支也不排除 Unpowered. 两者与本地 research\engine-dllsrc 对应行一致, DLL SHA256 已记录在上文. 结论: 本轮仅需修正 DemonFormPower.ModifyDamageAdditive 的敌对 dealer 判定, 不需要 integration 添加任何重复伤害 hook.
### 2026-09-28 恶魔实现已落盘

- 已修改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs, 移除编译门. previousTarget 仅记录 T(n) 的调度目标, grantedStrength 独立记录实际入账. 既有 Strength 读取 PowerCmd.ModifyAmount 返回的本次落点并按当前引擎 SetAmount 的 999999999 上下限换算; 首次 Strength 用明确的 mutable 实例, 观察首个真实 DisplayAmountChanged, 并检查挂载或零值移除, 不把零申请返回的空挂载计为成功.
- 撤回被定义为来源清理, 不是第二次 Strength debuff 申请: 通过 SetAmount 减去实际账本, 必要时按原负力量余额 ApplyInternal 恢复, 零值走 awaited PowerCmd.Remove. 因而不会让 Artifact 或授予倍率再次吞掉/放大撤回, 保留普通数值/UI 事件, 不伪造 AfterPowerAmountChanged 通知. 外部明确移除非零 Strength 实例时丢弃旧来源账本, 避免重新伤害随后获得的力量.
- 在授予 hook 内移除本形态时先记 removed, 不递归等待同一授予. 授予完成的 finally 统一撤回. 撤回一次门先于任何 await. 敌方判断为 target == Owner, 非空 dealer, dealer.Side != Owner.Side, 覆盖 Unpowered, 排除自伤和友方.
- 精确支持边界: 当前实现针对正常 PowerCmd 生命周期及数值修改类授予 hook, 并隔离授予后 hook 的额外力量. 如果 BeforePowerAmountChanged 内又重入改写同一个既有 Strength, 或第三方绕过 PowerCmd/Remove 直接覆写力量, 没有来源事务 id 可唯一归因, 仍待中央复现和监督确认, 不宣称任意第三方交互已正确. 所有运行, 异常/取消和战中存档路径均未验证.
### 2026-09-28 回响实现已落盘

- 已修改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs, 移除编译门. 正常合法 playCount 上只加 1, 不覆盖其它来源次数; <= 0 的取消系列不复活, int.MaxValue 不溢出. 重复查询不消费, 仅引擎实际修改者回调 AfterModifyingCardPlayCount 消费, 重复回调不再次 Flash.
- 进入牌不追溯依赖真实引擎 GeneratePlayCount 在 OnPlay 之前的调用顺序及 modifyingModels 身份集合, 不按 CardModel 永久排除进入卡. 此证据不是实机出牌验证. 其它修改者在本效果之后把最终计数改成 0 的自定义取消协议没有通用最终计数回调, 属于未验证组合.
### CODE_COMPLETE - 六个生产效果已完成落盘

- 状态仅表示本实现者的生产写集已经完成, 不表示构建, 测试, 监督审查或实机通过. 未创建 FormEffectRules.cs, 未写任何 tools/form-effects-probe 文件, 未改主工程, integration 文件, 本地化文件或共享配置, 未运行 git.
- 最后一个收口: DemonFormPower.cs:90 在更新目标账本后检查 owner.CanReceivePowers, 避免直接调用 ModifyAmount 时绕过原泛型 Apply 的可接收检查, 同时不把被拒绝的旧回合差值滚入下一次调度.

实际修改的生产路径与当前行号:

- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:41-119. 重点: :76 首份手动 Before 预留, :92 对象匹配 After, :104 自己的回合重置. 对应首批 P1-1.
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:24-107. 重点: :30 GrantExitEnergy, :49 Before, :59 After, :90 退出. 对应首批 P1-2.
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:24-231. 重点: :76 目标和实收账本, :130 首次实际授予观察, :174 敌方伤害, :191 精确来源清理. 对应首批 P1-3 和 P1-4.
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs:19-46. :35 保留官方 Doom 判定. 仅删除条件编译门.
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs:42-69. :49 合法计数守卫, :59 实际修改者回调消费. 首批 P2-5 已收窄为防御性修订, 不是已复现的进入牌 bug.
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs:18-54. :40 一次性发满 max(n,3), 不承担自动退出.

交接给 integration 和监督:

- 外部 Calm 对 SerpentFormPower 的可变实例先设 GrantExitEnergy=false 再非泛型 Apply. 默认 true 服务于内部姿态. 不用全局静态开关.
- Celestial 发满 max(n,3). 原生 Divinity 的 3 能量必须由 integration 在原支付点抑制, 不能先多给后扣. 自动退出仍由姿态承载统一处理.
- 当前测试 DLL 的无源伤害结论已静态确认, 不增加任何额外伤害 patch. 精确入口为 CreatureCmd.Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay). 对应 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\CreatureCmd.cs:258-283, 接到 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:1495 和 :2520-2534. 当前 DLL 反编译结果一致, Unpowered 仍走 additive.
- 探针替身需要忠实覆盖实际 API: PowerCmd.ModifyAmount 返回本次 SetAmount 前计算的整数落点, SetAmount 自身限幅并发 DisplayAmountChanged; 非泛型 PowerCmd.Apply 返回 Task 而非 Power; ApplyInternal 执行 SetAmount 并挂载; RemoveInternal 不清除旧 Power.Amount; PowerModel.DeepCloneFields 重新 InitInternalData. 这些是协作者模拟边界, 不可当作真引擎行为已经实测.
- 当前源码只读行号核对已完成, 没有执行编译器, lint, 文本检查器或回归命令. 首批发现的行号指修订前快照; 固定基线目录为 G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms.

主会话后续命令建议, 仅在其中央验证授权下执行, 本实现者没有运行:

```powershell
$env:TEMP = 'G:\tmp'
$env:TMP = 'G:\tmp'
$env:DOTNET_CLI_HOME = 'G:\omp works\Sts\sts2-spire1\.dotnethome'
$env:NUGET_PACKAGES = 'G:\omp works\Sts\sts2-spire1\.nuget\packages'
# 待独立探针实现者交付后, 同一项目分别链接固定基线和生产源码.
dotnet run --project 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe' -p:FormsSourceRoot='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms'
dotnet run --project 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe' -p:FormsSourceRoot='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms'
# 构建路径与其它中央任务隔离, 不部署到 mods.
dotnet build 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' -c Release -m:1 -p:UseSharedCompilation=false -p:CopyToModsFolderOnBuild=false -p:BaseIntermediateOutputPath='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\effects-central\obj\' -p:OutputPath='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\effects-central\bin\'
```
## 进行中

- 本实现者无剩余生产编辑. 等待 effects-review 和中央验证, 探针已移交独立实现者.

## 未知

- 未运行任何构建, lint, 测试或游戏. 探针编写已移交, 本实现者没有探针产出.
- effects-review 会话 id 已由用户提供, 当前尚未取得其审查结论.- 尚缺实机证据: Watcher 实际进退姿态和各消费者通知, 多人 RNG 同步, X 费用, 自动/重放嵌套, 被中断 CardPlay, 各 Hook 异常和取消, 战中存档/重连. Clone 内部状态按原生语义重新初始化, 未宣称保留战中账本.
- 必须由监督明确复核的边界: 恶魔授予前 hook 重入改写同一 Strength 的来源归属, 第三方直接覆写聚合数值, 撤回绕过授予类 hook 的来源清理约定, 回响之后的第三方取消整个系列. 这些不以源码完成状态掩盖.