# 监视状态 — LogWatch

- 基准时刻: 2026-08-27 15:41
- 已分析 zip（简报已落盘）:
  - 1-142407.md ← checksum_28 (Slimed 打出)
  - 2-142627.md ← checksum_35 (SHINING_LIGHT 事件)
  - 3-143656.md ← checksum_286 (Slimed 复现, 断线)
  - 4-150002.md ← checksum_558 (DARV+DustyTome, 断线)
- 已登记的旧 zip（跳过）: 20260824_232954_checksum_249
- godot.log 基准大小: 900790 字节（其内 0 条 divergence；divergence/断线记录在轮转件 godot2026-08-27T15.39.46.log 中）
- 已知 zip 全名单见 known-zips.txt

核心结论（详见各简报）：
1. Slimed 系（#28/#29/#286）：Host 与 Remote 在打出 Slimed 后一张牌的 Hand/Draw 归属错位 —— 疑似已知 AFTP ClassicSlimed 联机标记丢失 bug。
2. SHINING_LIGHT（#35）：Niche RNG 计数 32 vs 28 —— AFTP 事件 RNG 消费不对称。
3. DARV（#558）：Remote 端多出一个 DustyTome(先古卡) 并顶掉 VELVET_CHOKER —— 已知 DustyTome/DARV 分歧史。
