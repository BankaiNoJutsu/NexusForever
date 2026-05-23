# Placeholder rename tracker

Tracks `Unknown*` / opaque `Data*` placeholders in `Source/**` (excluding EF migration history).
Use the evidence ladder from `CONTINUATION_GUIDE.md`: Observed -> Correlated -> Mapped -> Verified -> Implemented.

**Inventory (2026-05-23):** ~733 `Unknown*` references in 153 files; ~147 distinct symbol names; ~205 enum members (64 in `PrerequisiteType` alone).

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
| `+0x28` | i64 | `Unknown6` | `field_6` (almost always `-1016071787` / float bits ~`-239.97`) | **Blocked** - parsed by `ServerStoreOffers_Offer_ReadPayload` but not consumed in `Storefront_ApplyServerStoreOffers` @ `14044b750` (Apply copies id/strings/prices/currency rows only) |
| `+0x30` | 8 | `Unknown7` | `field_7` (usually `0`) | **Blocked** |
| `+0x34` | u32 | currency row count | - | Verified |
| `+0x40` | u32 | item row count | - | Verified |

Do **not** rename `Unknown6`/`Unknown7` to `Field6`/`Field7`; that duplicates opacity.

## Blocked buckets (do not rename without new evidence)

| Bucket | Count | Notes |
|--------|------:|-------|
| `PrerequisiteType.UnknownNNN` | 64 | Table ids + generic failure strings only |
| `Network.World` packet tails | ~400 refs | Many `Server0xNNNN` shapes decoded but event semantics open |
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

## Next decomp targets (highest ROI)

1. `PendingAccountItemGroup.Unknown2` - live sniff or Lua/UI frame code that reads cache `[2]`; grouped lookup and gift paths do not use it.
2. `ServerStoreOffers.Offer.Unknown6`/`Unknown7` - catalog apply drops them; opcode `0x098E` purchase-history rows use a different `+0x28` layout (`Storefront_HandleStorePurchaseHistoryReady` @ `14044c540`).
3. Confirm `0x082A` `[3]` 14-bit field against live `CurrencyId` / `AccountCurrencyType` (sniff or Lua `Game.Money` layout).
4. Group roster `GroupCharacter.Unknown10`-`Unknown22` - map `ServerGroupMemberDetailUpdate` consumer.

## Commands

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 0 `
  -ExtraPostScript InspectCodeAddress.java -ExtraPostScriptArgs @('1400acf80') -SkipCoverage
```

Log: `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`
