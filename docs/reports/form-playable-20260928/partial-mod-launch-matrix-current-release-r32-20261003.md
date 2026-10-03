# 当前 Release 字节部分 Mod 交叉启动矩阵 - 2026-10-03

## 结论

使用当前 Release payload（DLL `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`，PCK `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79`）重新跑了 9 个隔离挂载组合。9/9 进程退出码为 0；无超时、无窗口句柄、日志排空、无嵌套 manifest；共享配置未改变，测试 mods 清理完成，Steam settings 恢复。

## 证据

- runner：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r32-current-release-20261003.ps1`
- 汇总：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r32-current-release-20261003\matrix-summary.json`
- 运行副本：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\`

## 结果

- `BaseLib + Spire1`：Spire1 initializer 成功；Watcher 缺失时 Forms bridge 安全关闭。
- `BaseLib + Watcher + Spire1`：三者 initializer 成功，Watcher bridge 可绑定。
- `BaseLib + AutoAnthony + Spire1`：Spire1 正常启动，AutoAnthony 可选状态不阻塞。
- `BaseLib + AutoAnthony + Watcher + Spire1`：legacy bridge 正常启动。
- `BaseLib + AutoAnthony + Watcher + AutoAnthonyWatcher + Spire1`：官方 addon 接管，legacy bridge 关闭。
- `BaseLib + Watcher + AutoAnthonyWatcher + Spire1`：缺少 AutoAnthony 时 addon 被 ModLoader 拒绝，但 BaseLib、Watcher、Spire1 仍成功启动。
- `Spire1` 单独挂载：因 manifest 声明的 BaseLib 缺失而被拒绝，未调用 Spire1 initializer。

## 结论边界

这关闭的是当前 Release 字节的启动层依赖边界，不代表可见 UI、形态战斗、存档、重连、多人、性能或平衡验收。
