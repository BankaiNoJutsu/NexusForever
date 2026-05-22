# Gameplay / Economy / Social Workstream Status

Updated: 2026-05-22

Scope: matrix rows F-004 through F-015, F-027 through F-033 per `MISSING_FEATURE_MATRIX.md`.

Legend: **Partial** = real behavior exists but retail parity incomplete; **Blocked** = evidence gate prevents state mutation; **Verified** = boundary tests added this pass.

## Row Status

| ID | Status | Verification this pass | Primary blockers |
| --- | --- | --- | --- |
| F-004 Housing | Partial | Neighbor handlers + `residence_neighbor` EF + native `ServerHousingNeighbors` `0x0507` row sync + reserved row `+8` semantics + `ServerHousingCommunityDonateUpdate` `0x04FE` emit on donate + neighborhood list `0x0501/0506` + community plot reservation `0x051F` + community placement/privacy `0x053A/053B` emit paths + community rename result `0x078C` + interior wallpaper `0x050D` six-slot `HousingWallpaperInfo` id validation/cost debit/decor persistence + neighbor invite prompt/result/update packet coverage; packet-shape tests | Several neighborhood/community field names remain provisional; interior wallpaper ownership/unlock/refund precision remains unmapped |
| F-005 Marketplace | Partial | `MarketplaceAuctionHandlerTests` (9); durable `marketplace_auction` + `marketplace_commodity_order` EF; `GlobalMarketplaceManager` load/save, expiration sweep, `ServerAuctionOutbid`, offline DB credit settlement, commodity cross-match with `ServerCommodityAuctionFilledPartial`, `MarketplaceMailDelivery` + mail `ContentType` + shared achievement-text localized mail (`0x3950` → `278287` for item/commodity), `MarketplaceTransactionFee` from `GameFormula` `436`/`437` with minimum fee `1090`, fixed 48h item-auction listings (`821`), commodity `FILETIME` tiers `1056` validated on post, expired auctions with bids complete offline | Apply migration `20260522114611_MarketplacePersistence` on live DB if tables missing |
| F-006 Storefront | Partial | Storefront/account packet tests | Offline pending-group delivery; `0986/098C/098D/098F` variant semantics; durable CREDD persistence |
| F-007 Rewards | Partial / Blocked | `ServerRewardPropertySet` protocol tests | Live schedule/entry-state; `Server0x07CD` |
| F-008 Crafting | Partial | **New** `CraftingLootIdCraftHandlerTests` pins LootId craft path; existing simple/craft-item tests | Discovery/station math; `Server0x084B`/`0855`; rune/sigil semantics |
| F-009 Transport | Partial | Rapid transport / flight-path / vehicle packet tests | Service-token bypass; taxi completion; deployable vehicle semantics; `Server0x077E` |
| F-010 Group/matching | Partial | **New** `GroupLootRulesHandlerTests`; existing flag/matching tests | Group server cluster `0414..0718` emitters; `Client0x062A/0634`; durable raid locks; queue vote/replacement lifecycle |
| F-011 Guild/war party | Partial | War-party boss-token protocol tests | Guild bank/perks/holomarks; recruitment subscriptions; warplot plugs |
| F-012 ICComm/chat | Partial | ICComm/friendship/chat tests | Persistent channels; aux chat `01B8/01C1/01C4/01EF`; `Client0x0550` |
| F-013 Mail | Partial | Mail transaction tests | Multi-client delete parity; exact expiration; marketplace mail uses plain strings pending localized text IDs |
| F-014 Loot | Partial | **New** loot notify/collect/vacuum delegation tests; roll/assign tests | `Server0x08A0/08A8`; bind-on-pickup confirmation semantics |
| F-015 PvP/duels | Partial | Duel lifecycle tests | Observer/leash/warning packets; PvP cooldown persistence |
| F-027 Options | Partial | `OptionPersistenceTests` | Account-level option split; `056B..056D` readback |
| F-028 Support | Partial | Support/survey handler tests | DB case workflow; moderation UI |
| F-030 Realm transfer | Partial | `RealmTransferProtocolTests` | Real destinations/results; `Client0x0760/0762`; PTR copy |
| F-031 Fortune | Partial | Fortune packet-shape tests | Reward generation/payout evidence; durable resume |
| F-032 Leaderboards | Partial | `LeaderboardProviderTests` (empty compat) | Rank aggregation store; category rules from client readers |
| F-033 Challenges | Partial | `ChallengeChoiceHandlerTests` (GenericFail) | Accept/decline/update sequence; `ServerChallengeUpdate` lifecycle |

## Next implementation gates (priority order)

1. Confirm provisional neighborhood/community field names and interior wallpaper ownership/unlock/refund semantics.
2. Live DB migration `20260522114611_MarketplacePersistence` on environments still missing marketplace tables.
3. Offline `pendingGroups` delivery in `AccountInventoryManager` after gift flow evidence.
4. Challenge accept/decline client sequence → `ServerChallengeUpdate` / quest hooks.
5. Fortune reward table / payout evidence before inventory linkage.

## Tests added (2026-05-22)

- `Source/NexusForever.Game.Tests/Crafting/CraftingLootIdCraftHandlerTests.cs`
- `Source/NexusForever.Game.Tests/Group/GroupLootRulesHandlerTests.cs`
- `Source/NexusForever.Game.Tests/Loot/LootRequestHandlerTests.cs` (notify, collect, vacuum)

Focused F-004 gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingCommunityUpdateHandlerTests|FullyQualifiedName~ClientHousingCommunityRenameHandlerTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ClientHousingVisitResidenceHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"` -> **68 passed**.

Interior wallpaper focused gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ResidenceTests|FullyQualifiedName~PacketPlaceholderNamingTests"` -> **59 passed**.

Earlier broad workstream filter before the housing-neighbor cluster covered **544 passed**.

Current full game test gate:
Last clean `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj` -> **625 passed**. Current full gate was not rerun while unrelated marketplace worktree changes are ignored; the latest scoped F-004 gate above passed.
