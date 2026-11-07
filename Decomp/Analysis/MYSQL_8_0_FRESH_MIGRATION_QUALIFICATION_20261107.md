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

No product defect was found. Evidence tier: the existing Source cases that accept provider options, on real MySQL 8.0.46 with fresh schemas. This
covers Character-side and Character+Auth save/retry/rollback/commit-fault ownership across Quest, Challenge rewards, Contract claims, Crafting, Bank,
Housing, Achievement, Title and others. It does not cover MySQL 8.4.13, the configured retry execution strategy, the other Auth-only test classes
(account currency, account inventory, keybinding, world session admission) that have no provider seam, or socket, broker and load runs.

Run: set `NEXUS_FOREVER_TEST_MYSQL_SERVER="server=127.0.0.1;port=3306;user=<user>;password=<password>"` then
`dotnet test NexusForever.Game.Tests --filter "FullyQualifiedName~MySqlProviderQualification"`.
