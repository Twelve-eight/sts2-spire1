#!/usr/bin/env node
// audit-card-fidelity.mjs - three-way card fidelity audit (StS1 jar vs StS2 engine vs Spire1 mod)
//
// Scope:
//   --scope=mod    all mod/Spire1Code/Cards/*.cs vs StS1 jar
//   --scope=reuse  engine twins injected via SharedCardReuse vs StS1 jar
//   --scope=all    both (default)
//
// Authoritative StS1 source: desktop-1.0.jar via javap (constants before putfield of
// baseDamage/baseBlock/baseMagicNumber in <init>; upgradeDamage/upgradeBlock/
// upgradeMagicNumber/upgradeCost in upgrade()).
// KB JSON (research/sts1-kb) supplies cost/cost_upgraded/type/rarity + class->color mapping.
//
// Output: .tmp/audit/card-fidelity-report.json + console summary.
// Parse gaps are flagged (NOJAR/NOPARSE/NOCLASS/PARTIAL) - never guessed around.
// A missing input (jar, javap, card sources, KB) aborts with exit 2 - never a silent pass.
//
// ---------------------------------------------------------------------------
// RULES (2026-09-15 rewrite; each rule states why it is the rule)
// ---------------------------------------------------------------------------
// R1 ctor forms. The mod has exactly three base-ctor shapes:
//      `: Spire1Card(cost, type, rarity, target)`   (218 cards)
//      `: Spire1Curse()`                            (9 curses; base fixes -1/Curse/Curse/None)
//      `: Spire1Card` + own ctor calling `: base(..)` (1 card: GeneticAlgorithm)
//    The old single regex matched only the first, so curses/statuses reported NOPARSE and
//    most rows lost their enums. All three forms are resolved here.
// R2 absent channels. A channel is compared only when at least one side has a value.
//    Both-absent records nothing (the card simply has no such channel) - this removes the
//    `?(undefined/undefined)` noise that made every row look broken.
// R3 magic. StS1 stores one generic `baseMagicNumber`; StS2/BaseLib split it into semantically
//    named vars (PowerVar<T>, CardsVar, RepeatVar, HpLossVar, EnergyVar, ..) and some cards
//    route it into a DamageVar (Burn/Combust/Omega) or a private const (GeneticAlgorithm).
//    There is therefore no 1:1 var-slot counterpart, and the old "first non-dmg/blk var" pick
//    was a heuristic that produced false mismatches (Concentrate: CardsVar(3) lost to
//    EnergyVar(2)). The grounded rule instead requires the jar's `baseMagicNumber` to be
//    accounted for somewhere in the mod source outside the DamageVar/BlockVar slots *that the
//    jar actually has* (so a coincidental DamageVar cannot mask a wrong magic value, while a
//    DamageVar legitimately carrying the magic role still counts), and the jar's
//    `upgradeMagicNumber` to appear as an UpgradeValueBy delta. Validated on the corpus:
//    150/150 comparable rows pass, and the rule fails on 98/98 cards when the value is
//    destroyed (no vacuous passes).
// R4 target. `TargetType.None` and `TargetType.Self` are equivalent for playable cards:
//    CardModel.TryPlayCard passes null for both (verified in the decompiled CardModel), and
//    the only differing branch is ShowMultiCreatureTargetingVisuals, a cosmetic multi-target
//    highlight. The mod's deliberate None usage is a CardModel convention, not drift.
//    `RandomEnemy` is the flag that enables enemy-select; a jar ALL_ENEMY card whose KB text
//    says "random" may legitimately use it (SwordBoomerang, ThunderStrike).
// R5 rarity. StS1 `SPECIAL` has no single StS2 counterpart - it maps to Token, to Ancient when
//    the card lives in the EventCardPool (mod JAX, mirroring the shipped Apparition), or to
//    Curse for curse-type cards. StS1 statuses carry rarity COMMON while StS2 models statuses
//    as CardRarity.Status (engine's own Burn/Dazed/Wound do exactly this). Unplayable StS1
//    cards use cost -2; StS2 has no -2 and models them as -1 plus CardKeyword.Unplayable
//    (R8). Either encoding is accepted - what is checked is that the card is unplayable.
// R6 declared drift. mod/Spire1Code/Character/SharedCardReuse.cs is the authoritative registry
//    of accepted drift: RarityDriftTwins + FieldDriftTwins name the twins for which the mod
//    injects its OWN faithful class instead of the shipped one. Those rows are compared against
//    the mod's own class (which is what actually gets injected). Drift outside those sets stays
//    a MISMATCH - it is undeclared and must be reported.
// R7 legacy tokens. Cards registered in Spire1LegacyPool are deliberately retired (saved runs
//    reference their ids; BaseLib throws without a Pool). They are reported as EXEMPT with the
//    per-card reason and the concrete jar-vs-mod deltas, never silently dropped.
// R8 cost + unplayability sources. Two encoding gaps that silently disabled checks:
//    (a) The jar never stores cost with `putfield cost`. AbstractCard.<init> takes it as its
//        4th argument, pushed immediately before the `getfield CardStrings.DESCRIPTION` load
//        that supplies the 5th. Probe 2026-09-15 over the 337 cached javap dumps: 0 have
//        `putfield cost`, 337 carry it as the ctor argument. Reading only the field left jar
//        cost undefined for every card and dropped the cost check for 8 rows.
//    (b) StS1 marks unplayable as cost -2; StS2 has no -2 (0/176 engine cards use base(-2,..))
//        and uses CardKeyword.Unplayable instead - the engine's own Burn/Wound/Dazed do this.
//        Keying unplayability off card type alone missed Reflex/Tactician, which are type Skill
//        but still unplayable, and would have masked their cost drift.
//    Together these make the cost channel load-bearing: with them the audit reports the real
//    Reflex/Tactician -2-vs-1 drift that the type-only rule reported as a clean pass.
// ---------------------------------------------------------------------------

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = path.resolve(import.meta.dirname, "..");
const JAR = "G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar";
const JAVAP = "C:/Program Files/Zulu/zulu-21/bin/javap.exe";
const DLLSRC = path.join(ROOT, ".tmp/dllsrc/MegaCrit.Sts2.Core.Models.Cards");
const CARDS_DIR = path.join(ROOT, "mod/Spire1Code/Cards");
const CACHE = path.join(ROOT, ".tmp/audit/javap");
const KB_DIR = path.join(ROOT, "research/sts1-kb");
const REUSE_SRC = path.join(ROOT, "mod/Spire1Code/Character/SharedCardReuse.cs");

const scope = process.argv.includes("--scope=reuse") ? "reuse"
  : process.argv.includes("--scope=mod") ? "mod" : "all";

// ---------- missing-input guard (R: never a silent pass) ----------
{
  const missing = [];
  if (!fs.existsSync(JAR)) missing.push(`StS1 jar: ${JAR}`);
  if (!fs.existsSync(CARDS_DIR)) missing.push(`mod card sources: ${CARDS_DIR}`);
  if (!fs.existsSync(DLLSRC)) missing.push(`engine card decompile: ${DLLSRC}`);
  if (!fs.existsSync(KB_DIR)) missing.push(`StS1 knowledge base: ${KB_DIR}`);
  if (!fs.existsSync(REUSE_SRC)) missing.push(`reuse registry: ${REUSE_SRC}`);
  const kbFiles = fs.existsSync(KB_DIR) ? fs.readdirSync(KB_DIR).filter((x) => x.startsWith("cards-") && x.endsWith(".json")) : [];
  if (!kbFiles.length) missing.push(`KB JSON (cards-*.json) under ${KB_DIR}`);
  if (missing.length) {
    console.error("audit-card-fidelity: ABORT - required input missing:");
    for (const m of missing) console.error(`  - ${m}`);
    console.error("Refusing to emit a report from incomplete inputs (a partial audit reads as a clean one).");
    process.exit(2);
  }
  if (scope !== "reuse" && !fs.existsSync(CACHE)) {
    console.error(`audit-card-fidelity: ABORT - javap cache directory missing: ${CACHE}`);
    console.error("Run once with network/javap available to populate it, or restore the cache.");
    process.exit(2);
  }
}

fs.mkdirSync(CACHE, { recursive: true });

// ---------- KB index (class -> {color, cost, cost_upgraded, type, rarity, target, description_en}) ----------
const kb = new Map();
const kbByNorm = new Map();
for (const f of fs.readdirSync(KB_DIR).filter((x) => x.startsWith("cards-"))) {
  const arr = JSON.parse(fs.readFileSync(path.join(KB_DIR, f), "utf8"));
  for (const c of arr) {
    const rec = { ...c, file: f };
    if (!kb.has(c.class)) kb.set(c.class, rec);
    const norm = c.class.toLowerCase().replace(/[^a-z0-9]/g, "");
    if (!kbByNorm.has(norm)) kbByNorm.set(norm, rec);
  }
}
const COLOR_PKG = { RED: "red", GREEN: "green", BLUE: "blue", PURPLE: "purple", COLORLESS: "colorless" };

// ---------- reuse registry + declared drift (R6) ----------
const reuseLists = {};
{
  const src = fs.readFileSync(REUSE_SRC, "utf8");
  for (const m of src.matchAll(/System\.Type\[\]\s+(\w+)\s*=\s*\[([\s\S]*?)\];/g)) {
    reuseLists[m[1]] = Array.from(m[2].matchAll(/typeof\(Sts2Cards\.(\w+)\)/g), (x) => x[1]);
  }
}
function driftSet(name) {
  const src = fs.readFileSync(REUSE_SRC, "utf8");
  const m = new RegExp(`${name}\\s*=\\s*\\[([\\s\\S]*?)\\];`).exec(src);
  if (!m) return new Set();
  return new Set(Array.from(m[1].matchAll(/"(\w+)"/g), (x) => x[1]));
}
const RARITY_DRIFT = driftSet("RarityDriftTwins");
const FIELD_DRIFT = driftSet("FieldDriftTwins");
const DECLARED_DRIFT = new Set(Array.from(RARITY_DRIFT).concat(Array.from(FIELD_DRIFT)));

// ---------- legacy sink pool (R7) ----------
const LEGACY_EXEMPT = new Map([
  ["BecomeAlmighty", "StS1 Watcher option token (Wish choose-one screen). Unreachable in the mod (no Wish source); registered only so its SPIRE1-* id stays loadable. StS1 models it as an unplayable POWER (cost -2); the mod models it as an unplayable Skill."],
  ["LiveForever", "StS1 Watcher option token (Wish choose-one screen). Unreachable in the mod (no Wish source); registered only so its SPIRE1-* id stays loadable. StS1 models it as an unplayable POWER (cost -2); the mod models it as an unplayable Skill."],
  ["FameAndFortune", "StS1 Watcher option token (Wish choose-one screen). Unreachable in the mod (no Wish source); registered only so its SPIRE1-* id stays loadable. StS1 models it as an unplayable card (cost -2); the mod uses cost 0."],
  ["Beta", "StS1 Watcher token card (tempCards). Unreachable in the mod (no Watcher character); registered only so its SPIRE1-* id stays loadable."],
  ["Miracle", "StS1 Watcher token card (tempCards). Unreachable in the mod (no Watcher character); registered only so its SPIRE1-* id stays loadable."],
  ["Insight", "StS1 Watcher token card (tempCards). Unreachable in the mod (no Watcher character); registered only so its SPIRE1-* id stays loadable."],
  ["Safety", "StS1 Watcher token card (tempCards). Unreachable in the mod (no Watcher character); registered only so its SPIRE1-* id stays loadable."],
  ["Smite", "StS1 Watcher token card (tempCards). Unreachable in the mod (no Watcher character); registered only so its SPIRE1-* id stays loadable."],
  ["ThroughViolence", "StS1 Watcher token card (tempCards). Unreachable in the mod (no Watcher character); registered only so its SPIRE1-* id stays loadable."],
  ["Expunger", "StS1 Watcher token card (tempCards), spawned by a card that does not exist in the mod. Registered only so its SPIRE1-* id stays loadable."],
  ["Omega", "StS1 Watcher token card (tempCards), spawned by Beta which is itself unreachable. Registered only so its SPIRE1-* id stays loadable."],
  ["JAX", "StS1 Colorless SPECIAL card granted only by the Augmenter event (mod/Spire1Code/Events/DrugDealer.cs). StS2 has no SPECIAL bucket; the mod maps it to CardRarity.Ancient + EventCardPool, mirroring the shipped Apparition (documented in JAX.cs)."],
]);

// ---------- javap extraction ----------
const PKG_TRAIL = ["colorless", "curses", "status", "tempCards", "optionCards", "special", "purple", "red", "green", "blue"];
function javap(cls, pkg) {
  const cf = path.join(CACHE, `${cls}.txt`);
  if (fs.existsSync(cf)) return fs.readFileSync(cf, "utf8");
  const pkgs = [pkg, ...PKG_TRAIL.filter((x) => x !== pkg)];
  for (const p of pkgs) {
    try {
      const out = execFileSync(JAVAP, ["-p", "-c", "-classpath", JAR, `com.megacrit.cardcrawl.cards.${p}.${cls}`], { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
      fs.writeFileSync(cf, out);
      return out;
    } catch { /* try next package */ }
  }
  return null;
}
// mod class name -> StS1 jar class name + color override (basic strikes/defends per color)
const NAME_REMAP = {
  Strike: { cls: "Strike_Red", color: "RED" }, Defend: { cls: "Defend_Red", color: "RED" },
  StrikeDefect: { cls: "Strike_Blue", color: "BLUE" }, DefendDefect: { cls: "Defend_Blue", color: "BLUE" },
  StrikeSilent: { cls: "Strike_Green", color: "GREEN" }, DefendSilent: { cls: "Defend_Green", color: "GREEN" },
  StrikeWatcher: { cls: "Strike_Purple", color: "PURPLE" }, DefendWatcher: { cls: "Defend_Watcher", color: "PURPLE" },
  Void: { cls: "VoidCard", color: "COLORLESS" },
};

function num(tok) {
  // javap constant push -> number (strip the bytecode offset prefix "12: ")
  if (tok === undefined || tok === null) return undefined;
  const s = String(tok).trim().replace(/^\d+:\s*/, "");
  const m = /^(?:bipush|sipush|ldc\w*)\s+.*?(-?\d+(?:\.\d+)?)/.exec(s);
  if (m) return Number(m[1]);
  const small = { iconst_m1: -1, iconst_0: 0, iconst_1: 1, iconst_2: 2, iconst_3: 3, iconst_4: 4, iconst_5: 5, fconst_0: 0, fconst_1: 1, fconst_2: 2, dconst_0: 0, dconst_1: 1 };
  const head = s.split(/\s+/)[0];
  return small[head];
}

// last constant pushed before the given field/call, searching upward a few instructions
function constBefore(lines, from, back = 4) {
  for (let j = from - 1; j >= Math.max(0, from - back); j--) {
    const n = num(lines[j]);
    if (n !== undefined) return n;
  }
  return undefined;
}

function parseJar(text, cls) {
  if (!text) return null;
  const lines = text.split("\n").map((l) => l.replace(/\r/, ""));
  let ctorStart = -1, upgStart = -1;
  lines.forEach((l, i) => {
    if (ctorStart < 0 && new RegExp(`\\b${cls}\\(`).test(l)) ctorStart = i;
    if (upgStart < 0 && /void upgrade\(\)/.test(l)) upgStart = i;
  });
  if (ctorStart < 0 || upgStart < 0) return { _fail: "ctor/upgrade method not found" };
  const nextHeader = (from) => {
    for (let i = from + 1; i < lines.length; i++) {
      if (/^\s+(public|protected|private)\s/.test(lines[i]) && /\(/.test(lines[i]) && !lines[i].trim().startsWith("//")) return i;
    }
    return lines.length;
  };
  const ctor = lines.slice(ctorStart, nextHeader(ctorStart));
  const upg = lines.slice(upgStart, nextHeader(upgStart));

  const fieldVal = (arr, field) => {
    for (let i = 0; i < arr.length; i++) {
      if (new RegExp(`Field ${field}[:I]`).test(arr[i])) return constBefore(arr, i);
    }
    return undefined;
  };
  // Cost is NOT stored via `putfield cost` anywhere in the corpus (probe 2026-09-15:
  // 0/337 cached javap dumps have it in <init>); AbstractCard.<init> takes it as the 4th
  // argument, pushed immediately before the `getfield CardStrings.DESCRIPTION` load that
  // supplies the 5th. Reading only the field silently dropped the cost check for every card
  // whose cost is not a plain putfield - i.e. all the unplayable ones (-2/-1).
  const ctorArgCost = (arr) => {
    for (let i = 0; i < arr.length; i++) {
      if (!/CardStrings\.DESCRIPTION/.test(arr[i])) continue;
      return constBefore(arr, i, 3);
    }
    return undefined;
  };
  const callArg = (arr, call) => {
    // A constant argument is pushed immediately before the invoke. Anything else (e.g. Blood For
    // Blood's `cost - 1`) is a computed argument and must not be reported as a literal value.
    for (let i = 0; i < arr.length; i++) {
      if (arr[i].includes(call)) return num(arr[i - 1]);
    }
    return undefined;
  };
  const callArgUnknown = (arr, call) => {
    for (let i = 0; i < arr.length; i++) if (arr[i].includes(call)) return num(arr[i - 1]) === undefined;
    return false;
  };
  const en = (re) => { for (const l of ctor) { const m = re.exec(l); if (m) return m[1]; } return undefined; };

  const flags = [];
  for (let i = 0; i < upg.length; i++) {
    const mm = /Field (innate|selfRetain|exhaust|exhaustOnUseOnce|returnToHand|retain)\b/.exec(upg[i]);
    if (mm) flags.push(`${mm[1]}=${constBefore(upg, i)}`);
  }
  return {
    base: { cost: fieldVal(ctor, "cost") ?? ctorArgCost(ctor), damage: fieldVal(ctor, "baseDamage"), block: fieldVal(ctor, "baseBlock"), magic: fieldVal(ctor, "baseMagicNumber") },
    upgrades: {
      damage: callArg(upg, "upgradeDamage"), block: callArg(upg, "upgradeBlock"),
      magic: callArg(upg, "upgradeMagicNumber"), cost: callArg(upg, "upgradeBaseCost"),
      costIsComputed: callArgUnknown(upg, "upgradeBaseCost"),
    },
    enums: { type: en(/CardType\.(\w+)/), rarity: en(/CardRarity\.(\w+)/), target: en(/CardTarget\.(\w+)/) },
    flagVals: Array.from(new Set(flags)),
  };
}

// ---------- engine/mod C# parse ----------
const VAR_KINDS = ["DamageVar", "BlockVar", "HpLossVar", "HealVar", "CardsVar", "EnergyVar", "RepeatVar", "PowerVar", "MagicVar", "IntVar", "DynamicVar", "PlasmaVar"];

function parseCsharp(src) {
  // R1: all three mod base-ctor forms, plus the engine's `: base(..)` form.
  let cost, type, rarity, target, form;
  let m = /:\s*Spire1Card\s*\(\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)\s*\)/.exec(src);
  if (m) { [cost, type, rarity, target] = [+m[1], m[2], m[3], m[4]]; form = "Spire1Card(cost,type,rarity,target)"; }
  if (cost === undefined) {
    m = /:\s*Spire1Curse\s*\(\s*\)/.exec(src);
    if (m) { [cost, type, rarity, target] = [-1, "Curse", "Curse", "None"]; form = "Spire1Curse()"; }
  }
  if (cost === undefined) {
    m = /:\s*Spire1Status\s*\(\s*\)/.exec(src);
    if (m) { [cost, type, rarity, target] = [-1, "Status", "Status", "None"]; form = "Spire1Status()"; }
  }
  if (cost === undefined) {
    m = /:\s*base\s*\(\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)\s*\)/.exec(src);
    if (m) { [cost, type, rarity, target] = [+m[1], m[2], m[3], m[4]]; form = "base(cost,type,rarity,target)"; }
  }
  if (cost === undefined) return { _fail: "card base constructor not parsed (unknown form)" };

  // dynamic vars: BaseLib named vars and engine vars share the `new Kind(<n>m?` shape.
  // TheBomb uses the name-first form `new IntVar(TurnsKey, 3)` / `new IntVar("BombDamage", 40)`.
  const varRe = new RegExp(`new (${VAR_KINDS.join("|")})(?:<([A-Za-z0-9_.]+)>)?\\(\\s*(?:"[^"]*"|[A-Za-z_]\\w*\\s*,)?\\s*(-?[\\d.]+)m?`, "g");
  const vars = [];
  while ((m = varRe.exec(src))) vars.push({ kind: m[1], generic: m[2], base: Number(m[3]) });

  // Calculated-value constructors. The mod uses BaseLib's MakeCalculatedDamage/Block(base, bonus);
  // the engine decompile uses CalculationBaseVar(base) paired with CalculatedDamageVar/CalculatedBlockVar,
  // and ExtraDamageVar/CalculationExtraVar for the per-X bonus. Either way the jar's baseDamage/baseBlock
  // sits in the base slot, which is what the audit compares.
  const calc = [];
  for (const c of src.matchAll(/MakeCalculated(Damage|Block|Var)\s*\(\s*(?:"(\w+)"\s*,\s*)?(-?\d+)/g)) calc.push({ kind: c[1], name: c[2], base: Number(c[3]) });
  for (const c of src.matchAll(/new CalculationBaseVar\(\s*(-?[\d.]+)m?\s*\)/g)) calc.push({ kind: "Base", base: Number(c[1]) });
  for (const c of src.matchAll(/new CalculationExtraVar\(\s*(-?[\d.]+)m?\s*\)/g)) calc.push({ kind: "Extra", base: Number(c[1]) });
  for (const c of src.matchAll(/new ExtraDamageVar\(\s*(-?[\d.]+)m?\s*\)/g)) calc.push({ kind: "Extra", base: Number(c[1]) });
  for (const c of src.matchAll(/new CalculatedDamageVar\(/g)) calc.push({ kind: "CalcDamage" });
  for (const c of src.matchAll(/new CalculatedBlockVar\(/g)) calc.push({ kind: "CalcBlock" });

  // Whole-body OnUpgrade capture: expression-bodied (`=> x = 2;`) and block-bodied both land here.
  const onUpgIdx = src.search(/protected override void OnUpgrade\(\)/);
  let upgBody = "";
  if (onUpgIdx >= 0) {
    const rest = src.slice(onUpgIdx);
    const end = rest.search(/\n\s*(?:public|protected|private|internal)\s/);
    upgBody = end > 0 ? rest.slice(0, end) : rest;
  }
  const deltas = [];
  // `DynamicVars.Power<T>()` (Bash, Caltrops, SpotWeakness, ..), `DynamicVars.Damage`,
  // `DynamicVars["CalcBase"]` - all three addressing forms carry an upgrade delta.
  for (const d of upgBody.matchAll(/DynamicVars(?:\.Power<([A-Za-z0-9_]+)>\(\)|\.(\w+)|\[(?:"([^"]+)"|([A-Za-z_]\w*))\])\.UpgradeValueBy\(\s*(-?[\d.]+)m?\s*\)/g)) {
    deltas.push({ name: d[1] || d[2] || d[3] || d[4], by: Number(d[5]) });
  }
  // Some cards carry the value in a plain int field (`_blockEach = 5`) instead of a DynamicVar, and
  // express the upgrade as a field assignment (`OnUpgrade() => _blockEach = 7`). Capture both so the
  // convention is audited rather than reported as an uncompared channel.
  const fieldInits = new Map();
  for (const c of src.matchAll(/(?:private|protected|internal)\s+(?:readonly\s+)?(?:int|decimal|float)\s+(\w+)\s*=\s*(-?\d+(?:\.\d+)?)m?/g)) {
    fieldInits.set(c[1], Number(c[2]));
  }
  const fieldSets = new Map();
  for (const c of upgBody.matchAll(/(?:^|[^\w.>])(\w+)\s*=\s*(-?\d+(?:\.\d+)?)m?\s*;/g)) fieldSets.set(c[1], Number(c[2]));
  const upgCostCall = /EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)/.exec(upgBody);
  const maxUp = /MaxUpgradeLevel\s*=>\s*(\d+)/.exec(src);

  return {
    cost, type, rarity, target, form, vars, calc, deltas, fieldInits, fieldSets,
    costsX: /HasEnergyCostX\s*=>\s*true/.test(src),
    // Unplayability marker. StS1 encodes it as cost -2; StS2 has no -2 (probe 2026-09-15: 0/176
    // engine cards use base(-2, ..)) and uses CardKeyword.Unplayable instead - the engine's own
    // Burn/Wound/Dazed do exactly this. Reflex/Tactician are type Skill but still unplayable.
    unplayable: /CardKeyword\.Unplayable/.test(src),
    upgCost: (() => { const c = /UpgradeCost\(\s*(-?\d+)\s*\)/.exec(upgBody); return c ? Number(c[1]) : undefined; })(),
    energyCostDelta: upgCostCall ? Number(upgCostCall[1]) : undefined,
    maxUpgradeLevel: maxUp ? Number(maxUp[1]) : undefined,
    addKeywords: Array.from(upgBody.matchAll(/AddKeyword\(CardKeyword\.(\w+)\)/g), (x) => "+" + x[1]),
    rmKeywords: Array.from(upgBody.matchAll(/RemoveKeyword\(CardKeyword\.(\w+)\)/g), (x) => "-" + x[1]),
    _src: src,
  };
}

// ---------- comparison ----------
const RMAP = { BASIC: "Basic", COMMON: "Common", UNCOMMON: "Uncommon", RARE: "Rare", CURSE: "Curse" };
const TMAP = { ATTACK: "Attack", SKILL: "Skill", POWER: "Power", STATUS: "Status", CURSE: "Curse" };
const TGT = { ENEMY: "AnyEnemy", ALL_ENEMY: "AllEnemies", AOE: "AllEnemies", SELF: "Self", NONE: "None" };

function rarityAccepted(jarRarity, modRarity, jarType, modRarityPool) {
  if (jarRarity === undefined) return null;
  if (RMAP[jarRarity] === modRarity) return "direct";
  // R5: StS1 statuses carry COMMON; StS2 models statuses as CardRarity.Status (engine's own Burn/Dazed/Wound)
  if (jarType === "STATUS" && jarRarity === "COMMON" && modRarity === "Status") return "status-convention";
  // R5: StS1 SPECIAL has no single StS2 counterpart
  if (jarRarity === "SPECIAL") {
    if (jarType === "CURSE" && modRarity === "Curse") return "curse-convention";
    if (modRarity === "Token") return "special->Token";
    if (modRarity === "Ancient" && modRarityPool === "EventCardPool") return "special->Ancient(EventCardPool)";
  }
  return false;
}

function targetAccepted(jarTarget, modTarget, kbDesc) {
  if (jarTarget === undefined) return null;
  const mapped = TGT[jarTarget];
  if (mapped === modTarget) return "direct";
  // R4: None <-> Self for playable cards (TryPlayCard passes null for both)
  if ((jarTarget === "SELF" && modTarget === "None") || (jarTarget === "NONE" && modTarget === "Self")) return "None<->Self equivalence";
  // R4: a jar ALL_ENEMY card whose text says "random" may use RandomEnemy
  if (jarTarget === "ALL_ENEMY" && modTarget === "RandomEnemy" && /random/i.test(kbDesc || "")) return "ALL_ENEMY->RandomEnemy (card text says random)";
  // SELF_AND_ENEMY is a StS1 target for "choose a card, then an enemy"; StS2 folds it into AnyEnemy
  if (jarTarget === "SELF_AND_ENEMY" && modTarget === "AnyEnemy") return "SELF_AND_ENEMY->AnyEnemy";
  return false;
}

// R8 calculated values. Body Slam / Heavy Blade / Rampage / Glass Knife / Stack / Steam Barrier /
// Perfected Strike route the jar's base value through a calculated var: BaseLib's
// MakeCalculatedDamage/Block(base, bonus) or the engine's CalculationBaseVar(base) paired with
// CalculatedDamageVar/CalculatedBlockVar. The jar's baseDamage/baseBlock lands in the base slot.
function calcBase(cs, want) {
  // BaseLib MakeCalculated* carries the slot name directly; the engine decompile stores both
  // slots in CalculationBaseVar, disambiguated by the calculated var kind and by the slot name
  // (Perfected Strike's CalculationBaseVar is named "CalculationBase" while its per-Strike bonus
  // lives in ExtraDamageVar, so the base is not a damage channel).
  const named = cs.calc.filter((c) => c.kind === want);
  if (named.length) return named[0].base;
  // Engine decompile form: CalculationBaseVar(base) paired with CalculatedDamageVar /
  // CalculatedBlockVar. The decompiler emits those positionally (no slot name to read), so the
  // calculated var kind is the only disambiguator - and no card in the corpus declares both a
  // damage and a block calculated var, so the single Base var is that channel's base. Verified
  // against the jar: Body Slam baseDamage 0, Stack baseBlock 0, Perfected Strike baseDamage 6
  // are exactly the values in those base slots.
  if (hasCalcFor(cs, want)) {
    const bases = cs.calc.filter((c) => c.kind === "Base");
    if (bases.length === 1) return bases[0].base;
  }
  return undefined;
}
function calcDelta(cs) {
  // the extra/calculation var that carries the upgrade (ExtraDamageVar / CalculationExtraVar /
  // CalculationBaseVar depending on which the card upgraded)
  for (const d of cs.deltas) if (/Extra|Calculation/i.test(d.name)) return d.by;
  return undefined;
}
function calcDeltaFor(cs, want) {
  // only treat a calculation delta as this channel when the card really has a calculated var
  return hasCalcFor(cs, want) ? calcDelta(cs) : undefined;
}
function hasCalcFor(cs, want) {
  return want === "Damage" ? cs.calc.some((c) => c.kind === "CalcDamage") : cs.calc.some((c) => c.kind === "CalcBlock");
}
// a plain int field carrying the card's value (`_blockEach = 5`, `_copies = 1`), and its upgrade
// as a re-assignment (`OnUpgrade() => _blockEach = 7`). The re-assignment is absolute, so the
// effective delta is set-minus-init.
function fieldValue(cs) {
  for (const [name, v] of cs.fieldInits) {
    if (cs.fieldSets.has(name)) return { base: v, delta: cs.fieldSets.get(name) - v, name };
  }
  return undefined;
}

// R3: the magic channel rule (see header). Returns a list of problems.
function magicProblems(src, jarBase, maxUpgradeLevel) {
  if (jarBase.magic === undefined) return [];
  // A card pinned to MaxUpgradeLevel 0 mirrors the base game's unupgradable variant, so the jar's
  // upgrade-only magic path is unreachable (Burn/Decay).
  if (maxUpgradeLevel === 0) return [];
  let code = src.replace(/\/\/[^\n]*/g, "").replace(/\/\*[\s\S]*?\*\//g, "");
  // blank only the slots the jar actually has, so a coincidental DamageVar cannot mask a wrong magic
  if (jarBase.damage !== undefined) code = code.replace(/new DamageVar\(\s*-?[\d.]+m?/g, "new __SLOT__");
  if (jarBase.block !== undefined) code = code.replace(/new BlockVar\(\s*-?[\d.]+m?/g, "new __SLOT__");
  const has = (n) => new RegExp(`(?<![\\d.])${n}(?![\\d.])`).test(code);
  const problems = [];
  if (!has(jarBase.magic)) problems.push(`jar baseMagicNumber ${jarBase.magic} is not accounted for in the implementation`);
  return problems;
}

// R9 magic upgrade. StS1 has one magic number; the mod may apply its upgrade to any semantic var
// (Poison, Vulnerable, Thorns, Strength, ..), so any upgrade delta whose value matches the jar's
// upgradeMagicNumber satisfies the channel. The damage/block upgrade channels are consumed first,
// so Poisoned Stab's Damage+2 cannot also satisfy magic+1.
// Skipped when the mod pins MaxUpgradeLevel to 0: the card never upgrades, so the jar's upgrade
// path is unreachable (e.g. Burn, whose jar upgrades to 4 damage while the mod mirrors the base
// game's unupgradable Burn, documented in-source).
function magicUpgradeProblem(cs, jarUpg, jarBase) {
  if (jarUpg.magic === undefined) return null;
  if (cs.maxUpgradeLevel === 0) return null;
  // Burn / Combust / Omega: the jar's baseMagicNumber IS the card's damage, and the mod models it
  // as a DamageVar whose upgrade is the jar's upgradeMagicNumber. The damage channel already
  // carries the magic role, so requiring a separate spare delta would double-count it.
  if (jarBase.damage === undefined && jarBase.magic !== undefined) {
    const dmg = cs.deltas.find((d) => /^Damage$/i.test(d.name));
    if (dmg && String(dmg.by) === String(jarUpg.magic)) return null;
  }
  const used = new Set();
  const dmg = cs.deltas.find((d) => /^Damage$/i.test(d.name));
  if (dmg) used.add(dmg);
  const blk = cs.deltas.find((d) => /^Block$/i.test(d.name));
  if (blk) used.add(blk);
  const spare = cs.deltas.filter((d) => !used.has(d));
  const candidates = spare.map((d) => String(d.by));
  // A plain int field expresses its upgrade as an absolute re-assignment (`_copies = 1` ->
  // `_copies = 2`), so the delta the jar's upgradeMagicNumber must equal is set-minus-init.
  for (const [name, init] of cs.fieldInits) {
    if (cs.fieldSets.has(name)) candidates.push(String(cs.fieldSets.get(name) - init));
  }
  if (candidates.includes(String(jarUpg.magic))) return null;
  return `jar upgradeMagicNumber ${jarUpg.magic} is not applied as a separate upgrade delta (spare deltas: ${candidates.join(",") || "none"})`;
}

function cmp(scopeName, cls, jarInfo, cs, kbInfo) {
  const issues = [];
  const uncompared = [];
  const notes = [];
  const push = (name, impl, jar, accepted) => {
    if (impl === undefined && jar === undefined) return;             // R2: channel does not exist
    if (impl === undefined || jar === undefined) { uncompared.push(`${name}: jar=${jar} impl=${impl}`); return; }
    if (accepted === false) issues.push(`${name}: jar=${jar} impl=${impl}`);
    else if (accepted && accepted !== "direct") notes.push(`${name}: ${accepted}`);
  };

  if (!jarInfo || jarInfo._fail) return { cls, scope: scopeName, verdict: "NOJAR", detail: jarInfo?._fail || "javap failed" };
  if (!cs || cs._fail) return { cls, scope: scopeName, verdict: "NOPARSE", detail: cs._fail };

  const jarBase = jarInfo.base, jarUpg = jarInfo.upgrades;

  // cost: StS1 uses -2 for unplayable; StS2 uses -1. X-cost is -1 in StS1 and CostX in StS2.
  const jarCost = jarBase.cost;
  const kbCost = kbInfo ? kbInfo.cost : undefined;
  const unplayable = cs.unplayable || cs.type === "Status" || cs.type === "Curse" || jarInfo.enums?.type === "STATUS" || jarInfo.enums?.type === "CURSE";
  if (jarCost === -1) {
    if (!cs.costsX) issues.push(`cost: jar=-1 (X-cost) but the implementation does not set HasEnergyCostX`);
    else notes.push("cost: X-cost (jar -1, HasEnergyCostX => true)");
  } else if (jarCost === -2) {
    if (!unplayable) issues.push(`cost: jar=-2 (unplayable sentinel) but the implementation is playable (type ${cs.type}, no Unplayable keyword)`);
    // Either encoding is faithful: -2 keeps the StS1 literal, -1 is the StS2 sentinel (the engine's
    // own unplayable cards use -1). What matters is that the card is marked unplayable at all.
    else if (cs.cost !== -1 && cs.cost !== -2) issues.push(`cost: jar=-2 (unplayable) impl=${cs.cost} (expected -2 or the StS2 sentinel -1)`);
    else notes.push(`cost: unplayable (jar -2, mod ${cs.cost})`);
  } else if (jarCost !== undefined) {
    push("cost", cs.cost, jarCost, cs.cost === jarCost ? "direct" : false);
  } else if (kbCost !== undefined && kbCost >= 0) {
    push("cost", cs.cost, kbCost, cs.cost === kbCost ? "direct" : false);
  }

  const dmg = cs.vars.find((v) => v.kind === "DamageVar");
  const blk = cs.vars.find((v) => v.kind === "BlockVar");
  const fld = fieldValue(cs);
  // R8: a calculated var (or a plain int field) carries the jar base value when no DynamicVar does.
  // The field fallback is gated on the jar having that channel so an unrelated int field (e.g.
  // Dual Wield's copy count) is never compared against a damage/block base.
  const implDmg = dmg ? dmg.base : (calcBase(cs, "Damage") !== undefined ? calcBase(cs, "Damage") : (blk === undefined && fld && jarBase.damage !== undefined ? fld.base : undefined));
  const implBlk = blk ? blk.base : (calcBase(cs, "Block") !== undefined ? calcBase(cs, "Block") : (dmg === undefined && fld && jarBase.block !== undefined ? fld.base : undefined));
  push("dmg", implDmg, jarBase.damage, implDmg !== undefined && jarBase.damage !== undefined ? (implDmg === jarBase.damage ? "direct" : false) : undefined);
  push("blk", implBlk, jarBase.block, implBlk !== undefined && jarBase.block !== undefined ? (implBlk === jarBase.block ? "direct" : false) : undefined);

  // R3 magic
  for (const p of magicProblems(cs._src, jarBase, cs.maxUpgradeLevel)) issues.push(`magic: ${p}`);
  const muProblem = magicUpgradeProblem(cs, jarUpg, jarBase);
  if (muProblem) issues.push(`magic: ${muProblem}`);

  // upgrade deltas. Cards whose base sits in a calculated var express the upgrade on the
  // extra/calculation var instead of Damage/Block, so fall back to it before reporting a gap.
  const dU = cs.deltas.find((u) => /^Damage$/i.test(u.name)) || (calcDeltaFor(cs, "Damage") !== undefined ? { by: calcDeltaFor(cs, "Damage") } : undefined);
  const bU = cs.deltas.find((u) => /^Block$/i.test(u.name)) || (blk === undefined && calcDeltaFor(cs, "Block") !== undefined ? { by: calcDeltaFor(cs, "Block") } : undefined);
  const implUpgDmg = dU ? dU.by : (blk === undefined && fld && jarUpg.damage !== undefined ? fld.delta : undefined);
  const implUpgBlk = bU ? bU.by : (dmg === undefined && fld && jarUpg.block !== undefined ? fld.delta : undefined);
  push("upgDmg", implUpgDmg, jarUpg.damage, implUpgDmg !== undefined && jarUpg.damage !== undefined ? (implUpgDmg === jarUpg.damage ? "direct" : false) : undefined);
  push("upgBlk", implUpgBlk, jarUpg.block, implUpgBlk !== undefined && jarUpg.block !== undefined ? (implUpgBlk === jarUpg.block ? "direct" : false) : undefined);
  // Cost upgrade semantics differ by side: StS1 `upgradeBaseCost(n)` sets the cost to the ABSOLUTE
  // value n (0 means "costs 0 after upgrade", e.g. Body Slam 1->0), while StS2 `EnergyCost.UpgradeBy(n)`
  // adds a delta. Normalise the StS2 side to the resulting absolute cost before comparing.
  const implCostDelta = cs.upgCost !== undefined ? cs.upgCost : cs.energyCostDelta;
  if (jarUpg.costIsComputed) {
    // Blood For Blood: StS1 branches on the current cost before calling upgradeBaseCost, so the
    // jar's target cost is not a constant. The mod's unconditional UpgradeBy(-1) is the correct
    // model for the common branch; the difference is not auditable from bytecode constants.
    uncompared.push(`upgCost: jar computes the target cost from the current cost (mod: ${implCostDelta ?? "none"})`);
  } else if (jarUpg.cost !== undefined) {
    const implAbsolute = implCostDelta === undefined ? undefined : (cs.upgCost !== undefined ? cs.upgCost : cs.cost + implCostDelta);
    push("upgCost", implAbsolute, jarUpg.cost, implAbsolute === jarUpg.cost ? "direct" : false);
  } else if (implCostDelta !== undefined) {
    uncompared.push(`upgCost: jar=${jarUpg.cost} impl=${implCostDelta}`);
  }

  // rarity / type / target
  const jarRarity = jarInfo.enums?.rarity;
  const rAcc = rarityAccepted(jarRarity, cs.rarity, jarInfo.enums?.type || (kbInfo && kbInfo.type), cs.pool);
  if (jarRarity !== undefined) push("rarity", cs.rarity, RMAP[jarRarity] || jarRarity, rAcc);
  else if (kbInfo?.rarity) push("rarity", cs.rarity, kbInfo.rarity, cs.rarity === kbInfo.rarity ? "direct" : false);

  const jarType = jarInfo.enums?.type;
  if (jarType !== undefined) push("type", cs.type, TMAP[jarType] || jarType, cs.type === TMAP[jarType] ? "direct" : false);

  const jarTarget = jarInfo.enums?.target;
  const tAcc = targetAccepted(jarTarget, cs.target, kbInfo?.description_en);
  if (jarTarget !== undefined) push("target", cs.target, TGT[jarTarget] || jarTarget, tAcc);
  else if (kbInfo?.target) push("target", cs.target, kbInfo.target, cs.target === kbInfo.target ? "direct" : false);

  const verdict = issues.length ? "MISMATCH" : (uncompared.length ? "PARTIAL" : "OK");
  return { cls, scope: scopeName, verdict, issues, uncompared, notes, form: cs.form };
}

// ---------- build rows ----------
function resolveJar(cls) {
  const remap = NAME_REMAP[cls];
  const kbi = remap ? { class: remap.cls, color: remap.color } : (kb.get(cls) || kbByNorm.get(cls.toLowerCase().replace(/[^a-z0-9]/g, "")));
  const jarCls = kbi ? kbi.class : cls;
  const jar = kbi ? parseJar(javap(kbi.class, COLOR_PKG[kbi.color] || "colorless"), kbi.class) : null;
  return { jar, kbi, jarCls };
}

const rows = [];
if (scope !== "reuse") {
  for (const f of fs.readdirSync(CARDS_DIR).filter((x) => x.endsWith(".cs")).sort()) {
    const cls = f.replace(/\.cs$/, "");
    if (cls.startsWith("Spire1")) continue;   // base classes, not cards
    const src = fs.readFileSync(path.join(CARDS_DIR, f), "utf8");
    const cs = parseCsharp(src);
    if (cs._fail) { rows.push({ cls, scope: "mod", verdict: "NOPARSE", detail: cs._fail }); continue; }
    const poolM = /\[Pool\(typeof\((\w+)\)\)\]/.exec(src);
    cs.pool = poolM ? poolM[1] : undefined;
    const { jar, kbi } = resolveJar(cls);
    const row = cmp("mod", cls, jar, cs, kbi);
    row.pool = cs.pool;
    if (row.verdict === "MISMATCH" && LEGACY_EXEMPT.has(cls)) {
      row.verdict = "EXEMPT";
      row.exemptReason = LEGACY_EXEMPT.get(cls);
      row.exemptIssues = row.issues;
      row.issues = [];
    }
    rows.push(row);
  }
}
if (scope !== "mod") {
  for (const [list, targets] of Object.entries(reuseLists)) {
    for (const cls of targets) {
      // R6: declared drift twins are injected from the mod's own class; everything else from the engine
      const ownPath = path.join(CARDS_DIR, cls + ".cs");
      const useOwn = DECLARED_DRIFT.has(cls) && fs.existsSync(ownPath);
      const srcPath = useOwn ? ownPath : path.join(DLLSRC, cls + ".cs");
      if (!fs.existsSync(srcPath)) {
        rows.push({ cls, scope: `reuse:${list}`, verdict: "NOCLASS", detail: `no source at ${path.relative(ROOT, srcPath)}`, list });
        continue;
      }
      const cs = parseCsharp(fs.readFileSync(srcPath, "utf8"));
      if (cs._fail) { rows.push({ cls, scope: `reuse:${list}`, verdict: "NOPARSE", detail: cs._fail, list }); continue; }
      const { jar, kbi } = resolveJar(cls);
      const row = cmp(`reuse:${list}`, cls, jar, cs, kbi);
      row.list = list;
      row.source = useOwn ? "mod (declared drift)" : "engine";
      rows.push(row);
    }
  }
}

fs.mkdirSync(path.join(ROOT, ".tmp/audit"), { recursive: true });
const reportPath = path.join(ROOT, ".tmp/audit/card-fidelity-report.json");
fs.writeFileSync(reportPath, JSON.stringify(rows, null, 1));

const VERDICTS = ["OK", "EXEMPT", "PARTIAL", "MISMATCH", "NOJAR", "NOPARSE", "NOCLASS"];
const count = (v) => rows.filter((r) => r.verdict === v).length;
console.log(`scope=${scope} rows=${rows.length}`);
for (const v of VERDICTS) console.log(`${v}: ${count(v)}`);
for (const v of ["MISMATCH", "EXEMPT", "PARTIAL", "NOJAR", "NOPARSE", "NOCLASS"]) {
  const rs = rows.filter((r) => r.verdict === v);
  if (!rs.length) continue;
  console.log(`--- ${v} ---`);
  for (const r of rs) {
    if (v === "MISMATCH") console.log(`${r.cls} [${r.scope}] ${r.issues.join("; ")}`);
    else if (v === "EXEMPT") console.log(`${r.cls} [${r.scope}] ${r.exemptIssues.join("; ")} :: ${r.exemptReason.split(".")[0]}`);
    else if (v === "PARTIAL") console.log(`${r.cls} [${r.scope}] uncompared: ${r.uncompared.join("; ")}`);
    else console.log(`${r.cls} [${r.scope}] ${r.detail}`);
  }
}
console.log(`\nreport: ${path.relative(ROOT, reportPath).replace(/\\/g, "/")}`);
