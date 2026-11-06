# Deferred client-side interaction ownership

6 November 2026. Implemented the existing deferred activation path's original
actor/target lifetime boundary, including Q3963 ship controls. The pending state
no longer redirects completion to a replacement Player or a reused entity GUID.
Newer interactions invalidate old cast-finish callbacks. Cancellation clears
pending readiness, including zero-cast-time waiting spells. Both supported
completion routes consume one activation.

This is Source/SQLite qualification. It does not establish native CSI wire
correlation, minigame success policy or retail completion. F-025, Q3963's broader
journey and the overall restoration Goal remain unfinished.

## Source evidence and observable failures

The prior PendingClientSideInteractionActivationStore retained only numeric
entity/creature/CSI/base-spell IDs under an IWorldSession key.
ClientSpellClientSideInteractionHandler and event101 in
ClientEntityInteractionHandler then resolved the entity by GUID using the
session's current Player. Delayed ClientActivateUnitCastHandler.FinishCallback
could replace the pending activation without retaining the initiating actor or
a ticket identifying that cast.

Eleven original baseline cases fail on publication parent2cf8c5f7: actor/session/
queue/map/entity replacement and delayed finish after Player replacement could
still invoke activation or credit. Two later review negatives also fail on the
first candidate: waiting-spell cancellation had no cleanup callback, and an
event101 reply after Player removal threw NullReferenceException.

The initial14-case baseline had3 passing controls. Its late-finish fixture was
subsequently strengthened: visible lookup now respects the requested GUID, the
replacement uses GUID88 rather than77, and original Player/Quest probes are
retained when creating another actor. That initial pass is not presented as
proof of late-finish safety. The corrected final fixture passes.

## Owning behavior

Begin captures the original Player/session, event queue, account and account
session epoch, Player map/admission generation, and concrete target object/map.
It creates one ticket before casting. A cast finish can arm or cancel only its
own current ticket. Ready state is consumed once; a lightweight token remains
to detect reentrant replacement while releasing the captured actor/map graph
from the session store after consumption or cancellation.

Completion requires the original actor and target to remain current. A matching
GUID on another object is insufficient. A same-map re-admission is a new
generation. Both callback routes resolve the retained target, and activation
stages recheck ownership after fallible spell-finish/script callbacks.
Achievement checks follow the guarded objective stage. A packet after Player
removal is ignored. Mismatched replies preserve the current ready ticket; early
replies cannot consume an unarmed timed cast.

The existing waiting spell invokes FinishCallback from Spell.Finish;
TryCompleteClientSideInteraction and its timeout reach that path. Cancellation
now retires readiness for both timed and zero-time waiting casts. A client cancel
still finishes the original waiting spell and calls original-target failure once.
A retired callback cannot cancel or arm a later ticket.

Ordinary non-deferred completion keeps its existing route. The accepted request
IDs, zero/base-spell/CSI/entity aliases, actions and normal timing rules are
unchanged. No new interpretation of SpellCastId or the success event was added.
A missing active waiting spell can still represent an already finished timed
cast under the existing policy; blanket rejection would change that contract.

## Recovered row cross-check

The owning GameTable reader checked7 cached asset files against their retained
manifest. The [portable record](coverage/csi-ownership-20261106.json) includes
their SHA256/extents and the checked rows:

- Creature27196 activates spell32386, base18613, CSI266 with duration60000ms.
- Q3963 has objective5201: type5, data0, count1, flags16.
- Its reward-pane TargetGroup7573 is type1 with sole nonzero entry27196;
  indicators45401/45402 remain unchanged.

This confirms the focused fixture's row contract; the runtime tests use those
checked values with prepared accepted quest state. It is not an actual full-table
cast or proof of the CSI minigame/packet sequence. No raw client table is committed.
Q3479 was inspected first in queue order after the previously completed pickup
milestones. Its survivor/CSI native-smoke gate remains; this change does not
generalize the Q3963-only deferred route to it or reorder generated coverage.

## Verification and review

Final result:32 new cases within178 selected related cases, all passed with
no skips. The two real-Player Q3963 completion routes achieve objective5201,
reject an owned full Character flush, preserve zero SQL progress on failure,
retry without replaying completion and reconstruct fresh Quest progress/state.
Two further real-Player cases invalidate pending credit on same-map admission.
The remaining tests exercise parsed public activation/completion packets,
original actor/account/entity lifetime, reentrant stage replacement, superseded
finishes, cancellation, duplicate/mismatched/early replies and related existing
activation, channel, Q3963 and Quest persistence behavior.

Accepted quests, map/entity collaborators and WorldSession are prepared; the
Player, QuestManager, Quest, admission lifecycle and full Player Character save
are real in the four integrated cases. The actual cinematic/departure script,
public quest acceptance, physical CSI minigame, provider/socket and native UI are
not exercised. The cancellation hook is invoked through captured production
parameters; this is not a complete native timeout/tick trace.

The incremental owning WorldServer build and selected test graph are retained
under artifacts/csi-ownership. The test graph has the existing ASPIRE010 warning.
No full solution/full suite, native client, Computer Use, server stack or database
service was launched. Existing services/configuration and the original
game_rework checkout were not modified.

Consolidated primary-agent spec and standards self-reviews found no remaining
actionable issue within this boundary. The cancellation and removed-Player
findings above were executed before correction and retained. Existing non-CSI
semantics, runtime-owned data use and exact staged scope were checked.
The portable record binds all changed Source files and owning/test DLLs.

## Remaining work

Obtain an exact native/accepted-traffic witness for SpellCastId and successive
same-base CSI reply correlation before changing that policy. Full exception
recovery through arbitrary scripts, range/busy/minigame rules, timer/native UI
parity and the Q3963 departure/cinematic/physical journey remain separate.
No claim of Q3963 or F-025 retail completion follows; generated
retail_claim_allowed=false gates are preserved. Continue the broader canonical
gameplay/assessment queue. Computer Use/native-client launch remain stopped.
