# sts2-spire1 夜间批次完整报告（2026-08-24）

范围：BaseLib mod 将 StS1 内容移植到 StS2 public-beta v0.111.0。
状态：**全部工作已提交推送，HEAD=`b8d530d`，干净可复现。**

---

## 一、已交付修复（全部实机验证）

| # | 修复 | 根因定位 | 验证 |
|---|---|---|---|
| 1 | 302 卡面小图+大图恢复 | big 槽才是卡面主图：NCard.cs:1248←CardModel.Portrait:157←PortraitPath:143←BaseLib CustomCardModel.cs:300 前缀重定向 | 用户目验 |
| 2 | mutagenic_strength 遗物图三件套 | 资产缺失 | 465efe9 / 8482a85 |
| 3 | ROOM_FULL_OF_CHEESE 池耗尽崩溃 | 池空时随机选择抛异常 | SharedCardReuse 扩展官方复用卡（3deabac）|
| 4 | BladeDance B 级漂移 | 官方版自耗尽≠StS1 | 移出 SilentReuse，自有类回归现役（f75ec23）|
| 5 | Seek/Nightmare 打出卡死 | 缺 `.selectionScreenPrompt` 键 → CardModel.cs:129 throw | 补两语言 plural 键（f75ec23）|
| 6 | 5 卡通配符失配 | !X! 必须精确匹配 C# 注册变量名 | Aggregate 重写、Claw→!Increase!、Halt/Prostrate→!MagicNumber!、Streamline→!CostReduction!；ERR 306→1 实证 |
| 7 | Splash 候选集跨池泄漏 | 仅按池对象排除持有者 | SplashOwnSetSubtractPatch 集合差替换 OnPlay（4f26648）|
| 8 | 地图旅行竞态 | Harmony 不下探派生类 IsEnabled | MapTravelRescuePatch 挂基类 NClickableControl（3ebbab0）|
| 9 | PatchAll 一损俱损 | 整集 Patch 单点失败剥离全部 | MainFile 逐类 try/catch 加固 |
| 10 | BiasedCognition 致命缺键 / Rebound 潜伏 | ApplySelf<T> 无 amount 重载按名查 DynamicVars | 注册 PowerVar（659b098）|
| 11 | Recursion 玻璃球种兼容 | v0.111 新球种不在四球 switch | 未知球只 evoke 不重铸（275a308）|
| 12 | SteamBarrier !B!→!CB! / GlassKnife !D!→!CD! | 通配符映射表 | ERR 实证收敛 |
| 13 | 41 力量图标重生 | 有 StS1 源的 64+256 重生成；无源 20 张中性纹章 | ca8c0b2 |

## 二、本窗口新交付

### 1. SPIRE1-WATCHER 归档（用户指令）
- `Spire1Config.EnableSts1Watcher=false` 默认；`HideFromVanillaCharacterSelect` / `AllowInVanillaRandomCharacterSelect` 双 override。
- 模型保留注册（老存档兼容）。归档前覆盖推进至 39/77。

### 2. 商店购买守卫（用户实测驱动）
- **根因**：autoslay 只按 `IsStocked && EnoughGold` 过滤，禁药遗物（添水/SOZU）使商人拒购药水但槽位仍显示可购 → 循环空转至 maxAttempts=50。
- **终版** `ShopEnoughGoldGuardPatch`：postfix `MerchantEntry.EnoughGold` getter——`Hook.ShouldProcurePotion` 为 false 或药水栏满 ⇒ EnoughGold=false，原生循环自然跳过。全同步零阻塞。
- **实测**：sozu-ban ×6 日志、含药水商店秒过 ✓。
- **废弃方案留档**：HandleAsync 前缀 `.Wait()` 主线程死锁（用户目击冻结于“对对碰”残留画面）——教训：禁止在 Godot 主线程阻塞等待游戏任务。

### 3. 尘封魔典（DUSTY_TOME）机制闭环
- 官方遗物 zhs 名**尘封魔典**（RelicRarity.Ancient）；`SetupForPlayer` 从当前角色池抽 `CardRarity.Ancient` 牌升级入堆。
- Regent 池含 `TheSealedThrone`（Ancient Power）⇒ “储君→封印王座”实证吻合。
- **一代角色四池 Ancient 数=0** ⇒ 空集；原版 `NextItem(items).Id` 直接解引用 ⇒ NRE（Darv 事件打不开）。
- 树内已有 `DustyTomeAncientFallbackPatch`（03ae5d1）回退 PlaceholderID 对应官方池；本次冒烟经 `relic add DUSTY_TOME` 等效链路（ToMutable+SetupForPlayer+Obtain）验证 obtain 成功、无异常 ✓。

### 4. 两项挂账审计清偿（全绿）
- **非 cards 域通配符**：九域扫描，events 外全部零占位符；events 53 事件/1312 键占位符↔C# 注册名 0 失配。脚本 `.tmp/audit-event-vars.js`。
- **联机兼容**：RewardClamp（per-player 参数）、Splash（splash.Owner）、DustyTome 回退（每玩家 RNG 流）均无单例假设；AutoSlay 系全门控；联机要求双方 mod 集一致 ⇒ 补丁层安全。

## 三、覆盖矩阵（权威口径 coverage.js）

| 角色 | 覆盖 | 实缺 | 备注 |
|---|---|---|---|
| SPIRE1-IRONCLAD | 36/44 | 6（Bash/Berserk/LimitBreak/Reaper/SeeingRed/SeverSoul） | Strike/Defend 为起始牌替代类 N/A |
| SPIRE1-SILENT | 34/47 | 10 | Neutralize/StrikeSilent/DefendSilent N/A |
| SPIRE1-DEFECT | 54/58 | **0 ✅** | 余 4 全为起始牌替代类 |
| SPIRE1-WATCHER | 39/77 | — | 已归档，挂起 |

验证标准=日志出现 `Playing <完整id>`；统计脚本 `.tmp/night/coverage.js`（继承链解析池归属）。

## 四、重大复盘：卡牌注入器从未贡献过覆盖

四代演进全部失败并归档：
1. canonical 直传 AddGeneratedCardsToCombat —— “already have a pile” 异常被丢弃任务吞掉；
2. CreateCard+AddGenerated —— async 管线在 postfix 上下文 await 不恢复；
3. SetUpCombat 时点 —— PlayerCombatState/DrawPile 尚未创建；
4. PopulateCombatState+CreateCard/AddInternal —— 入堆成功但抽到即断回合链（turn3 “No playable turn” 超时退出）。

**关键结论**：历史覆盖增长全部来自角色自然出牌；跨角色注入失败的根源是 canonical Owner=null。已回滚原版实现（f2f3305 版本），后续覆盖消化策略=对应种子多跑整局自然 drain。相关教训（discarded task 吞异常、假绿审计哨兵、seed→角色漂移须逐局核对日志）全部写入 DEVLOG。

## 五、冒烟战果（本窗口）

| 局 | 角色 | 结果 |
|---|---|---|
| r2 guard-r2 | SPIRE1-DEFECT | ★胜利，两座商店通过、无停滞 |
| silent-drain | SPIRE1-IRONCLAD（漂移）| ★胜利 F17-A3 全零噪音 |
| r13 | — | 注入器破坏回合链超时退出（已回滚根治）|
| final-check | SPIRE1-IRONCLAD | ★胜利 F17-A3，ERR:0 EXC:0 NaN:0，无注入残留 ✓ |

## 六、挂账与建议

1. **自然覆盖 drain**：Ironclad 6 张 + Silent 10 张——各跑 2-3 局整局即可消化（种子→角色漂移，每局以日志 “Selecting character” 为准）。
2. **尘封魔典一代角色体验**：守卫/遗物链路已通，可选给每代角色配一张忠实 Ancient 卡以贴近原版体验（设计决策待定）。
3. **AFTP 沟通材料**：①其内置 loc 的 {Damage} 系模板缺变量渲染噪音；②突变之力力量图标 NOPE（pck 有引用无资源）；③添水类禁药遗物与其商店交互正常（我方守卫已兜底）。
4. **CodeOpt 优化流**未跑；跑后需重验一轮 autoslay 回归。
5. run_history 110×70B 图标宁缺勿造维持挂账。

## 七、提交清单（今晚推送序列）

```
ca8c0b2 art: regenerate 41 power icons (64+256), monogram fallbacks
f2f3305 feat: archive WATCHER behind config; shop purchase guard; card injector
e3c0b9d docs: DEVLOG — watcher archival, shop guard, dusty tome analysis
e7596cc docs: non-cards wildcard audit clean — 53 events/1312 keys, 0 mismatches
f9e3721 docs: multiplayer audit clean; correct dusty-tome analysis
b8d530d fix: shop EnoughGold guard (verified), relic injector ToMutable+SetupForPlayer; revert card injector
```

代理失效均走直连 fallback 推送成功；工作区干净，无未推送提交。
