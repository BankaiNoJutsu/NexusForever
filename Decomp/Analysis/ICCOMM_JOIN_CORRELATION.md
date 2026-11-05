# ICComm join correlation and named scoped channels

5 November 2026. The real public join/message/not-joined handlers now preserve
the requested ICComm channel identity. Guild replies no longer substitute the
guild display name; failed joins retain the name needed to select the client's
pending object. Different addon names within one Group or Guild scope have
separate channel IDs and delivery memberships. The
[portable milestone record](coverage/iccomm-join-correlation-20261105.json)
binds the Source candidate, assemblies, retained failures and scoped results.

## Behavior completed

- Every manager join result retains the exact requested name, including case,
  whitespace and Unicode, on success and failure. The existing public handler
  emits that name and the requested type in `0548` or `0547` respectively.
  It does not send a premature separate success result before the ID-bearing
  joined packet.
- Global backend keys retain the existing trimmed, invariant-uppercase policy.
  Two canonical aliases still share a channel, while each reply echoes its own
  request. Response correlation is independent of backend canonicalization.
- Group/Guild backend keys contain scope ID and exact requested name. Two
  addons in the same scope no longer exchange each other's payloads. Repeated
  joins reuse a membership; `0549` cleanup of one named channel preserves the
  other. Existing cross-scope, offline/stale affiliation, directed delivery and
  ordered sender-echo behavior remains covered.
- Admission rules and packet layouts are unchanged. This slice adds no name
  length rule, entitlement grant, quota, throttle or persistence policy.

The independent inputs are the pinned build16042
[native consumers](ICCOMM_NATIVE_LIFECYCLE_2026_11_05.md) and
[authored API/addon callers](ICCOMM_WEB_CORROBORATION_2026_11_05.md).
`140630cd0`/`140630930` select the pending type/name object for `0547`/`0548`;
the exact lookup is mapped at `140631e70`. Their recorded REA PE identity and
separate Ghidra/LLVM evidence are reused without another provider attempt.

## Verification

Against Source base `eaad953a`, 13 new public-handler cases produced 11 failures
and two passing cross-scope controls. They reproduced altered/empty reply names,
cross-addon delivery and cleanup that removed both collapsed memberships.
The same regressions are unchanged in the final candidate.

The owning WorldServer build and test-only build passed with zero warnings or
errors. The test-only build refreshed dependencies, and the test Game/WorldServer
DLL hashes match the owning outputs. One final scoped run with
`--filter FullyQualifiedName~ICComm` passed all 26 selected cases: 13 new public
regressions, eight existing manager controls and five additional cases selected
by the runner. The portable record retains their individual names. No full
solution build or full suite ran. Build times were 36.44s and 24.09s; final test
bodies took approximately eight seconds.

The code-review skill's independent Standards and Spec reviews each returned
zero findings against the two Source files. No subsequent Source edit changed
that reviewed candidate. All 20 latest updated-order upstream PR heads, bases,
states and merge states match the prior reviewed inventory, including #608 open
and #609 closed/unmerged. No upstream PR was changed.

## Remaining scope

This is Source/public-handler qualification. No native client or Computer Use
ran, so native ready/join/left callback execution and socket delivery remain
unqualified. Scoped exact-name keys are the supported Source policy here, not
proof of retail server case/whitespace equivalence. Circle contexts, one-name
association across multiple guild contexts, entitlement identity, joined-channel
quota, active formulas, server throttling, automatic leave notifications,
reconnect and persistent membership remain separate work. The five chat/entity
auxiliary producer gates are unchanged. Community creation finance remains the
next integrated economic outcome; its residence and Auth owners are distinct
from the completed ordinary Guild credit receipt.
