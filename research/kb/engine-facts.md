# sts2-spire1 引擎事实速查表（KB）

> 来源：`.tmp/dllsrc/` 反编译快照 + 运行日志实证。每条都经过至少一次踩坑验证。
> 本文件是"内容"，方法流程见 skill《sts2-spire1-card-audit》。

## 卡牌模型

| 主题 | 事实 |
|---|---|
| LocalKeywords 缓存 | 私有 `_keywords` 首访后永不刷新；动态关键词需基类 `ResetKeywordCache()` 反射重置 |
| 消耗判定 | `GetResultLocationForCardPlay` 读实例 `Keywords.Contains(Exhaust)` |
| CanonicalKeywords | 必须 **public override**（protected 被引擎忽略） |
| ModelId | `Slugify(类名)` 大写蛇形；特例映射见 DEVELOP.md（CreativeAI→CREATIVE_AI 等） |
| 池注入时机 | `AddModelToPool` 首次生成池时冻结；必须早于任何池生成（MainFile.Initialize 最先跑 SharedCardReuse.Register） |
| 奖励去重 | `GetPossibleCards` 无拥有去重 |
| 升级文案渲染 | 引擎只渲染同一条 description；升级差异必须由文案表达承载：`{IfUpgraded:show:新|旧}`(ShowIfUpgradedFormatter,升级预览自动绿显)、SimpleLoc swap `-旧-+新+`(横线包旧、加号包新;UpgradeSwapRegex `(?<=^|[^/])(?:-(.+?)-|\+(.+?[^/])\+)` 按序匹配)、diff 变量 `!X!`→`{X:diff()}`(预览高亮增量)。**费用徽章**(EnergyCost.UpgradeBy)与**关键字行**(OnUpgrade 里 AddKeyword,CardModel beforeDescription 注入)引擎自渲染,文案无需差异 |
| 升级形态四分类 | costOnly(纯降费,徽章自渲染)/keyword(AddKeyword,行自注入)/numeric(UpgradeValueBy,需 diff 变量)/behavior(_all 翻转、TargetType override、空体,必须 swap)——审计工具 `.tmp/upgrade-diff-audit.mjs` 可回归,2026-09-01 修复后 0 缺陷 |
| 卡牌 ID 链 | `ModelDb.GetEntry`=StringHelper.Slugify(类名);BaseLib PrefixIdPatch(ModelDb.GetEntry Postfix)对 ICustomModel 加命名空间根前缀:`SPIRE1-`+SLUGIFY(类名)→SPIRE1-IRONCLAD/SPIRE1-BURNING_BLOOD;CharacterId(ModelId?.Entry)即此串 |

## 联机

| 主题 | 事实 |
|---|---|
| 握手三道闸 | HandshakeManager.TryReadHandshakeMessage：①版本串→VersionMismatch ②玩法mod清单→ModMismatch ③ModelID哈希→VersionMismatch；非玩法差异仅告警 |
| ModelID 哈希 | 只混入 affectsGameplay=true 的模型条目；dll 字节一致⇒同哈希 |
| 失同步假阳性 | mod 清单级差异会触发 RitsuLib checksum 弹窗但状态可逐字段全同（#563 案例）；先数 differ 标记再定责 |
| mod 加载顺序 | ModManager.Initialize 拓扑排序(manifest dependencies→优先队列,手动列表做优先级)→逐 mod TryLoadMod→LoadFromAssemblyPath→调 initializer(无 ModInitializerAttribute 则 Harmony.PatchAll)。**无依赖边的两个 mod 顺序随用户 mod 列表**——桥接类兼容需 AssemblyLoad 事件兜底(见 Interop/AutoAnthonyLoadHook) |
| 同名程序集解析 | mod dll 间编译期引用(Private=false HintPath)在运行时解析到 ModManager 已 LoadFromAssemblyPath 的同简单名实例——BaseLib NuGet-vs-gamedir 模式,AutoAnthony 桥接同此 |
| 第三方本地配置门控分歧 | Act4Heart 1.1.7 冒火精英:地图标记(SuperEliteQuest)在每端 ModifyGeneratedMapLate 里按**本地** keys_enable 生成,MapPoint.Quests **不跨网序列化**;进战 buff(se_buff RNG=seed+act 四选一:力量/金属化/再生/最大HP)同样查本地标记。双端 keys_enable 不一致→host 端多一层 power→checksum 分歧(2026-09-01 实锤:METALLICIZE_POWER_A4H:17)。其 ConfigSynchronizer 只广播 host 配置无一致性校验。规避=全队对齐 dolso.act4_heart.config |
| 虚分派即钩子面 | 全池枚举者(PrismaticGem/ColorfulPhilosophers/UnlockState.CharacterCardPools/Kaleidoscope/Splash)全部经 `character.CardPool`/`c.CardPool` 虚 getter 分派取池——patch 角色池 getter 一次即全覆盖,无旁路 |
| 自定义 GameAction 模板 | VoteForMapCoordAction 四件套：OwnerId/ActionType/ExecuteAction/ToNetAction |
| 网络化通道 | client→RequestEnqueue→host 入队→广播（ActionQueueSynchronizer）；BaseLib ICustomMessage 占 id≥128 |
| 失同步假阳性 | mod 清单级差异会触发 RitsuLib checksum 弹窗但状态可逐字段全同（#563 案例）；先数 differ 标记再定责 |

## 部署与包

| 主题 | 事实 |
|---|---|
| 构建即部署 | csproj `CopyToModsFolderOnBuild` 把 dll/json/pdb(+pck) 拷进 live mods；**清单版本号只能改源头 mod/Spire1.json** |
| pck 内容 | powers.json 等 loc 资源在 pck 里；改 loc 必须重打 pck |
| 分装包 | 三包字节一致仅 character.txt 异；stage 布局 `dist/stage/Spire1-<Pkg>/mods/Spire1/*`；打包用 PowerShell Compress-Archive（本机无 zip CLI）|
| 打 pck | `dotnet .nuget/packages/bschneppe.sts2.pckpacker/0.1.1/tools/net9.0/any/StS2PckPacker.dll "mod/Spire1/" "Spire1" "<out>"` |
| 控制台 | modded 运行必开（NDevConsole.cs:359）；BaseLib 自动注册 AbstractConsoleCmd 子类 |

## 日志取证

| 主题 | 事实 |
|---|---|
| 位置 | `C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/logs/godot.log`（最新）+ 时间戳轮转件 |
| 时效 | 轮转会丢文件——冻结/异常后**先拷 logs 再杀进程** |
| RitsuLib 转储 | divergence zip 内 state-divergence-report.txt；differ 标记行数=真实差异字段数 |
| 进程检查 | 勿用 `tasklist //FI`（静默假0）；用 PowerShell `@(Get-Process SlayTheSpire2).Count` |
