# Restoration gap 2: the remaining prerequisite types (2026-11-08)

Part of [RESTORATION_GAPS_GOAL_PROMPT.md](RESTORATION_GAPS_GOAL_PROMPT.md). Builds on [PREREQUISITE_REMAINING_TYPE_GATES_20261107.md](PREREQUISITE_REMAINING_TYPE_GATES_20261107.md) and
[PREREQUISITE_TYPE_COVERAGE_20261107.md](PREREQUISITE_TYPE_COVERAGE_20261107.md). Evidence tier: Source/SQLite tests, table scans, and one agent-written native read that the lead did not re-derive
([PREREQUISITE_239_226_244_18_NATIVE_READ_20261108.md](PREREQUISITE_239_226_244_18_NATIVE_READ_20261108.md)); no retail parity claim.

## Corrected census (`scripts/prereq_census.py`, `scripts/prereq_triage.py`)
* Before this slice: **55 types with no handler, 2,516 of 49,184 slots (5.12%)**. The 2026-11-08 first estimate (56 types, 2,876 slots) was a name-matching artifact (type 3 `Class` is handled).
* Since the 2026-11-07 gate table the types 9, 107, 119, 122, 146, 149, 199 and 200 were implemented; no type is new.
* By client evaluator: **client stubs 24 types / 2,281 slots (91%)** (the client slot is the shared return-0 stub, so the meaning is server-side only and the binary cannot give it), real or inline client
  bodies 29 types / 232 slots, no evaluator case 2 types / 3 slots.
* This slice implements 226, 244 and 239 (19 slots). **52 types / 2,497 slots remain.**

## Implemented
| Type | Class | Semantics (agent-written native read, not re-derived) | Server side |
|---|---|---|---|
| 226 Entitlement (6 slots) | `PrerequisiteCheckEntitlement` | player units only; `n` = character amount + account amount of Entitlement `ObjectId`, capped at the row's `MaxCount` (the count type 273 uses); Equal `n != 0`, NotEqual `n == 0`, other comparisons false; Value unread | shares `PrerequisiteCheckEntitlementCount.GetCount` (type 273 behaviour unchanged) |
| 244 (1 slot) | `PrerequisiteCheckHasBindPoint` | it is **HasBindPoint**, not `OwnsAccountItem`: the player's current bind point exists in `BindPoint.tbl`; Equal exists, NotEqual absent; ObjectId and Value unread; same test as the Lua `HasBindPoint` | `IPlayer.TryGetBindPoint`; the enum member was renamed `OwnsAccountItem` -> `HasBindPoint` and its naming test row updated (the old name came from reading the single row's ObjectId) |
| 239 ActionBar (12 slots) | `PrerequisiteCheckActionBar` | bit 0 of the per-action-set unlock dword for set `Value` (0 to 3), written only by the apply of `ServerActionSet` 0x019D from its `Unlocked` field; Equal flag, NotEqual its negation | the server announces every set it has as unlocked (`Unlocked = 1`, `SpellManager.ActionSetPublication.cs:220`, `ActionSet.cs:391`) and keeps no locked state, so the flag is `Value < ActionSet.MaxActionSets` (4) |

Cross-checks by the lead: the 273 count function and the 0x016D bind point packet (`Player.Travel.cs:48` sends the bind point id on `ServerActionSetUInt16`) agree with the agent's reading; the `ServerActionSet`
`Unlocked = 1` behaviour was read in the server source. Not re-checked: the evaluator bodies, the vtable slots, the writers of the client fields.
Tests: `EntitlementBindPointActionBarPrerequisiteTests` (22 cases) were written before the classes and first run after them (one missing `using` fixed, then green); not red against missing classes.
Filter Prerequisite|Entitlement|Bind|ActionSet: 2,090 cases pass (Source/SQLite).

## Not implemented, with the reason
* **18 ScheduledEvent (17 slots).** The client set (`player+0x7290`, an `unordered_set<uint32>`) is filled only by opcode 0x036F (`ServerUInt32AndFlag`: flag 0 inserts the id, flag 1 erases it) and then refreshes the
  event dependencies. The server has no 0x036F, no ScheduledEvent table or manager, so the set is always empty (Equal never true, NotEqual always true). What the ids are (live events?) is unresolved. Implementing
  the check without a producer would only turn NotEqual true.
* **47 QuestObjective47 (1,788 slots, 71% of what is left) and its caster/target variant 277.** Client stub; the meaning is server-side. New facts from this slice:
  * Its server-evaluated consumers are large: `Spell4Effects` TargetApply 202, CasterApply 118 and persistence 14, `Spell4` CasterCast 122, TargetCast 41, AoeTarget 23 and persistence 20, `GossipEntry` 43,
    `CommunicatorMessages` 7, `StoryPanel` 7, `Creature2Action` 7 (`PathScientistCreatureInfo` and `ItemSpecial` columns also reference it).
  * **161 spells carry a type 47 cast gate** (107 distinct names, mostly creature attacks: Scratch, Firebloom, Ice Wave, Punch, Whirlwind, Rockfall, Slash, Summon Dagun Pet, ...). `Spell.cs:512-521`
    evaluates `CasterCastPrerequisite` and `TargetCastPrerequisites` for the caster unit (`MeetsUnit`), and an unhandled type evaluates false, so these spells fail their cast check today. Whether creature AI
    casts through that path was not traced.
  * The earlier statement that Value is a QuestObjective id is **not supported**: 731 of the 1,788 Values (41%) are QuestObjective ids, about what chance gives for dense small ids; no other table (Spell4Base,
    Quest2, Item2, Achievement, and 100 more tables ranked by hit rate against a shifted control) shows a signal, and sample names from those tables do not fit the gated spells.
  * Decision for the user: whether unknown server-only gates may pass through (so those 161 spells can cast) or stay blocking until a witness exists. Nothing was changed.
* **The other 22 client-stub types** (154 Holdout 119, 153 HoldoutWave 95, 126 PathHoldout 72, 49 Schedule 68, 30 FacilityWithinDistance 54, 193, 127, 142, 189, 147, 203, 276, 259, 192, 285, 271, 243, 222, 163, 48, 294,
  278): server-side concepts with no client body; each needs a retail or server witness. The path holdout family (126, 127, 153, 154: about 300 slots) would also need the soldier holdout state.
* **Real bodies blocked on server systems:** PvP ratings 251 to 255 and 230 (about 120 slots; no rating producer), warplots 187 and 234, settler hubs 150 to 152, NPC inventory 248, `ActionBar`-style locked state
  for the `UnlockActionBar` effect (needs a locked state first), account/bonus words 235, 237, 225, 224, and unread bodies 116, 77, 216, 131, 112, 145, 281, 228, 260, 21 and 295.

## Follow-ups
1. If an action-set lock is ever modelled (the `UnlockActionBar` spell effect, rows "Action Set 2 / 3 / 4 Unlock"), prerequisite 239 and `ServerActionSet.Unlocked` must read it; today both are unconditional.
2. The 161 type 47 spells need a decision or a witness.
3. 0x036F and the scheduled-event ids are a candidate for a future live-event pass.
