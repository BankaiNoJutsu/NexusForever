# Label coverage pass 1: Lua method tables (2026-11-08)

Goal: systematically raise the share of the client (`WildStar64.exe`, build 16042) that has a durable name, starting with the largest source that can be extracted mechanically. Evidence tier of the labels: the
method name is read from a string the client itself registers (Observed); the class prefix is a heuristic from the registering function's strings (Correlated at best). No label here is a behavioural claim.

## Baseline (measured with `scripts/SurveyLabels.java` on the project `I:\tools\ghidra_proj`, program `WildStar64`)
* 24,993 functions; 24,630 still carried the default `FUN_` name and 363 any other name. The tracked inventory (`coverage/function_label_inventory.csv`) counted 2,182 of 25,069 durable labels for the client
  (about 8.7%), 731 of 25,129 for `Houston64.exe` and 44 of 4,522 for `StsConnLib64.MT.dll`.
* Only 10 RTTI type descriptors exist (standard-library types), so class names cannot come from RTTI. 45,038 defined strings exist; 9,356 are referenced by exactly one function (2,336 of those functions were
  unnamed); that is the next candidate source, not used in this pass.

## Method
1. `scripts/FindPairRuns.java` scans every initialised non-executable block for runs of 16-byte records (pointer to a printable C string, pointer to a function entry), minimum three records: **193 runs, 3,180
   entries, 3,080 distinct functions** (69 functions are bound under several names). These are the `luaL_Reg`-style method tables the client registers for its Lua API (window, unit, quest, housing, ... classes), plus
   the Lua standard library tables.
2. `scripts/RunOwners.java` finds the code that references each run and lists the strings that code references. `scripts/RunOwners2.java` searched memory for 8-byte pointers to the 97 runs with no code reference
   and found none (those tables are reached by base-plus-offset arithmetic from a neighbouring table), so they stay unresolved.
3. `scripts/lua_table_labels.py` builds `Lua_<Class>_<Method>` when the registrar's strings give a confident class (first identifier-like string that starts with `Game.`, `Client.` or `Apollo.`, ends with `Lib`, or
   sits before `__index`, and all references agree), else `Lua_T<table address>_<Method>`. Existing durable labels are never overwritten. `scripts/lua_table_labels_append.py` appends the rows to
   `function_labels.csv` under a dated comment line.
4. Applied to the Ghidra project with the existing `scripts/ApplyNexusForeverLabels.java` (project backed up first to `I:\tools\ghidra_proj_backup_20261108`; result `applied=4977, created=80, localLabels=31,
   missing=21, duplicateRows=4`, the missing and duplicate rows are older labels). The evidence inventory was backfilled with `Update-FunctionEvidenceInventoryFromLabels.ps1` (2,753 rows added, status `Mapped` with the
   script's standard blocker text; the BOM that script adds to the file was removed) and `Get-DecompileMappingArtifactGaps.ps1` now reports `missingFunctionEvidenceRows: 0`.

## Result
* **2,661 new durable labels** (`function_labels.csv` 3,116 -> 5,777 data rows). Named functions in the project: 363 -> 4,834 of 25,073 (about 19.3%, counting every non-default name including imports and CRT); functions
  still on the default name 24,630 -> 20,239.
* Class resolved for 94 of 193 tables (1,880 functions, for example `Lua_Apollo_*` 498, `Lua_Unit_*` 275, `Lua_Quest_*` 88); 99 tables (1,200 functions) are `Lua_T<table address>_*` because the class could not be
  resolved. The largest are tables at `140c56a80` (148 entries, window methods such as `Close`/`IsVisible`), `140c59e50`, `140c58c00` and `140c59100`.
* 12 label names occur twice (for example three `Lua_Apollo_GridWindow_DeleteAll` rows for different tables that share a class string).

## Validation
* 419 of the 3,080 functions already had a durable label. Of the 410 whose old name is not a bare `FUN_` name, **385 contain the Lua method name** (94%); 11 old labels were `FUN_` names; 25 differ (list below
  as the first examples). Most differences are naming style (`Tradeskill_SendClientTradeskillLearn` for the Lua method `LearnTradeskill`), but two look like conflicts that need a review: `140001ba0`
  (old `ClientCraftingAbandon_WritePayload`, table entry `io` in the Lua loader list) and `1400d7b40` / `1400da180` (old `ClientDB_RegisterModelCamera` / `..._ByWeapon`, table entries `Close` / `IsVisible`).
  Either the old labels or the table alignment is wrong for those; the old label was kept.
* Not validated: the class prefixes (heuristic), and that every run is a method table (a run is only required to be three consecutive name-string/function pairs).

## Next candidates
1. The 1,683 functions that still have a uniquely referenced informative string (error and log text, table names, event names): propose names from the string and have a reviewer accept them.
2. Resolve the 99 unresolved tables by following the neighbouring-table arithmetic in their registrar functions, or from the Lua error strings in the method bodies.
3. The 20 `vftable` symbols and the packet registration tables are already covered by earlier passes; the quest, spell and prerequisite evaluators are labelled mostly by hand and remain a long tail.
4. Re-run `Export-FunctionLabelInventory.ps1` / `Get-DecompCoverageSnapshot.ps1` to refresh the coverage snapshot (not done in this pass; the numbers above come from `SurveyLabels.java`).
