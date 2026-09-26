#!/usr/bin/env node
// scan-hooks.mjs - Slay the Spire 1 knowledge-base hook scanner.
//
// Scans the javap -p output for one package directory below the class root and
// writes an array of { name, hooks } records. PATTERNS keys, regexes and detection
// semantics are unchanged from the previous CommonJS version; argument handling,
// javap invocation, failure semantics and the output transaction were hardened per
// docs/DEVELOP-hook-scan-20260924.md.
//
// The scanner reports declared members found in the extracted class files. It does
// not resolve inherited behaviour, game reachability, localization or card pool
// registration, and it does not claim anything about monsters-scan.json.
//
// Package names, option values and every visited input path are validated before use;
// see HOOK-SCAN.md for the reproduction steps and the still-unverified boundary.

import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = path.dirname(fileURLToPath(import.meta.url));
const DEFAULT_CLASS_ROOT = path.join(
  SCRIPT_DIR,
  ".tmp-javap",
  "cls",
  "com",
  "megacrit",
  "cardcrawl",
);
const DEFAULT_PACKAGE = "powers";
const JAVAP_ARGS_PREFIX = ["-p"];
const JAVAP_TIMEOUT_MS = 60000;
const JAVAP_MAX_BUFFER = 10000000;
const STDERR_SNIPPET_LIMIT = 400;
const JAVAP_NAMES = process.platform === "win32" ? ["javap.exe", "javap"] : ["javap", "javap.exe"];
// Package names must be a single safe directory name. The two classes below are
// disjoint and are checked separately so each refusal names one concrete reason:
// PATH_CONTROL_CHARS covers C0, DEL and C1 controls, UNSAFE_PACKAGE_CHARS covers the
// remaining characters that break path structure or Windows file names.
const PATH_CONTROL_CHARS = /[\u0000-\u001f\u007f-\u009f]/;
const UNSAFE_PACKAGE_CHARS = /[\\/:<>"|?*]/;
const TRAILING_SPACE_OR_DOT = /[ .]$/;
const EDGE_WHITESPACE = /^\s|\s$/u;
const WIN32_RESERVED_DEVICE_NAMES = new Set([
  "con",
  "prn",
  "aux",
  "nul",
  "com1",
  "com2",
  "com3",
  "com4",
  "com5",
  "com6",
  "com7",
  "com8",
  "com9",
  "lpt1",
  "lpt2",
  "lpt3",
  "lpt4",
  "lpt5",
  "lpt6",
  "lpt7",
  "lpt8",
  "lpt9",
  "conin$",
  "conout$",
]);

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

const USAGE = [
  "Usage: node scan-hooks.mjs [package] [options]",
  "",
  "Scans javap -p output for one package directory below the class root and writes",
  "an array of { name, hooks } records to a JSON file.",
  "",
  "Arguments:",
  "  package            single safe directory name below the class root",
  "                     (default: " + DEFAULT_PACKAGE + ")",
  "",
  "Options:",
  "  --base <dir>       class root that contains the package directories",
  "                     (default: <script dir>/.tmp-javap/cls/com/megacrit/cardcrawl)",
  "  --output <file>    output JSON path",
  "                     (default: <script dir>/.tmp-javap/<package>-scan.json)",
  "  --javap <file>     javap executable (default: JAVA_HOME/bin/javap, then PATH)",
  "  --help             print this text and exit 0",
  "",
  "Exit status is non-zero when the class root or package directory is missing, when",
  "no scanable class file is found, or when javap fails, times out or exceeds its",
  "output limit. The output file is only replaced after every class succeeded.",
  "",
  "The package argument must be a single directory name: separators, absolute paths,",
  "traversal segments, control characters and Windows reserved device names are",
  "rejected. Symlinks and reparse points inside the class root are not followed.",
  "",
  "Reproduction notes and the explicit JDK 21 requirement for central verification are",
  "in HOOK-SCAN.md next to this script.",
  "",
].join("\n");

class ScanError extends Error {
  constructor(message, exitCode) {
    super(message);
    this.name = "ScanError";
    this.exitCode = typeof exitCode === "number" ? exitCode : 1;
  }
}

function fail(message, exitCode) {
  throw new ScanError(message, exitCode);
}

function oneLine(value) {
  return String(value).replace(/\s+/g, " ").trim();
}

function describeError(err) {
  return oneLine(err !== null && err !== undefined && err.message ? err.message : err);
}

function quote(value) {
  return JSON.stringify(String(value));
}

function parseArgs(argv) {
  const options = { base: null, output: null, javap: null, help: false, pkg: null };
  const positionals = [];
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === "--help") {
      options.help = true;
      continue;
    }
    if (arg === "--") {
      for (let j = i + 1; j < argv.length; j += 1) positionals.push(argv[j]);
      break;
    }
    const match = /^--(base|output|javap)(?:=(.*))?$/.exec(arg);
    if (match) {
      const key = match[1];
      let value = match[2];
      if (value === undefined) {
        i += 1;
        if (i >= argv.length) fail("option --" + key + " requires a value", 2);
        value = argv[i];
      }
      if (value === "") fail("option --" + key + " requires a non-empty value", 2);
      if (PATH_CONTROL_CHARS.test(value)) {
        fail("option --" + key + " value contains a control character: " + quote(value), 2);
      }
      if (value.trim() === "") fail("option --" + key + " value must not be blank", 2);
      options[key] = value;
      continue;
    }
    if (arg.length > 1 && arg.startsWith("-")) fail("unknown option " + arg, 2);
    positionals.push(arg);
  }
  if (positionals.length > 1) {
    fail("expected at most one package name, received " + positionals.length, 2);
  }
  options.pkg = positionals.length === 1 ? positionals[0] : DEFAULT_PACKAGE;
  return options;
}

function validatePackageName(pkg) {
  if (pkg === "") fail("package name must not be empty", 2);
  if (pkg === "." || pkg === "..") {
    fail("package name must not be a relative traversal segment: " + pkg, 2);
  }
  if (pkg !== path.posix.basename(pkg) || pkg !== path.win32.basename(pkg)) {
    fail("package name must be a single directory name without separators: " + pkg, 2);
  }
  if (path.isAbsolute(pkg)) fail("package name must not be an absolute path: " + pkg, 2);
  if (PATH_CONTROL_CHARS.test(pkg)) {
    fail("package name contains a control character: " + quote(pkg), 2);
  }
  if (UNSAFE_PACKAGE_CHARS.test(pkg)) {
    fail("package name contains a character that is not allowed in a directory name: " + quote(pkg), 2);
  }
  if (TRAILING_SPACE_OR_DOT.test(pkg)) {
    fail("package name must not end with a space or a dot: " + quote(pkg), 2);
  }
  if (EDGE_WHITESPACE.test(pkg)) {
    fail("package name must not start or end with whitespace: " + quote(pkg), 2);
  }
  // Windows resolves CON, NUL, COM1..COM9, LPT1..LPT9 (and the CONIN$/CONOUT$
  // console handles) even with an extension, so such a name is never a real directory.
  if (WIN32_RESERVED_DEVICE_NAMES.has(pkg.split(".")[0].toLowerCase())) {
    fail("package name must not be a Windows reserved device name: " + quote(pkg), 2);
  }
  return pkg;
}

function lstatOrFail(target, label) {
  try {
    return fs.lstatSync(target);
  } catch (err) {
    fail(label + " is not readable: " + target + " (" + describeError(err) + ")");
  }
}

function requireDirectory(target, label) {
  const stats = lstatOrFail(target, label);
  if (stats.isSymbolicLink()) {
    fail(label + " is a symbolic link or reparse point, refusing to follow it: " + target);
  }
  if (!stats.isDirectory()) fail(label + " is not a directory: " + target);
  return stats;
}

function isOutside(parent, child) {
  const rel = path.relative(parent, child);
  return rel === "" || rel === ".." || rel.startsWith(".." + path.sep) || path.isAbsolute(rel);
}

function isExecutableFile(target) {
  try {
    return fs.statSync(target).isFile();
  } catch (err) {
    return false;
  }
}

function firstMatchInDir(dir, names) {
  for (const name of names) {
    const candidate = path.join(dir, name);
    if (isExecutableFile(candidate)) return candidate;
  }
  return null;
}

function pathsEqual(left, right) {
  const a = process.platform === "win32" ? left.toLowerCase() : left;
  const b = process.platform === "win32" ? right.toLowerCase() : right;
  return a === b;
}

function realpathOrFail(target, label) {
  try {
    return fs.realpathSync(target);
  } catch (err) {
    fail(label + " cannot be canonicalised: " + target + " (" + describeError(err) + ")");
  }
}

// Scoping is enforced on canonical paths. requireDirectory and walkClassTree already
// refuse any symbolic link or reparse point via lstat; the checks below are the backstop
// that keeps every read inside the declared class root even if such an entry is missed.
function resolveScope(base, root) {
  const canonicalBase = realpathOrFail(base, "class root");
  const canonicalRoot = realpathOrFail(root, "package directory");
  if (isOutside(canonicalBase, canonicalRoot)) {
    fail("package directory canonical location escapes the class root: " + canonicalRoot
      + " (class root " + canonicalBase + ")");
  }
  return { canonicalBase, canonicalRoot };
}

function findJavapOnPath(names) {
  const raw = process.env.PATH;
  if (raw === undefined || raw === "") return null;
  for (const entry of raw.split(path.delimiter)) {
    const dir = entry.replace(/^"(.*)"$/, "$1").trim();
    if (dir === "") continue;
    const found = firstMatchInDir(dir, names);
    if (found !== null) return found;
  }
  return null;
}

function resolveJavap(explicit) {
  if (explicit !== null) {
    // parseArgs has already rejected blank and control-character values, so the value
    // here is a non-empty string path. It is used as a single execFileSync argument and
    // is never concatenated into a shell command line.
    const target = path.resolve(explicit);
    if (!fs.existsSync(target)) fail("--javap executable not found: " + target);
    if (!isExecutableFile(target)) fail("--javap path is not a readable file: " + target);
    return target;
  }
  const javaHome = process.env.JAVA_HOME;
  const checked = [];
  if (javaHome !== undefined && javaHome.trim() !== "") {
    const home = javaHome.trim().replace(/^"(.*)"$/, "$1");
    const found = firstMatchInDir(path.join(home, "bin"), JAVAP_NAMES);
    if (found !== null) return found;
    checked.push(path.join(home, "bin"));
  }
  const onPath = findJavapOnPath(JAVAP_NAMES);
  if (onPath !== null) return onPath;
  const detail = checked.length > 0 ? " (JAVA_HOME bin checked first: " + checked.join(", ") + ")" : "";
  fail("javap not found: pass --javap <file>, or set JAVA_HOME, or add javap to PATH" + detail);
}

function assertInScope(target, scope, label) {
  const resolved = realpathOrFail(target, label);
  if (!pathsEqual(resolved, scope.canonicalRoot) && isOutside(scope.canonicalRoot, resolved)) {
    fail(label + " canonical location is outside the scanned package: " + target + " -> " + resolved
      + " (package directory " + scope.canonicalRoot + ")");
  }
  return resolved;
}

function walkClassTree(dir, scope, visit) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  entries.sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0));
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    const stats = lstatOrFail(full, "input entry");
    if (stats.isSymbolicLink() || entry.isSymbolicLink()) {
      fail("input tree contains a symbolic link or reparse point, refusing to follow it: " + full);
    }
    assertInScope(full, scope, "input entry");
    if (stats.isDirectory()) walkClassTree(full, scope, visit);
    else if (stats.isFile() && entry.name.endsWith(".class")) visit(full, entry.name);
  }
}

function toRecordName(base, full) {
  const rel = path.relative(base, full);
  return rel.slice(0, -".class".length).split(path.sep).join("/");
}

function stderrSnippet(value) {
  if (value === undefined || value === null) return "";
  let text = Buffer.isBuffer(value) ? value.toString("utf8") : String(value);
  text = text
    .split(/\r?\n/)
    .filter((line) => line.trim() !== "")
    .join(" | ");
  if (text.length > STDERR_SNIPPET_LIMIT) text = text.slice(0, STDERR_SNIPPET_LIMIT) + "...";
  return oneLine(text);
}

function describeJavapFailure(err) {
  const code = err && err.code !== undefined ? String(err.code) : "-";
  const status = err && err.status !== undefined && err.status !== null ? String(err.status) : "-";
  const signal = err && err.signal ? String(err.signal) : "-";
  const message = err && err.message ? String(err.message) : String(err);
  const fields = ["code=" + code, "status=" + status, "signal=" + signal];
  if (code === "ETIMEDOUT" || (status === "-" && signal !== "-")) {
    fields.push("timeout=" + JAVAP_TIMEOUT_MS + "ms");
  }
  if (code.indexOf("MAXBUFFER") !== -1 || /maxbuffer/i.test(message)) {
    fields.push("maxBuffer=" + JAVAP_MAX_BUFFER);
  }
  const snippet = stderrSnippet(err && err.stderr);
  fields.push("detail=" + (snippet !== "" ? snippet : oneLine(message)));
  return fields.join(" ");
}

function runJavap(javap, classFile) {
  let out;
  try {
    out = execFileSync(javap, JAVAP_ARGS_PREFIX.concat([classFile]), {
      encoding: "utf8",
      timeout: JAVAP_TIMEOUT_MS,
      maxBuffer: JAVAP_MAX_BUFFER,
      killSignal: "SIGKILL",
      windowsHide: true,
    });
  } catch (err) {
    fail("javap failed for class " + classFile + ": " + describeJavapFailure(err));
  }
  if (typeof out !== "string" || out.trim() === "") {
    fail("javap produced empty output for class " + classFile);
  }
  return out;
}

function writeJsonAtomically(outPath, rows) {
  const outDir = path.dirname(outPath);
  const dirStats = lstatOrFail(outDir, "output directory");
  if (dirStats.isSymbolicLink()) {
    fail("output directory is a symbolic link or reparse point, refusing to write through it: " + outDir);
  }
  if (!dirStats.isDirectory()) fail("output directory is not a directory: " + outDir);
  if (fs.existsSync(outPath)) {
    const existing = lstatOrFail(outPath, "existing output");
    if (existing.isDirectory()) fail("output path is an existing directory: " + outPath);
    if (existing.isSymbolicLink()) {
      fail("output path is a symbolic link or reparse point, refusing to replace it: " + outPath);
    }
  }
  const tempPath = path.join(
    outDir,
    "." + path.basename(outPath) + "." + process.pid + "." + randomUUID() + ".tmp",
  );
  const payload = JSON.stringify(rows, null, 1);
  let fd = null;
  let created = false;
  try {
    fd = fs.openSync(tempPath, "wx", 0o666);
    created = true;
    fs.writeFileSync(fd, payload, { encoding: "utf8" });
    fs.fsyncSync(fd);
    fs.closeSync(fd);
    fd = null;
    fs.renameSync(tempPath, outPath);
    created = false;
  } catch (err) {
    if (fd !== null) {
      try {
        fs.closeSync(fd);
      } catch (closeErr) {
        // keep the original failure
      }
    }
    if (created) {
      try {
        fs.unlinkSync(tempPath);
      } catch (unlinkErr) {
        // only the temp file this process created is removed, never a directory tree
      }
    }
    fail("failed to write " + outPath + ": " + describeError(err));
  }
}

function summarise(rows, outPath, excludedInnerClasses) {
  const lines = [
    "scan-hooks: wrote " + outPath,
    "total " + rows.length + " (excluded inner classes containing $: " + excludedInnerClasses + ")",
  ];
  const byHook = new Map();
  for (const row of rows) {
    for (const hook of row.hooks) {
      const current = byHook.get(hook);
      if (current === undefined) byHook.set(hook, [row.name.split("/").pop()]);
      else current.push(row.name.split("/").pop());
    }
  }
  for (const hook of hooks) {
    const current = byHook.get(hook);
    lines.push("## " + hook + " " + (current === undefined ? 0 : current.length) + " : "
      + (current === undefined ? "" : current.join(",")));
  }
  process.stdout.write(lines.join("\n") + "\n");
}

function main() {
  const options = parseArgs(process.argv.slice(2));
  if (options.help) {
    process.stdout.write(USAGE);
    return;
  }
  const pkg = validatePackageName(options.pkg);
  const base = path.resolve(options.base === null ? DEFAULT_CLASS_ROOT : options.base);
  requireDirectory(base, "class root");
  const root = path.join(base, pkg);
  if (isOutside(base, root)) {
    fail("package directory escapes the class root: " + root);
  }
  requireDirectory(root, "package directory");
  const scope = resolveScope(base, root);

  const found = [];
  walkClassTree(root, scope, (full) => found.push(full));
  const usable = [];
  let excludedInnerClasses = 0;
  for (const full of found) {
    if (path.basename(full).includes("$")) {
      excludedInnerClasses += 1;
      continue;
    }
    usable.push({ full, name: toRecordName(base, full) });
  }
  usable.sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0));
  if (usable.length === 0) {
    fail("no scanable class file below " + root + " (found " + found.length
      + " class files, " + excludedInnerClasses + " excluded as inner classes)");
  }

  const javap = resolveJavap(options.javap);
  const rows = [];
  for (const item of usable) {
    const out = runJavap(javap, item.full);
    rows.push({ name: item.name, hooks: hooks.filter((h) => PATTERNS[h].test(out)) });
  }

  const outPath = path.resolve(
    options.output === null
      ? path.join(SCRIPT_DIR, ".tmp-javap", pkg + "-scan.json")
      : options.output,
  );
  writeJsonAtomically(outPath, rows);
  summarise(rows, outPath, excludedInnerClasses);
}

try {
  main();
} catch (err) {
  if (err instanceof ScanError) {
    process.stderr.write("scan-hooks: " + err.message + "\n");
    process.exitCode = err.exitCode;
  } else {
    process.stderr.write("scan-hooks: unexpected failure: " + describeError(err) + "\n");
    if (err && err.stack) process.stderr.write(String(err.stack) + "\n");
    process.exitCode = 1;
  }
}