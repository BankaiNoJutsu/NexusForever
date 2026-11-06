# Scientist scanner lifecycle

6 November 2026. The public Scientist request/dismiss flow now keeps scanner
admission, state publication and removal attached to its original object and
owner context. This completes a bounded Source lifecycle correction alongside
the earlier [name producer](SCIENTIST_SCANBOT_NAME_PUBLICATION.md).
The [portable record](coverage/scanner-lifecycle-20261106.json) binds the six
Source files, baseline/final results, binaries, review and upstream refresh.
Base: eb359ee7a0a8f6abc080464121839553ccbe608d.

## Behavior and reproduced faults

A foreign unsummon sharing the active GUID could clear the original scanner's
state. Dismissal after an owner map change looked up that GUID in the new map,
which could remove another entity. Queued additions after map/session/owner loss
could be adopted or left pending. Rejected dismiss state publication prevented
cleanup. Concrete removal also needed to notify its birth PathManager when the
map no longer resolved the owner or the ordinary unsummon callback threw.

Deployment now reserves its pending creation and captures the original
Character/GUID, Path manager, account/session, map and event queue. It checks
that context before creation and again after map admission. An existing account
must be bound to that same player session. Active and pending authority uses
object reference identity; a GUID alias cannot clear it.

Before a queued scanner runs base visibility callbacks, its birth manager checks
the captured context and original map. A stale addition records only the existing
Guid/Map/Position registration, queues its removal and returns. GridEntity's
valid admission still performs those same assignments before its existing
callbacks. A nearby concrete Player receives no scanner create or auxiliary
packets in the stale cases; the valid-add control still receives its create.

Dismissal removes the captured original object in a finally block. Concrete
removal notifies the birth manager in a finally block as well. Rejected state
packets are recreated for retry on the original queue. Context changes suppress
stale publication; a later state or new deployment supersedes an old notice.
The last context is retained only so a failed clear-state notice can finish.

## Validation

Local retained logs are under artifacts/continuation-20261106/scanner-lifecycle/.
The original seven-case baseline has six behavior failures and one control.
Review then exposed two material gaps: pre-rebound account admission and
visibility before stale-add rejection. The corrected review reproduction has
four failures and one valid-create control. Three failures strengthen existing
stale-add cases, so the baseline totals are not added together.

The final candidate passes all 11 new lifecycle cases and all 243 selected
Path, Grid range, Player visibility and map-admission cases, with no failures or
skips. The final World owning build is clean in 12.53 seconds; the test-only
build is clean in 14.08 seconds. The scoped runner reports 3 seconds.
No full solution or full suite was run.

Missing namespace imports, an initially unconfigured observer movement manager
and initial fixture warnings are retained as setup diagnostics, separate from
the genuine failures. Earlier passing 166/167-case candidates precede the final
review corrections. Seven of 138 DLLs differ from the initial baseline; both
owning Game and World DLLs exactly match the tested copies. This is not a
same-test-DLL claim. All six root/candidate Source identities match, with only
the eight previously retained project/filter and BOM/newline differences
outside the owned candidate scope.

## Standards

One initial account-session ownership finding was reproduced and resolved.
The final review finds zero remaining actionable issues: the account guard
binds to the captured player session, and the Grid registration extraction
preserves valid admission. All six final Source hashes match. The reviewer
performed inspection only.

## Spec

One initial stale visibility finding was reproduced and resolved. The final
review finds zero remaining issues: stale admission stops before base visibility
or entity script callbacks, the registration remains removable, and negative
Player publication plus valid-create controls exercise that boundary. All six
final Source hashes match. Final build/test results were pending at inspection;
the final executions above subsequently passed.

## Remaining qualification

The handlers, concrete scanner/base callbacks and concrete Player visibility are
real Source paths. Metadata, owner/account/session/map/movement, spatial registry
and MapFile are prepared fixtures. This is not an actual-table scanner corpus,
physical placement, provider, restart, socket, hosted deployment, native UI or
retail qualification. Current-reference guards do not prove a general concurrent
map-epoch protocol. The 20-PR refresh is unchanged, including #608 open and #609
closed/unmerged.

Profile unlock rules, cooldown timing, scan earning and Scientist experimentation
still need their own evidence. Path item notification/overflow work remains
separate under [LWS-061](LAUGHINGWS_PATH_FORTUNE_BLOCKER_REVIEW.md#lws-061-path-reward-precision).
No guessed reward policy or new unlock is introduced. Defaults15:1/17:1 remain;
no44/seven baseline grants. Native launch and Computer Use remain stopped.
Continue independent actionable gameplay/progression; this result does not
complete the overall Goal.
