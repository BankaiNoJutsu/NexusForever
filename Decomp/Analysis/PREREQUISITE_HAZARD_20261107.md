# Prerequisites 111 and 113 (hazards) (2026-11-07)

Evidence tier: **native bodies read** (`FUN_1404a03e0`, `FUN_1404a04f0` -> `FUN_1404a6070`); the record fields are identified from the `Hazard` packet field order; the server mapping is **inferred**. Source/SQLite only. No retail parity claim.

## Native
The client keeps its hazards in a manager (`DAT_140c659a8`) with a tree keyed by hazard id. A record's field `+8` is the meter value and `+0x28` the Enabled flag: the `Hazard` packet order is HazardId, Type, MeterValue, MaxValue, CurrentThreshold, ProcSpell4Id, HazardUnitId, PulseTimeLeft,
UnitBased, StartsFull, Enabled, Suspended, DoNotRefill, i.e. `+0`, `+4`, `+8`, `+0xc`, `+0x10`, `+0x14`, `+0x18`, `+0x1c`, `+0x20`, `+0x24`, `+0x28`, ...
- **111** (`HazardProperty111`, 47 slots, Equal 44): the hazard `ObjectId` has a record and `+0x28` is non-zero; Equal returns it, NotEqual its negation, anything else false; with no manager Equal is false and NotEqual true.
- **113** (`HazardProperty113`, 36 slots, Values 0 to 584, comparisons >= 10, <= 12, < 8, > 5, == 1): `FUN_1404a6070` returns the record's `+8` or the constant `DAT_140b7a23c` = **-1.0** when there is no record; the value is compared with `(float)Value` through the float helper, actual first.
- **112** (`HazardProperty112`, 4 slots, `FUN_1404a0490` -> `FUN_1404a6190(…, ObjectId, Value)`) was not read.

## Server
The server already models hazards (`HazardStateCollection`, `Player.Hazard`, the `Hazard` packet with `Enabled = true` for every listed hazard). Added `HazardStateCollection.IsActive` / `GetMeterValue` (advances the meter like `BuildHazards`; -1 when absent), `IPlayer.IsHazardEnabled` / `GetHazardMeter`, and
`PrerequisiteCheckHazardProperty111` (shared boolean tail) and `PrerequisiteCheckHazardProperty113` (`CompareFloat`, Value read as signed). `HazardPrerequisiteTests` (16 cases) were written with the code (no red run); the 1,577-case Prerequisite/Hazard Source/SQLite filter passes (separate scope).

## Limits
`Enabled` is always true for a listed hazard, so 111 reduces to "the server has the hazard active"; a hazard the server suspended is still enabled (as the packet says). The meaning of the meter versus the hazard's maximum for the retail Values is not checked.
