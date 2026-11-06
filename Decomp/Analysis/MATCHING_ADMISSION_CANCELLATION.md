# Matching admission cancellation and role confirmation

6 November2026. Public solo/random queue requests now retain cancellation and
their original Player/session/queue/account context through deferred admission.
Leave, leave-all and logout invalidate pending work before eligibility and
again before refusal or admission. A cancelled request cannot enter on a later
matching tick or publish its old refusal to a replacement Player. A later
request has its own admission, and leaving another match type preserves the
original request.

## Party roles and original notices

Party role checks retain their original participants. Cancellation works after
one member has selected roles and after all roles have succeeded but before
admission. It does not reuse the single-response operation to decline an
already-responded member. Terminal role checks cannot be expired by the next
long tick. Cleanup removes only the old check's mappings and removes all owned
mappings before status publication, so callbacks cannot erase a new check.
Duplicate party requests preserve the live check and its existing responses.

The public05B2 handler applies responses through the matching manager's owned
operation. The Player and MatchType must belong to the pending check; an
explicit decline cannot become consent, and duplicate positive responses are
inert. Existing parsed role masks and queue validation remain in place. The
[earlier native/authored corroboration](MATCHING_ROLE_WEB_CORROBORATION_2026_11_05.md)
supplies the known05B2 confirmation/decline and type boundary; this milestone
does not assign a new role, queue composition or diagnostic protocol policy.

The manager registers the role-check identity before initial publication.
Each started/cancelled notice checks that the same role check still owns those
mappings. A callback that cancels or replaces it stops the old fanout; an old
cancel cannot follow the replacement check's started notice. Participant guards
also suppress notices to replaced sessions. Eligibility is checked before
both failure publication and successful admission, including each refusal
recipient. Offline-member validation returns the existing refusal instead of
dereferencing a missing Player.

Already-admitted queue state keeps its existing logout/login readback. This
change concerns admission and pending role checks; it does not choose a new
reconnect policy for admitted queues.

## Reproduced behavior and verification

The published-base parsed-handler probe has eight genuine failures and two
passing uncancelled controls. Specific and random joins cancelled by leave or
leave-all still enter on the next tick. Logout can admit the request or reach
the validator's null dereference. Party leave ignores the pending role check;
logout after successful role responses throws before cancelling it.

The first corrected candidate passes10 cases, and the expanded candidate
passes27 new/all117 scoped cases. Review then identifies two stale-feedback
paths. Three executed negatives retain the old Level refusal reaching a
replacement Player, a Started notice sent after cancellation, and an old Role
cancel sent after the new check's Started. The fixes pass all30 new/all120
unique affected cases against the final runtime. Both final review axes have
zero remaining findings.

Tests use the actual parsed queue/party/random/leave/role-response handlers,
concrete matching manager, proposals, role checks/members, characters and
eligibility validator. Registry/session/map/catalog collaborators are fixtures;
the final queue backend records validated admission and installs the real
proposal/Character queue state. Existing matching, party/requeue, wait-time,
role, deserter, backfill-validation, cleanup and packet/diagnostic controls
are included. This is not a full matchmaker, broker or socket execution result.

The [portable record](coverage/matching-admission-cancellation-20261106.json)
binds the fixed base, exact Source and DLL identities, original/review
negatives, final scoped results, builds and both review stages. Initial fixture
compile errors and unsupported DispatchProxy init-setter failures are retained
separately and excluded from the production-negative count. Local evidence is
under `artifacts/continuation-20261106/matching-cancellation/`. The isolated
candidate excludes concurrent root work. No full solution or full suite ran.

The final owning/test-only builds are clean in16.78/17.40s; the final120-case
test body takes0.442s. Compilation and
fixture correction dominate this gate, rather than database/provider work.
No persistent schema changes or new provider workload is part of this slice.

F-010 remains partial. Post-admission matchmaking/group/transfer/publication
races, broader concurrent lifecycle, accepted replacement/merge sequencing,
nonzero raid timing and the remaining diagnostic/emitter semantics retain
their separate gates. Actual catalogs, broker/socket/deployment and native/UI/
retail qualification remain open. Client and Computer Use remain stopped.
Continue the wider roadmap; do not repeat this completed admission slice.
