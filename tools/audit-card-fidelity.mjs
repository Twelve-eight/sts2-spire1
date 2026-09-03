#!/usr/bin/env node
// audit-card-fidelity.mjs — three-way card fidelity audit (StS1 jar vs StS2 engine vs Spire1 mod)
//
// Scope:
//   --scope=mod    all mod/Spire1Code/Cards/*.cs vs StS1 jar
//   --scope=reuse  engine twins injected via SharedCardReuse vs StS1 jar
//   --scope=all    both (default)
//
// Authoritative StS1 source: desktop-1.0.jar via javap (constants before putfield of
// baseDamage/baseBlock/baseMagicNumber in <init>; upgradeDamage/upgradeBlock/
// upgradeMagicNumber/upgradeCost + flag assignments in upgrade()).
// KB JSON (research/sts1-kb) supplies cost/cost_upgraded/type/rarity + class→color mapping.
//
// Output: .tmp/audit/card-fidelity-report.json + console summary.
// Parse gaps are flagged (MANUAL/NOJAR/NOPARSE) — never guessed around.

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

const scope = process.argv.includes("--scope=reuse") ? "reuse"
  : process.argv.includes("--scope=mod") ? "mod" : "all";

fs.mkdirSync(CACHE, { recursive: true });

// ---------- KB index (class -> {color, cost, cost_upgraded, type, rarity}) ----------
const kb = new Map();
const kbByNorm = new Map();
for (const f of fs.readdirSync(KB_DIR).filter((x) => x.startsWith("cards-"))) {
  const arr = JSON.parse(fs.readFileSync(path.join(KB_DIR, f), "utf8"));
  for (const c of arr) { kb.set(c.class, { ...c, file: f }); kbByNorm.set(c.class.toLowerCase().replace(/[^a-z0-9]/g,''), { ...c, file: f }); }
}
// sts1data structured numbers (green/blue/purple/colorless/temp) as second source
const data2 = new Map();
for (const f of ["cards-green-blue-purple.json", "cards-colorless.json", "cards-temp.json"]) {
  const p = path.join(ROOT, "research/sts1data", f);
  if (!fs.existsSync(p)) continue;
  for (const c of JSON.parse(fs.readFileSync(p, "utf8"))) data2.set(c.cls, c);
}

const COLOR_PKG = { RED: "red", GREEN: "green", BLUE: "blue", PURPLE: "purple", COLORLESS: "colorless" };

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
    } catch (e) { /* try next package */ }
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
  // javap constant push → number (strip the bytecode offset prefix "12: ")
  if (!tok) return undefined;
  tok = tok.trim().replace(/^\d+:\s*/, "");
  const m = /^(bipush|sipush|ldc\w*)\s+.*?(-?\d+(?:\.\d+)?)/.exec(tok);
  if (m) return Number(m[2]);
  const small = { iconst_m1: -1, iconst_0: 0, iconst_1: 1, iconst_2: 2, iconst_3: 3, iconst_4: 4, iconst_5: 5, fconst_0: 0, fconst_1: 1, fconst_2: 2, dconst_0: 0, dconst_1: 1 };
  if (small[tok.split(/\s+/)[0]] !== undefined) return small[tok.split(/\s+/)[0]];
  return undefined;
}

function parseJar(text, cls) {
  if (!text) return null;
  const lines = text.split("\n").map((l) => l.replace(/\r/, ""));
  // find <init> and upgrade() method ranges
  const starts = [];
  lines.forEach((l, i) => {
    if (/^\s+(public|protected|private|).*\b(init|upgrade)\(/.test(l) || /^\s+public void upgrade\(\)/.test(l)) starts.push(i);
  });
  // ctor = first method whose header contains "(" and "init" via "<init>"? javap shows "public com.megacrit...Claw(...)" — card ctor has class name.
  let ctorStart = -1, upgStart = -1;
  lines.forEach((l, i) => {
    if (new RegExp(`\\b${cls}\\(`).test(l) && ctorStart < 0) ctorStart = i;
    if (/void upgrade\(\)/.test(l) && upgStart < 0) upgStart = i;
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

  const grab = (arr, field) => {
    for (let i = 0; i < arr.length; i++) {
      if (new RegExp(`Field ${field}[:I]`).test(arr[i])) {
        for (let j = i - 1; j >= Math.max(0, i - 4); j--) {
          const n = num(arr[j]);
          if (n !== undefined) return n;
        }
      }
    }
    return undefined;
  };
  const en = (re) => { for (const l of ctor) { const m = re.exec(l); if (m) return m[1]; } return undefined; };
  const enums = {
    type: en(/CardType\.(\w+)/),
    rarity: en(/CardRarity\.(\w+)/),
    target: en(/CardTarget\.(\w+)/),
  };
  const base = {
    damage: grab(ctor, "baseDamage"),
    block: grab(ctor, "baseBlock"),
    magic: grab(ctor, "baseMagicNumber"),
  };
  const upgrades = {
    damage: grabCalls(upg, "upgradeDamage"),
    block: grabCalls(upg, "upgradeBlock"),
    magic: grabCalls(upg, "upgradeMagicNumber"),
    cost: grabCalls(upg, "upgradeCost"),
  };
  const flags = [];
  for (const l of upg) {
    const m = /^\s*\d+: (\w+)/.exec(l);
    for (const f of ["innate", "selfRetain", "exhaust", "exhaustOnUseOnce", "returnToHand", "isInnate", "freeToPlayOnce", "retain"]) {
      if (new RegExp(`Field ${f}\\b`).test(l)) flags.push(`${m && m[1] === "putfield" ? "set" : "?"}:${f}`);
    }
  }
  // capture "true"/"false" values for flags: look 1-2 lines above putfield for iconst_1/0
  const flagVals = [];
  for (let i = 0; i < upg.length; i++) {
    const mm = /Field (innate|selfRetain|exhaust|exhaustOnUseOnce|returnToHand|retain)\b/.exec(upg[i]);
    if (mm) {
      let v;
      for (let j = i - 1; j >= Math.max(0, i - 3); j--) {
        const n = num(upg[j]);
        if (n !== undefined) { v = n; break; }
      }
      flagVals.push(`${mm[1]}=${v}`);
    }
  }
  return { base, upgrades, enums, flagVals: [...new Set(flagVals)] };
}

function grabCalls(lines, call) {
  // upgradeDamage(N) — argument pushed before invoke; take the const preceding the invoke line
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes(call)) {
      for (let j = i - 1; j >= Math.max(0, i - 4); j--) {
        const n = num(lines[j]);
        if (n !== undefined) return n;
      }
      return undefined; // no const visible — treat as unknown
    }
  }
  return undefined;
}

// ---------- engine/mod C# parse ----------
function parseCsharp(src) {
  const ctor = /(?::\s*base|:\s*Spire1Card)\s*\(\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)/.exec(src);
  if (!ctor) return { _fail: "ctor not parsed" };
  const vars = [];
  const varRe = /new (DamageVar|BlockVar|HealVar|IntVar|DynamicVar|MagicVar|EnergyVar|PlasmaVar)\s*\(\s*(-?\d+(?:\.\d+)?)m?\s*(?:,|\))/g;
  let m;
  const varSection = src.slice(0, src.indexOf("OnPlay")) + (src.indexOf("OnPlay") < 0 ? src : "");
  while ((m = varRe.exec(varSection))) vars.push({ kind: m[1], base: Number(m[2]) });
  const onUpg = /protected override void OnUpgrade\(\)\s*(?:=>|{)([\s\S]*?)(?:;|})/.exec(src);
  const upgBody = onUpg ? onUpg[1] : "";
  const deltas = [];
  const dRe = /DynamicVars(?:\.(\w+)|\["([^"]+)"\])\.UpgradeValueBy\(\s*(-?\d+(?:\.\d+)?)m?\s*\)/g;
  while ((m = dRe.exec(upgBody))) deltas.push({ var: m[1] || m[2], by: Number(m[3]) });
  const addKw = [...upgBody.matchAll(/AddKeyword\(CardKeyword\.(\w+)\)/g)].map((x) => "+" + x[1]);
  const rmKw = [...upgBody.matchAll(/RemoveKeyword\(CardKeyword\.(\w+)\)/g)].map((x) => "-" + x[1]);
  const costUp = /UpgradeCost\(\s*(-?\d+)\s*\)/.exec(upgBody);
  return {
    cost: Number(ctor[1]), type: ctor[2], rarity: ctor[3], target: ctor[4],
    vars, upg: deltas, flags: [...addKw, ...rmKw],
    costUp: costUp ? Number(costUp[1]) : undefined,
  };
}

// ---------- reuse list ----------
function reuseTargets() {
  const src = fs.readFileSync(path.join(ROOT, "mod/Spire1Code/Character/SharedCardReuse.cs"), "utf8");
  const targets = [];
  const listRe = /(?:private|internal|public)?\s*(?:static\s+readonly|static)\s+System\.Type\[\]\s+(\w+)\s*=\s*\[([\s\S]*?)\];/g;
  let m;
  while ((m = listRe.exec(src))) {
    const list = m[1], body = m[2];
    for (const t of body.matchAll(/typeof\(Sts2Cards\.(\w+)\)/g)) targets.push({ cls: t[1], list });
  }
  return targets;
}

// ---------- comparison ----------
function cmp(scopeName, cls, jarInfo, csharpInfo, kbInfo) {
  const issues = [];
  const fields = [];
  const push = (name, a, b) => {
    if (a === undefined || b === undefined) { fields.push(`${name}:?(${a}/${b})`); return; }
    if (a !== b) issues.push(`${name}: jar=${b} impl=${a}`);
  };
  const RMAP = { COMMON: "Common", UNCOMMON: "Uncommon", RARE: "Rare", BASIC: "Basic", SPECIAL: "Special", CURSE: "Curse" };
  const TMAP = { ATTACK: "Attack", SKILL: "Skill", POWER: "Power", STATUS: "Status", CURSE: "Curse" };
  const TGT = { ENEMY: "AnyEnemy", AOE: "AllEnemies", SELF: "Self", NONE: "None" };
  if (!jarInfo || jarInfo._fail) return { cls, scope: scopeName, verdict: "NOJAR", detail: jarInfo?._fail || "javap failed" };
  if (!csharpInfo || csharpInfo._fail) return { cls, scope: scopeName, verdict: "NOPARSE", detail: csharpInfo?._fail || "csharp not parsed" };
  if (kbInfo?.cost === -1 && (csharpInfo.cost === 0 || csharpInfo.cost === -1)) {
    // X-cost: StS1 code uses -1, KB records 0 — encoding artifact, not a mismatch
  } else {
    push("cost", csharpInfo.cost, kbInfo ? kbInfo.cost : jarInfo.base.cost);
  }
  const jarBase = jarInfo.base, jarUpg = jarInfo.upgrades;
  const cvars = csharpInfo.vars || [];
  const dmg = cvars.find((v) => v.kind === "DamageVar");
  const blk = cvars.find((v) => v.kind === "BlockVar");
  const others = cvars.filter((v) => v.kind !== "DamageVar" && v.kind !== "BlockVar" && v.kind !== "HealVar");
  push("dmg", dmg?.base, jarBase.damage);
  push("blk", blk?.base, jarBase.block);
  if (jarBase.magic !== undefined || others.length) push("magic", others[0]?.base, jarBase.magic);
  const dU = csharpInfo.upg.find((u) => /^(Damage|damage)$/.test(u.var));
  const bU = csharpInfo.upg.find((u) => /^(Block|block)$/.test(u.var));
  const oU = csharpInfo.upg.find((u) => !/^(Damage|damage|Block|block)$/.test(u.var));
  push("upgDmg", dU?.by, jarUpg.damage);
  push("upgBlk", bU?.by, jarUpg.block);
  if (jarUpg.magic !== undefined || oU) push("upgMagic", oU?.by, jarUpg.magic);
  if (jarUpg.cost !== undefined || csharpInfo.costUp !== undefined) push("upgCost", csharpInfo.costUp, jarUpg.cost);
  push("rarity", csharpInfo.rarity, jarInfo.enums?.rarity ? RMAP[jarInfo.enums.rarity] : (kbInfo ? kbInfo.rarity : undefined));
  push("type", csharpInfo.type, jarInfo.enums?.type ? TMAP[jarInfo.enums.type] : (kbInfo ? kbInfo.type : undefined));
  push("target", csharpInfo.target, jarInfo.enums?.target ? TGT[jarInfo.enums.target] : (kbInfo ? kbInfo.target : undefined));
  const verdict = issues.length ? "MISMATCH" : (fields.length ? "PARTIAL" : "OK");
  return { cls, scope: scopeName, verdict, issues, fields };
}

const rows = [];
if (scope !== "reuse") {
  for (const f of fs.readdirSync(CARDS_DIR).filter((x) => x.endsWith(".cs"))) {
    const cls = f.replace(/\.cs$/, "");
    if (cls.startsWith("Spire1")) continue;
    const src = fs.readFileSync(path.join(CARDS_DIR, f), "utf8");
    const impl = parseCsharp(src);
    const remap = NAME_REMAP[cls];
    const kbi = remap ? { class: remap.cls, color: remap.color } : (kb.get(cls) || kbByNorm.get(cls.toLowerCase().replace(/[^a-z0-9]/g, '')));
    const jar = kbi ? parseJar(javap(kbi.class, COLOR_PKG[kbi.color] || "colorless"), kbi.class) : null;
    rows.push(cmp("mod", cls, jar, impl, kbi));
  }
}
if (scope !== "mod") {
  for (const t of reuseTargets()) {
    const cls = t.cls;
    const src = fs.readFileSync(path.join(DLLSRC, cls + ".cs"), "utf8");
    const impl = parseCsharp(src);
    const remap = NAME_REMAP[cls];
    const kbi = remap ? { class: remap.cls, color: remap.color } : (kb.get(cls) || kbByNorm.get(cls.toLowerCase().replace(/[^a-z0-9]/g, '')));
    const jar = kbi ? parseJar(javap(kbi.class, COLOR_PKG[kbi.color] || "colorless"), kbi.class) : null;
    const r = cmp(`reuse:${t.list}`, cls, jar, impl, kbi);
    r.list = t.list;
    rows.push(r);
  }
}

// cross-check vs sts1data structured numbers (second source) where available
for (const r of rows) {
  const s = data2.get(r.cls);
  if (!s) continue;
  r.sts1data = {
    cost: s.cost, dmg: s.base?.baseDamage, blk: s.base?.baseBlock, magic: s.base?.baseMagicNumber,
    upgDmg: s.upgrade?.upgradeDamage, upgBlk: s.upgrade?.upgradeBlock, upgMagic: s.upgrade?.upgradeMagicNumber,
  };
}

fs.mkdirSync(path.join(ROOT, ".tmp/audit"), { recursive: true });
fs.writeFileSync(path.join(ROOT, ".tmp/audit/card-fidelity-report.json"), JSON.stringify(rows, null, 1));

const count = (v) => rows.filter((r) => r.verdict === v).length;
console.log(`scope=${scope} rows=${rows.length}`);
for (const v of ["OK", "PARTIAL", "MISMATCH", "NOJAR", "NOPARSE"]) console.log(`${v}: ${count(v)}`);
console.log("--- MISMATCH ---");
for (const r of rows.filter((r) => r.verdict === "MISMATCH")) console.log(`${r.cls} [${r.scope}] ${r.issues.join("; ")}`);
console.log("--- NOJAR/NOPARSE ---");
for (const r of rows.filter((r) => r.verdict === "NOJAR" || r.verdict === "NOPARSE")) console.log(`${r.cls} [${r.scope}] ${r.detail}`);
