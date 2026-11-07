# Execution-strategy scope audit of production transactions (2026-11-07)

Context: the production MySQL options install a retrying execution strategy; EF rejects strategy-executed work (`SaveChanges`) inside a
transaction opened outside `CreateExecutionStrategy().Execute(Async)`. One real instance (`ContractCompletionExecutor.Recover`) was fixed
([CONTRACT_COMPLETION_RETRY_STRATEGY_RECOVERY.md](CONTRACT_COMPLETION_RETRY_STRATEGY_RECOVERY.md)).

## Audit

A read-only subagent traced all 56 production `BeginTransaction(Async)` sites in `Source/` (not tests, not migrations) through their enclosing methods and callers, with
special attention to catch/finally/recovery paths. Result (agent report, not independently re-traced line by line by me): no confirmed defect of the fixed kind. One site is
caller-owned (`CharacterManager.UpdateCharacterCoreAsync`: it opens a transaction only when none exists, and its caller wraps it in a strategy). Three are safe but fragile because
they call the synchronous `strategy.Execute` with a delegate that returns a Task and then `SaveChangesAsync`: `CharacterAtomicSave.ExecuteCore` (after a real `await receipt.AuthorizeAsync`
when a token fee is charged), `Residence.Association` and `Residence.Donation` (no await before the save). The audit also noted
that the dominant pattern (a driver context runs the strategy and a fresh inner context begins the transaction) depends on EF's ambient strategy scope being AsyncLocal, not per context.

## Pins

`ExecutionStrategyAmbientScopeTests` (3 cases, SQLite with a strategy that enforces EF's user-transaction rule but never retries) confirms the two assumptions the fragile sites rely on:
a synchronous `Execute` returning a Task keeps the scope across an `await` (including a real 20 ms delay before `SaveChangesAsync`), and the same work outside any strategy scope is rejected
(negative control). All 3 pass on first run, so these are regression pins for the framework behavior, not a defect fix. Together with `ContractCompletionRetryingStrategyTests`
(a fresh inner context begins its transaction inside the outer strategy's scope) they cover both shapes.

## Limits

The audit is static. A production recovery path that no test reaches under the production options, or a site the agent misread, would not appear; the production-options MySQL
census found no further production-opened chains. The check is against EF Core 9.x behavior in this repository's packages; an EF upgrade should rerun these pins.
