# Prerequisite type 40 (Health) compares a percentage (2026-11-07)

Evidence tier: **native body read**, with the retail values (50, 30, 25, 75, 10, 100) corroborating the percentage reading. Source/SQLite only. No retail parity claim.

## Native

Evaluator case `0x28` calls `FUN_1404a4d90(unit, comparison, Value)`: false for a null unit; otherwise it takes the unit's virtual `+0x40` (current health) times the constant `DAT_140b7a0dc` (**100.0f**), divides by the unit's virtual `+0x48`
(maximum health) and compares the resulting float with `(float)Value` through the float helper `FUN_1404a2010` (1 equal, 2 not equal, 3 greater, 4 greater or equal, 5 less, 6 less or equal; IEEE semantics). Retail: 406 slots,
`Value` 50 (72), 30 (51), 25 (29), 75 (25), 10 (25), 100 (23); comparisons less-or-equal 155, greater-or-equal 73, greater 67, less 100, equal 11.

## Server

`PrerequisiteCheckHealth` compared the **raw** health with `Value` (its comment said it was the integer variant, distinct from the "scaled" type 172). With `Value` a percentage, a "health at most 30" requirement held only for units
with 30 hit points or fewer. Type 172 (`HealthScaled`) already compares an integer percentage and was left unchanged.

## Change

`PrerequisiteCompare.CompareFloat` (new, the native float helper) and a rewritten `PrerequisiteCheckHealth` (`health * 100f / maxHealth`, null unit false). The existing `Health_ComparesRawHealthOnTarget` and the type 40 case of
`UnitPrerequisiteContextTests` encoded the raw reading and were replaced by percentage versions (`Health_ComparesHealthPercentOnTarget`, 7 cases, 4 failed first; the context case now expects 45 of 90 to be 50). 5,183
Prerequisite/Spell/Quest Source/SQLite cases pass (separate scope).

Also in this change: `PrerequisiteCheckClass` returned **true** for comparisons other than Equal and NotEqual; native returns false (`FUN_14049c560`). No retail row uses another comparison (Equal 355, NotEqual 5), so no data
changes, but the default was wrong; `ClassNeverMeetsAnUnsupportedComparison` (3 cases) was written after the one-line fix and was not observed failing.

## Read, no change needed

- **7 AchievementState** (`FUN_14049f770`): `ObjectId` is an achievement id, the state is 1 when the achievement record has its completion pointer set and 0 otherwise, compared with `Value` for equality (Equal) or inequality
  (NotEqual); retail `Value` is 1 (698), 0 (397) and 3 (1). The server's completed-or-not mapping matches except that it returns false for a `Value` above 1 even for NotEqual (the one `Value` 3 slot).
- **2 Race** (`FUN_14049c4e0`) compares the *local* player's race (`+0xd8`, default 0x11 when none) and **3 Class** the passed unit's class (`+0xdc`, local player when none, default 0x17).
- **4 Faction** (`FUN_14049c620`) compares the faction id from the object at `unit+0x118` (virtual `+0x18`), type 250 `BaseFaction` the one at `unit+0x110`; both objects' identity (current versus base faction) is unread, and the server
  uses `Faction1` for both.
- **59 SpellTier** (909 slots, `FUN_1404a4f60`) walks the unit's spell list and ends in the integer compare helper with arguments the decompiler lost; its body needs a disassembly before it can be compared with
  `SpellPrerequisiteHelper.MeetsSpellTier`.
