# Prerequisite type 102: an active spell effect id on the unit (2026-11-07)

Evidence tier: **native body read; the id space is established from the retail tables**, not from the class of the list's elements. Source/SQLite only. No retail parity claim.

## Found by

The `Value`/`ObjectId` audit of native argument use against the server checks (see [PREREQUISITE_VEHICLE_SEAT_20261107.md](PREREQUISITE_VEHICLE_SEAT_20261107.md)); type 102 ranked high because it has 353 slots and
the server read `Value` while the native body reads `ObjectId`.

## Native body

Evaluator case `0x66` calls `FUN_1404a4fe0(unit, comparison, ObjectId)`: false for a null unit; otherwise it walks the linked list at `unit+0x15d0` (next pointer at node `+0x30`, 6 qwords) and tests whether the node's
virtual `+0x30` equals `ObjectId`. Equal returns the match, NotEqual its negation, every other comparison is false. `Value` is not read. (Type 15, `UnderSpell`, walks the list at `unit+0x15c8` the same way and
compares virtual `+8` with `Value`; its ids are Spell4 ids, 2,450 of 2,514 distinct Values.)

## Id space

290 distinct retail `ObjectId`s for type 102: **226 are `Spell4Effects` ids** (78%), 34 are `Spell4` ids, 16 `Spell4Base` ids and none is a `SpellCoolDown` id (202 rows). For type 15, `Spell4` hits 97% and `Spell4Effects` 60%.
So the list at `unit+0x15d0` holds the unit's active spell *effect* instances and virtual `+0x30` is the effect row id. Retail: 353 slots, NotEqual 249, Equal 104, `Value` 0 in 254 (non-zero in 99, unread).

## What the server did

`PrerequisiteCheckSpellCooldownNodeOnUnit` (the enum name is an older guess) took `Value` as a `SpellCoolDown` id, returned false for any unit that was not a player, and compared "is on cooldown" with 1. With `Value` 0 in
254 of 353 slots it tested node id 0, and the id space was wrong for all of them.

## Change

`IUnitEntity.HasActiveSpellEffectId(uint)` (new; `UnitEntity` walks its pending spells' persistent effects through the new `Spell.HasPersistentEffectWithId`, the same lifetime rule the existing
`HasPersistentEffectOfType` uses) and a rewritten check (null unit false; Equal active; NotEqual not active; other comparisons false; `parameters.Target ?? player`). `PrerequisiteCheckActiveEffectIdTests` (9 cases)
failed to compile before the interface member existed and pass now (the red run was a compile failure); 2,054 Prerequisite/Spell/Cooldown Source/SQLite cases pass in a separate scope. The cooldown-node helpers used by
types 103 and 104 are unchanged.

## Not changed

- **103 SpellCooldownNodeOnTarget (36 slots) and 104 (3)** ignore `ObjectId` in the server while the native bodies (`FUN_14046a190`, `FUN_14046a210`) use it; they were not read in this pass.
- **129 ActiveSpellEffectOnUnit (146 slots)** was read afterwards and fixed (a spell group id on the unit's spell list, [PREREQUISITE_ACTIVE_SPELL_GROUP_20261107.md](PREREQUISITE_ACTIVE_SPELL_GROUP_20261107.md)); **130** (spells cast by the target) is still on its earlier behaviour.
- The server's active-effect lookup covers persistent effects with a lifetime event; instantaneous effects are not instances in the native list either, but this was not verified.
