# Contract acceptance identity and persistence

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

Earlier checkpoint8474: [Contract completion transaction](CONTRACT_COMPLETION_TRANSACTION.md)
implements the receipt/point/terminal-graph persistence core at8474 with8,474 full
tests,629 MySQL cases and populated migration preservation. Live completion,
ordinary reward composite ownership and Contract modifier capture remain required.
The runtime stays on8378; current completion code/migrations are not staged.

Checkpoint8378 (31 October2026, Europe/Zurich) adds a durable identity at actual
supported Contract acceptance. It preserves that identity through normal saves,
shared Challenge/Quest transactions, retries and relog. A real receiverless accept
for Contract9880 survives four socket world sessions. Contract completion receipts,
point credit, selections, claims, reset and AccountItem handoff remain required;
this change enables none of those reward operations.

The source/catalog mapping remains in
[CONTRACT_REWARD_TRACK_IMPLEMENTATION.md](CONTRACT_REWARD_TRACK_IMPLEMENTATION.md).
This identity is a server persistence mechanism, not a claim about the client's
internal GUID format or retail completion/reset policy.

## Lifecycle and ownership

`QuestManager.QuestAdd(IQuestInfo)` allocates one lowercase, nonzero32-character
GUID only when a new acceptance belongs to a concrete Contract with a matching
canonical reward source. Mentioning a Quest does not allocate one. Re-adding the
same active concrete Quest retains its identity, including a historicalNULL.
Acceptance after abandonment, completion or botch gets a new generation. Ordinary
quests, unsupported Contract rows and custom managers without the additive
source capability keep their existing behavior and have no reward authority.

`Quest.ContractAcceptance.cs` keeps the scalar under the Quest persistence gate.
The normal Quest row snapshot and the shared Character Challenge/Quest snapshot
carry it with their existing concrete owners. Acknowledging an older frozen write
cannot overwrite a later actor acceptance. Rollback and uncertain/lost commit
acknowledgement retain the same generation. Loaded malformed identities and
non-Contract identities fail before publication.

The nullable `character_quest.contractAcceptanceId` column is an EF concurrency
token. An older context cannot update a row after another acceptance generation
was installed, even if that replacement happened after its initial read. A unique
`(id, contractAcceptanceId)` index prevents one character from sharing a nonNULL
generation between Quest rows. HistoricalNULLs remain valid and are never inferred
to be previously rewarded or newly accepted.

The normal packet acceptance checks remain authoritative: offering, prerequisites,
receiver/item validation, repeat/reset and capacity checks are preserved. The
socket scenario never resends an accept for an already active Quest; that packet
path already rejects duplicate active acceptance. Its second process observes the
retained identity without creating a new acceptance.

## Additive schema qualification

MySQL migration `20261030230416_ContractQuestAcceptance` and SQLite migration
`20261030230524_ContractQuestAcceptance` add only the nullable `char(32)` column
and unique index. Neither has a default, backfill, receipt or award operation.
Both provider snapshots match their current models. Down migrations were not run.

A populated SQLite previous-schema upgrade/replay retains the old Quest and
objective values withNULL identities. For MySQL, an actual runtime-schema clone
retains all59 tables,22,953 old rows and eight local extension columns. The
qualified additive migration and idempotent replay preserve every old field and
all175 historical Quest identities remainNULL.

The first MySQL proof used a fresh migration-only schema and correctly stopped
because it lacked those eight runtime extensions. The exact-schema clone then
applied/replayed successfully; its initial verifier mishandled MySQL's lowercase
migration-history table name. A separate read-only verifier checked the retained
before manifest and already upgraded clone, allowing exactly one new migration
history row. Original code, failures and both schemas remain retained.

Only after those gates passed, the owned isolated World and Character API were
stopped. Applying the same migration twice to
`nexus_assessment_20261029_character` preserved all59 tables/22,953 old rows and
175NULL historical identities. All private configurations stayed unchanged.
Frozen World/API outputs were staged; the other seven service identities stayed
unchanged. The original checkout and databases were not upgraded.

## Real socket acceptance and relog

The read-only actual-table audit verifies Contract9880: level50, unrestricted
faction, no extra prerequisite, offered group52, PvE track5, quality1 and50 base
points. Base points are source metadata only; no points were awarded here.

`Tools/NexusForever.LoadTest` now provides `run contract-acceptance`. It requires
an explicit owned numbered Load fixture, matching local assessment schemas,
loopback endpoints and an unused evidence directory. It uses the existing normal
STS/Auth/world handshake and mapped Quest accept sentinel300, then reads mapped
Quest initialization and state-change packets with bounded collections.

Account27/Character74 (`Load9701A`) accepted9880 through a genuine packet. Normal
logout persisted the identity; the next login published the accepted Quest and
preserved it. A fresh harness process then completed two more sessions without
sending another accept. The same identity remains accepted, offline, with latest
presence epoch26 closed. Exactly one nonNULL identity exists; the175 historical
rows remainNULL. Fixture inventory, Character currency, Challenge tracks/slots
and medal/claim receipts have identical before/after hashes. No fixture placement,
Quest/token SQL writes, forced objectives/completion or reward commands were used.

The server log independently verifies four admissions/bootstrap/EnteredWorld/
disconnect/cleanup cycles and four initial reward reloads, with zero errors,
unfinished admissions or late/duplicate bootstraps. All nine services remain
healthy, six queues drain naturally, and five fixture epoch owners close offline.
Original77 tables/21,989 rows,43 copied legacy characters, default entitlements
15:1/17:1/no44 and private configurations remain preserved. World PID1880 and
Character API PID33508 are the current owned replacements. Native UI and walking
were not exercised; Computer Use remains stopped.

## Verification and retained evidence

Evidence root:
`I:/GIT/NexusForever-assessment-report/remaining-goal/contract-acceptance`.

- Full solution build: **0 warnings,0 errors**. Full suite: **8,378/8,378**, zero
  skipped.45 new cases since8333:21 persistence,3 schema and21 harness cases.
- The persistence cases cover actual manager lifecycle, terminal reacceptance,
  historicalNULL handling, malformed identities, custom managers, concrete shared
  captures, older acknowledgements, rollback/uncertain commits, duplicate identity
  constraints and stale writers. The harness cases verify actual packet writers/
  readers, alignment, initial-state replacement, malformed/bounded collections,
  canonical identities and foreign fixture rejection.
- Exact-source MySQL replay: **536/536** cases, including all21 new persistence
  cases. Two frozen provider model gates and31 unchanged compatibility probes pass.
  Final harness changes preserve all23 copied helper sources and167 backend DLLs
  byte-for-byte; `provider-source-bridge.json` binds these gates to the8378 build
  without claiming another provider run.
- `final-review3` freezes5,794 Source/tool files,20 changed paths since8333 and169
  DLLs. The two changed DLLs after the8357 provider qualification are the load
  harness and test assembly. Runtime backend DLLs are identical.
- `MigrationVerify/result.json`, `RuntimeMigration/result.json`, both
  `run*/contract-acceptance.json` reports, `runtime-final-state.json` and
  `lifecycle-log-audit.json` retain the distinct migration, socket and preservation
  results. An initial scenario wrapper stopped before opening a connection while
  World was still loading maps; its log is retained separately.
- Content validation remains31 CSVs/167,484 rows, all `not_retail_complete`.
  The unavailable June19 Q5597 bundle is not recovered or inferred. Its existing
  dated review and native UI gap remain explicit. No commit/push was made.

## Remaining connected work

The acceptance-keyed receipt/point/exact-Quest persistence core is implemented
and verified at8474. Connect the live completion producer with explicit ordinary
reward composite ownership and the8563 verified actor modifier capture, preserving the
existing FIFO/normal-save ownership rules. Preserve all ordinary Quest rewards,
objective/script effects, consumed items, achievements and later actor intent.

Then implement prerequisite-filtered persistent selections, the explicit accepted
Contract repeat/reset/carry policy, all selectable Item2/AccountItem choices and
final currency awards. AccountItem2485/2486 require an idempotent Auth wallet
handoff/outbox with recovery across Character/Auth commits. A saved acceptance
identity alone cannot grant credit or substitute for that receipt/handoff.

Provider/crash/duplicate/cancellation tests and genuine Contract objective,
completion, earning, claim and reset socket checks remain required. Track35 Zone
earning still needs independent producer evidence. All broader assessment,
upstream, combat, AMP, content and native UI gates remain in the active Goal.
