# Contract and reentrant Challenge transaction composition

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

The following records the earlier8784 composition checkpoint.

Checkpoint8784 preserves ordinary Contract rewards when their gameplay callbacks
activate, advance, finish or terminate a Challenge. These transitions and the
Contract receipt/points now commit in the same serializable Character transaction.
The full build has zero warnings/errors; all **8,784 full tests**,105 focused cases,
1,063 exact-source MySQL runs, two provider model gates and31 compatibility probes
pass. This slice adds35 tests. MySQL reruns the993 prior cases and includes35 new
cases plus the same35 with configured `EnableRetryOnFailure`.

## Ownership before callbacks

The Contract producer acquires both its frozen Quest scope and the Challenge
persistence lease before ordinary reward callbacks. A companion journal records
immutable Challenge commands synchronously, before any nested item/Quest callback.
Each command retains its exact predecessor and resulting Challenge state. A
competing owner, raw save or independent reward request cannot escape that lease.

The Quest capture depth is included in pending reward work. A nested Player save
therefore waits even while a reward callback is running before its capture has
been appended. Reverse Contract requests during independent Quest reward work
retain the existing deferred completion queue and accepted-generation checks.

## One SQL outcome

Each Challenge command uses a fresh EF tracker on the outer Character unit's
existing connection and serializable transaction. Fresh trackers satisfy the
repository's one-operation contract without weakening its baseline checks. They
own neither connection nor transaction, cannot commit independently, and propagate
rejection to the parent. Only the final parent may commit.

Participant targets are captured before flush, checked after flush, and folded
into the parent's sealed save graph. The last target of each row key replaces its
earlier intermediate target. Character ownership and deletion witnesses remain
enforced. Command-owned attempt, medal, track and slot targets are protected;
the final Challenge root alone may enroll its captured ordinary-save target.

The Contract receipt/points and every ordinary Character manager then join the
same final graph. Commit validation reads the resulting Challenge aggregate using
a read-only participant. Only a successful parent task publishes that aggregate
on the actor. The normal snapshot retains late actor intent as dirty. Auth still
precedes Character in the existing Player flow; account-side delivery remains a
separate idempotent handoff/outbox requirement.

A rejected input-mailbox experiment is absent from the source. Deferring an input
until an acknowledgement would lose it if SQL committed but the acknowledgement
was lost. This composition instead gives Contract, Challenge and ordinary Character
effects one durable outcome. A failed or uncertain task quarantines the old actor
and requires authoritative reload; it never authorizes reward callback replay.

## Regression and provider evidence

The19 new public-producer cases cover prior-medal variants, activation plus all
medals, nonterminal progress, expiry, abandon, legacy direct prizes, Auth/flush/
precommit/lost-ack faults, protected-target tampering, late Auth-gap intent,
reverse reentrancy/nested Player saves and early persistence ownership. The16
participant cases cover shared-transaction trackers, helper-only enrollment,
independent commit/read-only misuse, suppressed writes, target/mask tampering,
foreign ownership and deletion witnesses.

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/contract-composite`.
`full3/full.trx` binds8,784 tests; `focused-final2/focused.trx` binds105. The frozen
proof hashes5,821 Source/load-tool files,14 changed paths since8749,169 DLLs and33
copied test/helper sources. `final-review/MySqlProof/result.json` retains1,063
passing per-case SQL snapshots on fresh schema `nexus_assessment_contract_comp_31a1`.
Cleanup is limited to the verified synthetic fixture owner. The schema is retained.
No mapped model or migration changed in this slice. `freeze.py` and `verify.py`
retain the inventory and evidence checks. Earlier compile/fixture failures remain
in the evidence root; the final frozen runs above passed.

## Runtime and remaining work

Current source is not staged. The separately qualified8378 runtime retains all9
service identities,6 naturally drained queues, original77 tables/21,989 rows/43
copied legacy Characters, accepted Contract and Challenge data, private configuration
and schema. Entitlement defaults remain15:1/17:1, with no automatic44 grant.

Before deployment, qualify the complete upgrade and apply the previously reviewed
Contract receipt/track migrations: the running schema still lacks those two tables.
Source/provider tests do not establish socket or native earning. Contract point/
reload publication, persistent eligible selections, explicit repeat/reset/carry,
Item2 counts/binding, final credits, AccountItem/Auth handoff, full-table ordinary
reward/pushed-item correlation and objective/completion/earn/choose/claim/relog/
reset deployment proof remain. Broader nested producers also require coverage.

Zone35, unknown timed/general Challenge producers, AMP/combat/upstream/content and
the rest of the assessment remain in scope. All167,484 content rows remain
`not_retail_complete`. Computer Use stays stopped. The June19 Q5597 UI bundle is
unavailable and is not requested again. No commit/push was made; the Goal is active.
