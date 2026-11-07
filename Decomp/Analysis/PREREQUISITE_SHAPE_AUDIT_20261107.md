# Prerequisite retail-shape and jump-table audit of the implemented types (2026-11-07)

Evidence tier: **native bodies (decompile/disassembly) compared with each implemented check; documentation of one pass**. Source/SQLite only. No retail parity claim.

Method: (1) dumped the evaluator jump table (`FUN_1404a2100`, int32 entries at `1404a4848`, index `type-1`, base `140000000`) and mapped the blocks the decompile merged; (2) listed, for every implemented type, the retail ObjectId/Value distributions from `Prerequisite.tbl` next to its implemented meaning and looked for ids that cannot belong to that meaning; (3) read the evaluator body of every implemented type whose case is inline.

Corrected in this pass (each with its own record, tests written first):
| Type | Was | Native | Slots | Record |
|---|---|---|---|---|
| 64 | path type level | unit resource slot ObjectId (Resource0..10) vs Value, floats | 107 | PREREQUISITE_RESOURCE_SLOT_20261107.md |
| 55 | currency amount | unit property ObjectId vs Value, floats | 4 | PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md |
| 177 | NPC-gated challenge presence | unit base level (+0x38) vs Value | 220 | PREREQUISITE_TRUE_LEVEL_20261107.md |
| 12 | `!IsAlive`, any comparison | flag(+0x250 or +0x254 or Ghost) == Value, Equal/NotEqual only | 168 | PREREQUISITE_DEAD_STATE_BOOLEAN_20261107.md |
| 115 | unhandled | property vs property, floats | 18 | PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md |

Found and **not** changed (exact gate recorded):
- **24 (InPhase) and 25 (CanSeePhase)**: both vtable slots (`+0x290`, `+0x298`) are the same function `FUN_14049e640`: `flag = (*(uint*)(globalPlayerObject + 0x6750) >> (Value & 31)) & 1`; Equal returns flag, NotEqual its negation, ObjectId unused (retail Values 0 to 7 are bit indices; 97 + 48 slots). The server implements 24 as "entity public-event phase compared with Value" and 25 as "caster and target share a public-event phase", neither of which is this test. The only literal writer of that mask is the object constructor (zero init, `1403e2c8c`); how the client fills it is not visible, and the server has no per-player phase mask, so no honest replacement exists. Needed: the path that fills `+0x6750` (a packet field or a quest/phase effect) and a server phase-mask model.
- **70 (ChallengeTier)** was gated on challenge state field `+0x20` and then implemented once the row reader showed it is CurrentTier ([PREREQUISITE_CHALLENGE_TIER_20261107.md](PREREQUISITE_CHALLENGE_TIER_20261107.md)). **82 (UnitEntityType)** was given the boolean tail ([PREREQUISITE_UNIT_ENTITY_TYPE_20261107.md](PREREQUISITE_UNIT_ENTITY_TYPE_20261107.md)).
- **220 (PathMissionChecklistItemComplete)**: native `FUN_14049fe80` (mission lookup by ObjectId, virtual `+0x50(Value)`, Equal returns it, NotEqual its negation, absent mission NotEqual true) matches the server's tail shape; which clue/bit the virtual tests per path type stays unverified. No change.
- **218 (Datacube)**: native `FUN_1404a1580` is false for a non-player unit, otherwise `flag = FUN_1404a7f50(datacubeManager, ObjectId)` (the id is in the known set `+0x3f8`, or in a volume's member list), Equal returns the flag, NotEqual its negation, Value unused. All 64 retail slots have Value 0 and the server helper's Value 0 branch has the same Equal/NotEqual outcome (present vs absent), so no change.
- **219 (Volume)**: native `FUN_1404a80b0` looks the volume id up in the tree at `+0x438` and tests `popcount(record.+0x24) >= FUN_1404a8430()` (a volume is complete when its collected-bit count reaches the required count); retail has 3 NotEqual slots. The server helper treats it as a Journal datacube existing, which is not that test. Gate: a server volume-completion model (collected bits per volume against the required count from the Datacube/Volume tables). 3 slots; not changed.
- **191 (PetEntitySpell4)**: its pet-spell lookup (`FUN_140561c30` with the unit field `+0x1600`, falling back to the local player's pet) was not re-read.
- Inline types checked and consistent with their implementation or already corrected earlier: 1, 5, 11, 15, 22/23/33-35 (shared tail), 28, 37, 39, 40, 50, 56, 59/60, 71, 73, 78, 80, 86/87, 93, 95, 102-105, 114, 129/130, 172 (no retail rows), 215, 279/280. "Consistent" here means the body's reads match the implemented meaning, not that every server field behind them is verified.

Lesson: the earlier handler-table address comments in NF were unreliable (0 of 13 agreed); the jump table and the evaluator body are the reference. Any type still described only by an NF comment should be re-read the same way.

## Attempted and not resolved: type 199 (public event state `+0x1d0 == 1`)
`FUN_1404a0b70` returns `event != null && *(int*)(event+0x1d0) == 1` (Equal returns it, NotEqual its negation; 14 retail slots). I looked for the meaning of that field: the Lua `Game.PublicEvent.IsActive` (`1406899b0`) calls event virtual `+0x68` instead, not the field; an instruction search for compares against `[reg+0x1d0]` found only unrelated classes (`1403998e0`, `1404739b0`, `1406d34c0`, `1408c5050`), and the writers of `+0x1d0` are too numerous to attribute without the event class (not located). Gate unchanged: the public event class and the packet-to-field consumer for `ServerPublicEventStart`/`End`, the same technique that resolved the challenge row (70).
