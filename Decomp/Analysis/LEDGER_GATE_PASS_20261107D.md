# Ledger gate pass D: re-verification of the older gate rows (2026-11-07)

Evidence tier: recomputation from the extracted client tables (`I:\tools\extract\DB`) and greps of the worktree at this commit. No gameplay claim, not retail parity. Complements
[LEDGER_GATE_PASS_20261107C.md](LEDGER_GATE_PASS_20261107C.md) (queue) and [STOPPING_CONDITION_20261107.md](STOPPING_CONDITION_20261107.md) (the older gate table), whose rows are re-checked here one by one.

| Older row (STOPPING_CONDITION table) | Re-check performed now | Result |
|---|---|---|
| Public 07CE (Contract types 0/1) | `ClientRewardTrackHandlers.cs`: the claim switch has Loyalty and Character Challenge cases only; no Contract case | Still unwired, gate unchanged (reset edge rules, `nChoiceIdx` mapping, native timing) |
| Quest reward type 10 (86 rows) | `Quest2Reward.tbl` row types: 1 x4021, 2 x228, 3 x691, 4 x378, 5 x1, 7 x10, **10 x86**; no rows of type 6, 8 or 9; type 10 rows carry `ObjectId` 0 and an amount (24 in the first rows) | 86 rows confirmed; zero rows for 6/8/9 confirmed; currency mapping still has no witness |
| Type 12 achievements (842 rows) | `Achievement.tbl`: 4,943 rows, 83 distinct types, **type 12 x842** (next: 35 x797, 6 x436, 2 x426) | Confirmed; per-script witness still needed |
| Unmapped achievement types / PvP producers | `LeaderboardScoreIngestion` is referenced only by its DI registration and its own tests; no gameplay producer calls it | Confirmed: no PvP producer exists |
| 2,478 unhandled spell-effect rows | `Spell4Effects.tbl` (131,010 rows) minus the 104 `SpellEffectHandler(SpellEffectType.X)` registrations in `NexusForever.Game`: **2,478 rows over 30 types** (FacilityModification 771, UnlockInlaidAugment 281, ChangeDisplayName 268, ModifySpell 255, SpellCounter 200, ChangePhase 161, Script 104, ForcedAction 95, ModifySpellEffect 82, ModifyCreatureFlags 75, ...) | Exactly the recorded number and top types; DataBits semantics still have no witness |
| Persistence decisions (Primal Matrix, Friendship auto-response, raid locks, guild bank-tab storage, Support case readback) | Greps of the database projects and the Friendship service for PrimalMatrix, AutoResponse, RaidLock/InstanceLock, GuildBankTab/BankTab, SupportCase/Ticket | No model or table exists for any of them; the schema/DDL decision gate stands |
| MySQL copied-upgrade / recovery / socket / load | This stretch: all 49 harness classes pass under the tolerant production options on 8.0.46 ([MYSQL_RETRY_CONVERSION_YIELD_20261107.md](MYSQL_RETRY_CONVERSION_YIELD_20261107.md)); 8.4.13, socket, broker, load not run | Narrower gate: 8.4.13, socket, broker/restart, load (runtime) |
| Communicator 609 table identity | Only the 494,256-byte `CommunicatorMessages.tbl` exists under the extract directory | Still absent; gate unchanged |
| Native-gated F-rows (F-001 to F-003, F-025 to F-031, F-033, F-034, content queue) | Not re-checkable without the native client (launch stopped by instruction) | Gate unchanged |
| Currencies 15/17 (entitlement defaults 15:1, 17:1) | `AccountCurrencyType`: 15 = CrimsonEssence, 17 = ViridianEssence; nothing in this stretch touched the entitlement files | Unchanged |

Rows closed or narrowed since the older table was written: telegraph shapes 3 and 6 and the Square/Rectangle/Cross extents (inferred, [TELEGRAPH_SHAPES_3_AND_6_20261107.md](TELEGRAPH_SHAPES_3_AND_6_20261107.md),
[TELEGRAPH_BOX_EXTENTS_20261107.md](TELEGRAPH_BOX_EXTENTS_20261107.md)); prerequisite types 5, 42, 50, 59, 60, 130, 280; the MySQL production-options qualification on 8.0.46.

Conclusion: every older row was re-checked above and its recorded number or absence reproduces. No row has gained a witness, so none can move from gated to actionable. This is the stopping-condition statement for the work reachable without
retail, native or runtime inputs, not completion.

Update (same day, later): the 2,478-row spell-effect recount above was taken before `UnlockInlaidAugment` (281 rows) got a handler; the same recomputation now gives 2,197 rows over 29 types ([PREREQUISITE_231_INLAID_AUGMENTATION_20261107.md](PREREQUISITE_231_INLAID_AUGMENTATION_20261107.md)).

## Prerequisite handler recount (same day, after types 5, 42, 50, 53, 54, 59, 60, 74, 110, 130, 231 and 280)
`Prerequisite.tbl` has 49,184 type slots; 177 handler classes are registered; **3,029 slots over 69 types** still have no handler (down from 3,841 over 73 at the start of the stretch). Largest remaining: 47 QuestObjective47 (1,788, client stub), 107 (124) and 119 (122) and 162 (8)
(participation slot `unit+0x35f8`), 154 Holdout (119), 153 HoldoutWave (95), 126 PathHoldout (72), 49 Schedule (68) and 30 FacilityWithinDistance (54) (all client stubs), 111/113/112 hazard property (87), 149 State (46), 146 ItemTradeSkillLevel (34), 9 EpisodeState (34),
116 Stealth (32), the arena/battleground/warplot ratings 251 to 255 (111) and 230 (8), 115 Unit (18), 18 ScheduledEvent (17), 239 ActionBar (12), 114 Vital (10).
Dead ends this pass, so they are not repeated: type 116 reads the client unit field `+0x54 > 1`, but 354 instructions write offset `+0x54` across the client and none could be tied to a stealth state without type information (the only stealth string is `CombatLogStealth`);
type 47 is a client stub whose `Value` is a QuestObjective id (Equal 847, NotEqual 941) with no client body to read, so its server meaning has no witness. Gates for each remaining type are in [LEDGER_GATE_PASS_20261107B.md](LEDGER_GATE_PASS_20261107B.md).
