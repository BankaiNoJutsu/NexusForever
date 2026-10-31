# Contract claims through the existing session actor queue

Latest continuation: [Realm bank persistence and loading](REALM_BANK_CODECS_AND_LOADING.md)
qualifies checkpoint10277: **10,277 full tests**,18 new/207 focused,1,796 MySQL
cases (530 configured-strategy cases), four Auth race probes, four model gates and
31 compatibility probes; zero full-build warnings/errors. Six executed10259
negatives reproduce missing single-write save guards, discarded deletion intent,
changed-owner deletion and post-enrollment location restoration. Inventory bank
writes now share outcome tracking; deletion retains its real Item owner and exact
SQL witnesses. Overlapping outcomes retain earlier failures, while expiry waits
for active bank units without losing elapsed time.103 provider bank cases pass.
Both DB binaries remain identical to the10207 upgrade bindings; all726 Contract
quotes pass. Periodic bank dirty/expiry persistence, general asynchronous recovery,
post-commit publication, public07CE claims, retail reset edges and the broader Goal
remain. The8378 runtime, nine identities, six drained queues, original data/config/
schema and defaults15:1/17:1/no44 stay preserved. No staging/commit/push, native
launch or Computer Use. Q5597 remains unavailable; no repeat request or reset
policy question is pending. The full Goal stays active.

Earlier checkpoint10190: [Contract account delivery and session synchronization](CONTRACT_ACCOUNT_SESSION_SYNC.md)
qualifies checkpoint10190: **10,190 full tests**,25 new/353 focused,1,693 MySQL
cases (including427 configured-strategy repeats), four Auth race probes, four
model gates and31 compatibility probes; zero full-build warnings/errors. Production
Player initialization supplies the Auth factory, and the existing reward queue
serializes entry/relog and post-claim account delivery, wallet publication and a
fresh account-owned Character reload. Eligible30-second unseen-intent polling
recovers late pending or acknowledged work without repeat packets. Original-account/
realm, source transfer/deletion, local/later wallet intent, failure/recovery and
dead-owner boundaries are covered. Character DB DLL stays unchanged; Auth's factory
API changes its DLL, so a fresh populated30-table/145-row Auth upgrade verifies
unchanged schema/data/defaults. All726 actual quotes pass. Public07CE claims,
retail reset edges, further lifecycle/load qualification, actual delivery/copied-
upgrade/socket/deployment/native gates, track35 and the broader Goal remain.
The8378 runtime, nine identities, six drained queues, original data/config/schema
and defaults15:1/17:1/no44 stay preserved. No staging/commit/push, native launch or
Computer Use. Q5597 remains unavailable; no repeat request or reset preference
question is pending. The full Goal stays active.

Earlier checkpoint10165: [Authoritative account wallet refresh](ACCOUNT_WALLET_AUTHORITATIVE_REFRESH.md)
qualifies checkpoint10165: **10,165 full tests**,39 new/328 focused,1,643 MySQL
cases (including402 configured-strategy repeats), four Auth race probes, four
model gates and31 compatibility probes; zero full-build warnings/errors. The
initial unchanged STS outer-timeout failure is preserved with12 passing isolated
socket checks and a complete passing rerun. A private serializable wallet
observation merges unapplied local intent, validates required credit receipts and
publishes once on the original account/session actor. Contract claims also honor
native disconnect/logout flags. Both DB DLLs/upgrade bindings stay unchanged;
all726 actual quotes pass. Public07CE routing, connected account drain/relog and
wallet refresh/source-revision adoption, retail reset edges, actual delivery/
copied-upgrade/socket/deployment/native checks, track35 and the broader Goal
remain. Live-era evidence supports final-reward collection as the reset working
direction; no user policy preference is pending. The8378 runtime, nine identities,
six drained queues, original data/config/schema and defaults15:1/17:1/no44 remain
preserved. No staging/commit/push, native client or Computer Use. Q5597 remains
unavailable and is not requested again. The full Goal stays active.

The following records the earlier10126 actor-queue checkpoint.

Checkpoint10126 connects internal Contract claim admission to the existing
Character reward queue and a retained Quest owner. The full solution builds with
zero warnings/errors; **10,126 full tests**,31 new/178 focused pass. Frozen MySQL
qualification covers1,565 cases (1,362 Character,74 Auth,129 dual-database), with
363 configured-strategy repeats included. Four independent Auth race probes,
four provider model gates,31 compatibility probes and all726 actual-table quote
choices pass.

The Game API remains internal and the WorldServer07CE handler is unchanged.
Production routing waits for connected account intent draining/relog recovery and
retail reset. No subset of canonical choices is exposed as a completed feature.
The full assessment Goal remains active.

## Admission and ownership

QuestManager admits only an earned, active, selected unclaimed row from the exact
Contract snapshot already published for its current Player, Quest/Challenge
managers, account and session. It freezes the complete aggregate, canonical award,
current eligibility and operation identity. Reentrant prerequisite/item adapters
cannot replace that owner. Invalid rows/choices, insufficient points, inactive or
unpublished state, competing saves/inventory work, stale sessions, missing item
metadata, dirty final-credit wallets and changed cap metadata refuse enrollment.

A Contract-specific ticket holds the existing Character-wide Quest reward scope
through preparation, queued execution, actor adoption and publication. It shares
the registered Character reward queue, rather than creating a parallel executor
or bypassing Challenge inventory ownership. Prepared Item2 reservations and any
live credit reservation bind to the exact immutable command and retained database.
The SQL worker receives scalar command data and existing delivery ownership;
Quest/account/session callbacks remain on the actor.

The ticket validates the executor's private terminal proof, exact source/account/
quote receipt and the complete expected Contract aggregate before adopting live
currency. Inventory publication then precedes currency publication; only afterward
does the existing committed-state callback publish composed Contract/Challenge/
Loyalty track state. The ticket keeps its Quest ownership until that complete
publication succeeds. Replaying an already claimed row cannot grant again.

Claiming a nonfinal account-currency choice commits the original durable outbox
intent without pretending that its account wallet has already been credited.
Final choices retain the existing separate2,500,000-credit award. Cycle, points
and excess remain unchanged in this slice; reset is not inferred here.

## Later intent, failure and disposal

Later Quest changes remain dirty for an ordinary save. Later currency operations
use the retained semantic queue from10095, so final-credit adoption cannot replace
them. Normal/direct saves refuse overlap before Auth enrollment. Once the claim
has fully released, the same Player's ordinary save persists those later changes
against the newly committed currency baseline without replacing the claim receipt.

Before-commit failure cannot publish a claim. Lost acknowledgement resolves with
the existing executor and retained operation/delivery identities; a proved commit
publishes once. Conflicting or unproved outcomes quarantine the Quest/currency
owners and retain transaction exclusion until terminal evidence permits release.
This is not a claim of transparent relog or production retry UX.

Soft invalidation and final disposal suppress items, money and track packets even
when SQL subsequently commits. The final worker continuation releases only
terminal bookkeeping and retires captured persistence owners; it invokes no
session/gameplay publication. A gated test stops polling session events before
the commit finishes and proves those leases settle without actor adoption.

## What the tests prove

The31 new cases include12 combinations across both Contract tracks, first/final
milestones and all three canonical choices;14 admission refusals; before/after
commit faults; one interleaved ordinary Quest/currency save; and soft/final
invalidation while commit is gated. They use the real Player, QuestManager,
ChallengeManager, session event queue, inventory/currency owners and SQL executor.
Session/network, scripts and prerequisite surfaces are test adapters. Static
reward identities are actual; delivery ItemInfo metadata is synthetic.

All31 run on SQLite in the full suite and twice on MySQL (ordinary and configured
strategy). The Character proof snapshots each outbox and explicitly cleans only
synthetic Character42 intents because committed outboxes intentionally survive
Character deletion. Other existing Character/Auth/dual-database cases remain
green. Both database DLLs are unchanged, binding the previously qualified populated
Character/Auth upgrades without claiming new migration runs.

Evidence under
`I:\GIT\NexusForever-assessment-report\remaining-goal\contract-claim-queue`
binds5,869 source/tool files, seven changed Source paths (two added),169 frozen
DLLs and45 exact-source case/helper files. The final verifier checks the test
superset, SQL snapshots, provider binaries, prior upgrade/probe bindings, actual
input tables and documentation.

The separately qualified8378 runtime remains unchanged: nine service identities,
six drained queues, original data, accepted Contract, private configuration/schema
and defaults15:1/17:1/no44. No source/migration staging, commit, push, native client
launch or Computer Use occurred. The unavailable Q5597 bundle is not requested.

## Continuing the full Goal

Connect the account outbox consumer and authoritative online wallet merge, including
relog/transfer/deletion recovery. Resolve dirty-wallet admission and complete
production lifecycle behavior, including native disconnect/logout flags in addition
to the explicit invalidation paths tested here, then enable the registered WorldServer07CE route
for every canonical choice. Implement the retail reset transition and its unclaimed/
excess edges with explicit evidence and tests. Actual full-table item delivery,
copied upgrades, connected sockets, deployment/native UI, track35 and the broader
upstream, assessment, AMP, combat and content requirements all remain.

[Retail reset evidence](CONTRACT_RETAIL_RESET_EVIDENCE.md) now includes complete
archive-name inventories, REA-managed archive-library evidence and two fresh
targeted extractions. Both the Contracts addon and English text match their
configured copies. The archive entry's SHA-1 hashes compressed bytes; its difference
from an uncompressed-file SHA-1 was not an asset mismatch. No arbitrary policy
preference is pending, and absent matching strings do not establish retail server
behavior.
