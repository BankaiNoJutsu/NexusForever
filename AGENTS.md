# AGENTS.md

## Scope

This file applies to the whole repository. At the time it was written there
were no nested `AGENTS.md` files; re-scan before assuming that is still true.

Use this as the quick orientation file for Codex sessions. Keep it concise and
update it when repeated session friction appears.

## First Files To Read

- `README.md` for the project summary, requirements, and quick commands.
- `Tools/Setup/README.md` before changing local setup, launch, database, broker,
  or client runtime flows.
- `CURRENT_STATUS.md` for the 36 feature-area completion snapshot and consolidated
  emit/producer gaps before broad status or parity discussions.
- `Decomp/Analysis/README.md` and `Decomp/Analysis/CONTINUATION_GUIDE.md`
  before any client-binary, Ghidra, opcode, packet, spell, or evidence-backed
  implementation work.
- `Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md` before live-client passes on
 partial gameplay/economy/social blockers (crafting, transport, matching, guild,
 ICComm, duels, fortune).
- `Tools/DataMapping/README.md` and `Tools/DataMapping/sql/README.md` before
  changing data-mapping scripts, staging tables, or safe world imports.
- `Tools/NexusForever.LoadTest/README.md` before changing or running the
  login-to-world profiling harness.

## Repo Map

- `Source/NexusForever.sln` is the primary .NET solution. `Source/NexusForever.slnx`
  is also present.
- `Source/Directory.Packages.props` centrally pins NuGet package versions.
- `Source/NexusForever.*` contains the emulator services, shared libraries,
  network models, game logic, database contexts, EF migrations, Aspire app host,
  and tests.
- `Source/NexusForever.Game.Tests` contains the current xUnit test project.
- `Tools/Setup` contains the local database/broker/client setup and launch
  PowerShell scripts.
- `Tools/DataMapping` contains Python mapping scripts, generated mapping
  outputs, review queues, and SQL promotion/verification scripts.
- `Tools/NexusForever.LoadTest` contains the load/profiling harness.
- `Tools/QuestTableInspector` contains focused table-inspection helpers.
- `Decomp/Analysis` contains the repeatable Ghidra workflow, durable labels,
  findings, and continuation playbooks.
- `Obsolete` is historical. Do not derive current build or CI expectations from
  it unless the task is explicitly about legacy material.

## Environment Facts

- Projects target `net10.0`. The root README requires Visual Studio 2026 with
  .NET 10 and C# 14 support.
- No `global.json` was found, so the installed .NET SDK controls the build.
- Most source projects have nullable disabled; some newer tools and Aspire
  projects enable nullable. Match the containing project.
- Runtime setup needs MySQL or MariaDB, RabbitMQ or Azure Service Bus, and a
  WildStar build 16042 client. `Tools/Setup` can provision portable Docker-backed
  MySQL/RabbitMQ dependencies when configured to do so.
- DataMapping Python scripts use the standard library and the MySQL command-line
  client; no Python package manifest was found.
- No active CI workflow file was found in the repo. `Obsolete/.travis.yml` is
  historical, while the root README links Azure build badges.

## Common Commands

Run these from the repository root unless noted.

Restore:

```powershell
dotnet restore Source\NexusForever.sln
```

Build the full solution:

```powershell
dotnet build Source\NexusForever.sln -v minimal --nologo
```

For a narrow C# edit loop, build the owning project instead of the whole
solution, for example:

```powershell
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
```

Run the current tests:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

There is no separate repo-specific lint, format, or typecheck command
documented in this checkout. Treat `dotnet build` as the C# compile/typecheck
gate, and preserve local formatting style in touched files.

Initialize standalone local dependencies and databases:

```powershell
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf
```

Start the full local standalone flow:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 -ClientDirectory "D:\Games\WildStar" -PromptForRootPassword
```

After the first setup, restart only auth/world for a faster local loop:

```powershell
.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1 -ClientDirectory "D:\Games\WildStar" -PromptForRootPassword
```

Run the Aspire app host:

```powershell
dotnet run --project Source\NexusForever.Aspire.AppHost\NexusForever.Aspire.AppHost.csproj
```

Run the default DataMapping pass:

```powershell
python Tools\DataMapping\map_wildstar_data.py
```

Run a DataMapping smoke pass:

```powershell
python Tools\DataMapping\map_wildstar_data.py --limit-creatures 200 --limit-relation-rows 500 --limit-spawns 500 --output-dir Tools\DataMapping\output_test
```

Verify safe world imports after applying DataMapping staging:

```powershell
Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

Set up and run the decompile workflow only when the task needs it:

```powershell
.\Decomp\Analysis\setup_decomp_tools.ps1
.\Decomp\Analysis\run_ghidra_analysis.ps1
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
.\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
```

For multi-binary export refreshes, prefer runner-level per-target parallelism:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ProjectLayout PerTarget -MaxDecompiledFunctions 2400 -MaxParallel 3
```

Run the load-test harness commands listed in
`Tools/NexusForever.LoadTest/README.md` only against a prepared local server.

## Durable Work Memory

- Treat `~/vault` as durable work memory when cross-session notes are useful.
- Prefer updating canonical notes over creating near-duplicate notes or spreading the same state across multiple files.
- Route notes intentionally: TODOs to task notes, people details to people notes, project state to project notes, daily summaries to daily notes, and temporary thoughts to scratch notes.
- When recording durable work, preserve decisions, blockers, owners, dates, and useful links back to repo files, issues, PRs, or evidence.
- If nothing meaningful changed, do not churn the vault with timestamp-only or wording-only edits.

## Conventions And Constraints

- Inspect relevant docs and source before editing. Prefer `rg` for repo search.
- Do not change application behavior for documentation-only tasks.
- Match the style of the file being edited. Production code commonly uses
  block-scoped namespaces, constructor dependency injection, explicit types in
  many places, and local `var` where the type is obvious. Newer tools/tests may
  use file-scoped namespaces and collection expressions.
- Keep project references and package versions consistent with
  `Source/Directory.Packages.props`.
- For database schema work, keep EF models, migrations, and model snapshots
  consistent. Migrations live under the corresponding `Source/NexusForever.Database.*`
  project and are applied by `Source/NexusForever.Aspire.Database.Migrations`.
- Runtime code must not query `wildstar_client`, `jabbithole`, or `nf_map_*`
  staging/reference tables. Promote reviewed data into runtime-owned world/auth
  tables, scripts, or code-owned assets first.
- For local investigation of achievement IDs, names, checklist rows, and related
  client data, query the imported MySQL reference databases first:
  `wildstar_client` has `achievement`, `achievementchecklist`,
  `achievementtext`, `achievementgroup`, and related tables; `jabbithole` may
  also have useful `achievements`, `achievement_titles`, and
  `character_achievements` data. Do not build scratch TBL readers for this
  unless the reference DBs are unavailable or stale.
- For decompile work, follow the evidence ladder in
  `Decomp/Analysis/CONTINUATION_GUIDE.md`. Do not implement behavior from a
  single observation, do not copy decompiled client source into the repo, and
  leave unknown fields diagnostic-only or explicitly blocked.
- Do not broaden packet, spell, crypto, token, anti-tamper, or runtime state
  behavior beyond mapped and verified evidence.
- `Source/NexusForever.WorldServer` imports `ExtractGameTableFiles.targets` and
  `GenerateBaseMapFiles.targets` only when `ExtractGameTables` or
  `GenerateMapFiles` is set. Those targets need `WsInstallDir` pointing at a
  WildStar client `Patch` directory.
- Treat `Obsolete` as archival. If reviving anything from it, first validate it
  against current `net10.0` projects and docs.

## Generated And Local Files

Avoid editing or committing generated/local artifacts unless a task explicitly
asks for that artifact:

- `bin`, `obj`, `Debug`, `Release`, `.vs`, `.idea`, `artifacts`, `.nexusforever-runtime`.
- `*.tbl`, `*.nfmap`, generated map/table outputs in build directories.
- `all_jabbithole_mysql.sql`, `all_wildstar_client_mysql.sql`,
  `jabbithole_mysql`, `wildstar_client_mysql`, `SQL_Loot`, and `Various SQL`
  dumps.
- `Tools/DataMapping/output`, `Tools/DataMapping/output_test`, and generated
  apply/report files under those output folders.
- `Tools/DataMapping/review` unless the task is specifically a manual mapping
  review. Use `Tools/DataMapping/creature_bridge_overrides.example.csv` as the
  tracked template.
- `Decomp/Client64`, `Decomp/Analysis/ghidra_projects`,
  `Decomp/Analysis/exports`, and `Decomp/Analysis/logs`. Durable decompile
  knowledge belongs in tracked labels, findings, coverage summaries, and
  focused tracker docs.
- `artifacts/` (gitignored): local wiki dumps, load-test JSON, build scratch,
  and live-session capture JSON. Promote audit snapshots to
  `Tools/WikiArchiveAudit/reports/` or quest coverage to
  `Decomp/Analysis/coverage/` when they should be versioned.
- Local server JSON files may contain machine-specific settings. Prefer editing
  `*.example.json` templates or setup scripts unless the task calls for a local
  runtime config change.

## Done When

For any task:

- The relevant docs/source were inspected before editing.
- The change stays within the requested behavior and ownership boundary.
- No unrelated user changes were reverted.
- Generated/local artifacts are left untouched unless intentionally regenerated.
- Verification was run at the narrowest useful scope, or the blocker is named.
- The final response lists files changed, verification commands/results,
  blockers or unknowns, and useful follow-up.

For C# behavior changes:

- Build the changed project or full solution.
- Add or update focused xUnit tests when the change affects shared logic,
  packet/model contracts, database boundaries, or regression-prone behavior.
- Run `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj`
  when tests are relevant or changed.

For setup/database/data-mapping changes:

- Exercise the affected script path when safe, or run a smoke mode.
- Document required services, credentials, and any skipped live verification.
- For safe world imports, include the SQL verification result when applied.

For decompile/evidence-backed work:

- Record labels/findings/tracker updates with enough addresses, function names,
  export paths, and source paths for the next session to rediscover the evidence.
- End in one of the states from `Decomp/Analysis/CONTINUATION_GUIDE.md`:
  mapped-only with an explicit blocker, implemented with verification, or
  rejected with the reason recorded.
