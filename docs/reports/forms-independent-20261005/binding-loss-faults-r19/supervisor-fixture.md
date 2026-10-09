# binding-loss-faults-r19 监督报告 (第一阶段: FIXTURE_ONLY)

范围: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke`
监督请求: `supervisor.request.md`
worker: `01a109cb-2810-7463-b8c0-eccd6b48ff4f`
模型/路由: `global:deepseek-v4.1-flash` / `wb2api` (用户本轮唯一指定)

## 门禁证据 (纠偏)

- 权威门禁 (hub 原生): `fixture-gate-notice.json`, `Tool=multi_agent_v1.wait_agent`, worker `01a109cb-2810-7463-b8c0-eccd6b48ff4f`, `Status=completed`, `TimedOut=false`, `Phase=FIXTURE_ONLY`, `ObservedAt=2026-10-05T10:08:02.6121414+08:00`, sha256 `C266FD6B503EFA5B4F8D7A085DB23F47BA4C290D052BB0C4FE000530B8999696`.
- 主会话本轮的 `wait_threads` 返回仅为**状态观察**, 不作为 hub native wait 门禁证据.
- 门禁成立, 进入第一阶段窄审.

## 已确认

### S1 hash 与写集核对 (通过)
- `BindingLossSmokeRunner.cs`: 51275 bytes, sha256 `DF1C081877F7675BCEBDB1E7BF22D2AEFE791EB376AA2026990E4FDAA6446063` — 与门禁通知一致.
- `README.md`: 6304 bytes, sha256 `FCE44D0324092F5B822523B453B4F40A8E033ED9974246B2DC39B0A3616F3083` — 与门禁通知一致.
- `git status --porcelain` 仅 2 个文件 M: `BindingLossSmokeRunner.cs`, `README.md`.
- `FormNativeSmokeRunner.cs` (sha256 `D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC`, 08:27) 与 `LifecycleSmokeRunner.cs` (sha256 `0DAB1681DAAEABBDFBA967EC85A9F77851FE16356885DBAD7F434311ACC1620B`, 07:54) 未改, 与 FIXTURE_ONLY 范围一致.
- 两文件首字节非 BOM (0x75 0x73 0x69 0x6E / 0x23 0x20 0x46 0x6F).

### S2 合法 target 选择 (通过)
- 新增 `BindingLossEffectiveTarget` (`BindingLossSmokeRunner.cs:764-773`): `TargetType.AnyEnemy` 才传敌方 target (为空时回退 `HittableEnemies.FirstOrDefault()`); 其它 TargetType 一律 null.
- 调用点 `BindingLossSmokeRunner.cs:666-671`: 先解析 actionTarget, 再 `new PlayCardAction(card, actionTarget)`.
- 权威契约核对一致:
  - `WatcherCrescendo.cs:24-25` 与 `WatcherTranquility.cs:24-25` 构造 `TargetType)0` = None.
  - `WatcherStrike_P.cs:20` 构造 `TargetType)2` = AnyEnemy.
  - `CardModel.cs:1762-1785` IsValidTarget(null) 对非 AnyEnemy/AnyAlly 返回 true, 传敌方 target 对 None 返回 false.
  - `PlayCardAction.cs:85-88` CanPlay/IsValidTarget 不通过即 Cancel+return; `GameAction.cs:141-160` finally 仍置 State=Finished 并 TrySetResult CompletionTask.
- 结论: 旧 r20 "completed 但 rawStance=None" 假完成根因成立, 修复方向正确.
- 覆盖边界 (非缺陷, 明确记录): 本 fixture 只实际使用 None (Crescendo/Tranquility) 与 AnyEnemy (Strike) 两型; AnyAlly 传 null 会被 IsValidTarget 判 false, 但本 fixture 不涉及 AnyAlly 牌.

### S3 入场诊断与故障路径保护 (通过)
- `BindingLossTargetDiagnostics` (`BindingLossSmokeRunner.cs:779-811`) 记录 targetType / requestedTargetPresent / actionTargetPresent / isValidTarget; `includeCanPlay=true` 时另记 canPlay / unplayableReason / preventer / canPlayTargeting, 否则只写 canPlayDeferred.
- 入场调用带 `recordPreflightDiagnostics: true` (`BindingLossSmokeRunner.cs:243`); 桥失效后的 Strike 与 Tranquility 调用不带该参数 (默认 false, 行 376-384 / 398-407).
- 权威契约: `CardModel.cs:1725-1755` CanPlay(out reason, out preventer) 内部调 `Hook.ShouldPlay` (行 1746). 桥失效后若预调用 CanPlay 会先消费生产 fail-closed guard 的抛错, 破坏后续 action 故障关联; 只写 canPlayDeferred 是正确保护.
- 入场结果新增 `entryBefore` / `entryAfter` (`BindingLossSmokeRunner.cs:245-246`) 与 action 级 targetDiagnostics, 诊断面足够定位目标错误.

### S4 手牌离开判据 (通过)
- `BindingLossSmokeRunner.cs:715-721`: 动作后读 `card.Pile?.Type.ToString() ?? "none"` 记 cardPileAfter, `cardLeftHand = !Equals(cardPileAfter, "Hand")`.
- `passed` 新增 `&& cardLeftHand` (`BindingLossSmokeRunner.cs:724-728`); 入场 gate 以 `enterRun.Passed` 为前置 (`BindingLossSmokeRunner.cs:247-251`), 故 "Cancel 假完成" 不再能通过入场.
- 权威契约: `PlayCardAction.CancelAction` (`PlayCardAction.cs:110-128`) 不移动牌堆; 真实出牌经 `CardModel.cs:1866` `AddDuringManualCardPlay` 离开 Hand (`CardPileCmd.cs:827-850`). 故 Cancel 牌留 Hand 判失败, 真出牌才可能通过.
- 残留边界 (非阻塞, 建议第二阶段顺手加固): 若 card.Pile 变为 null 则 cardPileAfter="none" 亦算 cardLeftHand=true; 但入场另有 raw Wrath + carrier + effects 三重 gate, 桥失效后 strikeExecuted 另需 CardPlay history FinishedCount 增长, 因此该边界不构成当前假阳性路径.

### S5 README 文案 (通过)
- `README.md:67-70` 正确说明按真实 TargetType 决定 target, None 必须传 null, 传敌方 target 会 IsValidTarget false → Cancel 后仍可能 Finished 的假阳性.
- `README.md:75-78` 正确列出入场诊断字段与桥失效后 canPlayDeferred 的原因.
- 未声称热卸载/完整读档/可见 UI/长战斗已通过; 未降低普通三姿态与 turns 探针门禁.

## 进行中

- 无. 第一阶段窄审已收敛.

## 未知 (不属本阶段结论)

- 编译与实机复测由 hub 中央做, 本阶段未执行, 故不得声称实机通过.
- typed fault 对象级关联与 Lifecycle Harmony 元数据扩展未实现 (按两阶段调整, 属第二阶段).
- r5 负例在修复后是否真正进入失效阶段, 需 hub 中央复测确认.

## 第一阶段结论

FIXTURE_SUPERVISION_PASS

- 依据: 两文件 hash 与门禁通知逐字一致; 写集仅 2 文件且另 2 文件未改; 合法 target 选择与权威源码一致; 入场诊断仅在绑定期调用 CanPlay, 失效期不预消费故障; 手牌离开判据能区分 Cancel 假完成与真实出牌; README 文案与实际行为一致.
- 该 PASS 仅覆盖 FIXTURE_ONLY 阶段, 不代表 typed fault / Lifecycle / 整体任务通过.
- 第二阶段须等 hub `send_input` 携带新的 native wait 证据后再审, 不自主轮询 peer.
