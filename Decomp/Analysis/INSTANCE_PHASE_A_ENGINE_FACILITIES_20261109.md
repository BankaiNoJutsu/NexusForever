# Instance encounters, Phase A: engine facilities (2026-11-09)

Part of [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Evidence tier: Source (code read and unit tests). No retail parity claim; no boss value is encoded.

## Inventory (code read this session)
| Facility | State | Where |
|---|---|---|
| 1. Health-threshold phases | Existed only as inline per-boss checks (Katja Zarkhov: `health/max < 0.55`). **Added** a reusable tracker. | `NexusForever.Script.Main/AI/EncounterHealthPhases.cs` |
| 2. Timed and sequenced casts | Exists: `IScriptEventManager` (delay, random range, cancel by id/type), `IEntityCastEvent` (cast a Spell4 id, optional current target), plus the `CombatAI` special-attack profile system. | `NexusForever.Script/Template/Event`, `NexusForever.Script.Main/AI` |
| 3. Interrupt armor | Exists as an entity vital/property (`InterruptArmor`, `Property.InterruptArmorThreshold`). Not verified: how an interrupt affects a pending `IEntityCastEvent`. | `IWorldEntity`, `UnitEntity` |
| 4. Add waves, summons | Exists: `IEntitySummonEvent`, `SummonFactory`. Not verified: invulnerable-until-condition adds and shield removal. | `Template/Event`, `Katja` script |
| 5. Reset on wipe | Pattern exists per script (`OnLeaveCombat` cancels events and clears flags, Katja). No shared helper; spawn, door and platform restore not verified. | scripts |
| 6. Yells | `IEntitySayEvent` existed; **added** `IEntityYellEvent`/`EntityYellEvent` (`NpcYell`, default range 310) since the retail boss lines appear in the yell channel. Text ids are not resolved; callers pass the string. | `Template/Event/EntityYellEvent.cs` |

Event types register by reflection (`AddScriptTemplateEvent`), so the new yell event needs no extra registration.

## Added
* `EncounterHealthPhases(params float[] fractions)`: each fraction strictly between 0 and 1 and unique; `Update(health, maxHealth)` returns the newly reached thresholds highest first (reached when health fraction is less than or equal to the threshold, so "at 60%" fires at exactly 60%); each fires once per fight; healing back does not re-arm; `Reset()` re-arms (call on leave combat/wipe); `HasCrossed(index)`; zero max health ignored.
* `EntityYellEvent`/`IEntityYellEvent`.
* Tests: `EncounterHealthPhasesTests` (12 cases) and `EntityYellEventTests` (3 cases, including the delay and cancel behaviour through `ScriptEventManager`). Focused run: 15 passed, 0 failed. Disclosure: the tests and the code were written together and the tests were not run red first (they cannot compile without the new types); the first run after implementation passed. The boundary rule (less than or equal) is a choice, not an observation: the guides say a phase "begins when the boss reaches 60%".

## Not done and not verified (open Phase A items)
* Effect of an interrupt on a scheduled cast (item 3), and whether interrupt armor can be depleted by script-cast spells.
* Invulnerable-until-condition adds, shield removal and despawn on wipe (item 4).
* A shared wipe/reset helper that restores spawns, doors and platforms (item 5); today only per-script event cancellation exists.
* Any wiring of `EncounterHealthPhases` into `UnitEntity.OnHealthChange` handlers: scripts must call `Update` from their own `OnHealthChange`; no script uses it yet.
These are the next engine slices, to be built against the first boss that needs each (Blade-Wind needs the invulnerable disciples and shield removal).

## Update (2026-11-09, after the provisional encounter work)
* **Item 1 (health thresholds) closed:** `EncounterHealthPhases` is now used by `ProvisionalEncounterRunner` (every provisional boss script) and by the disabled Blade-Wind and Trogun phase placeholders, with tests.
* **Item 2 (scheduled casts) closed for provisional use:** `ProvisionalEncounterRunner` plus `ProvisionalEncounterEntityScript` cast spells on estimated delays and intervals through `entity.CastSpell`, optionally at the current target; see [INSTANCE_PROVISIONAL_ENCOUNTERS_20261109.md](INSTANCE_PROVISIONAL_ENCOUNTERS_20261109.md).
* **Item 3 (interrupt versus a pending cast) verified by code and an existing test, not newly built:** `UnitEntity.AddCCState` cancels the entity's active casting spells with `CastResult.SpellInterrupted` when the state interrupts casting (`CrowdControlStateRules.InterruptsActiveCasting`: Interrupt and Knockdown), and the existing test `UnitEntityDamageResultTests.AddCCState_WhenStateInterruptsActiveCasting_CancelsActiveCastingSpells` covers it. A scripted cast goes through the same spell objects, so an interrupt cancels it. The provisional runner does not change its schedule after an interrupt (the next cast still fires on its interval); whether retail delayed the next cast is unknown.
* **Item 5 (wipe reset) partly done:** `OnLeaveCombat` resets the runner to the opening phase (tested). Restoring spawns, doors and platforms on a wipe is not implemented.
* **Item 4 (invulnerable-until-condition adds, shield removal) still open:** no provisional plan models adds or shields.
