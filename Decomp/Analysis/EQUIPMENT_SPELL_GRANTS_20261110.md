# Spells granted by equipped items, runes and rune sets (2026-11-10)

Evidence tier: **raw tables** (`Item2`, `ItemSpecial`, `ItemRuneInstance`, `ItemSet`, `ItemSetBonus`, `Spell4Effects`) and two tooltips. No native consumer was read and there is no capture of an
equip. Status: **Implemented, inferred.** No retail parity claim.

Before this the server never cast an on-equip spell: nothing read `ItemSpecial.spell4IdOnEquip` or `ItemSetBonus.spell4Id`.

## 1. Where the grants are in the tables

| Source | Table path | Rows |
|---|---|---|
| The item itself | `Item2.itemSpecialId00` -> `ItemSpecial.spell4IdOnEquip` (with `prerequisiteIdGeneric00`) | 1,185 of 6,035 ItemSpecial rows carry an on-equip spell; 344 of them are used by an item |
| A socketed rune | a rune is its own `Item2` row (family 33) with its own ItemSpecial: the "Rune Special - ..." spells | 220 rune items carry one |
| A rune set | the rune's `ItemRuneInstance.itemSetId` and `itemSetPower` (1 to 3); `ItemSet.itemSetBonusId00..11` -> `ItemSetBonus.requiredPower`, `spell4Id` | 982 rune items name a set; 135 of 749 bonuses grant a spell |

Tooltip witnesses that these are standing effects of wearing the item: rune special "Untouchable" (Spell4 `81880`) "Every 1s while in combat, gain an Empower"; the Esper healer rune set
bonus spells `87345` and `87346` are what the caster prerequisite of the Mental Boon rows tests for (`SPELL_EFFECT_PROXY_20261110.md` 1d).

## 2. What is implemented
`EquipmentSpellRules.GetGrantedSpells` lists the spells of the equipped items, their socketed runes, and the set bonuses whose power is reached. `SpellManager.RefreshEquipmentSpells` runs
once a second: it casts a newly granted spell on the player and, when the source is gone, removes the states the spell left (by Spell4 id) and ends it if it is still running.

Only spells the server can take away again are granted (`EquipmentSpellRules.IsSupported`): no cast time, channel or finite duration, and every row a property modifier, a proc, a visual,
or a proxy (a ticking proxy must carry effect flag 0x2, see `SPELL_EFFECT_PROXY_20261110.md` 1g). Of the on-equip spells in use that covers the gadget procs (65), the plain property
specials (12), the visuals (10) and the rune specials built from procs and proxies. It leaves out, on purpose:

* 228 "Dungeon Test" specials (large property blocks plus ItemVisualSwap rows on test gear);
* specials with effect types whose state the server cannot remove by spell (types 50, 55, 73, 112, 121, 145).

## 3. Assumed, not evidenced
* **Set power adds up across everything equipped.** The tables give each rune a power and each bonus a threshold; that the sum is taken over all equipped gear rather than per item is
  assumed.
* **`prerequisiteIdGeneric00` gates the grant.** It is evaluated on the player with the item as context. 714 ItemSpecial rows carry one; which of the two spells of the row it gates
  (on-equip, on-activate, or both) is not known.
* **Timing.** The server polls once a second, so a grant or a revoke can lag an equip by up to that long. When the retired server applied them is not known.
* **Death.** A granted spell that is no longer in place on a living player is cast again; whether the retired server kept on-equip effects through death is not known.
* `ItemSetBonus` rows without a spell (614) grant a property (`unitProperty2Id`, `scalar`, `offset`); those are **not** implemented here.

## 4. Verification
`EquipmentSpellRulesTests` (grant listing, set power thresholds, the supported-spell rule). Not run in a live session.
