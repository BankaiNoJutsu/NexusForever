# Spell System Progress Tracker

Date: 2026-05-15

This is the working implementation tracker for the global `Spell4` / `Spell4Effects` restoration effort. The detailed evidence lives in:

- `Global Spell System Reverse Engineering.md`
- `Spell Effect Evidence Matrix.md`
- `Spell Reverse Engineering Runtime Candidates.md`
- `Tools/SpellReverseEngineering/world_context_queries.sql`

## Current Completion

| Area | Status | Notes |
| --- | --- | --- |
| Structural model | Mostly mapped | `Spell4Base`, concrete `Spell4`, ordered effects, telegraphs, target flags, timing, Jabbithole/world context joins. |
| Runtime scheduler | Implemented, partial parity | Delay rows and duration-bounded tick rows execute through the spell event queue; duration lifetimes handle known families. |
| Target acquisition | Partial | Caster, explicit target, telegraph targets, duplicate merge, explicit primary-target range/vertical/facing-angle validation, `Spell4ValidTargets` dead-target bit `0x08`, target/position AOE anchoring from `Spell4TargetMechanics`, selected-target forwarding for player single-target/target-AOE/chain casts, and AOE target count/range/angle filtering are implemented. All observed `Spell4AoeTargetConstraints.TargetSelection` values `1..5` are now wired: `1` closest, `2` furthest, `3` random, `4` lowest-health, and `5` most-missing-health. Explicit primary targets now resolve through `IWorldEntity`, so valid-target bit `0x02` is enforced for interactable/object targets and the spell core can carry non-unit primary targets through selection, spell-start anchoring, and narrow world-target `Activate`/`Fluff` execution. Full target mechanics, remaining non-corpse valid-target masks, broader world-target effect routing, groups, and AOE prerequisites remain. |
| Diagnostics | Active | `primary-target-validation`, enriched `target-selection`, `effect-schedule`, `effect-lifetime`, `effect-dispatch`, `effect-result`, `spell-go`, `proc`, `proc-probe`, `ravel-signal`, `player-collection`, `housing-teleport`, `support-stuck`, and family diagnostics are available. |
| Command fixtures | Active | `/spell inspect4` and `/spell cast4` support concrete `Spell4` work. |

## Implemented Or Partially Implemented Families

| Family | Status | Remaining Work |
| --- | --- | --- |
| `Damage` / `DistanceDependentDamage` / `DistributedDamage` | Partial | Formula base, armor, crit/glance shell, shield absorption, and damage descriptions exist. Distance/distribution families currently share the damage path; retail distance falloff, target-count splitting, rounding, weapon terms, multi-hit, procs, and result side effects remain. |
| `Transference` | Conservative | Applies decoded drain damage through the shared damage path, restores the caster's decoded vital by transfer rate, and emits `CombatLogTransference`; source-vital modes, shield/absorb healing basis, and exact overkill/glance parity remain. |
| `Heal` | Partial | Health healing uses shared formula path and `CombatLogHeal`; HoT cadence needs validation. |
| `VitalModifier` | Conservative | Positive flat and max-percent restores for supported vitals; sentinel, parameter-driven, drain, and alias modes remain. |
| `SapVital` | Conservative | Decodes `DataBits00` as `Vital`, treats clean `DataBits01/02` payloads as percent-of-max restore/drain amounts by mode, preserves spell source context for health-vital drains, reuses explicit physical/tech/magic/fall/suffocate `damageType` values when present, and emits `CombatLogVitalModifier`; parameter-driven, secondary-payload, unsupported vital, and out-of-range rows remain diagnostic-only. |
| `ClampVital` | Conservative | Decodes `DataBits02` as a max-health ratio, tracks effect-id current-health caps, reapplies caps after health changes, and removes duration-backed caps; mode/vital fields and non-health clamps remain. |
| `HealShields` | Conservative | Formula path restores shield capacity with overheal tracking, `CombatLogHeal`, and `DamageType.HealShields` in the outgoing damage description; packet parity and HoT cadence need validation. |
| `DamageShields` | Conservative | Direct shield damage with `CombatLogDamageShield`; only 5 global rows, so sniff validation is still needed. |
| `ShieldOverload` | Conservative | All-zero payload rows set shields to zero and suppress shield regeneration for the effect lifetime; non-zero payload variants remain diagnostic-only. |
| `UnitStateSet` | Partial | Decodes and tracks raw unit state ids by effect id, emits diagnostics, removes duration-backed states, participates in force-remove/dispel cleanup, and now treats states `6/7/8/9/13/14/15/16/17/18/22/23` as hostile-effect immunity that returns `TargetInvulnerable` on hostile primary-target validation and centrally drops later hostile effects with `CombatLogImmune`; state `1` block math remains evidence-only. |
| `SetBusy` | Partial | Decodes `DataBits00=1` as set busy and `DataBits00=0` as clear busy, tracks effect-id busy latches, broadcasts/replays busy state, rejects direct activate/interact requests on busy targets, and removes duration-backed busy state; CSI/deferred interaction parity and context-id semantics remain. |
| `Absorption` | Conservative | Applies formula-backed absorb pools, consumes them before shields, and removes duration-backed pools; damage-type filtering and stat packet parity remain. |
| `HealingAbsorption` | Conservative | Applies formula-backed healing-absorb pools, consumes incoming health heals before they land, reports absorbed heal amounts, and removes duration-backed pools; shield-heal behavior and packet parity remain. |
| `ModifyInterruptArmor` | Conservative | `DataBits00` applies interrupt armor through the bounded interrupt-armor vital path so gains clamp to the target's current `InterruptArmorThreshold`, and duration rows remove it later; `DataBits01` is preserved as a likely consume/remove-on-interrupt mode pending sniff validation. |
| `ThreatModification` / `ThreatTransfer` | Conservative / Diagnostics | Threat modification handles add, reduce, clear, set, and fixate-like modes through `ThreatManager`; transfer rows decode and trace only pending party/raid semantics. |
| `UnitPropertyModifier` | Partial | Tracks modifiers by concrete effect instance, refreshes repeated rows from the same spell/effect without letting stale expiry remove the replacement, removes timed modifiers, and rechecks player-scoped spell/effect persistence prerequisites; stack groups, non-player persistence, and exact buff packet parity remain. |
| `PersonalDmgHealMod` | Conservative | Maps common personal damage/heal modifier codes to existing outgoing damage, incoming damage, and healing multiplier properties; the shared property lifecycle now uses effect-instance cleanup plus player-scoped persistence rechecks, while uncommon codes, non-player persistence, and auxiliary fields remain diagnostic-only. |
| `CCStateSet` | Partial | Set/remove packets, logs, active mask, timed removal, and conservative cast/movement coupling exist; DR, stun breakout, interrupt armor, tether, and additional-data behavior remain. |
| `CCStateBreak` | Partial | Removes tracked CC states, preserves original remove casting ids, and wires player knockdown break through the same tracked-removal path; small-payload semantics and broader breakout parity remain. |
| `SpellDispel` | Conservative | Removes locally tracked dispellable spell state by `SpellClass`, emits dispel logs, and shares buff/CC cleanup with force-remove; stacking, priority, and externally-owned aura parity remain. |
| `ForceFacing` / `NpcForceFacing` | Conservative | Forces affected units to face the spell target/position or applies decoded NPC degree offsets through movement rotation commands; exact blend/mode flags and player-control parity remain. |
| `ForcedMove` | Conservative | Applies timed velocity impulse; exact movement physics and type semantics remain. |
| `Proxy` / non-random variants | Conservative | Chained casts and target forwarding exist; random-exclusive, channel ownership, and shape forwarding remain. |
| `SummonCreature` | Conservative | Creates `INonPlayerEntity` from `Creature2`, places it, tracks duration cleanup; ownership, AI, service payloads, and formation parity remain. |
| `SummonTrap` | Conservative | Creates duration-backed trap/probe entities from decoded `Creature2` ids and preserves trigger `Spell4`, arm time, radius, and flags in diagnostics; trigger/ownership AI remains. |
| `SummonVehicle` | Conservative | Creates `IVehicleEntity` from decoded `Creature2`/`UnitVehicle` ids, optionally boards a player when the row requests it, and traces vehicle creation; seat modes, despawn ownership, and scripted deployables remain. |
| `NpcExecutionDelay` | Structural | Named diagnostics plus duration-backed spell lifetime hold; AI scheduler coupling remains. |
| `SpellForceRemove` variants | Conservative | Cleans tracked properties, CC, stealth, aggro immunity, absorption, and other tracked local states. Remove type `2` uses concrete `Spell4`; remove type `3` uses `Spell4Base`; type `1` and count/stack fields remain. |
| `CooldownReset` | Conservative | Resets all player cooldowns for zero-payload rows and concrete `Spell4` cooldowns when the payload resolves to a spell id; cooldown-group modes remain. |
| `ModifySpellCooldown` / `ActivateSpellCooldown` | Conservative | Modifies or activates resolved concrete player spell cooldowns for obvious millisecond/reset rows; cooldown-group/category and ratio modes remain diagnostic-only. |
| `ModifyAbilityCharges` | Conservative | Adds or sets charges on known player spells using decoded spell id/count/mode; mode table and action-set edge cases remain. |
| `ActionBarSet` | Conservative | Shows decoded `ActionBarShortcutSet` ids through `ServerShowActionBar` using the floating spell bar lane; shortcut-set lane, associated-unit subtleties, and duration cleanup remain. |
| `SpellEffectImmunity` / `SpellImmunity` | Conservative | Tracks effect-type immunity from `SpellEffectImmunity.DataBits00` and concrete-spell immunity from `SpellImmunity` mode `0`; both centrally drop matching later effects with `CombatLogImmune`. `SpellImmunity` modes `1` and `2` remain diagnostic-only. |
| `Scale` | Conservative | Applies networked scale changes, supports transition-in/out payloads, tracks duration-backed restoration, and participates in force-remove/dispel cleanup. |
| `FactionSet` | Conservative | Applies raw `Faction2` id from `DataBits00` and restores duration-backed temporary changes through existing faction packets. |
| `ItemVisualSwap` | Conservative | Applies client visual slot/display swaps through `AddVisual`, snapshots previous slot visuals for duration-backed rows, restores them on expiry, and participates in force-remove/dispel cleanup; slot naming and packet/sniff validation remain. |
| `DisguiseOutfit` | Conservative | Applies decoded `Creature2OutfitInfo` ids plus primary and secondary item-display visuals, tracks duration-backed restoration, and participates in force-remove/dispel cleanup; `DataBits03/04/05` and full slot variants remain. |
| `MimicDisguise` | Conservative | Copies caster display, outfit, and item visuals onto the target, removes target-only visuals, tracks duration-backed restoration, and participates in force-remove/dispel cleanup; owner/source modes and display-name pairing remain. |
| `QuestAdvanceObjective` / `AchievementAdvance` | Conservative | Advances active quest objectives by decoded objective id/progress and grants decoded achievement ids through `AchievementManager`. |
| `AddSpell` / `GrantXP` / `PathXpModify` / `GrantLevelScaledXP` / `GiveAugmentPowerToPlayer` / `Kill` / `GiveItemToPlayer` / `ReputationModify` / `GiveSchematic` / `RewardPropertyModifier` | Conservative | Teaches resolved player spells, grants flat XP, grants path XP/path levels through `PathManager`, grants level-scaled XP as a percent of the current level span, grants runtime AMP bonus power through `ServerAmpPowerUpdate`, routes kill effects through normal health/death handling, creates decoded inventory items, applies reputation deltas, sends schematic-unlock packets while updating obtain-schematic objectives, and applies duration-backed account reward-property modifiers. Prestige, ability point, inlaid augment unlock, AMP bonus persistence, and schematic persistence surfaces remain incomplete. |
| `DelayDeath` | Conservative | Tracks prevent-death states, consumes one on fatal damage, leaves the unit at 1 HP, emits `CombatLogDelayDeath`, and casts decoded trigger spells immediately or after `DataBits02`; exact mode semantics and forced-death expiry remain. |
| `Proc` | Conservative | Decodes trigger event, trigger `Spell4`, float-bitcast chance, target data, cooldown/sentinel, and remaining raw fields; tracks proc state by effect id, supports duration cleanup and force-remove/dispel cleanup, emits `proc` and `proc-probe` diagnostics, and now dispatches conservative holder-side trigger casts for trigger events `1/6/10/12/16/20`. `targetData` `1/2/9` resolve to the holder, `4/12` resolve to the counterpart unit in the observed event, cooldown is consumed only when the trigger cast queues successfully, and same-chain reentry is blocked per proc effect id. Unsupported trigger events and target-data tails remain diagnostic-only. |
| `RavelSignal` | Structural diagnostics | Decodes `DataBits00` as signal mode, `DataBits01` as the likely signal id, preserves remaining payload fields, shows semantics in `/spell inspect4`, and emits `ravel-signal` diagnostics. No receiver/script mutation is implemented yet. |
| `DespawnUnit` | Conservative | Delayed non-player map removal; non-zero payload modes remain. |
| `Disembark` | Conservative | Resolves a player target/owner, safely dismounts mounted players through the existing vehicle path, and emits `CombatLogMount`; `DataBits00` reason/mode values remain unnamed. |
| `Activate` | Partial | Quest activation objective updates, world-target `Activate`/`Fluff` execution, and `ClientActivateUnitCast` spell resolution now work for object interactions; CSI/script/visibility payload behavior remains. |
| `Stealth` / `RemoveStealth` / `AggroImmune` | Conservative | Local state, timed removal, stealth logs, and attackability gate; visibility planes and exact immunity behavior remain. |
| `Teleport`, housing, support-stuck, and unlock/pet/title handlers | Narrow | Existing `WorldLocation2` teleport behavior, guarded housing recall/escape through the residence map-lock path, `/stuck` suicide through normal death handling, plus guarded dye, mount, pet flair, vanity pet, title grant, and title revoke paths. Vanity pet unlock now sends `ServerUnlockVanityPet`; duplicate/unknown rows are safe no-ops with diagnostics. |

## High-Priority Remaining Families

| Priority | Family / Area | Why It Matters | Next Action |
| ---: | --- | --- | --- |
| 1 | Target mechanics and valid target masks | Affects every family and prevents wrong target application. | Decode remaining non-dead valid-target categories and broader world-target effect families beyond the now-wired `0x02` activate slice, then continue on groups and AOE prerequisites. |
| 2 | `Proc` validation | Conservative runtime now covers 1,747 rows across 1,486 spells, but retail parity still needs proving. | Validate kill, enter-combat, cast, damage, receive-damage, and heal-other fixtures against `proc-probe` and `proc-dispatch` traces; keep unsupported target-data tails diagnostic-only until new evidence lands. |
| 3 | `RavelSignal` | 5,262 rows; high row count and likely content scripting. | Structural diagnostics are implemented; next blocker is decoding the receiver/script graph that consumes mode/signal/payload values. |
| 4 | Shield/transference families | Smaller but clear formula semantics. | Validate `HealShields`, `DamageShields`, and `Transference` with shield pot, Warrior/rune, and life-drain fixtures. |
| 5 | `SapVital` validation | Percent-of-max vital restore/drain rows are now wired conservatively, and health-vital drains preserve source/damage-type context without guessing on zero-valued rows. | Validate level-up/medkit restore rows, self-destruct/damage rows, and resource-drain rows, with extra attention to whether the client emits additional normal damage/result data beyond `CombatLogVitalModifier`, before widening parameter, secondary-payload, or flat/high-scalar behavior. |
| 6 | `ClampVital` validation | Health-ceiling rows are now wired from a compact ratio set. | Validate Laveka/Starmap/CQ Treasure clamp fixtures against observed HP caps, duration cleanup, and mode/vital bits. |
| 7 | Damage/heal multiplier validation | Personal damage and heal modifiers now feed the shared property and calculator path. | Validate `PersonalDmgHealMod` with Analyze Weakness, Nano Spike, Unsteady Miasma, and Sigil/Fusion Probe fixtures, including outgoing, incoming, and duration cleanup behavior. |
| 8 | `ShieldOverload` validation | Shield shutdown has a simple dominant shape but needs packet/sniff parity. | Validate shield drop, regeneration suppression, buff removal, and non-zero payload diagnostics. |
| 9 | `UnitStateSet` validation | Broad state latch family now has a conservative hostile-immunity shell, but state `1` and packet parity still need proof. | Validate state `1` block rows, confirm client/combat-log parity and any beneficial-only exceptions for states `6/7/8/9/13/14/15/16/17/18/22/23`, and keep remaining state ids evidence-only until new witnesses land. |
| 10 | `SetBusy` validation | Activation/object busy state now drives visible in-use state and conservative direct activate/interact rejection. | Validate instant set plus delayed clear rows, duration-backed rows, `ServerUnitInUse` parity, non-zero context ids, and whether CSI/deferred interaction paths need the same gate. |
| 11 | `Absorption` / `HealingAbsorption` validation | Duration-backed defensive anti-damage and anti-heal pools are now wired conservatively. | Validate absorb amount, consumption order, `DataBits04` damage-type behavior, healing-absorb combat logs, and whether shield heals should be affected. |
| 12 | Threat modification validation | Encounter aggro control now has a conservative runtime shell. | Validate drop/wipe/set/add/fixate modes with known dungeon/raid fixtures. |
| 13 | `ModifyInterruptArmor` validation | Small, clear CC-adjacent family now wired conservatively. | Validate duration cleanup and `DataBits01` variants such as `43457`, `42697`, and `42698`. |
| 14 | `SpellDispel` validation | Buff/debuff cleanup is now structurally wired but depends on aura tracking and class semantics. | Validate Purify/Sterilize/Purge-style rows and confirm `DataBits00/01/05` count/priority behavior. |
| 15 | Cooldown and charge validation | Player-facing ability reset, cooldown modify, and charge mechanics now have conservative handlers. | Validate `CooldownReset`, `ModifySpellCooldown`, `ActivateSpellCooldown`, and `ModifyAbilityCharges` against known charged/cooldown spells. |
| 16 | Immunity validation | Effect-type and concrete-spell immunity are now central and can block later effects. | Validate `SpellEffectImmunity` with CC/damage immunity fixtures and `SpellImmunity` mode `0` rows such as `46559`, `46927`, `48020`, and `31074`; keep modes `1` and `2` evidence-only. |
| 17 | Scale/faction/visual/reward utility validation | Several small families are now wired but need packet/sniff checks. | Validate `Scale`, `FactionSet`, `ItemVisualSwap`, `DisguiseOutfit`, `MimicDisguise`, `QuestAdvanceObjective`, `AchievementAdvance`, `ReputationModify`, `GiveItemToPlayer`, `GiveSchematic`, `RewardPropertyModifier`, `AddSpell`, `GrantXP`, `PathXpModify`, `GrantLevelScaledXP`, `GiveAugmentPowerToPlayer`, and `Kill` fixtures. |
| 18 | Stack groups and broader persistence parity | Needed for remaining buff/debuff correctness after the effect-instance lifecycle pass. | Map `Spell4StackGroup` arbitration and widen persistence beyond direct-player prerequisite evaluation. |
| 19 | Advanced CC and movement | Combat feel and encounter mechanics. | Validate with sniffable CC/facing/forced-move fixtures. |
| 20 | Summon ownership/AI/turrets/services | Needed for encounter adds, traps, settlers, turrets, vendors, and vehicles. | Add ownership tracking once an AI/controller model is identified, validate `SummonTrap` trigger behavior, and validate `SummonVehicle` auto-board/deployable rows. |
| 21 | Runners, cast events, thresholds | Spell graph behavior beyond plain effect rows. | Build graph query and implement only proven edge types. |
| 22 | `ModifyCreatureFlags` | Small but visible in placed context. | Evidence-only for now; payload does not safely map to current 8-bit create flags. |

## Evidence-Only Boundaries Rechecked

These families were rechecked against the imported `Spell4Effects` data and local runtime surfaces on 2026-05-14. They are intentionally not implemented yet because the data does not identify a safe mutation target in the current server, or because applying the obvious-looking payload would likely be wrong.

| Family / Area | Evidence | Boundary |
| --- | --- | --- |
| `UnlockActionBar` | Four class unlock rows encode action-set indices `0..3`. | Current action-set model exposes four sets by default and has no locked/unlocked state to mutate. |
| `GiveAbilityPointsToPlayer` | One row grants amount `1`. | Ability/tier points are currently fixed by `ActionSet.MaxTierPoints`; no bonus-point storage or packet model is present. |
| `UnlockInlaidAugment` | 281 clean rows encode AMP/inlaid augment ids. | AMP unlock persistence/storage is missing; applying `AddAmp` would equip an AMP, not unlock it. |
| `RestedXpDecorBonus` / `ModifyRestedXP` | Payloads are float-bitcast rest-XP multipliers/fill markers. | `XpManager` only stores rest XP and computes login accrual; decor bonus lifetime and fill semantics need modeling before mutation. |
| `RewardBuffModifier` | Three daily reward buff rows with duration and 1.5x-style floats. | No reward-buff runtime surface is present beyond account reward properties. |
| `ChangeDisplayName` / `MimicDisplayName` | Payloads point at localized text/name mode data. | World entities do not expose a temporary display-name/nameplate mutation surface. |
| `SummonPet` / `PetCastSpell` | Payloads reference creature and spell ids for combat/class pets. | Vanity pets exist, but combat pet ownership, stance, AI, and lifetime state are not restored. |
| `ChangePhase` / `ChangePlane` | Bitmask-like payloads and duration rows exist. | Phase/plane visibility is player/world-state sensitive and lacks a general spell-owned state manager. |
| `ModifyCreatureFlags` | Add/remove-looking rows use values far beyond the current create-flag enum and include names like selection/nameplate/cinematic states. | Needs client/sniff validation before mutating `CreateFlags`; several values would be misnamed if treated as existing enum bits. |
| `Hazard*`, `FacilityModification`, `Script` plus `RavelSignal` receiver behavior | High row counts or clear content-scripting intent. | Require script/hazard/facility managers or receiver graph decoding before safe runtime behavior. |

## Verification Loop

1. Query representative fixtures with `Tools/SpellReverseEngineering/world_context_queries.sql`.
2. Inspect concrete spells with `/spell inspect4 <spell4Id>`.
3. Cast with `/spell cast4 <spell4Id>` against a controlled caster/target.
4. Compare diagnostics, outgoing packets, combat logs, entity state, and map changes.
5. Promote a field mapping only when SQL shape, runtime behavior, and sniff/client evidence agree.

## Last Verified Build

`dotnet build Source\NexusForever.Game\NexusForever.Game.csproj` and `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj` passed on 2026-05-15.

Known warnings / environment notes:

- Existing `Spline.formation` CS0649 warning in `NexusForever.Game`.
- Full solution builds are currently noisy/failing when running server processes lock output DLLs, including observed `GroupServer` and `ChatServer` copy targets.
