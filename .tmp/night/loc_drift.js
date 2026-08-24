// loc_drift.js — compare our zhs/eng card descriptions against the official StS1 KB.
// Our key form: "SPIRE1-<SLUG>.description" (flat strings).  KB: {id:"Bash", description_zh, ...}
import fs from 'fs';
import path from 'path';

const R = process.cwd().replace(/\\/g, '/');
const KB = R + '/research/sts1-kb';
const ours = {};
for (const lang of ['zhs', 'eng'])
  ours[lang] = JSON.parse(fs.readFileSync(`${R}/mod/Spire1/localization/${lang}/cards.json`, 'utf8'));

const kbCards = [];
for (const f of fs.readdirSync(KB)) {
  if (!/^cards-.*\.json$/.test(f)) continue;
  kbCards.push(...JSON.parse(fs.readFileSync(path.join(KB, f), 'utf8')));
}
// SLUG -> jar id: STRIKE->Strike, THUNDER_CLAP->ThunderClap
const camel = slug => slug.toLowerCase().replace(/(^|_)([a-z])/g, (_, __, c) => c.toUpperCase());
const kbById = new Map(kbCards.filter(c => c.id && c.description_en).map(c => [c.id.replace(/\s+/g, ''), c]));

// collect card entries (exclude .title / .smartDescription)
const ids = new Set();
for (const k of Object.keys(ours.zhs))
  if (k.startsWith('SPIRE1-') && k.endsWith('.description')) ids.add(k.slice(7, -'.description'.length));

const norm = s => (s || '').replace(/\s+/g, '').toLowerCase();
const stripPh = s => (s || '')
  .replace(/![\w.]+!/g, '!X!')
  .replace(/\{[\w.]+\}/g, '{X}')
  .replace(/\bNL\b/gi, ' ')
  .replace(/#y|#r|#b|#g|#/gi, '');

function sim(a, b) {
  const A = [...norm(stripPh(a))], B = [...norm(stripPh(b))];
  if (!A.length || !B.length) return 0;
  const dp = new Array(B.length + 1).fill(0);
  for (const ca of A) {
    let prev = 0;
    for (let j = 1; j <= B.length; j++) {
      const t = dp[j];
      if (ca === B[j - 1]) dp[j] = prev + 1;
      else if (dp[j - 1] > dp[j]) dp[j] = dp[j - 1];
      prev = t;
    }
  }
  return dp[B.length] / Math.max(A.length, B.length);
}

let rows = [], noKb = [];
for (const slug of ids) {
  const kb = kbById.get(camel(slug));
  const oz = ours.zhs[`SPIRE1-${slug}.description`];
  const oe = ours.eng[`SPIRE1-${slug}.description`];
  if (!kb) { noKb.push(slug); continue; }
  rows.push({ slug, szh: sim(oz, kb.description_zh), sen: sim(oe, kb.description_en), oz, kz: kb.description_zh });
}
rows.sort((a, b) => a.szh - b.szh);

let md = `# 本地化漂移报告（vs 官方 StS1 原文）\n\n生成：${new Date().toISOString()}\n\n` +
  `- 我方卡描述条目：${ids.size}\n- 对上官方 KB：${rows.length}\n- KB 未命中：${noKb.length}（StS2 原生复用项/衍生牌属正常）\n\n` +
  `## 相似度最低 30 条（人工复核队列）\n\n| 卡 | zhs | eng |\n|---|---|---|\n`;
for (const r of rows.slice(0, 30)) md += `| ${r.slug} | ${(r.szh * 100).toFixed(0)}% | ${(r.sen * 100).toFixed(0)}% |\n`;
md += `\n## zhs 相似度 <85% 明细\n`;
for (const r of rows.filter(r => r.szh < 0.85))
  md += `\n### ${r.slug}（${(r.szh * 100).toFixed(0)}%）\n- 我方：${r.oz}\n- 官方：${r.kz}\n`;
md += `\n## KB 未命中清单\n${noKb.map(s => '- ' + s).join('\n')}\n`;
fs.writeFileSync(R + '/research/kb/loc-drift-report.md', md);
console.log(`entries=${ids.size} matched=${rows.length} noKb=${noKb.length}`);
console.log('worst12:', rows.slice(0, 12).map(r => `${r.slug}:${(r.szh * 100) | 0}%`).join(' '));
