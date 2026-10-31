# Account wallet operations and durable save settlement

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

Earlier checkpoint10126: [Contract claims through the existing session actor queue](CONTRACT_CLAIM_ACTOR_QUEUE.md)
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

Earlier checkpoint10095: [Contract credit ownership and actor publication](CONTRACT_CREDIT_ACTOR_RESERVATION.md)
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

Earlier checkpoint10068: [Character currency persistence and frozen normal saves](CHARACTER_CURRENCY_DURABLE_SAVE.md)
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

Earlier checkpoint10042: [Contract claim transactions and durable account handoff](CONTRACT_CLAIM_TRANSACTION_AND_ACCOUNT_HANDOFF.md)
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

The following records the earlier9995 wallet checkpoint.

Checkpoint9995 fixes ordinary account-currency persistence, a prerequisite for the
remaining Contract compensation handoff. The full build has zero warnings/errors;
**9,995 full tests**,44 new/101 focused,1,327 MySQL cases, four independent MySQL
race/retry probes, four provider model gates and31 compatibility probes pass.
The existing Character database binary is unchanged from9951.

The complete Contract claim aggregate, Character credit enrollment, Auth outbox/
receiver, authoritative actor publication and reset remain required. This pass
implements the wallet persistence primitive they need; it enables no public claim
or compensation producer. Current source/migrations stay undeployed.

## Reproduced defects

The earlier AccountCurrency.Save wrote the actor's cached absolute amount and
cleared dirty state before SQL settlement. A concurrent credit could disappear;
failure could leave the actor with no operation to retry.

`OldWalletProbe2` and `NewWalletProbe2` run identical real MySQL scenarios against
separately frozen9951/current assemblies. A cached100 wallet earns10 after an
external credit raises SQL to107: the old implementation overwrites it with110;
the new one retains both credits and commits117. In a separate explicit transaction,
an owned before-commit failure rolls back the10 credit. The old retry leaves SQL
at100; the corrected retry retains its operation and commits110. Both successful
probe results bind the loaded binaries and retain fresh proof schemas.

The first old probe tried to intercept an implicit single UPDATE; MySQL executed
that statement without an outer transaction, so the intended commit hook did not
fire. That failed fixture, its source and schema remain retained. The second pair
uses an explicit transaction on both implementations and proves the actual defect.

## Append-only operations and the existing Auth owner

AccountCurrency now retains ordered immutable credit/debit operations with stable
keys. Save captures a batch without acknowledging it. The existing Auth save-work
boundary stages that batch while holding the account's established durable lock.
The account pending-ID allocator is only used to acquire that existing owner; its
common save-unit receipt remains an internal witness, not a client item grant.

`account_currency_operation` stores a globally unique operation key, account and
currency owner, amount/direction, complete before/after arithmetic and source
bytes. Duplicate keys validate those operands and cannot become another account's
or currency's award. Already applied operations are observed without replaying;
subsequent legitimate wallet changes are retained. The journal survives account
removal, so recreating an owner cannot reopen an old operation. This is independent
of the Loyalty earning ledger and creates no points or entitlement grants.

MySQL uses current locking reads for the wallet and operation receipts. Merely
locking the account is insufficient if an outer repeatable-read transaction already
has a consistent snapshot. SQL commit guards verify exact tracked identities,
original keys, durable wallet values and every new/replayed receipt. Missing or
altered wallet/receipt writes reject the whole unit.

MySQL `20261031050655_AccountCurrencyOperations` and SQLite
`20261031051048_AccountCurrencyOperations` add only the empty journal and its owner
index. Existing currency models, balances and columns are unchanged. No history
is inferred from a cached balance, and no migration backfills an operation.

## Settlement and compatibility

Only terminal commit acknowledges the captured operations. Rollback, disposal,
failed flush and unknown acknowledgement preserve their keys for a fresh context.
A pending registration rejects a competing context. It may refresh its snapshot
before staging; after staging, later actor intent belongs to a newer save.

Committed SQL becomes the wallet baseline and later unacknowledged operations
remain visible/dirty. Arithmetic stays checked. If a spend arriving after the final
guard cannot reconcile with the committed baseline, the wallet requires reload
and the captured session is disconnected; that spend is never acknowledged or
allowed to overwrite SQL. Cleanup disables change detection while detaching owned
rows, so invalid original keys cannot interrupt terminal settlement of other Auth
owners. The database identity is retained without a password and survives MySQL
connection opening/password redaction.

Cosmic currency with a Loyalty owner still uses that owner's established source/
earning journal. All16 existing currency kinds retain their normal arithmetic;
wallet operations do not grant items or change entitlement defaults. This component
adopts balance state at settlement but sends no grant packets from SQL callbacks.
The connected compensation owner must serialize/reconcile the live account and
publish its authoritative wallet state before acknowledging the claim.

## Verification

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/account-wallet`.

- `full-build3.log`, `full3/full.trx` and `focused4/focused.trx`:9,995 full/101
  focused, zero failed/skipped; every9,951 earlier test remains present. The44 new
  cases comprise41 wallet regressions and three provider/schema cases. The focused
  pass also includes the existing currency manager, Loyalty and12 login-harness
  cases. An earlier full run exposed two new rollback-cleanup failures and one
  login timeout; the corrected focused and final full runs pass. Their logs remain.
- `AuthProof/result.json`:41 exact-source MySQL cases plus33 configured retry
  cases, each with a hashed SQL snapshot. Four independent probes verify a current
  wallet after an older repeatable-read snapshot, a late already-applied receipt,
  parallel actors preserving both credits and configured retry after partial
  wallet staging. These use fresh schema `nexus_assessment_wallet_auth_31a1`.
- `final-review/MySqlProof/result.json`:all1,253 prior Character cases pass again,
  including216 configured retry cases. The combined case count is1,327; the four
  independent Auth probes and before/after probes are reported separately.
- `AuthUpgradeProof/result.json`:30 existing tables/145 logical rows retain their
  complete column/row projections through the new migration and replay. Existing
  ordinary/Loyalty wallets, pending/inventory rows and15:1/17:1 values remain.
  Only the empty journal is added; current model column coverage is complete.
  This is a populated synthetic upgrade, not a copied runtime/retail upgrade.
- Four frozen model gates cover Character/Auth on MySQL/SQLite;31 upstream
  compatibility probes pass. The unchanged Character DLL binds9951's retained
  legacy60-table/68-row and selected61-table/69-row populated upgrade proofs.
- `final-review` freezes5,853 Source/load-tool files,14 changed paths since9951,
  169 DLLs and39 copied provider case/helper sources. `AwardSourceAudit` rechecks
  all726 actual-table Contract quotes:486 Item2,240 Auth currency choices and six
  final credit awards. It grants nothing.
- Read-only preservation retains the8378 runtime's nine process identities, six
  drained queues, original77 tables/21,989 rows,43 copied legacy characters,
  accepted Contract, private configurations/schema and defaults15:1/17:1/no44.
  Computer Use/native remain stopped. No commit/push or runtime staging occurred.
  Content validation stays31 CSVs/167,484 rows, all `not_retail_complete`.

## Continue the full Goal

Connect canonical Contract claims to the complete Character transaction, validated
claimed-state adoption and final credit before/after ownership. Add the durable
Character outbox and stable Auth compensation operation using this journal, with
crash/relog/transfer recovery and authoritative actor publication. Implement the
explicit Contract reset/carry policy, then actual delivery/full-table/copied-upgrade/
socket/deployment qualification. Track35, all broader assessment/upstream/AMP/combat/
content work and native UI gates remain. Q5597's unavailable historical bundle is
not requested again; the full Goal stays active.
