你是 实现者, 范围: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r28.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api; reasoning xhigh; native Codex; no fallback`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-controller-r28\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 4 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.
## 实现任务与精确白名单
除唯一报告外, 仅可新增 G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r28.ps1. 本节覆盖模板代码禁止项, 不可改其它任何文件. 基线 G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1, SHA256 E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1. 从此冻结副本复制, r24原件与旧模式原语义必须保持. WRITE code only, SKIP build/lint/tests/游戏/Git. 每完成一面立即写报告; 最多8分钟完成. 不委派/peer/其它harness/模型或fallback.

## 具体需求
1. 增加SmokeMode runtime-safety与runtime-unselected, 映射实际开关 --forms-runtime-safety 与 --forms-runtime-unselected, 分别只跑1个独立场景, caseId r1-runtime-safety 与 r2-runtime-unselected. 两场挂BaseLib/Watcher/Forms/FormsNativeSmoke, 不挂Spire1. headless全机制保留.
2. 所有新schema来自实际冻结源 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs (SHA256 F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919) 与同目录RUNTIME-SAFETY.md. 此源正在补静态监督, 不可更改, 有不足flag而不是编造字段或宽松放过. 调用者形态故障分类r27正在独立窄修, 不复写已有双action parser.
3. 新运行报告严格typed验证 testOnly=true, scenario精确对应开关, status=completed, passed=true, exitCode整数0, 最终阶段post-quit, 完整drain/cleanup成功. 三命令恰3且各有唯一label, 真实command提交/settled/非timeout/noncancelled/明确Forms unavailable+restart/所有原始before-after证据严格unchanged; 不直接prefix调用. 精确guard target/owner/prefixCount1/prefix类型RemovePrefix/identity稳定, Shutdown旧两owner0. 未选局恰2真实action, 成功finished且同一CardModel reference的started/finished各有增量, Strike实际damage与源期望相等并原生支付, Tranquility raw Wrath->Calm, selected=false/carrier无Forms效果. 不靠顶层bool直接判通过.
4. productionIdentity必须loaded=true, location为实际G:隔离客户端加载路径, sha256精确等于本次FormsPayload/Forms.dll (不是固定r18hash). 原始fault数组与expected/unexpected严格typed, 任意不明/未关联或late fault失败. 若现有报告无法独立证明某点, 保守拒绝并明确flag缺口. 不只检测文件存在或passed字串.
5. 保持全旧解析函数与5旧模式行为, 所有隔离路径deny/reparse, 共享config hash不变, 游戏超时/无窗口/日志排空/fixture staging/保留mods与cleanup机制不改. Test-Evidence原分支在新模式else分支不受影响. 局部可新增Test-RuntimeSafetyEvidence从Test-Evidence分流, 不引入全局自动变量Error或吞未知字段.
6. 在报告列每个新增函数和schema字段权威行号, 最小差异, r24与旧主体未变hash, 新文件最终hash. 未实现项flag. 最终CODE_COMPLETE, 全部路径列表.