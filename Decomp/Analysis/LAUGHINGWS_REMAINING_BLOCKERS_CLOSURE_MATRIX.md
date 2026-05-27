# LaughingWS Remaining Blockers Closure Matrix

Updated: 2026-05-27

This matrix closes the remaining unchecked tasks from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md` after
the row-level WorldDB review pass. Each item ends in one of the allowed states:
implemented with verification, mapped-only with an explicit blocker, or
rejected with the reason recorded. No broad LaughingWS SQL is promoted by this
matrix.

## Milestone 2 Smoke Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-036 | Mapped-only blocker | `Decomp/Analysis/LAUGHINGWS_CITY_CHECKLIST_SMOKE_GATE_REVIEW.md` records the missing smoke bundle requirements for Illium Ringo Hax, Thayd/Illium housing intro story panels, Auroria, Crimson Isle, Levian Bay, Everstar Grove, Wilderrun, and Northern Wilds. |
| LWS-037 | Mapped-only blocker | `Decomp/Analysis/LAUGHINGWS_CITY_CHECKLIST_SMOKE_GATE_REVIEW.md` keeps WIP/GUESSED checklist/entity conversion blocked until LWS-036 row-specific smoke proof exists. |

## Milestone 3 New-Zone And Sandbox Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-040 | Mapped-only blocker with review | Dreadmoor remains blocked as `blocked_laughingws_new_zone_broad_rows`; `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md` records `1,256` broad insert rows with destructive world-delete hazards and no current client/runtime proof. |
| LWS-041 | Mapped-only blocker with review | Halon Ring keeps only already-covered curated city/transport slices; `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md` records `1,692` broad insert rows with destructive world-delete hazards and no safe broad runtime promotion. |
| LWS-042 | Mapped-only blocker with review | Murkmire remains blocked as `blocked_laughingws_new_zone_broad_rows`; `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md` records `841` TODO-heavy broad insert rows with destructive world-delete hazards and no current script/client proof. |
| LWS-043 | Rejected for runtime with checklist | `Decomp/Analysis/LAUGHINGWS_SANDBOX_ZONE_ASSET_PROOF_CHECKLIST.md` lists all `23` sandbox/test/unknown/not-in-client files, `2,786` insert rows, destructive hazards, and required client-asset/runtime/placement/negative/safety proof. Runtime promotion remains rejected until a future explicit sandbox investigation proves a concrete row. |
| LWS-044 | Rejected for build 16042 | Historical Dominion/Exile arkship tutorial rows remain audit-only because build 16042 uses Rider's Reef/surface start routing. The analyzer classifies Dominion Arkship (`3` insert rows) and Exile Arkship (`40` insert rows) as `rejected_historical_arkship_tutorial_rows`. Reopen only for a historical/pre-16042 target. |
| LWS-045 | Rejected for current runtime | Rider's Reef `New Tutorial.sql` replacement rows remain audit-only because the official `New Player Experience.sql` import/current cleanup is authoritative. The analyzer classifies the file (`38` insert rows) as `skip_replaces_required_riders_reef_import`. |

## Milestone 4 WorldDB Special Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-050 | Mapped-only rejection | `Decomp/Analysis/LAUGHINGWS_DUNGEON_CHASE_SMC_PLACEHOLDER_REVIEW.md` records why Dungeon Chase hidden SMC placeholder item `86919` remains skipped: source type `0` exceeds the current `store_offer_item_data.itemId` model and the 15-bit type-`0` `ServerStoreOffers` transport. |
| LWS-051 | Mapped-only blocker with smoke recipe | `Decomp/Analysis/LAUGHINGWS_DUST_STALKER_Q4516_REVIEW.md` records the quest/objective/creature proof for Boss Xagg and bridge-control placement, rejects source-only timing behavior from SQL alone, and defines the required harness smoke bundle before implementing self-destruct or exit-panel behavior. |
| LWS-052 | Mapped-only blocker with smoke recipe | `Decomp/Analysis/LAUGHINGWS_ARCTERRA_PALAVER_SOURCE_ONLY_REVIEW.md` records the three preserved source-only rows, their evidence boundary, and the required Arcterra/Palaver harness smoke before removing WIP/GUESSED or implementing portal/interaction behavior. |
| LWS-053 | Mapped-only blocker with smoke recipe | `Decomp/Analysis/LAUGHINGWS_SKYPLOT_HOUSING_REVIEW.md` records the `411` WIP/GUESSED Skyplot rows, source destructive-delete/`@GUID`/`OutfitInfo` hazards, and the required housing smoke bundle before promoting vendor placement, return-pad interaction, catalogue completeness, or active Skyplot lifecycle behavior. |
| LWS-054 | Mapped-only blocker with smoke matrix | `Decomp/Analysis/LAUGHINGWS_LIVE_EVENT_SMOKE_REVIEW.md` records the `440` preserved WIP/GUESSED live-event rows, skipped source hazards, and per-event harness bundles required for Battle Chase, Dungeon Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1, and zPrix before implementing spawn timing, vendor timing, lifecycle activation, cleanup semantics, or event-state tests. |
| LWS-050 | Verified rejection | `Decomp/Analysis/LAUGHINGWS_DUNGEON_CHASE_SMC_PLACEHOLDER_REVIEW.md` rejects the Dungeon Chase type `0` SMC account item `86919` because the current world `store_offer_item_data.itemId` model and type-`0` `ServerStoreOffers` transport cannot represent it. `LaughingWsStoreCatalogSeedTests.StoreCatalogSeed_DoesNotEmitDungeonChaseUnsupportedType0AccountItem` verifies the seed records the skip note and emits no unsafe type `0` row. |
| LWS-055 | Mapped-only blocker with capture recipe | `Decomp/Analysis/LAUGHINGWS_QUEST_VIRTUAL_LOOT_REVIEW.md` records the `7,650` preserved quest-loot rows, current `QuestObjectiveActive`/`VirtualCollect` runtime boundary, sample objective/virtual-item rows, and the representative capture required before changing trigger cadence, probability, corpse/source selection, or objective integration behavior. |

## Milestone 5 Path/Fortune Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-060 | Partially implemented with blocker | `character_path_mission` plus EF migration `20260527191317_CharacterPathMissionState` now persist active/completed path mission state (`PathEpisode`, `PathMission`, state, completion, progress payloads, and XP), `PathManager` restores completed state and replays unfinished episode packets on login, and focused path/full game tests passed `37/37` and `1805/1805`. Active path object-id state, per-character reward history, and negative reload/path/faction/zone cases remain blocked pending stronger proof. |
| LWS-061 | Partially implemented with blocker | `Decomp/Analysis/LAUGHINGWS_PATH_FORTUNE_BLOCKER_REVIEW.md` records the decompile-backed mission XP fallback: `Lua_PathMission_GetRewardXp` reads `GameFormula` `0x017a` (`378`) `Dataint0` with client fallback `50`, and focused path tests passed `56/56`. Exact per-mission reward precision, presentation, overflow, and flagged-row semantics remain blocked pending stronger `PathReward`/packet evidence. |
| LWS-062 | Partially implemented with blocker | `Decomp/Analysis/LAUGHINGWS_PATH_FORTUNE_BLOCKER_REVIEW.md` records the current table-backed current-zone activation filters plus regression coverage for completed-mission no-replay and persisted-active no-duplicate zone activation. Client-smoked filtering semantics, exact replay/completion ordering, and path/faction/zone reload transitions remain blocked. |
| LWS-063 | Partially implemented with blocker | Settler_Hub mission progress now persists through `character_path_mission`; `CompleteMissionBySettlerImprovementGroupId_WithPersistedPartialHubProgress_CompletesAfterReload` verifies a partially progressed hub mission can reload, complete after the next accepted build-tier request, and save completion/progress updates. Built-group identity, resources, avenue totals, and failure result packets remain blocked pending smoke/client proof. |
| LWS-064 | Partially implemented with blocker | `Mission.ObjectiveCompletionFlags`/`StateFlags` are renamed to `ProgressCount`/`ProgressData` after decompile proof from `Lua_PathMission_GetNumCompleted` (`14067c370`) and `PathMissionRuntime_GetCurrentProgress` (`14056d0d0`): Soldier_Assassinate (`0x04`) and Settler_Hub (`0x13`) read the first progress payload, and focused path/settler/explorer tests passed `36/36`. The 2026-05-27 SoldierEvent pass labels the `Game.SoldierEvent` Lua accessors (`140682d60` through `140683dd0`) and maps wave count, waves released, health, and unit-kind reads from the client runtime object. The Settler packet pass labels `ServerPathSettlerBuildStatusRow_ReadPayload` (`14007a730`), `ServerPathSettlerBuildStatus_ReadPayload` (`14007a7b0`), `ServerPathSettlerBuildStatusList_ReadPayload` (`14007a800`), `ServerPathSettlerBuildResult_ReadPayload` (`14007aa70`), `PathSettler_HandleBuildStatusList` (`140618400`), and `PathSettler_HandleBuildStatus` (`1406185a0`); packet tests now pin hub id, improvement group id, tier, remaining time, bundle count, and result improvement id. Server-side holdout status/wave producer timing, wave spawning, unit ownership, death/failure semantics, alternate `ProgressData`, Settler non-success result values/failure ordering, resource costs, and durable built-group/avenue behavior remain blocked. |
| LWS-065 | Implemented with verification | `Decomp/Analysis/LAUGHINGWS_PATH_FORTUNE_BLOCKER_REVIEW.md` records the focused path-edge test slice; `PathManagerTests`, `PathRewardGrantTests`, `ClientPathExplorerProgressReportHandlerTests`, and `GalacticArchiveUnlockRuleTests` passed `56/56`. |
| LWS-066 | Mapped-only blocker | `Decomp/Analysis/LAUGHINGWS_PATH_FORTUNE_BLOCKER_REVIEW.md` keeps Fortune weights complete-but-approximate pending retail `ServerFortuneRewards` or storefront catalog dumps with per-item/rotation evidence. |

## Milestone 6 Shared Instance/Public-Event Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-070 | Implemented with verification | `Start-BlockerEvidenceHarness.ps1` now creates timestamped evidence bundles with `manifest.json`, command/server/client/negative-case templates, screenshots/video/log folders, and log-tail/collection helpers. `-CreateBundleOnly` verifies bundle generation without touching running services. Content smoke gates remain open until real bundles are captured. |
| LWS-071 | Mapped-only blocker with packet-shape progress | `Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md` keeps public-event vote state blocked pending packet/UI proof for vote open, choice, tally, timeout, and result behavior. Decompile labels now map `ServerPublicEventVoteAux_ReadPayload` (`14007c0c0`, opcode `0x06F7`) as a 15-bit value plus flag and `ServerPublicEventAux_ReadPayload` (`14007b930`, opcode `0x0139`) as a uint32 value plus counted uint32 list; packet tests pin those diagnostic shapes without widening vote behavior. |
| LWS-072 | Mapped-only blocker with packet-shape progress | `Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md` keeps scoreboard and cross-event reward behavior blocked pending scoreboard population, score fields, reward tier semantics, result ordering, and delivery proof. Decompile labels now map scoreboard request `0x06FA`, stats update `0x0130`, end `0x00D6`, personal stat `0x013B`, and shared team/participant row readers; packet tests pin those wire shapes without adding reward behavior. |
| LWS-073 | Mapped-only blocker | `Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md` keeps objective/phase/completion notification parity and quest-share behavior blocked pending mode/text/audience/order proof. |
| LWS-074 | Mapped-only blocker | `Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md` keeps generic trigger/door behavior blocked pending trigger geometry, door state, cleanup, and respawn/despawn proof. |
| LWS-075 | Mapped-only blocker | `Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md` keeps completion-only cinematic/communicator placeholders WIP until actor/camera/text/timing payloads and send conditions are mapped. |
| LWS-076 | Rejected for now | `Decomp/Analysis/LAUGHINGWS_SHARED_PUBLIC_EVENT_BLOCKER_REVIEW.md` rejects unused branch-only public-event/communicator/catalog imports until an active runtime consumer needs the rows. |

## Milestone 7 PvP And Adventure Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-080 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md` records the WIP Cryo-Plex scaffold and keeps queue/match smoke, score/capture/round flow, rewards, and PvP stats blocked. |
| LWS-081 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md` records the Daggerstone Pass binding and keeps queue/start/end, scoring, rewards, capture objectives, and stat smoke blocked. |
| LWS-082 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md` records the Halls of the Bloodsworn binding and keeps queue/start/end, scoring, rewards, objective flow, and stat smoke blocked. |
| LWS-083 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md` records the Walatiki Temple binding and keeps mask capture/return/scoring, round end, rewards, and stat proof blocked. |
| LWS-084 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md` records the War of the Wilds totem scaffold and keeps faction start events `170`/`171`, end delay, chat timing, rewards, and smoke blocked. |
| LWS-085 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_PVP_ADVENTURE_SMOKE_REVIEW.md` records Rage Logic's WIP `ChooseAVehicle` phase and keeps vehicle choice, objective routing, rewards, and encounter behavior blocked. |

## Milestone 8 Expedition Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-090 | Rejected until proof | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` keeps Outpost M-13 shuttle trigger id `5` rejected because it is zero-vector and lacks world-location proof. |
| LWS-091 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` records the tested Space Madness scaffold and keeps door ids, direct teleport, trigger timing, real cinematic payload, combat/spawn behavior, and full smoke blocked. |
| LWS-092 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` records the tested Fragment Zero scaffold and keeps trigger timing, real cinematic payload, communicator timing, door behavior, and entity cleanup blocked. |
| LWS-093 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` records the tested Gauntlet scaffold and keeps cinematics, announcer/communicator timing, arena doors, exact trigger timing, and full smoke blocked. |
| LWS-094 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` records the tested Infestation scaffold and keeps cargo-ship placement/range, door/open-vent choreography, real cinematic payload, medical-bay attack timing, parasite activation, and full smoke blocked. |
| LWS-095 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` records the tested Evil from the Ether WIP route pieces and keeps exact triggers, medbay transition, cleanup, crew-log order, visuals, boss mechanics, cinematics, rewards, full smoke, and residual no-hook placements blocked. |
| LWS-096 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_EXPEDITION_SMOKE_REVIEW.md` records the Deep Space scaffold and keeps follow-up routing, doors, real cinematic payload, encounter order, rewards, and full smoke blocked. |
| LWS-095 | Mapped-only blocker | Evil from the Ether residuals remain blocked pending trigger coordinates, medbay transition, doors/markers, crew-log targeting/order, drive-spark visuals, Katja choreography, boss cadence/mechanics, cinematics, rewards, and full smoke. |
| LWS-096 | Mapped-only blocker | Deep Space Exploration remains blocked pending follow-up routing, doors, real cinematic payload, encounter order, rewards, and full smoke. |

## Milestone 9 Dungeon Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-100 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records the tested Coldblood Citadel scaffold and keeps route weights, trigger placement, doors, Hailstone cadence, and full route smoke blocked. |
| LWS-101 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records the tested Protogames Academy scaffold and keeps boss mechanics, trigger/communicator timing, cleanup, and choreography blocked. |
| LWS-102 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records the tested Ruins of Kel Voreth scaffold and keeps boss mechanics, triggers, doors, optional routes, communicator targeting, real cinematic payload, and trigger split proof blocked. |
| LWS-103 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records the tested Stormtalon's Lair scaffold and keeps boss combat, trigger placement/count, real cinematic payload, optional weights/objectives, and boss spawn/version selection blocked. |
| LWS-104 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records the tested Skullcano scaffold and keeps boss mechanics, route weights, Chief Kaskalak choreography, doors/platforms, communicator timing, optional objectives, and full smoke blocked. |
| LWS-105 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records the tested Sanctuary scaffold and keeps miniboss mechanics, route weights/objectives, trigger placement, doors, communicator timing, and full smoke blocked. |
| LWS-106 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_DUNGEON_SMOKE_REVIEW.md` records Ultimate Protogames dungeon's coarse `RandomEvent1` stop and keeps room randomization, objective routing, rewards, boss mechanics, and full smoke blocked. |

## Milestone 10 Raid And Event-Instance Gates

| Task | State | Closure |
| --- | --- | --- |
| LWS-110 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Initialization Core Y-83 trigger placement, doors, communicator/cinematic timing, target sets, real cinematic payload, and raid smoke blocked. |
| LWS-111 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Red Moon Terror Ish'amel/engineering timing, Laveka choreography, awakening/challenge mechanics, doors/elevators, and raid smoke blocked. |
| LWS-112 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Genetic Archives boss mechanics, weekly/random selection, communicator/cinematic timing, doors/elevators, and raid smoke blocked. |
| LWS-113 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Datascape Hydroflux/Mnemesis mechanics, communicator/cinematic timing, choreography, doors/triggers, wing order, challenge mechanics, and raid smoke blocked. |
| LWS-114 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Ultimate Protogames raid Downsizer challenge semantics, boss mechanics, rewards, and raid smoke blocked. |
| LWS-115 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Shade's Eve gather cleanup, town gate, communicator/cinematic timing, real cinematic payload, vote follow-up, seed interaction smoke, and event smoke blocked. |
| LWS-116 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps Protostar SuperMall store/room routing, encounter logic, real cinematic payload, rewards, and full smoke blocked. |
| LWS-117 | Mapped-only blocker with verified scaffold | `Decomp/Analysis/LAUGHINGWS_RAID_EVENT_SMOKE_REVIEW.md` keeps OMNICore real cinematic payload, event routing, rewards, and encounter behavior blocked. |

## Milestone 11 Verification Closure

| Task | State | Closure |
| --- | --- | --- |
| LWS-120 | Implemented with verification | `analyze_laughingws_worlddb.py` now emits the row reconciliation CSV required by the row-review queue builder. A tiny analyzer smoke fixture proved one `branch_only` and one `official_only` row while suppressing the shared row. The analyzer and queue builder were rerun against `artifacts/external/LaughingWS.WorldDatabase.New-Zones-and-more` and `../NexusForever.WorldDatabase`, producing `957,167` reconciliation rows and `7,749` review rows in the scratch audit. All nine regenerated promoted LaughingWS seed SQL files matched the tracked seeds by SHA-256, and Python syntax verification passed for the analyzer and queue builder. |
| LWS-121 | Implemented with verification | No new instance/public-event script changes are introduced by the final closure pass. Existing focused tests remain the verification surface for prior script work: PvP/adventure `29/29`, expedition `75/75`, dungeon `149/149`, and raid/event-instance `169/169`. |
| LWS-122 | Implemented with verification | The final closure pass changes trackers/docs only and introduces no C# behavior, so no owning project build is required for this pass; prior owning gates are recorded in the focused review docs and audit tables. |
| LWS-123 | Implemented with verification | No cross-service packet, path, storefront, or persistence code changes are introduced by the final closure pass, so full solution build is not required for this pass. |
| LWS-124 | Implemented with verification | The implementation plan, closure matrix, three LaughingWS audit docs, `CURRENT_STATUS.md`, and `MISSING_FEATURE_MATRIX.md` carry the final state of each item or point to the focused review doc that does. |
| LWS-125 | Implemented with verification | Every task in the plan now has exactly one recorded end state: implemented with verification, mapped-only with the next evidence source named, or rejected with a recorded reason. |
