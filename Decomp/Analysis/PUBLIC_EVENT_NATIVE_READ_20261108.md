# Public event objects: native client read for prerequisite types 199, 107, 119, 122, 200 (2026-11-08)

Evidence tier: **native decompile/disassembly of WildStar64.exe (build 16042), Ghidra 12.1.4 headless, plus `Prerequisite.tbl`/`PublicEvent*.tbl` row census**. Documentation only: no server code, tests or existing docs were changed. No retail parity claim; "server should" items are proposals.
Technique: the type's evaluator -> the object it reads -> the packet handler that writes the field (same method as `PREREQUISITE_CHALLENGE_TIER_20261107.md`). Native addresses below are from this pass; older NF comments are not relied on.

## Summary

| Type | Slots | Status | One-line meaning (native) |
|---|---|---|---|
| 199 | 14 | **resolved** | `ObjectId` PublicEvent id: the **reason of the last `ServerPublicEventEnd` received for that event == 1 (CompleteSuccess)**; default 2 (CompleteFailure) |
| 107 | 124 | **resolved except per-mission-type progress word** | `ObjectId` PathMission id: `(mission progress word) & (1 << creature QuestChecklistIdx)` |
| 119 | 122 | **resolved** | `ObjectId` PublicEventObjective id: parent event active and `(objective ObjectiveData) & (1 << QuestChecklistIdx)` |
| 122 | 1 | **resolved as dead-in-client** | objective's event active and objective id present in a per-unit map that the client never fills |
| 200 | 11 | **resolved** | objective's event active and `(objective Busy or event Busy) & Value` |

Server state today: none of the five has a check class (only `PrerequisiteType` enum entries; `PrerequisiteManager` logs "Unhandled PrerequisiteType"). So every statement "current check right/wrong" below is "absent".

## Objects found (the main unblocker)

* Global manager `DAT_140c65980` (class vtable `140b67420`, created once at client start by `FUN_140496660` from the PublicEvent tables; never recreated, so its state lives for the client session). Init `FUN_1404981b0` creates **every PublicEvent row** as an event object (factory vtable `+0xb0` = `FUN_1405f63a0`, 0x1f8 bytes, ctor `FUN_1405f00e0`, **event vtable `140b6f140`**) and every PublicEventObjective row as an objective (factory `+0xb8` = `FUN_1405f63d0`, base ctor `FUN_1405f0010`, **objective vtable `140b6ed20`**; special subclasses for objective types 0x1b/0x1c/0x1e).
* Lookup `FUN_140498a40(mgr, eventId, 0)` = event by id (map at instance set `+8`); `FUN_140498af0` = objective by id (map `+0x70`, key = objective vtable `+0x158`). Manager `+0x40` (`FUN_140498bd0`) is a thunk "objective by id, then its parent event (objective `+0x30` returns `+8`)", so **for 119/122/200 `ObjectId` is a PublicEventObjective id** and the event is the objective's parent (matches the tables: 119 ids reach 5337, 200 ids 3019-3025 belong to event 605).
* Event fields: `+0x18` active flag (vtable `+0x68` = `FUN_14038c790`; this is what Lua `Game.PublicEvent.IsActive` `1406899b0` returns), `+0x1c` Busy, `+0xb8` team, **`+0x1d0` last end reason**, `+0x1d4` temp flag. Objective fields: `+0x10` status (0 Inactive, 1 Active, 2 Succeeded, 3 Failed; vtable `+0x50/58/60/68/70` test it), `+0x18` ObjectiveData, `+0x1c` DynamicMax, `+0x24` Count (float), `+0x28` Busy.

## Packet-to-field mapping (readers checked against the server models)

| Packet | Native handler | Writes |
|---|---|---|
| `ServerPublicEventStart` | event vtable `+0xd8` = `FUN_1405f2ae0` (fails if already active) | event `+0x18 = 1`; `+0xb8` team (struct dword 1); `+0x1c` = Busy (dword 3); objectives rows via objective setters |
| `ServerPublicEventEnd` (reader `14007bb80`: u14 id, u32 reason, u32 elapsed, stats, counted rows, tier, type, 3 thresholds) | event vtable `+0xe8` = `FUN_1405f30a0` (fails if **not** active) | **event `+0x1d0 = struct dword 1 = Reason`** (also `+0x1d4` 1 then 0; `+0x18 = 0`) |
| `ServerPublicEventLeave` | event vtable `+0xe0` = `FUN_1405f2e90` | deactivates (`+0xb8` virtual); **does not touch `+0x1d0`** |
| `ServerPublicEventObjectiveUpdate` / row inside Start | objective setters `FUN_1405f1780` (dword 0 -> `+0x10` Status, dword 1 -> `+0x18` ObjectiveData, 2 -> `+0x1c`, count float -> `+0x24`) and `FUN_1405f17c0` (ElapsedTimeMs -> `+0x38` start time, **Busy -> `+0x28`**) | per objective |
| entity create (NonPlayer 0, CollectableUnit 8, Simple 10, Platform 11, AiTurret 13, SimpleCollidable 32) | `FUN_140456960` | **unit `+0x35f8 = (signed byte) QuestChecklistIdx`**; ctor `FUN_140454380` initialises -1 |
| two-dword message {unit guid, idx} | `FUN_1403dc280` (sets `+0x35f8`, marks UI dirty) | opcode not identified (see Unknowns) |

Writers of event `+0x1d0` in the whole `.text` (raw disp32 + instruction search): only `FUN_1405f2710` (event Init, vtable `+8`, value **2**) and `FUN_1405f30a0` (End); one reader, `FUN_1404a0b70`. Other `+0x1d0` hits (`1404445d0`, `14046cdc0`, `1405212e0`, ...) are unrelated classes.

Enum evidence: Lua registrar `FUN_140687d40` pushes `PublicEventParticipantRemoveReason_Logout 0, CompleteSuccess 1, CompleteFailure 2, LeftArea 3`, identical to server `PublicEventRemoveReason` (Logout 0, Success 1, Failure 2, LeftArea 3).

## Per type

### 199 (`FUN_1404a0b70`, 14 slots): resolved
`flag = event(ObjectId) exists && event+0x1d0 == 1`; Equal returns flag, NotEqual its negation; manager absent -> false for both. So **Equal = the last End the client received for that event carried Reason Success**; before any End, after a Failure/LeftArea/Logout End, the value is not 1 (default 2), so NotEqual is true. Leave packets and a restart (Start) do not reset it; only a new End overwrites it; it is cleared only by client restart (manager is session-lifetime). Rows: ObjectId 1, 4 (not in `PublicEvent.tbl`, never true), 308/320/321 (sub events, parent 146), 447 (type 19 Shiphand, zone 2624), 676 (sub event, parent 673); Value always 1 (ignored), comparisons Equal/NotEqual only.
Server: absent. Propose `PrerequisiteCheckPublicEvent199`: per-character `Dictionary<uint,PublicEventRemoveReason>` on `PublicEventCharacter` (written where `PublicEvent.FinishTeams` sends `ServerPublicEventEnd`, `PublicEvent.cs` ~line 520, and nowhere else), result `reason == Success`, `CompareFlag(comparison, flag)`; no entry = false. Inferred (not native): that the retail server's own check used the same per-player "last result"; the client behaviour above is native.
Tests: Success End -> Equal true/NotEqual false; Failure End -> Equal false; no End -> NotEqual true; Success then Leave/LeftArea -> still true; Success then Failure End -> false; unknown event id (1, 4) -> Equal false, NotEqual true.

### 107 (`FUN_14049fff0`, 124 slots): slot resolved, mission word partly
The "participation slot" `unit+0x35f8` is **the creature's `QuestChecklistIdx`** (signed byte from the entity-create model, -1 when 0xFF; server `IWorldEntity.QuestChecklistIdx` already carries it). `DAT_140c65970` is the **PathManager** (Lua `Game.PathMission` resolves through `FUN_14067b760` -> `FUN_1403d7bc0`; `+0xc` is the unlocked-path bitmask); 102 of 124 slots' ObjectIds are `PathMission.tbl` ids (33..3325), the rest are retired ids below the Quest2 id range. Logic: if the evaluated unit is Player (20) or Ghost (23) (and the target absent or the same kinds) -> Equal false, NotEqual true; otherwise mission = PathMission(ObjectId); missing mission or idx == -1 -> 0; else `mission.vtable+0x40() & (1 << (idx & 31))`; Equal returns it, NotEqual its negation. Value unused. `+0x40` is the mission progress word used by `FUN_14056d0d0` for mission types 2,4,5,6,7,0x13,0x15,0x18 (types 3,0xd,0xf,... use `+0x48` there); 107 reads `+0x40` regardless of type.
Server: absent (enum `QuestObjective7`, name misleading: it is a **path mission checklist-bit-of-this-creature** test). Propose `PrerequisiteCheckPathMissionCreatureChecklist`: subject = `parameters.Target ?? player` unit via `MeetsUnit`; Player/Ghost -> NotEqual only; `idx = WorldEntity.QuestChecklistIdx` (255 -> none); read the evaluating player's `PathManager` mission `ProgressData` (the same field `TryIsPathMissionChecklistItemComplete` uses at `PathManager.cs:1341`) and test bit `idx & 31`; do NOT reuse type 220's "completed mission => all bits true" shortcut. Inferred: server `ProgressData` equals the client `+0x40` word for every mission type.
Tests: bit set/clear, idx 255, missing mission, player target, idx >= 32 wraps, NotEqual negation.

### 119 (`FUN_1404a0650`, 122 slots): resolved
Same player/ghost short-circuit as 107. Else: objective(ObjectId) found, its parent event active (`+0x18 != 0`), unit idx >= 0, result `objective.vtable+0x90(idx)` = `(1 << (idx & 31)) & objective+0x18` (ObjectiveData); Equal returns the masked word, NotEqual its negation. Value unused. The server packet already documents ObjectiveData as the checklist bitfield (`PublicEventObjectiveStatus.ObjectiveData`, ActivateTargetGroupChecklist/TalkToChecklist).
Server: absent. Propose check using `PublicEventPrerequisiteHelper.FindObjective` (needs its `objectiveData` exposed read-only on `IPublicEventObjective`), require the player to be a member of the event (client "active" means Start received, not merely "map has the event"; `GetRunningEvent` does not test membership), idx from the evaluated creature. Tests: bit set/clear, event not joined, idx -1, NotEqual when objective missing.

### 122 (`FUN_1404a08f0`, 1 slot: prerequisite 11645, objective 748, Equal): dead-in-client
Objective's event active and the key `ObjectId` is present in the map at `unit+0x3780` (key = **objective id**, not event id as the older gate note said). That map (and the sibling event-id map at `+0x3760`, read by `FUN_1404a0810`/the objective picker `FUN_1405f5860`) is created empty in the ctor `FUN_140454380` and **no insertion code exists**: a raw byte search of all of `.text` for disp32 `0x3780/0x3788/0x3760/0x3768` finds only the ctor and the three readers. Effective client result: Equal always false, NotEqual always true.
Server: absent. Propose the faithful dead behaviour (Equal false, NotEqual true) until a witness shows otherwise; do not invent a participation model. Caveat: an inserter reaching the maps through a sub-object base (e.g. `this+0x3700`) was not excluded. Test: constant Equal false / NotEqual true.

### 200 (`FUN_1404a0bf0`, 11 slots): resolved
Objective found, parent event active, then `objective.vtable+0xa0()` = `Busy ? 1 : event+0x1c (Busy)`, result `that & Value` (disassembled: `AND ESI,R14D` with `R14D` = Value). Equal returns the masked word, NotEqual `masked == 0`. Busy comes from the Start/Update `Busy` bool (objective `+0x28`, event `+0x1c`); the server has `IPublicEventObjective.IsBusy` and `IPublicEvent.IsBusy`. Row census: 9 of 11 slots have Value 0 (objectives 3019-3025 of event 605, 3148, and 1), so for them Equal is **always false** and NotEqual always true; only prerequisite 37105 (objectives 2921, 2676, Value 1) can be true.
Server: absent. Propose `((objective.IsBusy || objective.Event.IsBusy ? 1u : 0u) & value) != 0` with the same membership/active rule. Tests: Value 0 constant, Value 1 with objective busy, event busy only, event inactive.

## Remaining unknowns
1. Opcode and reader of the `{unit guid, idx}` message handled by `FUN_1403dc280` (no pointer/call reference found to the function; not in vtables or pdata-independent tables). It only matters for idx changes after create.
2. Per-mission-type meaning and exact bit layout of PathMission `+0x40` (vtable not located); server equivalence of `ProgressData` unverified. Types 2/14 (13 of the 107 rows) read `+0x40` although `FUN_14056d0d0` uses `+0x48` for them.
3. Whether any code path fills the `unit+0x3760/+0x3780` maps indirectly (affects only 122).
4. Whether retail's server evaluated 199 with the same per-player last-result rule (only the client side is native).
5. Evaluated-unit selection when both unit and target are non-player (the `param_5` swap in 107/119) was read from the decompile, not from disassembly.

## Tooling notes
New helper scripts (nothing existing edited): `I:\tools\scripts\FindDispRange.java` (ranged instruction text search), `FindPtr.java` (qword/dword pointer search), `FindBytes.java` (raw byte search in `.text`). `DecompForce` may have created function entries in the Ghidra project.

## Lead review and implementation (2026-11-08)
Review: the lead re-decompiled the evaluators of 200, 119 and 122 (the tails above are confirmed: for 200 a missing objective or inactive event gives word 0, so Equal false and NotEqual true; for 119 the Player/Ghost short-circuit; for 122 a lookup in a map the agent found never filled) and the two writers of event `+0x1d0` (Init stores 2; the End handler `FUN_1405f30a0` stores packet dword 1 at `+0x1d0`, confirmed in the decompile), so type 199's meaning is confirmed by the lead. The 119 mask `(1 << (idx & 31)) & ObjectiveData` and the `PathMission +0x40` word for 107 are the agent's disassembly reading and were not re-derived.
Implemented now (tests first; the new test class did not compile until the checks existed): `PrerequisiteCheckPublicEventObjective122` (constant Equal false / NotEqual true), `PrerequisiteCheckPublicEventObjective200` (Busy mask), `PrerequisiteCheckPublicEventObjectiveObject119` (target creature's checklist bit; a signed-negative or 255 index means none), with `IPublicEventObjective.ObjectiveData` exposed and `PublicEventPrerequisiteHelper.FindActiveObjective` (event not finalised, player a member of the event, own team's objective preferred). Tests: `PublicEventObjectiveStatePrerequisiteTests` (21 cases); PublicEvent+Prerequisite filter 3,526 cases pass (Source/SQLite scope). Not implemented yet: 199 (needs a per-character last `ServerPublicEventEnd` reason store written where `PublicEvent.FinishTeams` delivers the End, including the pending-start queue path) and 107 (needs the path mission progress word; the agent could not locate the vtable and the per-type layout).
