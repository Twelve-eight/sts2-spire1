你是 监督审查员, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api; reasoning xhigh; native Codex; no fallback`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-finalization-r29\supervisor.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 同批门禁
现在只读请求, 写WAITING_GATE与已确认/进行中/未知后结束, 不读在写源码, 不提前审查, 不使用peer/list/read/wait_threads, 不再委派. hub真实native wait本批worker completed后写本目录gate-notice.json并send_input通知才审冻结实现. 不构建/lint/测试/代码/Git/游戏/其它harness/模型. 指定global:deepseek-v4.1-flash/wb2api/xhigh.

## 门禁后审核
仅按本目录worker.request.md做窄静态审核: pre-quit必先写phase, full raw fault从订阅到final不被后半段覆盖, expected root真实对象且来自settledcommand, 引用单root完整leaf归属由共享r27helper承担, latefault/quitdrain/writefailed统一status/passed/exitCode且必要非零真实退出, 正常路径无Environment.Exit. 精确冻结hash, 不变的3command/2actionfixture与schema, README诚实边界. 最多5分钟, 每一面先落盘. 有P1 NEEDS_REWORK; 静态完整通过SUPERVISION_PASS不代表实机.