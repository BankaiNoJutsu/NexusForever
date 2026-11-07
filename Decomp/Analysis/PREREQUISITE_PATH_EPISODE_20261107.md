# Prerequisite 53 (PathEpisode) is "Value is the current episode" (2026-11-07)

Evidence tier: **native body read** (`FUN_14049fd80`); the server's notion of the current episode is **inferred**. Source/SQLite only. No retail parity claim.

## Native
Evaluator case `0x35` calls vtable `+0x480` = `FUN_14049fd80(…, comparison, Value)`: the result is true only when the path service (`DAT_140c65970`) exists, it has a current episode object at `+0x58`, and `Value` equals the episode id read through
that object's definition (`*(*(episode + 0x48))`). Comparison 1 returns the match, 2 its negation, anything else false. `ObjectId` is not read (it is 0 in all 33 retail slots). Retail `Value`s are PathEpisode ids (121 x8, 284 x8, 246 x6, 454 x5, 238 x4, ...),
all with comparison Equal.

## Server
No handler existed for type 53 and the server kept no copy of the current episode, only the packet it sends (`ServerPathSetCurrentEpisode`, from `PathManager.SendPathCurrentEpisode`). `IPathManager.CurrentEpisodeId` (new) records the last episode sent and is
cleared when the zone episode is replayed (`ReplayCurrentZoneEpisode`); `PrerequisiteCheckPathEpisode` compares `Value` with it (0 means none, so nothing matches) using the shared boolean tail.

## Limits
- That the client's `+0x58` current episode equals the last `ServerPathSetCurrentEpisode` the server sent is assumed; the packet handler was not read.
- `PathManager` itself is not unit tested for the new property (it needs a session); the check is tested through the interface (`PrerequisiteCheckPathEpisodeTests`, 12 cases, written together with the check, no red run). The 1,839-case Prerequisite/Path Source/SQLite filter passes (separate scope).
