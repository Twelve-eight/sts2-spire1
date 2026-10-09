你是只读审查员, 范围见本请求文件. 用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时, 不得再委派.

## 唯一可写路径

报告文件由本请求文件末尾给出. 只可写这一个报告文件; 不改产品代码, 构建, 部署, 游戏, 共享配置, 不写 C:.

## 增量落盘硬要求

1. 拿到第一条可用结论后立即追加到报告, 然后才做下一步.
2. 每完成一个检查面就追加一次;结论未定也写入已排除可能性和证据.
3. 报告固定三段: `## 已确认`, `## 进行中`, `## 未知`.
4. 最终回复只需摘要并给出报告绝对路径.

每项结论要包含: 优先级, 绝对路径与准确行号, 触发条件, 契约或宣称, 当前控制流, 可复现命令, 最小修复范围, 尚缺实机证据. 不把源码推理称为实机通过.

语言只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引用本地路径和行号.

项目根: `G:\omp works\Sts\sts2-spire1`
当前交接: `G:\omp works\Sts\sts2-spire1\docs\HANDOFF-forms-current-20261005.md`
独立化目标: Forms 不引用 Spire1, Spire1 不硬引用 Forms;缺 Watcher 时 fail-closed;Watcher 晚加载可重试;同进程重加载/退出幂等;不覆盖其他会话 dirty 修改.
审查 Forms 的模型类名/ModelID,Power/Modifier 自动注册,资源路径,本地化键以及 Watcher 运行时类型契约. 目标是拆成独立 mod 后保留存档和联机身份兼容, 同时避免 Spire1 与 Forms 重复注册或类型冲突. 对照 `research\_decomp`, `research\RESEARCH-form-mod-20260926.md`, Forms 源码和资源. 输出必须保留的类型名和 ID, 可迁移的 namespace 变化边界, 以及需要中央静态门禁的检查项. 只读, 不改代码, 不构建.
报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\identity-audit.md`

