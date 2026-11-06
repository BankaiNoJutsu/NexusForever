# Cinematic completion ownership

Verified 6 November 2026 against published parent
`cd1dee96e1460e8fe322ade16052653db578d096`.

Parsed `ClientCinematicState` completion now retires exactly the original
playback before public-event continuation. Idle completion is harmless. Explicit
advance cannot replace an active movie; callbacks may queue or start valid
follow-up playback without the old completion clearing it. Callback exceptions
still release the original frame and advance the queue. A failed start releases
its frame, and setup that revokes context before publication releases the
unpublished frame and continues to valid queued work.

Each queued playback captures its original map, public-event manager, session,
event queue, account and World-session epoch. Stale queued work is skipped and
old completion cannot progress a replacement context. Existing intro suppression
and ordinary cinematic content remain unchanged.

## Concrete publication and event continuation

`CinematicBase` freezes its completed setup ID before the first notification.
Its packet writers and all fourteen provided derived emitters use a playback-bound
session adapter. Every individual enqueue checks the original frame and context,
including packets emitted inside actor, camera and keyframe writers. Reentrant
completion can start the next movie while suppressing the retired movie's tail.
The original public start method and interface-only extension path remain
available; the packet-stream guarantee applies to the provided Base-derived
cinematics.

`PublicEventCharacter` takes an event-membership snapshot and rechecks original
actor context, exact membership and event finalisation before every script
callback. Newly joined or replaced events do not adopt the old completion, and
one script's context change prevents a revoked second script from receiving it.
The public handler is inert after its session's Player has been removed.

## Executed evidence

| Candidate | Result |
| --- | --- |
| Original parsed-handler/real-playback baseline | Eight failures, two controls |
| First review negatives on unchanged reviewed runtime | Four failures: mutable first-notify ID, old packet tail, and two per-script ownership cases |
| Setup context-revocation negative | One failure: valid queued follow-up never started |
| Final candidate | All 25 new cinematic cases and 1,896 selected existing cases pass; zero failed/skipped |

Tests use the parsed public handler, concrete CinematicManager, CinematicBase,
PublicEventManager and PublicEventCharacter. Player/session/account and event
scripts are collaborators. Real `NorthernWildsOnCreate` verifies normal actor and
derived playback packets, then verifies suppression after completion reenters at
the first actor packet. The other controls cover FIFO continuation, idle states,
exceptions, session/queue/epoch changes and frozen started identity.

Both final Source review axes have zero remaining findings after material fixes.
The final owning WorldServer build is clean in 10.54s; the test-only build is
clean in 4.03s; the selected test run reports 11s. Additional builds and narrow
negative runs were required to reproduce and fix the reviewed boundaries. No
full solution build or full suite ran. The adapter's initial missing-namespace
compile diagnostic is retained separately from the runtime negatives.

The [portable record](coverage/cinematic-completion-20261106.json) binds all twenty
owned Source paths, final runtime/test DLLs, original and review negatives,
scoped results and reviews. The isolated candidate was compared against all
6,283 tracked Source paths: eighteen tracked task changes and eight retained
historical literal/filter differences, plus two new task files. Foreign root
Source was excluded. Raw evidence remains under
`artifacts/continuation-20261106/cinematic-completion/`. The resumption refresh
retains the same twenty upstream heads/bases/states, including #608 open and #609
closed/unmerged.

## Remaining boundary

This closes the supported Source completion/publication slice of F-022. It does
not qualify broader authored cinematics, quest/physical triggers or later mission
content. The client Ended payload contains no playback identity: a duplicate old
Ended received after the next movie is active cannot be correlated by this fix.
Client cancellation and that ordering policy require separate evidence.
Configured providers, deployed sockets and native/retail remain open; no fresh
provider/server qualification ran. Client launch and Computer Use remain stopped.
Continue the independent gameplay/progression queue and preserve concurrent work.
