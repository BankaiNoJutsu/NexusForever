# Plan: Reverse Engineer Spell Effect Handlers

Treat spell restoration as an effect-family decoding problem, not a spell-by-spell implementation problem. The leverage point is the existing data-driven pipeline: once an effect family is understood and its handler semantics are implemented, many spells start working at once. The practical workflow is to combine decompiled client behavior, retail sniff results, and local trial-and-error into a repeatable evidence loop.

## Steps

1. Stabilize the analysis surface around the existing runtime path so all reverse engineering targets the same core seams: `GlobalSpellManager.cs`, `SpellBaseInfo.cs`, `SpellInfo.cs`, `Spell.cs`, `SpellEffectHandler.cs`, and `DamageCalculator.cs`.
2. Define a standard spell decomposition model for all future work: base targeting and cast metadata from `Spell4Base` and `Spell4`, effect rows from `Spell4Effects`, telegraphs from `Spell4Telegraph` and `TelegraphDamage`, and any chained or scripted behavior. This keeps investigation structured instead of ad hoc.
3. Build an effect-family evidence matrix outside the code. For each `SpellEffectType`, capture representative spell IDs, observed behavior from sniffs, decompiled client hints, all `DataBits` and `ParameterType` and `ParameterValue` fields, timing, prerequisites, and whether the server already partially interprets the family.
4. Add structured diagnostics at the central execution points so local casts produce usable reverse-engineering evidence. The first instrumentation points should be `Spell.cs`, `DamageCalculator.cs`, and packet boundaries in `GameSession.cs`.
5. Use a repeatable sniff-to-runtime comparison loop. Reproduce one retail behavior from sniff evidence, trigger the same effect family locally through `SpellCommandCategory.cs`, compare packets, combat logs, and state changes, then record mismatches as hypotheses about parameter meaning.
6. Prioritize effect families by restoration leverage, not by completeness. Recommended order: damage and healing-adjacent families first, then `UnitPropertyModifier`, then `Proxy`, then transport and teleport families, then cosmetic and unlock families.
7. Introduce a conceptual parameter interpretation layer before expanding handlers. The goal is to normalize raw `Spell4Effects` fields into named semantics per effect family such as base scalar, flat additive, stat coefficient, duration, tick interval, chained spell ID, and prerequisite hooks.
8. Decode one family at a time using three evidence sources together: decompiled client code for structure, retail sniffs for externally visible truth, and local trial-and-error for ambiguous fields. Do not consider a family solved from client decompilation alone.
9. Only decode non-effect support systems when they block a chosen family. In practice that means working into `TargetMechanics`, `ValidTargets`, `Conditions`, `CCConditions`, `AoeTargetConstraints`, and `StackGroup` as needed, instead of trying to solve the whole spell system up front.
10. Convert verified interpretations into shared server behavior in the existing central paths so one handler update improves many spells instead of introducing per-spell logic.
11. Add small regression fixtures around representative spells per decoded family to prove that one family implementation improves multiple spells and stays stable over time.
12. Repeat the same family workflow for future restoration so spell work becomes an evidence-driven throughput process rather than a one-off reverse-engineering exercise.

## Relevant Files

- `Spell.cs` - cast lifecycle, target selection, effect dispatch, and the best central hook for spell diagnostics.
- `SpellEffectHandler.cs` - current family handlers and direct raw `DataBits` consumption.
- `DamageCalculator.cs` - the clearest example of partial parameter interpretation already in place.
- `GlobalSpellManager.cs` - gametable cache and effect handler registration.
- `SpellInfo.cs` - tier-level resolved metadata including effects, telegraphs, cooldowns, and prerequisites.
- `SpellBaseInfo.cs` - base-level target mechanics, valid targets, spell class, and prerequisite wiring.
- `SpellTargetInfo.cs` - runtime per-target effect container and a good future home for decoded context.
- `Spell4EffectsEntry.cs` - the main opaque row that must be decoded by family.
- `GameTableManager.cs` - confirms the spell-related gametables already available to the runtime.
- `SpellCommandCategory.cs` - fastest local cast harness for controlled testing.
- `GameSession.cs` - packet send and receive boundary for optional sniff-comparison diagnostics.

## Verification

1. Produce a written matrix for the first priority effect families with explicit known versus unknown field mappings.
2. Confirm local casts can emit enough structured data to explain the full path from `Spell4Effects` raw fields to runtime outcome.
3. Validate one damage-family spell against sniff evidence closely enough to justify the decoded `ParameterType` and `ParameterValue` semantics.
4. Validate one non-damage family such as `Teleport`, `Proxy`, or `UnitPropertyModifier` using the same workflow.
5. Confirm that one family-level implementation change improves multiple spells without adding spell-specific hardcoding.

## Decisions

- Included: reverse-engineering workflow, effect-family prioritization, instrumentation strategy, decoder-first design, and validation loop tied to client reverse engineering plus sniff evidence.
- Excluded: implementing all effect families now, chasing full retail-perfect parity, or decoding every target and prerequisite subsystem before a chosen family needs it.
- Recommended principle: prefer family-level semantics over per-spell exceptions, and only add exceptions when evidence proves the family model is insufficient.