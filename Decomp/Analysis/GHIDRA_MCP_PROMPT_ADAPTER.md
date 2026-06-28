# Ghidra MCP Prompt Adapter

This note adapts the upstream
[`bethington/ghidra-mcp` prompt workflows](https://github.com/bethington/ghidra-mcp/blob/main/docs/prompts/README.md)
to the NexusForever decompile process.

Use the upstream prompts as interactive Ghidra discipline. Use this repository's
`function_labels.csv`, findings, trackers, and focused C# changes as the durable
NexusForever record.

## Start A Session

From the repository root, print the project handoff:

```powershell
.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe
```

Open the printed `.gpr` in Ghidra and open the printed program in CodeBrowser.
Then use MCP in this order:

```text
mcp__ghidra_mcp.list_instances
mcp__ghidra_mcp.connect_instance(project="<printed project>")
mcp__ghidra_mcp.list_tool_groups
mcp__ghidra_mcp.load_tool_group(group="function")
```

Load other groups only when needed, such as `datatype`, data-analysis, dynamic
analysis, or debugger groups.

For live `debugger_*` MCP proxy tools, start the standalone debugger bridge in a
separate PowerShell session before attaching:

```powershell
I:\ghidra_12.1_PUBLIC\Start-GhidraDebuggerBridge.ps1
```

The bridge listens on `http://127.0.0.1:8099` by default. For first-time setup,
create the local debugger venv and install the optional Windows dependencies:

```powershell
I:\ghidra_12.1_PUBLIC\Start-GhidraDebuggerBridge.ps1 -CreateVenv -InstallRequirements -ValidateOnly
```

If the optional `debugger` Python package was not copied with the Ghidra MCP
checkout, pass `-GhidraMcpSourceRoot <path-to-ghidra-mcp>`. When the local
`.venv-ghidra-debugger` exists, the launcher uses it automatically unless a
specific `-Python` is supplied.

## Pick The Prompt

| Nexus task | Upstream prompt | Nexus adaptation |
| --- | --- | --- |
| Document one mapped function | [`FUNCTION_DOC_WORKFLOW_V5.md`](https://github.com/bethington/ghidra-mcp/blob/main/docs/prompts/FUNCTION_DOC_WORKFLOW_V5.md) | Use the type-first, comments-last ordering inside Ghidra. Mirror stable function names to `function_labels.csv` before ending the pass. |
| Investigate structure or parameter types | [`DATA_TYPE_INVESTIGATION_WORKFLOW.md`](https://github.com/bethington/ghidra-mcp/blob/main/docs/prompts/DATA_TYPE_INVESTIGATION_WORKFLOW.md) | Use the local [`DATA_TYPE_INVESTIGATION_WORKFLOW.md`](DATA_TYPE_INVESTIGATION_WORKFLOW.md) checklist, build offset maps across all relevant functions, and record durable structure findings before implementing server behavior. |
| Label strings or globals | [`STRING_LABELING_CONVENTION.md`](https://github.com/bethington/ghidra-mcp/blob/main/docs/prompts/STRING_LABELING_CONVENTION.md) and [`TOOL_USAGE_GUIDE.md`](https://github.com/bethington/ghidra-mcp/blob/main/docs/prompts/TOOL_USAGE_GUIDE.md) | Useful for local Ghidra clarity. Promote only behaviorally meaningful function labels or findings to source control. Do not churn labels for generic UI/source-path strings. |
| Check ambiguous pure logic | Tool guide dynamic analysis notes | Use `analyze_dataflow` or `emulate_function` as a cross-check, not as standalone implementation evidence. |
| Find missed functions | [`ORPHANED_CODE_DISCOVERY_WORKFLOW.md`](https://github.com/bethington/ghidra-mcp/blob/main/docs/prompts/ORPHANED_CODE_DISCOVERY_WORKFLOW.md) | Treat as an explicit boundary-recovery task only. Do not create functions in bulk without triage, and do not enable inline scripts unless the pass calls for it. |
| Cross-version propagation | Cross-binary workflow prompts | Usually out of scope for build 16042-only Nexus work. Use only when comparing client versions is the explicit target. |

## Function Documentation Adapter

Use upstream V5 ordering for Ghidra edits:

1. Analyze and classify the function.
2. Rename the function and set a prototype.
3. Re-fetch variables, resolve types, then rename variables.
4. Add plate and inline comments after naming and typing.
5. Run the completeness check and fix significant deductions.

Then do the Nexus durability step:

1. Add or update the stable function row in
   `Decomp/Analysis/function_labels.csv`.
2. Re-export only the affected binary:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe
```

3. Confirm the label appears in `exports\<binary>\functions.csv` and, when in
   the selected set, `selected_decompiled.c`.
4. Record the mapping in `INITIAL_FINDINGS.md` or the focused tracker.

The upstream "do not edit filesystem files" rule applies while running the
interactive prompt itself. It does not replace the Nexus source-controlled
handoff.

## Data-Type Investigation Adapter

Use MCP to improve the local Ghidra database when type information reduces
decompiler ambiguity, especially for:

- packet writer/reader objects;
- callback-table and vtable families;
- table registration rows;
- entity, item, spell, and Lua userdata helper contexts;
- fixed parser structs where the same offsets appear across multiple functions.

Use [`DATA_TYPE_INVESTIGATION_WORKFLOW.md`](DATA_TYPE_INVESTIGATION_WORKFLOW.md)
as the Nexus checklist for target scoping, offset maps, existing-type searches,
family-wide type application, and verification.

Do not create a Ghidra struct from a single offset observation. First build an
offset map with:

- address and function name;
- read/write/call-site access;
- offset, size, and inferred type;
- matching field names in NexusForever source, packets, tables, or diagnostics;
- open uncertainty.

Only translate data-type discoveries into C# behavior once they reach the
normal Nexus evidence ladder threshold.

## Tool Discipline

- Prefer native MCP tools over inline Ghidra scripts.
- Use batch MCP operations when available for variable renames, comments, tags,
  and labels.
- Type before using Hungarian prefixes.
- Use `rename_or_label` plus explicit type application for globals and strings.
- Use dynamic analysis tools as cross-checks when static evidence is ambiguous.
- If a tool is not visible after connecting, call `list_tool_groups` and load the
  relevant group; lazy bridges may expose only a subset at first.
- Keep MCP server bindings on loopback unless authentication is configured.

## What Counts As Done

End an MCP-assisted pass in the same states as any decompile pass:

- `Mapped only`: Ghidra has useful local names/types/comments, durable labels or
  findings were updated, and implementation remains blocked with the reason.
- `Implemented`: durable labels/findings and NexusForever code changed, and the
  focused build/test/runtime check passed.
- `Rejected`: the MCP investigation falsified the target hypothesis, with no
  speculative source changes left behind.

Do not end with only interactive Ghidra edits. If the mapping matters, leave the
next session a source-controlled trail.
