
你是 实现者, 范围: G:\omp works\.tooling\refresh-workshop-payloads.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `gateway, 用户指定 wb2api, 更细实际路由需元数据证据`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004/source-freshness-worker-r4.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 2 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

## 实现者特例与精确写集
本节替代模板的产品只读限制. 除唯一报告外只允许修改 G:\omp works\.tooling\refresh-workshop-payloads.ps1 的freshness Where-Object最小条件与相邻必要注释. 不写其它文件, 包括临时取证文件. 不构建/parse/lint/test/pack/提交, 不再委派. 主会话集中运行验证与精确快照.
先读 G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md 的新增文档JSON契约. 真正全量VerifyOnly已复现 docs\reports\workshop-prep-20261004\pck-warning-worker-r3-wait-gate.json 触发Spire1 REBUILD_REQUIRED; 中央源码前后1404文件无变, r15DLL未变. 证据 G:\omp works\.tmp\workshop-prep-20261004-central\pre-refresh-final-readonly.log/json. 旧脚本精确快照 refresh-workshop-payloads.pre-doc-freshness-r4.ps1 同目录, 隔离复现脚本 run-source-freshness-fixtures.ps1 由中央执行, 不自行运行.
当前freshness段L860-880扫描 *.cs/*.csproj/*.props/*.json, 对repo根docs报告JSON无区分. 最小方案只排除相对repoDir路径以docs/或docs\开头且扩展名为.json的文件. 可以在原Where-Object增加一个有括号的条件, 用相对repo路径Substring且case-insensitive匹配. 禁止仅在全路径ignored dirs加入docs, 那会误排除mod/.../docs的runtime JSON. 禁止排除docs中的cs/csproj/props, 因ChaosBridge等根项目的默认Compile可能包含docs源码. 禁止改mtime,移动报告,改变其它excludes/held-back/全量/pack/hash/digest/copy策略.
静态核对七项目主csproj/props/project.godot与AdditionalFiles资源模式, 确认根docs JSON不是真实生产输入. 路径行定义就在脚本L133-140. 如存在根docs JSON真实依赖, 先落盘并收紧匹配或返回NEEDS_DECISION, 不猜测. 范围只读7项目契约, 可grep固定内容配置, 不大范围扫描SDK.
第一条源码结论马上写报告, 每完成一个面立即追加. 最终 CODE_COMPLETE, 行号/最终hash/修改文件/未验证面. 实现者与监督同批, hub真实wait completed后激活. 本轮仅global:deepseek-v4.1-flash,max; 细路由以session metadata为证.


## 同一freshness面的追加已复现缺陷
中央隔离矩阵发现原excludes匹配完整绝对路径. 合法隔离Root位于workspace .tmp下, 所以每一个输入都因外层.tmp匹配被排除. before-ps7中新生产cs/csproj/props/json均Exit0, 不触发REBUILD_REQUIRED. DLL实际mtime是2026-10-04T17:39:20Z, 新cs实际mtime是2026-10-04T21:15:44Z, 不是时间造假或未来fixture. 这是额外漏查, 不能采信隔离docs JSON正例为旧版文档误报复现; 旧版docs误报证据来自真实Sts全量pre-refresh-final-readonly.log.
最小修复还须把原ignored dirs匹配改为相对repoDir路径, 不让repo外workspace .tmp祖先影响源码扫描. repo内根级/嵌套原有obj/bin/.godot/node_modules/.tmp/.nuget/.dotnethome/research/tools仍须忽略, 可给相对路径加前导分隔符后匹配原regex. 同一相对路径用于根docs JSON识别. 保留四extensions/mtime/held-back过滤和所有失败闭合. 不增加全盘扫描或重构.
中央将增加internal-tools-cs/internal-research-json/internal-obj-cs三项不触发REBUILD_REQUIRED的正例. 最终隔离矩阵13项, 原版7个真实input漏查, 修复后13项必须通过. 主会话集中执行, 你不执行测试. 每完成此面立即落盘, 不攒到最后.
