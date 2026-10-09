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
