你是 接续实现者, 范围: 本文件补充范围列出的六个绝对路径.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `gateway, requested wb2api; finer route only if metadata exposes it`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\resume-finalizer-r2.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 8 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.


## 接续实现者的补充范围

上面的单报告只写条款对报告成立. 本轮接续实现者还仅可编辑以下六个产品文件, 同批有两个独立监督者. 原代理句柄已 not_found, 不采信未完成监督. 不重做已落盘实现, 不开展新研究, 不构建/parse/lint/test/预处理/pack/deploy/Steam/游戏/共享配置/C:写入, 不创建scratch文件. 无缺口就零代码改动并尽快返回 CODE_COMPLETE.

G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1
G:\omp works\.tooling\refresh-workshop-payloads.ps1
G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj
G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj
G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj
G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj

读取 G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md 和原 release-pipeline-worker-20261005.md, quick-pck-worker-20261005.md, 都在本报告目录. 确认原六文件最终交付仍符合其实现规格; 必要的最小收尾才可编辑. 重点是四项目独立于部署的 producer 是否在本次成功pack后生成 digest, skipped/failure/disabled/inner-export 不能重标旧包, canonical输出路径不可错误. 对脚本保留此前 SHA/mtime/路径/PDB/全量预检/held-back门禁.

每个检查面立即落盘, 最终列全六文件绝对路径和 SHA256, 具体改动或无改动, 返回 CODE_COMPLETE. 只负责接续交付, 不审自己的实现为通过. 监督者由主会话同批派发, 完成后立即结束本轮, 不等待监督者. 控制在5分钟内完成这个交付确认, 如确有复杂缺口先落盘并返回 NEEDS_SPLIT.