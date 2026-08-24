# DEVLOG 审计报告 — devlog-audit-20260825

- **对象**：`DEVLOG.md`（HEAD=`50f8e29`，823 行）全部需求与结论 vs 仓库现状。
- **方法**：只读静态核查。证据源 = 仓库代码、`git log/show`、引擎反编译快照 `research/engine-dllsrc/`、BaseLib 反编译 `research/baselib-dll/`、javap 转储 `research/sts1-javap/`、jar 实测（`G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar` 现场解包/javap 反汇编）、live 部署物 md5。未运行游戏、未构建。
- **行号说明**：DEVLOG 引用的引擎行号以 `.tmp/dllsrc`（v0.111.0 源树）为准；本审计在 `research/engine-dllsrc`（同一二进制的反编译）复核，个别 ±10 行内偏差已在备注注明，语义全部一致。

---

## A. 需求追踪表（27 条）

| # | 日期 | 需求原文摘录 | 实现载体 (commit/文件) | 状态 | 证据 |
|---|------|-------------|------------------------|------|------|
| A1 | 08-20 | "Efficiency pass — the user asked for this explicitly AFTER the security review" | Session4 效率审查：Clash/SignatureMove IsPlayable 重写 | 已实现 | `mod/Spire1Code/Cards/Clash.cs:21` = `PileType.Hand.GetPile(Owner).Cards.All(...)` |
| A2 | 08-20 | 会话收尾项 "clean `_staging`" | mod/_staging 清理 | 部分 | `mod/_staging/louse-extracted-data.md` 至今仍在盘上 |
| A3 | 08-21 | 用户要求三份库接口文档常驻 `docs/`（§5.8 "Per user request"） | docs/BaseLib-API.md 等 3 份 | 已实现 | `wc -c` 实测 109997 / 93215 / 136786 字节，与 DEVLOG 记录逐字节一致 |
| A4 | 08-21 | BaseLib 3.4.5 为唯一运行时依赖（用户下载官方 zip 安装） | `Spire1.json` deps + live mods/BaseLib | 已实现 | `mod/Spire1.json:10-12` 恰为 `[{"id":"BaseLib","min_version":"3.4.5"}]`；`mods/BaseLib/BaseLib.json` `"dependencies": []`；live 目录存在 |
| A5 | 08-21 | 启动事故处置：3.3.5 备份目录移出 mods/（§8.3） | 目录操作 | 已实现 | live `Slay the Spire 2/mods/` 下无 BaseLib-3.3.5-backup（ls 实测），仅 BaseLib/Spire1 等 |
| A6 | 08-21 | 用户报告 Silent 全开崩溃 → 根因修复 | `3806762` + run-history 占位图标 40 对 | 已实现 | `Monsters/Spire1Encounter.cs:54-56` CustomRunHistoryIconPath 覆写；`DEVLOG-crash-snapshot.txt` 已入库（git ls-files）；repo `images/run_history/` 110 文件 |
| A7 | 08-23 | 用户限制评审并发 ≤3 | 流程遵守（12.2 "concurrency capped at 3 per user"） | 已遵守 | DEVLOG.md:509 |
| A8 | 08-23 | 用户决策：放弃自研地牢呈现，转向生态互补层（Session 14） | DEVELOP §0 改写、`84bc1f9` 收编遗留 | 已实现且已执行到 P1 全链路 | `DEVELOP.md:8` "DIRECTION PIVOT (session 14, user decision)"；P1 冒烟见 DEVLOG §15 战果#4 |
| A9 | 08-23 | 生态四件套订阅并下载到位（用户动作） | workshop 本地化目录 | 已实现 | `G:/steam/steamapps/workshop/content/2868840/{3746969593,3747537811,3787796638,3785039319}` 四目录实测存在 |
| A10 | 08-23 | 兼容补丁随 Spire1 发布承载、AFTP 冻结（用户指令，写入 DEVELOP §0） | DEVELOP 条款 + AutoSlayModdedScreenHandlersPatch | 已实现 | `DEVELOP.md:11`；`Patches/AutoSlayModdedScreenHandlersPatch.cs:199-200`（含 TotalFloor 49→120 transpiler） |
| A11 | 08-23 | 对外沟通必须披露 agent 参与（用户指令，入 DEVELOP §5） | DEVELOP 条款 | 已实现 | `DEVELOP.md:79` "(user directive … ox-alpha … Twelve-eight)" |
| A12 | 08-23 | 层数勘误（用户要求核实后修正） | 内部结论更正 + 对外评论 PATCH | 内部已实现；外部评论无法静态核验 | 勘误依据成立：`research/sts1-javap/AbstractDungeon.txt:157 MAP_HEIGHT=15`；补丁注释含层数数学 `AutoSlayModdedScreenHandlersPatch.cs:163-173` |
| A13 | 08-23 | 第二条发言撤稿致歉（用户指令） | 第三条评论文稿代拟 | 草稿就绪，外发属用户动作 | `.tmp/issues/aftp10-third-comment.md` 存在；同目录另有 act4heart/megacrit 两份草稿 |
| A14 | 08-23 | 用户报告"包扎没卡图"→ 302 张小卡面替换真美术 | `5588f9f` | 已实现 | `mod/Spire1/images/card_portraits/*.png` 332 张、<2KB 占位 0 张（find 实测） |
| A15 | 08-23 | 用户目验 17+ 铁甲卡无图 → big 大图重生成（战果#7） | `465efe9` | 已实现 | `images/card_portraits/big/` 332 张、<2KB 0 张 |
| A16 | 08-24 | Splash 语义修正（用户实机反馈候选集漂移） | SplashOwnSetSubtractPatch | 已实现 | `Patches/SplashOwnSetSubtractPatch.cs` 存在（按 Id.Entry 集合差方案） |
| A17 | 08-24 | SPIRE1-WATCHER 归档（用户指令：AFTP 生态已有成品 Watcher） | `f2f3305` 配置门禁 → **`0c2ac26` 移除开关改永久隐藏** | 已实现但与 DEVLOG 记录漂移 ⚠ | `Character/Watcher.cs:28-33` 注释 "ARCHIVED (**no config switch by user decision**)"，Hide=true 硬编码；`git log -S EnableSts1Watcher` 显示 f2f3305 加、0c2ac26 删 |
| A18 | 08-24 | 商店购买守卫（用户实测定位 autoslay 空转 ~1 分钟） | ShopEnoughGoldGuardPatch 终版（b8d530d） | 已实现 | `AutoSlay/ShopEnoughGoldGuardPatch.cs` 挂 MerchantEntry.EnoughGold postfix；引擎点 `MerchantEntry.cs:32` |
| A19 | 08-24 | 尘封魔典机制定罪与修复（用户报告"发的牌是封印王座"） | DustyTomeAncientFallbackPatch（03ae5d1/7c98579）+ DebugRelicInjectPatch 实测链 | 已实现+冒烟实测通过 | `Patches/DustyTomeAncientFallbackPatch.cs` 存在；NRE 链前提成立（`Rng.cs:289-300` 空集返回 default，NRE 在解引用处——见 DEVLOG 自我修正段） |
| A20 | 08-25 | GA 双修正（用户实锤"遗传算法不该是红色牌"+数值仲裁） | `af6d1d7` | 已实现且 jar+loc 双源吻合 | `Cards/GeneticAlgorithm.cs:18 [Pool(typeof(DefectCardPool))]`；jar 含 `com/megacrit/cardcrawl/cards/blue/GeneticAlgorithm.class`；官方 eng 文案逐字="Gain !B! Block. Permanently increase this card's Block by !M!. NL Exhaust."（本次现场解包验证） |
| A21 | 08-25 | 无视 mod 差异联机补丁，默认开 | `d0181a0` MpIgnoreModDiffPatch | 已实现 | `Config/Spire1Config.cs:59 IgnoreMpModDifferences=true`；三闸定位成立（HandshakeManager.cs:117 版本串→VersionMismatch / :124 清单→ModMismatch / :129 哈希→VersionMismatch） |
| A22 | 08-25 | 地图页跳过节点按钮，默认开（火堆黑屏自救） | `41e7acc` SkipNodeButtonPatch | 已实现；真人可视/点击验证待做 | `Config/Spire1Config.cs:65 EnableSkipNodeButton=true`；`Patches/SkipNodeButtonPatch.cs:53-55` SetTravelEnabled(true)+反射 RecalculateTravelability+原生投票管线 |
| A23 | 08-25 | 清单版本回退根治（csproj 覆盖 bug，源头改版号） | `cb70f82` | 已实现 | `mod/Spire1.json:6 version=0.9.1`；live json 同为 0.9.1（cat 实测） |
| A24 | 08-25 | 观者卡牌退出总览（Card Library 门控） | `bd6c539` ArchivedCharacterGatePatch | 已实现 | `Patches/ArchivedCharacterGatePatch.cs:45 ArchivedPools={typeof(WatcherCardPool)}`；引擎钩子 `CardModel.cs:829 ShouldShowInCardLibrary` + `NCardLibraryGrid.cs:162` 过滤均存在 |
| A25 | 08-25 | KB 知识库落库（KBBuilder 产出） | `566552e` research/sts1-kb/ | 已实现 | 目录恰 15 文件（含 build_kb.mjs）；计数逐项命中：紫 77、遗物 186、药水 43、事件 54（jq 实测） |
| A26 | 08-25 | 0.9.1 发布闭环（dll/pck/json 三件套+三 zip） | dist/ 三包 | 已实现 | live md5 实测 dll=`c2d99b10…` pck=`40025b18…` 与 DEVLOG 记载一致；json 0.9.1；`dist/Spire1-{Ironclad,Silent,Defect}.zip` 存在 |
| A27 | 08-20 | Session-3 遗留两项收尾：Mushrooms 授寄生虫、DrugDealer 解锁 J.A.X. | session4 phase1 | 已实现 | `Events/Mushrooms.cs:45 AddCurseToDeck<Parasite>`；`Events/DrugDealer.cs:35 Option(TestJax)`、`:51 CreateCard<JAX>` |

---

## B. 结论验证表（45 条）

| # | 结论原文摘录 | 当前判定 | 证据 | 备注 |
|---|-------------|---------|------|------|
| B1 | `RelicFactory.RollRarity` 只返回 Common/Uncommon/Rare，Event 遗物永不入随机池（RelicFactory.cs:80-93） | **成立** | engine-dllsrc/MegaCrit.Sts2.Core.Factories/RelicFactory.cs:80-93（三元仅三种返回） | 行号逐字命中 |
| B2 | RelicImagePath 缺图回退占位 relic.png（StringExtensions.cs:49-65） | 成立 | mod StringExtensions.cs:49-65（RelicImagePath+BigRelicImagePath 双双回退） | 行号命中 |
| B3 | TryModifyRewards(:2140) 是加金忠实钩子；ModifyGoldGained 对每次 GainGold 触发（PlayerCmd.cs:144） | 成立 | AbstractModel.cs:2140；PlayerCmd.cs:141-149（ModifyGoldGained 于方法首行、早退 ：146-149） | GainGold 实际起于 ：141 |
| B4 | AfterGoldGained(:767) 在 amount>0 早退之后，零收益不触发 | 成立 | AbstractModel.cs:767 + PlayerCmd.cs:146-149 早退在前 | |
| B5 | ModifyCardPlayCount(:1495)/AfterModifyingCardPlayCount(:851) 打出次数钩子对 | 成立 | AbstractModel.cs:1495/:851 行号逐字命中 | |
| B6 | GetResolved() X-cost 返回 CapturedXValue；GetWithModifiers(All) 对 CostsX 早退 _base | 成立 | CardEnergyCost.cs:155-162 与 ：105-107（`if (CostsX) return num;`） | Madness/Splash 均依赖此语义 |
| B7 | FromChooseACardScreen 超 3 张抛异常（:252） | 成立 | CardSelectCmd.cs:252-256 "Only works with less than 3 cards" | |
| B8 | AddGeneratedCardToCombat(:267)；Random 位置经 Rng.Shuffle（:508-511） | 成立 | CardPileCmd.cs:267 签名、:510 Random→Rng.Shuffle.NextInt | |
| B9 | 伤害截断不四舍五入（Creature.cs:449 Math.Clamp(int)） | 成立 | Creature.cs:449 `(int)Math.Clamp(amount, 0m, 999999999m)` | 行号命中 |
| B10 | Player.GetRelic<T>() 在 Player.cs:532 | 成立 | Player.cs:532 `public T? GetRelic<T>()` | |
| B11 | CanonicalVars 每实例惰性读一次（CardModel.cs:538-549）；缓存共享会成 bug | 前半成立；后半推理成立 | CardModel.cs:540-550 lazy getter（null 才建）；DynamicVarSet.cs:14 按**引用**存 Dictionary、:24-32 类型访问器=同字典查找 | SetOwner 行未复读，但引用存储可见，推理链完整 |
| B12 | N'loth's Gift "NO hook exists…不可实现"（Session 4） | **不成立（已被推翻）** | CardRarityOdds.cs:83 `public RollWithoutChangingFutureOdds`；DEVLOG §13 AFTP Transpiler 实证；DEVELOP.md:9 | DEVLOG 自身在 §5.4/§13 已记录反转；代码注释仍陈旧（见 D2） |
| B13 | FaceTrader.getRandomFace=未拥有五脸按序收集+Circlet 兜底+miscRng.randomLong shuffle 取[0]；Trade 分支免费 | **成立（本次新鲜反汇编）** | jar `events/shrines/FaceTrader.class` getRandomFace 全量字节码（hasRelic×5→size==0 加 Circlet→new Random(miscRng.randomLong())→shuffle→get(0)）；Trade 分支仅 logMetricObtainRelic+spawnRelicAndObtain，无 damage/gainGold | 与 DEVLOG §5.7 描述逐点吻合 |
| B14 | Madness 用 EnergyCost.SetThisCombat(0)，不用 SetToFreeThisCombat（后者多清星费） | 成立 | CardModel.cs:1273-1276（SetToFreeThisCombat 额外 SetStarCostThisCombat(0)）；Cards/Madness.cs:28-31 同款论证注释 | |
| B15 | FaceOfCleric 挂 AfterCombatVictory(:556) 非 AfterCombatEnd(:520)；GainMaxHp 内部 Heal | 成立 | AbstractModel.cs 两钩子均在（:520/:556 区间）；CreatureCmd.cs:841 GainMaxHp→内部 `await Heal(creature, num)` | DEVLOG 称 Heal 在 ：853，实测 :854，±1 |
| B16 | CultistMask 无任何机制效果（纯外观） | 成立 | Relics/CultistMask.cs 全文仅 Flash()+TalkCmd.Play，无常量无数值 | StS1 侧亦无机制（face-relics json） |
| B17 | NlothsMask 扣遗物但宝箱照付金币：进场 BeginRelicPicking 先消费 ShouldGenerateTreasure，开箱路径再见已消费即付金 | 成立 | TreasureRoom.cs:47 BeginRelicPicking；TreasureRoomRelicSynchronizer.cs:105 Hook.ShouldGenerateTreasure 闸；OneOffSynchronizer.cs:128-138 再问一次→true→GainGold | 三点链路与描述完全一致 |
| B18 | SsserpentHead 必须判 MapPointType.Unknown 而非房间类型（?节点提前定型） | 成立 | RunManager.cs:985 switch：Unknown→Odds.UnknownMapPoint.Roll（进入房间前已定型） | |
| B19 | act.Index=-2 安全：引擎仅在 ModelDb.cs:334 if(Index>=0) 后读 Index | 成立（抽查） | ModelDb.cs:330-338 该 guard 存在 | "仅一处"未做全库穷举，抽查未见他处 |
| B20 | 单人选幕=改 StartNewSingleplayerRun 的 acts 参数；多人第二调用点 NCharacterSelectScreen.cs:745 | 成立 | NCharacterSelectScreen.cs:744-747 多人路径直调 RunState.CreateForNewRun | |
| B21 | MoveState 无 FollowUpState 时抛 "No valid followup state." | 成立 | MoveState.cs:69 `FollowUpState?.Id ?? throw new InvalidOperationException("No valid followup state.")` | 异常文本逐字一致 |
| B22 | RollMove 从 RunRng.MonsterAi 抽取 | 成立 | MonsterModel.cs:418 `RollMove(targets, Creature, RunRng.MonsterAi)`；RunRngSet.cs:77 | |
| B23 | AngryPower.onAttacked 门控 owner!=null && dmg>0 && !HP_LOSS && !THORNS | **成立（javap 反汇编）** | desktop-1.0.jar AngryPower.class：ifnull(DamageInfo.owner)/ifle(amount)/if_acmpeq HP_LOSS/if_acmpeq THORNS 四连跳转 | ReviewGremlins P1 REJECTED 的仲裁正确 |
| B24 | --autoslay 契约需 !IsReleaseGame()+HasArg("autoslay")（NGame.cs:694） | 成立 | NGame.cs:694 逐字命中 | runTimeout=25min 未单独复核 |
| B25 | SelectionScreenPrompt 缺键即 throw（修复#8 根因） | 成立 | CardModel.cs:129-137 InvalidOperationException("No selection screen prompt…") | |
| B26 | 卡面主图走 big 槽：BaseLib 把 PortraitPath getter 重定向到 CustomPortraitPath（战果#7 根因） | 成立 | baselib-dll BaseLib.Abstracts/CustomCardPortraitPath.cs:23-27 postfix；我方 Cards/Spire1Card.cs:18 override CustomPortraitPath→BigCardImagePath | DEVLOG 引用源树行号 268-311，反编译树行号不同、机制相同 |
| B27 | ShouldGenerateTreasure 是 veto 型宝箱总闸（AbstractModel.cs:2325） | 成立 | AbstractModel.cs:2325-2329 bool 属性钩子 | |
| B28 | 商店守卫挂点 EnoughGold ⇒ Cost<=_player.Gold | 成立 | MerchantEntry.cs:32 | |
| B29 | 握手三道闸：版本串不符→VersionMismatch；清单不符→ModMismatch；哈希不符→VersionMismatch | 成立 | HandshakeManager.cs:117/:124/:129 三处 return | d0181a0 的放行策略与其对应 |
| B30 | "[ERROR] Act 4 is not yet implemented" 为良性日志（cs:579 case 3） | 成立 | ProgressSaveManager.cs:579 Log.Error($"Act {act+1} is not yet implemented.") | 逐字命中 |
| B31 | ShouldShowInCardLibrary 是 getter 且 NCardLibraryGrid 入册过滤 | 成立 | CardModel.cs:829 getter；NCardLibraryGrid.cs:162 if 过滤 | bd6c539 前提成立 |
| B32 | GenerateAllEncounters 为抽象必须声明；BaseLib 后缀附加自定义遭遇 | 成立 | ActModel.cs:285 abstract；baselib-dll Baselib.Patches.Content/AddActContent.cs 引用之 | |
| B33 | 引擎共 121 个原版怪物类 | 成立 | engine-dllsrc/MegaCrit.Sts2.Core.Models.Monsters/ 计 121 .cs | 数字精确 |
| B34 | StS2 只带 RedMask/FuneraryMask/JeweledMask/GremlinHorn（+Circlet），五脸 0 存在 | 成立 | engine relics 目录 grep 仅上述四文件命中，无 CultistMask/FaceOfCleric/GremlinMask/NlothsMask/SsserpentHead | FaceTrader 锁定论证的搜索结论复现 |
| B35 | CustomMonsterModel 具备 CustomVisualPath/CreateCustomVisuals/SetupCustomAnimationStates/音效属性 | 成立（行号随工件版本偏移） | baselib-dll BaseLib.Abstracts/CustomMonsterModel.cs:13/:26/:31/:36 | DEVLOG 引 v3.4.5 源树行号，本审计对 v3.3.5 反编译树 |
| B36 | RelicCmd 表面 = Obtain<T>/Obtain(idx=-1)/Remove/Replace/Melt | 成立 | RelicCmd.cs:22/:35/:61/:74/:89 五成员齐全 | ForgottenAltar 用 Replace 的前提成立 |
| B37 | docs 三份 API 文档 109997/93215/136786 B | 成立 | wc -c 实测逐字节相等 | |
| B38 | face-relics-and-madness.json = 20547 B 且字段结构经校验 | 成立 | wc -c 相等；文件存在于 research/sts1data/ | 结构校验未重跑，大小精确 |
| B39 | JmcModLib.Runtime.xml = 206083 B（602 documented members） | 成立（大小）；602 未复算 | wc -c 相等（workshop 目录实测） | 成员计数与零内容面结论未逐条重扫 |
| B40 | KB 计数：紫 77 / 遗物 186 / 药水 43 / 事件 54 | 成立 | jq 实测四值逐一相等 | |
| B41 | events.json 656 键（S5.7 时点）；cards.json 673(S4)/675(S5.7) | events.json 成立（今仍 656）；cards.json 现 677 | jq 实测 eng/zhs 均 events=656、cards=677 | cards 增长属后续正常演进，非矛盾 |
| B42 | 遭遇账目：Act1 20 + Act2 17 + Act3 17 + Ending 2 = 56 | **小漂移**：盘上 55（act2=17、act3=16、act4=2，其余 20 无显式 HomeActs） | Encounters/*.cs grep HomeActs 统计 | 与 §12.2 "weak variants deleted" 相符；§12.1 的 "17 encounters" 未随之更新 |
| B43 | S10.1 "111 duplicated cards → Spire1LegacyPool" | 历史时点值无法复算；现盘 76 | `grep -F '[Pool(typeof(Spire1LegacyPool))]' Cards | wc -l` = 76 | 其间 8781855（恢复 12 张）、PureSts1Pools 等多轮池调整，非矛盾但数字不可比 |
| B44 | S11.2 "331/331 mapped"（卡面） | 文档内自我纠正的漂移（已闭合） | DEVLOG 战果#5/#7 自曝 302 张 ~314B 占位并重生成；现盘 332/332 无 <2KB | DEVLOG 已把教训固化为"已映射声明必须附尺寸/字节数" |
| B45 | S9.2 zhs 遗物映射 34/37 official names | **隐性缺口**：仅数据准备，从未落盘 | `zhs/relics.json` 自 a26fbc7 至今为 `{}`（3B）；Relics/*.cs 零中文字符串（grep -P [\x{4e00}-\x{9fff}] = 0）；遗物名实为代码内英文 RelicLoc（baselib RelicLoc record） | 中文玩家遗物名将回落英文；DEVLOG 后续会话再未提及此缺口 |

---

## C. 漂移项汇总（文档声称 X，代码是 Y）

1. **观者归档门禁（A17/B 类）**：DEVLOG（2026-08-24 夜间批次）称 "`Spire1Config.EnableSts1Watcher=false` 默认"；现状 `Watcher.cs:28-33` **硬编码永久隐藏且无任何配置开关**，注释明确 "no config switch by user decision"。演变链：f2f3305（加开关）→ 0c2ac26（"remove watcher switch"，commit message 有记录）。判定：代码比文档新、方向由后续用户裁定驱动，属**文档滞后**而非实现缺陷。
2. **N'loth's Gift 可实现性（B12/D2）**：S4 断言"不可实现"已被 S5.4/S13 推翻（`CardRarityOdds.RollWithoutChangingFutureOdds` public 缝真实存在，本审计独立确认）。但 `Events/Nloth.cs:26` 类注释仍写 "NlothsGift is not implementable"——**注释陈旧**，与 DEVELOP §0/§9 的现行结论相抵。
3. **遗物中文本地化（B45）**：DEVLOG §9.2 给人以"34/37 已映射"的进展印象；实际落盘为零（zhs/relics.json={}，代码零中文）。数据准备 ≠ 交付。
4. **遭遇总数（B42）**：§12.1 各幕合计隐含 56，盘上 55（Act3 少 1，系同日 §12.2 删除弱变体所致，前文未回填）。
5. **STATUS 头严重滞后**：文件头部 STATUS 停留在 session 6（"M2 monsters in flight…6 subagents writing right now"），而正文已完成至 M15/0.9.1 发布。活文档特性，但对"Resumable with zero prior chat"的自我要求构成误导风险——新读者应以正文末尾为准。
6. （非漂移，记录以免误报）LegacyPool 111→76、cards.json 673→677 均为时点快照差异，有明确 commit 链解释。

---

## D. 自我标记"未验证/待实证"条目及现状

| # | 出处 | 待验证内容 | 现状（本次审计判定） |
|---|------|-----------|---------------------|
| U1 | S4 验证节 | 冒烟清单 1-7（Cursed Tome Take、Forgotten Altar 替换、Odd Mushroom +25%、TombRed Mask 金额标题、Council/Vampires/Nest/Mausoleum/Mushrooms 遗留项） | **仍未正式逐项实机验收**。autoslay 自然局间接覆盖了事件 handler 与部分奖励路径，但无逐项目击记录 |
| U2 | S5 验证节 | 脸面具清单 1-6（FaceTrader 免费、GremlinMask 弱自身、FaceOfCleric 胜后+1、NlothsMask 金付 relic 无、SsserpentHead ?节点 50 金、WindingHalls 2×Madness） | **同上，无逐项实机记录**；代码层全部核实无误（B13-B17） |
| U3 | §5.3 | RitsuLib character skins unproven `[INFERENCE]` | 随 §6.2 弃用 RitsuLib 而**失效（问题消失）** |
| U4 | §5.3 | FreePlayBindingRegistry ordering `[UNVERIFIED]` | 同上失效；Necronomicon 最终走引擎原生 `SetToFreeThisTurn`（CardModel.cs:1267-1271 已核） |
| U5 | §6 验证节 | "NOT yet rebuilt with Exordium+patch" | 已闭合：S7 起 Debug+Release 构建绿并多次部署（20:59 等） |
| U6 | 2026-08-24 夜间 | DustyTome "AfterObtained GetById(null) 行为待实证" | 已被自我修正（NRE 实发于 SetupForPlayer 内部，03ae5d1 早已修）+ 冒烟批次 DebugRelicInjectPatch 实测全通（sozu-ban 铁证） |
| U7 | 2026-08-25 夜间 | 火堆黑屏真根因 | **未决**（原始日志轮转丢失）；缓解=跳过按钮上线；复现协议已定（先拷 logs 再杀进程） |
| U8 | E2/E1 实验 | SpeedX NaN 真凶 | AutoProceed/turbo 双双洗清（3250 vs 3279；3711 vs 3609）；后 P1SMOKE4/5 NaN=0、P1SMOKE9 "8 胜 0 崩、NaN 全零"——**经验上消失，机制未定罪**（低优先级挂账合理） |
| U9 | seed2b 尸检 | 回主菜单资源加载失败（肖像类 mod 冲突嫌疑） | 未决，明示"非我方层问题，独立待查" |
| U10 | 深夜追加 | 跳过按钮可视/点击真人验证 | 待做（Godot 无 UI 自动化，DEVLOG 已如实标注） |
| U11 | 待办移交 | Thunderclap jar 归属复核、CodeOpt 流、覆盖 drain 尾巴 | 明示未动（本审计未发现相关新改动，属实） |
| U12 | §15 | SpeedX 联系作者、MegaCrit/Act4Heart 草稿递交 | 用户动作项；三份草稿齐备于 `.tmp/issues/` |
| U13 | §5.5/代码 | Girya rest-site Lift 选项未接线 | FLAG 仍在：`Relics/Girya.cs:27`（被动版已有，成长途径缺失） |
| U14 | §10.3 | AcidSlimeS maxRepeats=1 与 StS1 base 自由 50/50 的偏差 | LOW 挂账保留（cosmetic deviation） |
| U15 | §10.3/待办 | run_history 110 张占位图标 | 已知低影响缺口（StS1 无官方图标源，宁缺勿造）；repo images/run_history 实测 110 文件 |
| U16 | P1SMOKE4-r1 | SimpleLoc 同源竞态 | 补丁护栏在位（a598f65/dd943c8/3ebbab0），r2/r3 未复现，覆盖矩阵完成 |
| U17 | P1SMOKE 系列 | 四角色完整胜利覆盖矩阵 | **已闭合**：P1SMOKE8 五角色+官方 Defect 加映全胜，P1SMOKE9 累计 8 胜 0 崩 |

---

## 总评

- 抽验的 **34 个提交哈希全部真实存在**；live 部署物 md5、docs 字节数、KB 计数、jar 反汇编（AngryPower 门控、GA 文案、FaceTrader 逻辑）、引擎行号断言等**高价值结论无一虚报**，多处达到逐字节/逐行号精度。
- 发现的真实问题集中在**文档时效**而非技术错误：观者归档开关描述滞后（C1）、Nloth.cs 陈旧注释与现行结论相抵（C2）、遗物中文本地化只备未发（C3）、遭遇计数未回填（C4）、STATUS 头过期（C5）。
- 所有"BE HONEST ABOUT THIS"声明的诚实性经交叉核验成立：该标 UNVERIFIED 的都标了，事后被推翻的结论均有留痕反转记录。
