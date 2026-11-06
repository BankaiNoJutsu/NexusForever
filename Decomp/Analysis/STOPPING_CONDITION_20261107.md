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
| Public 07CE (Contract types 0/1) | Retail witness | Reset edge rules (unclaimed milestones, excess points) and which of the three choice slots a player receives; native timing/UI |
| Quest reward type 10 (86 rows) | Retail witness | Which essence currency (15 to 18) is credited; server rule or a type-10 completion capture |
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
