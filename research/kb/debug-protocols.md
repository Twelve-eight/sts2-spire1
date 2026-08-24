# 调试与取证协议（KB）

## 火堆黑屏类冻结取证（SOP）

1. 冻结瞬间**不要杀进程**：`robocopy C:\Users\o_Obl\AppData\Roaming\SlayTheSpire2\logs G:\backups\logs-<时间戳> /E`
2. 记录最后 50 行：`tail -50 godot.log`
3. 判别签名：
   - "入队动作永不执行"（Enqueueing 后无 Attempting to find ready action）= 动作执行器停摆
   - 用户控制台 win/block/draw 是自救痕迹，不是病因——往更早翻
4. 才允许杀进程。恢复点语义：从最近已完成房间重启；若上一节点是事件则重打该事件。

## RitsuLib divergence zip 对拍

- 位置同 logs 目录，文件名含 checksum 序号。
- 读法：先数 `differ` 出现行数 → 只深读这些段；players/piles/choices/rewards 全同 = 清单级假阳性。
- 我方缓解：Spire1Config.IgnoreMpModDifferences（握手放行+弹窗抑制）；诊断 zip 仍落盘。

## 覆盖 drain 夜间管线

- 循环器：`.tmp/night/night_drain.ps1`（hub start name=night-drain 启动；cutoff 内自动连跑 --autoslay）
- 归档：每局 godot.log → `.tmp/p1-smoke/autoslay-<seed>.log`
- 汇总：`node .tmp/night/coverage.js` → 终态打印 + queue-<pool>.txt 缺口队列 + COVERAGE.md
- 注意：autoslay 选角=种子随机（AutoSlayer._random.NextItem 未锁定按钮），SPIRE1 角色命中率随包内可见角色数浮动；character.txt=all 时约 2/N。

## 控制台速用

- modded 自动开 DevConsole；常用 `relic add DUSTY_TOME`、`draw N`、`block N`、`win`
- 非战斗状态敲命令有入队停摆风险（公测版调度缺陷）——测试尽量在战斗内做

## 已知良性噪音（勿误报）

AFTP {Damage} 渲染噪音、chosen_death.ogg 缺失、Act4-not-implemented、Asset not cached 懒加载、退出时 RID 泄漏告警。
