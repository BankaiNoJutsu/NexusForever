# Normal auction bid transaction

## Physical producer loss and fresh Character reload

6 November 2026, parent f1b2ef766c5ae65fce3046f32ed76dff6b8fbfb5.
The supported normal bid now has a headless physical-process qualification through
the parsed public handler, a real DI scope and original Player event queue.
One control and six terminated producer windows preserve the complete recorded
listing, escrow, wallet, ordinary Quest and receipt graph. Production Source,
schema, settlement and routing are unchanged.

| Producer boundary | Fresh state before explicit retry | Public retry outcome |
| --- | --- | --- |
| Completed control |110 credits, Quest progress 2, two receipts | Rejected duplicate; unchanged graph |
| Before ordinary preflight commit |100 credits, progress 1, no receipts/bid | One 10-credit debit; ends 90/progress 1 |
| Before financial commit |120 credits, progress 2, no financial receipt/bid | One 10-credit debit; ends 110 |
| After financial commit |110 credits, progress 2, one financial receipt | Rejected duplicate; original graph retained |
| Before reconciliation commit |110 credits, progress 2, one financial receipt | Rejected duplicate; original graph retained |
| After reconciliation commit |110 credits, progress 2, two receipts | Rejected duplicate; unchanged graph |
| Before result acceptance |110 credits, progress 2, two receipts | Rejected duplicate; unchanged graph |

Each case has independent reload, explicit public retry and another fresh
duplicate-request process. A final seven-process audit observes results before
fixture actor loading, asserts the loaded wallet against SQL, and repeats the
rejected duplicate with exact snapshot/hash equality. Earlier producer results
remain bound to their original candidate; only observer placement, assertions
and the no-reseed audit mode change in the final test candidate.

Transaction interceptors pause real committing/committed callbacks; publication
pauses before result acceptance. The parent pins each original process handle,
creation time and executable identity before terminating that generation.
It does not throw simulated lost acknowledgements or choose commit outcomes.
Receipt IDs and payload hashes stay stable across rejected retries. The two
post-financial/pre-reconciliation windows retain one financial receipt: fresh
loading does not fabricate the killed actor's reconciliation receipt.
Uncommitted ordinary credit/Quest intent is correctly absent after preflight loss.

All seven cases pass in 110.317s across 28 children, and seven fresh audits pass
in 24.221s. The current helper's injected post-launch exception cleanup passes
in 4.833s. All 36 qualified generations and their 36 owned temporary directories
are independently absent. All seven retained SQLite databases pass integrity
checks; three earlier fixture/superseded-helper generations are also absent.
Two setup failures (connection configuration and ulong SQL ordering) are retained
separately. A later blanket two-receipt audit assumption was corrected to the
already-enforced phase-specific receipt counts; it was not a production fault.

The initial test/runner builds are clean in 14.71s/2.65s; final builds are clean
in 14.40s/2.64s. Six existing default public bid controls pass in 12s against the
initial candidate. The linked record holds exact build times and both 138-DLL
manifests; only the test DLL changes between candidates. No owning production
build, full solution or full suite was repeated. The 6291 tracked Source comparison
retains only eight historical candidate variants, plus this new test entry.
Both final review axes have zero findings.

The [portable record](coverage/normal-bid-process-recovery-20261106.json) binds
the new test Source, process gates/identities, both candidates, state results,
cleanup control, independent absence/integrity checks and reviews. Raw evidence
is under artifacts/continuation-20261106/normal-bid-process-recovery/.

This uses prepared real Player/session/registry/metadata collaborators and seeded
synthetic catalogs/listing, with CharacterDatabase.GetCharacters, CurrencyManager
and marketplace Initialise on fresh processes. It does not qualify autonomous
hosted World restart, Auth/character selection, socket/client relog, actual
catalog delivery, MySQL/copied upgrade, database-server loss, other auction
settlements, broker/deployment/load or native/retail behavior. Client launch and
Computer Use remain stopped. Continue the broader gameplay/progression queue.

## Synchronous fallback rejection replies

6 November 2026, parent 8240e77735cdeb85c9f41553071ceade21045505.
The five public auction bid/post/cancel and commodity post/cancel handlers now
retain rejected results on real-Player synchronous fallback paths. After the
durable route declines, they capture the existing Game-owned publication
lifetime before invoking the legacy operation. Both branches use the same typed
callback. Two concrete factory methods are exposed across the existing
World/Game helper boundary; the generic retry implementation is unchanged.

Failed result enqueue waits one second on the original actor queue. Each attempt
uses a fresh captured DTO. Existing Player, Character/account identity, session,
queue, current registry, logout/disconnect and publication guards stay in force.
Auction countdowns are rebuilt from their captured deadline; commodity FILETIME
values retain their existing snapshot behavior. Reply retry invokes no financial
operation, save, debit, item removal or settlement. Successful callbacks keep
their immediate single-send behavior and existing wire shape.

The clean unchanged-production baseline fails seven of nine new parsed-handler
cases: all five result sends lose their rejection, plus the Player/queue
replacement cases propagate the failed send. Ordinary rejection and a real
Player transient bid are passing controls. Final evidence passes all nine new
cases within 234 affected cases, plus 58 disjoint durable purchase controls:
292 unique cases, zero failures/skips. Failed DTO mutation cannot change the
retry's serialized bytes. All five negative routes have zero currency/inventory
mutations; the successful transient bid debits 100 once and publishes immediately.
Original Player/queue replacement suppresses later retry.

Owning WorldServer/test-only builds are clean in 18.58s/14.14s. The two scoped
executions report 69s/64s; no full solution or full suite ran. Both Source review
axes have zero findings. The Game, WorldServer and test DLLs change; the test
assembly was rebuilt after public factory/helper API changes and is not claimed
byte-identical to baseline. All three reviewed Source files and owning/test
copies match the retained manifests. The 6290-file tracked Source comparison
contains two owned changes and eight retained historical candidate variants,
plus the new test; foreign root Source was excluded.

Initial fixture RBAC omissions, a helper-name compile failure and an avoidable
stale-DLL attempt are retained separately and excluded from the clean baseline
and final qualification. No production fault is inferred from those diagnostics.

The [portable record](coverage/marketplace-fallback-replies-20261106.json) binds
the three Source paths, baseline/final binaries, scoped TRX files, reviews and
unchanged 20-PR upstream refresh. Raw evidence is under
artifacts/continuation-20261106/marketplace-fallback-replies/.

The new fixture uses a real Player type with prepared session/account/registry
and proxy inventory/currency collaborators, real parsed public handlers,
transient book and EventQueue. It does not prove full initialized gameplay,
durable transient finance, physical socket failure, cross-process recovery,
fresh provider behavior, deployment or retail. Existing SQLite coordinator
results keep their separate supported settlement scope. Broader financial and
request/reply work remains; client launch and Computer Use remain stopped.

## Posting and cancellation rejection replies

5 November2026. The real Player-backed public auction posting/cancellation and
commodity posting/cancellation routes now retain rejected result callbacks.
Together with the purchase correction below, five public routes share one private
actor/retry implementation. Two concrete payload adapters retain their existing
wire semantics: auction expiry is recalculated from a captured deadline, while
commodity FILETIME timestamps are copied unchanged. Each failed-send attempt gets
a fresh DTO. Successful replies keep each coordinator's authority and ordering.

Four real public-handler baselines failed after an enqueue rejected before
acceptance: three exceptions reached the event queue, while auction posting lost
its final rejection. The new cases exercise posting rollback, auction and
commodity custody conflicts, and insufficient funds. They mutate the failed DTO
and verify the retry is isolated, with account locks released and the existing
financial/custody assertions retained. All four new cases and203 selected cases
across the five coordinator suites pass, with zero failures/skips. Both final
review axes have zero findings. Game build:33.33s; test compile:21.47s; final
scoped execution:82s. These are observed validation times, not performance claims.

The [portable report](coverage/marketplace-error-publication-20261105.json) binds
the nine changed Source files, all6345 Source/Tools inputs, matching owning/test
DLLs, baseline/final scoped results, reviews and incremental upstream refresh.
Local evidence: `artifacts/continuation-20261105/marketplace-error-publication/`.
No SQL/model/migration changed, no service was started and no new configured
provider or full-suite run is claimed. Legacy/transient fallback errors,
physical socket failures, cross-process replay, deployed behavior and retail
remain separate. Client/CUA stay stopped; entitlement defaults/no-auto-grants
remain unchanged. Continue the broader roadmap after this grouped integration.

## Recovery for rejected public purchase replies

5 November2026. Public bid/buyout errors now retain their reply after an enqueue
rejected before acceptance. This includes immediate admission rejection,
insufficient funds during preparation and rejected SQL settlement. Successful
replies keep the existing coordinator's listing authority and ordering.

The rejection route captures the original Player, Character/account identity,
session and event queue. It checks those owners, the account's session, current
player registry, disconnect/logout state and supplied publication guard before
each attempt. A failed send waits one second on that original queue. Each attempt
gets a fresh DTO from the captured rejection view, with its expiry recomputed
from the captured deadline. Mutating the first failed DTO or reusable request
cannot alter the retry. Financial settlement and reservation release finish
independently; a reply retry neither saves nor restarts a financial operation.

Five baseline cases reproduced an IOException escaping to the buyer event queue
and losing the rejection. All six new cases now pass, including original-player
and queue replacement suppression, normal-bid and buyout SQL failure, preparation
failure, and admission rejection with zero Character saves or bid receipts.
All112 selected public coordinator/auction compatibility cases pass in76seconds.
The owning Game build passes in37.53seconds; final test compile takes20.49seconds,
both with zero warnings/errors. Both final review axes have zero findings.

The [portable report](coverage/auction-error-publication-20261105.json) binds
the two changed Source files, all6344 Source/Tools inputs, matching owning/test
DLLs, baseline and final scoped results, review and incremental upstream check.
Local evidence: `artifacts/continuation-20261105/auction-error-publication/`.
No SQL/model/migration changed, and no new configured-provider run is claimed.
Other marketplace error producers, physical socket loss, cross-process replay,
deployment and retail qualification remain open. Client/CUA remain stopped and
the entitlement defaults/no-auto-grant rules are preserved.

## Recovery for the committed buyer result

4 November 2026. Three public-handler cases reproduced an exception reaching
the buyer's event queue after the ordinary bid and its wallet settlement had
already committed. The buyer lost the successful result.

The original reply still reports the accepted outcome as soon as settlement
finishes and releases the listing and account reservations. If enqueue fails,
the original buyer's event queue retains the reply. A retry
waits through a competing local settlement and sends only while the captured
listing still applies, its deadline is in the future, and the original buyer
and session remain current. Each send gets a fresh copy of the accepted bid
with the countdown calculated from the listing's absolute deadline.

The retained Carbine listing handlers route a successful `ItemAuctionBidResult`
through `OnItemAuctionUpdated`, which replaces or inserts the auction by ID.
The auction browse handler also displays the accepted message and refreshes the
search. This is why a delayed success cannot carry an older listing through a
superseding bid. See the [client consumer evidence](#client-consumption-of-settlement-notices).

The private SQLite regressions exercise retry after payload mutation, replacement
of the original session's player, and suppression after a newer bid is adopted
during reconciliation. They verify the original wallet debit, receipt counts,
and ordinary save before the reply. The first candidate passed 257 scoped
auction cases in 85 seconds. Review identified the relative expiry issue.
After correction, all 45 public bid coordinator cases passed in 61 seconds,
including the decreasing retry countdown. Local evidence is in
`artifacts/continuation-20261104/normal-bid-result-publication/`.

See the [ordered buyout result and won notice](AUCTION_BUYOUT_TRANSACTION.md#ordered-buyer-result-and-won-notice)
for the separate terminal-removal case. Error replies, physical socket loss,
cross-process replay and retail remain open.

## Guarded live outbid retry

4 November 2026. A transient send failure on the original outbid recipient
previously reached the event queue after the persisted bid or buyout committed.
The settlement released its reservations and the outbid notice was lost.
The public handler baseline reproduced that failure in four cases.

Outbid publication now uses the same retained notice path as the terminal
auction notices. It retries on the original live actor and queue, and sends
a fresh packet from the captured auction snapshot. Settlement releases its
reservations and sends the buyer's reply independently. The owning order book
waits through a later in-flight settlement for the same auction. A rejected
later bid leaves the original notice eligible. A changed listing suppresses
the old notice, which could otherwise remove the refreshed entry by auction
ID. A buyout requires explicit listing absence from reconciliation and
continued absence in the local order book. Later wallet edits do not
invalidate that listing proof. Session replacement drops the old callback.

The tests use the registered public bid handler with private SQLite and
original player actors. They cover normal bid and buyout retry, rebinding,
a held and committed rebid, a held rebid rejected by SQL, and a buyout
with a competing wallet edit. The first candidate suppressed that last
notice, which the new case reproduced before the listing proof was narrowed.
The owning build and 209 scoped cases passed, including all six public-flow
regressions. The scoped tests ran in 57 seconds. Local evidence is in
`artifacts/continuation-20261104/auction-outbid/`.
Cross-server and restart replay, error reply retry, and wire or
retail qualification remain open. See the later [committed seller notice](AUCTION_BUYOUT_TRANSACTION.md#committed-notice-for-the-original-seller).

## Buyer reply with a paused outbid queue

4 November 2026. The public bid handler still waited for the previous
bidder's first notice attempt after the debit, refund and wallet
reconciliation finished. Two baseline cases paused that actor after the
refund and reproduced the blocked buyer reply.

The coordinator now releases the listing and account reservations once
reconciliation finishes, then queues seller and outbid notices on their
captured actor queues. The buyer receives the accepted bid result even while
the previous bidder's queue is paused. The original queue can send the
outbid notice when it resumes and the listing still matches. A replacement
queue drops the pending notice. The listing, escrow item, debit and refund
are checked in the public-handler cases.

The owning test-project build passed and 226 scoped auction cases passed,
including both regressions. The build took 35 seconds and the tests took
88 seconds. Local logs, TRX results and source/DLL bindings are in
`artifacts/continuation-20261104/auction-outbid-independent/`.
Cross-process replay, deployed delivery and retail remain open.

## Client consumption of settlement notices

4 November 2026. The retained build 16042 image
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`
selects these cases from the entity dispatch table at `0x1403f1344`.

| Opcode | Table slot | Case | Event |
| --- | --- | --- | --- |
| 0560 | `0x1403f2740` | `0x1403f0d55` | `ItemAuctionBidResult` |
| 0561 | `0x1403f2744` | `0x1403f0cf3` | `ItemAuctionExpired` |
| 0562 | `0x1403f2748` | `0x1403f0cc7` | `ItemAuctionOutbid` |
| 0564 | `0x1403f2750` | `0x1403f0cdd` | `ItemAuctionWon` |

The outbid, expired and won cases pass the decoded object through the same
event-dispatch branch at `0x1403ecfef`. The bid result passes the first
DWORD and a pointer to the payload at +8. The native `Game.ItemAuction`
`__eq` binding points to `0x1406a0270`. It compares the two objects' +8
QWORDs, excluding the both-zero case. The cancellation producer at
`0x1406a20c0` uses the same QWORD for the auction ID sent in 0094.

Both archived Carbine listings scripts route outbid, won and expired to
the same removal callback. It removes an equal auction from the current
list and redraws. The auction script also emits a message containing the
outbid amount, and distinguishes an owner sale from a winner purchase in
the won handler. One archived variant additionally requests updates after
an outbid. The archives corroborate these consumers. Their exact patch
provenance and the original server's emission timing remain unverified.
[Listings archive](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/MarketplaceListings/MarketplaceListings.lua#L453)
· [Auction archive](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/MarketplaceAuction/MarketplaceAuction.lua#L1383)

An outbid retry therefore needs the original recipient and a check for a
superseding bid on the same auction. Replaying an old notice after the
recipient bids again can remove that auction from the refreshed listings.
The current committed transfer path sends `ServerAuctionWon` to the
receiver. The owner-sale branch identifies a separate seller publication
candidate now covered by the later [committed seller notice](AUCTION_BUYOUT_TRANSACTION.md#committed-notice-for-the-original-seller).
The later outbid guard is recorded above.

The pinned PE reads, LLVM excerpts and four byte-verified addon files are
retained under `artifacts/continuation-20261104/auction-consumer/`. This is
static consumer evidence. Client execution, socket delivery, replay timing
and retail parity remain open.

## Original bid settlement

1 November 2026. Qualified source/private-SQLite boundary11,095: zero full-test
failures/skips, zero build warnings/errors and zero remaining findings on either
review axis. SourceTools pin d212179e32372141aad6dbc8423a4cf7b67dfe61fac093f74e47f418135efc11.
The [4 November MySQL qualification](DESTINATION_PROVIDER_QUALIFICATION.md#normal-bid-and-buyout-sql-settlement)
adds migrated-schema command and reconciliation evidence. Public-flow,
deployment and retail gates remain open.

The existing `ClientAuctionBuyOrderSubmitHandler` now sends persisted normal
bids through one owned operation. Its asynchronous callback retains the three
request scalars, original player/session and logger. Buyouts retain their
existing path. Original-actor complete ordinary saves precede wallet capture;
the loaded or confirmed-created Character/account/database identity authorizes
each currency owner. Self-bid increases debit only the difference.

The SQL command freezes listing/escrow preimages, wallets, Character owners,
refund-mail identity and command identity. It locks affected Characters in a
deterministic order, checks custody/deletion/expiration and writes listing,
buyer debit, online prior-wallet credit or offline refund mail and outcome
receipt in the same serializable transaction. A lost acknowledgement triggers
a fresh receipt observation; uncertain observations retain ownership. Later
SQL state is read and adopted without rewriting the historical bid outcome.

Original actors adopt the confirmed balances, apply deferred semantic currency
intent and persist their complete ordinary Character graphs before publishing.
Those saves have their own complete-graph receipt. A fresh locked receipt
absence proves rollback and allows the same frozen graph to retry on its
original actor, without repeating Auth saving or earning effects. Partial
enrollment stays owned too; unsealed flush/commit remains forbidden. Failed
observations retain the original owners rather than guessing rollback.

Account admission/deletion and listing mutations exclude the held operation.
Retired actors finish persistence while suppressing wallet/outbid replies;
replacement sessions receive no original request result. Separate actor queues
perform each actor's preparation, adoption and publication. No worker calls
live gameplay callbacks. Confirmed creation provenance is a prerequisite within
this milestone; it does not redesign creation-token settlement.

Evidence is local in `artifacts/continuation-20261031/normal-auction-bid/`:

- `public-enrollment-reproduction.trx`: six public-flow controls pass; two
  enrollment faults fail with SQL120 versus125 after abort and110 versus115
  after commit. The later+5 intent was not durable.
- `public-enrollment-corrected.trx`:43 pass, including both corrected faults.
- `public-lifetime-boundaries.trx`:113 pass, including public first/self/online/
  offline bids, main-acknowledgement loss, retirement/replacement, account
  exclusion, partial enrollment, rejected early flush and existing auction tests.
- `integrated-affected.trx`:1,037 pass; adds the insufficient-funds compatibility
  regression and existing bank/mail/Contract/Challenge/currency/migration cases.
- `masked-state-baseline.trx`: eight actual failures/eleven controls for a
  masked Player field cleared before a rolled-back save. The earlier19-case
  `masked-state-reproduction.trx` fixture query failure is excluded.
- `retained-graph-corrected.trx`:52 pass. `legacy-newer-intent.trx`: four pass,
  covering actual legacy keybinding creates/updates/deletes, tradeskill/schematic
  mutations and newer explicitly modified fields winning over retained values.
- `review-fixes-affected.trx`:391 pass, including consecutive rollback and an
  online prior bidder with a known-empty wallet.
- `replay-boundary-negative.trx`: two actual failures for incomplete retained
  insert merging and a foreign stored Item preimage. The latter is a raw
  reconciliation-layer control, not a public Inventory exploit reproduction.
- `replay-boundary-corrected.trx`:61 pass. Final review axes have zero remaining
  findings at SourceTools pin1d5bd36087005ef90182ab39ef997c389f0ca19396bdcb0f9863a46fbe00862d.
- Three full builds passed with zero warnings/errors, but full tests remain
  unqualified: `failed-full.trx`11,086 passes/six failures;
  `second-failed-full.trx`11,091 passes/one failure;
  `diagnostic-failed-full.trx`11,092 passes/two failures. Five initial readiness
  failures were corrected as a cleanup/admission fixture mismatch. Intermittent
  Challenge drain/teardown failures remain unresolved; isolated reruns passed.
  The third full observed one managed reward connection open during the wait,
  without identifying its exact SQL phase or native resource holder.
- Phase-only diagnostics and producer/routing coverage pass102 cases;28 healthy
  cost controls pass, with2.627–5.061ms total origin capture per traced fixture.
  Both review axes have zero findings for the diagnostic delta. `phase-full.trx`
  passes11,095 cases at SourceTools pin
  d212179e32372141aad6dbc8423a4cf7b67dfe61fac093f74e47f418135efc11;
  its full build passed with zero warnings/errors in29.19seconds. Full tests
  took11m40s (command712.27seconds). The6,131-input archive and nine DLL pairs
  verify in `verification.json`. No timeout cause is inferred. Detailed failure records
  and limitations are retained in the local `CHALLENGE_TIMEOUT.md` artifact.
- `review.diff` compares this consolidated milestone against the qualified
  wallet11,019 archive. Earlier failed fixture/build experiments remain saved.

The MySQL and SQLite migrations add the three-column `marketplace_bid_receipt`
table. Generation/model/source checks do not qualify an installed MySQL schema,
configured-provider execution strategy, upgrade, socket or native operation.
Actual provider/table/gameplay evidence remains open. The historical handoff
checker deliberately rejects changed Source; its recorded checkpoint result is
reused under the continuation policy.

Remaining economic work includes buyout, proceeds/direct item delivery and other
multi-party operations. Contract public07CE claim/reset remains disabled until
unclaimed/excess-point rules and all726 canonical reward choices are qualified.
Entitlement defaults15:1/17:1 remain; no44 or seven baseline grants, native client
launch, setup/configuration, commit or push is authorized by this record.
