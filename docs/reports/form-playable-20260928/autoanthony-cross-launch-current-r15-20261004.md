# AutoAnthony cross launch current r15 2026-10-04

## 已确认

- [P0] 当前交叉矩阵绑定的 Spire1.dll 为 8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06, length=900608.
- [P0] m6 无 Watcher 时 Spire1 仍初始化, third-party=Pending, settled=false. 这表示缺少 Watcher 不会制造硬引用或崩溃.
- [P0] m7 有 AutoAnthony 和 Watcher 时使用 LegacyBridge, third-party=LegacyBridge, settled=false.
- [P0] m8 追加 AutoAnthonyWatcher 后使用 OfficialAddon, third-party=OfficialAddon, settled=true, legacy path 不阻塞.
- [P0] m9 缺少 AutoAnthony 时 AutoAnthonyWatcher 被依赖检查拒绝, BaseLib, Watcher 和 Spire1 仍成功初始化.
- [P0] 9 个场景都 exitCode=0,无超时,无非零窗口句柄,日志排空,shared mod_configs 未变化,Steam safe=true.
- [P1] AutoAnthony 自身在 m6, m7, m8 输出 Expected 65 complete v111 Colorless cards, found 76. 该错误来自第三方资源版本自检,没有阻塞 Spire1.

## 进行中

- [P1] 该报告只覆盖可选桥接和启动层,不将第三方资源版本问题归因于 Spire1.

## 未知

- [P2] 未验证 AutoAnthony 自身资源内容修复,未验证 UI,长战斗,存档,多人,性能和完整平衡.

结论: 当前字节的 AutoAnthony optional bridge cross launch 通过,并保持 fail-closed.