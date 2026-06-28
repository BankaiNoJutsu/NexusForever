# NexusForever Client64 Decompilation Setup

This folder contains a repeatable Ghidra headless workflow for analyzing the native
Windows client binaries in `Decomp\Client64`.

The first-pass goal is not a perfect source recreation. It is to build durable
reverse-engineering artifacts that help map the WildStar client to NexusForever
data tables, packets, opcodes, auth/realm flow, and world-session behavior.

## What Gets Analyzed

By default `run_ghidra_analysis.ps1` analyzes the client-owned binaries that are
most likely to contain useful protocol and data-loading code:

- `WildStar64.exe`
- `Houston64.exe`
- `StsConnLib64.MT.dll`

Third-party DLLs such as DirectX, Steam, Bink, and dbghelp are skipped by default.
Use `-AllClientBinaries` if you intentionally want to import everything.

## Toolchain

`setup_decomp_tools.ps1` installs a portable toolchain under:

```powershell
$env:USERPROFILE\.codex\tools\nexusforever-decomp
```

Pinned downloads:

- Eclipse Temurin JDK 21.0.11+10, Windows x64 ZIP
- Ghidra 12.0.4 public release ZIP

Both downloads are SHA-256 checked before extraction. The scripts do not require
system-wide Java or Ghidra installation.

## Recreate The Setup

From the repository root:

```powershell
.\Decomp\Analysis\setup_decomp_tools.ps1
```

Run the default analysis:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1
```

The runner automatically applies function names from
`Decomp\Analysis\function_labels.csv` before export. This keeps high-value
function mapping reproducible even if `ghidra_projects` is deleted and rebuilt.
`selected_decompiled.c` now defaults to manifest-based reuse, so repeated runs
with unchanged binary, label, and selection inputs do not rebuild the selected
output file. The exporter also maintains a canonical per-function cache under
`selected_decompiled_cache\functions\<binary-fingerprint>` and reuses an
already-exported function body instead of writing another physical copy. Old
context-hash cache folders are removed by the exporter; only `functions` is
kept, used, and populated. Normal runs incrementally warm this full-program
cache up to `-MaxWarmFunctionsPerRun`, so repeated passes steadily reduce the
number of uncached functions.
Single-target runs now default to isolated per-target Ghidra projects, which
removes project-lock contention between targeted runs against different
binaries. Multi-target and full-pass runs keep the shared project by default.
The runner also holds a small per-project gate under `ghidra_projects` before
opening Ghidra, so concurrent sessions wait instead of surfacing Ghidra's
`Unable to lock project` failure. If an interactive Ghidra session or older
runner already owns the project, the updated runner retries until the project is
free; use `-ProjectLockTimeoutMinutes <minutes>` when automation should stop
waiting after a bounded period.
For multi-binary export refreshes, prefer `run_ghidra_analysis.ps1 -MaxParallel`;
it starts one isolated runner per target, writes per-run summaries under
`logs\runs`, and refreshes coverage once after all workers complete.

When switching from the headless export loop to an interactive `ghidra-mcp`
inspection pass, print the MCP-ready project and evidence-file hints first:

```powershell
.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe
```

The helper reads `logs\LATEST_RUN_SUMMARY.json`, analysis manifests, and known
`.gpr` files. It does not modify Ghidra projects or exports. Open the listed
project in Ghidra, use `mcp__ghidra_mcp.list_instances`, then connect with the
printed project query. MCP comments and tentative renames are useful for focused
inspection, but durable labels still belong in `function_labels.csv` followed
by an export-only verification pass. See
[`GHIDRA_MCP_PROMPT_ADAPTER.md`](GHIDRA_MCP_PROMPT_ADAPTER.md) for how to apply
the upstream `bethington/ghidra-mcp` prompt workflows without losing the
source-controlled Nexus evidence trail, and
[`DATA_TYPE_INVESTIGATION_WORKFLOW.md`](DATA_TYPE_INVESTIGATION_WORKFLOW.md) for
generic pointer and structure typing passes.

Run a smaller or larger decompiler export:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -MaxDecompiledFunctions 350
```

Re-export from the existing Ghidra project after changing the export script,
without re-running full analysis:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly
```

`-AnalysisMode Auto` is the default. It imports and analyzes a target only when
the Ghidra project or analysis manifest is missing or stale; otherwise it uses
`-process -noanalysis`. `-ExportOnly` remains a compatibility alias for
`-AnalysisMode Skip`, and `-AnalysisMode Force` forces a fresh import/analysis.

Bound the full-program cache warming done during a normal run:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -Targets WildStar64.exe -CacheWarmMode Incremental -MaxWarmFunctionsPerRun 250
```

For full-program `Complete` cache passes on large binaries, raise the headless JVM heap
(default Ghidra `MAXMEM` is 2G):

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -CacheWarmMode Complete -GhidraMaxHeap 8G
```

See `FULL_DECOMPILE_PASS_STATUS.md` for the 2026-05-27 full decompile pass notes.

Promote evidence-backed rows into `function_labels.csv` from exports (including `DB\*.tbl`
xrefs):

```powershell
.\Decomp\Analysis\Promote-FunctionLabelsFromExports.ps1
```

See `FUNCTION_LABEL_MAPPING_PASS.md` for the 2026-05-28 label/comment pass.

Use `-CacheWarmMode Off` for a no-decompile metadata/xref pass, or
`-CacheWarmMode Complete` when you intentionally want to warm every missing
function for the current target.

When `-ExtraPostScript` is used without an explicit `-MaxDecompiledFunctions`,
the runner inherits the current manifest cutoff for that target so helper
passes do not accidentally shrink `selected_reasons_summary.csv` or the
coverage snapshot. Pass `-MaxDecompiledFunctions` explicitly when you
intentionally want a smaller or larger focused export.

When a pass only needs helper-script output, add `-SkipDefaultExport` to skip
the normal `ExportNexusForeverAnalysis.java` export and reopen the saved Ghidra
program just once for the helper work.

Inspect the latest decompile manifests, selected-function counts, and fragment
reuse after a run:

```powershell
.\Decomp\Analysis\Test-DecompileManifest.ps1
```

Fail fast when the latest run has a missing manifest, stale output, or a
fingerprint mismatch against `logs\LATEST_RUN_SUMMARY.json`:

```powershell
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
```

Generate the latest export and opcode coverage snapshot without rerunning Ghidra:

```powershell
.\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
```

Single-target runs use per-target projects by default. If an older shared
project exists but the split project has not been created yet, export-only Auto
mode falls back to the shared project for compatibility. Run the same target
once without `-ExportOnly` to seed the split project.

Force the legacy shared project layout for a targeted run:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ProjectLayout Shared
```

Force a split per-target project even when running multiple targeted passes from
automation:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -Targets WildStar64.exe -ProjectLayout PerTarget
```

Seed or refresh the default target projects in parallel:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -AnalysisMode Force -ProjectLayout PerTarget -MaxParallel 3
```

Re-export the default targets in parallel from existing per-target projects:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -AnalysisMode Skip -ProjectLayout PerTarget -MaxDecompiledFunctions 2400 -MaxParallel 3
```

`Start-DecompileBatch.ps1` remains available as a compatibility wrapper for the
older command shape.

Force a fresh `selected_decompiled.c` rebuild after manual Ghidra project edits
that are not captured by `function_labels.csv`:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -DecompileMode Force
```

Skip `selected_decompiled.c` entirely when you only need the cheaper CSV and
string/xref exports:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -DecompileMode Skip
```

Re-export or analyze only one binary while iterating on a focused subsystem:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets StsConnLib64.MT.dll -MaxDecompiledFunctions 260
```

The default export now includes repeatable function-pointer family inventories
for vtable, callback-table, and dispatcher-family recovery. Attach a focused
helper script when you need nearby raw data or instruction windows after that
first pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140b73540','20')
```

Skip the default export when you only need the helper-script output:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140b73540','20')
```

Batch `FindDataReferences.java` probes for many addresses in one saved-program
session instead of reopening Ghidra once per address:

```powershell
$addrs = @('1405c0f00','1405c11c0','1405c1fc0','1405c26a0','1405c2d40','1405c2e70','1405c3360')
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe `
  -ExtraPostScript FindDataReferences.java -ExtraPostScriptArgs (@('--maxRefs','8') + $addrs)
```

Trace direct callers of a mapped function when a subsystem appears to be wired
through registration tables or callback dispatch:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ExtraPostScript TraceFunctionCallers.java -ExtraPostScriptArgs @('140636280','6')
```

Use the ASCII dumper when a short inline string has not been auto-defined in the
listing:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ExtraPostScript DumpAsciiAtAddress.java -ExtraPostScriptArgs @('140b2f000','16')
```

Skip source-controlled labels when testing raw Ghidra output:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -NoApplyLabels
```

Analyze every `.exe` and `.dll` in `Decomp\Client64`:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -AllClientBinaries
```

## Output Layout

Exports are written under `Decomp\Analysis\exports\<binary-name>\`.

Per binary:

- `summary.txt` - PE/Ghidra metadata and memory blocks.
- `functions.csv` - function address/name index.
- `imports.csv` - external symbols/imports and reference counts.
- `strings.csv` - all strings Ghidra defined.
- `string_xrefs.csv` - xref address and containing function for interesting strings.
- `interesting_strings.csv` - packet/network/data/auth/gameplay keyword hits.
- `selected_xrefs.csv` - functions selected because they reference interesting strings or imports.
- `selected_reasons_summary.csv` - why each selected function made the focused export and whether it is inside the current decompile cutoff.
- `selected_call_edges.csv` - direct caller/callee edges for selected functions, including call/jump site addresses and selected/cutoff state.
- `function_pointer_families.csv` - contiguous exact-function pointer families from non-executable memory, useful for vtable, callback-table, and dispatcher recovery.
- `function_pointer_family_slots.csv` - per-slot breakdown for each exported function-pointer family, including namespace, thunk state, and slot reference counts.
- `selected_decompiled.c` - Ghidra C output for the selected functions.
- `selected_decompiled.manifest` - local cache metadata used to reuse `selected_decompiled.c` when the export inputs are unchanged.
- `decompile_cache_summary.properties` - full-program cache counts, including cached, warmed, remaining, and already-exported skip totals.
- `selected_decompiled_cache/functions/<binary-fingerprint>` - canonical per-function fragment cache. A function that already exists here is reused and is not re-exported into another context directory.

Function labels are maintained in `Decomp\Analysis\function_labels.csv` and
applied by `scripts\ApplyNexusForeverLabels.java` before `selected_decompiled.c`
is written. Changing the label file invalidates the selected-output manifest,
but it does not create another physical copy of an already-exported function in
the canonical cache.

Focused helper scripts under `Decomp\Analysis\scripts` can be run with
`-ExtraPostScript`, after the normal export, for one-off inspection without
changing the repeatable CSV export shape.

`Get-GhidraMcpWorkflowHints.ps1` bridges the same local artifacts into
interactive `ghidra-mcp` sessions. Use it before opening CodeBrowser when a pass
needs MCP-assisted function inspection, datatype review, live debugger tracing,
or a quick handoff from a latest run summary to the correct per-target project.
`GHIDRA_MCP_PROMPT_ADAPTER.md` maps the upstream prompt README to the local
evidence ladder, label file, and tracker expectations.

The Ghidra project is kept under `Decomp\Analysis\ghidra_projects` so the same
analysis can be opened interactively in Ghidra later. Single-target Auto runs
create per-target projects such as `NexusForeverClient64_WildStar64`, while
shared runs keep using `NexusForeverClient64`.
Project gate files are kept under
`Decomp\Analysis\ghidra_projects\.project_locks`; they are local ignored files
and are safe to leave behind because the live file handle, not the file content,
is the lock.

Logs are written under `Decomp\Analysis\logs`.
Batch runs write per-target summaries and job output logs under
`Decomp\Analysis\logs\runs\<run-id>\`. The batch wrapper then writes
`batch_summary.json` in that run folder and updates `logs\LATEST_RUN_SUMMARY.json`
unless `-NoLatestSummary` is supplied. Individual `run_ghidra_analysis.ps1`
workers can also use `-RunId`, `-SummaryPath`, and `-SkipCoverage` directly when
automation needs collision-free output.
When omitted, batch run ids include milliseconds and the PowerShell process id
so two sessions started in the same second do not collide in `logs\runs`.

`logs\LATEST_RUN_SUMMARY.json` now includes a per-target manifest summary with
selected-function counts and cache reuse/decompile counts, so targeted passes
can confirm whether they reused the existing export or invalidated it.

`Get-DecompCoverageSnapshot.ps1` writes reviewable coverage artifacts under
`Decomp\Analysis\coverage` and keeps the machine-oriented JSON snapshot under
`Decomp\Analysis\logs`:

- `coverage\LATEST_COVERAGE_SUMMARY.md` - human-readable snapshot of export and opcode coverage.
- `logs\LATEST_COVERAGE_SUMMARY.json` - machine-readable summary for automation and handoffs.
- `coverage\export_coverage_inventory.csv` - per-target export coverage counts.
- `coverage\opcode_coverage_inventory.csv` - opcode/model/handler inventory from `GameMessageOpcode.cs` and message source files.

`run_ghidra_analysis.ps1` refreshes these coverage artifacts after each run.

Durable mapping ledgers live under `Decomp\Analysis\mapping_artifacts`:

- `native_opcode_provenance.csv` - opcode to native registration, payload functions, source model/handler, status, and blocker.
- `function_evidence_inventory.csv` - evidence behind durable labels and mapped function families.
- `structure_offset_maps.csv` - wire, memory, XML, or table offsets with size and access evidence.
- `dispatcher_registration_exports.csv` - curated registration-table, dispatcher, vtable, and callback-family rows.
- `source_traceability_map.csv` - native evidence to NexusForever source and test coverage.
- `live_evidence_bundle_manifest.csv` - repeatable live-client capture plans and results.
- `packet_fixture_plan.csv` - packet layouts that should become encode/decode fixtures.

Validate those ledgers after edits:

```powershell
.\Decomp\Analysis\Validate-DecompileMappingArtifacts.ps1
```

Audit full-map completion gaps without mutating ledgers:

```powershell
.\Decomp\Analysis\Get-DecompileMappingArtifactGaps.ps1
```

Use `-FailOnGap` when the audit should act as a completion gate.

Backfill missing function-evidence rows from durable labels:

```powershell
.\Decomp\Analysis\Update-FunctionEvidenceInventoryFromLabels.ps1
```

Backfill missing opcode-provenance rows from coverage:

```powershell
.\Decomp\Analysis\Update-NativeOpcodeProvenanceFromCoverage.ps1
```

Backfill traceability and live-evidence rows from opcode provenance:

```powershell
.\Decomp\Analysis\Update-SourceTraceabilityFromOpcodeProvenance.ps1
.\Decomp\Analysis\Update-LiveEvidenceManifestFromBlockedProvenance.ps1
```

The client binaries, Ghidra projects, exports, and raw logs are reproducible
local artifacts and are ignored by Git. Coverage reports live outside the
ignored `logs` tree so they can be reviewed and committed when useful.

See `INITIAL_FINDINGS.md` for the first pass of protocol/data anchors found in
the generated exports.

See [mapping_artifacts/README.md](mapping_artifacts/README.md) for the durable
seven-layer mapping workflow that links native evidence to source traceability,
live evidence bundles, and packet fixture planning.

See [CLIENT_LOGGING.md](CLIENT_LOGGING.md) for evidence-backed retail client
`CLog` switches, severity levels, and NexusForever launcher integration.

See [CONTINUATION_GUIDE.md](CONTINUATION_GUIDE.md) for the repeatable map,
label, implement, and verification workflow to use when continuing the
client-binary decompile.

See [DATA_TYPE_INVESTIGATION_WORKFLOW.md](DATA_TYPE_INVESTIGATION_WORKFLOW.md)
for the structure and parameter type investigation checklist to use when generic
Ghidra types obscure packet, table, entity, item, spell, or callback layouts.

Quest coverage (curated scripts vs generic table-driven vs blocked objective
types) is tracked in [QUEST_IMPLEMENTATION_STATUS.md](QUEST_IMPLEMENTATION_STATUS.md).
Row-level content retail-completeness across quests, public events, path
missions, dungeons, raids, generated inventories, the next-slice queue, and
blocker details is tracked in
[CONTENT_RETAIL_COMPLETENESS_TRACKER.md](CONTENT_RETAIL_COMPLETENESS_TRACKER.md).
Regenerate the quest inventory with
`python Tools/WikiArchiveAudit/quest_implementation_audit.py`; regenerate and
validate the content retail-completeness tracker with
`python Tools/WikiArchiveAudit/content_retail_completeness_audit.py` and
`python Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py`.

See [EVIDENCE_LOOP_PROCEDURE.md](EVIDENCE_LOOP_PROCEDURE.md) for the spell and
packet evidence workflow that pairs the decompile exports with fixture SQL,
`/spell inspect4`, `/spell cast4`, diagnostics, and packet comparisons.

For repo-wide feature-area completion (36 matrix rows), see
[`CURRENT_STATUS.md`](../../CURRENT_STATUS.md) at the repository root.

For gameplay/economy/social partial features (crafting, transport, matching,
guild, ICComm, duels, fortune), use [GAMEPLAY_ECONOMY_SOCIAL_STATUS.md](GAMEPLAY_ECONOMY_SOCIAL_STATUS.md),
[BLOCKER_EVIDENCE_PLAN.md](BLOCKER_EVIDENCE_PLAN.md), and matching detail in
[MATCHING_IMPLEMENTATION_STATUS.md](MATCHING_IMPLEMENTATION_STATUS.md).
Use `.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1` for Trace-logged client
smoke passes.

## How To Use The First-Pass Output

Good starting points for packet/data mapping:

1. Search `interesting_strings.csv` for `opcode`, `packet`, `message`, `auth`,
   `realm`, `world`, `send`, `recv`, `spell`, `quest`, `entity`, and `.tbl`.
2. Open the matching entries in `selected_xrefs.csv` to find the function
   addresses that reference those strings or network imports.
3. Read the matching sections in `selected_decompiled.c`.
4. Compare the observed constants and call shapes with
   `Source\NexusForever.Network\Message\GameMessageOpcode.cs` and the packet
   model classes in `Source\NexusForever.Network.World\Message\Model`.

Expect generated function names such as `FUN_140123456` until functions are
renamed in the Ghidra project. Keep manual renames in the project rather than in
the exported C, then re-run the export when useful.
