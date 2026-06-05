# NexusForever Feature Restoration - Current Status

Last updated: 2026-06-04 (F-005 direct commodity fill order-mutation persistence gate and multi-order price-priority fill coverage; option/keybind aux packet contracts for `0x056B`, `0x056C`, and `0x056D`, marketplace aux packet contracts for `0x06DF` and `0x07D5`, chat aux row/envelope packet contracts for `0x01B8`, `0x01C1`, and `0x01C4`, plus story/recruitment boundary packet contracts for `0x074A` and `0x077E`; ported useful LaughingWS branch scripts/data overlays, quest-loot/store/catalog/WIP-Dust-Stalker-quest-instance/WIP-live-event/Skyplot-housing overlays, settler build acknowledgements, active Settler hub build-count progress, WIP current-zone path episode activation with optional PathMission prerequisite filtering, active-path object-id completion guard, active Soldier assassinate kill progress with decompile-mapped ProgressCount, branch-informed active-only node/explore-zone/power-map-validated Explorer progress completion, PathMission and PathMissionType achievement credit, client-mapped GameFormula 0x017a path XP fallback for known completed path missions without configured XP, and unflagged PathRewardType.Mission grants for known completed path missions, branch starter-zone/map-only hooks, Shade's Eve/Infestation/Fragment Zero/Gauntlet/Ruins of Kel Voreth/Stormtalon's Lair/Skullcano/Initialization Core Y-83/Red Moon Terror/Genetic Archives/Sanctuary of the Swordmaiden/Datascape/Protogames/Space Madness/Evil from the Ether event/map chains, WIP-guessed Coldblood Citadel, Ruins of Kel Voreth, Sanctuary of the Swordmaiden, Skullcano, and Stormtalon's Lair optional-objective rolls, Space Madness/Gauntlet/Infestation/Protogames Academy/Fragment Zero/Shade's Eve/Red Moon Terror/Genetic Archives/Datascape/Ruins of Kel Voreth/Skullcano/Initialization Core Y-83/Sanctuary/Evil from the Ether/Cryo-Plex/War of the Wilds trigger objective/message/teleport/PvP scripts and phase broadcasts, SQL-backed boss objective-credit hooks including WIP Red Moon Terror Laveka credit, Skullcano and Sanctuary WIP route scaffolding, remaining map-only bindings plus WIP entry/boss/phase/cinematic-hook scaffolds for Deep Space, Rage Logic, Ultimate Protogames dungeon/raid, Protostar SuperMall, Journey into OMNICore-1, Fragment Zero, Infestation, Space Madness, Shade's Eve, Ruins of Kel Voreth, Stormtalon's Lair, Initialization Core Y-83, Genetic Archives, Datascape, and Gauntlet, narrow branch spell-script hooks for Marauder Mine/Pulse Blast, and the client-table-backed Exo-Lab 22 range teleporter; focused branch/path/event tests 552/552, focused instance/public-event tests 436/436, focused map-only entry/boss/phase/cinematic-hook scaffold tests 22/22, focused event/trigger cinematic-hook tests 155/155, focused PvP/adventure branch tests 25/25, focused Evil from the Ether tests 12/12, focused Coldblood Citadel tests 10/10, focused Ruins of Kel Voreth tests 16/16, focused Sanctuary of the Swordmaiden tests 38/38, focused Skullcano tests 34/34, focused Stormtalon's Lair tests 19/19, focused Protogames Academy trigger tests 25/25, focused Fragment Zero trigger tests 13/13, focused Gauntlet trigger tests 16/16, focused Infestation trigger tests 9/9, focused Datascape/objective-credit tests 102/102, path-progress tests 36/36, focused transporter tests 49/49, plus branch spell tests 11/11; F-004/F-009/F-024 remain partial)

Maintained from `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, focused trackers
(`MATCHING_IMPLEMENTATION_STATUS.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`), and
code audits. Update after every implementation pass so it stays the single source
of truth for feature-area completion.

---

## Quick Stats

| Metric | Value |
|--------|-------|
| Total feature areas | 36 (`F-001`..`F-036`; matrix rows `F-016`..`F-020` are five spell-runtime rows) |
| Fully complete | 15 (sections marked **COMPLETE** below) |
| Partial (real handlers/state exist; retail parity incomplete) | 15 |
| Blocked / diagnostic / structural-only | 3 (`F-001` blocked, `F-002` diagnostic, `F-003` structurally closed) |
| Consolidated runtime gaps (emit/producer proof) | 5 rows in **Remaining Blocked Items** |
| Related trackers | `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `MATCHING_IMPLEMENTATION_STATUS.md`, `ENTITY_AUX_DECODE_ROADMAP.md`, `BLOCKER_EVIDENCE_PLAN.md` |
| Placeholder inventory (2026-06-05) | 12 `Client0xNNNN`, 1 `Server0xNNNN` (`Server0x0015`), 16 client diagnostic log-only surfaces, 24 `PrerequisiteType.UnknownNNN`, and 2 quest objective unknowns (`Unknown27`, `Unknown29`); the audit-prompt ghost literal names are absent; `Client0x0701` MCP/cache recheck remains registration-only at `MOV EDX,0x701` (`140079e05`); `Client0x0928` remains shared `uint32+5-bit` registration-only at `1400a8524` through `ServerUInt32UInt5_ReadPayload` (`14008ce80`) |
| Build | 0 errors, 0 warnings |
| Tests | 2504 passed, 0 failed, 0 skipped |
| Handlers surveyed | 200+ |
| Production TODO/FIXME/NotImplemented | 0 |
| **LaughingWS blocker tracking (2026-05-27)** | Evidence harness added; broad new-zone/sandbox/arkship/Rider's Reef rows remain blocked or rejected where proof is missing; Dungeon Chase hidden SMC item `86919` rejected for current seeds because the world store model and type-`0` storefront transport cannot represent it |
| **Dust Stalker Q4516 review (2026-05-27)** | Boss Xagg and bridge-control data remain WIP/GUESSED; self-destruct and exit-panel behavior are blocked pending a harness smoke bundle rather than inferred from source-only SQL |
| **Arcterra/Palaver source-only review (2026-05-27)** | Arcterra Caretaker, Coldblood portal, and Palaver Ish'amel rows stay WIP/GUESSED pending placement, portal target, interaction, and quest smoke proof |
| **Crafting review (2026-06-04)** | Aux emit remains blocked; fixed-recipe success, additive modifier state, and durable rune bridge are evidence-backed |
| **Ghidra decomp evidence (2026-05-23 pass 3)** | Consumer dispatch found at WorldSocket_ProcessServerMessage; entity-create aux verified emitted by EntityCreateAuxiliaryPacketBuilder; 44,991 xrefs created |
| **Addon corpus audit (2026-05-23)** | 876 addons scanned; map tracked/threat/crafting/housing strings confirmed, but only threat list is already implementation-backed |

### Status legend (how to read **COMPLETE** vs **PARTIAL**)

| Label | Meaning |
| --- | --- |
| **COMPLETE** | Client handlers and a working server/runtime path exist with focused tests; known retail gaps may still be listed in the section or in **Remaining Blocked Items**. |
| **PARTIAL** | Substantial behavior exists, but retail parity, producer semantics, or persistence edges remain open. |
| **BLOCKED** | Evidence gate prevents widening behavior (e.g. F-001 STS crypto). |
| **DIAGNOSTIC** | Safe parse/log only (F-002). |
| **STRUCTURALLY CLOSED** | All server opcodes have models (F-003); one numeric `Server0x0015` contract and feature-owned emitters remain evidence-gated. |

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
| 6 | AssignMasterLoot offline winner | Returns false + warning before winner resolution/broadcast when assignee is offline. |
| 7 | FinaliseRoll offline winner | Logs and keeps the item assigned for deferred delivery if the winner reconnects before corpse expiry. |
| 11 | DeliverAllLoot partial success | Mixed delivery now reports incomplete success, preserves delivered rows, keeps failed rows retryable, and suppresses complete generated granted-notify. |
| 18 | Duel "leash" comment | Corrected to "inter-duelist distance limit" (matches code checking 120m between players). |
| 19 | Cancel/timeout sets winner/loser | `ServerDuelResult` now sends 0/0 for Cancelled and DeclinedRequest reasons. |
| 20 | No disconnect hook | `OnPlayerDisconnect` added to `IDuelManager` + `DuelManager`; called from `WorldSession.OnDisconnect`. |
| 21 | MemberIndex never set | `ServerGroupMemberFlagsChanged` now sets `MemberIndex` from `GroupIndex`; opcode `0x0438` was corrected to the native `ServerGroupIdentityListAndUInt32Array` shape, with the provisional leading value still sourced from `GroupIndex` pending consumer proof. |
| 22 | Ready-check clear stale UI | `HasReadyCheckFlags` guard removed; `ServerGroupReadyCheckStatusUpdate` sent unconditionally. |
| 24 | Group flag fan-out packet allocation | `GroupMemberFlagsUpdatedHandler` now builds one immutable role/flag packet and one ready-check packet per update, then enqueues them to online members. |
| 25 | Shared group fan-out helper | Added `GroupMemberFanoutHelper` for online-member broadcast resolution across internal group handlers. |
| 26 | Housing edit mode stub | Now validates residence permission via `CanModifyResidence` and records per-player server edit state on the residence map; ack/broadcast packet semantics remain unmapped. |
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
| 13 | Corrupt DB row crashes startup | Marketplace auction microchip-id parsing now logs corrupt format/overflow values and returns an empty array instead of throwing during startup load. |
| 14 | SearchAuctions materializes all | Restructured: filter, sort, page via Skip/Take, then CloneAuction only for current page. |
| 15 | O(n^2) commodity matching | Added per-item/per-side price indexes for commodity match candidate selection and immediate sell preflight. |
| 16 | Persist deadlock risk | Changed `.GetAwaiter().GetResult()` to `.ConfigureAwait(false).GetAwaiter().GetResult()`. |
| 17 | Null item skipped silently | Added `log.Debug` when skipping auctions with `model.Item == null`. |
| 32 | Field-level tests for 0x034F / 0x034C | Added `ServerSupportUInt32AndFlags_WritesValueFlagsAndPadding()` and explicit `ServerSupportUInt5AndSixUInt32` 27-bit padding consume coverage. |
| 33 | Test placement | Moved `ServerRealmAuxUInt32TripletList` coverage to dedicated `RealmAuxPacketShapeTests`. |
| 34 | Consolidate triplet row types | `ServerSpellUInt32TripletListRow` now inherits the shared `ServerUInt32Triplet` writer. |
| 35 | Consolidate counted triplet lists | Spell `0x080F`/`0x0810` and realm aux `0x05A1` packets now reuse `ServerUInt32TripletListPayload`. |
| 36 | Server0x08CC duplication | Inherits from `ServerUInt32WideStringPayload`, removing ~10 lines of duplicated code. |
| 37 | RowCount property confusing | Added XML doc: "for diagnostic / log use only." |
| 39 | lootInstances no lock | Added XML doc documenting world-thread-only mutation assumption. |
| 40 | NextLootId unsynchronized | `GlobalLootManager.NextLootId` now allocates atomically with `Interlocked.Increment`, preserves the high-bit loot-unit id range, and never returns zero. |
| 42 | Quest script xUnit tests | Created `QuestScriptConstructionTests.cs` with DI smoke test for FollowUpQuestScript<T>. |
| 9 | Delivered loot instance sweep | Fully delivered loot instances are now removed from the manager immediately after final collect/vacuum/roll/master resolution. |
| 8 | Active loot lookup scans | Owner-unit and looter indexes now back notify, runtime snapshot, collect/roll/master lookup, and vacuum paths. |

### P2 - Deferred

| # | Item | Reason |
|---|------|--------|
| 8 | Active loot tick walk | Timing-wheel/shard optimization needs profiling after owner/looter indexing and #9 immediate cleanup. |
| 10 | Duplicate aggregation logic | Refactor deferred. |
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
  - 2026-06-04 focused coverage pins the NF additive bridge as state-only: a valid station records additive/catalyst Item2 modifiers for the next fixed-recipe craft, `ClientCraftingAbandon` clears that state, and neither path emits blocked `ServerCraftingCurrentCraft`, `0x084B`, or `0x0855` packets.

- **`Lua_RegisterItemDataBindings`** (0x140413a20):
  - Registers `CodeEnumRuneType` through `Lua_RegisterCodeEnumValue` (0x1400eff50).
  - Native rune values are pinned as `Air=7`, `Water=8`, `Earth=9`, `Fire=10`, `Logic=11`, `Life=12`, `Fusion=13`, matching `RuneType`.
  - `Lua_GameItemData_GetRuneSlots` (0x14041b8d0) now maps through `ItemData_AddRuneSlotsLuaFields` (0x140673b80): live item data reads slot type bytes at `itemData+0x388`, converts compact values 1..7 through `ItemRuneSlotType_ToRuneType` (0x140514660), and reads installed rune Item2 ids at `itemData+0x518 + index*4`.
  - This closes the rune-type enum question and the durable rune item bridge: `item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`, `RandomGlyphData`/`Glyphs`, and migration `20260531224115_ItemMicrochipIdsAndRuneSlots` now preserve socket types and installed rune Item2 ids. A 2026-06-04 taxonomy cleanup rejects a distinct client microchip-install mutator: client install is mapped to `0x085B` (`RuneCrafting_SendClientRuneInstall`), while the separate `ServerItemMicrochips` (`0x056C`) / `Inventory_UpdateItemMicrochipsFromWire` patch path remains server-side and producer-blocked.
  - 2026-06-04 Ghidra MCP recheck: `ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload` (0x1400a3af0) reads `0x084B` as four `uint32` values, one `float`, then one `uint32`; `ServerUInt32AndTwoFloats_ReadPayload` (0x140081df0) reads `0x0855` as one `uint32` and two `float` values. `Crafting_HandleServerCraftingFinish` (0x1405e6690) and `Crafting_HandleServerCraftingCurrentCraft` (0x1405e6830) prove finish/current-craft consumers only, so aux enqueue intent remains blocked.

### Housing - Neighbor Lua Table (F-004)
- **`Housing_BuildNeighborLuaTable`** (0x1404b4e40):
  - Builds per-neighbor Lua row with fields: `nId`, `nClassId`, `nPathId`, `fLastOnline`, `strRealmName`, `strCharacterName`, `strWorldZone`, `nLevel`, `nFactionId`, `ePermissionNeighbor`.
  - Called when building neighbor Lua rows for `ServerHousingNeighbors` (0x0507) / `Housing_HandleNeighborUpdate` paths (`FUN_1406f9fe0`, `FUN_140735cc0`); **not** called from `Housing_HandleNeighborhoodList` (0x0506).
  - Addon scan confirmed NeighborNotes uses `ICCommLib.JoinChannel("NeighborShare")` for peer-to-peer share/search data, so that addon does not unblock the server neighborhood trigger path.
- **`ClientDB_RegisterHousingNeighborhoodInfo`** (WildStar64 `0x140205900`) is now durably labelled from the `DB\HousingNeighborhoodInfo.tbl` / `HousingNeighborhoodInfo` loader path. This proves the world client has the table metadata, but it still does **not** map table columns to `0x0506` row fields or prove a server send trigger.
- A 2026-06-04 packet-placeholder guard now keeps the `0x0501` row tail as
  `NeighborhoodWireUInt64_AfterRealmIds` / `NeighborhoodWireUInt32_0..2` and
  rejects `HousingNeighborhoodInfo.tbl` column-name synthesis (`BaseCost`,
  `MaxPopulation`, `HousingMapInfoIdPrimary`) until row backing is proven.

### Map Tracked Unit / Threat Addon Follow-up
- Map addon evidence (`GuardZoneMap`, `LUI_ZoneMap`, `RavenMap`) confirms Lua consumers for `MapTrackedUnitUpdate`, `MapTrackedUnitDisable`, and `GetMapTrackedUnitData(id)`, but the server opcodes are `0x0849`/`0x0848`; `0x0264` is the unrelated `ServerEntityCreateAuxScalarList`.
- Producer behavior remains blocked for map tracked units: tracked-unit id allocation, update cadence, disable lifetime, and `TrackingSlotId` selection are not proven. A 2026-06-04 Ghidra MCP recheck reconfirmed only the client cache update/remove plus Lua enumeration chain, and read-only `TrackingSlot` data has duplicate objective groups (`5010`, `5138`), rejecting objective-only slot selection. Focused source/test passes added a null-table guard, packet naming guard, and duplicate-objective selection guard for `TrackingSlotHelper`; it remains one-way table lookup only.
- Threat addon evidence confirms `TargetThreatListUpdated` is a real UI event; this is already backed by mapped `ServerEntityThreatListUpdate` (`0x0909`) and `ThreatManager.BroadcastThreatList()`.

### Remaining Decomp Targets
- **Entity aux consumers** (F-025): Need `Network_RegisterServerOpcode_0351` (0x14006c290, 56KB) to map opcodes to consumer handlers.
- **PvP cooldown/state consumer** (F-015): `ServerPvpCooldownUpdate` (`0x013E`) still only maps to shared `ServerUInt32_ReadPayload` (`14007ab50`) with no opcode-specific apply consumer found; direct Ghidra MCP plugin pass mapped adjacent `ServerUnitPvpStateChange` (`0x08BD`) reader `ServerUnitPvpStateChange_ReadPayload` (`140098160`) and apply path `Entity_ApplyUnitPvpFlagsChanged` (`1403ddc60`), which updates entity `+0x15a8` and dispatches `UnitPvpFlagsChanged`.
- **Item context action / Pet stance** (F-026): `0x00B7` native empty reader is
  mapped; item-context producer/consumer semantics remain unknown. Pet stance
  request/apply flow is mapped through `Pet_SetStance_SendClientPetSetStance`
  (`14050a270`) for `0x068E`, `Pet_GetStance_ReadCachedStance`
  (`14050a130`) for the local cache read, and
  `Pet_ApplyStanceChangedPayload` (`1403c0a80`) for `0x068F`, but server
  producer timing/scope remains unknown.
- **ServerRaidQueueStatus / ServerRaidInfoResponse row** (F-010): reader `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` mapped; adjacent `ServerRaidInfoResponse_ReadPayload` @ `14008c010` reads `0x071A` count-plus-0x20-byte rows and `Group_DispatchRaidInfoResponse` @ `1406042b0` maps row fields to saved-instance id, world id, FILETIME expiration, days-from-now, and prime level; pass 134 found no cached static standalone `0x0718` sender beyond registration and the selected caller into the row reader remains `14008c010`; zero-value `0x0718` compatibility emit remains, while standalone non-zero queue timing/aliases stay blocked.
- **ServerMatching0x05CF** (F-010): raw `uint32` reader `ServerUInt32_LocalReadThunk` @ `140099110` mapped; candidate apply helper `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0` remains correlated only until a real apply dispatcher/index or live `0x05CF` witness proves manager `+0xa0` semantics; the prior `140e1e66c` data ref is PE `.pdata` unwind metadata, the `WorldSocket+0x15b0` slot-11 scan found the Fortune positive control but no matching-helper route, and the 2026-06-05 MCP recheck found only broad manager globals plus shared `0x05CF`/`0x085D` registration refs.
- **Client0x062A/0634** (F-010): shared `uint32` reader/writer `14007d000`/`14007d010` mapped and handlers are log-only/test-pinned; 2026-06-05 MCP/cache recheck still finds `0x062A`/`0x0634` only in movement-spline registration (`1400a82b6`/`1400a872c`), while positive controls `0x05D5` and `0x0635` have real send helpers. Sender/intent remains blocked until a native send site or live queue UI sniff appears.
- **PrerequisiteType.Unknown260** (F-022): live case `0x104` / `Prerequisite_CheckHousingPlotPlugState_Table260` maps active housing plot/plug flags `0x08`/`0x20` against state `5`; the 2026-06-05 MCP passes label the `HousingPlotInfo` / `HousingPlugItem` lookup helpers (`140205fa0`/`140206c60`), the residence plot-entry lookup (`1405aea10`), `HousingPlotEntry_DispatchBuildCompleteIfState5` (`1405a9920`), and `HousingResidence_SetBuildState5AndDispatchComplete` (`1405a9980`), proving state `5` is `HousingBuildComplete`-backed. Enum rename remains blocked because row `36343` still lacks a prerequisite use-site and the active residence state fields at the returned subrecord remain unnamed.
- **PrerequisiteType.IsLocalPlayerEntity** (F-022): pass 140 recovered the formerly missing native function entry at `14049c7b0` as `Prerequisite_CheckIsLocalPlayerEntity`. The body is the already-implemented type 63 Equal/NotEqual identity comparison between the evaluated entity and `DAT_140c65898+0x78` local-player entity; no runtime behavior changed.
- **PrerequisiteType.DeadState / State** (F-022): pass 141 recovered live vtable entries `Prerequisite_CheckDeadState` (`14049c800`, case `0x0c` / `+0x80`) and `Prerequisite_CheckEntityStateFlags_Table149` (`14049c840`, case `0x95` / `+0x88`). DeadState now has a native live witness but NF keeps the existing `IsAlive` proxy until entity `+0x250` / `+0x254` / type `0x17` fields are server-owned; State remains mapped-only/blocked because type-149 row ids have no direct prerequisite-column references and `entity+0x1e4` ownership is incomplete. Pass 142 rejected the strongest cached `+0x1e4` writer cluster (`PrimalMatrix_UpdateNodeVisualStateFlags` `1406e70a0` / `PrimalMatrix_HandleNodeAllocationInput` `1406e73e0`) as a PrimalMatrix node visual/allocation path, not prerequisite entity-state evidence.
  Pass 143 mapped the scene proximity-cue reader/application cluster (`SceneProximityCue_LookupConfigById` `1404cc070`, `SceneProximityCue_IsCandidateInRange` `140722d30`, `SceneProximityCue_CheckPrerequisiteGate` `1407234a0`, `SceneProximityCue_ProcessQueuedCandidate` `1404cc0d0`, `Entity_ApplySceneProximityCue` `14047a1f0`, `SceneProximityCue_SelectWorldZoneEntry` `140722ed0`): it strengthens `entity+0x2ac` as an action/cue-blocking gate and `+0x250/+0x254` as approach/movement blockers, but still gives no native writer for evaluated-entity `+0x1e4` and no type-149 use-site.

### F-001 - STS Token Crypto - BLOCKED
Token crypto handshake, external-account edge routes, and optional envelope
fields remain blocked until crypto/token semantics are mapped safely.

### F-002 - Client Diagnostic Opcodes - DIAGNOSTIC
16 client diagnostic surfaces with diagnostic handlers exist: 12 numeric
`Client0xNNNN` models plus `ClientAccountRealmData`,
`ClientRealmListRealmRow`, `ClientRealmListMessageRow`, and
`ClientAddonModuleList`. Every packet's wire shape is pinned by focused tests.
A 2026-06-04 guard now pins the unresolved diagnostic handler family as
log-only with no plaintext or encrypted server emit; focused diagnostic
coverage passed 90/90. A 2026-06-05 realm-row follow-up keeps
`ClientAccountRealmData`, `ClientRealmListRealmRow`, and
`ClientRealmListMessageRow` structural-only: selected cache call edges nest
their writers under the `Client0x0760` row / `ServerRealmList` serializers,
and no client send owner surfaced. A 2026-06-05 follow-up pins `Client0x00C8` as a
shared 5-bit `MatchType` wire shape that must not be aliased to queue-leave or
challenge-choice behavior without an opcode-specific sender; a second follow-up
pins `Client0x00ED` as mapped `uint64 + uint32 + uint64 + 3 bits` but rejects
duel, mail, and path names until an opcode-specific owner appears. A third
follow-up pins `Client0x011B`/`Client0x011D` as empty/`uint32` shared-writer
shapes and rejects loot-bind, mail, loot-vacuum, and tradeskill-reset aliases
without a direct owner. A fourth follow-up pins `Client0x012D` as a
one-wide-string shared-writer diagnostic and rejects `ClientSuggest`,
account-item, realm-transfer, and pet/quest names until an opcode-specific
sender appears. A fifth follow-up pins `Client0x063E` as the sibling
one-wide-string shared-writer diagnostic and rejects `ClientSuggest`,
account-item, support-ticket, marketplace/auth/status, and filter aliases until
an opcode-specific sender appears. A sixth follow-up pins `Client0x0550` as a
one-`uint32` shared-writer diagnostic and rejects ICComm, spell-list,
matching-replacement, movement-ack, tradeskill-reset, ability-book, combat-log,
and P2P-trading aliases until an opcode-specific sender appears. Client request
intent and server response behavior remain unknown. Needs Ghidra decomp of
client writer functions by feature cluster.

### F-003 - Server Unresolved Output Opcodes - STRUCTURALLY CLOSED
All 702 server opcodes have models. One `Server0xNNNN` enum/model placeholder
remains: `Server0x0015`, whose native reader `ServerUInt5UInt32_ReadPayload`
(`140081f00`) maps one 5-bit field plus one `uint32` but not semantic owner or
producer behavior; the same reader is reused by matching opcode `0x0628` and
inside `ServerFortuneRewards`, so `Server0x0015` remains neutral rather than
being inferred as matching average-wait state. A 2026-06-05 cache/xref recheck
found the positive `0x0628` apply control at
`MatchingManager_ApplyMatchingAverageWaitTimeUpdated` but no equivalent
`0x0015` apply owner or producer; the fresh helper trace was blocked by the open
Ghidra project lock. Of the 62 shape-mapped aux/spell
packets with neutral fields, 12 have controlled entity-create or selected
housing emit paths and 50 remain gated behind consumer or producer evidence
before production emission.
Named-but-partial packets remain tracked in their feature rows.
Eight native size-1 aux registrations now use empty wire models instead of
raw byte payloads: `0x00B7`, `0x00DF`, `0x00EE`, `0x0101`, `0x0143`,
`0x014D`, `0x0160`, and `0x0187`. Their feature-owned producer semantics
remain blocked.
Scalar aux wrappers are also tightened where the native readers are proven:
`0x0181`, `0x01A6`, and `0x0846` now write direct `uint32` payloads, while
`0x0186` is corrected from a raw 4-byte placeholder to a 14-bit scalar.
The adjacent reputation/path-XP aux wrappers are narrowed from raw byte arrays:
`0x01A7` and `0x01A8` now write `uint64 + uint32`, while `0x01A9` now writes
`uint14 + uint32`. Producer semantics for `0x01A6` through `0x01A9` remain
blocked.
The story/recruitment boundary packets are also narrowed: `0x074A` now writes
five `uint32` fields plus one `uint16`, and `0x077E` now writes the native
count-plus-uint32-list shape shared with `ServerFlightPathUpdate`.

### F-004 - Housing - PARTIAL (21/22 handlers complete)
**Implemented:** Residence create (level-14 gate), neighbor persistence,
harvest splitting, direct visits, community rename/placement/privacy/removal,
return handler, vendor list, decor/plug/remodel/flags delegation.

**Fixed this pass:** `ClientHousingEditModeHandler` - was empty-body stub,
now validates residence map, records per-player server edit state, and logs the
toggle. Private residence visits now allow owners/authorized modifiers through
the residence entrance teleport path while
unauthorized visitors still receive `Visit_Private`. Decor create now validates
colour shift, scale, and non-crate plot position before currency debit,
achievement update, or `DecorCreate`; focused housing coverage passed 93/93.
A 2026-06-04 F-004 closure removed the WIP hardcoded
`ServerHousingProperties.Residence.NeighbourhoodId` value; property rows now use
the residence `GuildOwnerId` when present and `0` otherwise, with focused
housing coverage passing 32/32.

**Branch housing script port:** The useful housing scripts from
`LaughingWS/NexusForever` branch `Questing-and-more` are now in
`Source/NexusForever.Script.Main/Housing`: the housing portal (`26350`) grants
missing Recall/Escape House spell-base rows and casts the housing dialog spell,
while the Thayd/Illium housing intro decor entities (`54400`/`54401`/`54403`/
`54404`/`65296`/`65297`/`65298`/`65299`) send their mapped story panels.
The branch housing active-prop doors (`65852`/`70052`/`75398`) now initialise
closed, toggle `StandState`, and emit the matching door emote from the
activator's visibility set. Focused branch-script/event tests passed with the
transporter/creature/Space Madness/Protogames Academy ports; see the top status
line for the latest combined count.

**Blocked:** `ServerHousingNeighborhoodEntry` (0x0501) and
`ServerHousingNeighborhoodList` (0x0506) - packet models exist but nobody
sends them. Unknown client request trigger (not accessible via addon API).
The old tracker note that a hardcoded neighborhood property blocks this packet
cluster is stale: `ServerHousingProperties.Residence.NeighbourhoodId` is a
separate property row and now uses the residence `GuildOwnerId` when present and
`0` otherwise. A 2026-06-04 Ghidra MCP recheck reconfirmed only the client cache
consumer and ruled out adjacent community placement, plot reservation, and visit
senders (`0x052B`, guild-operation `0x04B1`, `0x052F`) as the missing `0x0506`
trigger. A later 2026-06-04 export pass labelled WildStar64
`ClientDB_RegisterHousingNeighborhoodInfo` (`0x140205900`), confirming the table
loader only; row backing and send timing remain blocked.
Focused tracker-cleanup verification passed 27/27 against housing packet shapes,
residence-session non-emission, and the three residence neighborhood property
cases; pass 119 housing/placeholder guard coverage passed 106/106 and keeps the
row tail wire-named until producer/backing proof exists.

### F-005 - Marketplace / Auction / Commodity / CREDD - COMPLETE
**All 15 client messages have complete handlers.** Auction post/search/buyout/bid,
commodity post/cancel/buy/sell matching, CREDD exchange/history/redeem.
DB persistence for auctions, commodity orders, CREDD orders, CREDD history.
Slot limits: Free 3 / Signature 30. Offline marketplace credits via mail.
Server aux packet contracts `0x06DF` and `0x07D5` are reader-mapped and
packet-covered; their marketplace producer/consumer semantics remain blocked.
The 2026-06-04 MCP/export recheck found only registration call-site/data xrefs
for those aux readers, not a static apply path, producer, or emit timing proof.
Unsupported auction property min/max, rune-slot, equippable-by filters, and
property sort are rejected at validation instead of accepted and silently
ignored; retail stat/rune/equippable filtering remains unmapped.

**Fixed this pass:** Commodity order expiration - `ProcessExpiredCommodityOrders()`
scans every 1s, refunds/returns expired orders, sends `ServerCommodityAuctionRemoved`.
Marketplace settlement delivery-failure guards now keep auction/commodity records
active when item delivery cannot be made. Commodity sell cancel/fill uses
conservative stack-slot capacity preflight plus mail fallback success before
mutation; auction expiry/buyout waits for delivery success before seller credit
and order removal. `ForceImmediate` commodity orders now match without becoming
resting orders, skip the active-order cap, and refund/return unmatched remainder
immediately; partial commodity fill refunds now exclude the filled purchase cost.
Persisted commodity rows now validate id, owner, Item2, quantity, escrow,
legacy `ForceImmediate`, and list/expiration ordering before load, skipping
corrupt rows before they can refund or deliver. Marketplace mail content type is
now persisted in `character_mail.contentType`, preserving auction expired/return
semantics after reload while legacy rows still fall back to sender type. Auction
search paging now handles huge client page values by returning an empty page
instead of overflowing. Settled auction delete persistence saves the moved item
state instead of deleting the item row. Auction/commodity insert DB failures now
roll back transient listing/order/item/escrow state, auction bid update DB
failure restores the prior bid and refunds the bidder, and marketplace mail save
failures restore attached item owner state before reporting delivery failure.
Online-inventory auction buyout now persists the auction delete and moved item
state before buyer debit, seller credit, winner notification, auction removal,
or inventory delivery; delete-save failure returns `DbFailure` and leaves the
auction active. Online-inventory auction cancel now likewise persists the
auction delete plus returned item state before bidder refund, inventory return,
or auction removal; delete-save failure returns `DbFailure` and leaves the
auction active. Online-inventory auction expiration now persists the auction
delete plus moved item state before direct winner inventory delivery, seller
credit, winner notification, or auction removal; delete-save failure leaves the
auction active. Direct commodity buy/sell cancels now persist the order delete
before escrow refund or inventory item recreation, so delete-save failure
returns `DbFailure` and leaves the commodity order active. Direct commodity
expiration likewise persists the order delete before online escrow refund,
inventory item recreation, removal notification, or order removal.
Auction won/return mail settlement now composes mail creation, attached item
save, and auction row deletion in one character DB save for buyout, cancel, and
expiration mail delivery; composed save failure restores attached item owner
state and leaves the settlement uncommitted.
Commodity sell-order return mail for cancel/expiration now composes mail
creation, returned-item save, and commodity order deletion in one character DB
save; expiration fallback uses commodity return mail content instead of the
fill-mail path, and composed save failure leaves the order active without a
removal notification.
Commodity fill mail now composes buyer mail creation, purchased-item save, and
resting buy/sell order update/delete in one character DB save when a match must
deliver purchased items by mail; composed save failure leaves the match unfilled
and keeps resting commodity orders active.
Direct commodity fills now persist the resting buy/sell order update/delete
before online inventory delivery, seller credit, buyer price-improvement refund,
or fill notifications; save failure leaves the match unfilled, refunds the
force-immediate buyer's escrow, and keeps the resting order active. A focused
multi-order direct buy test now pins price-priority split fills and the
price-improvement refund across two sell orders.
Commodity matching now uses per-item/per-side price indexes for opposite-side
candidate selection and immediate sell preflight, preserving price-priority
behavior while avoiding full commodity-list scans for every match attempt.
Remaining final settlement DB transaction/save atomicity across offline auction
or commodity rows, item state, and online/offline currency persistence remains
open.

### F-006 - Storefront / Account Inventory - COMPLETE
Catalog, purchase, account currency charge, claim/return, pending item groups,
daily login, coupon redemption, VC packages, wallet updates, purchase history,
privilege restriction, purchase-velocity gate (10/hr), CREDD redeem (1000:1).
70 focused packet/account tests. All `0969..0991` opcodes mapped.
Character-select direct account purchases now apply entitlement/currency changes
immediately and refresh the character list; in-world account-inventory direct
grants now handle targeted character rows and enforce character-slot cap
prerequisites through entitlement max-count.
The `LaughingWS/Questing-and-more` account/storefront slice is superseded by
the current implementation: its useful account inventory, cooldown, operation
result, account-tier, catalog, purchase, and purchase-history surfaces are
already covered, while the branch's `0x097D` transaction-update name is rejected
by current pending-item-group delete evidence and its `0x03DD` subscription
packet remains unmapped/unemitted.

### F-007 - Reward Rotation - PARTIAL
Game-table refresh emits (`0x07CA` schedule, `0x07CD`/`0x07D3` content context),
`AccountRewardRotationGrantManager` + `account_reward_rotation_grant` persistence,
claim path via `ClientRewardUpdateRequest`, and non-empty `0x07C8` entry-state on
refresh/claim are implemented with focused tests.

2026-06-04 Ghidra MCP recheck: `0x07CD` still registers only
`ServerRewardRotationContentContext_ReadPayload` (`14008fcb0`) with no static
apply handler; `Reward_SendRewardUpdateRequest` (`140636ba0`) sends only the
content-type index through `0x07CC` and reads manager throttle slots at
`+0x150/+0x158/+0x160`. This maps the request-throttle correlation but not the
server context apply assignment.
`RewardRotationRuntimeEvidenceTests` now pins the generated evidence artifact's
blocker text for the missing `0x07CD` apply helper and blocked `Flag` /
throttle-slot assignment, so capture bundles preserve the mapped-only boundary.

**Blocked:** per-content authoritative reward mapping precision, `0x07CD` apply/`Flag`
consumer semantics, dynamic throttle-slot assignment, and full schedule filter parity
(player level/difficulty). Next evidence source is a retail `0x07CD` capture or
dynamic breakpoint on the runtime apply dispatch.

### F-008 - Crafting / Tradeskill - PARTIAL (12/12 handlers functional)
**All 12 client handlers complete** with proper request/response cycles.
Fixed-recipe crafting, supply satchel, tradeskill lifecycle, schematic
learning, profession modifiers, additive state/abandon clearing, rune slot management, rune fusion (add,
clear, install, reroll - all 4 operations send `ServerTradeskillSigilResult`).

**Remaining gaps:**
- Complex craft stats (CraftStats, ApSpSplitDelta, ChargeCounts discarded)
- Discovery attempt coordinate packing, hot/cold emit timing, and unlock mutation remain blocked; validate with an F-008 live capture before enabling non-success discovery results
- Crafting station request semantics are implemented; native service-key names remain diagnostic-only
- 0x084B/0x0855 emit intent (corrected 24-byte and 12-byte payload shapes modeled, never sent; 2026-06-04 Ghidra MCP recheck found reader/registration evidence only)
- Non-success sigil result rules and server-side `ServerItemMicrochips` (`0x056C`)
  patch producer precision
  (microchip taxonomy cleanup verification passed 149/149)

### F-009 - Rapid Transport / Taxi / Flight Path - PARTIAL (route-table fix verified; blockers remain)
Pricing/request validation is partial. The 2026-06-04 taxi/rapid-transport
capture proved live `ClientRapidTransport` (`0x0141`) requests for taxi nodes
`89`/`88` plus `ClientFlightPathPurchase` (`0x00FF`) route chains
`[236]`, `[124]`, `[124, 121]`, `[8, 114]`, and `[8]`; the failure root cause
was `TaxiRoute.tbl` not being loaded. `TaxiRoute` is now `[GameData]`, the
rapid/taxi handlers reject cleanly when `TaxiRoute`, `TaxiNode`, or
`WorldLocation2` tables are unavailable, and focused handler tests cover
captured table-backed route charging, rapid-transport spell cast context, and
flight-path destination teleport. Service-token bypass, global route-state
snapshots, taxi embark/completion timing, broader charge/teleport parity, and
vehicle deployable/passenger semantics remain blocked by live/native evidence.
`ServerVehicleEmbarkAux` (`0x01B2`) now uses its mapped client-reader shape
(`flag`, 2-bit value, `uint64`, two `uint32` fields) instead of a fixed raw
payload. Field semantics and runtime producer timing remain blocked.
`ServerRecruitmentAuxUInt32List` (`0x077E`) now matches the shared
`ServerFlightPathUpdate_ReadPayload` count-plus-uint32-list shape; flight/taxi
alias semantics and emit timing remain blocked.

**Branch transporter port:** The useful table-backed transporter catalogue from
`LaughingWS/NexusForever` branch `Questing-and-more` is now consolidated in
`Source/NexusForever.Script.Main/Transport/WorldLocationTeleporterEntityScript.cs`.
It covers the branch's Illium/Thayd city route pads plus Star-Comm Basin,
Algoroc/Northern Wilds, Datascape, Ellevar, Genetic Archives, and
Illium/Thayd Arcterra/Halon Ring/Malgrave/Museum routes through `WorldLocation2`
destinations, plus the table-backed Exo-Lab 71 (`28454` -> `21760`) and
Exo-Lab 22 (`25886` -> `19282`) range triggers. The Exo-Lab 22 destination is
the client `WorldLocation2` row whose coordinates match the branch route, while
the official world database supplies the outer `25886` teleporter placement; the
branch's extra source entity row remains unimported because its SQL marks the
ActivePropId as fake. A narrower coordinate route pass now ports the branch's
starter-zone and cross-zone pads only where the official world database has paired
portal/transport placement evidence: Everstar Grove -> Celestion (`32269`),
Deradune/Levian Bay -> Crimson Isle (`45366`/`70174`), Northern Wilds ->
Everstar Grove (`70172`), and Ellevar/Crimson Isle -> Levian Bay
(`30352`/`70173`). The script guards missing rows / missing worlds / pending
teleports and preserves the Exo-Lab trigger-plane side check. Focused
transporter tests passed 49/49. The branch housing return pad remains
superseded by current housing return handling.

### F-010 - Group / Raid / Matching - PARTIAL (replacement backfill remains blocked)
All 13 group client handlers, 16 internal group handlers, 14 matching handlers,
raid info, quest share, instance settings - all fully functional.
Group instance difficulty now routes through the group server with leader-only
runtime group-state mutation, online member `InstanceDifficulty` sync, and
`ServerGroupInstanceDifficultyResponse` fan-out. Internal group broadcast handlers
now use a shared online-member fan-out helper, and group flag updates reuse one
immutable role/flag packet plus one ready-check packet per update; group-focused
tests passed 158/158.
Ready-check pending state is intentionally sent as a single `Pending`
member-flag update after in-memory ready/has-set-ready clears, and the world
handler always emits `ServerGroupReadyCheckStatusUpdate` including status `0`
for pending/cleared state.

**Validation/logging only (not full LFR backfill):**
- `ClientMatchingMatchInitiateLookingForReplacementsHandler` / `ClientMatchingStopLookingForReplacementsHandler` validate in-progress match membership and native role mask `0..2`; they do not start a server replacement queue
- Removed the unsupported replacement anchor queue/merge runtime; addon and sender
  evidence prove the UI/event boundary, not server producer timing or merge rules
- `ServerRaidQueueStatus` (0x0718) emitted alongside `ServerRaidInfoResponse` as zero-value compatibility state; shared row fields are mapped through `0x071A` / `Group_DispatchRaidInfoResponse`, but standalone non-zero queue timing and queue-only aliases remain blocked
- `ServerMatching0x05CF` remains a raw `uint32` packet only: reader `140099110`
  is mapped, and `MatchingManager_ApplyManagerUInt32Field0xA0` (`1405c41c0`)
  is only a correlated apply candidate until a real apply dispatcher/index or
  live packet witness is proven. The prior `140e1e66c` xref is PE `.pdata`
  unwind metadata, not a matching dispatch-table cell; the `WorldSocket+0x15b0`
  slot-11 scan found the Fortune `vtable+0x58` positive control but no
  `1405c41c0` match. A 2026-06-05 MCP recheck confirms the candidate apply
  helper only touches broad manager globals `140c65b98` / `140c65898`, while
  `ServerUInt32_LocalReadThunk` refs resolve to the shared `0x05CF` and
  `0x085D` registration rows, not an apply dispatcher.
- Shared one-flag matching outputs `0x05B0`/`0x05CC`/`0x05F1` remain wire-mapped
  only. `MatchingManager_ApplyMatchingRoleCheckStarted` (`1405c0e90`) consumes
  `payload[0]` and dispatches `MatchingRoleCheckStarted`, but its only direct
  ref is `140e1e2c4` in PE `.pdata`; the `WorldSocket+0x15b0` slot-11 scan
  found the Fortune `vtable+0x58` positive control but no matching-helper entry
  for `1405c0e90`, `1405c1850`, `1405c21d0`, `1405c2440`, or `1405c24a0`.
  The adjacent zero-payload matching event helpers likewise only have `.pdata`
  xrefs.
- `Client0x062A`/`Client0x0634` remain numeric diagnostic packets; focused
  handler tests now pin both as log-only with no queue/raid-state emit until a
  native sender or live sniff proves their semantics
- Matching leave/cleanup lifecycle hardening now snapshots queue collections and
  match-team members before removal mutates the underlying dictionaries; focused
  matching tests passed 90/90.

**Ghidra decomp confirmed:** All 6 group reader functions decompiled.
9 of 10 unresolved opcodes already correctly wired:
- 0x0414 ServerGroupInstanceDifficultyResponse [ok]
- 0x042A ServerGroupKickResult [ok]
- 0x0431 ServerGroupLootRuleValidationResult [ok]
- 0x0436 ServerGroupRosterUpdate [ok]
- 0x0438 ServerGroupIdentityListAndUInt32Array [ok; producer semantics provisional]
- 0x0441 ServerGroupReadyCheckStatusUpdate [ok]
- 0x045A ServerGroupRequestJoinWindow [ok]
- 0x0461 ServerQuestShareResult [ok]
- 0x0468 ServerGroupTargetIdentityPrimeLevelList [ok; producer semantics blocked, stale stat/detail emit removed]

**Blocked:** exact standalone `ServerRaidQueueStatus` queue-position semantics
and non-zero timing (shared row fields are mapped through `0x071A`, but pass
134 found no cached static sender beyond registration and no queue producer/apply
owner is proven);
replacement queue fill still needs live accept/teleport smoke and multi-slot
role-fill verification; `ServerMatching0x05CF` still needs either a real
matching apply dispatcher/index that ties opcode `0x05CF` to `1405c41c0` or a
live match-ready / queue-state sniff that explains manager field `+0xa0`;
`0x05B0`/`0x05CC`/`0x05F1` still need a real dispatcher or live role-check /
queue capture before their single flag can be treated as semantic evidence
because the known `WorldSocket+0x15b0` slot-11 path does not expose the matching
helpers;
`Client0x062A`/`0634`
still need a native sender or live sniff before semantic rename or mutation.

### F-011 - Guild / Recruitment / War Party - PARTIAL (blocked)
Guild handlers and recruitment output models exist. Bank transactions, perks,
holomarks, standards, recruitment subscriptions, boss-token inventory,
warplot plug state incomplete. Needs decomp.
The nearby `0x077E` packet contract is now mapped as a counted uint32 list
rather than a fixed four-field payload; recruitment/pet producer semantics
remain blocked.

### F-012 - ICComm / Chat / Friendship - PARTIAL (blocked)
Transient membership, offline cleanup, directed/ordered delivery. Entitlement
checks, persistent channels, throttling, auto-response persistence incomplete.
Needs decomp.
Group/guild ICComm transient channels now revalidate scoped affiliation on send
and update, removing stale senders/recipients before delivery; focused ICComm
tests passed 8/8.
Chat aux packet contracts are now mapped and typed for `0x01B8`, `0x01C1`,
`0x01C4`, and `0x01EF`: `0x01B8` writes the 4-bit variant chat row from
`140085ca0`, `0x01C1`/`0x01C4` write the wide-string plus 5-bit counted-row
envelopes from `140086410`/`140085fe0`, and `0x01EF` writes the counted
notification rows from `1400a0890`. Chat/cinematic producer semantics and
runtime emit conditions remain blocked.

### F-013 - Mail - COMPLETE
Cash collection, return, attachment deletion, delayed pending-mail promotion,
COD sender settlement, delete result/unavailable signaling, expiration sweep.
Returns now require a real player/GM sender id before moving mail to outgoing;
system/marketplace/zero-sender mail returns report `MailCannotReturn` without
mutating the available mail item, and `MailItem.ReturnMail` enforces the same
invariant.
Marketplace item-delivery fallback is now gated on mail service availability so
auction/commodity state remains uncommitted when neither inventory nor mail can
receive the item; marketplace mail content type now persists across reload via
`character_mail.contentType`; marketplace mail save failures restore attached
item owner state before reporting delivery failure; auction return/won mail and
commodity return/fill mail can compose marketplace row mutations with the mail
save; final marketplace DB transaction/save parity remains under F-005.

### F-014 - Loot - COMPLETE
Basic delivery, group roll/master-loot runtime, roll/assign/result packets,
`ServerLootNotify` ingestion, `ServerLootBindOnPickup`, `ServerLootWinner`.
`LootInstanceResolutionTests` now pin runtime `ServerLootItemUpdate` refresh
packets after roll/finalise/master/deferred delivery plus remote
`ServerLootNotification` feedback. `GlobalLootManager` also rejects non-looter
collect/roll/master-assignment requests before the lower-level `LootInstance`
invariant calls, closing the handler-facing D-L7 crash guard. Corpse group
loot now keeps group context for same-map multi-member groups while filtering
tracked looters, eligible recipients, and master-loot candidates to players
within `LOOT_RANGE`; out-of-range same-map members no longer receive unusable
loot UI.
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
visual effect id for the resolved quality. Imported direct `creature_loot` rows
now coexist with old-table quest-only loot groups such as the LaughingWS
`LaughingWS quest loot:` overlay; direct imports are skipped only when a
DataMapping flat `DataMapping creature_loot` group already represents the same
creature, avoiding duplicate broad drops while preserving quest-objective
virtual-item loot.

### F-015 - PvP / Duels - COMPLETE
**Fixed this pass:** Duel leash/distance system added.
- `DuelLeashRange = 120f` - checks distance each update
- `DuelLeashTimeout = 15s` - auto-cancels duel
- `ServerDuelLeftArea` sent when out of range
- `ServerDuelCancelWarning` sent on re-entry
Cancelled and declined duel results now emit zero winner/loser unit ids, and
active player disconnects are routed through `DuelManager.OnPlayerDisconnect`
from `WorldSession.OnDisconnect`; focused PvP tests pin both boundaries.

PvP toggle-off now starts a player-owned pending cooldown with
`ServerPvpCooldownUpdate` and `RetailCertainRules.PvpFlagCooldownMs`, keeping
`PvPFlag.Enabled` active until the timer expires and then sending
`ServerPvpCooldownClear`. Pending toggle-off cooldowns now persist through
`character.pvpFlagDisableUntilUtc`, reload as `PvPFlag.Enabled` with the
remaining timer, resend `ServerPvpCooldownUpdate` on login, and clear the
stored expiry on cancel or expiration; focused PvP cooldown coverage is 14/14.
Native pass 135 confirms `0x08BD` client state apply via
`ServerUnitPvpStateChange_ReadPayload` (`140098160`) and
`Entity_ApplyUnitPvpFlagsChanged` (`1403ddc60`) / `UnitPvpFlagsChanged`, while
the standalone `0x013E` cooldown consumer remains blocked behind apply-owner or
live UI evidence.
Open-world player-vs-player attackability remains deliberately active-duel-only
through `Player.CanAttack` and `DuelManager.AreDueling`; instanced PvP match
combat is a separate content-map/match path. Broader non-duel open-world PvP
rules remain blocked pending retail proof.

**Cryo-Plex branch harvest:** arena map/event/sub-event IDs from the
LaughingWS branch are now wired using the existing Slaughterdome arena runtime
pattern: forcefield door release on match start, death stat updates, timed
holocrypt resurrection, and team-spawn reset. The active Cryo-Plex behavior is
now marked WIP-guessed in code and covered by focused PvP/adventure branch
tests. Manual queue/match smoke is still needed before calling Cryo-Plex
retail-complete.
Branch-derived Daggerstone Pass (`438`/`466`, world `2166`), Halls of the
Bloodsworn (`876`/`877`, world `3449`), and Walatiki Temple (`217`/`366`,
world `797`) map bindings are also present so the existing PvP content-map base
can create, join, and finish their public events; the bindings are marked
WIP-guessed in code, and mode-specific scoring/round/capture objectives still
need proof before behavior work.

### F-016-F-020 - Spell Runtime - PARTIAL (family-by-family)
Damage/heal/shields/vitals, stack groups/buffs/CC/movement, procs, summons,
RavelSignal. Many families conservative or partial. Work family-by-family
with fixture captures. Most blocked on decomp.
**Spell wrapper packet naming pass:** `ServerSpellWrapperTierEntry` (`0x0818`)
now names its leading `uint32` `SpellWrapperId`, matching dispatcher
`1403ee403 -> SpellService_LookupSpellWrapperByWrapperId ->
SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast`. Wire shape is unchanged;
production emission remains blocked pending wrapper lifecycle sniff evidence
and selector-tail semantic correlation.
**Branch trap/spell ports:** Bramble trap (`27768`) now follows the useful
branch behavior: failed activation casts penalty spell `46051`; successful
activation removes tracked state from that spell and destroys the trap.
`MarauderMineEntityScript` ports the branch disarm activation for mine creatures
`16718`/`24251` by casting detonation spell `26443` at the activator, and
`MarauderMineExplosionSpellScript` destroys the mine only after that detonation
finishes without cancellation. `EngineerPulseBlastSpellScript` ports the branch
hidden Volatility proc for the current Pulse Blast damage spell tiers
(`37302`, `56145`..`56152`) by casting hidden spell `42148` when selected
targets include a hostile unit; client spell/effect map rows corroborate the
Pulse Blast tooltip and hidden `+15 Volatility` effect. The supporting
`ISpellScript` hooks are deliberately narrow (`OnCast`, `OnExecute`,
`OnFinish`); generic branch `SpellParameters.CompleteAction`,
`currentPhase`/phase callbacks, and broad spell-script semantics remain blocked
pending stronger runtime evidence.
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

**Spell cast result reader pass (2026-06-05):** `ServerSpellCastResult`
(`0x07FC`) now has a durable native reader label:
`ServerSpellCastResult_ReadPayload` (`WildStar64.exe` `0x140094fb0`) reads a
leading `uint32`, `18`-bit `Spell4Id`, and `9`-bit `CastResult`. The leading
managed `Unknown0` field remains neutral because current evidence is
registration/reader-only; client request context-token mapping is not enough to
prove result echo semantics without an apply consumer or live echo capture. A
same-day pass 132 MCP/cache recheck still found no apply owner: the MCP bridge
could not connect to the open project, and cached call edges for `140094fb0`
only show internal bit-reader calls. `PacketPlaceholderNamingTests` now guards
against `ContextToken`, `ClientContextToken`, and `CastingId` aliases.

### F-021 - Action Set / LAS / AMP / Attributes - PARTIAL (blocked)
LAS size/spec/tier/AMP preflight checks exist. `UpdateSpellInProgress`,
async spell update transaction, authoritative attribute allocation,
action-bar lock state incomplete. Needs decomp.

### F-022 - Quests / Path Missions / Public Events - PARTIAL
**Coverage audit:** see `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`
(5,194 client quests; no current unsupported objective-type blockers in client
data; 56 hand-curated script rows).
Branch-derived `Q10510LearningToShopQuestScript` now has focused coverage for
the Smart Shopper item/title objective snapshot; exact retail refresh timing is
still marked WIP-guessed in code.
Q5573/Q5604 cinematic-complete objective helpers are covered and explicitly
marked WIP-guessed for exact cinematic completion timing.
Q5584 trapped-assistant and Q5594 ship-control branch interactions are also
covered with focused tests, with quest/teleport activation gating explicitly
marked WIP-guessed in code until live retail smoke proves the access rules.
Q8855 Dominion soldier proximity objective credit/despawn is now explicitly
marked WIP-guessed in code; focused tests already pin accepted/missing gates and
range registration.
Q3486/Q3673 Northern Wilds cinematic/mention/reward helpers now mark branch
timing as WIP-guessed where un-smoked, and Q3486 Loftite Crystal range,
virtual-item reward, despawn, and no-op gates have focused coverage.
Q3487 cannon/Ultrabot and Q3667 control-panel helpers now mark branch-derived
proximity/checklist/path/achievement timing as WIP-guessed, with focused tests
for accepted/missing gates, Ultrabot movement, and achievement duplicate guard.
PathManager now ports the branch current-zone PathEpisode activation as
WIP-guessed table-backed runtime behavior on player zone updates: it matches
current world/root zone/path, filters mission rows by faction and optional
`PrerequisiteId`, and activates the episode without claiming durable path
persistence, exact XP/rewards, or exact unlock sequencing. Explorer ExploreZone
missions now complete only when already active and the player's current
`WorldZone` resolves to a matching `MapZone.Id` (or the world-level
`MapZoneWorldJoin` fallback); this is WIP-guessed from the branch `ObjectId ==
MapZone.Id` mapping. Explorer power-map progress reports now
complete only active Explorer type-`0x12` missions whose `ObjectId` has a
matching `PathExplorerPowerMap` row; ready/active timers, failure packets,
server-owned progress cadence, and durable completion state remain blocked.
Soldier_Assassinate missions now progress from creature kill rewards when the
killed Creature2 id or associated target group matches `PathSoldierAssassinate`;
decompile proof maps Soldier_Assassinate current progress to the first mission
payload now named `ProgressCount`, while alternate progress payload semantics
remain blocked per mission type.
Settler_Hub missions now progress from accepted build-tier requests when the
improvement group maps to the active mission's `PathSettlerHub`, using
`PathSettlerHub.MissionCount` as the contribution target; decompile proof maps
Settler_Hub current progress to the first mission payload now named
`ProgressCount`. Durable built-group state, resource costs, avenue totals,
failure/result precision, and alternate progress payload semantics remain
blocked.
Settler build status packet shape is now mapped for the safe emitted surface:
`ServerPathSettlerBuildStatus` (`0x0671`) reads `PathSettlerHubId` plus one
status row; `ServerPathSettlerBuildStatusList` (`0x066E`) reads hub id, count,
and counted rows; each status row is improvement-group id, tier, remaining time,
and bundle count. `ServerPathSettlerBuildResult` (`0x066C`) now names its second
field `PathSettlerImprovementId`. `PathSettlerBuildHandlerTests` now pin the
safe no-emit boundary for out-of-range improvement group/hub ids, unmapped build
tiers, zero improvement ids, and improvement ids outside the mapped 15-bit
result field; invalid requests still route only through the guarded mission
completion surface and do not invent failure packets. Non-success result values,
durable built-group ownership, resource costs, and failure ordering remain
blocked.
Soldier holdout client accessors are mapped further: `Game.SoldierEvent` now has
labels for type/state, health, elapsed/max time, wave count, waves released, and
defend/auxiliary/escaping unit list accessors. These accessors read a client
runtime holdout object re-resolved by event id, so generic server holdout status
and wave runtime remain blocked pending packet/live capture or packet-consumer
mapping. The current Soldier holdout status, next-wave, end, and death packet
models now have source comments at that boundary and focused serialization
coverage; `PacketPlaceholderNamingTests` passed `51/51`, and the
network-world build passed after pinning those packet shapes.
Path mission runtime state now has a narrow character-owned persistence model:
`character_path_mission` records mission id, episode id, state, completion,
progress payloads, and XP; `PathManager` restores completed mission state and
replays unfinished active episodes during initial path packets. Sequencing
regressions now cover completed-mission no-replay and persisted-active
no-duplicate zone activation. Settler_Hub build-count mission progress also
survives reload through this model, so a partially progressed hub can complete
after the next accepted build-tier request. Active path object-id state,
reward-history persistence, exact replay ordering, full Settler built-group/
resource/avenue backing state, and broader faction/zone/reload negatives remain
blocked pending stronger proof. Persisted active path mission replay is now
guarded by the current active path and table-backed mission/episode/faction/
prerequisite checks, so different-path, missing-episode, and mismatched
episode/path persisted missions no longer replay on login or suppress
current-path zone activation for the same episode id. Focused path-manager
coverage now also pins wrong-faction, unmet simple path-prerequisite,
missing-episode, and mismatched-episode-path persisted replay rejection, with
safe constructor-time evaluation before `player.PathManager` is assigned.
Settler hub rejection coverage now pins zero/missing improvement groups, missing
hubs, different hub ids, and non-Settler active paths without extra mission
updates; focused coverage is `49/49`.
Known path mission completions now also fire `AchievementType.PathMission` with
the mission id and `AchievementType.PathMissionType` with the mission row's
`PathMissionTypeEnum`; total/count-style mission achievement semantics remain
blocked. Known active-path mission completions with no stronger configured XP now
use client-mapped `GameFormula` `0x017a` (`378`) `Dataint0`, with the client
fallback `50` if the row is unavailable; exact per-mission path reward precision
remains blocked. Mismatched
mission/path rows do not receive the fallback, and object-id mission completion
is now limited to active-path mission rows. Unflagged
`PathRewardType.Mission` rows keyed by completed mission id now grant the same
supported reward payloads as level rewards (item, spell, title, and scanbot
profile); item rewards use `PathReward.Count` as a WIP-guessed stack count with
zero falling back to one, and focused reward/path coverage now passes `74/74`.
Exact reward presentation, overflow behavior, reward history, and retail
flagged reward semantics still remain blocked. The last full current game test
project pass remains `1806/1806`.
Public-event vote runtime is now hardened inside the existing proven vote path:
client vote opcode `0x06EE` remains mapped as event id, vote id, team id, and
choice, while `PublicEventVote.Choice` ignores finalised, non-participant,
duplicate, and invalid responses. Focused lifecycle tests cover detailed vote
initiate, tally/end, timeout default choice, and negative responses. Packet
tests pin client vote and server initiate/detailed-initiate/tally/end shapes;
handler/runtime tests now pin forwarding and validation of the full mapped
event/vote/team/choice envelope so stale vote ids and cross-team replies are
ignored before vote state changes. Retail UI sequence, exact timeout cadence,
mismatch feedback, tally/end ordering, and default-choice semantics remain
blocked pending capture evidence. The blocker harness now has
`-PublicEventVoteScoreboardSmoke` for the missing vote/scoreboard packet/UI
proof.
Public-event scoreboard/end packet shapes are now decompile-backed for the safe
model boundary: scoreboard request `0x06FA` carries public-event id plus
subscribe flag, stats update `0x0130` carries counted team/participant rows,
personal stat `0x013B` carries event/stat/value, and end `0x00D6` carries
personal/team/participant/objective rows plus reward tier/type/threshold fields.
Scoreboard subscribe requests now emit one current participant-scoped stats
snapshot for the requested event, and team stat rows aggregate each member's
latest absolute stat value instead of using the last member update. The
unsubscribe no-op test now also proves the handler returns before map/event
lookup, so it does not emit snapshots or create an implied subscription state.
Reward delivery, exact live subscription cadence, score-field precision, result
ordering, and reward tier semantics remain blocked pending content smoke/sniff
evidence.
Public-event objective packet shapes now also have a decompile-backed safe
boundary: objective notification mode `0x0133` carries objective id plus
notification mode, objective status update `0x0134` carries objective id plus a
status row, objective update `0x0132` carries status/busy/elapsed/notification
mode plus counted location and map-region rows, and objective start `0x06F9`
uses the shared 15-bit scalar reader. Exact objective/update text,
phase/completion notification audience and ordering, and quest-share behavior
remain blocked pending capture evidence. The current `0x0132` producer now
suppresses duplicate full-objective updates for unchanged busy-state calls,
with focused objective coverage for that no-duplicate boundary; this is runtime
hardening only, not notification parity.
Public-event trigger objective producers are hardened at the current mapped
boundary: `ParticipantsInTriggerVolume` and `Turnstile` are tied to the client
Lua public-event constants and now ignore zero object ids and non-player
entities. Focused trigger tests cover volume enter/leave deltas and turnstile
entry, duplicate in-range suppression, no turnstile leave/decrement emit, plus
world-location horizontal radius and vertical clamp behavior. Exact branch
trigger rows, coordinates, door ids/states, cleanup timing, and
respawn/despawn behavior remain blocked pending content proof.
Communicator/story-panel packet models now have safe packet-shape coverage for
`ServerCommunicatorMessage`, `ServerStoryTextCommunicator`, and
`ServerStoryPanelCustomShow`, with comments tied to the client
`CommunicatorMessages` and `StoryPanel` table/export anchors. Shared
`StoryMessage` actor rows are also packet-covered for creature, custom text,
localized text, player, creature-unit, and self-player token sources. Real
actor/camera/text timing, send conditions, skip behavior, and replacement of
completion-only cinematic placeholders remain blocked pending capture/decompile
proof.
**NorthernWilds Veteran pass updated this pass.** Core quest/objective,
public-event, challenge, path packet, and opt-in spawn-promotion surfaces exist,
but full 164-NPC parity is still blocked by 3 enabled Jabbithole creatures with
unresolved Creature2 bridges.

**Coldblood Citadel branch harvest:** public event `907` / world `3522` now has
a conservative script scaffold imported from the LaughingWS `Instances-and-more`
branch: map binding, phase/objective progression, communicator dispatch,
entry/gather trigger handling, Hailstone Gatecrasher kill credit, and the
branch optional-objective rolls for liquid Soulfrost, shards, Pell rally,
prisoners, canisters, and Soulfrost traps. Optional objective selection is
explicitly WIP-guessed in code; exact route weights and the full dungeon path
still need live/manual smoke before treating the event as retail-complete.
War of the Wilds (`world 1393`, public event `158`) now has the branch-mapped
base adventure fight scaffold for the giant Moodie totem and totem-health
objectives; the scaffold is marked WIP-guessed in code and covered by focused
PvP/adventure branch tests. Faction-specific start events (`170`/`171`) and
end-delay/chat timing remain blocked. `Start-BlockerEvidenceHarness.ps1`
`-PvpAdventureSmoke` now creates the targeted LWS-080 through LWS-085
PvP/adventure smoke worksheet for Cryo-Plex, Daggerstone Pass, Halls of the
Bloodsworn, Walatiki Temple, War of the Wilds, and Rage Logic; queue/match,
scoring, rewards, PvP stats, faction-start, vehicle-choice, and encounter
parity still require completed bundles.
Outpost M-13 (`world 1319`, public event `108`) now has a conservative
branch-derived objective chain from Captain Milo through Hive Queen and shuttle
return; the branch's unknown final shuttle world-location trigger remains
intentionally omitted because the branch candidate uses world-location id `5`
at the zero vector and is not safe runtime behavior without retail/table proof.
Space Madness (`world 2149`, public event `390`) now has a conservative
branch-derived public-event phase/objective chain from Captain Tero through the
all-clear signal, with focused tests pinning initial phase, dynamic group
objectives, side objective activation, finish behavior, and inactive-objective
guarding. The participant-gather world-location triggers for the airlock and
research-laboratory handoffs are now ported as WIP-guessed code with comments
marking the missing retail trigger-row/timing proof. The branch's placeholder
door creature IDs and direct local teleport remain intentionally omitted pending
retail/client proof.
Protogames Academy (`world 3173`, public event `667`) now has the branch map
binding plus a conservative objective chain from academy initiation through
Wrathbone, with focused tests covering initial phase, group objective max
counts, phase advancement, finish behavior, and inactive-objective guarding.
The PhineasARotostar1..9 phase communicator broadcasts are now ported as
WIP-guessed behavior with code comments/tests. The branch gather/teleporter
world-location triggers are now also ported as WIP-guessed code with tests
pinning ids, objective object ids, and positions. Invulnotron, Gromka, Iruki
Boldbeard, Seek-N-Slaughter, and Icebox Mk. 2 now have WIP-guessed
objective-credit hooks only; exact boss combat mechanics, trigger timing,
communicator timing, platform/launcher cleanup, and entity-removal choreography
remain blocked pending stronger content smoke or client/table proof.
Fragment Zero (`world 3180`, public event `680`) now has a conservative
branch-derived objective chain starting at the branch-enabled "Continue the
Search for the Missing Crew" phase through Hugo's final objective, with focused
tests covering initial phase, multi-objective activation, prototype objective
group activation, phase advancement, finish behavior, and inactive-objective
guarding. Supervisor Lola phase broadcasts and the Captain Hugo continuation
follow-up plus the early search/friendly-Skeech/continuation triggers are now
WIP-guessed with code comments/tests. Exact trigger timing, cinematic gating,
doors, and entity cleanup remain blocked pending retail smoke proof.
Gauntlet (`world 2183`, public event `446`) now uses the corrected
`Expedition\Gauntlet` map binding path and has a conservative branch-derived
objective chain from Pilot Taboro through Judge Kain/Agent Lex, with focused
tests covering group objective counts, chamber branch activation, branch kill
objective activation, finish behavior, and inactive-objective guarding. The
airlock and swarm-pit participant-gather triggers are now ported as
WIP-guessed code with comments/tests marking missing retail trigger-row/timing
proof. Cinematics, announcer/communicator timing, arena door choreography, and
manual expedition smoke remain blocked.
Infestation (`world 1232`, public event `95`) now has a conservative
branch-derived objective chain from cargo-ship entry through the medical-bay and
heal-shorthand phase, with focused tests covering initial phase, dynamic group
objective counts, side objective activation, objective reset behavior, parasite
finish handling, and inactive-objective guarding. The branch's guessed
turnstile trigger is now ported as WIP-guessed code with comments/tests marking
missing retail placement/range proof. Untracked door/open-vent choreography, exact
medical-bay attack timing, parasite objective activation, and manual expedition
smoke remain blocked pending proof.
Expedition smoke coverage is now easier to collect: `Start-BlockerEvidenceHarness.ps1`
`-ExpeditionSmoke` creates the targeted LWS-090 through LWS-096 worksheet for
Outpost M-13, Space Madness, Fragment Zero, Gauntlet, Infestation, Evil from the
Ether, and Deep Space Exploration, including the known branch worlds/events and
negative cases for placeholder triggers, premature doors/shuttles/teleports,
wrong-phase triggers, completion-only cinematics, early finishes, and rewards.
The focused expedition scaffold filter now passes `78/78`; shuttle, door,
trigger, cinematic, communicator, teleport, cleanup, routing, reward, and
full-smoke parity remain blocked until completed bundles or decompile-backed
payload proof exist.
Ruins of Kel Voreth (`world 1336`, public event `161`) now has the branch map
binding plus a conservative main boss chain from the Blood Pit through
Forgemaster Trogun, with focused tests covering the initial phase,
branch-mapped Blood Pit objective count, deterministic Drokk/Gurka/challenge
activation, phase advancement, finish behavior, and inactive-objective
guarding. The branch optional objective rolls for slave mercy, Eldan data
storage, forge destruction, Osun, and war supplies are WIP-guessed with code
comments/tests. The branch's guessed trigger placement, door choreography, exact
optional route weights/objective availability, cinematics, and missing boss
entity scripts remain blocked.
Stormtalon's Lair (`world 382`, public event `145`) now has the branch map
binding plus a conservative main objective chain from Thundercall Pell survival
through Stormtalon, with focused tests covering the initial phase, objective
activation, group-count handling for the High Priest gather objective, phase
advancement, finish behavior, inactive-objective guarding, and the branch
optional objective rolls for tainted stems, altar data, storm totems,
prisoners, grenades, and the Arcanist/Overseer route split. The branch's
guessed trigger placement, Stormtalon reborn cinematic, exact optional route
weights/objective availability, boss spawn/version selection, and missing boss
entity scripts remain blocked.
Skullcano (`world 1263`, public event `148`) now has the branch map binding
plus a conservative main route through Thunderfoot/Stew-Shaman Tugga,
WIP-guessed cave/chasm route selection, Bosun Octog, the Redmoon platform
handoff, and Mordechai Redmoon. Focused tests cover the initial phase, opening
boss activation, random route selection, Find Chief cave-path objectives,
Dorian/Artemis cave callouts, cave continuation objectives, dynamic group counts
for chasm/platform objectives, final-approach objective activation, phase
advancement, Redmoon Terraformer activation, finish behavior, and
inactive-objective guarding. The branch's chasm, Find Chief, platform, and
Eldan Terraformer trigger scripts are now ported as WIP-guessed objective
updates with code comments marking the missing smoke proof. Branch optional side
objectives for captured Lopp, Redmoon prisoners/marauders, and missile consoles
are WIP-guessed with code comments/tests. Exact cave/chasm route weights, Chief
Kaskalak cave route choreography, door/platform choreography, communicator
timing, exact optional route weights/objective availability, and manual dungeon
smoke remain blocked pending proof.
Initialization Core Y-83 (`world 3040`, public event `595`) now has the branch
map binding plus a conservative quarantine-door-to-boss objective chain: mapped
freebot/door objectives advance to the door phase, the door objective advances
to the boss phase, and defeating the Prime Evolutionary Operants finishes the
event. Focused tests cover the initial phase, door objective activation, boss
challenge objective activation, phase advancement, finish behavior, and
inactive-objective guarding. The branch's guessed quarantine trigger placement,
door entity choreography, communicator/cinematic timing, and manual raid smoke
remain blocked pending proof.
Red Moon Terror (`world 3032`, public event `705`) now has the branch map
binding plus a conservative main raid objective chain from the Brig through
Laveka, with focused tests covering all single-objective phases, multi-objective
engineering/medbay/morgue phases, the full phase-advance chain, finish behavior,
and inactive-objective guarding. Ish'amel Robomination/engineering callouts are
WIP-guessed with code comments/tests; the branch static Laveka row is preserved
as WIP/GUESSED seed data and now attaches a WIP-guessed objective-credit script
for the final Laveka objective despite the source lacking an `entity_script`
row. Exact communicator/cinematic timing, Laveka choreography/awakening
mechanics, encounter-specific challenge mechanics, door/elevator
movement, and manual raid smoke remain blocked pending proof.
Genetic Archives (`world 1462`, public event `159`) now has the branch map
binding plus a conservative middle-to-final raid objective chain from the
post-Experiment/Kuralak ascent through Dreadphage Ohmna, with focused tests
covering guardian, archive-defense, convergence, miniboss, and Ohmna objective
activation; branch sub-objective activation; phase advancement; finish behavior;
and inactive-objective guarding. Ohmna entry/final cinematic callouts are
WIP-guessed with code comments/tests; exact communicator/cinematic timing,
weekly/random encounter selection, boss entity choreography, door/elevator
movement, and manual raid smoke remain blocked pending proof.
Sanctuary of the Swordmaiden (`world 1271`, public event `166`) now has the
branch map binding plus a conservative dungeon objective chain from Deadringer
Shallaos through Spiritmother Selene's corrupted form, with focused tests
covering initial, path, temple, Moldwood, Rayna, Skash, terrace, Ondu, relic,
escort, final-boss, phase-advance, finish, and inactive-objective behavior. The
branch's random Temple/Moldwood route choice is now ported as WIP-guessed phase
routing without branch door/trigger placement, and the Spiritmother Selene
random-path callout plus temple, Moldwood, Lifeweaver Terrace, and
flame-miniboss communicator triggers are WIP-guessed objective/message updates
with code comments/tests marking the missing smoke proof. Branch optional side
objectives for corrupted Torine spirits, Moldwood corruptors, skurge/crawlers,
soul spores, terrorantulas, and Corrupted Lifeweaver Pell are WIP-guessed with
code comments/tests. Exact route weights/objective availability, trigger
placement, door choreography, communicator timing, and manual dungeon smoke
remain blocked pending proof.
Datascape (`world 1333`, public event `157`) now has the branch map binding plus
a conservative raid objective chain from the opening system-daemon/probe split
through the Limbo, earth, logic, and volatility wings, all three personality
datacores, and Avatus. Focused tests cover opening activation, single- and
multi-objective phases, branch sub-objective activation, phase advancement, the
Warmonger Chuna branch bug fix, three-datacore gating before the Oculus handoff,
finish behavior, and inactive-objective guarding. The branch's
Caretaker111..114 wing callouts are WIP-guessed with code comments/tests.
`laughingws_instance_entity_wip_seed.sql` now preserves the placed Datascape
boss/objective anchors for ED-1, TX-67, P2-Z, the system daemons, frost boulders,
Frostbringer Warlock, Bio-Enhanced Broodmother, Gloomclaw, Hyper-Accelerated
Skeledroid, Augmented Herald, the Warmongers, and Avatus with deterministic
high entity IDs and WIP objective-credit scripts. Placeholder-coordinate rows,
trash carpets, elemental weekly/wing mechanics, exact communicator/cinematic
timing, encounter choreography, door/trigger placement, exact wing-order retail
proof, challenge mechanics, and manual raid smoke remain blocked pending proof.
The SQL-backed `entity_script` hook pass from
`LaughingWS.WorldDatabase.New-Zones-and-more` now reconciles all non-full-dump
boss script names for Protogames Academy, Ruins of Kel Voreth, Stormtalon's
Lair, Skullcano, Sanctuary of the Swordmaiden, and Genetic Archives, plus the
source-only Protogames Academy Seek-N-Slaughter/Icebox Mk. 2 WIP hooks,
Red Moon Terror Laveka WIP objective-credit hook, Datascape placed-anchor
objective-credit hooks, and the
`OptimizedMemoryProbeTX-67EntityScript` alias, as WIP-guessed objective-credit/
loader hooks with code comments and focused test coverage. Exact boss combat
mechanics, encounter choreography, placeholder-coordinate Datascape rows, and the
all-in-one-only Datascape Hydroflux/Mnemesis script references remain blocked
pending stronger retail/client proof.
Shade's Eve (`world 3044`, public event `597`) now has the branch map binding
plus a conservative early objective chain from fountain discovery through the
locals vote handoff, with focused tests covering the initial phase, dynamic
group objective count, phase advancement, vote start, and inactive-objective
guarding. The branch's guessed trigger placement, gather-ring cleanup, town
gate opening, exact communicator/cinematic timing, and exact vote follow-up
remain blocked; TheAngel1/TheAngel5 callouts are WIP-guessed with code
comments/tests.
Remaining branch map-only and entry/boss/phase/cinematic-hook scaffold content now covers Deep Space
Exploration (`world 2188`, public event `447`), Rage Logic (`world 1627`,
public event `213`), Ultimate Protogames dungeon (`world 2980`, public event
`594`), Ultimate Protogames raid (`world 3041`, public event `642`), Protostar
SuperMall in the Sky (`world 3094`, public event `679`), Journey into
OMNICore-1 (`world 3045`, public event `605`), Fragment Zero, Infestation,
Space Madness, Shade's Eve, Ruins of Kel Voreth, Stormtalon's Lair,
Initialization Core Y-83, Genetic Archives, Datascape, and Gauntlet cinematic
hooks. Focused map-binding
tests pin `19/19` public event IDs. Deep Space, Rage Logic, Ultimate Protogames
dungeon/raid, Protostar SuperMall, OMNICore, Fragment Zero, Infestation, Space
Madness, Shade's Eve, Ruins of Kel Voreth, Stormtalon's Lair, Initialization
Core Y-83, Genetic Archives, Datascape, and Gauntlet now also have WIP-guessed
entry/boss/phase/cinematic-hook scaffolds covered by `22/22` map-entry tests and
`155/155` event/trigger cinematic-hook tests: Deep Space activates
`TalkToCrewMembers`, Rage Logic sets the branch vehicle-choice phase, Ultimate
Protogames dungeon activates `InitiateUltimateProtogames` then stops at the
coarse random-event gate, Ultimate Protogames raid activates the Downsizer
objective set and finishes on Downsizer defeat, and SuperMall activates the
greeter gather objective then stops at the coarse random-path gate; OMNICore,
Deep Space, SuperMall, Fragment Zero, Infestation, Space Madness, and Shade's
Eve queue immediate completion-only placeholders for their branch on-create
cinematic hooks, while Ruins, Stormtalon, Initialization Core, Genetic Archives,
Datascape, Fragment Zero, and Gauntlet queue matching completion-only placeholders
at the branch-derived event/trigger cinematic points. Deeper event objective
routing, encounter logic, door/trigger placement, real cinematic payloads,
rewards, vehicle choice, boss challenge semantics, and route/randomization
choreography remain blocked pending proof.
The branch Northern Wilds Dominion gate helper (`12653`) now opens visible
Icefury Gate door `16799` and closes it after the mapped 10 second delay, with
focused branch-script coverage.
The branch map-zone objective hooks are now ported into the current map scripts:
Northern Wilds Q3486 credits tower-arrival objective `4987` and shows story
panel `1575` when an accepted player enters zone `729`; Crimson Isle Q5596
credits crash-site objective `8255` when an accepted player enters zone `1611`.
Focused `EarlyZoneEntityObjectiveCreditTests` cover accepted and missing-quest
guards.
The branch starter-zone intro cinematic hooks are now wired through current
cinematic interfaces: Northern Wilds (`3480` missing), Crimson Isle (`5593`
missing), Everstar Grove (`6296` missing), and Levian Bay (`6780` missing)
queue their corresponding `*OnCreate` cinematic on player map entry, with
accepted-quest guards covered by the same focused tests.

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
  episode with Jabbithole XP `25`; Explorer progress reports now complete only
  active Explorer Vista missions with a matching `PathExplorerNode` table row
  instead of creating runtime mission state or completing unrelated active
  missions from a client report, ExploreZone missions can complete on zone
  entry when the current map zone matches the active mission, power-map reports
  can complete active missions, Soldier assassinate missions now progress from
  matching killed Creature2/target-group counts, and Soldier tower-defense build packets complete
  matching active event missions, Soldier holdout control-point activations
  now complete Northern Wilds Soldier missions `33`, `34`, and `156`, and
  Settler build-tier packets progress active Settler_Hub missions against
  `PathSettlerHub.MissionCount` while emitting table-backed build result/status
  acknowledgements for the client UI.
  Scientist mission-creature mappings from
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

### F-024 - Evil from the Ether (world 3404) - PARTIAL / WIP-GUESSED
Branch-derived map and public-event `781` scripts exist for the expedition
route: objective/phase progression, dynamic trigger creation, door management,
medbay/upper-deck/escape teleports, cinematic queuing, communicator messages,
crew-log callouts, portal/tether behavior, and Security Chief/Ravenous/Katja
combat scaffolds. The branch-derived trigger, teleport, communicator, cinematic,
and encounter assumptions are now explicitly marked WIP-guessed in code.
Focused Evil from the Ether event/trigger regressions pass `12/12`, and the
focused instance/public-event slice passes `436/436` with isolated output.

**Blocked:** manual expedition playthrough, exact retail trigger rows and
coordinates, direct medbay transition proof, cinematic ids and completion timing,
door/marker cleanup choreography, boss spell cadence/mechanics, objective
notification parity, rewards, and unsupported content/spell dependencies remain
blocked before this can be called retail-complete.

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
registration anchors now exist for all six after the 2026-06-04
`FindImmediateInstructions` pass added `0x0939`/`0x093D`/`0x093E`; field-level
decomp verified against C# models. No production emit sites in
`NexusForever.Game` / `WorldServer` (packet-shape tests only); the 2026-06-04
source audit found only regular `ServerEntityStatUpdateFloat`/`Integer`
runtime sends, not `ServerEntityStat*` aux constructors outside tests/models.
Pass 117 adds a packet-placeholder guard for the six neutral aux field sets
(`Value*` / shared `Value` / `Text`) and passed focused placeholder/entity aux
coverage `91/91`.
The stale native label `FUN_140939650` is now rejected as an aux consumer
candidate because the cached fragment is zero-argument viewport/grid global
math with label-only xrefs, not a socket/opcode/payload apply handler.
Consumer-handler dispatch semantics and map-tracked-unit producer timing remain
blocked (indirect calls through the function pointer registration table and no
server-side tracked-id/TrackingSlot selection proof). A 2026-06-04 focused
Ghidra MCP pass reconfirmed `0x0849`/`0x0848` as client cache update/remove
consumers only; duplicate `TrackingSlot.PublicEventObjectiveId` groups reject
objective-only slot selection. Pass 118 adds a packet naming guard plus a
duplicate-objective helper guard so no objective-only reverse selector is
available. `TrackingSlotHelper` now has a focused
null-table guard and tests for 15-bit id masking/objective lookup, but no
producer or reverse selector.

### F-026 - Items / Unlocks / Costumes / Pets / Titles - PARTIAL
Inventory, title, pet, costume, unlock surfaces exist. Generic unlock
lifecycle tested. Costume forget/unlock parity exists.
Normal `ClientItemUse` spell activation now preflights empty stack/charges and
consumes only after `TryCastSpell` returns `CastResult.Ok`; failed/dead/disabled
casts are covered by `ClientItemUseHandlerTests` and no longer delete the
activated consumable. Decor item-use now preflights empty stack/charges and
residence access before consuming, catches housing access failures before
mutation, and creates decor only after `Inventory.ItemUse` succeeds; this is
covered by `ClientItemUseDecorHandlerTests`.
Treasure item use now consumes category/type `94/200` items with table-backed
character currency payloads before granting currency, covering `Cache of
Corroded Coins` (`50806`) from both `ClientItemUse` and item context-action
paths. Stackable charged consumables now repair zero stored charges before item
packets are built and before cast/consume, covering `Basic Medishot` (`14838`)
without making non-stackable charge items free to use. Starter loot-bag seed
coverage now includes `Nexus Survival Kit` (`83615`) with the retail-evidence
starter supply set and `Protostar's Revolutionary Rucksack 1-10`
(`80875`-`80884`) as consumable loot bags. Rucksack 1 keeps the
patch-note-backed faction starter mount licenses (`Equivar (Provisionary)
License` for Exile, `Velocirex (Provisionary) License` for Dominion), rucksacks
1-9 grant the next rucksack, and all ten include a WIP-inferred one-item PvP
gear roll from the matching client Mk I-X item block (`82720`-`83311`) because
the original server-side container relation is absent from the current
Jabbithole/DataMapping imports.
`ServerSupplySatchelAux` (`0x019A`) now uses the shared mapped reader shape
(`6-bit value`, `uint32`) and `ServerCostumeItemAux` (`0x037F`) now uses its
direct mapped reader shape (`14-bit value`, three `uint32` fields, two flags)
instead of fixed raw payloads. Nearby option/keybind aux packets `0x056B`,
`0x056C`, and `0x056D` now use direct reader-backed field shapes instead of
fixed raw payloads. Producer/consumer semantics for those aux packets remain
blocked.

**Blocked:** `ClientItemContextActionHandler` still keeps `SelectedBranch`
diagnostic-only for non-use item actions until right-click branch semantics are
mapped. Pet stance packet shape and client apply are mapped through
`Pet_SetStance_SendClientPetSetStance` (`14050a270`),
`ServerUInt32UInt5_ReadPayload` (`14008ce80`),
`Pet_GetStance_ReadCachedStance` (`14050a130`), and
`Pet_ApplyStanceChangedPayload` (`1403c0a80`) for `ServerPetStanceChanged`
(`0x068F`), but server producer timing/scope remains unmapped.

### F-027 - Options / Keybindings - COMPLETE
6 handlers, account+character keybinding split, casting options, combat
log preferences. Zero stubs. Nearby server aux packet contracts `0x056B`,
`0x056C`, and `0x056D` are packet-covered, but exact client-facing option
readback/initialisation semantics remain blocked.

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
`apply_creature_loot.py` now writes per-pass selected creature/item counts plus
status-filter, missing Creature2, invalid/missing Item2, duplicate, and
runtime mapped-flat-group skip metrics to `creature_loot_apply_report.json`.

**LaughingWS data intake (2026-05-27):** `Tools\DataMapping\analyze_laughingws_worlddb.py`
audits the external `NexusForever.WorldDatabase.New-Zones-and-more` branch before
promotion. Reviewed extracts are limited to additive/idempotent runtime overlays:
`laughingws_map_entrance_seed.sql` keeps `23` missing `map_entrance` rows,
`laughingws_city_content_seed.sql` keeps `17` curated placed
city/museum/quest-terminal `entity` rows for the ported transport, Illium
Museum content, WIP/GUESSED Illium Ringo Hax placement, and WIP/GUESSED
Skull's Eye escape pod terminal, plus `8` coordinate-keyed WIP/GUESSED
`questChecklistIdx` updates for the official Thayd/Illium housing intro props
with guarded WIP/GUESSED fallback inserts for older local imports missing those
rows, and
`laughingws_quest_instance_wip_seed.sql` keeps `3` WIP/GUESSED Dust Stalker
quest-instance `entity` rows plus `4` Boss Xagg `entity_stats` rows for quest
`4516`, stripping the branch world delete and replacing its source health `1`
with DataMapping-backed stats while leaving the exit panel marked source-only.
`laughingws_small_world_wip_seed.sql` keeps `16` WIP/GUESSED small-world
`entity` rows plus `59` `entity_stats` rows for Tactical Uplink, Captain
Darkstone, Commander Durek, Cortex Prime Whitewater, Corrigan Doon, Star-Comm
Basin The Caretaker, source-only Arcterra Caretaker/Coldblood portal
placements, source-only Palaver Point Ish'amel, and the reviewed Northern Wilds
Deadeye Brightland, Scientist Lusk, Elder Bartol, and Q3673 Signal Flare
placements after reconciling DataMapping-backed branch creature/area/display/
outfit mismatches where available; Dominion/Exile Arkship tutorial rows are
audit-only/rejected for current build-16042 runtime seeds. It also applies
`140` coordinate-keyed WIP/GUESSED `questChecklistIdx` updates to official
rows: `2` Everstar Grove Exo-Lab 71 Eldan Teleporter rows, `33` Wilderrun Kel
Ulgar Weapon Rack / Tattered Banner / Intact Data Cache / Cassian Commonwealth
Emblem rows, `67` Auroria objective rows for Black Hoods Alchemy Supplies,
Cubig Security Zapper Console, Seismic Thumper,
Hycrest infected/plague-victim clusters, Freebot Drills, Navigation Control
Panels, Bingberry Stills, and GL-04 Auto Turrets, plus `24` Crimson Isle
objective rows for Megatech Terminals, Exile Anti-Air Cannons, Dreg Tents,
Dominion Demolitions Experts, Power Regulators, and Tower Controls, plus `14`
Levian Bay Signal Flare / Drop Pod Landing Beacon rows for Lighting the Way and
Lost in the Fog. On older local bases missing those official Everstar Grove,
Auroria, Crimson Isle, or Levian Bay rows, the seed inserts guarded WIP/GUESSED
fallback `entity` rows in the
`1100210001`..`1100210067`, `1100220001`..`1100220024`, and
`1100230001`..`1100230002`, and `1100240001`..`1100240014` subranges, plus up
to `219` fallback `entity_stats` rows; branch Wilderrun Dorian rows remain
blocked
because their source creature ids do not match current DataMapping identity,
Northern Wilds branch spline-mode changes remain blocked pending movement
proof, branch Supply Officer Windward vendor rows are rejected in favor of the
existing DataMapping vendor import, and Everstar Grove/Levian Bay/Auroria/
Crimson Isle branch-only residual rows remain blocked pending row-level proof.
`laughingws_instance_entity_wip_seed.sql` keeps `75` WIP/GUESSED instance-local
`entity` rows plus `67` `entity_event`, `35` `entity_script`, and `80`
`entity_stats` rows for Coldblood Citadel, Protostar SuperMall, Space Madness,
Gauntlet, Fragment Zero, Infestation, Outpost M-13, Evil from the Ether
drive-spark phase anchors, Protogames Academy, Ruins of
Kel Voreth, Stormtalon's Lair, Skullcano, Sanctuary of the Swordmaiden, and
Genetic Archives, map-bound Ultimate Protogames, WIP-script-backed Red Moon
Terror Laveka, Shade's Eve Etty/fountain anchors, and the placed Datascape
boss/objective anchors where current WIP scripts/map bindings already exist. It also
applies `14` coordinate-keyed WIP/GUESSED updates against the official Evil from
the Ether import: `7` branch area reconciliations for Captain Weir, the gather
ring, medbay controls, and spare-parts crates, plus `7` crew-log
`questChecklistIdx` reconciliations for the branch-derived
`CrewLogEntityScript` without duplicating official spawns; full encounter
choreography, trigger timing, NPC interaction, Protogames static trigger
cleanup, combat behavior, Evil from the Ether crew-log/area smoke, Datascape
placeholder rows/trash carpets/elemental mechanics, and cinematics remain
blocked pending client smoke proof.
`laughingws_store_catalog_seed.sql` keeps `3,866` store-table upserts, including
the reviewed Valentine's Day, Shades Eve, and Winterfest store-event rows, after
stripping the branch dump's DDL/deletes and broad event entity rows. The store
extractor now also audits Dungeon Chase by canonicalising its legacy store-table
aliases and known `Field_7` typo, but the hidden SMC placeholder remains skipped
because source type `0` item `86919` exceeds the current world store model and
15-bit storefront transport proof; see
`Decomp/Analysis/LAUGHINGWS_DUNGEON_CHASE_SMC_PLACEHOLDER_REVIEW.md`.
`laughingws_quest_loot_seed.sql` keeps `7,650` virtual-item quest loot rows from
`Loot\CreatureQuestLoot.sql`, shifting source `loot_group` ids into a
DataMapping-owned high range so they do not collide with current runtime loot
groups (`1174` loot groups, `1174` loot items, `5302` entity loot links).
`laughingws_live_event_wip_seed.sql` keeps `440` WIP/guessed live-event rows
(`198` `entity`, `68` `entity_stats`, `8` `entity_vendor`, `24`
`entity_vendor_category`, and `142` `entity_vendor_item`) with deterministic
high entity IDs and SQL comments marking the branch-authored placement as not
retail-verified.
`laughingws_housing_skyplot_wip_seed.sql` keeps `411` WIP/guessed Skyplot
housing rows (`3` `entity`, `12` `entity_stats`, `2` `entity_vendor`, `72`
`entity_vendor_category`, and `322` `entity_vendor_item`) with deterministic
high entity IDs, strips the source world-replacement delete, and comments the
one branch row whose omitted `OutfitInfo` column was corrected.
The audit also treats explicit `ScriptFilterScriptName` aliases as implemented
script names; after the SQL-backed hook pass, missing C# script references are
limited to rejected all-in-one Datascape Hydroflux/Mnemesis mechanics rows.
`BranchCatalogCleanupTests` now guards that rejection by keeping the
all-in-one-only Hydroflux/Mnemesis script names and representative empty or
door/platform/marker-only branch catalog files out of runtime source until an
active, evidence-backed consumer exists.
The latest LaughingWS SQL audit reports `0` unsupported current-schema targets.
Pure `map_entrance` source files are now marked covered by the generated map
entrance seed instead of remaining as unresolved candidates.
Store catalog/event store rows and `Loot\CreatureQuestLoot.sql` are likewise
marked covered by their generated seeds; mixed event files now pair store
coverage with WIP live-event coverage or official-duplicate classification
instead of generic manual review.
Reviewed source files consumed by the city-content, Dust Stalker
quest-instance, small-world, instance-entity, and Skyplot extractors are now
also marked covered by their generated seeds, while the historical arkship,
city-content placeholder, live-event placeholder/duplicate/remover, and
Initialization Core Y-83 source rows are explicit rejections. The PvP arena
forcefield/map-entrance rows in the branch Cryo-Plex and Slaughterdome SQL are
now classified as covered by the official arena import already used by current
runtime scripts, while Slaughterdome's decorative spectator rows remain
rejected/audit-only pending retail placement proof. Five Isigrol open-world
branch dumps are now classified as covered by the official world database
import. The broad new-zone Dreadmoor, Halon Ring, and Murkmire SQL files are
now classified as `blocked_laughingws_new_zone_broad_rows` because the source
rows are hand-authored placeholders, mostly zero-coordinate/area-zero, or
quest-checklist/stat-heavy without current script support. Illium's useful
branch rows are now fully covered by the city-content seed while its orphan
vendor stubs are rejected, Thayd's useful housing intro checklist rows are
covered by the city-content seed while its residual vendor rows are blocked,
and Levian Bay, Everstar Grove, Auroria, and Crimson Isle useful official-row
quest checklist updates are covered by the small-world seed while residual
branch-only rows are blocked. Northern Wilds row review found `23` useful
entity/stat rows covered by the small-world seed, retained `9` official-only
rows, rejected `30` duplicate entity/vendor rows, and blocked `8` branch
spline-mode rows. Wilderrun row review found `33` useful checklist rows covered
by the small-world seed, retained `33` official-only rows, and blocked `5`
branch-only Dorian entity/stat queue rows because current DataMapping Dorian
identities are `25779`/`51292`, not `27455`/`27843`/`58746`.
The remaining LaughingWS implementation-plan smoke/proof gates are tracked
task-by-task in `Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_CLOSURE_MATRIX.md`
as still-blocked, rejected, or already-verified states; no broad SQL or unproven
runtime behavior was promoted by that tracker.
`Start-BlockerEvidenceHarness.ps1 -Lws036ChecklistSmoke` now creates a
row-specific worksheet, default target worlds, and negative-case checklist for
the Illium Ringo Hax, Thayd/Illium housing intro, and small-world checklist
smoke gate. This does not promote any WIP/GUESSED row; it only makes the
required client/server evidence bundle repeatable.
`Start-BlockerEvidenceHarness.ps1 -NewZoneAssetProof` similarly creates a
row-level asset-proof worksheet and negative/safety checklist for Dreadmoor,
Halon Ring broad residuals, Murkmire, and sandbox/test-zone investigations.
Those rows remain blocked or runtime-rejected until completed row-level bundles
prove client assets, exact placement, runtime ownership, and safety.
`Start-BlockerEvidenceHarness.ps1 -DustStalkerQ4516Smoke` now creates the
LWS-051 Dust Stalker target worksheet, defaults world `1138` and objectives
`7633`, `6189`, `7637`, `7638`, and `7639`, and captures the required
premature-bridge, premature-exit, missed-timer, and repeat-interaction negative
cases. Dust Stalker self-destruct and exit behavior remain blocked until a real
client/server bundle proves the timer and sequence.
`Start-BlockerEvidenceHarness.ps1 -ArcterraPalaverSourceSmoke` now creates the
LWS-052 Arcterra/Palaver source-only target worksheet, defaults worlds `3335`
and `3519`, preloads the known Palaver objective ids, and captures invalid
portal target/state, absent Palaver quest state, missing Caretaker prerequisite,
and repeat-interaction negative cases. The Caretaker, Coldblood portal, and
Ish'amel rows remain WIP/GUESSED until a real bundle proves placement and
behavior.
`Start-BlockerEvidenceHarness.ps1 -SkyplotHousingSmoke` now creates the LWS-053
Skyplot housing target worksheet, defaults world `1229`, and captures return
pad without residence/community session, vendor before unlock, invalid catalog
row, and inactive-lifecycle negative cases. The Skyplot rows remain WIP/GUESSED
until a real bundle proves vendor placement/catalog, return-pad interaction, and
active housing/community lifecycle behavior.
`Start-BlockerEvidenceHarness.ps1 -LiveEventSmoke` now creates the LWS-054
live-event target worksheet and captures inactive event, event-ending,
post-cleanup, and skipped-source-row negative cases for Battle Chase, Dungeon
Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1, and zPrix. Live-event
spawn/vendor/lifecycle/cleanup behavior remains blocked until per-event bundles
prove concrete timing and cleanup semantics.
`Start-BlockerEvidenceHarness.ps1 -QuestVirtualLootSmoke` now creates the
LWS-055 quest virtual-loot target worksheet, defaults representative objectives
`13011`, `12605`, `6700`, `6313`, and `21278`, and captures inactive objective,
pre-activation kill, completed-objective repeat, and declined/abandoned loot
negative cases. Quest-loot behavior remains at the current safe
`QuestObjectiveActive`/`VirtualCollect` preservation boundary until real bundles
prove cadence, probability, source selection, and objective integration.
Focused loot regressions now pin that boundary: quest virtual loot groups require
`IsActiveObjectiveId` for condition `8`, and virtual-item loot delivery updates
`QuestObjectiveType.VirtualCollect` plus the mapped loot grant without adding
new cadence/probability/source-selection behavior.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now dry-runs the
LWS-051 through LWS-055 harness presets with `-CreateBundleOnly` and verifies
their manifests, target worksheets, default ids, helper files, and negative-case
scaffolds. This protects the capture workflow only; Dust Stalker timing,
Arcterra/Palaver source-only behavior, Skyplot housing behavior, live-event
lifecycle, and quest-loot retail cadence remain blocked pending real bundles.
The same dry-run guard now covers `-Lws036ChecklistSmoke` and
`-NewZoneAssetProof`, verifying their manifests, target worksheets, default
world ids, helper files, and negative-case scaffolds. This protects the capture
workflow only; LWS-036/LWS-037 still need completed client/server smoke, and
LWS-040 through LWS-043 still need row-level client-asset/runtime-placement and
safety proof before any blocked or runtime-rejected row can be promoted.
The current implementation-plan pass leaves remaining unchecked rows only where
they are explicitly mapped-only, rejected, or blocked on the next evidence
source; LWS-036 through LWS-125 are tracked with one of those states rather than
treated as complete as a group. The
last verification pass reran Python syntax checks for the LaughingWS
extractors/analyzer, repaired analyzer emission of the row reconciliation CSV,
and reran the scratch analyzer/row-review queue path against the local
LaughingWS external snapshot plus official worlddb comparison. The scratch
audit produced `957,167` reconciliation rows and `7,749` queue rows, and all
nine regenerated promoted LaughingWS seed SQL files matched the tracked seeds by
SHA-256. Prior focused xUnit gates remain the script verification surface:
path/Fortune `56/56`, PvP/adventure `29/29`, expedition `78/78`, dungeon
`149/149`, and raid/event-instance `169/169`. No new data overlay,
cross-service packet, storefront, or persistence code path was introduced by
this closure-matrix pass.
`-DungeonSmoke` creates the targeted LWS-100 through LWS-106 worksheet for
Coldblood Citadel, Protogames Academy, Ruins of Kel Voreth, Stormtalon's Lair,
Skullcano, Sanctuary of the Swordmaiden, and Ultimate Protogames dungeon. It
defaults the map-script-backed worlds/events and records negative cases for
premature triggers, premature doors/platforms/launchers/teleporters,
wrong-route optional objectives, completion-only cinematics, and early
finish/reward behavior; these rows remain mapped-only until completed bundles
or decompile-backed payload proof exists.
`-RaidEventSmoke` creates the targeted LWS-110 through LWS-117 worksheet for
Initialization Core Y-83, Red Moon Terror, Genetic Archives, Datascape, Ultimate
Protogames raid, Shade's Eve, Protostar SuperMall in the Sky, and Journey into
OMNICore-1. It defaults the map-script-backed worlds/events and records negative
cases for premature triggers/target sets, premature doors/elevators/gates/rooms,
wrong-route weekly/wing/room selection, completion-only cinematics, and early
finish/reward behavior; these rows remain mapped-only until completed bundles
or decompile-backed payload proof exists.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now also dry-runs
the shared public-event vote/scoreboard, public-event objective notification,
PvP/adventure, expedition, dungeon, and raid/event-instance presets with
`-CreateBundleOnly`. It verifies their
manifests, target worksheets, default world/public-event/objective ids, helper
files, and negative-case scaffolds; this is capture-workflow coverage only, and
the affected LWS-071 through LWS-073, LWS-080 through LWS-085, LWS-090 through
LWS-096, LWS-100 through LWS-106, and LWS-110 through LWS-117 rows remain
blocked or mapped-only until completed bundles or decompile-backed payload proof
exists.
LWS-070's evidence harness is now implemented: `Start-BlockerEvidenceHarness.ps1`
creates timestamped blocker-evidence bundles with manifests,
command/log/client-observation/negative-case templates, screenshot/video/log
folders, and log-tail/collection helpers. Content smoke gates remain open until
real bundles are captured.
LWS-043's sandbox-zone checklist remains runtime-rejected: all `23`
test/unknown/unfinished/not-in-client files (`2,786` insert rows) remain
runtime-rejected as `sandbox_only_client_asset_check_required` until a future
explicit sandbox investigation supplies client-asset, placement, negative-case,
and safety proof.
LWS-040 through LWS-042 remain mapped-only new-zone blockers in
`Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: Dreadmoor (`1,256`
insert rows), Halon Ring broad residuals (`1,692` insert rows), and Murkmire
(`841` insert rows) remain `blocked_laughingws_new_zone_broad_rows`; only the
curated Halon city/transport crumbs are covered by the city-content seed.
`Tools/DataMapping/test_laughingws_worlddb_classifications.py` now guards these
WorldDB decisions with the actual analyzer recommendation logic: broad new-zone
sources remain blocked, all `23` test-zone files remain sandbox-only,
Dominion/Exile Arkship stay historical-only, and Rider's Reef `New Tutorial.sql`
stays skipped rather than additive.
Algoroc's row-level review retained `5` official-only rows and blocked `64`
branch-only queue rows as `blocked_laughingws_algoroc_residual_rows`.
Celestion's row-level review retained `19` official-only queue rows, rejected
`2` zero-coordinate placeholder rows, and blocked `50` branch-only queue rows
as `blocked_laughingws_celestion_residual_rows`. Galeras's row-level review
blocked `41` branch-only queue rows as
`blocked_laughingws_galeras_residual_rows`. Thayd's row-level review keeps the
housing intro branch entities covered by the city-content seed, retains `1,646`
official-only queue rows, and blocks `25` branch-only vendor queue rows as
`blocked_laughingws_thayd_residual_rows`. Whitevale's row-level review retains
`15` official-only queue rows and blocks `105` branch-only queue rows as
`blocked_laughingws_whitevale_residual_rows`. Levian Bay's row-level review
keeps the already-covered `14` checklist rows in the small-world seed, retains
`20` official-only entity rows, and blocks `115` branch-only residual queue
rows as `blocked_laughingws_levian_bay_residual_rows`. Everstar Grove's
row-level review keeps the already-covered `2` Exo-Lab 71 checklist rows in
the small-world seed, retains `3` official-only entity rows, and blocks `59`
branch-only residual queue rows as
`blocked_laughingws_everstar_grove_residual_rows`. Auroria's row-level review
keeps the already-covered `67` checklist rows in the small-world seed, retains
`74` official-only entity rows, and blocks `56` branch-only residual queue rows
as `blocked_laughingws_auroria_residual_rows`. Crimson Isle's row-level review
keeps the already-covered `24` checklist rows in the small-world seed, retains
`24` official-only entity rows, and blocks `74` branch-only residual queue rows
as `blocked_laughingws_crimson_isle_residual_rows`. Deradune's row-level review
retains `505` official-only queue rows and blocks `108` branch-only queue rows,
including `47` checklist-index rows, as
`blocked_laughingws_deradune_residual_rows`. Ellevar's row-level review retains
`51` official-only queue rows and blocks `129` branch-only queue rows,
including `65` checklist-index rows, as
`blocked_laughingws_ellevar_residual_rows`. The remaining
`extract_additive_world_overlay` audit bucket is `0`
broad/residual files after Evil from the Ether, Datascape, Algoroc, Celestion,
Galeras, Thayd, Whitevale, Deradune, and Ellevar residual active rows were reviewed and
classified as blocked instance-entity residuals rather than generic extraction
candidates, the new-zone broad rows were marked blocked, and the Isigrol
duplicates plus Thayd/Illium/Levian Bay/Everstar Grove/Auroria/Crimson
Isle/Northern Wilds/Wilderrun reviewed
residuals were removed from the unresolved bucket.
It also now treats the branch Rider's Reef `New Tutorial.sql` file as
superseded/audit-only instead of an additive-overlay candidate because the
official `New Player Experience.sql` import already supplies the active
build-16042 rows and the branch-only combat rows are marked testing-only or
wrong-faction in source.
The audit also now keeps all `23` `Test zones` files as sandbox/client-asset
work only, rather than suggesting additive runtime extraction for test maps,
unfinished zones, unknown areas, or `Not in Client` SQL.
`Tools\Setup\Initialize-NexusForever.ps1` imports these overlays after the
primary runtime seed when present; when a LaughingWS entity seed hash changes, setup
clears only that seed's owned deterministic high-ID range before reimport. The
generated seeds intentionally omit DDL, deletes, raw `REPLACE`, and foreign-key
disable blocks. `verify_safe_world_imports.sql` now has expected-count mismatch
metrics for every promoted LaughingWS overlay slice, not only the largest WIP
entity seeds.

### F-030 - Realm / Character Select/List/Transfer - PARTIAL
Character list/select works. Realm transfer returns compatibility list.
Current-realm select ignored for client safety. Offline target returns
ServerDown; online transfer returns conservative `Internal` until handoff is
implemented. `ClientInitiatePTRCharacterCopy` (`0x06E7`) carries selected
character id, `ClientPtrCopy` (`0x06E8`) is a mapped native size-1 empty
payload, and `ServerPtrCharacterCopyQueued` (`0x06EA`) is a mapped empty
payload whose native consumer `140020ea0` fires Lua `PTRCharacterCopyQueued`.
`ServerRealmTransferDestinationsAux` (`0x03EF`) now uses its mapped reader
envelope (`uint32` plus counted raw bytes) instead of a fixed raw 0x10 payload.
Focused PTR packet tests pin both empty shapes and verify `ClientPtrCopyHandler`
does not emit queue notices until handoff timing is mapped. Real transfer
destinations/results, `TransferFlag` semantics, `0x03EF` payload contents,
PTR queue producer timing, copy mutation, new realm notices, and optional
realm-message/admin packets remain incomplete.

### F-031 - Fortune Minigame - EMULATOR IMPLEMENTED / RETAIL WEIGHTS BLOCKED
4 handlers functional, session persistence code path via `account_fortune_session`,
reward payouts with account item grants, card flip state tracking.
Weighted emulator rarity-tier pool + `ServerFortuneRewards.RewardItemProbabilities`
match the mapped retail client UI transport (`FortunesLib.GetFortunesLootList`
reads server floats and shows `fProbability = value * 100`; native evidence
2026-05-23). Native chain follow-up on 2026-06-02 maps `socket+0x15a8`
`FortuneNode_ApplyServerFortunePackets` to server opcodes `0x03CF`-`0x03D2`
and names `ServerFortuneCardUpdate.HasUpdate`.
Focused Fortune session tests now guard that runtime flip updates preserve
`HasUpdate=true`, matching `Fortune_ApplyCardUpdate`'s early-return behavior
when the leading bool is false. Payout coverage now also pins that flipped-card
account-item grants carry the current character target identity and explicitly
set the account-inventory `hasTargetPlayerIdentity` flag.
2026-06-04 live local UI logs showed storefront/reward-rotation refreshes but no
Fortune notify/start packets before a chest drag onto the pedestal; reward
rotation index `0` now keeps the existing storefront-catalog ordering and sends
Fortune status/cards through `IFortuneSessionManager` so the UI has a
current/reset Fortune state before chest placement.
Follow-up live logs showed `ClientFortuneStart` reached the server, then reset
because account `1` had no spendable Fortune Coin (`AccountCurrencyType=5`) row
and only claimable Fortune Coin account-item bundles. `FortuneSessionManager.Start`
now auto-claims a matching `CanClaim` Fortune Coin account item before
re-checking and debiting the one-coin start cost.
A subsequent live client crash after the deal persisted cards `3215`, `166`,
and `26`; local `wildstar_client.accountitem` data showed `166` and `26` are
entitlement-only rows with `item2Id=0`. The crash address maps to
`0x14078A28B` inside `FUN_14078a1a0`, which `Fortune_ApplyCards` calls for each
displayed card and which dereferences the resolved card display object at
`+0x158` without a null guard. `FortuneRewardPool` now deals only item-backed
rows (`Item2Id != 0`) so `ServerFortuneCards` does not send non-displayable
account rewards into the native card UI, and `FortuneSessionManager` discards
stored sessions containing non-displayable card ids before sending page-load
status. `Tools/Setup/sql/runtime_auth_seed.sql` now clears transient stored
Fortune sessions during local setup so stale pre-guard card rows do not survive
fresh seed/import flows.
2026-06-04 Ghidra MCP recheck reconfirmed `Fortune_ApplyRewards` only copies
server-provided item/money probability arrays into UI state; it does not expose
an active rotation source. The same pass maps `Fortune_ApplyReset` value `3` to
the click-empty reset path, so `ServerFortuneReset.Unknown` is now
`ResetCode`.
F-007 `RewardRotation*` table/packet labels are now explicitly rejected as
Madame Fay active-rotation evidence: they explain the separate
reward-rotation/storefront schedule surface and bootstrap ordering, while
Fortune catalog probabilities still enter the UI only through
`ServerFortuneRewards`.
`Decomp/Analysis/FORTUNE_WEIGHT_AUDIT.md` verifies the local
catalog/table state, current Fortune tests, and the rejection of the
`Questing-and-more` branch's old hardcoded gacha handler as non-evidence for
retail weights/rotation.
**Blocked:** exact per-item retail weights and active rotation catalog (no
`AccountItem.tbl` weight column; no retail `ServerFortuneRewards` or
storefront-server catalog capture). `Start-BlockerEvidenceHarness.ps1`
`-FortuneRewardsSmoke` now creates the targeted LWS-066 capture worksheet for
that packet/catalog proof plus card/payout/reload negative cases.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` dry-runs the preset
and verifies the manifest, worksheet, helper files, and negative-case scaffold;
exact weights still require a real retail/catalog evidence bundle. Focused
`FortuneRewardPoolTests` now exercise the real pool with synthetic
`AccountItem.tbl`/`Item2.tbl` rows so the mapped item2 probability catalog stays
separate from Fortune Coin/non-item account rewards and current picker-only
account rewards remain guarded without promoting retail rotation parity.
Focused Fortune/reward-rotation boundary coverage passed 32/32 after the
false-source cleanup.

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
| `ServerRaidQueueStatus` never sent | **Partially false.** Emitted with raid-info as zero-value compatibility; shared row field names are mapped, but standalone queue-position semantics and non-zero timing remain blocked. |
| Source TODO/FIXME/NotImplemented search proves unfinished production code | **False after 2026-06-04 audit.** Scoped C# searches now leave no production TODO/FIXME/NotImplemented markers. Remaining hits are test doubles, explicit unsupported guardrails, WIP content docs, or blocked tracker entries with named evidence gates. |

| # | Feature | Gap | Evidence status (latest) | Blocker |
|---|---------|-----|------------------------------|---------|
| 1 | F-025 | Entity-stat aux: 0x0889, 0x08CC, 0x08F4, 0x0939, 0x093D, 0x093E | **Mapped** (wire + XML); no production emit; 2026-06-04 immediate scans added durable registration anchors for `0x0939`/`0x093D`/`0x093E` and still found registration/reader evidence only; 2026-06-04 source audit found no `ServerEntityStat*` aux constructors outside tests/models; stale candidate `FUN_140939650` rejected as viewport/grid global math rather than a packet consumer; focused aux/visual/tracking slice passed `28/28`; pass 117 placeholder guard keeps neutral aux field names until semantic proof and passed `91/91` | Per-opcode `vtable+0x58` apply handler, apply-table classification, or sniff order |
| 2 | F-025 | Map-tracked-unit producer timing and TrackingSlot selection | **Mapped** consumer chain (`TrackingSlot.tbl`); **Implemented** lookup/selection guard tests; **Blocked** producer; Ghidra MCP recheck 2026-06-04 found client cache update/remove only and duplicate objective groups reject objective-only slot selection; pass 118 guard coverage passed `97/97` | Native server send site or public-event marker sniff for `0x0849`/`0x0848`; `TrackingSlotHelper` remains one-way table lookup only |
| 3 | F-004 | NeighborhoodEntry/List (0x0501/0x0506) | **Mapped** row fields (`NeighborhoodWireUInt*`); **Blocked** NF emit; Ghidra MCP recheck 2026-06-04 confirmed consumer-only cache apply and rejected adjacent community/visit senders; 2026-06-04 runtime cleanup removed the unrelated hardcoded `ServerHousingProperties.Residence.NeighbourhoodId` placeholder; pass 119 rejects `HousingNeighborhoodInfo.tbl` column-name synthesis and focused guard coverage passed `106/106` | Live housing UI/realm-login sniff or native server-push path before `Housing_HandleNeighborhoodList` @ `1404ba4f0`; no `Housing_SendClient*` label for `0x0506` trigger yet |
| 4 | F-008 | Discovery/station/complex-craft semantics and 0x084B/0x0855 emit intent | **Mapped** aux wire; 2026-06-04 Ghidra MCP recheck found registration/readers and finish/current-craft consumers only; discovery mutations disabled; service keys `0x2C`/`0x4F`/`0x57` diagnostic; durable rune bridge implemented; distinct C2S microchip-install mutator rejected because client install is `0x085B` while `0x056C` is a server item patch | Native aux/current-craft producer path or live crafting capture for enqueue/timing, discovery unlock/hot-cold proof, non-success sigil rules, and `ServerItemMicrochips` (`0x056C`) producer timing |
| 5 | F-031 | Per-item retail Madame Fay weights and active rotation | **Mapped** catalog transport; emulator `RewardRarity` tiers audited; 2026-06-04 Ghidra MCP recheck found no active-rotation source, renamed `ServerFortuneReset.ResetCode`, and payout grants now target the current character identity; F-007 `RewardRotation*` labels rejected as Fortune active-rotation evidence | Retail `ServerFortuneRewards` capture or storefront-server catalog dump (`FORTUNE_WEIGHT_AUDIT.md`) |

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
| 0x14008bf80 / 0x14008c010 | ServerRaidQueueStatus (0x0718) shared row reader plus ServerRaidInfoResponse (0x071A) count-array reader; fields mapped via Group_DispatchRaidInfoResponse, standalone queue timing blocked |
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
