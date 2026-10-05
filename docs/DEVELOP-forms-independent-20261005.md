# 姿态形态独立 mod 契约 - 2026-10-05

## 1. 目标

把当前已在 Spire1 内可玩的 Forms 内容拆成独立加载的 `Forms` mod.

硬目标:

- `Forms.dll` 不产生 `Spire1.dll` 的 AssemblyRef.
- `Spire1.dll` 不产生 `Forms.dll` 的 AssemblyRef, TypeDef 或 manifest 前置项.
- Forms 在 `BaseLib + Watcher + Forms` 下可独立初始化和运行.
- Forms 缺 Watcher 时只禁用自己的选项和桥接, 不阻塞 Spire1 或其它 mod.
- Spire1 缺 Forms 时仍可初始化, 普通 Spire1 姿态和内容不因 Forms 缺失而失效.
- Watcher 和 Forms 晚加载, 重复初始化, 绑定失败回滚, 程序退出均有明确的 fail-closed 语义.
- 形态 power 和 modifier 的旧 ModelDb 身份继续使用 `SPIRE1-*` CustomID, 不因新程序集和 namespace 改名而丢旧档解析身份.

本契约不宣称视觉 UI, 长战斗, 战中存档, 多人同步或朋友机器验收已完成. 新字节必须重新构建并单独验证.

## 2. 项目和发布边界

### 2.1 新项目

独立项目根:

`G:\omp works\Sts\sts2-forms`

项目文件:

- `Forms.csproj`
- `Forms.json`, id 固定为 `Forms`
- `project.godot`, assembly name 固定为 `Forms`
- `Forms/` 资源根
- `FormsCode/` C# 源码
- `localization/eng` 和 `localization/zhs`
- `DEVELOP.md` 和 `DEVLOG.md`

构建基础沿用已验证的 Godot.NET.Sdk 4.5.1, net9.0, BaseLib 3.4.5, sts2, 0Harmony, ModAnalyzers 和 PckPacker. Forms manifest 只声明 BaseLib. Watcher 仅运行时反射探测, 不写进 csproj 和 manifest.

### 2.2 Spire1 项目

`G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` 默认移除 `Spire1Code\Forms\**`, `FormNativeSmokeRunner.cs` 和 `FormNativeSmokePatch.cs`.

Spire1 只保留普通姿态, 普通 cards/powers 和一个不引用 Forms 的兼容入口:

- `StanceCmd` 通过反射定位 `Forms` 的公开兼容入口.
- Forms 不存在或入口签名不匹配时, 兼容入口报告 disabled, 普通 Spire1 路径继续工作.
- Powers gate 不再按 Forms namespace 或 Forms 类型做编译期例外. 独立 Forms 程序集的 powers 不属于 Spire1 powers gate.
- 原生产 Spire1 不再包含 Forms smoke runner. smoke runner 后续迁移为独立工具或 Forms 测试项目, 不进入任何朋友发布包.

### 2.3 稳定反射协议

Spire1 只按类型全名和方法签名反射, 不引用 Forms 程序集. Forms 侧提供:

- `Forms.FormsCode.Interop.FormsRuntimeEntryPoint`
- `public static bool IsAvailable`
- `public static bool IsSelected(Player player)`
- `public static int KindOf(Type stanceType)`
- `public static int CurrentKind(Player player)`
- `public static Task Enter(PlayerChoiceContext ctx, Player player, Type nativeStanceType, CardModel? source)`
- `public static Task Exit(PlayerChoiceContext ctx, Player player, CardModel? source)`

返回值 `0` 表示 None, `1` Calm, `2` Wrath, `3` Divinity. 缺少类型, 缺少方法, 参数或返回值漂移均为兼容层 disabled, 不抛到普通 Spire1 入口.

Forms 可选探测 Spire1 的兼容通知入口, 但不依赖它才能初始化:

- `Spire1.Spire1Code.Interop.FormsCompatibilityEntryPoint`
- `public static Task Dispatch(Player player, PlayerChoiceContext ctx, int previousKind, int nextKind)`

若不存在, Forms 仍保留 Watcher 原生通知和自己的效果生命周期, 只跳过 Spire1 自有 `IOnStanceChanged` 消费者.

### 2.4 命名和身份

Forms 源码可使用独立 namespace, 推荐 `Forms.FormsCode` 和 `Forms.FormsCode.Powers`.

下列 10 个类型必须显式标注 `CustomIDAttribute`, ID 保持不变:

- `SPIRE1-VOID_SERPENT_STANCE_POWER`
- `SPIRE1-DEMON_REAPER_STANCE_POWER`
- `SPIRE1-ECHO_CELESTIAL_STANCE_POWER`
- `SPIRE1-VOID_FORM_EFFECT_POWER`
- `SPIRE1-SERPENT_FORM_POWER`
- `SPIRE1-DEMON_FORM_POWER`
- `SPIRE1-REAPER_FORM_EFFECT_POWER`
- `SPIRE1-ECHO_FORM_EFFECT_POWER`
- `SPIRE1-CELESTIAL_FORM_POWER`
- `SPIRE1-FORM_STANCE_MODIFIER`

独立资源路径使用 `res://Forms/...`. 本地化 key 可继续使用已有 `SPIRE1-*` 身份, 但新资源文件必须只进入 Forms.pck. 不复制未知多语言资产.

## 3. 生命周期契约

Forms Watcher bridge 使用进程内状态机:

`Unbound -> Retryable -> Installing -> Bound`

确定性签名不兼容, 部分回滚失败或检测到旧程序集身份冲突时进入 `Terminal`.

收到 ProcessExit 或等价退出信号后进入 `ShuttingDown`.

不变量:

- `IsAvailable` 只在 `Bound` 为 true.
- Watcher 未加载, ModelDb 尚未可用或程序集仍可能晚加载时为 `Retryable`, 可由 AssemblyLoad 通知唤醒.
- AssemblyLoad 只发通知, 不在任意线程直接访问 Godot 或安装 Harmony. 真正绑定在后续已知主线程入口执行.
- Installing 中任何一步失败, 先撤销本 Harmony owner 的已安装 patch, 清空 delegate 和程序集引用, 再按失败类型进入 Retryable 或 Terminal.
- 二次 ModelDb.Init, 重复 TryBind 或同进程重载不会叠加 patch, 不复用已卸载程序集的 MethodInfo.
- ShuttingDown 后禁止新绑定, 取消 AssemblyLoad 订阅, 清空所有静态 delegate 和 native marker 引用; patch 回调统一立即返回.
- `FormStanceModifier` 在选择新局和加载旧局时必须在桥不可用时显式失败, 但缺 Forms 本身不能让 Spire1 初始化失败.

## 4. 资源和测试工具

Forms 需要自己的 `Forms.pck`, 只包含 Forms 资源和本地化. Spire1.pck 不再包含 Forms 图片和 localization.

原 `FormNativeSmokeRunner` 不进入 Spire1 或 Forms 玩家生产 DLL. 迁移后的测试 runner 必须单独项目或明确测试编译常量, 默认发布构建不编译它.

## 5. 交付门禁

源码改动后由主会话集中执行, 子代理不构建:

1. Forms 和 Spire1 各自 Release 构建, 输出固定在 `G:\omp works\.tmp\forms-independent-20261005\`.
2. 两个 DLL 的 AssemblyRef, manifest 和 TypeDef 检查.
3. Forms.dll 不得出现 `Spire1` AssemblyRef; Spire1.dll 不得出现 `Forms` AssemblyRef 或 Forms TypeDef.
4. 两个 PCK 的资源根和本地化路径集合检查.
5. `BaseLib + Forms`.
6. `BaseLib + Watcher + Forms`.
7. `BaseLib + Spire1`.
8. `BaseLib + Watcher + Spire1`.
9. 全组合启动和至少一个新字节的 headless 运行证据.

在新字节通过前, 不覆盖正式 `workshop\content\Spire1`, 不写 Steam 安装, 不改共享 `mod_configs`, 不上传 Workshop.

## 6. 未知和停止条件

以下仍然未知, 不得用静态审查代替:

- 独立 Forms 程序集被 ModelDb 发现和实例化的实际时序.
- Watcher 晚加载后 AssemblyLoad 到主线程绑定的真实路径.
- 独立 carrier 与 Spire1 的原有 IOnStanceChanged 消费者之间的兼容完整性.
- Forms 旧档和联机身份在独立程序集下的真实解析.
- 新 Forms PCK 视觉资源在实际 UI 中显示.

若独立项目不能在不引用 Spire1 的前提下编译, 不允许留下复制的死类型或 stub 冒充完成; 应在 DEVLOG 记录阻塞点并停在可恢复断点.

## 2026-10-05 接续增量契约

- 独立仓库实际采用标准 repo/mod 布局: G:\omp works\Sts\sts2-forms\mod\Forms.csproj, FormsCode 和 Forms 资源根均位于 mod 下. 第2.1节的平面列表仅表达项目内容, 不要求仓根就是Godot项目根.
- 显式关闭并移除默认自动部署. 本轮所有DLL/PCK和中间文件仅写 G:\omp works\.tmp\forms-independent-20261005, 新源码不覆盖 canonical Release 或 Workshop staging. 本局已选模式而运行桥不可用, IsSelected/Enter/Exit 任一环节均须明确失败, 不静默吞掉切换或退回普通规则.
- 重加载保证分层: 重复初始化/重复ModelDb.Init/一次程序集身份的晚加载应幂等. 已就绪菜单后的AssemblyLoad必须由持续且有界的主线程消费者唤醒. 真正程序集替换或同simple name多程序集在未证明ModelDb支持重绑定前须fail closed, 清空旧缓存, 明确需要重启进程, 不宣称热替换已可用.
- Shutdown与Terminal须在撤patch前发布关闭状态; 已进入的async callback及await后续以绑定代数核对, 不访问已清空或已换代的native引用. 初始化自身Harmony class安装失败必须回滚本owner且不能继续发布Bound. 正常非Forms对局的原生语义不能被生命周期门禁误吞.
- 两侧完全无AssemblyRef是构建门禁; 10个旧CustomID只是身份保留, 不等于战中读档或多人已经验证. 普通Spire1仍使用的共享姿态图标可以保留, Forms专属modifier/power本地化不得重复注册到Spire1生产包.
- 主会话不依据过期的2026-10-03 15:30前台窗口操作用户桌面或启动可见游戏. 后续独占无窗口烟测仍须限定隔离非Steam载体/新APPDATA/GSE/TEMP并绑定本轮新字节;不能用r15旧报告覆盖.

## 独立存档安全面增量契约 (2026-10-05T07:50:34.6257455+08:00)
- 旧raw Forms modifier保护属于独立安全面, 不受Spire1内容池可用性开关支配. 在最早Initialize入口显式安装并由Harmony精确证明; Phase3通用扫描跳过已显式安装的唯一guard类型, 重复Initialize不得叠加.
- 内容不可用分支和Phase1异常不得被当作Forms存档保护已生效的替代证据. 显式安装/证明失败必须如实报告未闭合, 不静默降低已选Forms规则.
- 两个mod均缺失仍无本保护代码可执行; 当前工作不宣称覆盖此环境.

## 2026-10-05 战中绑定丢失验收增量

- R10源码审查确认战中Terminal/Shutdown撤patch后会放回原生规则, 尚待实际复现. 在此面关闭前不把独立包称为重加载已验收.
- 先用r5生产字节和新增test-only载体复现已选局的下一张牌/姿态变更, 再改产品并用同一载体复测. 超时或未执行不能算拒绝.
- 修复须有不依赖Watcher旧程序集delegate且不因其patch清理而失效的已选局外层保护, 非Forms局继续原生规则. 明确带重启原因的拒绝且无原生副作用是安全门, 不宣称真正热替换.

## 2026-10-05 持续战中保护增量契约 (待实现/待验收)

依据: runtime-loss-audit-r10, stable-guard-api-scout-r15, cleanup-guard-scout-r17. r17标题与最小建议在IsEnding单独判据上有不一致, 以权威引擎IsCurrentLiveCombat/IsInProgress组合为准, 不采用只看IsEnding的判定. 所有该面结论目前是源码证据; 先修测试原始marker证据并用r5真实复现, 然后才进入生产实现.

1. 本局规则身份只能来自RunState.Modifiers中的FormStanceModifier. Watcher桥可用性不是规则切换开关. 本局已选而桥Terminal/ShuttingDown/pending不可用时, 不得继续支付/出牌/原生变姿态/伤害落地并静默回退原生倍率.
2. 保护必须在Watcher桥的patch撤销后仍存活. 优先使用FormStanceModifier的原生model hooks覆盖支付前ShouldPlay, 伤害落地前, power amount修改前与回合边界. BeforeCardPlayed已在支付之后不能冒充支付前防线. 精确签名以本机当前引擎源码为准, 不造API或stub.
3. 原生marker的PowerCmd.Remove没有amount-change hook, 须有仅引用稳定引擎/本项目类型的独立进程生命周期安全owner, 名称Forms.FormStanceSafety. 不持有Watcher Assembly/MethodInfo/delegate, 只按权威FullName识别marker. MainFile常规扫描必须跳过此显式安装类型; 重复Initialize不叠patch, 精确目标/owner/prefix身份由Harmony元数据证明. 安装/证明失败不能发布Bound.
4. live判定不使用CombatState.IsLiveCombat(), 该实现恒true. 至少同时检查当前CombatState, CombatManager.IsCurrentLiveCombat(combat id)或等价的当前identity+IsInProgress, 并排除IsEnding. 无本局modifier, 非当前战斗, 已结束/正在结束或拆除的清理路径必须放行, 不截断正常teardown. 未证明的quit专用路径仍写未知.
5. MainFile.Shutdown/桥Terminal会撤两个原owner并清Watcher动态引用, 但安全owner故意驻留至进程退出, 不属于可热卸载对象. 已选局下一实际动作须有明确Forms不可用与重启进程的异常理由, 无原生副作用; timeout/pending/无故障cancelled不是安全通过. 不承诺异常自动将用户送回菜单或整局终止; 交互ActionExecutor可能记录异常后继续队列, 故必须检查真实action执行/完成/fault与日志排空.
6. 生命周期验收需分开验证两个原owner为0和独立安全owner精确仍在PowerCmd.Remove上且不重复. 实机覆盖独立/同挂正常玩法, terminal与shutdown已选局两张实际牌被拒绝且无能量/HP/marker变化, 以及不选Forms时保留原生行为. 核心remove prefix与伤害/回合边界需独立证据, 不能只以卡牌拒绝替代全部路径验收.
7. 新字节集中Release构建, 结构门禁, 无窗口隔离烟测, 交叉启动与精准Git备份完成后才生成独立Beta. 不写Steam/共享mod_configs/C:/canonical Release/Workshop/另一发布会话脚本. 未验收UI/长战斗/跨进程读档/多人/真正热替换/性能仍明确保留.