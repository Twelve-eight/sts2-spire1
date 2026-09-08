# CORE-DEVLOG - Slay the Spire 2 modding development core (2026-09-09)

Purpose: the knowledge layer that SERVES development going forward. Full history lives in
FULL-DEVLOG.md. This document is the distilled contract: what exists, how it works, and
where the authoritative details live. Includes this session's findings.

## 1. Project map

| Repo (G:/omp works/) | Purpose | Workshop | Status |
|---|---|---|---|
| sts2-spire1 | central research + KB + Spire1 compat layer; the only knowledge repo | - | active |
| AutoAnthonyRelics | 60 seed chaos relics mod (replaces all non-ancient relic sources) | 3798163198 online v0.4.2 | active |
| sts2-perfect | "Perfect" card mod (unlimited upgrades, pre-enchanted) | 3798201953 online v0.2.0 | active |
| chaosbridge | per-character deterministic pools + transform no-replacement dedup | - | active (engine-level) |

## 2. Repository layout (sts2-spire1)

- research/kb/*.md - de-originalized mechanics knowledge (35 docs): pool architecture,
  combat semantics, hooks matrix, AutoAnthony contracts, porting cookbook, pitfalls.
  Authoritative for engine facts.
- research/engine-api-index.md - 1.4MB signature-only index of the decompiled engine
  (classes/methods, no bodies). Generated from research/engine-dllsrc which is LOCAL-ONLY
  (gitignored + purged from history 2026-09-09). Regenerate: scan .cs signature lines.
- docs/ - API references (BaseLib/JmcModLib/RitsuLib), design, audits.
- mod/ - Spire1 mod source (KB AR3 contract era).
- research/ (local-only, NOT tracked): engine-dllsrc (3539 files decompiled StS2),
  sts1-javap, sts1-kb/.tmp-javap, sigdump/typedump bin/obj.

## 3. Engine knowledge essentials (what we learned, repeated)

- Reward pools: real engine pool via `[Pool(typeof(...SharedRelicPool))]` attribute;
  `IsShared` only feeds the compendium, NOT rewards. Ancient/starter/event rarities
  never enter the grab bag -> replace-population patch strips non-chaos models from
  deques + _originalRelics; RefreshRarity must not re-add vanilla.
- Pool replacement (AutoAnthony v0.4): Populate postfix strips every non-chaos model
  from grab-bag deques and _originalRelics; RefreshRarity cannot re-add vanilla.
- One class = one [HarmonyPatch] target; two targets in one class silently patch only
  the last (offline repro verified) - split patch classes per target.
- LocString/LocTable._translations reflection rewrites live descriptions per seed
  (BaseLib bakes placeholder once at startup; we rewrite on seed capture).
- Transform batches sample WITHOUT replacement (PandorasBox double card bug) - pushed
  via CardCmd.Transform prefix/finalizer, thread-static batch exclusion.
- Perfect gating: ColorlessCardPool.GetUnlockedCards postfix removes card from
  rewards/shops/transforms; OnPlay/cost hooks gate effects; MaxUpgradeLevel override
  is required (base default 1 = cannot upgrade twice).
- Settings pages: BaseLib SimpleModConfig + registration; mod_configs/*.cfg; Dolso
  framework uses mod_configs/dolso.<mod>.config with FileSystemWatcher (live reload).
- Localization: mods ship zhs+eng settings_ui.json + loc; scrubbed ASCII-safe.
- pck auto-deploy: csproj post-build copies to mods/ dir; watch out for pck never
  deploying (missing export step in csproj) - fixed session 37-38.

## 4. Toolchain / environment (repeatable procedures)

- Builds: dotnet (JDK not needed); NUGET_PACKAGES + DOTNET_CLI_HOME on G: (never C:).
- Deploy: csproj auto-deploy to G:/steam/steamapps/common/Slay the Spire 2/mods/<Mod>/
- Workshop publish: G:/omp works/.tmp/workshop-push.ps1 + per-repo
  workshop/workshop_upload.vdf (publishedfileid fixed). steamcmd needs a NEW Steam
  Guard code EVERY login (sentry not persisted). Full procedure: .tmp/STEAMCMD-ACCESS.md
- 302 accelerator (steamcommunity_302.cli) hosts block can break steamcmd login;
  retry + guard code path is the norm. Secret scan note: env vars STEAM_ACCOUNT /
  STEAM_PASSWORD are user-level, NEVER commit values.
- Session archive: G:/backups/omp-sts-sessions-20260909.zip (124 StS sessions, excludes
  the live one) + index json beside it. label-sessions.cjs regenerates the index.
- Language hygiene (HARD): only zh/en/fr/de/ru + ASCII punctuation allowed in prompts
  and files; CJK only via local file paths; no emoji, no U+2014/2013/2190/2192/2026.
- GLM relay sensitive-phrase filtering: trigger patterns (no further text / arp-player
  / Time Warp joins / triple-dot amplifiers) cause 4xx-500 responses; the scrub hook
  (G:/omp works/.omp/hooks/pre/strip-illegal.ts) neutralizes via String.fromCharCode.
- act4heart: mod_configs/dolso.act4_heart.config keys_enable=true (3 keys required);
  live reload via FileSystemWatcher. Set for ALL local accounts (~/AppData/Roaming/
  SlayTheSpire2/steam/<acc>/mod_configs/).

## 5. Version / release state (2026-09-09)

- AutoAnthonyRelics v0.4.2: 1/3/5 entry counts, pool replacement, live descriptions,
  60 icons, settings page with toggle + multiplier, workshop 3798163198.
- sts2-perfect v0.2.0: MaxUpgradeLevel=int.MaxValue fix, EnablePerfect settings toggle
  + pool gate + effect gating, enchant icon = card art, workshop 3798201953.
- chaosbridge: TransformBatchDedup, per-character pools.
- All four repos clean, pushed. Steam client updated; mods deployed to mods/ dir.

## 6. Governance reminders

- Never write to C:. All caches/builds on G: (verify free space: 23GB free 09-09).
- Git identity: Twelve-eight <Twelve-eight@users.noreply.github.com>; proxy env for
  GitHub: -c http.proxy= direct works when 302 down? (use global gitconfig proxy).
- Decompiled engine stays LOCAL forever (copyright); only signature index is public.
- Every code change: build + deploy + smoke (run game or log-verify); devlog entry.
- Backup discipline: git push + robocopy for non-code; do NOT delete originals after
  move without user confirmation (game junk moved to G:/backups/sts2-game-junk-20260909).

## 7. Session knowledge index (what was decided where)

- Session 2026-08-09..09-01: omp engine/setup work (not StS) - archived, low value for
  StS now (see FULL-DEVLOG).
- 2026-09-06 (recovery + audit): jsonl repair (unescaped quotes), session continuity,
  public-content audit began.
- 2026-09-07 (AutoAnthony dev): random relic mod development -> v0.4 mechanics; deploy
  watcher for pck/dll auto-deploy; workshop injection approach.
- 2026-09-07 (steam subscription injection): local mod injection scheme + steamcmd push.
- 2026-09-08 (collab E2E): omp collab encryption verification (not StS).
- 2026-09-09 (THIS session): security audit (all public surfaces clean; sts2-spire1
  decompile split to local-only + API index), game-dir junk backup (mods.zip, silent
  skin batch, BaseLib 3.3.5 backup), Perfect workshop publish (3798201953), act4heart
  keys_enable=true, steamcmd accessibility doc. See FULL-DEVLOG for the session log.

## 8. Open items / risk

- sts2-spire1 history was rewritten (decompile purge) - remote force-pushed; any old
  clone must re-clone or force-pull (old history contains decompiled source).
- Workshop BBCode descriptions: paste the file (DESCRIPTION-BBCODE.txt) via web editor
  for full formatting; VDF description is the fallback short version.
- 4th steam account (76561199478895791) may also need act4heart keys_enable if used.
- Steam Guard flow: user must supply the code per publish (no sentry persistence).