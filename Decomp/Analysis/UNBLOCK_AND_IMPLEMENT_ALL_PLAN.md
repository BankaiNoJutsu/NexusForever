# NexusForever Unblock And Implement All Plan

Created: 2026-06-17
Updated: 2026-06-20 CEST

This plan turns the current blocked, diagnostic-only, partial, and
`not_retail_complete` surfaces into a repeatable closure program. It is not a
single implementation patch. Each slice must end as one of:

- `Implemented`: evidence-backed behavior changed, verification passed, and
  trackers were updated.
- `Mapped only`: evidence improved, but runtime mutation remains blocked with a
  named missing source.
- `Rejected`: the hypothesis did not hold and no speculative behavior remains.

Do not widen runtime behavior from a packet shape, string match, generated
field name, or WIP/GUESSED data row alone.

## Current Baseline

- Total feature areas: 36.
- Implementation-complete areas: 13.
- Partial areas: 21.
- Diagnostic or structural-only areas: 2.
- Consolidated runtime producer/emit gaps: 5.
- Next restoration queue rows: 23.
- Next restoration queue blocker detail rows: 2,286.
- Non-blocker content inventory rows still marked `not_retail_complete`:
  165,606.
- Full validator scope: 31 generated files / 167,892 rows, all currently
  `not_retail_complete`.
- Production `TODO` / `FIXME` / `NotImplemented` markers: 0 by the current
  status audit; remaining `NotImplementedException` hits are test doubles.

Primary source-of-truth files:

- `CURRENT_STATUS.md`
- `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`
- `Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md`
- `Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md`
- `Decomp/Analysis/GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`
- `Decomp/Analysis/MATCHING_IMPLEMENTATION_STATUS.md`
- `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`
- `Decomp/Analysis/ENTITY_AUX_DECODE_ROADMAP.md`

Current next-slice actionability:

| State | Rows |
| --- | ---: |
| `quest_validation_ready` | 12 |
| `split_required_high_blocker_volume` | 9 |
| `instance_validation_ready` | 1 |
| `split_required_producer_gap` | 1 |

Current next-slice blocking source:

| Source | Rows |
| --- | ---: |
| `client_smoke` | 14 |
| `datamapping_review` | 6 |
| `runtime_test` | 3 |

Current next-slice blocker-detail categories:

| Category | Rows |
| --- | ---: |
| `instance_dependency` | 736 |
| `public_event_evidence` | 584 |
| `script_objective_producer` | 538 |
| `instance_entity` | 105 |
| `quest_world_dependency` | 89 |
| `instance_reward` | 48 |
| `quest_reward` | 43 |
| `quest_prerequisite` | 37 |
| `quest_objective` | 26 |
| `quest_achievement` | 14 |
| `quest_loot` | 14 |
| `quest_script` | 12 |
| `quest_episode_evidence` | 11 |
| `instance_portal_evidence` | 11 |
| `quest_creature` | 10 |
| `script_runtime_action` | 5 |
| `script_presentation` | 3 |

Largest full-inventory generated families:

| Inventory | Rows |
| --- | ---: |
| `content_retail_completeness_quest_world_dependencies.csv` | 39,038 |
| `content_retail_completeness_challenge_evidence.csv` | 22,721 |
| `content_retail_completeness_quest_prerequisites.csv` | 14,965 |
| `content_retail_completeness_quest_creatures.csv` | 13,412 |
| `content_retail_completeness_quest_zone_evidence.csv` | 11,285 |
| `content_retail_completeness_quest_rewards.csv` | 8,767 |
| `content_retail_completeness_quest_reward_evidence.csv` | 8,618 |
| `content_retail_completeness_quest_loot.csv` | 7,650 |
| `content_retail_completeness_quest_objectives.csv` | 7,108 |
| `content_retail_completeness_quests.csv` | 5,194 |

## Current Queue Contract

The next-slice queue is the execution driver. The full inventory is the backlog
and rollup source; do not select directly from it until the generator promotes a
row into an actionable queue or a targeted full-inventory campaign is explicitly
chosen.

Process current queue rows in rank order unless a row is blocked by an
unavailable dependency. After each row:

1. Record the final state as implemented, mapped-only, rejected, or blocked.
2. Regenerate at least `next-slice`.
3. Run the validator.
4. Re-read `content_retail_completeness_next_slice_queue.csv` before selecting
   the next row.

Current queue order:

| Rank | Slice | Primary blocker | Source | Action |
| ---: | --- | --- | --- | --- |
| 1 | `quest-5597` Dregs and Thieves | `quest_loot` | client smoke | Capture dialog, objective, loot, reward, achievement, creature bridge, and chain smoke. |
| 2 | `quest-3479` From the Wreckage | `quest_world_dependency` | client smoke | Capture survivor/yeti/dialog/reward/achievement smoke and verify excluded-quest branch behavior. |
| 3 | `quest-3480` Reporting for Duty | `quest_world_dependency` | client smoke | Capture survivor/yeti/dialog/reward/achievement smoke and resolve the unmatched starter bridge if still present. |
| 4 | `quest-3486` Empowered Tower | `quest_loot` | client smoke | Capture loot/reward/story/objective smoke and reward provenance. |
| 5 | `quest-3668` Indigenous Intelligence | `quest_world_dependency` | client smoke | Capture world, objective, receiver, reward, achievement, and remaining TargetGroup smoke; keep missing `14054`/`11913` evidence blocked unless reviewed placement exists. |
| 6 | `quest-3673` Contact with Thayd | `quest_reward` | client smoke | Capture signal flare, hidden completion, reward, achievement, and finisher proof. |
| 7 | `quest-3886` Fiery Distraction | `quest_world_dependency` | client smoke | Capture Durek, torch, Skeech Hut activation, reward, achievement, and Q3886 -> Q3673 smoke. |
| 8 | `quest-3963` More Important Than Revenge | `quest_world_dependency` | client smoke | Capture starter, objective, teleporter, reward, achievement, and route smoke. |
| 9 | `quest-5573` Powering Down | `quest_world_dependency` | client smoke | Capture Power Regulator, cinematic, reward, and branch smoke. |
| 10 | `quest-5575` Seizing Power | `quest_world_dependency` | client smoke | Capture Power Regulator, hidden objective, reward, and branch smoke. |
| 11 | `quest-5580` Enforced Radio Silence | `quest_world_dependency` | client smoke | Capture Tower Controls, Kezrek, reward, achievement, merge, and route smoke. |
| 12 | `quest-3797` Securing the Area | `quest_world_dependency` | client smoke | Capture objective, route, reward, achievement, and branch smoke; leave unresolved world-dependency evidence named. |
| 13 | `instance-2980` Ultimate Protogames | `instance_dependency` | client smoke | Split by room/objective; do not attempt full dungeon closure. |
| 14 | `instance-382` Stormtalon's Lair | `public_event_evidence` | DataMapping review | Split by boss/objective/challenge row. |
| 15 | `instance-3009` Vault of the Archon | `script_objective_producer` | runtime test | Split by public-event objective and mechanic chain. |
| 16 | `instance-1271` Sanctuary of the Swordmaiden | `public_event_evidence` | DataMapping review | Split by terrace/temple/boss public-event chain. |
| 17 | `instance-3180` Fragment Zero | `script_objective_producer` | runtime test | Split by search trigger, ambush, timer, and cinematic rows. |
| 18 | `instance-1263` Skullcano | `public_event_evidence` | DataMapping review | Split by route, Thunderfoot, Seismic Tremor, and challenge rows. |
| 19 | `instance-2183` Gauntlet | `public_event_evidence` | DataMapping review | Split by opening/arena/score/timer rows. |
| 20 | `instance-3404` Evil from the Ether | `script_objective_producer` | runtime test | Split by portal, drive/log/teleporter, Katja, and timer rows. |
| 21 | `instance-2149` Space Madness | `public_event_evidence` | DataMapping review | Split by creature bridge/objective/timer/Rowsdower rows. |
| 22 | `instance-3041` Ultimate Protogames Raid map | `instance_dependency` | client smoke | Validation-ready Downsizer/map-binding/client-smoke pass. |
| 23 | `instance-1336` Ruins of Kel Voreth | `public_event_evidence` | DataMapping review | Split by final-boss, trigger, and public-event objective rows. |

## Operating Rules

1. Scope one row, packet cluster, subsystem, spell family, quest chain, or
   instance flow per implementation pass.
2. Extract the exact tracker row, owning source files, focused tests, and known
   blocker before editing.
3. Use the evidence ladder consistently: `Observed`, `Correlated`, `Mapped`,
   `Verified`, `Implemented`, `Blocked`, `Rejected`.
4. Require producer, sender, apply owner, callback/table owner, live packet
   capture, runtime test, or manual smoke proof before mutating runtime state.
5. Keep runtime code away from `wildstar_client`, `jabbithole`, and `nf_map_*`.
   Promote reviewed data into runtime-owned SQL, scripts, or code-owned assets.
6. Update durable trackers in the same pass as implementation.
7. Never claim retail parity until automated tests and required client/manual
   smoke are both recorded.

## Phase 0 - Rebuild The Baseline

Run this before a serious closure campaign or after major tracker churn.

```powershell
dotnet restore Source\NexusForever.sln
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
python Tools\WikiArchiveAudit\content_retail_completeness_audit.py
python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py
```

Then snapshot:

- Current quick stats from `CURRENT_STATUS.md`.
- `content_retail_completeness_next_slice_queue.csv`.
- `content_retail_completeness_next_slice_blockers.csv`.
- Any changed top-gap rollups in `CONTENT_RETAIL_COMPLETENESS_TRACKER.md`.

Output of this phase is a work queue grouped by owner:

- Protocol/decompile.
- Runtime feature subsystem.
- DataMapping/runtime seed.
- Quest/public-event/path/contract content.
- Instance/dungeon/raid/event content.
- Manual live-client smoke.

## Phase 1 - Close The Five Consolidated Runtime Producer Gaps

These are high-leverage because they repeatedly block field names, packet
emitters, and status upgrades elsewhere.

| Gap | Evidence required | Implementation target |
| --- | --- | --- |
| F-025 entity-stat aux: `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E` | Per-opcode `vtable+0x58` apply handler, apply-table classification, or accepted sniff/order evidence | Add production emitters only for proven stat fields and timing; keep unproven fields neutral |
| F-025 map-tracked unit producer | Native send site or public-event marker capture for `0x0848` / `0x0849` | Emit update/disable from proven public-event/objective producers and proven `TrackingSlot` selection |
| F-004 housing neighborhood entry/list `0x0501` / `0x0506` | Live housing UI or realm-login sniff, or native server-push path | Implement real neighborhood list/entry production; remove placeholder assumptions |
| F-008 crafting current-craft and aux | Native producer path or live crafting capture for `0x084B`, `0x0854`, `0x0855`, `0x056C` | Implement mid-craft cadence, stats/quality, discovery or non-success behavior, and item microchip patch timing only where proven |
| F-031 Fortune retail weights and active rotation | Retail `ServerFortuneRewards` capture or storefront-server catalog dump | Replace emulator rarity-tier guesses with active rotation and per-item weights |

Suggested order:

1. Crafting current-craft and aux, because it has live harness support and clear
   UI actions.
2. Housing neighborhood list, because it is a contained session/producer
   problem.
3. Map-tracked unit, because it unblocks public-event visibility and objective
   tracking.
4. Entity-stat aux, because it needs the deepest native apply evidence.
5. Fortune weights, because it likely depends on external retail/storefront
   catalog evidence.

## Phase 2 - Focused Decompile And Packet Evidence Passes

Use cached fragments first. Refresh exports only when the cache is missing,
stale, or too shallow.

```powershell
.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 500
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
.\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
```

Priority packet clusters:

1. STS token auth: `/Auth/LoginTokenStart`, `/Auth/TokenKeyData`,
   `/Auth/RequestToken`, `/Auth/AssociateMyExternalAccount`.
2. Matching and raid: `ServerMatching0x05CF`, `0x0600`, `Client0x062A`,
   `Client0x0634`, standalone non-zero `0x0718`.
3. Housing/community aux and neighborhood list.
4. Crafting/tradeskill aux and current-craft packets.
5. Marketplace/reward/generic-map aux: `0x06DF`, `0x07CD`, `0x07D3`,
   `0x07D5`.
6. Spell aux: `0x07FC`, `0x080F`, `0x0810`, `0x0812`.
7. Realm transfer/PTR: `0x03EF`, `0x06EA`, destination/copy flow.
8. Client diagnostic opcodes with neutral names: only rename or mutate after a
   sender or consumer is proven.

Implementation gate for this phase:

- If the pass finds only registration/reader evidence, update trackers as
  `Mapped only / Blocked`.
- If it finds an apply owner or producer but fields are still unclear, add
  labels and packet tests, but keep runtime behavior diagnostic.
- If it reaches `Verified`, implement only the proven path and add focused
  tests.

## Phase 3 - Runtime Feature Closure By Subsystem

Work these as narrow vertical slices.

### Housing

Targets:

- Edit-mode ack/broadcast semantics.
- Neighborhood list producer trigger and fields.
- Remaining community field names.
- Decor ownership, unlock, and refund precision.

Proof required:

- Native producer or live housing UI capture.
- Focused residence/neighborhood tests.
- Client smoke for visit, edit, decor mutation, and reload behavior.

### Marketplace, Storefront, Rewards, CREDD

Targets:

- Marketplace aux `0x06DF` / `0x07D5` semantics.
- CREDD owned-order tails.
- Storefront VC/package request-confirm path.
- Coupon native sender for `0x0790`.
- Reward rotation `0x07CD` apply/flag/throttle semantics.
- Item/currency/property delivery after reward claim.

Proof required:

- Native consumer/producer or live packet capture.
- Marketplace/account/mail persistence tests.
- DB migration and rollback coverage for monetary state.

### Crafting, Tradeskill, Runes

Targets:

- Current-craft emit cadence.
- Discovery roll and unlock mutation.
- Non-success sigil result rules.
- Service-key names and profession modifier scaling.
- `0x056C` microchip patch timing.

Proof required:

- Live station capture with packet evidence.
- Native producer evidence for aux/current-craft packets.
- Focused tests for material debit, result packets, persistence, and failure
  branches.

### Transport, Taxi, Flight, Vehicles

Targets:

- Transport service-token bypass.
- Global route state.
- Taxi embark/completion.
- Passenger/seat/deployable vehicle semantics.
- `0x077E` producer semantics.

Proof required:

- Live route captures for known nodes.
- Spell evidence for cast context and debit.
- Tests for charge, cooldown, teleport, route rejection, and reload state.

### Group, Matching, Guild, ICComm

Targets:

- Replacement backfill and merge lifecycle.
- Non-zero raid queue semantics.
- Matching `0x05CF` and `0x0600` meanings.
- Guild bank economy, recruitment, boss tokens, perks, holomarks.
- ICComm entitlement checks and persistent channels.

Proof required:

- Two-client live captures for queue, ready, invite, guild bank, recruitment,
  and channel flows.
- Native sender/consumer evidence for diagnostic opcodes.
- Persistence tests for every durable state mutation.

### Spells, LAS, AMP, Attributes

Targets:

- One spell effect family per pass.
- Proc unsupported tails.
- `UpdateSpellInProgress` async transaction model.
- Attribute allocation/refund and bonus unlock edges.

Proof required:

- `!spell inspect4`, `!spell capture4`, `!spell capturenext`, proc reports, and
  live cast logs.
- Focused family tests before enabling runtime dispatch.
- Unknown `DataBits` remain diagnostic until field ownership is proven.

### Items, Unlocks, Costumes, Pets, Titles

Targets:

- Item-data aux producers.
- Repair durability-update timing.
- Supply-satchel aux.
- Unlock list deltas.
- Pet flair ownership/object/name validation.
- Pet lifecycle timing and scope.

Proof required:

- Native producer or accepted item/costume/pet UI capture.
- Packet shape tests plus manager persistence tests.

### Realm Transfer And PTR

Targets:

- Real transfer destinations.
- Success/handoff results.
- PTR queue/copy mutation.
- `0x03EF` payload semantics.

Proof required:

- Character-select packet sequence or native producer/apply path.
- Conservative handlers until destination/copy timing is proven.

### Challenges, Leaderboards, Fortune

Targets:

- Challenge `Client0x00C8`, share-init, reward UI, medal odds, timed scoring.
- Leaderboard season and medal filters.
- Fortune active rotation and per-item retail weights.

Proof required:

- Native request selector or live UI capture for each missing selector.
- Focused result/reward tests and persistence tests.

## Phase 4 - Content Retail Completeness Queue

Do not manually chase the entire generated inventory. Use the generated queue
and top-gap rollups.

Default order:

1. Work the current next-slice queue in rank order.
2. Split rows with `split_required_high_blocker_volume` into smaller world,
   public-event, objective, or boss-chain slices.
3. Promote rows with existing runtime tests but missing client smoke only after
   the smoke evidence is captured.
4. Resolve bridge blockers that unblock many downstream rows, but keep the
   reviewed bridge separate from the remaining retail-complete client-smoke
   gate.
5. Move to full-inventory families only after the generated queue no longer has
   an actionable row for that lane, or after the user explicitly asks for a
   targeted full-inventory campaign.
6. Move to lower-confidence WIP/GUESSED overlays only after authoritative proof.

Main content lanes:

| Lane | Current blocker pattern | Closure path |
| --- | --- | --- |
| Quest identity and objectives | Objective text/order, handler smoke, no-objective lifecycle | Validate Quest2/objective identity, add row-specific tests, run client smoke |
| World, zone, creature placement | Creature bridges, world triggers, zone visibility | Review DataMapping bridges, promote runtime spawns/scripts, verify placement and credit |
| Prerequisites and episode chains | Chain visibility, faction routing, missing episode bridge | Prove accept/complete sequencing and episode bridge behavior |
| Rewards, loot, achievements | Reward UI, loot cadence, achievement progression | Add grant/persistence tests, capture client presentation |
| Public events, challenges, paths, contracts | Event objective producers, path/contract bridge reviews | Prove objective producers, event flow, reward/scoreboard behavior |
| Instances, dungeons, raids | Encounter mechanics, portals, triggers, boss choreography | Work one world/event chain at a time with focused scripts and smoke |
| Script producers and cinematics | Presentation payloads, phase order, runtime actions | Tie source scripts to client rows and packet/UI behavior |

Per content slice checklist:

1. Extract IDs from `content_retail_completeness_next_slice_queue.csv`.
2. Read blocker rows from `content_retail_completeness_next_slice_blockers.csv`.
3. Resolve DataMapping bridge or script ownership first.
4. Add or update runtime scripts only for proven rows.
5. Add focused xUnit coverage for credit, negative cases, replay, cleanup, and
   persistence.
6. Run content tracker generator and validator.
7. Run live-client smoke when UI, timing, combat choreography, cinematic,
   reward presentation, or route behavior cannot be automated.
8. Update tracker status.

## Phase 5 - DataMapping And Runtime Seed Promotion

Use the authoring workflow only when a row depends on reference tables or
reviewed DataMapping output.

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -EnableDataMappingAuthoring -PromptForRootPassword
python Tools\DataMapping\map_wildstar_data.py --include-spline-candidates
python Tools\DataMapping\load_mapping_staging_tables.py --apply
```

Then apply and verify safe runtime rows:

```powershell
Get-Content -Raw Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world

Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

Promotion rules:

- Use only `unique_name`, `scored_name`, or reviewed bridge rows for runtime
  imports.
- Use relation-scoped overrides when a global creature bridge is unsafe.
- Keep spline candidates audit-only until movement proof exists.
- Keep LaughingWS WIP/GUESSED rows WIP until placement, lifecycle, and client
  smoke are proven.
- Do not absorb deterministic LaughingWS overlay ranges into the primary
  runtime seed.

## Phase 6 - Live Evidence Harness Campaign

Use harness presets instead of ad hoc local runs.

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -DungeonSmoke `
  -BundleName "targeted-dungeon-smoke" `
  -ClientDirectory "I:\WildStar" `
  -PromptForRootPassword
```

Useful presets:

- `-FortuneRewardsSmoke`
- `-PublicEventVoteScoreboardSmoke`
- `-PublicEventObjectiveNotificationSmoke`
- `-PvpAdventureSmoke`
- `-ExpeditionSmoke`
- `-DungeonSmoke`
- `-RaidEventSmoke`
- `-QuestVirtualLootSmoke`
- `-SkyplotHousingSmoke`
- `-LiveEventSmoke`

Direct restart fallback:

```powershell
.\Tools\Setup\Restart-NexusForeverLocal.ps1 `
  -ClientDirectory "I:\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -LogLevel Trace `
  -PromptForRootPassword
```

For every bundle, collect:

- `manifest.json`
- command transcript
- server log notes
- client observation notes
- screenshots or video when UI behavior matters
- negative-case notes
- client `Logs\*.txt`
- client `Errors\WildStar64*.log` if decode or crash behavior appears

Promote only summarized evidence into tracked docs.

## Phase 7 - Verification Gates

For C# runtime behavior:

```powershell
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

For script or instance work:

```powershell
dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj --no-restore -v minimal --nologo
dotnet build Source\NexusForever.Script.Instance\NexusForever.Script.Instance.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

For broad source, packet, database, or multi-service changes:

```powershell
dotnet build Source\NexusForever.sln -v minimal --nologo
```

For decompile mapping artifacts:

```powershell
.\Decomp\Analysis\Validate-DecompileMappingArtifacts.ps1
.\Decomp\Analysis\Get-DecompileMappingArtifactGaps.ps1
```

For content-retail tracker changes:

```powershell
python Tools\WikiArchiveAudit\content_retail_completeness_audit.py
python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py
```

## Final Retail-Parity Closure Criteria

A feature or row can move to retail-complete only when all of these are true:

1. The packet/data/runtime semantics are proven or irrelevant.
2. Runtime implementation exists in the owning subsystem.
3. Focused automated tests pass.
4. DataMapping or SQL imports are verified when data-backed rows changed.
5. Live/manual smoke is recorded for behavior automation cannot prove.
6. Generated trackers no longer mark the row `not_retail_complete`.
7. Remaining edge cases are explicitly `Rejected` or `Blocked` with named
   evidence sources.
8. `CURRENT_STATUS.md` and focused trackers agree on the state.

## Suggested Execution Order

1. Rebuild the baseline and lock the generated queue.
2. Process rank `1` through `12` quest rows as validation-ready rows: focused
   xUnit first, then DataMapping bridge review or client smoke as named by the
   row.
3. Process validation-ready `instance-3041` when the queue reaches it, because
   it is a bounded client-smoke row rather than a split-required instance.
4. For split-required instances, open one objective, public event, boss,
   room, route, or timer sub-slice at a time. Do not widen to the full instance.
5. Run a DataMapping bridge campaign for families that repeatedly appear in the
   queue: quest creatures, quest zones, public-event creatures, public-event
   objectives, and instance dependencies.
6. Run a client-smoke campaign for rows already covered by focused tests:
   quest dialog/objective/reward/achievement, instance routing/portal/door,
   public-event objective UI, reward presentation, and replay/cleanup.
7. Run a runtime-test campaign for producer-pattern rows:
   `script_objective_producer`, `script_runtime_action`, event flow, timers,
   trigger volumes, activate/checklist paths, and reward side effects.
8. In parallel only when a row demands it, close the five consolidated runtime
   producer gaps: crafting current-craft/aux, housing neighborhood list,
   map-tracked unit producer, entity-stat aux, and Fortune weights.
9. Continue spell, creature, path, challenge, contract, and script families one
   fixture or bridge family at a time, feeding improved generator classifications
   back into the queue.
10. Finish marketplace/storefront/reward, matching/raid, guild/ICComm, transport,
    and diagnostic packet semantics only when native producer/consumer evidence
    or accepted live packet captures prove fields and timing.
