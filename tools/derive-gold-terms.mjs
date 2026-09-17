#!/usr/bin/env node
/**
 * derive-gold-terms.mjs - regenerate the GOLD_TERMS list embedded in check-loc-markup.mjs.
 *
 * WHY: a hand-maintained keyword list is a guess that rots silently. The first two repair
 * passes used one and both missed 集中 (Focus), which is bare in five Spire1 cards and is
 * exactly the "yellow next to white" defect the user reported. Derive the set instead.
 *
 * SOURCES (authoritative only):
 *   (a) .tmp/f05-verify/zhs-{relics,powers,card_keywords}.json - engine text. A term the
 *       engine wraps in [gold] at least THRESHOLD times is one the engine treats as a
 *       highlighted unit.
 *   (b) the mod's own zhs tables - any term already written as *X* or [gold]X[/gold] is
 *       by definition intended as a unit here; leaving it bare elsewhere is an internal
 *       inconsistency regardless of what the engine does.
 *
 * THRESHOLD = 11 is load-bearing: 打击 / 防御 (card-NAME fragments, e.g. the cards "打击"
 * and "防御") sit at exactly 10 and must stay bare. Verify that before lowering it.
 *
 * BARE_BY_DESIGN / NEVER are measured exclusions, not guesses: the engine leaves 充能球
 * bare in 22 of 24 occurrences, and 平静 / 真言 appear unwrapped.
 *
 * Usage: node tools/derive-gold-terms.mjs        (prints the array to paste into the guard)
 */
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const VERIFY = join(ROOT, "..", "..", ".tmp", "f05-verify");
const LOC = join(ROOT, "mod", "Spire1", "localization", "zhs");

const THRESHOLD = 11;
// Bare by design in the engine (measured): wrapping these renders wrong.
const BARE_BY_DESIGN = new Set(["充能球", "平静", "真言", "充能球栏位", "闪电充能球", "未被格挡", "覆甲"]);
// Card-name fragments, never keywords.
const NEVER = new Set(["打击", "防御"]);

// (a) authority gold counts
const counts = {};
for (const f of ["zhs-relics.json", "zhs-powers.json", "zhs-card_keywords.json"]) {
  const d = JSON.parse(readFileSync(join(VERIFY, f), "utf8"));
  for (const [k, v] of Object.entries(d)) {
    if (typeof v !== "string" || k.endsWith(".title")) continue;
    for (const m of v.matchAll(/\[gold\]([^[\]]+?)\[\/gold\]/g)) counts[m[1]] = (counts[m[1]] || 0) + 1;
  }
}

// (b) the mod's own units
const units = new Set();
for (const name of ["cards", "relics", "powers"]) {
  const d = JSON.parse(readFileSync(join(LOC, `${name}.json`), "utf8"));
  for (const [k, v] of Object.entries(d)) {
    if (typeof v !== "string" || k.endsWith(".title") || k.endsWith(".flavor")) continue;
    for (const m of v.matchAll(/\*([^*]+)\*/g)) units.add(m[1]);
    for (const m of v.matchAll(/\[gold\]([^[\]]+?)\[\/gold\]/g)) units.add(m[1]);
  }
}

const usable = (w) => w.length >= 2 && w.length <= 6 && !/[{}[\]?%]/.test(w) && !BARE_BY_DESIGN.has(w) && !NEVER.has(w);
const derived = Object.entries(counts).filter(([w, c]) => c >= THRESHOLD && usable(w)).map(([w]) => w);
let terms = Array.from(new Set([...derived, ...[...units].filter(usable)]));
// Drop any term that merely contains a shorter one (e.g. 金币时 -> keep 金币).
terms = terms.filter((w) => !terms.some((o) => o !== w && o.length < w.length && w.includes(o)));
terms.sort();

console.log(`// authority(>=${THRESHOLD}): ${derived.length}, mod units: ${[...units].filter(usable).length} -> ${terms.length} terms`);
console.log(`// near-threshold (must stay bare): ${Object.entries(counts).filter(([w, c]) => c === THRESHOLD - 1 && usable(w)).map(([w, c]) => `${w}:${c}`).join(" ") || "(none)"}`);
console.log(JSON.stringify(terms, null, 2));
