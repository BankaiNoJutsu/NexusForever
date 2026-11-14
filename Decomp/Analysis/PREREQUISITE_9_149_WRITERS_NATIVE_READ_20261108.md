Agent-written native read (Ghidra 12.1.4 headless, WildStar64.exe build 16042); the lead has not re-derived it.

**2026-11-14 scoped supersession:** the lead has now re-derived **type149 only**:
[primary State and field writers](PREREQUISITE_149_PRIMARY_SUBJECT_20261114.md).
Tier: **Verified native default selector/predicate, named packet reader/writer
bindings and constructor zeroing; Implemented / Source player wrapper**.
The older type149 ownership-blocked/no-reference comment is stale. The raw
explicit-offset search supports known direct stores, not exhaustive alias,
bulk or indirect writer coverage; do not promote an unrestricted "only writer"
claim from it. Type9 remains agent-written and unreviewed in this pass. Original-
server scheduling remains unknown; type102 remains Blocked. No retail parity claim.

# Prerequisite 9 (EpisodeState) and 149 (State): writers

## Q1. Type 9: who writes the episode quest-state map (episode+0x50)
**The map is not keyed by quest id.** Key = `Quest2.QuestCategoryId` of each quest of the episode (0 mapped to 1); offset 0x1a4 in the Quest2 row matches `Quest2Entry.QuestCategoryId` (checked: 0x14 Type, 0x38 WorldZoneId, 0x1b4 QuestRepeatPeriodEnum line up). Node +0x20 = category, +0x24 = episode state 0..3 (Unknown, Mentioned, Active, Complete).

**Only writer: `FUN_140787d10(episode)` (recompute).** Read directly. Other users: destructor `140787a10` (erase), copy at its start, reader `140788be0` (vtable +0xc8: map[cat] or 0), aggregate `140788b50` (vtable +0xc0). Episode object = 0x68 bytes (ctor `140787970`, vtable `140b77ed0`): list at +0x28 (nodes: +0x10 EpisodeQuest row, +0x18 quest object whose +8 is the Quest2 row), filled at load by `140723ce0` via `140787bd0` (one entry per EpisodeQuest row whose Quest2 row exists). The map starts empty.

**Algorithm of `140787d10`** (state s of each quest = `FUN_1405fbc40(questMgr, questId)`, 0 when the player has no state):
1. Old aggregate saved; every existing node set to 0.
2. For every list entry: cat = Quest2.QuestCategoryId (0 -> 1). Node created if absent (so one node per category present among the episode's EpisodeQuest quests, not only quests the player has). A temp map `done[cat]` starts at 1.
3. s=0: if row is null or `row.Flags & 1` == 0 then done[cat]=0. s in {1,2,4}: same clear, stored[cat]=max(stored,2). s=3: stored=max(stored,2) (does not clear done). s in {5,6,7}: same clear, stored=max(stored,1).
4. Every cat with done[cat]==1 gets stored[cat]=3. So Complete = every non-optional (`EpisodeQuest.Flags` bit 0 clear) quest of the category is state 3; a category of only optional quests is Complete from the start.
5. If the aggregate changed, Lua event `EpisodeStateChanged(episodeId, old, new)` fires; `FUN_14077d8b0` notifies the category object when a node changed.
Aggregate (`140788b50`): all nodes 3 or map empty -> 3; else max of the non-3 nodes.

**(a)** Untouched quests: present, as state 0, they keep done[cat]=0, so the episode is NOT Complete. An episode is vacuously Complete (3) only if its map is empty: no resolvable EpisodeQuest quests, or before the first recompute. Confidence: read directly. (Arguments of the lookup `FUN_1405a8a40` were lost by the decompiler; assumed quest id from row+8: inferred.)
**(b)** Stored value is the EpisodeState enum, not a quest state. Client quest state numbering equals the server `QuestState` 0..7 (cases 5,6,7 = Mentioned/Abandoned/Ignored grouped; 3 = Completed; the client treats ids in its completed set as 3). Confidence: read directly for the table above; the enum equality is inferred from the repo enum.
**(c)** Triggers (all end in `FUN_1405fbdc0(questMgr, quest, state)` -> `140553410(quest)` -> `140724340(episode)` -> `140787d10`, for every episode of that quest): `ServerQuestInit` 0x035F (handler `1405fb350`, calls it per quest in all three lists, then event QuestInit), `ServerQuestStateChange` 0x035C (`1405fb670`), and client-local `1405fca00` (sets Mentioned=5 from the communicator `14043c1a0` / `1405fccb0`, no packet). Initial full recompute for all episodes: `140724100`, called by `ServerPlayerCreate` 0x025E (`1403b5f80`, after resetting the quest manager, so all episodes start Unknown/0) and by teardown `1403e3350`. Opcodes verified in the dispatcher jump table `1403f1344` (size 0x912, opcode-0x61); the case-to-handler call sites match. Confidence: read directly. Server models: `ServerPlayerCreate`, `ServerQuestInit`, `ServerQuestStateChange`.
**Quirk**: ServerQuestInit with a repeatable quest (`QuestRepeatPeriodEnum != 0`) and timer 0 is not marked complete. Quests belonging to no episode are attached to episode id 1 with a null row (`1407241f0`).

## Q2. Type 149: writers of unit+0x1e4
Unit class = ctor `140454380` (vtable `140b66fd0`). Writers by raw scan of all `[reg+0x1e4]` operands:
- ctor: zero.
- **`FUN_140641640` = apply of `ServerUnitInUse` 0x0755** (call site `1403ee6b2`, case start `1403ee6ac`): looks up the unit by id (`1403d90d0`), stores `unit+0x1e4 = packet.value`, sets dirty `+0x18a8`; if value != 0 and the unit is the open NPC dialog target (`DAT_140c7dc64`) it calls `Dialog_Close` (`1405548f0`). So +0x1e4 = unit "in use" (busy) flag. Server already sends this from `WorldEntity` busy state. Confidence: read directly (the handler, the opcode via jump table, the unit class by id lookup); the packet-reader width (bool vs uint) not checked.
- Other hits are different classes: `140444f60` (ItemManager, item object), `1405d2cc0`, `1405212e0`, `1405f00e0`, `1405f2710`, `1404b1bb0`, `140438480`, `1403ce4a0` and the rest (floats, vector counts, UI/timer objects).
Result: type 149 state = (InUse != 0) || InCombat, Equal/NotEqual vs Value. Confidence: read directly for both flags; the "no other unit writer" claim rests on the operand scan (indirect addressing through computed pointers would be missed): inferred.

Scripts: `I:\tools\claude-tmp\RawCalls.java`, `JT.java` (new). Ghidra scratch outputs under `I:\tools`.

Lead review and implementation (2026-11-08): the native claims above are the agent's reading and were **not re-derived** by the lead. One independent cross-check: opcode 0x0755 is `ServerUnitInUse` in the repository's opcode table and the server already sends it from the entity busy state, which agrees with the agent's reading of `unit+0x1e4` as the in-use flag. Not cross-checked: the episode recompute and aggregate details, the `Quest2` offsets, the "category 0 counts as 1" rule, and the "no other writer of `+0x1e4`" claim (an operand scan can miss computed pointers).

**Type 149 is implemented** as `PrerequisiteCheckEntityState`: `state = IsBusy || InCombat`, Equal `state == Value`, NotEqual `state != Value`, other comparisons false; the unit is the target when one is given, otherwise the player. Tests `UnitStatePrerequisiteTests` (7 cases).
**Type 9 is implemented** as `PrerequisiteCheckEpisodeState(IGameTableManager)` with the per-category recompute and aggregate described above; the player's quest state comes from `QuestManager.GetQuestState`, `EpisodeQuest.Flags & 1` marks optional quests, a quest without a `Quest2` row is skipped, an episode missing from the `Episode` table has state 0 (inferred), Equal returns `state == Value`, NotEqual its negation, other comparisons false. Tests `EpisodeStatePrerequisiteTests` (15 cases). Both test files were written before the classes and first run after them: 21 of 22 passed and the one failure was a wrong expectation in my own test (a single completed quest makes the episode 3), corrected; neither was run red against a missing class. Filter Prerequisite|Quest|Episode: 4,824 cases pass (Source/SQLite scope).

Caveats: the client is event driven (the episode state is recomputed when a quest packet arrives; every episode starts at 0 at login until `ServerQuestInit`), the server evaluates on demand from the current quest states, which should agree once the quest list is loaded; the repeatable-quest quirk (a repeatable quest with timer 0 in `ServerQuestInit` is not marked complete) and the "quests of no episode attach to episode 1" quirk are not modelled; the server's `QuestState` numbering equals the client's by inference.

