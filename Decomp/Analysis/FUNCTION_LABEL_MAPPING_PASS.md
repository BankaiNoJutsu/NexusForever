# Function Label Mapping Pass (2026-05-28)

Follow-up to the full-program decompile cache pass. This pass promotes **evidence-backed**
names and comments into `function_labels.csv`, then reapplies them through Ghidra headless
(`ApplyNexusForeverLabels.java`).

## Comment Mechanism (canonical)

| Layer | Behavior |
| --- | --- |
| `function_labels.csv` | Column 4 (`comment`) is the durable source text. |
| `ApplyNexusForeverLabels.java` | Sets each function comment to `NexusForever: {comment}` when applied. |
| Ghidra / re-export | Labels appear in `functions.csv`, `selected_decompiled.c` (when selected), and the project. |

**Style to match:** short factual sentences — field order, opcode names, table paths, or
`selected_reasons_summary.csv` anchor strings. Example (existing):

```csv
WildStar64.exe,14007ab80,ClientRapidTransport_WritePayload,Serialises world opcode 0x0141 payload as a 14-bit node id plus a 32-bit opaque field.
```

Example (ClientDB promotion from `string_xrefs.csv`):

```csv
Houston64.exe,1400a7a00,ClientDB_RegisterAccountItem,Registers client DB table loader for DB\AccountItem.tbl; table path string xref at 140a4bbc0.
```

## Promotion Sources

| Source | Evidence | Confidence |
| --- | --- | --- |
| Existing `function_labels.csv` rows | Prior mapped passes | Mapped |
| `functions.csv` semantic names | Export name + prefix allow-list | Correlated–Mapped |
| `selected_reasons_summary.csv` | Label/target/string selection reasons | Mapped |
| `string_xrefs.csv` `DB\*.tbl` | Table path string inside registering function | Mapped (ClientDB_Register*) |

## Results

| Binary | Rows in `function_labels.csv` | New rows this pass | Ghidra `applied=` (latest export) |
| --- | ---: | ---: | ---: |
| `WildStar64.exe` | 1,636 | 4 | (see latest log) |
| `Houston64.exe` | 731 | 693 | 731 |
| `StsConnLib64.MT.dll` | 40 | 16 | (see latest log) |
| **Total** | **2,407** | **713** | |

Inventory join (`coverage/function_label_inventory.csv`): 54,652 internal functions;
**2,393** rows report `hasDurableLabel=True` after export refresh.

## Commands

```powershell
.\Decomp\Analysis\Promote-FunctionLabelsFromExports.ps1
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -ProjectLayout PerTarget `
  -Targets Houston64.exe,WildStar64.exe,StsConnLib64.MT.dll `
  -DecompileMode Auto -CacheWarmMode Off -GhidraMaxHeap 8G -MaxParallel 3
.\Decomp\Analysis\Export-FunctionLabelInventory.ps1
.\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
```

## Blockers (remaining `FUN_*` backlog)

| Binary | Internal functions | Labeled in CSV | Unlabeled |
| --- | ---: | ---: | ---: |
| `WildStar64.exe` | 25,001 | ~1,631 | ~23,370 |
| `Houston64.exe` | 25,129 | ~731 | ~24,398 |
| `StsConnLib64.MT.dll` | 4,522 | ~45 | ~4,477 |

Unlabeled functions still have **canonical decompile cache** fragments. Naming them requires
additional anchors (packet writers, Lua callbacks, spell families, sniffs) — not bulk `FUN_`
renames.

## Evidence ladder state

**Mapped only** — labels and comments record export-backed registration and selection
evidence. No NexusForever gameplay implementation from this pass.

## Suggested commit message

```
Document and promote evidence-backed function labels after full decompile cache.

Add Promote-FunctionLabelsFromExports.ps1 (ClientDB table xrefs, selection reasons),
expand function_labels.csv by 713 rows (mostly Houston ClientDB), refresh coverage
inventory and mapping pass notes; re-apply labels via Ghidra export.
```
