# Prerequisite 162 (PublicEventObjective2) is the objective percentage (2026-11-07)

Evidence tier: **native body read** (`FUN_1404a0a70`); the server mapping is **inferred** (the `+0xf0` mode is not modelled; the required count is the server's own completion rule). Source/SQLite only. No retail parity claim.

Native: the public event manager (`DAT_140c65980`) is asked for the objective `ObjectId`; if it exists and its virtual `+0x68` is non-zero, then when virtual `+0xf0` is 0 the value is the objective count (virtual `+0x98`) divided by the required count (virtual `+0xd0`; 0 when that is 0),
otherwise a float progress (virtual `+0x88`); the result times `100.0` is compared with `Value` through the float helper `FUN_1404a2010`, actual first. A missing objective gives 0. Retail: 8 slots, Values 100, 50, 30, 60, comparisons >= x4 and == x4.

Server: `PublicEventPrerequisiteHelper.GetObjectivePercent` (the objective as in type 123, `count * 100 / required`, where required is the dynamic maximum when set and the table `Count` otherwise, 0 for no objective or no maximum) and `PrerequisiteCheckPublicEventObjective162`
using `CompareFloat`. `PrerequisiteCheckPublicEventObjectivePercentTests` (14 cases) were written with the check (no red run); the 3,405-case Prerequisite/PublicEvent Source/SQLite filter passes (separate scope).

## Other bodies read in the same batch (no code change)
Details are in the gate table ([PREREQUISITE_REMAINING_TYPE_GATES_20261107.md](PREREQUISITE_REMAINING_TYPE_GATES_20261107.md)): 107 and 119 (participation slot bit), 122 (event participation record), 199 (event state `+0x1d0 == 1`), 200 (objective mask), 149 (`+0x1e4 || +0x2ac`).
