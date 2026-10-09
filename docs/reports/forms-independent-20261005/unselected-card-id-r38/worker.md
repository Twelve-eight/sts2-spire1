# RuntimeSafetySmokeRunner 未选形态卡牌 ID 修复报告

## 已确认

- 修改前 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs` SHA256: `C638B984846E91B269018332CCFEB60A4F69C309C3BAA48DDA5CE0D6468A9F92`。
- 现状缺陷 (修改前): 第 1044-1069 行 `RuntimeSafetyPrepareCardAsync` 先在 1056 行 `CombatState.CreateCard` 得到 mutable card, 随后在 1063 行直接构造 `new PlayCardAction(card, effectiveTarget)`; 未先把 card 加入 Hand, 也未等待 `CardPileCmd.Add` 完成或验证 `card.Pile?.Type == PileType.Hand`。第 537-540 行虽调用 `RuntimeSafetyAddPreparedCardToHandAsync`, 但 action 已在 prepare 内构造完毕。
- 权威引擎证据: `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs` 第 172559-172583 行 `NetCombatCardDb.GetCardId` 只查 `_cardToId`, 未注册即抛 `Card ... could not be found in combat ID database!`; 第 172591-172597 行 `OnPileContentsChanged` 是 pile change 时注册卡牌的路径。因此构造 action 前必须让卡牌真实进入已订阅的 pile。
- 已有成功载体: `FormNativeSmokeRunner.cs` 第 661-688 行与 `BindingLossSmokeRunner.cs` 第 751-789 行均采用 `CreateCard` -> `CardPileCmd.Add(card, PileType.Hand, skipVisuals: true)` -> 等待完成 -> 验证 Hand -> `new PlayCardAction(...)` 顺序。
- 修复方案: 在 `RuntimeSafetyPrepareCardAsync` 内按 main-thread 顺序 `CreateCard` -> 取 `CardPileCmd.Add(..., skipVisuals: true)` 的 Task -> `AwaitOperationWithTimeoutAsync` 等待 -> main-thread 验证 `card.Pile?.Type == PileType.Hand` -> 之后才 main-thread 构造 `PlayCardAction` 并计算 baseDamage。删除调用点两处 `RuntimeSafetyAddPreparedCardToHandAsync` 与该方法本身, 保留 `RuntimeSafetyPreparedCard` record 字段形状。
- 修改后 SHA256: `9522D2E43D694FAA9AD5157EF83AA878C407C8AFEBE2BC51662FAC04004F5F79`; 文件长度 97976 -> 98069 bytes; 行数 1580 -> 1584; LF 1580 -> 1584; CRLF 7 -> 7; 首字节 `75 73 69` (using), 无 BOM。
- 精确 delta:
  - 第 538 行删除: `await RuntimeSafetyAddPreparedCardToHandAsync(strikePrepared, operations);`
  - 第 540 行删除: `await RuntimeSafetyAddPreparedCardToHandAsync(tranquilityPrepared, operations);`
  - 旧第 1044-1071 行 `RuntimeSafetyPrepareCardAsync` 主体替换为新的异步顺序实现, 新行号 1042-1097。
  - 旧第 1073-1093 行 `RuntimeSafetyAddPreparedCardToHandAsync` 已删除, 其职责并入新的 `RuntimeSafetyPrepareCardAsync`。
  - 新方法末尾与下一个方法之间补回空行, 最终行数 1584。
- 新 `RuntimeSafetyPrepareCardAsync` 顺序: main-thread `CreateCard` (1052-1062) -> main-thread 调用 `CardPileCmd.Add(card, PileType.Hand, skipVisuals: true)` 得到 Task (1063-1066) -> `AwaitOperationWithTimeoutAsync` 等待完成 (1067-1072) -> main-thread 验证 `card.Pile?.Type == PileType.Hand` (1073-1078) -> main-thread 构造 `new PlayCardAction(card, effectiveTarget)` 并计算 baseDamage (1079-1096)。
- 第 537-538 行现在两次 prepare 各自完成 Create/Add/等待/验证/action 构造后才返回; 第 539 行 `RuntimeSafetyExpectedStrikeDamageAsync`、Shutdown 与 Shutdown 后 enqueue 路径未改。
- 契约保持: `RuntimeSafetyPreparedCard` record 字段未变 (CardEntry, Card, Action, Target, BaseDamage); `RuntimeSafetyPlayPreparedCardAsync` 未改; Shutdown 前两张真实 CardModel 和两份真实 action 均已准备, Shutdown 后仍只 enqueue 既有 action, 不新建 action。
- 静态自检: 全文件仅剩 1 处 `new PlayCardAction`, 位于第 1087 行, 在该方法内位于 `CardPileCmd.Add` 等待 (1067-1072) 与 Hand 验证 (1073-1078) 之后; 已无 `RuntimeSafetyAddPreparedCardToHandAsync` 残留调用或定义。
- 边界遵守: 未伪造卡牌 ID, 未反射写 `NetCombatCardDb`, 未改生产 Forms, 未改 controller, 未改其它文件。

## 进行中

- 无。

## 未知

- 未构建, 未 lint, 未运行测试, 未运行游戏, 未部署, 未写共享配置。按请求这些验证全部跳过; 上述结论均为静态源码检查。
- 需要生产代码或控制器改动的阻塞项: 未发现。