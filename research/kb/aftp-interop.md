# AFTP 互操作档案（KB）

> 2026-08-25 夜间建立。许可证结论、fork 拓扑、问题清单、验证阻塞。

## 许可证结论（已核验）

| 仓库 | 许可证 | 后果 |
|---|---|---|
| Cany0udance/ActsFromThePast（主仓） | **无 LICENSE 文件** = 默认保留所有权利 | GitHub ToS 内可 fork/私改；**二进制对外发布需作者书面许可**；本机私用+好友局属灰区，谨慎 |
| Kziz3988/ActsFromThePastMultiplayerBalance | **MIT** | 自由修改/再发布，保留版权声明即可 |
| Ajama11/ActsFromThePastTweaks | （第三方先例）社区存在 tweaks 类 fork 的先例 | — |

- 主仓 README 明示：该仓库不再发版（比"九月前停更"更强：永久停更于 GitHub 侧）。
- 我方 mod 与 AFTP 是运行时 Harmony 共存关系，不捆绑不分发其代码——无许可证传染。

## fork 拓扑（2026-08-25 建立）

```
Twelve-eight/ActsFromThePast                  ← fork of Cany0udance（无License）
Twelve-eight/ActsFromThePastMultiplayerBalance← fork of Kziz3988（MIT）
本地克隆：G:/omp works/aftp-{ActsFromThePast, ActsFromThePastMultiplayerBalance}
上游参照：G:/omp works/aftp-upstream（Cany0udance 浅克隆）
构建产物 stage：G:/omp works/aftp-stage/
```

## 构建移植记录

- 主仓 csproj 硬编码 `C:\Program Files (x86)\Steam` → 已改 G 盘路径；
  PostBuild 从直拷 live mods 改为 **aftp-stage/**（曾误部署进 live mods 造成与工坊版同 id 冲突风险，已回滚并根治）。
- 主仓依赖 BaseLib **3.3.6**（nuget），我方环境跑 3.4.5——版本漂移是潜在兼容问题源。
- MPBalance 带 `Sts2PathDiscovery.props` 注册表自动发现，零修改即构建成功。

## 问题清单（待修，按证据强度排序）

1. **火堆进入黑屏死锁**（多次实机复现；根因未定，嫌疑序：Act4Heart overlay hook → Watcher(ws) rest 场景 → AFTP-MP-Balance 房间表 → 本体）。取证协议：冻结瞬间先拷 logs 再杀进程。
2. **转场遮罩暗屏卡死家族**（Esc 可解）：与 1 可能同源。
3. **MP 失同步三案**：粘液抽牌堆移位 / Sunder Cancel / win 前黑屏。RitsuLib #563/#249 判定为 mod 清单级假阳性，非 AFTP 实伤——真正归责待最小集实验。
4. **Wheel of Change / Match&Keep 小游戏在 autoslay 下停摆**：我方 AutoSlayModdedScreenHandlersPatch 已兜底（仅 autoslay 生效）；上游若做官方 handler 更佳。
5. **BaseLib 版本漂移**：fork 构建用 3.3.6 vs 环境 3.4.5，升级 pin 前需回归。

## 验证阻塞

- 本机未启用 AFTP 地牢 → fork 改动无实机靶场。启用涉及联机 mod 集一致性（双方都要同版本），等用户决策窗口。
