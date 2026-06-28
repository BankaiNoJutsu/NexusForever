# Client Binary Decompile Continuation Guide

This guide is the working playbook for continuing the NexusForever client binary
reverse-engineering effort. It is meant for future human or AI-assisted passes
that need to map native client behavior, label functions, and turn proven
findings into clean NexusForever server changes.

The goal is not to recreate or copy client source. The goal is to extract
behavioral evidence from locally owned WildStar 16042 client binaries, correlate
that evidence with NexusForever tables, packets, and runtime systems, and
implement server behavior from the evidence in the existing C# codebase.

## Current Artifacts

| Artifact | Purpose |
| --- | --- |
| `Decomp/Client64` | Local client binaries. Ignored by Git. |
| `Decomp/Analysis/setup_decomp_tools.ps1` | Installs the pinned Java and Ghidra toolchain. |
| `Decomp/Analysis/run_ghidra_analysis.ps1` | Runs or re-exports headless Ghidra analysis. |
| `Decomp/Analysis/Get-GhidraMcpWorkflowHints.ps1` | Prints MCP-ready project, program, and evidence-file hints from the latest summaries, manifests, and `.gpr` files. |
| `Decomp/Analysis/GHIDRA_MCP_PROMPT_ADAPTER.md` | Maps upstream `bethington/ghidra-mcp` prompt workflows onto the Nexus evidence ladder and durable label workflow. |
| `Decomp/Analysis/DATA_TYPE_INVESTIGATION_WORKFLOW.md` | Local checklist for evidence-backed parameter typing, structure discovery, offset maps, and family-wide type application. |
| `Decomp/Analysis/scripts/ApplyNexusForeverLabels.java` | Applies source-controlled function labels before export. |
| `Decomp/Analysis/scripts/ExportNexusForeverAnalysis.java` | Writes repeatable CSV and decompiler exports. |
| `Decomp/Analysis/scripts/DumpNearbyData.java` | Dumps nearby data-table slots and resolves pointer targets. |
| `Decomp/Analysis/scripts/DumpAsciiAtAddress.java` | Dumps raw bytes and printable ASCII for short undefined strings. |
| `Decomp/Analysis/scripts/InspectCodeAddress.java` | Decompiles a function or dumps raw instructions at a code address. |
| `Decomp/Analysis/scripts/TraceFunctionCallers.java` | Lists direct references to a target function and prints each caller instruction window. |
| `Decomp/Analysis/function_labels.csv` | Durable function label map. This is the main bridge from native addresses to named evidence. |
| `Decomp/Analysis/INITIAL_FINDINGS.md` | Living summary of mapped behavior and follow-up implementation. |
| `Decomp/Analysis/mapping_artifacts/README.md` | Seven-layer durable mapping workflow for opcode provenance, function evidence, structure offsets, dispatcher registrations, source traceability, live evidence bundles, and packet fixture planning. |
| `Decomp/Analysis/Validate-DecompileMappingArtifacts.ps1` | Validates mapping artifact headers, evidence-ladder states, unique row ids, and review dates. |
| `Decomp/Analysis/Get-DecompileMappingArtifactGaps.ps1` | Audits missing coverage between opcode inventory, function labels, and the seven mapping ledgers. |
| `Decomp/Analysis/STOREFRONT_CATALOG_UNAVAILABLE.md` | Resolved local storefront *Catalogue Unavailable* chain (realm data center id, catalog timing, `0x0989`). |
| `Decomp/Analysis/Get-DecompCoverageSnapshot.ps1` | Generates the current export and opcode coverage inventories from local artifacts and source. |
| `Decomp/Analysis/exports/<binary>/selected_reasons_summary.csv` | Selection audit for the focused export, including why a function was selected and whether it is inside the current decompile cutoff. |
| `Decomp/Analysis/exports/<binary>/selected_call_edges.csv` | Direct caller/callee edges for each selected function, including call/jump site addresses and whether the neighbor is also selected or inside the decompile cutoff. |
| `Decomp/Analysis/exports/<binary>/function_pointer_families.csv` | Contiguous exact-function pointer families from non-executable memory, useful for vtable, callback-table, and dispatcher-family recovery. |
| `Decomp/Analysis/exports/<binary>/function_pointer_family_slots.csv` | Per-slot function metadata for each pointer family, including namespace, thunk state, and slot reference counts. |
| `Decomp/Analysis/exports/<binary>` | Generated analysis exports. Ignored by Git. |
| `Decomp/Analysis/logs/LATEST_RUN_SUMMARY.json` | Queryable summary of the last headless run, target set, project layout, log paths, and export outputs. |
| `Decomp/Analysis/logs/LATEST_COVERAGE_SUMMARY.json` | Queryable snapshot of export counts plus opcode/model/handler coverage. |
| `Decomp/Analysis/coverage/LATEST_COVERAGE_SUMMARY.md` | Human-readable coverage snapshot and priority queues. |
| `Decomp/Analysis/coverage/export_coverage_inventory.csv` | Per-target export coverage counts for the current local snapshot. |
| `Decomp/Analysis/coverage/opcode_coverage_inventory.csv` | Opcode/model/handler coverage inventory for the current local snapshot. |
| `Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md` | Live-client evidence passes for remaining partial features (F-008..F-021, F-031) with implement/diagnostic/blocked boundaries. |
| `CURRENT_STATUS.md` (repo root) | Single source of truth for all 36 feature-area completion states, consolidated emit gaps, and code-audit notes vs false "ghost gap" claims. |
| `Decomp/Analysis/MATCHING_IMPLEMENTATION_STATUS.md` | Matching-only implemented vs mapped-only vs rejected surfaces. |
| `Decomp/Analysis/Start-BlockerEvidenceHarness.ps1` | Starts Trace logging + client console and prints log-tail commands for evidence bundles. |

The default analysis targets are:

- `WildStar64.exe`
- `Houston64.exe`
- `StsConnLib64.MT.dll`

## Working Principles

1. Keep client binaries, Ghidra projects, logs, and decompiled exports out of
   source control.
2. Keep durable knowledge in source-controlled artifacts:
   `function_labels.csv`, `INITIAL_FINDINGS.md`, `mapping_artifacts/*.csv`,
   focused tracker documents, and clean C# implementation changes.
3. Prefer behavioral summaries over copied decompiler output. Use addresses,
   function names, export file names, and short line references to make evidence
   findable.
4. Label only when the name is supported by evidence. Use conservative names
   such as `Parse...`, `Send...`, `Register...`, `Resolve...`, or
   `Maybe...` when confidence is not complete.
5. Implement only the behavior that is stable across evidence sources. Keep
   uncertain payload fields diagnostic-only.
6. Each pass should leave a trail: what was mapped, what was labelled, what was
   implemented, what stayed blocked, and how it was verified.
7. Use `ghidra-mcp` as a focused interactive probe, not as the durable record.
   Mirror stable labels into `function_labels.csv` and re-export before relying
   on them in future passes.

## Evidence Ladder

Use the same confidence language in findings, labels, commit messages, and
implementation comments.

| Status | Meaning | Allowed action |
| --- | --- | --- |
| Observed | A string, import, constant, branch, or call shape appears in one client function. | Record in notes. Do not implement. |
| Correlated | The observation lines up with a table, packet, enum, handler, sniff, or existing server path. | Add a tentative finding and possibly a conservative label. |
| Mapped | Inputs, outputs, field names, constants, and call direction are understood well enough to explain the function. | Add a durable label and update `INITIAL_FINDINGS.md`. |
| Verified | Local runtime behavior, sniff evidence, table data, or packet shape confirms the mapped meaning. | Implement server behavior. |
| Implemented | NexusForever code was changed and build or focused runtime checks passed. | Record the implementation and verification result. |
| Blocked | The evidence shows a feature but not enough safe semantics to mutate server state. | Keep diagnostic-only and list the blocker. |

## Repeatable Flow

### 1. Choose A Narrow Target

Start each pass with one behavior family, not a whole binary. Good target shapes:

- One STS transaction, such as `/Auth/LoginFinish` or `/Auth/ConsumeGameToken`.
- One packet framing question, such as header size, opcode width, size limits,
  compression, or encryption boundary.
- One Client DB table registration path, such as `WorldSocket.tbl` or
  `RealmDataCenter.tbl`.
- One enum or Lua registration family, such as public-event objective constants.
- One spell or gameplay family, such as `Proc`, `RavelSignal`, target masks, or
  shield-related effects.

Write the target as a one-sentence question:

```text
What fields does the client send and parse for /Auth/<Transaction>, and what
does NexusForever need to accept or emit for build 16042 compatibility?
```

```text
What constants does the client expose for <enum/system>, and do the current
NexusForever enum values match the client-facing values?
```

```text
Which Spell4Effects payload fields are consumed by the client for <family>, and
what server behavior can be implemented without guessing?
```

### 2. Refresh Or Scope The Export

Use the existing Ghidra project whenever possible:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets StsConnLib64.MT.dll -MaxDecompiledFunctions 350
```

For a focused world-client pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 500
```

For a fresh default analysis:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1
```

For callback-table, vtable, or raw-stub work, start with the standard export so
`function_pointer_families.csv` and `function_pointer_family_slots.csv` capture
the reusable structure, then attach a focused helper script when you need local
data bytes or instruction windows:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 340 -ExtraPostScript DumpNearbyData.java -ExtraPostScriptArgs @('140b73540','20')
```

If you omit `-MaxDecompiledFunctions` on a helper-script export-only pass, the
runner preserves the current manifest cutoff for that target. This keeps
`selected_reasons_summary.csv`, the manifest, and the coverage snapshot aligned
with the existing `selected_decompiled.c` breadth. Pass
`-MaxDecompiledFunctions` explicitly when you intentionally want to narrow or
widen the focused export.

Re-export commands now default to manifest-based reuse for `selected_decompiled.c`.
If the binary fingerprint, label state, `-MaxDecompiledFunctions`, exporter
version, and ordered top-N selected functions are unchanged, the exporter keeps
the existing decompiler output and refreshes only the cheaper CSV and text
artifacts.

The exporter also keeps a canonical per-function cache under
`selected_decompiled_cache/functions/<binary-fingerprint>`. If a function body
has already been exported for the same binary/function identity, later runs reuse
that canonical fragment instead of writing another physical copy. The exporter
removes old `selected_decompiled_cache/<context-hash>` folders; only the
`functions` folder is kept, used, and populated.

By default, normal runs incrementally warm the full-program cache up to the
`-MaxWarmFunctionsPerRun` budget. Use `-CacheWarmMode Off` for a cheap xref-only
pass, `-CacheWarmMode Incremental` for bounded background warming, and
`-CacheWarmMode Complete` only when you intentionally want to fill every missing
function in one pass.

`-AnalysisMode Auto` is the default and skips Ghidra import/analysis when the
project and analysis manifest match the current binary. `-ExportOnly` remains a
compatibility alias for `-AnalysisMode Skip`; use `-AnalysisMode Force` when you
need to rebuild the Ghidra project state.

Single-target runs now default to per-target Ghidra projects so different
binaries can be processed in parallel without fighting over the old shared
project lock. Multi-target and full-pass runs still default to the shared
project. If you only have the legacy shared project on disk, export-only Auto
mode falls back to it until the per-target project is seeded by a non-export
run.

Updated runners take a per-project gate before opening Ghidra. Concurrent
sessions using the scripts wait on that gate, and if an interactive Ghidra
session or older runner already has the project open, the script retries the
headless open instead of failing on the first `Unable to lock project` message.
Use `-ProjectLockTimeoutMinutes <minutes>` when a scheduled job should stop
waiting after a bounded period; the default is to wait until the project becomes
available.

For multi-binary refreshes, use runner-level parallelism with per-target
projects instead of one shared multi-target run:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ProjectLayout PerTarget -MaxParallel 3 -MaxDecompiledFunctions 2400
```

The runner starts one worker per target with collision-free `-RunId`,
`-SummaryPath`, and `-SkipCoverage` settings, then writes a merged summary and
refreshes the latest coverage once after all workers complete. Use
`-AnalysisMode Force -ProjectLayout PerTarget` when a target needs its split
project seeded or refreshed:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -AnalysisMode Force -ProjectLayout PerTarget -MaxParallel 3
```

`Start-DecompileBatch.ps1` remains available for older command lines, but it now
forwards cache-warm and analysis-mode parameters to the main runner.

Use `-ProjectLayout Shared` when you intentionally want the old single-project
behavior for a targeted pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -ProjectLayout Shared
```

Use `-DecompileMode Force` after manual interactive Ghidra edits that are not
represented in `function_labels.csv`:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -DecompileMode Force
```

Use `-DecompileMode Skip` when the pass only needs strings, imports, and xrefs:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -DecompileMode Skip
```

Increase `-MaxDecompiledFunctions` only when the selected functions are too
shallow. If the desired function is not selected, prefer improving
`HIGH_VALUE_STRING_PATTERNS`, adding a durable label, or locating a better xref
over exporting thousands of functions.

### Optional Ghidra MCP Probe

Use `ghidra-mcp` when a static export points at the right function family but
the next question is easier in an open CodeBrowser project: datatype shape,
nearby symbols, current decompiler view, cross-reference navigation, comments,
or live debugger tracing.

Before connecting, print the project handoff from the repo root:

```powershell
.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe
```

The helper reads the latest run summary, analysis manifests, and `.gpr` files,
then prints the matching project name, program name, export folder, and MCP
sequence. Open the listed `.gpr` in Ghidra and the listed program in
CodeBrowser. In Codex, call `mcp__ghidra_mcp.list_instances`, connect to the
printed project query with `mcp__ghidra_mcp.connect_instance`, then call
`mcp__ghidra_mcp.list_tool_groups` and load the needed groups if the bridge is
lazy-loaded.

Keep the handoff boundary clean:

- Use MCP for focused inspection, tentative renames/comments, datatype review,
  and debugger traces.
- Follow `GHIDRA_MCP_PROMPT_ADAPTER.md` when applying upstream prompt workflows
  such as Function Documentation V5 or Data Type Investigation.
- Do not treat a Ghidra-only rename as durable evidence.
- Add stable names to `function_labels.csv` only after the function is mapped.
- Re-export the affected binary and confirm the names appear in `functions.csv`
  and `selected_decompiled.c`.
- Use `-DecompileMode Force` only when you intentionally need fresh output for
  interactive Ghidra edits that are not represented in `function_labels.csv`.

## Selection Criteria Reference

Functions enter the focused export in priority order:

1. `label:` reasons from `Decomp/Analysis/function_labels.csv`
2. `target:` reasons from `HIGH_VALUE_STRING_PATTERNS` in `ExportNexusForeverAnalysis.java`
3. `import:` reasons from interesting import xrefs
4. `string:` reasons from the broader keyword list

Use `selected_reasons_summary.csv` in the matching export folder to see the
exact reason set for each function, the priority bucket that won, the first xref
that pulled it in, and whether it made the current `selected_decompiled.c`
cutoff.

Use this when a focused pass misses a target function:

- Check whether a nearby anchor already selected the function through `selected_reasons_summary.csv`.
- Add a durable label before widening keyword lists when the function meaning is already mapped.
- Add a new high-value pattern only when the anchor is stable and specific enough to avoid export noise.
- Raise `-MaxDecompiledFunctions` only after labels and durable string anchors still leave the selected set too shallow.

## Export Caching And Invalidation

`ExportNexusForeverAnalysis.java` keeps `selected_decompiled.c` fast by caching
per-function fragments in a canonical full-program store under
`selected_decompiled_cache/functions/<binary-fingerprint>/`. Old context-hash
cache folders are removed by the exporter; once a function exists in the
canonical store, later runs skip re-exporting it.

`-DecompileMode Auto` is the normal choice for repeated passes:

- Reuses the existing `selected_decompiled.c` when the binary fingerprint, label state, Ghidra version, and selected function list are unchanged.
- Reuses canonical cached fragments for unchanged functions when the selected set expands or contracts.
- Writes fresh CSV and text artifacts even when decompilation is skipped.

Use `-DecompileMode Force` after interactive Ghidra edits or when you need to
throw away cached decompile output. Use `-DecompileMode Skip` when the pass only
needs strings, xrefs, imports, selection auditing, or helper-script output.

The easiest ways to understand cache behavior are:

- `selected_decompiled.manifest` in the export folder for decompile fingerprint details.
- `decompile_cache_summary.properties` for full-program cached, warmed, remaining, duplicate, and already-exported counts.
- `selected_reasons_summary.csv` for the current selection and cutoff.
- `logs/LATEST_RUN_SUMMARY.json` for the most recent headless run inputs and outputs.
- `coverage/LATEST_COVERAGE_SUMMARY.md` and `coverage/opcode_coverage_inventory.csv` for the current export and opcode backlog.

### 3. Search Exports For Anchors

Use `rg` first. Search the generated exports and source together so each native
anchor is immediately compared with server code.

```powershell
rg -n "LoginFinish|AuthType|UserId|Aliases|RoleId" Decomp\Analysis\exports Source
```

```powershell
rg -n "WorldSocket|RealmDataCenter|\.tbl|Register" Decomp\Analysis\exports Source Tools
```

```powershell
rg -n "PublicEventObjectiveType|DefendObjectiveUnits|Lua_Register" Decomp\Analysis\exports Source
```

When an anchor appears in `interesting_strings.csv`, open the matching rows in:

1. `interesting_strings.csv` for the literal string and address.
2. `string_xrefs.csv` for exact xref addresses, especially data-table refs.
3. `selected_reasons_summary.csv` for why the function stayed in the focused export and whether it is inside the current decompile cutoff.
4. `selected_call_edges.csv` for direct caller/callee follow-up when the anchor lands on a serializer, wrapper, or shared helper and you need the next step in the local call chain without reopening Ghidra.
5. `selected_xrefs.csv` for the referencing function entry when the xref is code.
6. `selected_decompiled.c` for the selected function body.
7. `functions.csv` for nearby named or unlabeled functions.
8. Source files under `Source/` for the current server behavior.

For vtable or callback-table work, search `function_pointer_families.csv` and
`function_pointer_family_slots.csv` before reopening Ghidra. Use the family
address, first named slots, thunk counts, and per-slot reference counts to pick
the right helper-script follow-up.

### 4. Map The Function

For each candidate function, create a small function map before changing code.

```markdown
### Candidate: <binary> <address>

- Proposed name:
- Current Ghidra name:
- Selected export location:
- Anchors:
- Inputs:
- Outputs:
- Constants:
- Calls out to:
- Called by:
- Matching NexusForever files:
- Confidence:
- Open questions:
```

Good mapping cues:

- Transaction or route strings, such as `/Auth/ConsumeGameToken`.
- XML or field strings, such as `GameCode`, `Token`, `ClientNetAddress`.
- Table paths, such as `DB\WorldSocket.tbl`.
- Import references, such as `send`, `recv`, `WSASend`, `select`, `connect`.
- Enum registration names and adjacent integer constants.
- Repeated child parsing loops, which often identify arrays like
  `Aliases/Alias` or `Roles/RoleId`.
- Packet writer order, especially size, opcode, payload, and bit counts.
- Shared helper calls from multiple higher-level functions.

Avoid over-naming helper functions too early. A name like
`StsConn_ParseRoles` is good once repeated `RoleId` nodes are proven. A name
like `CryptoHandshake_FinaliseRetailSecureLogin` is too strong unless every
part of that claim is mapped.

### 5. Update Mapping Artifacts

When the pass produces reusable evidence, update the relevant CSV under
`Decomp/Analysis/mapping_artifacts` before or alongside findings updates:

| Layer | File |
| --- | --- |
| Native opcode provenance | `native_opcode_provenance.csv` |
| Function evidence | `function_evidence_inventory.csv` |
| Structure and offset maps | `structure_offset_maps.csv` |
| Dispatcher or registration rows | `dispatcher_registration_exports.csv` |
| Native-to-source traceability | `source_traceability_map.csv` |
| Live-client capture plan or result | `live_evidence_bundle_manifest.csv` |
| Packet fixture backlog | `packet_fixture_plan.csv` |

Use these ledgers for compact facts: function labels, addresses, opcodes, field
order, bit widths, source paths, confidence state, and blockers. Keep longer
explanations in `INITIAL_FINDINGS.md` or a focused tracker, then point the CSV
row at that finding.

Validate the ledgers after edits:

```powershell
.\Decomp\Analysis\Validate-DecompileMappingArtifacts.ps1
```

When working toward full seven-layer coverage, run the gap audit:

```powershell
.\Decomp\Analysis\Get-DecompileMappingArtifactGaps.ps1
```

Use `-FailOnGap` only when checking whether the full seven-layer map is complete.
Use `Update-FunctionEvidenceInventoryFromLabels.ps1` to backfill mapped
function-evidence rows from durable `function_labels.csv` entries before doing
manual source-correlation passes.
Use `Update-NativeOpcodeProvenanceFromCoverage.ps1` to add conservative
coverage-derived opcode provenance rows, then refine individual rows with exact
native registration and payload functions during focused passes.
Use `Update-SourceTraceabilityFromOpcodeProvenance.ps1` and
`Update-LiveEvidenceManifestFromBlockedProvenance.ps1` to keep source and
live-evidence ledgers aligned after opcode provenance backfills.

### 6. Add Durable Labels

Add labels to `Decomp/Analysis/function_labels.csv` after the function is mapped
well enough to be useful in future exports.

CSV format:

```csv
program,address,name,comment
WildStar64.exe,140000000,Subsystem_ActionObject,Short evidence-backed description.
```

Naming convention:

| Prefix | Use |
| --- | --- |
| `StsConn_` | STS transaction, XML, auth, token, or account parsing behavior. |
| `StsInetSocket_` | STS socket envelope, connect, dispatch, and socket crypto edge behavior. |
| `Network_` | Shared world network message, packet, endpoint, or diagnostic helpers. |
| `NetworkSocket_` | Select/non-blocking socket path. |
| `NetworkIOCP_` | Overlapped or IOCP-style socket path. |
| `ClientDB_` | Client database table registration and loader functions. |
| `Lua_` | Lua binding or enum registration functions. |
| `<System>_Parse...` | Data parser or response parser. |
| `<System>_Send...` | Request or packet builder. |
| `<System>_Register...` | Registration table, enum, handler, or script binding. |

After editing labels, re-export to confirm the label applies:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets <binary> -MaxDecompiledFunctions 350
```

Then check:

```powershell
rg -n "<NewFunctionName>" Decomp\Analysis\exports\<binary>\functions.csv Decomp\Analysis\exports\<binary>\selected_decompiled.c
```

If the script reports "No function at address", open the function in Ghidra or
check `functions.csv`; the address may be inside a thunk or a containing
function. Use the real function entry when possible.

### 7. Update Export Selection When Needed

If a target string is repeatedly useful but not exported as interesting, add it
to `HIGH_VALUE_STRING_PATTERNS` in
`scripts/ExportNexusForeverAnalysis.java`.

Good high-value patterns are stable field, route, enum, table, or subsystem
names:

```java
"consumegametoken", "clientnetaddress", "serverpublickey"
```

Avoid broad patterns that explode selection noise:

```java
"id", "data", "value", "manager"
```

Re-export and confirm the pattern shows up in `interesting_strings.csv` and
`string_xrefs.csv`, and selects the expected code xrefs when the reference is
inside a function body.

### 7. Implement Conservatively

Implementation should happen only after the function map reaches at least
`Mapped`, and preferably `Verified`.

Before editing code, identify:

- The current NexusForever source files that own the behavior.
- Whether the server already has a partial implementation.
- Whether the client behavior is request parsing, response writing, enum value,
  state mutation, packet framing, or diagnostics.
- The minimal server change needed to match the proven behavior.
- The focused verification command or manual runtime check.

Common implementation surfaces:

| Native evidence | NexusForever surface |
| --- | --- |
| STS request fields | `Source/NexusForever.Network.Sts/Model/*Message.cs` |
| STS response fields | `Source/NexusForever.Network.Sts/Model/*Response.cs` |
| STS transaction behavior | `Source/NexusForever.StsServer/Network/Message/Handler/*.cs` |
| World packet framing | `Source/NexusForever.Network/Packet` and `Source/NexusForever.Network/Session` |
| World opcodes and messages | `Source/NexusForever.Network.World/Message` |
| Public-event constants | `Source/NexusForever.Game.Static/PublicEvent` |
| Client DB table meaning | `Source/NexusForever.GameTable`, `Tools/DataMapping`, and matching runtime managers |
| Spell effect payloads | `Source/NexusForever.Game/Spell` and `Source/NexusForever.Game.Abstract` |
| Action set, PvP, interaction, and player runtime state | `Source/NexusForever.Game`, `Source/NexusForever.WorldServer/Network/Message/Handler` |
| Crafting, marketplace, support, friendship, ICComm, trade, options, and other world request clusters | `Source/NexusForever.Network.World/Message/Model`, `Source/NexusForever.WorldServer/Network/Message/Handler`, and matching game services |
| Expedition or content behavior | `Source/NexusForever.Script.Instance` |

Keep uncertain values surfaced through diagnostics or comments in the tracker,
not through server mutations. For example, an unknown `DataBits03` should stay
recorded in diagnostics until it has a known behavior.

### 8. Verify The Change

Use the narrowest verification that proves the implemented behavior.

Typical checks:

```powershell
dotnet build Source\NexusForever.sln --no-restore
```

Focused source search:

```powershell
rg -n "<field-or-function-name>" Source Decomp\Analysis
```

STS behavior:

- Confirm message model read/write order matches the mapped client parser or
  builder.
- Confirm handler state changes do not depend on fields the client does not
  send.
- Confirm failure cases still produce existing errors.

Packet behavior:

- Confirm writer field sizes match the client bit or byte widths.
- Confirm size and opcode fields match the client framing.
- Confirm logging does not truncate or misformat opcodes.

Spell or gameplay behavior:

- Inspect with `/spell inspect4 <spell4Id>`.
- Cast with `/spell cast4 <spell4Id>` in a controlled local session.
- Compare diagnostics, combat logs, packets, entity state, and table row
  interpretation.

### 9. Record The Result

Update `INITIAL_FINDINGS.md` or the focused tracker with:

- New labels added.
- Evidence location in `selected_decompiled.c`.
- Field order, constants, or enum values mapped.
- NexusForever files changed.
- Verification command and result.
- Anything intentionally left unimplemented.

Use this compact format:

```markdown
Follow-up implemented from this pass:

- `<FunctionName>` at `<address>` writes/reads `<field list>` around
  `exports\<binary>\selected_decompiled.c:<line>`.
- NexusForever now `<short implementation summary>`.
- `<Unknown field or endpoint>` remains blocked because `<specific missing evidence>`.
```

## Current Map

This map is a state ledger. It summarizes the known high-value areas from
`INITIAL_FINDINGS.md` so the next pass can start from known terrain and avoid
re-mapping packet surfaces that are already closed.

| Area | Binary | Durable labels or anchors | NexusForever surfaces | State | Next useful work |
| --- | --- | --- | --- | --- | --- |
| STS auth transactions | `StsConnLib64.MT.dll` | `StsConn_SendLoginStart`, `StsConn_SendLoginFinish`, `StsConn_OnLoginFinishResponse`, `StsConn_SendRequestGameToken`, `StsConn_SendConsumeGameToken`, `StsConn_OnConsumeGameTokenResponse` | `NexusForever.Network.Sts/Model`, `NexusForever.StsServer/Network/Message/Handler` | Implemented for login/account/game-token compatibility. | Continue unmapped auth/token/external-account routes only when a local client flow needs them. |
| STS connect envelope | `StsConnLib64.MT.dll` | `StsInetSocket_ParseConnectFields`, `StsInetSocket_SendConnectEnvelope`, `StsInetSocket_DispatchConnectEnvelope` | `ClientConnectMessage`, STS session setup | Implemented for known connect/login-start fields. | Validate remaining optional envelope fields against observed startup before changing session state. |
| Token crypto handshake | `StsConnLib64.MT.dll` | `StsConn_SendTokenKeyData`, `ServerRand`, `ServerPublicKey`, `ServerSignature`, `PremasterSecret`, `AuthnToken` | STS auth models and handlers | Mapped only / blocked. | Keep blocked until the crypto/token-login semantics are understood safely. Do not guess crypto behavior. |
| World framing and bit IO | `WildStar64.exe` | `Network_SendMessageById`, `Network_SendResolvedMessage`, `Network_SerialiseResolvedMessage`, bit reader/writer labels | `NexusForever.Network/Packet`, `NexusForever.Network/Session`, world messages | Implemented where packet size/opcode/bit order differed or needed confirmation. | Continue into receive/framing/dispatch only if a client-server packet mismatch appears. |
| Socket driver behavior | `WildStar64.exe` | `NetworkSocket_*`, `NetworkIOCP_*`, `Network_ParseEndpointAddress` | `NetworkSession`, connection setup | Implemented only for low-risk compatibility such as `TCP_NODELAY`; mostly mapped diagnostics. | Use for compatibility and diagnostics, not large rewrites without a runtime issue. |
| Client DB registration | `WildStar64.exe`, `Houston64.exe` | `ClientDB_RegisterRealmDataCenter`, `ClientDB_RegisterWorldSocket`, `ClientDB_RegisterPublicEventUnitPropertyModifier`, table path strings | `GameTable`, `Tools/DataMapping`, runtime managers | Mapped table-registration pattern. | Label more table registrations when a table is needed for data mapping or runtime behavior. |
| Public-event constants | `WildStar64.exe` | `Lua_RegisterPublicEventConstants`, `PublicEventObjectiveType_*`, status/category/notification strings | `Game.Static/PublicEvent`, public event scripts | Implemented for confirmed enum values and aliases. | Extend only when a public-event objective flag/notification behavior needs server logic. |
| Game.Spell Lua and spell metadata | `WildStar64.exe` | `Lua_RegisterGameSpellBindings`, 46 `Lua_GameSpell_*` labels, `Lua_InsertStringIntPair`, spell enum strings | `Game.Static/Spell`, `Game/Spell`, `/spell inspect`, data mapping outputs | Implemented for metadata/diagnostics; several runtime behaviors remain blocked. | Continue family-by-family: prove a concrete `Spell4Effects` consumer before mutating spell runtime. |
| Action set and LAS requests | `WildStar64.exe` | `Lua_RegisterActionSetLib`, `Lua_ActionSetLib_RequestActionSetChanges`, `ActionSet_ValidateRequestedChanges`, `ActionSet_CheckPvPRestriction`, `ActionSet_CheckUpdateSpellInProgress` | `Game/Spell`, `WorldServer/Network/Message/Handler/Spell` | Implemented for 12-slot LAS size, active spec, tier, AMP, PvP/in-combat/dead preflight, and cache refresh. | `UpdateSpellInProgress` remains blocked until a real asynchronous spell-update transaction model is found or introduced; do not add a synthetic boolean gate. |
| Rapid transport and taxi/flight path | `WildStar64.exe` | `ClientRapidTransport_WritePayload`, `RapidTransport_SendClientRapidTransport`, `Map_BuildRapidTransportNodeInfoTable`, `Map_GetRapidTransport*`, `ClientFlightPathPurchase_WritePayload`, `FlightPath_SendPurchaseRequest` | `WorldServer/Network/Message/Handler/PlayerPath`, `Game/Entity/PathManager`, `GameTable` | Implemented for validated receive boundaries, pricing evidence, and read-only treatment of the second `ClientRapidTransport` field as `ContextToken`; transport movement/payment is partial. | Map service-token bypass selection, taxi embark, route state, and cast-result correlation before using `ContextToken` for validation or charging/teleporting beyond proven behavior. |
| Crafting and tradeskill requests | `WildStar64.exe` | `ClientCrafting*WritePayload`, `Crafting_SendClient*`, `Tradeskill_SendClientTradeskill*`, `ServerCraftingCurrentCraft_ReadPayload`, `Crafting_HandleServerCraftingCurrentCraft`, `Lua_RegisterCraftingBindings` | `Network.World/Message/Model/Crafting`, `WorldServer/Network/Message/Handler/Crafting`, `GameTable` | Implemented for packet layouts, table validation, conservative fixed-recipe success, station validation, profession persistence, and sigil result value names. | Retail parity remains blocked on discovery roll/unlock semantics, exact station service-key meanings, complex craft stats, broader material-source precision, durable rune item data, non-success sigil result rules, and aux emit intent. |
| Rune crafting requests | `WildStar64.exe` | `ClientCraftingRuneSlot*WritePayload`, `RuneCrafting_SendClientRune*`, `CodeEnumTradeskillResult` | `Network.World/Message/Model/Crafting`, `WorldServer/Network/Message/Handler/Crafting` | Implemented as validated request parsing/runtime-only mutation with native sigil result values. | Durable item rune data semantics and non-success result-rule precision remain blocked. |
| Marketplace request state | `WildStar64.exe` | `Marketplace_*WritePayload`, `Marketplace_SendClient*` auction and commodity labels | `WorldServer/Network/Message/Handler/Marketplace`, marketplace message models | Implemented with deterministic empty/failure responses; payloads and validation are mapped. | Real auction/commodity persistence, search state, bid/post/cancel mutation, and mail settlement remain blocked. |
| Storefront catalog and purchase state | `WildStar64.exe` | `Storefront_RequestCatalog_WritePayload`, `Storefront_PurchaseCharacter_WritePayload`, `Storefront_PurchaseAccount_WritePayload`, `NetworkBitWriter_WriteIdentity` | `Game/Storefront`, `Network.World/Message/Model`, `WorldServer/Network/Message/Handler/Account` | Implemented for catalog send, mapped purchase packet parsing, local-target validation, account-currency charging, and account-inventory item creation. | Gifting, cooldowns, exact purchase result UI, and unsupported offer item types remain blocked pending native packet/result mapping. |
| Account inventory and account-item claim | `WildStar64.exe` | `ServerAccountItems_ReadPayload`, `ServerAccountItemCooldownSet_ReadPayload`, `ServerAccountItemsPending_ReadPayload`, `AccountInventoryItem_ReadPayload`, `AccountItem_SendClientAccountItemTake`, `AccountItem_SendClientClaimPendingItemGroup` | `Database.Auth`, `Game/Account/Inventory`, `Network.World/Message/Model`, `WorldServer/Network/Message/Handler/Account` | Implemented for persisted account inventory list/add, claim-state enum, `ClientAccountItemTake`, and supported `AccountItem` grant paths. | Pending account item groups, gift delivery, cooldown mutation, and unmapped account item effects remain blocked. |
| P2P trading request state | `WildStar64.exe` | `Trade_SendClientP2PTrading*`, `ClientP2PTradingUInt64_WritePayload`, shared zero-payload and raw field writers | `WorldServer/Network/Message/Handler/Trade`, `Game/Trade/TradeManager`, `Network.World/Message/Model` | Implemented for transient invite/session/offer/commit/settlement using existing item/currency APIs. | Remaining precision: trade locks against concurrent inventory changes, exact eligibility/money-limit rules, unknown item-update fields, `ServerP2PTradeResult.Cancelled` semantics, and sniff/client UI verification. |
| Duel/PvP request state | `WildStar64.exe` | `Lua_GameLib_InitiateDuel`, `Lua_GameLib_AcceptDuel`, `Lua_GameLib_DeclineDuel`, `Lua_GameLib_ForfeitDuel`, `Lua_GameLib_SetIgnoreDuelRequests`, `Lua_GameLib_TogglePvpFlags`, shared zero-payload and bool writers | `WorldServer/Network/Message/Handler/Pvp`, `Game/Pvp/DuelManager`, `Game/Entity/Player` | Implemented for safe transient duel lifecycle, runtime PvP toggle, and durable pending toggle-off cooldown persistence. | Remaining precision: observer broadcasts, duel-area leash/warnings, countdown duration, forced-map PvP, and duel reward/stat effects. |
| Support/report/survey requests | `WildStar64.exe` | `ClientIncidentReport_WritePayload`, `ClientSupportTicket_WritePayload`, `ClientReportBug_WritePayload`, `ClientStuck_WritePayload`, `ClientSuggest_WritePayload`, `ClientCustomerSurveySubmit_WritePayload` | `WorldServer/Network/Message/Handler/Support`, `WorldServer/Network/Message/Handler/Misc` | Implemented as parsed diagnostics and deterministic ticket result/failure where safe. | Real support-ticket/report/survey persistence or moderation workflows remain blocked until a backing service is designed. |
| Options and combat-log preferences | `WildStar64.exe` | `ClientCombatOptions_WritePayload`, `Options_SendClientOptionsCasting`, `ClientOptions`, combat-log one-uint writers | `WorldServer/Network/Message/Handler/Option`, option message models | Implemented as validated parsing/logging; no durable store yet. | Add a small account/character option store before using these preferences to filter outbound combat logs. |
| Friendship request state | `WildStar64.exe` | `Lua_FriendshipLib_*`, `ClientFriendship*WritePayload`, `NetworkBitWriter_WriteWideString`, `NetworkBitWriter_Write4BitValue` | Friendship server handlers, `WorldServer/Network/Message/Handler/Friendship` | Implemented for account block, remove-by-name, note-by-name, and transient ignore-strangers; auto-response diagnostic-only. | Persist ignore-strangers if needed; auto-response needs account/chat storage and delivery semantics. |
| ICComm request state | `WildStar64.exe` | `ClientICCommChannelJoin_WritePayload`, `ClientICCommMessage_WritePayload`, `ClientP2PTradingUInt64_WritePayload` | `WorldServer/Network/Message/Handler/ICComm`, `Game/ICComm/ICCommManager`, `Network.World/Message/Model/ICComm` | Implemented for transient global/group/guild channel membership, join results, message result echo, directed delivery, ordered sender echo, and not-joined cleanup. | Remaining precision: entitlement checks, exact directed-vs-ordered delivery semantics, persistent membership, explicit leave/logout UI behavior, throttling, and guild/group lifecycle hooks. |
| Interaction, CSI, busy, and target feedback | `WildStar64.exe` | `CSIKey_HandlePress`, `CSIAction_*`, `Interaction_AttemptTargetAction`, `Interaction_HandlePostActionTargetFeedback`, `DeferredActionQueue_*`, `Loot_CheckInteractionRange` | `WorldServer` entity interaction handlers, `Game/Entity/UnitEntity`, scripts | Implemented for busy target gating and visible busy state; broader client interaction flow is mapped in labels. | Deeper deferred-action/current-object/current-target semantics need sniffs or stronger table/callback evidence before server mutation. |
| Loot interaction and bindcheck | `WildStar64.exe` | `Loot_PrepareAndDispatchBindcheck`, `Loot_SendClientLootItemCollect`, `Loot_HandlePendingLootInteract`, `Loot_DispatchBindcheckEvent` | Loot handlers, item/loot runtime | Mapped/partially implemented at request and diagnostic boundaries. | Export-only revalidation and real loot assignment/roll/master-loot state remain open. |
| Vital modifier and SapVital | `WildStar64.exe` | `CombatLog_DispatchVitalModifierEvent`, `Lua_InsertStringIntPair`, `SapVital`, `VitalModifier`, spell-effect enum block | `Game/Spell`, `Game/Entity/UnitEntity`, combat logs | Implemented for conservative health vital source/damage-type handling; enum-registration evidence improved. | Direct gameplay consumer for `SapVital` and unsupported alias vitals remain blocked. |
| Item visual, disguise, scale, shield/vital spell effects | `WildStar64.exe` | Spell-effect name block and targeted runtime helper labels | `Game/Spell`, `Game/Entity/UnitEntity`, diagnostics | Implemented incrementally where effect ownership/restoration semantics were proven. | Continue with one effect family at a time; keep unknown `DataBits` diagnostic-only. |
| Audio runtime and selector mapping | `WildStar64.exe` | `AudioOutput_*`, `AudioSelector_*`, `AudioRuntime*`, `AudioResampler_*` | Findings/labels only | Mapped only. | Use as helper/context evidence for client callback families; do not implement server audio behavior. |

### Current State Precision

Use the current map as a state ledger, not just a list of decompile anchors:

- **Implemented** means NexusForever now has code that handles the mapped client
  surface, even when the safe behavior is an empty result or deterministic
  failure because a backing service does not exist.
- **Mapped only / blocked** means the packet or function shape is known, but the
  server-side state boundary is still missing. Do not add mutation just because
  the request fields are known.
- For request clusters, record three separate layers: packet shape, local client
  sender guard/state, and server response/state mutation. Most remaining gaps
  are in the third layer.
- When `INITIAL_FINDINGS.md` says a handler returns an empty result or
  deterministic failure, treat that as an implemented compatibility boundary,
  not as full feature support.

### Good Next Targets

Prefer one of these shapes when choosing a new pass:

- A blocked state machine or backing service with fully mapped packets, such as
  marketplace persistence, crafting success, or support-ticket storage. P2P
  trade and ICComm now have transient runtime implementations; return there
  only for precision gaps such as offer locks, entitlement checks, message
  delivery packet choice, item eligibility, money limits, or result/update-field
  verification.
- A narrow runtime precision gap inside an implemented cluster, such as duel
  observer broadcasts, PvP cooldowns, rapid-transport service-token/route
  selection, or a real server-side spell-update transaction model for
  `UpdateSpellInProgress`.
- One spell-effect family with table rows, diagnostics, and at least one client
  consumer anchor. Avoid broad `Spell4Effects` sweeps.
- A client DB registration needed by a concrete source or data-mapping task.

## Prompt Guide

These prompts are designed for future AI-assisted passes. Paste only the prompt
that matches the current stage. Keep the target narrow and include the current
state from this guide.

### Session Primer

Use this when starting a new decompile continuation session:

```text
You are working in NexusForever. Continue the client binary reverse-engineering
effort using Decomp/Analysis/CONTINUATION_GUIDE.md as the workflow.

Read:
- Decomp/Analysis/README.md
- Decomp/Analysis/CONTINUATION_GUIDE.md
- Decomp/Analysis/INITIAL_FINDINGS.md
- Decomp/Analysis/function_labels.csv
- Decomp/Analysis/GHIDRA_MCP_PROMPT_ADAPTER.md when using ghidra-mcp
- Decomp/Analysis/DATA_TYPE_INVESTIGATION_WORKFLOW.md when structure or
  parameter types are in scope

For an interactive Ghidra pass, first run:

.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets <binary>

Then open the printed Ghidra project, use ghidra-mcp list_instances,
connect_instance, and list_tool_groups, and keep durable labels in
function_labels.csv.

Do not touch Decomp/Client64, generated exports, logs, or Ghidra project files
except by running the existing scripts. Do not copy decompiled client source into
the repository. Summarize behavior with addresses, labels, field names, and short
line references.

Target for this pass:
<one narrow target question>

Deliver:
- mapped evidence
- labels to add, if justified
- implementation changes only for verified behavior
- updated findings/tracker notes
- verification result
```

### Discovery Prompt

Use this before labels or implementation:

```text
Investigate <target subsystem or field> in the existing Ghidra exports.

Use rg over Decomp/Analysis/exports, Decomp/Analysis/function_labels.csv, and
Source. Identify the smallest set of candidate functions. For each candidate,
produce:
- binary and address
- current function name
- selected_decompiled.c location
- strings/imports/constants that anchor it
- likely inputs and outputs
- matching NexusForever source files
- confidence level using Observed/Correlated/Mapped/Verified/Blocked

Do not edit code yet. End with the recommended next labels and the minimum
evidence still needed before implementation.
```

### Mapping Prompt

Use this once candidate functions are known:

```text
Map <function address or label> in <binary> into a durable NexusForever finding.

Read the matching selected_decompiled.c section, selected_xrefs.csv rows,
interesting_strings.csv rows, function_labels.csv, and matching Source files.

Produce a function map with:
- proposed stable label
- evidence-backed purpose
- field order or constant mapping
- caller/callee relationships if visible
- current server behavior
- recommended implementation, diagnostic-only handling, or blocked decision

If the label is justified, update Decomp/Analysis/function_labels.csv and
re-export only <binary> to verify the label is applied.
```

### Label Prompt

Use this when the mapping is solid but no server change is safe yet:

```text
Add durable labels for the mapped <subsystem> functions only.

Rules:
- edit Decomp/Analysis/function_labels.csv
- use existing naming prefixes from CONTINUATION_GUIDE.md
- keep comments factual and short
- run an export-only Ghidra pass for the affected binary
- verify each new label appears in functions.csv and selected_decompiled.c
- update INITIAL_FINDINGS.md with what was labelled and why no implementation
  was added yet
```

### Implementation Prompt

Use this after a target reaches `Mapped` or `Verified`:

```text
Implement the verified NexusForever behavior for <target>.

Evidence:
- <binary> <function label/address>
- <selected_decompiled.c short location>
- <field/constant behavior summary>
- <matching source files>

Constraints:
- preserve existing project patterns
- implement only evidence-backed behavior
- leave unknown fields diagnostic-only or blocked in the tracker
- update INITIAL_FINDINGS.md or the focused tracker with the exact follow-up
- run the narrowest build or test command that proves the change

Deliver a concise summary of files changed, behavior implemented, labels added,
and verification result.
```

### Review Prompt

Use this after a mapping or implementation pass:

```text
Review the decompile continuation changes as a code/evidence review.

Check:
- labels are evidence-backed and use stable names
- findings cite enough location information to be rediscovered
- implementation does not go beyond mapped behavior
- generated artifacts or client binaries were not committed
- uncertain fields remain diagnostic-only or blocked
- verification is adequate for the behavior changed

Lead with bugs, risks, missing evidence, or missing tests. If no issues are
found, state the remaining residual risk.
```

## Implementation Checklist

Before editing source:

- [ ] Target question is narrow.
- [ ] Relevant exports are current enough.
- [ ] Candidate function map exists.
- [ ] Evidence confidence is at least `Mapped`.
- [ ] Matching NexusForever source surface is identified.
- [ ] Unknown fields are explicitly listed.

Before adding labels:

- [ ] Address is the function entry or a verified containing function.
- [ ] Name describes proven behavior.
- [ ] Comment is factual and short.
- [ ] Any MCP/Ghidra-only rename worth keeping is mirrored to `function_labels.csv`.
- [ ] Export-only run applies the label.
- [ ] Label appears in generated exports.

Before finishing a mapping-only pass:

- [ ] Relevant `mapping_artifacts/*.csv` rows were added or intentionally left unchanged.
- [ ] `Validate-DecompileMappingArtifacts.ps1` passes.
- [ ] Longer evidence is recorded in `INITIAL_FINDINGS.md` or a focused tracker when the CSV row cannot stand alone.
- [ ] Source traceability, fixture backlog, and live evidence blockers are explicit when they apply.

Before changing Ghidra datatypes:

- [ ] Generic pointer parameters were investigated across all related functions.
- [ ] Offset map records access size, access mode, and evidence source.
- [ ] Existing structures were searched before creating or renaming a type.
- [ ] Type application was verified across the whole mapped function family.

Before finalizing implementation:

- [ ] Source changes are minimal and local to the owning subsystem.
- [ ] No generated decompile output was copied into source.
- [ ] Diagnostics cover unresolved fields when useful.
- [ ] Findings/tracker updated.
- [ ] Build or focused verification completed.
- [ ] Any blocked behavior is named with the missing evidence.

## Common Pitfalls

- Implementing from a string match alone. A string proves an anchor, not the
  whole behavior.
- Treating a decompiler variable name as meaningful. Prefer field strings,
  constants, call order, and server-side correlations.
- Naming helpers too specifically before callers are understood.
- Broadening export patterns until every function is selected. Add narrow
  high-value patterns or labels instead.
- Running parallel workers against the shared Ghidra project or shared latest
  summary. Use `Start-DecompileBatch.ps1` or give each worker its own `-RunId`
  and `-SummaryPath`. The runner now waits on per-project Ghidra gates, but
  per-run summaries are still cleaner for parallel automation.
- Leaving an MCP or interactive Ghidra rename only in the local project. If the
  name is evidence-backed, mirror it to `function_labels.csv`; if it is still a
  hypothesis, keep it out of source-controlled labels.
- Mutating spell state from unknown `DataBits` because the value "looks like"
  an id. Prove the id space and behavior first.
- Implementing crypto, anti-tamper, or token behavior from partial structure.
  Keep these blocked until semantics are clear and locally testable.
- Forgetting to update findings. The next pass depends on the written trail more
  than on the current Ghidra project.

## Definition Of Done For A Pass

A decompile continuation pass is done when it leaves the repository in one of
these states:

- **Mapped only:** labels and findings were updated, no server behavior was safe
  to change, and the blocker is explicit.
- **Implemented:** labels/findings were updated, NexusForever behavior changed,
  verification ran, and remaining uncertainty is documented.
- **Rejected:** the evidence did not support the target hypothesis, the rejected
  mapping is recorded briefly, and no speculative changes remain.

Do not leave a pass with only local Ghidra renames, unrecorded export knowledge,
or implementation changes that are not tied back to the evidence.
