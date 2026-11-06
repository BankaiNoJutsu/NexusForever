# Original combat threat relation lifecycle

6 November 2026. Threat cleanup now retains the original actors, managers,
birth IDs/maps and reciprocal concrete entries. Losing visibility no longer
leaves the original peer threatened, and a different object reusing a GUID
cannot receive that cleanup. The existing NPC CombatAI leash-reset routine
completes its target clear, heal/reset and return movement even when threat
removal publication or a script notification fails.

## Logical cleanup and callback ownership

Both original concrete entries retire before notifications. Packet/script
failures are logged and do not stop the remaining logical cleanup. Failed
admission or initial peer lookup cancels the original local entry. Clear takes
a snapshot of entry objects, so an older removal cannot delete a callback-created
replacement with the same IDs. New explicit targets start their own relation
rather than inheriting another object's accumulated threat.

Reciprocal binding verifies that the requesting entry is still owned. Actor
authority is checked again after initial peer lookup and after zero-threat
publication, before invoking add/remove callbacks. This prevents old work from
calling scripts through a replaced owner manager. Normal external-manager
compatibility is retained with the original manager and entry identity.

The public IThreatManager API, threat scoring/absolute-setting arithmetic,
aggro distances, leash timing and packet layouts are unchanged. This is a
Source lifecycle correction, not a new social-aggro or retail policy.

## Reproductions and verification

The original baseline has ten failures and one passing visible-pair control.
It reproduces lost-visibility reciprocity, GUID-reuse redirection, interrupted
multi-target cleanup, reentrant replacement loss, failed admission/manager
replacement, and two real CombatAI leash-reset failures.

Two additional admission counterexamples fail on the initial implementation:
an older request can rebind a freshly replaced peer after a callback, and an
external peer lookup exception can retain local admission. Those are corrected.
Standards review then identifies one P2 spanning two actor callback boundaries;
both owner-replacement negatives fail before the final guards. All intermediate
failures remain retained and are not counted as successful qualification.

All 20 new and all 107 scoped cases pass across ThreatManagerTests and
CombatAITests. Controls include original birth-ID cleanup, fresh target threat,
clear snapshots, normal external managers and authoritative empty-list readback.
Both final review axes have zero remaining findings.

Final owning/test-only builds are clean in 9.74/6.96 seconds. The runner reports
747ms for the final synthetic test execution. These are observed check times,
not server throughput. No full solution build or full suite ran.

The isolated comparison covers 6,300 parent tracked Source files, three owned
tracked changes and two new test files, with eight retained historical
filter/format differences. All 138 DLLs are bound; only Game and Game.Tests
differ from baseline, and owning Game matches its test copy. Foreign working
tree Source is excluded.

The [portable record](coverage/threat-relation-lifecycle-20261106.json) pins
the five Source inputs, baseline/final DLLs, failures, controls, final filter,
timings and both reviews. Raw logs/TRX/source snapshots remain under
artifacts/continuation-20261106/threat-relation-lifecycle/.
The latest 20 reviewed upstream PR heads/bases/states are unchanged, including
#608 open and #609 closed/unmerged.

## Remaining qualification

These are real service/script routines with prepared entity/map/session/movement
collaborators and synthetic positions. Actual world/creature-kit lifecycle,
social aggro, observers, combat/reward/stat producers and broader F-019 remain
open. Logged rejected ephemeral packets do not establish reliable socket replay
or exact native ordering. Arbitrary external-manager reentrancy/concurrency and
deployment/native/retail behavior are not qualified.

No schema, migration, entitlement or setup changed. No provider or native-client
workload was started/stopped; the observed foreign MySQL workload is preserved.
Client launch and Computer Use remain stopped. Primal allocation remains
diagnostic until authoritative essence debit/synchronization is established;
no ledger was selected from the two client caches. The overall Goal stays active.
