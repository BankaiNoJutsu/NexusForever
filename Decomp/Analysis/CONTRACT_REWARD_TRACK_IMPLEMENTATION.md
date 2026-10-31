# Contract reward tracks: mapped sources and durable implementation

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

Earlier checkpoint9951: [Contract receipt and inventory enrollment](CONTRACT_REWARD_INVENTORY_ENROLLMENT.md)
qualifies checkpoint9951: **9,951 full tests**,49 new/192 focused,1,253 MySQL runs,
two model gates and31 compatibility probes, with zero build warnings/errors.
Typed Contract receipts and item enrollment preserve Challenge ownership; exact
pre-commit receipt/slot/Item SQL witnesses reject suppressed writes. Lost-ack
recovery retains account/quote/source/time/IDs and quarantines conflicts. Additive
MySQL/SQLite migrations preserve both legacy and already-selected populations.
All726 actual-table awards still quote correctly. Public claims, full aggregate
repository/executor, claimed-state adoption, Character currency/Auth outbox/reset,
real delivery/socket/deployment/native checks and the full broader Goal remain.
Current source/migrations stay unstaged: the8378 backend,9 identities,6 drained
queues, originals/config/schema and defaults15:1/17:1/no44 remain preserved.
Computer Use stays stopped; Q5597 remains unavailable and is not requested again.

Earlier checkpoint9902: [Contract award planning and selected-cycle quotes](CONTRACT_REWARD_AWARD_QUOTES.md)
qualifies checkpoint9902: **9,902 full tests**,780 new/focused cases and an
actual-table audit of all726 choices (486 Item2,240 account currency, six final
credit awards). The full build has zero warnings/errors. Immutable quotes retain
selected-cycle provenance and complete separate Character/Auth payout effects;
unknown/mixed effects, overflow and changed quotes fail closed. Quotes do not
claim or grant rewards. All prior5833 Source/tool files are unchanged; three
files were added. No schema/default/runtime changes: the8378 backend,9 identities,
originals/config/schema/defaults15:1/17:1/no44 and6 drained queues are preserved.
The1172 MySQL/two model/31 compatibility gates below remain the separate9122
checkpoint, not new runs. Connected claims/reset/carry/Auth handoff, real delivery,
socket/deployment/native checks and the full broader assessment Goal remain active.
Computer Use stays stopped; the unavailable Q5597 bundle is not requested again.

Earlier checkpoint9122: [Contract track choice through the registered actor queue](CONTRACT_REWARD_TRACK_CHOICES.md)
qualifies checkpoint9122: **9,122 full tests**,32 new/674 focused,1,172 MySQL runs
(1,124 prior +24 choices +24 retry), two model gates and31 compatibility probes,
with zero build warnings/errors. Registered07CF routes Contract0/1 through the
real Quest owner and existing queue. Published/baseline/busy/foreign guards precede
capture; active flags and optional legacy-cycle selections commit together. Choose
provenance records an operation, never fabricated acceptance/history. Other active
types and dirty Quest/Challenge intent survive; lost-ack recovery cannot reroll.
No schema/default changes or runtime staging: the8378 backend,9 identities,
originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain preserved.
Claims/reset/Auth handoff/full-table/socket/deployment/native gates and the pending
reset-policy preference remain. Computer Use stays stopped; the full Goal is active.

Earlier checkpoint8679: [Atomic Contract/ordinary Character save enrollment](CONTRACT_NORMAL_SAVE_TRANSACTION.md)
qualifies checkpoint8679: **8,679 full tests**,116 new/116 focused,853 MySQL cases
(680 prior +116 new +57 configured-retry cases), two model gates and31 compatibility
probes, with zero build warnings/errors. The complete enrolled Character graph,
receipt/points and exact Quest SQL witnesses share one serializable unit. Real
frozen Quest and ordinary Currency saves, partial stubs, later intent, suppressed
writes, rollback and lost acknowledgement are covered. No model/migration changes
or entitlement-default changes are introduced. The live Player completion journal/
FIFO and Auth handoff, then selection/claim/reset/publication/socket qualification,
remain required; no live earning is enabled. The qualified8378 runtime, all9 service
identities, originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain
preserved. Computer Use stays stopped and the full assessment Goal remains active.

Earlier checkpoint8563: [Contract modifier ownership](CONTRACT_REWARD_MODIFIERS.md)
verifies all12 actual effect lifetimes, whole-scalar projection and frozen modifier
inputs at8563:8,563 full tests/680 MySQL cases, plus real-table projection. Signature
and entitlement44 remain Challenge-only. Live actor/ordinary reward composite
integration, selection/claim/reset and Auth handoff remain required. Current code
is not staged; the qualified8378 acceptance runtime remains unchanged.

Earlier checkpoint8474: [Contract completion transaction](CONTRACT_COMPLETION_TRANSACTION.md)
implements the receipt/point/terminal-graph persistence core at8474 with8,474 full
tests,629 MySQL cases and populated migration preservation. Live completion,
ordinary reward composite ownership and Contract modifier capture remain required.
The runtime stays on8378; current completion code/migrations are not staged.

Earlier checkpoint8378: [Contract acceptance identity and persistence](CONTRACT_ACCEPTANCE_PERSISTENCE.md)
qualifies checkpoint8378:8,378 tests,536 MySQL cases, additive MySQL/SQLite
migrations and actual Contract9880 acceptance through four socket world sessions.
The runtime is now staged at8378; historicalNULLs and all existing values are
preserved. Completion receipts/point credit/selection/claims/reset/account handoff
remain required. The8333 static mapping and its historical verification below
remain retained with their original scope.

Checkpoint8333 implements immutable Contract point-source descriptions and complete static reward catalogs. It does not yet enable Contract point credit, selection, claims, reset or account-currency delivery. Those connected runtime steps below remain required by the full assessment Goal. Existing Contract offering/completion, Character Challenge rewards, Loyalty and entitlement defaults remain preserved.

## Exact-build mapping

Binary: `I:/WildStar/Client64/WildStar64.exe`, build16042, SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`. Existing Ghidra cache: `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_<hash>/`.

Registration1406a2ec0 walks method table140c5d5f0. Direct read-only PE section/pointer/string decoding verifies these exact bindings independently of the cached pseudocode:

| Method | Table cell | Function | Mapped behavior |
| --- | --- | --- | --- |
| GetQuality |140c5d630|1406a3140|Resolve the Quest's periodic group; read quality at+0x18. |
| GetType |140c5d640|1406a31e0|Read periodic group type at+0x14. |
| GetRewardTrackType |140c5d650|1406a3280|Group type1 maps to track type0; group type2 maps to1. Other valid group types return sentinel5. Missing context returns nil. |
| GetRewardTrackValue |140c5d660|1406a3340|Qualities1–4 read GameFormula1193 dataint0–03. Missing-row fallbacks are20/100/200/500. Configured build16042 values are50/150/450/500. |

The five function bindings are recorded here. CSV label application/readback remains a separate pending step because the label workflow requires corresponding exports; this pass does not change that map or claim fresh Ghidra analysis or live debugger execution. REA MCP is connected but its native providers are unconfigured; its status is retained in `contract-tracks/rea-session-status.json`. Cached Ghidra fragments and original PE bytes were sufficient for this mapping without configuration repair or client launch.

Ten hashed configured tables corroborate the fields and complete choices. Of55 Quest2 rows with the Contract flag,51 resolve through groups49–54:16 PvP and35 PvE. Four flagged rows have no usable periodic group and retain the existing blocker. The current groups use qualities1–3. Quality4 is mapped by the native function and tested synthetically, not claimed as a present offered Contract.

| Track | Type/owner | Costs | Static rows/choices |
| --- | --- | --- | --- |
|4|0, Character Contract PvP|1000/2000/3000|121 rows,363 choices |
|5|1, Character Contract PvE|1000/2000/3000/4000/5000|121 rows,363 choices |

Rows carry class/level/rank prerequisites. Nonfinal slots must retain those filters instead of sampling from the full class/rank pool. Final rows270 and69 are unconditional and each includes CurrencyType1 amount2,500,000, alongside its selectable Item2 reward. AccountItem choices2485/2486 represent currency14, Protostar Promissory Notes, amounts5/7. Type1 AccountItem must not be treated as an Item2 id or dropped from the choices. These are source facts, not proof of delivery/reset timing or retail RNG.

Type4/track35 has one row535 and a6,000,000-point cost. Its earning source remains unmapped. It is excluded from the Contract catalog; generic zone title completion is not evidence for this reward-track producer.

## Implemented static boundary

`IContractManager.TryGetRewardSource` provides an additive default-false capability for existing custom managers. The concrete manager first applies its existing canonical Contract/objective/reward/item/group validation, then snapshots Quest/group/track ownership, track prerequisite, quality, base points and explicit fallback provenance. Source values are copied into immutable `ContractRewardSource` records when the lazy catalog is built. Later mutable table/caller metadata cannot rewrite a quote. Unsupported group type/quality, ambiguous or invalid track owners and negative native-int formula representations yield no quote without changing the existing lifecycle.

A source description is not a completion receipt, offering check, player prerequisite decision or permission to award points. It deliberately says BasePoints; any additional modifier ownership still requires evidence before credit is enabled.

`ContractRewardCatalog` preserves both complete type0/type1 catalogs:242 rows,726 canonical choices, all cost ordinals, row/track prerequisites, binding/final flags and separate currency values. It validates Item2, AccountItem and CurrencyType reference existence, rejects malformed identities/costs/masks/choices/quantities and copies interpreted values into read-only snapshots. Actor-captured satisfied prerequisite ids filter candidates without class/rank fallback. This class does not choose a cycle, sample, grant, consume, publish or touch SQL.

## Verification

Evidence root: `I:/GIT/NexusForever-assessment-report/remaining-goal/contract-tracks`.

- Full solution build: **0 warnings,0 errors**. Full suite: **8,333/8,333**, zero skipped, including350 new cases since7983. Full TRX SHA256:`947C90B7292B3EB992A612BE0383DDC0828536EE0FC4364C5259F22B2133BE77`. Focused603 pass;31 unchanged compatibility probes pass.
- The86 source-description cases cover all55 actual flagged rows, exact configured versus fallback formula values, zero/signed-boundary values, invalid owners/types/qualities, existing lifecycle validation, custom-manager compatibility and frozen metadata. The264 catalog cases retain every one of242 actual rows plus malformed-data, prerequisite, ownership and immutable-snapshot guards. Item/AccountItem/CurrencyType test data supplies identity existence only; these tests do not exercise item effects or wallet delivery.
- `ImplementedSourceAudit/result.json` runs real ContractManager, QuestInfo and ContractRewardCatalog with ten actual configured tables in a cache-disabled audit process. It confirms51 sources/four blocked rows,242 rewards/726 choices, both final currency awards and two actual compensation AccountItems. Private configuration is hashed unchanged; no player, database, broker, socket or native UI execution occurs in that audit.
- Final source/build inventory binds169 DLLs,5,784 Source/tool files and exactly7 changed Source paths since7983. No model, migration, existing Quest reward producer or reward packet routing changed. Early source-audit ItemEntry naming and catalog accessor compile errors, and the complete-catalog test failure which exposed the final currency awards, remain retained rather than overwritten.
- Read-only preservation verifies the separately qualified7983 runtime remains healthy: all9 services with their identities,6 naturally drained queues, closed latest presence, original data, defaults15:1/17:1/no44, configurations and schema. No current Contract code is staged or claimed as socket-verified. Its pending transaction work cannot be inferred from the earlier515-provider/16-cycle Challenge proof.
- Content validation remains31 CSVs/167,484 rows, all `not_retail_complete`. Original checkout clean, branch/head unchanged, no commit/push. Computer Use stays stopped.

## Required connected implementation

1. **Connect durable completion.** Acceptance generations are socket-verified at8378. The generation-keyed receipt/point/exact-Quest persistence core and executor are verified at8474; historical rows remain unbackfilled. Complete live actor/composite ordinary-reward ownership and wire the8563 verified Contract modifier capture before enabling that core. Preserve historical/duplicate/retry/daily-accept/disconnect/crash safety without minting a new generation on retry or inferring old completion counts.
2. **One Character unit with concrete Quest owners.** Integrate Contract points and the captured terminal Quest graph with the established actor/FIFO and normal-save ownership rules. Freeze source values, eligibility and any evidenced modifier before worker execution. Preserve ordinary Quest rewards, consumed quest items, objective/script effects, achievements and later actor changes. Keep failure quarantine/reload and committed-publication boundaries. Do not route Contract points through a synthetic Challenge medal or backfill completed Quests.
3. **Selections and claims.** Persist the prerequisite-filtered canonical selected row at each cost ordinal. Define the full accepted Contract repeat/reset/carry policy explicitly; the Challenge policy is not retail evidence for Contract timing. Preserve all equipment choices, quantities and final credits. Reject stale/unpublished/busy/foreign requests using actual owner/revision/cycle state;07CE has no cycle nonce. Publish only committed composed state without displacing Loyalty/Challenge owners.
4. **AccountItem compensation and cross-database recovery.** AccountItems2485/2486 lead to an account wallet while Contract progression/items/credits belong to Character. Use an atomic supported unit or a durable idempotent handoff/outbox, with a stable receipt identity and recovery after either database commits. Reuse the mapped AccountItem grant/consumption intent rules; preserve14 currency amounts5/7. Test crash/relog/duplicate and account/character ownership changes. A Character receipt cannot merely omit this choice or publish an uncommitted wallet mutation.
5. **Verification of the connected path.** Add provider tests for all delivery kinds, eligibility/selection stability, real Quest completion and repeat/reset, write-acknowledgement loss, interleaved normal saves, cancellation/disposal and account handoff recovery. Run full build/tests and scoped migration preservation gates if schema changes. Then restage the isolated backend and exercise genuine Contract accept/objective/complete/earn/choose/claim/relog/reset using actual sources. Native UI remains pending explicit Computer Use resumption.
6. **Zone progression and broader Goal.** Independently map track35's actual point source and ownership before implementing it. Continue all remaining assessment, upstream semantics, AMP, combat, content and native evidence gates; this document does not narrow the full Goal to Contracts or static catalogs.
