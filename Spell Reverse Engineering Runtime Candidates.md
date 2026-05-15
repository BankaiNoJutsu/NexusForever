# Spell Reverse Engineering Runtime Candidates

Date: 2026-05-14

This is the runtime fixture layer for the global spell reverse-engineering loop. It turns the data join:

`WorldDatabase entity Creature` -> `Jabbithole creature_spells` -> `Jabbithole spells.game_id` -> `Spell4.ID` -> `Spell4Effects`

into concrete local server commands.

The helper importer creates `world_runtime_spell_candidates`, one row per placed creature spell. Each row includes placement counts, effect-family flags, a simple priority score, and generated commands:

- `/spell inspect4 <spell4_id>` for raw and decoded spell structure.
- `/spell cast4 <spell4_id>` for direct local execution of the concrete `Spell4` row.
- `/spell inspect <spell4_base_id> <spell4_tier>` and `/spell cast <spell4_base_id> <spell4_tier>` for the existing base/tier form.

## Regenerate

```powershell
python Tools\SpellReverseEngineering\import_world_creature_context.py --world-db I:\GIT\NexusForever.WorldDatabase --database nexus_spell_re --user bankai --password bankai
```

```powershell
& 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe' -ubankai -pbankai --batch --raw --database=nexus_spell_re -e "SELECT continent, zone, creature_id, LEFT(creature_description, 90) AS creature, spell4_id, spell4_base_id, spell4_tier, placements, placement_effect_rows, priority_score, effect_families, inspect_command, cast_command FROM world_runtime_spell_candidates WHERE entity_type = 0 ORDER BY priority_score DESC, placements DESC, placement_effect_rows DESC LIMIT 40;"
```

Current placed `NonPlayer` candidate coverage:

| Metric | Count |
| --- | ---: |
| Candidate creature-spell rows | 2,246 |
| Distinct placed creatures | 623 |
| Distinct concrete `Spell4` ids | 1,066 |
| Damage-family candidate rows | 2,239 |
| Proxy-family candidate rows | 291 |
| `UnitPropertyModifier` candidate rows | 52 |
| Rows with delay, tick, or duration data | 1,192 |

## First Fixture Set

These are not "special case" spells. They are representative placed creature spells with enough real content usage to make local behavior meaningful.

| Fixture | Creature context | Concrete spell | Why it matters | Commands |
| --- | --- | --- | --- | --- |
| Terminite Rumble | Wilderrun, creature `29885`, Pixpox Splorg, 127 placements | `Spell4=35234`, base `20935`, tier `1` | High-placement damage plus `UnitPropertyModifier`, forced move, NPC delay, and creature flag mutation. Good duration/removal test for `InterruptArmor_Threshold`. | `/spell inspect4 35234` then `/spell cast4 35234` |
| Pollinate | Algoroc, creature `16680`, Dominion Research Equipment, 9 placements | `Spell4=57625`, base `37192`, tier `1` | Compact multi-family case: forced move, damage, property modifier, proxy, NPC delay. Includes a persistence prerequisite on `Armor`. | `/spell inspect4 57625` then `/spell cast4 57625` |
| Wide Shot | Whitevale, creature `26453`, Doomtide Corruptordrone, 28 placements | `Spell4=56911`, base `36530`, tier `1` | Damage plus proxy plus 3500ms `MoveSpeedMultiplier`, useful for property modifier lifetime and proxy parent/root behavior. | `/spell inspect4 56911` then `/spell cast4 56911` |
| Annihilate Essence | Auroria, creature `26155`, Honeyhive Guardian, 71 placements | `Spell4=57353`, base `36941`, tier `1` | Damage plus plain proxy. Its damage and proxy rows have `tickTime=1000` without a duration, making it a good witness for the still-unknown tick-only semantics. | `/spell inspect4 57353` then `/spell cast4 57353` |
| Hivemind Trap | Auroria, creature `26155`, Honeyhive Guardian, 71 placements | `Spell4=57355`, base `36943`, tier `1` | Damage plus `CCStateSet` (`Tether`) and forced move. Useful for timed CC set/remove and active-state tracking. | `/spell inspect4 57355` then `/spell cast4 57355` |
| Medic Discharge Chain | Celestion, creature `31507`, Celestion Snap Trap, 65 placements | `Spell4=77996`, base `53869`, tier `1` | Multi-row proxy chain with targetFlags split between telegraph and caster, plus `SpellForceRemove` removing chained spell `77997`. Good proxy forwarding and force-remove fixture. | `/spell inspect4 77996` then `/spell cast4 77996` |
| Vulcan Slam | Auroria, creature `26189`, Biting Boulder, 46 placements | `Spell4=59523`, base `38768`, tier `1` | Damage plus repeated proxy rows targeting `6` (`Target|Telegraph`). Useful for duplicate effect ordering and proxy target forwarding. | `/spell inspect4 59523` then `/spell cast4 59523` |
| Arc Thrower | Auroria, creature `26189`, Biting Boulder, 46 placements | `Spell4=52796`, base `34609`, tier `1` | Damage plus proxy with `durationTime=500` to `Generic Snare 50%`. Good short-duration property/proxy interaction witness. | `/spell inspect4 52796` then `/spell cast4 52796` |

## Scheduler Fixtures

The central scheduler currently handles delayed effects and rows that have both `tickTime` and `durationTime`. Tick-only and duration-only rows are still tracked as separate unknowns.

| Fixture | Creature context | Concrete spell | Timing | Commands |
| --- | --- | --- | --- | --- |
| Nanobites DoT | Whitevale, creature `26127`, Calmwater Longsnout, 54 placements | `Spell4=44739`, base `28852`, tier `1` | Damage pulse: `delayTime=1000`, `tickTime=1000`, `durationTime=10000`. | `/spell inspect4 44739` then `/spell cast4 44739` |
| Burning DoT | Auroria, creature `26189`, Biting Boulder, 46 placements | `Spell4=46670`, base `30654`, tier `1` | Damage pulse: `delayTime=1000`, `tickTime=1000`, `durationTime=10000`. | `/spell inspect4 46670` then `/spell cast4 46670` |
| Bleed DoT | Auroria, creature `26189`, Biting Boulder, 46 placements | `Spell4=43759`, base `27920`, tier `1` | Damage pulse: `delayTime=2000`, `tickTime=2000`, `durationTime=6000`. | `/spell inspect4 43759` then `/spell cast4 43759` |

## Target Selection Fixtures

These fixtures validate central target selection: one entity should only appear once with merged `Caster`/`Target`/`Telegraph` flags, explicit targets should obey concrete `Spell4` range/facing gates, target/position AOEs should anchor away from the caster when context is supplied, and telegraph candidates should respect `Spell4AoeTargetConstraints` range, angle, target count, and smart-selection ordering.

| Fixture | Creature context | Concrete spell | Target evidence | Commands |
| --- | --- | --- | --- | --- |
| Ice Pound Trail | Whitevale, creature `23151`, Sunderstone Proximity Mine, 130 placements | `Spell4=38478`, base `22983`, tier `1` | AOE target count `10`, range `5`; telegraph damage plus CC/forced move. | `/spell inspect4 38478` then `/spell cast4 38478` |
| Terminite Rumble | Wilderrun, creature `29885`, Pixpox Splorg, 127 placements | `Spell4=35234`, base `20935`, tier `1` | AOE target count `10`, range `60`; damage plus property modifier and forced move. | `/spell inspect4 35234` then `/spell cast4 35234` |
| Medic Discharge Chain | Celestion, creature `31507`, Celestion Snap Trap, 65 placements | `Spell4=77996`, base `53869`, tier `1` | AOE target count `5`, range `15`; split target flags across telegraph and caster proxy rows. | `/spell inspect4 77996` then `/spell cast4 77996` |
| Slasher Dash Proxy | Algoroc, creature `13120`, Loftite Crystal, 56 placements | `Spell4=30999`, base `17386`, tier `1` | AOE target count `10`, range `1`; compact dash/knockdown telegraph. | `/spell inspect4 30999` then `/spell cast4 30999` |
| Vulcan Slam | Auroria, creature `26189`, Biting Boulder, 46 placements | `Spell4=59523`, base `38768`, tier `1` | AOE target count `10`, range `25`; repeated proxy rows with `Target|Telegraph` target flags. | `/spell inspect4 59523` then `/spell cast4 59523` |
| Target AOE Test | Global spell data | `Spell4=912`, base `912`, tier `1` | Client test row for `targetType=3` target AOE, target count `10`, range `7`, angle `360`; validates primary-target anchoring. | Select a nearby unit, `/spell inspect4 912`, then `/spell cast4 912` |
| Position AOE Test | Global spell data | `Spell4=915`, base `915`, tier `1` | Client test row for `targetType=4` position AOE, target count `10`, range `7`, angle `360`; item/position-driven casts should anchor on supplied `SpellParameters.Position`. | `/spell inspect4 915`; validate through an item or script path that supplies position |
| Lowest Health AE Test | Global spell data | `Spell4=27181`, base `13599`, tier `1` | Named witness for `targetSelection=4`: "lowest absolute health in a 30 yd AE". | `/spell inspect4 27181` then `/spell cast4 27181` |
| Missing Health AE Test | Global spell data | `Spell4=27182`, base `13600`, tier `1` | Named witness for `targetSelection=5`: "unit missing the most health in a 30 yd AE". | `/spell inspect4 27182` then `/spell cast4 27182` |

## Unresolved TargetMechanic Fixtures

These rows cover client target-mechanic families that still map to unresolved server labels (`type 0`, `type 6`, `type 7`). Treat them as evidence-only inspect and cast fixtures for follow-up tracing, not as justification to hardcode new targeting behavior.

| Target type | Known mechanic ids | Concrete spell | Why it matters | Commands |
| --- | --- | --- | --- | --- |
| Type `0` | `1`, `2`, `44` | `Spell4=305`, base `305`, tier `1` | `Q388` anti-tank mine explosion is a simple concrete witness for the unresolved client `type 0` branch and remains a candidate for deeper `IsSelfSpell` tracing. | `/spell inspect4 305` then `/spell cast4 305` |
| Type `6` | `17` | `Spell4=5157`, base `4526`, tier `1` | Consumable buff row mapped end-to-end to mechanic `17`; currently the cleanest inspect-first witness for unresolved client `type 6`, even though practical casting still depends on item context. | `/spell inspect4 5157`; cast through the matching item or consumable path if available |
| Type `7` | `18`, `25`, `26`, `33`, `52`, `58`, `63`, `69` | `Spell4=339`, base `339`, tier `1` | Auto-attack witness for mechanic `25`; useful for the client service-lookup branch currently surfaced as unresolved `type 7`. | `/spell inspect4 339` then `/spell cast4 339` |
| Type `7` | `18`, `25`, `26`, `33`, `52`, `58`, `63`, `69` | `Spell4=26813`, base `13307`, tier `1` | Client test AE witness for mechanic `18`; complements the auto-attack row with a non-basic-attack shape under the same unresolved branch. | `/spell inspect4 26813` then `/spell cast4 26813` |

## EMM-Bearing Innate Fixtures

These rows confirm that non-zero EMM fields are live data on both `Spell4` and `Spell4Effects`. Treat them as inspect-first fixtures for pattern comparison, not as proof of EMM semantics.

| Fixture | Concrete spell | EMM evidence | Commands |
| --- | --- | --- | --- |
| Esper Mind Cleanse Finisher | `Spell4=284`, base `284`, tier `1` | `InnateCostEMMId1=4`; baseline effect `301` has zero EMM, then heal rows `2555`-`2558` keep `emmComparison=0` while `emmValue` steps `1` -> `4`. | `/spell inspect4 284` then `/spell cast4 284` |
| Esper Mind Stab Finisher | `Spell4=405`, base `405`, tier `1` | `InnateCostEMMId0=4`; baseline effect `505` has zero EMM, then damage rows `2549`-`2552` keep `emmComparison=0` while `emmValue` steps `1` -> `4`. | `/spell inspect4 405` then `/spell cast4 405` |
| Esper Healing Nova Finisher | `Spell4=1691`, base `1691`, tier `1` | `InnateCostEMMId1=4`; baseline effect `2568` has zero EMM, then rows `2569`-`2572` step `emmValue` `1` -> `4` and the final row also flips `emmComparison` to `1`. | `/spell inspect4 1691` then `/spell cast4 1691` |
| Medic Cell Shock | `Spell4=1798`, base `1798`, tier `1` | `InnateCostEMMId0=7`; baseline effect `2783` has zero EMM, then rows `2816`-`2819` step `emmValue` `1` -> `4` and the final row also flips `emmComparison` to `1`. | `/spell inspect4 1798` then `/spell cast4 1798` |
| Medic Guillotine Charge | `Spell4=1851`, base `1851`, tier `1` | `InnateCostEMMId0=5`; baseline effect `2877` has zero EMM, then rows `2878`-`2881` step `emmValue` `1` -> `4` and the final row also flips `emmComparison` to `1`. | `/spell inspect4 1851` then `/spell cast4 1851` |

## Proxy Variant Fixtures

The placed `NonPlayer` slice is dominated by plain `Proxy`, but global data gives clean non-random proxy variant fixtures. These validate that variant rows decode `DataBits00`, emit `proxy` diagnostics, preserve parent/root spell context, forward the realized proxy target to child `Target` rows, and route delayed or duration-bound pulses through the shared scheduler.

| Fixture | Context | Concrete spell | Proxy evidence | Commands |
| --- | --- | --- | --- | --- |
| Stemdragon Telegraph Base | Global spell data | `Spell4=27279`, base `13767`, tier `1` | `ProxyLinearAE` rows chain to `27290` across several delayed rows. | `/spell inspect4 27279` then `/spell cast4 27279` |
| Proxy Channel Test | Global spell data | `Spell4=28307`, base `14789`, tier `1` | `ProxyChannel` chains to `28308`. | `/spell inspect4 28307` then `/spell cast4 28307` |
| Satellite Targeting Base | Global spell data | `Spell4=30147`, base `16571`, tier `1` | `ProxyChannel` chains to `29889` with `tickTime=1000`, `durationTime=5000`. | `/spell inspect4 30147` then `/spell cast4 30147` |
| Spikehorde Progressive Telegraph | Global spell data | `Spell4=28360`, base `14842`, tier `1` | `ProxyChannelVariableTime` chains to `28270`, `durationTime=3700`. | `/spell inspect4 28360` then `/spell cast4 28360` |

## Heal Fixtures

Placed `NonPlayer` context only exposes a small heal surface, but the global table contains many HoTs. These are useful for validating `healing-output`, `CombatLogHeal`, and positive health application.

| Fixture | Creature context | Concrete spell | Notes | Commands |
| --- | --- | --- | --- | --- |
| Life Force Aura | Whitevale, creature `26019`, Nimble Steamglider, 9 placements | `Spell4=74368`, base `50723`, tier `1` | Large direct placed heal row. | `/spell inspect4 74368` then `/spell cast4 74368` |
| Environmental Heal | Whitevale, creature `23346`, Hungry Thermock Citizen, 1 placement | `Spell4=32495`, base `18716`, tier `1` | Placed heal row with base value. | `/spell inspect4 32495` then `/spell cast4 32495` |
| Warm and Fuzzy | Global spell data | `Spell4=41389`, base `25586`, tier `1` | HoT-style heal with `tickTime=1000`, `durationTime=60000`. | `/spell inspect4 41389` then `/spell cast4 41389` |

## Vital Modifier Fixtures

`VitalModifier` rows mutate health, shields, focus, and class resources directly. The current runtime intentionally supports only positive flat restores and max-percent restores for supported vitals; sentinel, parameter-vector, and drain-like rows should produce `vital-modifier` diagnostics without mutating state.

| Fixture | Context | Concrete spell | Vital evidence | Commands |
| --- | --- | --- | --- | --- |
| Health Dispenser Refill | Global battleground/object spell data | `Spell4=26549`, base `13043`, tier `1` | `Health`, large flat-full payload with `DataBits05=1.0f`; should clamp to max health. | `/spell inspect4 26549` then `/spell cast4 26549` |
| Shield Restore Combo | Global item/utility spell data | `Spell4=31982`, base `18236`, tier `1` | Shield restore row with flat amount alongside instant heal text. | `/spell inspect4 31982` then `/spell cast4 31982` |
| System Restoration Full Heal | Global holdout buff data | `Spell4=34473`, base `20443`, tier `1` | `ShieldCapacity`, `DataBits05=100.0f`; max-percent restore path. | `/spell inspect4 34473` then `/spell cast4 34473` |
| Ten Percent Heal | Global utility spell data | `Spell4=45398`, base `29440`, tier `1` | Percent-style shield/HP/resource restore label with `DataBits05=10.0f`. | `/spell inspect4 45398` then `/spell cast4 45398` |
| Champion Healed | Global public-event spell data | `Spell4=74662`, base `51004`, tier `1` | `Health`, small flat amount `15`; validates non-full flat restore. | `/spell inspect4 74662` then `/spell cast4 74662` |
| Health Drain Diagnostic | Global spell data | `Spell4=41137`, base `25340`, tier `1` | `Health`, negative/ambiguous ticking payload; should trace skipped reason rather than apply. | `/spell inspect4 41137` then `/spell cast4 41137` |

## Shield Fixtures

`HealShields` and `DamageShields` share the damage/heal formula shape but mutate shield capacity instead of health. These validate `shield-healing-output`, `shield-damage-output`, `CombatLogHeal`, `CombatLogDamageShield`, and shield clamping.

| Fixture | Context | Concrete spell | Shield evidence | Commands |
| --- | --- | --- | --- | --- |
| Bolstering Strike Shield Restore | Global Warrior spell | `Spell4=32346`, base `18573`, tier `1` | `HealShields`, target flags caster, Assault/Support-style parameters plus base term. | `/spell inspect4 32346` then `/spell cast4 32346` |
| Medishot Shield Restore | Global consumable | `Spell4=35014`, base `20841`, tier `1` | `HealShields`, item-budget style payload; pairs with health heal text. | `/spell inspect4 35014` then `/spell cast4 35014` |
| Shield Heal Overtime Pot | Global PVP consumable | `Spell4=53249`, base `34949`, tier `1` | `HealShields`, `tickTime=2000`, `durationTime=6000`. | `/spell inspect4 53249` then `/spell cast4 53249` |
| Field Commander Defense Grid | Global rune set spell | `Spell4=82746`, base `58121`, tier `1` | `HealShields`, `tickTime=1000`, `durationTime=12000`, target max shield coefficient. | `/spell inspect4 82746` then `/spell cast4 82746` |
| Supercharged Shield Damage | Global rune special spell | `Spell4=82315`, base `57749`, tier `1` | `DamageShields`, item budget parameter. | `/spell inspect4 82315` then `/spell cast4 82315` |
| Energized Arms Shield Drain | Global rune set spell | `Spell4=82710`, base `58094`, tier `1` | `DamageShields`, caster max health coefficient; validates direct shield damage. | `/spell inspect4 82710` then `/spell cast4 82710` |

## Transference Fixtures

`Transference` rows are life/energy drain effects. The current runtime damages the target through the shared damage path, restores the caster's decoded vital by transfer rate, and emits `CombatLogTransference`.

| Fixture | Context | Concrete spell | Transference evidence | Commands |
| --- | --- | --- | --- | --- |
| Generic Life Drain DoT | Global spell data | `Spell4=37818`, base `22613`, tier `1` | `Health` transfer with `tickTime=1001`, `durationTime=5000`, and parameter-driven damage. | `/spell inspect4 37818` then `/spell cast4 37818` |
| Hookfoot Life Leech | Global telegraph spell data | `Spell4=39581`, base `24125`, tier `1` | Transfer-rate `0.5` row, useful for validating heal amount versus adjusted damage. | `/spell inspect4 39581` then `/spell cast4 39581` |
| Transfer Rate Tests | Global test rows | `Spell4=5949` and `5950` | Explicit `10x` and `0.5x` transfer-rate names with base-only damage payloads. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Shield-Capacity Drain | Global neutralizer rows | Use query 8 for `Transference` with `DataBits00=3` | Validates shield-capacity transfer behavior and unsupported/overheal diagnostics. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |

## Absorption Fixtures

`Absorption` creates a temporary absorb pool and consumes incoming damage before normal shield absorption. These validate `absorption-output`, `absorption`, `CombatLogAbsorption`, damage-description `AbsorbedAmount`, and duration cleanup.

| Fixture | Context | Concrete spell | Absorption evidence | Commands |
| --- | --- | --- | --- | --- |
| Potion of Protection | Global deprecated consumable | `Spell4=25155`, base `11746`, tier `1` | Float base absorb payload, `durationTime=60000`, general category `7`. | `/spell inspect4 25155` then `/spell cast4 25155` |
| Woodhaven's Protection | Global quest reward spell | `Spell4=46678`, base `30662`, tier `1` | Large long-duration absorb, `durationTime=900000`. | `/spell inspect4 46678` then `/spell cast4 46678` |
| Ionis Cyst Stage 1 | Global zone-boss spell | `Spell4=74797`, base `51134`, tier `1` | Absorption plus NPC execution delay, `durationTime=600000`, coefficient parameters. | `/spell inspect4 74797` then `/spell cast4 74797` |
| Swarm Buffer Shield | Global spider boss proxy | `Spell4=77476`, base `53406`, tier `1` | Parameter-driven absorb, `durationTime=60000`. | `/spell inspect4 77476` then `/spell cast4 77476` |
| Icy Sheen | Global zone-boss spell | `Spell4=81579`, base `57091`, tier `1` | Absorb plus interrupt armor fixture, useful for stacked defensive state validation. | `/spell inspect4 81579` then `/spell cast4 81579` |
| Absorption Shield All | Early shield spell | `Spell4=8820`, base `7814`, tier `1` | General all-purpose category row, `durationTime=15000`. | `/spell inspect4 8820` then `/spell cast4 8820` |

## Healing Absorption Fixtures

`HealingAbsorption` creates a temporary pool that absorbs incoming health healing before health changes. These validate `healing-absorption-output`, `healing-absorption`, `CombatLogHealingAbsorption`, `CombatLogHeal.Absorption`, health-heal consumption, and duration cleanup.

| Fixture | Context | Concrete spell | Healing absorption evidence | Commands |
| --- | --- | --- | --- | --- |
| Heavy Anti-Heal | Global spell data | `Spell4=75608`, base `51858`, tier `1` | Base amount `200000`, mode `1`, `durationTime=10000`. | `/spell inspect4 75608` then `/spell cast4 75608` |
| Medium Anti-Heal | Global spell data | `Spell4=87264`, base `62363`, tier `1` | Base amount `100000`, mode `1`, `durationTime=60000`. | `/spell inspect4 87264` then `/spell cast4 87264` |
| Timed Anti-Heal | Global spell data | `Spell4=87567`, base `62641`, tier `1` | Base amount `50000`, mode `1`, `durationTime=30000`. | `/spell inspect4 87567` then `/spell cast4 87567` |
| Parameter Anti-Heal | Global spell data | `Spell4=87792`, base `62863`, tier `1` | Parameter-driven row with `ParameterType00=23`, `ParameterValue00=1`, `durationTime=30000`. | `/spell inspect4 87792` then `/spell cast4 87792` |
| Small Anti-Heal | Global spell data | `Spell4=87867`, base `62929`, tier `1` | Base amount `10000`, mode `1`, `durationTime=10000`. | `/spell inspect4 87867` then `/spell cast4 87867` |

## Personal Damage And Heal Modifier Fixtures

`PersonalDmgHealMod` maps compact modifier codes onto existing damage/heal multiplier properties. These validate `personal-dmg-heal-mod`, `effect-lifetime`, outgoing damage multipliers, target damage-taken multipliers, and healing incoming/outgoing multipliers.

| Fixture | Context | Concrete spell | Modifier evidence | Commands |
| --- | --- | --- | --- | --- |
| Analyze Weakness | Global Stalker spell | `Spell4=60738`, base `42471`, tier `1` | Incoming physical/tech/magic damage multipliers for 8000 ms, scaling by tier up to `60748`. | `/spell inspect4 60738` then `/spell cast4 60738` |
| Nano Spike | Global Stalker spell | `Spell4=88239`, base `63400`, tier `1` | Mixes outgoing and incoming physical/tech/magic multiplier rows for 4000 ms. | `/spell inspect4 88239` then `/spell cast4 88239` |
| Unsteady Miasma | Global Engineer aura | `Spell4=41491`, base `25698`, tier `1` | Outgoing physical/tech/magic damage modifier rows with zero duration on an aura shell. | `/spell inspect4 41491` then `/spell cast4 41491` |
| Sigil of Recovery | Global Spellslinger heal aura | `Spell4=88130`, base `63305`, tier `1` | Incoming healing multiplier for 3000 ms. | `/spell inspect4 88130` then `/spell cast4 88130` |
| Fusion Probe | Global Medic aura | `Spell4=88057`, base `63249`, tier `1` | Incoming healing multiplier rows, useful for packet/log validation around healing. | `/spell inspect4 88057` then `/spell cast4 88057` |

## Interrupt Armor Fixtures

`ModifyInterruptArmor` applies a decoded interrupt armor amount and removes duration-backed rows after `durationTime`. These validate `modify-interrupt-armor`, `effect-lifetime`, `CombatLogModifyInterruptArmor`, and whether temporary armor cleanup matches client behavior.

| Fixture | Context | Concrete spell | Interrupt armor evidence | Commands |
| --- | --- | --- | --- | --- |
| Titan's Strength IA Proxy | Global CC/interrupt armor spell | `Spell4=43457`, base `27627`, tier `1` | Amount `1`, `DataBits01=0`, `durationTime=20000`. | `/spell inspect4 43457` then `/spell cast4 43457` |
| Reinforced IA Proxy | Global CC/interrupt armor spell | `Spell4=55775`, base `35987`, tier `1` | Amount `1`, `DataBits01=0`, `durationTime=15000`. | `/spell inspect4 55775` then `/spell cast4 55775` |
| Modify +2 Remove | Global test spell | `Spell4=42697`, base `26886`, tier `1` | Amount `2`, `DataBits01=1`, paired with no-remove variant; validates remaining consume/remove-on-interrupt hypothesis. | `/spell inspect4 42697` then `/spell cast4 42697` |
| Modify +2 No Remove | Global test spell | `Spell4=42698`, base `26887`, tier `1` | Amount `2`, `DataBits01=0`, same duration as remove variant. | `/spell inspect4 42698` then `/spell cast4 42698` |
| Carbonated Destruction | Global vending-machine spell | `Spell4=79320`, base `55030`, tier `1` | High amount `35`, `durationTime=10000`; validates additive/clamping assumption. | `/spell inspect4 79320` then `/spell cast4 79320` |
| Icy Sheen | Global item/special spell | `Spell4=81579`, base `57091`, tier `1` | Amount `12`, `durationTime=3000`; useful high-value duration fixture. | `/spell inspect4 81579` then `/spell cast4 81579` |

## Threat Fixtures

`ThreatModification` mutates local threat lists for add, reduce, clear, set, and fixate-like modes. `ThreatTransfer` is decoded and traced only until party/raid transfer semantics are proven.

| Fixture | Context | Concrete spell | Threat evidence | Commands |
| --- | --- | --- | --- | --- |
| Tactical Retreat Drop Threat | Global Stalker spell | `Spell4=34298`, base `20268`, tier `1` | `ThreatModification`, mode `2`, drop/clear naming. | `/spell inspect4 34298` then `/spell cast4 34298` |
| Medic Calm | Global Medic spell | `Spell4=41603`, base `25799`, tier `1` | Mode `0`, ratio-like `DataBits01=0.75`; validates reduce/add ambiguity diagnostics. | `/spell inspect4 41603` then `/spell cast4 41603` |
| Generic Threat Wipe | Global utility spell | `Spell4=52825`, base `34629`, tier `1` | Mode `6`, explicit wipe naming. | `/spell inspect4 52825` then `/spell cast4 52825` |
| Set Raid Threat To 1 | Global raid utility | `Spell4=53057`, base `34833`, tier `1` | Mode `4`, `DataBits03=1`, target flags telegraph. | `/spell inspect4 53057` then `/spell cast4 53057` |
| Avatus Add Hate | Global Datascape spell | `Spell4=69974`, base `47050`, tier `1` | Mode `4`, add-hate name and low absolute value. | `/spell inspect4 69974` then `/spell cast4 69974` |
| Engineer Code Red | Global Engineer spell | `Spell4=41602`, base `25798`, tier `1` | `ThreatTransfer`, ratio-like payload and duration. | `/spell inspect4 41602` then `/spell cast4 41602` |

## Dispel Fixtures

`SpellDispel` removes locally tracked aura-like state whose source spell class matches the dispel row. Apply a timed tracked buff/debuff first, then cast the cleanse against the same player/target and compare `dispel`, `force-remove`, remove-packet, and combat-log output.

| Fixture | Context | Concrete spell | Dispel evidence | Commands |
| --- | --- | --- | --- | --- |
| Medic Sterilize | Global Medic spell data | Use query 26 to pick a tier | Cleanses `DebuffDispellable` (`SpellClass=38`) with count-like fields. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Spellslinger Purify | Global Spellslinger spell data | Use query 26 to pick a tier | Cleanses dispellable debuffs; good count/priority witness. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Warrior Expulsion | Global Warrior spell data | Use query 26 to pick a tier | Purge-style row for dispellable buffs (`SpellClass=36`) in several variants. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| PvP Debuff Cleanse | Global PvP spell data | Use query 26 to pick a tier | Includes rows that mention non-dispellable debuff class `39`; validates special cleanse behavior. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |

## Cooldown And Charge Fixtures

`CooldownReset` has a small obvious surface: zero-payload rows reset all player cooldowns, and targeted rows resolve a concrete `Spell4` id. `ModifyAbilityCharges` targets concrete charged spells and currently treats mode `0` as add and mode `1` as set.

| Fixture | Context | Concrete spell | Cooldown/charge evidence | Commands |
| --- | --- | --- | --- | --- |
| Cooldown Reset All | Global cooldown reset rows | `Spell4=45410`, `54729`, `60957`, `71912`, `76329`, `79324`, `79325`, or `81632` | Zero-payload `CooldownReset`; validates `ResetAllSpellCooldowns`. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Targeted Cooldown Reset | Global cooldown reset row | `Spell4=72378`, target `72379` | `CooldownReset.DataBits01` resolves to concrete spell id `72379`. | `/spell inspect4 72378` then `/spell cast4 72378` |
| Charge Modifier | Global charge rows | `Spell4=54652`, `58941`, `46867`, `87364`, `87399`, or `87353` | `ModifyAbilityCharges.DataBits00` points at target charged spell, `DataBits01` count, `DataBits02` mode. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Modify Spell Cooldown | Global cooldown modify rows | Use query 27 | Concrete rows resolve `DataBits01` to `Spell4`; negative milliseconds reduce, positive milliseconds set, and ambiguous group/ratio modes should trace skipped. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Activate Spell Cooldown | Global cooldown activate row | `Spell4` from query 27, target `63410` | One-row family that starts the target spell's configured cooldown. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |

## Action Bar Fixtures

`ActionBarSet` rows show temporary quest, vehicle, carrying-object, housing, and public-event bars. The current handler sends the decoded `ActionBarShortcutSet` through `ServerShowActionBar` as a floating spell bar.

| Fixture | Context | Concrete spell | Action-bar evidence | Commands |
| --- | --- | --- | --- | --- |
| Turret Action Bar | Global PvZ/test spell | `Spell4=2943`, base `2943`, tier `1` | `ActionBarShortcutSetId=2`, `durationTime=20000`; useful temporary-bar duration witness. | `/spell inspect4 2943` then `/spell cast4 2943` |
| Rift Sealer Action Bar | Global quest spell | `Spell4=5348`, base `4654`, tier `1` | Long-duration action bar `20` for quest interaction. | `/spell inspect4 5348` then `/spell cast4 5348` |
| Voreth Hammer Action Bar | Global public event spell | `Spell4=34486`, base `20456`, tier `1` | Public-event hammer bar with `durationTime=300000`. | `/spell inspect4 34486` then `/spell cast4 34486` |
| Carrying Object Bar | Global carrying rows | `Spell4=28101`, `28167`, `28390`, or query 28 | `DataBits02=1` carrying-object rows; validates associated unit and secondary payload diagnostics. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |

## Immunity, Scale, Faction, Visual, And Utility Fixtures

These validate the small effect families added after the core combat/state shell. Use query 28 in `Tools/SpellReverseEngineering/world_context_queries.sql` to pick concrete rows.

| Fixture | Context | Concrete spell | Evidence | Commands |
| --- | --- | --- | --- | --- |
| Effect-Type Immunity | Global `SpellEffectImmunity` rows | Use query 28 | `DataBits00` names the blocked `SpellEffectType`; validate dropped effects, `CombatLogImmune`, and duration cleanup. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Concrete Spell Immunity | Global `SpellImmunity` mode `0` rows | `Spell4=46559`, `46927`, `48020`, or `31074` | `DataBits01` names a concrete blocked `Spell4`; validate `spell-immunity`, `spell-immune-blocked`, `CombatLogImmune`, force-remove/dispel cleanup, and duration cleanup. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Scale Transition | Global or placed scale rows | `Spell4=44691`, `49869`, `72072`, or query 28 | `DataBits00` float target scale, `DataBits01/02` transition timings, duration-backed restore. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Faction Change | Global `FactionSet` rows | Use query 28 | `DataBits00` raw Faction2 id; validate `ServerEntityFaction` and duration restore. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Item Visual Swap | Global visual rows | Use query 28 | `DataBits00` raw client visual slot, `DataBits01` display id, `DataBits04/05` colour/dye data; immediate rows should update visuals and duration rows should trace the restore-tracking gap. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Disguise Outfit | Global disguise/equipment rows | `Spell4=29249`, `31349`, `30834`, `32823`, `47860`, `51513`, or query 28 | `DataBits00` outfit id, `DataBits01` primary item display, `DataBits02` secondary display when greater than `1`; validate `OutfitInfo`, visible item slots, timed restoration, and `disguise-outfit` diagnostics. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Mimic Disguise | Global clone/hologram rows | `Spell4=38974`, `41818`, `41913`, `46271`, `69525`, `69609`, or `71074` | Target copies caster display, outfit, and item visuals; validate `mimic-disguise`, target-only visual removal, force-remove/dispel cleanup, and duration restoration. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Disembark | Global vehicle/eject rows | `Spell4=1527`, `77194`, `80044`, or query 28 | `DataBits00` mode with zero secondary payload; validate mounted player dismount, passenger removal packets, and `CombatLogMount`. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Quest Objective Advance | Global quest utility rows | Use query 28 | `DataBits00` objective id and `DataBits02` progress through `QuestManager.ObjectiveUpdate`. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |
| Reward Utility | Global utility rows | `AchievementAdvance`, `ReputationModify`, `GiveItemToPlayer`, `GiveSchematic`, `RewardPropertyModifier`, `AddSpell=60333/60338/70866/70867`, `GrantXP=31527`, and `Kill` rows from query 28 | Validates achievement grants, reputation deltas, inventory item creation, schematic unlock packet/objective updates, duration-backed reward-property modifiers, spellbook teaching, flat XP grants, and normal death-state routing. | `/spell inspect4 <spell4>` then `/spell cast4 <spell4>` |

## Summon Creature Fixtures

`SummonCreature` rows create world entities from `Creature2` ids. The current runtime validates the creature id, creates an `INonPlayerEntity`, applies conservative radius/angle placement from `DataBits03..05`, and removes duration-backed summons through the spell event queue.

| Fixture | Context | Concrete spell | Summon evidence | Commands |
| --- | --- | --- | --- | --- |
| Mobile Crafting Station | Global tradeskill utility spell | `Spell4=42049`, base `26243`, tier `1` | Summons creature `21793` after 1000 ms for 3600000 ms; radius data `5/5`. | `/spell inspect4 42049` then `/spell cast4 42049` |
| Assault Power Tech-Totem | Global settler utility spell | `Spell4=31561`, base `17893`, tier `1` | Summons creature `25459` for 600000 ms from caster target flags. | `/spell inspect4 31561` then `/spell cast4 31561` |
| Guild Bank Summon | Global guild perk spell | `Spell4=52770`, base `34583`, tier `1` | Summons creature `51698` for 600000 ms; long-lived service object. | `/spell inspect4 52770` then `/spell cast4 52770` |
| Mini Vendbot | Global settler utility spell | `Spell4=58640`, base `38054`, tier `1` | Summons creature `53735` for 300000 ms; non-zero `DataBits06=8663` remains payload evidence. | `/spell inspect4 58640` then `/spell cast4 58640` |
| PVP Turret Setup | Global PVP objective spell | `Spell4=87007`, base `62110`, tier `1` | Summons creature `75088` for 300000 ms; `DataBits08=87012` likely follow-up spell hook. | `/spell inspect4 87007` then `/spell cast4 87007` |
| Void Remnants Formation | Global encounter spell | `Spell4=66439`, base `44223`, tier `1` | Paired rows summon creatures `58479`/`58480` for 180000 ms with angle data `90/270`. | `/spell inspect4 66439` then `/spell cast4 66439` |
| Avatus Holo Cannons | Global warplot boss spell | `Spell4=70614`, base `47590`, tier `1` | Paired rows summon creature `60223` for 120000 ms with radius `15/15` and angle `90/270`. | `/spell inspect4 70614` then `/spell cast4 70614` |

## Summon Trap Fixtures

`SummonTrap` rows create duration-backed trap/probe entities from `Creature2` ids and preserve trigger spell, arm time, radius, and mode fields in diagnostics. Trigger firing and owner AI are still validation targets.

| Fixture | Context | Concrete spell | Trap evidence | Commands |
| --- | --- | --- | --- | --- |
| Stalker Proximity Mine | Global class/test spell | `Spell4=34094`, base `20068`, tier `1` | Creature `26850`, trigger `34095`, arm time `2000`, radius `3.0`, lifetime `120000`. | `/spell inspect4 34094` then `/spell cast4 34094` |
| Tether Mine | Global Stalker spell | `Spell4=38860`, base `23237`, tier `1` | Creature `26850`, trigger `38861`, radius-like `5.0`, lifetime `30000`. | `/spell inspect4 38860` then `/spell cast4 38860` |
| Medic Rejuvenator | Global Medic spell | `Spell4=40653`, base `24878`, tier `1` | Creature `37712`, trigger `40664`, repair/probe flags, lifetime `60000`. | `/spell inspect4 40653` then `/spell cast4 40653` |
| Engineer Personal Defense Unit | Global Engineer spell | `Spell4=44494`, base `28664`, tier `1` | Creature `45493`, trigger `44521`, radius `2.0`, lifetime `30000`. | `/spell inspect4 44494` then `/spell cast4 44494` |
| Medic Health Probe | Global Medic spell | `Spell4=60886`, base `39177`, tier `1` | Creature `54549`, trigger `60891`, radius `2.0`, lifetime `15000`. | `/spell inspect4 60886` then `/spell cast4 60886` |

## Summon Vehicle Fixtures

`SummonVehicle` rows create `IVehicleEntity` instances from `Creature2` plus `UnitVehicle` ids. `DataBits02` is treated as an auto-board mode when non-zero.

| Fixture | Context | Concrete spell | Vehicle evidence | Commands |
| --- | --- | --- | --- | --- |
| Galeras Speeder | Global quest spell | `Spell4=29420`, base `15534`, tier `1` | Creature `22404`, vehicle `317`, auto-board mode `1`. | `/spell inspect4 29420` then `/spell cast4 29420` |
| Speeder Activate | Global quest activate spell | `Spell4=39310`, base `23905`, tier `1` | Same speeder payload as `29420`, validates activate/caster target context. | `/spell inspect4 39310` then `/spell cast4 39310` |
| Hycrest Vehicle Repair | Global adventure spell | `Spell4=49970`, base `33601`, tier `1` | Creature `48438`, vehicle `486`, auto-board mode `1`. | `/spell inspect4 49970` then `/spell cast4 49970` |
| Farside War Mech | Global raid-key spell | `Spell4=70155`, base `47169`, tier `1` | Paired vehicle rows for creatures `59595`/`59596`, vehicle `539`, no auto-board flag. | `/spell inspect4 70155` then `/spell cast4 70155` |
| LPI Mounted | Global mounted state spell | `Spell4=80238`, base `55717`, tier `1` | Creature `70527`, vehicle `581`, auto-board mode `1`. | `/spell inspect4 80238` then `/spell cast4 80238` |

## NPC Execution Delay Fixtures

`NpcExecutionDelay` is a timing-heavy family. Most rows have all-zero payload and a populated `durationTime`; the current runtime emits `npc-execution-delay` diagnostics and keeps the spell execution alive for duration-backed rows.

| Fixture | Context | Concrete spell | Delay evidence | Commands |
| --- | --- | --- | --- | --- |
| Ionis Cyst Stage 1 | Global zone-boss spell | `Spell4=74797`, base `51134`, tier `1` | All-zero payload, `durationTime=600000`; long encounter hold. | `/spell inspect4 74797` then `/spell cast4 74797` |
| Woeful Chains Boss Example | Global test/boss spell | `Spell4=32229`, base `18460`, tier `1` | All-zero payload, `delayTime=600`, `durationTime=15000`. | `/spell inspect4 32229` then `/spell cast4 32229` |
| Forgemaster Trogun Shout | Global dungeon telegraph spell | `Spell4=40700`, base `24912`, tier `1` | Non-zero payload `9/3/100/250/0/1`, `delayTime=500`, `durationTime=10000`. | `/spell inspect4 40700` then `/spell cast4 40700` |
| Rolling Rampage Base | Global prototype telegraph spell | `Spell4=28003`, base `14487`, tier `1` | All-zero payload, `durationTime=7500`; short telegraph hold. | `/spell inspect4 28003` then `/spell cast4 28003` |
| Blind Rotating Cones | Global encounter spell | `Spell4=47593`, base `31524`, tier `1` | Non-zero float-like payload fields, `durationTime=7000`. | `/spell inspect4 47593` then `/spell cast4 47593` |

## CC Fixtures

`CCStateSet.DataBits00` maps to the server `CCState` enum. These placed cases validate `CombatLogCCState`, `ServerEntityCCStateSet`, timed active-state tracking, caster CC-condition blocking, and `ServerEntityCCStateRemove`.

| Fixture | Creature context | Concrete spell | CC state and timing | Commands |
| --- | --- | --- | --- | --- |
| Ice Pound Trail | Whitevale, creature `23151`, Sunderstone Proximity Mine, 130 placements | `Spell4=38478`, base `22983`, tier `1` | `Knockdown`, `durationTime=2000`, targetFlags `4`. | `/spell inspect4 38478` then `/spell cast4 38478` |
| Hivemind Trap | Auroria, creature `26155`, Honeyhive Guardian, 71 placements | `Spell4=57355`, base `36943`, tier `1` | `Tether`, `durationTime=15000`, targetFlags `2`. | `/spell inspect4 57355` then `/spell cast4 57355` |
| Blinding Dust | Algoroc, creature `19193`, fungal spore, 40 placements | `Spell4=55862`, base `36009`, tier `1` | `Blind`, `durationTime=4500`, targetFlags `2`. | `/spell inspect4 55862` then `/spell cast4 55862` |
| Battlemaiden Attack | Celestion, creature `32523`, Torine Battlemaiden, 21 placements | `Spell4=48757`, base `32491`, tier `1` | `Stun`, `durationTime=3000`, targetFlags `2`. | `/spell inspect4 48757` then `/spell cast4 48757` |

## CC Break Fixtures

The current placed `NonPlayer` join does not expose direct `CCStateBreak` rows. Use global/player spells for this family, especially after applying a timed CC fixture first. These validate `cc-state-break` diagnostics, tracked active-state removal, duplicate timed-remove suppression, `ServerEntityCCStateRemove`, and `CombatLogCCStateBreak`.

| Fixture | Context | Concrete spell | Break evidence | Commands |
| --- | --- | --- | --- | --- |
| Unstoppable Force | Global/player spell data | `Spell4=30568`, tier `1` | `DataBits00=28372991`, broad break mask. | `/spell inspect4 30568` then `/spell cast4 30568` |
| Calm | Global/player spell data | `Spell4=41603`, tier `1` | `DataBits00=28371455`, broad break mask. | `/spell inspect4 41603` then `/spell cast4 41603` |
| Extricate | Global/player spell data | `Spell4=42241`, tier `1` | `DataBits00=33548677`, targetFlags `4`, ally-style break mask. | `/spell inspect4 42241` then `/spell cast4 42241` |
| Void Slip | Global/player spell data | `Spell4=58428`, tier `1` | `DataBits00=28371455`, broad break mask. | `/spell inspect4 58428` then `/spell cast4 58428` |
| Break Free | Global/PvP gadget data | `Spell4=64955`, tier `1` | `DataBits00=29092799`, plus non-zero `DataBits01/02`. | `/spell inspect4 64955` then `/spell cast4 64955` |

## Force Remove Fixtures

`SpellForceRemove.DataBits00` is treated as the remove type and `DataBits01` as the remove target id. Type `2` rows most strongly match concrete `Spell4.ID`; type `3` rows most strongly match `Spell4Base.ID`. The runtime currently applies conservative cleanup for tracked spell states by concrete id for type `2` and base id for type `3`.

| Fixture | Creature context | Concrete spell | Remove evidence | Commands |
| --- | --- | --- | --- | --- |
| Medic Discharge Chain | Celestion, creature `31507`, Celestion Snap Trap, 65 placements | `Spell4=77996`, base `53869`, tier `1` | Remove type `2`, remove id `77997`, data `1/3/3/0/3`. | `/spell inspect4 77996` then `/spell cast4 77996` |
| Terminite Egg Cleanup | Deradune, creature `24388`, Terminite Egg, 18 placements | `Spell4=43894`, base `28052`, tier `1` | Remove type `2`, remove id `43896`, data `1/1/1/0/3`. | `/spell inspect4 43894` then `/spell cast4 43894` |
| Scout Drone Cleanup | Levian Bay, creature `26021`, Scout Drone, 14 placements | `Spell4=50212`, base `32940`, tier `1` | Remove type `2`, remove id `51350`, data `1/1/1/1/2`. | `/spell inspect4 50212` then `/spell cast4 50212` |
| Nimble Steamglider Cleanup | Whitevale, creature `26019`, Nimble Steamglider, 9 placements | `Spell4=73178`, base `49561`, tier `1` | Remove type `1`, remove id `292`, data `1/0/100/0/2`; base-vs-concrete semantics still open. | `/spell inspect4 73178` then `/spell cast4 73178` |
| Deactivated Freebot Cleanup | Auroria, creature `26281`, deactivated freebot, 6 placements | `Spell4=77531`, base `53460`, tier `1` | Remove type `3`, remove id `53340`, data `1/1/2/0/2`; validates the base-scope cleanup path. | `/spell inspect4 77531` then `/spell cast4 77531` |

## Activate Fixtures

`Activate` currently has no placed `NonPlayer` rows in the world context join, so use global quest/object spells and a controlled player plus activated entity target. These validate `activate` diagnostics and quest objective updates for `ActivateEntity`, `ActivateEntity2`, `ActivateTargetGroup`, and `ActivateTargetGroupChecklist`.

| Fixture | Creature context | Concrete spell | Activate evidence | Commands |
| --- | --- | --- | --- | --- |
| Unlocking Flux Ward | Global quest spell data | `Spell4=1903`, base `1903`, tier `1` | `DataBits00=655`; compact non-zero quest activation row. | `/spell inspect4 1903` then `/spell cast4 1903` |
| Throw Incendiary Bomb | Global quest spell data | `Spell4=2511`, base `2511`, tier `1` | Common non-zero payload `703/200/0/0/0/0`. | `/spell inspect4 2511` then `/spell cast4 2511` |
| Burying Heart | Global quest spell data | `Spell4=3817`, base `3503`, tier `1` | Less common payload `5692/0/1/3/3/0`. | `/spell inspect4 3817` then `/spell cast4 3817` |
| Halon Ring Activate | Global quest spell data | `Spell4=40785`, base `24992`, tier `1` | Simple mode-like payload `1/0/0/0/0/0`. | `/spell inspect4 40785` then `/spell cast4 40785` |
| Silo N22 Panel Activate | Global quest/object spell data | `Spell4=57122`, base `36739`, tier `1` | Common panel payload `5/0/0/0/0/0`; pair with `57123`. | `/spell inspect4 57122` then `/spell cast4 57122` |

## State Toggle Fixtures

These cover `Stealth`, `RemoveStealth`, and `AggroImmune`. They validate state tracking, `CombatLogStealth`, timed removal, and the conservative local attackability gate for aggro immunity.

| Fixture | Creature context | Concrete spell | State evidence | Commands |
| --- | --- | --- | --- | --- |
| Temporary Stealth Potion | Global spell data | `Spell4=35227`, base `20928`, tier `1` | `Stealth`, zero payload, `durationTime=15000`. | `/spell inspect4 35227` then `/spell cast4 35227` |
| Tactical Retreat | Global/player spell data | `Spell4=38913`, base `23282`, tier `1` | `Stealth`, payload `10/0/1/0/0/0`, `durationTime=2000`; tier rows scale to 6000ms. | `/spell inspect4 38913` then `/spell cast4 38913` |
| Revealed Cloak | Global spell data | `Spell4=28089`, base `14572`, tier `1` | `RemoveStealth`, zero payload, targetFlags `2`. | `/spell inspect4 28089` then `/spell cast4 28089` |
| Flashlight Stealth Search | Global spell data | `Spell4=29507`, base `15963`, tier `1` | `RemoveStealth`, payload `2/29517/0/1/2147483647/60`, targetFlags `4`. | `/spell inspect4 29507` then `/spell cast4 29507` |
| Generic Aggro Immune | Global spell data | `Spell4=28898`, base `15378`, tier `1` | `AggroImmune`, zero payload, targetFlags `2`, no duration. | `/spell inspect4 28898` then `/spell cast4 28898` |
| Halon Ring No Aggro | Global quest spell data | `Spell4=41528`, base `25724`, tier `1` | `AggroImmune`, zero payload, `durationTime=15000`. | `/spell inspect4 41528` then `/spell cast4 41528` |
| Immune to CC/Damage | Global spell data | `Spell4=54035`, base `35346`, tier `1` | `AggroImmune`, zero payload, `durationTime=6000`; likely paired with other immunity behavior. | `/spell inspect4 54035` then `/spell cast4 54035` |

## Despawn Fixtures

`DespawnUnit` is mostly driven by scheduling: 668 of 855 global rows have `delayTime > 0`, and 748 rows have zero payload. The runtime guards player targets and removes non-player world entities from the map when the scheduled effect fires.

| Fixture | Creature context | Concrete spell | Despawn evidence | Commands |
| --- | --- | --- | --- | --- |
| Grendelus Bomb Damage | Auroria, creature `27859`, XT-9 Probebot, 29 placements | `Spell4=63431`, base `41520`, tier `1` | `DespawnUnit`, targetFlags `1`, `delayTime=100`. | `/spell inspect4 63431` then `/spell cast4 63431` |
| Hut-Hut Self Destruct | Levian Bay, creature `28313`, Malfunctioning Scout Drone, 26 placements | `Spell4=72072`, base `48506`, tier `1` | `DespawnUnit`, targetFlags `1`, immediate row with non-zero `DataBits04=1.0f`. | `/spell inspect4 72072` then `/spell cast4 72072` |
| Life Force Detonation | Celestion, creature `30400`, Firestorm Pyroslinger, 24 placements | `Spell4=79366`, base `55074`, tier `1` | `DespawnUnit`, targetFlags `1`, `delayTime=1`. | `/spell inspect4 79366` then `/spell cast4 79366` |
| Metal Maw Detonation Bombs | Algoroc, creature `20795`, Chua, 21 placements | `Spell4=60614`, base `38955`, tier `1` | `DespawnUnit`, targetFlags `1`, `delayTime=500`. | `/spell inspect4 60614` then `/spell cast4 60614` |

## ForcedMove Fixtures

`ForcedMove.DataBits00` is the movement type id. The current handler emits `forced-move` diagnostics and applies a conservative velocity impulse from decoded magnitude fields, so these fixtures are meant to validate direction, magnitude, and timed reset rather than declare retail physics parity.

| Fixture | Creature context | Concrete spell | Movement evidence | Commands |
| --- | --- | --- | --- | --- |
| Ice Pound Trail | Whitevale, creature `23151`, Sunderstone Proximity Mine, 130 placements | `Spell4=38478`, base `22983`, tier `1` | Movement type `4`, `DataBits03=250`, `DataBits06=5.0`, targetFlags `4`; paired with `Knockdown`. | `/spell inspect4 38478` then `/spell cast4 38478` |
| Slasher Dash Proxy | Algoroc, creature `13120`, Loftite Crystal, 56 placements | `Spell4=30999`, base `17386`, tier `1` | Movement type `4`, `DataBits03=400`, `DataBits06=2.0`, targetFlags `4`; paired with `Knockdown`. | `/spell inspect4 30999` then `/spell cast4 30999` |
| Bruising Rush | Everstar Grove, creature `28092`, Livingroot Keeper, 26 placements | `Spell4=39867`, base `24099`, tier `1` | Movement type `3`, `DataBits01/02=15.0`, `DataBits03=360`, targetFlags `1`; self-rush style case. | `/spell inspect4 39867` then `/spell cast4 39867` |
| Hookshot | Auroria, creature `27465`, Blackheart Summoner, 16 placements | `Spell4=40494`, base `24711`, tier `1` | Movement type `9`, `DataBits03=266`, `DataBits05=1`, `DataBits06=0.5`, targetFlags `2`; pull-like direction witness. | `/spell inspect4 40494` then `/spell cast4 40494` |

## Delay Death Fixtures

`DelayDeath.DataBits01` resolves to a trigger `Spell4` for nearly every row, and `DataBits02` is now treated as the trigger delay in milliseconds. The current runtime consumes one active prevent-death state on fatal damage, leaves the target at 1 HP, emits `CombatLogDelayDeath`, and casts the trigger spell immediately or after that delay.

| Fixture | Context | Concrete spell | Delay-death evidence | Commands |
| --- | --- | --- | --- | --- |
| Warrior To the Pain | Global player spell | `Spell4=59176`, base `25335`, tier `1` | Trigger `41133`, extra cooldown-like `300000`, mode `0`; validates death-prevent plus heal trigger. | `/spell inspect4 59176` then `/spell cast4 59176` |
| Stalker Last Stand | Levian Bay adventure spell | `Spell4=76529`, base `52704`, tier `1` | Trigger `76530`, trigger delay `3000`, mode `0`. | `/spell inspect4 76529` then `/spell cast4 76529` |
| Engineer Unbreakable | Rune set spell | `Spell4=82737`, base `57960`, tier `2` | Trigger `84397`, trigger delay `3000`; validates trigger fluff/proc ordering. | `/spell inspect4 82737` then `/spell cast4 82737` |
| Flame of Faith | Wilderrun quest spell | `Spell4=46212`, base `30237`, tier `1` | Trigger `42084`, data payload `4992`; quest death-prevention fixture. | `/spell inspect4 46212` then `/spell cast4 46212` |
| Spectral Swarm Death Explosion | Esper pet spell | `Spell4=55690`, base `35974`, tier `9` | Mode `2`, trigger `55715`, trigger delay `500`, duration `10000`; validates mode-2 death-explosion behavior. | `/spell inspect4 55690` then `/spell cast4 55690` |

## Sap Vital Fixtures

`SapVital.DataBits00` maps to `Vital`, `DataBits03` is the current mode field, and clean `DataBits01/02` float payloads now apply as percent-of-max restore or drain amounts. The current runtime intentionally skips parameter-driven rows, secondary-payload rows, unsupported vitals, and high-scalar ambiguous payloads.

| Fixture | Context | Concrete spell | Sap evidence | Commands |
| --- | --- | --- | --- | --- |
| Level Up Full Restore | Global system spell | `Spell4=410`, base `410`, tier `1` | Health and resource rows with mode `1`, `DataFloat02=100`; validates full percent restore. | `/spell inspect4 410` then `/spell cast4 410` |
| Northwatch Medkit | Global deprecated quest/item spell | `Spell4=1274`, base `1274`, tier `1` | Health mode `1`, `DataFloat02=25`; validates positive restore. | `/spell inspect4 1274` then `/spell cast4 1274` |
| Restore HP Over Time | Global deprecated test spell | `Spell4=1887`, base `1887`, tier `1` | Health mode `1`, `DataFloat02=8`; validates tick scheduling plus percent restore. | `/spell inspect4 1887` then `/spell cast4 1887` |
| 25 Percent Health Damage | Global test spell | `Spell4=4066`, base `3737`, tier `1` | Health mode `0`, `DataFloat02=25`; validates percent drain/damage. | `/spell inspect4 4066` then `/spell cast4 4066` |
| Exploding Barrel Sap | Global dungeon test spell | `Spell4=16056`, base `8452`, tier `1` | Health mode `2`, `DataFloat01=0.5`, delay `250`; validates ratio payload drain. | `/spell inspect4 16056` then `/spell cast4 16056` |
| Self Destruct Damage | Global test spell | `Spell4=2404`, base `2404`, tier `1` | Health mode `0`, `DataFloat02=100`; validates full-health drain on self-target style rows. | `/spell inspect4 2404` then `/spell cast4 2404` |
| Debilitating Barrage | Global encounter spell | `Spell4=41847`, base `26042`, tier `1` | Resource rows for `Resource7` and `Resource0`; validates unsupported-vital diagnostics plus supported resource drain when max data exists. | `/spell inspect4 41847` then `/spell cast4 41847` |
| Aquatic Buffer Skipped Row | Placed creature context, spell appears on several world creatures | `Spell4=71977`, base `48416`, tier `1` | Breath vital with negative payload; validates unsupported-vital/diagnostic behavior. | `/spell inspect4 71977` then `/spell cast4 71977` |

## Clamp Vital Fixtures

`ClampVital.DataBits02` is a float-bitcast health ceiling ratio. The current runtime stores effect-id clamps, caps current health to `ceil(MaxHealth * ratio)` with a minimum of 1, reapplies caps after later health changes, and removes duration-backed clamps.

| Fixture | Context | Concrete spell | Clamp evidence | Commands |
| --- | --- | --- | --- | --- |
| Laveka Live Realm | Global RMT realm spell | `Spell4=75440`, base `51703`, tier `1` | Ratio `1.0`, mode/vital payload `0/2`; useful as a no-effective-cap control row. | `/spell inspect4 75440` then `/spell cast4 75440` |
| Laveka Dead Realm Clamp | Global RMT realm spell | `Spell4=75525`, base `51782`, tier `1` | Ratio `0.05`, mode/vital payload `1/0`, `DataBits04=1`; validates aggressive health ceiling. | `/spell inspect4 75525` then `/spell cast4 75525` |
| Skeleton Dead Realm Clamp | Global RMT realm spell | `Spell4=75587`, base `51839`, tier `1` | Ratio `0.001`; validates minimum-1 HP floor behavior. | `/spell inspect4 75587` then `/spell cast4 75587` |
| Starmap Damaged Surface | Global RMT starmap spell | `Spell4=84383`, base `59542`, tier `1` | Ratio `0.01`, `DataBits04=1`; targetFlags `2`. | `/spell inspect4 84383` then `/spell cast4 84383` |
| Black Hole Growing Singularity | Global RMT starmap spell | `Spell4=85458`, base `60608`, tier `1` | Ratio `0.02`, targetFlags `4`; validates telegraph-target clamp application. | `/spell inspect4 85458` then `/spell cast4 85458` |
| Food Sickness | Global RMT miniboss spell | `Spell4=86766`, base `61896`, tier `1` | Ratio `0.1`, duration `10000`; validates duration cleanup. | `/spell inspect4 86766` then `/spell cast4 86766` |
| CQ Treasure Clamp Staircase | Global RMT miniboss spells | `Spell4=87732..87739`, bases `62805..62812`, tier `1` | Ratios descend from `0.9` to `0.2`; validates obvious ratio staircase. | `/spell inspect4 87732` then `/spell cast4 87732` |

## Shield Overload Fixtures

Most `ShieldOverload` rows have all-zero payload plus a duration. The current runtime treats those as temporary shield shutdown: current shields are set to zero, normal shield regeneration is suppressed while the effect state is active, and duration cleanup resumes regeneration. Non-zero payload rows remain diagnostic-only.

| Fixture | Context | Concrete spell | Shield overload evidence | Commands |
| --- | --- | --- | --- | --- |
| Test Shield Overload | Global test spell | `Spell4=27663`, base `14147`, tier `1` | All-zero payload, duration `6000`; simple shield shutdown fixture. | `/spell inspect4 27663` then `/spell cast4 27663` |
| Test Shield Overload Variant | Global test spell | `Spell4=42363`, base `26554`, tier `1` | All-zero payload, duration `5000`; validates duration cleanup. | `/spell inspect4 42363` then `/spell cast4 42363` |
| Warrior Shield Burst | Global deprecated player spell | `Spell4=50186`, base `21934`, tier `2` | All-zero payload, delay `100`, duration `4000`; validates delayed overload state. | `/spell inspect4 50186` then `/spell cast4 50186` |
| Medic Dematerialize | Global player spell | `Spell4=60044`, base `27006`, tier `5` | All-zero payload, duration `2600`; validates player ability style row. | `/spell inspect4 60044` then `/spell cast4 60044` |
| Wingo Scanbot Dematerialize | Placed creature context | `Spell4=76637`, base `52812`, tier `1` | All-zero payload, duration `8000`; placed creature fixture from world context. | `/spell inspect4 76637` then `/spell cast4 76637` |
| Unstable Anomaly Diagnostic | Global engineer spell | `Spell4=70005`, base `47081`, tier `1` | Non-zero payload `163/2/0.7`, duration `4500`; validates diagnostic-only branch. | `/spell inspect4 70005` then `/spell cast4 70005` |
| Shield Shred Diagnostic | Global encounter spell | `Spell4=74912`, base `51249`, tier `1` | Non-zero payload `45192/0/0/1.0/1/60`, no duration; validates skipped secondary payload. | `/spell inspect4 74912` then `/spell cast4 74912` |

## Proc Fixtures

`Proc.DataBits01` resolves to the trigger `Spell4` in the overwhelming majority of rows, and `DataBits02` is a float-bitcast chance. The current runtime registers the decoded proc state and lifetime cleanup only; these fixtures are for proving trigger-event and target-routing behavior before enabling actual event dispatch.

| Fixture | Context | Concrete spell | Proc evidence | Commands |
| --- | --- | --- | --- | --- |
| Momentum On Kill | Global item/talent proc | `Spell4=7116`, base `3574`, tier `1` | Trigger event `1`, trigger spell `7117`, chance `1.0`, target data `2`; validates on-kill shell. | `/spell inspect4 7116` then `/spell cast4 7116` |
| Readiness Enter Combat | Global item/talent proc | `Spell4=4876`, base `2525`, tier `1` | Trigger event `6`, trigger spell `4877`, chance `1.0`, target data `2`; validates enter-combat shell. | `/spell inspect4 4876` then `/spell cast4 4876` |
| Brutal Damage Proc | Global item special | `Spell4=4046`, base `2019`, tier `1` | Trigger event `12`, trigger spell `4047`, chance `0.15`, target data `4`; validates damage/on-hit shell. | `/spell inspect4 4046` then `/spell cast4 4046` |
| Bleed Damage Proc | Global item special | `Spell4=4075`, base `2052`, tier `1` | Trigger event `12`, trigger spell `4076`, chance `0.2`, target data `4`, payload `DataBits05=6`. | `/spell inspect4 4075` then `/spell cast4 4075` |
| Explosive Damage Proc | Global item special | `Spell4=4191`, base `2130`, tier `1` | Trigger event `12`, trigger spell `4192`, chance `0.35`, target data `4`; validates AOE trigger spell routing. | `/spell inspect4 4191` then `/spell cast4 4191` |
| Warrior Confrontation | Deprecated Warrior proc | `Spell4=1024`, base `1024`, tier `1` | Trigger event `16`, trigger spell `1030`, chance `1.0`, duration `200`; validates damage-taken registration and duration cleanup. | `/spell inspect4 1024` then `/spell cast4 1024` |
| Sprint Daze | Global movement proc | `Spell4=1316`, base `1316`, tier `1` | Trigger event `16`, trigger spell `40364`, chance `1.0`, target data `33`, extra filter payload in `DataBits06..08`. | `/spell inspect4 1316` then `/spell cast4 1316` |
| Spellslinger Affinity | Global class proc | `Spell4=39063`, base `23829`, tier `1` | Trigger event `76`, trigger spell `79078`, chance `1.0`, duration `500`, cooldown-like `5000`, target data `17`; validates cooldown/target-route evidence. | `/spell inspect4 39063` then `/spell cast4 39063` |

## Unit State Set Fixtures

`UnitStateSet.DataBits00` is now decoded as a raw unit state id and tracked for duration cleanup. The current runtime records the state and emits diagnostics, but individual state-id behavior such as block, invulnerability, all-spell immunity, barrier, and burrow targeting is still validation-only.

| Fixture | Context | Concrete spell | Unit-state evidence | Commands |
| --- | --- | --- | --- | --- |
| Yeti Brawler Block | Global creature family spell | `Spell4=3946`, base `1947`, tier `1` | State `1`, duration `1000`, simple payload; validates block-state tracking. | `/spell inspect4 3946` then `/spell cast4 3946` |
| Shadow Crawler Block | Global creature family spell | `Spell4=4360`, base `2222`, tier `1` | State `1`, duration `1000`, simple payload; validates common NPC block row. | `/spell inspect4 4360` then `/spell cast4 4360` |
| Generic Invulnerability | Global test spell | `Spell4=4178`, base `2122`, tier `1` | State `6`, duration-backed invulnerability naming; validates non-block state tracking. | `/spell inspect4 4178` then `/spell cast4 4178` |
| All Spell Immunity | Global deprecated spell | `Spell4=2362`, base `1258`, tier `1` | Paired states `7/8/9/16/17/18`, all-spell-immunity naming; validates multi-row state tracking. | `/spell inspect4 2362` then `/spell cast4 2362` |
| Sonic Barrier | Global barrier spell | `Spell4=36601`, base `22663`, tier `1` | State `22`, barrier naming; validates barrier cluster without applying combat gates yet. | `/spell inspect4 36601` then `/spell cast4 36601` |
| Gloomclaw Burrow Move | Placed/encounter-style spell | `Spell4=56899`, base `36489`, tier `1` | State `23`, burrow move naming; validates movement/targeting candidate state. | `/spell inspect4 56899` then `/spell cast4 56899` |

## Set Busy Fixtures

`SetBusy.DataBits00=1` sets busy and `DataBits00=0` clears busy. The strongest rows pair an immediate set with a delayed clear on the same concrete spell; duration-backed rows also expire through the spell lifetime queue.

| Fixture | Context | Concrete spell | Busy evidence | Commands |
| --- | --- | --- | --- | --- |
| Generic 10s Activate Busy | Global quest spell | `Spell4=35174`, base `20905`, tier `1` | Immediate set row, delayed clear after `10000` ms. | `/spell inspect4 35174` then `/spell cast4 35174` |
| Explicit Set/Unbusy | Global quest spell | `Spell4=41576`, base `25772`, tier `1` | Name says set busy, wait, set unbusy; delayed clear after `10000` ms. | `/spell inspect4 41576` then `/spell cast4 41576` |
| Tugga Context Busy | Global signal spell | `Spell4=36188`, base `21770`, tier `1` | Context id `11504`, delayed clear after `5000` ms. | `/spell inspect4 36188` then `/spell cast4 36188` |
| Junk Context Busy | Global Halon Ring spell | `Spell4=41873`, base `26068`, tier `1` | Context id `2`, delayed clear after `30000` ms. | `/spell inspect4 41873` then `/spell cast4 41873` |
| Finder's Rock Duration Busy | Global activation spell | `Spell4=47634`, base `31565`, tier `1` | Set row has duration `180000` plus delayed clear at `180000` ms. | `/spell inspect4 47634` then `/spell cast4 47634` |
| Activate And Clear Effects | Global city spell | `Spell4=76797`, base `52715`, tier `1` | Context id `27096`, duration `8000`; validates duration cleanup. | `/spell inspect4 76797` then `/spell cast4 76797` |

## Progression Reward Fixtures

`PathXpModify` now supports raw path XP and path-level grants through `PathManager`. `GrantLevelScaledXP` now grants a percent of the current level span through `XpManager`, capped by the encoded max level. `GiveAugmentPowerToPlayer` now grants runtime AMP bonus power through `ServerAmpPowerUpdate`. `GrantLevelScaledPrestige`, ability points, and inlaid augment unlocks remain evidence-only until their storage/runtime surfaces are identified.

| Fixture | Context | Concrete spell | Progression evidence | Commands |
| --- | --- | --- | --- | --- |
| Discovery Path XP | Global discovery spell | `Spell4=7105`, base `3579`, tier `1` | `PathXpModify` amount `1`, mode `0`; validates raw path XP. | `/spell inspect4 7105` then `/spell cast4 7105` |
| Tier 2 Discovery Path XP | Global discovery spell | `Spell4=83956`, base `59698`, tier `1` | `PathXpModify` amount `1`, mode `0`, plus activation/busy utility rows. | `/spell inspect4 83956` then `/spell cast4 83956` |
| PvP Path XP Reward | Global PvP reward spell | `Spell4=39550`, base `24134`, tier `1` | `PathXpModify` amount `10`, mode `0`, with PvP secondary payload. | `/spell inspect4 39550` then `/spell cast4 39550` |
| Mystery Box Path Level | Global item reward spell | `Spell4=71363`, base `48854`, tier `1` | `PathXpModify` amount `1`, mode `1`; validates one-level grant. | `/spell inspect4 71363` then `/spell cast4 71363` |
| PTR Path Level Grant | Global test spell | `Spell4=78323`, base `54472`, tier `1` | `PathXpModify` amount `30`, mode `1`; validates capped multi-level grant. | `/spell inspect4 78323` then `/spell cast4 78323` |
| Walatiki Participation XP | Global PvP reward spell | `Spell4=42932`, base `26968`, tier `1` | `GrantLevelScaledXP` percent `20`, max level `50`, mode `1`. | `/spell inspect4 42932` then `/spell cast4 42932` |
| Walatiki Win XP | Global PvP reward spell | `Spell4=42933`, base `26969`, tier `1` | `GrantLevelScaledXP` percent `10`, max level `50`, mode `1`. | `/spell inspect4 42933` then `/spell cast4 42933` |
| Sabotage Bomb XP | Global PvP objective spell | `Spell4=80298`, base `56884`, tier `1` | `GrantLevelScaledXP` percent `5`, targetFlags `4`; validates player-owner resolution. | `/spell inspect4 80298` then `/spell cast4 80298` |
| AMP Power Unlock | Global class unlock spell | `Spell4=67476`, base `44968`, tier `1` | `GiveAugmentPowerToPlayer` amount `1`; validates `ServerAmpPowerUpdate` and action-set bonus-power grant. | `/spell inspect4 67476` then `/spell cast4 67476` |

## Housing And Stuck Utility Fixtures

`HousingTeleport.DataBits00=0` and the all-zero `HousingEscape` row now route through the existing player residence teleport path. `HousingTeleport.DataBits00=2` only appears on deprecated auto-attack starter rows and remains guarded. `SupportStuck` now uses the normal health/death path; durability loss/free-suicide differences are still diagnostic-only.

| Fixture | Context | Concrete spell | Utility evidence | Commands |
| --- | --- | --- | --- | --- |
| Teleport To House | Global housing recall | `Spell4=38408`, base `23865`, tier `1` | `HousingTeleport`, all-zero payload; validates own-residence resolution and residence map lock. | `/spell inspect4 38408` then `/spell cast4 38408` |
| Recall House Stuck Test | Global support/recall spell | `Spell4=49882`, base `34141`, tier `1` | `HousingTeleport`, all-zero payload with 10s cast; validates delayed/cast housing recall. | `/spell inspect4 49882` then `/spell cast4 49882` |
| Housing Escape | Global housing escape | `Spell4=41323`, base `25628`, tier `1` | `HousingEscape`, all-zero payload; validates escape variant using the same residence entrance path. | `/spell inspect4 41323` then `/spell cast4 41323` |
| Support Stuck Free Suicide | Global stuck recovery | `Spell4=52577`, base `36674`, tier `1` | `SupportStuck`, `DataBits00=1.0f`; validates death routing and diagnostic durability marker. | `/spell inspect4 52577` then `/spell cast4 52577` |
| Support Stuck Durability Suicide | Global stuck recovery | `Spell4=53455`, base `37606`, tier `1` | `SupportStuck`, all-zero payload; validates second suicide variant. | `/spell inspect4 53455` then `/spell cast4 53455` |

## Force Facing Fixtures

`ForceFacing` has all-zero payload rows and now uses spell target/position context. `NpcForceFacing.DataBits00` is a float-bitcast degree offset; current runtime applies the offset relative to the affected unit's current yaw and uses `DataBits03` as a short rotation duration when available.

| Fixture | Context | Concrete spell | Facing evidence | Commands |
| --- | --- | --- | --- | --- |
| Camera Pose Action Bar | Global quest/camera spell | `Spell4=47370`, base `31189`, tier `1` | `ForceFacing`, targetFlags `3`, all-zero payload; validates target/position resolution. | `/spell inspect4 47370` then `/spell cast4 47370` |
| Turn Turret | Global test spell | `Spell4=28155`, base `14595`, tier `1` | `NpcForceFacing`, targetFlags `2`, angle offset `180` degrees. | `/spell inspect4 28155` then `/spell cast4 28155` |
| Aggressor Bot Mine Deploy | Global encounter spell | `Spell4=42057`, base `26204`, tier `1` | Multiple `NpcForceFacing` rows with `-45` and `-90` degree offsets. | `/spell inspect4 42057` then `/spell cast4 42057` |
| Skull Splitter | Global humanoid telegraph spell | `Spell4=66017`, base `43736`, tier `2` | `NpcForceFacing` rows with `270` and `180` degree offsets plus `DataBits03=300`. | `/spell inspect4 66017` then `/spell cast4 66017` |
| Ice Smasher | Arcterra global spell | `Spell4=83288`, base `59125`, tier `1` | Repeated `270/180/135/45` offsets with 300 ms turn payloads. | `/spell inspect4 83288` then `/spell cast4 83288` |

## Family-Specific Queries

Top scored candidates:

```sql
SELECT
  continent,
  zone,
  creature_id,
  LEFT(creature_description, 90) AS creature,
  spell4_id,
  spell4_base_id,
  spell4_tier,
  placements,
  placement_effect_rows,
  priority_score,
  effect_families,
  inspect_command,
  cast_command
FROM world_runtime_spell_candidates
WHERE entity_type = 0
ORDER BY priority_score DESC, placements DESC, placement_effect_rows DESC
LIMIT 40;
```

Property modifier candidates:

```sql
SELECT
  continent,
  zone,
  creature_id,
  LEFT(creature_description, 90) AS creature,
  spell4_id,
  spell4_base_id,
  spell4_tier,
  placements,
  spell4_description,
  effect_families,
  inspect_command,
  cast_command
FROM world_runtime_spell_candidates
WHERE entity_type = 0
  AND has_unit_property_modifier = 1
ORDER BY placements DESC, priority_score DESC
LIMIT 30;
```

Timing candidates:

```sql
SELECT
  continent,
  zone,
  creature_id,
  LEFT(creature_description, 90) AS creature,
  spell4_id,
  spell4_base_id,
  spell4_tier,
  placements,
  effect_families,
  inspect_command,
  cast_command
FROM world_runtime_spell_candidates
WHERE entity_type = 0
  AND (has_delay = 1 OR has_tick = 1 OR has_duration = 1)
ORDER BY priority_score DESC, placements DESC
LIMIT 30;
```

Proxy chain details for any candidate:

```sql
SELECT
  spell4_id,
  orderIndex,
  targetFlags,
  delayTime,
  tickTime,
  durationTime,
  chained_spell4_id,
  chained_spell4_description
FROM world_proxy_chain_context
WHERE entity_type = 0
  AND spell4_id IN (57353, 77996, 56911, 59523, 52796)
ORDER BY spell4_id, orderIndex;
```

## Evidence Loop

1. Pick a fixture from `world_runtime_spell_candidates`.
2. Run `/spell inspect4 <spell4_id>` and save the decoded effect/timing/target output.
3. Run `/spell cast4 <spell4_id>` against a controlled target.
4. Compare `SpellDiagnostics effect-schedule`, `effect-lifetime`, `effect-dispatch`, `healing-output`, `healing-absorption-output`, `healing-absorption`, `shield-healing-output`, `shield-damage-output`, `vital-modifier`, `proc`, `summon-creature`, `summon-trap`, `npc-execution-delay`, `spell-effect-immunity`, `spell-immunity`, `mimic-disguise`, `scale`, `faction-set`, `quest-advance-objective`, `delay-death`, `delay-death-triggered`, `delay-death-trigger-cast`, `force-facing`, `forced-move`, and `spell-go` logs with `ServerSpellStart`, `ServerSpellGo`, combat logs, target state, visible buff/property changes, spawned/despawned world entities, movement commands, and spell lifetime.
5. Promote a field interpretation only when the SQL row, local runtime behavior, and sniff/client evidence agree.
