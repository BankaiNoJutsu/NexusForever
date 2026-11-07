# Prerequisite 82 (UnitEntityType) boolean tail; 70 (ChallengeTier) native body (2026-11-07)

Evidence tier: **native bodies read (decompile of `FUN_14049c5b0` and `FUN_14049f9d0`)**. Source/SQLite only. No retail parity claim.

## 82 UnitEntityType - implemented
`FUN_14049c5b0(…, unit, comparison, Value)`: false for a null unit; `flag = *(int*)(unit+0x80) == Value`; comparison 1 returns the flag, 2 its negation, anything else false. The earlier implementation used the generic ordering compare; ordering comparisons are now false and a null unit fails even for NotEqual. Retail: 1 slot (Value 10), so no meaningful retail change. Tests: `PrerequisiteCheckUnitEntityTypeTests` (9 cases) written with the change (no red run); 1,640-case Prerequisite filter passes (Source/SQLite scope).

## 70 ChallengeTier - read, **not** changed (gate recorded)
`FUN_14049f9d0(…, comparison, ObjectId, Value)`: with the challenge service (`DAT_140c65948`) present, look up challenge `ObjectId` in the state tree at `+0x30`; `flag = found && Value < *(uint*)(state + 0x20)` (a missing challenge is false); Equal returns the flag, NotEqual its negation.
The server's proxy compares the challenge completion count with Value using the generic compare, which is a different test (Value 0, 1, 2 over 25 retail slots; the native test is "the state field is greater than Value").
Gate: the identity of runtime-state field `+0x20`. `INITIAL_FINDINGS.md` lists `+0x14` current count, `+0x1c` completion count, `+0x28` current tier from their getters; `+0x20` is not listed and the `ServerChallengeUpdate` field order (CurrentCount, GoalCount, ObjectiveCompletion, CurrentTier, LastRewardTier, CompletionCount) does not map onto those offsets monotonically, so CurrentTier, LastRewardTier and others are all candidates. Needed: the `ServerChallengeUpdate` handler's store into `+0x20`, or a getter that reads it. Until then the proxy stays; it is probably wrong for Equal/ordering rows.

Update (same day): the gate on `+0x20` was resolved by reading the row reader and the update consumer, see [PREREQUISITE_CHALLENGE_TIER_20261107.md](PREREQUISITE_CHALLENGE_TIER_20261107.md); type 70 is now implemented.
