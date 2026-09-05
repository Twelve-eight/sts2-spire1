# KB 研究方法与实录坑（Research Methods & Field Notes）— sts2-spire1

> 目的：把字节码/反编译取证过程中的**可复用方法**与**实际踩过的坑**固化下来，让后续会话（含主会话续作、子代理分工）不再重复付学费。原则"skill 放方法、KB 放事实"在缺 skill 基建的本仓库落地为：本篇放方法，`mechanics/` 与 `kb/sts2-*.md` 放事实。按 AGENTS.md §1，本篇同时是"下次该怎么做"的入口。
> 维护：每遇新坑/新方法即追加；日期标注首录时间。本文由 2026-09-04/05 两轮 KB 深化会话（power/relic 矩阵扫描 + 仲裁卷写作）沉淀。

---

## 1. 取证工作流总览（StS1 jar 侧）

标准流程：**提取类 → javap 反汇编 → 读偏移 → 常量池扫引用 → 写卷（每条规则附出处与置信度）**。

```bash
# 1) 提取（只读 jar，落 G: 临时区；cls/ 已 gitignore）
export JAVA_HOME="C:\Program Files\Zulu\zulu-21"
cd "G:\steam\steamapps\common\SlayTheSpire"
unzip -o -q desktop-1.0.jar "com/megacrit/cardcrawl/powers/Foo.class" -d "<scratch>/cls"

# 2) 反汇编（方法面用 -p，语义/偏移用 -c -p）
"$JAVA_HOME/bin/javap" -c -p "<scratch>/cls/com/megacrit/cardcrawl/powers/Foo.class" > Foo.javap.txt

# 3) 引用方全量枚举：见 §4 常量池扫描法
# 4) 全量钩子面扫描：research/sts1-kb/scan-hooks.mjs（见 §5）
```

**M1 javap 直接喂 .class 文件路径**，不要用 `-cp <dir> com.megacrit...Foo`（MSYS 环境下 classpath 传 Windows 路径实测失败"找不到类"，浪费一轮排查）。置信度：本机实测 2026-09-04。

**M2 javap 两档用法**：`-p` 只看"覆写了哪些钩子/字段"（方法面）；`-c -p` 看分支与偏移（写卷引用 `#offset` 用它）。引用格式约定：`类名#方法` + javap 偏移号（如 offset 952-957）。

**M3 产物归档**：反汇编 .txt 留在 `research/sts1-kb/.tmp-javap/`（git 跟踪，供对账；`cls/` 目录已 gitignore）。卷内引用偏移时可注明"快照行号会漂移，以方法名定位为准"。

## 2. shell / 工具坑实录（本机 Git Bash + MSYS + ugrep）

**P-M1 unzip 通配符不跨目录**：本机 Info-ZIP 构建对 `*.class`、`com/megacrit/*` 一律 `filename not matched`（`*` 不匹配 `/`）。批量提取唯一可靠做法：
```bash
unzip -l desktop-1.0.jar | grep -oE "com/megacrit/[a-zA-Z0-9_$/]+\.class" | sort -u > classlist.txt
tr -d '\r' < classlist.txt | xargs unzip -o -q desktop-1.0.jar -d cls   # 见 P-M2
```

**P-M2 清单文件 CRLF 陷阱**：`unzip -l` 输出经管道/重定向落盘后每行带 `^M`，xargs 喂给 unzip 后**所有**名字都 not matched（症状极具迷惑性——单个显式路径又明明可行）。先 `tr -d '\r'`。首录 2026-09-04（当时排查了三轮才定位到 ^M）。

**P-M3 Windows 反斜杠路径在 bash 双引号里被吞**：`"G:\omp works\...$(basename $x)"` 这类写法实测坏（`\` 转义了后续字符）。**统一用正斜杠** `G:/omp works/...`，工具链全兼容。

**P-M4 本机 grep 是 ugrep**：行为差异（如 `grep -|pattern` 报 invalid option；二进制匹配提示不同）。要点：扫 .class 常量池必须 `grep -a`（按文本处理二进制）；复杂文本处理**直接写 Node 脚本**（fs 读字节 + latin1 字符串 + includes/正则），不要在 bash 里做转义体操。

**P-M5 Node 内联脚本的反斜杠转义**：`node -e "..."` 里写 `/\\/g` 会被 bash 双引号吃掉反斜杠导致语法错（2026-09-05 实录）。**两个以上语句的脚本一律落 .js 文件再跑**（`scan-hooks.mjs` 即此产物）。

**P-M6 PowerShell 写文件加 BOM**（AGENTS.md §10 已录，此处归档引用）：写 .java/.mjs 一律用 Write 工具或 `-Encoding ASCII`。

## 3. 常量池扫描法（调用者全量枚举）

**M4 方法**：Node 把 .class 按 latin1 读成字符串，`includes("SomeName")` 过滤。用于"全 jar 里谁引用 X"（SuicideAction、EndTurnDeathPower、applyFocus、onChangeStance、uniqueStancesThisCombat 等调用点枚举都靠它）。

**M5 局限（重要）**：字符串命中只证"常量池里出现"，不证"调用了该方法"——方法名可能来自无关常量（如子串、或仅作为被引类型出现）。**每个结论用 javap 复核调用点偏移**后才可写进卷。例：`RemoveSpecificPowerAction` 命中 `orbs/AbstractOrb` 是类型引用而非调用；`EnergyManager` 命中一堆能量遗物实为字段读取引用，它们并不覆写 `onEnergyRecharge`（MarkOfPain 真实挂点是 atBattleStart）。

**M6 继承可见性陷阱**：`javap -p 子类` 只列**子类自己声明/覆写**的成员——这正是扫描"覆写了什么"想要的；但继承字段（如 `AbstractRelic.counter`）不会出现在子类输出，别误读为"没有 counter"。

## 4. 钩子面全量扫描（scan-hooks.mjs）

**M7 子串污染**：`includes("atEndOfTurn")` 会误命中 `atEndOfTurnPreEndTurnCards`；同理 `onAfterUseCard ⊃ onUseCard`、`atDamageFinalReceive ⊃ atDamageReceive`、`onAttackedToChangeDamage ⊃ onAttacked`。**必须按方法签名正则匹配**（`/void atEndOfTurn\(boolean\)/`）。2026-09-05 首版扫描即因此出错后返工。

**M8 工具**：`research/sts1-kb/scan-hooks.mjs <powers|relics|...>`——对包内全部 .class 跑 javap -p，按签名正则归类钩子，输出 JSON（`.tmp-javap/<pkg>-scan.json`）+ 控制台按钩子分组成员列表。签名模式表（PATTERNS）就在脚本头部，加新钩子直接扩表。

**M9 "双钩子"复核**：扫描发现某类同时实现两个钩子（Equilibrium/Ritual/Malleable 的 `atEndOfTurn`+`atEndOfRound`）时，**必须读两段方法体**分清各自职责（效果 / 到期自移除 / 数值复位），不能只报"双触发"。Ritual 还示范了"怪物侧走 atEndOfTurn、玩家侧走 atEndOfRound + skipFirst"的分侧实现。

## 5. 时序推导法（队列语义）

**M10 FIFO 推导三件套**：①块级调用点偏移（`GameActionManager#getNextAction` 的新回合块 1983-2228、开局块、哨兵链）决定**入队先后**；②`addToBot`=队尾、`addToTop`=队首（多次 addToTop 连用 = 逆序执行）；③**构造期 vs 执行期**区分——`DrawCardAction` 3 参构造器 new `PlayerTurnEffect` 的瞬间就重置能量（视效类构造器藏逻辑）、`UseCardAction` 构造器同步发 onUseCard 钩子。渎神 vs 无实体旗舰仲裁就是这三件套推出来的（death-arbitration.md R17-R19）。

**M11 onModifyPower 是全局刷新枢纽**：任何 power 增删后 `AbstractDungeon.onModifyPower()` 会刷手牌数值、**条件刷新宝珠**（`hasPower("Focus")` 门！）、逐怪 `applyPowers()`（重算实伤快照与意图显示）。排查"数值没刷新/意外刷新/意图与实伤不一致"类问题第一站看这里。注意门的两侧不对称：Focus 完全移除后宝珠**冻结不回落**（orbs.md R12 勘误实录）。

**M12 随机源分账**：aiRng（怪物 move）/ shuffleRng（洗牌）/ cardRandomRng（随机选卡/勺子）/ treasureRng / merchantRng / potionRng（StS1 六流，loot-rewards.md L01-L04）；StS2 侧 `RunRngSet`（Runs/RunRngSet.cs）共 **12 条 run 级流**：UpFront / Shuffle / UnknownMapPoint / CombatCardGeneration / CombatPotionGeneration / CombatCardSelection / CombatEnergyCosts / CombatTargets / MonsterAi / Niche / CombatOrbs / TreasureRoomRelics，另有 **PlayerRngType 三条玩家级流**（Rewards / Shops / Transformations，可存档、跨战役）——卷间引用统一用这些名字，别写"随机数"。

## 6. StS2 C# 侧工作流

**M13** 源码在 `research/engine-dllsrc/MegaCrit.Sts2.Core.*`（Godot EA build 反编译，直接可读）。定位用 `grep -n "public static async\|#方法名"`；**行号会随重导出漂移，引用以 `文件#方法` 为主、行号为辅**。Builder 命令族（AttackCommand 等）先读字段与 Execute()，钩子顺序全在 `Hooks/Hook.cs`（2568 行，一个静态门面分发到 models 的钩子接口实现）。

**M14 StS2 免死/死亡的仲裁入口**是 `CreatureCmd.Kill → Hook.ShouldDie(preventer)`（与 StS1 的 damage() 尾部拦截链完全不同形态），见 `kb/sts2-combat-semantics.md` S06。

## 7. 写卷与自检纪律

**M19 扫描结论必须双方法交叉**（2026-09-05 实录）：同一批 Events 文件，Node walk 报 0 命中、grep -rln 报 7 命中——直接读单文件证实 grep 为真、walk 结果错误（根因未查，弃用）。**全量扫描结论入卷前必须与 grep -rln 交叉验证**；不一致时以 grep 为准并把分歧记为待查。
**M18 宣告发现前先验证约定**（2026-09-05 勘误实录）：I7 选牌键核对曾按 events 域习惯假设连字符蛇形，产出"DualWield 缺键即炸"的假发现并写进勘误段——**应先从存量真实键反推命名约定（`SPIRE1-DUAL_WIELD` 下划线），再跑核对**。约定跨域不同（cards=下划线、events=连字符）时必须在脚本里显式声明并留出处。

**M15 旗舰结论回测**：用户给的"结论示例"（如渎神 vs 无实体）要能从卷内规则**机械推导**复现；推不出来 = 有规则缺口或写错。
**M16 偏移重对**：成卷后把引用的关键偏移逐个用 grep/javadoc 重对一遍（2026-09-05 一轮反思抓出 4 处错误：钨杆挂点偏移、Focus 失去侧行为、意图刷新时机、turn-phase R02 能量保留）。
**M17 置信度标注不省略**：高=字节码/C# 直接可证；中=推断（注明环节）；低=wiki/间接。开放问题逐条留档——它们就是下一轮深挖的任务清单。
