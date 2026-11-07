# Prerequisite type coverage census (2026-11-07)

Inputs: build 16042 `DB\Prerequisite.tbl` (32,131 rows, 3 type slots each), `DB\PrerequisiteType.tbl` (273 rows: type id to the localized error-text id) and the English text
`en-US.bin` from `ClientDataEN.archive`, extracted read-only with `Tools/ArchiveProbe` (which gained an optional `--index=<file>` argument, default unchanged). Server side:
the `[PrerequisiteCheck(PrerequisiteType.X)]` registrations under `Source/NexusForever.Game` (146 types). Static grep; no test was run.

## Result

- 49,184 type slots across the table use 232 distinct types; **99 of those types have no server handler**, covering **9,965 slots (20%)** and **8,380 of 32,131 prerequisite rows (26%)**.
- `PrerequisiteManager` evaluates an unhandled type as **false** (it logs "Unhandled PrerequisiteType" and returns false), so content gated by one of these rows is silently unavailable or
  rejected. No name/case mismatch inflates the count (every handler name exists in the enum, and no unhandled type differs only by case).
- Unhandled slots by what the client's own error text says the type checks (text paraphrased): public event or public-event objective 3,264; generic "you cannot do that right now" 2,005
  (types 47, 44, ...); quest objective 1,306 (+124 quest object); path mission 693; challenge 429; tradeskill 303; holdout 297; distance 269; Eldan augmentation 241; difficulty 113; achievement 93; other 819.
- Largest individual unhandled types (id, slots, enum name): 118 (2,422, PublicEventObjective188), 47 (1,788, QuestObjective47), 22 (597, QuestObjective0), 117 (458, PublicEvent117),
  23 (379, QuestObjective1), 19 (268, TradeSkillProfession), 54 (256, PlayerPathMission), 231 (241, EldanAugmentation231), 74 (236, ChallengeLocked), 31 (215, CreatureWithinDistance),
  44 (214, InTargetGroup), 67 (191, PathMissionRequirement).

## What is and is not known

The error text gives the category a type checks, not its comparison semantics: how `PrerequisiteComparison`, `ObjectId` and `Value` combine for each type (for example whether type 22
tests an objective as complete, incomplete or at a count) is not in any table. Implementing a type from the text alone would be a guess, so none was implemented.

Native search (Ghidra project of `WildStar64.exe`): the client reads `DB\Prerequisite.tbl` and `DB\PrerequisiteType.tbl` in table-loader functions (`FUN_140225760`, `FUN_140225ba0`) but
the evaluator itself was not located. A scan for runs of at least 120 consecutive code pointers found 20 candidates in `.rdata`; the length-301 run at 0x140b6a9c0 is not the
handler table (entry 118 is a 3-byte stub). The evaluator is probably a large switch rather than a pointer table; finding it needs a decompile-side search (for example callers of the
Prerequisite table accessor) and is not done.

## Gate and next step

Gate: per-type comparison/object/value semantics, from the native evaluator (decompile) or a retail capture of an affected quest, vendor or reward. Next independent step: locate the evaluator by
following the callers of the Prerequisite table loader, then implement the largest types (118, 47, 22, 117, 23) one at a time with red tests from real table rows. Evidence tier of this record:
raw-table and text census plus static source grep.

## Discrepancy in the implemented quest-objective types (types 68 and 94)

Field-shape check against the retail rows (build 16042 tables, `ObjectId` tested against the Quest2 and QuestObjective id spaces):

| Type (enum) | Slots | `ObjectId` is a Quest2 id | `ObjectId` is also a QuestObjective id | Comparisons | Common `Value`s |
|---|---|---|---|---|---|
| 68 QuestObjective (implemented) | 2,132 | 2,122 (99.5%) | 954 (45%, about the density of that id range) | 1, 2 | 0, 1, 2, 3 |
| 94 QuestObjective6 (implemented) | 781 | 771 (98.7%) | 345 | 1, 2 | 0, 1, 2, 3 |
| 22, 23, 33, 34, 35, 36 (unhandled) | 597, 379, 152, 87, 74, 17 | 555, 362, 147, 84, 70, 15 | far below | mostly 1 (also 4, 5, 2) | 0, 1, 2, 3 |
| 47 QuestObjective47, 107 (unhandled) | 1,788, 124 | 48, 22 | 47, 7 | 1, 2 | large ids (47), 0 (107) |

`PrerequisiteCheckQuestObjective` and `...QuestObjective6` pass `ObjectId` to `QuestObjectivePrerequisiteHelper.IsObjectiveComplete`, which matches
`objective.ObjectiveInfo.Id == objectId` over the player's active quests. The data says `ObjectId` is a **quest** id for these types, so that lookup can only
match by numeric coincidence of two dense id ranges, and the check mostly evaluates "objective not found". `Value` is below the quest's listed objective count in 94% of the type-68
slots (objective-index-like), but equals the count in many of the remaining ones (completed-count-like); neither reading fits every slot, so the semantics (index, count, or state, per comparison)
cannot be chosen from the tables. Not changed: the existing tests encode the objective-id reading, and replacing it needs the native evaluator or a capture that shows which quests or
interactions a given row gates. This is a probable defect in an implemented check affecting 2,913 slots, recorded as evidence, not as a fix.


## Native evaluator located and quest-objective index family implemented (same day)

**Where it is.** A scan of every function of at least 3,000 bytes in the Ghidra project for a `switch` with at least 100 cases containing the labels 0x16, 0x2f and 0x76
found three candidates; one (`FUN_1404a2100`, 10,054 bytes, 271 cases) is the prerequisite evaluator: `uint FUN_1404a2100(this, unit[2], args[])` switches on `args[0]` (the
type) and forwards `args[1]` = comparison, `args[2]` = objectId, `args[3]` = value to virtual methods of its owner object (or handles the type inline). The other two hits
were an `EGenericError` code-to-text switch and a third unrelated switch. The vtable that holds the virtual implementations was not identified (the length-523 run at 0x140b6eb10 is a different class), so
the *bodies* of the virtual checks, and therefore the exact meaning of each type, are still unread; what is recorded below is **which arguments each type uses**, taken from the switch itself
(inline cases only; shared-label cases such as 22 to 35 are resolved separately).

Argument usage for the largest unhandled types (comparison, objectId, value, unit/target):

| Type | Enum name | Slots | Evaluator arguments used | Virtual slot |
|---|---|---|---|---|
| 118 | PublicEventObjective188 | 2422 | comparison, objectId, value | 0x540 |
| 47 | QuestObjective47 | 1788 | comparison, unit, value | 0x2f8 |
| 22 | QuestObjective0 | 597 | value | inline/shared |
| 117 | PublicEvent117 | 458 | comparison, objectId | 0x538 |
| 23 | QuestObjective1 | 379 | value | inline/shared |
| 19 | TradeSkillProfession | 268 | comparison, objectId, unit | 0x3d8 |
| 54 | PlayerPathMission | 256 | comparison, unit, value | 0x488 |
| 231 | EldanAugmentation231 | 241 | comparison, objectId, unit | 0x1a0 |
| 74 | ChallengeLocked | 236 | comparison, objectId, unit | 0x450 |
| 31 | CreatureWithinDistance | 215 | comparison, objectId, unit, value | 0x368 |
| 44 | InTargetGroup | 214 | comparison, unit, value | 0x370 |
| 67 | PathMissionRequirement | 191 | comparison, unit, value | 0x490 |
| 123 | PublicEventObjective123 | 184 | comparison, objectId, value | 0x568 |
| 155 | PathMission155 | 174 | comparison, objectId, unit, value | 0x508 |
| 125 | Challenge125 | 153 | comparison, objectId, unit | 0x458 |
| 33 | QuestObjective2 | 152 | value | inline/shared |
| 56 | DifficultyRankSlotAssigned | 124 | unit, value | inline/shared |
| 107 | QuestObjective7 | 124 | comparison, objectId, target, unit | 0x4b8 |
| 119 | PublicEventObjectiveObject | 122 | comparison, objectId, target, unit | 0x548 |
| 154 | Holdout | 119 | comparison, objectId, value | 0x4e0 |
| 143 | Difficulty | 113 | comparison, value | 0x3a0 |
| 153 | HoldoutWave | 95 | comparison, objectId, value | 0x4d8 |
| 201 | AchievementObject | 93 | comparison, objectId, unit, value | 0x430 |
| 34 | QuestObjective3 | 87 | value | inline/shared |
| 35 | QuestObjective4 | 74 | value | inline/shared |

Notes: type 47 ignores `ObjectId` (which is why none of its `ObjectId`s match any table); type 117 has no value and no unit; types 118 and 123 are public-event checks with no unit; type 54
uses only comparison and value (no `ObjectId`). Types 22, 23, 33, 34, 35 jump to one shared call; type 36 makes the same call directly.

**Implemented from this evidence.** Types 22, 23, 33, 34, 35 and 36 (`QuestObjective0..5`) all end in the same virtual call `(unit, comparison, objectId, index, value)` with index 0, 1, 2, 3, 4, 5. The retail rows
support the reading "progress of the active quest's objective at that index compared with Value": `ObjectId` is a Quest2 id in 555 of 597 (type 22) and similar shares for the others, and `Value` is at or below the
required count of that quest's objective at the index in 417/445, 291/308, 119/122, 65/66, 56/56 and 11/11 slots (types 22, 23, 33, 34, 35, 36), with all six comparison operators used.
`PrerequisiteCheckQuestObjective0..5` (new, `PrerequisiteCheckQuestObjectiveIndex.cs`) compare that progress with `Value`; a quest that is not active, or has no objective at the index, stays false, which is
how these types behaved while unhandled, so no previously true result changes. `PrerequisiteCheckQuestObjectiveIndexTests` (16 cases) failed to compile before the check classes existed and pass now; 919 Prerequisite cases pass
(Source/SQLite scope). 1,606 prerequisite slots (597+379+152+87+74+17) change from always-false to evaluated.

**Not changed, still open.** What retail does for a completed or abandoned quest (progress treated as the full count, or as unavailable) is unknown, so those cases stay false. The meaning of types 68 and 94 (`ObjectId` is a
quest id in the tables but the server reads it as an objective id) is not resolved by the argument shapes and remains the recorded discrepancy. The other large unhandled types (118, 47, 117, 19, 54, ...) need the virtual bodies.


## Types 68 and 94 resolved from the native bodies (same day)

The discrepancy recorded above is resolved. In the native evaluator, type 68 reaches `FUN_14049f460` and type 94 reaches `FUN_14049f5b0` (identified by the
call shapes: case 0x44 passes `(unit, comparison, objectId, value)` and case 0x5e additionally passes `target`; the older comment in the repo that placed type 94 at `14049f700` is
not supported, since that function is a different check, a state compare `virtual+0xc0 == value`). Both look up the quest with id `ObjectId` in the client's quest table
(`FUN_1405a8a40`, hash-map find keyed by quest id) and then call `FUN_1405510d0(quest, value)`: if `value` is not below the quest's objective count it returns false; if the quest's state is **3
(Completed)** it returns true; otherwise it returns bit `value + 2` of the player's per-quest objective mask for `value < 6`. Equal returns that result, NotEqual returns its negation, and any other
comparison returns false (type 94 additionally selects the unit bit and handles non-player units). So `ObjectId` is a **quest id** and `Value` is an **objective index**, matching the table
shape (quest ids in about 99% of slots, `Value` 0 to 3, comparisons only 1 and 2).

Fix: `PrerequisiteCheckQuestObjective` (68) and `PrerequisiteCheckQuestObjective6` (94) now use `QuestObjectivePrerequisiteHelper.IsQuestObjectiveIndexComplete` and `CompareCompletion`
(quest state Completed counts every index below the native bound of 6 as complete; an Accepted or Achieved quest uses that objective's completion; any other state or an index beyond the
quest's objectives is not complete). The previous behaviour (match `ObjectId` against active objective ids and compare a 0/1 with `Value`) was wrong for the retail rows; 2,913 slots are affected.
The test `QuestObjective_ComparesObjectiveCompletion`, which encoded the objective-id reading, was replaced by 12 discriminating cases plus two more (Completed quest, objective id is not a quest id); 8 of the
12 failed against the old code (the others pass either way). 930 Prerequisite cases and 4,182 Quest/Communicator/Prerequisite/Challenge cases pass (Source/SQLite; scopes overlap, not additive).

Limits: for Completed quests the objective count is not available from the quest manager, so the bound is the native 6, not the quest's real count; type 94's unit-type handling (NPC units) and per-unit
bit are not modelled (one evaluated player); the type 22 to 36 family's completed-quest behaviour is still unknown because its virtual body was not read.


## Public event types 117 and 118 implemented from the native bodies (same day)

With the native-body method above (match each evaluator call shape to a function in the base vtable at `0x140b67740`, then confirm with the table data), two more of the
largest unhandled types were read:

- **117** (slot +0x538, `FUN_1404a0540`, 458 slots): `(this, comparison, objectId)`; asks the global public-event manager (`DAT_140c65980`, virtual +0x48) about the event id, returns it
  for Equal and its negation for NotEqual. Retail: `ObjectId` is a PublicEvent id (457 of 458), comparisons 1 and 2 only. Modelled as "the event exists on the player's map and is not finished".
- **118** (slot +0x540, `FUN_1404a0590`, 2,422 slots, the largest unhandled type): `(this, comparison, value, objectId)`; finds the public event objective with id `objectId` through the same manager
  (an existence/active check, then a second lookup), reads its value (virtual +0x38, 0 when absent) and compares it for **equality** with `Value`; Equal and NotEqual only. Retail `Value` is 1 or 2 for
  about 97% of slots (1,689 and 658 of 2,422) with comparisons 1 (2,139) and 2 (283); `PublicEventStatus` (Inactive 0, Active 1, Succeeded 2, Failed 3) is the matching server enum, so the objective
  status is compared. That the native value is the status and not a count is inferred from this distribution, not read from the virtual body.

Implemented as `PrerequisiteCheckPublicEvent117` and `PrerequisiteCheckPublicEventObjective118` with `PublicEventPrerequisiteHelper` (map public-event manager, events, teams; the objective of a team the
player is a member of is preferred, otherwise the first match, otherwise Inactive). `PrerequisiteCheckPublicEventTests` (13 cases) did not compile before the classes existed and pass now; 5,330 Prerequisite/
PublicEvent/Quest cases pass (Source/SQLite). 2,880 previously always-false slots (458 + 2,422) are now evaluated.

**Alignment caveat.** The vtable alignment is validated by call shape and table data per type, not globally: the slot at +0x3d8 (type 19) resolved to a `void` function that cannot be the evaluator's uint-returning
call, the repo's older comments cite different addresses for 13 types (0 of 13 agree), and one other slot (+0x408, the type 22 to 36 family) resolved to a mismatching shape. Types 68, 94, 117 and 118 match on shape
and data; 54, 74 and 231 have matching shapes but were not implemented here (their lookups call helper functions whose objects are not yet identified).


## Types 231, 74 and 54 read but not implemented (same day)

- **231 (EldanAugmentation231, 241 slots, all `NotEqual` with `Value` 0)**: the body (`FUN_14049d7b0`) returns false unless the unit is the local player, then scans a player-held array of 16-bit AMP ids
  (count at +0x6df0, ids at +0x6df4) for `ObjectId` and returns found for Equal and not found for NotEqual. An implementation that mapped it to `ISpellManager.IsAmpEnabled` was written and **withdrawn**: the existing
  `AmpLifecycleTests.RegisteredPrerequisiteDispatchKeepsEnabledAndGlobalAmpListsSeparate` pins type 231 as a *different* list from the enabled-AMP list used by type 227, and it failed against the new check.
  The server has no model of the 231 list, so the type stays unhandled (evaluates false, which for NotEqual-only rows means the gated content stays unavailable). Needs identification of that list (what writes player+0x6df4).
- **74 (ChallengeLocked, 236 slots)**: `FUN_14049fa80` returns one 32-bit field of the challenge record found by `ObjectId` (`record+0x2c` of the entry in the client challenge table) for Equal, zero-test for NotEqual. That the field is
  "locked" is only the type's name and its error text; the field's meaning is not read, so it was not implemented.
- **54 (PlayerPathMission, 256 slots)**: `FUN_1404a1950` looks up a path mission by `Value` and returns completion-like state that depends on the mission's own sub-type (a state test for sub-types 4 to 7, a table membership test for sub-type 0,
  and a virtual call otherwise); not implemented because the sub-type dispatch is not mapped to server concepts.

Environment note: during this work `C:` became full (0 MB free). Builds, tests and Ghidra still ran by redirecting `TEMP`, `TMP`, `DOTNET_CLI_HOME` and the NuGet HTTP cache to `I:\tools\claude-tmp` for the individual commands, with `--no-restore -m:1 -nodeReuse:false -p:NuGetAudit=false`;
no files on `C:` were deleted.


## Type 19 implemented; type 47 is a client stub (same day)

- **19 (TradeSkillProfession, 268 slots)**: the Ghidra "void" signature of `FUN_14049f0f0` is a tail call into the shared compare helper. The body looks up the `TradeskillTier` row with id `ObjectId`
  (267 of 268 retail slots match a row), finds the unit's matching tradeskill and compares the unit's tier in it with the row's `Tier` using the comparison; `Value` is unused (255 of 268 are 0) and a unit without that
  tradeskill compares as tier 0. Retail comparisons: greater-or-equal 181, equal 66, less-than 21. Implemented as `PrerequisiteCheckTradeskillTier`, reusing `TradeskillPrerequisiteHelper.GetPlayerTradeskillTierRank`.
  `PrerequisiteCheckTradeskillTierTests` (17 cases) did not compile before the class existed and pass now; 1,039 Prerequisite/Tradeskill/AMP cases pass (Source/SQLite). The order of the compare helper's
  arguments (player tier versus row tier) is assumed from the data, where greater-or-equal dominates, not read from the helper.
- **47 (QuestObjective47, 1,788 slots)**: its slot in the only vtable holding the evaluator's methods (verified by scanning every data reference to the type-68 body: exactly one vtable, `0x140b67740`) is the shared
  `return 0` stub (`FUN_140001ba0`). The client therefore never evaluates type 47, so the client binary cannot give its meaning; the type is presumably server-side. Its retail rows (comparison 1 and 2, large `Value`s such as
  13855 and 22453, `ObjectId` unused) need a server-side witness. Left unhandled.


## Public event types 120 and 123 implemented; client-stub types identified (same day)

- **120 (PublicEvent120, 42 slots)**: `FUN_1404a0760`: the public event with id `ObjectId` must be active, then `Value` 0 accepts any phase and any other `Value` must equal the event's phase (virtual +0x170); Equal returns the
  result, NotEqual its negation. Retail: `ObjectId` is a PublicEvent id (41 of 42), comparison always Equal, `Value` 1 to 8 (the server's `IPublicEvent.Phase`). Implemented as `PrerequisiteCheckPublicEvent120`.
- **123 (PublicEventObjective123, 184 slots)**: `FUN_1404a09d0` tail-calls the compare helper with the objective's progress count (virtual +0x98, also the numerator of the type 162 percentage `+0x98 / +0xd0`);
  retail `ObjectId` is a PublicEventObjective id in all 184 slots, comparisons >= (86) and = (86) plus < and >, `Value` 0 to 5. Implemented as `PrerequisiteCheckPublicEventObjective123` (missing objective has count 0). The
  separate virtual used for type 118 (+0x38) versus this one (+0x98) supports reading 118 as status and 123 as count.
- `PrerequisiteCheckPublicEventPhaseCountTests` (14 cases) did not compile before the classes existed and pass now; 2,857 Prerequisite/PublicEvent/AMP cases pass (Source/SQLite). 226 more slots are evaluated.
- **Not implemented**: 119 (122 slots) and 162 (8 slots, a percentage: count/max × 100 compared with Value), 121 (0 slots) and 122 (1 slot) are small or involve the player's own participation maps (`player+0x3760/0x3780`) that
  the server does not model identically; the compare helper's argument order is assumed from the data for 123.
- **Client stubs (the evaluator slot is the shared `return 0`)**: 153 HoldoutWave and 154 Holdout (121 and 119 slots), 163 PublicEventParticipantCount, 164 PublicEventParticipant164, and 47 above. The client never evaluates these, so
  the client binary gives no semantics; they are server-side concepts and need a server-side witness.


## Type 31 (CreatureWithinDistance) implemented (same day)

- **31 (CreatureWithinDistance, 215 slots)**: `FUN_14049ed50` (evaluator slot +0x368) reads the unit position, walks the nearby-entity list and looks for an entity whose creature id (virtual +8) equals `ObjectId` and whose
  squared 3D distance from the unit is at most `(float)Value` squared. Equal returns found, NotEqual returns not found, other comparisons are false. Retail: comparisons Equal 112 and NotEqual 103; `Value` 128 (46), 10 (22), 32 (17),
  20 (16), 100 (13). Implemented as `PrerequisiteCheckCreatureWithinDistance` using the player's visible creatures (`GetVisibleCreature`) and `Position`.
- `PrerequisiteCheckCreatureWithinDistanceTests` did not compile before the class existed and pass now; 1,010 Prerequisite/AMP cases pass (Source/SQLite). The server's visible-creature set (grid visibility) stands in for the
  client's nearby-entity list; the two radii are not compared here, so far creatures (Value 128 against a smaller visibility range) are assumed equivalent. No retail parity claim.


## Type 44 (InTargetGroup) implemented for the creature-list and composite group types (same day)

- **44 (InTargetGroup, 214 slots)**: `FUN_14049ee40` returns false when the unit or the argument is 0, evaluates a TargetGroup against the unit with `FUN_1403b4a20` (0 means member) and returns Equal: member, NotEqual: not
  member, other comparisons false. The group id is `Value` (214 of 214 retail slots are TargetGroup ids; `ObjectId` is 0 in 213). The group evaluator switches on the group's `Type`: 1/2 creature id in/not in the seven data ids
  (unit virtual +0x10, the first int of the unit's creature record), 3/4, 5/6, 7/8 and 12/13 the same shape against other unit fields (virtuals +0x30, +0x20, +0x28, +0x38 reading `unit+0x118`, `+0xd8`, `+0xdc`,
  `+0xd0`), 9 a list test against `unit+0x140`, 10 every listed sub-group (skipping empty slots and its own id) and 11 any one of them. Retail slots by group type: 1 (146), 11 (48), 3 (16), 12 (3), 10 (1).
- Implemented as `PrerequisiteCheckInTargetGroup` for group types 1, 2, 10 and 11 (195 of 214 slots plus any composite of them); a group that depends on types 3 to 9, 12 or 13 (19 slots, whose unit fields are not identified)
  meets neither comparison. The subject follows the `CreatureDifficulty` precedent (`parameters.Target ?? player`, explicit `MeetsUnit`); a player has no creature id, so type 1 groups only match when a creature target is supplied.
- `PrerequisiteCheckInTargetGroupTests` (21 cases) and the 1,010 existing Prerequisite/AMP cases pass (1,031 total, Source/SQLite). The test and class were written together, so this slice did not observe a red run first.
  Whether retail evaluates the player or a target as the unit for these slots is not established. No retail parity claim.


## Type 143 (Difficulty) implemented; 201 and 124 bodies read (same day)

- **143 (Difficulty, 113 slots)**: `FUN_14049ef70` compares the singleton field `+0x6424` with `Value` (Equal: equal, NotEqual: not equal, others false; `ObjectId` unused). That field sits next to `+0x6428` (prime level) and `+0x642c`
  (level-scaling flag), the same trio the server sends in `ServerInstanceSettings` (`Difficulty`, `PrimeLevel`, `WorldForcesLevelScaling`), and INITIAL_FINDINGS names `+0x6424` "worldDifficulty". Retail rows are Equal only with
  `Value` 1 (74) or 0 (39), matching `WorldDifficulty` Veteran/Normal (a path-type reading, 0 to 3, would show larger values). Implemented as `PrerequisiteCheckWorldDifficulty` over `Player.InstanceDifficulty`.
  `PrerequisiteCheckWorldDifficultyTests` (10 cases) did not compile before the class existed and pass now; 1,041 Prerequisite/AMP cases pass (Source/SQLite). The wire-field-to-singleton mapping is inferred from field adjacency, not
  traced through the packet handler.
- **201 (AchievementObject, 93 slots)**, read not implemented: `FUN_14049f810` looks up the achievement with id `ObjectId` in the client's achievement map (`DAT_140c65c28+0x70`), calls `FUN_140642a00(achievement, Value)` and
  returns it for Equal, its negation for NotEqual. Retail: NotEqual 71, Equal 22, `Value` 0 to 5 (checklist-style index), `ObjectId` an achievement id. `FUN_140642a00` (probably "checklist item `Value` is complete") is not decompiled yet.
  Next step: decompile it and map to `AchievementManager` checklist state.
- **124 (ChallengeCompletionCount, 7 slots)**, read not implemented: `FUN_14049fb50` looks up the challenge with id `ObjectId` (`DAT_140c65948+0x30`) and compares its record field `+0x28` (completion count) with `Value`
  through the shared compare helper. Too small to prioritise.
- **67, 61, 110, 155 (path missions)**: bodies at `FUN_14049fe10`, `FUN_14049fef0`, `FUN_1404a00e0`, `FUN_1404a0360` read through a path-mission manager (`DAT_140c65970`, lookup `FUN_1403d7bc0`); 67 returns a mission field
  (virtual +0x38) as a boolean for Equal, 61 uses a helper count (`FUN_1404a1aa0`), 110 and 155 are tail-call shapes whose arguments Ghidra lost. Needs the path-mission manager mapped before implementing.


## Type 201 (AchievementObject) implemented as an achievement checklist-bit test (same day)

- **201 (AchievementObject, 93 slots)**: `FUN_14049f810` looks up the player's record of achievement `ObjectId` and returns `FUN_140642a00(record, Value)`: bit `Value` of the record word at `+0x14` for 0 to 31, of the word at
  `+0x18` for 32 to 63, and 0 beyond (a debug break path). Those two words are the `ProgressState` and `CreditedChecklistMask` the server writes in the `Achievement` network record. A missing record gives 0. Equal returns the bit,
  NotEqual its negation, other comparisons false. Retail: NotEqual 71, Equal 22, `Value` 0 to 11 (all inside the first word), `ObjectId` an achievement id.
- Implemented as `PrerequisiteCheckAchievementChecklistBit` plus `IBaseAchievementManager.IsChecklistBitSet` (`BaseAchievementManager`). `AchievementChecklistBitPrerequisiteTests` (16 cases) did not compile before and pass; 1,346
  Prerequisite/AMP/Achievement cases pass (Source/SQLite; this scope is wider than the 1,041 above and must not be added to it). For checklist achievements the server keeps the completed mask in `ProgressState`; for
  count-style achievements `ProgressState` is a counter, so a bit test there reads counter bits exactly as the client would. Only the first word is observed in retail data. No retail parity claim.


## Path mission types 61, 67 and 155 implemented; 110 read (same day)

- The shared compare helper `FUN_1404a2090(comparison, actual, expected)` was decompiled: Equal/NotEqual, `3: expected < actual`, `4: expected <= actual`, `5: actual < expected`, `6: actual <= expected`, so the "actual first, Value second"
  order assumed earlier for types 19 and 123 is confirmed.
- **67 (PathMissionRequirement, 191 slots)**: `FUN_14049fe10` looks up the mission with id `Value` (a PathMission id in 174 of 191) through the path-mission manager (`DAT_140c65970`, `FUN_1403d7bc0`) and reads the mission's
  virtual `+0x38` (the flag type 61 counts, read as completion); Equal returns it, NotEqual its negation, missing mission 0. Implemented as `PrerequisiteCheckPathMissionRequirement` over `IPathManager.IsMissionComplete`.
- **61 (PathMissionCount, 25 slots)**: `FUN_1404a1aa0` counts the missions of the episode with id `ObjectId` (22 of 25 are PathEpisode ids, none 0) whose `+0x38` is set and whose definition's `PathMissionFlags` (`+0x34`)
  lacks 0x40 (one mission in the table has it), then compares count with `Value`. Implemented as `PrerequisiteCheckPathMissionCount` over the PathMission table's `PathEpisodeId` and `IsMissionComplete`.
- **155 (PathMission155, 174 slots)**: `FUN_1404a0360` takes the mission id from the fifth (stack) argument = `ObjectId` (PathMission id in 155 of 174), reads its state (virtual `+0xd0`, 0 when absent) and compares it with
  `Value`: 0 to 4, matching `PathMissionState` (NoMission 0, Unlocked 2, Started 3, Complete 4), Equal 138 and NotEqual 36. Implemented as `PrerequisiteCheckPathMissionState` with a new `IPathManager.GetMissionState`.
- **110 (PlayerPathMissionCount, 47 slots)**, read not implemented: `FUN_1404a00e0` looks up the mission with `ObjectId` (PathMission id in 40 of 47) and compares `mission.+0x48()` if `mission.+0x30()` is non-zero else
  `mission.+0x40()` with `Value` (1 to 8 for Equal; 20 to 100 for the other comparisons, so probably counts versus percentages). The mission class's vtable and those three virtuals are not identified, so the server field to
  compare is unknown. Gate: identify the PathMission runtime class vtable (`+0x30/+0x40/+0x48`).
- `PrerequisiteCheckPathMissionTests` (27 cases) and `GetMissionState_...` (PathManager) did not compile before and pass; 1,429 Prerequisite/AMP/Path cases pass (Source/SQLite; a different filter scope than earlier totals).
  The test classes for 61/67 were written before this slice's red build was run for them (the first run was already green). Completion meaning of `+0x38` is inferred from the count helper and the retail value pattern.


## Gates recorded for types 53, 54, 107, 119 and 162 (same day)

- **53 (PathEpisode, 33 slots; not handled)**: `FUN_14049fd80` is true when `Value` equals the id of the player's *current* episode (the client's current-episode object, `DAT_140c65970+0x58`, whose episode entry id is at `+0x48`).
  Retail rows are Equal only with `Value` a PathEpisode id (32 of 33). The server never stores a single current episode: `ServerPathSetCurrentEpisode` is sent once per newly activated episode (`activatedEpisodes`), and the
  client's rule for switching the current episode (zone change versus activation) is not read. Gate: the client's current-episode selection rule.
- **54 (PlayerPathMission, 256 slots; not handled)**: `FUN_1404a1950` looks `Value` up in the *current episode's* mission map (same `+0x58` object, so it needs the same current-episode rule), then returns `mission.+0x28()`; when the
  global flag at `DAT_140c65970+8` is 0 it first requires `+0x28() != 0` and branches on the mission definition's `PathMissionTypeEnum` (`+0xc`): type 0 tests whether the mission's `ObjectId` is in a map at `+0x68`, types 4 to 7 return
  bit 0 of virtual `+0x58`, others fall through to `+0x28()`. The meanings of `+0x28`, `+0x58` and the `+0x68` map are not identified. Gate: current-episode rule plus the PathMission runtime class vtable (same gate as type 110).
- **107 (QuestObjective7, retail uses `target` and `unit`)**: `FUN_14049fff0` reads quest `Value`'s bit mask (`FUN_1403d7bc0` quest lookup, virtual `+0x40`) at the bit index in `unit+0x35f8` (the unit's slot in the
  event/quest participation); units of type `0x14`/`0x17` short-circuit (NotEqual true). **119 (PublicEventObjectiveObject)** `FUN_1404a0650` and **162 (percentage `+0x98/+0xd0`)** `FUN_1404a0a70` use the same per-player slot
  `unit+0x35f8` against the public-event manager (`DAT_140c65980`, virtuals `+0x30/+0x40/+0x68/+0x90`). The server has no per-player participation-slot index, so these stay unhandled. Gate: the participation-slot model
  (what assigns `unit+0x35f8`).


## TargetGroup evaluator corrected (types 1, 2 and 11) and type 196 (Racial) implemented (same day)

- Reading the wrapper virtuals behind `FUN_1403b4a20` while implementing type 44 showed that the server's existing spell-targeting evaluator (`TargetGroupCriteriaEvaluator`, used by `Spell` for `CastGroup` and `AoeGroup`)
  disagreed with the native body in two places, both checked against the retail tables:
  - **Types 1 and 2** were pass-through "FactionGroupId diagnostics". The native body compares a unit id (wrapper virtual `+0x10`) with the seven data ids, and the retail rows are Creature2 ids (19,555 of 19,740 type 1 ids and
    69 of 72 type 2 ids exist in Creature2). Type 1 is therefore a creature include list and type 2 its exclusion. `Spell4Base` references type 1 groups as the cast group of 2,227 spells and the AoE group of 624, so those spells
    previously accepted every target.
  - **Type 11** required every sub-group to fail. The native case `0xb` returns success as soon as one sub-group passes (and failure when none does); the retail type 11 rows list other TargetGroup ids (2,288 of 2,302 data ids
    are group ids, 1,845 of them type 1 creature lists), i.e. a union. The old rule inverted 284 cast groups and 292 AoE groups.
  - Both native loops skip empty slots and the group's own id, and a sub-group id with no row fails; the evaluator now does the same when a TargetGroup table is loaded (without a table the legacy pass-through remains).
  The evaluator's other mappings (types 3/4 faction, 5/6 race, 7/8 class, 9 creature list, 12/13 unit race) are unchanged. `TargetGroupCriteriaEvaluatorTests` encoded the superseded behaviour (types 1/2 always true,
  type 11 all-fail); those assertions were replaced with the native semantics and new cases (8 failed against the old evaluator first, then passed). 4,989 Spell/TargetGroup/Prerequisite/Quest cases pass (Source/SQLite).
  Which unit field wrapper `+0x10` reads (`*(unit+0x18)` first int) is taken to be the creature id because the data say so; the evaluator now relies on `IWorldEntity.CreatureId` for it. No retail capture compares an actual cast.
- **Type 44** now delegates to this evaluator (`PrerequisiteCheckInTargetGroup`), replacing the narrower first version (group types 1, 2, 10, 11 only): all 214 retail slots (group types 1, 11, 3, 12, 10) are handled; an unknown
  group row is not a member. `PrerequisiteCheckInTargetGroupTests` was updated (the "unmodelled type" case became faction-group cases).
- **196 (Racial, 40 slots)**: `FUN_14049c520` compares the first int of the pointer at `unit+0xd0` (0 when null; the same field TargetGroup types 12/13 test) with `Value`; all 40 retail Values are UnitRace ids, Equal 34 and
  NotEqual 6. Implemented as `PrerequisiteCheckUnitRace` over `CreatureEntry.UnitRaceId` (`parameters.Target ?? player`). `PrerequisiteCheckUnitRaceTests` (9 cases) were written together with the class (no separate red run).
- Also read: **49 Schedule** (68 slots) and **30 FacilityWithinDistance** (54 slots) point at the shared `return 0` client stub (`FUN_140001ba0`), so they are server-side concepts like 47/153/154/163/164. **149 State** (46 slots)
  tests `unit+0x1e4`/`+0x2ac` presence (Value 0/1) with unidentified fields; **141 IsOutOfBounds** (36) compares the unit position with a world-location record's radius (`FUN_14024b980(ObjectId)`, record `+0x4` radius, `+0xc` position,
  `+0x28` world id) and the current world; **146 ItemTradeSkillLevel** (34) and **252 Personal3V3ArenaRating** (27, reads a rating through `FUN_1405bffe0(…, 1, -1)`) need further field mapping.


## Quest objective family 22/23/33/34/35/36 re-audited against the native body (same day)

Evidence tier: **native body read** (Ghidra, `FUN_14049f4d0`, the vtable slot `+0x408` of `0x140b67740`), implementation checked only by Source/SQLite tests written from that reading. No retail witness.

The earlier implementation (see "Implemented from this evidence" above) compared the active quest's objective progress with `Value` and returned false for anything not active. That reading came from call shapes and table distributions; the body, which had not been read, differs:

- `FUN_1405a8a40(_, ObjectId)` is a hash lookup of the quest **definition** by id. A missing definition gives progress 0.
- `FUN_1405fbc40` returns the player's quest state. State **3 (Completed)** gives `FUN_140551840(definition, index)`: 0 when the index is at or beyond the definition's objective count or the objective is empty; otherwise the objective's required count `+0x10`, replaced by **100** when the objective type (`+4`) is in the bitset `0x400000018104` (types 2, 8, 15, 16, 46: the kill types), the count is at least 2 and flag `0x200` (`+8`) is clear.
- Any other state looks the quest up in the active-quest tree and calls `FUN_1406113b0(entry+0x28, index)` for the objective's progress; **not active gives 0**. That function also rescales kill-type progress by `100 * x * DAT_140b79d04` (0.001f) when the raw value is above 1 and flag `0x200` is clear.
- In every case the result is passed to `FUN_1404a2090(comparison, progress, Value)` (tail jump; the register setup is `EDX=comparison, R8D=progress, R9D=Value`). So a quest the player has not started compares progress **0**; it does not fail outright.

Slots affected (join of Prerequisite.tbl, Quest2.tbl `Objectives[index]` and QuestObjective.tbl): of 1,306 family slots, 225 point at an index whose objective id is 0 or missing and 73 at a quest id not in Quest2; of the 1,008 slots whose objective resolves, 207 have `Value` 0. Under the old rule all of these were false; under the native body they compare 0 (so `Equal 0`, `LessThan n`, `LessThanOrEqual n` and `NotEqual 0` follow the comparison). 30 slots are kill-type objectives of the percent kind, 18 of them `Equal`.

Change: `QuestObjectivePrerequisiteHelper.GetObjectiveProgress` implements Completed (required count, 100 rule), active (`IQuestObjective.Progress`) and zero otherwise; the six checks now take `IGameTableManager`. The percent rescale of the active kill-type path is **not** applied: the unit of the client's runtime value behind virtual `+0x40` (objective class not identified) is unknown, so the server's own progress is compared unscaled for those 30 slots, as before. Next experiment: identify the runtime objective class vtable (referenced by `FUN_1406113b0`'s array at `param_1[idx+2]`) and read virtual `+0x40`.

Tests: `PrerequisiteCheckQuestObjectiveIndexTests` rewritten (30 cases, adding not-started, missing-index and Completed-quest cases). The previous test `AQuestThatIsNotActiveOrAnObjectiveThatDoesNotExistStaysFalse` encoded the superseded behaviour and was replaced; the red run was a compile failure (the checks had no table argument), not a behavioural failure. 4,128 Prerequisite/Quest Source/SQLite cases pass (separate scope from the 30).


## Bounds, entitlement count and PvP flag (same day)

Evidence tier: **native body read** (Ghidra, bodies at the vtable `0x140b67740` slots below); implementation checked by Source/SQLite tests written from that reading. No retail witness. The tests
for these three were written before the classes but the first build was already green, so no red run was observed.

- **141 IsOutOfBounds (36 slots, slot +0x280, `FUN_14049e540`)**: looks up the WorldLocation2 row `ObjectId` through `FUN_14024b980` (the function loads the table named `WorldLocation2`);
  a missing row, or a row whose world (`+0x28`) differs from the current world, returns false for every comparison. Otherwise `distance(unit position, row +0xc..+0x14) < row.+4 (Radius)`;
  Equal returns it, NotEqual its negation, other comparisons false. Retail: 35 Equal, 1 NotEqual. Implemented as `PrerequisiteCheckIsOutOfBounds` (same table access as type 16).
- **273 EntitlementCount (17 slots, +0x678, `FUN_1404a1670`)**: player-type units only (`unit+0x80` is 0x14 or 0x17); the count is `FUN_1403d2140(ObjectId)`: the Entitlement row's `MaxCount`
  (`+4`, 0 or no row means 0xffffffff) capped sum of the character list (`global+0x7f58`) and the account list amounts; the compare helper receives (comparison, count, Value) (register setup read
  from the tail jump). Retail: `ObjectId` is an Entitlement id (2 to 75) in all 17, mostly LessThan caps. Implemented as `PrerequisiteCheckEntitlementCount` (character plus account amount, capped).
- **217 PvpFlag (9 slots, +0x1f8, `FUN_14049db90`)**: compares `FUN_1403b48b0(unit)` (1 when bit 0 of `unit+0x15a8` is set, else, for unit types 3 and 7 with an owner, the owner's value) with Value;
  Equal and NotEqual only. Implemented as `PrerequisiteCheckPvpFlag` over `IPlayer.PvPFlag != Disabled` for `parameters.Target ?? player`; a unit that is not a player counts as unflagged (the owner
  rule for pets and summons is not modelled).
- `BoundsEntitlementPvpPrerequisiteTests` (27 cases) pass; 1,268 Prerequisite/Entitlement/PvP Source/SQLite cases pass in the same run scope (the 27 are included).

Read, not implemented:
- **126 PathHoldout (72 slots, +0x4c8)** is the shared `return 0` client stub (server-side concept, like 47/153/154/163/164).
- **111 HazardProperty111 (47 slots, `FUN_1404a03e0`)**: finds the hazard (`ObjectId` key, tree at `DAT_140c659a8+0x38`) and tests that field `+0x28` of its record is non-zero; Equal returns it, NotEqual its negation (true
  when the hazard manager is absent). The record's field `+0x28` is unidentified and `HazardStateCollection` is internal state of the server, so the mapping is not safe.
- **113 HazardProperty113 (36 slots, `FUN_1404a04f0`)**: computes a hazard property through `FUN_1404a6070` and compares it with Value; the property read is unidentified.
- **239 ActionBar (12 slots, `FUN_14049ca10`)**: player units only; `Value` is the bar index (< 4) and the test is bit 0 of the dword at `global+0x1448 + index*4` (Equal) or its negation; the
  global array is unidentified.
- **18 ScheduledEvent (17 slots, `FUN_14049ef00`)**: `Value` is looked up in a hash set at `global+0x72a0`; Equal when present, NotEqual when absent. The set (active scheduled events?) is not identified.
- **146 ItemTradeSkillLevel (34 slots, `FUN_1404a0f90`)**: looks up an item record and a tradeskill-level table (`FUN_1405be150`), scans for the level band that contains the unit's value, then compares; needs the two tables identified.


## Type 56 implemented as inferred; type 116 gated (same day)

Evidence tier: body **read**, array identity **inferred**. Implemented, inferred (see the Evidence Ladder in `CONTINUATION_GUIDE.md`): no retail parity claim. The tests were written together with the class; no red run.

- **56 (enum name `DifficultyRankSlotAssigned`, 124 slots)**: evaluator case 0x38 (inline, shared tail at `LAB_1404a22ec`) reads `Value` as a slot index; for an index below 28 the result is whether the pointer at
  `unit+0x2d8 + index*0x10` is non-null (0 otherwise); the tail returns it for Equal, its negation for NotEqual (the `EDX == 0` branch) and false for any other comparison; no unit gives false.
  Retail: comparisons Equal 64 and NotEqual 60, `ObjectId` unused (114 of 124 are 0), `Value` 0 to 24: 8 (21), 0 (17), 9 (12), 20 (12), 2 (10), 15 (9), 10 (6), 21 (6), 24 (6), 11 (5). The prerequisites are referenced from
  Item2 (35), Creature2 (58), Spell4 and Quest2 rows. That distribution matches the server's `EquippedItem` indexes (System 8, Chest 0, Augment 9, Bag3 20, Head 2, Shields 15, Implant 10, Gadget 11, bank bags 21 to 24), so
  the array is read as the unit's equipped-item slots. **That identity is inferred**, not read from code: it would be confirmed by the code that fills `unit+0x2d8` (the equipment-update packet consumer) or a
  capture of an Item2 use gated by such a prerequisite. The enum name `DifficultyRankSlotAssigned` comes from an older guess and is not supported. Implemented as `PrerequisiteCheckEquipmentSlotAssigned`
  (player's `Equipped` inventory location, index at or beyond 28 is unassigned). `PrerequisiteCheckEquipmentSlotAssignedTests` (15 cases) pass; 1,128 Prerequisite cases pass in the same scope (the 15 are included).
- **116 (Stealth, 32 slots)**: case 0x74 returns `*(int*)(unit+0x54) > 1` through the same tail (Equal: true, NotEqual: false), `Value` and `ObjectId` unused. The field's meaning is known only from the
  failure text ("Stealth property requirement not met"); why the test is `> 1` (not a flag) is unexplained, so it is not mapped to `IsStealthed`. Gated on the unit field `+0x54`.


## Types 9 and 118: attempts and exact missing inputs (same day)

- **9 EpisodeState (34 slots, slot +0x420, `FUN_14049f700`)**: looks up an object by `ObjectId` in a global id-keyed hash table (`DAT_140c665d8`, via `FUN_1407242b0`), compares its virtual `+0xc0` with `Value`
  (Equal: equal, NotEqual: not equal; an absent object has state 0 for the compare). The 28 distinct retail `ObjectId`s all exist in `Episode.tbl` (300 rows, the smallest table containing all 28), `Value` is 3 in 33 of 34
  slots, comparisons Equal 32 and NotEqual 2. The server has no quest-episode runtime state, and the meaning of state 3 (completed?) and of which quests it counts is the body of the episode class's virtual `+0xc0`,
  not yet identified (the runtime class behind `DAT_140c665d8` was not located). Gated on that virtual and on a server episode model; `EpisodeQuest.tbl` (quest to episode rows) is the likely input.
- **118 (2,422 slots), second attempt**: the public event manager is `DAT_140c65980`; its methods at vtable `+0x30` (objective by id, then virtual `+0x38`) and `+0x40` could not be tied to a vtable. The vtables found by
  scanning references to `FUN_140498a40` (`0x140b67438`/`0x140b67508`, with slots `+0x68`, `+0xa0`, `+0x70`) belong to a different table-driven manager and do not match the 118 call shape. Next experiment:
  find the writer of `DAT_140c65980` (the reference list has a write at `0x140689523` outside a function; create the function and read the constructor to get the vtable) and decompile its slots `+0x30`/`+0x40`
  and the objective class's virtual `+0x38`. Until then the status-versus-count reading of 118 stays inferred (see the 117/118 section).
- **115 Unit (18 slots)** and **114 Vital (10 slots)** were read at case level only: 115 calls `FUN_1404a2010(unit, comparison)` (unit-dependent compare, `Value` unused by the call), 114 compares two unit property
  reads (`FUN_140466520(unit, ObjectId)` and `(unit, Value)`) through `FUN_1404a2010`; their helpers were not read.


## Type 118 and 123 confirmed by the Lua objective accessors (same day)

Evidence tier: native bodies plus the client's own Lua accessors. This closes the open point in "Public event types 117 and 118": whether the value compared by type 118 is a status or a count.

- The `Game.PublicEventObjective` method table (mapped in `INITIAL_FINDINGS.md`, "Eighty-eighth Game.PublicEventObjective method-table follow-up") binds `GetStatus` at `14068dd50`. Its decompiled body fetches the objective
  from the public event manager (`DAT_140c65980` vtable `+0x30`, the same lookup type 118 uses) and returns the result of **virtual `+0x38`** on the objective object.
- Type 118 (`FUN_1404a0590`) compares exactly that virtual with `Value`. So 118 compares the objective **status**, as implemented. The status constants registered in Lua are `Inactive 0, Active 1, Succeeded 2, Failed 3`,
  and the retail `Value`s are 1 (1,689 slots) and 2 (658), Equal 2,139 and NotEqual 283.
- `GetCount` (`14068e110`) reaches the raw counter through virtual `+0x98`, the virtual type 123 compares, which confirms 123 as the objective count.
- Status change: 118 and 123 move from "inferred from the value distribution" to read-confirmed. No code change was needed.
