# Contributing to Spire1 / 参与贡献

Spire1 ports the original Slay the Spire characters (Ironclad / Silent /
Defect), their cards, relics, potions and six StS1 events to Slay the Spire 2
through [BaseLib](https://github.com/Alchyr/BaseLib-StS2).

Spire1 将《杀戮尖塔》初代的三名角色（铁甲战士 / 静默猎手 / 故障机器人）、
卡牌、遗物、药水与 6 个一代独有事件移植到《杀戮尖塔 2》，运行时依赖 BaseLib。

This document covers the build, the conventions a patch must follow, and the
review/PR flow. The Chinese summary of each section follows the English one.
下文每节先英文、后中文摘要；术语保留英文原名。

- Repository: https://github.com/Twelve-eight/sts2-spire1
- Workshop item: https://steamcommunity.com/sharedfiles/filedetails/?id=3799031900
- Issues: https://github.com/Twelve-eight/sts2-spire1/issues

---

## 1. Prerequisites / 环境要求

| Requirement | Verified version / note |
|---|---|
| Slay the Spire 2 | **0.111.x** public beta (Steam app id `2868840`); owned and installed locally |
| BaseLib | **3.4.5** (runtime dependency, declared in `mod/Spire1.json`) |
| .NET SDK | **9.0.x** (the project targets `net9.0`) |
| Godot | **4.5.1 mono** (`Godot.NET.Sdk/4.5.1`) — only needed to export a `.pck` |
| PowerShell | 5.1 or 7.x for the scripts under `tools/` |
| OS | Windows is the verified platform; the csproj has Linux/macOS branches for path discovery |

One non-obvious requirement: the **game assemblies**. The build references
`sts2.dll` and `0Harmony.dll` from the game's data directory; they are not
redistributable and are not in this repository.

`ConsoleDisplay.cs` (the console display-name helper) is vendored at
`shared/ModConventions/ConsoleDisplay.cs`, so no extra checkout is needed. To
build against a different copy, pass
`-p:SharedModConventions=/absolute/path/to/ModConventions`.

中文：需要正版 StS2 0.111.x（Steam appid 2868840）、BaseLib 3.4.5、.NET 9 SDK、
以及导出 pck 时才需要的 Godot 4.5.1 mono。构建还需引用游戏数据目录下的
`sts2.dll` 与 `0Harmony.dll`（不可再分发，也不在本仓库内）。
`ConsoleDisplay.cs` 已 vendor 到本仓库的 `shared/ModConventions/`，无需额外克隆；
要换用别的副本，传 `-p:SharedModConventions=<目录>` 覆盖即可。

---

## 2. Build / 构建

Run every command from the repository root. Close the game first: it locks
`Spire1.dll` while running.
先关闭游戏，运行中的游戏会锁住 `Spire1.dll`。

### 2.1 Compile only (recommended for contributors) / 仅编译

```bash
dotnet build mod/Spire1.csproj -c Release \
  -p:Sts2Path="C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2" \
  -p:CopyToModsFolderOnBuild=false
```

`-p:CopyToModsFolderOnBuild=false` disables the deploy/copy targets and the
PCK packer, so nothing is written into a game installation. Point
`-p:Sts2Path=` at your own game root.
`-p:CopyToModsFolderOnBuild=false` 会关闭部署与 pck 打包，不会写入任何游戏目录；
`-p:Sts2Path=` 指向你自己的游戏根目录。

### 2.2 Deploy to a local test copy / 部署到测试副本

The default path discovery only accepts the maintainer's test copies and
**refuses to fall back to a Steam install**. `AssertModsPathIdentity` allows
only the deploy roots listed in its `ModsPath` allowlist inside
`mod/Spire1.csproj`. That guard is machine-specific; outside this workstation,
use the compile-only build above and copy `Spire1.dll` + `Spire1.json` into
your own `mods/Spire1/` manually.
默认的部署守卫只允许 `mod/Spire1.csproj` 里 `AssertModsPathIdentity` 白名单内的
部署根目录，并拒绝写入 Steam 安装目录。外部贡献者请用上面的“仅编译”方式，
手动复制产物到你自己的 `mods/Spire1/`。

### 2.3 Export a PCK / 导出 pck

```bash
dotnet publish mod/Spire1.csproj -c Release \
  -p:GodotPath="<path to Godot 4.5.1 mono executable>" \
  -p:Sts2Path="<path to Slay the Spire 2>"
```

`GodotPublish` runs Godot's `--export-pack`, verifies the produced PCK and its
digest, and fails closed on a stale or missing artifact. Release packaging is
maintainer-run (`tools/release/Build-Spire1Release.ps1`); do not commit
generated payloads.
`GodotPublish` 会调用 Godot 导出 pck、校验摘要，并在产物过期/缺失时失败关闭。
发布打包由维护者执行，PR 不要提交生成的 payload。

### 2.4 Release gates / 发布门禁

```bash
powershell -NoProfile -ExecutionPolicy Bypass \
  -File tools/build-gates/run-release-gates.ps1 \
  -Dll "mod/.godot/mono/temp/bin/Release/Spire1.dll" \
  -Gates "assemblyref,manifest,typedef"
```

The gates parse the compiled ECMA-335 metadata (AssemblyRef table 0x23,
TypeDef table 0x02) rather than scanning strings. They assert that the DLL has
no hard reference to optional runtime mods and that the manifest matches the
built payload. Exit codes: `0` pass, `2` gate failure, `3` usage error.
门禁读取编译后的 ECMA-335 元数据表（AssemblyRef 0x23 / TypeDef 0x02），
而不是字符串扫描；`0` 通过、`2` 门禁失败、`3` 用法错误。

### 2.5 NuGet cache note / NuGet 缓存注意

`NuGet.config` keeps `globalPackagesFolder` at the repo-local `.nuget/packages`
(relative to that file, and git-ignored), so a fresh clone restores without any
machine-specific edit. Set `NUGET_PACKAGES` if you want the cache elsewhere.
仓库里的 `NuGet.config` 把包缓存放在仓库内的 `.nuget/packages`（相对该文件，
不入库），换机器无需修改；想把缓存放到别处可设置 `NUGET_PACKAGES`。

---

## 3. Code conventions / 代码约定

### 3.1 Project rules / 项目规则

- Target framework `net9.0`; `Nullable` and `ImplicitUsings` are enabled.
- Root namespace `Spire1`; content lives under
  `Spire1.Spire1Code.{Cards,Relics,Powers,Potions,Character,Events,Interop,Patches,Config,...}`.
- Content IDs are `SPIRE1-<CLASS_NAME_UPPER_SNAKE>`; class name is the
  identity and drives the localization key. Do not rename a shipped class:
  old saves resolve models by ID.
- Every card extends `Spire1Card`, every relic extends `Spire1Relic`, and pool
  membership comes from `[Pool(...)]`, never manual registration.
- Card effects go through engine commands (`CommonActions`, `*Cmd`). Never
  mutate run/combat state directly.
- Numbers must be **vanilla StS1**: verify against `desktop-1.0.jar` /
  authoritative sources, and mark anything unconfirmed in a comment. Do not
  invent values.
- Optional mods (AutoAnthony, Acts from the Past, RitsuLib, ...) must be
  reached by reflection/string type names. A compile-time `Reference` to them
  is a release-gate failure. BaseLib is the only allowed runtime dependency.
- `Spire1Code/Forms/**` and the form-native smoke runner are excluded from the
  assembly on purpose; the Forms content lives in the separate `sts2-forms`
  repository. Do not re-add it here.

中文：类名即内容 ID（`SPIRE1-*`），改名会破坏旧存档；效果只能走引擎命令；
数值必须来自一代原版并注明依据；可选 mod 一律反射接入，禁止编译期引用；
Forms 源码已迁出本仓库，不要重新引入。

### 3.2 Localization / 本地化

- Every content class needs entries in **both**
  `mod/Spire1/localization/eng/*.json` and `.../zhs/*.json`.
  `Alchyr.Sts2.ModAnalyzers` fails the build with **STS001** when a key is
  missing.
- Use dynamic variables (`!D!`, `!B!`, `!M!`, `!Block!`, ...) instead of
  hard-coded numbers.
- Do **not** use StS1 markup: `[R]`/`[G]`/`[B]`/`[W]`, `#r`/`#y`, literal
  ` NL ` line breaks. They render as raw text or break BBCode parsing.
- Card keywords and colored spans are engine-defined terms; reuse the exact
  casing/spelling already present in the `eng` tables.

中文：每个内容类必须在 eng 与 zhs 两套表里同时有条目，否则 STS001 直接失败；
使用 `!D!` 等动态变量；禁止一代颜色码、能量标签与 ` NL ` 换行。

### 3.3 Comments and docs / 注释与文档

Comments should explain **why** (engine behaviour, a verified constraint, a
past incident), not restate the code. Design contracts go to `DEVELOP.md`;
dated contracts and the chronological log are internal records and are not
distributed with this repository. Update the relevant public document in the
same PR when behaviour changes.
注释解释“为什么”（引擎行为、已验证约束、历史事故），不复述代码。
行为变更时同步更新 `DEVELOP.md`（内部日志与历史契约不随仓库分发）。

---

## 4. Testing and evidence / 测试与证据

There is no unit-test project: this is a game mod, and most behaviour is only
observable in-game. The minimum bar for a PR is:

1. `dotnet build` exits `0` with no new warnings that you introduced.
2. Release gates pass (Section 2.4).
3. Localization scans pass (`node tools/check-loc-markup.mjs` if your change
   touches loc).
4. If the change affects gameplay, include a log excerpt from
   `%APPDATA%\SlayTheSpire2\logs\godot.log` showing a normal load and the
   exercised path. State the game version, mod version and BaseLib version.
5. If you could not run the game, say so explicitly. "Compiles" is not
   "verified"; do not describe a build as playable without a log.

中文：没有单元测试工程；最低门槛是编译 0 错误、发布门禁通过、本地化扫描通过。
涉及玩法时必须附 `godot.log` 证据并写明三个版本号；没实机跑过就明确说没跑过。

---

## 5. Pull requests / PR 流程

1. Fork, branch from `master`: `fix/<topic>` or `feat/<topic>`.
2. Keep one logical change per PR. Do not mix refactors, content and version
   bumps.
3. Do **not** bump `mod/Spire1.json` version or edit `workshop/workshop_upload.vdf`
   unless the PR is explicitly a release PR. Release PRs must keep manifest,
   Workshop changenote and payload hashes aligned.
4. Fill in `.github/PULL_REQUEST_TEMPLATE.md`, including the verification
   commands you actually ran.
5. Disclose AI assistance in the PR description if any (project policy).
6. There is **no public CI**. A GitHub runner cannot legally obtain `sts2.dll`,
   so workflows would be permanently red. Reviewers build locally; paste your
   local evidence instead.

中文：从 `master` 切分支，一个 PR 只做一件事；非发布 PR 不要改版本号或 VDF；
按模板填写并列出真实跑过的命令；如使用 AI 辅助请披露。
本仓库**不配置公开 CI**：GitHub runner 拿不到 `sts2.dll`，工作流只会长期变红，
因此以本地构建与日志作为证据。

### Commit messages / 提交信息

Use a scoped prefix consistent with history:
`fix(spire1): ...`, `feat(spire1): ...`, `docs: ...`, `release(spire1): ...`,
`workshop: ...`, `tools: ...`. Body explains the root cause and the evidence.
提交信息使用 `fix(spire1):` / `feat(spire1):` / `docs:` / `release(spire1):`
等前缀，正文写根因与证据。

---

## 6. Reporting bugs / 报告问题

Open an issue with the **Bug report** template and include:

- StS2 game version, Spire1 version (manifest or Workshop page), BaseLib
  version and where it came from (Workshop or GitHub release);
- reproduction steps and the character/act/card involved;
- `%APPDATA%\SlayTheSpire2\logs\godot.log` (attach the file or paste the
  `Spire1`/`Exception` lines);
- the full mod list, especially multiplayer mods and act mods.

For multiplayer divergence, also attach the `ritsulib_state_divergence_*`
bundle if RitsuLib produced one. Use the **Feature request** template for
content proposals; balance changes should cite the vanilla StS1 value.

中文：用 Bug 模板提交，附游戏/mod/BaseLib 三个版本、复现步骤、
`%APPDATA%\SlayTheSpire2\logs\godot.log` 与完整 mod 列表；联机分歧另附
RitsuLib 的 divergence 包。

---

## 7. Licensing of contributions / 贡献授权

By submitting code you agree it is licensed under the MIT License (see
`LICENSE`). Do not submit ripped game assets, extracted atlases, audio, or
text you do not have the right to redistribute. Assets converted from StS1 are
documented in `NOTICE` and are not MIT-licensed.
提交代码即视为同意以 MIT 授权（见 `LICENSE`）。不要提交无 redistribution
权利的游戏素材、图集、音频或文本；从一代转换的素材见 `NOTICE`，不适用 MIT。
