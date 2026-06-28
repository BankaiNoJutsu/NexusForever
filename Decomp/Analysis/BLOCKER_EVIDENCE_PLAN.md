# Evidence Plan To Close Remaining NexusForever Blockers

Status date: 2026-06-19 CEST (aligned with F-022 Q5597 Chua Explosives indicator 17899 activation UI smoke review)

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
| F-005 marketplace aux `0x06DF`/`0x07D5` | Core auction/commodity behavior, marketplace persistence, settlement mail, and the two aux packet models/tests exist | `ServerAuctionPostAux` and `ServerAuctionsByFilterAux` are reader-shape mapped only; 2026-06-17 recheck found registration and reader-local parsing evidence but no marketplace apply/producer path or runtime emitter | Native marketplace apply helper, runtime producer/send site, server producer witness, or accepted live/retail marketplace packet capture proving field semantics and emit timing |
| F-007 Reward rotation | Game-table refresh emits `0x07CD`/`0x07D3` content context, `0x07CA` schedule rows, persisted `account_reward_rotation_grant` entry-state, claim handling, level/difficulty-aware schedule row order, and reward-property partial-table guards | `ServerRewardRotationContentContext` `UInt0`/`UInt1`/`UInt3`/`Flag` are wire-mapped/correlated only; 2026-06-17 cached-export/source recheck kept `0x07CD` mapped-only | `0x07CD` apply/`Flag` consumer semantics, dynamic throttle-slot assignment, item/currency/property delivery after claim, exact retail schedule/content reward selection |
| F-008 Crafting | Fixed-recipe craft, station/tradeskill mismatch rejection, additive zero-station rejection, LootId delivery, durable rune item bridge (`item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`), packet-shape pins; C2S rune install mapped to `0x085B` | Service-key numeric logging, complex `CraftStats`/`ChargeCounts`, and `0x084B`/`0x0855`/`0x056C` wire models only | Discovery rolls/unlock mutation, service-key `0x2C`/`0x4F`/`0x57` names, current-craft cadence, `0x084B`/`0x0855` emit intent, non-success sigil precision, `ServerItemMicrochips` (`0x056C`) producer timing |
| F-009 Transport | `TaxiRoute.tbl` runtime loading, clean route/node/location table rejection, rapid transport route resolve, credit/cooldown rejection branches, captured table-backed rapid route debit/cast context, RapidTransport prerequisite type 269 using client taxi-node cast context, contiguous flight-path purchase charge/teleport validation, and `ClientSpellCastWithServiceToken` (`0x00C2`) wire/cost-gate source alignment | `0x00C2` is packet-shape/source-aligned only for generic service-token spell casting; no rapid/taxi service-token branch is proven | Transport service-token bypass, global route state, taxi embark/completion, broader charge/teleport parity, passenger/seat modes, deployable vehicles |
| F-010 Group/matching | Replacement `0x05D5`/`0x0602` validation+logging, zero `ServerRaidQueueStatus` with raid-info plus non-zero wire-order guard, flexible roles, deserter persistence, `Client0x062A`/`Client0x0634` log-only handler guards, `ServerMatching0x05CF` raw `uint32` shape plus correlated apply candidate | `Client0x062A`/`Client0x0634` value logging only until native sender/live UI context is proven; `ServerMatching0x05CF` remains diagnostic/mapped-only until the apply-table index or live payload context is proven; `ServerRaidQueueStatus` row fields are mapped through `0x071A`/`Group_DispatchRaidInfoResponse`, but the standalone non-zero `0x0718` producer/timing remains blocked | Replacement backfill/merge, non-zero raid queue semantics, `0x05CF` field/apply semantics, `Client0x062A`/`Client0x0634` sender intent |
| F-011 Guild | Holomark update handler + guild manager paths exist; cached 2026-06-09 recruitment packet-family recheck source-aligns `0x049F` and `0x0767..0x076F` packet shapes with current models/tests; cached 2026-06-09 war-party boss-token recheck source-aligns `0x0956` request, `0x0951` token-list response rows, and `0x094F` guild-boss-token cast item/context boundary with focused tests | Recruitment detail/subscribe handlers remain log-only; recruitment packet evidence is shape-only without runtime state ownership; war-party token-list handling remains empty and boss-token casts return `BossTokenNotReady` | Bank economy, perks, recruitment persistence, subscribe/update timing, list/detail population, availability semantics, boss-token inventory, cast acceptance/results, match-results population, and warplot semantics |
| F-012 ICComm/chat | Join/message validation, transient membership, typed chat aux packet contracts | Chat aux packets are non-emitted packet contracts only until producer semantics are proven | Entitlement gating, persistent-channel restore, chat aux producer timing/field semantics |
| F-015 Duel/PvP | Duel leash/cancel-warning/disconnect and PvP toggle-off cooldown persistence are implemented/test-pinned | — | Observer/reward parity |
| F-016..F-020 Spells | Per-family fixtures where captured; hostile damage effects use caster-side attack permission so captured Pulse Blast-style damage can flow to combat logs | `!spell procunsupported` etc. | Uncaptured effect families |
| F-021 LAS | Preflight checks where mapped | — | `UpdateSpellInProgress`, async transaction semantics; 2026-06-18 recheck maps client local guard/cache only and names live/native transaction evidence as the unlocker |
| F-031 Fortune | Coin cost, emulator rarity-tier pool, UI probability transport, `account_fortune_session` persistence code path | Local catalog/table audit only (`FORTUNE_WEIGHT_AUDIT.md`); F-007 `RewardRotation*` schedules rejected as Fortune active-rotation evidence; no live Fortune play artifact found | Per-item retail Madame Fay weights and active rotation catalog |

A blocker is **closed** only when the evidence bundle, implementation, tests, and tracker note agree. Anything proven only by current emulator logs but not by retail/native producer semantics is **emulator-parity implemented**, not **retail-parity proven**.

For F-001, an accepted startup STS evidence bundle needs the raw route
transcript from connect through world handoff, matching STS/Auth server logs,
client-visible login result, at least one negative route-order case, and a JSON
or text artifact naming any `/Auth/LoginTokenStart`, `/Auth/TokenKeyData`,
`/Auth/RequestToken`, or `/Auth/AssociateMyExternalAccount` traffic. A capture
that only proves the existing SRP/login-finish/game-token path is useful smoke
evidence but does not unblock token/RSA implementation.

2026-06-17 F-001 blocked recheck: cached `StsConnLib64.MT.dll` fragments still
map only the client-side token/auth support paths, while current source has no
server models or handlers for `/Auth/LoginTokenStart`, `/Auth/TokenKeyData`,
`/Auth/RequestToken`, or `/Auth/AssociateMyExternalAccount`. `StsConn_SendLoginTokenStart`
(`180003d70`) sends `ClientRand`, `StsConn_SendTokenKeyData` (`18000a730`)
reads `ServerRand` / `ServerPublicKey` / `ServerSignature`, calls
`StsConn_ValidateTokenServerKeyMaterial` (`180012de0`), creates an RSA client
through `StsCrypt_CreateRsaClient` (`180037cb0`), and sends
`PremasterSecret`, `AuthnToken`, optional `AuthProviderCode`, and `AppId`;
`StsConn_SendRequestToken` (`180004c40`), `StsConn_SendAssociateMyExternalAccount`
(`1800067d0`), and `StsConn_OnAuthnTokenResponse` (`180008230`) remain support
path evidence. `Test-DecompileManifest.ps1 -Targets StsConnLib64.MT.dll
-FailOnMismatch` currently fails with `label-fingerprint-mismatch`; the Ghidra
project `NexusForeverClient64_StsConnLib64_MT` exists, but no running Ghidra MCP
instance or live startup STS capture is available. The worksheet
`artifacts/blocker_evidence/20260617-225659-F001-sts-token-optional-auth-recheck`
records the missing evidence path and negative cases. Next unblockers are a
refreshed/validated STS decompile export or an accepted startup STS capture that
proves exact RSA key format, signature/trust anchor, premaster derivation,
`TokenKeyData` reply fields, post-token session/crypto transition, and route
ordering before any token/RSA server behavior is added.

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
services.

A 2026-06-19 Q5597 Chua Explosives indicator 17899 activation UI smoke review
created
`artifacts\blocker_evidence\20260619-161627-quest-5597-chua-explosives-indicator-17899-activation-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated quest
world-dependency evidence now records Q5597's `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator03` / WorldLocation2 `17899` as
`runtime_q5597_chua_explosives_indicator17899_spawn_and_credit_tested_pending_client_ui_smoke`.
Current evidence proves emulator-side indicator spawn and virtual-collect credit
only: WorldLocation2 `17899` is in world `870`, WorldZone `1325` (`Scarhide
Camp`), position `-7144,-995.343,-1008.03`, radius `55`, all phases
`4294967295`; QuestObjective `8256` is `VirtualCollect` data `364` count `6`;
TargetGroup `4373` contains Creature2 `24286`; `CrimsonIsleMapScript`
fallback-spawns the 30 reviewed Chua Explosives placements in world `870` area
`1218`; and focused tests load all 30 fallbacks, activate every fallback to
credit virtual item `364` while Q5597 is accepted, remove the entity, reject
missing-quest activation, and avoid duplicate credit. The next unblocker is
accepted live build 16042 evidence that indicator `17899` appears/routes
correctly, Chua Explosives activation/objective UI smokes, Scarhide Camp
density/placement is client-visible, despawn/respawn visual state is correct,
duplicate/negative cases match expectations, and Mondo dialog,
reward/achievement UI, and end-to-end Q5596->Q5597->Q5604 flow are captured.
Focused world-dependency generator coverage passed; content-retail audit
regenerated; validator reports `31` files / `168,101` rows, all
`not_retail_complete`; focused Q5597 xUnit filter passed `15/15`; generated
Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with indicator `17899` still blocker rank `17`
and achievement checklist row `5495` next at blocker rank `18`.

A 2026-06-19 Q5597 Chua Explosives indicator 17897 activation UI smoke review
created
`artifacts\blocker_evidence\20260619-160917-quest-5597-chua-explosives-indicator-17897-activation-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated quest
world-dependency evidence now records Q5597's `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator02` / WorldLocation2 `17897` as
`runtime_q5597_chua_explosives_indicator17897_spawn_and_credit_tested_pending_client_ui_smoke`.
Current evidence proves emulator-side indicator spawn and virtual-collect credit
only: WorldLocation2 `17897` is in world `870`, WorldZone `1325` (`Scarhide
Camp`), position `-7270.21,-992.368,-828.018`, radius `55`, all phases
`4294967295`; QuestObjective `8256` is `VirtualCollect` data `364` count `6`;
TargetGroup `4373` contains Creature2 `24286`; `CrimsonIsleMapScript`
fallback-spawns the 30 reviewed Chua Explosives placements in world `870` area
`1218`; and focused tests load all 30 fallbacks, activate every fallback to
credit virtual item `364` while Q5597 is accepted, remove the entity, reject
missing-quest activation, and avoid duplicate credit. The next unblocker is
accepted live build 16042 evidence that indicator `17897` appears/routes
correctly, Chua Explosives activation/objective UI smokes, Scarhide Camp
density/placement is client-visible, despawn/respawn visual state is correct,
duplicate/negative cases match expectations, and Mondo dialog,
reward/achievement UI, and end-to-end Q5596->Q5597->Q5604 flow are captured.
Focused world-dependency generator coverage passed; content-retail audit
regenerated; validator reports `31` files / `168,101` rows, all
`not_retail_complete`; focused Q5597 xUnit filter passed `15/15`; generated
Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with indicator `17897` still blocker rank `16`
and indicator `17899` next at blocker rank `17`.

A 2026-06-19 Q5597 Chua Explosives indicator 17898 activation UI smoke review
created
`artifacts\blocker_evidence\20260619-155949-quest-5597-chua-explosives-indicator-17898-activation-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated quest
world-dependency evidence now records Q5597's `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator01` / WorldLocation2 `17898` as
`runtime_q5597_chua_explosives_indicator17898_spawn_and_credit_tested_pending_client_ui_smoke`.
Current evidence proves emulator-side indicator spawn and virtual-collect credit
only: WorldLocation2 `17898` is in world `870`, WorldZone `1325` (`Scarhide
Camp`), position `-7022.49,-995.437,-946.002`, radius `55`, all phases
`4294967295`; QuestObjective `8256` is `VirtualCollect` data `364` count `6`;
TargetGroup `4373` contains Creature2 `24286`; `CrimsonIsleMapScript`
fallback-spawns the 30 reviewed Chua Explosives placements in world `870` area
`1218`; and focused tests load all 30 fallbacks, activate every fallback to
credit virtual item `364` while Q5597 is accepted, remove the entity, reject
missing-quest activation, and avoid duplicate credit. The next unblocker is
accepted live build 16042 evidence that indicator `17898` appears/routes
correctly, Chua Explosives activation/objective UI smokes, Scarhide Camp
density/placement is client-visible, despawn/respawn visual state is correct,
duplicate/negative cases match expectations, and Mondo dialog,
reward/achievement UI, and end-to-end Q5596->Q5597->Q5604 flow are captured.
Focused world-dependency generator coverage passed; content-retail audit
regenerated; validator reports `31` files / `168,101` rows, all
`not_retail_complete`; focused Q5597 xUnit filter passed `15/15`; generated
Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with indicator `17898` still blocker rank `15`
and indicator `17897` next at blocker rank `16`.

A 2026-06-19 Q5597 Chua Explosives indicator 17896 activation UI smoke review
created
`artifacts\blocker_evidence\20260619-154422-quest-5597-chua-explosives-indicator-17896-activation-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated quest
world-dependency evidence now records Q5597's `QuestObjective.worldLocationsIdIndicator`
slot `worldLocationsIdIndicator00` / WorldLocation2 `17896` as
`runtime_q5597_chua_explosives_indicator17896_spawn_and_credit_tested_pending_client_ui_smoke`.
Current evidence proves emulator-side indicator spawn and virtual-collect credit
only: WorldLocation2 `17896` is in world `870`, WorldZone `1218`
(`Bloodstone Canyon`), position `-7133.34,-994.206,-856.854`, radius `55`,
all phases `4294967295`; QuestObjective `8256` is `VirtualCollect` data `364`
count `6`; TargetGroup `4373` contains Creature2 `24286`; `CrimsonIsleMapScript`
fallback-spawns the 30 reviewed Chua Explosives placements in world `870` area
`1218`; and focused tests load all 30 fallbacks, activate every fallback to
credit virtual item `364` while Q5597 is accepted, remove the entity, reject
missing-quest activation, and avoid duplicate credit. The next unblocker is
accepted live build 16042 evidence that indicator `17896` appears/routes
correctly, Chua Explosives activation/objective UI smokes, density/placement is
client-visible, despawn/respawn visual state is correct, duplicate/negative cases
match expectations, and Mondo dialog, reward/achievement UI, and end-to-end
Q5596->Q5597->Q5604 flow are captured. Focused world-dependency generator
coverage passed; content-retail audit regenerated; validator reports `31` files /
`168,101` rows, all `not_retail_complete`; focused Q5597 xUnit filter passed
`15/15`; generated Q5597 queue row remains rank `1` with `32` blocker details
and `retail_claim_allowed=false`, with indicator `17896` still blocker rank `14`
and indicator `17898` next at blocker rank `15`.

A 2026-06-19 Q5597 Crimson Isle world-zone map visibility smoke review created
`artifacts\blocker_evidence\20260619-152429-quest-5597-crimson-isle-world-zone-map-visibility-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated quest
world-dependency evidence now records Q5597's `Quest2.worldZoneId` `622`
dependency as
`runtime_q5597_crimson_isle_world_zone_assets_tested_pending_client_visibility_smoke`.
Current evidence proves emulator-side map-surface assets only: Quest2 world zone
`622` is Crimson Isle, `CrimsonIsleMapScript` is owned by world `870`, focused
map-script tests load the map, spawn Mondo Zax `24187` at receiver WorldLocation2
`17803`, and spawn 30 Chua Explosives `24286` placements used by QuestObjective
`8256` (`VirtualCollect` `364`). QuestManager and chain coverage prove Q5597
accept, completion, and follow-up gates only after the server assets are
available. The next unblocker is accepted live build 16042 evidence for
WorldZone `622` map/zone activation and visibility, route/path-arrow or map
guidance, episode/zone ordering, Mondo accept/complete dialog, Chua Explosives
objective UI, rewards, achievements, duplicate/negative cases, and end-to-end
Q5596->Q5597->Q5604 smoke. Focused world-dependency generator coverage passed;
content-retail audit regenerated; validator reports `31` files / `168,101`
rows, all `not_retail_complete`; focused Q5597 xUnit filter passed `15/15`;
generated Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with the world-zone dependency still blocker rank
`12` and the Q5597 Mondo receiver location next at blocker rank `13`.

A 2026-06-19 Q5597 Mondo receiver location dialog/completion smoke review
created
`artifacts\blocker_evidence\20260619-153253-quest-5597-mondo-receiver-location-dialog-completion-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated quest
world-dependency evidence now records Q5597's `Quest2.WorldLocation2IdReceiver`
`17803` dependency as
`runtime_q5597_mondo_receiver_spawn_accept_complete_tested_pending_dialog_ui_smoke`.
Current evidence proves emulator-side receiver spawn and lifecycle gates only:
WorldLocation2 `17803` is the build 16042 Mondo Zax receiver in world `870`,
WorldZone `1227` (`Bloodstone Canyon`), position `-7662.75,-942.948,-671.901`,
radius `1.76183`, all phases `4294967295`; `CrimsonIsleMapScript`
fallback-spawns Creature2 `24187` (`Mondo Zax`) there, matching reviewed
DataMapping `source_coordinate_id` `15089`; Creature2 `24187` carries Q5597 in
`QuestIdGiven` and `QuestIdReceive`; focused QuestManager coverage accepts
Q5597 only with visible Mondo, completed Q5596, Dominion faction, and the level
gate, completes achieved Q5597 only with visible Mondo, grants Quest2Reward rows
`3000` and `5236`, and updates Bloodstone achievement `4134`; `Q5596QuestScript`
grants Q5597 and `Q5597QuestScript` grants Q5604. The next unblocker is
accepted live build 16042 evidence for Mondo accept/completion dialog, receiver
placement/subzone/phase visibility, reward UI and inventory persistence,
achievement UI/progression, duplicate/negative cases, and end-to-end
Q5596->Q5597->Q5604 smoke. Focused world-dependency generator coverage passed;
content-retail audit regenerated; validator reports `31` files / `168,101`
rows, all `not_retail_complete`; focused Q5597 xUnit filter passed `15/15`;
generated Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with the Mondo receiver location still blocker
rank `13` and the Q5597 Chua Explosives objective indicator location next at
blocker rank `14`.

A 2026-06-19 Q5597 Mondo finisher reward/achievement UI smoke review created
`artifacts\blocker_evidence\20260619-144826-quest-5597-mondo-finisher-reward-achievement-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
creature evidence now records Q5597's finisher relation `1564` as
`runtime_q5597_mondo_finisher_spawn_rewards_achievement_and_followup_tested_pending_dialog_ui_smoke`.
Current evidence proves emulator-side completion hooks only: DataMapping
relation `1564` maps Jabbithole creature `3367` to reviewed build 16042
Creature2 `24187` (`Mondo Zax`), Creature2 `24187` carries
`QuestIdReceive=5597`, `CrimsonIsleMapScript` fallback-spawns Mondo at Quest2
receiver WorldLocation2 `17803` matching reviewed DataMapping
`source_coordinate_id` `15089`, focused QuestManager coverage completes
achieved Q5597 only with visible Mondo, grants fixed Quest2Reward rows `3000`
and `5236`, updates Bloodstone achievement `4134`, and Q5597QuestScript grants
Q5604 as the follow-up. The next unblocker is accepted live build 16042 evidence
for Mondo completion dialog, reward UI and inventory persistence, achievement
UI/progression, duplicate/negative completion cases, and end-to-end
Q5597->Q5604 smoke. Focused creature generator coverage passed;
content-retail audit regenerated; validator reports `31` files / `168,101`
rows, all `not_retail_complete`; focused Q5597 xUnit filter passed `15/15`;
generated Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with relation `1564` still blocker rank `9`.

A 2026-06-19 Q5597 Chua Explosives objective activation UI smoke review created
`artifacts\blocker_evidence\20260619-150153-quest-5597-chua-explosives-objective-activation-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
creature evidence now records Q5597's objective relation `1090` as
`runtime_q5597_chua_explosives_30_fallbacks_virtual_collect_guards_tested_pending_client_ui_smoke`.
Current evidence proves emulator-side objective activation only: DataMapping
relation `1090` maps Jabbithole creature `3462` to reviewed build 16042
Creature2 `24286` (`Chua Explosives`), QuestObjective `8256` is
`VirtualCollect` data `364` count `6`, TargetGroup `4373` contains Creature2
`24286`, `CrimsonIsleMapScript` fallback-spawns 30 reviewed placements in world
`870` area `1218`, and `Q5597ChuaExplosivesEntityScript` plus its creature
wrapper credit virtual item `364` while Q5597 is accepted, remove the entity,
ignore missing quest state, and suppress repeated activation. The next
unblocker is accepted live build 16042 evidence for activation/objective UI,
exact density and placement, respawn/despawn visual state, Mondo dialog/client
flow, reward UI and inventory persistence, achievement UI/progression,
duplicate/negative cases, and end-to-end Q5597->Q5604 smoke. Focused creature
generator coverage passed; content-retail audit regenerated; validator reports
`31` files / `168,101` rows, all `not_retail_complete`; focused Q5597 xUnit
filter passed `15/15`; generated Q5597 queue row remains rank `1` with `32`
blocker details and `retail_claim_allowed=false`, with relation `1090` still
blocker rank `10` and starter relation `1922` next at blocker rank `11`.

A 2026-06-19 Q5597 Mondo starter accept dialog/prerequisite UI smoke review
created
`artifacts\blocker_evidence\20260619-151528-quest-5597-mondo-starter-accept-dialog-prereq-ui-smoke-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
creature evidence now records Q5597's starter relation `1922` as
`runtime_q5597_mondo_starter_spawn_accept_gate_and_followup_tested_pending_dialog_ui_smoke`.
Current evidence proves emulator-side starter availability and accept gates
only: DataMapping relation `1922` maps Jabbithole creature `3367` to reviewed
build 16042 Creature2 `24187` (`Mondo Zax`), Creature2 `24187` carries
`QuestIdGiven=5597`, `CrimsonIsleMapScript` fallback-spawns Mondo at Quest2
receiver WorldLocation2 `17803` matching reviewed DataMapping
`source_coordinate_id` `15089` within eight reviewed placements in world `870`
area `1885`, `Q5596QuestScript` grants Q5597 as the follow-up, and focused
QuestManager coverage accepts Q5597 only with visible Mondo, completed Q5596,
Dominion faction, and the level gate satisfied, while rejecting missing Mondo,
missing Q5596, wrong faction, or low level. The next unblocker is accepted live
build 16042 evidence for Mondo accept dialog, prerequisite visibility and denial
text/UI, exact Mondo spawn choice/density, reward/achievement side effects, and
end-to-end Q5596->Q5597->Q5604 smoke. Focused creature generator coverage
passed; content-retail audit regenerated; validator reports `31` files /
`168,101` rows, all `not_retail_complete`; focused Q5597 xUnit filter passed
`15/15`; generated Q5597 queue row remains rank `1` with `32` blocker details
and `retail_claim_allowed=false`, with relation `1922` still blocker rank `11`
and the Q5597 world-zone dependency next at blocker rank `12`.

A 2026-06-19 Q5597 Crimson Isle quest-zone WorldZone 12 bridge review created
`artifacts\blocker_evidence\20260619-143751-quest-5597-crimson-isle-quest-zone-worldzone12-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated zone
evidence now records Q5597's `quest_zones` relation `633` as
`datamapping_q5597_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof`
rather than generic matched-zone runtime visibility smoke. Current evidence
proves only a stale or incomplete DataMapping route candidate: archived
Jabbithole zone `9` (`Crimson Isle`) maps to WorldZone `12` with
`match_status=matched` and source `last_seen_in=3`, but current build 16042
`world_zone_client_map.csv` shows WorldZone `12` has no client name, parent zone
`0`, `allow_access=0`, and no Crimson Isle ownership. Current named Crimson
Isle evidence instead uses Q5597's `Quest2.worldZoneId` `622` plus child rows
`623`, `629`, `1217`, `1218`, `1219`, `1227`, and `1284`. The next unblocker is
a reviewed current WorldZone replacement, MapZone or QuestDirection evidence,
or accepted live build 16042 route, visibility, and map-guidance smoke,
followed by episode/zone ordering, Mondo dialog, objective UI, rewards,
achievements, duplicate/negative cases, and end-to-end Q5597 smoke.
Focused zone generator coverage passed; content-retail audit regenerated;
validator reports `31` files / `168,101` rows, all `not_retail_complete`;
focused Q5597 xUnit filter passed `15/15`; generated Q5597 queue row remains
rank `1` with `32` blocker details and `retail_claim_allowed=false`, with
relation `633` still blocker rank `8`.

A 2026-06-19 Q5597 Auroria call-zone WorldZone 6 missing bridge review
created
`artifacts\blocker_evidence\20260619-142700-quest-5597-auroria-call-zone-worldzone6-missing-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated zone
evidence now records Q5597's `quest_call_zones` relation `4641` as
`datamapping_q5597_auroria_call_zone_worldzone6_missing_reviewed_nonprimary_pending_route_proof`
rather than generic partial-zone review. Current evidence proves only a stale
or incomplete DataMapping route candidate: archived Jabbithole zone `5`
(`Auroria`) maps to WorldZone `6` with `match_status=partial` and source
`last_seen_in=0`, but current build 16042 `world_zone_client_map.csv` has no
WorldZone `6` row or client name. Current named Auroria evidence instead uses
WorldZone `36` (`Auroria`) plus child rows `697` (`Northeastern Auroria`),
`1228` (`Northwestern Auroria`), `1302` (`Central Auroria`), and `1303`
(`Southern Auroria`), and Q5597's own current `Quest2.worldZoneId` remains
`622` (`Crimson Isle`). The next unblocker is a reviewed current WorldZone
replacement, MapZone or QuestDirection evidence, or accepted live build 16042
route, visibility, and map-guidance smoke, followed by episode/zone ordering,
Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases,
and end-to-end Q5597 smoke. Focused zone generator coverage passed;
content-retail audit regenerated; validator reports `31` files / `168,101`
rows, all `not_retail_complete`; focused Q5597 xUnit filter passed `15/15`;
generated Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with relation `4641` still blocker rank `7`. A 2026-06-19 Q5597 Malgrave call-zone WorldZone 42 Protostar Honeyworks bridge review
created
`artifacts\blocker_evidence\20260619-141909-quest-5597-malgrave-call-zone-worldzone42-protostar-honeyworks-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated zone
evidence now records Q5597's `quest_call_zones` relation `4640` as
`datamapping_q5597_malgrave_call_zone_worldzone42_protostar_honeyworks_reviewed_nonprimary_pending_route_proof`
rather than generic matched-zone runtime visibility smoke. Current evidence
proves only a stale or incomplete DataMapping route candidate: archived
Jabbithole zone `35` (`Malgrave`) maps to current WorldZone `42` (`Protostar
Honeyworks`) with source `last_seen_in=0`, while current build 16042 Malgrave
evidence uses WorldZone `377` (`Malgrave`), WorldZone `1711` (`The Malgrave
Trail`), WorldZone `1890` (`Southern Malgrave`), WorldZone `1928` (`Central
Malgrave`), WorldZone `1933` and `2433` (`Malgrave`), and WorldZone `4372`
(`Malgrave`), and Q5597's own current `Quest2.worldZoneId` remains `622`
(`Crimson Isle`). The next unblocker is a reviewed current WorldZone
replacement, MapZone or QuestDirection evidence, or accepted live build 16042
route, visibility, and map-guidance smoke, followed by episode/zone ordering,
Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases,
and end-to-end Q5597 smoke. Focused zone generator coverage passed;
content-retail audit regenerated; validator reports `31` files / `168,101`
rows, all `not_retail_complete`; focused Q5597 xUnit filter passed `15/15`;
generated Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with relation `4640` still blocker rank `6`. A 2026-06-19 Q5597 Northern Wastes call-zone WorldZone 41 Fort Glory bridge review
created
`artifacts\blocker_evidence\20260619-140947-quest-5597-northern-wastes-call-zone-worldzone41-fort-glory-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated zone
evidence now records Q5597's `quest_call_zones` relation `4639` as
`datamapping_q5597_northern_wastes_call_zone_worldzone41_fort_glory_reviewed_nonprimary_pending_route_proof`
rather than generic matched-zone runtime visibility smoke. Current evidence
proves only a stale or incomplete DataMapping route candidate: archived
Jabbithole zone `34` (`Northern Wastes`) maps to current WorldZone `41` (`Fort
Glory`) with source `last_seen_in=0`, while current build 16042 Northern Wastes
evidence uses WorldZone `1784`, child WorldZone `1785`, and child rows `1786`,
`1787`, `1788`, `1790`, `1792`, `1798`, `4036`, `4374`, `4377`, `4378`, and
`4483`, and Q5597's own current `Quest2.worldZoneId` remains `622` (`Crimson
Isle`). The next unblocker is a reviewed current WorldZone replacement,
MapZone or QuestDirection evidence, or accepted live build 16042 route,
visibility, and map-guidance smoke, followed by episode/zone ordering, Mondo
dialog, objective UI, rewards, achievements, duplicate/negative cases, and
end-to-end Q5597 smoke. Focused zone generator coverage passed; content-retail
audit regenerated; validator reports `31` files / `168,101` rows, all
`not_retail_complete`; focused Q5597 xUnit filter passed `15/15`; generated
Q5597 queue row remains rank `1` with `32` blocker details and
`retail_claim_allowed=false`, with relation `4639` still blocker rank `5`. A 2026-06-19 Q5597 Housing Skymap call-zone WorldZone 60 Mozyk Quarry bridge review
created
`artifacts\blocker_evidence\20260619-135937-quest-5597-housing-skymap-call-zone-worldzone60-mozyk-quarry-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated zone
evidence now records Q5597's `quest_call_zones` relation `4638` as
`datamapping_q5597_housing_skymap_call_zone_worldzone60_mozyk_quarry_reviewed_nonprimary_pending_route_proof`
rather than generic matched-zone runtime visibility smoke. Current evidence
proves only a stale or incomplete DataMapping route candidate: archived
Jabbithole zone `40` (`Housing Skymap`) maps to current WorldZone `60` (`Mozyk
Quarry`) with source `last_seen_in=0`, while current build 16042
Housing/Skymap evidence uses parent WorldZone `1630` (`Housing`), WorldZone
`1136` and `4373` (`Skymap`), and WorldZone `1265` and `4837` (`Community
Skymap`), and Q5597's own current `Quest2.worldZoneId` remains `622` (`Crimson
Isle`). The next unblocker is a reviewed current WorldZone replacement,
MapZone or QuestDirection evidence, or accepted live build 16042 route,
visibility, and map-guidance smoke, followed by episode/zone ordering, Mondo
dialog, objective UI, rewards, achievements, duplicate/negative cases, and
end-to-end Q5597 smoke. A 2026-06-19 Q5597 Illium call-zone WorldZone 78 Ellevar bridge review
created
`artifacts\blocker_evidence\20260619-135045-quest-5597-illium-call-zone-worldzone78-ellevar-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated zone
evidence now records Q5597's `quest_call_zones` relation `4637` as
`datamapping_q5597_illium_call_zone_worldzone78_ellevar_reviewed_nonprimary_pending_route_proof`
rather than generic matched-zone runtime visibility smoke. Current evidence
proves only a stale or incomplete DataMapping route candidate: archived
Jabbithole zone `46` (`Illium`) maps to current WorldZone `78` (`Western
Ellevar`) with source `last_seen_in=0`, while current build 16042 Illium
evidence uses WorldZone `2191` plus child WorldZone rows `2192`, `2193`,
`2194`, `2195`, `2196`, `3014`, `4202`, `4296`, `4298`, `4365`, `4485`,
`4486`, and `4957`, and Q5597's own current `Quest2.worldZoneId` remains `622`
(`Crimson Isle`). The next unblocker is a reviewed current WorldZone
replacement, MapZone or QuestDirection evidence, or accepted live build 16042
route, visibility, and map-guidance smoke, followed by episode/zone ordering,
Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases,
and end-to-end Q5597 smoke. A 2026-06-19 Q5597 Crimson Isle call-zone WorldZone 12 bridge review
created
`artifacts\blocker_evidence\20260619-133236-quest-5597-crimson-isle-call-zone-worldzone12-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
zone evidence now records Q5597's `quest_call_zones` relation `311` as
`datamapping_q5597_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof`
rather than generic matched-zone runtime visibility smoke. Current evidence
proves only a stale or incomplete DataMapping route candidate: archived
Jabbithole zone `9` (`Crimson Isle`) maps to WorldZone `12`, but
`world_zone_client_map.csv` shows WorldZone `12` has no client name, parent
zone `0`, `allow_access=0`, and no Crimson Isle ownership. Current named
Crimson Isle evidence instead uses Q5597 `Quest2.worldZoneId` `622` plus child
WorldZone rows `623`, `629`, `1217`, `1218`, `1219`, `1227`, and `1284` under
parent `622`. The next unblocker is a reviewed current WorldZone replacement,
MapZone or QuestDirection evidence, or accepted live build 16042 route,
visibility, and map-guidance smoke, followed by episode/zone ordering, Mondo
dialog, objective UI, rewards, achievements, duplicate/negative cases, and
end-to-end Q5597 smoke. A 2026-06-19 Q5597 episode order chain handoff review created
`artifacts\blocker_evidence\20260619-131927-quest-5597-episode-order-chain-handoff-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
episode evidence now records Q5597's EpisodeQuest row `2398` / Episode `464`
(`The Guns of Bloodstone`) as
`runtime_q5597_episode_order_q5596_q5597_q5604_handoff_tested_pending_client_episode_smoke`
rather than generic pending episode progression smoke. Current tests prove only
emulator chain ownership and table/order alignment: EpisodeQuest order `2` /
flags `0` in WorldZone `622` maps to Jabbithole episode `98`,
`Q5596QuestScript` grants Q5597, `Q5597QuestScript` grants Q5604, and
`Q5604TacticalDemolitionsQuestScript` grants Q5580/Q5583 on completion. The
next unblocker is live build 16042 episode tracker/order UI presentation, quest
visibility and turn-in state, Mondo dialog, Chua Explosives objective UI,
reward and achievement side effects, duplicate/negative cases, and end-to-end
Q5596->Q5597->Q5604 smoke. A 2026-06-19 Q5597 Crimson Isle world-zone map-surface review created
`artifacts\blocker_evidence\20260619-130912-quest-5597-crimson-isle-world-zone-map-surface-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
world-dependency evidence now records Q5597's `Quest2.worldZoneId` `622`
(`Crimson Isle`) as server-map-surface covered rather than generic pending map
activation smoke. Current tests prove only emulator-side availability:
`CrimsonIsleMapScript` is owned by world `870`, focused tests load the map
script, spawn Mondo Zax `24187` at receiver WorldLocation2 `17803`, and spawn
the reviewed Chua Explosives `24286` placements used by objective `8256`. The
next unblocker is live build 16042 map/zone activation and visibility,
episode/zone ordering, path or map guidance, Mondo dialog, objective UI,
reward and achievement side effects, duplicate/negative cases, and end-to-end
Q5597 smoke. A 2026-06-19 Q5597 prerequisite accept-gate review created
`artifacts\blocker_evidence\20260619-125638-quest-5597-prerequisite-accept-gate-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
prerequisite evidence now records Q5597's level `1`, Dominion faction `1`, and
required Quest2 `5596` rows as focused-test-backed server accept gates rather
than generic pending row smoke. Current QuestManager coverage proves only the
emulator gate: Q5597 accepts with visible Mondo Zax `24187`, Dominion faction,
the level gate satisfied, and Q5596 complete, and rejects missing Mondo,
missing Q5596, wrong faction, or a low level. The next unblocker is live build
16042 Mondo availability/dialog and denial UI, Q5596-to-Q5597 chain
presentation, rewards, achievements, duplicate/negative cases, and end-to-end
Q5597 smoke. A 2026-06-19 Q5597 Chua Explosives script quest-state guard review
created
`artifacts\blocker_evidence\20260619-124414-quest-5597-chua-explosives-script-quest-state-guard-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
Script.Main progression evidence now resolves `Q5597.cs` line `49`
`ObjectiveUpdate(QuestObjectiveType.VirtualCollect, 364, 1)` to QuestObjective
`8256` and records the surrounding `GetQuestState(5597)` accepted-state check
as `owner_link_status=quest_state_guard_matches_quest`. The row remains
`script_quest_typed_objective_update_matched_pending_trigger_smoke` /
`not_retail_complete`; the next unblocker is live build 16042 trigger timing,
client objective UI, duplicate/repeat behavior, reward and achievement side
effects, and source-drop versus activation-only proof. A 2026-06-19 Q5597 Chua Explosives virtual-loot source review created
`artifacts\blocker_evidence\20260619-123609-quest-5597-chua-explosives-virtual-loot-source-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
quest-loot evidence now records both LaughingWS loot groups `1200000482` and
`1200001069`, their item type `6` / staticId `364` child rows, and entity
`24286` bindings as matching VirtualCollect QuestObjective `8256` data `364`.
`LootInstanceItem.cs` and `LootInstanceDeliveryTests.cs` prove the generic
runtime VirtualItem delivery path updates `QuestObjectiveType.VirtualCollect`,
but Q5597 also has direct `Q5597ChuaExplosivesEntityScript` activation credit.
The next unblocker is live build 16042 source/drop evidence or decompile/table
producer proof that decides drop-source versus activation-only behavior, then
client-visible loot UI, drop cadence, inventory cleanup, duplicate/negative
cases, and end-to-end Q5597 smoke. A 2026-06-19 Q5597 Auroria stale WorldZone blocker classification
created
`artifacts\blocker_evidence\20260619-122203-quest-5597-auroria-stale-worldzone-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`. Regenerated
quest-zone evidence now records `quest_call_zones` source_relation_id `4641` /
Jabbithole zone `5` (`Auroria`) -> WorldZone `6` as partial because current
build 16042 WorldZone evidence has no WorldZone `6` row/name. Current candidate
context includes WorldZone `36` (`Auroria`) and child rows such as WorldZone
`1228` (`Northwestern Auroria`), but no replacement is promoted from names
alone; Q5597's own Quest2 WorldZone remains `622` (`Crimson Isle`). The next
unblocker is reviewed WorldZone.tbl, MapZone/QuestDirection, decompile/table
route evidence, or accepted live client route smoke that identifies the current
replacement and then proves quest visibility, map guidance, episode ordering,
and Q5597 client flow. A 2026-06-19 Q5597 Chua Explosives bridge review created
`artifacts\blocker_evidence\20260619-121140-quest-5597-chua-explosives-bridge-review`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; full
DataMapping and content-retail audit refresh now report Jabbithole creature
`3462` -> Creature2 `24286` as a reviewed bridge for objective relation
`1090`, backed by QuestObjective `8256` (`VirtualCollect`, data `364`, count
`6`), TargetGroup `4373`, all `30` world `870` / area `1218` source
coordinates, `CrimsonIsleMapScript` fallback placements, and
`Q5597ChuaExplosivesEntityScript` virtual item `364` credit while Q5597 is
accepted. Focused `--no-build` Q5597 tests (`15/15`) and generated validation
(`31` files / `168,101` rows, all `not_retail_complete`) prove only the
current server-side bridge and runtime fallback behavior. The row remains
blocked pending live build 16042 Mondo dialog/receiver flow, Chua Explosives
activation and objective UI timing, exact density, respawn/despawn, reward and
achievement UI, Q5596->Q5597->Q5604 chain behavior, and named negative cases.
A 2026-06-19 Q5597 validation-gate recheck created
`artifacts\blocker_evidence\20260619-002956-quest-5597-dregs-and-thieves-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q5597 tests (`15/15`) prove only current server-side
fallback spawn, virtual-collect credit, reward, and achievement coverage.
Online Jabbithole corroborates the Crimson Isle quest surface, Mondo Zax
start/complete flow, Chua Explosives objective, and listed rewards, but the
worksheet does not close Q5597 because no live client screenshots/video,
server/client log excerpts, or negative-case observations were captured. The
next unblocker is to complete that bundle, or rerun the harness without
`-CreateBundleOnly`, and capture live build 16042 Mondo dialog/receiver flow,
Chua Explosives activation/UI credit timing, exact density/respawn/despawn,
reward UI/inventory persistence, achievement UI, Q5596->Q5597->Q5604 chain
behavior, and the named negative cases. A 2026-06-19 Q3479 validation-gate
recheck created
`artifacts\blocker_evidence\20260619-003639-quest-3479-from-the-wreckage-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3479 tests (`38/38`) prove only current server-side
fallback spawn, Bosun/Deadeye indexing, trapped-survivor CSI credit,
shared-yeti kill credit, reward, cash, and achievement coverage. Online
Jabbithole corroborates the Northern Wilds quest surface, Bosun Redmark
start flow, Deadeye Brightland completion flow, Trapped Survivor/yeti
objectives, and listed reward choices, but the worksheet does not close Q3479
because no live client screenshots/video, server/client log excerpts, or
negative-case observations were captured. The next unblocker is to complete
that bundle, or rerun the harness without `-CreateBundleOnly`, and capture
live build 16042 Bosun accept flow, Deadeye completion dialog, survivor CSI
activation/UI credit timing, yeti combat/tap/threat/respawn behavior,
reward-choice UI/inventory persistence, achievement UI, Q4106->Q3479->Q3667
chain behavior, Q3480 exclusion behavior, map/episode guidance, and the named
negative cases. A 2026-06-19 Q3480 validation-gate recheck created
`artifacts\blocker_evidence\20260619-004632-quest-3480-reporting-for-duty-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3480 tests (`36/36`) prove only current server-side
fallback spawn, Commander Durek/Deadeye indexing, Q3479 exclusion gating,
trapped-survivor CSI credit, shared-yeti kill credit, reward, cash,
achievement, and Q3480->Q3667 follow-up coverage. Online Jabbithole
corroborates the Northern Wilds quest surface, Commander Durek start flow,
Deadeye Brightland completion flow, Trapped Survivor/yeti objectives, and
listed reward choices, but the worksheet does not close Q3480 because no live
client screenshots/video, server/client log excerpts, or negative-case
observations were captured. The next unblocker is to complete that bundle, or
rerun the harness without `-CreateBundleOnly`, and capture live build 16042
Commander Durek accept flow, Deadeye completion dialog, survivor CSI
activation/UI credit timing, yeti combat/tap/threat/respawn behavior,
reward-choice UI/inventory persistence, achievement UI, Q3479 exclusion
behavior, Q3480->Q3667 chain behavior, map/episode guidance, and the named
negative cases. A 2026-06-19 Q3486 validation-gate recheck created
`artifacts\blocker_evidence\20260619-004925-quest-3486-empowered-tower-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3486 tests (`34/34`) prove only current server-side
fallback spawn, Master Control Panel receiver visibility, accepted-in-zone
arrival story/objective credit, Loftite proximity collection, Crystal
Guardian/Frostbite target-group credit, selectable reward, cash, achievement,
completion cinematic, and Q3671/Q3797 quest-mention coverage. Online
Jabbithole corroborates the Northern Wilds quest surface, Master Control Panel
start/complete flow, power-cable and Loftite/Crystal Guardian objectives, and
listed reward choices, but the worksheet does not close Q3486 because no live
client screenshots/video, server/client log excerpts, or negative-case
observations were captured. The next unblocker is to complete that bundle, or
rerun the harness without `-CreateBundleOnly`, and capture live build 16042
Master Control Panel accept/complete flow, arrival story-panel and objective
timing, Loftite Crystal proximity collection/despawn behavior, Crystal Guardian
and Frostbite combat/drop/credit timing, reward-choice UI/inventory
persistence, achievement UI, Q3667->Q3486->Q3671/Q3797 chain behavior,
map/episode guidance, and the named negative cases. Source reward `5319`
(`Item2` `1377`, Jabbithole game reward `2147`, canonical reward row `1707`)
remains blocked as a provenance collision until source review or accepted
client/runtime evidence resolves it. A 2026-06-19 Q3668 validation-gate
recheck created
`artifacts\blocker_evidence\20260619-005231-quest-3668-indigenous-intelligence-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3668 tests (`14/14`) prove only current server-side
Bartol receiver visibility, Scientist Lusk fallback, Imprisoned Survivor
fallbacks and activate target-group credit, reviewed Skeech nested target-group
fallbacks, generic kill-credit expansion, selectable reward, pushed item
dependency tracking, and achievement coverage. Online Jabbithole corroborates
the Northern Wilds quest surface, Deadeye Brightland start flow, Bartol
Sunward completion flow, Scientist Lusk talk objective, Imprisoned Survivor
rescue objective, Coldburrow Cavern/Skeech kill context, and listed reward
choices, but the worksheet does not close Q3668 because no live client
screenshots/video, server/client log excerpts, or negative-case observations
were captured, and it does not resolve missing target-group members `14054` or
`11913`. The next unblocker is to complete that bundle, or rerun the harness
without `-CreateBundleOnly`, and capture live build 16042 Deadeye accept flow,
Lusk dialog/activation and objective credit, Imprisoned Survivor
activate/transform/despawn timing, Skeech combat/tap/threat/respawn/loot
behavior, Bartol completion dialog, reward-choice UI/inventory persistence,
pushed item `6912` accept/abandon/replay behavior, achievement UI,
Q3486/Q3671/Q3668 chain behavior, zone/routing guidance, and the named negative
cases. Target-group members `14054` and `11913` remain blocked pending reviewed
current Creature2/source-coordinate evidence, client/decompile table proof, or
accepted live/retail capture. A 2026-06-19 Q3673 validation-gate recheck
created
`artifacts\blocker_evidence\20260619-005712-quest-3673-contact-with-thayd-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3673 tests (`26/26`) prove only current server-side
Signal Flare fallbacks, checklist index assignment, SucceedCSI and
ActivateTargetGroupChecklist credit coverage, hidden objective `13391`
duplicate suppression, completion cinematic, follow-up grant, selectable reward,
and achievement coverage. Online Jabbithole corroborates the Northern Wilds
quest surface, Deadeye Brightland start/complete flow, call and Unknown
completion surface, Signal Flare objectives, and listed currency reward; the
WildStar Wiki independently corroborates the Signal Flare rescue objective. The
worksheet does not close Q3673 because no live client screenshots/video,
server/client log excerpts, or negative-case observations were captured, and it
does not resolve the unmatched finisher source_relation_id `1842` / Jabbithole
creature `23645` (`Unknown`). The next unblocker is to complete that bundle, or
rerun the harness without `-CreateBundleOnly`, and capture live build 16042
Deadeye/call accept and completion flow, Signal Flare activation and CSI packet
timing, checklist and hidden-objective timing, cinematic timing, reward-choice
UI/inventory persistence, achievement UI, Q3886->Q3673->Q3670 chain behavior,
zone/routing guidance, and the named negative cases. The Unknown finisher bridge
remains blocked pending reviewed current Creature2/source-coordinate evidence,
a replacement relation, client/decompile table proof, or accepted live/retail
capture. A 2026-06-19 Q3886 validation-gate recheck created
`artifacts\blocker_evidence\20260619-010104-quest-3886-fiery-distraction-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3886 tests (`11/11`) prove only current server-side
Commander Durek starter/receiver relation, Burning Torch fallback entities and
SucceedCSI credit, Skeech Hut fallback entities and TargetGroup `1460`
checklist indexes, Q3886->Q3673 follow-up handoff, selectable reward, and
achievement coverage. Online Jabbithole corroborates the Northern Wilds /
Shipwrecked and Coldburrow Cavern quest surface, Commander Durek start/complete
flow, Burning Torch and Skeech Hut objectives, and listed cash/item-choice
rewards, but the worksheet does not close Q3886 because no live client
screenshots/video, server/client log excerpts, or negative-case observations
were captured, and it does not resolve zone bridge/routing semantics. The next
unblocker is to complete that bundle, or rerun the harness without
`-CreateBundleOnly`, and capture live build 16042 Commander Durek/call accept
flow and Commander Durek completion dialog, Burning Torch activation and CSI
packet timing, Skeech Hut ActivateTargetGroupChecklist credit/despawn/respawn
behavior, reward-choice UI/inventory persistence, achievement UI,
Q3886->Q3673 chain behavior, zone/routing guidance for zone `1` partial matches
and Coldburrow/Northern Wilds `202->219` visibility, and the named negative
cases. A 2026-06-19 Q3963 validation-gate recheck created
`artifacts\blocker_evidence\20260619-010411-quest-3963-more-important-than-revenge-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q3963/teleporter-boundary tests (`16/16`) prove only
current server-side Q3487->Q3963 handoff, Q3963 lifecycle logging, Northern
Wilds Ship Controls fallback entities `27196` at `45401`/`45402`,
ActivateEntity credit for `27196` in world `426`, no teleporter behavior for
`27196` in Northern Wilds, Galeras Deadeye receiver `16622` completion gating,
selectable reward, and achievement coverage. Online Jabbithole corroborates the
Northern Wilds / Vengeance quest surface, Deadeye Brightland start/complete
flow, call and Unknown start surface, Ship Controls objective, and cash reward,
but the worksheet does not close Q3963 because no live client screenshots/video,
server/client log excerpts, or negative-case observations were captured, and it
does not resolve the local selectable-item reward UI, zone bridge/routing
semantics, or unmatched starter source_relation_id `2130` / Jabbithole creature
`23645` (`Unknown`). The next unblocker is to complete that bundle, or rerun the
harness without `-CreateBundleOnly`, and capture live build 16042
Deadeye/call/Unknown start behavior, Ship Controls activation and ActivateEntity
packet/UI timing, confirmation that Northern Wilds controls do not behave as
transporters, Galeras Deadeye completion dialog, reward-choice UI/inventory
persistence, achievement UI, Q3487->Q3963 chain and Algoroc/Galeras handoff
visibility, zone/routing guidance for zone `1` partial matches and Algoroc
`13->17` visibility, and the named negative cases. The Unknown starter bridge
remains blocked pending reviewed current Creature2/source-coordinate evidence,
a replacement relation, client/decompile table proof, or accepted live/retail
capture. A 2026-06-19 Q5573 validation-gate recheck created
`artifacts\blocker_evidence\20260619-011205-quest-5573-powering-down-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q5573 tests (`3/3`) prove only current server-side
Q5593->Q5573 grant, Q5573->Q5596 branch grant, reviewed Power Regulator
`24999` fallback placements with checklist indexes `1..3`, completion
cinematic queue, hidden objective `12870` credit, and achieved-state duplicate
suppression. Online Jabbithole corroborates the Crimson Isle / Operation
Shieldbreaker quest surface, Mondo Zax/call start and completion flow, Megatech
Shield Generator and Power Regulator objectives, Power Regulator as the
involved objective NPC/object, and listed cash/choice rewards, but the worksheet
does not close Q5573 because no live client screenshots/video, server/client
log excerpts, or negative-case observations were captured, and it does not
resolve the partial Crimson Badlands zone bridge. The next unblocker is to
complete that bundle, or rerun the harness without `-CreateBundleOnly`, and
capture live build 16042 Mondo/call accept and completion flow, objective
`8524` trigger timing, Power Regulator activation and
ActivateTargetGroupChecklist packet/UI timing, active-prop visual/despawn/
respawn behavior, cinematic timing, reward-choice UI/inventory persistence,
achievement UI, Q5593->Q5573->Q5596 branch behavior, Crimson Isle and Crimson
Badlands visibility/routing, and the named negative cases. The `quest_call_zones`
source_relation_id `2122` / Jabbithole zone `65` -> WorldZone `131` partial
bridge remains blocked pending reviewed current WorldZone evidence or accepted
live/client proof. A 2026-06-19 Q5575 validation-gate recheck created
`artifacts\blocker_evidence\20260619-011828-quest-5575-seizing-power-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q5575 tests (`3/3`) prove only current server-side
Q5595->Q5575 grant, Q5575->Q5596 branch grant, reviewed Power Regulator
`24999` fallback placements with checklist indexes `1..3`, hidden objective
`12871` credit after objective `8371`, no unproven cinematic queue, and
achieved-state duplicate suppression. Online Jabbithole corroborates the
Crimson Isle / Operation Shieldbreaker quest surface, Kezrek Warbringer/call
start surface, call completion flow, Megatech Shield Generator and Power
Regulator objectives, Power Regulator as the involved objective NPC/object, and
listed cash/choice rewards, but the worksheet does not close Q5575 because no
live client screenshots/video, server/client log excerpts, or negative-case
observations were captured, and it does not prove Kezrek runtime spawn/dialog
routing. The next unblocker is to complete that bundle, or rerun the harness
without `-CreateBundleOnly`, and capture live build 16042 Q5595->Q5575
grant/accept, Kezrek/call starter dialog and call completion flow, objective
`8523` trigger timing, Power Regulator activation and ActivateTargetGroupChecklist
packet/UI timing, active-prop visual/despawn/respawn behavior, hidden-objective
timing, reward-choice UI/inventory persistence, achievement UI, Q5575->Q5596
branch behavior, Crimson Isle visibility/routing, and the named negative cases.
Starter source_relation_id `2212` / Jabbithole creature `3385` (`Kezrek
Warbringer`) -> Creature2 `24158` remains reviewed but retail-blocked pending
runtime spawn/dialog proof or accepted client/live evidence. A 2026-06-19 Q5580
validation-gate recheck created
`artifacts\blocker_evidence\20260619-012209-quest-5580-enforced-radio-silence-recheck`
with `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch`; local source and
focused `--no-build` Q5580 tests (`2/2`) prove only current server-side
Q5604->Q5580/Q5583 grant, Q5580/Q5583->Q5594 merge gating, reviewed Tower
Controls `26559` fallback placements with checklist indexes `1..2`, objective
`8227` / TargetGroup `2889` ActivateTargetGroupChecklist coverage, and Kezrek
Warbringer `24158` receiver fallback plus reviewed receiver override. Online
Jabbithole corroborates the Crimson Isle / Operation Annihilator quest surface,
Kezrek Warbringer/call start and completion surface, the Tower Controls
objective at Megatech Station, Tower Controls as the involved objective
NPC/object, and listed cash/six weapon choice rewards, but the worksheet does
not close Q5580 because no live client screenshots/video, server/client log
excerpts, or negative-case observations were captured, and it does not prove
Kezrek runtime dialog/routing or Tower Controls activation timing. The next
unblocker is to complete that bundle, or rerun the harness without
`-CreateBundleOnly`, and capture live build 16042 Q5604->Q5580/Q5583
grant/accept, Kezrek starter/dialog and completion flow or confirmed
script-grant-only retail route, Tower Controls activation and
ActivateTargetGroupChecklist packet/UI timing, active-prop visual/despawn/respawn
behavior, quest direction/map guidance, reward-choice UI/inventory persistence,
achievement UI, Q5580/Q5583->Q5594 merge behavior, Crimson Isle visibility/
routing, and the named negative cases. Kezrek source_relation_id `465`/`1559`
/ Jabbithole creature `3385` (`Kezrek Warbringer`) -> Creature2 `24158` remains
reviewed but retail-blocked pending runtime dialog proof or accepted client/live
evidence. A 2026-06-17 Q5597 create-bundle recheck fixed the printed client log
tail paths for root client installs such as `I:\WildStar`: the harness now uses
the same resolved client log root for console tail hints and generated helper
scripts, so future bundles point at `I:\WildStar\Logs` and
`I:\WildStar\Errors` instead of the parent drive root. The corrected Q5597
bundle skeleton is
`artifacts\blocker_evidence\20260617-213642-quest-5597-dregs-and-thieves`;
it is a worksheet only and does not close the Q5597 client-smoke blocker.
The Q3479 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-214306-quest-3479-from-the-wreckage`;
it records objectives `4467;4564`, rewards `2476;2479;8569`, achievements
`1432;1433;1434`, and the negative cases required for the next manual
Bosun/Deadeye/survivor/yeti/reward/achievement client-smoke pass, but it does
not close the Q3479 `datamapping_review` or client-smoke blockers. The
2026-06-17 focused recheck passed `38/38` with
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v
minimal --nologo --filter "FullyQualifiedName~Q3479"`.
The Q3480 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-214531-quest-3480-reporting-for-duty`;
it records objectives `4470;4565`, rewards `1676;1677;1678`, achievement
`5327`, and the negative cases required for the next manual Commander
Durek/Deadeye/survivor/yeti/reward/achievement client-smoke pass, but it does
not close the Q3480 `client_smoke` blocker.
The Q3486 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-214714-quest-3486-empowered-tower`;
it records objectives `4987;4485`, rewards `1706;1707;2147`, achievements
`3469;5327`, and the negative cases required for the next manual Master Control
Panel/arrival/Loftite/Crystal Guardian/Frostbite/reward/achievement
client-smoke pass, but it does not close the Q3486 `client_smoke` blocker or
the source reward `5319` provenance collision.
The Q3668 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-215006-quest-3668-indigenous-intelligence`;
it records objectives `4744;4745;4791`, rewards `2190;2191;4768`, achievement
`3490`, and the negative cases required for the next manual
Deadeye/Lusk/Bartol/survivor/Skeech/reward/achievement client-smoke pass.
TargetGroup members `14054` and `11913` remain blocked on reviewed current
Creature2/source-coordinate evidence: the local DataMapping output maps source
`14054` to non-Q3668 travel station Creature2 `25857`, and exact local
creature/spawn map searches found no `11913` row.
The Q3673 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-215214-quest-3673-contact-with-thayd`;
it records objectives `4748;4888;4889;4890;13391`, rewards
`2187;2188;2189;2481;3001;3846`, achievement `3490`, and the negative cases
required for the next manual Deadeye/Signal-Flare/hidden-completion/reward/
achievement client-smoke pass. Source relation `finisher:1842:23645` remains
blocked on reviewed current Creature2 bridge evidence because local
DataMapping keeps Jabbithole creature `23645` unmatched with zero candidates.
The Q3886 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-215359-quest-3886-fiery-distraction`;
it records objectives `5053;5052`, rewards `2192;2430;4767`, achievement
`3490`, duplicate relation `8506`, zone-review rows `76;5459;235;3439`, and
the negative cases required for the next manual Durek/Burning-Torch/Skeech-Hut/
reward/achievement/Q3673-handoff client-smoke pass. The zone rows and duplicate
Burning Torch relation remain review blockers until client-visible routing and
placement behavior are captured.
The Q3963 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-215544-quest-3963-more-important-than-revenge`;
it records objective `5201`, rewards `3706;3707;4771`, achievement `3491`,
the `11063` starter-context caveat, unmatched relation `starter:2130:23645`,
and the negative cases required for the next manual Ship-Controls/Galeras-
Deadeye/reward/achievement client-smoke pass. The focused verification also
includes the Ship Controls non-teleport regression for Northern Wilds world
`426`; retail completion remains blocked until the `11063`/`23645` starter
identity and client-visible Q3487-to-Q3963 flow are captured.
The Q5573 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-215735-quest-5573-powering-down`;
it records objectives `8524;8229;12870`, rewards `2989;2991;2992`, achievement
`4132`, Power Regulator source-coordinate ids `15177;15176;15178`, unpromoted
Mondo Zax source-coordinate ids `15087;15088;15089;21174;136782;1863809;2480207;2561752`,
and the negative cases required for the next manual Mondo/Power-Regulator/
cinematic/reward/achievement/Q5596-branch client-smoke pass.
The Q5575 validation-gate worksheet is
`artifacts\blocker_evidence\20260617-220052-quest-5575-seizing-power`;
it records objectives `8523;8371;12871`, rewards `2993;2994;2995`, achievement
`4133`, Kezrek Warbringer source-coordinate ids `15181;170390`, Power Regulator
source-coordinate ids `15177;15176;15178`, and the negative cases required for
the next manual Kezrek/Shield-Generator/Power-Regulator/reward/achievement/
Q5596-branch client-smoke pass. Objective evidence rows `2703;2704` and zone
rows `810;1315` remain review blockers until client-visible ordering, routing,
and placement behavior are captured.
The latest Q5580 validation-gate worksheet is
`artifacts\blocker_evidence\20260618-011658-quest-5580-enforced-radio-silence-recheck`;
it records objective `8227`, rewards `3005;3007;3008;3009;3010;3011`,
achievement `4135`, Tower Controls source-coordinate ids `15122;15123`,
activeProp ids `1137353;1137404`, Kezrek Warbringer source-coordinate ids
`15181;170390`, Jabbithole objective row `1452`, zone rows `309;620`, episode
quest `2389`, and the negative cases required for the next manual
Tower-Controls/Kezrek/reward/achievement/Q5594-merge client-smoke pass.
Objective row `1452`, zone rows `309;620`, and episode row `2389` remain review
blockers until client-visible ordering, routing, episode presentation, and
placement behavior are captured. The 2026-06-18 recheck created the worksheet
with `-CreateBundleOnly`, verified focused Q5580 tests `2/2`, blocker harness
presets `2/2`, and generated content-retail validation `31` files / `168,101`
rows all `not_retail_complete`; no live client screenshots or server/client log
excerpts were captured.
The latest Q3797 validation-gate worksheet is
`artifacts\blocker_evidence\20260619-081509-quest-3797-securing-the-area-recheck`;
it records objective `4918`, rewards `2480;2482;4770`, TargetGroup `1177`,
target members `11945;11948;11962;11963;12212;12213`, reviewed rootbrute/yeti
source-coordinate ids
`8272460;8272461;8272462;8272464;3837530;3785147;8122314;8122316`, Durek
Creature2 `11066`, receiver WorldLocation2 `7727`, compatibility Durek `11061`,
Jabbithole objective row `732`, partial zone rows `96;166`, and the negative
cases required for the next manual Durek/rootbrute-yeti/reward/Q3486-prereq
client-smoke pass. Zone rows `96;166`, type-9/non-spawned target branches, and
combat placement behavior remain review blockers until current bridge evidence
and client-visible gameplay behavior are captured. The 2026-06-19 recheck also
confirmed that the reviewed Snowstalker `244->11945` source-coordinate rows are
the western Northern Wilds/shared-yeti cluster rather than Q3797-specific
Settler's Reach placements, so the Snowstalker relation stays credit-mapped but
retail-blocked pending Q3797-specific source-coordinate proof or accepted
client/live evidence. The recheck created the worksheet with
`-CreateBundleOnly`, verified focused Q3797 tests `22/22`, and left generated
content-retail rows `not_retail_complete`; no live client screenshots or
server/client log excerpts were captured.
The 2026-06-17 combined P0 quest recheck passed `185/185` with
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v
minimal --nologo --filter
"FullyQualifiedName~Q5597|FullyQualifiedName~Q3479|FullyQualifiedName~Q3480|FullyQualifiedName~Q3486|FullyQualifiedName~Q3668|FullyQualifiedName~Q3673|FullyQualifiedName~Q3886|FullyQualifiedName~Q3963|FullyQualifiedName~Q5573|FullyQualifiedName~Q5575|FullyQualifiedName~Q5580|FullyQualifiedName~Q3797"`;
the worksheets remain validation targets, not retail-completion proof.
`-Lws036ChecklistSmoke` adds `lws-036-targets.md`, preloads the
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
The latest Ultimate Protogames split-row validation worksheet is
`artifacts\blocker_evidence\20260618-012815-instance-2980-ultimate-protogames-recheck`;
it supersedes the earlier skeleton
`artifacts\blocker_evidence\20260617-220641-instance-2980-ultimate-protogames-split-validation`
for the current validation-gate recheck. The worksheet records world `2980`,
public event `594`, the automated room-producer objective set
`3250;2680;4669;3206;3210;2676;2868;2869;2872;4692;2926;2941;2920;4561;2862;4442;2675;2847;2864;2678;2863;4648;4657`,
and the negative cases required for the next manual start-button/room-route/
challenge/reward dungeon-smoke pass. The instance remains split-required and
blocked on room randomisation, challenge/timer/failure/scoreboard semantics,
teleporter/hidden-holder/rookie rows, cleanup/replay behavior, rewards,
achievements, and full dungeon client smoke. The 2026-06-18 focused recheck
passed `620/620` with
`dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v
minimal --nologo --filter
"FullyQualifiedName~NexusForever.Game.Tests.Instances.UltimateProtogamesEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.PublicEventObjectiveCreditEntityScriptTests"`,
blocker harness presets passed `2/2`, and generated content-retail validation
still reports `31` files / `168,101` rows, all `not_retail_complete`.
The 2026-06-18 Gate Console bridge-review worksheet is
`artifacts\blocker_evidence\20260618-031106-instance-2980-gate-console-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2244`: Jabbithole creature `28533` (`Gate Console`) -> Creature2 `62427`.
The bridge matches the existing Prototentiary room placement while the runtime
script still credits build 16042 TargetGroup `10569` / objective `2847` and
accepts both `62987` and `62427`. Focused Ultimate Protogames/public-event
objective-credit tests passed `620/620`, the mapper-loader check applied the
override as `reviewed`, generated `creature_public_event_map.csv` reports
relation `2244` as `match_status=reviewed`, and the content-retail blocker row
now reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`.
The full DataMapping generator and content-retail audit later completed against
the refreshed outputs, but this closes the ambiguous bridge review only; live Gate
Console objective UI, door choreography, visual/replay cleanup, reward,
achievement, and full dungeon smoke remain the named external evidence source.
The 2026-06-19 Warden bridge-review worksheet is
`artifacts\blocker_evidence\20260619-083228-instance-2980-warden-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2219`: Jabbithole creature `28413` (`Warden`) -> Creature2 `62324`. This was
not promoted from scored-name matching alone: build 16042 objective `2678` uses
TargetGroup `12474`, the existing Ultimate Protogames runtime script spawns
Creature2 `62324` in world `2980` / area `4333` at `-20718,-945,-6923`,
`WardenEntityScript` credits objective `2678`, and regenerated PE299
source-coordinate rows match that placement/stat bundle. Full DataMapping and
content-retail audit refreshes completed; generated blocker detail now reports
relation `2219` as `match_status=reviewed`, `original_match_status=scored_name`,
and `reviewed_public_event_creature_blocked_spawn_credit_smoke`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the Warden bridge review; live
Prototentiary room/phase order, Warden visibility/combat timing,
stealth/no-alarm and timed-bonus semantics, door/cage choreography, reward,
medal, achievement, replay cleanup, and full dungeon smoke remain the named
external evidence source.
The 2026-06-19 Mondo's Monstrosity bridge-review worksheet is
`artifacts\blocker_evidence\20260619-085627-instance-2980-mondos-monstrosity-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2415`: Jabbithole creature `28306` (`Mondo's Monstrosity`) -> Creature2
`62575`. This was not promoted from scored-name matching alone: build 16042
objective `2926` uses TargetGroup `10657`, whose sole member is Creature2
`62575`; paired objective `2941` shares WorldLocation2 `41745`; the existing
Ultimate Protogames runtime script spawns Creature2 `62575` in world `2980` /
area `4351` around `-16501,-910,-10991`; `MondosMonstrosityEntityScript`
credits objectives `2926` and `2941`; and regenerated PE299 source-coordinate
rows match that placement/stat/spell bundle. Full DataMapping and
content-retail audit refreshes completed; generated public-event evidence now
reports relation `2415` as `match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`, while
`creature_map.csv` records `original_match_status=scored_name`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the Mondo's Monstrosity bridge
review; live Mondo route timing, timed qualification, phase order, spawn
visibility/combat timing, Mondo's Crate timing/selection, reward, medal,
achievement, replay cleanup, and full dungeon smoke remain the named external
evidence source.
The 2026-06-19 Mondo's Crate bridge-review worksheet is
`artifacts\blocker_evidence\20260619-091914-instance-2980-mondos-crate-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2184`: Jabbithole creature `28277` (`Mondo's Crate`) -> Creature2 `62549`.
This was not promoted from unique-name matching alone: build 16042 objective
`2920` is the Mondo's Crate script objective at WorldLocation2 `41745`,
TargetGroup `10666` has sole Creature2 `62549`, the existing Ultimate
Protogames runtime script spawns Creature2 `62549` in world `2980` / area
`4351` at `-16500,-910,-10998`, and `MondosCrateEntityScript` credits
objective `2920`. Regenerated DataMapping carries PE299 coordinate `6132219`
for source creature `28277` at that crate placement/stat bundle. Full
DataMapping and content-retail audit refreshes completed; generated
public-event evidence now reports relation `2184` as `match_status=reviewed`
and `reviewed_public_event_creature_blocked_spawn_credit_smoke`, while
`creature_map.csv` records `original_match_status=unique_name`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the Mondo's Crate bridge review;
live script trigger timing, `60000` ms timer start/fail semantics, random room
routing, crate row selection/despawn state, reward, medal, achievement, replay
cleanup, and full dungeon smoke remain the named external evidence source.
The 2026-06-19 Gilded Fowl bridge-review worksheet is
`artifacts\blocker_evidence\20260619-093630-instance-2980-gilded-fowl-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2185`: Jabbithole creature `28278` (`Gilded Fowl`) -> Creature2 `63055`.
This was not promoted from unique-name matching alone: build 16042 objectives
`2862` and `4442` target TargetGroup `12263`, whose sole member is Creature2
`63055`; the existing Ultimate Protogames runtime script spawns Creature2
`63055` in world `2980` / area `4347` at `-21242,-807,-10806`; and
`GildedFowlEntityScript` credits objectives `2862` and `4442`. Regenerated
DataMapping carries PE299 coordinate `8011506` for source creature `28278` at
that placement/stat bundle. Full DataMapping and content-retail audit refreshes
completed; generated public-event evidence now reports relation `2185` as
`match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`, while
`creature_map.csv` records `original_match_status=unique_name`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the Gilded Fowl bridge review; live
Power Plunge qualification/scoring, random room route timing, reward, medal,
achievement, replay cleanup, and full dungeon smoke remain the named external
evidence source.
The 2026-06-19 Misplaced Mammoth bridge-review worksheet is
`artifacts\blocker_evidence\20260619-095412-instance-2980-misplaced-mammoth-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2188`: Jabbithole creature `28290` (`Misplaced Mammoth`) -> Creature2
`63312`. This was not promoted from unique-name matching alone: build 16042
objective `4692` targets TargetGroup `12577`, whose sole member is Creature2
`63312`, and uses WorldLocation2 `41745`; the existing Ultimate Protogames
runtime script spawns Creature2 `63312` in world `2980` / area `4351` at
`-16482,-910,-10996`; and `MisplacedMammothEntityScript` credits objective
`4692`. Regenerated DataMapping carries PE299 coordinate `8469464` for source
creature `28290` at that placement/stat bundle. Full DataMapping and
content-retail audit refreshes completed; generated public-event evidence now
reports relation `2188` as `match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`, while
`creature_map.csv` records `original_match_status=unique_name`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the Misplaced Mammoth bridge
review; live timed-room routing, `30000` ms objective failure timing,
kill-credit UI timing, reward, medal, achievement, replay cleanup, and full
dungeon smoke remain the named external evidence source.
The 2026-06-19 Ruffles bridge-review worksheet is
`artifacts\blocker_evidence\20260619-100931-instance-2980-ruffles-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2217`: Jabbithole creature `28408` (`Ruffles`) -> Creature2 `65794`. This was
not promoted from unique-name matching alone: build 16042 objective `4561`
(`Hunt Ruffles`) targets TargetGroup `12390`, whose sole member is Creature2
`65794`; the existing Ultimate Protogames runtime script spawns Creature2
`65794` in world `2980` / area `4348` at `-16866,-802,5570`; and
`RufflesEntityScript` credits objective `4561`. Regenerated DataMapping carries
PE299 coordinate `7930052` for source creature `28408` at that placement/stat
bundle. Full DataMapping and content-retail audit refreshes completed;
generated public-event evidence now reports relation `2217` as
`match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`, while
`creature_map.csv` records `original_match_status=unique_name`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the Ruffles bridge review; live
hunt route/pathing, objective UI timing, reward, medal, achievement, replay
cleanup, and full dungeon smoke remain the named external evidence source.
The 2026-06-19 Malfunctioning Yellow Tank bridge-review worksheet is
`artifacts\blocker_evidence\20260619-103428-instance-2980-malfunctioning-yellow-tank-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2232`: Jabbithole creature `28491` (`Malfunctioning Yellow Tank`) -> Creature2
`62546`. This was not promoted from unique-name matching alone: build 16042
objectives `2676`, `2868`, `2869`, and `2872` use TargetGroup `12671`, whose
members are Creature2 `62542`, `62543`, and `62546`; the existing Ultimate
Protogames runtime script spawns Creature2 `62546` in world `2980` / area
`4336` at `-29077,-938,1552`; and `MalfunctioningTankEntityScript` filters the
three tank Creature2 rows and credits the tank objectives. Regenerated
DataMapping carries PE299 coordinate `6144867` for source creature `28491` at
that placement/stat bundle. Full DataMapping and content-retail audit refreshes
completed; generated public-event evidence now reports relation `2232` as
`match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. The separate
duplicate source relation `2628` was reviewed later; this worksheet closes only
relation `2232`.
Focused Ultimate Protogames/public-event objective-credit tests passed
`620/620`, and generated content-retail validation still reports `31` files /
`168,101` rows, all `not_retail_complete`. This closes only the Yellow Tank
relation `2232` bridge review; live tank-room route selection, timer and
`50%` / Incinerate bonus semantics, objective UI timing, reward, medal,
achievement, replay cleanup, and full dungeon smoke remain the named external
evidence source.
The 2026-06-19 Wrecked Blue Tank bridge-review worksheet is
`artifacts\blocker_evidence\20260619-105145-instance-2980-wrecked-blue-tank-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2234`: Jabbithole creature `28496` (`Wrecked Blue Tank`) -> Creature2 `62543`.
This was not promoted from unique-name matching alone: build 16042 objectives
`2676`, `2868`, `2869`, and `2872` use TargetGroup `12671`, whose members are
Creature2 `62542`, `62543`, and `62546`; the existing Ultimate Protogames
runtime script spawns Creature2 `62543` in world `2980` / area `4336` at
`-29015,-938,1544`; and `MalfunctioningTankEntityScript` filters the three
tank Creature2 rows and credits the tank objectives. Regenerated DataMapping
carries PE299 coordinate `6145453` for source creature `28496` at that
placement/stat bundle. Full DataMapping and content-retail audit refreshes
completed; generated public-event evidence now reports relation `2234` as
`match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. Remaining PE594
creature bridge candidates stay separate rows. Focused Ultimate
Protogames/public-event objective-credit tests passed `620/620`, and generated
content-retail validation still reports `31` files / `168,101` rows, all
`not_retail_complete`. This closes only the Wrecked Blue Tank relation `2234`
bridge review; live tank-room route selection, timer and `50%` / Incinerate
bonus semantics, objective UI timing, reward, medal, achievement, replay
cleanup, and full dungeon smoke remain the named external evidence source.
The 2026-06-19 Busted Red Tank bridge-review worksheet is
`artifacts\blocker_evidence\20260619-110945-instance-2980-busted-red-tank-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2243`: Jabbithole creature `28516` (`Busted Red Tank`) -> Creature2 `62542`.
This was not promoted from unique-name matching alone: build 16042 objectives
`2676`, `2868`, `2869`, and `2872` use TargetGroup `12671`, whose members are
Creature2 `62542`, `62543`, and `62546`; the existing Ultimate Protogames
runtime script spawns Creature2 `62542` in world `2980` / area `4336` at
`-29052,-938,1497`; and `MalfunctioningTankEntityScript` filters the three
tank Creature2 rows and credits the tank objectives. Regenerated DataMapping
carries PE299 coordinate `6150582` for source creature `28516` at that
placement/stat bundle. Full DataMapping and content-retail audit refreshes
completed; generated public-event evidence now reports relation `2243` as
`match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. Remaining PE594
creature bridge candidates stay separate rows. Focused Ultimate
Protogames/public-event objective-credit tests passed `620/620`, and generated
content-retail validation still reports `31` files / `168,101` rows, all
`not_retail_complete`. This closes only the Busted Red Tank relation `2243`
bridge review; live tank-room route selection, timer and `50%` / Incinerate
bonus semantics, objective UI timing, reward, medal, achievement, replay
cleanup, and full dungeon smoke remain the named external evidence source.
The 2026-06-19 Cage Console bridge-review worksheet is
`artifacts\blocker_evidence\20260619-112945-instance-2980-cage-console-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2245`: Jabbithole creature `28535` (`Cage Console`) -> Creature2 `63037`.
This was not promoted from unique-name matching alone: build 16042 objective
`2864` uses TargetGroup `10583`, whose sole member is Creature2 `63037`, count
`1`, and WorldLocation2 `41759`; aggregate/timed rows `2678`, `2863`, and
`2858` share the same target group; the existing Ultimate Protogames runtime
script spawns Creature2 `63037` in world `2980` / area `4333` at
`-20703,-945,-6926`; and `SneakyPrisonCageConsoleEntityScript` filters
Creature2 `63037` and credits TargetGroup `10583` once. Regenerated DataMapping
carries PE299 coordinate `6152147` for source creature `28535` at that runtime
placement. Full DataMapping and content-retail audit refreshes completed;
generated public-event evidence now reports relation `2245` as
`match_status=reviewed` and
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. Remaining PE594
creature bridge candidates stay separate rows.
Focused Ultimate Protogames/public-event objective-credit tests passed
`620/620`, and generated content-retail validation still reports `31` files /
`168,101` rows, all `not_retail_complete`. This closes only the Cage Console
relation `2245` bridge review; live cage release, door choreography,
stealth/no-alarm and Fast Hands timer semantics, objective UI timing, reward,
medal, achievement, replay cleanup, and full dungeon smoke remain the named
external evidence source.
The 2026-06-19 duplicate Yellow Tank bridge-review worksheet is
`artifacts\blocker_evidence\20260619-114800-instance-2980-duplicate-yellow-tank-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2628`: duplicate Jabbithole creature `32287` (`Malfunctioning Yellow Tank`)
-> Creature2 `62546`. This was not promoted from unique-name matching alone:
build 16042 objectives `2676`, `2868`, `2869`, and `2872` use TargetGroup
`12671`, whose members are Creature2 `62542`, `62543`, and `62546`; the
existing Ultimate Protogames runtime script spawns Creature2 `62546` in world
`2980` at `-29077,-938,1552`; and `MalfunctioningTankEntityScript` filters the
three tank Creature2 rows and credits the tank objectives. Regenerated
DataMapping carries PE299 coordinate `7806864` for source creature `32287` at
the same runtime Yellow Tank placement/stat bundle as reviewed relation `2232`.
Full DataMapping and content-retail audit refreshes completed; generated
public-event evidence now reports relation `2628` as `match_status=reviewed`
and `reviewed_public_event_creature_blocked_spawn_credit_smoke`. Focused
Ultimate Protogames/public-event objective-credit tests passed `620/620`, and
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. This closes only the duplicate Yellow Tank relation
`2628` bridge review; live tank-room route selection, timer and `50%` /
Incinerate bonus semantics, objective UI timing, duplicate-source double-count
prevention, reward, medal, achievement, replay cleanup, and full dungeon smoke
remain the named external evidence source.
The latest Stormtalon's Lair validation worksheet is
`artifacts\blocker_evidence\20260618-013255-instance-382-stormtalons-lair-recheck`;
it supersedes the earlier skeleton
`artifacts\blocker_evidence\20260617-220807-instance-382-stormtalons-lair-validation`
for the current validation-gate recheck. The worksheet records world `382`,
public event `145`, achievements `2414;3462`, public event objectives
`312;313;314;464;539;540;554;555;556;557;558;559;561;562;828;842;843;844;1692;2380;2381;2382;2487;3201;4867;5177;5343`,
and the negative cases required for the next manual Stormtalon combat,
optional-route, challenge, reward, and full-dungeon smoke pass. The instance
remains split-required and blocked on combat mechanics, cinematic payload,
optional route weights, wave cadence, boss spawn/version selection, timed bonus
and grenade-disable semantics, visual/despawn states, rewards, achievements,
and client smoke. The 2026-06-18 focused recheck passed Stormtalon event-script
and trigger tests `43/43` plus shared public-event objective-credit tests
`579/579`; blocker harness presets passed `2/2`, and generated content-retail
validation still reports `31` files / `168,101` rows, all
`not_retail_complete`.
The latest Vault of the Archon validation worksheet is
`artifacts\blocker_evidence\20260618-013646-instance-3009-vault-of-the-archon-recheck`;
it supersedes the earlier skeleton
`artifacts\blocker_evidence\20260617-221050-instance-3009-vault-of-the-archon-validation`
for the current public-event binding recheck. The worksheet records world
`3009`, public events
`666;668;669;677;678;693;696;874;875`, achievements
`4360;4361;4362;4363;4366;4586;4587;4588;4589;4590;4591;5587;5588;5589;5590;5591;5592;5593;5594;5595;5596;5597;5599;6841;6842;6843;6844;6845;6846;6847;6848;6893;6904`,
and the queued objective set
`4299;4300;4301;4302;4303;4304;4305;4306;4308;4310;4311;4314;4318;4320;4321;4322;4323;4324;4325;4326;4327;4328;4329;4332;4340;4384;4456;4457;4458;4459;4949;4950;4952;4953;4954;4955;4971;4972;4973;4980;4986;4987;4994;5001;5002;5054;5100;5133;5136;5149;5155;5220;5249;5260;5292;5293;5294;5295;4347;4348;4349;4350;4378;4379;4380;4381;4391;4392;4393;4394;4395;4398;4403;4404;4434;4441;4451;4452;4453;4462;4463;5153;5154;4333;5266`.
The 2026-06-18 slice bound the missing mapped public-event dependencies
`668`, `669`, `874`, and `875` through the existing
`EventBaseContentMapScript` additional-event path, without adding their missing
protect/fail-state, medal/reward, transport, escape, producer, or completion
semantics. Focused Hall of the Hundred event-script, optional-event, and
map-binding verification passed `269/269` from alternate output path
`artifacts\test-bin\vault` because local `NexusForever.WorldServer` PID `53392`
held the normal `NexusForever.Script.Instance.dll` output. Blocker harness
presets passed `2/2`, and generated content-retail validation still reports
`31` files / `168,101` rows, all `not_retail_complete`.
The next manual pass still needs party-count, portal/exit, placement,
side-event visual-state/spawn, combat/choreography, medal/reward, protect-NPC,
escape transport/mount, reward, achievement, and full World Story smoke
evidence before any retail-complete claim.
The latest Sanctuary of the Swordmaiden validation worksheet is
`artifacts\blocker_evidence\20260618-014841-instance-1271-sanctuary-of-the-swordmaiden-recheck`;
it records world `1271`, public events `166;202`, achievements
`2639;3464;5306;5307;5308;5309;5310;5311`, and the queued objective set
`479;480;481;482;483;486;492;493;494;495;496;497;499;500;501;502;503;504;505;613;614;615;627;628;629;630;639;641;655;661;662;1366;1468;1469;1470;1503;1504;1506;1700;2211;2212;2383;2384;2385;2386;2387;3204;5180;5346;489;677;678`.
The 2026-06-18 slice bound mapped public event `202` (`Spiritual Revival`)
through `SanctuaryOfTheSwordmaidenMapScript.AdditionalPublicEventIds`, without
adding ritual, escort, protect, talk, creature, reward, achievement, route, or
challenge semantics. Focused Sanctuary event/trigger/objective-credit plus
map-binding verification passed `657/657` from alternate output path
`artifacts\test-bin\sanctuary`; blocker harness presets passed `2/2`; generated
content-retail validation still reports `31` files / `168,101` rows, all
`not_retail_complete`. The next manual pass still needs PE202 objectives
`489`, `677`, and `678` producer proof, ambiguous PE202 creature bridge review,
exact boss/miniboss mechanics, route weights/objectives, Selene escort and
challenge completion/failure proof, residual ritual/teleporter/controller and
achievement producers, placement and visual/despawn state,
trigger/door/communicator timing, rewards, achievements, negative cases, and
full dungeon smoke evidence before any retail-complete claim.
The latest Fragment Zero validation worksheet is
`artifacts\blocker_evidence\20260618-015726-instance-3180-fragment-zero-recheck`;
it records world `3180`, public event `680`, achievements `6005;6020;6027`,
and the queued objective set
`4397;4416;4417;4418;4420;4421;4422;4423;4424;4425;4426;4427;4429;4430;4431;4432;4443;4444;4445;4446;4447;4448;4449;4450;4454;4455;4584;4585;4586;4632;4635;4641;4642;4655;4684;4686;4690;4693`.
Focused Fragment Zero event/cinematic-hook verification passed `64/64` from
alternate output path `artifacts\test-bin\fragment-zero`; blocker harness
presets passed `2/2`; generated content-retail validation still reports `31`
files / `168,101` rows, all `not_retail_complete`. No runtime behavior was
changed in the 2026-06-18 recheck: objective `4425` remains mapped-only because
build 16042 records it as objectless Exterminate (`ObjectId=0`, `Count=0`,
`WorldLocation2=49225`) and current source only activates the objective plus
ambush communicator. The Skeech horde death script remains rejected as a
`4425` producer because it credits only `4416` and tier objectives
`4584/4585/4586`. The next manual pass still needs Skeech ambush `4425`
wave-completion proof, objective delta/replay evidence, reward/medal side
effects, exact trigger lifecycle/cleanup, real cinematic payload and timing,
communicator/door choreography, Jo/Syrus/Hugo entity presentation and cleanup,
gold-medal timer/reward semantics, rewards, achievements, and full expedition
smoke evidence before any implementation or retail-complete claim.
The latest Skullcano validation worksheet is
`artifacts\blocker_evidence\20260618-020259-instance-1263-skullcano-recheck`;
it records world `1263`, public event `148`, achievements
`5293;5294;5295;5296;5297`, and the queued objective set
`321;322;323;324;328;329;333;334;335;336;337;338;339;340;362;364;365;372;376;440;820;838;839;840;841;941;942;943;944;1989;3203;5179;5345`.
Focused Skullcano event/trigger/objective-credit verification passed `631/631`
from alternate output path `artifacts\test-bin\skullcano`; blocker harness
presets passed `2/2`; generated content-retail validation still reports `31`
files / `168,101` rows, all `not_retail_complete`. No runtime behavior or data
changed in the 2026-06-18 recheck. Current automation remains scoped to the
test-backed mapped producers; residual objectives `328`, `336`, `337`, `339`,
`340`, `838-841`, `941-944`, `1989`, `3203`, `5179`, and `5345` stay blocked
pending reviewed bridge/runtime/live evidence. The shared object `2821` chasm
trigger was rejected as proof for escort `328`, and phase activation or boss
death alone was rejected for bonus/challenge/timer/rookie reward rows. The next
manual pass still needs Chief Kaskalak escort `328`, Redmoon jailor/release,
Primal Fire essence collection, hidden path selectors, remaining bonus/
challenge/timer rows, exact boss mechanics, cave/chasm route weights,
terraformer script-objective timing, trigger owner reconciliation, reviewed
creature bridge confirmation, door/platform/communicator timing, rewards,
achievements, negative cases, and full dungeon smoke evidence before any
implementation or retail-complete claim.
The latest Gauntlet validation worksheet is
`artifacts\blocker_evidence\20260618-020839-instance-2183-gauntlet-recheck`;
it records world `2183`, public event `446`, achievements
`3718;4186;6011;6026`, and the queued objective set
`1818;1819;1821;1822;1834;1835;1837;1842;1854;1857;1858;1859;1864;1865;1866;1869;1870;1871;1872;1873;1874;1875;1896;1914;1915;1942;1951;1957;1967;1974;1977;2001;4670;4671;4707`.
Focused Gauntlet verification passed `36/36` from alternate output path
`artifacts\test-bin\gauntlet`; blocker harness presets passed `2/2`; generated
content-retail validation still reports `31` files / `168,101` rows, all
`not_retail_complete`. No runtime behavior or data changed in the 2026-06-18
recheck. Current automation remains scoped to the test-backed mapped producers;
residual rows `1875`, `1896`, `1942`, `1957`, `1967`, `1974`, `4670`, and
`4707` stay blocked pending runtime-owned score/completion/timer proof.
Activation-only rows were rejected as completion or medal proof. The next
manual pass still needs score-token, Golden Skull, Splorg, main-event,
second/third-arena, current-score, gold-timer, door/product,
cinematic/announcer, arena-door, route-selection, encounter, reward,
achievement, negative-case, and full expedition smoke evidence before any
implementation or retail-complete claim.
The latest Evil from the Ether validation worksheet is
`artifacts\blocker_evidence\20260618-021307-instance-3404-evil-from-the-ether-recheck`;
it records world `3404`, public event `781`, achievements
`7048;7049;7050;7051`, and the queued objective set
`4895;4908;4919;4920;4921;4922;4923;4924;4925;4926;4927;4937;4938;4939;4940;4941;4942;4943;4944;4947;4948;4956;4957;4961;4962;4974;4975;4976;4977;4978;4979;5013`.
Focused Evil from the Ether event/trigger verification passed `77/77` from
alternate output path `artifacts\test-bin\evil-from-the-ether`; blocker harness
presets passed `2/2`; generated content-retail validation still reports `31`
files / `168,101` rows, all `not_retail_complete`. No runtime behavior or data
changed in the 2026-06-18 recheck. Current automation remains scoped to the
test-backed mapped producers; residual objective `4942` stays blocked pending
an exploding-portal hit/failure producer, objective UI delta, owner/timing,
replay cleanup, reward/medal side effects, portal placement/visual/despawn, and
full expedition smoke. Portal death credit was rejected as proof for `4942`
because `EthericPortalEntityScript` credits mapped portal-kill rows, not the
avoidance objective.
The latest Space Madness validation worksheet is
`artifacts\blocker_evidence\20260618-021942-instance-2149-space-madness-recheck`;
it records world `2149`, public event `390`, achievements
`3716;4180;6009;6024`, and the queued objective set
`1579;1588;1589;1590;1591;1592;1593;1594;1595;1596;1597;1602;1603;1641;1642;1643;1656;1662;2115;4705;4708;4709;4710;4711;4712;4736`.
Focused Space Madness event-script verification passed `45/45` from alternate
output path `artifacts\test-bin\space-madness`; blocker harness presets passed
`2/2`; generated content-retail validation still reports `31` files /
`168,101` rows, all `not_retail_complete`. No runtime behavior or data changed
in the 2026-06-18 recheck. Current automation remains scoped to the test-backed
mapped producers; residual objectives `1662`, `2115`, and `4708` stay blocked
pending hallucination wave-count, air-scrubbing timer/wave, and volatile
Rowsdower hit/failure producers plus objective UI deltas, owner/timing,
cleanup/replay, reward/medal side effects, placement and visual/despawn proof,
and full expedition smoke. Activation-only rows, livestock death credit, and
air-scrubber control activation were rejected as proof for those residual
objectives.
The 2026-06-18 Space Madness Skittish Rowsdower bridge-review worksheet is
`artifacts\blocker_evidence\20260618-145609-instance-2149-space-madness-skittish-rowsdower-bridge-review`.
It records tracked DataMapping reviews for public-event creature relations
`2350` and `2025`: Jabbithole creatures `3704` and `28120`
(`Skittish Rowsdower`) -> Creature2 `45723`. The bridge is backed by
candidate-count-1 mapping, level-1 world `2149` / area `2419` source
coordinate rows (`96` and `100` rows), and Plague Burst spell evidence for
source creature `28120`. Objective `4708` remains volatile Rowsdower avoidance
with objective type `9`, WorldLocation2 `37709`, objective object `0`, and
target group `0`. Full DataMapping generation and content-retail regeneration
completed; generated `creature_public_event_map.csv` reports relations `2350`
and `2025` as `match_status=reviewed`. This closes only the duplicate normal
bridge review. Live Creature2 `45723` objective ownership, spawn/import
placement, explosion/failure behavior, objective-credit behavior, replay
cleanup, reward, medal, achievement, and negative-case proof remain the named
external evidence source.

The 2026-06-18 Space Madness Enraged Lamp bridge-review worksheet is
`artifacts\blocker_evidence\20260618-143737-instance-2149-space-madness-enraged-lamp-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`150`: normal level-32 Jabbithole creature `1039` (`Enraged Lamp`) ->
Creature2 `45866`. The bridge is backed by PE390 relation `150`, 82 local
world `2149` / area `2416` source coordinate rows, and Jab/Punch/Flourishing
Combo/Haymaker source spell rows. TargetGroup `6343` lists Creature2 `45866`
with the hallucination-trio rows `45879` and `45849`, while tied level-32
candidate Creature2 `46724` belongs to TargetGroup `6449`; no current PE390
objective row in the generated objective map uses TargetGroup `6343` or
`6449`. Same-name level-50 source creature `28168` / relation `2036` remains
ambiguous-name to Creature2 `69185`. Full DataMapping generation and
content-retail regeneration completed; generated `creature_public_event_map.csv`
reports relation `150` as `match_status=reviewed` while relation `2036`
remains `ambiguous_name`. This closes only the normal bridge review. Live
Creature2 `45866` objective ownership, spawn/import placement,
objective-credit behavior, combat behavior, replay cleanup, reward, medal,
achievement, Prime/level-50 behavior, and negative-case proof remain the named
external evidence source.

The 2026-06-18 Space Madness Raging Oxianbull bridge-review worksheet is
`artifacts\blocker_evidence\20260618-142024-instance-2149-space-madness-raging-oxianbull-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`499`: normal level-32 Jabbithole creature `3679` (`Raging Oxianbull`) ->
Creature2 `45728`. The bridge is backed by PE390 relation `499`, 81 local
world `2149` / area `2419` source coordinate rows, and Bash/Ram/Bucking
Frenzy/Overwhelming Bellow source spell rows. TargetGroup `6363` lists
Creature2 `45728` with livestock-family rows, but no current PE390 objective
row in the generated objective map uses TargetGroup `6363`;
`HallucinatingLivestockEntityScript` remains scoped to Creature2 `46483` and
`46714`. Same-name level-50 source creature `28066` / relation `2011` remains
mapped-only to Creature2 `69172`. Full DataMapping generation and
content-retail regeneration completed; generated `creature_public_event_map.csv`
reports relation `499` as `match_status=reviewed` while relation `2011`
remains `scored_name`. This closes only the normal bridge review. Live
Creature2 `45728` objective ownership, spawn/import placement,
objective-credit behavior, combat behavior, replay cleanup, reward, medal,
achievement, Prime/level-50 behavior, and negative-case proof remain the named
external evidence source.

The 2026-06-18 Space Madness Violent Oxiancow bridge-review worksheet is
`artifacts\blocker_evidence\20260618-140414-instance-2149-space-madness-violent-oxiancow-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`422`: normal level-32 Jabbithole creature `3686` (`Violent Oxiancow`) ->
Creature2 `45729`. The bridge is backed by PE390 relation `422`, 91 local
world `2149` / area `2421` source coordinate rows, and Bash/Ram/Bucking
Frenzy/Overwhelming Bellow source spell rows. TargetGroups `6363` and `12672`
list Creature2 `45729`, and TargetGroup `12677` lists now-reviewed level-50
Creature2 `69139`, but no current PE390 objective row in the generated
objective map uses any of those target groups; `HallucinatingLivestockEntityScript`
remains scoped to Creature2 `46483` and `46714`. Same-name level-50 source
creature `28018` / relation `2003` is reviewed by the Prime bridge slice above
and still blocked on runtime/client smoke. Full
DataMapping generation and content-retail regeneration completed; generated
`creature_public_event_map.csv` reports relation `422` as
`match_status=reviewed`; relation `2003` is reviewed by the later Prime slice
above. This closes only the normal bridge review. Live Creature2 `45729`
objective
ownership, spawn/import placement, objective-credit behavior, combat behavior,
replay cleanup, reward, medal, achievement, Prime/level-50 behavior, and
negative-case proof remain the named external evidence source.

The 2026-06-18 Space Madness Rampaging Oxian bridge-review worksheet is
`artifacts\blocker_evidence\20260618-134038-instance-2149-space-madness-rampaging-oxian-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`161`: normal level-32 Jabbithole creature `3708` (`Rampaging Oxian`) ->
Creature2 `45730`. The bridge is backed by PE390 relation `161`, 82 local
world `2149` / area `2419` source coordinate rows, and Skull Smash/Resounding
Charge/Ram/Bash/Bucking Frenzy/Overwhelming Bellow source spell rows.
TargetGroup `6363` lists Creature2 `45730`, but no current PE390 objective row
in the generated objective map uses TargetGroup `6363`, and
`HallucinatingLivestockEntityScript` remains scoped to Creature2 `46483` and
`46714`. Same-name level-50 source creature `28126` / relation `2027` remains
mapped-only to Creature2 `69140`. Full DataMapping generation and
content-retail regeneration completed; generated `creature_public_event_map.csv`
reports relation `161` as `match_status=reviewed` while relation `2027`
remains `scored_name`. This closes only the normal bridge review. Live
Creature2 `45730` objective ownership, spawn/import placement,
objective-credit behavior, combat behavior, replay cleanup, reward, medal,
achievement, Prime/level-50 behavior, and negative-case proof remain the named
external evidence source.

The 2026-06-18 Space Madness Smoke Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-130819-instance-2149-space-madness-smoke-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`160`: normal level-32 Jabbithole creature `3696` (`Smoke Nightmare`) ->
Creature2 `46722`. The bridge is backed by PE390 relation `160`, 67 local
world `2149` / area `2416` source coordinate rows, and Jab/Punch source spell
rows. TargetGroup `6449` lists Creature2 `46722`, but no current PE390
objective row in the generated objective map uses TargetGroup `6449`, and
objective `1590` script evidence remains scoped to TargetGroups `6400`/`6401`.
Same-name level-50 source creature `28115` / relation `2022` remains
mapped-only to Creature2 `69208`. Full DataMapping generation and
content-retail regeneration completed after normalizing the Eye Nightmare,
Foaming Roandoe, and Smoke Nightmare override CSV rows back to eight fields;
generated `creature_public_event_map.csv` reports relation `160` as
`match_status=reviewed` while relation `2022` remains `scored_name`. This
closes only the normal bridge review. Live Creature2 `46722` objective
ownership, spawn/import placement, objective-credit behavior, combat behavior,
replay cleanup, reward, medal, achievement, Prime/level-50 behavior, and
negative-case proof remain the named external evidence source.

The 2026-06-18 Space Madness Foaming Roandoe bridge-review worksheet is
`artifacts\blocker_evidence\20260618-124849-instance-2149-space-madness-foaming-roandoe-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`159`: normal level-32 Jabbithole creature `3684` (`Foaming Roandoe`) ->
Creature2 `45727`. The bridge is backed by PE390 relation `159`, 62 local
world `2149` / area `2419` source coordinate rows, and Ram/Headbutt/Bucking
Frenzy source spell rows. TargetGroup `6363` lists Creature2 `45727` with
livestock-family rows, but no current PE390 objective row in the generated
objective map uses TargetGroup `6363`, and `HallucinatingLivestockEntityScript`
remains scoped to Creature2 `46483` and `46714`. Same-name level-50 source
creature `28056` / relation `2010` is now tracked by the Prime bridge review
above. Full DataMapping generation and content-retail regeneration completed;
generated `creature_public_event_map.csv` reports relation `159` as
`match_status=reviewed` while the later Prime bridge slice above reports
relation `2010` as `match_status=reviewed`. This
closes only the normal bridge review. Live Creature2 `45727` objective
ownership, spawn/import placement, objective-credit behavior, combat behavior,
replay cleanup, reward, medal, achievement, Prime/level-50 behavior, and
negative-case proof remain the named external evidence source.

The 2026-06-18 Space Madness Masked Arachnid bridge-review worksheet is
`artifacts\blocker_evidence\20260618-123039-instance-2149-space-madness-masked-arachnid-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`156`: normal level-32 Jabbithole creature `3667` (`Masked Arachnid`) ->
Creature2 `46720`. The bridge is backed by PE390 relation `156`, 92 local
world `2149` / area `2416` source coordinate rows, and Pinch/Slice source
spell rows. TargetGroup `6448` lists Creature2 `46720`, but no current PE390
objective row in the generated objective map uses TargetGroup `6448`.
Same-name level-50 source creature `28170` / relation `2037` remains
mapped-only to Creature2 `69206`. Full DataMapping generation and
content-retail regeneration completed; generated
`creature_public_event_map.csv` reports relation `156` as
`match_status=reviewed` while relation `2037` remains `scored_name`. This
closes only the normal bridge review. Live Creature2 `46720` objective
ownership, spawn/import placement, objective-credit behavior, combat behavior,
replay cleanup, reward, medal, achievement, Prime/level-50 behavior, and
negative-case proof remain the named external evidence source.

The 2026-06-18 Space Madness Eye Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-121241-instance-2149-space-madness-eye-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`154`: normal level-32 Jabbithole creature `3663` (`Eye Nightmare`) ->
Creature2 `46723`. The bridge is backed by PE390 relation `154`, 62 local
world `2149` / area `2416` source coordinate rows, and Punch/Jab/Flourishing
Combo/Haymaker source spell rows. TargetGroup `6449` lists Creature2 `46723`,
but no current PE390 objective row in the generated objective map uses
TargetGroup `6449`, and objective `1590` script evidence remains scoped to
TargetGroups `6400`/`6401`. Same-name level-50 source creature `28023` /
relation `2005` is reviewed by the Prime bridge slice above and still blocked
on runtime/client smoke. Full DataMapping
generation and content-retail regeneration completed; generated
`creature_public_event_map.csv` reports relation `154` as
`match_status=reviewed`; relation `2005` is reviewed by the later Prime slice
above. This closes only the normal bridge review. Live Creature2 `46723`
objective
ownership, spawn/import placement, objective-credit behavior, combat behavior,
replay cleanup, reward, medal, achievement, Prime/level-50 behavior, and
negative-case proof remain the named external evidence source.

The 2026-06-18 Space Madness Hallucinating Cubig bridge-review worksheet is
`artifacts\blocker_evidence\20260618-115352-instance-2149-space-madness-hallucinating-cubig-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`152`: normal level-32 Jabbithole creature `3654` (`Hallucinating Cubig`) ->
Creature2 `45724`. The bridge is backed by PE390 relation `152`, 66 local
world `2149` / area `2421` source coordinate rows, and Chomp/Bite/Bruising
Rush/Pulverizing Vault source spell rows. Same-name level-50 source creature
`28092` / relation `2016` is reviewed by the later Prime bridge slice to
Creature2 `69137`. Full
DataMapping generation and content-retail regeneration completed; generated
`creature_public_event_map.csv` reports relation `152` as
`match_status=reviewed`; relation `2016` is reviewed by the later Prime slice.
This
closes only the normal bridge review. Live Creature2 `45724` spawn/import
placement, livestock objective-credit ownership, combat behavior, replay
cleanup, reward, medal, achievement, Prime Cubig runtime behavior, and
negative-case proof remain the named external evidence source.

The 2026-06-18 Space Madness Maddened Roanstag bridge-review worksheet is
`artifacts\blocker_evidence\20260618-114717-instance-2149-space-madness-maddened-roanstag-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`149`: normal level-32 Jabbithole creature `3643` (`Maddened Roanstag`) ->
Creature2 `45726`. The bridge is backed by PE390 relation `149`, 63 local
world `2149` / area `2421` source coordinate rows, and Headbutt/Ram/Bucking
Frenzy source spell rows. Same-name level-50 source creature `28134` /
relation `2119` remains mapped-only to Creature2 `69138`. Full DataMapping
generation and content-retail regeneration completed; generated
`creature_public_event_map.csv` reports relation `149` as
`match_status=reviewed` while relation `2119` remains `scored_name`. This
closes only the normal bridge review. Live Creature2 `45726` spawn/import
placement, livestock objective-credit ownership, combat behavior, replay
cleanup, reward, medal, achievement, and negative-case proof remain the named
external evidence source.

The 2026-06-18 Space Madness Exact Change 3.1 bridge-review worksheet is
`artifacts\blocker_evidence\20260618-112920-instance-2149-space-madness-exact-change-31-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2023`: Jabbithole creature `28116` (`Exact Change 3.1`) -> Creature2 `69204`.
The bridge is backed by PE390 relation `2023`, Q9883 `Deconstruction`
objective relation `8027`, contract relation `1112`, 59 local world `2149` /
area `2416` source coordinate rows, and Exact Change 3.x spell-family rows.
Full DataMapping generation and content-retail regeneration completed;
generated `creature_public_event_map.csv`, `creature_quest_map.csv`, and
`contract_creature_map.csv` report the related rows as reviewed. This closes
only the bridge review. Live Creature2 `69204` spawn/import placement, Space
Madness objective-credit ownership, Q9883/contract spawn availability and tap
behavior, Prime/contract client smoke, replay cleanup, reward, medal,
achievement, and negative-case proof remain the named external evidence
source.

The 2026-06-18 Space Madness Exact Change 3.0 bridge-review worksheet is
`artifacts\blocker_evidence\20260618-111017-instance-2149-space-madness-exact-change-30-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`151`: Jabbithole creature `3645` (`Exact Change 3.0`) -> Creature2 `46483`.
The bridge is backed by PE390 objective `1593`, direct
`KillEventObjectiveUnit` count `10`, WorldLocation2 `37709`, 94 local world
`2149` / area `2416` source coordinate rows, reviewed WIP instance entity
`1100300009`, and existing `HallucinatingLivestockEntityScript`
objective-credit tests for `46483`/`46714`. Focused Space Madness/public-event
objective-credit tests passed `624/624`, full DataMapping generation and
content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `151` as
`match_status=reviewed`, and the content-retail creature blocker row now
reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`. This
closes only the unique-name bridge review. Live phase-four spawn/import
placement, livestock combat behavior, objective `1593` UI timing and credit,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Exact Change 3.1 relation `2023` remains
untouched.

The 2026-06-18 Space Madness Party Down-Grazer bridge-review worksheet is
`artifacts\blocker_evidence\20260618-104608-instance-2149-space-madness-party-downgrazer-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2017`: Jabbithole creature `28093` (`Party Down-Grazer`) -> Creature2
`69901`. The bridge is backed by build 16042 client objective `4711`, object
TargetGroup `12597`, sole TargetGroup member `69901`, parent objective `4712`,
71 local world `2149` / area `2414` source coordinate rows, and existing
`PartyDowngrazerEntityScript` TargetGroup plus aggregate-credit tests. Focused
Space Madness/public-event objective-credit tests passed `624/624`, full
DataMapping generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `2017` as
`match_status=reviewed`, and the content-retail creature blocker row now
reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`. This
closes only the unique-name bridge review. Live escaped-experiment
spawn/import placement, objective `4711` collection timing, parent objective
`4712` aggregate UI behavior, replay cleanup, reward, medal, achievement, and
full expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Talking Rockmite bridge-review worksheet is
`artifacts\blocker_evidence\20260618-102826-instance-2149-space-madness-talking-rockmite-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2020`: Jabbithole creature `28112` (`Talking Rockmite`) -> Creature2 `69900`.
The bridge is backed by build 16042 client objective `4710`, object TargetGroup
`12596`, sole TargetGroup member `69900`, parent objective `4712`, 85 local
world `2149` / area `2414` source coordinate rows, and existing
`ExperimentalRockmiteEntityScript` TargetGroup plus aggregate-credit tests.
Focused Space Madness/public-event objective-credit tests passed `624/624`,
full DataMapping generation and content-retail regeneration completed,
generated `creature_public_event_map.csv` reports relation `2020` as
`match_status=reviewed`, and the content-retail creature blocker row now
reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`. This
closes only the unique-name bridge review. Live escaped-experiment
spawn/import placement, objective `4710` collection timing, parent objective
`4712` aggregate UI behavior, replay cleanup, reward, medal, achievement, and
full expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Air Scrubber Controls bridge-review worksheet is
`artifacts\blocker_evidence\20260618-100902-instance-2149-space-madness-air-scrubber-controls-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`147`: Jabbithole creature `3638` (`Air Scrubber Controls`) -> Creature2
`46437`. The bridge is backed by PE390 objective `1595`,
object/reward-pane TargetGroup `6371`, TargetGroup member `46437`, a single
local world `2149` / area `2421` source coordinate row, and existing
`AirScrubberControlsEntityScript` TargetGroup credit tests. TargetGroup `6371`
also contains Creature2 `45861` (`Main Vent Lever`), but that bridge/placement
path remains a separate blocker. Focused Space Madness/public-event
objective-credit tests passed `624/624`, full DataMapping generation and
content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `147` as
`match_status=reviewed`, and the content-retail creature blocker row now
reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`. This
closes only the unique-name bridge review. Live objective `1595` UI
timing/repeat behavior, objective `2115` air-scrubbing timer/wave behavior,
Main Vent Lever evidence, replay cleanup, reward, medal, achievement, and full
expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Engineering Computer bridge-review worksheet is
`artifacts\blocker_evidence\20260618-094918-instance-2149-space-madness-engineering-computer-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`146`: Jabbithole creature `3635` (`Engineering Computer`) -> Creature2
`45973`. The bridge is backed by PE390 objective `1603`,
object/reward-pane TargetGroup `6372`, sole TargetGroup member `45973`, a
single local world `2149` / area `2421` source coordinate row,
`SpaceMadnessEventScript` all-clear phase spawn coverage, and the existing
`EngineeringComputerEntityScript` TargetGroup credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `146` as
`match_status=reviewed`, and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the unique-name bridge review. Live objective `1603` UI timing and credit,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source.

The 2026-06-18 Space Madness Hazmat Suit bridge-review worksheet is
`artifacts\blocker_evidence\20260618-093151-instance-2149-space-madness-hazmat-suit-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`145`: Jabbithole creature `3634` (`Hazmat Suit`) -> Creature2 `45981`. The
bridge is backed by PE390 objective `1656`, reward-pane TargetGroup `6362`,
sole TargetGroup member `45981`, local world `2149` / area `2417` source
coordinate candidates, `SpaceMadnessEventScript` phase spawn coverage, and the
existing `HazmatSuitEntityScript` filter/direct-credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `145` as
`match_status=reviewed`, and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Hazmat Suit spawn/import placement,
objective `1656` UI timing and credit, replay cleanup, reward, medal,
achievement, and full expedition smoke remain the named external evidence
source.

The 2026-06-18 Space Madness Metallic Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-091305-instance-2149-space-madness-metallic-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`158`: Jabbithole creature `3674` (`Metallic Nightmare`) -> Creature2 `58798`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup
`6402`, nested TargetGroup `6401`, TargetGroup member `58798`, local world
`2149` / area `2416` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `158` as
`match_status=reviewed`, same-name relation `2312` remains `scored_name`, and
the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the ambiguous-name bridge review. Live Metallic Nightmare spawn/import
placement, panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2312`
(`28077` -> `69222`) and objective `1662` hallucination-wave mechanics remain
untouched.

The 2026-06-18 Space Madness Hookfoot Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-085151-instance-2149-space-madness-hookfoot-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`157`: Jabbithole creature `3670` (`Hookfoot Nightmare`) -> Creature2 `46721`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup
`6402`, nested TargetGroup `6401`, TargetGroup member `46721`, local world
`2149` / area `2416` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `157` as
`match_status=reviewed`, same-name relation `2035` remains `scored_name`, and
the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Hookfoot Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2035`
(`28166` -> `69207`) and objective `1662` hallucination-wave mechanics remain
untouched.

The 2026-06-18 Space Madness Gnarled Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-083248-instance-2149-space-madness-gnarled-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`144`: Jabbithole creature `3628` (`Gnarled Nightmare`) -> Creature2 `58783`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup
`6402`, nested TargetGroup `6401`, TargetGroup member `58783`, local world
`2149` / area `2414` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `144` as
`match_status=reviewed`, same-name relation `2026` remains `scored_name`, and
the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Gnarled Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2026`
(`28121` -> `69223`) and objective `1662` hallucination-wave mechanics remain
untouched.

The 2026-06-18 Space Madness Looming Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-081422-instance-2149-space-madness-looming-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`155`: Jabbithole creature `884` (`Looming Nightmare`) -> Creature2 `46135`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup
`6402`, nested TargetGroup `6401`, TargetGroup member `46135`, local world
`2149` / area `2416` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `155` as
`match_status=reviewed`, same-name relation `2021` remains `scored_name`, and
the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Looming Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2021`
(`28114` -> `69221`) and objective `1662` hallucination-wave mechanics remain
untouched.

The 2026-06-18 Space Madness Horrifying Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-075422-instance-2149-space-madness-horrifying-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`31`: Jabbithole creature `1189` (`Horrifying Nightmare`) -> Creature2
`46132`. The bridge is backed by PE390 objective `1590`, reward-pane
TargetGroup `6402`, nested TargetGroup `6401`, TargetGroup member `46132`,
local world `2149` / area `2416` spawn candidates, creature spell rows, and
the existing `SavePanickedWorkersNightmareEntityScript` filter/credit tests.
Focused Space Madness/public-event objective-credit tests passed `624/624`,
full DataMapping generation and content-retail regeneration completed,
generated `creature_public_event_map.csv` reports relation `31` as
`match_status=reviewed`, same-name relation `2034` remains `scored_name`, and
the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the ambiguous-name bridge review. Live Horrifying Nightmare spawn/import
placement, panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2034`
(`28148` -> `69219`) and objective `1662` hallucination-wave mechanics remain
untouched.

The 2026-06-18 Space Madness Shocking Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-073231-instance-2149-space-madness-shocking-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`30`: Jabbithole creature `1128` (`Shocking Nightmare`) -> Creature2 `46126`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6400`, TargetGroup member `46126`, local world `2149` /
area `2414` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `30` as
`match_status=reviewed`, same-name relation `2038` remains `scored_name`, and
the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Shocking Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2038`
(`28172` -> `69215`) and objective `1662` hallucination-wave mechanics remain
untouched.

The 2026-06-18 Space Madness Rending Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-071049-instance-2149-space-madness-rending-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`29`: Jabbithole creature `1121` (`Rending Nightmare`) -> Creature2 `46130`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6400`, TargetGroup member `46130`, local world `2149` /
area `2414` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `29` as
`match_status=reviewed`, the later Prime bridge slice above reports same-name
relation `2006` as `match_status=reviewed`, and the content-retail blocker row
now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Rending Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source. Same-name relation `2006`
(`28035` -> `69218`) is now tracked by the Prime bridge review above; objective
`1662` hallucination-wave mechanics remain untouched.

The 2026-06-18 Space Madness Captain Tero bridge-review worksheet is
`artifacts\blocker_evidence\20260618-065133-instance-2149-space-madness-captain-tero-bridge-review`.
It records the tracked DataMapping review for public-event creature relations
`28` and `2117`: Jabbithole creature `1097` (`Captain Tero`) -> Creature2
`45900`, and duplicate source creature `28243` (`Captain Tero`) -> Creature2
`45900`. The bridge cluster is backed by PE390 objective `1579`, TargetGroup
`6342`, TargetGroup sole member `45900`, reviewed runtime entity `1100300005`,
and the existing `CaptainTeroEntityScript` filter/TargetGroup talk-credit
tests. Focused Space Madness/public-event objective-credit tests passed
`624/624`, full DataMapping generation and content-retail regeneration
completed, generated `creature_public_event_map.csv` reports relations `28`
and `2117` as `match_status=reviewed`, and both content-retail blocker rows
now report `reviewed_public_event_creature_blocked_spawn_credit_smoke`. This
closes only the scored-name bridge review for the duplicate Captain Tero rows.
Live shuttle placement, dialog timing, phase visibility, activation UI timing
and credit, replay cleanup, reward, medal, achievement, and full expedition
smoke remain the named external evidence source.

The 2026-06-18 Space Madness Observation Deck Computer bridge-review worksheet is
`artifacts\blocker_evidence\20260618-063502-instance-2149-space-madness-observation-deck-computer-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`27`: Jabbithole creature `979` (`Observation Deck Computer`) -> Creature2
`45972`. The bridge is backed by PE390 objective `1589`, TargetGroup `6356`,
TargetGroup sole member `45972`, reviewed runtime entity `1100300007`, and the
existing `ObservationDeckComputerEntityScript` filter/TargetGroup credit tests.
Focused Space Madness/public-event objective-credit tests passed `624/624`,
full DataMapping generation and content-retail regeneration completed,
generated `creature_public_event_map.csv` reports relation `27` as
`match_status=reviewed`, and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the unique-name bridge review. Live coordinate precision, phase visibility,
activation UI timing and credit, replay cleanup, reward, medal, achievement,
and full expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Ravenous Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-061645-instance-2149-space-madness-ravenous-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`26`: Jabbithole creature `446` (`Ravenous Nightmare`) -> Creature2 `46133`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6401`, TargetGroup member `46133`, local world `2149` /
area `2415` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `26` as `match_status=reviewed`,
and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Same-name relation `2013` (`28076` -> `69220`)
remains untouched; live Ravenous Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat
choreography, replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Objective `1662` hallucination-wave
mechanics remain untouched.

The 2026-06-18 Space Madness Writhing Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-055746-instance-2149-space-madness-writhing-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`25`: Jabbithole creature `392` (`Writhing Nightmare`) -> Creature2 `46123`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6400`, TargetGroup member `46123`, local world `2149` /
area `2413` spawn candidates, creature spell rows, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `25` as `match_status=reviewed`,
and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the ambiguous-name bridge review. Same-name relation `2009` (`28046` ->
`69213`) remains untouched; live Writhing Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat choreography,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Objective `1662` hallucination-wave
mechanics remain untouched.

The 2026-06-18 Space Madness Foaming Roandoe Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-181059-instance-2149-space-madness-foaming-roandoe-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2010`: level-50 source creature `28056` (`Foaming Roandoe`) -> Creature2
`69171`. The bridge is backed by exact level-50 source/client mapping, 104
world `2149` / area `2419` coordinate rows, spell evidence `6382` / `6422` /
`6423`, and already-reviewed normal source creature `3684` / relation `159` ->
Creature2 `45727`. TargetGroup `6363` lists normal Creature2 `45727` with
livestock-family rows, but no current PE390 objective row uses TargetGroup
`6363`. No TargetGroup row currently lists Creature2 `69171`, and no current
PE390 objective row owns Creature2 `69171`. Full DataMapping generation and
content-retail regeneration completed, generated `creature_public_event_map.csv`
reports relation `2010` as `match_status=reviewed`. This closes only the
level-50 Foaming Roandoe bridge review; live relation `2010` spawn/import
placement, Creature2 `69171` objective ownership / usage, objective credit,
phase timing, replay cleanup, reward, medal, achievement, and full Prime
expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Writhing Nightmare Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-175116-instance-2149-space-madness-writhing-nightmare-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2009`: level-50 source creature `28046` (`Writhing Nightmare`) -> Creature2
`69213`. The bridge is backed by exact level-50 source/client mapping, 65
world `2149` / area `2413` coordinate rows, spell evidence `2745` / `2762` /
`8142` / `10856` / `10859`, and already-reviewed normal source creature `392`
/ relation `25` -> Creature2 `46123`. Objective `1590` uses reward-pane
TargetGroup `6402`, which nests TargetGroup `6400`; TargetGroup `6400`
contains normal Creature2 `46123`, and `SavePanickedWorkersNightmareEntityScript`
filters the normal row. No TargetGroup row currently lists Creature2 `69213`,
and no current PE390 objective row owns Creature2 `69213`. Full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `2009` as
`match_status=reviewed`. This closes only the level-50 Writhing Nightmare
bridge review; live relation `2009` spawn/import placement, Creature2 `69213`
objective ownership / usage, objective credit, phase timing, replay cleanup,
reward, medal, achievement, and full Prime expedition smoke remain the named
external evidence source.

The 2026-06-18 Space Madness Dark Creeper bridge-review worksheet is
`artifacts\blocker_evidence\20260618-173021-instance-2149-space-madness-dark-creeper-bridge-review`.
It records the tracked DataMapping review for public-event creature relations
`2008` and `2246`: level-50 source creature `28040` (`Dark Creeper`) ->
Creature2 `69163`, and normal level-32 source creature `9512` (`Dark Creeper`)
-> Creature2 `48345`. The bridges are backed by exact source/client level
matches, faction `218`, action set `2348`, display group `24216`, display
`21629`, 56 and 95 world `2149` / area `2421` coordinate rows, shared Dark
Creeper spell-family evidence, TargetGroup `12680` for Creature2 `69163`, and
TargetGroup `12675` for Creature2 `48345`. No current PE390 objective row uses
TargetGroup `12680`, TargetGroup `12675`, Creature2 `69163`, or Creature2
`48345` directly. Full DataMapping generation completed; the first
content-retail regeneration with a relative tracker path failed with Windows
`[Errno 22] Invalid argument`, and the same regeneration succeeded with absolute
paths. Generated `creature_public_event_map.csv` now reports relations `2008`
and `2246` as `match_status=reviewed`. This closes only the Dark Creeper bridge
review; live relation `2008` / `2246` spawn/import placement, objective
ownership/usage, objective credit, phase timing, replay cleanup, reward, medal,
achievement, and full expedition smoke remain the named external evidence
source.

The 2026-06-18 Space Madness Panicked Worker bridge-blocker worksheet is
`artifacts\blocker_evidence\20260618-172641-instance-2149-space-madness-panicked-worker-bridge-blocker`.
It records the blocked DataMapping review for public-event creature relations
`2007` and `2422`: source creature `28037` / level-50 Panicked Worker and
source creature `863` / level-32 Panicked Worker both currently map to
Creature2 `46062` as `ambiguous_name`. The ranked bridge candidates remain
tied: both source rows have equal-score exact-name Creature2 candidates
`46062` and `46063`, shared faction `219`, level overlap, score `140`, no
exact-level match, difficulty mismatch, and no spatial/entity match to break
the tie. Coordinate evidence has 21 world `2149` / area `2416` rows for
source creature `28037` and 24 for source creature `863`, but no Panicked
Worker spell rows, no TargetGroup rows listing Creature2 `46062` or `46063`,
and no current PE390 objective rows using either worker row as objective object
or reward-pane TargetGroup. Objective `1590` still uses reward-pane
TargetGroup `6402`, the nightmare target-group set already covered by
`SavePanickedWorkersNightmareEntityScript`. No override, DataMapping
regeneration, safe-import SQL, runtime spawn, script, objective, reward, medal,
achievement, or replay-cleanup behavior was changed. Relations `2007` and
`2422` remain blocked pending build 16042 live-client/server smoke or retail
packet/log capture for worker spawn identity and objective ownership,
decompile/client table evidence distinguishing Creature2 `46062` from `46063`,
or authoritative row-level target-group/objective evidence linking one worker
row to PE390.

The 2026-06-18 Space Madness Rending Nightmare Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-170357-instance-2149-space-madness-rending-nightmare-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2006`: level-50 source creature `28035` (`Rending Nightmare`) -> Creature2
`69218`. The bridge is backed by exact level-50 source/client mapping, 100
world `2149` / area `2414` coordinate rows, Lingering Venom/Swipe/Rend/Caustic
Leap spell evidence, and already-reviewed normal source creature `1121` /
relation `29` -> Creature2 `46130`. Objective `1590` uses reward-pane
TargetGroup `6402`, which nests TargetGroup `6400`; TargetGroup `6400`
contains normal Creature2 `46130`, and `SavePanickedWorkersNightmareEntityScript`
filters the normal row. No TargetGroup row currently lists Creature2 `69218`,
and no current PE390 objective row owns Creature2 `69218`. Full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `2006` as
`match_status=reviewed`, content-retail audit tests passed `98/98`, validator
reported `31` files / `168101` rows all `not_retail_complete`, blocker
evidence harness passed `2/2`, `RuntimeDataBoundaryTests` passed `6/6`, and
tracked override CSV field counts are clean. This closes only the level-50
Rending Nightmare bridge review; live relation `2006` spawn/import placement,
Creature2 `69218` objective ownership / usage, objective credit, phase timing,
replay cleanup, reward, medal, achievement, and full Prime expedition smoke
remain the named external evidence source.

The 2026-06-18 Space Madness Eye Nightmare Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-164555-instance-2149-space-madness-eye-nightmare-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2005`: level-50 source creature `28023` (`Eye Nightmare`) -> Creature2
`69209`. The bridge is backed by exact level-50 source/client mapping, 61
world `2149` / area `2416` coordinate rows, Punch/Jab/Flourishing Combo/Haymaker
spell evidence, and already-reviewed normal source creature `3663` / relation
`154` -> Creature2 `46723`. TargetGroup `6449` covers only normal
Nightmare-family members `46722`, `46723`, and `46724`, no current PE390
objective row uses TargetGroup `6449`, objective `1590` uses reward-pane
TargetGroup `6402`, and `SavePanickedWorkersNightmareEntityScript` remains
scoped to nested TargetGroups `6400` / `6401` rows. No TargetGroup row
currently lists Creature2 `69209`, and no current PE390 objective row owns
Creature2 `69209`. Full DataMapping generation and content-retail regeneration
completed, generated `creature_public_event_map.csv` reports relation `2005`
as `match_status=reviewed`, content-retail audit tests passed `98/98`,
validator reported `31` files / `168101` rows all `not_retail_complete`,
blocker evidence harness passed `2/2`, `RuntimeDataBoundaryTests` passed `6/6`,
and tracked override CSV field counts are clean. This closes only the level-50
Eye Nightmare bridge review; live relation `2005` spawn/import placement,
Creature2 `69209` objective ownership / usage, objective credit, phase timing,
replay cleanup, reward, medal, achievement, and full Prime expedition smoke
remain the named external evidence source.

The 2026-06-18 Space Madness Blazing Crewman Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-161841-instance-2149-space-madness-blazing-crewman-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2004`: level-50 source creature `28020` (`Blazing Crewman`) -> Creature2
`69205`. The bridge is backed by exact level-50 source/client mapping, 78
world `2149` / area `2416` coordinate rows, Arcane Bolt/Firestorm/Erupting
Fissure spell evidence, and already-reviewed normal source creature `3642` /
relation `148` -> Creature2 `46714`. TargetGroup `6447`, objective `1662`,
and `HallucinatingLivestockEntityScript` cover only normal Creature2 `46714`;
no TargetGroup row currently lists Creature2 `69205`, and no current PE390
objective row owns Creature2 `69205`. Full DataMapping generation and
content-retail regeneration completed, generated `creature_public_event_map.csv`
reports relation `2004` as `match_status=reviewed`, content-retail audit tests
passed `98/98`, validator reported `31` files / `168101` rows all
`not_retail_complete`, blocker evidence harness passed `2/2`,
`RuntimeDataBoundaryTests` passed `6/6`, and tracked override CSV field counts
are clean. This closes only the level-50 Blazing Crewman bridge review; live
relation `2004` spawn/import placement, Creature2 `69205` objective ownership
/ usage, objective credit, phase timing, replay cleanup, reward, medal,
achievement, and full Prime expedition smoke remain the named external
evidence source.

The 2026-06-18 Space Madness Violent Oxiancow Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-155502-instance-2149-space-madness-violent-oxiancow-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2003`: level-50 source creature `28018` (`Violent Oxiancow`) -> Creature2
`69139`. The bridge is backed by exact level-50 source/client mapping, 101
world `2149` / area `2419` coordinate rows, Ram/Bash/Bucking
Frenzy/Overwhelming Bellow spell evidence, and TargetGroup `12677` grouping
Creature2 `69139` with other Prime livestock and scorched rows; no current
PE390 objective row uses TargetGroup `12677`. Normal source creature `3686` /
relation `422` remains reviewed to Creature2 `45729`. Full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `2003` as
`match_status=reviewed`, content-retail audit tests passed `98/98`, validator
reported `31` files / `168101` rows all `not_retail_complete`, blocker
evidence harness passed `2/2`, `RuntimeDataBoundaryTests` passed `6/6`, and
tracked override CSV field counts are clean. This closes only the level-50
Violent Oxiancow bridge review; live relation `2003` spawn/import placement,
TargetGroup `12677` objective ownership/usage, objective credit, phase timing,
replay cleanup, reward, medal, achievement, and full Prime expedition smoke
remain the named external evidence source.

The 2026-06-18 Space Madness Smoldering Billow Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-153727-instance-2149-space-madness-smoldering-billow-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2002`: level-50 source creature `28006` (`Smoldering Billow`) -> Creature2
`69168`. The bridge is backed by exact level-50 source/client mapping, 93 world
`2149` / area `2421` coordinate rows, Dice/Slashing Strikes/Slice/Feet of Fury
spell evidence, and TargetGroup `12681` grouping Creature2 `69168` with
`69169` and `69170`; no current PE390 objective row uses TargetGroup `12681`.
Lower source creature `9513` / relation `2247` remains ambiguous-name to
Creature2 `48351` and was not promoted. Full DataMapping generation and
content-retail regeneration completed, generated `creature_public_event_map.csv`
reports relation `2002` as `match_status=reviewed`, content-retail audit tests
passed `98/98`, validator reported `31` files / `168101` rows all
`not_retail_complete`, blocker evidence harness passed `2/2`,
`RuntimeDataBoundaryTests` passed `6/6`, and tracked override CSV field counts
are clean. This closes only the level-50 Smoldering Billow bridge review; live
relation `2002` spawn/import placement, TargetGroup `12681` objective
ownership/usage, objective credit, phase timing, replay cleanup, reward, medal,
achievement, and full Prime expedition smoke remain the named external evidence
source.

The 2026-06-18 Space Madness Major Lee Barmy duplicate bridge-review worksheet is
`artifacts\blocker_evidence\20260618-151712-instance-2149-space-madness-major-lee-barmy-duplicate-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2120`: duplicate source creature `28252` (`Major Lee Barmy`) -> Creature2
`45812`. The bridge is backed by candidate-count-1 mapping, a world `2149` /
area `2482` coordinate row, already-reviewed companion source creature `332` /
relation `23` -> Creature2 `45812`, PE390 objective `1588`, TargetGroup
`6358`, TargetGroup sole member `45812`, `SpaceMadnessEventScript` reviewed
talk NPC spawn `1100300006`, and `MajorLeeBarmyEntityScript` filter/target-group
credit evidence. Full DataMapping generation and content-retail
regeneration completed, generated `creature_public_event_map.csv` reports
relation `2120` as `match_status=reviewed`, content-retail audit tests passed
`98/98`, validator reported `31` files / `168101` rows all
`not_retail_complete`, blocker evidence harness passed `2/2`,
`RuntimeDataBoundaryTests` passed `6/6`, and tracked override CSV field counts
are clean. This closes only the duplicate unique-name bridge review; live
relation `2120` spawn/import placement, objective `1588` UI timing and talk
credit, phase timing, replay cleanup, reward, medal, achievement, and full
expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Major Lee Barmy bridge-review worksheet is
`artifacts\blocker_evidence\20260618-054032-instance-2149-space-madness-major-lee-barmy-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`23`: Jabbithole creature `332` (`Major Lee Barmy`) -> Creature2 `45812`. The
bridge is backed by PE390 objective `1588`, TargetGroup `6358`, TargetGroup
sole member `45812`, and the existing `MajorLeeBarmyEntityScript`
filter/target-group credit tests. Focused Space Madness/public-event
objective-credit tests passed `624/624`, full DataMapping generation and
content-retail regeneration completed, generated `creature_public_event_map.csv`
reports relation `23` as `match_status=reviewed`, and the content-retail
blocker row now reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`.
This closes only the unique-name bridge review. Same-target relation `2120`
(`28252` -> `45812`) is now tracked by the later duplicate bridge review above;
live relation `23` spawn/import placement, objective `1588` UI timing and talk
credit, phase timing, replay cleanup, reward, medal, achievement, and full
expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Electrifying Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-052313-instance-2149-space-madness-electrifying-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`24`: Jabbithole creature `389` (`Electrifying Nightmare`) -> Creature2
`46127`. The bridge is backed by PE390 objective `1590`, reward-pane
TargetGroup `6402`, nested TargetGroup `6400`, TargetGroup member `46127`, local
world `2149` / area `2414` spawn candidates, creature spell rows, and the
existing `SavePanickedWorkersNightmareEntityScript` filter/credit tests.
Focused Space Madness/public-event objective-credit tests passed `624/624`,
full DataMapping generation and content-retail regeneration completed,
generated `creature_public_event_map.csv` reports relation `24` as
`match_status=reviewed`, and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Same-name relation `2029` (`28128` -> `69216`)
remains untouched; live Electrifying Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat choreography,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Objective `1662` hallucination-wave
mechanics remain untouched.

The 2026-06-18 Space Madness Verminous Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-050409-instance-2149-space-madness-verminous-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`22`: Jabbithole creature `205` (`Verminous Nightmare`) -> Creature2 `46131`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6400`, TargetGroup member `46131`, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Source creature
`205` is represented only by PE390 relation `22`; no competing same-name
public-event creature row was found. Focused Space Madness/public-event
objective-credit tests passed `624/624`, full DataMapping generation and
content-retail regeneration completed, generated `creature_public_event_map.csv`
reports relation `22` as `match_status=reviewed` with source-row
`last_seen_in=1`, and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the unique-name bridge review. Live Verminous Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat choreography,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Objective `1662` hallucination-wave
mechanics remain untouched.

The 2026-06-18 Space Madness Venomous Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-044521-instance-2149-space-madness-venomous-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`20`: Jabbithole creature `64` (`Venomous Nightmare`) -> Creature2 `46128`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6400`, TargetGroup member `46128`, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `20` as `match_status=reviewed`,
and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Venomous Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat choreography,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Same-name relation `2039` (`28183` ->
`69217`) and objective `1662` hallucination-wave mechanics remain untouched.
The 2026-06-18 Space Madness Stinging Nightmare bridge-review worksheet is
`artifacts\blocker_evidence\20260618-042832-instance-2149-space-madness-stinging-nightmare-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`18`: Jabbithole creature `39` (`Stinging Nightmare`) -> Creature2 `46124`.
The bridge is backed by PE390 objective `1590`, reward-pane TargetGroup `6402`,
nested TargetGroup `6400`, TargetGroup member `46124`, and the existing
`SavePanickedWorkersNightmareEntityScript` filter/credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `18` as `match_status=reviewed`,
and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the scored-name bridge review. Live Stinging Nightmare spawn/import placement,
panicked-worker objective `1590` UI timing and credit, worker combat choreography,
replay cleanup, reward, medal, achievement, and full expedition smoke remain
the named external evidence source. Same-name relation `2014` (`28086` ->
`69214`) is reviewed by the later Stinging Nightmare Prime bridge slice;
objective `1662` hallucination-wave mechanics remain untouched.
The 2026-06-18 Space Madness Stinging Nightmare Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-202500-instance-2149-space-madness-stinging-nightmare-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2014`: Jabbithole creature `28086` (`Stinging Nightmare`) -> Creature2 `69214`.
The bridge is backed by exact level-50 source/client mapping, 86 world `2149` /
area `2413` coordinate rows, source spell evidence `2893` / `2911` / `2787` /
`6402`, client Spell4 evidence `32965` / `32966` / `35963` / `44638`, and the
already-reviewed normal relation `18` (`39` -> `46124`). Objective `1590`
evidence remains scoped to the normal row through TargetGroup `6402` -> `6400`
and `SavePanickedWorkersNightmareEntityScript`; no TargetGroup row currently
lists Creature2 `69214`, and objective `2115` TargetGroup `12684` does not list
Creature2 `69214` through nested groups `12682` or `12683`. This closes only the
scored-name Prime bridge review. Live Stinging Nightmare Prime spawn/import
placement, Creature2 `69214` objective ownership/credit, objective `2115`
wave/timer ownership, replay cleanup, reward, medal, achievement, and full
expedition smoke remain the named external evidence source.

The 2026-06-18 Space Madness Hallucinating Cubig Prime bridge-review worksheet is
`artifacts\blocker_evidence\20260618-202730-instance-2149-space-madness-hallucinating-cubig-prime-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2016`: Jabbithole creature `28092` (`Hallucinating Cubig`) -> Creature2
`69137`. The bridge is backed by exact level-50 source/client mapping, 96 world
`2149` / area `2421` coordinate rows, source spell evidence `6436` / `6455` /
`6456` / `6461`, client Spell4 evidence `6891` / `39867` / `39868` / `6892`,
already-reviewed normal relation `152` (`3654` -> `45724`), and the objective
`2115` target-group chain TargetGroup `12684` -> `12683` -> `12677` containing
Creature2 `69137`. No direct Space Madness script or test binding for Creature2
`69137`, Creature2 `45724`, or Hallucinating Cubig was found. Full DataMapping
generation and content-retail regeneration completed, generated
`creature_public_event_map.csv` reports relation `2016` as
`match_status=reviewed`, content-retail audit tests passed `98/98`, validator
reported `31` files / `168101` rows all `not_retail_complete`, blocker evidence
harness passed `2/2`, `RuntimeDataBoundaryTests` passed `6/6`, and tracked
override CSV field counts are clean. This closes only the level-50
Hallucinating Cubig bridge review; live relation `2016` spawn/import placement,
Creature2 `69137` objective ownership / usage, objective credit, objective
`2115` wave ownership, phase timing, replay cleanup, reward, medal,
achievement, and full Prime expedition smoke remain the named external evidence
source. Relations `153`, `2007`, and `2422` remain blocked pending
row-level/live/client evidence.

The 2026-06-18 Space Madness Slinking Slank bridge-review worksheet is
`artifacts\blocker_evidence\20260618-040814-instance-2149-space-madness-slinking-slank-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`2033`: Jabbithole creature `28141` (`Slinking Slank`) -> Creature2 `69899`.
The bridge is backed by PE390 objective `4709`, object/TargetGroup `12595`,
TargetGroup sole member `69899`, and the existing `ExperimentalSlankEntityScript`
filter/credit tests. Focused Space Madness/public-event objective-credit tests
passed `624/624`, full DataMapping generation and content-retail regeneration
completed, generated `creature_public_event_map.csv` reports relation `2033`
as `match_status=reviewed`, and the content-retail blocker row now reports
`reviewed_public_event_creature_blocked_spawn_credit_smoke`. This closes only
the unique-name bridge review. Live Slinking Slank spawn/import placement,
objective `4709` UI timing, parent objective `4712` progression, replay cleanup,
reward, medal, achievement, and full expedition smoke remain the named external
evidence source.
The 2026-06-18 Space Madness Hallucinating Worker multi-member bridge blocker is
recorded in generated content-retail evidence rather than a runtime override.
Public-event creature relation `153` maps Jabbithole creature `3655`
(`Hallucinating Worker`) to ambiguous Creature2 `45903`, while build 16042
objective `1594` uses TargetGroup `6370` with members `45903` and `45904`.
The audit now emits
`blocked_public_event_creature_ambiguous_multi_member_targetgroup_requires_relation_smoke`
with `target_group_id=6370`, because a one-to-one global creature bridge cannot
prove which target-group member or spawn variant relation `153` owns. Focused
audit tests passed `98/98`, generated content-retail validation still reports
`31` files / `168,101` rows, and full content-retail regeneration completed.
No runtime behavior, spawn import, or DataMapping bridge override was widened.
The next artifact required to unblock the row is relation- or spawn-scoped
bridge review, live client/server objective-credit smoke, or a captured
placement/import artifact proving member selection for TargetGroup `6370`.
The 2026-06-18 Space Madness Datapad bridge-review worksheet is
`artifacts\blocker_evidence\20260618-033257-instance-2149-space-madness-datapad-bridge-review`.
It records the tracked DataMapping review for public-event creature relation
`19`: Jabbithole creature `48` (`Datapad`) -> Creature2 `45993`. The bridge is
backed by PE390 objective `1596`, TargetGroup `6384`, TargetGroup member
`45993`, Space Madness-only Jabbithole relation/spawn evidence, and the existing
`CrewDatapadEntityScript` target-group checklist credit tests. Focused Space
Madness/public-event objective-credit tests passed `624/624`, the mapper-loader
check applied the override as `reviewed`, full DataMapping generation and
content-retail regeneration completed, generated `creature_public_event_map.csv`
reports relation `19` as `match_status=reviewed`, and the content-retail
blocker row now reports `reviewed_public_event_creature_blocked_spawn_credit_smoke`.
This closes only the ambiguous bridge review; live Datapad spawn/import,
objective `1596` UI timing, checklist count behavior, visual/replay cleanup,
reward, achievement, and full expedition smoke remain the named external
evidence source.
The latest Ultimate Protogames Downsizer validation worksheet is
`artifacts\blocker_evidence\20260618-022350-instance-3041-map-ultimateprotogamesraid-recheck`;
it records world `3041`, public event `642`, and the queued objective set
`3197;3266;3267;3268;3269`. Focused Downsizer, map-binding, and instance
settings verification passed `35/35` from alternate output path
`artifacts\test-bin\downsizer`; blocker harness presets passed `2/2`;
generated content-retail validation still reports `31` files / `168,101` rows,
all `not_retail_complete`. No runtime behavior or data changed in the
2026-06-18 recheck. Current automation remains scoped to the test-backed
objective `3197` activation/death-credit/finalization path, challenge-objective
activation for `3266`/`3267`/`3268`/`3269`, map binding, and shared
instance-settings handling. The row remains blocked on exact portal entry
routing, Downsizer challenge ability semantics, boss mechanics, reward and
achievement side effects, cleanup/replay, negative cases, and full dungeon
smoke. Challenge activation, `3197` death credit/finalization, and map binding
were rejected as shortcut proof for retail completion.
The latest Ruins of Kel Voreth validation worksheet is
`artifacts\blocker_evidence\20260618-023014-instance-1336-ruins-of-kel-voreth-recheck`;
it records world `1336`, public event `161`, achievements
`2637;3463;5299;5300;5301;5302;5303;5304;5305`, and the queued objective set
`444;445;446;447;448;449;450;451;453;456;457;459;461;845;846;847;869;975;976;977;1695;3202;5178;5344`.
Focused Ruins event/trigger/objective-credit verification passed `614/614`
from alternate output path `artifacts\test-bin\ruins-of-kel-voreth`; blocker
harness presets passed `2/2`; generated content-retail validation still
reports `31` files / `168,101` rows, all `not_retail_complete`. No runtime
behavior, data seeds, or generated CSV rows changed in the 2026-06-18 recheck.
Current automation remains scoped to the test-backed main phase chain, reviewed
boss spawn anchors and duplicate guards, death-credit producers for objectives
`444`, `446`, `447`, `449`, and `453`, and checklist activation credit for
objectives `451`, `456`, `459`, and `461`. Residual objectives
`457`, `845`, `847`, `869`, `975`, `976`, `977`, `1695`, `3202`, `5178`, and
`5344` remain mapped-only/blocked. Drokk exo-defense `847` activation was
rejected as failure/credit proof, WIP optional activation was rejected as route
availability proof, and boss death credit was rejected as mechanics/reward
proof. The next manual pass still needs portal entry, objective UI deltas,
exact phase/trigger placement, Drokk exo-defense failure/credit,
health-station, hidden jump puzzle, challenge/timer, rookie, kill-count,
boss-mechanic, door, optional-route selection/weight, spawn-cadence,
interactable visual-state, faction-communicator, cinematic, reward,
achievement, cleanup/replay, negative-case, and full dungeon smoke evidence
before any retail-complete claim.
The 2026-06-17 combined remaining-instance recheck passed `974/974` with the
focused Sanctuary, Fragment Zero, Skullcano, Gauntlet, Evil from the Ether,
Space Madness, Downsizer, map-binding/settings, and Ruins filters; this verifies
the current mapped/runtime WIP boundary only and does not close the worksheet
smoke blockers above.
`-RaidEventSmoke` adds `lws-110-117-raid-event-targets.md`, preloads worlds
`1333`, `1462`, `3032`, `3040`, `3044`, `3045`, and `3094`, preloads
public events `157`, `159`, `595`, `597`, `605`, `679`, and `705`, and
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

**F-002 client diagnostic opcode cluster: keep numeric/log-only until opcode-specific ownership is proven.**
For a native or accepted payload pass, target each unresolved diagnostic opcode
separately from its shared helper positive controls. Accepted proof for
renaming or mutating `Client0x00C8`, `Client0x00ED`, `Client0x011B`,
`Client0x011D`, `Client0x012D`, `Client0x0550`, `Client0x063E`,
`Client0x0701`, `Client0x07E3`, or `Client0x0928` must include an
opcode-specific native sender, post-read consumer, callback/table owner,
indirect send rail, or captured payload with a client-visible effect. Shared
helper shape is not enough.

2026-06-17 cached-export/source/artifact recheck: `mcp__ghidra_mcp.list_instances`
found no running Ghidra instance. Cached `WildStar64.exe` fragments keep the
target opcodes mapped-only: `Network_RegisterServerOpcode_0351` (`14006c290`)
still registers `0x00C8`, `0x00ED`, `0x011B`, `0x011D`, `0x012D`, and
`0x0701`; `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) still
registers `0x0550`, `0x063E`, `0x07E3`, and `0x0928`. Helper fragments prove
only the already-modeled wire shapes. Current source keeps the models neutral
and handlers log-only/no-emit, and existing `artifacts\packet_evidence` rows
contain no target hits. Focused diagnostic/placeholder tests passed `106/106`;
harness presets passed `2/2`; content-retail validation reported `31` files
and `168,101` `not_retail_complete` rows; the `WildStar64.exe` manifest check
passed with `200/200` reused fragments. Worksheet:
`artifacts/blocker_evidence/20260617-233631-20260617-F002-client-diagnostic-opcodes-recheck`.

**F-002 `Client0x0928`: keep diagnostic-only until opcode-specific ownership is proven.**
For a native or accepted payload pass, target `0x0928` separately from pet stance and enable packet evidence for both the diagnostic row and the positive pet controls `0x068E`/`0x068F`. Accepted proof for renaming or mutating `Client0x0928` must include an opcode-specific native sender, post-read consumer, callback/table owner, indirect send rail, or captured payload with a client-visible effect. Shared `uint32 + 5-bit` helpers are not enough.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 evidence keeps `Client0x0928` mapped-only: `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x0928` size `8` at `selected_decompiled.c:27834` with `ClientUInt32UInt5_WritePayload` (`1400898b0`) and `ServerUInt32UInt5_ReadPayload` (`14008ce80`), proving only one `uint32` plus one 5-bit field. The positive pet controls stay separate: `Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x068E` to the same writer at `selected_decompiled.c:13114` and `0x068F` to the same reader at `selected_decompiled.c:13182`, but `Pet_SetStance_SendClientPetSetStance` (`14050a270`) sends only `0x068E`, while `Pet_ApplyStanceChangedPayload` (`1403c0a80`) / `Pet_GetStance_ReadCachedStance` (`14050a130`) explain only `0x068F`. Current source keeps `Source/NexusForever.Network.World/Message/Model/ClientUnresolvedDiagnosticPackets.cs` `Client0x0928.LeadingValue` / `TrailingBits` neutral and `Source/NexusForever.WorldServer/Network/Message/Handler/Misc/ClientUnresolvedDiagnosticHandlers.cs` log-only; packet and handler tests preserve that boundary. Do not alias `0x0928` to pet stance, cooldown UI, reward-property, movement, or buff behavior until a second opcode-specific witness exists.

**F-025 Entity-stat aux: keep `0x0889`/`0x08CC`/`0x08F4`/`0x0939`/`0x093D`/`0x093E` blocked until per-opcode apply or live order proof.**
For a live pass, enable packet evidence for the six entity-stat aux opcodes plus regular stat updates and exercise combat, death/respawn, vehicle/passenger transitions, emotes, item use, CSI/direct interaction, and reputation/override-looking UI states. Capture the exact action transcript, server logs, client-visible state, and negative case where regular `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger` is sufficient without aux. Implement only an aux producer or semantic field rename proven by a per-opcode native apply handler, apply-table classification, or accepted live sniff/order witness.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments still show `0x0889` through the shared three-`uint32` triplet reader, `0x08CC` through the shared `uint32` plus wide-string reader, and `0x08F4`/`0x0939`/`0x093D`/`0x093E` through reader-only entity-stat aux shapes. Source search still finds the six aux packet models only in models/tests/negative guards, while regular runtime stat sends use `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger`. Keep the six aux producers and semantic field names blocked.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` again returned no running Ghidra instance, so the pass used cached `WildStar64.exe` fragments and current source/tests. `140080bf0`, `1400980f0`, `140097620`, `140097ee0`, `140097690`, and `140097f70` remain reader-only evidence for the six aux shapes, and stale `FUN_140939650` remains rejected as viewport/grid global math rather than a packet consumer or apply owner. Current source still finds the six `ServerEntityStat*` aux packets only in packet models, packet-shape tests, placeholder naming guards, and negative entity-create emission guards; regular stat sends stay on `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger`. The worksheet `artifacts/blocker_evidence/20260617-224723-F025-entity-stat-aux-recheck` records the missing live/manual path and negative cases. Keep producers and semantic names blocked until a per-opcode `WorldSocket+0x15b0` `vtable+0x58` apply handler, apply-table classification, or accepted live sniff/order witness proves field semantics and emit timing.

2026-06-17 interactive MCP recheck: `mcp__ghidra_mcp.connect_instance` connected to `NexusForeverClient64_WildStar64` over `http://127.0.0.1:8089` with `/WildStar64.exe` active, but the debugger server at `127.0.0.1:8099` was not running. Static MCP xrefs for `140080bf0`, `1400980f0`, `140097620`, `140097ee0`, `140097690`, `140097f70`, and `ServerEntityVisualInfoUpdate_ReadPayload` (`140098460`) again resolved to `Network_RegisterServerOpcode_0351`, shared row-reader calls, or data/unwind metadata only. The `140c1e858`/`140c1e900` pointer region around `140080bf0` and `140097620` decompiled as generic bit-reader helper slots, not packet apply handlers. `FindOpcodeComparisons.java` run through the MCP script runner found the true aux opcode immediates only in `Network_RegisterServerOpcode_0351`; additional `0x0939`/`0x093D`/`0x093E` hits decompiled as `GameFormula_GetEntryById` uses, and other matches were UI/object offsets or allocation sizes. Keep the six aux producers and semantic names blocked until a live debugger trace on the dispatch/apply path or accepted packet capture proves field semantics and emit timing.

**F-025 Map-tracked unit: keep `0x0849`/`0x0848` producers blocked until native send-site or live marker proof.**
For a live pass, enable packet evidence for `0x0849`/`0x0848`, exercise public-event/objective marker UI states that show map-tracked units, and capture the exact event/objective action transcript, marker appearance/disappearance timing, server logs, client logs, and negative cases where objective progress does not create a marker. Implement only when evidence proves tracked-unit id allocation, update cadence, disable lifetime, and `TrackingSlotId` selection; do not synthesize `TrackingSlotId` from `PublicEventObjectiveId` because duplicate `TrackingSlot` rows exist.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` returned no running Ghidra instance, so the pass used cached `WildStar64.exe` fragments and current source/tests. `Network_RegisterServerOpcode_0351` still registers `0x0849` size `0x14` to `ServerMapTrackedUnitUpdate_ReadPayload` (`1400a6c10`) and `0x0848` size `4` to the shared `ServerUInt32_ReadPayload`. `MapTrackedUnitUpdate_ApplyAndDispatch` (`1403f4170`), `MapTrackedUnitDisable_ApplyAndDispatch` (`1403f4200`), `ClientEvent_MapTrackedUnitUpdate_Dispatch` (`140430f80`), `Lua_GameLib_GetMapTrackedUnitData` (`140511c80`), and `ClientDB_RegisterTrackingSlot` (`1402426a0`) remain client read/apply/Lua/table-loader evidence only. The worksheet `artifacts/blocker_evidence/20260617-224146-F025-map-tracked-unit-producer-recheck` records the missing live/manual path and negative cases. Keep production emits blocked until a native send site or accepted public-event marker capture proves `0x0849`/`0x0848` timing and fields.

**F-003 Server0x0015: keep neutral until opcode-specific ownership is proven.**
For a live pass, enable packet evidence for `0x0015`, `0x0628`, and `0x03D2`, then exercise matching average-wait updates and Fortune UI opens as negative controls while watching for any separate `0x0015` payload. Accepted proof for changing `Server0x0015` must include a native `0x0015` apply/producer path, post-read consumer, or live `0x0015` packet with a client-visible effect distinct from matching and Fortune row-helper traffic.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached `ServerUInt5UInt32_ReadPayload` (`140081f00`) still proves only the shared 5-bit plus `uint32` reader registered for `0x0015` and `0x0628`; cached `ServerFortuneRewards_ReadPayload` (`140081f60`) calls the same reader as a money-reward row helper; cached `MatchingManager_ApplyMatchingAverageWaitTimeUpdated` (`1405c0e00`) remains the positive `0x0628` apply path. Keep `Server0x0015.Value0`/`Value1` neutral and non-emitted.

2026-06-17 cached-export/source/artifact recheck: `mcp__ghidra_mcp.list_instances`
again found no running Ghidra instance. Cached `WildStar64.exe` fragments still
register `0x0015` and `0x0628` to `ServerUInt5UInt32_ReadPayload`
(`140081f00`), which proves only one 5-bit field plus one `uint32`.
`ServerFortuneRewards_ReadPayload` (`140081f60`) calls that helper only as an
internal `0x03D2` money-reward row reader, while
`MatchingManager_ApplyMatchingAverageWaitTimeUpdated` (`1405c0e00`) remains the
positive `0x0628` apply path. Current source has no `Server0x0015` emitter,
and existing `artifacts\packet_evidence` rows contain no `0x0015` target hits.
Focused placeholder/matching/Fortune boundary tests passed `121/121`; harness
presets passed `2/2`; content-retail validation reported `31` files and
`168,101` `not_retail_complete` rows; the `WildStar64.exe` manifest check
passed with `200/200` reused fragments. Worksheet:
`artifacts/blocker_evidence/20260617-234147-20260617-F003-server-0015-recheck`.

**F-003 ServerTimeOfDayAuxUInt32: keep `0x0846` non-emitted until producer timing is proven.**
For a live or native pass, enable packet evidence for `0x0845`, `0x0846`, and
the nearby shared-slot `0x01A6` control, then exercise login/world entry,
time-of-day prerequisite gates, and normal clock sync. Accepted proof for
emitting or renaming `ServerTimeOfDayAuxUInt32` must include a native server
producer/send site, apply/consumer owner, callback/table owner, or accepted
`0x0846` capture with a client-visible effect distinct from normal `0x0845`
clock sync.

2026-06-17 cached-export/source/artifact recheck: `mcp__ghidra_mcp.list_instances`
found no running Ghidra instance. Cached `WildStar64.exe` registration still
binds `0x0846` size `4` to unlabelled `LAB_140080c60`, with `0x01A6` reusing
the same selected slot and no standalone `140080c60.fragment.c` in the selected
cache. `0x0845` remains the normal three-`uint32` `ServerTimeOfDay` packet, and
`Prerequisite_CheckTimeOfDay` (`14049dd10`) remains comparison-only evidence.
Current source emits only `ServerTimeOfDay` from `Player.SendInGameTime`, and
existing `artifacts\packet_evidence` rows contain no `0x0846` target hits.
Focused aux/prerequisite tests passed `470/470`; harness presets passed `2/2`;
content-retail validation reported `31` files and `168,101`
`not_retail_complete` rows; the `WildStar64.exe` manifest check passed with
`200/200` reused fragments. Worksheet:
`artifacts/blocker_evidence/20260617-234715-20260617-F003-time-of-day-aux-recheck`.

**F-003 story/recruitment boundary: keep `0x074A` / `0x077E` non-emitted until producer timing is proven.**
For a live or native pass, enable packet evidence for `0x074A`, `0x077E`, and
the positive `ServerFlightPathUpdate` (`0x0188`) counted-list control, then
exercise story communicator/unit transitions, recruitment UI updates, pet
despawn/stance-adjacent transitions, and flight-path updates. Accepted proof
for emitting or renaming `ServerStoryCommunicatorAux` or
`ServerRecruitmentAuxUInt32List` must include a native producer/apply owner,
post-read consumer, callback/table owner, or accepted live capture with a
client-visible effect and server-log correlation.

2026-06-17 cached-export/source/artifact recheck: `mcp__ghidra_mcp.list_instances`
found no running Ghidra instance. Cached `WildStar64.exe` registration still
binds `0x074A` size `0x18` to `ServerStoryCommunicatorAux_ReadPayload`
(`140080c80`) and `0x077E` size `0x10` to
`ServerFlightPathUpdate_ReadPayload` (`14008eaa0`). The readers prove only five
`uint32` fields plus one `uint16` for `0x074A`, and one `uint32` count plus a
counted `uint32` list for `0x077E`. Current source has no runtime producer for
either aux packet, and existing `artifacts\packet_evidence` rows contain no
target hits for either opcode/name or the flight-path control. Focused
packet-shape tests passed `101/101`; harness presets passed `2/2`;
content-retail validation reported `31` files and `168,101`
`not_retail_complete` rows; the `WildStar64.exe` manifest check passed with
`200/200` reused fragments. Worksheet:
`artifacts/blocker_evidence/20260617-235326-20260617-F003-story-recruitment-boundary-recheck`.

**F-007 Reward rotation content context: keep `0x07CD` field semantics blocked until retail/live apply proof.**
For a live pass, enable packet evidence for `0x07CC`, `0x07CD`, `0x07D3`, `0x07CA`, `0x07C8`, and the three entry-state delta opcodes `0x07C7`/`0x07C9`/`0x07CB`. Open Content Finder Bonus Rewards and storefront reward-rotation surfaces on a level-50 character, force refreshes for indexes `0..6`, claim a visible reward when available, reload the character, and capture a negative case where a refresh request produces no claimable reward. The accepted bundle must include the transcript, server logs, packet JSON, visible UI state before/after refresh and claim, and the generated reward-rotation runtime evidence artifact. Implement or rename only if a retail `0x07CD` payload or a dynamic breakpoint on the runtime apply dispatch proves `UInt0`/`UInt1`/`UInt3` slot assignment and `Flag` meaning.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances, so no fresh labels or exports were added. Cached `ServerRewardRotationContentContext_ReadPayload` (`14008fcb0`) still proves only the `0x07CD` wire shape, cached `RewardRotation_ManagerInit` (`140635840`) initializes seven throttle slots at `manager + 0x150 + index * 0x14`, cached `Reward_SendRewardUpdateRequest` (`140636ba0`) sends only the content-type index through `0x07CC`, and cached `RewardRotation_GetLoadedScheduleForContent` (`140636c40`) proves refresh-by-index / loaded-schedule lookup. Keep `ServerRewardRotationContentContext.UInt0`/`UInt1`/`UInt3`/`Flag` neutral and mapped-only.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` again returned no running Ghidra instance, so no fresh labels or exports were added. Cached registration still binds `0x07CD` size `0x28` to `ServerRewardRotationContentContext_ReadPayload` (`14008fcb0`) at `selected_decompiled.c:13312` and `0x07D3` size `0x10` to `ServerRewardRotationContentContextArray_ReadPayload` (`14008fdc0`) at `selected_decompiled.c:13313`. `14008fcb0` still proves only 14-bit index, four `uint32` fields, counted content-id array, and trailing flag; `14008fdc0` only proves counted rows. `RewardRotation_ManagerInit` (`140635840`) and `Reward_SendRewardUpdateRequest` (`140636ba0`) prove the seven request slots and index-only `0x07CC` request path, while `RewardRotation_GetLoadedScheduleForContent` (`140636c40`) and `RewardRotation_ApplyServerScheduleUpdate` (`140636280`) prove loaded-schedule lookup and `0x07CA` schedule application, not `0x07CD` content-context apply semantics. The worksheet `artifacts/blocker_evidence/20260617-231511-F007-reward-rotation-content-context-recheck` records the missing live/native evidence path and negative cases. Keep `UInt0`/`UInt1`/`UInt3` neutral/correlated and `Flag=false` until retail/live `0x07CD` payload evidence or a dynamic apply-dispatch breakpoint proves field assignment and consumer meaning.

**F-004 Housing neighborhood list: keep `0x0501`/`0x0506` producer blocked until native/live proof.**
Open the housing/neighborhood/community UI from a prepared residence and, if available, repeat from realm login into a housing-enabled character. Include outbound packet evidence for early housing outputs and inbound client requests around housing/neighborhood UI open. Capture `ServerHousingNeighborhoodEntry` / `ServerHousingNeighborhoodList` only if they actually appear, plus matching server logs, visible UI state, and a negative case such as non-community or no-neighborhood state. Implement only trigger/row fields proven by native producer evidence or an accepted live bundle; do not synthesize rows from `HousingNeighborhoodInfo.tbl`, residence session state, or `ServerHousingProperties.Residence.NeighbourhoodId`.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments still show `0x0501` / `0x0506` as row/list readers, `Housing_HandleNeighborhoodList` as client cache apply plus `HousingNeighborhoodRecieved`, and `ClientDB_RegisterHousingNeighborhoodInfo` as table-loader-only evidence. Source search still finds the neighborhood packets only in models/tests/negative guards. Keep the producer blocked pending a native server-push path or accepted live housing UI/realm-login capture proving trigger, row backing, field population, and realm/session timing.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` again returned no running Ghidra instance, so the pass used cached `WildStar64.exe` fragments and current source/tests. `14009cbe0` still proves only the `0x0501` / shared-row wire shape, `14009ebf0` proves only the `0x0506` realm/count/list reader, `1404ba4f0` proves only client cache apply plus `HousingNeighborhoodRecieved`, and `140205900` remains `HousingNeighborhoodInfo.tbl` loader evidence only. The worksheet `artifacts/blocker_evidence/20260617-223732-F004-housing-neighborhood-list-recheck` records the missing live/manual path and negative cases. Do not emit `0x0501`/`0x0506`, rename row-tail fields, or back rows from table/residence state until a native server-push/send-site path or live housing UI / realm-login capture proves trigger, row backing, field population, and realm/session timing.

**F-005 Marketplace aux: keep `0x06DF`/`0x07D5` non-emitted until marketplace producer proof.**
For a live or native pass, exercise auction post/search/filter, buyout, bid, cancel, commodity submit/fill/cancel, and CREDD views with packet evidence enabled for marketplace response/update clusters plus `0x06DF` and `0x07D5`. Accepted proof for widening these aux packets must include a native marketplace apply/producer path, server producer witness, callback/table owner, or accepted marketplace packet capture that ties the fields to visible UI state and emit timing.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments under `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` keep both packets mapped-only: `selected_decompiled.c:13318` registers `0x06DF` size `0x20` to `ServerAuctionPostAux_ReadPayload` (`140090090`), and `selected_decompiled.c:13314` registers `0x07D5` size `0x14` to `ServerAuctionsByFilterAux_ReadPayload` (`14008fe80`). Selected xrefs are label-only and selected call edges stay reader-local through bit-read/allocation/copy helpers. Current source paths remain packet-contract only: `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs` and `Source/NexusForever.Game.Tests/Network/PacketPlaceholderNamingTests.cs`; no `WorldServer` emitter was found. Keep `ServerAuctionPostAux.Values`/`Data`/`Value` and `ServerAuctionsByFilterAux.UInt14Value`/`Value1..Value3`/`Flag` neutral until producer evidence proves field semantics and timing.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` again returned no running Ghidra instance, so no fresh labels or exports were added. Cached registration still binds `0x07D5` size `0x14` to `ServerAuctionsByFilterAux_ReadPayload` (`14008fe80`) at `14006c290.fragment.c:627` / `selected_decompiled.c:13314`, and `0x06DF` size `0x20` to `ServerAuctionPostAux_ReadPayload` (`140090090`) at `14006c290.fragment.c:631` / `selected_decompiled.c:13318`. `14008fe80` still reads one 14-bit field, three `uint32` fields, and one flag; `140090090` still reads a `uint32` count, counted `uint32` array, counted byte array of the same count, and one trailing `uint32`. Current `WorldServer` marketplace handlers emit status/result/search/owned-list/commodity packets only, with no aux send site. The worksheet `artifacts/blocker_evidence/20260617-230906-F005-marketplace-aux-recheck` records the missing live/native evidence path and negative cases. Keep both aux models non-emitted and neutral until a native marketplace apply/producer path, server producer witness, or accepted marketplace packet capture proves fields and emit timing.

**F-010 Group/Raid/Matching: implementable for empty/status diagnostics, still blocked for non-zero queue semantics.**
Open raid and queue UI, request raid info, start/stop replacement search if an in-progress match can be created. Capture logs for `ClientRaidInfoRequest`, `ServerRaidQueueStatus`, `ServerMatching0x05CF` if it appears, `ClientMatchingMatchInitiateLookingForReplacements`, `ClientMatchingStopLookingForReplacements`, and diagnostics `Client0x062A`/`Client0x0634`. Implement only observed empty-status/validation behavior; keep non-zero queue position, `0x05CF` field semantics, and backfill merge lifecycle blocked until a live non-zero payload or client consumer proves meaning.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` returned no
running Ghidra instance. Cached `WildStar64.exe` fragments and current
source/tests keep the remaining F-010 packet cluster mapped-only:
`ServerUInt32_LocalReadThunk` (`140099110`) proves only the shared raw
`uint32` shape for `0x05CF`, `MatchingManager_ApplyManagerUInt32Field0xA0`
(`1405c41c0`) remains a correlated manager-field candidate without opcode
index or `ClientEvent` ownership, `ServerHousingCommunityPlotReservation_ReadPayload`
(`140086e70`) proves only the reused identity-plus-`uint32` shape for `0x0600`,
`ServerRaidQueueStatus_ReadPayload` (`14008bf80`) plus
`ServerRaidInfoResponse_ReadPayload` (`14008c010`) and
`Group_DispatchRaidInfoResponse` (`1406042b0`) support only the `0x071A`
raid-info row names, and `ClientTradeskillResetTalents_WritePayload`
(`14007d010`) / `ClientUInt32_ReadPayload` (`14007d000`) remain shared
one-`uint32` helpers for `Client0x062A` / `Client0x0634`. The 2026-06-08 solo
and LAN CDB bundles captured normal queue, leave, average-wait, and match-ready
traffic but no real send/receive rows for `0x05CF`, `0x0600`, `0x062A`,
`0x0634`, `0x0718`, `0x0719`, or `0x071A`. The worksheet
`artifacts/blocker_evidence/20260617-230330-F010-matching-raid-cluster-recheck`
records the missing evidence source and negative cases. Next unblocker remains
a full-party/full-team replacement or raid-info live capture with packet/log/UI
evidence, or a real non-`.pdata` native dispatcher/producer table tying these
opcodes to client state.

2026-06-18 CEST blocked recheck: no running Ghidra CodeBrowser was available,
so no new native labels or exports were added. The local
`NexusForeverClient64_WildStar64` project still exists and the
`WildStar64.exe` decompile manifest passed with `200` selected functions reused
from canonical cache. Current cached/source evidence still stops at the same
boundaries: shared raw-`uint32` reader plus unowned manager-field candidate for
`0x05CF`, reused identity-plus-`uint32` reader for `0x0600`, `0x071A`
raid-info row mapping only for `0x0718`, and shared one-`uint32` client helpers
for `0x062A` / `0x0634`. Existing artifact scans found no newer F-010
packet-evidence bundle; prior CDB bundles remain positive controls for
neighboring queue/leave/average-wait/match-ready flow but negative for the
blocked target opcodes. Focused matching/raid verification passed `168/168`.
The worksheet
`artifacts\blocker_evidence\20260618-024435-F010-matching-raid-cluster-recheck`
records the negative cases. Keep the packet cluster blocked until an
interactive native producer/apply-table pass or accepted live matching/raid
capture proves timing and field meaning for `0x05CF`, `0x0600`, `0x062A`,
`0x0634`, standalone non-zero `0x0718`, `0x0719`, or `0x071A`.

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

2026-06-18 CEST cached-export/source/artifact recheck: no running Ghidra MCP
instance was available, so this pass used cached selected fragments, current
source/tests, and existing packet/spell evidence artifacts. Cached fragments
still prove only generic `0x00C2` service-token spell-cast shape and result
handling, while `ClientRapidTransport_WritePayload` (`14007ab80`) and existing
spell-evidence files prove separate `0x0141` credit-route rapid-transport
captures for spell `82922`. Source search found no rapid/taxi service-token
bypass, global route-state snapshot, or taxi embark/completion producer, and
artifact search found no `0x00C2` / `ClientSpellCastWithServiceToken` hits.
Worksheet:
`artifacts\blocker_evidence\20260618-002052-20260617-F009-service-token-global-route-recheck`.
Keep service-token transport bypass and global route-state blocked until a
native transport producer or accepted live transport UI capture proves route id,
source/destination, cost, service-token debit, and teleport timing for `0x00C2`.

**F-008 Crafting/Rune discovery: station request semantics and durable rune bridge closed; keep discovery, service-key names, and aux emit blocked.**
Station request branches are covered by native sender evidence and focused tests: simple/complex/autocraft can carry zero station ids, while additive requires a non-zero station before `0x084A`. The durable rune item bridge is implemented through `item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`, `RandomGlyphData`/`Glyphs`, and migration `20260531224115_ItemMicrochipIdsAndRuneSlots`, with focused rune tests covering socket/install round trips. Current evidence rejects a distinct client microchip-install mutator: client rune install is `0x085B` (`RuneCrafting_SendClientRuneInstall`), while `ServerItemMicrochips` (`0x056C`) and `Inventory_UpdateItemMicrochipsFromWire` are server-side item patch/update evidence with blocked producer timing. For the next live pass, teleport to crafting hubs, use station UI, `!item lookup <name> 25`, `!item add <itemId> <qty>`, then craft complex/random outputs and failed cases. Capture logs for station unit id, schematic id, material ids, craft type, completion/rejection reason, hot/cold UI state, and any current-craft/finish packets. Implement only behavior proven by logs or producer decomp. Discovery roll thresholds/unlock mutation, `CraftStats`/`ChargeCounts` semantics beyond diagnostics, service-key names `0x2C`/`0x4F`/`0x57`, current-craft cadence, `0x084B`/`0x0855` emit intent, non-success sigil precision, and `0x056C` server-side microchip patch timing remain blocked without producer/native/live evidence.

2026-06-09 live F-008 station/craft proof: bundle `artifacts\blocker_evidence\20260609-212659-F008-crafting-live` plus packet evidence `artifacts\packet_evidence\F008-crafting-live\20260609-201741-packet-evidence.jsonl` closed the generic-station fixed-recipe smoke for emulator parity. Creature `21793` uses activate spell `1817` with prerequisite `1050` (`IsPlayer == 0`), so activate-spell prerequisites must evaluate against the activated unit target. The live station unit `354` exposed `Creature2.TradeSkillIdStation = 4294967295`, proving the all-tradeskills sentinel can arrive as `uint.MaxValue`; older SQL/reference checks also showed `2147483647`, so validation accepts both sentinels. The client sent `ClientCraftingCraftItemAutoCraft(0x0852)` for the UI `Simple Craft` button, not `ClientCraftingSimpleCraft`; after the sentinel fix, contexts `47..49` completed for schematic `270` at station `354`, emitted `ServerSupplySatchelUpdate(0x0199)` and `ServerCraftingFinish(0x0853)`, and produced item `14838` with persisted stack `12` after three crafts while material `11` dropped to `28`. Treat the repeated `TradeSkillProfession` prerequisite warnings during UI refresh as a separate prerequisite-coverage issue, not an auto-craft blocker.

2026-06-09 live F-008 Tech Tree/talent proof: the observed `+40 Technologist XP (480/450)` result did not indicate a lost XP write. Character `30` persisted `tradeskillXp = 480`, and tier achievement `1481` completed after crossing the tier-2 `450` threshold. The remaining `480/450` display issue is now blocked on packet/order evidence because the packet evidence filter omitted `ServerProfessionUpdate(0x0860)`. The real server-state bug was that completed `TradeskillAchievementReward` rows were not applied: achievement `1470` reward `599` grants one Technologist talent point, and achievement `1471` reward `162` grants one talent point plus schematic `2954` (`Spirovine Extract`). Runtime now applies those table-backed rewards idempotently, backfills already-completed rewards on login/learn, and removes the old simplified 10-talent-point learn/reset seed. A live retry proved the final dependency gap: `TradeskillAchievementReward.tbl` existed in runtime assets but was not marked `[GameData]`, so WorldServer never loaded it and the reward pass saw zero rows. `TradeskillAchievementReward` is now runtime-required game data and the contract tests pin it; focused reward/table verification passed 47/47 and WorldServer build passed. After reconnect/save, character `30` persisted Technologist `talentPoints = 2` and schematic `2954` with XP still `480`. Capture `0x0860` before changing profession-update ordering or Crafting Result progress display behavior.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances, so no fresh labels or exports were added. Cached WildStar64 fragments under `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5` keep this mapped-only: `selected_decompiled.c:13809` registers `0x084B` size `0x18` to `ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload` (`1400a3af0`), `selected_decompiled.c:13807` registers `0x0855` size `0x0c` to `ServerUInt32AndTwoFloats_ReadPayload` (`140081df0`), `selected_decompiled.c:13843` registers `0x0854` size `0x50` to `ServerCraftingCurrentCraft_ReadPayload` (`1400a46b0`), and `selected_decompiled.c:13815` registers `0x056C` size `0x28` to `ServerItemMicrochips_ReadPayload` (`1400a3d50`). `Crafting_HandleServerCraftingCurrentCraft` (`1405e6830`) only applies the decoded current-craft state and dispatches `CraftingUpdateCurrent`; `Inventory_UpdateItemMicrochipsFromWire` (`1403b8540`) / `InventoryItem_ApplyMicrochipsFromWire` (`14056aa20`) only apply item microchip state and dispatch `ItemModified`. Current source paths remain shape-only or finish-only: `Source/NexusForever.Network.World/Message/Model/Crafting/ServerCraftingCurrentCraft.cs`, `Source/NexusForever.Network.World/Message/Model/Crafting/ServerCraftingAuxPackets.cs`, `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs`, and `Source/NexusForever.WorldServer/Network/Message/Handler/Crafting/ClientCraftingCraftHandlers.cs`, with `CraftingSimpleCraftHandlerTests`/`CraftingAdditiveHandlerTests` guarding no blocked current-craft or aux emits. Keep current-craft, aux, and microchip producers blocked until a native producer/send site, apply-table owner/callback, or accepted live crafting/item-replication capture proves enqueue cadence, field population, microchip patch timing, and result-state boundaries.

2026-06-17 F-008 current-craft/aux blocked recheck: The Ghidra MCP bridge was
available, but `mcp__ghidra_mcp.list_instances` returned no running CodeBrowser
instance. `Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe` confirmed
the local `NexusForeverClient64_WildStar64` project and
`Test-DecompileManifest.ps1 -FailOnMismatch` passed for the cached export. The
same cached fragments still provide reader/apply evidence only. Prior F-008
packet evidence under `artifacts\packet_evidence\F008-crafting-live\*.jsonl`
was parsed by real `OpcodeHex` values; it contains `0x0111`, `0x0199`,
`0x0569`, `0x084C`, `0x0852`, `0x0853`, and `0x0856`, but no actual
`0x084B`, `0x0854`, `0x0855`, or `0x056C` records. Local server processes and
`I:\WildStar` are present, but no WildStar client/crafting action is running in
this environment and the blocker harness has no F-008-specific live preset. The
worksheet
`artifacts\blocker_evidence\20260617-223039-F008-crafting-current-craft-aux-recheck`
records the attempted path and negative cases without launching services. Keep
current-craft, aux, discovery/non-success, and microchip producers blocked until
the next artifact is either an interactive Ghidra producer/send-site map or a
live capture with packet evidence for `0x084B`, `0x0854`, `0x0855`, `0x056C`,
`0x0853`, `0x0860`, and item/satchel updates during complex/discovery,
non-success, rune/item-patch, and station mismatch cases.

2026-06-18 CEST F-008 current-craft/aux blocked recheck: A follow-up pass again
found no running Ghidra CodeBrowser, so no new labels or native producer/send
sites were added. `Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe`
confirmed the local `NexusForeverClient64_WildStar64` project, and
`Test-DecompileManifest.ps1 -Targets WildStar64.exe -FailOnMismatch` passed
with `200` selected functions reused from canonical cache. Cached evidence
remains reader/apply-side only for `0x084B`, `0x0855`, `0x0854`, and `0x056C`;
the prior F-008 live packet evidence contains only `0x0111`, `0x0199`,
`0x0569`, `0x084C`, `0x0852`, `0x0853`, and `0x0856`, with no actual
`0x084B`, `0x0854`, `0x0855`, or `0x056C` records. Focused crafting
packet/handler/discovery tests passed `58/58`. The worksheet
`artifacts\blocker_evidence\20260618-023758-F008-crafting-current-craft-aux-recheck`
records the negative cases against synthesizing current-craft, aux, discovery,
service-key names, or microchip producers from reader-only/fixed-recipe
evidence. Keep the slice blocked until the next artifact is an interactive
Ghidra producer/send-site map or an accepted live crafting/item-replication
capture with packet evidence for `0x084B`, `0x0854`, `0x0855`, `0x056C`,
`0x0853`, `0x0860`, and item/satchel updates.

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

2026-06-18 CEST cached-export/source/artifact recheck: no running Ghidra MCP instance was available, so this pass used cached selected fragments, current source/tests, and existing `artifacts\packet_evidence`. Cached readers still prove only packet shape for `0x01B8` (`140085ca0` row variant dispatch), `0x01C1` (`140086410` wide string + 5-bit rows + flag + `uint16` footer), `0x01C4` (`140085fe0` wide string + 5-bit rows + `uint16` footer), and `0x01EF` (`1400a0890` counted notification rows). Source search again found no runtime producer outside opcode/model/test code; runtime chat and ICComm paths emit named structured packets instead, and packet-evidence artifacts had no target opcode/name hits. Worksheet: `artifacts\blocker_evidence\20260618-000929-20260617-F012-chat-aux-recheck`. Keep the cluster mapped-only/producer-blocked until a native producer/apply owner, post-read consumer, callback/table owner, dynamic dispatch proof, or accepted two-client chat/ICComm/cinematic/social capture proves runtime timing and field semantics.

**F-013 realm-info/mail aux: keep `0x05A1` non-emitted until producer timing is proven.**
For a native or accepted capture pass, enable packet evidence for
`ServerRealmInfoResponse` (`0x059D`), `ServerRealmAuxUInt32TripletList`
(`0x05A1`), `ServerMailResult` (`0x05A2`), `ServerMailAvailable` (`0x05A3`),
realm list traffic, and realm-transfer destination traffic. Exercise realm-info
requests, realm-list refresh, mail open/result flows, and realm-transfer
destination UI. Accepted proof for emitting or renaming `0x05A1` must include a
native producer/apply owner, post-read consumer, callback/table owner, or
accepted live capture tying the counted triplet rows to visible realm/mail state
and server logs.

2026-06-18 CEST cached-export/source/artifact recheck: `mcp__ghidra_mcp.list_instances`
found no running Ghidra instance. Cached `WildStar64.exe` registration still
binds `0x05A1` size `0x10` to
`ServerRealmAuxUInt32TripletList_ReadPayload` (`140080b00`), and the dedicated
registration wrapper `14006e125` places it between `ServerRealmInfoResponse`
(`0x059D`) and `ServerMailResult` (`0x05A2`). The reader proves only one
`uint32` count plus counted rows of three `uint32` fields. Current source has
no runtime producer for the aux packet, and existing packet-evidence bundles
contain no target hits for `0x05A1`, `ServerRealmAuxUInt32TripletList`, or
`140080b00`. Focused realm/mail tests passed `54/54`; harness presets passed
`2/2`; content-retail validation reported `31` files and `168,101`
`not_retail_complete` rows; the `WildStar64.exe` manifest check passed with
`200/200` reused fragments. Worksheet:
`artifacts/blocker_evidence/20260618-000112-20260617-F013-realm-info-mail-05A1-recheck`.

**F-015 Duel/PvP observer/reward: close lifecycle smoke, keep observer/reward parity blocked.**
Two clients: `!teleport name Thayd`, initiate duel, accept, decline, forfeit, toggle PvP, then move one player beyond the 120f leash with `!teleport coordinates` and wait 15 seconds. Capture challenge/countdown/leash/cancel-warning logs and client UI. Implement lifecycle/cooldown fixes only if mismatched; observer broadcasts, reward parity, and retail stat behavior remain blocked.

**F-016-F-020 Spell families/procs/loot/reward: implement one fixture per captured spell family.**
For each target spell: `!spell inspect4 <spell4Id>`, `!spell capture4 <spell4Id>`, `!spell diag4 <spell4Id>`, or `!spell capturenext` before a real client cast. Pulse Blast damage is closed for emulator parity by the 2026-06-08 bundle above; repeat it only when validating `ClientCastSpellContinuous` JSON capture coverage or a damage regression. Brutal `4046 -> 4047` post-registration proc dispatch is closed for emulator parity by the 2026-06-09 WorldServer log; repeat only for regression proof or a cleaner holder-specific JSON export. For combat/loot: use Exile combat lanes `!teleport location 51739` or `!teleport location 51740`; for Dominion, prefer `!teleport location 52898` and avoid `53015` unless the current character is in the matching final Dominion tutorial phase. Use `!loot capturenext`, `!reward capturenext`, `!spell procstates`, `!spell procreport`, `!spell procunsupported`. Implement only captured spell/effect/proc fixtures with tests; leave uncaptured effect families blocked.

2026-06-17 spell aux blocked recheck: `mcp__ghidra_mcp.list_instances` found no
running Ghidra instance, so no fresh labels or dynamic apply-table proof were
available. Cached `WildStar64.exe` fragments still bind `0x07FC` to
`ServerSpellCastResult_ReadPayload` (`140094fb0`: leading `uint32`, 18-bit
`Spell4Id`, 9-bit `CastResult`), `0x080F`/`0x0810` to
`ServerSpellUInt32TripletList_ReadPayload` (`140095da0`) plus
`ServerSpellUInt32TripletListRow_ReadPayload` (`140080bf0`), and `0x0812` to
`ServerSpellFourUInt32_ReadPayload` (`14007fef0`). Source search still finds
the triplet/four-uint packets only in packet models/tests, and local
`artifacts\packet_evidence` contains no target rows. The worksheet
`artifacts\blocker_evidence\20260617-232253-20260617-232300-F016-spell-aux-recheck`
records the missing evidence path. Keep `ServerSpellCastResult.Unknown0`
neutral and keep `0x080F`/`0x0810`/`0x0812` non-emitted until a native
apply/producer path, callback/table owner, dynamic dispatch proof, or accepted
spell packet capture proves semantics and timing.

**F-021 Action Set/LAS: implement preflight/result mapping only if UI plus logs prove it.**
Use `!spell add <spell4BaseId> [tier]`, `!spell resetcooldown`, then change LAS slots, tiers, spec, and AMP state in normal, dead, combat, and PvP states. Capture UI result and any server logs. `UpdateSpellInProgress` and async transaction semantics remain blocked unless a real in-progress transition is observed.

2026-06-18 CEST recheck: Ghidra MCP was reachable but no running instance was
available, so this pass used cached selected fragments, current source/tests,
and artifact searches. `ActionSet_CheckUpdateSpellInProgress` (`1403bb8d0`)
checks a client-local `GameFormula` `0x41e` marker in the spell/update list;
`Lua_ActionSetLib_RequestActionSetChanges` (`1407580e0`) returns result `0x26`
before `ActionSet_SendPendingActionSetChanges` (`1403bb480`) sends `0x00B1`;
`Lua_AbilityBook_UpdateSpellTier` (`140748390`) uses the same guard; and
`Lua_AbilityBook_ClearCachedLASUpdates` (`140748630`) clears `entity+0x1458`
and resets `entity+0x6ddc`. Current server handlers still validate then mutate
LAS/AMP state synchronously, and no local packet/live artifact proves a real
in-progress transition. Worksheet:
`artifacts/blocker_evidence/20260618-002935-20260617-F021-las-update-in-progress-recheck`.
Do not add a synthetic server `UpdateSpellInProgress` gate until a live
WildStar build 16042 client/server capture or native server evidence proves the
transaction start, clear, packet timing, and result surface.

**F-030 Realm transfer/PTR: keep `0x03EF` and PTR queue/copy state blocked until producer proof.**
For a native or accepted capture pass, exercise `ClientGetRealmTransferDestinations`, `ClientRealmTransfer`, `ClientInitiatePTRCharacterCopy`, and `ClientPtrCopy` from character select with packet evidence enabled for realm-transfer/PTR response clusters. Accepted proof for widening behavior must include a native producer/apply path, callback/table owner, dynamic dispatch proof, server realm/catalog artifact, or captured packet sequence tying the destination/PTR fields to visible UI state, queue state, transfer result, and handoff timing.

2026-06-09 cached-export/source recheck: cached `WildStar64.exe` fragment `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/14007d790.fragment.c` keeps `ServerRealmTransferDestinationsAux` (`0x03EF`) mapped-only. `ServerRealmTransferDestinationsAux_ReadPayload` (`14007d790`) reads one `uint32`, a `uint32` byte count, allocates that count, and copies raw bytes into a pointer-backed buffer. `Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x03EF` size `0x10` at `selected_decompiled.c:12787` with null write/handler slots, and the same reader is reused by `0x01B4` at `selected_decompiled.c:13804`; selected xrefs are label-only and selected call edges stay reader-local. Current source emits `ServerTransferDestinationRealmList` from `Source/NexusForever.WorldServer/Network/Message/Handler/Misc/ClientGetRealmTransferDestinationsHandler.cs`, keeps `ServerRealmTransferDestinationsAux` only in `Source/NexusForever.Network.World/Message/Model/ServerClusterAuxPackets.cs` plus `PacketPlaceholderNamingTests`, and keeps `ClientRealmTransferHandler` conservative for invalid/offline/online targets. Keep `0x03EF` non-emitted and raw payload neutral until producer evidence explains destination semantics and transfer/PTR timing.

2026-06-17 realm-transfer/PTR blocked recheck: `mcp__ghidra_mcp.list_instances`
again found no running Ghidra instance. Cached fragments still provide only
reader/sender/consumer evidence: `14007d790` maps the `0x03EF` raw byte
envelope, `140022270` maps `0x06E7` selected-character send plus `0x0244`,
`14063f540` and `140707d80` map zero-byte `0x06E8` sends, and `140020ea0` maps
client consumption of `0x06EA` as Lua `PTRCharacterCopyQueued`. Registration in
`14006c290.fragment.c` still binds `0x06EA` to `ServerEmpty_ReadPayload`; the
adjacent complex writer `14007dd40` remains `0x0592` evidence, not `0x06EA`.
Current source keeps `ServerRealmTransferDestinationsAux` and
`ServerPtrCharacterCopyQueued` model/test-only, emits only the empty structured
realm-transfer destination list, and keeps PTR-copy request handlers
diagnostic-only. Existing `artifacts\packet_evidence` contained no target
realm/PTR rows. The worksheet
`artifacts\blocker_evidence\20260617-232905-20260617-234000-F030-realm-ptr-recheck`
records the missing evidence path. Keep `0x03EF`/`0x06EA` non-emitted and
transfer/copy mutation disabled until a native producer/apply path,
callback/table owner, dynamic dispatch proof, server realm/catalog artifact, or
accepted character-select packet capture proves destination payload semantics,
queue timing, and copy mutation.

**F-031 Madame Fay/Fortune: local tests validate session/payout boundaries and UI probability transport; per-item retail weights remain blocked.**
Use Fortune UI with enough `FortuneCoin`/account currency, capture logs and screenshots for cost/result/session behavior, reload persistence (`account_fortune_session`), and displayed `fProbability` values. Emulator now sends `ServerFortuneRewards.RewardItemProbabilities` from rarity-tier weights; `FORTUNE_WEIGHT_AUDIT.md` confirms `AccountItem.tbl` has no weight column, F-007 `RewardRotation*` schedules are rejected as Fortune active-rotation evidence, and no local live Fortune play artifact was found. Exact per-account-item retail weights still need retail `ServerFortuneRewards` capture or storefront-server catalog evidence.

2026-06-09 cached-export/source recheck: Ghidra MCP discovery found no running instances. Cached WildStar64 fragments still show `ServerFortuneRewards` as item2/money/probability transport, `Fortune_ApplyRewards` as client-side cache application of server-provided arrays, `FortunesLib.GetFortunesLootList` as UI exposure of cached probabilities, and `FortuneNode_ApplyServerFortunePackets` as the `0x03CF`-`0x03D2` dispatcher. Source still emits `ServerFortuneRewards` from emulator rarity-tier `FortuneRewardPool`; no retail active-rotation source, per-item weight table, or storefront-server catalog proof surfaced.

2026-06-17 blocked recheck: `mcp__ghidra_mcp.list_instances` again returned no running Ghidra instance, so the pass used cached `WildStar64.exe` fragments, current source/tests, and `FORTUNE_WEIGHT_AUDIT.md`. `ServerFortuneRewards_ReadPayload` (`140081f60`) remains item2/money/probability transport only; `Fortune_ApplyRewards` (`1407292a0`) copies server-provided arrays into UI state; `FortunesLib_GetFortunesLootList` (`140766370`) shows `fProbability = serverFloat * 100`; `FortuneNode_ApplyServerFortunePackets` (`1404d60f0`) dispatches `0x03CF`-`0x03D2`; and `ServerFortuneCards_ReadPayload` (`1400a0b10`) remains card-state transport evidence. The LWS-066 worksheet `artifacts/blocker_evidence/20260617-225039-F031-fortune-retail-weights-recheck` records the missing retail packet/catalog path and negative cases. Keep exact item probabilities, money reward arrays, and active rotation blocked until a retail `ServerFortuneRewards` capture, storefront-server catalog dump, or native/server producer artifact proves item ids and probabilities.

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
