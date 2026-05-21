# Missing Feature and Opcode Decode Matrix

Updated: 2026-05-21

This is the system-by-system workboard for restoring retail features from
evidence. It complements `CONTINUATION_GUIDE.md`, `INITIAL_FINDINGS.md`, and
`plan-evidenceBackedRestorationEpic.prompt.md`.

The current opcode coverage baseline is structurally closed:

| Direction | Structural state | Semantic state |
| --- | --- | --- |
| Client | `347/347` models, no missing handlers | 17 `Client0xNNNN` rows are diagnostic-only. |
| Server | `692/692` models | 120 `Server0xNNNN` rows are shape-only output models. |
| Core | `3/3` models | No current backlog. |

Closing structural coverage does not mean retail feature support is complete.
Every row below should be treated as a feature task that must climb the evidence
ladder before mutating server state.

## Status Legend

| Status | Meaning |
| --- | --- |
| Structural | Packet shape or source surface exists, but the behavior is not decoded. |
| Diagnostic | Server parses or emits safe diagnostics only. |
| Compatibility | Server returns empty, failure, or conservative output so the client remains usable. |
| Partial | Some real state exists, but retail parity is incomplete. |
| Blocked | More client-reader, sniff, table, or runtime evidence is required. |
| Ready | The next task is implementation or verification, not more broad discovery. |

## How To Work A Row

1. Pick one system row, not one random opcode.
2. Decode only the opcodes and client functions listed in that row.
3. Produce a function map with binary, address, labels, fields, callers,
   matching NexusForever files, confidence, and blockers.
4. Rename packet/model fields only after read/write order and intent are mapped.
5. Implement the smallest server behavior that is proven by the evidence.
6. Verify with serialization tests, focused xUnit tests, and live smoke when
   the feature crosses runtime state.

## Master Feature Matrix

| ID | System / feature | Current support | Missing feature surface | Decode / evidence targets | Owning code surface | Next task |
| --- | --- | --- | --- | --- | --- | --- |
| F-001 | STS token crypto and optional auth routes | Login/account/game-token compatibility is implemented. | Token crypto handshake, external-account edge routes, and optional envelope fields remain blocked. | `StsConn_SendTokenKeyData`, `ServerRand`, `ServerPublicKey`, `ServerSignature`, `PremasterSecret`, `AuthnToken`; startup STS captures. | `Source/NexusForever.Network.Sts`, `Source/NexusForever.StsServer` | Keep blocked until crypto/token semantics are mapped safely. |
| F-002 | Client diagnostic receive opcodes | 17 conservative models plus diagnostic handlers exist. | The client request intent and server response behavior are unknown. | `Client0x003D`, `00C8`, `00ED`, `011B`, `011D`, `012D`, `0142`, `0550`, `062A`, `0634`, `063E`, `0701`, `0760`, `0762`, `07B6`, `07E3`, `0928`. | `ClientUnresolvedDiagnosticPackets.cs`, `ClientUnresolvedDiagnosticHandlers.cs`, `GameMessageOpcode.cs` | Decode client writer functions by feature cluster; keep handlers non-mutating until mapped. |
| F-003 | Server unresolved output opcodes | 120 structural `IWritable` models exist. | Packet names, field names, emit sites, and feature semantics are unknown. | See the "Server Opcode Decode Matrix" below. | `ServerUnresolvedOutputPackets.cs`, `GameMessageOpcode.cs`, packet tests | Work by adjacent named opcode cluster, then replace each structural model with a named packet. |
| F-004 | Housing residence, neighbors, communities, decor, plugs | Basic residence surfaces and handlers exist; several requests are compatibility/partial. | Neighbor permissions, roommate persistence, community placement/donation, decor/plugin prerequisites, interior wallpaper, vendor list, and visit state need retail parity. | Housing `Server0x00CA..00D1`, `010D`, `0110`, `04FE`, `0501`, `0506`, `0514`, `0516`, `0519`, `051F`, `053A`, `053B`, `078C`; `Housing_*` client labels. | `Source/NexusForever.Game/Housing`, `ResidenceMapInstance.cs`, housing handlers/models | Decode housing server outputs first, then implement persistent permission/decor/community state with tests. |
| F-005 | Marketplace, auction, commodity exchange, CREDD | Request payloads are mapped; handlers return deterministic empty/failure in many paths. | Real listing/search state, bid/post/cancel mutation, commodity order matching, CREDD exchange flow, and mail settlement are missing. | `Marketplace_*WritePayload`, `Marketplace_SendClient*`, `Server0x026A`, `06DF`, `07D3`, `07D5`; auction/commodity response readers. | `WorldServer/Network/Message/Handler/Marketplace`, marketplace models, mail manager | Build persistent auction/commodity store only after server result/update packets are decoded. |
| F-006 | Storefront, account inventory, pending item groups, generic unlocks | Catalog, supported purchase, account currency charge, and some claim paths exist. | Gifting, pending-group transfer/offline delivery, cooldown mutation, coupon policy, exact purchase result UI, and unsupported offer item effects remain blocked. | Account/store opcodes `0969..0991`; `ServerAccountItems*`, `ServerStore*`, `AccountInventoryItem_ReadPayload`, gift/claim client senders. | `Game/Account/Inventory`, `Game/Storefront`, `Database.Auth`, account handlers/models | Decode the `0969..0991` cluster into account/store packet names, then implement one pending-group path at a time. |
| F-007 | Reward rotation and reward properties | Placeholder/diagnostic provider exists. | Live schedule rows, entry-state rows, state/value semantics, and unsupported request indices remain blocked. | `ServerRewardRotation*`, `ServerRewardPropertySet`, `Server0x07CD`, `ClientRewardUpdateRequest`. | `Game/Account/Reward`, reward handlers/models | Capture or decode real schedule/entry-state semantics before generating non-placeholder rows. |
| F-008 | Crafting, tradeskills, rune crafting | Packet layouts and validation are mapped; many handlers are non-mutating. | Real craft success, material consumption, discovery rolls, output creation, station constraints, profession persistence, rune item data, and sigil result meanings are missing. | `ClientCrafting*`, `Tradeskill_SendClient*`, `Server0x084B`, `0855`, `ServerTradeskillSigilResult`. | `WorldServer/Network/Message/Handler/Crafting`, crafting models, supply satchel, item manager | Decode crafting result/update packets, then implement simple craft success with material and output tests. |
| F-009 | Rapid transport, taxi, flight path, vehicles | Pricing/request validation is partial; some vehicle embark/disembark behavior exists. | Service-token bypass, route state, taxi embark and completion, charge/teleport rules, passenger/seat modes, and vehicle deployable semantics are incomplete. | `ClientRapidTransport_WritePayload`, `ServerFlightPathUpdate`, `Server0x077E`, `ClientFlightPathPurchase`, vehicle server outputs near `01B2`, `08CC`. | `Game/Entity/PathManager`, player path handlers, vehicle handlers, spell vehicle effects | Decode flight/taxi read paths and correlate with route tables before charging or teleporting broadly. |
| F-010 | Group, raid, queue, matching | Group and matching handlers exist; queue/match runtime is partial. | Role checks, queue status timing, replacement/vote flows, raid info, group member flags, loot rules, cross-realm group data, and position updates need parity. | `Server0x0414`, `042A`, `0431`, `0436`, `0438`, `0441`, `045A`, `0461`, `0468`, `0718`; matching `Client0x062A`, `0634`. | `Game/Group`, `Game/Matching`, group/matching handlers/models | Decode group output cluster and queue status packets, then implement the smallest queue lifecycle with tests. |
| F-011 | Guild, recruitment, war party | Guild handlers and recruitment output models exist. | Bank transactions, perks, holomarks, standards, recruitment subscriptions, war party boss tokens/results, and warplot plug state need validation or implementation. | Recruitment outputs `0767..076D`, `ServerWarParty*`, guild handler request labels, nearby `Server0x077E` if correlated. | `Game/Guild`, guild handlers, war party models | Map recruitment/warparty readers, then choose one persistent guild subfeature. |
| F-012 | ICComm, chat, friendship, social options | ICComm has transient membership; friendship has partial account/block/note support; chat mostly works. | Entitlement checks, persistent channels, exact directed-vs-ordered delivery, leave/logout behavior, throttling, auto-response, ignore-strangers persistence, and some chat aux packets remain missing. | `Client0x0550`, chat `Server0x01B8`, `01C1`, `01C4`, `01EF`, `Lua_FriendshipLib_*`, `ClientFriendship*`. | `Game/ICComm`, `WorldServer/Network/Message/Handler/ICComm`, chat/friendship handlers | Decode chat/ICComm auxiliary outputs before adding persistent channel state. |
| F-013 | Mail | Mail request handlers exist. | COD settlement, attachment/cash transaction integrity, settlement from marketplace, return/delete state, and expiration behavior need parity. | Mail request handlers, `ServerMailResult`, `ServerMailItemDeprecation`, `Server0x05A1`, account gift/pending group flows. | `Game/Entity/MailManager`, mail handlers/models | Add transactional tests around attachment/cash movement before widening live mail settlement. |
| F-014 | Loot, bindcheck, rolling, master loot | Basic loot delivery and recent Rider's Reef fixes exist; request/diagnostic boundaries are mapped. | Real loot assignment, bind-on-pickup confirmation, group rolls, master-loot assignment, parent source tracking, and packet parity remain open. | `Loot_*` labels, `ServerLoot*`, `Server0x08A0`, `08A8`, `ClientLootAssignMaster`, `ClientLootRollAction`. | `Game/Loot`, loot handlers/models | Decode `08A0/08A8` and implement one group-roll flow with tests. |
| F-015 | PvP and duels | Transient duel lifecycle and PvP toggle exist. | Observer broadcasts, duel leash/warning state, countdown timing, PvP cooldown persistence, forced-map PvP, rewards/stats, and exact cancel/result semantics need parity. | Duel/PvP labels, `ServerDuel*`, `ServerPvpCooldown*`, `Server0x00EE` if correlated to duel/path boundary. | `Game/Pvp`, PvP handlers/models | Decode duel warning/result readers, then add countdown/leash tests. |
| F-016 | Spell runtime: procs | Conservative holder-side proc dispatch exists for mapped events/routes. | Unsupported trigger events and targetData tails, exact chance/cooldown ordering, recursion rules, and school-specific edge cases need validation. | `Proc.DataBits00..09`, `ProcRuntimeEvidenceCollector`, `!spell procreport`, `Server0x080F/0810/0812` if spell auxiliary. | `Game/Spell`, spell commands/tests | Validate event fixtures and widen only through `ProcDispatchEvidenceBoundary`. |
| F-017 | Spell runtime: damage/heal/shields/vitals | Many families are conservative or partial. | Retail formula parity, distance/distribution splitting, shield/absorb packet parity, HoT cadence, SapVital modes, ClampVital modes, alias vitals, and result side effects remain incomplete. | `Spell Effect Evidence Matrix.md`, `Spell System Progress Tracker.md`, spell diagnostics, combat log readers. | `Game/Spell`, `Game/Entity/UnitEntity`, combat log models | Work family-by-family with fixture captures; do not widen from one witness row. |
| F-018 | Spell runtime: stack groups, buffs, CC, movement | Several states are tracked locally with timed cleanup. | Stack arbitration, broader aura persistence, DR/stun breakout, tether/additional CC data, force-move physics, facing blend modes, and packet parity are incomplete. | `Spell4StackGroup`, `CCStateSet`, `CCStateBreak`, `ForcedMove`, `ForceFacing`, `ServerEntityCC*`. | `Game/Spell`, `Game/Entity/Movement`, combat logs | Decode stack arbitration or CC packet tails before broad behavior changes. |
| F-019 | Spell runtime: summons, traps, vehicles, AI services | Conservative creation/removal exists for some summon families. | Ownership, AI/controller hookup, trap trigger behavior, formation, service payloads, turret/deployable state, and summoned vehicle seat modes are incomplete. | `SummonCreature`, `SummonTrap`, `SummonVehicle`, `NpcExecutionDelay`, service-token labels. | `Game/Spell`, entity factory, combat AI, content scripts | Pick one placed fixture and implement only its proven controller/trigger loop. |
| F-020 | Spell runtime: RavelSignal and scripted receivers | Structural diagnostics and `OnSignal(signalId)` bridge exist. | Receiver graph, signal modes, payload values, and script-state effects are not decoded. | `RavelSignal`, `SpellRouteEvent_*`, content scripts, runtime signal logs. | `Game/Spell`, scripts, `IWorldEntityScript` | Decode receiver/callback graph before adding generic signal side effects. |
| F-021 | Action set, LAS, ability, AMP, attributes | LAS size/spec/tier/AMP and some preflight checks exist. | `UpdateSpellInProgress`, async spell update transaction, authoritative attribute allocation/refund, bonus ability/AMP unlock persistence, and action-bar lock state remain incomplete. | `Server0x00B0`, `016B`, `016D`, `016E`, `019C`, `01A4`, `ClientRequestActionSetChanges`, `ActionSet_CheckUpdateSpellInProgress`. | `Game/Spell`, ability models, spell handlers | Map async spell update transaction before adding synthetic update gates. |
| F-022 | Quests, path missions, public events | Core quest/objective and many public-event packets exist; recent tutorial fixes landed. | Path mission edge types, public-event votes/scoreboards, event phase scripts, objective notification parity, quest share precision, and some objective types remain incomplete. | `Server0x0139`, `06F7`, path opcodes around `0160/0181`, public-event labels, content smoke logs. | `Game/Quest`, `Game/PublicEvent`, path manager, scripts | Use one content flow at a time: NPE, Rider's Reef, or world `3404`. |
| F-023 | Content: NPE and Rider's Reef | Recent Rider's Reef/final-departure and mine/loot fixes landed; NPE login-world smoke works. | Manual client playthrough validation remains for quest acceptance, kill loops, rewards, respawn, CSI, hoverboard/projector reliability, and final terminal/checklist flow. | Runtime logs, `I:\WildStar` client smoke, tutorial scripts, quest direction tables. | `Source/NexusForever.Script.Main/Tutorial`, quest/entity/runtime code | Run manual client smoke and turn each observed blocker into a focused code task. |
| F-024 | Content: Evil from the Ether / world 3404 | Map asset and world import are staged; scripts exist. | Manual expedition playthrough, public event `781`, doors, interactables, teleports, phase transitions, encounter mechanics, and unsupported spell/content blockers remain. | `Plan Evil From The Ether Expedition Port.md`, world `3404` scripts, runtime smoke logs. | `Source/NexusForever.Script.Instance`, map/instance/public-event code | Start local stack and validate one encounter phase at a time. |
| F-025 | Entity create/update, visibility, phasing, interaction, CSI | Core entity create/update works; busy target and interaction gates are partial. | Unknown `ServerEntityCreate` substructures, deferred action queues, current object/target semantics, CSI parity, phase visibility, threat/entity stat packets, and map-tracked-unit parity remain open. | `Server0x025F`, `0260`, `0261`, `0263`, `0264`, `0889`, `08CC`, `08F4`, `0939`, `093D`, `093E`; `Interaction_*`, `DeferredActionQueue_*`. | `Game/Entity`, world entity models, entity handlers, scripts | Decode one entity-create/update tail and add a packet serialization regression before runtime mutation. |
| F-026 | Items, supply satchel, costumes, pets, titles, generic unlocks | Inventory, title, pet, costume, unlock surfaces exist in parts. | Item swap/error aux packets, item eligibility, supply satchel precision, costume forget/unlock parity, pet flair, generic unlock persistence, and account/character unlock list deltas remain incomplete. | `Server0x00B7`, `0183`, `019A`, `037F`, `0567`, `056B`, `056C`, `056D`, `0980`, `0986`. | `Game/Entity/Item`, account unlock/costume/pet/title managers | Decode item/unlock packet tails and implement one unlock or costume lifecycle with tests. |
| F-027 | Options, keybindings, combat-log preferences | Keybinding and option parsing/logging exists. | Durable option store and combat-log filtering by preference are missing. | `ClientCombatOptions_WritePayload`, `ClientOptions`, `BiInputKeySet`, nearby item/options server opcodes `056B..056D`. | Option handlers, account/character keybinding managers | Add account/character option storage before filtering outbound logs. |
| F-028 | Support, reports, surveys, stuck | Parsed diagnostics and deterministic ticket result/failure exist. | Persistent ticket/report/survey backend, moderation workflow, and support-case result semantics are missing. | `ClientIncidentReport`, `ClientSupportTicket`, `ClientReportBug`, `ClientStuck`, `ClientSuggest`, `ClientCustomerSurveySubmit`, `Server0x0347..0351`, `ServerSupportTicketResult`. | Support handlers, account/support models if introduced | Design a backing service boundary before storing support data. |
| F-029 | Client DB, data mapping, EF placeholder proof | Table registration pattern is mapped and DataMapping exists. | More table registrations, staging-to-runtime promotions, EF placeholder renames, and migration validation remain case-by-case. | Client DB registration labels, `Tools/DataMapping`, `AccountInventoryModel.Unknown*`, `DataBits*`, table fixture SQL. | GameTable, Database projects, DataMapping tools | Rename fields only after packet/data contract plus migration proof. |
| F-030 | Realm, character select/list/transfer, PTR/copy | Character list/select works; some realm and PTR/copy packets exist. | Realm transfer destinations/results, PTR copy state, new realm notices, optional realm messages, and character admin result packets need parity. | `Client0x0760`, `0762`, `Server0x03EF`, `Server0x0347..0351`, `ServerPtrCharacterCopyQueued`, realm message/list readers. | Character handlers, realm/server managers, pregame models | Decode realm/client-list auxiliary requests before changing realm state. |
| F-031 | Fortune minigame | Client and server packet families are modeled; all four client handlers are diagnostic-only. | Fortune session state, card reveal and resume/reset flow, storefront/game synchronisation, reward payout, and eligibility or persistence semantics are missing. | `ClientFortuneNotifyGame`, `ClientFortuneNotifyStorefront`, `ClientFortuneStart`, `ClientFortuneFlipCard`, `ServerFortuneCards`, `ServerFortuneCardUpdate`, `ServerFortuneRewards`, `ServerFortuneReset`; fortune UI readers. | `WorldServer/Network/Message/Handler/Fortune`, fortune models, storefront/account inventory surfaces if linked | Decode the start/flip/reward loop and implement one conservative session lifecycle before adding payout state. |
| F-032 | Leaderboards | PvE and PvP request handlers return deterministic empty compatibility responses. | Real leaderboard rows, rank/stat aggregation, personal placement, season/filter semantics, and map or prime-level category rules are missing. | `ClientLeaderboardPveRequest`, `ClientLeaderboardPvpRequest`, `ServerLeaderboardPve`, `ServerLeaderboardPvp`; leaderboard UI readers and category selectors. | `WorldServer/Network/Message/Handler/Leaderboard`, leaderboard models, backing store/service if introduced | Decode one PvE and one PvP reader path before introducing ranking storage or cached results. |
| F-033 | Challenges and shared challenges | Challenge packet models exist; shared-challenge preference toggles exist; the choice handler is diagnostic-only. | Active challenge lifecycle, share/accept/timeout state, objective/result updates, reward tiers, and quest/objective integration are missing. | `ClientChallengeChoice`, `ServerChallengeUpdate`, `ServerChallengeResult`, `ServerChallengeShared`, `ServerChallengeShareTimeout`, `Client0x00C8`, challenge UI readers, `QuestObjectiveType.CompleteChallenge`. | challenge models, option handlers, `GameTable/Challenge*`, quest/objective surfaces | Decode the accept/decline and update sequence before mutating quest, group, or reward state. |
| F-034 | Datacubes, journals, Galactic Archive / Codex | Datacube and archive persistence, login init, interact unlock/view flows, and packet models exist. | Broader content hookups, archive-link/interact-unlock parity, full journal/datacube progression semantics, rule unlock parity, and wider Codex UX coverage remain incomplete. | `ServerDatacubeUpdateList`, `ServerDatacubeUpdate`, `ServerDatacubeVolumeUpdate`, `ClientGalacticArchiveUnlock`, `ClientGalacticArchiveViewed`, `ServerGalacticArchiveRefresh`, archive link/unlock rules, creature `ArchiveArticleIdInteractUnlock`. | `Game/Entity/DatacubeManager`, `Game/Entity/GalacticArchiveManager`, `Game/Entity/SimpleEntity`, galactic archive handlers/models | Verify one datacube or journal pickup chain and one archive-link unlock flow end-to-end, then close remaining field and trigger gaps. |
| F-035 | Achievements and realm-firsts | Character, guild, and global achievement managers, persistence, init/update packets, and many runtime updaters exist. | Full trigger coverage, Steam achievement ingest, exact realm-first broadcast semantics, remaining updater parity, and achievement UI edge cases are incomplete. | `ServerAchievementInit`, `ServerAchievementUpdate`, `ServerRealmFirstAchievement`, `ClientSteamAchievements`, `AchievementType` callsites, realm-first and guild-achievement flows. | `Game/Achievement`, achievement models, `ClientSteamAchievementsHandler`, achievement updaters/tests | Audit unsupported `AchievementType` families and map `ClientSteamAchievements` before widening cross-system trigger coverage. |
| F-036 | Zone maps and zone completion | Hex discovery, persistence, login sync, and `ServerZoneMap` output exist. | Zone completion semantics, title/reward payout, faction-specific completion rules, and integration with quest/challenge/datacube/journal totals from `ZoneCompletion` remain missing or unused. | `ServerZoneMap`, `MapZone*`, `MapZoneHexGroup*`, `ZoneCompletion`, `AchievementType.MapComplete`, zone-map UI and completion readers. | `Game/Map/ZoneMapManager`, `Game/Map/ZoneMap`, zone-map models, achievement/title surfaces | Validate one explored-zone completion flow against `ZoneCompletion.tbl`, then implement conservative completion and reward/title payout only after thresholds are proven. |

## Client Diagnostic Opcode Matrix

These are receive-side client opcodes with safe logging only. The first decode
step is to find the native writer/sender path and prove when the client sends
the request.

| Likely cluster | Opcode(s) | Shape | Neighbor evidence | First task |
| --- | --- | --- | --- | --- |
| Login/world entry | `Client0x003D` | `0x18` bytes | Between max-character-level and player-entered-world. | Find writer around world-entry/client-ready state. |
| Challenge/housing boundary | `Client0x00C8` | `0x4` bytes | Between challenge update and housing privacy. | Check whether this is challenge ack, housing ack, or UI state. |
| Duel/path boundary | `Client0x00ED` | `0x20` bytes | After duel initiate, before path scientist dismiss scanbot. | Search sender callsites from duel and path UI. |
| Character/loot/mail | `Client0x011B`, `Client0x011D` | `0x1`, `0x4` bytes | Around character list, loot bind, and mail delete. | Decode whether these are loot bind acks or mail/list UI requests. |
| Quest/pet customisation | `Client0x012D` | `0x8` bytes | After quest objective world location, before pet list. | Map client request owner: quest tracker or pet UI. |
| Rapid transport/appearance | `Client0x0142` | `0x10` bytes | After rapid transport, before appearance result. | Correlate with rapid transport service-token follow-up. |
| ICComm/spell list | `Client0x0550` | `uint32` | Between directed ICComm message and spell list. | Check if this is ICComm ack or ability-book request. |
| Matching/movement control | `Client0x062A`, `Client0x0634` | `uint32`, `uint32` | Between matching average wait and movement control ack. | Decode matching queue UI acks before queue state changes. |
| Auth denied/marketplace status | `Client0x063E` | wide string | Between auth denied and marketplace status. | Determine if this is crash/report/status text or marketplace filter. |
| Public event/queue | `Client0x0701` | `0x8` bytes | After public event trigger UI, before queue finish. | Map public-event UI ack vs queue action. |
| Realm list/messages | `Client0x0760`, `Client0x0762` | `0x58`, `0x10` bytes | Around realm first achievement, realm list, realm messages. | Decode realm-list refresh or realm-message request path. |
| Instance/account item | `Client0x07B6` | `0x20` bytes | After reset instances, before account item return. | Correlate with instance reset UI or pending item groups. |
| Character/destination arrow | `Client0x07E3` | `uint32` | Between character list and destination arrow clear. | Decode pregame/world UI request source. |
| Cooldown/reward property | `Client0x0928` | `0x8` bytes | Between cooldown list and reward property set. | Check reward-property or cooldown-list client ack path. |

## Server Opcode Decode Matrix

These 120 server-output opcodes are all shape-only models. The "likely cluster"
is based on enum ordering and neighboring named opcodes, not a semantic claim.

| Likely cluster | Opcode(s) | Shape evidence | Neighbor evidence | First decode task |
| --- | --- | --- | --- | --- |
| Achievement / LAS opening | `0x00B0` | `18`-bit scalar | Between achievement update and action-set request. | Find client reader for `0x00B0`; compare with ability/achievement UI state. |
| Item context / friendship bridge | `0x00B7` | `0x1` byte | Between item context action and friendship block. | Check item result vs social block response readers. |
| Early housing / instance UI | `0x00CA`, `0x00CB`, `0x00CC`, `0x00CD`, `0x00CE`, `0x00D1` | `0x10`, `0x1`, `0x4`, `0x4`, `0x8`, `0x1` bytes | After housing privacy request, before instance settings. | Decode as a housing batch before touching residence state. |
| Datacube / path / resurrection edge | `0x00DF`, `0x00EE`, `0x0101`, `0x0160`, `0x0181` | byte and uint shapes | Around datacube, duel, resurrection, path scientist scanbot. | Split by reader registration; do not assume one subsystem. |
| Housing basics | `0x010D`, `0x0110` | `0x1`, `0x10` bytes | Around guild holomark and `ServerHousingBasics`. | Decode housing-basics follow-up readers. |
| Public event auxiliary | `0x0139`, `0x06F7` | `0x10`, `0x8` bytes | Around public-event map/objective/bomb status. | Trace public-event UI update readers and scoreboard paths. |
| Appearance / repair / instance / character admin | `0x0143`, `0x014D`, `0x0157`, `0x0347`, `0x0348`, `0x034C`, `0x034D`, `0x034E`, `0x0351` | byte, `0x8`, `0x10..0x40` bytes | Around rapid transport/appearance, repair, instance reset, survey/character delete. | Decode each reader before grouping; several adjacent systems collide here. |
| LAS / abilities / reputation / path XP | `0x016B`, `0x016D`, `0x016E`, `0x019C`, `0x01A4`, `0x01A6`, `0x01A7`, `0x01A8`, `0x01A9` | `0x4..0x20` bytes | Around action bars, stance, player changed, AMP list, reputation, path XP. | Search ability/reputation UI reader registrations. |
| Inventory / supply / item/options | `0x0183`, `0x019A`, `0x0567`, `0x056B`, `0x056C`, `0x056D` | `uint32+flag`, `0x8`, `0x10..0x28` bytes | Around item move, supply satchel, mail item deprecation, item error, keybinds. | Decode item-error/swap/options readers before implementing item side effects. |
| Entity/player create and CREDD | `0x0186`, `0x0187`, `0x025F`, `0x0260`, `0x0261`, `0x0263`, `0x0264`, `0x026A` | `0x1..0x50` bytes | Around entity select, flight path, player/entity create, CREDD exchange. | Decode `025F..0264` with entity-create reader evidence; decode `026A` with CREDD separately. |
| Vehicle / chat / cinematic boundary | `0x01B2`, `0x01B8`, `0x01C1`, `0x01C4`, `0x01EF` | `0x10`, `0x68`, `0x20`, `0x20`, `0x10` bytes | Vehicle embark, chat flag/list/message, cinematic start. | Trace chat auxiliary readers and vehicle reader separately. |
| Costume / emote | `0x037F` | `0x18` bytes | Between emote and costume item forget. | Decode costume/emote UI response path. |
| Realm transfer / account gift | `0x03EF` | `0x10` bytes | Between realm transfer destinations and account item gift. | Decode realm-transfer or gift pending-group response. |
| Group / raid / queue | `0x0414`, `0x042A`, `0x0431`, `0x0436`, `0x0438`, `0x0441`, `0x045A`, `0x0461`, `0x0468`, `0x0718` | `0xC..0x60` bytes | Around group difficulty, invite/kick/mark/member flags/action result, queue status, raid info. | Map group reader table, then implement one group status/update packet at a time. |
| Housing neighbor/community | `0x04FE`, `0x0501`, `0x0506`, `0x0514`, `0x0516`, `0x0519`, `0x051F`, `0x053A`, `0x053B`, `0x078C` | `0x10..0x30` bytes | Around community donate, neighbors, neighbor invite/evict/permission, community rename. | Decode neighbor/community result packets, then persist permissions. |
| Realm info / mail | `0x05A1` | `0x10` bytes | Between realm info response and mail result. | Check realm-info vs mail-result reader before implementing. |
| Marketplace / reward / generic map | `0x06DF`, `0x07CD`, `0x07D3`, `0x07D5` | `0x20`, `0x28`, `0x10`, `0x14` bytes | Around auction post, reward update request, generic map node, auctions by filter. | Decode marketplace/reward/generic-map readers separately. |
| Story / flight / pets | `0x074A`, `0x077E` | `0x18`, `0x10` bytes | Story communicator/unit and recruitment/pet despawn; `077E` shares flight-path read shape. | Decode story text auxiliary and flight update alias before emit-site changes. |
| Spell/cooldown auxiliary | `0x080F`, `0x0810`, `0x0812` | all `0x10` bytes | Around cooldown, target list, buff remove. | Decode spell broadcast reader edge before wiring spell runtime. |
| Map / crafting / tradeskill | `0x0846`, `0x084B`, `0x0855` | `0x4`, `0x18`, `0xC` bytes | Around time-of-day/map tracked unit and crafting current/profession load. | Split map-tracked-unit from crafting result readers. |
| Unit combat / loot / entity stat | `0x0889`, `0x08A0`, `0x08A8`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E` | `0xC..0x48` bytes | Around reputation override, death, loot, vehicle passenger, entity visual/stat, emote/item use. | Decode loot and entity-stat readers with packet tests before runtime mutation. |
| Account / storefront / unlock terminal cluster | `0x0969`, `0x096A`, `0x096B`, `0x096C`, `0x096E`, `0x0971`, `0x0976`, `0x0977`, `0x0978`, `0x097A`, `0x097B`, `0x097D`, `0x097E`, `0x0980`, `0x0986`, `0x0989`, `0x098A`, `0x098C`, `0x098D`, `0x098E`, `0x098F`, `0x0990`, `0x0991` | `0x1..0x60` bytes | Around account currency/entitlements/items, support ticket result, generic unlocks, store categories/offers. | Decode this as the first account/store packet family before adding pending/gifting/cooldown behavior. |

## Suggested Execution Order

| Order | Slice | Why first |
| ---: | --- | --- |
| 1 | Account/storefront terminal cluster `0969..0991` | Dense unresolved server-output cluster with direct ties to account inventory, store offers, generic unlocks, cooldowns, and pending groups. |
| 2 | Housing neighbor/community cluster | Large feature gap with many adjacent unresolved opcodes and clear source surfaces. |
| 3 | Crafting/tradeskill success | Packets are already mostly mapped; missing work is stateful backend behavior. |
| 4 | Marketplace/auction/commodity | Backing service is missing, but request packet coverage is strong. |
| 5 | Group/matching/queue | Needed for multiplayer content and has a coherent unresolved server cluster. |
| 6 | Spell auxiliary `080F/0810/0812` plus proc/shield validation | High gameplay value, but must stay family-first. |
| 7 | Loot/group-roll/master-loot | Retail feel improvement and direct dependency for group content. |
| 8 | Content smoke blockers for NPE, Rider's Reef, and world `3404` | Turns protocol/runtime work into visible gameplay restoration. |

## Task Template

Use this block when starting any row above:

```markdown
### Target

- Matrix row:
- One-sentence question:
- Candidate opcode(s):
- Candidate client function(s):
- Current NexusForever surface:
- Evidence level before work:

### Decode Plan

- Search terms:
- Export files to inspect:
- Expected reader/writer shape:
- Unknowns to keep diagnostic-only:

### Implementation Gate

- Minimum evidence required:
- Files likely to change:
- Tests or smoke command:
- Done state: Mapped only / Implemented / Rejected
```
