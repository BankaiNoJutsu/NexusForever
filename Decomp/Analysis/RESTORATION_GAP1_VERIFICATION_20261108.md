# Restoration gap 1: verification of the 2026-11-08 changes (2026-11-08)

Part of [RESTORATION_GAPS_GOAL_PROMPT.md](RESTORATION_GAPS_GOAL_PROMPT.md). Scope: the consequences and untested paths of the prerequisite 24/25/232/9/149/146 handlers and the ChangePhase, ChangePlane
and ChangeDisplayName effects. Evidence tier: Source/SQLite tests and table scans with the server's own table models; nothing here is retail parity.

## Method
`scripts/PrereqRefScan` loads every table the server has a model for (`NexusForever.GameTable` models, the extracted build 16042 `DB\*.tbl`) and, for every field named `Prerequisite*`, counts the references to
prerequisite ids that contain a slot of type 9, 24, 25, 146, 149 or 232 (it writes the detail to `I:\tools\prereq_refs_detail.tsv` and the slot values to `prereq_values.tsv`). `prereq_2425_impact.py` and
`prereq_2425_bits.py` evaluate the 24/25 slots under the old and the new semantics. This replaces a first raw scan that matched any word of any table and produced many false hits (for example `Spline2Node`
appeared to reference 1,042 type 146 prerequisites; its `SplineId` column merely collides with small prerequisite ids), which also means earlier notes that 146/149 are "used by `Spline2Node`" were wrong.

## Findings
1. **24/25: no server-evaluated gate changed behaviour for a default player.** The server evaluates 24/25 only through `Spell4` (CasterCast, TargetCast, Aoe*, Persistence), `Spell4Effects` (Apply,
   Persistence), `Creature2` (Visibility, ActivateSpell: loaded but not evaluated, see item 3 and 7), `Achievement`, `CommunicatorMessages`: 54 slot references (the analysis set included the two
   `Creature2` columns; neither contributes a 24/25 reference). Under the old rule (entity spawn phase 0, caster and target in the same phase) and the new one
   (default mask 1) the results are identical for all 54 (24 false->false, 18 true->true for type 24; 12 false->false for type 25). The only change is for entities with a non-zero spawn `EntityEvent.Phase`.
   The earlier fear that Value-1 type 25 gates flipped to false does not apply to any server-evaluated column.
2. **The gates that need a non-zero bit can now be satisfied.** 36 server-evaluated Equal slots need bit 1, 2, 4, 5, 6 or 8 (type 24) or bit 4, 5 or 20 (type 25) (all in `Spell4Effects` apply/persistence
   columns plus 3 in `Creature2`/`CommunicatorMessages`); every one of those bits is added by some ChangePhase row, so none is structurally unsatisfiable. Whether the right spell reaches the right player is not
   tested. They were false before the ChangePhase work as well.
3. **Most 24/25 gates are client-evaluated.** The 2,212 `MapZoneHex`, 1,262 `MapZoneHexGroupEntry`, `Creature2` visibility and priority, `Spell4Visual`, `VisualEffect` and `ClientEvent` references are decided by the
   client from the words it receives in packet 0x0171, which now follow the ChangePhase effects. `Creature2.PrerequisiteIdVisibility` is set for 4,420 creatures (2,285 distinct prerequisites); the server loads it
   (`CreatureInfo.PrerequisiteVisibilityEntry`) but never evaluates it, which is correct only if the client does (it does not need the server for it).
4. **ChangePhase word mapping: weak support for the chosen mapping, not decidable.** The unequal rows: 61285 Transimulator Synchronization (5/1), 62696 Lights Out - Signal Symbiotes (1/4), 62920 Lights Out -
   Phase Test (1/0), 72278 Training (0/2), 75501 Dissipate (33/32), 45279 NWADV - Penalty Box Debuff (0x8001/0), 63307 and 63369 Tripping Buzzbings (4/13, 4/12). Only the penalty box row argues for it: it sets
   a high bit (bit 15) in DataBits00 and nothing in DataBits01, which fits "the penalised player perceives the penalty box" (`PhasesIPerceive`), while the opposite mapping would make other players see the
   penalised one in a phase the player does not perceive. The others fit either way. The mapping stays an assumption.
5. **Plane gates: only the NotEqual ones are server-evaluated.** The 224 `Spell4` CasterCast references to type 232 use prerequisites 16018, 35363, 36098 and 37819, each "plane word & 4 is not set" plus other
   slots, so a caster with plane bit value 4 (the 14 timed ChangePlane rows) can no longer cast those 224 spells. The Equal gates 28187 (Equal 4), 34027 (Equal 1) and 34083 (Equal 2) are referenced by no
   `Prerequisite*` column of a scanned table except `ClientEvent` (28187); 34027 and 34083 appear only in columns that are not named prerequisite fields (`TargetGroup`, `SpellPhase`, `Spell4Telegraph`), not
   scanned here. So bits 1 and 2 having no known producer does not affect any scanned server gate.
6. **146 cannot be true on the server today, for a reason outside the handler.** The 18 achievements whose `PrerequisiteIdObjective` contains a 146 slot are all achievement type 33 (confirmed independently
   with the server's `Achievement` model; 44 of 4,943 achievements have type 33). The server's `AchievementType` enum has no value 33, so nothing updates them, and `CanUpdateAchievement` evaluates
   `PrerequisiteIdObjective` without an item (`BaseAchievementManager.cs:316`), which the handler (correctly) treats as false. New sub-gap: type 33 achievements need an update producer that supplies the crafted
   output item. The vendor-sell and vendor-list callers named in the native read are client-side; the server has no evaluator for them.
7. **9 and 149 consumers.** 9: `CommunicatorMessages` (17, server-evaluated through `CommunicatorMessage`), `GossipEntry` (11), `Creature2` visibility (5, client). 149: `Spell4Effects` TargetApply 28 and
   CasterApply 2, `Spell4` TargetCast 5 and CasterCast 1 (server), `Creature2` ActivateSpell 32 and AnimState 102 (AnimState is client), `GossipEntry` 20, `VisualEffect` 2. No server code evaluating
   `GossipEntry.PrerequisiteId` or `Creature2.PrerequisiteIdActivateSpell00` was found (grep), so those 31 and 32 references are unverified.

## Tests added
`PlayerPhasePlaneStateTests` (7 cases, real `Player` constructed with a session proxy): default words, a phase change updating both words and sending 0x0171 once (a replaced record sends nothing), removal
restoring the default, the plane word and 0x0172 only when it changes (two effects holding the same bit), generic `RemoveTrackedSpellStates` removing the phase, plane and timed name changes of one spell and
leaving another spell's change and a permanent name alone, a non-matching removal changing nothing, and a timed name falling back to the previous one. They test existing code, so they passed on the first run
(not red). Not covered: the display-name resend to late observers in `Player.Visibility`, the plane resend after add-to-map, and the three effects through a full cast.

## Consequences for the goal list
* Gap 1 is closed except the three uncovered paths above and the unverifiable mapping (item 4).
* New sub-gap for the list: **achievement type 33** (44 achievements, 18 gated by 146) has no server producer.
* The Gossip and creature-activate prerequisite consumers (items 7) are unverified server paths worth a look when gossip or activation work is done.
