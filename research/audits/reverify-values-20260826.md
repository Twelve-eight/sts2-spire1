# 重验报告 — 怪物 AI / 事件数值（adversarial re-verification）

- 执行者: ReVerifyValues · 日期: 2026-08-26 · 项目冻结点 HEAD = 6ba5c8d
- 方法: 不信任任何已写结论；只认四类硬证据 —— StS1 javap 反编译、StS2 引擎反编译源(research/engine-dllsrc)、我方当前代码、运行日志。
- **勘误**: 任务给的 StS1 路径 `G:/steam/steamapps/common/Slay the Spire/desktop-1.0.jar` 不存在；实际为 `G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar`（365,086,855 B）。javap = Zulu 21.0.8。
- 本轮新生成字节码存档: `.tmp/reverify-javap/`（24 类 + Random + AngryPower，含 -v ConstantValue 复核）。
- 分布类结论用蒙特卡洛复核（每项 200 万回合，固定种子）。

---

## 一、怪物 AI 真值表（Session 7.2–7.6 / DEVLOG L265-298）

### M1. JawWorm：频带 25/30/45 + 子卷 0.5625/0.357/0.416 — ✅ 成立
字节码 (.tmp/reverify-javap/monsters.exordium.JawWorm.txt:378-459)：
- 首招 CHOMP(byte 1, ATTACK)；之后按 roll r 分三带：
  - r<25: lastMove(CHOMP) ? randomBoolean(0.5625) ? BELLOW : THRASH : CHOMP
  - 25<=r<55: lastTwoMoves(THRASH) ? randomBoolean(0.357) ? CHOMP : BELLOW : THRASH
  - r>=55: lastMove(BELLOW) ? randomBoolean(0.416) ? CHOMP : THRASH : BELLOW
我方 Monsters/JawWorm.cs:64-101：bandPicker 权重 25/30/45 ≡ 阈值 25/55；三带守卫与互补权重逐分支等价；所有 MoveState 有 FollowUpState，bands.AddState(bandPicker, ()=>true) 在位（7.5 修复未回退）。
注: L274 括注「0.5625 after Bellow→56.25% THRASH」是修复前旧表述，已被 7.5(L289-291) 自纠；现行代码注释正确。

### M2. AcidSlime L/M 权重表 — ✅ 真值确认；⚠️ 两点保留
字节码频带宽 (AcidSlime_L.txt:434-543, AcidSlime_M.txt:257-541)：L base 30/40/30(<30/<70)、L A17+ 40/30/30(<40/<70)、M base 30/40/30(<30/<70)、M A17+ 40/40/20(<40/<80)。子卷细节同步确认（L-base cap 违约后 50/50、0.4 SPIT/0.6 WEAK、0.4 SPIT/0.6 TACKLE；L-A17 0.6/0.6/0.4+lastMove；M-A17 中段 0.5）⇒ 「vanilla 用条件子卷而非平 cap」备注正确。
⚠️ 保留一（量化）: 我方平权重+maxRepeats=2 近似 ≠ 原版条件结构。模拟(2e6 回合)：L-base 原版长期 Spit/Tackle/Lick = 35.7%/29.6%/34.7% vs 平权重 31.0%/38.1%/31.0%（Tackle 高估 +8.5pp）；AcidSlimeL.cs:67-68 注释「reproduce the same long-run mix」不成立。M-A17 原版 39.0/39.9/21.1 vs 平权重 38.9/38.9/22.2 —— 吻合良好。
⚠️ 保留二（政策不一致）: 双酸泥+尖泥把 A17 表门控在 HasAscension(DeadlyEnemies)；引擎 AscensionLevel 枚举共 10 级、DeadlyEnemies=第 9 级 ⇒ StS2 A9/A10 会启用 A17 表；而 StS1 至 A16 都用 base 表，且 L276 自述政策是「A17-only 一律落底档」（Looter/SlaverBlue Weak/SlaverRed Vuln 已落底档）。代码注释自辩「nearest higher-difficulty tier」——有意为之但与 DEVLOG 政策冲突。

### M3. SpikeSlime caps — ❌ DEVLOG 表述错误；✅ 当前代码反而与字节码一致
字节码 (SpikeSlime_L.txt:383-501, _M.txt:192-310 同构)：base tackle<=2(lastTwo)/lick<=2(lastTwo)；A17+ tackle<=2(lastTwo)/lick<=1(lastMove)。**不存在任何 tackle max1 档**。L295 的「base tackle max2/lick max1; A17+ tackle max1/lick max2」双向不符。
我方 SpikeSlimeL.cs:59-79（注释即正确真值）：normalAi lick cap=2 / ascendedAi lick cap=1、tackle 均 2；权重 30/70 与频带一致。**风险**: 后人照 L295 文字「再修」会把正确实现改坏。

### M4. SlaverBlue「cycle S,S,R (stab max2/rake max1)」 — ❌ 文字错误；✅ 代码=原版 base 档
字节码 (SlaverBlue.txt:267-362)：STAB iff num>=40 && !lastTwo(STAB)；否则 RAKE iff !lastTwo(RAKE) else STAB（base rake<=2）；A17 rake lastMove 档已按 L276 政策弃用。首招 60/40 ✓。
我方 SlaverBlue.cs:57-66: AddBranch(stab,2,60f)/AddBranch(rake,2,40f)，引擎 GetStateWeight 语义下精确复现 base 转移概率（模拟长期 STAB 占比 53.3%；"cycle" 只是模式化近似，「rake max1」是被弃用的 A17 行为）。

### M5. Louse 双分支历史映射 — 结构 ✅ 代码正确；❌ 「~80% BITE」数字错误；❌ L294 括注方向写反
字节码 (LouseNormal.txt:383-485, LouseDefensive.txt:430-532 同构)：base: r<25 用 lastTwo(GROW/WEB)?BITE:X、r>=25 用 lastTwo(BITE)?X:BITE；A17+: r<25 改 lastMove。我方 GrowGuard/WebGuard（base=lastTwo、A17+=lastMove）与之一致。
❌ 数字: 蒙特卡洛+马尔可夫解析双验证：base 长期 BITE=58.36%、A17+=63.63%。DEVLOG:294、LouseNormal.cs:86、LouseDefensive.cs:88 三处「~80% BITE / ~20% debuff-buff」均错（行为没错，文档错；也非 wiki 口径 75%）。
❌ 方向: L294 写「base lastMove guard, A17+ lastTwo guard」，恰与字节码（也与我方代码）相反。

### M6. Lagavulin 伤害唤醒 STUN — ✅
原版 damage() 掉血即 setMove(4, Intent.STUN)（dump:581-600），takeTurn case4 仅弹 STUNNED 文本并 changeState("OPEN")+预置 ATTACK ⇒ 唤醒回合被吃掉。引擎 Creature.StunInternal（Creature.cs:525-543）确置 MustPerformOnceBeforeTransitioning=true + SetMoveImmediate；MoveState.CanTransitionAway 由它门控（MoveState.cs:21-34）⇒ 缺标志则 stun 态被下次 RollMove 直接甩开（当年 bug 机理）。我方 Lagavulin.cs:139-143 在位；getMove 分支 sleep(!isOut)/debuff(cnt>=2 或 lastTwo attack)/attack 与原版 getMove(dump:643-684) 逐条一致。

### M7. Session 12.2 四修
- WrithingMass P0 (L518-519) — ✅ 以 Session 15 形态落地：root(WRITHING_RESOLVE)→5 招式→root，无纯条件环；ResolveFirst/ResolveBands 重掷范围 [10,99]/[20,99]/[0,19]/[40,99]/[0,39]/[0,69] 与原版递归逐一对应（random(int)=nextInt(range+1) 含上界——random.txt:116-124 实证）；float 先于 int 的抽取顺序同字节码。commit 361d330 在 git 历史。注：L518-519 记载的「reroll AddState(bands,()=>true)」修法已被取代——照旧文行动会走回头路。
- AwakenedOne P1 (L520-521) — ✅ 原版阈值在 roll 上（form1 r<25 / awakened r<50）+历史守卫；重生置 firstTurn=true（damage() offset 325）⇒ form2 开 DARK_ECHO。我方 AwakenedOne.cs:130-141 RollHundred()（每回合缓存 0-99）谓词逐分支等价。
- Maw P1 (L522-523) — ⚠️ 部分成立。「NOM 卡 1 击」确已修复（自增移入 bands 首谓词 Maw.cs:84,125-134），但与原版仍差一次自增：原版 getMove 入口无条件 turnCount++（Maw.txt:320-327）且开局意图必经 getMove（Maw 无 usePreBattleAction/ctor setMove）⇒ 开局那次也自增(tc 1→2)；我方 opening 路径不经过 bands ⇒ 敌方第 k 回合 NOM 击数 floor(k/2) vs 原版 floor((k+1)/2)，k>=3 奇数回合少 1 击（每击 5 伤）。引擎调用时序已核实：CombatManager.AfterCreatureAdded 开局 RollMove、Creature.PrepareForNextTurn 每敌方回合一次、MonsterMoveStateMachine.FindNextMoveState 的 !_performedFirstMove 早退保证开局不会提前走 bands。
- Snecko P1 (L524-525) — ✅ 原版链 firstTurn GLARE → r<40 TAIL → lastTwo(BITE)?TAIL:BITE（Snecko.txt:414-493）；我方 Snecko.cs:71-74 四谓词完全一致。

### M8. 7.2 区间其余裁定
- AngryPower 拒绝裁定 (L269) — ✅ 复核成立：onAttacked 门控 = owner!=null && dmg>0 && !HP_LOSS && !THORNS（angrypower.txt）。
- SlimeBoss 分裂 1×SpikeSlimeL + 1×AcidSlimeL 非 ×2 (L271) — ✅ die() 各一个 SpawnMonsterAction（SlimeBoss.txt:345-370）；我方 SlimeBoss.cs:170-183 ToMutable+SpawnHp 继承当前 HP 各 Add 一只。
- Acid 配对 Slimed 挂 11/12 与 7/8 (L270) — ✅ L spit 11/12+Slimed2、M spit 7/8+Slimed1，tackle 纯攻击；我方常量与 StatusIntent 构造一致。
- SlaverRed Entangle 25%/turn + stabRun<2&&roll>=55 (L275) — ✅ 原版 firstTurn STAB；num>=75&&!usedEntangle→ENTANGLE；num>=55&&usedEntangle&&!lastTwo(STAB)→STAB；base scrape lastTwo。我方 SlaverRed.cs:61-83 谓词链逐条等价（三招均置 _everMoved）。

---

## 二、事件数值（Phase 2 log，DEVLOG L60-69）

| # | 断言 | 裁定 | 关键证据 |
|---|---|---|---|
| E1 | CursedTome 1/3-1/2-确定 | ✅ | 候选序 Necronomicon→Enchiridion→Nilry's Codex（各仅当未持有）、空则 Circlet、list.get(miscRng.random(size-1))；random(int)=nextInt(range+1) 含上界 ⇒ 概率正确。我方 CursedTome.cs:106-135 同构（Rng.NextInt(count) 等价）。交付方式差异已在注释披露。 |
| E2 | MoaiHead 333 金 | ✅ | goldAmount ConstantValue=int 333（javap -v 实证）；流程=玩家交出 Golden Idol（先 loseRelic 后 RainingGoldEffect(333)+gainGold(333)）；槽位恒显示、未持有转锁定文案。我方 OfferIdol 先 Remove 再 GainGold ✓。注：DEVLOG 句子须读作「收购」金像；操作细节无误。 |
| E3 | GoldenIdolEvent 先给后巨石 | ✅ | spawnRelicAndObtain@119 先于 screenNum=1@141；已持有时给 Circlet(offset 74)。我方 TakeTheIdol 先 Obtain 再进 BOULDER 页。 |
| E4 | ForgottenAltar 非对称路径 | ✅ | gainChalice(dump:229-305)：已持 BloodyIdol ⇒ 只 spawn Circlet 且保留 GoldenIdol（logMetricRelicSwap 属误导性调用，确如所述）；否则 onUnequip+instantObtain(player,idx) 原位替换。引擎 RelicCmd.Replace=IndexOf→Remove→Obtain(idx)（RelicCmd.cs:74-84），我方正用之。 |
| E5 | MindBloom 999 金+双 Normality+升级全部 | ✅ | Rich: sipush 999×3 处(logMetric/RainingGold/gainGold)+两个独立 new Normality() ShowCardAndObtainEffect；Awake: 遍历 canUpgrade 全升级+直接 makeCopy(Mark of the Bloom) 无 Circlet 兜底；Healthy: new Doubt()+heal(maxHealth)（heal 量=maxHP 非直接 set）；第三选项由 floorNum%50<=40 分流。我方 IAmAwake/IAmRich/IAmHealthy 一一对应。 |
| E6 | TombRedMask 222 金 | ✅ | GOLD_AMT ConstantValue=222（javap -v）；[Don]=持 RedMask 才可选、效果为 gainGold(222)（收入非支出）；[Offer]=loseGold(all)+得 RedMask；OPTIONS[2]/[3] 夹 live gold。我方一致。注：「pays 222 gold」措辞有方向歧义，字节码方向是收入。 |
| E7 | DrugDealer 两选项免费 | ✅ | 全类 0 处 loseGold/gainGold/increaseMaxHp/damage/decreaseMaxHealth（grep 计数=0）；MutagenicStrength 已持有→Circlet 替代。我方相同。 |
| E8 | AccursedBlacksmith Pain→WarpedTongs 硬编码 | ✅ | Rummage 分支 new Pain() 先 ShowCardAndObtain、后 spawnRelicAndObtain(new WarpedTongs())——无稀有度掷无兜底(dump:147-219)。我方 AddCurseToDeck<Pain>→Obtain<WarpedTongs>。 |
| E9 | Bonfire 诅咒→SpiritPoop | ✅ | CURSE 臂（metric 标签「Offered Curse」实证 switch 映射）=hasRelic("Spirit Poop")?Circlet:SpiritPoop（setReward case1）；其余臂 Basic 无奖/Common&Special heal5/Uncommon 全愈/Rare +10MaxHP&全愈；选卡 getPurgeableCards 不排诅咒、引擎 FromDeckForRemoval 仅滤 Eternal(CardSelectCmd.cs:739-742) ⇒ 我方实现成立。 |
| E10 | WindingHalls [Focus] 加 Writhe | ✅ | Focus=heal(healAmt)+new Writhe()（ShowCardAndObtainEffect，dump:419-477）；Embrace Madness=2×独立 Madness(dump:221-232)。我方 Heal(25%)+AddCurseToDeck<Writhe>。 |

---

## 三、统计

24 项明细裁定：**✅ 19 / ⚠️ 2 / ❌ 3**
- ❌ 三项均为「DEVLOG 文字 vs 字节码」层错误（M3 SpikeSlime 表述、M4 SlaverBlue 表述、M5 ~80% 数字与守卫方向括注）——当前代码无一需要回滚，三处代码反而是对的。
- ⚠️ 两项：Maw NOM off-by-one（唯一真实玩法影响：奇数回合>=3 时 NOM 少 1 击×5 伤）；AcidSlime 附带（平权重近似量化偏差 + slimes A17 表门控在 StS2 第 9 级升格与 L276「落底档」政策冲突）。
- 文档一致性提醒：L274 括注已被 7.5 自纠但留有旧文；L518-519 的 WrithingMass 修法描述已被 Session 15 取代。
- 已知先验对账：Feed 谓词、Armaments+3 属卡牌域，不在本域，未重验。
