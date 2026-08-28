# 游戏侧 Mod 资源盘点（inventory-mods.md）

> 调研日期：2026-08-28。只读盘点：本地 `G:/steam/steamapps/common/Slay the Spire 2/mods/` + 工坊 `G:/steam/steamapps/workshop/content/2868840/`。
> 证据源：各目录 manifest json、文件 md5/字节校验、当日 `godot.log`（`C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/logs/`）、引擎反编译源 `research/engine-dllsrc/`、GitHub API。
> 运行态基线：`godot.log` L695 `--- RUNNING MODDED! --- Loaded 30 mods (35 total)` —— 35 注册 = 28 工坊订阅 + 7 本地；30 在载（RegentFX 用户禁用 + 双源去重禁 4，见 §4）。
> 维护：mod 安装/订阅变化时刷新；表内 dll/pck 为精确字节数。

## 1. 本地 mods/ 清单（7 个 mod + 1 个惰性 zip）

| id | 名称 | 版本 | 来源/性质 | gameplay | dll (B) | pck (B) | 依赖 |
|---|---|---|---|---|---|---|---|
| Spire1 | Spire1 | 0.9.1 | **本项目构建部署**（md5 `5e0083d5acc68c7a31357c228ea7de73`，与部署校验值一致；dll 内命名空间 `Spire1.Spire1Code.*`） | 是 | 1,021,440 | 30,942,206 | BaseLib ≥3.4.5 |
| ActsFromThePast | Acts from the Past | 1.0.5 | **本项目 fork 部署**（md5 `c2c49c620fce7c5f94d3ddba6046cbd5`；联机修复版：本地 dll 比 ws 版多 `INetGameService`/`NetGameType`/`get_RebalancedModeEffective` 符号；pck 与 ws 版字节一致） | 是 | 1,017,856 | 110,327,596 | BaseLib ≥v3.3.6 |
| BaseLib | BaseLib | v3.4.5 | 上游官方本地镜像（与工坊 3737335127 逐字节一致，**非 fork**，详 §5） | 否 | 1,090,560 | 131,880 | 无 |
| Sts2RichPresenceFix | Steam 详细状态修复 (Rich Presence Fix) | 1.0.0 | 第三方本地安装（leddele，仅本地无工坊副本） | 否 | 12,288 | — | 无 |
| silentSkin | a大猎手娘化 | 0.0.1 | 第三方管理器批量目录 `slaysp2manager-batch-93738c6b-*`（旧版；工坊 0.7.1 已退订成磁盘残留） | 否 | — | 4,991,919 | 无 |
| RegentFemPortraits | 尖塔储君卡图娘化 | v0.6.0 | 第三方旧版本地副本（工坊 v1.0 更新但双源判负，见 §4） | 否 | 24,576 | 48,593,496 | 无 |
| Mesugaki | Mesugaki_Regent | 0.1.1 | 第三方旧版本地副本（工坊 0.1.2 更新且胜出，**本地已被引擎禁用**，见 §4） | 否 | 5,632 | 2,988,140 | 无 |

- `Spire1/character.txt` = `all\n`（4 字节，三角色全启用 flavor）。
- `Spire1/Spire1.pdb`（319KB）随构建部署，无害。
- `mods.zip`（59.8MB，约 2 个月前）为**惰性历史归档**：引擎仅递归扫描 json/dll/pck（`ModManager.ReadModsInDirRecursive`，ModManager.cs L399-422），zip 不参与加载。内含 BaseLib/Mesugaki/RegentCardsAnimeRework/SpeedX/typing/BetterSpire2Lite/DamageMeter/ModConfig 等旧版，及 chujunkamian1、FogboundPaths、PaelGirl、qload、quickRestart2、RelicFold、RemoveMultiplayerPlayerLimit、MultiplayerSaveSlots、MonsterPredictorLite、ModListSorter 等已不在载的历史 mod。

## 2. 工坊 content/2868840 清单（34 目录 = 28 订阅在载 + 6 退订残留）

订阅口径：`godot.log` L712 `Scanning subscribed Workshop items. Reported=28, Returned=28`。磁盘 34 − 28 = 6 个退订残留（引擎不扫描，Steam 未清理）。

### 2.1 订阅在载（28）

| workshopId | id | 名称 | 版本 | gameplay | dll (B) | pck (B) | 依赖 | 备注 |
|---|---|---|---|---|---|---|---|---|
| 3737335127 | BaseLib | BaseLib | v3.4.5 | 否 | 1,090,560 | 131,880 | 无 | 双源判负→本地胜（§4） |
| 3746969593 | ActsFromThePast | Acts from the Past | 1.0.5 | 是 | 1,125,888 | 110,327,596 | BaseLib ≥v3.3.6 | 双源判负→本地 fork 胜（§4） |
| 3747497501 | RegentFX | 万象辉星[RegentFX] | 0.5.1 | 否 | 245,760 | 60,716,376 | 无 | **用户 settings 禁用**（log L79） |
| 3747526103 | JmcModLib | JmcModLib | 1.9.0 | 否 | 19,456 | 998,408 | 无 | 另含 Runtime.dll 516,096 + Newtonsoft.Json 723,368 + dispatch/ 引导源码 |
| 3747526116 | Watcher | Watcher | 0.9.24 | 是 | 478,208 | 16,448,756 | 无 | min_game 0.111.0；75 个 Harmony 补丁 |
| 3747528152 | intentgraph2 | Intent Graph | 1.5.1 | 否 | 7,168 | 428,124 | 无 | 另含 core 306,176×2（含 lt-0.110.0 旧版）、baselib 8,192、ritsulib 14,336、Antlr4 192,000 |
| 3747531469 | BetterSpire2Lite | BetterSpire2 Lite | v1.83.10 | 否 | 134,144 | — | 无 | 附 BetterSpire2_localization/ 14 语言 .lang |
| 3747537811 | Act4Heart | Act 4 Heart | 1.1.7 | 是 | 120,832 | 15,727,048 | 无 | min_game 0.109.0；不可中途禁用 |
| 3747554236 | typing | sts2_typing | 0.0.6 | **是** | 110,080 | — | 无 | 联机聊天却标 gameplay，进 MP 握手清单 |
| 3747557003 | ModConfig | 皮皮配置: ModConfig | 0.2.3 | 否 | 54,784 | 326,604 | 无 | 通用配置框架 |
| 3747557283 | DamageMeter | 皮皮统计: Skada | 1.14.7 | 否 | 501,760 | 454,644 | 无 | |
| 3747557357 | SpeedX | 皮皮极速: SpeedX | 0.11.11 | 否 | 182,272 | 361,448 | 无 | |
| 3747557762 | Rewind | 皮皮倒带: Rewind | 0.26.18 | 否 | 385,536 | 365,784 | 无 | |
| 3747597614 | necrobinderSkin | necrobinderSkin | 0.9.1 | 否 | — | 7,065,566 | 无 | 纯 pck 皮肤 |
| 3747602295 | STS2-RitsuLib | RitsuLib | 0.5.17 | 否 | 33,792 | — | 无 | loader + lib/{0.107.1,0.109.0,0.110.0,0.111.0} 各 ~8.59MB runtime dll（本机选 0.111.0→0.5.15，log L155） |
| 3747606660 | STS2-ShowPlayerHandCards | Show Player Hand Cards | 0.6.3 | 否 | 100,864 | — | RitsuLib ≥0.2.27 | |
| 3747606792 | STS2-MultiPlayerPotionView | Multiplayer Potion View | 0.3.3 | 否 | 63,488 | — | RitsuLib ≥0.2.27 | |
| 3747606832 | STS2-MultiplayerLimitBreak | Multiplayer Limit Break | 0.2.7 | 是 | 139,776 | — | RitsuLib ≥0.5.4 | 16 人联机 |
| 3747626826 | QuickSlAndRerollStart | 多功能快速SL（Better Menu） | v0.0.0 | 否 | 24,064 | — | 无 | 附 lib/{0.107.1,0.111.0} runtime dll 各 24,064 |
| 3747751411 | RegentFemPortraits | 尖塔储君卡图娘化 | v1.0 | 否 | 24,576 | 63,826,568 | 无 | 双源判负（版本解析失败）→本地 v0.6.0 胜（§4） |
| 3747764087 | CardTracker | CardTracker | v1.5 | 否 | 39,424 | — | 无 | |
| 3747793307 | RelicTracker | RelicTracker | v1.25 | 否 | 112,128 | — | 无 | 附 Localization/ 10 语言 .loc |
| 3748603697 | Mesugaki | Mesugaki_Regent | 0.1.2 | 否 | 15,872 | 4,522,820 | 无 | **双源胜出**：本地 0.1.1 被禁（§4） |
| 3783173082 | STS2-MesugakiRegentSkinFix | Mesugaki Regent Skin Fix | 0.1.1 | 否 | 20,992 | — | RitsuLib ≥0.4.13；Mesugaki ≥0.1.2 | |
| 3785039319 | ActsFromThePastMultiplayerBalance | AFTP Multiplayer Balance | 0.0.1 | 是 | 31,232 | 267,152 | ActsFromThePast ≥1.0.5 | |
| 3787753911 | LieRenTVmod | LieRenTVmod | v0.1.2 | 否 | — | 27,394,668 | 无 | 纯 pck 角色；选人界面资源有 UID 失效告警（log L1209-1211） |
| 3787796638 | ActToggler2 | Act Toggler 2 | v1.0.0 | 是 | 22,528 | 11,600 | BaseLib ≥3.4.0 | |

### 2.2 退订残留（6，磁盘存在但引擎不加载）

| workshopId | id | 名称 | 版本 | gameplay | dll (B) | pck (B) |
|---|---|---|---|---|---|---|
| 3747591649 | silentSkin | silentSkin | 0.7.1 | 否 | — | 4,991,919 |
| 3747626664 | RegentCardsAnimeRework | RegentCardsAnimeRework | v1.0 | 否 | 48,640 | 34,682,076 |
| 3748864970 | AnimeWaifuSilent | AnimeWaifuSilent | v1.4.0 | 否 | 26,112 | 37,168,356 |
| 3770163208 | PerfectedStrikeAnime | Perfected Strike: As Depicted | 0.3.2 | 否 | 34,304 | — |
| 3772619739 | BulletTimeAnime | Bullet Time: As Depicted | 0.3.3 | 否 | 66,048 | — |
| 3774274248 | MeleeAttack | 近身攻击 | 1.0.2.1 | 否 | 226,816 | 1,813,488 |

## 3. 加载顺序与禁用汇总

- 重排序后加载序（log L84-110）：DamageMeter → ModConfig → typing → SpeedX → RelicTracker → **RitsuLib** → MultiPlayerPotionView → necrobinderSkin → CardTracker → ShowPlayerHandCards → intentgraph2 → Sts2RichPresenceFix → BetterSpire2Lite → MultiplayerLimitBreak → JmcModLib → silentSkin → LieRenTVmod → Act4Heart → Watcher → QuickSlAndRerollStart → Rewind → Mesugaki → MesugakiRegentSkinFix → **BaseLib** → MaxHpSizeMod → **Spire1** → **ActToggler2** → **ActsFromThePast** → **AFTP-MPB** → **RegentFemPortraits**。
- 依赖序成立：BaseLib(23) 先于 Spire1(25)/ActToggler2(26)/AFTP(27)；RitsuLib(5) 先于其 4 个下游。
- 5 个未加载：RegentFX（settings 禁用，log L79）+ AFTP-ws / BaseLib-ws / RegentFemPortraits-ws（双源重复）+ Mesugaki-local（双源旧版）。
- 在载 gameplay mod 共 8 个（MP 握手受影响）：Spire1、ActsFromThePast、AFTP-MPB、ActToggler2、Watcher、Act4Heart、MultiplayerLimitBreak、typing。

## 4. 双源（本地+工坊同名）解析规则

引擎实现：`research/engine-dllsrc/MegaCrit.Sts2.Core.Modding/ModManager.cs` `RemoveDisabledMods`（L340-380）。流程：先查 settings 禁用 → 本地目录 mod 按 id 建索引 → 工坊 mod 同 id 者按版本比较：

1. **任一侧版本解析失败** → "unknown version" → 工坊版禁用，**本地胜**（L365-368）
2. **版本相等** → 工坊版禁用，**本地胜**（L373-377）
3. **工坊版本更大** → 本地版禁用，**工坊胜**（L369-372）
4. **本地版本更大** → 工坊版禁用，本地胜（L378 以下）

实测四例（`godot.log` L80-83，多份轮转日志一致）：

| mod | 本地 | 工坊 | 判定分支 | 日志行 | 实际加载 |
|---|---|---|---|---|---|
| ActsFromThePast | 1.0.5（fork） | 1.0.5 | 相等→本地胜 | L80 | 本地 fork dll（L638） |
| BaseLib | v3.4.5 | v3.4.5 | 相等→本地胜 | L82 | 本地 dll（L592） |
| RegentFemPortraits | v0.6.0 | v1.0 | 工坊版本串 `v1.0` 两段式**解析抛异常**→按 unknown 处理→本地胜 | L83 | 本地 v0.6.0（L650；RitsuLib mod-list dump 亦确认 v0.6.0） |
| Mesugaki | 0.1.1 | v0.1.2 | 工坊更大→**工坊胜，本地禁用** | L81 | 工坊 dll（L575） |

**重要更正**：并非"同名一律本地胜"。规则是版本比较——相等/不可解析时本地胜；**工坊更新时工坊胜**。当前 Mesugaki 实际运行的是工坊 0.1.2（本地 0.1.1 已被引擎禁用）。`dist/friends-pack/README-安装说明.txt` L19"本地版会自动优先"对 AFTP 成立（版本相等），但作为通用表述不准确：上游一旦发新版而本地未同步，工坊版会反超（Mesugaki 即实例；对 AFTP fork 而言上游 1.0.6+ 也会反超）。

**版本解析陷阱**（`research/engine-dllsrc/MegaCrit.Sts2.Core.Debug/SemanticVersion.cs`）：`FromString` 仅接受三段式 `Major.Minor.Patch`（可带 `v` 前缀 L66-69、`-` 预发布、`+` 元数据）；两段式如 `v1.0` 在收尾 switch 落入 default 抛 `InvalidOperationException`（L143-163），`TryFromString` 捕获后返回 false → 双源场景按 "unknown version" 判工坊负。当前受影响：RegentFemPortraits（工坊 v1.0 永远输给任何可解析的本地版本）。

## 5. BaseLib 特别调查

**版本与字节等价性**
- 本地 `mods/BaseLib` 与工坊 `3737335127/BaseLib` **逐字节一致**：dll md5 `4380fd038fda7ca92708fd09a8aebf39`（1,090,560 B）、pck md5 `ceed43342d19ef116880e18e5e0682f4`（131,880 B）、manifest 相同（271 B，CRLF）。本地副本是镜像非 fork；双源相等 → 本地路径加载（log L592-593）。
- manifest 字段：id=BaseLib / version=**v3.4.5**（符合预期 3.4.5）/ author=Alchyr / affects_gameplay=false / dependencies=[] / min_game_version=0.107.1。

**部署 DLL 的构建锚点**
- dll 内嵌 `AssemblyInformationalVersion = 3.4.5+4a97642d7843309cdf35c46a11e3f46132cee049`（版本+git commit）。
- GitHub API 实证：`4a97642d` = "fix patches"（Alchyr，2026-08-14T00:49:34Z）；上游 tag `v3.4.5` → `22757933ba`（"Merge pull request #379"，2026-08-14T01:01:51Z）；提交链 `22757933` ← `d821b6ef1c`（"3.4.5" 版本号提交）← `867bcbc034`（"make large image patch conditional"）← `4a97642d`。
- 结论：**部署 DLL 构建于 v3.4.5 tag 前 3 个提交**；tag 比它多 1 个功能提交（867bcbc034 大图补丁条件化）+1 个版本号提交。作者先构建上传工坊、后打 tag。

**与 `research/BaseLib-StS2` 源码树的关系**
- research 树是上游 master 的 **shallow clone**（`.git/shallow` = `22757933`；reflog：2026-08-18 clone 自 github.com/Alchyr/BaseLib-StS2），HEAD=`22757933` = **v3.4.5 tag 提交本身**。
- 判定：**同一 3.4.5 发布周期、非同一构建点**——部署二进制 = `4a97642d` 构建；源码树 = tag 提交（晚 2 个提交，含 1 处功能差异）。若将来从 research 树重建 BaseLib，行为会比当前部署版多 867bcbc034 的"大图补丁条件化"一处差异，二进制不等价。
- 两边 `BaseLib.json` 内容一致（仅行尾 LF vs CRLF，270 vs 271 字节）；research 树无构建产物（build/ 仅 BaseLib.props，无 publish/）。

**源码结构**（262 个 .cs；csproj：net9.0 / Godot.NET.Sdk 4.5.1 / LangVersion 14 / Krafs.Publicizer 公开化 sts2.dll / 构建即拷贝到 mods/）

| 目录 | .cs 数 | 内容要点 |
|---|---|---|
| Abstracts/ | 43 | 接口与基类：ICustomPower、ICustomModel、ILocalizationProvider、ConstructedCardModel(16KB) 等 |
| Utils/ | 51 | SpireField(20KB)、WhatMod、SavePatchUtils、WeightedList、ReflectionUtils + Patching(14)/NodeFactories(7)/ModInterop(2)/Attributes(1) |
| Patches/ | 74 | 引擎补丁按域分：UI(23)/Localization(11)/Content(10)/Features(7)/Utils(7)/Hooks(4)/Compatibility(3)/Fixes(3)/Audio(2)/Saves(2)/Networking(1) + PostModInitPatch.cs |
| Hooks/ | 11 | HealthBarForecast 注册表与接口群（IModifyScryAmount、IMaxHandSizeModifier 等）+ BaseLibHooks |
| Extensions/ | 31 | Type/Player/Power/RelicModel 等扩展方法 |
| Config/ | 20 | SimpleModConfig(30.8KB)/ModConfig(23KB)/ConfigAttributes(15KB)/BaseLibConfig + UI(13) |
| BaseLibScenes/ | 7 | NLogWindow/NRewardHighlight/NCustomLinkedRewardSet/NHorizontalScrollContainer + Acts(2) |
| 其余 | 25 | Cards(10，含 Variables)/Common(6，Rewards)/Commands(2)/ConsoleCommands(2)/Diagnostics(2，HarmonyPatchDump)/Monsters(1，MoveBuilder)/Audio(1，ModAudio)/根 BaseLibMain.cs |
| BaseLib/（数据侧，pck 源） | — | scenes 3 个 tscn（LogWindow/linked_reward_set/dynamic_background）、localization 7 语言（deu/eng/ita/jpn/kor/rus/zhs）、images、mod_image.png(4.7KB) |

## 6. 其他备注

- `known-zips.txt` 交叉引用：6 个 zip 全部为本项目 RitsuLib state-divergence 调试转储（`.tmp/watch/`，2026-08-24/27），对应工坊 3747602295（RitsuLib）联机分歧排查；无其他工坊 id 交叉。
- 缺 `min_game_version`（日志告警 "Assuming that it is supported"）：DamageMeter、ModConfig、Rewind、Mesugaki、RegentFemPortraits、silentSkin、LieRenTVmod。
- RitsuLib 变体分发：loader v0.5.17 按宿主版本选 `lib/<ver>/STS2-RitsuLib.dll`（本机 v0.111.0 → runtime 0.5.15，log L155）；manifest 声明 0.5.17 但实际运行 0.5.15，看版本以运行时为准。
- 唯一 settings 级禁用：RegentFX（log L79）。
