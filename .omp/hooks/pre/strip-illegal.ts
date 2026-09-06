import type { HookAPI } from "@oh-my-pi/pi-coding-agent/extensibility/hooks";

// Hard request filter: replace every non-approved char in the outgoing LLM
// context with ASCII equivalents before it reaches the provider. Approved =
// ASCII + CJK ideographs/punct + accented Latin (FR/DE) + Cyrillic (RU) +
// guillemets. Same map as repo .tmp/scrub-illegal.mjs.
// Two layers:
//  1. tool_result: strip tool output before it enters history (prevention).
//  2. context: strip the assembled request messages (final gate).
// Fail-open on any error; never blocks or alters tool semantics otherwise.
const KEEP = /[\u4E00-\u9FFF\u3400-\u4DBF\uF900-\uFAFF\u3000-\u303F\uFF00-\uFFEF\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u017F\u0400-\u04FF\u00AB\u00BB\u00DF]/;
const MAP: Record<string, string> = {
  "\u2192": "->", "\u2190": "<-", "\u2194": "<->", "\u21D2": "=>", "\u21C4": "<->", "\u21CC": "<->",
  "\u2191": "^", "\u2193": "v", "\u2197": "^", "\u2198": "v", "\u21A8": "^",
  "\u2014": "-", "\u2013": "-", "\u2015": "-", "\u2012": "-",
  "\u2026": "...", "\u22EF": "...",
  "\u00D7": "x", "\u00F7": "/", "\u00B1": "+/-", "\u00B7": "-", "\u00B0": " deg", "\u00B2": "^2", "\u00B3": "^3",
  "\u2265": ">=", "\u2264": "<=", "\u2260": "!=", "\u2248": "~", "\u223C": "~", "\u2212": "-",
  "\u00A7": "Sec ", "\u00A9": "(c)", "\u00AE": "(R)", "\u2122": "(TM)",
  "\u2605": "*", "\u2606": "*",
  "\u2713": "[x]", "\u2714": "[x]", "\u2717": "[ ]", "\u2718": "[ ]", "\u2705": "[x]",
  "\u26A0": "WARN ", "\uFE0F": "", "\uFEFF": "", "\u00A0": " ",
  "\u274C": "X", "\u2757": "!", "\u2753": "?",
  "\u2460": "1)", "\u2461": "2)", "\u2462": "3)", "\u2463": "4)", "\u2464": "5)",
  "\u2465": "6)", "\u2466": "7)", "\u2467": "8)", "\u2468": "9)", "\u2469": "10)",
  "\u246A": "11)", "\u246B": "12)", "\u246C": "13)", "\u246D": "14)", "\u246E": "15)", "\u246F": "16)",
  "\u2610": "[ ]", "\u2612": "[x]",
  "\u222A": "U", "\u2283": " contains ", "\u2208": " in ", "\u2234": "U+2234",
  "\u279C": "->",
  "\u201C": "\"", "\u201D": "\"", "\u2018": "'", "\u2019": "'", "\u201E": "\"",
};
const BOX = /[\u2500-\u257F]/;
const SHAPE = /[\u25A0-\u25FF]/;

function fixChar(c: string): string {
  if (c >= "\x20" && c <= "\x7E") return c;
  if (c === "\t" || c === "\n" || c === "\r") return c;
  if (KEEP.test(c)) return c;
  const m = MAP[c];
  if (m !== undefined) return m;
  if (BOX.test(c) || SHAPE.test(c)) return "";
  return "";
}

function sanitize(s: string): string {
  let out = "";
  let dirty = false;
  for (const c of s) {
    const r = fixChar(c);
    if (r !== c) dirty = true;
    out += r;
  }
  return dirty ? out : s;
}

function deepStrip(value: unknown, changed: { flag: boolean }): unknown {
  if (typeof value === "string") {
    const r = sanitize(value);
    if (r !== value) changed.flag = true;
    return r;
  }
  if (Array.isArray(value)) {
    let dirty = false;
    const out = value.map((v) => {
      const r = deepStrip(v, changed);
      if (r !== v) dirty = true;
      return r;
    });
    return dirty ? out : value;
  }
  if (value && typeof value === "object") {
    let dirty = false;
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value)) {
      const r = deepStrip(v, changed);
      if (r !== v) dirty = true;
      out[k] = r;
    }
    return dirty ? out : value;
  }
  return value;
}

export default function (pi: HookAPI): void {
  pi.on("tool_result", async (event) => {
    try {
      const changed = { flag: false };
      const content = (event.content ?? []).map((chunk) =>
        chunk && typeof chunk === "object"
          ? deepStrip(chunk, changed)
          : deepStrip(chunk, changed)
      );
      const details = deepStrip(event.details, changed);
      if (!changed.flag) return undefined;
      return { content, details };
    } catch {
      return undefined;
    }
  });

  pi.on("context", async (event) => {
    try {
      const changed = { flag: false };
      const messages = (event.messages ?? []).map((msg) => deepStrip(msg, changed));
      if (!changed.flag) return undefined;
      return { messages };
    } catch {
      return undefined;
    }
  });
}
