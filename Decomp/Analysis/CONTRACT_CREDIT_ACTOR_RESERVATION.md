# Contract credit ownership and actor publication

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

The following records the earlier10095 credit-reservation checkpoint.

Checkpoint10095 adds an internal live credit reservation for the existing durable
Contract claim executor. The full solution builds with zero warnings/errors;
**10,095 full tests**,27 new/103 focused pass. Frozen MySQL qualification covers
1,503 cases (1,300 Character,74 Auth,129 dual-database), including332 configured
provider-strategy repeats. Four independent Auth race probes, four provider model
gates,31 compatibility probes and all726 actual-table quote choices pass.

This is the currency ownership/publication component. It is not yet connected to
the registered public07CE actor queue. No source or migration is deployed, and
no native UI result is claimed.

## Clean baseline and exact execution ownership

CurrencyManager can reserve a canonical final-credit quote only for its current
Character, account and session. Existing wallets must be clean and have one known
SQL baseline. An absent currency uses a private unregistered owner until commit;
preparation neither creates a live currency row nor dirties a normal save.
The existing row persistence owner is retired while reserved, preventing direct
absolute writes or competing normal/frozen saves. Player admission checks this
before starting Auth saves; disposal invalidates the pending owner.

Only immutable before-row existence/amount enters the SQL command. The live ticket
retains quote, operation, account/session references, currency metadata, database
identity and inventory reservation. Adoption requires the original executor's
private completion proof, the exact retained command/delivery/database, terminal
commit, matching canonical receipt and the expected currency witness. A copied
execution record without that private proof cannot authorize money. A changed
account identity/reference, Character, session, cap or invalidated owner fails
closed and requires authoritative reload.

Successful adoption updates the live amount and its durable persistence baseline
without scheduling another currency UPDATE. The packet, earned-currency
achievement and EarnCurrency Quest event publish once and only on the actor's
explicit publication step. Claim replay cannot emit them again. A reservation
can cancel before execution; after execution starts, an unproved outcome remains
pinned rather than being treated as rollback.

## Later ordinary intent and reentrant publication

Later CurrencyManager grants and spends for the reserved currency retain their
requested amounts, directions and loot flags in order. They do not publish a
speculative balance, increment achievements or update Quest objectives while the
claim outcome is pending. Affordability checks cannot spend that pending credit.
Operations must fit both commit and rollback shadows until the outcome is known;
invalid arithmetic is rejected before entering the queue.

After proven commit, the ordinary operations replay against the actual credited
balance using the captured cap. After a pre-execution cancellation they replay
against the original balance. Actual earned amounts drive ordinary callbacks,
so a grant capped after a final credit cannot invent achievement/Quest progress.
Unlimited addition is checked; capped addition clamps the requested increment
before summing, preventing an overflow from wrapping below the cap.

The admission guard remains held through packets, achievements, Quest events and
the complete deferred queue. Reentrant ordinary currency calls join that queue;
they cannot start a competing claim or save mid-publication. Invalidation in a
packet callback stops subsequent gameplay/deferred effects and quarantines the
owner. No gameplay or network callback belongs to the SQL worker or a dead-owner
continuation.

## Qualification boundaries

The27 regressions use the real CurrencyManager, actual canonical reward69 and
the real SQL claim/executor path. Player/account/session/achievement/Quest surfaces
are recording proxies, and ItemInfo stack metadata remains synthetic. Cases cover
existing/absent wallets, unlimited/capped credit, deferred grants/spends,
pre-execution cancellation, blocked saves/direct writes, dual-outcome arithmetic,
untrusted execution records, changed owners, dirty baselines, once-only packets,
reentrant ordering/admission and invalidation. They are component integration
tests, not proof of a connected WorldServer packet handler or native UI.

The70 Contract claim cases now run as67 SQL-backed cases plus62 configured
retry repeats in the dual-database harness; three pure cap cases stay in the full
suite. Existing Character/Auth harnesses retain their1,300/74 cases. Both database
DLL hashes are unchanged, binding the prior60/61/62-table Character upgrades and
30-table Auth upgrade. The previous10068 before/after currency defect probes are
retained as historical evidence, not presented as new runs.

Evidence under
`I:\GIT\NexusForever-assessment-report\remaining-goal\contract-live-currency`
binds5,867 source/tool files, eight changed Source paths (three added),169 frozen
DLLs and44 exact-source case/helper sources. Failed fixture compile and initial
warning artifacts remain retained; the qualified final build has zero warnings.
The separate8378 runtime remains unchanged: nine service identities, six drained
queues, original data, accepted Contract, configuration/schema and defaults
15:1/17:1/no44. No commit, push, staging, Computer Use or native client launch.

## Continuing the full Goal

Connect this component to public claim admission and the registered actor queue,
including complete Quest scope ownership, inventory/currency presentation order,
failed/uncertain/relog/dead-owner settlement and ordinary save/gameplay coexistence.
Then connect account intent draining and authoritative online Auth wallet merging.
Do not infer those lifecycle guarantees from this component's green tests.

The user now directs retail reset behavior rather than selecting a server policy.
[Retail reset evidence](CONTRACT_RETAIL_RESET_EVIDENCE.md) supports final-reward
collection as the working reset direction; earlier unclaimed milestones and excess
points still require stronger evidence. No preference question remains pending.
Actual full-table item delivery, copied upgrades, connected sockets, deployment,
native UI, track35 and the broader upstream/assessment/combat/AMP/content work all
remain in the active Goal. Q5597 historical smoke evidence is unavailable and is
not requested again.
