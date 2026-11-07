# Challenge prerequisite fields (2026-11-07)

Evidence tier: **native bodies read**; the field identities are **inferred** from the order of `ServerChallengeUpdate`, so 124 and 160 are *Implemented, inferred*. Source/SQLite only. No retail parity claim.

## Native bodies

The four challenge prerequisites read one 32-bit field of the client challenge record with id `ObjectId`. The record is found in an ordered map at `DAT_140c65948+0x30` (value pointer at node `+0x28`); an absent record reads
as 0, and a missing map returns false.

| Type | Slot / body | Field read | Use | Retail |
|---|---|---|---|---|
| 74 ChallengeLocked | +0x450, `FUN_14049fa80` → `FUN_14048d5a0` | `+0x2c` | Equal: the field (non-zero is true); NotEqual: field is 0 | 236 slots (NotEqual 185, Equal 51) |
| 124 ChallengeCompletionCount | +0x460, `FUN_14049fb50` | `+0x28` | (comparison, field, Value) through `FUN_1404a2090` | 7 slots, Values 1 to 45 |
| 125 Challenge125 | +0x458, `FUN_14049fac0` | `+0x34` | Equal: the field; NotEqual: field is 0 | 153 slots (NotEqual 145, Equal 8) |
| 160 Challenge160 | +0x468, `FUN_14049fbe0` | `+0x14` | (comparison, field, Value) | 33 slots, Values 0 to 20 |

## Record layout reading

If the record stores the `ServerChallengeUpdate` values in packet order as 4-byte fields (ChallengeId `+0x00`, Type `+0x04`, TargetGroupId `+0x08`, QualifyCount `+0x0c`, QualityTotal `+0x10`, CurrentCount `+0x14`, GoalCount `+0x18`,
ObjectiveCompletion `+0x1c`, CurrentTier `+0x20`, LastRewardTier `+0x24`, CompletionCount `+0x28`, Unlocked `+0x2c`, Activated `+0x30`, OnCooldown `+0x34`, LeftArea `+0x38`), then:

- 124 reads CompletionCount. The type's name and failure text ("Invalid challenge completion count") were written independently of this layout and agree, which is the corroboration for the layout up to `+0x28`.
- 160 reads CurrentCount; its Values (0, 1, 2, 4, 6, 20) look like progress counts.
- 74 reads Unlocked and 125 reads OnCooldown.

## Implemented

`PrerequisiteCheckChallengeCompletionCount` (124) and `PrerequisiteCheckChallengeCurrentCount` (160), through a new `IChallengeManager.GetCurrentCount` (`ChallengeManager` already had `GetCompletionCount`); a challenge the
manager has no state for compares 0, as an absent client record does, and an id above 16 bits is absent. `ChallengeCountPrerequisiteTests` (16 cases) pass; the first build failed to compile against the missing `GetCurrentCount`
(the red run was a compile failure). 1,920 Prerequisite/Challenge Source/SQLite cases pass in the same scope (the 16 are included). 40 previously always-false slots now evaluate.

## Not implemented: 74 and 125 (389 slots)

- 74's failure text is "The specified challenge is locked", but 185 of its 236 slots are NotEqual. Under the layout reading NotEqual means the field at `+0x2c` is 0, i.e. *not unlocked*, which a requirement worded
  "locked" would fail on when the challenge is unlocked. Either the text belongs to the rarer Equal rows only, or the field is not the packet's `Unlocked` bool.
- Whether a record exists for every challenge or only for the ones the server sent matters for both: an absent record is 0, so 74 NotEqual and 125 Equal/NotEqual differ for unsent challenges. The server's `activeChallenges`
  map is built from the challenges the character has touched, and always sends `Unlocked = true`.
- Next experiment: find the client reader of `ServerChallengeUpdate` (the 14-bit id, 4-bit type, then the fields in packet order) and confirm where it stores `Unlocked` and `OnCooldown` and whether it widens bools to 4 bytes;
  then decide the unsent-challenge case from the challenge list bootstrap.
