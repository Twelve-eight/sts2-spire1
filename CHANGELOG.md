# Changelog / 变更记录

本文件记录 Spire1 的版本历史。版本号以 `mod/Spire1.json` 为准，发布渠道为
Steam Workshop item [3799031900](https://steamcommunity.com/sharedfiles/filedetails/?id=3799031900)。
条目按版本倒序；`1.2.1` / `1.2.2` 只是元数据重传，没有代码或玩法变更。

Version numbers follow `mod/Spire1.json`. Workshop change notes are written in
Chinese and carry more detail than this file; this changelog is the
repository-side summary.

---

## [1.2.5] — 2026-10-20

### Changed
- 工坊标题与游戏内 mod 名统一为「杀戮尖塔一代角色」（游戏内显示为简称），更换封面。
- Mod ID 仍为 `Spire1`，旧存档与 `mod_list` 条目不受影响。

### Fixed
- **联机分歧**：79 张已退役的 `SPIRE1-*` 卡在 `get_Pool()` 抛
  `InvalidProgramException`，导致两端对「该卡是否已打出」判定相反、校验和分叉。
  现为 `Spire1CardPool` 增加可达池兜底（`Spire1CardPool → Ironclad` 池）。
  这是**崩溃兜底，不是根因修复**：走到兜底时卡框与图标按铁甲池渲染。

## [1.2.4] — 2026-10-11（构建；修复随 1.2.5 一并发布）

### Fixed
- **Strike canonical bug**：deck-grant guard 把 `ModelDb.Card<Strike>()` 的
  canonical 实例注册为 `RunState` hook listener；`Hook.AfterDeath` 每次迭代都会抛
  `CanonicalModelException`，随后 combat turn loop 死亡，表现为出牌动画卡死。
  修复方式：改用 `.ToMutable()` 实例。
- 补齐 4 张缺失卡图（`apotheosis` / `discovery` / `slimed` / `the_bomb`）与
  `dark_shackles` 的 500×380 悬停大图；移除无用的 `parasite.png`。

### Changed
- 移除 Spire1 内置的 Forms 源码（迁往独立仓库 `sts2-forms`）。

## [1.2.3] — 2026-10-10（版本号于 2026-09-30 提升）

### Added
- 新设置「将内容登记进下一局」：控制是否把一代内容注册进新对局；该值在对局创建时
  锁存进存档，修改只影响之后新开的对局。
- `FormsMissingModifierSaveGuardPatch`：旧存档引用已移除的 `SPIRE1-*` modifier 时，
  显式标记为 `Deprecated`，不再静默回落为原生倍率。

### Fixed
- `EnableSts1Events` 此前结构性失效：移除事件的代码跑在 mod 初始化阶段，而事件实例
  要到 `ModelDb.Init` 才入池，开关真假都拦不住。现改为运行期过滤。
- 联机：移除 Spire1 → AutoAnthony / AutoAnthonyWatcher 的编译期依赖，避免 AA 缺席时
  客户端装配触发 `AssemblyLoad` 异常。

### Changed
- Forms（姿态形态）拆为独立 mod（仓库 `sts2-forms`）：Spire1 不再编译期引用 Forms，
  跨 mod 通信走反射桥；旧 `SPIRE1-*` CustomID 保留以兼容存档。
- `StanceCmd` 改为接受任意 stance 字符串，不再硬绑 StS1 枚举。
- 观者三姿态的形态替换玩法为实验性功能，需在自定义对局的修正列表中手动开启，
  默认不启用，且依赖已安装的观者 mod 提供姿态入口；**尚未完成实机对战验收**。

## [1.2.2] — 2026-09-20

### Fixed
- 仅元数据：修正 1.2.1 变更记录里的 Markdown 星号（Steam 只渲染 BBCode），
  重新发布。内容与 1.2.1 相同。

## [1.2.1] — 2026-09-20

### Fixed
- 仅元数据：1.2.0 的工坊变更记录被 steamcmd 的 KeyValues 解析器截断
  （反斜杠转义引号导致字符串提前结束），本条重新发布完整原文。

## [1.2.0] — 2026-09-17

### Fixed
- 中文文本全面校正：统一关键字高亮；修正 11 处星号错位、44 处转义串内的无效标记、
  32 处一代颜色码（`#r`/`#y`）、4 处 ` NL ` 换行、能量符号 `[R]`、12 处嵌套颜色标签、
  2 处句尾重复从句。
- 术语更正：Frail = 脆弱（原「柔弱」）、Smite = 惩恶（原「惩戒」）、
  Plating = 覆甲（原「多层护甲」）。
- 先古之民「欧罗巴斯」的初始遗物升级不再变成占位符「头环」：铁甲战士 / 静默猎手 /
  故障机器人分别正确升级为黑暗之血 / 长蛇戒指（新）/ 注能核心（新）。

### Changed
- 卡池收敛：移除 7 张无法获得的一代观者衍生物卡，并级联移除欧米伽与欧米伽之力；
  耸肩无视、黑暗镣铐保留（仍属旧存档兼容池）。
- 清单计数更正为 **222 张卡牌 / 24 件遗物**。
- 已核实与 RitsuLib 0.6.2 兼容。

## [1.1.0] — 2026-09-12

### Changed
- 接入 AFTP 生态：移除 46 个由 Acts from the Past 已提供的一代事件，以及仅由这些事件
  产出的一次性卡（Bite / Parasite / RitualDagger）与 6 件 FaceTrader / Mushrooms 遗物；
  清理 1214 条本地化键。
- 保留 6 个本 mod 独有事件：瘾君子 / 返璞归真 / 药贩子 / 祛咒之泉 / 写给自己的便条 /
  尖塔之心（其中 SpireHeart 不自动加入）。
- 保留与 AFTP 的共存补丁（KnowingSkull 本地化修正、共享事件过滤）。
- 本版描述中的 230 张卡 / 22 件遗物为当时口径，已被 1.2.0 的 222 / 24 取代。

## [1.0.0] — 2026-09-10

首个 Workshop 版本。

### Changed
- 移除观者（Watcher）：角色、3 个卡池、77 张卡、Strike/DefendWatcher、Eruption /
  Vigilance、PureWater 遗物、全部美术与本地化键、`ArchivedCharacterGatePatch`。
- 移除自建一代地牢：5 个 Act、57 个遭遇、69 种怪物、地牢选择补丁、休息点背景补丁、
  相关配置项；`LegacyActSharedEventFilter` 改为仅服务 AFTP。
- 旧存档中的 `SPIRE1-WATCHER` / 地牢内容由引擎降级为占位卡（不崩溃）。

### Fixed
- 28 条遗物描述重写：把损坏的 `'#b\n'` 标记改为 `!Var!` 动态变量，修正语义槽位。
- 补译仅剩英文的药水文本（Explosive / Fear）。
- 清理孤儿键：8 个 LEAN 遗物、6 个 LEAN 药水，以及观者/地牢相关键。

### Compatibility
- 安装 AutoAnthonyWatcher 时，观者桥接交给该 addon，Spire1 跳过第三方观者桥接，
  避免卡池 getter 补丁冲突。

---

## Pre-1.0 / 1.0 之前

开发期的 0.9.x 构建（0.9.0、0.9.1、0.9.2）没有可核对的公开变更记录，故不在此列出。
其内容在 1.0.0 收敛为三职业（铁甲战士 / 静默猎手 / 故障机器人）+ 卡牌 / 遗物 /
药水 / 6 个独有事件。

Pre-1.0 builds had no verifiable public changelog and are intentionally omitted.
