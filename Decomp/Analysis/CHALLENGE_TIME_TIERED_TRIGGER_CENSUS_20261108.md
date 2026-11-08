# Time-tiered challenges: completion-trigger census (2026-11-08)

Read-only census; no code changed, no retail parity claim. Evidence tier: **table census + source read** (Challenge, ChallengeTier, TargetGroup, WorldLocation2, WorldZone, Creature2 from `I:\tools\extract\DB`; en-US text; server sources). Geometric and name correlations below are **inferences, not proof**. Builds on [CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md) (80 rows with flag 0x8; the limit is per-challenge server data).

## 1. What Challenge.tbl gives per flagged row (columns per `ChallengeEntry.cs`)
Columns 2..9 are `Target`, `ChallengeFlags`, `WorldZoneIdRestriction`, `TriggerVolume2IdRestriction`, `WorldZoneId`, `WorldLocation2IdIndicator`, `WorldLocation2IdStartLocation`, `CompletionCount`; 17..21 are `VirtualItemIdDisplay`, `TargetGroupIdRewardPane`, two `QuestDirectionId`, `RewardTrackId`. Of the 80: indicator WL2 set on 64, start WL2 on 52 (both on 51; indicator == start on 8: 294, 325, 546, 666, 1014, 1054, 1058, 1078), neither on 15, `TriggerVolume2IdRestriction` on 16 (no TriggerVolume2 table exists in the extract, so those volumes are unreadable), Target or reward-pane group on 23, VirtualItem on 2 (47, 690). `CompletionCount` is -1 everywhere except 1105/1106 (6). Nothing in the tables stores a limit, a finish, a count or a waypoint list.
- **Indicator = finish (inference).** For the 42 General rows with distinct indicator/start in one world, the indicator zone name equals the destination named in the description in all 4 rows I checked that name a destination outside the start zone (Thayd Sprint -> Fortune's Ground; Supply Run -> Camp Dustdevil; Swiftfoot Supplies -> Elder Wisdom Tree; Contents Under Pressure -> Vigilant Heart Outpost), and for 13 of 14 "top/climb" names the indicator is higher than the start (median +81 units on Position1). Indicator radii are small (1 to 30) on race/climb rows. When indicator == start (8 rows) it is the play area, not a finish. Retail use of the column is not read from the client.
- **Target namespaces.** Types 0/7/5 targets are TargetGroup ids (type 1/9 creature-id lists): 271 Slank (24407/24139/24293), 276 Scanbot 5000, 546 Generator Controls, 957 Explorer Radar Dish, 1105/1106 six Kit/Corrigan item objects each, 817 six creatures, 839 Gnasher, 1136 four squirg zombies (75247, 75263-75265). 244/245 -> creatures 23623/23624 (absent from Creature2.tbl), 304/314 -> group 3 = creature 1 (placeholder), 996 target 131 resolves to nothing. 690 (type 1) target 41777 = Spell4Base "Osun Afterblaster" (unconfirmed as the rock throw; its reward pane group 5096 = Pronto the Peep). General Targets (166 -> 3 spiderbot ids, 477 = 31722, 956 = 44441, 1085 -> creature 11379 absent) do not fit their descriptions in either namespace: unresolved. Reward-pane groups name the objective object: Pulp-Gulp bottle (666), Electrostatic Container (743), Vind Plushie at the summit (753).
- 244-247, 304, 314 are zone 262 "Quest Test Island" (text-less QA rows); 332, 461, 823, 996 have no text; their WorldZoneId 1480 is not in WorldZone.tbl.

## 2. Server census (producer status)
- Producers that exist: (a) `UnitEntity.RewardKiller` -> `ChallengeCombatHooks` credits **Combat (0)** challenges whose Target group contains the creature (and auto-starts on flag 0x20); (b) `InteractionObjectiveUpdater` -> `ChallengeActivationHooks` credits **ChecklistActivate (7)**; (c) `ChallengeProgressHooks.OnChallengeProgress` / `TryAdvanceProgress` has **zero non-test callers** (no script project, map script, zone or trigger code calls it). Only `ChallengeType.Combat` and `ChecklistActivate` are referenced outside the enum and tests.
- Grep of all 69 distinct flagged names and the ids over `Source` (scripts included): no challenge match (only the unrelated quest `Q5573PoweringDown` and the phrase "The Grid"). The only per-id special case is 103 Skeech Slayer.
- No code reads `WorldLocation2IdIndicator/StartLocation`, `TriggerVolume2IdRestriction` or `VirtualItemIdDisplay`; `TryActivate` checks only `WorldZoneIdRestriction`; `LeftArea` is never set true at runtime (area-fail is dead); timer expiry deactivates with `TimerExpired` and no tier. `WorldLocationVolumeGridTriggerEntity` (radius plus vertical clamp) exists for quest EnterArea and could be reused but is not wired to challenges.
- **Classification of the 80: producer 0; partial hook 10; nothing 70.** Partial = 244, 245, 271, 304, 314 (kill hook counts kills) and 276, 546, 957, 1105, 1106 (activation hook counts interactions), but their tier goals are milliseconds (or 2683995..) read as counts and the zero first goal is compacted away, so no tier is reachable. 271 would even auto-start on a Slank kill, then need 60000 kills.
- **Structural blockers even with a producer:** 10 single-tier `[0]` rows (22, 470, 743, 753, 996, 1035, 1054, 1058, 1116, 1136) get `goalCount` 0, so `TryAdvanceProgress` returns false forever; 51 multi-tier rows with first goal 0 lose Bronze to compaction; durable rewards exclude flag 0x8 so these use the legacy path; all use the 300 s default limit.

## 3. Mechanism groups (from description text, Target/reward-pane data and locations)
| Group | n | Type mix | Shared rule candidate | Data present | Data missing |
|---|---|---|---|---|---|
| R reach/climb on foot | 31 | 2 | player enters finish WL2 while active; tier = highest goal <= remaining time | indicator distinct from start on 26 | finish for 753, 776, 850, 1116 (no WL2) and 1014 (ind == start); limit; verify finish = indicator |
| V reach by vehicle/mount | 3 | 2 | R plus mounted check | 511, 671 indicator distinct | 325 finish (ind == start); mount ids; limit |
| W waypoint/obstacle sequence | 6 | 2 | ordered checkpoints then finish | indicator on all | waypoint/ring/geyser/maze entity ids and order; 470 ball entity; 477 geyser timing |
| H reach with hazard fail | 6 | 2 | R plus fail-on-event (beam, mine, jetpack, light) | indicator on all | hazard event definitions; 979/981 jetpack-use rule |
| D carry / deliver / follow | 6 | 2 | R plus carried item or creature alive | indicator on 4 | carried item or follower ids; 1054/1058 destinations (ind == start); 456 explode timer |
| I interact/collect N objects | 14 | 7 x5, 5 x2, 2 x7 | count interactions on Target group (hook exists for type 7) | groups for 276, 546, 957, 1105, 1106, 817, 839 | N per challenge (goals are ms, not counts) for all 14; object ids for 273, 294, 391, 508, 666, 910, 1078 |
| K kill N / kill target | 7 | 0 x5, 1, 2 | kill hook (Combat) | groups for 271, 1136, 244, 245 | N or kill-all rule; 690 spell and kill flow; 304/314 are placeholders |
| S survive for duration | 1 | 2 | timer expiry = success (743, goals `[0]`) | reward-pane creature | confirmation that expiry completes; limit |
| Q text-less/test | 6 | 2 x5, 5 | none | none | existence in retail unknown (QA island, no text) |

R plus V (34 rows, 28 with a usable indicator) is the only group that shares one completion shape; W/H/D reuse it plus extra events; I/K need counts the tables do not hold.

## 4. ChallengeType among the flagged rows
`ChallengeType` (Combat 0, Ability 1, General 2, UNUSED01 3, Item 4, Collect 5, ChecklistActivate 7) is identical to upstream PR #498 (file never modified since). Type 0 (5 rows): kill hook, Target as group, ms goals read as counts. Type 1 (690): no hook; code says ability targets name spells and `ChallengeAbilityActivationTests` asserts that interactions and reward-pane membership do not credit it. Type 5 (817, 839, 996): enum only, no server code path; the client `ChallengeLog.lua` has no Collect entry (no area timer, icon or plural), so these rows may not be startable at all (unverified). Type 7 (276, 546, 957, 1105, 1106): activation hook, same count/ms mismatch.

## 5. The three unflagged ms-valued rows
- **423 and 1115 Elemental Purge** (type 0, goals 0/30000/60000, flags 0, Halon Ring, restrictions 4522/4523, quest directions 2305/2306): "Kill creatures in the Chambers of the Primevals", Target group 12287 (type 11 OR of 12288-12294, murgh and pumera exotic models). Most likely a kill-**score** challenge (tiers are points), not time. Per-creature point values are not in the tables; the server adds 1 per kill.
- **499 Dummy Damage** (type 2, 5000/10000/20000, flags 0, Auroria): "Do as much damage as you can to the Voreth Dummy before time runs out": damage dealt within a limit, ascending thresholds (count-style). No damage producer exists; `TryAdvanceProgress(damage)` would fit the existing count semantics.

## 6. Per-challenge table
Legend: ty = ChallengeType; mech = group above; prod = N none, P-kill/P-act partial hook; tiers = goals in seconds (ms/1000; a bare 1 is 1 ms; 314 raw); data = I indicator WL2, S start WL2, V TriggerVolume2 restriction, T Target or reward-pane group; gap = missing beyond the limit (fin? = candidate finish is the indicator, NOFIN = no usable finish, N = count).

| id | name | ty | mech | prod | tiers | data | gap |
|---|---|---|---|---|---|---|---|
| 22 | Treeclimber | 2 | R | N | 0 | ISV- | fin? |
| 47 | Supply Run | 2 | D | N | 0/90/120 | IS-- | carry+fin? |
| 67 | Windspire Ascent | 2 | R | N | 1/11/23 | IS-- | fin? |
| 116 | The Grid | 2 | H | N | 1 | I--- | haz+fin? |
| 118 | Eldan Wall Scaler | 2 | R | N | 0/30/40 | IS-- | fin? |
| 119 | Eldan Construct Border Sca | 2 | R | N | 0/30/40 | IS-- | fin? |
| 166 | Windrunner | 2 | R | N | 0/15/30 | IS-T | fin? |
| 184 | Scaling Mount Tempest | 2 | R | N | 1/30/75 | IS-- | fin? |
| 194 | Race to the XAS Dig Site | 2 | R | N | 0/180/225 | I--- | fin? |
| 204 | VISTA: Gallow Fields | 2 | R | N | 0/240 | IS-- | fin? |
| 244 | (no text) | 0 | K | P-kill | 10 | ---T | N/score |
| 245 | (no text) | 0 | K | P-kill | 10/20/30 | ---T | N/score |
| 246 | (no text) | 2 | Q | N | 10 | ---- | unknown |
| 247 | (no text) | 2 | Q | N | 10/20/30 | ---- | unknown |
| 265 | To the Roof | 2 | R | N | 0/180/240 | IS-- | fin? |
| 271 | Cellar Dwellers | 0 | K | P-kill | 0/60/150 | I--T | N/score |
| 273 | Bringin' the Bingberries | 2 | I | N | 0/100/180 | I--- | obj+N |
| 276 | Surveying Device Says... | 7 | I | P-act | 0/45/120 | I--T | obj+N |
| 294 | Veggie Delight | 2 | I | N | 0/60/120 | IS-- | obj+N |
| 304 | (no text) | 0 | K | P-kill | 0/30/60 | ---T | N/score |
| 314 | (no text) | 0 | K | P-kill | 2683995/2683996/2683997 | ---T | N/score |
| 325 | Owanee Speedway | 2 | V | N | 0/30/40 | IS-- | NOFIN |
| 329 | Race to the Top | 2 | R | N | 0/28/33 | IS-- | fin? |
| 332 | (no text) | 2 | Q | N | 0/100/180 | -S-- | unknown |
| 391 | Skeech Saboteur | 2 | I | N | 60/120/200 | IS-- | obj+N |
| 422 | Getting a Better Signal | 2 | R | N | 0/30/45 | IS-- | fin? |
| 435 | Stairway to Heaven | 2 | R | N | 0/60/90 | IS-- | fin? |
| 456 | Contents Under Pressure | 2 | D | N | 0/90/135 | IS-- | carry+fin? |
| 461 | (no text) | 2 | Q | N | 0/30/60 | IS-- | unknown |
| 470 | Kickin' It | 2 | W | N | 0 | ISV- | way+fin? |
| 477 | Splashin' Dash | 2 | W | N | 120 | ISVT | way+fin? |
| 500 | Murgh Along | 2 | R | N | 0/30/60 | IS-- | fin? |
| 506 | VISTA: Hijunga Village | 2 | R | N | 0/5 | IS-- | fin? |
| 508 | Can You Dig It? | 2 | I | N | 0/60 | I--- | obj+N |
| 511 | Ravenok Racer | 2 | V | N | 0/10/20 | IS-- | fin? |
| 515 | Shock to Your Systems | 2 | H | N | 0/45 | I--- | haz+fin? |
| 529 | Swiftfoot Supplies | 2 | D | N | 0/30 | IS-- | carry+fin? |
| 546 | Powering Down | 7 | I | P-act | 0/30/60 | IS-T | obj+N |
| 559 | One Small Hop | 2 | R | N | 0/15/20 | IS-- | fin? |
| 579 | A Spirited Climb | 2 | R | N | 5/20/35 | IS-- | fin? |
| 609 | Rock Climber | 2 | R | N | 0/7/15 | IS-- | fin? |
| 641 | XT-9 Hazardous Hallway | 2 | H | N | 1/20/40 | IS-- | haz+fin? |
| 654 | Jungle Dash | 2 | R | N | 1/50/80 | IS-- | fin? |
| 655 | To the High Flame | 2 | R | N | 1/40/60 | IS-- | fin? |
| 662 | View From a Hill | 2 | R | N | 0/60/75 | IS-- | fin? |
| 666 | Pulp-Gulp Guzzler | 2 | I | N | 60/90/120 | ISVT | obj+N |
| 671 | Faster and Fearless | 2 | V | N | 0/20/40 | IS-- | fin? |
| 680 | Race to the Unknown | 2 | R | N | 0/45/56 | IS-- | fin? |
| 686 | How to Save a Life | 2 | D | N | 0/30/60 | IS-- | carry+fin? |
| 690 | Peep Chase | 1 | K | N | 0/30/60 | ---T | N/score |
| 691 | Dockside Delivery! | 2 | R | N | 0/30/45 | IS-- | fin? |
| 743 | Grounding the Storm | 2 | S | N | 0 | --VT | timer rule |
| 753 | Top of the World | 2 | R | N | 0 | --VT | NOFIN |
| 776 | Broodrot Barnstorming | 2 | R | N | 1/45/60 | ---- | NOFIN |
| 817 | Honey Samples | 5 | I | N | 0/120/240 | I--T | obj+N |
| 823 | (no text) | 2 | Q | N | 0/30/60 | ---- | unknown |
| 839 | Phantasmal Flora Collectio | 5 | I | N | 0/120/240 | I--T | obj+N |
| 850 | Mudwalker Ascent | 2 | R | N | 0/15/25 | ---- | NOFIN |
| 910 | Sandcastle Critique | 2 | I | N | 0/5/10 | ---- | obj+N |
| 923 | Entrance Exam | 2 | W | N | 0/540/570 | I--- | way+fin? |
| 956 | Back to Basic | 2 | W | N | 0/18/25 | IS-T | way+fin? |
| 957 | Just Keep Swinging | 7 | I | P-act | 0/90/180 | ISVT | obj+N |
| 973 | Red Light Green Light | 2 | H | N | 0/15/25 | ISV- | haz+fin? |
| 979 | Doing Things The Hard Way | 2 | H | N | 0/180/360 | ISV- | haz+fin? |
| 981 | Doing Things The Hard Way | 2 | H | N | 0/180/360 | ISV- | haz+fin? |
| 996 | (no text) | 5 | Q | N | 0 | I--T | unknown |
| 1002 | Thayd Sprint | 2 | R | N | 1/60/70 | IS-- | fin? |
| 1014 | Windwalker | 2 | R | N | 1/10/30 | IS-- | NOFIN |
| 1035 | Imbuement: A Hop, Skip, an | 2 | R | N | 0 | ISV- | fin? |
| 1048 | Whirlwind Racer | 2 | R | N | 0/20 | IS-- | fin? |
| 1054 | Imbuement: An Urnest Reque | 2 | D | N | 0 | IS-- | carry+NOFIN |
| 1058 | Imbuement: The Windswept P | 2 | D | N | 0 | IS-- | carry+NOFIN |
| 1078 | Diagnostic Tests | 2 | I | N | 0/180/265 | ISVT | obj+N |
| 1079 | Resonance Runner | 2 | W | N | 0/45/90 | ISV- | way+fin? |
| 1085 | Windy Climb | 2 | R | N | 1/37/75 | IS-T | fin? |
| 1105 | Another Sassy Mess | 7 | I | P-act | 0/30/60 | I--T | obj+N |
| 1106 | Another Scruffy Mess | 7 | I | P-act | 0/30/60 | I--T | obj+N |
| 1116 | Climbing the Pyre | 2 | R | N | 0 | --V- | NOFIN |
| 1134 | Diabolic Drift Dash | 2 | W | N | 0/108/138 | ISV- | way+fin? |
| 1136 | Squirg Run | 2 | K | N | 0 | --VT | N/score |

## 7. Evidence that would unblock each group
- **R/V (34 rows)**: confirm finish = indicator with one retail run per sample where the finish position is known (Thayd Sprint: indicator 41555 at 3900,-859,-1683 radius 10, zone Fortune's Ground; Rock Climber, Windspire Ascent), plus the limit; ideally the client consumer of the `WorldLocation2IdIndicator` column. Rows without a finish location (753, 776, 850, 1014, 1116, 325) need footage of the finish or the `TriggerVolume2` data.
- **W/H/D**: per-challenge waypoint, ring, hazard and carried-object ids (creature spawns in world data, not in tables) and hazard rules from footage (979/981 jetpack, 973 light cycle, 116/641 beams, 515 mines).
- **I/K**: the per-challenge N (goals are ms, so N lives elsewhere) from footage or progress text, and confirmation that the existing type-7 hook credits the right objects (Scanbot, Generator Controls, Radar Dish, item objects).
- **S (743)**: footage showing expiry = completion.
- **Cross-cutting**: limits for 77 of 80 (observed: 1002, 956, 979, 753; community: 546); a design decision for the first-tier goal 0 and the 10 `[0]` rows (a completion path that does not use `goalCount`); whether `TriggerVolume2IdRestriction` (16 rows, table not extracted) is the area-fail volume.

Review note (lead, 2026-11-08): written by a research subagent. Spot-checked by the lead: `TryAdvanceProgress`/`OnChallengeProgress` have no production callers (only the manager, the hook wrapper and the interface). The mechanism grouping and the indicator-as-finish inference are the agent's reading and are unverified.
