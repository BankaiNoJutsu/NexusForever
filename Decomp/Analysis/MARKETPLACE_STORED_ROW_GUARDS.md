# Marketplace stored-row and expiry guards

Date: 1 November 2026. Source/private SQLite evidence only. Full solution and
test-copy rebuilds pass with zero warnings/errors. All10783 full tests and154
rebuilt marketplace cases pass with zero failures/skips; all297 prior reviewed
mail cases remain in the rebuilt full run. All earlier cases remain,34 new cases
are included, and nine6105-input archives/eight owning/test-copy DLLs verify
against the final review pin. Local verification.json binds the exact evidence.

## Reproduced and implemented

Previously, missing auction/order updates logged and returned from their save
callbacks, and deletes silently skipped absent rows. SaveBlocking could report
success while stale memory debited a bid, refunded an order, returned escrow or
delivered a commodity fill. Expected persisted update/delete now throws through
the existing failure boundary when its row is absent.

New non-immediate commodity records enter the memory book before their initial
SQL insert. RequiresStoredRow distinguishes those pending incoming records from
loaded or successfully inserted records. Fill staging skips pending incoming
rows; insertion skips completely consumed incoming records instead of resurrecting
them as zero-quantity listings. Plain in-memory-only mode remains available.

Review found two further bypasses and both were executed before correction:

- Direct unbid expiry returned Items/notified/removed memory before checking its
  delete result. It now confirms deletion first, restores Item ownership and
  retains the memory record when persistence fails.
- Capacity can change between the caller and fallback. Direct auction fallback
  now confirms deletion and offline participant credits before Item delivery,
  and reports completed persistence to its caller. Its winning-expiry path also
  preserves offline seller proceeds previously omitted on this transition.
  Commodity fallback keeps a supplied fill callback in the initial mail decision;
  later free space cannot bypass resting-order/credit/Item/header/link writes.
  No-callback callers retain their direct inventory path.

## Executed evidence and corrections

Local artifacts: artifacts/continuation-20261031/marketplace-rows.

- Initial14:7 failed/7 passed against unchanged production. The auction fixture's
  inherited17-item stack was corrected to match its one-item listing; the
  separately frozen corrected14 reproduced7 failed/7 passed. Original retained.
- First production guard:14 passed. The next22 exposed2 actual direct-expiry
  failures and1 test-fixture error. A property-set constant mail ID overrode the
  test counter factory. A fresh counter-only proxy corrected it; the failed
  fixture run and its exact inputs remain separate.
- Expanded auction28:7 failed/21 passed, including the expiry/capacity failures
  and missing offline seller proceeds. Corrected28 passed.
- Expanded commodity34:6 failed/28 passed, including partial/full missing and
  actual SQL UPDATE/DELETE faults plus retained-row proceeds controls.
  Corrected34 passed. Positive controls accept one delivery through inventory
  or item mail and require persisted seller proceeds, rather than mirroring the
  selected branch. Twenty distinct real failing case names are preserved across
  the corrected original, auction-capacity and commodity-capacity baselines.

The initial packet test draft attempted read-only property assignments and did
not compile. It was corrected to parse an actual request body before execution.
An interpolated test-trigger analyzer warning was replaced by two fixed SQL
statements before executing the commodity baseline. These logs remain explicit.

Final two-axis review reports zero findings at Source/Tools SHA256
fe1181981824baa3159385f22e170cf4833e015627a2f0f0603d9500526927f5.
Prior review findings and snapshots remain recorded separately. The provisional
commodity-fixed freeze was renamed from its generic placeholder label before
qualification; archive contents were not changed.

## Remaining requirements

Presence does not reject an extant rewritten row or provide a complete CAS.
Wallet debit/refund/proceeds, direct Item delivery and book/reply publication
still need complete captured settlement and outcome recovery. Known failures can
still emit provisional wallet deltas before compensation. Post-commit direct
delivery exceptions and SaveBlocking lost acknowledgement are not resolved.
Do not blindly replay public operations, ID allocation or arbitrary callbacks.
Cross-process ownership and durable consumed-packet replay remain separate.

No fresh configured MySQL/upgrade/deployment/socket/load/native/retail completeness.
Computer Use/native launch remain stopped. Defaults15:1/17:1, no automatic44/seven
baseline grants and no push remain. Continue economic recovery, Contract and
broader feature/evidence requirements after this bounded slice.

Upstream03:30UTC refresh:55 open PRs,608 newest open.609 closed without merging
at02:58:37UTC, with unchanged head and no explanation returned by the comments
query. All55 open heads, retained609 and both branches remain unchanged. The
all-state created-since29 October query returns609/608 only; merged query returns
598 only. See the609 reconciliation record for limits. The historical handoff
verifier was rerun and rejected evolved Source at checkpoint10277 equality;
the retained failure does not replace this current binding.
