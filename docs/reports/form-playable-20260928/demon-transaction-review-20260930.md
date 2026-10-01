# Demon transaction review 2026-09-30

## 已确认
- 已先核对当前监督审查会话的本地 session 元数据：`C:\Users\o_Obl\.codex\sessions\2026\10\01\rollout-2026-10-01T06-21-13-01a0f468-55aa-7ce3-a004-52d3c76c5cb3.jsonl`。
- 当前 turn 的真实运行模型记录为 `ovoapi:6.1sol`，不匹配本轮硬性指定的 `gpt-6-astra-ar`；同一记录的 `turn_context` 位于对应 JSONL 的 ordinal `5`/`63`。
- session 元数据的 `model_provider` 记录为 `gateway`，但未提供可确认的完整 `gateway -> agentrouter` 路由证据；因此不能声称路由满足要求。
- 按用户最新指令，在模型不匹配且路由无法确认后立即停止；不再等待实现者、不读取或审查代码、不构建、不测试、不部署、不修改游戏或共享配置。
- 本文件是本轮唯一写入路径：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\demon-transaction-review-20260930.md`。

## 进行中
- 无。因模型与路由门禁不通过，审查未启动。

## 未知
- 当前会话是否实际经过 `gateway -> agentrouter`：可见 session 元数据不足以确认。
- 未对 Demon 写入边界修复进行任何有效审查结论；未确认 `PowerModel.SetAmount` 插入点、clamp 后 stored amount、write-after-event-throw、write-before-exception、nested `SetAmount`、`ModifyAmount` async depth、transpiler 唯一匹配失败行为，或 `DemonFormPower` 兼容性。
