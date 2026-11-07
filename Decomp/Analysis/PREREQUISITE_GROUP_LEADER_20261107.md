# Prerequisite type 42 (IsGroupLeader) treats "no group" as leader and ignores Value (2026-11-07)

Evidence tier: **native body read** (`FUN_14049e850`, evaluator vtable slot `+0x310`). Source/SQLite only. No retail parity claim.

## Native
Reads the group manager at `DAT_140c65898 + 0x6c50`. `a` = the local player's entry from the member array (`+0x58`) found by `FUN_14079ee60`, or 0 when there is no manager or no entry; `b` = the member at the leader index (`+0x68`) when the member count (`+0x60`) is non-zero, else 0. Comparison 1 returns `a == b`, comparison 2 `a != b`, anything else 0. `Value` is not read. With no group both are 0, so the local player counts as the leader.

## Retail
One slot (Prerequisite row 5948, first of three: 42 Equal, 40 Health `<=` 10, 69 ChallengeRequirement), comparison Equal, `Value` 0, `ObjectId` 0.

## Server before / change
`Compare(comparison, isLeader ? 1 : 0, Value)`: with the retail slot (Equal, Value 0) it held for a player who is **not** the leader (or has no group) and failed for the leader, the opposite of the native result. Now `CompareFlag(comparison, isLeader)` where `isLeader` is true without a group or when the group's leader is the player. A group id the state manager does not know is treated as "not the leader" (the native member array is not available to model it).

## Tests
`PrerequisiteCheckIsGroupLeaderTests` (12 cases; 6 failed first). `PrerequisiteCheckTests.IsGroupLeader_ComparesPartyLeader` encoded the Value reading and was removed. 1,986 Prerequisite/Group Source/SQLite cases pass (separate scope).

## Also read, no change
**Type 1 Level** (case 1): `unit+0x3c` if non-zero else `unit+0x38`, integer compare, false for a null unit. The server has no effective/mentored level (the `+0x3c` value), so `Level` is equivalent today; the manager already returns false for a null subject.
