# Prerequisite 74 (ChallengeLocked) reads the client record's Unlocked field (2026-11-07)

Evidence tier: **native body read** for the field (`FUN_14049fa80` -> `FUN_14048d5a0`, offset `+0x2c`, confirmed by the same record layout as 124/125/160); the server's mapping (unlocked = tracked) is **inferred**. Source/SQLite only. No retail parity claim.

## Why it was deferred and why it can proceed
[PREREQUISITE_CHALLENGE_FIELDS_20261107.md](PREREQUISITE_CHALLENGE_FIELDS_20261107.md) mapped the four challenge prerequisites but left 74 because what the client holds for an untouched challenge (an absent record, field 0, or a record with `Unlocked`) was unknown for retail.
That question concerns which challenges retail listed to the client, not how a server record maps to the client's field. The server sends `Unlocked = true` for every challenge it tracks (`ChallengeManager`, the packet builder) and sends nothing for the rest, so within this server the client record
exists exactly for tracked challenges and its `Unlocked` field is 1 for them and 0 (absent) for the others. That is the same rule the already-implemented 124, 125 and 160 use.

## Change
`IChallengeManager.IsChallengeUnlocked(ushort)` (tracked state exists) and `PrerequisiteCheckChallengeLocked` (Equal: unlocked, NotEqual: not unlocked, other comparisons false, an id above 16 bits is absent; shared boolean tail). 236 retail slots (NotEqual 185, Equal 51) previously always evaluated false.
`ChallengeUnlockedPrerequisiteTests` (8 cases) was written with the check (no red run); the 2,278-case Prerequisite/Challenge Source/SQLite filter passes (separate scope).

## Limit
If retail listed locked zone challenges to the client with `Unlocked = 0` before they were touched, the results coincide for NotEqual (both 0) and Equal (both false); a retail capture would only matter if retail sent `Unlocked = 1` for challenges this server has not started tracking.
