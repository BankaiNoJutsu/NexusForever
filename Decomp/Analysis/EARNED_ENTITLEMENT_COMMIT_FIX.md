# Earned entitlement commit boundary

2026-10-29. The first relational reproduction confirmed two lost-reward cases:
after earned Loyalty row278 delivered AccountItem166, redeeming it granted
entitlement41 in memory. An Auth failure rolled back both the entitlement and
source deletion. Retry retained the source deletion but AccountEntitlement.Save
had already cleared its dirty mask, so the source disappeared while a fresh
account still loaded entitlement41 as zero. Both newly created and existing-zero
entitlement rows reproduced the loss; their normal-commit controls passed.
The parent's initial combined result was315/317, with precisely those two failures.

## Bounded implementation

AccountEntitlement now stages through the existing Auth save unit. It clears
Create/Amount intent only after actual terminal commit. Failure, rollback,
savepoint uncertainty and disposal retain the intent. A protected base-state lock
and mutation revision synchronize amount and dirty acknowledgement without changing
the public Amount API, clamping, entitlement limits or Character behavior. There
is no new schema, entitlement journal or default grant.

Real Account.Save reserves entitlement unit ownership before any other child
Save can acknowledge legacy dirty state. Overlapping contexts for the same owner
are rejected rather than silently skipping a benefit. A rejected Auth context is
permanently invalid: neither later SaveChanges nor an already-open outer Commit
can publish its partial currency/Loyalty work, even after the earlier owner finishes.
Retry uses a fresh context. Reservations are released by terminal completion or
context disposal. Aggregate earned redemption must use the real Account.Save path.

## Explicit snapshot policy

- Before SQL staging, another Save on the same context refreshes its registered
  amount and revision.
- After staging, a later amount mutation that is not re-enrolled belongs to the
  next snapshot. The older captured unit may commit; its acknowledgement retains
  the newer mutation as dirty for a fresh unit.
- Calling Save again to re-enroll that changed revision in the already-staged
  context permanently rejects that unit and requires rollback. A numeric0→1
  round trip is detected through revision, even if its final amount equals the
  old snapshot. This prevents enrolling a new source deletion with an old benefit.

This distinguishes ordinary asynchronous snapshot completion from attempted
post-staging enrollment. A time-varying ownership check cannot make a rejected
context valid again.

## Regression evidence

EarnedLoyaltyEntitlementDurabilityTests uses actual build16042 track/reward/account
item data, real Account.Save and actual currency, entitlement, inventory and Loyalty
owners. It follows earned pending→account inventory→TakeItem, then a real SQLite
constraint failure, retry and fresh-account reload. Seventeen cases cover both
create/update reproduction controls, outer commit/rollback, both savepoint
positions, context/transaction disposal, pre-staging refresh, later mutations,
post-SQL round trips and overlap rejection with already-flushed currency work.
An unrelated dirty-child probe confirms that rejection precedes legacy child
acknowledgement in the actual Account.Save implementation.

The exact Entitlement.tbl hash is
`7d84102f4da712194318e0e76d3761c59048d3e98a2bd3fe1af1b72c0f721abc`.
Defaults15/17 remain at1 throughout the tests.

The parent's combined focused gate passes **376/376**, zero warnings/errors,
including all17 dedicated earned-entitlement cases. The subsequent full solution
build has zero warnings/errors and **6717/6717 tests pass**. Logs are retained in
remaining-goal/map-harness-entitlement-focused.log,
remaining-goal/map-entitlement-integrated-build.log and
remaining-goal/map-entitlement-integrated-tests.log. Three old map-entry fixtures
were initialized with valid map/GUID/bootstrap state for the separate map fix;
their assertions were preserved.

Independent read-only review found no additional defect in the accepted snapshot
contract. The final frozen6717 checkpoint also passes **34 aggregate real-MySQL
earned-redemption checks** in a new owned30-table clone. The proof covers actual
Account.Save with raw278/166/41 data, new/existing-zero rollback/retry and relog,
pre-SQL refresh, staged numeric round trips, permanent overlapping-context rejection
with preexisting currency work, sync/async retries, before/after-commit replay,
and later unenrolled mutation surviving older commit/rollback. Defaults15/17 and
source/receipt/binding preservation hold; the original30 source tables are unchanged.
Evidence is retained in remaining-goal/MySqlEarnedEntitlementProof_29e1, with
the narrative report at remaining-goal/MYSQL_EARNED_ENTITLEMENT_PROOF.md. The
initial315/317 reproduction is retained as failure evidence, not a passing result.
