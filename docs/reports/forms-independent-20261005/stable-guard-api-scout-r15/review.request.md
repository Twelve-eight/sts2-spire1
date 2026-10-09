你是 只读引擎API审查员. 用户只准global:deepseek-v4.1-flash/wb2api/xhigh, 不换模型/路由/fallback, 不再委派, 不运行codex exec/omp. 先读G:\omp works\AGENTS.md和同目录protocol.md.
唯一可写报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\stable-guard-api-scout-r15\review.md. 首条可用结论立即增量落盘, 每面写一次, 分已确认/进行中/未知. 不改任何代码或其它文件, 不构建/lint/测试/游戏/部署/git, 不写C:/Steam/共享配置.
范围: 查本机当前引擎权威源码 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc 及原生Model hooks, 找一个真正存活于Forms Watcher桥Terminal/Shutdown撤patch后的已选Forms局外层fail-closed入口. R10已查出的静默回落源码缺口见 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-loss-audit-r10\review.md, 不重复检查其lease/清理.
最多3项输出, 最多5分钟. 要回答具体确认的签名/绝对路径行号/调用顺序与失败传播, 不是写实现:
1. FormStanceModifier继承的原生hook是否有在任何PlayCardAction支付/出牌副作用之前执行的可用hook, 且在Forms MainFile/Watcher桥全部撤Harmony patch后仍参与已选RunState的原生listener链. 若有给确切virtual签名和调用点, 无则明确无, 不编造BeforeCardPlayed一定早于支付.
2. 同样需要原生敌方回合/自动打牌/非牌伤害与native变姿态的安全边界. 若单modifier hook无法覆盖, 找最少的核心engine稳定Harmony目标及精确签名, 可按已选局查询区别普通非Forms局, 不持有Watcher Assembly/MethodInfo/delegate. 说明相关异常会fault/cancel真实action还是被吞后继续, 尚缺动态证据明确标注.
3. Shutdown完整清理与存活guard矛盾怎样最小化处理: 优先不依赖Harmony的modifier/model listener, 若必须新owner存活则必须有真实非ProcessExit关闭语义并记录process退出策略;不宣称热卸载. 给一套证据充分的最小方向, 或明确未知阻塞. 不把菜单Bound/三姿态首回合已通过当此面通过.
只查这个API盲区, 不扩UI/多人/性能, 不写产品实现. 未实机路径不作已复现.