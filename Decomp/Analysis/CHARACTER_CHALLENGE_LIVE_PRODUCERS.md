# Live Character Challenge earning producers

## Shared Challenge preference admission

5 November 2026. Build 16042's `SetSharedChallengePreference` binding at
`0x1407055b0` sends world opcode `0x012B` with option type 1 and the selected
integer. The enum registration at `0x14070c3e6` passes three 16-byte records
from `0x140c38260`. The helper at `0x1400f0520` reads the name pointer and
integer from each record. The records use UTF-16 enum names.

| Value | Name | Existing invitation gate |
| --- | --- | --- |
| 0 | AutoAccept | Allow invitations |
| 1 | Prompt | Allow invitations |
| 2 | AutoReject | Block invitations |

The archived Interface Options addon uses AutoAccept as its default and sends
Prompt when its checkbox is selected. The Challenge Log sends AutoReject
through the same setter when the recipient selects Always Reject.
[Options addon](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/OptionsInterface/OptionsInterface.lua#L768)
· [Challenge Log](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/Challenges/ChallengeLog.lua#L1479)

The server option handler projects the three known preferences into the
existing stored invitation gate. Separate automatic activation remains open.
The packet regression reads both 32-bit fields from the raw body and exercises
the option handler followed by the existing invitation receiver. The baseline
blocked AutoAccept and rejected AutoReject as malformed.
The registered ChallengesLib table at `0x140b73890` has 13 methods before
its null terminator. It includes both replies. The other library-name
reference at `0x14050b6a1` registers the same callback at `0x140711f80`.
A bounded scan for literal `mov edx, 0xc5`
within 64 bytes of a direct dispatch at `0x1403f4900` confirmed four
sites with choices 1, 2, 14 and 15. Other encodings and other opcodes remain
outside that scan. A correlated share-init sender or packet is still needed.

REA PE resource evidence
`ev_f71b42226c3996b3e918194b6cb8eaa0e487687f1986da9cd7b0651c0f36a0b1`
binds the client to SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
LLVM disassembly and pinned addon source establish the option values and
producer. Local evidence is under
`artifacts/continuation-20261105/challenge-share-native/`.
The first scoped run exposed a readiness fixture that omitted the World
session binding used by character selection. The fixture now covers a failed
load on the original Player and after replacement. The final owning build
took 25 seconds with zero warnings or errors. All 77 scoped tests passed in
34 seconds.

## Target progress belongs to the original attempt

5 November 2026. Combat kills and object activations capture the active
attempts at the start of target progress. A Challenge completed by the event
can run a Quest objective callback that starts or restarts another attempt.
The new attempt does not inherit the event. An unchanged matching attempt
still earns its credit.

The baseline threw on both the combat and activation paths when the
completion callback added a Challenge. It also gave progress to a restarted
Checklist attempt when no row was added. The test enters both existing
gameplay hooks and drives the callback through the Challenge completion's
Quest objective update. The owning build took 44 seconds with zero warnings
or errors. All 214 scoped tests passed in 61 seconds. Local results are under
`artifacts/continuation-20261105/challenge-target-ownership/`.

## Terminal Challenge area timers

5 November 2026. Completing an attempt or reaching its active timeout
clears area grace before the terminal transition is captured. An inactive
attempt cannot emit a later LeftArea result. Older inactive rows that
retain a grace value are shown as inactive without an area countdown.

The baseline emitted LeftArea after successful gameplay completion and
after TimerExpired. A reloaded inactive row also generated an extra
status update on the old grace timeout. The owning build passed in
27 seconds with zero warnings and errors. All 66 scoped Challenge and
Contract companion tests passed in 32 seconds. Evidence is under
`artifacts/continuation-20261105/challenge-terminal-timers/`.
The area-grace regressions start from a prepared timer. Retail area entry,
scoring rules and native presentation remain open.

## Legacy Challenge result ownership

5 November 2026. Legacy Challenge attempts apply tier progression and terminal
gameplay effects before sending their immediate result notices. A failed
Challenge status, medal or completion enqueue can no longer interrupt the item
grant or quest objective update. A reentrant activation replaces the runtime
generation and drops any unsent result for the older attempt. The generation
is local to the Player and leaves historical attempt IDs and save rows intact.

Six baseline cases reproduced interrupted progress or delivery, repeat
completion, duplicate activation and a rejected restart during a terminal
medal notice. The owning build passed with zero warnings and errors in
36 seconds. All 144 scoped Challenge and Contract companion cases passed in
86 seconds. Evidence is under
`artifacts/continuation-20261105/legacy-challenge-result-ownership/`.
Legacy items still use normal inventory persistence. Unsupported earning rules,
deployed sockets and native presentation remain open.

## Confirmed medal and completion results

4 November 2026. Four baseline cases reproduced a live Challenge result
or status enqueue running before the durable medal and Quest transition
was admitted. A failing enqueue could interrupt the producer.

Durable count-based attempts now publish the existing TierAchieved and
Completed results after the owning Character transaction confirms and
the actor adopts its state. The same rule applies when the Challenge
transitions join a Contract transaction. Live status and activation
packets still follow gameplay progress. A failed live status send can no
longer interrupt a durable transition. Progress and activation recheck the
original active attempt and tier after an immediate send, so a reentrant
advance cannot complete it twice. Historical and unsupported attempts keep
their immediate result path.

Committed results retain their original tier, recipient and attempt.
Send retries leave the next result queued, so Completed cannot overtake
its medal. A new attempt or a rebound session invalidates old notices.
A composed reward-pane reload gets one retry with fresh copies of the
current rows on the same actor. A superseded or persistent reload failure
still follows the original failure and quarantine path.
The session's bound Player is checked both before enqueue and before
acknowledging the published snapshot for claims.

Build 16042 PE reads bind 00C6 at `0x1403ef25b` to `0x14048e2b0`.
Cases 8 and 9 use payload+8 plus one and call `0x14048dde0`.
That helper writes the tier for a key matching the Challenge ID.
00C7 uses a separate consumer at `0x14048ded0`. The keyed result
write is the reason to avoid applying an old result to a restarted
attempt. Evidence is in
`artifacts/continuation-20261104/challenge-result-publication/`.
Scoped coverage spans 270 cases across two runs on the same Game DLL.
The first run passed 254 and exposed 16 readiness fixtures without a bound
Player. The corrected readiness and retry run passed all 34 cases, including
those 16. The runs took 87 and 18 seconds of test execution. The earlier
fixture compile failure and disk-full interruption are retained separately.
Deployed socket order, restart replay and native presentation remain open.

## Original earning integration

Current socket earning/claims and production retry follow-through are recorded in
[checkpoint7983](CHARACTER_CHALLENGE_SOCKET.md). The7959 results below retain their
original scope; its open socket gate is superseded by that bounded actual-source
run. Native UI and the other remaining semantics stay open.

Checkpoint 7959 connects mapped count-based Challenge activation, medal advancement and termination to the durable Character reward queue. All 16 validated type3 catalogs are supported through the existing Challenge producers. Quest completion still runs immediately on the actor, with its exact concrete owners and script effects captured for the same terminal SQL unit. CharacterTrack completion does not also issue a legacy direct prize. Historical NULL markers and unsupported/time-tiered source shapes retain the existing legacy path. Real socket earning/claim/relog/reset and native UI proof remain open; Computer Use stays stopped.

## Activation and progression

Real world admission already waits for the initial durable load at checkpoint7903. Activation of a supported source with contiguous available positive-count tiers captures a new stable attempt id, prerequisites and its LegacyDirect/CharacterTrack decision. The decision does not change when modifiers or prerequisites later change. Missing catalogs, empty29/30/31/32/34, unknown time-tiered flag0x8, holes/unavailable tiers and custom managers without production reward initialization retain legacy behavior. No historical completion count is converted into medal credit.

The existing count-goal transition is the earning event. Each tracked medal captures the whole property4/data3 modifier and its revision before queueing, then credits one100-point base increment through the existing float32 multiplication/truncation policy. Initial LastRewardTier0 does not create a bronze receipt. The combat auto-activation path also handles a goal reached by its initial progress. One-, two- and three-tier sources terminate at their actual final tier.

Actor progress remains immediate while older commands execute. Each command keeps its immutable after state, and its FIFO head resolves the current durable before state. Older adoption preserves later progress/timers and dirty revisions. Rapid completed/restarted attempts keep a queued producer lineage bound to the exact RewardIntent objects; that lineage permits retaining a later accepted attempt identity. Arbitrary external identity replacements still fail the original merge guard. Normal Player saves remain excluded across queued units, while the frozen normal-save path preserves events occurring between an older Auth and Character phase.

Abandonment, active timeout and area failure enqueue terminal state without inventing a completion, Quest effect or unearned medal. Earned points remain. A tracked attempt whose modifier owner becomes unavailable is quarantined and disconnected; it never switches to a direct prize.

## Legacy compatibility

New supported LegacyDirect attempts use a LegacyAdvance command to persist reached tiers without creating track points or medal receipts. Their direct item and Quest completion still execute once on the actor in their existing order. The terminal attempt and Challenge row then agree on the next authoritative load. Direct items retain their existing normal inventory persistence; this change does not claim that legacy item delivery is part of the track SQL receipt transaction.

Historical active NULL attempts remain NULL through completion or abandonment. An immediate subsequent activation may carry its exact unsaved inactive legacy predecessor into the new attempt. This internal activation path checks character/Challenge ownership, NULL markers, inactive state and equal or one-step completion history against the real SQL baseline. It cannot steal a durable attempt or skip history. It creates no attempt or medal for that historical predecessor. The new attempt earns only its own medals. Standard repository activation retains its original strict contract.

Inventory.ItemCreate can use unreserved capacity while reward work blocks normal saves. Mixed historical legacy completion with pending tracked medals or a real reserved claim preserves its direct prize, both Quest events and the reserved delivery. A failed Quest preparation leaves terminal completion and its provisional legacy prize uncommitted and prevents stale normal saves.

## Verification

Evidence: `I:/GIT/NexusForever-assessment-report/remaining-goal/challenge-live-producers/final-review`; runtime: `remaining-goal/runtime-checkpoint7959`.

- Full solution build: **0 warnings, 0 errors**. Full suite: **7,959/7,959**, zero skipped. Full TRX SHA256:`55EDFCDD26577426A9E135D206E12240512D6FF3E12F9B6699751C0437A8BC51`. **293 focused cases** pass.
- **56 new producer cases** exercise actual Activate choices, public progress and combat auto-activation methods with the real Player/Quest/Challenge actor managers. They cover all16 catalogs, separate per-medal modifiers/revisions, no direct-plus-track double grant, stable LegacyDirect mode, historical NULL completion/restart/abandon, invalid legacy predecessors, empty/unsupported source shapes, one/two/three tiers, zero/one/two earned medals at all three non-completing terminations, rapid repeated attempts, normal Auth-gap overlap, later dirty progress/timers, unavailable modifiers, read-only preparation failure, nonterminal/terminal lost write acknowledgements, mixed legacy/tracked work and pending claims, and fresh-actor relog. Registered07CF/07CE handlers consume real actor-earned state and reset one completed claim cycle without reearning. Unrelated Player managers are mocked; reward catalogs are actual build16042 data, while Challenge definitions/creature targets and Character42 are synthetic. The relog fixture reconstructs Player/Quest/Challenge owners; it does not launch a native client or claim a socket login workload.
- **512 exact-source provider cases** pass without exclusions in fresh retained `nexus_assessment_challenge_live_30a1`:56 new plus456 prior reward/Quest/readiness cases. Each has hashed SQL state before guarded fixture cleanup. Counts include provider-independent dispatch and SQLite guards, not512 concurrent MySQL workloads. Two frozen provider model gates bring combined replay/model checks to **514**, with zero relational differences and no pending model changes. No models, migrations or schema changes were introduced.
- A read-only audit of the configured actual source tables hashes Challenge.tbl/ChallengeTier.tbl and preserves configuration. Of643 Challenges,439 reference the supported catalogs;363 pass the contiguous positive-tier/non-time-tiered source gate, covering all16 tracks. This is table eligibility, not final actor prerequisite/modifier/world-placement eligibility. Actual Challenge103 (track16,combat,flags32,target8851,tier150/count5) is a concrete next socket-test candidate on the reviewed Northern Wilds bridge. See `challenge-live-producers/SourceAudit/result.json`.
- Verification binds **169 DLLs, 5,772 Source files, 21 copied test/helper files and 9 changed Source paths since7903**. All31 unchanged compatibility probes pass. Original checkout, branch/head, entitlement/loyalty sources, default15:1/17:1/no44 and private configurations remain preserved. Pre-existing Humanizer/RabbitMQ.Client test-versus-World dependency differences remain separately bound. No new commit/push.
- Frozen World PID2136 and Character API PID296, with unchanged Auth PID9380, pass **215 complete protocol login/world/cleanup cycles** and215 initial composed reward reloads. All9 services remain healthy; the other7 retain their identities. Six queues drain naturally, generations settle offline without repair, and originals/configurations/schema inventory remain unchanged. Lifecycle errors, unfinished admissions and late/duplicate bootstraps are zero. This socket workload does not earn or claim; new reward tables remain empty and old attempt markers remain NULL. The registered-handler earning tests above are a separate scope.

Retained early evidence includes the initial test accessor compile error and the first47-case run with two terminal acknowledgement cases failing. Those injections hit the read-only Quest preparation commit before the reward write; they were corrected to target contexts with staged write entities. Separate preparation-failure regressions retain that failure path and verify quarantine/no terminal SQL effects. The52- and53-case intermediate suites passed before adding the final source-tier variants; their artifacts remain historical.

## Remaining gates

Extend the isolated socket harness through genuine earning, choose, claim, relog and reset using an actual eligible Challenge source and its mapped runtime producer. The actual-source audit identifies Challenge103/track16/one tier of5 combat kills as a concrete candidate; verify its world placement and runtime target/kill path before using it. Validate native presentation only after explicit resumption of Computer Use. Unknown time-tiered scoring, producer-specific collect/item/general semantics, share-init ownership, challenge-specific achievement triggers and exact retail RNG/reset parity remain outside the verified count-based slice. The existing uniform/all-claims/carry policy remains the explicitly accepted upstream policy, not a new retail-RNG claim.

F-033 stays partial. The full assessment Goal, other matrix/content rows and all31 CSVs/167,484 not_retail_complete rows remain active. The June19 Q5597 historical UI bundle is unavailable; its recovery review does not replace native UI evidence.
