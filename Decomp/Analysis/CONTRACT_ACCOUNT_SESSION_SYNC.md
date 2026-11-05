# Contract account delivery through the session reward queue

## Claimed bit after account settlement

5 November 2026. A Character claim receipt reserves its selected slot as soon
as the Character transaction commits. For a compensation award, the composed
reward-track reload sets the claimed bit only once the source outbox records
acknowledged Auth delivery. Character-only item and credit claims retain their
existing publication path. Duplicate requests remain blocked while Auth settles.

The baseline reproduced an early claimed bit on both Contract tracks while
Auth was paused. The regression also forces a reload from the account owner
during both the Auth commit and the Character acknowledgement. It checks the
settled wallet update precedes the first claimed bit. The owning build passed
with zero warnings and errors in 25 seconds. All 76 targeted claim, wallet,
relog, and compositor cases passed in 16 seconds. A broader filter was
stopped after 10 minutes 25 seconds and remains recorded as interrupted.
Local evidence is under
`artifacts/continuation-20261105/contract-account-claimed-publication/`.
The retained six-table audit supplies quote effects for 726 choices, including
486 force-bound item choices. The raw build 16042 tables were unavailable in the
checked destination and archived checkout. Actual Item2 stack and delivery
qualification, retail reset policy and public 07CE routing remain open.

## Historical continuation

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

The following records the earlier 10190 session-synchronization checkpoint.

Checkpoint10190 continues [authoritative wallet observation](ACCOUNT_WALLET_AUTHORITATIVE_REFRESH.md).
The full Goal remains active. The production Player now supplies an Auth context
factory, and its existing Character reward queue runs account synchronization at
world entry/relog, after internal account-note claims, and on an eligible30-second
poll for unseen intents. Public WorldServer07CE Contract claim routing is still
disabled pending reset and further end-to-end qualification.

## Connected ownership

`AuthDatabase.CreateContext` mirrors the existing caller-owned Character factory;
it rejects an uninitialized database. No configuration, schema or migration is
changed. `Player.Initialise` supplies both factories to `ChallengeManager`.
Custom/legacy adapters without that Auth factory or durable account wallet retain
their established path.

`ChallengeManager.ContractAccounts.cs` and the existing `CharacterRewardQueue`
retain the normal-save/inventory/Challenge owner across the entire operation.
`ContractAccountSync` captures the original Character, account, realm, session,
WorldSessionEpoch, Quest manager, Challenge manager and wallet manager. It freezes
the award catalog on the actor. Only copied identities/catalogs and fresh context
factories are used on SQL workers; no worker publishes a packet or invokes a
gameplay callback.

The source list is scoped to the original account and realm, including intents
from another or deleted Character. Every selected row must retain its canonical
payload and scalar owners. Initial and post-claim scans include acknowledged rows:
Auth credit/source ACK may have completed after the admission wallet model was
read. Each database retains its password-free fingerprint across the list,
delivery, retry and wallet observation.

For each currency group, the actor reserves its wallet before starting delivery.
The read waits on a private barrier until the durable receiver has observed the
exact Auth receipt and source acknowledgement for every retained intent. Delivery
uses the existing global operation key and independent transactions; before-commit
and lost-acknowledgement faults cannot regrant or increment the source revision
twice. Bounded retries preserve the same immutable intents. A busy wallet save is
allowed to settle before reservation; later local adds/spends remain semantic
journal entries throughout.

After actor wallet adoption/publication, the same queued owner performs a fresh
Character load. `LoadForAccount` retains the original account id; both initial
execution and recovery check the locked SQL Character, and save-validation guards
reject owner changes. A transferred or deleted Character cannot publish fresh
Character data to the previous account. Its committed original-account credit
remains durable. The privately proved loaded snapshot must show acknowledgement
of current-cycle claims; then the existing compositor adopts the new Contract
revision while retaining ordinary Quest/Challenge state and account tracks.
Another claim or normal Player save cannot enter the gap between these steps.

Observed source ids become poll witnesses only after successful actor settlement.
An eligible30-second poll excludes those ids and finds late pending **or already
acknowledged** intents, including work completed after the initial login scan.
An empty poll releases its prepared owner without a Character reload or network
packet. Poll time pauses while another reward owner is busy;30 seconds is not a
wall-clock delivery guarantee. Offline intents are recovered on their account's
next entry, or by the already available durable receiver; this is not a global
offline-account daemon. Large-history query/load performance remains unqualified.

Terminal logout/disconnect, replaced account/session/manager/epoch and invalidated
owners cannot publish late account packets or world readiness. Final disposal
releases read reservations and prepared bookkeeping without polling session events;
the independent SQL worker may finish its original durable intent. Queue failure
disconnects the captured original session rather than a replacement session.

## Verification

Final evidence:
`I:\GIT\NexusForever-assessment-report\remaining-goal\contract-account-sync\final-review\VERIFICATION.json`.
The verifier binds all gates below; no retail or deployed-runtime claim follows
from these source/provider checks.

- Full solution build: zero warnings/errors. **10,190 full tests**,25 new and353
  focused passed; every earlier10,165 test remains present and passing.
- New real-actor cases cover both Contract track types, local and later wallet
  intent plus normal Quest saves, both databases' before/after commit faults,
  terminal/replaced/final owners, pending/acknowledged relog, original-account/
  realm filtering, deleted-source outboxes, corrupt intent/receipt refusal,
  successive acknowledged revisions, final Item2/Character-credit coexistence,
  transferred/deleted current Characters, and late-intent polling without repeats.
- **1,693 MySQL cases**:1,374 Character,140 Auth and179 dual-database, including427
  configured-strategy repeats, plus four independent Auth race probes.
- Four provider model gates,31 original compatibility probes, and726 real-table
  quote choices. Actor delivery ItemInfo/stack metadata remains synthetic.
- 5,876 source/tool files,17 changed paths versus10165, three added;169 frozen DLLs
  and48 copied case/helper sources. Character DB DLL is unchanged. Auth DLL changes
  for the factory API; all Auth model/migration sources remain byte-identical and
  its fresh populated upgrade preserves30 old tables/145 logical rows, wallet and
  pending/inventory data, defaults15:1/17:1, and idempotent replay without grants.
- Existing Character upgrade bindings60/61/62 tables and68/69/70 rows remain bound
  by the unchanged Character DLL. Current model gates report no pending changes
  or relational operations for either provider.

The separately qualified8378 backend, nine process identities, six drained queues,
original77 tables/21,989 rows/43 copied legacy Characters, private configuration,
schema and entitlement defaults15:1/17:1/no44 are preserved. Source/migrations are
not deployed. No staging, commit, push, native WildStar launch or Computer Use.
The unavailable Q5597 UI bundle is not requested again.

## Remaining Goal requirements

The user directs retail reset behavior with no policy preference pending.
[Live-era reset evidence](CONTRACT_RETAIL_RESET_EVIDENCE.md) supports final-reward
collection as the working direction; unclaimed earlier milestones and excess
points remain unproved. Final retail slots pay Item2 plus Character credits;
account-note compensation comes from earlier milestones. No reset/forfeiture/
carry rule is installed by this change.

Public all-choice07CE routing, reset, further failure/lifecycle and large-history
load qualification, all actual-item delivery, copied-runtime upgrades, sockets,
deployment/native checks, track35 earning, ordinary full Quest coexistence and
the broader assessment/upstream/AMP/combat/content requirements remain. Current
source/provider gates do not establish native UI behavior or retail completion.
