# STS1 数据重验说明, 2026-09-23

本页区分文件清点, 原样生成器重提取, 有损标点映射比较和仍缺的来源证据. 这里引用的是主会话已保存的运行结果及只读来源审查, 本次文档任务未运行生成器, 测试, lint, 构建或游戏, 未修改任何 JSON. 不将同源输出可重现称为全库独立语义审查, 完整官方版本认证或实机验收.

## 1. 本轮输入与运行身份

- 原始清单: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/reextract-manifest.json. CheckedAt=2026-09-23T10:55:19.6372385+08:00, NodeVersion=v24.18.0, ExitCode=0.
- 输入 JAR: G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar. 清单记录的 InputSHA256 与 InputAfterSHA256 均为 CFAD868AC8D65A88E71A0BF096FB09F78811E553EFFE0787C5309A655E081673. 这固定本次读取对象, 不单凭安装路径或哈希认证官方发行版本.
- 来源生成器: G:/omp works/Sts/sts2-spire1/research/sts1-kb/build_kb.mjs. GeneratorSHA256=0BA86C2EE614C2AD7029DF75FB10D8EA5E8A7AF3D366AF81F414E6492F32E849. 本次使用的原样隔离副本: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/build_kb.mjs. 来源报告记录两者字节相同, 不是另写一个独立提取器交叉验证.
- 输出目录: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517. 各文件旧快照 SourceSHA256, 新输出 ReextractedSHA256 和条目数均保存在上述清单, 不是历史生成时已经存在的溯源信息.
- 生成日志: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/generator.stdout.json, 其中 warnings 为 []. 错误日志: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/generator.stderr.log. 无警告和退出成功仅描述此次生成过程, 不证明解析器及所有数值正确.

## 2. 13 份重提取与 14 份清点不是同一覆盖范围

- 13 份实际输出由 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/reextract-manifest.json 与 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/generator.stdout.json 共同列明: 十份 cards-red.json, cards-green.json, cards-blue.json, cards-purple.json, cards-colorless.json, cards-curses.json, cards-status.json, cards-tempCards.json, cards-optionCards.json, cards-deprecated.json;另有 relics.json, potions.json, events.json. 这些文件均位于上节给出的绝对输出目录.
- 数量为十份卡牌合计 438 条, relics.json 186 条, potions.json 43 条, events.json 54 条. 数量相符不等于注册链完整, 所有内容可获得或每个数值独立验证通过.
- 根目录 14 份 JSON 的清点证据: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/reports/knowledge-json-provenance.md. 第 14 份为 G:/omp works/Sts/sts2-spire1/research/sts1-kb/monsters-scan.json, 73 条记录中含 AbstractMonster. 这是类扫描记录数, 不是实际可遇见怪物总数. 不将清点它与成功重提取它混为一谈.

## 3. 原始差异仍然存在

- 原始 SHA256 比较结果: 13 对中 11 对逐字节相同, 即十份卡牌与 potions.json;events.json 和 relics.json 不同. 证据为 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/reextract-manifest.json.
- 未做标点映射的对象比较: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/semantic-comparison.json. 两份差异文件均 semanticEqual=false, events.json 的 differenceCount=53, relics.json 的 differenceCount=46. 这里的字段名 semanticEqual 仅指比较程序的对象相等结果, 不是游戏语义正确性评级.
- 上述计数按记录字段统计. 来源审查报告 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/reports/knowledge-json-provenance.md 另按字符串叶统计 events.json 为 56 项, relics.json 为 46 项;options_zh 数组的字段和元素不是相同计数口径. 不将 53 个字段写成 53 个字符串叶或测试断言数.
- 差异不只在 zhs: events.json 包含零起始索引 $[29].description_en. 定位见 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/semantic-comparison.json;旧文件 G:/omp works/Sts/sts2-spire1/research/sts1-kb/events.json:671 与新文件 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/events.json:671. 本页只列字段和路径, 不复制本地化正文.
- JSON 解析后的字符串仍不同, 因此不是只改了转义写法或序列化排版. 原始差异没有因后续映射比较而消失, 本次也未覆盖旧快照或新输出.

## 4. 限定标点映射的等级与两次结果

- 只对新对象的字符串值递归执行单向映射: U+2018/U+2019 -> U+0027; U+201C/U+201D -> U+0022; U+2014 -> U+002D; U+2026 -> 三个 U+002E. 不改变对象键, 数值或布尔值. 比较源码: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/verify-punctuation.cjs.
- 首次结果: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/punctuation-mapping-first-harness-result.json, 时间 2026-09-23T03:00:33.870Z. 两文件均 rawEqual=false, lossyPunctuationMappingEqual=false. 主会话在恢复说明 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/requests/knowledge-provenance-retry-once.md 中明确归因于首次夹具将省略号替换为两个点;这是夹具错误, 不是产品数据缺陷. 本次文档任务未重建或运行首次夹具, 不把该归因写成本任务独立复现.
- 修正后结果: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/punctuation-mapping-result.json, 时间 2026-09-23T03:01:04.493Z. 已读源码使用 String.fromCharCode(46).repeat(3), 两文件均 rawEqual=false, lossyPunctuationMappingEqual=true. 两个时间点分别为本地 2026-09-23 11:00:33.870 与 11:01:04.493, UTC+08:00;保留先前失败证据, 不将其当成后次结果.
- 当前可确认的等级是主会话已实际执行并保存的限定映射后全对象相等比较. 在这两个固定对象和该映射范围内, 差异可被标点转换解释. 本次文档任务只读取结果和源码, 未再运行比较脚本.
- 该映射有损: ASCII 引号不能唯一还原左右引号, 点和短横也不能唯一反推原字符. 映射后相等不是原始字节相同, 不是文本原样照录, 不是所有数值已独立核验. 转换是谁在何时以何工具完成仍未知, 不能据此自动反向还原或覆盖任一 JSON.

## 5. monsters-scan.json 的生成链尚未补齐

- G:/omp works/Sts/sts2-spire1/research/sts1-kb/build_kb.mjs:326-331,345-378 只写上述 13 份 JSON, 不生成根目录 monsters-scan.json.
- G:/omp works/Sts/sts2-spire1/research/sts1-kb/scan-hooks.mjs:4,59-73 使用旧路径的 class 树, 输出 name/hooks 结构到旧路径 .tmp-javap 目录. 根目录 monsters-scan.json 是 monster 加九个布尔字段的结构. 未建立从该脚本及 class 树到当前根目录文件的完整转换关系, 不因此断言整个仓库不存在其它生成器.
- 主会话实际运行原样隔离副本的失败证据: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/scan-hooks-result.json 与 G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/evidence/kb-reextract-105517/scan-hooks.stderr.log. 结果 ExitCode=1, RequireNotDefined=true, .mjs 顶层 require 在 Node ES module 入口报错. 这不是一次成功的怪物重扫描, 本任务未修复或运行该脚本.
- 尚缺与当前 schema 对应的真实生成器, 源 class 树与输入 JAR 指纹关联, 逐字段的声明方法/继承方法/调用点判定规则. 来源链缺口不直接证明 73 条布尔值错误, 也不证明其正确.

## 6. deprecated 分类与缺本地化不等于不可获得

- 分类源码: G:/omp works/Sts/sts2-spire1/research/sts1-kb/build_kb.mjs:192,217-218,304-320,326-331. deprecated 输出混合原 deprecated 包记录及双语名称均为空而从其它包转入的记录. tempCards 和 optionCards 仍为各自独立文件.
- 输出记录不保留原 pkg 与完整 class 路径. Impulse 的 note 只描述缺本地化, 不能替代 CardLibrary, 卡池注册或可达性调用链证明. 相关旧记录定位: G:/omp works/Sts/sts2-spire1/research/sts1-kb/cards-deprecated.json:1199-1214. 本次不修改该分类或补造来源字段.
- 同样, 测试遗物, 占位药水, 事件文本键或抽象基类在数据中存在, 不自动等于正式可获得内容. 本轮不认证 Beta/正式发行边界, 不认证每个记录的运行时可达性.

## 7. 仍未完成的验证与改动边界

- 本次清单补齐当前输入, 生成器和输出的运行关联, 没有补回历史 JSON 的生成时间, 原始输入身份或标点转换步骤. 旧快照没有内嵌来源清单, 不以新证据倒填历史事实.
- 原样生成器在同一输入上输出相同, 可能重复同一个解析器假设或缺陷. 需要独立权威字节码定位和注册链核对才能提升数值与可达性证据等级;本页不声称已完成这些工作.
- 只修订 G:/omp works/Sts/sts2-spire1/research/sts1-kb/README.md 与本页. 不改 JSON, 生成器, mechanics, 配置, 游戏安装或历史归档, 不运行测试或构建, 不提交. 后续执行或数据发布须另按授权范围进行.
- 修订契约: G:/omp works/Sts/sts2-spire1/docs/KNOWLEDGE-RECHECK-20260923.md. 原始来源审查: G:/omp works/.tmp/workspace-audit-20260923-01a0cbfd/reports/knowledge-json-provenance.md. 上述绝对路径指向本机本轮证据, 不作为跨机器都存在的公共制品链接.
