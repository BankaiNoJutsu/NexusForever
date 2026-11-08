# Label coverage pass 2: string anchors (2026-11-08)

Follows [LABEL_COVERAGE_PASS_LUA_TABLES_20261108.md](LABEL_COVERAGE_PASS_LUA_TABLES_20261108.md). Same project (`WildStar64`, build 16042), same tools. Evidence tier: the names are read from strings the client
itself references (Observed); the role of a function is its relation to that string, not a behavioural claim.

## Method
1. `scripts/UniqueStringFuncs.java` lists every still default-named function together with the strings only it references: **1,783 functions** (down from 2,336 before pass 1).
2. Two mechanical rules were taken from that list; free-text and Lua-field strings were not (too ambiguous to name safely):
   * **ClientDB registration template (362 functions).** Every function with a `DB\<Table>.tbl` string is exactly 515 bytes and has the same shape (a decompile of `1401e8580` shows the table descriptor
     object allocation and `DB\AccountCurrencyType.tbl` load); no table name repeats. Label: `ClientDB_Register<Table>`, the convention of the 32 older labels. Three functions have an extra descriptor string
     (`ChatChannel`, `Redacted` with `MTXCategory`, `WordFilterAlt` with `WordFilterChina`); the .tbl name is used and the extra string is in the comment.
   * **Lua event raisers (531 functions).** `ClientEvent_DispatchNamedEvent` (`1400ea3e0`, an existing label) is called from 1,043 sites; `scripts/EventCallers.java` reads the event-name string loaded just before
     each call. A still-unnamed caller whose call sites name one event (475) or two events (56) is labelled `LuaEvent_<Name>_Raise` (or `LuaEvents_<A>_<B>_Raise`); a name shared by several functions gets the function
     address appended (241 do). 50 of them are over 1,000 bytes, so the label says only that the function raises the event, and the comment says it does other work as well. Callers with more than two
     events, or with a call whose string could not be read (11), were skipped.
3. `scripts/string_anchor_labels.py` builds the rows; `scripts/string_anchor_labels_append.py` replaces the older placeholder rows and appends the new ones under a dated comment line.

## Placeholder rows replaced
`function_labels.csv` contained 464 `WildStar64.exe` rows named `FUN_xxxxxxxx` whose comment is "Consumer dispatch candidate - may route server->client packets" (an earlier automatic candidate pass). They
counted as durable labels but named nothing, and the comment was wrong for the 362 table registration functions (the decompiler header showed it). 369 of them were at addresses labelled by this pass and were
removed; the other 95 remain (they are still placeholders and are not counted below).

## Result
* **893 new durable labels** (362 `ClientDB_Register*`, 531 `LuaEvent*`). `function_labels.csv` went from 5,777 to 6,301 data rows (893 added, 369 placeholders removed); the gap report counts 6,302 durable function label rows.
* Named functions in the project: 4,834 -> **5,727 of 25,073 (about 22.8%)**; functions on the default name 20,239 -> 19,346; functions with a unique string and no name 1,683 -> 1,051.
* Applied to the Ghidra project with `ApplyNexusForeverLabels.java` (`applied=5501, missing=21, duplicateRows=4`, the last two are older rows); the project state before this pass is backed up in
  `I:\tools\ghidra_proj_backup_20261108_pass1`.
* The evidence inventory was backfilled (524 new rows; `missingFunctionEvidenceRows: 0`). The 388 rows whose label changed got the new name and comment, and `labelRow` was recomputed for every row because
  removing the placeholder lines shifted the line numbers (`scripts/refresh_function_evidence_rows.py`; only `labelRow`, `durableLabel`, `purpose` and `anchors` changed, 5,593 / 388 / 388 / 388 rows). The BOM that
  the backfill script adds was removed again.

## Validation
* The table rule rests on the uniform template (362 of 362 are 515 bytes, no duplicate table name) and one decompile; it was not checked function by function.
* An independent hit for the event rule: `140787d10` (2,438 bytes) is labelled `LuaEvent_EpisodeStateChanged_Raise`; the separate agent read of prerequisite 9 identified exactly that function as the episode state
  recompute that fires `EpisodeStateChanged`.
* Not validated: that an event name always identifies the function's main role (several are handlers that raise an event among other work, hence the size note), and that every `DB\...tbl` function is a
  registration of that table (assumed from the template).

## Next candidates
1. The 1,051 functions with a unique string that is not a table path or an event name: Lua field names (`nCount`, `logoutReason`) mark payload builders, log text marks error paths. Needs per-category rules and review.
2. The 99 unresolved Lua method tables from pass 1 (1,200 functions).
3. Callers of other already-labelled helpers (`Network_RegisterServerOpcode_*`, `ClientDB_*` accessors, `Lua_InsertStringIntPair`) as further anchors: a label propagates to every function that calls a named helper with a string argument.
4. Re-run the coverage snapshot scripts; the numbers here come from `scripts/SurveyLabels.java`.
