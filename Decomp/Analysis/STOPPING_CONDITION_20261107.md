# Stopping condition statement (2026-11-07)

Correction (same day): the matrix triage's small Source gaps were still actionable, so "no actionable independent slice
remains" was premature when first written. Three were fixed in [SOURCE_ROBUSTNESS_FIXES_20261107.md](SOURCE_ROBUSTNESS_FIXES_20261107.md).
The two remaining micro-slices (UnitEntity Spell4 guards, RavelSignal dispatch pins) are now also done; everything in the table
below is unchanged and gated.

The unified prompt's stopping condition: after a full pass of the ledger, no actionable independent slice remains,
and the gates blocking each remaining item are stated precisely. This is a stopping condition, not completion and
not a retail-parity claim. Passes: [LEDGER_GATE_PASS_20261106.md](LEDGER_GATE_PASS_20261106.md) (queue, F-001 to F-036,
T-series, content inventory), plus raw-table censuses ([RAW_TABLE_EXTRACTION_20261106.md](RAW_TABLE_EXTRACTION_20261106.md),
[CONTRACT_FULL_TABLE_CLASSIFICATION_20261106.md](CONTRACT_FULL_TABLE_CLASSIFICATION_20261106.md),
[ACHIEVEMENT_ZONE_TABLE_CENSUS_20261106.md](ACHIEVEMENT_ZONE_TABLE_CENSUS_20261106.md),
[ACHIEVEMENT_UNMAPPED_TYPE_CANDIDATES_20261106.md](ACHIEVEMENT_UNMAPPED_TYPE_CANDIDATES_20261106.md),
[COMMUNICATOR_RETAIL_REPUTATION_CORPUS_20261106.md](COMMUNICATOR_RETAIL_REPUTATION_CORPUS_20261106.md),
[SPELL_EFFECT_HANDLER_CENSUS_20261106.md](SPELL_EFFECT_HANDLER_CENSUS_20261106.md),
[QUEST_OBJECTIVE_TYPE_CENSUS_20261106.md](QUEST_OBJECTIVE_TYPE_CENSUS_20261106.md)).

## Completed or excluded with evidence this continuation

- Scalar Character save acknowledgement (9a0939a0): fixed, Source/SQLite.
- Quest AccountItem reward (1edfaf74): paired settlement; retail data has no type 6/8/9 rows (excluded, 0 rows).
- F-036 non-title zone rewards: excluded (ZoneCompletion has no other reward column).
- Communicator 609 reputation predicate: corroborated against all 23 retail rows (zero-bound handling already implemented).
- Quest objective types: 10,026 of 10,031 rows covered; 5 stray rows unused.
- Contract tracks 4/5: all 726 choices classify (Item2/AccountItem, count 1) against hash-matched tables.
- Mixed-bundle quest atomicity: no gap reproduced. Leaderboard ingestion: pinned, no defect.

## Remaining items and the exact blocking input

| Item | Blocking gate | Exact missing input |
|---|---|---|
| Public 07CE (Contract types 0/1) | Retail witness | Reset edge rules (unclaimed milestones, excess points); native `nChoiceIdx` mapping to the table's three columns; native timing. Choice policy is now evidenced as player-selected by index (CLIENT_LUA_CONTRACT_AND_REWARD_EVIDENCE_20261107.md) |
| Quest reward type 10 (86 rows) | Retail witness | Which essence currency (15 to 18) is credited; server rule or a type-10 completion capture. Client UI does not render type 10 and holds no currency mapping |
| Type 12 achievements (842 rows) | Per-script witness | Which content script credits each achievement; requires reading/porting each encounter's script behavior |
| Other unmapped achievement types (87/88/96/61/94/102, 109/112/114/83, etc.) | Producer witness | Credited actor and trigger (e.g. type 109 inviter vs invitee); PvP producers (kills, arena rating) also do not exist yet (LeaderboardScoreIngestion has no callers) |
| 2,478 unhandled spell-effect rows (FacilityModification 771, UnlockInlaidAugment 281, ChangeDisplayName 268, ...) | Retail witness | Per-type DataBits semantics and observable result; the client holds name strings only |
| AMP/ModifySpell families, #557 Armor Pierce, #595 shield healing/Focus, procs, stacks/CC, summons/AI | Native/retail witness | Server formula/ordering evidence; not present in the client binary or tables |
| Primal Matrix save, Friendship auto-response, raid locks, guild bank-tab storage, Support case readback | Schema/DDL decision | A persistence design decision plus (for auto-response) a retail restore witness |
| MySQL copied-upgrade / recovery / socket / load qualification, cross-guild durable replay, broker/restart (fresh forward migration on 8.0.46 now passes: see MYSQL_8_0_FRESH_MIGRATION_QUALIFICATION_20261107.md) | Harness + runtime | The earlier MySQL 8.4.13 adapter harness (not in this repository) or a new one; MySQL 8.0 and the reference databases are running locally; socket/broker runtime is not configured |
| Native-gated rows (F-001 to F-003 STS/diagnostics/aux packets, F-025 to F-031, F-033, F-034, content restoration queue) | Native client / retail witness | Accepted live captures or native consumer/producer mapping; native client launch and Computer Use remain stopped by instruction |
| Communicator 609 table identity | Reconciliation | The 481759-byte table `6a46e0e5...` is not in this client; the 494,256-byte table here is used instead |

Tooling state: REA 6.8.0 and Ghidra 12.1.4 are configured at project scope and the doctor passes for Ghidra; a headless
analysis of `WildStar64.exe` is running outside the repository. Native string probes show the client holds no
achievement-type or spell-effect server semantics, so it is not expected to close the rows above.

## Re-verification (2026-11-07, at 6390ac86)

Re-read against the repository, remote and tooling after the later work. Local and `origin/legacy-di-storefront-hardening`
are both at 6390ac86; every document linked above exists. Changes to the gate table since it was first written:

- Source robustness: all five matrix-triage micro-slices are done ([SOURCE_ROBUSTNESS_FIXES_20261107.md](SOURCE_ROBUSTNESS_FIXES_20261107.md)).
- MySQL: fresh-schema and copied-upgrade forward migration pass on local MySQL 8.0.46
  ([MYSQL_8_0_FRESH_MIGRATION_QUALIFICATION_20261107.md](MYSQL_8_0_FRESH_MIGRATION_QUALIFICATION_20261107.md)). Recovery, retry-strategy,
  public-flow, socket, broker and load qualifications still need the adapter harness or runtime.
- Contract 07CE: choice policy (player-selected, zero-based choice index) and enum classification are corroborated by the
  client Lua and decompilation ([CLIENT_LUA_CONTRACT_AND_REWARD_EVIDENCE_20261107.md](CLIENT_LUA_CONTRACT_AND_REWARD_EVIDENCE_20261107.md));
  the reset edge rules have no evidence anywhere reachable (client Lua and decompiled presentation code show none), so 07CE stays unwired.
- Unmapped achievement types: description-based candidates exist for most
  ([ACHIEVEMENT_UNMAPPED_TYPE_CANDIDATES_20261106.md](ACHIEVEMENT_UNMAPPED_TYPE_CANDIDATES_20261106.md)); crediting actor/trigger
  per type still needs a producer witness, and the PvP producers they depend on do not exist in source.
- Ghidra 12.1.4 analysis of `WildStar64.exe` is complete; it yields presentation-side names/structure only.

Every remaining row in the table above is blocked on an input that does not exist in the repository, on this machine's tables,
the client binary, its Lua, or the local databases: a retail/server-side witness (07CE reset rules, type-10 currency, per-type
achievement and spell-effect semantics), a schema/DDL design decision, or the recovery/socket/broker/load harness and runtime.
No independent Source/SQLite slice with usable evidence remains.

Update: an opt-in MySQL driver runs the 52 existing provider-option test classes plus the three Character+Auth two-database classes (Contract claim,
crafting settlement, bank recovery) on real MySQL 8.0.46 and all pass with plain options. Under the production options (retry strategy plus string concurrency
generator) 16 of 55 classes pass as written; most failures are tests that open their own transactions, and a few Contract cases are unexplained
(see the census section of MYSQL_8_0_FRESH_MIGRATION_QUALIFICATION_20261107.md). Remaining MySQL-gated work: wrapping those tests for the retry strategy or
explaining the Contract/InventoryRewardDelivery differences, the Auth-only classes without a provider seam, MySQL 8.4.13, socket, broker and load.

Update (retry strategy): the production-options MySQL run found and fixed a real recovery defect in `ContractCompletionExecutor`
([CONTRACT_COMPLETION_RETRY_STRATEGY_RECOVERY.md](CONTRACT_COMPLETION_RETRY_STRATEGY_RECOVERY.md)). The retry-strategy gate is therefore partly worked, not closed: other
classes still need their tests made strategy-aware (or each failure traced to a production site) before the production-options run can be claimed for them.

Update: the two previously unexplained production-options failure groups (Contract overflow, InventoryRewardDelivery Contract admission and allocation) are traced to
the same test-opened-transaction cause (see the MySQL record). No remaining production-options failure is attributed to a production site; the remaining MySQL work is
mechanical test-fixture work, Auth-only classes without a provider seam, MySQL 8.4.13, and socket, broker and load runs.

Update: the Auth-seam classes (account currency, crafting account outbox, world session admission, account keybinding) now run on MySQL 8.0.46 and pass with plain options;
under the production options two pass fully and two fail only through test-opened transactions. Remaining MySQL work is test-fixture strategy-awareness for the other
classes, MySQL 8.4.13, and socket, broker and load runs.

Update: a static audit of all 56 production transaction sites found no further instance of the retry-strategy recovery defect, and three pins confirm the EF scope assumption the
fragile sites rely on ([EXECUTION_STRATEGY_SCOPE_AUDIT_20261107.md](EXECUTION_STRATEGY_SCOPE_AUDIT_20261107.md)).

Update: all 59 driver classes pass on MySQL 8.0.46 with the production string-concurrency generator and no retry strategy (1.24 hours), so that production difference is qualified at
this tier; only the retry-strategy runs have failures (test-opened transactions, plus the fixed Contract recovery defect).

## Re-verification (2026-11-07, after the native-evaluator work)

The first version of this record said the client binary "holds presentation-side names/structure only" and that no independent Source/SQLite slice with usable evidence remained. Later work on the same day
showed the client does hold server-relevant semantics where it mirrors a server decision: the prerequisite evaluator and its row builder, the TargetGroup evaluator, and the Lua accessors of runtime objects.
That produced independent slices (each committed with its own record and evidence tier in `ASSESSMENT_REMAINING_WORK.md`): prerequisite types 31, 44, 143, 201, 61/67/155, 196, 141, 273, 217, 56 (inferred), 124/125/160 and the
corrected 22/23/33-36, 11 and 52 comparisons; the TargetGroup evaluator (types 1/2/10/11, zero slots), quest objective, public event and challenge target membership; prerequisite row evaluation
(slots after an empty slot are never evaluated); and the Ring telegraph shape (inferred). The stopping statement above is therefore **superseded**, not confirmed: it must not be read as "nothing is left".

Current prerequisite state: 3,841 of 49,184 slots (7.8%, 73 types) still have no handler. What remains, with the exact missing input:

| Types (slots) | Blocking input |
|---|---|
| 47 (1,788), 153/154 (214), 126 (72), 49 (68), 30 (54), 163/164 | The client evaluator slot is the shared `return 0` stub, so the meaning is server-side only: needs a server or retail witness |
| 54 (256), 110 (47), 53 (33) | Path mission class virtuals `+0x28`, `+0x30`, `+0x58` unidentified (`IsComplete +0x38`, state `+0xd0` are known); see "Path mission virtuals identified from the Lua accessors" |
| 231 (241) | The player-held 16-bit AMP list read by 231 is a different list from the enabled-AMP list; its writer is unidentified |
| 74 (236) | Field `+0x2c` of the client challenge record is `Unlocked`; the client's record set for untouched challenges (retail first challenge update) decides Equal/NotEqual for them |
| 107 (124), 119 (122), 162 | Per-player participation slot `unit+0x35f8` and the current-episode rule |
| 111/113 (83), 239, 18, 146, 9, 114/115, 116, 194/195/232/292 | Unit or manager state objects unidentified; see the per-type sections of `PREREQUISITE_TYPE_COVERAGE_20261107.md` |
| 249 to 255 arena/warplot ratings (~150) | PvP rating producers and storage do not exist in the server |

Other gates unchanged: public 07CE reset rules, quest reward type 10 currency, per-type achievement credit, the 2,478 unhandled spell-effect rows, persistence design decisions (Primal Matrix, Friendship, raid locks, guild bank
tabs), the MySQL qualification runs, and everything native-client or retail-witness gated. New items from this pass: the creature visibility prerequisite
([CREATURE_VISIBILITY_PREREQUISITE_20261107.md](CREATURE_VISIBILITY_PREREQUISITE_20261107.md): 4,420 creatures, unknown evaluating side) and telegraph shapes 3 and 6 (263 rows, the client renderer's shape code
not reached, see [TELEGRAPH_RING_SHAPE_20261107.md](TELEGRAPH_RING_SHAPE_20261107.md)).
