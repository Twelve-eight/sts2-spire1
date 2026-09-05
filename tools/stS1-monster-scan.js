// stS1-monster-scan.js — per-monster hook-surface census (data layer v0).
// Usage: node tools/stS1-monster-scan.js  → monsters-scan.json + console summary.
// Flags each concrete monster class for: custom rollMove, takeTurn (all),
// changeState (phase transforms), escape paths, usePreBattleAction/Universal,
// die()/damage() overrides, summon helpers (summon?), special heal().
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const javap = 'C:/Program Files/Zulu/zulu-21/bin/javap.exe';
const root = 'G:/omp works/sts2-spire1/research/sts1-kb/.tmp-javap/cls/com/megacrit/cardcrawl/monsters';
const rows = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.class') && !e.name.includes('$')) {
      const rel = path.relative(root, p).replace(/\\/g, '/').replace('.class', '');
      let dis = '';
      try { dis = execFileSync(javap, ['-c', '-p', p], { encoding: 'utf8', maxBuffer: 1e7 }); } catch (err) { continue; }
      const flags = {
        rollMoveOverride: /public void rollMove\(\)/.test(dis),
        changeState: /void changeState\(java\.lang\.String\)/.test(dis),
        escape: /(public void escape\(\)|public void escapeNext\(\))/.test(dis),
        preBattle: /void usePreBattleAction\(\)/.test(dis),
        universalPreBattle: /void useUniversalPreBattleAction\(\)/.test(dis),
        dieOverride: /void die\(boolean\)|public void die\(\)/.test(dis),
        damageOverride: /public void damage\(com\.megacrit\.cardcrawl\.cards\.DamageInfo\)/.test(dis),
        healOverride: /public void heal\(int\)/.test(dis),
        summon: /Summon\w*Action|makeMinions|Add\d*Minion/.test(dis),
      };
      rows.push({ monster: rel, ...flags });
    }
  }
})(root);
rows.sort((a, b) => a.monster.localeCompare(b.monster));
fs.writeFileSync('G:/omp works/sts2-spire1/research/sts1-kb/monsters-scan.json', JSON.stringify(rows, null, 1));
const agg = {};
for (const k of Object.keys(rows[0]).filter(k => k !== 'monster')) agg[k] = rows.filter(r => r[k]).length;
console.log('monsters:', rows.length);
for (const [k, v] of Object.entries(agg)) console.log(`  ${k}: ${v}`);
for (const r of rows.filter(r => r.rollMoveOverride || r.escape || r.summon)) {
  console.log('  special:', r.monster, Object.entries(r).filter(([k, v]) => k !== 'monster' && v).map(([k]) => k).join(','));
}
