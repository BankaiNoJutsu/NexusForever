# Provisional encounter framework and Stormtalon's Lair scripts (2026-11-09)

Authorised by the user on 2026-11-09 ("Allow provisional encounters"): approximate boss behaviour from observed clips and table rows, using ESTIMATED cast intervals, labelled provisional, disabled by default and never claimed as retail. This deliberately relaxes the goal prompt's no-invented-values rule for cast timing only; every other rule stands. Evidence tier of the behaviour: Source (code) over Observed/Mapped inputs; **not retail parity**.

## What exists
* `ProvisionalEncounterPlan`/`ProvisionalPhase`/`ProvisionalCast` (`NexusForever.Script.Instance/ProvisionalEncounter.cs`): phases entered at descending health fractions (via `EncounterHealthPhases`), each with casts (spell id, first delay, interval; zero interval means once per phase). Each phase replaces the previous phase's casts. A very large tick emits each cast at most once.
* `ProvisionalEncounterRunner`: reports the casts due; does nothing while disabled or out of combat; `Reset` on leaving combat. Disabled by default.
* `ProvisionalEncounterEntityScript` (`IUnitScript`, `IOwnedScript<ICreatureEntity>`): casts the runner's due spells through the owner (optionally targeting the owner's current target). Process-wide default `EnabledByDefault = false`. It does not credit objectives, so it can sit beside the existing credit scripts.
* Stormtalon's Lair (`Dungeon/StormtalonsLair/Script/StormtalonProvisionalEncounterScripts.cs`), creature filters from the repo (Blade-Wind 17160/33405, Aethros 17166/32703, Stormtalon 17163/33406); spell rows from the `[STL] e312/e313/e314` Spell4 tags:
  * Blade-Wind: Thunder Cross 38868; at 60% (single-source, disputed with 50%) Lightning Strike 38836 plus Thunder Cross.
  * Aethros: Torrent 36104 only (phases need adds, not modelled).
  * Stormtalon: Static Charge 39696; Static Wave 39717 from about 90%; Lightning Rod 39852 from about 50% (single guide).
  Not modelled: disciples, wisps, adds, tornadoes, interrupt-armor rules, target rules, phase 3 of Blade-Wind, Aethros phases 2 and 3.

## What is estimated
Every first delay and interval (15/25 s, 6/12 s, 14/30 s, 12/25 s, 10/20 s, 6/30 s, 4/25 s) is an estimate from "periodically" in guides plus the stated ratio (two Lightning Strikes per Thundercross); no retail timing source exists here. The tank/target choice of each cast is also unknown. Whether the spell rows' effects are fully handled by the server's spell system was not tested.

## Tests (Source tier)
`ProvisionalEncounterRunnerTests` 10, `ProvisionalEncounterEntityScriptTests` 4, `StormtalonProvisionalEncounterTests` 5 (creature filters, disabled by default and no credit, Blade-Wind phase 2 adds Lightning Strike): the focused run `FullyQualifiedName~ProvisionalEncounter|FullyQualifiedName~StormtalonProvisional` passed 19, 0 failed. The wider `Instances|Script` scope passed 2,495 of 2,495 (different scope, not added). Disclosure: tests and code were written in the same step and the first compile of the entity-script and Stormtalon tests needed small fixes; not run red first.

## Next
The same pattern can be applied per instance using the agent packets' agreed skeletons and the tag catalog for rows, with each plan's estimates stated. Order of the goal prompt: Kel Voreth, Skullcano, Sanctuary, Genetic Archives, Datascape, and so on. Each needs creature ids from the repo scripts, rows from the catalog, thresholds only where a packet reports them.

## Kel Voreth (2026-11-09, second slice)
`Dungeon/RuinsOfKelVoreth/Script/KelVorethProvisionalEncounterScripts.cs`: Trogun (creatures 32531/32533, verified by name against Creature2), Grond (32534/32535), Drokk (32536/32539). Rows from the `[KV] e449/e444/e446` tags: Trogun Volcanic Strike 52059 (targets the current target, since the guides say it tracks the tank), Forgemaster's Call 52639, Exanite Weapon 38882 (once at the 50% forge phase, the only threshold the second-source packet confirms with four clips); Grond Thrash 37376 and Headbutt 40176 (no thresholds: his 75% and 50% unlocks were not confirmed and Mutilate at 50% is contradicted); Drokk Homing Barrage 39422 only (his 85/65/45/25 phases were not confirmed). All delays and intervals are estimates; the orbs, volcanoes, fleas, traps, constructs and tethers are not modelled. Tests: `KelVorethProvisionalEncounterTests` 5; the focused run `ProvisionalEncounter|StormtalonProvisional|KelVorethProvisional` passed 24, 0 failed. Written together with the code, not run red first.
