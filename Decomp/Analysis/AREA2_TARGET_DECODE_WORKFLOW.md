# Area 2 Target Decode Workflow

This workflow is the focused follow-on to `EVIDENCE_LOOP_PROCEDURE.md` for the
target-helper chain and target types `0`, `6`, and `7`.

Status update (2026-05-19): the helper-chain decode is no longer the primary
decomp blocker. Use this workflow for follow-up witness validation and
world-target routing questions, not as the main next-area roadmap item.

Use it when the question is specifically about `Spell4TargetMechanics`,
`Spell4ValidTargets`, or the client-side helper path behind
`Game.Spell:IsSelfSpell` and related target helpers.

## Data Boundary

Use the two local MySQL schemas for different purposes:

- `nexus_spell_re` is the focused spell/runtime fixture mirror and should stay the default for fast queries `45` through `47`.
- `wildstar_client` is the canonical import of extracted client tables. Use it when an Area 2 pass needs client-side support tables that are not present in `nexus_spell_re`, such as `Spell4ServiceTokenCost`.

## Current Critical Anchors

- `FUN_1403acd90` - current durable label: spell-wrapper/service resolver.
- `FUN_1403b4ec0` - current durable label: unresolved bool helper in the target path.
- `FUN_1407a0fd0` - current durable label: target-flags resolver after the type-7 lookup branch.
- `DAT_140c65b70 + 0x788` - spell-id keyed tree used by the mapped type-7 service-lookup branch.
- Witness spells: `305`, `11820`, `339`, and `26813`.

## Goals

Area 2 is complete only when the pass produces all three outputs:

1. A documented target-type decision table for `0`, `6`, and `7`.
2. A documented valid-target-mask boundary for the next safe non-corpse categories beyond `0x02` and `0x08`.
3. A proven next runtime slice for world-target family expansion, or an explicit blocker that keeps the slice diagnostic-only.

## Step 1: Pick One Witness Family

Start with one of the unresolved target witnesses from
`Spell Reverse Engineering Runtime Candidates.md`:

- Type `0`: `Spell4=305`
- Type `6`: `Spell4=11820`
- Type `7`: `Spell4=339` or `26813`

Current type-`6` caveat: `11820` is backed by `ItemSpecial=3777` with
`spell4IdOnActivate=11820`, but the extracted client tables currently expose
zero `item2.itemSpecialId00` owners for that item-special row. Treat it as a
real mechanic-`17` witness with an incomplete practical cast surface.

For data-side context, run query `45` from
`Tools/SpellReverseEngineering/world_context_queries.sql`. When the witness
depends on client-only side tables that are not mirrored locally, use the
schema-qualified `wildstar_client` variant from the same query file.

## Step 2: Capture The Spell Shape

Run the normal fixture pair first.

```text
/spell inspect4 <spell4Id>
/spell cast4 <spell4Id>
```

Record at minimum:

1. `Spell4TargetMechanics` id, target type, and flags.
2. `Spell4ValidTargets.targetBitmask`.
3. Target-angle and AOE constraint values.
4. Whether the spell carries a unit target, world-object target, or position target in current runtime handling.

## Step 3: Refresh The Client Export

Use a focused export for `WildStar64.exe` and validate the manifest.

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 260
.\Decomp\Analysis\Test-DecompileManifest.ps1 -Targets WildStar64.exe -FailOnMismatch
```

If the pass only needs xrefs or helper-script output, use `-DecompileMode Skip`.

## Step 4: Walk The Helper Chain

For each witness, answer these questions in order:

1. Does the witness go straight through `Game.Spell:IsSelfSpell`, or through another wrapper first?
2. Does it hit `FUN_1403acd90`, `FUN_1403b4ec0`, or `FUN_1407a0fd0`?
3. If the witness is type `7`, what node key and secondary spell payload does the `DAT_140c65b70 + 0x788` tree resolve?
4. Which wrapper offsets are actually read on the resolved object?

Current documentation-grade offsets to preserve:

- `+0x20` tree key
- `+0x28` secondary spell payload
- `+0x7c` target-shape selector
- `+0x9c` unresolved conditional target-AOE gate
- `+0x108` resolved targeting-flags word used by `IsFreeformTarget`

## Step 5: Classify The Result Conservatively

Every witness pass should end in one of four states:

- `Mapped`: helper path, offsets, and branch outcome are understood.
- `Documentation-Grade`: offsets or tree structure are proven, but the final helper is still blocked.
- `Observed`: one branch or offset is seen, but there is not enough evidence to name semantics.
- `Blocked`: current decompile/export does not expose enough of the helper path.

Do not widen runtime target behavior until at least two witnesses agree.

## Step 6: Promote Only The Stable Pieces

Safe promotion targets after each pass:

1. `Decomp/Analysis/INITIAL_FINDINGS.md`
2. `Decomp/Analysis/function_labels.csv`
3. `Spell System Progress Tracker.md`
4. `Global Spell System Reverse Engineering.md`

If the witness only improves structure, update docs and labels. If it changes a
runtime assumption, keep the runtime behavior diagnostic-only until a second
independent witness agrees.

## Current Guardrails

- Do not implement target types `0`, `6`, or `7` from SQL alone.
- Do not widen world-target effect routing from the current activation-object slice without a proven helper result.
- Do not infer new valid-target mask categories from names alone; keep them evidence-only until decompile and runtime agree.