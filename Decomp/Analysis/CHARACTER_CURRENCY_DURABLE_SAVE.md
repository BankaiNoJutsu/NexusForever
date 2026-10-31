# Character currency persistence and frozen normal saves

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

The following records the earlier10068 currency-save checkpoint.

Checkpoint10068 fixes Character currency save acknowledgement and includes currency
in the existing actor's frozen normal-save boundary. The full solution builds with
zero warnings/errors; **10,068 full tests**,26 new/304 focused tests pass. Frozen
provider qualification covers1,449 MySQL cases,305 configured-strategy repeats
within those cases, four separate Auth race probes, four model gates and31
compatibility probes. Two additional old/current currency probes reproduce and
repair the defects below. Public Contract claims remain disabled and the full
assessment Goal remains active.

## Reproduced failures and corrected ownership

Previously `Currency.Save` cleared its dirty mask as soon as it attached a row.
If an enclosing transaction rolled back, retrying the same actor silently omitted
the unsaved currency. It also saved a cached absolute amount without checking
the original SQL baseline, permitting an unaccounted credit to be overwritten.

`OldCurrencyProbe` and `NewCurrencyProbe` run identical public API scenarios
against frozen10042/current assemblies in separate synthetic MySQL schemas.
Both use explicit serializable transactions. A loaded100 wallet changes to125,
flushes and rolls back: the old retry leaves SQL at100; the current retry commits
125. Separately, an external credit changes SQL100 to107 after the loaded actor
sets125. The old save overwrites SQL with125; the current save refuses that stale
baseline and retains107. It does not guess how an external operation should merge
with an arbitrary absolute assignment. The small external probe projects emit
constant-filter compiler warnings; the full solution build has zero warnings.

Currency now reuses the existing commit-observed row persistence primitive. It
retains complete scalar Character/currency/amount identities, versioned dirty
intent, possible uncertain outcomes and a database/row lease through terminal
settlement. Create/update intent survives disposal, failed or suppressed flush,
rollback and unknown commit acknowledgement. A fresh retry reconciles actual SQL
against the retained candidates. Tracked replacement, modified-state suppression,
original-value tampering and post-flush SQL rewriting fail the owned unit. An
independent loaded owner or changed identity cannot redirect an unfinished write.
No schema or currency operation journal is added by this change.

## Frozen actor normal saves

The Player captures concrete Currency values before starting Auth work, using
the same frozen Character token as its Quest snapshot. The captured row list
and amounts join the Character stage with Quest, Challenge and ordinary reward
effects. A dirty value changed during Auth or later Character enrollment cannot
rewrite that captured save. Values added to an initially clean or absent currency
also remain dirty for the next save. Acknowledgement clears only the revisions
that actually reached SQL; current live intent is retained.

Player saves with the real CurrencyManager use the existing serializable atomic
Character transaction and complete graph witnesses, including ordinary saves
without a Contract completion. Custom manager fallbacks retain their existing
path. The frozen Character scope prevents escaping normal currency writes while
a Quest/reward owner is active. Persistence settlement has no network or gameplay
callback. Existing cap behavior, immediate ordinary currency packets, achievement
and Quest updates, entitlement defaults and reward semantics are preserved.

The26 regressions comprise22 direct currency durability cases and four real Player
normal-save cases. The direct cases cover create/update disposal, rollback,
failure/suppression, pending outer commit, later actor intent, before/after commit
faults, tracked tampering, post-flush SQL rewriting, frozen capture, stale external
balance and foreign identity. The real Player cases cover later intent during
Auth and after currency enrollment, plus initially clean and absent currencies.
Existing Contract/ordinary prizes and completion counts survive all successful
coexistence cases.

## Verification and preserved boundaries

The full suite retains all10,042 tests from the prior checkpoint. The current
Character provider harness runs1,300 cases:1,253 prior cases,26 new cases and21
additional configured-strategy repeats. Auth runs74 cases and four independent
race probes; the dual-database claim coordinator runs75 cases. The actual-table
quote audit still covers all726 choices. Real item delivery metadata remains
separately bounded as documented in the
[claim handoff checkpoint](CONTRACT_CLAIM_TRANSACTION_AND_ACCOUNT_HANDOFF.md).

Both Character and Auth database DLL hashes are unchanged. That binds the prior
60/61/62-table populated Character upgrades (68/69/70 logical rows) and the
30-table/145-row Auth upgrade without pretending those were new migration runs.
The four current provider model checks have no pending model changes or relational
differences. Evidence under
`I:\GIT\NexusForever-assessment-report\remaining-goal\character-currency`
retains5,864 source/tool hashes, seven changed Source paths (two added),169 frozen
DLLs and43 exact-source case/helper files. Build/test failures from fixture
development remain retained. The verifier binds source, binaries, cases, SQL
snapshots, the before/after probes, prior upgrades, input tables and status docs.

The8378 runtime remains unchanged: nine service identities, six drained queues,
original77 tables/21,989 rows/43 copied legacy characters, accepted Contract,
configuration and runtime schema are preserved. Defaults remain15:1/17:1/no44,
with no automatic seven-item grant. No source/migration deployment, commit, push,
Computer Use or native client launch occurred. The June19 Q5597 smoke bundle is
unavailable and is not requested again.

Next is claim-specific live currency reservation and authoritative adoption, then
public07CE admission through the registered actor queue and once-only item/currency
publication. Account outbox draining/relog recovery and authoritative online Auth
wallet merging, reset/carry policy, actual full-table delivery/copied-upgrade/
socket/deployment/native gates, track35 and the entire broader assessment,
upstream, combat, AMP and content Goal remain required. This checkpoint does not
declare Contracts, retail parity or the full Goal complete.
