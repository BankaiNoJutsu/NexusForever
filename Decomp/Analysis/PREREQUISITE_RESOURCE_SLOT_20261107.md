# Prerequisite 64 is a resource-slot compare, not a path level (2026-11-07)

Evidence tier: **native body read from the disassembly; correction of a superseded reading**. Source/SQLite only. No retail parity claim.

Method: the evaluator `FUN_1404a2100` switches on `type - 1` through the int32 table at `1404a4848` (offsets from the image base; dumped with `I:\tools\scripts\DumpInts.java`, 295 entries). Entry 64 targets `1404a24ad`:
`unit = *param_2; if (!unit) return false; idx = (int)ObjectId; actual = idx < 0xb ? *(float*)(unit + 0x21c + 4*idx) : 0.0f; expected = (float)(int)Value; FUN_1404a2010(comparison, actual, expected)`.
The dword array at `unit+0x21c` is the one `FUN_140466520` exposes as vitals 5 to 0xb, 0x10, 0x1b, 0x1c, 0x1d (slots 0 to 10), which are `Resource0` to `Resource10` in the server `Vital` enum.

Retail: 107 slots, ObjectId 1 to 4 (4 is not a path type; paths are 0 to 3), Value 0, 1, 25, 50, 60, 100: resource thresholds. The earlier `PathTypeLevel` reading (path type ObjectId, path level Value via `PathLevel.tbl`) had no native body behind it and is **superseded**; the separate `PathLevel` check is unchanged.
Behaviour change: slots with ObjectId 1 to 4 now compare the unit's `Resource1..Resource4` vitals with Value instead of a path level (an unreadable vital counts 0). Revert recipe: restore `PrerequisiteCheckPathTypeLevel` from git history (the commit before this one) and the enum name.

Server: `PrerequisiteCheckResourceSlot` (enum renamed `PathTypeLevel` to `ResourceSlot`), `CompareFloat`. `PrerequisiteCheckResourceSlotTests` was written first (red on the missing class), then green. Prerequisite filter: 1,613 cases pass (separate Source/SQLite scope).
Not verified: which retail resource each slot is per class (Engineer kinetic cell is plausible); a retail capture of a slot 1 to 4 row.
