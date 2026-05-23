# Matching Implementation Status

Status date: 2026-05-23

This tracker closes matching work only when a surface is one of:

- **Implemented + verified**: runtime behavior exists and has focused tests or a client smoke result.
- **Mapped-only / blocked**: client or retail evidence is mapped far enough to explain the boundary, but runtime behavior is intentionally not guessed.
- **Rejected**: evidence shows the suspected behavior is not part of the matching surface.

Generated exports, logs, and client binaries remain local artifacts. Add durable labels only when a client function/consumer is mapped enough for the next pass to rediscover it.

## Implemented + Verified

| Surface | Status | Runtime notes | Verification |
| --- | --- | --- | --- |
| Queue leave/requeue hardening | Implemented + verified | Missing queue state no longer throws on normal leave/remove paths; all-queue leave snapshots the queue list before mutation. | Focused matching tests passed `31/31`. |
| Role-check UI status | Implemented + verified | `ServerMatchingQueueStatus.ReadyMatchType` now comes from `IMatchingManager.GetReadyMatchType` and updates when role checks are created/removed. | `MatchingCharacterStatusTests`. |
| Match-ready response counts | Implemented + verified | Match-ready packets use pending/accepted proposal response counters instead of raw member counts. | `MatchProposalTeamTests`; existing proposal tests in focused slice. |
| Deserter UI packet | Implemented + verified | Deserter sync now emits `ServerMatchingPenaltyUpdated` with per-match-type PvE/PvP remaining times instead of unrelated kick cooldown packets. | `MatchingDeserterManagerTests`. |
| Deserter restart persistence | Implemented + verified | Active deserter state is persisted in `character_matching_penalty`, restored on login/lazy queue checks, and removed when cleared or expired. | `MatchingDeserterManagerTests`. |
| Queue-left UI sync | Implemented + verified | Authoritative queue removal now emits `ServerMatchingLeftQueue` before the refreshed `ServerMatchingQueueStatus`. | `MatchingCharacterStatusTests`. |
| Flexible role selection | Implemented + verified | The old `1 tank / 1 healer / 3 DPS` reducer was removed. Dungeon/adventure role validation now preserves each player's selected role mask and rejects `Role.None` or undefined role bits; full groups are not rejected solely for non-trinity composition. | `MatchingRoleEnforcerTests`. |
| Raid queue packet neutralization | Implemented + verified | `ServerRaidQueueStatus` keeps the compatibility zero emission from raid-info, but speculative queue/game-type field names were reverted to neutral `Unknown*` fields after the client reader only proved wire widths. | `GroupPacketShapeTests`; build compile. |
| Replacement role-mask guard | Implemented + verified | `ClientMatchingMatchInitiateLookingForReplacements` now accepts only native-emitted role bits `0..2` (`0x07`, locally `Tank/Healer/DPS`) before any future replacement backfill logic can consume the mask. | `ClientMatchingMatchInitiateLookingForReplacementsHandlerTests`. |
| Average wait UI (`0x0628`) | Implemented + verified | `ServerMatchingAverageWaitTimeUpdate` emitted on queue join, after non-solo pop samples queue time, and on login for active queues; EasyMatchMaker listens via `MatchingAverageWaitTimeUpdated`. | `MatchingAverageWaitTimeTests`; addon corpus `ADDON_CORPUS_EVIDENCE.md`. |
| Replacement stale-match guard | Implemented + verified | In-progress replacement proposals now either merge into the registered active match or close the replacement queue with `UnableToQueue`; stale registry/group state can no longer fall through and create a fresh match. | `MatchManagerReplacementTests`. |
| Warplot surrender winner | Implemented + build-covered | Surrender completion records the surrendering team and awards victory to the opposite team. | Covered by focused matching build; direct match-state test still needs a heavier PvP match fixture. |
| Vote rejection packets | Implemented + build-covered | Failed vote-kick/surrender initiate/cast paths now return the matching vote failure packet unless the personal cooldown packet is more specific. | Covered by focused matching build; packet-sequence fixture is still a useful follow-up. |

Focused verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~Matching|FullyQualifiedName~ClientRaidInfoRequestHandlerTests"
```

Result: passed `50/50`.

Latest packet/evidence refresh verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~Matching|FullyQualifiedName~ClientRaidInfoRequestHandlerTests|FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~ClientDiagnosticPacketShapeTests"
```

Result: passed `84/84`.

Latest full verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
dotnet build Source\NexusForever.sln -v minimal --nologo
```

Results: test project passed `928/928`; solution build succeeded with `0` warnings and `0` errors.

## Mapped-only / Blocked

| Surface | Current evidence | Blocker before implementation |
| --- | --- | --- |
| `Client0x062A` / `Client0x0634` | `ClientWorldOpcodeRegister_MovementSpline` @ `1400a8190` registers both as 4-byte generic uint32 payloads using the same scalar write/read helpers as other diagnostic opcodes. | Native sender semantics or live sniff tying the value to a matching action. |
| `ServerRaidQueueStatus` | `ServerRaidQueueStatus_ReadPayload` @ `14008bf80` reads `uint64 + 15-bit uint32 + uint64 + uint32 + uint32`; runtime emits only zero-value compatibility state from raid-info and keeps neutral fields. | Queue consumer semantics and live non-zero payload examples. |
| Replacement queue fill | **Partial (2026-05-23):** `0x05D5` opens an in-progress anchor queue (`SetInProgress`, `ServerMatchingMatchInProgressReady` path); `0x0602` closes it; successful replacement proposals call `Match.AddReplacementMembers` instead of spawning a new match. Replacement candidates are filtered against the requested role mask before popping, and stale replacement groups now close instead of creating a new match. Addon corpus: EMM `MatchLookingForReplacements`; public AddOn Studio API events also list `MatchLookingForReplacements` / `MatchStoppedLookingForReplacements`. | Multi-replacement sequencing and live client smoke for accept -> teleport into running instance. |
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

1. Map client consumers/senders for opcodes `0x05B0..0x0628`, `Client0x062A`, `Client0x0634`, and `ServerRaidQueueStatus`.
2. Capture or decompile the replacement-flow sequence: request replacement, queue status, ready accept, add to existing match, and cleanup.
3. Add a focused PvP match fixture so warplot surrender and vote packet sequences can be verified without a live client.
4. Run build-16042 client smoke once local dependencies/client are available: solo queue, party queue, role check, ready accept/decline, enter/leave/requeue, replacement fill, vote kick, warplot surrender, deserter denial/allowance, and relog status restoration.
