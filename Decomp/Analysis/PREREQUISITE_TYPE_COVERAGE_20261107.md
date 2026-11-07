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
