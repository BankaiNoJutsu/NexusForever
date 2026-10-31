# Authoritative account wallet observation and actor publication

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

The following records the earlier10165 wallet-refresh checkpoint.

Checkpoint10165 continues [Contract claim actor ownership](CONTRACT_CLAIM_ACTOR_QUEUE.md).
The full Goal remains active. This is an internal primitive for the remaining
Contract outbox consumer; production draining, relog recovery, source track
revision adoption and WorldServer07CE routing are still required. No public
claim path is enabled by this change.

## Ownership and merge

`AccountCurrency.Refresh.cs` reserves an idle ordinary wallet and copies its
ordered immutable operation journal. A pending save cannot be stolen, and a
normal/direct wallet save cannot run until the reservation ends. Ordinary local
adds and spends can continue to append semantic operations. Cosmic currency
owned by the Loyalty journal refuses this path.

The worker uses fresh, empty Auth contexts and the configured execution strategy.
One serializable transaction reads the account, wallet and captured operation
receipts in the existing account-before-wallet order; MySQL uses current locking
reads. It performs no allocation, credit, save, actor mutation or network work.
Factories must retain the same password-free database fingerprint across retries
and prior saves. A required external credit must have an exact durable operation
receipt, including account, currency, direction, source and arithmetic; its old
after-balance need not equal the current wallet after legitimate spending.

Only the reservation's privately retained successful read can be adopted. The
actor starts from the observed SQL amount, skips captured operations whose exact
receipts are present, then applies every remaining local operation in order.
For example, SQL107 plus a pending add10, then later spend4/add2, becomes115;
the next ordinary save persists those three operations once. An unknown earlier
commit carrying add10 is skipped when its receipt exists, even after another
legitimate external credit. No absolute cached balance is written back.

Preview completes before mutating the wallet or acknowledging keys. Missing
owners/wallets, changed receipts, wrong database and arithmetic underflow/overflow
quarantine the cache. Cancellation of a read preserves every local operation and
late worker completion cannot adopt it. An absent unsaved wallet retains its
creation state across repeated observations.

`AccountCurrencyManager.Refresh.cs` binds the original account id, manager,
WorldSessionEpoch, currency object and session. The caller awaits `Completion`
off the actor and invokes `TryPublish` on its session actor. The manager holds
admission through both wallet packets, rejects reentrant save/refresh and checks
native disconnect state before and during publication. Later semantic mutations
remain dirty. Repeated publication cannot send or regrant again. The SQL worker
does not dispatch any actor callback.

The wallet observation can become stale after its transaction ends; later SQL
changes require another observation. It must be started after the corresponding
outbox delivery is observed, with that credit's required operation receipt. This
primitive does not itself schedule a worker, drain the outbox, advance Contract
snapshot revisions or establish a Character/logout lifetime.

## Contract lifetime guards

Contract selection/claim admission, claim adoption and Character credit
reservation/publication now check native `NetworkSession.IsDisconnecting` and
terminal `LogoutState.Logout/Finished`, in addition to their retained object,
session, database and invalidation witnesses. Countdown `Timer` retains existing
behavior. Gated regression tests change those native flags after execution begins:
committed SQL is retained, no late item/currency/track packet is published, and
the stale owners require reload. These are server-side socket-free native session
objects; no WildStar process or Computer Use was started.

## Verification and remaining work

Verification artifacts are under
`I:\GIT\NexusForever-assessment-report\remaining-goal\contract-wallet-refresh`.
The final verifier must pass before treating the following as qualified:

- Full solution build: zero warnings/errors. Full rerun:10,165 passed;
  39 new cases and328 focused passed, all earlier10,126 tests retained.
- The initial full run hit the unchanged STS outer timeout test; its failed TRX
  is preserved. The12-case independent socket recheck and full rerun qualify the
  unchanged source rather than dropping that test.
- MySQL:1,643 cases, comprising1,374 Character,140 Auth and129 dual-database,
  including402 configured-strategy repeats; four independent Auth race probes.
- Four provider model gates,31 original compatibility probes and726 actual
  Contract quote choices. Item delivery metadata in actor fixtures is synthetic.
- Source/tool inventory5,873 files;11 changed paths versus10126, four added;
  169 frozen DLLs and47 copied case/helper sources. Both database DLLs unchanged;
  prior populated upgrade evidence remains bound by those exact binaries.

No source/migration deployment, schema change, staging, commit or push. The
separately qualified8378 runtime, nine process identities, six drained queues,
original data/configuration and entitlement defaults15:1/17:1/no44 remain the
preservation boundary. The unavailable Q5597 smoke bundle is not requested again.

The user directs retail reset behavior, with no policy preference pending.
[Live-era evidence](CONTRACT_RETAIL_RESET_EVIDENCE.md) supports final-reward
collection as the working reset trigger; unclaimed-earlier and excess-point
edges remain inferred/unproved. This change does not install an arbitrary reset
rule. Public all-choice routing, connected account drain/relog/refresh and source
revision handling, reset, all actual-item delivery, copied-upgrade/socket/runtime/
native qualification, track35 and the broader assessment/upstream/combat/AMP/
content requirements remain in the active Goal.
