# Visual assets audit r11 - 2026-10-01

范围: `G:\omp works\Sts\sts2-spire1` 的姿态形态视觉资源和 Godot 路径。只读审查; 不构建, 不部署, 不启动游戏, 不修改产品代码或共享配置。

模型和路由: 用户锁定 `6.1sol` / `agentrouter`; 本轮未委派子代理。

## 已确认

- [P1] 源码存在明确的 Godot 图标路径请求。绝对路径: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs:40-49`。证据: `IconFile` 将 Calm/Wrath/Divinity 映射为 `calm_power.png`/`wrath_power.png`/`divinity_power.png`, 并请求 `res://Spire1/images/powers/` 与 `res://Spire1/images/powers/big/`。命令: `Get-Content -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\WatcherFormStancePower.cs' | ForEach-Object ...`。边界: 该结论仅证明源码请求字符串, 尚未证明 `Spire1.pck` 内存在对应条目或运行时可加载, 也不是实际游戏视觉验收。

## 进行中

- 检查面 1: 读取并确认 `Spire1.pck` 内的资源目录和条目, 待完成。
- 检查面 2: 对比源码请求路径和 PCK 打包路径, 待完成。
- 检查面 3: 核对 eng/zhs powers localization 的载体和六个效果 Power token, 待完成。
- 检查面 4: 结合现有 headless 日志判断 missing image 的证据边界, 待完成。

## 未知

- `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1\Spire1.pck` 是否含有形态 icon/effect 资源, 未由本条结论确认。
- 当前证据是否能把 missing image 唯一归因于 PCK 缺资源、大小写/路径不一致或其它原因, 未知。
- 未进行真实游戏启动、视觉显示或玩家可见 tooltip 验收。
