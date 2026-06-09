# NexusForever Feature Restoration - Current Status

Last updated: 2026-06-09 (decompile-evidence closure checkpoint; F-011 war-party boss-token packet boundary; F-011 guild recruitment packet-family cached-export recheck; F-022 active path helper static-table guard; F-022 path mission static-table guard; F-004 residence entrance static-data guard; F-035 achievement primary-table startup guard; F-014 creature DropLoot Creature2 partial-table guard; F-026 item-manager item/item-slot partial-table guard; F-026 item display-source partial-table guard; F-022 quest target-group cache partial-table guard; F-006/F-026 entitlement-manager partial-table guard; F-006 account-currency partial-table guard; F-007 reward-property partial-table guard; F-014 generated loot reward-table guard; F-005 auction search selector-table guard; F-022 path reward scanbot profile guard; F-022 path reward Spell4 table guard; F-022 path reward title partial-table guard; F-026 title manager persisted-title partial-table guard; F-026 account-item generic-unlock partial-table guard; F-026 persisted generic-unlock partial-table guard; F-026 normal item-use partial-table guard; F-008 tradeskill request partial-table guard; F-008 additive modifier materialization partial-table guard; F-008/F-011 rune/additive partial-table guard; F-026 scanbot rename ownership/type guard and vanity-pet summon effect guard; F-027 combat-log disable-others boolean guard; F-034 datacube table-unavailable guard; F-035 Steam achievement ingest blocked classification; F-036 zone-completion non-title reward blocked classification; F-033 challenge reward/share-init blocked classification; F-032 leaderboard medal/season filter blocked classification; F-028 support-case readback blocked classification; F-031 Fortune reward-pool partial-table guard; F-008 crafting fixed-recipe partial-table guard; F-026 repair-vendor formula-table guard; F-011 guild create-cost formula-table guard; F-004 housing vendor-list partial-table guard; F-004 housing community-rename formula-table guard; F-026 decor item-use HousingDecorInfo partial-table guard; F-009 transport table-unavailable coverage; F-021 non-spell action-set item partial-table guard; F-012 emote partial-table guard; mapped small client request partial-table guard; F-036 generic-map partial-table guard; F-026 generic-unlock manager partial-table guard; F-026 account-costume partial-table guard; F-022 quest guidance partial-table guard; F-022 quest-info reward partial-table guard; F-022 character-XP partial-table guard; F-022 path-level partial-table guard; F-009 rapid-transport spell-formula partial-table guard; F-026 pet customisation invalid type/slot guard; F-006 account-item terminal missing-inventory guard; F-031 Fortune flip missing-inventory guard; F-030 realm-transfer known-character guard; F-036 zone-completion stale table-cache guard; F-034 persisted datacube duplicate-row merge guard; F-032 leaderboard duplicate-score dedupe guard; F-035 achievement checklist-bit mask guard; F-033 challenge progress overflow guard; F-017 Charged Shot charge-release threshold runtime and continuous/charge spell-cast target forwarding; F-005 item-auction offline seller settlement guard, commodity buy-order offline expiration refund guard, commodity-fill offline-credit guard, auction bidder-refund offline-credit guard, direct commodity fill order-mutation persistence gate, and multi-order price-priority fill coverage; item-data aux packet contracts for `0x056B`, `0x056C`, and `0x056D`, marketplace aux packet contracts for `0x06DF` and `0x07D5`, chat aux row/envelope packet contracts for `0x01B8`, `0x01C1`, and `0x01C4`, plus story/recruitment boundary packet contracts for `0x074A` and `0x077E`; ported useful LaughingWS branch scripts/data overlays, quest-loot/store/catalog/WIP-Dust-Stalker-quest-instance/WIP-live-event/Skyplot-housing overlays, settler build acknowledgements, active Settler hub build-count progress, WIP current-zone path episode activation with optional PathMission prerequisite filtering, active-path object-id completion guard, active Soldier assassinate kill progress with decompile-mapped ProgressCount, branch-informed active-only node/explore-zone/power-map-validated Explorer progress completion, PathMission and PathMissionType achievement credit, client-mapped GameFormula 0x017a path XP fallback for known completed path missions without configured XP, and unflagged PathRewardType.Mission grants for known completed path missions, branch starter-zone/map-only hooks, Shade's Eve/Infestation/Fragment Zero/Gauntlet/Ruins of Kel Voreth/Stormtalon's Lair/Skullcano/Initialization Core Y-83/Red Moon Terror/Genetic Archives/Sanctuary of the Swordmaiden/Datascape/Protogames/Space Madness/Evil from the Ether event/map chains, WIP-guessed Coldblood Citadel, Ruins of Kel Voreth, Sanctuary of the Swordmaiden, Skullcano, and Stormtalon's Lair optional-objective rolls, Space Madness/Gauntlet/Infestation/Protogames Academy/Fragment Zero/Shade's Eve/Red Moon Terror/Genetic Archives/Datascape/Ruins of Kel Voreth/Skullcano/Initialization Core Y-83/Sanctuary/Evil from the Ether/Cryo-Plex/War of the Wilds trigger objective/message/teleport/PvP scripts and phase broadcasts, SQL-backed boss objective-credit hooks including WIP Red Moon Terror Laveka credit, Skullcano and Sanctuary WIP route scaffolding, remaining map-only bindings plus WIP entry/boss/phase/cinematic-hook scaffolds for Deep Space, Rage Logic, Ultimate Protogames dungeon/raid, Protostar SuperMall, Journey into OMNICore-1, Fragment Zero, Infestation, Space Madness, Shade's Eve, Ruins of Kel Voreth, Stormtalon's Lair, Initialization Core Y-83, Genetic Archives, Datascape, and Gauntlet, narrow branch spell-script hooks for Marauder Mine/Pulse Blast, and the client-table-backed Exo-Lab 22 range teleporter; reward-rotation schedule row order/player-level/difficulty refresh, runtime `item_salvage` exact/type-level salvage path, live group/guild CDB evidence, and full Game test project 2817/2817; focused branch/path/event tests 552/552, focused instance/public-event tests 436/436, focused map-only entry/boss/phase/cinematic-hook scaffold tests 22/22, focused event/trigger cinematic-hook tests 155/155, focused PvP/adventure branch tests 25/25, focused Evil from the Ether tests 12/12, focused Coldblood Citadel tests 10/10, focused Ruins of Kel Voreth tests 16/16, focused Sanctuary of the Swordmaiden tests 38/38, focused Skullcano tests 34/34, focused Stormtalon's Lair tests 19/19, focused Protogames Academy trigger tests 25/25, focused Fragment Zero trigger tests 13/13, focused Gauntlet trigger tests 16/16, focused Infestation tests 9/9, focused Datascape/objective-credit tests 102/102, path-progress tests 36/36, focused transporter tests 49/49, focused quest tests 284/284, focused XP/rest-XP tests 237/237, focused costume tests 7/7, focused generic-unlock tests 22/22, focused account-item/generic-unlock tests 67/67, focused generic-map tests 6/6, focused vendor repair tests 10/10, focused item-use tests 9/9, broader item-use tests 20/20, focused crafting partial-table tests 12/12, focused tradeskill request tests 5/5, focused rune/modifier partial-table tests 63/63, broader crafting tests 90/90, focused Fortune reward-pool tests 6/6, broader Fortune tests 27/27, focused Option tests 36/36, focused Pet tests 293/293, focused Support tests 126/126, focused archive/datacube tests 18/18, broader PathManager tests 67/67, focused Steam achievement tests 2/2, focused achievement tests 25/25, focused Challenge tests 45/45, focused mapped-request tests 5/5, focused emote tests 2/2, broader chat tests 6/6, focused zone-completion tests 12/12, broader map tests 457/457, plus branch spell tests 11/11; F-004/F-009/F-024 remain partial)

Supplemental update: 2026-06-09 decompile-evidence closure checkpoint.
The current evidence-gated protocol/native backlog now has a named state and
next evidence source in `Decomp/Analysis/FEATURE_BACKLOG_WORKING_QUEUE.md`,
`MISSING_FEATURE_MATRIX.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`,
`MATCHING_IMPLEMENTATION_STATUS.md`, `ENTITY_AUX_DECODE_ROADMAP.md`, and
`BLOCKER_EVIDENCE_PLAN.md`. This is not a retail-parity claim and no runtime
behavior was widened. Rows remain `Mapped only`, `Blocked`, `Rejected`, or
diagnostic-only unless the focused tracker states `Implemented`. The next
evidence sources are native apply/producer paths, opcode-specific senders or
consumers, dynamic apply-dispatch proof, accepted packet captures, or retail
server/catalog artifacts; live-client, content, setup, and data-mapping
validation remain intentionally out of scope until re-enabled.

Supplemental update: 2026-06-09 F-001 STS token/optional auth
cached-export/source recheck kept the optional token/RSA branch mapped-only and
blocked. Ghidra MCP discovery found no running instances, so no labels or
exports were added. Tracked `StsConnLib64.MT.dll` labels plus cached fragments
reconfirm the native token-login path as separate from the implemented
password/SRP flow: `StsConn_SendLoginTokenStart` (`180003d70`) sends
`ClientRand` for transaction `0x61`, `StsConn_SendTokenKeyData` (`18000a730`)
reads `ServerRand`, `ServerPublicKey`, and `ServerSignature`, validates them
through `StsConn_ValidateTokenServerKeyMaterial` (`180012de0`), creates an RSA
client through `StsCrypt_CreateRsaClient` (`180037cb0`), then sends
`PremasterSecret`, `AuthnToken`, optional `AuthProviderCode`, and `AppId` for
transaction `0x3b`. `StsConn_SendRequestToken` (`180004c40`) and
`StsConn_SendAssociateMyExternalAccount` (`1800067d0`) also remain mapped
external/token support paths, while `StsConn_OnAuthnTokenResponse`
(`180008230`) reads only the reply `AuthnToken`. Current source has handlers
for SRP login/key data, login finish, request/consume game token, user-info,
verified-IP, game-account, and presence compatibility routes, but no
`/Auth/LoginTokenStart`, `/Auth/TokenKeyData`, `/Auth/RequestToken`, or
`/Auth/AssociateMyExternalAccount` models/handlers. No runtime behavior changed;
the exact RSA key format, signature/trust anchor, premaster derivation,
TokenKeyData reply fields, post-token session/crypto transition, and startup
route ordering still need native proof or live STS captures before
implementation.

Supplemental update: 2026-06-09 F-002 structural diagnostic realm/addon
cached-export/source recheck kept `ClientAccountRealmData`,
`ClientRealmListRealmRow`, `ClientRealmListMessageRow`, and
`ClientAddonModuleList` mapped-only. Ghidra MCP discovery found no running
instances, so no labels or exports were added. Cached `WildStar64.exe`
fragments reconfirmed `ClientAccountRealmData_WritePayload` (`1400aba70`) as
one nested account-realm row, `Client0x0760_WritePayload` (`1400abd30`) and
`Client0x0762_WritePayload` (`1400ac410`) as realm-list row/message serializers,
and `ServerRealmList_WritePayload` (`1400ac770`) as the proven parent row
serializer. The realm structural rows still have no standalone sender or
consumer event. Cached `ClientUnresolvedDiagnosticPacket07B6_WritePayload`
(`140080220`), row helper `14007ff70`, `Client0x07B6_SendFromModuleList`
(`1403f42e0`), and `Lua_GetAddons` (`140043370`) still support addon-module
structural naming and prove the module-list walk, but not server-side consumer
intent. Current source/tests keep all four handlers log-only with no gameplay
state mutation or server emit.

Supplemental update: 2026-06-09 F-003 `Server0x0015`
cached-export/source recheck kept the lone server placeholder mapped-only.
Ghidra MCP discovery found no running instances, so no labels or exports were
added. Cached `WildStar64.exe` fragments reconfirmed `ServerUInt5UInt32_ReadPayload`
(`140081f00`) as the shared 5-bit plus `uint32` reader registered for both
`0x0015` and matching `0x0628`, with `ServerFortuneRewards_ReadPayload`
(`140081f60`) calling the same reader as a money-reward row helper. The
positive matching apply path remains `MatchingManager_ApplyMatchingAverageWaitTimeUpdated`
(`1405c0e00`), which consumes the shared row as match type plus average wait
and dispatches `MatchingAverageWaitTimeUpdated`; no equivalent `0x0015`
apply/producer/post-read consumer surfaced. Current source/test guards keep
`Server0x0015.Value0`/`Value1` neutral and non-emitted.

Supplemental update: 2026-06-09 F-005 marketplace aux `0x06DF` / `0x07D5`
cached-export/source recheck kept both marketplace aux packets mapped-only.
Ghidra MCP discovery found no running instances, so no labels or exports were
added. Cached `WildStar64.exe` fragments reconfirmed
`ServerAuctionPostAux_ReadPayload` (`140090090`) as count, counted `uint32`
array, counted byte array of the same count, and trailing `uint32`, and
`ServerAuctionsByFilterAux_ReadPayload` (`14008fe80`) as one 14-bit value,
three `uint32` fields, and one flag. `Network_RegisterServerOpcode_0351`
(`14006c290`) still only registers `0x06DF` at size `0x20` and `0x07D5` at
size `0x14`; selected call edges show only parsing helpers, and current source
finds these packets only in packet models and packet-shape tests. No static
marketplace apply helper, runtime producer, field consumer, or emit timing proof
surfaced. Keep both packets non-emitted until a native apply/producer path,
retail/live marketplace capture, or server producer witness proves field
semantics and timing.

Supplemental update: 2026-06-09 F-007 reward rotation `0x07CD`
cached-export/source recheck kept the content-context packet mapped-only.
Ghidra MCP discovery found no running instances, so no new labels or exports
were added. Cached `WildStar64.exe` fragments still show
`ServerRewardRotationContentContext_ReadPayload` (`14008fcb0`) registered for
`0x07CD` with a null static handler, `RewardRotation_ManagerInit`
(`140635840`) initializing seven throttle slots at
`manager + 0x150 + index * 0x14`, `Reward_SendRewardUpdateRequest`
(`140636ba0`) sending only the index through `0x07CC`, and
`RewardRotation_GetLoadedScheduleForContent` (`140636c40`) proving refresh
request/loaded-schedule lookup rather than `0x07CD` apply semantics. Current
source already keeps `ServerRewardRotationContentContext.UInt0`/`UInt1`/`UInt3`
neutral and records the missing apply-helper/`Flag`/throttle-slot blocker. No
runtime behavior changed; next evidence remains a retail `0x07CD` capture or a
dynamic breakpoint on the runtime apply dispatch.

Supplemental update: 2026-06-08 F-022 / audit F-027 active path helper
static-table guard implemented; active path mission helpers now treat missing
`PathMission`, `PathSoldierTowerDefense`, `PathSoldierAssassinate`,
`PathSettlerImprovementGroup`, and `PathSettlerHub` static data like missing
rows. Explorer explore-zone completion, active object-id completion, Soldier
tower/assassinate progress, Settler hub improvement progress, and Settler hub
progress percent helpers now return no-progress/zero-progress instead of
throwing when those tables are unavailable. `PathManagerTests` pin the
missing-table no-completion boundaries. Focused PathManager verification passed
81/81 and broader path verification passed 172/172 from alternate output
directories. Exact path reward presentation, unlock sequencing, current-zone
activation proof, per-mission producer timing, exact Soldier/Settler retail
semantics, and broader content smoke remain evidence-gated.

Supplemental update: 2026-06-08 F-022 / audit F-027 path mission
static-table guard implemented; `PathManager.CompleteMission()` and Explorer
mission completion helpers now treat missing `PathMission` static data like
missing mission rows, preserving completion/reward behavior for direct
completion and no-progress behavior for Explorer node/power-map hooks. Explorer
vista completion now treats a missing `PathExplorerNode` table like no matching
node rows, and power-map completion treats a missing `PathExplorerPowerMap`
table like a missing power-map row. `PathManagerTests` pin direct missing-table
mission completion plus Explorer missing-table no-completion boundaries. Focused
PathManager verification passed 72/72 and broader path verification passed
163/163 from alternate output directories. Exact path reward presentation,
unlock sequencing, current-zone activation proof, per-mission producer timing,
and broader content smoke remain evidence-gated.

Supplemental update: 2026-06-08 F-004 residence entrance static-data guard
implemented; `GlobalResidenceManager.GetResidenceEntrance()` now treats missing
`HousingPropertyInfo` and `WorldLocation2` tables/rows like missing residence
entrance data and throws the existing `HousingException`; `ResidenceEntrance`
now treats missing `World` table/row the same way. `GlobalResidenceManagerTests`
pin missing-table, empty-table, and table-backed entrance boundaries. Focused
verification passed 7/7 and broader housing verification passed 121/121 from
alternate output directories. Neighborhood `0x0501`/`0x0506`, edit-mode
ack/broadcast, community/session precision, decor ownership/refund precision,
and exact entrance UI/timing remain evidence-gated.

Supplemental update: 2026-06-08 F-035 / audit F-042 achievement
primary-table startup guard implemented; `GlobalAchievementManager.Initialise()`
now treats a missing `Achievement` table like an empty achievement cache, and a
missing `CharacterDatabase` service like no persisted realm-first preload.
Unknown achievement ids still return `null`, character/guild achievement lists
still return empty sequences for unavailable types, and table-backed player vs
guild indexing remains unchanged. `GlobalAchievementManagerPartialTableTests`
pin missing-table, empty-table, and table-backed cache boundaries. Focused
global achievement verification passed 3/3 and broader achievement verification
passed 32/32 from alternate output directories. Steam payload grammar,
achievement-id correlation, data-only/unowned achievement families, exact
realm-first UI/timing, and broader achievement UI edge cases remain
evidence-gated.

Supplemental update: 2026-06-08 F-014 / audit F-019 creature
DropLoot Creature2 partial-table guard implemented; `GlobalLootManager.DropLoot`
now treats a missing `Creature2` table like a missing creature row before loot
recipient selection, loot-instance creation, or loot notify emission. Missing
and empty `Creature2` sources both return the existing false/no-drop boundary.
`GlobalLootManagerTests.DropLoot_WithUnavailableCreatureTableReturnsFalseWithoutLoot`
pins missing-table and empty-table behavior. Focused creature DropLoot
verification passed 2/2 and broader loot verification passed 93/93 from
alternate output directories. Standalone `ServerLootCanLoot`, exact
parent/source selection, bind-on-pickup confirmation policy, roll/master UI
parity, and loot aux producer timing remain evidence-gated.

Supplemental update: 2026-06-08 F-022 / audit F-026 global quest-manager
cache partial-table guard implemented; `GlobalQuestManager` now treats missing
`Quest2` tables like an empty quest-info cache, missing `Creature2` tables like
empty quest giver/receiver relation caches, and missing `CommunicatorMessages`
tables like empty communicator, quest-delivery, and quest-state trigger caches.
Null quest/communicator id arrays are also treated like empty arrays while
building those caches. Public getters preserve the existing missing-data
behavior: unknown quest and communicator ids return `null`, and unavailable
relations/triggers return empty sequences. `GlobalQuestManagerPartialTableTests`
pin missing-table, empty-table, table-backed quest relation, delivered
communicator, and state-trigger boundaries. Focused global quest-manager
verification passed 3/3 and broader quest/communicator-adjacent verification
passed 322/322 from alternate output directories. Exact objective guidance UI,
receiver/visibility content smoke, public-event producer timing, path target
semantics, and broader quest/path content parity remain evidence-gated.

Supplemental update: 2026-06-08 F-016/F-021 spell metadata cache
partial-table guard implemented; `GlobalSpellManager` now treats missing
`Spell4`, `Spell4Effects`, `Spell4Telegraph`, `TelegraphDamage`, and
`Spell4Thresholds` tables like empty spell caches, and treats a missing
`Spell4Base` table like an empty spell-base cache. `GetSpellBaseInfo()` now
uses the existing invalid-spell `ArgumentOutOfRangeException` boundary when the
`Spell4Base` table or row is unavailable. `SpellBaseInfo.GetSpellInfo()` now
returns `null` when no `Spell4` tier data exists for a known base spell or when
the requested tier is outside the materialized cache. `SpellBaseInfo` and
`SpellInfo` now resolve secondary spell metadata tables through nullable
lookups, preserving the existing missing-row behavior while avoiding
partial-table startup crashes. `GlobalSpellManagerPartialTableTests` pin
missing-table, empty-table, table-backed cache ordering, missing dependency-table
materialization, and sparse/out-of-range tier lookup boundaries. Focused
spell-manager verification passed 8/8 and broader spell/action-set/pet
verification passed 597/597 from alternate output directories. Spell aux
producer timing, proc tails, immunity/effect evidence, wrapper lifecycle,
current/action-set update transactions, and retail spell semantics remain
evidence-gated.

Supplemental update: 2026-06-08 F-026 / audit F-033 item-manager
item/item-slot partial-table guard implemented; `ItemManager` now treats a
missing `Item` table like an empty static-item cache and a missing `ItemSlot`
table like an empty equipped-slot index. `GetItemInfo()` continues to return
null for unavailable item rows, and `GetEquippedBagIndexes()` continues to
return an empty sequence for unavailable slot rows. `ItemManagerPartialTableTests`
pin missing-table, empty-table, and table-backed equipped-slot indexing
boundaries. Focused item-manager verification passed 5/5 and broader
item/costume/marketplace-adjacent verification passed 117/117 from alternate
output directories. Item/error aux producers, exact visual/equip update timing,
remaining item eligibility precision, and deeper costume/unlock lifecycle parity
remain evidence-gated.

Supplemental update: 2026-06-08 F-026 / audit F-033 item display-source
partial-table guard implemented; `AssetManager` now treats a missing
`ItemDisplaySourceEntry` table like an empty display-source cache, and
`ItemInfo.GetDisplayId()` treats unavailable display-source rows like an empty
candidate set before returning the existing zero-display fallback. Table-backed
single-row and power-level fallback visual selection remain unchanged.
`ItemInfoDisplaySourceTests` pin missing-table, empty-table, single-row, and
multi-row fallback boundaries. Focused item-display-source verification passed
6/6 and broader item/costume/marketplace display-adjacent verification passed
104/104 from alternate output directories. Item/error aux producers, exact
visual update timing, remaining item eligibility precision, and deeper costume
or unlock lifecycle parity remain evidence-gated.

Supplemental update: 2026-06-08 F-022 / audit F-026 quest target-group
cache partial-table guard implemented; `AssetManager` now treats a missing
`TargetGroup` table like an empty creature-target-group cache and like missing
target-group rows during quest-objective target expansion, and treats a missing
`QuestObjective` table like an empty quest-objective target cache.
`TargetGroupCriteriaEvaluator` now preserves the existing missing nested-row
pass-through behavior when recursive target-group criteria are evaluated without
the `TargetGroup` table. `AssetManagerTargetGroupTests` and
`TargetGroupCriteriaEvaluatorTests` pin missing-table, empty-table, and
table-backed cache/recursive boundaries. Focused target-group verification
passed 49/49 and broader quest/target-group-adjacent verification passed
371/371 from alternate output directories. Exact objective guidance UI,
receiver/visibility content smoke, public-event producer timing, path target
semantics, and broader quest/path content parity remain evidence-gated.

Supplemental update: 2026-06-08 F-007 / audit F-009 reward-property
modifier-source partial-table guard implemented; `AssetManager` now treats a
missing `RewardPropertyPremiumModifier` table like an empty premium modifier
source while preserving table-backed Hybrid filtering and tier fall-through
behavior. `AssetManagerRewardPropertyTests` pin missing-table, empty-table, and
table-backed modifier grouping boundaries. Focused reward-property verification
passed 8/8 and broader Reward verification passed 172/172 from alternate output
directories. Reward-rotation claim item/currency/property delivery, `0x07CD`
apply/flag/throttle semantics, exact schedule selection, and reward/content
context producer semantics remain evidence-gated.

Supplemental update: 2026-06-08 F-035 / audit F-042 achievement
checklist metadata partial-table guard implemented; `AchievementInfo` now treats
a missing `AchievementChecklist` table like an empty checklist source when
materializing achievement metadata. Missing/empty checklist tables no longer
null-ref during achievement startup/materialization, and table-backed rows still
filter by `AchievementId`. `AchievementInfoTests` pin missing-table,
empty-table, and table-backed filtering boundaries. Focused achievement metadata
verification passed 13/13 and broader achievement verification passed 29/29
from alternate output directories. Steam payload grammar, achievement-id
correlation, data-only/unowned achievement families, exact realm-first UI/timing,
and broader achievement UI edge cases remain evidence-gated.

Supplemental update: 2026-06-08 F-004 housing residence visual
partial-table guard implemented; `Residence` visual setters now treat missing
`HousingWallpaperInfo` and `HousingDecorInfo` tables like missing rows,
preserving the existing `ArgumentOutOfRangeException` before visual mutation
or dirty-state changes. Persisted residence decor load now treats unavailable
wallpaper/decor static tables like missing decor rows and preserves
`DatabaseDataException` instead of null-refing. `ResidenceTests` pin
missing-table, empty-table, table-backed visual setter, and persisted decor
load boundaries. Focused residence verification passed 15/15 and broader
residence/interior-wallpaper/vendor-list/decor item-use verification passed
27/27 from alternate output directories. Housing edit-mode ack/broadcast,
neighborhood list producer fields, decor ownership/unlock/refund precision,
residence visual UI timing, and broader community/session semantics remain
evidence-gated.

Supplemental update: 2026-06-08 F-006/F-026 entitlement-manager
partial-table guard implemented; account and character entitlement persisted
loads now treat a missing `Entitlement` table like missing entitlement rows and
throw the existing `DatabaseDataException` instead of null-refing. Shared
entitlement updates now resolve `Entitlement` through a nullable lookup, so
missing tables and empty tables reach the existing invalid entitlement
`ArgumentException` before mutation or entitlement packet emission.
`EntitlementManagerTests` pin account persisted-load, character persisted-load,
and direct update missing/empty-table boundaries. Focused entitlement
verification passed 4/4, entitlement-adjacent verification passed 20/20, and
broader account verification passed 140/140 from the alternate output
directory.

Supplemental update: 2026-06-08 F-006 account-currency
partial-table guard implemented; `AccountCurrency` load/materialization and
`AccountCurrencyManager` creation now treat a missing `AccountCurrencyType`
table like missing account-currency rows. Persisted balances still load and
serialize for wallet readback, while new balance creation for add/subtract
reaches the existing invalid static-currency exception before mutation or wallet
packet emission. `AccountCurrencyManagerTests` pin persisted readback plus
missing-table and empty-table creation boundaries. Focused account-currency
verification passed 4/4, loot-bag/account-currency verification passed 16/16,
and broader account verification passed 136/136 from the alternate output
directory.

Supplemental update: 2026-06-08 F-006 account-item entitlement-table
partial-table guard implemented; entitlement-backed account-item grant planning
now treats a missing `Entitlement` table like missing entitlement rows,
returning the existing `InvalidAccountItem` result before entitlement, currency,
delete, or cooldown side effects. `AccountItemHandlerTests` pin both immediate
account entitlement and mixed grant-plan missing-table boundaries. Focused
account-item verification passed 19/19 and broader account-inventory
verification passed 100/100 from the alternate output directory.

Supplemental update: 2026-06-08 F-006 account-item table
partial-table guard implemented; `AccountInventoryManager.CanAddItem()` and
`AccountInventoryItem` construction now treat a missing `AccountItem` table like
missing account-item rows, returning false for grant eligibility and preserving
the existing invalid account-item exception for persisted/create item materialization.
`AccountItemHandlerTests` pin both request-path and persisted-load boundaries.
Focused account-item verification passed 17/17 and broader account-inventory
verification passed 98/98 from the alternate output directory.

Supplemental update: 2026-06-08 F-006 account-item cooldown-group
partial-table guard implemented; `AccountInventoryManager` now treats a missing
`AccountItemCooldownGroup` table like an empty configured cooldown-group list
while still preserving and emitting persisted account cooldown rows.
`AccountItemCooldownTests` pin missing-table persisted cooldown readback.
Focused cooldown verification passed 8/8 and broader account-inventory
verification passed 96/96 from the alternate output directory.

Supplemental update: 2026-06-08 F-006 daily-login reward-table
partial-table guard implemented; `DailyLoginRewardManager` now treats a missing
`DailyLoginReward` table like an empty reward schedule for login-update refresh
and claim evaluation, preserving packet emission with zero available rewards and
returning the existing no-reward result before inventory checks or item grants.
`DailyLoginRewardManagerTests` pin both missing-table update and claim
boundaries. Focused daily-login verification passed 7/7 and broader
account-inventory verification passed 95/95 from the alternate output directory.

Supplemental update: 2026-06-08 F-024 map-instance pending-removal
formula-table guard implemented; `MapInstance.EnqueuePendingRemoval()` now
treats a missing `GameFormula` table like the already-documented missing row
case for formula `1123`, preserving the 30-second client-default pending
world-removal timer and still sending `ServerPendingWorldRemoval`.
`MapInstancePendingRemovalTests` pin both the missing-table fallback and a
table-backed override. Focused verification passed 2/2 and the broader map
bucket passed 459/459 from the alternate output directory.

Supplemental update: 2026-06-08 F-007 / audit F-009 reward-property
partial-table guard implemented; `RewardPropertyManager` now skips premium
modifier rows when `RewardProperty` or entitlement static data is unavailable,
and spell reward-property modifier resolution now treats a missing
`RewardProperty` table like an unknown reward property. Focused
`RewardPropertyManagerTests` passed 5/5 and the broader reward/spell bucket
passed 375/375 from the alternate output directory.

Supplemental update: 2026-06-08 F-014 / audit F-019 generated loot
reward-table guard implemented; `GlobalLootManager` generated-loot validation
now treats missing `AccountCurrencyType`, `AccountItem`, and `VirtualItem`
static tables like missing reward rows, returning the existing
`invalid-loot-item` preflight result before loot-bag source consumption or
generated reward delivery. Focused GlobalLootManager / loot-bag verification
passed 32/32 and broader loot verification passed 91/91 from the alternate
output directory.

Supplemental update: 2026-06-08 F-005 / audit F-005 marketplace auction
search selector-table guard implemented; `GlobalMarketplaceManager` now resolves
auction search family/category/type selectors through nullable
`Item2Family`/`Item2Category`/`Item2Type` lookups, so unavailable selector
static tables follow the existing invalid-packet boundary instead of crashing
before search. Focused marketplace auction verification passed 54/54 and the
marketplace / mail bucket passed 89/89 from the alternate output directory.

Supplemental update: 2026-06-08 F-022 / audit F-027 path reward scanbot
profile guard implemented; `PathManager.GrantPathReward()` now resolves
`PathReward.PathScientistScanBotProfileId` through a nullable
`PathScientistScanBotProfile` lookup before delegating to pet customisation, so
unavailable scanbot-profile static data skips only the scanbot reward portion
while supported sibling rewards still grant. Focused path manager verification
passed 67/67, the path / title / spell / pet bucket passed 373/373, and the
broader path / generic-unlock / pet / title / spell collection bucket passed
410/410 from the alternate output directory.

Supplemental update: 2026-06-08 F-022 / audit F-027 path reward Spell4
table guard implemented; `PathManager.GrantPathReward()` now resolves
`PathReward.Spell4Id` through a nullable `Spell4` table lookup, so missing
`Spell4` tables behave like missing spell rows and skip only the spell reward
portion while still granting supported sibling rewards. Focused path manager
verification passed 66/66, the path / title / spell collection bucket passed
81/81, and the broader path / generic-unlock / pet / title / spell collection
bucket passed 409/409 from the alternate output directory.

Supplemental update: 2026-06-08 F-022 / audit F-027 path reward title
partial-table guard implemented; `PathManager.GrantPathReward()` now resolves
`PathReward.CharacterTitleId` through a nullable `CharacterTitle` lookup before
delegating to `TitleManager`, so a reward row with unavailable title static data
skips only the title portion and still grants supported sibling rewards.
Focused path manager verification passed 65/65, the path / title / spell
collection bucket passed 80/80, and the broader path / generic-unlock / pet /
title / spell collection bucket passed 408/408 from the alternate output
directory.

Supplemental update: 2026-06-08 F-026 title manager persisted-title
partial-table guard implemented; `TitleManager` now skips persisted
`CharacterTitle` rows whose static data is unavailable, clears an active title
that is no longer owned after partial-load filtering, resolves `AddTitle` and
`RevokeTitle` through nullable `CharacterTitle` lookups before mutation, and
treats missing `CharacterTitle` tables as an empty source for `AddAllTitles`.
Focused title manager verification passed 6/6, the title / spell collection
bucket passed 33/33, and the broader generic-unlock / pet / title / spell
collection bucket passed 343/343 from the alternate output directory.

Supplemental update: 2026-06-08 F-026 pet customisation persisted-flair
partial-table guard implemented; `PetCustomisationManager` now skips persisted
pet-flair rows whose `PetFlair` static data is unavailable, saved
customisation slots with unavailable flair rows resolve to empty slots, and
zero-flair slot clears no longer require `PetFlair` table availability. Focused
pet customisation manager verification passed 3/3 and broader pet verification
passed 293/293 from the alternate output directory.

Supplemental update: 2026-06-08 F-026 title spell effect partial-table guard
implemented; `HandleEffectTitleGrant` and `HandleEffectTitleRevoke` now resolve
`CharacterTitle` through nullable table lookups, so a missing title table
follows the existing unknown-title diagnostic path before `HasTitle`,
`AddTitle`, or `RevokeTitle`. Focused spell collection verification passed 9/9
and broader spell / generic-unlock / pet / title verification passed 334/334
from the alternate output directory.

Supplemental update: 2026-06-08 F-026 pet-flair spell effect partial-table
guard implemented; `HandleEffectUnlockPetFlair` now resolves `PetFlair`
through a nullable table lookup, so a missing `PetFlair` table follows the
existing unknown-pet-flair diagnostic path before `HasFlair` or `UnlockFlair`.
Focused spell collection verification passed 5/5 and broader spell /
generic-unlock / pet verification passed 312/312 from the alternate output
directory.

Supplemental update: 2026-06-08 F-026 collection-spell Spell4
partial-table guard implemented; `TryLearnCollectionSpell` now resolves
`Spell4` through a nullable table lookup, so mount and vanity-pet collection
spell effects treat a missing `Spell4` table like an unknown spell before
`AddSpell` or unlock-packet emission. Focused spell collection verification
passed 3/3 and broader spell / generic-unlock verification passed 27/27 from
the alternate output directory.

Supplemental update: 2026-06-08 F-026 learn-dye-color generic-unlock
partial-table guard implemented; `HandleEffectLearnDyeColor` now resolves
`GenericUnlockEntry` through a nullable table lookup before the existing
unknown-generic-unlock diagnostic and invalid unlock path, so partial table
loads no longer throw before `GenericUnlockManager.Unlock()` can return the
mapped invalid result. Focused spell collection verification passed 1/1 and
broader spell / generic-unlock verification passed 25/25 from the alternate
output directory.

Supplemental update: 2026-06-08 F-026 account-item generic-unlock
partial-table guard implemented; account inventory generic-unlock claim grants
now resolve `GenericUnlockSet` and `GenericUnlockEntry` through nullable table
lookups, returning the existing `InvalidAccountItem` result before grant
application or account-item deletion when either static table is unavailable.
Focused account-item verification passed 15/15 and broader account-item /
generic-unlock verification passed 67/67 from the alternate output directory.

Supplemental update: 2026-06-08 F-026 persisted generic-unlock
partial-table guard implemented; account generic-unlock manager startup now
resolves persisted `AccountGenericUnlock` rows through nullable
`GenericUnlockEntry` lookups, skips missing table/row entries from transient
runtime readback without deleting database state, and loads table-backed rows as
non-dirty. Focused manager verification passed 6/6 and broader generic-unlock
verification passed 22/22 from the alternate output directory.

Supplemental update: 2026-06-08 F-026 normal item-use partial-table guards
implemented; activated item-use now treats missing `ItemSpecial` tables like
missing rows before spell cast or item consume, and currency-treasure item-use
now treats missing `CurrencyType` tables like missing rows before item consume
or currency grant. Focused item-use verification passed 9/9 and broader
item-use verification passed 20/20 from the alternate output directory.

Supplemental update: 2026-06-08 F-008 tradeskill request partial-table guards
implemented; tradeskill learn/drop, pick-talent, and reset validation now treat
missing `Tradeskill`, `TradeskillBonus`, and `TradeskillTalentTier` tables like
missing rows through the existing invalid-packet boundary. Focused tradeskill
request verification passed 5/5 and broader crafting verification passed 90/90
from the alternate output directory.

Supplemental update: 2026-06-08 F-008 additive modifier materialization
partial-table guards implemented; queued additive/catalyst modifiers now treat
missing `Item`, `TradeskillAdditive`, and `TradeskillCatalyst` tables like
missing rows during fixed-recipe materialization, returning the existing
invalid-modifier reasons before inventory checks or item debits. Focused
additive/rune verification passed 63/63 and broader crafting verification passed
85/85 from the alternate output directory.

Supplemental update: 2026-06-08 F-021 stance/action-set/AMP/non-spell
partial-table guards expanded; direct and persisted `ActionSet.AddAmp` paths
now treat missing `EldanAugmentation` tables like missing AMP rows, throwing
the existing invalid-AMP exception before owner-save or AMP-list mutation.
Non-spell bag-item shortcuts still reject a missing `Item` table before
action-set save while known `Item2` rows commit. Focused AMP/action-set
verification passed 28/28 and broader Spell verification passed 221/221 from
alternate output directories; earlier stance/action-set guard coverage passed
2/2, 21/21, and 633/633.

Supplemental update: 2026-06-08 F-008/F-011 rune/additive partial-table guards
implemented; rune install and additive/catalyst validation now treat missing
`Item`, `Item2Category`, `TradeskillAdditive`, `TradeskillCatalyst`, and
`ItemSpecial` tables like missing rows through existing invalid-result paths.
Focused rune verification passed 57/57 and broader crafting verification passed
82/82 from the alternate output directory.

Maintained from `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, focused trackers
(`MATCHING_IMPLEMENTATION_STATUS.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`), and
code audits. Update after every implementation pass so it stays the single source
of truth for feature-area completion.

---

## Quick Stats

| Metric | Value |
|--------|-------|
| Total feature areas | 36 (`F-001`..`F-036`; matrix rows `F-016`..`F-020` are five spell-runtime rows) |
| Implementation-complete | 13 (sections marked **COMPLETE** below; not a retail-parity claim) |
| Partial (real handlers/state exist; retail parity incomplete) | 21 (`F-016`..`F-020` count as five spell-runtime rows; includes F-031 retail-weight parity blocked) |
| Blocked / diagnostic / structural-only | 2 (`F-002` diagnostic, `F-003` structurally closed; `F-001` is partial with token-crypto blocked) |
| Consolidated runtime gaps (emit/producer proof) | 5 rows in **Remaining Blocked Items** |
| Related trackers | `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `MATCHING_IMPLEMENTATION_STATUS.md`, `ENTITY_AUX_DECODE_ROADMAP.md`, `BLOCKER_EVIDENCE_PLAN.md` |
| Placeholder inventory (2026-06-05) | 12 `Client0xNNNN`, 1 `Server0xNNNN` (`Server0x0015`), 16 client diagnostic log-only surfaces, 24 `PrerequisiteType.UnknownNNN`, and 2 quest objective unknowns (`Unknown27`, `Unknown29`); the audit-prompt ghost literal names are absent; pass 158 keeps `Client0x07E3` registration-only at `1400a8282` through `ClientUInt32_ReadPayload` (`14007d000`) / `ClientTradeskillResetTalents_WritePayload` (`14007d010`), with MCP still returning `Transport closed`, live-plugin helper xrefs shared, selected send-helper scans finding no `0x07E3` sender, and message-name slot `140c27018` unxrefed; pass 155 keeps `Client0x062A`/`Client0x0634` registration-only at `1400a82b6`/`1400a872c`; pass 154 keeps `Client0x00C8` registration-only; the 2026-06-09 cached recheck keeps `Client0x0701` registration/writer-only at `selected_decompiled.c:13899` / `1400a69d0` with no selected sender or `140dd0a74` owner; pass 152 keeps `Client0x0928` shared `uint32+5-bit` registration-only |
| Diagnostic `Client0x00C8` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `Network_RegisterServerOpcode_0351` (`14006c290`) still registers decimal `200` (`0x00C8`) size `4` with `LAB_14008a140` / `ClientMatchType_ReadPayload` and `LAB_14008a150` / `ClientMatchType_WritePayload`, the same shared helper pair used by `0x05B5` `ClientMatchingQueueLeave` and `0x05B6` `ClientMatchingQueueLeaveAsGroup`. The reader advances 5 bits and the writer masks/writes one `0x1f` value, proving only the `MatchType` wire shape. Selected xrefs for `14008a150` are label-only, selected call edges are writer-local, and selected send-helper scans found no `0x00C8` sender or consumer. Challenge-choice positive controls send `0x00C5` through `ClientChallengeChoice_SendActivate` (`140710c10`), `ClientChallengeChoice_SendAbandon` (`140710d60`), `ClientChallengeChoice_SendAcceptShared` (`140711ea0`), and `ClientChallengeChoice_SendDeclineShared` (`140711f10`); matching queue dispatch `14076c830` sends `0x05EF`/`0x05F3`/`0x05F8`/`0x05F9`, not `0x00C8`. Current source keeps `Client0x00C8.MatchType` neutral and log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x00C8` capture before any semantic rename or runtime mutation. |
| Diagnostic `Client0x012D` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `Network_RegisterServerOpcode_0351` (`14006c290`) still registers `0x012D` size `8` with `LAB_14007ae30` and shared `ClientSuggest_WritePayload` (`14007ae80`). The writer only calls `NetworkBitWriter_WriteWideString` (`140336040`), proving one wide-string field and no request semantics. The same writer is reused by support/account/status siblings including `0x0833`, `0x0233`, `0x07C6`, `0x063E`, `0x03E0`, `0x03AC`, and `0x039B`; selected xrefs for `14007ae80` are label-only and selected call edges only show the wide-string write. Positive controls stay separate: `Support_SendClientSuggest` (`14063b9f0`) sends `0x0833`, `AccountItem_SendClientClaimPendingItemGroup` (`140006ba0`) sends `0x0233`, `AccountItem_SendClientReturnPendingItemGroup` (`140006c50`) sends `0x07C6`, and `ClientRealmTransfer_SendFromLuaDispatch` (`140027d80`) sends separate `0x0142` plus `0x0244` follow-up traffic. Exact `0x012D` cache/export scans found only the registration row plus unrelated struct-offset/switch-helper hits, not a send-helper owner. Current source keeps `Client0x012D.Text` neutral and log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x012D` capture before any semantic rename or runtime mutation. |
| Diagnostic `Client0x063E` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x063E` size `8` with `LAB_14007ae30`, shared `ClientSuggest_WritePayload` (`14007ae80`), and `LAB_140080d20`. The writer only calls `NetworkBitWriter_WriteWideString` (`140336040`), proving one wide-string field; `LAB_140080d20` has no selected standalone body/label and is reused by `0x0724`, `0x058C`, and `0x00CE`, while nearby `140080d30` and `140080d70` are different server readers. Exact `0x063E` cache/export scans found only the registration row and shared-slot references, not an opcode-specific sender or consumer. Positive controls stay separate: `Support_SendClientSuggest` (`14063b9f0`) sends `0x0833`, account-item claim/return (`140006ba0`/`140006c50`) send `0x0233`/`0x07C6`, `ClientSupportTicket_WritePayload` (`14007cc20`) belongs to `0x06D1`, `Marketplace_RequestCommodityInfo_WritePayload` (`140085420`) belongs to `0x03E6`, and `Marketplace_AuctionsByFilterRequest_WritePayload` (`14009a7d0`) belongs to `0x07DC`. Current source keeps `Client0x063E.Text` neutral and log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x063E` capture before any semantic rename or runtime mutation. |
| Diagnostic `Client0x0550` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x0550` size `4` at `1400a824e` with `ClientUInt32_ReadPayload` (`14007d000`), shared `ClientTradeskillResetTalents_WritePayload` (`14007d010`), and `ServerUInt32_ReadPayload` (`14007ab50`). The read helper advances 32 bits and the writer serialises one raw `uint32`; selected call edges for `14007d010` are writer-local plus `ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), not a `0x0550` owner. Exact selected scans found `0x0550`/`0x550` only at the registration row and writer-label comments, and no `Network_SendOpcodePayloadHelper(...0x0550...)` path; the static message-name rail has no selected `140c247e0` evidence. Positive controls stay separate: ICComm writers `1400877a0`/`1400875e0` belong to `0x0546`/`0x054B`, `ServerSpellList_ReadPayload` (`140096060`) belongs to `0x0551`, `MatchingReplacement_SendStartLookingForReplacements` (`14076aa30`) sends `0x05D5`, `TargetSelection_SendClientMovementControlAck` (`14057a630`) sends `0x0635`, `Tradeskill_SendClientTradeskillResetTalents` (`14059acb0`) sends `0x0858`, `Trade_SendClientP2PTradingInitiateTrade` (`1403a6a40`) sends `0x0192`, and ability-book/combat-log evidence is separate. Current source keeps `Client0x0550.Value` neutral and log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x0550` capture before any semantic rename or runtime mutation. |
| Diagnostic `Client0x062A`/`Client0x0634` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x062A` size `4` at `1400a82b6` and `0x0634` size `4` at `1400a872c` with `ClientUInt32_ReadPayload` (`14007d000`), shared `ClientTradeskillResetTalents_WritePayload` (`14007d010`), and `ServerUInt32_ReadPayload` (`14007ab50`). `14007d000` advances 32 bits, `14007d010` writes one raw `uint32`, and `14007ab50` reads one raw `uint32`; selected xrefs are label-only and selected call edges for `14007d010` remain writer-local plus `ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`). Exact selected scans found `0x062A`/`0x0634` only at registration/comment evidence, found no `Network_SendOpcodePayloadHelper(...0x062A/0x0634...)`, and found no selected `140c25488`/`140c25528` static name-rail evidence. Positive controls stay separate: `MatchingReplacement_SendStartLookingForReplacements` (`14076aa30`) sends `0x05D5`, and `TargetSelection_SendClientMovementControlAck` (`14057a630`) sends `0x0635` plus possible `0x063A` follow-up. Current source keeps both `Value` fields neutral and handlers log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x062A`/`0x0634` capture before any semantic rename or queue/movement runtime mutation. |
| Diagnostic `Client0x0701` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient for the writer and registration evidence. `Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x0701` size `8` at the registration row (`selected_decompiled.c:13899`; setup literal `140079e05`) with `LAB_1400a69c0` and `ClientUInt2UInt32_WritePayload` (`1400a69d0`). The cached writer serialises one 2-bit field followed by one `uint32`; no standalone `1400a69c0` fragment is present in the selected cache. Selected xrefs for `1400a69d0` are label-only, selected call edges are writer-local plus generic bitstream helper calls, selected exact scans found `0x0701` only at registration/comment evidence, no selected `Network_SendOpcodePayloadHelper(...0x0701...)`, and no selected `140dd0a74` pointer-owner evidence. Positive controls stay separate: challenge-choice senders (`140710c10`, `140710d60`, `140711ea0`, `140711f10`) send `0x00C5`, `Matching_QueueDispatchFromUi` (`14076c830`) sends `0x05EF`/`0x05F3`/`0x05F8`/`0x05F9`, and `MatchingReplacement_SendStartLookingForReplacements` (`14076aa30`) sends `0x05D5`. Current source keeps `Client0x0701.LeadingBits` / `TrailingValue` neutral and log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x0701` capture before any semantic rename or runtime mutation. |
| Diagnostic `Client0x07E3` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x07E3` size `4` at `1400a8282` with `ClientUInt32_ReadPayload` (`14007d000`), shared `ClientTradeskillResetTalents_WritePayload` (`14007d010`), and `ServerUInt32_ReadPayload` (`14007ab50`). The helpers prove only one raw `uint32` in each direction; selected xrefs are label-only and selected call edges for `14007d010` remain writer-local plus `ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), not a `0x07E3` owner. Exact selected scans found `0x07E3` only at the registration row/comment evidence, found no `Network_SendOpcodePayloadHelper(...0x07E3...)`, and found no selected `140c27018` static name-rail evidence. Positive controls stay separate: `MatchingReplacement_SendStartLookingForReplacements` (`14076aa30`) sends `0x05D5`, `TargetSelection_SendClientMovementControlAck` (`14057a630`) sends `0x0635` plus possible `0x063A` follow-up, `ClientPlayerMovementSpeedUpdate_SendAndDispatch` (`1404dafb0`) sends `0x063B`, and `Tradeskill_SendClientTradeskillResetTalents` (`14059acb0`) sends `0x0858`. Current source keeps `Client0x07E3.Value` neutral and log-only; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x07E3` capture before any semantic rename or runtime mutation. |
| Diagnostic `Client0x0928` recheck (2026-06-09) | Cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) still registers `0x0928` size `8` at row `1400a8524` with read-advance `LAB_140089240`, writer `ClientUInt32UInt5_WritePayload` (`1400898b0`), and reader/apply helper `ServerUInt32UInt5_ReadPayload` (`14008ce80`). Those helpers prove only `uint32 + 5-bit` wire shape. The positive pet controls stay separate: `0x068E` registers the same writer in `Network_RegisterServerOpcode_0351` (`14006c290`) and has real sender `Pet_SetStance_SendClientPetSetStance` (`14050a270`), while `0x068F` registers the same reader and applies through `Pet_ApplyStanceChangedPayload` (`1403c0a80`) / `Pet_GetStance_ReadCachedStance` (`14050a130`). Selected xrefs for `1400898b0` are label-only, selected call edges are writer-local except positive pet send-helper evidence, and selected scans find `0x0928` only in registration. Current source keeps `Client0x0928.LeadingValue` / `TrailingBits` neutral, log-only, and packet-shape/test-pinned; require an opcode-specific sender, consumer, callback/table owner, indirect send rail, or accepted `0x0928` capture before any semantic rename or runtime mutation. |
| Server placeholder recheck (2026-06-09) | `Server0x0015` remains mapped-only/test-pinned: cached-export/source recheck found no running Ghidra MCP instance, `140081f00` remains a shared 5-bit plus `uint32` reader registered for `0x0015` and `0x0628` and called as a Fortune money-reward row helper, while only `0x0628` has the positive `MatchingManager_ApplyMatchingAverageWaitTimeUpdated` (`1405c0e00`) apply path. It still needs an opcode-specific apply/producer path, post-read consumer, or live payload before semantic rename or production emit. |
| Prerequisite/objective placeholder recheck (2026-06-07) | The inventory still has exactly 24 `PrerequisiteType.UnknownNNN` entries and 2 quest objective placeholders. `PrerequisiteTypeNamingTests` plus new `QuestObjectiveTypeNamingTests` pin these as neutral until handler/table/objective-data ownership is proven; focused naming tests passed 119/119. |
| Build | 0 errors, 0 warnings |
| Tests | 2817 passed, 0 failed, 0 skipped |
| Handlers surveyed | 200+ |
| Production TODO/FIXME/NotImplemented | 0 |
| **LaughingWS blocker tracking (2026-05-27)** | Evidence harness added; broad new-zone/sandbox/arkship/Rider's Reef rows remain blocked or rejected where proof is missing; Dungeon Chase hidden SMC item `86919` rejected for current seeds because the world store model and type-`0` storefront transport cannot represent it |
| **Dust Stalker Q4516 review (2026-05-27)** | Boss Xagg and bridge-control data remain WIP/GUESSED; self-destruct and exit-panel behavior are blocked pending a harness smoke bundle rather than inferred from source-only SQL |
| **Arcterra/Palaver source-only review (2026-05-27)** | Arcterra Caretaker, Coldblood portal, and Palaver Ish'amel rows stay WIP/GUESSED pending placement, portal target, interaction, and quest smoke proof |
| **Crafting review (2026-06-04)** | Aux emit remains blocked; fixed-recipe success, additive modifier state, and durable rune bridge are evidence-backed |
| **Ghidra decomp evidence (2026-05-23 pass 3)** | Consumer dispatch found at WorldSocket_ProcessServerMessage; entity-create aux verified emitted by EntityCreateAuxiliaryPacketBuilder; 44,991 xrefs created |
| **Addon corpus audit (2026-05-23)** | 876 addons scanned; map tracked/threat/crafting/housing strings confirmed, but only threat list is already implementation-backed |

### Status legend (how to read **COMPLETE** vs **PARTIAL**)

| Label | Meaning |
| --- | --- |
| **COMPLETE** | Client handlers and a working server/runtime path exist with focused tests; this is not a retail-parity claim, and known retail gaps may still be listed in the section or in **Remaining Blocked Items**. |
| **PARTIAL** | Substantial behavior exists, but retail parity, producer semantics, or persistence edges remain open. |
| **BLOCKED** | Evidence gate prevents widening behavior (e.g. F-001 STS crypto). |
| **DIAGNOSTIC** | Safe parse/log only (F-002). |
| **STRUCTURALLY CLOSED** | All server opcodes have models (F-003); one numeric `Server0x0015` contract and feature-owned emitters remain evidence-gated. |

`Decomp/Analysis/GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` intentionally uses
**Partial** for implementation-complete rows when retail parity is still
incomplete. Use this file for the handler/runtime completion snapshot, and the
gameplay/matrix trackers for retail-parity gaps.

---

## Code Review Backlog - Fixes Applied 2026-05-23

From `Decomp/Analysis/CODE_REVIEW_BACKLOG_2026-05-23.md` (43 items).

### P0 - Fixed / Verified

| # | Item | Fix |
|---|------|-----|
| 1 | Loot bag consumed before delivery | Delivery is preflighted before consumption; successful use consumes the bag before granting loot, avoiding both destroyed rewards and duplicate grants. |
| 2 | ServerSupportSurveyList 5th uint32 | **Verified correct**: Ghidra decomp (0x1400a4260) confirms 5 uint32s where 5th is row count. Wire format matches. |
| 3 | Q3487Shellshock GrantAchievement throw | Added `HasCompletedAchievement` guard before `GrantAchievement(1296)`. |

### P1 - Fixed / Partially Closed

| # | Item | Fix |
|---|------|-----|
| 4 | Group loot solo fallback | Group context now created when sameMapMembers > 1 even if all out of range. |
| 5 | Killer force-added out of range | Removed force-add block; eligibility logic handles it naturally now. |
| 6 | AssignMasterLoot offline winner | Returns false + warning before winner resolution/broadcast when assignee is offline. |
| 7 | FinaliseRoll offline winner | Logs and keeps the item assigned for deferred delivery if the winner reconnects before corpse expiry. |
| 11 | DeliverAllLoot partial success | Mixed delivery now reports incomplete success, preserves delivered rows, keeps failed rows retryable, and suppresses complete generated granted-notify. |
| 18 | Duel "leash" comment | Corrected to "inter-duelist distance limit" (matches code checking 120m between players). |
| 19 | Cancel/timeout sets winner/loser | `ServerDuelResult` now sends 0/0 for Cancelled and DeclinedRequest reasons. |
| 20 | No disconnect hook | `OnPlayerDisconnect` added to `IDuelManager` + `DuelManager`; called from `WorldSession.OnDisconnect`. |
| 21 | MemberIndex never set | `ServerGroupMemberFlagsChanged` now sets `MemberIndex` from `GroupIndex`; opcode `0x0438` was corrected to the native `ServerGroupIdentityListAndUInt32Array` shape, with the provisional leading value still sourced from `GroupIndex` pending consumer proof. |
| 22 | Ready-check clear stale UI | `HasReadyCheckFlags` guard removed; `ServerGroupReadyCheckStatusUpdate` sent unconditionally. |
| 24 | Group flag fan-out packet allocation | `GroupMemberFlagsUpdatedHandler` now builds one immutable role/flag packet and one ready-check packet per update, then enqueues them to online members. |
| 25 | Shared group fan-out helper | Added `GroupMemberFanoutHelper` for online-member broadcast resolution across internal group handlers. |
| 26 | Housing edit mode stub | Now validates residence permission via `CanModifyResidence` and records per-player server edit state on the residence map; ack/broadcast packet semantics remain unmapped. |
| 27 | TargetPlayerIdentity ignored | Now resolved via `GetResidenceByOwner` + permission check, matching sibling handlers. |
| 28 | Cannon no quest 3487 gate | `OnActivateSuccess` now checks `GetQuestState(3487) == Accepted` before crediting objective. |
| 29 | Map spawn fallback missing | `NorthernWildsMapScript` now spawns cannon (11251) + ultrabot (12526) with DB fallback checks. |
| 30 | Q3667 objective 4770 unverified | Added comment confirming verification against Quest2.tbl. |
| 31 | Manual GrantNext() boilerplate | Q3479, Q3667, Q3886, Q3487 refactored to `FollowUpQuestScript<T>` base class. |
| 41 | PublishAsync missing FireAndForget | Added `.FireAndForgetAsync()` to `ClientGroupFlagsChangedHandler`. |
| 43 | TutorialMapInfo/Position names | Renamed to `NorthernWildsMapInfo`/`NorthernWildsMapPosition`. |

### P2 - Fixed / Documented

| # | Item | Fix |
|---|------|-----|
| 12 | DB null no-op silent divergence | Added `log.Warn` in `Initialise()` and `Persist()` when DB context is null. |
| 13 | Corrupt DB row crashes startup | Marketplace auction microchip-id parsing now logs corrupt format/overflow values and returns an empty array instead of throwing during startup load. |
| 14 | SearchAuctions materializes all | Restructured: filter, sort, page via Skip/Take, then CloneAuction only for current page. |
| 15 | O(n^2) commodity matching | Added per-item/per-side price indexes for commodity match candidate selection and immediate sell preflight. |
| 16 | Persist deadlock risk | Changed `.GetAwaiter().GetResult()` to `.ConfigureAwait(false).GetAwaiter().GetResult()`. |
| 17 | Null item skipped silently | Added `log.Debug` when skipping auctions with `model.Item == null`. |
| 32 | Field-level tests for 0x034F / 0x034C | Added `ServerSupportUInt32AndFlags_WritesValueFlagsAndPadding()` and explicit `ServerSupportUInt5AndSixUInt32` 27-bit padding consume coverage. |
| 33 | Test placement | Moved `ServerRealmAuxUInt32TripletList` coverage to dedicated `RealmAuxPacketShapeTests`. |
| 34 | Consolidate triplet row types | `ServerSpellUInt32TripletListRow` now inherits the shared `ServerUInt32Triplet` writer. |
| 35 | Consolidate counted triplet lists | Spell `0x080F`/`0x0810` and realm aux `0x05A1` packets now reuse `ServerUInt32TripletListPayload`. |
| 36 | Server0x08CC duplication | Inherits from `ServerUInt32WideStringPayload`, removing ~10 lines of duplicated code. |
| 37 | RowCount property confusing | Added XML doc: "for diagnostic / log use only." |
| 39 | lootInstances no lock | Added XML doc documenting world-thread-only mutation assumption. |
| 40 | NextLootId unsynchronized | `GlobalLootManager.NextLootId` now allocates atomically with `Interlocked.Increment`, preserves the high-bit loot-unit id range, and never returns zero. |
| 42 | Quest script xUnit tests | Created `QuestScriptConstructionTests.cs` with DI smoke test for FollowUpQuestScript<T>. |
| 9 | Delivered loot instance sweep | Fully delivered loot instances are now removed from the manager immediately after final collect/vacuum/roll/master resolution. |
| 8 | Active loot lookup scans | Owner-unit and looter indexes now back notify, runtime snapshot, collect/roll/master lookup, and vacuum paths. |

### P2 - Deferred

| # | Item | Reason |
|---|------|--------|
| 8 | Active loot tick walk | Timing-wheel/shard optimization needs profiling after owner/looter indexing and #9 immediate cleanup. |
| 10 | Duplicate aggregation logic | Refactor deferred. |
| 38 | Opcode/docs naming drift | Documentation polish deferred. |

## Ghidra Decomp Evidence (2026-05-23, 2400-function run)

### Crafting - Station Semantics / Discovery Diagnostics (F-008)
- **`Crafting_HandleServerCraftingFinish`** (0x1405e6690):
  - `CraftingDiscovery` field at `param_2[4]`. Value `3` = Success. All other values dispatch `CraftingDiscoveryHotCold` client event.
  - `param_2[5]` = discovery value sent with the event.
  - On craft completion, discovery state resets: `DiscoveryVectorMultiplier=1.0f`, `DiscoveryRadiusMultiplier=1.0f`, position/unknown fields zeroed.
  - NexusForever keeps complex-craft `CraftStats`, `ApSpSplitDelta`, and `ChargeCounts` diagnostic-only for server behavior. Focused coverage now pins that non-zero complex-craft stat/charge payloads still follow fixed-recipe completion and emit `CraftingDiscovery.Success`/`CraftingDirection.None`.
  - Addon scan confirmed `CraftingLib.CodeEnumCraftingDiscoveryHotCold` usage in Athena, and `Lua_Crafting_AddCoordinateDiscoveryInfo` maps UI distance-band fields, but neither proves authoritative server-side coordinate packing, hot/cold emit timing, or discovery-unlock mutation.

- **`Crafting_SendClientComplexOrSimpleCraft`** (0x140399780):
  - Resolves `CraftingStationUnitId` and schematic, validates via `SpellCast_ResolveTargetsAndValidate`.
  - Sends opcode 0x084F (complex) or 0x0850 (simple) depending on schematic flags.
  - `Crafting_GetStationServiceKeyForSchematic` (0x1405926a0) maps `TradeskillSchematic2` to numeric station service keys `0x4F` for `TradeSkillId=22`, `0x57` for tier-zero `Flags & 0x04`, and `0x2C` otherwise. `Crafting_FindStationUnitForServiceKey` (0x1403a0d20) resolves that service key through the current context tree and returns the station unit id, or 0 when no key exists. Native service-key names remain unmapped; server validation continues to use `Creature2.TradeSkillIdStation`, not service-key integers.
  - Server now validates forged non-zero `CraftingStationUnitId` values against the player's map and `Creature2.TradeSkillIdStation`, while preserving the observed client ability to send 0 from the simple/complex sender.

- **`Crafting_SendClientCraftingAdditive`** (0x14059b7c0):
  - Sends opcode 0x084A with station unit ID + additive/catalyst Item2 IDs.
  - Validates additive count and schematic match.
  - Native sender requires a non-zero station unit before emitting 0x084A; server additive handling now rejects zero, unknown, or non-station unit ids.
  - 2026-06-04 focused coverage pins the NF additive bridge as state-only: a valid station records additive/catalyst Item2 modifiers for the next fixed-recipe craft, `ClientCraftingAbandon` clears that state, and neither path emits blocked `ServerCraftingCurrentCraft`, `0x084B`, or `0x0855` packets.

- **`Lua_RegisterItemDataBindings`** (0x140413a20):
  - Registers `CodeEnumRuneType` through `Lua_RegisterCodeEnumValue` (0x1400eff50).
  - Native rune values are pinned as `Air=7`, `Water=8`, `Earth=9`, `Fire=10`, `Logic=11`, `Life=12`, `Fusion=13`, matching `RuneType`.
  - `Lua_GameItemData_GetRuneSlots` (0x14041b8d0) now maps through `ItemData_AddRuneSlotsLuaFields` (0x140673b80): live item data reads slot type bytes at `itemData+0x388`, converts compact values 1..7 through `ItemRuneSlotType_ToRuneType` (0x140514660), and reads installed rune Item2 ids at `itemData+0x518 + index*4`.
  - This closes the rune-type enum question and the durable rune item bridge: `item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`, `RandomGlyphData`/`Glyphs`, and migration `20260531224115_ItemMicrochipIdsAndRuneSlots` now preserve socket types and installed rune Item2 ids. A 2026-06-04 taxonomy cleanup rejects a distinct client microchip-install mutator: client install is mapped to `0x085B` (`RuneCrafting_SendClientRuneInstall`), while the separate `ServerItemMicrochips` (`0x056C`) / `Inventory_UpdateItemMicrochipsFromWire` patch path remains server-side and producer-blocked.
  - 2026-06-04 Ghidra MCP recheck: `ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload` (0x1400a3af0) reads `0x084B` as four `uint32` values, one `float`, then one `uint32`; `ServerUInt32AndTwoFloats_ReadPayload` (0x140081df0) reads `0x0855` as one `uint32` and two `float` values. `Crafting_HandleServerCraftingFinish` (0x1405e6690) and `Crafting_HandleServerCraftingCurrentCraft` (0x1405e6830) prove finish/current-craft consumers only, so aux enqueue intent remains blocked.
  - 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance and reconfirmed the selected `WildStar64.exe` fragments as reader/apply evidence only: `0x084B` / `0x0855` have field-shape readers, `ServerCraftingCurrentCraft` stores decoded state and dispatches `CraftingUpdateCurrent`, and `0x056C` is an item microchip patch path that applies wire data into item state and dispatches `ItemModified`. Current source still emits `ServerCraftingFinish` for fixed-recipe success while focused crafting tests guard that additive/complex-craft paths do not emit blocked `ServerCraftingCurrentCraft`, `0x084B`, or `0x0855` packets.

### Housing - Neighbor Lua Table (F-004)
- **`Housing_BuildNeighborLuaTable`** (0x1404b4e40):
  - Builds per-neighbor Lua row with fields: `nId`, `nClassId`, `nPathId`, `fLastOnline`, `strRealmName`, `strCharacterName`, `strWorldZone`, `nLevel`, `nFactionId`, `ePermissionNeighbor`.
  - Called when building neighbor Lua rows for `ServerHousingNeighbors` (0x0507) / `Housing_HandleNeighborUpdate` paths (`FUN_1406f9fe0`, `FUN_140735cc0`); **not** called from `Housing_HandleNeighborhoodList` (0x0506).
  - Addon scan confirmed NeighborNotes uses `ICCommLib.JoinChannel("NeighborShare")` for peer-to-peer share/search data, so that addon does not unblock the server neighborhood trigger path.
- **`ClientDB_RegisterHousingNeighborhoodInfo`** (WildStar64 `0x140205900`) is now durably labelled from the `DB\HousingNeighborhoodInfo.tbl` / `HousingNeighborhoodInfo` loader path. This proves the world client has the table metadata, but it still does **not** map table columns to `0x0506` row fields or prove a server send trigger.
- A 2026-06-04 packet-placeholder guard now keeps the `0x0501` row tail as
  `NeighborhoodWireUInt64_AfterRealmIds` / `NeighborhoodWireUInt32_0..2` and
  rejects `HousingNeighborhoodInfo.tbl` column-name synthesis (`BaseCost`,
  `MaxPopulation`, `HousingMapInfoIdPrimary`) until row backing is proven.
- 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance
  and reconfirmed the selected `WildStar64.exe` fragments as reader/apply/table
  evidence only: `0x0501` reads the single neighborhood row, `0x0506` reads a
  realm plus counted `0x30`-byte rows, `Housing_HandleNeighborhoodList` clears
  and rebuilds the client neighborhood cache before dispatching
  `HousingNeighborhoodRecieved`, and `ClientDB_RegisterHousingNeighborhoodInfo`
  only proves table-loader presence. Current source still has packet models,
  packet-shape tests, and residence-session negative emission guards, with no
  production `ServerHousingNeighborhoodEntry` / `ServerHousingNeighborhoodList`
  send site.

### Map Tracked Unit / Threat Addon Follow-up
- Map addon evidence (`GuardZoneMap`, `LUI_ZoneMap`, `RavenMap`) confirms Lua consumers for `MapTrackedUnitUpdate`, `MapTrackedUnitDisable`, and `GetMapTrackedUnitData(id)`, but the server opcodes are `0x0849`/`0x0848`; `0x0264` is the unrelated `ServerEntityCreateAuxScalarList`.
- Producer behavior remains blocked for map tracked units: tracked-unit id allocation, update cadence, disable lifetime, and `TrackingSlotId` selection are not proven. A 2026-06-04 Ghidra MCP recheck reconfirmed only the client cache update/remove plus Lua enumeration chain, and read-only `TrackingSlot` data has duplicate objective groups (`5010`, `5138`), rejecting objective-only slot selection. Focused source/test passes added a null-table guard, packet naming guard, and duplicate-objective selection guard for `TrackingSlotHelper`; it remains one-way table lookup only.
- Threat addon evidence confirms `TargetThreatListUpdated` is a real UI event; this is already backed by mapped `ServerEntityThreatListUpdate` (`0x0909`) and `ThreatManager.BroadcastThreatList()`.

### Remaining Decomp Targets
- **Entity aux consumers** (F-025): Need `Network_RegisterServerOpcode_0351` (0x14006c290, 56KB) to map opcodes to consumer handlers.
- **PvP cooldown/state consumer** (F-015): `ServerPvpCooldownUpdate` (`0x013E`) still only maps to shared `ServerUInt32_ReadPayload` (`14007ab50`) with no opcode-specific apply consumer found; direct Ghidra MCP plugin pass mapped adjacent `ServerUnitPvpStateChange` (`0x08BD`) reader `ServerUnitPvpStateChange_ReadPayload` (`140098160`) and apply path `Entity_ApplyUnitPvpFlagsChanged` (`1403ddc60`), which updates entity `+0x15a8` and dispatches `UnitPvpFlagsChanged`.
- **Item context action / Pet stance** (F-026): `0x00B7` native empty reader is
  mapped and the 2026-06-09 cached-export/source recheck found no producer,
  post-read consumer, or source emitter beyond the registration row; item-context
  producer/consumer semantics remain unknown. Pet stance request/apply flow is
  mapped through `Pet_SetStance_SendClientPetSetStance` (`14050a270`) for
  `0x068E`, `Pet_GetStance_ReadCachedStance` (`14050a130`) for the local cache
  read, and `Pet_ApplyStanceChangedPayload` (`1403c0a80`) for `0x068F`, but
  server producer timing/scope remains unknown.
- **ServerRaidQueueStatus / ServerRaidInfoResponse row** (F-010): reader `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` mapped; adjacent `ServerRaidInfoResponse_ReadPayload` @ `14008c010` reads `0x071A` count-plus-0x20-byte rows and `Group_DispatchRaidInfoResponse` @ `1406042b0` maps row fields to saved-instance id, world id, FILETIME expiration, days-from-now, and prime level; pass 157 retried MCP (`Transport closed`) and direct-plugin/cache xrefs still show `14008c010` as the only real code caller into `14008bf80`, exact selected send-helper scans find no `0x0718` producer, and data refs for `1406042b0` / `14008c010` are unowned metadata or PE `.pdata`-shaped runtime entries rather than opcode ownership; zero-value `0x0718` compatibility emit remains, while standalone non-zero queue timing/aliases stay blocked.
- **ServerRaidQueueStatus cached-export/source recheck** (F-010, 2026-06-09): cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `Network_RegisterServerOpcode_0351` (`14006c290`) still registers `0x0718` size `0x20` to `ServerRaidQueueStatus_ReadPayload` (`14008bf80`) and adjacent `0x071A` size `0x10` to `ServerRaidInfoResponse_ReadPayload` (`14008c010`). The `0x0718` row still reads `uint64`, 15-bit `uint32`, `uint64`, `float`, and `uint32`; `0x071A` still allocates count * `0x20` and calls the row reader. Selected xrefs for both readers are label-only, selected call edges keep `14008c010` as the only code caller into `14008bf80`, and `Group_DispatchRaidInfoResponse` (`1406042b0`) maps the row to `RaidInfoResponse` / `strSavedInstanceId` / world fields before a `ClientEvent` dispatch. No selected `Network_SendOpcodePayloadHelper` / `Network_SerialiseBufferedMessageById` evidence ties standalone non-zero `0x0718` to a producer. Current source keeps the raid-info zero-value compatibility emit in `ClientRaidInfoRequestHandler` and has packet-shape / placeholder tests only; next evidence remains a real non-`.pdata` producer/apply table or accepted non-zero `0x0718` capture proving queue-state semantics and timing.
- **ServerMatching0x05CF** (F-010): raw `uint32` reader `ServerUInt32_LocalReadThunk` @ `140099110` mapped; candidate apply helper `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0` remains correlated only until a real apply dispatcher/index or live `0x05CF` witness proves manager `+0xa0` semantics; the prior `140e1e66c` data ref is PE `.pdata` unwind metadata, the `WorldSocket+0x15b0` slot-11 scan found the Fortune positive control but no matching-helper route, and the 2026-06-05 MCP bridge retry still returns `Transport closed`, so the cache/direct-plugin recheck remains limited to broad manager globals plus shared `0x05CF`/`0x085D` registration refs. A 2026-06-06 solo queue live probe hit neighboring `0x05EF`, `0x05B4`, `0x05CA`, and `0x05C8` paths but did not capture `0x05CF` or hit `1405c41c0`.
- **ServerMatching0x05CF cached-export/source recheck** (F-010, 2026-06-09): existing `WildStar64.exe` cache `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` was sufficient. `Network_RegisterServerOpcode_0351` (`14006c290`) still registers `0x05CF` size `4` to `ServerUInt32_LocalReadThunk` (`140099110`), shared with `0x085D`; selected xrefs are label-only and call edges only reach `FUN_14006c090`. `MatchingManager_ApplyManagerUInt32Field0xA0` (`1405c41c0`) still writes the payload to manager `+0xa0` / optional subobject `+0x98` and calls `FUN_1400a8020`, but no selected xref or call edge proves opcode dispatch or `ClientEvent` ownership. Current source keeps `ServerMatching0x05CF` as neutral `ServerUnresolvedUIntPayload`, packet-shape only, non-emitted. Next evidence remains a real non-`.pdata` apply dispatcher/index binding `0x05CF` to `1405c41c0`, or an accepted `0x05CF` capture near `0x05CA`/`0x05CC` queue and match-ready transitions.
- **ServerMatchingGroupMemberRoleSelection** (F-010): `0x0600` remains a structural identity-plus-`uint32` wrapper through shared reader `ServerHousingCommunityPlotReservation_ReadPayload` @ `140086e70`; pass 159 MCP retry still returns `Transport closed`, direct-plugin xrefs show the reader is reused by eight registration rows plus `ServerLootWinner` row helpers rather than a matching apply owner, exact selected send-helper scans find no `0x0600` producer, and role-check UI/apply functions stay separate. The managed trailing field is now neutral `TrailingValue` until a real role-selection consumer or live payload proves role semantics.
- **ServerMatchingGroupMemberRoleSelection cached-export/source recheck** (F-010, 2026-06-09): cached `WildStar64.exe` fragments under `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` were sufficient. `Network_RegisterServerOpcode_0351` (`14006c290`) still registers `0x0600` size `0x18` to `ServerHousingCommunityPlotReservation_ReadPayload` (`140086e70`), the same identity-plus-`uint32` reader reused by seven other registration rows including `0x051F`. The selected xref is label-only; selected call edges are reader-local `FUN_14006c090` / `NetworkBitReader_ReadUInt64` calls plus two `ServerLootWinner_ReadPayload` (`1400a4e50`) row-helper calls, not matching role-selection ownership. Matching role-check/UI helpers (`14076b770`, `1405c0e90`, `1405c0760`, `1405c3d30`) remain separate and do not reference this reader. Current source keeps `ServerMatchingGroupMemberRoleSelection.TrailingValue` neutral with packet-shape-only coverage and no runtime emitter. Next evidence remains a real `0x0600` apply/consumer table, native producer, callback owner, or accepted live payload tying the trailing value to role-selection state.
- **Client0x062A/0634** (F-010): shared `uint32` reader/writer `14007d000`/`14007d010` mapped and handlers are log-only/test-pinned; pass 155 MCP/cache recheck still finds `0x062A`/`0x0634` only in movement-spline registration (`1400a82b6`/`1400a872c`), selected send-helper scans found no sender, message-name slots `140c25488`/`140c25528` are unxrefed, and positive controls `0x05D5` and `0x0635` have separate real send helpers. Sender/intent remains blocked until a native send site or live queue UI sniff appears.
- **Client0x0701** (F-002): 2-bit plus `uint32` writer `ClientUInt2UInt32_WritePayload` (`1400a69d0`) remains mapped and the handler is log-only/test-pinned; the 2026-06-09 cached-export/source recheck found only the `Network_RegisterServerOpcode_0351` registration row for `0x0701` (`selected_decompiled.c:13899`, setup literal `140079e05`), label-only xrefs for the writer, writer-local call edges, no selected `Network_SendOpcodePayloadHelper(...0x0701...)`, no selected `140dd0a74` pointer-owner evidence, and no standalone `1400a69c0` fragment in the selected cache. Positive controls for challenge choice, matching queue dispatch, and matching replacement use separate opcodes. Sender/intent remains blocked until a native send site, callback/table owner, indirect send rail, or accepted public-event/queue capture appears.
- **Client0x07E3** (F-002): shared `uint32` reader/writer `14007d000`/`14007d010` mapped and handler remains log-only/test-pinned; the 2026-06-09 cached-export/source recheck found only the `ClientWorldOpcodeRegister_MovementSpline` registration row (`1400a8282`), shared helper evidence, selected call edges for `14007d010` limited to writer-local code plus `ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), no selected `Network_SendOpcodePayloadHelper(...0x07E3...)`, and no selected static name-rail evidence for `140c27018`. Positive controls `0x05D5`, `0x0635`/`0x063A`, `0x063B`, and `0x0858` stay separate. Sender/intent remains blocked until a native send site, callback/table owner, indirect send rail, or accepted character/destination UI capture appears.
- **PrerequisiteType.Unknown260** (F-022): live case `0x104` / `Prerequisite_CheckHousingPlotPlugState_Table260` maps active housing plot/plug flags `0x08`/`0x20` against state `5`; the 2026-06-05 MCP passes label the `HousingPlotInfo` / `HousingPlugItem` lookup helpers (`140205fa0`/`140206c60`), the residence plot-entry lookup (`1405aea10`), `HousingPlotEntry_DispatchBuildCompleteIfState5` (`1405a9920`), and `HousingResidence_SetBuildState5AndDispatchComplete` (`1405a9980`), proving state `5` is `HousingBuildComplete`-backed. Enum rename remains blocked because row `36343` still lacks a prerequisite use-site and the active residence state fields at the returned subrecord remain unnamed.
- **PrerequisiteType.IsLocalPlayerEntity** (F-022): pass 140 recovered the formerly missing native function entry at `14049c7b0` as `Prerequisite_CheckIsLocalPlayerEntity`. The body is the already-implemented type 63 Equal/NotEqual identity comparison between the evaluated entity and `DAT_140c65898+0x78` local-player entity; no runtime behavior changed.
- **PrerequisiteType.DeadState / State** (F-022): pass 141 recovered live vtable entries `Prerequisite_CheckDeadState` (`14049c800`, case `0x0c` / `+0x80`) and `Prerequisite_CheckEntityStateFlags_Table149` (`14049c840`, case `0x95` / `+0x88`). DeadState now has a native live witness but NF keeps the existing `IsAlive` proxy until entity `+0x250` / `+0x254` / type `0x17` fields are server-owned; State remains mapped-only/blocked because type-149 row ids have no direct prerequisite-column references and `entity+0x1e4` ownership is incomplete. Pass 142 rejected the strongest cached `+0x1e4` writer cluster (`PrimalMatrix_UpdateNodeVisualStateFlags` `1406e70a0` / `PrimalMatrix_HandleNodeAllocationInput` `1406e73e0`) as a PrimalMatrix node visual/allocation path, not prerequisite entity-state evidence.
  Pass 143 mapped the scene proximity-cue reader/application cluster (`SceneProximityCue_LookupConfigById` `1404cc070`, `SceneProximityCue_IsCandidateInRange` `140722d30`, `SceneProximityCue_CheckPrerequisiteGate` `1407234a0`, `SceneProximityCue_ProcessQueuedCandidate` `1404cc0d0`, `Entity_ApplySceneProximityCue` `14047a1f0`, `SceneProximityCue_SelectWorldZoneEntry` `140722ed0`): it strengthens `entity+0x2ac` as an action/cue-blocking gate and `+0x250/+0x254` as approach/movement blockers, but still gives no native writer for evaluated-entity `+0x1e4` and no type-149 use-site.
- **Entity target relationship-code helper** (decompile-only): pass 144 labeled `Entity_GetRelationshipCodeToTargetId` (`14045a950`) and `EntityTarget_ApplyTargetedByUnitUpdate` (`14045bdc0`), correcting the old `UnitState_UpdateCachedStateFields` note so `entity+0x108` is treated as cached target entity id and `entity+0x10c` as a relationship code. Pass 145 promoted stale `EntityCriteria_GetAttr2_Unk118` (`1403b49c0`) to `EntityCriteria_GetFaction2Id` and mapped `entity+0x118` component slots `+0x18` Faction2Id, `+0x20` reputation value, and `+0x30` relationship code to Faction2Id. Pass 146 recovered the concrete Faction2 service/component vtable family: `Entity_InitBaseFaction2ComponentById` (`14045ac60`) seeds base `+0x110` from UnitCreated field `+0xd8`, `Entity_SetActiveFaction2ComponentById` (`14045ab70`) sets active `+0x118` from field `+0xd4`, and `Faction2Component_GetDispositionCodeToFaction2Id` (`140787830`) maps FactionLevel `0..2` to Hostile code `0`, `8..10` to Friendly code `2`, and all other levels to Neutral code `1`. Pass 147 tied those UnitCreated offsets back to `ServerEntityCreate_ReadPayload` (`140096fa0`) field order: `+0xd4` is the main 14-bit `Faction1` field and `+0xd8` is the main 14-bit `Faction2` field. The similarly populated entity-create aux `Value4`/`Value5` slots remain neutral because native `0x0263` reads three generic 17-bit values and no apply/consumer path proves semantic names. Pass 148 rechecked the known native `WorldSocket+0x15b0` handler families and the `0x025F`-`0x0264` reader xrefs; only diagnostic/log, console, options/addons, and Fortune nodes are mapped, with no entity-create aux apply owner. The native code categories now map to managed `Disposition` order; no runtime behavior changed.

### F-001 - STS / Token Crypto - PARTIAL (token crypto blocked)
Core STS login/account/game-token compatibility is implemented. STS session
dispatch now enforces non-`None` handler states, so `/Auth/LoginStart` requires
the connected state and `/Auth/KeyData` requires login-start, while existing
`SessionState.None` compatibility routes remain accepted for post-auth/account
and presence requests. Token crypto handshake, external-account edge routes, and
optional envelope fields remain blocked until crypto/token semantics are mapped
safely.
A 2026-06-09 cached-export/source recheck reconfirmed that the optional token
branch is mapped but not implementation-ready: native `StsConnLib64.MT.dll`
fragments prove `LoginTokenStart` -> server key material -> `TokenKeyData`
field flow, but do not prove the server-side RSA/signature trust anchor,
premaster derivation, reply fields, post-token encryption/session transition,
or startup route ordering. NexusForever still intentionally has no
`/Auth/LoginTokenStart`, `/Auth/TokenKeyData`, `/Auth/RequestToken`, or
`/Auth/AssociateMyExternalAccount` route handlers.

### F-002 - Client Diagnostic Opcodes - DIAGNOSTIC
16 client diagnostic surfaces with diagnostic handlers exist: 12 numeric
`Client0xNNNN` models plus `ClientAccountRealmData`,
`ClientRealmListRealmRow`, `ClientRealmListMessageRow`, and
`ClientAddonModuleList`. Every packet's wire shape is pinned by focused tests.
A 2026-06-04 guard now pins the unresolved diagnostic handler family as
log-only with no plaintext or encrypted server emit; focused diagnostic
coverage passed 90/90. A 2026-06-05 realm-row follow-up keeps
`ClientAccountRealmData`, `ClientRealmListRealmRow`, and
`ClientRealmListMessageRow` structural-only: selected cache call edges nest
their writers under the `Client0x0760` row / `ServerRealmList` serializers,
and no client send owner surfaced. A 2026-06-05 follow-up pins `Client0x00C8` as a
shared 5-bit `MatchType` wire shape that must not be aliased to queue-leave or
challenge-choice behavior without an opcode-specific sender; a second follow-up
pins `Client0x00ED` as mapped `uint64 + uint32 + uint64 + 3 bits` but rejects
duel, mail, and path names until an opcode-specific owner appears. A third
follow-up pins `Client0x011B`/`Client0x011D` as empty/`uint32` shared-writer
shapes and rejects loot-bind, mail, loot-vacuum, and tradeskill-reset aliases
without a direct owner. A 2026-06-09 cached-export/source recheck reconfirms
`Client0x012D` as a one-wide-string shared-writer diagnostic and rejects
`ClientSuggest`, account-item, realm-transfer, and pet/quest names until an
opcode-specific sender appears. A 2026-06-09 cached-export/source recheck
reconfirms `Client0x063E` as the sibling one-wide-string shared-writer
diagnostic; `LAB_140080d20` is shared/no-owner evidence, and suggest,
account-item, support-ticket, marketplace/auth/status, and filter aliases stay
rejected until an opcode-specific sender appears. A 2026-06-09
cached-export/source recheck reconfirms `Client0x0550` as a one-`uint32`
shared-writer diagnostic; selected send-helper and static name-rail evidence
found no `0x0550` owner, so ICComm, spell-list, matching-replacement,
movement-ack, tradeskill-reset, ability-book, combat-log, and P2P-trading
aliases stay rejected until an opcode-specific sender appears. A 2026-06-09
cached-export/source recheck keeps `Client0x0701` as a 2-bit plus `uint32`
diagnostic; selected export evidence found no sender or pointer-owner, so
public-event, queue, and movement aliases stay rejected until an opcode-specific
sender appears. A 2026-06-09
cached-export/source recheck likewise keeps `Client0x07E3` as a one-`uint32`
shared-writer diagnostic; selected send-helper and static name-rail evidence
found no `0x07E3` owner, so destination-arrow, character, pregame,
tradeskill, matching, and movement aliases stay rejected until an
opcode-specific sender appears. Client request
intent and server response behavior remain unknown. Needs Ghidra decomp of
client writer functions by feature cluster.
A 2026-06-09 cached-export/source recheck keeps `Client0x00ED` mapped-only and
diagnostic: Ghidra MCP discovery found no running instances, cached
`ClientUnresolvedDiagnosticPacket00ED_WritePayload` (`1400a6200`) still writes
one `uint64`, one `uint32`, one `uint64`, and three trailing bits, selected
xrefs/call edges only show the label, internal jumps, and generic bitstream
helpers, and focused source/tests keep `Value0`..`Value5` neutral with a
log-only/no-emit handler. Duel, mail, and path aliases remain rejected until an
opcode-specific sender, consumer, callback/table owner, or live payload proves
semantics.
A 2026-06-09 cached-export/source recheck also keeps `Client0x011B` and
`Client0x011D` mapped-only and diagnostic. Cached
`ClientCraftingAbandon_WritePayload` (`140001ba0`) is still an empty shared
writer; selected caller/xref evidence points at shared pointer/table contexts,
not an `0x011B` owner. Cached `ClientTradeskillResetTalents_WritePayload`
(`14007d010`) still writes one raw `uint32`; its selected code caller remains
`ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), not an
opcode-specific `0x011D` owner. Current source/tests keep `Client0x011B` empty,
`Client0x011D.Value` neutral, and both handlers log-only/no-emit. Loot-bind,
mail, loot-vacuum, and tradeskill-reset aliases remain rejected until a native
sender/consumer, indirect send rail, or live payload proves semantics.

### F-003 - Server Unresolved Output Opcodes - STRUCTURALLY CLOSED
All 702 server opcodes have models. One `Server0xNNNN` enum/model placeholder
remains: `Server0x0015`, whose native reader `ServerUInt5UInt32_ReadPayload`
(`140081f00`) maps one 5-bit field plus one `uint32` but not semantic owner or
producer behavior; the same reader is reused by matching opcode `0x0628` and
inside `ServerFortuneRewards`, so `Server0x0015` remains neutral rather than
being inferred as matching average-wait state. A 2026-06-09 cached-export/source
recheck found no running Ghidra MCP instance and reconfirmed the same boundary:
`0x0015` and `0x0628` both register the shared reader in
`Network_RegisterServerOpcode_0351`, `ServerFortuneRewards_ReadPayload`
(`140081f60`) calls it as a row helper, and the only positive matching apply
control remains `MatchingManager_ApplyMatchingAverageWaitTimeUpdated`
(`1405c0e00`), which dispatches `MatchingAverageWaitTimeUpdated` for `0x0628`.
No opcode-specific `0x0015` apply owner, producer, or post-read consumer
surfaced.
Of the 62 shape-mapped aux/spell
packets with neutral fields, 12 have controlled entity-create or selected
housing emit paths and 50 remain gated behind consumer or producer evidence
before production emission.
Named-but-partial packets remain tracked in their feature rows.
Eight native size-1 aux registrations now use empty wire models instead of
raw byte payloads: `0x00B7`, `0x00DF`, `0x00EE`, `0x0101`, `0x0143`,
`0x014D`, `0x0160`, and `0x0187`. Their feature-owned producer semantics
remain blocked.
A 2026-06-09 cached-export/source recheck keeps `ServerItemContextActionAck`
(`0x00B7`) mapped-only: `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x00B7` size `1` at `selected_decompiled.c:13226` to shared
`ServerEmpty_ReadPayload` (`14007d8e0`), whose cached fragment returns success
without reading fields when the payload pointer is present. Exact selected
scans found only that `0x00B7` registration, selected xrefs for `14007d8e0`
remain label-only, selected call edges show no opcode-specific reader caller,
and current source finds `ServerItemContextActionAck` only in the neutral packet
model plus packet-placeholder tests. Item/friendship positive controls stay
separate: `ItemUse_SendClientItemUse` (`140398cc0`) sends client opcode
`0x0943`, `Friendship_SendClientFriendshipBlock` (`1405e39e0`) sends `0x00B8`,
and `Friendship_SetAutoResponseMessagesAndSend` (`1405de2e0`) uses
`ClientFriendshipSetAutoResponseMessage_WritePayload` (`14007e4b0`) for
`0x03B7`. Keep `0x00B7` non-emitted until a native server producer/send site,
post-read apply owner, callback/table owner, or accepted item-context/friendship
capture proves intent and timing.
Scalar aux wrappers are also tightened where the native readers are proven:
`0x0181`, `0x01A6`, and `0x0846` now write direct `uint32` payloads, while
`0x0186` is corrected from a raw 4-byte placeholder to a 14-bit scalar.
The adjacent reputation/path-XP aux wrappers are narrowed from raw byte arrays:
`0x01A7` and `0x01A8` now write `uint64 + uint32`, while `0x01A9` now writes
`uint14 + uint32`. Producer semantics for `0x01A6` through `0x01A9` remain
blocked.
A 2026-06-09 cached-export/source recheck keeps `ServerTimeOfDayAuxUInt32`
(`0x0846`) mapped-only: `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x0846` size `4` to unlabelled reader slot `LAB_140080c60`, the
same selected slot used by `0x01A6`; selected cache has no standalone
`140080c60` fragment, selected scans found no `0x0846` sender/producer or
apply owner beyond registration, and current runtime time sync still emits the
separate `ServerTimeOfDay` (`0x0845`) packet from `Player.SendInGameTime`.
`Prerequisite_CheckTimeOfDay` (`14049dd10`) only proves the client-side
time-of-day prerequisite comparison path, not `0x0846` producer timing.
The story/recruitment boundary packets are also narrowed: `0x074A` now writes
five `uint32` fields plus one `uint16`, and `0x077E` now writes the native
count-plus-uint32-list shape shared with `ServerFlightPathUpdate`.

### F-004 - Housing - PARTIAL (21/22 handlers complete)
**Implemented:** Residence create (level-14 gate), neighbor persistence,
harvest splitting, direct visits, community rename/placement/privacy/removal,
return handler, vendor list, decor/plug/remodel/flags delegation.

**Fixed this pass:** `ClientHousingEditModeHandler` - was empty-body stub,
now validates residence map, records per-player server edit state, and logs the
toggle. Private residence visits now allow owners/authorized modifiers through
the residence entrance teleport path while
unauthorized visitors still receive `Visit_Private`. Decor create now validates
colour shift, scale, and non-crate plot position before currency debit,
achievement update, or `DecorCreate`; focused housing coverage passed 93/93.
Decor move now rejects negative scale before decor mutation or broadcast; the
focused map-instance decor/wallpaper class passed 4/4 from the alternate output
directory. Community rename now treats a missing `GameFormula` table like the
existing missing row `2395` path, returning `HousingResult.Failed` before
currency checks/debits or community/residence rename; focused rename coverage
passed 3/3 and broader housing coverage passed 104/104. Housing vendor-list
requests now tolerate missing `HousingPlugItem` tables by sending an empty list
and missing `HousingContributionInfo` tables by keeping plug costs at `0`;
focused vendor-list coverage passed 3/3 and broader housing coverage passed
107/107. Residence entrance resolution now treats missing
`HousingPropertyInfo`, `WorldLocation2`, and `World` static data through the
existing `HousingException` boundary before teleport helper construction;
focused entrance coverage passed 7/7 and broader housing coverage passed
121/121.
A 2026-06-04 F-004 closure removed the WIP hardcoded
`ServerHousingProperties.Residence.NeighbourhoodId` value; property rows now use
the residence `GuildOwnerId` when present and `0` otherwise, with focused
housing coverage passing 32/32.
The 2026-06-09 cached-export/source recheck for the housing-basics follow-up
pair kept `ServerHousingBasicsEmpty` (`0x010D`) and
`ServerHousingBasicsFollowup` (`0x0110`) mapped at wire shape only.
`Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x010D` size `1`
to shared `ServerEmpty_ReadPayload` (`14007d8e0`) and `0x0110` size `0x10` to
`ServerHousingBasicsFollowup_ReadPayload` (`14008de70`: `uint32`, 18-bit
scalar, `uint32`, 8-bit scalar). The positive `ServerHousingBasics` (`0x010E`)
apply boundary is separate: `ServerHousingBasics_ReadPayload` (`14008e610`)
feeds client state written by `1403cd6d0`, which dispatches
`HousingBasicsUpdated` / `HousingPrivacyUpdated`. Selected xrefs for
`14008de70` remain label-only and selected call edges are reader-local, so the
managed `ResidenceManager.SendHousingBasics` -> `HousingAuxiliaryPacketEmitter`
compatibility bundle keeps `0x0110` fields neutral/defaulted until a native
producer/apply owner, callback/table owner, or accepted housing-login/return
capture proves follow-up semantics.
The 2026-06-09 cached-export/source recheck for
`ServerHousingCommunityDonateUpdate` (`0x04FE`) kept the donate-update field
semantics mapped-only. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x04FE` size `0x18` at `selected_decompiled.c:13681` to
`ServerHousingCommunityDonateUpdate_ReadPayload` (`14009e930`), whose cached
fragment reads a `uint32` count and then two allocated parallel `uint32` arrays
of that count. Selected xrefs for `14009e930` are label-only and selected call
edges stay inside reader-local bit-read, allocation, and raw-copy helpers. The
separate client positive control registers `ClientHousingCommunityDonate`
(`0x04F5`) at `selected_decompiled.c:13648` to
`ClientHousingCommunityDonate_WritePayload` (`14009da00`), and
`Housing_SendClientCommunityDonate` (`1404b9ca0`) sends one selected decor row
after resolving `HousingDecorInfo` and rejecting rows with `Flags & 0x8`.
Current source emits `ServerHousingCommunityDonateUpdate` from
`ClientHousingCommunityDonateHandler` after copying donated crate decor to the
community residence and deleting the source decor, while the model and packet
tests keep the two arrays as neutral `Value0`/`Value1`. Native evidence still
does not prove the array meanings, resource/contribution costs, exact transfer
semantics, or broader ownership/unlock policy.
The 2026-06-09 cached-export/source recheck for
`ServerHousingCommunityPlotReservation` (`0x051F`) kept the packet field-mapped
without widening broader community semantics. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `0x051F` size `0x18` at
`selected_decompiled.c:13683` to `ServerHousingCommunityPlotReservation_ReadPayload`
(`140086e70`), whose cached fragment reads target residence identity
(14-bit realm plus 64-bit residence id) and one `uint32` plot index. Selected
xrefs for `140086e70` are label-only; selected call edges are reader-local
through `FUN_14006c090` / `NetworkBitReader_ReadUInt64`, with only
`ServerLootWinner_ReadPayload` row-helper reuse. `Lua_HousingLib_GetReservedCommunityPlotIndex`
(`140737470`) looks up the type-7 community cache through
`Housing_FindCommunityResidenceEntryByIdentity` (`14057ff90`) and exposes
`nPlotIndex` / `bHasReservation` only when the stored value is not
`0xffffffff`. Client reserve/remove positive controls stay on `0x04B1` guild
operations: `Housing_SendClientCommunityPlotReservation` (`14057fe90`) sends
operation `0x29` with the requested plot index, while
`Housing_SendClientCommunityPlotReservationRemoval` (`14057ff10`) sends
operation `0x2A` with the target name. Current source emits
`ServerHousingCommunityPlotReservation` from `CommunityOperations.SendCommunityPlotReservation`
after plot-reservation or reservation-removal guild operations, using
`unchecked((uint)plotIndex)` so `-1` becomes the native `uint.MaxValue`
sentinel pinned by packet-shape tests. This does not prove retail permission
policy, temporary/permanent plot-state timing, teleport/unload behavior,
persistence timing, or community UI broadcast scope beyond the existing source
path.

**Branch housing script port:** The useful housing scripts from
`LaughingWS/NexusForever` branch `Questing-and-more` are now in
`Source/NexusForever.Script.Main/Housing`: the housing portal (`26350`) grants
missing Recall/Escape House spell-base rows and casts the housing dialog spell,
while the Thayd/Illium housing intro decor entities (`54400`/`54401`/`54403`/
`54404`/`65296`/`65297`/`65298`/`65299`) send their mapped story panels.
The branch housing active-prop doors (`65852`/`70052`/`75398`) now initialise
closed, toggle `StandState`, and emit the matching door emote from the
activator's visibility set. Focused branch-script/event tests passed with the
transporter/creature/Space Madness/Protogames Academy ports; see the top status
line for the latest combined count.

**Blocked:** `ServerHousingNeighborhoodEntry` (0x0501) and
`ServerHousingNeighborhoodList` (0x0506) - packet models exist but nobody
sends them. Unknown client request trigger (not accessible via addon API).
The old tracker note that a hardcoded neighborhood property blocks this packet
cluster is stale: `ServerHousingProperties.Residence.NeighbourhoodId` is a
separate property row and now uses the residence `GuildOwnerId` when present and
`0` otherwise. A 2026-06-04 Ghidra MCP recheck reconfirmed only the client cache
consumer and ruled out adjacent community placement, plot reservation, and visit
senders (`0x052B`, guild-operation `0x04B1`, `0x052F`) as the missing `0x0506`
trigger. A later 2026-06-04 export pass labelled WildStar64
`ClientDB_RegisterHousingNeighborhoodInfo` (`0x140205900`), confirming the table
loader only; row backing and send timing remain blocked. A 2026-06-09 cached
fragment/source recheck again found no running Ghidra MCP instance, confirmed
`14009cbe0`, `14009ebf0`, `1404ba4f0`, and `140205900` are reader/apply/table
loader evidence only, and found no production source emitter for `0x0501` /
`0x0506`.
Focused tracker-cleanup verification passed 27/27 against housing packet shapes,
residence-session non-emission, and the three residence neighborhood property
cases; pass 119 housing/placeholder guard coverage passed 106/106 and keeps the
row tail wire-named until producer/backing proof exists.

### F-005 - Marketplace / Auction / Commodity / CREDD - COMPLETE
**All 15 client messages have complete handlers.** Auction post/search/buyout/bid,
commodity post/cancel/buy/sell matching, CREDD exchange/history/redeem.
DB persistence for auctions, commodity orders, CREDD orders, CREDD history.
Slot limits: Free 3 / Signature 30. Offline marketplace credits via mail.
Server aux packet contracts `0x06DF` and `0x07D5` are reader-mapped and
packet-covered; their marketplace producer/consumer semantics remain blocked.
The 2026-06-04 MCP/export recheck found only registration call-site/data xrefs
for those aux readers, not a static apply path, producer, or emit timing proof.
Unsupported auction property min/max, rune-slot, equippable-by filters, and
property sort are rejected at validation instead of accepted and silently
ignored; retail stat/rune/equippable filtering remains unmapped.

**Fixed this pass:** Commodity order expiration - `ProcessExpiredCommodityOrders()`
scans every 1s, refunds/returns expired orders, sends `ServerCommodityAuctionRemoved`.
Marketplace settlement delivery-failure guards now keep auction/commodity records
active when item delivery cannot be made. Commodity sell cancel/fill uses
conservative stack-slot capacity preflight plus mail fallback success before
mutation; auction expiry/buyout waits for delivery success before seller credit
and order removal. `ForceImmediate` commodity orders now match without becoming
resting orders, skip the active-order cap, and refund/return unmatched remainder
immediately; partial commodity fill refunds now exclude the filled purchase cost.
Persisted commodity rows now validate id, owner, Item2, quantity, escrow,
legacy `ForceImmediate`, and list/expiration ordering before load, skipping
corrupt rows before they can refund or deliver. Marketplace mail content type is
now persisted in `character_mail.contentType`, preserving auction expired/return
semantics after reload while legacy rows still fall back to sender type. Auction
search paging now handles huge client page values by returning an empty page
instead of overflowing. Auction search selector validation now treats missing
family/category/type static tables like missing selector rows and rejects the
request through the existing invalid-packet boundary. Settled auction delete
persistence saves the moved item state instead of deleting the item row.
Auction/commodity insert DB failures now
roll back transient listing/order/item/escrow state, auction bid update DB
failure restores the prior bid and refunds the bidder, and marketplace mail save
failures restore attached item owner state before reporting delivery failure.
Online-inventory auction buyout now persists the auction delete and moved item
state before buyer debit, seller credit, winner notification, auction removal,
or inventory delivery; delete-save failure returns `DbFailure` and leaves the
auction active. Online-inventory auction cancel now likewise persists the
auction delete plus returned item state before bidder refund, inventory return,
or auction removal; delete-save failure returns `DbFailure` and leaves the
auction active. Online-inventory auction expiration now persists the auction
delete plus moved item state before direct winner inventory delivery, seller
credit, winner notification, or auction removal; delete-save failure leaves the
auction active. Direct commodity buy/sell cancels now persist the order delete
before escrow refund or inventory item recreation, so delete-save failure
returns `DbFailure` and leaves the commodity order active. Direct commodity
expiration likewise persists the order delete before online escrow refund,
inventory item recreation, removal notification, or order removal.
Auction won/return mail settlement now composes mail creation, attached item
save, and auction row deletion in one character DB save for buyout, cancel, and
expiration mail delivery; composed save failure restores attached item owner
state and leaves the settlement uncommitted.
Commodity sell-order return mail for cancel/expiration now composes mail
creation, returned-item save, and commodity order deletion in one character DB
save; expiration fallback uses commodity return mail content instead of the
fill-mail path, and composed save failure leaves the order active without a
removal notification.
Commodity fill mail now composes buyer mail creation, purchased-item save, and
resting buy/sell order update/delete in one character DB save when a match must
deliver purchased items by mail; composed save failure leaves the match unfilled
and keeps resting commodity orders active.
Direct commodity fills now persist the resting buy/sell order update/delete
before online inventory delivery, seller credit, buyer price-improvement refund,
or fill notifications; save failure leaves the match unfilled, refunds the
force-immediate buyer's escrow, and keeps the resting order active. A focused
multi-order direct buy test now pins price-priority split fills and the
price-improvement refund across two sell orders.
Commodity matching now uses per-item/per-side price indexes for opposite-side
candidate selection and immediate sell preflight, preserving price-priority
behavior while avoiding full commodity-list scans for every match attempt.
Item-auction sale settlement now composes auction row deletion, moved item
state, and offline seller credit mail in one required character DB save for
direct winner-inventory delivery and auction-won mail delivery; no-DB
offline-seller sale attempts stay pending rather than delivering the item and
dropping seller proceeds. Expired offline commodity buy orders now compose order
deletion and refund credit mail in one required character DB save, leaving the
order active when the refund cannot persist. Commodity fills now compose
resting order update/delete with offline seller proceeds and offline buyer
price-improvement refunds in one required character DB save, leaving matches
unfilled when those credits cannot persist. Remaining final settlement
atomicity is source-local closed: auction bidder refunds now compose with bid
updates, auction delete, or item-return mail saves, and the old marketplace
`CreditCharacter` fallback has been removed.

### F-006 - Storefront / Account Inventory - COMPLETE
Catalog, supported account-currency purchase, claim/return, pending item groups,
daily login, coupon redemption, wallet updates, purchase history, privilege
restriction, purchase-velocity gate (10/hr), and CREDD redeem (1000:1).
70 focused packet/account tests. All `0969..0991` opcodes mapped.
Character-select direct account purchases now apply entitlement/currency changes
immediately and refresh the character list; in-world account-inventory direct
grants now handle targeted character rows and enforce character-slot cap
prerequisites through entitlement max-count.
Account-item take, pending group claim/return/gift, daily-login claim, and
coupon redemption now fail closed with existing `AccountOperationResult`
failures when an account inventory manager is unavailable; failed take requests
skip account persistence and character-list refresh.
Daily-login reward refresh and claim paths now also treat a missing
`DailyLoginReward` table like an empty configured schedule, emitting zero
available rewards and avoiding inventory checks or grants when no table-backed
reward row exists.
Configured account-item cooldown-group seeding now treats a missing
`AccountItemCooldownGroup` table like an empty configured list while preserving
persisted cooldown rows and active cooldown-list emission.
Account-item existence checks and inventory-item materialization now also treat
a missing `AccountItem` table like missing account-item rows: add eligibility
returns false, while persisted/create item materialization keeps the existing
invalid account-item exception boundary.
Entitlement-backed account-item grant planning now also treats a missing
`Entitlement` table like missing entitlement rows, returning the existing
`InvalidAccountItem` result before entitlement, currency, delete, or cooldown
side effects.
Account and character entitlement managers now also treat a missing
`Entitlement` table like missing entitlement rows during persisted load and
direct updates, preserving the existing `DatabaseDataException` / invalid
entitlement exception boundaries before mutation or entitlement packet
emission.
Account-currency load/materialization now also treats a missing
`AccountCurrencyType` table like missing account-currency rows: persisted
balances remain readable and serializable, while new balance creation for add
or subtract reaches the existing invalid static-currency exception before
mutation or wallet packet emission.
Storefront catalog offer-item data construction now also treats a missing
`AccountItem` table like missing account-item rows, preserving the existing
invalid `ItemId` catalog-data exception before store offer item-data packet
rows are built.
Account-item generic-unlock claim grants now also treat missing
`GenericUnlockSet` / `GenericUnlockEntry` tables like invalid static data,
returning `InvalidAccountItem` before unlock grant application or item deletion.
Storefront purchases now reject non-type-0 catalog offer-item rows before
currency debit or account-item delivery; type-1/type-2 offer effects remain
catalog-readable but runtime-blocked until retail purchase semantics are mapped.
The `LaughingWS/Questing-and-more` account/storefront slice is superseded by
the current implementation: its useful account inventory, cooldown, operation
result, account-tier, catalog, purchase, and purchase-history surfaces are
already covered, while the branch's `0x097D` transaction-update name is rejected
by current pending-item-group delete evidence and its `0x03DD` subscription
packet remains unmapped/unemitted.
Known retail gaps remain for the real-money/Protobucks VC request-confirm path,
the non-empty `0x026A` owned-order tail, live `0x0986`/`0x098F` emission,
non-zero `096A..096C` leading-field producers, type-1/type-2 offer-item purchase
effects, and the native coupon sender for `0x0790`.

### F-007 - Reward Rotation - PARTIAL
Game-table refresh emits (`0x07CA` schedule, `0x07CD`/`0x07D3` content context),
`AccountRewardRotationGrantManager` + `account_reward_rotation_grant` persistence,
claim path via `ClientRewardUpdateRequest`, and non-empty `0x07C8` entry-state on
refresh/claim are implemented with focused tests.

2026-06-06 live-local triage of an empty Content Finder Bonus Rewards tab found
three schedule/display issues. First, the default refresh path selected rewards
with player level `1` and world difficulty flags `0`, while the imported
`RewardRotationItem`/`Essence`/`Modifier` rows require level `50` and
normal/veteran flags (`1`/`2`). Second, a new-binary smoke showed non-empty
`0x07CA` rows for indexes `1..6` but the tab still empty because
`Game.MatchMakingEntry.GetRotationRewards` filters loaded rows by the selected
entry's normal/veteran context. The default world request path now passes the
live player level plus a known normal/veteran mask, and the schedule builder
expands that mask into separate normal and veteran candidate rows per
content/reward family. Third, the native apply path uses the 14-bit schedule
field as `RewardRotationContent` id and the trailing 32-bit field as the
`RewardRotationItem`/`Essence`/`Modifier` lookup id; the prior writer sent
content id first and count/value last, so the client could receive rows without
being able to resolve visible reward entries. `ServerRewardRotationScheduleArray`
now writes the native apply order and generated rows put the catalog row id in
the trailing lookup field. This restores display-eligible level-50 rows without
claiming exact retail per-content selection parity. The full
`NexusForever.Game.Tests` project now passes `2770/2770` with the corrected
packet model, reward schedule path, and item-salvage manager contract.

2026-06-08 source-local guard: premium reward-property modifiers now skip rows
whose `RewardProperty` table/row or entitlement table/row is unavailable instead
of crashing account reward-property setup or emitting guessed values. Type-based
reward-property updates no-op when `RewardProperty` static data is unavailable,
and spell reward-property modifier resolution follows the existing
`unknown-reward-property` diagnostic boundary for a missing `RewardProperty`
table. Focused reward-property verification passed 5/5 and the broader
reward/spell bucket passed 375/375 from the alternate output directory.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running
instances, so no new labels or exports were added. Cached fragments still show
`0x07CD` registering only `ServerRewardRotationContentContext_ReadPayload`
(`14008fcb0`) with a null static handler; `RewardRotation_ManagerInit`
(`140635840`) initializes seven throttle slots at
`manager + 0x150 + index * 0x14`; `Reward_SendRewardUpdateRequest`
(`140636ba0`) sends only the content-type index through `0x07CC` and reads
those throttle slots; and `RewardRotation_GetLoadedScheduleForContent`
(`140636c40`) proves refresh-by-index / loaded-schedule lookup, not server
context apply assignment.
`RewardRotationRuntimeEvidenceTests` now pins the generated evidence artifact's
blocker text for the missing `0x07CD` apply helper and blocked `Flag` /
throttle-slot assignment, so capture bundles preserve the mapped-only boundary.

**Blocked:** per-content authoritative reward mapping precision, `0x07CD` apply/`Flag`
consumer semantics, dynamic throttle-slot assignment, and exact retail
difficulty/content reward selection. Next evidence source is a retail `0x07CD`
capture or dynamic breakpoint on the runtime apply dispatch.

### F-008 - Crafting / Tradeskill - PARTIAL (12/12 handlers functional)
**All 12 client handlers complete** with proper request/response cycles.
Fixed-recipe crafting, supply satchel, tradeskill lifecycle, schematic
learning, profession modifiers, additive state/abandon clearing, rune slot management, rune fusion (add,
clear, install, reroll - all 4 operations send `ServerTradeskillSigilResult`).
Material availability now combines satchel and inventory counts with widened
arithmetic so saturated inventory stack totals cannot overflow into a false
missing-material failure before debit.
Fixed-recipe crafting now also tolerates partial game-table loads for the
schematic, item, material, and tier tables: missing schematic/item tables follow
the existing invalid-request boundary before mutation, missing material tables
fall back to inventory-only debits, and missing tier tables complete with zero
craft XP. Tradeskill request validation now treats missing `Tradeskill`,
`TradeskillBonus`, and `TradeskillTalentTier` tables like missing rows through
the existing invalid-packet boundary. Rune install, additive/catalyst request
validation, and queued modifier materialization now treat missing `Item`,
`Item2Category`, `TradeskillAdditive`, `TradeskillCatalyst`, and `ItemSpecial`
tables like missing rows through the existing invalid-result paths. Focused
partial-table verification passed 12/12, focused tradeskill request
verification passed 5/5, focused rune/modifier verification passed 63/63, and
broader crafting verification passed 90/90.

2026-06-09 live station/craft proof closed the generic-station fixed-recipe
smoke for emulator parity. Bundle
`artifacts\blocker_evidence\20260609-212659-F008-crafting-live` and packet
evidence `artifacts\packet_evidence\F008-crafting-live\20260609-201741-packet-evidence.jsonl`
show the Thayd generic station opening through activate spell `1817` on
creature `21793` after prerequisite `1050` (`IsPlayer == 0`) is evaluated
against the activated unit. The UI `Simple Craft` button sent
`ClientCraftingCraftItemAutoCraft(0x0852)` with station unit `354`; that station
reported `Creature2.TradeSkillIdStation = 4294967295`, so validation now
accepts both observed all-tradeskills sentinel encodings (`uint.MaxValue` and
the SQL/reference `int.MaxValue`). Contexts `47..49` completed schematic `270`,
emitting `ServerSupplySatchelUpdate(0x0199)` plus
`ServerCraftingFinish(0x0853)` and producing item `14838` x12 after three
crafts while material `11` persisted at `28`.

2026-06-09 live Tech Tree/talent proof split the follow-up into one fixed
server-state bug and one remaining presentation/evidence gap. The Technologist
craft threshold did persist server-side: character `30` had `tradeskillXp =
480`, and tier achievement `1481` was completed after crossing the tier-2
`450` XP requirement. The visible Crafting Result text still showed
`480/450`, so the suspected issue is now packet/order/client presentation
rather than lost XP; the packet evidence filter did not include
`ServerProfessionUpdate(0x0860)`, so exact profession-update payload/order
remains blocked pending a capture with `0x0860` enabled. The actual missing
gameplay state was Tech Tree reward application: completed achievement `1470`
maps through `TradeskillAchievementReward` `599` to one Technologist talent
point, and achievement `1471` maps through reward `162` to one talent point plus
schematic `2954` (`Spirovine Extract`). NexusForever now applies completed
`TradeskillAchievementReward` rows idempotently, backfills them on login and
tradeskill learn, and removed the old simplified 10-free-talent-point seed from
learn/reset behavior. A live retry exposed the final dependency: the
`TradeskillAchievementReward.tbl` file existed in runtime assets but was not
marked `[GameData]`, so WorldServer never loaded it and the reward pass saw zero
rows. `TradeskillAchievementReward` is now a runtime-required game table and is
pinned by the game-table contract tests. Focused F-008/tradeskill reward/table
verification passed 47/47, and WorldServer build passed. After reconnecting
character `30`, the repair path persisted Technologist `talentPoints = 2` and
schematic `2954`, while retaining `tradeskillXp = 480`.

The 2026-06-09 cached-export/source recheck kept current-craft and crafting aux
producers blocked: `Network_RegisterServerOpcode_0351` (`14006c290`) registers
`0x0854` size `0x50` to `ServerCraftingCurrentCraft_ReadPayload`
(`1400a46b0`), `0x084B` size `0x18` to
`ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload` (`1400a3af0`), and
`0x0855` size `0x0c` to `ServerUInt32AndTwoFloats_ReadPayload`
(`140081df0`). `Crafting_HandleServerCraftingCurrentCraft` (`1405e6830`) only
stores current-craft state and dispatches `CraftingUpdateCurrent`, while
`Crafting_HandleServerCraftingFinish` (`1405e6690`) remains the positive
finish/hot-cold consumer. Current source emits `ServerCraftingFinish` and
`ServerTradeskillSigilResult` only, and focused tests guard against
`ServerCraftingCurrentCraft`, `0x084B`, and `0x0855` production.

**Remaining gaps:**
- Complex craft stats (CraftStats, ApSpSplitDelta, ChargeCounts discarded)
- Discovery attempt coordinate packing, hot/cold emit timing, and unlock mutation remain blocked; validate with an F-008 live capture before enabling non-success discovery results
- Crafting station request semantics are implemented; native service-key names remain diagnostic-only
- ServerCraftingCurrentCraft (`0x0854`) cadence and 0x084B/0x0855 emit intent (corrected 80-byte, 24-byte, and 12-byte payload shapes modeled, never sent; 2026-06-09 cached-export/source recheck found reader/apply evidence only)
- Crafting-result profession-progress display/order, including the observed
  `480/450` stale threshold, remains blocked on live packet evidence that
  includes `ServerProfessionUpdate(0x0860)`
- Non-success sigil result rules and server-side `ServerItemMicrochips` (`0x056C`)
  patch producer precision
  (microchip taxonomy cleanup verification passed 149/149)

### F-009 - Rapid Transport / Taxi / Flight Path - PARTIAL (route-table fix verified; blockers remain)
Pricing/request validation is partial. The 2026-06-04 taxi/rapid-transport
capture proved live `ClientRapidTransport` (`0x0141`) requests for taxi nodes
`89`/`88` plus `ClientFlightPathPurchase` (`0x00FF`) route chains
`[236]`, `[124]`, `[124, 121]`, `[8, 114]`, and `[8]`; the failure root cause
was `TaxiRoute.tbl` not being loaded. `TaxiRoute` is now `[GameData]`, the
rapid/taxi handlers reject cleanly when `TaxiRoute`, `TaxiNode`, or
`WorldLocation2` tables are unavailable. Rapid transport also rejects missing
or empty rapid-transport spell formula data with `RapidTransportInvalid` before
cooldown checks, currency debit, or spell cast, and focused handler tests cover
captured table-backed route charging, rapid-transport spell cast context, and
flight-path destination teleport. Transport handler tests now also pin missing
taxi-node and world-location table rejection before rapid-transport debit/cast
or flight-path debit/teleport. A 2026-06-08 local F-009 pass captured node `88`
(`Sylvan Glade, Celestion`) as an accepted rapid-transport route `124` / price
`531` / spell `82922` case, repeated node `89` (`Grimhold, Celestion`) retries
as `SpellCooldown` rejects after the one-hour `82922` cooldown was set, route
`[120]` as an accepted `ClientFlightPathPurchase` source `88` -> destination
`89`, and node `89` as an accepted rapid-transport route `120` after
`!spell resetcooldown 82922`. The same pass captured node `214` (`The Pools of
Vitara, Celestion`) as a client-visible rapid destination with UI price
`1g 14s 23c` that the server rejects with `RapidTransportInvalid` because
`TaxiRoute.tbl` has no routes to or from `214`; a follow-up user observation of
node `212` (`Hijunga Village, Celestion`, UI price `1g 20s 80c`) matches the
same no-`TaxiRoute` class while still having valid `TaxiNode`/`WorldLocation2`
data. The fresh server log also captured Whitevale nodes `222` (`Raxen's
Holdout`) and `223` (`Doomtide Village`) as valid `TaxiNode`/`WorldLocation2`
rows with zero `TaxiRoute` rows and `RapidTransportInvalid` rejects. This keeps
formula-driven rapid transport pricing/teleport parity blocked beyond captured
table-backed route branches. The packet evidence contains only
`ClientRapidTransport` (`0x0141`) rows for rapid requests, not
`ClientSpellCastWithServiceToken` (`0x00C2`).
A 2026-06-09 cached-export/source recheck for
`ClientSpellCastWithServiceToken` (`0x00C2`) kept that sub-surface
source-aligned but did not widen transport bypass semantics.
`Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x00C2` size
`8` at `selected_decompiled.c:13077` with
`ClientSpellCastWithServiceToken_WritePayload` (`140089570`), whose cached
fragment writes an 18-bit context token followed by a 32-bit `Spell4` id.
`ServiceToken_HandleCastResult` (`140520c10`) gates on
`Spell4.PropertyFlags & 0x20000000` and calls
`ServiceToken_SendClientSpellCastWithServiceToken` (`1403994f0`), which
returns `0x014B` on insufficient funds or sends opcode `0x00C2` through
`Network_SendOpcodePayloadHelper`. Current source already mirrors this with
`ClientSpellCastWithServiceTokenHandler`, `SpellParameters.UseServiceTokenCost`,
and `Spell.TryConsumeServiceTokenCost`. Remaining F-009
transport service-token/global-route blockers still need native/live evidence
that a transport UI branch uses this path and proves route-state, cost, and
teleport timing.
Focused transport verification passed 77/77.
Vehicle handler boundary tests pin
`ClientVehicleEmbark` as diagnostic/log-only and `ClientVehicleDisembark` as a
guarded `Dismount()` delegate only when the player is already platformed.
Transport service-token bypass, global route-state snapshots, taxi embark/completion
timing, broader charge/teleport parity, seat assignment, passenger lifecycle,
and deployable vehicle semantics remain blocked by live/native evidence.
`ServerVehicleEmbarkAux` (`0x01B2`) now uses its mapped client-reader shape
(`flag`, 2-bit value, `uint64`, two `uint32` fields) instead of a fixed raw
payload. Field semantics and runtime producer timing remain blocked.
`ServerRecruitmentAuxUInt32List` (`0x077E`) now matches the shared
`ServerFlightPathUpdate_ReadPayload` count-plus-uint32-list shape; flight/taxi
alias semantics and emit timing remain blocked.

**Branch transporter port:** The useful table-backed transporter catalogue from
`LaughingWS/NexusForever` branch `Questing-and-more` is now consolidated in
`Source/NexusForever.Script.Main/Transport/WorldLocationTeleporterEntityScript.cs`.
It covers the branch's Illium/Thayd city route pads plus Star-Comm Basin,
Algoroc/Northern Wilds, Datascape, Ellevar, Genetic Archives, and
Illium/Thayd Arcterra/Halon Ring/Malgrave/Museum routes through `WorldLocation2`
destinations, plus the table-backed Exo-Lab 71 (`28454` -> `21760`) and
Exo-Lab 22 (`25886` -> `19282`) range triggers. The Exo-Lab 22 destination is
the client `WorldLocation2` row whose coordinates match the branch route, while
the official world database supplies the outer `25886` teleporter placement; the
branch's extra source entity row remains unimported because its SQL marks the
ActivePropId as fake. A narrower coordinate route pass now ports the branch's
starter-zone and cross-zone pads only where the official world database has paired
portal/transport placement evidence: Everstar Grove -> Celestion (`32269`),
Deradune/Levian Bay -> Crimson Isle (`45366`/`70174`), Northern Wilds ->
Everstar Grove (`70172`), and Ellevar/Crimson Isle -> Levian Bay
(`30352`/`70173`). The script guards missing rows / missing worlds / pending
teleports and preserves the Exo-Lab trigger-plane side check. Focused
transporter tests passed 49/49. The branch housing return pad remains
superseded by current housing return handling.

### F-010 - Group / Raid / Matching - PARTIAL (replacement backfill remains blocked)
All 13 group client handlers, 16 internal group handlers, 14 matching handlers,
raid info, quest share, instance settings - all fully functional.
Group instance difficulty now routes through the group server with leader-only
runtime group-state mutation, online member `InstanceDifficulty` sync, and
`ServerGroupInstanceDifficultyResponse` fan-out. Internal group broadcast handlers
now use a shared online-member fan-out helper, and group flag updates reuse one
immutable role/flag packet plus one ready-check packet per update; group-focused
tests passed 158/158.
Ready-check pending state is intentionally sent as a single `Pending`
member-flag update after in-memory ready/has-set-ready clears, and the world
handler always emits `ServerGroupReadyCheckStatusUpdate` including status `0`
for pending/cleared state.

The 2026-06-06 live CDB smokes confirmed the solo group-finder flow:
`Matching_QueueDispatchFromUi` (`14076c830`) and
`ClientMatchingQueue_WritePayload` (`140098a70`) hit before each
`ClientMatchingQueue(0x05EF)` server receive; the widened helper pass also hit
`Network_SendOpcodePayloadHelper` for `0x05EF` eleven times and `0x05B4` nine
times, proving queue submit and leave-all are client-owned helper sends; queue
cancellation used `ClientMatchingQueueLeaveAll(0x05B4)`; server
`ServerMatchingMatchReady(0x05CA)`
hit `MatchingManager_ApplyMatchingGameReady` (`1405c39f0`); ready prompt
responses hit `MatchingManager_SendClientGameReadyResponse` (`1405c3500`) and
arrived as `ClientMatchingGameReadyResponse(0x05C8)` false declines plus one
true accept that completed `ServerMatchingMatchJoined`. The same pass produced no
live witness for `0x05CF`, `0x0600`, `0x062A`, `0x0634`, `0x0719`, or
standalone non-zero `0x0718`.

**Validation/logging only (not full LFR backfill):**
- `ClientMatchingMatchInitiateLookingForReplacementsHandler` / `ClientMatchingStopLookingForReplacementsHandler` validate in-progress match membership and native role mask `0..2`; they do not start a server replacement queue
- Focused replacement/packet/raid-info boundary tests passed 40/40 on 2026-06-07 from the alternate output directory
- Removed the unsupported replacement anchor queue/merge runtime; addon and sender
  evidence prove the UI/event boundary, not server producer timing or merge rules
- `ServerRaidQueueStatus` (0x0718) emitted alongside `ServerRaidInfoResponse` as zero-value compatibility state; shared row fields are mapped through `0x071A` / `Group_DispatchRaidInfoResponse`, but standalone non-zero queue timing and queue-only aliases remain blocked. Pass 157 direct-plugin/cache recheck found no exact `0x0718` send-helper hit, no code caller into the row reader except `ServerRaidInfoResponse_ReadPayload` (`14008c010`), and no owner for the extra data refs.
- `ServerMatching0x05CF` remains a raw `uint32` packet only: reader `140099110`
  is mapped, and `MatchingManager_ApplyManagerUInt32Field0xA0` (`1405c41c0`)
  is only a correlated apply candidate until a real apply dispatcher/index or
  live packet witness is proven. The prior `140e1e66c` xref is PE `.pdata`
  unwind metadata, not a matching dispatch-table cell; the `WorldSocket+0x15b0`
  slot-11 scan found the Fortune `vtable+0x58` positive control but no
  `1405c41c0` match. The 2026-06-05 MCP bridge retry still returns
  `Transport closed`; cache/direct-plugin evidence confirms the candidate apply
  helper only touches broad manager globals `140c65b98` / `140c65898`, while
  `ServerUInt32_LocalReadThunk` refs resolve to the shared `0x05CF` and `0x085D`
  registration rows, not an apply dispatcher.
- Shared one-flag matching outputs `0x05B0`/`0x05CC`/`0x05F1` remain wire-mapped
  only. `MatchingManager_ApplyMatchingRoleCheckStarted` (`1405c0e90`) consumes
  `payload[0]` and dispatches `MatchingRoleCheckStarted`, but its only direct
  ref is `140e1e2c4` in PE `.pdata`; the `WorldSocket+0x15b0` slot-11 scan
  found the Fortune `vtable+0x58` positive control but no matching-helper entry
  for `1405c0e90`, `1405c1850`, `1405c21d0`, `1405c2440`, or `1405c24a0`.
  The adjacent zero-payload matching event helpers likewise only have `.pdata`
  xrefs.
- `Client0x062A`/`Client0x0634` remain numeric diagnostic packets; focused
  handler tests now pin both as log-only with no queue/raid-state emit until a
  native sender or live sniff proves their semantics
- Matching leave/cleanup lifecycle hardening now snapshots queue collections and
  match-team members before removal mutates the underlying dictionaries; focused
  matching tests passed 90/90.

**Ghidra decomp confirmed:** All 6 group reader functions decompiled.
9 of 10 unresolved opcodes already correctly wired:
- 0x0414 ServerGroupInstanceDifficultyResponse [ok]
- 0x042A ServerGroupKickResult [ok]
- 0x0431 ServerGroupLootRuleValidationResult [ok]
- 0x0436 ServerGroupRosterUpdate [ok]
- 0x0438 ServerGroupIdentityListAndUInt32Array [ok; producer semantics provisional]
- 0x0441 ServerGroupReadyCheckStatusUpdate [ok]
- 0x045A ServerGroupRequestJoinWindow [ok]
- 0x0461 ServerQuestShareResult [ok]
- 0x0468 ServerGroupTargetIdentityPrimeLevelList [ok; producer semantics blocked, stale stat/detail emit removed]

**Blocked:** exact standalone `ServerRaidQueueStatus` queue-position semantics
and non-zero timing (shared row fields are mapped through `0x071A`, but pass
157 found no cached static sender beyond registration; direct-plugin xrefs keep
`14008c010` as the only code caller into `14008bf80`, while candidate data refs
are unowned pointer metadata or PE `.pdata`-shaped runtime entries rather than a
queue producer/apply owner);
replacement queue fill still needs live accept/teleport smoke and multi-slot
role-fill verification; `ServerMatching0x05CF` still needs either a real
matching apply dispatcher/index that ties opcode `0x05CF` to `1405c41c0` or a
live match-ready / queue-state sniff that explains manager field `+0xa0`;
`0x05B0`/`0x05CC`/`0x05F1` still need a real dispatcher or live role-check /
queue capture before their single flag can be treated as semantic evidence
because the known `WorldSocket+0x15b0` slot-11 path does not expose the matching
helpers;
`Client0x062A`/`0634`
still need a native sender or live sniff before semantic rename or mutation.

### F-011 - Guild / Recruitment / War Party - PARTIAL (blocked)
Guild handlers and recruitment output models exist. `ServerGuildBankTabInventory`
(`0x047A`) now emits the native-mapped row count before item rows. Guild bank
money/item transaction and tab-open handlers are test-pinned as log-only/no-emit
request surfaces until bank economy and tab state are modeled. Bank
transactions, perks, holomarks, standards, recruitment subscriptions,
boss-token inventory, warplot plug state incomplete. Needs decomp.
The 2026-06-06 live Guild smoke verified server-side guild membership setup
(`ClientChat` command -> `ServerGuildResult`/`Join`/`Roster`/`MemberChange`,
DB guild `Nexus`/character `Joy Ner`). The widened elevated Guild UI CDB pass
then captured live client-owned `ClientGuildBankMoneyTransaction` (`0x04A8`)
deposit sends and a `ClientGuildOperation` (`0x04B1`) bank-management/log send.
Guild/community registration now fail-closes missing create-cost `GameFormula`
tables through `GuildResult.UnableToProcess` before currency checks, currency
debits, or guild creation; focused guild-register coverage passed 5/5 and
broader guild coverage passed 21/21.
Guild standard construction and validation now also treat missing
`GuildStandardPart` and `DyeColorRamp` tables like missing rows, preserving the
existing invalid-standard-part exception and invalid dye validation result before
guild standard registration can proceed.
Buy-tab proof remains blocked because current guild state advertises influence
`0` and does not yet model/persist guild influence or bank-tab count.
The nearby `0x077E` packet contract is now mapped as a counted uint32 list
rather than a fixed four-field payload; recruitment/pet producer semantics
remain blocked.
A 2026-06-09 cached-export/source recheck kept the guild recruitment packet
family source-aligned but producer-blocked. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `ClientRecruitmentGuildGetDetailedGuildInfo` (`0x076E`)
size `0x10` at `selected_decompiled.c:13324` with the shared identity
reader/writer shape, while `RecruitmentGuild_SendClientGetDetailedGuildInfo`
(`140584840`) is called by
`Lua_GameRecruitmentGuild_GetDetailedGuildInfo` (`14069e5c0`) and sends
`0x076E` through `Network_SendOpcodePayloadOrPackedHelper` (`1403f4740`) using
the current realm id and selected recruitment guild identity. Adjacent
recruitment server registrations at `selected_decompiled.c:13369-13377` map
`0x049F` as identity + identity + `uint32`, `0x0767` as counted guild rows with
recruiter names, `0x0768` as identity + 12 demand bytes, `0x0769` as identity +
description string, `0x076A` as identity + description + age scalar + tax bit +
minimum level + demands, `0x076B` as counted guild rows without recruiters,
`0x076C` as identity + `uint32`, and `0x076D` as counted recruiter rows with
name arrays plus online flags. Current source has matching packet models and
packet-shape tests, but recruitment detail/subscribe handlers remain log-only
and no selected native/server evidence proves list/detail population, subscribe
timing, persistence, or availability semantics.
A second 2026-06-09 cached-export/source recheck source-aligned the war-party
boss-token packet boundary while keeping boss-token runtime state blocked.
`Network_RegisterServerOpcode_0351` (`14006c290`) registers
`ClientWarPartyBossTokensRequest` (`0x0956`) size `0x10` at
`selected_decompiled.c:13334` with the shared guild identity shape,
`ClientCastGuildBossToken` (`0x094F`) size `0x18` at
`selected_decompiled.c:13336` to writer `140091a80`, and
`ServerWarPartyBossTokens` (`0x0951`) size `0x20` at
`selected_decompiled.c:13363` to reader `140092990`. `FUN_14057ef20` sends
`0x0956` through `Network_SendOpcodePayloadHelper` when type-3 guild boss-token
state is absent and otherwise dispatches `WarPartyBossTokensUpdated`;
`140092990` consumes guild identity, row count, and 18-bit token-item /
`uint32` count rows; `GuildBossToken_SendClientCastGuildBossToken`
(`1403991b0`) scans type-3 boss-token entries, validates the spell target
context, then sends `0x094F` through `Network_SendOpcodePayloadHelper` with
guild identity, token item id, and context token. Current source now reads the
`ClientCastGuildBossToken.Item2Id` field at the mapped 18-bit width and keeps
war-party token list handling conservative: `ClientWarPartyBossTokensRequest`
returns an empty token list, and boss-token casts return `BossTokenNotReady`.
Real boss-token inventory, cast acceptance/results, match-results population,
and warplot plug state remain blocked.

### F-012 - ICComm / Chat / Friendship - PARTIAL (blocked)
Transient membership, offline cleanup, directed/ordered delivery. Entitlement
checks, persistent channels, throttling, auto-response persistence incomplete.
Needs decomp.
Group/guild ICComm transient channels now revalidate scoped affiliation on send
and update, removing stale senders/recipients before delivery; focused ICComm
tests plus friendship social-option and chat aux shape coverage passed 12/12 on
2026-06-07.
Chat aux packet contracts are now mapped and typed for `0x01B8`, `0x01C1`,
`0x01C4`, and `0x01EF`. A 2026-06-09 cached-export/source recheck kept this
cluster mapped-only: cached `WildStar64.exe` fragments under
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
reconfirm `Network_RegisterServerOpcode_0351` (`14006c290`) binding `0x01B8`
to row writer/reader `140085af0`/`140085ca0`, `0x01C4` to envelope
writer/reader `140085e30`/`140085fe0`, `0x01C1` to `1400861b0`/`140086410`,
and `0x01EF` to reader-only `1400a0890`. Selected non-row callers
`140086520` and `140086600` are local wrapper helpers and do not prove runtime
ownership. Source still contains these packets only in
`ServerClusterAuxPackets.cs` and packet-shape tests. Chat/cinematic producer
semantics and runtime emit conditions remain blocked pending a native
producer/apply path, callback/table owner, dynamic dispatch proof, or accepted
packet capture.
Chat item/quest link construction now treats missing `Item` and `Quest2`
tables like missing rows, throwing the existing invalid link exception before
appending chat text or format rows. Focused `ChatMessageBuilderTests` passed
6/6, and the broader chat bucket passed 11/11 from the alternate output
directory on 2026-06-08.

### F-013 - Mail - COMPLETE
Cash collection, return, attachment deletion, delayed pending-mail promotion,
COD sender settlement, delete result/unavailable signaling, expiration sweep.
Returns now require a real player/GM sender id before moving mail to outgoing;
system/marketplace/zero-sender mail returns report `MailCannotReturn` without
mutating the available mail item, and `MailItem.ReturnMail` enforces the same
invariant.
Marketplace item-delivery fallback is now gated on mail service availability so
auction/commodity state remains uncommitted when neither inventory nor mail can
receive the item; marketplace mail content type now persists across reload via
`character_mail.contentType`; marketplace mail save failures restore attached
item owner state before reporting delivery failure; auction return/won mail and
commodity return/fill mail can compose marketplace row mutations with the mail
save; final marketplace DB transaction/save parity remains under F-005.
Known retail gaps remain for broader delete-state parity, exact expiration
timing, VIP/trial social gates, and offline marketplace currency transaction
atomicity.

### F-014 - Loot - COMPLETE
Basic delivery, group roll/master-loot runtime, roll/assign/result packets,
`ServerLootNotify` ingestion, `ServerLootBindOnPickup`, `ServerLootWinner`.
Pass 160 labels the native `ServerLootWinner_ReadPayload` reader (`1400a4e50`),
registered for `0x08A3`, matching the existing packet model and
`Loot_HandleLootWinner` consumer (`1403db610`).
`LootInstanceResolutionTests` now pin runtime `ServerLootItemUpdate` refresh
packets after roll/finalise/master/deferred delivery plus remote
`ServerLootNotification` feedback. `GlobalLootManager` also rejects non-looter
collect/roll/master-assignment requests before the lower-level `LootInstance`
invariant calls, closing the handler-facing D-L7 crash guard. Corpse group
loot now keeps group context for same-map multi-member groups while filtering
tracked looters, eligible recipients, and master-loot candidates to players
within `LOOT_RANGE`; out-of-range same-map members no longer receive unusable
loot UI.
**Improved this pass:** Loot expiry/cleanup in GlobalLootManager + LootInstance;
account-currency granted notifies now use one real reward row instead of split
fake shower rows, generated granted-notify rewards now batch all actual
delivered rows into one explosion notify, delivered account-currency loot emits
a local generic floater plus a loot-channel chat line for the actual amount and
currency granted,
delivered character-currency/cash loot emits the same readable floater and
loot-channel chat feedback while preserving the existing currency update path,
delivered static-item loot emits a loot-channel item-link chat line to the
winning looter,
generated crafting loot can carry the crafting station as a distinct
`ParentUnitId` visual source, and runtime tests cover distinct parent propagation
plus quality-preserving granted static, virtual, and account-item drop
presentation; loot inspect/capture diagnostics now include the table-backed loot
visual effect id for the resolved quality. Single-stack no-charge loot bags now
catch source-item delete failures and return `item-delete-failed` before
generated reward delivery. Generated loot preflight now treats missing
`AccountCurrencyType`, `AccountItem`, and `VirtualItem` static tables like
missing reward rows, returning `invalid-loot-item` before source-item
consumption or generated reward delivery. Direct character-currency manager
requests now also treat missing `CurrencyType` static tables like missing
currency rows, throwing the existing invalid static-currency exception before
affordability success, character-currency mutation, or currency update packets.
Creature DropLoot now treats unavailable `Creature2` static data like a missing
creature row, returning false before recipient selection, loot-instance
creation, or loot notify emission.
Imported direct `creature_loot` rows
now coexist with old-table quest-only loot groups such as the LaughingWS
`LaughingWS quest loot:` overlay; direct imports are skipped only when a
DataMapping flat `DataMapping creature_loot` group already represents the same
creature, avoiding duplicate broad drops while preserving quest-objective
virtual-item loot.

### F-015 - PvP / Duels - COMPLETE
**Fixed this pass:** Duel leash/distance system added.
- `DuelLeashRange = 120f` - checks distance each update
- `DuelLeashTimeout = 15s` - auto-cancels duel
- `ServerDuelLeftArea` sent when out of range
- `ServerDuelCancelWarning` sent on re-entry
Cancelled and declined duel results now emit zero winner/loser unit ids, and
active player disconnects are routed through `DuelManager.OnPlayerDisconnect`
from `WorldSession.OnDisconnect`; focused PvP tests pin both boundaries.

PvP toggle-off now starts a player-owned pending cooldown with
`ServerPvpCooldownUpdate` and `RetailCertainRules.PvpFlagCooldownMs`, keeping
`PvPFlag.Enabled` active until the timer expires and then sending
`ServerPvpCooldownClear`. Pending toggle-off cooldowns now persist through
`character.pvpFlagDisableUntilUtc`, reload as `PvPFlag.Enabled` with the
remaining timer, resend `ServerPvpCooldownUpdate` on login, and clear the
stored expiry on cancel or expiration; focused PvP cooldown coverage is 14/14.
Native pass 135 confirms `0x08BD` client state apply via
`ServerUnitPvpStateChange_ReadPayload` (`140098160`) and
`Entity_ApplyUnitPvpFlagsChanged` (`1403ddc60`) / `UnitPvpFlagsChanged`, while
the standalone `0x013E` cooldown consumer remains blocked behind apply-owner or
live UI evidence.
Open-world player-vs-player attackability remains deliberately active-duel-only
through `Player.CanAttack` and `DuelManager.AreDueling`; instanced PvP match
combat is a separate content-map/match path. Broader non-duel open-world PvP
rules remain blocked pending retail proof. The 2026-06-07 source recheck kept
this row mapped-only outside the implemented duel/cooldown core: focused
`DuelManagerTests`, `PvpPacketShapeTests`, `PvpFlagCooldownTests`,
`PvpCombatBoundaryTests`, and `PvpAdventureBranchScriptTests` passed 29/29 from
the alternate output directory, while observer broadcasts, forced-map PvP,
arena/battleground scoring, stat, and reward parity still require native/live
producer proof or completed queue/match smoke bundles.

**Cryo-Plex branch harvest:** arena map/event/sub-event IDs from the
LaughingWS branch are now wired using the existing Slaughterdome arena runtime
pattern: forcefield door release on match start, death stat updates, timed
holocrypt resurrection, and team-spawn reset. The active Cryo-Plex behavior is
now marked WIP-guessed in code and covered by focused PvP/adventure branch
tests. Manual queue/match smoke is still needed before calling Cryo-Plex
retail-complete.
Branch-derived Daggerstone Pass (`438`/`466`, world `2166`), Halls of the
Bloodsworn (`876`/`877`, world `3449`), and Walatiki Temple (`217`/`366`,
world `797`) map bindings are also present so the existing PvP content-map base
can create, join, and finish their public events; the bindings are marked
WIP-guessed in code, and mode-specific scoring/round/capture objectives still
need proof before behavior work.

### F-016-F-020 - Spell Runtime - PARTIAL (family-by-family)
Damage/heal/shields/vitals, stack groups/buffs/CC/movement, procs, summons,
RavelSignal. Many families conservative or partial. Work family-by-family
with fixture captures. Most blocked on decomp.
**Spell wrapper packet naming pass:** `ServerSpellWrapperTierEntry` (`0x0818`)
now names its leading `uint32` `SpellWrapperId`, matching dispatcher
`1403ee403 -> SpellService_LookupSpellWrapperByWrapperId ->
SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast`. Wire shape is unchanged;
production emission remains blocked pending wrapper lifecycle sniff evidence
and selector-tail semantic correlation.
The 2026-06-09 cached-export/source recheck keeps the paired wrapper follow-ups
mapped at client-consumer scope only. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `0x0819` size `8` at `selected_decompiled.c:13429` to
shared `ServerTwoUInt32_ReadPayload` (`14007a040`) and `0x0818` size `0x20` at
`selected_decompiled.c:13430` to
`ServerOpcode0818_ReadSpellWrapperIdAndTierEntry` (`140095e60`). The `0x0818`
reader consumes `SpellWrapperId` then the shared tier-entry helper, while
tracked dispatcher labels map `0x0818` case `1403ee403` through
`SpellService_LookupSpellWrapperByWrapperId` (`140561c30`) into
`SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast` (`14053f710`) and map
`0x0819` case `1403ee3af` into `SpellWrapperNode_PruneChildrenAndRefresh`
(`140718af0`) with payload field zero as the wrapper-node entity id and field
one as the spell-wrapper id. Selected xrefs for the readers/apply helpers remain
label-only or reader/apply-local, and current source finds both packets only in
models, packet-shape tests, and negative tutorial guards, so production emission
remains blocked pending a wrapper lifecycle capture, native producer timing
proof, or selector-tail semantic correlation.
**Branch trap/spell ports:** Bramble trap (`27768`) now follows the useful
branch behavior: failed activation casts penalty spell `46051`; successful
activation removes tracked state from that spell and destroys the trap.
`MarauderMineEntityScript` ports the branch disarm activation for mine creatures
`16718`/`24251` by casting detonation spell `26443` at the activator, and
`MarauderMineExplosionSpellScript` destroys the mine only after that detonation
finishes without cancellation. `EngineerPulseBlastSpellScript` ports the branch
hidden Volatility proc for the current Pulse Blast damage spell tiers
(`37302`, `56145`..`56152`) by casting hidden spell `42148` when selected
targets include a hostile unit; client spell/effect map rows corroborate the
Pulse Blast tooltip and hidden `+15 Volatility` effect. The supporting
`ISpellScript` hooks are deliberately narrow (`OnCast`, `OnExecute`,
`OnFinish`); generic branch `SpellParameters.CompleteAction`,
`currentPhase`/phase callbacks, and broad spell-script semantics remain blocked
pending stronger runtime evidence.
**2026-06-08 F-017 Pulse Blast live proof:** bundle
`artifacts\blocker_evidence\20260608-225224-F016-spell-proc-loot-live`
closes the player-originated Pulse Blast damage fixture for emulator parity.
The structured `!spell capture4 37302` artifact
`artifacts\verify\spell-evidence\20260608-205817660-spell4-37302...json`
shows spell `37302` / base `22522`, `CastResult=Ok`, three selected targets,
three damage dispatches, six effect results, shield deltas, and
`ServerSpellGo` combat-log output. The later real client action-bar proof in
`NexusForever.WorldServer_20260608_57172.log` shows
`ClientCastSpellContinuous(0x4DB)` driving `42276` / base `26468` into proxy
`37302`, selecting hostile target `179`, dispatching effects `79090`, `79092`,
and `79093`, and emitting `spell-go` combat-log rows. No new structured JSON
was exported for the client cast because `ClientCastSpellContinuousHandler`
bypasses `ClientSpellEvidenceCaptureHelper`; that is evidence-tooling debt, not
a Pulse Blast runtime blocker. Proc post-registration triggers, uncaptured
effect families, and broader retail spell parity remain family-by-family
blocked.
**2026-06-09 F-016 Brutal proc live proof:** the same evidence bundle now has
server-log proof for post-registration proc dispatch. `!spell capture4 4046`
registered Brutal on holder/source `334` with trigger event `12`
(`deal-damage`), trigger spell `4047`, chance `0.15`, targetData `4`, and route
`counterpart`. Real tutorial combat from Creature2 `73464` holder/source `334`
into player `294` produced matching `proc-probe` rows for `damage-dealt` and
`deal-damage`, expected `chance-roll-failed` controls, reentrant guards during
triggered spell recursion, and successful `proc-dispatch ...
action=cast-trigger-spell` rows for `4047`. The fresh `procreport` JSON exports
for holders `1` and `294` are empty because the selected/invoker holder was not
proc holder `334`; that is an artifact-selection caveat, not a runtime proc
blocker. Broader unsupported trigger events, targetData tails, exact retail
chance/cooldown ordering, and school-specific proc semantics remain
family-by-family partial.
**AI movement this pass:** Rider's Reef `CombatAI` now resolves spell/range
profiles through `ICombatProfileProvider` with a layered combat-kit catalog:
`AI/CombatProfiles.json` carries only the default/manual overrides, while the
reviewed `AI/CombatKits.json` asset maps shared Rider's Reef and Northern Wilds
kits to combat-tagged Creature2 rows. The provider reports manual, reviewed-kit,
action-derived, ignored-rule, and rejected-rule counts; active
`Creature2Action` rules must be exact/evidence-labelled before they can resolve
combat, so the current empty rule asset keeps sampled rows diagnostic-only. It
also exposes detailed creature/action audit rows consumed by
`Tools/CombatProfileAudit` for markdown/CSV review queues, and adds bounded
same-faction social assist with focused combat tests. Profiles
can now define cooldown-gated special attacks that cast
ordinary `Spell4` rows through the existing spell runtime while honoring
primary-target minimum range, hit-radius-aware effective maximum range, and
vertical range; failed spell runtime starts do not consume the AI cooldown, and
non-instant `CastTime` rows stop chase movement/auto-attacks during the windup,
and `CCState.Interrupt` cancels active casts and clears the AI windup lockout so
profiled NPCs can resume after a proven interrupt. Ready special attacks now
rotate through deterministic weighted slots with a small per-creature cooldown
offset so identical packs do not always fire the first ready skill in lockstep.
This allows profiled NPC
windups/telegraphs when the chosen spell data supports them. Patrolling
creatures now walk back to leash before relaunching their data-backed patrol
spline, including the existing reverse-speed spline normalisation. Server follow
movement now applies a small deterministic
follower-GUID spread around known targets, so multiple chasers choose distinct
final points at the requested distance instead of collapsing onto one endpoint;
this is emulator-side local avoidance polish, not retail parity proof.

**Spell cast result reader pass (2026-06-05):** `ServerSpellCastResult`
(`0x07FC`) now has a durable native reader label:
`ServerSpellCastResult_ReadPayload` (`WildStar64.exe` `0x140094fb0`) reads a
leading `uint32`, `18`-bit `Spell4Id`, and `9`-bit `CastResult`. The leading
managed `Unknown0` field remains neutral because current evidence is
registration/reader-only; client request context-token mapping is not enough to
prove result echo semantics without an apply consumer or live echo capture. A
same-day pass 132 MCP/cache recheck still found no apply owner: the MCP bridge
could not connect to the open project, and cached call edges for `140094fb0`
only show internal bit-reader calls. `PacketPlaceholderNamingTests` now guards
against `ContextToken`, `ClientContextToken`, and `CastingId` aliases.
Pass 149 direct-plugin/source recheck kept that blocker in place: direct xrefs
to `140094fb0` remain data/registration-only, and managed producers are mixed
between default-zero sends and the support stuck context-token echo.

**Spell auxiliary triplet/four-uint32 pass (2026-06-05):** MCP retry still
failed (`list_instances`, `connect_instance`, and `list_tool_groups` all
returned `Transport closed`), so the live Ghidra plugin endpoint was used for
focused xrefs. `ServerSpellUInt32TripletList_ReadPayload` (`0x140095da0`,
opcodes `0x080F` / `0x0810`) has only data/registration xrefs
(`0x140dcf718`, `0x1400747f1`, `0x140074803`, `0x140074822`, `0x140074834`);
the shared row reader `0x140080bf0` proves only three `uint32` fields, and
`ServerSpellFourUInt32_ReadPayload` (`0x14007fef0`, opcode `0x0812`) remains
registration/data-only for spell-aux use. NexusForever has models/tests but no
runtime producers for these spell-aux packets, so `Value*` field names and emit
paths remain blocked pending an apply owner or live correlated rows.

**Spell aux cached-export/source recheck (2026-06-09):** existing cached
`WildStar64.exe` fragments under
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
were sufficient, so no Ghidra refresh or label change was needed.
`Network_RegisterServerOpcode_0351` (`14006c290`) still registers `0x07FC`
size `0x0c` to `ServerSpellCastResult_ReadPayload` (`140094fb0`: `uint32`,
18-bit `Spell4Id`, 9-bit `CastResult`), `0x080F`/`0x0810` size `0x10` to
`ServerSpellUInt32TripletList_ReadPayload` (`140095da0`: `uint32` count plus
`ServerSpellUInt32TripletListRow_ReadPayload` `140080bf0` triplet rows), and
`0x0812` size `0x10` to `ServerSpellFourUInt32_ReadPayload` (`14007fef0`:
four `uint32` fields). Selected xrefs remain label-only and selected call edges
only show bit-reader/allocation/row-reader calls; the row reader is also shared
by `0x07F5`, `0x07F7`, `0x0889`, `0x0908`, and `0x090A`, while the four-uint
reader is shared with housing/community `0x078C`. Current source still has
mixed `ServerSpellCastResult` emitters but no proof for renaming `Unknown0`,
and source search finds `ServerSpellUInt32TripletList`,
`ServerSpellUInt32TripletListVariant`, and `ServerSpellFourUInt32` only in
models/tests. Keep this cluster mapped-only pending a native apply/producer
path, callback/table owner, dynamic dispatch proof, or accepted spell packet
capture.

### F-021 - Action Set / LAS / AMP / Attributes - PARTIAL (blocked)
LAS size/spec/tier/AMP preflight checks exist. `UpdateSpellInProgress`,
async spell update transaction, authoritative attribute allocation,
action-bar lock state incomplete. Needs decomp.
`ClientSetStanceHandler` now treats a missing `Class` table like a missing class
row, throwing before player innate-state mutation or `ServerStanceChanged`
emission while preserving the table-backed stance-change path.
`ClientRequestActionSetChangesHandler` now treats missing `Spell4` and
`EldanAugmentation` tables like missing rows, returning the existing
`UnknownSpellId` or `EldanAugmentationInvalidId` result before save/cache or
AMP mutation.
`ClientCommitAmpSpecHandler` now uses injected game-table data for AMP
validation, treats a missing `EldanAugmentation` table like an invalid AMP id,
and commits validated AMP entries without a second singleton lookup.
`ClientRespecAmpsHandler` now treats missing `EldanAugmentationCategory` and
`EldanAugmentation` tables like missing rows, returning the existing invalid
category/id results before AMP mutation or refresh emits.
Direct and persisted `ActionSet.AddAmp` materialization now also treats a
missing `EldanAugmentation` table like a missing AMP row, preserving the
existing invalid-AMP exception before owner-save or AMP-list mutation.

### F-022 - Quests / Path Missions / Public Events - PARTIAL
**Coverage audit:** see `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`
(5,194 client quests; no current unsupported objective-type blockers in client
data; 56 hand-curated script rows).
Branch-derived `Q10510LearningToShopQuestScript` now has focused coverage for
the Smart Shopper item/title objective snapshot; exact retail refresh timing is
still marked WIP-guessed in code.
Q5573/Q5604 cinematic-complete objective helpers are covered and explicitly
marked WIP-guessed for exact cinematic completion timing.
Q5584 trapped-assistant and Q5594 ship-control branch interactions are also
covered with focused tests, with quest/teleport activation gating explicitly
marked WIP-guessed in code until live retail smoke proves the access rules.
Q8855 Dominion soldier proximity objective credit/despawn is now explicitly
marked WIP-guessed in code; focused tests already pin accepted/missing gates and
range registration.
Q3486/Q3673 Northern Wilds cinematic/mention/reward helpers now mark branch
timing as WIP-guessed where un-smoked, and Q3486 Loftite Crystal range,
virtual-item reward, despawn, and no-op gates have focused coverage.
Q3487 cannon/Ultrabot and Q3667 control-panel helpers now mark branch-derived
proximity/checklist/path/achievement timing as WIP-guessed, with focused tests
for accepted/missing gates, Ultrabot movement, and achievement duplicate guard.
PathManager now ports the branch current-zone PathEpisode activation as
WIP-guessed table-backed runtime behavior on player zone updates: it matches
current world/root zone/path, filters mission rows by faction and optional
`PrerequisiteId`, and activates the episode without claiming durable path
persistence, exact XP/rewards, or exact unlock sequencing. Explorer ExploreZone
missions now complete only when already active and the player's current
`WorldZone` resolves to a matching `MapZone.Id` (or the world-level
`MapZoneWorldJoin` fallback); this is WIP-guessed from the branch `ObjectId ==
MapZone.Id` mapping. Explorer power-map progress reports now
complete only active Explorer type-`0x12` missions whose `ObjectId` has a
matching `PathExplorerPowerMap` row; ready/active timers, failure packets,
server-owned progress cadence, and durable completion state remain blocked.
Path mission completion now treats missing `PathMission` static data like
missing mission rows: direct completion can still complete and grant configured
mission rewards without achievement/XP metadata, while Explorer vista and
power-map helpers return no-progress. Explorer vista completion also treats a
missing `PathExplorerNode` table like no matching node rows, and power-map
completion treats a missing `PathExplorerPowerMap` table like a missing
power-map row. Focused PathManager coverage passed 72/72 and broader path
coverage passed 163/163.
Active path helper paths now also treat missing `PathMission`,
`PathSoldierTowerDefense`, `PathSoldierAssassinate`,
`PathSettlerImprovementGroup`, and `PathSettlerHub` static data like missing
rows. Explorer explore-zone completion, active object-id completion, Soldier
tower/assassinate progress, Settler hub improvement progress, and Settler hub
progress percent helpers return no-progress/zero-progress instead of throwing
when those tables are unavailable. Focused PathManager coverage passed 81/81
and broader path coverage passed 172/172.
Soldier_Assassinate missions now progress from creature kill rewards when the
killed Creature2 id or associated target group matches `PathSoldierAssassinate`;
decompile proof maps Soldier_Assassinate current progress to the first mission
payload now named `ProgressCount`, while alternate progress payload semantics
remain blocked per mission type.
Settler_Hub missions now progress from accepted build-tier requests when the
improvement group maps to the active mission's `PathSettlerHub`, using
`PathSettlerHub.MissionCount` as the contribution target; decompile proof maps
Settler_Hub current progress to the first mission payload now named
`ProgressCount`. Durable built-group state, resource costs, avenue totals,
failure/result precision, and alternate progress payload semantics remain
blocked.
Settler build status packet shape is now mapped for the safe emitted surface:
`ServerPathSettlerBuildStatus` (`0x0671`) reads `PathSettlerHubId` plus one
status row; `ServerPathSettlerBuildStatusList` (`0x066E`) reads hub id, count,
and counted rows; each status row is improvement-group id, tier, remaining time,
and bundle count. `ServerPathSettlerBuildResult` (`0x066C`) now names its second
field `PathSettlerImprovementId`. `PathSettlerBuildHandlerTests` now pin the
safe no-emit boundary for out-of-range improvement group/hub ids, unmapped build
tiers, zero improvement ids, and improvement ids outside the mapped 15-bit
result field; invalid requests still route only through the guarded mission
completion surface and do not invent failure packets. Non-success result values,
durable built-group ownership, resource costs, and failure ordering remain
blocked.
Soldier holdout client accessors are mapped further: `Game.SoldierEvent` now has
labels for type/state, health, elapsed/max time, wave count, waves released, and
defend/auxiliary/escaping unit list accessors. These accessors read a client
runtime holdout object re-resolved by event id, so generic server holdout status
and wave runtime remain blocked pending packet/live capture or packet-consumer
mapping. The current Soldier holdout status, next-wave, end, and death packet
models now have source comments at that boundary and focused serialization
coverage; `PacketPlaceholderNamingTests` passed `51/51`, and the
network-world build passed after pinning those packet shapes.
Path mission runtime state now has a narrow character-owned persistence model:
`character_path_mission` records mission id, episode id, state, completion,
progress payloads, and XP; `PathManager` restores completed mission state and
replays unfinished active episodes during initial path packets. Sequencing
regressions now cover completed-mission no-replay and persisted-active
no-duplicate zone activation. Settler_Hub build-count mission progress also
survives reload through this model, so a partially progressed hub can complete
after the next accepted build-tier request. Active path object-id state,
reward-history persistence, exact replay ordering, full Settler built-group/
resource/avenue backing state, and broader faction/zone/reload negatives remain
blocked pending stronger proof. Persisted active path mission replay is now
guarded by the current active path and table-backed mission/episode/faction/
prerequisite checks, so different-path, missing-episode, and mismatched
episode/path persisted missions no longer replay on login or suppress
current-path zone activation for the same episode id. Focused path-manager
coverage now also pins wrong-faction, unmet simple path-prerequisite,
missing-episode, and mismatched-episode-path persisted replay rejection, with
safe constructor-time evaluation before `player.PathManager` is assigned.
Settler hub rejection coverage now pins zero/missing improvement groups, missing
hubs, different hub ids, and non-Settler active paths without extra mission
updates; focused coverage is `49/49`.
Known path mission completions now also fire `AchievementType.PathMission` with
the mission id and `AchievementType.PathMissionType` with the mission row's
`PathMissionTypeEnum`; total/count-style mission achievement semantics remain
blocked. Known active-path mission completions with no stronger configured XP now
use client-mapped `GameFormula` `0x017a` (`378`) `Dataint0`, with the client
fallback `50` if the row is unavailable; missing `PathLevel` rows now skip XP
and level-reward mutation without aborting mission completion or supported
mission rewards, and missing target level rows make `AddLevels` fail closed.
Path unlock/change requests now also guard missing `GameFormula` rows `2365`
and `2366`: the handlers send `GenericError.ItemBadStaticData` through the
existing path result packet and return before service-token checks/debits,
unlock mutation, or path activation.
Exact per-mission path reward precision remains blocked. Mismatched
mission/path rows do not receive the fallback, and object-id mission completion
is now limited to active-path mission rows. Unflagged
`PathRewardType.Mission` rows keyed by completed mission id now grant the same
supported reward payloads as level rewards (item, spell, title, and scanbot
profile); item rewards use `PathReward.Count` as a WIP-guessed stack count with
zero falling back to one, and focused reward/path coverage now passes `74/74`.
Exact reward presentation, overflow behavior, reward history, and retail
flagged reward semantics still remain blocked. The last full current game test
project pass recorded in this file is now `2790/2790`.
Public-event vote runtime is now hardened inside the existing proven vote path:
client vote opcode `0x06EE` remains mapped as event id, vote id, team id, and
choice, while `PublicEventVote.Choice` ignores finalised, non-participant,
duplicate, and invalid responses. Focused lifecycle tests cover detailed vote
initiate, tally/end, timeout default choice, and negative responses. Packet
tests pin client vote and server initiate/detailed-initiate/tally/end shapes;
handler/runtime tests now pin forwarding and validation of the full mapped
event/vote/team/choice envelope so stale vote ids and cross-team replies are
ignored before vote state changes. Retail UI sequence, exact timeout cadence,
mismatch feedback, tally/end ordering, and default-choice semantics remain
blocked pending capture evidence. The blocker harness now has
`-PublicEventVoteScoreboardSmoke` for the missing vote/scoreboard packet/UI
proof.
Public-event scoreboard/end packet shapes are now decompile-backed for the safe
model boundary: scoreboard request `0x06FA` carries public-event id plus
subscribe flag, stats update `0x0130` carries counted team/participant rows,
personal stat `0x013B` carries event/stat/value, and end `0x00D6` carries
personal/team/participant/objective rows plus reward tier/type/threshold fields.
Scoreboard subscribe requests now emit one current participant-scoped stats
snapshot for the requested event, and team stat rows aggregate each member's
latest absolute stat value instead of using the last member update. The
unsubscribe no-op test now also proves the handler returns before map/event
lookup, so it does not emit snapshots or create an implied subscription state.
Reward delivery, exact live subscription cadence, score-field precision, result
ordering, and reward tier semantics remain blocked pending content smoke/sniff
evidence.
Public-event objective packet shapes now also have a decompile-backed safe
boundary: objective notification mode `0x0133` carries objective id plus
notification mode, objective status update `0x0134` carries objective id plus a
status row, objective update `0x0132` carries status/busy/elapsed/notification
mode plus counted location and map-region rows, and objective start `0x06F9`
uses the shared 15-bit scalar reader. Exact objective/update text,
phase/completion notification audience and ordering, and quest-share behavior
remain blocked pending capture evidence. The current `0x0132` producer now
suppresses duplicate full-objective updates for unchanged busy-state calls,
with focused objective coverage for that no-duplicate boundary; this is runtime
hardening only, not notification parity.
Public-event trigger objective producers are hardened at the current mapped
boundary: `ParticipantsInTriggerVolume` and `Turnstile` are tied to the client
Lua public-event constants and now ignore zero object ids and non-player
entities. Focused trigger tests cover volume enter/leave deltas and turnstile
entry, duplicate in-range suppression, no turnstile leave/decrement emit, plus
world-location horizontal radius and vertical clamp behavior. Exact branch
trigger rows, coordinates, door ids/states, cleanup timing, and
respawn/despawn behavior remain blocked pending content proof.
Communicator/story-panel packet models now have safe packet-shape coverage for
`ServerCommunicatorMessage`, `ServerStoryTextCommunicator`, and
`ServerStoryPanelCustomShow`, with comments tied to the client
`CommunicatorMessages` and `StoryPanel` table/export anchors. Shared
`StoryMessage` actor rows are also packet-covered for creature, custom text,
localized text, player, creature-unit, and self-player token sources. Real
actor/camera/text timing, send conditions, skip behavior, and replacement of
completion-only cinematic placeholders remain blocked pending capture/decompile
proof.
**NorthernWilds Veteran pass updated this pass.** Core quest/objective,
public-event, challenge, path packet, and opt-in spawn-promotion surfaces exist,
but full 164-NPC parity is still blocked by 3 enabled Jabbithole creatures with
unresolved Creature2 bridges.

**Coldblood Citadel branch harvest:** public event `907` / world `3522` now has
a conservative script scaffold imported from the LaughingWS `Instances-and-more`
branch: map binding, phase/objective progression, communicator dispatch,
entry/gather trigger handling, Hailstone Gatecrasher kill credit, and the
branch optional-objective rolls for liquid Soulfrost, shards, Pell rally,
prisoners, canisters, and Soulfrost traps. Optional objective selection is
explicitly WIP-guessed in code; exact route weights and the full dungeon path
still need live/manual smoke before treating the event as retail-complete.
War of the Wilds (`world 1393`, public event `158`) now has the branch-mapped
base adventure fight scaffold for the giant Moodie totem and totem-health
objectives; the scaffold is marked WIP-guessed in code and covered by focused
PvP/adventure branch tests. Faction-specific start events (`170`/`171`) and
end-delay/chat timing remain blocked. `Start-BlockerEvidenceHarness.ps1`
`-PvpAdventureSmoke` now creates the targeted LWS-080 through LWS-085
PvP/adventure smoke worksheet for Cryo-Plex, Daggerstone Pass, Halls of the
Bloodsworn, Walatiki Temple, War of the Wilds, and Rage Logic; queue/match,
scoring, rewards, PvP stats, faction-start, vehicle-choice, and encounter
parity still require completed bundles.
Outpost M-13 (`world 1319`, public event `108`) now has a conservative
branch-derived objective chain from Captain Milo through Hive Queen and shuttle
return; the branch's unknown final shuttle world-location trigger remains
intentionally omitted because the branch candidate uses world-location id `5`
at the zero vector and is not safe runtime behavior without retail/table proof.
Space Madness (`world 2149`, public event `390`) now has a conservative
branch-derived public-event phase/objective chain from Captain Tero through the
all-clear signal, with focused tests pinning initial phase, dynamic group
objectives, side objective activation, finish behavior, and inactive-objective
guarding. The participant-gather world-location triggers for the airlock and
research-laboratory handoffs are now ported as WIP-guessed code with comments
marking the missing retail trigger-row/timing proof. The branch's placeholder
door creature IDs and direct local teleport remain intentionally omitted pending
retail/client proof.
Protogames Academy (`world 3173`, public event `667`) now has the branch map
binding plus a conservative objective chain from academy initiation through
Wrathbone, with focused tests covering initial phase, group objective max
counts, phase advancement, finish behavior, and inactive-objective guarding.
The PhineasARotostar1..9 phase communicator broadcasts are now ported as
WIP-guessed behavior with code comments/tests. The branch gather/teleporter
world-location triggers are now also ported as WIP-guessed code with tests
pinning ids, objective object ids, and positions. Invulnotron, Gromka, Iruki
Boldbeard, Seek-N-Slaughter, and Icebox Mk. 2 now have WIP-guessed
objective-credit hooks only; exact boss combat mechanics, trigger timing,
communicator timing, platform/launcher cleanup, and entity-removal choreography
remain blocked pending stronger content smoke or client/table proof.
Fragment Zero (`world 3180`, public event `680`) now has a conservative
branch-derived objective chain starting at the branch-enabled "Continue the
Search for the Missing Crew" phase through Hugo's final objective, with focused
tests covering initial phase, multi-objective activation, prototype objective
group activation, phase advancement, finish behavior, and inactive-objective
guarding. Supervisor Lola phase broadcasts and the Captain Hugo continuation
follow-up plus the early search/friendly-Skeech/continuation triggers are now
WIP-guessed with code comments/tests. Exact trigger timing, cinematic gating,
doors, and entity cleanup remain blocked pending retail smoke proof.
Gauntlet (`world 2183`, public event `446`) now uses the corrected
`Expedition\Gauntlet` map binding path and has a conservative branch-derived
objective chain from Pilot Taboro through Judge Kain/Agent Lex, with focused
tests covering group objective counts, chamber branch activation, branch kill
objective activation, finish behavior, and inactive-objective guarding. The
airlock and swarm-pit participant-gather triggers are now ported as
WIP-guessed code with comments/tests marking missing retail trigger-row/timing
proof. Cinematics, announcer/communicator timing, arena door choreography, and
manual expedition smoke remain blocked.
Infestation (`world 1232`, public event `95`) now has a conservative
branch-derived objective chain from cargo-ship entry through the medical-bay and
heal-shorthand phase, with focused tests covering initial phase, dynamic group
objective counts, side objective activation, objective reset behavior, parasite
finish handling, and inactive-objective guarding. The branch's guessed
turnstile trigger is now ported as WIP-guessed code with comments/tests marking
missing retail placement/range proof. Untracked door/open-vent choreography, exact
medical-bay attack timing, parasite objective activation, and manual expedition
smoke remain blocked pending proof.
Expedition smoke coverage is now easier to collect: `Start-BlockerEvidenceHarness.ps1`
`-ExpeditionSmoke` creates the targeted LWS-090 through LWS-096 worksheet for
Outpost M-13, Space Madness, Fragment Zero, Gauntlet, Infestation, Evil from the
Ether, and Deep Space Exploration, including the known branch worlds/events and
negative cases for placeholder triggers, premature doors/shuttles/teleports,
wrong-phase triggers, completion-only cinematics, early finishes, and rewards.
The focused expedition scaffold filter now passes `78/78`; shuttle, door,
trigger, cinematic, communicator, teleport, cleanup, routing, reward, and
full-smoke parity remain blocked until completed bundles or decompile-backed
payload proof exist.
Ruins of Kel Voreth (`world 1336`, public event `161`) now has the branch map
binding plus a conservative main boss chain from the Blood Pit through
Forgemaster Trogun, with focused tests covering the initial phase,
branch-mapped Blood Pit objective count, deterministic Drokk/Gurka/challenge
activation, phase advancement, finish behavior, and inactive-objective
guarding. The branch optional objective rolls for slave mercy, Eldan data
storage, forge destruction, Osun, and war supplies are WIP-guessed with code
comments/tests. The branch's guessed trigger placement, door choreography, exact
optional route weights/objective availability, cinematics, and missing boss
entity scripts remain blocked.
Stormtalon's Lair (`world 382`, public event `145`) now has the branch map
binding plus a conservative main objective chain from Thundercall Pell survival
through Stormtalon, with focused tests covering the initial phase, objective
activation, group-count handling for the High Priest gather objective, phase
advancement, finish behavior, inactive-objective guarding, and the branch
optional objective rolls for tainted stems, altar data, storm totems,
prisoners, grenades, and the Arcanist/Overseer route split. The branch's
guessed trigger placement, Stormtalon reborn cinematic, exact optional route
weights/objective availability, boss spawn/version selection, and missing boss
entity scripts remain blocked.
Skullcano (`world 1263`, public event `148`) now has the branch map binding
plus a conservative main route through Thunderfoot/Stew-Shaman Tugga,
WIP-guessed cave/chasm route selection, Bosun Octog, the Redmoon platform
handoff, and Mordechai Redmoon. Focused tests cover the initial phase, opening
boss activation, random route selection, Find Chief cave-path objectives,
Dorian/Artemis cave callouts, cave continuation objectives, dynamic group counts
for chasm/platform objectives, final-approach objective activation, phase
advancement, Redmoon Terraformer activation, finish behavior, and
inactive-objective guarding. The branch's chasm, Find Chief, platform, and
Eldan Terraformer trigger scripts are now ported as WIP-guessed objective
updates with code comments marking the missing smoke proof. Branch optional side
objectives for captured Lopp, Redmoon prisoners/marauders, and missile consoles
are WIP-guessed with code comments/tests. Exact cave/chasm route weights, Chief
Kaskalak cave route choreography, door/platform choreography, communicator
timing, exact optional route weights/objective availability, and manual dungeon
smoke remain blocked pending proof.
Initialization Core Y-83 (`world 3040`, public event `595`) now has the branch
map binding plus a conservative quarantine-door-to-boss objective chain: mapped
freebot/door objectives advance to the door phase, the door objective advances
to the boss phase, and defeating the Prime Evolutionary Operants finishes the
event. Focused tests cover the initial phase, door objective activation, boss
challenge objective activation, phase advancement, finish behavior, and
inactive-objective guarding. The branch's guessed quarantine trigger placement,
door entity choreography, communicator/cinematic timing, and manual raid smoke
remain blocked pending proof.
Red Moon Terror (`world 3032`, public event `705`) now has the branch map
binding plus a conservative main raid objective chain from the Brig through
Laveka, with focused tests covering all single-objective phases, multi-objective
engineering/medbay/morgue phases, the full phase-advance chain, finish behavior,
and inactive-objective guarding. Ish'amel Robomination/engineering callouts are
WIP-guessed with code comments/tests; the branch static Laveka row is preserved
as WIP/GUESSED seed data and now attaches a WIP-guessed objective-credit script
for the final Laveka objective despite the source lacking an `entity_script`
row. Exact communicator/cinematic timing, Laveka choreography/awakening
mechanics, encounter-specific challenge mechanics, door/elevator
movement, and manual raid smoke remain blocked pending proof.
Genetic Archives (`world 1462`, public event `159`) now has the branch map
binding plus a conservative middle-to-final raid objective chain from the
post-Experiment/Kuralak ascent through Dreadphage Ohmna, with focused tests
covering guardian, archive-defense, convergence, miniboss, and Ohmna objective
activation; branch sub-objective activation; phase advancement; finish behavior;
and inactive-objective guarding. Ohmna entry/final cinematic callouts are
WIP-guessed with code comments/tests; exact communicator/cinematic timing,
weekly/random encounter selection, boss entity choreography, door/elevator
movement, and manual raid smoke remain blocked pending proof.
Sanctuary of the Swordmaiden (`world 1271`, public event `166`) now has the
branch map binding plus a conservative dungeon objective chain from Deadringer
Shallaos through Spiritmother Selene's corrupted form, with focused tests
covering initial, path, temple, Moldwood, Rayna, Skash, terrace, Ondu, relic,
escort, final-boss, phase-advance, finish, and inactive-objective behavior. The
branch's random Temple/Moldwood route choice is now ported as WIP-guessed phase
routing without branch door/trigger placement, and the Spiritmother Selene
random-path callout plus temple, Moldwood, Lifeweaver Terrace, and
flame-miniboss communicator triggers are WIP-guessed objective/message updates
with code comments/tests marking the missing smoke proof. Branch optional side
objectives for corrupted Torine spirits, Moldwood corruptors, skurge/crawlers,
soul spores, terrorantulas, and Corrupted Lifeweaver Pell are WIP-guessed with
code comments/tests. Exact route weights/objective availability, trigger
placement, door choreography, communicator timing, and manual dungeon smoke
remain blocked pending proof.
Datascape (`world 1333`, public event `157`) now has the branch map binding plus
a conservative raid objective chain from the opening system-daemon/probe split
through the Limbo, earth, logic, and volatility wings, all three personality
datacores, and Avatus. Focused tests cover opening activation, single- and
multi-objective phases, branch sub-objective activation, phase advancement, the
Warmonger Chuna branch bug fix, three-datacore gating before the Oculus handoff,
finish behavior, and inactive-objective guarding. The branch's
Caretaker111..114 wing callouts are WIP-guessed with code comments/tests.
`laughingws_instance_entity_wip_seed.sql` now preserves the placed Datascape
boss/objective anchors for ED-1, TX-67, P2-Z, the system daemons, frost boulders,
Frostbringer Warlock, Bio-Enhanced Broodmother, Gloomclaw, Hyper-Accelerated
Skeledroid, Augmented Herald, the Warmongers, and Avatus with deterministic
high entity IDs and WIP objective-credit scripts. Placeholder-coordinate rows,
trash carpets, elemental weekly/wing mechanics, exact communicator/cinematic
timing, encounter choreography, door/trigger placement, exact wing-order retail
proof, challenge mechanics, and manual raid smoke remain blocked pending proof.
The SQL-backed `entity_script` hook pass from
`LaughingWS.WorldDatabase.New-Zones-and-more` now reconciles all non-full-dump
boss script names for Protogames Academy, Ruins of Kel Voreth, Stormtalon's
Lair, Skullcano, Sanctuary of the Swordmaiden, and Genetic Archives, plus the
source-only Protogames Academy Seek-N-Slaughter/Icebox Mk. 2 WIP hooks,
Red Moon Terror Laveka WIP objective-credit hook, Datascape placed-anchor
objective-credit hooks, and the
`OptimizedMemoryProbeTX-67EntityScript` alias, as WIP-guessed objective-credit/
loader hooks with code comments and focused test coverage. Exact boss combat
mechanics, encounter choreography, placeholder-coordinate Datascape rows, and the
all-in-one-only Datascape Hydroflux/Mnemesis script references remain blocked
pending stronger retail/client proof.
Shade's Eve (`world 3044`, public event `597`) now has the branch map binding
plus a conservative early objective chain from fountain discovery through the
locals vote handoff, with focused tests covering the initial phase, dynamic
group objective count, phase advancement, vote start, and inactive-objective
guarding. The branch's guessed trigger placement, gather-ring cleanup, town
gate opening, exact communicator/cinematic timing, and exact vote follow-up
remain blocked; TheAngel1/TheAngel5 callouts are WIP-guessed with code
comments/tests.
Remaining branch map-only and entry/boss/phase/cinematic-hook scaffold content now covers Deep Space
Exploration (`world 2188`, public event `447`), Rage Logic (`world 1627`,
public event `213`), Ultimate Protogames dungeon (`world 2980`, public event
`594`), Ultimate Protogames raid (`world 3041`, public event `642`), Protostar
SuperMall in the Sky (`world 3094`, public event `679`), Journey into
OMNICore-1 (`world 3045`, public event `605`), Fragment Zero, Infestation,
Space Madness, Shade's Eve, Ruins of Kel Voreth, Stormtalon's Lair,
Initialization Core Y-83, Genetic Archives, Datascape, and Gauntlet cinematic
hooks. Focused map-binding
tests pin `19/19` public event IDs. Deep Space, Rage Logic, Ultimate Protogames
dungeon/raid, Protostar SuperMall, OMNICore, Fragment Zero, Infestation, Space
Madness, Shade's Eve, Ruins of Kel Voreth, Stormtalon's Lair, Initialization
Core Y-83, Genetic Archives, Datascape, and Gauntlet now also have WIP-guessed
entry/boss/phase/cinematic-hook scaffolds covered by `22/22` map-entry tests and
`155/155` event/trigger cinematic-hook tests: Deep Space activates
`TalkToCrewMembers`, Rage Logic sets the branch vehicle-choice phase, Ultimate
Protogames dungeon activates `InitiateUltimateProtogames` then stops at the
coarse random-event gate, Ultimate Protogames raid activates the Downsizer
objective set and finishes on Downsizer defeat, and SuperMall activates the
greeter gather objective then stops at the coarse random-path gate; OMNICore,
Deep Space, SuperMall, Fragment Zero, Infestation, Space Madness, and Shade's
Eve queue immediate completion-only placeholders for their branch on-create
cinematic hooks, while Ruins, Stormtalon, Initialization Core, Genetic Archives,
Datascape, Fragment Zero, and Gauntlet queue matching completion-only placeholders
at the branch-derived event/trigger cinematic points. Deeper event objective
routing, encounter logic, door/trigger placement, real cinematic payloads,
rewards, vehicle choice, boss challenge semantics, and route/randomization
choreography remain blocked pending proof.
The branch Northern Wilds Dominion gate helper (`12653`) now opens visible
Icefury Gate door `16799` and closes it after the mapped 10 second delay, with
focused branch-script coverage.
The branch map-zone objective hooks are now ported into the current map scripts:
Northern Wilds Q3486 credits tower-arrival objective `4987` and shows story
panel `1575` when an accepted player enters zone `729`; Crimson Isle Q5596
credits crash-site objective `8255` when an accepted player enters zone `1611`.
Focused `EarlyZoneEntityObjectiveCreditTests` cover accepted and missing-quest
guards.
The branch starter-zone intro cinematic hooks are now wired through current
cinematic interfaces: Northern Wilds (`3480` missing), Crimson Isle (`5593`
missing), Everstar Grove (`6296` missing), and Levian Bay (`6780` missing)
queue their corresponding `*OnCreate` cinematic on player map entry, with
accepted-quest guards covered by the same focused tests.

**Build 16042 start-zone classification:** Novice creation starts both factions
in Rider's Reef (`world 3460`). Veteran creation skips to the surface starter
zones: Exile Human/Granok to Northern Wilds (`426`), Exile Aurin/Mordesh to
Everstar Grove (`990`), Dominion Chua/Draken to Crimson Isle (`870`), and
Dominion Cassian/Mechari to Levian Bay (`1387`). Gambler's Ruin and Destiny are
older faction arkship tutorial zones and should not displace Rider's Reef for
16042 restoration unless a task explicitly targets historical/pre-16042 parity.

**Tutorial (Rider's Reef - world 3460):** PARTIAL / NOT COMPLETE
```
Exile:   10513->10527->10518->10525->10540->10519->10520->10528
Dominion: 10521->10532->10524->10526->10541->10522->10523->10530
```
All 12 quest scripts are present and recent focused regressions cover the
tutorial communicator checkpoint sequence, build 16042 character-create start
rows, tutorial quest follow-up grants, terminal-to-world routing plus destination
welcome-quest grants, final quest completion before visible surface receiver,
duplicate welcome-quest guards, no welcome grant when teleport is denied,
terminal recording on activation, per-player terminal
storage, known-terminal filtering, cross-faction/missing terminal default
routing, fallback-spawned departure terminals/checklist interactables,
both-faction starter quest grants and follow-up re-entry root-skip behavior,
logout/re-entry recovery boundary excluding the final departure quests from
auto-completion,
table-backed reward coverage for cryopod Omnibit and final-departure item
rewards,
combat-projector quest delta recovery, map-cache
fallback anchoring, delta-only tutorial quest initialisation, mine/combat lane behavior, and
faction-specific projector text. The hoverboard presentation/recovery pass now
casts table-backed ring lightning, sprint/trail, and booster spells
(`82460`, `82298`, `85424`), no longer casts the wrong `81662` scan on
opening step-pad objective completion, keeps a narrow `81662`
`CCState.Disable` suppression as a safety guard, starts the `82298` trail visual
when the hoverboard projector objective grants the board, covers ring/booster
effects when the range-entering entity is the mounted vehicle and the pilot
must receive the spell, strengthens booster velocity, snaps padded finish
recovery to
`WorldLocation2 51734`, and remounts through `85562` after relog while the ride
objective is still incomplete; the finish snap also runs when normal
area-objective sync already completed the ride objective before recovery checks
the finish location. A follow-up projector/ring correction keeps `84387` mapped-only
pending its exact retail producer, narrows ring contact to `82460` only, and makes
direct/cast projector activation fall back to hoverboard equip `85562` if the
activate spell path is blocked. Rider's Reef is still not complete until Exile
and Dominion client playthroughs verify quest acceptance/turn-in UI, kill loops,
rewards, respawn/re-entry, CSI, hoverboard and projector reliability, final
terminal/checklist flow, and VO timing.

**NorthernWilds (world 426):** CORRECTED - 3 independent chains:
```
Chain A: 3479/3480 -> 3667 -> 3486 -> 3671 -> 3668  (+3797 side)
Chain B: 3487 -> 3963                                (independent)
Chain C: 3886 -> 3673 -> 3670                         (independent)
```
**Created this pass:** Q3479, Q3886, Q3797, Q3963 scripts.
**Fixed this pass:** Q3667 quest script added, Q3487 chain grant to Q3963.
Focused regressions now pin the Q3479/Q3480 -> Q3667, Q3667 -> Q3486,
Q3486 -> Q3671/Q3797 mention/cinematic, Q3671 -> Q3668, Q3487 -> Q3963,
Q3886 -> Q3673, and Q3673 -> Q3670 cinematic/follow-up boundaries. Entity
regressions also pin Q3667 control-panel objective `4770` and Q3487 cannon
checklist objective credit from table-backed creature/checklist data.

2026-05-25 Veteran Northern Wilds follow-up:

- The 20-quest local block is now curated through `4696`: added scripts for
  `3741`, `3777`, `3781`, `3783`, `4526`, `4666`, `4667`, and `4696`, with
  Jabbithole/client objective evidence for supply crates, Loftite crystals, and
  captive soldiers.
- Public event `154` Dominion Ultrabot now starts for players in Camp Icefury
  and credits objective `371` when creature `12526` dies.
- Combat challenge progress now resolves direct and nested target groups, which
  covers Northern Wilds `103`/`105`; `ChallengeRequirement` prerequisites now
  compare challenge completion count instead of logging unhandled.
- The 13 Northern Wilds path missions activate for the player's active path
  episode with Jabbithole XP `25`; path reward rows with missing `Spell4`
  references now skip the spell grant instead of aborting the rest of the
  reward row; Explorer progress reports now complete only
  active Explorer Vista missions with a matching `PathExplorerNode` table row
  instead of creating runtime mission state or completing unrelated active
  missions from a client report, ExploreZone missions can complete on zone
  entry when the current map zone matches the active mission, power-map reports
  can complete active missions, Soldier assassinate missions now progress from
  matching killed Creature2/target-group counts, and Soldier tower-defense build packets complete
  matching active event missions, Soldier holdout control-point activations
  now complete Northern Wilds Soldier missions `33`, `34`, and `156`, and
  Settler build-tier packets progress active Settler_Hub missions against
  `PathSettlerHub.MissionCount` while emitting table-backed build result/status
  acknowledgements for the client UI.
  Scientist mission-creature mappings from
  Jabbithole now drive scan metadata and activation-completion hooks for
  `42`, `160`, and `648` (`18680`, `15888`, and the mapped Skeech Creature2
  IDs). Generic Soldier wave simulation and the generic Scientist scan-result
  packet remain partial pending stronger packet/result evidence.
- Jabbithole zone `1` has `164` enabled creatures; `161` now bridge to
  Creature2, with `3` still unmatched (`Ability Training Kiosk`, `Invisible
  Channel Unit`, and `Unknown`). `Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql`
  now has an opt-in entity-spawn promotion path (`1000000000 + source_coordinate_id`
  IDs, per-world/per-area filters, per-spawn `entity_stats`, reviewed
  exact-name ambiguous bridge opt-in, and targeted direct Jabbithole coordinate
  fallback for rows whose source `worldid` is blank). Local verification promoted
  `4,375` mapped entity rows / `15,423` stat rows and raised Northern Wilds
  runtime coverage from `90` to `161` enabled Jabbithole creatures in world
  `426`. Reviewed local bridge aliases were `1055` -> `11705`, `3351` ->
  `25876`, and `11313` -> `18680`. The remaining `3` require stronger Creature2
  evidence before runtime promotion. This is the accepted safe stop boundary for
  the Veteran Northern Wilds pass: do not force-map those three rows without new
  table, packet, or live-client evidence.

**CrimsonIsle (world 870):** CORRECTED - definitive chain from Jabbithole `quest2.pre_quest0/01/02`:
```
Roots:  5595 -> 5575 -.
        5593 -> 5573 -+-> 5596 -> 5597 -> 5604 -> 5580 -.
        5593 -> 8855  (parallel branch)       5604 -> 5583 -+-> 5594
Standalone: 5584, 5610 (type 11)
```
**Created/covered in this quest pass:** Q5593, Q5596, Q5597, Q5583 scripts and
the shared CrimsonIsleQuestChain helper.
**Fixed this pass:** Q5593 grants 5573/8855; Q5573/Q5575 grant Q5596 through
the table-backed OR prerequisite gate (`preq_flags = 1`); Q5604 grants both
Q5580 and Q5583; Q5580/Q5583 only grant Q5594 after both table-backed
prerequisites are complete; Q5594 warbot kill now guards already-completed
achievement 1730 while still crediting objective 8249; focused quest-chain
regressions cover the Q5595 -> Q5575, Q5596 -> Q5597, Q5597 -> Q5604 direct
follow-ups, these gates, final-warbot credit, and Q8855 Dominion soldier
activation/despawn objective credit.

### F-023 - NPE / Rider's Reef - PARTIAL
Recent tutorial/final-departure, mine/loot, hoverboard, combat-projector,
map-cache, communicator checkpoint, character-create start-row, quest-chain,
spell/effect terminal activation, terminal routing/welcome-quest, and
direct activation checklist credit for the actual departure checklist
interactables, final quest completion before visible surface receiver, plus
cross-faction/missing terminal fallback, both-faction starter grants,
follow-up re-entry root-skip behavior, logout/re-entry recovery boundary,
table-backed reward coverage, and
terminal/checklist fallback-spawn fixes are built and test-covered. The timestamped
evidence matrix is `Decomp/Analysis/coverage/RIDERS_REEF_EVIDENCE_MATRIX_2026-05-25.md`.
Rider's Reef is **not complete**. Manual client playthrough validation remains
for acceptance, turn-in, kill loops, rewards, respawn/re-entry, CSI,
hoverboard/projector reliability, final terminal/checklist flow, all four
terminal handoffs, and retail VO overlap/timing. Completion means the full build
16042 Rider's Reef Novice flow and terminal handoff into Everstar Grove/Northern
Wilds or Crimson Isle/Levian Bay; Gambler's Ruin and Destiny are older arkship
context, not the active 16042 Novice target. See F-022 Tutorial section.
The local Auth/World restart check on 2026-05-25 succeeded twice without
world-log reload/error/stack-overflow matches; the latest fresh PIDs were
`30632`/`43212` with ports `24000` and `23115` reachable. The later initial
quest chain correction makes fresh Rider's Reef entry grant only `10513`/`10521`
and recover `10527`/`10532` only after the movement root is completed. Focused
Rider's Reef regressions passed at 79/79 after the correction; the narrow
map/chain slice passed 17/17. A follow-up live UI correction now avoids sending
a mid-play `ServerQuestInit` refresh when the movement root advances into
`10527`/`10532`; the flow relies on the normal quest state delta while
`QuestManager` still suppresses the completed movement root during login
snapshots when the paired hoverboard quest is active. This keeps the opening
"three points -> platform -> projector" beat to one client-visible tutorial
chain; focused quest/map/chain/projector tests passed 35/35 and the broader
`FullyQualifiedName~Tutorial` filter passed 74/74. A later hoverboard presentation/recovery pass
added table-backed ring/booster visual casts, removed the wrong
opening step-pad `81662` scan cast, kept only the narrow scan `Disable` CC
safety suppression, added projector-completion trail start, mounted-vehicle
ring/booster pilot coverage, stronger boost velocity, finish snap to `51734`,
and direct hoverboard remount recovery for disconnects mid-ride; a later
finish-snap correction covers the case where the ride objective was already
complete before recovery runs. A follow-up projector/ring correction narrowed
ring contact to the character-side `82460` lightning/objective FX, kept `84387`
mapped-only pending exact producer evidence, and made direct/cast projector
activation fall back through hoverboard equip `85562`. Focused finish/effect/opening tests passed 58/58
after rebuild, the ring Power Boost remap filter passed 23/23, the
projector/ring correction filter passed 11/11, the broader correction slice
passed 82/82, and the rebuilt tutorial filter passed 78/78; the
combat-mine telegraph follow-up now keys the
danger-zone warning window off `Spell4.CastTime` (`85430`/`85629`/`85630`) before
falling back to `SpellDuration`, with focused Start-vs-Go/Finish regression
coverage. An earlier broader `FullyQualifiedName~Tutorial` filter passed 66/66.
No full native client
Exile/Dominion playthrough has been completed from this API context. A
read-only character DB/log audit found only partial early NPE evidence: 28 local
characters were still on `worldId=3460`, one local character was on Northern
Wilds `426` at the Human/Granok Veteran start coordinates, and no persisted
final Rider's Reef (`10528`/`10530`) or destination welcome quest
(`9112`/`9113`/`9126`/`9127`) rows were present.

**DataMapping safe import bridge (2026-05-25):** the safe staging-to-runtime SQL
now includes the Rider's Reef world `3460` cleanup for tutorial combat rows:
delete stray `73665` Beacon Arrow entities and normalize imported turret
creatures `73494`/`74862` to runtime `NonPlayer` rows with combat factions
`1441`/`1442`. Local verification loaded `95/95` `nf_map_*` staging tables,
applied the safe import, and reported zero Beacon Arrow rows, zero wrong-faction
turrets, and zero wrong-type turrets.

### F-024 - Evil from the Ether (world 3404) - PARTIAL / WIP-GUESSED
Branch-derived map and public-event `781` scripts exist for the expedition
route: objective/phase progression, dynamic trigger creation, door management,
medbay/upper-deck/escape teleports, cinematic queuing, communicator messages,
crew-log callouts, portal/tether behavior, and Security Chief/Ravenous/Katja
combat scaffolds. The branch-derived trigger, teleport, communicator, cinematic,
and encounter assumptions are now explicitly marked WIP-guessed in code.
Focused Evil from the Ether event/trigger regressions pass `12/12`, and the
focused instance/public-event slice passes `436/436` with isolated output.
Map-instance pending-removal runtime now resolves formula `1123` through a
nullable `GameFormula` lookup, so unavailable static data uses the existing
30-second client-default fallback instead of throwing before
`ServerPendingWorldRemoval`; focused pending-removal verification passed `2/2`
and the broader map bucket passed `459/459` from the alternate output
directory.

**Blocked:** manual expedition playthrough, exact retail trigger rows and
coordinates, direct medbay transition proof, cinematic ids and completion timing,
door/marker cleanup choreography, boss spell cadence/mechanics, objective
notification parity, rewards, and unsupported content/spell dependencies remain
blocked before this can be called retail-complete.

### F-025 - Entity Create/Update/Visibility/Interaction - PARTIAL

**Implemented:** Core entity create/update works. Entity-create aux packets
(0x025F-0x0264) emitted via `EntityCreateAuxiliaryPacketBuilder.BuildPreCreatePackets()`
before `ServerEntityCreate` with entity GUID, type, faction, prop ID, controller GUID,
socket ID. Busy target and interaction gates partial. `ShowRealmBank` (interaction 67)
loads realm bank items and no longer marks an account/realm as loaded when the
character database is unavailable, so a later successful DB connection can still
hydrate the realm bank. CSI objective crediting covered by focused regression tests.
The 2026-06-09 live follow-up verified eligible Realm Bank item movement after
wire-location normalization: an item round-tripped `Inventory -> RealmBank ->
Inventory` with `ClientItemMove(0x0182)`/`ServerItemMove(0x0569)` and Realm Bank
encoded as wire location `0x0A`; remaining red-cross cases are client-side item
eligibility filtering.
Threat list via `ThreatManager.BroadcastThreatList()`.

**Emitted:** entity-create auxiliary opcodes `0x025F`..`0x0264` via
`EntityCreateAuxiliaryPacketBuilder.BuildPreCreatePackets()` from
`Player.AddVisible()` before `ServerEntityCreate`.

**Blocked:** 6 entity-stat auxiliary opcodes (0x0889, 0x08CC, 0x08F4, 0x0939,
0x093D, 0x093E) - wire shapes modeled, Ghidra reader labels exist and
registration anchors now exist for all six after the 2026-06-04
`FindImmediateInstructions` pass added `0x0939`/`0x093D`/`0x093E`; field-level
decomp verified against C# models. No production emit sites in
`NexusForever.Game` / `WorldServer` (packet-shape tests only); the 2026-06-04
source audit found only regular `ServerEntityStatUpdateFloat`/`Integer`
runtime sends, not `ServerEntityStat*` aux constructors outside tests/models.
Pass 117 adds a packet-placeholder guard for the six neutral aux field sets
(`Value*` / shared `Value` / `Text`) and passed focused placeholder/entity aux
coverage `91/91`.
2026-06-09 cached-export/source recheck found no running Ghidra MCP instance and
reconfirmed the selected `WildStar64.exe` fragments as reader evidence only:
`0x0889` uses the shared three-`uint32` triplet reader (`140080bf0`), `0x08CC`
uses the shared `uint32` plus wide-string reader (`1400980f0`), `0x08F4`
uses `140097620`, `0x0939` uses `140097ee0`, `0x093D` uses `140097690`, and
`0x093E` uses `140097f70`. Current source still finds the six
`ServerEntityStat*` aux models only in packet models, packet-shape tests,
placeholder naming guards, and negative entity-create emission guards; runtime
stat sends remain the regular `ServerEntityStatUpdateFloat` /
`ServerEntityStatUpdateInteger` paths.
The stale native label `FUN_140939650` is now rejected as an aux consumer
candidate because the cached fragment is zero-argument viewport/grid global
math with label-only xrefs, not a socket/opcode/payload apply handler.
Consumer-handler dispatch semantics and map-tracked-unit producer timing remain
blocked (indirect calls through the function pointer registration table and no
server-side tracked-id/TrackingSlot selection proof). A 2026-06-04 focused
Ghidra MCP pass reconfirmed `0x0849`/`0x0848` as client cache update/remove
consumers only; duplicate `TrackingSlot.PublicEventObjectiveId` groups reject
objective-only slot selection. Pass 118 adds a packet naming guard plus a
duplicate-objective helper guard so no objective-only reverse selector is
available. `TrackingSlotHelper` now has a focused
null-table guard and tests for 15-bit id masking/objective lookup, but no
producer or reverse selector.

### F-026 - Items / Unlocks / Costumes / Pets / Titles - PARTIAL
Inventory, title, pet, costume, unlock surfaces exist. Generic unlock
lifecycle tested, including invalid set/entry, already-unlocked, partial
multi-entry unlocks, and consume-failure invalid result without granting locked
entries. The generic unlock manager now treats a missing `GenericUnlockEntry`
table like an empty/invalid static-data surface: direct unlocks send the existing
`Invalid` result, and `UnlockAll` no-ops without emits.
Persisted account generic-unlock rows now resolve `GenericUnlockEntry` during
manager startup; missing table/row entries are skipped from transient runtime
state and readback without database mutation, while table-backed rows load
non-dirty. Focused manager coverage passed 6/6 and broader generic-unlock
verification passed 22/22.
Account-item generic-unlock claim grants now resolve `GenericUnlockSet` and
`GenericUnlockEntry` through nullable table lookups and return the existing
`InvalidAccountItem` account-operation result before unlock grants or item
deletion when partial static tables are unavailable.
Learn-dye-color spell effects now also treat a missing `GenericUnlockEntry`
table like an unknown generic-unlock row, preserving diagnostics and delegating
to the existing invalid generic-unlock result path; focused spell collection
coverage passed 1/1 and broader spell / generic-unlock verification passed
25/25.
Collection spell unlock effects now also treat a missing `Spell4` table like an
unknown spell, returning before `SpellManager.AddSpell` or mount/vanity-pet
unlock packet emission; focused spell collection coverage passed 3/3 and the
broader spell / generic-unlock bucket passed 27/27.
Pet-flair spell effects now treat a missing `PetFlair` table like an unknown
pet-flair row, returning before `HasFlair` or `UnlockFlair`; focused spell
collection coverage passed 5/5 and the broader spell / generic-unlock / pet
bucket passed 312/312.
Title grant/revoke spell effects now treat a missing `CharacterTitle` table
like an unknown-title row, returning before `HasTitle`, `AddTitle`, or
`RevokeTitle`; focused spell collection coverage passed 9/9 and the broader
spell / generic-unlock / pet / title bucket passed 334/334.
Title manager load now skips persisted title rows whose `CharacterTitle`
static data is unavailable, clears an active title that is filtered out by the
partial load, and guards `AddTitle`, `RevokeTitle`, and `AddAllTitles` through
nullable `CharacterTitle` table lookups; focused title manager coverage passed
6/6, title / spell collection coverage passed 33/33, and the broader
generic-unlock / pet / title / spell collection bucket passed 343/343.
Pet customisation manager load now skips persisted pet-flair rows whose
`PetFlair` static data is unavailable, saved customisation slots with
unavailable flair rows resolve to empty slots, and zero-flair slot clears no
longer require `PetFlair` table availability; focused pet customisation
manager coverage passed 3/3 and broader pet coverage passed 293/293.
Costume forget/unlock parity exists, and costume unlock now rejects
non-equippable inventory items before creating an account unlock, soulbinding
the item, or sending success. Account costume unlock/forget now also tolerates
partial costume static-table loads: missing `GameFormula` row/table `1203` uses
the documented client default unlock limit, and missing `Item` tables return the
existing `InvalidItem` forget result without deleting the known unlock.
Costume save now treats missing `ItemDisplay` tables as unavailable dye metadata
and missing `DyeColorRamp` tables/rows as invalid nonzero dyes, returning
`CostumeSaveResult.InvalidDye` before costume mutation or success packets.
`CostumeItem.GenerateDyeMask()` now rejects unknown nonzero dye ramps instead of
null-refing; focused costume save coverage passed 9/9 and broader costume /
packet-shape coverage passed 18/18.
Normal `ClientItemUse` spell activation now preflights empty stack/charges and
consumes only after `TryCastSpell` returns `CastResult.Ok`; failed/dead/disabled
casts are covered by `ClientItemUseHandlerTests` and no longer delete the
activated consumable. Activated item-use now also treats missing `ItemSpecial`
tables like missing rows before casts or consumption; focused item-use
verification passed 9/9. Decor item-use now preflights empty stack/charges and
residence access before consuming, catches housing access failures before
mutation, and creates decor only after `Inventory.ItemUse` succeeds; this is
covered by `ClientItemUseDecorHandlerTests`. Missing `HousingDecorInfo` tables
now follow the same invalid-decor boundary before residence lookup, item
consumption, or decor creation.
Repair-vendor item requests now treat missing repair-cost `GameFormula`
table/row `0x022F` like zero computable repair cost, returning before
affordability checks, credit debit, or durability mutation; focused vendor
repair coverage passed 10/10.
Treasure item use now treats missing `CurrencyType` tables like missing rows
before consumption or currency grants, and otherwise consumes category/type
`94/200` items with table-backed character currency payloads before granting
currency, covering `Cache of Corroded Coins` (`50806`) from both
`ClientItemUse` and item context-action paths. Broader item-use verification
passed 20/20. Stackable charged consumables now repair zero stored charges
before item packets are built and before cast/consume, covering `Basic Medishot`
(`14838`) without making non-stackable charge items free to use. Starter loot-bag seed
coverage now includes `Nexus Survival Kit` (`83615`) with the retail-evidence
starter supply set and `Protostar's Revolutionary Rucksack 1-10`
(`80875`-`80884`) as consumable loot bags. Rucksack 1 keeps the
patch-note-backed faction starter mount licenses (`Equivar (Provisionary)
License` for Exile, `Velocirex (Provisionary) License` for Dominion), rucksacks
1-9 grant the next rucksack, and all ten include a WIP-inferred one-item PvP
gear roll from the matching client Mk I-X item block (`82720`-`83311`) because
the original server-side container relation is absent from the current
Jabbithole/DataMapping imports.
Item salvage now shares the client `ClientItemUseLootBag` (`0x015E`) item
location/guid packet shape for non-loot-bag items, but routes through a
dedicated `GlobalLootManager.TrySalvageItem` path. The server now generates
from the runtime-owned `item_salvage` table, preflights delivery, deletes one
source item with `ItemUpdateReason.Salvage`, and then delivers the generated
reward; missing salvage data still returns `ItemCannotBeSalvaged`. The safe
DataMapping import promotes `item_salvage_map.csv` exact rows as `purpose = 0`
and `client_source_salvage_map.csv` client type/level rows as `purpose = 1`, so
salvage no longer depends on high-range `item_loot`/`loot_group` fallback rows.
Per-instance dynamic `NotSalvageable` flag parity remains unmapped.
`SupplySatchelManager` now resolves `TradeskillMatStackLimit` defensively: a
missing, invalid, or non-positive reward property falls back to the default
100-material cap, oversized caps clamp to the packet-safe `ushort.MaxValue`,
and saved over-cap material counts reject additional material without mutation
or a misleading stack update. The login packet builder also ignores material ids
outside the fixed 512-slot payload. Material conversion now fails closed when an
inventory item or material id cannot be mapped to a `TradeskillMaterial` row,
returning the full remainder or reporting the item as full before inventory
mutation. Moving a cached material back to inventory also no-ops when the saved
material has no live material entry. `SupplySatchelManagerTests` cover the
fallback, over-cap, clamp, packet-capacity, mapped-item, unmapped-item,
unmapped-material, and stale-conversion cases.
`ServerSupplySatchelAux` (`0x019A`) now uses the shared mapped reader shape
(`6-bit value`, `uint32`) and `ServerCostumeItemAux` (`0x037F`) now uses its
direct mapped reader shape (`14-bit value`, three `uint32` fields, two flags)
instead of fixed raw payloads. Nearby item-data aux packets `0x056B`,
`0x056C`, and `0x056D` now use direct reader-backed field shapes instead of
fixed raw payloads. Producer/consumer semantics for those aux packets remain
blocked.
The 2026-06-09 cached-export/source recheck kept `ServerSupplySatchelAux`
(`0x019A`) mapped-only. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x019A` size `8` at `selected_decompiled.c:12829` to shared
`MatchingQueueResultWaitTime_ReadPayload` (`14007fcf0`), whose cached fragment
reads one 6-bit field and one `uint32`. Selected xrefs for `14007fcf0` are
label-only and selected call edges stay reader-local through `FUN_14006c090`.
The same reader is reused by `ServerCharacterDeleteResult` (`0x00E6`),
`ServerMatchingMatchKickCooldownUpdate` (`0x0616`), and
`ServerMatchingMatchOperationResult` (`0x0623`), whose managed emitters are
separate positive controls and do not prove supply-satchel semantics. Current
source still emits `ServerSupplySatchelUpdate` (`0x0199`) for stack updates and
finds `ServerSupplySatchelAux` only in the model/opcode enum and
packet-placeholder tests.
The 2026-06-09 cached-export/source recheck kept `ServerCostumeItemAux`
mapped-only: `Network_RegisterServerOpcode_0351` (`14006c290`) registers
`0x037F` size `0x18` to `ServerCostumeItemAux_ReadPayload` (`1400874a0`),
whose cached fragment reads one 14-bit value, three `uint32` fields, and two
trailing 1-bit flags. Selected xrefs for `1400874a0` are label-only and
selected call edges stay reader-local through `FUN_14006c090`. Adjacent
`ClientEmote` (`0x037E`) has a separate writer (`140087270`), and costume
unlock/forget/save positive controls dispatch named result events rather than
proving a `0x037F` producer. Source still finds `ServerCostumeItemAux` only in
`ServerClusterAuxPackets.cs`, `GameMessageOpcode.cs`, and packet-placeholder
tests.
The 2026-06-09 cached-export/source recheck kept this item-data aux band
mapped-only. Cached `WildStar64.exe` fragments under
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
reconfirm `Network_RegisterServerOpcode_0351` (`14006c290`) registering
`0x056B` size `0x20` to `ServerItemModdableData_ReadPayload` (`1400a3ce0`:
item guid, threshold data, random glyph data, random circuit data), `0x056C`
size `0x28` to `ServerItemMicrochips_ReadPayload` (`1400a3d50`: item guid,
maker character id, random circuit data, 18-bit power-core item id, 3-bit
count, counted microchip item ids), and `0x056D` size `0x18` to
`ServerItemGlyphs_ReadPayload` (`1400a3e40`: item guid, random glyph data,
4-bit count, counted glyph item ids). Selected xrefs for all three readers are
label-only and selected call edges stay inside bit readers, allocation, and raw
array copy helpers. The known client apply helpers
`Inventory_UpdateItemMicrochipsFromWire` (`1403b8540`),
`InventoryItem_ApplyMicrochipsFromWire` (`14056aa20`),
`Inventory_UpdateItemSocketBitsFromWire` (`1403b85a0`), and
`InventoryItem_ApplyGlyphsFromWire` (`14056aba0`) prove item-state patch
application and `ItemModified` dispatch on the client, but not server send-site
ownership or emit timing. Source still finds `ServerItemModdableData`,
`ServerItemMicrochips`, and `ServerItemGlyphs` only in packet models and
packet-shape tests. Keep all three packets non-emitted pending a native server
producer path, apply-table classification, callback/table owner, or accepted
item-replication capture.

**Blocked:** `ClientItemContextActionHandler` still keeps `SelectedBranch`
diagnostic-only for non-use item actions until right-click branch semantics are
mapped. Pet stance packet shape and client apply are mapped through
`Pet_SetStance_SendClientPetSetStance` (`14050a270`),
`ServerUInt32UInt5_ReadPayload` (`14008ce80`),
`Pet_GetStance_ReadCachedStance` (`14050a130`), and
`Pet_ApplyStanceChangedPayload` (`1403c0a80`) for `ServerPetStanceChanged`
(`0x068F`). `ClientPetSetStanceHandlerTests` now pin the current
server-state-only boundary: owned pet requests update `IPetEntity.Stance`,
invalid/foreign requests do not mutate pet state, and the handler emits no
blocked `ServerPetStanceChanged` response. Server producer timing/scope remains
unmapped.
`ClientPetCustomisation` now rejects unsupported pet types and flair slot
indexes outside the four-slot customisation payload before calling
`IPetCustomisationManager.AddCustomisation`. The handler sends
`ServerPetCustomisationFailed` with `PetTypeNotSupported` or `InvalidSlot`,
and `ClientPetCustomisationHandlerTests` pin valid delegation plus both failure
packets. Flair ownership, object validity, name validation, and broader pet
lifecycle producer timing remain evidence-gated. `ClientSummonVanityPet` now
resolves the learned spell tier and requires that tier to contain a
`SummonVanityPet` effect before calling `CastSpell`, so other learned spell
families cannot be cast through the vanity-pet request path.
`ClientPathScientistSetScannerName` now requires `PetType.ScanBot` and an
existing scanbot customisation before calling `RenamePet`, so non-scanbot or
locked-profile rename requests fail before row creation/mutation. Focused Pet
verification passed 287/287.

### F-027 - Options / Keybindings - COMPLETE
6 handlers, account+character keybinding split, casting options, combat
log preferences. Zero stubs. Keybinding updates now materialize stale binding
ids before removing pending-created rows, so clearing newly-created bindings
before save no longer invalidates dictionary enumeration. Character-scoped
keybinding request/update packets now reject character ids that do not match the
logged-in player before readback, mutation, or response enqueue.
`ClientCombatLogDisableOthers` now preserves the raw `uint` payload and rejects
values greater than `1` before updating player combat-log preferences. Focused
option verification passed 36/36. Nearby server aux packet contracts `0x056B`,
`0x056C`, and `0x056D` are packet-covered, but additional option types, broader
option readback/initialisation semantics, and aux producers remain blocked.

### F-028 - Support / Reports / Surveys / Stuck - COMPLETE
5 support handlers + stuck handler. Stuck cooldowns are RecallBind 30m,
RecallHouse 20m, and Death/Suicide 10m via `RetailStuckCooldownTracker`;
cooldowns are recorded only after the server validates the selected stuck
destination/action, so missing transmat or residence data returns a prerequisite
failure without poisoning the next valid attempt; a missing `WorldLocation2`
table now follows the same prerequisite-failure path instead of throwing during
setup or partial game-table loads. JSONL submission store. Survey parsing.
2026-06-08 source/protocol recheck found no modeled client support-case
list/read request and no server support-case readback payload beyond the current
submit result and mapped aux packet shapes, so durable case workflow/readback is
blocked pending protocol/backend evidence. Focused Support verification passed
126/126.
Durable database-backed case lifecycle, support-case readback, stuck-result
signaling precision, and report/survey admin tooling remain incomplete.

### F-029 - Client DB / Data Mapping - COMPLETE
Table registration pattern mapped, DataMapping exists. Field renames
gated on packet/data contract + migration proof.

**Verified safe import pass (2026-05-25):** smoke mapping completed against
`Tools\DataMapping\output_test`; full staging reload from `Tools\DataMapping\output`
loaded all `95` `nf_map_*` tables; `verify_safe_world_imports.sql` passed for
vendor, loot, creature-info, item-container, and Rider's Reef world-row cleanup
metrics without adding any runtime dependency on staging/reference tables.
`apply_creature_loot.py` now writes per-pass selected creature/item counts plus
status-filter, missing Creature2, invalid/missing Item2, duplicate, and
runtime mapped-flat-group skip metrics to `creature_loot_apply_report.json`.

**LaughingWS data intake (2026-05-27):** `Tools\DataMapping\analyze_laughingws_worlddb.py`
audits the external `NexusForever.WorldDatabase.New-Zones-and-more` branch before
promotion. Reviewed extracts are limited to additive/idempotent runtime overlays:
`laughingws_map_entrance_seed.sql` keeps `23` missing `map_entrance` rows,
`laughingws_city_content_seed.sql` keeps `17` curated placed
city/museum/quest-terminal `entity` rows for the ported transport, Illium
Museum content, WIP/GUESSED Illium Ringo Hax placement, and WIP/GUESSED
Skull's Eye escape pod terminal, plus `8` coordinate-keyed WIP/GUESSED
`questChecklistIdx` updates for the official Thayd/Illium housing intro props
with guarded WIP/GUESSED fallback inserts for older local imports missing those
rows, and
`laughingws_quest_instance_wip_seed.sql` keeps `3` WIP/GUESSED Dust Stalker
quest-instance `entity` rows plus `4` Boss Xagg `entity_stats` rows for quest
`4516`, stripping the branch world delete and replacing its source health `1`
with DataMapping-backed stats while leaving the exit panel marked source-only.
`laughingws_small_world_wip_seed.sql` keeps `16` WIP/GUESSED small-world
`entity` rows plus `59` `entity_stats` rows for Tactical Uplink, Captain
Darkstone, Commander Durek, Cortex Prime Whitewater, Corrigan Doon, Star-Comm
Basin The Caretaker, source-only Arcterra Caretaker/Coldblood portal
placements, source-only Palaver Point Ish'amel, and the reviewed Northern Wilds
Deadeye Brightland, Scientist Lusk, Elder Bartol, and Q3673 Signal Flare
placements after reconciling DataMapping-backed branch creature/area/display/
outfit mismatches where available; Dominion/Exile Arkship tutorial rows are
audit-only/rejected for current build-16042 runtime seeds. It also applies
`140` coordinate-keyed WIP/GUESSED `questChecklistIdx` updates to official
rows: `2` Everstar Grove Exo-Lab 71 Eldan Teleporter rows, `33` Wilderrun Kel
Ulgar Weapon Rack / Tattered Banner / Intact Data Cache / Cassian Commonwealth
Emblem rows, `67` Auroria objective rows for Black Hoods Alchemy Supplies,
Cubig Security Zapper Console, Seismic Thumper,
Hycrest infected/plague-victim clusters, Freebot Drills, Navigation Control
Panels, Bingberry Stills, and GL-04 Auto Turrets, plus `24` Crimson Isle
objective rows for Megatech Terminals, Exile Anti-Air Cannons, Dreg Tents,
Dominion Demolitions Experts, Power Regulators, and Tower Controls, plus `14`
Levian Bay Signal Flare / Drop Pod Landing Beacon rows for Lighting the Way and
Lost in the Fog. On older local bases missing those official Everstar Grove,
Auroria, Crimson Isle, or Levian Bay rows, the seed inserts guarded WIP/GUESSED
fallback `entity` rows in the
`1100210001`..`1100210067`, `1100220001`..`1100220024`, and
`1100230001`..`1100230002`, and `1100240001`..`1100240014` subranges, plus up
to `219` fallback `entity_stats` rows; branch Wilderrun Dorian rows remain
blocked
because their source creature ids do not match current DataMapping identity,
Northern Wilds branch spline-mode changes remain blocked pending movement
proof, branch Supply Officer Windward vendor rows are rejected in favor of the
existing DataMapping vendor import, and Everstar Grove/Levian Bay/Auroria/
Crimson Isle branch-only residual rows remain blocked pending row-level proof.
`laughingws_instance_entity_wip_seed.sql` keeps `75` WIP/GUESSED instance-local
`entity` rows plus `67` `entity_event`, `35` `entity_script`, and `80`
`entity_stats` rows for Coldblood Citadel, Protostar SuperMall, Space Madness,
Gauntlet, Fragment Zero, Infestation, Outpost M-13, Evil from the Ether
drive-spark phase anchors, Protogames Academy, Ruins of
Kel Voreth, Stormtalon's Lair, Skullcano, Sanctuary of the Swordmaiden, and
Genetic Archives, map-bound Ultimate Protogames, WIP-script-backed Red Moon
Terror Laveka, Shade's Eve Etty/fountain anchors, and the placed Datascape
boss/objective anchors where current WIP scripts/map bindings already exist. It also
applies `14` coordinate-keyed WIP/GUESSED updates against the official Evil from
the Ether import: `7` branch area reconciliations for Captain Weir, the gather
ring, medbay controls, and spare-parts crates, plus `7` crew-log
`questChecklistIdx` reconciliations for the branch-derived
`CrewLogEntityScript` without duplicating official spawns; full encounter
choreography, trigger timing, NPC interaction, Protogames static trigger
cleanup, combat behavior, Evil from the Ether crew-log/area smoke, Datascape
placeholder rows/trash carpets/elemental mechanics, and cinematics remain
blocked pending client smoke proof.
`laughingws_store_catalog_seed.sql` keeps `3,866` store-table upserts, including
the reviewed Valentine's Day, Shades Eve, and Winterfest store-event rows, after
stripping the branch dump's DDL/deletes and broad event entity rows. The store
extractor now also audits Dungeon Chase by canonicalising its legacy store-table
aliases and known `Field_7` typo, but the hidden SMC placeholder remains skipped
because source type `0` item `86919` exceeds the current world store model and
15-bit storefront transport proof; see
`Decomp/Analysis/LAUGHINGWS_DUNGEON_CHASE_SMC_PLACEHOLDER_REVIEW.md`.
`laughingws_quest_loot_seed.sql` keeps `7,650` virtual-item quest loot rows from
`Loot\CreatureQuestLoot.sql`, shifting source `loot_group` ids into a
DataMapping-owned high range so they do not collide with current runtime loot
groups (`1174` loot groups, `1174` loot items, `5302` entity loot links).
`laughingws_live_event_wip_seed.sql` keeps `440` WIP/guessed live-event rows
(`198` `entity`, `68` `entity_stats`, `8` `entity_vendor`, `24`
`entity_vendor_category`, and `142` `entity_vendor_item`) with deterministic
high entity IDs and SQL comments marking the branch-authored placement as not
retail-verified.
`laughingws_housing_skyplot_wip_seed.sql` keeps `411` WIP/guessed Skyplot
housing rows (`3` `entity`, `12` `entity_stats`, `2` `entity_vendor`, `72`
`entity_vendor_category`, and `322` `entity_vendor_item`) with deterministic
high entity IDs, strips the source world-replacement delete, and comments the
one branch row whose omitted `OutfitInfo` column was corrected.
The audit also treats explicit `ScriptFilterScriptName` aliases as implemented
script names; after the SQL-backed hook pass, missing C# script references are
limited to rejected all-in-one Datascape Hydroflux/Mnemesis mechanics rows.
`BranchCatalogCleanupTests` now guards that rejection by keeping the
all-in-one-only Hydroflux/Mnemesis script names and representative empty or
door/platform/marker-only branch catalog files out of runtime source until an
active, evidence-backed consumer exists.
The latest LaughingWS SQL audit reports `0` unsupported current-schema targets.
Pure `map_entrance` source files are now marked covered by the generated map
entrance seed instead of remaining as unresolved candidates.
Store catalog/event store rows and `Loot\CreatureQuestLoot.sql` are likewise
marked covered by their generated seeds; mixed event files now pair store
coverage with WIP live-event coverage or official-duplicate classification
instead of generic manual review.
Reviewed source files consumed by the city-content, Dust Stalker
quest-instance, small-world, instance-entity, and Skyplot extractors are now
also marked covered by their generated seeds, while the historical arkship,
city-content placeholder, live-event placeholder/duplicate/remover, and
Initialization Core Y-83 source rows are explicit rejections. The PvP arena
forcefield/map-entrance rows in the branch Cryo-Plex and Slaughterdome SQL are
now classified as covered by the official arena import already used by current
runtime scripts, while Slaughterdome's decorative spectator rows remain
rejected/audit-only pending retail placement proof. Five Isigrol open-world
branch dumps are now classified as covered by the official world database
import. The broad new-zone Dreadmoor, Halon Ring, and Murkmire SQL files are
now classified as `blocked_laughingws_new_zone_broad_rows` because the source
rows are hand-authored placeholders, mostly zero-coordinate/area-zero, or
quest-checklist/stat-heavy without current script support. Illium's useful
branch rows are now fully covered by the city-content seed while its orphan
vendor stubs are rejected, Thayd's useful housing intro checklist rows are
covered by the city-content seed while its residual vendor rows are blocked,
and Levian Bay, Everstar Grove, Auroria, and Crimson Isle useful official-row
quest checklist updates are covered by the small-world seed while residual
branch-only rows are blocked. Northern Wilds row review found `23` useful
entity/stat rows covered by the small-world seed, retained `9` official-only
rows, rejected `30` duplicate entity/vendor rows, and blocked `8` branch
spline-mode rows. Wilderrun row review found `33` useful checklist rows covered
by the small-world seed, retained `33` official-only rows, and blocked `5`
branch-only Dorian entity/stat queue rows because current DataMapping Dorian
identities are `25779`/`51292`, not `27455`/`27843`/`58746`.
The remaining LaughingWS implementation-plan smoke/proof gates are tracked
task-by-task in `Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_CLOSURE_MATRIX.md`
as still-blocked, rejected, or already-verified states; no broad SQL or unproven
runtime behavior was promoted by that tracker.
`Start-BlockerEvidenceHarness.ps1 -Lws036ChecklistSmoke` now creates a
row-specific worksheet, default target worlds, and negative-case checklist for
the Illium Ringo Hax, Thayd/Illium housing intro, and small-world checklist
smoke gate. This does not promote any WIP/GUESSED row; it only makes the
required client/server evidence bundle repeatable.
`Start-BlockerEvidenceHarness.ps1 -NewZoneAssetProof` similarly creates a
row-level asset-proof worksheet and negative/safety checklist for Dreadmoor,
Halon Ring broad residuals, Murkmire, and sandbox/test-zone investigations.
Those rows remain blocked or runtime-rejected until completed row-level bundles
prove client assets, exact placement, runtime ownership, and safety.
`Start-BlockerEvidenceHarness.ps1 -DustStalkerQ4516Smoke` now creates the
LWS-051 Dust Stalker target worksheet, defaults world `1138` and objectives
`7633`, `6189`, `7637`, `7638`, and `7639`, and captures the required
premature-bridge, premature-exit, missed-timer, and repeat-interaction negative
cases. Dust Stalker self-destruct and exit behavior remain blocked until a real
client/server bundle proves the timer and sequence.
`Start-BlockerEvidenceHarness.ps1 -ArcterraPalaverSourceSmoke` now creates the
LWS-052 Arcterra/Palaver source-only target worksheet, defaults worlds `3335`
and `3519`, preloads the known Palaver objective ids, and captures invalid
portal target/state, absent Palaver quest state, missing Caretaker prerequisite,
and repeat-interaction negative cases. The Caretaker, Coldblood portal, and
Ish'amel rows remain WIP/GUESSED until a real bundle proves placement and
behavior.
`Start-BlockerEvidenceHarness.ps1 -SkyplotHousingSmoke` now creates the LWS-053
Skyplot housing target worksheet, defaults world `1229`, and captures return
pad without residence/community session, vendor before unlock, invalid catalog
row, and inactive-lifecycle negative cases. The Skyplot rows remain WIP/GUESSED
until a real bundle proves vendor placement/catalog, return-pad interaction, and
active housing/community lifecycle behavior.
`Start-BlockerEvidenceHarness.ps1 -LiveEventSmoke` now creates the LWS-054
live-event target worksheet and captures inactive event, event-ending,
post-cleanup, and skipped-source-row negative cases for Battle Chase, Dungeon
Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1, and zPrix. Live-event
spawn/vendor/lifecycle/cleanup behavior remains blocked until per-event bundles
prove concrete timing and cleanup semantics.
`Start-BlockerEvidenceHarness.ps1 -QuestVirtualLootSmoke` now creates the
LWS-055 quest virtual-loot target worksheet, defaults representative objectives
`13011`, `12605`, `6700`, `6313`, and `21278`, and captures inactive objective,
pre-activation kill, completed-objective repeat, and declined/abandoned loot
negative cases. Quest-loot behavior remains at the current safe
`QuestObjectiveActive`/`VirtualCollect` preservation boundary until real bundles
prove cadence, probability, source selection, and objective integration.
Focused loot regressions now pin that boundary: quest virtual loot groups require
`IsActiveObjectiveId` for condition `8`, and virtual-item loot delivery updates
`QuestObjectiveType.VirtualCollect` plus the mapped loot grant without adding
new cadence/probability/source-selection behavior.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now dry-runs the
LWS-051 through LWS-055 harness presets with `-CreateBundleOnly` and verifies
their manifests, target worksheets, default ids, helper files, and negative-case
scaffolds. This protects the capture workflow only; Dust Stalker timing,
Arcterra/Palaver source-only behavior, Skyplot housing behavior, live-event
lifecycle, and quest-loot retail cadence remain blocked pending real bundles.
The same dry-run guard now covers `-Lws036ChecklistSmoke` and
`-NewZoneAssetProof`, verifying their manifests, target worksheets, default
world ids, helper files, and negative-case scaffolds. This protects the capture
workflow only; LWS-036/LWS-037 still need completed client/server smoke, and
LWS-040 through LWS-043 still need row-level client-asset/runtime-placement and
safety proof before any blocked or runtime-rejected row can be promoted.
The current implementation-plan pass leaves remaining unchecked rows only where
they are explicitly mapped-only, rejected, or blocked on the next evidence
source; LWS-036 through LWS-125 are tracked with one of those states rather than
treated as complete as a group. The
last verification pass reran Python syntax checks for the LaughingWS
extractors/analyzer, repaired analyzer emission of the row reconciliation CSV,
and reran the scratch analyzer/row-review queue path against the local
LaughingWS external snapshot plus official worlddb comparison. The scratch
audit produced `957,167` reconciliation rows and `7,749` queue rows, and all
nine regenerated promoted LaughingWS seed SQL files matched the tracked seeds by
SHA-256. Prior focused xUnit gates remain the script verification surface:
path/Fortune `56/56`, PvP/adventure `29/29`, expedition `78/78`, dungeon
`149/149`, and raid/event-instance `169/169`. No new data overlay,
cross-service packet, storefront, or persistence code path was introduced by
this closure-matrix pass.
`-DungeonSmoke` creates the targeted LWS-100 through LWS-106 worksheet for
Coldblood Citadel, Protogames Academy, Ruins of Kel Voreth, Stormtalon's Lair,
Skullcano, Sanctuary of the Swordmaiden, and Ultimate Protogames dungeon. It
defaults the map-script-backed worlds/events and records negative cases for
premature triggers, premature doors/platforms/launchers/teleporters,
wrong-route optional objectives, completion-only cinematics, and early
finish/reward behavior; these rows remain mapped-only until completed bundles
or decompile-backed payload proof exists.
`-RaidEventSmoke` creates the targeted LWS-110 through LWS-117 worksheet for
Initialization Core Y-83, Red Moon Terror, Genetic Archives, Datascape, Ultimate
Protogames raid, Shade's Eve, Protostar SuperMall in the Sky, and Journey into
OMNICore-1. It defaults the map-script-backed worlds/events and records negative
cases for premature triggers/target sets, premature doors/elevators/gates/rooms,
wrong-route weekly/wing/room selection, completion-only cinematics, and early
finish/reward behavior; these rows remain mapped-only until completed bundles
or decompile-backed payload proof exists.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now also dry-runs
the shared public-event vote/scoreboard, public-event objective notification,
PvP/adventure, expedition, dungeon, and raid/event-instance presets with
`-CreateBundleOnly`. It verifies their
manifests, target worksheets, default world/public-event/objective ids, helper
files, and negative-case scaffolds; this is capture-workflow coverage only, and
the affected LWS-071 through LWS-073, LWS-080 through LWS-085, LWS-090 through
LWS-096, LWS-100 through LWS-106, and LWS-110 through LWS-117 rows remain
blocked or mapped-only until completed bundles or decompile-backed payload proof
exists.
LWS-070's evidence harness is now implemented: `Start-BlockerEvidenceHarness.ps1`
creates timestamped blocker-evidence bundles with manifests,
command/log/client-observation/negative-case templates, screenshot/video/log
folders, and log-tail/collection helpers. Content smoke gates remain open until
real bundles are captured.
LWS-043's sandbox-zone checklist remains runtime-rejected: all `23`
test/unknown/unfinished/not-in-client files (`2,786` insert rows) remain
runtime-rejected as `sandbox_only_client_asset_check_required` until a future
explicit sandbox investigation supplies client-asset, placement, negative-case,
and safety proof.
LWS-040 through LWS-042 remain mapped-only new-zone blockers in
`Decomp/Analysis/LAUGHINGWS_NEW_ZONE_BROAD_ROW_REVIEW.md`: Dreadmoor (`1,256`
insert rows), Halon Ring broad residuals (`1,692` insert rows), and Murkmire
(`841` insert rows) remain `blocked_laughingws_new_zone_broad_rows`; only the
curated Halon city/transport crumbs are covered by the city-content seed.
`Tools/DataMapping/test_laughingws_worlddb_classifications.py` now guards these
WorldDB decisions with the actual analyzer recommendation logic: broad new-zone
sources remain blocked, all `23` test-zone files remain sandbox-only,
Dominion/Exile Arkship stay historical-only, Rider's Reef `New Tutorial.sql`
stays skipped rather than additive, the reviewed Algoroc/Celestion/Galeras/
Thayd/Whitevale/Deradune/Ellevar residual files stay blocked or rejected, and
the partial small-world seed sources keep their residual blocker/rejection
labels instead of regressing into additive import candidates.
Algoroc's row-level review retained `5` official-only rows and blocked `64`
branch-only queue rows as `blocked_laughingws_algoroc_residual_rows`.
Celestion's row-level review retained `19` official-only queue rows, rejected
`2` zero-coordinate placeholder rows, and blocked `50` branch-only queue rows
as `blocked_laughingws_celestion_residual_rows`. Galeras's row-level review
blocked `41` branch-only queue rows as
`blocked_laughingws_galeras_residual_rows`. Thayd's row-level review keeps the
housing intro branch entities covered by the city-content seed, retains `1,646`
official-only queue rows, and blocks `25` branch-only vendor queue rows as
`blocked_laughingws_thayd_residual_rows`. Whitevale's row-level review retains
`15` official-only queue rows and blocks `105` branch-only queue rows as
`blocked_laughingws_whitevale_residual_rows`. Levian Bay's row-level review
keeps the already-covered `14` checklist rows in the small-world seed, retains
`20` official-only entity rows, and blocks `115` branch-only residual queue
rows as `blocked_laughingws_levian_bay_residual_rows`. Everstar Grove's
row-level review keeps the already-covered `2` Exo-Lab 71 checklist rows in
the small-world seed, retains `3` official-only entity rows, and blocks `59`
branch-only residual queue rows as
`blocked_laughingws_everstar_grove_residual_rows`. Auroria's row-level review
keeps the already-covered `67` checklist rows in the small-world seed, retains
`74` official-only entity rows, and blocks `56` branch-only residual queue rows
as `blocked_laughingws_auroria_residual_rows`. Crimson Isle's row-level review
keeps the already-covered `24` checklist rows in the small-world seed, retains
`24` official-only entity rows, and blocks `74` branch-only residual queue rows
as `blocked_laughingws_crimson_isle_residual_rows`. Deradune's row-level review
retains `505` official-only queue rows and blocks `108` branch-only queue rows,
including `47` checklist-index rows, as
`blocked_laughingws_deradune_residual_rows`. Ellevar's row-level review retains
`51` official-only queue rows and blocks `129` branch-only queue rows,
including `65` checklist-index rows, as
`blocked_laughingws_ellevar_residual_rows`. The remaining
`extract_additive_world_overlay` audit bucket is `0`
broad/residual files after Evil from the Ether, Datascape, Algoroc, Celestion,
Galeras, Thayd, Whitevale, Deradune, and Ellevar residual active rows were reviewed and
classified as blocked instance-entity residuals rather than generic extraction
candidates, the new-zone broad rows were marked blocked, and the Isigrol
duplicates plus Thayd/Illium/Levian Bay/Everstar Grove/Auroria/Crimson
Isle/Northern Wilds/Wilderrun reviewed
residuals were removed from the unresolved bucket.
It also now treats the branch Rider's Reef `New Tutorial.sql` file as
superseded/audit-only instead of an additive-overlay candidate because the
official `New Player Experience.sql` import already supplies the active
build-16042 rows and the branch-only combat rows are marked testing-only or
wrong-faction in source.
The audit also now keeps all `23` `Test zones` files as sandbox/client-asset
work only, rather than suggesting additive runtime extraction for test maps,
unfinished zones, unknown areas, or `Not in Client` SQL.
`Tools\Setup\Initialize-NexusForever.ps1` imports these overlays after the
primary runtime seed when present; when a LaughingWS entity seed hash changes, setup
clears only that seed's owned deterministic high-ID range before reimport. The
generated seeds intentionally omit DDL, deletes, raw `REPLACE`, and foreign-key
disable blocks. `verify_safe_world_imports.sql` now has expected-count mismatch
metrics for every promoted LaughingWS overlay slice, not only the largest WIP
entity seeds.

### F-030 - Realm / Character Select/List/Transfer - PARTIAL
Character list/select works. Realm transfer returns an empty compatibility
destination list. Current-realm select is ignored for client safety. Unknown
transfer target returns `InvalidRealm`, offline target returns ServerDown, and
online transfer returns conservative `Internal` until handoff is implemented.
When the session has a logged-in player or loaded character list, realm-transfer
requests for a different character now return `InvalidCharacter` before target
realm status is considered; sessions without loaded character data retain the
existing compatibility fallback.
`ClientInitiatePTRCharacterCopy` (`0x06E7`) carries selected
character id, `ClientPtrCopy` (`0x06E8`) is a mapped native size-1 empty
payload, and `ServerPtrCharacterCopyQueued` (`0x06EA`) is a mapped empty
payload whose native consumer `140020ea0` fires Lua `PTRCharacterCopyQueued`.
`ServerRealmTransferDestinationsAux` (`0x03EF`) now uses its mapped reader
envelope (`uint32` plus counted raw bytes) instead of a fixed raw 0x10 payload.
The 2026-06-09 cached-export/source recheck kept `0x03EF` mapped-only: cached
`WildStar64.exe` fragment
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/14007d790.fragment.c`
reconfirms `ServerRealmTransferDestinationsAux_ReadPayload` (`14007d790`)
reads one `uint32`, one `uint32` byte count, allocates that many bytes, and
copies raw bytes into the pointer-backed buffer. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `0x03EF` size `0x10` with null write/handler slots and
the same reader is also registered for `0x01B4`; selected xrefs are label-only
and selected call edges show only bit-read, allocation, and raw-copy helpers.
Current source still emits the structured `ServerTransferDestinationRealmList`
empty compatibility response for `ClientGetRealmTransferDestinations`, not
`ServerRealmTransferDestinationsAux`. Payload semantics and producer timing
remain blocked pending a native producer/apply path, callback/table owner,
dynamic dispatch proof, accepted realm-transfer packet capture, or server
catalog/realm-list artifact that explains the raw byte payload.
The 2026-06-09 PTR queue/copy handoff cached-export/source recheck kept
`0x06EA` mapped-only: cached `WildStar64.exe` fragments under
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
reconfirm `ClientInitiatePTRCharacterCopy_SendFromLuaDispatch` (`140022270`)
as the character-select `0x06E7` sender followed by `ClientEncrypted` `0x0244`,
`ClientPtrCopy_SendFromLuaDispatch` (`14063f540`) and
`ClientPtrCopy_SendFromLuaDispatch2` (`140707d80`) as separate zero-byte
`0x06E8` senders, and `ServerPtrCharacterCopyQueued_DispatchLuaEvent`
(`140020ea0`) as the client consumer that dispatches Lua
`PTRCharacterCopyQueued`. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x06EA` size `1` to `ServerEmpty_ReadPayload`; adjacent
`ServerPtrCharacterCopyQueued_WritePayloadCluster` (`14007dd40`) is registered
for `0x0592`, not `0x06EA`. Selected xrefs/call edges show `0x06E8` send-helper
calls and `0x06EA` client event dispatch only; no native producer, queue state
mutation, copy mutation, or accepted payload proves when NexusForever should
emit `ServerPtrCharacterCopyQueued`.
Focused PTR packet tests pin both empty shapes and verify PTR-copy handlers do
not emit queue notices until handoff timing is mapped. Real transfer
destinations/success results, `TransferFlag` semantics, `0x03EF` payload
contents, PTR queue producer timing, copy mutation, new realm notices, and
optional realm-message/admin packets remain incomplete.

### F-031 - Fortune Minigame - EMULATOR IMPLEMENTED / RETAIL WEIGHTS BLOCKED
4 handlers functional, session persistence code path via `account_fortune_session`,
reward payouts with account item grants, card flip state tracking.
Weighted emulator rarity-tier pool + `ServerFortuneRewards.RewardItemProbabilities`
match the mapped retail client UI transport (`FortunesLib.GetFortunesLootList`
reads server floats and shows `fProbability = value * 100`; native evidence
2026-05-23). Native chain follow-up on 2026-06-02 maps `socket+0x15a8`
`FortuneNode_ApplyServerFortunePackets` to server opcodes `0x03CF`-`0x03D2`
and names `ServerFortuneCardUpdate.HasUpdate`.
Focused Fortune session tests now guard that runtime flip updates preserve
`HasUpdate=true`, matching `Fortune_ApplyCardUpdate`'s early-return behavior
when the leading bool is false. Payout coverage now also pins that flipped-card
account-item grants carry the current character target identity and explicitly
set the account-inventory `hasTargetPlayerIdentity` flag.
Flip requests now fail closed with the mapped click-empty reset when account
inventory delivery is unavailable, before marking a card flipped or emitting a
card update.
2026-06-04 live local UI logs showed storefront/reward-rotation refreshes but no
Fortune notify/start packets before a chest drag onto the pedestal; reward
rotation index `0` now keeps the existing storefront-catalog ordering and sends
Fortune status/cards through `IFortuneSessionManager` so the UI has a
current/reset Fortune state before chest placement.
Follow-up live logs showed `ClientFortuneStart` reached the server, then reset
because account `1` had no spendable Fortune Coin (`AccountCurrencyType=5`) row
and only claimable Fortune Coin account-item bundles. `FortuneSessionManager.Start`
now auto-claims a matching `CanClaim` Fortune Coin account item before
re-checking and debiting the one-coin start cost. The 2026-06-07 follow-up pins
the character-target boundary: a Fortune Coin bundle targeted at another
character is not auto-claimed, no currency is added or debited, the account
inventory row remains, and the client receives the mapped click-empty reset.
A subsequent live client crash after the deal persisted cards `3215`, `166`,
and `26`; local `wildstar_client.accountitem` data showed `166` and `26` are
entitlement-only rows with `item2Id=0`. The crash address maps to
`0x14078A28B` inside `FUN_14078a1a0`, which `Fortune_ApplyCards` calls for each
displayed card and which dereferences the resolved card display object at
`+0x158` without a null guard. `FortuneRewardPool` now deals only item-backed
rows (`Item2Id != 0`) so `ServerFortuneCards` does not send non-displayable
account rewards into the native card UI, and `FortuneSessionManager` discards
stored sessions containing non-displayable card ids before sending page-load
status. `Tools/Setup/sql/runtime_auth_seed.sql` now clears transient stored
Fortune sessions during local setup so stale pre-guard card rows do not survive
fresh seed/import flows.
2026-06-04 Ghidra MCP recheck reconfirmed `Fortune_ApplyRewards` only copies
server-provided item/money probability arrays into UI state; it does not expose
an active rotation source. The same pass maps `Fortune_ApplyReset` value `3` to
the click-empty reset path, so `ServerFortuneReset.Unknown` is now
`ResetCode`.
2026-06-09 cached-export/source recheck found no running Ghidra MCP instance and
reconfirmed the selected `WildStar64.exe` fragments as transport/consumer
evidence only: `ServerFortuneRewards_ReadPayload` (`140081f60`) reads item2 ids,
money rows, and parallel item/money probability arrays; `Fortune_ApplyRewards`
(`1407292a0`) copies those server-provided arrays into Fortune UI state;
`FortunesLib_GetFortunesLootList` (`140766370`) exposes cached item2 rows and
shows `fProbability = serverFloat * 100`; `FortuneNode_ApplyServerFortunePackets`
(`1404d60f0`) dispatches `0x03CF`-`0x03D2`; `ServerFortuneCards_ReadPayload`
(`1400a0b10`) remains the card-state reader. Current source still emits
`ServerFortuneRewards` from the emulator rarity-tier `FortuneRewardPool`, and no
retail active-rotation or per-item weight source surfaced.
F-007 `RewardRotation*` table/packet labels are now explicitly rejected as
Madame Fay active-rotation evidence: they explain the separate
reward-rotation/storefront schedule surface and bootstrap ordering, while
Fortune catalog probabilities still enter the UI only through
`ServerFortuneRewards`.
`Decomp/Analysis/FORTUNE_WEIGHT_AUDIT.md` verifies the local
catalog/table state, current Fortune tests, and the rejection of the
`Questing-and-more` branch's old hardcoded gacha handler as non-evidence for
retail weights/rotation.
**Blocked:** exact per-item retail weights and active rotation catalog (no
`AccountItem.tbl` weight column; no retail `ServerFortuneRewards` or
storefront-server catalog capture). `Start-BlockerEvidenceHarness.ps1`
`-FortuneRewardsSmoke` now creates the targeted LWS-066 capture worksheet for
that packet/catalog proof plus card/payout/reload negative cases.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` dry-runs the preset
and verifies the manifest, worksheet, helper files, and negative-case scaffold;
exact weights still require a real retail/catalog evidence bundle. Focused
`FortuneRewardPoolTests` now exercise the real pool with synthetic
`AccountItem.tbl`/`Item2.tbl` rows so the mapped item2 probability catalog stays
separate from Fortune Coin/non-item account rewards and dealt cards stay
item-backed without promoting retail rotation parity.
The real pool now also treats a missing `AccountItem` table as an empty
catalog/card source and a missing `Item2` table like a missing item row with
Normal rarity fallback, preserving item-backed card-only behavior without
changing weights or rotation.
Focused Fortune/reward-rotation boundary coverage passed 32/32 after the
false-source cleanup; focused `FortuneRewardPoolTests` passed 6/6 and the
focused Fortune suite passed 27/27 after the reward-pool partial-table guard.

### F-032 - Leaderboards - COMPLETE
Real database-backed pipeline (NOT empty stub). `DatabaseLeaderboardStore`
with MySQL, per-scope/category cache limiting, ranking, personal placement,
and score ingestion. `LeaderboardAggregation` keeps 50 visible rows while
preserving the viewer's personal placement row, and request handling scopes PvE
by type/map/prime level and PvP by arena/team or battleground class category.
Duplicate persisted scores for the same character now dedupe to the best
score before per-scope/category row caps and visible response ranking.
If the character database is unavailable during a request, the database store
now returns an empty response without caching that empty state, so later
requests can hydrate once the DB is available. Focused leaderboard verification
passed 17/17. A 2026-06-08 source/protocol recheck keeps season and
medal-filter semantics blocked: `ClientLeaderboardPveRequest` only reads
type/map/prime selectors, `ClientLeaderboardPvpRequest` only reads type, and
the provider/store currently scope only by those mapped selectors. Response
rows still carry `RewardedTier`, but no mapped request field proves
bronze/silver/gold or season filtering.
Known retail gaps remain for exact retail row caps/refresh cadence and broader
live score-ingestion coverage.

### F-033 - Challenges - COMPLETE
609-line `ChallengeManager`. 4 choice types: Activate, Abandon, AcceptShared,
DeclineShared. Full lifecycle: activation, overflow-safe tier progress clamping,
tier advancement (3 tiers),
completion, sharing (30s timeout), retail two-active-challenge cap through
`RetailCertainRules.MaxConcurrentActiveChallenges`, combat kill progress through
direct/nested target groups, and `QuestObjectiveType.CompleteChallenge` hook. DB
persistence. Missing `Challenge` data now returns `GenericFail`, and missing
`ChallengeTier` data resolves tier goals to zero rather than crashing during
partial game-table loads. Large progress deltas now clamp at the current tier
goal without `uint` wrap; focused challenge-manager verification passed 12/12.
A 2026-06-08 source/protocol recheck keeps reward tracks, medal win-chance, and
share-init ownership blocked: `ChallengeEntry.RewardTrackId` and
`RewardTrack*` tables exist, but no challenge reward-track runtime/packet
surface is mapped; `ServerChallengeUpdate` only carries tier/count/timer fields
and `ServerChallengeResult.Data` only carries the mapped tier/localized-string
value; `ShareWithTarget()` is source-local with no mapped client share-init
request. Focused challenge verification passed 45/45.
Known retail gaps remain for `Client0x00C8` decode and full result/reward
parity beyond the mapped tier/result packets.

### F-034 - Datacubes / Journals / Galactic Archive - COMPLETE
`GalacticArchiveManager` handles unlock, view, rule-based auto-unlock
(achievement/path/quest rules with ALL/ANY flag), archive-link child unlock
authorization through `ArchiveLink.tbl`, and DB persistence. Partial game-table
loads now fail closed: missing `ArchiveArticle` data skips persisted archive
state and rejects unlock/view requests, missing `ArchiveEntryUnlockRule` data
sends login refresh without rule auto-unlocks, and missing `ArchiveEntry` data
skips entry-title rewards without losing article unlock state.
Persisted duplicate datacube rows for the same id/type now merge progress
instead of throwing during character load; missing `Datacube` and
`DatacubeVolume` tables now reject new datacube/journal additions through the
existing invalid-row path before state mutation or packet emission. Focused
archive/datacube verification passed 18/18, and broader PathManager coverage
passed 64/64.
`ServerGalacticArchiveRefresh` + `ServerGalacticArchiveUpdate`.
Known retail gaps remain blocked for broader content hookups, full
journal/datacube progression semantics, path-mission rule parity, and wider
Codex UX coverage until a pickup-chain/live-client or native rule evidence
bundle proves the missing semantics.

### F-035 - Achievements - COMPLETE
Generic base manager with checklist/value progress tracking, prerequisite
checks, completion marking, and clamped scalar progress. Realm-first tracking
loads existing character/guild completions, claims each realm-first achievement
once per realm process, and broadcasts the mapped achievement/guild/name packet
on first completion. Guild achievement forwarding and title grants on completion
are implemented. Runtime trigger callsites exist for kills/kill groups,
quests/contracts, zone entry/map completion, activation/discovery/secret stash,
crafting/tradeskills, costume/costume-set unlocks, reputation, level/path level,
path mission/type, currency/account currency/primal essence, items/titles,
duels, critical deathblows, guild/circle/group/friend joins, housing plug/decor
actions, public-event objectives, and targeted emotes.
Checklist bit indexes outside the 32-bit progress mask now fail closed instead
of aliasing to bit 0; focused achievement verification passed 25/25.
Achievement startup now also treats a missing `Achievement` primary table as an
empty cache and missing character DB registration as no persisted realm-first
preload; focused global achievement verification passed 3/3 and broader
achievement verification passed 32/32.
`ClientSteamAchievements` remains diagnostic-only: a 2026-06-08 recheck pins the
current packet grammar to `SteamGameId`, byte length, and ASCII payload, and the
handler logs without emitting packets or mutating achievement state. Focused
Steam achievement verification passed 2/2. Known retail gaps remain for
data-only/unowned achievement type families, Steam achievement payload grammar
and achievement-id correlation beyond the raw ASCII payload, exact realm-first
UI/timing semantics, and achievement UI edge cases.

### F-036 - Zone Maps / Zone Completion - COMPLETE
Hex group discovery, movement-based rate limiting (10 unit^2 threshold),
`WorldZone` parent chain + `MapZoneWorldJoin` fallback. Zone completion
with `AchievementType.MapComplete`, `ZoneCompletionRewardResolver`,
faction-aware `ZoneCompletionFactionEnum` row selection, and
quest/challenge/datacube/tale/journal threshold gating through
`ZoneCompletionProgressTracker`. DB persistence. Partial game-table loads now
fail closed: missing persisted `MapZone` rows are skipped, missing hex-group
tables leave maps incomplete with zero explored percent, and missing
quest/datacube/volume/world-zone progress tables resolve as no completion
progress rather than crashing or granting titles. `ZoneCompletionProgressTracker`
now recomputes `EpisodeQuest` and `MapZone`/`WorldZone` progress inputs from
the current `GameTableManager` on each lookup, so an earlier partial table load
cannot cache an empty result across a later complete provider refresh, and
world-zone parent traversal is cycle guarded.
Generic-map node request/choice handlers also fail closed during setup or
partial game-table loads: missing `GenericMapNode` tables are ignored like
unknown rows, and missing `WorldLocation2` tables prevent destination teleports
without emitting node state.
A 2026-06-08 source/table recheck keeps non-title rewards and path-specific
completion rewards blocked: `ZoneCompletionEntry` exposes only
`CharacterTitleIdReward`, and `ZoneMapManager.TryGrantZoneCompletionRewards()`
only awards titles through `ZoneCompletionRewardResolver.TryGetTitleReward()`.
Focused zone-completion verification passed 12/12 and broader map verification
passed 457/457.
Known retail gaps remain for full zone-completion UX/timing semantics,
path-specific completion semantics, and live-client validation of category
totals. Generic-map content-context producer semantics remain blocked
separately from the conservative request/choice handler path.

---

## Remaining Blocked Items

This table is the consolidated **runtime producer/emit** backlog. Broader partial
features (guild bank, taxi routes, spell families, STS crypto, reward rotation,
and others) stay in their `F-00x` sections and in
`Decomp/Analysis/MISSING_FEATURE_MATRIX.md`.

### Code audit (2026-05-23): not implemented despite occasional "ghost gap" claims

| Claim | Code reality |
| --- | --- |
| Entity-stat aux emitted from `Player.AddVisible()` | **False.** Only entity-**create** aux (`0x025F`..`0x0264`) is emitted; stat aux (`0x0889`..`0x093E`) has no production emitter. |
| `HousingNeighborhoodEmitter` on residence `AddEntity` | **False.** `ServerHousingNeighborhoodEntry` / `List` are modeled and shape-tested only; no `Game`/`WorldServer` send site. |
| `SendCraftingStatsUpdate` / `SendCraftingQualityUpdate` before craft success | **False.** `SendCraftSuccess` emits `ServerCraftingFinish` only; `0x084B`/`0x0855` are modeled-only. |
| `TryCompleteFixedRecipe(chargeCounts:)` / `CalculateTotalCharges()` | **False.** Complex craft logs `ChargeCounts` but calls `TryCompleteFixedRecipe` without applying them. |
| `ClientItemContextAction` delegates to `ClientItemUse` | **False.** Handler validates/logs; `SelectedBranch` stays diagnostic-only. |
| LFR `SetInProgress` + server queue integration | **False.** `0x05D5`/`0x0602` are validation/logging only; replacement backfill was removed per evidence rollback. |
| `ServerRaidQueueStatus` never sent | **Partially false.** Emitted with raid-info as zero-value compatibility; shared row field names are mapped, but standalone queue-position semantics and non-zero timing remain blocked. |
| Source TODO/FIXME/NotImplemented search proves unfinished production code | **False after 2026-06-04 audit.** Scoped C# searches now leave no production TODO/FIXME/NotImplemented markers. Remaining hits are test doubles, explicit unsupported guardrails, WIP content docs, or blocked tracker entries with named evidence gates. |

| # | Feature | Gap | Evidence status (latest) | Blocker |
|---|---------|-----|------------------------------|---------|
| 1 | F-025 | Entity-stat aux: 0x0889, 0x08CC, 0x08F4, 0x0939, 0x093D, 0x093E | **Mapped** (wire + XML); no production emit; 2026-06-04 immediate scans added durable registration anchors for `0x0939`/`0x093D`/`0x093E` and still found registration/reader evidence only; 2026-06-04 source audit found no `ServerEntityStat*` aux constructors outside tests/models; 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance, reconfirmed `140080bf0`, `1400980f0`, `140097620`, `140097ee0`, `140097690`, and `140097f70` as reader-only fragments, and again found no production source emitter outside models/tests/negative guards; stale candidate `FUN_140939650` rejected as viewport/grid global math rather than a packet consumer; focused aux/visual/tracking slice passed `28/28`; pass 117 placeholder guard keeps neutral aux field names until semantic proof and passed `91/91` | Per-opcode `vtable+0x58` apply handler, apply-table classification, or sniff order |
| 2 | F-025 | Map-tracked-unit producer timing and TrackingSlot selection | **Mapped** consumer chain (`TrackingSlot.tbl`); **Implemented** lookup/selection guard tests; **Blocked** producer; Ghidra MCP recheck 2026-06-04 found client cache update/remove only and duplicate objective groups reject objective-only slot selection; 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance, reconfirmed selected `WildStar64.exe` fragments as read/apply/Lua consumer-only, and found no production `ServerMapTrackedUnitUpdate` / `ServerMapTrackedUnitDisable` emitter in source; pass 118 guard coverage passed `97/97` | Native server send site or public-event marker sniff for `0x0849`/`0x0848`; `TrackingSlotHelper` remains one-way table lookup only |
| 3 | F-004 | NeighborhoodEntry/List (0x0501/0x0506) | **Mapped** row fields (`NeighborhoodWireUInt*`); **Blocked** NF emit; Ghidra MCP recheck 2026-06-04 confirmed consumer-only cache apply and rejected adjacent community/visit senders; 2026-06-04 runtime cleanup removed the unrelated hardcoded `ServerHousingProperties.Residence.NeighbourhoodId` placeholder; 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance, reconfirmed `14009cbe0` / `14009ebf0` reader shapes, `1404ba4f0` cache apply plus `HousingNeighborhoodRecieved`, and `140205900` table-loader-only evidence, and found no production source emitter; pass 119 rejects `HousingNeighborhoodInfo.tbl` column-name synthesis and focused guard coverage passed `106/106` | Live housing UI/realm-login sniff or native server-push path before `Housing_HandleNeighborhoodList` @ `1404ba4f0`; no `Housing_SendClient*` label for `0x0506` trigger yet |
| 4 | F-008 | Discovery/station/complex-craft semantics and 0x084B/0x0855 emit intent | **Mapped** aux wire; 2026-06-04 Ghidra MCP recheck found registration/readers and finish/current-craft consumers only; 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance, reconfirmed `0x084B` / `0x0855` reader-only evidence, `ServerCraftingCurrentCraft` client-state apply plus `CraftingUpdateCurrent` dispatch, and `0x056C` item microchip patch/apply evidence only; discovery mutations disabled; service keys `0x2C`/`0x4F`/`0x57` diagnostic; durable rune bridge implemented; distinct C2S microchip-install mutator rejected because client install is `0x085B` while `0x056C` is a server item patch | Native aux/current-craft producer path or live crafting capture for enqueue/timing, discovery unlock/hot-cold proof, non-success sigil rules, and `ServerItemMicrochips` (`0x056C`) producer timing |
| 5 | F-031 | Per-item retail Madame Fay weights and active rotation | **Mapped** catalog transport; emulator `RewardRarity` tiers audited; 2026-06-04 Ghidra MCP recheck found no active-rotation source, renamed `ServerFortuneReset.ResetCode`, and payout grants now target the current character identity; 2026-06-09 cached-export/source recheck found no running Ghidra MCP instance, reconfirmed `140081f60`, `1407292a0`, `140766370`, `1404d60f0`, and `1400a0b10` as transport/consumer evidence only, and found current source still emitting `ServerFortuneRewards` from emulator rarity-tier `FortuneRewardPool` without a retail active-rotation or per-item weight source; F-007 `RewardRotation*` labels rejected as Fortune active-rotation evidence | Retail `ServerFortuneRewards` capture or storefront-server catalog dump (`FORTUNE_WEIGHT_AUDIT.md`) |

---

## Ghidra Decomp Addresses (for next pass)

### Entity Aux Readers (field-level shapes tracked against C# models)
| Address | Function | Handles | Field Shape |
|---------|----------|---------|-------------|
| 0x140095c20 | ServerEntityCreateAuxRow_ReadPayload | 0x025F (44 bytes) | ushort + uint + 3xuint32 + uint + 3xuint32 + uint + uint |
| 0x140095ce0 | ServerEntityCreateAuxRowList_ReadPayload | 0x0260 | counted rows of 0x025F |
| 0x140095a80 | ServerEntityCreateAuxBitPackedRowList_ReadPayload | 0x0261 | counted rows of 0x0263 |
| 0x1400959c0 | ServerEntityCreateAuxBitPackedRow_ReadPayload | 0x0263 (36 bytes) | uint + uint + 3x17bit + uint + 3xuint32 |
| 0x140095b40 | ServerEntityCreateAuxScalarList_ReadPayload | 0x0264 (24 bytes) | uint + ushort + uint + counted uint list |
| 0x140097620 | ServerEntityStatUInt32UInt5UInt32_ReadPayload | 0x08F4/0x087F/0x0938 (12 bytes) | uint + 5-bit + uint |
| 0x140097690 | ServerEntityStatUInt32UInt5Pair_ReadPayload | 0x093D (16 bytes) | uint + 5-bit + uint + uint |
| 0x140097ee0 | ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload | 0x0939 (24 bytes) | uint + 14-bit + 18-bit + wide string |
| 0x140097f70 | ServerEntityStatTwoUInt32UInt64_ReadPayload | 0x093E (16 bytes) | uint + uint + uint64 |
| 0x1400980f0 | ServerUInt32WideString_ReadPayload | 0x08CC and others (16 bytes) | uint + wide string |

### Registration Table Entries (from Ghidra decomp at 0x14006c290)
The registration function stores opcode-to-reader mappings in a lookup table.
Helper scripts can extract candidate server-to-client reader addresses from
that table, but semantic producer/consumer dispatch rules remain blocked until
candidate labels are reviewed individually.

### Group/Raid Readers (decompiled, confirmed)
| Address | Function | Handles |
|---------|----------|---------|
| 0x1406031d0 | Group_HandleMemberAdd_ReadPayload | Member role/promote (dispatches Group_MemberPromoted) |
| 0x140603380 | Group_HandleMemberRemove_ReadPayload | Member kick/leave (calls Group_CopyMemberStatBlock) |
| 0x140603450 | Group_HandleMemberPromote_ReadPayload | Member detail update (5 role fields at 0xd0-0xe8) |
| 0x140603610 | Group_HandleMemberFlags_ReadPayload | Member flags + quest tracking (860 bytes) |
| 0x140603970 | Group_HandleReadyCheck_ReadPayload | Ready check (dispatches Group_ReadyCheck, 60s timeout) |
| 0x140603a60 | Group_HandleGroupDisband_ReadPayload | Roster update / disband (2116 bytes) |

### Next Decomp Targets (not yet labeled)
| Address | Hypothesized Role |
|---------|-------------------|
| 0x14008bf80 / 0x14008c010 | ServerRaidQueueStatus (0x0718) shared row reader plus ServerRaidInfoResponse (0x071A) count-array reader; fields mapped via Group_DispatchRaidInfoResponse, standalone queue timing blocked |
| ~0x1404dbxxx | Crafting complex craft stat consumer (`0x084B`/`0x0855`) |
| ~0x140095xxx | Entity auxiliary 0x025F-0264 registration block |
| ~0x140097xxx | Entity auxiliary 0x0889-093E registration block |
| ~0x140605xxx | Looking-for-replacements match handlers |

---

## Build Commands

```powershell
# Game assembly (when server is running)
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo

# Script.Main (quest/map scripts)
dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo

# Full test suite
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v minimal --nologo

# Ghidra decomp (from PowerShell, NOT bash)
powershell -NoProfile -ExecutionPolicy Bypass -Command `
  "& { Set-Location 'I:\GIT\NexusForever'; & '.\Decomp\Analysis\run_ghidra_analysis.ps1' -Targets 'WildStar64.exe' -MaxDecompiledFunctions 1000 -DecompileMode Force -ProjectLayout PerTarget }"
```

## Database

```
MySQL 8.0: C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe
Jabbithole: host=127.0.0.1, user=bankai, pass=bankai, db=wildstar_client
NexusForever: host=127.0.0.1, user=bankai, pass=bankai, db=nexus_forever_world
```

## Key Paths

```
Repo root:        I:\GIT\NexusForever
Branch:           codex/evil-expedition-port
Script.Main:      Source\NexusForever.Script.Main\
Game:             Source\NexusForever.Game\
WorldServer:      Source\NexusForever.WorldServer\
Tests:            Source\NexusForever.Game.Tests\
Decomp:           Decomp\Analysis\
Ghidra:           C:\Users\Bankai\.codex\tools\nexusforever-decomp\ghidra_12.0.4_PUBLIC
Game tables:      Source\NexusForever.WorldServer\bin\Debug\net10.0\tbl\
Functions CSV:    Decomp\Analysis\function_labels.csv
Decomp cache:     Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\
```
