# 引擎/mod 契约与不变量目录（Invariants Catalog, 池架构卷之外）— sts2-spire1 知识库

> 定位：`pool-architecture.md` 收录池/注册表族（I1-I3）；本卷收录其余一切"**vanilla 代码依赖但引擎不强制**"的契约与不变量。每条格式：陈述 / 事故证据（DEVLOG 行号锚点）/ 正确做法 / 检测。这是"asker 型 agent 读 KB 发现问题"的弹药库；评审时配合 `semantics-review-checklist.md` 使用。
> 维护：每次实机事故结案后追加；编号连续（池架构卷的 I1-I3 不在此重复）。
> 行号锚点为 DEVLOG.md 写作时快照，漂移时按关键词检索（各条附关键词）。

---

## I4 联机状态一致性：一切牌堆/状态变更必须走同步分发层

**陈述**：多人对局中，任何直接修改本地 pile/creature 状态而不经 Cmd/同步层的写法，都会造成"单侧生效"——牌堆数量分歧、checksum 分叉、对局错位。
**事故证据**：DEVLOG "联机粘液失同步验尸"（行 778-780）：官方状态牌 SLIMED 打出后 `players[1].piles.Draw` 本地 12 vs 远端 11（远端少一张 STRIKE_REGENT），**全部 RNG 流一致** ⇒ 排除种子问题，定性"抽牌动作未同步"；高危嫌疑为第三方 mod 直插 DrawCmd（Multiplayer Limit Break）。同局另一模式："finished execution, but was in state Canceled!"（任务取消后仍继续执行）。
**正确做法**：我方补丁层联机安全三查（DEVLOG 已核：RewardClamp/SplashOwnSetSubtract/DustyTomeFallback 均按 player 参数取上下文、无静态单例假设）；新增任何状态写入必须走 `CardPileCmd/CreatureCmd/PowerCmd` 等命令层。
**检测**：代码评审（找直改 `pile.Cards`/`_powers` 的补丁）；联机复测用最小 mod 集二分。**冒烟为何测不出**：autoslay 单机跑，联机分歧只有真实双人局能暴露。

## I5 模型生命周期：canonical 模型只读，战斗实例必须 ToMutable

**陈述**：`ModelDb` 里的 canonical 模型是**模板**；战斗内 Obtain/CloneCard/入堆/改字段必须先 `ToMutable()`（或经 CreateCard 等工厂）。直接操作 canonical 会得到 "used in incorrect place" 类异常或静默污染全局模板。
**事故证据**：DEVLOG 行 772（尘封魔典遗物链实测）：`relic.ToMutable()` + `SetupForPlayer(player)` 后 `RelicCmd.Obtain` 才成功；卡牌注入器四代失败的复盘——"跨角色注入的 canonical Owner=null 是根因"，历史覆盖增长全部来自自然出牌。
**正确做法**：注入/生成一律 `ToMutable()` → 填 Owner/上下文 → 走 Cmd；跨角色注入需 per-player 上下文（player.Character/player.PlayerRng）。
**检测**：代码评审（canonical 直用模式）；运行时靠 "used in incorrect place" 异常兜底（但注入器失败会静默——见 M4/I6 教训）。

## I6 资产管线：文件存在 ≠ 内容可用；"已映射"声明必须带证据

**陈述**：占位图/空文件/错误槽位在文件枚举意义上"存在"，消费方（渲染）照样失败或显示灰块。资产完成度断言必须附带尺寸/字节数/内容证据；且要确认**消费方实际读取的槽位**（Harmony 重定向会让"自以为的路径"失效）。
**事故证据**：DEVLOG 行 708（战果 #5）："331/331 mapped"实际 302 张 ~314B 纯色占位；卡面主图走 big 槽（BaseLib `CustomCardModel.cs:268-311` 把 PortraitPath 重定向到 CustomPortraitPath）——"光看自己基类会漏"。
**正确做法**：资产校验脚本必须检查字节数阈值与实际消费槽位（小图/大图分离）；占位重生成脚本只动 <阈值 文件（幂等）。
**检测**：资产 lint（字节阈值 + 尺寸）；目验用户清单（不可省略）。

## I7 本地化：变量通配符必须与 C# 注册名精确一致；eng 不是权威

**陈述**：SimpleLoc 把 `!X!` 转 `{注册名:diff()}`，特判表仅 D/CD/B/CB/C/E/H 七字母，**M 不在表内（透传原名）**；SmartFormat 按 C# `CanonicalVars` 注册名解析。zhs 缺键回落 eng——eng 自己也可能写错。唯一权威 = C# 注册名。
**事故证据**：DEVLOG 行 732-738（修复 #9）：全量扫描器收敛 5 卡 10 处失配（Aggregate !E!、Claw !M!→!Increase!、Halt/Prostrate !M!→!MagicNumber!、Streamline !M!→!CostReduction!）；"715f42d 的对齐 eng 标准不充分"。另见行 732 修复 #8：`CardModel.SelectionScreenPrompt`（CardModel.cs:129）缺 `.selectionScreenPrompt` 键直接 throw → 出牌僵死。
**正确做法**：新卡落地时以 C# 注册名为准写双语文案；用选牌界面的卡必须带 selectionScreenPrompt 键（两语言）；审计脚本比对"每卡注册变量名 vs 中英通配符"（既有扫描器模式，注意 loc 键带连字符——假绿教训见 DEVLOG 非 cards 域审计段）。
**检测**：通配符审计脚本（ cards + events 两域已零失配，作为回归基线）；lint 应纳入新卡 PR。
**2026-09-05 评审门实测（I7 的选牌提示键子项，含一次勘误）**：首轮交叉核对报 "DualWield 缺键即炸"，复核后为**扫描器误报**——卡牌 loc id 的分隔符是**下划线**（`SPIRE1-DUAL_WIELD.selectionScreenPrompt`，类名 CamelCase→`_`→大写），连字符蛇形是 events 域的习惯，两域不同。修正后的事实：`SelectionScreenPrompt` 属性（CardModel.cs:129-141，protected，缺键即抛）只被**直读它的卡**需要（当前= DualWield，键在，绿）；仅调用 3 参 `FromChooseACardScreen`（CardSelectCmd.cs:252）的卡（ForeignInfluence/Wish 等）不需要键——横幅是通用 `gameplay_ui:CHOOSE_CARD_HEADER`（NChooseACardSelectionScreen.cs:255），不回读卡属性。教训入 research-methods.md M18。

## I8 类名/标识符假设：静态解析必须兼容数字与符号变体

**陈述**：我方命名含数字（Spire**1**CardPool、SPIRE1- 前缀、StS1 名字变体 wreath/wreath 拼写差）。任何静态扫描的正则若写 `[A-Za-z]+`，会静默漏掉带数字标识符——**假绿**。
**事故证据**：DEVLOG 战果 #6 教训（"`[A-Za-z]+` 匹配不了带数字的类型名（Spire1CardPool）"）；池归属 lint（tools/pool-audit.mjs）首版两次返工（主构造函数 `class Cleave() : Spire1Card(...)`、`[Pool]` Inherited 基类链——research-methods.md M7-M9）。
**正确做法**：标识符正则一律 `[A-Za-z0-9_]`；扫描器对"主构造函数/继承链/Attribute Inherited"三种语义逐个写测试样本后再上线。
**检测**：lint 自身用已知案例做单测样例集（GA 漏挂、Cleave 主构造、孪生白名单）。

## I9 事件/遗产效果对池的隐式数量要求

**陈述**：引擎事件可能对"角色池"提出结构性要求（不重复张数、稀有度分布、Ancient 稀有度存在性）；不满足时从崩溃（CreateForReward 循环耗尽抛异常）到 NRE（NextItem(空集).Id 解引用）形态不一。
**事故证据**：DEVLOG 战果 #6（ROOM_FULL_OF_CHEESE ≥8 Common，行 ~724 段）+ "尘封魔典认知修正"段（DustyTome 对四代角色 Ancient 牌数=0 → `NextItem(空集)` 解引用 NRE；修复 = DustyTomeAncientFallbackPatch 回退官方池）。
**正确做法**：新角色入池跑容量契约（P5/I2c）；补丁空集回退必须 per-player 上下文。
**检测**：lint（容量统计）+ 控制台实测（`relic add DUSTY_TOME`，modded 运行控制台 NDevConsole.cs:359 可用）。

## I10 内容注册 vs 首次枚举的时序（与池架构 I0b+ 互链）

**陈述**：ModHelper.AddModelToPool 在池内容冻结后抛异常；AllCharacters/内容表在 mod 初始化期可能为空（KeyNotFoundException 'CHARACTER.IRONCLAD'）。跨 mod 兼容层必须惰性扫描（AssemblyLoad 兜底 + 首次调用时增量注册）。
**事故证据**：chaosbridge 设计与 Session 27 gotchas（"ModelDb.AllCharacters cannot be enumerated at mod-initializer time (content tables empty)"）；engine-dllsrc `ModHelper.cs#AddModelToPool`（"too late!" 异常）。
**正确做法**：ChaosBridge 模式——对每个后续装配的程序集增量扫描注册；不假设加载顺序（无依赖边时顺序随用户 mod 列表，engine-facts.md 行 27）。
**检测**：多 mod 组合冒烟（桥接日志行断言，如 "AutoAnthony bridge: X -> Y"）。

## I11 候选集双重过滤契约：任何池读取必须过 GetUnlockedCards（解锁态 + 联机约束）

**陈述**：StS2 池读取的规范入口是 `pool.GetUnlockedCards(unlockState, CardMultiplayerConstraint)`——内部做两件事：`FilterThroughEpochs`（解锁/纪元门，虚方法）+ 按运行类型的 `MultiplayerConstraint` 互斥过滤（None/MultiplayerOnly/SingleplayerOnly 三态，CardPoolModel.cs 行 101-116）。绕过它直读 `AllCards` 会同时漏掉两层。
**为何重要**：直读 AllCards 的代码会（a）把未解锁/错误纪元的卡发给玩家；（b）在联机局发单人限定卡（或反之）——两类都无异常、纯语义错误，冒烟不可见。
**正确做法**：一切候选集/赠卡/变形入口走 GetUnlockedCards 并传入运行真实 constraint（各 vanilla 调用点均传 `RunState.CardMultiplayerConstraint`）；移植卡的 `MultiplayerConstraint` 若未声明默认 None（恒可用）——如需限定必须显式。
**检测**：评审 grep `.AllCards` 的消费点（ModelDb.AllCards 的 Distinct 全集另有用途，区分对待）；跨对照 invariants I4（联机状态一致性）。
**三层细化**（2026-09-05 补）：解锁门的具体实现 = 各角色池**覆写 FilterThroughEpochs**，按 Epoch 解锁包逐个 `RemoveAll(IsEpochRevealed<XEpoch>() ? 保留 : 删)`（IroncladCardPool.cs 行 122-140：Ironclad2/5/7Epoch 三包示范；五角色池+Colorless 均覆写）。⇒ 候选集过滤实为**三层**：纪元包（池子类实现）→ 联机约束（枚举互斥）→ （I13 的）存档侧不持久化该状态。BaseLib `CustomCardPoolModel` 未覆写 ⇒ **mod 卡恒可用**（移植 mod 期望行为；要解锁门须自行覆写）。出处补 `Models.CardPools/IroncladCardPool.cs#FilterThroughEpochs`。

## I12 联机同步拓扑：一切跨端状态变更走专用 Synchronizer；checksum 纪律不容绕过

**陈述**：StS2 联机把"跨端可达的状态变更"全部收敛到 `Multiplayer.Game/*Synchronizer` 专用通道：ActChange、Event、EventCombat、Flavor、MapSelection(+Vote)、Reward、RewardsSet、RestSite、TreasureRoomRelic、Reaction、OneOff（`MegaCrit.Sts2.Core.Multiplayer.Game/` 全目录）+ 战斗态 `Multiplayer/CombatStateSynchronizer`。任何**绕过 Synchronizer 直改共享状态**的补丁=单侧生效（I4 的机制根源）。
**checksum 纪律**（`ChecksumTracker.cs`）：宿主/客户非对称——client 生成后发 `ChecksumDataMessage`，host 接收比对（行 119-140）；每端维护**最近 20 条** TrackedChecksum 滚动窗（行 180-186），按 id 配对、乱序进队列等待、过期 divergence 报错（行 205-211）；分歧经 `StateDivergenceMessage`/`StateDivergenceException` 显式化。指纹上下文 = action 类型名或上下文字符串（行 168-170）。CombatManager/回合机在每段变更后插桩（"After player turn phase one end" 等，turn-machine 卷 T06）。
**mod 内容一致性边界**：引擎只同步**状态**；池**内容**一致性由"两端装同一套 mod"保证（DEVLOG 实录：双方 BaseLib 同版本号不同构建源仍完成整场战斗，但分装包 character.txt 只影响可见性；BaseLib 构建源不一是真实隐患）。第三方随机池 mod（AutoAnthony）自带 host-authoritative 池快照与 regenerate 校验（chaosbridge-design.md），引擎不管 mod 池的同步。
**正确做法**：我方补丁联机三查（per-player 上下文/无静态单例/走 Cmd+Synchronizer）；新增跨端可见行为必须新建或复用 Synchronizer 而非本地直改。
**检测**：代码评审（直改 pile/_powers 的补丁模式）；联机最小集复测 + checksum 日志比对（`.tmp/p1-smoke` 流程）。**冒烟为何测不出**：autoslay 单机；checksum 只在真双人局活跃。
**出处**：`Multiplayer.Game/ChecksumTracker.cs`（全文 303 行）+ Synchronizer 目录清单。置信度：**高**。

## I13 存档持久面契约：卡牌只有五个字段能活过存档边界

**陈述**：`SerializableCard`（Saves.Runs/SerializableCard.cs）持久化集合 = **{ ModelId, CurrentUpgradeLevel, Enchantment, SavedProperties(Props), FloorAddedToDeck }**，别无其他。一切战斗内临态（damage/block/temporary/costForTurn 类）不在持久面。
**推论（移植硬约束）**：任何跨战斗成长（永久加攻、费用永久降、复活标记……）**必须**落在 `[SavedProperty]` 标注的模型属性上（Attribute: SerializationCondition 默认 AlwaysSave + order 字段控制序列化先后）——GeneticAlgorithm 的 DeckVersion 方案即此契约的正确实现（DEVLOG GA 修正段）；忘走 SavedProperty = 下场战斗/读档后成长静默蒸发，无异常。
**模型身份**：存档以 **ModelId** 引用模型（ModelIdRunSaveConverter），mod 模型靠程序集类型稳定生成 id——重命名类 = 老存档断链（含 SPIRE1-* 卡）。
**RNG 持久面**：`SerializablePlayerRngSet` 按流类型存 Seed + 计数（Dictionary<PlayerRngType, SerializableRng>）⇒ 读档续局确定性成立的前提是**流清单不被新版本改名/增删**（PlayerRngType 枚举是存档格式的一部分）。
**出处**：`Saves/SerializableRun.cs`（Acts/Modifiers/EventsSeen/Players/MapHistory/Ascension/NumReloads）、`Saves.Runs/SerializablePlayer.cs`（HP/MaxEnergy/Gold/BaseOrbSlotCount/Deck/Relics/Potions/Discovered*）、`Saves.Runs/SerializableCard.cs`、`SavedPropertyAttribute.cs`。置信度：**高**。

## I14 联机加入契约：握手只带解锁态，跑局内容宿主权威

**陈述**：`Multiplayer.Game/JoinFlow.cs#AttemptJoin`（行 168-183）：客户端 → 宿主只发 `maxAscensionUnlocked + unlockState.ToSerializable()`；跑局内容由宿主权威持有与分发（客户端经同步消息接收）。**池不传输**——两端各自按本机 mod 集重建（I0b+），因此"两端同 mod 同版本"是内容一致的前提（I12）。
**事故证据**：DEVLOG 联机验尸——双方 BaseLib 同版本号不同构建源仍完成整场战斗（可见性分装无碍状态一致），但内容差异属高危（同 ModelId 不同语义会静默分叉，checksum 只在变更点采样）。
**检测**：联机冒烟最小集 + 校验和日志比对；兼容层（ChaosBridge/AA 桥）必须两端同装（chaosbridge-design.md 前提）。
**出处**：`JoinFlow.cs#AttemptJoin`。置信度：**高**。

## I15 联机奖励分配：每玩家独立奖励栈，无共享池

**陈述**：`RewardsSetSynchronizer`（453 行）为每玩家维护独立 `rewardsStack`（RewardSetState 栈）+ `completedRewards[setId]` 三态（None/Completed/Skipped）；本地 `SelectLocalReward/SkipLocalRewardsSet` 与远端 `HandleRewardSelectedMessage/HandleRewardSetSkippedMessage` 汇入同一 `SelectRewardForPlayer(player, …)`——**各自结算各自的奖励集**，不存在"抢同一张卡"的共享池语义。
**完成判定**：`set.AllRewardsSuccessfullySelected` → Completed；跳过 → 未选奖励逐个 `OnSkipped()`（药水消失类语义在 reward 实现里）→ Skipped；完成后弹栈 + completionSource 放行。
**乱序容忍**：消息按 setId 缓冲（BufferedMessage），集合未知时入队、产生后回放；`RewardSynchronizer`（341 行）另有 RunLocation 定向缓冲（RewardObtained/GoldLost/CardRemoved 三类消息）。
**正确做法**：影响奖励的补丁必须同时考虑本地与远端消息两条入口（改一条漏一条=单侧生效，I4 同族）；跳过语义（OnSkipped）不可绕过。
**检测**：联机双人同选/一选一跳/双跳三场景冒烟；代码评审找只挂本地入口的补丁。
**深水区补（2026-09-05）**：本地入口是**网络优先**——`SelectLocalReward` 先 `SendMessage(RewardSelectedMessage)` 再本地应用（乐观应用，行 207-232）；远端消息若 setId ≥ 本端 nextId（集合尚未创建）→ 进 bufferedMessages 等创建后回放（行 249-273）；重复完成不崩——`CompleteRewardsSet` 检出已完成只 Log.Error（行 371-380）。关闭本条"中置信"残留。

---

## 维护规则

- 新事故 → DEVLOG → 本卷编号条目 → semantics-review-checklist.md 加提问（三联动）。
- 条目一旦"已被代码修复"仍保留：本卷记录的是**契约**（为什么必须这样写），不是补丁清单；补丁位置在 DEVLOG/代码注释。
