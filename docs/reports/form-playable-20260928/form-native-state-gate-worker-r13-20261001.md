# form-native-state-gate-worker-r13

## 已确认

- 2026-10-01: 已读取请求文件 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\worker-form-state-gate-r13-request.txt`, 契约 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md`, 当前实现文件和既有审查 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-flow-review-r12-20261002.md`. 本轮路由固定为用户指定的 `6.1sol via agentrouter`; 不委派其它代理.
- 2026-10-01: 既有审查已指出真实 action 成功路径 `FormNativeSmokeRunner.cs:476-482` 只依据 action evidence 写 `status=passed`, `Snapshot` 的 `:753-780` 仅记录 `formModeSelected`, native stance 和类型字符串, 没有把形态状态纳入不可逆成功 gate. 这是本轮最小修复目标.
- 2026-10-01: 当前实现的真实形态创建语义已核对: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:100-113` 的 `CreateEffects()` 精确映射 Calm -> `VoidFormEffectPower` + `SerpentFormPower`, Wrath -> `DemonFormPower` + `ReaperFormEffectPower`, Divinity -> `EchoFormEffectPower` + `CelestialFormPower`; `:82-91` 说明两个 effect 都必须成功应用, 否则回滚. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:20-21,38-45` 提供真实 `IsSelected` 和 `FormStanceKind` 语义.
- 2026-10-01: 当前真实 carrier 类型已核对: `VoidSerpentStancePower.cs:4-7`, `DemonReaperStancePower.cs`, `EchoCelestialStancePower.cs` 分别对应 Calm, Wrath, Divinity. 当前 runner 的 `Snapshot` 以 `WatcherFormStancePower` 和名称包含 `Form` 的字符串收集证据, 该证据过宽, 不能作为成功 gate.

## 进行中

- 将在不改变真实 run, room, card, action API 链路的前提下, 为三个场景增加精确预期映射和主线程内形态证据 gate. 形态 gate 失败将锁存场景失败, 并保留 before/after 证据.

## 未知

- 尚未构建、测试、部署或启动游戏. 目标发行版中真实 Watcher 卡片完成后 native stance、form carrier 和两个 effect 的最终时序尚未实机确认.
- 尚未证明 `CurrentKind(player)` 在卡片 action 完成后一定返回场景预期值, 以及目标二进制中所有当前代码类型引用的兼容性.

## 实现完成

- 2026-10-01: 已完成最小实现, 只修改 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`; `FormNativeSmokePatch.cs` 保持不变. 普通启动 gate 和真实 run, room, card, action 链路未改.
- 2026-10-01: `FormNativeSmokeRunner.cs:773-800` 增加场景精确映射: calm -> `FormStanceKind.Calm` + `VoidSerpentStancePower` + `VoidFormEffectPower` + `SerpentFormPower`; wrath -> `FormStanceKind.Wrath` + `DemonReaperStancePower` + `DemonFormPower` + `ReaperFormEffectPower`; divinity -> `FormStanceKind.Divinity` + `EchoCelestialStancePower` + `EchoFormEffectPower` + `CelestialFormPower`. 映射依据 `WatcherFormStancePower.CreateEffects()` 的 `:100-113`, 没有直接构造或应用形态 Power.
- 2026-10-01: `FormNativeSmokeRunner.cs:802-872` 增加精确 form state evidence gate. Gate 同时要求 `formModeSelected == true`, `nativeWatcherStance` 等于预期 `FormStanceKind`, 精确 carrier 类型存在, 以及两个 `CreateEffects()` 预期 effect 类型各自存在; 通过完整类型名和精确字符串相等比较, 不使用宽泛的 `Form` 字符串命中作为成功条件. evidence 记录实际类型、预期类型、计数、逐项布尔值和 failure.
- 2026-10-01: `FormNativeSmokeRunner.cs:378-382` 记录出牌前 `before` 和 `formGateBefore`; `:481-501` 在真实 action 成功后记录 `after` 和 `formGateAfter`, 只有 action evidence 成功且 form gate 通过才写 `status=passed`. form gate 失败时设置 `formFailureLatched` 和 `formGateFailureLatched`, 写入可诊断 failure, 保持场景 failed, 不允许 action 成功掩盖形态未生效.
- 2026-10-01: `FormNativeSmokeRunner.cs:276-288,590-592` 记录不可逆形态失败锁存状态. `FormStanceMode.IsSelected(player)` 和 `FormStanceWatcherBridge.CurrentKind(player)` 仍在 `Snapshot` 的主线程调用 `:873-900` 内读取; 未新增未知 API, 未改变真实 Watcher 卡 action 链.

## 未知补充

- 2026-10-01: 仅完成静态回读, 未构建、未测试、未部署、未启动游戏. 尚未验证三个真实 Watcher 卡在目标发行版中完成 action 后是否按预期留下 carrier 和两个 effect, 也未验证 native stance 时序、真实 JSON 序列化结果和 gate failure 的实机路径.
- 2026-10-01: 当前 gate 对 effect 的不变量是 `WatcherFormStancePower.CreateEffects()` 规定的两个具体类型同时存在; 未宣称其它时序或效果数值已通过实机验证. 未修改共享 `mod_configs`, Steam 安装、构建产物或 C:.
