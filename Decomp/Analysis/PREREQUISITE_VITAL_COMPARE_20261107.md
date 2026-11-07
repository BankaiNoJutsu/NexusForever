# Prerequisite 114 compares two vitals of the unit (2026-11-07)

Evidence tier: **native body and argument order read from the disassembly**. Source/SQLite only. No retail parity claim.

Evaluator case `0x72` (`1404a2419`): `FUN_140466520(unit, ObjectId)` is called first and its result moved to `XMM6`, then `FUN_140466520(unit, Value)`; the float helper `FUN_1404a2010` is then called with `EDX = comparison`, `XMM2 = XMM6` (the first result, the **actual**) and
`XMM3` (the second result, the **expected**). So the test is `vital(ObjectId) <comparison> vital(Value)` as floats. A null unit is false. `FUN_140466520` is the vital reader type 73 already uses (ids 5 to 11 read the dwords at `+0x21c...`, 1 to 3 virtuals, others a default of 0).
Retail: 10 slots, `ObjectId` and `Value` are vital ids 7, 8 and 9 with comparisons >= and >: (7,8,>=) x2, (7,9,>=) x2, (8,7,>) x2, (9,7,>) x2, (8,9,>), (9,8,>) - a tie-break pattern between three resources.

Server: `PrerequisiteCheckVital114` reads both vitals with `IUnitEntity.TryGetVitalValue` (0 when unreadable) and uses `CompareFloat`. `PrerequisiteCheckVital114Tests` (13 cases) were written with the check (no red run); the 1,600-case Prerequisite/Vital Source/SQLite filter passes (separate scope).
