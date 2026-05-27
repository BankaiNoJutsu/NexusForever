# LaughingWS Shared Public-Event Infrastructure Review

Date: 2026-05-27

## Decision

LWS-071 through LWS-076 are closed without widening shared public-event
behavior. Current code has useful WIP event scripts, packet models, and
completion-only cinematic placeholders, but the remaining shared framework work
requires packet/client evidence or content-specific smoke before implementation.

## LWS-071 Public-Event Votes

Mapped-only blocker. Public-event vote packets are modeled and some WIP content
can call `StartVote`, but exact retail/UI behavior remains unmapped for vote
open, choice, timeout, tally, and result. Do not implement broader vote state
without client capture proving the packet sequence and timeout/result handling.

Implemented packet-shape progress: the formerly raw public-event aux packets now
have decompile-backed diagnostic fields. `ServerPublicEventVoteAux_ReadPayload`
(`WildStar64.exe` `14007c0c0`, opcode `0x06F7`) reads a 15-bit value plus one
flag. `ServerPublicEventAux_ReadPayload` (`14007b930`, opcode `0x0139`) reads a
uint32 value, a 5-bit count, and counted uint32 values. These names intentionally
stay generic because producer semantics and vote lifecycle ordering are still
unmapped.

Required proof:

- `ServerPublicEventVoteInitiate` / detailed initiate timing and fields;
- `ClientPublicEventVote` choice semantics;
- tally/result/end packet order;
- timeout behavior and default choice handling;
- negative cases for late vote, duplicate vote, and non-participant vote.

Focused verification:

```powershell
dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
```

Result: network build passed, packet placeholder tests passed `35/35`, and the
decompile manifest validated `WildStar64.exe ok` on 2026-05-27 after adding the
public-event aux labels.

## LWS-072 Scoreboards And Rewards

Mapped-only blocker. Public-event start/end packets contain reward fields and
scoreboard request models exist, but exact scoreboard row shape, score fields,
reward tiers, failure/completion result packets, and reward delivery are not
mapped enough for cross-event behavior.

Implemented packet-shape progress: decompile labels now map
`ClientPublicEventRequestScoreboard_WritePayload` (`WildStar64.exe` `14007c620`,
opcode `0x06FA`) as public-event id u14 plus subscribe bit,
`ServerPublicEventStatsUpdate_ReadPayload` (`14007bde0`, opcode `0x0130`) as
public-event id plus counted team/participant rows,
`ServerPublicEventEnd_ReadPayload` (`14007bb80`, opcode `0x00D6`) as
personal/team/participant/objective rows plus reward tier/type/thresholds, and
the shared team/participant/personal-stat row readers. NexusForever comments and
packet tests now pin those shapes, but reward delivery and retail scoreboard
population semantics remain blocked.

Implement only event-specific rewards after a capture proves scoreboard rows,
reward tier/type semantics, and item/currency delivery.

## LWS-073 Objective Notification Parity

Mapped-only blocker. Current scripts can update phases/objectives, and
`ServerPublicEventObjectiveNotificationMode` is modeled, but exact objective
update text, phase notification text, completion notification, and quest-share
behavior remain unmapped.

Keep notification behavior conservative until a capture maps mode values, text
ids, target audience, and packet order.

## LWS-074 Trigger/Door Framework

Mapped-only blocker. WIP trigger scripts exist for specific instances, but the
generic framework cannot be widened from branch SQL alone. Trigger rows, radius,
coordinates, door ids, open/close state, cleanup timing, respawn/despawn timing,
and negative cases must be proven per content family before adding generic
door/trigger behavior.

## LWS-075 Cinematic/Communicator Framework

Mapped-only blocker. Current completion-only cinematic placeholders intentionally
avoid pretending to know actor, camera, text, and timing payloads. Replace them
only after client-reader/decompile evidence or smoke captures map the real
payload and send conditions.

Required proof includes actor rows, camera subject/position/timing, text or
communicator ids, start/finish conditions, skip behavior, and follow-up phase or
objective changes.

## LWS-076 Catalog Cleanup

Rejected for now. Unused branch-only `PublicEventCreature`,
`CommunicatorMessage`, `PublicEventObjective`, and `PublicEventPhase` catalogs
remain unported until an active runtime consumer needs them. Importing inactive
catalog rows without behavior would add noise and make later proof harder to
audit.
