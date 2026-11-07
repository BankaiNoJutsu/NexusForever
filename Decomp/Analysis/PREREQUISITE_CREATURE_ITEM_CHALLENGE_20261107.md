# Prerequisite types 38, 69, 14 and 13 (2026-11-07)

Evidence tier: **native bodies read**, with the retail value ranges and the client's Lua accessors corroborating each field; the item location masks are read from the helper's code. Source/SQLite only. No retail parity claim.

Found by reading the native bodies of the highest-volume handled types (after types 6 and 43, see [PREREQUISITE_QUEST_STATE_20261107.md](PREREQUISITE_QUEST_STATE_20261107.md)).

| Type | Native body | What it tests | Retail | What the server did |
|---|---|---|---|---|
| 38 `IsCreature` | `FUN_14049ecc0` (+0x350) | the integer at the start of the structure at `unit+0x18` (0 when none) equals `Value`; Equal / NotEqual only | 1,182 slots; `Value` is a Creature2 id (16623, 20814, 27445, ...; 0 in 7); Equal 1,020, NotEqual 162 | compared a creature-kind flag (1 or 0) with `Value`: true only for the 7 zero slots |
| 69 `ChallengeRequirement` | `FUN_14049f990` -> `FUN_14048d530` (+0x440) | the client challenge record's field at **+0x30** (the Lua `IsActivated` accessor reads it); Equal the field, NotEqual its negation, `Value` not read | 396 slots; Equal 303, NotEqual 93; `Value` 0 in 364 | compared the challenge **completion count** with `Value` |
| 14 `ItemOnCharacter` | `FUN_14049d200` (+0x128) | `FUN_1403ac590(inventory, 0x303, Value)`: an item with Item2 id `Value` in locations 0, 1, 8, 9; Equal found, NotEqual not found, `ObjectId` not read | 493 slots; `Value` is the item id in 491, `ObjectId` 0 in 468 | counted items by `ObjectId` and compared the count with `Value` |
| 13 `ItemEquipped` | `FUN_14049d1c0` (+0x120) | the same helper with mask 1 (location 0, Equipped) | 27 slots; `Value` the item id in 26 | read `ObjectId` as an equipped slot index |

The helper `FUN_1403ac590` returns 1 as soon as one container selected by a bit of the mask holds an item whose `Item2` id equals the argument; bit n is inventory location n (0 Equipped, 1 Inventory, 2 PlayerBank, 8 and 9
unmodelled, 10 SharedBank). Type 246 (`DoesNotOwnAccountItemOnCharacter`, mask 0x707) uses the same helper and was already correct in effect.

## Change

- `PrerequisiteCheckIsCreature`: `CreatureId == Value` (Equal / NotEqual, others false, a unit with no creature has id 0), `parameters.Target ?? player`.
- `PrerequisiteCheckChallengeRequirement`: `IChallengeManager.IsChallengeActivated(ObjectId)` through the flag rule; `Value` ignored.
- `PrerequisiteCheckItemOnCharacter` and `PrerequisiteCheckItemEquipped` use the new `InventoryPrerequisiteHelper.HasItem` over the Equipped and Inventory bags (13: Equipped only) with `Value` as the item id and the flag rule. Locations 8 and 9 are
  not modelled by the server.
- `CreatureItemChallengePrerequisiteTests` (24 cases) failed 12 before the change and pass now. Four existing cases encoded the old readings and were replaced (`ChallengeRequirement_ComparesChallengeCompletionCount`,
  `IsCreature_ComparesTargetEntityKind`, and the type 38 case of `UnitPrerequisiteContextTests`, which gained a subject-versus-target case). 1,403 Prerequisite Source/SQLite cases pass; the wider 6,609-case run (Prerequisite/Quest/Challenge/
  Inventory/Item) had only these 7 superseded cases failing before they were updated.

## Not changed / open

- **214 SpellBaseId** (642 slots, `FUN_14049d680`) returns false for non-player units for every comparison and otherwise tests spell base id `ObjectId` (`FUN_1403bb170`); the server matches except for the non-player case.
- **250 BaseFaction** (573 slots): compares `unit+0x110` object's virtual `+0x18()` with `Value`; the server compares `Faction1`, which is plausible (retail `Value` 166/167 are Dominion/Exile) but the virtual was not read.
- Types 7 (AchievementState), 1 (Level), 59 (SpellTier), 26 and 40 were not read in this pass.
