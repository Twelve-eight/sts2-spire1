你是Forms生命周期窄返工实现者. 产品唯一可写 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs, 必要时同目录 FormStanceBridgePump.cs, MainFile.cs. 文档可写 G:\omp works\Sts\sts2-forms\DEVELOP.md 与 DEVLOG.md. 唯一报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-worker.md.
先读 dsv41f-round3\forms-supervisor.md. 当前r4源只修编译, 仍需收敛S-03..S-07. 不重设计六效果/公开interop/10个CustomID.
1. TryBindOnMainThreadEntry Bound早返回必须处理pending/身份, 委托TryBind或一致路径. 绑定成功后不能停止全部AssemblyLoad主线程消费者: 保留O(1)帧pump, Bound时仅通知epoch变化才扫描身份, 不每帧AppDomain全扫描. AssemblyLoad回调只通知且不触Godot/Harmony. 继续调用旧绑定前必须检查代数/epoch; pending发生时不得继续使用旧native delegate.
2. 确定性Terminal安装catch也要退订AssemblyLoad, 清pending/重试预算/native refs, 不无条件EnsureSubscription. 所有Terminal路径统一调用清理, 包含Spire1StanceNotification.Reset. 回滚失败保持Terminal不能重试叠patch. 正常Retryable的延迟ModelDb marker可用应能重新尝试, 不把一次Ready当持续唤醒保证.
3. AfterMarkerRemoved在await original前捕获BindingLease, await后以及每轮PowerCmd.Remove前都确认代数, 关闭即无副作用返回. pending读/写使用显式跨线程同步. 正常Bound非Forms原生语义保留, 不以关闭处理误吞正常原生场景.
4. 核对ProcessExit调用Shutdown时的Godot主线程边界, 关闭状态/取消订阅/清托管引用可跨线程, 但不得从任意退出线程访问Godot节点或安装Harmony. 已知主线程入口才负责节点释放; 无后续帧时无需为销毁节点触发跨线程Godot调用. 本项以本机API/源码为证据, 不凭猜测宣称QueueFree线程安全. 核对ModelDb.Init是否真已知主线程, 如不是则该postfix只通知, pump只由已知Godot主线程入口建立.
5. 更新本地DEVLOG/DEVELOP保证层级, 真正替换同名程序集安全failclosed要求重启, 不宣称支持热替换. 本批配有监督, 完成后由hub真实wait gate激活. 只修上述生命周期小增量.
用户唯一模型 global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh. 原生Codex, 禁止再委派和其它模型/fallback/codex exec/omp. 不写C:, 不构建lint测试部署, 不运行游戏, 不写Steam/共享mod_configs/canonical/Workshop, 不改其它发布会话脚本, 不执行git操作. 主会话集中构建和实机. 第一可用结论立即写唯一报告, 每面追加, 固定 ## 已确认 / ## 进行中 / ## 未知. 完成写CODE_COMPLETE/修改文件与未验证面, 立即结束实现回合. 任务限为下述小写集, 超过10分钟即落盘精确剩余接口供hub拆分. 不做无关重构.
契约 G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md. 前r3编译两错误已由r4修复, 中央Forms r4真实Release0errors/1warning且Forms.dll/PCK已经生成, 保留这些修法. 两侧PE门禁已通过, 不新增硬引用.
# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-worker.md` (产品/文档仅可写上方白名单; 报告只写此文件; 不构建/部署/游戏/共享配置, 不写 C:).

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

---

## 主会话收割方法

```powershell
Get-ChildItem -LiteralPath '<报告目录>' -File | Sort-Object LastWriteTime |
  Select-Object Name,Length,LastWriteTime
```

- 子代理超时/中断/额度耗尽时: 先读报告文件, 把 `已确认` 当作可用证据, `进行中` 当作半成品复核, 不得当作无产出.
- 收到第一批落盘结果后即向用户汇报一次, 后续增量补充.