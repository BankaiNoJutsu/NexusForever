# Matching Implementation Status

Status date: 2026-06-04

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
| Raid queue packet neutralization | Implemented + verified | `ServerRaidQueueStatus` keeps the compatibility zero emission from raid-info. Non-zero wire order is test-pinned, but speculative queue/game-type field names remain neutral `Unknown*` because the client evidence proves only row layout and a count-plus-array helper. | `GroupPacketShapeTests`; focused slice passed `20/20`. |
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

Latest raid-queue placeholder guard verification (2026-06-04):

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~ClientRaidInfoRequestHandlerTests|FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f010-raid-queue-placeholder\
```

Result: passed, 93/93. This slice pins the neutral `ServerRaidQueueStatus.Unknown0..4` names until non-zero queue semantics are proven.

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
| `ServerMatching0x05CF` | `Network_RegisterServerOpcode_0351` @ `14006c290` registers opcode `0x05CF` with size 4 and reader `ServerUInt32_LocalReadThunk` @ `140099110` (same thunk as `ServerTradeskillSigilResult` @ `0x085D`, different opcode slot). Matching-manager apply-table probes found correlated candidate `MatchingManager_ApplyManagerUInt32Field0xA0` @ `1405c41c0` / table cell `140e1e66c`; it writes the payload to manager `+0xa0` and refreshes UI state, but no opcode-to-table-cell index or live packet witness ties that cell uniquely to `0x05CF`. `FindImmediateInstructions` finds only the registration row; no NexusForever emit. | Recover the apply-table index/walker that binds `0x05CF` to cell `140e1e66c`, or capture live `0x05CF` during match-ready / participant-count / queue UI flows and correlate with `0x05CA`/`0x05CC`. |
| `Client0x062A` / `Client0x0634` | `ClientWorldOpcodeRegister_MovementSpline` @ `1400a8190` registers both as 4-byte generic uint32 payloads via `ClientTradeskillResetTalents_WritePayload` @ `14007d010` / read @ `14007d000`. `MOV R8D` and `MOV EDX` probes return registration-only hits (`1400a82b6` for `0x62A`). `TraceFunctionCallers` on `14007d010` and `14007dc80` show no gameplay send path. NexusForever handlers are now test-pinned as log-only with no queue-state emit. | Native sender via `14076aa30`-style UI cluster trace or live sniff tying value to matching/queue UI. |
| `ServerRaidQueueStatus` | `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` reads `uint64 + 15-bit uint32 + uint64 + uint32 + uint32`; adjacent helper `14008c010` reads a 32-bit row count and an array of 0x20-byte rows through that reader. Runtime emits only zero-value compatibility state from raid-info, and `GroupPacketShapeTests` pins non-zero wire order while fields stay neutral. | Queue consumer semantics and live non-zero payload examples. |
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
