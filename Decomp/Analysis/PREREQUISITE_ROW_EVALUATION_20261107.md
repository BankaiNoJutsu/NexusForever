# Prerequisite row evaluation (2026-11-07)

Evidence tier: **native body read** (Ghidra: row evaluator `FUN_1404a1ca0`, row builder `FUN_1404a5190`, table walk `FUN_1404a5240`), implementation checked by Source/SQLite tests written from that reading.
No retail witness; the affected rows are authoring accidents, so a retail capture of one would be the only external confirmation.

## What the client does

- `FUN_1404a5240` walks every row of `Prerequisite.tbl` and builds a 0x20-byte object for it through `FUN_1404a5190`: id, flags, and up to three slot pointers.
- `FUN_1404a5190` copies the slots **in order and returns at the first slot whose type is 0**. A slot is `{type, comparison, objectId, value}` (record words 2..4, 5..7, 8..10, 11..13).
- `FUN_1404a1ca0(id)` finds the row (absent id: false), then evaluates the built slots in order. Flags bit 0 clear means AND, set means OR. It stops at the first decisive slot (a false for AND, a true for OR) unless a debug word is set;
  an AND with no built slots is true and an OR with no built slots is false.

## What the server did

`PrerequisiteManager.Meets` and `MeetsUnit` skipped an empty type slot (`continue`) and still evaluated the slots after it. Retail flags are only 0 and 1, so the mode handling already matched. Eight of 32,131 rows have a type after an empty slot:

| Row | Flags | Types | Native result | Server before |
|---|---|---|---|---|
| 3900, 5540 | AND | 6, 0, 6 | slot 0 only | slot 0 and slot 2 |
| 3962 | AND | 0, 15, 0 | true (no slots) | type 15 |
| 3963 | AND | 0, 0, 15 | true | type 15 |
| 4244 | AND | 0, 2, 0 | true | type 2 |
| 5601 | AND | 0, 15, 15 | true | types 15, 15 |
| 6258 | AND | 0, 6, 0 | true | type 6 |
| 4460 | OR | 0, 14, 0 | false (no slots) | type 14 |

## Change

`PrerequisiteManager` stops at the first `PrerequisiteType.None` slot in `Meets` (AND and OR) and `MeetsUnit`. `PrerequisiteManagerTests.SlotsAfterAnEmptyTypeSlotAreNeverEvaluated` (6 cases, each run through `Meets` and
`MeetsUnit`, counting check invocations) failed 5 of 6 before the change (the sixth, a no-gap control, passes either way after correcting its expected count) and passes now. 5,543 Prerequisite/Quest/Spell/AMP/Challenge
Source/SQLite cases pass (separate scope).

## Not changed / open

- The client's debug word (`param_1+0x6c`) that disables short-circuiting only changes how many slots are evaluated, not the result; it is not modelled.
- Which gameplay consumes the eight rows was not traced; the six always-true rows relax a gate and the one always-false OR row (4460) tightens one.
