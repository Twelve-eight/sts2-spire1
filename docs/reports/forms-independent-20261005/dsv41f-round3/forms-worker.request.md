你是 Forms 实现返工者. 唯一产品写集 G:\omp works\Sts\sts2-forms, 唯一报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round3\forms-worker.md. 原实现已存在, 不重复创建项目, 不改 Spire1. 本批有独立监督, 由主会话实际 wait completed 后激活.
先核对当前源码和 Forms 本地 DEVELOP.md, 查清以下具体疑点再最小修复. 主会话实际诊断构建日志 G:\omp works\.tmp\forms-independent-20261005\forms-build-r1.log 已复现 MainFile.cs:24 的 Logger 歧义.
1. 修编译阻断: MainFile Logger 明确 using alias / full name, FormStanceWatcherBridge 的 AssemblyLoad handler 使用真实 AssemblyLoadEventHandler 签名.
2. 删除 Forms.csproj 的自动复制目标, 或默认 false 且最终绝对路径 fail-closed 身份门禁. 本轮无自动部署需求, 优先删除自动复制目标并在文档说明由隔离打包部署. 不留下普通 build 可写 Steam 的路径.
3. AssemblyLoad 只通知不触 Godot/Harmony, 但必须有持续主线程消费者: 当前只有 ModelDb.Init / NMainMenu._Ready 一次性入口, 菜单已 ready 后加载 Watcher 不能无限等待. 用有明确生命周期和退出控制的 Godot 主线程帧 pump 或经核对的已有调度入口. 无每帧 AppDomain 全扫描, 无工作线程 Godot, 无重复 pump, Retryable 才重试, 等 ModelDb 原生 marker 真正可用. 必须在后续时序重试, 不只靠一次 ready.
4. Shutdown 在撤 patch 前先发布不可运行状态, 所有 native patch callbacks / async transpiler helpers / await 后续都统一非抛异常关闭门禁. 正常 Bound 且非 Forms 对局仍保留原生行为, 不能用 !IsAvailable 简单吞掉正常原生资源和通知. Shutdown / Terminal / 回滚时不得继续调已清空 native delegate, 禁止旧 await 续体污染新生命周期. 清空 pending/native markers/兼容通知缓存/泵.
5. Watcher 真正替换或同 simple name 多程序集时, 每次发布或继续使用已有 binding 前核对身份/代数. 已绑定的早返回不能绕过 AssemblyLoad pending 和身份检测. 可安全 fail closed 并要求完整重启, 不能宣称真实热卸载或旧档已验收. 正常重复 ModelDb.Init / Initialize 保持幂等, 不叠 patch.
6. MainFile 安装自身 Harmony patch 必须全成功才可用; 部分失败应回滚本 owner, 禁止 _patchesInstalled=true 和仍发布桥 Bound. 显式 shutdown 要涵盖 MainFile 自身 patches 和 ProcessExit/AssemblyLoad 订阅. 反射通知 Spire1 缓存也要检测身份替换, 不保留旧静态 MethodInfo.
7. 完整保留10个CustomID/六效果的既定语义/公开FormsRuntimeEntryPoint签名. 本轮只修生命周期与构建, 不重新设计数值. 无 smoke runner 进入生产. 更新 Forms DEVELOP/DEVLOG, 明确动态程序集替换的保证范围和未知.
旧审查线索路径 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\forms-supervisor.md, 必须自行从实际源码核实, 不复制其通过声明.
用户本轮唯一指定 global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh. 禁止再委派, 禁止任何其它模型或 fallback, 禁止启动 codex exec / omp / 外部 agent harness. 不写 C:, 不构建 lint 测试部署, 不运行游戏, 不改 Steam / 共享 mod_configs, 不操作 canonical Release / Workshop staging, 不执行 git add -A/reset/clean. 主会话集中构建验证. 不改其它会话的发布脚本.
增量落盘: 第一条可用结论立即写唯一报告, 每检查或完成一面再次追加. 按 ## 已确认 / ## 进行中 / ## 未知 分段. 代码完成记录 CODE_COMPLETE 和实际修改文件列表, 未验证面明确标识. 只允许中文 English French German Russian 和 ASCII 标点.
旧 round2 两个 supervisor 的元数据在恢复后曾解析为未授权的 ovoapi:6.1sol. 旧审查不作为本轮验收证据, 只能作待重新核实的线索. 两个旧 worker 的元数据确认一直是指定 DeepSeek. 所有新产物必须经过本批新监督.
契约: G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md.
## 模板原件
# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round3\forms-worker.md` (产品代码仅可写上述白名单; 报告只写此文件; 不构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 10 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

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