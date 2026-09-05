// StS2 hook matrix scanner: Hook.cs dispatches virtual methods on AbstractModel;
// this extracts which model files override which hooks.
// Usage: node scan-sts2-hooks.mjs
import fs from 'node:fs';
import path from 'node:path';
const root = 'G:/omp works/sts2-spire1/research/engine-dllsrc';
const hookSrc = fs.readFileSync(path.join(root, 'MegaCrit.Sts2.Core.Hooks/Hook.cs'), 'utf8');
const hookNames = [...new Set([...hookSrc.matchAll(/await model\.([A-Z]\w+)\(/g)].map(m => m[1]))].sort();
const scanDirs = [
  'MegaCrit.Sts2.Core.Models',
  'MegaCrit.Sts2.Core.Models.Cards',
  'MegaCrit.Sts2.Core.Models.Monsters',
  'MegaCrit.Sts2.Core.Models.Powers',
  'MegaCrit.Sts2.Core.Models.Relics',
  'MegaCrit.Sts2.Core.Entities.Orbs',
  'MegaCrit.Sts2.Core.Entities.Enchantments',
  'MegaCrit.Sts2.Core.Entities.Potions',
];
const overrides = {}; // hook -> [{area, name}]
for (const dir of scanDirs) {
  const d = path.join(root, dir);
  if (!fs.existsSync(d)) continue;
  (function walk(x) {
    for (const e of fs.readdirSync(x, { withFileTypes: true })) {
      const p = path.join(x, e.name);
      if (e.isDirectory()) walk(p);
      else if (e.name.endsWith('.cs')) {
        const src = fs.readFileSync(p, 'utf8');
        const cls = e.name.replace(/\.cs$/, '');
        for (const h of hookNames) {
          const re = new RegExp('override\\s+(?:async\\s+)?Task(?:<[^>]*>)?\\s+' + h + '\\b');
          if (re.test(src)) (overrides[h] ??= []).push(dir.split('.').pop() + '/' + cls);
        }
      }
    }
  })(d);
}
fs.writeFileSync('G:/omp works/sts2-spire1/research/kb/sts2-hook-matrix.json', JSON.stringify(overrides, null, 1));
console.log('hooks with implementers:', Object.keys(overrides).length, '/', hookNames.length);
for (const h of Object.keys(overrides).sort()) {
  console.log('##', h, overrides[h].length);
  console.log(overrides[h].join(', '));
}
