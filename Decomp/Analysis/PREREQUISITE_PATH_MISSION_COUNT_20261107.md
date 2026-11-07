# Prerequisite 110 (PlayerPathMissionCount) and the mission field mapping (2026-11-07)

Evidence tier: **native bodies read**; the server mapping is **inferred** (definition types 5 and 6 and the missing-mission case are not read). Source/SQLite only. No retail parity claim.

## Field mapping (corrects [PREREQUISITE_PATH_MISSION_ACTIVE_20261107.md](PREREQUISITE_PATH_MISSION_ACTIVE_20261107.md))
The mission object is built from a `ServerPathMissionUpdate` entry by the factory `FUN_14056dce0` -> constructor `FUN_14056b0f0(this, template, value1, value2, value3)`. The packet order on the server is `PathMissionId`, `Completed`, `ProgressCount`, `ProgressData`, so:
`+0x24` (IsComplete, virtual `+0x38`) = **Completed**, `+0x1c` (virtual `+0x40`; popcount in `+0x48`) = **ProgressCount**, `+0x20` (virtual `+0x58`) = **ProgressData**; `+0x18` is the separately set Activate flag. The earlier record called `+0x1c` "progress" and `+0x20` "flags";
`+0x20` is the **ProgressData** word (bit 0 is what the state virtual `+0xd0` and the type 4 to 7 test read), not an independent flags field.

## Type 110
Evaluator case `0x6e` calls `FUN_1404a00e0(…, comparison, ObjectId, Value)`: if the path service exists and `FUN_1403d7bc0(service, ObjectId)` returns the mission, then virtual `+0x30` decides: result 0 gives virtual `+0x40` (the ProgressCount), otherwise `+0x48` (the population
count of the ProgressCount); the value is passed to the integer compare helper. `+0x30` (`FUN_14056b260`) is true for definition types 3, 0xd, 0xe, 0x11, 0x14, 0x17, 0x19, 0x1a and 0x1b, depends on a table row for types 5 and 6 (`FUN_140223380(definition + 0x14)`, field `+0x10 - 1 < 2`)
and is false otherwise. Retail: 47 slots, `Value` counts (3, 4, 6, 30, 60) and percentages (33, 66, 100), comparisons == 26, >= 7, > 6, <= 5, < 3.

## Server
`IPathManager.TryGetMissionProgress(id, out progressCount)` (new, reads the runtime state's `ProgressCount`) and `PrerequisiteCheckPlayerPathMissionCount`: popcount for the listed definition types (from `PathMission.tbl`'s `PathMissionTypeEnum`), the plain word otherwise, then the shared integer compare (progress first).
A mission the server does not have counts as 0 (the native body's behaviour when the lookup fails is lost in the decompile). `PrerequisiteCheckPlayerPathMissionCountTests` (16 cases) was written together with the check (no red run); the 1,855-case Prerequisite/Path Source/SQLite filter passes (separate scope).

## Limits
- Definition types 5 and 6 are treated as plain counts (the table row behind `FUN_140223380` was not read).
- Whether the server's `ProgressCount` has the same meaning per mission type as the client's (for example a percentage versus a count) is not verified; the server keeps its own progress representation per mission type.
