# Contract completion in an ordinary Character save

Latest continuation: [Contract track choice through the registered actor queue](CONTRACT_REWARD_TRACK_CHOICES.md)
qualifies checkpoint9122: **9,122 full tests**,32 new/674 focused,1,172 MySQL runs
(1,124 prior +24 choices +24 retry), two model gates and31 compatibility probes,
with zero build warnings/errors. Registered07CF routes Contract0/1 through the
real Quest owner and existing queue. Published/baseline/busy/foreign guards precede
capture; active flags and optional legacy-cycle selections commit together. Choose
provenance records an operation, never fabricated acceptance/history. Other active
types and dirty Quest/Challenge intent survive; lost-ack recovery cannot reroll.
No schema/default changes or runtime staging: the8378 backend,9 identities,
originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain preserved.
Claims/reset/Auth handoff/full-table/socket/deployment/native gates and the pending
reset-policy preference remain. Computer Use stays stopped; the full Goal is active.

Checkpoint8679 introduced a transaction boundary for enrolling ordinary Character
reward changes together with the Contract receipt, point increment and terminal
Quest graphs. At that checkpoint the public `QuestComplete` producer and `Player`
save scheduler were not connected. The8749 continuation above connects the source
producer; runtime deployment, publication and selection/claim/reset remain pending.

Qualification results are recorded under
`I:/GIT/NexusForever-assessment-report/remaining-goal/contract-normal-save`.
Checkpoint8679 passes **8,679 full tests**,116 new/116 focused,853 exact-source
MySQL cases, two provider model gates and31 compatibility probes, with zero build
warnings/errors. The MySQL proof reruns680 prior cases, adds116 new cases and
repeats57 with configured `EnableRetryOnFailure`.

The separately qualified8378 isolated runtime remains unchanged. The original
checkout, existing accepted Contract, Challenge rewards and entitlement defaults
15:1/17:1 remain preserved; entitlement44 is not automatically granted. Computer
Use remains stopped, and the full assessment Goal remains active.

## Atomic Character enrollment

`CharacterDatabase.SaveAtomic` delegates to `CharacterAtomicSave`. A fresh,
exclusively owned context binds one Character identity, begins a serializable
transaction and locks the existing nondeleted Character before inspecting missing
receipt/track rows. The actor enrollment callback runs synchronously on the
caller's stack, once, before the first asynchronous database operation. No actor
callback is retained by the validation or transaction completion observers.

The configured execution strategy scopes the returned asynchronous operation.
Actor enrollment is never replayed by a strategy retry. A synchronous failure
that prompts another strategy invocation is rejected before the actor callback;
asynchronous save/commit errors return to the caller. Failure or uncertainty
requires authoritative actor reload, rather than replaying ordinary gameplay.

A bad context factory cannot steal an existing atomic unit, an ordinary save
already in progress, a caller-owned transaction, tracked entities or a rejected
context. Rejection does not dispose the foreign context. The owned context is
disposed after its terminal outcome. The enrollment callback cannot flush or
commit before the complete graph is sealed; catching that error does not make
the rejected context reusable.

`CharacterSaveGraph` freezes each enrolled row's state, current/original values,
modified-property mask and complete expected SQL target. Partial stubs use the
stored row plus only the actual modified properties, preserving untouched columns.
For written MySQL DATETIME values, the server performs the mapped precision
conversion in its own session. Actual generated defaults are adopted after flush.
Mapped FK paths enforce Character ownership, including Quest objectives and item
owners, and deletion of the locked Character is rejected.

The graph validates before flush and explicit commit. Target/original changes,
suppressed writes, detached rows, late unrelated enrollment and SQL tampering after
flush reject the entire unit. Deleted rows must remain absent. These guarantees
cover EF-owned transactions; direct ADO.NET transaction control is outside the
existing `CharacterContext` validation contract.

## Joining the Contract receipt

`ContractNormalSaveUnit` begins before ordinary managers enroll. It requires the
atomic context and canonical track owner, checks every frozen Quest before graph
and rejects an already-receipted acceptance. After ordinary enrollment, `Enroll`
adds the receipt and increments points/revision with checked arithmetic. Existing
cycle and active state remain unchanged. The receipt's immutable payload retains
the existing command's source, modifier evaluation, choice and complete Quest
graphs. No selection, reward grant, reset or carry policy is inferred.

Receipt and track ownership starts at `Enroll`, before the complete ordinary graph
is sealed. Independent guards retain their original concurrency values, targets
and SQL witnesses. Before commit, complete Quest SQL graphs must also match the
command. Omitting the terminal root, changing an objective or accepted generation,
or suppressing the receipt/points cannot commit the other ordinary rewards alone.

Results from `Enroll` remain provisional. `TerminalCommitObserved=false` means no
positive EF commit observation; it does not prove rollback. A commit interceptor
can throw after SQL committed but before the normal observer runs. Any failed
save task requires authoritative reload regardless of the observed boolean.
An existing receipt is rejected before ordinary enrollment can be replayed.

## Regression scope and remaining integration

The new tests enroll ordinary currency, item and Character XP changes for every
one of the51 actual supported Contract sources, and verify one receipt/increment
and the complete terminal Quest graph. Fault cases cover ownership, early flush
or commit, partial/suppressed targets, pre-seal receipt changes, post-flush SQL
tampering, overflow, reused contexts and lost acknowledgements. Real Quest frozen
enrollment and the existing Currency save implementation are exercised on success,
rollback and later Quest intent. This is source-level enrollment evidence; it is
not a native-client, socket or complete Player completion qualification.

The live integration must still freeze an accepted-generation completion journal
with the normal Player save before Auth work, defer busy/reentrant requests before
ordinary gameplay side effects, preserve the concrete Quest and later intent,
and quarantine failed/uncertain saves. Its transaction must include all existing
ordinary Character rewards. Auth rewards need an idempotent handoff/outbox; this
Character transaction does not make two databases atomic. Historical NULL
acceptances and unmapped modifier owners must retain legacy behavior without
inventing or backfilling points.

Then finish persistent eligible selections, explicit repeat/reset/carry semantics,
Item2 quantities/binding, final credits and AccountItem delivery, publication and
objective/completion/earn/choose/claim/relog/reset provider/socket verification.
Zone35's producer and all broader upstream/combat/AMP/content requirements remain
in scope. The June19 Q5597 UI bundle is unavailable; its historical references
remain superseded by the documented recovery review with native UI checks pending.
No commit, push, runtime staging or schema change is part of this continuation.

## Reproducible checkpoint8679 evidence

The final evidence set is `contract-normal-save/final-review2/VERIFICATION.json`.
`full3/full.trx` and `full-build3.log` bind the final solution run;
`focused-final2/focused.trx` covers all116 new regressions. The provider harness
compiles31 copied test/helper files against169 frozen DLLs. All5,816 tracked or
untracked Source/load-tool files are hashed, with six changed paths relative to
checkpoint8563. No EF model, migration or snapshot changes are present.

`final-review2/MySqlProof/result.json` records853 passing cases and independent
per-case SQL snapshots on the fresh retained
`nexus_assessment_contract_normal_31a1` schema. Scoped fixture-owner cleanup occurs
only there. Both provider models have zero pending changes and zero relational
operations; all31 frozen upstream compatibility cases pass. Runtime preservation
checks retain9 service identities,6 naturally drained queues, all77 original
tables/21,989 original rows/43 copied legacy characters, private configuration,
the existing accepted Contract and exact entitlement defaults.

`freeze.py` and `verify.py` reproduce the source/binary/evidence inventory checks.
Earlier attempts are retained: test-fixture setup failures, corrected lost-ack
interpretation, the intermediate8,670/8,678 full runs, and copied proof harnesses
that refused their initial stale output paths before any database operation. The
final source freeze and qualification use the corrected paths and final guard
against stealing an ordinary save before its transaction exists.
