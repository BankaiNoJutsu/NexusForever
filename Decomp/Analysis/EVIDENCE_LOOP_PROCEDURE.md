# Spell And Packet Evidence Loop Procedure

This is the concrete Area 1 workflow for turning one decompile question into a
repeatable evidence bundle that can drive a safe server change.

The loop combines five local sources of truth:

- Focused Ghidra exports under `Decomp/Analysis/exports/<binary>`.
- `logs/LATEST_RUN_SUMMARY.json` plus the per-target `selected_decompiled.manifest`.
- Fixture SQL in `Tools/SpellReverseEngineering/world_context_queries.sql`.
- In-world `/spell inspect4` and `/spell cast4` commands.
- `SpellDiagnostics` trace output, packet boundaries, and state changes.

## Standard Output Bundle

Every pass should leave the same artifact bundle, even when the answer is
"blocked":

1. One narrow question.
2. One refreshed or reused decompile export.
3. One or more concrete fixture spells.
4. One normalized inspect capture.
5. One runtime cast capture.
6. One comparison against packets, diagnostics, and decompile evidence.
7. One durable update to findings, labels, or focused tracker docs.

## Step 1: Choose One Narrow Question

Good questions stay scoped to one boundary:

- What does target type `7` consume before `FUN_1403b4ec0` decides self-spell behavior?
- Which follow-up packets appear after `ServerSpellStart` for a given cast outcome?
- Which `RavelSignal` mode and signal rows are actually placed in content?

Bad questions are broad and mix multiple blockers:

- "Implement spell parity."
- "Decode all spell packets."

## Step 2: Refresh The Export And Check Reuse

Run only the target binary you need.

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 260
```

If the pass only needs strings, imports, and xrefs, skip the expensive C output.

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -DecompileMode Skip
```

Then validate the latest manifest summary.

```powershell
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
```

Use the manifest data to answer three questions before moving on:

1. Did the run write the expected export directory?
2. Did the selected-function set stay stable or change?
3. Did the exporter reuse cached fragments or fully re-decompile?

## Step 3: Pick A Fixture Spell Or Family Witness

Use `world_context_queries.sql` instead of hand-built SQL. Start with the query
that matches the question:

- Query `3`: broad placed spell candidates.
- Query `4` or `5`: multi-family witnesses.
- Query `8`: formula-heavy damage or heal rows.
- Query `45`: unresolved target-helper witnesses for target types `0`, `6`, and `7`.
- Query `46`: `RavelSignal` rows with placed context and payload fields.
- Query `47`: activation-object world-target families beyond the current runtime slice.
- Query `48`: a local regression probe for service-token property-flag rows in `nexus_spell_re`, plus the commented cross-schema cost-row and mismatch variant that pulls the durable client witnesses from `wildstar_client`.

Use the two local spell-data schemas intentionally:

- `nexus_spell_re` is the lean fixture database for fast spell/runtime joins.
- `wildstar_client` is the canonical import of extracted client tables and should be used whenever Area 2 needs client-only support tables that are not mirrored into `nexus_spell_re`.

Example:

```powershell
& 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe' -ubankai -pbankai --batch --raw --database=nexus_spell_re -e "SELECT * FROM world_runtime_spell_candidates WHERE entity_type = 0 ORDER BY priority_score DESC, placements DESC LIMIT 20;"
```

## Step 4: Capture Normalized Inspect Evidence

Run `/spell inspect4 <spell4Id>` for the concrete spell chosen in Step 3.

Capture at least these fields from the inspect output:

1. `Spell4` id, base spell id, and tier.
2. Timing and cooldown data.
3. Target mechanics, valid targets, and AOE constraints.
4. All effect rows with effect type, target flags, timing, data bits, and parameters.
5. Any relevant prerequisite, threshold, telegraph, or service-token fields.

`SpellCommandCategory` already prints the stable inspect surfaces needed for this
loop. Preserve the raw chat output in the evidence note or handoff doc.

## Step 5: Capture Runtime Evidence

Run `/spell cast4 <spell4Id>` in a controlled setup that matches the target
question. When object-target work is involved, select the world object first.

During or immediately after the cast, capture:

1. The outgoing and incoming packet boundary trace.
2. `SpellDiagnostics` entries for target selection, primary-target validation,
   effect dispatch, effect schedule, and effect result.
3. Family-specific diagnostics such as `ravel-signal`, `proc`, `sap-vital`,
   `set-busy`, `absorption`, or `threat-modification`.
4. Visible state deltas such as HP, shields, aura state, busy state, spawned
   entities, or quest/objective updates.

Useful log anchors already emitted by the server include:

- `SpellDiagnostics packet-boundary`
- `SpellDiagnostics target-selection`
- `SpellDiagnostics primary-target-validation`
- `SpellDiagnostics effect-dispatch`
- `SpellDiagnostics effect-result`

## Step 6: Compare Against Decompile Evidence

Use the refreshed export to answer whether the runtime observation is already
explained by known evidence or is still blocked.

The minimum comparison set is:

1. `selected_decompiled.c`
2. `selected_xrefs.csv`
3. `selected_reasons_summary.csv`
4. `functions.csv`
5. The relevant server files under `Source/`

If the runtime observation and decompile shape disagree, record the mismatch and
keep the behavior diagnostic-only.

## Step 7: Promote The Result Conservatively

Only promote when at least two witnesses agree:

- Client decompile plus runtime or packet output.
- SQL shape plus runtime output.
- Packet model plus client send or parse behavior.

Promotion targets, in order:

1. `Decomp/Analysis/INITIAL_FINDINGS.md`
2. `Decomp/Analysis/function_labels.csv`
3. Focused tracker docs such as `Spell System Progress Tracker.md`
4. Narrow runtime or packet-model changes in `Source/`

If the evidence is still incomplete, record the blocker instead of widening the
runtime shell.

## One-Pass Example

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 260
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
& 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe' -ubankai -pbankai --batch --raw --database=nexus_spell_re -e "SELECT * FROM spell4 s JOIN spell4base b ON b.ID = s.spell4BaseIdBaseSpell JOIN spell4targetmechanics tm ON tm.ID = b.spell4TargetMechanicId WHERE tm.targetType IN (0,6,7) ORDER BY tm.targetType, tm.ID, s.ID LIMIT 20;"
```

Then in-world:

```text
/spell inspect4 <spell4Id>
/spell cast4 <spell4Id>
```

Follow the output through diagnostics, packets, and the focused export before
changing any server behavior.