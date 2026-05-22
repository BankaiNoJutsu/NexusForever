# Gameplay / Economy / Social Workstream Status

Updated: 2026-05-22

Scope: matrix rows F-004 through F-015, F-027 through F-033 per `MISSING_FEATURE_MATRIX.md`.

Legend: **Partial** = real behavior exists but retail parity incomplete; **Blocked** = evidence gate prevents state mutation; **Verified** = boundary tests added this pass.

## Row Status

| ID | Status | Verification this pass | Primary blockers |
| --- | --- | --- | --- |
| F-004 Housing | Partial | Neighbor handlers + `residence_neighbor` EF + native `ServerHousingNeighbors` `0x0507` row sync + reserved row `+8` semantics + `ServerHousingCommunityDonateUpdate` `0x04FE` emit on donate + neighborhood list `0x0501/0506` + community plot reservation `0x051F` + community placement/privacy `0x053A/053B` emit paths + community rename result `0x078C` + interior wallpaper `0x050D` six-slot `HousingWallpaperInfo` id validation/cost debit/decor persistence + neighbor invite prompt/result/update packet coverage; packet-shape tests | Several neighborhood/community field names remain provisional; interior wallpaper ownership/unlock/refund precision remains unmapped |
| F-005 Marketplace | Partial | `MarketplaceAuctionHandlerTests` (11) + `CREDDExchangeHandlerTests` (5); durable `marketplace_auction` + `marketplace_commodity_order` EF; `GlobalMarketplaceManager` load/save/search, expiration sweep, `ServerAuctionOutbid`, offline DB credit settlement, commodity cross-match + cancel-failure `ServerCommodityOrderResult`, commodity cancel/return mail, `MarketplaceMailDelivery` + localized mail (`0x3950` → `278287`), CREDD `0x026A` non-empty price buckets + `097A` cache rows + durable `account_credd_*` history | Apply migration `20260522114611_MarketplacePersistence` on live DB; retail `0x026A` owned-order row pointers; commodity multi-order partial-fill precision |
| F-006 Storefront | Partial | Storefront/account/pending/CREDD/terminal/velocity/VC tests (`54` focused); daily-login claim (`078F` @ `1400070f0`); VC package (`082E` @ `1404f1d50`); coupon handler on `0790` wire; CREDD redeem (`0268`/`097B`); `0x026A` header+buckets (`14042b9a0`); `097A` cache rows; purchase success emits `098C`/`098D` (client `082A`/`0828`); `096A..096C` unused leading field (emit 0); purchase history (`098E`); catalog updated (`0989`); unlock sync (`0983`/`0984`); velocity gate (`0971`+`account_store_purchase_history`); migrations `120000`/`130000`/`140000` in tree | Real-money VC billing; `0x026A` owned-order tail; live `0986`/`098F` emit; non-zero `096A..096C` leading-field producers; native coupon sender for `0790` |
| F-007 Rewards | Partial | Per-content schedule catalog; `account_reward_rotation_grant` migration; `AccountRewardRotationRefreshProvider` non-empty `0x07C8`; **83** reward-filtered tests | `0x07CD` apply consumer; claim-path `RecordGrant`; player level/difficulty in schedule filter |
| F-008 Crafting | Partial | **New** `CraftingLootIdCraftHandlerTests` pins LootId craft path; existing simple/craft-item tests | Discovery/station math; `Server0x084B`/`0855`; rune/sigil semantics |
| F-009 Transport | Partial | Rapid transport / flight-path / vehicle packet tests | Service-token bypass; taxi completion; deployable vehicle semantics; `Server0x077E` |
| F-010 Group/matching | Partial | `GroupLootRulesHandlerTests`; `ServerGroupInstanceDifficultyResponse` (`0x0414`) typed + fan-out on `ClientGroupSetInstanceDifficulty` | Remaining cluster `042A..0718` emitters; `Client0x062A/0634`; durable raid locks; queue vote/replacement lifecycle |
| F-011 Guild/war party | Partial | War-party boss-token protocol tests | Guild bank/perks/holomarks; recruitment subscriptions; warplot plugs |
| F-012 ICComm/chat | Partial | ICComm/friendship/chat tests | Persistent channels; aux chat `01B8/01C1/01C4/01EF`; `Client0x0550` |
| F-013 Mail | Partial | Delete-from-view + `ServerMailUnavailable`; UTC instant expiry + `ServerMailItemDeprecation`; marketplace `ServerMailAvailable` online notify; **18** mail tests | Save/reload delete edge cases; broader atomicity |
| F-014 Loot | Partial | BoP two-step collect + `ServerLootBindOnPickup` `OwnerUnitId`/`LootUnitId`; `CanTryRoll`/`CanTryMasterAssign`; **34** loot tests | `ServerLootCanLoot` emit timing; parent source tracking |
| F-015 PvP/duels | Partial | Duel lifecycle tests | Observer/leash/warning packets; PvP cooldown persistence |
| F-027 Options | Partial | `OptionPersistenceTests` | Account-level option split; `056B..056D` readback |
| F-028 Support | Partial | Stuck failures via `ServerSpellCastResult` + spell4 ids; recall-house uses global residence entrance; **19** support tests | `Server0x0347..0351`; DB case workflow |
| F-030 Realm transfer | Partial | `RealmTransferProtocolTests` | Real destinations/results; `Client0x0760/0762`; PTR copy |
| F-031 Fortune | Partial | Fortune coin cost; card deal + flip payout via `FortuneRewardPool`; **15** fortune tests | Retail weight table; durable resume |
| F-032 Leaderboards | Partial | `LeaderboardProvider` + in-memory store + aggregation; **10** leaderboard tests | DB scores; live ingestion hooks |
| F-033 Challenges | Partial | `ChallengeManager` lifecycle + 18 challenge tests | Persistence; `Client0x00C8`; share-init opcode; reward tracks; objective hooks |

## Next implementation gates (priority order)

1. Ghidra field proof for remaining group cluster `0x042A..0x0718` and wire emitters.
2. Apply auth migrations: `20260522120000_AccountPendingItem`, `20260522130000_AccountCREDDExchange`, `20260522140000_AccountDailyLoginAndStoreHistory`, `20260522150000_AccountRewardRotationGrant`, character `20260522114611_MarketplacePersistence`.
3. Challenge DB persistence + combat objective hooks + `Client0x00C8` decode.
4. Fortune retail weight table + session DB resume.
5. Leaderboard DB + dungeon/PvP score ingestion.
6. Spell families F-016..F-020 (family-by-family evidence ladder).

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
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj` -> **722 passed** (2026-05-22).
