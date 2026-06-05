# Placeholder rename tracker

Tracks `Unknown*` / opaque `Data*` placeholders in `Source/**` (excluding EF migration history).
Use the evidence ladder from `CONTINUATION_GUIDE.md`: Observed -> Correlated -> Mapped -> Verified -> Implemented.

**Inventory (2026-05-23):** ~761 `Unknown*` token matches in 176 `Source/**/*.cs` files (ripgrep); ~147 distinct symbol names; ~205 enum members (64 in `PrerequisiteType` alone).

## Initiative status - ACTIVE (2026-06-05 pass 132)

Pass 132 rechecked `ServerSpellCastResult.Unknown0`, the leading `uint32` in
server opcode `0x07FC`. Question: does the current cache or MCP bridge prove the
field is the echoed client cast context token? Current support remains Mapped
wire / Blocked semantics: native reader `ServerSpellCastResult_ReadPayload`
(`WildStar64.exe` `140094fb0`) reads `uint32`, `18`-bit `Spell4Id`, and
`9`-bit `CastResult`, but no client apply owner or live echo capture is mapped.

MCP workflow hints resolved project `NexusForeverClient64_WildStar64`, but the
bridge only exposed one socket as project `unknown`; `connect_instance` by the
real project name and by `unknown` timed out / reported no match. The cache
recheck therefore stayed on source-controlled exports: the reader fragment shows
only the three field reads, `selected_call_edges.csv` shows only internal
bit-reader calls plus self jumps, and `selected_xrefs.csv` contributes no
apply/consumer edge for the reader. The selected opcode scan still separates the
`0x07FC` registration row from unrelated mask/offset immediates.

Disposition: keep the managed field named `Unknown0`. A new
`PacketPlaceholderNamingTests` guard rejects `ContextToken`,
`ClientContextToken`, and `CastingId` aliases until a `0x07FC` client apply path
or live non-zero cast-result capture proves the field's client-side owner.

## Initiative status - ACTIVE (2026-06-05 pass 128)

Pass 128 audited `Client0x07E3`, one of the remaining `12` `Client0xNNNN`
opcode placeholders in the character / destination-arrow boundary. Question:
can the one-`uint32` shape be promoted from a numeric diagnostic to a concrete
pregame/world UI request? Current support remains Mapped wire / Blocked
semantics: source reads one raw `uint32` into neutral `Client0x07E3.Value`,
logs it through the unresolved diagnostic handler, and performs no character,
destination-arrow, matching, movement, tradeskill, or server-response mutation.

Ghidra MCP decompiled `ClientWorldOpcodeRegister_MovementSpline`
(`WildStar64.exe` `1400a8190`) and confirmed opcode `0x07E3` is registered at
literal `1400a8282` with size `4`, `ClientUInt32_ReadPayload` (`14007d000`),
shared `ClientTradeskillResetTalents_WritePayload` (`14007d010`), and
`ServerUInt32_ReadPayload` (`14007ab50`). MCP batch decompile of the reader and
writer confirmed the field contract only: the reader advances `0x20` bits and
the writer serialises one 32-bit field. `selected_call_edges.csv` still shows
the shared writer's only selected code caller as
`ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), plus registration
and intra-function edges; it does not surface an opcode-specific `0x07E3`
sender or post-read consumer. The `140c1ef80` function-pointer selector table
also reuses `ClientTradeskillResetTalents_WritePayload` at multiple slots, so
that table is not opcode ownership evidence.

Disposition: `Client0x07E3` stays diagnostic-only. The positive-control
distinction remains important: known sibling `0x05D5` has both registration and
gameplay-send evidence, while current `0x07E3` evidence is registration/shared
serializer only. A focused headless `FindOpcodeComparisons` rerun was attempted
as `client07e3_opcode_scan`, but it could not acquire the open Ghidra project
lock held by the interactive MCP/CodeBrowser session; no result from that
timed-out helper is used as evidence. Blocker: a native `0x07E3` sender,
post-read consumer, indirect send-rail owner, or live character/destination UI
capture tied to opcode `0x07E3` is required before renaming or implementing
behavior.

## Initiative status - ACTIVE (2026-06-05 pass 127)

Pass 127 audited `Client0x0550`, one of the remaining `12` `Client0xNNNN`
opcode placeholders in the ICComm / spell-list boundary. Question: can the
one-`uint32` shape be promoted to ICComm, server spell-list, matching
replacement, movement-control ack, tradeskill-reset, ability-book, combat-log,
or P2P-trading behavior from adjacency or shared-writer evidence? Current
support is Mapped wire / Blocked semantics: source reads one raw `uint32` into
neutral `Client0x0550.Value`, logs it through the unresolved diagnostic handler,
and performs no ICComm, spell-list, matching, movement, tradeskill, ability,
combat-log, trade, or server-response mutation.

Native registration in `ClientWorldOpcodeRegister_MovementSpline`
(`WildStar64.exe` `1400a8190`) binds opcode `0x0550`, registered size `4`, read
label `ClientUInt32_ReadPayload` (`14007d000`), and shared
`ClientTradeskillResetTalents_WritePayload` (`14007d010`) at literal
`1400a824e`. The direct writer body serialises only one 32-bit field. The same
writer is reused by many named rows, including `0x0192`, `0x0248`, `0x0249`,
`0x05D5`, `0x0635`, and `0x0858`, so the field width alone does not prove
intent.

`TraceFunctionCallers 14007d010 80` found 67 direct references: one real code
caller, `ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), broad
registration/data rows in `Network_RegisterServerOpcode_0351`, movement-spline
registration/data rows, and raw data refs (`140c1ec38`, `140c1ec68`,
`140c1ec88`, `140c1ef80`, `140c1ef88`, `140c1f040`, `140c1f058`,
`140c1f168`). No opcode-specific `0x0550` gameplay sender or post-read consumer
surfaced. A narrowed `FindOpcodeComparisons` pass over `0x0550`, ICComm
`0x0546`/`0x054B`, `0x0551`, `0x05D5`, `0x0635`, `0x0858`, `0x017A`, and
combat-log `0x0248`/`0x0249` found `0x0550` only at registration literal
`1400a824e`; the remaining `0x550` matches were struct/stack offsets, not
opcode sends. `DumpNearbyData 140c247e0 8`, the static slot for
`DAT_140c1f210 + (0x0550 - 3) * 0x10`, found no defined message-name metadata
for the indirect `Network_SendMessageById` rail.

Disposition: `Client0x0550` stays diagnostic-only. Packet placeholder tests now
reject ICComm channel/message, server spell-list, matching replacement,
movement-control ack, tradeskill reset, ability-book, combat-log, and
P2P-trading aliases while preserving the exact one-`uint32` contract. Blocker:
a runtime indirect sender, post-read consumer, or live ICComm / spell-list /
ability-book capture tied to opcode `0x0550` is required before renaming or
implementing behavior.

## Initiative status - ACTIVE (2026-06-05 pass 126)

Pass 126 audited `Client0x063E`, one of the remaining `12` `Client0xNNNN`
opcode placeholders in the auth-denied / marketplace-status boundary.
Question: can the one-wide-string shape be promoted to `ClientSuggest`,
account-item pending group, support-ticket, marketplace/auth/status, or auction
filter behavior from the shared string writer? Current support is Mapped wire /
Blocked semantics: source reads one wide string into neutral
`Client0x063E.Text`, logs it through the unresolved diagnostic handler, and
performs no support, account-item, marketplace, auth, auction, or
server-response mutation.

Native registration in `ClientWorldOpcodeRegister_MovementSpline`
(`WildStar64.exe` `1400a8190`) binds opcode `0x063E`, registered size `8`,
read label `LAB_14007ae30`, and shared `ClientSuggest_WritePayload`
(`14007ae80`). The direct writer body still serialises only one wide string via
`NetworkBitWriter_WriteWideString`, matching `Client0x063E.Text`. The same
writer is shared with `0x012D`, `0x0833`, `0x0233`, and `0x07C6`, so field order
alone does not prove intent.

`TraceFunctionCallers 14007ae80 18` found 18 direct references: registration
rows in `Network_RegisterServerOpcode_0351`, two registration rows in
`ClientWorldOpcodeRegister_MovementSpline`, and raw data refs (`140c1eda8`,
`140c1edb0`); no gameplay sender or post-read consumer surfaced. A narrowed
`FindOpcodeComparisons` pass over `0x063E`, `0x012D`, `0x0833`, `0x0233`, and
`0x07C6` found `0x063E` only at registration literal `1400a821a`. The same scan
found proven semantic senders for sibling opcodes:
`Support_SendClientSuggest` (`0x0833`),
`AccountItem_SendClientClaimPendingItemGroup` (`0x0233`), and
`AccountItem_SendClientReturnPendingItemGroup` (`0x07C6`). Those are
false-rename guards rather than evidence for `0x063E`.

Disposition: `Client0x063E` stays diagnostic-only. Packet placeholder tests now
reject `ClientSuggest`, sibling `Client0x012D`, account-item pending-group,
support-ticket, marketplace/auth/status, and auction-filter aliases while
preserving the exact one-wide-string contract. Blocker: a native `0x063E`
sender, post-read consumer, or live auth-denied / marketplace-status /
support-text capture is required before renaming or implementing behavior.

## Initiative status - ACTIVE (2026-06-05 pass 125)

Pass 125 audited `Client0x012D`, one of the remaining `12` `Client0xNNNN`
opcode placeholders in the quest/pet-customisation boundary. Question: can the
one-wide-string shape be promoted to `ClientSuggest`, account-item pending
group, realm-transfer, quest, or pet behavior from the shared string writer?
Current support is Mapped wire / Blocked semantics: source reads one wide
string into neutral `Client0x012D.Text`, logs it through the unresolved
diagnostic handler, and performs no support, account-item, realm-transfer,
quest, pet, or server-response mutation.

Native registration in `Network_RegisterServerOpcode_0351` (`WildStar64.exe`
`14006c290`) binds opcode `0x012D`, registered size `8`, read label
`LAB_14007ae30`, and shared `ClientSuggest_WritePayload` (`14007ae80`).
`TraceFunctionCallers 14007ae80 18` found 18 direct references: registration
rows in `Network_RegisterServerOpcode_0351`, two registration rows in
`ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`), and raw data refs
(`140c1eda8`, `140c1edb0`); no gameplay sender or post-read consumer surfaced.
The direct writer body still serialises only one wide string via
`NetworkBitWriter_WriteWideString`.

A narrowed `FindOpcodeComparisons` pass over `0x012D`, `0x0833`, `0x063E`,
`0x0233`, and `0x07C6` found `0x012D` only at the registration literal
`14006c8dc`, plus unrelated arithmetic/offset uses in `FUN_1403b2c40`,
`FUN_1408c4190`, and `FUN_1408c5600`. The same scan found proven semantic
senders for sibling opcodes: `AccountItem_SendClientClaimPendingItemGroup`
(`0x0233`), `AccountItem_SendClientReturnPendingItemGroup` (`0x07C6`), and
`Support_SendClientSuggest` (`0x0833`). Those are false-rename guards rather
than evidence for `0x012D`.

Disposition: `Client0x012D` stays diagnostic-only. Packet placeholder tests now
reject `ClientSuggest`, account-item pending-group, realm-transfer, sibling
`Client0x063E`, and quest/pet-style field aliases while preserving the exact
one-wide-string contract. Blocker: a native `0x012D` sender, post-read consumer,
or live quest tracker / pet customisation / account rail capture is required
before renaming or implementing behavior.

## Initiative status - ACTIVE (2026-06-05 pass 124)

Pass 124 audited `Client0x011B` and `Client0x011D`, two of the remaining
`12` `Client0xNNNN` opcode placeholders in the character/loot/mail boundary.
Question: can either be promoted to loot bind confirmation, mail UI/list
request, loot vacuum, or tradeskill-reset behavior from adjacency or shared
writer evidence? Current support is Mapped wire / Blocked semantics: source
keeps `Client0x011B` as an empty unresolved diagnostic request and
`Client0x011D` as a one-`uint32` unresolved diagnostic request in
`ClientUnresolvedDiagnosticPackets.cs`, with log-only handlers and no loot,
mail, crafting, player, or server-response mutation.

Native registration in `Network_RegisterServerOpcode_0351` (`WildStar64.exe`
`14006c290`) binds `0x011B` to shared zero-payload
`ClientCraftingAbandon_WritePayload` (`140001ba0`) and `0x011D` to shared
`ClientTradeskillResetTalents_WritePayload` (`14007d010`). `TraceFunctionCallers
140001ba0 12` found 833 references, a broad shared-helper surface rather than
an opcode-specific owner. `TraceFunctionCallers 14007d010 12` found 67
references: registration/data rows plus
`ClientCompoundTradeskillUInt32_WriteCluster` (`14007dc80`), again without a
direct `0x011D` gameplay sender or post-read consumer. Earlier non-registration
hits remain rejected: `0x011B`/`0x011D` in Lua lexer/parser functions are token
ids, `Movement_UpdateAndSendFallStateOpcodes` uses
`GameFormula_GetEntryById(0x11B)`, and
`SpellCast_SendClientCastSpellOrPosition` returns `CastResult.IllegalSpellCast`
(`0x011D`) rather than sending opcode `0x011D`.

Disposition: both placeholders stay diagnostic-only. Packet placeholder tests
now reject loot-bind, mail, loot-vacuum, and tradeskill-reset aliases while
preserving the exact empty / one-`uint32` field contracts. Blocker: a native
`0x011B`/`0x011D` sender, post-read consumer, or live character/loot/mail UI
capture is required before renaming or implementing behavior.

## Initiative status - ACTIVE (2026-06-05 pass 123)

Pass 123 audited `Client0x00ED`, another one of the remaining `12`
`Client0xNNNN` opcode placeholders. The packet contract is mapped but the
semantic owner remains blocked: native registration in
`Network_RegisterServerOpcode_0351` (`WildStar64.exe` `14006c290`) binds opcode
`0x00ED` to local read/advance label `1400a61e0` and writer
`ClientUnresolvedDiagnosticPacket00ED_WritePayload` (`1400a6200`) with
registered size `0x20`. The writer serialises one `uint64`, one `uint32`, one
`uint64`, and three trailing bits, matching the managed `Value0..5` shape.

Fresh caller evidence did not unlock a rename. `TraceFunctionCallers
1400a6200 10` found five references: three raw data references
(`140dd0a14`, `140b97b64`, `140b97b74`) and two registration-table setup refs
inside `Network_RegisterServerOpcode_0351` (`140079cce`, `140079ced`), with no
gameplay sender or post-read consumer. `InspectCodeAddress 1400a61e0 32`
reports no standalone function at the paired read label; it is a local
registration label between `ClientCraftingAdditive_WritePayload` and the
`0x00ED` writer, so no function label was added. Prior exact `0xED` immediate
hits remain rejected as mail-side `GameFormula_GetEntryById(0xed)` lookups
around `ClientMailSend` (`0x0168`), not packet evidence.

`Client0x00ED` therefore stays diagnostic-only with neutral `Value0..5` fields.
Packet placeholder tests now reject aliases to duel, mail, and path surfaces
until a native `0x00ED` sender, post-read consumer, or live duel/path/mail UI
capture proves an owner.

## Initiative status - ACTIVE (2026-06-05 pass 122)

Pass 122 audited `Client0x00C8`, one of the remaining `12` `Client0xNNNN`
opcode placeholders. The packet wire shape is mapped but the semantic owner is
blocked: `Network_RegisterServerOpcode_0351` (`WildStar64.exe` `14006c290`)
binds decimal `200` (`0x00C8`) to `ClientMatchType_ReadPayload`
(`14008a140`) / `ClientMatchType_WritePayload` (`14008a150`) with registered
size `4`, and the same reader/writer pair is reused by
`ClientMatchingQueueLeave` (`0x05B5`) and `ClientMatchingQueueLeaveAsGroup`
(`0x05B6`). The direct reader still proves only one 5-bit `MatchType` field.

Fresh evidence did not produce an opcode-specific sender. `TraceFunctionCallers
14008a150 8` found six refs, all registration-table setup references inside
`Network_RegisterServerOpcode_0351`; the selected decompile confirms the
`0x00C8` registration row and the shared `0x05B5`/`0x05B6` rows, but no
gameplay caller. `ClientChallengeChoice` (`0x00C5`) already owns the mapped
challenge send cluster (`140710c10`, `140710d60`, `140711ea0`, `140711f10`),
so `Client0x00C8` must not be promoted from enum adjacency or the shared
`MatchType` payload alone. `PacketPlaceholderNamingTests` now rejects aliases to
queue-leave, group queue-leave, and challenge choice while preserving the
mapped `MatchType` field. Next evidence source: a native `0x00C8` sender,
client consumer, or live challenge/housing/matching UI capture tied to this
opcode.

## Initiative status - ACTIVE (2026-06-05 pass 121)

Pass 121 tightened the lone `Server0xNNNN` placeholder boundary instead of
renaming it from adjacency. `Server0x0015` still maps to
`ServerUInt5UInt32_ReadPayload` (`WildStar64.exe` `140081f00`), which reads one
5-bit field followed by one `uint32`. The selected decompile registers opcode
`0x0015` with that reader at `selected_decompiled.c` registration line
`12928`, and matching opcode `0x0628` at line `13535`; the same reader body is
also called inside `ServerFortuneRewards_ReadPayload` (`140081f60`) for counted
money reward rows. A focused `TraceFunctionCallers 140081f00 8` pass found six
refs: one raw data ref, one unconditional row-helper call from
`ServerFortuneRewards_ReadPayload`, and four registration-table data refs for
the `0x0015` and `0x0628` tuples, with no opcode-specific apply or producer
caller. Because the helper is a reusable wire reader, not a semantic owner,
`Server0x0015.Value0/Value1` stay neutral and must not be renamed to
`MatchType`/`AverageWaitTime` until an opcode-specific consumer, apply-table
owner, producer, or live `0x0015` payload proves that meaning.

Focused Ghidra `FindOpcodeComparisons` over `0x0015`/`0x0628` reconfirmed the
`0x0628` registration literal and found the `0x0015` registration tuple, but the
small `0x15` immediate saturates the global scan with bit counts, offsets, and
writer-size arithmetic. Treat those hits as non-semantic unless a call target or
dispatch path ties them to opcode `0x0015`. Packet placeholder tests now guard
that `Server0x0015` has only `Value0`/`Value1` and no matching average-wait
aliases.

A 2026-06-05 follow-up rechecked the ownership side from the selected cache:
`Network_RegisterServerOpcode_0351` registers `0x0015` with
`ServerUInt5UInt32_ReadPayload` at line 241 of the cached fragment and `0x0628`
with the same reader at line 848, while selected call edges still show only the
`ServerFortuneRewards_ReadPayload` row-helper caller for `140081f00`.
`MatchingManager_ApplyMatchingAverageWaitTimeUpdated` remains the positive
`0x0628` apply control and dispatches `MatchingAverageWaitTimeUpdated`, but
selected xrefs/function-pointer inventories expose no equivalent `0x0015` apply
owner or producer. MCP and a bounded `TraceFunctionCallers 140081f00 12`
helper attempt were blocked by the open `NexusForeverClient64_WildStar64`
project lock, so no local Ghidra rename/export was attempted.

## Initiative status - ACTIVE (2026-06-05 pass 129)

Pass 129 rechecked `Client0x0701` with MCP against the current WildStar64
project and cache. The opcode remains Mapped wire / Blocked semantics:
`Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x0701` with size
`8`, read-advance label `1400a69c0`, and writer
`ClientUInt2UInt32_WritePayload` (`1400a69d0`). The read-advance stub only adds
`0x22` bits to the reader cursor, and the writer serializes `param_2[0]` as two
bits followed by `param_2[1]` as one `uint32`.

MCP assembly/xrefs keep the opcode in registration-only territory:
`140079de1` loads the writer, `140079df0` loads the read-advance label,
`140079e05` is `MOV EDX,0x701`, `140079e0a` sets size `8`, and `140079e13`
calls the registrar vtable. `get_function_xrefs 1400a69d0` found only the two
registration setup refs plus raw data pointer `140dd0a74`; `audit_global
140dd0a74` reports no xrefs. `get_function_xrefs 14006c290` still shows the
registrar called from `WorldSocket_InitPersistentRuntimeNodes` (`14000bdc4`)
only. No sender, post-read consumer, callback owner, Lua entry, string anchor,
or live capture surfaced, so the managed packet stays numeric/log-only and no
label or C# behavior rename is safe.

## Initiative status - ACTIVE (2026-06-05 pass 130)

Pass 130 rechecked `Client0x0928` with MCP against the current WildStar64
project and cache. The opcode remains Mapped wire / Blocked semantics:
`ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) registers `0x0928` at
`1400a8524` with size `8`, read-advance label `140089240`, writer
`ClientUInt32UInt5_WritePayload` (`1400898b0`), and final reader/apply helper
`ServerUInt32UInt5_ReadPayload` (`14008ce80`). The writer serializes
`param_2[0]` as one `uint32` followed by `param_2[1] & 0x1f` as a 5-bit field;
`ServerUInt32UInt5_ReadPayload` reads the same `uint32 + 5-bit` shape.

The pass also corrected an old positive-control breadcrumb: `140070cc4` is the
`0x068E ClientPetSetStance` registration literal inside
`Network_RegisterServerOpcode_0351`, not a gameplay send site. The gameplay
send witness remains `14050a31d`, which calls `Network_SendOpcodePayloadHelper`
with opcode `0x068E`. `ServerUInt32UInt5_ReadPayload` (`14008ce80`) is not
`0x0928`-specific either: MCP xrefs show it registered for `0x068F
ServerPetStanceChanged` at `140071a36` and for `0x0928` at `1400a8524`, plus
unxrefed raw data pointer `140dceca4`. No
opcode-specific sender, post-read consumer, callback owner, Lua/UI anchor, or
live capture surfaced for `0x0928`, so the managed packet remains numeric and
log-only.

## Initiative status - ACTIVE (2026-06-05 pass 120)

Pass 120 refreshed the placeholder inventory before selecting the next semantic
rename slice. Current source scans find exactly `12` `Client0xNNNN` enum/model
names (`Client0x00C8`, `Client0x00ED`, `Client0x011B`, `Client0x011D`,
`Client0x012D`, `Client0x0550`, `Client0x062A`, `Client0x0634`,
`Client0x063E`, `Client0x0701`, `Client0x07E3`, `Client0x0928`) and one
`Server0xNNNN` enum/model name (`Server0x0015`). The client diagnostic surface
is `16` log-only packets: the `12` numeric models plus
`ClientAccountRealmData`, `ClientRealmListRealmRow`,
`ClientRealmListMessageRow`, and `ClientAddonModuleList`. The current enum
backlog also has `24` `PrerequisiteType.UnknownNNN` entries and the two quest
objective placeholders `QuestObjectiveType.Unknown27`/`Unknown29`.

Broad placeholder tails remain mixed evidence gates rather than automatic rename
targets: the 2026-06-05 scan found `90` distinct `Unknown*`, `37` `Value*`,
`35` `*Wire*`, and `9` `Trailing*` Source tokens; `141` Source C# `WIP` hits;
`108` script/cinematic/content WIP-guessed tracker hits; and zero production
`TODO`/`FIXME`/`NotImplemented` hits. The three audit-prompt ghost literal names
are absent by `rg`. Packet placeholder naming tests now pin the 12-client/1-server
opcode inventory so stale 17-client or 0-server counts do not re-enter the
trackers.

## Initiative status - ACTIVE (2026-06-04 pass 119)

Pass 119 tightened the F-004 housing neighborhood boundary. Packet placeholder
coverage now pins `ServerHousingNeighborhoodEntry` (`0x0501`) /
`ServerHousingNeighborhoodList` (`0x0506`) as mapped wire shapes with
`NeighborhoodWireUInt64_AfterRealmIds` and `NeighborhoodWireUInt32_0..2`
retained until producer/backing-store proof exists. The guard explicitly rejects
renaming/synthesizing row fields from `HousingNeighborhoodInfo.tbl` table-column
names such as `BaseCost`, `MaxPopulation`, or `HousingMapInfoIdPrimary`.
Runtime emits remain blocked until a live housing UI/realm-login sniff or native
server-push path proves `0x0506` ordering and row backing. Focused housing /
placeholder verification passed 106/106.

## Initiative status - ACTIVE (2026-06-04 pass 118)

Pass 118 tightened the F-025 map-tracked-unit boundary. Packet naming coverage
now pins `ServerMapTrackedUnitUpdate.TrackingSlotId` as the 15-bit
`TrackingSlot.tbl` row id and rejects a direct `PublicEventObjectiveId` packet
field. `TrackingSlotHelperTests` now cover duplicate rows sharing
`PublicEventObjectiveId = 5010` and guard that the helper does not expose an
objective-only `TrackingSlotIdForPublicEventObjective` selector. Runtime emits
for `0x0849` / `0x0848` remain blocked until tracked-unit id allocation,
update cadence, disable lifetime, and slot selection are proven by a native
send site or accepted live public-event marker capture. Focused verification
passed 97/97.

## Initiative status - ACTIVE (2026-06-05 pass 133/134)

Pass 133 closed `ServerRaidQueueStatus.Unknown0..4` as shared raid-info row
fields, not queue-position tails. Native registration binds `0x0718` to
`ServerRaidQueueStatus_ReadPayload` (`14008bf80`) and adjacent `0x071A` to
new durable label `ServerRaidInfoResponse_ReadPayload` (`14008c010`), which
reads a count and 0x20-byte rows through the same row reader. The downstream
consumer `Group_DispatchRaidInfoResponse` (`1406042b0`) maps row offsets to
`strSavedInstanceId`, `nWorldId`, `strDateExpireUTC`, `fDaysFromNow`, and
`nPrimeLevel`, so NexusForever now uses `SavedInstanceId`, `WorldId`,
`DateExpireUTC`, `DaysUntilExpire`, and `PrimeLevel` on the `0x0718` row model.
The all-zero compatibility emit remains; standalone non-zero `0x0718` producer
timing and queue-only aliases stay blocked until a live payload or producer/apply
owner appears. Ghidra MCP attach was attempted but failed because the visible
socket advertised only `unknown` and the TCP fallback timed out. Pass 134 retried
the MCP bridge (`list_instances` and `connect_instance` both returned
`Transport closed`) and re-scanned the cache: exact opcode hits remain
registration/reader-only, `selected_call_edges.csv`
shows the only selected caller into `14008bf80` as `14008c010`, and no static
standalone `0x0718` sender literal was found.

## Initiative status - ACTIVE (2026-06-04 pass 117)

Pass 117 classified the F-025 entity-stat auxiliary payload fields as
intentional neutral names, not missed renames. Current source audit finds
`ServerEntityStatUInt32Triplet`, `ServerEntityStatUInt32WideString`,
`ServerEntityStatUInt32UInt5UInt32`,
`ServerEntityStatUInt32UInt14UInt18WideString`,
`ServerEntityStatUInt32UInt5Pair`, and `ServerEntityStatTwoUInt32UInt64` only
in opcode/model definitions and focused tests; runtime stat sends still use
`ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger`. Packet naming
coverage now pins the neutral `Value*` / shared `Value` / `Text` fields until a
per-opcode `WorldSocket+0x15b0` apply handler, apply-table classification, or
live sniff/order witness proves field semantics and emit timing. Focused
verification passed 91/91.

## Initiative status - ACTIVE (2026-06-04 pass 116)

Pass 116 classified `ServerRaidQueueStatus.Unknown0..4` as intentional neutral
F-010 tails based on the evidence available at the time. Pass 133 superseded
the field-name part of this note by mapping those offsets through the `0x071A`
raid-info list consumer; only standalone non-zero `0x0718` producer timing and
queue-only semantics remain blocked.

## Initiative status - ACTIVE (2026-06-04 pass 115)

Pass 115 classified marketplace `AuctionInfo.Unknown2` as a blocked tail, not a
safe semantic rename. Source audit shows the field is a 32-bit value at the end
of `AuctionInfo`, persists through `marketplace_auction.unknown2`, and is cloned
or loaded through `GlobalMarketplaceManager`, but new auctions leave it at zero
and no current `Game` / `WorldServer` consumer or producer writes a non-zero
semantic value. Packet coverage now pins the field as round-tripped wire state
only. Keep the neutral name until a native marketplace auction row consumer,
retail capture with non-zero value, or client UI/Lua reader proves ownership.

## Initiative status - ACTIVE (2026-06-02 pass 114)

Pass 114 renamed `Unknown267` to `GroupIsRaid`. Native case `0x10b`
dispatches vtable `+0x320` to `14049e9a0`, which returns bit 1 from the current
group context flags at `*(DAT_140c65898+0x6c50)+8`; NexusForever maps that bit
as `GroupFlags.Raid`, and the retail rows use `NotEqual 0` against that raid
bit. `Unknown260` stays unnamed: state `5` is now backed by named native
`HousingBuildComplete` helpers, but active residence fields `+0x60/+0x64` and
row/use-site reachability remain blocked.

## Initiative status - ACTIVE (2026-06-02 pass 113)

Pass 113 renamed `Unknown295` to `EvaluatedEntityPresent`. Native case `0x127`
is inline and checks only that the evaluated entity pointer is present when the
comparison is `NotEqual`; retail row `44550` is a target-apply gate on Engineer
Portable Black Hole Large Pull, and NF already proxies the same target presence.

## Initiative status - ACTIVE (2026-06-02 pass 112)

Pass 112 closed all duplicate-body aliases as known-but-not-renamed prerequisite
ids: `Unknown223`, `Unknown240`, `Unknown245`, `Unknown275`, `Unknown286`,
`Unknown289`, `Unknown290`, and `Unknown291`. They remain `UnknownNNN` because
the live body is mapped but no distinct semantic enum owner is proven.

## Initiative status - ACTIVE (2026-06-02 pass 111)

Pass 111 closed `Unknown144` as an unreachable/skipped dispatcher candidate:
no retail prerequisite rows, no external refs, no live case `0x90`, and only a
diagnostic handler-table body (`1404a0b70`) that the live dispatcher does not
reach.

## Initiative status - ACTIVE (2026-06-02 pass 110)

Pass 110 closed the path-settler orphan helper ids `Unknown189`, `Unknown192`,
and `Unknown193` as rejected semantic enum candidates. Their helper bodies stay
labelled for native runtime study, but live prerequisite dispatch reaches
`140001ba0`, handler-table slots are `_purecall`, and no direct caller proves
enum ownership.

## Initiative status - ACTIVE (2026-06-02 pass 109)

Pass 109 closed the content-referenced no-op prerequisite ids as rejected
semantic candidates: `Unknown48`, `Unknown142`, `Unknown203`, `Unknown271`,
`Unknown278`, and `Unknown285`. Their retail references remain documented in
`PREREQUISITE_UNKNOWN_CLOSURE_MATRIX.md`, but live dispatch reaches
`140001ba0` (`return 0`), so these are disabled/dead/test gates unless future
evidence proves a non-stub predicate path.

## Initiative status - ACTIVE (2026-06-02 pass 108)

Pass 108 closed the first five no-row/no-ref no-op prerequisite ids as rejected
semantic candidates: `Unknown222`, `Unknown259`, `Unknown272`, `Unknown283`,
and `Unknown294`. They retain `UnknownNNN` enum names for table-id stability,
but no longer need rename work unless new evidence proves a non-stub live path.

## Initiative status - ACTIVE (2026-06-02 pass 107)

Pass 107 created `PREREQUISITE_UNKNOWN_CLOSURE_MATRIX.md` for the 26 remaining
`PrerequisiteType.UnknownNNN` ids. The matrix combines enum/test buckets,
retail prerequisite rows, cross-table prerequisite references, live dispatcher
cases, raw PE vtable targets, and handler-table targets. It also corrected the
guardrail classification for `Unknown267`: this was a diagnostic field-owner
candidate (`14049e9a0` bit-1 predicate), not a duplicate-body alias.

## Initiative status - ACTIVE (2026-06-02 pass 106)

Pass 106 audited the account-item prerequisite names after the type 275
demotion. Raw PE reads prove live vtable `0x140b67800` (`+0xc0`) points to
`Prerequisite_CheckOwnsAccountItem` (`14049ca70`) for type 244 and
`0x140b67870` (`+0x130`) points to
`Prerequisite_CheckDoesNotOwnAccountItemOnCharacter` (`14049d240`) for type
246. Retail `wildstar_client.prerequisite` rows also line up: type 244 has one
`Equal` row with `objectId0` matching `Item2`, and type 246 has 53 `NotEqual`
rows whose `value0` values all match `Item2`. Names stay mapped; no handler was
added because account-item ownership state is still a runtime implementation
boundary.

## Initiative status - ACTIVE (2026-06-02 pass 105)

Pass 105 demoted prerequisite type 275 back to `Unknown275`: row-shape evidence
looked like account-item ownership, but live case `0x113` dispatches vtable
`+0x6a0` to `Prerequisite_CheckItemTradeSkillKnown` (`1404a17e0`). Treat it as
a duplicate-body alias until a distinct semantic owner is proven.

## Initiative status - ACTIVE (2026-06-02 pass 104)

Pass 104 added packet naming guardrails for registration-only diagnostic client
opcodes (`Client0x011B`, `Client0x011D`, `Client0x012D`, `Client0x0550`,
`Client0x062A`, `Client0x0634`, `Client0x0701`, `Client0x07E3`, and
`Client0x0928`). Their wire shapes remain mapped, but semantic renames stay
blocked until gameplay sender/consumer evidence or live sniffs prove owners.
The 2026-06-04 F-002 handler guard extends the runtime boundary: the unresolved
diagnostic handler family is test-pinned as log-only with no plaintext or
encrypted server emit; focused diagnostic coverage passed 90/90.
The 2026-06-05 realm-row guard keeps `ClientAccountRealmData`,
`ClientRealmListRealmRow`, and `ClientRealmListMessageRow` structural-only:
selected call edges nest the row writers under the `0x0760` row /
`ServerRealmList` serializers, with no opcode-specific client send owner.

## Initiative status - ACTIVE (2026-06-02 pass 101)

Pass 101: PE registration for entity-stat/threat triplet opcodes (`0x0889`/`0x0908`/`0x090A` -> `ServerSpellUInt32TripletListRow_ReadPayload` @ `140080bf0`); labeled `Prerequisite_CheckPetOrEsperPetEntity` @ `14049cae0` (type 266). No enum renames for blocked stubs/aliases.

Pass 102 adjunct (2026-06-04): rejected stale native label `FUN_140939650` as an
F-025 packet consumer candidate. Cached export evidence shows viewport/grid
global recalculation with label-only xrefs, not a `WorldSocket+0x15b0`
handler/apply node. Entity-stat aux `ValueN` fields and `0x08A8` flags remain
blocked.

## Initiative status - ACTIVE (2026-06-02 pass 100)

Pass 100 corrected handler-table[5] mislabel (`14049d470` is orphan entity-id list walk; live type 5 Reputation uses inline `entity+0x118` + `1404a2010`). Labeled `PrerequisiteManager_ApplyComparisonFloat` @ `1404a2010`.

## Initiative status - ACTIVE (2026-05-23 resume)

Decomp pass 130 added group stat-block layout mapping and retail catalog wire scalar names. Ghidra shared project was lock-blocked; used cached `140607490.fragment.c` and retail SQL.

## Current roadmap buckets (2026-06-02)

This tracker intentionally separates names that are safe to use from names that
must stay opaque until another evidence source appears.

### Implemented in the active prerequisite / diagnostic slice

- Mapped `PrerequisiteType` names with conservative NF checks:
  `AccountCurrencyAmount`, `CREDDPendingOrderState`, `PetOrEsperPetEntity`,
  `PathMissionChecklistItemComplete`, `HousingResidenceLoaded`,
  `HousingNeighborResidence`, `DailyLoginDaysTotal`,
  `DailyLoginRewardsAvailable`, `SpellTierUnlocked`, and the account-item /
  spell / path helper names listed below.
- Structural client packet names backed by row serializers:
  `ClientAccountRealmData`, `ClientRealmListRealmRow`, and
  `ClientRealmListMessageRow`. Their standalone send/consumer events remain
  blocked, so handlers stay diagnostic-only.

### Deliberately blocked / keep opaque

- No-op or `_purecall` prerequisite dispatch slots: `Unknown48`, `Unknown142`,
  `Unknown189`, `Unknown192`, `Unknown193`, `Unknown203`, `Unknown222`,
  `Unknown259`, `Unknown271`, `Unknown272`, `Unknown278`, `Unknown283`,
  `Unknown285`, and `Unknown294`.
- Duplicate-body aliases without a semantic owner: `Unknown223`, `Unknown240`,
  `Unknown245`, `Unknown275`, `Unknown286`, `Unknown289`, `Unknown290`, and
  `Unknown291`.
- Diagnostic predicates with blocked field owner: `Unknown260`.
- Broad static/table placeholders such as `Stat.Unknown*`,
  `InventoryLocation.Unknown*`, `UserTextFlags.Unknown*`, and generic packet
  tails remain out of scope until consumer evidence proves names.

### Runtime blocker field-mapping tranche (2026-06-02)

| Area | Status | Code / notes |
| --- | --- | --- |
| F-025 map-tracked (`0x0849`/`0x0848`) | **Mapped**; lookup/selection guard **Implemented**; producer **Blocked** | `TrackingSlotId` XML; `TrackingSlotHelper` (null-guarded one-way table lookup, no emit, no objective-only reverse selector); RE: native producer / handler-node path |
| F-004 neighborhood (`0x0501`/`0x0506`) | **Mapped**; table loader labelled; emit **Blocked** | `NeighborhoodWireUInt64_*` / `NeighborhoodWireUInt32_*`; pass 119 guards against `HousingNeighborhoodInfo.tbl` column-name synthesis; RE pass: no `ClientHousing*` in `0x0502`-`0x0509`, apply via DATA slots @ `140bd97b0`/`140e0ec28` -> `Housing_HandleNeighborhoodList`; WildStar64 `ClientDB_RegisterHousingNeighborhoodInfo` @ `140205900` proves table loader only; not `0x052C`->`0x0526`; 2026-06-04 Ghidra MCP recheck also ruled out adjacent `0x052B`, `0x04B1`, and `0x052F` community/visit senders; separate WIP magic value `0x190000000000000A` was removed from `ServerHousingProperties.Residence.NeighbourhoodId` |
| F-025 entity-stat aux (`0x0889`..`0x093E`) | **Mapped**; emit **Blocked** | `ValueN` retained; per-opcode XML + apply gate documented; pass 117 pins neutral names with packet placeholder coverage |
| F-008 crafting aux (`0x084B`/`0x0855`) | **Mapped**; emit **Blocked** | `ValueN` / `FloatValue*` retained; service keys diagnostic |
| F-031 Fortune weights | **Mapped**; retail weights/rotation **Blocked**; reset code **Renamed**; F-007 `RewardRotation*` source **Rejected** | `ServerFortuneReset.ResetCode`; `FORTUNE_WEIGHT_AUDIT.md`; debug catalog probability sample log |

Harness bundles (when available): `f025-map-tracked-unit`, `f031-fortune-playthrough` via `Start-BlockerEvidenceHarness.ps1`.

### Next evidence targets

- Per-opcode entity-stat aux `vtable+0x58` apply handlers (see `ENTITY_AUX_DECODE_ROADMAP.md`).
- Map-tracked producer: live sniff for `TrackedUnitId` allocation + `TrackingSlotId` selection rule.
- Housing `0x0506`: server-push trigger (live sniff / realm data); registration @ `140077730`; WildStar64 `HousingNeighborhoodInfo.tbl` table loader @ `140205900` is mapped but does not prove row backing; Houston64 has no world-opcode `0x506` enqueue in Ghidra scan (MFC dialog ids only); observed `RequestJoinNeighborhood`/`RequestLeaveNeighborhood` strings have no usable current xref.
- Crafting `0x084B`/`0x0855` emit sites from native craft-finish path.
- Retail `ServerFortuneRewards.RewardItemProbabilities` capture for Madame Fay
  rotation or storefront-server catalog dump; F-007 `RewardRotation*` schedules
  are rejected as Fortune active-rotation evidence.
- Packet `ValueN` fields: follow `ENTITY_AUX_DECODE_ROADMAP.md` one opcode
  cluster at a time; do not rename or emit from shape adjacency alone.
- Client diagnostics still needing a gameplay send-site or live sniff:
  `Client0x011B`, `Client0x011D`, `Client0x012D`, `Client0x0550`,
  `Client0x062A`, `Client0x0634`, `Client0x0701`, `Client0x07E3`, and
  `Client0x0928`.

## Initiative status - prior closure note

The 2026-05-23 closure pass marked the first tranche complete. Every in-scope placeholder was either **renamed with evidence** (below) or **explicitly blocked / out of scope** with Ghidra or runtime negative evidence. Remaining `Unknown*` in `Source/**` are intentional until a future pass adds per-field proof.

**In scope:** packet/account/storefront/pending-item/achievement/quest-objective placeholders touched by the 2026-05 decomp passes, plus correlated group stat-block packets (`0x0436` / `0x0468`).

**Out of scope (do not bulk-rename):** remaining `PrerequisiteType.UnknownNNN` members, generic `Network.World` packet tails and `Server0x*` unresolved rows, item wire offset names (`Unknown44`, ...), `Stat` / `InventoryLocation` enums, table-backed `Game.Static` ids without client strings, and cosmetic renames (`Field6`, `PurchaseField0`, housing `Unknown0` without semantics).

**Re-open criteria:** new Ghidra consumer for a blocked offset, live capture with non-zero blocked wire fields, or NF runtime proof tying `QuestObjectiveType` / prerequisite id to behavior.

## Implemented (safe to use in code)

| Symbol | Real name | Evidence |
|--------|-----------|----------|
| Achievement `Data0` / `Data1` | `ProgressState` / `CreditedChecklistMask` | Client `Achievement_GetProgressValue` @ `140642b30`; NF achievement runtime |
| `QuestObjectiveType.Unknown10` | `ScriptedTargetGroupChecklist` | NF runtime + scripts |
| `QuestObjectiveType.Unknown20` | `CompletePublicEventRecipe` | `QuestObjective.Data` = `PublicEventObjective.Id` |
| `QuestObjectiveType.Unknown28` | `RescuePublicEventCreatures` | same |
| `QuestObjectiveType.Unknown31` | `CompletePublicEventObjective` | same |
| Account item flag `Unknown1` | `HasTargetPlayerIdentity` | `AccountInventoryItem_ReadPayload` @ `1400a9c20` |
| `ServerAccountCurrencyGrant` tail | `Reason` / `Reserved` | Packet shape tests |
| Storefront purchase shared payload | `PaymentCurrencySlot`, `PurchaseMoneyAmountBits`, `PurchaseOptionId`, `PurchaseExtensionId` | `Storefront_PurchaseCharacter_WritePayload` @ `1400acf80`; `StorefrontLib_PurchaseOffer` @ `1404f1150` |
| Storefront account purchase tail | `AccountPurchaseExtensionId`, `AccountTarget`, `RecipientName` | `Storefront_PurchaseAccount_WritePayload` @ `140080a20`; `Storefront_SendClientPurchaseAccountOffer` @ `1404507e0` |
| `PendingAccountItemGroup` wire fields | `SenderAccountId`, `TargetAccountId`, `HasTargetPlayerIdentity` (u64) | `PendingAccountItemGroup_ReadPayload` @ `1400a9b20`; cache insert @ `140005bf0`; NF `account_pending_item` columns |
| Group stat block | `StatBlockPrefix17`, `GroupMemberId`, `GroupMemberStatSlot` | `Group_CopyMemberStatBlockFromPayload` @ `140607490`; `GROUP_MEMBER_STAT_BLOCK.md` |
| Store catalog offer tail | `RetailCatalogWireScalar`, `RetailCatalogWireByte` | `RetailStoreOfferWireConstants.CatalogWireScalarBits`; retail `field_6`/`field_7`; not consumed in Apply @ `14044b750` |
| `AuctionInfo.UnknownArray` | `MicrochipIds` | NF marketplace persistence + shared `Item.Microchips` 3-bit wire array |
| `ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo.Unknown30`-`Unknown38` | `PrimeLevel`, `IsParty`, `Roles` | `MatchingQueuedPlayerInfo_ReadPayload` @ `140098500`; correlates with `ClientMatchingQueue_WritePayload` @ `140098a70` and `MatchingQueueJoinQueueData_ReadPayload` @ `1400989d0` |
| `ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo.Unknown3C` | `TrailingUInt32_0x3C` | `MatchingQueuedPlayerInfo_ReadPayload` @ `140098500`; uint32 wire confirmed, semantics blocked |
| `ServerMatching0x05CF` | keep numeric | `ServerUInt32_LocalReadThunk` @ `140099110` proves one `uint32`; `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0` is only a correlated apply candidate. The 2026-06-05 reference refresh found `1405c41c0` referenced only by `140e1e66c`, and `DumpNearbyData` then proved `140e1e66c` / `140e1e228` are PE `.pdata` `_IMAGE_RUNTIME_FUNCTION_ENTRY` entries rather than dispatch cells. A later MCP recheck found the helper only references broad globals `140c65b98` / `140c65898`, and the raw reader refs are the `0x05CF` / `0x085D` registration rows, so keep numeric until a real apply dispatcher/index or live sniff proves the semantic owner |
| `ServerRaidQueueStatus.Unknown0..4` | `SavedInstanceId`, `WorldId`, `DateExpireUTC`, `DaysUntilExpire`, `PrimeLevel` | `0x0718` row reader `14008bf80` is reused by `ServerRaidInfoResponse_ReadPayload` (`14008c010`) for opcode `0x071A`; `Group_DispatchRaidInfoResponse` (`1406042b0`) maps row offsets to `strSavedInstanceId`, `nWorldId`, `strDateExpireUTC` (Windows FILETIME), `fDaysFromNow`, and `nPrimeLevel`. Queue-only aliases remain blocked. |
| `ServerSpellCastTargetReport.UnknownStructure0` / `unknownStructure0` | `TargetReportRow` / `TargetReportRows` | Partial row map (`CasterId` known); tail fields blocked |
| `PrerequisiteType.Spell221` / `Unknown269` | `ActionSetSpell` / `RapidTransport` | Table ids 221/269; client dispatch @ `1404a2100` cases `0xdd` / `0x10d` |
| `PrerequisiteType.Unknown170` | `GameFormula` | `value0` is `gameformula.id`; client case `0xaa` @ `1404a2100` (+0x90) |
| `PrerequisiteType.Unknown277` | `QuestObjective47OnCasterAndTarget` | Client case `0x115` requires caster **and** target to pass QuestObjective47 handler (+0x2f8) |
| `PrerequisiteType.Unknown292` | `PrimalMatrixNode` | `objectId0` matches `primalmatrixnode.id`; client case `0x124` (+0x6c8) |
| `PrerequisiteType.Unknown275` | no enum rename | Row-shape evidence uses `NotEqual` and `accountitem.item2Id`, but live case `0x113` dispatches vtable `+0x6a0` -> `Prerequisite_CheckItemTradeSkillKnown` (`1404a17e0`); duplicate-body alias, semantic owner blocked |
| `PrerequisiteType.Inventory` | `DoesNotOwnAccountItemOnCharacter` | 53 rows all `NotEqual`; every `value0` matches `item2.id`; client case `0xf6` vtable `+0x130` raw slot `0x140b67870` -> `14049d240` |
| `PrerequisiteType.Unknown50` | `UnderSpellOnTarget` | Client case `0x32` calls `FUN_1404699f0` on target `param_2[1]`; `value0` is Spell4 id |
| `PrerequisiteType.Unknown59` | `SpellTier` | `objectId0` is Spell4 id; `value0` is tier rank; client case `0x3b` via `FUN_1404a4f60` |
| `PrerequisiteType.Unknown60` | `SpellTierOnTarget` | Client case `0x3c` calls `FUN_14046a110` on target `param_2[1]`; same row shape as `SpellTier` |
| `PrerequisiteType.Unknown56` | `DifficultyRankSlotAssigned` | Client case `0x38` checks populated pointer at `entity+0x2d8 + value0*0x10` when `value0 < 0x1c` |
| `PrerequisiteType.Unknown106` | `TargetEntityLookupHit` | Client `14049e900` (+0x318) resolves target identity `+0x1a0` via `FUN_14079ee60` |
| `PrerequisiteType.Unknown244` | `OwnsAccountItem` | One row `Equal` with `objectId0` matching `item2.id`; client case `0xf4` vtable `+0xc0` raw slot `0x140b67800` -> `14049ca70`; NPC types `0x14`/`0x17` |
| `PrerequisiteType.Unknown248` | `NpcInventoryItemCount` | Client `1404a0cb0` (+0x5c0) counts Item2 id on NPC via `1405f68f0` bag walk |
| `PrerequisiteType.Unknown279` | `UnderSpellOnCasterAndTarget` | Client case `0x117` requires caster **and** target to pass UnderSpell helper `FUN_1404a4ec0` |
| `PrerequisiteType.Unknown129` | `ActiveSpellEffectOnUnit` | Client case `0x81` inline walk of `entity+0x15c8` with SpellService compare on `value0` |
| `PrerequisiteType.Unknown130` | `ActiveSpellEffectOnTarget` | Client case `0x82` calls `FUN_140469a70` on caster with target `+8` Spell4 filter |
| `PrerequisiteType.Unknown82` | `UnitEntityType` | Client case `0x52` via `14049c5b0` (+0x48); entity `+0x80` unit type id vs `value0` |
| `PrerequisiteType.Unknown89` | `PetMatchTarget` | Client case `0x59` via `14049d4f0` (+0x160); pet lookup id match caster vs target |
| `PrerequisiteType.Unknown102`-`105` | `SpellCooldownNodeOnUnit` / `OnTarget` / `InServiceSet` / `SpellEffectTypeOnUnit` | Client cases `0x66`-`0x69` walk `entity+0x15d0` cooldown-node list |
| `PrerequisiteType.Unknown133`-`136` | `ItemStatData` / `ItemStatId` / `AppliedItemStatId` / `Item2Id` | Item eval handlers `1404a0d00`-`1404a0d90`; type `135` `objectId0` = `ItemStat.Id` @ `+0x144` |
| `PrerequisiteType.Unknown90` | `MountVehicleType` | Client `14049e730` (+0x2c0) mount subobject unit-type gate |
| `PrerequisiteType.Unknown191` | `PetEntitySpell4` | Client case `0xbf` inline `Prerequisite_ResolveSpellOnPetEntity` @ `1404695e0` on player/pet `entity+0x1600` |
| `PrerequisiteType.InfrastructureState` (148) | NF `PrerequisiteCheckInfrastructureState` | Client `1404a0150`; retail pairs `PathSettlerInfrastructure.Id` with `PathMission` type `21` (`0x15`) |
| `PrerequisiteType.State` (149) | mapped-only / no runtime handler | Live case `0x95` vtable `+0x88` -> `Prerequisite_CheckEntityStateFlags_Table149` (`14049c840`); compares boolean `(entity+0x1e4 != 0 || entity+0x2ac != 0)` to `value0`, but row ids have no direct prerequisite-column references found and field ownership remains blocked. Pass 142 rejected `PrimalMatrix_UpdateNodeVisualStateFlags` (`1406e70a0`) / `PrimalMatrix_HandleNodeAllocationInput` (`1406e73e0`) as a same-offset PrimalMatrix node visual/allocation false lead, not entity-state ownership. Pass 143 labelled the scene proximity-cue cluster (`SceneProximityCue_LookupConfigById` `1404cc070`, `SceneProximityCue_IsCandidateInRange` `140722d30`, `SceneProximityCue_CheckPrerequisiteGate` `1407234a0`, `SceneProximityCue_ProcessQueuedCandidate` `1404cc0d0`, `Entity_ApplySceneProximityCue` `14047a1f0`, `SceneProximityCue_SelectWorldZoneEntry` `140722ed0`) and strengthens actual entity `+0x2ac` as a cue/action-blocking gate, but `+0x1e4` still lacks a native writer/owner and State stays unimplemented. |
| `PrerequisiteType.PetEntitySpell4` (191) | NF `PrerequisiteCheckPetEntitySpell4` | Client case `0xbf`; vanity-pet summon proxy until `entity+0x1600` wrapper id is mapped |
| `PrerequisiteType.ItemRolledPropertyValue` (was `Unknown140`) | renamed pass 19 | `1404a0e80` + `14040e610` Property slot ids at `+0x94..+0xcc`; zero `Prerequisite.tbl` rows |
| `PrerequisiteType.ItemSpecial` (138) | NF handler pass 21 | `1404a0dd0` +0x114/+0x118; zero tbl rows typed 138 |
| `PrerequisiteType.ItemMicrochip` (139) | NF handler pass 21/24 | `1404a0e10` + `14049bdc0`; objectId0 7-0xd = `RuneType` socket bitmask via `IItem.RuneSlots`; wire `Microchips[]` separate |
| Augment microchip **install** opcode | pass 44/46 mapped (replication + partial patch) | **Client C2S install** = `0x085B` only. Wire **`Microchips[]`** on **full add** via `SharedItem_ReadPayload` -> `140569c90` -> `14056aa20`. **Existing-slot microchip-only patch** = `1403b8540` (caller PE `0x1403eef8a`, cluster `0x1403eed00`) - **player opcode still blocked** (pass 47: **not** `0x046F` guild bank) |
| Partial inventory slot field apply | pass 46 correlated | PE cluster `0x1403eed00`-`0x1403ef100` per-field patches on **player bag**; distinct from `0x0111` and from **`0x046F`/`0x047A` guild bank** |
| `ServerGuildBankInventoryAdd_ReadPayload` | pass 47-48 mapped | `140092580` = **`0x046F`** @ `14006c290`; apply `GuildBank_ApplyInventoryAdd` `14057d190`; apply-table cell **`140e1a760`** |
| `ServerGuildBankTabInventory_ReadPayload` | pass 47-48 mapped | `140092470` = **`0x047A`**; apply `GuildBank_ApplyTabInventoryRows` `14057cdc0`; apply-table cell **`140e1a718`** |
| Partial inventory field-apply gap | pass 48 inspect | Cluster **`0x1403eed00`**; microchip case **`0x1403eef8a`->`1403b8540`**; inspect fragments in cache; **not** spell jumptable `0x1403f1344`; opcode still blocked |
| `Prerequisite_CheckFaction` | pass 47 mapped | Type **128** case `0x80` @ `+0x68` -> `14049c720` + `PlayerFactionService_IsFactionOrAncestor` `1407176b0` |
| `ItemAdded` native subscribers | pass 44 mapped | Only dispatcher `1403b8060` in `string_xrefs`; Lua ClientEvent paths + `140409330` case **9** `uItem` refresh; no additional labeled C++ subscriber in export cache |
| `Game.ItemData.GetDetailedInfo` | pass 45 mapped | `140417de0` PE registration at `.data` `0x140c58c10` with string `GetDetailedInfo`; builds `tPrimary`/`tCompare` (not `GetRuneSlots`) |
| Tooltip `tRunes` | pass 45 mapped | `140673ab0` reuses `140673b80` for tooltip context; distinct Lua key from `GetRuneSlots` / `GetDetailedInfo` |
| `PrerequisiteType.ItemMicrochip` objectId0=0 | pass 45 rejected on client | `1404a0e10` bitmask-only; rows 10610/10685 need NF count proxy until alternate client handler found |
| ItemSpecial row `+0x10` -> item-eval `+0x114` | pass 35 correlated + rejected | SQL `spell4IdOnEquip`; augment rows hold Spell4 FK (81886+). Do not use as socket mask except rare values subset socket-bit set (only id 3694=4 in reference DB) |
| Category **177** fusion-tier `item2TypeId` | pass 36 correlated | Types **515/516/530/563** (and empty **514/517**) map to `RuneType.Fusion` via `item2TypeId >= 502`; elemental band **340-345** unchanged (base **333**) |
| `Item.Build` sparse `Glyphs[]` | pass 37 implemented | `ItemRuneNetworkWire` emits one glyph id per `RuneSlots` index (0 = empty); wire->`itemData+0x518` producer still blocked |
| Wire -> shared-runtime | pass 39 mapped | `SharedItemRuntime_ApplyParseBuffer` 140411b60: glyphs `parse+0x88` -> runtime `+0x8f`; microchips via `ItemEval_ApplyItemSpecialToEval` -> runtime `+0x20` (Inspect-only via 1403deff0) |
| Wire -> entity inventory item | pass 40 mapped | `InventoryItem_ApplySharedItemPayload` 140569c90: glyphs `parse+0x88` -> entity `+0xbc`; microchips `parse+0x78` -> entity `+0x98`; compact sockets via `InventoryItem_RefreshItemState` entity `+0x60` |
| Entity item -> item-eval | pass 41 mapped | `InventoryItem_RefreshItemState` 14056a430 -> `ItemEval_ApplyItemSpecialToEval` with entity `+0xbc`/`+0x98`/`+0x60` |
| Entity `+0xbc` -> shared-runtime `[0x8f]` | pass 42 mapped | `ItemEval_CommitRuneDataFromLinkedEntity` 140413520 on `ItemEval_CommitPendingRuneData` 140412ad0 reads `(*runtime)[0]+0xbc` |
| Entity `+0x60` -> runtime socket fields | pass 42 mapped | Same commit copies entity `+0x60` compact bytes into runtime `+0x5d` region (GetRuneSlots `+0x388` proxy via `+0x71` index) |
| GetRuneSlots `+0x544` / `[0xa9]` gates | pass 43 mapped | Scratch-context indices alias embedded shared-runtime `+0x4a4` / `[0x95]`; filled by `ItemEval_SyncFromItemDataUserdata` `140410680` from Lua `Game.ItemData` userdata (`140417660` + `140410300`), not separate parent-only writers |
| Entity item -> Game.ItemData | blocked (narrowed) | No direct entity->userdata writer; live `GetRuneSlots` reads userdata native blob synced via `140410680`; entity glyphs reach userdata through eval commit `140413520` + Lua refresh (`ItemAdded` / case 9), not `ItemModified`->`itemData` |
| Slot update -> shared runtime | pass 41 mapped | `Inventory_ApplySlotUpdateAndDispatchItemAdded` 1403b8380 -> slot `Entity_GetIndexedSlotEntry` + `ItemEval_CommitPendingRuneData` + `ItemAdded` |
| Wire -> item-eval | pass 38 mapped | `ItemEval_SyncFromSharedItem` 140410300 copies runtime `+0x20`/`+0x158` into item-eval; `itemData+0x388`/`+0x518` writer still blocked |
| `FUN_14040f320` | `ItemRuneType_ToCompactSocketId` | RuneType 7-13 -> compact 1-7 for `0x0859` `IsNotFusion` (`14051ed80`) |
| `PrerequisiteType.Unknown92`-`93` | `ActiveSpellTargetMechanic` / `Spell4EffectCategoryOnUnit` | Client cases `0x5c`/`0x5d` on active spell-effect list |
| `PrerequisiteType.Unknown280` | `SpellTierOnCasterAndTarget` | Client case `0x118` requires caster **and** target to pass SpellTier helper `FUN_1404a4f60` |
| `PrerequisiteType.Unknown77` | `QuestObjectiveOnTarget` | Client case `0x4d` (+0x548) passes target context; `objectId0` is QuestObjective id |
| `PrerequisiteType.Unknown172` | `HealthScaled` | Client case `0xac` inline: `Entity_GetHealthScaleFactor` `14047a940` * health `+0xd08+0x8c`; fallback `Creature2_GetRowByUnitId` `14022d500` on `+0xd8` |
| `PrerequisiteType.Unknown174` | `ItemTradeSkillKnown` | `PrerequisiteTypeHandlerTable[174]` -> `Prerequisite_CheckItemTradeSkillKnown` `1404a17e0` -> `TradeSkill_ClientHasKnownItem2Id` `1403b91d0` (known-recipe list binary search); NPC types `0x14`/`0x17` |
| `PrerequisiteType.ItemTradeSkill` (173) | NF `PrerequisiteCheckItemTradeSkill` | Handler table `1404a1790` -> `TradeSkill_CheckItemTradeskillRequirement` `1403c16e0`; tier rank from `TradeskillTier` + `GetTradeskillXp` |
| `PrerequisiteType.ItemTradeSkillKnown` (174) | NF `PrerequisiteCheckItemTradeSkillKnown` | Handler table `1404a17e0` -> `TradeSkill_ClientHasKnownItem2Id` `1403b91d0`; `HasLearnedSchematic` on schematics referencing Item2 |
| `PrerequisiteType.Unknown178` | `ProgressTrackOnMatchingEntity` | Handler table[178] `1404a1890` + `Progress_GetTrackedScalar` `1403fa980`; NF uses challenge completion count when `objectId0` is a `Challenge.tbl` id (track table `+0x8010` still blocked) |
| `PrerequisiteType.TradeSkill` (175) | NF `PrerequisiteCheckTradeSkill` | Handler table stub; NF tier rank via `HasTradeskill` + `TradeskillTier` |
| `PrerequisiteType.ChallengeObject` (176) | NF `PrerequisiteCheckChallengeObject` | Handler table stub; NF `IsChallengeActivated` for `Challenge.tbl` id |
| `PrerequisiteType.TrueLevel` (177) | NF `PrerequisiteCheckPlayerGlobalTreeLookup` | Handler `1404a1830` + `1403d407b`; enum name unverified - rename blocked |
| `PrerequisiteType.CreatureDifficultyRank` (180) | NF `PrerequisiteCheckCreatureDifficultyRank` | Handler table stub; NF `RankValue` on target difficulty row |
| `PrerequisiteType.CreatureDifficulty` (179) | NF `PrerequisiteCheckCreatureDifficulty` | Spell path + NF manager; handler table[179] `1404a18f0` is PrimalMatrix lookup `1404d6a60` (documented mismatch) |
| `PrerequisiteType.PrimalMatrixNode` (292) | NF stub `PrerequisiteCheckPrimalMatrixNode` | Live vtable `+0x6c8`; handler table `_purecall`; allocation runtime blocked |
| `PrerequisiteType.LiveEventCount` (168) | `AccountItemCount` | Handler table[168] `Prerequisite_CheckAccountItemCount` `1404a1620` -> `AccountItem_CountOwnedByItem2Id` `1403d2140`; live case `0xa8` still vtable `+0x5a0` |
| `PrerequisiteType.LiveEvent169` (169) | `AccountItemCountCompared` | Handler table[169] `Prerequisite_CheckAccountItemCountCompared` `1404a1670`; NF `PrerequisiteCheckAccountItemCountCompared` |
| `PrerequisiteType.Unknown171` (171) | `DailyLoginDaysTotal` | Handler table[171] reads accountItemList+0x180 = DailyLogin Value0; NF `GetDailyLoginDaysTotal()` |
| `PrerequisiteType.Unknown96` | `EvalContextFloatByObjectId` | Handler table[96] `Prerequisite_CheckEvalContextFloatByObjectId` `14049f810`; active eval-context list key `+0x20`, float `+0x28`; NF item stat eval proxy |
| `PrerequisiteType.Unknown97`-`Unknown99` | `AccountItemListItem2CountNpc97` / `AccountItemListItem2CountNpc98` / `AccountItemListItem2CountNpc99` | Handler table entries `14049f8c0` / `14049f990` / `14049f9d0`; NPC type gate then `AccountItemList_WalkItem2IdMatch`; numeric suffix retained because no finer native distinction is proven |
| `PrerequisiteType.Unknown100`-`Unknown101` | `AccountItemListItem2Count100` / `AccountItemListItem2Count101` | Handler table entries `14049fa80` / `14049fac0`; same account Item2 row walk without NPC gate; numeric suffix retained because no finer native distinction is proven |
| `PrerequisiteType.Unknown223` | no enum rename | Raw PE vtable `PTR_FUN_140b67740+0x438` -> `14049f8c0`; same NPC-gated account Item2 row-count handler as type 97, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown233` | `SpellTierUnlocked` | Live vtable `+0x1a8` -> `Prerequisite_CheckSpellTierUnlocked` `14049d820`; `objectId0` Spell4 row, compares ability-book tier data to Spell4.tierIndex, ignores `value0`, Equal/NotEqual only |
| `PrerequisiteType.Unknown240` | no enum rename | Raw PE vtable `+0x2e8` -> `Prerequisite_CheckCurrency` `14049e7e0`; `objectId` currency id, `value` amount, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown241` | `HousingResidenceLoaded` | Live vtable `+0x638` -> `Prerequisite_CheckHousingResidenceLoaded` `1404a11e0`; NPC gate then Equal/NotEqual on active housing TargetResidence identity at world client `+0x7500/+0x7508` |
| `PrerequisiteType.Unknown186` | `HousingNeighborResidence` | Live vtable `+0x628` -> `Prerequisite_CheckHousingNeighborResidence_Table186` `1404a10e0`; NPC gate then Equal/NotEqual on current housing residence identity presence in cached neighbor map. Only retail row `17536` pairs `HouseOwnership` with type 186 for housing seed/relic/active-prop gates. |
| `PrerequisiteType.Unknown245` | no enum rename | Raw PE vtable `+0x698` -> `Prerequisite_CheckItemTradeSkill` `1404a1790`; NPC-gated Item2 tradeskill tier requirement, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown275` | no enum rename | Raw PE vtable `+0x6a0` -> `Prerequisite_CheckItemTradeSkillKnown` `1404a17e0`; row object ids correlate with AccountItem Item2 rows, but live semantics duplicate the known tradeskill item2 predicate |
| `PrerequisiteType.Unknown286` | no enum rename | Raw PE vtable `+0x688` -> `Prerequisite_CheckDailyLoginDaysTotal` `1404a1710`; accountItemList+0x180, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown287` | `DailyLoginRewardsAvailable` | Raw PE vtable `+0x690` -> `Prerequisite_CheckDailyLoginRewardsAvailable` `1404a1750`; accountItemList+0x184 |
| `PrerequisiteType.Unknown289` | no enum rename | Raw PE vtable `+0x3d0` -> `Prerequisite_CheckMount` `14049f0c0`; duplicate of type 84 body without proven finer semantic owner |
| `PrerequisiteType.Unknown290` | no enum rename | Raw PE vtable `+0x3a8` -> `Prerequisite_CheckUnderForcedMovement` `14049efa0`; duplicate of type 79 body without proven finer semantic owner |
| `PrerequisiteType.Unknown291` | no enum rename | Raw PE vtable `+0x6c0` -> `Prerequisite_CheckProgressTrackOnMatchingEntity` `1404a1890`; duplicate of type 178 body without proven finer semantic owner |
| `PrerequisiteType.Unknown293` | `AccountCurrencyAmount` | `InspectCodeAddress` at `14049d3a0`: for `objectId0 <= 18` reads `DAT_140c635f0+0x15d0+0xd0+objectId0*8`, otherwise compares zero, then `ApplyComparison`; account-item add/remove/update writers `140006000`/`140004c40`/`140005600` prove `+0xd0` slots are `AccountItem.accountCurrencyEnum` balances; row 44322 uses objectId0=15 Crimson Essence, value0=5000 |
| `PrerequisiteType.Unknown266` | `PetOrEsperPetEntity` | Corrected mapping: live case `0x10a` uses decimal vtable operand `200` (`+0xc8`) -> `14049cae0`; instruction window reads entity `+0x80`, subtracts `0x18`, and accepts values `0x18`/`0x19`, which NF `EntityType` maps to `Pet`/`EsperPet`. This is not matching/PvP candidate `14049e300`. |
| `PrerequisiteType.Unknown268` | `CREDDPendingOrderState` | Live case `0x10c` vtable `+0x3c8` -> `Prerequisite_CheckCREDDPendingOrderState_Table268` `14049f090`; compares `DAT_140c635f0+0x1708` CREDD pending-order flag to `objectId0`. Row `38851` is `NotEqual objectId0=1` and gates `creature2.prerequisiteIdVisibility` for CREDD Exchange NPCs in Thayd/Illium and the MTX Protostar consultant. |
| `PrerequisiteType.Unknown159` | `PositionalRequirementBetweenCasterAndTarget` | Client case `0x9f` dispatches vtable `+0x1e0` -> `14049d940`; body resolves `PositionalRequirement` from `objectId0`, computes angular cone using row `AngleCenter`/`AngleRange`, checks caster-vs-target bearing, and swaps facing source when row `Flags & 1`; retail row 6117 uses objectId0=6 (`AngleCenter=0`, `AngleRange=300`, `Flags=0`). |
| `PrerequisiteType.Unknown63` | `IsLocalPlayerEntity` | Client case `0x3f` dispatches vtable `+0x78` -> `Prerequisite_CheckIsLocalPlayerEntity` (`14049c7b0`); pass 140 created the formerly missing Ghidra function entry. The body compares evaluated entity `+8` identity to `DAT_140c65898+0x78` local-player entity `+8`, supports Equal/NotEqual only, and retail rows 3977/22213 are used by `VisualEffect.prerequisiteId` local-vs-nonlocal gates. |
| `PrerequisiteType.PublicEventParticipant166` (166) | `LiveEventTreeLookup` | Handler table[166] `1404a1580` -> LiveEventTree_LookupByObjectId 1404a7f50; NF requires NPC target + LiveEvent row (progress proxy 0) |
| `PrerequisiteType.LiveEvent167` (167) | `LiveEventWorldFactionBranch` | Handler table[167] `1404a15d0` -> 1404a80b0 + LiveEvent_GetWorldFactionField 1404a8430 (world 0xa6/0xa7 = Dominion/Exile fields +0x68/+0x88) |
| `PrerequisiteType.GameFormula` (170) live path | `Prerequisite_CheckEntitySetCountAt7188` @ 14049c880 | Counts NPC entities in player global +0x7188 set; distinct from tbl formula-id and handler-table +0x110 paths |
| `PrerequisiteType.HealthScaled` (172) | NF `PrerequisiteCheckHealthScaled` | Live case `0xac` inline scaled health; NF uses raw `IWorldEntity.Health` proxy; handler table[172] = `accountItemList+0x184` daily-login rewards |
| `PrerequisiteType.Unknown220` | `PathMissionChecklistItemComplete` | Live case `0xdc` vtable `+0x498` -> `Prerequisite_CheckPathMissionChecklistItemComplete_Table220` `14049fe80`; NF proxy checks completed missions as complete and otherwise tests persisted path mission `ProgressData` bit `value` for `value < 32`; per-type checklist/clue ownership remains blocked. |
| `PrerequisiteType.Unknown48` | no enum rename | Two retail rows: `10846` pairs `QuestObjectiveOnTarget` with type 48 but is unreferenced; `20451` is NotEqual zero and is referenced by `Spell4Effects.prerequisiteIdCasterPersistence` on dungeon raid-wipe timer spell 47945 effects. Live case `0x30` calls vtable `+0x1e8`, but that slot resolves to no-op stub `140001ba0`; semantic owner blocked. |
| `PrerequisiteType.Unknown142` | no enum rename | 9 retail rows; references are Spell4Effects caster/target apply gates for deprecated Spellslinger variants and Dangerous Game. Live case `0x8e` passes eval-context `param_2[4]` to vtable `+0x170`, but that slot resolves to no-op stub `140001ba0`; eval-context slot meaning remains blocked. |
| `Client0x0928` | blocked | Pass 130/pass 136/pass 137/pass 138 MCP/cache recheck: shared wire with `ClientPetSetStance` (`0x068E`) and `ServerPetStanceChanged` (`0x068F`) reader shape; `140070cc4` is the pet-stance registration literal, not a gameplay send, while `Pet_SetStance_SendClientPetSetStance` (`14050a270`) is the actual `0x068E` send witness. `0x068F` applies through `Pet_ApplyStanceChangedPayload` (`1403c0a80`) and is read back by `Pet_GetStance_ReadCachedStance` (`14050a130`), but `0x0928` remains registration-only at `1400a8524` with shared reader/apply helper `ServerUInt32UInt5_ReadPayload` (`14008ce80`); no opcode-specific owner surfaced. |
| `PrerequisiteType.Unknown189` (189) | no enum rename | Orphan label `Prerequisite_CheckPathSettlerHubBuildProgress` (`1404a01e0`) reads record+0x60, but live case `0xbd` dispatches a no-op stub and handler table[189] is `_purecall`; NF prerequisite handler removed until dispatch proof exists |
| `PrerequisiteType.Unknown192` (192) | no enum rename | Orphan label `Prerequisite_CheckPathSettlerHubContributionProgress` (`1404a0260`) reads record+0x68, but live case `0xc0` dispatches a no-op stub and handler table[192] is `_purecall`; NF prerequisite handler removed until dispatch proof exists |
| `PrerequisiteType.Unknown193` (193) | no enum rename | Orphan label `Prerequisite_CheckPathSettlerHubProgressPercent` (`1404a02e0`) reads record+0x64, but live case `0xc1` dispatches a no-op stub and handler table[193] is `_purecall`; NF prerequisite handler removed until dispatch proof exists |
| `accountItemList+0x110` (type 170 handler table) | blocked | `1404a16c0` compares scalar vs `value0`; `140022192` uses +0x108/+0x110; NF `GetAccountInventoryItemCount()` row-count proxy only |
| `PrerequisiteTypeHandlerTable` base | `0x140b67870` (not `0x140b66870`) | PE pointer scan for `1404a1620` / type **168** slot; pass 52 correction |
| `Client0x003D` | `ClientAccountRealmData` | `ClientAccountRealmData_WritePayload` @ `1400aba70` = one `AccountRealmData` row; selected call edges only show nesting in `Client0x0760_WritePayload`, and no selected send helper surfaced |
| `Client0x0760` | `ClientRealmListRealmRow` | `Client0x0760_WritePayload` @ `1400abd30` + `ServerRealmList_WritePayload` (`1400ac770`) realm row serializer; `1400ac2c0` is a row+trailing-bit wrapper, not a proven send-site |
| `Client0x0762` | `ClientRealmListMessageRow` | `Client0x0762_WritePayload` @ `1400ac410` is called by `ServerRealmList_WritePayload` (`1400ac770`); client send-site still blocked |
| Prerequisite handler evidence | pass 49/86/106 | Use **`0x140b67870` / `PrerequisiteTypeHandlerTable[typeId]`** for handler-table bodies, but correlate with the live `PrerequisiteManager_EvaluateTypeSlot` case and row data before renaming. Type 244/246 names are retained because pass 106 proved both live vtable slots and retail row semantics; do not rename from pointer evidence alone. |
| `AchievementEntry.Value` | `RequiredProgress` | Achievement progress runtime |
| `Quest2Entry.FactionLevelCompPreq*` | `FactionLevelRequireAtMostPreq*` | Quest prerequisite comparison semantics |
| `RewardRotationModifierEntry.Value` | `ModifierValue` | Reward rotation schedule builder |
| `ServerRewardRotationContentContext.UInt0`/`UInt1`/`UInt3`/`Flag` | **Keep neutral** | 2026-06-04 Ghidra MCP: `0x07CD` reader `14008fcb0` maps field order and `Reward_SendRewardUpdateRequest` `140636ba0` maps request-throttle slots, but no static apply helper links the row fields to slot assignment or flag semantics. Rename only after retail capture or dynamic dispatch proof. |
| Reputation packets `Value` | `ReputationDelta` / `ReputationAmount` | Packet shape tests |
| `GroupCharacter.UnknownStruct1` | `PrimeLevelInfo` / `PrimeLevels` | `MatchingPrimeLevelInfo_ReadPayload` @ `1400ad150` |
| `GroupCharacter.Unknown11`-`Unknown22` | `Health` ... `HealingAbsorbMax` (packed `ushort` vitals) | `GroupCharacter_ReadPayload` @ `140082420` `+0x54`..`+0x6a`; same set as `ServerGroupMemberStatUpdate` |
| `GroupCharacter.Unknown28`/`Unknown29` | `PhaseFlags1` / `PhaseFlags2` | Parsed `+0x94`/`+0x98`; matches `ServerGroupMemberStatUpdate` @ `140083d30` |
| Spell damage `UnknownStructure3` | `TrailingStructure` / `TrailingStructures` | `SpellDamageDescription_ReadPayload` @ `1400946c0`; row reader `SpellDamageTrailingRow_ReadPayload` @ `1400945e0` (seven damage uint32s + 3-bit tail; row consumer blocked) |
| `TrailingStructure.Unknown0`-`Unknown7` | `RawDamage` ... `GlanceAmount`, `DamageType` (3-bit) | Same field widths as parent `DamageDescription` via `SpellDamageTrailingRow_ReadPayload` @ `1400945e0`; per-row gameplay use blocked |
| `ServerSpellEffectNestedTargets` placeholders | `ServerUniqueId`, `Spell4EffectId`, `TargetId`, `DamageDescriptions` | Reader @ `140095810`; shared damage row @ `1400946c0`; structural diff vs `0x07F6` (`ServerSpellEffectDamage_ReadPayload` @ `140095910`) |
| `0x07F6` post-parse consumer | `SpellWrapper_ApplyServerEffectDamage` @ `1405a5800`, `SpellWrapper_DispatchEffectDamageCombatLog` @ `14053f3f0` | Via `Entity_ExecuteSpellEffectHighRange` jump case `1403ee268`; combat-log primary row only; trailing rows still blocked |
| `ServerP2PTradeUpdateItem` unknown tail | `RandomCircuitData`, `RandomGlyphData`, `ThresholdData`, `WorldRequirement_Item2Id`, `Item2IdMicrochip[5]` | Client opcode `0x018E` read `FUN_1400aa2b0` @ `1400aa2b0`, write `FUN_1400aa030` @ `1400aa030`; same tail as mail `ServerMailAvailable.Attachment` minus charges and eight glyph slots |

## Mapped - storefront purchase (`0x082A` / `0x0828`) - detail reference

Decompiled 2026-05-23 (`InspectCodeAddress` on `WildStar64.exe`).

**Wire order** (`Storefront_PurchaseCharacter_WritePayload` @ `1400acf80`):

| Index | Width | Client source (`StorefrontLib_PurchaseOffer` @ `1404f1150` -> `Storefront_SendClientPurchaseCharacterOffer` @ `140450720`) | Proposed name | Confidence |
|-------|-------|------------------------------------------------------------------------------------------------------------------|---------------|------------|
| `[0]` | u32 | Offer id (`uVar3`) | `OfferId` | Verified |
| `[1]` | 5-bit | `Game.Money` field at `+0x14` (`param_4`) | `PaymentCurrencySlot` | Implemented |
| `[2]` | u32 | `Game.Money` amount float bits (`param_5`) | `PurchaseMoneyAmountBits` | Implemented |
| `[3]` | 14-bit | Global table constant `*(DAT_140c7aac0+8)` | `CurrencyId` (NF property name) | Verified in handlers |
| `[4]` | u32 | Lua purchase arg 3 (`plVar13`) | `PurchaseOptionId` | Implemented |
| `[6..]` | Identity | Current player `+0x1a0/+0x1a8` | `Target` | Verified |
| `[10]` | u32 | Lua purchase arg 4 (`uVar5`) | `PurchaseExtensionId` | Implemented |

NF handlers currently use only `OfferId` and `CurrencyId` (14-bit field); other fields are client-side pricing/UI context.

Account route adds after shared payload: `AccountPurchaseExtensionId` (u32 at struct `+0x30`, same Lua arg 4 binding as `PurchaseExtensionId` but second wire slot), `AccountTarget`, `RecipientName` (`Storefront_PurchaseAccount_WritePayload` @ `140080a20`).

## Mapped - `ServerStoreOffers` offer row (`0x098B`)

`ServerStoreOffers_Offer_ReadPayload` @ `1400a0dc0` struct layout (client parse):

| Offset | Size | NF wire | Retail DB | Status |
|--------|------|---------|-----------|--------|
| `+0x00` | u32 | `Id` | `id` | Verified |
| `+0x08` | wstr | `Name` | `name` | Verified |
| `+0x10` | wstr | `Description` | `description` | Verified |
| `+0x18` | 8 | `PricePremium` / `PriceAlternative` | (derived in NF from prices table) | Verified |
| `+0x20` | u32 | `DisplayFlags` | `displayFlags` | Verified |
| `+0x28` | i64 | `RetailCatalogWireScalar` | `field_6` (`RetailStoreOfferWireConstants.CatalogWireScalarBits`) | **Mapped** - retail wire passthrough; Apply @ `14044b750` does not copy |
| `+0x30` | 8 bits | `RetailCatalogWireTrailingByte` | `field_7` (usually `0`) | **Mapped** - `FUN_14006be30` @ `1400a0dc0` |
| `+0x34` | u32 | currency row count | - | Verified |
| `+0x40` | u32 | item row count | - | Verified |

Do **not** rename to bare `Field6`/`Field7`; use `RetailCatalogWireScalar` / `RetailCatalogWireByte` for the documented passthrough.

## Blocked - per-field (closed without rename)

| Symbol | Evidence summary |
|--------|------------------|
| `PendingAccountItemGroup.Unknown2` | Wire `+0x10`; cache `[2]` @ `140005bf0`; lookup by `Id` only @ `140007810`; gift UI @ `140519260` uses group string at cache `+0x38` and UI target fields - not slot `[2]` |
| `GroupCharacter.StatBlockPrefix17` | 17-bit prefix at parsed `+0x1c` in stat block; copied to client `+0x98`; gameplay meaning blocked |
| `GroupMemberStatSlot.Value` | Five-row ushort in stat block; per-row semantics blocked |
| `GroupCharacter.Unknown10` | `uint32` after mentoring (`+0x50`); no labeled consumer |
| `ServerSpellCastResult.Unknown0` | Native reader `ServerSpellCastResult_ReadPayload` @ `140094fb0` proves `0x07FC` starts with a `uint32` before `18`-bit `Spell4Id` and `9`-bit `CastResult`. Pass 132 rechecked MCP/cache: the MCP bridge could not connect to the open project, cached call edges only show internal bit-reader calls, and no apply consumer or live echo capture proves the leading field is the echoed context token. Packet placeholder coverage now rejects `ContextToken` / `ClientContextToken` / `CastingId` aliases, so no field rename or echo behavior widening is safe. |
| `QuestObjectiveType.Unknown27` / `Unknown29` | No localization / single-digit table counts; no NF runtime handler |
| `PathMissionChecklistItemComplete` exact checklist runtime | Enum/check implemented with an NF proxy; exact per-path-type `+0x50` checklist/clue ownership remains blocked until path mission runtime state is modeled beyond completed-mission and persisted `ProgressData` bits. |
| `PrerequisiteType.Unknown260` | One retail row `36343` nests `OtherPrerequisite` -> row `17536` (`HouseOwnership` + `HousingNeighborResidence`) and type 260 Equal with `objectId1=12` (`HousingPlugItem` row flags `2`), but no imported `wildstar_client` prerequisite reference column points at row `36343`; generated DataMapping hits for `36343` are display/action/source ids, not prerequisite references. Live case `0x104` vtable `+0x640` -> `1404a1220` ignores row object/value and resolves active housing plot/plug state through `ClientDB_GetHousingPlotInfo` (`140205fa0`), current/default `ClientDB_GetHousingPlugItem` (`140206c60`), `HousingResidence_FindPlotEntryByPlotInfoId` (`1405aea10`), plug flags `0x08`/`0x20`, and active residence state fields vs state value `5`. State `5` is backed by native `HousingBuildComplete`: `HousingPlotEntry_DispatchBuildCompleteIfState5` (`1405a9920`) dispatches and resets plot-entry state `5`, while `HousingResidence_SetBuildState5AndDispatchComplete` (`1405a9980`) transitions residence state `4` -> `5`, clears handles, and dispatches completion. The active residence state fields and row/use-site reachability remain unnamed, so no enum rename. |
| Matching/rating candidate `14049e300` | Rejected as type 266: live prerequisite case `0x10a` uses decimal vtable operand `200` (`+0xc8`) -> `14049cae0`, not `+0x200`. `14049e300` remains orphan/unassigned until a caller or slot proves ownership. |
| `PrerequisiteType.ScanCreature` native path | Enum name remains table/text-backed, but stale labels were rejected: handler table[62] `14049e9a0` is now labelled `Prerequisite_CheckGroupIsRaid_Table62AndLive267`, not a scientist checklist walk; live case `0x3e` dispatches vtable `+0x4a8` -> raw `14049ff30`, which has scan-looking target bitmask fields but no safe semantic label yet. |
| `PrerequisiteType.GroupIsRaid` | Renamed from `Unknown267` in pass 114: live case `0x10b` vtable `+0x320` -> `Prerequisite_CheckGroupIsRaid_Table62AndLive267` `14049e9a0`; bit 1 at `*(DAT_140c65898+0x6c50)+8` maps to `GroupFlags.Raid`, and retail rows use `NotEqual 0` against that raid bit. |
| `Prerequisite_CheckScanCreature_LiveCase3E` (`14049ff30`) | Pass 88: live type 62 case `0x3e` vtable `+0x4a8`; target-unit scan bitmask predicate documented; NF still uses datacube-only `PrerequisiteCheckScanCreature`. |
| `PrerequisiteType.HousingNeighborResidence` (186) | Pass 95: `PrerequisiteCheckHousingNeighborResidence` proxies neighbor presence on player residence; native visit-context map membership still blocked. |
| `Prerequisite_CheckHousingPlotPlugState_Table260` (`1404a1220`) | Pass 95 plus 2026-06-05 MCP passes: diagnostic label for type 260 live body; lookup helpers `ClientDB_GetHousingPlotInfo` (`140205fa0`), `ClientDB_GetHousingPlugItem` (`140206c60`), and `HousingResidence_FindPlotEntryByPlotInfoId` (`1405aea10`) are mapped, but enum rename still blocked even though state `5` is backed by named native `HousingBuildComplete` helpers. |
| `HousingPlotEntry_DispatchBuildCompleteIfState5` (`1405a9920`) | 2026-06-05 MCP pass: plot-entry build-state helper; dispatches `HousingBuildComplete` when `+0xb8 == 5`, passes plot index+1, then resets state to `1`. |
| `HousingResidence_SetBuildState5AndDispatchComplete` (`1405a9980`) | 2026-06-05 MCP pass: residence/property build-state helper; transitions `+0xb8` from `4` to `5`, clears handles at `+0x60/+0x68`, and dispatches `HousingBuildComplete`. |
| `PrerequisiteType.Unknown144` | No `wildstar_client.prerequisite` rows found and live `PrerequisiteManager_EvaluateTypeSlot` skips case `0x90` (`0x8f` -> vtable `+0x3a0`, `0x91` -> inline Equal true). Handler table[144] `1404a0b70` is labelled only as `Prerequisite_CheckLiveEventStateEqualsOne_Table144`: it looks up `objectId` through `PublicEventService_GetLiveEventById` and tests runtime record `+0x1d0 == 1`, but reachability and field meaning are blocked, so no enum rename. |
| Handler table[5] `14049d470` | Renamed pass 100 to `Prerequisite_CheckEntityIdInEvalContextList_Table5Orphan` - walks eval-context `+0x2b8` list; **not** live type 5. Live `Reputation` = case `0x5` inline `entity+0x118` vtable `+0x20` + `PrerequisiteManager_ApplyComparisonFloat` `1404a2010`. |
| `PrerequisiteType.Unknown203` | Rows exist, but live case `0xcb` dispatches vtable `+0x348` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown222` | Rows `9470`/`9472` exist, but live case `0xde` dispatches vtable `+0x380` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown259` | Rows `39430`/`39431`/`39799`/`39800` use objectId0 `38923`/`38924`, but live case `0x103` dispatches vtable `+0x530` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown271` | Rows exist, but live case `0x10f` dispatches vtable `+0x178` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown272` | No retail rows found and live case `0x110` dispatches vtable `+0x180` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown278` | One row (`40531`, Equal `objectId0=1`, `value0=1`) is referenced by `Spell4.prerequisiteIdAoeTarget` on `[TEST] Origin/Target Prereqs - PBAE - Tier 3` (`Spell4` 84203). Live case `0x116` calls vtable `+0x300` on caster and target, but that slot resolves to no-op stub `140001ba0`; no semantic owner is proven. |
| `PrerequisiteType.Unknown283` | No retail rows found and live case `0x11b` dispatches vtable `+0x6a8` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown285` | Rows `41892`/`43208` exist, but live case `0x11d` dispatches vtable `+0x6b0` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown294` | One row (`44546`) is unreferenced in every `wildstar_client` column containing `prerequisite`; it uses `NotEqual` with all ids/values zero. Live case `0x126` dispatches vtable `+0x6d0` -> `140001ba0` no-op stub, and handler table[294] is `_purecall`; no semantic rename. |
| `PrerequisiteType.EvaluatedEntityPresent` | Renamed from `Unknown295` in pass 113: live case `0x127` inline NotEqual + evaluated entity present, value0 ignored; row `44550` is target-apply gate on Engineer Portable Black Hole Large Pull. |

## Blocked buckets (do not rename without new evidence)

| Bucket | Count | Notes |
|--------|------:|-------|
| `PrerequisiteType.UnknownNNN` | remaining | Table ids + generic failure strings only - **out of scope** |
| `Network.World` packet tails | ~400 refs | Many `Server0xNNNN` shapes decoded but event semantics open - **out of scope** |
| `Stat.Unknown*`, `InventoryLocation.Unknown*` | ~15 | No client stat/location enum strings |
| `EntityCreateFlag.Unknown08` | 1 | Used in cinematics; no label |
| Marketplace `AuctionInfo.Unknown2` | 1 | **Blocked tail.** 2026-06-04 source audit: 32-bit final `AuctionInfo` field, persisted as `marketplace_auction.unknown2`, cloned/loaded for preservation, default-zero on newly posted NF auctions, and no current `Game`/`WorldServer` semantic producer or consumer. Packet coverage pins round-trip only. |
| F-010 standalone `ServerRaidQueueStatus` queue producer | 1 surface | **Blocked producer/timing.** Pass 133 mapped the shared row fields through `0x071A` and `Group_DispatchRaidInfoResponse`; pass 134 found no cached static sender literal beyond registration, with `14008c010` still the only selected caller into the row reader. Do not invent queue-position/status aliases or non-zero `0x0718` emits until a live payload, native producer, or opcode-specific apply owner appears. |
| `ServerSpellCastResult.Unknown0` | 1 | **Blocked tail.** Native reader `140094fb0` proves a leading `uint32` in `0x07FC`, but apply/consumer semantics are not mapped; pass 132 cache edges add no apply owner. Do not infer echo behavior from nearby client request context-token fields alone. |
| Item packet offset names (`Unknown44`, etc.) | many | Layout known, semantics not |

## Mapped - `PendingAccountItemGroup` (`0x0976` / `0x0979`) - detail reference

| Offset | NF property | Notes |
|--------|-------------|-------|
| `+0x00` | `Id` | Pending item id |
| `+0x08` | `AccountItemId` | |
| `+0x10` | `Unknown2` | **Blocked** - stored in client cache `[2]`; grouped-map lookup uses `Id` at `+0x00` only (`AccountPendingItemGroupCache_LookupByPendingItemId` @ `140007810`); gift/claim UI paths read `TargetAccountId` at `+0x38`, not this field |
| `+0x18` | `Group` | Wide string key |
| `+0x20` | `SenderAccountId` | Implemented |
| `+0x28` | `SenderIdentity` | |
| `+0x38` | `TargetAccountId` | Implemented; account-gift target (`ClientAccountItemGiftPendingItemGroupToAccount`) |
| `+0x40` | `ClaimState` | 5-bit |
| `+0x48` | `HasTargetPlayerIdentity` | Wire u64; NF emits `0`/`1` |
| after | `TargetIdentity` | Second identity in 0x60 struct |

## Deferred (future passes - not part of closed initiative)

1. `PendingAccountItemGroup.Unknown2` - live `0x0979` capture with non-zero `+0x10`, or Lua/UI reading cache `[2]`.
2. `ServerStoreOffers.Offer.Unknown6`/`Unknown7` - any client path outside catalog Apply that consumes `+0x28/+0x30` (purchase history @ `14044c540` uses a different row layout).
3. `0x082A` purchase `[3]` 14-bit - live sniff vs `AccountCurrencyType` if handlers diverge from retail.
4. Group roster tail - decompile consumer for `GroupCharacter.Unknown10` (`uint32` @ parsed `+0x50` after mentoring).
5. Marketplace `AuctionInfo.Unknown2` - native auction row consumer, retail capture with a non-zero final uint32, or client UI/Lua reader proving the field owner.
6. Standalone `ServerRaidQueueStatus` queue semantics - live non-zero `0x0718` capture, native producer, or apply-table owner proving queue position/status timing beyond the shared raid-info row.
7. `ServerSpellCastResult.Unknown0` - `0x07FC` apply/consumer mapping or live cast-result capture that proves whether the leading uint32 echoes the client context token.

## Deferred - structurally mapped packet opcodes

| Opcode | Structure | Evidence | Blocker |
|--------|-----------|----------|---------|
| `Client0x011B` / `Client0x011D` / `Client0x012D` | empty / `uint32` / wide string | Pass 124 rechecked `0x011B`/`0x011D`: `TraceFunctionCallers 140001ba0 12` found 833 shared zero-payload helper refs, and `TraceFunctionCallers 14007d010 12` found 67 shared uint32 helper refs including `ClientCompoundTradeskillUInt32_WriteCluster`, but neither produced an opcode-specific gameplay sender or consumer. Pass 125 rechecked `0x012D`: `TraceFunctionCallers 14007ae80 18` found only shared registration/data refs, and narrowed exact opcode scans found `0x012D` only in registration plus unrelated arithmetic/offset uses; sibling senders exist for `0x0833` `ClientSuggest`, `0x0233` claim-pending, and `0x07C6` return-pending, so those names must not be inherited. Pass 94 comparison scan remains valid: native registration is confirmed at `Network_RegisterServerOpcode_0351` (`14006c290`) with `0x011B` -> zero-payload writer `140001ba0`, `0x011D` -> `ClientUInt32_ReadPayload` / `ClientTradeskillResetTalents_WritePayload`, and `0x012D` -> `ClientSuggest_WritePayload`. Non-registration hits are rejected: `0x011B`/`0x011D` in `LuaLexer_ReadNextToken`/parser functions are Lua token ids, `Movement_UpdateAndSendFallStateOpcodes` uses `GameFormula_GetEntryById(0x11B)`, and `SpellCast_SendClientCastSpellOrPosition` returns `CastResult.IllegalSpellCast` (`0x011D`) rather than sending opcode `0x011D`. | Gameplay sender/consumer or live sniff before semantic rename |
| `Client0x063E` | wide string | Pass 126 rechecked `0x063E`: native registration in `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) binds `0x063E` to shared `ClientSuggest_WritePayload` (`14007ae80`), proving only one wide string. `TraceFunctionCallers 14007ae80 18` found only shared registration/data refs, and narrowed exact opcode scans found `0x063E` only at registration literal `1400a821a`; sibling senders for `0x0833` support suggest, `0x0233` claim-pending, and `0x07C6` return-pending must not be inherited. | Native `0x063E` sender, post-read consumer, or live auth/marketplace/support capture before semantic rename |
| `Client0x0550` | `uint32` via `14007d010` | Pass 127 rechecked `0x0550`: native registration in `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) binds `0x0550` at `1400a824e` to `ClientUInt32_ReadPayload` (`14007d000`) / `ClientTradeskillResetTalents_WritePayload` (`14007d010`). `TraceFunctionCallers 14007d010 80` found the compound tradeskill cluster plus registration/data refs only, a narrowed exact opcode scan found `0x0550` only at registration literal `1400a824e`, and `DAT_140c1f210` static slot `140c247e0` is undefined for the message-id name rail. ICComm `0x0546`/`0x054B`, spell-list `0x0551`, matching replacement `0x05D5`, movement ack `0x0635`, tradeskill reset `0x0858`, ability-book `0x017A`, combat-log `0x0248`/`0x0249`, and P2P-trading `0x0192` must not be inherited. | Runtime indirect sender, post-read consumer, or live ICComm/spell-list/ability-book capture before semantic rename |
| `Client0x062A` / `0x0634` / `0x07E3` | `uint32` via `14007d010` | Pass 58 PE: sole opcode literals each in `ClientWorldOpcodeRegister_MovementSpline` (`1400a82b6`/`872c`/`8282`); `0x062A`/`0x0634` handlers are test-pinned log-only. Pass 128 MCP/cache rechecked `0x07E3`: registration at `1400a8282` binds size `4`, `ClientUInt32_ReadPayload`, shared `ClientTradeskillResetTalents_WritePayload`, and `ServerUInt32_ReadPayload`; the shared writer's selected code caller is still only `ClientCompoundTradeskillUInt32_WriteCluster`, and the `140c1ef80` selector-table reuse does not prove opcode ownership. Pass 131 MCP/cache rechecked `0x062A`/`0x0634`: `1400a82b6`/`1400a872c` remain registration-only, selected export has no `Network_SendOpcodePayloadHelper` sends for either opcode, `0x05D5` gameplay send is `MOV EDX,0x5D5` @ `14076ab61` + call @ `14076ab69` (not stale registration literal `140075918`), and `TargetSelection_SendClientMovementControlAck` @ `14057a630` is the labeled `0x0635` positive-control sender. | Indirect send rail, opcode-specific sender/consumer, or live sniff before semantic rename |
| `Client0x0701` | 2-bit + `uint32` | Pass 129 MCP/cache rechecked the registration row: `Network_RegisterServerOpcode_0351` (`14006c290`) binds `0x0701` to read-advance label `1400a69c0` (`+0x22` bits) and writer `ClientUInt2UInt32_WritePayload` (`1400a69d0`). Assembly at `140079de1`/`140079df0` loads the writer/read-advance callbacks, `MOV EDX,0x701` at `140079e05` sets the opcode, and `140079e13` calls the registrar. Writer xrefs are only those registration setup refs plus unxrefed raw pointer `140dd0a74`; no sender/consumer surfaced. | Gameplay sender, post-read consumer, callback owner, indirect send rail, or live capture before rename |
| `Client0x0928` | `uint32` + 5-bit field | Pass 130/pass 136/pass 137 MCP/cache rechecked `0x0928`: registration at `ClientWorldOpcodeRegister_MovementSpline` (`1400a8190`) / `1400a8524` binds read-advance `140089240`, shared writer `ClientUInt32UInt5_WritePayload` (`1400898b0`), and reader/apply helper `ServerUInt32UInt5_ReadPayload` (`14008ce80`). `ServerUInt32UInt5_ReadPayload` is also registered for `ServerPetStanceChanged` (`0x068F`) at `140071a36`, whose client apply path is `Pet_ApplyStanceChangedPayload` (`1403c0a80`); `ClientPetSetStance` (`0x068E`) registers the same writer at `140070cc4` and has the actual send witness at `14050a31d`. | Opcode-specific sender, post-read consumer, callback owner, indirect send rail, Lua/UI anchor, or live capture before rename |

## Verification (initiative closure)

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj `
  --filter "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~AccountInventoryPendingGroupTests|FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~StorefrontPurchaseHandlerTests" -v minimal --nologo
```

## Commands

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 0 `
  -ExtraPostScript InspectCodeAddress.java -ExtraPostScriptArgs @('1400acf80') -SkipCoverage
```

Log: `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`
