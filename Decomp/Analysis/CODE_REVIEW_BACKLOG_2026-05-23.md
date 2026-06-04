# Code Review Backlog - 2026-05-23

Plan-only backlog from parallel review of recently modified game logic, network packets, WorldServer handlers, and quest scripts.

**Status audit:** 2026-05-23 (second pass vs `RETAIL_FEATURE_EVIDENCE.md` + current tree). Key commits: `ad2c11f6`, `0d785f25`, `41ab84f8`, `ac95f25c`, `de275ee5`. Current review corrected loot bag ordering to preflight, consume, then grant, and kept crafting discovery rolls blocked until server-side thresholds are mapped.

## Fix status summary

| Status | Count | Notes |
|--------|-------|-------|
| **Fixed** | 52+ | See table below |
| **Partial** | 2 | Housing edit ack/broadcast consumer mapping; marketplace final-settlement DB/save atomicity outside guarded insert/bid/mail, item-auction won/return-mail same-save deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failure paths |
| **Retail-aligned (not bugs)** | 1 | Grey/trash group auto-roll is **Certain** retail (Strain 1.0.9) - see crosswalk below |
| **Open** | Marketplace final-settlement cross-resource DB/save atomicity, remaining retail loot packet timing |

### Fixed since backlog was written

| Backlog # | Item | Evidence |
|-----------|------|----------|
| 1, 5 | Loot bag consume/delivery ordering | `TryUseLootBag` now preflights delivery, consumes the item, then grants loot; `ItemUse` failure no longer grants duplicate rewards. |
| 2 | Commodity buy escrow | `ValidateCommodityOrder` requires `Price == PricePerUnit * Quantity` with overflow guard. |
| 3 | Auction cancel rollback | `CancelAuction` verifies inventory/mail return before removing listing and refunding bidder. |
| 4 | Commodity sell cancel double mail | `CancelCommodityOrder` decides one return path and mails at most once. |
| 12 | Marketplace DB-null divergence logging | `Initialise` and `Persist` warn when `CharacterDatabase` is unavailable instead of silently diverging from durable state. |
| 14 | Auction search page materialization | `SearchAuctions` no longer clones every matched auction before paging and guards huge page skips; exact sort still materializes the ordered match list for total count. |
| 16 | Marketplace persist sync wait | `Persist` uses `.ConfigureAwait(false).GetAwaiter().GetResult()`. |
| 17 | Marketplace null item load logging | `Initialise` logs skipped persisted auctions whose `model.Item` is null. |
| 6 | Soulbind matching stacks | `SoulbindDeliveredStaticItems` only soulbinds newly-created matching item GUIDs. |
| 7 | Q3487 achievement throw | `HasCompletedAchievement` guard in `Q3487Shellshock.cs` |
| 21 | `MemberIndex` on group flags | `GroupMemberFlagsUpdatedHandler.cs` sets `message.Member.GroupIndex` |
| 22 | Ready-check clear / stale UI | Handler always sends `ServerGroupReadyCheckStatusUpdate` (status 0 when flags cleared) |
| 24 | Group member flag packet allocation | `GroupMemberFlagsUpdatedHandler` now builds one immutable role/flag packet and one ready-check packet per update, then fans them out to online members. |
| 25 | Group online-member fan-out helper | `GroupMemberFanoutHelper` centralizes online group member resolution for internal group broadcast handlers. |
| 28 | Q3487 cannon no quest gate | `GetQuestState(3487) == Accepted` in `OnActivateSuccess` |
| 29 | Q3487 map fallbacks | `NorthernWildsMapScript` spawns cannon 11251 + ultrabot 12526 |
| D-Q1 | Q3487 cannon objective type | Client SQL shows quest 3487 objective `4489` is type `10` (`ScriptedTargetGroupChecklist`), matching the current script. |
| D-Q2 | Q3487 cannon activate-cast fan-out | Generic activate-cast objective updates are suppressed for scripted cannon creature `11251`; the script remains the checklist-credit owner. |
| D-M4 | Auction post tradeability | `PostAuction` now rejects non-owner/non-inventory, soulbound, and equipped-bag items before inventory removal or listing creation. |
| D-M5 | Economy status doc coverage | `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` now documents unsupported auction filter rejection, auction cancel rollback, delivery guards, and auction post tradeability. |
| D-M3 | Marketplace DB/save failure guards | Auction post inserts, commodity order inserts, auction bid updates, marketplace mail persistence failures, item-auction won/return-mail same-save auction deletions, online-inventory auction buyout/cancel/expiration delete-save failures, and direct commodity cancel/expiration delete-save failures now return `DbFailure`/no-delivery results and roll back or avoid transient item/order/escrow/currency state where possible. |
| D-G3 | Ready-check pending clear | Ready-check pending status is already covered by unconditional `ServerGroupReadyCheckStatusUpdate`; in-memory ready clears remain deliberate de-duplication. |
| D-G4 | Duel-only open-world PvP boundary | Current `Player.CanAttack` player-target behavior is documented and test-pinned as active-duel-only outside instanced PvP match flows. |
| 4 | Corpse loot single-eligible group context | `CreateLootRecipientContext` keeps group context when same-map group members exist, but only in-range members become tracked looters/eligible recipients. |
| 5 | Corpse loot out-of-range UI leak | Corpse loot recipients, looter ids, and master candidates are filtered by `LOOT_RANGE`; no eligible group recipients means no corpse loot instance is created. |
| 19 | Duel cancelled/declined result ids | `ServerDuelResult` now emits zero winner/loser unit ids for cancelled and declined results; focused regressions pin both cases. |
| 20 | Duel disconnect cleanup | `WorldSession.OnDisconnect` calls `DuelManager.OnPlayerDisconnect`, and focused manager coverage pins immediate active-duel cancellation. |
| 31 | Manual `GrantNext()` | Q3479, Q3667, Q3886, Q3487 use `FollowUpQuestScript<T>` |
| 41 | `FireAndForgetAsync` on group flags | `ClientGroupFlagsChangedHandler.cs` |
| 18 (partial) | Duel leash packets | `DuelManager.UpdateActiveDuelLeash` - `ServerDuelLeftArea` / `ServerDuelCancelWarning` / timeout cancel |
| 6 | Offline master loot | `AssignMasterLoot` returns `false` before winner resolution/broadcast when assignee offline; focused regression covers the boundary. |
| 7 (partial) | Offline roll winner | `FinaliseRoll` logs warn and keeps the item assigned for deferred collection before corpse expiry. |
| D-L1-D-L4 | Loot sync packets | Delivery, roll, master assignment, expiry, and corpse-empty paths now broadcast update/remove/notification packets to the looter audience. |
| D-L7 | Loot handler throws | `GlobalLootManager` GiveLoot/Roll/Assign reject non-looters before `LootInstance` invariant calls; `LootInstance` throws remain internal guardrails. |
| 32 | Support aux field-level packet tests | `ServerSupportUInt32AndFlags` covers value/2-bit flags/30-bit padding, and `ServerSupportUInt5AndSixUInt32` now consumes the 27-bit zero tail. |
| 33 | Realm aux packet test placement | `ServerRealmAuxUInt32TripletList` coverage moved from support packet tests to dedicated `RealmAuxPacketShapeTests`. |
| 34 | Triplet row type consolidation | `ServerSpellUInt32TripletListRow` now inherits the shared `ServerUInt32Triplet` writer. |
| 35 | Counted triplet list consolidation | Spell `0x080F`/`0x0810` and realm aux `0x05A1` packets now reuse shared `ServerUInt32TripletListPayload`. |
| 36 | Shared wide-string payload | `ServerEntityStatUInt32WideString` / old `Server0x08CC` duplication resolved through shared `ServerUInt32WideStringPayload`. |
| 37 | Support survey row count naming | `ServerSupportSurveyList.RowCount` is documented as diagnostic/log-only; wire writes `Rows.Count` directly. |
| 39 | Loot instance mutation lock assumption | `GlobalLootManager.lootInstances` documents the world-thread-only mutation contract. |
| 40 | Loot unit id allocation | `GlobalLootManager.NextLootId` now uses atomic increment, preserves the high-bit server-owned id range, and skips zero. |
| 42 | Quest script construction xUnit | `QuestScriptConstructionTests` provides DI smoke coverage for `FollowUpQuestScript<T>`. |
| 43 | Northern Wilds map type names | `NorthernWildsMapScript` nested types were renamed to `NorthernWildsMapInfo` / `NorthernWildsMapPosition`. |
| - | Commodity expiration sweep | `ProcessExpiredCommodityOrders` (`ad2c11f6`) - aligns F-005 mail settlement |
| - | Deserter cross-queue | `MatchingQueueValidator` calls `MatchingDeserterManager.CanQueue` - RETAIL crosswalk was **Partial**, now wired |
| - | Housing plug harvest split | `RetailHousingHarvestSplit` / `RetailHousingHarvestGrant` - RETAIL F-004 **Partial**->better than group `HarvestRule` gap |
| - | Trash roll policy | Uses `RetailCertainRules.TrashItemMaxQualityId` - **Aligned** with F-014 Certain |
| - | Housing permission check | `ClientHousingEditModeHandler` uses `CanModifyResidence` (`ad2c11f6`) |
| 26 | Housing edit server state | `ClientHousingEditModeHandler` now delegates to `IResidenceMapInstance.SetEditMode`; `ResidenceMapInstance` stores/clears per-player edit residence state and validates the target residence is loaded on the current residence map. |
| - | Matching random party | Routed to `IMatchingManager.JoinRandomPartyQueue` (`ad2c11f6`) |
| Survey 0x034E | Missing 5th header | **Not a bug** - row count is the fifth uint32 |

### Partial / broken (needs follow-up)

| Backlog # | Item | Current state |
|-----------|------|---------------|
| R3-M4/R3-M5 DB save atomicity | Delivery-helper failures plus auction/commodity insert, bid-update, marketplace mail-save, item-auction won/return-mail same-save auction deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failures are guarded; remaining final settlement delete/update failures across commodity mail, offline auction/order rows, item state, and online/offline currency persistence still need a real transaction boundary under F-005/F-013. |
| 26 | Housing edit mode | Validates map + residence permission and stores per-player server edit state; still **no mapped ack / broadcast packet semantics** |

### Still open (high signal)

| Backlog # | Item |
|-----------|------|
| 8 | Active loot-instance tick walk profiling |
| 23, 38 | See sections below |

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
| 4 | **FIXED 2026-06-03:** Group corpse loot keeps group context for multi-member same-map groups while filtering participants to in-range members only. | Bug (fixed) |
| 5 | **FIXED 2026-06-03:** Out-of-range same-map group members are no longer tracked looters, eligible recipients, or master candidates for corpse loot; empty eligible group contexts are skipped. | Bug (fixed) |
| 6 | **FIXED:** `AssignMasterLoot` returns `false` when the assignee is offline and leaves the item master-lootable instead of resolving/broadcasting a winner. | Bug |
| 7 | **FIXED:** `FinaliseRoll` leaves the item assigned to the offline roll winner for deferred collection instead of marking it delivered or granting to the wrong player. | Bug (fixed) |
| 8 | **PARTIALLY CLOSED 2026-06-03:** owner-unit and looter indexes now back notify, runtime snapshot, collect/roll/master lookup, and vacuum paths; the remaining active-instance `Update` walk needs profiling before a timing-wheel/shard refactor. | Optimization (partial fixed) |
| 9 | **PARTIALLY CLOSED 2026-06-03:** fully delivered instances are removed immediately after final collect/vacuum/roll/master resolution instead of lingering until the next expiry sweep; active-instance scan/indexing cost remains under #8. | Optimization (partial fixed) |
| 10 | **FIXED 2026-06-03:** Duplicate loot aggregation rejects amount overflow without wrapping the existing row amount. | Refactor (fixed) |
| 11 | **FIXED 2026-06-03:** `DeliverAllLoot` now returns incomplete success when any pending row fails, while preserving successful deliveries and retryable failed rows; generated granted-notify is suppressed on mixed delivery. | Bug / Refactor (fixed) |

**Tests to add:** retail `ServerLootCanLoot` timing and parent/source tracking once direct evidence is available.

### Marketplace (`GlobalMarketplaceManager`)

| # | Item | Category |
|---|------|----------|
| 12 | **FIXED:** `Initialise` and `Persist` warn when DB context is null. | Bug (fixed) |
| 13 | **FIXED 2026-06-03:** persisted marketplace microchip-id parsing treats format and overflow values as corrupt DB input, logs, and returns an empty array instead of breaking world startup. | Bug (fixed) |
| 14 | **FIXED / mitigated:** `SearchAuctions` pages before cloning and returns an empty page for huge skips; exact sorting still materializes the ordered match list for count/order correctness. | Optimization (fixed) |
| 15 | **FIXED 2026-06-03:** commodity matching now uses per-item, per-side price indexes for opposite-side candidate selection and immediate sell preflight; exact retail multi-order partial-stack precision remains a separate semantic blocker. | Optimization (fixed) |
| 16 | **FIXED:** `Persist` uses `.ConfigureAwait(false).GetAwaiter().GetResult()`. | Optimization (fixed) |
| 17 | **FIXED:** `Initialise` logs skipped auctions with `model.Item == null`. | Refactor (fixed) |

### PvP (`DuelManager`)

| # | Item | Category |
|---|------|----------|
| 18 | **FIXED:** Active duel leash is explicitly documented/tested as inter-duelist distance, sends left-area/cancel-warning packets, and cancels after timeout. | Bug (fixed) |
| 19 | **FIXED 2026-06-03:** Cancelled/declined duel results emit zero winner/loser unit ids; focused timeout and decline regressions pin the boundary. | Bug (fixed) |
| 20 | **FIXED 2026-06-03:** `WorldSession.OnDisconnect` invokes `DuelManager.OnPlayerDisconnect`, and active disconnect cancellation is now covered by `DuelManagerTests`. | Bug (fixed) |

**Remaining F-015 tests:** observer/reward/stat parity and broader arena/battleground PvP score/reward parity.

---

## P1 - Network & handlers

### Group (`GroupMemberFlagsUpdatedHandler`)

| # | Item | Category |
|---|------|----------|
| 21 | `MemberIndex` never set. **FIXED** | Bug |
| 22 | Ready-check clear stale UI. **FIXED** (always sends 0x0441). | Bug |
| 23 | Non-promotion updates send provisional `ServerGroupIdentityListAndUInt32Array` values for `0x0438`; reader-backed shape is covered, but leading/parallel uint32 semantics remain blocked pending client consumer proof. | Bug (blocked) |
| 24 | **FIXED 2026-06-03:** `GroupMemberFlagsUpdatedHandler` now reuses one role/flag packet and one ready-check packet per update fan-out. | Optimization (fixed) |
| 25 | **FIXED 2026-06-03:** `GroupMemberFanoutHelper` centralizes online-member fan-out for internal group broadcast handlers. | Refactor (fixed) |

**Tests to add:** `MemberIndex`; `ReadyStatus` 1/2; clear behavior; both recipients; offline member 303.

### Housing (`ClientHousingEditModeHandler`)

| # | Item | Category |
|---|------|----------|
| 26 | Housing edit mode ack/state. **PARTIAL** (permission check + server-side edit state; no mapped ack/broadcast). | Bug (blocked) |
| 27 | `TargetPlayerIdentity` / `CanModifyResidence`. **FIXED** in handler validation. | Bug (security) |

### Matching (`ClientMatchingQueueRandomPartyHandler`)

| - | Thin delegate to `MatchingManager`-**no functional gap** vs random/party siblings. | OK |

---

## P1 - Quest scripts

| # | File | Item | Category |
|---|------|------|----------|
| 28 | Q3487 cannon quest gate. **FIXED** | Bug |
| 29 | Q3487 map spawns. **FIXED** | Bug |
| 30 | Q3667 objective `4770`. **FIXED** - client SQL corroborates Quest2 row 3667 and QuestObjective row 4770. | Bug (verified) |
| 31 | `FollowUpQuestScript<T>`. **FIXED** (listed quests) | Refactor |

**Manual checks:** cannon without quest 3487; double final-warbot kill with achievement 1730; objective 4770 in client data.

---

## P2 - Network hygiene (no duplicate opcodes found)

| # | Item | Category |
|---|------|----------|
| 32 | **FIXED 2026-06-03:** Field-level tests cover `ServerSupportUInt32AndFlags` value/flags/padding and `ServerSupportUInt5AndSixUInt32` full 27-bit padding consume. | Test (fixed) |
| 33 | **FIXED 2026-06-03:** `ServerRealmAuxUInt32TripletList` coverage lives in dedicated `RealmAuxPacketShapeTests`. | Test (fixed) |
| 34 | **FIXED 2026-06-03:** `ServerSpellUInt32TripletListRow` now reuses the shared `ServerUInt32Triplet` writer. | Refactor (fixed) |
| 35 | **FIXED 2026-06-03:** Realm aux `0x05A1` and spell triplet list `0x080F`/`0x0810` now reuse `ServerUInt32TripletListPayload`. | Refactor (fixed) |
| 36 | **FIXED:** Old `Server0x08CC` / entity-stat wide-string duplication uses shared `ServerUInt32WideStringPayload`. | Refactor (fixed) |
| 37 | **FIXED / documented:** `ServerSupportSurveyList.RowCount` is diagnostic/log-only; `Write` serializes `Rows.Count`. | Refactor (fixed) |
| 38 | Crafting aux opcode emit semantics remain unmapped after packet-shape correction. | Evidence |

**Verified OK:** crafting aux scalars (0x084B, 0x0855); fixed-size support bit layouts; no duplicate `[Message]` for scoped opcodes.

---

## P2 - Cross-cutting

| # | Item | Category |
|---|------|----------|
| 39 | **FIXED / documented:** `lootInstances` mutation is documented as world-thread-only. | Refactor (fixed) |
| 40 | **FIXED 2026-06-03:** `NextLootId` allocation is atomic, remains in the high-bit loot-unit id range, and skips zero. | Bug (fixed) |
| 41 | `ClientGroupFlagsChangedHandler` `FireAndForgetAsync`. **FIXED** | Bug (latent) |
| 42 | **FIXED:** `QuestScriptConstructionTests` covers `FollowUpQuestScript<T>` construction. | Test (fixed) |
| 43 | **FIXED:** `NorthernWildsMapScript` nested types are `NorthernWildsMapInfo` / `NorthernWildsMapPosition`. | Refactor (fixed) |

---

## Suggested implementation waves (initial numbering)

See **Updated implementation waves** in the deep pass addendum for the current priority order.

---

## Blockers / unknowns

| Item | Needs |
|------|--------|
| Marketplace P0 #2-#4 | Reproduce escrow/cancel with tests; confirm retail `Price` validation |
| `ServerSupportSurveyList` | Export `1400a4260` - count as 5th header vs separate field (likely OK) |
| #21 `MemberIndex` | Client expectation for 0x0437 vs `GroupIndex`, plus `0x0438` leading/parallel uint32 semantics |
| #22 Ready-check clear | Retail `ReadyStatus` when flags cleared |
| #18 Duel leash | Design: distance between players vs anchor point |
| #30 Q3667 objective 4770 | Resolved: `wildstar_client_mysql/Quest2.tbl.sql:583` links objective `4770` to quest `3667`, and `wildstar_client_mysql/QuestObjective.tbl.sql:615` identifies it as type `5` (`ActivateEntity`) for creature `11194`. |

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
| D-L1 | **FIXED:** `BroadcastLootNotification` is called after successful collect/master/roll delivery for other looters; `LootInstanceResolutionTests` pins deferred roll delivery notification. | Bug |
| D-L2 | **FIXED:** `ServerLootItemUpdate` is broadcast after roll selection/finalisation, master assignment, and delivery; `LootInstanceResolutionTests` now asserts the runtime packets. | Bug |
| D-L3 | **FIXED:** `RemoveExpiredLootInstances` calls `SendLootRemoveToAllLooters()` before removing expired corpses. | Bug |
| D-L4 | **FIXED:** empty corpse cleanup after collect/vacuum uses `SendLootRemoveToAllLooters()` rather than only the requester. | Bug |
| D-L5 | **FIXED / clarified 2026-06-03.** Corpse loot still uses `ConfigureLootDistribution` for Need/Greed/Master/RR/FFA, while resource harvest uses `TryDeliverHarvestLoot` and now filters group recipients by range plus delivery preflight before applying `HarvestRule`. | Bug (fixed) |
| D-L6 | **RETAIL-ALIGNED / NOT BUG:** Trash-quality items (`Quality <= RetailCertainRules.TrashItemMaxQualityId`) auto-roll for 2+ eligible players, matching Strain 1.0.9 evidence. | Not bug |
| D-L7 | **FIXED:** `GlobalLootManager` resolves owner/item, then rejects non-looters before calling `LootInstance.GiveLoot` / `RollLoot` / `AssignMasterLoot`; focused manager coverage pins no handler-facing throw. | Bug |
| D-L8 | **FIXED 2026-06-03.** Loot vacuum now refreshes `ServerLootNotify` when it delivered nothing because remaining rows are pending roll/master state, preserving the visible pending loot state without inventing an item-error result. | Bug (fixed) |
| D-L9 | **FIXED 2026-06-03 as test-backed emulator policy.** All-pass rolls send a zero-value `ServerLootWinner`, clear pending roll state through `MarkDeliveredWithoutWinner`, emit the final item update/remove sequence, and do not grant inventory. | Test / policy (fixed) |

**Handler gaps:** `ClientItemUseLootBagHandler` only maps `inventory-full` to `GenericError`; other failure reasons silent (crafting uses `loot-delivery:{reason}`).

**Tests missing:** remaining group drop integration edges and broader `ActivationInteractionGuards` on loot handlers.

---

### P0 / P1 - Marketplace (deep pass)

| # | Item | Severity |
|---|------|----------|
| D-M1 | Property min/max filters accepted in `ValidateAuctionFilter` but **ignored** in `MatchesAuctionRequest`. | P1 |
| D-M2 | **FIXED 2026-06-03.** `ForceImmediate` commodity orders now skip resting-order slot limits/persistence, match immediately, and refund/return unmatched remainder without leaving a live order. | P1 fixed |
| D-M3 | **FIXED 2026-06-03 for delivery-helper, insert, bid-update, mail-save, item-auction won/return-mail same-save deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failures.** Expire/fill/buyout/cancel paths now leave auction/commodity state uncommitted when item delivery cannot be made through inventory or mail; settled auction delete persistence saves the moved item state instead of deleting the item row; auction/commodity insert DB failures and auction bid-update DB failures roll back transient state; marketplace mail-save failures restore attached item owner state before reporting delivery failure; item-auction won/return-mail settlement now composes mail creation, attached item save, and auction row deletion in one character DB save; online-inventory buyout delete-save failure returns `DbFailure` before buyer debit, seller credit, winner notify, auction removal, or item delivery; online-inventory cancel delete-save failure returns `DbFailure` before bidder refund, owner inventory return, or auction removal; online-inventory expiration delete-save failure returns before seller credit, winner notify, auction removal, or item delivery; direct commodity cancel/expiration delete-save failure returns or stays active before escrow refund, item recreation, notification, or order removal. Remaining final settlement cross-resource DB transaction/save failures remain open for commodity mail, offline rows, item state, and online/offline currency persistence. | P1 |
| D-M4 | **FIXED 2026-06-03.** `PostAuction` now rejects items that are not owner-matched inventory items, are soulbound, or are equippable bags before calling `Inventory.ItemRemove` or creating an auction row. | P1 fixed |
| D-M5 | **FIXED 2026-06-03.** `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` now documents unsupported auction property/rune/equippable filter rejection, auction cancel rollback, marketplace delivery-failure guards, and auction post tradeability validation. | Doc fixed |

**Tests missing:** retail property/rune/equippable filter implementation, exact multi-order commodity fill/partial-stack routing, remaining final settlement cross-resource DB transaction/save failures for commodity mail and offline credit/currency paths.

---

### P1 - Group (deep pass)

| # | Item |
|---|------|
| D-G1 | **FIXED 2026-06-03 for packet coverage.** `ServerGroupIdentityListAndUInt32Array` (0x0438) has reader-backed packet shape coverage, and `ServerGroupMemberFlagsChanged` (0x0437) now has field-level packet-shape coverage. Remaining 0x0438 leading/parallel uint32 semantics stay an explicit mapped-only blocker. |
| D-G2 | **FIXED 2026-06-03.** `function_labels.csv` now has durable labels for `ServerGroupMemberFlagsChanged_ReadPayload` (`0x0437`, `1400838c0`) and `ServerGroupIdentityListAndUInt32Array_ReadPayload` (`0x0438`, `140083990`). `0x0439`-`0x0440` remain intentionally unused in the enum. |
| D-G3 | **RESOLVED 2026-06-03.** `Group.StartReadyCheckAsync` still clears ready flags in-memory before `SetFlagAsync(Pending)` to avoid a double message, and the old #22 clear gap is closed by unconditional `ServerGroupReadyCheckStatusUpdate` emission with status `0`; `GroupMemberFlagsUpdated_BroadcastsReadyCheckStatusWhenPending` pins the pending packet. |
| D-G4 | **DOCUMENTED / TESTED 2026-06-03.** Current open-world player-vs-player attacks are active-duel-only through `Player.CanAttack` + `DuelManager.AreDueling`; instanced PvP match combat remains a separate map/match path. `PvpCombatBoundaryTests.OpenWorldPlayerTargetWithoutActiveDuel_IsNotAttackable` pins the no-duel boundary. Broader open-world PvP parity remains evidence-gated. |
| 24/25 | **FIXED 2026-06-03.** `GroupMemberFanoutHelper` resolves online members once per broadcast path, and `GroupMemberFlagsUpdatedHandler` now builds immutable role/flag + ready-check packets once per update before per-player enqueue. |

---

### P1 - Quests (deep pass)

| # | Item |
|---|------|
| D-Q1 | **RESOLVED 2026-06-03.** Local client SQL confirms quest `3487` objective `4489` is type `10` (`ScriptedTargetGroupChecklist`) with target group data `1322`; the current script type matches the quest data, so the type-14 cannon hypothesis does not apply to this quest. |
| D-Q2 | **FIXED 2026-06-03.** `InteractionObjectiveUpdater` suppresses generic activate-cast objective fan-out for scripted Q3487 cannon creature `11251`, avoiding unrelated `SucceedCSI`/`TalkTo` update attempts after `Q3487DominionCannonEntityScript.OnActivateSuccess` handles checklist credit. |
| D-Q3 | **FIXED 2026-06-03.** Q3667 objective **4770** is corroborated by local client SQL: `Quest2.tbl.sql:583` links it to quest `3667`, and `QuestObjective.tbl.sql:615` defines type `5` (`ActivateEntity`) for creature `11194`. |
| D-Q4 | **FIXED 2026-06-03.** Current `Q5594WarbotEntityScript` grants achievement `1730`, not `1296`, and guards with `HasCompletedAchievement` before `GrantAchievement`; `CrimsonIsleQuestChainTests.Q5594Warbot_OnKilled_GuardsAchievementAndCreditsObjective` pins both branches. |

---

### P2 - Network / decomp (deep pass)

| # | Item |
|---|------|
| D-N1 | `Decomp/Analysis/exports` **empty** - no Ghidra snippets for `1400a4260` in checkout. |
| D-N2 | No WorldServer emitters for 0x0347-0x0351, 0x05A1, 0x084B, 0x0855 - intentional modeled-only (F-003). |
| D-N3 | Resolved: `ServerEntityStatUInt32WideString` (`0x08CC`) now reuses shared `ServerUInt32WideStringPayload`; no `Server0x08CC` placeholder remains. |
| D-N4 | Resolved: `RETAIL_FEATURE_EVIDENCE.md` no longer references `Server0x05A1`; `0x05A1` is tracked as `ServerRealmAuxUInt32TripletList`. |
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
| **F-005** Marketplace 3/30 slots, mail settlement | **Aligned** (S7 + `MarketplaceAccountLimitsTests`) | Limits enforced; mail on win/outbid/expire. Escrow integrity, cancel return ordering, delivery-helper failure non-mutation, insert failure rollback, bid-update rollback, mail-save failure rollback, item-auction won/return-mail same-save auction deletion, online-inventory buyout/cancel/expiration delete-save rollback, direct commodity cancel/expiration delete-save rollback, and price-indexed commodity candidate selection are now guarded; remaining final settlement DB transaction/save atomicity remains open. |
| **F-005** partial fill / cancel precision | **Not sure** in retail doc | P0 cancel/escrow bugs fixed; multi-stack delivery uses conservative slot preflight; exact retail partial-stack routing remains worth testing. |
| **F-014** Need/Greed/Master/RR + trash rolls | **Certain** (Strain 1.0.9) | RR/Need/Master/FFA in `ConfigureLootDistribution`; trash roll **implemented**. Loot update/remove/notification packets are now wired; corpse group recipients now stay range-filtered; BoP timing remains a parity concern. |
| **F-014** `HarvestRule` (group) | Group loot rules in beta; **housing** harvest split is F-004 | Resource harvest now reads `GroupLootState.HarvestRule` with range and delivery preflight; corpse loot remains governed by normal loot rules. |
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
| D-L7 loot handler throws | **Resolved 2026-06-03.** `GlobalLootManager` now checks `LootInstance.HasLooter` immediately after owner/item resolution and returns before collect, roll, or master-assign invariant calls for non-looters; `GlobalLootManagerTests.LootRequestBoundaries_NonLooterReturnsWithoutThrowing` pins the boundary. |
| Offline master loot fixed row | **Resolved 2026-06-03.** `AssignMasterLoot` now checks `TryGetPlayer(assignee)` before `ResolveAssignedWinner` / broadcasts and returns `false` for offline assignees; `LootInstanceResolutionTests.AssignMasterLoot_OfflineAssigneeRejectsWithoutResolvingWinner` pins the no-mutation boundary. |
| R3-L1 item-use consume-before-cast | **Resolved 2026-06-03.** `ClientItemUseHandler` now preflights empty stack/charges, calls `TryCastSpell`, and consumes the item only after `CastResult.Ok`; `ClientItemUseHandlerTests` pin failed-cast/no-consume, success ordering, and empty-stack no-cast. |
| R3-L2 decor item-use consume-before-create | **Resolved 2026-06-03.** `ClientItemUseDecorHandler` now preflights empty stack/charges and residence access before consuming, catches housing access failures before mutation, and creates decor only after `Inventory.ItemUse` succeeds; `ClientItemUseDecorHandlerTests` pin blocked residence/no-consume, empty-stack no-op, consume-failure/no-decor, and success ordering. |
| R3-M7 system/marketplace mail return | **Resolved 2026-06-03.** `MailManager.ReturnMail` now rejects mail without a real player/GM sender id before moving it to outgoing, and `MailItem.ReturnMail` enforces the same invariant; mail tests pin non-player sender `MailCannotReturn` and no mutation. |
| R3-M4/R3-M5 marketplace settlement delivery | **Resolved 2026-06-03 for delivery-helper, item-auction won/return-mail same-save deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failures.** Commodity sell cancel/fill and auction expiry/buyout paths now preflight or require successful item delivery before destructive order, seller-credit, or removal mutations; commodity inventory delivery uses stack-size capacity preflight via handler-provided `IItemManager`, and mail fallback is gated by `MarketplaceMailDelivery.IsAvailable`. Insert, bid-update, mail-save, item-auction won/return-mail same-save auction deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failures are now guarded separately; remaining final settlement DB transaction/save atomicity remains open under F-005/F-013. |
| D-M4 auction post tradeability | **Resolved 2026-06-03.** `GlobalMarketplaceManager.PostAuction` now mirrors the conservative P2P trade boundary for listings: owner-matched item, inventory location, not soulbound, and not an equippable bag. Invalid posts return `AuctionCannotFillOrder` without inventory removal or listing creation. |
| D-M5 economy status doc | **Resolved 2026-06-03.** `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` records the C1-C4 marketplace guard set, unsupported property/rune/equippable filter rejection, auction cancel rollback, and D-M4 auction posting tradeability boundary. |
| D-G3 ready-check pending clear | Resolved 2026-06-03. The in-memory ready-flag clear before `Pending` is intentional de-duplication; the world handler now always emits `ServerGroupReadyCheckStatusUpdate`, including status `0` for pending/cleared state. |
| D-G4 open-world PvP boundary | Documented/tested 2026-06-03. `Player.CanAttack` keeps player targets non-attackable unless `DuelManager.AreDueling` is true; broader open-world PvP behavior remains blocked pending retail proof instead of being inferred from duel code. |
| #13 marketplace `UnknownArray` | **Resolved 2026-06-03.** Current code names this field `MicrochipIds`; `DeserializeMicrochipIds` now catches `FormatException` and `OverflowException`, logs the corrupt DB value, and returns an empty array instead of throwing during marketplace auction load. |
| D-Q3 Q3667 objective 4770 | Resolved 2026-06-03. Client SQL evidence is now recorded: `Quest2.tbl.sql:583` links objective `4770` to quest `3667`, and `QuestObjective.tbl.sql:615` defines it as type `5` (`ActivateEntity`) for creature `11194`. |
| D-Q4 Q5594 achievement | Resolved 2026-06-03. Current source grants achievement **1730** and guards duplicate grants with `HasCompletedAchievement`; the focused Crimson Isle quest-chain test covers both already-complete and not-complete branches. |
| D-Q1 Q3487 cannon objective type | Resolved 2026-06-03. `Quest2.tbl.sql:493` links quest `3487` to objectives `4489`, `4825`, and `4535`; `QuestObjective.tbl.sql:514` defines objective `4489` as type `10` (`ScriptedTargetGroupChecklist`) with target group data `1322`, matching the current cannon script. |
| D-Q2 Q3487 cannon generic fan-out | Fixed 2026-06-03. `InteractionObjectiveUpdater` now treats creature `11251` as script-handled for activate-cast objective credit, and `InteractionObjectiveUpdaterTests.UpdateActivateSuccessObjectives_Q3487CannonActivateCast_SuppressesGenericObjectiveUpdates` pins the no-generic-update boundary. |
| D-N1 empty exports | Stale. Local ignored exports now exist and include `ServerSupportSurveyList_ReadPayload` (`function_labels.csv:1074`, `exports/WildStar64.exe/functions.csv:1841`). |
| D-N3 `Server0x08CC` duplication | Fixed/refactored. `Server0x08CC` now inherits `Shared.ServerUInt32WideStringPayload` (`ServerUnresolvedOutputPackets.cs:471`). |

### P1 - Loot, item use, and delivery

| # | Item | Category |
|---|------|----------|
| R3-L1 | **FIXED 2026-06-03.** `ClientItemUseHandler` preflights empty stack/charges, uses `TryCastSpell`, and consumes only after `CastResult.Ok`; failed/dead/disabled casts no longer delete normal activated consumables. | Bug (fixed) |
| R3-L2 | **FIXED 2026-06-03.** `ClientItemUseDecorHandler` preflights stack/charges and residence access, catches `HousingException` before consume, and calls `DecorCreate` only after `Inventory.ItemUse` succeeds. | Bug (fixed) |
| R3-L3 | **FIXED 2026-06-03.** `TryDeliverHarvestLoot` now excludes out-of-range same-map group members, preflights each possible recipient with generated-loot validation and static-item delivery checks, and handles the one-valid-recipient case before advancing round-robin state. | Bug (fixed) |
| R3-L4 | **FIXED 2026-06-03.** Duplicate loot item merges now compute the merged amount as `ulong`, reject overflow with a warning, and keep the existing loot row amount unchanged instead of wrapping `uint`. | Bug (fixed) |

### P1 - Marketplace and mail

| # | Item | Category |
|---|------|----------|
| R3-M1 | **FIXED 2026-06-03.** Same-top-bidder auction increases now treat the existing held bid as escrow, checking/subtracting only the delta and avoiding a self-refund. | Bug (fixed) |
| R3-M2 | **FIXED 2026-06-03.** `ForceImmediate` commodity orders now match immediately without persistence or account-slot consumption, refund buy-order unmatched escrow, return sell-order unmatched items through inventory/mail, and avoid live unmatched remainders; partial-fill buy refunds now exclude the filled purchase cost. | Bug (fixed) |
| R3-M3 | **FIXED 2026-06-03 by rejection.** Unsupported auction property min/max filters, rune-slot filters, equippable-by filters, and `AuctionSort.Property` now throw during `ValidateAuctionSearch` instead of being accepted and ignored/default-sorted. Retail implementation remains blocked on item stat/rune/equippable evidence. | Bug (fixed by rejection) |
| R3-M4 | **FIXED 2026-06-03.** Commodity sell-order cancel and fill now require sufficient stack-slot capacity for direct inventory delivery or a successful mail fallback before removing/filling orders; insufficient buyer/owner slots with no mail leave orders unfilled/unremoved. | Bug (fixed) |
| R3-M5 | **FIXED 2026-06-03 for delivery-helper, item-auction won/return-mail same-save deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failures.** Auction expiry/buyout and commodity fill use boolean delivery helpers; expired auctions/commodity sell orders stay active when item delivery fails, and seller credit/order removal waits for delivery success. Insert, bid-update, mail-save, item-auction won/return-mail same-save auction deletion, online-inventory auction buyout/cancel/expiration, and direct commodity cancel/expiration delete-save failures are now guarded separately; remaining final settlement DB transaction/save atomicity remains open under F-005/F-013. | Bug (fixed; final DB atomicity open) |
| R3-M6 | **FIXED 2026-06-03.** Marketplace mail `ContentType` is persisted in `character_mail.contentType`; reload preserves explicit marketplace content types while legacy rows with `0` still fall back to sender-type defaults. | Bug (fixed) |
| R3-M7 | **FIXED 2026-06-03.** `MailManager.ReturnMail` rejects non-player/zero-sender mail with `MailCannotReturn` before moving it, and `MailItem.ReturnMail` throws before persisting `RecipientId = 0` when no player/GM sender exists. | Bug (fixed) |
| R3-M8 | **FIXED 2026-06-03.** Auction search page math now multiplies as `ulong` and returns an empty page when the requested skip exceeds `Enumerable.Skip`'s `int` boundary; `SearchAuctions_HugePage_ReturnsEmptyPageWithoutOverflow` pins `uint.MaxValue`. | Bug (fixed) |
| R3-M9 | **FIXED 2026-06-03.** Persisted commodity rows now validate id, owner, item, quantity, price, escrow multiplication/overflow, legacy `ForceImmediate`, and list/expiration ordering before load; corrupt rows are skipped with a warning before they can refund or deliver. | Bug (fixed) |

### P1 - Group, PvP, housing, matching, social

| # | Item | Category |
|---|------|----------|
| R3-G1 | **FIXED 2026-06-03.** `ClientGroupSetInstanceDifficulty` now publishes an authoritative group-server request; `Group.SetInstanceDifficultyAsync` requires the leader, preserves runtime group difficulty, sends the normal action result, and the world update handler syncs cached group state plus online member `InstanceDifficulty` before broadcasting `ServerGroupInstanceDifficultyResponse`. | Bug (fixed) |
| R3-P1 | **FIXED 2026-06-03.** PvP toggle-off now requests a player-owned pending disable cooldown instead of clearing `PvPFlag.Enabled` immediately; the update tick clears the flag and sends `ServerPvpCooldownClear` only after the cooldown expires. `character.pvpFlagDisableUntilUtc` now persists active pending cooldowns across logout/restart and clears on cancel/expiry. | Bug (fixed) |
| R3-H1 | **FIXED 2026-06-03.** Private residence visits now reject ordinary visitors with `Visit_Private` but allow owners/authorized modifiers through the same entrance teleport path used by other permitted visits. | Bug (fixed) |
| R3-H2 | **FIXED 2026-06-03.** Housing decor create now validates colour shift, scale, and non-crate plot position before `DecorCreate`, currency debit, or purchase achievement updates; invalid plot placement returns `Decor_InvalidPosition` without mutation. | Bug (fixed) |
| R3-Match1 | **FIXED 2026-06-03.** Missing queue state is idempotent on remove, and `MatchingCharacter.GetMatchingCharacterQueues()` now returns a snapshot so all-queue leave/requeue status paths cannot enumerate a live dictionary while removals occur. | Bug (fixed) |
| R3-Match2 | **FIXED 2026-06-03.** `MatchCleanup()` snapshots team members before calling `MatchLeave`, so cleanup can remove every member without invalidating the active team-member enumeration. | Bug (fixed) |
| R3-IC1 | **FIXED 2026-06-03.** ICComm scoped channels now revalidate group/guild affiliation on send and update; stale senders return `NotInChannel`, stale recipients are pruned before delivery, and update removes members who left their scoped guild/group. | Bug (fixed) |
| R3-X1 | **FIXED 2026-06-03.** Remaining raw WorldServer handler `PublishAsync` calls now use `FireAndForgetAsync`: `ClientChatJoinHandler`, `ClientChatModeratorHandler`, `ClientGroupInviteResponseHandler`, and both `ClientGroupLeaveHandler` branches. A handler-surface scan now finds no unwrapped `PublishAsync` calls. | Bug / Refactor (fixed) |

### P1/P2 - Quest, packet, and tracker hygiene

| # | Item | Category |
|---|------|----------|
| R3-N1 | **FIXED 2026-06-03.** `Get-DecompCoverageSnapshot.ps1` now treats both legacy `ServerNNNN`/`ClientNNNN` and current `Server0xNNNN`/`Client0xNNNN` opcode names as placeholders. The refreshed coverage snapshot restores `decodeState=placeholder`, `placeholderModeled=True`, and the placeholder decode queue for unresolved numeric packet models. | Bug (fixed) |
| R3-Q1 | **FIXED 2026-06-03.** `quest_implementation_audit.py` now scopes implementation-quality evidence to each `ScriptFilterOwnerId` block instead of the whole file, and uses the same parser for script-id discovery. The refreshed quest audit now reports 9 `curated_partial_stub_script` quests instead of hiding them in generic coverage. | Bug (fixed) |
| R3-Q2 | **FIXED 2026-06-03.** Remaining manual Northern Wilds follow-up grants are on the established helper path: `Q3671` now inherits `FollowUpQuestScript<T>`, and `Q3673` inherits the same base while preserving the Achieved cinematic hook before delegating completion grants. `Q3480` was already on `FollowUpQuestScript<T>`, and `Q5604` remains on the Crimson Isle shared multi-grant helper. | Refactor (fixed) |
| R3-Q3 | **FIXED 2026-06-03.** Quest script coverage is no longer construction-only: current `Quests` tests cover follow-up grants (`NorthernWildsQuestChainTests`, `CrimsonIsleQuestChainTests`, `TutorialQuestChainRoutingTests`), achievement repeat guards (`NorthernWildsUltrabotScriptTests`, `CrimsonIsleQuestChainTests`), cinematic-complete objectives (`CrimsonIsleBranchInteractionTests`, `TutorialCommunicatorCheckpointTests`), and targeted objective gates (`EarlyZoneEntityObjectiveCreditTests`, branch interaction tests). | Test (fixed) |

### Current priority waves

1. **Wave A (data-loss / crash guards):** complete as of 2026-06-03 (R3-L1, R3-L2, R3-L3, R3-L4, R3-M4/R3-M5, R3-M7 closed).
2. **Wave B (economy correctness):** complete as of 2026-06-03 (R3-M1, R3-M2, R3-M3, R3-M4/R3-M5, R3-M6, R3-M7, R3-M8, R3-M9 closed).
3. **Wave C (group/housing/matching state):** complete as of 2026-06-03 (R3-G1, R3-P1, R3-H1/R3-H2, R3-Match1/R3-Match2, R3-IC1 closed).
4. **Wave D (tracker/test cleanup):** R3-N1, R3-Q1, R3-Q2, R3-Q3, D-Q1-D-Q4, and D-G1-D-G4 packet/tracker coverage complete as of 2026-06-03. Remaining group work is the mapped-only 0x0438 leading/parallel uint32 semantics blocker.

Original addendum verification: read-only source inspection and subagent review.
R3-L1 closure verification (2026-06-03): the focused item/loot/stuck/activate
filter passed 25/25, and full
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
passed 2489/2489.
R3-L2 closure verification (2026-06-03): the focused decor item-use filter
passed 4/4, the broader item/housing filter passed 22/22, and full
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
passed 2493/2493.
R3-L3 closure verification (2026-06-03): the focused `GlobalLootManagerTests`
filter passed 8/8, and the broader loot filter
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
passed 61/61.
D-L8 closure verification (2026-06-03): the focused `GlobalLootManagerTests`
filter passed 9/9, and the broader loot filter
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
passed 62/62.
D-L9 closure verification (2026-06-03): the focused
`LootInstanceResolutionTests` filter passed 5/5, and the broader loot filter
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
passed 63/63.
Loot #4/#5 corpse recipient closure verification (2026-06-03): the focused
`GlobalLootManagerTests|LootInstanceResolutionTests|LootRequestHandlerTests`
filter passed 20/20 after adding the in-range corpse recipient regression.
R3-M7 closure verification (2026-06-03): the focused mail filter passed 21/21,
and full
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
passed 2495/2495.
R3-M4/R3-M5 closure verification (2026-06-03): the focused marketplace filter
passed 15/15, the broader marketplace/mail filter passed 21/21, and full
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
passed 2502/2502.
Marketplace #13 overflow closure verification (2026-06-03): the focused
`MarketplaceAuctionHandlerTests` filter passed 16/16 after adding the overflow
microchip-id parser regression, the broader marketplace/mail filter passed
22/22, and full
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
passed 2503/2503.
R3-M8 closure verification (2026-06-03): the focused `MarketplaceAuctionHandlerTests`
filter passed 17/17, the broader marketplace/mail filter passed 23/23, and
full
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
passed 2504/2504.
R3-M1 closure verification (2026-06-03): the focused `MarketplaceAuctionHandlerTests`
filter passed 18/18 after adding the same-top-bidder delta debit regression,
and the broader marketplace/mail guard passed 24/24.
D-M4/D-M5 closure verification (2026-06-03): the focused
`MarketplaceAuctionHandlerTests|MarketplaceAccountLimitsTests|MarketplaceOwnedListHandlerTests`
filter passed 33/33 after adding the auction tradeability regressions, and the
broader marketplace/mail filter passed 57/57.
F-005 online-inventory buyout delete-save guard verification (2026-06-03):
the focused `MarketplaceAuctionHandlerTests.AuctionBuyout_WhenInventoryDeliveryDeletePersistFails`
regression passed 1/1, the broader marketplace filter passed 47/47, and the
broader marketplace/mail filter passed 64/64.
F-005 online-inventory cancel delete-save guard verification (2026-06-03):
the focused `MarketplaceAuctionHandlerTests.AuctionCancel_WhenInventoryReturnDeletePersistFails`
regression passed 1/1, the broader marketplace filter passed 48/48, and the
broader marketplace/mail filter passed 65/65.
F-005 direct commodity cancel delete-save guard verification (2026-06-03):
the focused `MarketplaceAuctionHandlerTests.CommodityBuyOrderCancel_WhenDeletePersistFails|MarketplaceAuctionHandlerTests.CommoditySellOrderCancel_WhenInventoryReturnDeletePersistFails`
regressions passed 2/2, the broader marketplace filter passed 50/50, and the
broader marketplace/mail filter passed 67/67.
F-005 direct commodity expiration delete-save guard verification (2026-06-03):
the focused `MarketplaceAuctionHandlerTests.CommodityBuyOrderExpire_WhenDeletePersistFails|MarketplaceAuctionHandlerTests.CommoditySellOrderExpire_WhenInventoryReturnDeletePersistFails`
regressions passed 2/2, the broader marketplace filter passed 52/52, and the
broader marketplace/mail filter passed 69/69.
F-005 online-inventory expiration delete-save guard verification (2026-06-03):
the focused `MarketplaceAuctionHandlerTests.AuctionExpire_WithOnlineWinnerInventoryDeliveryDeletePersistFails`
regression passed 1/1, the broader marketplace filter passed 53/53, and the
broader marketplace/mail filter passed 70/70.
F-005/F-013 item-auction won-mail same-save delete verification (2026-06-03):
the focused `MarketplaceAuctionHandlerTests.AuctionBuyout_WhenMailPersistFails|MarketplaceMailSettlementTests.MarketplaceMailDelivery_AdditionalSaveFailure`
regressions passed 2/2, the broader marketplace filter passed 54/54, and the
broader marketplace/mail filter passed 71/71.
F-005/F-013 item-auction return-mail same-save delete verification (2026-06-03):
the focused `MarketplaceMailSettlementTests.MarketplaceMailDelivery_AdditionalSaveFailure|MarketplaceMailSettlementTests.MarketplaceMailDelivery_AuctionReturnAdditionalSaveFailure`
regressions passed 2/2, the broader marketplace filter passed 55/55, and the
broader marketplace/mail filter passed 72/72.
D-G3/D-G4 closure verification (2026-06-03): the focused
`GroupFlagHandlerTests|PvpCombatBoundaryTests|PvpFlagCooldownTests` filter
passed 11/11, and the broader group/PvP filter passed 190/190.
PvP #19/#20 closure verification (2026-06-03): the focused
`DuelManagerTests|PvpCombatBoundaryTests|PvpPacketShapeTests|PvpFlagCooldownTests`
filter passed 16/16 after adding declined-result and active-disconnect
regressions.
R3-L4 closure verification (2026-06-03): the focused loot filter
`FullyQualifiedName~LootInstanceResolutionTests|FullyQualifiedName~LootInstanceDeliveryTests|FullyQualifiedName~GlobalLootManagerTests`
passed 13/13.
D-Q1/D-Q2 closure verification (2026-06-03): the focused
`InteractionObjectiveUpdaterTests|EarlyZoneEntityObjectiveCreditTests|ClientActivateUnitCastHandlerTests`
filter passed 37/37, and the broader quest/entity filter
`Quests|InteractionObjectiveUpdaterTests|ClientActivateUnitCastHandlerTests`
passed 162/162.
