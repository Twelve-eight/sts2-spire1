# worker report - r25 binding-loss recovery

## 已确认

- 范围: 只写 4 个指定测试文件与唯一报告 worker.md; 不构建/lint/测试/部署/git; 不启动其它 harness; 不再委派; 不读凭据/路由配置; 不写 C:/Steam/共享配置/canonical/Workshop.
- 工作树基线: sts2-forms HEAD=642753fcce105dbbbf4a1f8843dd7d0ea89a9168. 4 文件相对 HEAD 均有未提交差异, 属候选实现, 不继承旧报告 PASS.
- 更正一条错误推断: 曾以为 actionFailure=true 会使 actionExecutedAndSettled 永不可达. 权威证据 GameAction.cs:120-160 在 Execute finally 中对已完成 _executionTask 设置 State=Finished 并 TrySetResult(), 故 action.CompletionTask 为 RanToCompletion, 外部 await 不抛, actionFailure=false; 真实 fault 保留在 GameAction.Exception (GameAction.cs:63). 该点不是缺陷.
- P2 缺陷已修 (BindingLossSmokeRunner.cs): 旧代码 BindingLossSmokeRunner.cs:428-431 用 ReadCardPlayHistory 读 watcherStrikeHistory, 而该字段由 FormNativeSmokeRunner.cs:1882 固定为 DescribeCardPlayHistory(player, "WATCHER_STRIKE_P"), 因此 Tranquility 探针会错读 Strike 历史. 现改为按真实 CardModel 对象 ReferenceEquals 读取.
  - 新增 BindingLossCardHistoryEvidence: card / cardTypeFullName (card.GetType().FullName) / cardInstanceIdentity (card.ToString(), 非空) / observedByCardReference / beforeStartedCount / afterStartedCount / beforeFinishedCount / afterFinishedCount.
  - 读取点 BindingLossSmokeRunner.cs:777 (before) 与 875 (after), 过滤条件 ReferenceEquals(entry.CardPlay.Card, card), 见 1298-1304.
  - strikeExecuted / probeActualCardPlay 改从各自 actualCardHistory finished delta 读; strikeStarted / probeStarted 纳入 regression; 历史缺失或负值使 safetyPassed=false (1274-1296, 541-542).
  - actualCardHistory 进入 BindingLossRunEvidence (1121), 故 nextStrike / stanceChangeProbe 均携带. watcherStrikeHistory 字段语义未改.
- P1 lifecycle 缺陷已修 (LifecycleSmokeRunner.cs): 旧 SafetyGuardProof 只按 owner+PowerModel 参数名+prefixCount==1 判定. 现要求:
  - 真实 target canonical identity == PowerCmd.Remove(...PowerModel...) (424-428);
  - prefix patch type == Forms.FormsCode.FormStanceSafetyGuard, declaring method == RemovePrefix (429-432);
  - SafetyGuardSameIdentity 增加 patch type 与 canonical RemoveMethodIdentity 稳定性比较 (436-458).
  证据权威: FormStanceSafetyGuard.cs:57-63/83-104 (精确 target 与 _prefix 身份).
- final 订阅 (FormNativeSmokeRunner.cs:163-298): TaskHelper.UnobservedFault 订阅保留到 post-quit drain; raw/expected/unexpected 均持久化; approved roots 以真实 Exception 身份传入 final 门禁; unexpected 非空即 exitCode=1, 不被覆盖.
- README.md 已补充 actualCardHistory schema/未知即失败, guard 精确身份, 故意进程驻留且不可热卸载, 并明确不声称实机已验收.
- 静态自检: 三份 .cs 去字符串/注释后 braces/parens/brackets 差值均为 0; 无旧 BindingLossCardRun 签名残留; 仅 4 个指定文件为修改状态; RuntimeSafetySmokeRunner.cs 未改.

修改文件 SHA256:
- BindingLossSmokeRunner.cs 1C6E990E46A22434DA7921E212CECD680AEB12563B0AB8EF714998476242750D
- FormNativeSmokeRunner.cs 51E79681D39A1A188F02545679A5E6D286BA7EAA9DD676C0C7A8DA3352DB5B99
- LifecycleSmokeRunner.cs 0224CFEBE15147F6F0E9C8CC0E4C5034BA651EEDBC67FF0549C981765C065DE9
- README.md 3EEF6B54D97943A172856FE8798EF25B98D2BBDDE360939528BB56564FF3C64C

## 进行中

- 无. 本阶段写集完成, 停写等待 hub 门禁与同批监督.

## 未知

- 编译是否通过; 未构建, 未测试.
- 正反例/实机证据; 由主 hub 集中验证, 本报告不宣称通过.
