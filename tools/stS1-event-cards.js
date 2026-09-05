// stS1-event-cards.js — extract concrete card IDs granted by StS1 events
// (CardLibrary.getCopy/getCard ldc args) and relic ids (returnRandomRelic
// has no args; relic choice is inside the relic). Data layer.
// Usage: node tools/stS1-event-cards.js
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const javap = 'C:/Program Files/Zulu/zulu-21/bin/javap.exe';
const dir = 'G:/omp works/sts2-spire1/research/sts1-kb/.tmp-javap/cls/com/megacrit/cardcrawl/events';
const out = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.class') && !e.name.includes('$')) {
      const raw = fs.readFileSync(p).toString('latin1');
      if (!/CardLibrary/.test(raw)) continue;
      let dis = '';
      try { dis = execFileSync(javap, ['-c', '-p', p], { encoding: 'utf8', maxBuffer: 1e7 }); } catch (err) { continue; }
      // capture ldc string constants immediately preceding CardLibrary.getCopy/getCard invokes
      const lines = dis.split('\n');
      const grants = [];
      for (let i = 0; i < lines.length; i++) {
        if (/helpers\/CardLibrary\.(getCopy|getCard)/.test(lines[i])) {
          for (let j = i - 1; j >= Math.max(0, i - 6); j--) {
            const m = lines[j].match(/String\s+(.+?)\s*$/);
            if (m) { grants.push(m[1]); break; }
          }
        }
      }
      if (grants.length) out.push(e.name.replace('.class', '') + ' :: ' + [...new Set(grants)].join(', '));
    }
  }
})(dir);
console.log(out.sort().join('\n'));
console.log('total', out.length);
