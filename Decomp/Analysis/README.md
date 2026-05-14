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

Run a smaller or larger decompiler export:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -MaxDecompiledFunctions 350
```

Re-export from the existing Ghidra project after changing the export script,
without re-running full analysis:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly
```

Re-export or analyze only one binary while iterating on a focused subsystem:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets StsConnLib64.MT.dll -MaxDecompiledFunctions 260
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

Function labels are maintained in `Decomp\Analysis\function_labels.csv` and
applied by `scripts\ApplyNexusForeverLabels.java` before `selected_decompiled.c`
is written.

The Ghidra project is kept under `Decomp\Analysis\ghidra_projects` so the same
analysis can be opened interactively in Ghidra later.

Logs are written under `Decomp\Analysis\logs`.

The client binaries, Ghidra projects, exports, and logs are reproducible local
artifacts and are ignored by Git.

See `INITIAL_FINDINGS.md` for the first pass of protocol/data anchors found in
the generated exports.

See [CONTINUATION_GUIDE.md](CONTINUATION_GUIDE.md) for the repeatable map,
label, implement, and verification workflow to use when continuing the
client-binary decompile.

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
