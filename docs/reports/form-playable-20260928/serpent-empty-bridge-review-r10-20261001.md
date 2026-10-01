# Serpent empty bridge review r10

- 模型: ovoapi:6.1sol
- 审查范围: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs`
- 约束: 未构建、未测试、未改生产代码、未部署、未启动游戏。
- 证据: 实现报告 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\serpent-empty-bridge-worker-r9-20261001.md`; 中央探针 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-probe-r9-20261001-01\all-after.log`。

## 已确认

- 中央探针记录 `TOTAL 123 PASS 123 FAIL 0 SELECTED 123`，且与本轮限定的 Serpent 场景相关通过项包括：
  - `serpent.repeated_before_and_after_same_object_hit_once`
  - `serpent.inflight_damage_is_awaited_and_after_is_deduplicated`
  - `serpent.empty_enemies_do_not_call_rng`
  - `serpent.removed_source_live_listener_finishes_pending`
  - `serpent.removed_source_and_bridge_duplicates_hit_once_and_clean_up`
  - `serpent.exit_energy_exactly_two_once`
- 重复 listener 已在 `SerpentFormPower.cs:101-114` 先以 `pending.plays.Remove(cardPlay)` 去重；失败时直接走重复回调分支，不执行 `CreatureCmd.Damage`，因此不重复伤害或 RNG。completion bridge 且 tracker 已空时，`SerpentFormPower.cs:105-111` 只做一次清理移除，不支付退出能量。
- 空 completion bridge 清理与失败条件一致：`SerpentFormPower.cs:107-110` 要求 bridge 仍挂载且共享 tracker 为空，然后标记 `exitHandled` 并移除自身；后续 `AfterRemoved` 在 `SerpentFormPower.cs:150-157` 转入 bridge 恢复路径，空 tracker 在 `SerpentFormPower.cs:215-220` 清理后返回。
- 正常移除承接仍成立：`SerpentFormPower.cs:159-179` 在源 power 移除时保留 pending tracker，并在 `SerpentFormPower.cs:251-337` 按正常 receiver、已有 bridge、创建 completion bridge 的顺序承接；成功挂载后共享同一 root tracker，原 listener 的 `finally` 位于 `SerpentFormPower.cs:140-147`，因此最后一个真实 play 仍负责清理 bridge。
- 退出能量单次语义未被重复 bridge 清理破坏：重复分支先将 `exitHandled` 设为 true（`SerpentFormPower.cs:109`），而真实移除的 `FinishRemoval` 在 `SerpentFormPower.cs:196-210` 以该标志防重；中央探针的 `serpent.exit_energy_exactly_two_once` 通过。

## 进行中

- 已完成本轮最多两项失败条件核对；未发现需要提出的 P0-P2 问题。

## 未知

- 未进行真实游戏战斗验收；中央探针明确是窄范围生命周期协作者与 command spies，非完整游戏 bridge/UI/多人/存档/真实 scheduler，且 scripted RNG 不是 `MegaRandom`。
- 未验证编译器、运行时 hook 顺序、真实引擎并发竞态或部署后的实际 power 生命周期。

## 结论

- **审查通过（就本轮限定的两项失败条件而言）**：未发现重复 listener 重复伤害/RNG/退出能量、空 completion bridge 清理、或正常移除承接方面的代码问题。
- **已排除面**：重复 callback 去重、空 tracker bridge cleanup、pending root handoff、completion bridge 的单次退出能量门闩，均有源码控制流与中央 123/123 探针结果支持。
- **未验证边界**：源代码和探针不等于真实战斗验收；必须由后续受控的真实运行流程确认。
