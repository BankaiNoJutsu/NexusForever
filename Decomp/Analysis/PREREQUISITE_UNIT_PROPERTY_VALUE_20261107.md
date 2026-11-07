# Prerequisite 55 is a unit property compare, not a currency amount (2026-11-07)

Evidence tier: **native body read from the disassembly; correction of a superseded reading**. Source/SQLite only. No retail parity claim.

Evaluator jump-table entry 55 targets `1404a243f` (table method in `PREREQUISITE_RESOURCE_SLOT_20261107.md`): with the unit non-null, `actual = ObjectId <= 0xC4 ? *(float*)(unit + 0x5c4 + 8*ObjectId) : 0.0f`, `expected = (float)(int)Value`, then `FUN_1404a2010(comparison, actual, expected)`.
Retail: 4 slots, ObjectId 9 (ResourceMax0), 41 (ShieldCapacityMax), 100 (MoveSpeedMultiplier), Value 1000, 1, 0. These are `Property` ids (there is no currency type 41 or 100; `CurrencyType` 9 is `WinterfestColdCast`), so the previous `Currency` reading is superseded. This also supports the array mapping used for type 115 ([PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md](PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md)): the 0xC4 bound equals the largest `Property` (196) and the retail ids here are float-valued properties.

Server: `PrerequisiteCheckUnitPropertyValue` (enum `Currency` renamed `UnitPropertyValue`); `PrerequisiteCheckCurrency` now serves type 240 only (240's native slot is a vtable call `+0x2e8` with the comparison and ObjectId, unread, so that mapping is unchanged and unverified). `PrerequisiteCheckUnitPropertyValueTests` was written first, then green. Prerequisite filter: 1,613 cases pass (Source/SQLite scope).
Not verified: that the first float of each array entry equals `GetPropertyValue` (still inferred); a retail row capture. Behaviour changes only for the 4 retail type 55 slots.
