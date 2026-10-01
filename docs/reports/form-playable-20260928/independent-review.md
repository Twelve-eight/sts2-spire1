# 独立只读审查报告

- 检查开始: 2026-09-28 19:06:40 +08:00.
- 唯一可写文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\independent-review.md`.
- 固定快照: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod`.
- 原活跃源码根: `G:\omp works\Sts\sts2-spire1\mod`. 后续问题行号均对应固定快照, 同时给出原活跃路径. 活跃源码可能被主会话继续修改.
- 执行边界: 当前 Codex 会话独立只读审查, 不再委派, 不构建, 不测试, 不运行或修改游戏, 不修改代码或共享配置, 不提交或推送.
- 请求指定模型为 `gpt-6-astra-ar`, 路由为 `gateway -> agentrouter -> gpt-6-astra`. 这是请求声明, 不是本会话实际模型或路由的元数据证明; 本轮未切换模型或路由.

## 已确认
### 最终结论

- 最新更正: 2026-09-28 19:27:16 +08:00. F7 正式撤回, 不再进入实现批次的缺陷清单.

- 收尾时间: 2026-09-28 19:22:28 +08:00. 更正后保留 7 项可操作问题, 4 项 P1, 3 项 P2. F7 已因中央直接反证撤回. 每项均附快照行号, 触发条件, 契约与控制流, 只读证据复核命令, 未运行的行为复现建议, 最小修复范围及尚缺实机证据.
- P1: F1 进入牌消费下一张免费; F3 姿态身份与联动不兼容; F4 实际力量与回滚账本不一致; F8 新联机局各端使用本地内容闩锁.
- P2: F2 自动退出漏派发姿态事件; F5 Serpent 漏算自动/重复出牌; F6 Demon 漏算敌方反伤.
- F1-F6 描述形态启用分支的行为源码问题, 不是当前默认发行包已发生这些运行故障的声明. 中央提供的显式 SPIRE1_FORM_MOD 完整工程构建已成功且含全部 Forms 类型. 初审快照未找到 FormStanceCmd 的外部进入调用, 这是入口问题, 不能等同编译失败. F8 独立于该形态编译符号.
- 本审查没有执行构建, 测试, 隔离探针或实机操作; 已只读复核中央提供的成功构建日志和 TypeDef 清单, 并据此撤回 F7. 其它命令字段仅供重读源码证据, 不等同已经具备可执行的游戏行为复现脚本.
- 按唯一写入白名单持续增量保存本报告; 未修改产品代码, DEVLOG, 配置, 快照, 游戏安装或 Git 提交. 不再委派, 未变更模型或路由. 模型与路由的实际元数据仍未核验.

### 接收与范围记录

- 2026-09-28 19:06:40 +08:00: 已读取任务请求, `G:\omp works\START-HERE.md` 和 `G:\omp works\docs\WORKSPACE-PROJECTS.md`. 本项目仍在维护范围. 目录迁移的既有修改及明确 held-back 层不直接视为本轮缺陷.
- 初审以源码作为静态审查证据, 随后按中央直接构建反证撤回 F7. 其它行为仍无隔离复现或实机证据. 逐面增量记录保留, 当前有效问题为 7 项.

### 固定快照与契约核对

- 2026-09-28 19:07:28 +08:00: 已确认固定快照存在, `Spire1Code\Forms` 下为 10 个源文件. `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline-status.txt` 中 Forms, Run, Spire1ContentSnapshotPatch, Sts1EventToggleFilterPatch 均为既有未跟踪内容. 本轮不改变基线状态, 不把 `NuGet.config`, `Sts2PathDiscovery.props` 等路径清理项计为缺陷.
- 权威需求已读取: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:18-31` 规定六种效果, 下一张免费/额外打出, 敌方每次伤害加 n, 力量退出回滚及姿态联动. `G:\omp works\Sts\sts2-spire1\docs\MECH-reaperform-doom-20260926.md:8-12` 规定 Reaper 判据为玩家或其宠物, PoweredAttack, TotalDamage 大于 0.
- 项目旧 DEVELOP 中的模型选择及 Steam 历史路径不覆盖本次请求. 本轮不依赖历史构建或实机通过声明.
### 检查面 1: 形态入口与 Void-Serpent 源码

- 2026-09-28 19:07:58 +08:00: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs:18-28` 的三个进入包装调用原 `StanceCmd.Enter<T>`, 退出调用原 `StanceCmd.Exit`. 是否存在调用者及编译开关仍待全链搜索.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidSerpentStancePower.cs:30-40` 顺序挂载/移除两个效果 Power, 自身未实现额外状态. 互斥与联动仍依赖原 StanceCmd.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:49-65` 使用 `RunState.Rng.CombatTargets.NextItem` 而非系统随机数, 并在无可攻击敌人时返回. 已排除直接使用非确定性 Random 这一类问题; 本记录不证明联机一致性或全局枚举顺序.
### 检查面 2: StanceCmd 与编译入口

- 2026-09-28 19:08:31 +08:00: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:23-53` 只取第一个 StancePower, 相同实际类型直接返回, 切换时先移除旧 Power 再施加新 Power, 成功后派发自定义事件. `:99-131` 对 Power 和全部牌堆中的接口监听者去重后逐个 await. 原手动切换并非完全没有事件派发.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:18-20` 的 `IsIn<TStance>` 是精确 Power 类型查询, 不是 `StanceName` 语义查询. 新形态的类型联动适配尚待逐个消费者核对.
- 静态搜索整个固定 `baseline\mod` 的 `*.cs`, `*.props`, `*.csproj` 未找到 `SPIRE1_FORM_MOD` 定义或 FormStanceCmd 的外部调用. 10 个 Forms 类均由该符号包围. `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj:65-96` 明确 held-back 的是 AftpFireFlyPerfCompat 与 AftpCardStateCompat, 本轮不会把这两层未编译报告为错误. Forms 的入口状态将与研究记录交叉核对, 暂不冒称可玩.
### 检查面 3: Demon-Reaper 源码与官方出牌语义

- 2026-09-28 19:09:06 +08:00: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonReaperStancePower.cs:29-39` 顺序挂载两效果并在退出时移除. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs:36-45` 的 dealer 为玩家或其宠物, PoweredAttack, TotalDamage 大于 0 及施加量判据, 与已读 MECH 契约一致; 是否完整等同官方实现将直接核对官方文件, 不按研究报告宣称通过.
- `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Cards\CardPlay.cs:48-73` 明确每次重复打出都有独立 PlayIndex, IsLastInSeries 仅筛选系列末次. 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\SerpentFormPower.cs:71-92` 使用 Before/After 配对, 不排除自动打出或系列前几次; 同时专门防止新施加效果追溯触发进入牌. 当前自定义 Serpent 与此存在语义差别, 将连同需求核对.
### F1 [P1] 进入牌会消费刚授予的下一张免费次数

- 检查时间: 2026-09-28 19:10:16 +08:00. 证据级别: 固定快照源码与引擎调用链, 未运行复现.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:94-99`; 挂载点 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidSerpentStancePower.cs:30-33`.
- 触发条件: 接通形态入口后, 通过普通手动出牌在 `OnPlay` 内进入 Void-Serpent, 且结算后战斗仍进行. 该问题是启用该形态后的逻辑缺陷, 不是当前默认构建已实机触发的声明.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:19` 要求本回合下一张牌免费, 之后每回合首张免费.
- 当前控制流: 新效果的计数从 0 开始. 进入牌 OnPlay 先施加效果, 同一进入牌稍后进入 AfterCardPlayed. 该效果未判断自己是否见过这次 BeforeCardPlayed, 直接把计数增为 1, `ShouldSkip` 在 `:67` 随即拒绝下一张免费. 引擎证据: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1926-1965` 先 OnPlay 后 AfterCardPlayed; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:278-283` 在后置阶段重新枚举当前监听者, 并非只通知出牌前存在的效果.
- 可复现命令(只读源码证据, 非行为复现): `$p='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\VoidFormEffectPower.cs'; $a=Get-Content -LiteralPath $p; 94..99 | ForEach-Object { '{0}: {1}' -f $_,$a[$_-1] }`.
- 未运行的复现建议: 进入牌正常付费并授予该形态后, 检查同回合手中原本 1 费的下一张牌. 期望能量和星星成本归零且只消费一次; 当前代码在进入牌结束时已用完名额. 再覆盖进入牌重复打出与自动打出分支.
- 最小修复范围: 在 VoidFormEffectPower 内按实际出牌实例记录 BeforeCardPlayed/AfterCardPlayed 配对, 或显式排除本次授予效果的 CardPlay; 保留下一回合重置及重复系列只消费一次的语义. 不以 CardModel 永久身份排除以后再次打出的同一张牌.
- 尚缺实机证据: 可玩入口, 开启形态的构建, 真实进入牌后的费用与支付结果, 重复打出和联机一致性.

### 检查面 4: Echo-Celestial 与 Reaper

- 2026-09-28 19:10:16 +08:00: Forms 下 10 个文件已全部逐行读取. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs:44-49` 确实以 `max(RoundNumber,3)` 先加能量后抽牌, 不是每回合重复给予. 抽牌使用 ThrowingPlayerChoiceContext 的边界尚需后续核对.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\ReaperFormEffectPower.cs:36-45` 与官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\ReaperFormPower.cs:57-62` 的三个触发判据一致, 当前挂载 Amount 为 1. 未发现把敌方出血或中毒误当 PoweredAttack 的分支, 不作为问题报告.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoFormEffectPower.cs:43-64` 在修改次数后的独立回调中消费, 不是在每次 OnPlay 中递归重放. 引擎 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1887,2029-2034` 在进入牌 OnPlay 之前就计算本次次数. 因而不能套用 F1 断言 Echo 也消费进入牌; 修改者判重仍待核对 Hook 实现.
### F2 [P2] 下回合自动退出绕过姿态事件, 丢失 Mental Fortress 联动

- 检查时间: 2026-09-28 19:11:17 +08:00. 证据级别: 固定快照源码, 未运行复现.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\EchoCelestialStancePower.cs:36-40`.
- 触发条件: 玩家在 Echo-Celestial 中持有 MentalFortressPower, 到自己的下一回合开始自动退出.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:27,30-31` 要求下回合自动退出且保留姿态切换联动.
- 当前控制流: 自动退出直接 `PowerCmd.Remove(this)`, `AfterRemoved` 在 `:44-47` 仅移除两个效果, 不调用 `StanceCmd.Exit/Dispatch`. 自定义接口只由 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:56-65,99-131` 派发, 因而 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\MentalFortressPower.cs:26-31` 没有被调用, 该次退出不给格挡. 手动 Exit 会派发, 自动退出与手动退出不一致.
- 可复现命令(只读源码证据, 非行为复现): `rg -n 'PowerCmd.Remove|StanceCmd.Exit|Dispatch|OnStanceChanged' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\EchoCelestialStancePower.cs' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Extensions\StanceCmd.cs'`.
- 未运行的复现建议: 持有 3 层 MentalFortressPower, 进入该形态后结束回合, 在下一回合观察自动退出. 期望新增 3 格挡且姿态事件恰好一次; 当前自动移除路径不会给予该 3 格挡.
- 最小修复范围: 自动到期通过统一退出命令, 并核对当前姿态仍是本实例. 不另行重复手动触发 AfterRemoved, 避免双派发.
- 尚缺实机证据: 回合开始时姿态图标移除, 格挡变化, 事件次数以及多人回合顺序.

### F3 [P1] 新姿态没有保留原姿态身份, Rushdown 与 Calm 查询失效

- 检查时间: 2026-09-28 19:11:17 +08:00. 证据级别: 固定快照源码与契约, 未运行复现.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonReaperStancePower.cs:17-21`; 同类位置 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidSerpentStancePower.cs:18-22`.
- 触发条件: 形态入口接通后, 有 RushdownPower 时进入 Demon-Reaper; 或有 LikeWaterPower 时在 Void-Serpent 中结束回合.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:10-15,30-31` 将两组形态分别映射为 Wrath/Calm 并明确保留 Rushdown 等联动.
- 当前控制流: 三个新姿态直接继承 StancePower, 使用新的 StanceName, 既不是 WrathPower/CalmPower, 也没有语义映射. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\RushdownPower.cs:25-30` 只接受 `to is WrathPower`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Powers\LikeWaterPower.cs:25-33` 使用 `StanceCmd.IsIn<CalmPower>`; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs:18-20` 最终查询具体 Power 类型. 所以仅复用 StanceCmd 的派发无法保留这两条联动.
- 可复现命令(只读源码证据, 非行为复现): `rg -n 'class (DemonReaperStancePower|VoidSerpentStancePower)|StanceName|is not WrathPower|IsIn<CalmPower>' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code'`.
- 未运行的复现建议: Rushdown 为 2 层时从无姿态进入 Demon-Reaper, 期望抽 2 张; LikeWater 为 5 层时在 Void-Serpent 结束回合, 期望得 5 格挡. 当前对应谓词均为 false.
- 最小修复范围: 给 StancePower/StanceCmd 建立统一的 Calm/Wrath/Divinity 语义分类, 迁移 IsIn 与所有类型相关联动消费者. 不能仅修显示名称, 也不能为取得类型身份而恢复原 Wrath/Divinity 的伤害倍数.
- 尚缺实机证据: 两条联动的抽牌/格挡, 原普通姿态路径不回归, 新旧形态切换不叠加效果.

### Echo 判重排除项

- 2026-09-28 19:11:17 +08:00: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:1384-1395` 仅把实际改变次数的模型加入 modifyingModels; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:2029-2034` 随后消费这些修改者. 当前 EchoFormEffectPower 不改变次数时不会被加入该列表. 已排除正常主路径中在第二张牌重复消费/再增次数的猜测; 自动嵌套出牌仍需有授权的实机验证.
- F1 补充证据: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatState.cs:411-417` 每次枚举都收集当前 creature.Powers, 包含进入牌刚挂载的 Void 效果.
### 检查面 5: Run 与内容/事件补丁初读

- 2026-09-28 19:12:48 +08:00: 已逐行读取固定快照的 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\Spire1RunContent.cs:16-25`, `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\Spire1ContentSnapshotModifier.cs:17-24`, `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1ContentSnapshotPatch.cs:32-100` 与 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Sts1EventToggleFilterPatch.cs:34-60`.
- 内容快照写入点是 RunManager.ToSave 后缀, 读取点是 RunState.FromSerializable 前缀, 新局使用 CreateForNewRun 后缀. 读取缺失快照明确返回 true, 保存明确 RemoveAll 同 ID 后新增一枚, 不是无限追加重复快照. 已排除这两个直接缺陷猜测.
- 事件补丁在 GenerateRooms 后从实际 RoomSet.events 移除本 mod 的 Spire1Event, 并非仅在初始化器阶段移除注册池. 是否覆盖官方生成和读档时序仍需引擎证据; 不把未实际加载补丁说成通过.
### F4 [P1] 力量账本记录请求量而非实际施加量, 退出后残留力量

- 检查时间: 2026-09-28 19:13:47 +08:00. 证据级别: 固定快照与官方遗物/PowerCmd 源码, 未运行复现.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:90-92,111-117`.
- 触发条件: 持有本场尚未触发的官方 RuinedHelmet, 在第 1 回合首次获得力量的来源是进入 Demon-Reaper, 然后离开该姿态.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:21-23` 要求追踪本姿态施加量并在退出时回滚, 退出后不留下本姿态力量.
- 当前控制流: 第 1 回合目标为 1, 请求 `Apply<StrengthPower>(1)`, 随后无条件给账本加 1. `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Relics\RuinedHelmet.cs:32-58` 会把首次正力量翻倍为 2, 而负量不翻倍. `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:124-139,231-241` 确认这类 hook 修改会进入实际层数. 离开时只扣账本 1, 因而残留 1 力量. 同一账本还驱动下回合 delta 计算, 所以差额不会自行消失.
- 可复现命令(只读源码证据, 非行为复现): `rg -n 'appliedStrength|Apply<StrengthPower>|modifiedAmount \*=|amount <= 0m' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\DemonFormPower.cs' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Relics\RuinedHelmet.cs'`.
- 未运行的复现建议: 初始力量 0, RuinedHelmet 未使用, 第 1 回合进入后实际力量应为 2, 离开后应回到 0. 当前源码算术路径为 0 -> 2 -> 1. 再覆盖已有永久力量与跨回合刷新, 确认只回滚本姿态的实际贡献.
- 最小修复范围: DemonFormPower 追踪实际贡献而非请求 delta, 同时将目标回合量与实际累计贡献分开建模; 回滚只反转该贡献, 并处理施加被改写/拒绝/战斗结束的结果. 不只把返回对象非 null 当作施加成功证明, 官方 Apply 泛型的返回值本身不等同实际变化量.
- 尚缺实机证据: RuinedHelmet 联动, 精确力量与退出后伤害, 跨回合刷新及其它力量来源保持不变.

### F5 [P2] Serpent 漏掉自动打出及重复系列中的前几次出牌

- 检查时间: 2026-09-28 19:13:47 +08:00. 证据级别: 固定快照与官方 CardPlay/Serpent 源码, 未运行复现.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:42-46`.
- 触发条件: 已处于 Void-Serpent 时, 自己的一张牌被自动打出, 或因 Replay/额外打出而拥有 PlayCount 大于 1.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:20` 要求每次出牌对随机敌人造成 3 伤害, 没有手动出牌或系列末次限定.
- 当前控制流: `IsAutoPlay` 直接返回, `!IsLastInSeries` 也直接返回. 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Cards\CardPlay.cs:41-73` 把每次重复作为独立出牌, `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:1896-1965` 逐次发 Before/After. 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\SerpentFormPower.cs:71-92` 也以 Before/After 配对触发, 不排除自动和重复. 当前自定义实现把一次重复两次的系列由 6 伤害压成 3, 自动打出则是 0.
- 可复现命令(只读源码证据, 非行为复现): `rg -n 'AfterCardPlayed|IsAutoPlay|IsLastInSeries|BeforeCardPlayed|amountsForPlayedCards' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\SerpentFormPower.cs' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\SerpentFormPower.cs'`.
- 未运行的复现建议: 仅剩一个足够存活的可攻击敌人, 先确认形态已生效; 让一张无伤害牌执行 2 次, 期望额外 3+3, 当前仅 3. 再自动打出一张牌, 期望额外 3, 当前为 0.
- 最小修复范围: SerpentFormPower 按实际出牌实例使用 Before/After 配对, 移除这两个不属于该伤害效果的过滤条件. 配对同时避免新施加效果反向触发进入牌; 不照搬 Void 免费次数的系列消费规则.
- 尚缺实机证据: 自动与重复打出的逐次伤害, 每次实际触发对应的 CombatTargets 消耗, 双端 RNG 顺序及战斗结束边界.
### F6 [P2] Demon 只增加 PoweredAttack, 漏掉确实来自敌人的反伤

- 检查时间: 2026-09-28 19:14:45 +08:00. 证据级别: 固定快照与官方敌人/Thorns 源码, 未运行复现.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormPower.cs:95-108`.
- 触发条件: 处于 Demon-Reaper, 攻击带 ThornsPower 的官方敌人 SpinyToad 或 Toadpole, 从敌方受到反伤.
- 契约: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-mod-20260926.md:21-23` 是从所有敌人处受到的每次伤害加 n, 并非只有 PoweredAttack 加 n. 此处不能套用仅属于 Reaper 灾厄效果的 PoweredAttack 限制.
- 当前控制流: `!props.IsPoweredAttack()` 直接返回 0. 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Monsters\SpinyToad.cs:76` 会给自身 5 层 ThornsPower; `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\ThornsPower.cs:17-22` 以敌人为 dealer, 使用 `Unpowered | SkipHurtAnim` 反伤. 这条真实存在的敌方伤害路径被当前条件排除. 同时方法完全未检查 dealer 的敌我关系, 因而不能用该谓词等价表达敌方来源.
- 可复现命令(只读源码证据, 非行为复现): `rg -n 'IsPoweredAttack|ThornsPower|CreatureCmd.Damage' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\DemonFormPower.cs' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Monsters\SpinyToad.cs' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models.Powers\ThornsPower.cs'`.
- 未运行的复现建议: 第 3 回合, 无格挡和其它伤害修正的玩家在 Demon-Reaper 中攻击带 5 层 Thorns 的 SpinyToad. 期望该敌方反伤为 8, 当前本效果贡献为 0, 反伤仍为 5. 自伤另列负例, 应不额外加 n.
- 最小修复范围: 按 dealer 与 Owner 的敌方关系筛选, 覆盖所约定的所有敌方伤害类型, 不限定为 PoweredAttack. 若某些伤害类型无法在此 hook 覆盖, 应集中在实际伤害管线的合适层实现, 不能修改 Reaper 的正确判据.
- 尚缺实机证据: 敌方反伤, 普通攻击, 多段攻击, 无来源伤害与自伤的区分及伤害显示/实际扣血一致性.
### 更正记录: F7 已撤回, 不再作为缺陷或修复任务

- 更正时间: 2026-09-28 19:27:16 +08:00. 处理依据: 中央提供的实际构建日志与 DLL TypeDef 清单, 已逐项只读复核. 本审查未重跑构建, 未修改源码.
- 撤回结论: 原 F7 关于启用 SPIRE1_FORM_MOD 后因缺少 CardModel 导入而确定编译失败的判断错误, 正式撤回其 P1 定级及补齐 9 个 using 的修复建议. F7 编号保留为撤回记录, 避免已接收报告的实现批次误用旧结论.
- 构建证据: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\before-build.log:61` 记录完整工程产出 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\before\bin\Spire1.dll`; 同日志 `:124-125` 为 58 个警告, 0 个错误. 这些数字来自该日志, 不是本审查执行的构建结果.
- 类型证据: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline-form-types.txt:2-11` 列出全部 10 个 Forms 顶层类型, 包括 FormStanceCmd 及三组姿态/效果类型. 因此这次成功构建不是通过排除 Forms 代码获得的空分支成功.
- 参数与基线归属: 中央明确说明这是本轮前未修改的完整工程, 显式 `DefineConstants=SPIRE1_FORM_MOD` 的构建. 本轮已复核上述日志结果及类型清单, 不重新运行或扩查构建流程.
- 错误原因: 原审查把局部源文件缺少显式 using 的观察, 错误提升为完整工程必然出现类型解析错误的确定结论; 未掌握完整编译上下文便给出失败判断. 实际启用分支构建和 TypeDef 证据优先, 不能以局部静态推理抵抗直接反证.
- 明确区分: 源码受条件编译符号控制, 不等于启用该符号后编译失败. 默认构建是否包含该分支, 启用分支是否编译成功, 游戏入口是否可达是三个不同问题. 现已确认中央提供的启用构建成功且包含 Forms 类型; 这不自动证明可玩入口或其它行为验收通过.
- 证据指纹: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\before-build.log` SHA256 `488D2DDA4F037AD8C3255A07667726CB5EEE277201CF0A758DF03CE38849A2F3`; `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline-form-types.txt` SHA256 `C2C9D1C8D548AB895B180286F68E400D28F797F1E64795710DB146D6FE280935`.
- 收束决定: 其余 F1-F6 与 F8 保持原固定快照证据和未运行边界, 由实现批次接收. 本轮不新增第 8 项替补, 不再扩展审查范围.
### F8 [P1] 全新联机开局各端读取本地闩锁, 存档快照不能同步本次新局

- 检查时间: 2026-09-28 19:15:54 +08:00. 证据级别: 固定快照与官方联机开局源码, 未运行联机.
- 位置: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1ContentSnapshotPatch.cs:35-42`.
- 触发条件: 两端均安装本 mod, 没有额外保证该设置一致的配置同步组件, 双方 RegisterContentNextRun 不同, 新开多人局而非加载已有存档.
- 宣称: 同文件 `:27-29` 声称各端通过 SerializableRun/FromSerializable 采用房主值以保证卡池登记一致; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\Spire1RunContent.cs:6-13` 将该闩锁作为本局内容登记的单一来源.
- 当前控制流: 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Multiplayer.Game.Lobby\StartRunLobby.cs:415-421,441-461,469-500` 广播种子/修正后让每端本地启动. `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:1156-1159` 的正常全新联机路径调用 CreateForNewRun, 不是 FromSerializable. 当前后缀因此在各端读取自己的设置, 没有从开局消息注入或读取本 mod 快照. 快照仅在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1ContentSnapshotPatch.cs:69-79` 的 ToSave 后缀产生, 读档同步路径不能倒过来修正已生成的新局. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Config\Spire1Config.cs:98-100` 又将本地闩锁用于卡牌/遗物/事件过滤, 因而双方立即得到不同内容 gate.
- 可复现命令(只读源码证据, 非行为复现): `rg -n 'CreateForNewRun|FromSerializable|StartNewMultiplayerRun' 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs'`; `rg -n 'RegisterContentNextRun|ContentRegistered|Spire1ContentSnapshotModifier' 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code'`.
- 未运行的复现建议: 使用互相独立且事先备份的两端测试配置, 房主 true, 客户端 false, 其它内容 gate 全 true, 从选人界面新开联机局. 比较每端 new-run content latch 日志和生成后的池 ID, 期望都采用房主 true, 当前分别保持 true/false. 同机共享 AppData 不能直接充当独立配置反例, 应由获授权的主会话安排隔离环境.
- 最小修复范围: 新局由房主在开局协议中提供单一内容快照并在任何池生成前被各端采用; 可复用开局 modifier 通道, 但必须覆盖正常界面实际传参, 不能只追加磁盘保存后缀. 保持旧档缺快照的既定 true 语义.
- 尚缺实机证据: 双端新局对照, 池/RNG/checksum 首次分歧位置, 加载旧档/新档一致性, 与可选配置同步 mod 的协作. 本报告不声称已经看到游戏发生断线或 checksum 错误.
### 检查面 6: 最近 AutoAnthony 纯反射化的已查边界

- 2026-09-28 19:17:08 +08:00: 已读 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs:20-69` 以及 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:1-520`. 静态字段只保存本项目类型, int 枚举常量与反射元数据, 目前未见静态字段直接强引用 AutoAnthony 类型. 程序集缺席时在 Apply 入口返回 false; 这只能排除所查源码中的直接强类型引用, 不等于已检查生成 DLL 的 AssemblyRef 或 JIT.
- `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:234-279` 在挂补丁前检查所列必需成员是否可解析, 缺失时返回 false; `:387-406` 的观者池解析仅在非空时缓存成功, 因此未注册时的一次 null 不会永久锁死解析. `:428-469` 对继承自基类的池 getter 使用实例相等守卫, 不是对全部池无条件替换. 这三个具体实现面已读取, 未凭假设增加兼容性缺陷.
- F6 补充: 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:2520-2534` 的 additive 分发本身没有排除 Unpowered, 当前漏算来自 Demon 自己的筛选条件.
- F7 更正: 原 BaseLib.props 局部检查不覆盖完整编译上下文, 不支持确定编译失败的推论; 已依据中央成功构建与 TypeDef 证据撤回. F8 已再直接读取 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:1156-1161`, 普通新联机路径确实为本地 CreateForNewRun 后 SetUpNewMultiplayer/StartRun.
- 该阶段原列 8 项, 后经中央直接反证撤回 F7, 当前保留 7 项. 不扩充替补问题, 不继续扩大审查范围.
### 检查面 7: 克隆与生命周期证据边界

- 2026-09-28 19:18:40 +08:00: 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\PowerModel.cs:517-525,582-597` 明确每次克隆会重新 InitInternalData 并清空 Owner, 不是共享同一个 Data 对象. 已排除默认克隆直接共享 Void/Echo/Demon 私有 Data 的猜测. 该机制同时不保留私有计数; 由于尚未证明当前形态存在需保留这些状态的运行中克隆消费者, 不据此追加克隆缺陷.
- F2 补充: 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291-298` 只 RemoveInternal 后调用 AfterRemoved, 没有本 mod 的 IOnStanceChanged 派发. `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatManager.cs:924-926` 确认 AfterPlayerTurnStart 在本回合基础抽牌之后执行.
- 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\CardPileCmd.cs:1007-1060` 会将传入 choiceContext 继续传入抽牌 hook. Celestial 使用 ThrowingPlayerChoiceContext 的实际选择需求未证明, 只作为未知边界, 不新增未经复现的异常问题.
- 已读完固定快照 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:1-692`. `:573-597` 创建的是反射得到的真实 GeneratedCharacter 数组, 按整数去重排序; `:675-690` 的起手替换包含 active run, active character 与开关三重条件. 未执行 Harmony/JIT 或第三方加载, 不能把源码注释中的历史 IL/实测声明当本轮结果.
### 检查面 8: 事件生成与范围收尾

- 2026-09-28 19:22:28 +08:00: 官方 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\ActModel.cs:331-347` 将实际候选写入 `_rooms.events`, 与 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Sts1EventToggleFilterPatch.cs:49-54` 的删除目标对应. `ActModel.FromSave` 直接恢复已保存的 RoomSet, 见同一官方文件 `:510-514`; 本轮不宣称旧档已有事件被重新生成或重新过滤.
- BaseLib 的 Good/Bad modifier 拼接只包含 CustomModifierModel, 见 `G:\omp works\Sts\sts2-spire1\research\baselib-dll\BaseLib.Abstracts\ModelDbPatches.cs:16-17,33-66`. 本项目内容快照继承普通 ModifierModel, 因而不会通过这两段拼接混入可选玩法修正. 真正的 ModelDb 注册和 SavedProperty 往返没有执行验证.
- AutoAnthony 最近纯反射变更的提交边界为 `274ce69`, 日期 2026-09-27, 通过只读 Git 历史核对. 对其父版本也存在的旧挂接策略未认作此次纯反射化引入的新问题. 已查边界记录在检查面 6/7, 不构成 AutoAnthony 全量无缺陷结论.
- 结束时保留固定快照结论, 不追随主会话后续改码改行号. 仓库仍有基线中的未提交修改与并行新增文档, 本轮没有清理, reset, commit 或 push, 不宣称仓库干净.

### 快照身份记录

- 对照时间: 2026-09-28 19:22:28 +08:00. 下列 SHA256 属于固定快照文件; 活跃文件状态仅表示本次读取时点, 以后可能改变.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\CelestialFormPower.cs`: SHA256 `E231B4DC989CC04862B67EAB0AE17BB91086C5992BCCDA2B1251941D2C7E448D`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\DemonFormPower.cs`: SHA256 `FCC481CF35C8642BD64E28DA51E4FCE3A3934132C3BAB7FBF16D01B671AA5D55`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\DemonReaperStancePower.cs`: SHA256 `082E7BBE254FA41752C6CC12F8AEA71C3FF16A107D801370C6A661412A6E1630`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\EchoCelestialStancePower.cs`: SHA256 `768D85179CE2CDE06F129EB858D13D74BE11FC6DDFAB54CBE86B0BF95592C5CD`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\EchoFormEffectPower.cs`: SHA256 `5FA19298212C5E1389B2D82932C5CDA5D0EA29246CEE9067F5994FD2FD7EF54E`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\FormStanceCmd.cs`: SHA256 `E0F500493A70C7117989437E246DDCA2FF64BED6F8C3862642B03A2E47301B47`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\ReaperFormEffectPower.cs`: SHA256 `8E3B0C992A9841CF8E480DAA115E42B8B7B9A667F22253F0496E4E496DD6E88F`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\SerpentFormPower.cs`: SHA256 `9096714D3658D286F4854284F71B8FF008A995C5F8053D81CE87D65444D5368A`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\VoidFormEffectPower.cs`: SHA256 `2825AB8955E21BDA41535995F6D05E0505A81F30228F4103951D39B4B39483A3`; 原活跃文件 不同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Forms\VoidSerpentStancePower.cs`: SHA256 `4E41103F419273131D0974518C6B02F7FED7599A2258699DC6BB2C992482332B`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Extensions\StanceCmd.cs`: SHA256 `10603A8AB9953E7F87C766D92D070C313B600684F54BCA17604A394BDA4293DE`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Patches\Spire1ContentSnapshotPatch.cs`: SHA256 `AAC682E93C78A80520EDA8C9240400CA9F461A19381EF5CDD12E8D2EA78A885C`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Patches\Sts1EventToggleFilterPatch.cs`: SHA256 `35C2FA602A62F60216D8F8F11185D1694CB22893C93E90D3C943BCCC98DB4703`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`: SHA256 `9D4AEB7FB0C0808B2541A5E2860F7AED94095085AFF811ED2D59F452AD3537D8`; 原活跃文件 相同.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs`: SHA256 `16780DD0228DA1E8F4F3593A5F7DA78F08904F752A79C0F850A75B1FE87D0CCD`; 原活跃文件 相同.

## 进行中

- 本次独立只读审查及 F7 反证更正已收尾, 无仍在执行的检查或子代理.
- 早期候选已收敛: Void 进入牌为 F1, Serpent 过滤为 F5, Demon 账本为 F4, 敌方伤害范围为 F6, 新局内容同步为 F8. 原有逐面已确认记录保留在上方.
- F7 已撤回且不需要按原建议修复; 本轮未修改产品代码. 其余 F1-F6 与 F8 已由实现批次接收, 修复和真实行为验证由主会话继续. 本审查不再扩展范围.

## 未知

- 显式启用 SPIRE1_FORM_MOD 的完整工程构建成功及 Forms 类型存在已有中央直接证据, 不再列为未知. 游戏入口可达性, 资源/图标/本地化加载, 角色选择与卡牌进入方式仍未实机验证; 编译成功不等于行为验收.
- 其余 F1-F6 与 F8 的行为复现建议尚未由本审查执行. 真实支付, 每次伤害, 力量回滚, 抽牌选择, 回合到期和联机行为均没有本轮实机证据; 无已运行隔离复现.
- Void/Echo/Demon 私有状态在活动战斗克隆, 重放或恢复中的消费者未完整审计. 官方私有数据克隆会重置这一事实, 本身不足以证明当前功能发生存档或联机错误.
- Celestial 抽牌传入 ThrowingPlayerChoiceContext 时, 实际会触发何种玩家选择, 以及异常/战斗结束后是否残留形态效果, 本轮没有建立可达反例.
- AutoAnthony 的实际运行版本, Harmony 弱类型结果参数的 JIT/装箱, 真实加载顺序, 可选扩展组合及跨版本行为未执行验证. 源码中的历史反编译和验收宣称未作本轮证据.
- 内容快照的模型注册, SavedProperty 磁盘/联机往返, 正常读档时事件池既有内容, 与可选配置同步组件共同运行的行为未实测. F8 仅给出没有额外同步保障时的新局反例.
- 明确 held-back 的 AftpFireFlyPerfCompat 与 AftpCardStateCompat 未解除门禁, 未作为本轮缺陷; 路径迁移的既有修改未覆盖或提交.
- 实际模型和 provider 路由元数据不在本轮已获取证据中. 请求里的模型/路由文字不代替元数据证明.
