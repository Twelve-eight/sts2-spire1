---
name: sts2-spire1-card-audit
description: sts2-spire1（StS1→StS2 BaseLib 移植）卡牌/遗物一致性审计五步法与引擎速查表。当任务涉及卡牌数值仲裁、本地化占位符校验、覆盖审计、联机失同步归责时使用。
---

# 卡牌一致性审计五步法（sts2-spire1）

## 第一步：loc ↔ 实现双向扫描
- 占位符：`!D! !B! !M! !C!` 等（cards/powers 域）与 `{X}`（events 域）分开扫。
- loc 键规则：`SPIRE1-<类名蛇形>`，文件三键组 `title/description/smartDescription` × `zhs/eng`。
  - 力量键格式：`SPIRE1-<SLUG>.title`；类内字符串正则**必须含连字符**，否则空转假绿（踩坑两次）。
- 脚本：`.tmp/audit-event-vars.js`（events 域 {X} 与 DynamicVars 注册名比对）；非 cards 九域已证零占位符。

## 第二步：jar 字节码仲裁（数值唯一权威）
- jar：`G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar`
- 类路径：`com/megacrit/cardcrawl/cards/{red,green,blue,colorless}/<Java名>.class`
  - **注意类名与游戏 ID 不同**：GeneticAlgorithm.class 的 ID 是 "Genetic Algorithm"（带空格）。
- 描述不在 class 里——在 **`localization/`（单数！）** 下：`localization/eng/cards.json`、`localization/zhs/cards.json`。
  zhs 即官方简中原文；描述保留 `!X!` 占位符与 ` NL ` 换行原样。
- 提取套路：unzip 单文件 → `tr -c '[:print:]' '\n'` 或 node/python 解 JSON。勿信记忆（GA"加敏捷"实为官方原文"加格挡"，2026-08-25 实锤）。

## 第三步：引擎源码链路验证
快照在 `.tmp/dllsrc/`（反编译）。高频事实：
- `LocalKeywords` 缓存私有 `_keywords`，首访后永不刷新 ⇒ 动态关键词需基类 `ResetKeywordCache()` 反射重置；
  消耗判定在 `GetResultLocationForCardPlay` 读实例 `Keywords.Contains(Exhaust)`。
- `CanonicalKeywords` 必须 **public override**（protected 会被引擎忽略）。
- `ModelId = Slugify(类名)` 大写蛇形；特例映射表见 DEVELOP.md。
- `AddModelToPool` 在首次生成池时冻结 ⇒ 注入必须早于任何池生成（MainFile.Initialize 里 SharedCardReuse.Register 最先跑）。
- 奖励生成 `GetPossibleCards` 无拥有去重。

## 第四步：运行时探针日志
- `[Spire1]` 前缀 Info 探针打在 OnPlay 等关键路径（例：GA 打 `extra/gain/deck 三元组`）。
- 游戏日志：`C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/logs/godot.log`（最新）+ 时间戳轮转件。
- modded 运行控制台必开（NDevConsole.cs:359），`relic add X` / `draw N` 可做最小复现。

## 第五步：RitsuLib 转储对拍
- 失同步自动产 divergence zip 于 logs 目录；读其中 `state-divergence-report.txt`。
- **先数 differ 标记再下结论**：#563 案例（2026-08-25）全部差异=mod 清单级假阳性
  （BaseLib 本地 vs 工坊来源、非玩法 mod 多寡、分装包名不同），玩家状态逐字段全同。
- 我方缓解已上线：`MpIgnoreModDiffPatch`（握手放行）+ RitsuLib 弹窗抑制（Spire1Config.IgnoreMpModDifferences）。

---

## 引擎速查表（联机/部署）
| 主题 | 事实 |
|---|---|
| 握手三道闸 | 版本串→玩法 mod 清单→ModelID 哈希；前两不符才拒绝，非玩法差异仅告警 |
| ModelID 哈希 | 只混入 affectsGameplay=true 的模型；双方 dll 字节一致即同哈希 |
| 地图放行 | 无"房间完成"字段；`NMapScreen.IsTravelEnabled` 本地门控，胜利路径=`SetTravelEnabled(true)` |
| 自定义 GameAction | `VoteForMapCoordAction` 是最佳模板（OwnerId/ActionType/ExecuteAction/ToNetAction 四件套）|
| 部署 | csproj `CopyToModsFolderOnBuild` 构建即部署 dll/json/pdb+pck 到 live —— **清单版本号只能改源头 `mod/Spire1.json`**，手改 live 会被下次构建冲掉 |
| pck 内容 | powers.json 等本地化资源在 pck 里；dll 只含代码。改 loc 必须重打 pck |
| 分装包 | 三包字节一致仅 character.txt 异（ironclad/silent/defect/all）；stage 目录 `dist/stage/Spire1-<Pkg>/mods/Spire1/*`，PowerShell Compress-Archive 打包（无 zip CLI）|
| 日志取证时效 | godot*.log 会轮转丢失——冻结/异常发生后第一时间拷走现场日志再杀进程 |
