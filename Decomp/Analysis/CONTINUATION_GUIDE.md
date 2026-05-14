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
| `Decomp/Analysis/scripts/ApplyNexusForeverLabels.java` | Applies source-controlled function labels before export. |
| `Decomp/Analysis/scripts/ExportNexusForeverAnalysis.java` | Writes repeatable CSV and decompiler exports. |
| `Decomp/Analysis/function_labels.csv` | Durable function label map. This is the main bridge from native addresses to named evidence. |
| `Decomp/Analysis/INITIAL_FINDINGS.md` | Living summary of mapped behavior and follow-up implementation. |
| `Decomp/Analysis/exports/<binary>` | Generated analysis exports. Ignored by Git. |

The default analysis targets are:

- `WildStar64.exe`
- `Houston64.exe`
- `StsConnLib64.MT.dll`

## Working Principles

1. Keep client binaries, Ghidra projects, logs, and decompiled exports out of
   source control.
2. Keep durable knowledge in source-controlled artifacts:
   `function_labels.csv`, `INITIAL_FINDINGS.md`, focused tracker documents, and
   clean C# implementation changes.
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

Increase `-MaxDecompiledFunctions` only when the selected functions are too
shallow. If the desired function is not selected, prefer improving
`HIGH_VALUE_STRING_PATTERNS`, adding a durable label, or locating a better xref
over exporting thousands of functions.

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
2. `selected_xrefs.csv` for the referencing function entry.
3. `selected_decompiled.c` for the selected function body.
4. `functions.csv` for nearby named or unlabeled functions.
5. Source files under `Source/` for the current server behavior.

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

### 5. Add Durable Labels

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

### 6. Update Export Selection When Needed

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
selects the expected xrefs.

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

This map summarizes the known high-value areas so the next pass can start from
known terrain.

| Area | Binary | Durable labels or anchors | Server implementation surfaces | Next useful work |
| --- | --- | --- | --- | --- |
| STS auth transactions | `StsConnLib64.MT.dll` | `StsConn_SendLoginStart`, `StsConn_SendLoginFinish`, `StsConn_OnLoginFinishResponse`, `StsConn_SendRequestGameToken`, `StsConn_SendConsumeGameToken`, `StsConn_OnConsumeGameTokenResponse` | `NexusForever.Network.Sts/Model`, `NexusForever.StsServer/Network/Message/Handler` | Continue unmapped auth/token/external-account routes only when a local client flow needs them. |
| STS connect envelope | `StsConnLib64.MT.dll` | `StsInetSocket_ParseConnectFields`, `StsInetSocket_SendConnectEnvelope`, `StsInetSocket_DispatchConnectEnvelope` | `ClientConnectMessage`, STS session setup | Validate any remaining optional envelope fields against observed client startup. |
| Token crypto handshake | `StsConnLib64.MT.dll` | `StsConn_SendTokenKeyData`, `ServerRand`, `ServerPublicKey`, `ServerSignature`, `PremasterSecret`, `AuthnToken` | STS auth models and handlers | Keep blocked until the handshake semantics are understood safely. Do not guess crypto behavior. |
| World send framing | `WildStar64.exe` | `Network_SendMessageById`, `Network_SendResolvedMessage`, `Network_SerialiseResolvedMessage`, bit writer labels | `NexusForever.Network/Packet`, `NexusForever.Network/Session`, world messages | Continue into receive/framing/dispatch if client-server packet mismatch appears. |
| Socket driver behavior | `WildStar64.exe` | `NetworkSocket_*`, `NetworkIOCP_*`, `Network_ParseEndpointAddress` | `NetworkSession`, connection setup | Use only for compatibility and diagnostics, not large rewrites without a runtime issue. |
| Client DB registration | `WildStar64.exe`, `Houston64.exe` | `ClientDB_RegisterRealmDataCenter`, `ClientDB_RegisterWorldSocket`, table path strings | `GameTable`, `Tools/DataMapping`, runtime managers | Label more table registrations when a table is needed for data mapping. |
| Public-event constants | `WildStar64.exe` | `Lua_RegisterPublicEventConstants` and `PublicEventObjectiveType_*` strings | `Game.Static/PublicEvent`, public event scripts | Extend enum validation for objective flags and notification behavior. |
| Spell runtime hints | `WildStar64.exe` | `tSpell4IdAbility`, `tUnitProperty`, public-event and Lua anchors | `Game/Spell`, spell trackers, diagnostics | Continue family-by-family using diagnostics and data queries. |

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
- [ ] Export-only run applies the label.
- [ ] Label appears in generated exports.

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
