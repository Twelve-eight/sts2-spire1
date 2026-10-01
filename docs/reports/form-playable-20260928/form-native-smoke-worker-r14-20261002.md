# 姿态形态真实战斗 smoke runner r14 实现报告

- 日期: 2026-10-02
- 角色: bounded implementation
- 用户指定模型: 6.1sol
- 用户指定路由: agentrouter
- 限制: 仅静态修改与静态核对；未构建、未测试、未部署、未启动游戏。
- 允许产品文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`、`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs`

## 已确认

### 检查面 0：当前源码与前轮审查缺口

已读取 r12 主线程审查、r12 flow 审查、r13 state-gate 审查和当前两个目标源文件。当前源码已经包含部分 r14 前置返工（真实形态 gate、`JsonWriteResult`、部分 startup detached 处理），但仍需完成：

- 非异常 action 失败与 `UnobservedFault` 必须统一锁存并写入 action failure。
- `cardPlay.status` 必须由真实 action 终态派生，不能保留 `enqueued`。
- final status 必须同时受业务 `exitCode`、quit gate 和 quit drain 约束。
- scenario/final evidence 写入失败必须进入失败传播。
- main-thread deferred callback、process-frame、startup/action completion 的超时操作必须进入同一 bounded drain 或终止性隔离。

当前这些结论是源码静态证据；真实运行状态仍未知。

## 进行中

- 正在回读 `FormNativeSmokeRunner.cs` 的完整控制流，先保存每个 action/operation 的实际终态字段，再按最小范围修复。

## 未知

- 未构建、未测试、未部署、未启动游戏；目标二进制中的 deferred 调度、真实 action 时序、形态实际效果和 JSON 退出时序均未知。
