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

## First fault-injection pins (same day)
`CharacterAtomicSaveRetryingStrategyTests` (2 cases, SQLite with a strategy that really retries once on an injected `TransientTestException`): a transient fault on the owner read inside `CharacterAtomicSave`'s
strategy scope is retried by the strategy, the production guard refuses the replay ("Actor enrollment cannot be retried"), the enrollment callback runs 0 times, nothing commits, and a later unit enrolls once
and commits; a fault during the asynchronous commit stage is not a strategy replay, rolls back, the enrollment ran once, and a later unit commits once. Both passed on first run, so they are regression pins
of existing behaviour (no defect found), not a fix. The SQLite strategy's notion of transience (`ShouldRetryOn`) is the test's own; the production MySQL strategy classifies MySQL errors itself.
The 22 `CharacterAtomicSave` cases pass (separate scope). Residence association/donation and the guild units remain without this kind of pin.

## Replay-safety read of the production strategy units (sampled, same day)
About 60 production sites run work through `CreateExecutionStrategy().Execute(Async)`. Read this pass: the synchronous family that mutates in-memory gameplay state carries an explicit `entered` guard that turns a
strategy replay into a non-retriable `InvalidOperationException` (`CharacterAtomicSave`, `CharacterDatabase.CreateCharacter`, `Residence.Association`, `Residence.Donation`, `ChallengeManager.ContractCompanion`,
`Currency.BankChange`), so a transient fault cannot apply the same enrollment twice; the asynchronous family sampled (`ContractAccountDelivery.ReadSource`/`ObserveAuth`/`Acknowledge`, the Fortune publication
verification) begins from a fresh inner context and re-reads its authoritative rows inside the delegate, which is replay-safe by construction. Not read: the Marketplace commands, `CraftingAccountDelivery`,
`MailManager.Delivery`, `RealmBankManager.Command`, `AuthContext.SaveUnit`, `CharacterChallengeRewardExecutor`, `QuestManager.Rewards`, `VendorAccountPurchase`, `AccountCurrency.Refresh` and the Friendship/Group/Chat
handlers. Those are the remaining candidates for a replay-safety read or a fault-injection pin; no defect is known.

Addendum: `GlobalMarketplaceManager.AuctionPostingCommand.Attempt` (read) is replay-safe by receipt: each attempt uses a fresh owned context, locks, returns the stored outcome when the `MarketplaceBidReceipt`
row already exists, and otherwise re-verifies the complete preimage before staging; a failed attempt falls to `ResolveAsync`. The other Marketplace commands share the `ExecuteAsync(Attempt)` shape but were not read.

## Auction posting under a really replaying strategy (same day)
Existing strategy coverage found in the tests (really retrying strategies): Auth save unit and pending-item durability, challenge reward executor, crafting outbox and settlement, world session admission,
character query bootstrap, friendship presence, `CharacterAtomicSaveTests` (IOException). Areas with none: Marketplace, Group, Chat, Fortune, Server.Character handlers, the Auth bootstrap in `AuthDatabase`.
`AuctionPostingRetryingStrategyTests` (2 cases, SQLite, a strategy that replays once on an injected transient `IOException`; the test counts the replay): a transaction failing before commit is replayed
and commits, and a commit whose acknowledgement is lost is replayed and finds its receipt; both leave exactly one listing, one escrowed item and one receipt, and the wallet unchanged. Both passed first
run (pins, no defect). The existing 37 auction posting cases still pass. Settlement, commodity posting/return and the bid commands share the `ExecuteAsync(Attempt)` shape and are not pinned yet.

## Remaining strategy units read (same day)
Replay-safe by construction (read): `CommodityPostingCommand.Attempt`, `CommodityReturnCommand.Attempt` and `AuctionSettlementCommand.Attempt` (each fresh owned context, receipt checked first, then complete preimage
verification, same shape as auction posting, which is now pinned); `CraftingAccountDelivery.Acknowledge`/`ResolveReservation` (fresh context, `FOR UPDATE` re-read, idempotent `Delivered = true`, plus existing
really-retrying crafting tests); `MailManager.Delivery` inbox load and `QuestManager.Rewards.PrepareWorker` (read-only transactions in fresh contexts, results created per attempt); `AccountCurrency.Refresh`
observation (read-only, fresh context); `RealmBankManager.Command.Attempt` (fresh context per attempt, explicit lost-acknowledgement recovery through a retained commit graph, covered by
`RealmBankItemIdentityTests.MailDeliveryRecovery`). Not read: `ContractAccountSync`, `CharacterChallengeRewardExecutor`/`VendorAccountPurchase` (both have really-retrying tests), the `AuthDatabase` bootstrap and
`CharacterManager` in `Server.Character`.

**Upstream observation, not changed:** the Group service handlers (for example `GroupPlayerInviteHandler.Handle`; the same shape appears in the other Group, Friendship and Chat handlers) run their work inside
`strategy.ExecuteAsync` on the shared injected `_context` and call `_messagePublisher.PublishAsync` *before* `SaveChangesAsync`/`CommitAsync`. A transient commit fault would replay the delegate on a context that
still tracks the first attempt's entries and would publish the result message again (and a non-transient failure still leaves the message published). These handlers come from the upstream service refactor
(`c0899fce`, `99752578`), not from this restoration; changing them is an upstream design decision (publish after commit or an outbox) and no retail or ledger requirement asks for it, so it is recorded here only.
