# Normal auction bid transaction

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

Buyout and error replies, physical socket loss, cross-process replay and retail
remain separate. A successful buyout reply also reaches the client's listing
update handler, so its recovery needs to account for the terminal removal.

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
Cross-server and restart replay, buyout and error reply retry, and wire or
retail qualification remain open. See the later [committed seller notice](AUCTION_BUYOUT_TRANSACTION.md#committed-notice-for-the-original-seller).

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
