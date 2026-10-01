# 无前台运行验证只读研究

- 开始时间: 2026-09-28 19:20:22 Asia/Shanghai.
- 唯一写入: `G:/omp works/Sts/sts2-spire1/docs/reports/form-playable-20260928/runtime-scout.md`. 不启动程序, 不运行验证工具, 不构建, 不操作前台或已有进程, 不写产品代码/安装/配置, 不再次委派.
- 请求模型: `gpt-6-astra-ar`; 请求路由: `gateway -> agentrouter -> gpt-6-astra`. 当前使用 Codex 会话工具进行静态读取; 实际模型与路由元数据未核验, 按请求由主会话核验.
- 证据等级: 本报告只提供本轮源码检查和历史材料定位, 没有本轮隔离运行或实机证据.

## 已确认

- 2026-09-28 19:20:22: 已读取工作区入口及任务契约. `G:/omp works/.tmp/form-playable-20260928-01a0e7ad/requests/runtime-scout.md:27-36` 明确本轮仅查证路径隔离/模组筛选/战斗测试入口, 不能执行候选启动命令. `G:/omp works/AGENTS.md:18-26` 明确 Steam 只读和共享 mod_configs 风险; 本报告中的任何命令均为待授权建议, 不表示已运行.

### 1. [P1] 不能把现有 --autoslay 当作形态语义的无副作用测试开关

- 确认时间: 2026-09-28 19:20:55.
- 触发条件: 游戏命令行含忽略大小写且去掉前导 '-' 后等于 autoslay 的参数.
- 源码证据: `G:/omp works/Sts/sts2-spire1/mod/Spire1Code/Patches/AutoSlayGatePatch.cs:19-32` 在该条件下把 NGame.IsReleaseGame() 改为 false; 同文件 `:50-89` 还在战斗房间将玩家 LoseHpInternal 伤害清零, 并将非正生命设置抬到 1. 异常读取房间时默认开启不死, 见 `:56-69`.
- 宣称或契约: 文件注释 `:10-17` 说明这是放开引擎内置完整自动爬塔的开关, 不是单战斗测试入口.
- 当前控制流: 参数同时影响发布模式与生命语义, 因而会污染形态生命/伤害相关断言. 不能仅因它自动退出就称为原版行为实测.
- 可复核命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\AutoSlayGatePatch.cs'`. 此为源码读取, 不是实机复现.
- 最小改动建议: 单战斗探针使用独立参数, 不传 --autoslay; 本轮不改代码. 主菜单后接入点需继续核对引擎.
- 尚缺实机证据: 当前 E: 安装是否加载同一 Spire1 产物, 真实菜单与退出, 六形态和 Watcher 原卡行为均未运行.

- 引擎调用确认: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:694-700,758-764` 的发布包确实把 IsReleaseGame() 固定为 true, AutoSlay 分支先 await LaunchMainMenu(skipLogo: true), 再读取 seed/log-file 并调用 AutoSlayer.Start. 不是只运行一场战斗.

- 还有独立的行为污染: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms/CombatRoomHandler.cs:39-45,60-63,89-104` 在引擎 AutoSlay 战斗处理器里施加 999 PlatingPower/999 RegenPower, 第 3 回合起另加 200 StrengthPower, 通过 CardCmd.AutoPlay 而非手动 PlayCardAction 出牌. 即使移除 Spire1 不死补丁, 也不能直接拿该处理器验虚空的手动免费额度或原始伤害数值.

### 2. [P0] BaseLib 配置直接绑定原生用户目录, APPDATA 隔离尚无证明

- 确认时间: 2026-09-28 19:23:54.
- 触发条件: 任一 BaseLib ModConfig 实例构造, 不需要开局或保存游戏.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/baselib-dll/BaseLib.Config/ModConfig.cs:95-115` 将路径固定为 Path.Combine(OS.GetUserDataDir(), "mod_configs", filename), 紧接着调用 Init().
- 宣称或契约: 本轮契约 `G:/omp works/Sts/sts2-spire1/docs/DEVELOP-form-playable-20260928.md:8-9` 要求完全隔离配置和存档. C# 中这一调用不能证明 Windows 原生 OS.GetUserDataDir() 会服从进程 APPDATA.
- 当前控制流: 配置根在构造时捕获, 主菜单后再设置环境变量或更换 SaveManager 已不足以改写此实例的路径.
- 可复核命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\baselib-dll\BaseLib.Config\ModConfig.cs' -Pattern 'GetUserDataDir|_path|void Init|Save'`.
- 最小改动建议: 必须在启动前证明原生用户目录可重定向, 并覆盖 Godot user:// 与 BaseLib 路径; 不能只设置 APPDATA 后就执行 E: 游戏. 本轮不写配置.
- 尚缺实机证据: 当前 E: EXE 的原生目录解析, 配置是否创建在隔离根, 日志/存档/退出保存是否全部落入隔离根.

- 路径链补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves/UserDataPathProvider.cs:12-38,41-71` 将账号根置于 user://<platform>/<id>, 模组局档案目录为 modded/profile<id>. IsRunningModded 只增加子目录, 不隔离 user:// 根或 mod_configs.

- 初始化时序补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/OneTimeInitialization.cs:43-61` 在 ModManager.Initialize 前已调用 SaveManager.InitSettingsData(); `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves.Managers/SettingsSaveManager.cs:26-40` 在读失败时会新建并写 settings.save. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:649-659,685-687` 还在主菜单前迁移/归档旧目录, 云同步和初始化档案. 因此 shouldSave:false 或菜单后的路径检查不能保护启动期共享数据.

- 原生路径证据边界: 当前 EXE 的 .rdata 同时含 APPDATA@78749084, LOCALAPPDATA@78749094, SHGetKnownFolderPath@88890336. 字符串存在不能证明 OS.GetUserDataDir 的调用链或 APPDATA 优先级; 本地给定的 engine-dllsrc 是托管游戏代码, 不是 Windows Godot OS 实现. 本轮尚无匹配版本的原生源码或隔离真测可证明 APPDATA 足够, 因此当前安全判定为不放行游戏启动.

### 3. [P1] TestMode 是改变游戏分支的进程级状态, 不是 --headless 的同义词

- 确认时间: 2026-09-28 19:23:54.
- 触发条件: TestMode.IsOn 被设置为 true, 或测试场景路径出现在引擎命令行.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.TestSupport/TestMode.cs:12-29` 中 IsOn 是可写静态 bool, IsOff 为其取反; IsTestRunFromCmdline() 单独检查参数包含 RiderTestRunner/. `:50-55` 明确 TurnOnInternal() 只允许 NetCoreRunner/CiCoreRunner 调用.
- 宣称或契约: `:20-25` 解释主场景晚于 autoload, 所以 Logger/Sentry 等早期代码不能只看 IsOn.
- 当前控制流: 无任何 headless 参数映射到 IsOn 的逻辑位于本文件; 两种开关不能混称. IsOn 的影响范围仍在核对.
- 可复核命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.TestSupport\TestMode.cs'`.
- 最小改动建议: 将真实游戏进程无窗口探针和 TestMode 逻辑探针分开报告, 不在原游戏初始化前盲目翻全局状态.
- 尚缺实机证据: 发布包是否包含测试场景, TestMode 下命令链可运行边界, 以及正常模式与测试模式的差异.

- 补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves/SaveManager.cs:180-197` 在 TestMode.IsOn 时选 MockGodotFileIo("user://test"), 正常模式才走 GodotFileIo/可能的 Steam 云端. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModManager.cs:89-93,163-175` 在 TestMode 中默认跳过模组初始化; ResetForTests 虽可放开下一次初始化, TryLoadMod 是否执行真初始化仍需复核. 所以 TestMode 不是保留完整加载链的纯视觉开关.

- 模组加载差异已核实: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModManager.cs:795-840,870-875` 在 TestMode.IsOn 时不加载真正 DLL/PCK, 改为 TestInitializers 字典; 即使 ResetForTests 放开 Initialize, 也不能当作真实 BaseLib/Watcher/Spire1 的装载测试.

- NonInteractiveMode 也不是 headless 同义词: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/NonInteractiveMode.cs:8-18` 只取 TestMode.IsOn 或默认 false 的 AutoSlayerCheck 回调, 未读取 DisplayServer. 不能因原生 headless 就推定全部异步错误会自动变成进程失败.

### 4. [P0] 引擎按 EXE 所在目录发现模组, mod_list 是禁用列表而非严格白名单

- 确认时间: 2026-09-28 19:24:31.
- 触发条件: ModManager.Initialize(...) 且未传 nomods.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModManager.cs:74-111` 从 OS.GetExecutablePath() 的父目录读取 mods 和 mods_STEAMTEST, SteamInitializer.Initialized 时还读取 Workshop; `:121-127` 先移除禁用模组再排序加载. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModSettings.cs:10-24` 只把列表里同 id, 同 source, 且 IsEnabled=false 的条目视作禁用.
- 宣称或契约: ModManager 注释 `:68-72` 约定在 ModelDb/LocManager 之前加载模组. 不能在主菜单才筛选已加载的 DLL/PCK.
- 当前控制流: 只写 BaseLib/Watcher/Spire1 三个 enabled 条目不能排除 E: 中其它模组; 未列出的条目默认启用. 空列表首次启用还会进入复制旧存档分支, 见 `:134-155`. 已确认的参数只有该加载入口的 nomods 总开关, 尚无 mods-path 证据.
- 可复核命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Modding\ModManager.cs' -Pattern 'CommandLineHelper|GetExecutablePath|mods_STEAMTEST|ReadSteamMods|RemoveDisabledMods'`.
- 最小改动建议: 后续若获准建立隔离游戏副本, 应只在副本的 mods 放所需三件及独立测试载体, 并明确关闭 Steam/Workshop 来源; 不移动/禁用/替换 E: 原安装模组. 配置格式与存放根继续核对.
- 尚缺实机证据: 当前 EXE 与反编译版本同一性, 实际仅加载允许集合, 独立副本原生目录完全隔离.

- 参数与格式补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/CommandLineHelper.cs:10-31,34-50` 只解析 OS.GetCmdlineArgs(), 支持 --key=value 或 --key value, 没有白名单机制. ModManager 整文件只调用一次 CommandLineHelper, 即 `:83` 的 nomods. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/SettingsSaveMod.cs:13-20` 的字段为 id/source/is_enabled; `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModSource.cs:6-16` 枚举原始值依次 None=0, ModsDirectory=1, SteamWorkshop=2, JSON 是否写数字仍需核对序列化器.

- 设置归属补充: 命中 `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves/SettingsSave.cs:66` 的 mod_settings 字段. 这是引擎 settings.save 内的对象, 不是名为 mod-settings 的独立文件. 具体序列化字符串和值还在核对.

- 可用的 Steam 隔离参数已找到: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:1385-1393` 明确接受 --force-steam=off 并在 SteamInitializer.Initialize 前直接返回. 这比假定缺 steam_api64.dll 或模拟器天然离线可靠. 配合 `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModManager.cs:108-110` 可从控制流排除 Workshop 发现, 配合 `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves/SaveManager.cs:190-196` 排除云存储初始化; 仍须当前产物运行核验.

- JSON 枚举补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves/MegaCritSerializerContext.cs:38-47` 注册 SnakeCaseJsonStringEnumConverter<ModSource>, 所以配置应使用 source:"mods_directory" 或 source:"steam_workshop", 不应凭枚举数字猜写现行格式.

### 5. [P1] C# 游戏层明确识别 headless, 但未证明当前 EXE 静音与用户目录安全

- 确认时间: 2026-09-28 19:25:51.
- 触发条件: 原生 DisplayServer.GetName() 为 headless.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:769-776` 跳过 ApplyDisplaySettings/ApplySyncSetting, 仍设置 Engine.MaxFps. `:667-671` 仍执行 AudioManager/DebugAudio 的音量设置.
- 宣称或契约: 目标是无窗口且无音频, 不是最小化或隐藏一个正常渲染窗口.
- 当前控制流: 渲染层有 headless 分支, 不等于所有音频扩展随 Godot Dummy 音频一起禁用. 本轮只读观察 E:/Slay the Spire 2/ 下存在 SlayTheSpire2.exe, SlayTheSpire2.pck, fmod.dll, fmodstudio.dll 和 libGodotFmod.windows.template_release.x86_64.dll.
- 可复核命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs' -Pattern 'headless|SetMasterVol|SetSfxVol|SetMasterAudioVolume'`.
- 最小改动建议: 候选参数 --headless 与 --audio-driver Dummy 仍需原生帮助/本地历史证据核验, 还要核对 FMOD; 不用 Start-Process -WindowStyle Hidden 替代 --headless, 本轮不启动.
- 尚缺实机证据: 当前 E: EXE 真实参数支持, 启动至菜单是否无窗口/无音频, 崩溃处理器是否会弹窗, 所有写路径是否隔离.

- 历史证据不能冒充 headless: `G:/omp works/.tmp/sts2-reflection-autoslay-smoke-20260927/godot-FXR2706-defect-bridge.log:3,6,10,1225,1311` 显示 Direct3D 12, FMOD 初始化, Steam 运行和实际 GPU. 该日志不是无窗口/静音/隔离目录通过证明, 本轮没有重放它.

- 当前 EXE 的静态二进制证据: 对 `E:/Slay the Spire 2/SlayTheSpire2.exe` 只读解析 PE 的 .rdata, 文件大小 894748672 字节. 帮助参数字符串位置为 --headless@78789653, --audio-driver@78788902, --log-file@78789774, --path@78788395, --main-pack@78788622, --disable-crash-handler@78792378. 二进制没有源码行号, 这里明确使用从 0 起算的文件字节偏移. 本轮未执行 EXE 的 --help. 在该节没有找到 --user-data-dir 或 GODOT_USER_HOME; 这不是完整原生控制流的反证, 但不能把它们当已支持参数推荐.

- 原生帮助文案已只读定位: `E:/Slay the Spire 2/SlayTheSpire2.exe` 在 --headless 字符串邻近的帮助文案明确说其等价于 --display-driver headless --audio-driver Dummy. --log-file <file> 的帮助明确覆盖项目默认日志路径. 这证明原生参数的定义, 仍不是 FMOD 扩展或真实启动通过的证据. --path 的帮助只定义为含 project.godot 的项目目录, 不能拿它冒充用户数据或 mods 根的重定向.

### 6. [P2] 现有 menu-background-probe 明确是契约桩, 不能复用为真实 Godot 战斗证明

- 确认时间: 2026-09-28 19:27:35.
- 触发条件: 试图把已有隔离探针通过结果等同于原生游戏验证.
- 源码证据: `G:/omp works/Sts/sts2-spire1/tools/menu-background-probe/README.md:3-5` 明确节点/事件/deferred 队列为桩; `G:/omp works/Sts/sts2-spire1/tools/menu-background-probe/Program.cs:11-17,19-29,90-92` 使用合成树与受控 Callable/OS 状态, 只是真实 Harmony 装配.
- 宣称或契约: README 和程序输出均说明不覆盖原生引擎时序, Spine, 多人大厅或帧时间.
- 当前控制流: 该探针测试选人背景补丁, 不启动 NGame, 不建立真实 RunState/CombatState, 不加载 Watcher 原卡.
- 可复核命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\tools\menu-background-probe\README.md'`. 现有脚本只读, 本轮不执行任何 build 或 probe.
- 最小改动建议: 桩只作为纯规则和装配回归, 实机层另设独立测试载体; 原生/反射历史工具继续按路径定向查证.
- 尚缺实机证据: 六形态真实卡牌链, 资源与 UI, 原生主线程时序, Watcher 通知消费者.

- 其它定向核对: `G:/omp works/.tmp/callabletest/Program.cs:9-25` 仅比较 Callable/委托身份, 无 NGame/战斗; `G:/omp works/.tmp/finstatetest/Program.cs:5-20,23-39` 测试本地 Target 类的 Harmony Finalizer, 也不是原生游戏入口. 这两者不能作为本轮实机载体复用.

### 7. [P1] 已有真实单局入口与无键鼠单战斗引导链, 但需要独立测试载体

- 确认时间: 2026-09-28 19:29:05.
- 触发条件: 独立进程完成游戏启动, 原生主线程上调用局内 API.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:481,1137` 公开 GameStartupComplete 和 Task<RunState> StartNewSingleplayerRun(CharacterModel character, bool shouldSave, IReadOnlyList<ActModel> acts, IReadOnlyList<ModifierModel> modifiers, string seed, GameMode gameMode, int ascensionLevel = 0, DateTimeOffset? dailyTime = null). `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes.Debug/NSceneBootstrapper.cs:83-109,130-132` 是真实引擎已有的单场景参考实现.
- 宣称或契约: Bootstrapper 注释 `:22-25` 明确以角色/房间/细节参数跳到场景; 本轮契约 `G:/omp works/Sts/sts2-spire1/docs/DEVELOP-form-playable-20260928.md:22-28` 要求真实局内 ModifierModel 和 Watcher 原通知链.
- 当前控制流: 引导器先 await GameStartupComplete, CreateForNewRun -> SetUpNewSingleplayer -> LoadRunAssets -> Launch -> NRun.Create -> SetActInternal(0) -> 两个位置同步器 -> settings.Setup -> EnterRoomDebug(指定 encounter.ToMutable()). 这条链无需点击菜单, 也不需要完整自动爬塔.
- 可复核命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes.Debug\NSceneBootstrapper.cs'`.
- 最小改动建议: 测试载体在主线程等待启动完成并额外确认主菜单已存在, 使用 shouldSave:false 与固定 seed/角色/修正/遭遇. 不用 Task.Run 调用 Godot API, 不碰已有游戏. --bootstrap 是否能直接发现 mod 的设置实现继续核对, 不能现在宣称可直接使用.
- 尚缺实机证据: 测试载体尚未实现或运行, 当前原生资源是否支持无窗口创建战斗, 战斗开始/结束/自己退出的完整结果.

- 已排除直接使用 --bootstrap: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes.Debug/IBootstrapSettingsSubtypes.cs:9-13` 的发布生成表为空且 Count 恒为 0, `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes.Debug/BootstrapSettingsUtil.cs:9-15` 因而返回 null; `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes.Debug/NSceneBootstrapper.cs:85-90` 记录缺类型并退出初始化. 仅在 mod 内实现 IBootstrapSettings 不会自动进入该已生成空表. 建议复用调用链而不是直接推荐 --bootstrap 作为现成测试命令.

- 自己退出的风险: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:978-1000` 的 NGame.Quit() 会写 settings/prefs/progress/profile, 不受 shouldSave:false 保护. 测试载体应在完成自身断言/报告后对自己的 SceneTree 调用 Quit(exitCode), 不枚举或结束外部进程; 这仍不能替代启动前的数据根隔离.

### 8. [P1] 模型和战斗有真实 API, 手动出牌必须保留支付与完整 Hook 链

- 确认时间: 2026-09-28 19:34:07.
- 触发条件: 建立只覆盖一个战斗的测试, 或用反射测试误把直接调用效果当作手动出牌.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Models/ModelDb.cs:423-431,473-478` 为 ModelDb.Init(Type[]? injectedModelTypes = null) 和 InitIds(); `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/OneTimeInitialization.cs:79-85` 明确初始化顺序为 LocManager.Initialize -> AssemblyInfo.Init -> ModelDb.Init -> ModelIdSerializationCache.Init -> ModelDb.InitIds -> 消息/动作类型. 正常菜单后的测试不能再无条件重跑 ModelDb.Init.
- 精确状态签名: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunState.cs:304-316` 为 CreateForTest(IReadOnlyList<Player>? players = null, IReadOnlyList<ActModel>? acts = null, IReadOnlyList<ModifierModel>? modifiers = null, GameMode gameMode = GameMode.Standard, int ascensionLevel = 0, string? seed = null); `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Combat/CombatState.cs:153-162` 为 CombatState(EncounterModel? encounter = null, IRunState? runState = null, IReadOnlyList<ModifierModel>? modifiers = null, IReadOnlyList<BadgeModel>? badgeModels = null, MultiplayerScalingModel? multiplayerScalingModel = null), 初始 RoundNumber=1.
- 当前控制流: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Combat/CombatManager.cs:444-474` 的 SetUpCombat(CombatState state) 重置/填充玩家战斗状态和卡表, AfterCombatRoomLoaded() 才启动真实异步回合循环. 仅 new CombatState 或更改 RoundNumber 不触发入场/回合 Hook. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunManager.cs:446-461` 提供 SetUpTest(RunState state, INetGameService gameService, bool disableCombatStateSync = true, bool shouldSave = false), 但这属于测试态替代, 不等于 NGame 实机启动.
- 宣称或契约: 六形态区分手动/自动/重复出牌, 并要求真实回合开始, 见 `G:/omp works/Sts/sts2-spire1/docs/DEVELOP-form-playable-20260928.md:34-52`.
- 可复核命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs' -Pattern 'OnPlayWrapper|CanPlay|ToMutable'`.
- 最小改动建议: 优先让真实 CombatRoom 创建状态; 若做逻辑探针, 明确 TestMode/MockStore 边界. 后续核对 PlayCardAction 而不把 OnPlay 直接调用当成手动打牌.
- 尚缺实机证据: 单战斗驱动尚未跑通, 真 Watcher 模型注册/形态 Hook/卡费支付/回合切换均未执行.

- 完整手动出牌已定位: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.GameActions/PlayCardAction.cs:42-52,62-103` 的构造签名是 PlayCardAction(CardModel cardModel, Creature? target), 执行时验证在手牌堆/CanPlay/合法目标, await SpendResources(), 再 await OnPlayWrapper(..., isAutoPlay:false, resources). `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.GameActions.Multiplayer/ActionQueueSynchronizer.cs:146` 提供 RequestEnqueue(GameAction action), `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.GameActions/GameAction.cs:61` 提供 CompletionTask. 不应绕过支付直接调用 OnPlayWrapper 验证免费牌.

- mutable 卡与出牌链补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Combat/CombatState.cs:168-181,199-202` 的 CreateCard<T>(Player owner) / CreateCard(CardModel canonicalCard, Player owner) 执行 ToMutable -> Owner/战斗注册 -> AfterCreated. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/CardPileCmd.cs:324,341` 的 Add(CardModel card, PileType newPileType, CardPilePosition position = CardPilePosition.Bottom, AbstractModel? clonedBy = null, bool skipVisuals = false) 可将真实卡加入 Hand. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1858-1887,1915-1965` 记录每次 CardPlay 的 IsAutoPlay/PlayIndex/PlayCount, 经过 BeforeCardPlayed -> OnPlay -> History 完成 -> AfterCardPlayed, 正是进入牌不追溯及重复次数的验收位置.
- 自动与手动必须分测: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/CardCmd.cs:51,126-130` 的 Task AutoPlay(PlayerChoiceContext choiceContext, CardModel card, Creature? target, AutoPlayType type = AutoPlayType.Default, bool skipXCapture = false, bool skipCardPileVisuals = false) 以 isAutoPlay:true 进入包装器. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/PlayerCmd.cs:279` 的 EndTurn(Player player, bool canBackOut, Func<Task>? actionDuringEnemyTurn = null) 返回 void, 不能 await 此调用冒充下一回合就绪; 应等待 PlayerCombatState.Phase==Play 且 TurnNumber 增加, 参考 `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms/CombatRoomHandler.cs:114-123`.
- 异步断言风险: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.GameActions/GameAction.cs:61-63,135-148` 的 CompletionTask 完成信号不等于动作无异常, 测试还应检查 action.Exception/状态并验证具体结果, 不能只以 await 返回判通过.

- 异步故障收集已有接口: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/TaskHelper.cs:10-14,26-40` 公开 event Action<Exception> UnobservedFault, RunSafely 的非取消异常会触发事件并重新抛出. 单战斗探针应在启动测试前订阅, 任一该事件或 action.Exception 非空都判失败, 退出前解除订阅, 避免只有正常退出码却漏掉 fire-and-forget 故障.

#### 收束补充: 战斗与出牌最小调用链

确认范围为已经读取的源码接口与控制流, 以下组合没有运行或编译验证, 不是现成可执行脚本. 原生用户目录/无窗口/静音安全前置条件由主会话接手, 本研究不再扩查.

1. 测试载体只在自己的新进程和 Godot 主线程内工作, 不传 --autoslay, 不提前打开 TestMode. await NGame.Instance.GameStartupComplete 后还须确认 RootSceneContainer.CurrentScene is NMainMenu. 不能只以该 Task 完成当启动成功: G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:574-592 在错误分支也完成它.
2. 调用已确认的 NGame.StartNewSingleplayerRun(character, shouldSave:false, canonicalActs, mutableModifiers, fixedSeed, GameMode.Custom). 角色/修正必须来自当前真正已加载的 ModelDb, 不造替代类型. 此入口会建立 RunState, SetUpNewSingleplayer, 加载资源, FinalizeStartingRelics, Launch, 创建 NRun 并 EnterAct(0), 依据同文件 :1137-1143,1179-1188. ModifierModel.OnRunCreated 要求 mutable, 依据 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Models/ModifierModel.cs:73-84. GameMode.Custom 存在于 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/GameMode.cs:6-11.
3. await RunManager.Instance.EnterRoomDebug(roomType, MapPointType.Unassigned, encounter.ToMutable(), showTransition:false), 获取实际 CombatRoom. 依据 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunManager.cs:1091-1158. G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Rooms/CombatRoom.cs:80-84,122-142,197-230 显示它创建携带 run modifier 的 CombatState, 加入玩家/怪物, SetUpCombat, AfterRoomEntered, 最后 AfterCombatRoomLoaded. 不在外面再重复 SetUpCombat 或另起回合循环.
4. 等待 CombatManager.Instance.IsInProgress 且该玩家 PlayerCombatState.Phase == PlayerTurnPhase.Play. 必须有界等待并确认仍是同一战斗, 不能因 EnterRoomDebug 返回或 RoundNumber==1 就直接出牌. 该回合循环是异步启动, 依据 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Combat/CombatManager.cs:470-474; 就绪条件有 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms/CombatRoomHandler.cs:114-123 的现成参考.
5. 从真实 canonical CardModel 通过 room.CombatState.CreateCard(canonicalCard, player) 创建 mutable 卡, await CardPileCmd.Add(card, PileType.Hand), 然后按合法目标建立并排队动作. 依据 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Combat/CombatState.cs:176-181 与 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/CardPileCmd.cs:324-341.

```csharp
var action = new PlayCardAction(card, target);
RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
await action.CompletionTask;
```

6. 这三行只表示已确认的调度 API. 还要断言 action.Exception 为空, 未发生 TaskHelper.UnobservedFault, 卡牌历史确有预期开始/完成记录, 费用和形态效果符合契约. 取消/空执行或只有 CompletionTask 返回均不算通过. PlayCardAction 才保留 CanPlay/目标验证/SpendResources/完整 OnPlayWrapper; 不用直接 OnPlay 或 OnPlayWrapper 替代手动打牌. 引用本项前述 PlayCardAction.cs:62-103, GameAction.cs:61-63,135-148 和 TaskHelper.cs:10-14,26-40.
7. 自动打牌单独通过已确认的 CardCmd.AutoPlay(...) 路径测试, 不与手动免费额度混淆. 切换回合先记录 PlayerCombatState.TurnNumber, 调用返回 void 的 PlayerCmd.EndTurn(player, canBackOut:false), 再有界等待同一战斗回到 Play 且 TurnNumber 增加. 不直接修改 RoundNumber 来伪造回合开始. 依据 G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/PlayerCmd.cs:279-287.
8. Watcher 原通知链已确认到 helper 层: G:/omp works/.tmp/form-playable-20260928-01a0e7ad/watcher-WatcherCombatHelper-current.cs:26,39-65 的 internal static 类提供 Task EnterWrath/EnterCalm/EnterDivinity(Player owner, CardModel? source) 和 Task ExitStance(Player owner). :537-547 先退出再进入, 同姿态幂等; :594-621 保留 Rushdown, FlurryOfBlows, MentalFortress 和 VioletLotus 消费者. 真实原卡进入应通过上面的出牌链, 不能用自造姿态调用替代证明. 各原卡 OnPlay 本轮未逐一核对, 实际 Watcher DLL 绑定和六效果断言仍未运行.

最小新增范围仅是主会话未来的独立测试载体/失败收集/断言, 本报告没有新增产品代码或测试程序. 选择界面处理, 资源加载时序, 有界等待与自己退出仍需主会话实现并验证.

### 9. [P1] 修正可做无磁盘内存往返, 但不能冒充恢复整场战斗

- 确认时间: 2026-09-28 19:40:46.
- 触发条件: 验证每局形态修正经原生序列化后保持身份/状态, 同时避免保存到用户档案.
- 源码证据: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Models/ModifierModel.cs:139-159` 为 ToMutable(), SerializableModifier ToSerializable(), static ModifierModel FromSerializable(SerializableModifier serializable); 保存 Id 与 SavedProperties, 恢复通过 SaveUtil.ModifierOrDeprecated(..).ToMutable() 然后 Props.Fill. `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves.Runs/SerializableModifier.cs:9-32` 同时提供 JSON 字段 id/props 和网络 Serialize(PacketWriter)/Deserialize(PacketReader).
- 宣称或契约: `G:/omp works/Sts/sts2-spire1/docs/DEVELOP-form-playable-20260928.md:22,56-57` 要求修正随原生局档持久化, 战中存档/重连没有证据必须单列未知.
- 当前控制流: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunManager.cs:670-708` 的 SerializableRun ToSave(AbstractRoom? preFinishedRoom) 把 State.Modifiers.ToSerializable() 放入返回对象, 方法本身没有写文件; `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Saves/JsonSerializationUtility.cs:82-108` 的 ToJson<T>(T obj) 与 FromJson<T>(string json) 可在内存往返; `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunState.cs:291-301` 的 FromSerializable(SerializableRun save) 还原修正集合.
- 可复核命令: `Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs' -Pattern 'ToSerializable|FromSerializable|OnRunLoaded|OnRunCreated'`.
- 最小改动建议: 探针在已初始化的隔离进程中执行 ToSave(null) -> ToJson -> FromJson<SerializableRun> -> RunState.FromSerializable, 检查 Success/SaveData, 修正实际类型/Id/属性/IsCanonical=false, 并比较 modifier 开与关两组; 不调用磁盘 SaveRun 来偷换范围.
- 尚缺实机证据: 真实修正代码/资源加载, OnRunLoaded 生命周期, 跨进程恢复, 战中 Power 状态和多人协议均未实测. 仅内存往返不能解除这些边界.

- 恢复边界补充: `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Models/ModifierModel.cs:73-95` 区分 OnRunCreated 与 OnRunLoaded; `G:/omp works/Sts/sts2-spire1/research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunManager.cs:354-363,520-564` 的保存局设置走 InitializeSavedRun 并把 shouldSave 强制为 true. 内存 FromSerializable 不等于完整生命周期恢复, 也不能用 SetUpNewSingleplayer 对读回对象冒充续档.

#### 收束补充: 往返证据的终点

- 已确认的最小数据链为 RunManager.Instance.ToSave(null) -> JsonSerializationUtility.ToJson(...) -> JsonSerializationUtility.FromJson<SerializableRun>(json) -> 检查 Success/SaveData -> RunState.FromSerializable(...). 比较修正类型/Id/属性和 mutable 身份即可形成局内身份的内存断言; 本轮没有执行这条组合链.
- ToSave 方法本体不直接写文件, 不表示任意 mod 回调都已做无副作用审计. 本报告不以此保证启动/退出或其它组件的写入隔离.
- 这不证明 OnRunLoaded, 跨进程续档, 战中 Power 状态, 重连或多人同步. 不为补这些证据而在本研究中运行 SetUpSavedSingleplayer, 不启动第二个进程. 相关源码见本项既有引用, 后续由主会话接手.

## 进行中

- 2026-09-28 19:51:26 Asia/Shanghai: 按用户最新指示收束, 本研究已结束, 不再开展新检查. 前 7 项已有证据保留, 本次仅补齐第 8 项战斗/出牌最小链与第 9 项往返边界.
- 原生用户目录与无窗口启动安全已交由主会话接手. 上文增量记录中的待核对事项均表示未验证边界, 不是本研究仍在继续执行.
- 没有运行游戏/验证程序/构建, 没有操作前台或已有用户进程, 没有再次委派. 本次唯一写入仍为本报告, 不提交或推送其它文件.

## 未知

- 原生 OS.GetUserDataDir 对 APPDATA 的真实行为, 全部配置/日志/存档/崩溃数据的隔离, 无窗口/静音与前台不受影响, 本报告均未认证. 主会话负责后续研究.
- 没有已验证可安全执行的启动命令. 上文参数和控制流只是源码线索, 不构成执行授权或安全承诺.
- 第 8 项组合流程尚未编译或实机跑通. 原卡选择界面处理, 资源和原生主线程时序, 当前二进制与反编译证据的同一性, 模组初始化/绑定, 超时与自身退出均待验证.
- 六形态实际入场/退出, 手动/自动/重复打牌, Watcher 原通知消费者, 进入牌不追溯和回合边界没有本轮实测断言. 源码存在不等于行为已通过.
- 修正的内存往返流程也未在本轮执行, 更不覆盖跨进程续档/战中 Power 状态/重连/多人.
- 自定义模式入口的可见与可选, tooltip/图标/本地化, 动画和音效正确性仍需要获准后的真实运行或用户目视/听觉验收. 离线反射, TestMode 和契约桩均不能替代这些证据.
