# r26 worker 报告 (唯一可写)

模型: global:deepseek-v4.1-flash / 路由 wb2api / xhigh. 只改 RuntimeSafetySmokeRunner.cs 与 RUNTIME-SAFETY.md.

## 已确认

### 1. 两处 file-scoped namespace (P0, 阻塞编译)
- 绝对路径: `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs`
- 行号: 26 `namespace FormsNativeSmoke.Patches;`, 38 `namespace FormsNativeSmoke.Run;`
- 触发条件: 任意构建该文件时. C# 单文件只允许一个 file-scoped namespace 且必须为首个声明;
  此处同文件出现两个 `namespace X;`, 且第 28 行 `[HarmonyPatch]` 属性位于第一个 file-scoped namespace 之后.
- 当前控制流: 第 26 行进入 Patches file-scoped namespace; 第 28-36 行定义 RuntimeSafetySmokePatch;
  第 38 行再次声明 `namespace FormsNativeSmoke.Run;` -> 语法非法.
- 最小修复范围: 把两处改为 block namespace: `namespace FormsNativeSmoke.Patches { ... }` 与
  `namespace FormsNativeSmoke.Run { ... }`, 保持 `RuntimeSafetySmokePatch` 全名
  `FormsNativeSmoke.Patches.RuntimeSafetySmokePatch` 与 partial `FormNativeSmokeRunner` 归属
  `FormsNativeSmoke.Run.FormNativeSmokeRunner` 不变.
- 尚缺实机证据: 无(纯语法事实, 编译前即可确认).

## 进行中
- 无. 本轮静态实现已完成, 等待主 hub 集中构建与实机验收.

## 未知
- 未构建/未 lint/未测试/未运行/未部署/未启动游戏 (按 r26 限制).
- 真实编译输出未知; 目标二进制对新增 patch / Cubex encounter / 各命令签名的兼容性未知.
- 两场景真实 JSON / 退出码 / 主线程记录 / 原始 faults / 全量 no-mutation / 最终 settled drain 均未取得.
- 未声明 UI / 读档 / 长战斗 / 多人 / 性能 / 热卸载通过.

## 交付物 (CODE_COMPLETE 候选, 未验收)

### 1. `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs`
- bytes 82442, lines 1360, SHA256 `F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919`.
- 只改本文件; 未改 r25 原有测试源 / README / 工程 / 生产 / 控制器.

### 2. `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md`
- bytes 5952, lines 90, SHA256 `891FE834C0ECC6D4C5A0FA746531153BC05D74472C165E914973490B649B08FC`.
- 只改本文件.

### r26 契约逐条落点 (静态证据, 行号以最终文件为准)
- 契约1 两 file-scoped namespace -> block: 第 31 行 `namespace FormsNativeSmoke.Patches`, 第 44 行 `namespace FormsNativeSmoke.Run`;
  `RuntimeSafetySmokePatch` 与 partial `FormNativeSmokeRunner` 全名/归属不变.
- 契约2 生产身份: `CaptureFormsAssemblyIdentityAsync` 读 AppDomain 已载入 Forms 的 `Location` + 文件 SHA256,
  要求 Location 为可读 G: 文件; guard 前后 `RuntimeSafetyGuardEvidence` 记录 owner/prefixCount/prefix 类型与方法名/
  Remove 身份, `RuntimeSafetyOwnerCounts` 记录三 owner 计数; `guardPassed` 要求前后 `prefixCount==1`、
  prefix 为 `Forms.FormsCode.FormStanceSafetyGuard.RemovePrefix`、旧 owner 归零、Remove 身份相同, 并参与 `passed`.
- 契约3 主线程: `RuntimeSafetyBindOnMainThreadAsync` 用 `InvokeOnMainThreadWithTimeoutAsync` 包裹 TryBind;
  所有真实对象读取/命令提交/Shutdown 均走同一 gate; 不在 await 后直接 TryBind; 不调用生产 prefix 冒充 dispatch.
- 契约4 三命令: Remove 用当前 `WatcherMod.Wrath` marker; Damage 用玩家 + 真实 enemy dealer;
  ModifyAmount 用 owned `StrengthPower`; `CaptureRuntimeCommandAsync` 区分 settled / timeout / faulted /
  synchronous submission failure, 未 settled 立即 throw 终止后续 probe; 每条要求 `Forms unavailable` + `restart`;
  `RuntimeSafetyFinalizeProbe` 对完整 HP/block/energy/full-name amounts/raw marker before-after 严格比对.
- 契约5 控制局: 显式验证 `RunState.Modifiers` 无 `FormStanceModifier` 且 carrier/effect 为空;
  Strike/Tranquility 的真实 `CardModel` 在 Shutdown 前创建并入手, 失效后只 enqueue 已备 `PlayCardAction`;
  history 按同一 CardModel reference 统计 Started/Finished 增量; expected 按引擎顺序 (先加算 Strength, 后乘算 Wrath 2x)
  从真实 `DynamicVars.Damage.BaseValue` 计算, 其它 owner power 出现即 fail closed.
- 契约6 drain/fault: `RuntimeSafetyDrainAndCleanupAsync` 清理新操作二次 drain; fault 订阅保留到 post-quit 最终排空;
  final fault 门按场景结束点收窄; `RunRuntimeSafetyAndQuitAsync` 统一 fail closed 回写 passed/status/exitCode;
  报告写入返回结果, 缺环境/非法路径/写失败改 passed=false + exitCode=1, post-quit 写失败也记录 failed.
- 契约7 开关: 未请求时 :TryStartRuntimeSafety 直接 return 零动作; 两开关各独立进程, 与既有 smoke 开关重叠写 invalid 并失败;
  shouldSave=false / 固定 seed / 真实 Cubex 保留; 只写 `SPIRE1_FORM_SMOKE_REPORT` 下两报告.

## 证据边界 (严格)
- 以上全部为当前源码静态核对 (使用当前 engine-dllsrc 与 `mod/FormsCode` 权威源), 非实机复现.
- 未运行任何构建 / 测试 / lint / git / 部署 / 游戏进程; 未使用 peer / 委派 / 其它 harness.
- 模型与路由: 请求文本为 `global:deepseek-v4.1-flash` / `wb2api` / xhigh; 本会话未取得独立会话元数据,
  不把请求文字冒充已解析路由证据.
