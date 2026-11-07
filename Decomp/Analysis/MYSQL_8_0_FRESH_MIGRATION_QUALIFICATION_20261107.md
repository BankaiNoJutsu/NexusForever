# Fresh-schema migration qualification on local MySQL 8.0.46 (2026-11-07)

Scope: current source (`game_rework`-derived worktree HEAD 9fe9577b) applied to brand-new disposable schemas.
Provider: MySQL Server 8.0.46 on 127.0.0.1:3306 with the repository's `Aspire.Database.Migrations` project (single-project
build; `AccountCreation.Accounts` empty, no world-data import) and the documented local development account. Seven schemas
`nexus_qual_20261107_{auth,character,world,group,chat,friendship,query}` did not exist beforehand and were dropped afterwards
(`SHOW DATABASES LIKE 'nexus_qual_%'` returns nothing). Existing `nexus_*` and reference schemas were not touched.

| Schema | Migrations applied (`__EFMigrationsHistory`) | Tables |
|---|---|---|
| auth | 42 | 33 |
| character | 49 | 68 |
| world | 17 | 29 |
| group | 1 | 12 |
| chat | 1 | 7 |
| friendship | 2 | 17 |
| query | 1 | 2 |

The host exited 0 with no migration errors (one benign sensitive-data-logging warning). Character's 49 applied
migrations equal the count recorded by the earlier MySQL 8.4.13 qualifications; the Auth, World, Group, Chat, Friendship and
Query counts equal the number of migration files in their projects.

## Evidence limits

- Tier: fresh-schema forward migration on one MySQL 8.0 minor version only. Not 8.4.13, not a copied-upgrade from an older
  schema, not a public-flow or recovery test, no retry-strategy profile, no socket/broker/load run.
- The earlier configured-MySQL recovery qualifications still need their separate adapter harness (not in this repository).
- Migration success does not qualify gameplay behavior or retail parity.

## Copied-upgrade (2026-11-07)

Older migrated schemas from the 29 October assessment (`nexus_assessment_20261029_auth` at 39 migrations,
`nexus_assessment_20261029_character` at 36) were copied with `mysqldump --single-transaction` into disposable schemas
`nexus_qual_up_20261107_{auth,character}` (the originals were only read). The other five contexts used fresh disposable
schemas. The same migrations host was run once; exit 0, no migration errors.

| Schema | Migrations before → after | Tables before → after | Pre-existing tables with changed row count |
|---|---|---|---|
| auth | 39 → 42 | 31 → 33 | none (10,091 → 10,094 rows; +3 = migration-history rows) |
| character | 36 → 49 | 59 → 68 | none (22,956 → 22,969 rows; +13 = migration-history rows) |

Exact `COUNT(*)` of every base table was compared between the untouched originals and the upgraded copies (a first attempt
at pre-upgrade counts was discarded because of a shell-quoting bug; the originals were never modified, so the comparison is
sound). The upgraded schemas end with the same migration and table counts as the fresh-schema run above. All eight
disposable schemas were dropped; `SHOW DATABASES LIKE 'nexus_qual%'` returns nothing.

Limits: row counts, not row contents, were compared; World (the older schema has 18 applied migrations vs 17 in source) was
not copied; MySQL 8.0.46 only (not 8.4.13); no public-flow, recovery, retry-strategy, socket or load run.

## Provider qualification on MySQL 8.0.46 (2026-11-07)

`MySqlProviderQualificationTests` (new, opt-in, `Source/NexusForever.Game.Tests/Database`) discovers by reflection every public test
class that already exposes a `DbContextOptions<CharacterContext>` constructor (52 classes) and runs each of its facts and
theory cases against its own fresh disposable MySQL schema (created and dropped by the driver, `Pooling=False`). It is skipped
unless `NEXUS_FOREVER_TEST_MYSQL_SERVER` is set to a server connection string without a database (no credentials in the repository);
`NEXUS_FOREVER_TEST_MYSQL_CLASSES` optionally restricts it to named classes.

**Final result: all 52 classes pass on the local MySQL Server 8.0.46** (57.4 minutes, one full run with the final driver), using the
documented development account; `SHOW DATABASES LIKE 'nexus_qual%'` and `'nf_remodel%'` were empty afterwards.

How the first run's five failing classes were resolved (the first run passed 47 of 52):
- Driver defects: pooled connections exhausted the server ("Too many connections") until pooling was disabled; theory cases with omitted
  optional parameters (the two Bank recovery classes) now get their declared defaults; fixtures that expect the caller to create the
  schema (CharacterItemDurabilityTests) get `EnsureCreated`; the housing remodel class's own guard needs a primary schema named
  `nf_remodel_*`, so the driver uses that prefix for it and drops its `_foreign` companion; fixtures documented as foreign-key-free
  synthetic ones (CharacterItemDurabilityTests) run with `SET SESSION foreign_key_checks = 0`, mirroring their SQLite `Foreign Keys=False`.
- Test source: two fault-injection triggers were SQLite-only. `CharacterItemDurabilityTests` (3 cases) and
  `CharacterRewardItemEnrollmentTests` (1 case) now choose the SQLite `RAISE(ABORT, ...)` trigger or the MySQL `SIGNAL SQLSTATE '45000'`
  trigger by provider (the convention already used by `ArchivePublicationTests` and others). Every assertion is unchanged; the two classes still
  pass on SQLite (93 cases).

### Two-database (Character + Auth) flows

A second theory, `TwoDatabaseClassHoldsOnMySql`, drives the three classes that also accept an Auth database, each case with its own
Character schema, a separate Auth schema and (for crafting) a foreign Auth schema, all created and dropped by the driver:
`ContractClaimTransactionTests` (Character + Auth `DbContextOptions`), `CraftingSettlementTests` (Character options + Auth and foreign Auth
connection strings) and `BankDeferredCurrencyRecoveryTests` (Character options + vendor Auth connection). All three pass on MySQL 8.0.46
(10.5 minutes; no leftover schemas). That covers the Contract claim, crafting award delivery/admission and bank recovery flows across two real
databases, which the earlier single-database run could not.

### Production options (retry strategy) census

Setting `NEXUS_FOREVER_TEST_MYSQL_PRODUCTION_OPTIONS=1` builds every context through the repository's own `UseConfiguration`, i.e. the production MySQL
options: `EnableRetryOnFailure(5, 10 s)` and the string-concurrency `IUpdateSqlGenerator`. One full run (1.07 hours) over the 55 driver theory cases:
**16 classes pass completely; 39 have failures (590 of 1,300 cases)**. About 95% of the failure messages are one cause: EF's
`MySqlRetryingExecutionStrategy` rejects tests that open their own user transaction with `Database.BeginTransaction` ("does not support
user-initiated transactions"). Those tests were written for the SQLite path and would have to wrap their transaction in
`CreateExecutionStrategy().Execute` to run under this strategy; the production save paths do (for example `ContractCompletionExecutor.Run` executes inside
the execution strategy, and `CharacterAtomicSaveTests` passes 19 of 20 cases through the production save path, the one failure again being a
test-opened transaction). So this census measures which test classes are portable to the retry strategy as written, not product behaviour under it.

Failures with a different cause, not yet explained: `ContractCompletionExecutorTests` 3 of 10 (the lost-commit-acknowledgement and cancellation-after-commit
cases, assertion at line 34 on `Recovered`), a few `Assert.Throws` mismatches in `ContractCompletionRepositoryTests`, and `Assert.True`/`Assert.Equal` failures in
`InventoryRewardDeliveryTests` (Contract admission cases and partial id allocation). A plausible explanation for the executor cases, unconfirmed by a trace,
is that the fault-injecting interceptor throws an `IOException` once and the retrying strategy transparently re-runs the unit on a fresh context, so the explicit
recovery path the test asserts is never needed. These cases need a test variant that disables the retry for fault injection, or a trace, before any claim is made.

#### Second census after the Contract recovery fix (same day)

After fixing `ContractCompletionExecutor.Recover` and making the shared challenge fixture strategy-aware ([CONTRACT_COMPLETION_RETRY_STRATEGY_RECOVERY.md](CONTRACT_COMPLETION_RETRY_STRATEGY_RECOVERY.md)),
one full production-options run (1.08 hours) gave **20 of 55 classes passing completely; 35 with failures (294 of 1,174 cases, down from 590 of 1,300)**. The driver
now prints the NexusForever call chain for each strategy rejection. Every remaining rejection has one of these shapes: a test method that opened its own transaction and
then called a persistence `Save`/`Read` path (`ExperiencePersistence`, `Quest`/`QuestObjective`, `Item`, `Title`, `Costume`, `Keybinding`, `GuildAffiliation`,
`Progression`, `Residence`, `ChallengeManager`, `Player.Save`), or called a repository constructor inside its own transaction. No remaining chain contains a production
executor or delivery recovery opening the transaction, which was the signature of the defect fixed above. That is an absence in this run, not a proof that no other
production site exists: a production recovery that none of these tests reaches would not appear.

Previously unexplained failures, now traced (rerun of those two classes, 3.4 minutes):
- `ContractCompletionRepositoryTests.OverflowCannotLeaveATerminalQuestOrReceipt` (4 cases): the `Assert.Throws` exact-type mismatch carries the retry
  strategy's own `InvalidOperationException` ("does not support user-initiated transactions") as the actual exception, so this is the same test-opened-transaction cause.
- `InventoryRewardDeliveryTests` (Contract admission, partial id allocation, changed definitions): these tests open a serializable transaction themselves and then
  assert `Assert.Throws<InvalidOperationException>(() => f.Stage(context, held))`. Under the retry strategy that assertion is satisfied by the strategy's own
  `InvalidOperationException`, thrown before `Stage` reaches the failure the test is trying to provoke, so the following assertion fails (for example
  `Assert.True(context.IsSaveRejected)` at InventoryRewardDeliveryTests.cs:151 and the resolved-state check at :564). The same shape appears in the other
  cases of the family, but only these two sites were read line by line; the rest of the family is inferred from the shared pattern.

Consequence: no failure in the second census is attributed to a production site. These classes need their own transaction opened through the execution
strategy (or a strategy-aware helper) before they can be run under the production options.

Evidence tier: the existing Source cases on real MySQL 8.0.46 with the production options. Not a statement that production code is correct under the retry
strategy, and not retail parity.

No product defect was found. Evidence tier of the plain-options runs above: the existing Source cases that accept provider options, on real MySQL 8.0.46 with fresh schemas. This
covers Character-side and Character+Auth save/retry/rollback/commit-fault ownership across Quest, Challenge rewards, Contract claims, Crafting, Bank,
Housing, Achievement, Title and others. It does not cover MySQL 8.4.13, the Auth-only test classes (account currency, account inventory, keybinding, world
session admission) that have no provider seam, or socket, broker and load runs.

Run: set `NEXUS_FOREVER_TEST_MYSQL_SERVER="server=127.0.0.1;port=3306;user=<user>;password=<password>"` (optionally
`NEXUS_FOREVER_TEST_MYSQL_PRODUCTION_OPTIONS=1`) then `dotnet test NexusForever.Game.Tests --filter "FullyQualifiedName~MySqlProviderQualification"`.
