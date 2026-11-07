# Decision on the two upstream observations (2026-11-07)

Evidence tier: code reading ([MYSQL_RETRY_CONVERSION_YIELD_20261107.md](MYSQL_RETRY_CONVERSION_YIELD_20261107.md)); no new test run. These are decisions, not retail claims.

## 1. Publish before commit in `GroupPlayerInviteHandler` and `GroupMemberRequestHandler`
Decision: **not changed.**
- The two handlers come from the upstream service refactor (`c0899fce`), not from this restoration. No ledger item, assessment row or retail witness asks for their ordering.
- Moving the publish after the commit is not a strict improvement. Today a failed commit after a publish sends a success-looking result for a rolled-back change (and a replay publishes twice); after the move a failed publish after a
  commit loses the notice and the broker's redelivery re-runs the handler against already-committed state, so the user would see a different result. Removing both failure modes needs an outbox (a durable message row committed with the
  change and drained afterwards), which is a new persistence design for the Group service and needs a schema decision.
- Nothing in the repository exercises these handlers under a failing commit, so there is no failing test to reproduce first.
Revisit when: the Group service is given a durable outbox, or a Group test harness can inject commit faults.

## 2. `AuthDatabase.TryAddStorePurchaseHistory` replay idempotency
Decision: **not changed.**
- The hazard needs a commit whose acknowledgement is lost *and* a strategy that then replays. The MySQL connector's lost-acknowledgement error (number 0 with an IO cause) is documented in `RealmBankManager.Command` as not
  classified transient, so the production strategy does not replay it.
- The row is a velocity-limit reservation that the caller deletes on delivery failure; a duplicate would at worst count one extra purchase against the window until it expires.
- A fix needs a stable purchase key, which the table does not have (a schema decision). Deduplicating on the existing columns (account, offer, currency, price, purchase time) could merge two genuine purchases made within the
  column's time resolution.
Revisit when: the history table gets a purchase key, or the connector's classification of that error changes.

Neither item is blocked on an asset or witness; both are declined on design grounds recorded above.
