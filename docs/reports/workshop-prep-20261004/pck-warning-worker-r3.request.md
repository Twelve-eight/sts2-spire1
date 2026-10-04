
你是 实现者, 范围: 四项目 ReadPckMetadata 内联任务.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `gateway, 用户指定 wb2api, 更细实际路由需元数据证据`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:/omp works/Sts/sts2-spire1/docs/reports/workshop-prep-20261004/pck-warning-worker-r3.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 1 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

## 实现者特例与精确任务
本节替代模板的产品只读限制. 除唯一报告外只允许修改以下四文件的新增 ReadPckMetadata 块: G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj, G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj, G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj, G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj. 不改其他文件, 不提交, 不运行 build/parse/lint/test/pack, 不再委派.
中央真实四项目 Release Rebuild 已通过, 但每项目新增 1 warning CS0162 来自 RoslynCodeTaskFactory 生成的临时文件, 因 Fragment try 和 catch 都返回, 自动尾部不可达. 证据: G:\omp works\.tmp\workshop-prep-20261004-central 下 <Id>-release-rebuild-r2-before-warning-fix.log 与 four-mods-rebuild-results-r2-before-warning-fix.json. 旧监督快照 <Id>.csproj.r2-supervised 与最初 <Id>.csproj.before 都在中央目录.
消除本轮引入的 CS0162, 不 suppress warning. 首选最小方案: Fragment 成功路径删除显式 return true, 让工厂自动生成的尾部按 Log.HasLoggedErrors 返回; 保留 missing/error 的 false 返回, 证实符合真实工厂契约. 若无法取得此默认尾部契约的源码证据, 则用 Code Type=Method 与 public override bool Execute() 明确包住原有 try/catch, 保留原 true/false 语义, 不改变 ParameterGroup/输出属性/TaskName/targets. 明确选择证据与原因, 不用猜测. 四份补丁保持相同, 编码/BOM/换行/尾部原样. 不改已有 Perfect 原始 dirty.
契约为 G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md. 第一条结论立即落盘, 完成每面追加. 最终 CODE_COMPLETE, 列出四个路径/最终hash/未验证项. 同批监督由主会话 wait completed 激活; 真实复验由中央执行.
