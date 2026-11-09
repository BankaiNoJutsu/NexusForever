# Instance encounter goal prompt: dungeons, expeditions, raids and event instances, built with video (2026-11-09)

Purpose: take the 27 instance rows in [content_retail_completeness_instances.csv](coverage/content_retail_completeness_instances.csv) (all `mapped_wip_scaffold` or `client_recorded_review_required`: kill credit and objective routing exist, boss mechanics do not) from scaffold to the most retail-like encounters the available evidence supports, using retail video as one input among several.
Written after the Stormtalon's Lair pilot, [DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md](DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md), which established what video can and cannot give. It complements, and does not replace, the [unified goal](UNIFIED_ASSESSMENT_RETAIL_RESTORATION_GOAL_PROMPT.md), the [video witness goal](VIDEO_WITNESS_GOAL_PROMPT.md) and the [restoration gaps goal](RESTORATION_GAPS_GOAL_PROMPT.md). Writing this file starts nothing.

```text
Continue NexusForever retail restoration for WildStar build 16042 by implementing instance encounters (dungeons, expeditions, raids, event instances and World Story instances) as far as the
evidence allows, following Decomp/Analysis/INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md. Work in the worktree I:\GIT\NexusForever-unified-20261106 (branch codex/unified-restoration-20261106);
the push target is origin/legacy-di-storefront-hardening in BankaiNoJutsu/NexusForever (normal pushes only, fetch first; never force, reset, merge to main or open upstream PRs).
Read AGENTS.md, ASSESSMENT_REMAINING_WORK.md (dated notes at the top), the Stormtalon survey and the prompt's section for the instance you are on before touching code.

Rules (carried over, non-negotiable)
- Evidence ladder Observed -> Correlated -> Mapped -> Verified. Video is Observed at most. Never claim retail parity; Source/SQLite tests are not retail parity; never add test totals across incompatible scopes.
- Video must be retail only: upload date before 2018-11-28 (an empty upload_date is not evidence) or official Carbine/WildStar material; emulator footage, _Excluded_NexusForever and post-shutdown uploads are never evidence.
  Prefer clips from the 2014-06-02 launch onward; beta clips (before launch) may differ from build 16042 and count only as Correlated unless a post-launch clip agrees.
- Resolve a video by URL or YouTube id, never by corpus id (ids were renumbered on 2026-11-09). Single-commentator guides are single-source: a number or threshold needs two independent clips to be called agreed.
- Failing or focused test first, fix in the owning project (NexusForever.Script.Instance for encounter scripts, Game only for a missing engine facility), then run only the affected
  `dotnet test NexusForever.Game.Tests --filter ...` (no full-solution builds). If a test was not run red, say so in the record. Update (never weaken) tests that encode superseded behaviour.
- Never encode an invented value. An unobserved cast order, interval, damage, range or spawn position is left as a named, documented placeholder that is disabled by default, not guessed.
- Native reads: Ghidra 12.1.4 headless only, one process at a time; an agent's native or video read is "agent-written, not re-derived/not re-viewed" until the lead re-checks it.
- Add a dated note at the top of ASSESSMENT_REMAINING_WORK.md for every slice, linking its record with the exact evidence tier. Commit explicit paths with the Co-Authored-By line from the session
  attribution rule; do not commit .claude/ or .mcp.json. Native client launch and Computer Use remain stopped.
- Test command (PowerShell, from ...\Source): set TEMP, TMP = I:\tools\claude-tmp and DOTNET_CLI_HOME = I:\tools\claude-tmp\dotnet, then
  dotnet test NexusForever.Game.Tests -m:1 -nodeReuse:false -p:NuGetAudit=false --filter "..." and read only the `error CS|Passed!|Failed!|[FAIL]` lines.

Phases, in order
A. Engine facilities every encounter needs (build and test once, reuse everywhere).
B. Per-instance evidence packets (survey, then bind rows, then measure), in the priority order below.
C. Per-instance implementation slices from the packets, one boss at a time.
D. Smoke and reward closure per instance.
Stop condition: for every instance row, each encounter is either implemented to the evidence tier recorded for it, or has an exact statement of the missing input and why nothing available supplies it.
Completion is not the stop condition and no parity claim is made.
```

## What the pilot established (inputs to every phase)
* **Video gives** phase order and health thresholds, ability names and rough order within a phase, add and wave counts, door, platform and cinematic order, optional-objective routes, interrupt counts, boss yell lines (chat log and banner). Corpus: about 140 dated clips for Stormtalon's Lair, between 15 and 150 for most others (counts in the survey record, corrected for undated uploads).
* **Video does not give** cast names (the library is 720p, cast bars are not legible), cooldowns (guides say "periodically"), damage, telegraph sizes, spawn positions, add stats, trigger placement or drop tables.
* **Tables give** Spell4 names and raw columns (cooldown, effects), telegraph shapes and sizes, text ids, `Creature2` rows and `PublicEvent*` rows. There is no creature-to-spell table (`Creature2Action` is talk/emote data), so which spell a boss casts must be bound from other evidence. Spell4 ids for one dungeon sit in a loose block of consecutive ids among unrelated spells; internal names such as `[STL]` mark a few rows. A `LocalizedText` id referenced from a spell row (for example 534487 on Spell4 63114 "Tempest") is a real binding anchor.
* **Boss yells** are fired by server script (the yell texts are not referenced by any table), so their order and triggers come from clips only.
* **Server today:** each dungeon script is kill credit and objective routing (`PublicEventObjectiveCreditEntityScript`), with trigger scripts and placeholders. No boss ability, phase or add logic exists. `NexusForever.Script.Template` has an `EntityCastEvent`.

## Phase A: engine facilities (do first, bounded)
Check what exists before building; each item gets a test and a record.
1. **Health-threshold phases:** a script-side facility to fire a handler once as a creature's health crosses a percentage (boss and add), robust to heal-through and leaving combat/reset.
2. **Scheduled and sequenced casts:** cast a Spell4 id on a timer or after another cast, with target selection (tank, random player, all players), interruptible casts, and cast-time/telegraph publication through the existing spell pipeline. Prefer the existing `ISpellParameters` and entity cast paths.
3. **Interrupt armor and stuns:** boss interrupt armor counts (the guides state them: Blade-Wind 2, Aethros infinite with four interrupts needed for the white mechanic, Static Wave 4) and the effect of an interrupt on a scheduled cast.
4. **Add waves and invulnerability:** spawn groups, invulnerable-until-condition creatures, shield removal, despawn on wipe/reset.
5. **Encounter reset on wipe and re-entry:** state reset, spawn restore, door/platform restore, so an encounter can be retried.
6. **Yell and cinematic hooks:** emit boss yell text ids by trigger (`LocalizedText` ids) and cinematic cues; keep the cinematic payload placeholders already in the repo.
Acceptance: each facility is covered by focused tests that do not depend on any specific boss.

## Phase B: per-instance evidence packet (template)
For each instance and boss, produce one record under `Decomp/Analysis/` named `INSTANCE_<name>_PACKET_<date>.md` with:
1. **Footage list:** post-launch dated clips with subtitles first, URL, upload date, length, resolution, role (guide, full run, boss only), and which tier (normal/veteran, level 20/50).
2. **Phase and ability skeleton:** from at least two independent clips per claim; mark single-source items.
3. **Row binding:** candidate Spell4 ids (names, id block, `[STL]`-style internal names, text-id anchors, effect types), `Creature2` ids per version (normal, veteran, level 50), `PublicEventObjective` ids, and the target groups already mapped. Tier each binding: text-id anchor or internal name = Correlated at best, name-and-block only = Correlated (weak).
4. **Timing measurements:** from clips, telegraph or health-bar events at 0.5 s resolution over at least two clips; record the spread, never a single number as fact.
5. **Open inputs:** exactly what is missing (cast list, intervals, positions) and whether a native read, a packet capture or another clip source could supply it.
Do not skip ahead to code for a boss whose packet has no ability skeleton agreed by two clips.

## Phase C: implementation slice rules
* One boss per slice. Slice contents: a test fixing the phase thresholds and ability order the packet agrees on; the script in `NexusForever.Script.Instance/<kind>/<instance>/`; placeholders (disabled) for anything unobserved; a record update.
* Cast rows: use only rows bound at Correlated or better (text anchor, internal name, or two-clip agreement on name and telegraph). Disabled-by-default placeholder for the rest.
* Keep the existing kill credit, objective routing and reward scaffolds intact; extend rather than rewrite, and update superseded tests explicitly.
* Optional objectives and challenges (for example "Stormchaser", "finish phase 2 in 10 seconds", "no tornado hits") are implemented only after the boss they belong to and only from agreed observations.

## Phase D: smoke and reward closure
Per instance: the existing blocker harness (`Start-BlockerEvidenceHarness.ps1`, `-DungeonSmoke`) is the proof-bundle format; native client launch stays stopped, so record that live-client smoke remains pending. Rewards, loot and medal tiers come from tables and the existing reward scaffolds, never from video.

## Priority order (by footage, scaffold maturity and dependency)
Counts are dated, subtitled-and-unsubtitled retail clips matched by title/description (survey record has the method). A short footage count is a blocker, not a reason to guess.
1. **Stormtalon's Lair** (world 382): about 149 clips. Pilot done: skeleton for all three bosses, one table anchor (Tempest 63114), candidate row block 63110 to 63267, no cast list. Start with Blade-Wind the Invoker (fewest hidden inputs), then Aethros (Tempest has a text anchor), then Stormtalon.
2. **Ruins of Kel Voreth** (1336): about 136. Bosses: Forgemaster Trogun, Slavemaster Drokk, Grond the Corpsemaker; 2015 Boss Guides exist (`_K6WIpU09G4`, `w3kbhbj9CcA`, `AMIEBH8VoUc`).
3. **Genetic Archives** (1462) and **Datascape** (1333): about 122 and 116 clips; raids add 20-man scale; do after the dungeon template is proven. Experiment X-89 raid guide `BgZeBYlVGgE` and System Daemons `V5PPaN7cIK8` exist.
4. **Skullcano** (1263, about 69) and **Sanctuary of the Swordmaiden** (1271, about 52).
5. **Protogames Academy** (3173), **Ultimate Protogames** (2980, 3041), **Coldblood Citadel** (3522, one clip): only after a footage check; Coldblood Citadel likely stops at "missing input".
6. **Expeditions:** Infestation (1232), Outpost M-13 (1319), Rage Logic (1627), Space Madness (2149), Gauntlet (2183, about 21 clips), Deep Space Exploration (2188), Fragment Zero (3180), Evil from the Ether (3404). Footage counts unmeasured; run the packet survey first.
7. **Raids and large instances:** Redmoon Terror (3032, 3102, 3524; about 15 clips), Augmentor's Raid (3040), BAD (3042, client-recorded review required), Ohmna and Avatus/Kuralak/Maelstrom clips in the library map to Genetic Archives, Datascape and open-world bosses.
8. **World Story and event instances:** Vault of the Archon (3009), Journey into OMNICore-1 (3045), Shade's Eve (3044), Protostar Winterfest (3094).
Each instance's packet records its footage count and may end at "missing input"; that is a finished outcome, not a failure.

## Known open decisions the lead owns
* Cast order and intervals can be measured from video only as Observed ranges; decide per boss whether a measured range is sufficient to enable a cast or it stays a disabled placeholder.
* Normal versus veteran versus level-50 row binding: needs clips of each version; do not copy one version's skeleton to another without a second source.
* Whether to commission a native read of the creature spell list (no table link exists); needs an owner decision on effort, since the earlier Ghidra reads cost several passes each.
* Normal-mode and level-20 differences: guides cover mostly veteran.
