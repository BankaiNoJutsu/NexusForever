# Restoration gaps goal prompt (2026-11-08)

Purpose: replace broad label-coverage work with a goal aimed at the specific gaps that still stand between the server and retail behaviour. Created after the ChangePhase, ChangePlane,
ChangeDisplayName and prerequisite 9/24/25/146/149/219 work and the three label passes, which showed that decompile coverage is not what limits restoration (see
[LABEL_COVERAGE_PASS_3_20261108.md](LABEL_COVERAGE_PASS_3_20261108.md)). It complements, and does not replace, the
[unified goal](UNIFIED_ASSESSMENT_RETAIL_RESTORATION_GOAL_PROMPT.md) and the [video witness goal](VIDEO_WITNESS_GOAL_PROMPT.md). Writing this file does not start anything.

```text
Continue NexusForever retail restoration for WildStar build 16042 by closing the specific gaps listed in Decomp/Analysis/RESTORATION_GAPS_GOAL_PROMPT.md, in the order given there.
Work in the worktree I:\GIT\NexusForever-unified-20261106 (branch codex/unified-restoration-20261106); the push target is origin/legacy-di-storefront-hardening in
BankaiNoJutsu/NexusForever (normal pushes only, fetch first; never force, reset, merge to main or open upstream PRs). Read AGENTS.md, ASSESSMENT_REMAINING_WORK.md (the dated notes at the top)
and the record linked from each gap before touching it. Do not start broad labelling, census or survey work that does not feed a named gap.

Rules (carried over, non-negotiable)
- Evidence ladder Observed -> Correlated -> Mapped -> Verified. Source/SQLite tests are not retail parity; never claim parity; never add test totals across incompatible scopes.
- Failing or focused test first, fix in the owning project, then run only the affected `dotnet test NexusForever.Game.Tests --filter ...` (no full-solution builds). If a test was not run red,
  say so in the record. Update (never weaken) tests that encode superseded behaviour and say which.
- Native reads: Ghidra 12.1.4 headless only (I:\ghidra_12.1.4_PUBLIC, project I:\tools\ghidra_proj, WildStar64), one Ghidra process at a time. A read by an agent is "agent-written, not
  re-derived" until the lead re-checks it; re-check the cheap facts (tables, opcode names) independently and say what was not re-checked.
- Video evidence is Observed at most and must be retail only: upload date before 2018-11-28 or official Carbine/WildStar material; emulator footage is never evidence.
- Add a dated note at the top of ASSESSMENT_REMAINING_WORK.md for every slice, linking its record with the exact evidence tier. Commit explicit paths with the Co-Authored-By line from the
  session attribution rule; do not commit .claude/ or .mcp.json.
- Native client launch and Computer Use remain stopped. The user still has to set CLAUDE_CODE_TMPDIR=I:\tools\claude-tmp and delete the empty stray directory tree I:\Users.
- Test command (PowerShell, from ...\Source): set TEMP, TMP = I:\tools\claude-tmp and DOTNET_CLI_HOME = I:\tools\claude-tmp\dotnet, then
  dotnet test NexusForever.Game.Tests -m:1 -nodeReuse:false -p:NuGetAudit=false --filter "..." and read only the `error CS|Passed!|Failed!|[FAIL]` lines.

Gaps, in order (details, inputs and acceptance in the document)
1. Verification debts from the 2026-11-08 changes (cheap, high value).
2. The remaining prerequisite types without a handler (triage, then implement what the evidence supports).
3. Path mission checklist bitmask producers (prerequisite 107 reads 0 for most slots).
4. Time-tiered challenges: per-challenge limits and completion triggers.
5. Taxi debit timing.
6. Reward track follow-ups (final-build scale, Free-to-Play claim/reset, public 07CE).
7. Telegraph shape 6.
8. Remaining spell-effect rows, only through a native read or a server system that now exists.
Stop condition: every gap is either closed with code, tests and a record, or has an exact statement of the missing input and why nothing available supplies it. Completion of restoration is
not the stop condition and no parity claim is made.
```

## 1. Verification debts from the 2026-11-08 changes
These are code already pushed whose consequences or paths were not checked. Do them first; each is bounded and each can change a behaviour.
* **Prerequisites 24/25 consequences.** They now test the player's `PhasesIPerceive` mask (default 1), so every 24/25 with a Value other than 0 is false for players until `ChangePhase` sets a bit.
  Enumerate the 134 prerequisite ids that contain a 24/25 slot and the tables that reference them (`MapZoneHex`, `MapZoneHexGroupEntry`, `Spell4`, `Item2`, `WorldLocation2`, `Creature2`, `Spline2`,
  `GossipEntry`, `VisualEffect`); list which server-evaluated gates (spell casts, gossip, vendor or item use) now fail and whether a `ChangePhase` row (161 rows, see
  [SPELL_EFFECT_CHANGEPHASE_20261108.md](SPELL_EFFECT_CHANGEPHASE_20261108.md)) is known to set that bit. Record the list; do not paper over it.
* **ChangePhase word mapping.** DataBits00 is applied to `PhasesIPerceive` and DataBits01 to `PhasesThatPerceiveMe` by analogy. Eight rows with unequal words (spells 61285, 62696, 62920, 72278,
  75501, 45279, 63307, 63369) behave differently under the opposite mapping; look for any client Lua or table text that names those spells' purpose and decide, or record that it cannot be decided.
* **Plane gates.** Prerequisites 28187 (Equal 4), 34027 (Equal 1) and 34083 (Equal 2) now fail without a `ChangePlane` source. Find what the server evaluates them for and whether bits 1 and 2 have a
  producer ([SPELL_EFFECT_CHANGEPLANE_20261108.md](SPELL_EFFECT_CHANGEPLANE_20261108.md)).
* **146 callers.** Check that the server's vendor-sell, vendor-list and achievement-objective (crafting output) paths pass `IPrerequisiteParameters.Item`; if not, 146 is false everywhere and the
  fix belongs in those callers ([PREREQUISITE_146_NATIVE_READ_20261108.md](PREREQUISITE_146_NATIVE_READ_20261108.md)).
* **9 and 149 callers.** Confirm the quest, vendor and spline paths that evaluate them see the right subject and the quest manager.
* **Untested paths.** Add tests for `UnitEntity.RemoveTrackedSpellStates` with phase, plane and display-name changes, the display-name resend in `Player.Visibility`, and the plane resend after
  add-to-map; test the three effects through a full cast, not only the handler.
Acceptance: each item has a test or a recorded finding with the evidence tier.
Status (2026-11-08): done except three untested paths and the unverifiable mapping, see [RESTORATION_GAP1_VERIFICATION_20261108.md](RESTORATION_GAP1_VERIFICATION_20261108.md); it adds a sub-gap, achievement type 33 (44 achievements, 18 gated by 146) has no server producer.

## 2. Remaining prerequisite types without a handler
A rough census (name match between `Prerequisite.tbl` and the `[PrerequisiteCheck]` attributes) shows 56 types and about 2,900 of 49,184 slots (5.8%) without a handler. The count is not clean:
`QuestObjective47` (1,788 slots) is probably a name-matching artifact because the enum name differs from the handler registration, and type 3 (360 slots) carries the placeholder enum name `value0`, so check whether it is a real unhandled type or another naming artifact; verify both first.
The rest: path holdout 154/153/126/127 (about 300 slots, needs the soldier holdout state), Schedule 49 (68) and ScheduledEvent 18 (17), FacilityWithinDistance 30 (54), Stealth 116 (32), the arena,
battleground and warplot ratings 251 to 255 (about 110, no such systems), ActionBar 239, PlayersInWorld 147, QuestObjectiveOnTarget 77 and several unknown types (142, 189, 193, 203).
Method: fix the census, then for each remaining type read the native evaluator (agent allowed, one at a time), decide whether the server has the state it needs, implement the ones that do and record
the others with the exact missing system. Prior records: [PREREQUISITE_TYPE_COVERAGE_20261107.md](PREREQUISITE_TYPE_COVERAGE_20261107.md),
[PREREQUISITE_9_149_WRITERS_NATIVE_READ_20261108.md](PREREQUISITE_9_149_WRITERS_NATIVE_READ_20261108.md) for the method.

## 3. Path mission checklist bitmask producers
Prerequisite 107 tests a bit of the mission's `ProgressCount` word ([PREREQUISITE_107_PATH_MISSION_WORD_20261108.md](PREREQUISITE_107_PATH_MISSION_WORD_20261108.md)), but the server has no producer
of checklist bitmasks for mission types 5, 6, 13, 14 and 17, so 97 of 124 retail slots read 0. Find how retail set those bits per mission type (client Lua and the mission tables first, a native read
if needed), implement the producers with tests, and re-run the 107 evaluations over the retail rows.

## 4. Time-tiered challenges
80 challenges carry `ChallengeFlags & 8`; the rule (tier goal = minimum remaining time) and the limit source (packet `TimeTotalActive`) are established, 12 limits are observed and 69 are missing, and
the completion triggers are missing: the census found 0 of 80 with a producer and `TryAdvanceProgress` has no production caller. Records:
[CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md), [CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md](CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md),
[CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md](CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md). Work in two parts: (a) the server side that is independent of footage, the progress producers
per `ChallengeType` (and the ten single-tier rows that can never complete), implemented and tested; (b) the limits, by finishing the unread OCR cue windows (about 400 later 2014 to 2015 windows
plus 2013 and 2016 to 2018) or recording the exact missing clips. Do not implement the tier selection until a limit source exists for the challenge it applies to.

## 5. Taxi debit timing
Retail debits the fare per leg near take-off (four dated clips, [VIDEO_WITNESS_TAXI_DEBIT_20261108.md](VIDEO_WITNESS_TAXI_DEBIT_20261108.md)); the server settles all legs atomically at arrival
(`ClientFlightPathPurchaseHandler`). What retail does when a later leg cannot be paid, and the refund rule, are unwitnessed. Either find a retail clip of a failed or interrupted multi-leg flight
or record that none exists; restructure the debit only with a decided failure rule. The fare itself is done ([TAXI_FARE_NATIVE_READ_20261108.md](TAXI_FARE_NATIVE_READ_20261108.md)).

## 6. Reward track follow-ups
Open: the final-build zone track scale (6,000,000 in 16042 tables against 600,000 in the 2016 PTR), a Free-to-Play era clip of claiming a zone or challenge milestone and the counter afterwards,
and the public 07CE reset/unclaimed/excess policy (do not expose a partial set of 726 choices). Records: [VIDEO_WITNESS_REWARD_TRACK_PASS3_20261108.md](VIDEO_WITNESS_REWARD_TRACK_PASS3_20261108.md),
[REWARD_TRACK_POINTS_NATIVE_READ_20261108.md](REWARD_TRACK_POINTS_NATIVE_READ_20261108.md). Use the corpus tooling in the video witness goal; wire nothing until the scale question is answered.

## 7. Telegraph shape 6
Megalith Rockfall is the candidate ([VIDEO_WITNESS_TELEGRAPH_20261108.md](VIDEO_WITNESS_TELEGRAPH_20261108.md)); nothing is measured. Needs a retail top-down clip with the spell named on screen and a
measurable area. Record the clip or the exact reason none exists; implement the shape only with a measurement.

## 8. Remaining spell-effect rows
1,752 rows over 26 types ([SPELL_EFFECT_REMAINING_20261108.md](SPELL_EFFECT_REMAINING_20261108.md)). Do not write handlers from row shape alone. Candidates that need only a native read:
ForcedAction (95), ModifyCreatureFlags (75, the flag word `DataBits03`), RestedXpDecorBonus (25). Candidates that need a server system first: UnlockActionBar (action-set unlock state),
HousingPlantSeed (garden plot and plant lifecycle), SharedHealthPool (encounter group state). The large inconclusive types (FacilityModification 771, SpellCounter 200) stay held.

## By-product rule for labels
Add durable labels only as a by-product of a read for one of the gaps above (name what you read, add the evidence row). The two open label conflicts from pass 1 (`140001ba0`, `1400d7b40` and
`1400da180`) are resolved only if a gap's read passes through them.
