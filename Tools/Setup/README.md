# NexusForever Setup Automation

`Initialize-NexusForever.ps1` automates the standalone local setup flow from the
NexusForever server guide.

It creates the emulator databases, imports the split SQL dump folders in the
repo, runs Entity Framework migrations, imports the optional official world
database, and can start the standalone server processes.

## Prerequisites

- Either a reachable MySQL or MariaDB server on the configured `-MySqlHost` and `-MySqlPort`, Docker Desktop for automatic portable provisioning, or `-DatabaseProvider Sqlite` for runtime-only local databases.
- Either a reachable RabbitMQ broker on the configured `-BrokerHost` and `-BrokerPort`, or Docker Desktop for automatic portable provisioning.
- MySQL/MariaDB command-line client (`mysql.exe`) if you want to target an existing external database server without Docker.
- .NET SDK required by this repo.
- Optional: a `NexusForever.WorldDatabase` clone next to this repo, for example
  `I:\GIT\NexusForever.WorldDatabase`. The script also checks the guide's
  `C:\nexusforever-database` path.

The script searches common MariaDB/MySQL locations under `C:\Program Files`.
If it cannot find `mysql.exe`, pass the path explicitly with `-MySqlExe`.

By default, the setup scripts use the configured MySQL and RabbitMQ endpoints if
they are already reachable. If a local endpoint is missing and Docker Desktop is
available, the scripts automatically start a repo-scoped portable MariaDB or
RabbitMQ container and reuse it on later runs.

Use `-DependencyMode ExternalOnly` to disable portable provisioning and require
pre-existing services.

Use `-DependencyMode PortableDocker` to force the scripts to use the portable
Docker-backed dependencies even if local services are already running.

SQLite skips MySQL/MariaDB dependency resolution and stores the six runtime
databases under `.nexusforever-runtime\sqlite` by default. RabbitMQ is still
required for the standalone server processes. DataMapping authoring imports
(`jabbithole` and `wildstar_client`) remain MySQL/MariaDB-only.

## Full Local Setup

From the repository root:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf
```

Runtime-only SQLite setup:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -DatabaseProvider Sqlite
```

Use `-SqliteDirectory "D:\NexusForever\sqlite"` to place the database files
outside the default `.nexusforever-runtime\sqlite` directory.

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

To launch against SQLite runtime databases:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -DatabaseProvider Sqlite `
  -ClientDirectory "D:\Games\WildStar"
```

For the faster local edit loop after the repo has already been initialized once,
use the local restart wrapper instead of the full setup path:

```powershell
.\Tools\Setup\Restart-NexusForeverLocal.ps1 `
  -ClientDirectory "D:\Games\WildStar" `
  -PromptForRootPassword
```

For a SQLite restart, pass `-DatabaseProvider Sqlite` to the restart wrapper as
well so it rewrites runtime configs to the SQLite connection strings.

That wrapper stops any running `NexusForever.*` process, then launches the full
local standalone stack through the same runtime-prep flow. The delegated
launcher build runs once for the solution so server, client connector, and
WorldServer runtime script assemblies stay in sync without a separate
per-project pre-build. It skips launching the client when WildStar is already
running.

If you want auth/world-only restart behavior from the base launcher, pass
`-RestartAuthWorldOnly`. When you pass `-RestartExistingServers:$false`
without that switch, the base launcher now keeps already-running local server
processes and only starts the ones that are missing.

`-SkipSetup` skips database/broker/user/migration setup, but it still performs
the launcher build step before starting or reusing server processes. This keeps
runtime assemblies in sync after source edits and avoids mixed-version startup
failures.

To launch the client with extra WildStar command-line arguments, pass
`-ClientArguments`. For the built-in client console discovered in the retail
binary, prefer `-EnableClientConsole`, which appends `-Console` for you without
relying on a dash-prefixed string argument. The client checks `Alt` plus the
hard-coded virtual key `0xC0` in game to toggle the console window, so the
physical key varies by keyboard layout. On a typical US layout this is the
backtick key; on a Swiss layout it is the key that produces `¨`, `!`, or `]`
depending on modifiers.

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ClientDirectory "D:\Games\WildStar" `
  -EnableClientConsole `
  -PromptForRootPassword
```

### Retail client file logging (`CLog`)

`-LogLevel` on the setup scripts configures **NexusForever server** NLog output
only. The retail WildStar client has a separate logging system (`CLog`) controlled
by `-log*` command-line switches parsed at startup.

`Restart-NexusForeverLocal.ps1` defaults server `-LogLevel` to `Info` to keep
the normal edit-loop output readable. Pass `-LogLevel Trace` when you need
full server-side diagnostic output.

Prefer `-EnableClientLogging`, which appends `-logFile`, `-logFlush`, and
`-logDefaultLevel` for you. Combine with `-EnableClientConsole` when you also
want the in-game dev console:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ClientDirectory "D:\Games\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -PromptForRootPassword
```

Optional tuning:

| Switch | Effect |
|--------|--------|
| `-ClientLogLevel Trace` | Most verbose client threshold (`Error` … `Trace`; default `Trace`) |
| `-ClientLogStdout` | Also mirror CLog output to stdout |
| `-ClientLogDir "D:\WildStarLogs"` | Override default `<install>\Logs` |
| `-ClientArguments '-logNetwork','3'` | Pass any retail `-log*` switch verbatim |

Staged switches are written to `Client64\config.json` as `ExtraArguments` and
reused on later runs, the same way as `-EnableClientConsole`. Pass an explicit
override switch when you want to replace the staged profile.

After launch, tail client output:

```powershell
Get-ChildItem "D:\Games\WildStar\Logs\*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Wait -Tail 200
Get-ChildItem "D:\Games\WildStar\Errors\WildStar64*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Wait -Tail 200
```

Evidence-backed switch names, severity levels, init chain, and the distinction
between `CLog`, `DebugLogService`, and `Errors\` crash logs are documented in
[`Decomp/Analysis/CLIENT_LOGGING.md`](../Decomp/Analysis/CLIENT_LOGGING.md).

`-ClientArguments '-Console'` still works as a generic extra argument path, but
`-EnableClientConsole` is the preferred dedicated switch for this case.
Later local launcher and local restart runs now reuse the already staged
client `ExtraArguments` by default, so once you have enabled the client console
you do not need to repeat `-EnableClientConsole` unless you want to replace the
staged client arguments explicitly.

If your keyboard layout does not map the client's hard-coded toggle key cleanly,
or you do not want to guess which physical key corresponds to `0xC0`, run the
helper below after the WildStar window is open. It posts
WM_SYSKEYDOWN and WM_SYSKEYUP for virtual key `0xC0` directly to the running
client window instead of relying on the physical key mapping.

```powershell
.\Tools\Setup\Send-WildStarConsoleToggle.ps1
```

If the client was launched elevated through `NexusForever.ClientConnector`, the
helper will prompt for elevation automatically before it posts the messages.
If posted messages appear to succeed but the console still does not show, retry
from an already elevated PowerShell session with the stronger hardware-style
input path:

```powershell
.\Tools\Setup\Send-WildStarConsoleToggle.ps1 -FocusWindow -InputMethod SendInput -SkipElevationPrompt
```

Running the `SendInput` path from an already elevated shell avoids a fresh UAC
prompt stealing foreground focus away from the WildStar window just before the
injected key is sent.

What it does:

- runs `Initialize-NexusForever.ps1` with the same database and RabbitMQ
  options you pass through
- reuses reachable local MySQL and RabbitMQ endpoints when they already exist,
  or auto-starts portable Docker-backed replacements for missing local
  dependencies
- stages shared `tbl` and `map` runtime assets under `.nexusforever-runtime`
  and rewrites the runtime JSON files to those absolute paths
- creates or verifies the local player, gm, and admin login accounts
- starts the standalone server executables and waits for the local endpoints to
  come up before launching the client through `NexusForever.ClientConnector`
  when it is available in the current build output

For local development, this now mirrors the official client connection guide
more closely: the launcher writes `Client64\config.json` with the selected host
and language, stages the built `NexusForever.ClientConnector` runtime files into
the WildStar `Client64` directory, and then launches that staged copy from
there. If the PowerShell session is not already
elevated, Windows should prompt for elevation because the official guide says
the client connector should be run as Administrator.

The local launch path also writes a `RealmDataCenterId` into the staged client
config and defaults it to `6`. That matches the localhost or offline
`RealmDataCenter` row from the extracted client data. Earlier local launches
hardcoded `9`, which points `StoreBannerDataUrlTemplate` at the retired retail
host `http://static.wildstar-online.com/banners/` and can surface the client
log `malformed store banner data: 80072ee7` plus the store UI fallback. Both
the staged `NexusForever.ClientConnector` flow and the direct `WildStar64.exe`
fallback now use the same `-RealmDataCenterId` parameter so the two local
launch routes stay aligned. Override it only when you intentionally need a
different extracted client table row.

Storefront banners need a reachable `StoreBannerDataUrlTemplate` from
`RealmDataCenter.tbl`. For stock retail clients, use
`-StoreBannerMode HostsRedirect`: the launcher uses `RealmDataCenterId 9` when
no explicit ID is supplied, configures WorldServer to also listen on port 80,
and adds a Windows hosts entry mapping `static.wildstar-online.com` to
`127.0.0.1` so the existing retail banner URL resolves locally. This mode must
run from an elevated PowerShell session. For custom-files client builds, use
`-StoreBannerMode LooseData` to stage a loose
`Data\DB\RealmDataCenter.tbl` override pointing at
`http://localhost:5000/banners/`. Pass `-StoreBannerMode None` to disable local
banner setup.

Example stock-client banner launch:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 -ClientDirectory "D:\Games\WildStar" -StoreBannerMode HostsRedirect -PromptForRootPassword
```

When the client still fails in a way the server logs do not explain, check the
retail client's `Logs\*.txt` CLog files and `Errors\WildStar64*.log` reports
under the WildStar install root (one level above `Client64`). Enable file logging
with `-EnableClientLogging` or see [`Decomp/Analysis/CLIENT_LOGGING.md`](../Decomp/Analysis/CLIENT_LOGGING.md).
Those logs can preserve useful client-side diagnostics even for local emulator
sessions. For example,
`I:\WildStar\Errors\WildStar64.16042.Error.00007FF77A697597.93f3cb35.DAN.260530.215616.log`
captured `Error: Message size exceeds remaining!`, `Malformed Packet:
unPackFromStreamFn failure`, and `Invalid or foreign Message Id #988` for a
bad world-stream decode, which is much more actionable than a generic launcher
failure.

If you intentionally need different values for `-AuthHost` and `-PatcherHost`,
the script falls back to a direct `WildStar64.exe` launch because the current
client connector only supports one shared host.

The map generator now uses a bounded parallel worker count by default. Pass
`-MapGeneratorParallelism` to the launcher if you want to force a specific
worker count.

To compare single-worker and parallel map generation outside the launcher, use
the benchmark helper. It writes logs and outputs below `.nexusforever-runtime`
by default:

```powershell
.\Tools\Setup\Benchmark-MapGenerator.ps1 `
  -PatchPath "D:\Games\WildStar\Patch" `
  -Parallelism 4
```

The setup scripts create these local accounts by default:

- `player` / `player`: `Player` role (`roleId=1`) for ordinary gameplay.
- `gm` / `gm`: `GameMaster` role (`roleId=2`) for GM and spell/debug commands.
- `admin` / `admin`: `Administrator` role (`roleId=3`) for full local administration.

RBAC notes:

- `!spell` and other privileged chat commands are permission-gated. Use `gm` or
  `admin` when validating spell diagnostics in game.
- Fresh accounts created outside these setup scripts still fall back to the
  realm default `Player` role until you add rows in
  `nexus_forever_auth.account_role`.
- The command layer uses the same generic chat error for unknown commands and
  missing permission, so role assignment is the first thing to verify when a
  known command is rejected.

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

Force the scripts to use their portable Docker-backed database and broker
runtime even if local services are already listening on the default ports:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -DependencyMode PortableDocker `
  -ClientDirectory "D:\Games\WildStar"
```

## What It Creates

Emulator databases:

- `nexus_forever_auth`
- `nexus_forever_character`
- `nexus_forever_world`
- `nexus_forever_group`
- `nexus_forever_chat`
- `nexus_forever_friendship`

Reference dump databases, only when `-EnableDataMappingAuthoring` is passed:

- `jabbithole` from `jabbithole_mysql\*.sql`
- `wildstar_client` from `wildstar_client_mysql\*.sql`

Normal setup is runtime-only and skips these reference databases. Existing
authoring artifacts can be previewed or removed with
`Tools\Setup\Remove-DataMappingAuthoringArtifacts.ps1`.

Runtime auth data:

- `Tools\Setup\sql\runtime_auth_seed.sql` is imported into
  `nexus_forever_auth` after EF migrations and local account creation. It keeps
  local auth state compatible with current runtime expectations; currently it
  clears transient Fortune sessions so stale pre-guard card rows cannot crash
  Madame Fay's Fortune page on load.

Promoted runtime world data:

- `Tools\DataMapping\sql\runtime_world_seed.sql` is imported into
  `nexus_forever_world` after the official world database SQL files. This seed
  carries the reviewed DataMapping runtime rows and does not require running the
  mapper on a fresh machine.
- `Tools\DataMapping\sql\laughingws_map_entrance_seed.sql` is imported after
  the DataMapping seed when present. It adds missing matching `map_entrance`
  rows extracted from LaughingWS without replacing official rows.
- `Tools\DataMapping\sql\laughingws_city_content_seed.sql` is imported after
  the map entrance seed when present. It adds placed city/museum transport rows,
  Illium Museum relic/projector rows, the Halon Ring return ship pads, the
  WIP/GUESSED Illium Ringo Hax placement, and the WIP/GUESSED Skull's Eye
  escape pod terminal while skipping zero-coordinate placeholders and broad zone
  replacement SQL. It also applies `8` coordinate-keyed WIP/GUESSED
  `questChecklistIdx` updates to official Thayd/Illium housing intro props.
- `Tools\DataMapping\sql\laughingws_quest_instance_wip_seed.sql` is imported
  after the city content seed when present. It adds the WIP/GUESSED Dust Stalker
  quest-instance rows for Boss Xagg, the bridge controls, and the source-only
  exit panel while stripping the branch world delete and non-deterministic
  `@GUID` allocation.
- `Tools\DataMapping\sql\laughingws_small_world_wip_seed.sql` is imported after
  the WIP quest-instance seed when present. It adds `16` small WIP/GUESSED
  placed entity rows plus `59` `entity_stats` rows from reviewed Crimson
  Badlands, Northern Wastes, Farside, Tideborn Facility, Star-Comm Basin,
  Arcterra, Palaver Point, and Northern Wilds branch SQL, while stripping source
  world deletes and `@GUID` allocation. It also applies `140` coordinate-keyed
  WIP/GUESSED `questChecklistIdx` updates to official rows: `2` Everstar Grove
  teleporter rows, `14` Levian Bay objective rows, `33` Wilderrun interactable
  rows, `67` Auroria objective rows, and `24` Crimson Isle objective rows, with
  guarded Everstar Grove, Levian Bay, Auroria, and Crimson Isle fallback inserts
  for `107` `entity` rows and up to `219` fallback `entity_stats` rows when an
  older local import lacks those official rows. The Arcterra/Palaver rows plus
  Everstar Grove, Levian Bay, Wilderrun, Auroria, and Crimson Isle checklist
  indices remain WIP/GUESSED pending placement/stat/interaction or quest-smoke
  proof.
- `Tools\DataMapping\sql\laughingws_instance_entity_wip_seed.sql` is imported
  after the WIP small world seed when present. It adds `72` WIP/GUESSED
  instance-local `entity` rows plus `64` `entity_event`, `35` `entity_script`,
  and `80` `entity_stats` rows for Coldblood Citadel, Protostar SuperMall,
  Space Madness, Gauntlet, Fragment Zero, Infestation, Outpost M-13,
  Evil from the Ether drive-spark phase anchors, Protogames Academy,
  Ruins of Kel Voreth, Stormtalon's Lair, Skullcano,
  Sanctuary of the Swordmaiden, Genetic Archives, map-bound Ultimate
  Protogames, WIP-script-backed Red Moon Terror Laveka, Shade's Eve early
  anchors, and placed Datascape boss/objective anchors
  where current scripts/map bindings already exist. It also
  reconciles official Evil from the Ether branch area hints and crew-log
  `questChecklistIdx` order to the branch-derived route/`CrewLogEntityScript`
  behavior with `14` coordinate-keyed WIP/GUESSED updates. It strips source world deletes and
  `@GUID` allocation, and leaves full encounter choreography, NPC interaction,
  combat behavior, trigger cleanup, and cinematics blocked pending smoke proof.
- `Tools\DataMapping\sql\laughingws_live_event_wip_seed.sql` is imported after
  the WIP instance entity seed when present. It preserves branch-authored
  live-event entity/vendor rows with deterministic high entity IDs. The SQL
  comments mark this content as WIP/guessed because event lifecycle, spawn
  timing, cleanup, and retail placement proof are not complete.
- `Tools\DataMapping\sql\laughingws_housing_skyplot_wip_seed.sql` is imported
  after the WIP live-event seed when present. It preserves the branch Skyplot
  return pad plus the two housing vendors and their catalogue rows with
  deterministic high entity IDs. The generated SQL marks this as WIP/guessed and
  strips the source `DELETE FROM entity WHERE world = @WORLD` replacement.
- `Tools\DataMapping\sql\laughingws_store_catalog_seed.sql` is imported after
  the WIP Skyplot housing seed when present. It is an additive store-catalog overlay
  extracted from `LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more`,
  including reviewed fixed-ID event store rows, without that branch's DDL or
  broad world replacement SQL.
- `Tools\DataMapping\sql\laughingws_quest_loot_seed.sql` is imported after
  the store catalog seed when present. It adds the branch's virtual-item quest
  loot with DataMapping-owned high loot group IDs, without importing the source
  dump's DDL or foreign-key toggles.

When a LaughingWS entity seed has a new hash, setup clears only that seed's
owned deterministic high-ID range before re-importing it. The generated seed SQL
stays additive and delete-free, while setup prevents stale generated rows from
lingering after extractor changes:

- city content: `1100000000..1100099999`
- quest instance: `1100100000..1100199999`
- small world: `1100200000..1100299999`
- instance entity: `1100300000..1100399999`
- Skyplot housing: `2000000000..2099999999`
- live event: `2100000000..2147483647`

When `-EnableDataMappingAuthoring` is passed and either split folder is missing
or empty, the script falls back to the older single-file dump for that database:

- `all_jabbithole_mysql.sql`
- `all_wildstar_client_mysql.sql`

The reference databases get a `__nexusforever_imports` marker table during
authoring setup, so later authoring runs skip already imported files unless
their hash changed. With the split folders, markers are tracked per SQL file,
so a rerun can continue after the last completed file instead of starting the
whole dump again. Use `-RecreateLargeDumpDatabases` with
`-EnableDataMappingAuthoring` only when you intentionally want to drop and
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

Create databases, copy configs, build, and migrate without the official world
database import:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -SkipWorldDatabaseImport
```

Import the development reference databases for DataMapping authoring:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -EnableDataMappingAuthoring
```

Preview and then remove old DataMapping authoring artifacts from an existing
local server:

```powershell
.\Tools\Setup\Remove-DataMappingAuthoringArtifacts.ps1 -PromptForRootPassword
.\Tools\Setup\Remove-DataMappingAuthoringArtifacts.ps1 -PromptForRootPassword -Apply
```

Skip the promoted runtime seed overlays while still importing the official world
database:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -SkipRuntimeWorldSeedImport
```

Recover from a failed partial authoring reference SQL import without dropping
completed reference databases:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf -EnableDataMappingAuthoring -RepairPartialLargeDumpDatabases
```

Force both reference databases to be dropped and rebuilt:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -EnableDataMappingAuthoring -RecreateLargeDumpDatabases
```

Use custom split SQL folder locations:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 `
  -PromptForRootPassword `
  -EnableDataMappingAuthoring `
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

`MySQL/MariaDB is reachable ... but no mysql client was found`:

- Install MariaDB/MySQL client tools.
- Or install Docker Desktop so the setup script can use a transient MariaDB
  client container against the reachable server.

`ERROR 1264 ... Out of range value for column 'cost'`:

- Re-run authoring setup with
  `-EnableDataMappingAuthoring -RepairPartialLargeDumpDatabases`.
- The importer now normalizes reference SQL `INTEGER` columns to `BIGINT`.
- Do not pass `-KeepLargeDumpIntegerColumns` unless you want the old behavior.

`ERROR 1118 ... Row size too large`:

- Re-run authoring setup with
  `-EnableDataMappingAuthoring -RepairPartialLargeDumpDatabases`.
- The importer now converts over-wide `VARCHAR(...)` declarations to `TEXT`
  inside affected `CREATE TABLE` blocks.

`ERROR 1064 ... near 'maxValue ...'`:

- Re-run authoring setup with
  `-EnableDataMappingAuthoring -RepairPartialLargeDumpDatabases`.
- The importer now quotes generated column identifiers in `CREATE TABLE`
  blocks.

`ERROR 1049 ... Unknown database 'wildstar_client'` during the client dump:

- Re-run authoring setup with
  `-EnableDataMappingAuthoring -RepairPartialLargeDumpDatabases`.
- The importer now selects the target database inside each stream and on client
  reconnects, instead of relying only on the startup `--database` argument.

Reference databases are being dropped every run:

- That only happens during authoring setup when
  `-RecreateLargeDumpDatabases` is passed.
- Omit that switch for normal runs; matching imports are skipped by hash marker.
- Use `-RepairPartialLargeDumpDatabases` after a failed import to rebuild only
  incomplete reference databases.
- During split-folder imports, `-RepairPartialLargeDumpDatabases` can also
  repair a single unmarked file by dropping just that file's existing table and
  importing it again.

`ERROR 1050 ... Table '...' already exists` during a split-folder import:

- Re-run authoring setup with
  `-EnableDataMappingAuthoring -RepairPartialLargeDumpDatabases`.
- The script will detect the table created by the unmarked file, drop only that
  table, and import the file again.

An older monolithic import already exists:

- Normal authoring runs leave a completed older single-file import intact.
- To switch that database to split-file markers, run once with
  `-EnableDataMappingAuthoring -RecreateLargeDumpDatabases`.
- After that, omit `-RecreateLargeDumpDatabases`; the per-file markers will
  skip completed split imports.

`dotnet-ef is not available`:

- Pass `-InstallDotNetEf`, or run `dotnet tool install dotnet-ef --global`.

RabbitMQ command not found:

- Install RabbitMQ and make `rabbitmqctl` available on `PATH`.
- Or pass `-SkipRabbitMqUser` and create the `nexusforever` broker user manually.

`RabbitMQ broker is not reachable on localhost:5672` during local launch:

- Start the RabbitMQ service before running `Start-NexusForeverLocal.ps1`.
- If your broker is listening elsewhere, pass the correct `-BrokerHost` and `-BrokerPort` values.
- The chat, group, friendship, character, and world servers all depend on the AMQP broker during startup.

`RabbitMQ is not reachable ... install Docker Desktop`:

- No reachable broker was found on the configured host and port.
- Install Docker Desktop if you want the scripts to provision a portable local
  RabbitMQ instance automatically.
- On Windows, the setup scripts now attempt to start Docker Desktop automatically
  when the Docker CLI is installed but the daemon is stopped.
- Or pass `-DependencyMode ExternalOnly` and point the scripts at an existing
  broker.

`Docker Desktop was started but the Docker daemon did not become ready`:

- Wait for Docker Desktop to finish starting, then rerun the setup script.
- Confirm Docker Desktop is installed and that WSL2 or Hyper-V backend is healthy.
- Use `-DependencyMode ExternalOnly` if you prefer to manage MySQL and RabbitMQ
  outside Docker.

`Portable dependencies were created for the first time, but -SkipSetup was requested`:

- Run once without `-SkipSetup` so the freshly provisioned MariaDB and
  RabbitMQ runtime can be initialized with users, databases, migrations, and
  imported data.
- After that first successful run, `-SkipSetup` can reuse the same portable
  dependency containers.

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
