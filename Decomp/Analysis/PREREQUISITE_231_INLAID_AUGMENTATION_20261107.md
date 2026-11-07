# Prerequisite 231 is "inlaid augmentation not yet unlocked"; its list, producer and the missing persistence (2026-11-07)

Evidence tier: **native writer and reader read** (`FUN_1403b5f80`, `FUN_14049d7b0`) plus a **data correlation** (235 of 240 ids). The effect-to-list link is Correlated, not Verified. No code change: the producer needs persistence that does not exist. Source/SQLite only. No retail parity claim.

## What the list is
[PREREQUISITE_TYPE_COVERAGE_20261107.md](PREREQUISITE_TYPE_COVERAGE_20261107.md) recorded that type 231 scans a player-held array of 16-bit ids at `player + 0x6df4` (count at `+0x6df0`) and that the writer was unidentified. The writer is the **player-create handler**
`FUN_1403b5f80` (`ServerPlayerCreate`): it stores the count byte (`param_2[0x39]`) at `+0x6df0` and `memcpy`s `count * 2` bytes from the packet into `+0x6df4`. That is the packet's `UnlockedInlaidAugmentationIds` field (a 6-bit count and 16-bit ids; the older model name for it was
`KnownDyes`). The server fills this list nowhere (`ServerPlayerCreate.UnlockedInlaidAugmentationIds` is never populated), so every client believes the character has unlocked none.

## Type 231
All 241 retail slots are `NotEqual` with `Value` 0 and an `ObjectId` between 13 and 936 (240 distinct). The body returns false unless the unit is the local player, so for NotEqual-only rows the content is available exactly when the id is **not** in the list.
Because the server never reports a populated list, a correct check would be true for every row today, while the unhandled type evaluates false and keeps the gated content unavailable.

## Producer
The `UnlockInlaidAugment` spell effect (type 127, 281 retail rows, currently unhandled): `DataBits01..09` are always 0 and `DataBits00` has 244 distinct values, **235 of which are exactly the type 231 `ObjectId`s** (235 of the 240 prerequisite ids appear as an effect value).
So the effect adds `DataBits00` to the character's unlocked list and prerequisite 231 stops the same grant from being offered twice. `ServerLasAuxPackets` already carries a message with the same `UnlockedInlaidAugmentationIds` field (the incremental update).

## Why nothing was implemented
A faithful implementation needs (1) a per-character persisted set of unlocked ids (a new Character table plus MySQL and SQLite migrations), (2) the `UnlockInlaidAugment` effect handler, (3) the list in `ServerPlayerCreate` and the aux update packet, and (4) the type 231 check (also pinned by
`AmpLifecycleTests.RegisteredPrerequisiteDispatchKeepsEnabledAndGlobalAmpListsSeparate`, which requires it to be a list separate from the enabled-AMP list used by 227). Step 1 is a schema/DDL decision and needs the migration projects to be built, which this lane does not do; an in-memory-only version
would silently forget unlocks at relog and was not added. This also moves the 281 `UnlockInlaidAugment` rows from "no witness" to "data-correlated producer, needs persistence".

## Remaining gate
A persistence design (table and migrations) approved for the Character database, and confirmation of the effect's observable result from a capture (that the aux update packet is sent when the effect fires).
