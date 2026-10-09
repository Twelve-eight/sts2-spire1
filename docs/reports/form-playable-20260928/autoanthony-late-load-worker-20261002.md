# AutoAnthony late load worker report - 2026-10-02

## 已确认

- 首条源码证据已先于编辑落盘: 编辑前 `AutoAnthonyCompatBridge.Apply` 在第 201-204 行使用单一 `_applied` 短路,第 224-227 行把 From, pool/deck, third-party 三组结果合并为一个状态;因此 core 已成功时,后续 Watcher 或 AutoAnthonyWatcher 到达无法再次进入第三方 capability 评估.
- 首条时序证据已先于编辑落盘: 编辑前 `AutoAnthonyLoadHook.OnAssemblyLoad` 仅筛选程序集名 `AutoAnthony`,并在 `Apply` 返回 true 后摘除事件;它没有监听 `Watcher` 或 `AutoAnthonyWatcher`.
- 本轮只修改请求白名单内的两个产品文件,并修改本报告;未构建,未测试,未部署,未启动游戏.
- `AutoAnthonyCompatBridge.Apply` 已去除整体 `_applied` 短路,改为独立跟踪 `From overloads` 与 `pool/deck getters` 两个 core group;晚加载重试时只处理尚未成功的 core group.
- Watcher capability 已独立为 `Pending`, `LegacyBridge`, `OfficialAddon`, `Unsupported` 四态. Watcher 缺席保持 `Pending`,因此 `AutoAnthony` 先到时不会把可选能力永久锁死.
- 可选 Watcher patch 采用全套计数和本地已安装方法列表;数量不完整时尝试按当前 Harmony ID 回滚并禁用该能力,不把异常抛回 Spire1 initializer.
- 检测到 `AutoAnthonyWatcher` 时优先于旧 Watcher 路径,清理本桥保存的旧 Watcher patch 与映射,将 capability 标记为 `OfficialAddon`.
- `AutoAnthonyLoadHook` 现在监听 `AutoAnthony`, `Watcher`, `AutoAnthonyWatcher` 三个程序集名;成功或 capability settled 后通过统一的 `UnhookAssemblyLoad` 摘除.

## 进行中

- 增量复核已确认监督审查的五个阻断边界仍存在: core 部分成功被错误锁存, Watcher 回滚结果被忽略, WatcherCardPool 缺失仍被接受, optional 外层异常未统一回滚,以及官方 addon 接管失败后没有可靠重试状态.
- 当前修复只允许写入两个 Interop 产品文件和本报告;本轮继续不构建,不测试,不部署,不启动游戏.
- 检查面 1 已完成: core `From overloads` 固定期望 3 个成员, `pool/deck getters` 固定期望 6 个成员;只有完整计数才置完成. 部分安装或异常会记录已安装方法,先按 Harmony ID 回滚;回滚未完成时保留 pending 列表,后续重试先清理旧 patch,避免重复安装.
- 检查面 2 已完成: Watcher capability 现在要求 `Watcher.CardPool`, `WatcherCardPool.AllCards`, `WatcherCardPool.AllCardIds` 三个成员完整存在并全部安装;缺少池类型直接禁用,局部安装失败检查回滚结果,回滚失败保持 `Pending` 与可重试 partial 列表,不会返回 settled.
- 检查面 3 已完成: `PatchThirdPartyEntries` 自带局部安装捕获和回滚;异常后清理映射与池缓存,回滚失败保留待清理方法,后续先清理再重试,避免半安装状态和重复 Harmony patch.
- 检查面 4 已完成: `AutoAnthonyWatcher` 接管失败现在进入 `OfficialAddonPending`,保留旧 patch 方法清单并保持 `Apply=false`;加载监听不会摘除,后续目标程序集事件可再次尝试完整卸载.
- 检查面 5 已完成: legacy Watcher 三件套安装成功后不直接报告 settled,而是保留 AssemblyLoad 监听,以覆盖官方 addon 晚于旧桥加载的顺序;只有官方接管成功或明确 Unsupported/OfficialAddon 才允许摘除.

## 未知

- 未构建确认当前工作树中的 C# 编译结果,未确认运行期 Harmony patch 数量,AssemblyLoad 顺序,Watcher pool 注册时序或官方 addon 的实际卸载效果.
- 尚未把本轮修改提交或推送;工作树含有其他会话的既有未提交变更,不能整体暂存.

- 最终静态复读发现一个仍需在本轮闭合的精确点：`PatchThirdPartyEntriesCore` 的 `Unsupported` 分支仍返回 `true`（当前源码第 534 行），会令 `Apply` 被加载钩子视为 settled 并摘除事件；这与“官方 addon 可晚加载”的 fail-closed 要求不一致。按本轮既定范围仅修正该返回值，不扩展检查面。
