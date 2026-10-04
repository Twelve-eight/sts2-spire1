# 选人背景暂停的中央隔离探针

链接实际 CharacterSelectBackgroundPause.cs, 使用测试副本的真实 0Harmony. Godot 节点, 事件和 deferred 队列为明确的契约桩, 不是原生引擎. 覆盖装配, 幂等, 各处理模式恢复, 动态子树, 移出重入, 分支可见性, 模式通知重入, 主线程门禁与空闲不轮询. 最后一项明确证明无事件的晚期外部模式改写仍可能重新启用处理, 不是该问题已解决.

中央先用 dotnet build 将 OutputPath 和 IntermediateOutputPath 显式指向 G: 隔离目录, 再直接运行生成的 BackgroundProbe.dll. 不执行游戏, 不写 Steam 或共享配置. Probe 的通过不解除 SPIRE1_MENU_PERFORMANCE_PROBE 编译门禁, 不替代游戏内背景/角色解锁/多人大厅和帧时间 A/B.