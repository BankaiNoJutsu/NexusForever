# Matching Implementation Status

Status date: 2026-06-05

This tracker closes matching work only when a surface is one of:

- **Implemented + verified**: runtime behavior exists and has focused tests or a client smoke result.
- **Mapped-only / blocked**: client or retail evidence is mapped far enough to explain the boundary, but runtime behavior is intentionally not guessed.
- **Rejected**: evidence shows the suspected behavior is not part of the matching surface.

Generated exports, logs, and client binaries remain local artifacts. Add durable labels only when a client function/consumer is mapped enough for the next pass to rediscover it.

## Implemented + Verified

| Surface | Status | Runtime notes | Verification |
| --- | --- | --- | --- |
| Group instance difficulty authority | Implemented + verified | `ClientGroupSetInstanceDifficulty` now routes through the group server; only the leader can update runtime group `InstanceDifficulty`, and the world update handler syncs cached group state plus online members before broadcasting `ServerGroupInstanceDifficultyResponse`. | `GroupInstanceDifficultyHandlerTests`; group-focused tests passed `155/155`; WorldServer and GroupServer builds passed. |
| Queue leave/requeue hardening | Implemented + verified | Missing queue state no longer throws on normal leave/remove paths; exposed queue collections are snapshots so all-queue leave/requeue status paths cannot enumerate a live dictionary while removal mutates it. | `MatchingCharacterStatusTests`; focused matching tests passed `90/90`. |
| Match cleanup member removal | Implemented + verified | `MatchCleanup()` snapshots each team's member list before calling `MatchLeave`, so cleanup can remove members from the same team collection without invalidating enumeration. | `MatchCleanupSnapshotTests`; focused matching tests passed `90/90`. |
| Role-check UI status | Implemented + verified | `ServerMatchingQueueStatus.ReadyMatchType` now comes from `IMatchingManager.GetReadyMatchType` and updates when role checks are created/removed. | `MatchingCharacterStatusTests`. |
| Match-ready response counts | Implemented + verified | Match-ready packets use pending/accepted proposal response counters instead of raw member counts. | `MatchProposalTeamTests`; existing proposal tests in focused slice. |
| Deserter UI packet | Implemented + verified | Deserter sync now emits `ServerMatchingPenaltyUpdated` with per-match-type PvE/PvP remaining times instead of unrelated kick cooldown packets. | `MatchingDeserterManagerTests`. |
| Deserter restart persistence | Implemented + verified | Active deserter state is persisted in `character_matching_penalty`, restored on login/lazy queue checks, and removed when cleared or expired. | `MatchingDeserterManagerTests`. |
| Queue-left UI sync | Implemented + verified | Authoritative queue removal now emits `ServerMatchingLeftQueue` before the refreshed `ServerMatchingQueueStatus`. | `MatchingCharacterStatusTests`. |
| Flexible role selection | Implemented + verified | The old `1 tank / 1 healer / 3 DPS` reducer was removed. Dungeon/adventure role validation now preserves each player's selected role mask and rejects `Role.None` or undefined role bits; full groups are not rejected solely for non-trinity composition. | `MatchingRoleEnforcerTests`. |
| Raid-info row field closure | Implemented + verified / standalone blocked | `ServerRaidQueueStatus` keeps the compatibility zero emission from raid-info, but its shared `0x0718` row fields now use the `0x071A` raid-info names proven by `ServerRaidInfoResponse_ReadPayload` (`14008c010`) and `Group_DispatchRaidInfoResponse` (`1406042b0`): `SavedInstanceId`, `WorldId`, `DateExpireUTC`, `DaysUntilExpire`, and `PrimeLevel`. Pass 134 found no static standalone `0x0718` sender literal beyond registration, and the only selected caller into `ServerRaidQueueStatus_ReadPayload` remains `14008c010`; queue-only aliases and non-zero `0x0718` producer timing remain blocked. | `GroupPacketShapeTests`; `ClientRaidInfoRequestHandlerTests`; `PacketPlaceholderNamingTests`. |
| Replacement role-mask guard | Implemented + verified | `ClientMatchingMatchInitiateLookingForReplacements` and `ClientMatchingStopLookingForReplacements` validate in-progress match membership; initiate accepts only native-emitted role bits `0..2` (`0x07`, locally `Tank/Healer/DPS`) and both handlers log without starting a server backfill queue or emitting blocked replacement/status packets. | `MatchingLookingForReplacementsValidationTests`. |
| `Client0x062A` / `Client0x0634` diagnostic boundary | Implemented + verified | Both handlers remain log-only and do not emit queue or raid state while native sender semantics are registration-only. | `ClientUnresolvedDiagnosticHandlerTests`; diagnostic-focused slice passed `93/93`. |
| Average wait UI (`0x0628`) | Implemented + verified | `ServerMatchingAverageWaitTimeUpdate` emitted on queue join, after non-solo pop samples queue time, and on login for active queues; EasyMatchMaker listens via `MatchingAverageWaitTimeUpdated`. | `MatchingAverageWaitTimeTests`; addon corpus `ADDON_CORPUS_EVIDENCE.md`. |
| Warplot surrender winner | Implemented + build-covered | Surrender completion records the surrendering team and awards victory to the opposite team. | Covered by focused matching build; direct match-state test still needs a heavier PvP match fixture. |
| Vote rejection packets | Implemented + build-covered | Failed vote-kick/surrender initiate/cast paths now return the matching vote failure packet unless the personal cooldown packet is more specific. | Covered by focused matching build; packet-sequence fixture is still a useful follow-up. |

Prior focused verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~Matching|FullyQualifiedName~ClientRaidInfoRequestHandlerTests"
```

Result: passed `50/50`.

Latest replacement-boundary verification (2026-06-03):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~MatchingLookingForReplacementsValidationTests|FullyQualifiedName~MatchingPacketShapeTests|FullyQualifiedName~ClientRaidInfoRequestHandlerTests"
```

Result: passed `40/40` after MSBuild retried a transient locked testhost DLL.

Latest matching lifecycle hardening verification (2026-06-03):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Matching" -v minimal --nologo
```

Result: passed `90/90`.

Latest group instance-difficulty authority verification (2026-06-03):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Group" -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo
dotnet build Source\NexusForever.Server.GroupServer\NexusForever.Server.GroupServer.csproj --no-restore -v minimal --nologo
```

Results: group-focused tests passed `155/155`; both builds succeeded with `0` warnings and `0` errors.

Latest shared one-flag matching cluster refresh (2026-06-05):

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript InspectCodeAddress.java -ExtraPostScriptArgs @('1400807f0')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript TraceFunctionCallers.java -ExtraPostScriptArgs @('1400807f0','12')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript FindOpcodeComparisons.java -ExtraPostScriptArgs @('05B0','05CC','05F1','64')
```

Result: passed. `LAB_1400807f0` is a local reader thunk between
`ServerUInt32PairArray_ReadPayload` and `ServerDailyLoginUpdate_ReadPayload`,
with no containing function at that exact address. It null-checks the payload
target, loads `R8D = 1`, and jumps to `14006c090`. `TraceFunctionCallers`
reported `27` references, all registration/data references rather than
gameplay callers. The meaningful opcode-specific comparison hits were the
registration rows in `Network_RegisterServerOpcode_0351` for `0x05B0`,
`0x05CC`, and `0x05F1`; the wider immediate scan saturated on unrelated struct
offsets. Current NexusForever models therefore stay mapped as shared one-flag
wire surfaces only. `0x05CC.Ally` and `0x05F1.RolesRequired` remain local
compatibility semantics until a client consumer or live matching capture proves
flag meaning and emit timing.

Latest matching apply-helper xref rejection (2026-06-05):

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript FindDataReferences.java -ExtraPostScriptArgs @('--maxRefs','16','1405c0e90','1405c1850','1405c21d0','1405c2440','1405c24a0')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript TraceFunctionCallers.java -ExtraPostScriptArgs @('1405c0e90','10')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140e1e2c4','16')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript FindVtableSlotReferences.java -ExtraPostScriptArgs @('11','1405c0e90','140b40000','140e00000','16')
```

Result: passed. `MatchingManager_ApplyMatchingRoleCheckStarted` @ `1405c0e90`
sets matching-manager `+0xa4`, dispatches `MatchingRoleCheckStarted`, and
passes `payload[0]` to the event, so it remains a semantic candidate for the
`0x05F1` flag. It is still not opcode-owned: `TraceFunctionCallers` found only
one data ref at `140e1e2c4` with no instruction, `DumpNearbyData 140e1e2c4`
places that address inside `_IMAGE_RUNTIME_FUNCTION_ENTRY[46407]`, and the
vtable-slot scan for slot `0x58` found `matches=0`. The adjacent event helpers
`1405c1850`, `1405c21d0`, `1405c2440`, and `1405c24a0` also have exactly one
data ref each, all in the same PE `.pdata` range (`140e1e3b4`, `140e1e48c`,
`140e1e4a4`, `140e1e4b0`). These helpers can inform live-capture expectations,
but the `.pdata` refs are rejected as matching dispatch or opcode-index proof.

Latest WorldSocket slot-11 matching dispatcher rejection (2026-06-05):

```powershell
Import-Csv .\Decomp\Analysis\function_pointer_family_slots.csv |
  Where-Object { $_.function_entry -in '1405c0e90','1405c41c0','1405c1850','1405c21d0','1405c2440','1405c24a0' }
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -RunId f010_slot11_fortune_positive -ExtraPostScript FindVtableSlotReferences.java -ExtraPostScriptArgs @('11','1404d60f0','140b40000','140e00000','16')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -RunId f010_slot11_matching_05cf_candidate -ExtraPostScript FindVtableSlotReferences.java -ExtraPostScriptArgs @('11','1405c41c0','140b40000','140e00000','16')
```

Result: passed. `WorldSocket_ProcessServerMessage` @ `140014f10` deserializes
server messages, handles AccountInventory/Storefront fast paths, then walks
callback-view `param_1+0x14b0` (native `WorldSocket+0x15b0`) and calls each
node's `vtable+0x58` apply slot before advancing through `node+0x20`.
`WorldSocket_FilterServerMessageHandlers` @ `140014d30` walks the same chain's
`vtable+0x50` filter slot before deserialize. The slot scanner finds the
positive-control Fortune route: vtable pointer `140b690f0`, slot cell
`140b69148`, and target `FortuneNode_ApplyServerFortunePackets` @ `1404d60f0`
with `matches=1`. The same slot scan returned `matches=0` for matching helpers
`1405c0e90`, `1405c41c0`, `1405c1850`, `1405c21d0`, `1405c2440`, and
`1405c24a0`; the pointer-family inventory also has zero rows for those helper
entries. This rejects the known `WorldSocket+0x15b0` / `vtable+0x58` route as
the F-010 matching dispatcher. Matching still needs a different dispatcher/index
outside exported vtable slots, or a live queue / role-check / match-ready
capture that ties the packet values to client state.

Latest `ServerMatching0x05CF` apply-cell xref refresh (2026-06-05):

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript FindDataReferences.java -ExtraPostScriptArgs @('--maxRefs','12','1405c41c0','140e1e66c','140e1e660','140e1e228','140099110')
```

Result: passed. `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0`
has exactly one data reference at `140e1e66c`; at this point that address was
still being tested as a possible table cell, and the follow-up below rejects
that interpretation. `ServerUInt32_LocalReadThunk` @ `140099110` has only
registration-block references in `Network_RegisterServerOpcode_0351`, consistent
with the known shared raw-uint32 rows for `0x05CF` and `0x085D`. This keeps
`ServerMatching0x05CF` mapped-only / blocked; no semantic rename or emit is safe
until a real apply dispatcher/index or a live packet witness ties `0x05CF` to
the candidate and manager field.

Latest `ServerMatching0x05CF` unwind-metadata false-lead rejection (2026-06-05):

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140e1e228','32')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140e1e66c','8')
```

Result: passed. Both `140e1e228` and `140e1e66c` are contained by Ghidra's
`_IMAGE_RUNTIME_FUNCTION_ENTRY[46407]` array (`140dc7000`), i.e. PE `.pdata`
exception/unwind metadata. The `0x0C` spacing that made nearby addresses look
like matching apply-table cells is the runtime-function-entry stride, not an
opcode dispatch structure. The `1405c41c0` function remains a correlated
one-uint32 matching-manager apply candidate by behavior, but the `140e1e66c`
xref is rejected as opcode-link evidence. The next static unblocker must find a
real dispatcher/index outside `.pdata`, or a live `0x05CF` capture must prove
the manager `+0xa0` field.

Latest `ServerMatching0x05CF` MCP dispatcher recheck (2026-06-05):

```text
MCP batch_decompile:
  1405c41c0, 140099110, 14006c290, 140014f10, 1405c4140, 1405c3d30,
  1405c0e90, 1405c39f0, 1405c3af0, 1405c4260
MCP get_bulk_xrefs:
  1405c41c0, 140099110, 1405c0e90, 1405c39f0, 1405c3af0,
  1405c4140, 1405c3d30, 140e1e66c, 140e1e660, 140e1e63c, 140e1e228
MCP audit_globals_in_function 1405c41c0
MCP audit_global 140e1e66c / 140e1e660
MCP get_assembly_context 1400759f5,140075a07,14007947d,14007948f
```

Result: still blocked. `MatchingManager_ApplyManagerUInt32Field0xA0`
(`1405c41c0`) writes `payload[0]` to matching-manager `+0xa0`, mirrors it to a
looked-up sub-object at `+0x98` when present, then calls `FUN_1400a8020`; MCP
global audit finds only broad manager globals `140c65b98` and `140c65898`.
There is still no `ClientEvent` edge, opcode comparison, vtable slot, or
non-`.pdata` table cell tying this helper to `0x05CF`. MCP audits of
`140e1e66c` / `140e1e660` report untyped zero-xref data locations, matching the
prior PE `.pdata` rejection. `ServerUInt32_LocalReadThunk` (`140099110`) remains
registration/shared structural evidence only: assembly at `1400759f5` /
`140075a07` installs it for `0x05CF`, while `14007947d` / `14007948f` installs
the same thunk for `0x085D`. No semantic rename or producer emit is safe.

Next static unblocker: recover an actual matching apply dispatcher/index outside
PE `.pdata` and outside the rejected `WorldSocket+0x15b0` slot-11 route.
Otherwise, use a live queue / match-ready capture that correlates `0x05CF` with
neighboring `0x05CA` / `0x05CC` state changes.

Latest `ServerMatching0x05CF` tracker reconciliation (2026-06-04):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MatchingPacketShapeTests|FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f010-matching-05cf\
```

Result: passed `98/98`.

Latest diagnostic-opcode boundary verification (2026-06-04):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ClientUnresolvedDiagnosticHandlerTests|FullyQualifiedName~ClientDiagnosticPacketShapeTests|FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~ClientRaidInfoRequestHandlerTests" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f010-diagnostics\
```

Result: passed `93/93`.

Latest raid-queue wire-order verification (2026-06-04):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~GroupPacketShapeTests" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f010-raid-queue\
```

Result: passed `20/20`.

Latest raid-info row field verification (2026-06-05):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~ClientRaidInfoRequestHandlerTests|FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo --artifacts-path I:\GIT\NexusForever\artifacts\dotnet-test-pass133
```

Result: passed, 108/108. This slice pins the shared raid-info row names and keeps standalone queue aliases blocked.

Prior packet/evidence refresh verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~Matching|FullyQualifiedName~ClientRaidInfoRequestHandlerTests|FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~ClientDiagnosticPacketShapeTests"
```

Result: passed `84/84`.

Latest full verification (2026-05-23 evidence-backed rollback):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
dotnet build Source\NexusForever.sln -v minimal --nologo
```

Results: test project passed `943/943`; solution build succeeded with `0` warnings and `0` errors.

## Mapped-only / Blocked

| Surface | Current evidence | Blocker before implementation |
| --- | --- | --- |
| `ServerMatchingManagerFlag` / `ServerMatchingMatchParticipantCountUpdate` / `ServerMatchingRoleCheckStarted` | Native server registration binds `0x05B0`, `0x05CC`, and `0x05F1` to shared reader slot `LAB_1400807f0` with registered size 4. `InspectCodeAddress 1400807f0` shows a tiny local thunk that sets `R8D = 1` before jumping to `14006c090`; `TraceFunctionCallers` found registration/data refs only. The opcode comparison scan only produced useful hits for the three registration rows (`140075bd2`, `140075cf2`, `140075db6`) and then saturated on unrelated offsets. `MatchingManager_ApplyMatchingRoleCheckStarted` @ `1405c0e90` consumes `payload[0]` and dispatches `MatchingRoleCheckStarted`, but its only direct ref is `140e1e2c4` in PE `.pdata`, and the `WorldSocket+0x15b0` slot-11 scan found no `vtable+0x58` entry for it while finding the Fortune positive control. NexusForever currently emits `0x05CC.Ally` and `0x05F1.RolesRequired`, but native proof only establishes the shared one-flag wire shape plus a non-opcode-owned semantic candidate. | Recover the client apply/consumer path or live queue/role-check capture proving exact flag semantics and emit timing before renaming fields further, widening behavior, or using these packets as semantic evidence for `0x05CF`. |
| `ServerMatching0x05CF` | `Network_RegisterServerOpcode_0351` @ `14006c290` registers opcode `0x05CF` with size 4 and reader `ServerUInt32_LocalReadThunk` @ `140099110` (same thunk as `ServerTradeskillSigilResult` @ `0x085D`, different opcode slot). Matching-manager probes found correlated candidate `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0`; it writes the payload to manager `+0xa0` and refreshes UI state, but no opcode-to-consumer index or live packet witness ties it uniquely to `0x05CF`. The 2026-06-05 `FindDataReferences` refresh found `1405c41c0` referenced only by `140e1e66c`, and the follow-up `DumpNearbyData` pass proved `140e1e66c` / `140e1e228` are PE `.pdata` `_IMAGE_RUNTIME_FUNCTION_ENTRY` entries, not matching dispatch cells. The later `WorldSocket+0x15b0` slot-11 scan found the Fortune positive control but no `vtable+0x58` match for `1405c41c0`; the MCP recheck found only broad manager globals `140c65b98` / `140c65898` in the helper, while `140099110` remains registration-only/shared with `0x085D`; `selected_call_edges.csv` still shows no `ClientEvent_DispatchNamedEvent` edge. No NexusForever emit. | Recover a real apply dispatcher/index outside PE `.pdata` that binds `0x05CF` to `1405c41c0`, or capture live `0x05CF` during match-ready / participant-count / queue UI flows and correlate with `0x05CA`/`0x05CC`. |
| `Client0x062A` / `Client0x0634` | `ClientWorldOpcodeRegister_MovementSpline` @ `1400a8190` registers both as 4-byte generic uint32 payloads via `ClientTradeskillResetTalents_WritePayload` @ `14007d010` / read @ `14007d000`. `MOV R8D` and `MOV EDX` probes return registration-only hits (`1400a82b6` for `0x62A`). `TraceFunctionCallers` on `14007d010` and `14007dc80` show no gameplay send path. NexusForever handlers are now test-pinned as log-only with no queue-state emit. | Native sender via `14076aa30`-style UI cluster trace or live sniff tying value to matching/queue UI. |
| `ServerRaidQueueStatus` / `ServerRaidInfoResponse` row | `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` reads the shared 0x20-byte row. Adjacent `ServerRaidInfoResponse_ReadPayload` @ `14008c010` reads a 32-bit row count and array of those rows for opcode `0x071A`; `Group_DispatchRaidInfoResponse` @ `1406042b0` maps offsets to `SavedInstanceId`, `WorldId`, Windows FILETIME `DateExpireUTC`, float `DaysUntilExpire`, and `PrimeLevel`. Runtime still emits only zero-value `0x0718` compatibility state from raid-info. Pass 134 cache probes found exact `0x0718` hits only in registration/reader surfaces and no selected static sender path. | Standalone non-zero `0x0718` queue producer timing and queue-position/status aliases. |
| Replacement queue fill | **Mapped client surface only (2026-05-23 rollback):** `0x05D5` and `0x0602` are handled as validation/logging surfaces. Addon corpus: EMM `MatchLookingForReplacements`; public AddOn Studio API events also list `MatchLookingForReplacements` / `MatchStoppedLookingForReplacements`. Runtime anchor queues, accepted replacement merges, and close timing were removed because the available evidence does not prove server producer behavior. | Retail sniff/native evidence for queue anchor timing, multi-replacement sequencing, accepted replacement merge, and teleport into the running instance. |
| Partial PUG role composition | The shipped `MatchMaker.lua` UI exposes DPS/Tank/Healer toggles from `MatchMakingLib.GetEligibleRoles()`, stores selected values in `tQueueOptions[*].arRoles`, requires at least one selected role for non-solo non-arena queues, and passes the option table to `MatchMakingLib.Queue` / `QueueAsGroup`. It does not prove a server-side 1/1/3 reducer. | Native/server-side evidence or live captures showing exact partial-group fill policy. |
| Manager flag / group-is-queued / list-players broadcasts | `QueueJoin`, `LeftQueue`, `QueueStatus`, `PenaltyUpdated`, and `AverageWaitTimeUpdate` are runtime-emitted; manager-flag and list-players timing still incomplete. | Client consumer mapping or retail captures for remaining opcodes in `0x05B0..0x0628`. |
| PvP rating, rewards, leaderboards | Matching packets can be shaped, but formulas and award triggers are outside current evidence. | Retail formula evidence or UI packet dependency requiring implementation. |
| Random map/team selector parity | Existing selector is conservative compatibility behavior. | Table/client evidence for random map and team selection rules per match type. |

## Rejected / Out of Scope

| Surface | Reason |
| --- | --- |
| Renaming diagnostic packet models without sender proof | Cosmetic names would weaken the evidence ladder and make future packet work harder to audit. |
| Guessing rating/reward formulas | Does not block matching queue UI correctness and lacks retail formula evidence. |
| Committing generated decompile exports/logs | Repo policy keeps generated/local artifacts out of source control. |

## Next Evidence Targets

1. Map client consumers/senders for opcodes `0x05B0..0x0628`, **`ServerMatching0x05CF` (`0x05CF`)**, `Client0x062A`, `Client0x0634`, and `ServerRaidQueueStatus`.
2. Capture or decompile the replacement-flow sequence: request replacement, queue status, ready accept, add to existing match, and cleanup.
3. Add a focused PvP match fixture so warplot surrender and vote packet sequences can be verified without a live client.
4. Run build-16042 client smoke once local dependencies/client are available: solo queue, party queue, role check, ready accept/decline, enter/leave/requeue, replacement fill, vote kick, warplot surrender, deserter denial/allowance, and relog status restoration.
