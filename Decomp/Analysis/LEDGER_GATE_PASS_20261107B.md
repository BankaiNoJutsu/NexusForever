# Ledger gate pass B (2026-11-07, after the second prerequisite audit)

Evidence tier: reading of existing records, native bodies and local tables. Source/SQLite unless stated. Not completion and not a retail-parity claim. It supersedes the gate table in
[STOPPING_CONDITION_20261107.md](STOPPING_CONDITION_20261107.md) only where it names new items; every older row there stands.

## Closed since the previous pass (committed, with records)
Prerequisite 5 ([PREREQUISITE_REPUTATION_FLOAT_20261107.md](PREREQUISITE_REPUTATION_FLOAT_20261107.md)), 59/280 ([PREREQUISITE_SPELL_STACK_COUNT_20261107.md](PREREQUISITE_SPELL_STACK_COUNT_20261107.md)), 42
([PREREQUISITE_GROUP_LEADER_20261107.md](PREREQUISITE_GROUP_LEADER_20261107.md)).

## Remaining items with the exact gate

| Item | Gate | Exact missing input |
|---|---|---|
| Prerequisite 4 / 250 (872 slots) | Native field identity | Which entity-create packet field is stored at entity `+0x110` and which at `+0x118` (server `Faction1`/`Faction2` mapping); body of both checks is read and only Equal/NotEqual occur |
| Prerequisite 50 (450), 103 (36), 130 (55) | Server state model | A per-effect caster/target record on tracked spell states (native reads spell object `+0x154` source unit and effect-node `+0x20`); the server records no caster |
| Prerequisite 214 (642), 190 (298), 245/275 (530) | Native semantics | Meaning of the flag in the per-tier object (214), the flair state values 0/1/2 and PetFlair row field (190), and the per-category sorted lists at state `+0x568` and the 245 helper `FUN_1403c16e0`; the account-item branch already has native polarity |
| Prerequisite 54 (implemented, see [PREREQUISITE_PATH_MISSION_ACTIVE_20261107.md](PREREQUISITE_PATH_MISSION_ACTIVE_20261107.md)), 110 implemented ([PREREQUISITE_PATH_MISSION_COUNT_20261107.md](PREREQUISITE_PATH_MISSION_COUNT_20261107.md)); 53 implemented ([PREREQUISITE_PATH_EPISODE_20261107.md](PREREQUISITE_PATH_EPISODE_20261107.md)) | Native identity (class now mapped) | Mission class virtuals `+0x28` (labelled "IsVisibleOrStarted" only from a Lua guard), `+0x30`, `+0x58`, and what `DAT_140c65970+8` is; GetMissionState values (1/2/3) disagree with the server enum (0/2/3/4) |
| Prerequisite 9 (34) | Native mapping | The writer of the per-quest state at episode map node `+0x24` and the server has no episode model |
| Prerequisite 47 (1,788), 76 (281), 153/154, 126, 49, 30, 163/164 | Server-only concept | Client evaluator slot is the `return 0` stub; needs a server or retail witness |
| Prerequisite 231 (**implemented**, see [PREREQUISITE_231_INLAID_AUGMENTATION_20261107.md](PREREQUISITE_231_INLAID_AUGMENTATION_20261107.md); open: MySQL application of the migration and a capture of packet 0x019C), 74, 107/119/162, 111/113, 239, 18, 146, 114/115/116, 194/195/232/292, 249 to 255 | Native identity / producers | As listed in STOPPING_CONDITION_20261107.md (AMP list writer, challenge untouched-record set, participation slot, unit/manager objects, PvP rating producers) |
| Telegraph shapes 3 (104 rows) and 6 (159 rows) | Native renderer | Shape switch of the client telegraph renderer (the builder chain `FUN_1405458e0` to `FUN_14061a840` has no shape branch); even once read, the renderer only fixes geometry, not server hit rules |
| Creature visibility prerequisite (4,420 creatures) | Native identity | Evaluating side (client vs server) |
| Public 07CE, quest reward type 10, achievement producers, unhandled spell-effect rows, persistence design decisions, native-gated F-rows | As in STOPPING_CONDITION_20261107.md | Unchanged retail/schema/native witnesses |
| MySQL retry-strategy qualification of 35 test classes | Mechanical test work, **actionable but large** | About 280 `BeginTransaction` call sites across about 50 test files must run through `CreateExecutionStrategy().Execute`; each class run takes minutes to an hour on local MySQL 8.0.46. Not started this pass: the static audit of 56 production sites found no further defect, so the expected yield is qualification, not a fix |

Conclusion: the independent slices that had usable evidence in this pass are done. The one remaining item that needs no new witness is the MySQL test-fixture conversion above, which is large and mechanical; the rest are gated as stated. This is a status, not a stopping declaration for the MySQL item.
