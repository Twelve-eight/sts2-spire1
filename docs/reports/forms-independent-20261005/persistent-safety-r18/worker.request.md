# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\mod\FormsCode.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\persistent-safety-r18\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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

---

## 主会话收割方法

```powershell
Get-ChildItem -LiteralPath '<报告目录>' -File | Sort-Object LastWriteTime |
  Select-Object Name,Length,LastWriteTime
```

- 子代理超时/中断/额度耗尽时: 先读报告文件, 把 `已确认` 当作可用证据, `进行中` 当作半成品复核, 不得当作无产出.
- 收到第一批落盘结果后即向用户汇报一次, 后续增量补充.
## 写集与硬门禁
唯一报告如上, 另授权以下代码文件, 不得改其它文件:
G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs
G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs
G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs
新文件 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs
不构建/lint/测试/部署/游戏/git/新子代理. 不启动其它harness/模型/fallback. reasoning xhigh.
现在先研究和增量报告, 不改代码. 必须等hub给出r5真实基线复现的落盘路径和BASELINE_CONFIRMED通知后才写代码. 不以源码audit当真实复现. 若读完前置而仍无通知, 回复等待即可, hub会send_input.
同批监督由hub原生wait你的completed后通知, 完成写CODE_COMPLETE并给hash/精确行号/未知边界.

## 已定契约与前置
先读 G:\omp works\Sts\sts2-forms\DEVELOP.md 的最新持续战中保护增量契约; G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-loss-audit-r10\review.md; stable-guard-api-scout-r15\review.md 与 cleanup-guard-scout-r17\review.md (均在同父报告目录).
权威当前引擎源码 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc . r17报告标题/建议有漂移, 不使用单独IsEnding判live; CombatState.IsLiveCombat恒true, 禁止依赖.
范围只修已选Forms局桥Terminal/Shutdown/pending失效后静默恢复原生规则. 普通不选Forms局必须保留原生规则. 生产仍r5未改, test-only载体另有人负责, 不改测试文件.

## 实现要求
1. 让本局FormStanceModifier存活的原生model hooks提供支付前ShouldPlay, 伤害HP/Block落地前, power amount变更前, side-turn start/end very early边界的fail-closed. 精确override签名查权威源码. 不以支付后的BeforeCardPlayed冒充支付前保护. 真操作中抛标准InvalidOperationException, 消息同时含Forms unavailable与restart game process及UnavailableReason. 不默认声称throw能回菜单或终止整局; 不吞异常退回原生行为.
2. 唯一必要的独立engine prefix补PowerCmd.Remove(PowerModel?)不经过model amount hook的marker删除路径. owner固定Forms.FormStanceSafety, 显式最早初始化安装, 不被MainFile常规扫描重复注册. Harmony实际目标+owner+prefix精确身份数量1须证明; 安装或证明失败使MainFile.PatchesHealthy false且无法Bound, 不被随后常规扫描成功覆盖.
3. Safety guard进程生命周期, 不因MainFile.Shutdown或Watcher bridge Terminal/rollback撤销. 它只持有引擎固定方法/本项目方法, 不持有Watcher Assembly/MethodInfo/delegate. 三native marker FullName权威为WatcherMod.Wrath/Calm/Divinity, 可另外识别本项目carrier, 不猜额外类型. 重复Initialize/Shutdown幂等, 关闭后的两原owner可清0但安全owner故意驻留; ProcessExit不用Godot或重装Harmony. 不宣称真热卸载.
4. 只拦本局已选Forms且当前实际live战斗且桥不可用. 对PowerCmd.Remove必须精确关联power.Owner.CombatState是当前战斗, manager.IsInProgress且非IsEnding; 用当前combat id或current state引用核对, 不凭全局是否任意战斗live. 缺Power/null/普通局/非当前combat/已结束或ending teardown放行. modelhooks同样避免影响非Forms/非当前combat, 本局modifier存活时全体战斗目标的伤害不能绕过.
5. PatchesHealthy已有owner安装规则保持; 标注FormStanceMode.IsSelectedAndBound等旧说明中静默native fallback只适用于非Forms, 不能再把已选+失效写成普通模式. 只改必要注释/统一错误消息, 不改6个效果数值/技能/旧CustomID/资源/反射协议.
6. 可预测复杂度: 正常路径轻量O(1)或仅现有本局modifier查询, 不每帧枚举Harmony/AppDomain或大内存, 无工作线程Godot; 锁顺序避免与Bridge.Gate/PhaseLock死锁. 如果某API或逻辑不能准确实现, 写阻塞证据, 禁止stub.