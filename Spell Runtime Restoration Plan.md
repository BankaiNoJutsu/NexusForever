# Spell Runtime Restoration Plan

## Summary
- Keep the first implementation milestone focused on the generic spell runtime, but use Evil from the Ether as the later vertical-slice validation target because the research report identifies it as the best public near-retail slice.
- Treat [deep-research-report.md](</I:/GIT/NexusForever/deep-research-report.md>) as an added planning input: the main blockers are spell/effect dispatch, prerequisites, combat formula order, unknown spell/ability messages, and NavMap/LoS.
- Use repo data first. Ask for Discord/sniff/client-decompile artifacts only for specific fixture gaps.

## Key Changes
- Build a behavior-first validation loop before expanding handlers:
  - Normalize `/spell inspect4` and `/spell cast4` output into repeatable fixture notes.
  - Add a small spell fixture harness or command-log format that captures raw `Spell4Effects`, decoded semantics, target selection, packet/combat-log output, and state deltas.
  - Keep decompiler notes and Discord claims as hypotheses until a fixture proves them.
- Prioritize core runtime work in this order:
  - Targeting/valid-target diagnostics and enforcement for proven masks only.
  - Damage/heal/vitals formula order, including Armor Pierce and shield/absorb ordering.
  - Proc/event dispatch with recursion guards and conservative target routing.
  - Buff/debuff lifecycle: stack groups, refresh, duration removal, persistence checks.
  - Unknown spell/ability message decoding only where fixtures expose packet mismatch.
  - Diagnostic-only `RavelSignal` handler until a receiver/script surface is proven.
- Add spatial work as a parallel research track, not a blocker for the first spell-core patch:
  - Confirm current `.nfmap` terrain/grid capabilities.
  - Search for public/private NavMap or LoS artifacts by `NavMap`, `LoS`, `mesh extractor`, and Rawaho references.
  - Plan server-side LoS/pathing around extracted collision/nav primitives, not hardcoded encounter exceptions.

## Interfaces
- Add named internal types only where they reduce ambiguity:
  - `SpellProcEvent`
  - `ProcEventContext`
  - a spell fixture/log model for raw row, decoded row, selected targets, emitted packets/logs, and state changes
- Extend `IUnitEntity` only for generic spell-owned state and proc notification. Avoid creating per-spell or per-instance APIs.
- Keep handler behavior family-based; no spell-specific hardcoding unless evidence proves a retail exception.

## Test Plan
- Build: `dotnet build Source\NexusForever.slnx --no-restore`.
- Fixture checks:
  - Proc holders: `7116`, `4876`, `4046`, `4075`, `4191`, `1024`, `1316`, `39063`.
  - Unit state/busy: `3946`, `3955`, `4360`, `35174`, `41576`, `36188`, `51510`.
  - Formula/shield/absorb fixtures from the existing evidence matrix.
  - Top placed `RavelSignal` rows from `world_context_queries.sql`.
- Acceptance:
  - Unknown behavior logs evidence without mutating state.
  - Proven behavior mutates state through generic handlers.
  - Existing spell diagnostics remain usable.
  - No regression to current damage/heal/proxy/property behavior.

## Assumptions
- First milestone remains Spell Core, per your choice.
- Evil from the Ether is the later retail-adjacent validation slice, not the first code target.
- LoS/pathing is required for near-retail PvE, but first implementation can proceed with diagnostics and fixture selection while spatial artifacts are located.
- The current dirty worktree is preserved; no unrelated changes are reverted.
