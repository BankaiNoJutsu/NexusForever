# Gameplay / Economy / Social Workstream Status

Updated: 2026-05-25 (F-031 fortune weight audit; third-pass fortune/leaderboard/challenge blockers)

Scope: matrix rows F-004 through F-015, F-027 through F-033 per `MISSING_FEATURE_MATRIX.md`.

For the 36-row completion snapshot and consolidated emit gaps, see [`CURRENT_STATUS.md`](../../CURRENT_STATUS.md).

Legend: **Partial** = real behavior exists but retail parity incomplete; **Blocked** = evidence gate prevents state mutation; **Verified** = boundary tests added this pass.

## Row Status

| ID | Status | Verification this pass | Primary blockers |
| --- | --- | --- | --- |
| F-004 Housing | Partial | Neighbor handlers + `residence_neighbor` EF + native `ServerHousingNeighbors` `0x0507` row sync + reserved row `+8` semantics + `ServerHousingCommunityDonateUpdate` `0x04FE` emit on donate + neighborhood list `0x0501/0506` packet shapes only + community plot reservation `0x051F` + community placement/privacy `0x053A/053B` emit paths + community rename result `0x078C` + interior wallpaper `0x050D` six-slot `HousingWallpaperInfo` id validation/cost debit/decor persistence + neighbor invite prompt/result/update packet coverage; packet-shape tests | Neighborhood list producer trigger/fields, several neighborhood/community field names, and interior wallpaper ownership/unlock/refund precision remain unmapped |
| F-005 Marketplace | Partial | `MarketplaceAuctionHandlerTests` (11) + `CREDDExchangeHandlerTests` (5); durable `marketplace_auction` + `marketplace_commodity_order` EF; `GlobalMarketplaceManager` load/save/search, expiration sweep, `ServerAuctionOutbid`, offline DB credit settlement, commodity cross-match + cancel-failure `ServerCommodityOrderResult`, commodity cancel/return mail, `MarketplaceMailDelivery` + localized mail (`0x3950` → `278287`), CREDD `0x026A` non-empty price buckets + `097A` cache rows + durable `account_credd_*` history | Apply migration `20260522114611_MarketplacePersistence` on live DB; retail `0x026A` owned-order row pointers; commodity multi-order partial-fill precision |
| F-006 Storefront | Partial | Storefront/account/pending/CREDD/terminal/velocity/VC tests (`54` focused); daily-login claim (`078F` @ `1400070f0`); VC package (`082E` @ `1404f1d50`); coupon handler on `0790` wire; CREDD redeem (`0268`/`097B`); `0x026A` header+buckets (`14042b9a0`); `097A` cache rows; purchase success emits `098C`/`098D` (client `082A`/`0828`); `096A..096C` unused leading field (emit 0); purchase history (`098E`); catalog updated (`0989`); unlock sync (`0983`/`0984`); velocity gate (`0971`+`account_store_purchase_history`); migrations `120000`/`130000`/`140000` in tree | Real-money VC billing; `0x026A` owned-order tail; live `0986`/`098F` emit; non-zero `096A..096C` leading-field producers; native coupon sender for `0790` |
| F-007 Rewards | Partial | Per-content schedule catalog; `account_reward_rotation_grant` migration; `AccountRewardRotationGrantManager` + claim `RecordGrant` path; non-empty `0x07C8` on refresh/claim; **83** reward-filtered tests | `0x07CD` apply/`Flag` consumer; item/currency delivery after claim; player level/difficulty schedule filter |
| F-008 Crafting | Partial | `CraftingLootIdCraftHandlerTests` pins LootId craft path; simple/craft-item tests now pin fixed-recipe zero-station success plus non-zero station/tradeskill mismatch rejection; `CraftingAdditiveHandlerTests` pins additive zero-station rejection; `CraftingPacketShapeTests` pin `ServerCraftingCurrentCraft`, native `CodeEnumTradeskillResult` and `CodeEnumRuneType` values, native `GetRuneSlots` live item-data offsets, and corrected typed `0x084B`/`0x0855` auxiliary payloads; `CraftingDiscoveryEvaluatorTests` pins coordinate-discovery distance bands, attempt-coordinate parsing, and station service-key mapping (`0x2C`/`0x4F`/`0x57`) | Live validation of attempt-coordinate packing; profession-modifier scaling for discovery radii; `ServerCraftingCurrentCraft` mid-craft emit cadence; aux opcode emit intent; shared-item/DB bridge for durable rune item data and non-success sigil result rules |
| F-009 Transport | Partial | Rapid transport / flight-path / vehicle packet tests | Service-token bypass; taxi completion; deployable vehicle semantics; `Server0x077E` |
| F-010 Group/matching | Partial | `GroupLootRulesHandlerTests`; `ServerGroupInstanceDifficultyResponse` (`0x0414`) typed + fan-out on `ClientGroupSetInstanceDifficulty`; ready-check status (`0x0441`) now emits for Pending/Ready/HasSetReady flag updates; replacement LFR `0x05D5`/`0x0602` validation/logging only; `ServerRaidQueueStatus` (`0x0718`) zero-value compatibility emit with raid-info (`ClientRaidInfoRequestHandlerTests`) | Non-zero raid queue semantics; `Client0x062A/0634` sender meaning; durable raid locks; replacement server backfill/merge lifecycle |
| F-011 Guild/war party | Partial | War-party boss-token protocol tests | Guild bank/perks/holomarks; recruitment subscriptions; warplot plugs |
| F-012 ICComm/chat | Partial | ICComm/friendship/chat tests | Persistent channels; aux chat `01B8/01C1/01C4/01EF`; `Client0x0550` |
| F-013 Mail | Partial | Delete-from-view + `ServerMailUnavailable`; UTC instant expiry + `ServerMailItemDeprecation`; marketplace `ServerMailAvailable` online notify; **18** mail tests | Save/reload delete edge cases; broader atomicity |
| F-014 Loot | Partial | BoP two-step collect + `ServerLootBindOnPickup` `OwnerUnitId`/`LootUnitId`; `CanTryRoll`/`CanTryMasterAssign`; **34** loot tests | `ServerLootCanLoot` emit timing; parent source tracking |
| F-015 PvP/duels | Partial | Duel lifecycle tests; active duels now send left-area/cancel-warning packets and cancel after the leash timeout | Observer/reward parity; PvP cooldown persistence |
| F-027 Options | Partial | `OptionPersistenceTests` | Account-level option split; `056B..056D` readback |
| F-028 Support | Partial | Stuck failures via `ServerSpellCastResult` + spell4 ids; recall-house uses global residence entrance; **19** support tests; `SupportPacketShapeTests` now pin typed `0x0347..0x0351` auxiliary payloads | DB case workflow and support-case readback semantics |
| F-030 Realm transfer | Partial | `RealmTransferProtocolTests` | Real destinations/results; `Client0x0760/0762`; PTR copy |
| F-031 Fortune | Partial | Fortune coin cost; emulator rarity-tier `FortuneRewardPool` + `RewardItemProbabilities`; `account_fortune_session` persistence code path; `FORTUNE_WEIGHT_AUDIT.md` catalog/table audit; **17** fortune tests | Exact per-item retail weights and active rotation catalog (no tbl weight column, no local live Fortune capture) |
| F-032 Leaderboards | Partial | `DatabaseLeaderboardStore` + `LeaderboardProvider` + `LeaderboardScoreIngestion` + aggregation; **10** leaderboard tests | Season/filter semantics; broader live score ingestion coverage |
| F-033 Challenges | Partial | `ChallengeManager` lifecycle + `character_challenge` persistence + 18 challenge tests | `Client0x00C8` decode; share-init opcode; reward tracks; combat objective hooks |

## Next implementation gates (priority order)

1. ~~Ghidra field proof for remaining group cluster `0x042A..0x0718` and wire emitters.~~ **Done:** cluster typed + emitters; `0x0441` ready-check status emits on Pending/Ready/HasSetReady updates.
2. Apply auth/character migrations on live DB when missing: `20260522120000_AccountPendingItem`, `20260522130000_AccountCREDDExchange`, `20260522140000_AccountDailyLoginAndStoreHistory`, `20260522150000_AccountRewardRotationGrant`, `20260522190000_AccountFortuneSession`, `20260522190000_CharacterChallenges`, `20260522114611_MarketplacePersistence`.
3. ~~Challenge DB persistence~~ **Done in tree** (`character_challenge`); remaining: combat objective hooks + `Client0x00C8` decode.
4. Fortune per-item retail weights (rarity-tier weights + UI probability transport implemented; live retail `ServerFortuneRewards` or storefront catalog capture still needed).
5. ~~Leaderboard DB~~ **Done in tree** (`DatabaseLeaderboardStore`); remaining: season rules + broader ingestion hooks.
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

Earlier broad workstream filter before the housing-neighbor cluster covered **544 passed**.

Current full game test gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo` -> **943 passed** (2026-05-23).
