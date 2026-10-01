# 姿态形态可玩化契约 - 2026-09-28

## 当前目标与授权

- 继续实现 2026-09-26 已确认的六形态规则, 交付真正可以从游戏入口使用的构建, 不把编译成功或脚手架当成可玩验收.
- 主会话负责契约, 协调, 集中构建, 隔离和运行验证, 备份. 实现子代理只写明确白名单, 不构建或测试. 每名实现者同批配一名等待完成后审查的监督者.
- 本轮用户指定所有子代理为 Astra via agentrouter. 当前主会话和独立审查子会话元数据为 gpt-6-astra-ar / gateway, 注册表映射 agentrouter / gpt-6-astra, 未见该模型 fallback. 后续逐个核验实际元数据.
- 用户正在前台玩 CS. 禁止激活窗口, 键鼠控制, 弹出可见游戏或工具窗口, 停止用户游戏. 集中构建单进程和低优先级. 实机只考虑无窗口且存档/配置完全隔离的测试; 做不到则明确留给用户目视验收.
- 不写 Steam 安装, 不改共享 mod_configs, 不发布 Workshop. 测试 DLL 只可部署到 E:\Slay the Spire 2 或副本 B, 部署前检查进程与目标身份. 所有生成物和缓存写 G:.

## 本轮前基线

- 活跃根为 G:\omp works\Sts\sts2-spire1. 工作区迁移改动和其它未提交源码均保留, 不整体 git add 或 reset.
- 固定审查快照为 G:\omp works\.tmp\form-playable-20260928-01a0e7ad\baseline.
- 既有研究实际存在于 research\RESEARCH-form-mod-20260926.md. 它是线索, 旧模型和实测声明不作为本轮证据.
- StancePower 和 StanceCmd 实际在本仓 Powers 与 Extensions 下. 旧摘要的缺少 Watcher 编译依赖判断不成立.
- 10 个 Forms 源文件被 SPIRE1_FORM_MOD 包围, 默认未定义; 无进入调用者. 本轮显式定义后的编译阶段已生成 DLL, 完整构建结果以 before-build.log 最终输出为准.

## 可玩入口和隔离

- 继续作为 Spire1 的独立可选玩法交付, 不新建重复源码仓, 不新增未经规格授权的平衡数值卡.
- 在原生自定义对局的修正列表中加入姿态形态选项. 用真实 ModifierModel/CustomModifierModel 作为每局启用身份, 随原生存档和多人建局协议持久化. 不用全局静态 bool 或共享配置决定进行中对局的规则.
- 该选项只进入自定义对局的手选列表, 不进入 GoodModifiers/BadModifiers 的随机每日或涅奥池. BaseLib 3.4.5 的 CustomModifierModel 已确认存在; Alignment.None 配合 NCustomRunModifiersList.GetAllModifiers 的窄后缀可以实现.
- 主要正常玩法入口为已安装的 Watcher 角色和现有进入/退出姿态卡. 保留 Watcher 原始 Calm/Wrath/Divinity Power 标记及原始 OnStanceChanged 通知, 这样 Rushdown, Mental Fortress, Flurry of Blows, Violet Lotus 等现有消费者仍识别原类型.
- Watcher 通过严格签名检查的可选反射桥接接入, 不添加编译期 AssemblyRef, 不复制或修改 Watcher 二进制. 绑定失败应清楚记录并隐藏或阻止该模式入口, 不半替换玩法.
- 默认构建编译所有形态, 移除失效的 SPIRE1_FORM_MOD 门. 两个已有 AFTP held-back 编译门保持原样.
- 无姿态仍为无形态. 同姿态重复进入幂等, 不重复发入场资源. 切换必须先完整退出旧形态再进入新形态.
- 本仓 StanceCmd 与 FormStanceCmd 的支持路径也须可达且保持语义一致. 外部 Watcher 与内部姿态不得同时叠出两套效果. 不为测试而留下误导性的空入口.

## 六形态行为

### 平静: 虚空与群蛇

- 虚空: 进入后本回合下一张手动打出的牌免费; 后续每个自己的回合开始重置一次免费额度. 进入姿态的那张牌不能追溯消费新额度. 能量和星星费用按官方免费牌 hook 处理, X 费用语义遵循引擎, 不另造规则.
- 免费额度按一次手动打牌系列消费, 自动打出不消耗. 重复打出的后续次数不重复消耗费用. 必须用实际开始/完成记录区分进入牌与之后的牌, 不能只按 CardModel 身份永久排除某张牌.
- 群蛇: 每次实际完成的 CardPlay 造成 3 点无源伤害, 包含自动和重复打出, 但效果在该次 BeforeCardPlayed 时必须已经存在. 进入姿态的牌不追溯触发. 目标从 HittableEnemies 经 RunState.Rng.CombatTargets 选择, 无目标不掷 RNG.
- 离开平静时获得 2 能量且只给一次. 外部 Watcher 原 Calm 已负责这 2 点时, 自定义群蛇不得再支付. Violet Lotus 等原有额外联动保留. 战斗结束清理不发能量.

### 愤怒: 恶魔与死神

- n 为引擎当前回合数, 正常首回合为 1. 恶魔在入场时以及每个自己的回合开始提供 T(n)=n*(n+1)/2 力量, 只补相对于此前目标的差值.
- 记录实际被接受的形态力量增量, 不是盲记请求 delta. 移除时只撤回形态来源的部分, 不吞掉其它力量或产生负漂移. 特别检查 Hook 对力量授予或撤回的干预, 精确支持边界写入报告, 不声称未测交互正确.
- 从敌方来源受到的每次伤害额外加 n. 判定必须核对 target, 非空 dealer 及敌对阵营. 不给自伤或友方伤害加 n; 不能擅自把规格缩为敌方有源攻击. 若引擎无源分支跳过常规 hook, 必须报告并实现正确窄接入, 不用注释遮掩.
- 死神严格沿用当前官方 ReaperFormPower: dealer 为本人或其宠物, IsPoweredAttack, TotalDamage>0; 给该目标等量 DoomPower. 格挡部分计入 TotalDamage, 多段逐次触发. 不变成回血, 不偷换为仅看 CardType.Attack.
- 外部愤怒原始攻防乘 2 在此模式中停用, 不是叠加六形态和原姿态倍伤.

### 神格: 回响与天人

- 入场后下一张牌额外打出一次, 只消耗一次额外打出机会. 保留其它合法重放来源的 playCount, 不覆盖其次数. 进入神格的那张牌不能追溯获得或消费新回响.
- 入场立刻获得 max(n,3) 能量并抽同样张数. 不叠加 Watcher 原神格额外 3 能量; 不用先多给再扣回的办法伪造等价效果.
- 下一个自己的回合开始退出, 经原姿态切换通知链完整清理. 原 Watcher 的回合结束即退在此模式中应被替换, 原神格攻伤乘 3 停用.
- 如果在回合开始 hook 内进入, 不能被同次 hook 列表立即清掉. 明确记录入场回合并用实际阶段或回合身份区分下一次开始.

## 状态, 本地化和资源

- Power 实例状态不共享 canonical/clone 可变容器, 不引入未同步随机源, 不在异步延续中碰 Godot 工作线程. 所有游戏命令必须 await.
- 状态通过引擎 Power 生命周期与 run modifier 承载. 若战中存档/重连/clone 无完整证据, 单独标未知, 不说已联机验证.
- 形态效果必须在玩家可见 tooltip 中准确解释, 有可解析的 eng/zhs 本地化与现有可用图标; 不要求新增 AI 美术. zhs 术语依据 G:\omp works\docs\terminology-glossary.md 与用户已确认六形态名称.
- 英文及中文资源不复制未知多语言文件. 代码和报告遵守 check-agent-text.mjs.

## 分工和验收

- effects 实现者只改六个效果 Power, 必要的纯效果辅助代码及 tools\form-effects-probe; 另有 effects-review 配对监督.
- integration 实现者改三个姿态承载, FormStanceCmd, 可选 Watcher 桥接, 自定义模式入口, 本地化及构建接入; 另有 integration-review 配对监督. 六效果文件由 effects 唯一拥有.
- independent-review 是用户单独要求的基线审查, 不替代任一配对监督.
- 主会话集中运行: 改前复现与改后回归, Release 构建, 编译产物类型和依赖检查, 真实 Watcher 二进制的 Harmony 绑定, 自定义模式列表和序列化往返, 无窗口真实游戏验证(若可完全隔离).
- 部署与提交只含本任务拥有的变更. 上线构建仍不能自动发布 Workshop; 不把未运行的多人/视觉路径写成已通过.
## 2026-10-01 路由恢复与本轮验收门禁

- 实际元数据发现前批返工及审核模型为 ovoapi:6.1sol.这些产物保留但不计有效实现/监督,不能沿用其通过声明.
- 后续 Codex 原生子代理显式 model=gpt-6-astra-ar;本轮独立审核和两组实现/监督 metadata 已核验为 gpt-6-astra-ar/gateway,registry 指向 agentrouter/gpt-6-astra 且无模型 fallback.
- 新有效监督必须建立真实 wait_agent 完成门禁.中央冻结源码诊断不替代正式监督后集成构建.
- 用户前台 CS 边界持续有效.只有真实 headless 且子进程环境完全隔离的入口才可考虑运行;仅 Hidden 启动请求不构成无窗口证明.尚无六形态真实战斗,视觉,存档和多人验收.
- 本轮路由,范围与中央验证恢复记录: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\route-incident-and-central-r6-20261001.md.

## 2026-10-01 群蛇桥接清理回归

- 旧 source 与新 completion bridge 共享 tracker 时,重复 AfterCardPlayed 不可重复伤害,也不得留下已无 pending 的隐藏桥.清理必须覆盖未消费 pending 的重复回调分支.
- 中央隔离复现为 central-probe-r8-20261001-01/serpent-before.log,27个场景中1个失败.不将该结果称为真实战斗或原生 scheduler 验收.

## 2026-10-01 真实 Mod 初始化证据与下一步

隔离真实游戏启动已通过,证据目录为 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r10-20261001-03`. 运行于 2026-10-01T21:40:23.4622317+08:00 至 2026-10-01T21:40:45.1586434+08:00,`exitCode=0`,未超时,21 次窗口句柄采样均为 0. 共享 `mod_configs` 哈希不变,隔离 `steam_settings` 和 `mods` 已恢复. 日志确认 BaseLib 282 个补丁成功,真实加载 `BaseLib`,`Watcher`,`Spire1`,以及 `Spire1 Forms: Watcher bridge bound; custom-run modifier available`.

这仍然只是真实 Mod 初始化证据. 尚未证明自定义修正进入一局,Watcher 原卡进入三类姿态,真实手动出牌和回合链,效果数值,视觉资源,存档或多人. 当前 P0 是在真实主线程内实现 gated 的单战斗载体,不使用 `--autoslay` 或 TestMode. 已知 Sentry,Dummy 渲染器和 Godot 资源泄漏噪声仍单列,不能写成无错误.

本轮有效子代理路由记录在 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\agent-routes-r12-20261001.json`. r12 采用 Codex 原生 `multi_agent_v1`,实际 `ovoapi:6.1sol`,provider `gateway`,请求路由 `agentrouter`. r11 解析到不允许的模型后已在写码前关闭,不采信其结果.

## 2026-10-02 P0 三形态真实出牌闭环

本节覆盖当前最终源码和最新 Release 构建，不覆盖前面旧 staging 的无效证据。

- 当前最终静态审查由 `generic-nested-task-fix-final-review-20261001.md` 和 `generic-nested-task-fix-supervisor-20261001.md` 给出，结论均为 `PASS（仅限静态审查）`。两份报告明确未把静态审查写成构建或实机通过；本批实际路由为用户允许的 `6.1sol` / `agentrouter`。
- 中央 Release 构建日志为 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-build-generic-fix-20261001.log`，结果 `58 warnings, 0 errors`。最终 `Spire1.dll`、`Spire1.pck`、`Spire1.pdb` 哈希分别为 `7418C03435ECF22A4669037D4684E5BFD05665DC2BAF517DE8B2EF56255AE000`、`C8F718AB73F3C054FC3434F1CE0277E6C7E7E76DBF96A23104CF940231EC63BD`、`425384C464B8027FABC21B3442853F155F73CA5C9C0FAF0BC65A9FB8BBA8ED59`。
- 最新隔离运行脚本为 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\run-native-form-smoke.ps1`。它只启动 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game`，使用 headless、Dummy audio、隔离 APPDATA/LOCALAPPDATA/TEMP/GSE，并在退出后清理 staging。
- 本次真实运行于 `2026-10-02T04:55:35.0674498+08:00` 至 `2026-10-02T04:56:25.1038242+08:00`，退出码 `0`，49 次窗口句柄采样均为零，stdout/stderr 排空，共享 `mod_configs` 哈希未变，隔离 mods 已清理。
- 日志确认真实加载 `BaseLib`、`Watcher`、`Spire1`，并输出 `Spire1 Forms: Watcher bridge bound; custom-run modifier available`。真实出牌链路依次包含 `WATCHER_VIGILANCE`、`WATCHER_ERUPTION_P`、`WATCHER_BLASPHEMY`。
- 三份场景 JSON 均为本次新写入且 `status=passed`、`cardPlay.status=completed`、`formGateAfter.passed=true`、`unobservedFaults=[]`：Calm 对应 `VoidSerpentStancePower` + `VoidFormEffectPower`/`SerpentFormPower`；Wrath 对应 `DemonReaperStancePower` + `DemonFormPower`/`ReaperFormEffectPower`；Divinity 对应 `EchoCelestialStancePower` + `EchoFormEffectPower`/`CelestialFormPower`。
- `form-native-smoke-final.json` 为 `status=completed`、`exitCode=0`、`quitStatus=executed-main-thread`、`quitDrainSettled=true`、`quitDrainOutcome=settled`；`run-final.json` 为 `exitCode=0`、`sharedConfigSha256Unchanged=true`、`cleanupCompleted=true`。
- 旧的 `Loaded 2 mods (4 total)` 运行因错误 staging（嵌套 BaseLib/Watcher 且缺 `Spire1.json`）明确作废，不再作为当前构建证据。
- stderr 的 Sentry crashpad 缺失、Dummy renderer/Godot RID 和资源泄漏、headless 资源缓存提示单列为环境噪声；图片文件在源码和 PCK 中存在，未据此声称视觉通过。
- 仍未验证可见 UI、长战斗的全部数值边界、存档重载、重连和多人同步。该记录证明 P0 的真实三形态出牌闭环，不等于完整平衡和多人验收。

完整本轮证据见 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-r17-final-20261002.md`。

### 效果事务审查边界

- 旧 `effects-review.md` 中的 F1/F3 结论来自较早快照，不能直接套到当前源码；当前源码已经有 `SpendResources` 返回 Task 的包装、逐卡 token 和 `PowerModel._amount` 写入后捕获。
- 本轮新增的两个独立静态审查代理都只落盘到中途：已确认当前支付期卡身份守卫和真实 Task 包装的首条事实，但没有形成最终 PASS/REWORK。它们的未完成报告不作为通过证据。
- 因此本记录关闭的是 P0 的真实三张姿态卡出牌和形态 carrier/effect 生成，不关闭完整效果事务边界。支付失败后的 pending 清理、复杂嵌套重入、力量事务故障注入、完整数值平衡仍保留为后续审查项。
