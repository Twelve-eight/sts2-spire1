# binding-loss-faults-r19 phase3 增量报告

范围: 仅 FormNativeSmokeRunner.cs / BindingLossSmokeRunner.cs 与本报告.
请求: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\phase3.request.md
模型/路由: 请求指定 global:deepseek-v4.1-flash / wb2api; 当前可见工具未提供实际会话路由元数据, 不将请求文字冒充实测路由. 本轮未调用模型/回退/其它 harness, 未再委派.
边界: 不构建/lint/测试/游戏/部署/git, 不改生产/Lifecycle/README/控制器, 不轮询 peer.

## 已确认

### P3-01 (P1) 中央编译的两个确定错误
- 证据: G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r19-final.log:4-5,20-21.
- 当前源码: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:789 缺 GameActionState 命名空间; :849 named argument actionExecutedAndSettled 与 record 参数 ActionExecutedAndSettled 大小写不符.
- 触发: 中央构建现有 phase2 字节; 日志为 CS0103 / CS1739. 本会话未重跑构建.
- 最小修复: 增加 MegaCrit.Sts2.Core.Entities.Actions using, 改正确参数名称, 不猜枚举值.
- 尚缺: 修订后中央构建与实际游戏证据.

## 进行中

- 核对两张真实拒绝 proof 的 JSON 证据与当前 reference identity 控制流.
- 核对 final 异常分类、cleanup/quit late fault 与退订时序.

## 未知

- 修订后的中央编译/实机/同批监督结果.
- 实际会话模型/provider route 元数据未由当前工具证实.

## P3-02 已确认：编译错误修复已落盘
- 已补充 GameActionState 所属命名空间 using MegaCrit.Sts2.Core.Entities.Actions。
- 已将 record 命名参数改为 ActionExecutedAndSettled: false。
- 证据类型：源码修改；未构建、未测试，中央编译待 hub。

### 进行中
- 双 action 证明持久化与硬门禁，随后 final 分类。
### 未知
- 修订后编译和实机结果尚未验证。

## P3-03 已确认：双 action proof 已落盘
- actionRejectionEvidence 固定恰两项：Strike 与 Tranquility，早退保留 false/空证据，不计进入牌。identityMatchedFaultSequences 仅从原始 fault 经现有 reference identity helper 匹配产生。
- BeforeExecuted 对同一真实 action 对象记录执行；timeout/actionFailure 不算 settled proof。安全通过新增双 proof 硬条件；未改变 fault identity helper。
- 修正 none/None 比较大小写，null pile 不算 cardLeftHand。能量双向变化与原始 marker/伤害/HP/block 门禁保持。
- 证据类型：已落盘源码，未构建/测试。
### 进行中
- final 局部 roots 传递、全程分类与退订时序。
### 未知
- 中央编译、实机及同批监督尚待 hub。

## P3-04 已确认：final 分类已落盘
- RunAndQuitAsync 创建本次调用局部 roots，显式经 RunAsync 传到 binding-loss；仅场景中已执行/settled、Forms 重启拒绝、reference identity 非空的 action roots 被传回。普通 smoke 没有批准 roots，仍全 strict。
- pre-quit/post-quit 均复用同一原始 fault queue 和 sequence 写全部五项 fault 分类字段。cleanup/quit 期间保留订阅，在最外 finalization 的 finally 才退订；finalization 抛错也退订。
- final completed 要求 fault gate、既有 failure 为空、写证据成功、退出与有界 drain 成功。保留 quitExitCodeRequested，后续 failure 更新证据 exitCode，不能冒充已改变实际退出请求。
- 证据类型：落盘源码，未构建/测试/游戏。
### 进行中
- 仅做授权文件落盘复核与精确 hash 收割，不扩大读取范围。
### 未知
- 新字节编译/实机/同批监督结果待 hub。

## P3-05 已确认：落盘源码交付 CODE_COMPLETE
- 已复核并修正 RunBindingLossScenarioAsync 的局部 roots 参数签名，RunAsync 调用链与使用一致。
- Strike 的真实 FinishedCount 增量不再依赖 Passed；Tranquility 的真实 FinishedCount 增量与敌方伤害也列入 regression，fault 之后已有实际 CardPlay 不能误判无副作用。能量双向变化、HP/block、power marker 和原始 stance 门禁未降低。
- 本次只修改授权两个测试源文件和本唯一增量报告，未修改生产/Lifecycle/README/旧报告，未构建/lint/测试/游戏/部署/git/委派/peer/其它 harness/模型/fallback。
- SHA256（当前落盘字节）：
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs: DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A
  - G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs: 4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D
- CODE_COMPLETE 仅指代码落盘完成，不表示中央编译或实机门禁通过。

## 进行中
- 本 writer 已停止代码工作；待 hub 按原批次真实 completed 门禁收割并监督。无后台构建/测试任务。

## 未知
- 两文件当前字节的中央编译结果、binding-loss terminal/shutdown 实机双 action identity proof、cleanup/quit late fault 分类与同批监督结果尚未验证。
- 退出请求后发现 fault 只能使 final 证据失败，不能追改已提交的退出码；quitExitCodeRequested 与 evidence exitCode 分开记录。
- 本会话实际模型/provider route 元数据仍未由工具提供，不将请求指定路由冒充实测证据。
