# Type 102: primary subject and reference census (2026-11-14)

Provenance: lead re-derived the build-16042 dispatcher and helper with Ghidra
12.1.4 read-only decompilation and LLVM 21.1.8 original-byte disassembly. REA
`open_binary` bound the executable to SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Evidence tier: **Verified native subject selection**; the existing server effect
ID/producer/lifetime model retains its earlier correlation/inference limits.
No retail parity claim. Static/headless only.

Later continuation: [native node ID/lifetime](PREREQUISITE_102_NODE_LIFETIME_20261114.md)
recovers the native Spell4Effects ID, target ownership and registration lifetime.
It supersedes the native element/ID uncertainty below, while retaining explicit
server receiving-unit and untimed-lifetime coverage gates.

## Question and discrepancy, recorded before editing

Does type 102 retain the primary subject, and why did the 2026-11-10 audit report
352 spell references while the earlier 2026-11-14 pass reported 102 direct /
121 slot-weighted references?

Before editing, `PrerequisiteCheckSpellCooldownNodeOnUnit.Meets` delegated with
`parameters?.Target ?? player`; `MeetsUnit` retained its supplied subject. The
existing `ThePlayerEntryPointEvaluatesTheTargetWhenOneIsSupplied` encoded the
substitution. The competing selector readings were primary versus relationship
unit: a dispatcher read plus contradictory primary/target state separates them.
The question, candidate files and red/focused-build verification plan were
written before the production edit. No effect-instance producer change was
proposed from this selector read.

## Native witnesses, read directly

* Dispatcher `1404a2100`, case `0x66`: passes primary `*param_2`, comparison
  (slot word 1) and ObjectId (word 2) to `1404a4fe0`. The call site
  `1404a2cc1..1404a2ccc` loads R9D from slot `+8`, R8D from slot `+4`, and RDX
  from the first unit pointer, then calls the helper. It does not load the
  second unit for this case. Value (word 3) is unread.
* `1404a4fe0..1404a5077`: null primary returns false for every comparison.
  It obtains the list from primary `+0x15d0` (`1404a5007`), compares each node's
  virtual `+0x30` with the supplied ObjectId (`1404a5026..1404a5029`), and
  advances through node `+0x30`. Equal returns whether a node matches;
  NotEqual returns the negation; all other comparisons fail. No second-unit
  substitution or Value/zero-ObjectId special case is in this body.

Independent reference/caller cross-check: Spell4 26793 references prerequisite
10504 through `prerequisiteIdCasterCast` (NotEqual, ObjectId 44148, Value 0).
Spell4 33644 references 10096 through `prerequisiteIdTargetCast` (Equal, ObjectId
65808, Value 33730). Spell4 27141 references 5616 through target-cast, whose
slots 1/2 test ObjectIds 45049/44081. Current `Spell.CheckPrerequisites` explicitly
chooses caster or target as primary for those columns, placing the other unit
in relationship context. Apply and persistence routes also choose their subject
explicitly. These Source paths already retain that subject and are unchanged.
The native activation caller policy was not re-derived; its player/relationship
arguments do not independently qualify that caller's policy.

The helper is durably renamed `Prerequisite_CheckPrimaryListNodeId_LiveCase66`:
the old cooldown-node name implied a node class not established by the body.
One focused label was applied and re-exported (one applied; zero missing or
duplicates), with matching coverage and evidence-inventory updates. The enum
name remains historical. This pass does not qualify the concrete node class,
Spell4Effects ID interpretation, pending-spell coverage or effect lifetime.

## Reference-count reconciliation (reference-data tier)

The read-only imported `wildstar_client` census examines all three prerequisite
slots and all eight Spell4 / five Spell4Effects prerequisite columns. It does
not expand nested prerequisites or filter slots after None. An in-memory join
of exported rows and a separate SQL CTE census agree:

* **353 slots / 303 distinct prerequisite rows**, 290 distinct ObjectIds.
* **302 direct column references / 352 slot-weighted references**.
* 289 distinct source table rows, reaching 130 distinct prerequisite rows.
* Comparisons: 104 Equal / 249 NotEqual; Value zero in 254 slots.

A direct reference counts each table-row/column pointing at a containing
prerequisite once. A weighted reference counts each matching type-102 slot in
that prerequisite. Neither measures affected casts; the explicit-unit spell
paths already retain their subjects.

| Column | Direct | Weighted |
|---|---:|---:|
| Spell4 runner 00 | 10 | 19 |
| Spell4 caster cast | 9 | 9 |
| Spell4 target cast | 23 | 27 |
| Spell4 caster persistence | 1 | 1 |
| Spell4 target persistence | 7 | 7 |
| Spell4 AOE target | 28 | 28 |
| Effects caster apply | 23 | 29 |
| Effects target apply | 162 | 192 |
| Effects caster persistence | 1 | 1 |
| Effects target persistence | 38 | 39 |
| **Total** | **302** | **352** |

Runner 01, AOE preferred target and Effects target suspend each have zero reach.
The old audit's **352 is correct as a slot-weighted count**, so its numeric
coverage row is retained. The mapping script's input documentation now states
the weighted meaning of its historical `spellReferences` column; parsing/output
behavior is unchanged. The earlier 102/121 result is superseded. Its shortfall
is exactly **200 direct / 231 weighted**, equal to the target-apply plus
target-persistence effect columns. Excluding those two columns reproduces
102/121 exactly. The original session query text enumerated all prerequisite
columns; replaying the same query shape now gives 302/352. Thus the earlier
result was incomplete, but why those results were absent historically is
**unknown**. No claim of an intentional scope choice, query defect or database
mutation is made from these totals alone.

Reproduce the SQL census and concrete cast anchors with tracked
`coverage/prerequisite_102_reference_census_20261114.sql`, passed on stdin to the
configured MySQL client with database `wildstar_client`. It contains SELECT/CTE
statements only and no credentials. The independent row-join script, previous
query replay and detailed outputs remain ignored artifacts.

## Implementation and Source verification

Only the player wrapper changes behavior: it delegates with `player`. The
explicit-unit handler and existing `HasActiveSpellEffectId` model are unchanged.
Fourteen new cases cover contradictory membership through both real manager
entry points and spell apply; explicit target-as-primary controls; unread Value;
null primary at the owning check; and absent relationship parameters. Before
the production change: **10 failed / four passed**, with assertion failures.
The old substitution test is rewritten as
`ThePlayerEntryPointRetainsThePlayerAndAllowsExplicitEffectTargetEvaluation`,
preserving the explicit-target positive control. No warning/assertion is
suppressed. Generic player-manager null logging remains outside this slice.

Rebuilt focused gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`:
**2,996 passed / zero failed or skipped**. This includes 23 cases in the
type-102 class. Test compilation retains five CS8632 warnings in the unmodified
MySQL qualification test file and the existing ASPIRE010 CLI-bundle warning.
Explicit owning build:
`dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`:
**zero warnings/errors**. `Validate-DecompileMappingArtifacts.ps1` passes:
**8,332 rows / seven files**. `git diff --check` passes.

Full suite on the same rebuilt test project with
`--no-build --no-restore -v minimal --nologo --logger "trx;LogFileName=full.trx" --results-directory artifacts/prerequisite-type102/full`:
**15,112 passed / 17 failed / two skipped / 15,131 total**. All **23 type-102
cases passed**. Fifteen failures reported EF `ManyServiceProvidersCreatedWarning`;
the other two were `Assert.Equal` in the challenge reward executor and
`Assert.Single` in title publication. All **13 failed methods passed separate
processes, covering 19 cases**. Isolation does not establish their cause or make
the full suite green. No provider warning/assertion was suppressed or changed.

The read-only SQL census and independent row join agree on totals and all 13
columns (`census-cross-check.log`). The mapping helper's documentation-only edit
passes `python -m py_compile Decomp/Analysis/scripts/prerequisite_evaluator_case_map.py`.
End state: **Implemented / Source**, supported by Verified native selector
evidence; no retail parity claim.

## Reproduction and limits

Ignored outputs under `artifacts/prerequisite-type102/`: `native.txt`,
`native-run.log`, `native-disassembly.txt`, `dispatcher-disassembly.txt`,
`focused-labels.csv`, `labelled.txt`, `labelled-run.log`, `census.py`,
`census-summary.json`, `slots.json`, `references.json`, `census-sql.tsv`,
`previous-query-replay.py`, `previous-query-replay.log`, `red.log`, `green.log`,
`owning-build.log`, `mapping-validation.log`, `census-cross-check.log`, `full.log`,
`full/full.trx`, `full/summary.json`, `full/failed-methods.json`,
`full/isolated-results.json`, `full/isolated-*.log` and `rerun-full-failures.py`.
Use `Gap8Decomp.java` for 1404a2100 / 1404a4fe0 and original-byte disassembly of
the call site and helper. Focused label application uses
`ApplyNexusForeverLabels.java` before re-export in a read-only Ghidra session.
No decompiled client source is committed; generated exports are not refreshed.
REA's binary session is closed.

Native element class/ID and lifetime semantics, mixed non-unit prerequisite
bypass, activation caller policy, native acceptance and retail qualification
remain separate gates. No runtime deployment, native client execution or GUI.
