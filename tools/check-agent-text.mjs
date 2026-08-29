#!/usr/bin/env node
// Validate model-bound text. Reject; never mutate evidence.
// Allowed letters: Han, Latin, and Cyrillic. ASCII is always allowed.
// Hiragana, Katakana, Hangul, Arabic, Hebrew, Greek, emoji, and other
// non-approved scripts are rejected by Unicode script properties.
// Han is shared by Chinese and Japanese, so Han-only Japanese text cannot
// be identified perfectly from code points alone. Keep unknown raw text in
// a local file and pass only its path and line range.

import fs from "node:fs";

const allowedScript = /[\p{Script=Han}\p{Script=Latin}\p{Script=Cyrillic}]/u;
const allowedMark = /\p{Mark}/u;
const allowedPunctuation = /\p{Punctuation}/u;
const allowedSeparator = /\p{Separator}/u;

function isAllowed(ch) {
  const cp = ch.codePointAt(0);
  return cp === 0x09 || cp === 0x0a || cp === 0x0d ||
    (cp >= 0x20 && cp <= 0x7e) || allowedScript.test(ch) ||
    allowedMark.test(ch) || allowedPunctuation.test(ch) || allowedSeparator.test(ch);
}

function scanText(text) {
  const findings = [];
  for (const ch of text) {
    const cp = ch.codePointAt(0);
    if (!isAllowed(ch) && !findings.some((f) => f.cp === cp)) {
      findings.push({ cp });
    }
  }
  return findings;
}
function scanValue(value, path, findings) {
  if (typeof value === "string") {
    for (const finding of scanText(value)) {
      if (!findings.some((f) => f.cp === finding.cp)) {
        findings.push({ cp: finding.cp, path });
      }
    }
    return;
  }
  if (Array.isArray(value)) {
    value.forEach((item, index) => scanValue(item, `${path}[${index}]`, findings));
    return;
  }
  if (value && typeof value === "object") {
    for (const [key, item] of Object.entries(value)) {
      scanValue(item, `${path}.${key}`, findings);
    }
  }
}

function formatFindings(findings) {
  return findings.map((finding) =>
    `U+${finding.cp.toString(16).toUpperCase().padStart(4, "0")} at ${finding.path || "text"}`
  ).join(", ");
}

function hookResult(findings) {
  if (findings.length === 0) {
    return { permission: "allow" };
  }
  const detail = formatFindings(findings);
  return {
    permission: "deny",
    user_message: `Text rejected before model dispatch: ${detail}. Use a local path for raw multilingual data.`,
    agent_message: "Remove the non-approved script from the model-bound request, or reference the source file and line range instead."
  };
}

const hookMode = process.argv[2] === "--hook";
let findings;

if (hookMode) {
  try {
    const event = JSON.parse(fs.readFileSync(0, "utf8"));
    findings = [];
    scanValue(event, "$", findings);
    process.stdout.write(JSON.stringify(hookResult(findings)) + "\n");
    process.exitCode = findings.length === 0 ? 0 : 2;
  } catch (error) {
    process.stdout.write(JSON.stringify({
      permission: "deny",
      user_message: "Language firewall could not parse hook input; dispatch was blocked.",
      agent_message: "Fix the language firewall input or run the request again."
    }) + "\n");
    process.exitCode = 2;
  }
} else {
  let input;
  if (process.argv[2] === "--file") {
    input = fs.readFileSync(process.argv[3], "utf8");
  } else if (process.argv.length > 2) {
    input = process.argv.slice(2).join(" ");
  } else {
    input = fs.readFileSync(0, "utf8");
  }
  findings = scanText(input);
  if (findings.length > 0) {
    for (const finding of findings) {
      console.error(`Rejected code point U+${finding.cp.toString(16).toUpperCase().padStart(4, "0")}`);
    }
    process.exitCode = 2;
  } else {
    console.log("agent text accepted");
  }
}
