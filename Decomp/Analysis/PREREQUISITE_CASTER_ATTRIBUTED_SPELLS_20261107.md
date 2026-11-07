# Prerequisite types 50, 60 and 130 only count spells cast by the target (2026-11-07)

Evidence tier: **native bodies read** (`FUN_1404699f0`, `FUN_14046a110`, `FUN_140469a70` and evaluator cases `0x32`, `0x3c`, `0x82`). Source/SQLite only. The server's caster attribution and stack model are **inferred**. No retail parity claim.

## Native
All three walk the first unit's active-spell list (`unit+0x15c8`) and test the spell object's source unit (`+0x154`) against the second unit's id (`param_2[1]+8`):
- **60 (126 slots)**: sums node `+0x50` of spells with id `ObjectId` cast by the target; compares with `Value` (integer helper). Retail `Value` 1..60+, comparisons `==` 47, `<` 41, `>=` 28, `>` 10.
- **50 (450 slots)**: is there a spell with id **`Value`** cast by the target (Equal found, NotEqual not found, others false). `ObjectId` is not read.
- **130 (55 slots)**: is some such spell in spell group `ObjectId` (manager virtual `+0x38`, as type 129). `Value` not read.
- No second unit: Equal false, NotEqual true (60: other comparisons false). First unit null: false.

## Server before
50 asked whether the **target** held spell `Value` (or `ObjectId` when `Value` was 0), 130 read `Value` as a Spell4 id on the caster, 60 reused the ability-tier helper; none looked at who cast a spell, because tracked spell states carry only a casting id.

## Change
`IUnitEntity.RegisterSpellCaster(castingId, casterGuid)` (called from `Spell.ExecuteEffect` for every unit target before the handler creates states), `GetActiveSpellStackCount(spell4Id, casterGuid)` and `HasActiveSpellInGroup(groupId, casterGuid)` (distinct tracked castings registered to that caster; the unit's own unfinished pending spells only count for self-casts). Checks 50, 60 and 130 rewritten to the native shape. The registry drops entries without a live state once it exceeds 256.

## Limits
- States created through paths that bypass `Spell.ExecuteEffect` (none known) have no caster and never match.
- The unit's persistent-effect list and node `+0x50` count are modelled as before (see [PREREQUISITE_SPELL_STACK_COUNT_20261107.md](PREREQUISITE_SPELL_STACK_COUNT_20261107.md)); `+0x154` being the casting unit follows from the type 130 and 50 usage and the names, not from a field write.
- Type 103 (effect-node `+0x20` per-effect target) is still not modelled.

## Tests
`PrerequisiteCheckCasterAttributedSpellTests` (29 cases, including a real `UnitEntity` registry); the red run was a compile failure (members missing). `PrerequisiteCheckTests.UnderSpellOnTarget_UsesTargetActiveSpellState` encoded the superseded reading and was removed; the type 50 case of `UnitPrerequisiteContextTests` now expects the subject to hold the target's spell. 2,742 Prerequisite/Spell/Unit Source/SQLite cases pass (separate scope).
