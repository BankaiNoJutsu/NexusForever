# Durable Decompile Mapping Artifacts

This directory is the source-controlled bridge between local Ghidra evidence and
NexusForever source. It complements the generated exports under
`Decomp/Analysis/exports`, the coverage snapshots under `Decomp/Analysis/coverage`,
and durable labels in `Decomp/Analysis/function_labels.csv`.

The files here are ledgers, not generated decompiler dumps. Add rows only when a
mapping pass has enough evidence to be useful to the next session. Reference
ignored exports, logs, MCP observations, or live captures by path when needed,
but do not copy generated C, local Ghidra projects, client binaries, or raw logs
into this directory.

## Artifact Layers

| File | Layer | Purpose |
| --- | --- | --- |
| `native_opcode_provenance.csv` | Native opcode provenance matrix | Connects an opcode to native registration, payload reader/writer, send/apply owner, managed model, handler, confidence, and blockers. |
| `function_evidence_inventory.csv` | Function evidence inventory | Explains why a durable function label is trusted, including anchors, call relationships, related opcodes/tables, source paths, and verification state. |
| `structure_offset_maps.csv` | Structure and offset maps | Records wire, memory, XML, or table offsets with size, access mode, inferred field names, evidence source, and blockers. |
| `dispatcher_registration_exports.csv` | Dispatcher and registration-table exports | Curates registration rows, dispatcher families, callback tables, vtables, and helper-script outputs without committing generated exports. |
| `source_traceability_map.csv` | Source traceability map | Links native evidence ids to NexusForever models, handlers, services, tests, findings, and residual risk. |
| `live_evidence_bundle_manifest.csv` | Live evidence bundle manifests | Plans and records local client/server evidence passes, including capture roots, commands, logs, packet captures, and current blocker state. |
| `packet_fixture_plan.csv` | Packet fixture plan | Tracks packet layout evidence that should become encode/decode fixtures or tests. |

## Workflow

1. Start with a narrow target question from `CONTINUATION_GUIDE.md`.
2. Search current labels, coverage, exports, and source before adding rows:

   ```powershell
   rg -n "<opcode|label|field|table>" Decomp\Analysis Source
   ```

3. Use the generated artifacts as inputs:

   ```powershell
   .\Decomp\Analysis\Export-FunctionLabelInventory.ps1
   .\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
   ```

   Backfill missing function evidence rows from durable labels when closing the
   label-inventory layer:

   ```powershell
   .\Decomp\Analysis\Update-FunctionEvidenceInventoryFromLabels.ps1
   ```

   Backfill missing opcode provenance rows from the coverage inventory when
   closing the opcode-provenance layer:

   ```powershell
   .\Decomp\Analysis\Update-NativeOpcodeProvenanceFromCoverage.ps1
   ```

   Backfill source traceability and blocked live-evidence manifests from opcode
   provenance:

   ```powershell
   .\Decomp\Analysis\Update-SourceTraceabilityFromOpcodeProvenance.ps1
   .\Decomp\Analysis\Update-LiveEvidenceManifestFromBlockedProvenance.ps1
   ```

4. Fill the smallest ledger rows needed for the pass. Keep row text factual:
   field order, bit widths, function labels, source paths, confidence, and open
   blockers.
5. If a row describes a new durable function name, update
   `function_labels.csv`, re-export the affected binary, and verify the label in
   `functions.csv` before treating the row as Mapped.
6. Keep at least one evidence-backed example row in every ledger so schema
   intent stays visible in review.
7. Run the artifact validator:

   ```powershell
   .\Decomp\Analysis\Validate-DecompileMappingArtifacts.ps1
   ```

8. Run the gap audit when working toward full coverage:

   ```powershell
   .\Decomp\Analysis\Get-DecompileMappingArtifactGaps.ps1
   ```

   Add `-WriteReports` only when you intentionally want local CSV gap reports
   under `mapping_artifacts/gap_reports/` for review. Add `-FailOnGap` when
   using the audit as a completion gate.

## Evidence Status

Use the same evidence-ladder states as `CONTINUATION_GUIDE.md`:

| Status | Meaning in these files |
| --- | --- |
| `Observed` | One native string, constant, import, branch, call shape, log, or capture exists. |
| `Correlated` | Native evidence lines up with a packet, table, source path, handler, sniff, or runtime observation. |
| `Mapped` | Inputs, outputs, field order, constants, and call direction are explainable enough for durable labels or docs. |
| `Verified` | Runtime behavior, packet shape, table data, sniff evidence, or tests confirm the mapping. |
| `Implemented` | NexusForever behavior or diagnostics were changed and focused verification passed. |
| `Blocked` | The evidence trail is useful but still insufficient for safe server mutation or fixture claims. |

## Review Rules

- Keep generated/local artifacts out of source control.
- Keep unknown payload fields diagnostic-only and name the blocker.
- Prefer one row per proven mapping unit over broad subsystem summaries.
- Reference `INITIAL_FINDINGS.md` or a focused tracker whenever a row needs
  explanation beyond the CSV fields.
- Update `source_traceability_map.csv` when native evidence is implemented,
  intentionally diagnostic-only, or rejected at the source boundary.
- Add packet fixtures only after the payload layout is at least Mapped and the
  fixture bytes can be regenerated or explained.
