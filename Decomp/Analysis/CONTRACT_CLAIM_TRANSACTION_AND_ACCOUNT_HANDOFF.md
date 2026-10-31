# Contract claim transactions and durable account handoff

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

The following records the earlier10042 claim checkpoint.

Checkpoint10042 implements the backend claim aggregate and the two-database account
credit handoff. The full solution builds with zero warnings/errors and all
**10,042 tests pass**, including47 new regressions and1,579 focused tests. Frozen
MySQL qualification covers1,402 cases (1,253 existing Character,74 existing Auth,
75 claim/handoff),284 configured-strategy repeats within those cases, and four
separate Auth race probes. Four provider model gates and31 compatibility probes
pass. These are backend and synthetic fixture qualifications. Public Contract
claim routing remains disabled; source and migrations are not deployed.

## Atomic Character claim

The immutable claim command retains the complete published Contract baseline,
account/realm, selected reward quote, stable operation identity, timestamp and any
Character credit baseline. Metadata used by payout resolution is copied into
scalar catalog records before worker execution. Mutating GameTable rows later
cannot change a captured payout or cap.

The existing serializable Character reward unit locks the Character owner before
reading the complete aggregate. It checks account ownership, prerequisite, active
selected cycle, threshold, quote, points and revision. The same transaction marks
the slot claimed, advances revision once, writes the canonical claim receipt,
enrolls reserved Item2 delivery or an independent account-credit intent, and applies
any final Character credit award. It retains points, cycle and excess; no reset
policy is inferred. Challenge receipts and history are not used for Contract claims.

Character credits retain exact row existence and before/after amounts in a
canonical nullable receipt witness. Existing cap behavior and checked arithmetic
are preserved. Both tracked identities and durable SQL targets are checked before
commit; suppressed wallet, receipt, slot, outbox or inventory writes reject the
entire unit. Commit uncertainty resolves through fresh SQL evidence before any
publication or retry. The same retained claim is read-only on replay; a different
operation cannot replace its choice.

The snapshot loader validates current and historical claim receipts against their
embedded frozen quote and matching account intent. A claimed slot without its
receipt, or mismatched provenance, fails closed. Historical receipts survive track
removal. Validated claimed bits now appear in the real Quest owner's reward-track
presentation while preserving committed-snapshot identity. This does not admit a
public claim request or publish speculative currency/item state.

## Account credit handoff

The source transaction stores a canonical intent in
`character_contract_account_outbox`. Its original Character, account, realm,
selected cycle, ordinal and quote are immutable. The outbox has no Character
foreign key, so an already committed credit survives source deletion or transfer.
It is not a pending item grant or an entitlement default.

The handoff coordinator uses fresh, empty contexts on the captured database
identities. Auth applies the original credit through the existing account wallet
operation repository with stable global key `contract:{realm}:{claim}`. A
matching global receipt proves prior application independently of subsequent
legitimate spending. Conflicting operation ownership or source bytes cannot grant
or acknowledge a credit. An acknowledged source whose Auth receipt is missing
fails closed instead of minting a replacement operation.

After Auth proves application, a separate serializable Character unit marks the
source delivered and advances the retained matching track revision once. It uses
Character-before-outbox lock order and validates exact tracked and SQL targets.
Lost acknowledgements on either database are recovered from durable receipts;
there is no distributed transaction or actor/network callback in the coordinator.
An unavailable account leaves the original intent pending; it cannot fall back to
another account or realm. Background draining and authoritative online actor
wallet publication are still required before connecting a live producer.

## Additive schema and qualification

MySQL `20261031055232_ContractClaimDelivery` and SQLite
`20261031055429_ContractClaimDelivery` add only a nullable `characterCurrencyJson`
receipt column, the independent outbox and its lookup index. Existing claims get
no fabricated credit witness. No migration samples rewards, grants money or
entitlements, resets progression or backfills an account operation.

Three populated MySQL upgrades preserve every old-column row projection:
60 tables/68 logical rows before selection,61/69 with selected slots, and62/70
with an existing claimed slot and receipt. All reach the current63-table model,
replay migrations safely and create an empty outbox. The immediate predecessor
retains its original quote/delivery bytes, claimed state and microsecond timestamp
with a null credit witness. Those receipt bytes are opaque migration audit text,
not a semantically loadable payout fixture. The unchanged Auth database DLL binds
the separately qualified9995 Auth upgrade (30 old tables/145 logical rows).

The75 dual-database runs comprise40 SQL-backed regressions and35 configured retry
repeats. They cover14 unique representative actual catalog choices, final credits,
duplicate/foreign claims, stale owners/revisions/points/wallets, suppressed writes,
source/Auth/ack commit uncertainty, five/seven-note compensation, conflicting Auth
receipts, transfer/deletion and real Quest claimed-bit presentation. Three pure
currency-cap cases, three schema cases and the frozen-metadata regression also run
in the full suite. Actual-table quotation audits all726 choices (486 Item2,
240 account currency and six final credit choices). ItemInfo stack metadata in the
delivery fixtures is synthetic: this is not all726 real-item delivery qualification.

Evidence is retained under
`I:\GIT\NexusForever-assessment-report\remaining-goal\contract-claims`, with
5,862 source/tool hashes,28 changed Source paths,169 frozen DLLs and41 copied
case/helper sources. The final verifier binds results, snapshots, loaded binaries,
input tables and documentation. Failed fixture/command artifacts remain retained.

## Remaining live integration and full Goal

Next: own Character credit state through the normal-save boundary and actor queue,
admit public07CE claims only from the published snapshot, settle item/currency
publication once, drain/recover account intents and merge authoritative Auth
wallets into online actors. Reset/carry policy, actual item binding/stacking,
ordinary Quest coexistence, copied upgrades, connected sockets and native UI need
their own evidence. Track35's earning producer and all other assessment, upstream,
combat, AMP, content and live-verification gaps remain in the active full Goal.

The existing8378 backend remains running unchanged: nine service identities, six
drained queues, original data, accepted Contract, private configuration and runtime
schema are preserved. Defaults remain15:1/17:1 with no automatic44 or seven-item
grant. No commit, push, deployment, Computer Use or native client launch occurred.
The unavailable June19 Q5597 bundle remains an explicit evidence gap and is not
requested again.
