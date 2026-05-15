# NexusForever Setup Automation

`Initialize-NexusForever.ps1` automates the standalone local setup flow from the
NexusForever server guide.

It creates the emulator databases, imports the split SQL dump folders in the
repo, runs Entity Framework migrations, imports the optional official world
database, and can start the standalone server processes.

## Prerequisites

- MySQL or MariaDB server running on `127.0.0.1:3306`.
- MySQL/MariaDB command-line client (`mysql.exe`).
- RabbitMQ server running if you want the script to create the broker user.
- .NET SDK required by this repo.
- Optional: a `NexusForever.WorldDatabase` clone next to this repo, for example
  `I:\GIT\NexusForever.WorldDatabase`. The script also checks the guide's
  `C:\nexusforever-database` path.

The script searches common MariaDB/MySQL locations under `C:\Program Files`.
If it cannot find `mysql.exe`, pass the path explicitly with `-MySqlExe`.

## Full Local Setup

From the repository root:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf
```

## One-Command Local Launch

Use `Start-NexusForeverLocal.ps1` when you want one command to build the
solution, prepare runtime assets, start the standalone servers, and launch the
WildStar client against localhost.

From the repository root:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ClientDirectory "D:\Games\WildStar" `
  -PromptForRootPassword
```

What it does:

- runs `Initialize-NexusForever.ps1` with the same database and RabbitMQ
  options you pass through
- stages shared `tbl` and `map` runtime assets under `.nexusforever-runtime`
  and rewrites the runtime JSON files to those absolute paths
- creates or verifies the default login account
- starts the standalone server executables and waits for the local endpoints to
  come up before launching `WildStar64.exe`

The default login created by the launcher is:

- username: `nexusforever`
- password: `nexusforever`

If you already have extracted tables and generated maps, point the launcher at
those directories and skip the heavy setup work on later runs:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ExistingTableDirectory "D:\WildStarAssets\tbl" `
  -ExistingMapDirectory "D:\WildStarAssets\map" `
  -ClientDirectory "D:\Games\WildStar" `
  -SkipSetup
```

If staged assets are missing and you do not pass `-ExistingTableDirectory` and
`-ExistingMapDirectory`, the launcher needs a WildStar client `Patch`
directory. Passing `-ClientDirectory` is enough for the common layout because
the script automatically uses `<ClientDirectory>\Patch`.

On this machine, `mysql.exe` is available at:

```powershell
C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe
```

If auto-detection ever misses it, run:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 `
  -MySqlExe "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  -PromptForRootPassword `
  -InstallDotNetEf
```

## What It Creates

Emulator databases:

- `nexus_forever_auth`
- `nexus_forever_character`
- `nexus_forever_world`
- `nexus_forever_group`
- `nexus_forever_chat`
- `nexus_forever_friendship`

Reference dump databases:

- `jabbithole` from `jabbithole_mysql\*.sql`
- `wildstar_client` from `wildstar_client_mysql\*.sql`

If either split folder is missing or empty, the script falls back to the older
single-file dump for that database:

- `all_jabbithole_mysql.sql`
- `all_wildstar_client_mysql.sql`

The reference databases get a `__nexusforever_imports` marker table, so later
runs skip already imported files unless their hash changed. With the split
folders, markers are tracked per SQL file, so a rerun can continue after the
last completed file instead of starting the whole dump again. Use
`-RecreateLargeDumpDatabases` only when you intentionally want to drop and
re-import both reference databases.

`jabbithole_mysql\.sql` is skipped by default. It is a nameless aggregate-style
SQL file, not one of the table-specific split exports.

The reference SQL exports use SQLite-style `INTEGER` columns. MySQL treats
`INTEGER` as a 32-bit signed type, while the WildStar client data contains
larger values such as character customization costs. The script therefore
streams the reference SQL files through a compatibility normalizer that changes
`INTEGER` columns to `BIGINT` and over-wide `VARCHAR(...)` columns to `TEXT` in
`CREATE TABLE` blocks before import. It also quotes generated column names so
identifiers such as `maxValue` do not collide with MySQL syntax. Pass
`-KeepLargeDumpIntegerColumns` only if you intentionally want the original
integer column declarations and are prepared for MySQL strict-mode range errors.

For reference imports, the script also sends explicit `CREATE DATABASE` and
`USE` statements inside each import stream. This keeps MySQL pointed at the
correct target database even if the client reconnects during a long import.

## Useful Variants

Create databases, copy configs, build, and migrate without optional data imports:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -SkipLargeDumpImports -SkipWorldDatabaseImport
```

Recover from a failed partial reference SQL import without dropping completed
reference databases:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf -RepairPartialLargeDumpDatabases
```

Force both reference databases to be dropped and rebuilt:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -RecreateLargeDumpDatabases
```

Use custom split SQL folder locations:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 `
  -PromptForRootPassword `
  -JabbitholeSqlDirectory "I:\Exports\jabbithole_mysql" `
  -WildstarClientSqlDirectory "I:\Exports\wildstar_client_mysql"
```

Skip RabbitMQ user creation:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -SkipRabbitMqUser
```

Use a different official world database checkout:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 `
  -PromptForRootPassword `
  -WorldDatabasePath "I:\GIT\NexusForever.WorldDatabase"
```

When `-WorldDatabasePath` is omitted, the script checks these locations in
order:

- sibling checkout: `<repo parent>\NexusForever.WorldDatabase`
- guide checkout: `C:\nexusforever-database`

Official world SQL files are checked against the world schema after EF
migrations run. Files that insert into tables or columns missing from the
current NexusForever branch are skipped and any prior partial rows for that
file's `@WORLD`/`@EVENTID` are cleaned up before setup continues. This is
expected for branch-specific content such as `Instance\Expedition\Evil from the
Ether.sql` when you are not on the matching server branch.

If you intentionally want to raw-import those known branch-specific rows anyway,
you can opt in to compatibility tables:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 `
  -PromptForRootPassword `
  -CreateWorldDatabaseCompatibilityTables
```

Prefer switching NexusForever to the matching branch and rerunning migrations
when you need runtime support for that content.

Start the standalone servers after setup:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -StartServers
```

## Troubleshooting

`Required command 'mysql' was not found`:

- Re-run after this update; the script now auto-detects common install paths.
- Or pass `-MySqlExe "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"`.

`ERROR 1264 ... Out of range value for column 'cost'`:

- Re-run after this update with `-RepairPartialLargeDumpDatabases`.
- The importer now normalizes reference SQL `INTEGER` columns to `BIGINT`.
- Do not pass `-KeepLargeDumpIntegerColumns` unless you want the old behavior.

`ERROR 1118 ... Row size too large`:

- Re-run after this update with `-RepairPartialLargeDumpDatabases`.
- The importer now converts over-wide `VARCHAR(...)` declarations to `TEXT`
  inside affected `CREATE TABLE` blocks.

`ERROR 1064 ... near 'maxValue ...'`:

- Re-run after this update with `-RepairPartialLargeDumpDatabases`.
- The importer now quotes generated column identifiers in `CREATE TABLE`
  blocks.

`ERROR 1049 ... Unknown database 'wildstar_client'` during the client dump:

- Re-run after this update with `-RepairPartialLargeDumpDatabases`.
- The importer now selects the target database inside each stream and on client
  reconnects, instead of relying only on the startup `--database` argument.

Reference databases are being dropped every run:

- That only happens when `-RecreateLargeDumpDatabases` is passed.
- Omit that switch for normal runs; matching imports are skipped by hash marker.
- Use `-RepairPartialLargeDumpDatabases` after a failed import to rebuild only
  incomplete reference databases.
- During split-folder imports, `-RepairPartialLargeDumpDatabases` can also
  repair a single unmarked file by dropping just that file's existing table and
  importing it again.

`ERROR 1050 ... Table '...' already exists` during a split-folder import:

- Re-run with `-RepairPartialLargeDumpDatabases`.
- The script will detect the table created by the unmarked file, drop only that
  table, and import the file again.

An older monolithic import already exists:

- Normal runs leave a completed older single-file import intact.
- To switch that database to split-file markers, run once with
  `-RecreateLargeDumpDatabases`.
- After that, omit `-RecreateLargeDumpDatabases`; the per-file markers will
  skip completed split imports.

`dotnet-ef is not available`:

- Pass `-InstallDotNetEf`, or run `dotnet tool install dotnet-ef --global`.

RabbitMQ command not found:

- Install RabbitMQ and make `rabbitmqctl` available on `PATH`.
- Or pass `-SkipRabbitMqUser` and create the `nexusforever` broker user manually.

Official world database skipped:

- Clone `https://github.com/NexusForever/NexusForever.WorldDatabase.git` to
  a sibling folder such as `I:\GIT\NexusForever.WorldDatabase`.
- The script also checks `C:\nexusforever-database`.
- Pass the actual path with `-WorldDatabasePath` if your checkout lives
  somewhere else.

`ERROR 1146 ... entity_property doesn't exist` during official world import:

- Re-run after this update.
- The importer now skips world SQL files that require unsupported schema in the
  current branch. `Evil from the Ether.sql` is one known example unless you are
  on the matching NexusForever branch.
- Use `-CreateWorldDatabaseCompatibilityTables` only if you intentionally want
  raw compatibility tables for the known branch-specific rows.

`ERROR 1062 ... Duplicate entry ... entity_script.PRIMARY` during official
world import:

- Re-run after this update.
- Earlier failed imports can leave committed child rows without a version
  marker. The script now keeps foreign key checks enabled for official world SQL
  files and clears prior partial rows for the file's `@WORLD`/`@EVENTID` before
  retrying that file.

`ERROR 1054 ... Unknown column 'Value'` or `Unknown column 'value'` during
official world import:

- Re-run after this update.
- The world SQL exports use mixed-case column names, while the EF-created world
  schema uses lower/camel-case MySQL column names. The importer now normalizes
  those backticked column identifiers while streaming official world SQL files.
- A previous script revision could create compatibility tables with `alue`
  instead of `value` because MySQL backticks were inside a PowerShell
  double-quoted here-string. When `-CreateWorldDatabaseCompatibilityTables` is
  used, the script now repairs that typo automatically.
