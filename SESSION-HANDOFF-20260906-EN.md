# SESSION HANDOFF - sts2-spire1 (EN) - 2026-09-06

STATUS: The previous conversation was abandoned by the user because
non-approved script characters leaked into the chat after a handoff.
This file is the clean restart point. It supersedes
SESSION-HANDOFF-20260906.md (kept as history, scrubbed, Chinese).
All on-disk content was scrubbed to approved scripts (see section 12).
Start the next conversation by reading this file first.

## 1. Project

- sts2-spire1: Slay the Spire 1 (StS1) content mod for Slay the Spire 2
  (StS2). C# / .NET 9, BaseLib mod loader.
- Root: G:\omp works\sts2-spire1
- Repo: https://github.com/Twelve-eight/sts2-spire1 (branch master).
  git identity configured (Twelve-eight). Backup snapshot hook present.
- Governing docs (read first): G:\omp works\AGENTS.md (rules 1-11),
  DEVELOP.md (design/contracts; section 9 = review dispositions),
  DEVLOG.md (session 31 final + session 31.5 scrub note),
  docs/CODE-REVIEW-20260904.md (R1-R15 wave),
  research/kb/PLAN-2026-09-05.md (P1-P13 checklist).

## 2. Machine / build environment

- NEVER write to C: (nearly full). All caches on G:.
- Build env (bash):
  DOTNET_CLI_HOME=G:\omp works\sts2-spire1\.dotnethome
  NUGET_PACKAGES=G:\omp works\sts2-spire1\.nuget
  TEMP=G:\omp works\sts2-spire1\.tmp
  TMP=G:\omp works\sts2-spire1\.tmp
- JAVA_HOME must be C:\Program Files\Zulu\zulu-21 for javap/jar/audit
  (PATH java is Zulu 25 - always set explicitly).
- Build: cd mod && dotnet build -c Release. Deploys dll/pck/json to
  G:/steam/steamapps/common/Slay the Spire 2/mods/Spire1/ automatically.
- Game: G:\steam\steamapps\common\Slay the Spire 2.
  StS1 jar (read-only): G:\steam\steamapps\common\SlayTheSpire\desktop-1.0.jar.
- Workshop mods: AFTP 3746969593, Act4Heart 3747537811,
  Toggler2 3787796638, MPRebalance 3785039319.
- PowerShell writes a BOM to .cs/.java -> use the Write tool.
  Inline $ in bash loses vars -> write .ps1/.mjs files.
  Use the grep tool, not shell grep.
- git push uses proxy 127.0.0.1:7897; fallback:
  git -c http.proxy= -c https.proxy= push

## 3. Subagent routing / supervision

- All roles = agentrouter/glm-5.3:max via ~/.omp/agent/config.yml modelRoles.
- scout = read-only research; task = execution; reviewer = code review.
- Pair every code-writing subagent with a reviewer (AGENTS rules 4 and 8).
- Workers WRITE code only, SKIP build/lint/tests; main session builds
  centrally (this discipline caught missing usings in the R6 batch).
- Run node tools/check-agent-text.mjs "<prompt>" before dispatching.
- Cap 32 concurrent. Split tasks that exceed ~10 min without completion.

## 4. Git state (all pushed, master)

- 07185e3 docs(devlog): session 31 final
- 616117a fix(review-wave): R12 Fission full rewrite
- 10eeb48 fix(review-wave): R6 nine engine twins
- 2dab66d fix(review-wave): R9/R13/R14 + dispositions + P12 + DEVLOG
- 49c05b3 fix(review-wave): R1/R2/R3/R5/R7
- plus the hygiene-scrub commit from this handoff session (section 12).
- Acceptance: clean git status, no unpushed commits, hook present.

## 5. R-wave (docs/CODE-REVIEW-20260904.md): ALL IMPLEMENTED

- R2 P0: AutoAnthonyCompatBridge.ThirdPartyPoolContentsPrefix/IdsPostfix
  now early-return unless ReferenceEquals(__instance,
  ThirdPartyPoolInstance); instance resolved at Apply time via
  ModelDb.GetById<CardPoolModel>(ModelDb.GetId(poolType)) (WatcherCardPool).
  Fixes Harmony base-getter landing (workshop pool inherits get_AllCards
  from CardPoolModel -> corrupted ALL pools in chaos runs).
- R3 P0: FromSavePostfix/FromHistoryPostfix iterate entries and use
  TryMap(entry, out g) values instead of re-querying EntryMap (WATCHER
  lives only in ThirdPartyEntryMap -> KeyNotFoundException before).
- R1 P1: new Patches/Act3BossRewardPatch.cs, postfix on
  RewardsSet.WithRewardsFromRoom. Triggers only when room.RoomType==Boss
  && Rewards.Count==0 && run.CurrentActIndex==run.Acts.Count-2 && next
  act type name "TheEnding". Adds GoldReward(min,max) + potion roll
  (player.PlayerOdds.PotionReward.Roll) + CardReward(3). Root cause:
  Act4Heart FixAct3Boss_IL_ rewrites Acts.Count-1 -> Count-2 at the
  reward gate while its ModelDb.get_Acts hook appends TheEnding, so
  act-3 boss hits the empty-rewards early return.
- R5 P1: SharedCardReuse.FieldDriftTwins (Claw/Barrage/Flechettes/Chill/
  Darkness) - InjectTwin resolves own implementation for drift twins,
  explicit error if missing. javap-verified.
- R7 P1: 11 rarity fixes (BandageUp/Blind/Finesse/FlashOfSteel/
  GoodInstincts/SwiftStrike/Trip -> Uncommon; Brutality/LimitBreak ->
  Rare; DarkShackles -> Uncommon).
- R6 P1 (10eeb48): Hemokinesis/Offering/PerfectedStrike -> IroncladReuse;
  Shiv (Token, never pooled) and HandOfGreed (already shipped in the
  official ColorlessCardPool; injecting doubles weight) -> no action;
  Slimed/Apotheosis/Discovery/TheBomb -> own faithful classes. Missing
  usings fixed by central build. 4-curse adjudication: Normality/Writhe
  granted as shipped engine cards; CurseOfTheBell/Pride zero grant
  sources -> not implemented.
- R9 P1: ThreeCultistsEncounter + ShelledParasiteAndFungiEncounter
  (HomeActs=[2], bytecode coords, engine-default gold 10-20). Act-2 19/19.
- R12 P1 (616117a): Fission full rewrite - was Focus+Energy with doubling
  upgrade (entirely wrong). Now: 0-cost Rare Skill Exhaust; snapshot
  filledOrbCount n; base = clear orbs then gain n Energy then draw n;
  upgraded = EVOKE all orbs instead (Remove->Evoke text swap only, no
  numeric change). Command API only. Details in section 10.
- R13 P1: Evaluate+ inserts upgraded Insight+ via CombatState.CreateCard
  -> CardCmd.Upgrade -> CardPileCmd.AddGeneratedCardToCombat. Note:
  vanilla StS1 Evaluate.use() never upgrades (official text contradicts
  code); we follow the text per R13 ruling.
- R14 P2: Spire1Config.IgnoreMpHashMismatch (default OFF) gates
  hash-mismatch force-through separately from IgnoreMpModDifferences
  (default ON).
- R10 deferred (needs jar EventHelper.getEvent truth table); R15 accepted
  deviation (Burn upgrade unreachable in StS2 upgrade flow); R8 relic
  layer awaiting user scope decision (full port vs curated) - the ONLY
  open code item.

## 6. PLAN-2026-09-05.md status

- P1 relic pools init L13 (initializeRelicPools) - OPEN
- P2 shop full inventory mechanics - OPEN (reconcile open question 3:
  per-slot exact formulas with research/sts1-kb/mechanics/loot-rewards.md
  section L04-L06)
- P3 ascension KB - IN FLIGHT (see section 7)
- P4 bottled/innate deck matrix - OPEN
- P5-P9 StS2-side KB volumes - OPEN
- P10 porting cookbook - OPEN
- P11 SavedProperty lint (semantics-audit extension) - OPEN
- P12 mechanics README index refresh - DONE
- P13 kb README count refresh - DONE
- Each item: implement, commit, push.

## 7. P3 ascension KB - current state (do not lose)

- Agent cancelled twice after ~55 min silence (last file write 20:20).
  27 javap dumps remain in .tmp/audit/javap-asc/ (AbstractDungeon,
  AbstractPlayer, AbstractMonster, AbstractRoom, TheCity, TheBeyond,
  TheEnding, Exordium, SaveFile, MonsterRoom, MonsterRoomBoss,
  MonsterRoomElite, TreasureRoomBoss, GiantHead, character ctors, some
  monster ctors + com/ tree).
- Facts ALREADY extracted this session (offsets from the dumps):
  - A5: floor-1 HP loss in AbstractDungeon map-gen method, offsets
    ~198-235 (ascensionLevel >= 5 branch, currentHealth decrease).
  - A6: start currentHealth = round(maxHealth * 0.9f), offsets 306-323
    (>= 6).
  - A10: AscendersBane added to masterDeck via addToTop +
    markCardAsSeen, offsets 326-356 (>= 10).
  - A14: decreaseMaxHealth(getAscensionMaxHPLoss()), offsets 278-295
    (>= 14).
  - A13: combat gold reward * 0.75f with MathUtils.round,
    AbstractRoom offsets 633-647 (>= 13). Same method has a "Cursed Run"
    mod check at 661+ (context note).
  - A12: cardUpgradedChance 0.25 -> 0.125 in TheCity/TheBeyond ctors
    (offsets 59-80); TheEnding 0.5 -> 0.25 (offsets 55-76).
  - A18: potionSlots -= 1 in AbstractPlayer ctor offsets 382-397
    (>= 11). VERIFY level number against official sources when writing
    the volume.
  - A20: AbstractMonster.onFinalBossVictoryLogic offsets 3900+
    (>= 20, bossList manipulation - body partially read; finish from dump).
  - SaveFile persists is_ascension_mode + ascension_level.
- NOT yet extracted: A1-A4, A7-A9, A11, A15-A19. A16/A17 location:
  AbstractMonster has only 1 ascensionLevel ref (onFinalBossVictoryLogic)
  and MonsterRoom* have 0 -> check AbstractMonster.setHp/applyPowers and
  individual monster ctors in the dumps, or AbstractDungeon helpers.
- Deliverable: research/sts1-kb/mechanics/ascension.md (A01-A20 with
  javap offsets, follow the pattern of other volumes in that folder);
  add row to research/sts1-kb/mechanics/README.md table (20 rules;
  total 262 -> 282; recompute the table sum; update the bound re-run
  command line); mark P3 done in PLAN; commit + push.
- Recommendation: finish in the main session directly from the dumps.
  If dispatching, hand the agent the seed facts above.

## 8. Smoke-test queue for the user (outstanding proofs)

- R1 tri-state: act-3 boss rewards with key / without key / pure 3-act
  (expect rewards in all three).
- R2 chaos Pandora seed sweep: expect transforms of all
  CHAOS_COLORLESS_* cards, zero WATCHER_* leftovers.
- R9 encounters: Triple Cultists, Parasite and Fungi Beast spawn in Act 2.
- R12 Fission: base clears orbs, gains n energy, draws n; upgraded
  evokes all orbs instead.
- R6 cards: Slimed no-op; Apotheosis Rare no Innate; Discovery
  cross-color 3-choose-1; TheBomb Rare 3-turn 40.
- R13 Evaluate+: upgraded Insight+ lands in draw pile.
- G4 caveat: autoslay detects crashes/asset-missing only; gameplay
  semantics need human eyeball.

## 9. Key decisions (recorded in DEVELOP.md section 9)

- R1 patch self-gates (Boss + empty rewards + CurrentActIndex==Count-2 +
  next act "TheEnding") instead of toggling Act4Heart IL; mathematically
  inert on vanilla/AFTP-only/vanilla-4-act stacks. Ecosystem-patch rule:
  fixes live in our dll, not upstream.
- R2 guard via ThirdPartyPoolInstance reference-equality (mirrors AA's
  own ColorlessPoolContentsPatch discipline).
- Slimed monster-side references keep the ENGINE Slimed (6x
  AddToCombatAndPreview<Slimed> in slimes/SlimeBoss/TimeEater); the
  player layer registers only our no-op StatusCardPool class.
- PerfectedStrike scope: engine PlayerCombatState.AllCards (5 piles) vs
  jar hand+draw+discard (3 piles) treated as engine modeling equivalence.
- TheBomb dual-version coexistence accepted (official Uncommon in pool
  + our Rare, distinct IDs) - DarkShackles precedent.
- Offering audit row = parser artifact (tool var whitelist misses
  HpLossVar/CardsVar; jar magic 3 == engine CardsVar(3), MATCH).
- R14 hash bypass opt-in; mismatch = serialization safety unguaranteed.
- Fission base-branch orb removal via OrbCmd.RemoveSlots(cap)+
  AddSlots(cap): engine has no single RemoveAll; RemoveCapacity dequeues
  without firing orb effects/hooks; AddSlots restores capacity
  (clear-orbs-keep-slots). Documented in Fission.cs.
- 4 curses: Normality/Writhe already shipped as engine cards; Bell/Pride
  zero grant sources -> intentionally not implemented.

## 10. Engine facts pinned (do not re-derive)

- RewardsSet.WithRewardsFromRoom boss early-return at CurrentActIndex >=
  Acts.Count-1 (.tmp/dllsrc/MegaCrit.Sts2.Core.Rewards/RewardsSet.cs
  :85-101); boss composition at :238-241 (gold + RollForPotionAndAddTo +
  CardReward(3)).
- CardModel.Pool reverse-lookup: ModelDb.AllCardPools.FirstOrDefault(p =>
  p.AllCardIds.Contains(id)) (CardModel.cs:306).
- GetDefaultTransformationOptions uses original.Pool (CardFactory.cs
  :170-175).
- AA ColorlessPoolContentsPatch = base-getter patch + instance-type
  guards; PandorasBoxChaosPatch -> CardCmd.Transform ->
  CardFactory.CreateRandomCardForTransform(card, false, rng).
- SharedCardReuse: InjectTwin(pool, twin) -> RarityDriftTwins +
  FieldDriftTwins -> ResolveOwnImplementation (Assembly.GetType(
  "Spire1.Spire1Code.Cards."+name), BaseType.Name=="Spire1Card"); else
  ModHelper.AddModelToPool. AddOwnImplementations used in PureSts1Pools
  mode. NEVER inject a card the official ColorlessCardPool already ships
  (double weight via ConcatModelsFromMods).
- Fission (R12): 0-cost Rare Skill Exhaust; snapshot filledOrbCount n;
  base: OrbCmd.RemoveSlots(cap)+AddSlots(cap) then GainEnergy(n)+
  Draw(n); upgraded: OrbCmd.EvokeNext x n then GainEnergy(n)+Draw(n);
  OrbEvokeType None/All hover highlighting; loc uses SimpleLoc -x-+y+
  swap markers; Exhaust suffix omitted (engine auto-renders keyword).
- MoveType enum: Before=0, AfterLabel=1, After=2.

## 11. Commands / gates / known audit artifacts

- Fidelity audit:
  JAVA_HOME="C:\Program Files\Zulu\zulu-21" node tools/audit-card-fidelity.mjs
  --scope=all (javap cache .tmp/audit/javap/; extracts on demand).
- Gates: node tools/pool-audit.mjs && node tools/semantics-audit.mjs
  (0 orphans / all checks ok).
- Deploy check: md5sum mod/.godot/mono/temp/bin/Release/Spire1.dll
  "G:/steam/steamapps/common/Slay the Spire 2/mods/Spire1/Spire1.dll"
  (was f50e1dfacc11bb9898d4384831ba3288 at session end; re-verify after
  the scrub rebuild).
- Type presence: "C:\Users\o_Obl\.dotnet\tools\ilspycmd.exe" -l c <dll>
- Known audit artifacts: Offering [reuse] magic jar=3 impl=2 (parser
  whitelist misses HpLossVar/CardsVar); Slimed status/target noise;
  Token/Status/Special intentional; monster-HP 3 rows pre-adjudicated
  parse noise (TimeEater NOPARSE, Transient NOJAR, WrithingMass MISMATCH).

## 12. Language-hygiene scrub (this session, committed)

- Directive: no non-approved-script characters in on-disk content.
  Approved = Chinese/English/French/German/Russian text + ASCII control
  + ASCII punctuation.
- Applied .tmp/scrub-illegal.mjs: 12007 flagged chars replaced in 639
  files. Map: arrows -> "->", em/en dashes -> "-", ellipsis -> "...",
  multiplication sign -> "x", curly quotes -> ASCII quotes (JSON-escaped
  where needed), checkboxes -> [x]/[ ], section sign -> "Sec ", circled
  numbers -> "N)", box drawing -> ASCII, WARN for warning signs, BOM
  stripped, degree/superscript/middle-dot -> " deg"/"^2"/"-".
- KEPT: CJK ideographs and CJK punctuation (zhs localization, Chinese
  docs), accented Latin (FR/DE), Cyrillic (RU), guillemets.
- EXCLUDED (vendored upstream material, left verbatim):
  research/BaseLib-StS2 (BaseLib source incl. jpn/kor localization),
  research/baselib-dll, research/engine-dllsrc (engine decompile incl.
  the NLanguageDropdown language-name table), research/_decomp,
  research/ModTemplate-StS2, research/templates.
- All JSON re-validated after scrub; build/deploy refreshed; mechanical
  gates re-run. Re-scan shows zero flagged chars outside the excluded
  trees.
- FUTURE RULE: never write arrows, em/en dashes, ellipsis, mult signs,
  curly quotes, checkboxes, circled numbers, box drawing, or any
  non-approved script in any file a model may ingest. ASCII equivalents
  only, plus CJK/accented-Latin/Cyrillic text. Re-run
  .tmp/scan-illegal.mjs after writing new content; fix before commit.
