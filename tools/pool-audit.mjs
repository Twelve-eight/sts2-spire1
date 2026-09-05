// pool-audit.mjs — static [Pool] attribution lint for Spire1 card classes.
// Catches the GeneticAlgorithm class of bugs (missing [Pool] -> silent pool
// inheritance -> wrong-color card; see research/kb/pool-architecture.md I2b).
// Usage: node tools/pool-audit.mjs [cardsDir]
//   cardsDir defaults to mod/Spire1Code/Cards
// Exit code 0 = clean, 1 = findings (usable as PR gate).
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const cardsDir = path.resolve(process.argv[2] ?? path.join(root, 'mod', 'Spire1Code', 'Cards'));

const files = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.cs')) files.push(p);
  }
})(cardsDir);

// pass 1: per-file declarations
const classes = new Map(); // className -> {file, base, pool}
for (const f of files) {
  const src = fs.readFileSync(f, 'utf8');
  for (const m of src.matchAll(/class\s+([A-Za-z0-9_]+)\s*(?:\([^)]*\))?\s*(?::\s*([^\{;\n]+))?/g)) {
    const name = m[1];
    const baseRaw = (m[2] ?? '').split(',')[0] ?? '';
    const base = baseRaw.trim().split(/[(<]/)[0] || null;
    const pool = [...src.matchAll(/\[Pool\(typeof\(\s*([A-Za-z0-9_]+)\s*\)\)\]/g)].map(x => x[1]);
    classes.set(name, { file: path.relative(root, f), base, pools: pool });
  }
}

// pass 2: resolve pool via inheritance chain (within scanned set)
function resolvePool(name, seen = new Set()) {
  if (seen.has(name)) return null; // cycle guard
  seen.add(name);
  const c = classes.get(name);
  if (!c) return null;
  if (c.pools.length > 0) return { pool: c.pools[0], via: name, extra: c.pools.slice(1) };
  if (c.base && classes.has(c.base)) return resolvePool(c.base, seen);
  return null;
}

// SharedCardReuse twins: Cards/<Name> with base Spire1Card registered explicitly
// at runtime via ModHelper.AddModelToPool (no [Pool] attribute — by design).
const reuseSrc = fs.readFileSync(path.join(root, 'mod', 'Spire1Code', 'Character', 'SharedCardReuse.cs'), 'utf8');
const twins = new Set([...reuseSrc.matchAll(/typeof\(Sts2Cards\.([A-Za-z0-9_]+)\)/g)].map(m => m[1]));

const orphans = [], multi = [], byPool = {};
for (const [name, c] of classes) {
  const src = fs.readFileSync(path.join(root, c.file), 'utf8');
  const isAbstract = /\babstract\s+class\s+[A-Za-z0-9_]+\s*[:{]/.test(src);
  if (c.pools.length > 1) multi.push({ name, pools: c.pools, file: c.file });
  const r = resolvePool(name);
  if (r) (byPool[r.pool] ??= []).push(`${name}${r.via !== name ? ` (via ${r.via})` : ''}`);
  else if (!isAbstract) {
    if (c.base === 'Spire1Card' && twins.has(name)) {
      (byPool['<SharedCardReuse twin>'] ??= []).push(name);
    } else {
      orphans.push({ name, base: c.base, file: c.file });
    }
  }
}

console.log(`scanned ${files.length} files, ${classes.size} classes in ${path.relative(root, cardsDir)}`);
for (const p of Object.keys(byPool).sort()) console.log(`  ${p}: ${byPool[p].length}`);
if (multi.length) {
  console.log(`\n[warn] multiple [Pool] attributes (${multi.length}):`);
  for (const o of multi) console.log(`  ${o.name} [${o.pools.join(', ')}] (${o.file})`);
}
if (orphans.length) {
  console.log(`\n[FAIL] no resolvable [Pool] (${orphans.length}) — silent pool inheritance risk:`);
  for (const o of orphans) console.log(`  ${o.name} : ${o.base ?? '??'} (${o.file})`);
} else {
  console.log('\n[ok] every concrete class resolves to a pool');
}
process.exit(orphans.length ? 1 : 0);
