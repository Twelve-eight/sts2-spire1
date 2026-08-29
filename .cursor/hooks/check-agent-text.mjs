#!/usr/bin/env node
// Delegate hook validation to the single project checker.
// The hook runs from the project root according to the project hook contract.

import { spawnSync } from "node:child_process";
import fs from "node:fs";

const result = spawnSync(
  process.execPath,
  ["tools/check-agent-text.mjs", "--hook"],
  {
    cwd: process.cwd(),
    input: fs.readFileSync(0, "utf8"),
    encoding: "utf8"
  }
);

if (result.stdout) process.stdout.write(result.stdout);
if (result.stderr) process.stderr.write(result.stderr);
process.exitCode = result.status ?? 2;
