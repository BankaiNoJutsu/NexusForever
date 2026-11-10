# Proxy (effect type 26): execution limit and who casts the child (2026-11-10)

Evidence tier: **raw tables** (`Spell4Effects`, `SpellEffectType`), **game tooltip text** (Jabbithole `spells.flavor`, an independent rendering of the client tooltip) and table statistics. The client does not
execute spell effects, so there is no native consumer; the video corpus (`I:\tools\video_index\corpus.db`, 8,448 videos) was searched for a spoken statement and has none. Status: **Implemented, inferred.**
No retail parity claim.

## 1. DataBits04 is the maximum number of executions

Three independent observations:

1. **Client typing.** `SpellEffectType.tbl` types Proxy word 04 as an integer (code 1); only 17 words in the whole table carry that code.
2. **Tick count equality.** Among Proxy rows that tick (`tickTime != 0`), have a duration and a finite word 04, the value equals `durationTime / tickTime` in 52 of 76 rows: 12 with 6000 ms / 500 ms (44 rows),
   5 with 5000 / 1000, 6 with 30000 / 5000, 10 with 10000 / 1000, 20 with 10000 / 500. Ticking proxies are otherwise almost always `0xFFFFFFFF` (755 of 791 without a duration, 664 of 751 with one), while
   non-ticking proxies split 8,357 with 1 against 6,285 with `0xFFFFFFFF`.
3. **Tooltip witness.** Telekinetic Strike, Spell4 `32893`:

   | Effect row | Type | TargetFlags | Data |
   |---|---|---|---|
   | 62159 | Damage | 4 (area targets) | |
   | 62204 | Proxy | 4 (area targets) | DataBits00 = 30900, DataBits03 = 1.0, DataBits04 = 1, DataBits05 = 1000 |

   Spell `30900` is `VitalModifier` on target flag 1 (its caster): vital 6, amount 1. The tooltip reads "Deal 33.6%AP +18*lvl magic damage to 5 foes and generate a Psi Point". A per-target proxy without a limit
   would grant one point per foe hit.

A fourth, weaker check: the server already special-cased Warrior Relentless Strikes (row `177057`, target flag 4, child `53865` = two caster-flag VitalModifiers). That row has word 04 = 1, so the general rule
reproduces the hand-written case.

Rejected reading: a stack limit on the child spell. Spell `30900` is an instant vital change with nothing to stack, and the tick equality has no stack interpretation.

### Open
* **Scope with several targets on a ticking proxy.** The count is kept per effect row for the life of the spell instance. For a ticking proxy that hits several targets per tick with a finite limit this may
  exhaust early; no witness row was examined.
* **Word 04 = 0** (about 115 rows) is treated as unlimited; nothing shows what it means.
* **DataBits03** (float, 1.0 on 15,873 rows, 100.0 on 604, fractions on others) and **DataBits05** (60 on 10,099 rows, 1 on 2,444, 120, 30, 900, 1000) are still unread. A trigger chance and an internal
  cooldown or range are candidates; the two scales in word 03 are unexplained.
* Words 06 and 07 are prerequisites, see 1e (implemented later the same day).

## 1b. A ticking Proxy keeps its spell in DataBits01 (added later the same day)

| Tick time | Duration | Only word 00 set | Only word 01 set | Both set | Neither |
|---|---|---:|---:|---:|---:|
| no | no | 14,975 | 174 | 17 | 81 |
| no | yes | 79 | 7 | 1 | 7 |
| yes | no | 39 | 787 | 4 | 1 |
| yes | yes | 4 | 749 | 2 | 2 |

Where both are set on a ticking row they hold the same id. Example: Esper Mental Boon (Spell4 `33257`) rows `76772` to `76776`: tick 500 ms, duration 6000 ms, word 00 = 0, word 01 = 36562 to 36621, execution limit 12
(= 6000 / 500). The server read only word 00, so these rows cast nothing. The interpreter now uses word 01 when word 00 is 0 and the row has both a tick and a duration (749 rows), which are the rows the
scheduler pulses. Status: Implemented, inferred from the table split; upstream `spells-v2` names the word `PeriodicSpellId`.

Not changed: the tick-only rows (the scheduler does not pulse rows without their own duration; see 1c for where their lifetime comes from) and the 174 rows with only word 01 and no tick.

**DataBits03 is not simply a chance.** One tooltip agrees with a chance reading (Spell4 42566, word 03 = 0.1, "10% chance to disorient"), but Mental Boon has five rows to the same child (`87375`) and the same
caster-apply prerequisite with word 03 = 1.0, 0.8, 0.6, 0.4 and 0.2, next to a tooltip that scales per Psi Point, and Warrior Breaching Strikes has 0.25 on a row whose tooltip states no chance. Of 40 rows
with a fractional word 03 and a Jabbithole tooltip, only that one tooltip states the same percentage. Left unread.

## 1c. Tick-only rows live as long as their spell (added later the same day; not implemented)

The 804 Proxy rows with a tick and no duration of their own split by the owning spell:

| Owning spell | Rows | Spells |
|---|---:|---:|
| `Spell4.spellDuration` = `0xFFFFFFFF` | 155 | |
| `Spell4.spellDuration` finite (10 s on 40 rows, 6 s on 21, others) | 163 | 153 |
| no spell duration, cast method 0, no channel | 491 | 413 |
| no spell duration, channelled (`channelMaxTime` set) | 18 | 17 |

* The `0xFFFFFFFF` rows are permanent passives whose tooltip describes a standing proximity test: Medic Danger Zone (Spell4 `58912`, tick 500 ms, area target flag 4, word 01 = `60920`) "While within
  8m of a foe gain an Empower for 8s"; Warrior No Escape `59121`, Recklessness `59122`, Fortitude `59166`; rune special `82165`; "Offensive Sproutling" `61244` (tick 2000 ms) "receive magic damage
  ... every second" (the tooltip and the tick disagree by a factor of two on that one).
* The rows without a spell duration are milestone and AMP passives ("Engineer - Master Mechanic - Milestone", tick 5000 ms), toggles and creature hazard auras ("Flying Debris - Pulsing", "[WB] King
  Honeygrave - Raid Damage AOE Tick"). Tooltip witness for the tick: Warrior Power Link (`53599`, tick 250 ms, word 01 = `79788`) "Drains 56 Kinetic Energy every 0.25s".

Reading: a tick-only Proxy casts its periodic spell (word 01) every tick for as long as the owning spell is alive; the lifetime is the spell's (`spellDuration`, a toggle being switched off, a
passive being removed), not the row's. Evidence tier: table split plus the tooltip matches. **Partly implemented later the same day** (finite spell duration only, below). The permanent, duration-less and channelled groups
(664 rows) are still not pulsed: the server has no spell lifetime outside its pending-event queue, and learned passives are applied as properties without a spell instance, so those rows
need a lifetime model for passives, toggles and auras first. Their Proxy rows stay inert (`Spell.IsUnpulsedPeriodicProxy`). One finite row has an execution limit that is not
`spellDuration / tick` (Eldan Security Beam `39498`: tick 500 ms, spell duration 60 s, limit 60), so the limit is not derived from the lifetime.

**Finite spell duration: implemented, inferred.** Three tooltips state both the lifetime and the tick, and both match `Spell4.spellDuration` and the row's `tickTime`:

| Field spell | spellDuration | Tick-only rows | Parent tooltip |
|---|---:|---|---|
| Medic Restrictor field `38287` | 4000 | Proxy `131274`, tick 500, word 01 = `77118` | "leave behind a field that lasts for 4s. Apply a Snare to foes inside the field every 0.5s" |
| Warrior Polarity Field aura `38975` | 10000 | Damage `84528`, Proxy `158181`, VitalModifier `167502`, all tick 1000, delay 1000 | "Create a 8m field, lasting 10s ... deals ... damage to all foes inside the field every 1s" |
| Medic Nullifier field `58868` | 6000 | Proxy `141450`, tick 1000, word 01 = `77099` | "Create a field, lasting 6s, and apply an Affliction to foes inside the field every 1s" |

The Polarity Field rows show the rule is not specific to Proxy: 359 Damage rows and 30 rows of other types are tick-only inside a spell with a finite duration. `Spell.GetEffectPulseDuration`
now pulses any tick-only row every `tickTime` until `spellDuration` (first pulse at the delay, or at one tick, as for rows with their own duration). The interpreter reads word 01 on every
ticking Proxy row. A server-side guard that is not from any source stops these pulses when the caster is dead, because a few creature auras carry durations of hours (`9999999`, `99999999`).
Previously such rows executed once at cast time, or for Proxy rows cast nothing. No retail parity claim. The client classifies these spells itself: 559 of the 564 such rows belong to spells with cast method 10 (Aura), see `FORK_CROSSCHECK_20261110.md`. Permanent Auras (spell duration `0xFFFFFFFF`) pulse the same way until the spell ends, for effect types that leave no state behind; the 514 tick-only Proxy rows of spells with cast method 0 and no duration are still inert.

**DataBits05 was checked against the "can only be triggered by the same foe every N s" tooltips and is not that interval**: the five passives above state 15, 10, 10, 10 and 15 s and all carry 120.
Their word 07 holds a different id on each (24549, 24655, 24658, 37496, 38384), which was not followed. Word 05 clusters by authoring family rather than by any tooltip number: 60 on 10,099 rows,
1 on 2,444, 120 on 1,136, 900 mostly on Engineer spells, 1000 mostly on Esper spells, 12 on 319 rows that all tick and mostly carry word 03 = 100.0. Still unread.

Upstream `spells-v2` reads the same four words the server now reads (00 spell, 01 periodic spell, 04 maximum executions, with the same Telekinetic Strike example, and 06 prerequisite) and is
silent on words 03 and 05. That is agreement between two inferred readings, not independent evidence.

## 1d. DataBits03: the Mental Boon ladder in context (added later the same day; still unread)

The five Mental Boon rows with word 03 = 1.0, 0.8, 0.6, 0.4 and 0.2 (rows `228670`, `228728` to `228731`, repeated on all nine tiers `33257`, `55922` to `55929`) are not part of the base ability.
They share one caster-apply prerequisite (`43597`: the caster has spell `87345` or `87346`, "Rune Set - Esper - Healer - RMT - Base" tiers 1 and 2) and one child, `87375` "Rune Set - Esper -
Healer - RMT - ABil Mod", a single ModifySpell effect (word 01 = 634) lasting 5 s. The rows are identical apart from word 03. Mental Boon is a finisher that spends 1 to 5 Psi Points.

Competing readings, neither confirmed:

| Reading | For | Against |
|---|---|---|
| Chance that the row fires | Spell4 `42566` has 0.1 next to "10% chance to disorient" | five identical rows with a descending chance is an odd way to author anything; 39 of 40 fractional rows with a tooltip state no such percentage |
| Threshold on a normalised quantity (here Psi Points spent / 5), so that n points fire n rows | explains five otherwise identical rows in steps of 0.2 on a five-point finisher; the child is a stackable modifier | does not explain the 0.1 "10% chance" row, nor the 100.0 and 1000.0 values on ticking rows |

No tooltip for the rune set is in Jabbithole (`87345`, `87346` and `87375` have no record) and the video subtitle index has no statement of the set bonus. The experiment that separates the two is
a retail recording of an Esper with that set casting Mental Boon at 1 and at 5 Psi Points and counting the stacks of the 5 s modifier. Left unread.

## 1e. DataBits06 and DataBits07 are prerequisites: caster and target (added later the same day; implemented, inferred)

Evidence tier: raw tables only. No native consumer, no tooltip states the rule. No retail parity claim.

* **Both words resolve.** Word 06 is set on 470 Proxy rows and word 07 on 257; every value is the id of a `Prerequisite` row (470 of 470, 257 of 257). 220 of the word 07 values are also
  valid Spell4 ids, which is id overlap: the same values read as prerequisites give conditions that fit the row (below).
* **Word 06 describes the caster.** On rows that target foes or an area (target flags 2, 4, 6) it carries ability-book conditions that only the caster can meet: type 59 (spell tier) and 233
  (tier unlocked) on 25 rows. Examples: Stormtalon Lightning Storm (`63116`, area) has five proxies gated on the tier of the boss spell `39613` being 1 to 5; "Rune Set - Medic - Healer -
  Resource Probes" (`82553`, area) gates each proxy on a probe ability tier being unlocked; "Warrior - Add Cell - Relentless Strikes - T4+" gates on the spell tier.
* **Word 07 describes the target.** On target and area rows it carries type 38 (creature id) and type 3 (class) on 20 rows, for example "NPC - Battle Prowess" (`43558`, target flag 2), whose
  five proxies each require a different creature id. On area auras it is "does not already have the child spell": Medic Danger Zone (`58912`, area, periodic spell `60920`
  "Danger Zone - Cooldown", a 15 s marker on the target) carries prerequisite `24549` = type 50 NotEqual `60920`, and the tooltip says the passive "can only be triggered by the same foe
  every 15s". That also settles what the word 07 ids noted in 1c were.
* Mirrors the row's own `prerequisiteIdCasterApply` and `prerequisiteIdTargetApply`, which no Proxy row with these words uses for the same purpose.

Implemented in `Spell.MeetsProxyPrerequisites` (plain Proxy, type 26, only): word 06 is evaluated on the caster with the target as second unit, word 07 on the target with the caster as
second unit, before the handler runs. Assumed, not evidenced: a proxy that fails these does not use up an execution of word 04. Not examined: the other proxy effect types.

## 1f. DataBits03 is the trigger chance (added 2026-11-10, late; implemented, inferred)

Supersedes the two competing readings in 1d. Evidence tier: raw tables and tooltip text. No native consumer. No retail parity claim.

1. **Same layout as Proc.** The Proc effect (type 14) keeps a float chance in word 02 and a cooldown in milliseconds in word 04, and AMP tooltips confirm both on about twenty AMPs:
   "MKII Battle Suit" 25% chance, every 3s (0.25, 3000); "Anti-Magic Armor" 30% (0.3); "Make It Rain" 10%, every 3s (0.1, 3000); "Companion" 20%, every 10s (0.2, 10000);
   "Can't Stop This" 100%, every 20s (1.0, 20000); "Quick Response" every 15s (15000). Proxy has the same float in front of its execution limit: 1.0 on 15,873 rows, fractions on about 250.
2. **One Proxy tooltip states it.** Spell4 `42566`, 0.1, "10% chance to disorient".
3. **The Esper healer rune set puts 0.2 per Psi Point on separate spells.** Its child `87375` hangs off "Reverie - 2 Point Proxy" with 0.4, "3 Point Proxy" 0.6, "4 Point Proxy" 0.8, and
   "Mending Banner - Proxy 1" to "Proxy 4" with 0.2, 0.4, 0.6, 0.8. Each of those spells is already specific to a point count, so a threshold on the points spent (the second reading in 1d)
   would be redundant there; a chance that grows with the points spent is not.
4. **The children of the other fractional rows are chance-style bonuses by name:** "Breaching Strikes - Cooldown Reset" (0.25), "Reaver - Support Ability Cooldown" (0.3),
   "D+S - Inlaid C CDR Dematerialize" (0.2). Their unlock tooltips are not in Jabbithole, so these are names only.
5. **Older scale.** 100.0 (604 rows) and 1000.0 (53) sit on pre-release and test spells ("[Paladin] ..."), a percent scale; they always fire.

Of 17 distinct fractional rows that reach a tooltip, one states the percentage and none contradicts it; the rest are silent because the tooltip belongs to the base ability, not to the
bonus the row implements.

Implemented in `SpellHandler.PassesProxyChance`: a row with 0 < word 03 < 1 fires when a uniform roll is below it. 0 (115 rows) is treated as always, because nothing shows what it means.
Assumed: a failed roll does not use up an execution (word 04). Order: prerequisites (1e), then the roll, then the execution limit.

**Word 05 is still unread.** It is an integer with a default of 60 and "unlimited"-looking values (999, 1000, `0xFFFFFFFF`), and Proc word 05 draws on the same values (1 on most AMPs, 60,
10, 100). It is not the AMP "every N s" interval (that is Proc word 04) and no column of the parent or child spell matches it. Reads as a cap on something; on what is not known.
One more observation, not enough to read it: among AMP procs the word is 1 except on the two whose tooltip limits the proc "per foe" or "per target" (Frost Armor "Can only occur
every 10s per foe", Healing Touch "once every 4s per target"), where it is 10. That would make it the number of distinct targets a per-target limit keeps track of. The Proxy rows of
the "same foe every N s" passives carry 120, not 10, and two AMPs without such wording carry 60 and 100, so this stays a lead.

## 1g. Effect flag 0x2 marks a row that lives as long as its spell (added 2026-11-10, late)

`Spell4Effects.flags` bit `0x2` is set on every one of the 1,805 rows that tick without a duration of their own, on none of the 3,994 rows that tick with one, and on none of the
32,306 rows that have a duration and no tick. Among rows with neither it is set on 20,009 of 92,905 (permanent modifiers, procs). The bit therefore marks "this row has no lifetime of its own
and lives with the spell", which is the rule 1c arrived at from the tooltips.

Two more tooltip witnesses for the tick, both on spells that are **not** Auras (cast method 0, no spell duration):

| Spell | Row | Tooltip |
|---|---|---|
| Rune special "Untouchable" `81880` | Proxy, tick 1000, periodic spell `81881` | "Every 1s while in combat, gain an Empower" |
| Warrior Power Link `53599` | Proxy, tick 250, periodic spell `79788` = VitalModifier vital 6, amount -56 | "Toggle: While active ... Drains 56 Kinetic Energy every 0.25s" |

These are the 509 rows (430 spells) that stay inert: rune and item specials, milestones, toggles and creature "base" spells. The table gives them a tick and no end. 265 of the 430 spells
have neither a persistence prerequisite, a channel nor a cost that could end them; what ends them is whatever granted them (unequipping the rune, toggling the ability off, the creature
dying). **Implemented in a later commit the same day, see 1h**; when this section was written the server had no way to stop such a spell.

## 1h. Toggles and granted passives: the spell runs until it is removed (added 2026-11-10, late; implemented, inferred)

Evidence tier: raw tables and tooltip text; the mechanism is read off the two Warrior toggles, whose data is complete enough to follow end to end. No native consumer. No retail parity claim.

### What the data says a toggle is
Warrior Power Link (`53599`, tooltip "Toggle: While active, you and nearby party members deal 8% increased damage, and you gain Kinetic Drain ... Drains 56 Kinetic Energy every 0.25s"):

| Row | Type | Timing | Data | Caster-apply prerequisite |
|---|---|---|---|---|
| `210450` | Proxy | once | `79790` "Power Link - ON" (a permanent Aura, cast method 10, duration `0xFFFFFFFF`) | `37397`: the ON aura is **not** active |
| `210449` | Proxy | tick 250 ms, flag 0x2 | periodic spell `79787` "Power Link - Buff" | `37397` |
| `210627` | Proxy | tick 250 ms, flag 0x2 | periodic spell `79788` "KE Drain Tick" = VitalModifier, vital 6, -56; word 06 = `43748` (not in Overdrive) | `37397` |
| `210456` | Proxy | once | `79789` "Power Link - Spell Remove" = two SpellForceRemove rows (scope 3 = base spell) naming the base spells of the toggle (`35146`) and of the ON aura | `37398`: the ON aura **is** active |

So one ability is both switches. Pressed while off, it applies the ON aura and starts the two ticking rows; pressed while on, it casts the remover. The ON aura also carries a Proc
(event 87, cooldown 250 ms) that casts the same remover under prerequisite `37374`: "Kinetic Energy < 250 and not in Overdrive", which is the same 250 the ability needs to be cast.
Warrior Augmented Blade (`46935`) is built the same way. These are the only two abilities whose tooltip says "Toggle".

Three things follow, and each is now implemented:

1. **The ticking rows run until the spell is removed.** `Spell.GetEffectPulseDuration` pulses a tick-only row with effect flag 0x2 until the spell ends, for any spell without a
   finite duration (before: only permanent Auras), for effect types that leave no state behind.
2. **The caster-apply prerequisite of such a row is asked once, when the row starts.** It cannot be asked on every pulse: the rows require "ON is not active" and the same cast applies
   ON, yet the tooltip has them tick every 0.25 s while active. `Spell.ScheduleEffect` tests it at cast time and `MeetsApplyPrerequisites` skips it on later pulses; target-apply and the
   Proxy words 06 and 07 are still asked on every pulse.
3. **SpellForceRemove ends the running spell.** The handler removed tracked states only. It now also ends running spells of the target that are kept alive by such rows and match
   the same predicate (`UnitEntity.EndSpellLifetimes`), never the spell doing the removing.

### The automatic switch-off needed two more words
* **Proc trigger event 87 is "a class resource changed".** 31 Proc rows use it, on spells that react to a resource level: Warrior Full Defense ("While above 500 Kinetic Energy gain a
  Defense"), Kinetic Fury, Kinetic Buffer, Engineer Volatile Injection, Medic In Flux, the class "On Start" spells, and the two ON auras. The server now raises it when one of the
  resource stats changes (`ProcTriggerEventCandidate.ResourceChanged`). Inferred from the names and from the toggles; nothing states it.
* **Proc DataBits06 is a prerequisite on the holder.** All 392 populated values are Prerequisite ids. It is now evaluated before a proc fires.

Independent corroboration found online for one detail: the community wiki describes Overdrive as preventing "Kinetic Energy decay and Kinetic Energy Drain effects", which is what the
caster prerequisite in Proxy word 06 of the drain row (`43748`: not under Overdrive `42908`) says. The same search did not turn up any statement of whether a toggle switches itself off
at low Kinetic Energy; that behaviour comes from the data above only.

### Granted passives
The AMP lifecycle (`SpellManager.RefreshAugmentations`) already cast and retired AMP spells but excluded anything periodic. It now owns AMPs whose rows all tick on the caster
(`AugmentationSpellRules.IsPeriodic`): the spell is cast when the AMP is slotted, is not recast while it runs, ends with its caster and is cast again once alive, and is ended when the
AMP is retired. Tooltip witness: Stalker "Stealth Regen" (`59394`, Proxy tick 1000 ms, word 06 = `8127`) "While in Stealth you regenerate 5 health per second". Twelve AMPs have that
shape; those in a stack group other than 0 or 2 stay outside, as before.

### Server-side choices that come from no source
* A spell kept alive this way also ends when its caster dies, and when the same caster casts the same spell again.
* A failed start-time prerequisite means the row never starts for that cast; it is not retried.
* Any durationless spell with such rows now runs until removed, whoever casts it. No server script casts one of these 632 spells by id today, and a creature's ends with the creature,
  but a script that casts one on a player and never removes it would leave it ticking until the player dies.

### Still not covered
* Rune and item specials: granted on equip since a later commit the same day, see `EQUIPMENT_SPELL_GRANTS_20261110.md`.
* Milestone and innate class passives with ticking rows: they are mixed with other effect types and are not cast as spells.
* AMPs whose aura has area rows (Medic Danger Zone, Warrior No Escape, Recklessness) are owned since a later commit the same day: the periodic AMP rule accepts area rows and
  a visual row, and a caster-centred aura without an end rebuilds its telegraphs at the caster on every pulse. Fortitude (a property modifier next to the ticking row) and
  periodic AMPs in other stack groups are still outside.
* The retired server's behaviour when a toggle is on and the player logs out or zones is unknown; here the spell ends with the entity.

## 2. The parent's caster casts the child

Previously a unit-target Proxy made the **target** cast the child on itself. The other proxy variants (ProxyLinearAE, ProxyChannel, ProxyRandomExclusive) and the world-object path already cast from the parent's
caster.

* Of the 4,748 distinct child spells of proxies whose target flags include an explicit or area target, 4,562 have effects on non-caster target flags. A spell cast by a unit on itself whose effects address
  an explicit target is degenerate; a spell cast by the original caster at the proxy target is not.
* Of the 186 caster-only children, 48 contain `VitalModifier` (the resource builders, which must reach the original caster for the tooltip to hold).
* Under the old rule a Damage child was cast by the victim on itself, where the handler's `target.CanAttack(spell.Caster)` test compares the victim with itself.

Against: 39 caster-only children contain `ForcedMove`, 6 `Damage` and 4 `CCStateSet`. If any of those are meant to act on the proxy target they now act on the original caster. They were not examined row by
row.

## 3. Server change
`SpellHandler.HandleEffectProxy` now consumes one execution against DataBits04 before anything else (including the existing Relentless Strikes and Artillerybot Barrage special cases) and casts the child from
`spell.Caster` with the proxy target as primary target. Tests: `SpellEffectCombatRegressionTests.HandleEffectProxy_WithExecutionLimitOne_...` (the Telekinetic Strike rows),
`..._CountsExecutionsAgainstTheLimitForTheLifeOfTheSpell`, and the existing position-forwarding test, which now expects the caster to cast. One existing test encoded the target-casts rule.

Not verified: the change was not run in a live session. A child spell now goes through primary-target validation (range, facing, valid-target mask) as a cast by the original caster at the proxy target,
where a self-cast passed trivially; children that fail that validation would stop firing. Watch `primary-target-validation` diagnostics for proxied spells in the first live pass.

The Artillerybot Barrage rows (`117258`, `159172`) have word 04 = `0xFFFFFFFF`, yet the server's special case fires them once per cast. That special case came from server-side play testing, not from retail
evidence; it was left as it is.
