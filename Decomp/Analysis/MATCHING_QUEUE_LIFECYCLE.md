# Matching queue installation and leave publication

6 November 2026, continuing the published
[admission and role-context result](MATCHING_ADMISSION_CANCELLATION.md).
The parsed public queue/party/role/leave flows now preserve cancellation and
replacement ownership through the real queue, group, team, matcher and map
selector. MatchManager is the end boundary of this qualification.

Queue admission installs every proposal member's Character membership and the
group's teams, flags and map cache before join feedback. Cancellation during
that feedback removes the installed state, stops later peer join packets and
cannot hand an empty/finalised group to immediate matching. Each join recipient
gets a fresh packet. Ordinary merge/map selection, configured debug immediate
matching and admitted logout/login readback remain covered.

Leave and leave-all retain their original targets. Group/proposal/Character
detachment completes before role-cancellation or leave feedback. Leave-all
detaches every original queue type before publishing any removal notice. The
existing immediate removal methods use the same deferred publication path.
Reference checks prevent old cleanup from removing later state. Per-Character
admission generation and captured Player/session ownership stop obsolete
untyped leave packets after a later admission, including a different queue type.
Without replacement, normal leave-all still publishes both removal sequences.

## Executed evidence

The initial seven public real-backend cases reproduce five genuine failures
on `c859448a9836f57969856d38c5024215eb517e68`: ghost state after leave/all,
stale party join fanout, cancelled immediate matching, and partially attached
party/group state during leave feedback. Two ordinary controls pass. An initial
test import ambiguity is retained separately as a fixture compile failure.

Review produced four further executed failures: an old untyped leave after a
different-type join; replacement party removal during role-cancellation feedback
for leave and leave-all; and sequential multi-queue removal that sends an old
tail after a new join. The last reproduction records both old state still
attached at the first notice and the final stale `ServerMatchingLeftQueue`.
These failures are corrected together in the final candidate.

All 19 new cases and all 139 selected matching/packet/diagnostic cases pass.
The final owning WorldServer build and test-only build are clean in
14.72/14.09 seconds; the final test body takes 0.416 seconds. Compilation,
executed review regressions and fixture preparation dominate validation.
Both final Source review axes have zero remaining findings, recorded in the
[portable record](coverage/matching-queue-lifecycle-20261106.json).

That record binds all 11 owned Source paths, four runtime/test DLLs, meaningful
baseline/review negatives, final selected results, source-isolation boundary
and review findings. Local evidence is under
`artifacts/continuation-20261106/matching-queue-lifecycle/`. The isolated candidate
excludes concurrent root changes. No full solution build or full test suite ran.
The latest 20 all-state upstream PR heads, bases and states are unchanged;
#608 remains open and #609 closed/unmerged.

## Remaining boundary

F-010 remains partial. Match proposal/response/transfer lifecycle, broader
replacement/merge sequencing, nonzero raid timing and outstanding diagnostic
producer/emitter semantics remain separate. Maps 71/72 and test team sizes are
synthetic supported fixtures; this is not actual-table or retail queue-policy
qualification. No database/provider, broker/socket/deployment or native/UI work
was performed. Client and Computer Use remain stopped; concurrent Source/docs,
client-data recovery and provider workloads are preserved. Continue the wider
gameplay/progression roadmap after publishing this Source milestone.
