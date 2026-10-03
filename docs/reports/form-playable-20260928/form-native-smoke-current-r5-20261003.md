# 当前 Release 字节三形态真实隔离烟测 - 2026-10-03

## 结论

源码驱动重建生成的当前 DLL `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2` 已在非 Steam 隔离游戏副本完成三条真实 Watcher 出牌链路。Calm、Wrath、Divinity 全部通过，且外层清理门禁通过。

## 运行身份

- runner：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r5-current-20261003.ps1`
- 结果：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r5-current-20261003\run-final.json`
- 游戏：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\SlayTheSpire2.exe`
- 挂载：BaseLib、Watcher、当前 Spire1 payload
- 运行时间：2026-10-03 09:25:51 +08:00 至 09:26:32 +08:00
- 进程退出码：0；未超时；40 次窗口句柄采样均为零；日志排空
- scenario 新鲜度：通过
- 共享 `mod_configs`：哈希未改变
- Steam settings：已恢复
- 测试 mods：已清理

## 三条路径

### Calm

- 入口：`WATCHER_VIGILANCE`
- carrier：`VoidSerpentStancePower`
- effects：`VoidFormEffectPower`、`SerpentFormPower`
- 后续两次 `WATCHER_STRIKE_P` 均完成
- 第一次免费，第二次支付 1 能量
- 两次总伤害均为 9
- `unobservedFaults=[]`

### Wrath

- 入口：`WATCHER_ERUPTION_P`
- carrier：`DemonReaperStancePower`
- effects：`DemonFormPower`、`ReaperFormEffectPower`
- 后续 `WATCHER_STRIKE_P` 完成
- 目标伤害 7；`StrengthPower=1`；`DoomPower=7`
- 能量从 1 降到 0；支付证据为 1
- `unobservedFaults=[]`

### Divinity

- 入口：`WATCHER_BLASPHEMY`
- carrier：`EchoCelestialStancePower`
- effects：`EchoFormEffectPower`、`CelestialFormPower`
- 后续 `WATCHER_STRIKE_P` 目标总伤害 12
- Echo 完成增量 2；两次 play count 均为 2；play indices 为 0、1
- 能量从 5 降到 4；两次播放各记录 1 能量
- `unobservedFaults=[]`

## 已知噪声

headless 环境仍报告 crashpad 缺失、Dummy renderer RID/resource leak；这些未被 runner 归入业务失败。日志确认 Watcher bridge bound、三张入口卡实际出牌和 Steamworks 正常关闭。

## 未关闭边界

未宣称可见 UI/图像/动画、长战斗回合边界、战中存档/读档、重连、多人同步、性能和完整数值平衡通过。
