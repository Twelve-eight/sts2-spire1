# Session 22 - 2026-08-29 - Migration and language safety

## Purpose

This file is the handoff entry for the next conversation. Continue the local vanilla StS2 teardown and multiplayer knowledge base work until the user says stop.

## Current state

Main repo: `G:/omp works/sts2-spire1`
AFTP fork: `G:/omp works/aftp-ActsFromThePast`
Game: `G:/steam/steamapps/common/Slay the Spire 2`

Latest main repo commit before this整理 pass: `e0bb3fa`.
Latest AFTP commit: `9b4c4fb`.
Working tree changes from this pass must be committed after final checks.

## Deployed files and package

Local deployed hashes:

- `Spire1.dll`: `8d510cee7022b94a1abdb65138d9a061`
- `Spire1.pck`: `aae4930e99f24a2c983b4f323299507a`
- `ActsFromThePast.dll`: `317ad0345f64fccef14d727ddbc46563`
- `ActsFromThePast.pck`: `ba60133a597bf7b80bddcccdd4c493db`

Package: `dist/friends-pack.zip`

- Includes `mods/Spire1` and `mods/ActsFromThePast`.
- `mods/Spire1/character.txt` is `all`.
- All three StS1 characters are enabled by default.
- The complete AFTP PCK is required.
- The package zip is excluded from git because it exceeds the GitHub 100 MB file limit.
- Expand the zip and compare all four hashes before sending it.

## Multiplayer incident and fixes

Latest black-screen log: `C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/logs/godot.log`

The DARV event intentionally creates one event clone per player. Non-shared events may show different options to different players. The bug was not that the options differed. The bug was that AFTP changed the local option generation on only one peer by reading `DarvOnlyInLegacyActs` directly.

Evidence from the incident:

- Log lines 11636-11637: host and remote had different DARV option lists.
- Line 11668: remote selected `DUSTY_TOME`.
- Line 11677: `Unspecified Rarity: Ancient` warning.
- Lines 11699-11716: five reward messages for set 12 were buffered because the local set did not exist.
- Lines 11744-11748: map move started, event checksum started, and `WaitForSync` waited forever.
- Later lines only had network heartbeat records. The process was alive; the game logic was waiting.

Root cause:

- Host config had `DarvOnlyInLegacyActs=true`.
- Remote used the default false value.
- The same option index mapped to different options.
- The remote DustyTome reward path created a reward set that the host did not create.
- `RewardsSetSynchronizer` buffers messages without expiry.
- `CombatStateSynchronizer.WaitForSync` has no timeout or cancellation.

AFTP fixes:

- Commit `f166f11`: `DarvOnlyInLegacyActsEffective` and `LegacyEnemiesGiveClassicSlimedEffective`; multiplayer uses the vanilla branch.
- Commit `9b4c4fb`: Effective accessors for the two shared-event pool filters; multiplayer keeps the vanilla pool on both peers.
- The local fork DLL was built with zero errors and deployed as hash `317ad0345f64fccef14d727ddbc46563`.

## Vanilla multiplayer knowledge base

Volume 4: `research/sts1-kb/mechanics-v3/per-player-view-and-mp-divergence.md`

Volume 5: `research/sts1-kb/mechanics-v3/room-synchronizers.md`

Core rules:

- `EventModel` clones one mutable event per player.
- `IsShared=false` means players can select independently.
- Non-shared option selection sends an index, not a stable option identity.
- Event RNG includes the player slot for non-shared events.
- Reward set IDs are assigned locally; missing sets cause indefinite buffering.
- Run RNG and shared relic grab-bag state must stay synchronized.
- `GenerateRooms` runs symmetrically on both peers from the shared run seed.
- `WaitForSync` is an unbounded await; the only engine-native release is completion or peer disconnect.
- Checksum calls must occur the same number of times on every peer.
- Never compare localized display text as an identity key.

Mod review checklist:

- A local config must not change option count, order, or meaning in multiplayer.
- A reward generation path must exist on both peers before reward messages are sent.
- Room and map generation hooks must be symmetric.
- Per-player content may use player RNG; structure must use shared run RNG or host-broadcast data.
- Hook-based option lists must have identical length and index semantics on both peers.
- Watch for `Buffering ... hasn't been created yet` and `still N messages for other locations`.

## Defensive instrumentation

`CombatSyncStallWatchPatch` wraps the original `WaitForSync` task. It logs a warning after 60 seconds without changing behavior. Single-player smoke `STALLW1` completed with victory, zero Spire1 errors, and zero stall warnings.

This proves the single-player no-op path only. Real multiplayer verification is still required for all AFTP fixes.

## Completed research and audits

- `research/kb/inventory/inventory-research.md`: research tree inventory.
- `research/kb/inventory/inventory-mods.md`: local and workshop mod inventory.
- `research/kb/inventory/workspace-inventory.md`: inventory index.
- `research/audits/critic-20260828.md`: independent critic with 16 findings and disposition.
- `research/sts1-kb/mechanics-v3/per-player-view-and-mp-divergence.md`: legal per-player divergence and failure modes.
- `research/sts1-kb/mechanics-v3/room-synchronizers.md`: rest site, treasure, map, and room synchronization.

## Work still open

- Real two-player test with the current package: ClassicSlimed, DUPLICATOR, DARV plus DustyTome, and one complete act.
- Verify the handshake ref fix in a real run.
- Verify rest-site rescue in an AFTP act.
- Decide whether old save entries should be stripped or preserved through a legacy pool.
- Decide the Girya rest-site behavior and the Nloth implementation.
- Add repository and legacy inventory volumes.
- Add a separate divergence runbook.
- Teardown shop, encounter, map, and room transition synchronization from the original game files.
- Review raw local configuration reads in all local and workshop mod code.
- Fix or document the autoslay sapphire-key strategy.
- Investigate PCK flags 2 support in local tools.
- Add the Rewind Cecil repair procedure to version control.
- Review AcidSlime conditional behavior and the stale coverage scripts.
- Update old STATUS and workplan statements when they conflict with later evidence.

## Language safety

Shared policy: `G:/omp works/AGENTS.md` section 5.
Project rule: `.cursor/rules/model-text-language.mdc`.
Hook config: `.cursor/hooks.json`.
Hook wrapper: `.cursor/hooks/check-agent-text.mjs`.
CLI checker: `tools/check-agent-text.mjs`.

Policy:

- Model-bound prompts, task contexts, subagent messages, and generated reports may contain only Chinese, English, French, German, or Russian text plus ASCII punctuation and control characters.
- Reject Japanese kana, Korean Hangul, Arabic, Hebrew, Greek, emoji, and other scripts.
- Han code points are shared by Chinese and Japanese. Han-only text cannot be classified perfectly by code point.
- Keep unknown raw source text local and pass its path plus line range.
- Never silently delete or transliterate source text, code, paths, logs, hashes, or evidence.
- The checker fails closed in hook mode and reports the code point and JSON path.

Before any future subagent dispatch:

1. Keep raw logs out of the prompt.
2. Reference local paths and line ranges instead.
3. Run `node tools/check-agent-text.mjs --hook` on the final JSON request.
4. Dispatch only after the checker returns `permission=allow`.

## Next session start

1. Read this file and `DEVELOP.md`.
2. Check package hashes against local deployed files.
3. Read the newest `godot.log` only by local path and narrow line ranges.
4. Continue the original-file teardown with shop, encounter, map, and transition synchronization.
5. Use subagents only with approved-language prompts and pair each implementation worker with a reviewer.
6. Record every research result in `DEVLOG.md` and the appropriate knowledge volume.
7. Do not claim multiplayer success without the real two-player acceptance run.
