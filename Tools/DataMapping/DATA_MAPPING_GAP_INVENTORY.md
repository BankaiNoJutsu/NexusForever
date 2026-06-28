# DataMapping Gap Inventory

Generated from local repo, `jabbithole`, `wildstar_client`, and
`nexus_forever_world` evidence on 2026-06-18.

This is the working inventory for missing or incomplete DataMapping-backed
content and mechanics. It intentionally separates these states:

- `mapped`: source/reference rows are represented in CSVs or typed client tables.
- `reviewed`: a bridge or row has been manually or spatially adjudicated.
- `promoted`: reviewed data has been imported into runtime-owned tables or
  scripts.
- `verified`: automated tests or client/runtime smoke prove the row behavior.
- `retail_complete`: not currently claimed by any generated content inventory.

Runtime code must not query `wildstar_client`, `jabbithole`, or `nf_map_*`.
Every item below must end as reviewed SQL, typed game-table use, script-owned
constants, or a tracked blocker.

## Local Evidence Snapshot

Reference databases are available locally.

| Source | Evidence |
| --- | ---: |
| DataMapping split-file coverage | 493/493 mapped, 0 partial, 0 unmapped |
| `wildstar_client.Quest2` | 5,194 rows |
| `wildstar_client.QuestObjective` | 10,031 rows |
| `wildstar_client.PublicEvent` | 642 rows |
| `wildstar_client.PublicEventObjective` | 3,703 rows |
| `wildstar_client.PathMission` | 1,064 rows |
| `wildstar_client.Challenge` | 643 rows |
| `wildstar_client.Achievement` | 4,943 rows |
| `wildstar_client.Creature2` | 53,137 rows |
| `wildstar_client.Item2` | 71,918 rows |
| `wildstar_client.Spell4` / `Spell4Effects` | 66,383 / 131,010 rows |
| `jabbithole.quests` | 2,992 rows |
| `jabbithole.creatures` | 24,679 rows |
| `jabbithole.coordinates` | 1,989,100 rows |
| `jabbithole.versioned_item_drops` | 522,130 rows |
| `jabbithole.vendor_items` | 20,650 rows |

Promoted runtime rows observed by `verify_safe_world_imports.sql`:

| Runtime surface | Rows |
| --- | ---: |
| `entity_vendor` / `entity_vendor_item` | 192 / 7,961 |
| DataMapping-owned mapped `entity` spawns | 1,312 |
| DataMapping-owned mapped `entity_stats` | 4,562 |
| `creature_loot` | 198,735 |
| mapped creature loot groups / loot items | 3,795 / 198,735 |
| mapped item-container groups / loot items | 272 / 23,865 |
| exact plus fallback `item_salvage` | 337,550 + 1,886 |

## P0 Operational Gaps

| Id | Gap | Evidence | Next action |
| --- | --- | --- | --- |
| DM-OPS-001 | Runtime world DB contained authoring `nf_map_*` tables. Clean runtime deployments should have zero. | **Locally reconciled 2026-06-17.** Rebuilt authoring staging into `nexus_forever_mapping`, confirmed runtime source has no `nf_map_*` consumers, then removed runtime `nf_map_*` residue. `verify_safe_world_imports.sql` now reports `runtime_world_nf_map_tables = 0` and `expected_runtime_world_nf_map_tables_0_mismatch = 0`. | Keep runtime setup free of `nf_map_*`; use `nexus_forever_mapping` for authoring staging only. |
| DM-OPS-002 | Local small-world WIP overlay was incomplete. | **Locally reconciled 2026-06-17.** Reapplied `Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql`; verifier now reports `16` small-world WIP entities, `59` stats, `7` Northern Wilds WIP rows, and `3` Q3673 signal flares with all related mismatch rows at `0`. | Operational drift is closed locally; rows remain WIP/GUESSED pending row-specific quest/client smoke. |
| DM-OPS-003 | Staging rows in runtime were stale relative to current CSV output. | **Locally reconciled 2026-06-17.** Runtime staging residue was removed after loading current staging into `nexus_forever_mapping`. The loader applied `96/96` authoring tables with zero missing CSV or required-column tables; `nf_map_creature` loaded `24,679` rows and `nf_map_creature_spline_candidate` loaded `188,480` rows. Older optional spline CSVs now load by omitting nullable/defaulted expanded columns instead of blocking staging. | Keep staging in `nexus_forever_mapping`; regenerate spline candidates with `--include-spline-candidates` before relying on expanded spline classification/runtime columns; do not restore `nf_map_*` into `nexus_forever_world`. |

## Foundational Mapping Gaps

| Id | Gap | Current evidence | Impact | Next action |
| --- | --- | --- | --- | --- |
| DM-FND-001 | Creature bridge uncertainty remains the largest downstream blocker. | `creature_map.csv`: 13,839 `unique_name`, 2,780 `scored_name`, 1,026 `reviewed`, 6,057 `ambiguous_name`, 977 `unmatched`. | Affects spawns, vendors, loot, creature spells, quests, public events, path missions, contracts, and challenges. | Continue review queue by downstream impact; promote only audited overrides to `creature_bridge_overrides.csv` or row-guarded relation overrides when the source creature is context-dependent. |
| DM-FND-002 | World spawn candidates are mostly mapped-only. | 732,694 spawn candidates; only 1,312 mapped entity spawns promoted locally. Rotation is absent in Jabbithole and defaults to zero. | Open-world quest NPCs, PE objectives, path missions, vendors, and target groups can be data-known but not spawned/verified. | Work world/area slices, not global imports; prove duplicate radius, area, spawn density, and quest credit before promotion. |
| DM-FND-003 | Northern Wilds raw coordinate import is unsafe by default. | Verifier reports 0 unsafe DataMapping spawns and 738 legacy/manual rows; docs record overdense raw coordinate imports. | Starter-zone bulk spawn import can regress local gameplay. | Keep bulk import blocked; promote only row-reviewed quest/objective placements. |
| DM-FND-004 | Quest objective bridge false-unmatched slot-0 gap is closed; residual objective evidence needs review/smoke. | `quest_objective_map.csv`: `4,488` `matched`, `70` `unmatched`. Content-retail objective evidence splits the matched rows into `4,465` `matched` plus `23` `reviewed_match`, with the same `70` residual unmatched rows. | Former order-0 false blockers now map to current `QuestObjective` rows; residual rows are non-current/unmatched or matched evidence still needing text/order/runtime trigger/client smoke. | Review the remaining `70` unmatched/non-current rows and smoke matched next-slice objective blockers before claiming row-specific objective parity. |
| DM-FND-005 | Quest zone bridge is often partial. | `quest_zone_map.csv`: 6,167 `matched`, 5,079 `partial`, 39 `unmatched`. | Map guidance, visibility, call-zone routing, and quest availability need review. | Audit zone mismatches by starter-zone and active quest chain; smoke path arrow/visibility behavior. |
| DM-FND-006 | Spline mapping is candidate-only. | 188,480 spline candidate rows; no direct source creature-to-spline foreign key. | Patrols and movement loops remain visually/static incomplete. | Review movement one row at a time with client/runtime smoke before any `entity_spline` promotion. |
| DM-FND-007 | Generic `client_source_*` exports are complete but semantic-light. | 272 generic client-source maps preserve every unmapped client table. | Polymorphic `objectId*`, flags, formulas, visuals, sound, tutorials, random text, and UI rows cannot be imported safely from raw labels. | Promote table families into curated maps only after owner semantics and conflict policy are known. |

### 2026-06-17 DM-FND next-slice review note

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2018` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28095` Smoldering Bonebeast -> Creature2 `69169`. This was not promoted
  from scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 64 world `2149` / area `2421` coordinate rows, source
  spell evidence `2777` / `2809` / `3327` / `5917`, client Spell4 evidence
  `32734` / `32735` / `34262` / `35556`, and objective `2115` TargetGroup
  `12684` nesting TargetGroup `12683` nesting TargetGroup `12681` containing
  Creature2 `69169`. Normal TargetGroup `12676` contains Creature2 `48352`,
  but normal source creature `9534` / relation `2254` remains `ambiguous_name`
  because ranked candidates tie Creature2 `48352` and `69169` without row-level
  proof. Generated DataMapping now reports `creature_public_event_map.csv`
  relation `2018` as reviewed while relation `2254` remains ambiguous; the
  modular content-retail audit selective path (`--only public-event-evidence,next-slice`)
  refreshed only the needed public-event evidence, next-slice queue, and
  next-slice blocker CSVs; PE390 totals are `57` reviewed, `12`
  ambiguous-name, and `53` scored-name. Focused verification passed:
  content-retail audit tests `102/102`, content-retail validator tests `34/34`,
  and generated content-retail validation `31` files / `168101` rows.
  Evidence bundle:
  `artifacts/blocker_evidence/20260618-212430-instance-2149-space-madness-smoldering-bonebeast-prime-bridge-review`.
  This closes only the level-50 bridge blocker; runtime spawn/import,
  objective ownership, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and client smoke remain blocked pending live
  build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2015` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28088` Crazed Handler -> Creature2 `69143`. This was not promoted from
  ambiguous-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 109 world `2149` / area `2421` coordinate rows, source
  spell evidence `3020` / `3305` / `2730` / `2497`, client Spell4 evidence
  `5649` / `5652` / `34552` / `52448`, and objective `2115` TargetGroup
  `12684` nesting TargetGroup `12683` nesting TargetGroup `12677` containing
  Creature2 `69143`. Normal TargetGroup `12672` contains Creature2 `46671`,
  but normal source creature `3631` / relation `2253` remains `ambiguous_name`
  for a separate review slice. Generated DataMapping now reports
  `creature_public_event_map.csv` relation `2015` as reviewed while relation
  `2253` remains ambiguous; PE390 totals are `56` reviewed, `12`
  ambiguous-name, and `54` scored-name. Evidence bundle:
  `artifacts/blocker_evidence/20260618-205113-instance-2149-space-madness-crazed-handler-prime-bridge-review`.
  This closes only the level-50 bridge blocker; runtime spawn/import,
  objective ownership, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and client smoke remain blocked pending live
  build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2013` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28076` Ravenous Nightmare -> Creature2 `69220`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 101 world `2149` / area `2414` coordinate rows, source
  spell evidence `3015` / `3073` / `2276` / `6098` / `5893`, client Spell4
  evidence `28936` / `28937` / `34264` / `36414` / `43759`, and
  already-reviewed normal source creature `446` / relation `26` -> Creature2
  `46133`. Objective `1590` uses reward-pane TargetGroup `6402`, which nests
  TargetGroup `6401`; TargetGroup `6401` contains normal Creature2 `46133`,
  and `SavePanickedWorkersNightmareEntityScript` filters the normal row. No
  TargetGroup row currently lists Creature2 `69220`, and no current PE390
  objective row owns Creature2 `69220`. The generated
  `creature_public_event_map.csv` now reports relation `2013` as `reviewed`;
  broad `creature_public_event_map.csv` totals after this review are `87`
  reviewed, `621` ambiguous-name, `917` scored-name, `921` unique-name, and
  `99` unmatched rows. PE390 totals are `53` reviewed, `13` ambiguous-name,
  and `56` scored-name. `creature_map.csv` totals after this review are
  `1,022` reviewed, `2,783` scored-name, `13,839` unique-name, `6,058`
  ambiguous-name, and `977` unmatched rows. This closes only the Ravenous
  Nightmare Prime bridge blocker; runtime spawn/import, objective ownership,
  objective credit, phase timing, rewards, medals, achievements, replay
  cleanup, and full Space Madness Prime client smoke remain blocked pending
  live build 16042 evidence. Evidence bundle:
  `artifacts/blocker_evidence/20260618-191413-instance-2149-space-madness-ravenous-nightmare-prime-bridge-review`.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2012` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28070` Decrepit Atrocity -> Creature2 `69170`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 66 world `2149` / area `2421` coordinate rows, source
  spell evidence `6096` / `6099` / `2611` / `6091` / `3300`, client Spell4
  evidence `4040` / `4041` / `54822` / `54823` / `54923`, and TargetGroup
  `12681` listing Creature2 `69170` with Prime rows `69168` and `69169`.
  No current PE390 objective row uses TargetGroup `12681`. Same-name normal
  source creature `9535` / relation `2249` remains `ambiguous_name` because
  exact-name ranked candidates Creature2 `48353` and Creature2 `69170` tie and
  no row-level objective proof distinguishes them. The generated
  `creature_public_event_map.csv` now reports relation `2012` as `reviewed`;
  broad `creature_public_event_map.csv` totals after this review are `86`
  reviewed, `621` ambiguous-name, `918` scored-name, `921` unique-name, and
  `99` unmatched rows. PE390 totals are `52` reviewed, `13` ambiguous-name,
  and `57` scored-name. `creature_map.csv` totals after this review are
  `1,021` reviewed, `2,784` scored-name, `13,839` unique-name, `6,058`
  ambiguous-name, and `977` unmatched rows. This closes only the Decrepit
  Atrocity Prime bridge blocker; runtime spawn/import, objective ownership,
  objective credit, phase timing, rewards, medals, achievements, replay
  cleanup, and full Space Madness Prime client smoke remain blocked pending
  live build 16042 evidence. Evidence bundle:
  `artifacts/blocker_evidence/20260618-185445-instance-2149-space-madness-decrepit-atrocity-prime-bridge-review`.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2011` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28066` Raging Oxianbull -> Creature2 `69172`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 95 world `2149` / area `2419` coordinate rows, source
  spell evidence `2631` / `5873` / `5875` / `6437`, client Spell4 evidence
  `32761` / `32762` / `53434` / `53435`, and already-reviewed normal source
  creature `3679` / relation `499` -> Creature2 `45728`. TargetGroup `6363`
  lists normal Creature2 `45728` with livestock-family rows, but no current
  PE390 objective row uses TargetGroup `6363`. No TargetGroup row currently
  lists Creature2 `69172`, and no current PE390 objective row owns Creature2
  `69172`. The generated `creature_public_event_map.csv` now reports relation
  `2011` as `reviewed`; broad `creature_public_event_map.csv` totals after
  this review are `85` reviewed, `621` ambiguous-name, `919` scored-name,
  `921` unique-name, and `99` unmatched rows. PE390 totals are `51` reviewed,
  `13` ambiguous-name, and `58` scored-name. `creature_map.csv` totals after
  this review are `1,020` reviewed, `2,785` scored-name, `13,839`
  unique-name, `6,058` ambiguous-name, and `977` unmatched rows. This closes
  only the Raging Oxianbull Prime bridge blocker; runtime spawn/import,
  objective ownership, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and full Space Madness Prime client smoke
  remain blocked pending live build 16042 evidence. Evidence bundle:
  `artifacts/blocker_evidence/20260618-183359-instance-2149-space-madness-raging-oxianbull-prime-bridge-review`.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2010` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28056` Foaming Roandoe -> Creature2 `69171`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 104 world `2149` / area `2419` coordinate rows, spell
  evidence `6382` / `6422` / `6423`, and already-reviewed normal source
  creature `3684` / relation `159` -> Creature2 `45727`. TargetGroup `6363`
  lists normal Creature2 `45727` with livestock-family rows, but no current
  PE390 objective row uses TargetGroup `6363`. No TargetGroup row currently
  lists Creature2 `69171`, and no current PE390 objective row owns Creature2
  `69171`. The generated `creature_public_event_map.csv` now reports relation
  `2010` as `reviewed`; broad `creature_public_event_map.csv` totals after
  this review are `84` reviewed, `621` ambiguous-name, `920` scored-name,
  `921` unique-name, and `99` unmatched rows. PE390 totals are `50` reviewed,
  `13` ambiguous-name, and `59` scored-name. The generated `creature_map.csv`
  totals after this review are `1,019` reviewed, `2,786` scored-name, `13,839`
  unique-name, `6,058` ambiguous-name, and `977` unmatched rows. This closes
  only the Prime bridge blocker; relation `2010` spawn/import placement,
  Creature2 `69171` objective ownership/usage, objective credit, phase timing,
  rewards, medals, achievements, replay cleanup, and full Space Madness Prime
  client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2009` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28046` Writhing Nightmare -> Creature2 `69213`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 65 world `2149` / area `2413` coordinate rows, spell
  evidence `2745` / `2762` / `8142` / `10856` / `10859`, and already-reviewed
  normal source creature `392` / relation `25` -> Creature2 `46123`.
  Objective `1590` uses reward-pane TargetGroup `6402`, which nests
  TargetGroup `6400`; TargetGroup `6400` contains normal Creature2 `46123`,
  and `SavePanickedWorkersNightmareEntityScript` filters the normal row. No
  TargetGroup row currently lists Creature2 `69213`, and no current PE390
  objective row owns Creature2 `69213`. The generated
  `creature_public_event_map.csv` now reports relation `2009` as `reviewed`;
  broad `creature_public_event_map.csv` totals after this review are `83`
  reviewed, `621` ambiguous-name, `921` scored-name, `921` unique-name, and
  `99` unmatched rows. PE390 totals are `49` reviewed, `13` ambiguous-name,
  and `60` scored-name. The generated `creature_map.csv` totals after this
  review are `1,018` reviewed, `2,787` scored-name, `13,839` unique-name,
  `6,058` ambiguous-name, and `977` unmatched rows. This closes only the Prime
  bridge blocker; relation `2009` spawn/import placement, Creature2 `69213`
  objective ownership/usage, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and full Space Madness Prime client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relations `2008` and `2246` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28040` Dark Creeper -> Creature2 `69163` and normal level-32 source
  creature `9512` Dark Creeper -> Creature2 `48345`. This was not promoted
  from scored-name matching alone: both rows have exact source/client level
  matches, faction `218`, action set `2348`, display group `24216`, display
  `21629`, 56 and 95 world `2149` / area `2421` coordinate rows, shared Dark
  Creeper spell-family evidence, and TargetGroups `12680` / `12675`. No
  current PE390 objective row uses TargetGroup `12680`, TargetGroup `12675`,
  Creature2 `69163`, or Creature2 `48345` directly. The generated
  `creature_public_event_map.csv` now reports relations `2008` and `2246` as
  `reviewed`; broad `creature_public_event_map.csv` totals after this review
  are `82` reviewed, `621` ambiguous-name, `922` scored-name, `921`
  unique-name, and `99` unmatched rows. PE390 totals are `48` reviewed, `13`
  ambiguous-name, and `61` scored-name. The generated `creature_map.csv` totals
  after this review are `1,017` reviewed, `2,788` scored-name, `13,839`
  unique-name, `6,058` ambiguous-name, and `977` unmatched rows. This closes
  only the Dark Creeper bridge blocker; runtime spawn/import, objective
  ownership/usage, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge blocker: did not promote Space
  Madness public-event creature relations `2007` or `2422`. Generated
  `creature_public_event_map.csv` maps source creatures `28037` and `863`
  (`Panicked Worker`) to Creature2 `46062` as `ambiguous_name`, but ranked
  bridge candidates tie both source rows between exact-name Creature2 rows
  `46062` and `46063` with score `140`, faction match, level overlap, no
  exact-level match, difficulty mismatch, and no spatial/entity match to break
  the tie. Source creature `28037` has `21` world `2149` / area `2416`
  coordinate rows and source creature `863` has `24`, but no Panicked Worker
  spell rows exist, no TargetGroup row lists Creature2 `46062` or `46063`, and
  no current PE390 objective row uses either worker row as objective object or
  reward-pane TargetGroup. Objective `1590` remains mapped through reward-pane
  TargetGroup `6402` to the nightmare target-group set already covered by
  `SavePanickedWorkersNightmareEntityScript`. Evidence bundle:
  `artifacts/blocker_evidence/20260618-172641-instance-2149-space-madness-panicked-worker-bridge-blocker`.
  No override, mapper regeneration, safe-import SQL, or runtime behavior was
  changed. Relations `2007` and `2422` remain blocked pending build 16042
  live-client/server smoke or retail packet/log capture for worker spawn
  identity and objective ownership, decompile/client table evidence
  distinguishing Creature2 `46062` from `46063`, or authoritative row-level
  target-group/objective evidence linking one worker row to PE390.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2006` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28035` Rending Nightmare -> Creature2 `69218`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 100 world `2149` / area `2414` coordinate rows,
  Lingering Venom/Swipe/Rend/Caustic Leap spell evidence, and already-reviewed
  normal source creature `1121` / relation `29` -> Creature2 `46130`.
  Objective `1590` uses reward-pane TargetGroup `6402`, which nests
  TargetGroup `6400`; TargetGroup `6400` contains normal Creature2 `46130`,
  and `SavePanickedWorkersNightmareEntityScript` filters the normal row. No
  TargetGroup row currently lists Creature2 `69218`, and no current PE390
  objective row owns Creature2 `69218`. The generated
  `creature_public_event_map.csv` now reports relation `2006` as `reviewed`;
  broad `creature_public_event_map.csv` totals after this review are `80`
  reviewed, `621` ambiguous-name, `924` scored-name, `921` unique-name, and
  `99` unmatched rows. The generated `creature_map.csv` totals after this
  review are `1,015` reviewed, `2,790` scored-name, `13,839` unique-name,
  `6,058` ambiguous-name, and `977` unmatched rows. This closes only the Prime
  bridge blocker; relation `2006` spawn/import placement, Creature2 `69218`
  objective ownership/usage, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and full Space Madness Prime client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2005` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28023` Eye Nightmare -> Creature2 `69209`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 61 world `2149` / area `2416` coordinate rows,
  Punch/Jab/Flourishing Combo/Haymaker spell evidence, and already-reviewed
  normal source creature `3663` / relation `154` -> Creature2 `46723`.
  TargetGroup `6449` covers only normal Nightmare-family members `46722`,
  `46723`, and `46724`, no current PE390 objective row uses TargetGroup
  `6449`, objective `1590` uses reward-pane TargetGroup `6402`, and
  `SavePanickedWorkersNightmareEntityScript` remains scoped to nested
  TargetGroups `6400` / `6401` rows. No TargetGroup row currently lists
  Creature2 `69209`, and no current PE390 objective row owns Creature2
  `69209`. The generated `creature_public_event_map.csv` now reports relation
  `2005` as `reviewed`; broad `creature_public_event_map.csv` totals after
  this review are `79` reviewed, `621` ambiguous-name, `925` scored-name,
  `921` unique-name, and `99` unmatched rows. The generated `creature_map.csv`
  totals after this review are `1,014` reviewed, `2,791` scored-name, `13,839`
  unique-name, `6,058` ambiguous-name, and `977` unmatched rows. This closes
  only the Prime bridge blocker; relation `2005` spawn/import placement,
  Creature2 `69209` objective ownership/usage, objective credit, phase timing,
  rewards, medals, achievements, replay cleanup, and full Space Madness Prime
  client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2004` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28020` Blazing Crewman -> Creature2 `69205`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 78 world `2149` / area `2416` coordinate rows,
  Arcane Bolt/Firestorm/Erupting Fissure spell evidence, and already-reviewed
  normal source creature `3642` / relation `148` -> Creature2 `46714`.
  TargetGroup `6447`, objective `1662`, and
  `HallucinatingLivestockEntityScript` cover only normal Creature2 `46714`; no
  TargetGroup row currently lists Creature2 `69205`, and no current PE390
  objective row owns Creature2 `69205`. The generated
  `creature_public_event_map.csv` now reports relation `2004` as `reviewed`;
  broad `creature_public_event_map.csv` totals after this review are `78`
  reviewed, `621` ambiguous-name, `926` scored-name, `921` unique-name, and
  `99` unmatched rows. The generated `creature_map.csv` totals after this
  review are `1,013` reviewed, `2,792` scored-name, `13,839` unique-name,
  `6,058` ambiguous-name, and `977` unmatched rows. This closes only the Prime
  bridge blocker; relation `2004` spawn/import placement, Creature2 `69205`
  objective ownership/usage, objective credit, phase timing, rewards, medals,
  achievements, replay cleanup, and full Space Madness Prime client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2003` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28018` Violent Oxiancow -> Creature2 `69139`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 101 world `2149` / area `2419` coordinate rows,
  Ram/Bash/Bucking Frenzy/Overwhelming Bellow spell evidence, and TargetGroup
  `12677` grouping Creature2 `69139` with other Prime livestock and scorched
  rows. No current PE390 objective row uses TargetGroup `12677`; normal source
  creature `3686` / relation `422` remains reviewed to Creature2 `45729`. The
  generated `creature_public_event_map.csv` now reports relation `2003` as
  `reviewed`; broad `creature_public_event_map.csv` totals after this review
  are `77` reviewed, `621` ambiguous-name, `927` scored-name, `921`
  unique-name, and `99` unmatched rows. The generated `creature_map.csv` totals
  after this review are `1,012` reviewed, `2,793` scored-name, `13,839`
  unique-name, `6,058` ambiguous-name, and `977` unmatched rows. This closes
  only the Prime bridge blocker; relation `2003` spawn/import placement,
  TargetGroup `12677` objective ownership/usage, objective credit, phase timing,
  rewards, medals, achievements, replay cleanup, and full Space Madness Prime
  client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2002` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: level-50 source creature
  `28006` Smoldering Billow -> Creature2 `69168`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50
  source/client mapping, 93 world `2149` / area `2421` coordinate rows,
  Dice/Slashing Strikes/Slice/Feet of Fury spell evidence, and TargetGroup
  `12681` grouping Creature2 `69168` with `69169` and `69170`. No current
  PE390 objective row uses TargetGroup `12681`; lower source creature `9513` /
  relation `2247` remains ambiguous-name to Creature2 `48351` and was not
  promoted. The generated `creature_public_event_map.csv` now reports relation
  `2002` as `reviewed`; broad `creature_public_event_map.csv` totals after
  this review are `76` reviewed, `621` ambiguous-name, `928` scored-name, `921`
  unique-name, and `99` unmatched rows. The generated `creature_map.csv` totals
  after this review are `1,011` reviewed, `2,794` scored-name, `13,839`
  unique-name, `6,058` ambiguous-name, and `977` unmatched rows. This closes
  only the Prime bridge blocker; relation `2002` spawn/import placement,
  TargetGroup `12681` objective ownership/usage, objective credit, phase timing,
  rewards, medals, achievements, replay cleanup, and full Space Madness Prime
  client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2120` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: duplicate source creature
  `28252` Major Lee Barmy -> Creature2 `45812`. This was not promoted from
  unique-name matching alone: the source row is candidate-count-1, has a world
  `2149` / area `2482` coordinate row, and matches the already-reviewed
  companion source creature `332` / relation `23` -> Creature2 `45812`. PE390
  objective `1588` uses TargetGroup `6358`, TargetGroup `6358` has sole member
  `45812`, `SpaceMadnessEventScript` spawns reviewed talk NPC entity
  `1100300006` with Creature2 `45812`, and `MajorLeeBarmyEntityScript` filters
  and credits Creature2 `45812` / TargetGroup `6358`. The generated
  `creature_public_event_map.csv` now reports relation `2120` as `reviewed`;
  broad `creature_public_event_map.csv` totals after this review are `75`
  reviewed, `621` ambiguous-name, `929` scored-name, `921` unique-name, and
  `99` unmatched rows. The generated `creature_map.csv` totals after this
  review are `1,010` reviewed, `2,795` scored-name, `13,839` unique-name,
  `6,058` ambiguous-name, and `977` unmatched rows. This closes only the
  duplicate bridge blocker; relation `2120` spawn/import placement, objective
  `1588` UI timing and talk credit, phase timing, rewards, medals,
  achievements, replay cleanup, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relations `2350` and `2025` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creatures `3704`
  and `28120` Skittish Rowsdower -> Creature2 `45723`. This was not promoted
  from unique-name matching alone: both source rows are candidate-count-1,
  level-1 world `2149` / area `2419` mappings to the same Creature2 row; source
  creature `3704` has `96` coordinate rows, source creature `28120` has `100`
  coordinate rows, and source creature `28120` has Plague Burst spell evidence.
  Objective `4708` remains volatile Rowsdower avoidance with objective type
  `9`, WorldLocation2 `37709`, objective object `0`, and target group `0`, so
  the objective owner, explosion/failure producer, spawn/import placement,
  objective-credit behavior, rewards, medals, achievements, replay cleanup, and
  full Space Madness client smoke remain blocked pending live build 16042
  evidence. The generated `creature_public_event_map.csv` now reports
  relations `2350` and `2025` as `reviewed`; `creature_public_event_map.csv`
  totals after this review were `74` reviewed, `621` ambiguous, `929`
  scored-name, `922` unique-name, and `99` unmatched rows. The generated
  `creature_map.csv` totals after this review were `1,009` reviewed, `2,795`
  scored-name, `13,840` unique-name, `6,058` ambiguous-name, and `977`
  unmatched rows.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `150` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `1039`
  Enraged Lamp -> Creature2 `45866`. This was not promoted from
  ambiguous-name matching alone: PE390 relation `150`, 82 local world `2149` /
  area `2416` source coordinate rows, and Jab/Punch/Flourishing
  Combo/Haymaker spell rows support the normal level-32 bridge. TargetGroup
  `6343` lists Creature2 `45866` with hallucination-trio rows `45879` and
  `45849`, while tied level-32 candidate Creature2 `46724` belongs to
  TargetGroup `6449`; no current PE390 objective row in the generated
  objective map uses TargetGroup `6343` or `6449`. The same-name level-50
  source creature `28168` / relation `2036` remains ambiguous-name to
  Creature2 `69185` and was not promoted. The generated
  `creature_public_event_map.csv` now reports relation `150` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `72` reviewed,
  `621` ambiguous, `929` scored-name, `924` unique-name, and `99` unmatched
  rows. The generated `creature_map.csv` totals after this review were `1,007`
  reviewed, `2,795` scored-name, `13,842` unique-name, `6,058`
  ambiguous-name, and `977` unmatched rows. The review closes only the normal
  bridge blocker; Creature2 `45866` objective ownership, spawn/import
  placement, objective-credit behavior, combat behavior, rewards, medals,
  achievements, replay cleanup, Prime/level-50 Enraged Lamp behavior, and full
  Space Madness client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `499` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3679`
  Raging Oxianbull -> Creature2 `45728`. This was not promoted from
  scored-name matching alone: PE390 relation `499`, 81 local world `2149` /
  area `2419` source coordinate rows, and Bash/Ram/Bucking Frenzy/Overwhelming
  Bellow spell rows support the normal level-32 bridge. TargetGroup `6363`
  lists Creature2 `45728` with livestock-family rows, but no current PE390
  objective row in the generated objective map uses TargetGroup `6363`;
  `HallucinatingLivestockEntityScript` remains scoped to Creature2 `46483` and
  `46714`. The same-name level-50 source creature `28066` / relation `2011`
  remains mapped-only to Creature2 `69172` and was not promoted. The generated
  `creature_public_event_map.csv` now reports relation `499` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `71` reviewed,
  `622` ambiguous, `929` scored-name, `924` unique-name, and `99` unmatched
  rows. The generated `creature_map.csv` totals after this review were `1,006`
  reviewed, `2,795` scored-name, `13,842` unique-name, `6,059`
  ambiguous-name, and `977` unmatched rows. The review closes only the normal
  bridge blocker; Creature2 `45728` objective ownership, spawn/import
  placement, objective-credit behavior, combat behavior, rewards, medals,
  achievements, replay cleanup, Prime/level-50 Raging Oxianbull behavior, and
  full Space Madness client smoke remain blocked pending live build 16042
  evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `422` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3686`
  Violent Oxiancow -> Creature2 `45729`. This was not promoted from
  scored-name matching alone: PE390 relation `422`, 91 local world `2149` /
  area `2421` source coordinate rows, and Bash/Ram/Bucking Frenzy/Overwhelming
  Bellow spell rows support the normal level-32 bridge. TargetGroups `6363`
  and `12672` list Creature2 `45729`, and TargetGroup `12677` lists level-50
  Creature2 `69139`, but no current PE390 objective row in the generated
  objective map uses any of those target groups; `HallucinatingLivestockEntityScript`
  remains scoped to Creature2 `46483` and `46714`. The same-name level-50
  source creature `28018` / relation `2003` is reviewed by the Prime bridge
  slice above and still blocked on runtime/client smoke. The generated
  `creature_public_event_map.csv`
  now reports relation `422` as `reviewed`; `creature_public_event_map.csv`
  totals after this review were `70` reviewed, `622` ambiguous, `930`
  scored-name, `924` unique-name, and `99` unmatched rows. The review closes
  only the normal bridge blocker; Creature2 `45729` objective ownership,
  spawn/import placement, objective-credit behavior, combat behavior, rewards,
  medals, achievements, replay cleanup, Prime/level-50 Violent Oxiancow
  behavior, and full Space Madness client smoke remain blocked pending live
  build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `161` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3708`
  Rampaging Oxian -> Creature2 `45730`. This was not promoted from scored-name
  matching alone: PE390 relation `161`, 82 local world `2149` / area `2419`
  source coordinate rows, and Skull Smash/Resounding Charge/Ram/Bash/Bucking
  Frenzy/Overwhelming Bellow spell rows support the normal level-32 bridge.
  TargetGroup `6363` lists Creature2 `45730` with livestock-family rows, but no
  current PE390 objective row in the generated objective map uses TargetGroup
  `6363`; `HallucinatingLivestockEntityScript` remains scoped to Creature2
  `46483` and `46714`. The same-name level-50 source creature `28126` /
  relation `2027` remains mapped-only to Creature2 `69140` and was not
  promoted. The generated `creature_public_event_map.csv` now reports relation
  `161` as `reviewed`; `creature_public_event_map.csv` totals after this
  review were `69` reviewed, `622` ambiguous, `931` scored-name, `924`
  unique-name, and `99` unmatched rows. The review closes only the normal
  bridge blocker; Creature2 `45730` objective ownership, spawn/import
  placement, objective-credit behavior, combat behavior, rewards, medals,
  achievements, replay cleanup, Prime/level-50 Rampaging Oxian behavior, and
  full Space Madness client smoke remain blocked pending live build 16042
  evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `160` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3696`
  Smoke Nightmare -> Creature2 `46722`. This was not promoted from
  scored-name matching alone: PE390 relation `160`, 67 local world `2149` /
  area `2416` source coordinate rows, and Jab/Punch spell rows support the
  normal level-32 bridge. TargetGroup `6449` lists Creature2 `46722`, `46723`,
  and `46724`, but no current PE390 objective row in the generated objective
  map uses TargetGroup `6449`; objective `1590` script evidence remains scoped
  to TargetGroups `6400`/`6401`. The same-name level-50 source creature
  `28115` / relation `2022` remains mapped-only to Creature2 `69208` and was
  not promoted. While adding this row, the Eye Nightmare, Foaming Roandoe, and
  Smoke Nightmare override reason text was normalized to avoid embedded commas
  in the unquoted override CSV and preserve `reviewer`/`reviewed_at` fields.
  The generated `creature_public_event_map.csv` now reports relation `160` as
  `reviewed`; `creature_public_event_map.csv` totals after this review were
  `68` reviewed, `622` ambiguous, `932` scored-name, `924` unique-name, and
  `99` unmatched rows. The review closes only the normal bridge blocker;
  Creature2 `46722` objective ownership, spawn/import placement,
  objective-credit behavior, combat behavior, rewards, medals, achievements,
  replay cleanup, Prime/level-50 Smoke Nightmare behavior, and full Space
  Madness client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `159` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3684`
  Foaming Roandoe -> Creature2 `45727`. This was not promoted from scored-name
  matching alone: PE390 relation `159`, 62 local world `2149` / area `2419`
  source coordinate rows, and Ram/Headbutt/Bucking Frenzy spell rows support
  the normal level-32 bridge. TargetGroup `6363` lists Creature2 `45727` with
  livestock-family rows, but no current PE390 objective row in the generated
  objective map uses TargetGroup `6363`; `HallucinatingLivestockEntityScript`
  remains scoped to Creature2 `46483` and `46714`. The same-name level-50
  source creature `28056` / relation `2010` remains mapped-only to Creature2
  `69171` and was not promoted. The generated `creature_public_event_map.csv`
  now reports relation `159` as `reviewed`; `creature_public_event_map.csv`
  totals after this review were `67` reviewed, `622` ambiguous, `933`
  scored-name, `924` unique-name, and `99` unmatched rows. The review closes
  only the normal bridge blocker; Creature2 `45727` objective ownership,
  spawn/import placement, objective-credit behavior, combat behavior, rewards,
  medals, achievements, replay cleanup, Prime/level-50 Foaming Roandoe
  behavior, and full Space Madness client smoke remain blocked pending live
  build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `156` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3667`
  Masked Arachnid -> Creature2 `46720`. This was not promoted from scored-name
  matching alone: PE390 relation `156`, 92 local world `2149` / area `2416`
  source coordinate rows, and Pinch/Slice spell rows support the normal
  level-32 bridge. TargetGroup `6448` lists Creature2 `46720`, but no current
  PE390 objective row in the generated objective map uses TargetGroup `6448`.
  The same-name level-50 source creature `28170` / relation `2037` remains
  mapped-only to Creature2 `69206` and was not promoted. The generated
  `creature_public_event_map.csv` now reports relation `156` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `66`
  reviewed, `622` ambiguous, `934` scored-name, `924` unique-name, and `99`
  unmatched rows. The review closes only the normal bridge blocker; Creature2
  `46720` objective ownership, spawn/import placement, objective-credit
  behavior, combat behavior, rewards, medals, achievements, replay cleanup,
  Prime/level-50 Masked Arachnid behavior, and full Space Madness client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `154` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3663`
  Eye Nightmare -> Creature2 `46723`. This was not promoted from scored-name
  matching alone: PE390 relation `154`, 62 local world `2149` / area `2416`
  source coordinate rows, and Punch/Jab/Flourishing Combo/Haymaker spell rows
  support the normal level-32 bridge. TargetGroup `6449` lists Creature2
  `46723`, but no current PE390 objective row in the generated objective map
  uses TargetGroup `6449`; objective `1590` script evidence remains scoped to
  TargetGroups `6400`/`6401`. The same-name level-50 source creature `28023` /
  relation `2005` remains mapped-only to Creature2 `69209` and was not
  promoted. The generated `creature_public_event_map.csv` now reports relation
  `154` as `reviewed`; `creature_public_event_map.csv` totals after this
  review were `65` reviewed, `622` ambiguous, `935` scored-name, `924`
  unique-name, and `99` unmatched rows. The review closes only the normal
  bridge blocker; Creature2 `46723` objective ownership, spawn/import
  placement, objective-credit behavior, combat behavior, rewards, medals,
  achievements, replay cleanup, Prime/level-50 Eye Nightmare behavior, and full
  Space Madness client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2016` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28092`
  Hallucinating Cubig -> Creature2 `69137`. This was not promoted from
  scored-name matching alone: exact level-50 source/client mapping, 96 local
  world `2149` / area `2421` source coordinate rows, shared Chomp/Bite/Bruising
  Rush/Pulverizing Vault spell evidence, the already-reviewed normal source
  creature `3654` / relation `152` -> Creature2 `45724`, and objective `2115`
  TargetGroup `12684` nesting TargetGroup `12683` nesting TargetGroup `12677`
  containing Creature2 `69137` support the Prime bridge. No direct Space
  Madness script or test binding for Creature2 `69137`, Creature2 `45724`, or
  Hallucinating Cubig was found. The generated `creature_public_event_map.csv`
  now reports relation `2016` as `reviewed`; broad `creature_public_event_map.csv`
  totals after this review are `89` reviewed, `621` ambiguous, `915`
  scored-name, `921` unique-name, and `99` unmatched rows; PE390 totals are `55`
  reviewed, `13` ambiguous, and `54` scored-name. The review closes only the
  Prime bridge blocker; Creature2 `69137` spawn/import placement, objective
  ownership/usage, objective credit, objective `2115` wave ownership, phase
  timing, rewards, medals, achievements, replay cleanup, and full Space Madness
  client smoke remain blocked pending live build 16042 evidence. Relations
  `153`, `2007`, and `2422` remain blocked pending row-level/live/client
  evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `152` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3654`
  Hallucinating Cubig -> Creature2 `45724`. This was not promoted from
  scored-name matching alone: PE390 relation `152`, 66 local world `2149` /
  area `2421` source coordinate rows, and Chomp/Bite/Bruising
  Rush/Pulverizing Vault spell rows support the normal level-32 bridge. The
  same-name level-50 source creature `28092` / relation `2016` is reviewed by
  the later Prime bridge slice to Creature2 `69137`. The generated
  `creature_public_event_map.csv` now reports relation `152` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `64`
  reviewed, `622` ambiguous, `936` scored-name, `924` unique-name, and `99`
  unmatched rows. The review closes only the normal bridge blocker; Creature2
  `45724` spawn/import placement, livestock objective-credit ownership, combat
  behavior, rewards, medals, achievements, replay cleanup, Prime Cubig runtime
  behavior, and full Space Madness client smoke remain blocked pending
  live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `149` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3643`
  Maddened Roanstag -> Creature2 `45726`. This was not promoted from
  scored-name matching alone: PE390 relation `149`, 63 local world `2149` /
  area `2421` source coordinate rows, and Headbutt/Ram/Bucking Frenzy spell
  rows support the normal level-32 bridge. The same-name level-50 source
  creature `28134` / relation `2119` remains mapped-only to Creature2 `69138`
  and was not promoted. The generated `creature_public_event_map.csv` now
  reports relation `149` as `reviewed`; `creature_public_event_map.csv` totals
  after this review were `63` reviewed, `622` ambiguous, `937` scored-name,
  `924` unique-name, and `99` unmatched rows. The review closes only the
  normal bridge blocker; Creature2 `45726` spawn/import placement, livestock
  objective-credit ownership, combat behavior, rewards, medals, achievements,
  replay cleanup, and full Space Madness client smoke remain blocked pending
  live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2023` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28116`
  Exact Change 3.1 -> Creature2 `69204`. This was not promoted from
  unique-name matching alone: PE390 relation `2023`, Q9883 `Deconstruction`
  objective relation `8027`, contract relation `1112`, 59 local world `2149` /
  area `2416` source coordinate rows, and Exact Change 3.x spell-family rows
  all map the source creature to Creature2 `69204`. This is bridge-only; no
  Space Madness phase spawn, script filter, contract objective behavior, or
  runtime data import was widened for `69204`. The generated
  `creature_public_event_map.csv` now reports relation `2023` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `62`
  reviewed, `622` ambiguous, `938` scored-name, `924` unique-name, and `99`
  unmatched rows. The review closes only the bridge blocker; Creature2 `69204`
  spawn/import placement, Space Madness objective-credit ownership,
  Q9883/contract spawn availability and tap behavior, Prime/contract client
  smoke, rewards, medals, achievements, replay cleanup, and negative cases
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `151` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3645`
  Exact Change 3.0 -> Creature2 `46483`. This was not promoted from
  unique-name matching alone: PE390 objective `1593` is a direct
  `KillEventObjectiveUnit` count-`10` row for Hallucinating Livestock,
  `SpaceMadnessEventScript` spawns Exact Change 3.0 entity `1100300009` as
  Creature2 `46483` during phase `4`, and
  `HallucinatingLivestockEntityScript` filters Creature2 `46483` and `46714`
  while crediting `PublicEventObjective.KillHallucinatingLivestock`. Source
  creature `3645` has 94 local world `2149` / area `2416` coordinate rows for
  Creature2 `46483`; reviewed WIP instance entity evidence tracks entity
  `1100300009`. The generated `creature_public_event_map.csv` now reports
  relation `151` as `reviewed`; `creature_public_event_map.csv` totals after
  this review were `61` reviewed, `622` ambiguous, `938` scored-name, `925`
  unique-name, and `99` unmatched rows. The review closes only the unique-name
  bridge blocker; phase-four spawn/import placement, livestock combat behavior,
  objective `1593` UI timing and credit, rewards, medals, achievements, replay
  cleanup, and full Space Madness client smoke remain blocked pending live
  build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2017` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28093`
  Party Down-Grazer -> Creature2 `69901`. This was not promoted from
  unique-name matching alone: build 16042 objective `4711` maps object
  TargetGroup `12597`, TargetGroup `12597` has sole member Creature2 `69901`,
  parent objective `4712` tracks the aggregate escaped-experiment collection,
  and `PartyDowngrazerEntityScript` filters Creature2 `69901` while crediting
  `PublicEventObjectiveType.ActivateTargetGroup` TargetGroup `12597` plus
  `PublicEventObjective.CollectEscapedCreatures`. Source creature `28093` has
  71 local world `2149` / area `2414` coordinate rows for Creature2 `69901`.
  The generated `creature_public_event_map.csv` now reports relation `2017` as
  `reviewed`; `creature_public_event_map.csv` totals after this review were
  `60` reviewed, `622` ambiguous, `938` scored-name, `926` unique-name, and
  `99` unmatched rows. The review closes only the unique-name bridge blocker;
  escaped-experiment spawn/import placement, objective `4711` collection
  timing, parent objective `4712` aggregate UI behavior, rewards, medals,
  achievements, replay cleanup, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2020` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28112`
  Talking Rockmite -> Creature2 `69900`. This was not promoted from
  unique-name matching alone: build 16042 objective `4710` maps object
  TargetGroup `12596`, TargetGroup `12596` has sole member Creature2 `69900`,
  parent objective `4712` tracks the aggregate escaped-experiment collection,
  and `ExperimentalRockmiteEntityScript` filters Creature2 `69900` while
  crediting `PublicEventObjectiveType.ActivateTargetGroup` TargetGroup `12596`
  plus `PublicEventObjective.CollectEscapedCreatures`. Source creature `28112`
  has 85 local world `2149` / area `2414` coordinate rows for Creature2
  `69900`. The generated `creature_public_event_map.csv` now reports relation
  `2020` as `reviewed`; `creature_public_event_map.csv` totals after this
  review were `59` reviewed, `622` ambiguous, `938` scored-name, `927`
  unique-name, and `99` unmatched rows. The review closes only the unique-name
  bridge blocker; escaped-experiment spawn/import placement, objective `4710`
  collection timing, parent objective `4712` aggregate UI behavior, rewards,
  medals, achievements, replay cleanup, and full Space Madness client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `147` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3638`
  Air Scrubber Controls -> Creature2 `46437`. This was not promoted from
  unique-name matching alone: PE390 objective `1595` maps object/reward-pane
  TargetGroup `6371`, TargetGroup `6371` contains Creature2 `45861` Main Vent
  Lever and Creature2 `46437` Air Scrubber Controls, and
  `AirScrubberControlsEntityScript` filters Creature2 `45861`/`46437` while
  crediting `PublicEventObjectiveType.ActivateTargetGroup` TargetGroup `6371`.
  Source creature `3638` has one local world `2149` / area `2421` coordinate
  row for Creature2 `46437`. The generated `creature_public_event_map.csv` now
  reports relation `147` as `reviewed`; `creature_public_event_map.csv` totals
  after this review were `58` reviewed, `622` ambiguous, `938` scored-name,
  `928` unique-name, and `99` unmatched rows. The review closes only the
  unique-name bridge blocker; Main Vent Lever evidence, objective `1595` UI
  timing/repeat behavior, objective `2115` air-scrubbing timer/wave behavior,
  rewards, medals, achievements, replay cleanup, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `146` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3635`
  Engineering Computer -> Creature2 `45973`. This was not promoted from
  unique-name matching alone: PE390 objective `1603` maps object/reward-pane
  TargetGroup `6372`, TargetGroup `6372` has sole member Creature2 `45973`,
  `SpaceMadnessEventScript` spawns the all-clear Engineering Computer as
  Creature2 `45973`, and `EngineeringComputerEntityScript` filters Creature2
  `45973` while crediting `PublicEventObjectiveType.ActivateTargetGroup`
  TargetGroup `6372`. Source creature `3635` has one local world `2149` / area
  `2421` coordinate row that aligns with the current runtime phase placement.
  The generated `creature_public_event_map.csv` now reports relation `146` as
  `reviewed`; `creature_public_event_map.csv` totals after this review were
  `57` reviewed, `622` ambiguous, `938` scored-name, `929` unique-name, and
  `99` unmatched rows. The review closes only the unique-name bridge blocker;
  objective `1603` UI timing and credit, rewards, medals, achievements, replay
  cleanup, and full Space Madness client smoke remain blocked pending live
  build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `145` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3634`
  Hazmat Suit -> Creature2 `45981`. This was not promoted from scored-name
  matching alone: PE390 objective `1656` maps reward-pane TargetGroup `6362`,
  TargetGroup `6362` has sole member Creature2 `45981`,
  `SpaceMadnessEventScript` spawns the phase Hazmat Suit as Creature2 `45981`,
  and `HazmatSuitEntityScript` filters Creature2 `45981` while
  direct-crediting `PublicEventObjective.EquipAHazmatSuit`. Source creature
  `3634` has local world `2149` / area `2417` coordinate candidates, while the
  current runtime phase placement uses area `2415`; placement/client smoke
  therefore remains blocked. The generated `creature_public_event_map.csv` now
  reports relation `145` as `reviewed`; `creature_public_event_map.csv` totals
  after this review were `56` reviewed, `622` ambiguous, `938` scored-name,
  `930` unique-name, and `99` unmatched rows. The review closes only the
  scored-name bridge blocker; Hazmat Suit spawn/import, objective `1656` UI
  timing and credit, replay cleanup, rewards, medals, achievements, and full
  Space Madness client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `158` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3674`
  Metallic Nightmare -> Creature2 `58798`. This was not promoted from
  ambiguous-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6401`, TargetGroup `6401`
  contains Creature2 `58798`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `58798` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `3674` also has local world `2149` / area `2416` spawn
  candidates and creature spell rows mapped to `58798`; the separate same-name
  source creature `28077` / relation `2312` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `158` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `55` reviewed,
  `622` ambiguous, `939` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the ambiguous-name bridge blocker; Metallic
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `157` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3670`
  Hookfoot Nightmare -> Creature2 `46721`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6401`, TargetGroup `6401`
  contains Creature2 `46721`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46721` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `3670` also has local world `2149` / area `2416` spawn
  candidates and creature spell rows mapped to `46721`; the separate same-name
  source creature `28166` / relation `2035` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `157` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `54` reviewed,
  `623` ambiguous, `939` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the scored-name bridge blocker; Hookfoot
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `144` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `3628`
  Gnarled Nightmare -> Creature2 `58783`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6401`, TargetGroup `6401`
  contains Creature2 `58783`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `58783` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `3628` also has local world `2149` / area `2414` spawn
  candidates and creature spell rows mapped to `58783`; the separate same-name
  source creature `28121` / relation `2026` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `144` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `53` reviewed,
  `623` ambiguous, `940` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the scored-name bridge blocker; Gnarled
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `155` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `884`
  Looming Nightmare -> Creature2 `46135`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6401`, TargetGroup `6401`
  contains Creature2 `46135`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46135` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `884` also has local world `2149` / area `2416` spawn
  candidates and creature spell rows mapped to `46135`; the separate same-name
  source creature `28114` / relation `2021` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `155` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `52` reviewed,
  `623` ambiguous, `941` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the scored-name bridge blocker; Looming
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `31` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `1189`
  Horrifying Nightmare -> Creature2 `46132`. This was not promoted from
  ambiguous-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6401`, TargetGroup `6401`
  contains Creature2 `46132`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46132` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `1189` also has local world `2149` / area `2416` spawn
  candidates and creature spell rows mapped to `46132`; the separate same-name
  source creature `28148` / relation `2034` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `31` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `51` reviewed,
  `623` ambiguous, `942` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the ambiguous-name bridge blocker; Horrifying
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `30` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `1128`
  Shocking Nightmare -> Creature2 `46126`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46126`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46126` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `1128` also has local world `2149` / area `2414` spawn
  candidates and creature spell rows mapped to `46126`; the separate same-name
  source creature `28172` / relation `2038` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `30` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `50` reviewed,
  `624` ambiguous, `942` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the scored-name bridge blocker; Shocking
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `29` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `1121`
  Rending Nightmare -> Creature2 `46130`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46130`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46130` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `1121` also has local world `2149` / area `2414` spawn
  candidates and creature spell rows mapped to `46130`; the separate same-name
  source creature `28035` / relation `2006` is now reviewed by the later Prime
  bridge slice above and remains runtime/client-smoke blocked. The generated
  `creature_public_event_map.csv` now reports relation `29` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `49` reviewed,
  `624` ambiguous, `943` scored-name, `930` unique-name, and `99` unmatched
  rows. The review closes only the scored-name bridge blocker; Rending
  Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relations `28` and `2117` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `1097`
  Captain Tero and duplicate source creature `28243` Captain Tero -> Creature2
  `45900`. This was not promoted from scored-name matching alone: PE390
  objective `1579` maps TargetGroup `6342`, TargetGroup `6342` has sole member
  Creature2 `45900`, `SpaceMadnessEventScript` spawns reviewed entity
  `1100300005`, and `CaptainTeroEntityScript` filters Creature2 `45900` while
  crediting TargetGroup `6342` through `PublicEventObjectiveType.TalkTo`. The
  generated `creature_public_event_map.csv` now reports relations `28` and
  `2117` as `reviewed`; `creature_public_event_map.csv` totals after this review were
  `48` reviewed, `624` ambiguous, `944` scored-name, `930` unique-name, and
  `99` unmatched rows. The review closes only the scored-name bridge blockers;
  shuttle placement, dialog timing, phase visibility, objective `1579` UI
  timing and replay behavior, rewards, medals, achievements, and full Space
  Madness client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `27` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `979`
  Observation Deck Computer -> Creature2 `45972`. This was not promoted from
  unique-name matching alone: PE390 objective `1589` maps TargetGroup `6356`,
  TargetGroup `6356` has sole member Creature2 `45972`,
  `SpaceMadnessEventScript` spawns reviewed entity `1100300007`, and
  `ObservationDeckComputerEntityScript` filters Creature2 `45972` while
  crediting TargetGroup `6356` once. The generated
  `creature_public_event_map.csv` now reports relation `27` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `46` reviewed, `624`
  ambiguous, `946` scored-name, `930` unique-name, and `99` unmatched rows.
  The review closes only the unique-name bridge blocker; coordinate precision,
  phase visibility, objective `1589` UI timing and replay behavior, rewards,
  medals, achievements, and full Space Madness client smoke remain blocked
  pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `26` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `446`
  Ravenous Nightmare -> Creature2 `46133`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6401`, TargetGroup `6401`
  contains Creature2 `46133`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46133` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `446` also has local world `2149` / area `2415` spawn
  candidates and creature spell rows mapped to `46133`; the separate same-name
  source creature `28076` / relation `2013` is reviewed by the Prime bridge
  slice above and remains runtime/client-smoke blocked. The generated
  `creature_public_event_map.csv` now reports relation `26` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `45` reviewed, `624`
  ambiguous, `946` scored-name, `931` unique-name, and `99` unmatched rows. The
  review closes only the scored-name bridge blocker; Ravenous Nightmare
  spawn/import, objective `1590` UI timing and worker combat choreography,
  rewards, medals, achievements, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `25` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `392`
  Writhing Nightmare -> Creature2 `46123`. This was not promoted from
  ambiguous-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46123`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46123` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `392` also has local world `2149` / area `2413` spawn
  candidates and creature spell rows mapped to `46123`; the separate same-name
  source creature `28046` / relation `2009` is now reviewed by the later Prime
  bridge slice above and remains runtime/client-smoke blocked. The generated
  `creature_public_event_map.csv` now reports relation `25` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `44` reviewed, `624`
  ambiguous, `947` scored-name, `931` unique-name, and `99` unmatched rows. The
  review closes only the ambiguous-name bridge blocker; Writhing Nightmare
  spawn/import, objective `1590` UI timing and worker combat choreography,
  rewards, medals, achievements, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `23` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `332`
  Major Lee Barmy -> Creature2 `45812`. This was not promoted from unique-name
  matching alone: PE390 objective `1588` maps TargetGroup `6358`, TargetGroup
  `6358` has sole member `45812`, and `MajorLeeBarmyEntityScript` filters
  Creature2 `45812` while crediting TargetGroup `6358` through
  `PublicEventObjectiveType.TalkTo`. The separate same-target source creature
  `28252` / relation `2120` is now tracked by the later duplicate bridge
  review above. The generated
  `creature_public_event_map.csv` now reports relation `23` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `43` reviewed, `625`
  ambiguous, `947` scored-name, `931` unique-name, and `99` unmatched rows.
  The review closes only the unique-name bridge blocker; relation `23`
  spawn/import placement, objective `1588` UI timing and talk credit, phase
  timing, rewards, medals, achievements, and full Space Madness client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `24` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `389`
  Electrifying Nightmare -> Creature2 `46127`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46127`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46127` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `389` also has local world `2149` / area `2414` spawn
  candidates and creature spell rows mapped to `46127`; the separate same-name
  source creature `28128` / relation `2029` remains untouched. The generated
  `creature_public_event_map.csv` now reports relation `24` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `42` reviewed, `625`
  ambiguous, `947` scored-name, `932` unique-name, and `99` unmatched rows.
  The review closes only the scored-name bridge blocker; Electrifying Nightmare
  spawn/import, objective `1590` UI timing and worker combat choreography,
  rewards, medals, achievements, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `22` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `205`
  Verminous Nightmare -> Creature2 `46131`. This was not promoted from
  unique-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46131`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46131` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `205` is only represented by PE390 relation `22`; no
  competing same-name public-event creature row was found. The generated
  `creature_public_event_map.csv` now reports relation `22` as `reviewed` with
  source-row `last_seen_in=1`; `creature_public_event_map.csv` totals after
  this review were `41` reviewed, `625` ambiguous, `948` scored-name, `932`
  unique-name, and `99` unmatched rows. The review closes only the unique-name
  bridge blocker; Verminous Nightmare spawn/import, objective `1590` UI timing
  and worker combat choreography, rewards, medals, achievements, and full Space
  Madness client smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `20` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `64`
  Venomous Nightmare -> Creature2 `46128`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46128`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46128` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `64` is only represented by PE390 relation `20`; the separate
  same-name source creature `28183` / relation `2039` remains untouched. The
  generated `creature_public_event_map.csv` now reports relation `20` as
  `reviewed`; `creature_public_event_map.csv` totals after this review were `40`
  reviewed, `625` ambiguous, `948` scored-name, `933` unique-name, and `99`
  unmatched rows. The review closes only the scored-name bridge blocker;
  Venomous Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `18` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `39`
  Stinging Nightmare -> Creature2 `46124`. This was not promoted from
  scored-name matching alone: PE390 objective `1590` maps reward-pane
  TargetGroup `6402`, TargetGroup `6402` nests `6400`, TargetGroup `6400`
  contains Creature2 `46124`, and `SavePanickedWorkersNightmareEntityScript`
  filters Creature2 `46124` while crediting `PublicEventObjective.SavePanickedWorkers`.
  Source creature `39` is only represented by PE390 relation `18`; the separate
  same-name source creature `28086` / relation `2014` is reviewed by the later
  Stinging Nightmare Prime bridge slice and remains runtime/client-smoke blocked. The
  generated `creature_public_event_map.csv` now reports relation `18` as
  `reviewed`; `creature_public_event_map.csv` totals after this review were `39`
  reviewed, `625` ambiguous, `949` scored-name, `933` unique-name, and `99`
  unmatched rows. The review closes only the scored-name bridge blocker;
  Stinging Nightmare spawn/import, objective `1590` UI timing and worker combat
  choreography, rewards, medals, achievements, and full Space Madness client
  smoke remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2014` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28086`
  Stinging Nightmare -> Creature2 `69214`. This was not promoted from
  scored-name matching alone: the bridge is backed by exact level-50 source/client
  mapping, 86 world `2149` / area `2413` coordinate rows, source spell evidence
  `2893` / `2911` / `2787` / `6402`, client Spell4 evidence `32965` / `32966` /
  `35963` / `44638`, and the already-reviewed normal source creature `39` /
  relation `18` -> Creature2 `46124`. PE390 objective `1590` evidence remains
  scoped to the normal row through reward-pane TargetGroup `6402`, nested
  TargetGroup `6400`, and `SavePanickedWorkersNightmareEntityScript`; no
  TargetGroup row currently lists Creature2 `69214`, and objective `2115`
  TargetGroup `12684` does not list Creature2 `69214` through nested groups
  `12682` or `12683`. The generated `creature_public_event_map.csv` now reports
  relation `2014` as `reviewed`; `creature_public_event_map.csv` totals after
  this review are `88` reviewed, `621` ambiguous, `916` scored-name, `921`
  unique-name, and `99` unmatched rows. The review closes only the scored-name
  bridge blocker; Stinging Nightmare Prime spawn/import placement, objective
  ownership/credit, objective `2115` wave/timer ownership, rewards, medals,
  achievements, replay cleanup, and full Space Madness client smoke remain
  blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `2033` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28141`
  Slinking Slank -> Creature2 `69899`. This was not promoted from name
  matching alone: PE390 objective `4709` maps object/TargetGroup `12595`,
  TargetGroup `12595` has sole member `69899`, and
  `ExperimentalSlankEntityScript` filters Creature2 `69899` while crediting
  TargetGroup `12595` plus parent `CollectEscapedCreatures`. The generated
  `creature_public_event_map.csv` now reports relation `2033` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `38` reviewed, `625`
  ambiguous, `933` unique-name, and `99` unmatched rows. The review closes only
  the unique-name bridge blocker; Slinking Slank spawn/import, objective `4709`
  UI timing, parent objective `4712` progression, rewards, medals,
  achievements, and full Space Madness client smoke remain blocked pending
  live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge blocker classification: did not
  promote Space Madness public-event creature relation `153` into
  `creature_bridge_overrides.csv`. Generated PE390 evidence maps source
  creature `3655` (`Hallucinating Worker`) to ambiguous Creature2 `45903`, but
  objective `1594` uses TargetGroup `6370` with both Creature2 `45903` and
  `45904`. A global one-to-one source creature override would not prove which
  target-group member or spawn variant relation `153` owns. The content-retail
  audit now reports this row as
  `blocked_public_event_creature_ambiguous_multi_member_targetgroup_requires_relation_smoke`
  with `target_group_id=6370`; the new multi-member blocker bucket has `20`
  next-slice rows across instance slices, and the generic ambiguous bridge
  bucket now has `118` next-slice rows. Required evidence remains relation- or
  spawn-scoped bridge review, live client/server objective-credit smoke, or a
  captured placement/import artifact before any bridge promotion.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Space Madness
  public-event creature relation `19` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `48`
  Datapad -> Creature2 `45993`. This was not promoted from name matching
  alone: generated candidate review had two exact-name Datapad rows (`34440`
  and `45993`), while PE390 objective `1596` maps object `6384`, TargetGroup
  `6384` contains Creature2 `45993`, and source creature `48` is only used by
  Space Madness relation `19` plus world `2149` / area `2416` Datapad spawn
  evidence. `CrewDatapadEntityScript` and focused Space Madness tests pin
  `45993` target-group checklist credit. The generated
  `creature_public_event_map.csv` now reports relation `19` as `reviewed`;
  `creature_public_event_map.csv` totals after this review were `37` reviewed, `625`
  ambiguous, and `99` unmatched rows. The review closes only the ambiguous
  bridge blocker; Datapad spawn/import, objective UI timing, checklist count
  presentation, rewards, achievements, and full Space Madness client smoke
  remain blocked pending live build 16042 evidence.

- 2026-06-18 DM-FND-001 / public-event bridge review: promoted Ultimate
  Protogames public-event creature relation `2244` into tracked
  `Tools/DataMapping/creature_bridge_overrides.csv`: source creature `28533`
  Gate Console -> Creature2 `62427`. This was not promoted from name matching
  alone: generated candidate review had two exact-name Gate Console rows
  (`62427` and `62987`), while build 16042 objective `2847` uses TargetGroup
  `10569` / Creature2 `62987` and the existing Prototentiary script/test
  evidence spawns the Jabbithole room placement as Creature2 `62427` while
  accepting both ids for credit. The generated `creature_public_event_map.csv`
  now reports relation `2244` as `reviewed`. The
  review closes only the ambiguous bridge blocker; Gate Console spawn-credit
  smoke, door choreography, objective UI timing, visual state, replay cleanup,
  rewards, achievements, and full Ultimate Protogames client smoke remain
  blocked pending live build 16042 evidence.

- DM-FND-001: promoted eight already-reviewed local creature bridge decisions
  into tracked `Tools/DataMapping/creature_bridge_overrides.csv`: source
  creatures `1073` Deadeye Brightland -> `11063`, `3367` Mondo Zax -> `24187`,
  `3385` Kezrek Warbringer -> `24158`, `3382` Power Regulator -> `24999`,
  `619` Master Control Panel -> `11194`, `1090` Ship Controls -> `27196`,
  `201` Deadeye Brightland -> `16622`, and `752` Bosun Redmark -> `11062`.
  They were already reviewed by existing-entity spatial evidence and now carry
  downstream next-slice context for `20` quest-creature blocker rows across
  Q3479, Q3480, Q3486, Q3668, Q3673, Q3963, Q5573, Q5575, Q5580, and Q5597.
- Continued review promoted source creature `104` Rootbrute Grimspore ->
  `12212`. This row was not promoted by name alone: Creature2 `12212` has the
  strongest local existing-entity evidence (`81` spatial matches, `21` entity
  matches, nearest `1.136`), keeps faction/level compatibility, and is the
  Q3797 TargetGroup `1177`/`6406` member covered by focused fallback-spawn and
  kill-credit tests. This advances the Q3797 objective relation and keeps
  competing same-name rows (`13118`, `18852`, `13549`, `18787`) unchosen.
- Public-event bridge review promoted `17` high-confidence spatial decisions
  into tracked `Tools/DataMapping/creature_bridge_overrides.csv`: Kuralak the
  Defiler `18294` -> `52969`, Gloomclaw `25826`/`25891` -> `30498`, Aethros
  `720`/`3077` -> `32703`, Bev-O-Rage `28283`/`28294` -> `61463`, Forgemaster
  Trogun `1929` -> `32531`, Slavemaster Drokk `1933` -> `32536`, Avatus
  `27408` -> `30505`, Grond the Corpsemaker `1315` -> `32534`, Grand Warmonger
  Tar'gresh `25654`/`25807` -> `48177`, Frostbringer Warlock `25669` ->
  `31674`, Blazing Crewman `3642` -> `46714`, Security Control Panel `92` ->
  `46092`, and Captain Milo `4653` -> `41201`. Each row was promoted only
  because `prioritize_creature_bridge_reviews.py` found a single
  existing-entity spatial candidate near Jabbithole spawn evidence for the
  public-event relation. `creature_public_event_map.csv` now has `31`
  `reviewed` rows, `631` `ambiguous_name` rows, and `99` `unmatched` rows; the
  PE high-confidence spatial suggestion queue is empty, leaving `6`
  medium-confidence spatial suggestions, `2` medium fuzzy suggestions, and
  `527` no-recommendation PE source creatures for manual/event-specific review.
- DM-FND-001: source creature `435` Commander Durek remains blocked as a
  global creature bridge because current evidence separates starter-side
  Creature2 `11061` from Land's Reach Creature2 `11066`. The blocker was
  reduced by adding tracked relation-scoped reviews in
  `Tools/DataMapping/creature_relation_bridge_overrides.csv`: Q3480 starter
  relation `821` -> `11061`, and Q3741 starter/finisher `69`/`53`, Q3783
  finisher `99`, Q3797 starter/finisher `166`/`1693`, and Q3886
  starter/finisher `1231`/`102` -> `11066`. These rows are guarded by
  relation id, source creature id, and Quest2 id, preserve the original
  ambiguous bridge fields in `creature_quest_map.csv`, and are backed by
  Quest2/WorldLocation2, Creature2 quest-giver/receiver rows, existing
  NorthernWilds script placement, and focused quest/cache tests. Q9071
  finisher relation `588` is still blocked because no local evidence proves
  whether that source relation belongs to the starter-side `11061` context,
  the Land's Reach `11066` context, or another Durek row.
- DM-FND-001 remaining blocked rows: source creature `23645` remains unmatched
  and needs a current Creature2 bridge or reviewed replacement relation.
  Source creatures `34` Bartol Sunward and `1043` Scientist Lusk are now
  reduced for Q3668 by relation-scoped reviews only: relation `37` maps to
  Northern Wilds Bartol `12737`, and relation `43` maps to Northern Wilds
  Scientist Lusk `12484`. Their Arkship same-name Creature2 variants remain
  excluded from global overrides. Source creature `756` Signal Flare still has
  row-specific script/test placement evidence but needs stronger global bridge
  review before it is safe as a tracked global override row.
- DM-FND-002: no broad SQL or Northern Wilds bulk coordinate import was added.
  The promoted bridge rows expose `127` reviewed spawn candidate rows in
  `creature_spawn_map.csv`, but runtime placement stays limited to existing
  script-owned fallbacks or checked-in safe seeds until duplicate density,
  world/area ownership, objective-credit relevance, and client smoke are proven
  row by row. The Durek relation-scoped reviews surface the two existing
  source creature `435` coordinate contexts (`1763` and `40899`) for quest
  relation validation, but no runtime spawn SQL was added and both coordinates
  remain unpromoted DataMapping evidence.

## PvE Content Inventory

### Quests

All 5,194 `Quest2` rows are currently `not_retail_complete`.

| Bucket | Rows | Meaning |
| --- | ---: | --- |
| `generic_table_driven` | 3,840 | Generic runtime can interpret rows, but world spawns, loot, rewards, achievements, and smoke are pending. |
| `no_objectives` | 1,291 | Accept/turn-in/reward/achievement validation still needs dialog and progression smoke. |
| `curated_full` | 49 | Focused tests exist, but manual/client smoke is still pending. |
| `generic_with_script` | 14 | Script hook exists, but world and owner-family proof are pending. |

Major quest dependency gaps:

| Dependency | Count / evidence |
| --- | ---: |
| Quest objectives | 7,108 rows, all mapped to generic handlers; 6,237 still pending world/trigger smoke. |
| Rewards | 8,767 reward/dependency rows; 5,329 mapped runtime reward rows pending row smoke; 86 blocked by active reward rotation schedule. |
| Prerequisites | 14,965 dependency rows; 9,260 accept-validation rows and 1,828 chain rows pending smoke; 2 missing client prerequisite rows. |
| Quest loot | 7,650 rows; 5,296 reviewed entity-loot bindings pending spawn/drop smoke; 18 orphaned current-objective mismatches. |
| World/interaction | 39,038 rows; large pending buckets for target-group members, world zones, objective indicator locations, receiver locations, and quest directions. |

Current quest-first work queue:

| Rank | Quest | Name | Primary blocker | Blockers |
| ---: | ---: | --- | --- | ---: |
| 1 | 5597 | Dregs and Thieves | quest zone evidence | 32 |
| 2 | 3479 | From the Wreckage | quest zone evidence | 44 |
| 3 | 3480 | Reporting for Duty | quest world dependency | 32 |
| 4 | 3486 | Empowered Tower | quest loot | 39 |
| 5 | 3668 | Indigenous Intelligence | quest creature | 39 |
| 6 | 3673 | Contact with Thayd | quest reward | 30 |
| 7 | 3886 | Fiery Distraction | quest creature | 23 |
| 8 | 3963 | More Important Than Revenge | quest creature | 21 |
| 9 | 5573 | Powering Down | quest world dependency | 31 |
| 10 | 5575 | Seizing Power | quest world dependency | 25 |
| 11 | 5580 | Enforced Radio Silence | quest world dependency | 26 |
| 12 | 3797 | Securing the Area | quest creature | 23 |

Work rule: close these with focused tests plus client/runtime smoke for dialog,
objective credit, reward presentation, achievement/progression UI, and negative
cases named in `content_retail_completeness_next_slice_queue.csv`.

### Public Events

| Gap | Evidence | Next action |
| --- | --- | --- |
| PE objective runtime mechanics are mostly unverified. | Public-event evidence rows: 5,013 total; 1,800 objective rows matched but pending runtime mechanics smoke. | For each event slice, prove activation, objective credit timing, state transitions, and reward/scoreboard behavior. |
| PE creature relationships inherit creature bridge uncertainty. | 2,645 PE creature evidence rows remain not retail-complete; generated `creature_public_event_map.csv` now has 60 reviewed, 622 ambiguous, 938 scored-name, 926 unique-name, and 99 unmatched rows. | Resolve affected Creature2 bridges before using rows as spawn/credit proof; remaining high-impact rows mostly need event-specific placement, target-group, or client-smoke evidence rather than automatic spatial promotion. |
| PE zones and parentage need visibility/activation proof. | 445 zone rows matched pending visibility smoke; 415 parent-not-current blockers. | Audit event parent/child chain against current build 16042 rows and smoke map/event UI. |
| PE auxiliary tables are inventory-only. | 162 auxiliary rows: votes, depot, virtual item depot, custom stats, stat display, bomb/gather/resource state. | Do not emit aux packets or implement vote/depot semantics until producer/UI behavior is proven. |

### Dungeons, Expeditions, Raids, Event Instances, World Story

All 27 tracked instance rows are `not_retail_complete`.

| Content type | Rows |
| --- | ---: |
| dungeon | 8 |
| expedition | 8 |
| raid | 7 |
| event instance | 2 |
| world story | 2 |

Instance blocker totals from the next-slice queue:

| Blocker category | Rows |
| --- | ---: |
| instance dependency | 736 |
| public event evidence | 584 |
| script objective producer | 457 |
| instance entity | 105 |
| instance reward | 48 |
| instance portal evidence | 11 |
| instance script handler | 5 |

Current instance work queue:

| Rank | World | Name | Actionability | Primary blocker | Blockers |
| ---: | ---: | --- | --- | --- | ---: |
| 13 | 382 | Stormtalon's Lair | split required | public event evidence | 126 |
| 14 | 2980 | Ultimate Protogames | split required producer gap | instance dependency | 331 |
| 15 | 3009 | Vault of the Archon | split required | instance dependency | 237 |
| 16 | 1271 | Sanctuary of the Swordmaiden | split required | public event evidence | 229 |
| 17 | 3180 | Fragment Zero | split required | script objective producer | 186 |
| 18 | 1263 | Skullcano | split required | public event evidence | 170 |
| 19 | 2183 | Gauntlet | split required | public event evidence | 206 |
| 20 | 3404 | Evil from the Ether | split required | script objective producer | 151 |
| 21 | 2149 | Space Madness | split required | public event evidence | 233 |
| 22 | 3041 | Ultimate Protogames Raid map | validation ready | instance dependency | 26 |
| 23 | 1336 | Ruins of Kel Voreth | split required | public event evidence | 129 |

Work rule: split large instances into boss/objective/portal/reward slices. A
scripted objective producer is not retail-complete until exact phase order,
credit timing, cleanup, rewards, achievements, and client-visible behavior are
smoked.

### Path Missions

| Gap | Evidence | Next action |
| --- | --- | --- |
| Path mission runtime/smoke is broad. | 4,623 evidence rows; 1,064 client path mission rows pending runtime smoke. | Pick path type slices: Explorer, Scientist, Soldier, Settler. Verify activation, progress, reward, and UI. |
| Creature targets need review. | 1,760 path mission creature rows pending review/smoke; 266 ambiguous; reviewed rows still need spawn/credit smoke. | Resolve bridge rows by path mission impact and local world placement. |
| Episode bridge has holes. | `path_episode_map.csv`: 80 matched, 13 unmatched. | Reconcile missing episode rows before claiming path episode progression. |

### Challenges

| Gap | Evidence | Next action |
| --- | --- | --- |
| Challenge reward-track parity is incomplete. | 22,721 challenge evidence rows; 16,536 reward-item rows pending reward-track smoke. | Map reward tracks/result packets before claiming full rewards. |
| Challenge target creatures need review/smoke. | `challenge_creature_map.csv`: 832 ambiguous, 115 unmatched, 173 reviewed. | Promote reviewed high-impact target rows; smoke activation, progress, tier, reward UI. |
| Challenge manager is implemented but not full retail. | Challenge lifecycle exists; share-init ownership, medal win-chance, and result/reward parity remain blocked elsewhere. | Keep challenge content rows pending until reward/medal/share evidence closes. |

### Contracts

| Gap | Evidence | Next action |
| --- | --- | --- |
| Contract rows are mapped but availability/reward behavior needs smoke. | 42 contract quests matched; 736 reward rows matched. | Verify periodic availability, accept/complete, rewards, achievement hooks, and UI. |
| Contract creature rows inherit bridge uncertainty. | 2,436 creature rows pending review/smoke; 385 ambiguous; 38 unmatched. | Resolve Creature2 bridge rows before objective-credit claims. |

### Achievements, Zone Completion, And Map Completion

| Gap | Evidence | Next action |
| --- | --- | --- |
| Achievement data is mapped but row-specific content hooks need proof. | 4,943 client achievements, 6,568 checklist rows, 2,324 Jabbithole achievement rows bridged. | Audit achievement type families whose object ids are data-only or lack runtime callsites. |
| Quest achievement dependencies need smoke. | Next-slice blockers include 14 quest-achievement blockers; all quest rows remain not-retail-complete. | For each content slice, verify achievement progress UI and side effects. |
| Zone completion is implemented but content validation remains open. | Zone completion tables are small; content tracker still marks rows not retail-complete. | Validate category totals, path-specific semantics, and non-title rewards only with table/client proof. |

### Loot, Rewards, And Vendors

| Gap | Evidence | Next action |
| --- | --- | --- |
| Mapped creature loot is not fully promoted. | 522,130 versioned drops mapped; 198,735 runtime `creature_loot` rows promoted. | Audit skipped statuses, ambiguous/unmatched Creature2 rows, duplicate/conflicting item rows, and stack-count policy. |
| Quest loot is reviewed but not smoked. | LaughingWS quest loot overlay: 1,174 groups, 5,302 entity-loot rows, 1,174 loot items; most pending drop smoke. | Smoke objective loot drops, virtual-item behavior, and negative cases per quest slice. |
| Vendor data is only safe for existing placed vendors. | 20,650 vendor rows mapped; 192 runtime `entity_vendor` rows locally. | Add missing placed vendor entities only through reviewed world slices; avoid creating stock for unplaced creatures. |
| Reward rotation remains data-known but semantics-gated. | `RewardRotation*` and instance reward evidence exist; Fortune weights explicitly not derivable from these tables. | Keep reward rotation and Fortune separate; require capture/catalog proof for active rotations and weights. |

## Game Mechanics Inventory

| Id | System | Missing or incomplete mapping | Data strength | Gate |
| --- | --- | --- | --- | --- |
| DM-MECH-001 | Spell runtime F-016..F-020 | Many spell effects, procs, buffs, CC, summons, traps, vehicles, target routing, and formulas remain family-gated. | High for fixtures: 66,383 `Spell4`, 131,010 `Spell4Effects`; 167 Jabbithole spells unmatched. | Use one family at a time with decompile/live `/spell inspect4`/cast evidence and focused tests. |
| DM-MECH-002 | Crafting and tradeskills F-008 | Discovery, current-craft, aux `0x084B`/`0x0855`, complex stats, non-success sigil rules, and microchip patch timing are blocked. | Strong static tables, weak producer timing. Schematics: 2,669 matched, 1,905 unmatched; materials/circuits/microchips have large partial/unmatched counts. | Audit schematics/components first; do not widen mutation until current-craft/aux evidence exists. |
| DM-MECH-003 | Items, unlocks, costumes, pets F-026 | Item-data aux, supply satchel aux, costume aux, pet lifecycle/timing, unlock deltas, item eligibility precision remain incomplete. | Strong item/account data; item sigils 81 matched, 1,227 unmatched. | Use item tables for validation only until producer/readback evidence exists. |
| DM-MECH-004 | Housing F-004 | Neighborhood list producer, edit-mode ack/broadcast, contribution/upkeep/refund/ownership precision, and community semantics are incomplete. | Strong decor/plug data; weak neighborhood packet semantics. | Implement decor/plug validation slices; keep `0x0501`/`0x0506` blocked until capture/native producer proof. |
| DM-MECH-005 | Transport F-009 | Taxi embark/completion, global route state, service-token bypass, passenger/seat behavior, deployable vehicles are incomplete. | Strong route/location data. | Route graph/pricing can be audited now; packet sequence and movement lifecycle need smoke/native proof. |
| DM-MECH-006 | Matching, group, raids F-010 | Replacement backfill, non-zero raid queue status, role selection, queue timing, raid locks, cross-realm pools remain incomplete. | Medium table data; packet semantics partially mapped. | Validate matching map/prerequisites first; keep ambiguous queue outputs non-emitted. |
| DM-MECH-007 | PvP and duels F-015 | Duel core works, but observers, rewards/stats, forced-map PvP, broader open-world PvP rules are incomplete. | Medium. | Require PvP/duel captures before widening combat boundaries. |
| DM-MECH-008 | Marketplace, storefront, reward rotation, Fortune F-005/F-006/F-007/F-031 | Marketplace core exists; residual property/rune filters, CREDD tails, aux packets, VC billing, type-1/type-2 store effects, active reward rotations, and Fortune weights remain incomplete. | Strong catalog/item data, weak retail active rotation/weight source. | Keep emulator-only Fortune weights; require retail capture/catalog dumps for exact weights and rotations. |
| DM-MECH-009 | Guild, war party, social, ICComm F-011/F-012 | Guild bank/perks/recruitment, warplot tokens/plugs, persistent ICComm, throttling, entitlement checks, chat aux remain incomplete. | Low to medium. | Use tables for validation fixtures; require lifecycle/state evidence for mutation. |
| DM-MECH-010 | Realm, PTR, support, options F-027/F-028/F-030 | Realm transfer destinations/success, PTR copy queue, support-case backend/readback, account-vs-character option readback remain incomplete. | Low to medium. | Treat as compatibility boundaries until product/backend design and packet evidence exist. |
| DM-MECH-011 | Entity aux, tracking, visibility, CSI F-025 | Entity-stat aux emits, map-tracked unit producer timing, phase visibility, visual flag semantics, deeper CSI/deferred action semantics remain incomplete. | Medium for packet shape and tables; weak for producer timing. | Do not emit shape-only packets; require native apply/send or live sniff. |
| DM-MECH-012 | World, map, environment, audio/visual data | World/socket/layer/clutter/sky/water/sound/visual tables are exported, but most are client presentation/reference only. | Strong extraction, low server mutation value. | Use as labels/fixtures unless an owning runtime feature needs specific rows. |

## Suggested Gradual Order

1. Keep local operational state clean: `nf_map_*` authoring/runtime split,
   `96/96` staging load, and small-world Northern Wilds overlay verification.
2. Continue creature bridge review for the highest-impact quest/public-event
   rows in the next-slice queue.
3. Close the first 12 quest validation slices, one quest at a time.
4. Split large instance rows by objective/boss/portal/reward and close the
   validation-ready `instance-3041` slice early.
5. Add curated audits for tradeskill schematic/component gaps and item sigil
   gaps before broadening crafting/item runtime behavior.
6. Work public-event/path/challenge/contract evidence through focused runtime
   hooks and smoke bundles, not broad imports.
7. Promote generic client-source table families only when the owning runtime
   system and semantics are known.

## Verification Commands Used

```powershell
python -m py_compile Tools\DataMapping\map_wildstar_data.py Tools\DataMapping\load_mapping_staging_tables.py

python Tools\DataMapping\map_wildstar_data.py --limit-creatures 200 --limit-relation-rows 500 --limit-spawns 500 --output-dir Tools\DataMapping\output_test

python Tools\DataMapping\map_wildstar_data.py --limit-creatures 200 --limit-relation-rows 500 --limit-spawns 500 --include-spline-candidates --output-dir Tools\DataMapping\output_test

python Tools\DataMapping\map_wildstar_data.py

python Tools\DataMapping\load_mapping_staging_tables.py --apply

python Tools\WikiArchiveAudit\content_retail_completeness_audit.py

python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py

python -m pytest Tools\WikiArchiveAudit\test_content_retail_completeness_audit.py Tools\WikiArchiveAudit\test_content_retail_completeness_validator.py -q

Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

Additional read-only summaries were taken from:

- `Tools\DataMapping\output\mapping_summary.md`
- `Tools\DataMapping\output\table_coverage_inventory.csv`
- `Decomp\Analysis\CONTENT_RETAIL_COMPLETENESS_TRACKER.md`
- `Decomp\Analysis\coverage\content_retail_completeness_*.csv`
- local `information_schema` row counts for `wildstar_client`, `jabbithole`,
  `nexus_forever_world`, and `nexus_forever_mapping`
