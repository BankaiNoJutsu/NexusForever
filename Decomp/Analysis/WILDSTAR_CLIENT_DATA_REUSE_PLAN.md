# WildStar Client Data Reuse Plan

Status date: 2026-06-05

## Purpose

This plan records how to reuse the local `wildstar_client` database for safe,
evidence-backed NexusForever server work, especially for feature areas that are
still partial, mapped-only, or blocked in `CURRENT_STATUS.md` and
`Decomp/Analysis/MISSING_FEATURE_MATRIX.md`.

The central rule is unchanged: runtime code must not query `wildstar_client`,
`jabbithole`, or `nf_map_*` staging tables. Client/reference data can guide
implementation, audits, tests, and reviewed imports, but runtime behavior must
consume only typed game tables, runtime-owned world/auth tables, scripts, or
code-owned assets.

## Local Evidence Snapshot

The local MySQL reference state was queried from the repository root on
2026-06-05.

| Fact | Local evidence |
| --- | --- |
| MySQL client | MySQL 8.0.46 |
| Available schemas | `wildstar_client`, `jabbithole`, `nexus_forever_world`, `nexus_forever_auth` |
| `wildstar_client` schema | 388 tables, 0 views, about 3.58M estimated rows |
| Split-file coverage | `nexus_forever_world.nf_map_table_coverage_inventory` reports 387 mapped `wildstar_client_mysql` files and 106 `jabbithole_mysql` files |
| Loaded staging | 95 `nf_map_*` tables are present in `nexus_forever_world` |
| Runtime boundary guard | `Source/NexusForever.Game.Tests/Data/RuntimeDataBoundaryTests.cs` asserts the EF world model does not map `nf_map_*`, `wildstar_client`, or `jabbithole` |

High-value exact row counts:

| Table | Rows | Main server use |
| --- | ---: | --- |
| `Spell4` | 66,383 | Spell family fixtures, validation, cooldowns, target/effect evidence |
| `Spell4Effects` | 131,010 | Effect dispatch fixtures; not behavior proof by itself |
| `Item2` | 71,918 | Rewards, loot, marketplace filters, account-item display validation |
| `Creature2` | 53,137 | Creature identity, display/outfit/action-set bridge |
| `WorldLocation2` | 33,396 | Transport, match entrances, quest/path/event indicators |
| `Prerequisite` | 32,131 | Quest, account, housing, item, spell, path, challenge gates |
| `Quest2` | 5,194 | Quest lifecycle and chain metadata |
| `QuestObjective` | 10,031 | Generic objective handlers and content hooks |
| `Quest2Reward` | 5,415 | Quest reward grants and reward gap audits |
| `Achievement` | 4,943 | Achievement types, object ids, rewards, prerequisites |
| `AchievementChecklist` | 6,568 | Checklist/object mapping |
| `Challenge` | 643 | Challenge activation, targets, counts, reward tracks |
| `ChallengeTier` | 1,684 | Challenge tier thresholds |
| `PathMission` | 1,064 | Path mission activation, target/object completion |
| `PathReward` | 302 | Path reward grants |
| `PublicEvent` | 642 | Public event placement, parentage, reward rotation references |
| `PublicEventObjective` | 3,703 | Event objective state and quest objective crosswalks |
| `ZoneCompletion` | 28 | Zone completion totals and title rewards |
| `MatchingGameType` | 69 | Match type level/team/time/rule metadata |
| `MatchingGameMap` | 99 | Queue map/world/prerequisite metadata |
| `TaxiNode` | 261 | Taxi node identity, faction, world location |
| `TaxiRoute` | 238 | Route graph and prices |
| `TrackingSlot` | 175 | Public-event objective to tracking-slot lookup |
| `HousingDecorInfo` | 3,248 | Decor cost, currency, scale, unlock prerequisite, active prop |
| `HousingPlugItem` | 514 | Plot compatibility, resources, contribution/upkeep, upgrades |
| `HousingContributionInfo` | 326 | Plug contribution item tiers and point values |
| `TradeskillSchematic2` | 2,867 | Recipe outputs, material costs, discovery coordinates |
| `TradeskillAdditive` | 129 | Additive tier/vector/radius evidence |
| `TradeskillCatalyst` | 164 | Catalyst modifier values |
| `AccountItem` | 2,219 | Account inventory, Fortune display candidates, entitlements |
| `RewardRotationContent` | 35 | Reward schedule content ids |
| `RewardRotationItem` | 6 | Reward-rotation item rows, not Fortune weights |
| `GameFormula` | 1,217 | Narrow formula lookups after id/function evidence |

Estimated category shape from `information_schema`:

| Category | Tables | Estimated rows | Planning meaning |
| --- | ---: | ---: | --- |
| Localized/UI text | 11 | 2,142,991 | Names, labels, and UX text only |
| World/creature reference | 24 | 545,668 | Creature identity, geometry, world locations, splines |
| Spell runtime | 34 | 372,514 | Spell/effect fixtures and validation witnesses |
| Content progression | 77 | 243,682 | Quest, path, public event, challenge, achievement, map completion |
| Item/account/reward | 28 | 91,276 | Rewards, loot, item eligibility, account inventory |
| Tradeskill/crafting | 14 | 6,444 | Recipes, materials, additives, catalysts, talents |
| Housing | 18 | 5,122 | Decor, plots, plugs, resources, contribution costs |
| Storefront | 3 | 2,342 | Store display/link metadata |
| Reward rotation | 8 | 671 | Reward schedules; separate from Fortune weights |
| Transport/directions | 4 | 664 | Taxi/bind/direction tables |
| Guild/warparty | 3 | 222 | Guild perks, permissions, standard parts |
| Matching/group | 5 | 183 | Match types, maps, rewards, prerequisites |

## Evidence Rules For Reuse

Use `wildstar_client` data as one witness. Promote behavior only when the data
has the right kind of second witness for the risk involved.

| Evidence class | Safe use | Required second witness | Unsafe inference |
| --- | --- | --- | --- |
| Stable static ids and labels | Enum/id naming, test fixtures, validation, UI-safe names | Typed `GameTable` model or existing packet/client consumer | Naming packet fields from nearby table names |
| Client table rules | Costs, counts, prerequisites, reward ids, min/max levels, route prices | Runtime code path already owns the behavior, or decompile/addon/live proof shows the client expects the rule | Assuming producer timing or mutation sequence from table rows |
| Cross-source DataMapping rows | Vendor, loot, creature stats, world/entity candidates, quest/objective bridges | `unique_name`, `scored_name`, or reviewed bridge status plus safe SQL verifier or smoke | Bulk importing raw coordinates, broad zone replacement SQL, or ambiguous creatures |
| Packet/server behavior | Serialization shape, emit timing, ack/result semantics | Client reader/apply path plus runtime/live packet evidence, or two independent native witnesses | Emitting shape-mapped aux packets because a table looks related |
| Stochastic/server-catalog data | Auditable emulator fallback only | Retail packet capture, storefront-server catalog dump, or native server artifact | Claiming retail weights or active rotations from `AccountItem` or `RewardRotation*` alone |

Promotion targets, in preferred order:

1. Existing typed `GameTable` access through `IGameTableManager`.
2. Runtime-owned world/auth tables via migration or reviewed seed SQL.
3. Script-owned constants only when they are narrow, documented, and tied to
   table/decompile/live evidence.
4. Decompile labels/findings/tracker docs when evidence improves structure but
   does not yet justify runtime mutation.

## Current Safe Pipelines

### Typed GameTables

The codebase already has typed entries for the most important client tables,
including quest, path, challenge, public event, achievement, zone completion,
housing, matching, taxi, item, account item, reward rotation, spell, creature,
world location, tracking slot, and tradeskill tables. Prefer these models over
new SQL readers when runtime only needs build-16042 table facts.

Important nuance: not every `GameTableManager` property is marked `[GameData]`
for default loading. Before depending on a table at runtime, verify it is loaded
in the relevant server path or add a focused load/test gate.

### DataMapping Staging

`Tools/DataMapping` already maps all split client/Jabbithole files into curated
CSV outputs and 95 `nf_map_*` staging tables. Existing safe imports cover:

- Vendor rows into `entity_vendor*`.
- Creature and item loot into runtime loot tables.
- Item-container loot groups.
- Creature-info template overrides.
- Optional reviewed entity spawn promotion.
- Focused LaughingWS overlays with WIP/GUESSED markers and deterministic high
  ids.

Staging remains authoring/reference data. Runtime code should only consume rows
after they have been promoted by reviewed SQL, scripts, or migrations.

### Tracker And Test Boundaries

Before widening behavior, update the owning tracker and run the narrowest useful
verification:

- C# runtime change: focused xUnit slice, then `NexusForever.Game.Tests` when
  shared behavior is touched.
- DataMapping/SQL change: mapper smoke or staging load, then
  `Tools/DataMapping/sql/verify_safe_world_imports.sql` when applied.
- Decompile/evidence change: record function addresses, export paths, and
  blocker/closure state in `Decomp/Analysis`.

## Priority Crosswalk For Missing And Incomplete Areas

### 1. Content Progression: F-022, F-023, F-024, F-033, F-036

Client data strength: high.

Key tables and staging:

- `Quest2`, `QuestObjective`, `Quest2Reward`, `EpisodeQuest`, `QuestDirection*`
- `PathMission`, `PathReward`, `PathExplorer*`, `PathScientist*`,
  `PathSettler*`, `PathSoldier*`
- `PublicEvent`, `PublicEventObjective`, `PublicEventVote`,
  `PublicEventDepot`, `PublicEventVirtualItemDepot`
- `Challenge`, `ChallengeTier`, `RewardTrack`, `RewardTrackRewards`
- `Achievement`, `AchievementChecklist`, `ZoneCompletion`, `WorldZone`,
  `MapZone*`, `TargetGroup`
- `nf_map_quest_*`, `nf_map_public_event_*`, `nf_map_path_*`,
  `nf_map_challenge_*`, `nf_map_creature_*`

Safe server reuse:

- Generic objective hooks where `QuestObjective.type`, `data`, `count`, and
  `TargetGroup` membership match an existing runtime event.
- Challenge target resolution and combat-objective hooks through `Challenge`,
  `ChallengeTier`, `TargetGroup`, and DataMapping creature bridges.
- Public event objective credit where runtime scripts already observe the
  creature/object/event transition.
- Zone completion category totals from `ZoneCompletion` once the counted
  sources are implemented.
- Quest/path/public-event reward grants from table rows after item/currency/
  title ids validate against typed tables.

Blocked or unsafe from table data alone:

- Encounter choreography, cinematic timing, phase cleanup, event spawn timing,
  and NPC combat behavior.
- Quest or path action side effects when the client table identifies an object
  id but no runtime event currently proves when to credit it.
- Full zone completion non-title rewards until category semantics and source
  counters are mapped.

Best next slices:

1. Close Challenge reward-track and combat-hook parity by joining `Challenge`,
   `ChallengeTier`, `RewardTrackRewards`, `TargetGroup`, and
   `nf_map_challenge_creature`; add focused tests for one kill target, one
   activation target, and one reward-track grant.
2. Expand `ZoneCompletion` from title-only to full category-gated completion:
   first prove quest/challenge/datacube/journal counts for one zone, then gate
   rewards by faction and category totals.
3. Continue world 3460 / Evil from the Ether / instance WIP rows as smoke-gated
   script work, not broad SQL imports.

### 2. Transport: F-009

Client data strength: high for route graph, medium for flight/taxi lifecycle.

Key tables:

- `TaxiNode` (`worldLocation2Id`, faction, node type, content tier)
- `TaxiRoute` (source, destination, price)
- `WorldLocation2`, `BindPoint`, `WorldSocket`, `MapEntrance` runtime rows

Safe server reuse:

- Build a route graph from `TaxiRoute`.
- Validate node faction and auto-unlock/recommended-level metadata.
- Charge table-backed prices for contiguous route segments.
- Resolve destination positions through `WorldLocation2`.

Blocked or unsafe:

- Taxi embark/completion packet sequence, passenger/seat behavior, global route
  state, service-token bypass, deployable vehicle semantics, and `0x077E`
  producer timing.

Best next slice:

- Implement a conservative taxi embark/completion state machine only after one
  client smoke or native consumer pass confirms the packet sequence. Until then,
  keep table use limited to route selection, pricing, and teleport validation.

### 3. Housing: F-004

Client data strength: high for decor/plug costs and prerequisites, low for
neighborhood packet semantics.

Key tables:

- `HousingDecorInfo`, `HousingWallpaperInfo`, `HousingDecorType`,
  `HousingDecorLimitCategory`
- `HousingPlugItem`, `HousingPlotInfo`, `HousingPlotType`,
  `HousingContributionInfo`, `HousingContributionType`, `HousingResource`
- `HousingNeighborhoodInfo`, `HousingMapInfo`, `HousingResidenceInfo`
- `HousingWarplotBossToken`, `HousingWarplotPlugInfo`

Safe server reuse:

- Decor cost/currency/prerequisite/scale validation.
- Plug plot compatibility, resource prerequisites, contribution tier item
  costs, upkeep cost ids, and upgrade chains.
- Housing vendor catalog validation from plug/decor tables plus reviewed world
  vendor rows.
- Warplot plug/boss-token validation once the owning warplot runtime exists.

Blocked or unsafe:

- Neighborhood list producer trigger and field naming for `0x0501`/`0x0506`.
- Edit-mode ack/broadcast semantics.
- Full ownership, refund, donation transfer, roommate, eviction, and privacy
  timing without packet/client proof.

Best next slices:

1. Add a housing plug contribution/upkeep audit using `HousingPlugItem` ->
   `HousingContributionInfo`; implement only validation and debit/refund paths
   that already have handler ownership.
2. Add decor unlock/refund precision from `HousingDecorInfo.prerequisiteIdUnlock`
   and cost fields, with tests for unsupported prerequisite rows staying blocked.
3. Keep `HousingNeighborhoodInfo` as diagnostic context until a live housing UI
   sniff or native producer path maps `0x0506` timing.

### 4. Crafting And Tradeskills: F-008

Client data strength: high for recipe structure, medium for discovery, low for
aux emit timing.

Key tables:

- `TradeskillSchematic2`
- `TradeskillMaterial`, `TradeskillMaterialCategory`
- `TradeskillAdditive`, `TradeskillCatalyst`, `TradeskillCatalystOrdering`
- `TradeskillBonus`, `TradeskillTalentTier`, `TradeskillTier`
- `TradeskillAchievementLayout`, `TradeskillAchievementReward`
- `Item2`, `ItemRuneInstance`, `ItemRuneSlotRandomization`, `GameFormula`

Safe server reuse:

- Fixed recipe output/material validation from `TradeskillSchematic2`.
- Additive/catalyst identity, tier, vector, and value validation.
- Tech-tree/talent unlock audits through achievement-backed rows.
- Rune slot generation and socket persistence where packet/item evidence is
  already mapped.

Blocked or unsafe:

- Authoritative coordinate-attempt packing, discovery unlock mutation, hot/cold
  emit timing, `ServerCraftingCurrentCraft`, `0x084B`, `0x0855`, non-success
  sigil result rules, and any distinct microchip install mutator.

Best next slices:

1. Compare implemented fixed-recipe paths against all `TradeskillSchematic2`
   rows with material costs and output counts; add a report for unsupported
   schematic flags.
2. Use `TradeskillAdditive` and `TradeskillCatalyst` for validation/state only
   until live crafting captures prove current-craft and aux emit cadence.
3. Audit tech-tree achievements to ensure schematic unlocks line up with
   achievement completion rows before mutating unlock state.

### 5. Matching, Group, PvP, And Leaderboards: F-010, F-015, F-032

Client data strength: medium.

Key tables:

- `MatchingGameType`, `MatchingGameMap`, `MatchingRandomReward`,
  `MatchingMapPrerequisite`, `MatchTypeRewardRotationContent`
- `WorldLocation2`, runtime `map_entrance`
- `PvPRatingFloor`, `RewardRotation*`, `CombatReward`

Safe server reuse:

- Queue eligibility from min/max level, team size, world id, and prerequisites.
- Match map metadata and entrance validation.
- Conservative reward rows after match completion once match lifecycle proves
  the award trigger.
- PvP/PvE leaderboard category fixtures from map/match type tables.

Blocked or unsafe:

- `ServerMatching0x05CF`, `Client0x062A`, `Client0x0634`, standalone non-zero
  `ServerRaidQueueStatus`, replacement backfill lifecycle, exact role-check
  flag semantics, and rating/reward formulas.

Best next slices:

1. Implement missing map/prerequisite validation that can return existing safe
   queue failures without emitting new packet families.
2. Add a data audit comparing `MatchingGameMap.worldId` to runtime
   `map_entrance` coverage.
3. Keep replacement and raid queue state mapped-only until live captures or a
   real client apply dispatcher ties packets to UI state.

### 6. Spell Runtime: F-016 Through F-020 And F-021

Client data strength: high for fixtures, low for behavior without runtime proof.

Key tables:

- `Spell4`, `Spell4Base`, `Spell4Effects`, `Spell4TargetMechanics`,
  `Spell4ValidTargets`, `Spell4AoeTargetConstraints`, `Spell4Telegraph`,
  `Spell4Thresholds`, `Spell4Cooldown`, `SpellLevel`
- `Spell4ServiceTokenCost`, `Spell4Reagent`, `Spell4Prerequisites`
- `GameFormula`, `UnitProperty2`, `TargetGroup`, `Creature2Action`
- `Class`, `ClassSecondaryStatBonus`, DataMapping `class_*` and
  `attribute_*` maps

Safe server reuse:

- Fixture selection for one spell family at a time.
- Validation, prerequisites, reagent/service-token costs, cooldown rows, and
  target masks after native/runtime evidence agrees.
- Attribute, AMP, and LAS audits where table rows map to existing manager state.

Blocked or unsafe:

- New effect family semantics from `Spell4Effects` alone.
- World-target routing for target types without helper-chain proof.
- Aux spell packet emits from shape-only packet models.
- Formula ids without a decompiled caller or already-mapped server use.

Best next slices:

1. Use `wildstar_client` only to select narrow witness spells, then run the
   `EVIDENCE_LOOP_PROCEDURE.md` loop.
2. Promote one family at a time with inspect/cast diagnostics, packet boundary
   proof, and focused tests.
3. Record blocked states rather than widening generic spell behavior.

### 7. Items, Unlocks, Marketplace Filters, Pets, And Costumes: F-005, F-006, F-026

Client data strength: high for item/account eligibility, medium for packet
readback, low for aux producer timing.

Key tables:

- `Item2`, `Item2Family`, `Item2Category`, `Item2Type`, `ItemSlot`,
  `ItemQuality`, `ItemStat`, `ItemSpecial`, `ItemSet*`
- `ItemRuneInstance`, `ItemRuneSlotRandomization`, `ItemRandomStat*`
- `AccountItem`, `AccountItemCooldownGroup`, `Entitlement`, `GenericUnlock*`
- `StoreLink`, `StoreDisplayInfo`, `StoreKeyword`
- `PetFlair`, mount/pet DataMapping maps

Safe server reuse:

- Marketplace item eligibility and unsupported-filter rejection.
- Account-item displayability and target-character validation.
- Generic unlock, costume, title, mount, pet, and flair prerequisites after the
  existing managers own the mutation.
- Item-container and creature loot through reviewed DataMapping imports.

Blocked or unsafe:

- Item/error aux producer semantics, supply satchel precision, costume forget
  ack semantics, option/item aux `0x056B..0x056D`, pet lifecycle timing/scope,
  and account/character unlock delta readback.

Best next slices:

1. Add item filter audits for marketplace property/rune/equippable semantics
   using item taxonomy tables; keep unsupported filters rejected until client
   search semantics are proven.
2. Expand generic unlock persistence only when the table row points at an
   implemented manager and packet deltas are already mapped.
3. Keep pet/costume/satchel packet readback mapped-only until producer evidence
   appears.

### 8. Reward Rotation And Fortune: F-007, F-031

Client data strength: medium for reward rotation, intentionally insufficient
for Fortune weights.

Key tables:

- `RewardRotationContent`, `RewardRotationItem`, `RewardRotationEssence`,
  `RewardRotationModifier`
- `MatchTypeRewardRotationContent`, `World.rewardRotationContentId`,
  `WorldZone.rewardRotationContentId`, `PublicEvent.rewardRotationContentId`
- `AccountItem`, `Item2`, `StoreLink`

Safe server reuse:

- F-007 reward-rotation schedule/catalog context and item/currency delivery
  once the claim path and filters are mapped.
- Fortune display catalog constrained to item-backed `AccountItem` rows already
  guarded by current tests.

Blocked or unsafe:

- Madame Fay exact per-item weights and active rotation. `AccountItem` has no
  chance, weight, season, or active-catalog column. `RewardRotation*` is a
  separate reward schedule surface and is explicitly rejected as Fortune active
  rotation evidence.

Best next slices:

1. Finish F-007 delivery/filter work from reward-rotation tables without
   reusing it as F-031 evidence.
2. Keep Fortune rarity-tier weights marked emulator-only until a retail
   `ServerFortuneRewards` capture, storefront-server catalog dump, or native
   server artifact supplies weights.

### 9. Entity Aux, Tracking, And Visibility: F-025

Client data strength: medium for lookup, low for producer timing.

Key tables:

- `TrackingSlot` (`publicEventObjectiveId`)
- `PublicEventObjective`
- `Creature2`, `Creature2Action`, `Creature2Display*`, `Creature2Outfit*`
- `UnitProperty2`, `WorldLocation2`, `MapZone*`

Safe server reuse:

- Tracking-slot lookup for public-event objectives where duplicate objective
  groups are guarded.
- Entity create display/outfit/stat defaults through reviewed DataMapping.
- Creature action rows as diagnostic or AI-authoring candidates.

Blocked or unsafe:

- Map-tracked-unit producer timing for `0x0849`/`0x0848`.
- Entity-stat aux emits for `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`,
  and `0x093E`.
- `ServerEntityVisualInfoUpdate` flag semantics.

Best next slices:

1. Keep `TrackingSlotHelper` one-way and test guarded until a public-event
   marker sniff or native send site proves producer timing.
2. Use DataMapping creature stats for runtime-owned creature/entity tables, not
   entity-stat aux packet emits.

### 10. Guild, War Party, ICComm, Realm Transfer, Support, Options

Client data strength: low to medium depending on area.

Useful tables:

- Guild/warparty: `GuildPerk`, `GuildPermission`, `GuildStandardPart`,
  `HousingWarplotBossToken`, `HousingWarplotPlugInfo`
- ICComm/chat: `ChatChannel`, `WordFilter`, localized text tables
- Realm: `RealmDataCenter`, character/realm packet evidence, auth/world DB
- Options/keybinds: `InputAction`, `InputActionCategory`, option packet models
- Support: `TicketCategory`, `TicketSubCategory`, `CustomerSurvey`

Safe server reuse:

- Validation fixtures, UI labels, category ids, and permission/perk catalog
  rows.
- Word-filter and ticket/category lookups where handlers already own the
  request.

Blocked or unsafe:

- Guild bank/perks/holomarks, recruitment subscriptions, persistent ICComm
  channels, realm transfer handoff, PTR copy timing, account-vs-character
  option readback, support case lifecycle, and moderation workflow.

Best next slices:

1. Use these tables to strengthen validation and tests.
2. Do not claim parity until packet/client/server lifecycle evidence exists.

## Suggested Execution Order

1. Challenge reward/combat closure (F-033): table-backed, visible gameplay, low
   protocol risk.
2. Zone completion category expansion (F-036): small table count, clear current
   title-only boundary.
3. Matching map/prerequisite audit (F-010): useful validation without new packet
   emits.
4. Housing plug contribution/upkeep validation (F-004): high-quality table data
   and clear blocked packet boundary.
5. Taxi route graph and embark evidence pass (F-009): route data is ready; only
   lifecycle timing needs proof.
6. Crafting schematic/additive audit (F-008): strong static data, but keep
   discovery/current-craft packets blocked.
7. Reward rotation delivery/filter closure (F-007): separate it carefully from
   Fortune.
8. Spell family fixture passes (F-016..F-020): high value, but must remain
   family-by-family.
9. Entity tracking producer proof (F-025): table lookup is already usable; send
   timing is the blocker.
10. Guild/ICComm/realm/support/options: use table data for validation while
    prioritizing packet/lifecycle evidence.

## Query Templates For Future Passes

Use these as starting points, not final implementation.

Challenge reward-track audit:

```sql
SELECT c.ID AS challengeId,
       c.challengeTypeEnum,
       c.target,
       c.completionCount,
       c.rewardTrackId,
       rt.ID AS rewardTrackId,
       rtr.ID AS rewardTrackRewardId
FROM wildstar_client.challenge c
LEFT JOIN wildstar_client.rewardtrack rt ON rt.ID = c.rewardTrackId
LEFT JOIN wildstar_client.rewardtrackrewards rtr ON rtr.rewardTrackId = rt.ID
WHERE c.rewardTrackId <> 0
ORDER BY c.ID, rtr.ID;
```

Matching map/runtime entrance coverage:

```sql
SELECT m.ID AS matchingGameMapId,
       t.matchTypeEnum,
       t.teamSize,
       t.minLevel,
       t.maxLevel,
       m.worldId,
       m.recommendedItemLevel,
       m.prerequisiteId
FROM wildstar_client.matchinggamemap m
JOIN wildstar_client.matchinggametype t ON t.ID = m.matchingGameTypeId
ORDER BY t.matchTypeEnum, m.worldId, m.ID;
```

Taxi route graph:

```sql
SELECT r.ID,
       r.taxiNodeIdSource,
       src.worldLocation2Id AS sourceLocation,
       r.taxiNodeIdDestination,
       dst.worldLocation2Id AS destinationLocation,
       r.price
FROM wildstar_client.taxiroute r
JOIN wildstar_client.taxinode src ON src.ID = r.taxiNodeIdSource
JOIN wildstar_client.taxinode dst ON dst.ID = r.taxiNodeIdDestination
ORDER BY r.taxiNodeIdSource, r.taxiNodeIdDestination;
```

Housing plug contribution/upkeep:

```sql
SELECT p.ID AS housingPlugItemId,
       p.housingPlotTypeId,
       p.housingContributionInfoId00,
       c0.item2IdTier00,
       c0.contributionPointValueTier00,
       p.housingContributionInfoIdUpkeepCost00,
       upkeep.item2IdTier00 AS upkeepItem2Id,
       upkeep.contributionPointValueTier00 AS upkeepPointValue
FROM wildstar_client.housingplugitem p
LEFT JOIN wildstar_client.housingcontributioninfo c0
       ON c0.ID = p.housingContributionInfoId00
LEFT JOIN wildstar_client.housingcontributioninfo upkeep
       ON upkeep.ID = p.housingContributionInfoIdUpkeepCost00
WHERE p.housingContributionInfoId00 <> 0
   OR p.housingContributionInfoIdUpkeepCost00 <> 0
ORDER BY p.ID;
```

Fortune negative proof check:

```sql
SELECT COLUMN_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'wildstar_client'
  AND TABLE_NAME = 'accountitem'
ORDER BY ORDINAL_POSITION;
```

The expected result has no weight, chance, season, or active rotation column.

## Done States

Each data-backed task should end in one of these states:

- Mapped-only with explicit blocker: table facts are recorded, but behavior is
  not widened.
- Implemented with verification: runtime consumes typed tables or promoted
  runtime-owned data and focused tests/smoke passed.
- Rejected with reason: the table/source was tempting but not valid evidence for
  the feature claim.

This keeps `wildstar_client` useful without letting it become a source of
guesswork.
