# Boolean prerequisite types: Equal returns the flag, NotEqual its negation (2026-11-07)

Evidence tier: **native bodies read** (evaluator cases and the shared tail). Source/SQLite only. No retail parity claim.

2026-11-14 follow-through: [type-86 primary-subject Jump](PREREQUISITE_86_PRIMARY_SUBJECT_20261114.md)
re-derives the inline selector/null guard/mask/tail and corroborates the Jump name
with shipped type/text payloads. The player wrapper now retains primary; null
unit checks fail before reading state. Existing boolean comparison is retained.
**Verified native + shipped type-name witness; Implemented / Source**, without
physical movement/timing or retail parity qualification.

2026-11-14 type87 follow-through: [primary-subject Moving](PREREQUISITE_87_PRIMARY_SUBJECT_20261114.md)
re-derives the indexed native bit2 predicate. The player wrapper now retains
primary and null fails; extra Source Velocity/vector/magnitude conditions are
removed. Shared boolean comparison and movement producers remain unchanged.
**Verified native + shipped type-name witness; Implemented / Source**.
No physical movement/timing or retail parity claim.

## Native behaviour

Prerequisite types 28 (InCombat), 39 (IsPlayer), 86 (Jump) and 87 (Moving) test one boolean property of the unit and finish through the evaluator's shared tail (`LAB_1404a22ec`, inlined in case `0x1c`):

| Type | Case | Flag |
|---|---|---|
| 28 InCombat | `0x1c` | 32-bit field at `unit+0x2ac` |
| 39 IsPlayer | `0x27` | `unit+0x80 == 0x14` |
| 86 Jump | `0x56` | `*(unit+0x1070) & 0x40` |
| 87 Moving | `0x57` | `*(unit+0x1070) & 2` |

The tail: comparison 1 returns the flag, comparison 2 returns "the flag is 0", every other comparison returns 0. **The slot's `Value` is not read.** (Type 42, `IsGroupLeader`, reads no `Value` either: `FUN_14049e850` compares the local
player's group entry with the leader's, Equal for equal and NotEqual for different; with no group both are 0.)

## Impact

Retail `Value` is 0 in 204 of the 230 type 28 slots, 184 of 200 type 39, 21 of 22 type 86 and 25 of 26 type 87 (435 slots with Equal or NotEqual and `Value` 0), with NotEqual dominating type 28 (159, "not in combat")
and Equal type 39 (161). The server compared the flag with `Value` (`Compare(comparison, flag, Value)`), so **Equal 0 meant "the flag is clear" and NotEqual 0 meant "the flag is set"**, the reverse of the native result for all
of those slots. Supporting detail: spell target-persistence row 903 (`InCombat Equal 0`, used by the raid wipe timer buff, recorded earlier in this ledger) only makes sense as "while in combat".

## Change

`PrerequisiteCompare.CompareFlag(comparison, flag)` (new) and the four checks use it; `Value` is ignored. `FlagPrerequisiteTests` (72 cases: each check, flag set and clear, Equal / NotEqual / other comparisons, `Value` 0, 1 and 7)
failed 40 of 72 before the change and pass now. Four existing cases encoded the old reading and were updated, not weakened (`InCombat_ComparesTargetCombatState`, `IsPlayer_ComparesTargetEntityKind`, `Jump_ComparesJumpStateFlag`
and the type 39 case in `UnitPrerequisiteContextTests`, replaced by a NotEqual/Equal pair that still proves the subject, not the target, is evaluated). 5,472 Prerequisite/Spell/Quest/Movement/Group Source/SQLite cases pass (separate scope).

## Not changed

- **42 IsGroupLeader** (1 retail slot) keeps `Compare(…, isLeader, Value)`; the native also treats "no group" as leader (both entries 0), which differs from the server's `isLeader = 0`. One slot, left for a group-state pass.
- Types with the same native shape and fewer than 11 retail slots each (90 MountVehicleType, 106, 171, 98, 266, 101, 267, 182, 183, 97, 88) were listed but not read.
- 116 Stealth, 281 and 295 use the same tail; 116 is gated on its field (see the type 56 / 116 section of `PREREQUISITE_TYPE_COVERAGE_20261107.md`).
