# compile-fix-r4 worker 报告

- 时间: 2026-10-05
- 模型/路由: global:deepseek-v4.1-flash / wb2api / xhigh (按派发请求记录, 未自行更换)
- Harness: 原生 Codex, 未委派, 未启动 omp 或 codex exec
- 范围: 仅 2 个白名单产品文件; 未构建, 未 lint, 未测试, 未部署, 未运行游戏, 未执行 git

## 已确认

### 1. [P0] FormStanceWatcherBridge.cs 未导入 Godot 命名空间, Node 等 Godot 符号未限定
- 文件: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs
- 触发条件: Release 诊断构建编译该文件; 日志 G:\omp works\.tmp\forms-independent-20261005\forms-build-r3.log 第 3 行报 FormStanceWatcherBridge.cs(75,20) CS0246 Node 缺 using 或限定.
- 宣称/契约: 该文件不依赖 using Godot, 采用显式限定避免 Godot.Logger 与 MegaCrit.Sts2.Core.Logging.Logger 重歧义; 同一文件全部 Godot 类型必须统一限定.
- 当前控制流: StartPumpLocked 读取 _pumpNode, 通过 Godot.Engine.GetMainLoop() 取得 SceneTree.Root, 创建 FormStanceBridgePump (继承 Godot.Node), AddChild 后保存; StopPumpLocked 用 GodotObject.IsInstanceValid 与 QueueFree 释放.
- 复现命令: 主会话已有 Release 诊断构建 exit1; 本轮不重跑. 证据日志路径见上.
- 最小修复范围: 仅把该文件内未限定 Godot 符号改为 Godot.Node / Godot.SceneTree / Godot.Engine / Godot.GodotObject; 不新增 using Godot; 不改生命周期与公开 interop 签名.
- 已应用: 行 75 `Godot.Node? _pumpNode`; 行 383 `Godot.Node? root`; 行 386 `((Godot.SceneTree)Godot.Engine.GetMainLoop()).Root`; 行 414 `Godot.Node? pump`; 行 424 `Godot.GodotObject.IsInstanceValid(pump)`.
- 尚缺实机证据: 未构建, 未实机.

### 2. [P0] MainFile.cs ResPath 使用 string.Concat, 非常量表达式
- 文件: G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs
- 行号: 25
- 触发条件: 同一 Release 诊断构建; 日志第 4 行报 MainFile.cs(25,35) CS0133 指派给 MainFile.ResPath 的表达式必须是常量.
- 宣称/契约: ModId 与 ResPath 必须仍固定 Forms 资源身份; ResPath 语义为 "res://" + ModId, 即 res://Forms.
- 当前控制流: 静态常量初始化, 无运行时分支; ResPath 用于 Forms 资源路径身份.
- 复现命令: 同上, 主会话 r3 日志.
- 最小修复范围: 将 string.Concat("res://", ModId) 改为常量字符串拼接 "res://" + ModId (两个操作数均为 const), 不改变值, 不改 ModId.
- 已应用: 行 25 现为 `public const string ResPath = "res://" + ModId;`, 值仍为 res://Forms.
- 尚缺实机证据: 未构建, 未实机.

### 3. [P1] 同文件 Godot 符号静态扫描结果
- 文件: 同 1.
- 证据: 正则扫描修复前该文件命中可编译符号 Node x4, GodotObject x1, SceneTree x1, Engine x1; 另有注释中的 Godot 文本 x3.
- 结论: 修复覆盖全部 4 类可编译符号, 未只改行 75. 修复后再次扫描, 未限定 Godot 类型符号为 0 (剩余命中均为注释文本).
- 尚缺实机证据: 未构建.

### 4. 写入面与编码核验
- 仅写入: FormStanceWatcherBridge.cs, MainFile.cs, 本报告 worker.md.
- 两产品文件均保持 UTF-8 无 BOM, LF 行尾; 未引入 CRLF.
- 未执行任何 git 操作; 未写 C:; 未触碰 Steam / 共享配置 / canonical / Workshop.

## 进行中
- 无. 两处最小修复均已落盘并复核.

## 未知
- 修改后是否仍有其它编译错误未验证, 本轮按要求不构建.
- 运行期绑定, 主线程 pump 与实机行为未验证.