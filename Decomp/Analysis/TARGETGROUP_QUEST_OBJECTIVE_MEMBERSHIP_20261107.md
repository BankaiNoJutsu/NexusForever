# TargetGroup membership for quest objectives, activations and challenges (2026-11-07)

Evidence tier: **native body read** for the group semantics (`FUN_1403b4a20`, the client target-group evaluator, re-read this session) and **retail table consistency**
for how groups are used. The step from "the client evaluator defines group membership" to "the retail server credits quest objectives and challenges with the same
membership" is an inference with no retail capture behind it. Verification is Source/SQLite only.

## What the native body says (re-read)

`FUN_1403b4a20(wrapper, group)` returns 0 for a member and 0x59 for a non-member. It tests all seven data slots and **skips zero slots** (it does not stop at the first
zero). Type 1 / 2: unit id in / not in the list. 3 / 4: faction (`+0x30`) in / not in. 5 / 6, 7 / 8: player race and class. 9: list. **10: every non-zero sub-group (other
than the group's own id) must pass. 11: the first passing sub-group passes.** 12 / 13: creature unit race in / not in. A missing group row is a non-member.

## Retail use of type 10

All 22 type-10 rows combine different dimensions, which only makes sense as AND: for example `14113` = unit race in {184, 278} AND faction not in {219, 693, 197},
spell AoE group `436` = creature 5865 AND faction 521, `909` = faction list AND creature exclusion list. They are used as Spell4Base AoE/cast groups (13 spells) and by three
`KillTargetGroups` quest objectives (18981, 19319, 19321). The one row of four disjoint creature lists (`9035`) is not referenced by any consumer checked.

## Gaps found in the server

1. `AssetManager.AddToTargets` flattened target groups to creature ids and recorded any group with a faction, race or exclusion criterion as "unhandled". The quest
   objective cache still stored the partial list (or an empty one), and the creature-to-group cache skipped the group. Of the quest objectives that use a group, **38 have a group
   with such criteria** (types 3, 11 containing 3, 10, 12): their targets were empty or incomplete, so kills could not progress them. Simulated on the retail tables with the
   new resolver they resolve to 6 to 1,083 creatures each (all 38 non-empty; Creature2 `FactionId` is word 100 and `UnitRaceId` word 6 of the raw record).
2. Type 10 was flattened as a **union** in `AssetManager` and `ChallengeManager`, though the native body is an AND. Retail rows that mix a creature list with a faction
   cannot be expressed as a union.
3. `ChallengeManager.TargetGroupContainsCreature` returned false for every non-list type: 47 retail challenges target a group with such criteria (faction groups
   and composites; combat, checklist and one more type).
4. The evaluator itself stopped at the first zero slot. **18 retail rows** (for example `1`, `924`, `1740`, `7040`) list ids after a zero, so those ids were never members. Type 11
   row `924` (9158, 920, 0, 925) also stopped scanning sub-groups at the gap.

## Change

- `TargetGroupCriteriaEvaluator` evaluates a `TargetGroupSubject` (creature id, Faction2, player race/class, unit race); the `IWorldEntity` overload builds one. Zero slots are skipped.
- New `TargetGroupCreatureResolver`: groups made only of creature id lists (types 1, 9) with types 10 (intersection) and 11 (union) resolve by set algebra; every other group is
  evaluated against each Creature2 definition (faction and unit race from the definition; a definition has no player race or class). `IsMember(group, creatureId)` evaluates one creature
  for `ChallengeManager`.
- `AssetManager`: quest objective targets use the resolver. The creature-to-group index keeps indexing every list-only group and additionally indexes groups with attribute criteria **only
  when a quest objective refers to them** (bounds startup work; other such groups were never indexed and still are not).
- `ChallengeManager` uses `IsMember` (the private flattening was removed). A missing Creature2 definition leaves faction and unit race at 0, so list groups still work.

## Tests (Source/SQLite)

- `AssetManagerTargetGroupTests`: 6 new cases (unit race AND not-faction objective, faction include, creature list AND faction, two lists AND, no creature data, attribute group referenced by an
  objective is indexed). 5 failed first; the no-creature-data case is a control that passes either way. The existing "mixed nested criteria does not index the parent" case still holds (no
  Creature2 table, no referencing objective).
- `ChallengeTargetIdentityTests`: 3 new cases (faction group for combat and checklist, AND group); all 3 failed first.
- `TargetGroupCriteriaEvaluatorTests.Evaluate_CreatureIdListTypes_ScanPastZeroSlots`: 7 cases, 3 failed against the stop-at-zero loop.
- Scoped results: 1,081 Challenge/AssetManager/TargetGroup cases and 4,849 Quest/Spell/Prerequisite cases pass. The scopes overlap and are not additive.

## Not proven / next evidence

- Whether the retail server credits kill/activate objectives and challenge progress with the client's group semantics. Corroboration would be a retail capture of a kill of a creature in such a
  group (for example objective 19319) or the server-side group-membership function.
- A creature's runtime faction can differ from its Creature2 `FactionId` (spawn overrides, faction changes). The index uses the definition.
- Player race/class criteria (types 5 to 8) cannot match a creature definition; no checked quest objective needs them.
- Spell AoE/cast groups with the corrected AND semantics are enforced by the evaluator at cast time (previous record); this change does not touch the spell path beyond the zero-slot fix.
