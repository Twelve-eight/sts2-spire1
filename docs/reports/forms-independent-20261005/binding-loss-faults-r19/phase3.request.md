# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\worker-phase3.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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

---

## 主会话收割方法

```powershell
Get-ChildItem -LiteralPath '<报告目录>' -File | Sort-Object LastWriteTime |
  Select-Object Name,Length,LastWriteTime
```

- 子代理超时/中断/额度耗尽时: 先读报告文件, 把 `已确认` 当作可用证据, `进行中` 当作半成品复核, 不得当作无产出.
- 收到第一批落盘结果后即向用户汇报一次, 后续增量补充.
## phase3窄返工写集
只改BindingLossSmokeRunner.cs和FormNativeSmokeRunner.cs, 唯一报告worker-phase3.md. 不改生产/Lifecycle/README/控制器. 不构建/lint/tests/game/deploy/git, 不再委派/peer工具/其它harness/模型/fallback. 同批原监督已原生收割NEEDS_REWORK, 会等本阶段hub真实wait completed才审. 最多10分钟, 每面立刻落盘; 未实现就flag不stub.
1. 先修中央编译两处确定错误: 权威日志G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r19-final.log . BindingLossSmokeRunner.cs:789缺MegaCrit.Sts2.Core.Entities.Actions的GameActionState命名空间; :849 named argument改为精确record参数ActionExecutedAndSettled. 不造枚举值.
2. phase2内部Exception reference identity关联不可放宽. 现ActionExecutedAndSettled/identityMatched等只在内存record, JSON无法复核是否双实际牌都真正执行settled. 请在场景result新增actionRejectionEvidence数组(恰两项, Strike与Tranquility, 进入牌不计), 每项至少label/cardEntry/actionExecutedAndSettled(bool)/explicitFormsRestart(bool)/actionFaultRootType(string)/identityMatchedFaultSequences(int数组). 序号只能来自实际按ReferenceEquals匹配该action root的原始UnobservedFaultEvidence. 不文本关联, 不造sequence, timeout/cancelled无fault/未执行不产生有效proof. 对应action对象保留真实state/completion/fault等原始证据. 安全passed必须同时要求两proof实际执行settled/明确Forms重启异常/真实identity关联成立.
3. 按原请求把fault证据落到final也补齐: 保留当前进程所有raw异常对象, 仅把已实际执行settled且身份关联证明成立的真实action fault roots传到final分类, 未选此场景的普通smoke仍全部strict. final写unobservedFaults/unobservedFaultEvidence/expectedRejections/unexpectedUnobservedFaults/unobservedFaultGatePassed, 与场景的sequence是同一单调来源. 保持订阅直到post-quit排空结束, 最后finally退订, 不漏捕获cleanup/quit late fault. 可在RunAsync到binding-loss调用链显式传递本次roots列表, 不使用持久全局吞错列表. late fault如果不匹配既有real root必须失败, 不按文本吞Forms错误. pre-quit与post-quit最终字段不能覆盖掉failure, 不把timeout当settled. 未实现全部final proof时如实报告, 不强制通过.
报告完成写CODE_COMPLETE, 精确2文件hash, 未实机边界.