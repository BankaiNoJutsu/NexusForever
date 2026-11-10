# Spell effect id words: which table each id belongs to (2026-11-10)

Evidence tier: **raw tables and names** (`Spell4Effects`, the referenced tables, `Spell4.description`, `GenericUnlockEntry.spriteIcon`). An id word is checked two ways: how many retail values exist in the
table the server assumes, and whether the name of the carrying spell agrees with the name of the referenced row. The client does not execute effects, so there is no native consumer. No retail parity claim.

## 1. Membership of DataBits00 (or the stated word) in the assumed table

| Effect | Word | Assumed table | Rows | Non-zero | In table |
|---|---|---|---:|---:|---:|
| TitleGrant (66) / TitleRevoke (67) | 00 | CharacterTitle | 51 / 1 | 51 / 1 | 51 / 1 |
| UnlockPetFlair (123) | 00 | PetFlair | 338 | 335 | 335 |
| UnlockMount (125) / UnlockVanityPet (136) / AddSpell (57) | 00 | Spell4 | 221 / 278 / 4 | 221 / 278 / 4 | 220 / 278 / 4 |
| SummonVanityPet (137) / SummonMount (36) / SummonPet (98) | 00 | Creature2 | 280 / 244 / 152 | all | all |
| SummonCreature (21) | 00 | Creature2 | 1,951 | 1,951 | 1,919 |
| Disguise (31) | 02 | Creature2 | 544 | 544 | 537 |
| ReputationModify (37) / FactionSet (9) | 00 | Faction2 | 70 / 186 | all | all |
| AchievementAdvance (92) | 00 | Achievement | 48 | 46 | 46 |
| Teleport (65) | 00 | WorldLocation2 | 389 | 382 | 381 |
| UnlockInlaidAugment (127) | 00 | EldanAugmentation | 281 | 281 | 274 |
| GiveItemToPlayer (43) | 00 | Item2 | 38 | 38 | 33 |
| GiveSchematic (38) | 00 | TradeskillSchematic2 | 1,171 | 1,171 | 385 |
| **QuestAdvanceObjective (42)** | 00 | QuestObjective | 251 | 251 | **116** |
| **LearnDyeColor (110)** | 00 | GenericUnlockEntry (Dye rows) | 223 | 223 | **161** |

GiveSchematic is not an id-space error: the 786 misses are low ids on spells named "Schematic: Solid Steel Greatsword" and similar, which predate `TradeskillSchematic2` (ids start at 32 there and those rows
are absent), while the "Blueprint: ..." spells resolve. Nothing was changed.

## 2. QuestAdvanceObjective: DataBits00 is the quest, DataBits01 the objective index
* 250 of 251 DataBits00 values are `Quest2` ids; only 116 are also `QuestObjective` ids.
* 200 of the 251 carrying spell names contain "Q" followed by DataBits00, for example Spell4 83778 "Galeras - 4 - Q5324 - Chain Lock - Activate" with DataBits00 5324, and 83750
  "SE14 - Q9686/Q9687 - Will o' Whim Collect" with two rows, 9687 and 9686.
* DataBits01 is 0 to 5 (0 on 100 rows, 1 on 58, 2 or more on 93); DataBits02 is 1 on 248 rows and 2 on 3.

The handler passed DataBits00 to `QuestManager.ObjectiveUpdate(objectiveId, progress)`. For the quest the row names that advanced nothing, and where the number is also an objective id it advanced that unrelated
objective. It now finds the active quest with that id and advances the objective at index DataBits01 by DataBits02 (1 when 0). Test: `SpellCollectionEffectTests.QuestAdvanceObjective_AdvancesTheIndexedObjectiveOfTheNamedQuest`.
Status: Implemented, inferred. Not verified: that the index is the objective's position in the quest (as `IQuestObjective.Index`) rather than another ordering, and DataBits03/04 (set on 1 and 5 rows).

## 3. LearnDyeColor: DataBits00 is the dye colour
* 222 of 223 values match a Dye `GenericUnlockEntry` by `unlockObject`; 161 match by entry id.
* Names: "Learn Dye - Red_Light1" (Spell4 46411) has DataBits00 2. The entry with `unlockObject` 2 has icon `Dye_Red_Light1`; the entry with id 2 has icon `Dye_Red_Dark2_Dull`. The same holds for
  Red_Dark2_Dull (11), Purple_Light1 (22) and Blue_Dark1 (45).

The handler unlocked the entry with the same id, teaching a different dye. It now resolves the Dye entry by colour and unlocks that entry; when the table is loaded and has no entry for the colour it
unlocks nothing. Tests: `SpellCollectionEffectTests.LearnDyeColor_ResolvesTheUnlockEntryByDyeColourNotByEntryId` and `..._WithNoEntryForTheColour_...`. Status: Implemented, inferred.

The matching cast prerequisite (type 245, "this dye is not known yet") is covered in `PREREQUISITE_CHECK_REVIEW_20261110.md`.

## 4. Not checked
Effects whose id is in a word other than 00, effects with several id words, and the ids inside Proxy variants, CCStateSet, UnitPropertyModifier and the vital families (those are enum values, not table ids).
