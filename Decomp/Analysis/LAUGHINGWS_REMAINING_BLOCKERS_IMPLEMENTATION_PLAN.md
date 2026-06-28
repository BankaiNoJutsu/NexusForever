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
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-Lws036ChecklistSmoke` creates a dedicated `lws-036-targets.md` worksheet,
  default target worlds, and required negative cases for this smoke gate. The
  row stays unchecked until an actual completed client/server evidence bundle
  proves the listed WIP/GUESSED rows. Dry-run guard progress (2026-05-28):
  `test_blocker_evidence_harness_presets.py` now verifies this preset's
  manifest, worksheet, default world ids, helper files, and negative-case
  scaffold with `-CreateBundleOnly`.
- [ ] LWS-037 Convert smoke-proven WIP/GUESSED checklist/entity rows to
  implemented status in audit docs. Leave unproven rows explicitly WIP/GUESSED
  or rejected.

## Milestone 3 - New-Zone And Sandbox Content Gates

- [x] LWS-040 Dreadmoor: keep the current broad rows blocked until client asset
  proof exists for world `22`; reject zero-coordinate placeholders unless a
  specific row gains table/client proof. Tracked in
  `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: `1,256` broad
  insert rows remain blocked as `blocked_laughingws_new_zone_broad_rows`.
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-NewZoneAssetProof` creates a row-level asset-proof worksheet and required
  negative/safety cases for this new-zone gate. Analyzer guard progress
  (2026-05-28): `test_laughingws_worlddb_classifications.py` asserts this
  source stays blocked and not additive. Dry-run guard progress (2026-05-28):
  `test_blocker_evidence_harness_presets.py` verifies this preset's manifest,
  shared asset-proof worksheet, default world ids, helper files, and
  negative-case scaffold with `-CreateBundleOnly`. This row is complete as a
  blocked broad-row guard; no Dreadmoor runtime rows are promoted.
- [x] LWS-041 Halon Ring: keep the curated city/transport slice already covered,
  then review the remaining mostly zero-coordinate/area-zero rows for narrow
  quest, transport, or NPC proof. Tracked in
  `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: only the curated
  city/transport crumbs remain covered by the city-content seed; `1,692` broad
  insert rows stay blocked as `blocked_laughingws_new_zone_broad_rows`.
  `-NewZoneAssetProof` now provides the repeatable capture worksheet, but broad
  residual rows stay blocked until a specific row gains proof beyond the curated
  city/transport seed. The analyzer guard asserts the broad file stays blocked
  and not additive; the dry-run guard verifies the shared asset-proof preset.
  This row is complete as a blocked residual-row guard; the already-reviewed
  Halon city/transport crumbs remain the only covered runtime slice.
- [x] LWS-042 Murkmire: reject or keep blocked the TODO-heavy,
  placeholder-stat/quest-checklist rows until current script support and client
  asset proof exist. Tracked in
  `Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: `841` broad
  insert rows remain blocked as `blocked_laughingws_new_zone_broad_rows`.
  `-NewZoneAssetProof` now provides the repeatable capture worksheet for any
  future row-level Murkmire investigation. The analyzer guard asserts this
  source stays blocked and not additive; the dry-run guard verifies the shared
  asset-proof preset. This row is complete as a blocked broad-row guard; no
  Murkmire runtime rows are promoted.
- [x] LWS-043 Test, unknown, unfinished, and not-in-client zones: build a
  sandbox-only asset proof checklist. Do not promote runtime seeds unless a
  future task explicitly targets a sandbox/client-asset investigation. Tracked
  in `Decomp/Analysis/LAUGHINGWS_SANDBOX_ZONE_ASSET_PROOF_CHECKLIST.md`: all
  `23` sandbox/test/unknown/not-in-client files (`2,786` insert rows) remain
  rejected from runtime seed extraction as
  `sandbox_only_client_asset_check_required`. `-NewZoneAssetProof` now creates a
  worksheet for future explicit sandbox investigations and keeps normal
  build-16042 runtime safety proof mandatory. The analyzer guard asserts all
  `23` test-zone files remain sandbox-only and not additive; the dry-run guard
  verifies the shared asset-proof preset. This row is complete as a runtime
  rejection plus future sandbox-proof checklist; no sandbox/test-zone runtime
  seeds are promoted.
- [x] LWS-044 Historical Dominion/Exile arkship tutorial rows: keep audit-only
  for build 16042. Reopen only for an explicit historical/pre-16042 target with
  table/client proof for canonical placements, display/outfit, stats, and
  interaction behavior. Tracked by the WorldDB audit's Arkship tutorial review:
  Dominion Arkship (`3` insert rows) and Exile Arkship (`40` insert rows) remain
  classified as `rejected_historical_arkship_tutorial_rows`; the analyzer guard
  asserts both files keep that rejection and are not additive. This row is
  complete as a build-16042 rejection, not as historical arkship restoration.
- [x] LWS-045 Rider's Reef `New Tutorial.sql` replacement rows: keep audit-only
  because the official `New Player Experience.sql` import remains authoritative.
  Reopen only if a specific branch-only row gains current retail proof despite
  the source testing-only/wrong-faction notes. Tracked by the WorldDB audit's
  Rider's Reef replacement review: `Map/Instances/Tutorial Zones/New Tutorial.sql`
  (`38` insert rows) remains classified as
  `skip_replaces_required_riders_reef_import`; the analyzer guard asserts it
  stays skipped and not additive. This row is complete as a current-runtime
  replacement rejection, not as Rider's Reef parity.

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
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-DustStalkerQ4516Smoke` now creates a dedicated
  `lws-051-dust-stalker-targets.md` worksheet, default world/objective filters,
  and negative cases for premature bridge use, premature exit use, timer expiry,
  and repeat interaction. This is capture infrastructure only; the row stays
  unchecked until a completed client/server bundle proves the sequence. Dry-run
  guard progress (2026-05-28): `test_blocker_evidence_harness_presets.py`
  verifies this preset's manifest, worksheet, default ids, helper files, and
  negative-case scaffold.
- [ ] LWS-052 Arcterra Caretaker, Arcterra Coldblood portal, and Palaver Point
  Ish'amel: smoke exact placement, stats, interaction, portal target, and quest
  behavior before removing WIP/GUESSED status. Closed as mapped-only in
  `Decomp/Analysis/LAUGHINGWS_ARCTERRA_PALAVER_SOURCE_ONLY_REVIEW.md`: the
  preserved rows stay WIP/GUESSED until a harness bundle proves placement,
  portal target, interaction, and Palaver quest behavior.
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-ArcterraPalaverSourceSmoke` now creates a dedicated
  `lws-052-arcterra-palaver-targets.md` worksheet, default world/objective
  filters, and negative cases for invalid portal target/state, absent Palaver
  quest state, missing Caretaker prerequisite, and repeat interaction. This is
  capture infrastructure only; the row stays unchecked until a completed
  client/server bundle proves the source-only behavior. The dry-run guard now
  verifies this preset's manifest, worksheet, default ids, helper files, and
  negative-case scaffold.
- [ ] LWS-053 Skyplot housing: smoke vendor placement, return-pad interaction,
  vendor catalog completeness, and active housing lifecycle behavior. Promote
  only proven placement/catalog rows.
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-SkyplotHousingSmoke` now creates a dedicated
  `lws-053-skyplot-housing-targets.md` worksheet, default world filter, and
  negative cases for return pad without residence/community session, vendor
  before unlock, invalid catalog rows, and inactive lifecycle. This is capture
  infrastructure only; the row stays unchecked until a completed client/server
  bundle proves placement, catalog, return-pad, and lifecycle behavior. The
  dry-run guard now verifies this preset's manifest, worksheet, default world
  id, helper files, and negative-case scaffold.
- [ ] LWS-054 Live events: smoke Battle Chase, Dungeon Chase, Shades Eve, Space
  Chase, Starfall, Sim Chase 1, and zPrix for spawn timing, vendor timing,
  lifecycle activation, and cleanup. Add tests for any runtime event state
  implemented.
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-LiveEventSmoke` now creates a dedicated `lws-054-live-event-targets.md`
  worksheet and negative cases for inactive event state, event ending during
  vendor interaction, post-cleanup rejection, and skipped duplicate/placeholder/
  malformed source row absence. This is capture infrastructure only; the row
  stays unchecked until completed client/server bundles prove concrete
  per-event lifecycle behavior. The dry-run guard now verifies this preset's
  manifest, worksheet, helper files, and negative-case scaffold.
- [ ] LWS-055 Quest virtual loot: capture quest-loot trigger cadence,
  probability, and objective integration for representative branch virtual-item
  rows. Keep current safe objective-loot preservation until retail probability
  evidence exists.
  Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
  `-QuestVirtualLootSmoke` now creates a dedicated
  `lws-055-quest-virtual-loot-targets.md` worksheet, default representative
  objective filters, and negative cases for inactive objectives, pre-activation
  kills, completed-objective repeats, and declined/abandoned loot windows. This
  is capture infrastructure only; the row stays unchecked until completed
  client/server bundles prove trigger cadence, probability, source selection,
  and objective integration. The dry-run guard now verifies this preset's
  manifest, worksheet, default objective ids, helper files, and negative-case
  scaffold. Runtime-boundary coverage now pins the current safe behavior:
  `ItemContainerLootTests.QuestVirtualLootGroup_DropsOnlyWhenQuestObjectiveIsActive`
  requires condition `8` to stay behind `IsActiveObjectiveId`, and
  `LootInstanceDeliveryTests.GiveLoot_VirtualItemUpdatesVirtualCollectObjective`
  requires mapped virtual-item delivery to update
  `QuestObjectiveType.VirtualCollect` while emitting only the mapped loot grant.

## Milestone 5 - Path, Settler, Explorer, Soldier, And Fortune Blockers

- [ ] LWS-060 Path persistence design: map the durable data model for active
  PathEpisode, PathMission, completed path missions, active path object ids,
  and per-character path reward history. Add EF models/migrations only after the
  schema is proven against current save/load behavior. Partial implementation:
  `character_path_mission` now persists active/completed mission state and
  hydrates it on login without replaying unfinished episodes on initial packets;
  object-id state, reward history, and negative reload/path/faction/zone proof
  remain open. Runtime progress (2026-06-04): live crash proof showed
  persisted multi-episode login replay can crash `OnPlayerPathRefresh`, so
  persisted rows no longer pre-mark episodes active or suppress current-zone
  activation for the same episode id. Focused path/pregame/world-entry tests
  passed `214/214`, followed by three clean local client logins; object-id
  state, reward history, broader zone/reload negatives, and exact activation
  ordering remain open.
- [ ] LWS-061 Path reward precision: map path XP/reward evidence, item counts,
  and unflagged `PathRewardType.Mission` grants. The WIP `30` XP fallback was
  replaced where evidence is exact: client `Lua_PathMission_GetRewardXp` reads
  `GameFormula` `0x017a` (`378`) `Dataint0` with fallback `50`. Focused
  reward/path tests now pass `74/74`, covering matching mission reward object
  ids, supported payloads, flagged-row rejection, and zero-count item fallback.
  Exact per-mission reward precision, presentation, overflow, reward history,
  and retail flagged-row semantics remain blocked in the closure matrix.
- [ ] LWS-062 Path unlock sequencing: prove current-zone episode activation,
  mission prerequisite filtering, faction filtering, and replay/completion
  rules through table data plus client smoke. Partial implementation: local
  tests cover table-backed faction/prerequisite filters plus completed-mission
  no-replay and persisted-active current-zone activation; broader client smoke
  and exact transition ordering remain open.
  Runtime progress (2026-06-04): login no longer replays persisted active
  episodes after a local `OnPlayerPathRefresh` crash was correlated with three
  persisted episode groups sent after `ClientEnteredWorld`. Persisted rows still
  hydrate durable state, while guarded current-zone activation handles runtime
  episode packets. Focused path/pregame/world-entry tests passed `214/214`.
- [ ] LWS-063 Settler hub state: map and implement durable built-group,
  resource, avenue, and hub progress state. Verify reload persistence and
  rejection paths. Partial implementation: Settler_Hub mission progress now
  persists/reloads through `character_path_mission`; built-group identity,
  resources, avenue totals, and failure result packets remain open.
  Runtime progress (2026-05-28): focused rejection tests now pin that zero or
  missing improvement-group ids, missing hub rows, different hub ids, and
  non-Settler active paths do not progress the mission or emit extra mission
  updates. Build-handler guard tests now also pin that out-of-range ids,
  unmapped tiers, zero improvement ids, and improvement ids outside the mapped
  15-bit result width do not emit status/result packets. Focused
  `PathSettlerBuildHandlerTests|PathManagerTests` passed `55/55`; built-group
  identity, resources, avenue totals, non-success result values, and failure
  ordering remain open.
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
  semantics remain blocked, so the task stays unchecked. Build-handler guard
  tests now pin the current no-emit boundary for unmapped or unwritable Settler
  build acknowledgements without inventing failure packets.
  Additional Soldier packet-shape progress: `ServerPathSoldierHoldoutStatus`,
  `ServerPathSoldierHoldOutNextWave`, `ServerPathSoldierHoldoutEnd`, and
  `ServerPathSoldierHoldoutDeath` now have source comments tied to the mapped
  `Game.SoldierEvent` accessor surface and focused packet tests pin the current
  serialized status/next-wave/end/death shapes. Generic holdout producer timing,
  wave scheduling, unit ownership, death/failure semantics, and packet ordering
  remain blocked, so the task stays unchecked.
- [x] LWS-065 Generic path edge semantics: add focused tests for object-id
  completion guards, active-only Explorer Vista/ExploreZone/power-map
  completion, active Soldier assassinate credit, and already-complete/reload
  behavior.
- [ ] LWS-066 Fortune weights and rotations: capture retail
  `ServerFortuneRewards` or a storefront-server catalog dump. Replace emulator
  rarity-tier weights only with per-item/rotation evidence; otherwise keep the
  current Fortune implementation complete-but-approximate. Evidence-harness
  progress: `Start-BlockerEvidenceHarness.ps1 -FortuneRewardsSmoke` now creates
  a worksheet for `ServerFortuneRewards` probabilities, active
  rotation/storefront context, card deal/flip/payout, reload persistence,
  insufficient currency, invalid flip, and repeated-flip rejection. The row
  remains blocked until a completed bundle contains retail/catalog evidence.
  Dry-run guard progress (2026-05-28): `test_blocker_evidence_harness_presets.py`
  now verifies this preset's manifest, worksheet, helper files, and
  negative-case scaffold. Runtime-boundary coverage now exercises the real
  `FortuneRewardPool` against synthetic `AccountItem.tbl`/`Item2.tbl` rows:
  `GetRewardCatalog_RealPoolAdvertisesOnlyMappedItemRewards` pins the mapped
  item/probability transport and filters Fortune Coin/non-item rows from
  `ServerFortuneRewards`, while
  `PickCardRewards_RealPoolKeepsNonItemAccountRewardsPickerOnly` preserves
  current picker-only account rewards without advertising unmapped item2
  probabilities.

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
  and participant/default-choice behavior remain blocked. Runtime hardening
  progress (2026-05-28): `PublicEventVote.Choice` is now idempotent for
  finalised votes, non-participants, duplicate responses, and invalid choices,
  and focused tests cover initiate, tally/end, timeout default choice, and
  duplicate/late/invalid response handling; packet tests also pin client vote
  read plus server initiate/detailed-initiate/tally/end serialization, and a
  handler test pins routing from `0x06EE` to `PublicEventManager.RespondVote`.
  Additional envelope-hardening progress (2026-05-28): the mapped client vote
  id/team id are now threaded through the response path; the public-event
  runtime ignores cross-team and stale-vote replies before mutating active vote
  state, and focused tests pin handler forwarding plus vote-id mismatch
  rejection.
  Retail UI packet sequence, exact timeout cadence, result order, vote/team id
  mismatch UI feedback, and participant/default-choice semantics remain blocked,
  so the row stays unchecked.
  Evidence-harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1
  -PublicEventVoteScoreboardSmoke` now creates a worksheet for vote
  initiate/detailed-initiate, client choices, tally/end ordering,
  timeout/default-choice behavior, vote/team id handling, and negative vote
  cases. The dry-run guard now verifies this preset's manifest, worksheet,
  helper files, and negative-case scaffold.
- [ ] LWS-072 Scoreboards and rewards: map exact scoreboard rows, score fields,
  reward delivery, and completion/failure result packets before adding
  cross-event reward behavior. Partial packet-shape progress: opcode `0x06FA`
  scoreboard request now has a mapped public-event id plus subscribe flag,
  `0x0130` stats update and `0x00D6` event end now have decompile-backed
  team/participant/objective/reward-threshold row shapes, and focused packet
  tests pin the current models. Runtime progress (2026-05-28): scoreboard
  subscribe requests now emit one participant-scoped `0x0130` snapshot for the
  requested event, and team stat rows now aggregate per-member absolute stat
  values instead of overwriting with the last member update; focused runtime
  tests cover handler routing, unsubscribe no-op before map/event lookup,
  non-member suppression, and combined
  team/participant stat payloads. Runtime reward delivery, exact live
  subscription cadence, eligibility, result ordering, score-field precision, and
  reward tier semantics remain blocked, so the row stays unchecked.
  Evidence-harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1
  -PublicEventVoteScoreboardSmoke` now creates a worksheet for scoreboard
  subscribe/unsubscribe cadence, stat row payloads, event end/reward-threshold
  rows, non-member request suppression, and reward delivery proof. The dry-run
  guard verifies the same shared preset without proving cadence or rewards.
- [ ] LWS-073 Objective notification parity: map objective update text, event
  phase notification, completion notification, and quest-share behavior.
  Packet-shape progress (2026-05-27): decompile labels now map objective
  notification mode `0x0133`, objective status update `0x0134`, objective
  update `0x0132`, and objective start `0x06F9`; focused packet tests pin the
  15-bit objective ids, status row, notification mode, location list, and
  map-region list. Text ids, audience, packet ordering, event phase/completion
  notification semantics, and quest-share behavior remain blocked, so the row
  stays unchecked. Runtime hardening progress (2026-05-28): unchanged
  objective busy-state calls no longer emit duplicate full objective updates,
  with the guard tied to `WildStar64.exe` `14007b490` / opcode `0x0132` and
  focused objective tests covering the no-duplicate boundary. This still does
  not prove notification parity. Evidence-harness progress (2026-05-28):
  `Start-BlockerEvidenceHarness.ps1 -PublicEventObjectiveNotificationSmoke`
  now creates an LWS-073 worksheet for objective start/update/status/
  notification packets, objective/phase text ids, quest-share side effects,
  target audience, ordering, duplicate suppression, and negative cases. The
  dry-run guard verifies this preset's manifest, worksheet, helper files, and
  negative-case scaffold without proving notification parity.
- [ ] LWS-074 Trigger/door framework: prove trigger rows, radius, coordinates,
  door ids, open/close state, cleanup timing, and respawn/despawn behavior.
  Runtime producer hardening progress (2026-05-28): the existing mapped
  `ParticipantsInTriggerVolume` and `Turnstile` public-event objective
  producers now ignore zero object ids and non-player entities, and tests cover
  trigger-volume enter/leave deltas, world-location horizontal radius plus
  vertical clamp behavior, turnstile one-shot entry, duplicate in-range
  suppression, and the no-leave-delta turnstile boundary. Exact trigger rows,
  coordinates, door ids/states, cleanup timing, and respawn/despawn behavior
  remain blocked, so the row stays unchecked.
- [ ] LWS-075 Cinematic/communicator framework: replace completion-only
  placeholders only after actor/camera/text/timing payloads and send conditions
  are mapped. Packet-shape progress (2026-05-28): communicator/story-panel
  packet comments now record the mapped client export anchors
  `DB\CommunicatorMessages.tbl`, `Communicator_ShowQuestMsg`,
  `DB\StoryPanel.tbl`, `MessageManager_DisplayStoryPanel`, and CommunicatorLib
  placement/overlay/background symbols; packet tests pin
  `ServerCommunicatorMessage`, `ServerStoryTextCommunicator`, and
  `ServerStoryPanelCustomShow` serialization plus shared `StoryMessage`
  actor-row serialization for creature, custom text, localized text, player,
  creature-unit, and self-player token sources. Real actor/camera/text timing,
  send conditions, completion-only placeholder replacement, and cinematic
  sequencing remain blocked, so the row stays unchecked.
- [x] LWS-076 Catalog cleanup: keep unused branch-only `PublicEventCreature`,
  `CommunicatorMessage`, `PublicEventObjective`, and `PublicEventPhase`
  catalogs unported until an active runtime consumer uses them. Guard progress
  (2026-05-28): `BranchCatalogCleanupTests` pins the current rejection by
  asserting the all-in-one-only Datascape Hydroflux/Mnemesis script names and
  known door/platform/marker-only or empty branch catalog files for War of the
  Wilds, Protogames Academy, Ruins of Kel Voreth, Skullcano, Shade's Eve,
  Gauntlet, Fragment Zero, Infestation, Outpost M-13, Space Madness,
  Datascape, Genetic Archives, Initialization Core Y-83, and Red Moon Terror
  remain absent until stronger consumer proof exists. This row is complete as a
  rejected/no-port guard, not as runtime feature parity.

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
  Evidence-harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1
  -PvpAdventureSmoke` now creates the LWS-080 through LWS-085 worksheet,
  defaults the known branch worlds/events, and records queue/match,
  disconnect/leave, premature reward, wrong-team/wrong-phase, and early-finish
  negative cases. The scaffold tests still pass `29/29`; queue/match, scoring,
  rewards, stats, faction-start, vehicle, and encounter behavior remain blocked
  until completed smoke bundles exist. The dry-run guard now verifies this
  preset's manifest, worksheet, default world/public-event ids, helper files,
  and negative-case scaffold.

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
  Evidence-harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1
  -ExpeditionSmoke` now creates the LWS-090 through LWS-096 worksheet, defaults
  the known expedition worlds/events, and records placeholder trigger,
  premature door/shuttle/teleport, wrong-phase trigger, completion-only
  cinematic, and early-finish reward negative cases. The focused expedition
  scaffold tests now pass `78/78`; shuttle, door, trigger, cinematic,
  communicator, teleport, cleanup, routing, reward, and full-smoke behavior
  remain blocked until completed smoke/decompile evidence exists. The dry-run
  guard now verifies this preset's manifest, worksheet, default
  world/public-event ids, helper files, and negative-case scaffold.

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
  `Start-BlockerEvidenceHarness.ps1 -DungeonSmoke` now creates the LWS-100
  through LWS-106 worksheet, defaults the map-script-backed worlds `382`,
  `1263`, `1271`, `1336`, `2980`, `3173`, and `3522`, defaults public events
  `145`, `148`, `161`, `166`, `594`, `667`, and `907`, and captures required
  negative cases for premature triggers, doors/platforms/launchers/teleporters,
  wrong-route optional objectives, completion-only cinematics, and early
  finish/rewards. The focused dungeon scaffold filter still passes `149/149`;
  route weights, trigger/door/choreography, real cinematics, boss mechanics,
  rewards, and full-route smoke remain blocked pending completed bundles. The
  dry-run guard now verifies this preset's manifest, worksheet, default
  world/public-event ids, helper files, and negative-case scaffold.

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
- [ ] LWS-115 Shade's Eve: prove gather-ring cleanup, town-gate opening,
  communicator/cinematic timing, real cinematic payload, exact vote follow-up,
  Etty/fountain seed interaction smoke, and event smoke.
- [ ] LWS-116 Protostar SuperMall in the Sky: prove store/room routing,
  encounter logic, real cinematic payload, rewards, and full smoke.
- [ ] LWS-117 Journey into OMNICore-1: map real cinematic actor/camera/text/timing
  payload, event routing, rewards, and encounter behavior before replacing the
  immediate completion placeholder.
  `Start-BlockerEvidenceHarness.ps1 -RaidEventSmoke` now creates the LWS-110
  through LWS-117 worksheet, defaults the map-script-backed worlds `1333`,
  `1462`, `3032`, `3040`, `3044`, `3045`, and `3094`, defaults public
  events `157`, `159`, `595`, `597`, `605`, `679`, and `705`, and
  captures required negative cases for premature triggers/target sets,
  doors/elevators/gates/rooms, wrong-route weekly/wing/room selection,
  completion-only cinematics, and early finish/rewards. The focused
  raid/event-instance scaffold filter still passes `169/169`; target sets,
  routing, doors/elevators/gates, real cinematics, encounter mechanics, rewards,
  and full smoke remain blocked pending completed bundles. The dry-run guard
  now verifies this preset's manifest, worksheet, default world/public-event ids,
  helper files, and negative-case scaffold.

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
