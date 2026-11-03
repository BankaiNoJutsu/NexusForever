# Client Binary Decompile Continuation Guide

Latest provider evidence: [44-case MySQL listing concurrency qualification](MARKETPLACE_CONDITIONAL_LISTING_WRITES.md#actual-mysql-qualification---3-november-2026)
uses unchanged Source/DLLs. Broader financial, physical-failure and retail gates remain open.

[Committed tradeskill publication](TRADESKILL_PUBLICATION.md) passes 15 new/238 scoped
and 15 MySQL cases with a clean owning-project build and both Source reviews.
Original ordinary/frozen graphs release immutable skill/recipe/reset notices;
initial lists use published rows and preserve quiet discovery and packet retries.
No full suite was run under the user's scoped-only policy. Broader gameplay,
restart/outbox, deployment and native/retail gates remain open; the Goal is active
and client/CUA stay stopped.

[Committed Archive publication](ARCHIVE_PUBLICATION.md) passes 18 new/241 affected,
18 MySQL and 12,007 full cases with a clean build and both Source reviews. Original
confirmed graphs release ordered unlock/view notices; initial sync, quiet rules,
later frozen bits and packet retries remain correct. Tradeskill publication
is next. Restart/outbox, deployment and native/retail remain open; the Goal is active and client/CUA stay stopped.

[Committed title publication](TITLE_PUBLICATION.md) passes 19 new/185 affected,
18 MySQL and 11,989 full cases with a clean build and both Source reviews. Ordinary,
frozen and bound-row saves release retained notices only after confirmation;
initial/entity state and accepted bulk lists remain correct. The linked archive
probe supplies the next defect. Restart/outbox and native/retail gates remain
open; the Goal is active and client/CUA remain stopped.

[Durable realm-first arbitration](REALM_FIRST_ARBITRATION.md) passes 16 new/237
affected and11,970 full cases with a clean build and both Source reviews.
Final MySQL coverage is 12+1 recovered after a retained disk-full setup failure;
two independent-process scenarios and controlled copied upgrades pass. Historical
closure and winner deletion remain safe. See the linked evidence boundaries.
Companion title publication has a new executed negative; restart/outbox and
native/retail gates remain open. The Goal stays active; client/CUA remain stopped.

[Committed character achievement publication](CHARACTER_ACHIEVEMENT_PUBLICATION.md) passes23 new/1,121
affected and11,954 full cases, plus56 fixture/6 warmed-cache passes and
versioned23+6 provider evidence. This includes the corrected
zero guild-rank key mapping. Earlier failures remain retained. Durable global
arbitration, companion publication and external/native/retail gates stay open.
The Goal is active; client/CUA remain stopped.

[Committed community placement/removal](HOUSING_ASSOCIATION_PUBLICATION.md) is the latest housing result.
The linked30-case/MySQL,526-affected and11,931-full gate preserves controlled
travel and all external/native/retail limits. Continue the broader roadmap;
the Goal remains active and client/CUA launch stays stopped.

[Committed remodel publication](HOUSING_REMODEL_PUBLICATION.md) is the latest housing result.
The linked 22-case/MySQL and 11,901-test full gate retains the earlier affected
timeout and all external/native/retail limits. A controlled placement probe
supplies the next failing flow; the broader Goal remains active.

[Confirmed community directory recovery](COMMUNITY_DIRECTORY_RECOVERY.md) is the latest housing result.
The linked 25-case/MySQL and 11,879-test full gate preserves external/native/retail
limits. A real remodel-publication probe supplies the next failing flow; the
broader Goal remains active.

[Committed community privacy](HOUSING_PRIVACY_PUBLICATION.md) is the latest housing recovery result.
The linked record covers 26 SQLite/MySQL regressions and 11,854 full tests.
External, native and retail qualification remain open. Visitor/search index lifecycle has a separate executed follow-up probe;
the broader Goal and remaining housing work stay open.

[Community rename fee settlement](HOUSING_RENAME_FEE_SETTLEMENT.md) records the preceding verified housing
recovery milestone. Its fee/names commit together and publication follows SQL;
the linked record covers 31 regressions on SQLite/MySQL and 11,828 full tests.
External, native and retail qualification remain open.
Placement publication and remaining housing work stay open; the broader Goal is active.

[Registered residence recovery](RESIDENCE_GRAPH_RECOVERY.md) now persists
complete residence/plot/decor/neighbor graphs through original owners, including
community creation and retirement. All20 new/340 affected,20 MySQL and11,772 full
tests pass; build/reviews and final bindings verify. Donation/fees/publication
and external/native/retail gates remain open; the Goal is active.

[Guild achievement recovery](GUILD_DISBAND_RECOVERY.md#guild-achievement-recovery-verified) retains guild-owned achievements with their original parent through failed,
rolled-back and uncertain saves, including achievement-only periodic saves and
first creation. Progress and reserved realm-first announcements wait for confirmed
persistence and retain immutable retry order. All21 new/439 affected,22 MySQL and
**11,752 full tests** pass; build/reviews and6,219-input/11-DLL bindings verify.
Cross-character atomicity, durable realm-first arbitration, plot-backed housing,
fees, live broker/restart and native/retail gates stay open. Defaults15:1/17:1/no44/
seven grants and client/CUA stop remain; the Goal is active.

[Guild creation graph recovery](GUILD_DISBAND_RECOVERY.md#guild-creation-graph-recovery-verified) retains the accepted root/data/rank/member
create through failed, rolled-back and uncertain saves, preserves later edits and
defers creation/member publication. Registered actors bind their configured route
before first save; attempted create/disband resolves SQL before cleanup. Arena
types now reload correctly. All21 new/245 affected,21 MySQL and **11,731 full tests**
pass; clean build/reviews and6,218-input/11-DLL bindings verify. Achievement and
plot-backed housing creation, fee atomicity, broker/restart and native/retail
qualification remain open. Defaults15:1/17:1/no44/seven grants and client/CUA stop
remain; the Goal is active and earlier checkpoints retain their own limits.

[Ordinary guild mutation recovery](GUILD_DISBAND_RECOVERY.md#ordinary-guild-mutations-verified) retains accepted header/data/rank/member
edits and deletion owners through failed saves, preserves later disband edits and
publishes committed net member changes through the periodic owner. All30 new/224
affected and **11,710 full tests** pass, with clean build/reviews and30 MySQL8.4.13
cases. Final6,217-input/11-DLL-pair bindings and owned-workload shutdowns verify.
Failed/uncertain whole creation, financial atomicity, broker/restart and external/
native/retail gates remain open. Defaults15:1/17:1/no44/seven grants and the client/
CUA stop remain. The Goal is active; earlier checkpoints retain their own limits.

[Latest guild/community name gate](GUILD_DISBAND_RECOVERY.md#name-lifecycle-recovery-verified)
reserves names before fees and retains guild/residence name intent and aliases
through failed saves. All14 new/184 affected and11,680 full tests pass; build,
Source reviews and final source/DLL bindings verify. MySQL name coverage passes
as12 retained plus2 corrected strategy fixtures; original failures stay retained.
Database load-provenance source changed; schema/migrations did not. Ordinary
other guild and failed/uncertain creation recovery, atomic fees, broker/restart
and native/retail limits remain open. Defaults15:1/17:1/no44/seven grants and
client/CUA stop remain. The Goal stays active; prior entries are historical.

This guide is the working playbook for continuing the NexusForever client binary
reverse-engineering effort. It is meant for future human or AI-assisted passes
that need to map native client behavior, label functions, and turn proven
findings into clean NexusForever server changes.

The goal is not to recreate or copy client source. The goal is to extract
behavioral evidence from locally owned WildStar 16042 client binaries, correlate
that evidence with NexusForever tables, packets, and runtime systems, and
implement server behavior from the evidence in the existing C# codebase.

## Current Artifacts

[Guild disband recovery](GUILD_DISBAND_RECOVERY.md) now qualifies the public/periodic
tombstone, post-commit cleanup and name-release path with 18 new/170 affected and
11,666 full passing tests. Source reviews/build and final input/DLL/test bindings
verify. Current upstream #401 is source-reconciled. This does not qualify ordinary
guild field/rename recovery, live brokers, restart/outbox durability or native/
retail behavior. No client/CUA launch or database schema/default change occurred.

[Current unreserved bank currency recovery](DESTINATION_PROVIDER_QUALIFICATION.md) drains late
currency for bank move/split/use/delete, single attachment and unreserved/new
wallet flows, and rejects publication reentry before accepting mutations.
Frozen `2c4aff9e`/6,202 inputs passes **11,634 full tests**, zero failures/skips,
all26 new/144 focused and all prior11,608 labels,26 MySQL cases and clean build/
reviews. Final bindings and all7 recorded workload shutdowns verify; schema/DB
DLLs are unchanged. Defaults15:1/17:1/no44/seven grants and client/CUA stop remain.
The later [scanner-name source gate](SCIENTIST_SCANBOT_NAME_PUBLICATION.md) passes11,648 full tests
and connects the accepted Scientist profile name to pending/active scanners and
visible08CC updates. Other five aux producers and native/retail timing remain
unqualified. Regular scoped commits/pushes remain authorized after review; the
Goal stays active. Earlier milestone entries retain their historical boundaries.

[Current legacy bank reconciliation](DESTINATION_PROVIDER_QUALIFICATION.md) retains deferred
currency and the complete original actor through rollback/lost acknowledgement,
then guards vendor/mail publication. Final `824fab02`/6,201 inputs passes
**11,608 full tests**, zero failures/skips, including all 41 new, 100 focused and
1,201 earlier affected labels. All 38 configured-MySQL cases and the synthetic
schema upgrade pass; full build/reviews and final source/DLL/archive bindings
verify. All 14 recorded workload generations and the owned listener are absent.
Failed/interrupted gates remain retained. Defaults15:1/17:1, no44/seven grants,
no client/CUA/commit/push and external/retail limits remain. The Goal stays active
for the next real-Player currency-free/unreserved-wallet audit and broader work.

[Current account keybinding recovery](DESTINATION_PROVIDER_QUALIFICATION.md) retains accepted
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

[Auction posting settlement](AUCTION_POSTING_TRANSACTION.md) qualifies the
last passing **11,354-case full source/private-SQLite gate**, zero failures/skips and
build warnings/errors. The public handler atomically escrows the exact whole Item
and listing, then completes original-actor reconciliation and guarded publication.
Held metadata/expiry defects have executed regressions and are corrected.193 focused,
1,106 affected and final reviews0/0 pass; 6,156 inputs/nine DLL pairs/test assembly
verify at a900f698. That gate also covers commodity posting; its earlier
failed full candidate and unresolved timing cause stay retained. Provider,
actual-table, socket/native and retail gates remain separate. No commit/push/client
launch. The Goal remains active.

[Commodity return settlement](COMMODITY_RETURN_SETTLEMENT.md) qualifies11,264 full source/private-SQLite
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

[Auction cancellation settlement](AUCTION_CANCELLATION_TRANSACTION.md) qualifies11,203
full source/private-SQLite cases, zero failures/skips and build warnings/errors;
review0/0. Public cancellation atomically returns the original whole Item and
refunds the held bid, with zero owner debit/proceeds, original-actor recovery,
direct/mail custody and guarded publication.545 affected cases pass. Historical auction boundary; see the canonical
ledger and result for limits. Earlier Challenge intermittent causes remain
unproven. No provider/native/retail completeness, commit or push is claimed.

[Currency conditional writes](CURRENCY_CONDITIONAL_WRITES.md)
pass10,955 full/24 new race cases, retaining all14 frozen-currency,264 marketplace
and297 reviewed mail cases. Eight executed failures/sixteen controls, both
original-provider model diffs0 DDL/0 connections, five6,118-input archives/nine
DLL pairs and final reviews0/0 verify. The first full's66 test-helper failures
are retained; the loaded-original fixture correction preserves every oracle and
passes corrected focused/full gates. One later sharing failure/drain diagnostic,
four isolated passes and the unchanged-binary default full retry remain separate;
the cause is unproven. Loaded provenance, full financial/actor/
deferred recovery and provider/Contract/native/retail work remain. No push/client.

[Frozen currency preparation](FROZEN_CURRENCY_PREPARATION.md)
passes10,931 full/14 focused tests, retaining all264 prior marketplace and297
reviewed mail cases.14 new cases, one executed review failure retained, four
6,117-input archives/nine DLL pairs and final reviews0/0 verify; full/test-copy
rebuilds have zero warnings/errors. Explicit token scope is tested; deferred
Abort now rejects/quarantines for both legacy/frozen callers instead of silently
releasing intent. Durable reconciliation is still unimplemented and the API is
unwired to financial handlers. Source/SQLite only; no push or client launch.

[Actor-queue predicate failure recovery](ACTOR_QUEUE_PREDICATE_RECOVERY.md)
passes 10,917 full tests and all thirteen queue cases, retaining all 264 prior
marketplace and 297 reviewed mail cases. Eight new cases, six executed baseline
failures/seven passes, three 6,116-input archives/nine DLL pairs and both final
reviews verify; rebuild/test-copy builds have zero warnings/errors. Failed
predicates no longer poison the front, and throwing hooks retain earlier deferred
work while preserving propagation. Captured financial/actor/delivery/provider/
Contract/native/retail work remains. No push or client launch.

[Confirmed marketplace save outcomes](MARKETPLACE_CONFIRMED_SAVE_OUTCOMES.md)
pass 10,909 full tests, all 264 marketplace and 297 prior reviewed mail cases,
with 13 new cases and zero failures/skips or rebuild/test-copy warnings/errors.
Four executed baseline failures/seven controls, a corrected eleven-case run and
both final reviews verify. Five 6,116-input archives/nine DLL pairs retain one
excluded stale-DLL prototype caused by preserved copy timestamps. Confirmed SQL
success survives later errors; unconfirmed outcomes remain explicit errors.
Complete captured financial/unknown-acknowledgement/actor/direct-delivery recovery,
provider/Contract/broader qualification remain. No push or client launch.

[Conditional marketplace listing writes](MARKETPLACE_CONDITIONAL_LISTING_WRITES.md)
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

[Marketplace listing SQL postimages](MARKETPLACE_LISTING_POSTIMAGES.md)
pass 10,860 full tests, including all 215 marketplace and 297 prior reviewed mail
cases; 24 new cases, zero failures/skips and zero rebuild/test-copy warnings or
errors. Six executed baseline failures/six controls and an executed corrected
late-writer regression are retained. Five archives bind 6,111 negative/6,112
later inputs and eight DLL pairs; both final reviews verify. Complete listing
rows/absence are checked before transaction commit, including shared mail
callbacks. CAS, captured financial/ambiguity and direct-delivery recovery,
Contract and broader qualification remain. No push or client launch.

[Revised upstream604 taxi reconciliation](UPSTREAM_604_TAXI_SAVE_RECONCILIATION.md)
passes10836 full tests and34 focused cases in rebuilt full,16 net-new cases plus
one explicitly corrected shuttle45 oracle. Five executed baseline failures/18
controls,6109 negative/6110 final inputs, four archives/eight DLLs and final
two-axis review verify. Rebuild/test-copy builds have0 warnings/errors. Taxi
discoveries settle at terminal commit, retain rollback/later intent and support
key-only lost-ack retry; real Player.Save coupling is covered. The exact revised
604 head36b59aaaaebbcd0ec3e23a94fd3cf9de8975923b is now reconciled while retaining
fork injection/stronger guards.608 remains handled;609 closed unmerged. Broader
marketplace/Contract/provider/native/retail work remains; no push/client launch.

[Marketplace database-route binding](MARKETPLACE_DATABASE_ROUTE_BINDING.md)
passes10820 full tests, including191 marketplace and297 prior reviewed mail cases
in rebuilt full,9 new cases and zero rebuild/test-copy warnings/errors. Two
executed cross-route failures/two controls, a corrected introduced orphan-ID
regression, five6108-input archives/eight DLLs and final two-axis review verify.
Equal rows in another database cannot confer ownership; normalized same-route
ordering, inserted remainders and startup ID marks remain covered. Full SQL
postimages, CAS and complete economic recovery remain.04:23UTC upstream604 alone
changed among58 refs; its exact old/new delta and later reconciliation are recorded above.
No provider/native/retail promotion, push or client launch.

[Marketplace loaded-row preimages](MARKETPLACE_LOADED_ROW_PREIMAGES.md)
pass10811 full tests, including all182 marketplace and297 prior reviewed mail
cases in the rebuilt full run,28 new cases and zero rebuild/test-copy warnings
or errors. Twenty-four executed baseline failures/two controls,6107 final/6106
negative inputs, four archives/eight DLLs and final two-axis review verify.
All16 auction/10 commodity scalars and live-view consistency are checked;
legacy CSV, memory mode, repeated bids and reentrant rejection remain covered.
This is read/staging validation. SQL postimages, route ownership, CAS and complete
financial/outcome recovery remain, alongside Contract and broader work. No
provider/native/retail promotion, push or client launch.

[Marketplace required-row and expiry guards](MARKETPLACE_STORED_ROW_GUARDS.md)
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

[Committed marketplace mail publication](MARKETPLACE_MAIL_PUBLICATION.md)
passes10749 full tests including297 reviewed mail cases on rebuilt DLLs,23 new
cases and zero rebuild warnings/errors. Six executed baseline failures and one
control,6104 final/6102 negative inputs, complete archives/eight DLLs and both
review axes verify. Post-commit lookup/notification errors preserve confirmed
SQL success and Item ownership; delivery waits for the original recipient actor
and save scope after SQL account/database routing checks. Marketplace economic
settlement and SaveBlocking ambiguity, Contract and broader work remain. No
configured provider/native/retail promotion, push or client launch.02:54UTC
upstream inventory remains56 open PRs,609 newest; all58 tracked refs unchanged.

Latest1 November [marketplace mail custody](MARKETPLACE_MAIL_CUSTODY_COMPATIBILITY.md)
passes10726 full tests including274 reviewed mail cases on rebuilt DLLs,23 new
cases and zero rebuild warnings/errors.6101 final/6100 negative/6101 intermediate-
failure archives/eight DLLs and both review axes verify. The known legacy origin/
stored-page check preserves full witnesses and terminal quarantine. Marketplace
actor publication/economics, Contract and broader Goal continue. No provider/
native/retail promotion, push or client launch.

Latest1 November [available-mail expiry publication](MAIL_EXPIRY_PUBLICATION.md)
passes10703 full tests including251 reviewed mail cases on rebuilt DLLs,18 new
cases and zero rebuild warnings/errors.6099 final/6096 negative/6099 failed-
fixture archives/eight DLLs and both review axes verify. One fixture sorting error
and the earlier manual-gate cleanup failure remain explicit. Marketplace custody/
publication/economics, Contract and broader Goal continue. No provider/native/
retail promotion, push or client launch.02:04UTC upstream heads remain unchanged;
609 newest and merged-since-assessment query returns the already reconciled598.

Latest1 November [manual return/delete publication](MANUAL_MAIL_ACTION_PUBLICATION.md)
passes10685 full/233 rebuilt mail tests,27 new cases and zero rebuild warnings/
errors.6095 final/6092 negative inputs, archives/eight DLLs and both review axes
verify. One earlier full-run Challenge cleanup sharing failure is retained with
unproven cause; three isolated passes and the same-binary default full retry pass
are separate observations. Expiry, marketplace, Contract and broader Goal remain.
No fresh provider/native/retail qualification, push or client launch is claimed.

Latest1 November [bulk mail selection](MAIL_SELECTION_TRANSACTION.md)
passes10658 full/206 focused mail tests,22 new cases and zero rebuild warnings/
errors.6091 final/6087 negative inputs, archives/eight DLLs and both review axes
verify. Manual return/delete publication and broader features continue. Later
upstream inventory remains56 open PRs,609 newest;608/609 and official branch
heads remain pinned. Source tests do not qualify provider/native/retail behavior.

Latest1 November [individual attachment collection](MAIL_ATTACHMENT_TAKE_TRANSACTION.md)
passes10636 full tests/184 focused mail cases,16 new cases and a zero-warning/
error rebuild. All6086 inputs/archive/eight DLLs and three distinct source epochs
verify; an invalid fixture and a false-positive SQL control remain explicit.
The later bulk source gate above has its own binding. The00:19:43UTC refresh has56 unchanged open
upstream heads. No wire/index, provider/native/retail completeness or push/launch
is claimed; the Goal remains active.

Latest1 November [outgoing-mail source gate](OUTGOING_MAIL_RETENTION_AND_PUBLICATION.md)
passes10620 full tests and49 focused outgoing/delivery cases,34 new cases, zero
failures/skips and zero full-build warnings/errors. The qualified snapshot's6081 inputs,
complete archives/eight DLLs and eight executed negative epochs verify. Earlier
checkpoints below retain their original proofs. [Attachment index evidence](MAIL_ATTACHMENT_INDEX_EVIDENCE.md)
is mapped-only; receive/update and independent Verified corroboration remain open.
The23:20:27UTC upstream refresh still has56 unchanged open heads. The Goal stays
active; no fresh provider/native/retail completeness or push/launch is claimed.

Latest legacy mail header/deletion checkpoint (31 October): [outcome recovery](MAIL_SEND_TRANSACTION_AND_INBOX_RECOVERY.md)
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

Prior player-mail creation checkpoint (31 October): [atomic player sends and inbox recovery](MAIL_SEND_TRANSACTION_AND_INBOX_RECOVERY.md)
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

Prior upstream609 source checkpoint (31 October): [communicator scheduling and reports](UPSTREAM_609_COMMUNICATOR_RECONCILIATION.md)
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

Destination continuation,31 October: source work is in
`C:/GIT/NexusForever/continuation`. The handoff-only `a50dfa13` commit adds the
[lean archive](../../Evidence/Continuation/README.md), verified/restored with its
standard-library helper after10 passing evidence-store tests. Its2,154 members
are under ignored `.continuation-evidence/`; use
`Tools/Handoff/Restore-Evidence.py --legacy-path` to resolve historical absolute
links. Match published hashes and
redaction records before treating a recovered member as a byte-identical original.
The Contract Lua/provenance and latest historical provider harnesses are local;
general caches, client binaries, raw datasets, private runtime state and installed
tool configuration are outside that archive. Current native Ghidra configuration
is unavailable; restoring focused exports does not establish a live provider or
new native qualification. Computer Use/native launch remain stopped. Source
build/tests and independent implementation can continue under the active Goal.

| Artifact | Purpose |
| --- | --- |
| `Decomp/Client64` | Local client binaries. Ignored by Git. |
| `Decomp/Analysis/setup_decomp_tools.ps1` | Installs the pinned Java and Ghidra toolchain. |
| `Decomp/Analysis/run_ghidra_analysis.ps1` | Runs or re-exports headless Ghidra analysis. |
| `Decomp/Analysis/Get-GhidraMcpWorkflowHints.ps1` | Prints MCP-ready project, program, and evidence-file hints from the latest summaries, manifests, and `.gpr` files. |
| `Decomp/Analysis/GHIDRA_MCP_PROMPT_ADAPTER.md` | Maps upstream `bethington/ghidra-mcp` prompt workflows onto the Nexus evidence ladder and durable label workflow. |
| `Decomp/Analysis/DATA_TYPE_INVESTIGATION_WORKFLOW.md` | Local checklist for evidence-backed parameter typing, structure discovery, offset maps, and family-wide type application. |
| `Decomp/Analysis/scripts/ApplyNexusForeverLabels.java` | Applies source-controlled function labels before export. |
| `Decomp/Analysis/scripts/ExportNexusForeverAnalysis.java` | Writes repeatable CSV and decompiler exports. |
| `Decomp/Analysis/scripts/DumpNearbyData.java` | Dumps nearby data-table slots and resolves pointer targets. |
| `Decomp/Analysis/scripts/DumpAsciiAtAddress.java` | Dumps raw bytes and printable ASCII for short undefined strings. |
| `Decomp/Analysis/scripts/InspectCodeAddress.java` | Decompiles a function or dumps raw instructions at a code address. |
| `Decomp/Analysis/scripts/TraceFunctionCallers.java` | Lists direct references to a target function and prints each caller instruction window. |
| `Decomp/Analysis/function_labels.csv` | Durable function label map. This is the main bridge from native addresses to named evidence. |
| `Decomp/Analysis/INITIAL_FINDINGS.md` | Living summary of mapped behavior and follow-up implementation. |
| `Decomp/Analysis/mapping_artifacts/README.md` | Seven-layer durable mapping workflow for opcode provenance, function evidence, structure offsets, dispatcher registrations, source traceability, live evidence bundles, and packet fixture planning. |
| `Decomp/Analysis/Validate-DecompileMappingArtifacts.ps1` | Validates mapping artifact headers, evidence-ladder states, unique row ids, and review dates. |
| `Decomp/Analysis/Get-DecompileMappingArtifactGaps.ps1` | Audits missing coverage between opcode inventory, function labels, and the seven mapping ledgers. |
| `Decomp/Analysis/STOREFRONT_CATALOG_UNAVAILABLE.md` | Resolved local storefront *Catalogue Unavailable* chain (realm data center id, catalog timing, `0x0989`). |
| `Decomp/Analysis/Get-DecompCoverageSnapshot.ps1` | Generates the current export and opcode coverage inventories from local artifacts and source. |
| `Decomp/Analysis/exports/<binary>/selected_reasons_summary.csv` | Selection audit for the focused export, including why a function was selected and whether it is inside the current decompile cutoff. |
| `Decomp/Analysis/exports/<binary>/selected_call_edges.csv` | Direct caller/callee edges for each selected function, including call/jump site addresses and whether the neighbor is also selected or inside the decompile cutoff. |
| `Decomp/Analysis/exports/<binary>/function_pointer_families.csv` | Contiguous exact-function pointer families from non-executable memory, useful for vtable, callback-table, and dispatcher-family recovery. |
| `Decomp/Analysis/exports/<binary>/function_pointer_family_slots.csv` | Per-slot function metadata for each pointer family, including namespace, thunk state, and slot reference counts. |
| `Decomp/Analysis/exports/<binary>` | Generated analysis exports. Ignored by Git. |
| `Decomp/Analysis/logs/LATEST_RUN_SUMMARY.json` | Queryable summary of the last headless run, target set, project layout, log paths, and export outputs. |
| `Decomp/Analysis/logs/LATEST_COVERAGE_SUMMARY.json` | Queryable snapshot of export counts plus opcode/model/handler coverage. |
| `Decomp/Analysis/coverage/LATEST_COVERAGE_SUMMARY.md` | Human-readable coverage snapshot and priority queues. |
| `Decomp/Analysis/coverage/export_coverage_inventory.csv` | Per-target export coverage counts for the current local snapshot. |
| `Decomp/Analysis/coverage/opcode_coverage_inventory.csv` | Opcode/model/handler coverage inventory for the current local snapshot. |
| `Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md` | Live-client evidence passes for remaining partial features (F-008..F-021, F-031) with implement/diagnostic/blocked boundaries. |
| `CURRENT_STATUS.md` (repo root) | Single source of truth for all 36 feature-area completion states, consolidated emit gaps, and code-audit notes vs false "ghost gap" claims. |
| `Decomp/Analysis/MATCHING_IMPLEMENTATION_STATUS.md` | Matching-only implemented vs mapped-only vs rejected surfaces. |
| `Decomp/Analysis/Start-BlockerEvidenceHarness.ps1` | Starts Trace logging + client console and prints log-tail commands for evidence bundles. |

The default analysis targets are:

- `WildStar64.exe`
- `Houston64.exe`
- `StsConnLib64.MT.dll`

## Working Principles

1. Keep client binaries, Ghidra projects, logs, and decompiled exports out of
   source control.
2. Keep durable knowledge in source-controlled artifacts:
   `function_labels.csv`, `INITIAL_FINDINGS.md`, `mapping_artifacts/*.csv`,
   focused tracker documents, and clean C# implementation changes.
3. Prefer behavioral summaries over copied decompiler output. Use addresses,
   function names, export file names, and short line references to make evidence
   findable.
4. Label only when the name is supported by evidence. Use conservative names
   such as `Parse...`, `Send...`, `Register...`, `Resolve...`, or
   `Maybe...` when confidence is not complete.
5. Implement only the behavior that is stable across evidence sources. Keep
   uncertain payload fields diagnostic-only.
6. Each pass should leave a trail: what was mapped, what was labelled, what was
   implemented, what stayed blocked, and how it was verified.
7. Use `ghidra-mcp` as a focused interactive probe, not as the durable record.
   Mirror stable labels into `function_labels.csv` and re-export before relying
   on them in future passes.

## Evidence Ladder

Use the same confidence language in findings, labels, commit messages, and
implementation comments.

| Status | Meaning | Allowed action |
| --- | --- | --- |
| Observed | A string, import, constant, branch, or call shape appears in one client function. | Record in notes. Do not implement. |
| Correlated | The observation lines up with a table, packet, enum, handler, sniff, or existing server path. | Add a tentative finding and possibly a conservative label. |
| Mapped | Inputs, outputs, field names, constants, and call direction are understood well enough to explain the function. | Add a durable label and update `INITIAL_FINDINGS.md`. |
| Verified | Local runtime behavior, sniff evidence, table data, or packet shape confirms the mapped meaning. | Implement server behavior. |
| Implemented | NexusForever code was changed and build or focused runtime checks passed. | Record the implementation and verification result. |
| Blocked | The evidence shows a feature but not enough safe semantics to mutate server state. | Keep diagnostic-only and list the blocker. |

## Repeatable Flow

### 1. Choose A Narrow Target

Start each pass with one behavior family, not a whole binary. Good target shapes:

- One STS transaction, such as `/Auth/LoginFinish` or `/Auth/ConsumeGameToken`.
- One packet framing question, such as header size, opcode width, size limits,
  compression, or encryption boundary.
- One Client DB table registration path, such as `WorldSocket.tbl` or
  `RealmDataCenter.tbl`.
- One enum or Lua registration family, such as public-event objective constants.
- One spell or gameplay family, such as `Proc`, `RavelSignal`, target masks, or
  shield-related effects.

Write the target as a one-sentence question:

```text
What fields does the client send and parse for /Auth/<Transaction>, and what
does NexusForever need to accept or emit for build 16042 compatibility?
```

```text
What constants does the client expose for <enum/system>, and do the current
NexusForever enum values match the client-facing values?
```

```text
Which Spell4Effects payload fields are consumed by the client for <family>, and
what server behavior can be implemented without guessing?
```

### 2. Refresh Or Scope The Export

Use the existing Ghidra project whenever possible:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets StsConnLib64.MT.dll -MaxDecompiledFunctions 350
```

For a focused world-client pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 500
```

For a fresh default analysis:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1
```

For callback-table, vtable, or raw-stub work, start with the standard export so
`function_pointer_families.csv` and `function_pointer_family_slots.csv` capture
the reusable structure, then attach a focused helper script when you need local
data bytes or instruction windows:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 340 -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140b73540','20')
```

If you omit `-MaxDecompiledFunctions` on a helper-script export-only pass, the
runner preserves the current manifest cutoff for that target. This keeps
`selected_reasons_summary.csv`, the manifest, and the coverage snapshot aligned
with the existing `selected_decompiled.c` breadth. Pass
`-MaxDecompiledFunctions` explicitly when you intentionally want to narrow or
widen the focused export.

Re-export commands now default to manifest-based reuse for `selected_decompiled.c`.
If the binary fingerprint, label state, `-MaxDecompiledFunctions`, exporter
version, and ordered top-N selected functions are unchanged, the exporter keeps
the existing decompiler output and refreshes only the cheaper CSV and text
artifacts.

The exporter also keeps a canonical per-function cache under
`selected_decompiled_cache/functions/<binary-fingerprint>`. If a function body
has already been exported for the same binary/function identity, later runs reuse
that canonical fragment instead of writing another physical copy. The exporter
removes old `selected_decompiled_cache/<context-hash>` folders; only the
`functions` folder is kept, used, and populated.

By default, normal runs incrementally warm the full-program cache up to the
`-MaxWarmFunctionsPerRun` budget. Use `-CacheWarmMode Off` for a cheap xref-only
pass, `-CacheWarmMode Incremental` for bounded background warming, and
`-CacheWarmMode Complete` only when you intentionally want to fill every missing
function in one pass.

`-AnalysisMode Auto` is the default and skips Ghidra import/analysis when the
project and analysis manifest match the current binary. `-ExportOnly` remains a
compatibility alias for `-AnalysisMode Skip`; use `-AnalysisMode Force` when you
need to rebuild the Ghidra project state.

Single-target runs now default to per-target Ghidra projects so different
binaries can be processed in parallel without fighting over the old shared
project lock. Multi-target and full-pass runs still default to the shared
project. If you only have the legacy shared project on disk, export-only Auto
mode falls back to it until the per-target project is seeded by a non-export
run.

Updated runners take a per-project gate before opening Ghidra. Concurrent
sessions using the scripts wait on that gate, and if an interactive Ghidra
session or older runner already has the project open, the script retries the
headless open instead of failing on the first `Unable to lock project` message.
Use `-ProjectLockTimeoutMinutes <minutes>` when a scheduled job should stop
waiting after a bounded period; the default is to wait until the project becomes
available.

For multi-binary refreshes, use runner-level parallelism with per-target
projects instead of one shared multi-target run:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ProjectLayout PerTarget -MaxParallel 3 -MaxDecompiledFunctions 2400
```

The runner starts one worker per target with collision-free `-RunId`,
`-SummaryPath`, and `-SkipCoverage` settings, then writes a merged summary and
refreshes the latest coverage once after all workers complete. Use
`-AnalysisMode Force -ProjectLayout PerTarget` when a target needs its split
project seeded or refreshed:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -AnalysisMode Force -ProjectLayout PerTarget -MaxParallel 3
```

`Start-DecompileBatch.ps1` remains available for older command lines, but it now
forwards cache-warm and analysis-mode parameters to the main runner.

Use `-ProjectLayout Shared` when you intentionally want the old single-project
behavior for a targeted pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ProjectLayout Shared
```

Use `-DecompileMode Force` after manual interactive Ghidra edits that are not
represented in `function_labels.csv`:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -DecompileMode Force
```

Use `-DecompileMode Skip` when the pass only needs strings, imports, and xrefs:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -DecompileMode Skip
```

Increase `-MaxDecompiledFunctions` only when the selected functions are too
shallow. If the desired function is not selected, prefer improving
`HIGH_VALUE_STRING_PATTERNS`, adding a durable label, or locating a better xref
over exporting thousands of functions.

### Optional Ghidra MCP Probe

Use `ghidra-mcp` when a static export points at the right function family but
the next question is easier in an open CodeBrowser project: datatype shape,
nearby symbols, current decompiler view, cross-reference navigation, comments,
or live debugger tracing.

Before connecting, print the project handoff from the repo root:

```powershell
.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe
```

The helper reads the latest run summary, analysis manifests, and `.gpr` files,
then prints the matching project name, program name, export folder, and MCP
sequence. Open the listed `.gpr` in Ghidra and the listed program in
CodeBrowser. In Codex, call `mcp__ghidra_mcp.list_instances`, connect to the
printed project query with `mcp__ghidra_mcp.connect_instance`, then call
`mcp__ghidra_mcp.list_tool_groups` and load the needed groups if the bridge is
lazy-loaded.

Keep the handoff boundary clean:

- Use MCP for focused inspection, tentative renames/comments, datatype review,
  and debugger traces.
- Follow `GHIDRA_MCP_PROMPT_ADAPTER.md` when applying upstream prompt workflows
  such as Function Documentation V5 or Data Type Investigation.
- Do not treat a Ghidra-only rename as durable evidence.
- Add stable names to `function_labels.csv` only after the function is mapped.
- Re-export the affected binary and confirm the names appear in `functions.csv`
  and `selected_decompiled.c`.
- Use `-DecompileMode Force` only when you intentionally need fresh output for
  interactive Ghidra edits that are not represented in `function_labels.csv`.

## Selection Criteria Reference

Functions enter the focused export in priority order:

1. `label:` reasons from `Decomp/Analysis/function_labels.csv`
2. `target:` reasons from `HIGH_VALUE_STRING_PATTERNS` in `ExportNexusForeverAnalysis.java`
3. `import:` reasons from interesting import xrefs
4. `string:` reasons from the broader keyword list

Use `selected_reasons_summary.csv` in the matching export folder to see the
exact reason set for each function, the priority bucket that won, the first xref
that pulled it in, and whether it made the current `selected_decompiled.c`
cutoff.

Use this when a focused pass misses a target function:

- Check whether a nearby anchor already selected the function through `selected_reasons_summary.csv`.
- Add a durable label before widening keyword lists when the function meaning is already mapped.
- Add a new high-value pattern only when the anchor is stable and specific enough to avoid export noise.
- Raise `-MaxDecompiledFunctions` only after labels and durable string anchors still leave the selected set too shallow.

## Export Caching And Invalidation

`ExportNexusForeverAnalysis.java` keeps `selected_decompiled.c` fast by caching
per-function fragments in a canonical full-program store under
`selected_decompiled_cache/functions/<binary-fingerprint>/`. Old context-hash
cache folders are removed by the exporter; once a function exists in the
canonical store, later runs skip re-exporting it.

`-DecompileMode Auto` is the normal choice for repeated passes:

- Reuses the existing `selected_decompiled.c` when the binary fingerprint, label state, Ghidra version, and selected function list are unchanged.
- Reuses canonical cached fragments for unchanged functions when the selected set expands or contracts.
- Writes fresh CSV and text artifacts even when decompilation is skipped.

Use `-DecompileMode Force` after interactive Ghidra edits or when you need to
throw away cached decompile output. Use `-DecompileMode Skip` when the pass only
needs strings, xrefs, imports, selection auditing, or helper-script output.

The easiest ways to understand cache behavior are:

- `selected_decompiled.manifest` in the export folder for decompile fingerprint details.
- `decompile_cache_summary.properties` for full-program cached, warmed, remaining, duplicate, and already-exported counts.
- `selected_reasons_summary.csv` for the current selection and cutoff.
- `logs/LATEST_RUN_SUMMARY.json` for the most recent headless run inputs and outputs.
- `coverage/LATEST_COVERAGE_SUMMARY.md` and `coverage/opcode_coverage_inventory.csv` for the current export and opcode backlog.

### 3. Search Exports For Anchors

Use `rg` first. Search the generated exports and source together so each native
anchor is immediately compared with server code.

```powershell
rg -n "LoginFinish|AuthType|UserId|Aliases|RoleId" Decomp\Analysis\exports Source
```

```powershell
rg -n "WorldSocket|RealmDataCenter|\.tbl|Register" Decomp\Analysis\exports Source Tools
```

```powershell
rg -n "PublicEventObjectiveType|DefendObjectiveUnits|Lua_Register" Decomp\Analysis\exports Source
```

When an anchor appears in `interesting_strings.csv`, open the matching rows in:

1. `interesting_strings.csv` for the literal string and address.
2. `string_xrefs.csv` for exact xref addresses, especially data-table refs.
3. `selected_reasons_summary.csv` for why the function stayed in the focused export and whether it is inside the current decompile cutoff.
4. `selected_call_edges.csv` for direct caller/callee follow-up when the anchor lands on a serializer, wrapper, or shared helper and you need the next step in the local call chain without reopening Ghidra.
5. `selected_xrefs.csv` for the referencing function entry when the xref is code.
6. `selected_decompiled.c` for the selected function body.
7. `functions.csv` for nearby named or unlabeled functions.
8. Source files under `Source/` for the current server behavior.

For vtable or callback-table work, search `function_pointer_families.csv` and
`function_pointer_family_slots.csv` before reopening Ghidra. Use the family
address, first named slots, thunk counts, and per-slot reference counts to pick
the right helper-script follow-up.

### 4. Map The Function

For each candidate function, create a small function map before changing code.

```markdown
### Candidate: <binary> <address>

- Proposed name:
- Current Ghidra name:
- Selected export location:
- Anchors:
- Inputs:
- Outputs:
- Constants:
- Calls out to:
- Called by:
- Matching NexusForever files:
- Confidence:
- Open questions:
```

Good mapping cues:

- Transaction or route strings, such as `/Auth/ConsumeGameToken`.
- XML or field strings, such as `GameCode`, `Token`, `ClientNetAddress`.
- Table paths, such as `DB\WorldSocket.tbl`.
- Import references, such as `send`, `recv`, `WSASend`, `select`, `connect`.
- Enum registration names and adjacent integer constants.
- Repeated child parsing loops, which often identify arrays like
  `Aliases/Alias` or `Roles/RoleId`.
- Packet writer order, especially size, opcode, payload, and bit counts.
- Shared helper calls from multiple higher-level functions.

Avoid over-naming helper functions too early. A name like
`StsConn_ParseRoles` is good once repeated `RoleId` nodes are proven. A name
like `CryptoHandshake_FinaliseRetailSecureLogin` is too strong unless every
part of that claim is mapped.

### 5. Update Mapping Artifacts

When the pass produces reusable evidence, update the relevant CSV under
`Decomp/Analysis/mapping_artifacts` before or alongside findings updates:

| Layer | File |
| --- | --- |
| Native opcode provenance | `native_opcode_provenance.csv` |
| Function evidence | `function_evidence_inventory.csv` |
| Structure and offset maps | `structure_offset_maps.csv` |
| Dispatcher or registration rows | `dispatcher_registration_exports.csv` |
| Native-to-source traceability | `source_traceability_map.csv` |
| Live-client capture plan or result | `live_evidence_bundle_manifest.csv` |
| Packet fixture backlog | `packet_fixture_plan.csv` |

Use these ledgers for compact facts: function labels, addresses, opcodes, field
order, bit widths, source paths, confidence state, and blockers. Keep longer
explanations in `INITIAL_FINDINGS.md` or a focused tracker, then point the CSV
row at that finding.

Validate the ledgers after edits:

```powershell
.\Decomp\Analysis\Validate-DecompileMappingArtifacts.ps1
```

When working toward full seven-layer coverage, run the gap audit:

```powershell
.\Decomp\Analysis\Get-DecompileMappingArtifactGaps.ps1
```

Use `-FailOnGap` only when checking whether the full seven-layer map is complete.
Use `Update-FunctionEvidenceInventoryFromLabels.ps1` to backfill mapped
function-evidence rows from durable `function_labels.csv` entries before doing
manual source-correlation passes.
Use `Update-NativeOpcodeProvenanceFromCoverage.ps1` to add conservative
coverage-derived opcode provenance rows, then refine individual rows with exact
native registration and payload functions during focused passes.
Use `Update-SourceTraceabilityFromOpcodeProvenance.ps1` and
`Update-LiveEvidenceManifestFromBlockedProvenance.ps1` to keep source and
live-evidence ledgers aligned after opcode provenance backfills.

### 6. Add Durable Labels

Add labels to `Decomp/Analysis/function_labels.csv` after the function is mapped
well enough to be useful in future exports.

CSV format:

```csv
program,address,name,comment
WildStar64.exe,140000000,Subsystem_ActionObject,Short evidence-backed description.
```

Naming convention:

| Prefix | Use |
| --- | --- |
| `StsConn_` | STS transaction, XML, auth, token, or account parsing behavior. |
| `StsInetSocket_` | STS socket envelope, connect, dispatch, and socket crypto edge behavior. |
| `Network_` | Shared world network message, packet, endpoint, or diagnostic helpers. |
| `NetworkSocket_` | Select/non-blocking socket path. |
| `NetworkIOCP_` | Overlapped or IOCP-style socket path. |
| `ClientDB_` | Client database table registration and loader functions. |
| `Lua_` | Lua binding or enum registration functions. |
| `<System>_Parse...` | Data parser or response parser. |
| `<System>_Send...` | Request or packet builder. |
| `<System>_Register...` | Registration table, enum, handler, or script binding. |

After editing labels, re-export to confirm the label applies:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets <binary> -MaxDecompiledFunctions 350
```

Then check:

```powershell
rg -n "<NewFunctionName>" Decomp\Analysis\exports\<binary>\functions.csv Decomp\Analysis\exports\<binary>\selected_decompiled.c
```

If the script reports "No function at address", open the function in Ghidra or
check `functions.csv`; the address may be inside a thunk or a containing
function. Use the real function entry when possible.

### 7. Update Export Selection When Needed

If a target string is repeatedly useful but not exported as interesting, add it
to `HIGH_VALUE_STRING_PATTERNS` in
`scripts/ExportNexusForeverAnalysis.java`.

Good high-value patterns are stable field, route, enum, table, or subsystem
names:

```java
"consumegametoken", "clientnetaddress", "serverpublickey"
```

Avoid broad patterns that explode selection noise:

```java
"id", "data", "value", "manager"
```

Re-export and confirm the pattern shows up in `interesting_strings.csv` and
`string_xrefs.csv`, and selects the expected code xrefs when the reference is
inside a function body.

### 7. Implement Conservatively

Implementation should happen only after the function map reaches at least
`Mapped`, and preferably `Verified`.

Before editing code, identify:

- The current NexusForever source files that own the behavior.
- Whether the server already has a partial implementation.
- Whether the client behavior is request parsing, response writing, enum value,
  state mutation, packet framing, or diagnostics.
- The minimal server change needed to match the proven behavior.
- The focused verification command or manual runtime check.

Common implementation surfaces:

| Native evidence | NexusForever surface |
| --- | --- |
| STS request fields | `Source/NexusForever.Network.Sts/Model/*Message.cs` |
| STS response fields | `Source/NexusForever.Network.Sts/Model/*Response.cs` |
| STS transaction behavior | `Source/NexusForever.StsServer/Network/Message/Handler/*.cs` |
| World packet framing | `Source/NexusForever.Network/Packet` and `Source/NexusForever.Network/Session` |
| World opcodes and messages | `Source/NexusForever.Network.World/Message` |
| Public-event constants | `Source/NexusForever.Game.Static/PublicEvent` |
| Client DB table meaning | `Source/NexusForever.GameTable`, `Tools/DataMapping`, and matching runtime managers |
| Spell effect payloads | `Source/NexusForever.Game/Spell` and `Source/NexusForever.Game.Abstract` |
| Action set, PvP, interaction, and player runtime state | `Source/NexusForever.Game`, `Source/NexusForever.WorldServer/Network/Message/Handler` |
| Crafting, marketplace, support, friendship, ICComm, trade, options, and other world request clusters | `Source/NexusForever.Network.World/Message/Model`, `Source/NexusForever.WorldServer/Network/Message/Handler`, and matching game services |
| Expedition or content behavior | `Source/NexusForever.Script.Instance` |

Keep uncertain values surfaced through diagnostics or comments in the tracker,
not through server mutations. For example, an unknown `DataBits03` should stay
recorded in diagnostics until it has a known behavior.

### 8. Verify The Change

Use the narrowest verification that proves the implemented behavior.

Typical checks:

```powershell
dotnet build Source\NexusForever.slnx --no-restore
```

Focused source search:

```powershell
rg -n "<field-or-function-name>" Source Decomp\Analysis
```

STS behavior:

- Confirm message model read/write order matches the mapped client parser or
  builder.
- Confirm handler state changes do not depend on fields the client does not
  send.
- Confirm failure cases still produce existing errors.

Packet behavior:

- Confirm writer field sizes match the client bit or byte widths.
- Confirm size and opcode fields match the client framing.
- Confirm logging does not truncate or misformat opcodes.

Spell or gameplay behavior:

- Inspect with `/spell inspect4 <spell4Id>`.
- Cast with `/spell cast4 <spell4Id>` in a controlled local session.
- Compare diagnostics, combat logs, packets, entity state, and table row
  interpretation.

### 9. Record The Result

Update `INITIAL_FINDINGS.md` or the focused tracker with:

- New labels added.
- Evidence location in `selected_decompiled.c`.
- Field order, constants, or enum values mapped.
- NexusForever files changed.
- Verification command and result.
- Anything intentionally left unimplemented.

Use this compact format:

```markdown
Follow-up implemented from this pass:

- `<FunctionName>` at `<address>` writes/reads `<field list>` around
  `exports\<binary>\selected_decompiled.c:<line>`.
- NexusForever now `<short implementation summary>`.
- `<Unknown field or endpoint>` remains blocked because `<specific missing evidence>`.
```

## Current Map

This map is a state ledger. It summarizes the known high-value areas from
`INITIAL_FINDINGS.md` so the next pass can start from known terrain and avoid
re-mapping packet surfaces that are already closed.

| Area | Binary | Durable labels or anchors | NexusForever surfaces | State | Next useful work |
| --- | --- | --- | --- | --- | --- |
| STS auth transactions | `StsConnLib64.MT.dll` | `StsConn_SendLoginStart`, `StsConn_SendLoginFinish`, `StsConn_OnLoginFinishResponse`, `StsConn_SendRequestGameToken`, `StsConn_SendConsumeGameToken`, `StsConn_OnConsumeGameTokenResponse` | `NexusForever.Network.Sts/Model`, `NexusForever.StsServer/Network/Message/Handler` | Implemented for login/account/game-token compatibility. | Continue unmapped auth/token/external-account routes only when a local client flow needs them. |
| STS connect envelope | `StsConnLib64.MT.dll` | `StsInetSocket_ParseConnectFields`, `StsInetSocket_SendConnectEnvelope`, `StsInetSocket_DispatchConnectEnvelope` | `ClientConnectMessage`, STS session setup | Implemented for known connect/login-start fields. | Validate remaining optional envelope fields against observed startup before changing session state. |
| Token crypto handshake | `StsConnLib64.MT.dll` | `StsConn_SendTokenKeyData`, `ServerRand`, `ServerPublicKey`, `ServerSignature`, `PremasterSecret`, `AuthnToken` | STS auth models and handlers | Mapped only / blocked. | Keep blocked until the crypto/token-login semantics are understood safely. Do not guess crypto behavior. |
| World framing and bit IO | `WildStar64.exe` | `Network_SendMessageById`, `Network_SendResolvedMessage`, `Network_SerialiseResolvedMessage`, bit reader/writer labels | `NexusForever.Network/Packet`, `NexusForever.Network/Session`, world messages | Implemented where packet size/opcode/bit order differed or needed confirmation. | Continue into receive/framing/dispatch only if a client-server packet mismatch appears. |
| Socket driver behavior | `WildStar64.exe` | `NetworkSocket_*`, `NetworkIOCP_*`, `Network_ParseEndpointAddress` | `NetworkSession`, connection setup | Implemented only for low-risk compatibility such as `TCP_NODELAY`; mostly mapped diagnostics. | Use for compatibility and diagnostics, not large rewrites without a runtime issue. |
| Client DB registration | `WildStar64.exe`, `Houston64.exe` | `ClientDB_RegisterRealmDataCenter`, `ClientDB_RegisterWorldSocket`, `ClientDB_RegisterPublicEventUnitPropertyModifier`, table path strings | `GameTable`, `Tools/DataMapping`, runtime managers | Mapped table-registration pattern. | Label more table registrations when a table is needed for data mapping or runtime behavior. |
| Public-event constants | `WildStar64.exe` | `Lua_RegisterPublicEventConstants`, `PublicEventObjectiveType_*`, status/category/notification strings | `Game.Static/PublicEvent`, public event scripts | Implemented for confirmed enum values and aliases. | Extend only when a public-event objective flag/notification behavior needs server logic. |
| Game.Spell Lua and spell metadata | `WildStar64.exe` | `Lua_RegisterGameSpellBindings`, 46 `Lua_GameSpell_*` labels, `Lua_InsertStringIntPair`, spell enum strings | `Game.Static/Spell`, `Game/Spell`, `/spell inspect`, data mapping outputs | Implemented for metadata/diagnostics; several runtime behaviors remain blocked. | Continue family-by-family: prove a concrete `Spell4Effects` consumer before mutating spell runtime. |
| Action set and LAS requests | `WildStar64.exe` | `Lua_RegisterActionSetLib`, `Lua_ActionSetLib_RequestActionSetChanges`, `ActionSet_ValidateRequestedChanges`, `ActionSet_CheckPvPRestriction`, `ActionSet_CheckUpdateSpellInProgress` | `Game/Spell`, `WorldServer/Network/Message/Handler/Spell` | Implemented for 12-slot LAS size, active spec, tier, AMP, PvP/in-combat/dead preflight, and cache refresh. | `UpdateSpellInProgress` remains blocked until a real asynchronous spell-update transaction model is found or introduced; do not add a synthetic boolean gate. |
| Rapid transport and taxi/flight path | `WildStar64.exe` | `ClientRapidTransport_WritePayload`, `RapidTransport_SendClientRapidTransport`, `Map_BuildRapidTransportNodeInfoTable`, `Map_GetRapidTransport*`, `ClientFlightPathPurchase_WritePayload`, `FlightPath_SendPurchaseRequest` | `WorldServer/Network/Message/Handler/PlayerPath`, `Game/Entity/PathManager`, `GameTable` | Implemented for validated receive boundaries, pricing evidence, and read-only treatment of the second `ClientRapidTransport` field as `ContextToken`; transport movement/payment is partial. | Map service-token bypass selection, taxi embark, route state, and cast-result correlation before using `ContextToken` for validation or charging/teleporting beyond proven behavior. |
| Crafting and tradeskill requests | `WildStar64.exe` | `ClientCrafting*WritePayload`, `Crafting_SendClient*`, `Tradeskill_SendClientTradeskill*`, `ServerCraftingCurrentCraft_ReadPayload`, `Crafting_HandleServerCraftingCurrentCraft`, `Lua_RegisterCraftingBindings` | `Network.World/Message/Model/Crafting`, `WorldServer/Network/Message/Handler/Crafting`, `GameTable` | Implemented for packet layouts, table validation, conservative fixed-recipe success, station validation, profession persistence, and sigil result value names. | Retail parity remains blocked on discovery roll/unlock semantics, exact station service-key meanings, complex craft stats, broader material-source precision, durable rune item data, non-success sigil result rules, and aux emit intent. |
| Rune crafting requests | `WildStar64.exe` | `ClientCraftingRuneSlot*WritePayload`, `RuneCrafting_SendClientRune*`, `CodeEnumTradeskillResult` | `Network.World/Message/Model/Crafting`, `WorldServer/Network/Message/Handler/Crafting` | Implemented as validated request parsing/runtime-only mutation with native sigil result values. | Durable item rune data semantics and non-success result-rule precision remain blocked. |
| Marketplace request state | `WildStar64.exe` | `Marketplace_*WritePayload`, `Marketplace_SendClient*` auction and commodity labels | `WorldServer/Network/Message/Handler/Marketplace`, marketplace message models | Implemented with deterministic empty/failure responses; payloads and validation are mapped. | Real auction/commodity persistence, search state, bid/post/cancel mutation, and mail settlement remain blocked. |
| Storefront catalog and purchase state | `WildStar64.exe` | `Storefront_RequestCatalog_WritePayload`, `Storefront_PurchaseCharacter_WritePayload`, `Storefront_PurchaseAccount_WritePayload`, `NetworkBitWriter_WriteIdentity` | `Game/Storefront`, `Network.World/Message/Model`, `WorldServer/Network/Message/Handler/Account` | Implemented for catalog send, mapped purchase packet parsing, local-target validation, account-currency charging, and account-inventory item creation. | Gifting, cooldowns, exact purchase result UI, and unsupported offer item types remain blocked pending native packet/result mapping. |
| Account inventory and account-item claim | `WildStar64.exe` | `ServerAccountItems_ReadPayload`, `ServerAccountItemCooldownSet_ReadPayload`, `ServerAccountItemsPending_ReadPayload`, `AccountInventoryItem_ReadPayload`, `AccountItem_SendClientAccountItemTake`, `AccountItem_SendClientClaimPendingItemGroup` | `Database.Auth`, `Game/Account/Inventory`, `Network.World/Message/Model`, `WorldServer/Network/Message/Handler/Account` | Implemented for persisted account inventory list/add, claim-state enum, `ClientAccountItemTake`, and supported `AccountItem` grant paths. | Pending account item groups, gift delivery, cooldown mutation, and unmapped account item effects remain blocked. |
| P2P trading request state | `WildStar64.exe` | `Trade_SendClientP2PTrading*`, `ClientP2PTradingUInt64_WritePayload`, shared zero-payload and raw field writers | `WorldServer/Network/Message/Handler/Trade`, `Game/Trade/TradeManager`, `Network.World/Message/Model` | Implemented for transient invite/session/offer/commit/settlement using existing item/currency APIs. | Remaining precision: trade locks against concurrent inventory changes, exact eligibility/money-limit rules, unknown item-update fields, `ServerP2PTradeResult.Cancelled` semantics, and sniff/client UI verification. |
| Duel/PvP request state | `WildStar64.exe` | `Lua_GameLib_InitiateDuel`, `Lua_GameLib_AcceptDuel`, `Lua_GameLib_DeclineDuel`, `Lua_GameLib_ForfeitDuel`, `Lua_GameLib_SetIgnoreDuelRequests`, `Lua_GameLib_TogglePvpFlags`, shared zero-payload and bool writers | `WorldServer/Network/Message/Handler/Pvp`, `Game/Pvp/DuelManager`, `Game/Entity/Player` | Implemented for safe transient duel lifecycle, runtime PvP toggle, and durable pending toggle-off cooldown persistence. | Remaining precision: observer broadcasts, duel-area leash/warnings, countdown duration, forced-map PvP, and duel reward/stat effects. |
| Support/report/survey requests | `WildStar64.exe` | `ClientIncidentReport_WritePayload`, `ClientSupportTicket_WritePayload`, `ClientReportBug_WritePayload`, `ClientStuck_WritePayload`, `ClientSuggest_WritePayload`, `ClientCustomerSurveySubmit_WritePayload` | `WorldServer/Network/Message/Handler/Support`, `WorldServer/Network/Message/Handler/Misc` | Implemented as parsed diagnostics and deterministic ticket result/failure where safe. | Real support-ticket/report/survey persistence or moderation workflows remain blocked until a backing service is designed. |
| Options and combat-log preferences | `WildStar64.exe` | `ClientCombatOptions_WritePayload`, `Options_SendClientOptionsCasting`, `ClientOptions`, combat-log one-uint writers | `WorldServer/Network/Message/Handler/Option`, option message models | Implemented as validated parsing/logging; no durable store yet. | Add a small account/character option store before using these preferences to filter outbound combat logs. |
| Friendship request state | `WildStar64.exe` | `Lua_FriendshipLib_*`, `ClientFriendship*WritePayload`, `NetworkBitWriter_WriteWideString`, `NetworkBitWriter_Write4BitValue` | Friendship server handlers, `WorldServer/Network/Message/Handler/Friendship` | Implemented for account block, remove-by-name, note-by-name, and transient ignore-strangers; auto-response diagnostic-only. | Persist ignore-strangers if needed; auto-response needs account/chat storage and delivery semantics. |
| ICComm request state | `WildStar64.exe` | `ClientICCommChannelJoin_WritePayload`, `ClientICCommMessage_WritePayload`, `ClientP2PTradingUInt64_WritePayload` | `WorldServer/Network/Message/Handler/ICComm`, `Game/ICComm/ICCommManager`, `Network.World/Message/Model/ICComm` | Implemented for transient global/group/guild channel membership, join results, message result echo, directed delivery, ordered sender echo, and not-joined cleanup. | Remaining precision: entitlement checks, exact directed-vs-ordered delivery semantics, persistent membership, explicit leave/logout UI behavior, throttling, and guild/group lifecycle hooks. |
| Interaction, CSI, busy, and target feedback | `WildStar64.exe` | `CSIKey_HandlePress`, `CSIAction_*`, `Interaction_AttemptTargetAction`, `Interaction_HandlePostActionTargetFeedback`, `DeferredActionQueue_*`, `Loot_CheckInteractionRange` | `WorldServer` entity interaction handlers, `Game/Entity/UnitEntity`, scripts | Implemented for busy target gating and visible busy state; broader client interaction flow is mapped in labels. | Deeper deferred-action/current-object/current-target semantics need sniffs or stronger table/callback evidence before server mutation. |
| Loot interaction and bindcheck | `WildStar64.exe` | `Loot_PrepareAndDispatchBindcheck`, `Loot_SendClientLootItemCollect`, `Loot_HandlePendingLootInteract`, `Loot_DispatchBindcheckEvent` | Loot handlers, item/loot runtime | Mapped/partially implemented at request and diagnostic boundaries. | Export-only revalidation and real loot assignment/roll/master-loot state remain open. |
| Vital modifier and SapVital | `WildStar64.exe` | `CombatLog_DispatchVitalModifierEvent`, `Lua_InsertStringIntPair`, `SapVital`, `VitalModifier`, spell-effect enum block | `Game/Spell`, `Game/Entity/UnitEntity`, combat logs | Implemented for conservative health vital source/damage-type handling; enum-registration evidence improved. | Direct gameplay consumer for `SapVital` and unsupported alias vitals remain blocked. |
| Item visual, disguise, scale, shield/vital spell effects | `WildStar64.exe` | Spell-effect name block and targeted runtime helper labels | `Game/Spell`, `Game/Entity/UnitEntity`, diagnostics | Implemented incrementally where effect ownership/restoration semantics were proven. | Continue with one effect family at a time; keep unknown `DataBits` diagnostic-only. |
| Audio runtime and selector mapping | `WildStar64.exe` | `AudioOutput_*`, `AudioSelector_*`, `AudioRuntime*`, `AudioResampler_*` | Findings/labels only | Mapped only. | Use as helper/context evidence for client callback families; do not implement server audio behavior. |

### Current State Precision

Use the current map as a state ledger, not just a list of decompile anchors:

- **Implemented** means NexusForever now has code that handles the mapped client
  surface, even when the safe behavior is an empty result or deterministic
  failure because a backing service does not exist.
- **Mapped only / blocked** means the packet or function shape is known, but the
  server-side state boundary is still missing. Do not add mutation just because
  the request fields are known.
- For request clusters, record three separate layers: packet shape, local client
  sender guard/state, and server response/state mutation. Most remaining gaps
  are in the third layer.
- When `INITIAL_FINDINGS.md` says a handler returns an empty result or
  deterministic failure, treat that as an implemented compatibility boundary,
  not as full feature support.

### Good Next Targets

Prefer one of these shapes when choosing a new pass:

- A blocked state machine or backing service with fully mapped packets, such as
  marketplace persistence, crafting success, or support-ticket storage. P2P
  trade and ICComm now have transient runtime implementations; return there
  only for precision gaps such as offer locks, entitlement checks, message
  delivery packet choice, item eligibility, money limits, or result/update-field
  verification.
- A narrow runtime precision gap inside an implemented cluster, such as duel
  observer broadcasts, PvP cooldowns, rapid-transport service-token/route
  selection, or a real server-side spell-update transaction model for
  `UpdateSpellInProgress`.
- One spell-effect family with table rows, diagnostics, and at least one client
  consumer anchor. Avoid broad `Spell4Effects` sweeps.
- A client DB registration needed by a concrete source or data-mapping task.

## Prompt Guide

These prompts are designed for future AI-assisted passes. Paste only the prompt
that matches the current stage. Keep the target narrow and include the current
state from this guide.

### Session Primer

Use this when starting a new decompile continuation session:

```text
You are working in NexusForever. Continue the client binary reverse-engineering
effort using Decomp/Analysis/CONTINUATION_GUIDE.md as the workflow.

Read:
- Decomp/Analysis/README.md
- Decomp/Analysis/CONTINUATION_GUIDE.md
- Decomp/Analysis/INITIAL_FINDINGS.md
- Decomp/Analysis/function_labels.csv
- Decomp/Analysis/GHIDRA_MCP_PROMPT_ADAPTER.md when using ghidra-mcp
- Decomp/Analysis/DATA_TYPE_INVESTIGATION_WORKFLOW.md when structure or
  parameter types are in scope

For an interactive Ghidra pass, first run:

.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets <binary>

Then open the printed Ghidra project, use ghidra-mcp list_instances,
connect_instance, and list_tool_groups, and keep durable labels in
function_labels.csv.

Do not touch Decomp/Client64, generated exports, logs, or Ghidra project files
except by running the existing scripts. Do not copy decompiled client source into
the repository. Summarize behavior with addresses, labels, field names, and short
line references.

Target for this pass:
<one narrow target question>

Deliver:
- mapped evidence
- labels to add, if justified
- implementation changes only for verified behavior
- updated findings/tracker notes
- verification result
```

### Discovery Prompt

Use this before labels or implementation:

```text
Investigate <target subsystem or field> in the existing Ghidra exports.

Use rg over Decomp/Analysis/exports, Decomp/Analysis/function_labels.csv, and
Source. Identify the smallest set of candidate functions. For each candidate,
produce:
- binary and address
- current function name
- selected_decompiled.c location
- strings/imports/constants that anchor it
- likely inputs and outputs
- matching NexusForever source files
- confidence level using Observed/Correlated/Mapped/Verified/Blocked

Do not edit code yet. End with the recommended next labels and the minimum
evidence still needed before implementation.
```

### Mapping Prompt

Use this once candidate functions are known:

```text
Map <function address or label> in <binary> into a durable NexusForever finding.

Read the matching selected_decompiled.c section, selected_xrefs.csv rows,
interesting_strings.csv rows, function_labels.csv, and matching Source files.

Produce a function map with:
- proposed stable label
- evidence-backed purpose
- field order or constant mapping
- caller/callee relationships if visible
- current server behavior
- recommended implementation, diagnostic-only handling, or blocked decision

If the label is justified, update Decomp/Analysis/function_labels.csv and
re-export only <binary> to verify the label is applied.
```

### Label Prompt

Use this when the mapping is solid but no server change is safe yet:

```text
Add durable labels for the mapped <subsystem> functions only.

Rules:
- edit Decomp/Analysis/function_labels.csv
- use existing naming prefixes from CONTINUATION_GUIDE.md
- keep comments factual and short
- run an export-only Ghidra pass for the affected binary
- verify each new label appears in functions.csv and selected_decompiled.c
- update INITIAL_FINDINGS.md with what was labelled and why no implementation
  was added yet
```

### Implementation Prompt

Use this after a target reaches `Mapped` or `Verified`:

```text
Implement the verified NexusForever behavior for <target>.

Evidence:
- <binary> <function label/address>
- <selected_decompiled.c short location>
- <field/constant behavior summary>
- <matching source files>

Constraints:
- preserve existing project patterns
- implement only evidence-backed behavior
- leave unknown fields diagnostic-only or blocked in the tracker
- update INITIAL_FINDINGS.md or the focused tracker with the exact follow-up
- run the narrowest build or test command that proves the change

Deliver a concise summary of files changed, behavior implemented, labels added,
and verification result.
```

### Review Prompt

Use this after a mapping or implementation pass:

```text
Review the decompile continuation changes as a code/evidence review.

Check:
- labels are evidence-backed and use stable names
- findings cite enough location information to be rediscovered
- implementation does not go beyond mapped behavior
- generated artifacts or client binaries were not committed
- uncertain fields remain diagnostic-only or blocked
- verification is adequate for the behavior changed

Lead with bugs, risks, missing evidence, or missing tests. If no issues are
found, state the remaining residual risk.
```

## Implementation Checklist

Before editing source:

- [ ] Target question is narrow.
- [ ] Relevant exports are current enough.
- [ ] Candidate function map exists.
- [ ] Evidence confidence is at least `Mapped`.
- [ ] Matching NexusForever source surface is identified.
- [ ] Unknown fields are explicitly listed.

Before adding labels:

- [ ] Address is the function entry or a verified containing function.
- [ ] Name describes proven behavior.
- [ ] Comment is factual and short.
- [ ] Any MCP/Ghidra-only rename worth keeping is mirrored to `function_labels.csv`.
- [ ] Export-only run applies the label.
- [ ] Label appears in generated exports.

Before finishing a mapping-only pass:

- [ ] Relevant `mapping_artifacts/*.csv` rows were added or intentionally left unchanged.
- [ ] `Validate-DecompileMappingArtifacts.ps1` passes.
- [ ] Longer evidence is recorded in `INITIAL_FINDINGS.md` or a focused tracker when the CSV row cannot stand alone.
- [ ] Source traceability, fixture backlog, and live evidence blockers are explicit when they apply.

Before changing Ghidra datatypes:

- [ ] Generic pointer parameters were investigated across all related functions.
- [ ] Offset map records access size, access mode, and evidence source.
- [ ] Existing structures were searched before creating or renaming a type.
- [ ] Type application was verified across the whole mapped function family.

Before finalizing implementation:

- [ ] Source changes are minimal and local to the owning subsystem.
- [ ] No generated decompile output was copied into source.
- [ ] Diagnostics cover unresolved fields when useful.
- [ ] Findings/tracker updated.
- [ ] Build or focused verification completed.
- [ ] Any blocked behavior is named with the missing evidence.

## Common Pitfalls

- Implementing from a string match alone. A string proves an anchor, not the
  whole behavior.
- Treating a decompiler variable name as meaningful. Prefer field strings,
  constants, call order, and server-side correlations.
- Naming helpers too specifically before callers are understood.
- Broadening export patterns until every function is selected. Add narrow
  high-value patterns or labels instead.
- Running parallel workers against the shared Ghidra project or shared latest
  summary. Use `Start-DecompileBatch.ps1` or give each worker its own `-RunId`
  and `-SummaryPath`. The runner now waits on per-project Ghidra gates, but
  per-run summaries are still cleaner for parallel automation.
- Leaving an MCP or interactive Ghidra rename only in the local project. If the
  name is evidence-backed, mirror it to `function_labels.csv`; if it is still a
  hypothesis, keep it out of source-controlled labels.
- Mutating spell state from unknown `DataBits` because the value "looks like"
  an id. Prove the id space and behavior first.
- Implementing crypto, anti-tamper, or token behavior from partial structure.
  Keep these blocked until semantics are clear and locally testable.
- Forgetting to update findings. The next pass depends on the written trail more
  than on the current Ghidra project.

## Definition Of Done For A Pass

A decompile continuation pass is done when it leaves the repository in one of
these states:

- **Mapped only:** labels and findings were updated, no server behavior was safe
  to change, and the blocker is explicit.
- **Implemented:** labels/findings were updated, NexusForever behavior changed,
  verification ran, and remaining uncertainty is documented.
- **Rejected:** the evidence did not support the target hypothesis, the rejected
  mapping is recorded briefly, and no speculative changes remain.

Do not leave a pass with only local Ghidra renames, unrecorded export knowledge,
or implementation changes that are not tied back to the evidence.
