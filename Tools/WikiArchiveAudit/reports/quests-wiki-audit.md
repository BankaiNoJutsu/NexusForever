# WildStar Wiki Archive Audit

- Generated: 2026-05-23
- Archive: `artifacts/wildstar-fandom-wiki-2026-05-18` (local, gitignored)
- Client SQL: `wildstar_client_mysql` (local, gitignored)
- Domains: quests
- Generator: `Tools/WikiArchiveAudit/audit_wildstar_wiki.py`

## Quest Wiki Page Coverage

- Quest infobox pages: 345
- Pages with objective bullets: 81
- Pages with reward fields: 322
- Pages with episode fields: 344
- Pages with previous/next chain fields: 46
- Pages with episode-progression transclusion: 32
- Wiki quest titles matched to DataMapping client names: 322/336

## Client Quest Table Coverage

| Client table | Rows | Result |
| --- | --- | --- |
| Quest2 | 5194 | PASS |
| QuestObjective | 10031 | PASS |
| Quest2Reward | 5415 | PASS |
| QuestCategory | 53 | PASS |
| QuestGroup | 23 | PASS |
| QuestHub | 209 | PASS |
| Episode | 300 | PASS |
| EpisodeQuest | 1497 | PASS |
| PeriodicQuestGroup | 85 | PASS |
| PeriodicQuestSet | 67 | PASS |

## Client Quest Data Details

- Client Quest2 rows: 5194
- Client Quest2 rows with objectives: 3903
- Client Quest2 rows with Quest2Reward rows: 1938
- Client Quest2 rows with prerequisite quest ids: 1594
- Client Quest2 rows with prerequisiteId: 1236
- Client Quest2 rows with level prerequisite: 5157
- Client Quest2 rows with race prerequisite: 0
- Client Quest2 rows with class prerequisite: 1
- Client Quest2 rows with player-faction gate: 3939
- Client Quest2 rows with item prerequisite: 165
- Client Quest2 rows with faction-level prerequisite fields: 5
- Client Quest2 rows with exclusion prerequisite fields: 631
- Client Quest2 rows with alt-receiver prerequisite fields: 153
- Client Quest2 rows with pushed item ids: 114
- Client Quest2 rows with virtual pushed item ids: 183
- Client Quest2 rows with repeat periods: 1214
- Client Quest2 rows with quest timers: 14
- Client Quest2 rows with group size: 127

## Quest Objective Type Coverage

| Type id | Server enum | Client rows | Result |
| --- | --- | --- | --- |
| 2 | KillCreature | 598 | PASS |
| 3 | TalkTo | 350 | PASS |
| 4 | CollectItem | 327 | PASS |
| 5 | ActivateEntity | 1556 | PASS |
| 8 | KillTargetGroups | 498 | PASS |
| 9 | ActivateEntity2 | 18 | PASS |
| 10 | ScriptedTargetGroupChecklist | 52 | PASS |
| 11 | SpellSuccess | 128 | PASS |
| 12 | SucceedCSI | 1187 | PASS |
| 13 | SpellSuccess2 | 3 | PASS |
| 14 | ActivateTargetGroupChecklist | 426 | PASS |
| 15 | KillNamedCreature | 26 | PASS |
| 16 | KillTargetGroup | 55 | PASS |
| 17 | EnterZone | 199 | PASS |
| 18 | TalkToTargetGroup | 35 | PASS |
| 19 | UNUSED19 | 2 | PASS |
| 20 | Unknown20 | 11 | PASS |
| 21 | GatheResource | 10 | PASS |
| 22 | EnterArea | 511 | PASS |
| 23 | ActivateTargetGroup | 96 | PASS |
| 24 | CompleteQuest | 39 | PASS |
| 25 | CompleteEvent | 152 | PASS |
| 27 | Unknown27 | 1 | PASS |
| 28 | Unknown28 | 5 | PASS |
| 29 | Unknown29 | 2 | PASS |
| 31 | Unknown31 | 260 | PASS |
| 32 | VirtualCollect | 709 | PASS |
| 33 | CraftSchematic | 215 | PASS |
| 35 | SpellSuccess3 | 10 | PASS |
| 36 | SpellSuccess4 | 6 | PASS |
| 37 | LearnTradeskill | 4 | PASS |
| 38 | ObtainSchematic | 2520 | PASS |
| 39 | CompleteMaxLevelQuests | 2 | PASS |
| 40 | ParticipateInGroupContent | 2 | PASS |
| 41 | PvPKills | 2 | PASS |
| 42 | EarnCurrency | 2 | PASS |
| 44 | CombatMomentum | 7 | PASS |
| 46 | KillCreature2 | 1 | PASS |
| 47 | CompleteChallenge | 2 | PASS |
| 48 | BeginMatrix | 2 | PASS |

## Quest Reward Type Coverage

| Type id | Server enum | Client rows | Runtime grant | Result |
| --- | --- | --- | --- | --- |
| 1 | Item | 4021 | yes | PASS |
| 2 | Reputation | 228 | yes | PASS |
| 3 | Money | 691 | yes | PASS |
| 4 | TradeSkillXp | 378 | yes | PASS |
| 5 | TradeSkill | 1 | yes | PASS |
| 7 | AccountCurrency | 10 | yes | PASS |
| 10 | RotationEssence | 86 | blocked | PASS |

## Server Quest Runtime Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| GlobalQuestManager caches Quest2 rows | yes | PASS |
| QuestInfo resolves prerequisite quests | yes | PASS |
| QuestInfo resolves QuestObjective rows | yes | PASS |
| QuestInfo resolves Quest2Reward rows | yes | PASS |
| Quest accept validates faction/race/class/level/prereq quests/prerequisiteId | yes | PASS |
| Quest accept validates prerequisite item | yes | PASS |
| Quest accept validates quest exclusion prerequisites | yes | PASS |
| Quest accept validates faction-level prerequisites | yes | PASS |
| Quest alternate receivers are mapped as client-side guidance | yes | PASS |
| Quest virtual pushed items are client-derived from quest state | yes | PASS |
| Quest accept grants pushed items | yes | PASS |
| Quest objective progress persists and sends updates | yes | PASS |
| Quest completion validates state and receiver/communicator | yes | PASS |
| Quest completion grants item rewards | yes | PASS |
| Quest completion grants reputation rewards | yes | PASS |
| Quest completion grants currency rewards | yes | PASS |
| Quest completion grants tradeskill XP and professions | yes | PASS |
| Quest completion grants account currency rewards | yes | PASS |
| Quest completion grants XP and cash overrides/formulas | yes | PASS |
| Quest repeat periods schedule daily/weekly reset | yes | PASS |
| World handlers route quest lifecycle messages | yes | PASS |
| ServerQuestInit serializes active/inactive/completed state | yes | PASS |
| ServerQuestObjectiveUpdate serializes objective progress | yes | PASS |

- Result: INFO, core quest lifecycle, item/exclusion/faction-level prerequisites, client-guidance alternate receivers, client-derived virtual pushed items, and item/money/reputation/tradeskill/account-currency rewards are covered; RotationEssence remains bounded by the active reward-rotation schedule blocker.

## Summary

Result: PASS

Warnings:
- 17 wiki quest page title(s) do not match DataMapping quest_client_map names; sample: (Un)Safety Protocols, ATTUNEMENT Over the Gorge, Breaking the Stonebreaker, Busting Skulls, Burning Trees, Called to Stormcaller Landing, Crafting Your First Light Armor, Materials for Beginner Tailors, On Chernabog's Trail
- Quest2Reward type 10 (RotationEssence) expands through server-provided active reward-rotation state keyed by WorldZone.RewardRotationContentId and Quest2.Id; no runtime grant is safe until the server has authoritative schedule entries and multiplier
