# Placeholder rename tracker

Tracks `Unknown*` / opaque `Data*` placeholders in `Source/**` (excluding EF migration history).
Use the evidence ladder from `CONTINUATION_GUIDE.md`: Observed -> Correlated -> Mapped -> Verified -> Implemented.

**Inventory (2026-05-23):** ~761 `Unknown*` token matches in 176 `Source/**/*.cs` files (ripgrep); ~147 distinct symbol names; ~205 enum members (64 in `PrerequisiteType` alone).

## Initiative status — ACTIVE (2026-05-23 resume)

Decomp pass 130 added group stat-block layout mapping and retail catalog wire scalar names. Ghidra shared project was lock-blocked; used cached `140607490.fragment.c` and retail SQL.

## Initiative status — prior closure note

The 2026-05-23 closure pass marked the first tranche complete. Every in-scope placeholder was either **renamed with evidence** (below) or **explicitly blocked / out of scope** with Ghidra or runtime negative evidence. Remaining `Unknown*` in `Source/**` are intentional until a future pass adds per-field proof.

**In scope:** packet/account/storefront/pending-item/achievement/quest-objective placeholders touched by the 2026-05 decomp passes, plus correlated group stat-block packets (`0x0436` / `0x0468`).

**Out of scope (do not bulk-rename):** `PrerequisiteType.UnknownNNN` (64 members), generic `Network.World` packet tails and `Server0x*` unresolved rows, item wire offset names (`Unknown44`, …), `Stat` / `InventoryLocation` enums, table-backed `Game.Static` ids without client strings, and cosmetic renames (`Field6`, `PurchaseField0`, housing `Unknown0` without semantics).

**Re-open criteria:** new Ghidra consumer for a blocked offset, live capture with non-zero blocked wire fields, or NF runtime proof tying `QuestObjectiveType` / prerequisite id to behavior.

## Implemented (safe to use in code)

| Symbol | Real name | Evidence |
|--------|-----------|----------|
| Achievement `Data0` / `Data1` | `ProgressState` / `CreditedChecklistMask` | Client `Achievement_GetProgressValue` @ `140642b30`; NF achievement runtime |
| `QuestObjectiveType.Unknown10` | `ScriptedTargetGroupChecklist` | NF runtime + scripts |
| `QuestObjectiveType.Unknown20` | `CompletePublicEventRecipe` | `QuestObjective.Data` = `PublicEventObjective.Id` |
| `QuestObjectiveType.Unknown28` | `RescuePublicEventCreatures` | same |
| `QuestObjectiveType.Unknown31` | `CompletePublicEventObjective` | same |
| Account item flag `Unknown1` | `HasTargetPlayerIdentity` | `AccountInventoryItem_ReadPayload` @ `1400a9c20` |
| `ServerAccountCurrencyGrant` tail | `Reason` / `Reserved` | Packet shape tests |
| Storefront purchase shared payload | `PaymentCurrencySlot`, `PurchaseMoneyAmountBits`, `PurchaseOptionId`, `PurchaseExtensionId` | `Storefront_PurchaseCharacter_WritePayload` @ `1400acf80`; `StorefrontLib_PurchaseOffer` @ `1404f1150` |
| Storefront account purchase tail | `AccountPurchaseExtensionId`, `AccountTarget`, `RecipientName` | `Storefront_PurchaseAccount_WritePayload` @ `140080a20`; `Storefront_SendClientPurchaseAccountOffer` @ `1404507e0` |
| `PendingAccountItemGroup` wire fields | `SenderAccountId`, `TargetAccountId`, `HasTargetPlayerIdentity` (u64) | `PendingAccountItemGroup_ReadPayload` @ `1400a9b20`; cache insert @ `140005bf0`; NF `account_pending_item` columns |
| Group stat block | `StatBlockPrefix17`, `GroupMemberId`, `GroupMemberStatSlot` | `Group_CopyMemberStatBlockFromPayload` @ `140607490`; `GROUP_MEMBER_STAT_BLOCK.md` |
| Store catalog offer tail | `RetailCatalogWireScalar`, `RetailCatalogWireByte` | `RetailStoreOfferWireConstants.CatalogWireScalarBits`; retail `field_6`/`field_7`; not consumed in Apply @ `14044b750` |
| `AuctionInfo.UnknownArray` | `MicrochipIds` | NF marketplace persistence + shared `Item.Microchips` 3-bit wire array |
| `ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo.Unknown30`–`Unknown38` | `PrimeLevel`, `IsParty`, `Roles` | `MatchingQueuedPlayerInfo_ReadPayload` @ `140098500`; correlates with `ClientMatchingQueue_WritePayload` @ `140098a70` and `MatchingQueueJoinQueueData_ReadPayload` @ `1400989d0` |
| `ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo.Unknown3C` | `TrailingUInt32_0x3C` | `MatchingQueuedPlayerInfo_ReadPayload` @ `140098500`; uint32 wire confirmed, semantics blocked |
| `ServerSpellCastTargetReport.UnknownStructure0` / `unknownStructure0` | `TargetReportRow` / `TargetReportRows` | Partial row map (`CasterId` known); tail fields blocked |
| `PrerequisiteType.Spell221` / `Unknown269` | `ActionSetSpell` / `RapidTransport` | Table ids 221/269; client dispatch @ `1404a2100` cases `0xdd` / `0x10d` |
| `PrerequisiteType.Unknown170` | `GameFormula` | `value0` is `gameformula.id`; client case `0xaa` @ `1404a2100` (+0x90) |
| `PrerequisiteType.Unknown277` | `QuestObjective47OnCasterAndTarget` | Client case `0x115` requires caster **and** target to pass QuestObjective47 handler (+0x2f8) |
| `PrerequisiteType.Unknown292` | `PrimalMatrixNode` | `objectId0` matches `primalmatrixnode.id`; client case `0x124` (+0x6c8) |
| `PrerequisiteType.Unknown275` | `DoesNotOwnAccountItem` | All rows `NotEqual` and join `accountitem.item2Id`; client case `0x113` (+0x6a0) |
| `PrerequisiteType.Inventory` | `DoesNotOwnAccountItemOnCharacter` | All rows `NotEqual`; `value0` is account `item2.id`; client case `0xf6` (+0x130) |
| `PrerequisiteType.Unknown50` | `UnderSpellOnTarget` | Client case `0x32` calls `FUN_1404699f0` on target `param_2[1]`; `value0` is Spell4 id |
| `PrerequisiteType.Unknown59` | `SpellTier` | `objectId0` is Spell4 id; `value0` is tier rank; client case `0x3b` via `FUN_1404a4f60` |
| `PrerequisiteType.Unknown60` | `SpellTierOnTarget` | Client case `0x3c` calls `FUN_14046a110` on target `param_2[1]`; same row shape as `SpellTier` |
| `PrerequisiteType.Unknown56` | `DifficultyRankSlotAssigned` | Client case `0x38` checks populated pointer at `entity+0x2d8 + value0*0x10` when `value0 < 0x1c` |
| `PrerequisiteType.Unknown106` | `TargetEntityLookupHit` | Client `14049e900` (+0x318) resolves target identity `+0x1a0` via `FUN_14079ee60` |
| `PrerequisiteType.Unknown244` | `OwnsAccountItem` | Client `14049ca70` (+0xc0) checks account Item2 ownership on NPC types `0x14`/`0x17` |
| `PrerequisiteType.Unknown248` | `NpcInventoryItemCount` | Client `1404a0cb0` (+0x5c0) counts Item2 id on NPC via `1405f68f0` bag walk |
| `PrerequisiteType.Unknown279` | `UnderSpellOnCasterAndTarget` | Client case `0x117` requires caster **and** target to pass UnderSpell helper `FUN_1404a4ec0` |
| `PrerequisiteType.Unknown129` | `ActiveSpellEffectOnUnit` | Client case `0x81` inline walk of `entity+0x15c8` with SpellService compare on `value0` |
| `PrerequisiteType.Unknown130` | `ActiveSpellEffectOnTarget` | Client case `0x82` calls `FUN_140469a70` on caster with target `+8` Spell4 filter |
| `PrerequisiteType.Unknown82` | `UnitEntityType` | Client case `0x52` via `14049c5b0` (+0x48); entity `+0x80` unit type id vs `value0` |
| `PrerequisiteType.Unknown89` | `PetMatchTarget` | Client case `0x59` via `14049d4f0` (+0x160); pet lookup id match caster vs target |
| `PrerequisiteType.Unknown102`–`105` | `SpellCooldownNodeOnUnit` / `OnTarget` / `InServiceSet` / `SpellEffectTypeOnUnit` | Client cases `0x66`–`0x69` walk `entity+0x15d0` cooldown-node list |
| `PrerequisiteType.Unknown133`–`136` | `ItemStatData` / `ItemStatId` / `AppliedItemStatId` / `Item2Id` | Item eval handlers `1404a0d00`–`1404a0d90`; type `135` `objectId0` = `ItemStat.Id` @ `+0x144` |
| `PrerequisiteType.Unknown90` | `MountVehicleType` | Client `14049e730` (+0x2c0) mount subobject unit-type gate |
| `PrerequisiteType.Unknown191` | `PetEntitySpell4` | Client case `0xbf` inline `Prerequisite_ResolveSpellOnPetEntity` @ `1404695e0` on player/pet `entity+0x1600` |
| `PrerequisiteType.InfrastructureState` (148) | NF `PrerequisiteCheckInfrastructureState` | Client `1404a0150`; retail pairs `PathSettlerInfrastructure.Id` with `PathMission` type `21` (`0x15`) |
| `PrerequisiteType.PetEntitySpell4` (191) | NF `PrerequisiteCheckPetEntitySpell4` | Client case `0xbf`; vanity-pet summon proxy until `entity+0x1600` wrapper id is mapped |
| `PrerequisiteType.ItemRolledPropertyValue` (was `Unknown140`) | renamed pass 19 | `1404a0e80` + `14040e610` Property slot ids at `+0x94..+0xcc`; zero `Prerequisite.tbl` rows |
| `PrerequisiteType.ItemSpecial` (138) | NF handler pass 21 | `1404a0dd0` +0x114/+0x118; zero tbl rows typed 138 |
| `PrerequisiteType.ItemMicrochip` (139) | NF handler pass 21/24 | `1404a0e10` + `14049bdc0`; objectId0 7–0xd = `RuneType` socket bitmask via `IItem.RuneSlots`; wire `Microchips[]` separate |
| Augment microchip **install** opcode | pass 44/46 mapped (replication + partial patch) | **Client C2S install** = `0x085B` only. Wire **`Microchips[]`** on **full add** via `SharedItem_ReadPayload` → `140569c90` → `14056aa20`. **Existing-slot microchip-only patch** = `1403b8540` (caller PE `0x1403eef8a`, cluster `0x1403eed00`) — **player opcode still blocked** (pass 47: **not** `0x046F` guild bank) |
| Partial inventory slot field apply | pass 46 correlated | PE cluster `0x1403eed00`–`0x1403ef100` per-field patches on **player bag**; distinct from `0x0111` and from **`0x046F`/`0x047A` guild bank** |
| `ServerGuildBankInventoryAdd_ReadPayload` | pass 47–48 mapped | `140092580` = **`0x046F`** @ `14006c290`; apply `GuildBank_ApplyInventoryAdd` `14057d190`; apply-table cell **`140e1a760`** |
| `ServerGuildBankTabInventory_ReadPayload` | pass 47–48 mapped | `140092470` = **`0x047A`**; apply `GuildBank_ApplyTabInventoryRows` `14057cdc0`; apply-table cell **`140e1a718`** |
| Partial inventory field-apply gap | pass 48 inspect | Cluster **`0x1403eed00`**; microchip case **`0x1403eef8a`→`1403b8540`**; inspect fragments in cache; **not** spell jumptable `0x1403f1344`; opcode still blocked |
| `Prerequisite_CheckFaction` | pass 47 mapped | Type **128** case `0x80` @ `+0x68` → `14049c720` + `PlayerFactionService_IsFactionOrAncestor` `1407176b0` |
| `ItemAdded` native subscribers | pass 44 mapped | Only dispatcher `1403b8060` in `string_xrefs`; Lua ClientEvent paths + `140409330` case **9** `uItem` refresh; no additional labeled C++ subscriber in export cache |
| `Game.ItemData.GetDetailedInfo` | pass 45 mapped | `140417de0` PE registration at `.data` `0x140c58c10` with string `GetDetailedInfo`; builds `tPrimary`/`tCompare` (not `GetRuneSlots`) |
| Tooltip `tRunes` | pass 45 mapped | `140673ab0` reuses `140673b80` for tooltip context; distinct Lua key from `GetRuneSlots` / `GetDetailedInfo` |
| `PrerequisiteType.ItemMicrochip` objectId0=0 | pass 45 rejected on client | `1404a0e10` bitmask-only; rows 10610/10685 need NF count proxy until alternate client handler found |
| ItemSpecial row `+0x10` → item-eval `+0x114` | pass 35 correlated + rejected | SQL `spell4IdOnEquip`; augment rows hold Spell4 FK (81886+). Do not use as socket mask except rare values ⊆ socket-bit set (only id 3694=4 in reference DB) |
| Category **177** fusion-tier `item2TypeId` | pass 36 correlated | Types **515/516/530/563** (and empty **514/517**) map to `RuneType.Fusion` via `item2TypeId ≥ 502`; elemental band **340–345** unchanged (base **333**) |
| `Item.Build` sparse `Glyphs[]` | pass 37 implemented | `ItemRuneNetworkWire` emits one glyph id per `RuneSlots` index (0 = empty); wire→`itemData+0x518` producer still blocked |
| Wire → shared-runtime | pass 39 mapped | `SharedItemRuntime_ApplyParseBuffer` 140411b60: glyphs `parse+0x88` → runtime `+0x8f`; microchips via `ItemEval_ApplyItemSpecialToEval` → runtime `+0x20` (Inspect-only via 1403deff0) |
| Wire → entity inventory item | pass 40 mapped | `InventoryItem_ApplySharedItemPayload` 140569c90: glyphs `parse+0x88` → entity `+0xbc`; microchips `parse+0x78` → entity `+0x98`; compact sockets via `InventoryItem_RefreshItemState` entity `+0x60` |
| Entity item → item-eval | pass 41 mapped | `InventoryItem_RefreshItemState` 14056a430 → `ItemEval_ApplyItemSpecialToEval` with entity `+0xbc`/`+0x98`/`+0x60` |
| Entity `+0xbc` → shared-runtime `[0x8f]` | pass 42 mapped | `ItemEval_CommitRuneDataFromLinkedEntity` 140413520 on `ItemEval_CommitPendingRuneData` 140412ad0 reads `(*runtime)[0]+0xbc` |
| Entity `+0x60` → runtime socket fields | pass 42 mapped | Same commit copies entity `+0x60` compact bytes into runtime `+0x5d` region (GetRuneSlots `+0x388` proxy via `+0x71` index) |
| GetRuneSlots `+0x544` / `[0xa9]` gates | pass 43 mapped | Scratch-context indices alias embedded shared-runtime `+0x4a4` / `[0x95]`; filled by `ItemEval_SyncFromItemDataUserdata` `140410680` from Lua `Game.ItemData` userdata (`140417660` + `140410300`), not separate parent-only writers |
| Entity item → Game.ItemData | blocked (narrowed) | No direct entity→userdata writer; live `GetRuneSlots` reads userdata native blob synced via `140410680`; entity glyphs reach userdata through eval commit `140413520` + Lua refresh (`ItemAdded` / case 9), not `ItemModified`→`itemData` |
| Slot update → shared runtime | pass 41 mapped | `Inventory_ApplySlotUpdateAndDispatchItemAdded` 1403b8380 → slot `Entity_GetIndexedSlotEntry` + `ItemEval_CommitPendingRuneData` + `ItemAdded` |
| Wire → item-eval | pass 38 mapped | `ItemEval_SyncFromSharedItem` 140410300 copies runtime `+0x20`/`+0x158` into item-eval; `itemData+0x388`/`+0x518` writer still blocked |
| `FUN_14040f320` | `ItemRuneType_ToCompactSocketId` | RuneType 7–13 → compact 1–7 for `0x0859` `IsNotFusion` (`14051ed80`) |
| `PrerequisiteType.Unknown92`–`93` | `ActiveSpellTargetMechanic` / `Spell4EffectCategoryOnUnit` | Client cases `0x5c`/`0x5d` on active spell-effect list |
| `PrerequisiteType.Unknown280` | `SpellTierOnCasterAndTarget` | Client case `0x118` requires caster **and** target to pass SpellTier helper `FUN_1404a4f60` |
| `PrerequisiteType.Unknown77` | `QuestObjectiveOnTarget` | Client case `0x4d` (+0x548) passes target context; `objectId0` is QuestObjective id |
| `PrerequisiteType.Unknown172` | `HealthScaled` | Client case `0xac` inline: `Entity_GetHealthScaleFactor` `14047a940` * health `+0xd08+0x8c`; fallback `Creature2_GetRowByUnitId` `14022d500` on `+0xd8` |
| `PrerequisiteType.Unknown174` | `ItemTradeSkillKnown` | `PrerequisiteTypeHandlerTable[174]` → `Prerequisite_CheckItemTradeSkillKnown` `1404a17e0` → `TradeSkill_ClientHasKnownItem2Id` `1403b91d0` (known-recipe list binary search); NPC types `0x14`/`0x17` |
| `PrerequisiteType.ItemTradeSkill` (173) | enriched | Same table path `1404a1790` → `TradeSkill_CheckItemTradeskillRequirement` `1403c16e0` |
| `PrerequisiteType.Unknown178` | blocked | Table handler `1404a1890` = entity-id match + `Progress_GetTrackedScalar` `1403fa980`; reject tbl “Equipped” hint |
| Prerequisite handler evidence | pass 49 | Use **`DAT_140b66870` / `PrerequisiteTypeHandlerTable[typeId]`** for handler bodies; **`PTR_FUN_140b67740` vtable slots** often disagree (destructor/UI) — do not rename from vtable PE pointer alone |
| `AchievementEntry.Value` | `RequiredProgress` | Achievement progress runtime |
| `Quest2Entry.FactionLevelCompPreq*` | `FactionLevelRequireAtMostPreq*` | Quest prerequisite comparison semantics |
| `RewardRotationModifierEntry.Value` | `ModifierValue` | Reward rotation schedule builder |
| Reputation packets `Value` | `ReputationDelta` / `ReputationAmount` | Packet shape tests |
| `GroupCharacter.UnknownStruct1` | `PrimeLevelInfo` / `PrimeLevels` | `MatchingPrimeLevelInfo_ReadPayload` @ `1400ad150` |
| `GroupCharacter.Unknown11`–`Unknown22` | `Health` … `HealingAbsorbMax` (packed `ushort` vitals) | `GroupCharacter_ReadPayload` @ `140082420` `+0x54`..`+0x6a`; same set as `ServerGroupMemberStatUpdate` |
| `GroupCharacter.Unknown28`/`Unknown29` | `PhaseFlags1` / `PhaseFlags2` | Parsed `+0x94`/`+0x98`; matches `ServerGroupMemberStatUpdate` @ `140083d30` |
| Spell damage `UnknownStructure3` | `TrailingStructure` / `TrailingStructures` | `SpellDamageDescription_ReadPayload` @ `1400946c0`; row reader `SpellDamageTrailingRow_ReadPayload` @ `1400945e0` (seven damage uint32s + 3-bit tail; row consumer blocked) |
| `TrailingStructure.Unknown0`–`Unknown7` | `RawDamage` … `GlanceAmount`, `DamageType` (3-bit) | Same field widths as parent `DamageDescription` via `SpellDamageTrailingRow_ReadPayload` @ `1400945e0`; per-row gameplay use blocked |
| `ServerSpellEffectNestedTargets` placeholders | `ServerUniqueId`, `Spell4EffectId`, `TargetId`, `DamageDescriptions` | Reader @ `140095810`; shared damage row @ `1400946c0`; structural diff vs `0x07F6` (`ServerSpellEffectDamage_ReadPayload` @ `140095910`) |
| `0x07F6` post-parse consumer | `SpellWrapper_ApplyServerEffectDamage` @ `1405a5800`, `SpellWrapper_DispatchEffectDamageCombatLog` @ `14053f3f0` | Via `Entity_ExecuteSpellEffectHighRange` jump case `1403ee268`; combat-log primary row only; trailing rows still blocked |
| `ServerP2PTradeUpdateItem` unknown tail | `RandomCircuitData`, `RandomGlyphData`, `ThresholdData`, `WorldRequirement_Item2Id`, `Item2IdMicrochip[5]` | Client opcode `0x018E` read `FUN_1400aa2b0` @ `1400aa2b0`, write `FUN_1400aa030` @ `1400aa030`; same tail as mail `ServerMailAvailable.Attachment` minus charges and eight glyph slots |

## Mapped - storefront purchase (`0x082A` / `0x0828`) - detail reference

Decompiled 2026-05-23 (`InspectCodeAddress` on `WildStar64.exe`).

**Wire order** (`Storefront_PurchaseCharacter_WritePayload` @ `1400acf80`):

| Index | Width | Client source (`StorefrontLib_PurchaseOffer` @ `1404f1150` -> `Storefront_SendClientPurchaseCharacterOffer` @ `140450720`) | Proposed name | Confidence |
|-------|-------|------------------------------------------------------------------------------------------------------------------|---------------|------------|
| `[0]` | u32 | Offer id (`uVar3`) | `OfferId` | Verified |
| `[1]` | 5-bit | `Game.Money` field at `+0x14` (`param_4`) | `PaymentCurrencySlot` | Implemented |
| `[2]` | u32 | `Game.Money` amount float bits (`param_5`) | `PurchaseMoneyAmountBits` | Implemented |
| `[3]` | 14-bit | Global table constant `*(DAT_140c7aac0+8)` | `CurrencyId` (NF property name) | Verified in handlers |
| `[4]` | u32 | Lua purchase arg 3 (`plVar13`) | `PurchaseOptionId` | Implemented |
| `[6..]` | Identity | Current player `+0x1a0/+0x1a8` | `Target` | Verified |
| `[10]` | u32 | Lua purchase arg 4 (`uVar5`) | `PurchaseExtensionId` | Implemented |

NF handlers currently use only `OfferId` and `CurrencyId` (14-bit field); other fields are client-side pricing/UI context.

Account route adds after shared payload: `AccountPurchaseExtensionId` (u32 at struct `+0x30`, same Lua arg 4 binding as `PurchaseExtensionId` but second wire slot), `AccountTarget`, `RecipientName` (`Storefront_PurchaseAccount_WritePayload` @ `140080a20`).

## Mapped - `ServerStoreOffers` offer row (`0x098B`)

`ServerStoreOffers_Offer_ReadPayload` @ `1400a0dc0` struct layout (client parse):

| Offset | Size | NF wire | Retail DB | Status |
|--------|------|---------|-----------|--------|
| `+0x00` | u32 | `Id` | `id` | Verified |
| `+0x08` | wstr | `Name` | `name` | Verified |
| `+0x10` | wstr | `Description` | `description` | Verified |
| `+0x18` | 8 | `PricePremium` / `PriceAlternative` | (derived in NF from prices table) | Verified |
| `+0x20` | u32 | `DisplayFlags` | `displayFlags` | Verified |
| `+0x28` | i64 | `RetailCatalogWireScalar` | `field_6` (`RetailStoreOfferWireConstants.CatalogWireScalarBits`) | **Mapped** - retail wire passthrough; Apply @ `14044b750` does not copy |
| `+0x30` | 8 bits | `RetailCatalogWireTrailingByte` | `field_7` (usually `0`) | **Mapped** - `FUN_14006be30` @ `1400a0dc0` |
| `+0x34` | u32 | currency row count | - | Verified |
| `+0x40` | u32 | item row count | - | Verified |

Do **not** rename to bare `Field6`/`Field7`; use `RetailCatalogWireScalar` / `RetailCatalogWireByte` for the documented passthrough.

## Blocked — per-field (closed without rename)

| Symbol | Evidence summary |
|--------|------------------|
| `PendingAccountItemGroup.Unknown2` | Wire `+0x10`; cache `[2]` @ `140005bf0`; lookup by `Id` only @ `140007810`; gift UI @ `140519260` uses group string at cache `+0x38` and UI target fields — not slot `[2]` |
| `GroupCharacter.StatBlockPrefix17` | 17-bit prefix at parsed `+0x1c` in stat block; copied to client `+0x98`; gameplay meaning blocked |
| `GroupMemberStatSlot.Value` | Five-row ushort in stat block; per-row semantics blocked |
| `GroupCharacter.Unknown10` | `uint32` after mentoring (`+0x50`); no labeled consumer |
| `QuestObjectiveType.Unknown27` / `Unknown29` | No localization / single-digit table counts; no NF runtime handler |

## Blocked buckets (do not rename without new evidence)

| Bucket | Count | Notes |
|--------|------:|-------|
| `PrerequisiteType.UnknownNNN` | 64 | Table ids + generic failure strings only — **out of scope** |
| `Network.World` packet tails | ~400 refs | Many `Server0xNNNN` shapes decoded but event semantics open — **out of scope** |
| `Stat.Unknown*`, `InventoryLocation.Unknown*` | ~15 | No client stat/location enum strings |
| `EntityCreateFlag.Unknown08` | 1 | Used in cinematics; no label |
| Marketplace `AuctionInfo.Unknown2` | 1 | No consumer label |
| Item packet offset names (`Unknown44`, etc.) | many | Layout known, semantics not |

## Mapped - `PendingAccountItemGroup` (`0x0976` / `0x0979`) - detail reference

| Offset | NF property | Notes |
|--------|-------------|-------|
| `+0x00` | `Id` | Pending item id |
| `+0x08` | `AccountItemId` | |
| `+0x10` | `Unknown2` | **Blocked** - stored in client cache `[2]`; grouped-map lookup uses `Id` at `+0x00` only (`AccountPendingItemGroupCache_LookupByPendingItemId` @ `140007810`); gift/claim UI paths read `TargetAccountId` at `+0x38`, not this field |
| `+0x18` | `Group` | Wide string key |
| `+0x20` | `SenderAccountId` | Implemented |
| `+0x28` | `SenderIdentity` | |
| `+0x38` | `TargetAccountId` | Implemented; account-gift target (`ClientAccountItemGiftPendingItemGroupToAccount`) |
| `+0x40` | `ClaimState` | 5-bit |
| `+0x48` | `HasTargetPlayerIdentity` | Wire u64; NF emits `0`/`1` |
| after | `TargetIdentity` | Second identity in 0x60 struct |

## Deferred (future passes — not part of closed initiative)

1. `PendingAccountItemGroup.Unknown2` — live `0x0979` capture with non-zero `+0x10`, or Lua/UI reading cache `[2]`.
2. `ServerStoreOffers.Offer.Unknown6`/`Unknown7` — any client path outside catalog Apply that consumes `+0x28/+0x30` (purchase history @ `14044c540` uses a different row layout).
3. `0x082A` purchase `[3]` 14-bit — live sniff vs `AccountCurrencyType` if handlers diverge from retail.
4. Group roster tail — decompile consumer for `GroupCharacter.Unknown10` (`uint32` @ parsed `+0x50` after mentoring).

## Deferred - structurally mapped packet opcodes

| Opcode | Structure | Evidence | Blocker |
|--------|-----------|----------|---------|
| `Client0x0760` | `RealmInfo` row | `Client0x0760_WritePayload` @ `1400abd30`; field order mirrors `ServerRealmList` realm entries | Native send-site or consumer event before semantic rename |
| `Client0x0762` | `NetworkMessage` row | `Client0x0762_WritePayload` @ `1400ac410`; field order mirrors `ServerRealmList` message entries | Native send-site or consumer event before semantic rename |

## Verification (initiative closure)

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj `
  --filter "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~AccountInventoryPendingGroupTests|FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~StorefrontPurchaseHandlerTests" -v minimal --nologo
```

## Commands

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 0 `
  -ExtraPostScript InspectCodeAddress.java -ExtraPostScriptArgs @('1400acf80') -SkipCoverage
```

Log: `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`
