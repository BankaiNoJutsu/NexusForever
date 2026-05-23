# WildStar Addon Corpus Evidence (`I:\Wildstar Addons`)

Updated: 2026-05-23

This document records **player-addon evidence** from the local CurseForge-era zip corpus (876 archives, 875 with Lua). It complements `RETAIL_FEATURE_EVIDENCE.md` (wikis/Carbine Lua) and Ghidra decompile notes. Addon Lua shows **client API and Apollo event names** the retail UI expects; it does not prove server wire layout by itself.

## Corpus facts

| Metric | Value |
| --- | --- |
| Location | `I:\Wildstar Addons` |
| Archives | 876 `.zip` (flat folder) |
| Archives with `.lua` | 875 |
| Machine-readable scan | `artifacts/addon_apollo_api_scan.json`, `artifacts/addon_inventory.txt` |

## Evidence ladder (addon-specific)

| Level | Meaning |
| --- | --- |
| **Observed** | String appears in one or more addon sources |
| **Correlated** | Same API/event in multiple addons or Carbine `MatchMaker` / `Mail` addons |
| **Mapped** | Matches a named opcode/model in NexusForever or a Ghidra label |
| **Verified** | Emulator test or client smoke confirms behavior |
| **Blocked** | Client API exists; server backfill/policy still unknown |

## High-signal addons by subsystem

### Matching / group finder

| Addon | Why it matters |
| --- | --- |
| **EasyMatchMaker-2.6** | Full Group Finder UI clone; registers `MatchingAverageWaitTimeUpdated`, vote kick/surrender, LFR, eligibility |
| **QueueView-70923.0** | `MatchingGameLib.GetQueueEntry`, `IsLookingForReplacements`, `IsFinished` |
| **ReQueue-0.5.2b** | Re-queue after instance; hooks `MatchMakingLib.Queue` / `QueueAsGroup`; `MatchingEligibilityChanged` |
| **MatchQ_v0.8a** | Queue overlay; `GroupLib` leader checks |
| **BehavedGroupFinder** | Thin Group Finder helper |
| **bettercontentfinder** | Same matching Apollo events as EMM (penalty, votes, LFR) |

**Client libraries (14 addons reference `MatchingGameLib`):**

- `GetQueueEntry`, `IsFinished`, `IsInGameInstance`, `IsLookingForReplacements`
- `ConfirmRole` / `DeclineRoleCheck` / `IsRoleCheckActive`
- `CastVoteKick`, `CastVoteSurrender`, `CanVoteSurrender`, `CanLookForReplacements`
- `LookForReplacements`, `StopLookingForReplacements`, `LeaveGame`

**Apollo events (frequency in corpus):**

| Event | Addons | Emulator crosswalk |
| --- | --- | --- |
| `MatchingJoinQueue` | 9 | Queue join path emits `ServerMatchingQueueJoin` |
| `MatchingLeaveQueue` | 10 | `ServerMatchingLeftQueue` + status |
| `MatchingGameReady` | 12 | Match-ready proposal packets |
| `MatchingRoleCheckStarted` | 5 | Role-check manager |
| `MatchingEligibilityChanged` | 3 | `ServerMatchingEligibilityChanged` (0x05B8) |
| `MatchingAverageWaitTimeUpdated` | 1 (EMM) | **`ServerMatchingAverageWaitTimeUpdate` (0x0628)** — was modeled-only; now emitted on join/sample/login (2026-05-23) |
| `MatchingPenaltyUpdated` | 1 (BCF) | `ServerMatchingPenaltyUpdated` (0x05D9) via `MatchingDeserterManager` |
| `MatchVoteKickBegin` / `MatchVoteKickEnd` | 2 | `ServerMatchingMatchVoteKickBegin` / Failed / Cancelled / Succeeded |
| `MatchVoteSurrenderBegin` / `End` | 2 | `ServerMatchingMatchVoteSurrenderBegin` / Failed; warplot forfeit in `PvpMatch.Surrender` |
| `MatchLookingForReplacements` | 2 | Client-local after `0x05D5`; public AddOn Studio API events list the same event; runtime handler validates/logs only because server backfill remains unproven |
| `MatchStoppedLookingForReplacements` | 2 | Client after `0x0602`; runtime handler validates/logs only because close/merge semantics remain unproven |

### Loot

| Addon | Evidence |
| --- | --- |
| **AutoLoot-v1.3.2** (Carbine) | `GameLib.GetLootRolls`, `IsNeedRollAllowed`, `RollOnLoot`, `PassOnLoot`; `LootRollUpdate` event |
| **RaidOps***, **MasterLoot*** | `MasterLootUpdate`, `Group_LootRulesChanged`; extend Carbine master loot |

Trash Need/Greed at quality ≤1 aligns with `RetailCertainRules.TrashItemMaxQualityId`.

### Mail / marketplace

| Addon | Evidence |
| --- | --- |
| **MailHelper-v1.3.0** | Hooks Carbine `Mail` addon; bulk-open UI (retail `TakeAll` flow is client-side) |
| **BetterMarketplace-v2.3-release** | `MarketplaceCommoditiesSubmit`, listing UI |
| **MagicMail**, **ProtoMail** | Compose shortcuts; no new wire proof |

`ItemAuctionWon` Apollo event: 1 addon; strain patch + `MarketplaceMailDelivery` already in emulator.

### Housing

| Addon | Evidence |
| --- | --- |
| **HousingTour_v4** | `HousingLib.RequestRandomResidenceList`, `GetNeighborList`, `VisitNeighborResidence`, `RequestTakeMeHome` |
| **HousingRepair***, **HousingToolboxPro** | Decor/repair UI; pairs with `ServerHousingHarvestItemsSentToOwner` work |
| **NeighborNotes_256** | Uses `ICCommLib.JoinChannel("NeighborShare")` for peer-to-peer residence search/share data; this is addon chat data, not direct proof for `0x0501`/`0x0506` neighborhood packets |

### Challenges / leaderboards

- `ChallengeActivate` (22 addons), `ChallengeCompleted` (15) — UI trackers; emulator `ChallengeManager` max 2 concurrent.
- No addon references `Leaderboard` wire names; use Carbine `Leaderboards` addon (S7) for UI.

### Map tracked units / threat / crafting triage

Follow-up scan against the original zip corpus confirmed the proposed addon
strings, but only one item changes implementation confidence:

| Area | Addon evidence | Emulator / decomp correlation | Verdict |
| --- | --- | --- | --- |
| Map tracked unit | `GuardZoneMap_2.0`, `LUI_ZoneMap`, and `RavenMap_version1.3.1` register `MapTrackedUnitUpdate` / `MapTrackedUnitDisable` and call `GetMapTrackedUnitData(id)` for `label` / `iconPath`. | This matches `ServerMapTrackedUnitUpdate` (`0x0849`) and `ServerMapTrackedUnitDisable` (`0x0848`), **not** `0x0264`; `0x0264` is `ServerEntityCreateAuxScalarList`. Native consumers are mapped, but producer timing, tracked-unit id allocation, disable lifetime, and `TrackingSlotId` selection remain unknown. | **Observed / mapped consumer, blocked producer.** Do not auto-emit from quest objectives yet. |
| Threat list | `Threat-2.1.1`, `FrostMod_ThreatBall`, `BijiPlates`, and other frames register `TargetThreatListUpdated`; threat addons consume alternating unit/value arguments. | `ServerEntityThreatListUpdate` (`0x0909`) is mapped as source unit + five unit ids + five values; `ThreatManager.BuildServerThreatListUpdate()` already sends the owner's current target first when present, and broadcasts on add/change/remove. | **Already implemented.** No new work from addon evidence. |
| Crafting discovery hot/cold | `Athena-2.5.5a` references `CraftingLib.CodeEnumCraftingDiscoveryHotCold.{Cold,Warm,Hot,Success}` and colors/strings for those states. | Native `ServerCraftingFinish` (`0x0853`) consumer proves non-`Success` values fire the UI event, but the server-side discovery coordinate math and discovery-unlock mutation remain unmapped. Fixed-recipe craft success intentionally emits `Success`. | **Mapped display enum, blocked mechanic.** Do not randomize Cold/Warm/Hot. |
| Crafting API / stations | Multiple crafting addons use `GetKnownTradeskills`, `GetTradeskillInfo`, `GetSchematicInfo`, and `AddAdditive`. | These APIs expose client UI metadata; native sender helpers prove the station-unit-id source and split fixed-recipe zero-station behavior from additive non-zero-station behavior. | **Native-mapped policy, partial naming.** Fixed-recipe zero station is accepted and additive zero station is rejected; exact 0x2C/0x4F/0x57 service-key meanings stay blocked pending stronger native/sniff proof. |
| Rune slots | Online `RuneMaster_2.3.0` uses `item:GetRuneSlots().arRuneSlots`, `tRune.eElement`, `tRune.itemRune`, and `Item.CodeEnumRuneType.*`; AddOn Studio lists related `Item` APIs such as `GetGlyphInfo`, `GetMicrochipInfo`, and `GetSigils`. | Native `Lua_GameItemData_GetRuneSlots` now maps `nMaximum`, `nAbsoluteMax`, `nMinimum`, `arRuneSlots`, compact slot type bytes at `itemData+0x388`, and installed rune Item2 ids at `itemData+0x518 + index*4`. | **Mapped client Lua contract, blocked storage bridge.** Do not persist/broadcast rune state until shared item `RandomGlyphData`/`Glyphs` are proven to feed those live offsets and DB fields. |

## Emulator gaps closed or updated from this pass

| Gap | Addon evidence | Action (2026-05-23) |
| --- | --- | --- |
| Average wait UI stuck at “unknown” | EMM: `MatchingAverageWaitTimeUpdated` → `MatchingGame.GetAverageWaitTime()` | Emit `ServerMatchingAverageWaitTimeUpdate` on queue join, post-pop sample, login restore |
| Deserter cross-activity | ReQueue + winter beta (not addon-specific) | **Already wired**: `MatchingQueueValidator` → `CanQueue` (doc was stale) |
| Penalty UI | BCF: `MatchingPenaltyUpdated` | **Already wired**: `ServerMatchingPenaltyUpdated` |
| Vote kick cooldown | EMM vote events | **Implemented**: `Match.Retail.TryInitiateVoteKick` + retail constants |
| Warplot surrender | EMM `MatchMaker_SurrenderMatch` | **Implemented**: `PvpMatch.Surrender` |
| Replacement LFR | EMM `FindReplacements` / `IsLookingForReplacements` | **Mapped client surface, blocked server behavior**: `0x05D5`/`0x0602` are validated/logged only; queue anchor, close timing, and accepted replacement merge are not implemented without stronger evidence |
| Group stat block tail | Raid frame addons use runtime stats, not wire names | See `GROUP_MEMBER_STAT_BLOCK.md` |

## Still blocked (do not guess)

1. **Replacement queue backfill** - needs retail sniff/native evidence for queue anchor timing, accepted replacement merge, teleport, and multi-slot role fills.
2. **`StatBlockPrefix17` / `GroupMemberStatSlot`** — no addon exposes wire layout; decomp copier only.
3. **`ServerRaidQueueStatus` non-zero fields** — no addon reads queue IDs from that packet.
4. **Realm bank storage** — only `Altometer` mentions `RealmBank` strings; entitlements exist, persistence unverified.

## Suggested next captures

1. Sniff `0x0628` after queue join with EasyMatchMaker enabled (confirm Apollo fires).
2. Sniff `0x05D5` through accepted replacement teleport before implementing a server backfill path.
3. Live group roster with BuffedRaid + NF `ServerGroupMemberStatUpdate` to validate packed floats.

## Related repo files

- `Decomp/Analysis/MATCHING_IMPLEMENTATION_STATUS.md`
- `Decomp/Analysis/RETAIL_FEATURE_EVIDENCE.md`
- `Decomp/Analysis/GROUP_MEMBER_STAT_BLOCK.md`
- `Source/NexusForever.Game/Retail/RetailCertainRules.cs`
- `artifacts/addon_inventory.txt` (generated list, gitignored under `artifacts/`)
