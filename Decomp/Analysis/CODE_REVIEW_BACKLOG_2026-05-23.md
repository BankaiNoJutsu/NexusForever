# Code Review Backlog - 2026-05-23

Plan-only backlog from parallel review of recently modified game logic, network packets, WorldServer handlers, and quest scripts.

**Status audit:** 2026-05-23 (second pass vs `RETAIL_FEATURE_EVIDENCE.md` + current tree). Key commits: `ad2c11f6`, `0d785f25`, `41ab84f8`, `ac95f25c`, `de275ee5`. Current review corrected loot bag ordering to preflight, consume, then grant, and kept crafting discovery rolls blocked until server-side thresholds are mapped.

## Fix status summary

| Status | Count | Notes |
|--------|-------|-------|
| **Fixed** | 18+ | See table below |
| **Partial** | 2 | Housing edit ack/state; marketplace settlement edge cases outside cancel escrow |
| **Retail-aligned (not bugs)** | 1 | Grey/trash group auto-roll is **Certain** retail (Strain 1.0.9) - see crosswalk below |
| **Open** | Marketplace P0, loot sync packets, soulbind scope, harvest **group** rule vs housing harvest |

### Fixed since backlog was written

| Backlog # | Item | Evidence |
|-----------|------|----------|
| 1, 5 | Loot bag consume/delivery ordering | `TryUseLootBag` now preflights delivery, consumes the item, then grants loot; `ItemUse` failure no longer grants duplicate rewards. |
| 2 | Commodity buy escrow | `ValidateCommodityOrder` requires `Price == PricePerUnit * Quantity` with overflow guard. |
| 3 | Auction cancel rollback | `CancelAuction` verifies inventory/mail return before removing listing and refunding bidder. |
| 4 | Commodity sell cancel double mail | `CancelCommodityOrder` decides one return path and mails at most once. |
| 6 | Soulbind matching stacks | `SoulbindDeliveredStaticItems` only soulbinds newly-created matching item GUIDs. |
| 7 | Q3487 achievement throw | `HasCompletedAchievement` guard in `Q3487Shellshock.cs` |
| 21 | `MemberIndex` on group flags | `GroupMemberFlagsUpdatedHandler.cs` sets `message.Member.GroupIndex` |
| 22 | Ready-check clear / stale UI | Handler always sends `ServerGroupReadyCheckStatusUpdate` (status 0 when flags cleared) |
| 28 | Q3487 cannon no quest gate | `GetQuestState(3487) == Accepted` in `OnActivateSuccess` |
| 29 | Q3487 map fallbacks | `NorthernWildsMapScript` spawns cannon 11251 + ultrabot 12526 |
| 31 | Manual `GrantNext()` | Q3479, Q3667, Q3886, Q3487 use `FollowUpQuestScript<T>` |
| 41 | `FireAndForgetAsync` on group flags | `ClientGroupFlagsChangedHandler.cs` |
| 18 (partial) | Duel leash packets | `DuelManager.UpdateActiveDuelLeash` - `ServerDuelLeftArea` / `ServerDuelCancelWarning` / timeout cancel |
| 6 (partial) | Offline master loot | `AssignMasterLoot` returns `false` + warn when assignee offline |
| 7 (partial) | Offline roll winner | `FinaliseRoll` logs warn and keeps the item assigned for deferred collection before corpse expiry. |
| D-L1-D-L4 | Loot sync packets | Delivery, roll, master assignment, expiry, and corpse-empty paths now broadcast update/remove/notification packets to the looter audience. |
| D-L7 (partial) | Loot handler throws | `GlobalLootManager` GiveLoot/Roll/Assign log+return; throws remain inside `LootInstance` |
| - | Commodity expiration sweep | `ProcessExpiredCommodityOrders` (`ad2c11f6`) - aligns F-005 mail settlement |
| - | Deserter cross-queue | `MatchingQueueValidator` calls `MatchingDeserterManager.CanQueue` - RETAIL crosswalk was **Partial**, now wired |
| - | Housing plug harvest split | `RetailHousingHarvestSplit` / `RetailHousingHarvestGrant` - RETAIL F-004 **Partial**->better than group `HarvestRule` gap |
| - | Trash roll policy | Uses `RetailCertainRules.TrashItemMaxQualityId` - **Aligned** with F-014 Certain |
| - | Housing permission check | `ClientHousingEditModeHandler` uses `CanModifyResidence` (`ad2c11f6`) |
| - | Matching random party | Routed to `IMatchingManager.JoinRandomPartyQueue` (`ad2c11f6`) |
| Survey 0x034E | Missing 5th header | **Not a bug** - row count is the fifth uint32 |

### Partial / broken (needs follow-up)

| Backlog # | Item | Current state |
|-----------|------|---------------|
| D-L6 | Trash auto-roll in groups | **Not a bug** - `RETAIL_FEATURE_EVIDENCE.md` lines 220-221 (Strain 1.0.9); code uses `RetailCertainRules.TrashItemMaxQualityId`. |
| 26 | Housing edit mode | Validates map + residence permission; still **no server edit state / ack / broadcast** |

### Still open (high signal)

| Backlog # | Item |
|-----------|------|
| 8-11 | Group/loot range UI, indexing, aggregation, partial delivery ergonomics |
| 12-17, 19-20, 23-25, 32-43 | See sections below |

---

## How to use this list

| Priority | Meaning |
|----------|---------|
| **P0** | Likely player-visible bug or data loss; fix before relying on the feature |
| **P1** | Wrong behavior, stale UI, or scalability pain at modest load |
| **P2** | Maintainability, test gaps, or polish |

Categories: **Bug** / **Optimization** / **Refactor** / **Test**

---

## P0 - Fix first

| # | Area | Item | Category |
|---|------|------|----------|
| 1 | `GlobalLootManager.TryUseLootBag` | **FIXED:** preflight delivery, consume, then grant; `ItemUse` failure no longer grants loot. | Bug |
| 2 | `GlobalMarketplaceManager` | **FIXED:** commodity escrow must equal `PricePerUnit * Quantity` and overflow is rejected. | Bug |
| 3 | `GlobalMarketplaceManager` | **FIXED:** auction cancel verifies return inventory/mail before removing listing and refunding bidder. | Bug |
| 4 | `GlobalMarketplaceManager` | **FIXED:** commodity sell cancel sends at most one return mail and only removes after return path succeeds. | Bug |
| 5 | _(duplicate of #1)_ | See #1. | - |
| 6 | `LootInstanceItem` | **FIXED:** soulbind scope is limited to newly-created matching item GUIDs. | Bug |
| 7 | `Q3487Shellshock` | Achievement 1296 throw. **FIXED** (`HasCompletedAchievement`). | Bug |

---

## P1 - Gameplay & economy

### Loot (`GlobalLootManager`, `LootInstance`)

| # | Item | Category |
|---|------|----------|
| 4 | Group loot falls back to solo when `eligible.Count <= 1`, even if killer is in a multi-member group on the same map-other members get no notify/rolls. | Bug |
| 5 | Killer force-added to `eligible` when outside `LOOT_RANGE` can get loot UI but fail `GiveLoot` until in range. | Bug |
| 6 | `AssignMasterLoot` returns `true` when assignee is offline; item stays on corpse. | Bug |
| 7 | `FinaliseRoll` leaves loot stranded if roll winner is offline (no `DeliverItem`, no `MarkDeliveredWithoutWinner`). | Bug |
| 8 | `lootInstances` is a flat `List` with linear scans and frequent `.Where(...).ToList()` on hot paths. Index by map/entity or shard by zone. | Optimization |
| 9 | `Update` walks every live instance every tick; delivered corpses linger until expiry sweep. | Optimization |
| 10 | Duplicate aggregation/overflow logic in `TryGenerateItemLoot` vs `TryGenerateLoot`. | Refactor |
| 11 | `DeliverAllLoot` partial success vs bag delivery semantics-align with #1. | Bug / Refactor |

**Tests to add:** post-consume delivery failure; group-in-range edge case; offline master/roll winner.

### Marketplace (`GlobalMarketplaceManager`)

| # | Item | Category |
|---|------|----------|
| 12 | `Persist` / `Initialise` no-op when DB context is null-silent divergence from memory. | Bug |
| 13 | `DeserializeUnknownArray` uses `uint.Parse`-corrupt DB row can break world startup. | Bug |
| 14 | `SearchAuctions` materializes and sorts all matches under `syncRoot` before paging. | Optimization |
| 15 | `TryMatchCommodityOrder` nested scan over all opposite orders per post. | Optimization |
| 16 | `Persist` uses `GetAwaiter().GetResult()` while holding `syncRoot`. | Optimization |
| 17 | `Initialise` skips auctions with `model.Item == null` without logging. | Refactor |

### PvP (`DuelManager`)

| # | Item | Category |
|---|------|----------|
| 18 | "Leash" is **inter-duelist distance** (120 m), not "left duel area"; comment at ~L229 does not match code. | Bug |
| 19 | Cancel/timeout still sets winner/loser GUIDs on `ServerDuelResult`. | Bug (client UX) |
| 20 | No explicit disconnect hook; stale sessions until `Update` invalidates. | Bug |

**Tests to add:** leash semantics; disconnect cleanup.

---

## P1 - Network & handlers

### Group (`GroupMemberFlagsUpdatedHandler`)

| # | Item | Category |
|---|------|----------|
| 21 | `MemberIndex` never set. **FIXED** | Bug |
| 22 | Ready-check clear stale UI. **FIXED** (always sends 0x0441). | Bug |
| 23 | Non-promotion updates always send `ServerGroupMemberRoleChange` (including disconnect flags)-verify vs retail. | Bug (uncertain) |
| 24 | New packet per member per fan-out; `GroupFlagsUpdatedHandler` reuses one instance. | Optimization |
| 25 | Shared "fan out to online group members" helper across internal handlers. | Refactor |

**Tests to add:** `MemberIndex`; `ReadyStatus` 1/2; clear behavior; both recipients; offline member 303.

### Housing (`ClientHousingEditModeHandler`)

| # | Item | Category |
|---|------|----------|
| 26 | Housing edit mode ack/state. **PARTIAL** (permission check + log; no ack/broadcast). | Bug (incomplete) |
| 27 | `TargetPlayerIdentity` / `CanModifyResidence`. **FIXED** in handler validation. | Bug (security) |

### Matching (`ClientMatchingQueueRandomPartyHandler`)

| - | Thin delegate to `MatchingManager`-**no functional gap** vs random/party siblings. | OK |

---

## P1 - Quest scripts

| # | File | Item | Category |
|---|------|------|----------|
| 28 | Q3487 cannon quest gate. **FIXED** | Bug |
| 29 | Q3487 map spawns. **FIXED** | Bug |
| 30 | Q3667 objective `4770` unverified. **OPEN** | Bug (unverified) |
| 31 | `FollowUpQuestScript<T>`. **FIXED** (listed quests) | Refactor |

**Manual checks:** cannon without quest 3487; double ultrabot kill with achievement 1296; objective 4770 in client data.

---

## P2 - Network hygiene (no duplicate opcodes found)

| # | Item | Category |
|---|------|----------|
| 32 | Missing field-level tests for `ServerSupportUInt32AndFlags`, full padding consume for `ServerSupportUInt5AndSixUInt32`. | Test |
| 33 | `ServerRealmAuxUInt32TripletList` test lives in `SupportPacketShapeTests`-move or split. | Test |
| 34 | Consolidate triplet row types (`ServerUInt32Triplet`, spell row, crafting inline). | Refactor |
| 35 | Consolidate counted triplet lists (realm 0x05A1 / spell 0x080F). | Refactor |
| 36 | `ServerUInt32WideStringPayload` vs `Server0x08CC` duplication. | Refactor |
| 37 | `RowCount` property on survey list unused and confusing vs wire. | Refactor |
| 38 | Opcode/docs naming drift (crafting comment `Server0x084B` vs `ServerCraftingAuxSixUInt32`). | Refactor |

**Verified OK:** crafting aux scalars (0x084B, 0x0855); fixed-size support bit layouts; no duplicate `[Message]` for scoped opcodes.

---

## P2 - Cross-cutting

| # | Item | Category |
|---|------|----------|
| 39 | Loot manager `lootInstances` has no lock; marketplace/duel use `syncRoot`-document/enforce world-thread-only mutation. | Refactor |
| 40 | `NextLootId` unsynchronized-safe only if all allocation is on world tick. | Bug (latent) |
| 41 | `ClientGroupFlagsChangedHandler` `FireAndForgetAsync`. **FIXED** | Bug (latent) |
| 42 | No xUnit for quest scripts (unlike packet shape tests). | Test |
| 43 | `NorthernWildsMapScript` nested types still named `TutorialMapInfo` / `TutorialMapPosition`. | Refactor |

---

## Suggested implementation waves (initial numbering)

See **Updated implementation waves** in the deep pass addendum for the current priority order.

---

## Blockers / unknowns

| Item | Needs |
|------|--------|
| Marketplace P0 #2-#4 | Reproduce escrow/cancel with tests; confirm retail `Price` validation |
| `ServerSupportSurveyList` | Export `1400a4260` - count as 5th header vs separate field (likely OK) |
| #21 `MemberIndex` | Client expectation for 0x0437/0x0438 vs `GroupIndex` |
| #22 Ready-check clear | Retail `ReadyStatus` when flags cleared |
| #18 Duel leash | Design: distance between players vs anchor point |
| #30 Q3667 objective 4770 | Quest2 table verification |

---

## Out of scope (initial pass)

- Analysis markdown / coverage CSV churn (documentation, not runtime).
- Broad repo-wide audit beyond the modified surface and direct dependencies.

---

## Deep pass addendum (2026-05-23)

Second pass: full WorldServer loot handler traces, marketplace handler + economy doc cross-check, decomp re-verification of aux opcodes, group/PvP/quest folder sweep.

### Corrections from first pass

| First pass | Deep pass verdict |
|------------|-------------------|
| **P0 #2** `ServerSupportSurveyList` missing 5th header | **Likely false alarm.** Wire writes `Value0`-`Value3` + `Rows.Count` = five uint32s after wide string; decomp label "five header fields" may include count. Still export `1400a4260` to confirm. Ergonomics: no named `Value4`; unused `RowCount` property. |
| Marketplace "only SearchAuctions O(n)" | **Four additional critical economy bugs** (escrow, cancel rollback, double mail) - see P0 #2-#4 above. |

---

### P0 - Loot & sync (deep pass)

| # | Item | Category |
|---|------|----------|
| D-L1 | `BroadcastLootNotification` implemented but **never called** - party loot chat (`ServerLootNotification`) dead. | Bug |
| D-L2 | `ServerLootItemUpdate` **never enqueued** in runtime - group loot UI likely stale after roll/master/partial loot. | Bug |
| D-L3 | `RemoveExpiredLootInstances` removes corpses with **no** `ServerLootRemove` to looters - ghost windows up to 30 min. | Bug |
| D-L4 | On collect, `ServerLootRemove` only to **requesting** player when instance empties - other group looters not notified. | Bug |
| D-L5 | `HarvestRule` on `GroupLootState` synced to clients but **never read** in `ConfigureLootDistribution`. | Bug |
| D-L6 | Trash-quality items (`Quality <= 1`) with 2+ eligible players **always** roll - bypasses Normal/Master/RR/FFA. | Bug |
| D-L7 | `FindLootInstance` + `GiveLoot`/`RollLoot`/`AssignMasterLoot` throw `InvalidOperationException` for non-looters - handlers don't catch. | Bug |
| D-L8 | Vacuum silently skips `RequiresRoll` / `OnlyMasterLootable` items with no client feedback. | Bug |
| D-L9 | All-pass rolls -> `MarkDeliveredWithoutWinner` - item destroyed with no delivery (by design per winner packet; no test). | Bug (edge) |

**Handler gaps:** `ClientItemUseLootBagHandler` only maps `inventory-full` to `GenericError`; other failure reasons silent (crafting uses `loot-delivery:{reason}`).

**Tests missing:** vacuum behavior, trash auto-roll, expiry + client remove, `BroadcastLootNotification`, group drop integration, all-pass rolls, `ActivationInteractionGuards` on loot handlers.

---

### P0 / P1 - Marketplace (deep pass)

| # | Item | Severity |
|---|------|----------|
| D-M1 | Property min/max filters accepted in `ValidateAuctionFilter` but **ignored** in `MatchesAuctionRequest`. | P1 |
| D-M2 | `ForceImmediate` persisted, never used in `TryMatchCommodityOrder`. | P1 |
| D-M3 | Mail/delivery helpers return `bool` often ignored on expire/fill - paid seller or removed order, no item if mail down. | P1 |
| D-M4 | `PostAuction` - no bind/tradeability check beyond inventory remove. | P1 |
| D-M5 | `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` does not document C1-C4, property filter gap, auction cancel rollback. | Doc |

**Tests missing:** `Price != PricePerUnit * Quantity`, sell-cancel double mail, auction cancel inv-full + mail fail, partial commodity fill, property filters, `ForceImmediate`.

---

### P1 - Group (deep pass)

| # | Item |
|---|------|
| D-G1 | `ServerGroupMemberRoleChange` (0x0438) writes **14 trailing zero bytes**; `ServerGroupMemberFlagsChanged` (0x0437) does not - asymmetric wire; **0x0437 has no shape test**. |
| D-G2 | `function_labels.csv`: no durable label maps 0x0437; 0x0439-0x0440 unused in enum. |
| D-G3 | `Group.StartReadyCheckAsync` clears ready flags in-memory then `SetFlagAsync(Pending)` per member - deliberate to avoid double message; interacts with #22 clear gap. |
| D-G4 | Open-world **player vs player combat only during active duel** (`Player.CanAttack` + `AreDueling`) - document as limitation if not retail. |

---

### P1 - Quests (deep pass)

| # | Item |
|---|------|
| D-Q1 | Q3487 cannon uses `ScriptedTargetGroupChecklist` (type 10) in script; retail activate path often `ActivateTargetGroupChecklist` (type 14) via `SpellEffectHandler`. |
| D-Q2 | `ClientActivateUnitCastHandler` still runs `InteractionObjectiveUpdater` after script - may double-credit SucceedCSI/TalkTo for creature 11251. |
| D-Q3 | Q3667 objective **4770** - only reference is script constant; no DataMapping/SQL corroboration in repo. |
| D-Q4 | `Q5594LastResistance.cs` also calls `GrantAchievement(1296)` without guard - same throw risk as Q3487. |

---

### P2 - Network / decomp (deep pass)

| # | Item |
|---|------|
| D-N1 | `Decomp/Analysis/exports` **empty** - no Ghidra snippets for `1400a4260` in checkout. |
| D-N2 | No WorldServer emitters for 0x0347-0x0351, 0x05A1, 0x084B, 0x0855 - intentional modeled-only (F-003). |
| D-N3 | `Server0x08CC` still duplicates 0x0347 wire (`1400980f0`) in `ServerUnresolvedOutputPackets.cs`. |
| D-N4 | Stale doc names: `MISSING_FEATURE_MATRIX.md` / `RETAIL_FEATURE_EVIDENCE.md` still reference `Server0x084B` / `Server0x05A1`. |
| D-N5 | Registration-size labels (e.g. 0x0347 `0x10`, survey row `0x30`) are often **client object sizes**, not fixed wire length. |

---

### Updated implementation waves

1. **Wave 1 (economy P0):** Marketplace #2-#4 (escrow validation, cancel ordering, commodity mail).
2. **Wave 2 (loot P0):** Bag consume order #5; soulbind scope #6; loot sync packets D-L1-D-L4.
3. **Wave 3 (quests + group):** #7 achievement guard; Q3487 gates/spawns; group `MemberIndex` + ready clear + 0x0437 test.
4. **Wave 4 (parity + scale):** Harvest rule, trash roll policy, search filters, loot indexing, packet refactors.

---

## Retail evidence crosswalk (`RETAIL_FEATURE_EVIDENCE.md`)

Maps backlog items to feature rows. Use **Aligned** items as "done for retail scope"; **Gap**/**Partial** as remaining work.

| Retail row | Evidence status | Backlog / code status |
|------------|-----------------|------------------------|
| **F-005** Marketplace 3/30 slots, mail settlement | **Aligned** (S7 + `MarketplaceAccountLimitsTests`) | Limits enforced; mail on win/outbid/expire. Escrow integrity and cancel return ordering are now guarded; broader settlement/mail failure paths still need tests. |
| **F-005** partial fill / cancel precision | **Not sure** in retail doc | P0 cancel/escrow bugs fixed; partial fill and mail-failure edge cases remain worth testing. |
| **F-014** Need/Greed/Master/RR + trash rolls | **Certain** (Strain 1.0.9) | RR/Need/Master/FFA in `ConfigureLootDistribution`; trash roll **implemented**. Loot update/remove/notification packets are now wired; BoP timing remains a parity concern. |
| **F-014** `HarvestRule` (group) | Group loot rules in beta; **housing** harvest split is F-004 | Resource harvest now reads `GroupLootState.HarvestRule`; corpse loot remains governed by normal loot rules. |
| **F-004** Housing harvest % split | **Certain** (S7 `HousingRemodel.lua`) | `RetailHousingHarvestSplit` on plug harvest - **Partial** per matrix, not same as group `HarvestRule` |
| **F-010** Ready-check / group flags | **Certain** (beta timings) | #21-22 **Fixed** (`0x0441` always sent) |
| **F-010** Deserter cross-activity queue | Was **Partial** in pass-7 table | **Fixed** - `MatchingQueueValidator` line 82 |
| **F-010** Votekick cooldowns | **Gap** in pass-7 table | Still **Gap** - constants only |
| **F-015** Duel leash | **Need confirmation** | **Partial** - distance leash + packets (`ad2c11f6`); not anchor-based area |
| **F-003** Support `0x0347..0351` | Typed models (F-003) | Packet shapes **Aligned**; no emitters (intentional) |
| **F-008** Crafting `0x084B`/`0x0855` | Aux semantics | `CraftingPacketShapeTests` - modeled only |

---

### Deep pass file index

| Area | Key paths |
|------|-----------|
| Loot handlers | `WorldServer/.../Loot/ClientUnsupportedLootHandlers.cs`, `ClientItemUseLootBagHandler.cs` |
| Loot core | `Game/Loot/GlobalLootManager.cs`, `LootInstance.cs`, `LootInstanceItem.cs` |
| Marketplace | `Game/Marketplace/GlobalMarketplaceManager.cs`, `WorldServer/.../Marketplace/ClientMarketplaceHandlers.cs` |
| Group | `WorldServer/.../Group/GroupMemberFlagsUpdatedHandler.cs`, `Network.World/.../ServerGroup*.cs`, `Server.GroupServer/Group/Group.cs` |
| PvP | `Game/Pvp/DuelManager.cs`, `WorldServer/.../Pvp/ClientPvpHandlers.cs` |
| Quests | `Script.Main/Quests/NorthernWilds/*`, `NorthernWildsMapScript.cs` |
| Packets | `Network.World/Message/Model/Support/ServerSupportAuxPackets.cs`, tests in `Game.Tests/Support/` |
| Evidence | `Decomp/Analysis/function_labels.csv`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` |

---

## Current-tree addendum (2026-05-23 parallel review)

Read-only pass against the current working tree using parallel subagents plus a
local cross-cutting scan. Local helper scripts under `Decomp/Analysis/scripts`
were reviewed as candidate discovery tooling; generated auto-label rows were
not kept in `function_labels.csv` unless they already met the evidence-backed
label rule. Items below supersede stale rows above where they explicitly say so.

### Status corrections from current source

| Backlog row | Current-tree correction |
|-------------|-------------------------|
| #18 duel leash comment | The code now documents the leash as inter-duelist distance (`Game/Pvp/DuelManager.cs:17`). Retail anchor-vs-distance semantics still need evidence, but the misleading comment is gone. |
| #19 duel cancel winner/loser | `ServerDuelResult` now zeroes winner/loser for `DuelCancelled` and `DeclinedRequest` (`DuelManager.cs:332`). |
| #20 duel disconnect | `WorldSession.OnDisconnect` calls `DuelManager.Instance.OnPlayerDisconnect(Player)` (`WorldServer/Network/WorldSession.cs:99`). |
| D-L7 loot handler throws | **Reopen.** `GlobalLootManager` still resolves a loot instance by owner/item before checking looter membership, and `LootInstance.GiveLoot` / `RollLoot` / `AssignMasterLoot` throw for non-looters (`LootInstance.cs:209`, `:239`, `:259`). |
| Offline master loot fixed row | The row saying `AssignMasterLoot` returns `false` when the assignee is offline is inaccurate. Current code logs the offline assignee and returns `true` (`LootInstance.cs:275`). |
| #13 marketplace `UnknownArray` | **Partial, not fixed.** `DeserializeUnknownArray` catches `FormatException` only; overflow values still throw during startup (`GlobalMarketplaceManager.cs:703`). |
| D-Q3 Q3667 objective 4770 | Tracker/source conflict. `Q3667TheTower.cs:31` says Quest2 verification exists, while this backlog still marks 4770 unverified. Add the Quest2/DataMapping evidence link or remove the source certainty. |
| D-Q4 Q5594 achievement | Correction: current `Q5594LastResistance` grants achievement **1730**, not 1296, but it is still unguarded and `GrantAchievement` throws when already complete (`Q5594LastResistance.cs:61`, `BaseAchievementManager.cs:94`). |
| D-N1 empty exports | Stale. Local ignored exports now exist and include `ServerSupportSurveyList_ReadPayload` (`function_labels.csv:1074`, `exports/WildStar64.exe/functions.csv:1841`). |
| D-N3 `Server0x08CC` duplication | Fixed/refactored. `Server0x08CC` now inherits `Shared.ServerUInt32WideStringPayload` (`ServerUnresolvedOutputPackets.cs:471`). |

### P1 - Loot, item use, and delivery

| # | Item | Category |
|---|------|----------|
| R3-L1 | `ClientItemUseHandler` consumes the item before casting and `UnitEntity.CastSpell` discards `TryCastSpell` failure (`ClientItemUseHandler.cs:49`, `UnitEntity.cs:1803`, `UnitEntity.cs:1889`). Dead/disabled/failed casts can delete consumables. | Bug |
| R3-L2 | `ClientItemUseDecorHandler` consumes decor items before `ResidenceManager.DecorCreate` can validate or create the residence (`ClientItemUseDecorHandler.cs:34`, `ResidenceManager.cs:39`, `ResidenceManager.cs:64`). Locked/no-residence failures can lose the item. | Bug |
| R3-L3 | `TryDeliverHarvestLoot` selects round-robin harvest recipients from all online same-map group members with no range check and no delivery preflight for the chosen recipient (`GlobalLootManager.cs:341`). | Bug |
| R3-L4 | `LootInstance.AddLootItem` merges duplicate loot through unchecked `Amount += amount`, unlike generated item-loot overflow guards (`LootInstance.cs:63`, `LootInstanceItem.cs:68`). Data-driven duplicate rows can wrap counts. | Bug / Refactor |

### P1 - Marketplace and mail

| # | Item | Category |
|---|------|----------|
| R3-M1 | Same-bidder auction increases require the player to afford the full new bid before the held current bid is refunded (`GlobalMarketplaceManager.cs:241`, `:258`, `:272`). Check/refund only the delta or preflight with escrow included. | Bug |
| R3-M2 | `ForceImmediate` commodity orders are parsed/persisted but matched like durable resting orders, consuming account slots and leaving unmatched remainder live (`CommodityOrder.cs:13`, `GlobalMarketplaceManager.cs:350`, `GlobalMarketplaceManager.cs:946`). | Bug |
| R3-M3 | Accepted auction property/rune/equippable filters and `AuctionSort.Property` are ignored or default to min-bid sorting (`ClientAuctionsByFilterRequest.cs:18`, `GlobalMarketplaceManager.cs:152`, `GlobalMarketplaceManager.cs:754`). Implement or reject them. | Bug |
| R3-M4 | Commodity cancel/fill uses inventory delivery when at least one slot is free, but multi-stack `ItemCreate` can partially create then return full; the order is still removed/filled (`GlobalMarketplaceManager.cs:412`, `GlobalMarketplaceManager.cs:1008`, `Inventory.cs:285`). | Bug |
| R3-M5 | Marketplace settlement still ignores mail-delivery failures on auction buyout/expiry and commodity fill; seller/order state can be committed before item delivery succeeds (`GlobalMarketplaceManager.cs:844`, `:881`, `:1015`). | Bug |
| R3-M6 | Marketplace mail `ContentType` is not persisted; reload resolves all item/commodity auction mail as `AuctionWon`, losing expired/return semantics (`MailItem.cs:109`, `MailItem.cs:138`, `MailItem.cs:341`). | Bug |
| R3-M7 | System/marketplace mail can be returned to `SenderId == 0` unless explicitly marked not-returnable (`MailItem.cs:142`, `MailItem.cs:294`). Reject non-player returns or mark these mails not-returnable at creation. | Bug |
| R3-M8 | Auction search page is unbounded; huge `request.Page` can overflow the checked `int` skip calculation (`GlobalMarketplaceManager.cs:161`). | Bug / Test |
| R3-M9 | Persisted commodity rows are trusted on load without item, quantity, escrow, or duration validation (`GlobalMarketplaceManager.cs:74`, `GlobalMarketplaceManager.cs:679`). Quarantine corrupt rows before they can refund or deliver. | Bug |

### P1 - Group, PvP, housing, matching, social

| # | Item | Category |
|---|------|----------|
| R3-G1 | Group instance difficulty accepts any member and only mutates the requester's `Player.InstanceDifficulty`; loot-rule changes require leader and server-side group state (`ClientGroupInstanceHandlers.cs:123`, `Server.GroupServer/Group/Group.cs:719`). | Bug |
| R3-P1 | PvP toggle-off clears `PvPFlag.Enabled` immediately, then sends a 5-minute cooldown packet with no pending state/timer (`ClientPvpHandlers.cs:190`, `Player.cs:2686`). Keep the flag active until cooldown expiry. | Bug |
| R3-H1 | Private residence visits always return `Visit_Private`, even for the owner or authorized modifiers, unlike neighbors/roommates branches (`HousingVisitHelper.cs:39`). | Bug |
| R3-H2 | Housing decor create subtracts currency before validating colour shift, scale, and plot bounds; create also lacks the plot-position validation used by move (`ResidenceMapInstance.cs:683`, `:711`, `:715`). | Bug |
| R3-Match1 | Matching leave is not idempotent: single leave dereferences a null queue when not queued, and leave-all enumerates live queue values while removal mutates the dictionary (`MatchingManager.cs:359`, `MatchingCharacter.cs:57`). | Bug |
| R3-Match2 | Match cleanup enumerates live team members while `MatchLeave` removes from the same dictionary (`Match.cs:168`, `MatchTeam.cs:77`). Snapshot before cleanup. | Bug |
| R3-IC1 | ICComm group/guild membership is checked at join only; send/update only verify channel membership and `InWorld`, so former group/guild members can keep transient channel traffic (`ICCommManager.cs:163`). | Bug |
| R3-X1 | Several WorldServer handlers still call `PublishAsync` from `void HandleMessage` without `FireAndForgetAsync` or an await-safe helper, so publish failures are unobserved (`ClientGroupLeaveHandler.cs`, `ClientChatJoinHandler.cs`, `ClientChatModeratorHandler.cs`, etc.). | Bug / Refactor |

### P1/P2 - Quest, packet, and tracker hygiene

| # | Item | Category |
|---|------|----------|
| R3-N1 | `Get-DecompCoverageSnapshot.ps1` placeholder detection misses `Server0xNNNN` / `Client0xNNNN` names because the regex omits `0x`; coverage reports say no placeholder queue while unresolved models remain (`Get-DecompCoverageSnapshot.ps1:363`). | Bug |
| R3-Q1 | `quest_implementation_audit.py` scans whole files per quest id, so mixed files can make stub-only terminal quest classes look implemented because sibling classes contain `FollowUpQuestScript` or objective calls (`quest_implementation_audit.py:176`). | Bug |
| R3-Q2 | Manual follow-up grant boilerplate remains in `Q3480`, `Q3671`, `Q3673`, and `Q5604`; convert to `FollowUpQuestScript<T>` or a shared helper while preserving cinematic/objective hooks. | Refactor |
| R3-Q3 | Quest script xUnit coverage is still only a construction smoke test for Q3479; add behavior tests for follow-up grants, achievement repeat guards, cinematic-complete objectives, and targeted objective gates (`QuestScriptConstructionTests.cs:11`). | Test |

### Current priority waves

1. **Wave A (data-loss / crash guards):** R3-L1, R3-L2, D-L7 reopen, offline master-loot result semantics, R3-M4/R3-M5, R3-M7.
2. **Wave B (economy correctness):** R3-M1, R3-M2, R3-M3, #13 overflow, R3-M6, R3-M8/R3-M9.
3. **Wave C (group/housing/matching state):** R3-G1, R3-P1, R3-H1/R3-H2, R3-Match1/R3-Match2, R3-IC1.
4. **Wave D (tracker/test cleanup):** D-Q3/D-Q4 corrections, R3-N1, R3-Q1/R3-Q2/R3-Q3, D-G1 0x0437 packet-shape coverage.

Verification for this addendum: read-only source inspection and subagent review;
no build/test run was needed for the markdown-only backlog update.
