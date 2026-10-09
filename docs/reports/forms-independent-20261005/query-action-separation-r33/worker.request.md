用户本轮唯一指定模型global:deepseek-v4.1-flash, 路由wb2api, reasoning xhigh, 原生Codex, no fallback. 禁止再委派, peer工具, 其它harness/model, close/resume, 配置/Steam/共享mod_configs/C:/发布脚本/Workshop/Git/游戏操作. 不构建/lint/test, hub集中验证. PowerShell必须显式shell="powershell.exe",login=false.
增量报告是交付面: 首条可用结论立即落盘, 每个检查面立即追加, 固定已确认/进行中/未知三段; 包含准确绝对路径与行号, 触发条件, 权威契约, 当前控制流, 尚缺实机. 不攒到最后一次性输出. 报告仅按可写白名单. 此规范来自G:\omp works\.tooling\subagent-report-protocol.md, 必须先读.
你是r33实现者. 唯一代码可写:
G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs
G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs
G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs
G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs
唯一报告G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\query-action-separation-r33\worker.md. 本代码白名单覆盖模板只读禁写代码项, 不改其它源/测试/文档/配置. 直接编辑文件, WRITE code only, SKIP build/lint/test.
已复现生产r30 Terminal真实失败, 证据G:\omp works\.tmp\forms-independent-20261005\native-r30-terminal-r27\b1-terminal\result.json及forms-binding-loss-binding-loss-terminal.json. Pump已收敛到身份变化, 不改pump. 原ShouldPlay在查询中抛出未关联UI faults, 不能按消息吞掉或取消真实动作冒充安全拒绝.
先读DEVELOP最新契约, 本机权威G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs核对ExecuteAction确切签名及其UpdateCardBeforeExecution/CanPlay/SpendResources次序. 如没有源或不能证明, flag INCOMPLETE不造API. 只做:
1. 提取复用无throw的已选当前live失效predicate到FormStanceMode, 保留原ThrowIfSelectedFormsCombatUnavailable行为给真实command/hook. 非当前/结束/正在结束/teardown及非Forms/Bound不拦.
2. FormStanceModifier.ShouldPlay查询只返回false表达不可玩, 不抛异常. 真动作保护必须另装在PlayCardAction.ExecuteAction原方法入口, 早于UI刷新/支付/出牌历史/效果, 不是BeforeCardPlayed. 真实动作仍明确抛Forms unavailable与restart原因, 不是cancel或pending通过.
3. 在现有FormStanceSafetyGuard显式驻留owner Forms.FormStanceSafety下新增PlayPrefix, 保留原PowerCmd.Remove prefix. EnsureInstalled/IsInstalled精确证明两个target/prefix/owner各1. 安装/证明失败不能发布Bound; 重复Initialize不叠; Shutdown/Terminal仍留双guard, 不在普通扫描重复注册. 原Remove前缀身份与语义不变. MainFile必要改注释/扫描或健康证明, 不扩其它owner/入口. 只稳定引擎/项目类型, 无Watcher或Spire1硬引用, 无新增工作线程Godot访问/缓存成本.
4. 核实两patch安装原子性/回滚, 正常Bound与无Forms牌不受影响. 生产新guard全球owner数将为2, 单target各1. 同批r34测试会按此契约独立补证据, 不改测试文件.
每一面落盘. 最多8分钟冻结CODE_COMPLETE或明确INCOMPLETE, 列全部修改文件与hash, 不留stub. 同批监督由hub真实wait后通知审查.