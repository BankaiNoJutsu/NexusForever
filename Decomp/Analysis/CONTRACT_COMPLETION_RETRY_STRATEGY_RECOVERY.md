# Contract completion recovery under the retrying execution strategy (2026-11-07)

## Defect

`ContractCompletionExecutor.Recover` opened its serializable transaction (`BeginTransactionAsync`) on a fresh context and ran
`SaveChangesAsync` outside any execution strategy. `Run` calls it from its `catch` block, which is reached after a non-transient failure
(for example a lost commit acknowledgement) has left the original `CreateExecutionStrategy().ExecuteAsync` scope. The production MySQL
options install `MySqlRetryingExecutionStrategy` (`UseConfiguration`: `EnableRetryOnFailure(5, 10 s)`); EF rejects strategy-executed work inside
a transaction that was not started through the strategy ("The configured execution strategy ... does not support user-initiated transactions"),
and the executor then classified the `InvalidOperationException` as `Conflict`. Net effect on a retry-enabled provider: the recovery that is
meant to resolve an uncertain Contract completion commit before the same command may credit again could not run, so the outcome stayed
unresolved as `Conflict` instead of `Committed`/`Failed` with `Recovered`. SQLite has no retrying strategy, so the default SQLite
tests could not see it. The sibling `CharacterChallengeRewardExecutor` already has this fix (`RecoverWithStrategyAsync`, with a comment
describing the same reason).

## How it was found

The production-options MySQL run of the opt-in driver (see [MYSQL_8_0_FRESH_MIGRATION_QUALIFICATION_20261107.md](MYSQL_8_0_FRESH_MIGRATION_QUALIFICATION_20261107.md))
failed three `ContractCompletionExecutorTests` cases whose diagnostic message read `outcome=Conflict ... does not support user-initiated transactions`.
An earlier hypothesis that the strategy transparently retried the injected `IOException` was wrong: `MySqlRetryingExecutionStrategy` does not
retry `IOException` (`ShouldRetryOn` is true only for `TimeoutException` among those tried).

## Fix and tests

`Recover` now runs its read-and-prove transaction inside `context.Database.CreateExecutionStrategy().ExecuteAsync`, clearing the change tracker at the start of
each attempt. `ContractCompletionRetryingStrategyTests` (new, 5 cases) rerun the existing lost-commit-acknowledgement (committed and not),
cancellation-after-commit, and resume-after-recovery-read-failure cases on SQLite with a strategy that enforces EF's user-transaction rule but never
retries; all 5 failed before the fix with `outcome=Conflict` and pass after, including the resume path that runs recovery inside the outer strategy.
Evidence: 2,797 Contract/Quest cases pass on SQLite; `ContractCompletionExecutorTests` passes fully on real MySQL 8.0.46 with the production options (it
failed 3 of 10 before). Tier: Source/SQLite plus the MySQL driver; not 8.4.13, not socket/broker/load.

## Test-fixture change

`CharacterChallengeRewardRepositoryTests.Fixture.Execute` opened its transaction outside the strategy, so every class using it failed under retry
options. It now runs through the context's execution strategy (identical single execution under SQLite). Repeating the production-options run over the
affected family reduced `QuestChallengeRewardQueueTests` from 52 of 57 failing cases to 2 of 57. The remaining failures traced so far are tests that
call `ChallengeManager.Save` or repository methods inside a transaction they opened themselves (call chain ends in the test method), not production sites.
The driver now prints the NexusForever call chain for these rejections so test-opened and production-opened transactions can be told apart.
