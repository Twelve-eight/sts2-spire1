# FormsNativeSmoke runtime safety probes incremental report

## 已确认

- 范围: 仅新增 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs 与 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md, 并增量写本报告.
- 约束: 不构建, 不 lint, 不测试, 不运行, 不部署, 不 git, 不再委派, 不用其它 harness/模型/fallback.
- 入口: MainFile.InstallOwnPatches() 扫描本测试程序集全部 [HarmonyPatch] 类型; 现有 FormNativeSmokePatch 已调用 FormNativeSmokeRunner.TryStart 与 LifecycleSmokeRunner.TryStart. 新增同程序集 patch 类可挂到 NGame._Ready, 但必须自带 opt-in 判断, 未传参时无动作.
- 权威引擎签名: PowerCmd.Remove(PowerModel?) 在 RemoveInternal() 之前执行 Harmony prefix; PowerCmd.ModifyAmount 在 Hook.BeforePowerAmountChanged 之后才 power.SetAmount(...); CreatureCmd.Damage(...) 在 DamageBlockInternal/LoseHpInternal 之前执行 Hook.BeforeDamageReceived. 证据: research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/PowerCmd.cs 与 CreatureCmd.cs.
- 生产 guard: FormStanceSafetyGuard owner 为 Forms.FormStanceSafety, 目标为 PowerCmd.Remove(PowerModel), 精确 1 prefix; FormStanceModifier 的 BeforeDamageReceived 与 BeforePowerAmountChanged 均为 fail-closed 钩子. 证据: mod/FormsCode/FormStanceSafetyGuard.cs, mod/FormsCode/FormStanceModifier.cs.
- 现有 partial helper 可复用: InvokeOnMainThreadWithTimeoutAsync, AwaitOperationWithTimeoutAsync, WaitForConditionWithTimeoutAsync, DrainDetachedOperationsAsync, WriteJsonIfConfigured, Snapshot, DescribeCreature, ReadEnergy, FindWatcherCharacter, FindCardByEntry, FindCardByTypeName, PlayBindingLossCardAsync, BindingLossSnapshot, ReadBindingLossRawStance, ApplyUnobservedFaultGate, QuitOnMainThreadAsync.
- 权威卡牌 fixture: WatcherMod.WatcherCrescendo 为 1 费 TargetType.None 入 Wrath; WATCHER_STRIKE_P 为 AnyEnemy; WatcherMod.WatcherTranquility 为 1 费 TargetType.None 入 Calm. 证据: .tmp/watchermod/WatcherMod/*.cs.
- 真实 Cubex encounter 类型全名: MegaCrit.Sts2.Core.Models.Encounters.CubexConstructNormal; 证据: research/_decomp/game/sts2.decompiled.cs 130269-130278.
- 已落盘 RuntimeSafetySmokeRunner.cs: 含 opt-in 开关, 独立进程冲突拒绝, 真实 run/Cubex/Crescendo setup, 场景 A 的 guard 元数据与三条命令探针, 场景 B 的非 Forms 控制与 Strike/Tranquility 探针, JSON 写出, 有界 drain 和 Quit.
- 已落盘 RUNTIME-SAFETY.md: 记录开关, 真实 setup, 两个场景契约, evidence 字段, 诚实边界.

## 进行中

- 代码已完成静态落盘, 但未编译验证; 需 hub 构建后修正签名/可访问性问题.
- 需 hub 在隔离 APPDATA/GSE 下分别运行两个开关并检查 JSON.
- 若构建失败或运行失败, 不得改写为 passed; 保留原始错误并按失败处理.

## 未知

- 未构建, 未运行, 未取得任何实机 JSON 证据.
- 目标二进制对新增 patch, 真实 Cubex encounter, 两个场景的签名兼容性均未验证.
- 尚未验证 JSON 中是否已包含 productionAssemblySHA256/location 字段; 当前实现未显式写入该字段, 属于 PARTIAL_CODE_COMPLETE 接续点.