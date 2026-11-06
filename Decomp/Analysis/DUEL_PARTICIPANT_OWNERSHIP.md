# Original duel participant ownership

6 November 2026. Duel requests, combat permission and termination now retain
the two original Players and their Character/realm, account/session epoch,
session queue, map and map-admission generation. A reused unit ID cannot accept,
decline, forfeit, cancel or win another Player's duel. A participant cannot be
both winner and loser, or receive combat permission against itself.

This implements the Source ownership boundary within partial F-015. It does not
establish observer policy, new PvP eligibility, retail defeat/health rules or
complete duel qualification. The existing request/packet shapes, countdown,
leash, result reasons and achievement types remain unchanged.

## Reproduced failure and integration

All 16 initial focused cases failed against published parent `a535663e`:
three parsed replacement requests, reused-ID permission/prerequisite lookup,
three invalid defeat pairs, reused-ID disconnect, six countdown owner changes,
both Players moving to another map, and a second recipient changing session
during challenge fanout. These are current Source failures, not client-native
observations. No new native reverse engineering was required for this slice.

`DuelManager` retains captured participant records, confirms concrete Player
identity for every lookup, and checks ownership before control/permission work.
The public initiate/accept/decline/forfeit handlers also require that the
requesting session owns its current Player. This prevents an unrelated source
session from controlling an otherwise valid Player/duel. Session/account/queue
replacement, epoch change, map transfer and same-map admission invalidate the
old duel. The surviving original recipient receives normal cancellation when
its own binding is still current.

New map-ownership reads use `Monitor.TryEnter`. A busy admission gate defers
the update rather than advancing/cancelling a duel from an unconfirmed snapshot.
An original defeat arriving during contention is retained because the existing
Player lethal-health producer does not replay that call. It settles once after
ownership can be checked; a later admission change cancels it. A repeated or
reversed result and a later forfeit cannot replace that retained outcome.
This is transient in-process retention, not a durable match-result receipt.

Session removal acknowledges only its own dictionary entries. An expired update
snapshot cannot remove a replacement duel created during a callback. Fanout
uses captured sessions and rechecks each original recipient, including whether
a later duel now owns the unit ID; it cannot follow `Player.Session` to a new
recipient. This does not introduce delivery acknowledgements or packet replay.

## Verification and limits

The final new cases include two real `Player` integrations. Parsed initiate and
accept traverse the actual manager/countdown into `Player.CanAttack`; combat
is denied before activation and for a replacement Player with the same ID.
Parsed forfeit revokes it, and actual `BeginMapAdmission()` on the same map
invalidates it. Achievement managers, transport, sockets and map bootstrap are
fixture adapters; these cases do not claim actual SQL achievement settlement,
damage/death/respawn completion, physical movement or native UI.

Additional controls cover busy ownership with a retained original defeat,
stale admission after release, reentrant replacement during an old update, and
all four foreign-source-session request handlers. The earlier 19 manager cases
now use bound World sessions/accounts/queues, preserving every behavior oracle.
The first real-Player test attempt exposed an uninitialized visibility dictionary
in the fixture; initializing that existing runtime collection fixes the setup.
Its failing artifact is retained. The foreign-session baseline attempt ended
with the .NET host's `Releasing the double mapped memory failed` fatal error,
without a TRX; it is not counted as an executed behavior failure.

All 25 new cases and 138 related cases pass on the final candidate. The related
scope includes all 19 earlier duel cases, PvP cooldown, Unit damage/result and
spell-damage permission controls. An intermediate final host failed its 90-second
protocol negotiation before executing tests; the final `--no-build` rerun passes
all 138 on the already compiled current assemblies in six seconds. Both owning
Game and World builds pass with zero warnings/errors. The
[portable coverage record](coverage/duel-participant-ownership-20261106.json)
records source and tested/owning assembly hashes, results, owning builds and both
primary-agent review axes. No full solution build or full
test suite is run. No runtime tables, migrations, entitlement defaults, broker
services, client assets or local server configuration change.

Remaining gates include full physical/native duel and defeat lifecycle,
arbitrary achievement/packet callback failure recovery, actor-queue and
cross-map concurrency beyond the executed controls, observer broadcasts,
forced-map/open-world PvP and reward/stat policy, process restart, provider,
socket/deployment and retail qualification. The packet has no duel-attempt nonce;
same-owner replay/correlation policy remains separate. Admission reads prove
their observed snapshot, not an atomic lock across both map actors. Existing
preflight and callback locking has not been generally redesigned.

F-015 and the unified restoration Goal remain unfinished. Continue independent
integrated gameplay; native client launch and Computer Use remain stopped.
