#!/usr/bin/env node
// audit-monster-hp.mjs — compare monster HP ranges: Spire1 mod vs StS1 jar bytecode.
// Mod side: MinInitialHp/MaxInitialHp expression literals (incl. AscensionHelperGetValueIfAscension(base, alt)).
// Jar side: setHp(X, Y) constants in <init> + ascension branch (A7 pattern setHp(50,56)).
// Package guess: exordium/city/beyond/ending by StS1 act membership (trial).

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = path.resolve(import.meta.dirname, "..");
const JAR = "G:/steam/steamapps/common/SlayTheSpire/desktop-1.0.jar";
const JAVAP = "C:/Program Files/Zulu/zulu-21/bin/javap.exe";
const CACHE = path.join(ROOT, ".tmp/audit/javap-mon");
const DIR = path.join(ROOT, "mod/Spire1Code/Monsters");
fs.mkdirSync(CACHE, { recursive: true });

const PKGS = ["exordium", "city", "beyond", "ending", "broodmother", "gremlin", "slime", "helper", ""];
// StS1 monster packages mostly exordium/city/beyond/ending + a few subfolders

function javapTrial(cls) {
  const cf = path.join(CACHE, `${cls}.txt`);
  if (fs.existsSync(cf)) return { text: fs.readFileSync(cf, "utf8"), pkg: null };
  for (const p of PKGS) {
    const fqn = p ? `com.megacrit.cardcrawl.monsters.${p}.${cls}` : `com.megacrit.cardcrawl.monsters.${cls}`;
    try {
      const out = execFileSync(JAVAP, ["-p", "-c", "-classpath", JAR, fqn], { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
      fs.writeFileSync(cf, out);
      return { text: out, pkg: p };
    } catch (e) { /* next */ }
  }
  return { text: null, pkg: null };
}

function num(tok) {
  if (!tok) return undefined;
  tok = tok.trim().replace(/^\d+:\s*/, "");
  const m = /^(bipush|sipush|ldc\w*)\s+.*?(-?\d+(?:\.\d+)?)/.exec(tok);
  if (m) return Number(m[2]);
  const small = { iconst_m1: -1, iconst_0: 0, iconst_1: 1, iconst_2: 2, iconst_3: 3, iconst_4: 4, iconst_5: 5 };
  const k = tok.split(/\s+/)[0];
  return small[k] !== undefined ? small[k] : undefined;
}

// jar: find setHp calls in ctor → two consts preceding each invoke
function jarHp(text) {
  if (!text) return null;
  const lines = text.split("\n");
  const calls = [];
  for (let i = 0; i < lines.length; i++) {
    if (/setHp/.test(lines[i]) && /invoke/.test(lines[i])) {
      const args = [];
      for (let j = i - 1; j >= Math.max(0, i - 8) && args.length < 2; j--) {
        const n = num(lines[j]);
        if (n !== undefined) args.unshift(n);
      }
      if (args.length === 2) calls.push(args);
    }
  }
  // ascending-HP variant usually the pair with larger numbers appearing near an A7 check;
  // heuristics: distinct pairs
  const uniq = [...new Set(calls.map((c) => c.join(",")))].map((s) => s.split(",").map(Number));
  return uniq; // e.g. [[48,54],[50,56]]
}

// mod: parse MinInitialHp / MaxInitialHp values (base first, ascension second)
function modHp(src) {
  const val = (re) => re.exec(src);
  const min = /MinInitialHp\s*=>\s*AscensionHelper\.GetValueIfAscension\([^,]+,\s*(\d+)\s*,\s*(\d+)\)/.exec(src)
    || /MinInitialHp\s*=>\s*(\d+)/.exec(src);
  const max = /MaxInitialHp\s*=>\s*AscensionHelper\.GetValueIfAscension\([^,]+,\s*(\d+)\s*,\s*(\d+)\)/.exec(src)
    || /MaxInitialHp\s*=>\s*(\d+)/.exec(src);
  if (!min || !max) return null;
  return {
    min: Number(min[2] ?? min[1]), minAsc: Number(min[2] !== undefined ? min[1] : min[1]),
    max: Number(max[2] ?? max[1]), maxAsc: Number(max[2] !== undefined ? max[1] : max[1]),
  };
}

const rows = [];
for (const f of fs.readdirSync(DIR).filter((x) => x.endsWith(".cs"))) {
  const cls = f.replace(/\.cs$/, "");
  if (cls.startsWith("Spire1") || cls.startsWith("I")) continue; // base + interface
  const src = fs.readFileSync(path.join(DIR, f), "utf8");
  const mh = modHp(src);
  if (!mh) { rows.push({ cls, verdict: "NOPARSE-MOD" }); continue; }
  const jarCls = cls.replace(/SlimeSplit|ISlimeSplitSpawn/, "SlimeBoss");
  const { text } = javapTrial(cls);
  if (!text) { rows.push({ cls, verdict: "NOJAR", mod: mh }); continue; }
  const pairs = jarHp(text);
  if (!pairs || !pairs.length) { rows.push({ cls, verdict: "NOJAR-SETHP", mod: mh }); continue; }
  const match = pairs.some(([a, b]) => a === mh.min && b === mh.max);
  rows.push({ cls, verdict: match ? "OK" : "MISMATCH", mod: mh, jar: pairs });
}
fs.writeFileSync(path.join(ROOT, ".tmp/audit/monster-hp-report.json"), JSON.stringify(rows, null, 1));
console.log("rows:", rows.length, "OK:", rows.filter((r) => r.verdict === "OK").length, "MISMATCH:", rows.filter((r) => r.verdict === "MISMATCH").length);
for (const r of rows.filter((r) => r.verdict !== "OK")) console.log(r.cls, r.verdict, JSON.stringify(r.mod), "jar:", JSON.stringify(r.jar));
