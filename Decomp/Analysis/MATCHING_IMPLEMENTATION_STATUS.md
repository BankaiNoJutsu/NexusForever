# Matching Implementation Status

Status date: 2026-06-06

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
| Raid-info row field closure | Implemented + verified / standalone blocked | `ServerRaidQueueStatus` keeps the compatibility zero emission from raid-info, but its shared `0x0718` row fields now use the `0x071A` raid-info names proven by `ServerRaidInfoResponse_ReadPayload` (`14008c010`) and `Group_DispatchRaidInfoResponse` (`1406042b0`): `SavedInstanceId`, `WorldId`, `DateExpireUTC`, `DaysUntilExpire`, and `PrimeLevel`. Pass 157 retried MCP (`Transport closed`) and found no static standalone `0x0718` sender literal beyond registration; direct-plugin xrefs keep `14008c010` as the only real code caller into `ServerRaidQueueStatus_ReadPayload`, and extra data refs resolve to unowned metadata / PE `.pdata`-shaped entries rather than queue ownership. | `GroupPacketShapeTests`; `ClientRaidInfoRequestHandlerTests`; `PacketPlaceholderNamingTests`. |
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

Latest live group/raid/matching CDB probe (2026-06-06):

```powershell
Start-Process powershell.exe -Verb RunAs -ArgumentList @(
  '-NoExit',
  '-ExecutionPolicy',
  'Bypass',
  '-File',
  'I:\GIT\NexusForever\artifacts\live-prereq-debug\Run-GroupRaidCdbProbe.ps1'
)
```

Artifacts:

- CDB log: `artifacts/live-prereq-debug/group_raid_matching_cdb_20260606_002604.log`
- WorldServer log: `Source/NexusForever.WorldServer/bin/Debug/net10.0/logs/NexusForever.WorldServer_20260605_44724.log`
- Widened follow-up CDB log:
  `artifacts/live-prereq-debug/group_raid_matching_cdb_admin_wide_20260606_110421.log`
- Widened follow-up WorldServer log:
  `Source/NexusForever.WorldServer/bin/Debug/net10.0/logs/NexusForever.WorldServer_20260606_31056.log`

Result: useful live correlation, no source behavior change. The CDB probe hit
`Matching_QueueDispatchFromUi` (`14076c830`) nine times and
`ClientMatchingQueue_WritePayload` (`140098a70`) nine times; the writer hits
had `r8=0x05ef`, matching WorldServer receives of
`ClientMatchingQueue(0x000005EF)`. The corresponding server rows include
Dungeon map `70` and WorldStory maps `102`/`103`, with role masks observed as
Tank, DPS, Tank+DPS, and SoloMatch/non-SoloMatch queue flags depending on the UI
selection. Queue cancellation used `ClientMatchingQueueLeaveAll(0x000005B4)`
and the server responded with `ServerMatchingQueueResultAnnounce`,
`ServerMatchingLeftQueue`, and refreshed `ServerMatchingQueueStatus`.

The widened follow-up added generic send-helper filters and confirmed the
client-owned helper path: `Network_SendOpcodePayloadHelper` hit for `0x05EF`
eleven times and `0x05B4` nine times, while `ClientMatchingQueue_WritePayload`
hit eleven times with `r8=0x05ef`. WorldServer receive rows around
`46301..49464` show the same queue/leave-all pairs and queue result/status
responses.

The solo queue flow also produced three match-ready prompts. WorldServer emitted
`ServerMatchingMatchReady(0x000005CA)`, CDB hit
`MatchingManager_ApplyMatchingGameReady` (`1405c39f0`), then responding to the
ready popup hit `MatchingManager_SendClientGameReadyResponse` (`1405c3500`).
WorldServer received `ClientMatchingGameReadyResponse(0x000005C8)` with both
false declines and one true accept; the accept path completed
`ServerMatchingMatchJoined`. This upgrades the existing queue / match-ready /
ready-response labels from static mapped to live-correlated.

Negative evidence from these solo passes matters: no CDB hit or server log
witness was captured for `ServerMatching0x05CF`,
`ServerMatchingGroupMemberRoleSelection` (`0x0600`), `Client0x062A`,
`Client0x0634`, standalone non-zero `ServerRaidQueueStatus` (`0x0718`),
`ClientRaidInfoRequest` (`0x0719`), replacement LFR start/stop, or transfer into
match. Those surfaces remain mapped-only / blocked and are not rename-grade
from solo queue smoke.

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

Latest `ServerMatching0x05CF` MCP bridge retry and dispatcher recheck (2026-06-05):

```text
mcp__ghidra_mcp.list_instances -> Transport closed
mcp__ghidra_mcp.connect_instance project=NexusForeverClient64_WildStar64 -> Transport closed

Cache/direct-plugin decompile and xref pass:
  1405c41c0, 140099110, 14006c290, 140014f10, 1405c4140, 1405c3d30,
  1405c0e90, 1405c39f0, 1405c3af0, 1405c4260
Direct-plugin xrefs:
  1405c41c0, 140099110, 1405c0e90, 1405c39f0, 1405c3af0,
  1405c4140, 1405c3d30, 140e1e66c, 140e1e660, 140e1e63c, 140e1e228
Direct-plugin audit_global 140e1e66c / 140e1e660
Selected-cache assembly/context check 1400759f5,140075a07,14007947d,14007948f
```

Result: still blocked. The Ghidra MCP bridge is visible to Codex but closes the
transport during instance discovery/connect, so this pass used the existing
WildStar64 export cache and direct-plugin xref/audit endpoints for the evidence
below. `MatchingManager_ApplyManagerUInt32Field0xA0`
(`1405c41c0`) writes `payload[0]` to matching-manager `+0xa0`, mirrors it to a
looked-up sub-object at `+0x98` when present, then calls `FUN_1400a8020`; the
candidate helper only references broad manager globals `140c65b98` and
`140c65898`.
There is still no `ClientEvent` edge, opcode comparison, vtable slot, or
non-`.pdata` table cell tying this helper to `0x05CF`. Direct-plugin audits of
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

Latest standalone `ServerRaidQueueStatus` producer recheck (2026-06-05 pass 157):

```text
mcp__ghidra_mcp.list_instances -> Transport closed
mcp__ghidra_mcp.connect_instance project=NexusForeverClient64_WildStar64 -> Transport closed

Direct-plugin/cache targets:
  14008bf80 ServerRaidQueueStatus_ReadPayload
  14008c010 ServerRaidInfoResponse_ReadPayload
  1406042b0 Group_DispatchRaidInfoResponse
  14006c290 Network_RegisterServerOpcode_0351
```

Result: mapped-only / blocked. `Network_RegisterServerOpcode_0351` still
registers `0x0718` size `0x20` with `ServerRaidQueueStatus_ReadPayload`
(`14008bf80`) and adjacent `0x071A` size `0x10` with
`ServerRaidInfoResponse_ReadPayload` (`14008c010`). The row reader still reads
`uint64`, 15-bit `uint32`, `uint64`, `uint32`, and `uint32`; the list reader
still reads a count, allocates `count * 0x20`, and calls `14008bf80` for each
row.

Direct-plugin xrefs keep `14008c010` as the only code caller into `14008bf80`.
The other refs are data: `140dcebd8`, registration setup at `1400717c4` /
`1400717d6`, and for `14008c010` anonymous cells `140b98c50` / `140b98c60`.
`audit_global` reports those cells as untyped zero-xref data, and raw memory
around `140b98c50` shows anonymous pointer metadata, not an owned opcode table.
`Group_DispatchRaidInfoResponse` (`1406042b0`) remains the `RaidInfoResponse`
named-event consumer, but its direct-plugin refs (`140e22bcc`, `140bf08e8`,
`140bf092c`) are also untyped zero-xref data; memory around `140bf08e8` and
`140e22ba0` has PE `.pdata`-style runtime-function-entry triples for
`1406042b0`, not dispatch ownership. Exact selected send-helper scans found no
`Network_SendOpcodePayloadHelper`, `Network_SendMessageById`, or
`Network_SerialiseBufferedMessageById` hit for `0x0718` / decimal `1816`.

No field rename, opcode rename, producer emit, or label change is safe from
this pass. The next evidence source remains a live non-zero `0x0718` payload or
a real non-`.pdata` apply/consumer table that binds standalone `0x0718` to a
queue-state update.

## Mapped-only / Blocked

| Surface | Current evidence | Blocker before implementation |
| --- | --- | --- |
| `ServerMatchingManagerFlag` / `ServerMatchingMatchParticipantCountUpdate` / `ServerMatchingRoleCheckStarted` | Native server registration binds `0x05B0`, `0x05CC`, and `0x05F1` to shared reader slot `LAB_1400807f0` with registered size 4. `InspectCodeAddress 1400807f0` shows a tiny local thunk that sets `R8D = 1` before jumping to `14006c090`; `TraceFunctionCallers` found registration/data refs only. The opcode comparison scan only produced useful hits for the three registration rows (`140075bd2`, `140075cf2`, `140075db6`) and then saturated on unrelated offsets. `MatchingManager_ApplyMatchingRoleCheckStarted` @ `1405c0e90` consumes `payload[0]` and dispatches `MatchingRoleCheckStarted`, but its only direct ref is `140e1e2c4` in PE `.pdata`, and the `WorldSocket+0x15b0` slot-11 scan found no `vtable+0x58` entry for it while finding the Fortune positive control. NexusForever currently emits `0x05CC.Ally` and `0x05F1.RolesRequired`, but native proof only establishes the shared one-flag wire shape plus a non-opcode-owned semantic candidate. | Recover the client apply/consumer path or live queue/role-check capture proving exact flag semantics and emit timing before renaming fields further, widening behavior, or using these packets as semantic evidence for `0x05CF`. |
| `ServerMatching0x05CF` | `Network_RegisterServerOpcode_0351` @ `14006c290` registers opcode `0x05CF` with size 4 and reader `ServerUInt32_LocalReadThunk` @ `140099110` (same thunk as `ServerTradeskillSigilResult` @ `0x085D`, different opcode slot). Matching-manager probes found correlated candidate `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0`; it writes the payload to manager `+0xa0` and refreshes UI state, but no opcode-to-consumer index or live packet witness ties it uniquely to `0x05CF`. The 2026-06-05 `FindDataReferences` refresh found `1405c41c0` referenced only by `140e1e66c`, and the follow-up `DumpNearbyData` pass proved `140e1e66c` / `140e1e228` are PE `.pdata` `_IMAGE_RUNTIME_FUNCTION_ENTRY` entries, not matching dispatch cells. The later `WorldSocket+0x15b0` slot-11 scan found the Fortune positive control but no `vtable+0x58` match for `1405c41c0`; the direct MCP bridge retry still fails with `Transport closed`, while fallback cache/direct-plugin evidence finds only broad manager globals `140c65b98` / `140c65898` in the helper and `140099110` remains registration-only/shared with `0x085D`; `selected_call_edges.csv` still shows no `ClientEvent_DispatchNamedEvent` edge. The 2026-06-06 solo queue / match-ready live probe confirmed neighboring `0x05EF`, `0x05B4`, `0x05CA`, and `0x05C8` paths but did not capture `0x05CF` or hit `1405c41c0`. No NexusForever emit. | Recover a real apply dispatcher/index outside PE `.pdata` that binds `0x05CF` to `1405c41c0`, or capture live `0x05CF` during match-ready / participant-count / queue UI flows and correlate with `0x05CA`/`0x05CC`. |
| `ServerMatchingGroupMemberRoleSelection` | Native registration binds `0x0600` size `0x18` to the shared identity-plus-`uint32` reader `ServerHousingCommunityPlotReservation_ReadPayload` @ `140086e70`. Pass 159 retried MCP (`Transport closed`) and used live-plugin/cache fallback: direct xrefs show `140086e70` reused by eight registration rows (`0x01B7`, `0x048B`, `0x0478`, `0x076C`, `0x0600`, `0x03C6`, `0x0533`, `0x051F`) plus two real row-helper calls from `ServerLootWinner` reader `1400a4e50`; exact selected send-helper scans found no `0x0600` producer; sampled non-registration `0x600` hits are allocator sizes, CRT/file-mode masks, or unrelated offsets; matching role-check UI/apply functions (`14076b770`, `1405c0e90`, `1405c0760`, `1405c3d30`) do not reference the `0x0600` reader or row. The managed trailing field is now neutral `TrailingValue` instead of `Role`. | Recover a real `0x0600` apply/consumer table, native producer, callback owner, or live payload tying the trailing uint32 to role-selection state before reintroducing role semantics or runtime emits. |
| `Client0x062A` / `Client0x0634` | `ClientWorldOpcodeRegister_MovementSpline` @ `1400a8190` registers both as 4-byte generic uint32 payloads via `ClientTradeskillResetTalents_WritePayload` @ `14007d010` / read @ `14007d000`. Pass 155 recheck: MCP still fails with `Transport closed`; exact selected rows remain `0x062A` at `1400a82b6` and `0x0634` at `1400a872c`, selected send-helper scans find no `0x062A`/`0x0634` sender, direct helper xrefs are shared registration/data plus `ClientCompoundTradeskillUInt32_WriteCluster`, and message-name slots `140c25488`/`140c25528` are untyped/unxrefed. Positive controls remain separate: `MatchingReplacement_SendStartLookingForReplacements` (`14076aa30`) sends `0x05D5`, and `TargetSelection_SendClientMovementControlAck` (`14057a630`) sends `0x0635`. NexusForever handlers are test-pinned as log-only with no queue-state emit. | Native sender via a non-registration UI/queue cluster trace, callback/table owner, indirect send rail, or live sniff tying value to matching/queue UI. |
| `ServerRaidQueueStatus` / `ServerRaidInfoResponse` row | `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` reads the shared 0x20-byte row. Adjacent `ServerRaidInfoResponse_ReadPayload` @ `14008c010` reads a 32-bit row count and array of those rows for opcode `0x071A`; `Group_DispatchRaidInfoResponse` @ `1406042b0` maps offsets to `SavedInstanceId`, `WorldId`, Windows FILETIME `DateExpireUTC`, float `DaysUntilExpire`, and `PrimeLevel`. Runtime still emits only zero-value `0x0718` compatibility state from raid-info. Pass 157 cache/direct-plugin probes found exact `0x0718` hits only in registration/reader surfaces, no selected static send-helper path, `14008c010` as the only real code caller into the row reader, and no owned opcode/apply table among extra data refs. | Standalone non-zero `0x0718` queue producer timing and queue-position/status aliases. |
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
