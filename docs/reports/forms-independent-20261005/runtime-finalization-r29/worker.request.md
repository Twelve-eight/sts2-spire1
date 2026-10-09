你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api; reasoning xhigh; native Codex; no fallback`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-finalization-r29\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 3 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.
## 精确实现白名单
除唯一报告外, 仅可改 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs 与同目录 RUNTIME-SAFETY.md. 本节覆盖模板代码禁止项. 所有生产和共享FormNativeSmokeRunner.cs禁止改. 不构建/lint/测试/Git/游戏, 不委派/peer/其它harness/模型. 最多8分钟, 每完成一面立即落盘.

## 已确认复现与依据
主hub已真实wait收割r26补审NEEDS_REWORK, 原实现CODE_COMPLETE门禁已保留. 阻断证据 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-safety-recovery-r26\supervisor.md 面3. 冻结hash F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919. 除收尾外三命令主线程/settled/noMutation及未选局damage静态面通过, 不重复改玩法fixture.

## 最小最终收尾契约
1. pre-quit写报告前先设置exitCode/quitStatus/quitDrainSettled/finalEvidencePhase="pre-quit". pre-quit-only不能被视为final成功. 不再先写completed再补phase字段.
2. final fault gate之后统一收敛passed/status/exitCode, 任意late fault, quit/drain失败或report-write失败都明确failed/passed=false/exitCode=1. 若SceneTree.Quit已提交0而后续失败, 必须对当前仅TEST进程尽最大可实现方式保证真实非零退出, 例如finally末在写盘后Environment.Exit(1)仅限失败; 成功路径不硬退. 不退出其它进程. 不只修改报告exitCode却让真实进程0冒充成功.
3. 原始fault完整保留: 当前RunRuntimeSafetyAndQuitAsync在final只ApplyUnobservedFaultGate(...faultsObservedAtScenarioEnd..., null), 会覆盖scenario原始fault数组. final必须保留从订阅开始的全部raw/typed/expected/unexpected, 审批root仅来自实际已settled明确Forms restart的command root, 使用r27单root引用身份规则, 不按文本吞未知异常. 可通过本文件内参数把真实expected root集合传回final; 不把Exception对象直接进JSON. selected/unselected/invalid统一fail closed. 不删除已有原始证据, 不将scenario count当full capture.
4. 不改现有持久化字段名/正常check字段schema/命令数/action数. 新增字段如确有必要在报告明确; 并行控制器r28正在基于现有schema实现, 最终会由hub集中正反例校验. RUNTIME-SAFETY.md只更新收尾说明和诚实边界, 不伪称已实机.
5. 交付准确新hash/行号/最小diff, 列出全部改动路径. Flag不能实现的部分, 不留stub. 最终CODE_COMPLETE. 未验收静态边界与实机证据分开.