# Evidence Plan To Close Remaining NexusForever Blockers

Status date: 2026-06-09 (aligned with current F-009 service-token spell-cast and F-011 recruitment / war-party boss-token packet cached-export follow-ups)

Use `I:\WildStar` build 16042, GM accounts, Trace server logs, client screenshots/video, and existing evidence collectors to turn each blocker into one of three states: **implementable now**, **diagnostic-only**, or **still retail/native blocked**. Server logs are first-class evidence, but they only prove current emulator/client interaction; retail producer semantics still need native/decompile, packet captures, or old retail logs.

External cross-checks confirm only project/client context, not blocker semantics: [NexusForever GitHub](https://github.com/NexusForever/NexusForever), [NexusForever installation wiki mirror](https://github-wiki-see.page/m/NexusForever/NexusForever/wiki/Installation), [WildStar slash command archive](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands).

## Blocker disposition (code + trackers)

| ID | Emulator-parity implemented | Diagnostic-only | Still blocked |
| --- | --- | --- | --- |
| F-001 STS token/optional auth | Password/SRP login, key-data, RC4 session encryption, login finish, request/consume game token, user info, verified-IP stubs, game-account, and presence compatibility routes are implemented; non-`None` handler ordering is test-pinned for `/Auth/LoginStart` and `/Auth/KeyData` | Optional request fields such as login-finish secondary/verified-IP flags and user-info external-account flags are parsed but do not widen auth semantics | `/Auth/LoginTokenStart`, `/Auth/TokenKeyData`, `/Auth/RequestToken`, and `/Auth/AssociateMyExternalAccount` remain blocked pending exact RSA key format, signature/trust anchor, premaster derivation, `TokenKeyData` reply fields, post-token session/crypto transition, and startup route ordering |
| F-002 `Client0x00ED` diagnostic | Packet shape is mapped and test-pinned as `uint64 + uint32 + uint64 + 3 bits` | Handler logs only and does not emit plaintext or encrypted responses; fields remain neutral `Value0`..`Value5`; duel/mail/path aliases are rejected | Opcode-specific native sender, consumer, callback/table owner, indirect send rail, or live `0x00ED` payload proving request intent and field semantics |
| F-002 `Client0x011B` / `Client0x011D` diagnostic | `0x011B` empty payload and `0x011D` one-`uint32` payload are mapped and test-pinned | Handlers log only and do not emit plaintext or encrypted responses; `0x011B` has no fields and `0x011D.Value` remains neutral; loot-bind, mail, loot-vacuum, and tradeskill-reset aliases are rejected | Opcode-specific native sender, consumer, callback/table owner, indirect send rail, or live `0x011B`/`0x011D` payload proving request intent and field semantics |
| F-002 structural realm/addon diagnostics | `ClientAccountRealmData`, `ClientRealmListRealmRow`, `ClientRealmListMessageRow`, and `ClientAddonModuleList` have mapped packet shapes and structural names; cached 2026-06-09 recheck ties realm rows to `ServerRealmList` row serializers and addon modules to `Client0x07B6_SendFromModuleList` / `Lua_GetAddons` | Handlers log only and do not mutate gameplay state or emit server responses; realm rows are structural row serializers, and addon-module list consumer intent is not proven | Standalone realm row sender/consumer or addon-list server consumer semantics via native callback/table owner, indirect send rail, or live payload capture |
| F-003 Server0x0015 | All server opcodes have models; `Server0x0015` has a packet-shape model and neutral-field tests | Shared `ServerUInt5UInt32_ReadPayload` (`140081f00`) proves only 5-bit plus `uint32`; matching `0x0628` and Fortune row-helper reuse are rejected as semantic aliases | Native `0x0015` apply/producer path, post-read consumer, or live `0x0015` payload capture |
| F-005 marketplace aux `0x06DF`/`0x07D5` | Core auction/commodity behavior, marketplace persistence, settlement mail, and the two aux packet models/tests exist | `ServerAuctionPostAux` and `ServerAuctionsByFilterAux` are reader-shape mapped only; cached 2026-06-09 recheck found registration and reader-local parsing evidence but no marketplace apply/producer path | Native marketplace apply helper, runtime producer/send site, server producer witness, or accepted live/retail marketplace packet capture proving field semantics and emit timing |
| F-007 Reward rotation | Game-table refresh emits `0x07CD`/`0x07D3` content context, `0x07CA` schedule rows, persisted `account_reward_rotation_grant` entry-state, claim handling, level/difficulty-aware schedule row order, and reward-property partial-table guards | `ServerRewardRotationContentContext` `UInt0`/`UInt1`/`UInt3`/`Flag` are wire-mapped/correlated only; 2026-06-09 cached-export recheck kept `0x07CD` mapped-only | `0x07CD` apply/`Flag` consumer semantics, dynamic throttle-slot assignment, item/currency/property delivery after claim, exact retail schedule/content reward selection |
| F-008 Crafting | Fixed-recipe craft, station/tradeskill mismatch rejection, additive zero-station rejection, LootId delivery, durable rune item bridge (`item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`), packet-shape pins; C2S rune install mapped to `0x085B` | Service-key numeric logging, complex `CraftStats`/`ChargeCounts`, and `0x084B`/`0x0855`/`0x056C` wire models only | Discovery rolls/unlock mutation, service-key `0x2C`/`0x4F`/`0x57` names, current-craft cadence, `0x084B`/`0x0855` emit intent, non-success sigil precision, `ServerItemMicrochips` (`0x056C`) producer timing |
| F-009 Transport | `TaxiRoute.tbl` runtime loading, clean route/node/location table rejection, rapid transport route resolve, credit/cooldown rejection branches, captured table-backed rapid route debit/cast context, RapidTransport prerequisite type 269 using client taxi-node cast context, contiguous flight-path purchase charge/teleport validation, and `ClientSpellCastWithServiceToken` (`0x00C2`) wire/cost-gate source alignment | `0x00C2` is packet-shape/source-aligned only for service-token spell casting; no transport branch is proven | Transport service-token bypass, global route state, taxi embark/completion, broader charge/teleport parity, passenger/seat modes, deployable vehicles |
| F-010 Group/matching | Replacement `0x05D5`/`0x0602` validation+logging, zero `ServerRaidQueueStatus` with raid-info plus non-zero wire-order guard, flexible roles, deserter persistence, `Client0x062A`/`Client0x0634` log-only handler guards, `ServerMatching0x05CF` raw `uint32` shape plus correlated apply candidate | `Client0x062A`/`Client0x0634` value logging only until native sender/live UI context is proven; `ServerMatching0x05CF` remains diagnostic/mapped-only until the apply-table index or live payload context is proven; `ServerRaidQueueStatus` row fields are mapped through `0x071A`/`Group_DispatchRaidInfoResponse`, but the standalone non-zero `0x0718` producer/timing remains blocked | Replacement backfill/merge, non-zero raid queue semantics, `0x05CF` field/apply semantics, `Client0x062A`/`Client0x0634` sender intent |
| F-011 Guild | Holomark update handler + guild manager paths exist; cached 2026-06-09 recruitment packet-family recheck source-aligns `0x049F` and `0x0767..0x076F` packet shapes with current models/tests; cached 2026-06-09 war-party boss-token recheck source-aligns `0x0956` request, `0x0951` token-list response rows, and `0x094F` guild-boss-token cast item/context boundary with focused tests | Recruitment detail/subscribe handlers remain log-only; recruitment packet evidence is shape-only without runtime state ownership; war-party token-list handling remains empty and boss-token casts return `BossTokenNotReady` | Bank economy, perks, recruitment persistence, subscribe/update timing, list/detail population, availability semantics, boss-token inventory, cast acceptance/results, match-results population, and warplot semantics |
| F-012 ICComm | Join/message validation, transient membership | — | Entitlement gating, persistent-channel restore |
| F-015 Duel/PvP | Duel leash/cancel-warning/disconnect and PvP toggle-off cooldown persistence are implemented/test-pinned | — | Observer/reward parity |
| F-016..F-020 Spells | Per-family fixtures where captured; hostile damage effects use caster-side attack permission so captured Pulse Blast-style damage can flow to combat logs | `!spell procunsupported` etc. | Uncaptured effect families |
| F-021 LAS | Preflight checks where mapped | — | `UpdateSpellInProgress`, async transaction semantics |
| F-031 Fortune | Coin cost, emulator rarity-tier pool, UI probability transport, `account_fortune_session` persistence code path | Local catalog/table audit only (`FORTUNE_WEIGHT_AUDIT.md`); F-007 `RewardRotation*` schedules rejected as Fortune active-rotation evidence; no live Fortune play artifact found | Per-item retail Madame Fay weights and active rotation catalog |

A blocker is **closed** only when the evidence bundle, implementation, tests, and tracker note agree. Anything proven only by current emulator logs but not by retail/native producer semantics is **emulator-parity implemented**, not **retail-parity proven**.

For F-001, an accepted startup STS evidence bundle needs the raw route
transcript from connect through world handoff, matching STS/Auth server logs,
client-visible login result, at least one negative route-order case, and a JSON
or text artifact naming any `/Auth/LoginTokenStart`, `/Auth/TokenKeyData`,
`/Auth/RequestToken`, or `/Auth/AssociateMyExternalAccount` traffic. A capture
that only proves the existing SRP/login-finish/game-token path is useful smoke
evidence but does not unblock token/RSA implementation.

## Evidence harness

Start Trace logging and the client console:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -Lws036ChecklistSmoke `
  -BundleName "LWS-036-starter-zone-checklist-smoke" `
  -ClientDirectory "I:\WildStar" `
  -WorldIds 426,990 `
  -ObjectiveIds 4888,4889,4890 `
  -NegativeCases "missing quest objective", "wrong world/objective" `
  -PromptForRootPassword
```

The harness creates `artifacts\blocker_evidence\<timestamp>-<bundle>` before
launching servers/client. Each bundle includes `manifest.json`, command
transcript, server-log notes, client-observation notes, negative-case notes,
screenshots/video/log folders, and helper scripts to tail or collect logs. Use
`-CreateBundleOnly` to create the bundle skeleton without touching running
services. `-Lws036ChecklistSmoke` adds `lws-036-targets.md`, preloads the
Illium/Thayd/Northern-Wilds/small-world target worlds when no explicit
`-WorldIds` are supplied, and fills the required negative cases for the
LaughingWS city/checklist smoke gate.
`-NewZoneAssetProof` adds `new-zone-asset-proof-targets.md`, preloads the
Dreadmoor/Halon/Murkmire world ids when no explicit `-WorldIds` are supplied,
and records the required negative/safety cases for LWS-040 through LWS-043.
`test_blocker_evidence_harness_presets.py` dry-runs `-Lws036ChecklistSmoke`
and `-NewZoneAssetProof` with `-CreateBundleOnly` and verifies their manifests,
worksheets, default world ids, helper files, and negative-case scaffolds; this
guards the capture workflow only and does not prove any row safe to promote.
`-DustStalkerQ4516Smoke` adds `lws-051-dust-stalker-targets.md`, preloads world
`1138` and objectives `7633`, `6189`, `7637`, `7638`, and `7639` when no
explicit ids are supplied, and records the required Boss Xagg, bridge-control,
exit-panel, timer-expiry, and repeat-interaction negative cases for LWS-051.
`-ArcterraPalaverSourceSmoke` adds `lws-052-arcterra-palaver-targets.md`,
preloads worlds `3335` and `3519` plus the known Palaver objective ids, and
records the required Caretaker prerequisite, Coldblood portal, Palaver
wrong-state, and repeat-interaction negative cases for LWS-052.
`-SkyplotHousingSmoke` adds `lws-053-skyplot-housing-targets.md`, preloads world
`1229`, and records the required return-pad, vendor-before-unlock,
invalid-catalog-row, and inactive-lifecycle negative cases for LWS-053.
`-LiveEventSmoke` adds `lws-054-live-event-targets.md` and records inactive
event, event-ending, post-cleanup, and skipped-source-row negative cases for
the Battle Chase, Dungeon Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1,
and zPrix LWS-054 proof passes.
`-QuestVirtualLootSmoke` adds `lws-055-quest-virtual-loot-targets.md`, preloads
representative objectives `13011`, `12605`, `6700`, `6313`, and `21278`, and
records inactive-objective, pre-activation kill, completed-objective repeat, and
declined/abandoned-loot negative cases for LWS-055.
`-PublicEventVoteScoreboardSmoke` adds
`lws-071-072-public-event-vote-scoreboard-targets.md` and records
non-participant/duplicate/late vote, non-member scoreboard request, and
scoreboard unsubscribe negative cases for LWS-071 and LWS-072 vote/scoreboard
proof passes.
`-PublicEventObjectiveNotificationSmoke` adds
`lws-073-public-event-objective-notification-targets.md` and records
non-audience objective packets, duplicate status transition, premature
quest-share/text, and post-completion notification-mode negative cases for the
LWS-073 objective notification proof pass.
`-PvpAdventureSmoke` adds `lws-080-085-pvp-adventure-targets.md`, preloads
worlds `797`, `1393`, `1627`, `2166`, `3022`, and `3449`, preloads public
events `158`, `170`, `171`, `213`, `217`, `366`, `438`, `466`, `581`, `582`,
`876`, and `877`, and records queue/match, disconnect/leave, premature reward,
wrong-team/wrong-phase, and early-finish negative cases for the PvP/adventure
proof passes.
`-ExpeditionSmoke` adds `lws-090-096-expedition-targets.md`, preloads worlds
`1232`, `1319`, `2149`, `2183`, `2188`, `3180`, and `3404`, preloads public
events `95`, `108`, `390`, `446`, `447`, `680`, and `781`, and records
zero-vector/placeholder trigger, premature door/shuttle/teleport, wrong-phase
trigger, completion-only cinematic, and early-finish reward negative cases for
the expedition proof passes.
`-DungeonSmoke` adds `lws-100-106-dungeon-targets.md`, preloads worlds `382`,
`1263`, `1271`, `1336`, `2980`, `3173`, and `3522`, preloads public events
`145`, `148`, `161`, `166`, `594`, `667`, and `907`, and records premature
trigger, premature door/platform/launcher/teleporter, wrong-route optional
objective, completion-only cinematic, and early-finish reward negative cases
for the dungeon proof passes.
`-RaidEventSmoke` adds `lws-110-117-raid-event-targets.md`, preloads worlds
`1333`, `1462`, `3032`, `3040`, `3041`, `3044`, `3045`, and `3094`, preloads
public events `157`, `159`, `595`, `597`, `605`, `642`, `679`, and `705`, and
records premature trigger/target-set, premature door/elevator/gate/room
interaction, wrong-route weekly/wing/room, completion-only cinematic, and
early-finish reward negative cases for the raid/event-instance proof passes.

Or invoke the setup script directly:

```powershell
.\Tools\Setup\Restart-NexusForeverLocal.ps1 `
  -ClientDirectory "I:\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -LogLevel Trace `
  -PromptForRootPassword
```

`-LogLevel` configures NexusForever server NLog output only. `-EnableClientLogging`
enables retail `CLog` file output under `<WildStar>\Logs`. See
`Decomp/Analysis/CLIENT_LOGGING.md` for switch details and tail commands.

The retail client toggles that console with `Alt` plus virtual key `0xC0`.
On a typical US layout that is backtick; on a Swiss layout it is the key that
produces `¨`, `!`, or `]` depending on modifiers. If the physical mapping is
unclear, use `.\Tools\Setup\Send-WildStarConsoleToggle.ps1` after the client
window is open.

Tail evidence logs in separate terminals:

```powershell
Get-Content -Wait -Tail 200 .\.nexusforever-runtime\logs\NexusForever_World.stdout.log
Get-Content -Wait -Tail 200 .\Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log
Get-Content -Wait -Tail 200 .\.nexusforever-runtime\logs\NexusForever_Group.stdout.log
Get-Content -Wait -Tail 200 .\.nexusforever-runtime\logs\NexusForever_Friendship.stdout.log
```

Use `gm/gm` as primary and `admin/admin` or `player/player` as a second client. Use chat commands with `!`; if using the raw cheat console path, omit `!`.

Baseline commands for every pass:

```text
!help
!help teleport
!character level 50
!currency character add Credits 10000000
!character save
!teleport name Thayd
!teleport name Illium
!teleport coordinates <x> <y> <z> [worldId]
```

2026-06-05 evidence review:

- `artifacts\verify\spell-evidence\20260605-113719317-spell4-82922...json` shows Rapid Transport `82922` now reaches `CastResult=Ok` with `RapidTransport` and `Fluff` effects after the type `269` prerequisite fix. The older `20260605-101900744...` artifact is the pre-fix `PrereqCasterCast` comparison.
- `artifacts\verify\spell-evidence\20260605-113837894-spell4-37302...json` shows Pulse Blast from creature `335` hitting player `295` with three `CombatLogDamage` rows and shield deltas, while the non-hostile/simple target is ignored.
- 2026-06-08 follow-up bundle `artifacts\blocker_evidence\20260608-225224-F016-spell-proc-loot-live` closes the player-originated Pulse Blast proof for emulator parity: `NexusForever.WorldServer_20260608_57172.log` shows `ClientCastSpellContinuous(0x4DB)` driving `42276` / base `26468` into proxy `37302` / base `22522`, selecting hostile target `179`, dispatching Pulse Blast damage effects `79090`, `79092`, and `79093`, and emitting `spell-go` combat-log rows. The latest structured Pulse Blast JSON remains `artifacts\verify\spell-evidence\20260608-205817660-spell4-37302...json` from `!spell capture4 37302`; `!spell capturenext` does not currently export continuous-cast JSON because `ClientCastSpellContinuousHandler` bypasses `ClientSpellEvidenceCaptureHelper`.
- `artifacts\verify\spell-evidence\20260605-113920634-spell4-4046...json` plus `artifacts\verify\proc-evidence\20260605-113928580...json` show Brutal registering a supported proc: trigger event `12` (`deal-damage`), trigger spell `4047`, chance `0.15`, targetData `4`, route `counterpart`.
- 2026-06-09 live log `NexusForever.WorldServer_20260609_11748.log` closes the Brutal post-registration trigger proof for emulator parity: `!spell capture4 4046` applied Brutal to holder/source `334`, then real combat from Creature2 `73464` holder/source `334` into player `294` produced matching `proc-probe` rows for `damage-dealt` and `deal-damage`, expected `chance-roll-failed` controls, `reentrant-proc` guards during triggered spell recursion, and successful `proc-dispatch ... action=cast-trigger-spell` rows for trigger spell `4047`. Fresh `20260609-164113...holder-1` and `20260609-164347...holder-294` reports are empty because `!spell procreport` was run on the selected/invoker holder instead of proc holder `334`; keep that as an artifact-selection caveat, not a runtime proc blocker.
- `artifacts\verify\reward-evidence` still contains empty/manual reward reports from the earlier pass, but the latest world log proves client refresh requests for indices `1-6` emitted content-context packets and index `0` emitted empty schedule/state placeholders. Two later command attempts failed as `0!reward`, so run reward commands one at a time and wait for each result.
- `artifacts\verify\account-evidence\20260605-114239710...json` confirms the coupon path remains intentionally blocked as `coupon-request-unmapped`, with `ServerAccountOperationResult InvalidCoupon`.
- 2026-06-08 follow-up `artifacts\verify\loot-evidence\20260608-205910592-loot-char-30-guid-294.json` captures a loot-bag `ServerLootNotify` for character `30` / player guid `294`, with account-currency item `6` amount `7`, `Granted=true`, and `LootUnitId=0`. Battle Beast corpse-visible loot still needs a separate source if corpse loot parity is targeted.
- The invisible Battle Beast respawn/client-state bug was reproduced in `NexusForever.WorldServer_20260605_25700.log` with 40k+ repeated “source unit ... is not visible” aggro skips after reused creature GUID respawns. The fix now sends loot remove plus destroy/create on non-player respawn; the next live pass should verify visible respawns and no persistent loot shower.
- Realm Bank now reaches runtime code. The local DB had skipped the hand-written
  `20260522180000_RealmBankItems` migration because the migration class lacked
  EF metadata; the migration is now discoverable and the local table can be
  created through the character migration path before re-running Realm Bank
  interaction proof.
- 2026-06-09 follow-up bundle
  `artifacts\blocker_evidence\20260609-185310-F025-realm-bank-live` did not
  close Realm Bank open/storage proof. `NexusForever.WorldServer_20260609_61596.log`
  shows `RealmBank` bags initialising, `ServerAccountEntitlements`/`ServerAccountEntitlement`
  sends, and account `1` now has `SharedRealmBankUnlock=1` plus
  `SharedRealmBankSlots=2`, but `realm_bank_item` stayed empty and no
  `ClientItemMove` was captured. The clicked UI/object produced unhandled
  `ClientEntityInteract` event `101` with `requestedEntity=0`, not the expected
  event `67` (`ShowRealmBank`). Repeating `!entitlement add` after the account
  is at the table max can throw `InvalidOperationException`, so future live
  passes should list current values first and only add missing positive grants.
- 2026-06-09 follow-up bundle
  `artifacts\blocker_evidence\20260609-191319-F025-realm-bank-live` captured
  both normal-bank item movement and later Realm Bank opens, but it still does
  not close Realm Bank storage. Packet evidence
  `artifacts\packet_evidence\F025-realm-bank-07EA\20260609-171630-packet-evidence.jsonl`
  decodes the early bank-like interactions as event `72` (`GuildBankerOpen`),
  event `66` (`ShowBank`), and event `101` zero-target warnings. World log
  `NexusForever.WorldServer_20260609_60448.log` lines 48399-48627 show four
  `ClientItemMove` requests moving items between `Inventory` and `PlayerBank`.
  Later in the same pass, packet evidence payload `EA072B04000043` appeared at
  17:23:59, 17:24:25, and 17:25:15 and decodes to entity guid `1067`, event
  `67` (`ShowRealmBank`). The account item take at 17:25:12 succeeded and sent
  `ServerAccountEntitlement(0x0973)` payload `73094B00000003000000`, updating
  `SharedRealmBankSlots` (`75`) to amount `3`; the user still saw a red-cross
  invalid drop target while trying to place items in the Realm Bank. No
  `ClientItemMove(0x0182)` or `ServerItemMove(0x0569)` followed the event `67`
  opens, and read-only DB checks showed `realm_bank_item` count `0` plus no
  character `item.location = 3`. Treat this as Realm Bank open/entitlement proof
  plus client-side drop rejection proof, not storage proof. The remaining Realm
  Bank blocker is exact open/snapshot/capacity or item-location UI state, not the
  basic entity interaction or entitlement grant.
- 2026-06-09 CDB follow-up in the same bundle
  `logs\f025-realm-bank-cdb_e384_2026-06-09_19-36-08-902.log` maps the red-cross
  drop one step deeper. The client hit `InteractionState_DispatchUiOpenClose`
  (`1403a71f0`) for state `0x43`, sent `ClientEntityInteract(0x07EA)` for
  guid `1067` / event `67`, hit `ItemDragDrop_ReadSharedRealmBankUiMode`
  (`1406d5a40`) once, and hit `ItemDragDrop_HandleGenericMovePaths`
  (`1406d50f0`) three times while dragging over the Realm Bank. It did not hit
  `ItemMove_ValidateAndMaybeConfirmBindOnEquip` (`1403c17d0`), did not send
  `ClientItemMove(0x0182)`, and did not receive `ServerItemMove(0x0569)`.
  Packet evidence also still contains no `0x0182`/`0x0569`, and DB storage stayed
  empty. Treat this as mapped-only native proof that the drop dies inside or
  before the generic drag/drop route resolves a valid Realm Bank destination.
- 2026-06-09 branch-level CDB follow-up in the same bundle
  `logs\f025-realm-bank-cdb-branch-1406d50f0_6f30_2026-06-09_19-46-21-254.log`
  supersedes the first pass for one controlled drag path. `1406d50f0` hit five
  times with drag type `DDBagItem`, `source_mode_44c=0x0A`, `state6644=0x43`,
  and target location byte `1`; `1403c17d0` then validated a move with
  `targetLoc=1`, `targetSlot=0x11`, `sourceLoc=0x0A`, `sourceSlot=0x04`, and
  the client sent `ClientItemMove(0x0182)` payload
  `01000000110000000A00000004000000`. World log
  `NexusForever.WorldServer_20260609_60448.log` lines 127024-127025 received
  `ClientItemMove(0x0182)` and immediately sent `ServerItemError(0x056A)`;
  DB checks still showed `realm_bank_item=0` and no character items at location
  `3` or `10`. This proves the shared Realm Bank item-location wire id is
  `0x0A` for item move traffic and narrows the blocker to server wire-location
  normalization, not entity interaction or entitlement. Source now normalizes
  `0x0A` <-> internal `InventoryLocation.RealmBank` in
  `Source/NexusForever.Network.World/Message/Model/Shared/ItemLocation.cs` and
  emits the same wire id for `ServerItemMove` drag/drop data via
  `Source/NexusForever.Game/Entity/Inventory.cs`; focused
  `ItemLocationPacketShapeTests` cover read, write, and drag/drop encoding. Next
  live proof after restarting the stack should capture `0x07EA,0x0182,0x0569`
  packet evidence while moving one harmless item into Realm Bank, back out, and
  through relog/reopen persistence.
- 2026-06-09 post-fix live proof in
  `artifacts\packet_evidence\F025-realm-bank-wire-0A\20260609-190558-packet-evidence.jsonl`
  and `NexusForever.WorldServer_20260609_41496.log` closes the server-side
  wire-location blocker for eligible items. After opening the actual Realm Bank
  with `ClientEntityInteract(0x07EA)` payload `EA072B04000043`, the client sent
  `ClientItemMove(0x0182)` at 21:15:18 and the server log lines 27118-27121
  moved item `0x58D` from `Inventory` index `19` to `RealmBank` index `0`,
  then sent `ServerItemMove(0x0569)` with the Realm Bank side encoded as wire
  location `0x0A`. At 21:15:35 the client sent the reverse `ClientItemMove`,
  and lines 27316-27319 moved item `0x58D` from `RealmBank` index `0` back to
  `Inventory` index `8` with another `ServerItemMove(0x0569)`. No
  `ServerItemError(0x056A)` appears in the post-fix window. A read-only DB check
  after the withdrawal showed `realm_bank_item_count=0`, character item
  locations only `0` and `1`, and account `1` entitlements `74=1`, `75=3`, which
  matches the item being withdrawn before the DB snapshot. The remaining red
  cross cases are client-side item eligibility filtering, not evidence of the
  old server wire-location rejection.

Round 2 follow-up commands after the 2026-06-05 evidence review:

```text
# Run one chat command at a time, and wait for the command response/log line before the next.
!help entitlement
!help spell
!help loot
!help reward
!help account
!character level 50
!currency character add Credits 10000000
!currency account add ServiceToken 1000
!character save

# RealmBank: run only after the character DB has the realm_bank_item table.
# Before restart, enable packet evidence for ClientEntityInteract and item sync:
# $env:NEXUSFOREVER_PACKET_EVIDENCE = '1'
# $env:NEXUSFOREVER_PACKET_EVIDENCE_DIR = 'I:\GIT\NexusForever\artifacts\packet_evidence\F025-realm-bank-07EA'
# $env:NEXUSFOREVER_PACKET_EVIDENCE_OPCODES = '0x07EA,0x0182,0x0569,0x0111,0x0968,0x0973'
!teleport name Thayd
!entitlement account list
# Only run positive grants if the list shows missing/zero values; do not use add 0.
!entitlement add SharedRealmBankUnlock 1
!entitlement add SharedRealmBankSlots 1
!character save
# Interact with the actual Realm Bank NPC/terminal and confirm the server sees event 67.
# If logs show event 101 with requestedEntity=0, that was the wrong UI/object.
# If packet evidence decodes event 66 or bag traces say PlayerBank, that was the normal bank.
# If event 67 appears but the client shows a red cross and no ClientItemMove follows,
# record this as client-side realm-bank drop rejection and continue with native/addon
# open/snapshot/capacity evidence instead of retrying entitlement grants.
# Move one harmless inventory item into and back out of Realm Bank, then relog and reopen.

# RapidTransport: prove spell 82922 no longer fails PrereqCasterCast for nodes 88/89.
!teleport name Thayd
!spell inspect4 82922
!spell capturenext
# Use the rapid transport UI for node 88; inspect artifacts\verify\spell-evidence.
!spell capturenext
# Use the rapid transport UI for node 89; inspect artifacts\verify\spell-evidence.

# Pulse Blast/damage: prove raw/adjusted damage, health/shield deltas, and combat logs are emitted.
!teleport location 51739
!spell inspect4 37302
!spell capture4 37302
!spell capturenext
# Cast Pulse Blast from the client against a hostile target, then inspect artifacts\verify\spell-evidence.
!spell inspect4 4046
!spell capture4 4046
# Brutal post-registration proc dispatch is closed for emulator parity by the
# 2026-06-09 WorldServer log. Repeat only for regression proof or a cleaner
# holder-specific JSON export; select the proc holder/source unit before export.
# Deal damage with Brutal active before exporting proc evidence.
!spell procstates
!spell procreport
!spell procunsupported

# Loot: Battle Beasts only produced immediate Omnibit grants; use a visible-loot source.
!item add 84623 1 1
!loot capturenext
# Right-click/use the loot bag item once, then inspect artifacts\verify\loot-evidence.
!loot capturenext
# Kill one loot-bearing creature or force one visible loot notify, then inspect artifacts\verify\loot-evidence.

# Reward/coupon: evidence-only until live schedule/state/coupon policy is proven.
!reward capturenext
# Open the reward/storefront UI once; inspect artifacts\verify\reward-evidence.
!reward report 0
!reward context 0
!reward report 1
!reward context 1
!reward report 2
!reward context 2
!account couponblockers
```

An evidence bundle is accepted only when it has: exact command/action transcript, matching server log lines with player/request ids, client-visible result, negative/rejection case, and JSON artifact where a collector exists.

For opcode-level packet passes, prefer server-decrypted evidence over pcap-only
interpretation. Start the target server with:

```powershell
$env:NEXUSFOREVER_PACKET_EVIDENCE = '1'
$env:NEXUSFOREVER_PACKET_EVIDENCE_DIR = 'I:\GIT\NexusForever\artifacts\packet_evidence\<pass-name>\server-decrypted'
$env:NEXUSFOREVER_PACKET_EVIDENCE_OPCODES = '0x0186,0x0187,0x0188,0x00CA,0x01A7,0x01A8,0x056B,0x056C,0x056D'
```

The recorder writes plaintext opcode/body JSONL records before outbound
encryption and after inbound decryption. Pair it with Wireshark/dumpcap on the
local ports for timing and TCP correlation, but use the server-decrypted JSONL
as the opcode source of truth.

For F-009 transport passes, include the client transport opcodes in the recorder
filter too:

```powershell
$env:NEXUSFOREVER_PACKET_EVIDENCE_OPCODES = '0x00FF,0x0141,0x0186,0x0187,0x0188'
```

## Blocker passes

**F-002 `Client0x0928`: keep diagnostic-only until opcode-specific ownership is proven.**
For a native or accepted payload pass, target `0x0928` separately from pet stance and enable packet evidence for both the diagnostic row and the positive pet controls `0x068E`/`0x068F`. Accepted proof for renaming or mutating `Client0x0928` must include an opcode-specific native sender, post-read consumer, callback/table owner, indirect send rail, or captured payload with a client-visible effect. Shared `uint32 + 5-bit` helpers are not enough.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 evidence keeps `Client0x0928` mapped-only: `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x0928` size `8` at `selected_decompiled.c:27834` with `ClientUInt32UInt5_WritePayload` (`1400898b0`) and `ServerUInt32UInt5_ReadPayload` (`14008ce80`), proving only one `uint32` plus one 5-bit field. The positive pet controls stay separate: `Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x068E` to the same writer at `selected_decompiled.c:13114` and `0x068F` to the same reader at `selected_decompiled.c:13182`, but `Pet_SetStance_SendClientPetSetStance` (`14050a270`) sends only `0x068E`, while `Pet_ApplyStanceChangedPayload` (`1403c0a80`) / `Pet_GetStance_ReadCachedStance` (`14050a130`) explain only `0x068F`. Current source keeps `Source/NexusForever.Network.World/Message/Model/ClientUnresolvedDiagnosticPackets.cs` `Client0x0928.LeadingValue` / `TrailingBits` neutral and `Source/NexusForever.WorldServer/Network/Message/Handler/Misc/ClientUnresolvedDiagnosticHandlers.cs` log-only; packet and handler tests preserve that boundary. Do not alias `0x0928` to pet stance, cooldown UI, reward-property, movement, or buff behavior until a second opcode-specific witness exists.

**F-025 Entity-stat aux: keep `0x0889`/`0x08CC`/`0x08F4`/`0x0939`/`0x093D`/`0x093E` blocked until per-opcode apply or live order proof.**
For a live pass, enable packet evidence for the six entity-stat aux opcodes plus regular stat updates and exercise combat, death/respawn, vehicle/passenger transitions, emotes, item use, CSI/direct interaction, and reputation/override-looking UI states. Capture the exact action transcript, server logs, client-visible state, and negative case where regular `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger` is sufficient without aux. Implement only an aux producer or semantic field rename proven by a per-opcode native apply handler, apply-table classification, or accepted live sniff/order witness.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments still show `0x0889` through the shared three-`uint32` triplet reader, `0x08CC` through the shared `uint32` plus wide-string reader, and `0x08F4`/`0x0939`/`0x093D`/`0x093E` through reader-only entity-stat aux shapes. Source search still finds the six aux packet models only in models/tests/negative guards, while regular runtime stat sends use `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger`. Keep the six aux producers and semantic field names blocked.

**F-003 Server0x0015: keep neutral until opcode-specific ownership is proven.**
For a live pass, enable packet evidence for `0x0015`, `0x0628`, and `0x03D2`, then exercise matching average-wait updates and Fortune UI opens as negative controls while watching for any separate `0x0015` payload. Accepted proof for changing `Server0x0015` must include a native `0x0015` apply/producer path, post-read consumer, or live `0x0015` packet with a client-visible effect distinct from matching and Fortune row-helper traffic.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached `ServerUInt5UInt32_ReadPayload` (`140081f00`) still proves only the shared 5-bit plus `uint32` reader registered for `0x0015` and `0x0628`; cached `ServerFortuneRewards_ReadPayload` (`140081f60`) calls the same reader as a money-reward row helper; cached `MatchingManager_ApplyMatchingAverageWaitTimeUpdated` (`1405c0e00`) remains the positive `0x0628` apply path. Keep `Server0x0015.Value0`/`Value1` neutral and non-emitted.

**F-007 Reward rotation content context: keep `0x07CD` field semantics blocked until retail/live apply proof.**
For a live pass, enable packet evidence for `0x07CC`, `0x07CD`, `0x07D3`, `0x07CA`, `0x07C8`, and the three entry-state delta opcodes `0x07C7`/`0x07C9`/`0x07CB`. Open Content Finder Bonus Rewards and storefront reward-rotation surfaces on a level-50 character, force refreshes for indexes `0..6`, claim a visible reward when available, reload the character, and capture a negative case where a refresh request produces no claimable reward. The accepted bundle must include the transcript, server logs, packet JSON, visible UI state before/after refresh and claim, and the generated reward-rotation runtime evidence artifact. Implement or rename only if a retail `0x07CD` payload or a dynamic breakpoint on the runtime apply dispatch proves `UInt0`/`UInt1`/`UInt3` slot assignment and `Flag` meaning.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances, so no fresh labels or exports were added. Cached `ServerRewardRotationContentContext_ReadPayload` (`14008fcb0`) still proves only the `0x07CD` wire shape, cached `RewardRotation_ManagerInit` (`140635840`) initializes seven throttle slots at `manager + 0x150 + index * 0x14`, cached `Reward_SendRewardUpdateRequest` (`140636ba0`) sends only the content-type index through `0x07CC`, and cached `RewardRotation_GetLoadedScheduleForContent` (`140636c40`) proves refresh-by-index / loaded-schedule lookup. Keep `ServerRewardRotationContentContext.UInt0`/`UInt1`/`UInt3`/`Flag` neutral and mapped-only.

**F-004 Housing neighborhood list: keep `0x0501`/`0x0506` producer blocked until native/live proof.**
Open the housing/neighborhood/community UI from a prepared residence and, if available, repeat from realm login into a housing-enabled character. Include outbound packet evidence for early housing outputs and inbound client requests around housing/neighborhood UI open. Capture `ServerHousingNeighborhoodEntry` / `ServerHousingNeighborhoodList` only if they actually appear, plus matching server logs, visible UI state, and a negative case such as non-community or no-neighborhood state. Implement only trigger/row fields proven by native producer evidence or an accepted live bundle; do not synthesize rows from `HousingNeighborhoodInfo.tbl`, residence session state, or `ServerHousingProperties.Residence.NeighbourhoodId`.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments still show `0x0501` / `0x0506` as row/list readers, `Housing_HandleNeighborhoodList` as client cache apply plus `HousingNeighborhoodRecieved`, and `ClientDB_RegisterHousingNeighborhoodInfo` as table-loader-only evidence. Source search still finds the neighborhood packets only in models/tests/negative guards. Keep the producer blocked pending a native server-push path or accepted live housing UI/realm-login capture proving trigger, row backing, field population, and realm/session timing.

**F-005 Marketplace aux: keep `0x06DF`/`0x07D5` non-emitted until marketplace producer proof.**
For a live or native pass, exercise auction post/search/filter, buyout, bid, cancel, commodity submit/fill/cancel, and CREDD views with packet evidence enabled for marketplace response/update clusters plus `0x06DF` and `0x07D5`. Accepted proof for widening these aux packets must include a native marketplace apply/producer path, server producer witness, callback/table owner, or accepted marketplace packet capture that ties the fields to visible UI state and emit timing.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments under `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` keep both packets mapped-only: `selected_decompiled.c:13318` registers `0x06DF` size `0x20` to `ServerAuctionPostAux_ReadPayload` (`140090090`), and `selected_decompiled.c:13314` registers `0x07D5` size `0x14` to `ServerAuctionsByFilterAux_ReadPayload` (`14008fe80`). Selected xrefs are label-only and selected call edges stay reader-local through bit-read/allocation/copy helpers. Current source paths remain packet-contract only: `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs` and `Source/NexusForever.Game.Tests/Network/PacketPlaceholderNamingTests.cs`; no `WorldServer` emitter was found. Keep `ServerAuctionPostAux.Values`/`Data`/`Value` and `ServerAuctionsByFilterAux.UInt14Value`/`Value1..Value3`/`Flag` neutral until producer evidence proves field semantics and timing.

**F-010 Group/Raid/Matching: implementable for empty/status diagnostics, still blocked for non-zero queue semantics.**
Open raid and queue UI, request raid info, start/stop replacement search if an in-progress match can be created. Capture logs for `ClientRaidInfoRequest`, `ServerRaidQueueStatus`, `ServerMatching0x05CF` if it appears, `ClientMatchingMatchInitiateLookingForReplacements`, `ClientMatchingStopLookingForReplacements`, and diagnostics `Client0x062A`/`Client0x0634`. Implement only observed empty-status/validation behavior; keep non-zero queue position, `0x05CF` field semantics, and backfill merge lifecycle blocked until a live non-zero payload or client consumer proves meaning.

**F-009 Rapid Transport/Flight: implement captured route behavior, keep global route-state/transport service-token blocked.**
At city hubs, run `!spell inspect4 82922`, then `!spell capturenext` before using the rapid transport/taxi/flight UI for nodes 88 and 89. Inspect `artifacts\verify\spell-evidence` and world logs for taxi node, context token, route id, source/destination, price, credit debit, spell id, and accept/reject reason. The 2026-06-05 fix maps prerequisite type 269 to the client taxi-node cast context; the next successful proof should no longer end with `CastResult=PrereqCasterCast` for the captured nodes. Implement route(s) and rejection branches proven by logs; do not infer route unlock tables, global route state, or transport service-token bypass.

2026-06-09 cached-export/source recheck: Cached
`ClientSpellCastWithServiceToken_WritePayload` (`140089570`) maps `0x00C2`
as an 18-bit context token plus 32-bit `Spell4` id;
`ServiceToken_HandleCastResult` (`140520c10`) gates on
`Spell4.PropertyFlags & 0x20000000`; and
`ServiceToken_SendClientSpellCastWithServiceToken` (`1403994f0`) checks
balance, returns `0x014B` for insufficient funds, or sends `0x00C2`. Current
source mirrors the packet shape and `UseServiceTokenCost` cost path. This does
not prove any rapid/taxi service-token bypass branch or route/teleport timing,
so keep that transport behavior blocked.

**F-008 Crafting/Rune discovery: station request semantics and durable rune bridge closed; keep discovery, service-key names, and aux emit blocked.**
Station request branches are covered by native sender evidence and focused tests: simple/complex/autocraft can carry zero station ids, while additive requires a non-zero station before `0x084A`. The durable rune item bridge is implemented through `item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`, `RandomGlyphData`/`Glyphs`, and migration `20260531224115_ItemMicrochipIdsAndRuneSlots`, with focused rune tests covering socket/install round trips. Current evidence rejects a distinct client microchip-install mutator: client rune install is `0x085B` (`RuneCrafting_SendClientRuneInstall`), while `ServerItemMicrochips` (`0x056C`) and `Inventory_UpdateItemMicrochipsFromWire` are server-side item patch/update evidence with blocked producer timing. For the next live pass, teleport to crafting hubs, use station UI, `!item lookup <name> 25`, `!item add <itemId> <qty>`, then craft complex/random outputs and failed cases. Capture logs for station unit id, schematic id, material ids, craft type, completion/rejection reason, hot/cold UI state, and any current-craft/finish packets. Implement only behavior proven by logs or producer decomp. Discovery roll thresholds/unlock mutation, `CraftStats`/`ChargeCounts` semantics beyond diagnostics, service-key names `0x2C`/`0x4F`/`0x57`, current-craft cadence, `0x084B`/`0x0855` emit intent, non-success sigil precision, and `0x056C` server-side microchip patch timing remain blocked without producer/native/live evidence.

2026-06-09 live F-008 station/craft proof: bundle `artifacts\blocker_evidence\20260609-212659-F008-crafting-live` plus packet evidence `artifacts\packet_evidence\F008-crafting-live\20260609-201741-packet-evidence.jsonl` closed the generic-station fixed-recipe smoke for emulator parity. Creature `21793` uses activate spell `1817` with prerequisite `1050` (`IsPlayer == 0`), so activate-spell prerequisites must evaluate against the activated unit target. The live station unit `354` exposed `Creature2.TradeSkillIdStation = 4294967295`, proving the all-tradeskills sentinel can arrive as `uint.MaxValue`; older SQL/reference checks also showed `2147483647`, so validation accepts both sentinels. The client sent `ClientCraftingCraftItemAutoCraft(0x0852)` for the UI `Simple Craft` button, not `ClientCraftingSimpleCraft`; after the sentinel fix, contexts `47..49` completed for schematic `270` at station `354`, emitted `ServerSupplySatchelUpdate(0x0199)` and `ServerCraftingFinish(0x0853)`, and produced item `14838` with persisted stack `12` after three crafts while material `11` dropped to `28`. Treat the repeated `TradeSkillProfession` prerequisite warnings during UI refresh as a separate prerequisite-coverage issue, not an auto-craft blocker.

2026-06-09 live F-008 Tech Tree/talent proof: the observed `+40 Technologist XP (480/450)` result did not indicate a lost XP write. Character `30` persisted `tradeskillXp = 480`, and tier achievement `1481` completed after crossing the tier-2 `450` threshold. The remaining `480/450` display issue is now blocked on packet/order evidence because the packet evidence filter omitted `ServerProfessionUpdate(0x0860)`. The real server-state bug was that completed `TradeskillAchievementReward` rows were not applied: achievement `1470` reward `599` grants one Technologist talent point, and achievement `1471` reward `162` grants one talent point plus schematic `2954` (`Spirovine Extract`). Runtime now applies those table-backed rewards idempotently, backfills already-completed rewards on login/learn, and removes the old simplified 10-talent-point learn/reset seed. A live retry proved the final dependency gap: `TradeskillAchievementReward.tbl` existed in runtime assets but was not marked `[GameData]`, so WorldServer never loaded it and the reward pass saw zero rows. `TradeskillAchievementReward` is now runtime-required game data and the contract tests pin it; focused reward/table verification passed 47/47 and WorldServer build passed. After reconnect/save, character `30` persisted Technologist `talentPoints = 2` and schematic `2954` with XP still `480`. Capture `0x0860` before changing profession-update ordering or Crafting Result progress display behavior.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances, so no fresh labels or exports were added. Cached WildStar64 fragments under `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` keep this mapped-only: `selected_decompiled.c:13809` registers `0x084B` size `0x18` to `ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload` (`1400a3af0`), `selected_decompiled.c:13807` registers `0x0855` size `0x0c` to `ServerUInt32AndTwoFloats_ReadPayload` (`140081df0`), `selected_decompiled.c:13843` registers `0x0854` size `0x50` to `ServerCraftingCurrentCraft_ReadPayload` (`1400a46b0`), and `selected_decompiled.c:13815` registers `0x056C` size `0x28` to `ServerItemMicrochips_ReadPayload` (`1400a3d50`). `Crafting_HandleServerCraftingCurrentCraft` (`1405e6830`) only applies the decoded current-craft state and dispatches `CraftingUpdateCurrent`; `Inventory_UpdateItemMicrochipsFromWire` (`1403b8540`) / `InventoryItem_ApplyMicrochipsFromWire` (`14056aa20`) only apply item microchip state and dispatch `ItemModified`. Current source paths remain shape-only or finish-only: `Source/NexusForever.Network.World/Message/Model/Crafting/ServerCraftingCurrentCraft.cs`, `Source/NexusForever.Network.World/Message/Model/Crafting/ServerCraftingAuxPackets.cs`, `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs`, and `Source/NexusForever.WorldServer/Network/Message/Handler/Crafting/ClientCraftingCraftHandlers.cs`, with `CraftingSimpleCraftHandlerTests`/`CraftingAdditiveHandlerTests` guarding no blocked current-craft or aux emits. Keep current-craft, aux, and microchip producers blocked until a native producer/send site, apply-table owner/callback, or accepted live crafting/item-replication capture proves enqueue cadence, field population, microchip patch timing, and result-state boundaries.

**F-011 Guild bank/perks/holomarks: implement observable persistence/logged packet handling only.**
Client 1: `!guild register Guild NFEvidence`; client 2: `!guild join Guild NFEvidence`. Exercise holomark changes, guild bank tab open, item/money deposits, recruitment subscribe/details, and reload after `!character save`. Use logs for bank/recruitment handlers and screenshots before/after reload. Implement holomark persistence or logged response-shape fixes if proven; keep bank economy, perk effects, recruitment parity, and warplot semantics blocked.

2026-06-09 cached-export/source recheck: `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `ClientRecruitmentGuildGetDetailedGuildInfo` (`0x076E`)
size `0x10` at `selected_decompiled.c:13324`, and
`RecruitmentGuild_SendClientGetDetailedGuildInfo` (`140584840`) sends `0x076E`
from `Lua_GameRecruitmentGuild_GetDetailedGuildInfo` (`14069e5c0`) through
`Network_SendOpcodePayloadOrPackedHelper` (`1403f4740`) using current realm id
plus selected guild identity. Adjacent registrations at
`selected_decompiled.c:13369-13377` source-align `0x049F` and
`0x0767..0x076D` with current recruitment availability, demand, description,
detail, list/update, minimum-level, and recruiter row models/tests. This proves
packet-family shape only; keep recruitment persistence, list/detail population,
subscribe/update timing, and availability semantics blocked until a native
producer/state owner or accepted recruitment UI capture proves them.

2026-06-09 war-party boss-token packet recheck:
`Network_RegisterServerOpcode_0351` (`14006c290`) registers
`ClientWarPartyBossTokensRequest` (`0x0956`) size `0x10` at
`selected_decompiled.c:13334`, `ClientCastGuildBossToken` (`0x094F`) size
`0x18` at `selected_decompiled.c:13336`, and `ServerWarPartyBossTokens`
(`0x0951`) size `0x20` at `selected_decompiled.c:13363`.
`FUN_14057ef20` sends `0x0956` when type-3 boss-token state is absent;
`140092990` reads guild identity, row count, and token rows as 18-bit item id
plus `uint32` count; `GuildBossToken_SendClientCastGuildBossToken`
(`1403991b0`) sends `0x094F` after type-3 boss-token lookup and target
validation. Source now reads `ClientCastGuildBossToken.Item2Id` at the mapped
18-bit width. Keep war-party token-list handling empty and boss-token casts
rejected until native/server boss-token inventory ownership, accepted cast/result
capture, match-results producer proof, or warplot plug evidence proves state and
emit timing.

**F-012 ICComm: implement transient routing/cleanup where proven, keep entitlement/persistent channels blocked.**
Use two clients in same hub and same guild/circle/community. Test join, leave/logout, direct message, guild channel message, invalid channel, and non-member send. Capture `ClientICComm*` logs plus both client screens. Test `!entitlement account list` and only use `!entitlement add <ExactEntitlementType> <value>` if help/logs identify the enum. Entitlement gating and persistent-channel restore remain blocked unless toggles produce reproducible client-visible differences.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances, so no fresh labels or exports were added. Cached WildStar64 fragments under `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` keep chat aux mapped-only: `selected_decompiled.c:12999` registers `0x01B8` to row writer/reader `ServerChatAuxRow_WritePayload` (`140085af0`) / `ServerChatAuxRow_ReadPayload` (`140085ca0`), `selected_decompiled.c:13000` registers `0x01C4` to `ServerChatAuxPayloadAlt_WritePayload` (`140085e30`) / `ServerChatAuxPayloadAlt_ReadPayload` (`140085fe0`), `selected_decompiled.c:13001` registers `0x01C1` to `ServerChatAuxPayload_WritePayload` (`1400861b0`) / `ServerChatAuxPayload_ReadPayload` (`140086410`), and `selected_decompiled.c:13723` registers `0x01EF` to reader-only `ServerChatAuxNotification_ReadPayload` (`1400a0890`). The fragments prove row/envelope/notification parsing, including `ServerChatAuxRow_ReadPayload` dispatch through `PTR_LAB_140c1ec90`, but selected call edges show only row/envelope relationships and wrapper helpers, not a runtime chat or ICComm producer. Current source paths remain packet-contract only: `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs` and `Source/NexusForever.Game.Tests/Network/PacketPlaceholderNamingTests.cs`; runtime chat and ICComm handlers do not emit these aux packets. Keep chat aux non-emitted and field names neutral until a native producer/apply path, callback/table owner, dynamic dispatch proof, accepted packet capture, or two-client social flow proves emit timing and runtime semantics.

**F-015 Duel/PvP observer/reward: close lifecycle smoke, keep observer/reward parity blocked.**
Two clients: `!teleport name Thayd`, initiate duel, accept, decline, forfeit, toggle PvP, then move one player beyond the 120f leash with `!teleport coordinates` and wait 15 seconds. Capture challenge/countdown/leash/cancel-warning logs and client UI. Implement lifecycle/cooldown fixes only if mismatched; observer broadcasts, reward parity, and retail stat behavior remain blocked.

**F-016-F-020 Spell families/procs/loot/reward: implement one fixture per captured spell family.**
For each target spell: `!spell inspect4 <spell4Id>`, `!spell capture4 <spell4Id>`, `!spell diag4 <spell4Id>`, or `!spell capturenext` before a real client cast. Pulse Blast damage is closed for emulator parity by the 2026-06-08 bundle above; repeat it only when validating `ClientCastSpellContinuous` JSON capture coverage or a damage regression. Brutal `4046 -> 4047` post-registration proc dispatch is closed for emulator parity by the 2026-06-09 WorldServer log; repeat only for regression proof or a cleaner holder-specific JSON export. For combat/loot: use Exile combat lanes `!teleport location 51739` or `!teleport location 51740`; for Dominion, prefer `!teleport location 52898` and avoid `53015` unless the current character is in the matching final Dominion tutorial phase. Use `!loot capturenext`, `!reward capturenext`, `!spell procstates`, `!spell procreport`, `!spell procunsupported`. Implement only captured spell/effect/proc fixtures with tests; leave uncaptured effect families blocked.

**F-021 Action Set/LAS: implement preflight/result mapping only if UI plus logs prove it.**
Use `!spell add <spell4BaseId> [tier]`, `!spell resetcooldown`, then change LAS slots, tiers, spec, and AMP state in normal, dead, combat, and PvP states. Capture UI result and any server logs. `UpdateSpellInProgress` and async transaction semantics remain blocked unless a real in-progress transition is observed.

**F-030 Realm transfer/PTR: keep `0x03EF` and PTR queue/copy state blocked until producer proof.**
For a native or accepted capture pass, exercise `ClientGetRealmTransferDestinations`, `ClientRealmTransfer`, `ClientInitiatePTRCharacterCopy`, and `ClientPtrCopy` from character select with packet evidence enabled for realm-transfer/PTR response clusters. Accepted proof for widening behavior must include a native producer/apply path, callback/table owner, dynamic dispatch proof, server realm/catalog artifact, or captured packet sequence tying the destination/PTR fields to visible UI state, queue state, transfer result, and handoff timing.

2026-06-09 cached-export/source recheck: cached `WildStar64.exe` fragment `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/14007d790.fragment.c` keeps `ServerRealmTransferDestinationsAux` (`0x03EF`) mapped-only. `ServerRealmTransferDestinationsAux_ReadPayload` (`14007d790`) reads one `uint32`, a `uint32` byte count, allocates that count, and copies raw bytes into a pointer-backed buffer. `Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x03EF` size `0x10` at `selected_decompiled.c:12787` with null write/handler slots, and the same reader is reused by `0x01B4` at `selected_decompiled.c:13804`; selected xrefs are label-only and selected call edges stay reader-local. Current source emits `ServerTransferDestinationRealmList` from `Source/NexusForever.WorldServer/Network/Message/Handler/Misc/ClientGetRealmTransferDestinationsHandler.cs`, keeps `ServerRealmTransferDestinationsAux` only in `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs` plus `PacketPlaceholderNamingTests`, and keeps `ClientRealmTransferHandler` conservative for invalid/offline/online targets. Keep `0x03EF` non-emitted and raw payload neutral until producer evidence explains destination semantics and transfer/PTR timing.

**F-031 Madame Fay/Fortune: local tests validate session/payout boundaries and UI probability transport; per-item retail weights remain blocked.**
Use Fortune UI with enough `FortuneCoin`/account currency, capture logs and screenshots for cost/result/session behavior, reload persistence (`account_fortune_session`), and displayed `fProbability` values. Emulator now sends `ServerFortuneRewards.RewardItemProbabilities` from rarity-tier weights; `FORTUNE_WEIGHT_AUDIT.md` confirms `AccountItem.tbl` has no weight column, F-007 `RewardRotation*` schedules are rejected as Fortune active-rotation evidence, and no local live Fortune play artifact was found. Exact per-account-item retail weights still need retail `ServerFortuneRewards` capture or storefront-server catalog evidence.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments still show `ServerFortuneRewards` as item2/money/probability transport, `Fortune_ApplyRewards` as client-side cache application of server-provided arrays, `FortunesLib.GetFortunesLootList` as UI exposure of cached probabilities, and `FortuneNode_ApplyServerFortunePackets` as the `0x03CF`-`0x03D2` dispatcher. Source still emits `ServerFortuneRewards` from emulator rarity-tier `FortuneRewardPool`; no retail active-rotation source, per-item weight table, or storefront-server catalog proof surfaced.

Harness shortcut:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -FortuneRewardsSmoke -PromptForRootPassword
```

This creates the LWS-066 worksheet for `ServerFortuneRewards`, active rotation or
storefront context, card deal/flip/payout, reload persistence, insufficient
currency, invalid flip, and repeated-flip rejection evidence.

## Test and close criteria

For each accepted bundle, add focused tests before implementation: handler branch, packet shape, persistence reload, or artifact parser test as appropriate. Then run the narrow owning tests, falling back to:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo
```

Related trackers: `MATCHING_IMPLEMENTATION_STATUS.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `MISSING_FEATURE_MATRIX.md`, `CURRENT_STATUS.md`, `CONTINUATION_GUIDE.md`.
