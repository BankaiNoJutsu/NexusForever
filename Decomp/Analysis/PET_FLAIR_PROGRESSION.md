# Confirmed pet-flair progression

5 November 2026. The real `HandleEffectUnlockPetFlair` producer now carries its
grant through the frozen Character Experience graph. Unlock notices wait for
confirmed storage, and initial ownership bits use confirmed rows. The
[portable record](coverage/pet-flair-progression-20261105.json) binds the scoped
Source files, final assemblies, retained failures and selected checks.
F-026 remains partial.

## Accepted behavior

Accepted flair IDs join the same immutable snapshot as pet customisations and
other owned progression. A grant arriving during Auth belongs to the next
Character snapshot. Rejected writes preserve intent; uncertain commit outcomes
use the existing whole-graph stored observation before acknowledgement. Unknown
loaded flair rows survive missing static metadata without becoming advertised
unlocks. No schema or entitlement change is needed.

The direct/legacy row adapter also retains its dirty insertion through staging,
rejection and rollback. It pins its first database route and acknowledges the
append-only row after a confirmed commit and stored observation. Retry can
observe an earlier uncertain insertion without attempting a duplicate key.

For real Players, confirmed acknowledgements turn retained grant intent into
immutable unlock descriptions. Publication stays on the original World queue
and checks the original Character/realm, Player/manager, account/session/epoch,
account session, player registry and disconnect state before each packet.
Loading grants persist quietly. Accepted earlier packets advance their cursor;
a rejected mutable packet retains its position and is rebuilt fresh for retry.
An actor replacement cannot inherit the remaining notices.

Initial ownership lists likewise copy confirmed bits and packet data. Slots that
refer to unconfirmed flair IDs are hidden in that list. This milestone covers
flair grant rows, unlock deltas and initial ownership bits; existing incremental
cosmetic name/slot timing is a separate boundary. A queued initial snapshot keeps
its captured bits through retry, followed by any later confirmed unlock delta.

The producer checks the existing 14-bit unlock ID domain before narrowing. The
manager checks the existing 4,096-bit initial mask bounds before accepting a
grant. These are packet representability guards, not new catalog or retail rules.

## Failures and validation

Three executed baseline probes failed against unchanged production Source:
an unlock was sent before storage, a rejected insert vanished on retry, and an
Auth-time grant entered the older frozen save. All three pass after integration.
The expanded focused set contains16 cases, including lost acknowledgement,
loading, duplicate grants, unknown loaded rows, original actor replacement,
mutable delta/list retries, raw ID narrowing and direct-row rollback.

All299 affected cases pass, including the16 focused cases, with zero failures
or skips. Exact group totals are in the portable record. The
selected coverage includes pet managers/handlers, the real spell collection
producer, shared Experience and normal-save snapshots, Character atomic saves,
exploration/reputation, Archive, LAS and ordinary Guild/Community credit creation.
Owning WorldServer and test-only builds are clean. No full solution or full suite
was run. An early fixture compilation correction and missing spell trace metadata
are retained separately from the three production negatives. The later expanded
fixture compilation used the actual mask buffer API before runtime validation.

## Standards

The independent read-only Standards review reports0 findings and no actionable
smell-baseline issues. The unused live flair-save helper was removed; final
packet mutation coverage changes only the fixture buffer API.

## Spec

The independent read-only Spec review reports0 findings. It confirms the real
producer/frozen save/confirmed ownership boundary and unknown-row preservation.
Both full reports remain in the linked local evidence set. Findings per axis:
Standards0, Spec0; neither axis has an outstanding issue.

## Remaining qualification

The latest20 all-state upstream PRs retain their previously reviewed heads,
bases and states, including608 open and609 closed/unmerged. This is a metadata
refresh, not additional implementation. Concurrent duel work in the shared tree
is recorded as a separate build dependency and excluded from this task's source
scope and commit staging.

The fixtures use synthetic table rows and disposable Character SQLite storage.
They establish Source behavior only. Actual catalog/prerequisite completeness,
the public item-consumption settlement boundary, physical restart/autonomous
publication recovery, configured providers, sockets/deployment, native timing
and retail behavior remain open. Default entitlements15:1/17:1 are preserved;
44/seven grants are not enabled. Continue the broader actionable gameplay and
progression queue without repeating this completed grant slice.
