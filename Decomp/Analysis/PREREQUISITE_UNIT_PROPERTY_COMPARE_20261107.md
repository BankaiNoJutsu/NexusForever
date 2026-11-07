# Prerequisite 115 compares two unit properties (2026-11-07)

Evidence tier: **native body read from the disassembly; server mapping to `Property` inferred**. Source/SQLite only. No retail parity claim.

Evaluator jump-table entry 115 (`1404a2461`, confirmed by dumping the int32 table at `1404a4848`; the decompile showed only `FUN_1404a2010(unit, comparison)` because it dropped the float arguments): with the unit non-null, `actual = *(float*)(unit + 0x5c4 + 8*ObjectId)` and `expected = *(float*)(unit + 0x5c4 + 8*Value)`, each 0 when the index is above `0xC4`; then `FUN_1404a2010(comparison, actual, expected)`. The neighbouring block at `1404a243f` (property `[ObjectId]` against `Value` as a float) is type 55, see [PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md](PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md), whose retail ids are also Property ids.
Retail: 18 slots, ObjectId in {0, 35, 34, 29}, Value in {2, 36, 29, 34, 35, 57}, all valid `Property` ids (largest `Property` is 196 = 0xC4, matching the array bound), which is why the array is read as the unit's properties. The rows look like rating-versus-rating tie-breaks (for example AssaultRating 35 against SupportRating 36).

Inferred, not verified: that the stride-8 array is the unit property table and that its first float is what `IWorldEntity.GetPropertyValue` returns (the second float of each entry was not read). Falsifier: the property-table reader or a capture of a type 115 row.

Server: `PrerequisiteCheckUnitProperty` uses `GetPropertyValue` for both ids (0 above 0xC4) and `CompareFloat`; 10 cases in `PrerequisiteCheckUnitPropertyTests` were written with the check (no red run); the 1,586-case Prerequisite Source/SQLite filter passes (separate scope).
