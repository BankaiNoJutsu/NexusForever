# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: achievements

## Achievement Wiki Page Coverage

- Achievement infobox pages: 726
- Pages with description fields: 725
- Pages with criteria fields: 204
- Pages with title reward fields: 47
- Pages with series sections: 204
- Wiki achievement titles matched to DataMapping client names: 474/689

## Client Achievement Table Coverage

| Client table | Rows | Result |
| --- | --- | --- |
| Achievement | 4943 | PASS |
| AchievementChecklist | 6568 | PASS |
| AchievementCategory | 273 | PASS |
| AchievementGroup | 9 | PASS |
| AchievementSubGroup | 8 | PASS |
| AchievementText | 55 | PASS |
| CharacterTitle | 427 | PASS |
| CharacterTitleCategory | 20 | PASS |
| Challenge | 643 | PASS |
| ChallengeTier | 1684 | PASS |

## Client Achievement Data Details

- Client Achievement rows: 4943
- Client AchievementChecklist rows: 6568
- Achievements with checklist rows: 1638
- Achievements with character title rewards: 228
- Achievements with parent tier id: 666
- Achievements with prerequisite fields: 1657
- Checklist rows with prerequisite fields: 5
- Guild achievement rows: 296
- Steam achievement name rows: 244

## Achievement Type Coverage

| Type id | Server enum | Client rows | Runtime trigger | Result |
| --- | --- | --- | --- | --- |
| 1 | KillCreatureEntry | 98 | yes | PASS |
| 2 | KillCreatureGroup | 426 | yes | PASS |
| 3 | QuestComplete | 10 | yes | PASS |
| 5 | KillCreatureChecklist | 135 | yes | PASS |
| 6 | QuestCompleteChecklist | 436 | yes | PASS |
| 7 | AchievementComplete | 233 | yes | PASS |
| 8 | EnterWorldZone | 7 | yes | PASS |
| 9 | unmapped | 88 | blocked | INFO |
| 12 | unmapped | 842 | blocked | INFO |
| 13 | unmapped | 19 | blocked | INFO |
| 14 | unmapped | 2 | blocked | INFO |
| 15 | DiscoverObject | 29 | yes | PASS |
| 16 | ActivateCreature | 21 | yes | PASS |
| 22 | unmapped | 1 | blocked | INFO |
| 26 | unmapped | 5 | blocked | INFO |
| 33 | unmapped | 44 | blocked | INFO |
| 35 | CraftItem | 797 | yes | PASS |
| 37 | TradeskillTier | 45 | yes | PASS |
| 38 | CostumeUnlock | 19 | yes | PASS |
| 40 | CraftItemChecklist | 215 | yes | PASS |
| 42 | ReputationLevel | 26 | yes | PASS |
| 44 | unmapped | 161 | blocked | INFO |
| 45 | QuestCompleteChecklistCount | 23 | yes | PASS |
| 46 | unmapped | 3 | blocked | INFO |
| 53 | MapComplete | 22 | yes | PASS |
| 54 | CharacterLevel | 21 | yes | PASS |
| 55 | unmapped | 2 | blocked | INFO |
| 56 | TitleEarned | 3 | yes | PASS |
| 57 | unmapped | 124 | blocked | INFO |
| 61 | unmapped | 70 | blocked | INFO |
| 62 | PathMission | 12 | blocked | PASS |
| 63 | PathMissionType | 47 | blocked | PASS |
| 64 | PathLevel | 4 | yes | PASS |
| 65 | unmapped | 27 | blocked | INFO |
| 66 | unmapped | 12 | blocked | INFO |
| 67 | GuildBelovedReputation | 3 | yes | PASS |
| 68 | unmapped | 5 | blocked | INFO |
| 72 | unmapped | 7 | blocked | INFO |
| 73 | unmapped | 4 | blocked | INFO |
| 75 | CurrencyEarned | 5 | yes | PASS |
| 76 | unmapped | 73 | blocked | INFO |
| 77 | unmapped | 33 | blocked | INFO |
| 79 | unmapped | 37 | blocked | INFO |
| 80 | ItemConsume | 129 | yes | PASS |
| 82 | FindSecretStash | 18 | yes | PASS |
| 83 | unmapped | 4 | blocked | INFO |
| 85 | unmapped | 1 | blocked | INFO |
| 86 | unmapped | 3 | blocked | INFO |
| 87 | unmapped | 16 | blocked | INFO |
| 88 | unmapped | 150 | blocked | INFO |
| 89 | unmapped | 4 | blocked | INFO |
| 92 | GuildMaxPathLevel | 3 | yes | PASS |
| 94 | unmapped | 20 | blocked | INFO |
| 96 | unmapped | 120 | blocked | INFO |
| 97 | DuelParticipate | 3 | yes | PASS |
| 98 | DuelWin | 3 | yes | PASS |
| 101 | unmapped | 2 | blocked | INFO |
| 102 | unmapped | 16 | blocked | INFO |
| 103 | ClassLevel50 | 24 | yes | PASS |
| 104 | EmoteTargetCreature | 12 | yes | PASS |
| 105 | CriticalDeathblow | 10 | yes | PASS |
| 106 | GuildOrCircleJoin | 2 | yes | PASS |
| 107 | GroupJoin | 1 | yes | PASS |
| 108 | FriendAdd | 1 | yes | PASS |
| 109 | unmapped | 1 | blocked | INFO |
| 111 | HousingPlugPlace | 4 | yes | PASS |
| 112 | unmapped | 4 | blocked | INFO |
| 113 | HousingDecorPurchase | 4 | yes | PASS |
| 114 | unmapped | 2 | blocked | INFO |
| 116 | RealmFirst | 86 | yes | PASS |
| 118 | unmapped | 1 | blocked | INFO |
| 121 | unmapped | 24 | blocked | INFO |
| 129 | unmapped | 3 | blocked | INFO |
| 130 | ContractQualityComplete | 2 | yes | PASS |
| 131 | ContractTypeComplete | 2 | yes | PASS |
| 133 | ContractComplete | 7 | yes | PASS |
| 137 | AccountCurrencyEarned | 20 | yes | PASS |
| 140 | unmapped | 1 | blocked | INFO |
| 141 | CostumeSetUnlock | 10 | yes | PASS |
| 143 | PublicEventObjectiveComplete | 22 | yes | PASS |
| 152 | PrimalEssenceEarned | 3 | yes | PASS |
| 156 | unmapped | 3 | blocked | INFO |
| 157 | unmapped | 6 | blocked | INFO |

## Server Achievement Runtime Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| GlobalAchievementManager caches Achievement rows | yes | PASS |
| AchievementInfo loads checklist rows | yes | PASS |
| Achievement persistence tracks data and completion time | no | FAIL |
| Achievement completion handles value and checklist progress | yes | PASS |
| Base manager sends initial/update packets | yes | PASS |
| Base manager checks achievement prerequisites | yes | PASS |
| Character achievement completion grants titles | yes | PASS |
| Realm-first achievement broadcasts are supported | yes | PASS |
| Quest completion triggers achievements | yes | PASS |
| Quest completion triggers checklist achievements | yes | PASS |
| Quest completion triggers counted checklist achievements | yes | PASS |
| Achievement completion triggers meta achievements | yes | PASS |
| Creature kill triggers achievements | yes | PASS |
| Creature kill triggers checklist creature achievements | yes | PASS |
| World-zone entry triggers achievements | yes | PASS |
| Simple object activation triggers creature-id achievements | yes | PASS |
| Shared activation success triggers discovery-object achievements | yes | PASS |
| Shared activation success triggers secret-stash achievements | yes | PASS |
| Crafting triggers crafted-item achievements | yes | PASS |
| Tradeskill tier changes trigger achievements | yes | PASS |
| Reputation level changes trigger achievements | yes | PASS |
| Map completion triggers achievements | yes | PASS |
| Character level changes trigger achievements | yes | PASS |
| Title grants trigger achievements | yes | PASS |
| Path level changes trigger achievements | yes | PASS |
| Guild beloved reputation totals trigger achievements | yes | PASS |
| Currency gains trigger earned-currency achievements | yes | PASS |
| Item consume triggers achievements | yes | PASS |
| Guild max-path-level totals trigger achievements | yes | PASS |
| Duel completion triggers participation and win achievements | yes | PASS |
| Class level-50 transitions trigger achievements | yes | PASS |
| Targeted emotes trigger creature/emote checklist achievements | yes | PASS |
| Critical deathblows trigger achievements | yes | PASS |
| Guild and circle joins trigger achievements | yes | PASS |
| Group joins trigger achievements | yes | PASS |
| Friend additions trigger achievements | yes | PASS |
| Housing plug placement triggers achievements | yes | PASS |
| Housing decor purchases trigger achievements | yes | PASS |
| Contract completions trigger contract achievements | yes | PASS |
| Account currency grants trigger achievements | yes | PASS |
| Primal essence account-currency grants trigger total achievements | yes | PASS |
| Costume item unlocks trigger wardrobe achievements | yes | PASS |
| Public event objective success triggers objective achievements | yes | PASS |
| AchievementAdvance spell effect grants achievements | yes | PASS |
| ServerAchievementInit serializes achievements | yes | PASS |
| ServerAchievementUpdate serializes achievements | yes | PASS |

- Result: INFO, persistence, packets, checklist/value completion, titles, realm-firsts, completion-driven meta achievements, and mapped trigger paths are covered; remaining client achievement types stay blocked until their event families are mapped.

## Summary

Result: FAIL
- server achievement runtime surface missing marker: Achievement persistence tracks data and completion time

Warnings:
- 216 wiki achievement page title(s) do not match DataMapping achievement_client_map names; sample: 2v2 Arena: Challenger I, 2v2 Arena: Challenger II, 2v2 Arena: Challenger III, 2v2 Arena: Challenger IV, 2v2 Arena: First Match Win I, 2v2 Arena: First Match Win II, 2v2 Arena: First Match Win III, 2v2 Arena: First Match Win IV
- 39 client achievement type id(s) are not named in AchievementType; unmapped type ids remain data-only
- 41 client achievement type id(s) have no mapped runtime trigger path
