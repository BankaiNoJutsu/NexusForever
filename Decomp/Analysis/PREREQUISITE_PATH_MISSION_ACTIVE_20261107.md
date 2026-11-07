# Prerequisite 54 (PlayerPathMission) is "the mission is active"; the path mission class mapped (2026-11-07)

Evidence tier: **native bodies read** (the class vtable and its virtuals). The server mapping (active = the Started state) is **inferred**. Source/SQLite only. No retail parity claim.

## How the class was found
The PathMission table consumers were followed again after a wrong vtable ([PREREQUISITE_AUDIT_PASS_20261107B.md](PREREQUISITE_AUDIT_PASS_20261107B.md)). `FUN_14056ed20` is the handler of the server's path mission update list: it looks each mission
template up, calls the factory `FUN_14056dce0`, which allocates a `0x40`-byte object, runs the base constructor `FUN_14056b0f0` and stores the concrete vtable **`140b6c950`**. The handler raises `PlayerPathMissionUnlocked` through the mission's
`+0x58` virtual and `PlayerPathMissionAdvanced` and the Activate/Deactivate events through the other virtuals below.

## The mission class (vtable `140b6c950`, object fields)
| Slot | Body | Meaning |
|---|---|---|
| `+0x18` `FUN_14056b380` | if `+0x18 == 0` and not complete: set `+0x18 = 1`, call `+0x88(1)` and `+0xa8` | **Activate** (event `PlayerPathMissionActivate`) |
| `+0x20` `FUN_14056b3d0` | if `+0x18 != 0`: set it to 0, call `+0x90(0)` and `+0xb0` | **Deactivate** |
| `+0x28` `FUN_14038c790` | returns field `+0x18` | **IsActive** |
| `+0x30` `FUN_14056b260` | true for definition types 3, 0xd, 0xe, 0x11, 0x14, 0x17, 0x19, 0x1a, 0x1b; types 5 and 6 depend on a table row (`+0x10 - 1 < 2`); else false | which progress counter the mission uses (bit-count or plain) |
| `+0x38` `140575a70` | returns field `+0x24` | **IsComplete** (the constructor's first update value) |
| `+0x40` `140575a80` | returns field `+0x1c` | progress value |
| `+0x48` `140575a90` | population count of field `+0x1c` | number of set progress bits |
| `+0x58` `140482340` | returns field `+0x20` | flags word (bit 0 = unlocked/started for the types below) |
| `+0xc0` `FUN_14056d920` | on a change of the `+0x20` flags: for definition types 2 and 0xe, bit 0 newly set with bit 1 clear raises `PlayerPathMissionUnlocked`; types 4 to 7 raise their path notices | flag change callback |
| `+0xd0` `FUN_14056b410` | complete -> **4**; type 0: `+0xd8` result + 2; types 2 and 0xe: `(flags & 1) * 2 | 1` (1 or 3); types 4 to 7: `(flags & 1) | 2` (2 or 3); type 0xd: derived from the flags; type 0x12: `+0xe0` result + 2; otherwise **3** | **mission state**: 1 locked, 2 available, 3 started/active, 4 complete |

The constructor (`FUN_14056b0f0`) takes three update values: `+0x24` = value 1, `+0x1c` = value 2, `+0x20` = value 3, and clears `+0x18`.

## Type 54
Evaluator case `0x36` calls `FUN_14049fdc0(unit, comparison, Value)`: with no service the result is 0; otherwise `FUN_1404a1950(0, Value)` looks the mission up in the player's mission map (`DAT_140c65970 + 0x58`, keyed by mission id).
Returned value: for a path word (`DAT_140c65970 + 8`) of 0 (the Soldier path) the mission's `+0x28` must be non-zero, then definition type 0 additionally requires a set membership (`DAT_140c65970 + 0x68`, keyed by the definition's `+0x14`), types 4 to 7 use bit 0 of the
`+0x58` flags and any other type returns `+0x28`; for every other path word the result is `+0x28`. Equal returns the value, NotEqual its negation, anything else 0. `ObjectId` is not read; `Value` is the mission id (retail Equal 247, NotEqual 9;
38 slots have `Value` 0, which matches no mission).

## Server change
`PrerequisiteCheckPlayerPathMission` (new): `GetMissionState(Value) == Started` (the server's Started state is the activated mission) with `CompareFlag` semantics; no path manager means not active. Not modelled: the Soldier-path holdout-table membership for definition
type 0 and the type 4 to 7 flag refinement. `PrerequisiteCheckPlayerPathMissionTests` (12 cases) and the 1,827-case Prerequisite/Path Source/SQLite filter pass (separate scope); the tests were written together with the check, so no red run was observed.

## Gate changes
- **54** (256 slots): mapped and implemented as above; open: the Soldier holdout set and an end-to-end check that the server's Started equals the client's activated flag.
- **110** (47 slots, `FUN_1404a00e0`): its virtual `+0x30` is now known (the table above), so it returns the bit-count (`+0x48`) for those types and the plain progress value (`+0x40`) otherwise; it compares that with `Value` for the mission with id `ObjectId`.
  Not implemented: the server has no per-mission progress value equal to the client's `+0x1c` (the server keeps its own progress representation), which is the remaining input.
- **53** (33 slots) was not re-read.
