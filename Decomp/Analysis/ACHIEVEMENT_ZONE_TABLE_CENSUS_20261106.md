# Achievement type and ZoneCompletion table census (2026-11-06)

Inputs: build 16042 `Achievement.tbl` and `ZoneCompletion.tbl` extracted from the installed client
([RAW_TABLE_EXTRACTION_20261106.md](RAW_TABLE_EXTRACTION_20261106.md)). Python read of the raw files;
no Source change, no gameplay test.

## F-036 ZoneCompletion: no non-title reward column

`ZoneCompletion.tbl` has 28 rows and exactly 10 fields (40-byte records), the same ten that
`ZoneCompletionEntry` already reads (Id, MapZoneId, ZoneCompletionFactionEnum, six counts,
CharacterTitleIdReward). There is no item, currency, path or other reward column. The F-036 open item
"non-title and path rewards for zone completion" therefore cannot be sourced from this table and is
excluded with evidence; any such reward would have to come from a different table or a native/retail
witness.

## F-035 Achievement types: 39 unmapped of 83

`Achievement.tbl` has 4,943 rows (96-byte records, 23 fields = `AchievementEntry`) across 83 distinct
`AchievementTypeId` values. `AchievementType.cs` names 44 of them (3,003 rows). The other 39 types cover
1,940 rows (39%) and have no server handler: 

| Type | Rows | Type | Rows | Type | Rows | Type | Rows |
|---|---|---|---|---|---|---|---|
| 12 | 842 | 44 | 161 | 88 | 150 | 57 | 124 |
| 96 | 120 | 9 | 88 | 76 | 73 | 61 | 70 |
| 33 | 44 | 79 | 37 | 77 | 33 | 65 | 27 |
| 121 | 24 | 94 | 20 | 13 | 19 | 87 | 16 |
| 102 | 16 | 66 | 12 | 72 | 7 | 157 | 6 |
| 26 | 5 | 68 | 5 | 73, 83, 89, 112 | 4 each | 46, 86, 129, 156 | 3 each |
| 14, 55, 101, 114 | 2 each | 22, 85, 109, 118, 140 | 1 each | | | | |

The client Lua (`UI/Achievements/Achievements.lua`) does not name these types. Meaning and trigger event
for each need the native client enum or a retail witness, which is the intended first use of REA/Ghidra.
Type 12 alone is 842 rows (17% of the table) and is the highest-value target.

Evidence tier: raw-table census only. Nothing here implements or claims behavior for the unmapped types.

## Structural observation on the largest unmapped types

Row-shape only; this does not establish what the types mean or how they progress.

| Type | Rows | Rows with WorldZoneId | Rows with ObjectId | Rows with ObjectIdAlt |
|---|---|---|---|---|
| 12 | 842 | 842 | 13 | 0 |
| 44 | 161 | 161 | 0 | 0 |
| 9 | 88 | 88 | 0 | 0 |
| 76 | 73 | 73 | 0 | 0 |
| 88 | 150 | 150 | 150 | 0 |
| 57 | 124 | 124 | 124 | 89 |
| 96 | 120 | 120 | 104 | 117 |
| 61 | 70 | 70 | 70 | 0 |

Types 12, 44, 9 and 76 (1,164 rows) are zone-scoped with essentially no object reference, so their progress
source is zone-level rather than a creature/item/quest id. A table-id overlap test against all extracted
tables was inconclusive (dense id ranges match many tables), so no semantic claim is made. Naming these types
still needs the native enum or a retail witness.

## Native client check (2026-11-06)

A string scan of `I:\WildStar\Client64\WildStar64.exe` (ASCII and UTF-16) finds only the display-side Lua API
(`AchievementsLib` Get*/CodeEnumAchievementCategory), event names and table names. It contains no
achievement-type names or numeric type enum, and the client Lua does not name them either. Achievement
progress types are therefore a server-side concept in retail; Ghidra on this binary is not expected to
recover the 39 unmapped type meanings. Per-type gate for every unmapped type listed above: a retail server
witness (capture of the progress event that advances that type) or community-documented type list; none is
available in the repository. Until then the unmapped types stay inert, which is the safe behavior.
