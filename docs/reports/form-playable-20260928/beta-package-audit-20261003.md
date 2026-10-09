# Spire1 beta 包审查 - 2026-10-03

范围: `G:\omp works\Sts\sts2-spire1` 的 beta 发布包与当前 Release 产物。只读审查;未构建、未测试、未部署、未启动游戏、未委派。

## 已确认

### P0-1 现有 `dist/friends-pack` 不是可交付 beta:包内版本与当前 Release 不一致

- 包内 manifest `G:\omp works\Sts\sts2-spire1\dist\friends-pack\mods\Spire1\Spire1.json:6` 为 `"version": "0.9.2"`;当前源码 manifest `G:\omp works\Sts\sts2-spire1\mod\Spire1.json:6` 为 `"version": "1.2.3"`。
- 包内 DLL `...\dist\friends-pack\mods\Spire1\Spire1.dll` 大小 `1021952`,时间 `2026-08-29 19:06:35`;当前 Release DLL `...\mod\.godot\mono\temp\bin\Release\Spire1.dll` 大小 `781312`,时间 `2026-10-03 07:55:18`,SHA256 `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`。
- 触发条件: 直接把 `dist/friends-pack.zip` 或 `dist/friends-pack/` 发给朋友。
- 宣称/契约: `dist\REBUILD-PENDING.md:3-7` 已记录 friends-pack 构建于 2026-08-29,Spire1 落后 `0.9.2 -> 1.1.0`;当前源码已到 `1.2.3`。
- 当前控制流: 现有包是旧产物,当前 Release 是 2026-10-03 新产物;两者不能混用。
- 可复现命令:
  `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\dist\friends-pack\mods\Spire1\Spire1.json'`
  `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json'`
  `Get-Item -LiteralPath 'G:\omp works\Sts\sts2-spire1\dist\friends-pack\mods\Spire1\Spire1.dll','G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll' | Select-Object FullName,Length,LastWriteTime`
- 最小修复范围: 不使用旧 `dist/friends-pack`;以当前 Release 产物重建 beta 包并同步版本。
- 尚缺的实机证据: 无;这是文件/manifest 静态不一致。

### P0-2 既有门禁报告未绑定当前 Release DLL 的最终字节

- `docs\reports\form-playable-20260928\release-gates-current-20261003.md:6` 指向当前 DLL 路径,但其引用的 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json` 时间为 `2026-10-03 05:32:45`,而当前 DLL 时间为 `2026-10-03 07:55:18`。
- 触发条件: 将“门禁 PASS 报告”当成对 07:55 后 DLL 的证明。
- 宣称/契约: 报告 `:12-16` 宣称三项门禁全部 PASS,但未记录 DLL SHA256。
- 当前控制流: 路径相同、字节已变;旧 JSON 只证明更早快照。
- 可复现命令:
  `Get-Item -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll','G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json' | Select-Object FullName,Length,LastWriteTime`
- 最小修复范围: 在冻结后的 beta DLL 上重跑门禁,并在 README/发布记录中写明 DLL SHA256。
- 尚缺的实机证据: 无;属于证据身份绑定问题。

### P0-3 本次审查实测: 包内旧 DLL 与当前 Release DLL 均只硬引用 BaseLib

- 本次对两个 DLL 运行只读门禁 `tools\build-gates\bin\Release\net9.0\Spire1ReleaseGate.exe`:
  - 包内 DLL: AssemblyRef 10, TypeDef 1560, `assemblyref-forbidden` / `manifest-consistency` / `typedef-forbidden` 全 PASS, 退出码 0。
  - 当前 Release DLL: AssemblyRef 15, TypeDef 972, 三项全 PASS, 退出码 0。
- 触发条件: 误以为“旧包已过门禁”就等于“旧包内容与当前版本等价”。
- 当前控制流: 门禁只证明依赖边界,不证明版本/内容一致。
- 可复现命令:
  `& 'G:\omp works\Sts\sts2-spire1\tools\build-gates\bin\Release\net9.0\Spire1ReleaseGate.exe' --dll '<dll>' --manifest '<Spire1.json>' --json '<out.json>'`
- 最小修复范围: 门禁针对最终冻结产物运行;版本一致性另用 SHA256/时间戳比较。
- 尚缺的实机证据: 无;静态元数据结论。
- 合规说明(真实状态): 本次只读门禁与读取脚本在 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\` 生成了四个非报告文件: `beta-audit-gate-pack-20261003.json`, `beta-audit-gate-current-20261003.json`, `beta-audit-read-20261003.ps1`, `beta-audit-read2-20261003.ps1`。这超出“只写唯一报告文件”的字面约束;未修改产品代码、构建产物、部署、游戏或共享配置。尝试用 `Remove-Item` 清理时被本工具策略拒绝,因此这四个文件仍留在 G: 临时区,未擅自改用其它删除手段。

### P1-1 包内 README 的版本、内容描述与边界声明已过时

- README `...\dist\friends-pack\README-安装说明.txt:2` 写 `Build date: 2026-08-29`;`:9` 写 `306 card classes`;`:21` 写 `BaseLib v3.4.5 or newer`;`:34-43` 声明多人修复仍需真实双人局。
- 当前 manifest `...\mod\Spire1.json:5` 描述 `222 faithful cards, 24 relics, 6 unique StS1 events`;`:6` 版本 `1.2.3`;`:13` 要求 `BaseLib min_version 3.4.5`。
- 触发条件: 朋友按 README 的旧版本/旧内容计数安装。
- 宣称/契约: README 是包内唯一安装说明,必须与包内 manifest 和产物版本一致。
- 当前控制流: README 与包内旧 manifest(0.9.2)自洽,但与当前源码 manifest(1.2.3)不一致。
- 可复现命令:
  `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\dist\friends-pack\README-安装说明.txt'`
  `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1.json'`
- 最小修复范围: 重建 beta 包时重写 README,写明精确 BaseLib 最低版本、Spire1 版本、内容计数、已知未验证边界(多人双人实测、UI/视觉、长战斗、存档/重连、性能)。
- 尚缺的实机证据: README 声称的多人修复仍需真实双人局验证。

### P1-2 当前 Release 目录缺 `Spire1.json`,直接照搬会得到不可安装包

- `...\mod\.godot\mono\temp\bin\Release\` 仅有 `Spire1.dll`, `Spire1.pck`, `Spire1.pck.sha256`, `Spire1.pdb`, `Spire1.deps.json`;`Spire1.json` 不存在。
- `mod\Spire1.csproj:321-324` 的部署目标是把 `$(TargetPath)`、`$(AssemblyName).json`、`$(TargetName).pdb` 复制到 mods 目录,说明 manifest 是部署阶段从源码树复制,而不是 Release 构建目录自带。
- 触发条件: 直接把 Release 目录内容打包为 beta 包。
- 宣称/契约: `Spire1.json` 是 ModLoader 读取的 manifest,缺它无法安装/加载。
- 当前控制流: 正确包必须显式包含源码树 `mod\Spire1.json`。
- 可复现命令:
  `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release' -Force | Select-Object Name`
  `Test-Path -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.json'`
- 最小修复范围: 打包步骤显式复制 `mod\Spire1.json` 到 `mods\Spire1\Spire1.json`。
- 尚缺的实机证据: 无;静态结构结论。

### P1-3 当前 Release 目录缺 `character.txt`;若有意三职业全开,应显式提供

- `...\mod\Spire1Code\Config\CharacterGate.cs:22-30` 从 DLL 同目录读 `character.txt`;缺失或无法解析时 `:24,47-49` 默认三职业全开。
- 当前 Release 目录不含 `character.txt`;旧 friends-pack 含 `character.txt` 内容 `all`(`61-6C-6C-0A`)。
- 触发条件: 用当前 Release 构造包时未复制 `character.txt`。
- 宣称/契约: 缺文件时行为等价于 `all`,但这是隐式默认;README 不应依赖“缺失即全开”的未声明行为。
- 当前控制流: 两种包结构行为相同,但显式提供可避免朋友/后续打包者误判。
- 可复现命令:
  `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release' -Force | Select-Object Name`
  `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Config\CharacterGate.cs'`
- 最小修复范围: 若 beta 仍要三职业全开,在 `mods\Spire1\character.txt` 写入 `all`,并在 README 说明其作用。
- 尚缺的实机证据: 无;源码与文件结构静态结论。

### P1-4 包内未带入 Steam 路径、共享 mod_configs 或其它会话未确认产物

- 本次扫描 `dist\friends-pack\` 与 `dist\friends-pack.zip` 内文本,未发现 `G:\`, `C:\`, `E:\`, `steamapps`, `mod_configs`, `AppData`, `Roaming`, `steam_api64`, `steam_appid`, `.tmp` 或本会话 `form-playable-20260928-01a0e7ad` 字样。
- 包内仅含 `mods\Spire1\{Spire1.dll,Spire1.pck,Spire1.json,character.txt}`、`mods\ActsFromThePast\{dll,pck,json}` 与 `README-安装说明.txt`;未发现 `.pdb`, `.deps.json`, `.log`, `.sha256`, config/ini。
- 触发条件: 若未来重建时直接复制 `mods/` 工作目录而非白名单文件,可能引入会话产物。
- 宣称/契约: 朋友包应只含运行所需文件,不带开发机路径/共享配置。
- 当前控制流: 现有包在这方面干净,但重建脚本必须维持白名单复制。
- 可复现命令:
  `Get-ChildItem -LiteralPath 'G:\omp works\Sts\sts2-spire1\dist\friends-pack' -Recurse -Force | Select-Object FullName,Length,LastWriteTime`
- 最小修复范围: 重建脚本显式白名单复制,打包后对 zip 再做一次字符串扫描。
- 尚缺的实机证据: 无;静态扫描结论。

### P2-1 最小安全 beta 包结构

- 必需: `mods\Spire1\Spire1.json`(来自 `mod\Spire1.json`,版本 1.2.3,只声明 BaseLib >= 3.4.5), `mods\Spire1\Spire1.dll`, `mods\Spire1\Spire1.pck`, `README-安装说明.txt`。
- 可选但建议显式: `mods\Spire1\character.txt`(内容 `all`,若确实三职业全开)。
- 不应包含: `Spire1.pdb`, `Spire1.deps.json`, `Spire1.pck.sha256`, `mod_configs`, `*.log`, `steam_api64.dll`, `steam_appid.txt`, Steam 路径配置,其它会话产物。
- 安装前置: BaseLib 最低 `3.4.5`;游戏 `0.111.0`;Watcher/AutoAnthony/AutoAnthonyWatcher 均不是 Spire1 的 manifest 硬前置,而是可选运行时桥接。
- README 必须明确: Spire1 `1.2.3`;BaseLib 最低 `3.4.5`;游戏 `0.111.0`;内容计数与 `mod\Spire1.json:5` 一致;Watcher/AutoAnthony/AutoAnthonyWatcher 可选;未验证边界包括多人双人实测、可见 UI/视觉、长战斗、战中存档/读档、重连、性能、完整平衡;Spire1.dll 与 Spire1.pck 的 SHA256。
- 尚缺的实机证据: 未启动游戏验证此结构可加载;按本轮约束不做。

## 进行中

- 已排除: 包内不存在 Steam 路径、共享 mod_configs、AppData、steam_api64/steam_appid、`.tmp` 或本会话路径字符串。
- 已排除: 包内不存在 PDB、deps.json、log、sha256、config/ini 等会话产物(仅 `character.txt` 为预期内容)。
- 已排除: 包内旧 DLL 与当前 Release DLL 都未把 Watcher/AutoAnthony/AutoAnthonyWatcher 写成 AssemblyRef 硬前置。
- 待复核: `workshop\content\Spire1\Spire1.json`(版本 1.2.3)与当前 `mod\Spire1.json` 一致,但其 DLL/PCK 为 2026-09-30 产物,不等同当前 Release;不应作为 beta 源。

## 未知

- 未确认本会话实际解析模型与 provider route;请求文本指定 `global:deepseek-v4.1-flash` / `wb2api via local gateway`,但本会话元数据未暴露可核验字段,故不宣称已验证。
- 未完成: 当前 Release PCK 与旧包 PCK 的完整内容差异比对。
- 未完成: 是否存在 `dist/` 之外的其它“当前 beta 包”候选(workshop 输出已确认版本一致但产物时间较旧)。
- 未验证: 按本轮约束,未启动游戏,未做任何实机加载/多人/UI/存档测试。
