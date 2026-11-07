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
