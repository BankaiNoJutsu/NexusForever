# Pending account-item durability

2026-10-29. This is the persistence prerequisite for reward-track claims. It adds
no reward track, earned currency, entitlement, or default grant.

## Behavior and transaction boundary

AccountInventoryManager previously deleted every persisted pending row for the
account and reinserted its memory snapshot. A stale online account could discard
an externally appended grant or resurrect an externally consumed row. The manager
now saves only owned differences against its committed per-object baseline:
new rows, changed fields, and known row deletions. Unrelated columns and newly
appended rows survive. Missing existing rows are treated as consumed, including
when a stale owner had queued an update or deletion.

Snapshots are acknowledged after actual outer commit. Rollback, context/transaction
disposal, failed flush and savepoint uncertainty retain intent for a fresh unit.
One in-flight unit per manager avoids duplicate staging; additions, updates and
removals occurring during that unit remain differences against its committed
snapshot. Caller-owned transactions retain ownership of their commit and rollback.

AuthContext stages pending work inside its save transaction. When it owns that
transaction, the provider execution strategy wraps the entire unit; tracked
changes are accepted after commit. A durable save-unit witness verifies an
ambiguous successful commit before retrying, so unrelated tracked inserts in the
same transaction are not replayed. Failure exceptions remain observable.

## Identity and migration

The account's durable nextPendingItemId counter and account-row database lock are
shared by online and offline append paths. The first reservation also reconciles
the counter above existing pending IDs. Consuming rows never reduces the counter;
fresh allocations cannot reuse a removed row's identity. MySQL uses current locking
reads, including when a caller-owned transaction already has a consistent snapshot.

account_pending_item_delivery stores stable append receipts after pending rows are
consumed. Repeating an append key returns its committed identity, and a receipt
whose pending row is absent means already consumed. This makes uncertain completion
retry safe even when a savepoint rollback left earlier writes committed. Separate
save-unit: GUID receipts reserve pendingItemId zero as a commit witness; zero is
never inserted as a client pending item. These durable receipts are intentionally
retained rather than removed with their pending rows.

The MySQL migration is 20261029200000_AccountPendingItemSequence; the SQLite
migration is 20261029165726_AccountPendingItemSequence. Both preserve existing
account and inventory rows. Their provider snapshots are updated. Existing pending
API group names remain unchanged. If allocation changes a previously announced
provisional numeric cache ID, the server deletes and re-adds only the affected
client group with its committed IDs.

## Verification

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter 'FullyQualifiedName~AccountPendingItemDurabilityTests|FullyQualifiedName~AccountInventoryPendingGroupTests|FullyQualifiedName~SqliteMigrationSmokeTests'
```

43/43 pass with zero warnings/errors. The assessment report retains
remaining-goal/pending-delta-focused.log. Regressions use real independent SQLite
contexts for external append/consume, known-row deletes, per-field updates,
concurrent manager/repository allocation, failure/retry, outer transaction
commit/rollback/disposal, both savepoint positions, newer in-flight mutations,
sync/async failures before and after commit, and preservation of an unrelated
purchase-history insert through ambiguous commit. Fresh/populated SQLite upgrades
and MySQL snapshot/generated migration consistency are exercised. No live MySQL
migration or runtime validation is claimed by this focused gate.

Independent review found and verified two further cache regressions: a newer
announced provisional row colliding with an earlier unit's committed ID, and a
silent addition later advertised by a full snapshot. Completion reconciles live
identity uniqueness, deletes all affected client groups before any re-add, and
tracks actual snapshot/group emission. Both staging orders are covered.

The broader existing account, shop, storefront, gift and reward filter also passes
661/661 before the three cache follow-up cases, with zero warnings/errors, retained in
remaining-goal/pending-account-shop-focused.log.

The existing rotation grant ledger and premium reward modifiers remain separate.
Reward-track claims still require their own scoped claim identity, eligibility,
slot ordering and earned-progress policy, using this pending append boundary.
