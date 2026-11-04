# Auction cancellation settlement

## Recovering the committed item return and cancellation result

4 November 2026. The public cancel handler now recovers an enqueue failure
after the Character commit. A failed direct ItemAdd previously could report
`DbFailure` for an already committed return. A failed cancellation result
escaped the actor event and the reply was lost.

The live retry resumes at the unsent stage after account and listing
reservations release. A direct return sends the item before the successful
cancellation result. The item retry uses the exact returned object in its
current Character-owned slot and builds a read-only packet snapshot. Listing
absence controls the cancellation result separately. A replacement listing
can make the result obsolete while the item return remains current. Actor or
event-queue replacement ends publication for the old request. Item retry
waits through active inventory save blocks and excludes Realm Bank custody.
Mail delivery keeps its existing committed publication path.

The baseline reproduces the item-add and result failures. The initial 219
affected cases passed in 93 seconds. After review, the read-only item projection
and listing-authority revision passed 164 affected cases in 57 seconds. The
final build and all 23 cancellation cases passed in 46 seconds, including five
new public-handler cases. Evidence and final binary identities are retained in
`artifacts/continuation-20261104/auction-cancel-publication/`. The scope is
source and private SQLite recovery on the live actor. Cross-process and restart
replay, deployed delivery and retail qualification remain open.

## Original cancellation settlement

Public ClientAuctionCancel now retains copied request scalars and the original
owner/session, then queues one owned settlement. Its transaction removes the
listing, returns the same whole Item directly or through a fixed return mail,
and refunds the full held bid to the original online bidder or offline credit
mail. It charges no owner debit and pays no seller proceeds. Existing fee/cap,
Item ID, binding/socket/charge semantics and cancellation packet behavior remain.
No outbid, won or expired notification is added to cancellation.

The CancelledReturn command validates stored owner/held-bid roles, zero charge,
complete Character/account/database preimages and direct/mail custody. Original
actors use complete preflight and ordinary reconciliation, current-state recovery,
retained receipts and delivery witnesses. Zero-change absent owner wallets remain
absent unless later intent creates them. Original retirement/replacement and
reentrant deferred mail publication stay bound to their captured actors. Listing
and account reservations exclude competing purchase, expiry and cancellation.
Legacy IPlayer adapter behavior remains through the existing helper route.

Evidence: artifacts/continuation-20261031/auction-cancellation/.
- producer-negative.trx retains11 failing public-handler reproductions: success
  could observe SQL100 instead of dirty ordinary wallet120 (and other retirement/
  reconciliation boundaries); initial.trx passes11 after integration.
- focused.trx passes166 shared bid/buyout/expiry/cancellation/actor/receipt cases.
- boundaries.trx passes16 public cancellation cases, including direct/full-bag
  mail, held/no bid, online/offline refund, absent wallet/later earning intent,
  main and ordinary lost acknowledgements, proven ordinary rollback, retirement,
  replacement, deferred-mail replacement, competing requests, custody conflict
  and owner/item refusals. Command tests add14 financial-role and settlement cases.
- An intermediate build overlapped focused tests and failed to copy their loaded
  DLL (MSB3027/MSB3021). No process/file intervention was used. The sequential
  candidate build passes with zero warnings/errors in19.75seconds.
- Both final review axes report0 findings at
  pin1c24292f474dc7eae868aca3c27e4d23a99bfa5a8080f7e80e61db97f880ba7f.
  affected.trx passes545 cases in2m18s. Full solution build passes0warnings/errors
  in24.76seconds; full.trx passes11,203 cases in12m57s (command789.02seconds).
  verification.json verifies6,135 inputs, nine owning/test DLL pairs and archive
  SHA2560c8c766e5c350ca9bc4ed4c5605749b73568be3724bfa3d2c85021b10053211e.
  No Source/build changes overlapped the full run. Narrow tests total roughly
  five minutes across development; full validation dominates this milestone.
  Group shared commodity cancellation/expiry behavior in the next candidate.

No migration or provider configuration changed. SQLite/source evidence does not
qualify configured MySQL strategies, deployment, actual tables, sockets/native,
gameplay or retail. Reuse historical continuation-check Source divergence.
Entitlement defaults15:1/17:1 remain; no automatic44/seven baseline grants.
Computer Use/native launch remain stopped. No commit or push.

18:45UTC all-state upstream search finds no PR updated since18:16UTC. Reuse the
unchanged604/608/609 reconciliation records and head metadata. Next complete
commodity cancellation/expiry returns and refunds, then the remaining economic,
Contract and broader feature queue. The Goal remains active.
