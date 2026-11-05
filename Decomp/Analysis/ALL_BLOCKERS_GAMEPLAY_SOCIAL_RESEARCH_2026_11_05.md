# Remaining gameplay and social blockers: authored-source research

Research date: 5 November 2026. This note covers crafting, transport, guild
recruitment/perks/warplots, ICComm/chat, duels, LAS, and matching. The complete
project inventory is maintained separately in
[ALL_BLOCKERS_INVENTORY_2026_11_05.md](ALL_BLOCKERS_INVENTORY_2026_11_05.md).
This is a research and next-work record; it does not claim that these gameplay
families are retail complete.

The useful new distinctions are concrete: the authored LAS UI handles an error
after its initial request has succeeded and closed the window; upstream's
recruitment author explicitly says Carbine exposed the data without shipping a
UI; the warparty UI requests a token inventory before opening; and pinned
ICComm registration pairs supply narrower callback targets. These findings
reduce the investigation scope. They do not supply missing retail reward,
entitlement, producer, or transaction rules.

## Evidence ownership and version boundary

The Lua sources below contain NCsoft/Carbine authorship headers, preserved by
the [Zod- addon mirror at immutable revision
aa608d0b837ff8037eb8509123d8e3ed4efcb9e4](https://github.com/Zod-/Wildstar-Carbine-Addons/tree/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live).
The mirror is not an authenticated publisher repository. Its files are
historical authored consumers, not build16042 server implementation or traffic.
The older API help is likewise a preserved original API surface, not a complete
final-build contract. A Lua event or a table getter cannot, by itself, assign an
opcode, send order, recipient set, or persistent server policy.

Native addresses in the existing-seed column below come from the current
[function labels](function_labels.csv) and linked native reports. They are not
new decompilation performed for this note. The separately identified PE scan
below is ordinary read-only byte inspection of the pinned executable; it did
not access the shared REA/Ghidra session.

## Family-level result and next input

**Source-owned** means work can be investigated or implemented against already
owned state and an explicit local invariant. It does not mean an unobserved
retail policy should be invented. **Native/data** means the existing client or
actual table corpus remains a useful next input. **External** means accepted
traffic, a server-owned catalog/rule, or physical qualification is still needed
if the native/table paths cannot establish the contract.

| Family | Current native/static seed | Authored finding and concrete next gate |
| --- | --- | --- |
| Crafting discovery, complex craft, microchip/aux | `140399780` selects complex `084F` or simple `0850`; `1400a5d70` writes the complex payload. Finish `1405e6690` owns the known hot/cold event; current-state `1405e6830` owns `0854`. `084B/0855` default on the inspected dispatcher. | UI separates current preview, completion request, learned schematic, and hot/cold presentation. **Native/data:** bind named `GetCurrentCraft` fields and completion inputs to actual row/rule state. **Source-owned:** settlement/failure/duplicate invariants can be checked separately. **Still missing:** actual discovery roll/unlock rules, auxiliary owner, finish/current/`0860` order and `056C` patch cadence. A float pair does not establish discovery coordinates. |
| Taxi, rapid transport, service tokens, vehicles | `1404acfa0` sends `00FF`; `1403993c0` sends `0141`. `1404adc50`, `1404af440`, `1404af1e0` build costs; `1404ad310`/`1404ad660` validate/build route chains. | UI distinguishes a taxi-unit purchase from rapid-transport confirmation and displays primary/alternate prices. **Native/data:** trace the exact named availability/cost branches and actual TaxiNode/TaxiRoute/GameFormula rows. **Source-owned:** route/cost/actor continuity and debit-once checks need no invented retail values. **Still missing:** token-bypass predicate, authoritative snapshots and embark/completion/seat/passenger lifecycle, plus physical journey qualification. |
| Guild recruitment | `14069e5c0` wraps `Game.RecruitmentGuild.GetDetailedGuildInfo`; `140584840` sends `076E`. Current subscribe/detail handlers only log. | Original upstream PR536 says Carbine exposed recruitment data to Lua without supplying a UI. **Source-owned:** design bounded subscription lifetime and persistence/readback from real guild profile state. **Native/data:** inspect API getters, subscription callback and list/detail field ownership. **Still missing:** authoritative profile backing, eligible listings and publication cadence; an absent official UI is not evidence that the protocol is unused. |
| Guild perks, boss tokens, warplots | `1403991b0` sends `094F` guild-boss-token cast; table loaders include GuildPerk and HousingWarplotBossToken. Existing prerequisite checks include GuildPerk type185 and WarplotCirclePerk type188. | Perk UI consumes costs/prerequisites from getters; token UI explicitly requests inventory and refreshes on its update event. **Source-owned:** actual-row prerequisite evaluation and atomic spend/consume/recovery can be developed once the positive operation is mapped. **Native/data:** getter/cast/table joins below are next. **Still missing:** activation expiry/reapplication, token consumption/spawn readiness and warplot state/reward policy. |
| ICComm entitlement, restore and chat auxiliaries | Join `0546` writer `1400877a0`; message `054B` writer `1400875e0`; `0549` shares the raw-u64 writer. Chat auxiliary readers include `01C1`, `01C4`, `01EF`. | Old help exposes `MissingEntitlement` but no entitlement ID; final-build binding targets are now narrower. Ordinary ChatLog saved settings are not ICComm membership restoration. **Source-owned:** current transient membership/stale-recipient cleanup and actor continuity are directly testable. **Still missing:** named entitlement/quota, durable-channel policy, throttle/delivery/leave/reconnect order and auxiliary producer ownership. |
| Duels, observers and PvP rewards | `140701120` initiate `00EC`; `140701150` accept `00E8`; `140701180` decline `00E9`; `1407011b0` forfeit `00EA`; `140701290` ignore-duel `0170`. | Authored callbacks expose opponent, acceptance countdown, out-of-area timer and cancel warning. **Source-owned:** terminal/cancel/duplicate and participant lifecycle invariants can be checked. **Still missing:** observer fanout, forced-map eligibility, reward/stat producer and match-specific score rules. Local chat/countdown UI is not observer broadcast or reward proof. |
| LAS asynchronous update | `1407580e0` is `RequestActionSetChanges`; `1403bb480` sends pending changes; `1403bb8d0` checks update-in-progress; `140748390` updates cached tiers. | Authored UI explicitly accepts a synchronous result, closes, and can reopen on later `ActionSetError`. **Source-owned:** probe current handler mutation/publication under failure, retry and actor replacement; trace the actual persistence owner before calling this a defect. **Native:** follow the in-progress clear/error/success transitions. **Still missing:** exact retail asynchronous completion/order; passing request validation alone cannot close it. |
| Matching backfill, nonzero raid state and diagnostics `062A/0634` | Replacement sender `14076aa30` emits `05D5`; stop uses `0602`. `0718` has a shared row reader but lacks a proven standalone nonzero producer. `062A/0634` remain shared-u32 request registrations without sender intent. | Authored `MatchingGameLib` exposes separate replacement-state, current-match and transfer controls. **Native:** getter/state-writer pairs below narrow the search. **Source-owned:** membership, recipient and cancellation invariants are separate from retail queue-anchor/merge policy. **Still missing:** accepted replacement sequencing/merge, nonzero `0718`, diagnostic senders/meaning. Keep their public handlers diagnostic until that input exists. |

The current boundaries in this table are reconciled with the
[all-family inventory](ALL_BLOCKERS_INVENTORY_2026_11_05.md),
[crafting aux report](CRAFTING_AUX_WEB_CORROBORATION_2026_11_05.md),
[matching status](MATCHING_IMPLEMENTATION_STATUS.md), and
[latest native contract report](DEEPER_ROADMAP_CONTRACTS_2026_11_05.md).
Previously resolved station keys/cache reset, role-mask conversion, neighborhood
consumer mapping, taxi/mount fixes and guild-bank/recovery work are not new
results of this pass.

## Crafting: the authored UI separates four lifecycle edges

The coordinate UI registers `CraftingUpdateCurrent`, `CraftingSchematicLearned`
and `CraftingDiscoveryHotCold` separately. Its current-craft consumers use
`nSchematicId`, `nSubSchematicId`, `fVectorX`, `fVectorY` and `nAdditiveCount`.
Before `CompleteCraft()` it stages a pending marker result; the hot/cold callback
takes `(eHotCold, eDirection)`, returns for `Success`, and otherwise annotates
the pending marker. This is concrete consumer structure; neither its drawing
radius nor its marker logic supplies the server's discovery probability,
unlock rule or exact packet ordering.
[CraftingGrid.lua:142-144,599-693](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/CraftingGrid/CraftingGrid.lua#L599-L693)

The circuit UI passes `self.tSocketInfo` into `GetCurrentCraft`,
`GetPreviewInfo` and ultimately `CompleteCraft(self.tSocketInfo)`. It separately
handles interruption and station close. These names are better native seeds
than treating any crafting-adjacent aux packet as a microchip operation.
[Crafting.lua:100-115,815-911](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/Crafting/Crafting.lua#L815-L911)

The existing [crafting aux report](CRAFTING_AUX_WEB_CORROBORATION_2026_11_05.md)
already pins `GetCurrentCraft -> 14059b2f0`, `CompleteCraft -> 14059bb60`,
`GetPreviewInfo -> 14059ada0`, and the three named station APIs. It also records
the historical `GetCurrentOverchargeInfo` mismatch. Follow these exact callbacks
and the known `0853/0854` consumers; do not resurrect an unproved standalone
microchip-install opcode or name neutral `084B/0855` fields from UI geometry.
The [aux taxonomy](ENTITY_AUX_DECODE_ROADMAP.md) retains `056C` item modification
and `ItemModified` as a separate lifecycle/cadence question.

## Transport: cost display and purchase navigation have different owners

`TaxiMap` registers `FlightPathUpdate`, `InvokeTaxiWindow`,
`InvokeShuttlePrompt` and close/world/combat callbacks. With a taxi unit it calls
`unitTaxi:PurchaseFlightPath(idNode)`; without one, confirmation action data uses
`CodeEnumConfirmButtonType.RapidTransport` and the node ID. Its prices come from
`monCostRapidTransport` and optional `monAltCostRapidTransport`.
[TaxiMap.lua:60-72,594-627,794-810](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/TaxiMap/TaxiMap.lua#L594-L627)

`StorefrontLib.CodeEnumStoreLink.ServiceTokens` is used to open the store when
the user cannot afford the trip. That proves purchase navigation, not a free
travel entitlement, cooldown bypass, token debit preference or server acceptance
rule. The pinned cost helpers in [function_labels.csv](function_labels.csv)
provide the stronger next path: formula `0x51B` selects pricing branches;
service-token cost uses TaxiNode tier and formula `0x51E`; the credit branch
also uses distance and formula `0x3B8`. Actual rows and the branch predicates
must be inspected before selecting values. These are bounded local-data/native
questions while the corpus is available, rather than automatically external
blockers.

Passenger/seat/deployable authority is not in this taxi UI. Existing Source
fixes are recorded in [taxi/mount](TAXI_MOUNT_UPSTREAM_FIXES.md) and
[movement/platform](MOVEMENT_PLATFORM_UPSTREAM_FIXES.md) notes. They do not
substitute for a real embark-to-arrival or vehicle/controller lifecycle witness.

## Guild recruitment: the upstream author explains the missing UI

[Upstream PR536](https://github.com/NexusForever/NexusForever/pull/536), authored
13 November 2025 and merged 10 May 2026, adds recruitment messages and states
that their data is available to Lua while Carbine supplied no interface using
it. The reviewed PR head is
`1ca3c86b7c203b17afaa188f953c4e0bca21edec`. This is a protocol author's account
of the implementation surface, not independent retail traffic. Repeating a
search for a missing shipped recruitment screen is lower yield than tracing
`Game.RecruitmentGuild` registrations and native state writers.

Current [subscribe](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientRecruitmentGuildSubscribeHandler.cs)
and [details](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientRecruitmentGuildGetDetailedGuildInfoHandler.cs)
handlers only log. The
[subscribe model](../../Source/NexusForever.Network.World/Message/Model/Guild/ClientRecruitmentGuildSubscribe.cs)
reads one Boolean. Existing
[detail](../../Source/NexusForever.Network.World/Message/Model/Guild/ServerRecruitmentGuildDetails.cs)
and [guild-info](../../Source/NexusForever.Network.World/Message/Model/Guild/RecruitmentGuildInfo.cs)
models name description, creation age, tax, minimum level, demands, identity and
recruiters. These make a real guild-backed subscription/readback design
reviewable. They do not justify fabricated listings, profile defaults or
unobserved refresh cadence. Native getter/setter and persisted profile ownership
are the concrete next inputs.

## Guild perks and warparty tokens: use row data and the inventory request

The perk UI calls `guild:GetPerks()`, refreshes a particular entry through
`GetPerk(idPerk)`, and issues `PurchasePerk(id)` or `ActivatePerk(id)`. It consumes
`arRequiredIds`, `achRequired`, `bIsUnlocked`, `fDurationInDays` and
`nActivateInfluenceCost`; cost and prerequisite values come from data returned
by the client API. The authored snapshot registers `GuildPerkUnlocked`,
`GuildPerkActivated`, and a literal `"GuildPerkDeactivated "` with a trailing
space. That last spelling is a snapshot quirk, not proof of the native event
name or a reason to reproduce it in server code.
[GuildPerks.lua:67-69,104,318-340,543-580,916-936](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/GuildContentPerks/GuildPerks.lua#L543-L580)

The local [GuildPerk table model](../../Source/NexusForever.GameTable/Model/GuildPerkEntry.cs)
already exposes purchase/activation influence costs, activation Spell4 ID, up to
three required perks and required achievement. Actual table rows plus native
getters can settle many values without a guide or arbitrary defaults. A
duration rendered by the UI does not establish server expiry persistence,
offline time handling or exactly-once influence settlement. The current
[activate handler](../../Source/NexusForever.WorldServer/Network/Message/Handler/Guild/ClientGuildPerkActivateHandler.cs)
delegates to the guild-operation manager; that registration is not itself
activation behavior proof.

`WarpartyBattle:OnOpen(guildWarparty)` requests `guildWarparty:ListBossTokens()`
when inventory has not arrived, then refreshes on `WarPartyBossTokensUpdated`.
`GetBossTokens()` yields entries with `nItemId` and `nCount`, rendered through
`Item.GetDataFromId`. `WarPartyBattleOpen/Close` and
`WarplotBattleStateChanged` are independent events.
[WarpartyBattle.lua:77-83,109-119,155-164,194-201](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/Warparty/WarpartyBattle.lua#L109-L119)

This gives a request/cache/update chain worth tracing, rather than assuming the
boss-token cast request also lists the inventory. Current
[boss-token handler](../../Source/NexusForever.WorldServer/Network/Message/Handler/Spell/ClientCastGuildBossTokenHandler.cs)
returns `BossTokenNotReady`; it does not consume or spawn. The safe next positive
input is an actual token Item2/table-to-cast mapping with readiness, ownership,
consume-once and spawn-failure rules. The authored inventory display establishes
none of those transaction rules and supplies no warplot reward policy.

## ICComm and chat: entitlement identity and restoration remain distinct

Subsequent [native lifecycle mapping](ICCOMM_NATIVE_LIFECYCLE_2026_11_05.md)
resolves the typed JoinChannel owner,0546 sender, exact pending type/name key,
callback roles and selected0547/0548/054A transitions. The
[authored-source follow-through](ICCOMM_WEB_CORROBORATION_2026_11_05.md) also
separates API eras and concrete addon callers. Earlier candidate/next-work
statements below are the research checkpoint; entitlement and server policy
remain unresolved, while response-name correlation is now concrete Source work.

The original API help lists `ICCommJoinResult.MissingEntitlement`, along with
`NoGuild`, `NoGroup`, `TooManyChannels`, `BadName`, `Join` and `Left`. It gives an
old `JoinChannel(strChannel,strFunction,nLuaEventHandler)` signature and
`SendMessage`, `SendPrivateMessage`, `SetControlFunction` names. No numeric
entitlement, quota, throttle or restoration contract is provided. This 2015
help must not be silently combined with the final native channel-type/guild-ID
writer or used to select an entitlement constant.
[ICCommLib.htm:8-39](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Classes/ICCommLib.htm#L8-L39)

Current [ICCommManager](../../Source/NexusForever.Game/ICComm/ICCommManager.cs)
stores channel IDs/keys/member objects in dictionaries and prunes lost group,
guild and offline membership. Its
[tests](../../Source/NexusForever.Game.Tests/ICComm/ICCommManagerTests.cs)
cover transient cleanup, directed delivery and stale group/guild members.
Those are source-owned behavioral tests; this pass did not execute them.
The [entitlement enum](../../Source/NexusForever.Game.Static/Entity/EntitlementType.cs)
names EconomyParticipation15 and FullSocialParticipation17 but no ICComm-specific
entry. Neither name alone authorizes a new ICComm gate or a blanket entitlement
grant. The native `MissingEntitlement` decision/caller is the useful next input.

Ordinary `ChatLog:OnSave`/`OnRestore` handles character-level presentation data;
`ChatSystemLib.JoinChannel` is a separate user action. This does not prove
durable ICComm channel IDs or membership replay. `OnChatMessage(channel,tMessage)`
documents `bAutoResponse`, `bGM`, `bSelf`, sender/realm, presence,
`arMessageSegments`, source unit, bubble/cross-faction flags and report ID.
These provide native getter/constructor leads for chat auxiliaries, but do not
identify `01C1/01C4/01EF` as their owners or prove their producers.
[ChatLog.lua:172-280,843-844,2373](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/ChatLog/ChatLog.lua#L843-L844)

## Duels and LAS: consumer behavior narrows the lifecycle

The duel UI's signatures are `DuelStateChanged(eNewState,unitOpponent)`,
`DuelAccepted(fCountdownTime)`, `DuelLeftArea(fTimeRemaining)`, and
`DuelCancelWarning()`. It invokes `AcceptDuel`/`DeclineDuel`; acceptance starts
a countdown and local UI chat presentation. This narrows event arguments but
does not reveal observer recipients, participant map policy, reward eligibility
or battleground score/stat rules.
[Duel.lua:39-42,51-60,67-139](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/MatchMaker/Duel.lua#L67-L139)

The LAS UI builds twelve action IDs, calls
`ActionSetLib.RequestActionSetChanges`, handles the immediate `eResult`, and
closes after success. Separately it subscribes to `AbilityBookChange`,
`SpecChanged` and `ActionSetError`. Its error handler can rebuild the UI after
the initial successful close. This is explicit authored evidence of a delayed
error path, not merely an inference from event names. Exiting also clears
cached LAS and AMP edits.
[AbilitiesBuilder.lua:51-53,252-267,285-301,813-823](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/Abilities/AbilitiesBuilder.lua#L813-L823)

At this audit's parent checkpoint, the
[request handler](../../Source/NexusForever.WorldServer/Network/Message/Handler/Spell/ClientRequestActionSetChangesHandler.cs)
validates, mutates shortcuts/tiers/AMPs, then queues spell, cache, item, action-set
and point notices. This method has no explicit save/commit receipt boundary.
That observation is a probe target, not a reproduced persistence defect:
the owning managers may save later. A controlled rejected-save, retry and
actor-replacement exercise can establish whether durable state and public
notices remain coherent. This source-owned question can progress before any
new retail capture. Native `UpdateSpellInProgress` set/clear and failure/result
writers remain necessary for exact retail asynchronous behavior.

Later implementation: [confirmed public LAS replies](ACTION_SET_PUBLICATION.md)
executes the rejected-save/retry/actor probe and fixes its three reproduced
reply failures plus a reviewed no-op acknowledgement defect. Sixteen focused
cases and shared controls pass. This supersedes the proposed public reply probe;
broader initialized AMP/combat timing and the native async/retail gates remain.

## Matching: separate current-match state from queue rows and diagnostics

The authored UI subscribes to `MatchLookingForReplacements` and
`MatchStoppedLookingForReplacements`; both update in-match controls and cancel
role-check display, while stop also hides pending information. Buttons call
`MatchingGameLib.LookForReplacements()` and `StopLookingForReplacements()`.
Current-match checks use `MatchingGameLib.GetQueueEntry()` and
`IsLookingForReplacements()`. Transfer is a separate action guarded by
`IsInGameInstance()` before `TransferIntoMatch()`.
[MatchMaker.lua:368-369,801-813,860-868,3693-3708](https://github.com/Zod-/Wildstar-Carbine-Addons/blob/aa608d0b837ff8037eb8509123d8e3ed4efcb9e4/Live/MatchMaker/MatchMaker.lua#L3693-L3708)

By comparison the queue window reads `MatchMakingLib.GetQueuedEntries`, using
the differently named library. Follow both registrations/state owners rather
than assigning every queue-looking datum to `0718` or the two diagnostic
requests. The [matching status](MATCHING_IMPLEMENTATION_STATUS.md) already
records mapped replacement requests with production fill rolled back, a
compatibility-zero `0718` emission, and log-only `062A/0634`. The November
[native report](DEEPER_ROADMAP_CONTRACTS_2026_11_05.md) narrows instruction-scan
false hits without finding their gameplay senders. This pass found no authored
source tying either numeric request to a specific UI call. An opposite-direction
`0x0634` handler is not C2S intent evidence.

## Fresh pinned PE callback and string seeds

Read-only inspection used
`C:\GIT\_archive\cluster-3d\NexusForever\Decomp\Client64\WildStar64.exe`,
SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The scan mapped file offsets through the PE section headers, searched the
NUL-terminated API name, located little-endian 64-bit name pointers, and retained
adjacent function pointers only when they fell in `.text`. These are candidate
registration pairs, not inspected callback behavior. An owner registration and
instructions must still corroborate ambiguous entries.

| Exact API literal | Name VA | Pair VA | Adjacent function VA | Pair bytes, little-endian |
| --- | --- | --- | --- | --- |
| `JoinChannel`, candidate A | `140b46cf8` | `140b74730` | `140730c30` | `f86cb44001000000300c734001000000` |
| `JoinChannel`, candidate B | `140b4dc78` | `140b76210` | `140764050` | `78dcb440010000005040764001000000` |
| `SendMessage` | `140b35818` | `140c5d730` | `1406a47d0` | `1858b34001000000d0476a4001000000` |
| `SetJoinResultFunction` | `140b357f8` | `140c5d750` | `1406a4a00` | `f857b34001000000004a6a4001000000` |
| `SetReceivedMessageFunction` | `140b357a0` | `140c5d760` | `1406a4ab0` | `a057b34001000000b04a6a4001000000` |
| `SetSendMessageResultFunction` | `140b357c0` | `140c5d770` | `1406a4b60` | `c057b34001000000604b6a4001000000` |
| `GetQueueEntry` | `140b4e4e8` | `140b763b0` | `14076a080` | `e8e4b4400100000080a0764001000000` |
| `IsLookingForReplacements` | `140b4e458` | `140b76410` | `14076a840` | `58e4b4400100000040a8764001000000` |
| `StopLookingForReplacements` | `140b4e5e0` | `140b76490` | `14076abb0` | `e0e5b44001000000b0ab764001000000` |

In particular, the exact pair names `1406a4a00` as
`SetJoinResultFunction`, improving on an older generic callback label; the pair
alone does not prove its dispatch logic. The two `JoinChannel` pairs must not
be conflated: ordinary chat and ICComm are separate likely owners until their
registration contexts are inspected.

Guild literals also occur in this executable: `PurchasePerk` at `140b344b0`,
`ActivatePerk` at `140b344c0`, `GetPerks` at `140b344d0`, `GetPerk` at
`140b344e0`, `GetBossTokens` at `140b34148`, and `ListBossTokens` at
`140b34178`. This bounded adjacent-pair scan recovered no function pair for
them. Trace instruction references/registration construction; do not claim
that these APIs are absent because they lack that particular table layout.

## Immutable web-source provenance

All Lua hashes below are SHA256 of the raw HTTP response bytes fetched from
the pinned mirror revision, with no newline normalization. The last-change
metadata comes from GitHub's commit history restricted to the exact path and
pinned revision. A mirror commit's build annotation is a historical bound, not
proof that every file was extracted from build16042.

| File under `Live/` | Last change / declared build | Raw SHA256 |
| --- | --- | --- |
| `CraftingGrid/CraftingGrid.lua` | `d37c0399f1d481faf23c027cced44b05a07ab3da`, 2017-03-22, 15134/API15 copied from PTR | `5bf348cbf4434e76f312ec88737b7bf2a1c8842f4a0f0a88eade0d7184534025` |
| `Crafting/Crafting.lua` | `5492ad2de7a597abd951043722c3f568e677789b`, 2016-07-29, 13938 | `fda549ecf88cfa4c93250cb3e803c9d9c6f9bb4c91665d5bbaf2715c43f54799` |
| `TaxiMap/TaxiMap.lua` | Same `5492ad2...`, 2016-07-29, 13938 | `7e08d012336f8915ce8821d9b28bf6dbf5e6ce5986b42cdc89050919c7b52c3d` |
| `GuildContentPerks/GuildPerks.lua` | Same `5492ad2...`, 2016-07-29, 13938 | `24574aaa7880f82430cea425700c674e2510309480dc960c0600b757c0a127be` |
| `Warparty/WarpartyBattle.lua` | Same `5492ad2...`, 2016-07-29, 13938 | `81d9ac7f0a56cc40add6d382ec575948692fb26ddf53c22c424509f3044a1752` |
| `MatchMaker/Duel.lua` | `4d3543b621c3a92c3e398d2390867967d5757755`, 2017-04-05, 15264 | `13b134c6d03500d0d30fbb5c6bfb6b364282e4bea5b03f87d696df706b410854` |
| `Abilities/AbilitiesBuilder.lua` | `dbc67089152bf7078a1516a599ff55c6412db1a4`, 2017-03-22, 15197 | `b64336e43be7fff15d8a78505ba975803d8ac3ce799b08303d43cafb0af9130d` |
| `ChatLog/ChatLog.lua` | `0827880c8a820b5cbb3f1dff3e3888c9ab9f0263`, 2017-09-12, 15736/API16 | `67782b8ecc23f7bda75c887d73f9adbc8e545f4ea503d373fd3ee560520c7d78` |
| `MatchMaker/MatchMaker.lua` | `8b5297088f58a1063d4c03e4447673bf3521d268`, 2017-06-08, 15481 | `7e8637829d2f3c88369c0e832e42e1087cf170ffc776ec5dc329dc253cdd6de7` |

The original-help source is
[NASADocs revision 5a154a6f1ec384ef852f216ed07f5ae0ed174126](https://github.com/wildstarnasa/NASADocs/tree/5a154a6f1ec384ef852f216ed07f5ae0ed174126),
19 June 2015. Its `ICCommLib.htm` raw SHA256 is
`1cc58e45c171592f21c12a7017426193cab31c596f3fbd2f0ff30f2ff8b87bce`.
The upstream recruitment PR dates/head were checked through the
[original GitHub PR API](https://api.github.com/repos/NexusForever/NexusForever/pulls/536).

## Search boundary and verification

- Exact API/event searches and the pinned original-authored addon/help source
  were used. General web queries for the narrow source names mostly returned
  unrelated or secondary material; no guide values were promoted to retail
  probabilities, entitlement IDs, token rules or packet timing.
- The publisher Steam announcement feed was inspected for relevant historical
  context. It supplied no accepted craft, ICComm, diagnostic-request, backfill
  or vehicle-state transcript, and no final-build rule used by this note.
  No broad claim of exhaustive archive coverage follows from that negative.
- Exact `062A/0634` searches supplied no original client-authored sender
  association. Opposite-direction handlers and registration-only hits were
  excluded. Existing role/station/neighborhood results were treated as prior
  evidence, not counted again as new unblocks.
- Existing Source was inspected read-only to distinguish an implementation
  opportunity from a missing external rule. LAS save-failure behavior was not
  reproduced; ICComm test names were inspected, not executed. No application
  build, provider, restart, broker, socket or native UI qualification is claimed.
- Only this Markdown note was written. No Source, archived client, database,
  shared REA/Ghidra session, runtime, client launch, Git commit or push changed.
  Local Markdown targets and whitespace were checked after writing.

The strongest next native paths are the ICComm binding/entitlement decision,
LAS asynchronous state transitions, guild getter/token-inventory registrations,
and the distinct matching current-match state writers. Source-owned recovery
and stale-actor probes can proceed alongside those inspections; missing retail
weights, reward policy and producer timing remain explicit gates.
