# Visual assets audit r12 - 2026-10-01

范围: G:\omp works\Sts\sts2-spire1 的姿态形态视觉资源和 Godot 路径。
限制: 只读审查。未修改产品代码、构建产物、部署目录、游戏、Steam 安装或共享配置；未构建、未部署、未启动游戏。
模型与路由: 用户指定 `6.1sol` / `agentrouter`。本报告未再委派子代理；以下结论仅来自本轮本地只读命令和文件内容。

## 已确认

### 初始证据
- 优先级: P0
- 结论: `Spire1.pck` 存在且为非空文件；当前仅证明打包文件存在，不能据此证明其中包含姿态 icon/effect 资源。
- 绝对路径与行号: `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck`，二进制无源码行号。
- 触发条件: 审查开始时先确认目标 PCK 可读。
- 命令: `Get-Item -LiteralPath 'G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck' | Select-Object FullName,Length,LastWriteTime`
- 证据: `Length = 28865174`，`LastWriteTime = 2026/9/30 1:45:45`。
- 最小修复范围: 无；本项仅记录资产包存在性。
- 尚缺的实机证据: PCK 内部目录/资源条目，以及真实游戏中的视觉加载结果。

## 进行中

- 检查面 1-5 已完成并追加到本报告；没有待执行的产品修改或运行时操作。
- 当前仅保留证据边界记录：真实游戏视觉显示、Godot 目标环境对反斜杠路径的实际容忍结果，以及隐藏 effect 是否按产品意图需要独立 icon，均未通过本轮只读审查确认。

## 未知

- 未证明真实游戏中三种姿态载体 icon 已成功加载并显示；PCK 条目存在不等于视觉验收通过。
- 未能仅凭当前证据在 PCK 缺资源、路径分隔符问题或二者叠加之间选择唯一缺图根因。
- 未判定六个 `IsVisibleInternal => false` effect 是否按产品意图需要独立 icon。
- `docs/terminology-glossary.md` 缺失，因此未对中文术语权威性作超出 token 覆盖的判断。

### 检查面 1: PCK 形态 icon/effect 资源
- 优先级: P1
- 结论: `Spire1.pck` 的 Godot PCK v3 目录可只读解析。载体使用的三组 icon 资源已在 PCK 中确认: `Spire1/images/powers/calm_power.png.import`、`Spire1/images/powers/wrath_power.png.import`、`Spire1/images/powers/divinity_power.png.import`，以及对应 `big/` 条目；同时存在对应 `.godot/imported/*ctex` 载荷。PCK 目录中没有 `power_atlas` 条目。
- 绝对路径与行号: PCK `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck`，二进制目录无源码行号；资源路径为 PCK 内路径。载体请求源码为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:40-49`。
- 触发条件: 用 Godot PCK v3 目录布局读取头部和目录；按 `flags=2` 将相对文件偏移加到 `file_base`，逐项核对资源名。
- 命令: `python - <<'PY' ...` 的只读解析脚本，读取 `GDPC` 头部、`file_base`、`directory_offset`、目录项和路径；筛选 `calm_power`、`wrath_power`、`divinity_power`、六个 effect Power 名称及 `power_atlas`。
- 证据: 头部为 `format=3`, `Godot=4.5.1`, `flags=2`, `file_base=112`, `directory_offset=28658590`, `file_count=1996`，目录结束偏移等于文件长度 `28865174`。三组载体各有普通和 `big` 的 `.import` 条目，并各有两项 `.godot/imported/*ctex`。六个 effect Power 名称对应的 `Spire1/images/powers/*.png.import` 条目均未在目录结果中出现。
- 资源路径证据:
  - `Spire1/images/powers/calm_power.png.import`
  - `Spire1/images/powers/big/calm_power.png.import`
  - `Spire1/images/powers/wrath_power.png.import`
  - `Spire1/images/powers/big/wrath_power.png.import`
  - `Spire1/images/powers/divinity_power.png.import`
  - `Spire1/images/powers/big/divinity_power.png.import`
  - 对应导入载荷示例: `.godot/imported/calm_power.png-15fc62f3e1f2529202cada5d52caeab2.ctex`、`.godot/imported/wrath_power.png-818ddcda6903261fff6f50c9c7fd1489.ctex`、`.godot/imported/divinity_power.png-8a1276352be7ac0affb2070df621dc25.ctex`。
- 解释边界: PCK 导出通常以原始资源的 `.import` 描述和 `.godot/imported/*.ctex` 载荷表示；本项证明了三组载体资源在包目录中可定位，不等于真实游戏已成功加载或视觉显示正确。六个 effect Power 的源码均为 `IsVisibleInternal => false`，本轮未把不可见 effect 当作必须有独立 icon 的事实。
- 最小修复范围: 若契约要求六个隐藏 effect 也拥有独立视觉资源，最小范围是新增六个对应普通和 `big` 资源并重打 PCK，同时核对其加载路径；当前证据不足以要求这项修复。
- 尚缺的实机证据: 真实游戏中 ResourceLoader 对 `.import` 和 `.ctex` 的解析、载体与隐藏 effect 的最终 UI 呈现。

### 检查面 2: 源码请求路径与 PCK 路径对比
- 优先级: P1
- 结论: 姿态承载器的三种 icon 名称及 Godot 路径与 PCK 中的正斜杠条目一致；但通用 Power 图片路径构造在 Windows 上使用 `Path.Join`，运行时日志已显示为反斜杠路径，形成确切的路径分隔符风险。当前证据不能把该风险单独认定为缺图的唯一根因。
- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:40-49`：`Calm/Wrath/Divinity` 分别映射到 `calm_power.png`、`wrath_power.png`、`divinity_power.png`；请求路径为 `res://Spire1/images/powers/` 与 `res://Spire1/images/powers/big/` 加文件名。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StringExtensions.cs:31-46`：`PowerImagePath` 和 `BigPowerImagePath` 通过 `Path.Join(MainFile.ResPath, ...)` 构造路径。
  - `G:\omp works\Sts\sts2-spire1\.tmp\review-security.md:30-33`：已有静态审查记录指出 Windows 下 `Path.Join` 产出反斜杠，并建议统一 `/`。
  - PCK 内路径见 `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck` 的目录条目；已确认条目使用 `Spire1/images/...` 正斜杠。
- 触发条件: 逐行读取承载器和通用路径构造源码，并将其与 PCK 目录条目和日志中的实际请求字符串对比。
- 命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs'`、`Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StringExtensions.cs'`、以及上一检查面使用的只读 PCK 目录解析脚本。
- 证据: 承载器源码字面量为 `res://Spire1/images/powers/...`；PCK 条目为 `Spire1/images/powers/...`；通用构造函数使用 `Path.Join`，不是 Godot 虚拟路径的显式 `/` 拼接。日志中的实际反斜杠请求在 `godot.log:395-415`，详见检查面 4。
- 解释边界: 仅从源码和 PCK 目录可以确认路径分隔符风险，不能据此证明三种承载 icon 在真实游戏中已经显示，也不能据此证明所有缺图均由分隔符造成。
- 最小修复范围: 只作为候选范围记录：统一 Godot 虚拟路径构造为 `/`，并重新做运行时加载验证；本轮未修改代码，未验证修复效果。

### 检查面 3: eng/zhs localization token 覆盖
- 优先级: P1
- 结论: `eng/powers.json` 与 `zhs/powers.json` 均覆盖 9 个目标 Power token 的 `title`、`description`、`smartDescription`，共 27 个字段/语言；两文件各有 198 个键，键集合相等。该结论是 token 覆盖检查，不等于术语质量或视觉显示验收。
- 绝对路径与行号:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json:173-199`：9 个目标 token 的三字段连续条目。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\zhs\powers.json:173-199`：对应中文条目。
  - 目标 token 为 `VOID_SERPENT_STANCE_POWER`、`DEMON_REAPER_STANCE_POWER`、`ECHO_CELESTIAL_STANCE_POWER`、`VOID_FORM_EFFECT_POWER`、`SERPENT_FORM_POWER`、`DEMON_FORM_POWER`、`REAPER_FORM_EFFECT_POWER`、`ECHO_FORM_EFFECT_POWER`、`CELESTIAL_FORM_POWER`。
- 触发条件: 读取两份 JSON，枚举 9 个 token 与 `title`、`description`、`smartDescription` 字段，并比较完整键集合。
- 命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json' -Raw | ConvertFrom-Json`、对应 `zhs` 文件，以及本轮使用的 PowerShell token/键集合检查脚本。
- 证据: 检查输出为 `eng: keys=198; missing=`、`zhs: keys=198; missing=`、`key-counts: eng=198 zhs=198`、`key-set-equal=True`。按 `rg -n`，英文本与中文本的目标条目分别位于 173-199 行。
- 术语边界: `G:\omp works\Sts\sts2-spire1\docs\terminology-glossary.md` 不存在（只读检查结果 `glossary=missing`）。因此本轮仅确认字段/键覆盖，不依据记忆判定中文术语是否符合仓库权威术语。
- 最小修复范围: localization 覆盖层面无已确认缺项；若后续发现术语问题，应先补充或定位仓库权威术语来源，再作定向修订。本轮未修改 localization。

### 检查面 4: headless missing image 的证据归因
- 优先级: P0
- 结论: 当前证据不能唯一归因。日志同时显示两类事实：运行时 Power 路径含 Windows 反斜杠；PCK 目录中没有六个隐藏 effect Power 的对应 `*.png.import` 条目。因此缺图可能涉及 PCK 缺资源、Godot 虚拟路径分隔符不一致，或二者叠加；本轮不猜测唯一根因。
- 绝对路径与行号:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r10-20261001-03\appdata\SlayTheSpire2\logs\godot.log:395-415`：日志中的缺图请求均形如 `res://Spire1\images\...`；通用 Power 缺图在 401-409 行，六个目标 effect 缺图在 410-415 行。
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StringExtensions.cs:31-46`：通用 Power 与 big Power 路径由 `Path.Join` 构造。
  - `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck`：上一检查面的只读目录解析未找到 `celestial_form_power.png.import`、`demon_form_power.png.import`、`echo_form_effect_power.png.import`、`reaper_form_effect_power.png.import`、`serpent_form_power.png.import`、`void_form_effect_power.png.import` 对应条目。
  - 六个 effect 的可见性源码证据：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\CelestialFormPower.cs:30`、`DemonFormPower.cs:44`、`EchoFormEffectPower.cs:29`、`ReaperFormEffectPower.cs:27`、`SerpentFormPower.cs:76`、`VoidFormEffectPower.cs:35` 均为 `IsVisibleInternal => false`。
- 触发条件: 将 headless 日志的实际请求字符串与 PCK 条目、路径构造源码和六个 effect 的可见性声明交叉核对。
- 命令: `Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r10-20261001-03\appdata\SlayTheSpire2\logs\godot.log'`，并使用上一检查面的只读 PCK 目录解析脚本按六个 effect 文件名筛选；源码使用 `rg -n --no-heading 'IsVisibleInternal|class ...' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms'`。
- 证据边界:
  - 六个 effect 缺图日志与 PCK 缺对应资源相互支持“当前包内未发现这些独立资源”的事实，但这些 effect 被声明为不可见，不能仅凭缺图日志断言用户 UI 必须显示它们。
  - 反斜杠路径与 PCK 正斜杠路径并存，且 Windows 版 Godot 的容忍行为未在本轮通过启动游戏或运行时实验验证。
  - `calm_power`、`wrath_power`、`divinity_power` 载体条目已在 PCK 中确认，但本次日志没有对应三种载体的缺图行；这既不是视觉加载成功证据，也不是实机验收。
- 最小修复范围: 只记录候选，不执行：先统一 Godot 虚拟路径分隔符并复核加载；对于六个隐藏 effect，按产品决策选择“补齐普通与 `big` 资源并重打 PCK”或“维持隐藏并处理通用 icon fallback 日志”。两条均未在本轮验证。

### 检查面 5: 最小修复范围（仅报告，不执行）
- 优先级: P1
- 结论: 本轮没有已验证的代码修复；最小候选范围分为两条独立决策线。
- 绝对路径与行号:
  - 路径分隔符线: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StringExtensions.cs:31-46`，将 Godot `res://` 虚拟路径构造统一为 `/`，并重新做只读/运行时加载验证（本轮不执行）。
  - 载体路径基线: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:40-49` 与 PCK 中已确认的三组 `calm/wrath/divinity` 普通及 `big` 条目。
  - effect 资源线: 六个 effect 类的不可见声明见 `CelestialFormPower.cs:30`、`DemonFormPower.cs:44`、`EchoFormEffectPower.cs:29`、`ReaperFormEffectPower.cs:27`、`SerpentFormPower.cs:76`、`VoidFormEffectPower.cs:35`；对应 PCK 缺条目证据见检查面 1 和 4。
- 触发条件: 根据检查面 1-4 的源码、PCK 目录、localization 和 headless 日志证据，划定不超出证据边界的候选范围。
- 命令: 本节复用检查面 1-4 的只读命令；没有执行构建、部署、游戏启动或产品文件修改。
- 最小候选范围:
  1. 统一 `res://` 路径分隔符为正斜杠，之后在允许的测试环境中复核 ResourceLoader 结果；此处只报告，不实施。
  2. 若产品要求隐藏 effect 也拥有独立资源，则为六个 effect 各补普通与 `big` 资源、重打 PCK，并复核加载；当前不能把该要求当作已确认需求。
  3. 若产品决策是 effect 保持隐藏，则不凭缺图日志补资源；应单独确认通用 `power.png`/`big/power.png` fallback 的预期行为，并处理或接受相关日志。该行为本轮未验证。
- 证据边界: 上述均为最小候选修复范围，不是已完成修复；本轮唯一写入是本报告文件。

## 审查收束
- 检查面 1-5 均已完成只读记录。
- 已确认: PCK 载体条目、源码路径差异、双语 token 覆盖、headless 缺图现象及其不可唯一归因边界。
- 未确认: 真实游戏视觉显示、Godot 在目标环境对反斜杠路径的实际容忍结果、六个隐藏 effect 是否按产品意图需要独立 icon。
- 本轮禁止项均未执行: 未修改代码或其它产品文件，未构建，未部署，未启动游戏，未触碰 Steam 安装或共享 `mod_configs`。
