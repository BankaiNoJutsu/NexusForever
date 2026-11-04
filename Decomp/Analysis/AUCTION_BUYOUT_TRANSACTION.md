# Auction buyout transaction

## Committed notice for the original seller

4 November 2026. The mapped `0564` event carries the auction to the Carbine
won handler. Its seller branch uses `Game.ItemAuction.IsOwned`. In the pinned
build 16042 PE, the binding at `0x140c5d510` leads to `0x1406a1fc0`, which
compares the decoded owner at +0x10 with the local player. This corroborates
the seller consumer. Original retail server timing remains unobserved.
[Archived won handler](https://github.com/NexusInstruments/CarbineAddons/blob/cf063b3e22b08f49ff51c12cec258a9f1cf86d68/MarketplaceAuction/MarketplaceAuction.lua#L1383)

Confirmed buyouts and winning expiries now send the sold notice to the original
online seller after actor reconciliation and proof that the listing is gone.
The notice retains the accepted auction and uses the original player, account,
session and queue. A transient send failure retries there. A retired or
rebound session drops the notice. Buyer replies and reservation release proceed
independently. The zero-proceeds path captures the notice recipient without
enrolling a seller wallet or ordinary save. An offline buyer can still settle
by mail while the online seller receives the notice.

The retained baseline shows six missing notices. The tests cover live seller
publication, retry, rebinding, retirement, offline buyers and the zero-proceeds
public buyout. The owning build and all 212 scoped cases passed. The scoped
tests ran in 58 seconds. Evidence is in
`artifacts/continuation-20261104/auction-seller/`. Qualification covers the
persisted auction producer and the live session enqueue boundary. Legacy
adapter paths, cross-server and restart replay, and retail behavior remain separate.

## Original buyout settlement

1 November 2026. Qualified source/private-SQLite boundary11,141: zero full-test
failures/skips, zero build warnings/errors and zero remaining findings on either
review axis. SourceTools pin
6dcc49b5662fde0b6fe3a80639342658f28fae04ccc3baeb82d7214a2938731d.

The [4 November MySQL qualification](DESTINATION_PROVIDER_QUALIFICATION.md#normal-bid-and-buyout-sql-settlement)
adds migrated-schema command and reconciliation evidence. Public-flow,
deployment and retail gates remain open.

The existing public ClientAuctionBuyOrderSubmit flow now captures a buyout at
its accepted price and prepares the original buyer, online seller and online
prior bidder. Complete ordinary preflight precedes wallet capture. The shared
AuctionPurchaseCommand atomically removes the listing, transfers the original
whole Item into an exact inventory slot or fixed mail attachment, debits the
buyer/self-bid difference, refunds the previous bidder and pays seller proceeds.
Existing fees, caps, offline credit mail, binding and Item IDs remain intact.
Stored zero charges remain zero; existing constructor/live/wire hydration is
preserved. No new Item initialization, ID allocation or binding grant occurs.

The command freezes all scalar preimages, delivery and mail identities. It locks
Characters in deterministic order and validates complete financial/custody
preimages and SQL postimages. The durable outcome receipt resolves lost main
acknowledgements. A fresh locked current-state read precedes delivery adoption.
Original actors retain later currency intent and persist complete ordinary
Character graphs before final success. Incoming packet admission stays under
the held auction owner through reconciliation, retirement and quarantine.

An immutable SQL-only delivery witness includes the Item, mail header and link
in the ordinary reconciliation receipt. A changed current custody row raises
an exact-witness conflict. Only a proven rollback lets the original actor drop
that witness, detach its exact admitted Item if necessary, and retry the
remaining frozen ordinary graph. Later currency intent stays durable; the
inventory then requires reload and no stale delivery is published. Custody
exclusions remain checked through flush and precommit. Admitted delivery holds
inventory expiry ticks until publication/release, preserving accumulated time. Unknown
outcomes retain ownership. Original actor/session guards are checked between
wallet packets and Item publication and remain attached to deferred mailbox
publication. Replacement actors receive no original operation packets.

Evidence is local in artifacts/continuation-20261031/auction-buyout/:

- buyout-public-negative.trx: one executed legacy split-settlement failure,
  twelve normal-bid controls. SQL100 instead of110 at public success.
- direct-buyout-integration.trx:60 passes. expanded-public.trx:26 passes/one
  replacement won-packet failure. custody-and-publication.trx:86 passes/one
  quarantine incoming-gate failure. Those corrections pass complete-focused94.
- affected-integration.trx:3,814 passes at the initial reviewed candidate.
- review-boundaries-negative.trx:three executed review failures/32 controls:
  wallet enqueue replacement leaked an Item packet; deferred mail reached a
  replacement; later mailed custody changed but public success was still sent.
- review-fixes-focused.trx:142 passes. complete-witness-focused.trx:145 passes,
  including later direct Item, mailed Item, header and attachment-index changes,
  ordinary rollback/lost acknowledgement/partial enrollment, direct/mail,
  online/offline/self refunds, online seller cap and existing fee boundaries.
- second-review-negative.trx:three actual failures/38 controls: obsolete cached
  delivery write retried after custody moved; duplicate slot occupant/extra mail
  link committed during ordinary flush. Exclusions now remain validated through
  commit; exact proof/preimage/canonical-postimage retirement removes only the
  obsolete delivery write. Unrelated retained intent remains protected.
- expiry-boundary.trx retains one actual timed-delivery failure plus five cases
  exposing a disposed-context access in the initial retirement correction.
  Elapsed expiry is now retained until admitted delivery publishes/releases.
- codec-retirement-negative.trx:one actual proof-boundary failure/two controls.
  Arbitrary socket intent could retire. Exact canonical-value checks now reject
  that retirement and preserve the edit through retry; no remote exploit claimed.
- retained-custody-corrected.trx329 and codec-retirement-corrected.trx330 pass;
  final-affected.trx720 pass, with earlier3,865 affected passes retained.
- Final review axes have zero remaining findings. Full solution build has zero
  warnings/errors in18.52seconds. The6,132-input archive and nine owning/test DLL
  pairs verify at the reviewed pin above. full.trx passes11,141 cases in12m13s (command744.82seconds). verification.json
  confirms unchanged Source, archive and all nine DLL pairs after the full run.

This is source/private-SQLite evidence. Live MySQL/upgrade/deployed schemas,
configured-provider strategies, actual tables, sockets/native/gameplay and retail
qualification remain open. Existing intermittent Challenge drain/cleanup failures
are retained with unproven cause; the preceding normal-bid full gate passed.
Historical handoff Source divergence is expected and its recorded result reused.
No setup/configuration, native client launch, commit or push. Defaults15:1/17:1;
no automatic44 or seven baseline character grants.

Next integrated economic outcome: settle auction expiry,
including held-bid proceeds/delivery and unsold returns, through its real producer.

Upstream17:14UTC metadata finds no PR updated since09:47UTC;604/608heads remain
36b59aaaaebbcd0ec3e23a94fd3cf9de8975923b/7db12d9a7c694bc5e66d9e0f4cdbc1b82155e69b,
609 remains closed unmerged atf9b6f7356798a41beea0768ff1f6b5a758ffeca3.
Reuse unchanged reconciliation evidence; no missing newer PR was returned.
