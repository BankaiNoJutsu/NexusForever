# Contract reward full-table classification (2026-11-06)

Inputs: build 16042 `RewardTrack.tbl`, `RewardTrackRewards.tbl`, `Item2.tbl`, `AccountItem.tbl` extracted from the
installed client ([RAW_TABLE_EXTRACTION_20261106.md](RAW_TABLE_EXTRACTION_20261106.md)); the SHA-256 values of
RewardTrack, RewardTrackRewards, Item2 and AccountItem match `ContractBuild16042Fixture.cs`
(RewardTrack AA2D0441..., RewardTrackRewards 81F72600..., Item2 F51673FD..., AccountItem 13A07FBB...).

Contract tracks 4 and 5 each have 121 `RewardTrackRewards` rows with three choice slots, 726 choices in total.

| Slot | Enum | Resolves in | Count per track | Notes |
|---|---|---|---|---|
| 0 | 0 | Item2 | 121 | count 1 |
| 1 | 0 | Item2 | 121 | count 1 |
| 2 | 1 | AccountItem | 120 | count 1; in 60 of them the id also exists in Item2 (numeric collision), so the enum, not id presence, must select the table |
| 2 | 0 | Item2 | 1 | the single row that also carries a currency (flags 3, point flag 4 or 16) |

Result: every one of the 726 choices resolves in exactly the table its enum selects, no choice is missing from
its table, and every count is 1. The full table therefore contains no entry that requires an unknown reward
type. Row point flags: track 4 = {1:60, 2:60, 4:1}; track 5 = {1:30, 2:30, 4:30, 8:30, 16:1}.

This closes the "full-table classification" part of the 07CE gate. Still open, so public 07CE stays unwired:
- the reset edge rules (unclaimed milestones, excess points) have no retail witness;
- which slot a player may choose or receives (faction/choice policy) is not determined by the table alone;
- native timing/UI and retail packet capture.

Evidence tier: raw-table census (Python read of the extracted files). No new gameplay test was added and no
Source code changed. The extracted files are not committed (228 MB of game data).
