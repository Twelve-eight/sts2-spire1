# 理论知识库:跨引擎移植的验证与审计原理(2026-09-04 批判审阅沉淀)

> 来源:sts2-spire1 批判审阅(docs/CODE-REVIEW-20260904.md)。
> 与 pitfalls.md 互补:pitfalls 记"具体坑",本文记"坑背后的普适规律"。
> 每条定律附本次实证,供未来项目直接引用。

## 定律 1:跨版本孪生漂移集中于"升级通道"

同名内容跨引擎移植时,基础数值(费/伤/挡/magic)在编写时被逐字段抄写,天然对齐;
而**升级 delta 与升级增删关键词是第二个思维通道**,最容易凭"升级当然变强"的
直觉跳过逐字段核对。同名孪生若不同,差异几乎总在升级路径上。

实证:复用通道 5 张违规卡(Claw/Barrage/Flechettes/Chill/Darkness)全部错在升级
路径或引擎侧单侧 buff(基伤 4→5 属引擎再平衡);基础字段错为零张。
**推论**:孪生核对清单必须显式包含 `upgradeDamage/upgradeBlock/upgradeMagicNumber/
升级关键词增删` 四项,且优先级高于基础字段。

## 定律 2:验证方向正交——"我方→官方"与"官方→我方"互不替代

- mod→官方:去重(同名卡是否可复用)、泄漏(池污染)、耦合(补丁冲突)。
- 官方→mod:保真(数值/语义)、**覆盖(全集→实现的矩阵)**。
只做前者的库会"自洽但残缺":每个已实现项都对,但全集有洞且无人察觉。

实证:本项目三轮人工 critic 全做了方向一;方向二第一次执行(2026-09-04)即产出
~40 张卡覆盖缺口 + 155 遗物缺口 + 10 张稀有度错。
**推论**:任何"内容移植完成"断言之前,必须存在并运行过全集→实现的覆盖矩阵;
`tools/audit-card-fidelity.mjs` 与 `tools/audit-monster-hp.mjs` 是本仓的复跑入口。

## 定律 3:补丁作用域 = 目标解析结果的声明域

Harmony 对目标类型上不存在的成员沿继承链向上解析:对"自定义子类 patch 其未
重声明的属性"实际钉住的是**基类实现**,作用域从"这个池"膨胀为"所有池"。
prefix/postfix 不携带 `__instance` 时无任何运行时守卫。

实证:Pandora 修复(e40db70)对 `WatcherCardPool` patch `AllCards/AllCardIds`,
工坊类未重声明 → 补丁落到 `CardPoolModel` 基类 getter,混沌局内全局换池内容
(CODE-REVIEW R2)。AA 自家的同类补丁带 `__instance + is Xxx` 分派,是正确形态。
**推论**:自定义内容补丁的 checklist:(a)确认目标成员在目标类型**自身**声明
(反编译看 class 头与成员表);(b)prefix/postfix 一律带 `__instance` 类型守卫;
(c)若目标必然在基类,考虑改钉子类真实重写的成员(如 `GenerateAllCards`)。

## 定律 4:以 Count 比较表达的"终幕语义"不可局部改写

引擎以 `CurrentActIndex >= Acts.Count - 1` 表达"这是最后一幕";多个消费点
(发奖、地图/门生成、房间生成、圣遗物调整、联机缩放)各自独立引用同一表达式。
第四幕 mod 为让三幕"不再终幕"而统一把常量 `-1` 改成 `-2`——对"生成通往下一幕"
的消费点方向正确,对"终幕不发奖"的消费点方向**相反**(把三幕也压进不发奖分支)。

实证:Act4Heart FixAct3Boss 对三处 `Count-1` 统一改写;`RewardsSet.WithRewardsFromRoom`
处方向反转,三幕 boss 在四幕在列时零奖励且与钥匙无关(CODE-REVIEW R1)。
MoveType 实证方法:反编译游戏自带 0Harmony.dll 内嵌 MonoMod.Cil,确认
`{Before=0, AfterLabel=1, After=2}` → `(MoveType)2`=After,`val.Prev` 命中常量。
**推论**:对"同一表达式多消费点"做 IL 改写时,必须枚举全部消费点并逐一判定
方向性;修复侧(本仓 postfix 补发)同理要限定"倒数第二幕+下一幕是第四幕"。

## 定律 5:无工具绑定的验证断言按修复波衰减

"逐字段核对"/"runtime verification CLOSED" 类断言若无可复跑命令支撑,
其可信度在下一个大修复波后即衰减:新批次以旧结论为前提继续叠加,
错误前提被"已验证"光环保护。

实证:8781855 复用波声称对 57 张孪生逐字段核对,尾段实际批量追加;
session 26 的 "bridge path CLOSED" 未能覆盖 R2(签名全对,但目标钉错)。
**推论**:DEVLOG/报告中每个"已验证"必须绑定复跑入口(命令/脚本+期望输出),
并注明当时的代码范围;新增批次触达同范围时必须复跑而非引用旧结论。

## 定律 6:代理舰队的并发上限是硬约束

平台侧"user concurrency limit exceeded"是账号级并发上限,不是瞬态故障;
对大 prompt 的批量派发以"Model request failed"泛化报错,误导性强。
诊断时应先看**外部系统的独立证据**(如路由后台流量),再归因;
正确编排:深度切片=单个子代理串行,横向独立维度=主会话同步推进。

## 附:可复跑工具清单(本次审阅产出)

- `tools/audit-card-fidelity.mjs --scope=all` — 304 我方卡 + 101 复用通道 vs
  一代 jar(javap 提取:费/伤/挡/magic/升级 delta/rarity/type/target)。
  已知解析局限:多变量卡的"magic"取 others[0] 有误报(Concentrate/
  Perseverance/WindmillStrike/Wish 已人工裁决为误报);诅咒卡(Spire1Curse
  基类形态)不参与数值对比;X 费卡 jar=-1 vs 引擎 0 为编码差异已特判。
- `tools/audit-monster-hp.mjs` — 66 怪 HP vs jar setHp(含 Ascension 分支)。
  已知局限:定值怪 `MaxInitialHp => MinInitialHp` 形态、单参 setHp(int) 需人工。