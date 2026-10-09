# Spire1 窄返工实现报告 (r5) - CODE_COMPLETE

- 范围: 仅 `spire1-worker.request.md` 指定写集. 未构建/未测试/未部署/未运行游戏/未写 C:/未委派/未执行 git/未改 Release 脚本-csproj-gate-config/未碰另一发布会话 staging.
- 唯一指定模型: global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh (原生 Codex; 未换模型, 未 fallback, 未起 omp/codex exec).
- 产物 (4 文件改动 + 1 新文件), 全部无 BOM:
  1. 新 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormsMissingModifierSaveGuardPatch.cs` (LF)
  2. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs` (CRLF)
  3. `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs` (仅注释, LF)
  4. `...\mod\Spire1\localization\{eng,zhs}\powers.json` + `{eng,zhs}\modifiers.json`

## 已确认

### 任务来源
- 已读请求 `...\runtime-rework-r5\spire1-worker.request.md` 与最终 P1/P2/P3 监督报告.
  注: 请求里的相对路径 `dsv41f-round3\spire1-supervisor.md` 在 `runtime-rework-r5` 下不存在; 实际文件为
  `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round3\spire1-supervisor.md` (已按此读取).

### 1) 缺 Forms 读旧档保护 (P1, 已实现)
- 权威源码核对:
  - `.tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs:168-173` `FromSerializable(SerializableModifier serializable)`
    先 `SaveUtil.ModifierOrDeprecated(serializable.Id).ToMutable()`, 再 `Props?.Fill`.
  - `.tmp\dllsrc\MegaCrit.Sts2.Core.Saves\SaveUtil.cs:75-78` `ModifierOrDeprecated` 查不到 id 时返回 `DeprecatedModifier`.
  - `.tmp\dllsrc\MegaCrit.Sts2.Core.Runs\RunState.cs:296` 读档即 `save.Modifiers.Select(ModifierModel.FromSerializable)`.
  - `.tmp\dllsrc\MegaCrit.Sts2.Core.Saves.Runs\SerializableModifier.cs:8-9` `Id` 为 `ModelId?`.
  - `.tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModelId.cs:11-13` `Entry` 为 `string`; `ToString()` 为 `Category.Entry`.
  - 因此 Forms 缺失时旧档原始身份在解析前丢失; 旧 `HasFormsDeclaredModifier` 只能看到 `DeprecatedModifier`.
- 实现: `FormsMissingModifierSaveGuardPatch.cs` 对 `ModifierModel.FromSerializable(SerializableModifier)` 加
  `[HarmonyPrefix]` + `[HarmonyPriority(Priority.First)]`. 完整控制流/异常边界:
  1. `Prefix(SerializableModifier serializable)` 首行 `IsLegacyFormStanceModifierId(serializable)` 只读
     `serializable?.Id` 与 `ModelId.Entry`, 与 `"SPIRE1-FORM_STANCE_MODIFIER"` 做 `StringComparison.Ordinal`.
     该路径不触 ModelDb, 不反射 Forms 类型, 不调用任何可能抛异常的引擎 API.
  2. `Id == null` 或 `Entry != 常量` -> `return true`: 原样放行, 引擎走 `ModifierOrDeprecated` 原逻辑.
     故其它 vanilla/mod modifier 与普通 deprecated 旧内容完全不受影响 (只按精确 Entry 命中).
  3. `Entry == 常量` -> 解析之前检查 `FormsCompatibilityBridge.IsAvailable`:
     - true (Forms 程序集存在 + entry point 签名通过 + 运行期 `IsAvailable`) -> `return true`, 正常解析.
     - false -> 抛 `InvalidOperationException` (带 `DisabledReason`). 绝不返回 true 让它降成 DeprecatedModifier
       后按普通规则继续; 也绝不返回 false 去吞掉原方法体.
  4. 若 `FormsCompatibilityBridge.IsAvailable` 自身抛 (如 `FindFormsAssembly` 枚举程序集失败, 或同名多程序集),
     异常自 prefix 直接向上抛 = fail closed, 不会静默放行.
  5. 全名 `Forms.FormsCode.FormStanceModifier` 与旧 CustomID 的等价性: 独立 Forms 生产
     `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:12-14` 为 `[CustomID("SPIRE1-FORM_STANCE_MODIFIER")]`;
     BaseLib `PrefixIdPatch` 把 CustomID 作为 `ModelId.Entry`, 与本 patch 比对的 raw Entry 一致.
- 安装通道: `MainFile.cs:250-262` 扫描本程序集所有 `[HarmonyPatch]` 类型并 `CreateClassProcessor(type).Patch()`;
  新 patch 文件无需改 MainFile. `Spire1.csproj` 默认编译 `Spire1Code/Patches/**` (只有 `Spire1Code/Forms/**` 与两个
  smoke 文件被 Remove), 故本文件进入 Spire1.dll.
- 明确保证范围 (不夸大): 本保护只在 Spire1 程序集内代码可执行时生效. Spire1 与 Forms 两个 mod 都缺失时,
  本 prefix 根本不存在, 该环境不受本保护覆盖; 本报告与源码注释均不宣称覆盖该环境.

### 2) 选择判定分歧 fail-closed (P2, 已实现)
- `FormsCompatibilityBridge.cs` `IsSelected(Player)` (现 131-181 行): 命中精确身份 (`HasFormsDeclaredModifier`) 后
  调 `bridge.IsSelected(player)`; 返回 false 时抛 `InvalidOperationException` (selection disagreement fail closed),
  返回 true 才 `return true`. 不再静默回普通姿态规则.
- 同时 `IsFormStanceModifier` 的 `CustomIDAttribute` 反射 `catch` 由 `return false` 改为显式抛出:
  属性反射失败无法排除该 modifier 携带旧 CustomID, 不能以 false 把它当普通局; 全名精确命中路径无反射, 不受影响.
- 与 Forms 侧语义对照: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs:19-20`
  `IsSelected` 用 `modifier is FormStanceModifier` 类型谓词. 合法 Forms 局两端同为 true; 仅身份分歧时触发显式失败.
- 普通局路径不变: `HasFormsDeclaredModifier==false` 时直接返回 false, 不进入桥调用.

### 3) PowersGate 注释更正 (P3, 仅注释, 行为未改)
- `Spire1PowersGatePatch.cs` `IsFormsPower` 注释 (现 696-702 行) 改为: 判定只依据程序集 simple name +
  collectible 载入状态, 与 entry point 签名校验/`IsAvailable` 无关; 签名失败/Retryable/Terminal/ShuttingDown 期间
  仍识别为 Forms 类型; 仅 Forms 程序集缺失 (或 type 为 null) 时返回 false. 方法体未动.

### 4) 本地化去重 (P2-hub, 已实现, 最小 diff)
- 去除精确键集合 = 9 个 Forms 专属 power 前缀 x 3 后缀 + 1 modifier x 2:
  - `SPIRE1-VOID_SERPENT_STANCE_POWER` / `SPIRE1-DEMON_REAPER_STANCE_POWER` / `SPIRE1-ECHO_CELESTIAL_STANCE_POWER` /
    `SPIRE1-VOID_FORM_EFFECT_POWER` / `SPIRE1-SERPENT_FORM_POWER` / `SPIRE1-DEMON_FORM_POWER` /
    `SPIRE1-REAPER_FORM_EFFECT_POWER` / `SPIRE1-ECHO_FORM_EFFECT_POWER` / `SPIRE1-CELESTIAL_FORM_POWER`
    各 `.title` / `.description` / `.smartDescription` (共 27 键);
  - `SPIRE1-FORM_STANCE_MODIFIER.title` / `.description` (共 2 键).
- 结果: `eng\powers.json` / `zhs\powers.json` 各删 27 键, 171 键; `eng\modifiers.json` / `zhs\modifiers.json` 各删 2 键,
  剩空对象 (保留 `{}` 等价内容 `{\n}`).
- 去除后静态核对 (逐文件):
  - JSON 均可被 `ConvertFrom-Json` 解析; 无重复键; 尾部逗号已修正; 无 BOM; 换行与原文一致 (LF).
  - 9 个 Forms power 前缀与 `SPIRE1-FORM_STANCE_MODIFIER.` 在两 locale 中 0 残留.
  - eng/zhs keys 数量一致 (171/171), 无 key 漂移.
  - 普通姿态/共享键全部保留: `SPIRE1-CALM_POWER.*` / `SPIRE1-WRATH_POWER.*` / `SPIRE1-DIVINITY_POWER.*` /
    `SPIRE1-DEVA_FORM_POWER.*` / `SPIRE1-WRAITH_FORM_POWER.*` / `SPIRE1-RUSHDOWN_POWER.*` /
    `SPIRE1-MENTAL_FORTRESS_POWER.*` / `SPIRE1-LIKE_WATER_POWER.*` / `SPIRE1-SIMMERING_FURY_POWER.*` /
    `SPIRE1-MANTRA_POWER.*` / `SPIRE1-TALK_TO_THE_HAND_POWER.*` / `SPIRE1-WAVE_OF_THE_HAND_POWER.*` 等均在.
  - 未新增任何 zhs 译名; 未改任何其它 assets; 未整体重格式化 (只按行删除).
- 非 Forms 键值相等的强证据 (可复现): 用 Forms 生产同源行块按原始相对顺序回填, 重建的 pre-edit 字节 SHA-256 与
  改动前实测 SHA-256 逐一相等, 证明除被删行外没有改动其它键/值/顺序/格式:
  - `eng\powers.json` 重建 `9405d9e0...579a` == pre-edit `9405d9e0...579a` MATCH
  - `zhs\powers.json` 重建 `ba8933fc...5414` == pre-edit `ba8933fc...5414` MATCH
  - `eng\modifiers.json` 重建 `3f6a98cc...85d8` == pre-edit `3f6a98cc...85d8` MATCH
  - `zhs\modifiers.json` 重建 `d43f6356...5cab` == pre-edit `d43f6356...5cab` MATCH
- 键集合一致性: Spire1 删除的 9 power 键集合与 `G:\omp works\Sts\sts2-forms\mod\Forms\localization\{eng,zhs}\powers.json`
  (27 键) 完全相同; modifier 2 键与 Forms `{eng,zhs}\modifiers.json` 相同.
- 消费方核对: 除 `Spire1Code\Forms\**` (默认不编译进 Spire1.dll) 与两个 smoke 文件外, Spire1 无对 9 个 Forms power
  类型的引用; 普通 `DemonForm` 卡用的是 vanilla `MegaCrit.Sts2.Core.Models.Powers.DemonFormPower` (无 SPIRE1- 前缀键).

### 修改文件与 SHA-256 (改动后实测)
- `FormsMissingModifierSaveGuardPatch.cs` (新)
- `FormsCompatibilityBridge.cs` (CRLF, 无 BOM)
- `Spire1PowersGatePatch.cs` (仅注释)
- `eng\powers.json` / `zhs\powers.json` / `eng\modifiers.json` / `zhs\modifiers.json` (上列)

## 进行中
- 无. 白名单内四项已全部落盘; 本回合按请求立即结束实现.

## 未知
- 未构建/未 lint/未测试/未部署/未运行游戏 (按请求), 故全部结论为源码级与静态 JSON 级证据, 不构成实机验证.
- 未实机验证: Forms 缺失时真实旧档经本 prefix 抛错的实际表现; 本局已选形态且桥可用但 Forms 报 IsSelected=false
  的真实触发面; `FormsCompatibilityBridge.IsAvailable` 在 Retryable/Terminal/ShuttingDown 之间的真实时序.
- 缺全模组场景未知 (不可由本写集覆盖): Spire1 与 Forms 两个 mod 都缺失时本 prefix 不存在, 该环境不受本 mod 保护.
- 未核对构建产物 AssemblyRef/TypeDef/manifest 与两 PCK 资源集合 (需主会话集中构建门禁).

CODE_COMPLETE