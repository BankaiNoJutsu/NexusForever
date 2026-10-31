# Contract completion producer and frozen save journal

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

The following records the earlier8749 checkpoint.

Checkpoint8749 connects public `QuestManager.QuestComplete` to the ordinary Player
save and the qualified atomic Character transaction. It passes **8,749 full tests**,
70 new/70 focused,993 exact-source MySQL runs, two provider model gates and31
compatibility probes, with zero build warnings/errors. The MySQL set reruns853
prior cases and adds70 public-completion cases plus the same70 with configured
`EnableRetryOnFailure`.

This source producer is not staged into the running server. The isolated runtime
remains on8378, with9 unchanged service identities,6 drained queues, existing
accepted Contract/Challenge data, original data and private configuration preserved.
Entitlement defaults remain15:1/17:1, with no automatic44 grant. No model, migration
or snapshot changes are introduced. The full assessment Goal remains active.

## Completion and ordinary rewards

The new journal requires the actual Player/QuestManager association, a concrete
Quest with a valid accepted-generation token, a ready Character reward persistence
identity, a canonical Contract source and a verified current-actor modifier. It
captures the source, modifier, ordinary choice, satisfied track prerequisite and
audit time before ordinary rewards run. Historical NULL acceptances, custom
managers without source authority and unsupported modifiers retain their ordinary
reward behavior without invented point history.

A frozen Quest scope is acquired before item reclaim, reward grants, state/objective
updates and gameplay callbacks. Raw Quest/objective saves cannot escape that owner.
The existing ordinary completion body keeps its order: pushed-item reclaim,
automatic/selected rewards, reputation/XP/money, terminal state and related Quest
objectives, repeat-period reset assignment, presentation, achievements/checklists,
Contract achievements and archive refresh. The new helper does not replace these
rewards with a point-only completion.

Nested Player saves wait until ordinary mutation finishes. The ready journal
transfers its exact scope/token into the normal Quest snapshot before Auth work.
Client packet processing waits while the save or a temporary inventory delay owns
the completion. Event continuations remain available. A direct save can finish a
ready journal after that delay and restore packet processing on success.

## Character transaction and settlement

The normal save selects `CharacterDatabase.SaveAtomic` when its frozen snapshot
contains a completion. Before any ordinary Character model enrollment, the
snapshot captures scalar SQL before graphs and constructs the frozen command.
The existing Quest persistence owners still verify their loaded baseline when
staging their frozen targets; SQL changes during Auth cannot silently become a
new actor baseline. After every ordinary Character manager has enrolled, the
receipt and point increment join the same unit. Complete Quest witnesses and the
whole enrolled Character graph are checked before commit.

The actor consumes exactly its captured ticket only after a successful save task
and positive terminal commit observation. A callback, snapshot, Auth, Character
flush/commit or uncertain-acknowledgement failure requires authoritative reload.
The old actor cannot run the same ordinary reward flow again. A lost acknowledgement
can leave a durable receipt even when the EF observer reported failure; that
negative observation never authorizes replay. Later Quest timer intent remains
dirty after an older frozen save and can be saved without another receipt.

Auth still precedes Character using the existing Player ordering. This does not
make the two databases atomic. Idempotent Auth handoff/outbox ownership remains
required for account-side rewards and before claiming complete composite delivery.

## Deferred requests

Busy completions wait before ordinary side effects on the session event queue.
Each request retains its concrete Quest reference and acceptance token, selected
input and communicator flag. Duplicate requests for the same generation retain
one submission. A replaced generation invalidates the old request; the replacement
can retain its own separately queued request. Requests run in arrival order,
including new completions submitted from an older save's completion callback.
Reentrant completion of the same Quest cannot replay its ordinary rewards.

Quarantined or invalidated owners discard queued requests. The queue does not
invent an acceptance, objective success, reward choice, rotation or reset policy.

## Verification scope

All51 actual canonical source IDs exercise public completion with their known
ordinary Quest2Reward prize, real inventory/currency/XP implementations, normal
Player save, one receipt and the expected point increment. Other Player managers
use the existing normal-save fixture's adapters. The static source fixture is a
scalar projection, not a complete retail Quest2 row: pushed-item arrays are
explicitly empty in that fixture, and test XP/money quotes are17/25. These cases
prove integration/ownership, not the missing retail values or native presentation.

The70 cases include busy normal/inventory saves, FIFO across three accepted
Contracts, nested saves, a delayed direct save, raw Quest/objective save attempts,
replacement tokens, frozen modifier removal, later Quest intent, Auth-gap foreign
SQL, callback failure, rollback, lost acknowledgement and legacy/custom fallbacks.
The prior Character/Challenge/Quest suites are rerun against the same frozen build.

Evidence root:
`I:/GIT/NexusForever-assessment-report/remaining-goal/contract-live`.
`full/full.trx` binds8,749 passing tests; `focused6/focused.trx` contains70 new
passing cases. The frozen proof compiles32 copied test/helper files against169
DLLs, hashes5,818 Source/load-tool files and records13 changed paths since8679.
`final-review/MySqlProof/result.json` retains993 passing per-case SQL snapshots on
fresh schema `nexus_assessment_contract_live_31a1`, with scoped synthetic-owner
cleanup only. Both database DLLs and model snapshots remain unchanged from8679.
`freeze.py` and `verify.py` retain reproducible inventory/evidence checks.

Earlier fixture setup/compile failures are retained. The first proof CLI attempts
ran before the asynchronous freeze finished and refused missing project paths;
the same live freeze handle was awaited, and the corrected proof runs passed.
No database operation occurred in those refused CLI attempts.

## Remaining work and deployment gate

Do not stage the new producer into the8378 runtime before qualifying the complete
deployment and applying the previously reviewed receipt/track migration. That
runtime still lacks the two completion tables. Current source/provider tests do
not establish socket or native-client earning.

Remaining requirements include actual full-table ordinary reward/pushed-item
correlation, further composite/reentrant producer coverage, committed Contract
point/reload publication, persistent eligible selections, explicit repeat/reset/
carry policy, Item2 quantities/binding, final credits and AccountItem/Auth handoff,
and objective/completion/earn/choose/claim/relog/reset provider/socket qualification.
The reentrant Challenge terminal-callback limitation at8749 is addressed by
the8784 companion composition linked above. Broader composite/reentrant coverage
and the remaining deployment gates still apply.

Zone35, unknown timed/general Challenge producers, AMP/combat/upstream/content
requirements remain in scope. All167,484 content rows remain `not_retail_complete`.
Computer Use stays stopped; native UI work requires explicit resumption. The June19
Q5597 bundle is unavailable and is not requested again. No commit or push was made.
