# sts2-spire1 停止开发审查总报告

> 审阅基线：HEAD = `5358e41`（112 commits）。日期：2026-08-26。
> 方法：3 个并行 reviewer subagent（代码质量 / 架构设计 / 历史复查）+ 主会话独立交叉验证（每条 High/Med 发现均经反编译源、git 历史、运行日志二次取证）。三份原始报告：`.tmp/review-code.md`、`.tmp/review-arch.md`、`.tmp/review-hist.md`。

## 一、裁定成立的问题（按严重度）

### High（4 条，全部实锤）

| # | 问题 | 证据 | 交叉验证 |
|---|---|---|---|
| **H-1** | **Register() 非 pure 分支丢失 Ironclad/Defect 孪生注入 → ROOM_FULL_OF_CHEESE 崩溃在默认配置回归**。commit `3a0de3d` 重构时把 `foreach IroncladReuse → Spire1CardPool`、`foreach DefectReuse → DefectCardPool` 两行连同 pure 分支一起删了，非 pure 路径只剩 Silent/Colorless 两行。铁甲池现役 Common 仅 6 张（Cleave/Clothesline/Flex/HeavyBlade/Warcry/WildStrike），该事件要求 8 张不重复 Common（`dllsrc RoomFullOfCheese.cs:38-41`），黑名单耗尽即抛 InvalidOperationException——DEVLOG 711-714 记录过、`3deabac` 修过的崩溃原样回归。RewardClampPatch 仅 pure=true 生效，兜不住默认路径 | `git show 3a0de3d^` 四行注入 vs 现两行；铁甲 Common 计数=6 | ✅ 主会话重数确认 6<8 |
| **H-2** | **MpIgnoreModDiffPatch 整体无效 + 日志谎报**：`AllowThrough(HandshakeResult __result, ...)` 缺 `ref`。HandshakeResult 是 struct（`dllsrc HandshakeResult.cs:5`），Harmony postfix 按值注入副本，`__result.status = Success` 改副本即丢弃——ModMismatch/哈希不符实际仍被引擎拒绝，但日志打 "forced through"。DEVLOG `d0181a0` 宣称的"联机握手放行修复"从未生效 | dllsrc struct 声明；Harmony 注入语义 | ✅ 主会话读签名确认无 ref |
| **H-3** | **RestSiteLightingRescuePatch 救援无效**：注入节点只设 `Name="RestSiteLighting"`，未设 `Owner`+`UniqueNameInOwner` → Godot `%` 场景唯一名不注册 → 引擎 `NRestSiteRoom._Ready` L324 `GetNode("%RestSiteLighting")`（非 OrNull）照样抛。黑屏只是从 L321 挪到 L324，还多两条"已救援"误导日志。同仓 RestSiteBackgroundPatch.cs:57-58 有正确配方未照抄 | 引擎 L324 + 同仓正确配方对照 | ✅ 主会话读两文件对照 |
| **H-4** | **三个用户可见开关是死开关**：`EnableSts1Characters/Cards/Relics`（及 helper `CharactersEnabled/CardsEnabled/RelicsEnabled`）全库零消费者。角色可见性由 character.txt 控制、无色卡注入无条件、遗物走 [Pool] 自动注册；BaseLib 设置界面把三个死开关渲染给玩家。DEVELOP.md §2c "master gates all" 承诺落空 | 全库 grep 仅命中 Config 定义处 | ✅ 主会话 grep 确认零引用 |

### Med（8 条实锤）

| # | 问题 | 证据 |
|---|---|---|
| **M-1** | **coverage.js THUNDERCLAP 蛇形化 bug**：`snake('ThunderClap')=THUNDER_CLAP` 永不匹配日志实际 `THUNDERCLAP`（官方 id 无下划线）→ 晨间汇总 "IRONCLAD 47/48 缺 ThunderClap（RNG 观察项）" 是记账错，真实覆盖 48/48 | `.tmp/night/coverage.js:31-59`；日志实测含 THUNDERCLAP |
| **M-2** | **Feed.cs:30 谓词取反未修**：`3cfbcf1` 只修了 LessonLearned；Feed 仍 `All(p => !p.ShouldOwnerDeathTriggerFatal())`，与官方 Feed（正谓词）语义相反——带死亡爆炸 power 的敌人被击杀时力量成长不触发 | 官方 `dllsrc Feed.cs:38` 正谓词 vs 我方取反 |
| **M-3** | **PureWater 效果实现错误**：官方"战斗开始加一张 Miracle 进手牌"（KB relics.json），我方实现"+2 能量"。注释自称"复用 Lantern 钩子无需发明效果"——是对官方效果的误读 | KB 双语描述 vs `PureWater.cs:33-39` |
| **M-4** | **MarkOfPain 能量时点错**：官方"每回合开始 +1 能量"，我方 BeforeCombatStart 单次 +1——Boss 遗物被实质削弱。FLAG 只覆盖 Wounds 未接线，能量时点偏差不在声明内 | KB 描述 vs `MarkOfPain.cs:21-26` |
| **M-5** | **Armaments 升级 +3 Block 违官方**：StS1 官方升级只改"升级一张→升级全部"，Block 恒 5（`cards-red.json` upgraded_description_diff 无 !B! 变化）；StS2 shipped 版同样无增量。`3a0de3d` 声称"官方 5→8"是错误记忆——昨晚当 bug 修的东西本身就是错修 | KB 升级 diff + dllsrc Armaments.cs 双源 |
| **M-6** | **DarkShackles 双注入**：官方 ColorlessCardPool.GenerateAllCards 已含该卡，我方 ColorlessReuse 又注入一次；ConcatModelsFromMods 盲 Concat 无去重 → 无色奖励候选权重翻倍 | `dllsrc ColorlessCardPool.cs:38` + ModHelper.cs:74-92 |
| **M-7** | **zhs 遗物表缺 36 条 .flavor**：eng 108 键 vs zhs 72 键，缺失全为 flavor。`8f69a72` 宣称 closes audit C3 实为半关闭 | 键集对账 |
| **M-8** | **AFTP/官方 shared 事件乱入一代楼层（用户点名，未修）**：根源在引擎 `ActModel.GenerateRooms` L334 `AllEvents.Concat(ModelDb.AllSharedEvents)` 硬拼接 18 个二代 shared 事件，自研幕无法过滤 | dllsrc ActModel.cs:334 |

### Low（合并 8 条：主会话+三 reviewer）

- **L-1** ResolveOwnImplementation 大小写敏感漏 3 张：`Sts2Cards.Afterimage→Cards.AfterImage`、`CreativeAi→CreativeAI`、`Ftl→FTL`——pure 模式 Silent 少 AfterImage(Rare)、Defect 少 CreativeAI+FTL（主会话独立发现）
- **L-2** PureSts1Adds 死代码：156 行定义、21 个元组，`3a0de3d` 后零引用（主会话独立发现）
- **L-3** DebugCardInjectPatch/DebugRelicInjectPatch 硬编码开发机绝对路径编译进发布 0.9.1 dll
- **L-4** Rewind Cecil 兼容补丁不入版本控制：全仓无脚本，DEVLOG:866 一行文字，换机不可复现
- **L-5** Girya StrengthBonus 是 C# 属性非 DynamicVar → 不进存档序列化（FLAG 只声明了举铁未接线）
- **L-6** SkipNodeButton tooltip 硬编码中文（英文客户端出中文 tooltip）；zhs settings_ui 有拼写错误垃圾键 DIFERENCES
- **L-7** AFTP fork 漂移 3 提交含 "I do not remember tbh"（94155c1）无法溯源
- **L-8** min_game_version=0.107 vs 按 v0.111 编译的 API 面不匹配；Tempest 注释张冠李戴；RitualDagger 注释反向过期；DEVELOP.md 四处文档漂移；ShiftingPower 缺 participants 门控

## 二、历史声明复查结论（HistAudit 全量核验）

- **修复声明真伪**：抽验 22 条 DEVLOG"已修"声明——提交号/哈希/计数全部命中，**无虚报**；终态部署 dll `aa8b4f33`/pck `65006e85` 与三 zip 六项全等。
- **但修复完整性系统性偏弱**：同病灶尾巴普遍留存（Feed 未随 LessonLearned 修、关键词剥离漏 11 张现役卡、文档勘误只写 DEVLOG 不改原文）。
- **187 局声称保守成立**（22:15 前实数 193）；补丁失败与致命异常全量 grep 均 0；KB 抽样与 jar 官方逐字一致。
- **Thunderclap jar 归属悬项闭环**：`desktop-1.0.jar` 实有 `ThunderClap.class`（大写 C），历史 notInJar 记录系 grep 大小写伪象。

## 三、正面确认（无需整改）

- AutoSlay 补丁家族门控干净（全部 --autoslay/AutoSlayer.IsActive 闸，人类对局零介入）
- DustyTomeAncientFallbackPatch 必要且 RNG 忠实；ArchivedCharacterGatePatch 归档方案正当（ModelID 稳定+显示层门控）
- 配置加载时序无竞态；Harmony 逐类 try/catch 注册稳健；跳过按钮联机语义达标（走原生投票管线）
- 本地化主表完整（七表 eng↔zhs 键集对齐）；引擎事实考据纪律出色（几乎每个补丁附 dllsrc 行号）

## 四、总评

这座工程的结构纪律与引擎考据质量显著高于同类 mod（钩子选型逐条标注引擎行号、数值经 jar/kb 双源仲裁），但**最后一轮大改动（`3a0de3d`）引入了两处静默回归**（默认配置事件崩溃回归 + 联机放行从未生效），加上历史修复的"同病灶尾巴"模式和配置面从未真正接线，说明：**此项目在"大改动后验证改动面之外"这件事上存在系统性缺口**——PoolCensus 日志打了但没人对账，'验证通过'声明依赖单点观察而非池成员终态断言。

若未来重启开发，优先清偿顺序：H-1/H-2（一行级修复）→ H-3（照抄同仓配方）→ H-4（要么接线要么删开关）→ M-2..M-5（对齐官方语义）。

> 三份原始报告（含每条完整证据链与修复建议）：
> - `.tmp/review-code.md`（CodeQuality，7 条）
> - `.tmp/review-arch.md`（ArchDesign，16 条 + 7 条正面确认）
> - `.tmp/review-hist.md`（HistAudit，10 条，含历史逐条 ✓/△/✗/↺/? 裁定）
