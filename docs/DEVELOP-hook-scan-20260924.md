# 知识库 hook 扫描器修复契约, 2026-09-24

## 已确认问题与证据边界

本轮已用原样 scan-hooks.mjs 的隔离副本实际复现顶层 require 在 Node ES module 中失败. 历史证据与源码指纹保存在 G:\omp works\.tmp\workspace-audit-20260923-01a0cbfd, 详见 research/sts1-kb/REVALIDATION-2026-09-23.md 及对应 provenance 报告. 当前脚本还硬编码已迁移的旧根路径, 将 javap 失败吞为无 hooks. 这些问题与现有 monsters-scan.json 的来源链缺口分开处理.

## 行为契约

1. 使用标准 Node ESM, 保留原有 PATTERNS 的键, 正则, 检测语义以及 name/hooks 输出 schema. javap 检测的是声明的成员, 不是完整继承行为或游戏可遇见集合.
2. 保留 powers 为默认包和单个包名位置参数, 提供 --base, --output, --javap 与 --help. --base 指向 com/megacrit/cardcrawl 的 class 根; 默认从脚本位置解析当前 .tmp-javap/cls/com/megacrit/cardcrawl. 输出默认仍为当前脚本 .tmp-javap/<pkg>-scan.json. 不重建旧根路径. javap 可从 --javap 或 JAVA_HOME/bin 解析, 最后才使用 PATH, 文档要求本机中央验证显式指定 JDK 21.
3. 包名仅允许单一安全目录名, 拒绝绝对路径, 路径分隔符和上级穿越. 明确拒绝输入树符号链接, 不跟随到范围外. .class 文件排序稳定, 继续排除名称中带 $ 的内部类. 缺路径, 空 class 集, javap 非零/超时/输出超限必须非零失败, 不能生成看似有效的空 hooks 记录.
4. 不通过 shell 拼接命令. javap 参数用 execFileSync 数组, 设置有界超时和 maxBuffer. 首个失败保留明确类路径和原因, 不用 ERR 伪造正常输出.
5. 全部 class 成功后才写目标. 同目录唯一临时文件, 独占创建, 最后原子重命名. 任意失败保留既有输出字节, 清理仅限自己创建的临时文件, 不递归删除.
6. 不改根目录 JSON, build_kb.mjs, 术语, 现有 mechanics 或 provenance 文档. 单独 HOOK-SCAN.md 说明如何复现和未验证范围, 不称该工具修复了旧 monsters-scan.json 来源链或已完成全知识库语义验收.

## 集中验证

工作者只写代码和限定文档, 不执行构建, lint, 测试或生成器. 主会话使用 G: 隔离输出, 真实 JDK 21 javap 与原版 JAR 抽取的 class 执行, 另用合成错误 class/失败验证器检查失败不覆盖, 路径空格和确定性. 不运行游戏, 不写 Steam, 不修改共享配置. 同批监督在真实原生完成门禁后开始. 保留全部旧 staged/dirty 内容.
