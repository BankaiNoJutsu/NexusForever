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
