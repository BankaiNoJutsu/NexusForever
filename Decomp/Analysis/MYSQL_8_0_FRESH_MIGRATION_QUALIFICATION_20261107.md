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
