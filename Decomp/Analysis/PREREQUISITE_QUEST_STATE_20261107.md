# Prerequisite types 6 (QuestState) and 43 (IsObjectiveActive) (2026-11-07)

Evidence tier: **native bodies read**; the retail rows agree with the readings (id spaces and value ranges). Source/SQLite only. No retail parity claim.

These are the two largest handled types by retail slot count: **8,790** and **3,631** of 49,184 slots (25%). Both had been implemented from the type names and message texts, never checked against the native bodies.

## Type 6 QuestState

Native `FUN_14049f690` reads the player's state of quest `ObjectId` (`FUN_1405fbc40`: 0 for a quest with no record, 3 Completed, ...), **maps the states 4 to 7 to 0**, and compares the result for equality with `Value`; Equal returns it, NotEqual
its negation, any other comparison is false. The server's states are Unknown 0, Accepted 1, Achieved 2, Completed 3, Botched 4, Mentioned 5, Abandoned 6, Ignored 7. Retail `Value`: 3 (3,619 slots), 2 (2,172), 1 (1,809), **0 (1,151)** and 4 (39); Equal 6,360,
NotEqual 2,430.

The server compared the raw state: `QuestManager.GetQuestState` returns `null` for a quest with no record, and `null == (QuestState)0` is false, so the 1,151 "not started" requirements failed for Equal and passed for NotEqual, and a quest
that was mentioned, abandoned, botched or ignored never counted as state 0. `Value` 4 (Botched) can never match natively (4 maps to 0). The check now maps null and the four states to 0.

`PrerequisiteCheckQuestStateTests` (51 cases: every state and no record against `Value` 0 to 4, Equal and NotEqual, other comparisons, an id beyond 16 bits) failed 7 before the change (the no-record and mapped states).

## Type 43 IsObjectiveActive

Native `FUN_14049f3d0` takes `ObjectId` as a **quest id** (1,336 of 1,381 distinct retail ids are Quest2 ids; 636 happen also to be QuestObjective ids) and `Value` as the **objective index** (0 to 5: 0 in 1,307 slots, 1 in 988, 2 in 646, ...). The test is true when
(1) the quest definition exists and the quest is in the client's active quest tree (`FUN_140552f50`), (2) the objective at the index is not complete (`FUN_1405510d0`, the function behind types 68 and 94) and (3) the objective is available
(`FUN_140553790`); Equal returns it, NotEqual its negation, other comparisons false. Availability: an index at or beyond the quest's objective count is not available; an objective of type 0 is; with flag `0x2` (sequential) every earlier objective
without flag `0x20` (optional) must be complete; with flag `0x1` only, walking back from the previous objective, one that is neither complete nor itself available makes it unavailable; without those flags it is available. The server's
`QuestObjectiveFlags` names match these bits (RequiresPreviousObjectives 0x1, Sequential 0x2, Optional 0x20).

The server read `ObjectId` as a QuestObjective id (`IsActiveObjectiveId`) and compared an active flag (0 or 1) with `Value` (the index): for index 2 to 5 (about 1,300 slots) it could never hold, and for 0 and 1 it held only by accident of the
flag. Retail: Equal 3,497, NotEqual 134.

`QuestObjectivePrerequisiteHelper.IsObjectiveActive` implements the rule; `PrerequisiteCheckIsObjectiveActive` now takes `IGameTableManager`. `PrerequisiteCheckIsObjectiveActiveTests` (20 cases: plain, sequential, requires-previous,
sequential-optional and type-0 objectives, completion combinations, out-of-range index, completed and unknown quests) did not compile before the constructor changed (the red run was a compile failure) and pass now. The earlier
`IsObjectiveActive_UsesQuestManagerActiveObjectiveId` test encoded the objective-id reading and was replaced by these.

## Scope

4,429 Prerequisite/Quest Source/SQLite cases pass (separate scope; the 71 new cases are included). `IQuestManager.IsActiveObjectiveId` is no longer used by this check and was left in place.

## Not checked / open

- Whether the client's "active quest tree" (`FUN_140552f50`) includes quests in the Botched state is not settled; the server's `GetActiveQuests()` is used.
- The objective-availability rule is the client's; whether retail content relied on the server applying exactly it is not witnessed.
