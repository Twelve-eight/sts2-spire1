# FormNativeSmoke 实现增量报告

## 已确认

### 1. [P0] 原生用户目录预检必须先于测试模组加载

- 确认时间: 2026-09-28, 本轮静态读取.
- 证据: G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\runtime-scout.md:29-40, :149-163; G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md:6-9, :34-52.
- 触发条件: 游戏启动在主菜单前就可能写配置, 主菜单后的 shouldSave:false 不能隔离启动期写入.
- 权威契约: 用户目录必须先验证落在调用者指定的 G: 隔离根, 模组测试只在独立副本执行.
- 当前控制流: 原生预检与独立模组分为两段; 模组需显式参数, 主线程挂载, 真实 PlayCardAction 支付链, 自己的 SceneTree.Quit.
- 可复核命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\runtime-scout.md'. 仅源码证据, 不是实机复现.
- 2026-09-28 原始范围: csproj, manifest, C# 驱动及预检资源. 此为历史记录, 本次仅交付第 2 项恢复范围的四文件, B 未实现.
- 尚缺实机证据: 全部构建, 预检和游戏运行. 本会话按授权不执行这些步骤.

### 2. [P1] 2026-09-30 恢复范围仅为 A, B 不进入本次交付

- 证据: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\native-worker-resume-20260930.md:32-37, :52-54.
- 触发条件: 本次恢复请求, 替代 2026-09-28 尚未完成的原卡/实战驱动计划.
- 权威契约: 仅新增 project.godot, userdata_probe.gd, Node PCK 打包脚本, README; 不运行打包/构建/测试/游戏/git, 不再委派.
- 当前控制流: 截至恢复时 tools\form-native-smoke 没有已落盘代码. 先做零 autoload 的原生预检载体和可审查的打包代码, 不等待 B.
- 可复核命令: Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\native-worker-resume-20260930.md'.
- 最小修复范围: 上述四文件与本报告, 不改生产源码或其它文档.
- 尚缺实机证据: 所有打包和实际二进制预检仍由主会话执行, CODE_COMPLETE 仅表示 A 的源码交付.

### 3. [P1] PCK 使用 Godot 4.5.1 官方 V3 写入布局

- 确认时间: 2026-09-30, 仅网络只读获取官方源码, 没有执行或保存外部程序.
- 权威证据: godotengine/godot 的 4.5.1-stable, core/io/file_access_pack.h:39-51; core/io/pck_packer.cpp:92-123, :148-199, :204-256; core/io/file_access_pack.cpp:260-325.
- 本地相关证据: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\godot-os-windows-4.5.1.cpp:2396-2417, :2487-2488.
- 触发条件: 为实际游戏 EXE 提供只含两个资源的独立 --main-pack, 不使用游戏原 PCK.
- 当前控制流: 官方 magic 为 0x43504447, 版本 3, PACK_REL_FILEBASE=2, 文件基址与目录偏移为两个 uint64, 16 个保留 uint32, 文件区 32 字节对齐. 目录记录相对文件偏移, 原始长度, MD5, 0 文件标志; 路径 UTF-8 长度按 4 对齐.
- 最小实现范围: G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\pack-userdata-probe.mjs:12-18,65-107,110-140. 仅硬编码两个输入文件, Node 内建模块, 不调用 Godot 或其它进程.
- 可复核命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\pack-userdata-probe.mjs'. 精确打包命令写入同目录 README, 本阶段不执行.
- 尚缺实机证据: PCK 尚未生成, 未经实际 EXE 加载, 文件格式源码一致性不等于二进制验收.
- 2026-09-30 增量落盘: 已新增 project.godot 与 pack-userdata-probe.mjs. 项目无 main_scene/autoload/插件, 禁用文件日志和音频输入, 指定 Dummy. 打包器固定两个资源, V3 布局, 仅允许本轮 G: .tmp 下已存在的普通父目录, 拒绝链接/目录穿越/覆盖, 没有执行打包. 此时 GDScript 与 README 尚未落盘, 不能宣称可运行.

### 4. [P0] GDScript 的显式目录/实际驱动校验已落盘

- 确认时间: 2026-09-30, 源码落盘, 未执行.
- 证据文件: G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\userdata_probe.gd:10-104 为入口与实际值校验, :107-176 为参数/路径/链接边界, :179-209 为逐条刷新和退出.
- 权威接口: Godot 4.5.1-stable doc/classes/DirAccess.xml:267-273 的 is_link 覆盖 junction/reparse point; doc/classes/AudioServer.xml:121-125 的 get_driver_name 返回实际驱动.
- 触发条件: 显式 --expected-appdata-root 与 --report 参数, 由 SceneTree 脚本独立执行.
- 当前控制流: 先验证本轮 G: .tmp 内的新报告路径和普通父目录, 持续 flush 真实 observed/result JSONL. 随后校验 APPDATA 精确一致, user_data_dir 子路径边界, LOCALAPPDATA/TEMP/TMP 隔离, 实际 headless/Dummy, 文件日志关闭, 专用项目身份和零 autoload. 不创建 user:// 文件, 不创建目录, 不覆盖既有报告.
- 最小修复范围: 仅 userdata_probe.gd. 参数/隔离失败返回 2, 报告 IO 失败返回 3, 全部检查完成且报告刷新成功才返回 0.
- 可复核命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\userdata_probe.gd'. 实际启动参数已写入 G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\README.md:53-61, 本会话不执行.
- 尚缺实机证据: GDScript 未解析执行, PCK 未生成. 链接检查只覆盖检查时点, 不认证对抗并发目录替换或脚本执行前的原生引擎写入.

### 5. [P1] 主会话运行说明已落盘, 不包含自动启动器或 B 代码

- 确认时间: 2026-09-30, 只读发现 E:\Slay the Spire 2\SlayTheSpire2.exe 与 G:\nodejs\node.exe 路径, 没有启动任一程序.
- 证据: G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\README.md:24-38 为目录准备和打包, :40-61 为环境/启动参数, :63-68 为判定边界.
- 触发条件: 主会话接手 A 产物后, 另行执行目录准备/打包/原生预检.
- 权威契约: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\requests\native-worker-resume-20260930.md:54; 本阶段只交付四文件.
- 当前控制流: 文档依序给出独占隔离目录, 只读 EXE 副本, 精确 Node 命令, 子进程环境, EXE 参数和 Hidden/低优先级/超时要求. 成功需本次 PID/新报告/最终 PASS/退出码 0 同时匹配; 未生成报告或崩溃不算通过.
- 最小修复范围: README, 不新增启动器, csproj, manifest 或 C# 驱动.
- 可复核命令: Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\README.md'.
- 尚缺实机证据: Node 版本/运行可用性, EXE 依赖完整性, PCK/GDScript 接受情况以及全部预检结果均未验证.
- 2026-09-30 最终静态回读清单: G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\project.godot:1-20; G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\pack-userdata-probe.mjs:1-141; G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\userdata_probe.gd:1-209; G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\README.md:1-72. 工具目录在该次读取时只有这四个文件, 没有 PCK, DLL 或其它生成产物. 行号是该时点的源码证据, 不是测试计数.
- CODE_COMPLETE 仅针对 2026-09-30 的 A 阶段. 主会话最短顺序: 按 README 准备 G: 独占目录和只读副本 -> 单独打包 -> 使用 Hidden/低优先级启动器及指定隔离环境做原生预检 -> 同时核对新报告和退出码. 本会话不执行任何一步运行操作.

## 进行中

- 2026-09-30: A 状态 CODE_COMPLETE, 四文件已完成源码落盘和静态回读, 没有剩余 A 编写步骤. B 不在本次范围且未开始. 打包和原生预检留给主会话, 不属于本会话已完成的验证.
- 本会话不运行构建/测试/游戏/打包, 不操作窗口/键鼠/其它进程, 不写 C:, 不 git, 不再委派.
- 请求模型与路由为 gpt-6-astra-ar / gateway -> agentrouter -> gpt-6-astra; 当前会话没有可采信的实际路由元数据, 不以请求文字冒充已核验路由.

## 未知

- A 的 Node/GDScript 均未执行, 未运行语法检查器, 未生成 PCK, 未启动 EXE. 当前二进制的原生目录, 缓存写入, 无窗口/Dummy 和异常退出行为均未验证.
- B 的 C# 载体/实际卡牌/内存往返未实现. tooltip/动画/音效, 多人/战中重连/跨进程续档不在 A 的证明范围.
