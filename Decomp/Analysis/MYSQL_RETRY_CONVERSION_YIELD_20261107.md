# What converting test-opened transactions to the retry strategy can and cannot show (2026-11-07)

Evidence tier: reading of the existing test helpers, the production-options census and the execution-strategy audit. No new test run. Not a retail claim.

## Finding
Five test classes are already converted (`GuildAchievementRecoveryTests`, `GuildCreationRecoveryTests`, `GuildMutationRecoveryTests`, `GuildRenameRecoveryTests`, `ResidenceRecoveryTests`). Each has a private
`WithConfiguredStrategy(context, action)` that runs the whole test unit inside `CreateExecutionStrategy().ExecuteAsync` and **throws if the unit is replayed** ("A configured strategy cannot
replay the ... fixture's gameplay callback"). So a converted test only places its user transaction inside the strategy scope (which is what EF's rule requires); it never exercises a retry, because
a unit that holds a user transaction, mutates in-memory gameplay state and asserts on it cannot be replayed safely. That is also why EF forbids retry of user-opened transactions.

Consequences for the roughly 280 remaining `BeginTransaction` sites in about 50 test files:
- converting them makes the classes **runnable** under the production options (portability), which would turn the census from "35 classes fail on test-opened transactions" into pass/fail on real content;
- it adds **no retry evidence**: production units that really retry (`ContractCompletionExecutor`, `CharacterAtomicSave`, the residence association and donation paths) open their own transaction inside the strategy
  and are the ones the static audit ([EXECUTION_STRATEGY_SCOPE_AUDIT_20261107.md](EXECUTION_STRATEGY_SCOPE_AUDIT_20261107.md)) and the 20 already-passing classes cover;
- the shared helper would have to be copied into or shared by each class, and each site's body re-indented inside a callback (one-line `await using var transaction` sites sit in different block shapes).

## What would add retry evidence instead
A transient-fault injection test per production retrying unit on MySQL: inject a transient `MySqlException` on the first attempt inside the production unit and assert the second attempt commits exactly once
with no duplicated side effect. `ContractCompletionRetryingStrategyTests` does this for the Contract executor; the same shape for `CharacterAtomicSave`, the residence association/donation and the guild
unit would be new, evidence-bearing slices. They need the MySQL runtime already available (server on 127.0.0.1:3306), not new witnesses.

## Status
The bulk conversion is a portability task of low evidential yield, not started. The fault-injection tests above are the useful MySQL slice.
