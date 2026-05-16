# Spell Evidence Capture Loop

This document standardizes the spell reverse-engineering loop for one fixture at a time.
It exists to keep spell evidence structured across both the new runtime exporter and the
manual notes still needed for sniff or content-context comparison.

## Goal

Every spell investigation should produce one evidence bundle that ties together:

- the source fixture or SQL witness
- the decoded spell structure from `/spell inspect4`
- the observed runtime behavior from `/spell capture4`, `/spell diag4`, `/spell cast4`, or an equivalent trigger path
- the diagnostic traces, packet observations, and state deltas that support or reject a hypothesis

## Suggested Bundle Location

Command-driven casts can now export a structured runtime artifact automatically under:

`artifacts/verify/spell-evidence/<timestamp>-spell4-<spell4Id>-cast-<castingId>-<short-name>.json`

The preferred trigger paths are:

- `/spell capture4 <spell4Id>` for a structured runtime evidence export
- `/spell diag4 <spell4Id>` when the same fixture also needs the guarded `Server07FB` blocked-immunity diagnostic send
- `/spell capturenext` before a normal client-driven spell, item use, activate-unit cast, or rapid-transport request when the client context token and request source must be preserved in the artifact
- `/spell diagnext` for the same real-client path when blocked-immunity diagnostic broadcasts also matter

Keep a companion working note per fixture under:

`artifacts/verify/spell-evidence/<spell4Id>-<short-name>.md`

If a fixture is temporary or exploratory, keep the same fields in a scratch note, then promote the stable result into the tracker documents.

## Minimum Bundle Contents

Each bundle should include:

1. Fixture identity
   - `Spell4Id`
   - spell name or short label
   - source witness such as `world_context_queries.sql`, `Spell Reverse Engineering Runtime Candidates.md`, or a placed creature/object join
2. Inspect output summary
   - target mechanics
   - valid-target flags
   - effect families
   - raw `DataBits` and decoded semantics worth validating
3. Cast or trigger context
   - caster type
   - target type
   - whether the path was `/spell capture4`, `/spell diag4`, `/spell cast4`, `ClientActivateUnitCast`, item use, or another trigger
4. Diagnostics and packet notes
   - the trace categories observed
   - the runtime evidence JSON artifact path when available
   - any spell broadcast family notes
   - combat log or packet behavior relevant to the hypothesis
5. State delta
   - health, shield, buffs, CC states, summoned units, despawns, teleports, or interaction state changes
6. Confidence outcome
   - Observed, Correlated, Mapped, Verified, Implemented, or Blocked
   - open questions that still block promotion

## Repeatable Loop

1. Choose one fixture from `Tools/SpellReverseEngineering/world_context_queries.sql`, `Spell Reverse Engineering Runtime Candidates.md`, or a focused issue.
2. Create or update the bundle note before running commands so the hypothesis and target surface are explicit.
3. Run `/spell inspect4 <spell4Id>` and record only the fields that matter to the current question.
4. Prefer `/spell capture4 <spell4Id>` for command-driven fixtures, `/spell diag4 <spell4Id>` when blocked-immunity follow-up packets matter, or `/spell capturenext` and `/spell diagnext` immediately before the real client action when client-originated context needs to survive into the exported artifact. Use `/spell cast4 <spell4Id>` or the narrowest real trigger path only when the structured exporter is not the right surface.
5. Record the relevant diagnostics, packet observations, combat logs, state changes, and exported JSON path.
6. Compare the result with client structure, sniff evidence, and SQL shape.
7. Promote the finding only when at least two evidence sources agree on the behavior.

## Promotion Gate

Use the existing Evidence Ladder language:

- Observed: one anchor, no safe behavior change
- Correlated: anchor lines up with table, packet, or runtime surface
- Mapped: structure is clear enough for a durable label or tracker note
- Verified: runtime, client, sniff, or table evidence agree strongly enough to implement
- Implemented: code changed and a focused verification passed
- Blocked: still missing safe semantics or target surface

## Near-Term Implementation Target

Runtime-side spell evidence export now exists for opt-in command-driven casts.
The next expansion target is to broaden capture beyond explicit spell commands and fold sniff metadata into the same bundle shape.