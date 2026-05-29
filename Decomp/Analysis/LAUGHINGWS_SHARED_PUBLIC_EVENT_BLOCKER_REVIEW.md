# LaughingWS Shared Public-Event Infrastructure Review

Date: 2026-05-27

## Decision

LWS-071 through LWS-076 remain conservative shared-framework blockers. Current
code has useful WIP event scripts, packet models, vote/scoreboard hardening, and
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

Runtime hardening progress (2026-05-28): `ClientPublicEventVote` remains mapped
as opcode `0x06EE` with event id u14, vote id u14, team id u14, and choice u32,
and the server-side `PublicEventVote.Choice` path is now idempotent for finalised
votes, non-participants, duplicate responses, and invalid choices. Focused tests
cover detailed vote initiate, tally/end when all members vote, timeout default
choice, duplicate/late/invalid response handling, client vote read, and server
initiate/detailed-initiate/tally/end serialization. The `ClientPublicEventVote`
handler now forwards the full mapped event/vote/team/choice envelope to the
public-event manager. The runtime validates team id against the player's current
public-event team and validates vote id against the active vote before accepting
the choice, so stale or cross-team replies are ignored before vote state changes.
This is intentionally not full closure because retail UI sequence, exact timeout
cadence, result ordering, mismatch feedback, and default-choice semantics still
need capture proof.

Evidence harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
now has a `-PublicEventVoteScoreboardSmoke` preset that creates a worksheet for
vote initiate/detailed-initiate, client choice, tally/end ordering,
timeout/default-choice behavior, vote/team id handling, and negative
non-participant/duplicate/late vote cases.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now dry-runs this
preset with `-CreateBundleOnly` and verifies its manifest, worksheet, helper
files, and negative-case scaffold.

Required proof:

- `ServerPublicEventVoteInitiate` / detailed initiate timing and fields;
- `ClientPublicEventVote` choice semantics;
- tally/result/end packet order;
- timeout behavior and default choice handling;
- negative cases for late vote, duplicate vote, and non-participant vote.

Focused verification:

```powershell
dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj --no-restore -v minimal --nologo
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PublicEventVoteTests|FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
```

Result: network build passed, packet placeholder tests passed `35/35`, and the
decompile manifest validated `WildStar64.exe ok` on 2026-05-27 after adding the
public-event aux labels.

Follow-up result (2026-05-28): network and game project builds passed, and the
combined focused vote/packet/handler test filter passed `49/49`. The manifest verifier
was not used as closure evidence for this runtime-only hardening pass because
the broader local decompile manifest state is dirty from unrelated export
refresh attempts.

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
packet tests now pin those shapes.

Implemented runtime progress (2026-05-28): `ClientPublicEventRequestScoreboard`
subscribe requests route through the map public-event manager and emit one
participant-scoped `ServerPublicEventStatsUpdate` snapshot for members of the
requested event. Public-event team stat rows now aggregate each member's latest
absolute stat value, so team totals no longer collapse to the last member update.
Focused tests cover subscribe routing, unsubscribe no-op behavior before map or
event lookup, non-member suppression, and combined team/participant stat rows.

Reward delivery, exact live subscription cadence, score-field precision,
result ordering, and reward tier/type semantics remain blocked.

Evidence harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
`-PublicEventVoteScoreboardSmoke` also captures scoreboard subscribe/unsubscribe
cadence, stat row payloads, event end/reward-threshold rows, non-member request
suppression, and reward delivery proof.
The same dry-run guard verifies the shared vote/scoreboard worksheet and
negative-case scaffold; it does not prove subscription cadence or rewards.

Implement only event-specific rewards after a capture proves scoreboard rows,
reward tier/type semantics, and item/currency delivery.

## LWS-073 Objective Notification Parity

Mapped-only blocker. Current scripts can update phases/objectives, and
`ServerPublicEventObjectiveNotificationMode` is modeled. Local decompile now
maps the table-backed objective text selectors: same-team viewers use
`LocalizedTextId` / `LocalizedTextIdShort`, other-team viewers use
`LocalizedTextIdOtherTeam` / `LocalizedTextIdOtherTeamShort`, and the Lua layer
also exposes join/start messages from
`LocalizedTextIdParticipantAdd` / `LocalizedTextIdStart`. Exact phase/completion
notification text emission, packet ordering, and quest-share behavior remain
unmapped.

Packet-shape progress (2026-05-27): the client registration table maps opcode
`0x0133` to `ServerPublicEventObjectiveNotificationMode_ReadPayload`
(`WildStar64.exe` `14007a3a0`), `0x0134` to
`ServerPublicEventObjectiveStatusUpdate_ReadPayload` (`14007b3c0`), `0x0132`
to `ServerPublicEventObjectiveUpdate_ReadPayload` (`14007b490`), and `0x06F9`
to the shared 15-bit scalar reader. NexusForever now writes objective
notification mode with a 15-bit objective id and has packet tests for
notification mode, objective status update, full objective update, and
objective start shapes.

Lua text semantics progress (2026-05-29): `Lua_PublicEventObjective_GetDescription`
(`WildStar64.exe` `14068d5b0`) and `GetShortDescription` (`14068d8a0`) switch
between the owning-team and other-team text ids based on the viewer's current
live-event team. `GetJoinMessage` (`140690f20`) and `GetStartMessage`
(`140691150`) read the live-objective entry fields that match
`PublicEventObjectiveEntry.LocalizedTextIdParticipantAdd` and
`LocalizedTextIdStart`. The description getters also replace the base
description with a 16-entry state-indexed text lookup when the internal
objective type is `0x18`; that shape matches the local
`PublicEventObjectiveStateEntry.LocalizedTextIdState00..15` table and strongly
points at `PublicEventObjectiveType.State` on the internal/game-table side.
`Lua_PublicEventObjective_GetObjectiveType` (`1406900a0`) returns that raw
objective type directly into Lua with no translation. The conflicting later
objective-type values visible in `Lua_RegisterPublicEventConstants`
(`14068c2c0.fragment.c`) are not authoritative numeric evidence: the same
fragment also scrambles already-proven status, notification-mode, and category
values. This narrows the remaining blocker surface to mode-driven
phase/completion text, packet audience/order, and quest-share side effects; it
does not prove when those messages are emitted.

Runtime hardening progress (2026-05-28): the existing objective update producer
now suppresses duplicate `ServerPublicEventObjectiveUpdate` payloads when
`SetBusy` is called with the already-active busy state. The source comment ties
the emission boundary to `WildStar64.exe` `14007b490` / opcode `0x0132`, and
`PublicEventObjectiveTests.SetBusy_WithUnchangedState_DoesNotBroadcastDuplicateObjectiveUpdate`
pins the no-duplicate behavior. This does not add notification-mode, text,
audience, ordering, or quest-share semantics.

Evidence harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
`-PublicEventObjectiveNotificationSmoke` now creates an LWS-073 worksheet for
objective start/update/status/notification packets, objective/phase text ids,
quest-share side effects, target audience, ordering, duplicate suppression, and
negative cases. `test_blocker_evidence_harness_presets.py` dry-runs the preset
and verifies its manifest, worksheet, helper files, and negative-case scaffold;
this is capture-workflow coverage only, not notification parity proof.

## LWS-074 Trigger/Door Framework

Mapped-only blocker. WIP trigger scripts exist for specific instances, but the
generic framework cannot be widened from branch SQL alone. Trigger rows, radius,
coordinates, door ids, open/close state, cleanup timing, respawn/despawn timing,
and negative cases must be proven per content family before adding generic
door/trigger behavior.

Runtime producer hardening progress (2026-05-28): the existing
`VolumeGridTriggerEntity` and `TurnstileTriggerEntity` producers are still
limited to the client-exposed public-event objective constants
`PublicEventObjectiveType_ParticipantsInTriggerVolume` and
`PublicEventObjectiveType_Turnstile` from `Lua_RegisterPublicEventConstants`.
They now ignore zero object ids and non-player entities, while volume triggers
keep paired enter/leave deltas. Focused tests cover enter, leave, zero-object,
non-player, world-location horizontal radius plus vertical clamp behavior, and
turnstile entry cases. Additional regressions pin that repeated in-range checks
do not duplicate volume or turnstile objective credit, and that turnstiles do
not emit a leave/decrement delta. This does not prove branch trigger rows,
coordinates, door ids/states, cleanup timing, or respawn/despawn behavior.

## LWS-075 Cinematic/Communicator Framework

Mapped-only blocker. Current completion-only cinematic placeholders intentionally
avoid pretending to know actor, camera, text, and timing payloads. Replace them
only after client-reader/decompile evidence or smoke captures map the real
payload and send conditions.

Required proof includes actor rows, camera subject/position/timing, text or
communicator ids, start/finish conditions, skip behavior, and follow-up phase or
objective changes.

Packet-shape progress (2026-05-28): current NexusForever communicator/story
packet models are now pinned at the safe table/export boundary. `ServerCommunicatorMessage`
records the `DB\CommunicatorMessages.tbl` and `Communicator_ShowQuestMsg`
anchors; `ServerStoryTextCommunicator` records the CommunicatorLib
placement/overlay/background symbols; and `ServerStoryPanelCustomShow` records
`DB\StoryPanel.tbl` plus `MessageManager_DisplayStoryPanel`. Focused packet
tests pin communicator id/condition flag, story communicator creature/duration/
placement/overlay/background fields, and story-panel sound/type/duration/style
fields. Additional packet coverage now pins the shared `StoryMessage` actor-row
wire shapes for creature, custom text, localized text, player, creature-unit,
and self-player token sources. This remains packet-shape only and does not prove
actor/camera/text timing, send conditions, skip behavior, or content-specific
cinematic sequence replacement.

## LWS-076 Catalog Cleanup

Rejected for now. Unused branch-only `PublicEventCreature`,
`CommunicatorMessage`, `PublicEventObjective`, and `PublicEventPhase` catalogs
remain unported until an active runtime consumer needs them. Importing inactive
catalog rows without behavior would add noise and make later proof harder to
audit.

Static guard progress (2026-05-28): `BranchCatalogCleanupTests` now asserts
that the rejected all-in-one-only Datascape `HydrofluxLogicAndWaterEntityScript`
and `MnemesisLogicAndWaterEntityScript` names are absent from runtime instance
source, and that the known door/platform/marker-only or empty branch catalog
files for War of the Wilds, Protogames Academy, Ruins of Kel Voreth, Skullcano,
Shade's Eve, Gauntlet, Fragment Zero, Infestation, Outpost M-13, Space Madness,
Datascape, Genetic Archives, Initialization Core Y-83, and Red Moon Terror
remain absent. This is a guard for the rejection decision only; it does not
promote any shared public-event catalog behavior.
