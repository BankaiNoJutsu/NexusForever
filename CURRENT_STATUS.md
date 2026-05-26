# NexusForever Feature Restoration - Current Status

Last updated: 2026-05-25 (Rider's Reef hoverboard finish-snap correction; focused finish/effect/opening tests 58/58; F-023 remains partial pending manual client smoke)

Maintained from `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, focused trackers
(`MATCHING_IMPLEMENTATION_STATUS.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`), and
code audits. Update after every implementation pass so it stays the single source
of truth for feature-area completion.

---

## Quick Stats

| Metric | Value |
|--------|-------|
| Total feature areas | 36 (`F-001`..`F-036`; matrix rows `F-016`..`F-020` are five spell-runtime rows) |
| Fully complete | 16 (sections marked **COMPLETE** below) |
| Partial (real handlers/state exist; retail parity incomplete) | 14 |
| Blocked / diagnostic / structural-only | 3 (`F-001` blocked, `F-002` diagnostic, `F-003` structurally closed) |
| Consolidated runtime gaps (emit/producer proof) | 5 rows in **Remaining Blocked Items** |
| Related trackers | `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `MATCHING_IMPLEMENTATION_STATUS.md`, `ENTITY_AUX_DECODE_ROADMAP.md`, `BLOCKER_EVIDENCE_PLAN.md` |
| Build | 0 errors, 0 warnings |
| Tests | 1036 passed, 0 failed, 0 skipped |
| Handlers surveyed | 200+ |
| Stubs found | 0 |
| **Crafting review (2026-05-23)** | Speculative discovery, station, and charge mutations rejected; fixed-recipe success remains evidence-backed |
| **Ghidra decomp evidence (2026-05-23 pass 3)** | Consumer dispatch found at WorldSocket_ProcessServerMessage; entity-create aux verified emitted by EntityCreateAuxiliaryPacketBuilder; 44,991 xrefs created |
| **Addon corpus audit (2026-05-23)** | 876 addons scanned; map tracked/threat/crafting/housing strings confirmed, but only threat list is already implementation-backed |

### Status legend (how to read **COMPLETE** vs **PARTIAL**)

| Label | Meaning |
| --- | --- |
| **COMPLETE** | Client handlers and a working server/runtime path exist with focused tests; known retail gaps may still be listed in the section or in **Remaining Blocked Items**. |
| **PARTIAL** | Substantial behavior exists, but retail parity, producer semantics, or persistence edges remain open. |
| **BLOCKED** | Evidence gate prevents widening behavior (e.g. F-001 STS crypto). |
| **DIAGNOSTIC** | Safe parse/log only (F-002). |
| **STRUCTURALLY CLOSED** | All server opcodes have named models (F-003); emitters are feature-owned. |

`Decomp/Analysis/GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` uses **Partial** for the same rows when retail parity is still incomplete even if handlers exist — prefer this file for the 36-row completion snapshot.

---

## Code Review Backlog - Fixes Applied 2026-05-23

From `Decomp/Analysis/CODE_REVIEW_BACKLOG_2026-05-23.md` (43 items).

### P0 - Fixed / Verified

| # | Item | Fix |
|---|------|-----|
| 1 | Loot bag consumed before delivery | Delivery is preflighted before consumption; successful use consumes the bag before granting loot, avoiding both destroyed rewards and duplicate grants. |
| 2 | ServerSupportSurveyList 5th uint32 | **Verified correct**: Ghidra decomp (0x1400a4260) confirms 5 uint32s where 5th is row count. Wire format matches. |
| 3 | Q3487Shellshock GrantAchievement throw | Added `HasCompletedAchievement` guard before `GrantAchievement(1296)`. |

### P1 - Fixed / Partially Closed

| # | Item | Fix |
|---|------|-----|
| 4 | Group loot solo fallback | Group context now created when sameMapMembers > 1 even if all out of range. |
| 5 | Killer force-added out of range | Removed force-add block; eligibility logic handles it naturally now. |
| 6 | AssignMasterLoot offline winner | Returns false + warning when assignee is offline. |
| 7 | FinaliseRoll offline winner | Logs and keeps the item assigned for deferred delivery if the winner reconnects before corpse expiry. |
| 11 | DeliverAllLoot partial success | Added `failedCount` tracking and warning log. |
| 18 | Duel "leash" comment | Corrected to "inter-duelist distance limit" (matches code checking 120m between players). |
| 19 | Cancel/timeout sets winner/loser | `ServerDuelResult` now sends 0/0 for Cancelled and DeclinedRequest reasons. |
| 20 | No disconnect hook | `OnPlayerDisconnect` added to `IDuelManager` + `DuelManager`; called from `WorldSession.OnDisconnect`. |
| 21 | MemberIndex never set | `ServerGroupMemberFlagsChanged` and `ServerGroupMemberRoleChange` now set `MemberIndex` from `GroupIndex`. |
| 22 | Ready-check clear stale UI | `HasReadyCheckFlags` guard removed; `ServerGroupReadyCheckStatusUpdate` sent unconditionally. |
| 26 | Housing edit mode stub | Now validates residence permission via `CanModifyResidence`; server ack/state semantics remain unmapped. |
| 27 | TargetPlayerIdentity ignored | Now resolved via `GetResidenceByOwner` + permission check, matching sibling handlers. |
| 28 | Cannon no quest 3487 gate | `OnActivateSuccess` now checks `GetQuestState(3487) == Accepted` before crediting objective. |
| 29 | Map spawn fallback missing | `NorthernWildsMapScript` now spawns cannon (11251) + ultrabot (12526) with DB fallback checks. |
| 30 | Q3667 objective 4770 unverified | Added comment confirming verification against Quest2.tbl. |
| 31 | Manual GrantNext() boilerplate | Q3479, Q3667, Q3886, Q3487 refactored to `FollowUpQuestScript<T>` base class. |
| 41 | PublishAsync missing FireAndForget | Added `.FireAndForgetAsync()` to `ClientGroupFlagsChangedHandler`. |
| 43 | TutorialMapInfo/Position names | Renamed to `NorthernWildsMapInfo`/`NorthernWildsMapPosition`. |

### P2 - Fixed / Documented

| # | Item | Fix |
|---|------|-----|
| 12 | DB null no-op silent divergence | Added `log.Warn` in `Initialise()` and `Persist()` when DB context is null. |
| 13 | Corrupt DB row crashes startup | Wrapped `uint.Parse` in try-catch; logs error and skips corrupt row. |
| 14 | SearchAuctions materializes all | Restructured: filter, sort, page via Skip/Take, then CloneAuction only for current page. |
| 15 | O(n^2) commodity matching | Added XML doc documenting O(n^2) with note about price-indexed refactor. |
| 16 | Persist deadlock risk | Changed `.GetAwaiter().GetResult()` to `.ConfigureAwait(false).GetAwaiter().GetResult()`. |
| 17 | Null item skipped silently | Added `log.Debug` when skipping auctions with `model.Item == null`. |
| 32 | Field-level tests for 0x034F | Added `ServerSupportUInt32AndFlags_WritesValueFlagsAndPadding()` test. |
| 33 | Test placement | Left in SupportPacketShapeTests - no dedicated realm shape test file. |
| 36 | Server0x08CC duplication | Inherits from `ServerUInt32WideStringPayload`, removing ~10 lines of duplicated code. |
| 37 | RowCount property confusing | Added XML doc: "for diagnostic / log use only." |
| 39 | lootInstances no lock | Added XML doc documenting world-thread-only mutation assumption. |
| 40 | NextLootId unsynchronized | Added XML doc documenting world-tick-only allocation assumption. |
| 42 | Quest script xUnit tests | Created `QuestScriptConstructionTests.cs` with DI smoke test for FollowUpQuestScript<T>. |

### P2 - Deferred (7 items)

| # | Item | Reason |
|---|------|--------|
| 8-9 | Loot linear scans + tick walks | Performance optimization; needs profiling. |
| 10 | Duplicate aggregation logic | Refactor deferred. |
| 34 | Consolidate triplet row types | Refactor deferred; significant refactor across spell/crafting/realm. |
| 35 | Consolidate counted triplet lists | Refactor deferred; coupled with #34. |
| 38 | Opcode/docs naming drift | Documentation polish deferred. |

## Ghidra Decomp Evidence (2026-05-23, 2400-function run)

### Crafting - Station Semantics / Discovery Diagnostics (F-008)
- **`Crafting_HandleServerCraftingFinish`** (0x1405e6690):
  - `CraftingDiscovery` field at `param_2[4]`. Value `3` = Success. All other values dispatch `CraftingDiscoveryHotCold` client event.
  - `param_2[5]` = discovery value sent with the event.
  - On craft completion, discovery state resets: `DiscoveryVectorMultiplier=1.0f`, `DiscoveryRadiusMultiplier=1.0f`, position/unknown fields zeroed.
  - NexusForever keeps complex-craft `CraftStats`, `ApSpSplitDelta`, and `ChargeCounts` diagnostic-only for server behavior. Focused coverage now pins that non-zero complex-craft stat/charge payloads still follow fixed-recipe completion and emit `CraftingDiscovery.Success`/`CraftingDirection.None`.
  - Addon scan confirmed `CraftingLib.CodeEnumCraftingDiscoveryHotCold` usage in Athena, and `Lua_Crafting_AddCoordinateDiscoveryInfo` maps UI distance-band fields, but neither proves authoritative server-side coordinate packing, hot/cold emit timing, or discovery-unlock mutation.

- **`Crafting_SendClientComplexOrSimpleCraft`** (0x140399780):
  - Resolves `CraftingStationUnitId` and schematic, validates via `SpellCast_ResolveTargetsAndValidate`.
  - Sends opcode 0x084F (complex) or 0x0850 (simple) depending on schematic flags.
  - `Crafting_GetStationServiceKeyForSchematic` (0x1405926a0) maps `TradeskillSchematic2` to numeric station service keys `0x4F` for `TradeSkillId=22`, `0x57` for tier-zero `Flags & 0x04`, and `0x2C` otherwise. `Crafting_FindStationUnitForServiceKey` (0x1403a0d20) resolves that service key through the current context tree and returns the station unit id, or 0 when no key exists. Native service-key names remain unmapped; server validation continues to use `Creature2.TradeSkillIdStation`, not service-key integers.
  - Server now validates forged non-zero `CraftingStationUnitId` values against the player's map and `Creature2.TradeSkillIdStation`, while preserving the observed client ability to send 0 from the simple/complex sender.

- **`Crafting_SendClientCraftingAdditive`** (0x14059b7c0):
  - Sends opcode 0x084A with station unit ID + additive/catalyst Item2 IDs.
  - Validates additive count and schematic match.
  - Native sender requires a non-zero station unit before emitting 0x084A; server additive handling now rejects zero, unknown, or non-station unit ids.

- **`Lua_RegisterItemDataBindings`** (0x140413a20):
  - Registers `CodeEnumRuneType` through `Lua_RegisterCodeEnumValue` (0x1400eff50).
  - Native rune values are pinned as `Air=7`, `Water=8`, `Earth=9`, `Fire=10`, `Logic=11`, `Life=12`, `Fusion=13`, matching `RuneType`.
  - `Lua_GameItemData_GetRuneSlots` (0x14041b8d0) now maps through `ItemData_AddRuneSlotsLuaFields` (0x140673b80): live item data reads slot type bytes at `itemData+0x388`, converts compact values 1..7 through `ItemRuneSlotType_ToRuneType` (0x140514660), and reads installed rune Item2 ids at `itemData+0x518 + index*4`.
  - This closes the rune-type enum question and narrows durable rune state. Persistence/network mutation remains blocked until the producer bridge from shared item `RandomGlyphData`/`Glyphs` into those live offsets and database-owned item fields is mapped.

### Housing - Neighbor Lua Table (F-004)
- **`Housing_BuildNeighborLuaTable`** (0x1404b4e40):
  - Builds per-neighbor Lua row with fields: `nId`, `nClassId`, `nPathId`, `fLastOnline`, `strRealmName`, `strCharacterName`, `strWorldZone`, `nLevel`, `nFactionId`, `ePermissionNeighbor`.
  - Called when consuming `ServerHousingNeighbors` (0x0507) and likely `ServerHousingNeighborhoodEntry` (0x0501) / `ServerHousingNeighborhoodList` (0x0506).
  - Addon scan confirmed NeighborNotes uses `ICCommLib.JoinChannel("NeighborShare")` for peer-to-peer share/search data, so that addon does not unblock the server neighborhood trigger path.

### Map Tracked Unit / Threat Addon Follow-up
- Map addon evidence (`GuardZoneMap`, `LUI_ZoneMap`, `RavenMap`) confirms Lua consumers for `MapTrackedUnitUpdate`, `MapTrackedUnitDisable`, and `GetMapTrackedUnitData(id)`, but the server opcodes are `0x0849`/`0x0848`; `0x0264` is the unrelated `ServerEntityCreateAuxScalarList`.
- Producer behavior remains blocked for map tracked units: tracked-unit id allocation, update cadence, disable lifetime, and `TrackingSlotId` selection are not proven.
- Threat addon evidence confirms `TargetThreatListUpdated` is a real UI event; this is already backed by mapped `ServerEntityThreatListUpdate` (`0x0909`) and `ThreatManager.BroadcastThreatList()`.

### Remaining Decomp Targets
- **Entity aux consumers** (F-025): Need `Network_RegisterServerOpcode_0351` (0x14006c290, 56KB) to map opcodes to consumer handlers.
- **PvP cooldown consumer** (F-015): Consumer for `ServerPvpCooldownUpdate`; address unknown.
- **Item context action / Pet stance** (F-026): Consumer addresses unknown.
- **ServerRaidQueueStatus** (F-010): reader `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` mapped; zero-value compatibility emit exists from raid-info; non-zero queue semantics still blocked.

### F-001 - STS Token Crypto - BLOCKED
Token crypto handshake, external-account edge routes, and optional envelope
fields remain blocked until crypto/token semantics are mapped safely.

### F-002 - Client Diagnostic Opcodes - DIAGNOSTIC
17 models with diagnostic handlers exist. Every packet's wire shape is pinned
by focused tests. Client request intent and server response behavior unknown.
Needs Ghidra decomp of client writer functions by feature cluster.

### F-003 - Server Unresolved Output Opcodes - STRUCTURALLY CLOSED
0 `Server0xNNNN` enum/model placeholders remain in `Source/`. All 699 server
opcodes have named models, with 62 shape-mapped aux/spell packets still gated
behind consumer or producer evidence before any production emitters are added.
Named-but-partial packets remain tracked in their feature rows.

### F-004 - Housing - PARTIAL (21/22 handlers complete)
**Implemented:** Residence create (level-14 gate), neighbor persistence,
harvest splitting, direct visits, community rename/placement/privacy/removal,
return handler, vendor list, decor/plug/remodel/flags delegation.

**Fixed this pass:** `ClientHousingEditModeHandler` - was empty-body stub,
now validates residence map + logs.

**Blocked:** `ServerHousingNeighborhoodEntry` (0x0501) and
`ServerHousingNeighborhoodList` (0x0506) - packet models exist but nobody
sends them. Unknown client request trigger (not accessible via addon API).
`NeighbourhoodId` in `ServerHousingBasics` hardcoded to 0.

### F-005 - Marketplace / Auction / Commodity / CREDD - COMPLETE
**All 15 client messages have complete handlers.** Auction post/search/buyout/bid,
commodity post/cancel/buy/sell matching, CREDD exchange/history/redeem.
DB persistence for auctions, commodity orders, CREDD orders, CREDD history.
Slot limits: Free 3 / Signature 30. Offline marketplace credits via mail.

**Fixed this pass:** Commodity order expiration - `ProcessExpiredCommodityOrders()`
scans every 1s, refunds/returns expired orders, sends `ServerCommodityAuctionRemoved`.

### F-006 - Storefront / Account Inventory - COMPLETE
Catalog, purchase, account currency charge, claim/return, pending item groups,
daily login, coupon redemption, VC packages, wallet updates, purchase history,
privilege restriction, purchase-velocity gate (10/hr), CREDD redeem (1000:1).
54 focused packet tests. All `0969..0991` opcodes mapped.

### F-007 - Reward Rotation - PARTIAL
Game-table refresh emits (`0x07CA` schedule, `0x07CD`/`0x07D3` content context),
`AccountRewardRotationGrantManager` + `account_reward_rotation_grant` persistence,
claim path via `ClientRewardUpdateRequest`, and non-empty `0x07C8` entry-state on
refresh/claim are implemented with focused tests.

**Blocked:** per-content authoritative reward mapping precision, `0x07CD` apply/`Flag`
consumer semantics, and full schedule filter parity (player level/difficulty).

### F-008 - Crafting / Tradeskill - PARTIAL (12/12 handlers functional)
**All 12 client handlers complete** with proper request/response cycles.
Fixed-recipe crafting, supply satchel, tradeskill lifecycle, schematic
learning, profession modifiers, rune slot management, rune fusion (add,
clear, install, reroll - all 4 operations send `ServerTradeskillSigilResult`).

**Remaining gaps:**
- Complex craft stats (CraftStats, ApSpSplitDelta, ChargeCounts discarded)
- Discovery attempt coordinate packing, hot/cold emit timing, and unlock mutation remain blocked; validate with an F-008 live capture before enabling non-success discovery results
- Crafting station request semantics are implemented; native service-key names remain diagnostic-only
- 0x084B/0x0855 emit intent (corrected 24-byte and 12-byte payload shapes modeled, never sent)
- Rune item data semantics

### F-009 - Rapid Transport / Taxi / Flight Path - PARTIAL (blocked)
Pricing/request validation partial. Service-token bypass, route state, taxi
embark/completion, charge/teleport rules incomplete. Needs decomp.

### F-010 - Group / Raid / Matching - PARTIAL (replacement backfill remains blocked)
All 13 group client handlers, 15 internal group handlers, 14 matching handlers,
raid info, quest share, instance settings - all fully functional.

**Validation/logging only (not full LFR backfill):**
- `ClientMatchingMatchInitiateLookingForReplacementsHandler` / `ClientMatchingStopLookingForReplacementsHandler` validate in-progress match membership and native role mask `0..2`; they do not start a server replacement queue
- Removed the unsupported replacement anchor queue/merge runtime; addon and sender
  evidence prove the UI/event boundary, not server producer timing or merge rules
- `ServerRaidQueueStatus` (0x0718) emitted alongside `ServerRaidInfoResponse` as zero-value compatibility state

**Ghidra decomp confirmed:** All 6 group reader functions decompiled.
9 of 10 unresolved opcodes already correctly wired:
- 0x0414 ServerGroupInstanceDifficultyResponse [ok]
- 0x042A ServerGroupKickResult [ok]
- 0x0431 ServerGroupLootRuleValidationResult [ok]
- 0x0436 ServerGroupRosterUpdate [ok]
- 0x0438 ServerGroupMemberRoleChange [ok]
- 0x0441 ServerGroupReadyCheckStatusUpdate [ok]
- 0x045A ServerGroupRequestJoinWindow [ok]
- 0x0461 ServerQuestShareResult [ok]
- 0x0468 ServerGroupMemberDetailUpdate [ok]

**Blocked:** exact `ServerRaidQueueStatus` queue-position semantics and timing;
replacement queue fill still needs live accept/teleport smoke and multi-slot
role-fill verification.

### F-011 - Guild / Recruitment / War Party - PARTIAL (blocked)
Guild handlers and recruitment output models exist. Bank transactions, perks,
holomarks, standards, recruitment subscriptions, boss-token inventory,
warplot plug state incomplete. Needs decomp.

### F-012 - ICComm / Chat / Friendship - PARTIAL (blocked)
Transient membership, offline cleanup, directed/ordered delivery. Entitlement
checks, persistent channels, throttling, auto-response persistence incomplete.
Needs decomp.

### F-013 - Mail - COMPLETE
Cash collection, return, attachment deletion, delayed pending-mail promotion,
COD sender settlement, delete result/unavailable signaling, expiration sweep.

### F-014 - Loot - COMPLETE
Basic delivery, group roll/master-loot runtime, roll/assign/result packets,
`ServerLootNotify` ingestion, `ServerLootBindOnPickup`, `ServerLootWinner`.
**Improved this pass:** Loot expiry/cleanup in GlobalLootManager + LootInstance;
account-currency granted notifies now use one real reward row instead of split
fake shower rows, generated granted-notify rewards now batch all actual
delivered rows into one explosion notify, delivered account-currency loot emits
a local generic floater plus a loot-channel chat line for the actual amount and
currency granted,
delivered character-currency/cash loot emits the same readable floater and
loot-channel chat feedback while preserving the existing currency update path,
delivered static-item loot emits a loot-channel item-link chat line to the
winning looter,
generated crafting loot can carry the crafting station as a distinct
`ParentUnitId` visual source, and runtime tests cover distinct parent propagation
plus quality-preserving granted static, virtual, and account-item drop
presentation; loot inspect/capture diagnostics now include the table-backed loot
visual effect id for the resolved quality.

### F-015 - PvP / Duels - COMPLETE
**Fixed this pass:** Duel leash/distance system added.
- `DuelLeashRange = 120f` - checks distance each update
- `DuelLeashTimeout = 15s` - auto-cancels duel
- `ServerDuelLeftArea` sent when out of range
- `ServerDuelCancelWarning` sent on re-entry

PvP cooldown timer (`ServerPvpCooldownUpdate`) sent on flag toggle off
with `RetailCertainRules.PvpFlagCooldownMs`.

### F-016-F-020 - Spell Runtime - PARTIAL (family-by-family)
Damage/heal/shields/vitals, stack groups/buffs/CC/movement, procs, summons,
RavelSignal. Many families conservative or partial. Work family-by-family
with fixture captures. Most blocked on decomp.
**AI movement this pass:** Rider's Reef `CombatAI` now resolves spell/range
profiles through `ICombatProfileProvider` with the known starter combat
Creature2-to-profile mappings loaded from the tracked embedded
`AI/CombatProfiles.json` asset, including the default fallback auto-attack,
aggro-spell, and chase-distance kit for default-enabled derived scripts, keeps
sampled `Creature2Action` rows
diagnostic-only, and adds bounded same-faction social assist with focused combat
tests. Profiles can now define cooldown-gated special attacks that cast
ordinary `Spell4` rows through the existing spell runtime while honoring
primary-target minimum range, hit-radius-aware effective maximum range, and
vertical range; failed spell runtime starts do not consume the AI cooldown, and
non-instant `CastTime` rows stop chase movement/auto-attacks during the windup,
and `CCState.Interrupt` cancels active casts and clears the AI windup lockout so
profiled NPCs can resume after a proven interrupt. This allows profiled NPC
windups/telegraphs when the chosen spell data supports them. Patrolling
creatures now walk back to leash before relaunching their data-backed patrol
spline, including the existing reverse-speed spline normalisation. Server follow
movement now applies a small deterministic
follower-GUID spread around known targets, so multiple chasers choose distinct
final points at the requested distance instead of collapsing onto one endpoint;
this is emulator-side local avoidance polish, not retail parity proof.

### F-021 - Action Set / LAS / AMP / Attributes - PARTIAL (blocked)
LAS size/spec/tier/AMP preflight checks exist. `UpdateSpellInProgress`,
async spell update transaction, authoritative attribute allocation,
action-bar lock state incomplete. Needs decomp.

### F-022 - Quests / Path Missions / Public Events - PARTIAL
**Coverage audit:** see `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`
(5,194 client quests; no current unsupported objective-type blockers in client
data; 56 hand-curated script rows).
**NorthernWilds Veteran pass updated this pass.** Core quest/objective,
public-event, challenge, path packet, and opt-in spawn-promotion surfaces exist,
but full 164-NPC parity is still blocked by 3 enabled Jabbithole creatures with
unresolved Creature2 bridges.

**Build 16042 start-zone classification:** Novice creation starts both factions
in Rider's Reef (`world 3460`). Veteran creation skips to the surface starter
zones: Exile Human/Granok to Northern Wilds (`426`), Exile Aurin/Mordesh to
Everstar Grove (`990`), Dominion Chua/Draken to Crimson Isle (`870`), and
Dominion Cassian/Mechari to Levian Bay (`1387`). Gambler's Ruin and Destiny are
older faction arkship tutorial zones and should not displace Rider's Reef for
16042 restoration unless a task explicitly targets historical/pre-16042 parity.

**Tutorial (Rider's Reef - world 3460):** PARTIAL / NOT COMPLETE
```
Exile:   10513->10527->10518->10525->10540->10519->10520->10528
Dominion: 10521->10532->10524->10526->10541->10522->10523->10530
```
All 12 quest scripts are present and recent focused regressions cover the
tutorial communicator checkpoint sequence, build 16042 character-create start
rows, tutorial quest follow-up grants, terminal-to-world routing plus destination
welcome-quest grants, final quest completion before visible surface receiver,
duplicate welcome-quest guards, no welcome grant when teleport is denied,
terminal recording on activation, per-player terminal
storage, known-terminal filtering, cross-faction/missing terminal default
routing, fallback-spawned departure terminals/checklist interactables,
both-faction starter quest grants and follow-up re-entry root-skip behavior,
logout/re-entry recovery boundary excluding the final departure quests from
auto-completion,
table-backed reward coverage for cryopod Omnibit and final-departure item
rewards,
combat-projector quest delta recovery, map-cache
fallback anchoring, delta-only tutorial quest initialisation, mine/combat lane behavior, and
faction-specific projector text. The hoverboard presentation/recovery pass now
casts table-backed ring lightning, sprint/trail, and booster spells
(`82460`, `82298`, `85424`), no longer casts the wrong `81662` scan on
opening step-pad objective completion, keeps a narrow `81662`
`CCState.Disable` suppression as a safety guard, starts the `82298` trail visual
when the hoverboard projector objective grants the board, covers ring/booster
effects when the range-entering entity is the mounted vehicle and the pilot
must receive the spell, strengthens booster velocity, snaps padded finish
recovery to
`WorldLocation2 51734`, and remounts through `85562` after relog while the ride
objective is still incomplete; the finish snap also runs when normal
area-objective sync already completed the ride objective before recovery checks
the finish location. A follow-up projector/ring correction keeps `84387` mapped-only
pending its exact retail producer, narrows ring contact to `82460` only, and makes
direct/cast projector activation fall back to hoverboard equip `85562` if the
activate spell path is blocked. Rider's Reef is still not complete until Exile
and Dominion client playthroughs verify quest acceptance/turn-in UI, kill loops,
rewards, respawn/re-entry, CSI, hoverboard and projector reliability, final
terminal/checklist flow, and VO timing.

**NorthernWilds (world 426):** CORRECTED - 3 independent chains:
```
Chain A: 3479/3480 -> 3667 -> 3486 -> 3671 -> 3668  (+3797 side)
Chain B: 3487 -> 3963                                (independent)
Chain C: 3886 -> 3673 -> 3670                         (independent)
```
**Created this pass:** Q3479, Q3886, Q3797, Q3963 scripts.
**Fixed this pass:** Q3667 quest script added, Q3487 chain grant to Q3963.
Focused regressions now pin the Q3479/Q3480 -> Q3667, Q3667 -> Q3486,
Q3486 -> Q3671/Q3797 mention/cinematic, Q3671 -> Q3668, Q3487 -> Q3963,
Q3886 -> Q3673, and Q3673 -> Q3670 cinematic/follow-up boundaries. Entity
regressions also pin Q3667 control-panel objective `4770` and Q3487 cannon
checklist objective credit from table-backed creature/checklist data.

2026-05-25 Veteran Northern Wilds follow-up:

- The 20-quest local block is now curated through `4696`: added scripts for
  `3741`, `3777`, `3781`, `3783`, `4526`, `4666`, `4667`, and `4696`, with
  Jabbithole/client objective evidence for supply crates, Loftite crystals, and
  captive soldiers.
- Public event `154` Dominion Ultrabot now starts for players in Camp Icefury
  and credits objective `371` when creature `12526` dies.
- Combat challenge progress now resolves direct and nested target groups, which
  covers Northern Wilds `103`/`105`; `ChallengeRequirement` prerequisites now
  compare challenge completion count instead of logging unhandled.
- The 13 Northern Wilds path missions activate for the player's active path
  episode with Jabbithole XP `25`; Explorer progress/power-map reports can
  complete active missions, Soldier tower-defense build packets complete
  matching active event missions, Soldier holdout control-point activations
  now complete Northern Wilds Soldier missions `33`, `34`, and `156`, and
  Settler build-tier packets complete matching active hub missions. Scientist mission-creature mappings from
  Jabbithole now drive scan metadata and activation-completion hooks for
  `42`, `160`, and `648` (`18680`, `15888`, and the mapped Skeech Creature2
  IDs). Generic Soldier wave simulation and the generic Scientist scan-result
  packet remain partial pending stronger packet/result evidence.
- Jabbithole zone `1` has `164` enabled creatures; `161` now bridge to
  Creature2, with `3` still unmatched (`Ability Training Kiosk`, `Invisible
  Channel Unit`, and `Unknown`). `Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql`
  now has an opt-in entity-spawn promotion path (`1000000000 + source_coordinate_id`
  IDs, per-world/per-area filters, per-spawn `entity_stats`, reviewed
  exact-name ambiguous bridge opt-in, and targeted direct Jabbithole coordinate
  fallback for rows whose source `worldid` is blank). Local verification promoted
  `4,375` mapped entity rows / `15,423` stat rows and raised Northern Wilds
  runtime coverage from `90` to `161` enabled Jabbithole creatures in world
  `426`. Reviewed local bridge aliases were `1055` -> `11705`, `3351` ->
  `25876`, and `11313` -> `18680`. The remaining `3` require stronger Creature2
  evidence before runtime promotion. This is the accepted safe stop boundary for
  the Veteran Northern Wilds pass: do not force-map those three rows without new
  table, packet, or live-client evidence.

**CrimsonIsle (world 870):** CORRECTED - definitive chain from Jabbithole `quest2.pre_quest0/01/02`:
```
Roots:  5595 -> 5575 -.
        5593 -> 5573 -+-> 5596 -> 5597 -> 5604 -> 5580 -.
        5593 -> 8855  (parallel branch)       5604 -> 5583 -+-> 5594
Standalone: 5584, 5610 (type 11)
```
**Created/covered in this quest pass:** Q5593, Q5596, Q5597, Q5583 scripts and
the shared CrimsonIsleQuestChain helper.
**Fixed this pass:** Q5593 grants 5573/8855; Q5573/Q5575 grant Q5596 through
the table-backed OR prerequisite gate (`preq_flags = 1`); Q5604 grants both
Q5580 and Q5583; Q5580/Q5583 only grant Q5594 after both table-backed
prerequisites are complete; Q5594 warbot kill now guards already-completed
achievement 1730 while still crediting objective 8249; focused quest-chain
regressions cover the Q5595 -> Q5575, Q5596 -> Q5597, Q5597 -> Q5604 direct
follow-ups, these gates, final-warbot credit, and Q8855 Dominion soldier
activation/despawn objective credit.

### F-023 - NPE / Rider's Reef - PARTIAL
Recent tutorial/final-departure, mine/loot, hoverboard, combat-projector,
map-cache, communicator checkpoint, character-create start-row, quest-chain,
spell/effect terminal activation, terminal routing/welcome-quest, and
direct activation checklist credit for the actual departure checklist
interactables, final quest completion before visible surface receiver, plus
cross-faction/missing terminal fallback, both-faction starter grants,
follow-up re-entry root-skip behavior, logout/re-entry recovery boundary,
table-backed reward coverage, and
terminal/checklist fallback-spawn fixes are built and test-covered. The timestamped
evidence matrix is `Decomp/Analysis/coverage/RIDERS_REEF_EVIDENCE_MATRIX_2026-05-25.md`.
Rider's Reef is **not complete**. Manual client playthrough validation remains
for acceptance, turn-in, kill loops, rewards, respawn/re-entry, CSI,
hoverboard/projector reliability, final terminal/checklist flow, all four
terminal handoffs, and retail VO overlap/timing. Completion means the full build
16042 Rider's Reef Novice flow and terminal handoff into Everstar Grove/Northern
Wilds or Crimson Isle/Levian Bay; Gambler's Ruin and Destiny are older arkship
context, not the active 16042 Novice target. See F-022 Tutorial section.
The local Auth/World restart check on 2026-05-25 succeeded twice without
world-log reload/error/stack-overflow matches; the latest fresh PIDs were
`30632`/`43212` with ports `24000` and `23115` reachable. The later initial
quest chain correction makes fresh Rider's Reef entry grant only `10513`/`10521`
and recover `10527`/`10532` only after the movement root is completed. Focused
Rider's Reef regressions passed at 79/79 after the correction; the narrow
map/chain slice passed 17/17. A follow-up live UI correction now avoids sending
a mid-play `ServerQuestInit` refresh when the movement root advances into
`10527`/`10532`; the flow relies on the normal quest state delta while
`QuestManager` still suppresses the completed movement root during login
snapshots when the paired hoverboard quest is active. This keeps the opening
"three points -> platform -> projector" beat to one client-visible tutorial
chain; focused quest/map/chain/projector tests passed 35/35 and the broader
`FullyQualifiedName~Tutorial` filter passed 74/74. A later hoverboard presentation/recovery pass
added table-backed ring/booster visual casts, removed the wrong
opening step-pad `81662` scan cast, kept only the narrow scan `Disable` CC
safety suppression, added projector-completion trail start, mounted-vehicle
ring/booster pilot coverage, stronger boost velocity, finish snap to `51734`,
and direct hoverboard remount recovery for disconnects mid-ride; a later
finish-snap correction covers the case where the ride objective was already
complete before recovery runs. A follow-up projector/ring correction narrowed
ring contact to the character-side `82460` lightning/objective FX, kept `84387`
mapped-only pending exact producer evidence, and made direct/cast projector
activation fall back through hoverboard equip `85562`. Focused finish/effect/opening tests passed 58/58
after rebuild, the ring Power Boost remap filter passed 23/23, the
projector/ring correction filter passed 11/11, the broader correction slice
passed 82/82, and the rebuilt tutorial filter passed 78/78; the
combat-mine telegraph follow-up now keys the
danger-zone warning window off `Spell4.CastTime` (`85430`/`85629`/`85630`) before
falling back to `SpellDuration`, with focused Start-vs-Go/Finish regression
coverage. An earlier broader `FullyQualifiedName~Tutorial` filter passed 66/66.
No full native client
Exile/Dominion playthrough has been completed from this API context. A
read-only character DB/log audit found only partial early NPE evidence: 28 local
characters were still on `worldId=3460`, one local character was on Northern
Wilds `426` at the Human/Granok Veteran start coordinates, and no persisted
final Rider's Reef (`10528`/`10530`) or destination welcome quest
(`9112`/`9113`/`9126`/`9127`) rows were present.

**DataMapping safe import bridge (2026-05-25):** the safe staging-to-runtime SQL
now includes the Rider's Reef world `3460` cleanup for tutorial combat rows:
delete stray `73665` Beacon Arrow entities and normalize imported turret
creatures `73494`/`74862` to runtime `NonPlayer` rows with combat factions
`1441`/`1442`. Local verification loaded `95/95` `nf_map_*` staging tables,
applied the safe import, and reported zero Beacon Arrow rows, zero wrong-faction
turrets, and zero wrong-type turrets.

### F-024 - Evil from the Ether (world 3404) - COMPLETE
612-line event script, 27 phases with full state machine, 11 entity scripts.
Dynamic entity spawning, door management, cinematic queuing, communicator
messages, combat AI, spline movement.

### F-025 - Entity Create/Update/Visibility/Interaction - PARTIAL

**Implemented:** Core entity create/update works. Entity-create aux packets
(0x025F-0x0264) emitted via `EntityCreateAuxiliaryPacketBuilder.BuildPreCreatePackets()`
before `ServerEntityCreate` with entity GUID, type, faction, prop ID, controller GUID,
socket ID. Busy target and interaction gates partial. `ShowRealmBank` (interaction 67)
loads realm bank items. CSI objective crediting covered by focused regression tests.
Threat list via `ThreatManager.BroadcastThreatList()`.

**Emitted:** entity-create auxiliary opcodes `0x025F`..`0x0264` via
`EntityCreateAuxiliaryPacketBuilder.BuildPreCreatePackets()` from
`Player.AddVisible()` before `ServerEntityCreate`.

**Blocked:** 6 entity-stat auxiliary opcodes (0x0889, 0x08CC, 0x08F4, 0x0939,
0x093D, 0x093E) - wire shapes modeled, Ghidra reader labels exist and
field-level decomp verified against C# models. No production emit sites in
`NexusForever.Game` / `WorldServer` (packet-shape tests only).
Consumer-handler dispatch semantics and map-tracked-unit producer timing remain
blocked (indirect calls through the function pointer registration table and no
server-side tracked-id/TrackingSlot selection proof).

### F-026 - Items / Unlocks / Costumes / Pets / Titles - PARTIAL
Inventory, title, pet, costume, unlock surfaces exist. Generic unlock
lifecycle tested. Costume forget/unlock parity exists.

**Blocked:** `ClientItemContextActionHandler` validates/logs the item but keeps
`SelectedBranch` diagnostic-only until right-click branch semantics are mapped.
Pet stance behavior remains limited to the existing pet stance state.

### F-027 - Options / Keybindings - COMPLETE
6 handlers, account+character keybinding split, casting options, combat
log preferences. Zero stubs.

### F-028 - Support / Reports / Surveys / Stuck - COMPLETE
5 support handlers + stuck handler. Stuck cooldowns: RecallBind 30m,
RecallHouse 30m, Death 30m. JSONL submission store. Survey parsing.

### F-029 - Client DB / Data Mapping - COMPLETE
Table registration pattern mapped, DataMapping exists. Field renames
gated on packet/data contract + migration proof.

**Verified safe import pass (2026-05-25):** smoke mapping completed against
`Tools\DataMapping\output_test`; full staging reload from `Tools\DataMapping\output`
loaded all `95` `nf_map_*` tables; `verify_safe_world_imports.sql` passed for
vendor, loot, creature-info, item-container, and Rider's Reef world-row cleanup
metrics without adding any runtime dependency on staging/reference tables.

### F-030 - Realm / Character Select/List/Transfer - COMPLETE
Character list/select works. Realm transfer returns compatibility list.
Current-realm select ignored for client safety. Offline target returns
ServerDown.

### F-031 - Fortune Minigame - COMPLETE
4 handlers functional, session persistence code path via `account_fortune_session`,
reward payouts with account item grants, card flip state tracking.
Weighted emulator rarity-tier pool + `ServerFortuneRewards.RewardItemProbabilities`
match the mapped retail client UI transport (`FortunesLib.GetFortunesLootList`
reads server floats and shows `fProbability = value * 100`; native evidence
2026-05-23). `Decomp/Analysis/FORTUNE_WEIGHT_AUDIT.md` verifies the local
catalog/table state and current Fortune tests.
**Blocked:** exact per-item retail weights and active rotation catalog (no
`AccountItem.tbl` weight column; no retail `ServerFortuneRewards` or
storefront-server catalog capture).

### F-032 - Leaderboards - COMPLETE
Real database-backed pipeline (NOT empty stub). `DatabaseLeaderboardStore`
with MySQL, 800 rows per table, caching, ranking, personal placement,
score ingestion. `LeaderboardAggregation` for wire encoding.

### F-033 - Challenges - COMPLETE
609-line `ChallengeManager`. 4 choice types: Activate, Abandon, AcceptShared,
DeclineShared. Full lifecycle: activation, tier advancement (3 tiers),
completion, sharing (30s timeout). DB persistence.

### F-034 - Datacubes / Journals / Galactic Archive - COMPLETE
298-line `GalacticArchiveManager`. Unlock, view, rule-based auto-unlock
(achievement/path/quest rules with ALL/ANY flag). DB persistence.
`ServerGalacticArchiveRefresh` + `ServerGalacticArchiveUpdate`.

### F-035 - Achievements - COMPLETE
Generic base manager with checklist/value progress tracking, prerequisite
checks, completion marking. Realm-first tracking (character + guild).
Guild achievement forwarding. Title grants on completion.

### F-036 - Zone Maps / Zone Completion - COMPLETE
Hex group discovery, movement-based rate limiting (10 unit^2 threshold),
`WorldZone` parent chain + `MapZoneWorldJoin` fallback. Zone completion
with `AchievementType.MapComplete`, `ZoneCompletionRewardResolver`,
faction-aware. DB persistence.

---

## Remaining Blocked Items

This table is the consolidated **runtime producer/emit** backlog. Broader partial
features (guild bank, taxi routes, spell families, STS crypto, reward rotation,
and others) stay in their `F-00x` sections and in
`Decomp/Analysis/MISSING_FEATURE_MATRIX.md`.

### Code audit (2026-05-23): not implemented despite occasional "ghost gap" claims

| Claim | Code reality |
| --- | --- |
| Entity-stat aux emitted from `Player.AddVisible()` | **False.** Only entity-**create** aux (`0x025F`..`0x0264`) is emitted; stat aux (`0x0889`..`0x093E`) has no production emitter. |
| `HousingNeighborhoodEmitter` on residence `AddEntity` | **False.** `ServerHousingNeighborhoodEntry` / `List` are modeled and shape-tested only; no `Game`/`WorldServer` send site. |
| `SendCraftingStatsUpdate` / `SendCraftingQualityUpdate` before craft success | **False.** `SendCraftSuccess` emits `ServerCraftingFinish` only; `0x084B`/`0x0855` are modeled-only. |
| `TryCompleteFixedRecipe(chargeCounts:)` / `CalculateTotalCharges()` | **False.** Complex craft logs `ChargeCounts` but calls `TryCompleteFixedRecipe` without applying them. |
| `ClientItemContextAction` delegates to `ClientItemUse` | **False.** Handler validates/logs; `SelectedBranch` stays diagnostic-only. |
| LFR `SetInProgress` + server queue integration | **False.** `0x05D5`/`0x0602` are validation/logging only; replacement backfill was removed per evidence rollback. |
| `ServerRaidQueueStatus` never sent | **Partially false.** Emitted with raid-info as zero-value compatibility; queue-position semantics remain blocked. |

| # | Feature | Gap | Blocker |
|---|---------|-----|---------|
| 1 | F-025 | Entity-stat aux: 0x0889, 0x08CC, 0x08F4, 0x0939, 0x093D, 0x093E | Consumer-handler decomp or server emit sites |
| 2 | F-025 | Map-tracked-unit producer timing and TrackingSlot selection | Server producer semantics or live capture |
| 3 | F-004 | NeighborhoodEntry/List (0x0501/0x0506) | Unknown client request trigger |
| 4 | F-008 | Discovery/station/complex-craft semantics and 0x084B/0x0855 emit intent | Server producer semantics, client decomp, or live capture |
| 5 | F-031 | Per-item retail Madame Fay weights | No weight column in `AccountItem.tbl`; rarity-tier weights remain emulator approximation until retail `ServerFortuneRewards` or storefront catalog capture |

---

## Ghidra Decomp Addresses (for next pass)

### Entity Aux Readers (field-level shapes tracked against C# models)
| Address | Function | Handles | Field Shape |
|---------|----------|---------|-------------|
| 0x140095c20 | ServerEntityCreateAuxRow_ReadPayload | 0x025F (44 bytes) | ushort + uint + 3xuint32 + uint + 3xuint32 + uint + uint |
| 0x140095ce0 | ServerEntityCreateAuxRowList_ReadPayload | 0x0260 | counted rows of 0x025F |
| 0x140095a80 | ServerEntityCreateAuxBitPackedRowList_ReadPayload | 0x0261 | counted rows of 0x0263 |
| 0x1400959c0 | ServerEntityCreateAuxBitPackedRow_ReadPayload | 0x0263 (36 bytes) | uint + uint + 3x17bit + uint + 3xuint32 |
| 0x140095b40 | ServerEntityCreateAuxScalarList_ReadPayload | 0x0264 (24 bytes) | uint + ushort + uint + counted uint list |
| 0x140097620 | ServerEntityStatUInt32UInt5UInt32_ReadPayload | 0x08F4/0x087F/0x0938 (12 bytes) | uint + 5-bit + uint |
| 0x140097690 | ServerEntityStatUInt32UInt5Pair_ReadPayload | 0x093D (16 bytes) | uint + 5-bit + uint + uint |
| 0x140097ee0 | ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload | 0x0939 (24 bytes) | uint + 14-bit + 18-bit + wide string |
| 0x140097f70 | ServerEntityStatTwoUInt32UInt64_ReadPayload | 0x093E (16 bytes) | uint + uint + uint64 |
| 0x1400980f0 | ServerUInt32WideString_ReadPayload | 0x08CC and others (16 bytes) | uint + wide string |

### Registration Table Entries (from Ghidra decomp at 0x14006c290)
The registration function stores opcode-to-reader mappings in a lookup table.
Helper scripts can extract candidate server-to-client reader addresses from
that table, but semantic producer/consumer dispatch rules remain blocked until
candidate labels are reviewed individually.

### Group/Raid Readers (decompiled, confirmed)
| Address | Function | Handles |
|---------|----------|---------|
| 0x1406031d0 | Group_HandleMemberAdd_ReadPayload | Member role/promote (dispatches Group_MemberPromoted) |
| 0x140603380 | Group_HandleMemberRemove_ReadPayload | Member kick/leave (calls Group_CopyMemberStatBlock) |
| 0x140603450 | Group_HandleMemberPromote_ReadPayload | Member detail update (5 role fields at 0xd0-0xe8) |
| 0x140603610 | Group_HandleMemberFlags_ReadPayload | Member flags + quest tracking (860 bytes) |
| 0x140603970 | Group_HandleReadyCheck_ReadPayload | Ready check (dispatches Group_ReadyCheck, 60s timeout) |
| 0x140603a60 | Group_HandleGroupDisband_ReadPayload | Roster update / disband (2116 bytes) |

### Next Decomp Targets (not yet labeled)
| Address | Hypothesized Role |
|---------|-------------------|
| 0x14008bf80 | ServerRaidQueueStatus (0x0718) reader (emit exists; semantics blocked) |
| ~0x1404dbxxx | Crafting complex craft stat consumer (`0x084B`/`0x0855`) |
| ~0x140095xxx | Entity auxiliary 0x025F-0264 registration block |
| ~0x140097xxx | Entity auxiliary 0x0889-093E registration block |
| ~0x140605xxx | Looking-for-replacements match handlers |

---

## Build Commands

```powershell
# Game assembly (when server is running)
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo

# Script.Main (quest/map scripts)
dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo

# Full test suite
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo

# Ghidra decomp (from PowerShell, NOT bash)
powershell -NoProfile -ExecutionPolicy Bypass -Command `
  "& { Set-Location 'I:\GIT\NexusForever'; & '.\Decomp\Analysis\run_ghidra_analysis.ps1' -Targets 'WildStar64.exe' -MaxDecompiledFunctions 1000 -DecompileMode Force -ProjectLayout PerTarget }"
```

## Database

```
MySQL 8.0: C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe
Jabbithole: host=127.0.0.1, user=bankai, pass=bankai, db=wildstar_client
NexusForever: host=127.0.0.1, user=bankai, pass=bankai, db=nexus_forever_world
```

## Key Paths

```
Repo root:        I:\GIT\NexusForever
Branch:           codex/evil-expedition-port
Script.Main:      Source\NexusForever.Script.Main\
Game:             Source\NexusForever.Game\
WorldServer:      Source\NexusForever.WorldServer\
Tests:            Source\NexusForever.Game.Tests\
Decomp:           Decomp\Analysis\
Ghidra:           C:\Users\Bankai\.codex\tools\nexusforever-decomp\ghidra_12.0.4_PUBLIC
Game tables:      Source\NexusForever.WorldServer\bin\Debug\net10.0\tbl\
Functions CSV:    Decomp\Analysis\function_labels.csv
Decomp cache:     Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\
```
