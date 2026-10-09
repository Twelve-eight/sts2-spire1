# Spire1 Forms Beta r3 独立只读审查 - 2026-10-03

审查对象: 提交 `a6e46e53ae891e4faa7b640a64c1000a9e566c9a`(release(spire1): publish forms beta r3 with source-driven gates)及其涉及的形态桥接、可选依赖加载、发布门禁与运行报告。
审查身份: 只读审查员。未改产品代码, 未构建, 未部署, 未启动游戏, 未再委派, 未写 C:。
声明: 本报告只记录本轮亲自复现的静态审查与字节级证据; 既有 smoke 报告只作为被审查对象, 不重新声称为本审查的运行验证。
指定模型: 用户本轮指定 `global:deepseek-v4.1-flash`, 路由 wb2api via local gateway; 本会话未派生任何子代理。

## 已确认(有证据)

### C1 [P0] 发布 payload 含未提交代码, 且发布脚本不校验代码与产物的一致性

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` 行 218-231: 直接 `dotnet build mod/Spire1.csproj`, 无"工作树必须干净"门禁, 无源码哈希。
  - 同文件行 233-238: DLL 候选按 `mod\.godot\mono\temp\bin\$Configuration\Spire1.dll` -> `mod\bin\...` -> `mod\publish\Spire1.dll` 取第一个存在者, 不比对时间戳或内容哈希。
  - 同文件行 270-281: `SourceCommit=(git -C $RepoRoot rev-parse HEAD)` 只被记录, 不参与任何校验。
- 触发条件: 在存在未提交修改的工作树上运行本脚本(本轮 r4 正是如此)。
- 本轮证据:
  - `git status --porcelain` 有 165 项改动; `git diff a6e46e5 --stat -- mod/Spire1Code` = 189 files changed, 2527 insertions(+), 124 deletions(-)。
  - `release-r4-current-20261003\evidence\release-manifest.json` 记录 `SourceCommit=f8be5c2674fed29e6876007a88c1c2c78f95add5`, 但工作树已含 a6e46e5 之后的改动。
  - 二进制取证: 在 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\release-r4-current-20261003\payload\mods\Spire1\Spire1.dll`(SHA256 `4F49BA0D...`)的原始字节里, UTF-8 名表含 `Spire1RelicPoolGatePatch`、`FilterDisabledRelics`、`IsAllowedInShops`、`_cardsGateLogged`、`_powersGateLogged`, UTF-16 串表含 `Collect card grant skipped: cards content group is off`。
  - 反证: `git grep -l "Spire1RelicPoolGatePatch" a6e46e5 -- mod`、`git grep -l "_cardsGateLogged" a6e46e5 -- mod` 均为空; `git show a6e46e5:mod/Spire1Code/Relics/Spire1Relic.cs` 无 `IsAllowed*`/`GetUnlockedRelics`; `git show a6e46e5:mod/Spire1Code/Powers/CollectPower.cs` 无 `_cardsGateLogged`/`IsEnabled`。这些符号只存在于本工作树(`mod/Spire1Code/Relics/Spire1Relic.cs` 行 37/45/54-55、`mod/Spire1Code/Powers/CollectPower.cs` 行 23-37/58-74)。
- 当前控制流: BUILD(工作树编译) -> 复制 DLL 到 payload -> 打 PCK -> 跑门禁(只看 AssemblyRef/manifest/TypeDef, 不看代码来源) -> 可选 `-Promote`。
- 最小修复范围: 在 `Build-Spire1Release.ps1` 增加"工作树必须干净, 否则需显式 `-AllowDirty`"门禁; 在 `release-manifest.json` 记录每个源文件的 SHA256(或 `git status --porcelain` 全文 + `git diff` 摘要), 使 payload 可追溯到确切源码; 或改为从固定 commit 导出后构建。
- 尚缺的实机证据: 本项为版本控制 + 二进制取证, 未运行游戏; 只证明发布可追溯性不成立, 不判断该未提交代码本身是否破坏运行。

### C2 [P1] AutoAnthony / Watcher / AutoAnthonyWatcher 仍无二进制硬引用, 也无 manifest 硬前置(静态 + 元数据双重证据)

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs` 行 1-5(仅 `System.Reflection`/`HarmonyLib`/引擎命名空间); 行 120-133 全部类型经 `AppDomain.CurrentDomain.GetAssemblies()` + `AaType(...)` 解析。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` 行 108-116(Watcher 程序集/类型反射解析); 行 193-215(`RequireMarker`/`RequireMethod` 严格签名校验)。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` 行 32-38: 明确注释 AutoAnthony interop 无编译期 `<Reference>`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1.json` 行 12-17: `dependencies` 只有 `BaseLib >= 3.4.5`。
- 触发条件: 未安装 Watcher/AutoAnthony 的机器加载 Spire1。
- 本轮复现命令与输出:
  `dotnet tools\build-gates\bin\Release\net9.0\Spire1ReleaseGate.dll --dll <payload dll> --config tools\build-gates\gate-config.json --gates assemblyref,manifest,typedef`
  `[gate] AssemblyRef 15 个, TypeDef 978 个`
  `[gate] PASS assemblyref-forbidden: AssemblyRef 表不含任何禁止的 mod 程序集 (检查了 5 个)`
  `[gate] PASS manifest-consistency: 二进制引用的 mod 程序集: BaseLib / manifest 声明的依赖 id: BaseLib`
  `[gate] PASS typedef-forbidden`
  `[gate] 结果: PASS (全部门禁通过)`
- 当前控制流: 桥接在 `Apply`/`TryBind` 内解析已加载程序集; 缺席时 `AutoAnthonyCompatBridge.Apply` 行 227-231 记录 absent 并返回 false, 不挂补丁。
- 最小修复范围: 无需修复; 若要保持, 应在新增任何桥接代码时同步扩充 `tools/build-gates/gate-config.json` 的 `forbiddenAssemblyRefs`。
- 尚缺的实机证据: r32 九场景交叉挂载报告声称 9/9 通过, 本轮未重跑, 不采信为本审查的运行证据。

### C3 [P1] 发布门禁对 r3 payload DLL 独立复现 PASS, 且 zip 身份与 beta-package-r3 报告一致

- 绝对路径:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\release-r4-current-20261003\payload\mods\Spire1\Spire1.dll`
  - `G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r3.zip`
- 本轮复现命令与输出: 见 C2 的门禁 PASS 输出; 另:
  - payload DLL 独立哈希 = `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`, 788480 bytes。
  - zip 独立哈希 = `D2C161A52DAA3F9694D38088C8CC03A07860AAC9B54D2831968987E6B45EEFD5`, 19372105 bytes; 包内恰 4 项: `mods/Spire1/Spire1.dll`、`mods/Spire1/Spire1.pck`、`mods/Spire1/Spire1.json`、`README-安装说明.txt`。
  - 包内 DLL/PCK/JSON 逐项 SHA256 = `4F49BA0D...` / `70CCBB4D...` / `CDBD57D5...`, 与 `docs\reports\form-playable-20260928\beta-package-r3-20261003.md` 一致。
- 当前控制流: 门禁工具解析 ECMA-335 元数据表; 三道表由 `tools/build-gates/gate-config.json` 单一事实源驱动。
- 最小修复范围: 无需修复。
- 尚缺的实机证据: 本项为字节/元数据静态证据, 不含任何真实运行证据。

### C4 [P1] 发布的 PCK 结构/路径集合独立复现正确, payload 不含被排除资产泄漏

- 绝对路径: zip 内 `mods/Spire1/Spire1.pck`; 校验器 `G:\omp works\Sts\sts2-spire1\tools\release\Verify-Spire1Pck.ps1`; 契约 `docs\RELEASE-PACKAGE-CONTRACT-20261003.md`。
- 本轮复现命令: `powershell -File G:\omp works\.tmp\form-playable-20260928-01a0e7ad\review-r3-independent-20261003\pck-compare3.ps1`(在内存中按 PCK 格式解析 zip 内 pck, 未写 C:)。
- 本轮复现输出:
  - 头: `magic=GDPC fmt=3 eng=4.5.1 flags=2 fb=112 do=19516958 cnt=1464`。
  - 集合比对: `expected=1464 actual=1464 missing=0 unexpected=0`。
  - `omega_in_pck=0`; `charui_in_pck=2`(仅 `big_energy.png`/`text_energy.png`, 与 allowlist 一致)。
  - 另独立统计: `.godot/imported/*.ctex` 720、`Spire1/**/*.import` 720、`Spire1/**/*.json` 24, 无其它后缀; 全部目录项数据偏移 32 字节对齐; 抽样 5 个 `.ctex` 的 `GST2` 头与 MD5 校验通过。
- 当前控制流: `asset-manifest.Kept` -> 逐项用 `res://Spire1/<rel>` 的 MD5 推导 `.ctex` 名与 `.import`/json 路径 -> 解析 PCK 目录项 -> 集合全等。
- 最小修复范围: 无需修复。补充说明: `tools/build-gates/gate-config.json` 的 `pckPublishGate` 段只被记录, `Program.cs`/`GateConfig.cs` 未解析它, 它是声明而非运行门禁; 真正的 PCK 结构门禁由 `Build-Spire1Release.ps1` 行 258-265 调用 `Verify-Spire1Pck.ps1` 执行。
- 尚缺的实机证据: 不含实机加载; 不能证明真实运行期 `ResourceLoader.Exists` 对每个路径都返回真。

### C5 [P1] payload 资产 allowlist 与当前具体模型类自洽: 无被排除却必需的图

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` 行 55-70(类名 -> snake stem)、行 118-150(card/relic/potion/power 图保留与 `omega_power` 排除)、行 160-172(既不在 keep 也无排除原因则 `throw ASSET-UNCLASSIFIED`)。
- 本轮复现命令: `powershell -File G:\omp works\.tmp\form-playable-20260928-01a0e7ad\review-r3-independent-20261003\asset-coverage.ps1` 与 `allowlist-gap.ps1`。
- 本轮复现输出:
  - Cards: 用脚本同款正则扫出 220 个具体 `Spire1Card|Spire1Curse` 类(222 个 .cs 中 2 个是基类 `Spire1Card.cs`/`Spire1Curse.cs`)。
  - 逐类核对 `images/card_portraits/<snake>.png` 与 `big/<snake>.png`: 有 10 个源图在源码目录即不存在(`apotheosis`、`big/dark_shackles`、`discovery`、`big/shrug_it_off`、`slimed`、`the_bomb` 及其 big), 由 `mod/Spire1Code/Extensions/StringExtensions.cs` 行 22-30 的 `card.png` fallback 覆盖, 不是漏包。
  - 把 Cards/Relics/Potions 三类具体类 stem 与 `asset-manifest.Excluded` 交叉, `excluded-but-needed` 计数 = 0。
  - `omega` 相关 2 项(`images/powers/omega_power.png`、`big/omega_power.png`)在 `Excluded`, 且代码无 `omega_power` 引用。
- 当前控制流: 脚本从 C# 具体类名推 snake stem -> 存在才 Add-Keep -> 未分类文件直接抛错。
- 最小修复范围: 无需修复。
- 尚缺的实机证据: 静态一致, 不证明 Godot 运行期路径命中; 终验需真人目视。

### C6 [P2] 发布脚本不会写入 Steam 安装、共享 mod_configs 或 C:, 但 `-Promote` 目标是仓库内 Workshop 目录

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` 行 3(默认只写 `.tmp`)、行 91(`Assert-Under $RepoRoot 'G:\omp works'`)、行 100(`Assert-Under $OutputRoot ...\.tmp`)、行 291-293(`workshopRoot = $RepoRoot\workshop\content\Spire1` + `Assert-Under` + `Assert-NoReparse`)、行 299-301(唯一 `Remove-Item`, 只删 payload 三件套以外的 `Spire1.pdb`/`.deps.json`/`.pck.sha256`)。
- 本轮复现证据:
  - `workshop`、`workshop\content`、`workshop\content\Spire1` 的 `LinkType` 均为空, 非 reparse point; 真身在仓库内 `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1`, 不是 Steam。
  - 脚本中 `Steam`/`mod_configs` 仅出现在注释; payload DLL 与 PCK 原始字节均不含 `G:\omp works`、`C:\Users`、`steamapps`、`.tmp\form-playable` 绝对路径串。
  - 补充: `Sts2PathDiscovery.props` 已去掉 Steam 回退(仅 `Sts2TestPath` -> `E:\Slay the Spire 2` -> 测试副本 B), 且 `mod/Spire1.csproj` 行 162-164 的 `AssertModsPathIdentity` 只在 `CopyToModsFolderOnBuild != false` 时生效, 本发布脚本固定传 `-p:CopyToModsFolderOnBuild=false`(行 222), 因此构建本身不触发任何部署写入。
- 当前控制流: OutputRoot 被限定在 `G:\omp works\.tmp`; `-Promote` 只写仓库内 `workshop\content\Spire1`; 都不触及 Steam 或共享 `mod_configs`。
- 最小修复范围: 无需修复; 若要更严, 可在 `-Promote` 前增加"目标不是 Steam 安装"的显式断言(当前只靠路径在仓库内隐式保证)。
- 尚缺的实机证据: 路径静态审查, 无实机写入测试; `-Promote` 本轮未运行。

### C7 [P2] 形态入口与三形态路径的静态控制流与报告一致(仅静态)

- 绝对路径与准确行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` 行 57-79(`TryBind`)、行 108-180(`ValidateBinding` 严格签名 + 目标存在性)、行 166-190(`VerifyInstalled` 全部通过才发布 `_binding`, 否则回滚)。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceCmd.cs` 行 17-27: 三入口转发到 `StanceCmd.Enter<CalmPower|WrathPower|DivinityPower>`。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs` 行 41-53: `FormStanceMode.IsEnabled` 时改走桥接; 行 60-69 保留无形态时的原版姿态路径。
  - 效果语义逐项核对: 虚空 `VoidFormEffectPower.cs` 行 109-131/213-262(支付事务 token, 自动打出不消耗)、群蛇 `SerpentFormPower.cs` 行 78-135(每条完成 CardPlay 3 点 Unpowered; 空 `HittableEnemies` 不掷 RNG)、恶魔 `DemonFormPower.cs` 行 83-141 + `DemonFormStrengthTransaction` 行 118-132/145-215/281-321(三角数, 只撤形态自身 accepted 部分)、死神 `ReaperFormEffectPower.cs` 行 26-35(dealer 本人或宠物 + `IsPoweredAttack` + `TotalDamage>0` -> Doom)、神格 `EchoFormEffectPower.cs` 行 42-68(playCount+1, 真正修改后才消耗)+ `CelestialFormPower.cs` 行 43-59(`max(round,3)` 能量与抽牌)。
- 触发条件: 自定义对局选中 `FormStanceModifier` 后进入三种姿态。
- 当前控制流: `ModelDb.Init` 后 `FormStanceModePatch` 调 `TryBind`; 入口经 `StanceCmd` -> `FormStanceWatcherBridge.Enter` -> 真实 Watcher `EnterCalm/EnterWrath/EnterDivinity`; 效果由 carrier `WatcherFormStancePower.CreateEffects` 挂载。
- 最小修复范围: 无(静态与报告一致)。
- 尚缺的实机证据: 全部形态数值、回合链、Void 免费额度多卡/自动打出边界、Divinity 同回合二次进入边界, 本轮均未运行。

## 进行中(半成品, 需复核)

### H1 [P1] `FormStanceWatcherBridge.TryBind` 是"一次性尝试", 绑定失败后无重试路径

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs` 行 34(`_attempted`)、行 58-62(`if (_attempted) return IsAvailable; _attempted = true;`)、行 98-190(失败时 `_binding=null` 并回滚); 唯一调用点 `mod/Spire1Code/Forms/FormStanceModePatch.cs` 行 9-13(`ModelDb.Init` 后置 `[HarmonyPriority(Priority.Last)]`)。
- 触发条件: `ModelDb.Init` 后置触发时 Watcher 程序集尚未加载, 或 Watcher 版本/签名漂移导致 `ValidateBinding` 抛异常。
- 当前控制流: 第一次 `TryBind` 失败即置 `_attempted=true`; 之后任何 `TryBind` 调用都直接返回 `IsAvailable`(false), 不会重试。`FormStanceMode.RequireAvailable` 行 24-27 会让形态局显式抛错(fail-closed, 不半替换)。
- 最小修复范围: 若判定"Watcher 只会在 ModelDb.Init 前加载"成立, 建议在 `TryBind` 失败时写一条明确的不可重试日志; 若认为存在晚加载, 需把 `_attempted` 改为"成功才锁死"并在 capability 程序集加载事件里重试(注意与 `AutoAnthonyLoadHook` 的线程边界一致)。
- 尚缺的实机证据: 未运行; 未验证 Watcher 实际加载时序是否总早于 `ModelDb.Init` 后置。

### H2 [P2] AutoAnthony 加载钩子的并发边界已收紧, 但仍有静态可证的非终止轮询窗口

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs` 行 501-539(`HookAssemblyLoad`, `AssemblyLoadGate` 内二次 shutdown 检查后才订阅, 行 521-523)、行 552-577(`UnhookAssemblyLoad`, 退订失败只记日志并保持 `_hooked=true`)、行 643-707(`EnsureRetryWakeSourceCore`: 先 `ThreadingTimer` 行 677, 失败再起 fallback `Thread` 行 698)、行 475-497(`ShouldStopNotifications`, 依赖 `_mainLoop` 有效)。
- 触发条件: AutoAnthony 程序集存在、核心补丁一直未 settle、且主线程 Apply 从未成功执行过(`CaptureMainLoop` 从未赋值 `_mainLoop`)。
- 当前控制流: `_mainLoop == null` 时 `ShouldStopNotifications()` 返回 false, fallback 线程以 1s 间隔持续轮询; 只有 `StopRetryTimer(permanent:true)`(行 738-770, 仅从 settled 成功路径进入)或 `OnProcessExit`(行 94-131)才能终止。该窄条件下线程无法自行停止。
- 最小修复范围: 在 `ShouldStopNotifications` 里为 `_mainLoop == null` 增加一个"从未取得主线程/已过 N 秒"的兜底判据, 或让 fallback 循环带上最大存活时间; 需与既有"不要在主线程未确认时误停"的设计意图对齐后再改。
- 尚缺的实机证据: 无并发/退出竞态注入; 上述为静态可证窗口, 非真实触发。三锁(`AssemblyLoadGate`/`RetryGate`/`ApplyGate`)顺序本轮只做源码阅读, 未做并发压测。

### H3 [P3] 发布脚本对 PCK packer 使用硬编码绝对路径

- 绝对路径与准确行号: `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1` 行 83: `$packer = 'G:\omp works\Sts\.nuget\packages\bschneppe.sts2.pckpacker\0.1.1\tools\net9.0\any\StS2PckPacker.dll'`; 行 87-89 只做 `Test-Path` 存在性检查。
- 触发条件: 换机器、改 `NUGET_PACKAGES`, 或该路径被清理后运行发布脚本。
- 当前控制流: 路径不存在时 `INPUT-MISSING` 直接抛错(fail loud, 不会静默跳过打包), 但无法自动解析到实际 NuGet 缓存位置。
- 最小修复范围: 改为从 `NuGet.config`/`project.assets.json` 或环境变量 `NUGET_PACKAGES` 解析 packer 路径, 保留 `INPUT-MISSING` 兜底。
- 尚缺的实机证据: 本项为脚本可移植性静态审查; 本轮该路径存在(`Test-Path` 为 True), 未触发失败。

## 未知(未覆盖)

- 可见 UI/动画/图标、长战斗全部回合边界、战中存档-读档、重连、多人同步、性能与完整平衡: 本轮为只读静态审查, 完全未运行游戏。
- 发布包的实机安装-加载-三角色-旧存档-运行历史路径: 未运行。
- `-Promote` 的真实执行、Workshop 上传与 Steam 侧可见性: 未运行。
- 并发压测(多锁顺序、`ProcessExit` 与 in-flight Apply 竞态、fallback 线程终止): 未做, 仅源码阅读。
- r32 九场景交叉挂载、r5 三形态烟测、r25/r23 既有实机报告: 只读取报告文本与结果 JSON, 未复跑其运行; 不把它们重新声称为本轮运行验证。
- 形态效果在真实多目标/多回合/多段攻击下的数值边界: 未验证。
- `AutoAnthonyWatcher` 官方 addon 接管后的 legacy unpatch 真实行为: 未运行。
- C1 中未提交代码本身是否引入回归: 本审查未构建, 无法判断。
