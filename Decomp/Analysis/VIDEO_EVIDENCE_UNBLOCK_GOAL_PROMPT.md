# Goal Prompt For Video Evidence Unblocking

Created: 2026-06-19

This file contains a paste-ready Codex Goal prompt for investigating local and
online WildStar videos, mapping their content to current NexusForever blockers,
and using parallel Codex workers/subagents where available to unblock
implementation or record precise remaining client-smoke gaps.

## Recommended Goal Prompt

Paste this into Codex from the NexusForever repository root:

```text
/goal Investigate and use video evidence to unblock the current NexusForever content-retail and feature blockers. Work from I:\GIT\NexusForever. Start by reading AGENTS.md, README.md, CURRENT_STATUS.md, Decomp/Analysis/MISSING_FEATURE_MATRIX.md, Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md, Decomp/Analysis/README.md, Decomp/Analysis/CONTINUATION_GUIDE.md, and Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md. Treat generated CSVs as the authority for queue order, blocker counts, row state, and retail_claim_allowed, especially Decomp/Analysis/coverage/content_retail_completeness_next_slice_queue.csv and Decomp/Analysis/coverage/content_retail_completeness_next_slice_blockers.csv. Use available parallelism aggressively: discover and use Codex multi-agent/subagent/subthread/worktree tools when available, and spawn workers for disjoint evidence discovery, video indexing, online search, CSV grouping, and independent implementation slices. The main goal runner remains the coordinator/integrator and owns final tracker updates, generator runs, verification, and completion claims.

Objective: build a reusable video-evidence map from local videos under I:\Videos and online videos, then use it row by row to move blockers into one of these states: implemented and verified, mapped/correlated by external video but still pending local emulator/client smoke, rejected as not relevant, or blocked with the exact missing video/capture/artifact named. Search I:\Videos recursively for gameplay videos and inspect filenames, durations, nearby metadata, frame samples, and any subtitles/descriptions available. Search online for WildStar videos when local coverage is missing, using targeted queries for the current queue slice, instance, raid, dungeon, adventure, public event, mechanic, or system. Prefer full UI-visible gameplay over trailers, retrospectives, boss-only clips, or edited highlight videos.

Parallel execution policy: after the initial doc/CSV baseline, fan out read-only workers immediately. Suggested worker lanes are: local I:\Videos inventory and timestamp extraction; online video search for missing instance/raid/adventure/public-event footage; queue/blocker CSV aggregation by blocker family and actionability; quest/zone video mapping; dungeon/adventure/expedition video mapping; raid/public-event video mapping; and system-mechanic videos for challenges, paths, contracts, matching, transport, loot, achievements, crafting/economy/social. Workers must return compact evidence tables with paths/URLs, timestamps, visible mechanics, blocker rows, confidence, and missing proof. Do not delegate reading AGENTS.md or skill instructions to workers as a substitute for the main runner reading them. Use multiple worker threads/sub-Codex threads for read-only searches whenever the tools support it.

For each queue iteration, re-read the generated next-slice queue and blocker-detail CSVs before choosing work. Process one narrow video-evidence slice at a time: one quest chain, one instance phase/objective, one public-event objective family, one challenge/path/contract mechanic, one raid boss, one dungeon/adventure room, or one feature-system flow. Do not widen split-required instances into full-instance implementation unless the queue emits a validation-ready sub-slice or the video evidence proves a bounded objective/phase that can be implemented and tested safely.

Implementation parallelism is allowed only for independent slices. Prefer sibling git worktrees or isolated sub-Codex workspaces for disjoint implementation lanes such as one quest chain, one instance objective family, one public-event objective family, or one system mechanic. Keep the main checkout as the integration tree. Cap active implementation workers to a small number, avoid overlapping files, and require each worker to report exact touched files, evidence used, tests run, and remaining blockers. Merge/integrate one worker result at a time, re-run the narrow tests and generated validators after integration, then re-read the queue before launching more implementation work. If workers conflict on generator code, shared runtime managers, tracker files, or generated CSVs, stop parallel writes and serialize the integration.

For each selected slice, make an evidence note that records: local video path or web URL, title, duration if known, timestamps, what is visible in the UI, exact content ids/names/objectives if visible or table-correlated, confidence level, whether the footage is retail/reference/local-emulator, and which blocker rows it addresses. For quests, look for accept/prerequisite UI, map/minimap/path-arrow routing, objective progress, interact/CSI/kill/collect timing, object or creature placement, despawn/respawn, turn-in, rewards, achievements, follow-up quests, and negative/repeat cases. For instances, raids, dungeons, adventures, expeditions, and public events, look for entry/portal/queue flow, start buttons, phase activation, objective tracker changes, doors/consoles/cages/buttons/teleporters, creature/object spawns, boss/add mechanics, timers, failure states, optional objectives, medal/scoreboard behavior, wipe/reset/replay behavior, final rewards, loot, achievements, lockouts, and claim UI. For systems, look for challenge timers/rewards, path mission activation/progress/rewards, contract board/rotation/claim UI, matching queue/role/teleport/reward flow, transport/rapid travel/taxi behavior, loot rolls/chests/personal loot, achievement checklist/toast behavior, crafting/economy/social UI, and any packet/loggable state transition.

Use video evidence conservatively. External videos may corroborate retail mechanics and justify tracker wording such as external_video_observed or mapped/correlated behavior, but they do not prove local emulator behavior. A blocker can move to implemented only when video evidence is corroborated by source/table/DataMapping/decompile/log evidence, implementation changes are made in the owning subsystem, focused tests pass, and generated outputs validate the state. If local client proof is required, use I:\WildStar build 16042 and Decomp/Analysis/Start-BlockerEvidenceHarness.ps1 to create an evidence bundle under artifacts/blocker_evidence, capturing manifest, notes, screenshots/video references, server logs, client observations, and negative cases. Use -CreateBundleOnly when services are already running or when only a worksheet is needed.

When implementation is safe, implement only evidence-backed behavior in the owning NexusForever subsystem, add or update focused tests, and update durable tracker/generator text so the exact video source and timestamp can be rediscovered. Do not query wildstar_client, jabbithole, nexus_forever_mapping, or nf_map_* from runtime code; use those only as authoring evidence, mapper output, reviewed promotion input, tests, or typed fixtures. Do not copy decompiled client source. Do not edit generated/local artifacts except through documented generator workflows. Do not revert unrelated dirty worktree changes.

After each slice, run the narrowest useful verification. For C# behavior, prefer the owning project build and focused xUnit filter, then broader tests if the change crosses shared contracts. For content-retail tracker changes, regenerate with python Tools\WikiArchiveAudit\content_retail_completeness_audit.py, then run python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py. Preserve not_retail_complete and retail_claim_allowed=false unless the generated outputs and manual/client evidence gates genuinely allow promotion.

Keep looping until every current queue slice and high-volume blocker family has a video-evidence disposition: used to implement and verify, correlated but still pending named local emulator/client smoke, rejected as irrelevant/insufficient, or blocked with the exact missing footage/capture/dependency and next command. Prioritize high-leverage blocker families first: instance_dependency, public_event_evidence, script_objective_producer, instance_entity, script_runtime_action, instance_reward, then the validation-ready quest/video-smoke rows. Finish with a concise report listing changed files, videos used with timestamps, blockers advanced, blockers still needing local capture, verification commands/results, and the next highest-value video search or capture.
```

## First Continuation Instruction

If Codex asks where to begin after activating the Goal, use:

```text
Start by rebuilding the current baseline from Decomp/Analysis/coverage/content_retail_completeness_next_slice_queue.csv and content_retail_completeness_next_slice_blockers.csv, then immediately fan out parallel read-only workers: one for I:\Videos inventory, one for online video search by queued content, one for blocker-family aggregation, and separate workers for quest, instance, raid/public-event, and system-mechanic evidence. Recombine their findings into one video-evidence map, then pick the first high-leverage blocker family where video can answer a concrete blocker question. Record timestamps and stop before implementation unless source/table/test evidence also supports the behavior.
```

## Completion Audit

Before marking the Goal complete, Codex must verify:

1. Every current next-slice queue row has either linked video evidence, an
   explicit no-useful-video result, or a named local capture still required.
2. High-volume instance/public-event/script blocker families have a reusable
   video-evidence map, not just one-off chat notes.
3. Parallel worker outputs were reconciled by the main runner, conflicts or stale
   findings were resolved against current generated CSVs, and integration did
   not leave divergent tracker claims.
4. Any implementation is backed by video plus source/table/log/test evidence.
5. Any tracker/generator wording that cites video evidence includes enough path,
   URL, title, and timestamp detail to rediscover the evidence.
6. External video corroboration is not treated as local emulator proof.
7. `content_retail_completeness_audit.py` and
   `validate_content_retail_completeness_outputs.py` were run after tracker or
   generator changes.
8. Remaining blockers name the exact missing video, capture, runtime service,
   local client step, Ghidra/decompile proof, packet capture, or reference data
   that would unlock the next pass.
