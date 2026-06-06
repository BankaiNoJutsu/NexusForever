# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: tradeskills

## Tradeskill Wiki Coverage

| Wiki fact | Present | Result |
| --- | --- | --- |
| Tradeskill page exists | yes | PASS |
| Crafting redirect exists | yes | PASS |
| Tradeskills redirect exists | yes | PASS |
| Tradeskill page documents level 10/two active limit | yes | PASS |
| Tradeskill page documents gathering section | yes | PASS |
| Tradeskill page documents production section | yes | PASS |
| Tradeskill page documents hobbies section | yes | PASS |
| Tradeskill page documents other section | yes | PASS |
| Tradeskill page documents runecrafting level 15 | yes | PASS |
| Tradeskill page documents salvaging level 8 | yes | PASS |
| Profession pages exist | yes | PASS |
| Profession pages found | Architect, Armorer, Cooking, Farmer, Mining, Outfitter, Relic Hunter, Runecrafting, Salvaging, Survivalist, Tailor, Technologist, Weaponsmith | PASS |
| Schematic list pages | 34 | PASS |
| Schematic professions found | Architect, Armorer, Outfitter, Tailor, Technologist, Weaponsmith | PASS |

## Client Tradeskill Table Coverage

| Client table | Rows | Result |
| --- | --- | --- |
| Tradeskill | 13 | PASS |
| TradeskillTier | 56 | PASS |
| TradeskillTalentTier | 92 | PASS |
| TradeskillSchematic2 | 2867 | PASS |
| TradeskillMaterial | 300 | PASS |
| TradeskillMaterialCategory | 17 | PASS |
| TradeskillProficiency | 13 | PASS |
| TradeskillHarvestingInfo | 200 | PASS |
| TradeskillCatalyst | 164 | PASS |
| TradeskillCatalystOrdering | 2 | PASS |
| TradeskillBonus | 248 | PASS |
| TradeskillAdditive | 129 | PASS |
| TradeskillAchievementReward | 761 | PASS |
| TradeskillAchievementLayout | 1609 | PASS |
| MaterialData | 69 | PASS |
| MaterialType | 22 | PASS |
| MaterialSet | 12 | PASS |
| MaterialRemap | 698 | PASS |
| ReplaceableMaterialInfo | 94 | PASS |
| ItemRuneInstance | 222 | PASS |

## Tradeskill Id Coverage

| Client id | Canonical name | Server enum | Wiki page | Schematic rows | Tier rows | Result |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Weaponsmith | Weaponsmith | yes | 225 | 6 | PASS |
| 2 | Cooking | Cooking | yes | 276 | 1 | PASS |
| 12 | Armorer | Armorer | yes | 211 | 6 | PASS |
| 13 | Mining | Mining | yes | 0 | 5 | PASS |
| 14 | Outfitter | Outfitter | yes | 208 | 6 | PASS |
| 15 | Survivalist | Survivalist | yes | 0 | 5 | PASS |
| 16 | Technologist | Technologist | yes | 289 | 6 | PASS |
| 17 | Architect | Architect | yes | 337 | 6 | PASS |
| 18 | Relic Hunter | Relic_Hunter | yes | 0 | 5 | PASS |
| 19 | Fishing | Fishing | no | 2 | 2 | INFO |
| 20 | Farmer | Farmer | yes | 0 | 1 | PASS |
| 21 | Tailor | Tailor | yes | 204 | 6 | PASS |
| 22 | Runecrafting | Runecrafting | yes | 1115 | 1 | PASS |

## Client Tradeskill Data Details

- Client Tradeskill rows: 13
- Client TradeskillTier rows: 56
- Client TradeskillSchematic2 rows: 2867
- Schematics with direct item output: 2645
- Schematics with loot output: 0
- Schematics with material costs: 2142
- Schematics with parent unlocks/chains: 507
- Schematics with fail output ids: 709
- Schematics with crit output or crit bonus: 0
- Schematics with additive slots: 0
- Schematics with discovery vectors: 288
- Schematics with catalyst ordering: 1
- Materials with stat-revolution item ids: 282
- Tiers with craft/first-craft XP data: 52
- Tiers with quest XP data: 47
- Tiers with relearn cost data: 43
- Harvesting rows with prerequisites: 89
- Harvesting rows with minimap markers: 178

## Server Tradeskill Runtime Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| Character tradeskill model persists xp/active/proficiency/talents | yes | PASS |
| CharacterContext maps CharacterTradeskill rows | yes | PASS |
| Player loads and saves tradeskill state | yes | PASS |
| Player learn/drop/talent/reset methods update state | yes | PASS |
| ServerProfessionsLoad and ServerProfessionUpdate serialize profession state | yes | PASS |
| Tradeskill handlers validate client Tradeskill and bonus/tier rows | yes | PASS |
| Craft handlers validate schematic rows | yes | PASS |
| Craft handlers require active schematic tradeskill | yes | PASS |
| Craft handlers support direct item and loot outputs | yes | PASS |
| Craft handlers consume schematic materials from satchel/inventory | yes | PASS |
| Craft handlers validate and consume transient additive/catalyst items | yes | PASS |
| Supply satchel persists and sends material stack updates | yes | PASS |
| Craft finish packet serializes success, crafted schematic, item, and earned XP fields | yes | PASS |
| Fixed-recipe craft success reports non-discovery Success hot/cold state | no | FAIL |
| Player persists and serializes learned/discovered schematic state | yes | PASS |
| GiveSchematic spell records durable learned-schematic state | yes | PASS |
| Rune handlers validate rune/item requests and send sigil results | no | FAIL |

- Result: INFO, profession persistence, learn/drop/talent/reset, fixed recipe crafting, material debits, additive/catalyst item validation and consumption, satchel updates, learned schematic state, craft/quest XP, non-discovery craft-finish Success hot/cold state, and rune packet surfaces are covered; hot/cold discovery math/unlocks, harvesting, additive/catalyst output math, fail/crit outputs, and durable rune state remain blocked.

## Summary

Result: FAIL
- server tradeskill runtime surface missing marker: Fixed-recipe craft success reports non-discovery Success hot/cold state
- server tradeskill runtime surface missing marker: Rune handlers validate rune/item requests and send sigil results

Warnings:
- client Tradeskill.tbl includes Fishing, but the archived wiki tradeskill page does not expose it as a profession page
- TradeskillTier rows include relearn costs, but reset/relearn cost and cooldown semantics are not mapped
- TradeskillSchematic2 rows include fail/crit outputs; current fixed-recipe handler only returns normal direct/loot output
- TradeskillSchematic2 rows include discovery vectors; fixed-recipe finish now reports Success, but hot/cold discovery math and unlock semantics remain blocked
- client additive/catalyst data exists; item validation/consumption is covered, but additive/catalyst output math remains blocked
- client harvesting rows exist, but harvesting runtime behavior is not mapped to server code
