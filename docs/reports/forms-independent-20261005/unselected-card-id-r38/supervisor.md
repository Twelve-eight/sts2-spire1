# RuntimeSafetySmokeRunner 未选形态卡牌 ID 修复 - 监督审核 (r38)

审核对象: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs`
门禁: `gate-notice.json` (CheckedAt 2026-10-05T14:45:29+08:00, WorkerResult=CODE_COMPLETE, NotBuilt/NotTested/NotRun=true)
gate-notice SHA256: `9522D2E43D694FAA9AD5157EF83AA878C407C8AFEBE2BC51662FAC04004F5F79`

## 已确认

- 现行文件 SHA256 = `9522D2E43D694FAA9AD5157EF83AA878C407C8AFEBE2BC51662FAC04004F5F79`, 与 gate-notice 一致; 长度 98069 bytes, 1584 行, 首字节 `75 73 69`(using), 无 BOM。
- 权威引擎证据核对通过 (`G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs`):
  - 172559-172566 `NetCombatCardDb.GetCardId` 在 `TryGetCardId` 失败时抛 `Card ... could not be found in combat ID database!`。
  - 172591-172597 `OnPileContentsChanged` 对 pile 内每张卡调用 `IdCardIfNecessary`, 即 pile change 是注册路径。
  - 172519-172543 `StartCombat` 对每个 pile 调 `OnPileContentsChanged` 并订阅 `ContentsChanged`。
  - 177382-177395 `NetCombatCard.FromModel` 调用 `NetCombatCardDb.Instance.GetCardId(card)`; 169859-169869 `PlayCardAction(CardModel, Creature?)` 构造函数调用 `NetCombatCard.FromModel(cardModel)`。故 action 构造前卡牌必须已在被订阅的 pile 中。
- 修复顺序核对通过 (新行号 1042-1097):
  - 1052-1062 main-thread `CombatState.CreateCard` 得到 mutable card。
  - 1063-1066 main-thread 调 `CardPileCmd.Add(card, PileType.Hand, skipVisuals: true)` 取回 Task。
  - 1067-1072 `AwaitOperationWithTimeoutAsync(addCard, RuntimeSafetyProbeTimeoutSeconds, terminalOnFailure: true)` 等待 Add 完成。
  - 1073-1078 main-thread 验证 `card.Pile?.Type == PileType.Hand`, 不满足即抛。
  - 1079-1096 验证通过后才在 main-thread 构造 `new PlayCardAction(card, effectiveTarget)` (1087) 并计算 baseDamage。
- 与既有成功载体形状一致: `FormNativeSmokeRunner.cs` 661-688、`BindingLossSmokeRunner.cs` 751-789 均为 CreateCard -> CardPileCmd.Add -> 等待 -> 验证 Hand -> new PlayCardAction; `CardPileCmd.Add(CardModel, PileType, ..., bool skipVisuals)` 签名见 sts2.decompiled.cs 194137。同批 r37 实机中 entry 卡 (`PlayBindingLossCardAsync`, 514) 走此顺序并通过。
- Shutdown 契约核对通过: 537-538 两处 `RuntimeSafetyPrepareCardAsync` 返回前各自完成 Create -> Add 等待 -> Hand 验证 -> action 构造, 返回时两张真实 CardModel 均已入 Hand、两份真实 `PlayCardAction` 均已构造; 553 行才执行 production Shutdown; 589/594 行 Shutdown 后仅 `RequestEnqueue(prepared.Action)` (1128-1140) 与等待 `prepared.Action.CompletionTask`, 未在 Shutdown 后新建 action 或 card。
- 未重现 r37 wrapper 覆盖问题: 390/403/415 行仍为 `result[...] = RuntimeSafetyProbeJson(...)` 字典写入, 396/409/421 行 `RuntimeSafetyFinalizeProbe` 原地改写同一字典; 全文件无 `RuntimeSafetyProbeEvidence.ToJson()` 覆盖这三个 key。
- 证据形状保持: `RuntimeSafetyPreparedCard` record 字段未变 (1553-1558); `RuntimeSafetyPreparedCardRun.ToJson` (1570-1580) 仍输出 label/card/passed/failure/before/after/action/actualCardHistory; `RuntimeSafetyPlayPreparedCardAsync` 未改; `RuntimeSafetyExpectedStrikeDamageAsync` 调用点 (539) 未改。
- 未伪造卡牌 ID / 未越界: 全文件无 `IdCardForTesting`/`ClearCardsForTesting`/`_cardToId`/直接反射写 `NetCombatCardDb`; 旧 `RuntimeSafetyAddPreparedCardToHandAsync` 无残留调用或定义; 唯一 `new PlayCardAction` 在 1087, 位于 Add 等待与 Hand 验证之后。14:40-14:50 窗口内仅本文件被写入, 未改生产 Forms / controller / 其它文件。
- 异常路径: Add 超时/fault 经 `terminalOnFailure: true` 抛 `TerminalOperationException` (FormNativeSmokeRunner.cs 3650-3691); Hand 验证失败抛 `InvalidOperationException` (1077-1078); 二者均在 action 构造之前。
- 代码风格: 新方法与相邻方法间空行保留 (1097-1099); 大括号平衡 (148/148); 缩进与文件一致。
- worker 报告"修改前" SHA256 `C638B984846E91B269018332CCFEB60A4F69C309C3BAA48DDA5CE0D6468A9F92` 由 r37 `gate-notice.json` 独立佐证一致。

## 进行中

- 无。

## 未知

- 构建/实机/运行: 本任务不构建、不测试、不运行, 未验证; 上述为静态核对, 未经编译器与实机确认。
- 本文件为未跟踪新文件, 无 git 基线, 无法做逐行 git diff; delta 由行号+内容核对确认。
- 未选局范围之外 (selected 路径) 未纳入本次最小 delta 审核范围。

SUPERVISION_PASS
