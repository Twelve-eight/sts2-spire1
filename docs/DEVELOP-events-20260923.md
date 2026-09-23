# Spire1 事件移除边界与默认开关契约, 2026-09-23

## 用户要求和已复现事实

用户报告一代注入事件允许移除永恒卡牌, 要求验证原版身份, 修复移除边界, 将事件注入默认设为关闭.

当前可复现对象为 NoteForYourself. 主会话已读取一代原版 G:\steam\steamapps\common\SlayTheSpire\desktop-1.0.jar, SHA256 CFAD868AC8D65A88E71A0BF096FB09F78811E553EFFE0787C5309A655E081673. javap 确认 com.megacrit.cardcrawl.events.shrines.NoteForYourself 存在, 且选择可移除卡牌时调用 CardGroup.getPurgeableCards. 原版还持久化 NOTE_CARD/NOTE_UPGRADE; 当前移植未实现跨局存储, 只能称为不完整移植, 不宣称完全复刻.

二代权威源码 CardModel.IsRemovable 排除 CardKeyword.Eternal, CardSelectCmd.FromDeckForRemoval 会调用该过滤, FromDeckGeneric 不会自动过滤. 当前事件误用后者. 主会话链接当前事件源码的隔离探针已复现 offeredEternal=True, keptEternal=False, 退出码 1. 选择器和牌堆依赖为按权威契约实现的桩, 不是实机证据.

证据目录: G:\omp works\.tmp\workspace-audit-20260923-01a0cbfd\evidence
- sts1-NoteForYourself.javap.txt
- spire1-removal-before.log
- spire1-removal-probe\RemovalProbe.csproj

## 最小修改与不变量

- NoteForYourself 使用 FromDeckForRemoval, 必要时在删除前再次过滤 IsRemovable, 不能直接检测固定卡牌 id 或复制永恒关键词判定.
- 保留正常交换的收牌/选牌/移除顺序, 不实现未经授权的跨局保存, 不改随机数或事件奖励.
- 复查所有 Events 内移除/变形调用. FountainOfCurseRemoval 已过滤 IsRemovable, BackToBasics 已用专用移除选择器, DrugDealer 已用专用变形选择器, 无证据不改.
- EnableSts1Events 属性默认 false, 保留 ConfigIgnore EventsEnabled 的总开关组合及显式用户值. 不改共享配置文件, 不强制把既有 true 配置重置为 false.
- 更新相关 eng/zhs 设置说明中的默认开启表述, 不改无关翻译, 不宣称完整移植或实机通过.
- 回归测试覆盖永恒不可选/不可删, 普通卡可移除, 默认关闭且显式开启保留. 测试可链接真实事件源码使用隔离依赖, 必须说明边界.

## 交付门禁

实现者只写代码和测试, 不运行构建测试. 监督者同批创建, 由主会话作为原生等待 hub 调用 wait_agent 接收完整产物后再通知监督审查; 子代理工具面没有 wait_agent 时不跨 harness 绕行. 主会话集中运行探针和隔离 Release 构建. 未获得独立游戏运行授权前不部署, 不启动游戏, 不改 Steam/共享配置. 保留该仓所有既有 staged/unstaged 修改, 只提交本轮所有权明确的路径.
