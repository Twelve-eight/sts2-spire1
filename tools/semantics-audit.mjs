// semantics-audit.mjs — mechanical subset of the semantics review gate
// (research/kb/semantics-review-checklist.md). Runs the automatable checks:
//   P4  [Pool] attribution lint          (delegates to pool-audit.mjs)
//   P1  pool-object exclusion patterns   (allowed only as Remove+Id-subtract pairs;
//        whitelist: Patches/SplashOwnSetSubtractPatch.cs)
//   I7  selection-screen cards vs .selectionScreenPrompt loc keys (both langs)
// Usage: node tools/semantics-audit.mjs   → exit 0 clean / 1 findings.
// The semantic questions (P2/P3/P5-P8, M1-M4) stay reviewer-driven — see checklist.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const findings = [];
const notes = [];

// ---- P4: pool attribution (reuse pool-audit.mjs) --------------------------
let p4 = '';
try {
  p4 = execFileSync('node', [path.join(root, 'tools', 'pool-audit.mjs')], { encoding: 'utf8' });
} catch (e) {
  p4 = e.stdout ?? String(e);
}
if (/no resolvable \[Pool\]/.test(p4)) findings.push('P4: [Pool] orphans present — see output above');
notes.push(['P4 pool attribution', /ok.*every concrete class resolves/.test(p4) ? 'ok' : 'FINDINGS']);

// ---- P1: pool-object exclusion --------------------------------------------
const codeRoot = path.join(root, 'mod', 'Spire1Code');
const whitelist = /SplashOwnSetSubtractPatch\.cs$/; // Remove kept for vanilla parity, then Id-subtract
const p1hits = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.cs')) {
      const src = fs.readFileSync(p, 'utf8');
      const rel = path.relative(root, p);
      if (/Remove\(\s*[\w.]*CardPool\s*\)/.test(src) && !whitelist.test(rel)) {
        p1hits.push(rel);
      }
      // pool-object comparison exclusion (no Id-level follow-up check here — reviewer verifies)
      if (/Where\(\s*\w+\s*=>\s*\w+\.(CardPool|Pool)\s*!=/.test(src) && !whitelist.test(rel)) {
        p1hits.push(rel + ' (Where != pool)');
      }
    }
  }
})(codeRoot);
if (p1hits.length) findings.push('P1: pool-object exclusion outside whitelist: ' + p1hits.join(', '));
notes.push(['P1 exclusion patterns', p1hits.length ? 'FINDINGS' : 'ok']);

// ---- I7: selection-screen prompt keys --------------------------------------
const cardsDir = path.join(codeRoot, 'Cards');
const snake = s => s.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toUpperCase(); // card loc ids use UNDERSCORE
const users = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.cs')) {
      const src = fs.readFileSync(p, 'utf8');
      const m = src.match(/class ([A-Za-z0-9_]+)\s*(\(|:)/);
      if (m && m[1] === e.name.replace('.cs', '') &&
          /SelectionScreenPrompt/.test(src)) users.push(m[1]); // only direct property readers need the key; 3-arg FromChooseACardScreen uses generic banner
    }
  }
})(cardsDir);
for (const lang of ['eng', 'zhs']) {
  const loc = JSON.parse(fs.readFileSync(path.join(root, 'mod', 'Spire1', 'localization', lang, 'cards.json'), 'utf8'));
  const keys = new Set(Object.keys(loc));
  const missing = users.filter(c => !keys.has('SPIRE1-' + snake(c) + '.selectionScreenPrompt'));
  // benign-possible: 3-arg FromChooseACardScreen without direct property read (verify at runtime)
  const direct = missing.filter(c => {
    const src = fs.readFileSync(path.join(cardsDir, c + '.cs'), 'utf8');
    return /SelectionScreenPrompt/.test(src);
  });
  const indirect = missing.filter(c => !direct.includes(c));
  if (direct.length) findings.push(`I7[${lang}]: missing .selectionScreenPrompt AND direct property read (throw risk on play): ${direct.join(', ')}`);
  if (indirect.length) notes.push(`I7[${lang}] indirect-missing (verify runtime path): ${indirect.join(', ')}`);
}
notes.push(['I7 selection keys', `checked ${users.length} users`]);

// ---- verdict ----------------------------------------------------------------
console.log('== semantics-audit ==');
for (const [k, v] of notes) console.log(`  [${v === 'ok' ? 'ok' : (v.startsWith('FINDINGS') ? 'FAIL' : 'note')}] ${k}: ${v}`);
if (findings.length) {
  console.log('\nFINDINGS:');
  for (const f of findings) console.log('  - ' + f);
  console.log('\nReviewer checklist: research/kb/semantics-review-checklist.md');
}
console.log(findings.length ? 'VERDICT: request-changes' : 'VERDICT: mechanical checks green (semantic questions still reviewer-driven)');
process.exit(findings.length ? 1 : 0);
