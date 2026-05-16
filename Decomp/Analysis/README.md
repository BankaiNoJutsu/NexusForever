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
with unchanged binary, label, and selection inputs do not re-run the decompiler.
When the selected set changes slightly, the exporter also reuses cached
per-function fragments for the unchanged functions and only decompiles the new
or invalidated entries.
Single-target runs now default to isolated per-target Ghidra projects, which
removes project-lock contention between targeted runs against different
binaries. Multi-target and full-pass runs keep the shared project by default.

Run a smaller or larger decompiler export:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -MaxDecompiledFunctions 350
```

Re-export from the existing Ghidra project after changing the export script,
without re-running full analysis:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly
```

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

Attach a focused helper script to an export-only pass when chasing callback
tables or raw code stubs:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140b73540','20')
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
- `selected_decompiled.c` - Ghidra C output for the selected functions.
- `selected_decompiled.manifest` - local cache metadata used to reuse `selected_decompiled.c` when the export inputs are unchanged.
- `selected_decompiled_cache/<context-hash>` - local per-function fragment cache used to avoid re-decompiling unchanged functions when the selected set expands or contracts.

Function labels are maintained in `Decomp\Analysis\function_labels.csv` and
applied by `scripts\ApplyNexusForeverLabels.java` before `selected_decompiled.c`
is written. Changing the label file invalidates the manifest and fragment cache
context automatically.

Focused helper scripts under `Decomp\Analysis\scripts` can be run with
`-ExtraPostScript`, after the normal export, for one-off inspection without
changing the repeatable CSV export shape.

The Ghidra project is kept under `Decomp\Analysis\ghidra_projects` so the same
analysis can be opened interactively in Ghidra later. Single-target Auto runs
create per-target projects such as `NexusForeverClient64_WildStar64`, while
shared runs keep using `NexusForeverClient64`.

Logs are written under `Decomp\Analysis\logs`.

`logs\LATEST_RUN_SUMMARY.json` now includes a per-target manifest summary with
selected-function counts and cache reuse/decompile counts, so targeted passes
can confirm whether they reused the existing export or invalidated it.

The client binaries, Ghidra projects, exports, and logs are reproducible local
artifacts and are ignored by Git.

See `INITIAL_FINDINGS.md` for the first pass of protocol/data anchors found in
the generated exports.

See [CONTINUATION_GUIDE.md](CONTINUATION_GUIDE.md) for the repeatable map,
label, implement, and verification workflow to use when continuing the
client-binary decompile.

See [EVIDENCE_LOOP_PROCEDURE.md](EVIDENCE_LOOP_PROCEDURE.md) for the spell and
packet evidence workflow that pairs the decompile exports with fixture SQL,
`/spell inspect4`, `/spell cast4`, diagnostics, and packet comparisons.

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
