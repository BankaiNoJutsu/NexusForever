# Goal Prompt For Northern Wilds Remaining Gaps

Created: 2026-06-24

This file contains a paste-ready Codex Goal prompt for closing the current
Northern Wilds implementation, evidence, and validation gaps identified from
the generated content-retail tracker outputs.

It intentionally covers all current Northern Wilds buckets from the latest scan:
public event `154`, path episode rewards, challenges `103`/`105`/`107`,
non-curated WorldZone `35` quests, Northern Wilds transport/portal rows, and the
already queue-ready curated quests that should be treated as validation/client
smoke work unless a fresh scan proves a real implementation gap.

## Current Baseline

Authoritative generated outputs:

- `Decomp\Analysis\coverage\content_retail_completeness_quests.csv`
- `Decomp\Analysis\coverage\content_retail_completeness_next_slice_queue.csv`
- `Decomp\Analysis\coverage\content_retail_completeness_next_slice_blockers.csv`
- `Decomp\Analysis\coverage\content_retail_completeness_public_event_evidence.csv`
- `Decomp\Analysis\coverage\content_retail_completeness_challenge_evidence.csv`
- `Decomp\Analysis\coverage\content_retail_completeness_path_mission_evidence.csv`
- `Decomp\Analysis\coverage\content_retail_completeness_instance_portal_evidence.csv`

Current scan summary:

- WorldZone `35` has `30` quest rows: `10` `curated_full`, `3`
  `generic_with_script`, and `17` `no_objectives`.
- Northern Wilds queue-ready validation rows are `Q3479`, `Q3480`, `Q3486`,
  `Q3668`, `Q3673`, `Q3886`, `Q3963`, and `Q3797`.
- The strongest current implementation/evidence target is public event `154`
  `Dominion Ultrabot`.
- Northern Wilds path mission rows are broadly implemented, but PathEpisode
  reward type-`2` rows for episodes `8`, `9`, `28`, and `82` remain blocked on
  reward presentation/timing evidence.
- Northern Wilds challenge rows for `103`, `105`, and `107` still have creature
  review/spawn, ambiguous creature, direct reward-item bridge, and UI smoke
  blockers.
- Northern Wilds transport/portal rows are lower priority, but still need
  creature bridge, placement, entry/exit, prerequisite, and client smoke proof.

## Recommended Goal Prompt

Paste this into Codex from the NexusForever repository root:

```text
/goal Close all current Northern Wilds remaining implementation, evidence, and validation gaps in I:\GIT\NexusForever. Start by reading AGENTS.md, README.md, CURRENT_STATUS.md, Decomp/Analysis/MISSING_FEATURE_MATRIX.md, Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md, Decomp/Analysis/README.md, and Decomp/Analysis/CONTINUATION_GUIDE.md. Treat generated content-retail CSVs as authoritative for row state, blocker text, queue order, actionability, and unsafe retail-complete gates; refresh from Decomp/Analysis/coverage/content_retail_completeness_quests.csv, content_retail_completeness_next_slice_queue.csv, content_retail_completeness_next_slice_blockers.csv, content_retail_completeness_public_event_evidence.csv, content_retail_completeness_challenge_evidence.csv, content_retail_completeness_path_mission_evidence.csv, and content_retail_completeness_instance_portal_evidence.csv before choosing work.

Scope: close the current Northern Wilds buckets as a full-area goal, not a one-row task. Include public event 154 Dominion Ultrabot; PathEpisode reward type-2 rows for episodes 8 Securing the Crash Site, 9 Mapping Northern Wilds, 28 Cold Science, and 82 Emergency Aid; challenge rows for 103 Skeech Slayer, 105 Rootbrute Slayer, and 107 Xenobite Egg Smasher; WorldZone 35 non-curated quest rows Q3670, Q3671, Q3689, Q3712, Q3741, Q3781, Q3783, Q4526, Q8944, Q8960, Q8961, Q8962, Q9112, Q10065, Q10066, Q10067, Q10068, Q10100, Q10101, and Q10138; Northern Wilds transport/portal rows including portal ids 113, 114, 167, 168, 221, 222, 234, and 256 when current CSVs still list them; and the queue-ready curated Northern Wilds validation rows Q3479, Q3480, Q3486, Q3668, Q3673, Q3886, Q3963, and Q3797.

Prioritize in this order unless current CSVs prove a better blocker multiplier: 1) PE154 Dominion Ultrabot creature bridge/zone visibility/event spawn and client smoke, using Source/NexusForever.Script.Main/Quests/NorthernWilds/DominionUltrabotPublicEventScript.cs, NorthernWildsMapScript.cs, and focused public-event tests as the current runtime surface; 2) PathEpisode reward type-2 evidence and implementation boundary for episodes 8/9/28/82, keeping PathManager and typed path tables as the owner; 3) challenge 103/105/107 creature-review/spawn rows, ambiguous Xenobite Egg rows, direct reward-item bridge rows, reward UI selection, and challenge client smoke; 4) the three generic-with-script quests Q3741, Q3781, and Q4526, separating already-tested server availability from remaining client/dialog/reward/achievement smoke; 5) no-objective and path-choice/tutorial quest rows Q3670/Q3671/Q3689/Q3712/Q3783/Q8944/Q8960/Q8961/Q8962/Q9112/Q10065-Q10068/Q10100/Q10101/Q10138, proving row-specific giver/receiver, prerequisite/exclusion, reward, achievement, and client dialog behavior before promotion; 6) Northern Wilds transport/portal rows, resolving Creature2 bridge, placement, entry/exit, prerequisite, and client smoke or rejecting stale definitions with evidence; 7) queue-ready curated quests only as validation/client-smoke work unless re-reading the blocker details reveals a true implementation gap.

Work one narrow slice at a time. For each slice, extract the exact CSV rows, source files, tests, DataMapping/review rows, current blocker_summary, evidence_sources, manual_validation, and completion_status. Classify the slice as Mapped, Verified, Implemented, Blocked, or Rejected using Decomp/Analysis/CONTINUATION_GUIDE.md. Gather evidence before changing behavior. Prefer current source, focused tests, generated CSV rows, reviewed DataMapping outputs, safe runtime seed/promoted rows, live WorldServer logs, local build-16042 client smoke, and decompile/client-table evidence where needed. Use external video only as corroboration, never as local emulator proof.

Implement conservatively in the owning runtime subsystem. Runtime code must not query wildstar_client, jabbithole, nexus_forever_mapping, nf_map_*, or other authoring/reference tables. Promote reviewed data into runtime-owned world/auth tables, scripts, or code-owned assets first. Do not copy decompiled client source. Do not broaden packet, spell, anti-tamper, token, movement, path, challenge, public-event, quest, or reward behavior beyond mapped and verified evidence. Do not revert unrelated dirty worktree changes.

For C# behavior changes, add or update focused tests in Source/NexusForever.Game.Tests and run the narrowest useful filter first, using an isolated OutDir under artifacts/test-bin if live servers lock normal Debug outputs. Useful starting filters include FullyQualifiedName~DominionUltrabot, FullyQualifiedName~NorthernWilds, FullyQualifiedName~PathManager, FullyQualifiedName~Challenge, FullyQualifiedName~Q3741, FullyQualifiedName~Q3781, FullyQualifiedName~Q4526, FullyQualifiedName~Quest, and FullyQualifiedName~PublicEvent as appropriate to the touched surface. Build the owning project when shared runtime contracts change.

For DataMapping or safe-import changes, follow Tools/DataMapping/README.md and Tools/DataMapping/sql/README.md. If reviewed placements or bridge rows are promoted, regenerate the affected mapping/audit outputs through the documented scripts and verify safe world imports with Tools/DataMapping/sql/verify_safe_world_imports.sql against a clean runtime deployment when applied. Keep generated/local artifacts untouched unless the documented generator workflow requires regeneration.

For tracker changes, regenerate with python Tools\WikiArchiveAudit\content_retail_completeness_audit.py and validate with python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py. Preserve not_retail_complete and retail_claim_allowed=false unless generated outputs plus focused tests plus required client/manual smoke genuinely prove retail completion. If the current environment cannot provide a needed dependency, such as I:\WildStar build 16042 client access, live server stack, MySQL/RabbitMQ, reference authoring databases, Ghidra/MCP, packet capture, or video/source evidence, stop that slice as Blocked with exact attempted commands, files inspected, evidence gathered, missing input, and next command or artifact that would unlock it.

Completion means every Northern Wilds row in the scoped buckets is one of: implemented and verified with source/tests/tracker/client evidence where required; rejected with recorded evidence; or blocked with a named unavailable dependency and next evidence command. Do not mark the goal complete while any scoped row is merely uninspected, vaguely mapped, indirectly proven, or silently omitted.
```

## First Continuation Instruction

If Codex asks where to begin after activating the Goal, use:

```text
Start by rebuilding the Northern Wilds baseline from the generated CSVs. Print the current WorldZone 35 quest bucket counts, the PE154 public-event blocker statuses, the challenge 103/105/107 blocker statuses, the PathEpisode 8/9/28/82 reward rows, and the Northern Wilds portal rows. Then pick PE154 Dominion Ultrabot as the first implementation/evidence slice unless the fresh scan shows that row has already moved. Do not edit code until the exact PE154 CSV rows, source owner, focused tests, DataMapping bridge status, and client-smoke blocker are identified.
```

## Completion Audit

Before marking the Goal complete, Codex must verify:

1. The generated CSVs were re-read after any tracker regeneration or source/data
   change.
2. Public event `154` rows are implemented, rejected, or blocked with exact
   creature bridge, zone visibility, spawn, reward/medal, and client-smoke
   evidence named.
3. PathEpisode reward rows for episodes `8`, `9`, `28`, and `82` are implemented
   with reward presentation/timing evidence, or remain explicitly blocked with
   the missing capture named.
4. Challenge rows for `103`, `105`, and `107` have dispositions for creature
   review/spawn, ambiguous creature rows, direct reward-item bridge rows, reward
   UI selection, and local client smoke.
5. `Q3741`, `Q3781`, and `Q4526` separate already-tested server-side behavior
   from remaining client/dialog/reward/achievement smoke, and no row is promoted
   on focused unit tests alone.
6. The no-objective/path-choice/tutorial quest rows have row-specific
   giver/receiver, prerequisite/exclusion, reward, achievement, and dialog
   dispositions or named blockers.
7. Northern Wilds transport/portal rows have current Creature2 bridge,
   placement, entry/exit, prerequisite, and client-smoke dispositions, or stale
   definitions are rejected with evidence.
8. Queue-ready curated quests `Q3479`, `Q3480`, `Q3486`, `Q3668`, `Q3673`,
   `Q3886`, `Q3963`, and `Q3797` were handled as validation/client-smoke rows
   unless current blocker details proved an implementation gap.
9. `content_retail_completeness_audit.py` and
   `validate_content_retail_completeness_outputs.py` pass after any generator,
   tracker, script, DataMapping, or evidence-status change.
10. No runtime code depends on authoring-only reference databases, generated
    local artifacts, or staging tables.
