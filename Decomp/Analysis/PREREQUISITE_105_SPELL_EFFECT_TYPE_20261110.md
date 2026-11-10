# Prerequisite type 105 and the `_TableN` handler labels (2026-11-10)

Evidence tier: **native body read** (fresh Ghidra 12.0.4 project of `WildStar64.exe`, `sha256:231bb2bb...96b3e5`, decompile of `PrerequisiteManager_EvaluateTypeSlot` `1404a2100`) plus **raw table rows**
(`wildstar_client.prerequisite`, build 16042). Source tests only. No retail parity claim.

## 1. What the evaluator does for type 105
`PrerequisiteManager_EvaluateTypeSlot` switches on the type word of the slot (`param_3[0]`). Case `0x69` is inline:

* a null entity returns 0;
* it walks the list at entity `+0x15d0` (next pointer at node `+0x30`), calls each node's virtual `+0x38` and compares the result with `param_3[2]`;
* comparison (`param_3[1]`) 1 returns whether a node matched, 2 returns the negation, any other comparison returns 0.

Which slot word is which (Observed plus Correlated): case `0x34` (type 52, Path) passes `param_3[1]` and `param_3[3]` to its handler, and the 552 retail Path slots carry the path in `value` (0 to 3). So
`param_3[1]` is the comparison, `param_3[3]` is Value and `param_3[2]` is ObjectId. Type 105 therefore compares against **ObjectId**.

Retail rows agree: all 7 type 105 slots have `value = 0` and the id in `objectId`.

| Prerequisite id | Slots (comparison, objectId, value) |
|---|---|
| 5631 | (1, 0, 0) |
| 7405 | (1, 83, 0) |
| 7909 | (2, 31, 0) |
| 11099 | (2, 3, 0) |
| 37231 | (2, 31, 0), (2, 100, 0), (2, 112, 0) |

Not read natively: what the node's virtual `+0x38` returns. That it is a `SpellEffectType` is **correlated** from the ids (3 ForcedMove, 31 Disguise, 83 Stealth, 100 MimicDisguise, 112 DisguiseOutfit; row 37231 reads as
"not disguised"). Row 5631 uses id 0, which is not a named effect type; unexplained.

## 2. Server change
`PrerequisiteCheckSpellEffectTypeOnUnit` read the effect type from `value`, so every retail slot asked for effect type 0. It now reads ObjectId and applies the native comparison rule (Equal, NotEqual, otherwise
false; a missing unit is false). Test: `PrerequisiteNativeComparisonTests.SpellEffectTypeOnUnitReadsTheEffectTypeFromObjectId`. Unchanged and unverified: the server's notion of an "active effect of a type"
(persistent effects of the unit's pending spells) as the analogue of the `+0x15d0` node list.

## 3. `14049fd50` is the Path handler, not a type 105 handler
`14049fd50` is referenced only from the pointer slot `140b67bb8`. Evaluator case `0x34` calls manager vtable `+0x478`, and `140b67740 + 0x478 = 140b67bb8`. Its body compares `*(int *)(DAT_140c65970 + 8)` with the
Value word, which matches `PREREQUISITE_NATIVE_COMPARISONS_20261107.md` (type 52 Path, `FUN_14049fd50`). The label is renamed `Prerequisite_CheckPath_LiveCase34`.
Not re-derived: that `140b67740` is the vtable the evaluator's `this` uses (taken from `PREREQUISITE_REMAINING_TYPE_GATES_20261107.md`; a second, 219-slot vtable at `140b67e20` is installed by `FUN_1404a1be0`
and `FUN_1404a5590` and was not examined). Supporting it: six labels that already cite "live case N vtable +off" (types 83, 149, 186, 220, 260, 268) all land on their slot with this base.

## 4. The `table[N] @ 0x140b67870` labels are indexed from a base that is not a table base
Nothing references `140b67870`; it lies inside the pointer run around the referenced vtable start. Labels named `_TableN` from that base are therefore attributed to the wrong type.
Mapping each such slot through the evaluator (generated from the decompile, not hand-read):

| Label (unchanged name) | Address | Evaluator case that calls this slot | What the evaluator does for the label's nominal type |
|---|---|---|---|
| `Prerequisite_CheckSpellTier_Table59` | `14049e820` | `0x14` (20), `+0x308` | 59: direct call to `Prerequisite_CheckSpellTier` |
| `Prerequisite_CheckSpellTierOnTarget_Table60` | `14049e850` | `0x2a` (42), `+0x310` | 60: inline |
| `Prerequisite_CheckPetMatchTarget_Table89` | `14049f3d0` | `0x2b` (43), `+0x3f8` | 89: `+0x160` -> `14049d4f0` `Prerequisite_CheckPetMatchTarget` |
| `Prerequisite_CheckMountVehicleType_Table90` | `14049f460` | `0x44` (68), `+0x400` | 90: `+0x2c0` -> `14049e730` `Prerequisite_CheckMountVehicleType` |
| `Prerequisite_CheckActiveSpellTargetMechanic_Table92` | `14049f5b0` | `0x5e` (94), `+0x410` | 92: `+0x168` -> `14049d5b0` `Prerequisite_CheckActiveSpellTargetMechanic` |
| `Prerequisite_CheckSpell4EffectCategoryOnUnit_Table93` | `14049f690` | `0x06` (6), `+0x418` | 93: inline (walks entity `+0x15c8`) |
| `Prerequisite_CheckSpellCooldownNodeOnUnit_Table102` | `14049fb50` | `0x7c` (124), `+0x460` | 102: direct call to `Prerequisite_CheckSpellCooldownNodeOnUnit` |
| `Prerequisite_CheckSpellCooldownNodeOnTarget_Table103` | `14049fbe0` | `0xa0` (160), `+0x468` | 103: inline |
| `Prerequisite_CheckSpellCooldownNodeInServiceSet_Table104` | `14049fc70` | `0xb0` (176), `+0x470` | 104: inline |
| `Prerequisite_CheckTargetEntityLookupHit_Table106` | `14049fd80` | `0x35` (53), `+0x480` | 106: `+0x318` -> `14049e900` `Prerequisite_CheckTargetEntityLookupHit` |
| `Prerequisite_CheckActiveSpellEffectOnUnit_Table129` | `1404a0540` | `0x75` (117), `+0x538` | 129: inline |
| `Prerequisite_CheckActiveSpellEffectOnTarget_Table130` | `1404a0590` | `0x76` (118), `+0x540` | 130: inline |
| `Prerequisite_CheckLiveEventStateEqualsOne_Table144` | `1404a0b70` | `0xc7` (199), `+0x5b0` | 144: no case |
| `Prerequisite_CheckGroupIsRaid_Table62AndLive267` | `14049e9a0` | none found at `+0x320` by this parse | 62: `+0x4a8` -> `14049ff30` |

Each of the first thirteen rows in `function_labels.csv` now carries a "TYPE ATTRIBUTION CONTRADICTED" note. Their names and body descriptions were **not** changed and **not** re-read.

## 5. Open, not done here
* The functions in section 4 are the client bodies for types 20, 42, 43, 68, 94, 6, 124, 160, 176, 53, 117, 118 and 199. Whether the server checks for those types match them was not examined. Types 117 and 118
  are listed elsewhere as large unhandled public-event types; their bodies (`1404a0540`, `1404a0590`) should be re-read before the existing "active spell effect" descriptions are trusted.
* The inline cases for types 60, 93, 103, 104, 129 and 130 were not compared with their server checks. Only type 105 was.
* The case-to-offset table was produced by parsing Ghidra's C output for `1404a2100`; `14049e9a0` (`+0x320`) found no case, which contradicts its label's "live type 267" claim and may be a limit of the parse
  (joined or fall-through cases).

Reproduction: `run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe -ExtraPostScript Gap8Decomp.java -ExtraPostScriptArgs @(<out>,'1404a2100')`, then
`function_pointer_family_slots.csv` for the slot owners; `FindDataReferences.java` on `140b67870`, `140b67bb8`, `14049fd50`.
