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
| `PrerequisiteType.Unknown170` | `GameFormula170` | `wildstar_client.prerequisite` rows use `value0` as `gameformula.id` (`1029`, `1031`, `1050`); client case `0xaa` @ `1404a2100` |
| `PrerequisiteType.Unknown277` | `QuestObjective47Both` | Client case `0x115` @ `1404a2100` requires caster **and** target to pass QuestObjective47 handler (+0x2f8) |
| `PrerequisiteType.Unknown292` | `PrimalMatrixNode` | `objectId0` matches `wildstar_client.primalmatrixnode.id` (`18`, `20`, `28`, `30`); client case `0x124` (+0x6c8) |
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
