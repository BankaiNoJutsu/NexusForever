# Prerequisite type 129: an active spell in a spell group (2026-11-07)

Evidence tier: **native body read**; the id space of `ObjectId` is established from the retail tables (range evidence), the group manager's own body was not read. Source/SQLite only. No retail parity claim.

## Found by

The same `Value`/`ObjectId` audit as [PREREQUISITE_VEHICLE_SEAT_20261107.md](PREREQUISITE_VEHICLE_SEAT_20261107.md) and [PREREQUISITE_ACTIVE_EFFECT_ID_20261107.md](PREREQUISITE_ACTIVE_EFFECT_ID_20261107.md); type 129 has 146 slots and the
server read `Value` where the native body reads `ObjectId`.

## Native body

Evaluator case `0x81`: false for a null unit; otherwise it walks the spell list at `unit+0x15c8` (the list type 15, `UnderSpell`, walks) and, for each spell, asks the spell group manager (`DAT_140c65b70`, virtual `+0x38`)
with the spell's id (virtual `+8`) and `ObjectId`. Equal returns "some spell answered yes", NotEqual its negation, any other comparison is false. `Value` is not read.

## Id space

All 146 retail `ObjectId`s for type 129 are at most **713**, and for the sibling type 130 at most 775. The largest `SpellGroupId` value in `Spell4GroupList.tbl` is **784** while the list ids run to 1579, so the ids are
spell **group** ids (one of the `SpellGroupId00..31` values of a list) rather than list ids: random list ids would exceed 784 in about half of the slots, and none does. (`Value` is non-zero in 44 slots and is a Spell4 id
in 27 of its 29 distinct values, but is never read.)

## What the server did

`PrerequisiteCheckActiveSpellEffectOnUnit` used `Value` as a Spell4 id (tracked state, pending spell or Spell4 group-list overlap) and, with `Value` 0 (102 of 146 slots), took `ObjectId` as a Spell4 id. A group id
is not a spell id, so those slots tested an unrelated spell.

## Change

`IUnitEntity.HasActiveSpellInGroup(uint)` (new): a spell belongs to the groups listed by the `Spell4GroupList` its Spell4 row points at (`Spell.Spell4GroupListContainsGroupId`); active spells are the tracked spell states
plus the unfinished pending spells, the set `HasActiveSpell4` already uses. `HasTrackedSpellState` now shares a `TrackedSpell4Ids()` enumeration with it (same result). The check returns false for a null unit and otherwise Equal /
NotEqual over the lookup (`parameters.Target ?? player`). `PrerequisiteCheckActiveSpellGroupTests` (13 cases, including a real `UnitEntity` with a tracked spell state) failed to compile before the members existed
(the red run was a compile failure) and pass now. `PrerequisiteCheckTests.ActiveSpellEffectOnUnit_UsesTrackedSpellStateFallback` encoded the superseded Value reading and was replaced by a group-membership version;
`UnitPrerequisiteContextTests` keeps its type 129 case (explicit subject) with the group lookup mocked. 2,770 Prerequisite/Spell/Unit/Combat Source/SQLite cases pass (separate scope).

## Not changed

- **130 ActiveSpellEffectOnTarget (55 slots, case `0x82`, `FUN_140469a70`)**: same group question, but only for spells whose source unit (spell object `+0x154`) is the *target* unit, i.e. spells cast by the target. The server's tracked
  states record no caster, so it stays on its earlier behaviour; the pending spells of the target are the only caster-attributed set.
- That the manager's `+0x38` call is group membership (rather than another relation between a spell and `ObjectId`) is inferred from the id range and the type's name; its body is a virtual call that was not followed.
