Agent-written native read (Ghidra 12.1.4 headless, WildStar64.exe build 16042); the lead has not re-derived it.

# Prerequisite 146 (ItemTradeSkillLevel)

Summary: type 146 does not involve the player at all. It computes a tradeskill tier from an **item's level** and compares it with `TradeskillTier(ObjectId).Tier`. `Value` is **ignored**. Corrects the earlier note ("the unit's value", "compared with Value").

## Body (read directly)
Dispatcher `FUN_1404a2100` case 0x92 (146): `vtable+0x608(this, param_2[2], comparison=param_3[1], objectId=param_3[2])`; `param_3[3]` (Value) is not passed. Evaluator `FUN_1404a0f90`, disassembly checked (`R9D = [tierRow+8]`).

## (a) What is `param_2[2]`
The evaluation block (stack struct built by the wrappers): +0 subject unit, +8 second unit, **+0x10 item object**, +0x18 item data (default item+8), +0x20 flags (8), +0x2c context, +0x30 -1. The item object is the 0x1f0-byte client item record built by `FUN_1404445d0` from an `Item2` row (manager `DAT_140c658f0`, lookup `FUN_1400b5df0`). Wrappers that fill slot +0x10 (read directly):
- `FUN_14049bf90`, from `FUN_1403c2630`: handler of `ClientSellItemToVendor` (0x166, sends `FUN_1403f4900(..., 0x166)`); the inventory item being sold, prerequisite id = vendor list entry (type 2) `+8`.
- `FUN_14065c020` (Lua vendor-list builder, "itemRequired"): same per-inventory-item evaluation for type 2 entries.
- `FUN_14049c010(unit, prereqId, item2Id)`, from `FUN_1405e6ad0` (Lua method of `Game.Achievement`, `FUN_140695720`): for achievement types 33/35/38/40/76/77 evaluates `Achievement.PrerequisiteIdObjective` (+0x3c) on the **output item of each `TradeskillSchematic2`** of the achievement's tradeskill. Retail: all 18 achievements that reference a 146 prerequisite are type 33 via `PrerequisiteIdObjective`.
All other callers (vtable +0x18/+0x20 callers among the ~92 functions that use `DAT_140c659a0`) leave +0x10 null. Null slot, or manager `DAT_140c65b90` missing, or tier/tradeskill row missing: returns **0 for every comparison**, including NotEqual (read directly). Situation names are partly inferred (confidence: structure read directly, vendor/achievement meaning inferred).

## (b) Scanned value (read directly)
Item object words, set in `FUN_1404445d0` from the Item2 row (client row offsets equal the server `Item2Entry` order): `+0x14c` = row `Flags` (`0x7c`); `+0x160` = row `PowerLevel` (`0x3c`); `+0x15c` = `RequiredLevel` (`0x40`) if `Flags&1 == 0` and `RequiredLevel != 0`, else 1. Value used = `Flags&1 ? PowerLevel : +0x15c`. Per-item data, not per-player. Row with flag `0x4000` takes flags/RequiredLevel from substitute Item2 74201 (flags 0x101, so bit 0 set, PowerLevel path); none of the 2,641 schematic outputs has 0x4000 (280 have bit 0).

## (c) Band index and comparison (read directly)
Record = `FUN_1405be150(tierRow.TradeSkillId)` (plain map lookup, not a scan); built by `FUN_1405bcf90` from `Tradeskill` + `TradeskillTier`: `thr[Tier-1] = ItemLevelMin` (+0x24 of the row, only `1 <= Tier <= 10`), missing tiers 0, `count` = highest Tier present.
```
v = (flags&1) ? PowerLevel : (RequiredLevel != 0 ? RequiredLevel : 1)
idx = 1
for i = count-1 downto 0: if v >= thr[i] { idx = i+1; break }   // inclusive lower bound
// below thr[0] or count 0 -> 1; above last -> count
return Compare(comparison, idx, tierRow.Tier)   // FUN_1404a2090: 1 ==, 2 !=, 3 >, 4 >=, 5 <, 6 <=, else false
```
Retail quirks (data read from `I:\tools\extract\DB`): Cooking, Mining, Survivalist, Relic Hunter, Farmer, Runecrafting have all thresholds 0, so idx = count (so `>=` always true); Fishing tiers 1 and 7 only, so idx is 6 below 45. Retail 146 slots: 34; comparisons 4 (26) and 1 (8); Value 0/1/10 irrelevant.

## (d) Server mapping
No player quantity. Use `IPrerequisiteParameters.Item` (`Info.Entry`: `Item2Entry.Flags`, `PowerLevel`, `RequiredLevel`) and `IGameTableManager.TradeskillTier` (`TradeSkillId`, `Tier`, `ItemLevelMin`); `PrerequisiteItemTypes` already marks 146 as item-context. `TradeskillPrerequisiteHelper.GetPlayerTradeskillTierRank` (player XP) is the wrong quantity. `ItemFlags` in GameTable.Static has no named bit 0. Not found: meaning of Item2 flag bit 0.

Tooling: first Ghidra call failed because `-scriptPath` with `;` is split by the .bat; used one path. Helpers: `I:\tools\claude-tmp\run146.ps1`, `DecompGrep.java`, `p146.py`, `p146b.py`.

Lead review and implementation (2026-11-08): the native claims above are the agent's reading and were **not re-derived** (dispatcher case 0x92, the evaluator body, the item-record offsets, the wrapper call sites, the Depreciated substitute). Re-checked by the lead against the extracted tables: `TradeskillTier` has 56 rows and `ItemLevelMin` is the word at offset 0x24 (the tradeskill threshold lists match the agent's description, including the all-zero tradeskills 2, 13, 15, 18, 20 and 22, the 10/3/30 oddity of tradeskill 16, the 45/45 of tradeskill 17 and the tiers 1 and 7 of tradeskill 19); the 34 retail slots are comparison 4 (26) and comparison 1 (8), and every ObjectId is a `TradeskillTier` id. This corrects the earlier note in [PREREQUISITE_24_25_219_NATIVE_READ_20261108.md](PREREQUISITE_24_25_219_NATIVE_READ_20261108.md) (the compared value is an item-derived level, not the unit's, and Value is ignored).

**Type 146 is implemented** as `PrerequisiteCheckItemTradeSkillLevel(IGameTableManager)`: the item comes from `IPrerequisiteParameters.Item`; level `v = (Flags & 1) ? PowerLevel : (RequiredLevel != 0 ? RequiredLevel : 1)`, with a Depreciated (0x4000) item taking Flags and RequiredLevel from Item2 74201 (kept as is if that row is missing); thresholds from `TradeskillTier` Tier 1 to 10 (missing 0, count = highest tier); band index = highest `i` with `v >= thr[i-1]` (1 below the first threshold); the slot's comparison compares the index with the row's `Tier` through the existing `PrerequisiteCompare.Compare`; a missing item or tier row is false for every comparison. Previously there was no handler, so every 146 slot was false with an "Unhandled PrerequisiteType" warning. Tests `ItemTradeSkillLevelPrerequisiteTests` (27 cases with theory rows) were written before the class and first run after it, green on the first run, not red. Filter Prerequisite|Item|Tradeskill|Achievement: 3,508 cases pass (Source/SQLite scope).

Caveats: the call sites that supply an item are the vendor sell check, the vendor list and the achievement objective check over `TradeskillSchematic2` output items (all 18 retail achievements that use a 146 prerequisite are type 33 via `PrerequisiteIdObjective`); whether the server's vendor and achievement paths pass `Item` for these is not checked, and where they do not the check is false as in the client's null-slot case. The meaning of Item2 flag bit 0 and the situation labels are unverified; the server mapping to `Item2Entry.Flags/PowerLevel/RequiredLevel` is the agent's inference from column order.

