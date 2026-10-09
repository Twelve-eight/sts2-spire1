# worker.md - runtime-controller-r28 实现者报告

状态: CODE_COMPLETE (静态实现完成; 本轮禁止 build/lint/run, 无实机证据).

## 已确认 (有证据)

### 产物与哈希
- 新增文件: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r28.ps1`, Length 59490, SHA256 `E476A110A08D127E0D467CBE056818D30A273ED41ABFF6118A6DAE42339E9EF6` (Get-FileHash).
- 冻结基线 r24: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1`, SHA256 `E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1` (与请求一致, r24 原件未改).
- 权威源: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs`, SHA256 `F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919` (与请求一致, 未改).
- 语法检查(非构建): `[Parser]::ParseFile` parse-errors = 0.

### 最小差异 (与 r24 对比, 旧主体仅 4 行改动)
`Compare-Object r24 r28` 中来自 r24 的改动行仅 4 条, 全部为窄扩展:
1. 第 1 行: ValidateSet 追加 `runtime-safety`,`runtime-unselected`.
2. 第 32 行: `$testArg` switch 追加 `--forms-runtime-safety` / `--forms-runtime-unselected`.
3. 第 368 行: `Test-Evidence` 增参 `[string]$FormsPayload`.
4. 第 448 行: 调用点 `Test-Evidence $dir $SmokeMode $FormsPayload`.
其余旧函数 (Test-SaveGuardEvidence/Test-BindingLossEvidence/Test-NoNativeMutation/Test-SafetyGuardEvidence 等) 逐字节未变; 旧 5 模式行为与隔离/共享配置/无窗口/日志排空/保留 mods/cleanup 主体未动 (新逻辑只在 `Test-Evidence` 的 runtime else 分支与新增函数中).

### 新模式映射 (请求 1)
- 第 39-40 行新增 cases: `r1-runtime-safety` 与 `r2-runtime-unselected`, 均 `Mods=@('BaseLib','Watcher','Forms',$testMod)`, `Forms=$true;Spire1=$false;Watcher=$true` (各 1 场景, 不挂 Spire1).
- headless 全机制保留: 第 425 行参数列表未改 (含 `--headless`,`--audio-driver Dummy`,`--disable-crash-handler` 等).

### 新解析函数 (请求 5, 局部新增, 不引入全局自动变量/不吞未知字段)
- `Test-RuntimeNumeric` (368): 严格 JSON 数字.
- `Test-RuntimeGuard` (372): owner/removeMethodFound/prefixIsExactGuard/prefixCount=1/prefixOwner/prefixPatchType=`Forms.FormsCode.FormStanceSafetyGuard`/prefixMethodName=`RemovePrefix`/removeMethodIdentity 非空/failure 必须缺省.
- `Test-RuntimeGuardSameIdentity` (387): 前后 owner/type/method/removeMethodIdentity 严格 Ordinal 相等且 prefixCount 恒 1.
- `Test-RuntimeOwnerCounts` (391): 三 owner key 全为整数非负.
- `Test-RuntimeFaults` (402): raw/text/expected/unexpected 四数组 typed; gate=true; unexpected 必须空; final(raw) 必须空; raw/expected 长度一致且逐 sequence 严格相同 (任意未关联 fault 失败).
- `Test-RuntimeSnapshotUnchanged` (433): ownerPowerFullNameAmounts 非空且 before==after; ownerPowerAmounts/markerFullNames/markerAmounts/energy/player HP/MaxHp/Block/IsDead/PowerTypes/stance 全部严格不变 (无值不当 0=0).
- `Test-RuntimeCommandProbe` (459): command 非空; passed/stateUnchanged=true; afterPending=false; markerPreserved 按需; evidence.submitted (或 synchronousSubmissionFailure) + settled=true + timedOut/cancelled=false + expectedFormsUnavailableRestart=true + faulted|sync; exceptionType 非空; exception/submitException 含 `Forms unavailable` 与 `restart`.
- `Test-RuntimeCardRun` (483): passed/card 严格; actualCardHistory.observedByCardReference=true; cardInstanceIdentity 非空; Started/Finished 计数非负且 after 严格 > before (同一 CardModel reference 有真实增量); before/after 均 formModeSelected=false.
- `Test-RuntimeTargetDamage` (507): target.CurrentHp/Block 整数, 计算 hpDrop+blockDrop.
- `Test-RuntimeSafetyEvidence` (518): 见下.
- 分流: `Test-Evidence` 第 371 行 `runtime-safety|runtime-unselected` -> `Test-RuntimeSafetyEvidence`; 旧分支未改.

### 新报告 typed 验证 (请求 3, 4)
- 根: testOnly=true; scenario 精确 `safety`/`unselected`; status=`completed`; passed=true; exitCode 整数 0; finalEvidencePhase=`post-quit`; quitDrainSettled=true; quitDrainOutcome=`settled`; cleanup=`completed`; cleanupFailure 必须缺省; cleanupDrainSettled 若存在必须 true.
- productionIdentity (539-571 源): loaded=true, passed=true; location 非空且归一化后位于隔离根 `G:\omp works\.tmp\forms-independent-20261005\` 之下 (实际隔离客户端加载路径); sha256 严格等于本轮 `$FormsPayload\Forms.dll` 实测 hash (非固定 r18 hash).
- guard: guardBefore 与 guardAfterShutdown 均须精确 guard 契约且身份稳定; 两 ownerCounts 三 key 精确 (Forms=0, Bridge=0, Safety=1); bridgeBoundAfterShutdown=false; unavailableReason 非空.
- faults: scenario 与 final 两套严格 typed; final raw 必须空 (late fault 失败); 不做顶层 bool 直判.
- safety 模式: 恰好 3 个真实 command probe (removeProbe marker 必须保留; damageProbe; powerAmountProbe), 三 command label 唯一; guardPassed=true.
- unselected 模式: selectedBefore=false; entryCarrierTypes/entryEffectTypes 必须为非空 typed 数组且长度 0; entryRawStance 为 Wrath/determinate/无 conflict; tranquilityStanceChangedToCalm=true; strikeExpectedDamage 数值非负; strikeExpectedEvidence 的 baseDamage/additive/multiplier 为数字, contributors/unexpectedPowerTypes 为数组且 unexpected 必须空 (未建模 owner power fail closed); strikeProbe.label=`runtime-unselected-native-strike` card=`WATCHER_STRIKE_P`; tranquilityProbe.label=`runtime-unselected-native-tranquility` card=`WatcherMod.WatcherTranquility`; 两张卡 Started/Finished 均有真实增量; 原生 Strike 实伤 == 期望值; 且 energy after < before (原生支付); Tranquility 前后 raw stance Wrath -> Calm.

### 字段权威行号 (RuntimeSafetySmokeRunner.cs)
- 开关/冲突: 48-49, 71-98. 报告名: 1209-1215. 根 schema: 1179-1187 (scenario: 116/230/384).
- productionIdentity: 539-571 (location G: 556-559; sha256 562). guard: 1097-1146. ownerCounts: 1149-1176.
- command evidence: 669-757, 1229-1265. probe: 769-780, 803-851. drain/cleanup: 584-656. final: 145-223 (post-quit 182).
- unselected 断言: 436-444, 453-465, 481-506, 491-497. card run JSON: 1335-1356. history: BindingLossSmokeRunner.cs 83-104, 1298-1305.
- fault gate: FormNativeSmokeRunner.cs 1277-1317, 1796-1809.

## 进行中 (半成品, 需复核)

- 无未完成代码面; 已按 8 分钟窗口收束.
- 需复核点 (静态, 非实机): (a) `Test-RuntimeCommandProbe` 允许同步提交失败 (submitted 或 synchronousSubmissionFailure 二者其一), 依据源 682-690 行 `CaptureRuntimeCommandAsync` 的 catch 分支; 若实机该命令走 faulted task 路径亦满足. (b) safety 模式未对 removeProbe/damageProbe/powerAmountProbe 追加除上述外的额外语义检查; 源 320-357 行的 `requireMarkerPreserved` 已映射. (c) 未校验 `encounter`/`bridgeBoundBefore` 等附加字段, 因源未纳入 passed 判据.

## 未知 (未覆盖)

- 无实机证据: 本轮按请求禁止 build/lint/test/run/游戏, 所有新 schema 均为静态权威源推导, 未经实机 JSON 复现; 两个新模式的真实退出码/JSON/fault/drain 未验证.
- r27 双 action parser 故障分类为独立窄修, 本文件未复写 (请求 2).
- 未验证: Forms.dll 加载 Location 实机路径、Cubex encounter 兼容性、各命令签名、raw fault 关联、最终 settled drain 是否在真实运行中成立.
- 未做: build/lint/test/git/deploy/游戏/委派/peer/其它 harness 或模型.

## 全部路径

- `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r28.ps1` (新增, SHA256 E476A110A08D127E0D467CBE056818D30A273ED41ABFF6118A6DAE42339E9EF6)
- `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-controller-r28\worker.md` (本报告)
- 只读引用: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1`, `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs`, `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md`, `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs`, `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`
