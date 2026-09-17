#!/usr/bin/env node
/**
 * check-loc-markup.mjs - guard Spire1 localization by running the REAL BaseLib
 * SimpleLoc conversion chain over every entry and asserting the output is clean.
 *
 * WHY THIS EXISTS
 * Spire1's localization was ported from StS1 text, which uses a different rich-text
 * dialect. Spire1 enables BaseLib SimpleLoc (mod/Spire1Code/MainFile.cs:53), whose
 * conversion chain (research/baselib-dll/BaseLib.Patches.Localization/SimpleLoc.cs:115)
 * is:
 *     *word*   -> [gold]word[/gold]     (GoldHighlightRegex)
 *     $word$   -> [blue]word[/blue]     (BlueHighlightRegex)
 *     {Var}    -> variable value        (NormalVariableRegex)
 *     !Var!    -> variable + :diff()    (DiffVariableRegex)
 *     @Var@    -> variable + :inverseDiff()
 *     [E]/[EE] -> energy icon           (EnergyIconsRegex)
 *     -a-+b+   -> {IfUpgraded:show:b|a} (UpgradeSwapRegex)
 *     '#'      -> ESCAPE: strip the '#', then convert NOTHING in the rest
 *
 * THE KEY INSIGHT (this is why the first repair pass was wrong)
 * Do NOT hand-maintain a list of "keywords that must be highlighted". That list is
 * a guess and it silently rots: the first pass used one, and it missed every
 * occurrence of 集中 (Focus) because nobody had listed it. Instead, RUN the real
 * chain and assert that the RESULT contains no leftover markup. Anything the chain
 * cannot consume is a defect by construction, with no list to maintain.
 *
 * DEFECT CLASSES DETECTED (all found by running the chain, not by inspection)
 *  1. literal '*' or '$' in the output  -> unbalanced/unconsumed emphasis marker.
 *     e.g. '将一张*伤口放入你的*抽牌堆*中.' has THREE '*' (odd count), so the chain
 *     emits '[gold]伤口放入你的[/gold]抽牌堆[gold]中[/gold]' - both wrong spans AND
 *     leftover markers. The English source says '*Wound*', i.e. the card name only.
 *  2. '#[colour code]' -> '#rX' etc. Simplify() has no '#' rule, so #r renders as text.
 *  3. '#'-escaped string containing '*X*' -> the escape disables conversion, so the
 *     asterisks render literally. Use [gold]X[/gold] inside an escaped string.
 *  4. 'NL' token -> StS1's newline marker; StS2 does not know it, renders "NL".
 *  5. nested tags ('[gold][gold]') -> '*' inside an existing [colour]..[/colour] span;
 *     the span already emphasises, so the inner '*' must be dropped.
 *  6. title carrying a highlight marker -> all 311 authoritative zhs titles are plain.
 *
 * LESSON (paid for once already): do not conclude "marker X is invalid" from counting
 * X in an official dump. The official text writes [gold] directly because it does not
 * go through SimpleLoc. Check whether the mod enables a converter FIRST.
 *
 * Usage: node tools/check-loc-markup.mjs
 */
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

// decodeURIComponent is required: this workspace lives under a path with a space
// ("G:\omp works"), and URL.pathname percent-encodes it to "omp%20works".
const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const LOC = join(ROOT, "mod", "Spire1", "localization");

// --- The real chain, copied verbatim from SimpleLoc.cs ----------------------
const Gold = /(?<=^|[^/])\*({.+?}|.+?(?=$|[\s*.,|}]))\*?/g;
const Blue = /(?<=^|[^/])\$({.+?}|.+?(?=$|[\s$.,|}]))\$?/g;
const NormalVar = /({)([^:}.]+)([:}])/g;
const DiffVar = /!(.*?)!/g;
const InverseVar = /@(.*?)@/g;
const Energy = /\[(?:(E\?)|(E+))\]/g;
const Plural = /(.*?{)([^{]+?)((?::[^{]*)?}(?:(?:[^{]*?[^{/])|(?:)))\(([^()]+?)\)/g;
const UpgradeSwap = /(?<=^|[^/])(?:(?:-(.+?)-)|(?:\+(.*?[^/])\+))(?:\+(.*?[^/])\+)?/g;

function simplify(loc) {
  if (loc.startsWith("#")) return loc; // Simplify() bails on '#'; ProcessSimpleLoc strips it first
  let s = loc.replace(Gold, "[gold]$1[/gold]");
  s = s.replace(Blue, "[blue]$1[/blue]");
  s = s.split("/*").join("*").split("/$").join("$");
  s = s.replace(NormalVar, "$1$2$3");
  s = s.replace(DiffVar, "{$1:diff()}");
  s = s.replace(InverseVar, "{$1:inverseDiff()}");
  s = s.replace(Energy, (m, q, e) => (q.length > 0 ? "{Energy:energyIcons()}" : e.length ? `{energyPrefix:energyIcons(${e.length})}` : m));
  s = s.replace(Plural, "$1$2$3{$2:plural:|$4}");
  s = s.split("/(").join("(");
  s = s.replace(UpgradeSwap, (m, a, b, c) => `{IfUpgraded:show:${(b || "") + (c || "")}|${a || ""}}`);
  s = s.split("/-").join("-").split("/+").join("+");
  return s;
}

/** What the engine finally receives: ProcessSimpleLoc strips one leading '#',
 *  then converts the rest ONLY when the string was not escaped. */
function effective(value) {
  if (value.startsWith("#")) return value.slice(1); // escaped: no conversion at all
  return simplify(value);
}

/** zhs terms the ENGINE treats as a highlighted [gold] unit.
 *
 *  DERIVED, NOT HAND-WRITTEN. Regenerate with:
 *      node tools/derive-gold-terms.mjs
 *  which takes the union of
 *    (a) terms the engine wraps in [gold] >= 11 times across the authoritative dumps
 *        (.tmp/f05-verify/zhs-{relics,powers,card_keywords}.json), and
 *    (b) terms this mod ALREADY treats as a unit (starred with *..* or already [gold]).
 *  A hand-maintained list is exactly what made the first two repair passes miss 集中 -
 *  it is a guess that rots silently. Do not reintroduce one.
 *
 *  The threshold of 11 is load-bearing: 打击 / 防御 are card-NAME fragments at exactly
 *  10 and must stay bare. */
const GOLD_TERMS = [
    "中毒", "休息处", "伤口", "保留", "先古牌", "免疫", "再生", "冰霜", "凋萎", "击晕",
    "力量", "升级", "变化", "召唤", "吊杀", "君王之剑", "吹哨", "多人游戏牌", "奇迹+", "奥斯提",
    "小刀", "平静", "弃牌堆", "惩恶", "手牌", "打击", "扫荡凝视", "抽牌堆", "探寻", "撕咬",
    "放松", "敏捷", "无实体", "易伤", "晕眩", "格挡", "污染", "活力", "涅奥之怒", "消耗",
    "灵体", "灵魂", "灾厄", "煤灰", "牌组", "玻璃充能球", "真言", "神化", "精英", "能量",
    "至亮之焰", "虚弱", "虚无", "蜡制遗物", "覆甲", "重放", "金币", "铸造", "闪电充能球", "防御",
    "附魔", "集中", "魂缚", "龙涎香",
];
/** Bare BY DESIGN in the engine (measured, not assumed): wrapping these is wrong. */
const BARE_BY_DESIGN = [
    "休息", "充能球", "先古", "商人", "复制", "技能", "技能牌", "攻击牌", "普通", "最大生命",
    "稀有", "能力牌", "负面状态", "遗物", "高塔", "黑暗",
];
/** Longer phrases containing a GOLD_TERM that are themselves distinct, unhighlighted units. */
const GOLD_TERM_EXCEPTIONS = ["未被格挡", "格挡值", "抽牌堆顶部", "抽牌堆顶部的", "闪电充能球", "充能球栏位", "最大生命"];

/** Returns GOLD_TERMs occurring outside every [colour] / *..* span. */
function bareGoldTerms(out) {
  const spans = [];
  for (const m of out.matchAll(/\[(?:gold|blue|red|green|purple)\][\s\S]*?\[\/(?:gold|blue|red|green|purple)\]/g)) spans.push([m.index, m.index + m[0].length]);
  for (const m of out.matchAll(/\*[^*]*\*/g)) spans.push([m.index, m.index + m[0].length]);
  const inside = (i, len) => spans.some(([a, b]) => i >= a && i + len <= b);
  const found = [];
  for (const w of GOLD_TERMS) {
    for (const m of out.matchAll(new RegExp(w, "g"))) {
      if (inside(m.index, w.length)) continue;
      // skip when this occurrence is part of a protected longer phrase
      const ctx = out.slice(Math.max(0, m.index - 6), m.index + w.length + 6);
      if (GOLD_TERM_EXCEPTIONS.some((p) => ctx.includes(p))) continue;
      found.push(w);
      break;
    }
  }
  return found;
}

const problems = [];

for (const lang of ["eng", "zhs"]) {
  for (const name of ["cards", "relics", "powers"]) {
    const path = join(LOC, lang, `${name}.json`);
    let table;
    try {
      table = JSON.parse(readFileSync(path, "utf8"));
    } catch (err) {
      problems.push({ path, key: "-", kind: "unparseable JSON", detail: err.message });
      continue;
    }
    for (const [key, value] of Object.entries(table)) {
      if (typeof value !== "string") continue;
      const out = effective(value);
      const report = (kind) => problems.push({ path, key, kind, detail: `${JSON.stringify(value)} -> ${JSON.stringify(out)}` });

      if (out.includes("*")) report("literal '*' survives conversion - unbalanced emphasis marker (check the '*' count is even and spans the right words)");
      if (out.includes("$")) report("literal '$' survives conversion - unbalanced emphasis marker");
      if (/\bNL\b/.test(out)) report("StS1 'NL' newline token - StS2 renders it literally; use a real newline");
      if (/#[rybgp][A-Za-z0-9]/.test(out)) report("StS1 colour code (#r/#y/#b/#g/#p) - SimpleLoc has no '#' rule, it renders literally");
      // StS1 energy token. The engine has no [R] tag (registered tags are gold/blue/red/
      // green/purple/orange/aqua/pink/... in Core.RichTextTags) and SimpleLoc has no rule
      // for it, so it renders as the literal text "[R]". Authority uses {Energy:energyIcons()}.
      if (/\[[RGYBUW]\]/.test(out)) report("StS1 energy token ([R]/[G]/[B]...) - not a registered engine tag, renders literally; use {Energy:energyIcons()}");
      if (/\[(gold|blue|red|green|purple)\]\[(gold|blue|red|green|purple)\]/.test(out)) report("nested highlight tag - drop the '*' inside an existing [colour] span");
      if (value.startsWith("#") && /\*[^*]+\*/.test(value)) report("'*X*' inside a '#'-escaped string - the escape disables SimpleLoc, so the asterisks render literally; write [gold]X[/gold]");
      // A clause duplicated after the final sentence break is a dangling fragment
      // (markup-neutral, so no markup rule catches it). Require the ENTIRE final
      // clause to already appear earlier - a shared verb ("Gain .. Strength" vs
      // "Gain .. Dexterity") must not trip this.
      {
        const segs = out.split(/[.\u3002]/).filter((s) => s.trim());
        const tail = (segs[segs.length - 1] ?? "").trim();
        const body = out.slice(0, Math.max(0, out.lastIndexOf(tail)));
        if (tail.length >= 12 && body.includes(tail)) report("dangling tail fragment - the final clause duplicates text from earlier in the string");
      }

      if (lang === "zhs") {
        // Whitespace between CJK and a marker: authoritative zhs has none
        // (see .tmp/f05-verify/zhs-relics.json: '获得[blue]{Block}[/blue]点[gold]格挡[/gold].').
        if (/[\u4e00-\u9fff][^\S\n]|[^\S\n][\u4e00-\u9fff]/.test(out)) report("whitespace adjacent to CJK - authoritative zhs has none");
        // Titles never carry highlight markers: all 311 authoritative zhs titles are
        // plain (0 with [gold]/*). Wrapping one makes the whole name render gold.
        if (key.endsWith(".title") && /\[gold\]|\[blue\]|\[red\]|\*/.test(out)) report("highlight marker inside a title - titles are plain text");
        // A term the engine highlights must not be bare here, or it renders white
        // beside gold siblings (the reported "yellow/white mix").
        if (!key.endsWith(".title") && !key.endsWith(".flavor")) {
          const bare = bareGoldTerms(out);
          if (bare.length) report(`bare keyword(s) ${bare.join("/")} - the engine highlights these, so they render white here while gold elsewhere`);
          // The inverse: a term the engine leaves BARE must not be wrapped here.
          // Wrapping it is the same inconsistency seen from the other side, and it
          // was invisible while BARE_BY_DESIGN was a hand-written exclusion list.
          for (const w of BARE_BY_DESIGN) {
            if (new RegExp(`\\[gold\\]${w}\\[/gold\\]`).test(out)) {
              report(`"${w}" is wrapped as a highlight, but the engine leaves it bare - drop the wrapper`);
              break;
            }
          }
          // A [colour] span whose content is not a unit the engine colours is an
          // over-wide span: the emphasis covers the wrong words (the same class of
          // bug as a misplaced '*'). Only non-gold colours are checked, because
          // [gold] legitimately spans multi-word phrases.
          for (const m of out.matchAll(/\[(red|green|purple)\]([\s\S]*?)\[\/\1\]/g)) {
            if (/[\u4e00-\u9fff]/.test(m[2]) && m[2].length > 4) {
              report(`over-wide [${m[1]}] span "${m[2]}" - the engine colours a short unit (usually a card name), not a whole clause`);
              break;
            }
          }
        }
      }
    }
  }
}

if (problems.length === 0) {
  console.log("loc markup: OK (0 problems)");
  process.exit(0);
}

console.log(`loc markup: ${problems.length} problem(s)`);
for (const p of problems) {
  console.log(`  [${p.kind}]`);
  console.log(`    ${p.path.replace(ROOT, "")} :: ${p.key}`);
  console.log(`    ${p.detail}`);
}
process.exit(1);
