# Commodity posting and fill settlement

Current working source is covered by the **11,354-case passing full gate** in
[auction posting](AUCTION_POSTING_TRANSACTION.md), pin a900f698 with6,156inputs/
nine DLL pairs and an explicit test-assembly hash. Commodity posting behavior and
its existing regressions pass in that current snapshot. This supersedes the old
source-qualification limit; it does not change the historical result below or
establish a cause/fix for its Challenge timing failure. Provider/native/retail
limits remain separate.

## Historical0cdad38c candidate

Full gate FAILED:11,323passed/1failed/0skipped. Session71428 is terminal with exit1.
The Challenge actor-drain failure(timeout,0) passes alone and all nine termination
theory cases pass against the same binaries; its cause remains unproven. These
controls do not qualify the failed full gate. No unchanged full retry is planned.
Continue independent whole-Item auction posting. Evidence is retained in
artifacts/continuation-20261031/commodity-posting/failed-gate-verification.json.

Implementation/review complete in source at0cdad38c/6,151inputs; final Spec0 and
Standards0 findings. All118 focused checks pass, including original-actor,
consumption/delivery, per-fill routing, earning/quest persistence and both expiry
admission regressions. Final affected execution passes1,123cases (4m50s); the full
build passes0warnings/errors (18.04s command/17.41s MSBuild). The full test command
took859.01s. No assertions/deadlines were weakened.
The latest qualified full archive remains commodity returns11,264; current later
Source is not yet fully qualified. Keep configured-provider/native/retail gates
separate.

Public submit must settle incoming debit/Item consumption, matched
resting orders, per-fill seller proceeds and buyer price improvement, purchased
Items/satchel/fixed mail and immediate unmatched refund/return in one receipt-backed
Character transaction, then complete original-actor ordinary reconciliation and
per-packet publication. Preserve best-price/insertion order, fee rounding,
capacity/binding/stacking, self-trade and ForceImmediate behavior. Existing IPlayer
adapters retain legacy behavior; do not expose an incomplete Player posting path.

Eight private-SQLite real public submit cases reproduce success before stored
wallet settlement:100 remains instead of the expected settled balance (e.g.88
for partial buy). Coverage includes buy/sell, resting/immediate and partial/no
match. The first run's eight missing-DI fixture errors are separately retained
and excluded. Source: CommodityPostingCoordinatorTests. Evidence:
artifacts/continuation-20261031/commodity-posting/producer-sql-negative.trx.

The public Player submit flow is now connected. A read-only complete quote retains
best-price/per-fill/remainder arithmetic. The command copies the order/account/
wallet/Item/material/fixed-mail graph, validates full pre/postimages under fresh
serializable context ownership and resolves exact receipt outcomes. Original actors
complete whole-Character preflight and reconciliation, reserve virtual post-sale
bag layouts, adopt confirmed partial/full consumption and delivery, and guard
wallet/Item/material publication. Consumed object references cannot recreate rows;
ordinary witnesses also guard consumed-row absence. Book ownership excludes
competing submit, match, expiry, cancellation and reload, including legacy adapters.

Public baseline integration passes78 cases; behavior-boundaries.trx passes97,
including27 public cases,12 direct command cases and existing return/cancellation/
Item-publication controls. Slot reuse, online counterparties, self trades,
best-price multi-order fills, satchel/mail, later grants, lost acknowledgements,
retirement/replacement and ordinary rollback pass. Scope disposal/request mutation
also pass. The final focused run adds resource refusal, competing operations,
publication mutation/replacement and six forged graph cases. The final review,
affected and failed full results above supersede this intermediate history.

The latest qualified full boundary remains [commodity returns](COMMODITY_RETURN_SETTLEMENT.md):
11,264 full tests. Its archived inputs/DLLs remain valid; additional working Source
is not yet qualified. No full rerun merely on resumption. Historical handoff
verifier Source divergence remains expected; do not alter the old records.

Follow [the source continuation note](../../artifacts/continuation-20261031/commodity-posting-next.md).
No new provider/schema setup, native/Computer Use, commit or push. Defaults15:1/
17:1 retained; no44 or seven baseline grants. Contract missing retail reset/
unclaimed/excess/full-choice delivery evidence remains separate; source/SQLite
work is not configured-provider, deployed/socket/native or retail qualification.

Initial consolidated review at3fd131b6/6,151inputs finds four Spec issues and one
overlapping Standards correctness issue (0documented/advisory findings): net
wallet effects omit positive earning, zero-expiration orders stop matching,
aggregate delivery changes per-fill capacity choices, and legacy admission can
reenter a posting between the fast gate and mutation. initial-review.* preserves
the fixed comparison and both reports. review-negative.trx executes all paths;
routing-negative.trx repeats both routing cases after supplying the missing
concrete mailbox fixture. Real earning quests persist22 instead of26, including
lost acknowledgement and ordinary rollback. Exact details/exclusions remain in
evidence-notes.md. The four fixes are batched; owning/test build passes0warnings/
errors in33.60seconds; later corrected focused checks pass as recorded above.
Final full qualification remains failed; retain the prior qualified archive.

Later review found the legacy fallback and first actor selector could bypass book
admission. expiry-negative.trx and lookup-negative.trx retain each executed failure.
Expiry now acquires admission before actor lookup and atomically installs the
asynchronous return owner before releasing selection; adapter fallback retains
the book throughout. Final pin0cdad38c owns118passing focused cases and reviews0/0.
The earlier1121affected cases atba8e passed; final-affected.trx passes1123cases.

## Failed full gate and narrow controls

The failed case is ChallengeLiveProducerTests.NonCompletingTerminationKeepsOnlyEarnedMedalsAndDoesNotRunCompletionEffects(timeout,0),
duration37.41s, at Actor.Drain's unchanged20s assertion. The run-level TRX stdout
contains the diagnostic; case stdout/stderr alone are insufficient. At elapsed
20.011s, current reward work/lease existed, executing was WaitingForActivation,
pending queue0, eventsTrue, normalSaveFalse. The thread pool had19 threads,
zero queued items and32751 available workers. One fixture connection was open
for18.011s; its latest observed EF event was DataReaderDisposing, age17.785s.
No connection failure was observed. Origin-stack collection total59.377ms,
maximum54.469ms. These non-atomic observations do not identify an active native
holder, prove provider blocking or establish scheduling/production causation.
The event lacks a current managed stack, so the exact intervening work is unknown.
Historical Challenge drain/sharing evidence remains separately qualified in
[currency conditional writes](CURRENCY_CONDITIONAL_WRITES.md).

challenge-drain-diagnostic.txt preserves the complete diagnostic;
challenge-isolated.trx passes the exact case in4s and
challenge-termination-controls.trx passes all nine theory cases in9s. No source
fix was inferred from their passes. No held-file probe, process intervention,
timeout relaxation or unchanged full retry was performed.

final-inputs.json/final-inputs.zip/final-binding.json and
failed-gate-verification.json bind6,151inputs/nine owning/test-copy DLL pairs.
Source/Tools and binaries remained unchanged through the full and isolated runs.
SourceToolsSHA256:0cdad38cfd338274f8563e13f29689ba3902fd89d32afb8ead5c2a4deee7f210
ArchiveSHA256:4a2222d21cbef8ce90a68cb868800972998c7c8443003963dd81534d32dfc13c
Identity verified2026-11-01T21:52:10UTC. This failed-gate identity record is not
passing verification.json. iteration-history.md retains the pre-terminal history.
Upstream incremental PR search at21:51UTC found no updates since21:30:04UTC;
reuse unchanged604/608/609 reconciliation(608open,609closedunmerged).
Evidence:upstream-final.json. No configured-provider/native/retail qualification.
