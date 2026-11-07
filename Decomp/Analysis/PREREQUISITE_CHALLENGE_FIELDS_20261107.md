# Challenge prerequisite fields (2026-11-07)

Evidence tier: **native bodies read, field offsets confirmed by the client's own Lua accessors**. Source/SQLite only. No retail parity claim.

## Native bodies

The four challenge prerequisites read one 32-bit field of the client challenge record with id `ObjectId`. The record is found in an ordered map at `DAT_140c65948+0x30` (value pointer at node `+0x28`); an absent record reads
as 0, and a missing map returns false.

| Type | Slot / body | Field read | Use | Retail |
|---|---|---|---|---|
| 74 ChallengeLocked | +0x450, `FUN_14049fa80` → `FUN_14048d5a0` | `+0x2c` | Equal: the field (non-zero is true); NotEqual: field is 0 | 236 slots (NotEqual 185, Equal 51) |
| 124 ChallengeCompletionCount | +0x460, `FUN_14049fb50` | `+0x28` | (comparison, field, Value) through `FUN_1404a2090` | 7 slots, Values 1 to 45 |
| 125 Challenge125 | +0x458, `FUN_14049fac0` | `+0x34` | Equal: the field; NotEqual: field is 0; other comparisons false | 153 slots (NotEqual 145, Equal 8) |
| 160 Challenge160 | +0x468, `FUN_14049fbe0` | `+0x14` | (comparison, field, Value) | 33 slots, Values 0 to 20 |

## Field identities

The `Game.Challenges` Lua method table (mapped in `INITIAL_FINDINGS.md`, "Eighty-sixth Game.Challenges method-table follow-up") binds accessors that read the same record. Decompiled this session:

| Lua method | Function | Record field |
|---|---|---|
| `GetCurrentCount` | `FUN_140685210` | `+0x14` |
| `GetCompletionCount` | `FUN_140685640` | `+0x28` |
| `IsActivated` | `FUN_1406859e0` | `+0x30` |
| `IsInCooldown` | `FUN_140685ad0` | `+0x34` (`!= 0`) |

These offsets match a record that stores the `ServerChallengeUpdate` values in packet order as 4-byte fields (ChallengeId `+0x00`, Type `+0x04`, TargetGroupId `+0x08`, QualifyCount `+0x0c`, QualityTotal `+0x10`, CurrentCount `+0x14`,
GoalCount `+0x18`, ObjectiveCompletion `+0x1c`, CurrentTier `+0x20`, LastRewardTier `+0x24`, CompletionCount `+0x28`, Unlocked `+0x2c`, Activated `+0x30`, OnCooldown `+0x34`, LeftArea `+0x38`). Four anchors (`+0x14`,
`+0x28`, `+0x30`, `+0x34`) are confirmed; `+0x2c` is the one field between CompletionCount and Activated, which in packet order is `Unlocked`. The type 124 name and failure text ("Invalid challenge completion count") were
written independently and agree.

## Implemented

`PrerequisiteCheckChallengeCompletionCount` (124), `PrerequisiteCheckChallengeCurrentCount` (160) and `PrerequisiteCheckChallengeOnCooldown` (125), through new `IChallengeManager.GetCurrentCount` and
`IsChallengeOnCooldown` (`ChallengeManager` already had `GetCompletionCount`). A challenge the manager has no state for compares 0 / not on cooldown, as an absent client record does, and an id above 16 bits is absent.
`ChallengeCountPrerequisiteTests` (22 cases) pass; the first build of the 124/160 tests failed to compile against the missing `GetCurrentCount` (the red run was a compile failure), the 125 tests were written before the
check but not run red. 1,928 Prerequisite/Challenge Source/SQLite cases pass in the same scope (the 22 are included). 193 previously always-false slots (7 + 33 + 153) now evaluate.

## Not implemented: 74 (236 slots)

- 74 reads `+0x2c`, the packet's `Unlocked`. Its failure text is "The specified challenge is locked", but 185 of its 236 slots are NotEqual (field is 0, i.e. *not unlocked*).
- Whether a client record exists for every challenge or only for the ones the server sent decides an untouched challenge's result (absent: 0, so Equal false and NotEqual true; present with `Unlocked`: the reverse). The
  server's runtime map holds only challenges the character has touched and always sends `Unlocked = true`; what retail did for untouched zone challenges is not known.
- Next evidence: the client reader of `ServerChallengeUpdate` (opcode 0x00C7: 14-bit id, 4-bit type, then the fields in packet order) to see which challenges the client lists, or a retail capture of a fresh character's first
  challenge update.

## Update (same day)
74 is now implemented: see [PREREQUISITE_CHALLENGE_UNLOCKED_20261107.md](PREREQUISITE_CHALLENGE_UNLOCKED_20261107.md). The 'Not implemented: 74' section above is superseded.
