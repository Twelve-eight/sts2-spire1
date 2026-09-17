#!/usr/bin/env node
/**
 * derive-gold-terms.mjs - regenerate the GOLD_TERMS / BARE_BY_DESIGN lists embedded in
 * check-loc-markup.mjs.
 *
 * WHY THIS EXISTS
 * A hand-maintained keyword list is a guess that rots silently. The first two repair
 * passes used one and BOTH missed 集中 (Focus) - bare in five Spire1 cards, and exactly
 * the "yellow next to white" defect the user reported. Derive the list instead.
 *
 * THE RULE (dominance, not an absolute count)
 * For each candidate term, count occurrences wrapped as a unit ([gold]X[/gold] or *X*)
 * versus bare, in the engine corpus and in the mod's own text. A term is GOLD when
 * wrapped >= 2 and >= 2x bare; BARE when bare >= 2 and >= 2x wrapped; otherwise NEUTRAL
 * (not checked - the evidence does not support a rule).
 *
 * An absolute-count threshold was tried first and was WRONG: it read my own earlier
 * over-wrapping as evidence. 攻击牌 has authority gold=2, bare=63 - an absolute
 * threshold wrapped it 12 times, and the engine in fact leaves card-type words bare.
 *
 * SOURCE PRECEDENCE
 * The engine corpus wins wherever it has an opinion (>= 2 occurrences), because it is
 * the authority. Only for terms the engine does not use at all (StS1-only, e.g. 平静 /
 * 真言 / 惩恶) does the mod's own majority decide.
 *
 * Usage: node tools/derive-gold-terms.mjs   (prints both arrays to paste into the guard)
 */
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const VERIFY = join(ROOT, "..", "..", ".tmp", "f05-verify");
const LOC = join(ROOT, "mod", "Spire1", "localization", "zhs");

const usable = (w) => w.length >= 2 && w.length <= 6 && !/[{}[\]?%]/.test(w);
const esc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

const engine = [];
for (const f of ["zhs-relics.json", "zhs-powers.json", "zhs-card_keywords.json"]) {
  const d = JSON.parse(readFileSync(join(VERIFY, f), "utf8"));
  for (const [k, v] of Object.entries(d)) if (typeof v === "string" && !k.endsWith(".title")) engine.push(v);
}
const mod = [];
for (const n of ["cards", "relics", "powers"]) {
  const d = JSON.parse(readFileSync(join(LOC, `${n}.json`), "utf8"));
  for (const [k, v] of Object.entries(d)) {
    if (typeof v === "string" && !k.endsWith(".title") && !k.endsWith(".flavor")) mod.push(v);
  }
}

/** {g, b} = occurrences wrapped as a unit vs bare. */
function tally(corpus, t) {
  const re = new RegExp(esc(t), "g");
  let g = 0, b = 0;
  for (const v of corpus) {
    const covered = [];
    for (const m of v.matchAll(new RegExp(`(\\[gold\\]|\\*)${esc(t)}(\\[/gold\\]|\\*)`, "g"))) {
      covered.push([m.index, m.index + m[0].length]);
    }
    for (const m of v.matchAll(re)) {
      if (covered.some(([a, z]) => m.index >= a && m.index + t.length <= z)) g++;
      else b++;
    }
  }
  return { g, b };
}

const cands = new Set();
for (const corpus of [engine, mod]) {
  for (const v of corpus) for (const m of v.matchAll(/\[gold\]([^[\]]+?)\[\/gold\]/g)) cands.add(m[1]);
}
// Seed from the guard's CURRENT lists. Without this the generator is not idempotent:
// a term that is (correctly) unwrapped everywhere no longer appears as a [gold] unit in
// any corpus, so it would drop out of the candidate set and vanish from the list -
// losing the rule that keeps it unwrapped. Seeding makes regeneration stable.
{
  const guard = readFileSync(join(ROOT, "tools", "check-loc-markup.mjs"), "utf8");
  for (const arr of ["GOLD_TERMS", "BARE_BY_DESIGN"]) {
    const m = guard.match(new RegExp(`const ${arr} = \\[([\\s\\S]*?)\\n\\];`));
    if (m) for (const w of m[1].matchAll(/"([^"]+)"/g)) cands.add(w[1]);
  }
}

const gold = [], bare = [];
for (const t of cands) {
  if (!usable(t)) continue;
  const e = tally(engine, t);
  const m = tally(mod, t);
  const src = e.g + e.b >= 2 ? e : m; // engine wins where it has an opinion
  if (src.g + src.b === 0) continue;
  if (src.g >= 2 && src.g >= 2 * Math.max(src.b, 1)) gold.push(t);
  else if (src.b >= 2 && src.b >= 2 * Math.max(src.g, 1)) bare.push(t);
}
gold.sort(); bare.sort();

const fmt = (a) => {
  const lines = [];
  for (let i = 0; i < a.length; i += 10) lines.push("  " + a.slice(i, i + 10).map((w) => `"${w}"`).join(", ") + ",");
  return lines.join("\n");
};
console.log(`// GOLD_TERMS (${gold.length})`);
console.log(fmt(gold));
console.log(`\n// BARE_BY_DESIGN (${bare.length})`);
console.log(fmt(bare));
