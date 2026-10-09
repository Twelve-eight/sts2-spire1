# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-recovery-r24\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 固定边界
本轮思考层级xhigh. 只准当前Codex原生设施, 不再委派, 不启动codex exec/omp/其它harness/模型/fallback. 禁peer thread/list/read/wait等工具, 不读凭据配置或改路由. 不构建/lint/测试/部署/游戏/git, 不写C:/Steam/共享配置/canonical/Workshop. 主hub集中验证.
旧会话部分后续阶段实际误用ovoapi:6.1sol/max, 已关闭且这些阶段的实现/监督不作交付. 当前候选仅是参考, 不得继承旧报告PASS或自己用请求文字宣称实际模型. 以当前权威引擎契约独立完整重审受影响差异并完成新DS产物, 然后同批监督/中央重建/正反例/实机重新建立证据. 首条结论立即落盘, 每完成一面落盘, 10分钟内有界; 无法准确实现的标缺口而非stub. 不因已有候选可编译就跳过审修.
## r24任务
只新增 G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1 和唯一worker报告. 不改r20/r22/r23/矩阵/测试/生产. r23是错误模型候选, 不直接继承其PASS; 从r22与r23差异完整独立重审并生成新r24, 新落盘代码需当前DS承担所有新增解析语义.
最小保留: 脚本无窗口独占客户端/G:环境/共享配置hash/日志排空/路径与reparse deny/Move主体, effects/saveguard/矩阵输入等原语义不改. 不扩runtime core模式. PowerShell宿主7.6.5, DateKind String可用.
必须独立落实:
1. typed数组保真empty/single/multi, null/missing/scalar拒绝, 禁pipe枚举造成array丢形状. 真实JSON PSCustom类型整数/非空字符串门, 按精确Ordinal不宽容大小写.
2. 恰双实际action: proof label/card和真实run对应, BeforeExecuted执行+settled+非取消故障+明确重启分量, 每proof及跨action序号不得重复认领. roots计数2是action数不是events数. raw/expected/final逐sequence的exceptionType/exception/observedUtc全相等, raw文本对应, 无遗漏/重复/伪对象/未认领事件. final与scenario精确对象对照; 无持久化proof的late事件保守拒绝, 不按文本制造关联. 所有支付/HP/block/power/原始marker/伤害/历史变化必须fail.
3. 同批r25约定增加nextStrike/stanceChangeProbe.actualCardHistory对象, 必须纳入门禁:
card为对应run.card, cardTypeFullName为对应真正类型(WatcherMod.WatcherStrike_P或WatcherMod.WatcherTranquility), cardInstanceIdentity为非空字符串, observedByCardReference为true. 四字段beforeStartedCount/afterStartedCount/beforeFinishedCount/afterFinishedCount必须非负integer且前后相等. 未观察/缺字段/actual history变化均失败, 不再让Tranquility独立出牌被固定Strike history漏掉. r25源码进行中不能读取, 只按本约定实现解析. 若真正类型FullName对照本地Watcher权威source发现有差异, 先flag证据, 不猜字符串.
4. lifecycle精确稳定引擎单参数PowerCmd.Remove(PowerModel power)与生产Forms.FormsCode.FormStanceSafetyGuard.RemovePrefix, owner Forms.FormStanceSafety, prefixCount整数1, target/type/method数组恰1, RemoveMethodIdentity前后稳定. 两旧owner Forms 与 Forms.FormStanceMode.Watcher必须整数0, 顶层bool不能替代.
5. 最终独立核对旧主体区域及文件hash未变, 中文记录已确认/进行中/未知. CODE_COMPLETE新r24hash, 不称AST或实机通过.
纯夹具参考 G:\omp works\.tmp\forms-independent-20261005\controller-r23-central-fixture-results.json 有55项本地主会话机械输出, 不代表候选来源或实机通过. 不自行测试, 由hub对新字节重新跑.
同批监督已由主hub安排, worker本阶段现在可开始. 完成后列实际修改文件/hash, CODE_COMPLETE或PARTIAL并停写等hub门禁. 不自行轮询监督.

