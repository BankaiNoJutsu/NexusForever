# LaughingWS Path, Settler, Soldier, And Fortune Blocker Review

Date: 2026-05-27

## Decision

Milestone 5 is closed task-by-task without inventing unmapped durable state.

LWS-060 now has a narrow implemented persistence slice for active/completed
path mission rows. LWS-062, LWS-063, and LWS-066 remain mapped-only blockers.
LWS-061 and LWS-064 have implemented progress, but exact reward/progress
precision still has named blockers. LWS-065 is implemented with focused
verification in the current test suite.

## LWS-060 Path Persistence Design

Partially implemented with blockers. `PathManager` now persists active and
completed mission state through `character_path_mission`, including mission id,
episode id, state, completion flag, `ProgressCount`, `ProgressData`, and
configured XP. Login construction restores completed mission status, and initial
path packets replay unfinished persisted episode state through the existing
`ServerPathSetCurrentEpisode`, `ServerPathEpisodeProgress`, and
`ServerPathMissionActivate` messages. EF migration
`20260527191317_CharacterPathMissionState` adds the character-owned table.

Still blocked pending stronger proof:

- object-id state after logout/reload;
- reward-history behavior for already-granted path rewards;
- negative cases for changing path, faction, zone, and character reload.

Focused verification:

- `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PathManagerTests" -v minimal --nologo` passed `37/37`.
- `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo` passed `1805/1805`.

## LWS-061 Path Reward Precision

Partially implemented. Current implementation supports table-backed path level
rewards, unflagged `PathRewardType.Mission` rows where current fields are known,
and a client-mapped mission XP fallback. It still keeps guardrails:

- mission completion XP uses state-provided XP when present; otherwise known
  active-path mission rows read `GameFormula` row `0x017a` (`378`) `Dataint0`
  with the client fallback `50` if the row is unavailable;
- item count uses `PathReward.Count` when non-zero, otherwise a single-item
  fallback;
- flagged reward rows remain unsupported;
- exact reward presentation and overflow behavior remain unmapped.

The decompile pass corrected the prior `PathRewardTable` reading: `Lua_PathMission_GetRewardXp`
(`WildStar64.exe` `14067c010`) calls `GameFormula_GetEntryById` (`140200220`)
with `0x017a`, reads `entry + 0x04` (`GameFormula.Dataint0`), and falls back to
`50` when the row is missing. The local extracted `PathReward.tbl` schema has
`11` fields (`ID`, `pathRewardTypeEnum`, `objectId`, `spell4Id`, `item2Id`,
`quest2Id`, `characterTitleId`, `prerequisiteId`, `count`, `pathRewardFlags`,
`pathScientistScanBotProfileId`) and no XP field, so per-mission `PathReward`
XP remains blocked pending stronger table or packet evidence.

## LWS-062 Path Unlock Sequencing

Partially implemented with blockers. `TryActivateCurrentZoneEpisode` uses
table-backed current world/root zone, active path, faction, prerequisite, and
id-width filters before sending episode and mission packets. Current regression
coverage also proves two reload sequencing guards: completed persisted missions
do not replay active episode packets, and an unfinished persisted active episode
suppresses duplicate activation packets on zone re-entry. This is still a useful
WIP path surface, not full retail sequencing.

Still required proof:

- current-zone activation timing;
- client-smoked prerequisite and faction filtering semantics;
- exact replay/completion ordering;
- reload behavior after path, faction, and zone changes.

## LWS-063 Settler Hub Durable State

Partially implemented with blockers. Settler hub progress still uses the
conservative active mission progress path, but that progress is now durable
through `character_path_mission`: a partially progressed Settler_Hub mission
reloads with its `ProgressCount`/`ProgressData` and can complete after the next
accepted build-tier request. This only covers mission progress durability, not
the full Settler built-group/resource/avenue backing model.

Do not add broader EF state or Settler mutation until smoke proves built group
identity, resources, hub/avenue progression, and failure result packets.

Focused verification:

- `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PathManagerTests" -v minimal --nologo` passed `38/38`.

## LWS-064 Settler/Soldier Progress Packets

Partially implemented. `Mission.ObjectiveCompletionFlags`/`StateFlags` have been
renamed to `ProgressCount`/`ProgressData` after a focused client pass:
`Lua_PathMission_GetNumCompleted` (`WildStar64.exe` `14067c370`) calls
`PathMissionRuntime_GetCurrentProgress` (`14056d0d0`), and that helper reads the
first runtime progress payload for count-style mission types including
`Soldier_Assassinate` (`0x04`) and `Settler_Hub` (`0x13`). NexusForever's active
Soldier assassinate and Settler hub progress now use that mapped first payload.

The Settler build status packet shape is now mapped and field-named for the
current safe emitted surface. `ServerPathSettlerBuildStatus_ReadPayload`
(`WildStar64.exe` `14007a7b0`, opcode `0x0671`) reads a 14-bit
`PathSettlerHubId` followed by one status row. `ServerPathSettlerBuildStatusList_ReadPayload`
(`14007a800`, opcode `0x066E`) reads a 14-bit hub id, a uint32 row count, and
counted status rows. The shared row reader (`14007a730`) reads
`PathSettlerImprovementGroupId` (`u14`), `Tier` (`i32`), `RemainingTimeMs`
(`u32`), and `BundleCount` (`u32`); `PathSettler_HandleBuildStatus`
(`1406185a0`) and `PathSettler_HandleBuildStatusList` (`140618400`) apply those
rows to client runtime state and raise `SettlerBuildStatusUpdate` for the
current player unit. `ServerPathSettlerBuildResult_ReadPayload` (`14007aa70`,
opcode `0x066C`) reads `Result`, `PathSettlerImprovementId`, and
`PathSettlerImprovementGroupId`.

Still blocked: `ProgressData` producer semantics remain per-type; Explorer
Vista (`0x0f`) and several other mission types use the alternate payload for
`GetNumCompleted`, Soldier holdout wave runtime is still unmapped, and Settler
build non-success result values, failure ordering, resource costs, durable
built-group state, and avenue/resource semantics still need packet or smoke
proof.

Additional Soldier holdout mapping was completed on 2026-05-27 through the
decompile workflow. The `Game.SoldierEvent` Lua method table near
`WildStar64.exe` `140c5c130` now has labels for the holdout accessors:
`Lua_SoldierEvent_GetWaveCount` (`1406837d0`) reads the runtime event wave count
at offset `0x90`, `Lua_SoldierEvent_GetWavesReleased` (`140683880`) calls the
released-wave helper, max/current health accessors read runtime health fields or
unit trees, and unit list accessors split holdout units by kind `0` defend, `1`
auxiliary, and `2` escaping. This strengthens the client object mapping but
does not prove server packet producer timing, wave spawning, unit ownership,
failure/death semantics, or packet ordering, so generic Soldier holdout runtime
remains blocked.

## LWS-065 Generic Path Edge Semantics

Implemented with verification. Focused tests cover the current generic path edge
semantics, including:

- object-id completion guards;
- mismatched path mission rejection;
- active-only Explorer Vista, ExploreZone, and power-map completion;
- active Soldier assassinate progress;
- already-complete duplicate completion guards;
- reward and path-mission achievement boundaries.

Verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PathManagerTests|FullyQualifiedName~PathRewardGrantTests|FullyQualifiedName~ClientPathExplorerProgressReportHandlerTests|FullyQualifiedName~GalacticArchiveUnlockRuleTests" -v minimal --nologo
```

Result: `56/56` passed on 2026-05-27 after the `GameFormula 0x017a` fallback
update.

Additional LWS-064 verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PathManagerTests|FullyQualifiedName~PathSettlerBuildHandlerTests|FullyQualifiedName~ClientPathExplorerProgressReportHandlerTests|FullyQualifiedName~ClientPathExplorerPowerMapProgressHandlerTests" -v minimal --nologo
```

Result: `36/36` passed on 2026-05-27 after the path progress payload rename.

Settler packet field-name verification:

```powershell
dotnet build Source\NexusForever.Network.World\NexusForever.Network.World.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PathSettlerBuildHandlerTests|FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
```

Result: network build passed, focused packet/handler tests passed `37/37`, and
the decompile manifest validated `WildStar64.exe ok` on 2026-05-27 after adding
the Settler packet labels.

## LWS-066 Fortune Weights And Rotations

Blocked. Current Fortune is complete enough for the emulator session flow and UI
probability transport, but exact Madame Fay per-item weights and active rotation
catalog remain unavailable. `FORTUNE_WEIGHT_AUDIT.md` records the local catalog
audit, rejects old branch hardcoded gacha arrays as retail evidence, and keeps
the current rarity-tier weights complete-but-approximate.

Replace emulator weights only after a retail `ServerFortuneRewards` capture or
storefront-server catalog dump maps per-item/rotation weights.
