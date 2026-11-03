# NexusForever fork assessment

[Committed community privacy](../HOUSING_PRIVACY_PUBLICATION.md) is the latest housing recovery result.
The linked record covers 26 SQLite/MySQL regressions and 11,854 full tests.
External, native and retail qualification remain open. Visitor/search index lifecycle has a separate executed follow-up probe;
the broader Goal and remaining housing work stay open.

[Community rename fee settlement](../HOUSING_RENAME_FEE_SETTLEMENT.md) records the preceding verified housing
recovery milestone. Its fee/names commit together and publication follows SQL;
the linked record covers 31 regressions on SQLite/MySQL and 11,828 full tests.
External, native and retail qualification remain open.
Placement publication and remaining housing work stay open; the broader Goal is active.

[Housing donation settlement](../HOUSING_DONATION_SETTLEMENT.md) now commits both original residence
graphs atomically and applies/publishes the transfer from the world update only
after SQL confirmation. All 25 focused/MySQL and 11,797 full tests pass; build,
reviews and 6,226-input/11-DLL bindings verify. Defaults 15:1/17:1/no44/seven grants
and client/CUA stop remain. Fees, other housing publication and external/native/
retail gates stay open; the Goal is active.

[Registered residence recovery](../RESIDENCE_GRAPH_RECOVERY.md) now persists
complete residence/plot/decor/neighbor graphs through original owners, including
community creation and retirement. All20 new/340 affected,20 MySQL and11,772 full
tests pass; build/reviews and final bindings verify. Donation/fees/publication
and external/native/retail gates remain open; the Goal is active.

[Guild achievement recovery](../GUILD_DISBAND_RECOVERY.md#guild-achievement-recovery-verified) retains guild-owned achievements with their original parent through failed,
rolled-back and uncertain saves, including achievement-only periodic saves and
first creation. Progress and reserved realm-first announcements wait for confirmed
persistence and retain immutable retry order. All21 new/439 affected,22 MySQL and
**11,752 full tests** pass; build/reviews and6,219-input/11-DLL bindings verify.
Cross-character atomicity, durable realm-first arbitration, plot-backed housing,
fees, live broker/restart and native/retail gates stay open. Defaults15:1/17:1/no44/
seven grants and client/CUA stop remain; the Goal is active.

[Guild creation graph recovery](../GUILD_DISBAND_RECOVERY.md#guild-creation-graph-recovery-verified) retains the accepted root/data/rank/member
create through failed, rolled-back and uncertain saves, preserves later edits and
defers creation/member publication. Registered actors bind their configured route
before first save; attempted create/disband resolves SQL before cleanup. Arena
types now reload correctly. All21 new/245 affected,21 MySQL and **11,731 full tests**
pass; clean build/reviews and6,218-input/11-DLL bindings verify. Achievement and
plot-backed housing creation, fee atomicity, broker/restart and native/retail
qualification remain open. Defaults15:1/17:1/no44/seven grants and client/CUA stop
remain; the Goal is active and earlier checkpoints retain their own limits.

[Ordinary guild mutation recovery](../GUILD_DISBAND_RECOVERY.md#ordinary-guild-mutations-verified) retains accepted header/data/rank/member
edits and deletion owners through failed saves, preserves later disband edits and
publishes committed net member changes through the periodic owner. All30 new/224
affected and **11,710 full tests** pass, with clean build/reviews and30 MySQL8.4.13
cases. Final6,217-input/11-DLL-pair bindings and owned-workload shutdowns verify.
Failed/uncertain whole creation, financial atomicity, broker/restart and external/
native/retail gates remain open. Defaults15:1/17:1/no44/seven grants and the client/
CUA stop remain. The Goal is active; earlier checkpoints retain their own limits.

[Guild/community name recovery](../GUILD_DISBAND_RECOVERY.md#name-lifecycle-recovery-verified)
reserves names before fees and retains guild/residence name intent and aliases
through failed saves. All14 new/184 affected and11,680 full tests pass; build,
Source reviews and final source/DLL bindings verify. MySQL name coverage passes
as12 retained plus2 corrected strategy fixtures; original failures stay retained.
Database load-provenance source changed; schema/migrations did not. Ordinary
other guild and failed/uncertain creation recovery, atomic fees, broker/restart
and native/retail limits remain open. Defaults15:1/17:1/no44/seven grants and
client/CUA stop remain. The Goal stays active; prior entries are historical.

[Guild disband recovery](../GUILD_DISBAND_RECOVERY.md) now retains accepted deletion
intent across failed/rolled-back saves and holds original actors/names until
post-commit publication succeeds. All 18 new/170 affected and 11,666 full tests
pass; build/reviews and final bindings verify. Current upstream #401's cache-cleanup
requirement is source-reconciled. Ordinary guild field/rename, live broker/restart,
provider and native/retail qualification remain open; entitlement defaults and
the client/CUA stop remain intact.

[Current unreserved bank currency recovery](../DESTINATION_PROVIDER_QUALIFICATION.md) drains late
currency for bank move/split/use/delete, single attachment and unreserved/new
wallet flows, and rejects publication reentry before accepting mutations.
Frozen `2c4aff9e`/6,202 inputs passes **11,634 full tests**, zero failures/skips,
all26 new/144 focused and all prior11,608 labels,26 MySQL cases and clean build/
reviews. Final bindings and all7 recorded workload shutdowns verify; schema/DB
DLLs are unchanged. Defaults15:1/17:1/no44/seven grants and client/CUA stop remain.
The later [scanner-name source gate](../SCIENTIST_SCANBOT_NAME_PUBLICATION.md) passes11,648 full tests
and connects the accepted Scientist profile name to pending/active scanners and
visible08CC updates. Other five aux producers and native/retail timing remain
unqualified. Regular scoped commits/pushes remain authorized after review; the
Goal stays active. Earlier milestone entries retain their historical boundaries.

[Current legacy bank reconciliation](../DESTINATION_PROVIDER_QUALIFICATION.md) retains deferred
currency and the complete original actor through rollback/lost acknowledgement,
then guards vendor/mail publication. Final `824fab02`/6,201 inputs passes
**11,608 full tests**, zero failures/skips, including all 41 new, 100 focused and
1,201 earlier affected labels. All 38 configured-MySQL cases and the synthetic
schema upgrade pass; full build/reviews and final source/DLL/archive bindings
verify. All 14 recorded workload generations and the owned listener are absent.
Failed/interrupted gates remain retained. Defaults15:1/17:1, no44/seven grants,
no client/CUA/commit/push and external/retail limits remain. The Goal stays active
for the next real-Player currency-free/unreserved-wallet audit and broader work.

[Current account keybinding recovery](../DESTINATION_PROVIDER_QUALIFICATION.md) retains accepted
complete account sets through failed/uncertain Auth saves, frozen registration,
later edits and direct-owner/EF restaging guards. Actual Account.Save through
AuthDatabase Save/SaveBlocking settles binding and wallet recovery together.
Defaults 15:1/17:1 remain; no automatic 44 or seven baseline character grants.
Frozen `795eae0a` passes 34 new, 193 affected and 34 MySQL Auth behaviors
(32 retained plus two corrected controls); full build/reviews are clean. The
initial seven-warning provider-cache full and 41 corrected controls remain
retained. Full suite: **11,567 passed, zero failures/skips**. Final source, DLL
and archive bindings, including the test overlay, verify. The owned database and
both runner/test-host/helper generations are absent. No staging/commit/push/CUA/
client launch. The Goal remains active for legacy deferred bank/mail
reconciliation and missing external/retail gates. Historical milestones remain
in the linked ledger.

Snapshot: 29 October 2026, Europe/Zurich.

## Follow-through status: active goal, 29 October 2026

[Auction posting settlement](../AUCTION_POSTING_TRANSACTION.md) qualifies the
last passing **11,354-case full source/private-SQLite gate**, zero failures/skips and
build warnings/errors. The public handler atomically escrows the exact whole Item
and listing, then completes original-actor reconciliation and guarded publication.
Held metadata/expiry defects have executed regressions and are corrected.193 focused,
1,106 affected and final reviews0/0 pass; 6,156 inputs/nine DLL pairs/test assembly
verify at a900f698. That gate also covers commodity posting; its earlier
failed full candidate and unresolved timing cause stay retained. Provider,
actual-table, socket/native and retail gates remain separate. No commit/push/client
launch. The Goal remains active.

[Commodity return settlement](../COMMODITY_RETURN_SETTLEMENT.md) qualifies11,264 full source/private-SQLite
cases, zero failures/skips and build warnings/errors; final reviews0/0.
Public buy/sell cancellation and real expiry atomically delete listings with full
held escrow refund or fungible direct/satchel/fixed-mail return, then complete
original-actor ordinary reconciliation and guarded publication. Accepted grants
survive custody conflicts in durable recovery mail; later material+1 rebases
against current80 as81. Wallet callbacks cannot mutate inventory, Items, codec
lists/rune children or satchel state. Legacy default sockets persist before
publication and remain available to crafting; semantic rune intent cannot be
retired as normalization. 824 affected cases pass. The6,145-input/nine-DLL
archive verifies at8c4f31890fe18e80f2f8f353e1e74a9c3656b2c0eaeefb23eadaf83ef3a3f8b7. Source/private SQLite is not configured-provider,
deployment, native/socket, actual-table or retail qualification.

[Auction cancellation settlement](../AUCTION_CANCELLATION_TRANSACTION.md) qualifies11,203
full source/private-SQLite cases, zero failures/skips and build warnings/errors;
review0/0. Public cancellation atomically returns the original whole Item and
refunds the held bid, with zero owner debit/proceeds, original-actor recovery,
direct/mail custody and guarded publication.545 affected cases pass. Historical auction boundary; see the canonical
ledger and result for limits. Earlier Challenge intermittent causes remain
unproven. No provider/native/retail completeness, commit or push is claimed.

[Currency conditional writes](../CURRENCY_CONDITIONAL_WRITES.md)
pass10,955 full/24 new race cases, retaining all14 frozen-currency,264 marketplace
and297 reviewed mail cases. Eight executed failures/sixteen controls, both
original-provider model diffs0 DDL/0 connections, five6,118-input archives/nine
DLL pairs and final reviews0/0 verify. The first full's66 test-helper failures
are retained; the loaded-original fixture correction preserves every oracle and
passes corrected focused/full gates. One later sharing failure/drain diagnostic,
four isolated passes and the unchanged-binary default full retry remain separate;
the cause is unproven. Loaded provenance, full financial/actor/
deferred recovery and provider/Contract/native/retail work remain. No push/client.

[Frozen currency preparation](../FROZEN_CURRENCY_PREPARATION.md)
passes10,931 full/14 focused tests, retaining all264 prior marketplace and297
reviewed mail cases.14 new cases, one executed review failure retained, four
6,117-input archives/nine DLL pairs and final reviews0/0 verify; full/test-copy
rebuilds have zero warnings/errors. Explicit token scope is tested; deferred
Abort now rejects/quarantines for both legacy/frozen callers instead of silently
releasing intent. Durable reconciliation is still unimplemented and the API is
unwired to financial handlers. Source/SQLite only; no push or client launch.

[Actor-queue predicate failure recovery](../ACTOR_QUEUE_PREDICATE_RECOVERY.md)
passes 10,917 full tests and all thirteen queue cases, retaining all 264 prior
marketplace and 297 reviewed mail cases. Eight new cases, six executed baseline
failures/seven passes, three 6,116-input archives/nine DLL pairs and both final
reviews verify; rebuild/test-copy builds have zero warnings/errors. Failed
predicates no longer poison the front, and throwing hooks retain earlier deferred
work while preserving propagation. Captured financial/actor/delivery/provider/
Contract/native/retail work remains. No push or client launch.

[Confirmed marketplace save outcomes](../MARKETPLACE_CONFIRMED_SAVE_OUTCOMES.md)
pass 10,909 full tests, all 264 marketplace and 297 prior reviewed mail cases,
with 13 new cases and zero failures/skips or rebuild/test-copy warnings/errors.
Four executed baseline failures/seven controls, a corrected eleven-case run and
both final reviews verify. Five 6,116-input archives/nine DLL pairs retain one
excluded stale-DLL prototype caused by preserved copy timestamps. Confirmed SQL
success survives later errors; unconfirmed outcomes remain explicit errors.
Complete captured financial/unknown-acknowledgement/actor/direct-delivery recovery,
provider/Contract/broader qualification remain. No push or client launch.

[Conditional marketplace listing writes](../MARKETPLACE_CONDITIONAL_LISTING_WRITES.md)
pass 10,896 full tests, all 251 marketplace and 297 prior reviewed mail cases,
with 36 new cases and zero failures/skips or rebuild/test-copy warnings/errors.
Twenty-eight executed competing-write failures/six controls are retained; both
nullable CSV directions pass. Four 6,114-input archives/nine DLL pairs and both
final reviews verify. Original-snapshot to new-runtime model diffs have zero
migration operations/connections for both providers; no physical migration is
needed. Live-MySQL predicate semantics, complete captured financial/ambiguity
and direct-delivery recovery, Contract and broader work remain. Fresh05:47UTC
metadata has55 open,608 newest open;609 closed unmerged,58 refs unchanged.
No push or client launch.

[Marketplace listing SQL postimages](../MARKETPLACE_LISTING_POSTIMAGES.md)
pass 10,860 full tests, including all 215 marketplace and 297 prior reviewed mail
cases; 24 new cases, zero failures/skips and zero rebuild/test-copy warnings or
errors. Six executed baseline failures/six controls and an executed corrected
late-writer regression are retained. Five archives bind 6,111 negative/6,112
later inputs and eight DLL pairs; both final reviews verify. Complete listing
rows/absence are checked before transaction commit, including shared mail
callbacks. CAS, captured financial/ambiguity and direct-delivery recovery,
Contract and broader qualification remain. No push or client launch.

[Revised upstream604 taxi reconciliation](../UPSTREAM_604_TAXI_SAVE_RECONCILIATION.md)
passes10836 full tests and34 focused cases in rebuilt full,16 net-new cases plus
one explicitly corrected shuttle45 oracle. Five executed baseline failures/18
controls,6109 negative/6110 final inputs, four archives/eight DLLs and final
two-axis review verify. Rebuild/test-copy builds have0 warnings/errors. Taxi
discoveries settle at terminal commit, retain rollback/later intent and support
key-only lost-ack retry; real Player.Save coupling is covered. The exact revised
604 head36b59aaaaebbcd0ec3e23a94fd3cf9de8975923b is now reconciled while retaining
fork injection/stronger guards.608 remains handled;609 closed unmerged. Broader
marketplace/Contract/provider/native/retail work remains; no push/client launch.

[Marketplace database-route binding](../MARKETPLACE_DATABASE_ROUTE_BINDING.md)
passes10820 full tests, including191 marketplace and297 prior reviewed mail cases
in rebuilt full,9 new cases and zero rebuild/test-copy warnings/errors. Two
executed cross-route failures/two controls, a corrected introduced orphan-ID
regression, five6108-input archives/eight DLLs and final two-axis review verify.
Equal rows in another database cannot confer ownership; normalized same-route
ordering, inserted remainders and startup ID marks remain covered. Full SQL
postimages, CAS and complete economic recovery remain.04:23UTC upstream604 alone
changed among58 refs; its exact old/new delta and later reconciliation are recorded above.
No provider/native/retail promotion, push or client launch.

[Marketplace loaded-row preimages](../MARKETPLACE_LOADED_ROW_PREIMAGES.md)
pass10811 full tests, including all182 marketplace and297 prior reviewed mail
cases in the rebuilt full run,28 new cases and zero rebuild/test-copy warnings
or errors. Twenty-four executed baseline failures/two controls,6107 final/6106
negative inputs, four archives/eight DLLs and final two-axis review verify.
All16 auction/10 commodity scalars and live-view consistency are checked;
legacy CSV, memory mode, repeated bids and reentrant rejection remain covered.
This is read/staging validation. SQL postimages, route ownership, CAS and complete
financial/outcome recovery remain, alongside Contract and broader work. No
provider/native/retail promotion, push or client launch.

[Marketplace required-row and expiry guards](../MARKETPLACE_STORED_ROW_GUARDS.md)
pass10783 full tests and154 rebuilt marketplace cases,34 new cases, with all297
prior reviewed mail cases retained in the full run. Rebuild/test-copy builds have
zero warnings/errors. Twenty distinct real executed failing cases, nine6105-input
archives/eight DLLs and final two-axis review verify; two earlier review findings
and one separate fixture failure remain preserved. Missing rows, empty incoming
resurrection and auction/commodity capacity-transition bypasses are guarded.
Full wallet/listing/Item settlement, rewritten-row witnesses, acknowledgement
recovery, Contract and broader work remain.03:30UTC upstream:55 open,608 newest
open;609 closed unmerged with unchanged head. No provider/native/retail promotion,
push or client launch.

[Committed marketplace mail publication](../MARKETPLACE_MAIL_PUBLICATION.md)
passes10749 full tests including297 reviewed mail cases on rebuilt DLLs,23 new
cases and zero rebuild warnings/errors. Six executed baseline failures and one
control,6104 final/6102 negative inputs, complete archives/eight DLLs and both
review axes verify. Post-commit lookup/notification errors preserve confirmed
SQL success and Item ownership; delivery waits for the original recipient actor
and save scope after SQL account/database routing checks. Marketplace economic
settlement and SaveBlocking ambiguity, Contract and broader work remain. No
configured provider/native/retail promotion, push or client launch.02:54UTC
upstream inventory remains56 open PRs,609 newest; all58 tracked refs unchanged.

Latest1 November [marketplace mail custody](../MARKETPLACE_MAIL_CUSTODY_COMPATIBILITY.md)
passes10726 full tests including274 reviewed mail cases on rebuilt DLLs,23 new
cases and zero rebuild warnings/errors.6101 final/6100 negative/6101 intermediate-
failure archives/eight DLLs and both review axes verify. The known legacy origin/
stored-page check preserves full witnesses and terminal quarantine. Marketplace
actor publication/economics, Contract and broader Goal continue. No provider/
native/retail promotion, push or client launch.

Latest1 November [available-mail expiry publication](../MAIL_EXPIRY_PUBLICATION.md)
passes10703 full tests including251 reviewed mail cases on rebuilt DLLs,18 new
cases and zero rebuild warnings/errors.6099 final/6096 negative/6099 failed-
fixture archives/eight DLLs and both review axes verify. One fixture sorting error
and the earlier manual-gate cleanup failure remain explicit. Marketplace custody/
publication/economics, Contract and broader Goal continue. No provider/native/
retail promotion, push or client launch.02:04UTC upstream heads remain unchanged;
609 newest and merged-since-assessment query returns the already reconciled598.

Latest1 November [manual return/delete publication](../MANUAL_MAIL_ACTION_PUBLICATION.md)
passes10685 full/233 rebuilt mail tests,27 new cases and zero rebuild warnings/
errors.6095 final/6092 negative inputs, archives/eight DLLs and both review axes
verify. One earlier full-run Challenge cleanup sharing failure is retained with
unproven cause; three isolated passes and the same-binary default full retry pass
are separate observations. Expiry, marketplace, Contract and broader Goal remain.
No fresh provider/native/retail qualification, push or client launch is claimed.

Latest1 November [bulk mail selection](../MAIL_SELECTION_TRANSACTION.md)
passes10658 full/206 focused mail tests,22 new cases and zero rebuild warnings/
errors.6091 final/6087 negative inputs, archives/eight DLLs and both review axes
verify. Manual return/delete publication and broader features continue. Later
upstream inventory remains56 open PRs,609 newest;608/609 and official branch
heads remain pinned. Source tests do not qualify provider/native/retail behavior.

Latest1 November [individual attachment collection](../MAIL_ATTACHMENT_TAKE_TRANSACTION.md)
passes10636 full tests/184 focused mail cases,16 new cases and a zero-warning/
error rebuild. All6086 inputs/archive/eight DLLs and three distinct source epochs
verify; an invalid fixture and a false-positive SQL control remain explicit.
The later bulk source gate above has its own binding. The00:19:43UTC refresh has56 unchanged open
upstream heads. No wire/index, provider/native/retail completeness or push/launch
is claimed; the Goal remains active.

Latest1 November [outgoing-mail source gate](../OUTGOING_MAIL_RETENTION_AND_PUBLICATION.md)
passes10620 full tests and49 focused outgoing/delivery cases,34 new cases, zero
failures/skips and zero full-build warnings/errors. The qualified snapshot's6081 inputs,
complete archives/eight DLLs and eight executed negative epochs verify. Earlier
checkpoints below retain their original proofs. [Attachment index evidence](../MAIL_ATTACHMENT_INDEX_EVIDENCE.md)
is mapped-only; receive/update and independent Verified corroboration remain open.
The23:20:27UTC upstream refresh still has56 unchanged open heads. The Goal stays
active; no fresh provider/native/retail completeness or push/launch is claimed.

Latest ordinary Item gate:10586 full/64 focused cases pass,21 new cases, zero
failures/skips and zero full-rebuild warnings/errors. Four executed failures/
three controls and6069/6067/6068 complete bindings verify. Shared-context changes,
stored no-ops and later intent are preserved; no cross-process concurrency token
or fresh provider/native/retail qualification is claimed. Remaining outgoing
mail/action/publication and broader feature requirements continue.

Latest31 October source gate: gift/COD receipt, wallet and sender settlement
recovery passes10565 full tests, zero failures/skips, a zero-warning/error
rebuild and23 reviewed cash-focused cases. The full run covers347 mail/bank cases;
three executed negatives/two controls and6066/6061-input archives/DLLs verify.
The21:25UTC upstream refresh retains56 unchanged open heads. NuGet's47-project
vulnerable/include-transitive query reports no vulnerable package entries.
Ordinary non-mail Item recovery and remaining action/publication work stay open.
The mapped reputation proposal awaits the continuation guide's Verified
corroboration;22 proposed cases are outside Source and never built/executed.

Latest31 October source checkpoint: attachment/header/Item aggregate recovery
passes10542 full tests under default concurrency, zero failures/skips, with a
zero-warning/error solution rebuild and324 focused mail/bank cases. Exact6060
inputs/DLLs/archive verify in mail-attachments-diagnostic. Earlier intermittent
Challenge timeout/teardown failures remain retained without a root-cause claim
or deadline change. The next gift/COD receipt delta has23 focused passes after
three baseline failures/two controls; its full gate is pending. Upstream608's
remaining refusals and609's source reconciliation are implemented at their
separate frozen gates;56 upstream open heads were unchanged through20:10UTC.
No fresh MySQL/provider/upgrade/load/native or retail completeness is claimed.

Latest legacy mail header/deletion checkpoint (31 October): [outcome recovery](../MAIL_SEND_TRANSACTION_AND_INBOX_RECOVERY.md)
passes **10,520 full tests**, zero failures/skips and zero full-build warnings/
errors. All10499 earlier cases remain;21 new cases and302 focused mail/bank cases
pass. Seven executed pre-change failures and one control are frozen separately.
All6,056 current inputs and their full archive match; the6,054-input negative
archive and eight DLLs also verify. Complete17-scalar header witnesses retain
intent through disposal, rollback and lost acknowledgement, reject conflicting/
stale owners and preserve later mutations. Delete/expiry tombstones leave only
after confirmed SQL absence. The player-send companion adopts its confirmed
header/database into this owner. Immediate legacy packets, attachment row/
deleted-attachment intent, outgoing publication and economic outcomes remain
separate work. No fresh MySQL/date conversion, copied-upgrade, socket/deployment,
native runtime or retail qualification. The Goal stays active; defaults15:1/17:1,
no automatic44/seven grants, no push and the native/Computer Use stop remain.

Prior player-mail creation checkpoint (31 October): [atomic player sends and inbox recovery](../MAIL_SEND_TRANSACTION_AND_INBOX_RECOVERY.md)
passes **10,421 full tests**, no failures/skips, including223 bank cases. The full
solution rebuild has zero warnings/errors. This adds31 tests:29 bank/mail cases,
one world-second mail timer regression and one concurrent event-queue regression.
Eight executed pre-change failures and exact binaries are frozen. All6,032
source/tool/binary inputs bound before the full run match afterward; all10,390
prior test cases remain. Postage plus gift, detached attachments, mail rows and
bank withdrawal share one retained transaction; actor delivery/polling covers
late commits, dead senders and delayed dirty attachments. Legacy creature/return/
COD collection/attachment taking and other mail outcomes remain separate work.
No fresh MySQL, upgrade, deployment, socket/native or retail qualification is
claimed. Rebuilt DB DLL identities changed despite unchanged DB source, so old
provider/upgrade binary bindings do not qualify these DLLs.

Prior upstream609 source checkpoint (31 October): [communicator scheduling and reports](../UPSTREAM_609_COMMUNICATOR_RECONCILIATION.md)
implements the pinned `f9b6f7356798a41beea0768ff1f6b5a758ffeca3` interpretation
while preserving injected services, scripted packet behavior and existing quest/
Contract save ownership. The refreshed inventory has56 open PRs; all55 earlier
heads, including608, remain unchanged. The162-case communicator/cleanup/live-
Challenge gate passes and the full solution rebuild has zero warnings/errors.
The full suite passes **10,499 tests**, zero failures/skips:71 communicator
cases and seven cleanup cases added since10421. All earlier cases remain;6,053
source/tool/binary inputs and their frozen archive match. Two earlier10492-case
full runs each reported one
Windows sharing violation in existing Challenge fixture cleanup; both are retained.
A bounded two-second cleanup retry preserves gameplay deadlines and propagates
persistent locks/other errors; seven in-memory cases include two executed negatives.
This does not identify the original lock holder or qualify native predicates,
actual full tables, MySQL/copied upgrades, deployment, sockets or retail behavior.
Public07CE, reset/unclaimed/excess edges, legacy mail outcomes, track35 and broader
work remain. The Goal is active. Defaults15:1/17:1/no44/no seven automatic grants
remain; Computer Use/native launch stay stopped. No staging, commit or push.

Prior bank vendor companion (31 October): [bank/payout ownership](../REALM_BANK_CODECS_AND_LOADING.md)
now captures full/partial sales and currency in one retained transaction, with
committed actor publication of payout and existing buyback effects. The full
solution rebuild passes with zero warnings/errors; **10,390 full tests** pass,
no skips, including194 bank cases. This adds21 vendor regressions, including the
final manager-owner guard. Six executed early-payout negatives are frozen with
exact binaries/source hashes. Intermediate bank gates179/190/193 remain separate.
That vendor gate predates the player-mail boundary above. No fresh MySQL,
native, deployment or retail qualification is claimed. The Goal remains active.

Prior local continuation (31 October): [captured bank recovery and actor publication](../REALM_BANK_CODECS_AND_LOADING.md)
passes **10,369 full tests** and173 focused bank cases, no skips, with zero full-build
warnings/errors under SDK10.0.401. This adds51 recovery/publication cases. The
earlier full10,367 gate predates the last two actor-finalization regressions and
is retained separately. Six original publication negatives and two final-callback
negatives were executed. Frozen commands retain exact SQL/revision witnesses across fresh-context replay; ordered
packets and equipment effects wait for the original live actor after commit.
Preparation rollback, stale/dead actors, reentrancy, expiry batching, deferred
open/resync and later dirty fields are covered. Mail/vendor bank ownership
handoffs remain a concrete source gap. Controlled SQLite replay does not qualify
actual configured MySQL retry/load behavior, deployment, native UI or retail.
The earlier [upstream608 refusal reconciliation](../UPSTREAM_608_CAST_REFUSALS.md) and
complete55-open-PR refresh remain: only608 is new,598 merged, other open heads
match the snapshot. The continuation verifier passed before edits. Public07CE,
retail reset/unclaimed/excess edges, actual full-table delivery, track35 and broader
requirements remain. Defaults15:1/17:1/no44 and no automatic seven-character grant
remain. Computer Use/native launch stay stopped. Local implementation changes
remain unstaged/uncommitted; no push was made. The Goal remains active.

The later handoff-only fast-forward to `a50dfa13` preserves the entire committed
Source tree and local implementation delta. At adoption, all6,013 tested inputs
matched without rebinding the10,369-test proof. The later vendor source delta has
its own6,026-input/10,390-test binding. Ten evidence-store tests pass;2,154 lean
archive members verify and restore to `.continuation-evidence/`. Historical
harnesses/focused exports and the exact Contract Lua are now local. Redacted
members remain derivatives, and restoration is not fresh MySQL/native evidence.

Earlier continuation: [Realm bank persistence and loading](I:/GIT/NexusForever-assessment/Decomp/Analysis/REALM_BANK_CODECS_AND_LOADING.md)
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

Earlier checkpoint10190: [Contract account delivery and session synchronization](I:/GIT/NexusForever-assessment/Decomp/Analysis/CONTRACT_ACCOUNT_SESSION_SYNC.md)
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

Earlier checkpoint10165: [Authoritative account wallet refresh](I:/GIT/NexusForever-assessment/Decomp/Analysis/ACCOUNT_WALLET_AUTHORITATIVE_REFRESH.md)
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

Earlier checkpoint10126: [Contract claims through the existing session actor queue](I:/GIT/NexusForever-assessment/Decomp/Analysis/CONTRACT_CLAIM_ACTOR_QUEUE.md)
qualifies checkpoint10126: **10,126 full tests**,31 new/178 focused,1,565 MySQL
cases (including363 configured-strategy repeats), four Auth race probes, four
model gates and31 compatibility probes; zero full-build warnings/errors. Internal
claim admission now holds Quest/queue/inventory/currency ownership through complete
publication, preserves later ordinary intent and releases terminal dead-owner
bookkeeping without packets. All three choices on both tracks are covered. The
WorldServer07CE route remains disabled while account draining/relog, authoritative
wallet merging and retail reset are connected. Both DB binaries/upgrade bindings
are unchanged; all726 actual quote choices pass. Actual delivery/copied-upgrade/
socket/deployment/native checks, track35 and the entire broader Goal remain.
Fresh archive anchors prove configured Contracts/English files match the client;
compressed-entry hashes explain the apparent SHA-1 difference. Retail reset edges
remain under investigation; no user policy preference is pending. The8378 runtime,
nine identities, six drained queues, original data/config/schema and defaults
15:1/17:1/no44 stay preserved. No staging/commit/push or native/Computer Use work.
Q5597 remains unavailable and is not requested again. The full Goal stays active.

Earlier checkpoint10095: [Contract credit ownership and actor publication](I:/GIT/NexusForever-assessment/Decomp/Analysis/CONTRACT_CREDIT_ACTOR_RESERVATION.md)
qualifies checkpoint10095: **10,095 full tests**,27 new/103 focused,1,503 MySQL
cases (including332 configured-strategy repeats), four Auth race probes, four
model gates and31 compatibility probes; zero full-build warnings/errors. A clean
live credit reservation adopts only an exact privately proved executor result,
publishes once, and retains later semantic currency intent through capped and
reentrant publication. Both DB DLLs and prior upgrade bindings remain unchanged;
all726 actual quote choices pass. Registered public07CE queue integration,
complete failure/uncertainty/relog/dead-owner settlement, account draining/online
wallet merging, actual delivery/copied-upgrade/socket/deployment/native checks
and the entire broader Goal remain. The user now directs retail reset semantics;
final-reward collection is the evidence-backed working inference, with unclaimed/
excess edges still under investigation. No policy preference remains pending.
The8378 runtime, nine identities, six drained queues, original data/config/schema
and defaults15:1/17:1/no44 remain preserved. No deployment/commit/push; Computer Use
stays stopped. Q5597 remains unavailable and is not requested again. Goal active.

Earlier checkpoint10068: [Character currency persistence and frozen normal saves](I:/GIT/NexusForever-assessment/Decomp/Analysis/CHARACTER_CURRENCY_DURABLE_SAVE.md)
qualifies checkpoint10068: **10,068 full tests**,26 new/304 focused,1,449 MySQL
cases (including305 configured-strategy repeats), four Auth race probes, two
old/current currency defect probes, four model gates and31 compatibility probes;
zero full-build warnings/errors. Currency dirty intent survives rollback/unknown
acknowledgement; stale SQL baselines fail closed. Frozen Player saves retain later
currency intent and use the existing atomic Character unit. Both database DLLs
are unchanged, binding prior60/61/62-table Character and30-table Auth upgrades;
all726 actual quote choices still pass. Public07CE claims, claim-specific currency
reservation/adoption and actor publication, account draining/online wallet merging,
reset/carry, actual item delivery/copied-upgrade/socket/deployment/native checks
and the entire broader Goal remain. The8378 backend, nine identities, six drained
queues, original data/config/schema and defaults15:1/17:1/no44 stay preserved.
Source/migrations remain undeployed. Computer Use stays stopped; Q5597 remains
unavailable and is not requested again. The full Goal remains active.

Earlier checkpoint10042: [Contract claim transactions and durable account handoff](I:/GIT/NexusForever-assessment/Decomp/Analysis/CONTRACT_CLAIM_TRANSACTION_AND_ACCOUNT_HANDOFF.md)
qualifies checkpoint10042: **10,042 full tests**,47 new/1,579 focused,1,402 MySQL
cases (including284 configured-strategy repeats), four independent Auth race probes,
four model gates and31 compatibility probes; zero build warnings/errors. Atomic
claims retain quote/account/cycle ownership, item/credit SQL witnesses and claimed
presentation. Durable original-account intents and global Auth operation receipts
recover either database's acknowledgement without regrant, including source
transfer/deletion. Populated60/61/62-table upgrades retain68/69/70 logical rows;
all726 actual-table choices still quote correctly. Public07CE claims, normal-save/
actor currency ownership and publication, account draining, reset/carry, real-item
delivery, copied-upgrade/socket/deployment/native qualification and the entire
broader Goal remain. The8378 backend, nine identities, six drained queues, original
data/config/schema and defaults15:1/17:1/no44 stay preserved. Source/migrations are
not staged. Computer Use stays stopped; Q5597 remains unavailable and is not
requested again. The full Goal remains active.

Earlier checkpoint9995: Account wallet operations and durable save
settlement (`Decomp/Analysis/ACCOUNT_CURRENCY_OPERATION_PERSISTENCE.md`) passes
**9,995 full tests**,44 new/101 focused,1,327 MySQL cases, four independent MySQL
race/retry probes, four model gates and31 compatibility probes, zero build warnings/
errors. Frozen before/after probes prove cached-balance credit loss and rollback
intent loss are fixed. Typed retained operations, current locking reads and exact
SQL witnesses preserve credits and prevent regrant across retries. Existing Loyalty
ownership/defaults stay unchanged. An additive Auth journal upgrade preserves all
30 old tables/145 logical rows; the Character DLL and9951 upgrade proofs remain.
All726 actual Contract quote choices still pass. Public Contract aggregate/adoption/
currency/outbox/reset/actor publication, real delivery/copied-upgrade/socket/
deployment/native verification and the full broader assessment/upstream/AMP/combat/
content Goal remain. The8378 runtime,9 identities,6 drained queues, original data,
config/schema and15:1/17:1/no44 defaults stay preserved. No staging/commit/push.
Computer Use stays stopped; unavailable Q5597 evidence is not requested again.
The full Goal remains active.

Earlier checkpoint9951: Contract receipt and inventory enrollment
(`Decomp/Analysis/CONTRACT_REWARD_INVENTORY_ENROLLMENT.md`) passes **9,951 full
tests**,49 new/192 focused,1,253 MySQL runs, two model gates and31 compatibility
probes, with zero build warnings/errors. Dedicated Contract receipts enroll item
stacks without Challenge history. Shared final SQL witnesses reject missing
receipts/claimed bits/Item writes; recovery retains account/quote/source/time/IDs.
Both populated legacy and already-selected MySQL upgrade paths preserve all prior
row projections and nonzero option/decor values. All726 actual-table award quotes
still pass. Public claims, complete aggregate actor/repository/executor adoption,
Character wallet effects, durable Auth handoff, reset/carry and real delivery/
copy-upgrade/socket/deployment/native qualification remain. The fail-closed claim
adoption guard must be replaced by that complete owner before claims are enabled.
The8378 backend,9 service identities,6 drained queues, originals/config/schema and
defaults15:1/17:1/no44 remain preserved. No staging/commit/push or entitlement-default
change. Computer Use stays stopped; Q5597 is unavailable and is not requested again.
The full assessment/upstream/AMP/combat/content Goal remains active.

Earlier checkpoint9902: Contract award planning and selected-cycle quotes
(`Decomp/Analysis/CONTRACT_REWARD_AWARD_QUOTES.md`) passes **9,902 full tests**,
780 new/focused cases and an independent actual-table audit of all726 choices:
486 Item2,240 Auth currency and six final Character credit awards. The full build
has zero warnings/errors. Immutable quotes retain selected-cycle/revision/points
and complete payout/provenance; mixed/unknown effects, quantity overflow and altered
quotes fail closed. This implements payout planning, not claim authorization or
delivery. Exactly three Source files were added; all5833 previous files remain
unchanged. Character/Auth/abstract binaries, schema, entitlement defaults and the
undeployed-source boundary are preserved. Nine8378 service identities, six drained
queues and original/config/schema/default evidence remain unchanged. The1172 MySQL,
two model and31 compatibility checks below remain historical9122 qualification,
not new provider runs. Connected claims/reset/carry/Auth handoff, actual delivery,
socket/deployment/native checks and all broader Goal work remain. Computer Use stays
stopped; Q5597 evidence remains unavailable and is not requested again. No commit
or push was made. The full Goal is active.

Earlier checkpoint9122: Contract track choice through the registered actor queue
(`Decomp/Analysis/CONTRACT_REWARD_TRACK_CHOICES.md`) passes **9,122 full tests**,
32 new/674 focused,1,172 MySQL cases (1,124 prior +24 choice +24 retry), two model
gates and31 compatibility probes, with zero build warnings/errors. Actual07CF routes
Contract0/1 to the real Quest owner/queue with published-snapshot, exact-baseline,
busy/foreign/eligibility guards. Active state and optional legacy selections commit
together; operation provenance invents no acceptance/history. Other types, ordinary
prizes and dirty Quest/Challenge intent survive; lost-ack recovery cannot reroll.
Database binaries/models/migrations are unchanged, binding the previous populated
upgrade/legacy repair evidence. Claims/reset-policy/carry/Auth handoff/full-table/
socket/deployment/native and the broader Goal remain. No source staging, commit or
push: the8378 backend,9 identities, original data/config/schema/defaults and6 drained
queues remain preserved. Computer Use stays stopped; all167,484 content rows remain
not_retail_complete. The Q5597 bundle is unavailable and is not requested again.
The complete assessment Goal remains active.

Earlier checkpoint8679: atomic Contract/ordinary Character save enrollment
(`Decomp/Analysis/CONTRACT_NORMAL_SAVE_TRANSACTION.md`) passes **8,679 full tests**,
116 new/focused,853 MySQL cases (680 prior +116 new +57 configured-retry cases),
two model gates and31 compatibility probes, with zero build warnings/errors.
The full enrolled Character graph, receipt/points and exact Quest witnesses commit
in one serializable unit. Real frozen Quest and ordinary Currency save enrollment,
partial stubs, later Quest intent, pre-seal ownership, suppressed writes, rollback,
context reuse and lost acknowledgement are covered. No model/migration changes or
entitlement-default changes are introduced. The live Player completion journal/
FIFO before Auth, complete ordinary reward ownership, Auth handoff and subsequent
selection/claim/reset/publication/socket work remain required. No live earning is
enabled. The8378 runtime and all9 identities, originals/config/schema/defaults and
6 drained queues remain preserved. Computer Use stays stopped; all167,484 content
rows remain not_retail_complete. The June19 Q5597 UI bundle is unavailable, with
its documented recovery review and pending native UI gate retained. The full Goal
remains active. No commit/push was made.

Earlier checkpoint8563: Contract modifier ownership
(`Decomp/Analysis/CONTRACT_REWARD_MODIFIERS.md`) passes **8,563 tests**,89 new/
225 focused,680 MySQL cases, two model gates and31 compatibility probes. All12
actual Contract effect lifetimes, whole-scalar packets and frozen evaluated inputs
are verified across51 Contract sources. Actual table projection verifies48 exact
evaluations; UI/text/source inventories are hashed. Signature/entitlement44 remain
Challenge-only; unsupported modifiers/fractional policy are not invented. The real
completion actor/ordinary reward composite unit and selection/claim/reset/Auth
handoff/Zone work remain required. Current code is not staged: the8378 backend,
all9 identities, originals/config/schema/defaults and6 drained queues are preserved.
Computer Use stays stopped, all content remains not_retail_complete and the full
Goal remains active. No commit/push was made.

Earlier checkpoint8474: Contract completion receipt and point transaction
(`Decomp/Analysis/CONTRACT_COMPLETION_TRANSACTION.md`) passes **8,474 tests**,
96 new/148 focused,629 exact-source MySQL cases, two model gates and31 compatibility
probes. The persistence core atomically stages a generation-keyed receipt, evaluated
point increment and exact terminal Quest graphs, with strict ownership and outcome
recovery. All51 supported source descriptions and prior Challenge/Quest cases pass.
A populated upgrade/replay preserves59 tables/22,956 old rows and the one accepted
identity, leaving two new tables empty. Live ordinary reward/actor composite
ownership and verified Contract modifiers remain required, followed by selection,
claim/reset/account handoff and Zone work. Current completion code/migrations are
not staged; all9 existing8378 services, original data/configs/schema/defaults and
6 drained queues remain preserved. Computer Use stays stopped, all content remains
not_retail_complete, and the full Goal remains active. No commit/push was made.

Earlier checkpoint8378: Contract acceptance identity and persistence
(`Decomp/Analysis/CONTRACT_ACCEPTANCE_PERSISTENCE.md`) passes **8,378 tests**,
45 new since8333,536 MySQL persistence cases, two model gates and31 compatibility
probes. Stable actual-acceptance generations preserve normal/shared Quest saves,
retries and historicalNULLs. Both additive migrations are qualified; the owned
MySQL upgrade preserves59 tables/22,953 old rows and175 historicalNULL identities.
Contract9880 accept/relog/resume passes four real socket sessions with one stable
identity and unchanged fixture rewards. Frozen World1880/API.Character33508 now
run;9 services,7 unchanged identities and6 drained queues pass. Originals/configs/
defaults15:1/17:1/no44 remain preserved. Durable Contract completion receipts,
point credit/selections/claims/reset/account handoff and Zone progression remain
required. Computer Use stays stopped; all content remains not_retail_complete and
the full Goal remains active. No commit/push was made.

Earlier checkpoint8333 (historical runtime state): Contract point sources and static reward catalogs
(`Decomp/Analysis/CONTRACT_REWARD_TRACK_IMPLEMENTATION.md`) passes **8,333 tests**,
350 new/603 focused and31 compatibility cases with zero build warnings/errors.
Exact native/cache/PE evidence identifies group-to-track ownership and formula1193;
ten actual tables verify51 source descriptions/four blocked rows and complete
242-row/726-choice catalogs, including prerequisites, final credits and AccountItem
compensation. Durable Contract completion/credit/claim/reset/account handoff and
Zone progression remain required; this static implementation does not enable them.
The separately qualified7983 backend remains running. Originals/defaults/configs/
schema are preserved; Computer Use stays stopped, all content remains
not_retail_complete and the full Goal remains active.

Earlier continuation: Character Challenge socket earning and claims (`Decomp/Analysis/CHARACTER_CHALLENGE_SOCKET.md`)
verifies actual Challenge103/track16:15 credited combat kills,3 completions,
300 earned points,3 receipts/items across relog and cycle1/zero remaining points.
Offline fixture placement is explicit; walking/native UI remain separate gates.
The real terminal operation exposed and now verifies a MySQL retry-strategy fix
in Quest preparation. The harness waits for a new choose reload before claiming.
Full **7,983 tests**, zero build warnings/errors,78 final focused,515 provider cases,
2 model gates and31 unchanged compatibility checks pass. Final driver changes
retain byte-identical provider helpers/backend assemblies.16 corrected lifecycle
cycles,16 initial/10 live reward reloads,9 healthy services/7 unchanged identities,
6 drained queues, closed latest epochs and preserved originals/configs/schema/
15:1/17:1/no44 are verified. The retained failed observations are explained and
the final claim run consumes existing earned points without reearning or SQL
repair. Native UI and unknown scoring/producer/share/achievement/RNG semantics
remain open. Computer Use stays stopped; F-033 stays partial, all content stays
not_retail_complete and the full assessment Goal remains active.

Earlier continuation: Live Character Challenge producers (`Decomp/Analysis/CHARACTER_CHALLENGE_LIVE_PRODUCERS.md`) wires mapped count-based activation, medals,
completion and non-completing termination for all16 supported Character catalogs,
with frozen attempt modes/modifiers and immediate captured Quest effects. Legacy
direct prizes, NULL history, immediate restarts and later actor intent are preserved.
Full **7,959 tests**, zero build warnings/errors,293 focused,512 exact-source provider
cases,2 model gates and31 unchanged compatibility checks pass. Registered choose/
claim/reset consumes actual actor-earned state. Frozen World/Character API passes
215 login/world/cleanup cycles/reloads;9 services,7 unchanged identities,
6 naturally drained queues, originals/configurations and15:1/17:1/no44 are preserved.
Socket earning/claim/relog/reset, native UI, unvalidated source semantics and all
other assessment work remain open. Computer Use stays stopped; F-033 stays partial
and all content stays not_retail_complete.

Earlier continuation: Character reward world readiness (`Decomp/Analysis/CHARACTER_REWARD_WORLD_READINESS.md`) passes **7,903 tests**, zero build warnings/errors,
38 focused cases, 456 exact-source provider cases, 2 model gates and 31 unchanged
compatibility probes. Real Character selection loads durable rewards before map
admission and waits for queue ownership to end; captured actor/session/account
bindings prevent stale entry. Bootstrap retains one composed reload. Frozen
World/Character API passes 212 complete cycles/reloads with 9 services,
7 unchanged identities and 6 naturally drained queues. Originals, private
configurations and defaults 15:1/17:1/no44 are preserved. Actual earning producers,
earning/claim protocol proof, native UI and the full assessment remain open.
Computer Use stays stopped; F-033 stays partial and all content not_retail_complete.

Earlier 30 October continuation: Normal-save snapshots (`Decomp/Analysis/REWARD_NORMAL_SAVE_SNAPSHOTS.md`) passes **7,870 tests**,
zero build warnings/errors,312 focused cases,423 exact-source provider cases,
2 provider model gates and31 unchanged compatibility probes. Ready direct/deferred
Player saves freeze Quest/Challenge targets before Auth; immediate events retain
their own state/revisions and FIFO reward adoption follows the older flush.
Objective ordinal0 is explicit, loaded rows sort by ordinal, and existing schema
defaults/store types are preserved. Frozen World/Character API passes236
complete cycles/reloads with9 services/7 unchanged identities,6 drained queues,
originals/configurations and15:1/17:1 defaults/no44 preserved. Startup readiness,
live earning producers and real earning/claim/native UI gates remain open.
Computer Use stays stopped; F-033 stays partial and all content not_retail_complete.

Earlier 30 October continuation: Shared Quest/Challenge queue (`Decomp/Analysis/QUEST_CHALLENGE_REWARD_QUEUE.md`) passes **7,831 tests**,
zero build warnings/errors,158 focused cases,384 exact-source provider replays
and31 unchanged compatibility probes. Quest captures now join the Challenge
queue's one command, recovery and preflighted adoption; both owners survive
SQL/cancellation and preserve later intent. Frozen World passes221 complete
protocol cycles/reloads with9 services/8 unchanged identities,6 drained queues,
originals/configurations and15:1/17:1 defaults/no44 preserved. Live earning
producers and real earning/claim/native UI gates remain open. Computer Use stays
stopped; F-033 stays partial and31 CSVs/167,484 rows remain not_retail_complete.

Earlier 30 October continuation: Quest reward actor ownership (`Decomp/Analysis/QUEST_REWARD_ACTOR_OWNERSHIP.md`) passes **7,774 tests**,
zero build warnings/errors,152 focused cases,327 exact-source provider cases
and31 unchanged compatibility probes. Real Quest completion census, character
save ownership, FIFO baselines and exact result adoption preserve later dirty
intent, scripts and replacement/restored owners. Frozen World passes224
complete protocol cycles/reloads; all9 services,8 unchanged identities,6 drained
queues, originals/configurations and15:1/17:1 defaults/no44 are preserved.
Challenge queue/producer integration and live earning remain open; these login
checks do not exercise the new internal mutation API. Computer Use stays stopped,
F-033 stays partial and all31 CSVs/167,484 rows remain not_retail_complete.

Earlier 30 October continuation: Durable presence generations (`Decomp/Analysis/FRIENDSHIP_PRESENCE_GENERATIONS.md`) passes **7,728 tests**,
zero build warnings/errors,80 focused and361 exact-source replay cases across
generation/producer/upgrade and retained Character reward/Quest regressions.
31 unchanged compatibility probes pass. Durable Auth epochs, captured admission
ordinals and persistent Friendship phase fencing order received events without
wall-clock ordering. Additive native/SQLite upgrades preserve old data/defaults
without backfill. Frozen World/Friendship passes203 complete versioned cycles,
with final markers matching durable epochs, offline presence and drained queues.
Quest actor integration/live earning and the full assessment remain open;
Computer Use stays stopped and all content remains not_retail_complete.

Earlier 30 October continuation: Friendship presence transactions (`Decomp/Analysis/FRIENDSHIP_PRESENCE_TRANSACTIONS.md`) passes **7,666 tests**,
zero build warnings/errors,42 focused cases,31 unchanged compatibility probes
and18 exact-source MySQL cases. Both handlers now retain database ownership and
refresh stale tracking; state/outbox commit together and provider retries reset
rolled-back caches. A different-character/realm logout preserves the current
identity. Frozen Friendship passes221 complete runtime cycles and final presence
settles without repair/relog. Session-generation ordering remains open; Quest
actor integration/live earning and the full assessment scope remain active.
Defaults/originals are preserved; Computer Use stays stopped.

Earlier 30 October continuation: Exact Quest/reward transaction participants (`Decomp/Analysis/CHARACTER_CHALLENGE_QUEST_TRANSACTION.md`) passes **7,648 tests**,
zero build warnings/errors,400 focused cases,31 unchanged compatibility probes
and281 exact-source provider cases. Immutable complete Quest graphs now join
terminal reward SQL units with full baseline/commit/recovery guards. Live actor
participant ownership/adoption and earning remain open. Frozen World/Auth passes
242 complete login/reload cycles. Initial mixed load had one cleanup-time
reconnect rejection (134/135); the separately named repeat passes105/105.
All9 previously stopped owned services were
restored and audited with defaults/originals preserved. No real network claim or
native UI proof follows; Computer Use stays stopped and F-033 remains partial.
One stale Friendship presence was observed after cleanup and settled by an
ordinary controlled relog; its ordering fix remains open with saved evidence.

Earlier 30 October continuation: Quest actual-commit durability (`Decomp/Analysis/QUEST_PERSISTENCE_DURABILITY.md`)
passes **7,599 tests**, zero build warnings/errors,31 unchanged compatibility
probes and232 exact-source provider cases, including36 new Quest durability cases.
Quest/objective intent now survives rollback, savepoints, acknowledgement loss
and later mutations; pending deletions retire after commit on the actor, with
safe cascade restoration and provider-matched reset conversion.
Frozen World/Auth passes176 complete runtime cycles with defaults/originals
preserved. Challenge completion must still join the exact Quest participant
snapshots to its reward unit before live earning is enabled. Real network claims
and native UI remain open; Computer Use stays stopped.

Earlier 30 October continuation: Character track routing/bindings (`Decomp/Analysis/CHARACTER_CHALLENGE_REWARD_ROUTING.md`)
passes **7,563 tests**, zero build warnings/errors,31 unchanged compatibility
probes and196 exact-source provider cases. Registered packets dispatch by static
type2/3 ownership. Character choose/claim uses the exact last published snapshot,
frozen eligibility and transactional bound inventory across all16 catalogs.
Repeated cycles, stale requests and acknowledgement recovery are verified.
Frozen World/Auth passes209 complete runtime cycles with defaults/originals
preserved. Live earning remains open: Challenge completion must also preserve
quest-objective durability across reward commits, saves and termination.
Real network claim/native UI gates remain open; Computer Use stays stopped.

Earlier 30 October continuation: Character reward actor queue/reloads (`Decomp/Analysis/CHARACTER_CHALLENGE_REWARD_QUEUE.md`)
passes **7,517 tests**, zero build warnings/errors, 31 unchanged compatibility
probes and 153 exact-source provider cases (including 34 new queue cases).
The real Player configures an ordered character reward queue; SQL and provider
discovery run off the actor, session events own committed adoption/publication,
and final disposal retains terminal cleanup without stale notifications.
Initial reloads compose persisted character cycles with account rows; all16
catalog selections retain ordinal identity. Frozen World/Auth passes 209
complete runtime cycles with original rows, configurations and defaults preserved.
Live earning producers, type-3 choose/claim routing and native UI remain open.
Computer Use remains stopped; F-033 stays partial.

Earlier 30 October continuation: Challenge reward ownership/adoption (`Decomp/Analysis/CHARACTER_CHALLENGE_REWARD_OWNERSHIP.md`)
passes **7,483 tests**, zero build warnings/errors, 31 unchanged compatibility
probes and 119 exact-source provider cases (59 repository, 39 executor,
21 ownership; includes a pure constructor guard and a SQLite Auth save guard).
Challenge reward leases now coordinate Player saves/cleanup, resolve uncertain
baselines and adopt committed state while preserving later runtime intent.
The frozen World/Auth build passes 182 complete runtime cycles; all six broker
queues drained naturally, configurations/defaults and original rows are preserved.
The live actor queue, earning producers, composed presentation and claim routing
remain unwired. Computer Use remains stopped; F-033 stays partial.

Earlier30 October continuation: Character reward execution/recovery (`Decomp/Analysis/CHARACTER_CHALLENGE_REWARD_EXECUTION.md`)
passes7,462 tests with zero build warnings/errors,31 unchanged compatibility
probes and98 exact source MySQL cases (59 repository,39 executor; one pure
constructor guard). Immutable commands, fresh whole-unit provider execution,
retained-claim resolution, database pinning and single-use resumption proofs are
implemented. Actor queue/save ownership, baseline adoption, live earning and
routing remain open. Runtime stays frozen7,364; F-033 remains partial.

Earlier30 October continuation: Character reward repository units (`Decomp/Analysis/CHARACTER_CHALLENGE_REWARD_REPOSITORY.md`)
pass7,423 tests with zero build warnings/errors,31 unchanged compatibility probes
and59 exact source MySQL cases. Activation/medal/completion/selection/reload/claim
units cover all16 complete catalogs, with root-row serialization, durable receipts,
retained cycles and inventory-backed delivery. The actor queue/provider execution
strategy, Challenge baseline adoption, live earning and packet routing remain
unwired. Runtime stays frozen7,364; no new native UI claim. F-033 stays partial.

Earlier30 October continuation: inventory reward delivery (`Decomp/Analysis/CHARACTER_REWARD_INVENTORY_DELIVERY.md`)
passes7,364 tests with zero build warnings/errors,31 unchanged compatibility
probes and38 final MySQL inventory-owner cases. Whole-delivery reservation,
receipt/item enrollment, recovery, publication, expiration deferral and Player
save/economy preflight are implemented. Runtime-discovered Auth realm refresh
now preserves health for unchanged endpoints. Production Challenge repository,
eligibility/earning/claim routing and native UI remain open. F-033 stays partial.

Frozen World7364/PID48568 and Auth7364/PID48880 pass200/200 protocol/lifecycle cycles (1 cold Eastern,198 mixed across70 seconds,1 existing-account replay). Every admission has one bootstrap, ACK request/reply, assigned disconnect and completed cleanup; there are zero World errors, unfinished admissions or late/duplicate bootstraps. The mixed run crosses a complete60-second realm refresh with no Auth rejection. Seven other service identities and the forwarding helper are unchanged. Six queues drain naturally; the Group queue's initial25,010 ready/50 unacknowledged messages decline to7,581/50 and then0/0 without purge or replay. Private configurations are unchanged. Reused fixtures retain exact15/17 defaults, no44/unearned rewards, and all12 synthetic identities are offline/null-active. Original77 tables,43 copied legacy characters and13 old entitlement rows retain their checks; all five new Character reward tables remain empty and old attempt markers remain null. See `remaining-goal/runtime-checkpoint7364/runtime-final-state.json` and `remaining-goal/inventory-reward-delivery/final-availability/VERIFICATION.json`.

30 October source continuation: the inventory reservation preparation layer passes
7,254/7,254 tests, a full zero-warning solution build,31 unchanged compatibility
probes and11 new-schema MySQL save-validation cases. The two allocator regressions
fail against frozen7182 and pass now. Atomic receipt/item enrollment, recovery
and publication remain open. World7182 replay and prior migration proofs retain
their original scope. See the tracked
`Decomp/Analysis/CHARACTER_REWARD_INVENTORY_RESERVATIONS.md` and external
`remaining-goal/inventory-reservation-foundation/VERIFICATION.json`.

The original comparison below is preserved as baseline evidence. It no longer
describes the current fork's unfixed defects or verification totals.

- Protocol/network, dirty jobs/entitlements, loot/group, travel/archive,
  taxi/mount, passive/housing and movement/platform slices are committed through
  `d2396d6e`. Solo-reset/mounted-visibility and subsequent goal changes are local
  and uncommitted. Existing entitlement defaults remain unchanged.
- Packet scopes now have a tested ownership correction: root-owned stateless
  EntityFactory keeps long-lived entities/deferred resolution outside packet
  disposal. The old registration failure is reproduced as a negative control.
- The audited dependency advisories are corrected, including formerly suppressed
  SharpCompress findings. The new archive adapter successfully extracted 384
  client tables and generated the world 3460 map from the installed client.
- Native build 16042/Ghidra/REA evidence confirms corrected prerequisite
  comparison IDs and active-duel type 204. Additional native-verified packet
  fixes cover resource conversion, trade-partner lists/timer and vehicle embark.
  Basic proficiency/gender and three-bit dash checks also pass. Native evidence
  now resolves percentage types 71/215; their handlers pass the full gate.
  Type16 world-location distance now has a mapped signed threshold conversion,
  same-world/full-XYZ handler,30 regressions and independent review. Its old
  type41 native label is corrected and re-exported without changing type41.
  Live type95 squared-distance and type73 raw-vital conversion are also
  corrected with34+27 regressions and exact native/table evidence.
- The earlier Challenge-foundation gate passed with **zero build warnings/errors** and
  **7,182/7,182 tests**, none skipped. All **31 original fork compatibility
  probes pass** against frozen7,182 references. Character item persistence adds
  actual-commit/SQL-flush snapshots, retry-safe tombstones/buyback, stale-owner
  checks and coordinated bank baselines;45 item/40 context/21 bank cases pass.
  The retained7,019 `MySqlCharacterItemProof_29g3` passes45 cases, including synthetic
  lost acknowledgement and a real InnoDB deadlock whose victim is the item
  writer. Source/copy item rows are preserved;169 DLLs/14 item-slice sources and
  the final full-suite TRX are hash-bound. The earlier29g1 provider failure is
  retained and its unstable credential-bearing lease key is corrected.
  See `remaining-goal/MYSQL_CHARACTER_ITEM_PROOF.md` and tracked
  `Decomp/Analysis/CHARACTER_ITEM_DURABILITY.md`. That earlier runtime gate was
  frozen7,182/WorldPID31676 with26 complete protocol/lifecycle cycles. The owned
  Character migration54→59 preserves old fields and produces no backfill;
  9 services remain active,8 other identities remain and6 queues drain. A fresh
  54-table/22,530-row MySQL schema/receipt proof passes9 cases with169 DLLs/190
  relevant sources. All16 Challenge catalogs, durable Challenge state, modifier
  snapshots and composed reload foundations pass; live character earning,
  repository and transactional delivery still require implementation. Native UI
  scenarios remain unperformed.
  That checkpoint includes AMP/satchel/read-model/harness, cooldown, community,
  assets, unit contexts, verified Hycrest intro/vote, native 07FD acceptance,
  real-table AMP extension, corrected reward-track/rotation routing and durable
  pending-item transactions/merge/monotonic IDs, complete account Loyalty,
  durable source-item consumption, map-admission ownership and earned-entitlement
  commit acknowledgement, native type16/95/73,78 per-pet dismiss cases,
  40 Challenge-property cases,21 medal-math cases and5 seed-default cases. At that earlier checkpoint, runtime ran7182;
  its26-cycle protocol/lifecycle proof is distinct from native UI validation. The
  community failure/rejoin retry variant is fixed and covered by the full gate;
  5,766 below is the historical starting count.
- Isolated populated MySQL copies migrate successfully and a second run is
  idempotent. A direct server-side copy proof preserves exact CHECKSUM TABLE
  results for all 51 existing data tables, including binary character FLOAT
  values. Original runtime databases remain unchanged. The runtime report
  distinguishes this proof from earlier mysqldump-based runtime copies.
- An owned local RabbitMQ node and isolated runtime completed login/world smoke
  and **36/36 bounded sessions with six fresh identities**. The live-discovered
  Query/Friendship first-create races and retry-strategy transaction bug are
  corrected with relational regression coverage; fresh-user traffic reports no
  recurrence. Four historical owned deadletters were snapshotted and replayed
  unchanged, leaving service/error queues drained normally. Native client UI
  startup now passes through the existing connector with the correct working
  directory; the native login screen and local authentication endpoint are
  verified. The user reached character selection; entering character 30 failed
  because the smoke runtime lacked Eastern.nfmap. The complete verified asset
  root fixed it, and the user confirmed that the I-drive client rendered the
  world and responded normally. Checkpoint6511 separately passed28/28 fresh
  mixed Eastern/NPE sessions. The retained frozen6541 passed9/9 more sessions with
  drained queues and completed cleanup. Its additive auth migration preserves
  102 tables / 22,322 old rows; all77 original auth/character tables remain
  unchanged after the gate. See RUNTIME_CHECKPOINT6541.md for exact boundaries.
  Subsequent log review exposed an early reply before map admission and a stale
  queued add after cleanup. Both server admission and harness ordering are now
  corrected. Frozen6717 passes26/26 sessions with26 matching actual admission,
  assigned-disconnect and cleanup cycles, zero late adds/errors and all6 test
  accounts offline/null-active. Original77 tables,43 copied legacy characters
  and13 existing entitlement rows remain unchanged. See RUNTIME_CHECKPOINT6717.md.
  The retained frozen6923/PID39024 passed23/23 sessions with23 matching full lifecycle
  cycles, zero late adds/errors,12 offline/null-active synthetic accounts and
  six drained queues. The live-discovered seed fixture defect is corrected only
  for new accounts: fresh9601–9603 retain exact15/17 defaults before/after login,
  while rerunning seed preserves an older account's exact70 rows across10 tables.
  The earlier6918 transport pass and failed default audit remain separate
  evidence. Original77 tables,43 copied legacy characters and13 old entitlement
  rows are unchanged. See RUNTIME_CHECKPOINT6923.md.
- Evidence recovery plus user-restored videos reduced missing sources from158
  to **zero** under the unchanged strict validator.
  The three restored videos have readable media containers, exact ledger
  durations and recorded SHA256 hashes at their original referenced paths.
  The user has no historical June19 Q5597 bundle; its original description is
  a planning-only worksheet. A current dated recovery review with16 passing
  focused tests supersedes its planning references and retains the pending
  native quest/achievement UI checks. Full canonical regeneration now validates
  31inventories /167,484rows, all not_retail_complete. All253 Python tests pass.
  Reviewed bridge inputs were replayed without guessing a new mapping. No
  retail-complete status or missing-source check was weakened.
- Remaining protocol and feature PRs now have behavior-level dispositions.
  Supported AMP lifecycle/grants, auto-satchel and native cooldown modifiers
  are implemented. Community housing and prior-save ordering, Hycrest's verified
  first mission vote and compatible NPC/target prerequisite contexts now pass
  their focused and full gates.
  Complete account Loyalty passes800 focused cases, the full6717 suite and
  32 final real-MySQL lifecycle checks across all eight tiers/44 rewards, with
  compensation, source consumption, forced binding and defaults preserved.
  Parent also applied those three additive migrations idempotently to owned
  auth, preserving all103 pre-existing tables /22,323 old rows and binding
  defaults. Corrected World6717 was staged and live verified. Additional earned-
  entitlement rollback/retry loss is corrected with commit-aware intent and safe
  snapshot/overlap handling:17 dedicated cases and34 final real-MySQL checks pass,
  without new schema or entitlement-default changes.
  Native068A per-owned-Engineer dismiss is implemented with78 regressions and
  callback/map-removal review. Property4/data3 now emits the whole multiplier
  and derives44 only from committed state, preserving raw deltas and defaults.
  Its32 cases and60 final MySQL checks pass on frozen6,923; the fresh owned copy
  has30 tables/815 rows and its source stayed unchanged during the proof.
  Contract/Challenge/Zone progression still needs evidence. Broader AMP families,
  disputed packet/effect semantics and native gameplay verification remain open.
- Asset preparation now uses staged generation, SHA256 inventory checks and
  successful-exit startup waits. A real run produced **384 tables, two locales
  and 825 maps**, and cache-only reuse succeeded. Runtime table-model loading
  and Windows PowerShell 5.1/7 validation pass. Aspire composition tests pass;
  full Docker-hosted Aspire startup remains unperformed. A read-only audit
  confirms both configured Docker engine pipes are absent and the service is
  stopped; no unrelated Docker resources were started or changed.

The active checklist is
[ASSESSMENT_REMAINING_WORK.md](I:/GIT/NexusForever-assessment/Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md).
Current evidence is in
[remaining-goal](I:/GIT/NexusForever-assessment-report/remaining-goal), especially
`DEPENDENCIES.md`, `DISPATCH_LIFETIMES.md`, `CONTENT_EVIDENCE.md`,
`PROTOCOL_PR_REVIEW.md`, `FEATURE_PR_REVIEW.md` and `RUNTIME_VERIFICATION.md`.

## Original assessment

**The fork is substantially broader in implemented functionality and regression coverage than official `game_rework`, but it is not a superset of current upstream correctness.** Keep it as the feature integration base and reconcile upstream changes in focused slices. A wholesale replacement with upstream would discard substantial work; a blanket merge would require extensive conflict resolution.

Fresh independent clone: [I:/GIT/NexusForever-assessment](I:/GIT/NexusForever-assessment). Branch: `legacy-di-storefront-hardening`. Its Git objects were seeded from the existing checkout and dissociated; the published remote SHA was verified, connectivity checks passed, and no alternates dependency remains. The original checkout was left unchanged.

## Compared revisions and scale

| Item | Verified value |
| --- | --- |
| Fork tip | [f069e0b9](https://github.com/BankaiNoJutsu/NexusForever/commit/f069e0b9d47a976013e1f7d4243d627c7f9eec70), dated 9 August 2026 |
| Official active branch | [game_rework at 1f643f63](https://github.com/NexusForever/NexusForever/commit/1f643f6374ec017ae46ac3bd72ad99d42c902d46), dated 28 October 2026 |
| Common ancestor | `a53c2129171a8894ff81bd1a4ff58e5abcee61b2` |
| Commits unique to each side | Fork 360; official `game_rework` 21 |
| Fork changes since common ancestor | 2,905 files; +828,550 / -6,104 lines |
| Source paths changed on fork | 2,591 |
| Paths changed on both sides | 132 |
| Trial merge | 102 conflict reports across 100 unmerged paths |
| Open official PR inventory | 55: 32 target `game_rework`, 11 expedition, 12 `develop` |
| Official expedition comparison | `3588ca5f`; 75 commits unique to expedition and 388 unique to fork |

Counts describe Git divergence, not feature completeness. Much of the line volume is evidence, tests, SQL and generated migrations: `Decomp` contributes 317,916 additions, `Tools` 137,752, and the game test project 132,713. The main game project accounts for 562 changed paths and 57,233 additions.

## Where the fork has substantial value

- **Storefront and account economy:** catalog/bootstrap delivery, banners, character and account purchases, gift handling, currency-slot validation, history persistence, delivery rollback and direct account grants. [StorefrontPurchaseService](I:/GIT/NexusForever-assessment/Source/NexusForever.WorldServer/Account/StorefrontPurchaseService.cs) and [purchase tests](I:/GIT/NexusForever-assessment/Source/NexusForever.Game.Tests/Account/Inventory/StorefrontPurchaseHandlerTests.cs) demonstrate a much broader implementation than the older, still-open [PR #411](https://github.com/NexusForever/NexusForever/pull/411), which targets `develop` and depends on #275.
- **Removal of the static legacy DI bridge:** commit `87961b2b` replaced it with injectable boundaries across the fork. This is real progress, although packet dispatch scopes remain disabled.
- **Gameplay and persistence:** marketplace/CREDD, mail, loot, crafting/runes, rewards, achievements, archive state, spells, paths, matching and many instance/quest scripts have concrete runtime implementations and tests.
- **Operational and evidence tooling:** Windows local setup, SQLite runtime migrations/query support, data mapping and reviewed seeds, load-test tooling, client evidence ledgers and content audits are substantial additions.
- **Regression coverage:** 5,766 fork tests pass on this clean clone. Current official `game_rework` contains no test project.

These are implementation and test observations. They do not establish complete retail behavior.

## Verified gaps and upstream lessons

**1. High priority: reconcile the merged protocol fixes, especially #587 and #591.**

The fork still uses 32-bit archive article IDs. Merged [PR #587](https://github.com/NexusForever/NexusForever/pull/587) uses 14 bits in both client packets and the server update. The fork emits a 12-byte update where upstream emits 10 bytes after padding; a 14-bit client request throws while the fork attempts a 32-bit read. Its existing [ArchivePacketShapeTests](I:/GIT/NexusForever-assessment/Source/NexusForever.Game.Tests/Network/ArchivePacketShapeTests.cs) assert the older layout.

Merged [PR #591](https://github.com/NexusForever/NexusForever/pull/591) also changes resurrection acceptance to include seven bits before the resurrection type, and auction cancellation to read the item ID before the auction ID. Compatibility probes fail against the fork and pass against current official code for all five tested archive/auction/resurrection cases. A valid upstream-shaped resurrection fixture becomes unsupported type 256 in the fork.

Port the verified layouts while retaining fork handlers and validation. Do not copy every #591 file wholesale: some changes already match the fork's wire behavior despite different names/comments. For example, its matching role response already reads the same 5 + 32 + 1 bits, and its storefront money field deliberately preserves the same float bit pattern in a uint.

**2. High priority: listener lifecycle and socket cleanup.**

[PR #572](https://github.com/NexusForever/NexusForever/pull/572) addresses the same `ConnectionListener` implementation retained on the fork. Imported tests reproduce an occupied-port startup timeout and shutdown returning before a session callback finishes. The restart test passed in this run. The same two failures occur on current official code because #572 remains open.

The merged [PR #571](https://github.com/NexusForever/NexusForever/pull/571) puts socket close in a finally block. The fork still calls `Shutdown` and `Close` in one try block, so an exception before Close skips release. This cleanup finding is source-based; the #571 socket-specific tests were not rerun here.

**3. High priority: evaluate group dirty-job fix #597.**

The fork's two dirty-character jobs and tracking repository retain the implementation described in [PR #597](https://github.com/NexusForever/NexusForever/pull/597): one context is reused for an entire loop. A separate writer can mark a tracked character dirty again without refreshing the tracked instance, allowing repeated stale processing. This is a strong, narrowly scoped integration candidate. The concurrent database reproduction from the PR was not rerun here.

**4. Packet EOF handling is improved over upstream, but incomplete.**

The fork already rejects first-time truncated reads. On the ten tests from [PR #573](https://github.com/NexusForever/NexusForever/pull/573), it passes eight; current official passes two. Both fail the two repeated-EOF cases because the reader advances its cached-bit state before a failed refill. Preserve the fork's optimized bulk reads while repairing both refill paths.

**5. Supplementary Unicode remains broken.**

Three of the nine tests from [PR #575](https://github.com/NexusForever/NexusForever/pull/575) fail on both branches. `ReadWideString` reads one UTF-16 char at a time and throws on supplementary characters. This is a small integration candidate.

Conversely, the fork already uses a lock and sets `cacheInitialized` only after successful preparation. It avoids the stuck initialization-flag design targeted by [PR #576](https://github.com/NexusForever/NexusForever/pull/576). That comparison is source-based; the PR's reflection-based tests target different private fields and were not imported.

**6. Treat closed PRs as design context, not automatic merge candidates.**

[PR #565](https://github.com/NexusForever/NexusForever/pull/565#issuecomment-5855351490) was closed because scoped packet dispatch had previously caused runtime issues. The fork removes the static service provider but still resolves messages and handlers from the injected provider; per-packet scope creation remains commented out. Investigate dependency lifetimes and exercise auth/world traffic before enabling it.

[PR #574](https://github.com/NexusForever/NexusForever/pull/574#issuecomment-5855323286) was closed after upstream removed `ConditionalEventType.Blocking`. The fork retains it and loses deferred events in three of four imported tests. No production caller explicitly selecting Blocking was found, so this is a retained API defect rather than a demonstrated live gameplay failure. Either retire it with upstream or preserve deferred events deliberately.

**7. Travel and archive PRs overlap with existing work but add useful missing behavior.**

- [#601](https://github.com/NexusForever/NexusForever/pull/601) adds persistent bind points, which the fork does not implement. [#602](https://github.com/NexusForever/NexusForever/pull/602) then recalls to that bind point; the fork's stuck transmat action currently uses the zone exit, and its Teleport effect ignores a zero location ID.
- [#604](https://github.com/NexusForever/NexusForever/pull/604) overlaps with the fork's existing opcode-0x00FF flight purchase handler, despite using different class names. The fork already validates contiguous route IDs, affordability and destination data. The PR adds node discovery/persistence and source-kiosk/faction checks worth reconciling; the fork handler does not check source proximity or route faction.
- [#603](https://github.com/NexusForever/NexusForever/pull/603), [#605](https://github.com/NexusForever/NexusForever/pull/605) and [#606](https://github.com/NexusForever/NexusForever/pull/606) overlap with existing housing teleport, AddSpell/Disembark, stuck and instance-return code. Their spell teaching, passive reapplication and bind-point fallback need focused comparisons.
- [#607](https://github.com/NexusForever/NexusForever/pull/607) adds pending-mount protection; the fork's CanMount checks platform/pending teleport without that pending-summon guard.
- [#588](https://github.com/NexusForever/NexusForever/pull/588) adds immediate achievement/quest archive hooks. The fork already has persistence, path rules and rule refresh at world entry, but no equivalent completion hooks were found. Its article title reward also triggers at initial article unlock, whereas the PR waits for every entry. Resolve these semantic differences after the wire fix.
- [#598](https://github.com/NexusForever/NexusForever/pull/598)'s duplicate-datacube exception is already handled more centrally: fork AddDatacube merges progress and resends the existing entry.

**8. Entitlement and loot work need deliberate integration choices.**

[#590](https://github.com/NexusForever/NexusForever/pull/590) combines a hidden-bit correction and entitlement insert fix with a policy decision to grant seven baseline character entitlements. The fork still treats bit 0x2 as Disabled and leaves the new-character entitlement Create save mask unset. It also grants two baseline account entitlements independently. Maintainers [question the automatic defaults](https://github.com/NexusForever/NexusForever/pull/590#issuecomment-6242189128). Separate correctness fixes from configurable server policy.

The largest recent upstream addition is the loot-rework merge, including group loot rules, master-looter/need-before-greed distribution and readonly group structures. The fork already has its own loot and persistence work. Reconcile responsibilities and schemas with tests rather than replacing one implementation wholesale.

The expedition prerequisite enum in [#594](https://github.com/NexusForever/NexusForever/pull/594) differs from both fork and current game_rework for comparison values 3–6. It warrants a client-evidence check, especially alongside broader prerequisite PRs #554/#568, before changing runtime semantics.

## Verification and limits

| Check | Result |
| --- | --- |
| Fork full solution build, SDK 10.0.300 | Passed; 0 errors, 114 warnings |
| Official exported snapshot full solution build, same SDK | Passed; 0 errors, 60 warnings |
| Fork existing tests | 5,766 passed, 0 failed |
| Separate fork upstream-regression/compatibility probes | 16 passed, 15 failed, 31 total |
| Same applicable probes on official snapshot | 14 passed, 13 failed, 27 total; Blocking API tests excluded because upstream removed it |
| Python content audit/validator unit tests | 253 passed |
| Committed content coverage validator | Failed: 158 unique evidence sources absent from this fresh clone |
| Git connectivity and clean checkout | Passed |

The supplemental harness is outside the clone. It imports tests from #572/#573/#574/#575, adapts only the listener test double's OnData signature, and adds five fixtures from merged #587/#591 packet layouts. Production code was not changed. These suites intentionally expose current defects; their aggregate totals are not comparable quality scores because upstream lacks the Blocking API and the fork has a stronger initial EOF check.

NuGet emitted actual dependency advisories on both builds. Shared reports include MySqlConnector and MessagePack; the fork adds SQLite native-library exposure while upstream retains older SharpCompress warnings. See the exact package/advisory tuples in [evidence-summary.json](I:/GIT/NexusForever-assessment-report/evidence-summary.json). Warning totals include repetitions across projects.

The committed content snapshot has **31 CSVs / 167,482 rows, all marked not_retail_complete**. All 23 queue rows with retail_claim_allowed are false. Missing evidence inputs include client SQL, Jabbithole dumps and generated DataMapping output; 748,954 repeated missing-source references were reported, not that many distinct missing files. Audit outputs were not regenerated from incomplete inputs. The repository's 13 “implementation-complete” feature areas explicitly do not claim retail parity.

No live client session, database migration against a populated server, broker flow or load test was run. All 55 open PRs were inventoried and compared by changed paths/blob identity; semantic review focused on relevant overlapping systems and recent fixes. Absent/renamed paths do not prove missing features.

## Recommended integration order

1. Repair #587 archive widths and the demonstrated #591 auction/resurrection layouts; add compatible fixtures to the fork suite.
2. Port #571 cleanup, #572 listener lifecycle, #573 repeated-EOF handling and #575 Unicode; evaluate #597 with a concurrent DB regression.
3. Separate #590 hidden-bit/insertion corrections from entitlement-default policy; investigate packet scopes with #565's runtime history in mind.
4. Reconcile the upstream loot/group refactor as its own larger integration slice.
5. Port missing bind-point/travel behavior and immediate archive hooks while retaining existing fork implementations, migrations and tests.
6. Rebuild the content evidence inputs and run client smoke checks before upgrading any retail-completeness claim.

The evidence and independent test harnesses are saved in [I:/GIT/NexusForever-assessment-report](I:/GIT/NexusForever-assessment-report). See [the complete PR triage](I:/GIT/NexusForever-assessment-report/PR-TRIAGE.md), [file comparison CSV](I:/GIT/NexusForever-assessment-report/pr-file-comparison.csv), [raw PR snapshot](I:/GIT/NexusForever-assessment-report/pull-requests.json), [merge preview](I:/GIT/NexusForever-assessment-report/upstream-merge-preview.txt) and test-result TRX files.

To reproduce from the fresh fork clone:

```powershell
git rev-list --left-right --count upstream/game_rework...HEAD
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-build --no-restore -v minimal --nologo
dotnet test I:\GIT\NexusForever-assessment-report\upstream-regressions\UpstreamRegressions.csproj -v minimal --nologo
dotnet test I:\GIT\NexusForever-assessment-report\baseline-regressions\BaselineRegressions.csproj -v minimal --nologo
python -m unittest discover -s Tools\WikiArchiveAudit -p 'test_content_retail_completeness*.py'
python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py
```

At the original recorded revisions both supplemental commands and the content
snapshot validator failed as described above. After reconciliation the fork's
upstream-regressions suite now passes 31/31; baseline-regressions intentionally
still targets the frozen official export rather than the repaired fork. The
content validator currently remains strict about four missing historical
inputs. Use an installed Python 3 interpreter; the baseline run used 3.12.13.
Original PR metadata, refs and baseline export remain frozen to the revisions
above; current follow-through evidence is recorded separately.
