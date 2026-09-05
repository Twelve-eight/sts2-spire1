// stS1-event-pool-usage.js — census which StS1 events use which pool APIs.
// Usage: node tools/stS1-event-pool-usage.js
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
      const raw = fs.readFileSync(p);
      const hasPoolRef = /AbstractDungeon|CardLibrary/.test(raw.toString('latin1'));
      if (!hasPoolRef) continue;
      let dis = '';
      try { dis = execFileSync(javap, ['-c', '-p', p], { encoding: 'utf8', maxBuffer: 1e7 }); } catch (err) { dis = String(err); }
      const uses = [...new Set([...dis.matchAll(/Method (com\/megacrit\/cardcrawl\/(?:dungeons\/AbstractDungeon\.(?:getCard|getRewardCards|returnRandomCurse|returnRandomRelic|getCardWithoutRng)|helpers\/CardLibrary\.\w+))/g)].map(m => m[1].replace('com/megacrit/cardcrawl/', '').replace('dungeons/', '').replace('helpers/', '')))];
      out.push(e.name.replace('.class', '') + ' :: ' + (uses.join(' ') || '(no direct pool-api call)'));
    }
  }
})(dir);
console.log(out.sort().join('\n'));
console.log('total', out.length);
