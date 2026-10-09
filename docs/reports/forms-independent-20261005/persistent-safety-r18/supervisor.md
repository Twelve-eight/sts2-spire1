# supervisor.md - persistent-safety-r18 监督审查

范围: G:\omp works\Sts\sts2-forms\mod\FormsCode
本批 worker 精确 id: 01a109c2-512b-7803-a873-dae6b8bbf773
模型/路由: global:deepseek-v4.1-flash / wb2api (用户本轮唯一指定)
边界: 仅静态审查; 未构建/未 lint/未测试/未部署/未跑游戏/未 git. 实机由 hub 集中执行.

## 已确认

### S18-00 门禁与 hash
- phase2-gate-notice.json: NATIVE_WORKER_COMPLETED / multi_agent_v1.wait_agent / Target 01a109c2-512b-7803-a873-dae6b8bbf773 / completed / TimedOut=false.
- 7 文件 SHA256 与 gate-notice 逐条一致 (2026-10-05 复核), 长度也一致.

### S18-01 支付前 ShouldPlay 与其它 native hooks 签名/dispatch 位置 (权威源码核对)
- 支付前: PlayCardAction.cs:82 `CanPlay` 在 :91 `SpendResources` 之前; CardModel.cs:1746 内 `Hook.ShouldPlay(...)`; Hook.cs:2370 签名与 AbstractModel.cs:2347 虚方法签名一致; FormStanceModifier.cs:33-37 override 一致.
- 伤害: AbstractModel.cs:608 签名; Hook.cs:415 dispatch; CreatureCmd.cs:285 在 :287 DamageBlockInternal / :293 LoseHpInternal 之前; FormStanceModifier.cs:40-51 一致.
- power amount: AbstractModel.cs:1057 签名; Hook.cs:1020; PowerCmd.cs:124(Apply)/231(ModifyAmount) 在 ApplyInternal/SetAmount 之前; FormStanceModifier.cs:54-64 一致.
- 回合: AbstractModel.cs:1247 BeforeSideTurnStart(ICombatState), :1354 BeforeSideTurnEndVeryEarly(IEnumerable<Creature>); Hook.cs:1156/1244; FormStanceModifier.cs:67-88 一致.
- PowerCmd.Remove 缺口: PowerCmd.cs:291 非泛型, :295 RemoveInternal + :297 AfterRemoved 不经 model hook; 泛型 :282 内部转调非泛型. engine 内 RemoveInternal 调用仅 PowerCmd.cs:295 (其它命中为 OrbModel/CardPile/RelicModel). prefix 覆盖两条路径, 精确.

### S18-02 selected/live 门禁 (要求 #4)
- FormStanceMode.cs:51-73: 非 Forms 放行; 再要求 IsInProgress && !IsEnding && !IsOverOrEnding && IsCurrentLiveCombat(CurrentCombatId) && ReferenceEquals(combatState, DebugOnlyGetState()); 仅当前 live 且桥不可用才 throw.
- 不使用 CombatState.IsLiveCombat (CombatState.cs:614 恒 true), 符合 r17 结论.
- Forms modifier 是 RunState 级 (RunState.cs:545/573 `list.AddRange(Modifiers)`; CombatRoom.cs:83 把 runState.Modifiers 传入 CombatState), 故 `RunState.Modifiers.Any(m => m is FormStanceModifier)` 对该局所有玩家成立, 多人下 FirstOrDefault 取不到玩家时仍由该回退命中, 非漏洞.
- teardown: IsEnding/!IsInProgress 提前放行; PowerCmd.Remove 对 null/非 stance/非当前/ending 放行 (FormStanceSafetyGuard.cs:95-105). 未误伤.

### S18-03 独立 owner 安装 proof / 幂等 / 扫描排除 / 初始化失败不 Bound (要求 #2/#3)
- FormStanceSafetyGuard.cs:23 HarmonyId="Forms.FormStanceSafety", 与 MainFile "Forms" (MainFile.cs:22) 及桥 "Forms.FormStanceMode.Watcher" (FormStanceWatcherBridge.cs:38) 三个 owner 互不相同.
- MainFile.cs:60-67 EnsureInstalled 在 InstallOwnPatches 之前显式执行; MainFile.cs:104-105 常规扫描 `type == typeof(FormStanceSafetyGuard)` continue; 失败走 MarkOwnerPatchesFailed → Terminal 并 return, 不 Bound.
- PatchesHealthy (MainFile.cs:44-45) 追加 FormStanceSafetyGuard.IsInstalled; Bridge.cs:177 `if (!MainFile.PatchesHealthy) throw` 使安装/证明失败无法 Bound.
- ProofHoldsLocked (FormStanceSafetyGuard.cs:78-93): 仅当 info.Prefixes 中 owner==HarmonyId && PatchMethod==prefix 计数==1; 目标由 GetMethod(nameof(Remove), {typeof(PowerModel)}) 精确取得 (仅一个 (PowerModel) 重载).
- MainFile.Shutdown 用 `UnpatchAll("Forms")` (MainFile.cs:201) 撤本 owner, 不触 "Forms.FormStanceSafety"; SafetyGuard 故意驻留, 与两个原 owner 撤销不矛盾.
- SafetyGuard usings 仅 System/Reflection/HarmonyLib/Commands/Models + 本项目类型; 不持有 Watcher Assembly/MethodInfo/delegate. 满足要求 #3.
- 初始化失败: EnsureInstalled throw → _installed=false + MainFile 不发布 Bound. 满足"初始化失败不能Bound".

### S18-04 pump / 线程 / 锁序 (要求 #3/#6)
- FormStanceBridgePump.cs:17-19 `_Ready` 设 ProcessMode=Always; 注释说明仅本节点, 不解除游戏暂停.
- PumpTick (Bridge.cs:495-560) 每帧 O(1): Bound 仅 `_assemblyLoadPending` 为真才核对; Retryable 有界突发 + 慢节奏单次 re-arm; 无每帧 AppDomain 全扫描; AppDomain.GetAssemblies 只在 BoundIdentityStillValid (通知后) 内.
- OnAssemblyLoad (Bridge.cs:383-391) 仅置 pending/epoch + 写字符串引用, 不触碰 Godot/Harmony; 在非主线程安全.
- StopPumpLocked (Bridge.cs:450-484): 非创建线程只置 `_pumpStopRequested`, Godot 释放留给主线程 PumpTick; 因 ProcessMode=Always, paused 树仍会 tick → 可释放 (静态推理).
- 锁序: Bridge.Gate → SafetyGuard.Gate 单向 (PatchesHealthy); SafetyGuard 持锁期间不再取 Bridge.Gate (EnsureInstalled 的 throw 在锁释放后由 MainFile catch 调 MarkOwnerPatchesFailed). 无 ABBA 死锁路径.
- Shutdown 非空理由: Bridge.cs:306 `UnavailableReason="Forms bridge shut down; restart the game process"`; AssemblyLoad 通知写 provisional 理由 (:390); Bound 身份复核通过清回空串 (:158). 满足要求 #3.

### S18-05 非 Forms 原生不变 / 注释 (要求 #1/#5)
- FormStanceMode.cs:26-30 IsSelectedAndBound 语义未变 (仅 Bound 才 true); 注释已改为"仅非 Forms 返回 false 保留原生", 已选+失效改由 fail-closed 入口拒绝. 与契约一致.
- 未改 6 个效果数值/技能/旧 CustomID/资源/反射协议 (diff 仅新增 hooks + 注释 + KindOf 三个 FullName).
- marker 权威 FullName 仅 WatcherMod.Calm/Wrath/Divinity + 本项目 carrier + Spire1 三型; 未猜额外类型.

## 进行中

- 无 (3 分钟窗口内收敛).

## 未知 (未覆盖, 保留)

- 未构建/lint/测试/部署/游戏/git; 编译与运行未由本会话验证.
- 实机 UI: ShouldPlay/回合边界 throw 在 UI/交互队列的真实表现 (交互 ActionExecutor 是否记录后继续队列) 未验证.
- terminal identity changed 真实收敛 (ProcessMode=Always 的实机因果) 未验证.
- long combat / 跨进程读档 / 多人 / 真热替换 / 性能 未验证.
- 覆盖完备性: 是否存在不经 5 个 model hook 与 Remove prefix 的其它状态变更路径, 需实机/动态枚举确认.

## 结论项 (最多 3 项, 均为 note 级, 非阻塞)

1. [P3] CombatManager.DebugOnlyGetState 生产使用
   - 绝对路径: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs:69
   - 权威契约: CombatManager.cs:303 "THIS IS TEMPORARY AND SHOULD ONLY BE USED IN TESTS"; :306 DebugOnlyGetState.
   - 触发: 任意 selected Forms live 战斗的 model hook / RemovePrefix.
   - 现状: 用该测试专用 API 做当前 state 引用核对. 当前引擎行为正确, 但违反引擎自述契约, 属脆弱点.
   - 最小修复范围: 若引擎提供生产可用的当前 state 访问器则替换; 否则保留并在 DEVLOG 记录为已知偏离.
   - 缺: 引擎是否有非 test-only 的等价访问器未在本轮穷举.

2. [P2] ShouldPlay throw 可能从 UI/CanPlay 路径重复抛出
   - 绝对路径: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:33-37 → FormStanceMode.cs:51-73
   - 触发: 已选 Forms 且桥不可用, 玩家悬停/尝试出牌 (CanPlay 被 UI 反复调用).
   - 契约: 要求支付前 fail-closed 抛 InvalidOperationException (已满足), 但未规定 UI 反复调用的频率上限.
   - 现状: 每次 CanPlay 都会抛; 可能造成异常刷屏/UI 卡顿.
   - 最小修复范围: 若实机观测到刷屏, 在抛点前加一次性日志或限频; 不改 fail-closed 语义.
   - 缺: 实机 UI 调用频率与异常处理方式.

3. [P3] EnsureInstalled 丢失 proof 后的重打补丁行为
   - 绝对路径: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs:50-75
   - 触发: _installed 为真但 ProofHoldsLocked 为假 (patch 被外部移除), 再次 EnsureInstalled.
   - 现状: 会对同 owner+同 PatchMethod 再次 harmony.Patch, 随后 Proof 计数!=1 → 置 _installed=false 抛异常 → 保持不 Bound (fail-closed 正确). 但"同 owner 同 PatchMethod 重复 Patch"的 Harmony 去重/替换语义未验证, 可能短暂存在 2 条 prefix.
   - 最小修复范围: 重装前先 `harmony.Unpatch(_target, All, HarmonyId)` 或复用已有 Harmony 实例语义.
   - 缺: Harmony 对同 (owner, PatchMethod) 重复 Patch 的实测语义.

## SUPERVISION_PASS (仅静态面)

- 要求 #1-#6 的静态面均已核对: 支付前 ShouldPlay 与四个 native hook 签名/dispatch 位置正确; 唯一必要 engine prefix 精确覆盖 PowerCmd.Remove(PowerModel?); Safety owner 固定 engine 方法且无 Watcher 引用; 三个 owner 分离, Shutdown 驻留与两原 owner 撤销不矛盾; 扫描排除 + 精确 proof + 初始化失败不 Bound; selected/live/ending/teardown 放行正确; pump 每帧 O(1) 且 ProcessMode=Always, 非主线程不触 Godot; 锁序无 ABBA.
- 3 项 note 级为 P3/P2 脆弱点/风险, 非已证实缺陷; 未构成 NEEDS_REWORK.
- 本结论不声称编译或实机通过; 实机边界见"未知".