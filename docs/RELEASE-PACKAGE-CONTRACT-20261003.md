# Spire1 发布包契约（2026-10-03）

## 决策

下一次 Spire1 发布采用**源码驱动的干净重建**，不直接在现有 `dist`、`workshop` 或源资产目录上做盲删。

原因：源资产可能通过动态路径、旧存档兼容、Godot `res://` 路径或运行历史读取；直接删除源文件无法证明没有反射/动态消费者。重建脚本先生成隔离 staging，再按代码可证明的 allowlist 打包，最后生成清单、大小和 SHA256。只有 staging 通过门禁和测试后，才允许显式 promote 到 Workshop payload。

## 正式 payload

正式 Workshop payload 只允许：

- `Spire1.dll`
- `Spire1.json`
- `Spire1.pck`

以下不是正式 payload：

- `Spire1.pdb`、`Spire1.deps.json`
- `Spire1.pck.sha256`（仅构建证据，保留在 staging/evidence）
- `dist/deprecated/**`
- `dist/friends-pack/**`
- 旧 zip、beta zip、探针输出、日志、共享配置

现有历史目录保留作证据，不会被脚本自动纳入重建。脚本默认只写 `G:\omp works\.tmp\`；显式 `-Promote` 才能写 Workshop payload，且绝不写 Steam 安装或共享 `mod_configs`。

## 资产 allowlist

### 保留

- `mod_image.png`
- `localization/**` 的全部语言 JSON（语言文件是按表加载，不能由单个 token 的静态 grep 证明安全删除）
- `images/run_history/**`（`Spire1Encounter` 以动态 encounter id 组成路径，且旧日志/运行历史已证明会访问）
- `images/charui/big_energy.png`、`images/charui/text_energy.png`（`Spire1RelicPool` 显式引用）
- 卡牌小图/大图：`Spire1Card`/`Spire1Curse` 当前具体类按 ID 约定需要的文件，及 `card.png` fallback
- 遗物小图、outline、大图：当前具体 `Spire1Relic` 类按 ID 约定需要的文件，及 `relic.png`、`relic_outline.png`、`big/relic.png` fallback
- 药水图、outline：当前具体 `Spire1Potion` 类按 ID 约定需要的文件，及 `potion.png`、`outline/potion.png` fallback
- `images/powers/**` 当前文件（除已确认无产品类/引用的 `omega_power.png` 及对应大图），以及 `power.png` fallback；形态图由 `WatcherFormStancePower` 显式引用

### 下一次重建默认排除

以下是源码驱动审计中已确认不是当前运行时路径所需的历史/模板资源：

- `images/charui/char_select_char_name.png`
- `images/charui/char_select_char_name_locked.png`
- `images/charui/character_icon_char_name.png`
- `images/charui/map_marker_char_name.png`
- `images/card_portraits/**` 中不对应当前 `Spire1Card`/`Spire1Curse` 具体类且不是 fallback 的文件（旧的二代卡、遗物、药水和 beta 卡面混入）
- `images/card_portraits/big/**` 中同样不对应当前具体类且不是 fallback 的文件
- `images/relics/**` 中不对应当前 `Spire1Relic` 具体类且不是 fallback 的文件
- `images/relics/big/**` 中不对应当前 `Spire1Relic` 具体类且不是 fallback 的文件
- `images/potions/**` 中不对应当前 `Spire1Potion` 具体类且不是 fallback 的文件
- `images/potions/outline/**` 中不对应当前 `Spire1Potion` 具体类且不是 fallback 的文件
- `images/powers/omega_power.png`、`images/powers/big/omega_power.png`

`images/run_history/**` 不在默认删除列表内；它们看似占位图，但动态运行历史路径使其不能仅凭“没有字面引用”删除。

## PCK 包内门禁

PNG 不会以原始 PNG 路径直接进入 PCK。`BSchneppe.StS2.PckPacker 0.1.1` 会把每个保留 PNG 转成：

- `.godot/imported/<filename>-<md5(res://Spire1/<relative-path>)>.ctex`
- `Spire1/<relative-path>.import`

保留的 JSON 以 `Spire1/<relative-path>` 进入 PCK。`tools/release/Verify-Spire1Pck.ps1` 必须逐项核对：

- Godot 4.5.1 format v3、112 字节头、`PACK_REL_FILEBASE=2`、32 字节文件区对齐；
- PCK 内路径集合恰好由 `asset-manifest.json` 推导得到；
- 不存在被排除源资产对应的 `.ctex`、`.import` 或 JSON；
- 每个目录项的 MD5 与实际 PCK 数据一致；
- 每个 `.ctex` 以 `GST2` 开头。

结构门禁失败时禁止 `-Promote`。

## 重建流程

入口：`tools/release/Build-Spire1Release.ps1`

1. 检查源目录、路径边界和 reparse point；拒绝 Steam 路径、共享 `mod_configs`、C: 写入和工作区外 staging。
2. 从当前 C# 具体模型类型生成资产 allowlist。
3. 在 `.tmp` 创建全新的 `Spire1` staging 资产树；不修改源 `mod/Spire1`。
4. 复制 manifest，验证根层 `has_dll=true`、`has_pck=true`。
5. 用当前项目 Release 编译 DLL，命令强制 `CopyToModsFolderOnBuild=false`。
6. 用已锁定的 `BSchneppe.StS2.PckPacker 0.1.1` 对 staging 资产树生成 PCK；当前资产只有 PNG/JSON，未使用 Godot 场景编译能力。
7. 生成 payload manifest、文件清单、SHA256 和 PCK digest；运行 PCK 包内结构门禁；正式 payload 不带 PDB、deps、digest。
8. 运行现有 AssemblyRef/manifest/TypeDef 门禁；再运行 payload 结构门禁。
9. 只部署测试副本并实际启动验证：加载、设置页、三角色、旧存档、运行历史；游戏无法自动驱动的视觉项必须明确交给真人，不得伪称通过。
10. 验证通过后才显式 `-Promote`，并再次确认 Workshop 目录只剩三件 payload 文件。

## 未验证边界

- 当前工作站没有 `C:\megadot\MegaDot_v4.5.1-stable_mono_win64.exe`，因此本轮不宣称完成 MegaDot/Godot export-pack 实机验证。
- PCK packer 路径可用于当前 PNG/JSON 资产，但最终发布前仍需在测试副本确认实际加载和旧存档运行历史。
- 现有工作树仍有并发代码修复；所有代码监督结论为 `SUPERVISION_PASS` 前，不把 staging 视为可发布件。
