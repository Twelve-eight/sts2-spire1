# 历史问题复查报告 — review-hist（2026-08-26）

> 复查对象：三份历史审计（critique-20260825.md 17 条 / devlog-audit-20260825.md A27+B45 / kb/pitfalls.md P-01..P-10）+ DEVLOG「已修复」声明抽样 + 晨间汇总验证声称 + sts1-kb 数据质量。
> 基线：HEAD=`5358e41`（112 commits）。方法：只读。git log/show、代码现状、jar 反汇编（desktop-1.0.jar）、245 份 autoslay 日志复算。
> 结论标记：已修复✓ / 部分修复△ / 未修复✗ / 又回归↺ / 无法验证？

## 总览

| 来源 | 已修复✓ | 部分修复△ | 未修复✗ | 无法验证？ |
|---|---|---|---|---|
| critique 17 条（含 P2-7 拆 5 子项） | 3（P1-1、P2-9、P3-10；另 P3-17 本次代闭环） | 4（P2-3、P2-4、P2-6、P2-8、P3-12、P3-14 计 6 中取△…详见表） | 7（P1-2、P2-5、P2-7 四子项、P3-11、P3-13、P3-15、P3-16 加重） | 0 |
| devlog-audit 关键项（A2/C1-C5/B42-B45/U10/U15/U17） | 6 | 1（C1） | 1（A2 _staging） | 2（U10、U17 重放） |
| pitfalls 10 条 | 7 | 2（P-01、P-02） | 0 | 1（P-10 未解，KB 自认） |
| 晨间汇总声称 | 补丁失败 0✓、异常口径✓、终态哈希✓、SWD/DEF/WAT 覆盖✓ | 187 局（盘面 193，保守成立） | **IRONCLAD 47/48 归因证伪↺**（工具 bug，真实 48/48） | Rewind 兼容（仓外零留痕） |

**一句话总评**：修复执行力真实存在（抽验的提交号全部命中、终态部署哈希逐字节复核相等），但「修复完整性」系统性偏弱——几乎每条已修项都留了同病灶尾巴（Feed 未修、关键词剥离漏 11 张、文档勘误只写 DEVLOG 不改原文），且覆盖工具自身的 id bug 把一次完美覆盖误报成 47/48。

---

## 一、critique-20260825.md 17 条逐条复查

|#|结论|证据（现状 @5358e41）|
|---|---|---|
|P1-1 商店守卫未门控|**已修复✓**|fc9ef16；`AutoSlay/ShopEnoughGoldGuardPatch.cs:37` `if (!Spire1.Spire1Code.Patches.AutoSlayImmortalityPatch.Active) return;`，Info 日志亦移入门内，正常对局零介入零刷屏|
|P1-2 联机放行默认开+弹窗抑制|**未修复✗（有意保留的设计决策）**|`Config/Spire1Config.cs:59` `IgnoreMpModDifferences { get; set; } = true;` 原样；`MpIgnoreModDiffPatch.cs` 放行+RitsuLib 弹窗抑制仍在（`MainFile.cs:63-67` 显式 Apply）。新增辩护：DEVLOG STATUS「失同步三案判定为清单级假阳性（divergence #563/#249 对拍）」；65a858f 给设置页补了说明文案。批评的两条修复方向（默认 false / 非阻断 HUD 提示）均未采纳|
|P2-3 LessonLearned Fatal 谓词取反|**部分修复△**|本卡已修：3cfbcf1 仅改 `Cards/LessonLearned.cs` 一行，现为 `All(p => p.ShouldOwnerDeathTriggerFatal())` ✓。但同病灶 `Cards/Feed.cs:30` 仍取反 `All(p => !p.ShouldOwnerDeathTriggerFatal())` ✗——pitfalls P-02 宣称的修复只落了一半；Feed 在 LegacyPool 且不在 PureSts1Adds 注入清单，暴露面仅老存档。另 `Cards/RitualDagger.cs:37-39` 注释反向过期：仍称 "LessonLearned.cs:30 negate … Not fixed here"，实际已修（新发现 #5）|
|P2-4 跳过按钮三缺陷|**部分修复△**|a) 会话内单次失效已修：`SkipNodeButtonPatch.cs:57` 每次 Open 复位 `button.Disabled = false;`（fc9ef16）。b) i18n 半修：`:53-54` Text 走 `SPIRE1_UI_SKIP_NODE` 双语键（eng/zhs ui.json 各 1 键实测在盘），但 `:41` TooltipText 仍硬编码中文「卡在房间出不去时使用：…」。c) 黑屏场景放行前提仍无真人验证（晨间汇总遗留表自认「待用户」）|
|P2-5 Girya 死遗物|**未修复✗（挂账设计决策）**|`Relics/Girya.cs:13` `StrengthBonus = 0`、`:27` FLAG "rest-site lift option not wired" 原样；晨间汇总遗留表列为「用户裁定方向后实现」，与 DEVELOP 承诺的暂存池方案均未动|
|P2-6 N'loth 空壳事件|**部分修复△**|文档面已反转：`Events/Nloth.cs` 类注释新增 FLAG UPDATE (2026-08-25) 写明 GetBaseOdds ×3 实现路线（6010f21）；功能面未动——`GenerateInitialOptions` 仍只有 Leave，双 offer 依旧 withheld，空壳事件继续占共享池坑位|
|P2-7.1 DEVELOP §7a vs 复用清单|**未修复✗**|DEVELOP.md:99 仍列 Claw/Chill/Darkness/Equilibrium/MachineLearning 为"differ mechanically → need our own class"；`Character/SharedCardReuse.cs` DefectReuse 仍注入 `Equilibrium(:36)`、`MachineLearning(:47)`、`Chill(:54)`、`Claw(:55)`、`Darkness(:57)`。两边矛盾原样|
|P2-7.2 DEVELOP §7b vs Watcher FLAG|**未修复✗**|`Character/Watcher.cs:15-16` 仍写 "no Calm/Wrath/Divinity/Mantra stance API exists in StS2 v0.111.0"，与 DEVELOP.md:101 "FEASIBLE … supersedes the earlier flag" 直接相抵|
|P2-7.3 FINAL-REPORT 观者开关描述|**部分修复△**|勘误只写在 DEVLOG.md:751（【2026-08-25 勘误】开关已随 0c2ac26 移除，永久硬隐藏）；FINAL-REPORT-20260824.md:29 陈文 "`Spire1Config.EnableSts1Watcher=false` 默认" 未改一字|
|P2-7.4 DungeonSelectionPatch 空池注释|**未修复✗**|`Patches/DungeonSelectionPatch.cs:42-44` 仍称 TheCity/TheBeyond/TheEnding "currently ship with empty encounter pools (their monsters are M2.5+ work)"，而 M2.5 全迁移早已落地（2aa5acd）|
|P2-7.5 Spire1LegacyPool 注释|**未修复✗**|`Character/Spire1LegacyPool.cs:7` 仍写 "group A + the six Strike/Defend variants" 入池；实测该池现役成员 76 张全为孪生退役卡，基础牌 Strike*/Defend*/Bash/Survivor/Neutralize **0 张**带 LegacyPool 属性（492c487 已归还现役池）——注释描述的是过期状态|
|P2-8 浮动版本+manifest 自动改写|**部分修复△**|BaseLib 已钉死：4695124，`Spire1.csproj:37` `Version="3.4.5"` ✓（ModAnalyzers 保持 `*`，pitfalls P-07 明言分析器可浮动=按设计）。但 `UpdateDependencyVersions` target（csproj:79-97）未删——钉版后沦为 no-op 死机构；lock 文件未提交（mod/packages.lock.json 不存在）。批评建议的「pin + lock file」完成一半|
|P2-9 四开关无设置文案|**已修复✓**|65a858f；eng/zhs settings_ui.json 各 31 键，四个开关 title/hover.title/hover.desc 双语齐全（node 解析实测键数相等）|
|P3-10 zhs 缺 ancients 表|**已修复✓**|be0c902；`localization/zhs/ancients.json` 1359B 在盘（eng 1333B 对照）|
|P3-11 调试注入器绝对路径入库|**未修复✗**|`DebugCardInjectPatch.cs:21`、`DebugRelicInjectPatch.cs:25` 仍硬编码 `G:\\omp works\\sts2-spire1\\.tmp\\night\\inject-queue.txt`。有 AutoSlayer 门控故低危，且无任何修复声明——挂账属实|
|P3-12 过程资产放 .tmp|**部分修复△**|事实类已入 research/kb/（a5c1795：engine-facts/aftp-interop/debug-protocols/defect-pool-case 等 6 文件+README）；但管线工具 coverage.js/night_drain.ps1/loc_drift.js/pck_ls.js 仍在 `.tmp/night/`，换机即失传的风险原样|
|P3-13 MainFile GetTypes 单点|**未修复✗**|`MainFile.cs:42` `foreach (var type in typeof(MainFile).Assembly.GetTypes())` 无 ReflectionTypeLoadException 防护；per-type try/catch 只护 Patch() 段（:50-56）。批评指出的异常路径原样|
|P3-14 autoslay=dev mode 未记录|**部分修复△**|DEVLOG.md:620 记录了 `--autoslay` 契约需 `IsReleaseGame()==false`（我方 AutoSlayGatePatch 解锁），等价于记录「冒烟跑在 dev-mode」；WORKPLAN/人工 release-path 检查清单未见|
|P3-15 min_game_version 滞后|**未修复✗**|`Spire1.json:7` `"min_game_version": "0.107.0"` 原样（目标 v0.111.0）|
|P3-16 ToolsOfTheTrade 注释错误|**未修复✗ 且加重**|`SharedCardReuse.cs:147` 注释仍 "0E, 6 dmg (+3)"（KB/jar 权威：POWER/RARE）；头注 "Every entry below was verified field by field"（:17-19）原样。**加重**：后续提交在同一清单里新增同类错误——`:63` Tempest 注释 "0E, shuffle everything back, draw 4 (+2), Exhaust" 与 Tempest（X 费、充能球系）毫无关系（新发现 #4）|
|P3-17 Thunderclap jar 归属悬空|**未闭环→本次代闭环✓**|WORKPLAN-20260825.md:61 悬项原文未动；但本次直接实证：`unzip -l desktop-1.0.jar` 含 `com/megacrit/cardcrawl/cards/red/ThunderClap.class`（注意类名大写 C），"notInJar" 确系 grep 大小写伪象。可勾销|

## 二、devlog-audit-20260825 关键项复查

|项|结论|证据|
|---|---|---|
|A2 `_staging` 清理「部分」|**仍未完成✗**|`mod/_staging/louse-extracted-data.md` 至今在盘（ls 实测）——审计后无任何清理动作|
|C1 观者归档门禁漂移|**部分修复△**|DEVLOG.md:751 行内【勘误】已加；FINAL-REPORT-20260824.md:29 原文未改|
|C2 Nloth 可实现性反转|**已修复✓**|6010f21 改写 `Events/Nloth.cs` 类注释，陈旧 "not implementable" 已删|
|C3 zhs relics 空表（B45）|**已修复✓**|8f69a72 后 `zhs/relics.json` 72 键 / eng 108 键（node 实测），不再是 `{}`|
|C4 遭遇计数 55/56|**已修复✓**|DEVLOG.md:509 行内【勘误 2026-08-25】"Act3 实际落盘 16 场…合计 55 而非隐含的 56"|
|C5 STATUS 头滞后|**已修复✓**|DEVLOG.md:6-9 已刷新为 session14+ 夜间批状态（0.9.1 发布/钉版/联机层/救援层）|
|B43 LegacyPool 76 张|**一致✓**|grep `[Pool(typeof(Spire1LegacyPool))]` Cards 目录 = 76 文件，与审计数吻合|
|B33 引擎 121 怪物类|**一致✓**|`.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Monsters/*.cs` = 121|
|U10 跳过按钮真人验证|**无法验证？**|晨间汇总遗留表自认待用户；无新增证据|
|U15 run_history 占位图标|**一致✓**|`mod/Spire1/images/run_history/` 实测 110 png|
|U17 四角色胜利矩阵|**日志佐证✓/重放？**|245 份 autoslay 日志在盘（含 heart/psmoke 系列），未逐局重放；下节矩阵复算间接支持|

## 三、pitfalls P-01..P-10 现状

|项|结论|证据|
|---|---|---|
|P-01 关键词双重渲染|**部分修复△（残留面见新发现 #3）**|1192c01 剥离 48 卡独立 Exhaust 行（Backstab zhs diff 实证 `\n消耗 。` 被剥）；但只覆盖 Exhaust 一种关键词、且正则只匹配行首形态。3a0de3d 追加 Ethereal×3 时 eng Dazed 漏剥。系统性扫描：11 张**现役**卡仍有独立形态残留|
|P-02 谓词取反|**部分修复△**|"去掉否定" 只落在 LessonLearned（3cfbcf1）；Feed.cs:30 取反原样|
|P-03 GA 池归属默认继承|**已修复✓**|af6d1d7；`Cards/GeneticAlgorithm.cs:18` `[Pool(typeof(DefectCardPool))]`；PoolCensus 探针在位（SharedCardReuse.Register → LogPoolCensus，四池稀有度分布打点）|
|P-04 补丁未按运行模式门控|**已修复✓**|fc9ef16 门控早退（同 P1-1）|
|P-05 主线程阻塞等待|**已修复✓**|全仓 8 处 `.Wait()` 中 7 处为引擎异步 `Cmd.Wait(delay)`（awaitable，非阻塞），1 处为教训注释；无 Task.Wait/.Result 残留|
|P-06 清单版本被构建覆盖|**已修复✓**|cb70f82 版本源头化；`Spire1.json` version=0.9.1，CopyToModsFolderOnBuild 复制的 AssemblyName.json 即源 json，语义一致|
|P-07 浮动依赖|**已修复✓**|csproj:37 BaseLib 3.4.5 钉死（分析器浮动属该条目自身豁免条款）|
|P-08 日志轮转吞现场|**已修复✓**|research/kb/debug-protocols.md 存在于库（a5c1795 入库）|
|P-09 进程检查假阳性|**已记录✓（薄）**|pitfalls 本体收录；debug-protocols.md 内未见对应条目（小缺口）|
|P-10 PoolCensus 探针日志缺失|**无法验证？（KB 自认未解）**|初始化期 sink 挂载时序问题，静态不可断言；代码侧探针逻辑在位|

## 四、DEVLOG「已修复/已完成」抽样验证（22 条）

抽样覆盖晨间汇总 F1-F12 修复账、晚间追加、以及 devlog-audit 抽验过的高价值声明（本次独立重验）：

|#|声明|结论|证据|
|---|---|---|---|
|1|F1 GA 双修正 af6d1d7|✓|GeneticAlgorithm.cs:18 池属性在位|
|2|F4 LessonLearned 谓词 3cfbcf1|✓（附 Feed 尾巴）|见 P2-3|
|3|F5 商店守卫门控 fc9ef16|✓|ShopEnoughGoldGuardPatch.cs:37|
|4|F6 跳过按钮复位+i18n fc9ef16|△|复位✓、Text 双语✓、Tooltip 中文硬编码（SkipNodeButtonPatch.cs:41）|
|5|F7 BaseLib 钉版 4695124|✓|csproj:37|
|6|F8 清单版本源头化 cb70f82|✓|Spire1.json version=0.9.1|
|7|F9 归档门控 bd6c539|✓|Patches/ArchivedCharacterGatePatch.cs 存在；Watcher.cs Hide=true 硬编码|
|8|F10 双消耗剥离 48 张 1192c01|△|Exhaust 行首形态确已剥；同行续接形态（LessonLearned zhs）与 Unplayable/Innate/Ethereal 家族漏网|
|9|F11 火堆救援 fbff0a8|✓|Patches/RestSiteLightingRescuePatch.cs 存在|
|10|F12 zhs 补表 be0c902/65a858f|✓|ancients 1359B；settings_ui 31 键×双语|
|11|晚间 Armaments 升级 Block 5→8（3a0de3d）|✓|Cards/Armaments.cs:41 `UpgradeValueBy(3m)` + 注释「官方 5→8」|
|12|晚间双虚无剥离 Carnage/GhostlyArmor/Dazed（3a0de3d）|△|Carnage/GhostlyArmor en+zhs 均净；**Dazed eng "Unplayable. Ethereal." 漏剥**（zhs 剥了虚无留不能被打出，eng 两词全留）|
|13|晚间 pure 全稀有度注入（3a0de3d）|✓|SharedCardReuse.Register pure 分支 AddOwnImplementations 三池全覆盖 + 教训注释在码|
|14|Rewind 兼容 Cecil 补丁 ✅ 验证通过|**？仓内零留痕**|git log -S "Rewind" 仅命中 docs 提交 5358e41；全仓（含 .tmp）无 Cecil 脚本/无 isChangingOwners 代码。Rewind 是 live 第三方 mod（`mods/Rewind/`），补丁应发生在仓外 live dll 上——repo 层面既无实现也无目标哈希记录，不可复核|
|15|Mushrooms 授寄生虫 / DrugDealer J.A.X.（A27）|✓|Events/Mushrooms.cs:45 `AddCurseToDeck<Parasite>`；DrugDealer.cs:35/:51|
|16|卡面 332+332 无占位（A14/A15）|✓|find 实测 small/big 各 332 png，<2KB 为 0 张|
|17|docs 三份 API 字节数（B37/A3）|✓|109997 / 93215 / 136786 逐字节相等|
|18|KB 计数 紫77/遗物186/药水43/事件54（A25/B40）|✓|node 解析逐一命中（77/186/43/54）|
|19|run_history 110 图标（A6）|✓|实测 110 png|
|20|引擎 121 怪物类（B33）|✓|dllsrc Monsters 计 121 .cs|
|21|0.9.1 三包发布（A26）|✓|dist/{Ironclad,Silent,Defect}.zip 存在；三包内 dll/pck md5 与 live **全等**（见下节）|
|22|GA 实战探针 extra/block 成长链|？|依赖夜局日志语义，未逐条重放；coverage 矩阵复算间接支持管线活性|

**抽样小结**：22 条中 15 条完全属实、4 条部分属实（都带着同款尾巴）、1 条（Rewind）repo 层面不可复核、2 条未重放。无一条纯属虚构。

## 五、晨间汇总验证声称核查

|声称|结论|核查过程|
|---|---|---|
|总归档 187 局|**基本成立△**|.tmp/p1-smoke 现 245 份 autoslay*.log（196 NIGHT + drain-r/psmoke/heart/defect-cov/pure 等），243 份含出牌记录；mtime ≤2026-08-25 22:15 的有 **193** 份 ≥187——方向是保守少报（约 ±6 局口径差），无虚增|
|补丁失败 0|**成立✓**|245 份日志全量 grep `Harmony patch .* failed\|failed to apply` = **0 命中**|
|异常 0|**成立✓（需口径注明）**|Unhandled exception / FATAL / Segmentation 全量 0 命中。注意：每局存在大量 Godot `ERROR: Volume can't be set to NaN.` 音频错误（如 NIGHT00209 达 2295 行）——历史 SpeedX NaN 案指的是战斗数值 NaN，音频音量 NaN 属引擎噪音，两者不应混用口径，建议在 KB 注明区分|
|终态覆盖 IRONCLAD 47/48「仅缺 ThunderClap（RNG 观察项）」|**证伪↺**|coverage.js 的 REUSE 表（:31）把类名写成 `'ThunderClap'`，:59 的 sid 计算蛇形化为 `THUNDER_CLAP`；而引擎日志实际落的 id 是 `THUNDERCLAP`（多局打出，played 集合实测含 THUNDERCLAP、不含 THUNDER_CLAP）。**工具 id 归一化 bug，非 RNG**——真实 Ironclad 覆盖为 48/48|
|SILENT 50/50 ✅、DEFECT 63/63 ✅、WATCHER 41/77|**精确复现✓**|以 coverage.js 同口径独立复算（只读版）：SILENT 50/50、DEFECT 63/63、WATCHER 41/77 逐一相符（WATCHER 归档冻结符合预期）|
|终态基线 live=三 zip=dll aa8b4f33 / pck 65006e85|**成立✓**|live mods/Spire1/Spire1.dll md5 前 8 位 `aa8b4f33`、pck `65006e85`；dist 三 zip 解包内核对 dll/pck md5 与 live **六项全等**|
|+1 局 pure 模式验证局|**成立△**|pure-* 日志 5 份跨天在盘（autoslay-pure-defect.log 等含 SPIRE1 出牌记录）；「+1」口径与盘面多份并存略含糊|

## 六、sts1-kb 数据质量抽查（cards-red/green/blue × jar 对拍）

**方法**：从 `G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar` 提取官方 `localization/eng/cards.json` 与 `localization/zhs/cards.json`，与 KB 条目机械 diff；另用 javap 反汇编抽查数值。

|维度|结果|
|---|---|
|英文描述逐字对拍|12/12 一致（Anger/Bash/Thunderclap/Uppercut/Deadly Poison/Adrenaline/Blade Dance/Deflect/Ball Lightning/Echo Form/Buffer/Coolheaded）|
|中文描述逐字对拍|4/4 一致（Blade Dance/Adrenaline/Bash/Echo Form，含 *小刀 类官方星标格式）|
|cost/rarity/type/target/keywords|抽验全对；javap 实证 Uppercut 构造器 cost=2(bipush 13→baseDamage, magic=1)、UNCOMMON、ATTACK 与 KB 一致|
|ThunderClap 存在性|jar 含 red/ThunderClap.class —— 顺带闭环 P3-17|
|**缺陷①无数值字段**|KB 条目仅存 cost/cost_upgraded/type/rarity/target/description/keywords，**不含 damage/block/magic 及升级 delta**（upgraded_description_diff 仅 62 条且多为文本差异）。因此 critique P3-16 的处方「以 KB 为源生成 reuse 清单（脚本化数值 diff）」**实际不可行**——KB 只能支撑文本/cost 对账，数值仲裁仍须回 jar/javap|
|**缺陷②id 双规范混用**|同一文件内 ~1/3 条目用带空格显示名 id（'Blade Dance'/'After Image'/'Ball Lightning'，red/green/blue 分别 33/38/30 条），其余用类名 id（'DeadlyPoison'）。任何按键消费方都要做两种归一|

## 七、发现清单（本次复查新发现问题，按严重度）

|#|严重度|文件:行|问题|证据|修复建议|
|---|---|---|---|---|---|
|1|High|.tmp/night/coverage.js:31,:59|IRONCLAD「47/48 缺 ThunderClap（RNG 观察项）」是**覆盖工具 id bug 而非 RNG**：REUSE 表 `'ThunderClap'` 蛇形化为 `THUNDER_CLAP`，引擎日志实际 id 为 `THUNDERCLAP`（played 集合实测命中前者不存在、后者存在）|本报告 §五复算记录；`Playing THUNDERCLAP` 多局在盘|REUSE 表改 `'Thunderclap'` 或 snake() 加特例映射；更正晨间汇总与 COVERAGE.md 结论为 48/48|
|2|Med|mod/Spire1Code/Cards/Feed.cs:30|Fatal 谓词取反同病灶未随 3cfbcf1 一并修复：pitfalls P-02 宣称已修，实际 Feed 仍 `All(p => !p.ShouldOwnerDeathTriggerFatal())`，持有 SPIRE1-FEED 的老存档 Fatal 分支死亡|Feed.cs:30 vs LessonLearned.cs 现状；3cfbcf1 只改一个文件|照抄 3cfbcf1 一行修复；或给退役类加显式「已知缺陷勿启用」注释并同步 P-02 条目|
|3|Med|mod/Spire1/localization/{zhs,eng}/cards.json（11 张现役卡）|P-01 关键词双渲染修复不完整：引擎按 CanonicalKeywords 自动渲染关键词行，以下现役卡仍有独立形态残留——LessonLearned zhs 句尾同行「消耗。」、Burn/Dazed/Void eng 行首 Unplayable./Ethereal.、Dazed/Void/Pain/Reflex/Regret/Tactician/Necronomicurse/DeusExMachina zhs 行首「不能被打出。」（状态牌出场频率高）；另有退役 Backstab zhs「固有。」、Feed zhs「消耗。」|node 扫描脚本输出（独立形态判定：行首或句号后成句且居尾/居首）；1192c01 仅剥 Exhaust+行首；3a0de3d eng Dazed 漏剥|把窄规则扩展到 Innate/Ethereal/Unplayable 三词 + 「句号后同行续接」形态；eng/zhs 两表同一脚本跑|
|4|Low|mod/Spire1Code/Character/SharedCardReuse.cs:63,:147|复用清单「verified field by field」头注持续失真：ToolsOfTheTrade 注释仍错（实为 POWER），后续提交又新增 Tempest 注释「0E, shuffle everything back, draw 4 (+2), Exhaust」与实际机制无关|SharedCardReuse.cs:63 vs :147 vs KB cards-blue.json|注释脚本化生成或删除逐条注释，让头注重新可信|
|5|Low|mod/Spire1Code/Cards/RitualDagger.cs:37-39|修复引入的反向过期注释：仍称 "Cards/LessonLearned.cs:30 negate … Not fixed here"，实际 3cfbcf1 已修，误导下一读者|RitualDagger.cs vs LessonLearned.cs 现状|更新注释为「已于 3cfbcf1 修复；Feed 除外」|
|6|Low|DEVELOP.md:99; Character/Watcher.cs:15; Patches/DungeonSelectionPatch.cs:42; Character/Spire1LegacyPool.cs:7|critique P2-7 四处文档漂移原地未动（§7a 复用清单矛盾 / stance API FLAG 自相矛盾 / M2.5 前空池注释 / LegacyPool 成员描述失实）|§一 P2-7 各子项引用|逐处加「截至 commit/日期」标注或改正；建立改清单必回写纪律|
|7|Low|FINAL-REPORT-20260824.md:29|观者开关勘误只写在 DEVLOG.md:751，权威报告原文未标注，单读 FINAL-REPORT 仍会得出幽灵 API 存在的结论|两文件对照|在 FINAL-REPORT 原位补一行勘误指向 0c2ac26/bd6c539|
|8|Low|WORKPLAN-20260825.md:61|Thunderclap 悬项未勾销（本次已代为闭环：jar 实证 red/ThunderClap.class 存在，notInJar 系大小写伪象）|unzip -l 输出|勾销待办并把结论回写 legacy-audit|
|9|Low|（仓外）live mods/Rewind|「Rewind 兼容 Cecil 补丁 ✅ 验证通过」repo 层面零留痕：无提交、无脚本、无目标 dll 哈希，换机/重装即失传且不可审计|git log -S Rewind；全仓 grep Cecil/isChangingOwners 仅 NuGet 缓存命中|把 Cecil 补丁脚本入 research/tools/ 并记录 Rewind.dll 原/新哈希|
|10|Low|mod/_staging/louse-extracted-data.md|session4 收尾项「clean _staging」历经三轮审计仍未执行|ls 实测|删除或将内容并入 research/sts1data/|

## 八、总评

历史审计的「发现」侧可信度高（抽验的提交号/行号/字节数/计数无一虚报），「收敛」侧有明显系统性弱点：**修复往往只处理被点名的那个实例，同模式的兄弟实例和交叉引用文档留在原处**——Feed 之于 LessonLearned、Unplayable 家族之于 Exhaust 剥离、FINAL-REPORT 之于 DEVLOG 勘误、Tempest 注释之于 ToolsOfTheTrade 教训，全是同一个惯性。另外验证基建自身开始产生错误结论（coverage.js 的 ThunderClap id bug 把 48/48 报成 47/48 并被写进晨间汇总当作「RNG 观察项」），在冻结归档前值得把这份报告的 #1/#2/#3 三个小修一并收掉。
