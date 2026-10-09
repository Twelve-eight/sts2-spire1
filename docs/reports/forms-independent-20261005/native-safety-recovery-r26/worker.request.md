# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-safety-recovery-r26\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## r26精确任务与限制
只准global:deepseek-v4.1-flash/wb2api/xhigh. 不fallback/再委派/其它harness/peer工具, 不构建/lint/测试/运行游戏/部署/git. 不写C:/Steam/共享配置/canonical/Workshop. 主hub集中验证. 一面完成即报告, 10分钟有界. 若无法准确实现则PARTIAL和缺口, 不stub/猜API.
worker只可改RuntimeSafetySmokeRunner.cs与RUNTIME-SAFETY.md, 另写自己唯一报告. 不改r25原有测试源/README/工程/生产/控制器. 原r21 worker首轮DS实现是未编译半成品; 旧监督轮次错误模型, 不继承其验证结论. 读native-safety-probes-r21/supervisor.md仅作候选定位, 所有事实需当前权威engine-dllsrc独立核对. 中央诊断日志 native-smoke-build-r19-phase3-r21-diag.log真实0 warnings/8 errors.
r25测试基线由主hub在启动本阶段时补baseline-gate.json, 只读该冻结文件/hash. 不读取正在写的另一个任务, 本阶段代码写集与r25完全分开, 私有partial helpers可复用但不改其定义.
必须修复:
1. 两file-scoped namespace改为正确block namespaces, 原Patch/Run实际全名及partial归属不变. 各API只核当前engine-dllsrc/watchermod, 不猜成员. 单靠编译不算guard证据.
2. 读取当前已载入Forms assembly Location与SHA256, 记录生产身份必填且从G:文件确实可读; 不拿候选磁盘hash冒充已载入location. guard前后真实Harmony精确单参数PowerCmd.Remove(PowerModel)与Forms.FormStanceSafetyGuard.RemovePrefix, owner=Forms.FormStanceSafety/prefixCount=1, Shutdown后两个旧owner整数0, identities前后相同; 所有这些实证必须参与passed.
3. 所有启动/绑定/模型/Godot/真实对象状态读取和command submission都走已知主线程gate, TryBind不在await之后直接调用, 不强行解除暂停. readonly快照记录真实线程门. 不调用生产prefix/override冒充引擎dispatch.
4. 已选Forms局真实Crescendo进入Wrath且marker/carrier/effects完整, 全fixture在失效前一次完成. production Shutdown后分别真实PowerCmd.Remove当前Wrath marker, CreatureCmd.Damage玩家且enemy dealer, PowerCmd.ModifyAmount owned Strength. 每条必须settled且明确Forms unavailable+restart Exception, 否则fail. 同步提交在主线程内明确结束并抛异常可称settled synchronous rejection; Timeout/pending不能叫settled. 未settled立刻终止后续probe, 不再读伪after/复用污染combat. 每步完整严格HP/block/energy/full-name power amounts/raw marker before/after不变, 无值/未知不能0=0通过. 全Task原始异常保留, expected只有明确重启且所有叶子批准的真实command root, 无关sibling或same text different ref仍unexpected.
5. 不选Forms控制局RunState.Modifiers确实没有FormStanceModifier, carrier/effects为空且rawWrath成立. Strike与Tranquility真实CardModel实例和足够能量在Shutdown前预创建; 失效后仅enqueue已备实例, 禁重新Create/Add卡/补能. 两张真实action完成且按同一CardModel reference的Started/Finished历史确有增量, 支付与原生damage/marker变化完整参与passed. 从当前实际dynamic vars/Strength/props和权威Watcher原生倍率算expected, 或证明无额外power/modifiers前提后计算, 不写死未经核对值. 调用原helper若会失效后CreateCard, 必须在本新增文件写独立prepared-card执行窄方法, 不改r25 helper.
6. drain所有command和submission操作, 任一未settled则不后续command/cleanup; cleanup异常或跳过原因严格失败, cleanup产生的操作二次drain. fault订阅保留到Quit/post-quit最终排空. 初/终passed,status,exitCode统一fail closed, final原始fault+expected+unexpected保留, 不能late失败被之前bool覆盖. 任何报告缺环境/非法路径/写失败必须非零退出, 不仅旁路reportWriteFailure. 场景成功只允许全部必需step实际执行通过且身份/guard/原始证据齐全. 退出前可先验证报告可写, post-quit写失败也记录failed并尽最大可实现方式非零退出, flag不可达边界.
7. 未请求开关必须零动作. --forms-runtime-safety与--forms-runtime-unselected独立进程且不与原smoke冲突. shouldSave=false/固定seed/真实Cubex encounter保留, 不mock引擎scheduler. 只SPIRE1_FORM_SMOKE_REPORT在G:目录写两报告. 新代码/文档最终hash与中文未验收边界, 不宣称UI/读档/长战斗/多人/性能/热卸载.
同批监督由主hub同步派发. CODE_COMPLETE列准确hash/路径与未验证边界, 无法全做则PARTIAL并停写, 不等待peer.

