# Contract completion receipt and point transaction

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

Earlier checkpoint8679: [Atomic Contract/ordinary Character save enrollment](CONTRACT_NORMAL_SAVE_TRANSACTION.md)
qualifies checkpoint8679: **8,679 full tests**,116 new/116 focused,853 MySQL cases
(680 prior +116 new +57 configured-retry cases), two model gates and31 compatibility
probes, with zero build warnings/errors. The complete enrolled Character graph,
receipt/points and exact Quest SQL witnesses share one serializable unit. Real
frozen Quest and ordinary Currency saves, partial stubs, later intent, suppressed
writes, rollback and lost acknowledgement are covered. No model/migration changes
or entitlement-default changes are introduced. The live Player completion journal/
FIFO and Auth handoff, then selection/claim/reset/publication/socket qualification,
remain required; no live earning is enabled. The qualified8378 runtime, all9 service
identities, originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain
preserved. Computer Use stays stopped and the full assessment Goal remains active.

Earlier checkpoint8563: [Contract modifier ownership](CONTRACT_REWARD_MODIFIERS.md)
verifies all12 actual effect lifetimes, whole-scalar projection and frozen modifier
inputs at8563:8,563 full tests/680 MySQL cases, plus real-table projection. Signature
and entitlement44 remain Challenge-only. Live actor/ordinary reward composite
integration, selection/claim/reset and Auth handoff remain required. Current code
is not staged; the qualified8378 acceptance runtime remains unchanged.

Checkpoint8474 implements the persistence core for a Contract completion receipt,
point increment and exact terminal Quest graphs in one Character transaction.
At checkpoint8474 the real `QuestComplete` producer was not connected. Its ordinary item,
currency, XP, reputation, achievement, script and Auth effects still need explicit
composite ownership, alongside verified Contract modifier capture. This checkpoint
does not enable live Contract earning, selections, claims or reset.

The running isolated backend remains the separately qualified8378 acceptance
build described in [CONTRACT_ACCEPTANCE_PERSISTENCE.md](CONTRACT_ACCEPTANCE_PERSISTENCE.md).
Its one accepted Contract and existing Challenge rewards remain intact. The full
assessment Goal remains active, including all broader upstream and retail gates.

## Implemented transaction boundary

`ContractCompletionCommand` copies the character, accepted generation, immutable
canonical source description, caller-evaluated points and evaluation provenance,
ordinary reward choice, satisfied prerequisites and complete scalar Quest changes.
The main target must complete that exact nonempty generation. An already completed
same-generation baseline cannot become a new completion. Missing/foreign/duplicate
graphs, unsupported owners, absent required prerequisites, invalid audit times and
empty evaluation provenance are rejected. Commands retain no player or actor
callback. The evaluated amount is an internal input, not a newly inferred retail
multiplier policy or permission to award points.

`QuestTransactionParticipants` now contains the existing Challenge repository's
complete Quest/objective SQL staging, exact baseline/replay checks, provider reset
encoding and terminal witnesses. Challenge repository methods delegate to it;
Challenge earning, claims, actor capture and adoption retain their existing paths.
Contract transactions reuse those scalar graph mechanics without creating a
Challenge attempt or synthetic medal.

`ContractCompletionRepository` requires a fresh context and caller-owned
serializable transaction. It locks the existing Character owner before inspecting
missing receipt/track rows. A new completion stages its receipt, one point
increment and revision, the completed Contract and all captured side-effect Quest
graphs together. Existing points/cycle/active fields are preserved except the
explicit increment/revision. Arithmetic overflow rejects the entire unit. It does
not cap, sample, choose, activate, reset, carry, grant or consume rewards.

The receipt key is `(characterId, acceptanceId)`. A versioned canonical payload
retains the exact source, evaluated amount/provenance, reward choice, prerequisites
and before/after Quest graphs. A duplicate must match the payload and scalar
ownership; it cannot change the amount, choice or graph. Audit time retains the
original receipt time and does not rewrite replay identity. A later accepted
generation and later track points can coexist with replay of the old receipt;
that read-only replay never restores the old completed Quest.

Staged entities, original concurrency values, target values and SQL witnesses
are checked before flush/explicit commit. Changed/detached rows, unrelated
enrollment, partial/suppressed graph writes and raw SQL tampering cannot commit
only the receipt or only the Quest. Savepoint uncertainty and terminal context
ownership remain rejected. Provisional results cannot be published as committed.

`ContractCompletionExecutor` runs fresh complete units off the actor stack through
the configured provider execution strategy. After a write/commit fault, it checks
the durable receipt with a fresh context and the same frozen command. Receipt
presence with the matching payload proves credit; absence requires the exact
before graphs before another attempt can proceed. Cancellation does not establish
that a commit failed, so recovery observes independently. Unavailable recovery
reads retain an uncertain outcome and exact, single-use executor/command/database
proof. Equivalent replacement commands, copied result records and foreign
executors cannot use that proof. Reused contexts are rejected without disposing
another live operation's context. No actor adoption or packet publication occurs
in this executor.

## Additive storage

MySQL migration `20261030234801_ContractCompletionReceipts` and SQLite migration
`20261030234818_ContractCompletionReceipts` add only:

- `character_contract_completion`: character/accepted-generation identity,
  Quest/track/base/evaluated points, versioned frozen payload and audit time.
- `character_contract_reward_track`: character/track identity, cycle, points,
  concurrency revision and active state.

Each table belongs to its Character. Receipt history has no cascading dependency
on a removable Quest or resettable track. Neither migration infers history,
backfills points, modifies an existing table or changes entitlement defaults.
Both provider snapshots match; Down migrations were not executed.

The populated SQLite upgrade starts at the accepted-generation migration, retains
the existing token/Quest/objective values, and leaves both new tables empty after
upgrade/replay. MySQL qualification clones the actual59-table runtime schema,
including eight extension columns,36 existing migration-history rows and all
22,956 current rows. Upgrade/replay preserves every old column value and the one
actual accepted identity across176 Quest rows; both new tables remain empty. The
runtime source is read-only throughout and still lacks these new tables.

The first retained scratch target exceeded MySQL's64-character advisory lock-name
limit after EF added its migration-lock suffix. No migration ran on that target.
`MigrationProof2` uses a fresh shorter target and passes. Both targets, original
programs, before manifests and the initial failure remain retained; no shared
tool/provider configuration was changed.

## Verification

Evidence root:
`I:/GIT/NexusForever-assessment-report/remaining-goal/contract-completion`.

- Full solution build: **0 warnings,0 errors**. Full suite: **8,474/8,474**, zero
  skipped.96 new cases since8378:83 repository/command cases,10 executor cases and
  three schema cases.148 focused cases include the49 existing Challenge/Quest
  transaction cases and three existing acceptance migration cases.
- All51 supported actual Contract source descriptions are passed through the
  repository with complete source catalogs and one atomic receipt/point/Quest
  transition plus read-only replay. Character42, achieved Quest/objective graphs
  and evaluated base-point inputs are synthetic fixtures. These cases do not
  execute actual Contract objectives, ordinary reward delivery or a native client.
- New failure cases cover distinct later acceptances, old replay after later
  points/generation changes, cross-Quest graph creation/deletion and ordinal zero,
  stale baselines, mutated/detached/original values, post-flush SQL tampering,
  rollback/flush failure, overflow, conflicting payloads, context boundaries,
  malformed inputs, commit acknowledgement loss, unavailable recovery reads,
  cancellation and exact proof/context lifetimes.
- **629/629 exact-source MySQL cases pass**, with zero exclusions: the prior536
  Challenge/Quest/acceptance cases rerun against the shared-helper extraction,
  plus all93 new Contract repository/executor cases. The provider runner includes
  all51 MemberData sources rather than silently omitting their theory. Complete
  snapshots precede scoped synthetic-owner cleanup in a fresh retained schema.
- Two frozen provider model gates and31 unchanged compatibility probes pass.
  `final-review` freezes5,807 Source/tool files,18 changed paths since8378,
  169 DLLs and27 current case/helper source files. Early test compilation fixes
  and the first compatibility invocation's incorrect project filename remain
  separate retained logs.
- Read-only preservation checks the live8378 backend, all nine unchanged service
  identities, six naturally drained queues, five closed fixture epoch owners,
  original77 tables/21,989 rows/43 copied legacy characters, accepted Contract,
  private configurations and exact defaults15:1/17:1/no44. The completion build and
  its migrations are not staged there. Computer Use remains stopped.
- Content validation remains31 CSVs/167,484 rows, all `not_retail_complete`.
  The unavailable June19 Q5597 bundle remains unavailable. The original checkout
  is clean; branch/head remain unchanged. No commit/push was made.

## Required connected implementation

The new repository intentionally rejects unrelated entity enrollment. Attaching
`Player.Save` or granting ordinary rewards around a separate point write would
not complete this feature. Integrate explicit composite Character participants
that preserve consumed/pushed items, all ordinary rewards, XP/reputation,
achievements, scripts and the captured terminal Quest graph in the same durable
operation. Preserve the existing actor FIFO, frozen normal-save ownership, later
dirty intent, quarantine and committed-publication rules. The standalone executor
must not become a second save owner that races those paths.

Contract-specific modifier ownership and the frozen command factory are verified
at8563. Wire that capture into the current production actor and its composite
completion unit before enabling credit. The mapped Contract API returns formula
base points; the Challenge multiplier and entitlement44 are not evidence for a
Contract bonus. Freeze the actual evaluation/provenance rather than recomputing
it during retry or borrowing a Challenge receipt.

Then persist prerequisite-filtered selections and implement the explicit accepted
Contract repeat/reset/carry policy, stale/unpublished/busy request guards and
composed presentation alongside Challenge/Loyalty. Preserve all726 choices,
Item2 quantities/binding, final credits and AccountItem2485/2486 compensation.
The latter needs a durable idempotent Auth handoff/outbox across Character/Auth
commits, including crash and ownership-change recovery.

Provider and genuine objective/completion/earning/choose/claim/relog/reset socket
qualification of the connected path remain required before live enablement.
Track35 Zone earning still needs independent source evidence. Native UI requires
explicit Computer Use resumption. All remaining assessment/upstream/combat/AMP/
content requirements remain in the active full Goal.
