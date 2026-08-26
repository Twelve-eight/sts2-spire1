# DEVLOG 结论推倒重验 —— 覆盖/冒烟/知识库/联机/第三方互操作（ReVerifyClaims，2026-08-26）

方法：不信任任何已写结论，只采信四类硬证据——当前代码（HEAD=6ba5c8d）、引擎反编译源（`.tmp/dllsrc/`）、git 历史、`.tmp/p1-smoke/` 归档日志与游戏 logs 目录原始产物。覆盖统计用**自写解析器**独立重算（锚定 class 声明正则 + `[Pool]` 继承链解析 + `SharedCardReuse.cs` 复用通道 + 全量日志 `Playing (\S+)` 集），id 归一化剥离分隔符后比较，**规避 coverage.js 的 THUNDERCLAP 蛇形化 bug**；未运行 coverage.js 本体。

---

## A. 冒烟声明

### A1. heart5 exit 0（第四幕全程自动 → 心脏斩杀 → 建筑师处决 → Victory）— ✅ 成立
- 实测 `autoslay-heart5.log`：L12525 `[AutoSlay] Action: Victory! Run completed and returned to main menu`、L12528 `Run completed successfully with seed=P1SMOKE1`；damagemeter 归档段 `CORRUPT_HEART_BOSS (4 turns)`；watchdog 击杀 / quit(1) 命中 **0**。
- 口径注记：归档日志不含进程退出码本身；"exit 0" 由 AutoSlay 成功收尾消息 + Godot 正常退出清理佐证，无法逐字节直读。日志内 [ERROR]×2 = `Act 4 is not yet implemented.` / `EpochModel was not found :(`，均为引擎提示噪音。

### A2. P1SMOKE3 IRONCLAD 首胜（战果 #6 回归局）— ✅ 成立（附两点偏差）
- 实测 `autoslay-p1smoke3.log`（mtime 08-23 18:54，即修复 commit 3deabac 后的回归局）：embark `SPIRE1-IRONCLAD`；L12570-12571 Victory 双行；**[ERROR]=0、exception=0**（历史最干净属实）；ROOM_FULL_OF_CHEESE 事件「大快朵颐→继续」零异常通过（L2201/L2207）；**NaN=3711 与声明逐字吻合**。
- 偏差①：首跑崩溃的原始日志未归档（现档已被回归局覆盖；`autoslay-repro.log` 不含 `couldn't generate a valid card` 签名）——"首跑 exit 1 @Gorge()" 现场已不可复核，仅 DEVLOG 自述。
- 偏差②：r2-r4（08-24 04:36-05:13，即 E2 turbo 对照局）同 seed P1SMOKE3 却 embark `SPIRE1-DEFECT` + Ascension 1，与 DEVLOG 自己注记的"同 seed 必同角色"矛盾（应为期间构建/配置变更所致）。不影响本条裁定，但该不变式陈述不可靠。

### A3. 「总归档 187 局」（morning-summary §五，08-25 22:15 实测）— ✅ 成立（保守少报）
- 实测盘面：`.tmp/p1-smoke/` 现 245 份 autoslay 日志（+1 speedx.cfg.bak）；含 run 启动行（`Starting run with seed=`）**242** 份；NIGHT 前缀 **196** 份；其中 mtime ≤ 2026-08-25 22:15 的 NIGHT 日志 **193** 份（freeze-review 的"193"即此口径）；全部日志 ≤ cutoff 者 242 份。任一口径均 ≥187 ⇒ 方向为保守少报，无虚增。
- 附带实测：179 份含 `Run completed successfully`（胜局）。

### A4. 终态覆盖矩阵 SILENT 50/50 ✅、DEFECT 63/63 ✅、IRONCLAD 真实 48/48、WATCHER 41/77 — ✅ 在其口径下全部成立（附分母过时警告）
独立重算结果（played 集 = 245 份日志去重，799 个不同 id）：
| 池 | 自有类（继承链解析） | 复用通道（SharedCardReuse 实际打出） | 工具口径合计 |
|---|---|---|---|
| IRONCLAD | **38/38 全覆盖** | 31/31 | 声明分母 38+10=**48 → 48/48 ✓**（真实全覆盖成立）|
| SILENT | **40/40 全覆盖** | 34/35（缺 STORMOFSTEEL） | 声明分母 40+10=**50 → 50/50 ✓** |
| DEFECT | **37/37 全覆盖** | 35/35 | 声明分母 37+26=**63 → 63/63 ✓** |
| WATCHER | **41/77**（缺单与工具输出逐名一致，含 WISH） | 无复用 | **41/77 ✓** |

- THUNDERCLAP 蛇形化 bug 反证成立：played 集实测含 `THUNDERCLAP`（多局）、不含 `THUNDER_CLAP` ⇒ coverage.js REUSE 表 `'ThunderClap'→THUNDER_CLAP` 的 sid 归一化确系 bug，IRONCLAD 47/48 系误报，freeze-review 的改判「真实 48/48」经我独立重算确认。
- ⚠️ 分母过时警告：coverage.js 的 REUSE 表硬编码 10/10/26，而源码 `SharedCardReuse.cs` 在 8781855（08-24 23:48 "inject 57 shipped twins"）后实际为 **31/35/35**。按当前 HEAD 真实池计算，覆盖为 IRONCLAD **69/69**、SILENT **74/75（缺 StormOfSteel）**、DEFECT **72/72**。声明的 50/50、63/63 在"当时代码路径 + 工具口径"内成立，但若作为**当前状态**引用会高估 Silent 覆盖（.tmp/night/coverage.md 至今仍输出旧分母数字，08-26 17:42 重跑亦未修）。

## B. 知识库声明

### B1. research/sts1-kb/ 数据卷 460+ 条 — ✅ 成立（大幅超出）
实测 JSON 条目数：cards-red 75 / green 75 / blue 75 / purple 77 / colorless 39 / curses 14 / status 5 / tempCards 9 / optionCards 5 / deprecated 64 = **438 卡**；relics **186**；potions **43**；events **54**。合计 **721 条 ≥ 460**。"460+"为保守下限（即便只数卡+事件=492 也满足）。

### B2. mechanics/ 语义卷 119 规则 — ✅ 成立（精确命中）
逐文件数 `Rnn` 锚点：action-manager 20 / turn-phase 18 / draw-exhaust 25 / triggers 18 / damage-pipeline 17 / status-stacking 21 = **119**，与 README 文件索引表逐格一致。

## C. 联机声明

### C1. 「MP 失同步三案判定为清单级假阳性（divergence #563/#249 对拍）…players/piles/choices/rewards/creatures 逐字段全同」— ❌ 结论过强（清单差异属实，玩家状态并非全同）
zip 原文重查（logs 目录 5 个 divergence zip 尚在，#563/#249 各有本端+对端两份）：
- **属实部分**：双方 BaseLib 来源不同（local ModsDirectory vs remote SteamWorkshop 3737335127）✓；远端多非玩法 mod（loadedMods.count 30 vs 31：CardTracker、PerfectedStrikeAnime、BulletTimeAnime、MeleeAttack 等）✓；Spire1 分装包名不同（`Spire1 - 铁甲战士` vs `Spire1 - 故障机器人`，同 id 同版本同 source）✓；gameplay mod 集合两侧相同（这正是握手原生放行、无需补丁的原因）；choices.nextChoiceIds / rewards.nextRewardIds / actions.lastExecuted* 全同 ✓；creatures.count=0 两侧全同 ✓；savedProperties.netIdMap 逐槽比对 **0 差异**且 mapHash 相同（0x6897DE66）。
- **反证部分**（逐字引用报告原文）：
  - #563（context: Exiting event room EVENT.ACTSFROMTHEPAST-VAMPIRES）：`09: local=RELIC.ACTSFROMTHEPAST-BLOOD_BANK (鲜血储蓄袋) floor=27; remote=Missing` —— players[1] 遗物清单跨端不一致。
  - #249：players[0] `10: local=RELIC.VELVET_CHOKER … floor=32; remote=RELIC.PHILOSOPHERS_STONE … floor=32`；players[1] `10: local=RELIC.VELVET_CHOKER …; remote=RELIC.DUSTY_TOME (尘封魔典) …` —— 两名玩家的遗物槽在两端视角下指向不同遗物。
- 另：RitsuLib 报告头自称 "48 SavedProperty net-id slot(s) differ" 而内容逐槽全同 —— 诊断器自身存在噪声标记，「假阳性」观感部分来自工具噪声，但上列遗物分歧是内容级差异，非噪声可解释。
- **裁定**：「三案握手/清单层无 gameplay-mod 差异」成立；但「players…逐字段全同 ⇒ 假阳性」被 zip 原文证伪——players.relics 存在至少 1+2 处跨端分歧。「假阳性」定性未被完整证明（piles/choices/rewards/creatures 全同属实，players 不是）。

### C2. IgnoreMpModDifferences「验证通过」真伪 — ❌ 放行无效（已知先验独立复核成立）；弹窗抑制半边有效
- 代码事实（本次独立复核）：引擎 `.tmp/dllsrc/MegaCrit.Sts2.Core.Multiplayer.Connection/HandshakeResult.cs:5` = `public struct HandshakeResult`；`TryReadHandshakeMessage` 按值返回。补丁 `MpIgnoreModDiffPatch.AllowThrough(HandshakeResult __result, …)` **缺 `ref`** ⇒ Harmony 对值类型返回值按副本绑定，`__result.status = HandshakeStatus.Success` 只改副本，调用方永远收不到 —— **放行从未生效**。git 史仅 d0181a0 一个提交触碰过此文件，`ref` 从未存在过。
- 运行时：现存 5 份 godot 日志仅有启动挂载日志 `MP ignore-mod-diff: RitsuLib divergence popup suppressed`，无任何 "forced through" 触发痕迹（08-24 当晚日志已轮转，无法佐证当时行为——但结构上无效不受影响）。
- 弹窗抑制半边：RitsuLibPopupSuppressionPatch 为 prefix skip-original（反射解析第三方类型，缺失静默跳过），设计有效且启动日志证实挂载；诊断 zip 独立落盘有实物旁证 ✓。
- 用语核查：devlog-audit-20260825.md A21 的措辞是「已实现」，DEVLOG 未曾声称放行做过双端实测；但 STATUS 将「握手放行+弹窗抑制」并列为已交付能力，前半句不成立。修复方向：`ref HandshakeResult __result`。

## D. 第三方互操作

### D1. BaseLib 3.4.5 md5 一致（live == NuGet == 官方 zip）— ✅ 成立（三件逐一相等）
| 文件 | live (`…/mods/BaseLib/`) | NuGet `alchyr.sts2.baselib/3.4.5` | `I:/Downloads/BaseLib.3.4.5.zip` |
|---|---|---|---|
| BaseLib.dll | 4380fd038fda7ca92708fd09a8aebf39 | 同（lib/net9.0） | 同 |
| BaseLib.json | 04faa8b337b5d4fd762c58f528e62674 | 同（Content） | 同 |
| BaseLib.pck | ceed43342d19ef116880e18e5e0682f4 | 同（Content） | 同 |

（live 位置 = `G:/steam/steamapps/common/Slay the Spire 2/mods/BaseLib/`，仓库内无 mods/ 目录。）

### D2. Rewind 兼容：Cecil 补丁 attribute 5参→6参「启动 0 异常」— ✅ 运行面成立 / ⚠️ 补丁本体零留痕
- Rewind.dll mtime **2026-08-25 21:36:27**（与晚间追加 21:00-22:15 窗口吻合）；其后全部 4 次启动（godot2026-08-25T22.14/22.20/22.26/22.32）+ 当前 godot.log：**exception=0、Cecil 相关错误=0**，每次 `Rewind v0.26.11 initialized!` 成功。
- 各日志残留 [ERROR] 均非 Cecil 类：Rewind.pck 缺失 manifest 错误（DEVLOG 已自曝「pck 误删待用户重装」，godot.log L565 一致）、`Act 4 is not yet implemented.`、`EpochModel was not found`、11 条本地化格式错误（新观察，非本声明范围）。
- ⚠️：Cecil 补丁脚本不在仓内（全仓 grep 仅审计文档提及），属仓外一次性二进制操作，不可复现；「0 异常」结论依赖运行证据而非可重放的补丁产物。

### D3. AFTP 双 fork 建立+克隆+构建绿 — ✅ 成立（产物级证据）
- `G:/omp works/aftp-ActsFromThePast` HEAD = **7416aef**「build: port paths to local machine; stage deploy instead of live mods」（与声明 commit 号一致）；`bin/Release/net9.0/ActsFromThePast.dll`（08-25 01:56）md5 `7b72131893b2604ba48d587599c6c052` == `aftp-stage/ActsFromThePast.dll` ⇒「产物走 stage 不进 live」闭环。
- `aftp-ActsFromThePastMultiplayerBalance` HEAD = **3ce2e1f Initial commit**（零修改）+ `bin/Release/ActsFromThePastMultiplayerBalance.dll`（08-25 01:50）。
- 注：「构建绿」以本地构建产物在盘为证（无 CI）；遵守只读约束未重跑 dotnet build。

## E. 本地化键数

### E1. cards.json 673 键（session 13 时点）— ✅ 间接成立
git 首次快照 9bcfc06（08-21）即为 eng=zhs=**675** = 声明 673 + Madness 波 +2（§5.7 明示 +2 键）的精确后值；此后 f75ec23 起 677（=现值，eng=zhs 一致）。「673」本体早于 git 无 blob，但增量链（673→675@首提交）严丝合缝。

### E2. events.json 655 / 633 键 — 655 ✅ 间接成立；633 ⚠️ 无法验证
- 首次快照 9bcfc06 即为 **656** = 声明 655 + FaceTrader TRADE 描述 +1（§5.7）的精确后值；当前 eng=zhs=656。
- 「633 keys」（session 4 J.A.X. 时点）早于 git 初始化（9bcfc06 是仓库首个提交），无任何 blob 可对；仅 DEVLOG 自述，量级与后续增量相容。缺证据源：session 1-5 时代的文件备份或 workshop 包存档。

---

## 统计

| # | 声明 | 裁定 |
|---|---|---|
| A1 | heart5 exit 0 | ✅（退出码为口径推定）|
| A2 | P1SMOKE3 IRONCLAD 首胜 | ✅（首跑崩溃现场无原始日志）|
| A3 | 总归档 187 局 | ✅ 保守少报（实测 ≤cutoff 193-242）|
| A4 | SILENT 50/50、DEFECT 63/63、IRONCLAD 真实 48/48、WATCHER 41/77 | ✅ 口径内成立；⚠️ 分母未随 8781855 更新，HEAD 真实覆盖 69/69、74/75（缺 StormOfSteel）、72/72、41/77 |
| B1 | KB 数据卷 460+ 条 | ✅（实测 721）|
| B2 | mechanics 119 规则 | ✅（精确 119）|
| C1 | 三案=清单级假阳性、状态逐字段全同 | ❌ 清单差异属实，但 players.relics 有 3 处跨端分歧（#563×1、#249×2）|
| C2 | IgnoreMpModDifferences 握手放行有效 | ❌ struct 缺 ref，放行从未生效；弹窗抑制半边 ✅ |
| D1 | BaseLib 3.4.5 md5 一致 | ✅（三件×三方逐一相等）|
| D2 | Rewind Cecil 启动 0 异常 | ✅ 运行面 / ⚠️ 补丁零留痕不可复现 |
| D3 | AFTP 双 fork 构建绿 | ✅（7416aef / 3ce2e1f，产物 md5 与 stage 一致）|
| E1 | cards.json 673 键 | ✅ 间接（首提交 675=673+2 精确吻合）|
| E2 | events.json 655/633 键 | 655 ✅ 间接；633 ⚠️ 早于 git 无法验证 |

**总计：13 条 = ✅10（含 2 条间接、1 条口径推定）· ❌2 · ⚠️1（另 A4/D2 含 ⚠️ 子项、A2 含偏差注记）**

关键修正建议（供冻结参考，不在本轮执行）：
1. MpIgnoreModDiffPatch.AllowThrough 加 `ref`（否则删除该半边，只保留弹窗抑制）。
2. 「三案假阳性」表述降级为「握手/清单层排除；players.relics 存在未解释跨端分歧」。
3. coverage.js REUSE 表改为解析 SharedCardReuse.cs 或删除，避免 50/50、63/63 作为"当前状态"被继续引用。
