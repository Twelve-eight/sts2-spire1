# AutoAnthony inherited getter 修复监督审查报告

审查时间: 2026-10-01
审查范围: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`
实现报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-getter-worker-20261001.md`
门禁状态: 主会话已提供实现者 `01a0f50b-09d0-76e3-a125-5b6726d552da` 的真实完成状态 `completed`, `timed_out=false`. 本轮不等待, 不再委派, 不构建, 不测试, 不改产品代码.

## 已确认

### 监督门禁已满足

- 已直接读取实现报告和最终源码.
- 实现报告声明唯一产品改动为 `AutoAnthonyCompatBridge.cs`, 未构建, 未测试, 未部署.
- 本审查只写本报告文件, 不把源码审查当作实机通过证据.

### getter 身份修复的源码控制流成立

- 证据: `AutoAnthonyCompatBridge.cs:625-630` 先用 `AccessTools.PropertyGetter(type, propertyName)` 沿继承链解析, 再取返回方法的 `DeclaringType`, 最后用 `AccessTools.DeclaredPropertyGetter(declaringType, propertyName)` 重取得声明类型上的 getter.
- 契约: `G:\omp works\Sts\sts2-spire1\.nuget\packages\lib.harmony\2.4.2\lib\net10.0\0Harmony.xml:1730-1734` 将 `DeclaredPropertyGetter` 定义为直接声明属性 getter, `:1781-1785` 将 `PropertyGetter` 定义为搜索继承链.
- 结论: 当前代码没有把继承的 `WatcherCardPool.AllCards` 或 `AllCardIds` 继续以反射类型身份直接交给 Harmony, 也没有使用 `GetBaseDefinition()` 把 override 强行改成最远祖先. 该声明身份修复在源码层面成立.
- 触发条件: `PatchThirdPartyEntries` 对 `WatcherMod.WatcherCardPool` 请求 `AllCards` 或 `AllCardIds`.
- 尚缺实机证据: 运行时 Harmony 接受的最终 `MethodBase` 身份和启动日志中的 `patch ... failed` 消失尚未验证.

### AllCardIds 已从 Prefix 改为 Postfix, 注册方向正确

- 证据: `AutoAnthonyCompatBridge.cs:374-375` 使用 `postfix: new HarmonyMethod(..., nameof(ThirdPartyPoolIdsPostfix))`.
- 证据: `AutoAnthonyCompatBridge.cs:620-621` 将 `PatchGetter` 的可选参数分开为 `prefix` 和 `postfix`, `:642` 以命名参数传入 `harmony.Patch(getter, prefix: prefix, postfix: postfix)`.
- 结论: `ThirdPartyPoolIdsPostfix` 不再被当作原 getter 执行前的 Prefix. `AllCards` 仍注册 `ThirdPartyPoolContentsPrefix` 为 Prefix, 与其通过 `ref __result` 短路原 getter 的语义一致.
- 最小修复范围已满足: 只改注册方向和通用 helper, 没有改 ID 合并正文 `:454-469`.

### Watcher 实例守卫存在且覆盖两个基类 getter 补丁

- 权威源码: `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherCardPool.cs:6` 声明 `WatcherCardPool : CardPoolModel`, 未声明 `AllCards` 或 `AllCardIds`.
- 权威源码: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardPoolModel.cs:44-60` 声明两个目标属性在 `CardPoolModel`, 其中 `AllCards` 为 virtual, `AllCardIds` 为非 virtual.
- 证据: `AutoAnthonyCompatBridge.cs:428-433` 和 `:454-458` 均先执行 `ReferenceEquals(__instance, ResolveThirdPartyPoolInstance())`, 非 canonical Watcher 池立即放行或返回.
- 结论: 将补丁落在 `CardPoolModel` getter 后, 当前两个补丁正文不会直接把普通角色池和共享池改成 Watcher 混沌内容. `Watcher` 自身的 `CardPool` 另有 override, 见 `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Watcher.cs:34`, 不会因本次 getter rebind 被误扩到 `CharacterModel.CardPool`.
- 尚缺实机证据: 需要在真实程序集内枚举 Harmony patch 元数据, 并访问至少一个非 Watcher 池和 Watcher 池确认分支结果.

### 当前静态检查未发现本次改动的明显编译签名错误

- 证据: `HarmonyMethod?` 可选参数和 `postfix:` 命名参数与现有 `Harmony.Patch` API 文档一致, 见 `G:\omp works\Sts\sts2-spire1\.nuget\packages\lib.harmony\2.4.2\lib\net10.0\0Harmony.xml:893-900`.
- 证据: `AccessTools.DeclaredPropertyGetter` 在本地 Harmony 2.4.2 API 文档存在, 见 `:1730-1734`.
- 证据: 所有新增类型均为当前文件已有的 `MethodInfo`, `HarmonyMethod`, `CardPoolModel` 和已有 nullable 语法, 未增加 AutoAnthony 或 Watcher 的编译期引用. `Spire1.csproj:32-38` 仍明确无 AutoAnthony compile-time reference.
- 结论: 仅按源码和本地 API 文档审查, 没有看到明显的 CS 编译风险. 这不是构建通过结论.

### 重复 Apply 和可选依赖缺席路径未被本次差异扩大

- 证据: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:199-229` 在 `_applied` 为 true 时直接返回, 成功安装任何补丁后将 `_applied` 设为 true. 按现有串行初始化路径, 下一次调用不会重复安装已成功的 getter 补丁; 本次 helper 修改没有改变锁存控制流.
- 证据: 同一源码 `:206-217` 在 AutoAnthony 缺席或反射解析失败时, 先于三个 patch group 返回 false. `:336-347` 在初次调用时遇到 AutoAnthonyWatcher 扩展或 Watcher 程序集缺席, 跳过第三方补丁. 本次修改没有新增第三方强类型引用.
- 检查边界: `_applied` 不是互斥锁, 不据此声称并发 Apply 安全. 部分成功后的失败组不再重试, 以及 Watcher 或其扩展晚于桥接加载时的行为, 属于原有初始化设计边界, 不是本次两处差异新引入的缺陷. 此处不要求扩大修复范围.
- 当前审查快照 SHA256: `B2C1E115D7D00BD369242B976D26D2B470BAE90122B6E5E5DF1ACABCD8F2DFA5`, 与 worker 报告声明一致. 已执行 `Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs' -Algorithm SHA256` 做文件身份核对, 未执行构建或测试.
## 进行中

### [P1/P2 候选] CardPoolModel 的全局 getter 补丁存在缓存污染风险, 代码机制已确认但运行触发未知

- 证据: `CardPoolModel.cs:60` 的 `AllCardIds` 首次访问会把 `AllCards.Select(...).ToHashSet()` 写入私有 `_allCardIds`, 后续直接复用; `:78-85` 说明只有 `InvalidateCardCache` 才会清理该缓存.
- 证据: `AutoAnthonyCompatBridge.cs:428-449` 在活跃混沌局对 canonical Watcher 实例短路 `AllCards` 原 getter, 返回 `ColorlessTypes()` 产生的混沌列表, 但不写 `CardPoolModel._allCards`.
- 证据: `AutoAnthonyCompatBridge.cs:454-469` 的 `AllCardIds` Postfix 只改当前 `__result`, 不清理或更新 `_allCardIds`.
- 触发条件: canonical Watcher 池在混沌局第一次访问 `AllCardIds`, 且此前没有让原生 `AllCardIds` 缓存建立. 这时基类 getter 会基于 Prefix 返回的混沌 `AllCards` 建立只含混沌 ID 的 `_allCardIds`; Postfix 的 `__result` 并集无法恢复未出现在基类结果中的 83 个原生 Watcher ID.
- 反向影响: 混沌局结束后, `AllCardIds` 仍可能返回活跃期间写入的缓存, 而非 Watcher 原生 ID 集. `CardModel.Pool` 在 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:298-316` 通过各池 `AllCardIds.Contains` 反查, 因此可能出现原生 Watcher 卡池归属丢失或归属错误.
- 最终等级: 这是由权威源码控制流推出的条件性 P1/P2 候选, 不是本轮实机复现. 代码级缓存污染机制已确认, 但游戏实际首次访问顺序和 AutoAnthony 是否在 run 前触发 Watcher `AllCardIds` 仍未知.
- 最小修复方向: 不应直接把源码缓存字段当作可写契约. 应在设计文档中明确 run 边界缓存策略, 再选择不会污染 canonical `AllCardIds` 的实现. 本轮不改代码, 不把该候选升级为已实机复现.

### 缓存候选的增量排除与影响范围修正

- 补充源码证据: `G:\omp works\.tmp\aa-decompile\src\AutoAnthony\ChaosRunDefinitions.cs:1666-1679` 的 `ResetCanonicalCardCaches` 只重置生成卡的费用, 动态变量, 关键词和标签, 不重置 `CardPoolModel._allCardIds`; `:1545-1560` 的 `DeactivateRun` 也没有恢复 Watcher 池缓存. 另一份本地转储 `G:\omp works\Sts\sts2-spire1\.tmp\autoanthony\AutoAnthony\ChaosRunDefinitions.cs:1506-1519` 与 `:1396-1409` 在这两点上相同. 两份都是源码转储证据, 还没有核验与本轮安装 DLL 完全同源.
- 初始化边界: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\ModelDb.cs:484-497` 会预加载所有卡并访问 `Pool`, 但没有直接预热每个池的 `AllCardIds`. `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherCard.cs:44` 自行 override `Pool`, 所以不能从预加载 Watcher 卡反推出 Watcher ID 缓存必然已经建立.
- 对上一条候选的影响修正: 不把原生 Watcher 卡必然在 `CardModel.Pool` 抛错当作发现, 因为它们有自己的 `Pool` override. 已定位到确实读取 Watcher `AllCardIds` 的消费者 `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherCardLibraryInjector.cs:80-82`. 该证据只支持保留条件性缓存风险, 不支持泛化的全局崩溃结论.
### 本轮收尾

- 无正在执行的审查动作. 按用户指示从已落盘最终产物收尾, 不再扩展, 不构建, 不测试, 不改产品代码.
- 最终已确认问题: 原继承 getter 目标身份错误已由 `DeclaringType` 重绑定路径修正; `AllCardIds` 已按 Postfix 注册; 两个基类 getter 的 `__instance` 守卫仍在; 当前静态审查未见本次差异的明显编译签名错误.
- 最终保留的风险: 活跃混沌局首次访问 `AllCardIds` 可能污染 `CardPoolModel._allCardIds`;这是条件性源码风险, 运行触发和实际消费者影响未知.
## 未知

- 未构建, 未运行 probe, 未启动游戏, 未读取新启动日志. 因此无法确认 `WatcherCardPool.AllCards` 和 `AllCardIds` 的 Harmony patch 已在真实程序集接受.
- 未确认运行时 `ResolveThirdPartyPoolInstance()` 返回的对象是否始终等于 `ModelDb.CardPool<WatcherCardPool>()` canonical 实例, 也未覆盖 ModelDb 初始化前后首次访问顺序.
- 未确认其它 mod 对 `CardPoolModel.get_AllCards` 或 `get_AllCardIds` 的 Harmony patch 顺序. 当前源码守卫只保证本补丁正文的实例范围, 不证明组合 patch 后的最终顺序.
- 未确认 AutoAnthony 缺席, AutoAnthonyWatcher 在场, Watcher 缺席以及非混沌局四种组合的日志和消费者行为.
- 可复现但未执行的源码核对命令:
  `rg -n "ThirdPartyPoolIdsPostfix|DeclaredPropertyGetter|ReferenceEquals|_allCardIds|AllCardIds" "G:\\omp works\\Sts\\sts2-spire1\\mod\\Spire1Code\\Interop\\AutoAnthonyCompatBridge.cs" "G:\\omp works\\Sts\\sts2-spire1\\research\\engine-dllsrc\\MegaCrit.Sts2.Core.Models\\CardPoolModel.cs"`
- 本报告结论是只读源码审查, 不能标记 AutoAnthony getter 修复已实机通过.
- 最终审查状态: NEEDS_REVIEW. 审查本身已 completed, 但运行时 Harmony 接受, 缓存首次访问顺序, 非 Watcher 池隔离和新日志消失均未由本轮验证.
