# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-safety-probes-r21\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 代码写集
报告如上, 只新增 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs 与 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md . 不改原有源文件/README/生产/控制器. 不构建/lint/测试/运行/game/deploy/git/再委派/其它harness/模型/fallback. reasoning xhigh. 同批监督必须hub wait completed后才审. 最多10分钟, 单面完成就立刻报告. 若无法准确调用API则flag而非stub.

## 原生入口与范围
同程序集test-only新文件可提供另一个Harmony NGame._Ready postfix (原MainFile扫描所有patch类), args明确 opt-in --forms-runtime-safety 与 --forms-runtime-unselected; 两者各独立进程, 不能同时请求或与已有smoke参数重叠. 未传参无动作. 新文件可作为internal static partial FormNativeSmokeRunner复用同类已有private启动/主线程调度/超时/排空/helpers, 但不得改原有文件. 当前BaseLib/Forms引用已存在, 不引Spire1/Watcher AssemblyRef, 不直接调用生产guard方法或prefix冒充安装/拦截.
等NGame.GameStartupComplete实际Task完成, ModelDb/Watcher/Forms真Bound, 使用真实Watcher character/固定种子和当前Cubex encounter; 复制现有已验证setup必要语义, 不创建mock/scheduler假对象或直接调用prefix. shouldSave=false, APPDATA/GSE由hub隔离. 所有Godot访问在已知主线程, async commands真实await和有界drain; 不抢前台或自己启动游戏.

## 场景A: --forms-runtime-safety 已选Forms局的核心防线
真实本局FormStanceModifier, 使用真实Crescendo进入Wrath并确认marker/carrier/effects. 所有card/能量fixture在失效前准备, 失效后不补能或改状态. 记录PowerCmd.Remove(PowerModel?)真实Harmony元数据owner=Forms.FormStanceSafety且精确prefix数量1. 调MainFile.Shutdown使桥失效, 记录两个旧owner0与安全owner仍1.
随后分别真实调用并await以下引擎路径, 每步有原始before/after HP/block/energy/full-name-power amounts/marker证据和真实exception:
- PowerCmd.Remove当前Watcher Wrath marker, 拦截应发生在RemoveInternal之前, marker不得消失.
- CreatureCmd.Damage (目标玩家, dealer真实敌人, 正值无随机), 必须在HP/block落地前明确拒绝.
- PowerCmd.Apply或ModifyAmount的真实数值变更 (取当前owned Strength等现有真实PowerModel), 必须在amount mutation前拒绝.
必要API签名/value props只查当前权威engine-dllsrc, 不猜或发明枚举数值. 不靠调用FormStanceModifier override直接证明engine dispatch. 每条命令均须明确Forms unavailable+restart异常, 非timeout/pending/cancelled, 全任务settled且没有无关fault. 保留预期异常raw证据. 若实现额外real end-turn/action覆盖导致复杂性大, 不扩范围, 标未知.

## 场景B: --forms-runtime-unselected 普通局控制
同样挂Forms但RunState.Modifiers没有FormStanceModifier, 先真实Crescendo入Wrath且Forms carrier/effect集合必须为空. 失效前一次fixture能量足够各probe, 之后生产MainFile.Shutdown. 真实Watcher Strike与Tranquility仍可正常完成且产生原生伤害/支付/marker变化, 无Forms不可用拒绝, 无异步fault; 实际CardPlay history和原始marker证明, 不把action CompletionTask当真实出牌. 验证稳定guard owner1仍未误伤非Forms局. 不硬编码未经权威核对的伤害数值; 记录actual与从当前真实卡牌/力量/原生倍率的权威expected计算.

## 输出/退出
只写SPIRE1_FORM_SMOKE_REPORT指定G:目录, 之外拒绝. 每模式一份 forms-runtime-safety.json 或 forms-runtime-unselected.json, 包含testOnly=true, scenario, status, passed, started/completedUtc, productionAssemblySHA256/location, guard metadata, steps(check名/实际命令/原始异常/before-after/changed/passed), raw/expected/unexpected faults, cleanup与drain. 成功只允许所有步骤执行通过且原始证据完整; 超时/无报告/未执行/不明证据必须失败. 最后主线程Quit exit0通过/1失败并记录post-quit drain settled; 不能跑到未settled时清空状态/宣称通过. 任一command未settled不能后续复用被污染战斗, cleanup如实标跳过. 出错不得吞掉或改变生产语义来变绿.
报告最终CODE_COMPLETE/hash/尚未实机边界. 不宣称UI/长战斗/读档/多人/真热替换/性能通过.