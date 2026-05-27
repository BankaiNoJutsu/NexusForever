# LaughingWS Dungeon Chase SMC Placeholder Review

Updated: 2026-05-27

## Decision

Reject the Dungeon Chase hidden SMC storefront placeholder from current runtime
seeds. Keep it as a mapped-only blocker until a future storefront/client-wire
pass proves a different item-data type or packet schema that can safely carry
account item `86919`.

The rejection is regression-covered by
`LaughingWsStoreCatalogSeedTests.StoreCatalogSeed_DoesNotEmitDungeonChaseUnsupportedType0AccountItem`,
which requires the generated seed to retain the explanatory skip note while
omitting any emitted type `0` `store_offer_item_data` row for account item
`86919`.

## Source Row

`Events/Dungeon Chase.sql` contains a hidden Dungeon Chase SMC placeholder in the
store tables. The current store extractor canonicalises the file's legacy table
aliases (`store_offer_category` and `store_offer_data`) and repairs the known
`Store_offer_item` `Field_7` column typo for parsing, but the hidden SMC row is
declared as item-data type `0` with item id `86919`.

## Current Runtime Limits

- `Source/NexusForever.Database.World/Model/StoreOfferItemDataModel.cs` stores
  `ItemId` as `ushort`, so the world runtime schema cannot represent `86919` in
  `store_offer_item_data.itemId`.
- `Source/NexusForever.Network.World/Message/Model/ServerStoreOffers.cs`
  models type `0` offer item data as `ushort AccountItemId` and writes it with
  `writer.Write(AccountItemId, 15u)`.
- `Tools/DataMapping/extract_laughingws_store_catalog.py` therefore keeps
  `TYPE0_ACCOUNT_ITEM_MAX = 0x7FFF` and removes type `0` rows above that limit
  before emitting the seed.
- `Tools/DataMapping/sql/laughingws_store_catalog_seed.sql` records the skipped
  row as: `WIP/GUESSED Dungeon Chase hidden store placeholder skipped: type 0
  account item 86919 exceeds current 15-bit store wire/schema proof`.

`86919` is greater than both the current 15-bit type `0` transport maximum
(`32767`) and the current world schema `ushort` storage maximum used by the
runtime store item-data model.

## Required Future Proof Before Reopening

Reopen this only if a focused storefront pass maps one of the following:

- the row's source `type` is wrong or needs canonicalisation to a currently
  supported non-type-`0` item-data schema;
- the client reader for the relevant store offer item-data case accepts a wider
  field for this exact item path; or
- the runtime world schema and packet model are intentionally widened with
  client-reader proof, migration coverage, and packet/seed regression tests.

Until then, emitting this placeholder would either truncate/corrupt the item id
or require unsupported schema and wire behavior.
