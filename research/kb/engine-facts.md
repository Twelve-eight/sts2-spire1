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

## 联机

| 主题 | 事实 |
|---|---|
| 握手三道闸 | HandshakeManager.TryReadHandshakeMessage：①版本串→VersionMismatch ②玩法mod清单→ModMismatch ③ModelID哈希→VersionMismatch；非玩法差异仅告警 |
| ModelID 哈希 | 只混入 affectsGameplay=true 的模型条目；dll 字节一致⇒同哈希 |
| 地图放行 | RunState 无"房间完成"字段；`NMapScreen.IsTravelEnabled` 各端本地门控；胜利路径=`SetTravelEnabled(true)`（CombatManager L1341-1343） |
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
