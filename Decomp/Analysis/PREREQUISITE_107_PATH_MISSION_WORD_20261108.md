# Prerequisite 107: the path mission word is `ProgressCount`, not `ProgressData` (2026-11-08)

Evidence tier: **native bodies read** (WildStar64.exe 16042, Ghidra 12.1.4 headless: evaluator, mission class vtable, both packet row readers, the update consumer) plus a `Prerequisite.tbl` x `PathMission.tbl` x `PathSoldierActivate.tbl` census. The server proposal is **inferred**. Documentation only: no source, tests or existing docs changed. No retail parity claim.
Resolves unknown 2 of [PUBLIC_EVENT_NATIVE_READ_20261108.md](PUBLIC_EVENT_NATIVE_READ_20261108.md) and **corrects its inference** that server `ProgressData` equals the client word.

## Answer
Evaluator `FUN_14049fff0` (case 107) calls `mission.vtable+0x40` and masks it with `1 << (creature QuestChecklistIdx & 31)`. There is **one** concrete mission class (vtable `140b6c950`, the only vtable stored by the factory `FUN_14056dce0` and by the init-list builder `FUN_14056da50`; base `140b6cae0`). Its slot `+0x40` is `FUN_140575a80`, which returns object field **`+0x1c`** for every mission type. So the answer to "which function per mission type" is: the same function; the types differ only in how `+0x1c` is *interpreted*.
`+0x1c` is the packet's **`ProgressCount`** (second 32-bit word), not `ProgressData` (`+0x20`, slot `+0x58`, a small flag word whose bit 0 feeds the mission-state virtual) and not `Completed` (`+0x24`).

## Packet chain (all native)
- **0x06BB ServerPathMissionUpdate**, registered in `FUN_14006c290` with reader `FUN_14008d580`: id 15 bits at row `+0`, 1 bit `+4` (Completed), 32 bits `+8` (ProgressCount), 32 bits `+0xc` (ProgressData). Identical to the server's `ServerPathMissionUpdate.Write`.
- **0x06BA ServerPathMissionActivate**, reader `FUN_14008d6b0` (u32 count, then `FUN_14008d600` rows of `0x18` bytes): the same four fields, then State (3 bits, `+0x10`) and GiverUnitId (`+0x14`). Identical to the server's `Mission.Write`. Consumer `FUN_14056ed20` passes `row[1], row[2], row[3]` to the factory, which passes them to `FUN_14056b0f0`: `+0x24 = Completed`, `+0x1c = row[2] = ProgressCount`, `+0x20 = row[3] = ProgressData`. The init list (`FUN_14056da50`, rows of 0x10 bytes) maps the same way.
- **Update consumer**: `FUN_14056f150` finds the mission and calls `FUN_14056ded0`, which calls the setter `FUN_14079d700(mission, Completed, ProgressCount, ProgressData)`: stores `+0x1c = ProgressCount`, `+0x20 = ProgressData`, `+0x24 = Completed`; a changed `+0x1c` raises the Advanced hook (`+0xb8`, event `PlayerPathMissionAdvanced`), a changed `+0x20` the `+0xc0` hook. Nothing else writes `+0x1c` after the constructor.
- `ServerPathMissionAdvanced` (0x00F4, `FUN_140572430`) does **not** touch `+0x1c`: it increments a counter on the mission's `'PEMP'` component (type 0x12 PowerMap missions only). Irrelevant to 107 (no 107 row is type 0x12).

## What the word means per mission type (`FUN_14056d0d0` "progress" and `FUN_14056b260` "bitmask flag")
- Bitmask-in-`ProgressCount` (progress = popcount, `+0x48`): types 3, 0xD (13), 0xE (14), 0x11 (17), 0x14, 0x17, 0x19, 0x1A, 0x1B (all these have `FUN_14056b260` true).
- Types 5 and 6 (Soldier Demolition/Rescue): bitmask only when the mission's `PathSoldierActivate` row (`PathMission.ObjectId`) has `SoldierActivateModeEnum` 1 or 2 (`FUN_140223380`, field `+0x10`); mode 0 is a plain count.
- Plain count (`+0x40` read as a number): types 2, 4, 7, 0x13, 0x15, 0x18 (the "+0x40 types" of the earlier note; they read the same field that the bitmask types popcount through `+0x48`). 107 reads the raw word regardless of type, so on these it tests bits of a number (legal, rarely meaningful).
- Lua names for the enum (recovered from the constant table in `FUN_140679e50`, double stores; two entries read from disassembly): 0 Soldier_Holdout, 2 Scientist_Scan, 4 Soldier_Assassinate, 5 Soldier_Demolition, 6 Soldier_Rescue, 7 Soldier_SWAT, 8..11 Soldier/Settler/Scientist/Explorer_Script, 12 Explorer_Door, 13 Explorer_ScavengerHunt, 14 Scientist_ScanChecklist, 15 Explorer_Vista, 16 Explorer_ExploreZone, 17 Explorer_ActivateChecklist, 18 Explorer_PowerMap, 19 Settler_Hub, 20 Scientist_FieldStudy, 21 Settler_Infrastructure, 22 Scientist_Experimentation, 23 Scientist_SpecimenSurvey, 24 Scientist_DatacubeDiscovery, 25 Settler_Mayor, 26 Settler_Sheriff, 27 Settler_Scout. (Script I:\tools\scripts\LuaDoubleConsts.java; its value pairing was off for some entries, so 8, 12, 13 were fixed by disassembly; 0, 2, 4, 7, 16, 18, 19, 21, 24 agree with the server's `PathManager` constants. The server's `ExplorerNodeMissionType = 0x0F` is Lua Explorer_Vista, a naming difference only.)

## Does server `ProgressData` equal the client word? No, for every type
Server `PathManager` writes `ProgressData` only as a 0/1 in-progress marker (`PathManager.cs` lines ~805, 1116, 1201, 1249, 1292; 0 on completion) and never builds a checklist bitmask there. The client word is `ProgressCount`, which the server fills as a plain counter (`+1` capped at the required count; `max(count,1)` on completion) for types 2, 4, 7 and 19 only. There is **no server producer at all** for types 5, 6, 13, 14, 17 (no reference to `PathSoldierActivate`/`PathExplorerActivate` outside the table loader), i.e. every type that carries 107 rows. `TryIsPathMissionChecklistItemComplete` (type 220) uses `ProgressData` and a completed-means-all-true shortcut; that is not the 107 word.

## Census of the 124 slots (123 prerequisite rows; Equal = comparison 1, NotEqual = 2; Value is unused)
| Mission type | Slots | Distinct missions | Equal / NotEqual | Notes |
|---|---|---|---|---|
| 5 Soldier_Demolition | 28 | 23 | 8 / 20 | 24 slots bitmask mode (activate mode 1: 21, mode 2: 3), 4 plain count (mode 0) |
| 6 Soldier_Rescue | 21 | 12 | 6 / 15 | all mode 1 (bitmask) |
| 17 Explorer_ActivateChecklist | 37 | 19 | 17 / 20 | bitmask |
| 14 Scientist_ScanChecklist | 11 | 9 | 6 / 5 | bitmask |
| 13 Explorer_ScavengerHunt | 4 | 3 | 0 / 4 | bitmask |
| 2 Scientist_Scan | 1 | 1 | 0 / 1 | plain count |
| mission id absent from PathMission.tbl | 22 | 10 ids (incl. 0) | 9 / 13 | retired ids: mission lookup fails, word 0 |
Total 124 = 102 resolved + 22 absent. So 97 slots are bitmask-mode, 4 plain-count, 22 constant (Equal false, NotEqual true). The earlier note's "13 rows of types 2/14" is 12 (1 + 11).

## Proposed server check (inferred)
`PrerequisiteCheckPathMissionCreatureChecklist` (enum `QuestObjective7`, same subject/short-circuit style as the new 119 check): evaluated unit is the target creature if present, else the player; Player/Ghost subject -> Equal false, NotEqual true; mission = player's `PathManager` mission `objectId` (absent -> word 0); `idx == -1` (byte 255) -> word 0; else `word = ProgressCount` via `IPathManager.TryGetMissionProgress`, `hit = (word & (1u << (idx & 31))) != 0`; Equal = hit, NotEqual = !hit, other comparisons false. Do **not** use `ProgressData`, and do **not** reuse type 220's completed shortcut (a completed mission in the client keeps its mask).
Tests: bit set/clear; idx 255 and -1; idx 32 wraps to bit 0; missing mission; mission id 0; Player and Ghost subject (Equal false, NotEqual true); NotEqual negation; `ProgressData` bit set but `ProgressCount` clear -> false (guards the corrected field); target creature chosen over the player.
Caveat: until producers exist (follow-up, not native-known: set the creature's `QuestChecklistIdx` bit in `ProgressCount` and send `ServerPathMissionUpdate` when the activate/scan/clue target is used, for Soldier Activate modes 1-2, Explorer ActivateChecklist/ScavengerHunt, Scientist ScanChecklist) the check reads word 0 for those missions: Equal false, NotEqual true.

## Remaining unknowns
1. Sign handling of idx: native tests only `== -1`; any other value is masked with `0x1f` (a byte >= 128 other than 255 is unverified).
2. Meaning of `SoldierActivateModeEnum` 1 vs 2 and how retail assigned bit positions (by creature `QuestChecklistIdx` is implied by 107, not shown by a producer).
3. Retail server behaviour on completion (mask kept or reset) is not observable from the client; the client keeps whatever the last update carried.
4. Opcode of the `FUN_14056da50` init list was not tied to a registration (0x06BA/0x06BB are).
5. Handler registration for 0x06BA/0x06BB was not found (readers are registered in `FUN_14006c290`; handlers are bound elsewhere), the consumer pairing rests on field structure.

Review and implementation (lead, 2026-11-08): the server side of the finding was re-checked (`PathManager` writes `ProgressData` only as a 0/1 in-progress marker at the lines that also bump `ProgressCount`; `TryGetMissionProgress` already returns `ProgressCount`). The native claims (vtable `+0x40` returning field `+0x1c` for every type, the 0x6BA/0x6BB packet chain) are the agent's reading and were not re-derived by the lead. Implemented `PrerequisiteCheckPathMissionCreatureChecklist` (type 107): Player/Ghost subject with no or a Player/Ghost target gives Equal false / NotEqual true; otherwise `ProgressCount & (1 << (idx & 31))` for the target creature's `QuestChecklistIdx` (signed byte, negative = none), 0 for a mission the player lacks. Tests `PathMissionCreatureChecklistPrerequisiteTests` (11 cases; written before the class, run after it); Prerequisite+PublicEvent+Path filter 3,880 cases pass (Source/SQLite scope). The server produces no checklist bitmasks yet for mission types 5, 6, 13, 14 and 17 (97 of the 124 slots), so those slots read word 0 (Equal false, NotEqual true) until producers exist; the earlier inference that server `ProgressData` equals the client word is refuted. Open: sign handling for idx values other than -1, the meaning of soldier activate modes 1 and 2, the handler registration of opcodes 0x6BA/0x6BB.

