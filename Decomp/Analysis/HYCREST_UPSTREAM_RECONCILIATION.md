# Hycrest intro and first vote: bounded upstream reconciliation

2026-10-29. Reviewed #585 at `fa10dcff46d7f4bd6886bdc09f4322c65ec56c39`, against PR base `146e4f0ca26f4eb4c6c5953097aeba3cef9e0829`; the final content commit's parent is `1e662657c75924a78e14e4aa7fa68e26be2b82ca`. The PR includes earlier engine commits, and explicitly describes video-measured positions, synthetic arrival cinema and unimplemented missions. This pass does not import those assumptions.

## Verified build16042 data

Source: tables extracted from the local `D:/WildStar/Patch` archive using the corrected archive reader. Binary fingerprint: WildStar64.exe SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`. Reproducible independent table inspector and JSON/text output are external under `remaining-goal/HycrestTableInspector/` and `hycrest-tables.log`.

| Data | Verified meaning |
| --- | --- |
| PublicEvent418/419 | Intro/main adventure, world1149. |
| Objective2113 | Intro418; TalkTo/type17; count1; target group7183. Text names Dawson18365; target group7183 type1 contains that same Creature2 ID. |
| Objective2155 | Intro418; TimedWin/type13; count1; FailureTimeMs20000; no flags. Its text asks the player to listen to Dawson's briefing. |
| Objective189 | Intro418; ParticipantsInTriggerVolume/type6; object1994; location13091; count0. Text identifies Vesna17778. The marker radius1 is not proof of physical trigger bounds. |
| Objective1708 | Main419's existing script-controlled parent objective. Existing controller behavior remains. |
| Vote45 | Three populated choices, duration45000ms; label IDs432878/432879/432880. The actual English strings are The Farmer's Daughter / The Science of Revenge / Leveling the Field. |
| PE420/421/422 | World1149, parent419; title strings match those vote labels in exactly that order. |

Table SHA256 digests:

```text
PublicEvent.tbl          c1968cd17f869b2383c93f67fd2a409e68f64de214827e5e67fb08157a0595fd
PublicEventObjective.tbl 9978e44044cf4b5fa1d8b0770ef9a3e262a5a07b4e5664f2a5c3b41dce9f29d7
PublicEventVote.tbl      ef40eae42140e7c63effb93858534e20c9bab33a791f02987ed84fbb25daae71
TargetGroup.tbl          107d87cdb52d510fda52d04ab5b1b2996acd65c194ef6a75f922b6f5c5c5204d
WorldLocation2.tbl       791e6397008a8c930066e86c763be2ba7c1d11b8b85a13efe3097bf296b56e74
en-US.bin                588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7
```

Cached native `Lua_PublicEventObjective_GetRequiredCount` at `14068ef30` treats type13 as a timer and returns its duration in seconds through vtable slot0x180. `PublicEventVoteInitiate_Apply` at `1405f5170` is the existing mapped vote consumer; the fork already has vote initiation/tally/end, default/timeout and response handlers. No new protocol layout is guessed here.

## Implemented contracts

1. **Briefing completion**: Dawson's successful activation credits only the existing participating-player TalkTo path, guarded to creature18365 and world1149. Hycrest2155 enables explicit `EnableTimedWinCompletion()` and waits the table's20seconds. The former immediate counter credit is removed. Expiry uses normal objective status, achievement and quest callbacks.
2. **Compatibility policy**: automatic TimedWin success is an explicit controller opt-in. Existing challenge/optional controllers retain their deadline-failure behavior; eight existing boundary regressions exposed why a blanket type13 success port was unsafe. Busy/inactive/reset and other objective semantics remain intact.
3. **Whole-party maximum**: objective189 activation uses current instance membership; joins/leaves mark the requirement dirty and reconcile on the next map tick after grid cleanup. `SetDynamicMax` broadcasts the existing objective state and checks the new bound. Empty instances retain a minimum requirement of1, avoiding invented empty-party completion.
4. **Range cleanup**: `GridEntity.RemoveVisible` now removes an existing in-range member exactly once instead of rechecking its unchanged position after visibility was removed. This closes stale trigger counts for map departure/transfer; it also prevents a removed but previously unentered entity from being added to range.
5. **First vote and selected container**: successful intro completion starts vote45 once. Only a validated choice0/1/2 routes to420/421/422. The callback queues creation for the next map tick, before `PublicEventManager` enumerates events; missing/unsupported data does not offer an unroutable vote. Repeated/invalid callbacks, stopped parents and missing/finalised containers are guarded.
6. **Existing content map preservation**: a default-true auto-child hook keeps other maps' current recursive behavior. Hycrest overrides it, creating419+418 initially and only the selected branch later, rather than eagerly joining every mutually exclusive branch. Late arrivals join the active mission and avoid the finished intro/parent. Parent shutdown cancels unfinished selected work without fabricating success.
7. **Real tick dispatch**: HycrestMap explicitly reimplements `IUpdate`; inheriting the interface's explicit default from the base map does not map a new public method automatically. Regression coverage invokes the actual `IUpdate` path.

## Verification

Focused Hycrest, public-event objective, generic event-map/range, objective-credit and HallOfTheHundred suites: **950 passed,0 failed,0 skipped; zero build warnings/errors**. Exact command, log and TRX are external in `remaining-goal/HYCREST_RECONCILIATION.md`, `hycrest-focused.log` and `hycrest-tests/`. `git diff --check` passes. Parent owns the full solution checkpoint.

## Explicit remaining boundaries

This is **partial #585 implementation**, not retail completion. No synthetic arrival cinematic, forced ship boarding, guessed ship path/seat offsets, arbitrary barn radius, text-length speech timing, automatic fall/jump helpers or video-derived timeout fallback is added. Existing physical entry/tutorial/platform behavior remains. The existing type6 volume producer must supply objective189 occupancy; exact Hycrest trigger placement/bounds still needs authoritative data or runtime capture. Dialogue text IDs are corroborated but their delivery order/timing, sound and presentation remain unmapped. Mission420/421/422 gameplay, later branch decisions, return portal and exact live group flow remain separate work. No entitlement/default/spawn SQL changes were made.
