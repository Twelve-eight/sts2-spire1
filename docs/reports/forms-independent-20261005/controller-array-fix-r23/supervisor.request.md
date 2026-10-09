# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 监督审查员, 范围: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r23.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-array-fix-r23\supervisor.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 同批门禁
同批worker是原r22实现01a109ed-ebd7-7df2-812b-28c22e7b4d8e, 现在进入独立r23窄修. 当前r22检查面先收敛写原supervisor.md, 然后本阶段只写本目录supervisor.md等待hub真实wait该worker最新completed并落盘gate-notice后send_input. 现在不提前审r23产物, 不peer工具/委派/其它harness/构建测试运行git.
门禁后只核对r22到r23数组边界窄修与hash. 空数组/单元素数组保留类型, null/missing/scalar仍拒绝, 其它身份关联/final和lifecycle所有fail-closed语义不能弱化. 按中央array-shape实测和源码双证据核对, 不声称实机. 不无限扩研究, 3分钟有界, 最终SUPERVISION_PASS或NEEDS_REWORK.
## 后置新增范围, 替代前述仅数组的窄范围
r22最新原生监督已返回SUPERVISION_NEEDS_REWORK共3项, 权威源报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-evidence-r22\supervisor.md 最后三检查面. 监督本r23单一脚本对该3项的修复, 在10分钟内有界完成:
1. 原先数组保真必须修.
2. action proof内sequence不得重复或跨action重复认领; raw与expected同sequence必须exceptionType/exception/observedUtc全相等且raw文本对应. final复用同强度字段/sequence/对象校验, 拒绝重复raw或无效expected, 不只比较集合与数量. 两action对应真实标签/cardEntry, 不信顶层bool代替未完成分量. phase3现在已经真实CODE_COMPLETE并有phase3-gate-notice.json, 可读取完成的schema, 不再空等: BindingLossSmokeRunner.cs=DE9956D6664426FDC48477C083B07E698E35BE16CA7ACFD1F178D4B1BC6B272A, FormNativeSmokeRunner.cs=4796BC487413BDE0C54E64159FF175FA88B2C67709E102E9D09BE30ADD86362D. expectedRejectionsExpected当前为实际roots数量, 不能冒充raw event数量; 一action允许多个按真实identity派生的事件, 根数应精确双action而所有event唯一认领.
3. lifecycle精确Remove目标/单参数/Forms.FormsCode.FormStanceSafetyGuard.RemovePrefix身份与真实生产类, 读取ownerPatchCountAfterShutdown两个精确旧owner为整数0. 完成的LifecycleSmokeRunner.cs实际输出格式为准, 不造类型字符串.
其它模式/隔离/路径/配置/hash/Move不改. 不新增运行开关或扩展core probes模式. 只写supervisor.md, 不改代码, 不测试或委派. 完成一面先落盘, 最终CODE_COMPLETE和hash/未验证边界. 同批监督只在最新hub原生wait completed门禁后审r23.