# FULL-DEVLOG - development session history (all omp sessions, archived)

Consolidated history of EVERY omp session in the workspace, with focus on Slay the
Spire 2 mod development. Core knowledge (service docs) is in CORE-DEVLOG.md. Raw
session transcripts are archived (see bottom). This session (2026-09-09) is included.

Format per session: date, title, size, what happened, where it lives now.

## 2026-08-09 Optimize project and consolidate dev skills (18.6MB)
- omp workspace bring-up; dev skill consolidation (SKILL.md authoring), relay/GLM
  pipeline understanding, project origins. Not StS-specific.
- Output: workspace conventions (AGENTS.md roots), skills in place.

## 2026-08-13 Fix unrecognized v2.zip data pack (2.7MB)
- Minecraft data-pack repair (not StS). Playderata workspace.

## 2026-08-19 Fix card-face disappearance and map background (29.9MB)
- Spire1-era porting start? Card art / map background fixes in the Spire1 modding
  pipeline (sts1 research roots: sts1-javap, sts1-kb created around this time).
- Output: sts1-kb structure, sts1 decompile dumps (now local-only).

## 2026-08-21 Make mc-savetool compatible with Geyser (5.2MB) / Use subagents and
end download task (6.5MB) / Clean C drive and install AMD driver (0.4MB)
- Playderata / system maintenance. Archived, no StS value.

## 2026-08-23 提交迁移材料并整理会话交接 (21MB) / 构建最新版 (6.5MB) / astrbot (2.8MB)
- Workspace migration (session handoff consolidation), omp builds, bilibili plugin
  search. Not StS.

## 2026-08-30 (0.016MB) / 2026-08-23 small sessions
- Empty/no-title stubs.

## 2026-09-01 Solve TUN proxy Minecraft server migration (1.3MB)
- MC networking; utility knowledge (proxy config) carried into STEAMCMD-ACCESS.

## 2026-09-05 提取近月少女的礼仪露娜线剧情 (1.9MB)
- Non-dev VN extraction; ASCII-readonly text pipeline practice.

## 2026-09-06 恢复状态并收尾提交 (5.3MB) - STSLINE START
- Session jsonl repair (unescaped quotes across 20 sessions), continuity recovery,
  final commits before audit.
- Output: session backup integrity, workspace snapshot.

## 2026-09-06 审计公开内容,清理目录,项目梳理 (15.1MB) - NOW LOCKED/OPEN (this thread's origin)
- Started public-content security audit, game-dir junk cleanup, project overview.
  Continued and completed on 2026-09-09 (below), record kept in this document.

## 2026-09-07 Deploy watcher setup and devlog updates (4.2MB)
- .tmp/deploy-watcher.mjs: watches mods/ outputs, auto-copies pck/dll to game mods dir.
- DEVLOG updates for AutoAnthonyRelics session 37-38.

## 2026-09-07 开发 autoanthony-relics 随机遗物 mod (0.5MB)
- AutoAnthony random-relic mod dev -> seeds the v0.3-v0.4 mechanics:
  pool replacement, entry tiers, live loc rewrite, icons (60), settings page.
- Output: AutoAnthonyRelics repo (workshop 3798163198).

## 2026-09-07 Steam 订阅自动本地注入方案 (0.6MB)
- Local workshop-injection scheme for testing (subscribe-to-local-mod flow),
  steamcmd publish automation design.

## 2026-09-08 验证 collab E2E 加密握手流程 (2.9MB)
- omp collab feature verification (encryption handshake). Not StS.

## 2026-09-09 THIS SESSION (locked, in progress when archived)
- Security audit of ALL public content (workshop items 3798163198/3798201953, git
  repos, docs):
  - Workshop surfaces: clean (no SteamID/account/paths/creds).
  - Git history: only env-var NAMES in pusher scripts, no values; no .env/cred files.
  - sts2-spire1: 3539 decompiled engine files were tracked -> SPLIT (user order):
    engine-dllsrc + sts1-javap + javap dumps + sigdump/typedump outputs now LOCAL-ONLY
    (untracked, gitignored, purged from history via git-filter-repo 2.47, force-pushed);
    generated research/engine-api-index.md (1.4MB, 22824 signature lines) as the
    tracked lookup layer; research/kb (35 docs) + docs (6) kept as mechanism layer.
- Game-dir cleanup: moved mods/mods.zip (61MB bundle), slaysp2manager batch silent
  skin, BaseLib-3.3.5-backup to G:/backups/sts2-game-junk-20260909/ (README inside);
  removed originals only after copy verified. mods/ now = 8 real mods.
- Perfect v0.2.0 published to workshop 3798201953 (guard code BFTWT used; page
  verified live; fileid written back to VDF).
- act4heart keys_enable false -> true for accounts 76561198801400830 /
  76561199033460852 / 76561199466878739 (three-key gate restored for act 4).
- steamcmd accessibility normalization: STEAMCMD-ACCESS.md + workshop-push.ps1
  (universal pusher: -GuardCode instant publish; fresh guard code per login).
- Workspace hygiene: scratch files removed; tools kept in .tmp/ (gen-icons,
  pck-extract, ocr, scrub-relay-words, label-sessions, workshop-push, etc).
- Session archive created (this task): G:/backups/omp-sts-sessions-20260909.zip
  (124 StS sessions, 53MB, excludes the locked live session).
- All 4 repos committed+pushed clean (see CORE-DEVLOG section 5).

## Archive detail

- Zip: G:/backups/omp-sts-sessions-20260909.zip (124 files)
- Index: G:/backups/omp-sts-sessions-20260909.index.json (444 sessions: main +
  subagents, size/date/path; label-sessions.cjs regenerates)
- README: G:/backups/omp-sts-sessions-20260909.README.md
- Source root: C:/Users/o_Obl/.omp/agent/sessions/--G--omp works--/
- Excluded: the locked live session (2026-09-06T15-02-03..09a.jsonl) - will be
  archived at session end on the next round.
- Subagent sessions excluded as non-StS: MC/Geyser, omp engine (flash/outbox/security/
  collab), system (block-props, smoke server, kline).

## How to extend

Repos keep their own DEVLOG.md (per-project detail). This FULL-DEVLOG consolidates
the session-level timeline; add a dated section at the end of this file whenever a
session closes, then archive the transcript into the zip pattern above.