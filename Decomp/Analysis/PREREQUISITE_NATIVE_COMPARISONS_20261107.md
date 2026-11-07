# Prerequisite comparison handling against the native bodies (2026-11-07)

Evidence tier: **native body read** for the two corrections; the audit that found them compares retail comparison usage with the operators each server check handles. Source/SQLite tests only.
No retail parity claim.

## Audit

For every check class that does not delegate to the shared compare helper, the retail comparison values used by its prerequisite type (`Prerequisite.tbl`) were compared with the operators the class handles.
Two real gaps were found; the rest delegate to a helper (SpellTier, Datacube, Volume, Waypoint, EvalContext float) or are placeholders.

| Type | Native body | Server before | Retail usage | After |
|---|---|---|---|---|
| 11 OtherPrerequisite (evaluator case 0xb) | comparison 2: NOT of the nested prerequisite; **every other comparison: the nested result** | Equal and NotEqual only, anything else false (with a warning) | Equal 3,559, NotEqual 322, **comparison 0 in 22 slots** | NotEqual negates, all others return the nested result (both `Meets` and `MeetsUnit`) |
| 52 Path (case 0x34, `FUN_14049fd50`) | path equals Value; 1 returns it, 2 its negation, other comparisons false | Equal only | Equal 538, **NotEqual 14** | NotEqual added |

`PrerequisiteNativeComparisonTests` (15 cases) failed 3 before the change (`Path` NotEqual, `OtherPrerequisite` comparison 0 and GreaterThan) and pass now; 4,405 Prerequisite/Quest/AMP Source/SQLite cases pass
(separate scope).

## Read, left as they are

- **194 MountUsage / 195 HoverboardUsage (4 + 4 slots)** share `FUN_14049ced0`: for the local player it combines an object at `global+0x6490 → +0x16f0` (virtual `+0x78`) with a list walk testing flag bits `0x10` and
  `0x100`; Equal returns the combined result. The server returns true for both. The objects are unidentified.
- **232 Plane (10 slots, `FUN_14049e610`)** tests `(global+0x6758 & Value) != 0` (Equal: set, NotEqual: not set); the server returns true. The plane bitmask has no server state (retail `Value` 4 in 8 slots, 1 and 2 once each).
- **292 PrimalMatrixNode (24 slots, `FUN_1404a18f0`)** reads client-side node state through `FUN_1404d6a60` for the local player; the server returns false until the Primal Matrix persistence design exists (schema gate).
