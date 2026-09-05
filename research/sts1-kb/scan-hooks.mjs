const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const base = 'G:/omp works/sts2-spire1/research/sts1-kb/.tmp-javap/cls/com/megacrit/cardcrawl';
const pkg = process.argv[2] || 'powers';
const root = path.join(base, pkg);
const javap = 'C:/Program Files/Zulu/zulu-21/bin/javap.exe';
const PATTERNS = {
  atStartOfTurn:      /void atStartOfTurn\(\)/,
  atStartOfTurnPostDraw: /void atStartOfTurnPostDraw\(\)/,
  duringTurn:         /void duringTurn\(\)/,
  atEndOfTurn:        /void atEndOfTurn\(boolean\)/,
  atEndOfTurnPreEndTurnCards: /void atEndOfTurnPreEndTurnCards\(boolean\)/,
  atEndOfRound:       /void atEndOfRound\(\)/,
  onEnergyRecharge:   /void onEnergyRecharge\(\)/,
  atEnergyGain:       /void atEnergyGain\(\)/,
  justApplied:        /justApplied/,
  stackPower:         /void stackPower\(int\)/,
  reducePower:        /void reducePower\(int\)/,
  onRemove:           /void onRemove\(\)/,
  onInitialApplication: /void onInitialApplication\(\)/,
  onSpecificTrigger:  /void onSpecificTrigger\(\)/,
  onUseCard:          /void onUseCard\(/,
  onAfterUseCard:     /void onAfterUseCard\(/,
  atDamageGive:       /float atDamageGive\(/,
  atDamageReceive:    /float atDamageReceive\(/,
  atDamageFinalGive:  /float atDamageFinalGive\(/,
  atDamageFinalReceive: /float atDamageFinalReceive\(/,
  modifyBlock:        /float modifyBlock\(/,
  onAttackedToChangeDamage: /int onAttackedToChangeDamage\(/,
  onAttackToChangeDamage: /int onAttackToChangeDamage\(/,
  onAttacked:         /int onAttacked\(/,
  onLoseHp:           /int onLoseHp\(/,
  wasHPLost:          /void wasHPLost\(/,
  onHeal:             /int onHeal\(/,
  onExhaust:          /void onExhaust\(/,
  onCardDraw:         /void onCardDraw\(/,
  onChannel:          /void onChannel\(/,
  onEvokeOrb:         /void onEvokeOrb\(/,
  onChangeStance:     /void onChangeStance\(/,
  onScry:             /void onScry\(/,
  onShuffle:          /void onShuffle\(\)/,
  onTrigger:          /void onTrigger\(\)/,
  checkTrigger:       /checkTrigger\(/,
  atTurnStart:        /void atTurnStart\(\)/,
  atTurnStartPostDraw: /void atTurnStartPostDraw\(\)/,
  atBattleStart:      /void atBattleStart\(\)/,
  atBattleStartPreDraw: /void atBattleStartPreDraw\(\)/,
  onPlayerEndTurn:    /void onPlayerEndTurn\(\)/,
  onManualDiscard:    /void onManualDiscard\(\)/,
  onMonsterDeath:     /void onMonsterDeath\(/,
  onVictory:          /void onVictory\(\)/,
  onBloodied:         /void onBloodied\(\)/,
  onEquip:            /void onEquip\(\)/,
  counter:            /\bcounter\b/,
};
const hooks = Object.keys(PATTERNS);
const rows = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.class') && !e.name.includes('$')) {
      const rel = path.relative(base, p).slice(0, -'.class'.length).split(path.sep).join('/');
      let out = '';
      try { out = execFileSync(javap, ['-p', p], { encoding: 'utf8', maxBuffer: 1e7 }); }
      catch (err) { out = 'ERR'; }
      const hits = hooks.filter(h => PATTERNS[h].test(out));
      rows.push({ name: rel, hooks: hits });
    }
  }
})(root);
fs.writeFileSync(`G:/omp works/sts2-spire1/research/sts1-kb/.tmp-javap/${pkg}-scan.json`, JSON.stringify(rows, null, 1));
console.log('total', rows.length);
const byHook = {};
for (const r of rows) for (const h of r.hooks) (byHook[h] ??= []).push(r.name.split('/').pop());
for (const h of hooks) console.log('##', h, (byHook[h] || []).length, ':', (byHook[h] || []).join(','));
