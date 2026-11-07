# Prerequisite types 59 and 280 count active spell stacks, not ability tiers (2026-11-07)

Evidence tier: **native body read** (disassembly of `FUN_1404a4f60`) plus **correlation** for what the summed field is. Source/SQLite only. No retail parity claim; the server stack model is **inferred**.

## Native
Evaluator case `0x3b` (type 59) calls `FUN_1404a4f60(unit, comparison, ObjectId, Value)`: false for a null unit; otherwise walks the list at `unit+0x15c8` (next at node `+0x18`, the same active-spell list type 129 and type 15 walk), calls virtual `+8` of the object at node `+0x38` (the spell id), and for every node whose id equals `ObjectId` adds the dword at node `+0x50`; the sum is compared with `Value` through the integer helper `FUN_1404a2090`. Case `0x118` (type 280) requires the same result for both units (`param_2[0]` and `param_2[1]`). The sibling `FUN_1404a4fe0` (type 102) walks `unit+0x15d0` instead; type 60 uses a different body and was not changed.

## Why stacks, not tiers
Retail type 59: 909 slots, 260 distinct `ObjectId`s of which 253 are Spell4 ids (only 104 are Spell4Base ids, so it is the tiered-spell id space, not the base). `Value` runs 0..60 (0 x55, 1 x110, 2 x142, 3 x134, ... 10 x53, 40 x13, 60 x1); ability tiers stop at 9. For several spells `Value` equals the cap of the Spell4StackGroup their row points at (group 758 cap 10, group 713 cap 16, group 582 cap 12) but not for all (group 330 cap 4 with Values 10..20; group 157 cap 2 with Values to 6), so "node `+0x50` is a stack count" is **Correlated, not read**.

## Server before / change
`SpellPrerequisiteHelper.MeetsSpellTier`: the player's ability-book tier of the spell's base spell. Now `IUnitEntity.GetActiveSpellStackCount(spell4Id)` (distinct tracked castings of the Spell4 id plus unfinished pending spells with it); `PrerequisiteCheckSpellTier` compares it with `Value` on `parameters.Target ?? player` (null unit false), `PrerequisiteCheckSpellTierOnCasterAndTarget` requires both units (missing target false). Type 60 still uses the old helper. `UnitEntity.TrackedSpell4Ids` is now derived from a new `TrackedSpellCastings` enumeration (same ids).

## Limits
The server collapses a re-applied (spell, effect entry) into one state, so counts above 1 arise only from different effect entries or castings; a real per-node stack counter does not exist. Spells the server does not track as a unit state report 0 stacks.

## Tests
`PrerequisiteCheckSpellStackCountTests` (19 cases): red as a compile failure first (constructor/`MeetsUnit` missing); one case then failed (my own test reused an effect entry id) and was corrected. `PrerequisiteCheckTests.SpellTier_ComparesSpellManagerTier` encoded the superseded tier reading and was removed. 2,705 Prerequisite/Spell/Unit Source/SQLite cases pass (separate scope).
