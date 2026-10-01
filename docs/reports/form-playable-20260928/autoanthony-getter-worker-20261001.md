# AutoAnthony inherited getter 修复实现报告

## 已确认

### [P2] 初始化失败来自继承 getter 的 Harmony 目标身份

- 记录时点: 2026-10-01 10:15:16 +08:00.
- 历史实机证据: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\hidden-smoke-20261001-mods-r1\appdata\SlayTheSpire2\logs\godot.log:650-656` 和 `:684-690`.
- 触发条件: 已加载 Watcher 与 AutoAnthony 时初始化 Spire1, 桥接尝试修补 `WatcherCardPool.AllCards` 和 `WatcherCardPool.AllCardIds`.
- 权威运行契约: 日志中的 Harmony 异常明确要求改为声明在 `MegaCrit.Sts2.Core.Models.CardPoolModel` 上的 `get_AllCards()` 和 `get_AllCardIds()` 实现.
- 已见控制流: 日志堆栈指向 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:630`, 经 `PatchGetter` 从 `PatchThirdPartyEntries` 发起. 这些是旧运行产物的源码行号, 当前源码尚待逐段核对.
- 只读证据命令: `Select-String -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\hidden-smoke-20261001-mods-r1\appdata\SlayTheSpire2\logs\godot.log' -Pattern 'patch WatcherCardPool.AllCards failed','patch WatcherCardPool.AllCardIds failed'`.
- 最小修复范围: 仅 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`, 解析可补丁的声明实现并保留 Watcher 实例范围守卫. 不能把所有 `CardPoolModel` 都变成混沌池.
- 尚缺的实机证据: 修复后重新构建和隐藏启动日志, Watcher 与非 Watcher 卡池消费者结果. 本实现者不执行构建或测试.
- 接手时产品文件相对 Git 无既有差异. 工作树其它未提交改动全部保留, 不暂存或提交.

### [P2] 实现前源码与日志目标一致, 两个实例守卫已存在

- 记录时点: 2026-10-01 10:17:02 +08:00.
- 实现前源码: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:361-375` 从 Watcher 类型查找两个继承属性; `:620-631` 直接把 `AccessTools.PropertyGetter(type, propertyName)` 的结果交给 Harmony, 没有按声明类型重新取得实现.
- 引擎契约: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\CardPoolModel.cs:44-55` 声明返回 `IEnumerable<CardModel>` 的无参实例虚 getter; `:60` 声明返回 `IEnumerable<ModelId>` 的无参实例非虚 getter.
- 守卫证据: 同一桥接产品文件 `:428-436` 与 `:454-462` 均先比较 `__instance` 与惰性解析的 Watcher canonical 池, 再检查 `IsRunActive()`. 修复会保留这两段原文, 其它卡池和非混沌局继续放行.
- 只读证据命令: `rg -n 'PatchGetter|ReferenceEquals|ThirdPartyPoolIdsPostfix' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs'`.
- 最小修复范围: 在白名单 helper 中使用声明实现身份, 不能用 `GetBaseDefinition()` 把合法 override 强制改成最远祖先实现. 编译和实机效果仍未验证.

### [P1] 源码发现 AllCardIds 后缀被注册成前缀

- 记录时点: 2026-10-01 10:17:02 +08:00.
- 触发条件: 两个 getter 成功绑定后, 混沌局第一次访问 canonical Watcher 池的 `AllCardIds`.
- 实现前控制流: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs:374-375` 把 `ThirdPartyPoolIdsPostfix` 作为 helper 的第四个位置参数传入; `:620` 将该参数定义为 `prefix`; `:630` 无条件注册为前缀.
- 契约: `:452-469` 明确是后缀, 需要在原 getter 产生 `IEnumerable<ModelId>` 后对 `__result` 做并集. 当前前缀注册会使 `:464` 在原 getter 之前读取结果, 与既有后缀语义不符. 这是源码结论, 不是本轮实机复现.
- 只读证据命令: `rg -n 'ThirdPartyPoolIdsPostfix|List<ModelId> merged|harmony.Patch\(getter' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs'`.
- 最小修复范围: 同一 helper 增加可选 postfix 参数, 仅该调用改为命名的 postfix 注册; 保持 ID 合并正文和其它映射不变.
- 尚缺证据: 主会话需检查 Harmony 的 Prefixes/Postfixes 元数据, 并验证 Watcher 活跃局及非 Watcher/非活跃局的消费者行为.
### [P2] 已核对声明 API 与补丁阶段契约

- 只读依据: `G:\omp works\Sts\sts2-spire1\.nuget\packages\lib.harmony\2.4.2\lib\net9.0\0Harmony.xml:1730-1734` 明确 `AccessTools.DeclaredPropertyGetter(Type, string)` 只解析直接声明属性; `:1781-1785` 明确 `PropertyGetter` 搜索继承链. `:893-900` 区分命名的 prefix 与 postfix 参数.
- 本地已有用法: `G:\omp works\Sts\sts2-spire1\research\BaseLib-StS2\Patches\Content\ContentPatches.cs:365-369` 先获取声明 getter 再补丁. 这是 API 和源码先例, 不是本轮已修代码实测.
- Watcher 声明佐证: `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherCardPool.cs:6-107` 继承 `CardPoolModel`, 未声明两个目标属性. 当前运行日志也独立给出了实际应补丁的基类方法.
- 实施选择: 保留当前继承链查找, 再用所得 `DeclaringType` 重新取得声明 getter, 不强制回退最远祖先. 检查 getter 是非静态, 非抽象, 无参方法, 用命名的 postfix 承载 ID 合并.
- 最小范围和待验收边界不变: 仅白名单产品文件; 不增加 AutoAnthony/Watcher 强类型引用; 主会话仍须运行中央构建, Harmony 元数据检查和新隐藏实机回归.
### [P2] 实现已落盘, 源码差异范围已读回核对

- 完成时点: 2026-10-01 10:22:59 +08:00.
- 唯一修改的产品路径: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`.
- 当前准确行号: `:374-375` 将 `ThirdPartyPoolIdsPostfix` 显式注册为 postfix; `:620-621` 为 helper 分离可选 prefix/postfix; `:625-631` 通过 getter 的 `DeclaringType` 重新取得直接声明实现; `:637-642` 拒绝静态/抽象/带参 getter, 再按指定阶段注册补丁.
- 保持不变: `:428-449` 的 Watcher 池实例与活跃局守卫和混沌卡生成正文, `:454-469` 的 ID 合并正文, 惰性池解析, 三个自有角色的映射和起手规则. 没有引入 AutoAnthony 或 Watcher 强类型引用, 没有修改项目引用或其它产品文件.
- 源码 SHA256: `B2C1E115D7D00BD369242B976D26D2B470BAE90122B6E5E5DF1ACABCD8F2DFA5`.
- 只读核对命令: `git -C 'G:\omp works\Sts\sts2-spire1' diff -- mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs`. 差异只有 getter 注册调用和 helper 两处, 未触及守卫与映射正文. 此处的 Git 参数是仓库相对 pathspec, 产品路径以上方绝对路径为准.
- 除产品文件外只写本报告. 未暂存, 未提交, 未推送, 未构建, 未运行 lint/probe/测试/游戏/部署, 未写入共享配置或 Steam 安装, 未操作窗口或键鼠.
- 尚缺证据: 真实 Harmony 接受新目标, 实际编译产物无第三方硬 AssemblyRef, 活跃与非活跃局的消费者隔离. 以下中央验证建议均未由本实现者执行.
## 进行中

- 已读取请求, 工作区约束及 `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md`.
- 初始计划已完成: getter 声明类型, Harmony 方法参数及现有实例守卫均已通过本地源码和文档核对, 修改已完成. 当前没有本实现者继续执行中的代码任务; 等待配对 reviewer 与主会话中央验证.

- 2026-10-01 10:21:03 +08:00: 唯一白名单产品文件已落盘. 已加入声明 getter 重绑定及无参实例实现检查, AllCardIds 已改为 postfix 命名注册. 正在仅通过读回源码和 Git diff 核对修改范围, 不运行任何构建或测试.

## 未知

- 初始未确认项已关闭: 实例守卫无需修改, 其它映射正文保持原样. 行为验收仍未知, 不以源码读回替代测试.
- 编译, lint, probe, 游戏, 部署和共享配置操作均未执行, 后续仍不执行. 不启动窗口或操作键鼠.
- 请求指定 `gpt-6-astra-ar`, `gateway -> agentrouter -> gpt-6-astra`; 当前会话实际模型和路由元数据未在本报告中独立核验, 不以提示词当作路由证据. 本实现者不再委派.
### 主会话中央验证建议, 本实现者未执行

1. reviewer 先审核上述两处差异. 核对继承 getter 的 `DeclaringType`/`ReflectedType` 归一, override 不被 `GetBaseDefinition()` 改写, 两个守卫不扩大作用域, ID 合并实际进入 Postfixes.
2. 中央构建前保留 G: 缓存和低优先级执行约束. 可用命令如下, 不是本轮已通过证据:

```powershell
$env:TEMP = 'G:\tmp'
$env:TMP = 'G:\tmp'
foreach ($name in 'NUGET_PACKAGES','NUGET_HTTP_CACHE_PATH','DOTNET_CLI_HOME') {
    [Environment]::SetEnvironmentVariable($name, [Environment]::GetEnvironmentVariable($name, 'User'), 'Process')
}
& 'C:\Program Files\dotnet\dotnet.exe' build 'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' -c Release -m:1 -nr:false -p:UseSharedCompilation=false -p:CopyToModsFolderOnBuild=false '-p:Sts2Path=G:\omp works\Sts\_runtime\sts2-test-client-B'
```

3. 全套形态探针可作为旁路回归, 但它不包含本次桥接类, 不能据此宣称 getter 修复已验证:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj' -c Release -m:1 -nr:false -p:UseSharedCompilation=false
& 'C:\Program Files\dotnet\dotnet.exe' run --project 'G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj' -c Release --no-build
```

4. 主会话需新增或扩展定向 Harmony 检查, 本轮没有假造该测试. 在真实游戏程序集和 Watcher 类型下验证 getter 声明为 `CardPoolModel`, 两个返回类型分别是 `IEnumerable<CardModel>` 和 `IEnumerable<ModelId>`, 目标均为无参实例方法. `ThirdPartyPoolContentsPrefix` 应在 AllCards 的 Prefixes 内; `ThirdPartyPoolIdsPostfix` 只应在 AllCardIds 的 Postfixes 内. 覆盖 Watcher 活跃混沌局的结果可枚举, 非 Watcher 池与非混沌局不被更改, 保留原卡选项仍按原逻辑工作.
5. 新 Release 部署及真实隐藏启动只由主会话执行. 可参考 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-hidden-smoke-20261001-mods-r1.ps1`, 但必须复制到新脚本并把硬编码运行输出目录改为全新 G: 目录, 不覆盖已有实机证据, 不直接重跑旧脚本. 继续使用测试副本 B, 隔离 APPDATA/LOCALAPPDATA/TEMP, 不抢焦点. 验收新日志中两个 getter 的 patch failed 消失, 再单独验证卡池消费者. 到达主菜单不等于战斗通过.
6. 仅在 reviewer 和主会话验收后由主会话按白名单提交. 当前产品源码变更与本报告均未由本实现者提交.

CODE_COMPLETE
