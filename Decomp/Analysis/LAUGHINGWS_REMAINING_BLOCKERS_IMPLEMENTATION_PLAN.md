# LaughingWS Remaining Blockers Implementation Plan

Updated: 2026-05-27

Scope:
- `Decomp/Analysis/LAUGHINGWS_WORLDDB_AND_MORE_AUDIT.md`
- `Decomp/Analysis/LAUGHINGWS_QUESTING_AND_MORE_AUDIT.md`
- `Decomp/Analysis/LAUGHINGWS_INSTANCES_AND_MORE_AUDIT.md`

Goal: close every remaining LaughingWS-derived blocker by turning each item into
one of the allowed continuation states: implemented with verification, mapped
only with an explicit blocker, or rejected with the reason recorded.

Closure note: row-level tasks through LWS-035 are closed inline below; the remaining smoke/proof gates stay open and are tracked task-by-task in `Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_CLOSURE_MATRIX.md` as blocked or rejected until stronger evidence exists.

Rules:
- Do not import the LaughingWS world database checkout directly.
- Do not promote broad replacement SQL until row-level proof exists.
- Keep WIP/GUESSED rows and scripts labeled until client/runtime smoke proves
  the behavior.
- Runtime code must not query `wildstar_client`, `jabbithole`, or `nf_map_*`
  reference/staging tables.
- For decompile-backed behavior, follow `CONTINUATION_GUIDE.md` and record
  addresses, labels, source paths, and remaining unknowns.
- For each code/data pass, update the owning audit, `CURRENT_STATUS.md`, and
  `MISSING_FEATURE_MATRIX.md` before closing the task.

## Milestone 0 - Rebaseline The Audit Inputs

- [x] LWS-000 Re-run the WorldDB analyzer:
  `python Tools\DataMapping\analyze_laughingws_worlddb.py`.
  Use `artifacts\laughingws_worlddb_audit_latest\laughingws_worlddb_file_inventory.csv`
  and `laughingws_worlddb_row_reconciliation.csv` as the source of truth.
- [x] LWS-001 Reconcile the count drift in the WorldDB audit: the summary says
  `7` `extract_additive_world_overlay` files while the "Still Blocked" text says
  `8`. Update the audit text after the rerun.
- [x] LWS-002 Snapshot the open review queues into the plan/tracker:
  `7` additive overlay files, `11` blocked residual/new-zone files, `23`
  sandbox/test-zone files, the two all-in-one missing Datascape mechanics script
  names, Dungeon Chase hidden SMC placeholder, and all WIP smoke-only slices.
- [x] LWS-003 Run a docs-only sanity check after tracker edits:
  `rg -n "extract_additive_world_overlay|blocked_laughingws|Still Blocked" Decomp\Analysis`.

## Milestone 1 - Close Broad WorldDB Row Reconciliation

- [x] LWS-010 Build a reusable row-review worksheet from
  `laughingws_worlddb_row_reconciliation.csv` that groups each changed row by
  file, table, row status, creature id, world id, coordinates, checklist index,
  vendor ids, script names, and whether the row already exists in the official
  runtime seed. Implemented by
  `Tools\DataMapping\build_laughingws_row_review_queue.py`, which writes
  `artifacts\laughingws_worlddb_audit_latest\laughingws_worlddb_row_review_queue.csv`.
- [x] LWS-011 Review `Map\Open World\Alizar\Algoroc.sql`
  (`122` row delta). Classify each branch-only or official-only row as
  promotable, official-retained, duplicate, rejected placeholder, or blocked.
  Closed in `Decomp/Analysis/LAUGHINGWS_ALGOROC_ROW_REVIEW.md`: `0`
  promotable rows, `5` official-retained rows, and `64` branch-only queue rows
  blocked as `blocked_laughingws_algoroc_residual_rows`.
- [x] LWS-012 Review `Map\Open World\Alizar\Celestion.sql`
  (`72` row delta) with the same classification. Closed in
  `Decomp/Analysis/LAUGHINGWS_CELESTION_ROW_REVIEW.md`: `0` promotable rows,
  `19` official-retained queue rows, `2` rejected zero-coordinate placeholder
  rows, and `50` branch-only queue rows blocked as
  `blocked_laughingws_celestion_residual_rows`.
- [x] LWS-013 Review `Map\Open World\Alizar\Galeras.sql`
  (`90` row delta) with the same classification. Closed in
  `Decomp/Analysis/LAUGHINGWS_GALERAS_ROW_REVIEW.md`: `0` promotable rows and
  `41` branch-only queue rows blocked as
  `blocked_laughingws_galeras_residual_rows`.
- [x] LWS-014 Review `Map\Open World\Alizar\Thayd.sql`
  (`-2883` row delta). Preserve already-covered housing/city rows and isolate
  only narrow residual rows with current quest, vendor, script, or smoke value.
  Closed in `Decomp/Analysis/LAUGHINGWS_THAYD_ROW_REVIEW.md`: the `3`
  branch-only housing intro entity rows remain covered by the city-content seed,
  `1,646` official-only queue rows are retained, and `25` branch-only vendor
  queue rows are blocked as `blocked_laughingws_thayd_residual_rows`.
- [x] LWS-015 Review `Map\Open World\Alizar\Whitevale.sql`
  (`127` row delta) with the same classification. Closed in
  `Decomp/Analysis/LAUGHINGWS_WHITEVALE_ROW_REVIEW.md`: `0` promotable rows,
  `15` official-retained queue rows, and `105` branch-only queue rows blocked
  as `blocked_laughingws_whitevale_residual_rows`.
- [x] LWS-016 Review `Map\Open World\Olyssia\Deradune.sql`
  (`-618` row delta), prioritizing the `40` branch `questChecklistIdx` deltas
  called out by the audit. Closed in
  `Decomp/Analysis/LAUGHINGWS_DERADUNE_ROW_REVIEW.md`: `47` branch-only
  checklist queue rows and `61` other branch-only queue rows are blocked as
  `blocked_laughingws_deradune_residual_rows`, while `505` official-only queue
  rows are retained.
- [x] LWS-017 Review `Map\Open World\Olyssia\Ellevar.sql`
  (`101` row delta), prioritizing the `43` branch `questChecklistIdx` deltas
  called out by the audit. Closed in
  `Decomp/Analysis/LAUGHINGWS_ELLEVAR_ROW_REVIEW.md`: `65` branch-only
  checklist queue rows and `64` other branch-only queue rows are blocked as
  `blocked_laughingws_ellevar_residual_rows`, while `51` official-only queue
  rows are retained.
- [x] LWS-018 For any promotable checklist-only rows from LWS-011 through
  LWS-017, add an extractor that emits coordinate-keyed updates plus guarded
  fallback inserts only when the current local import lacks the official rows.
  No extractor was emitted because the reviewed Algoroc, Celestion, Galeras,
  Thayd, Whitevale, Deradune, and Ellevar queues found `0` promotable
  checklist-only rows; Deradune and Ellevar checklist rows remain mapped-only
  as explicit blocked residual buckets.
- [x] LWS-019 For any promotable entity/stat/vendor/spline rows from LWS-011
  through LWS-017, emit a deterministic-id runtime-owned seed. Strip deletes,
  DDL, `REPLACE`, `@GUID`, and foreign-key toggles. No seed was emitted because
  the reviewed queues found `0` promotable entity/stat/vendor/spline rows; all
  non-covered branch-only rows are now blocked or rejected by named
  recommendations.
- [x] LWS-020 Update `Tools\Setup\Initialize-NexusForever.ps1` to import any new
  reviewed seed after the primary world seed and to clear only the seed-owned
  deterministic high-id range when the seed hash changes. No setup script
  change was required because LWS-018 and LWS-019 produced no new reviewed seed.
- [x] LWS-021 Extend `Tools\DataMapping\sql\verify_safe_world_imports.sql` with
  expected-count and drift checks for every new reviewed slice. No verifier
  change was required because Milestone 1 produced no new promoted slice.
- [x] LWS-022 Verify:
  `python -m py_compile Tools\DataMapping\analyze_laughingws_worlddb.py Tools\DataMapping\extract_laughingws_*.py`,
  run the updated extractor, run the analyzer, then run
  `verify_safe_world_imports.sql` against `nexus_forever_world`. Verified the
  analyzer and row-review queue rebuild; `verify_safe_world_imports.sql` was
  not rerun for this milestone because no new seed/import path was generated.

## Milestone 2 - Close Already-Partially-Covered World Residuals

- [x] LWS-030 Levian Bay residual rows: review branch-only/source-only rows after
  the covered `14` Signal Flare and Drop Pod Landing Beacon checklist updates.
  Promote only rows with Quest2/DataMapping/current script proof; otherwise
  reject or keep mapped-only. Closed in
  `Decomp/Analysis/LAUGHINGWS_LEVIAN_BAY_ROW_REVIEW.md`: the `14`
  checklist rows remain covered by the small-world seed, `20` official-only
  entity rows are retained, and `115` branch-only residual queue rows stay
  blocked as `blocked_laughingws_levian_bay_residual_rows`.
- [x] LWS-031 Everstar Grove residual rows: review branch-only vendor, spline,
  and source-only rows after the covered Exo-Lab 71 teleporter updates. Closed
  in `Decomp/Analysis/LAUGHINGWS_EVERSTAR_GROVE_ROW_REVIEW.md`: the `2`
  Exo-Lab 71 checklist rows remain covered by the small-world seed, `3`
  official-only entity rows are retained, and `59` branch-only residual queue
  rows stay blocked as `blocked_laughingws_everstar_grove_residual_rows`.
- [x] LWS-032 Auroria residual rows: review branch-only rows after the covered
  `67` checklist updates. Closed in
  `Decomp/Analysis/LAUGHINGWS_AURORIA_ROW_REVIEW.md`: the `67` checklist rows
  remain covered by the small-world seed, `74` official-only entity rows are
  retained, and `56` branch-only residual queue rows stay blocked as
  `blocked_laughingws_auroria_residual_rows`.
- [x] LWS-033 Crimson Isle residual rows: review Mondo Zax, Kezrek Warbringer,
  Supply Officer Mazzu, and fake-activeProp Eldan Teleporter Surface rows.
  Closed in `Decomp/Analysis/LAUGHINGWS_CRIMSON_ISLE_ROW_REVIEW.md`: the `24`
  checklist rows remain covered by the small-world seed, `24` official-only
  entity rows are retained, and `74` branch-only residual queue rows stay
  blocked as `blocked_laughingws_crimson_isle_residual_rows`.
- [x] LWS-034 Northern Wilds residual rows: prove or reject branch spline-mode
  changes for Dominion Swordsman rows; keep duplicate Supply Officer Windward
  vendor rows rejected unless new vendor evidence appears. Closed in
  `Decomp/Analysis/LAUGHINGWS_NORTHERN_WILDS_ROW_REVIEW.md`: `7` useful
  entity rows and `16` stats remain covered by the small-world seed, `9`
  official-only rows are retained, `30` branch duplicate entity/vendor rows are
  rejected, and `8` branch spline-mode rows stay blocked as
  `blocked_laughingws_northern_wilds_residual_rows`.
- [x] LWS-035 Wilderrun Dorian rows: resolve the identity mismatch for source
  creature ids `27455`, `27843`, and `58746` against current DataMapping,
  `wildstar_client`, and `jabbithole`; promote only if the Dorian identity and
  placement match. Closed in
  `Decomp/Analysis/LAUGHINGWS_WILDERRUN_ROW_REVIEW.md`: the `33` useful
  checklist rows remain covered by the small-world seed, `33` official-only
  rows are retained, and `5` branch-only Dorian entity/stat queue rows stay
  blocked as `blocked_laughingws_wilderrun_dorian_rows` because current
  DataMapping Dorian identities are `25779`/`51292`, not `27455`/`27843`/`58746`.
- [ ] LWS-036 Run targeted quest/client smoke for Illium Ringo Hax,
  Thayd/Illium housing intro story panels, Auroria, Crimson Isle, Levian Bay,
  Everstar Grove, Wilderrun, and Northern Wilds objective rows. Record exact
  character, world, coordinates, quest id, objective id, logs, and screenshots.
- [ ] LWS-037 Convert smoke-proven WIP/GUESSED checklist/entity rows to
  implemented status in audit docs. Leave unproven rows explicitly WIP/GUESSED
  or rejected.

## Milestone 3 - New-Zone And Sandbox Content Gates

- [ ] LWS-040 Dreadmoor: keep the current broad rows blocked until client asset
  proof exists for world `22`; reject zero-coordinate placeholders unless a
  specific row gains table/client proof. Closed in
  `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: `1,256` broad
  insert rows remain blocked as `blocked_laughingws_new_zone_broad_rows`.
- [ ] LWS-041 Halon Ring: keep the curated city/transport slice already covered,
  then review the remaining mostly zero-coordinate/area-zero rows for narrow
  quest, transport, or NPC proof. Closed in
  `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: only the curated
  city/transport crumbs remain covered by the city-content seed; `1,692` broad
  insert rows stay blocked as `blocked_laughingws_new_zone_broad_rows`.
- [ ] LWS-042 Murkmire: reject or keep blocked the TODO-heavy,
  placeholder-stat/quest-checklist rows until current script support and client
  asset proof exist. Closed in
  `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: `841` broad
  insert rows remain blocked as `blocked_laughingws_new_zone_broad_rows`.
- [ ] LWS-043 Test, unknown, unfinished, and not-in-client zones: build a
  sandbox-only asset proof checklist. Do not promote runtime seeds unless a
  future task explicitly targets a sandbox/client-asset investigation. Closed
  in `Decomp/Analysis/LAUGHINGWS_SANDBOX_ZONE_ASSET_PROOF_CHECKLIST.md`: all
  `23` sandbox/test/unknown/not-in-client files (`2,786` insert rows) remain
  rejected from runtime seed extraction as
  `sandbox_only_client_asset_check_required`.
- [ ] LWS-044 Historical Dominion/Exile arkship tutorial rows: keep audit-only
  for build 16042. Reopen only for an explicit historical/pre-16042 target with
  table/client proof for canonical placements, display/outfit, stats, and
  interaction behavior. Closed by the WorldDB audit's Arkship tutorial review:
  Dominion Arkship (`3` insert rows) and Exile Arkship (`40` insert rows) remain
  classified as `rejected_historical_arkship_tutorial_rows`.
- [ ] LWS-045 Rider's Reef `New Tutorial.sql` replacement rows: keep audit-only
  because the official `New Player Experience.sql` import remains authoritative.
  Reopen only if a specific branch-only row gains current retail proof despite
  the source testing-only/wrong-faction notes. Closed by the WorldDB audit's
  Rider's Reef replacement review: `Map/Instances/Tutorial Zones/New Tutorial.sql`
  (`38` insert rows) remains classified as
  `skip_replaces_required_riders_reef_import`.

## Milestone 4 - WorldDB Special Blockers

- [x] LWS-050 Dungeon Chase hidden SMC placeholder: map account item `86919`,
  source type `0`, and storefront item-data schema/wire limits. Implement only
  if the current database and 15-bit storefront transport can represent it.
  Closed as a mapped-only rejection in
  `Decomp/Analysis/LAUGHINGWS_DUNGEON_CHASE_SMC_PLACEHOLDER_REVIEW.md`: current
  `store_offer_item_data.itemId` and type-`0` `ServerStoreOffers` transport
  cannot safely represent `86919`; `LaughingWsStoreCatalogSeedTests` verifies
  the seed records the rejection without emitting the unsafe type-`0` row.
- [ ] LWS-051 Dust Stalker quest `4516`: smoke Boss Xagg, bridge controls, exit
  panel, self-destruct, and escape timing. Implement exit-panel behavior and
  timing only after logs/client behavior prove the trigger. Closed as
  mapped-only in `Decomp/Analysis/LAUGHINGWS_DUST_STALKER_Q4516_REVIEW.md`:
  Boss Xagg and bridge-control placement remain WIP/GUESSED data, while
  self-destruct and exit behavior stay blocked pending a harness smoke bundle.
- [ ] LWS-052 Arcterra Caretaker, Arcterra Coldblood portal, and Palaver Point
  Ish'amel: smoke exact placement, stats, interaction, portal target, and quest
  behavior before removing WIP/GUESSED status. Closed as mapped-only in
  `Decomp/Analysis/LAUGHINGWS_ARCTERRA_PALAVER_SOURCE_ONLY_REVIEW.md`: the
  preserved rows stay WIP/GUESSED until a harness bundle proves placement,
  portal target, interaction, and Palaver quest behavior.
- [ ] LWS-053 Skyplot housing: smoke vendor placement, return-pad interaction,
  vendor catalog completeness, and active housing lifecycle behavior. Promote
  only proven placement/catalog rows.
- [ ] LWS-054 Live events: smoke Battle Chase, Dungeon Chase, Shades Eve, Space
  Chase, Starfall, Sim Chase 1, and zPrix for spawn timing, vendor timing,
  lifecycle activation, and cleanup. Add tests for any runtime event state
  implemented.
- [ ] LWS-055 Quest virtual loot: capture quest-loot trigger cadence,
  probability, and objective integration for representative branch virtual-item
  rows. Keep current safe objective-loot preservation until retail probability
  evidence exists.

## Milestone 5 - Path, Settler, Explorer, Soldier, And Fortune Blockers

- [ ] LWS-060 Path persistence design: map the durable data model for active
  PathEpisode, PathMission, completed path missions, active path object ids,
  and per-character path reward history. Add EF models/migrations only after the
  schema is proven against current save/load behavior. Partial implementation:
  `character_path_mission` now persists active/completed mission state and
  replays unfinished episodes on initial packets; object-id state, reward
  history, and negative reload/path/faction/zone proof remain open.
- [ ] LWS-061 Path reward precision: map path XP/reward evidence, item counts,
  and unflagged `PathRewardType.Mission` grants. The WIP `30` XP fallback was
  replaced where evidence is exact: client `Lua_PathMission_GetRewardXp` reads
  `GameFormula` `0x017a` (`378`) `Dataint0` with fallback `50`. Exact
  per-mission reward precision remains blocked in the closure matrix.
- [ ] LWS-062 Path unlock sequencing: prove current-zone episode activation,
  mission prerequisite filtering, faction filtering, and replay/completion
  rules through table data plus client smoke. Partial implementation: local
  tests cover table-backed faction/prerequisite filters plus completed-mission
  no-replay and persisted-active no-duplicate activation; client smoke and exact
  transition ordering remain open.
- [ ] LWS-063 Settler hub state: map and implement durable built-group,
  resource, avenue, and hub progress state. Verify reload persistence and
  rejection paths. Partial implementation: Settler_Hub mission progress now
  persists/reloads through `character_path_mission`; built-group identity,
  resources, avenue totals, and failure result packets remain open.
- [ ] LWS-064 Settler/Soldier progress packets: decompile or capture packet
  fields for `ServerPathSettlerBuildStatus`,
  `ServerPathSettlerBuildResult`, and Soldier progress surfaces. Rename fields
  and widen behavior only after mapping. Implemented progress: `GetNumCompleted`
  maps Soldier_Assassinate and Settler_Hub to the first mission progress payload,
  now named `ProgressCount`; alternate `ProgressData`, Soldier holdout, and
  Settler failure/durable behavior remain blocked in the closure matrix.
  Additional mapped-only Soldier holdout progress: `Game.SoldierEvent` accessors
  are now labeled from `WildStar64.exe` `140682d60` through `140683dd0`, including
  wave count, waves released, health, and defend/auxiliary/escaping unit reads.
  This still does not prove server-side status/wave producer timing or generic
  holdout runtime semantics, so the task remains unchecked.
  Additional Settler packet progress: `ServerPathSettlerBuildStatus`
  (`0x0671`) and `ServerPathSettlerBuildStatusList` (`0x066E`) now have
  decompile-backed field names for hub id, counted status rows,
  improvement-group id, tier, remaining time, and bundle count; `ServerPathSettlerBuildResult`
  (`0x066C`) now names `PathSettlerImprovementId`. Non-success result values,
  failure ordering, resource costs, durable built-group state, and avenue/resource
  semantics remain blocked, so the task stays unchecked.
- [x] LWS-065 Generic path edge semantics: add focused tests for object-id
  completion guards, active-only Explorer Vista/ExploreZone/power-map
  completion, active Soldier assassinate credit, and already-complete/reload
  behavior.
- [ ] LWS-066 Fortune weights and rotations: capture retail
  `ServerFortuneRewards` or a storefront-server catalog dump. Replace emulator
  rarity-tier weights only with per-item/rotation evidence; otherwise keep the
  current Fortune implementation complete-but-approximate.

## Milestone 6 - Shared Instance/Public-Event Infrastructure

- [x] LWS-070 Build a repeatable manual smoke harness for instance, expedition,
  adventure, dungeon, raid, arena, battleground, and event-instance flows using
  `Start-BlockerEvidenceHarness.ps1`. Each bundle must include commands,
  server logs, screenshots/video, player ids, world ids, public event ids,
  objective ids, and negative cases. Implemented by extending
  `Decomp/Analysis/Start-BlockerEvidenceHarness.ps1` to create timestamped
  evidence bundles under `artifacts\blocker_evidence`, including
  `manifest.json`, command/server/client/negative-case templates,
  screenshots/video/log folders, and log-tail/collection helpers. The new
  `-CreateBundleOnly` mode validates bundle generation without touching running
  services.
- [ ] LWS-071 Public-event votes: map packet/UI behavior, implement vote state
  only for proven flows, and add tests for vote open, vote choice, timeout, and
  result. Partial packet-shape progress: opcode `0x06F7` now has a decoded
  15-bit value plus flag model, and opcode `0x0139` has a decoded uint32 value
  plus counted uint32 list model. Vote lifecycle, timeout, tally/result order,
  and participant/default-choice behavior remain blocked, so the row stays
  unchecked.
- [ ] LWS-072 Scoreboards and rewards: map exact scoreboard rows, score fields,
  reward delivery, and completion/failure result packets before adding
  cross-event reward behavior. Partial packet-shape progress: opcode `0x06FA`
  scoreboard request now has a mapped public-event id plus subscribe flag,
  `0x0130` stats update and `0x00D6` event end now have decompile-backed
  team/participant/objective/reward-threshold row shapes, and focused packet
  tests pin the current models. Runtime reward delivery, scoreboard population,
  eligibility, result ordering, and reward tier semantics remain blocked, so the
  row stays unchecked.
- [ ] LWS-073 Objective notification parity: map objective update text, event
  phase notification, completion notification, and quest-share behavior.
- [ ] LWS-074 Trigger/door framework: prove trigger rows, radius, coordinates,
  door ids, open/close state, cleanup timing, and respawn/despawn behavior.
- [ ] LWS-075 Cinematic/communicator framework: replace completion-only
  placeholders only after actor/camera/text/timing payloads and send conditions
  are mapped.
- [ ] LWS-076 Catalog cleanup: keep unused branch-only `PublicEventCreature`,
  `CommunicatorMessage`, `PublicEventObjective`, and `PublicEventPhase`
  catalogs unported until an active runtime consumer uses them.

## Milestone 7 - PvP And Adventure Content

- [ ] LWS-080 Cryo-Plex arena: run queue/match smoke, score/capture/round flow,
  rewards, and PvP stat capture. Implement only proven arena deltas.
- [ ] LWS-081 Daggerstone Pass: smoke queue, start/end, scoring, rewards,
  round/capture objectives, and stats.
- [ ] LWS-082 Halls of the Bloodsworn: smoke queue, start/end, scoring,
  rewards, objective flow, and stats.
- [ ] LWS-083 Walatiki Temple: smoke mask capture/return/scoring, round end,
  rewards, and stats.
- [ ] LWS-084 War of the Wilds: prove faction-specific start events `170` and
  `171`, end-delay behavior, chat timing, reward grant, and adventure smoke.
- [ ] LWS-085 Rage Logic: map vehicle choice, objective routing, rewards, and
  encounter behavior beyond the WIP `ChooseAVehicle` phase.

## Milestone 8 - Expedition Content

- [ ] LWS-090 Outpost M-13: find a real shuttle world-location trigger. Keep the
  branch id `5` zero-vector trigger rejected until table/client proof exists.
- [ ] LWS-091 Space Madness: prove branch door ids, direct local teleport,
  airlock/research trigger timing, real cinematic payload, combat/spawn
  behavior, and full expedition smoke.
- [ ] LWS-092 Fragment Zero: prove trigger timing, real cinematic payload,
  communicator timing, door behavior, and entity cleanup.
- [ ] LWS-093 Gauntlet: prove cinematics, announcer/communicator timing, arena
  doors, exact trigger timing, and full expedition smoke.
- [ ] LWS-094 Infestation: prove cargo-ship turnstile placement/range,
  door/open-vent choreography, real cinematic payload, medical-bay attack
  timing, parasite objective activation, and full smoke.
- [ ] LWS-095 Evil from the Ether: prove exact trigger rows/coordinates, direct
  medbay transition, door/marker cleanup, crew-log targeting/order, drive-spark
  interaction/visuals, Katja choreography, boss spell cadence/mechanics,
  cinematic ids/timing, rewards, and full smoke. Then review residual no-hook
  branch-only placements for promotion or rejection.
- [ ] LWS-096 Deep Space Exploration: map follow-up expedition routing, doors,
  real cinematic payload, encounter order, rewards, and full smoke.

## Milestone 9 - Dungeon Content

- [ ] LWS-100 Coldblood Citadel: prove optional route weights, objective trigger
  placement, door/choreography, Hailstone ability cadence, and full dungeon
  route.
- [ ] LWS-101 Protogames Academy: prove Invulnotron, Gromka, Iruki Boldbeard,
  Seek-N-Slaughter, Icebox Mk. 2 mechanics, trigger timing, communicator timing,
  platform/launcher cleanup, and entity-removal choreography.
- [ ] LWS-102 Ruins of Kel Voreth: prove boss mechanics, trigger placement,
  door choreography, optional route weights/objective availability,
  faction-specific communicator targeting, real Blood Pit cinematic payload,
  and the duplicate-owner Forgemaster/Drokk trigger split.
- [ ] LWS-103 Stormtalon's Lair: prove boss combat, trigger placement/count,
  real Stormtalon reborn cinematic payload, optional route weights, objective
  availability, and boss spawn/version selection.
- [ ] LWS-104 Skullcano: prove boss mechanics, cave/chasm route weights, Chief
  Kaskalak choreography, trigger placement, door/platform choreography,
  communicator timing/faction pairing, optional objective availability, and full
  smoke.
- [ ] LWS-105 Sanctuary of the Swordmaiden: prove miniboss mechanics, Temple vs
  Moldwood route weights, objective availability, trigger placement, door
  choreography, communicator timing, and full smoke.
- [ ] LWS-106 Ultimate Protogames dungeon: prove room randomization, objective
  routing, boss mechanics, rewards, and full smoke beyond `RandomEvent1`.

## Milestone 10 - Raid And Event-Instance Content

- [ ] LWS-110 Initialization Core Y-83: prove quarantine/corridor trigger
  placement, door entity choreography, communicator/cinematic timing, target-set
  proof, real open-door cinematic payload, and raid smoke.
- [ ] LWS-111 Red Moon Terror: prove Ish'amel/engineering timing, Laveka
  choreography, awakening/challenge mechanics, door/elevator movement, and raid
  smoke.
- [ ] LWS-112 Genetic Archives: prove Experiment X-89, Kuralak, Kuralak pillar,
  Ohmna, weekly/random encounter selection, boss choreography, communicator and
  cinematic timing, door/elevator movement, and raid smoke.
- [ ] LWS-113 Datascape: map Hydroflux and Mnemesis mechanics, communicator and
  cinematic timing, encounter choreography, door/trigger placement, exact
  wing-order proof, challenge mechanics, and raid smoke. Only then implement the
  all-in-one-only `HydrofluxLogicAndWaterEntityScript` and
  `MnemesisLogicAndWaterEntityScript` references.
- [ ] LWS-114 Ultimate Protogames raid: prove Downsizer challenge semantics,
  boss mechanics, rewards, and raid smoke.
- [ ] LWS-115 Shade's Eve: prove gather-ring cleanup, town-gate opening,
  communicator/cinematic timing, real cinematic payload, exact vote follow-up,
  Etty/fountain seed interaction smoke, and event smoke.
- [ ] LWS-116 Protostar SuperMall in the Sky: prove store/room routing,
  encounter logic, real cinematic payload, rewards, and full smoke.
- [ ] LWS-117 Journey into OMNICore-1: map real cinematic actor/camera/text/timing
  payload, event routing, rewards, and encounter behavior before replacing the
  immediate completion placeholder.

## Milestone 11 - Verification And Closure

- [x] LWS-120 For every implemented data overlay, run the extractor, analyzer,
  py-compile, setup import path if safe, and `verify_safe_world_imports.sql`.
- [x] LWS-121 For every instance or public-event script change, add focused tests
  under `Source\NexusForever.Game.Tests\Instances` or the owning test folder.
- [x] LWS-122 Run the narrow owning build/test after each pass, usually:
  `dotnet build Source\NexusForever.Script.Instance\NexusForever.Script.Instance.csproj --no-restore -v minimal --nologo`
  and
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`.
- [x] LWS-123 For cross-service packet, path, storefront, or persistence changes,
  run the full solution build:
  `dotnet build Source\NexusForever.sln -v minimal --nologo`.
- [x] LWS-124 Update the three LaughingWS audit docs, `CURRENT_STATUS.md`,
  `MISSING_FEATURE_MATRIX.md`, and any focused status tracker with the final
  state of each item.
- [x] LWS-125 End each pass in exactly one state: implemented with verification,
  mapped-only with the next evidence source named, or rejected with a recorded
  reason.
