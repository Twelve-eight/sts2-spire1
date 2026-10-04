You are the implementation worker in the existing Codex native session. The user-specified model remains global:deepseek-v4.1-flash, reasoning max, requested route gateway/wb2api. Do not change model or harness, launch other agent runtimes, or delegate. The same-batch supervisor is Tesla, agent 01a1071d-685c-7d21-b91a-cbdf62606483. Work only on this bounded release-preparation slice.

## Exact writable product files

G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1
G:\omp works\.tooling\refresh-workshop-payloads.ps1
G:\omp works\Sts\sts2-perfect\mod\Perfect.csproj
G:\omp works\Sts\sts2-mpconfigsync\mod\MpConfigSync.csproj
G:\omp works\Sts\sts2-heartshake\mod\HeartShake.csproj
G:\omp works\Sts\AutoAnthonyRelics\mod\QuriousCraftingRelics.csproj

Edit these files directly in your workspace. Do not edit gameplay code, descriptions, manifests, any other docs, git index or git history. Do not build, lint, test, pack, copy compiled artifacts, deploy, run games, contact Steam, or edit shared configuration. No writes on C:.

## Unique report path

G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004\release-pipeline-worker-20261005.md

On the first usable conclusion immediately persist it to this report before more work. Persist each completed file or invariant. Use exactly these sections: ## 已确认 / ## 进行中 / ## 未知. Report prose must be Chinese with ASCII punctuation. At completion record CODE_COMPLETE, all changed absolute paths, implementation details and unverified boundaries. Do not accumulate the whole report until the end.

## Specification

Read the coordinator contract G:\omp works\Sts\sts2-spire1\docs\WORKSHOP-PREPARATION-CONTRACT-20261005.md and your previous scout report. Implement minimal changes, preserving encoding and line endings.

1. Build-Spire1Release.ps1: Make explicit -Promote perform canonical synchronization only after PCK structure verification and all DLL gates pass, and before writing any Workshop payload. Reject -Promote with PlanOnly or Configuration != Release before any publication write. Do not synchronize on ordinary non-promote runs. Use the exact canonical mod\.godot\mono\temp\bin\Release\Spire1.dll, reject a fallback candidate for promotion, and prove canonical DLL equals the already-staged payload DLL before updating build PCK. Check every ancestor and file target for reparse, keep resolved targets inside the repo canonical build tree, and reject Steam/shared config/C: paths. Generate clean PCK from the allowlisted stage as today. Copy it to a uniquely named temporary file in the canonical directory, verify length/hash and actual mtime >= DLL (never fake timestamps), create a matching temporary 64hex ASCII digest, read it back, install PCK followed by digest, then read both back again. Abort before Workshop promote on any failure. Record the non-transactional two-file update honestly: not a pair-atomic transaction, and existing provenance must reject any incomplete pair. Remove only your own temporary files with LiteralPath and known resolved paths. Add fail-closed post-promote readback of all three final files and exact three-file allowlist. Existing source/stage safety checks must not be weakened.

2. refresh-workshop-payloads.ps1: Add an explicit row policy PublishPdb=false only to Spire1, default optional PDB behavior unchanged for other rows. Get-RowAllowlist must OMIT Spire1.pdb entirely for this row, so neither copy direction can reintroduce it and an existing staged Spire1.pdb is a hard allowlist violation. Do not silently delete stale files. Preserve all SHA, digest-required, mtime, freshness, path, held-back, ITEM inventory and -Only behavior. Fix the existing same-hash/different-mtime PCK refresh bug: when a staged PCK hash already equals the build hash but PCK mtimes differ, regenerate must actually copy the build PCK, not merely ALREADY_CURRENT; VerifyOnly remains strictly read-only and still rejects the mismatch; WhatIf remains read-only. Do not rewrite times to hide a producer issue. Keep the script ASCII and PowerShell 5.1 compatible.

3. The four csproj files: Add a minimal fail-closed quick-PackPck digest producer independent of CopyToModsFolderOnBuild so no-deploy builds produce their own digest. Prefer a narrow copy of the proven ReadPckMetadata inline task from Spire1 rather than new packages or external scripts. Capture pack start in a BeforeTargets=PackPck target; invalidate old digest before an enabled real pack; after success (_PckPackerExitCode=0 and PckPackerSkipped!=true), assert the canonical PCK exists, length > 0, mtime >= captured pack start and >= TargetPath DLL mtime, hash exact bytes, write adjacent 64hex ASCII .sha256 and read it back. Match PckPackerOutputPath and canonical build directory; refuse skipped/unsupported output instead of stamping a stale PCK. Do not change manifest, deployment targets or package versions. No producer activity when PckPackerEnabled=false or inner export; PackPck failure must remain failure.

Workers only write code and their incremental report; CENTRAL builds/tests are the coordinator's job. If a detail cannot be implemented safely, flag it with path/line rather than stub it. Finish this narrow patch; avoid further research or work outside the exact files.