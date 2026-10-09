# 冻结产物静态监督 (r25)

- 模型/路由: global:deepseek-v4.1-flash / wb2api / xhigh; 不委派, 不用其它 harness。
- 门禁: gate-notice.json (native wait_agent, worker 01a10a2a... completed, TimedOut=false) + gate-message.txt。
- 仅静态审查, 不构建/测试/peer, 不继承旧 PASS。

## 已确认
1. 冻结哈希与 gate-notice.json 逐字节一致 (4/4):
   - BindingLossSmokeRunner.cs = 1C6E990E46A22434DA7921E212CECD680AEB12563B0AB8EF714998476242750D (67286)
   - FormNativeSmokeRunner.cs = 51E79681D39A1A188F02545679A5E6D286BA7EAA9DD676C0C7A8DA3352DB5B99 (164478)
   - LifecycleSmokeRunner.cs = 0224CFEBE15147F6F0E9C8CC0E4C5034BA651EEDBC67FF0549C981765C065DE9 (26155)
   - README.md = 3EEF6B54D97943A172856FE8798EF25B98D2BBDDE360939528BB56564FF3C64C (8592)
2. 只改四文件; 生产代码未改 (git status 仅四文件 + 既有未跟踪 RuntimeSafety 两文件, 均不在本写集)。
3. 面1 单 action 叶子身份: FormNativeSmokeRunner.cs:1324-1366 CollectLeaves 要求每个 observed leaf 按 ReferenceEqualityComparer 属于该 root 的 leaf 集合; 空 AggregateException 与非 Forms/restart 叶子拒绝; 纯 wrapper 展开批准。此项满足。
4. 面2 真实两卡 history 已修: BindingLossSmokeRunner.cs:777-887 对本次 CreateCard 真实对象做 before/after ReferenceEquals 计数 (1298-1304), actualCardHistory 进入 BindingLossRunEvidence (1121), Tranquility 探针读自身 finished delta (458,1285-1296), 未知历史 -1 判失败; Strike watcherStrikeHistory 语义未改 (FormNativeSmokeRunner.cs:1998-2011)。
5. 面3 fixture/不变判据满足: 桥 Bound 时真实 PlayerCmd.GainEnergy 补到 >=2 (BindingLossSmokeRunner.cs:329-366), 双向能量 (FormNativeSmokeRunner.cs:2085-2092)、HP/Block (2104-2117)、owner powers (2119-2136)、raw marker determinate/nonconflict (BindingLossSmokeRunner.cs:475-490) 均检查, dualActionProof + explicitRejectionBoth 才可 pass (537-555)。Terminal 只靠生产 Always pump 消费 duplicate AssemblyLoad (993-1011)。
6. 面4 lifecycle 满足: LifecycleSmokeRunner.cs:393-434 用真实 Harmony 元数据核对 owner Forms.FormStanceSafety 恰 1 prefix、目标 PowerCmd.Remove(PowerModel?)、prefix 类型 Forms.FormsCode.FormStanceSafetyGuard 与声明方法 RemovePrefix; 重复 init 身份/数量不变 (158-168); Shutdown 两次后两旧 owner 0 而 safety 仍 1 (170-191); 不直接 call prefix; csproj 无 Spire1 引用。
7. 面5 final 满足: FormNativeSmokeRunner.cs:202-292 先落 pre-quit 再 quit/drain 再 post-quit gate 与最终写盘; raw/expected/unexpected 均持久化 (1295-1298); 批准 root 按 ReferenceEquals 去重传入 final (687-691); 写盘失败不静默覆盖 (277-292)。

## 进行中
- 无。

## 未知
- 实机/运行证据 (本监督不做, 归中央验证)。

## NEEDS_REWORK (P1)
P1 — 跨 action 的 approved-root 并集使 observed fault 失去唯一归属, 违反"一个 observed 组合跨两个合法 action 须保守拒绝"。
- 位置: BindingLossSmokeRunner.cs:687-692 (finalExpectedRejectionRoots 累积 + ApplyUnobservedFaultGate 传 expectedRejections), 与 FormNativeSmokeRunner.cs:1287-1343 (approvedRoots 取全部 root 的 leaf 并集后逐个 observed 匹配)。
- 触发条件: strike 与 stance-probe 两个 action 的 fault root 都被批准后, 任一 observed UnobservedFault 只要每个 leaf 分别落在两者并集任意位置, 即判 expected, 无需归属单一 action。
- 当前控制流: 单 fault 关联用 FaultMatchesExceptionIdentity(single root) 正确, 但 scenario/final gate 改用 FaultMatchesApprovedExceptionIdentities(union), 混合两合法 action 的 observed 组合被吞。
- 最小修复范围: gate 层对每个 observed fault 要求 leaf 集合完整包含于"某一个" approved root 的 leaf 集合, 而非所有 root 并集; 或 expectedRejections 记录 (root, actionLabel) 做 per-action 归属校验。
- 尚缺实机证据: 需中央验证用 r19 mixed-fault 类负例确认混合组合被拒。
- 结论: NEEDS_REWORK。

## 逐面结论 (时间 2026-10-05 12:10:35, 冻结哈希见上)

| 检查面 | 结论 | 关键行号 |
| --- | --- | --- |
| 面1 异常分支 fail closed / observed 覆盖 | 部分通过, 存在 P1 | FormNativeSmokeRunner.cs:1287-1343, 1324-1366; BindingLossSmokeRunner.cs:513-535, 687-692, 1090-1110 |
| 面2 真实两卡 actualCardHistory | 通过 | BindingLossSmokeRunner.cs:751-759, 777-780, 875-887, 1111-1122, 1274-1304 |
| 面3 2能量 fixture 与支付/HP/block/power/marker 不变判据 | 通过 | BindingLossSmokeRunner.cs:329-366, 451-576; FormNativeSmokeRunner.cs:2085-2136 |
| 面4 lifecycle 身份 (owner/prefix/Shutdown) | 通过 | LifecycleSmokeRunner.cs:94-201, 269-303, 313-434 |
| 面5 final post-quit drain 与持久化 | 通过 | FormNativeSmokeRunner.cs:187-299, 1277-1317 |
| README 中文说明与诚实边界 | 通过 | README.md (Lifecycle/BindingLoss 覆盖范围与诚实边界段) |

## 未知 (未完成/未覆盖面)

- 面1 跨 action 唯一归属的负例实测: 静态已判定 P1 存在, 但未做实机/负例运行验证 (本监督不做)。
- 实机行为、真实 Harmony owner 运行时数值、真实 fault 序列与 r19 mixed-fault 负例结果: 未覆盖, 归中央验证。
- 生产 Forms.dll 与冻结四文件的运行时一致性: 未构建/未加载, 未覆盖。

## 最终判定

- NEEDS_REWORK (阻断项: P1 跨 action approved-root 并集吞无关 siblings, 见上)。
- 不因时限或已有候选可编译而放宽; 其余四面静态核对通过不构成整体 PASS。