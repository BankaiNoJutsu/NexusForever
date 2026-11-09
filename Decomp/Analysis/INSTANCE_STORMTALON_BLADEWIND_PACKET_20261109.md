# Instance packet: Stormtalon's Lair, Blade-Wind the Invoker (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Builds on [DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md](DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md). Evidence tier: Observed (subtitle reads by the lead; two frames viewed) and Correlated (table names/ids). Not re-derived natively. No retail parity claim, no code.

## 1. Footage (dated, post-launch first; URLs only, corpus ids changed on 2026-11-09)
| Role | Date | URL | Notes |
|---|---|---|---|
| Veteran boss guide 1/3 | 2014-06-09 | https://www.youtube.com/watch?v=P7eQtXKxVlM | 249 s, subtitles, 1280x720, frames viewed at 20 to 68 s |
| Boss Guides (moepork) | 2015-09-28 | https://www.youtube.com/watch?v=8M53-Kf90a4 | 131 s, subtitles |
| Veteran boss, 2014-06-05 | 2014-06-05 | https://www.youtube.com/watch?v=1GBbV7_FxMQ | 261 s, subtitles, little detail |
| Engineer-tank run | 2014-06-18 | https://www.youtube.com/watch?v=CGni_NgcyeA | 1188 s, subtitles, phase-3 safe-spot talk |
| Veteran full runs | 2014-06 to 2015-08 | `dMFCXWRGnlk`, `supt2iMsFJU`, `OLR946zhq40`, `vKJJZYqfXn0`, `T2OJbSH7aD8`, `tT2maDc4Smk` | not yet read for this boss |
Other no-subtitle guides exist (2014-04 to 2014-07); not read. All candidate clips are 1280x720.

## 2. Skeleton (Observed)
Agreed by two independent commentators (2014-06-09 and 2015-09-28):
* Three phases. Phase 1: Blade-Wind with four invulnerable disciples and a large Thundercross telegraph (heavy damage plus knockdown, can be dodged or interrupted).
* Phase 2: everyone is disarmed on entry; the boss is invulnerable; Lightning Strike telegraphs (lock-on, then strike) on players; the disciples are shielded and the shield is removed by hitting them with the boss's own Lightning Strike; each dead disciple leaves a persistent static wisp that stuns on contact; Thundercross continues.
* Phase 3: another disarm on entry; wisps remain; a large telegraph attack that is interrupted or dodged; Lightning Strike telegraphs again.
Single-source (2015-09-28 only): phase 2 begins at 60% health; boss has two interrupt armor; two Lightning Strikes between each Thundercross in phase 2; the phase 3 large attack is named Electrostatic Pulse and is cast three times in a row (nine interrupts for the challenge). The 2014-06-09 clip gives the same attack as a rotation of "interrupt, interrupt, dodge", which is compatible with two armor then a dodge but is not the same statement.
Chat banner/yells seen at 24 s (2014-06-09, lead viewed): "Kneel before the soon-born god!" and neighbours in the chat log. Text ids 534481 to 534485, see the survey record; not referenced by any table.
Events panel at 24 s: optional objectives "Gather Corrupted Stem samples 0/15" and "Eliminate Thundercall Pell 9/20", challenge "Stormchaser" (objective 2380 in the repo enum).

## 3. Row binding (Correlated; nothing here is Mapped)
* Creature2: Blade-Wind the Invoker 17160 and 33405 (repo comment, objective 312 through TargetGroup 2585).
* Spell4 candidates in the Stormtalon id block (see the survey record): Thunder Cross 63231 (also 38786 and 63525), Lightning Strike 63252 to 63259 and 63267 (several variants), `[STL] e312 - Invoker - Static Barrier - MOO` 63171 and `... - Absorb Proxy` 63183. The internal name "e312 ... Invoker ... Static Barrier ... Absorb Proxy" is the strongest link: `e312` matches objective 312 (Blade-Wind) and "static barrier"/"absorb" matches the disciple shield. Tier: Correlated.
* Electrostatic Pulse: no row in the Stormtalon block (ids 39074, 47206, 47210, 70364 elsewhere); **unbound**.
* Effect shapes from the raw table (not cast order): Thunder Cross 63231 has a damage effect, a 2 s effect of type 0x4, a type 0x3 and a 2 s type 0x23; raw cooldown column 1500 (column meaning unverified).

## 4. Timing
None measured. An attempt to read boss health from the target-frame bar by pixel width in the 2014-06-09 clip was unreliable (the target frame alternates between the boss and disciples, so the bar went 96 to 88 to 55 to 70 to 0 to 85 teal pixels between 20 and 56 s); it is discarded. No cast gap is recorded. Needed: two clips, red/blue telegraph-area differencing at 0.5 s from encounter start, with the boss as selected target.

## 5. Open inputs
* Cast order and intervals (only "periodically" and the ratio "two Lightning Strikes per Thundercross" in phase 2).
* The binding of Thundercross, Lightning Strike and Electrostatic Pulse to a specific row per version (normal 17160 vs veteran 33405 style rows).
* Disciple creature ids, wisp spawn rule and speed, safe-spot geometry, telegraph sizes (these come from Spell4 telegraph rows and creature rows, not video).
* Second source for the 60% threshold and the interrupt armor value.

## Verdict
Blade-Wind has an agreed phase skeleton but no agreed cast order, interval or complete row binding, so per the prompt no encounter code is justified yet. Allowed next slices that need no unobserved value: (a) wire `EncounterHealthPhases` to Blade-Wind with the 60% threshold as a disabled, documented single-source placeholder; (b) the phase 2 disciple shield using rows 63171/63183 once creature ids for the disciples are found; (c) a timing pass on two clips. None started.
