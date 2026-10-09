你是 监督审查员, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api; reasoning xhigh; native Codex; no fallback`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\fault-single-root-r27\supervisor.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 同批监督门禁与边界
你与窄修实现者同批派发. 现在只读取本请求, 写WAITING_GATE及已确认/进行中/未知后结束当前轮次. 不读在写源码, 不提前审查. 不使用peer/chat/list/wait_threads. 若子会话缺native wait工具, 由主hub真正wait_agent实现者completed后创建此目录gate-notice.json并通知你; 文件存在或自然语言声明不是门禁. 后续收到真实门禁再审冻结源码. 不构建/测试/代码/Git/游戏/再委派/其它harness/模型. 模型只准global:deepseek-v4.1-flash, wb2api, xhigh.

## 门禁后检查面
唯一审核源 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs. 对照本目录worker.request.md的最小窄修契约与基线隔离反例 G:\omp works\.tmp\forms-independent-20261005\r25-approved-roots-baseline-r27.json. 检查单root完整leaf归属, 无跨root并集, same-action aggregate仍允许, empty/cycle/无关sibling/同文不同引用拒绝, 其余生产与schema/final raw未改. 按门禁hash重算并核对改动边界. 每完成一个面立即写唯一报告, 不攒到末尾. 最多6分钟. 有P1则NEEDS_REWORK, 仅静态完整通过才SUPERVISION_PASS, 不宣称实机.