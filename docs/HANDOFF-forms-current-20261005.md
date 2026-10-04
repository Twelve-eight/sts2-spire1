核对时点: 2026-10-05T03:53:37.1028487+08:00, Asia/Shanghai.
源码HEAD: 1149129e3ea46ad8695f9780ca1d7dcbcc79c4c2.

# 姿态形态当前进度与接续交接 - 2026-10-05

## 一句话结论

姿态形态不是只有脚手架: 当前 r15 字节已在真实隔离非 Steam 游戏中跑通三个姿态的首回合出牌和效果闭环, 也通过了九种可选 Mod 交叉启动. 但可见 UI, 完整长战斗, 战中读档, 重连, 多人, 性能和平衡仍未验收. 本轮没有上传 Workshop, 不要把本地发布目录当作玩家已经收到的版本.

## 当前源码和证据身份

- 活跃仓库: `G:\omp works\Sts\sts2-spire1`.
- 交接前 HEAD: `1149129`, 前一发布提交为 `af74f88`. 交接文档后置备份会产生新文档提交, 不改变 r15 的代码/二进制身份.
- 本次核对的 Forms 源码, StanceCmd, StancePower, AutoAnthonyLoadHook, FormNativeSmokeRunner, FormNativeSmokePatch 和 Spire1RunContent 对 HEAD 没有未提交差异. 全仓仍有路径整理, advice 删除, research 子模块和历史报告等既有 dirty, 不能将全仓称为 clean, 不能 reset 或 git add -A.
- 源文件与发布包 SHA256 快照: `G:\omp works\Sts\sts2-spire1\docs\HANDOFF-forms-source-snapshot-20261005.json`. 其中 CheckedAt 是本机 Asia/Shanghai 的核对时点, 不是新的实机运行时间.
- 当前正式本地 staging: `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1`, 恰有 `Spire1.dll`, `Spire1.json`, `Spire1.pck`.
- DLL: 900608 bytes, SHA256 `8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06`.
- 精简 PCK: 19669354 bytes, SHA256 `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79`.
- manifest: 548 bytes, SHA256 `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305`.
- 不要用旧 Beta r2/r4 ZIP 或旧完整 PCK 的 hash 替代当前 r15. 不同字节不能共用旧实机结论.

## 已落地的可玩入口和结构

- 设计主入口: `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md`. 按日期读其增量段落, 早期的未运行结论已被后来 r15 的窄运行证据部分覆盖, 不是所有旧缺口都自动关闭.
- 入口是原生自定义对局修正 `FormStanceModifier`, 非共享配置或全局静态开关. `FormStanceMode.IsSelected` 读取本局 Modifiers; 已选模式在桥接不可用时明确报错. 不进入随机每日/涅奥修正池.
- Watcher 是可选的运行时反射桥接, 不加入 Spire1 的 manifest 或编译硬前置. Spire1 发布 DLL 的 Mod AssemblyRef 只有 BaseLib, manifest 声明 BaseLib >= 3.4.5.
- 保留 Watcher 原生姿态 marker 与通知链. 切换先退出旧承载再进入新承载, 重复进入同姿态幂等, 无姿态不附加形态. 不同时叠两套原生/自定义效果.
- 平静 Calm: `VoidSerpentStancePower`, 配对 `VoidFormEffectPower` 和 `SerpentFormPower`.
- 愤怒 Wrath: `DemonReaperStancePower`, 配对 `DemonFormPower` 和 `ReaperFormEffectPower`.
- 神格 Divinity: `EchoCelestialStancePower`, 配对 `EchoFormEffectPower` 和 `CelestialFormPower`.
- 因此报告中的三形态 smoke 是三组姿态承载/六个效果, 不是六个效果分别完整单独验收. 姿态 zhs 名称已经核对 `G:\omp works\docs\terminology-glossary.md`.

## 继续开发必须保持的已定规则

以下为已确认设计契约, 不将其每一个边界都写成实机通过:

- 平静的虚空额度: 入场后的下一次手动打牌系列免费; 自己的回合开始重置. 进入姿态的牌不追溯消费, 自动打出不消费, 重放系列不能多扣费用. 费用应走引擎 hook, 不自造 X/星星规则.
- 群蛇: 实际完成的 CardPlay 造成 3 点无源伤害, 包括自动与重复打出, 但进入牌不追溯触发. 目标走 CombatTargets RNG; 没目标不消耗 RNG. 离开平静只给一次 2 能量, 外部原生 Calm 已给时不能叠给, 战斗结束清理不发能量.
- 恶魔: n 是引擎当前回合数; 入场/自己回合开始目标力量 T(n)=n*(n+1)/2, 只补差值并按实际接受的增量记账. 退出只撤形态来源, 不吞其它力量. 敌方来源对本人的每次伤害额外加 n, 不能缩成只有有源攻击; 自伤/友方不加.
- 死神: 按原版 ReaperFormPower 的本人/宠物, IsPoweredAttack, TotalDamage>0 判定给目标等量 Doom, 不是回血, 不偷换成 CardType.Attack. 模式下不叠原愤怒双倍攻防.
- 神格: 入场后的下一张牌增加一次打出, 保留其它合法重放次数, 进入牌不能追溯消费. 入场获得 max(n,3) 能量并抽同样张数, 不叠原神格额外 3 能量或三倍伤害. 下一个自己的回合开始退出, 不能在进入的同次 hook 列表中立刻被清掉.
- 所有命令 await, 状态按实例/真实动作引用隔离, 不引入未同步随机源或工作线程 Godot 访问. 不放开两个 AFTP held-back 编译层.

## 最新中央验证和源码审查

- 效果探针 r12: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-effects-probe-r12-central-20261004.md`. 权威运行输出 `TOTAL 133 PASS 133 FAIL 0 SELECTED 133`; 构建 0 errors, 3 warnings. 生产 Forms hooks 被链接, 但 command spies/窄生命周期对象不是完整引擎 scheduler, UI, 存档或多人.
- Void 免费事务审查: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-form-f1-deepseek-review-20261004.md`. 支付 Task 成功完成后才能 Claim; 每次支付独立 token; 引用身份与幂等清理; 进入牌不追溯消费; cancel-first 探针结构已逐项给源码证据. 真实动态 Harmony 注册, 真实 PlayCardAction 的执行期取消交错, 其它 SpendResources 调用点仍未被此只读报告验证.
- 力量事务/账本相关历史入口: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\demon-form-ledger-supervisor-20261002.md`, `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\DemonFormStrengthTransactionPatch.cs`. 读当前源码, 不把更早快照的 finding 或 PASS 直接套上当前字节.

## r15 真实隔离游戏证据

- 原生 smoke 报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-current-r15-20261004.md`.
- 原始结果: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r15-current-20261004\run-final.json`.
- 有效运行窗口为 2026-10-04 14:23:45 至 14:24:26 +08:00. exitCode=0, 无超时, 窗口句柄采样均零, 日志排空, 场景结果新鲜, 共享配置 hash 不变, 测试 settings 恢复且 mods 清理完成.
- Calm: 真正 `WATCHER_VIGILANCE` 进入原生平静; 两次 `WATCHER_STRIKE_P` 各总伤害 9, 第一次手动打牌免费, 第二次支付一次.
- Wrath: 真正 `WATCHER_ERUPTION_P` 进入原生愤怒; 后续 strike 伤害 7, Strength=1, Doom=7, 能量 1 -> 0.
- Divinity: 真正 `WATCHER_BLASPHEMY` 进入原生神格; 后续 strike 总伤害 12, Echo play counts=[2,2], 能量 5 -> 4.
- 三场景 status=passed, formGateAfter.passed=true, effectVerification.passed=true, unobservedFaults=[]. 这些是特定测试敌人和首回合的观测, 不是所有数值/Hook交互都正确的证明.
- 可选 Mod 矩阵报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\partial-mod-launch-matrix-current-r15-20261004.md`, 原始结果在同隔离根的 `partial-mod-matrix-r15-current-20261004\matrix-summary.json`.
- 9/9 组合正常退出. 缺 Watcher 不阻塞 Spire1; 缺 BaseLib 时 loader 明确拒绝 Spire1; AutoAnthony bridge 在 m6/m7/m8 分别为 Pending/LegacyBridge/OfficialAddon; 缺 AutoAnthony 时 addon 被拒绝而 Spire1 仍初始化.
- 第三方 AutoAnthony 的 `Expected 65 complete v111 Colorless cards, found 76` 仍是资源版本自检问题, 不等于 Spire1 启动失败, 本轮不维护停止维护的新版 autoanthonyrelics.

## 尚待验收及优先方向

1. 真正可见的自定义修正列表, Watcher 选人/进入对局, 图标/tooltip/动画和中文文案, 键鼠及手柄操作. headless 已通过不代表视觉或手动入口已通过.
2. 完整长战斗: 神格下一自己回合退出, 愤怒力量增长/撤回与敌方伤害各种来源, 平静退出能量/原生联动, 群蛇多次完成/无目标 RNG, Echo 与其它重放交错.
3. Divinity 当前 smoke 使用 Blasphemy, EndTurnDeathPower 会在后续回合结束战斗. 不能把这一窄场景标成完整长回合通过, 也不能为了烟测删除生产即死语义. 长战斗载体要重新设计隔离场景.
4. 真实支付失败/取消/嵌套重入与 action completion 交错, 第三方费用 hook 和力量 hook 干预, 动态 Harmony 目标命中记录.
5. 战中存档和跨进程读档, clone 状态, 双端建局/多人同步/重连. Run modifier 持久化设计不等于实机协议已验收.
6. 量化性能与平衡, 用户 Steam 安装和朋友机器可见安装. 仍无本轮新增实机证据.

## 与推送前准备会话的并行边界

- 本会话正在修复发布管线, 不是继续改形态玩法. 当前实现写集只有 `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`, `G:\omp works\.tooling\refresh-workshop-payloads.ps1` 和另外四 mod 的 csproj. 不与另一个形态开发会话争抢 Forms 源码.
- 本地标准 Release 目录和 Workshop content 正在做来源/字节门禁准备. 形态会话请使用新的 G: 隔离输出目录, 不自行覆盖 `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release` 或 `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1`, 不自行上传.
- 当前旧完整 canonical PCK 仍是 28866294 bytes / `CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E`, 本轮尚未执行修复后的 Promote. 正在把精简 producer 接入 canonical, 不能手工补摘要或改时间绕过门禁.
- 中央隔离发布门禁已在 PS7 跑通 11/11, 不属于形态实机测试. 接续实现和两个监督尚未给出本轮最终验收. 全量 refresh/VerifyOnly/GuardsOnly 以及 Workshop 上传均尚未完成.
- 形态源码一旦变化, 旧 DLL/PCK hash 对应的 r15 证据不再自动覆盖新构建. 需新 Release, 发布门禁和绑定新字节的 smoke; 两会话共享源码变化请落盘到 DEVLOG 并明确所用输出目录.
- 不写 `G:\steam\steamapps\common\Slay the Spire 2`, 不改共享 `G:\appdata\C-Users-o_Obl\Roaming\SlayTheSpire2\mod_configs`, 不启动/停止用户游戏, 不写 C: 缓存.
- 用户当前只允许 DeepSeek 子代理: `global:deepseek-v4.1-flash`, reasoning `max`, 原生 Codex multi_agent_v1, 总并发上限 12. 本轮 session provider 的细路由须按安全元数据核验, 未暴露的 wb2api 子路由写 Unknown. 旧契约里的 Astra/6.1sol 授权是历史记录, 不是本轮许可.

## 接手最短路径

先读本文件, 源码/hash快照和当前 r15 两份运行报告; 再按上述未验收面选择工作. 不重做已关闭的三姿态首回合 smoke, 不把启动矩阵当长战斗. 本文是核对时点快照, 后续发布准备状态以 `G:\omp works\Sts\sts2-spire1\DEVLOG.md` 的新段落和 `G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\coordination-20261005.md` 为准.