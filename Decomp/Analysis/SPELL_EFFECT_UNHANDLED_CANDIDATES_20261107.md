# Candidate meanings for the largest unhandled spell effect types (2026-11-07)

Inputs: the raw build 16042 `Spell4Effects.tbl` and `LocalizedText.tbl`, the imported `wildstar_client.spell4effects` table,
and spell names/flavor text from the local `jabbithole.spells` reference table (joined on spell id; third-party player-facing
text). Everything below is a candidate, not server proof. Companion to
[SPELL_EFFECT_HANDLER_CENSUS_20261106.md](SPELL_EFFECT_HANDLER_CENSUS_20261106.md). No Source change, no test.

| Effect type | Rows | What the carrying spells are | DataBits shape | Candidate | Strength |
|---|---|---|---|---|---|
| 0x7F UnlockInlaidAugment | 281 | Class augmentation passives ("Power Hitter", "Radiate", "Cheap Shot", "Laceration", "Armor Shred": each is "landing a hit/crit does X") | DataBits00 = 13..936, 244 distinct | The effect ties an augmentation passive spell to an augment id carried in DataBits00; the repo already models augmentation spells separately (`AugmentationSpellRules`, `SpellManager.Augmentations`), so the id's table is the open question | weak |
| 0x52 ChangeDisplayName | 268 | Control/convert-a-unit spells ("Hacked Robot", "Repair Probebot", "Temporary Control", "Power Minebot") | DataBits00 = 162,305..760,708; 234 of 268 values exist in `LocalizedText.tbl` | Sets the affected unit's displayed name from a localized-text id | moderate (shape + spell context; the text itself lives in the language archive, not extracted) |
| 0x47 FacilityModification | 771 | Quest/path gadget spells ("Experimental Loftite Railgun", "Collegium Tech", "Pheromone Sprayer"), DataBits02 in {1,2,3} | DataBits00 = 1..29,808 (403 distinct) | Unknown; the overlap with `MapZoneHex` ids (95%) is not evidence because those ids are dense and unrelated tables overlap similarly | inconclusive |
| 0x02 SpellCounter | 200 | not sampled | DataBits01 in {1000..5000}; DataBits00/02 look like float bit patterns | Unknown | inconclusive |
| 0x22 ChangePhase | 161 | not sampled | DataBits00/01 = 1..1,048,580 (26 distinct) | Likely a world-phase mask (values like 1048580 = 0x100004) | weak |

What is needed to implement any of these: the credited behavior (what the effect does to the target) and its network side
effects, i.e. a retail capture of the observable result or the server routine; the descriptions above only say what the
carrying spells are for. ChangeDisplayName additionally needs the entity-name update path and the localized text. Held.

Evidence tier: raw tables plus third-party descriptions; candidates only.
