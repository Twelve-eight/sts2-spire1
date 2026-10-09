你是 实现者, 范围: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs and FormStanceBridgePump.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api; reasoning xhigh; native Codex; no fallback`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\combat-pump-recovery-r30\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 实现写集覆盖条款
除唯一报告外, 仅可改 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs 与同目录 FormStanceBridgePump.cs. 不改其它源/测试/script/资源/构建/配置/Git. 如须新增明确必要的小类, 先报告建议给hub, 不自行扩scope. WRITE code only, SKIP builds/lint/tests/games. 不委派, peer或其它harness/模型. 最多8分钟完成一个小修复; 第一条结论即刻落盘, 每一面写一次.

## BASELINE_CONFIRMED: 新字节真实terminal失败
生产r18DLL 15960D604D95E19502AFF9F12D18259A9EE6DEF287CFC70E67F1EA058B129BF9, 新r27载体已中央编译与监督通过. 真实运行窗口2026-10-05 12:32:27至12:33:20 +08:00, 进程20688无窗口, 日志排空, 共享配置不变. 证据 G:\omp works\.tmp\forms-independent-20261005\native-r18-terminal-r27\b1-terminal\result.json 与 forms-binding-loss-binding-loss-terminal.json.
实际成功已选Forms真实Crescendo入Wrath后, 仅Assembly.Load重复Watcher, 生产桥40秒仍pending而不是Terminal. 不可用理由 Watcher assembly load pending; restart the game process if identity verification fails. bridgeRemoved=false, 未执行两个probe, safetyPassed=false. 还出现NEndTurnButton.HasPlayableCard->ShouldPlay的未关联真实Forms unavailable异常. 不称安全通过.

## 任务与保持条件
1. 先读当前pump调用/注册/创建与bind入口, 使用源码/引擎权威资料定位为何生产消费者未持续消费. FormStanceBridgePump目前_Ready内设ProcessMode.Always, _Process调PumpTick, root.AddChild创建; 不能假定这已运行. Godot sourcegenerator/动态程序集脚本注册与节点存在/processing差异须考虑. 不把尚未证明的因果当confirmed. 不运行游戏, 如需要runtime对象取证请立即列具体字段给hub.
2. 修生产消费者使战中及paused SceneTree仍可靠消费AssemblyLoad pending, 并在同名多Watcher实例时收敛Terminal/restart. 保持main-thread only, AssemblyLoad只发pending/epoch; 无worker Godot/Harmony调用. 正常Bound每帧O(1), 只epoch/pending变时扫一次身份; Retryable有界预算. 不强行解除暂停或改其它node ProcessMode, 不直接把所有pending当Terminal导致良性同identity晚载误拒.
3. 可靠消费者不依赖测试主动TryBind/PumpTick调用, 不通过测试或delay掩盖production失效. 重复init/菜单/绑定不会叠consumer. Shutdown/Terminal cleanup不能off-main Godot, 托管stop状态与主线程unsubscribe释放边界必须清楚. 不保留dead node/scaffolds.
4. 若初步发现实际consumer注册/周期未能证明, 选稳定引擎信号或等价main-thread入口补可靠消费, 只用准确API/本机权威DLL或docs, 不造API. 日志/诊断只一次或状态变化不做每帧大日志. 连续游戏实机归hub.
5. 不为吞UI异常而放宽选中局ShouldPlay保护. 额外UI异常在pending/Terminal是否代表独立交互问题请flag, 本窄修重点真实state收敛. 不扩大Spire1硬前置或Watcher AssemblyRef.
6. 报告准确hash/改动行号/原因证据等级/尚未知, FLAG不能完成, 不留stub. 最终CODE_COMPLETE并列全部路径. 本次新生产字节将重做结构/三形态/Shutdown/Terminal/交叉启动.