# Restoration gap 3: path mission checklist producers (2026-11-08)

Part of [RESTORATION_GAPS_GOAL_PROMPT.md](RESTORATION_GAPS_GOAL_PROMPT.md). Builds on [PREREQUISITE_107_PATH_MISSION_WORD_20261108.md](PREREQUISITE_107_PATH_MISSION_WORD_20261108.md) (agent-written native read of the mission
word, not re-derived). Evidence tier: table scans, Source/SQLite tests, and an **inferred** producer rule; no retail parity claim.

## What the data establishes
Joining `PathMission.PathMissionTypeEnum` with `PathMission.ObjectId` against the `Path*` tables (every ObjectId of the type matches a row):
| Mission type | Lua name | Missions | `ObjectId` table | Server before | Prerequisite 107 slots |
|---|---|---|---|---|---|
| 5 | Soldier_Demolition | 33 | `PathSoldierActivate` | no producer | 28 |
| 6 | Soldier_Rescue | 26 | `PathSoldierActivate` | no producer | 21 |
| 17 | Explorer_ActivateChecklist | 22 | `PathExplorerActivate` | no producer | 37 |
| 13 | Explorer_ScavengerHunt | 56 | `PathExplorerScavengerHunt` (+ `PathExplorerScavengerClue`) | no producer | 4 |
| 14 | Scientist_ScanChecklist | 57 | `PathScientistCreatureInfo` (the table type 2 uses; `ChecklistCount`) | no producer | 11 |
The server had no code that touched `PathSoldierActivate` or `PathExplorerActivate` at all, so these missions could not progress; 107 therefore read 0 for them.

## Implemented: activate checklist (types 17, 5, 6; 86 of the 124 slots)
* `IPathManager.ProgressActivateChecklistMissions(creature2Id, targetGroupIds, checklistIdx)` / `PathManager`: for each uncompleted mission of the player's path of type 17 (Explorer), or 5/6 (Soldier), look the
  `ObjectId` row up (`PathExplorerActivate` / `PathSoldierActivate`); the activated target matches when `Creature2Id` equals the entity's creature or `TargetGroupId` is one of its target groups.
  Bitmask missions (type 17 always; types 5/6 when `SoldierActivateModeEnum` is 1 or 2, the native `FUN_14056b260` rule) set bit `checklistIdx & 31` of `ProgressCount` once and complete when
  `popcount >= Count`; activate mode 0 is a plain count that increments on every activation. An update packet is sent for partial progress; completion goes through `CompleteMission`.
* Hook: `SimpleEntity.OnActivate` and `OnActivateCast` credit the activation with the entity's `CreatureId`, target groups (new `UnitEntity.GetCreatureTargetGroupIds`) and `QuestChecklistIdx`.
* Prerequisite 107 now reads a real bit for these missions: `TryGetMissionProgress` returns the mask the producer builds (tested).
Tests: `PathManagerTests.ChecklistActivation` (12 cases: distinct bits and completion, repeated target, target group, index wrap at 32, other path and completed mission ignored, soldier modes 1/2 bitmask
and mode 0 count, non-matching creature) were written before the method and first run after it (one `using` and one generic argument fixed, then green; not red). Filter Path|Prerequisite|SimpleEntity|Entity:
4,940 cases pass (Source/SQLite scope).

## Why the rule is inferred, and its limits
* Native facts (agent read): the client shows `popcount(ProgressCount)` as progress for these types, and 107 tests the bit `1 << (creature QuestChecklistIdx & 31)` of `ProgressCount`. Nothing in the client shows
  *when* the server sets a bit; "on activating a matching target" is the natural reading of the tables (`Creature2Id`/`TargetGroupId`/`Count`) and of the QuestChecklistIdx key, not a witness.
* `QuestChecklistIdx` comes from entity spawn data (`NonPlayerEntityModel`/world DB). Two targets with the same index (for example both 0 because unset) count once; retail assignment of bit positions was
  not observed.
* Only `SimpleEntity` activations are hooked. Other entity types activated through different handlers do not credit these missions. The hook itself (`ProgressPathChecklistMissions`) is not covered by a test; the
  PathManager method it calls is.
* The mission is progressed whenever it is not completed, without requiring the `Started` state (the same permissive rule as the Soldier assassinate hook); whether retail required the started state is unknown.
* No reset or loss of bits (retail behaviour on completion is not observable from the client).

## Not implemented (remaining 15 of the 124 slots, plus 22 constant ones)
* **13 Explorer_ScavengerHunt (4 slots, 56 missions):** needs the clue system (`PathExplorerScavengerHunt` start creature, up to seven `PathExplorerScavengerClue` rows with a radius, a relic spell); no server model.
* **14 Scientist_ScanChecklist (11 slots, 57 missions):** the progress would be a per-scan bit, but generic scanning does not feed missions on the server at all: the only caller of
  `ProgressScientistCreatureScanMission` is a Northern Wilds script. Needs the generic scan-to-mission link and the scanned creature's checklist index first.
* The 22 slots whose mission id is absent from `PathMission.tbl` stay constant (Equal false, NotEqual true).
