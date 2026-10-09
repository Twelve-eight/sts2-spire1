# 选人背景与卡图日志实验兼容层, 2026-09-23

## 范围与发布门禁

本轮用户要求继续优化所有活跃代码并处理选人掉帧报告. 新实现放在 Spire1 的独立 Experimental/MenuPerformance 目录, 全部源码用 SPIRE1_MENU_PERFORMANCE_PROBE 编译符号包围. 默认 Release 不包含这些类型. 仅中央隔离验证显式开启. 不修改 mod 清单或公共设置, 不部署测试/Steam, 不发布, 不启动游戏. 解除门禁前必须补测试副本中的可视生命周期, 多人大厅, 角色解锁和帧时间 A/B 验证. 不停用 Watcher 或其它 mod.

## 已核实的场景寿命

- 测试 A 的 sts2.dll SHA256 为 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9, 与本轮只读 Steam 对照相同. 不表示两份安装都已实际复现.
- G:\omp works\.tmp\workspace-audit-20260923-01a0cbfd\evidence\charselect-scene-lifetime\MegaCrit.Sts2.Core.Nodes.NSceneContainer.decompiled.cs 的 SetCurrentScene 对旧子节点 RemoveChildSafely 和 QueueFreeSafely. NGame 的 MainMenu 返回 RootSceneContainer.CurrentScene as NMainMenu, 新局路径调用 SetCurrentScene(NRun.Create(...)). 旧报告中的整个游戏生命周期常驻不能成立于该已读路径, 不能据此声称已证明整局战斗掉帧根因.
- 主菜单内部的缓存和背景仅隐藏问题仍存在. NCharacterSelectScreen._Ready 在 AnimatedBg 取得容器, OnSubmenuClosed 不销毁背景. _Process 更新联网大厅, 绝不可停用整个屏幕.
- 原始报告与反编译材料保持不变. 修订结论以本契约和独立验证报告说明, 不改原始证据或把源码推理写成实机测量.

## 线路 A: 仅暂停不可见的背景子树

- 只在 NCharacterSelectScreen._Ready 完成后装配 AnimatedBg 的事件驱动管理器, 幂等. 不改 _Process, lobby, 音效, 按钮, 解锁逻辑, 角色/皮肤加载和背景数量. 不全局修改 Spine.
- 当背景 IsVisibleInTree=false, 保存每个被改节点的原 ProcessMode 后置为 Disabled. 必须覆盖子孙的 Always 等显式模式, 不能只改父节点. 再显示时精确恢复原值, 原本 Disabled 仍 Disabled. 只遍历进入/可见性/结构变动事件, 不增加逐帧扫描.
- 保存对象和订阅具有明确生命周期, 节点移出时恢复并解除订阅, 重入可重新管理, 释放后不访问已失效对象. 动态添加到隐藏背景的子树必须被捕获. 原始模式只记一次, 重复隐藏不覆盖为 Disabled.
- 若子节点 _Ready 后自行更改 ProcessMode, 必须用事件后 deferred reconciliation 等可证实的边界处理, 或明确该边界并保持 held-back. 不能把不存在的 mode-changed 信号当真 API.
- 所有 Godot 节点操作只在主线程. 不创建 Task.Run 或后台树遍历. 控制器可单独关闭并恢复全部当前管理节点.

## 线路 B: ViolaSnakeBite 窄日志变换

- 只处理程序集 ViolaSnakeBite 中类型 ViolaSnakeBite.CardModel_GetPortrait_Patch 的 Postfix(CardModel, ref string), 不改安装文件, 不全局屏蔽 GD.Print, 不复制或改写其卡图字典/匹配/资源检查/结果赋值.
- 在主菜单 _Ready 后尝试安装, 允许第三方晚于 Spire1 初始化. 未安装模组或 ABI 不符时安全无操作. 安装幂等, 不对其它 Harmony 所有者做 UnpatchAll.
- 窄 transpiler 仅将确认的静态 void GD.Print 单参数调用替换为 pop, 保留原参数表达式求值, IL labels 和 exception blocks. 只移除该热路径的日志写出, 不声称消除所有字符串分配. 如字节结构/调用数不符, 原样返回全部 IL, 不做部分危险改写.
- 必须验证对已经被 Harmony 包装调用的第三方 Postfix 仍有效; 不能只证明静态 IL 中少了 Print. 中央使用真实 0Harmony 和合成安全资源边界做隔离执行对照. 未完成第三方实机加载顺序和卡图 smoke 前不解除门禁.

## 验证分级

中央负责构建, 真实本地隔离入口与源码链接探针, 工作者只写代码. 合成树/资源桩仅验证管理逻辑, 不称 Godot/游戏实机. 默认与符号开启的构建都检查程序集类型清单, 防止测试构建排除了新代码或普通构建意外带入. 所有输出与缓存在 G:, 不覆盖旧 dirty/staged 改动.