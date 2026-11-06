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
