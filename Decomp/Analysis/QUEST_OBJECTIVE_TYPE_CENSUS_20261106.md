# Quest objective type census (2026-11-06)

Input: build 16042 `DB\QuestObjective.tbl` (10,031 rows, 40 distinct `Type` values; SHA-256 matches
`ContractBuild16042Fixture.cs`, C3D31CEB...) against `QuestObjectiveType` references in `Source`
(Game, WorldServer). Static grep; census only, no tests run.

All 40 table types have a named enum member. Every type with rows is referenced by server code (for example
`TalkTo`, 350 rows, is driven from `InteractionObjectiveUpdater.cs` in WorldServer). The only rows whose type has
no server reference are 5 rows of unused or unknown types: UNUSED19 (2 rows), Unknown29 (2), Unknown27 (1). Quest
objective type coverage is therefore not a restoration gap; remaining quest work is per-objective producer/script
behavior (content-restoration queue), which is gated on retail or native witnesses.
