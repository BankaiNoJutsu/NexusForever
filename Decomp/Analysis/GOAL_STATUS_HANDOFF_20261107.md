# Goal status hand-off (2026-11-07)

Purpose: one place that says what this stretch changed, how much of it is verified, what is only inferred (and what would falsify it), and what each remaining item still needs. It does not claim completion or retail parity. The unified goal is **not** complete.
Evidence tiers follow `CONTINUATION_GUIDE.md`; everything below is Source/SQLite unless stated; totals from different scopes are not added.

## 1. Changed this stretch (commits on `legacy-di-storefront-hardening`)
| Area | What | Commit(s) | Tier |
|---|---|---|---|
| Prerequisite evaluator corrections | Types 5, 6, 13, 14, 38, 40, 42, 43, 50, 59/280, 60, 69, 83/84, 102, 129, 130 and the boolean-tail types 28/39/86/87 corrected against the native bodies; row evaluation stops at the first empty slot | 0d75e42f, 01be4219, d7d06361, b5f1795a, 0eb587d5, 7e5189fe, bf821d54, d45f9c10, 38100573, ae9fc08b, d261300b, d63704bc | native bodies read; mostly Implemented |
| Prerequisites newly handled | 20, 53, 54, 74, 110, 111, 113, 114, 115, 162, 231 (2,902 of 49,184 slots remain unhandled, 63 types) | ea78f62b, 7b838b2d, d48e8cd0, ad8fd474, cf632dc9, db42937c, 148cf7f7, and the hazard commit | native body read; server mappings inferred |
| Prerequisite mapping corrections | Type 64 was `PathTypeLevel` but the native block compares unit resource slot ObjectId with Value (107 retail slots, **behaviour changed**); type 55 was `Currency` but compares a unit property with Value (4 slots) | last commit of this stretch | native body read; Source/SQLite |
| Inlaid augmentation list | `character_inlaid_augmentation` table (MySQL + SQLite migrations), manager, player-create list, `UnlockInlaidAugment` effect (281 rows) + packet 0x019C, prerequisite 231 | 148cf7f7, 1eb9b46c | Implemented; migration applied on a fresh MySQL 8.0.46 schema; effect-to-list link data-correlated (235 of 240 ids) |
| Telegraph shapes | Ring (1), Cross (3), three-axis (6) added; Square/Rectangle/Cross parameters treated as full dimensions | 51c5ea7a, 88c9ced7, dcf317b6 | native builder read; hit rules inferred |
| MySQL | Tolerant production-options census, 49 of 49 harness classes pass on 8.0.46; replay pins for `CharacterAtomicSave` and marketplace posting/settlement lost-acknowledgement | be8eb5c0, a61a9fd7, 2c887e1c, a025973a | real MySQL 8.0.46 / Source |
| Records | Gate passes B, C, D; per-type gate table for every unhandled prerequisite; decisions on two upstream observations | 13b672b9, a025973a, 841156d5, ea78f62b, 401c4ae3 | documentation |

## 2. Inferred, not verified (and what would settle it)
| Item | Why only inferred | Falsifier / missing witness |
|---|---|---|
| Square/Rectangle/Cross parameters are full dimensions (about 2,860 retail rows, **behaviour changed**) | The client scales extents by 0.5 on every axis and shifts a Rectangle by half its length; the box mesh/shader extents were not read | Box mesh extents or a retail capture of a Square/Rectangle boundary; revert recipe in `TELEGRAPH_BOX_EXTENTS_20261107.md` |
| Shape 6 is an ellipse (Param00 x, Param02 z, Param01 vertical) | Builder stores three unscaled radii | A shape 6 hit capture |
| 53 current episode = last `ServerPathSetCurrentEpisode` sent | Packet handler not read | Handler read or capture |
| 54 active mission = server Started state; Soldier holdout set not modelled | Activate flag set elsewhere | Activate packet reader / capture |
| 74 unlocked = tracked challenge | Retail listing of untouched challenges unknown | Capture of a fresh character's first challenge update |
| 110 progress word (types 5/6 treated plain, missing mission = 0) | Table row behind types 5/6 unread | Read `FUN_140223380` row |
| 162 percentage uses the server's required count (float mode unmodelled) | `+0xf0` mode unread | Read the objective class |
| 20 group size = member count | Group manager count read; member list assumed | Low risk |
| 111/113 hazard Enabled and meter fields | Identified from the Hazard packet field order, not a client reader | Read the hazard record reader / capture |
| 115 unit property array = `GetPropertyValue` (first float of each stride-8 entry) | Array identified by the 0xC4 bound and property-id-shaped retail rows | Read the property-table reader / capture |
| 231 / `UnlockInlaidAugment` DataBits00 = augmentation id | Data correlation only | Capture of the effect firing |

## 3. Open outcomes and their exact gate
See [LEDGER_GATE_PASS_20261107C.md](LEDGER_GATE_PASS_20261107C.md) (queue), [LEDGER_GATE_PASS_20261107D.md](LEDGER_GATE_PASS_20261107D.md) (older rows recomputed) and [PREREQUISITE_REMAINING_TYPE_GATES_20261107.md](PREREQUISITE_REMAINING_TYPE_GATES_20261107.md) (every unhandled prerequisite type). In short:
- **Needs a retail or native witness:** public 07CE (reset edge rules, `nChoiceIdx`, timing), quest reward type 10 (which essence currency), tracks 35 and Challenge producers, AMP families, #595, #557, the 2,197 remaining unhandled spell-effect rows, per-type achievement credit.
- **Needs an asset not available:** the 481,759-byte Communicator table `6a46e0e5...` (table 609).
- **Needs a runtime harness not in the repository:** broker/restart, MySQL 8.4.13, sockets, load.
- **Needs a design decision:** publish-before-commit in two Group handlers; Auth store purchase-history idempotency key (declined with reasons in `UPSTREAM_OBSERVATIONS_DECISION_20261107.md`).
- **Client stubs (no body to read):** 2,281 prerequisite slots (47, 153/154, 126, 49, 30, ...): server-only concepts needing a server or retail witness.
- **Real client bodies still unread or unmapped:** about 700 prerequisite slots (hazard 111 to 113, 149, 116, 9, 18, 239, PvP ratings, ...), each with its blocking input in the gate table.

## 4. Not claimed
No retail parity. No native client launch, no Computer Use, no retail captures were used. I cannot prove that no further slice is reachable by reverse engineering: several slices listed as gated earlier (telegraph shapes, path mission class, inlaid augmentations) turned out to be reachable.
