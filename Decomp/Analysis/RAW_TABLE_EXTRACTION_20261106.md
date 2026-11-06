# Raw build 16042 table extraction (2026-11-06)

`Tools/ArchiveProbe` (read-only) extracted all 384 `DB\*.tbl` files (228 MB) from
`I:\WildStar\Patch\ClientData.index/.archive` to `I:\tools\extract\DB` (outside the repository; not
committed). Nine tables match the SHA-256 values recorded in `ContractBuild16042Fixture.cs`
exactly (Quest2, Quest2Reward, AccountItem, Item2, QuestObjective, RewardTrack, RewardTrackRewards,
GameFormula, PeriodicQuestGroup), so this client's tables are the recorded build 16042 data.

Raw tables are therefore available on this machine. Previous "raw table absent" gates should be re-opened
for any item that only needed table rows.

## Not found

No extracted table is 481759 bytes or hashes to `6a46e0e5...` (the file named by the Communicator 609
ledger entry). `CommunicatorMessages.tbl` here is 494256 bytes, SHA-256
`086346547b6e5f457ff25d121b5c555bf708a69a40b49db34c6870b18d137a75`. Either the ledger names a different
table or an earlier export; the 609 entry's table identity needs reconciling before use.

## Quest2Reward census (5,415 rows, 24-byte records)

| Type | Rows |
|---|---|
| 1 Item | 4,021 |
| 2 Reputation | 228 |
| 3 Money | 691 |
| 4 TradeSkillXp | 378 |
| 5 TradeSkill | 1 |
| 7 AccountCurrency | 10 |
| 10 RotationEssence | 86 |
| 6 AccountItem, 8 AccountGenericUnlock, 9 GenericUnlock | **0** |

Consequences:
- Types 6, 8 and 9 never occur in shipped content. Types 8/9 are excluded with evidence (nothing to
  implement). The type-6 handling added in 1edfaf74 is harmless but unused by retail data; its
  `ObjectId` inference is moot.
- Type 10 occurs 86 times, always `ObjectId=0`, `Flags=0`, `ObjectAmount` 24, 40 or 240 (quest ids from
  7068). Which essence currency (account currencies 15 to 18) and how the amount is granted is not
  determined by the table; it needs the native grant routine or a capture. This is the one genuinely
  unhandled quest reward type.

## Type 10 (RotationEssence) follow-up (2026-11-06)

Amount distribution across the 86 rows is 24 (68 rows), 40 (16 rows) and 240 (2 rows); `ObjectId` is always 0.
86 distinct quests (from 7068) carry them, 12 of which also have other reward rows. `RewardRotationEssence.tbl`
(20 rows) maps each rotation essence entry to one of account currencies 15 to 18 (CrimsonEssence, CobaltEssence,
ViridianEssence, VioletEssence) with level 50 and difficulty flags, but nothing links a quest to an essence entry.
Which currency a type-10 reward credits is therefore decided by server logic (likely the active rotation) and is not
recoverable from the tables or from the client binary (rewards are granted server-side). Gate: a retail server witness
of a type-10 quest completion, or the active-rotation rule from a rotation capture. Left unhandled and inert.
