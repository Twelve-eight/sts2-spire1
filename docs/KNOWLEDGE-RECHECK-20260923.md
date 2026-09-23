# 知识断言重验与修订契约, 2026-09-23

## 本轮新证据

主会话重新读取一代原版 JAR, SHA256 CFAD868AC8D65A88E71A0BF096FB09F78811E553EFFE0787C5309A655E081673. javap 输出位于 G:\omp works\.tmp\workspace-audit-20260923-01a0cbfd\evidence\sts1-knowledge-current-jar.javap.txt.

- Havoc 构造器传入 CardType.SKILL, 不是 POWER. 这是本轮新读的原版字节码, 不只是旧笔记之间互相引用.
- 原版 EnergyManager.class 原样提取到隔离 G: 目录, class SHA256 A2BF2D866B0D678C1D9107B4443CC31898AB4C1C5994B16095779B664253508A. 本轮实际 JVM 执行 recharge 方法, 协作者为可控桩. 起始剩余 7, 基础 3: 普通分支 after=3/setCalls=1, Ice Cream 和 Conserve 分支均 after=10/addCalls=1. 日志在同证据目录 energy-bytecode-probe\result.log. 该隔离实验不等于整局时序实机验收; PlayerTurnEffect 调 recharge 的调用边由本轮 javap 输出单独证明.
- 原始卡池重复项计数不是奖励概率. 消费者可能 Except/Distinct, 其相等语义也不等于按 ModelId 计数. 此轮只收紧 PoolCensus 的证据措辞, 不改池成员, 奖励算法或生成概率.

## 本次修改

- 修改当前 research/sts1-kb/mechanics/README.md 两条错误入口断言, 附本轮实验及源码定位, 不改历史归档原件.
- PoolCensus 的 raw duplicate 诊断不得再无条件宣称 duplicate reward weight. 保留数字和现有诊断结构, 明示需要沿具体消费者确认过滤/去重后权重.
- 不修改技能目录的 C: 文件, 不批量重算知识 JSON, 不将清点文件数冒充全量语义审查通过.
- 实现与监督同批, 主会话原生 hub 等完整产物后放行监督, 集中构建验证.

## 2026-09-23 知识文案后续修订

两份设置资源的 IGNORE_MP_MOD_DIFFERENCES.hover.desc 仍错误宣称同时放过 ModelID 哈希差异. MpIgnoreModDiffPatch 当前保留独立 IgnoreMpHashMismatch 门禁. 本次只修 eng/zhs 说明, 明确主开关控制模组清单差异, 哈希差异须另行显式开启. 不改变任何默认值或联机安全判断, 不把文案纠正当作双端握手通过.
## 原版数据重提取后的说明纠错

主会话已执行原样 build_kb.mjs 的隔离副本, 当前 JAR 哈希与此前相同. 13 份 JSON 输出中 11 份逐字节相同, events.json 与 relics.json 的实际字符串不同. 数据来源审查已发现差异可由 U+2018/U+2019 转 U+0027, U+201C/U+201D 转 U+0022, U+2014 转 U+002D, U+2026 转三个 U+002E 的有损映射解释, 仍须主会话集中执行该映射的全对象比较. 不把有损映射等价称为原文照录, 不修改任一 JSON 来消除差异. 数据数值的独立语义验证和游戏可达性仍未完成.
当前 README 应修正原文照录的绝对宣称, 修正旧重建路径, 将缺少本地化与已证明不可获得分开, 补充 monsters-scan.json 的生成链与类扫描边界. 新增当前重验说明, 固定输入/生成器/输出证据及未知项, 但不改历史归档或生成器算法.