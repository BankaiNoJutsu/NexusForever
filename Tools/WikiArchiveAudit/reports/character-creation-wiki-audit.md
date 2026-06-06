# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: character-creation

## Character Class/Race Availability

| Class | Wiki races | Client enabled races | Result |
| --- | --- | --- | --- |
| Warrior | cassian, chua, draken, granok, human, mechari, mordesh | cassian, chua, draken, granok, human, mechari, mordesh | PASS |
| Engineer | aurin, cassian, chua, granok, human, mechari, mordesh | aurin, cassian, chua, granok, human, mechari, mordesh | PASS |
| Esper | aurin, cassian, chua, human | aurin, cassian, chua, human | PASS |
| Medic | cassian, chua, granok, human, mechari, mordesh | cassian, chua, granok, human, mechari, mordesh | PASS |
| Spellslinger | aurin, cassian, chua, draken, human, mordesh | aurin, cassian, chua, draken, human, mordesh | PASS |
| Stalker | aurin, cassian, draken, human, mechari, mordesh | aurin, cassian, draken, human, mechari, mordesh | PASS |

## Class Roles

| Class | Wiki roles | Server default roles | Result |
| --- | --- | --- | --- |
| Warrior | DPS, Tank | DPS, Tank | PASS |
| Engineer | DPS, Tank | DPS, Tank | PASS |
| Esper | DPS, Healer | DPS, Healer | PASS |
| Medic | DPS, Healer | DPS, Healer | PASS |
| Spellslinger | DPS, Healer | DPS, Healer | PASS |
| Stalker | DPS, Tank | DPS, Tank | PASS |

## Class Item Proficiencies

| Class | Wiki-derived proficiencies | Client proficiencies | Result |
| --- | --- | --- | --- |
| Warrior | GreatWeapon, HeavyArmor | GreatWeapon, HeavyArmor | PASS |
| Engineer | HeavyArmor, HeavyGun | HeavyArmor, HeavyGun | PASS |
| Esper | LightArmor, Psyblade | LightArmor, Psyblade | PASS |
| Medic | MediumArmor, Resonators | MediumArmor, Resonators | PASS |
| Spellslinger | LightArmor, Pistols | LightArmor, Pistols | PASS |
| Stalker | Claws, LightArmor | Claws, MediumArmor | INFO |

## Character Start Coverage

- Supported client start keys: 24
- Seeded server start keys: 24
- Missing seeded keys: 0
- Extra seeded keys: 0
- Result: PASS

## Unsupported Enabled Client Starts

- Demo01 (1): 14 enabled rows
- Demo02 (2): 5 enabled rows
- CostumeOnly (99): 2 enabled rows
- Result: INFO, these rows are rejected by server validation until matching start locations are implemented.

## Player Paths

- Wiki paths: Explorer, Scientist, Settler, Soldier
- Server enum paths: Explorer, Scientist, Settler, Soldier
- Result: PASS

## Player Path Level Curve

| Path | Client levels | XP curve matches Soldier | Result |
| --- | --- | --- | --- |
| Soldier | 1-30 | True | PASS |
| Settler | 1-30 | True | PASS |
| Scientist | 1-30 | True | PASS |
| Explorer | 1-30 | True | PASS |

## Player Path Reward Coverage

| Path | Empty client levels | Covered client levels | Client reward kinds | Result |
| --- | --- | --- | --- | --- |
| Soldier | 1 | 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 | item, spell, title | PASS |
| Settler | 1 | 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 | item, spell, title | PASS |
| Scientist | 1 | 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 | item, scanbot, spell, title | PASS |
| Explorer | 1 | 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 | item, spell, title | PASS |

## Server Path Reward Grant Handlers

- Client level reward kinds: item, scanbot, spell, title
- Server grant handlers: item, scanbot, spell, title
- Result: PASS

## Wiki Path Reward Table Coverage

| Path | Wiki ranks | Blank wiki ranks | Empty client levels | Result |
| --- | --- | --- | --- | --- |
| Soldier | 1-30 | 1, 11, 21 | 1 | INFO |
| Settler | 1-30 | 1 | 1 | PASS |
| Scientist | 1-30 | 30 | 1 | INFO |
| Explorer | 1-30 | 30 | 1 | INFO |

## Path Ability Unlock Levels

| Ability | Path | Wiki levels | Client spell reward levels | Result |
| --- | --- | --- | --- | --- |
| Back into the Fray | Soldier | 4, 14, 27 | 4, 14, 27 | PASS |
| Bail Out! | Soldier | 5, 18, 29 | 5, 18, 29 | PASS |
| Combat Supply Drop | Soldier | 10, 23, 30 | 10, 23, 30 | PASS |
| Settler's Campfire | Settler | 4, 14, 27 | 4, 14, 27 | PASS |
| Summon: Mail Box | Settler | 5, 6 | 5 | INFO |
| Summon: Vendbot | Settler | 8, 18, 29 | 8, 18, 29 | PASS |
| Summon: Crafting Station | Settler | 23 | 23 | PASS |
| Holographic Distraction | Scientist | 4, 14, 27 | 4, 14, 27 | PASS |
| Summon Group | Scientist | 5, 18, 29 | 5, 18, 29 | PASS |
| Create Portal - Capital City | Scientist | 10, 23, 29 | 10, 23, 30 | INFO |
| Explorer's Safe Fall | Explorer | 3, 13, 26 | 4, 14, 27 | INFO |
| Jetpack Jump | Explorer | 4, 17, 28 | 5, 18, 29 | INFO |
| Translocate Beacon | Explorer | 9, 22, 30 | 10, 23, 30 | INFO |

## Path Ability Spell Handler Coverage

| Direct effect type | Path ability spell ids | Server handler | Result |
| --- | --- | --- | --- |
| VitalModifier (1) | 69661, 69664, 69665 | yes | PASS |
| ForcedMove (3) | 74979, 74980, 74981 | yes | PASS |
| Heal (10) | 69661, 69664, 69665 | yes | PASS |
| UnitPropertyModifier (11) | 57781, 57782, 57783 | yes | PASS |
| Fluff (17) | 32759, 32771, 32772, 58640, 58654, 58655, 58701, 58805, 58806, 69692, 69715, 69716, 69724, 69725, 69726, 70910, 70987, 74979, 74980, 74981 | yes | PASS |
| SummonCreature (21) | 58640, 58654, 58655, 58860, 60703, 60704, 70910, 70987 | yes | PASS |
| Proxy (26) | 32759, 32771, 32772, 69665, 69677, 69695, 69696 | yes | PASS |
| RavelSignal (81) | 69650, 69651, 69653 | yes | PASS |
| SettlerCampfire (105) | 32759, 32771, 32772 | yes | PASS |

- Result: PASS

## Path Ability Proxied Spell Handler Coverage

- One-hop proxied spell ids: 32760, 32775, 32776, 69666, 69679, 69683, 69688, 69689, 69690, 69691

| Proxied effect type | Proxied spell ids | Server handler | Result |
| --- | --- | --- | --- |
| VitalModifier (1) | 69666 | yes | PASS |
| Heal (10) | 32760, 32775, 32776, 69666 | yes | PASS |
| Fluff (17) | 32760, 32775, 32776 | yes | PASS |
| ThreatModification (46) | 69679, 69688, 69689 | yes | PASS |
| RavelSignal (81) | 69679, 69683, 69688, 69689, 69690, 69691 | yes | PASS |
| DespawnUnit (97) | 32760, 32775, 32776 | yes | PASS |
| RestedXpDecorBonus (109) | 32775, 32776 | no | INFO |

- Result: INFO, proxied effects require their own evidence before runtime implementation.

## Rested XP Spell Effect Coverage

| Effect type | Client spell ids | DataBits00 float values | ParameterValue00 values | Server handler | Result |
| --- | --- | --- | --- | --- | --- |
| ModifyRestedXP (134) | 71362, 81790, 82271 | -1.4999, 1.5, 5 | 0 | yes | PASS |
| RestedXpDecorBonus (109) | 28133, 32775, 32776, 34790, 38096, 38097, 38098, 38099, 38100, 38101, 38102, 38103, 38104, 38105, 38106, 38107, 38108, 38109, 38111, 52861, 52862, 52863, 52864, 52865, 71080 | 0.1, 0.2, 0.25, 0.4, 0.5, 0.6, 1.2, 1.4, 1.6, 1.8, 2 | 0, 0.04 | no | INFO |

- Result: INFO, direct `ModifyRestedXP` is handled; decor/aura `RestedXpDecorBonus` remains blocked.

## Summary

Result: PASS

Warnings:
- Stalker wiki proficiency wording differs from client Class.tbl.sql
- Soldier wiki blank reward ranks differ from client PathReward coverage (wiki 1, 11, 21, client 1)
- Scientist wiki blank reward ranks differ from client PathReward coverage (wiki 30, client 1)
- Explorer wiki blank reward ranks differ from client PathReward coverage (wiki 30, client 1)
- Summon: Mail Box wiki unlock levels differ from client PathReward spell levels (wiki 5, 6, client 5)
- Create Portal - Capital City wiki unlock levels differ from client PathReward spell levels (wiki 10, 23, 29, client 10, 23, 30)
- Explorer's Safe Fall wiki unlock levels differ from client PathReward spell levels (wiki 3, 13, 26, client 4, 14, 27)
- Jetpack Jump wiki unlock levels differ from client PathReward spell levels (wiki 4, 17, 28, client 5, 18, 29)
- Translocate Beacon wiki unlock levels differ from client PathReward spell levels (wiki 9, 22, 30, client 10, 23, 30)
- path ability proxied spell effects lack mapped server handlers: RestedXpDecorBonus
- RestedXpDecorBonus remains mapped-only; runtime stacking and persistence need stronger evidence
