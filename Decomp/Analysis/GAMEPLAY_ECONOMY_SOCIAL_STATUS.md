# Gameplay / Economy / Social Workstream Status

Updated: 2026-06-09 (F-011 war-party boss-token packet boundary; F-011 guild recruitment packet-family cached-export recheck; F-022 path mission static-table guard; F-004 residence entrance static-data guard; F-014 creature DropLoot Creature2 partial-table guard; F-026 item-manager item/item-slot partial-table guard; F-026 item display-source partial-table guard; F-022 quest target-group cache partial-table guard; F-006/F-026 entitlement-manager partial-table guard; F-006 account-currency partial-table guard; F-007 reward-property partial-table guard; F-014 generated loot reward-table guard; F-005 auction search selector-table guard; F-026 account-item generic-unlock partial-table guard; F-026 persisted generic-unlock partial-table guard; F-026 item-use table guard; F-008 tradeskill request partial-table guard; F-008 additive modifier materialization partial-table guard; F-008/F-011 rune/additive partial-table guard; F-033 challenge reward/share-init blocked classification; F-032 leaderboard medal/season filter blocked classification; F-028 support-case readback blocked classification; F-031 Fortune reward-pool partial-table guard; F-008 crafting fixed-recipe partial-table guard; F-026 repair-vendor formula-table guard; F-022 path-level partial-table guard; F-009 rapid-transport spell-formula partial-table guard; F-026 pet customisation invalid type/slot guard; F-006 account-item terminal missing-inventory guard; F-032 leaderboard duplicate-score dedupe guard; F-033 challenge progress overflow guard; F-027 keybinding character-scope guard; F-006 storefront type-0-only purchase delivery guard; F-031 Fortune target-scoped coin auto-claim guard; F-015 PvP duel/cooldown boundary recheck; F-032 per-scope/category leaderboard cache limiting; F-033 challenge cap/hook reconciliation; F-010 live group-finder CDB smoke correlated queue submit / leave-all / match-ready / decline paths and kept `0x05CF`, `0x0600`, `0x062A`, `0x0634`, `0x0719`, and standalone `0x0718` blocked; F-011 widened Guild UI CDB captured client-owned `0x04A8`/`0x04B1`; F-007 reward-rotation schedule row order/player-level/difficulty refresh; F-026/F-029 runtime `item_salvage` path; F-005 item-auction offline seller settlement guard + commodity buy-order offline expiration refund guard + commodity-fill offline-credit guard + auction bidder-refund offline-credit guard + direct commodity fill persistence gate + multi-order price-priority fill test; F-008 rune bridge closure; R3-L1/R3-L2 item-use guards; R3-L4 duplicate loot overflow guard; loot-bag single-stack delete-failure guard; corpse loot #4/#5 recipient range guard; atomic loot-unit id allocation; R3-M1 same-bidder auction delta guard; R3-M2 ForceImmediate commodity guard; R3-M3 unsupported auction filter rejection; D-M4 auction post tradeability guard; R3-M6 marketplace mail content persistence; R3-M7 mail return guard; R3-M4/R3-M5 marketplace delivery guards; marketplace insert/bid/mail-save plus online-inventory buyout/cancel/expiration and direct commodity cancel/expiration/fill delete-save failure guards; item-auction won/return-mail same-save auction delete guards; commodity return/fill-mail same-save order mutation guards; marketplace microchip overflow guard; R3-M8 auction search page guard; R3-M9 persisted commodity row quarantine; R3-H1/R3-H2/R3-H3 housing visit/decor-create/decor-move hardening; R3-G1 group instance difficulty authority; group online-member fan-out helper; D-G3 ready-check pending packet clarification; R3-IC1 ICComm scoped-membership revalidation; D-G4 duel-only open-world PvP boundary documented/tested; PvP #19/#20 duel cancel/disconnect regressions; durable PvP toggle-off cooldown persistence; support aux padding coverage)

Supplemental update: 2026-06-09 F-009 `ClientSpellCastWithServiceToken`
(`0x00C2`) cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed the service-token spell-cast
wire shape and client-side cost gate. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `0x00C2` size `8` at
`selected_decompiled.c:13077` to
`ClientSpellCastWithServiceToken_WritePayload` (`140089570`), which writes an
18-bit context token and a 32-bit `Spell4` id. `ServiceToken_HandleCastResult`
(`140520c10`) gates on `Spell4.PropertyFlags & 0x20000000`, then
`ServiceToken_SendClientSpellCastWithServiceToken` (`1403994f0`) either returns
`0x014B` for insufficient funds or sends `0x00C2`. Current source already
matches this via `ClientSpellCastWithServiceTokenHandler`,
`SpellParameters.UseServiceTokenCost`, and `Spell.TryConsumeServiceTokenCost`.
This keeps the packet source-aligned but does not prove any rapid/taxi
transport service-token bypass, route-state snapshot, or teleport timing.

Supplemental update: 2026-06-09 F-011 guild recruitment packet-family
cached-export/source recheck
Cached `WildStar64.exe` registration and fragment evidence kept recruitment
packets source-aligned but producer-blocked. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `ClientRecruitmentGuildGetDetailedGuildInfo` (`0x076E`)
size `0x10` at `selected_decompiled.c:13324`; the Lua path
`Lua_GameRecruitmentGuild_GetDetailedGuildInfo` (`14069e5c0`) calls
`RecruitmentGuild_SendClientGetDetailedGuildInfo` (`140584840`), which sends
`0x076E` through `Network_SendOpcodePayloadOrPackedHelper` (`1403f4740`) with
current realm id plus selected guild identity. Adjacent registrations at
`selected_decompiled.c:13369-13377` map `0x049F` and recruitment outputs
`0x0767..0x076D` to the existing managed identity, demand, detail, list,
update, minimum-level, and recruiter row models. Current handlers for
`0x076E` and `0x076F` remain log-only; recruitment persistence, subscribe/update
timing, list/detail population, and availability semantics remain blocked.

Supplemental update: 2026-06-09 F-011 war-party boss-token packet boundary
cached-export/source recheck
Cached `WildStar64.exe` registration and fragment evidence source-aligned the
war-party boss-token request/response boundary and corrected the guild-boss
token cast request width. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `ClientWarPartyBossTokensRequest` (`0x0956`) size `0x10` at
`selected_decompiled.c:13334`, `ClientCastGuildBossToken` (`0x094F`) size
`0x18` at `selected_decompiled.c:13336`, and `ServerWarPartyBossTokens`
(`0x0951`) size `0x20` at `selected_decompiled.c:13363`. `FUN_14057ef20`
sends `0x0956` when type-3 boss-token state is absent; `140092990` reads guild
identity, row count, and 18-bit token item / `uint32` count rows for `0x0951`;
`GuildBossToken_SendClientCastGuildBossToken` (`1403991b0`) sends `0x094F`
with guild identity, an 18-bit token item id, and a 32-bit context token after
target validation. Source now reads `ClientCastGuildBossToken.Item2Id` at the
mapped 18-bit width, while token-list responses stay empty and boss-token casts
still return `BossTokenNotReady`. Real token inventory, cast acceptance/results,
match-results population, and warplot plug state remain blocked.

Supplemental update: 2026-06-09 F-016 spell wrapper follow-up `0x0818` /
`0x0819` cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed the wrapper follow-up pair at
client-consumer scope only. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x0819` size `8` at `selected_decompiled.c:13429` to
`ServerTwoUInt32_ReadPayload` (`14007a040`) and `0x0818` size `0x20` at
`selected_decompiled.c:13430` to
`ServerOpcode0818_ReadSpellWrapperIdAndTierEntry` (`140095e60`). Durable labels
map `0x0818` dispatcher `1403ee403` through wrapper-id lookup (`140561c30`) and
`SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast` (`14053f710`), while
`0x0819` dispatcher `1403ee3af` prunes wrapper nodes through
`SpellWrapperNode_PruneChildrenAndRefresh` (`140718af0`). Current source has
packet models and packet-shape tests but no runtime emitter, so keep both
follow-ups blocked until wrapper lifecycle capture or producer timing proof
exists.

Supplemental update: 2026-06-09 F-026 `ServerItemContextActionAck`
(`0x00B7`) cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed zero-field transport evidence
only. `Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x00B7`
size `1` at `selected_decompiled.c:13226` to shared `ServerEmpty_ReadPayload`
(`14007d8e0`), and the reader performs no bit or field reads. Exact selected
scans found no other `0x00B7` references, selected xrefs are label-only,
selected call edges show no producer or apply owner, and current source has no
runtime emitter for `ServerItemContextActionAck`. Item-use and friendship
positive controls remain separate client senders (`0x0943`, `0x00B8`, and
`0x03B7`). Keep `0x00B7` zero-field and non-emitted until a native producer,
post-read consumer, callback/table owner, or accepted item-context/friendship
capture proves intent and timing.

Supplemental update: 2026-06-09 F-010 `ServerMatching0x05CF`
cached-export/source recheck
Existing cached `WildStar64.exe` fragments reconfirmed the raw-uint32 reader
and tentative matching-manager write candidate only. `0x05CF` remains registered
to `ServerUInt32_LocalReadThunk` (`140099110`), shared with `0x085D`;
`MatchingManager_ApplyManagerUInt32Field0xA0` (`1405c41c0`) still writes the
payload to manager `+0xa0` / optional subobject `+0x98`, but selected xrefs are
label-only and call edges do not prove a dispatcher/index or `ClientEvent`
owner. Current source remains neutral and non-emitted; keep the row blocked
until a real non-`.pdata` apply dispatcher/index or accepted `0x05CF` capture
around `0x05CA`/`0x05CC` appears.

Supplemental update: 2026-06-09 F-010
`ServerMatchingGroupMemberRoleSelection` cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed `0x0600` as a structural
identity-plus-`uint32` row through `ServerHousingCommunityPlotReservation_ReadPayload`
(`140086e70`), shared with seven other registration rows including `0x051F`. Selected
xrefs are label-only; selected call edges are reader-local plus
`ServerLootWinner_ReadPayload` (`1400a4e50`) row-helper calls, and matching
role-check/UI helpers do not reference the reader. Current source keeps
`TrailingValue` neutral and non-emitted; keep `0x0600` blocked until a real
apply/consumer table, producer, callback owner, or accepted live payload ties
the trailing value to role-selection state.

Supplemental update: 2026-06-09 F-010 standalone `ServerRaidQueueStatus`
cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed `0x0718` as the same 0x20-byte
row used by `0x071A` raid-info arrays: `ServerRaidQueueStatus_ReadPayload`
(`14008bf80`) reads saved-instance id, 15-bit world id, FILETIME, days float,
and prime level; `ServerRaidInfoResponse_ReadPayload` (`14008c010`) reads a
count and calls the row reader for each entry. Selected xrefs for both readers
are label-only, selected call edges keep `14008c010` as the only code caller
into `14008bf80`, and `Group_DispatchRaidInfoResponse` (`1406042b0`) proves the
`RaidInfoResponse` UI mapping rather than a standalone `0x0718` producer.
Current source still emits only a zero-value compatibility `0x0718` after
raid-info; keep non-zero queue semantics blocked until a real non-`.pdata`
producer/apply table or accepted non-zero `0x0718` capture proves timing and
field meaning.

Supplemental update: 2026-06-09 F-030 PTR queue/copy handoff
cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed the request/event split only.
`ClientInitiatePTRCharacterCopy_SendFromLuaDispatch` (`140022270`) sends
`0x06E7` with the selected character id and then `ClientEncrypted` `0x0244`;
`ClientPtrCopy_SendFromLuaDispatch` (`14063f540`) and
`ClientPtrCopy_SendFromLuaDispatch2` (`140707d80`) send separate zero-byte
`0x06E8` payloads; `ServerPtrCharacterCopyQueued_DispatchLuaEvent`
(`140020ea0`) consumes `0x06EA` by dispatching Lua `PTRCharacterCopyQueued`.
Registration (`14006c290`) binds `0x06EA` size `1` to `ServerEmpty_ReadPayload`,
while `ServerPtrCharacterCopyQueued_WritePayloadCluster` (`14007dd40`) belongs
to adjacent opcode `0x0592`. Source stays diagnostic-only for both PTR-copy
requests; keep `ServerPtrCharacterCopyQueued` non-emitted until a native
producer/apply path, queue/copy-state mutation, or accepted PTR-copy capture
proves timing and handoff semantics.

Supplemental update: 2026-06-09 F-005 marketplace aux `0x06DF` / `0x07D5`
cached-export/source recheck
Ghidra MCP discovery reported no running instances. Cached `WildStar64.exe`
fragments reconfirmed `ServerAuctionPostAux_ReadPayload` (`140090090`) and
`ServerAuctionsByFilterAux_ReadPayload` (`14008fe80`) as reader-shape evidence
only. `Network_RegisterServerOpcode_0351` (`14006c290`) still binds `0x06DF`
to size `0x20` and `0x07D5` to size `0x14`; selected call edges show only
internal bit/raw read helpers, and current source has packet models plus
packet-shape tests but no marketplace sender. Keep both packets non-emitted
until a native apply/producer path, retail/live marketplace capture, or server
producer witness proves field semantics and emit timing.

Supplemental update: 2026-06-09 F-007 reward rotation `0x07CD`
cached-export/source recheck
Ghidra MCP discovery reported no running instances. Cached `WildStar64.exe`
fragments reconfirmed `ServerRewardRotationContentContext_ReadPayload`
(`14008fcb0`) as the `0x07CD` reader/wire shape, `RewardRotation_ManagerInit`
(`140635840`) as seven request-throttle slot initialization,
`Reward_SendRewardUpdateRequest` (`140636ba0`) as the index-only `0x07CC`
refresh request, and `RewardRotation_GetLoadedScheduleForContent`
(`140636c40`) as loaded-schedule lookup. No apply helper, `Flag` consumer, or
dynamic slot-assignment proof surfaced, so the content-context fields remain
neutral pending a retail `0x07CD` capture or dynamic apply-dispatch breakpoint.

Supplemental update: 2026-06-09 F-004 housing neighborhood cached-export recheck
Ghidra MCP discovery reported no running instances. Cached `WildStar64.exe`
fragments reconfirmed `0x0501` and `0x0506` as reader-only packet shapes,
`Housing_HandleNeighborhoodList` as client cache apply plus
`HousingNeighborhoodRecieved`, and `ClientDB_RegisterHousingNeighborhoodInfo`
as table-loader-only evidence. Source still contains only models, packet-shape
coverage, and residence-session negative emission guards for
`ServerHousingNeighborhoodEntry` / `ServerHousingNeighborhoodList`, so
neighborhood producers remain blocked pending a native server-push path or an
accepted live housing UI/realm-login capture.

Supplemental update: 2026-06-09 F-004 housing community donate `0x04FE`
cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed
`ServerHousingCommunityDonateUpdate_ReadPayload` (`14009e930`) as count plus
two parallel `uint32` arrays, registered for `0x04FE` at
`selected_decompiled.c:13681`. Selected xrefs are label-only and selected call
edges stay reader-local. The separate `0x04F5` client donate path
(`ClientHousingCommunityDonate_WritePayload` `14009da00` and
`Housing_SendClientCommunityDonate` `1404b9ca0`) proves counted `DecorInfo`
request rows plus the `HousingDecorInfo.Flags & 0x8` no-donate guard, but not
the server update array meanings. Source currently emits the update after
copying donated crate decor and deleting the source decor; keep
`Entry.Value0`/`Value1`, contribution/resource costs, exact transfer behavior,
and ownership/unlock policy blocked pending a native apply owner or accepted
donate capture.

Supplemental update: 2026-06-09 F-004 housing community plot reservation
`0x051F` cached-export/source recheck
Cached `WildStar64.exe` fragments reconfirmed
`ServerHousingCommunityPlotReservation_ReadPayload` (`140086e70`) as target
residence identity plus one `uint32` plot index, registered for `0x051F` at
`selected_decompiled.c:13683`. `Lua_HousingLib_GetReservedCommunityPlotIndex`
(`140737470`) reads the type-7 community cache via
`Housing_FindCommunityResidenceEntryByIdentity` (`14057ff90`) and treats
`0xffffffff` as no reservation. Client reserve/remove positive controls send
`ClientGuildOperation` (`0x04B1`) operations `0x29` and `0x2A` through
`Housing_SendClientCommunityPlotReservation` (`14057fe90`) and
`Housing_SendClientCommunityPlotReservationRemoval` (`14057ff10`). Current
source emits `ServerHousingCommunityPlotReservation` from
`CommunityOperations.SendCommunityPlotReservation` and packet tests pin the
`uint.MaxValue` sentinel; keep broader permission, placement, teleport/unload,
persistence, and broadcast semantics evidence-gated pending native apply or
accepted community-reservation capture.

Supplemental update: 2026-06-09 F-031 Fortune retail weights cached-export recheck
Ghidra MCP discovery reported no running instances. Cached `WildStar64.exe`
fragments reconfirmed `ServerFortuneRewards` as the item2/money/probability
transport, `Fortune_ApplyRewards` as client-side cache application of
server-provided arrays, `FortunesLib.GetFortunesLootList` as the UI exposure
that displays `fProbability = serverFloat * 100`, and
`FortuneNode_ApplyServerFortunePackets` as the `0x03CF`-`0x03D2` dispatcher.
Source still emits `ServerFortuneRewards` from the emulator rarity-tier
`FortuneRewardPool`; no retail active-rotation or per-item weight source
surfaced. Keep exact Madame Fay weights and rotation catalog blocked pending a
retail `ServerFortuneRewards` capture, storefront-server catalog dump, or
native/server producer artifact.

Supplemental update: 2026-06-09 F-008 crafting aux cached-export recheck
Ghidra MCP discovery reported no running instances. Cached `WildStar64.exe`
fragments reconfirmed `0x084B`/`0x0855` as reader-only aux shapes at
`1400a3af0`/`140081df0`, `ServerCraftingCurrentCraft` (`0x0854`) as reader
`1400a46b0` plus client-state apply `1405e6830` / `CraftingUpdateCurrent`, and
`0x056C` as item microchip patch/apply plus `ItemModified`; source still emits
only `ServerCraftingFinish` in current fixed-recipe success paths, with
`CraftingPacketShapeTests`, `CraftingAdditiveHandlerTests`, and
`CraftingSimpleCraftHandlerTests` guarding blocked current-craft/aux emission.
Keep current-craft, aux, and microchip producers blocked pending native
producer timing or accepted live crafting/item-replication capture.

Supplemental update: 2026-06-09 F-012 chat aux / ICComm cached-export recheck
Ghidra MCP discovery reported no running instances. Cached `WildStar64.exe`
fragments reconfirmed `Network_RegisterServerOpcode_0351` (`14006c290`)
binding `0x01B8` to chat row writer/reader `140085af0`/`140085ca0`, `0x01C4`
to envelope writer/reader `140085e30`/`140085fe0`, `0x01C1` to
`1400861b0`/`140086410`, and `0x01EF` to reader-only notification parser
`1400a0890`. Selected callers `140086520` and `140086600` are wrapper helpers
only, and source still finds these packets only in models and packet-shape
tests. Keep chat aux producers and ICComm/chat runtime semantics blocked
pending a native producer/apply path, callback/table owner, dynamic dispatch
proof, accepted packet capture, or two-client social flow evidence.

Supplemental update: 2026-06-08 F-022 / audit F-027 active path helper
static-table guard implemented; active object-id, Explorer explore-zone,
Soldier tower/assassinate, and Settler hub helper paths now treat missing
`PathMission`, `PathSoldierTowerDefense`, `PathSoldierAssassinate`,
`PathSettlerImprovementGroup`, and `PathSettlerHub` static data like missing
rows and return no-progress/zero-progress. Focused PathManager verification
passed 81/81 and broader path verification passed 172/172 from alternate output
directories. Exact path reward presentation, unlock sequencing, current-zone
activation proof, per-mission producer timing, exact Soldier/Settler semantics,
and broader content smoke remain evidence-gated.

Supplemental update: 2026-06-08 F-022 / audit F-027 path mission
static-table guard implemented; `PathManager.CompleteMission()` treats missing
`PathMission` static data like missing mission metadata for direct completion,
while Explorer vista and power-map helpers treat missing `PathMission`,
`PathExplorerNode`, and `PathExplorerPowerMap` static data like missing rows and
return no-progress. Focused PathManager verification passed 72/72 and broader
path verification passed 163/163 from alternate output directories. Path reward
presentation, unlock sequencing, current-zone activation proof, per-mission
producer timing, and broader content smoke remain evidence-gated.

Supplemental update: 2026-06-08 F-004 residence entrance static-data
guard implemented; `GlobalResidenceManager.GetResidenceEntrance()` now treats
missing `HousingPropertyInfo` and `WorldLocation2` tables/rows like missing
residence entrance data and throws the existing `HousingException`;
`ResidenceEntrance` now treats missing `World` table/row the same way. Focused
entrance verification passed 7/7 and broader housing verification passed
121/121 from alternate output directories. Neighborhood `0x0501`/`0x0506`,
edit-mode ack/broadcast, community/session precision, decor ownership/refund
precision, and exact entrance UI/timing remain evidence-gated.

Supplemental update: 2026-06-08 F-014 / audit F-019 creature
DropLoot Creature2 partial-table guard implemented; `GlobalLootManager.DropLoot`
now treats a missing `Creature2` table like a missing creature row before loot
recipient selection, loot-instance creation, or loot notify emission. Focused
creature DropLoot verification passed 2/2 and broader loot verification passed
93/93 from alternate output directories. Standalone `ServerLootCanLoot`, exact
parent/source selection, bind-on-pickup confirmation policy, roll/master UI
parity, and loot aux producer timing remain evidence-gated.

Supplemental update: 2026-06-08 F-022 / audit F-026 global quest-manager
cache partial-table guard implemented; `GlobalQuestManager` now treats missing
`Quest2` tables like an empty quest-info cache, missing `Creature2` tables like
empty quest giver/receiver relation caches, and missing `CommunicatorMessages`
tables like empty communicator, quest-delivery, and quest-state trigger caches.
Null quest/communicator id arrays are also treated like empty arrays while
building those caches. Public getters preserve the existing missing-data
behavior: unknown quest and communicator ids return `null`, and unavailable
relations/triggers return empty sequences. Focused global quest-manager
verification passed 3/3 and broader quest/communicator-adjacent verification
passed 322/322 from alternate output directories. Exact objective guidance UI,
receiver/visibility content smoke, public-event producer timing, path target
semantics, and broader quest/path content parity remain evidence-gated.

Supplemental update: 2026-06-08 F-016/F-021 spell metadata cache
partial-table guard implemented; `GlobalSpellManager` now treats missing
`Spell4`, `Spell4Effects`, `Spell4Telegraph`, `TelegraphDamage`, and
`Spell4Thresholds` tables like empty spell caches, and treats a missing
`Spell4Base` table like an empty spell-base cache. `GetSpellBaseInfo()` now
uses the existing invalid-spell `ArgumentOutOfRangeException` boundary when the
`Spell4Base` table or row is unavailable. `SpellBaseInfo.GetSpellInfo()` now
returns `null` when no `Spell4` tier data exists for a known base spell or when
the requested tier is outside the materialized cache. `SpellBaseInfo` and
`SpellInfo` now resolve secondary spell metadata tables through nullable
lookups, preserving the existing missing-row behavior while avoiding
partial-table startup crashes. Focused spell-manager verification passed 8/8 and
broader spell/action-set/pet verification passed 597/597 from alternate output
directories. Spell aux
producer timing, proc tails, immunity/effect evidence, wrapper lifecycle,
current/action-set update transactions, and retail spell semantics remain
evidence-gated.

Supplemental update: 2026-06-08 F-026 / audit F-033 item-manager
item/item-slot partial-table guard implemented; `ItemManager` now treats a
missing `Item` table like an empty static-item cache and a missing `ItemSlot`
table like an empty equipped-slot index. `GetItemInfo()` continues to return
null for unavailable item rows, and `GetEquippedBagIndexes()` continues to
return an empty sequence for unavailable slot rows. Focused item-manager
verification passed 5/5 and broader item/costume/marketplace-adjacent
verification passed 117/117 from alternate output directories. Item/error aux
producers, exact visual/equip update timing, remaining item eligibility
precision, and deeper costume/unlock lifecycle parity remain evidence-gated.

Supplemental update: 2026-06-08 F-026 / audit F-033 item display-source
partial-table guard implemented; `AssetManager` now treats a missing
`ItemDisplaySourceEntry` table like an empty display-source cache, and
`ItemInfo.GetDisplayId()` treats unavailable display-source rows like an empty
candidate set before returning the existing zero-display fallback. Table-backed
single-row and power-level fallback visual selection remain unchanged. Focused
item-display-source verification passed 6/6 and broader item/costume/marketplace
display-adjacent verification passed 104/104 from alternate output directories.
Item/error aux producers, exact visual update timing, remaining item eligibility
precision, and deeper costume or unlock lifecycle parity remain evidence-gated.

Supplemental update: 2026-06-08 F-022 / audit F-026 quest target-group
cache partial-table guard implemented; `AssetManager` now treats a missing
`TargetGroup` table like an empty creature-target-group cache and like missing
target-group rows during quest-objective target expansion, and treats a missing
`QuestObjective` table like an empty quest-objective target cache.
`TargetGroupCriteriaEvaluator` now preserves the existing missing nested-row
pass-through behavior when recursive target-group criteria are evaluated without
the `TargetGroup` table. Focused target-group verification passed 49/49 and
broader quest/target-group-adjacent verification passed 371/371 from alternate
output directories. Exact objective guidance UI, receiver/visibility content
smoke, public-event producer timing, path target semantics, and broader
quest/path content parity remain evidence-gated.

Supplemental update: 2026-06-08 F-007 / audit F-009 reward-property
modifier-source partial-table guard implemented; `AssetManager` now treats a
missing `RewardPropertyPremiumModifier` table like an empty premium modifier
source while preserving table-backed Hybrid filtering and tier fall-through
behavior. `AssetManagerRewardPropertyTests` pin missing-table, empty-table, and
table-backed modifier grouping boundaries. Focused reward-property verification
passed 8/8 and broader Reward verification passed 172/172 from alternate output
directories. Reward-rotation claim delivery, `0x07CD` apply/flag/throttle
semantics, exact schedule selection, and reward/content context producers remain
evidence-gated.

Scope: matrix rows F-004 through F-015, F-026 through F-033 per `MISSING_FEATURE_MATRIX.md`.

For the 36-row completion snapshot and consolidated emit gaps, see [`CURRENT_STATUS.md`](../../CURRENT_STATUS.md).

Legend: **Partial** = real behavior exists but retail parity incomplete; **Blocked** = evidence gate prevents state mutation; **Verified** = boundary tests added this pass.

## Row Status

| ID | Status | Verification this pass | Primary blockers |
| --- | --- | --- | --- |
| F-004 Housing | Partial | Neighbor handlers + `residence_neighbor` EF + native `ServerHousingNeighbors` `0x0507` row sync + reserved row `+8` semantics + `ServerHousingCommunityDonateUpdate` `0x04FE` emit on donate + neighborhood list `0x0501/0506` packet shapes only + community plot reservation `0x051F` + community placement/privacy `0x053A/053B` emit paths + community rename result `0x078C` + `ServerHousingProperties.Residence.NeighbourhoodId` no longer hardcoded to the old WIP placeholder + interior wallpaper `0x050D` six-slot `HousingWallpaperInfo` id validation/cost debit/decor persistence + private residence visit modify-access teleport guard + housing edit-mode server state on `ResidenceMapInstance` + decor-create colour/scale/plot preflight before debit/mutation + decor-move negative-scale mutation guard + residence entrance static-data guard + neighbor invite prompt/result/update packet coverage; packet-shape and focused housing tests | Housing edit-mode ack/broadcast packet semantics, neighborhood list producer trigger/fields, several neighborhood/community field names, and decor ownership/unlock/refund precision remain unmapped |
| F-005 Marketplace | Partial | `MarketplaceAuctionHandlerTests` + marketplace/account/owned/mail settlement filters; 2026-06-08 marketplace/mail rechecks passed 86/86 and 89/89; durable `marketplace_auction` + `marketplace_commodity_order` EF; `GlobalMarketplaceManager` load/save/search, expiration sweep, same-bidder auction delta escrow debit, unsupported property/rune/equippable auction filters and property sort rejected before ignored search, auction posting now requires owner-matched inventory items that are not soulbound and not equippable bags, `ForceImmediate` commodity non-resting match/refund/return behavior, price-indexed commodity candidate selection, partial-fill buy escrow refund correction, direct multi-order commodity buy fill price-priority split and price-improvement refund, corrupt persisted auction microchip-id format/overflow quarantine, corrupt persisted commodity row validation/quarantine, marketplace mail `ContentType` persistence, huge auction-search page empty-page guard, auction search family/category/type selector-table invalid-request guard, settled auction delete persistence saves the moved item state instead of deleting the item row, auction/commodity insert DB failures roll back transient listing/order/item/escrow state, auction bid update DB failure refunds the bidder and restores prior bid state, marketplace mail persistence failures restore attached item owner state and leave auction buyout/expiry paths uncommitted, item-auction won/return-mail settlement now composes mail creation, attached item save, and auction row deletion in one character DB save for buyout/cancel/expiration mail delivery, item-auction offline seller proceeds now compose auction row deletion, moved item state, and seller credit mail in one required character DB save for direct winner-inventory and auction-won mail delivery, expired offline commodity buy-order refunds now compose order deletion and refund credit mail in one required character DB save, commodity fills now compose order changes with offline seller proceeds and offline buyer price-improvement refund credit mail in one required character DB save, auction bidder refunds now compose with bid updates, auction deletes, or item-return mail saves, commodity sell-order return-mail settlement now composes mail creation, returned-item save, and commodity order deletion for cancel/expiration mail delivery, commodity fill-mail settlement now composes buyer mail creation, purchased-item save, and resting buy/sell order update/delete for mail delivery, online-inventory auction buyout delete-save failure returns `DbFailure` before buyer debit, seller credit, winner notification, auction removal, or item delivery, online-inventory auction cancel delete-save failure returns `DbFailure` before bidder refund, inventory return, or auction removal, online-inventory auction expiration delete-save failure returns before seller credit, winner notification, auction removal, or item delivery, no-DB offline-seller sale attempts stay pending before buyer delivery/debit or auction removal, no-DB offline commodity buy-order expirations leave the order active before escrow is lost, no-DB commodity fills that require offline credits leave matches unfilled before buyer item delivery or notifications, no-DB auction updates/deletes that require offline bidder refunds leave auctions active before new-bid acceptance, buyer delivery, bidder refund loss, or item return, direct commodity buy/sell cancel delete-save failure returns `DbFailure` before escrow refund, item recreation, or order removal, direct commodity buy/sell expiration delete-save failure returns without refund, item recreation, removal notification, or order removal, direct commodity fill update/delete save failure leaves the match unfilled before buyer item delivery, seller credit, buyer price-improvement refund, or fill notification, `ServerAuctionOutbid`, same-save offline credit mail, commodity cross-match + cancel-failure `ServerCommodityOrderResult`, commodity cancel/return mail, auction cancel rollback and delivery-failure non-mutation guards for commodity cancel/fill and auction expiry/buyout, `MarketplaceMailDelivery` + localized mail (`0x3950` -> `278287`), CREDD `0x026A` non-empty price buckets + `097A` cache rows + durable `account_credd_*` history | Apply migrations `20260522114611_MarketplacePersistence` and `20260603120000_MailContentType` on live DB; retail property/rune/equippable auction filter semantics; retail `0x026A` owned-order row pointers; retail commodity partial-stack precision beyond current direct multi-order guard tests; marketplace aux `0x06DF`/`0x07D5` producer/consumer semantics |
| F-006 Storefront | Partial | Storefront/account/pending/CREDD/terminal/velocity/history tests; daily-login claim (`078F` @ `1400070f0`); purchase-history request (`082E` = `StorefrontLib.RequestHistory` @ `1404f1d50`) now returns `098E`; VC package Lua registration targets mapped-only (`PurchaseVirtualCurrencyPackage` @ `1404f1470`, `CompleteOrderVirtualCurrencyPackage` @ `1404f1550`); coupon handler on `0790` wire; CREDD redeem (`0268`/`097B`); `0x026A` header+buckets (`14042b9a0`); `097A` cache rows; purchase success emits `098C`/`098D` (client `082A`/`0828`); `096A..096C` unused leading field (emit 0); catalog updated (`0989`); unlock sync (`0983`/`0984`); character-select direct account purchases apply entitlement/currency grants immediately and refresh the character list; in-world direct account inventory claims handle targeted character rows with character-slot cap prerequisites enforced by entitlement max-count; missing account-inventory-manager requests fail closed for take, pending claim/return/gift, daily-login claim, and coupon redemption; daily-login reward refresh/claim paths treat a missing `DailyLoginReward` table as an empty schedule before inventory checks or grants; account-item cooldown startup treats a missing `AccountItemCooldownGroup` table as an empty configured list while preserving persisted cooldown rows; account-item existence/materialization paths treat a missing `AccountItem` table like missing account-item rows; entitlement-backed account-item grants treat a missing `Entitlement` table like missing entitlement rows before mutation; account and character entitlement managers treat a missing `Entitlement` table like missing entitlement rows during persisted load and direct updates before mutation or entitlement packet emission; account-currency load/materialization treats a missing `AccountCurrencyType` table like missing account-currency rows, preserving persisted balance readback and failing new balance creation before mutation or wallet packets; account-item generic-unlock claims reject missing `GenericUnlockSet` / `GenericUnlockEntry` tables before unlock grant application or item deletion; storefront purchases reject type-1/type-2 offer-item rows before charge/delivery and only grant mapped type-0 account-item rows; velocity gate (`0971`+`account_store_purchase_history`); migrations `120000`/`130000`/`140000` in tree; focused entitlement-manager verification passed 4/4, entitlement-adjacent verification passed 20/20, focused account-currency verification passed 4/4, loot-bag/account-currency verification passed 16/16, and broader account verification passed 140/140 on 2026-06-08 | Real-money/Protobucks VC billing request/confirm packet path; `0x026A` owned-order tail; live `0986`/`098F` emit; non-zero `096A..096C` leading-field producers; type-1/type-2 offer-item purchase effects; native coupon sender for `0790` |
| F-007 Rewards | Partial | Per-content schedule catalog; `account_reward_rotation_grant` migration; `AccountRewardRotationGrantManager` + claim `RecordGrant` path; non-empty `0x07C8` on refresh/claim; 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance and kept `0x07CD` mapped-only: cached `14008fcb0` proves the content-context reader, `140635840` proves seven throttle-slot defaults, `140636ba0` sends only index-only `0x07CC` refresh requests, and `140636c40` proves loaded-schedule lookup, not apply semantics; 2026-06-06 refresh now uses player level, expands normal/veteran rows, and writes the native schedule row order; reward-property premium modifiers and spell reward-property modifier effects now fail closed when `RewardProperty`/entitlement static tables are unavailable; focused reward-property tests passed 5/5 and broader reward/spell tests passed 375/375 | `0x07CD` apply/`Flag` consumer and throttle-slot assignment require retail capture or dynamic dispatch proof; item/currency/property delivery after claim; exact retail difficulty/content reward selection |
| F-008 Crafting | Partial | `CraftingLootIdCraftHandlerTests` pins LootId craft path; simple/craft-item tests now pin fixed-recipe zero-station success, non-zero station/tradeskill mismatch rejection, saturated inventory material-count availability without `uint` overflow, and missing schematic/item/material/tier table behavior; missing schematic/item tables reject before mutation, missing material tables use inventory-only material debits, and missing tier tables complete with zero XP; `TradeskillRequestHelperTests` pin missing `Tradeskill` / `TradeskillBonus` / `TradeskillTalentTier` table rejection; `CraftingAdditiveHandlerTests` pins additive zero-station rejection and queued modifier materialization guards; `CraftingPacketShapeTests` pin `ServerCraftingCurrentCraft`, native `CodeEnumTradeskillResult` and `CodeEnumRuneType` values, native `GetRuneSlots` live item-data offsets, and corrected typed `0x084B`/`0x0855` auxiliary payloads; `CraftingDiscoveryEvaluatorTests` pins coordinate-discovery distance bands, attempt-coordinate parsing, and station service-key mapping (`0x2C`/`0x4F`/`0x57`); `ItemRuneSocketTests` plus `item.runeSlots` migration, `ItemRuneSlotsCodec`, and `ItemRuneNetworkWire` pin durable rune socket/install persistence, shared-item wire population, and missing `Item`/`Item2Category`/`ItemSpecial` table behavior; rune/additive helpers reject missing `Item`, `TradeskillAdditive`, and `TradeskillCatalyst` tables through existing invalid request paths, and `CraftingModifierSessionStore` does the same before inventory checks/debits when building queued additive/catalyst item counts. Focused partial-table verification passed 12/12, focused tradeskill request verification passed 5/5, focused rune/modifier verification passed 63/63, and broader crafting verification passed 90/90 on 2026-06-08 | Live validation of attempt-coordinate packing; profession-modifier scaling for discovery radii; `ServerCraftingCurrentCraft` mid-craft emit cadence; aux opcode emit intent; non-success sigil result rules; any separate microchip install mutator precision |
| F-009 Transport | Partial | `TaxiRoute.tbl` default loading and clean missing-table rejection; rapid transport missing/empty spell-formula data now returns `RapidTransportInvalid` before cooldown, debit, or cast; captured table-backed rapid route selection/credit debit/cast context; contiguous flight-path route charging and destination teleport; rapid transport / flight-path / vehicle packet tests; vehicle embark/disembark handler-boundary tests; focused transport verification passed 73/73 | Service-token bypass; global route state; taxi embark/completion; broader charge/teleport parity; passenger/seat modes; deployable vehicle semantics; `Server0x077E` |
| F-010 Group/matching | Partial | `GroupLootRulesHandlerTests`; `ServerGroupInstanceDifficultyResponse` (`0x0414`) typed + authoritative group-server path on `ClientGroupSetInstanceDifficulty` with leader-only mutation, cached group `InstanceDifficulty`, online-member sync, and shared fan-out helper coverage; ready-check status (`0x0441`) now emits for Pending/Ready/HasSetReady flag updates, with `Group.StartReadyCheckAsync` deliberately clearing ready flags in-memory before the single pending update to avoid duplicate messages; group flag fan-out reuses one immutable role/flag packet and one ready-check packet per update; replacement LFR `0x05D5`/`0x0602` validation/logging only; `ServerRaidQueueStatus` (`0x0718`) zero-value compatibility emit with raid-info plus non-zero wire-order guard (`GroupPacketShapeTests`); `ServerMatching0x05CF` raw `uint32` shape plus correlated-but-unproven apply candidate `1405c41c0` are tracker-aligned as mapped-only; `Client0x062A/0634` handlers are log-only/test-pinned; 2026-06-06 live CDB smokes correlated client-owned `0x05EF` queue submit / `0x05B4` leave-all helper sends, `0x05CA` match-ready apply, and `0x05C8` ready responses for false declines plus a true accept | Non-zero raid queue semantics; `ServerMatching0x05CF` apply-table index/field semantics; `Client0x062A/0634` sender meaning; durable raid locks; replacement server backfill/merge lifecycle |
| F-011 Guild/war party | Partial | War-party boss-token protocol tests now pin `ClientWarPartyBossTokensRequest`, `ServerWarPartyBossTokens`, and the 18-bit `ClientCastGuildBossToken` item id boundary from cached `0x094F` writer `140091a80`; `ServerGuildBankTabInventory` (`0x047A`) row-count writer/test; guild bank money/item transaction and tab-open handlers are log-only/no-emit test-pinned; 2026-06-09 cached-export/source recheck maps recruitment `0x049F` and `0x0767..0x076F` packet shapes to current source/tests, with `0x076E` sender `140584840` reached from Lua wrapper `14069e5c0`; 2026-06-06 live smoke verified DB/server guild membership setup for `Joy Ner` in `Nexus`; widened Guild UI CDB captured client-owned `0x04A8` bank-money deposit sends and `0x04B1` bank-management/log operation send | Guild bank economy/perks/holomarks; recruitment persistence, subscribe/update timing, list/detail population, and availability semantics; boss-token inventory, cast acceptance/results, match-results population, and warplot plugs; buy-tab proof blocked by missing guild influence/bank-tab state |
| F-012 ICComm/chat | Partial | ICComm/friendship/chat tests; scoped group/guild ICComm channels revalidate affiliation on send/update, pruning stale senders/recipients before transient delivery; auto-response and ignore-strangers are pinned as transient social-option behavior; chat aux `01B8/01C1/01C4/01EF` packet contracts are covered; chat item/quest links now treat missing `Item` and `Quest2` tables like missing rows before appending text or format rows; focused chat-link verification passed 6/6 and broader chat verification passed 11/11; `Client0x0550` rechecked as a one-`uint32` diagnostic-only shared-writer row, with ICComm/spell-list/matching/tradeskill/ability aliases rejected until opcode-specific evidence appears | Persistent channels; entitlement checks; durable social-option persistence/readback; aux chat producer/runtime semantics; exact chat-format retail text policy; `Client0x0550` runtime indirect sender, post-read consumer, or live ICComm/spell-list/ability capture |
| F-013 Mail | Partial | Delete-from-view + `ServerMailUnavailable`; UTC instant expiry + `ServerMailItemDeprecation`; marketplace `ServerMailAvailable` online notify; non-player/zero-sender return rejection; marketplace item-delivery mail fallback gating; marketplace mail `ContentType` persistence with legacy sender-type fallback; marketplace mail persistence failures restore attached item owner state before reporting delivery failure; item-auction won/return-mail and commodity return/fill-mail settlement can compose an additional marketplace save action in the same character DB save; item-auction offline seller credit mail is composed with sale deletion/item save for direct and mail delivery; expired offline commodity buy-order refund mail is composed with order deletion; commodity-fill offline seller proceeds and offline buyer price-improvement refund mail compose with order mutation; auction bidder refund mail composes with bid/update/delete and item-return saves; 2026-06-08 marketplace/mail rechecks passed 86/86 and 89/89; F-005 direct commodity fill/bidder-refund guard coverage now lives in `MarketplaceAuctionHandlerTests` | Save/reload delete edge cases and exact expiration client behavior |
| F-014 Loot | Partial | BoP two-step collect + `ServerLootBindOnPickup` `OwnerUnitId`/`LootUnitId`; `CanTryRoll`/`CanTryMasterAssign`; roll/master row-state `ServerLootItemUpdate` coverage; direct/vacuum/deferred pickup now suppresses delivery-time `ServerLootItemUpdate` after live `#2208` evidence while preserving grant/notification/remove flow; non-looter request guard; duplicate loot amount overflow rejection; single-stack no-charge loot-bag delete-failure guard before generated reward delivery; generated loot account-currency/account-item/virtual-item reward-table invalid-item guard; creature DropLoot missing `Creature2` table no-drop guard; corpse group recipients/tracked looters/master candidates filtered to in-range players; atomic high-bit loot-unit id allocation that skips zero; offline master-loot assignee rejection before winner resolution; mixed loot delivery reports incomplete success, preserves delivered rows, keeps failed rows retryable, and suppresses complete generated granted-notify; fully delivered loot instances are removed immediately after final resolution instead of waiting for the expiry sweep; owner/looter indexes back active loot lookups and vacuum; focused loot tests passed 87/87, 91/91, and 93/93 | `ServerLootCanLoot` emit timing; parent source tracking |
| F-015 PvP/duels | Partial | Duel lifecycle tests; active duels now send left-area/cancel-warning packets and cancel after the leash timeout; cancelled/declined duel results emit zero winner/loser ids; `WorldSession.OnDisconnect` routes to `DuelManager.OnPlayerDisconnect` and active disconnect cancellation is test-pinned; PvP toggle-off keeps `PvPFlag.Enabled` active until the pending cooldown expires and persists the pending expiry in `character.pvpFlagDisableUntilUtc` across logout/restart; current open-world player-vs-player attackability is explicitly duel-only and pinned by `PvpCombatBoundaryTests`, while instanced PvP match combat remains a separate content-map path; focused PvP/adventure verification passed 29/29 on 2026-06-07 | Observer/reward/stat parity; broader open-world PvP rules beyond duels need retail proof before widening |
| F-026 Items/unlocks/costumes/pets | Partial | Generic unlock lifecycle, including consume-failure invalid result with no unlock grant, persisted-row missing-table skips without DB mutation, account-item generic-unlock claim partial-table rejection before grant/delete, learn-dye-color partial-table delegation to the existing invalid unlock path, collection-spell missing-`Spell4` rejection before spell grant/unlock packet, pet-flair missing-table rejection before flair mutation, title spell-effect missing-table rejection before title mutation, title manager persisted-load/add/revoke missing-table guards plus empty `AddAllTitles` fallback, and pet customisation persisted-load missing-table skips/zeroed saved flair slots plus zero-flair clears without `PetFlair` static data; item/costume aux packet-shape coverage; costume unlock rejects non-equippable inventory items before account unlock, soulbind, or success result; normal `ClientItemUse` activation consumes only after `TryCastSpell == Ok` and now treats missing `ItemSpecial` tables like missing rows before cast/consume; currency-treasure item use treats missing `CurrencyType` tables like missing rows before consume/grant; decor item-use consumes only after residence preflight and `Inventory.ItemUse`; repair-vendor requests treat missing repair-cost `GameFormula` data as zero cost before affordability, debit, or durability mutation; pet stance requests update owned server pet state without emitting blocked `0x068F`; pet customisation rejects unsupported pet types and invalid flair slots with `ServerPetCustomisationFailed` before manager mutation; supply satchel stack-limit fallback/clamp/over-cap, packet-capacity, unmapped item/material, and stale conversion guards; `0x056B`/`0x056C`/`0x056D` item-data aux readers and client microchip/glyph apply helpers are mapped, and the 2026-06-09 cached recheck found no producer evidence; focused generic-unlock verification passed 22/22; focused spell collection verification passed 9/9; focused account-item/generic-unlock verification passed 67/67; focused title manager verification passed 6/6; title / spell collection verification passed 33/33; broader generic-unlock / pet / title / spell collection verification passed 343/343; focused item-use verification passed 9/9; broader item-use verification passed 20/20; focused vendor verification passed 10/10; focused pet customisation manager verification passed 3/3; focused pet verification passed 293/293; focused supply-satchel verification passed 8/8 | Item/error aux producer semantics; remaining item eligibility precision; repair durability-update producer timing; supply-satchel aux producer semantics; unlock list deltas; pet flair ownership/object/name validation; pet lifecycle producer timing/scope |
| F-027 Options | Partial | `OptionPersistenceTests`; `KeybindingSetTests` cover clear-before-save mutation; character-scoped keybinding request/update packets reject non-current character ids before readback, mutation, or response enqueue; focused option verification passed 35/35 and option/support bucket verification passed 160/160 | Broader account-level option split; option readback/init; `056B..056D` item-data producer timing remains under F-026 |
| F-028 Support | Partial | Stuck failures via `ServerSpellCastResult` + spell4 ids; recall-house uses global residence entrance; missing `WorldLocation2` table returns prerequisite failure without teleport/cooldown; `SupportPacketShapeTests` pin typed `0x0347..0x0351` auxiliary payloads including `0x034F` value/flags padding and `0x034C` 27-bit tail padding; source/protocol recheck found no modeled support-case list/read request or server readback payload; focused Support-filter verification passed 126/126 | DB case workflow, support-case readback protocol/backend, and moderation tooling |
| F-030 Realm transfer | Partial | `RealmTransferProtocolTests` pin unknown/offline/online compatibility results and PTR diagnostic-only handlers; `ServerRealmTransferDestinationsAux` (`0x03EF`) is reader-backed as `uint32` plus counted raw bytes, and the 2026-06-09 cached recheck found registration/reader evidence only; the 2026-06-09 PTR handoff recheck found `0x06E7`/`0x06E8` client senders and `0x06EA` Lua-event consumer evidence, but no native `ServerPtrCharacterCopyQueued` producer or copy mutation | Real destinations/success results; `Client0x0760/0762`; `0x03EF` raw payload semantics/producer timing; PTR queue/copy handoff producer and mutation proof |
| F-031 Fortune | Partial | Fortune coin cost; emulator rarity-tier `FortuneRewardPool` + `RewardItemProbabilities`; missing `AccountItem` table returns an empty catalog/card pool and missing `Item2` table uses Normal rarity fallback; `ServerFortuneReset.ResetCode` value `3` mapped to click-empty reset; `account_fortune_session` persistence code path; target-scoped Fortune Coin account-item auto-claim guard; `FORTUNE_WEIGHT_AUDIT.md` catalog/table audit; F-007 `RewardRotation*` schedules rejected as Fortune active-rotation evidence; focused reward-pool verification passed 6/6 and broader Fortune verification passed **27/27** | Exact per-item retail weights and active rotation catalog (no tbl weight column, no local live Fortune capture or storefront-server catalog dump) |
| F-032 Leaderboards | Partial | `DatabaseLeaderboardStore` + `LeaderboardProvider` + `LeaderboardScoreIngestion` + aggregation; per-scope/category cache limiting; duplicate persisted scores dedupe to the best score before row caps and visible ranking; PvE type/map/prime and PvP arena/battleground-class scoping; database-unavailable requests return empty without caching; source/protocol recheck found no mapped season or medal request selector; focused leaderboard verification passed 17/17 | Season and medal-filter semantics remain blocked until a selector is decoded or captured; exact retail row caps/refresh cadence; broader live score ingestion coverage |
| F-033 Challenges | Partial | `ChallengeManager` lifecycle + `character_challenge` persistence; retail two-active-challenge cap; combat kill direct/nested target-group hooks; `QuestObjectiveType.CompleteChallenge` hook; missing `Challenge`/`ChallengeTier` table guards; large progress deltas clamp at tier goal without `uint` wrap; source/protocol recheck found no mapped challenge reward-track runtime/packet surface or client share-init request; focused challenge verification passed 45/45 | `Client0x00C8` decode; share-init ownership; reward tracks/medal win-chance; full result/reward parity |

Recent F-006 closure note (2026-06-08): storefront offer-item data construction now treats a missing `AccountItem` table like missing offer account-item rows before store item-data packet rows are built. Focused offer-item verification passed 3/3 and broader Storefront verification passed 45/45; Protobucks/VC request-confirm, owned-order tails, live `0986`/`098F`, `096A..096C` producers, type-1/type-2 offer-item effects, coupon native sender proof, exact catalog/dirty producer timing, offline transfer persistence, TTL/mail fallback, and coupon-aware routing remain evidence-gated.

Recent F-014 closure note (2026-06-08): creature `DropLoot` now treats a
missing `Creature2` table like a missing creature row before recipient
selection, loot-instance creation, or loot notify emission. Focused creature
DropLoot verification passed 2/2 and broader loot verification passed 93/93;
`ServerLootCanLoot`, exact parent/source selection, bind-on-pickup confirmation,
roll/master UI parity, and loot aux producer timing remain evidence-gated.

Recent F-014/F-026 closure note (2026-06-08): direct character-currency manager affordability, add, and subtract requests now treat a missing `CurrencyType` table like missing currency rows before currency-state mutation or currency update packets. Focused character-currency verification passed 7/7 and broader character-currency / loot-adjacent verification passed 32/32; exact retail currency chat/floater policy, item/error aux producers, and loot aux producer timing remain evidence-gated.

Recent F-011 closure note (2026-06-08): guild standard part construction and dye validation now treat missing `GuildStandardPart` and `DyeColorRamp` tables like missing guild-standard rows before guild registration accepts a standard. Focused guild-standard verification passed 5/5 and broader Guild verification passed 13/13; guild bank economy, influence/tab state, perks, holomark producer precision, recruitment, war-party/warplot, and exact standard UI timing remain evidence-gated.

Recent F-021 closure note (2026-06-08): direct and persisted `ActionSet.AddAmp` paths now treat missing `EldanAugmentation` tables like missing AMP rows before owner-save or AMP-list mutation. Focused AMP/action-set verification passed 28/28 and broader Spell verification passed 221/221; `UpdateSpellInProgress`, async spell-update transaction, exact lock/spec sequencing, attribute allocation/refund, bonus ability/AMP unlock persistence, and ability-book activation edges remain evidence-gated.

Recent F-026 closure note (2026-06-08): costume save now treats unavailable `ItemDisplay` / `DyeColorRamp` static data as invalid nonzero dye data, returning `CostumeSaveResult.InvalidDye` before costume mutation or success packets, and `CostumeItem.GenerateDyeMask()` rejects unknown nonzero dye ramps instead of null-refing. Focused costume save verification passed 9/9 and broader costume / packet-shape verification passed 18/18; costume aux producer semantics, exact UI timing, and deeper costume lifecycle parity remain evidence-gated.

Supplemental update: 2026-06-09 F-026 costume aux `0x037F`
cached-export/source recheck kept `ServerCostumeItemAux` mapped-only.
`Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x037F` size
`0x18` to `ServerCostumeItemAux_ReadPayload` (`1400874a0`), which reads one
14-bit value, three `uint32` fields, and two flags. Selected xrefs are
label-only, selected call edges are reader-local, adjacent `ClientEmote`
(`0x037E`) and costume result event helpers are separate positive controls, and
source finds the packet only in the model, opcode enum, and placeholder tests.
Keep the fields neutral and non-emitted pending a native producer/apply owner or
accepted costume/emote capture.

Supplemental update: 2026-06-09 F-026 supply-satchel aux `0x019A`
cached-export/source recheck kept `ServerSupplySatchelAux` mapped-only.
`Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x019A` size `8`
at `selected_decompiled.c:12829` to shared
`MatchingQueueResultWaitTime_ReadPayload` (`14007fcf0`), which reads one 6-bit
field and one `uint32`. Selected xrefs are label-only, selected call edges are
reader-local, and the same reader's `0x00E6`/`0x0616`/`0x0623` character-delete
and matching positive controls do not prove supply-satchel producer semantics.
Source still emits `ServerSupplySatchelUpdate` (`0x0199`) for stack updates and
finds `ServerSupplySatchelAux` only in the model, opcode enum, and placeholder
tests. Keep the fields neutral and non-emitted pending a native producer/apply
owner, accepted supply-satchel capture, or broader item/supply readback proof.

Recent F-004 closure note (2026-06-08): residence visual setters now treat missing `HousingWallpaperInfo` / `HousingDecorInfo` tables like missing static rows, throwing `ArgumentOutOfRangeException` before value mutation or `NeedsSave`; persisted residence decor load preserves `DatabaseDataException` when wallpaper/decor static tables are unavailable instead of null-refing. Focused residence verification passed 15/15 and broader residence/interior-wallpaper/vendor-list/decor item-use verification passed 27/27; edit-mode ack/broadcast, neighborhood list producer fields, decor ownership/unlock/refund precision, exact visual UI timing, and community/session semantics remain evidence-gated.

Recent F-004 closure note (2026-06-08): residence entrance resolution now treats missing `HousingPropertyInfo`, `WorldLocation2`, and `World` static data through the existing `HousingException` boundary before teleport helper construction. Focused entrance verification passed 7/7 and broader housing verification passed 121/121; edit-mode ack/broadcast, neighborhood list producer fields, decor ownership/unlock/refund precision, exact entrance UI timing, and community/session semantics remain evidence-gated.

## Next implementation gates (priority order)

1. ~~Ghidra field proof for remaining group cluster `0x042A..0x0718` and wire emitters.~~ **Done:** cluster typed + emitters; `0x0441` ready-check status emits on Pending/Ready/HasSetReady updates.
2. Apply auth/character migrations on live DB when missing: `20260522120000_AccountPendingItem`, `20260522130000_AccountCREDDExchange`, `20260522140000_AccountDailyLoginAndStoreHistory`, `20260522150000_AccountRewardRotationGrant`, `20260522190000_AccountFortuneSession`, `20260522190000_CharacterChallenges`, `20260522114611_MarketplacePersistence`.
3. ~~Challenge DB persistence and combat objective hooks~~ **Done in tree** (`character_challenge`, `ChallengeCombatHooks`); remaining: `Client0x00C8` decode, share-init ownership, and reward tracks/medal win-chance are blocked by missing mapped protocol/runtime surfaces.
4. Fortune per-item retail weights (rarity-tier weights + UI probability transport implemented; live retail `ServerFortuneRewards` or storefront-server catalog capture still needed; F-007 `RewardRotation*` schedules are a rejected source).
5. ~~Leaderboard DB and request-scope cache limiting~~ **Done in tree** (`DatabaseLeaderboardStore`); remaining: season rules and medal-filter semantics are blocked by missing mapped request selectors; exact row caps/refresh cadence and broader ingestion hooks also need evidence.
6. Spell families F-016..F-020 (family-by-family evidence ladder).
7. Consolidated emit gaps in `CURRENT_STATUS.md` (entity-stat aux, map-tracked units, housing `0x0501/0506`, crafting `0x084B/0x0855`/discovery).

## Tests added (2026-05-22)

- `Source/NexusForever.Game.Tests/Crafting/CraftingLootIdCraftHandlerTests.cs`
- `Source/NexusForever.Game.Tests/Group/GroupLootRulesHandlerTests.cs`
- `Source/NexusForever.Game.Tests/Loot/LootRequestHandlerTests.cs` (notify, collect, vacuum)

Focused F-004 gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingCommunityUpdateHandlerTests|FullyQualifiedName~ClientHousingCommunityRenameHandlerTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ClientHousingVisitResidenceHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"` -> **68 passed**.

Interior wallpaper focused gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"` -> **59 passed**.

Housing visit/decor-create hardening gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Housing" -v minimal --nologo` -> **93 passed**.

Housing decor-move scale guard:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin\ --filter "FullyQualifiedName~ResidenceMapInstanceInteriorWallpaperTests" -v minimal --nologo` -> **4 passed**.

Housing residence entrance static-data guard:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin-housing-entrance-focused\ --filter "FullyQualifiedName~GlobalResidenceManagerTests" -v minimal --nologo` -> **7 passed**.
Broader housing entrance guard:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin-housing-entrance-broad\ --filter "FullyQualifiedName~Housing" -v minimal --nologo` -> **121 passed**.

Loot-bag single-stack delete-failure guard:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin\ --filter "FullyQualifiedName~Loot" -v minimal --nologo` -> **87 passed**.

Keybinding character-scope guard:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin\ --filter "FullyQualifiedName~Option" -v minimal --nologo` -> **35 passed**.

Costume unlock non-equippable guard:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin\ --filter "FullyQualifiedName~AccountCostumeManagerTests|FullyQualifiedName~ClientItemGenericUnlockHandlerTests|FullyQualifiedName~AccountUnlockPacketShapeTests|FullyQualifiedName~ClientPetSetStanceHandlerTests|FullyQualifiedName~SupplySatchelManagerTests|FullyQualifiedName~ClientItemUseHandlerTests|FullyQualifiedName~ClientItemUseDecorHandlerTests" -v minimal --nologo` -> **34 passed**.

Options/support bucket gate:
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin\ --filter "FullyQualifiedName~Option|FullyQualifiedName~Support" -v minimal --nologo` -> **160 passed**.

Group instance difficulty authority gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Group" -v minimal --nologo` -> **155 passed**.

ICComm scoped-membership gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -p:UseAppHost=false -p:OutDir=I:\GIT\NexusForever\artifacts\codex-test-bin\ --filter "FullyQualifiedName~ICCommManagerTests|FullyQualifiedName~FriendshipSocialOptionTests|FullyQualifiedName~PacketPlaceholderNamingTests.ServerChatAux" -v minimal --nologo` -> **12 passed**.

Earlier broad workstream filter before the housing-neighbor cluster covered **544 passed**.

Current full game test gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` -> **943 passed** (2026-05-23).
