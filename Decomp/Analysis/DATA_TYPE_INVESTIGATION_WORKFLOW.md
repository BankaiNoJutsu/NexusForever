# Data Type Investigation And Structure Standardization Workflow

Use this workflow when a Ghidra/MCP pass needs to replace generic parameters
such as `int *`, `uint *`, `void *`, or `undefined8 *` with evidence-backed
structure types. The goal is better local decompilation and a durable
NexusForever evidence trail, not speculative server behavior.

The core rule is simple: infer a type from how the value is used across all
relevant functions, not from one parameter name or one offset.

## Nexus Boundaries

- Treat Ghidra datatype edits as local analysis state until the evidence is
  recorded in `INITIAL_FINDINGS.md`, a focused tracker, or
  `function_labels.csv` when a durable function label is justified.
- Do not copy decompiled client source into the repository. Record function
  names, addresses, offsets, constants, field names, and short export
  references instead.
- Do not implement NexusForever behavior from a single offset observation.
  Structure evidence can justify labels at `Mapped`; implementation still needs
  the normal `Verified` or explicitly safe compatibility boundary.
- Keep unknown fields diagnostic-only. Name structure fields by identity and
  role, not temporary state.
- Leave generated exports, Ghidra projects, logs, and client binaries out of
  source control.

## Identity Naming

Name a structure for what it is:

- Good: `UnitAny`, `SkillData`, `PacketBitWriter`, `ClientOpcodeEntry`.
- Avoid: `InitializedUnit`, `AllocatedSkill`, `ProcessedData`.

Use conservative field names until semantics are proven:

- Good: `field_1a4`, `flags`, `count`, `entryCount`, `targetUnitId`,
  `packetWriter`.
- Avoid: `health` or `spellId` unless cross-function evidence proves that role.

## Phase 1: Choose The Target Pattern

Start with one narrow question, for example:

```text
Which structure type is passed as the second parameter to the packet writer
family around WildStar64.exe 14007d010?
```

Search exports, labels, and source together:

```powershell
rg -n "void \*|int \*|uint \*|undefined8 \*|pUnit|pData|pPacket" Decomp\Analysis\exports Decomp\Analysis\function_labels.csv Source
```

When using `ghidra-mcp`, gather the same facts with function and datatype tools
when they are available:

```text
analyze_function_complete(<function>, include_xrefs=true, include_disasm=true)
get_function_variables(<function>)
search_data_types(<pattern>)
list_data_types(category="struct")
```

Record the starting set before editing Ghidra:

| Function | Address | Parameter | Current type | First usage pattern | Candidate type |
| --- | --- | --- | --- | --- | --- |
| `<name>` | `<addr>` | `pData` | `int *` | reads offsets `0x4`, `0xc` | `<unknown>` |

## Phase 2: Analyze One Function Deeply

For the first candidate, inspect both decompiled code and disassembly. Capture:

- null checks and early exits;
- every dereferenced offset;
- access size, such as byte, word, dword, qword, or pointer;
- read, write, compare, call, or address-taking behavior;
- constants compared against the field;
- loop bounds, array indexes, and stride calculations;
- allocation sizes, constructor-like writes, and destructor-like cleanup.

Useful patterns:

| Pattern | Possible meaning |
| --- | --- |
| `object + index * stride` | array of fixed-size structures |
| field used as loop bound | count or capacity |
| field passed to `memcpy` or string copy size | byte length or character count |
| field masked with constants | flags or packed enum |
| field loaded then called indirectly | function pointer or vtable slot |
| repeated writes during setup | constructor or parser output |

Do not assign the final field name yet. Build the offset facts first.

## Phase 3: Cross-Reference All Related Functions

Find other functions that receive the same object shape. Use:

- callers and callees of the first function;
- functions with the same generic parameter name;
- functions accessing the same offset range;
- registration tables, vtables, callback families, or packet opcode rows;
- matching NexusForever packet, table, entity, item, spell, or manager source.

For vtable and callback work, inspect:

- `exports\<binary>\function_pointer_families.csv`;
- `exports\<binary>\function_pointer_family_slots.csv`;
- helper-script output from `DumpNearbyData.java` or `FindDataReferences.java`
  when the standard export points at a table but not enough local context.

Create a single offset map across all functions:

| Offset | Size | Access | Candidate field | Inferred type | Evidence |
| ---: | ---: | --- | --- | --- | --- |
| `0x00` | 4 | read/write | `field_00` | `uint` | setup writes zero, readers compare with small constants |
| `0x04` | 4 | read | `flags` | `uint` | bit masks in three functions |
| `0x08` | 8 | call | `callback` | function pointer | indirect call after null check |

If a field role is unclear, trace producers or consumers. With MCP tools that
support data flow:

```text
analyze_dataflow(address=<write_site>, variable="<source>", direction="backward")
analyze_dataflow(address=<read_site>, variable="<loaded_value>", direction="forward")
```

Backward traces from writes identify whether a field stores a caller parameter,
table lookup, constant, or computed value. Forward traces from reads identify
whether the field feeds a loop counter, copy length, flag test, pointer
dereference, or outbound packet write.

## Phase 4: Search Existing Data Types

Before creating a new structure, search existing Ghidra datatypes:

```text
search_data_types(pattern="<domain>")
list_data_types(category="struct", limit=500)
```

Compare candidates against the offset map:

- field offsets match;
- field sizes match the actual access width;
- total size matches array stride or allocation size when visible;
- pointer fields and nested structures line up with calls and dereferences;
- naming matches the object's identity, not a transient operation.

If an existing structure mostly matches, update or extend it only where the
new evidence is stronger than the current layout.

## Phase 5: Create Or Refine The Structure

Create a structure only after the offset map has enough coverage to avoid
locking in a misleading shape. Prefer explicit unknown gaps over guessed names:

| Offset range | Field name | Type | Reason |
| --- | --- | --- | --- |
| `0x00..0x03` | `type` | `uint` | compared with known table ids |
| `0x04..0x07` | `flags` | `uint` | masked with `0x1`, `0x4`, `0x20` |
| `0x08..0x0f` | `target` | pointer | dereferenced after null check |
| `0x10..0x1f` | `unk_10` | bytes | no observed access yet |

When using MCP or Ghidra tools, the exact commands depend on the loaded bridge,
but the operation shape is:

```text
create_struct("<IdentityName>", fields)
set_parameter_type(function_address="<addr>", parameter_name="<name>", new_type="<IdentityName> *")
get_decompiled_code(function_address="<addr>", refresh_cache=true)
```

For complex domains, split helper structures by role:

- static table row, such as a client DB or opcode registration entry;
- runtime object, such as a unit, item, spell, or request context;
- parameter block passed between parser/serializer helpers;
- callback or vtable slot layout.

Do not force unrelated offsets into one large structure just because they share
a domain word such as "skill" or "unit".

## Phase 6: Apply Types Across The Whole Family

After the structure is defined, apply it consistently to every mapped function
that receives the same object shape. For each update:

- set the parameter type;
- refresh the decompiler view;
- confirm field names appear where raw offsets were previously used;
- check that no access now lands on the wrong field or alignment boundary;
- record any function that still needs a different helper structure.

Do not leave a family half typed without documenting why. A partial type pass is
acceptable only when the remaining functions are explicitly blocked by missing
evidence.

## Phase 7: Verify Field Offsets And Size

Verify the final layout against actual access sites:

| Function | Access | Expected field | Result |
| --- | --- | --- | --- |
| `<name>` | `[object + 0x04]` dword read | `flags` | matches |
| `<name>` | `[object + 0x08]` pointer deref | `target` | matches |

Check:

- highest observed offset plus field size;
- array stride;
- allocation size;
- compiler alignment and padding;
- signedness suggested by comparisons and arithmetic;
- byte and word fields that may be packed into a larger decompiler access;
- union-like paths where different branches write different roles.

If size or alignment does not match, revisit field widths before adding more
names.

## Phase 8: Record The Durable Finding

A completed datatype pass should leave a compact source-controlled note:

```markdown
### <StructureName> datatype map

- Binary/function family:
- Evidence confidence:
- Functions inspected:
- Structure size:
- Open blockers:

| Offset | Size | Field | Type | Evidence |
| ---: | ---: | --- | --- | --- |
| `0x04` | 4 | `flags` | `uint` | masks in `<function>` and `<function>` |
```

If the pass also justifies labels, update `function_labels.csv` and run an
export-only verification for the affected binary. If it only improves local
Ghidra types, record the structure map in `INITIAL_FINDINGS.md` or a focused
tracker and explain why no source behavior changed.

## Phase 9: Decide The NexusForever Action

Use the evidence ladder:

- `Observed`: record raw offset facts only.
- `Correlated`: add tentative finding when the layout matches source, packets,
  tables, or runtime diagnostics.
- `Mapped`: durable labels and datatype maps are appropriate.
- `Verified`: implementation can be considered if the server ownership boundary
  is clear.
- `Implemented`: C# changed and focused verification passed.
- `Blocked`: structure shape is known but mutation is unsafe; name the missing
  evidence.

For runtime code, do not query `wildstar_client`, `jabbithole`, or staging
mapping tables. Promote reviewed data into runtime-owned NexusForever tables,
scripts, or code-owned assets first.

## Completion Checklist

- [ ] Target question is narrow.
- [ ] All related functions found or the search boundary is documented.
- [ ] Offset map includes access size, mode, and evidence source.
- [ ] Existing structures were searched before creating a new one.
- [ ] New or refined structure name describes identity.
- [ ] Unknown fields and padding are explicit.
- [ ] Type application was verified in every updated function.
- [ ] Field offsets, structure size, stride, and alignment were checked.
- [ ] Durable labels, findings, or tracker notes were updated.
- [ ] Generated exports and local Ghidra artifacts were not committed.
- [ ] Final state is `Mapped only`, `Implemented`, `Rejected`, or `Blocked`.

## Common Pitfalls

- Treating a parameter name such as `pUnit` as proof of the underlying
  structure.
- Defining a structure from only one function's offsets.
- Naming a field from one comparison constant without checking writes and
  consumers.
- Missing array stride or allocation size, then creating a structure with the
  wrong total size.
- Collapsing static table rows, runtime objects, and parameter blocks into one
  type.
- Forgetting to mirror durable function names to `function_labels.csv`.
- Implementing packet, spell, crypto, token, anti-tamper, or runtime state
  behavior before the datatype evidence reaches the required confidence level.
