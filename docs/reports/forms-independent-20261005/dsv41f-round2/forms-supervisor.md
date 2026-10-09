WAITING_GATE

# Forms 独立化静态监督 - 激活后增量记录

## 已确认

### G-01 [P3] 完成门禁成立, 可以开始监督
- 路径与行号: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\coordination.md:26-33`; `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\forms-worker.md:41-52,89-93`.
- 证据: 主会话记录原生 `multi_agent_v1.wait_agent` 精确目标 `01a108c3-ee55-75b0-9671-b98379e2c7b9` 返回 `completed`, `timed_out=false`; 实现报告已存在并明确实现结束. 本监督由用户重新激活, 未自行轮询或等待其它代理.
- 风险: 完成状态不代表源码正确, 编译通过或实机通过; 既有 `WAITING_GATE` 仅保留为历史门禁记录.
- 最小修复: 无; 仅对门禁之后读取到的最终源码开展静态审查. 项目按用户确认的 `sts2-forms/mod/Forms.csproj` 正常布局核对, 不把 repo/mod 布局判作缺陷.

## 进行中

- 正在依次核对项目边界/资源/ID, 反射协议, 晚 AssemblyLoad 主线程调度, 安装回滚, ShuttingDown 回调与实现报告真实性. 每完成一面追加证据; 未写入 `SUPERVISION_PASS`.

## 未知

### U-01 [P2] 静态监督不能替代二进制与运行验收
- 路径与行号: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md:116-142`; `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\forms-worker.md:45-52`.
- 证据: 契约要求主会话集中构建、AssemblyRef/TypeDef/PCK 门禁和实际运行; 实现报告明确未构建/未测试. 本监督只读静态产物且只写本报告.
- 风险: ModelDb 真实时序、旧档/联机身份、UI 与真实晚加载行为均不可据此宣称通过.
- 最小修复: 由主会话在明确授权与隔离输出边界内完成中央验收; 本监督不构建、不测试、不部署、不启动游戏、不改共享配置、不再委派.

## 已确认

### F-01 [P1] 中央诊断构建真实失败: Logger 类型歧义
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs:4,6,24`; `G:\omp works\.tmp\forms-independent-20261005\forms-build-r1.log:3-9`.
- 证据: 主会话报告诊断构建 exit=1; 已只读核对日志中的 `CS0104`, `Logger` 在 `Godot.Logger` 与 `MegaCrit.Sts2.Core.Logging.Logger` 之间不明确, 日志为 0 警告/1 错误. 源文件同时导入两 namespace 并以未限定 `Logger` 声明属性. 源码 SHA256=`98E6467AB7267DF7DEF4BA1B4835F7708840728299A3AF30746B48F31A895F1E`.
- 风险: 当前 Forms 源码无法产出通过此次构建的独立 DLL; 实现报告 `forms-worker.md:52` 的人工核对不覆盖实际编译阻断. 本项是中央编译证据, 不是本监督执行了构建, 也不是发布或实机结果.
- 最小修复: 为属性类型使用 `MegaCrit.Sts2.Core.Logging.Logger` 或明确的 using alias; 由主会话重跑隔离诊断构建并继续收集后续编译错误.

### F-02 [P1] 默认自动复制没有最终 ModsPath 安装身份门禁
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\Forms.csproj:68-76`; `G:\omp works\Sts\sts2-forms\mod\Sts2PathDiscovery.props:26-35`; `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md:118-130`.
- 证据: `CopyToModsFolderOnBuild` 在属性不等于 `false` 时默认运行, 直接将 DLL/manifest/PDB 复制到 `$(ModsPath)$(MSBuildProjectName)/`; 目标未解析并验证最终绝对路径, 未检查 Steam 身份, 也无明确部署授权开关. 测试副本优先的 discovery 不防止 `/p:ModsPath=` 或 `/p:Sts2Path=` 的显式覆盖. 项目文件 SHA256=`E0D98E43A527D8EE5BD2CD230094DEB975F458C14443ECB745ACEC3BABC176E9`.
- 风险: 后续为解决 F-01 运行普通构建, 只要最终 ModsPath 指向 Steam 即可能触犯禁止写用户 play install 的硬边界; 当前构建已失败, 本监督没有尝试复制或复现此写入.
- 最小修复: 默认禁用自动部署; 若保留显式 opt-in, 对最终解析目的地 fail closed, 仅允许已确认的非 Steam 测试安装并拒绝 Steam 路径/身份. 不只依靠 discovery 默认值. 不增加绕过门禁的另一个自动复制目标.

## 已确认

### F-03 [P1] AssemblyLoad 订阅使用 Action, 与事件/回调真实签名不符
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs:53,236-258`.
- 证据: `_assemblyLoadHandler` 被声明为 `Action?`; 唯一 `OnAssemblyLoad` 方法需要 `(object?, AssemblyLoadEventArgs)` 两个参数, 却在第 240 行赋给 Action 并在第 241/248 行用于 `AppDomain.AssemblyLoad` 的订阅/退订. 该事件需要 `AssemblyLoadEventHandler`, 不是零参数 Action. 这是源码中的确定签名矛盾; 当前中央日志首先报告 Logger, 本监督未通过编译器复验此项.
- 风险: 即使修复 F-01, 本订阅与退订仍无法按这些类型编译; 晚加载唤醒和关闭退订也无从成立.
- 最小修复: 将字段类型改为 `AssemblyLoadEventHandler?`, 保持具名两参数回调和成对订阅/退订; 不以新 lambda 包装造成退订身份漂移. 交主会话集中编译复核.

## 已确认

### F-04 [P1] 已就绪主菜单中的晚 AssemblyLoad 没有后续主线程消费者
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMainMenuRetryPatch.cs:6-15`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModePatch.cs:9-14`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs:189-202,252-258`; `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md:103-104`.
- 证据: 完整 FormsCode 静态引用检索显示, 真正绑定仅由 ModelDb.Init postfix 与 NMainMenu._Ready postfix 触发. `OnAssemblyLoad` 只写 `_assemblyLoadPending=true`, 没有安排下一次主线程处理, 也无常驻帧回调/队列消费者. 在 ModelDb.Init 和当前主菜单 _Ready 都已结束后加载 Watcher, 设置 pending 不会重跑当前菜单 _Ready; 打开 modifier 列表的 patch 也只在绑定成功后才安装.
- 风险: 在已就绪主菜单原地等待的晚加载场景, 桥可以无限停在 Retryable, Forms 选项仍不可用. 这不是只有缺少实机证据, 而是源码缺少保证下一次主线程处理的路径. 实现报告 `forms-worker.md:32,48` 只能证明存在 Ready 时的重试入口, 不能证明此场景可唤醒.
- 最小修复: 在初始化/主菜单就绪时安装真实、可持续消费 pending 的主线程入口, 例如受生命周期管理的帧处理或既有主线程队列消费者. AssemblyLoad 回调仍只置通知, 不从任意线程触碰 Godot/Harmony; Terminal/ShuttingDown 停止消费. 不把一次性 _Ready postfix 作为晚加载保证.

### C-01 [P3] 安装失败回滚确实清空 native notification, 不重复报此缺陷
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs:125-179,385-392`.
- 证据: patch 安装后逐条核对本 Harmony owner/具体 PatchMethod; catch 第 145-149 行先清空 `_binding`, `_nativeNotification`, `_harmony`, BoundTargets 和 InstalledTargets, 再对 specs 的不同目标逐条撤销本 owner 并验证 owner 不再存在; 回滚残留或撤销异常进入 Terminal.
- 风险: 静态确认只覆盖字段清理和分支设计, 不证明真实 Harmony 回滚执行成功; 若回滚残留, 当前回调仍缺统一关闭门禁, 见后续 F-05.
- 最小修复: 保留 `_nativeNotification=null` 与仅撤本 owner 的逻辑, 不按旧报告假定 native notification 未清空; 与 F-05 的回调门禁一起复核实际失败路径.

## 已确认

### F-05 [P1] ShuttingDown/回滚残留中的 callback 与 transpiler helpers 没有统一立即返回门禁
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs:209-232,450-542,621-626,663-676`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs:23-34`; `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md:105-107`.
- 证据: Shutdown 先逐条 Unpatch, 到第 231 行才发布 ShuttingDown. Marker/stance/damage callbacks 与三个被注入原生 async MoveNext 的 helpers 都直接调用 `IsEnabled`, 没有 ShuttingDown/Terminal 门禁. 对已选 Forms 的 player, `_binding=null` 后 IsEnabled 转入 RequireAvailable 并抛异常, 不是立即返回. 对未选 Forms 的 player, `GainDivinityEntryEnergy` 仍调用 `PlayerCmd.GainEnergy`, `RemoveEndTurnDivinity` 仍调用 `PowerCmd.Remove`; `NotifyEndTurnDivinity` 会访问已经清空的 `_nativeNotification` 并抛异常. 已挂起的 `AfterStanceChanged` 在 await original 后仍用 `_binding!` 调 native context. Unpatch 不会撤销已在执行或 await 后恢复的旧调用栈.
- 风险: 退出/显式 teardown 或失败回滚残留时, Forms 回调可能继续产生 native 副作用或将清空引用变成异常, 违反关闭后统一立即返回契约. 回滚清空 native notification 是正确的, 但不能以此代替被注入 helper 的状态门禁.
- 最小修复: 在撤销 patch 前先原子发布关闭状态; 对 patch callbacks、runtime helpers 及 await 后续统一设置非抛异常的 shutdown/terminal gate, 关闭时不调 native、不调兼容通知. 保留未关闭且正常非 Forms 对局中的原生语义, 不用简单 `!IsAvailable` 全局跳过原生行为. 同时清空 pending 与后续通知缓存/marker 持有者, 避免挂起 continuation 使用旧绑定.

## 已确认

### C-02 [P3] 正常 repo/mod 布局、资源根与静态依赖边界成立
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\Forms.csproj:1-59`; `G:\omp works\Sts\sts2-forms\mod\Forms.json:2,8-15`; `G:\omp works\Sts\sts2-forms\mod\project.godot:13-15,25-27`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs:18-35,59-83`; `G:\omp works\Sts\sts2-spire1\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\build\BSchneppe.StS2.PckPacker.targets:5-16`.
- 证据: 项目存在于用户确认的 `sts2-forms/mod/Forms.csproj`, 名称默认为 Forms, Godot assembly name 与 manifest id 都为 Forms; SDK/net9/BaseLib 3.4.5/sts2/0Harmony/ModAnalyzers/PckPacker 依赖齐全, manifest 只声明 BaseLib. 实际 PckPacker targets 默认 source 为项目目录下 Forms/, res prefix 为 Forms, 不扫描 FormsCode/. 资源树仅 7 张 PNG 与 eng/zhs 各 powers/modifiers.json. MainFile 自持 initializer、Forms SimpleLoc 和本程序集 patch 扫描.
- 风险: 无 Spire1/Watcher Compile Reference 的静态结论不等于 DLL AssemblyRef 门禁已通过; 产物尚被 F-01/F-03 阻断. 初始化 patch 部分失败的可靠性另记后续 finding.
- 最小修复: 布局、id、assembly name 与资源根不需返工; 保持明确静态/二进制区分. 自动部署风险按 F-02 收口.

### C-03 [P3] 十个旧 CustomID 与本地化 key 完整, 未发现 Spire1 编译期引用或生产 smoke runner/stub
- 路径与行号: `G:\omp works\Sts\sts2-forms\mod\FormsCode\VoidSerpentStancePower.cs:6`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\DemonReaperStancePower.cs:6`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\EchoCelestialStancePower.cs:6`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\VoidFormEffectPower.cs:20`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\SerpentFormPower.cs:22`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\DemonFormPower.cs:27`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\ReaperFormEffectPower.cs:20`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\EchoFormEffectPower.cs:17`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\CelestialFormPower.cs:18`; `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:13`; `G:\omp works\Sts\sts2-forms\mod\Forms\localization\eng\powers.json:2-28`; `G:\omp works\Sts\sts2-forms\mod\Forms\localization\zhs\powers.json:2-28`; 两份 eng/zhs `modifiers.json:2-3` 位于同一绝对资源根.
- 证据: 十个类型均显式使用 BaseLib.Utils.Attributes.CustomID, 各值逐一匹配契约 Sec 2.4; 9 个 power 各有 title/description/smartDescription, modifier 有 title/description. 全部 Spire1 类型提及均为注释或 full-name 字符串, carrier 自有基类为 FormsStancePower. FormsCode 文件清单没有 FormNativeSmokeRunner/Patch, 没有 NotImplemented/TODO/stub 占位结果. Icon 路径使用 res://Forms/ 且对应大小图资源存在.
- 风险: CustomID 静态保持不证明旧存档/联机真实解析成功; 实际 ModelDb 重复身份检查尚需 lifecycle 面审查. 静态未见 runner/stub 不替代 DLL TypeDef 检查.
- 最小修复: ID/key/资源路径本身不需返工; 不更名旧身份, 不将 runner 混入生产. 由主会话在编译修复后执行 AssemblyRef/TypeDef/PCK 门禁.
