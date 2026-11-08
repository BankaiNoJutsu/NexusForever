# Label coverage pass 3: unresolved Lua tables and library functions (2026-11-08)

Follows [pass 1](LABEL_COVERAGE_PASS_LUA_TABLES_20261108.md) and [pass 2](LABEL_COVERAGE_PASS_STRING_ANCHORS_20261108.md). Evidence tier: Observed names (client strings) with the caveats below.
This pass is deliberately small: both targets turned out to be much less mechanical than the first two.

## A. The 99 unresolved Lua method tables
* `scripts/RunOwners3.java` and `scripts/RunOwners4.java` looked for references to **any** address inside each table (not only its first record) and, for each, the identifier-like strings referenced earlier in the
  registering function. 193 tables, 520 references.
* Tables are frequently reached by base-plus-offset arithmetic from a neighbouring table, so many have no reference of their own: only 7 of the 99 unresolved tables gained a candidate class, and checked
  against the 67 tables pass 1 had classified with confidence the new rule agreed on only 56 (11 disagreements), so it is **not reliable enough to apply in bulk**.
* Applied: the two tables whose candidate is independently confirmed. `140b66970` (9 entries) and `140c58c00` (79 entries) are `Game.ItemData`: the registrar strings name it and the method names (`isRuneData`,
  `GetSetBonuses`, `GetDetailedInfo`, `HasRestockingFee`, ...) match the existing durable `Lua_GameItemData_*` labels. 77 `Lua_T<table>_<Method>` rows were renamed to `Lua_GameItemData_<Method>` (the other 11
  functions already had a durable label); no name clashes.
* Not applied: the other five candidates (`GachaScene`, `AccountItemLib`, `DefaultStratum`, `LeaderboardLib`, `Game.CharacterTitle`) look wrong or unconfirmed against their method names (for example
  `GetPendingAccountItemGroups` is an account-item method, not a gacha-scene one). 97 tables (1,123 functions) stay `Lua_T<table address>_*`. Ideas for later: group tables by method-name overlap with
  classified tables, or read the Lua `luaL_register`-style registrar calls' argument registers instead of the nearest string.

## B. Stock library functions named from unique error text
The remaining unnamed functions with a unique string are heterogeneous (1,150 before this pass): 346 Lua payload builders with field names, 301 event or window identifiers, 159 format strings, 129 Hungarian
field names, 112 sentences, 103 file paths. Free text and field names are not safe to name mechanically. The one coherent group is the statically linked **Lua 5.1 core** and **zlib**, whose error messages
identify stock library functions. Eleven were labelled where exactly one function matched the message set:
`Lua51_luaL_argerror`, `Lua51_luaL_where`, `Lua51_panic`, `Lua51_errfile`, `Lua51_luaG_typeerror`, `Lua51_luaX_lexerror`, `Lua51_inclinenumber`, `Lua51_read_numeral`, `Lua51_read_long_string`,
`Lua51_check_match` and `Zlib_inflate_fast`. The names assume unmodified library sources and were **not compared with the original source** (the client's Lua is not byte-identical: for example its parser has
"too many local variables" style messages beside 5.1 ones). Skipped because two functions matched: `luaL_register` and `addinfo` (the second differs from `luaX_lexerror` only by one extra string).

## Result
* Pass 3 changed 77 existing names and added 11 labels (`function_labels.csv` 6,301 -> 6,312 data rows; the gap report counts 6,313); the Ghidra project was updated with `ApplyNexusForeverLabels.java` and the
  evidence inventory refreshed (11 rows added, 77 labels updated, `missingFunctionEvidenceRows: 0`).
* Named functions in the project: 5,727 -> **5,738 of 25,073 (about 22.9%)**; default-named functions 19,335; functions with a unique string and no name 1,040.

## Where the remaining coverage is
About 19,300 functions are still unnamed; the largest cheap sources are now exhausted. The next steps need judgement, not a scan:
1. Call-graph propagation from named helpers (callers of `ClientDB_*` accessors, `Lua_InsertStringIntPair`, the per-opcode readers and writers) with a reviewer accepting proposed names.
2. Per-system hand passes using the existing method (decompile, name, evidence row), prioritised by what the server work needs (quest, spell and challenge consumers).
3. Fix the two possible conflicts from pass 1 (`140001ba0`, `1400d7b40`/`1400da180`).
