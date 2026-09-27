# Spire1 发布门禁 - 发布前检查清单（release gates）

> 目的：把历史上"硬引用 AutoAnthony 却没在 manifest 声明 + 用不可证伪的字符串扫描核验"这类疏漏，
> 换成**结构性、可证伪**的发布门禁。所有断言都对**编译后字节的 ECMA-335 元数据表**做解析
> （AssemblyRef 表 0x23、TypeDef 表 0x02），而非对 DLL 做 "字符串 contains 类型名"。
> `#if` 与 `#else` 两分支都定义同名类型时，字符串扫描两分支都命中、不可证伪；读引用表/类型表则是硬证据。

工具位置：`tools/build-gates/`（在**仓库根**下，不在 `mod/` 树内，故不会被 `Spire1.csproj` 的默认 glob 编进 `Spire1.dll`）。

- `Spire1ReleaseGate.csproj` / `Program.cs` / `PeMetadataReader.cs` / `GateConfig.cs` —— 独立控制台工具（零 NuGet 依赖，纯 BCL 手写元数据解析，离线可构建；**不依赖 ilspycmd**，本机也确认 ilspycmd 未安装）。
- `gate-config.json` —— **单一事实源**：两张黑名单表 + manifest 一致性配置。改门禁只改这个文件。
- `run-release-gates.ps1` —— 一键包装（先 build 工具再校验，透传退出码）。
- `BuildGates.targets` —— 可选的 MSBuild 接线（默认不介入日常构建；见下）。

---

## 三道门禁

### #1 AssemblyRef 结构门禁（`assemblyref`）
解析 `Spire1.dll` 的 AssemblyRef 表（0x23），断言其中**不含**一组"禁止硬引用的运行期可选 mod 程序集"：
`AutoAnthony`、`AutoAnthonyWatcher`、`Watcher`、`DirectConnectIP`、`ActsFromThePast`（`BaseLib` 是允许的硬依赖，不在此表）。
命中即打印命中的 AssemblyRef 名并以退出码 2 失败。
> 这是编译器实际写进引用表的硬依赖证据：硬引用一个 mod ⇒ 表里必有它的行；纯反射运行期解析 ⇒ 表里没有它的行。无法被 `#if` 两分支同名类型欺骗。

### #2 manifest / 二进制依赖一致性门禁（`manifest`）
从 AssemblyRef 中挑出"mod 程序集"（排除 `System.*`/`Godot*`/`sts2`/`0Harmony`/`HarmonyLib` 等前缀与精确名后剩余的），
断言它们都在 `mod/Spire1.json` 的 `dependencies[].id` 里声明。有引用但未声明 ⇒ 退出码 2 并列出差异。
> 这正是历史 AutoAnthony 疏漏的直接检测：硬引用了它却没在 manifest 声明依赖，玩家侧不会自动装上被引用的 mod。

### #3 条件编译符号产物门禁 / TypeDef 黑名单（`typedef`）
解析 TypeDef 表（0x02），断言**不含**下列实验/桥接/held-back/Debug 类型，按 `命名空间.类名` **精确匹配**（不做子串扫描）。
另支持"整命名空间禁含"（前缀以 `.` 收尾，避免同前缀命名空间误伤）。命中即退出码 2。

条件编译符号 → 禁含类型对照（A 类盲区全集）：

| 符号 | 源文件 | 禁含的 TypeDef |
|---|---|---|
| `SPIRE1_MENU_PERFORMANCE_PROBE` | Experimental/MenuPerformance/*.cs | `...Experimental.MenuPerformance.CharacterSelectBackgroundPause`、`...ViolaPortraitLogCompat`（并由整命名空间 `Spire1.Spire1Code.Experimental` 兜底） |
| `SPIRE1_FORM_MOD` | Forms/*.cs（10 类） | `...Forms.CelestialFormPower` … `...Forms.VoidSerpentStancePower`（并由整命名空间 `Spire1.Spire1Code.Forms` 兜底） |
| `SPIRE1_AUTOANTHONY` | Interop/AutoAnthonyCompatBridge.cs | 由门禁 #1 断言 **AutoAnthony AssemblyRef 禁含** |
| `DEBUG` | Patches/DebugCard/DebugRelicInjectPatch.cs | `...Patches.DebugCardInjectPatch`、`...Patches.DebugRelicInjectPatch`（Release 天然排除，门禁二次确认） |
| `IncludeHeldBackLayers` | csproj（控制 Compile Remove） | `...Interop.AftpFireFlyPerfCompat`、`...Interop.AftpCardStateCompat` |

> **为什么必须按全名精确匹配**：`Spire1.Spire1Code.Powers` 下存在**合法**的 `DevaFormPower`/`WraithFormPower`/`StancePower`。
> 若用 "FormPower"/"StancePower" 子串扫描会误伤这些正常类型——这正是"字符串扫描不可证伪/易误报"的坑。

---

## 如何在 interop-refs 缺席下做发布构建

`Spire1.csproj` 只有当 `../.tmp/interop-refs/AutoAnthony.dll` 存在时才定义 `SPIRE1_AUTOANTHONY` 并硬引用 AutoAnthony。
**发布构建应在该 dll 缺席的环境下进行**，这样 AutoAnthony 桥接走空壳分支、不产生 AutoAnthony AssemblyRef：

```
# 确保 .tmp/interop-refs/AutoAnthony.dll 不存在（发布环境本就不该有它）
dotnet build mod/Spire1.csproj -c Release -p:CopyToModsFolderOnBuild=false -p:ModsPath="G:/omp works/.tmp/mods-scratch/"
```

> 注：另一 worker 正在把 `SPIRE1_AUTOANTHONY` 桥接改为**纯反射**、彻底删除对 AutoAnthony 的编译期引用。
> 改完后即便 interop-refs 存在，发布件也不应再含 AutoAnthony AssemblyRef——门禁 #1 会持续把关。

---

## 如何跑门禁

一键（先 build 工具，再校验默认 Release 输出）：
```
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build-gates/run-release-gates.ps1
```
指定 DLL / 子集 / 另存 JSON：
```
powershell ... run-release-gates.ps1 -Dll "<path>\Spire1.dll" -Gates "assemblyref,manifest,typedef" -Json "<out.json>"
```
直接调工具（CI 里）：
```
dotnet tools/build-gates/bin/Release/net9.0/Spire1ReleaseGate.dll --dll "<Spire1.dll>" --config tools/build-gates/gate-config.json
```

**退出码约定**（供 CI / 发布脚本判定）：
- `0` = 全部选定门禁通过
- `2` = 至少一道门禁失败（命中禁止 AssemblyRef / TypeDef，或 manifest 不一致）
- `3` = 用法/输入错误（缺参、DLL/config 找不到、无法解析）

---

## 门禁失败如何解读

| 失败 | 含义 | 处理 |
|---|---|---|
| `assemblyref-forbidden` 命中 AutoAnthony | 发布件硬引用了运行期可选 mod | 在无 interop-refs 的环境重新发布构建；或改纯反射不硬引用 |
| `manifest-consistency` "X 未声明" | 二进制引用了 mod 程序集 X 但 manifest 没声明依赖 | 或在 `Spire1.json` dependencies 补 X；或去掉对 X 的硬引用 |
| `typedef-forbidden` 命中 Forms/Experimental/Aftp*/Debug* | 实验/形态/held-back/Debug 代码被编进了发布件 | 确认对应 `#if` 符号未定义、`Compile Remove` 未被 `IncludeHeldBackLayers=true` 打开；用 Release 干净构建 |

---

## 可选：接线进 csproj（默认不接，交主会话决定）

本 worker **默认不改 `Spire1.csproj`**，只交付独立脚本，避免与正在改桥接块的另一 worker 冲突。
若要让发布校验构建自动跑门禁，在 `Spire1.csproj` 顶部加**一行**（仅此一行被授权）：

```xml
<Import Project="$(MSBuildThisFileDirectory)../tools/build-gates/BuildGates.targets"
        Condition="'$(RunReleaseGates)' == 'true'" />
```

然后：
```
dotnet build mod/Spire1.csproj -c Release -p:RunReleaseGates=true -p:CopyToModsFolderOnBuild=false
```
- 不传 `-p:RunReleaseGates=true` ⇒ 该 Import 不加载、Target 不挂，**日常构建零开销**。
- 门禁绑定在 `CopyToModsFolderOnBuild` **之前**，失败会在部署到测试副本之前中止。

---

## 实测证据（结构可证伪，非记忆）

对当前 `mod/.godot/mono/temp/bin/Release/Spire1.dll`（AssemblyRef 13 个、TypeDef 756 个）实跑：
- 门禁 #1：命中 `AutoAnthony` → FAIL（证明当前发布件确实硬引用了 AutoAnthony）。
- 门禁 #2：`AutoAnthony` 未在 manifest 声明 → FAIL（manifest 只有 BaseLib）。
- 门禁 #3：Forms/Experimental/Aftp*/Debug* 均不在当前件 → PASS。
- 阳性对照：把黑名单换成一个**已知存在**的类型 `...Interop.AutoAnthonyCompatBridge` → 门禁 #3 立即 FAIL；把禁止 AssemblyRef 换成不存在的名字 → 门禁 #1 PASS。**证明门禁可证伪、非假通过。**
