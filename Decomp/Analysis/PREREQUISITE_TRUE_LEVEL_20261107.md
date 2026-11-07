# Prerequisite 177 is the unit's base level, not a challenge lookup (2026-11-07)

Evidence tier: **native body read from the decompile of the evaluator; correction of a superseded proxy**. Source/SQLite only. No retail parity claim.

Evaluator case `0xB1` (`FUN_1404a2100`): `if (unit) FUN_1404a2090(comparison, *(int*)(unit + 0x38), Value)`. Type 1 (`Level`, case 1) reads `*(int*)(unit+0x3c)` (the scaled/effective level) and falls back to `unit+0x38` when it is 0; type 177 never reads `+0x3c`, so it is the true (unscaled) level. `ObjectId` is unused.
Retail: 220 slots, ObjectId 0 (or 6548), Value 1, 10, 40, 50: level thresholds. The previous implementation (`PrerequisiteCheckPlayerGlobalTreeLookup`: NPC target gate plus challenge activated/completed presence, labelled in its own comment as an unverified proxy) had no native support and is **superseded** together with `PlayerGlobalTreePrerequisiteHelper`.
Behaviour change: a type 177 slot no longer requires an NPC target and no longer reads the challenge manager; it compares the unit level (220 retail slots). Revert recipe: restore the two removed files and the enum attribute from git history (the commit before this one).

Server: `PrerequisiteCheckTrueLevel` (`Meets`/`MeetsUnit`, integer compare, null unit false). The server has no separate scaled level, so type 1 and type 177 read the same `Level`; they diverge only once mentoring/scaling is modelled. `PrerequisiteCheckTrueLevelTests` was written first, the superseded `PlayerGlobalTreeLookup_RequiresNpcAndChallengePresence` test was removed (it encoded the proxy). Prerequisite filter: 1,622 cases pass (Source/SQLite scope).
How it was found: a shape audit of every implemented type's retail ObjectId/Value distributions against its implemented meaning (`audit_shapes.py`), after the jump-table check found types 64 and 55 wrong.
