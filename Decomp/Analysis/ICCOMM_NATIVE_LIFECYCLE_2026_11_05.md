# ICComm admission, callback and queue lifecycle - 5 November 2026

Later Source result: [join correlation and named scoped channels](ICCOMM_JOIN_CORRELATION.md)
reproduced and fixed the response-name and collapsed-membership predictions
below through the real public handlers. Eleven baseline failures are fixed;
26 selected cases, clean owning/test builds and both reviews pass. This
supersedes the pending Source next step in this research record; its native,
entitlement, quota, reconnect and retail evidence limits remain intact.

The [all-family audit](ALL_REMAINING_BLOCKERS_RESEARCH_2026_11_05.md) left the
typed join owner, indirect sender and callback roles unresolved. This pass
maps those paths in the pinned build16042 client. It makes response-name and
pending/ready/left behavior concrete Source work; entitlement identity, server
quota and reconnect qualification remain open.

## Identity and independent corroboration

REA `inspect_pe_imports` freshly binds `WildStar64.exe` to SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
and evidence `ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
This establishes file identity, not native-provider decompilation. The existing
owned Ghidra12.0.4 project and LLVM provide the separate static observations.
No executable was launched and the archived input was unchanged.

The [authored API/addon corroboration](ICCOMM_WEB_CORROBORATION_2026_11_05.md)
distinguishes the old callback-argument API from the typed channel API, with
immutable revisions, raw hashes and actual Group/Guild callers. These are
historical sources; the matching binary owns the stronger statements below.
Portable [ranges and byte anchors](coverage/iccomm-lifecycle-20261105.json)
bind each selected path. Raw output stays in ignored
`artifacts/continuation-20261105/iccomm-follow-through/`.

## JoinChannel owner and the indirect sender are now mapped

`140763df0` registers `ICCommLib` from the table at `140b76210`, whose exact
JoinChannel pair points to `140764050`. The wrapper validates the `Game.ICComm`
userdata type on its returned object. This is distinct from candidate
`140730c30`, which sends ordinary-chat `01BA` and is not an ICComm sender.

The typed wrapper reads the requested name, defaults a missing channel type
to1, and reads `Game.Guild` userdata for type3. LLVM at `140764102..14076410F`
proves the service call's register arguments: type in EDX, wide name in R8,
guild-context ID in R9. Ghidra's initial wrapper pseudocode omitted these
arguments; that incomplete prototype is not evidence of a parameterless call.
The converted name span must exceed four two-byte units before this path
proceeds. This bounds the ordinary ASCII client-side admission check; it does
not establish all server-side name validation or Unicode policy.

`140631010` looks up the pending channel by **type plus exact requested wide
name**. `140631e70` compares the type, then raw unsigned16 code units without
case folding. Guild context is retained separately in the channel object.
For an absent key it creates the pending object and sends `0546`; for an
existing key it reuses the object and sends again only if the retained guild
context changes. The constructor `14062fb50` initializes ID0 and ready0.
`1406a4060` attaches the new Lua wrapper to that object's observer list. These
are selected client service semantics, not a global server idempotency rule.

## Server replies select existing pending objects

The indexed dispatcher cells and rel32 calls prove the following associations:

| Packet | Dispatch cell / case | Consumer | Observed behavior |
| --- | --- | --- | --- |
| `0547` join result | `1403f26dc` / `1403efe48` | `140630cd0` | Looks up type and name; sets ready iff result equals5; forwards the supplied result to join callbacks. This consumer does not install a channel ID. |
| `0548` joined | `1403f26e0` / `1403efe2a` | `140630930` | Looks up type and name first; adds the ID index, stores ID at channel+18, sets ready+2C to1, then invokes Join5 callbacks. |
| `054A` left | `1403f26e8` / `1403efe39` | `140630b70` | Looks up channel ID, clears ready, invokes Left6 callbacks, then removes the ID and type/name indexes. |
| `054C` send result | `1403f26f0` / `1403efe57` | `140630e50` | Looks up channel ID and invokes the separate send-result callback; an unknown ID sends `0549`. |
| `054E` directed message | `1403f26f8` / `1403efe1b` | `140630850` | Looks up channel ID and invokes the receive callback; an unknown ID sends `0549`. |

An unmatched type/name in the inspected `0547/0548` consumers produces no
observer transition. `IsReady` has an independent exact binding at `140c5d720`
to `1406a4770`, which reads the same channel+2C field. Thus an object returned
from JoinChannel is not itself evidence of completed admission.

Exact registration pairs also settle two old generic labels:

| Method | Callback address | Registration storage in Lua wrapper |
| --- | --- | --- |
| SetJoinResultFunction | `1406a4a00` | `+08` |
| SetReceivedMessageFunction | `1406a4ab0` | `+20` |
| SetSendMessageResultFunction | `1406a4b60` | `+38` |
| SetThrottledFunction | `1406a4c10` | `+50` |

The setter calls into `1406a4280` register callback state; the observed result
consumers invoke the distinct dispatcher `1406a4360`. The old received-message
description for `1406a4a00` is rejected. None of these pairs identifies an
entitlement or maximum joined-channel count.

## Concrete Source compatibility work exposed by these consumers

Current [ICCommManager](../../Source/NexusForever.Game/ICComm/ICCommManager.cs)
uses a normalized Global key but reduces Group/Guild keys to the scope ID. It
returns the guild's display name for Guild joins and a generated Group name
for an empty Group request. Several failure paths return an empty channel name.
The [handler](../../Source/NexusForever.WorldServer/Network/Message/Handler/ICComm/ClientICCommHandlers.cs)
places that returned name into `0547/0548`.

Comparing those paths with the native lookup predicts a concrete incompatibility:
joining Guild channel `AddonSync` can receive a response named after the guild,
which does not select the client's pending `(Guild, AddonSync)` object. A
NoGuild result with an empty response name has the same lookup problem. This
is a **static prediction**, not an executed server/client reproduction.

The next Source slice no longer needs an invented entitlement constant: pin
request-name correlation for successful and failed joins, preserve channel
names within Group/Guild scope, and test pending/ready/left transitions against
the observed reply order. Keep wire-request identity separate from any chosen
server canonical key. Native raw comparison alone does not prove that the
retail server treats differently cased global names as different channels.
The archived guild-name uniqueness rule and actual addon calls provide further
historical constraints in the companion web note.

## Queue throttling has concrete native paths, with data limits retained

Lua SendMessage reaches `140631240` through `1406a47d0`: it queues a message
and returns its local message ID. `140630580` drains per-type queues, checks a
shared service message budget at+90 and a separate type budget, then sends
`054B`. Enqueue success is not a network send or delivery acknowledgement.
`1407a00f0` refills budget state using an elapsed scalar times0.001 and caps
at the configured refill amount. No independent clock-origin or server rate
qualification was performed.

The service constructor `14062fdc0` supplies specific table targets:

| GameFormula row | Selected use and absent-row fallback |
| --- | --- |
| `0422` (1058) | Global upload uses Dataint0, default100; Global download uses Dataint01, default1000. |
| `04BB` (1211) | Group upload numerator uses Dataint0, default4000. The drain can divide it by nonzero client context6C50 field60; that field's complete membership semantics are not established here. |
| `04BC` (1212) | Guild upload/download use Dataint0/Dataint01, defaults1000/1000. |
| `04BD` (1213) | Per-budget limit at+8 uses Dataint0, default500000. The drain compares the type limit against text length; no new byte/bit conversion is inferred. |
| `04C2` (1218) | Shared outgoing message-budget refill amount uses Dataint0, default25. It is shared across the inspected type queues, not a separately observed25 allowance for each type. |

These are constructor fallbacks, not authenticated active table values. The
API15 manual's bits/second wording and the2015 per-type25 statement must not
overwrite the final client's observed budget structure. The upload/download
API bindings and getter paths are independently mapped, but server quotas,
burst accounting, complete recipient budgets and accepted traffic remain open.

`140630340` clears joined readiness, emits Left6 callbacks, sends `0549` for
retained nonzero IDs and removes local indexes. Its caller `1403e3350` is only
partly contextualized here. This is not proof of every disconnect, world-change
or automatic reconnect/rejoin path.

## Validation and remaining gates

All59 selected PE chained unwind ranges and33 explicit anchors (92 byte/hash
checks) agree with the pinned PE and copied Ghidra program. Explicit leaf/code
intervals are identified as intervals, not invented unwind function extents.
Changed labels are applied and focused-exported on the owned copy before
delivery; raw pseudocode is excluded from tracked files. Authored web evidence
retains13 raw-file hashes and exact revision/line citations.

No production Source, packet model or configuration changed. No application
tests, client launch, provider installation or retail capture was performed.
The named entitlement and its applicability, numeric joined-channel quota,
active formula values, server throttle/delivery policy, full leave/reconnect
ordering and persistent-channel policy remain unproved. Those gates survive;
the join owner, sender path, callback roles and selected reply transitions are
now mapped and ready to guide a bounded implementation.
