# Contract receipt and inventory enrollment

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

Earlier checkpoint9995: [Account wallet operations and durable save settlement](ACCOUNT_CURRENCY_OPERATION_PERSISTENCE.md)
qualifies checkpoint9995: **9,995 full tests**,44 new/101 focused,1,327 MySQL cases,
four MySQL race/retry probes, four provider model gates and31 compatibility probes,
with zero build warnings/errors. Wallet operations retain dirty intent across
rollback/unknown commits and preserve concurrent credits. Frozen9951/current probes
reproduce and repair both failures. Current MySQL locking reads and durable SQL
witnesses protect the wallet/receipt unit; existing Loyalty ownership remains.
The additive Auth journal upgrade preserves30 old tables/145 logical rows; the
Character DLL and its9951 upgrade proofs remain unchanged. This is the persistence
prerequisite for pending Contract/Auth handoff, not an enabled claim producer.
Full aggregate/claimed-state/currency/outbox/reset/actor publication and delivery/
socket/deployment/native gates, plus the entire broader Goal, remain. The8378
backend,9 identities,6 drained queues, originals/config/schema and defaults
15:1/17:1/no44 stay preserved; source/migrations remain unstaged. Computer Use stays
stopped, and the unavailable Q5597 bundle is not requested again.

Checkpoint9951 adds Contract-owned item enrollment to the existing inventory
reservation and fixes shared SQL witness gaps. The full solution build has zero
warnings/errors; **9,951 full tests**,49 new/192 focused cases,1,253 MySQL runs,
two provider model gates and31 compatibility probes pass. Both legacy and populated
selected-cycle MySQL upgrades preserve every prior table's row projections.

This is the inventory component of the remaining claim transaction. The public
Contract claim handler, complete aggregate repository/executor, Character currency
enrollment/publication, Auth outbox, claimed-state adoption and reset remain
unconnected. Current source and migrations are not staged. Neither this component
nor the earlier quote is authority to grant the rest of a reward.

## Separate durable ownership

`CharacterContractRewardClaimModel` records Character/track/cycle/ordinal,
reward/choice, stable unique claim identity, account owner, complete quote bytes,
delivery bytes/versions and creation time. The four-part slot key prevents a second
claim for the same cycle ordinal; a unique index prevents reuse of a claim identity.
History belongs to the Character and survives track deletion/reset. Only Character
deletion cascades it. Zero cycle/ordinal keys are explicitly non-generated.

Additive MySQL `20261031043515_ContractRewardClaims` and SQLite
`20261031043709_ContractRewardClaims` migrations add that empty table and a
default-false `claimed` column to existing Contract slots. They backfill no claims,
select no rewards, alter no points/history and grant no items/currencies/entitlements.
Both providers' snapshots/designers match their models.

`IInventoryRewardReservation.StageContract` is an explicit additive entry point.
Legacy implementations default to NotSupportedException; no Contract request falls
back to their Challenge stage. Concrete typed adapters query their own receipt,
slot and track tables. A retained reservation cannot change receipt families,
identity, account or quote on retry, even if its ordinal keys are otherwise equal.
No synthetic Challenge medal, completion or claim represents a Contract item.

Contract admission requires an Added receipt, a live serializable unit, actual SQL
Character/account ownership, the prepared actor account, an initialized earned
selected cycle, the exact previously unclaimed slot, and absence of a prior claim.
The receipt's quote must be present/versioned; the future aggregate owner must
still restore it against the full canonical award catalog and enroll every effect.
The inventory component does not validate arbitrary quote JSON as grant authority.

## Atomic SQL witnesses and recovery

The shared reservation keeps the existing whole-delivery planning, binding,
capacity/slot leases, ID allocation, live mutation exclusion and post-commit
publication. It freezes receipt identity and slot provenance across each attempt.
Original owner/key/reward/cost values and the tracked claimed transition cannot be
rewritten or suppressed during enrollment.

The old boundary checked live tracked values but could not alone prove their SQL
writes. Separate Character pre-commit callbacks now verify the exact durable
receipt/claimed-slot pair and every destination Item row, including absence of a
realm-bank owner. They execute outside individual Item persistence gates. Deleting
a flushed receipt/new stack, undoing a claimed bit or suppressing a merge causes the
whole transaction to reject and roll back before any live Item can publish.

Lost acknowledgement recovery uses a fresh serializable transaction in the same
database. It compares the complete typed receipt, source slot/provenance and all
Item before/after rows before changing any leaf's outcome. A changed Contract
account owner, quote, selected source, delivery, timestamp or Item quarantines the
whole delivery. A proved rollback permits retry with retained IDs and creation
time; a proved commit permits a single publication without regranting.

Creation time is normalized to MySQL datetime(6) precision before first enrollment,
then retained on both providers. Recovery now verifies that timestamp for Challenge
receipts too; the earlier comparison omitted it. Existing Challenge queue/executor
and configured-strategy recovery tests remain green.

Live Contract snapshot loading currently fails closed if claimed slots or claim
history are present. This temporary guard prevents advertising a claimed reward
as available before the authoritative claim adoption owner is connected. It must
be replaced by complete validated receipt adoption before public claims/deployment;
it is not a substitute for implementing that owner.

## Qualification

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/contract-delivery`.

- `full-build3.log`, `full3/full.trx` and `focused3/focused.trx` bind the final
  source:9,951 full/192 focused, zero failed/skipped. All9,902 prior tests remain
  present. The49 new cases comprise42 provider-backed inventory cases, one legacy
  default capability case and six schema cases.
- `final-review/MySqlProof/result.json` records1,253 passing runs and per-case SQL
  snapshot hashes on fresh schema `nexus_assessment_contract_claim_31a1`:1,172 prior
  runs plus81 inventory cases (39 earlier inventory tests and42 new regressions).
  The existing216 configured-retry actor/executor cases rerun the shared Challenge
  machinery. Contract actor/currency/outbox configured-retry integration is pending;
  the direct Contract inventory cases do not claim it.
- `LegacyUpgradeProof/result.json` preserves60 old tables/68 logical rows from the
  pre-selection schema. `SelectionUpgradeProof/result.json` preserves61 tables/69
  rows including an existing selected slot, with its new claimed value false.
  Both prove current model column coverage, idempotent migrations, preservation of
  nonzero legacy options/decor values, zero keys, retained history across track
  deletion, Character cascades and the unrelated synthetic owner. These are fresh
  populated synthetic upgrades, not copied retail or runtime upgrade proofs.
- Two frozen provider model gates show zero pending model/relational differences;
  31 unchanged upstream compatibility probes pass. `AwardSourceAudit/result.json`
  reruns all726 actual-table quotes with the current frozen Game binary:486 Item2,
  240 Auth currency choices and six final credit awards. It grants nothing.
- `final-review` freezes5,843 Source/load-tool files,15 changed paths since9902,
  169 DLLs and37 exact provider case/helper files. The first successful9,946/9,947
  full runs remain historical while account and Item SQL guards were completed.
  An initial compatibility command used a missing project filename; its log is
  retained, followed by the successful real UpstreamRegressions project run.
- `runtime-preservation/result.json` retains the8378 backend's nine process
  identities, six naturally drained queues, original data,43 copied legacy
  characters, accepted Contract, private configurations/schema and defaults
  15:1/17:1/no44. Computer Use/native remain stopped; no commit/push was made.
  Content validation remains31 CSVs/167,484 rows, all `not_retail_complete`.

## Continue the complete Goal

Next connect the aggregate claim command/repository/executor and actual published
actor snapshot to these typed inventory receipts, including exact Character wallet
before/after enrollment for final credits. Add durable idempotent Auth handoff for
AccountItems2485/2486, with crash/relog/account-transfer recovery and complete claim
adoption. Implement the explicit Contract repeat/reset/carry policy and remaining
ordinary/full-table/copy-upgrade/socket/deployment checks before enabling claims.

Native UI remains pending explicit Computer Use resumption. The unavailable June19
Q5597 bundle is not requested again. Track35's earning source and all broader
assessment/upstream/AMP/combat/content requirements remain in the active full Goal.
