# HANDOFF — sts2-spire1 → omp (2026-09-06,全项目唯一交接文档)

> 交接时刻 2026-09-06。本文档覆盖**全部** ZCode 接手期产物,omp 零上下文可接手。
> 三条线的产出都已入库并在本文合并:①审阅线(zcode 主会话,提交 `62cac97`:
> 批判审阅 + 审计工具 + 理论 KB);②KB 深化线(已终止代理,sessions 28-29 + 大量
> 语义卷,其未提交的交接草稿已合并进本文并删除原文件);③更早的 omp 线
> (PLAN-2026-09-05.md 为 omp 自己 staged 的计划,checkbox 全部未动)。
> 冲突时以本文 + DEVLOG 为准。

## 1. 项目一句话与当前状态

**sts2-spire1** = 把《杀戮尖塔 1》的角色/卡牌/圣遗物/力量/药水/事件/怪物/遭遇移植进
《杀戮尖塔 2》(Godot EA,BaseLib 生态,C#/.NET9,id/prefix `Spire1`/`SPIRE1-`)的 mod,
作为社区剧集栈(AFTP 三幕 + Act4Heart + ActToggler2 + MP Rebalance)的互补内容层;
配套知识库(KB)、审计工具与联机兼容层(AutoAnthonyCompatBridge)。
- git:master,HEAD 见 `git log`;全量已推送 `github.com/Twelve-eight/sts2-spire1`。
- **代码基线 = `9453ebc`(最后一个代码提交)**。其后 70+ 提交全是文档/KB/工具。
  **因此已部署的 `mods/Spire1/Spire1.dll` 仍含下述两个 P0**——修复+构建+部署是第一动作。
- 关联仓:chaosbridge(`G:\omp works\chaosbridge`,已发布 v0.1.0,github 私有,
  更新会话已完工下线,无需接手动作);AFTP fork(`G:\omp works\aftp-ActsFromThePast`)。

## 2. 首读索引(按序)

1. 本文 → 2. `DEVELOP.md`(设计契约;§5 契约、§7a lean-code、§7c/7d 事件与遭遇
   权威清单、§7f 桥接设计)→ 3. `docs/CODE-REVIEW-20260904.md`(2026-09-06 定稿的
   批判审阅:发现 R1-R15 + 反思 + DEVLOG 断言复核;**修复队列来自这里**)→
   4. `DEVLOG.md` 尾部 400 行(sessions 26-30)→ 5. `research/kb/PLAN-2026-09-05.md`
   (omp 自己的 P1-P13 计划)→ 6. `research/kb/review-theory-20260904.md`(六条定律)
   → 7. `research/kb/research-methods.md` M1-M19(字节码工作方法,**开工前必读**)。

## 3. ZCode 接手期发生了什么(omp 不知道的部分)

- **批判审阅(已完成,`62cac97`)**:两片由子代理完成(interop-patches、cards A-F),
  其余主会话单线;产出发现 R1-R15(P0×2、P1×6、P2×7、P3 择要)、两个可复跑审计
  工具、理论 KB 六定律、DEVLOG 断言复核表。**修复未实施**。
- **KB 深化(sessions 28-29,已终止代理)**:StS1 机制卷 8 篇(death-arbitration
  R01-R22 旗舰=渎神 vs 无实体裁决、defense-powers、orbs、stances、energy-cost、
  potions-combat、monster-ai)+ StS2 语义卷(sts2-combat-semantics S01-S14 等七卷)+
  治理卷(pool-architecture、invariants I4-I15、semantics-review-checklist、
  research-methods M1-M19)+ loot-rewards L12/宝箱概率结案。KB 规则总数 119→254+。
  其交接草稿(未提交)已合并进本文。
- **审计工具(已提交)**:`tools/audit-card-fidelity.mjs`、`tools/audit-monster-hp.mjs`
  (§6)。此前已存在的 `tools/pool-audit.mjs`、`tools/semantics-audit.mjs` 等照旧。

## 4. 优先队列(按此顺序执行)

1. **R2 [P0]** `mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs:185-188`:
   对 `WatcherCardPool` patch `AllCards/AllCardIds`,但工坊类未重声明这两个属性
   (`.tmp/watchermod/WatcherMod/WatcherCardPool.cs:6` 仅 `: CardPoolModel`)→
   Harmony 沿继承链解析,**补丁落在基类 getter 上对全部卡池全局生效**;两个方法
   (:212-253)无 `__instance` 守卫,混沌局内所有池内容/ID 被换 →
   `CardModel.Pool` 反查抛 `InvalidProgramException`。修复:两方法加
   `CardPoolModel __instance` + `poolType.IsInstanceOfType(__instance)` 守卫
   (镜像 AA 自家 `ColorlessPoolContentsPatch` 写法),或改 patch
   `GenerateAllCards`(工坊池真实重写的成员)。
2. **R3 [P0]** 同文件 `:323`/`:333`(FromSave/FromHistoryPostfix):
   `.Where(TryMap)` 过滤后回查 `EntryMap[e!]`,而 "WATCHER" 只在
   `ThirdPartyEntryMap`(:170)→ 存档/历史加载必抛。用 TryMap 的 out 值取结果。
3. **构建 + 部署 + 冒烟两个 P0**(构建预期 0 错;冒烟=加载含工坊观者条目的存档 +
   一局混沌;游戏进程可能被用户占用,锁则部署延迟——AGENTS §11)。
4. **R1 [P1] 三幕 boss 零奖励**(根因链已全程实证,报告 §R1):引擎
   `RewardsSet.WithRewardsFromRoom`(`.tmp/dllsrc/.../RewardsSet.cs:88`)对
   `CurrentActIndex >= Acts.Count-1` 的 boss 返回**空奖励集**;Act4Heart
   `FixAct3Boss` 把常量改 `Count-2`(`(MoveType)2`=After 已实证),而其
   `ModelDb.get_Acts` 钩子无条件追加 TheEnding → 三幕 boss(index 2)必落空分支,
   与钥匙无关。修复进本仓(DEVELOP §0 生态补丁规则):`WithRewardsFromRoom`
   postfix,当 `RoomType==Boss && CurrentActIndex==Acts.Count-2 && Acts[idx+1]`
   是第四幕时按引擎原始构成(:238-241)补发金币+药水 roll+`CardReward(3)`
   (私有成员经 AccessTools)。冒烟三态:有钥匙/无钥匙/纯三幕。
5. **R7 [P1]** 十处稀有度一行修复(BandageUp/Blind/Finesse/FlashOfSteel/
   GoodInstincts/SwiftStrike/Trip → Uncommon;Brutality/LimitBreak → Rare),
   然后跑审计工具回归归零。
6. **R5 [P1]** 复用通道五张违规卡处置(Claw 升级 2↔1、Barrage/Flechettes 基伤
   4↔5、Chill 升级 Innate↔去 Exhaust、Darkness 升级触发一次↔两次):移出
   `SharedCardReuse`,恢复我方忠实类现役(Claw/Chill/Darkness 类已在
   Spire1LegacyPool;核实 Barrage/Flechettes 我方类是否存在,缺则补写)。
7. **R6/R8 [P1]** 覆盖批次:先 9 张引擎孪生核对(Hemokinesis/Offering/
   PerfectedStrike/Shiv/Slimed/Apotheosis/Discovery/HandOfGreed/TheBomb——字段一致
   则复用,不一致则自建);再真实缺口(~30 张无色、DoubleTap/Exhume/Amplify/
   Electrodynamics/LockOn/SearingBlow;诅咒 4 张先确认授予来源)。圣遗物层
   (29/180+)先在 DEVELOP.md 记录范围决策再动工。
8. **R9-R15 [P2/P3]** 见报告(遭遇"3 Cultists"与 Shelled Parasite+Fungi 组队;
   条件事件出现门控;逐补丁 try/catch;Fission 重写;Evaluate+ Insight 升级;
   联机哈希旁路二次开关;Burn 升级)。
9. **PLAN-2026-09-05.md P1-P13**(omp 自己 staged 的 KB 计划,checkbox 未动;
   与 DEVLOG 续3 已落地的 L12/宝箱概率条目有邻接,恢复时先对账)每项 commit+push。

## 5. 待验证队列(诚实标记)

- Pandora 直接命中种子 sweep(DEVLOG session 27"REOPEN");R2 修复后期望:
  transform 产物全 `CHAOS_COLORLESS_*`、零新增原版 `WATCHER_*`。
- R1 修复三态冒烟(上)。
- AFTP Old Beggar zhs 渲染问题——静态审计无果,等用户截图。
- NoBlockFromCards 频率问题——原子目录数学已算,family-pick 概率分析被打断;
  用户不再关心即搁置。
- FTL/Finisher"计数是否含自身"——UNCONFIRMED,改前必须 javap jar。

## 6. 工具(可复跑验证——引用命令与输出,不做口头"已核对")

- `node tools/audit-card-fidelity.mjs --scope=all` — 三方卡牌保真(一代 jar javap ↔
  引擎反编译 ↔ 我方实现):304 我方卡 + 101 复用通道的 cost/伤/挡/magic/升级
  delta/rarity/type/target。已知解析局限(文件头 + review-theory 附录):多变量卡
  magic 取 others[0] 有误报(Concentrate/Perseverance/WindmillStrike/Wish 四张已
  人工裁决为误报);Spire1Curse 基类形态卡跳过;X 费 jar=-1 vs 引擎 0 已特判。
  需要 `JAVA_HOME=C:\Program Files\Zulu\zulu-21`;javap 缓存在 `.tmp/audit/javap/`。
- `node tools/audit-monster-hp.mjs` — 66 怪 HP vs jar(含升天分支);定值怪
  `MaxInitialHp => MinInitialHp` 与单参 `setHp(int)` 形态需人眼(审阅时 66/66 全对)。
- `node tools/pool-audit.mjs`([Pool] 归属 lint,应 0 孤儿)与
  `node tools/semantics-audit.mjs`(P4+P1+I7 一键门,应全绿)——基线自检。
- `node tools/check-agent-text.mjs` — 文字卫生检查器(AGENTS §5),派发前必跑。
- 钩子面扫描:`research/sts1-kb/scan-hooks.mjs <powers|relics>`、
  `kb/scan-sts2-hooks.mjs`、`stS1-monster-scan.js`、`stS1-event-cards.js`。
- 方法卷 `research-methods.md` M1-M19:javap 直接喂 .class 文件别用 -cp;unzip 通配
  不跨目录 + 清单 CRLF;MSYS/ugrep/Node 内联转义坑→落文件;常量池扫描"引用≠调用";
  钩子扫描必须签名正则;RNG 分账。**字节码工作前必读**。

## 7. 环境与机器事实(硬约束)

- **永不写 C 盘**(接近满);缓存/临时全在 G(`.nuget/ .tools/ .dotnethome/ .tmp/`)。
- JDK21=`C:\Program Files\Zulu\zulu-21`(PATH java 是 Zulu 25,须显式 JAVA_HOME)。
- 游戏 `G:\steam\steamapps\common\Slay the Spire 2`;一代 jar
  `G:\steam\steamapps\common\SlayTheSpire\desktop-1.0.jar`;workshop
  `G:\steam\steamapps\workshop\content\2868840\{3746969593=AFTP,3747537811=Act4Heart,
  3787796638=Toggler2,3785039319=MPRebalance}`。
- 反编译引用(勿重导):`.tmp/dllsrc/`(引擎)、`.tmp/act4heart/`、`.tmp/watchermod/`、
  `.tmp/autoanthony/`;`research/engine-dllsrc/`(行号随重导漂移,引用以 `文件#方法`)。
  MoveType 语义:反编译游戏自带 `0Harmony.dll` 的 `MonoMod.Cil.MoveType`
  (`Before=0,AfterLabel=1,After=2`)。
- git:全局 gitconfig 带 `http.proxy 127.0.0.1:7897`,失效时
  `git -c http.proxy= -c https.proxy= push` 直连 fallback。**hooks 在写工具后自动
  `git add -A`+commit——进行中文件会被顺带扫进提交**(实际发生过,63a9841);
  遇到他人进行中文件在 DEVLOG 注明,勿回滚他人提交。
- PowerShell 写文本加 BOM;内联 `$` 经 bash 会丢——一律落 .ps1/.mjs 文件再跑。

## 8. KB 使用指南(本项目最有价值的资产)

- **StS1 语义**:`research/sts1-kb/mechanics/`(action-manager/turn-phase/draw-exhaust/
  damage-pipeline/status-stacking/triggers/**death-arbitration R01-R22**/defense-powers/
  orbs/stances/energy-cost/potions-combat/monster-ai/turn-control/card-rewards/
  loot-rewards)。每条 `文件 Rnn` 独立可引用,置信度标注,字节码偏移可复核。
- **StS2 语义**:`research/kb/sts2-combat-semantics.md`(S01-S14)、sts2-card-play(C01-C06)、
  sts2-monster-ai(A01-A07)、sts2-combat-turn-machine(T01-T08)、sts2-orbs-enchantments(O01-O08)、
  sts2-afflictions(F01-F04)、sts2-event-pool-usage(E01-E05)、sts2-hook-matrix(62/71 钩子
  实现者全名单)。
- **治理三件套**:`pool-architecture.md`(池架构与玩法不变量,Splash/AutoAnthony 两事故
  根因层)、`invariants.md`(I4-I15:联机一致性/canonical 生命周期/资产存在≠内容/本地化
  变量权威/事件池容量/注册时序/存档持久面——每条含事故锚点+检测手段+冒烟盲区)、
  `semantics-review-checklist.md`(P1-P8 + 可整段派发的 reviewer 提示词)。
- 数据基线:`research/sts1-kb/cards-*/relics/potions/events JSON`(字节码直出)。
- 理论沉淀:`research/kb/review-theory-20260904.md`(六定律:孪生漂移集中于升级通道/
  验证方向正交/补丁作用域=解析声明域/Count 终幕语义不可局部改写/无工具绑定的断言
  按修复波衰减/代理并发硬约束)。

## 9. 开发模式纪律(本项目结晶,延续)

1. **三联动回填**:实机事故结案 → DEVLOG → `invariants.md` 加条目 →
   `semantics-review-checklist.md` 加提问(缺一不可)。
2. **G1 KB 先行**:跨池选牌/注册表类特性先写集合运算规格(全集/排除集/容量/第三方行为)。
3. **G2 语义评审门**:pool/registry 改动合入前过清单;asker 型 agent 读 KB 直接发现
   玩法语义 bug——机械冒烟在此类问题上**零检出**(Splash、AutoAnthony 两案均由用户
   实机揭示)。
4. **G3 机械门**:`node tools/semantics-audit.mjs` + `pool-audit.mjs` 全绿才可合入。
5. **G4 冒烟边界**:autoslay 检出域=崩溃/异常/资产缺失/覆盖增长;**玩法语义偏差不在
   检出域,不得以冒烟绿放行**。
6. **验证分层**:Mock/单测 → 控制台单卡(`card play`、`relic add`,NDevConsole.cs:359)
   → autoslay 全局冒烟。
7. **落盘纪律**:每增量 commit+push;每事故 DEVLOG;方法论回填 research-methods;
   "已验证"断言必须绑定复跑命令(断言半衰期律)。

## 10. 事故史速查(全文以 DEVLOG 为准)

| 事故 | 根因一句话 |
|---|---|
| Splash 候选集(#10 修复) | `list.Remove(owner.CardPool)` 按池**对象**排除;SharedCardReuse 令可调用集合≠池对象 → 改 Id 集合差 |
| AutoAnthony 对第三方角色失效(session 25) | `ChaosCharacterMapping.From` 类型门只认五引擎角色 → 桥接/ChaosBridge 接管 |
| GeneticAlgorithm 错色(af6d1d7) | 漏挂 `[Pool]` 静默继承父类池 → pool-audit.mjs 检出目标 |
| ROOM_FULL_OF_CHEESE 崩溃 | 事件要求 ≥8 不重复 Common,自建小池不足 → 容量契约 |
| 尘封魔典 NRE | 官方遗物从当前角色池抽 Ancient 牌,四代角色池空集 → fallback 补丁 |
| 联机粘液失同步 | 单侧抽牌未同步 → I4/I12 实证 |
| Pandora 观者池泄漏(e40db70) | AA transform 走 original.Pool;注意:**该修复自身带 R2/R3 两个 P0**(§4) |
| 三幕 boss 零奖励(R1) | Act4Heart 对 `Acts.Count-1` 的 IL 改写在发奖口方向反转(§4.4) |

## 11. omp 第一小时清单

1. 读本文 → `DEVELOP.md` → `docs/CODE-REVIEW-20260904.md` → DEVLOG 尾部 400 行
   → `research-methods.md`。
2. `git status` 干净;`git log --oneline -20` 对齐;确认 HEAD(交接后可能有新提交)。
3. `node tools/pool-audit.mjs`(0 孤儿)+ `node tools/semantics-audit.mjs`(全绿)验证基线。
4. 确认代码基线:最后一个**代码**提交(交接时为 `9453ebc`);若已实施 §4.1-4.3 则以
   git log 为准。检查游戏进程是否在跑(部署前)。
5. 按 §4 优先队列开工:R2→R3→构建部署冒烟→R1→R7→R5→覆盖批次→PLAN P1-P13。

## 12. 禁区

- 用户游戏进程/联机对局:不启动、不停止、不传送、不重启(无授权不干预;真实用户
  可能在玩)。部署遇文件锁:编译照常、部署延迟到进程退出后补做(AGENTS §11)。
- C 盘写入;StS1 jar 修改(只读取证)。
- KB 结论的置信度标注与"开放问题"段落:**不得为省事删除或简化**——它们是下一轮
  深挖的任务清单。
- `DEVELOP.md` §7c/§7d 权威清单有已知小洞(§7c 漏 SecretPortal——真实一代三幕事件,
  已实现且带 FLAG:传送分支未实现,仅 [Leave]);引用时记得核对而非盲信。
- 文字卫生:模型请求/产出仅中英法德俄 + ASCII(AGENTS §5);外部沟通披露 agent 作者身份
  (DEVELOP §5)。
