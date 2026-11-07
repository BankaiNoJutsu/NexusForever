# Creature2 visibility prerequisite (2026-11-07)

Status: **Blocked**, not implemented. Evidence tier: table correlation only. Nothing in this record is a retail parity claim.

## Facts

- `Creature2.PrerequisiteIdVisibility` (record word 13) is non-zero for **4,420 of 53,137 creatures** (2,285 distinct Prerequisite rows). The server loads it (`CreatureInfo.PrerequisiteVisibilityEntry`) and
  never evaluates it: `GridEntity.CanSeeEntity` returns true and `Player.CanSeeEntity` only adds hand-written tutorial and Northern Wilds rules (`Player.Visibility.cs`, `Player.cs`).
- The prerequisite types behind those rows (slot counts across the 4,420 creatures): 6 (2,999), 250 (747), 11 (653), 68 (553), 43 (498), 118 (311), 94 (168), 1 (144), 7 (138), 155 (87), 69 (71), 117 (70). Types 6, 43, 68, 94,
  155 and 69 are quest or path state, so this is the quest phasing mechanism: an NPC is shown while a quest condition holds.
- The hand-written tutorial rules correspond to table rows: the hoverboard Holoring (73416) and Start arrow (73707) both carry prerequisite 41803 (OR of two type 43 slots, objects 10527 and 10532, value 3) and the combat
  spinning hologram (73619) carries 41795. The projector, barrier, booster, finish line and pressure plates carry none, so the table does not describe all of the server's tutorial hiding.

## Why it is not implemented

1. **Who evaluates it is unknown.** The client has its own copy of the row evaluator (`FUN_1404a1ca0`) and of the field. If the client evaluates the field for units it receives, a server-side evaluation is redundant;
   if only the retail server evaluated it, the server must, and must re-evaluate when quest state changes. The hand-written `ForceRemoveStarterTutorialEntityFromClient` suggests the client does not hide these units by
   itself, but that is a hint from the existing workaround, not proof.
2. **Fail-closed risk.** About 4,100 prerequisite slots still have no handler (type 47 alone is 1,788) and evaluate false. Applying a visibility check now would hide every creature whose row contains an unhandled type
   for every player. A safe version needs per-creature fallbacks (show on unhandled types) plus refresh on quest, path and public event state changes.
3. The row evaluator's `Target`/`Unit` semantics for visibility (viewer versus creature) are not read.

## Next evidence

- Find the client reader of the field: the Creature2 row accessor (descriptor `u_Creature2`) and the functions that read record offset `0x34` then call the prerequisite wrapper (`FUN_14049bf10` and friends are reached through the
  evaluator vtable at `0x140b67740`, slots `+0x18` to `+0x20`, so a vtable-call search is needed).
- A retail capture of a quest-phased NPC appearing after the quest step, with the quest state at the time.
- Independent of those: the prerequisite types above that remain unhandled (see PREREQUISITE_TYPE_COVERAGE_20261107.md).
