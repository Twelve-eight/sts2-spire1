# Spire1 本地化标记诊断 (2026-09-17)

用户报告:pure 模式下卡牌描述的中文有"莫名其妙的空格","黄字(引擎关键字)与白字(纯文本)混淆".

## 关键前提:Spire1 走 BaseLib SimpleLoc,不是原生 StS2 语法

`mod/Spire1Code/MainFile.cs:53` 调用 `SimpleLoc.EnableSimpleLoc(ModId)`,注释逐字写着
"cards.json `!D!`/`!B!`/`*word*` tokens are converted at load".

`research/baselib-dll/BaseLib.Patches.Localization/SimpleLoc.cs:115 Simplify()` 的转换链:

| 源标记 | 正则 | 转换结果 |
|---|---|---|
| `*word*` | `GoldHighlightRegex`(:28) | `[gold]word[/gold]` -- **黄字** |
| `$word$` | `BlueHighlightRegex`(:32) | `[blue]word[/blue]` -- 蓝字 |
| `{Var}` | `NormalVariableRegex`(:36) | 变量值 |
| `!Var!` | `DiffVariableRegex`(:40) | 变量值 + `:diff()`(升级时显示差值) |
| `@Var@` | `InverseVariableRegex`(:44) | 变量值 + `:inverseDiff()` |
| `[E]` / `[EE]` | `EnergyIconsRegex`(:56) | 能量图标 |
| `-a-+b+` | `UpgradeSwapRegex`(:48) | 升级前后交换 |
| `#...` | `Simplify()` 首行**直接返回不处理** | 颜色码**不被转换** |

**所以 `*Block*` / `!D!` / `-a-+b+` 全部有效**,英文侧写法正确. 我先前据"官方 dump 里 `*` 出现
0 次"推断"星号无效"是**错误**的:官方文本直接用 `[gold]`,而 SimpleLoc 之后两者等价.

## 真正的缺陷

### 缺陷 1:zhs 关键字标记不一致(黄白字混淆的根因)

zhs 描述里关键字有两种写法,少数用 `*..*`(变黄字),多数是**裸词**(白字). 用户看到的
"黄字与白字混淆"即此.

**修法**:把 zhs 的裸关键字统一为 `*关键字*`. 英文侧**不动**(已正确).

**踩到的坑(重要)**:第一次修复只改了 60 条就"清零",因为正则用了 `\w` 作边界 --
而 `\w` 在 Python 与 JS 里**都匹配 CJK**,于是"被另一个汉字紧邻的关键字"全部漏掉.
改用 `[\[\*A-Za-z0-9_]` 作边界后,又找出 **101 处**. 守卫脚本里同样标注了这个陷阱.

### 缺陷 2:zhs 关键字周围多余空格

源自 StS1 中文写法 `层 易伤 .`. 权威 zhs 形式(`.tmp/f05-verify/zhs-relics.json`)是
**关键字前后无空格**:`获得[blue]{Block}[/blue]点[gold]格挡[/gold].`

**修法**:删掉标记与 CJK/标点之间的空格.

### 缺陷 3:`#r/#y/#b` 颜色码不被 SimpleLoc 转换

`Simplify()` 只有 `*`/`$`/`{}`/`!`/`@`/`[E]`/`-a-+b+` 七类规则,**没有 `#` 规则**,
所以 `#rBurning` 原样渲染. eng/zhs 的 relics.json 各 16 处.

**修法**:`#rX` -> `[red]X[/red]`,同理 `#y`->gold / `#b`->blue / `#g`->green / `#p`->purple.

### 缺陷 4(已排除):`-a-+b+` 与 `{IfUpgraded}` 等价,不需改

`SimpleLoc.MakeUpgradeSwap`(:150)产出 `{IfUpgraded:show:{B}|{A}}`,与手写 `{IfUpgraded}` 等价.
**确认 `-a-+b+` 已被正确处理,未做改动** -- 这是排查中唯一"看起来有问题但实际正确"的一项.

### 缺陷 5(发现并修复):标题不应有高亮标记

权威 311 个 zhs 标题**全部无高亮**(0 处含 `[gold]`/`*`). 我的第一版修复误把 8 个标题
(如 `闪电霹雳`)包成 `*闪电*霹雳`,已还原,并把该规则加进守卫.

## 规模(最终)

| 缺陷 | 修复条目 |
|---|---|
| zhs 裸关键字 | 101 |
| zhs 空格 | 含在上项内 |
| `#r/#y/#b` 颜色码 | 32(eng 16 + zhs 16) |
| 标题误包裹(回退) | 8 |

## 校验

`tools/check-loc-markup.mjs` 落盘为常驻守卫,检查四类问题:
颜色码 / 标记旁空格 / 裸关键字 / 标题内标记. 当前 `loc markup: OK (0 problems)`.

## 教训(值得记入 DEVLOG)

1. **判定"某个标记是否有效"时,不能只看官方 dump 的出现次数.** 官方文本用 `[gold]` 直写,
   不含 `*..*`,是因为官方**不经过** SimpleLoc. 正确顺序:先查 `MainFile` 的初始化,
   再查转换器源码,最后才看数据. 我据官方 dump 的 `*` 计数得出"星号无效",若照此改会
   **破坏 121 处正确的英文标记**.
2. **正则边界不要用 `\w`** -- Python/JS 里它匹配 CJK,会漏掉汉字紧邻的关键字.
3. **改文本前先看权威样本的空格/标记习惯**,不要凭"看起来别扭"判断.


## 未验证

修复后需在游戏中确认渲染. 纯文本层面无法证明黄字/白字正确.
