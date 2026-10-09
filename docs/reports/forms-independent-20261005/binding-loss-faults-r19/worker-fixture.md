# binding-loss-faults-r19 worker 增量报告 (FIXTURE-ONLY 阶段)

范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke
模型/路由: global:deepseek-v4.1-flash / wb2api (用户本轮唯一指定)
边界: 本轮只改 BindingLossSmokeRunner.cs 与 README.md 的 fixture 目标面; 未做 typed fault 过滤, 未改 Lifecycle, 未改生产; 不构建/测试/部署/game/git.

## 已确认

### F-00 r5 真实 terminal 停在入场, 根因是 fixture 目标错误
- r20 落盘: G:\omp works\.tmp\forms-independent-20261005\native-r5-bindingloss-terminal-r20\b1-terminal\forms-binding-loss-binding-loss-terminal.json
- 该 JSON: status=failed, failure="Real Wrath entry did not prove the selected Forms carrier/effects: selected=True, rawStance=None, bridgeStanceDiagnostic=None, carrierPresent=False, effectPresent=False"; entry.action.status=completed / state=Finished / completionTaskOutcome=completed.
- 权威契约: WatcherCrescendo 构造 TargetType 0 = None (G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherCrescendo.cs:24-25); WatcherTranquility 同为 TargetType 0 (WatcherTranquility.cs:24-25); WATCHER_STRIKE_P 为 TargetType 2 = AnyEnemy (WatcherStrike_P.cs:20).
- CardModel.IsValidTarget: null 时 TargetType != AnyEnemy 且 != AnyAlly 返回 true; 非 null 且 TargetType 非 AnyEnemy/AnyAlly 返回 false (CardModel.cs:1762-1785).
- PlayCardAction.ExecuteAction: CanPlay/IsValidTarget 不通过时直接 Cancel() 并 return (PlayCardAction.cs:85-88); GameAction.Execute 的 finally 在 _executionTask 完成后仍置 State=Finished 并 TrySetResult CompletionTask (GameAction.cs:152-160). 因此 "completed 但未出牌" 是目标类型错误导致的假完成.
- 结论: 旧 fixture 对全部牌传敌方 target, 导致 None 牌被 Cancel, 入场从未真实出牌.

### F-01 本轮最小修复已落盘
- BindingLossSmokeRunner.cs 新增 BindingLossEffectiveTarget: AnyEnemy 才传敌方 target, 其它 TargetType 传 null.
- 新增 BindingLossTargetDiagnostics: 记录 targetType / requestedTargetPresent / actionTargetPresent / isValidTarget; 仅入场阶段 (recordPreflightDiagnostics=true) 额外记录 canPlay / unplayableReason / preventer / canPlayTargeting. 桥失效后不预调用 CanPlay, 只写 canPlayDeferred, 避免消费真实 action 故障路径.
- 入场结果新增 entryBefore / entryAfter 快照字段, 便于失败诊断.
- 不把 Completed 冒充 CardPlay: passed 现在额外要求 action 后卡牌已离开 Hand (cardPileAfter / cardLeftHand), Cancel 的牌留在 Hand 会判失败.
- 未改任何数值, 未直接调用 EnterWrath, 未做故障过滤.

## 进行中

- 本会话无法构建 (hub 执行构建与实机). 静态自检已做: 花括号平衡, 调用点数量, 目标语义与权威源码一致.

## 未知

- 编译与实机复测未由本会话执行; r5 负例是否在本修复后真正进入失效阶段未知.
- typed fault 关联 / Lifecycle Harmony 元数据扩展仍未开始 (按本轮指令停止).


---

# 第二阶段 (typed fault + Lifecycle owner proof) 增量

## 进行中
- 开始第二阶段. 已确认 r5 fixture 目录 native-r5-bindingloss-terminal-r19-fixture 已创建但 JSON 未落盘 (仍在跑).
- 已确认当前 FormNativeSmokeRunner 用 ConcurrentQueue<string>, 只按 Count 判 newFault (行 165-167, 740-761, 1240-1266); 无 Exception 对象级关联.
- 计划: 新增 UnobservedFaultEvidence (Exception + ToString + 入队序号 + 时间), 保留原始证据; 门禁改为对象身份关联.
- 能量下降/HP/Block/power marker/actualCardPlay 全部纳入 mutation 判据.
- Lifecycle: 通过 Harmony Patch 真实 owner+target metadata 观测 Forms.FormStanceSafety 的 1 个 prefix, 不 direct-call.

## 未知
- 编译/实机未执行.

---

# 第二阶段 (typed fault + Lifecycle owner proof) 完成

## 已确认

### T-01 typed fault 对象级关联 (FormNativeSmokeRunner.cs)
- 新增 UnobservedFaultEvidence (Exception / Text / Sequence / ObservedUtc), 订阅时保留原始 Exception 对象 (行 165-168).
- 所有 scenario 的 unobservedFaults 由 ConcurrentQueue<string> 改为 ConcurrentQueue<UnobservedFaultEvidence>; JSON 同时输出 unobservedFaults(文本) 与 unobservedFaultEvidence(对象级).
- ApplyUnobservedFaultGate 新增 expectedRejections 参数; 通过 FaultMatchesExceptionIdentity 只按 reference identity (允许递归展开 AggregateException/InnerException) 判定, 绝不按文本.
- 门禁: unexpectedUnobservedFaults 必须为空才 unobservedFaultGatePassed=true; 否则 status=failed + terminalFailure.
- FaultMatchesExceptionIdentity 定义于 FormNativeSmokeRunner.cs (行 1296-1309).

### T-02 binding-loss expected 关联 (BindingLossSmokeRunner.cs)
- BindingLossCardRun 增加 FaultsAtStart / ActionFaultRoot / FaultsObservedDuringAction / ActionExecutedAndSettled.
- 每张牌记录 action.Exception 作为真实故障 root, 以及本次 action 期间的全部 UnobservedFault 对象; 不在 event 到达时删除.
- 只有 (1) 真实牌已执行且 CompletionTask settled, (2) observed Exception 与 action root 满足 reference identity, (3) 明确 Forms unavailable + restart, 才加入 expectedRejections.
- finally 调 ApplyUnobservedFaultGate(..., "binding-loss", expectedRejections); gate 失败强制 safetyPassed=false.
- BindingLossFaultMentionsFormsRestart 对 raw Exception 文本判 Forms+restart (仅作为第 3 条附加条件, 不是身份判据).

### T-03 副作用判据补全 (BindingLossSmokeRunner.cs)
- 能量改为双向检测 EnergyChanged (下降也算); 旧代码只判 energyAfter>energyBefore.
- 新增玩家 HP/Block (PlayerHpOrBlockChanged) 与 owner power marker 集合 (OwnerPowersChanged) 变化检测, 缺失/未知一律视为 mutated (fail closed).
- regression 现包含: strikeExecuted / damage>0 / stance 变化 / 能量双向变化 / 玩家 HP-Block / owner power marker / probe 侧同类 / stanceProbe.Passed.
- 不把 CardLeftHand none 当已出牌: cardLeftHand 需非 None 且非 Hand; 另 strikeExecuted 仍要求真实 card-play history FinishedCount 增长.

### T-04 Lifecycle owner proof (LifecycleSmokeRunner.cs)
- 新增 SafetyOwner = "Forms.FormStanceSafety", 并纳入 CountOwnerPatches.
- SafetyGuardEvidence 用 Harmony 真实元数据 (GetAllPatchedMethods + GetPatchInfo + patch owner/PatchMethod 反射) 读取 owner / 目标方法 / prefix 数量 / prefix 声明类型与方法.
- FindPowerCmdRemoveMethod 精确定位 PowerCmd.Remove(PowerModel?) (Public Static, 1 参数 PowerModel).
- SafetyGuardProof: removeMethodFound && removePatchedBySafetyOwner && prefixCount==1.
- 重复 MainFile.Initialize() 前后 prefix 身份/数量不变 (SafetyGuardSameIdentity); Shutdown 两次后两原 owner 清 0 而 SafetyOwner 仍存活且数量 1.
- 不直接调用 prefix 冒充安装; 不声称热卸载.

### T-05 README
- 已补充 typed fault 关联语义, 双向能量/HP/Block/power marker 判据, 以及 Lifecycle 的故意进程驻留 guard 区别; 未声称热卸载/完整读档/可见 UI/长战斗已通过.

## 进行中
- 无.

## 未知
- 编译/测试/实机由 hub 中央执行, 本会话未执行.
- r5 真实 terminal/shutdown 复现结果尚未落盘 (目录已建, JSON 未出); 预期拒绝的实际抛出路径未验证.
