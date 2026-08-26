# DEVLOG 修复声明推倒重验报告（ReVerifyFixes，2026-08-26）

**范围**：DEVLOG Sessions 8–15 全部 fixed/commit 段 + 2026-08-25 全部夜间批次所列 20 个 commit。
**方法**：git 历史（`git show --stat` / 定向 diff）× 当前 HEAD 代码逐点核对 × `.tmp/p1-smoke/` 246 份归档日志时间戳对拍。引擎权威源 `.tmp/dllsrc/`。不信任任何已写结论，只采信本次重新取得的证据。

---

## 一、逐 commit 裁定

### 1. `3806762` Silent NRE（run-history 图标，2026-08-21 23:12）— ✅内容存在｜HEAD：仍在
- commit 实含 **恰好 40 个 PNG**（grep 计数=40，即声明的 20 遭遇 × main/outline，70 字节占位）+ `Spire1Encounter` 图标 override。
- HEAD 核对：文件现居 `mod/Spire1Code/Monsters/Spire1Encounter.cs`（后迁址），`CustomRunHistoryIconPath/OutlinePath => CustomIconPath(Id.Entry)`，小写化处理带注释在位（`id.ToLowerInvariant()`）。未被削弱。
- 日志：当时归档管线尚未建立（`.tmp/p1-smoke/` 最早归档为 08-23），「Second launch confirmed」无档案可对拍 ⚠️（属管线前时代，非虚报嫌疑）。

### 2. `f5f7261` 战斗冻结（slime FollowUpState，2026-08-22 01:38）— ✅内容存在｜HEAD：仍在并已扩展
- commit diff：AcidSlimeS/M/L + SpikeSlimeL 共 14 行接线，与声明一致。
- HEAD 抽样 11 个怪物文件 FollowUpState 引用全部在位（AcidSlimeS=2 … Hexaghost=9）；Session 10.2 的「Final sweep: ALL monsters wired」与现状吻合。
- 日志：冻结原始栈无归档 ⚠️；间接证据强——08-23 之后 200+ 局完整自动战斗零该异常复发。

### 3. `fcb9ad2` 四幕壳（2026-08-21 21:59）— ✅内容存在｜HEAD：仍在
- commit 实含 TheCity/TheBeyond/TheEnding 三幕 + DungeonSelectionPatch 改造 + acts/settings_ui 双语 json，与声明逐项对应。
- HEAD：四幕文件齐（Acts/ 目录），DungeonSelectionPatch 四幕序列 Exordium→City→Beyond→Ending 在位（:48-51）。「gate 移除」属实。
- 后续 2aa5acd（Act2/3/4 full migration）是扩充非削弱。

### 4. `04eb5f7` 启动事故（2026-08-21 22:13）— ⚠️部分｜HEAD：快照仍在，修复本体不可 git 验证
- commit 只含 `DEVLOG-crash-snapshot.txt`（103 行现场日志摘录；HEAD 上文件存在 ✓）。
- 声明的修复 = 把 `mods/BaseLib-3.3.5-backup/` 移出 mods 目录——**repo 外文件系统动作，git 四类证据均无法证实或证伪**。叙述自洽但修复本体裁定为不可验证。

### 5. `6128311` 全量美术（2026-08-22 04:21）— ✅内容存在｜HEAD：仍在并被后续增强
- commit 884 文件全部位于 images/：card_portraits **660**（≈331 小图+big）、relics **108**（=36 类 × 3 尺寸，精确命中声明）、powers **100**（=50×2）、potions **16**（=8×2）。
- 与 DEVLOG 11.2 数字吻合；「893 art entries」为最终 pck 条目数（含后续增量），非矛盾。
- 注意：DEVLOG 自己在战果#5 已勘误 11.2 的「331/331 mapped」误导（存在≠真图，302 张曾为纯色占位，后经 5588f9f/465efe9 换真图）。本次重验确认勘误后的说法成立。

### 6. `62478e3` 卡去重（2026-08-22 00:34）— ✅内容存在｜HEAD：主体仍在，**有文档记载的部分回退**
- commit 含 sink pool + 大量卡片 `[Pool]` 改动 + starter deck 换原生模型。
- HEAD：starter decks 仍用 `MegaCrit.Sts2.Core.Models.Cards.Strike*/Defend*/Bash/Neutralize/Survivor` 原生模型 ✓；76 张卡带 `[Pool(typeof(Spire1LegacyPool))]`。
- 数量账自洽：111（本 commit）→ 492c487 返还基础牌+11 诅咒余 89 → 8781855 返还 12 → f75ec23 BladeDance 回现役 −1 ⇒ **76 = 77−1**，全部是 DEVLOG 明文记载的演进，非静默削弱。

### 7. `03ae5d1` + `7c98579` Darv/DustyTome NRE（2026-08-22 03:53 / 05:14）— ✅内容存在｜HEAD：仍在
- 03ae5d1 创建 DustyTomeAncientFallbackPatch（62 行）+ cpm.cs（131 行 scratch）；7c98579 补 regent 映射与空池守卫并清理 scratch 文件（ccpm/cpm 各删除）——两步与声明完全一致。
- HEAD：`Patches/DustyTomeAncientFallbackPatch.cs` 在位，ironclad/silent/defect/**regent** 四映射齐全（:62-65），空 fallback 守卫在位（:33,:46-52）。`Spire1LegacyPool` 已迁至正式路径 `Character/Spire1LegacyPool.cs`。
- 日志：`relic add DUSTY_TOME` 实测当时欠账；后经 DebugRelicInjectPatch 链路补验（f2f3305 批次），DEVLOG 已自行修正早期错误推断。

### 8. `af6d1d7` GA 双修正（2026-08-25 00:39）— ✅内容存在｜HEAD：仍在，**且有运行时日志实证**
- HEAD：`GeneticAlgorithm.cs:18 [Pool(typeof(DefectCardPool))]` ✓；语义为 Block（BaseBlock 1/2、BlockVar、CardBlock），无 Dexterity ✓。
- **日志实证**：NIGHT 归档存在成串探针 `[Spire1] GA play: extra=0 block=1 deck=ok → extra=4 block=5`，成长链活体确认——「验证通过」声明成立。
- 小瑕疵：`ExtraGain` 属性 doc-comment 仍写「本场战斗实际提供的敏捷值」，af6d1d7 改语义时未清旧注释（文档漂移，P3）。

### 9. `d0181a0` 联机放行（2026-08-25 00:54）— ✅内容提交存在，但 ❌**修复从未生效**（已知先验证实）｜HEAD：缺陷仍在
- **反证链**：引擎 `.tmp/dllsrc/MegaCrit.Sts2.Core.Multiplayer.Connection/HandshakeResult.cs:5` 声明 `public struct HandshakeResult`；调用方 `HandshakeManager.cs:54` 按值接收返回值。我方 postfix `AllowThrough(HandshakeResult __result, ...)`（MpIgnoreModDiffPatch.cs:36）按值拿到副本，内部 `__result.status = HandshakeStatus.Success` 只改写副本，**调用方读到的仍是原判定** → ModMismatch 一律放行、同版本哈希放行两条主路径全部无效。Harmony 对值类型返回值须声明 `ref HandshakeResult __result` 方能回写。
- 弹窗抑制半边（RitsuLibPopupSuppressionPatch）是 prefix 拦 void 调用，不受影响，应有效（MainFile.cs:67 显式 Apply 在位）。
- 配置默认 `IgnoreMpModDifferences = true` 属实在档（Spire1Config.cs:59）。
- divergence #563/#249 对拍实物不在仓库档案内 ⚠️（RitsuLib zip 落游戏 logs 目录，已轮转风险）。
- 结论：**功能级声明（联机可进）不成立**；代码提交行为「加了补丁」成立。后续无任何 commit 修复此缺陷。

### 10. `41e7acc` 跳过按钮（2026-08-25 00:58）— ✅内容存在｜HEAD：仍在且经 fc9ef16 加固
- SkipNodeButtonPatch（68 行）+ config 开关默认开（Spire1Config.cs:65）均在。
- fc9ef16 三缺陷修复全部在 HEAD：每次 Open 复位 `button.Disabled=false`、文案走 `TranslationServer.Translate("SPIRE1_UI_SKIP_NODE")` 且键缺失回落中文、按下后单次失效。双语 ui.json 键在位。
- 真人局可视化验证 DEVLOG 自认待用户完成（Godot UI 无法自动化）⚠️ 如实披露。

### 11. `bd6c539` 观者归档（2026-08-25 01:19）— ✅内容存在｜HEAD：仍在，**有正确时间戳的日志实证**
- ArchivedCharacterGatePatch 104 行 + Spire1.json 0.9.0→0.9.1 bump 与声明一致；HEAD `ArchivedPools = { WatcherCardPool }`（:45）。
- **日志实证**：`character archive: 77 model type(s) hidden from card library (pools: WatcherCardPool)` 出现在 NIGHT00212（mtime 08-25 03:13，晚于 commit 01:19）等多份归档——「启动日志 77 张对账」声明成立。（注：DEVLOG 引用名为 P1SMOKE4，而档案里 p1smoke4*.log 是 08-24 的旧 run；实际证据在 NIGHT 系列，引用名有误但证据本身真实存在。）

### 12. `3cfbcf1` LessonLearned 谓词（2026-08-25 02:21）— ✅本体在 HEAD，但其引证有误，**同病灶 Feed 未修**
- HEAD LessonLearned.cs:30 为正确形态 `All(p => p.ShouldOwnerDeathTriggerFatal())` ✓。
- **反证**：其声明称「matches Feed/HandOfGreed/TheHunt」。引擎官方 Feed（dllsrc `Models.Cards/Feed.cs:38`）确为无否定形态，但**仓库自己的 `mod/Spire1Code/Cards/Feed.cs:30` 是取反形态** `All(p => !p.ShouldOwnerDeathTriggerFatal())` —— 与引擎相反，Minion 免死语义颠倒（杀假死单位触发、杀真身不触发）。3cfbcf1 未顺手修 Feed，冻结审查先验「Feed 谓词未随 LessonLearned 修」**证实，HEAD 仍在**。
- 连带发现：`RitualDagger.cs:34-38` 注释仍写「Cards/LessonLearned.cs:30 negate… Not fixed here」——LessonLearned 已修，注释 stale（该卡自身实现 :97 正确）。

### 13. `fc9ef16` Critic P1/P2 修复（2026-08-25 02:19）— ✅内容存在｜HEAD：仍在
- 商店守卫门控：`ShopEnoughGoldGuardPatch.cs:37 if (!AutoSlayImmortalityPatch.Active) return;` ✓（类定义于 AutoSlayGatePatch.cs:51，编译闭合）。
- 跳过按钮三缺陷：见第 10 条，全部在位。

### 14. `4695124` BaseLib 钉版（2026-08-25 02:34）— ✅内容存在｜HEAD：仍在
- diff 精确：`Version="*"` → `Version="3.4.5"`（单行）。HEAD csproj:38 保持钉住。manifest 自动写 target 仍在但写入钉定版本，风险如声明消除。

### 15. `8781855` 恢复12张 + 孪生注入 + census（2026-08-24 23:48）— ✅内容存在｜HEAD：注入部分被 3a0de3d 削弱（见第 17 条）
- 12 张卡文件逐一改回角色池（抽查 Caltrops→SilentCardPool ✓）；SharedCardReuse +99；LogPoolCensus 与 ColorlessReuse（无色通道）在 HEAD 在位。
- 同时删除了旧 HandleAsync 版 ShopPurchaseGuardPatch（96 行）——与守卫两次废弃重来的记载一致。
- 小局限：census 打印在 non-pure 分支尾部，pure 分支 early return 不打印（观测盲区，P3）。

### 16. `1192c01` 双消耗剥离（2026-08-25 08:05）— ✅内容存在｜HEAD：仍在
- zhs diff 抽样（INTIMIDATE/DISARM/IMPERVIOUS/PUMMEL/SEEING_RED/WARCRY）均为删独立尾行「消耗 。」；HEAD 上 INTIMIDATE 等已无尾行 ✓。eng 1352 行大改为等量换行规范化（730+/726−）。
- 用户目测报告型问题，autoslay 日志不可能捕获 UI 渲染，无日志属合理 ⚠️。

### 17. `3deabac` CHEESE 修复（2026-08-23 18:49）— ✅内容存在，**后被 3a0de3d 推翻一半**（已知先验证实）
- commit 内容与声明精确一致：IroncladReuse(+10)/SilentReuse(+11)，Gorge 8-Common 契约、6 own Commons 名单逐字吻合。
- **当时验证有日志实证**：`autoslay-p1smoke3.log` mtime 08-23 **18:54**（fix 提交 18:49 后 5 分钟）内含 `Victory! Run completed`、RoomFullOfCheese 事件正常通过、CHEESE 异常文本 0 次——「回归通过」声明成立。原始崩溃日志被同名重跑覆盖未归档（before 态仅存于 DEVLOG 引文）⚠️。
- **HEAD 现状：被 3a0de3d 部分推翻**。3a0de3d 从非 pure 分支删掉 `DefectReuse→DefectCardPool` 与 `IroncladReuse→Spire1CardPool` 两行注入且未恢复（diff 可见 `- foreach (var cardType in DefectReuse)…` / `- foreach (var cardType in IroncladReuse)…`）。当前默认配置（PureSts1Pools=false）下：Silent 注入存活；**Ironclad 回到 6 own Commons、Defect 失去复用保护** → ROOM_FULL_OF_CHEESE Gorge 的 InvalidOperationException 回归路径重新打开。RewardClampPatch 仅 pure 模式激活（`if (!PureSts1Pools) return true`），不兜底。
- 归档中 246 局无一含「generate a valid card」文本 → 回归至今**无运行时表现记录**（其后 4 局 21:56–22:32 角色为 Regent/Necrobinder 系，未组成 Ironclad+CHEESE 触发条件），属潜伏雷非已爆雷。

### 18. `3a0de3d` pure 带宽修复（2026-08-25 21:10）— ✅声明内容存在，❌**修复自身引入回归**（已知先验证实）｜HEAD：回归仍在
- 声明内容三项核实：pure 分支改 AddOwnImplementations 全稀有度注入 ✓；Armaments 升级 Block +3→8（Armaments.cs:41 注释「官方 5→8」）✓；Carnage/GhostlyArmor/Dazed 独立虚无行剥离（HEAD zhs 文本无「虚无」尾行）✓。
- **回归实锤**：见第 17 条——同一 commit 删除非 pure 分支两行注入。这是「修 A 拆 B」型自我回归，后续无 commit 修复。
- 连带：`PureSts1Adds` 数组（SharedCardReuse.cs:156）自此失去唯一消费者，成为**死代码**（全仓 grep 仅声明处命中）。
- **验证缺口（任务重点 4）**：晨报 5358e41 自述 pure 稀有度修复「✅ 已修**待纯角色局实证**」——即提交时刻无任何运行验证。问题陈述侧的 DingyRug 全无色在 246 局归档中 **0 次命中**（大小写不敏感 + 中文名变体均无）→ 问题的日志证据也不在档案。该 commit 是全部受验对象中唯一「问题与验证双双无档案」的修复。

### 19. `5358e41` 终态汇总（2026-08-25 21:41）— ✅内容存在
- `research/audits/morning-summary-20260825.md` 在库，Cutoff 追加节实载 187 局、终态覆盖、live/zip 终哈希 dll aa8b4f33 / pck 65006e85。文档型交付，其中对 pure 修复如实标注验证缺口（见上）。

---

## 二、三个特别验证专项结论

| 先验 | 裁定 | 关键证据 |
|---|---|---|
| 3a0de3d 非 pure 分支丢 Ironclad/Defect 注入 → CHEESE 回归 | **✅证实，HEAD 仍在** | 3a0de3d diff 删除两行注入；HEAD Register() 非 pure 分支仅剩 SilentReuse+ColorlessReuse；RewardClampPatch `if (!PureSts1Pools) return true` 不兜底 |
| d0181a0 HandshakeResult struct 缺 ref → 放行从未生效 | **✅证实，HEAD 仍在** | dllsrc HandshakeResult.cs:5 `public struct`；HandshakeManager.cs:54 按值接返回值；postfix 形参无非 ref 的 `__result` 副本改写 |
| 3deabac CHEESE 修复被 3a0de3d 重新破坏 | **✅证实（Ironclad 半边）** | 3deabac 的 IroncladReuse 注入正是 3a0de3d 删掉的两行之一；Silent 半边存活 |

## 三、「验证通过」声明的日志对拍总表

| 声明 | commit 时刻 | 档案证据 | 时间戳吻合 |
|---|---|---|---|
| CHEESE 修复回归胜利 | 08-23 18:49 | autoslay-p1smoke3.log（Victory+CHEESE 异常 0 次） | ✅ 18:54，晚 5 分钟 |
| GA 成长链探针 | 08-25 00:39 | NIGHT 归档 `GA play: extra=N block=N+1 deck=ok` 串 | ✅ 后续夜间局 |
| 观者归档 77 张启动对账 | 08-25 01:19 | NIGHT00212 等多份 boot 行 | ✅ 03:13 起（DEVLOG 引用的 run 名 P1SMOKE4 有误，证据本体真实） |
| P1SMOKE9 八胜 | 08-24 05:35 (docs) | autoslay-p1smoke9.log | ✅ 05:32 |
| pure 稀有度修复 | 08-25 21:10 | **无**（晨报自述待实证；DingyRug 问题侧亦 0 命中） | ❌ 缺失 |
| 联机放行生效 | 08-25 00:54 | divergence #563/#249 实物不在仓库；且代码层面从未生效 | ❌ 缺失+无效 |
| 3806762 二启确认 / f5f7261 / fcb9ad2 | 08-21/22 | 归档管线未建，无档案 ⚠️（间接证据：后续 200+ 局零复发） | — |
| 1192c01 双消耗 | 08-25 08:05 | UI 渲染问题，autoslay 结构上无法捕获 ⚠️ 合理缺失 | — |

## 四、统计

- 受验 commit 条目：**19 条**（20 个哈希）。
- 内容存在性：**✅ 18 / ⚠️ 1**（04eb5f7 仅存证快照，修复本体在 repo 外不可 git 验证）；**❌ 0**（无虚造 commit 或凭空内容）。
- HEAD 存续：**完好 14**；**文档化部分回退 1**（62478e3，账目自洽 76=89−12−1）；**被后续削弱/推翻 3**（3deabac←3a0de3d；8781855 的注入半壁←3a0de3d；3a0de3d 自身携带回归）；**从未生效 1**（d0181a0 主补丁，struct 缺 ref）。
- 三个已知先验（3a0de3d 回归 / d0181a0 struct ref / 3deabac 被再破坏）：**全部证实**。
- 新发现的同病灶尾巴：Feed.cs 谓词取反（HEAD 仍在）、RitualDagger.cs stale 注释、GeneticAlgorithm.ExtraGain stale 注释、PureSts1Adds 死代码、LogPoolCensus pure 分支盲区。
- 总裁定：DEVLOG 修复声明**无虚报**（commit 内容与描述逐条对得上），但存在两类系统性问题：①「修复引入回归」一例（3a0de3d）且 HEAD 仍带伤；②「验证通过」的证据标准不一——3a0de3d 与 d0181a0 两例在当时和档案中都拿不出支撑其核心功能主张的运行证据。
