#!/usr/bin/env node
// Delegate hook validation to the single project checker.
// The hook contract does not guarantee a project-root cwd, so resolve the
// checker path from this wrapper's own location instead of process.cwd().
// Layout: <projectRoot>/.cursor/hooks/check-agent-text.mjs
//         <projectRoot>/tools/check-agent-text.mjs

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const wrapperDir = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(wrapperDir, "..", "..");
const checker = path.join(projectRoot, "tools", "check-agent-text.mjs");

let stdin = "";
try {
  stdin = fs.readFileSync(0, "utf8");
} catch (error) {
  // No stdin or stdin unreadable: deny (fail closed) via empty input path below.
  stdin = "";
}

const result = spawnSync(
  process.execPath,
  [checker, "--hook"],
  {
    cwd: projectRoot,
    input: stdin,
    encoding: "utf8"
  }
);

if (result.stdout) process.stdout.write(result.stdout);
if (result.stderr) process.stderr.write(result.stderr);
process.exitCode = result.status ?? 2;
