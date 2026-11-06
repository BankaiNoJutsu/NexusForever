# Candidate meanings for the unmapped Achievement types (2026-11-06)

Method: local read-only queries joining `wildstar_client.achievement` (the imported build 16042 table, same
`achievementTypeId`/`objectId`/`value` as the raw TBL) to `jabbithole.achievements` (third-party retail
achievement names/descriptions, joined on `game_id` = achievement id; 2,324 of 4,943 rows match) on the local MySQL
reference databases described in AGENTS.md. The player-facing description of each achievement is the only
semantic evidence: it shows what the achievement asks for, not how the retail server counted progress. Every
meaning below is therefore a **candidate** and carries no retail-server proof. Rows without a jabbithole match
are marked n/a; their meaning stays unknown.

| Type | Rows | Candidate meaning from retail descriptions | Object/value shape |
|---|---|---|---|
| 12 | 842 | Scripted/bespoke condition ("defeat X without ...", "win 25 5v5 Arena matches", "capture Uplinks 50 times") | ObjectId rarely set; value 0..2^31 |
| 88 | 150 | PvP: kills on control points in a named battleground (Halls of the Bloodsworn) | ObjectId set; value 1..10000 |
| 57 | 124 | Kill a named boss or earn a medal in a named instance | ObjectId set, alt on 89 |
| 96 | 120 | PvP single-match stat threshold (kills/assists/saves in Walatiki Temple) | ObjectId set; value 3..3,125,000 |
| 9 | 88 | Collect a named item from a defeated creature (head, mandible); also decor pack completion | value 0..15; n/a text for many |
| 76, 44, 79, 77, 33 | 73, 161, 37, 33, 44 | n/a for 76/44/79/77; 33 = guild salvage totals (10,000/100,000) | zone only / counts |
| 61 | 70 | PvP multi-kill (Double Kill count, up to 250) | ObjectId set; value 1..10000 |
| 65 | 27 | One-off world interaction (use mushrooms, jump a waterfall, sit in a throne) | ObjectId set; value 0..1 |
| 121 | 24 | Earn Bronze or better on challenges in a zone | zone only |
| 87 | 16 | PvP: kill N players, excluding Arenas (1 to 250,000) | no object; value 1..250000 |
| 94 | 20 | Personal Arena Rating thresholds (1,500..2,200) per bracket | ObjectId 16 rows; value 1500..2200 |
| 102 | 16 | Team Arena Rating thresholds (1,500..2,200) per bracket | ObjectId set; value 1500..2200 |
| 13 | 19 | Collect/visit all N places in an area | value up to 12 |
| 66 | 12 | Cosmic Rewards Program tier completion unlocking a mount | ObjectId 9 |
| 72, 89 | 7, 4 | Guild totals: challenges completed (2,500..), influence earned (2.5M..) | value 100..20,000,000 |
| 86 | 3 | Earn Gold on N challenges (150, 220) | value 80..220 |
| 112 | 4 | Housing: N players visiting your house at once (25, 50) | value 25..250 |
| 109 | 1 | Housing: set a player as your neighbor (achievement 4887) | value 1 |
| 114 | 2 | Housing: build/purchase a house type (Cozy House, Spacious House) | ObjectId set; value 1 |
| 83 | 4 | Housing/construction: complete N structures (50..1000) | value 50..1000 |
| 73 | 4 | Gain access to a raid (Datascape, Genetic Archives) | ObjectId set; value 1 |
| 26, 46, 22, 68 | 5, 3, 1, 5 | Specific consumable/turn-in/lantern/corpse-burning scripted counters | ObjectId set |
| 101 | 2 | Unlock all major Taxi Kiosks | value 1 |
| 140 | 1 | Collect the Protostar Premium Fabkits | none |
| 14, 55, 85, 118, 129, 156, 157 | 2, 2, 1, 1, 3, 3, 6 | n/a | |

Implications (each a hypothesis to confirm with a producer-side witness before any code):
- Types 87/88/96/61/94/102 are PvP statistic achievements; their producers (kill attribution, arena rating)
  exist partly in the PvP/leaderboard code (`LeaderboardScoreIngestion` still has no callers).
- Types 109, 114, 112, 83 are Housing events and sit beside the already mapped HousingPlugPlace (111) and
  HousingDecorPurchase (113).
- Type 12 (17% of the table) is scripted: its progress is credited by explicit content/script calls, not a
  generic event, so it cannot be mapped to a single trigger.

Evidence tier: local MySQL reference-database reads plus the raw table; no Source change, no test.
