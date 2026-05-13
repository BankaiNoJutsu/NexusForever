# Spell Effect Evidence Matrix

This matrix tracks effect-family decoding evidence. It is intentionally family-first: each row should accumulate representative spell IDs, sniff observations, client-decompile hints, local runtime output, known field mappings, unknowns, and validation status.

## Field Legend

- Raw row: `Spell4EffectsEntry`
- Core identity: `Id`, `SpellId`, `OrderIndex`, `EffectType`, `TargetFlags`, `DamageType`
- Timing: `DelayTime`, `TickTime`, `DurationTime`
- Opaque fields: `DataBits00` through `DataBits09`
- Parameter vectors: `ParameterType[0..3]`, `ParameterValue[0..3]`
- Apply/persistence hooks: `PrerequisiteIdCasterApply`, `PrerequisiteIdTargetApply`, `PrerequisiteIdCasterPersistence`, `PrerequisiteIdTargetPersistence`, `PrerequisiteIdTargetSuspend`

## Priority Families

| Effect family | Representative spells | Known field mappings | Unknowns to resolve | Server support | Evidence status |
| --- | --- | --- | --- | --- | --- |
| `Damage`, `Heal`, `DistanceDependentDamage`, `DistributedDamage`, `HealShields`, `DamageShields` | Fill from `/spell inspect <baseId> [tier]` and retail sniff IDs. | `DataBits00` is interpreted as a float type multiplier. `DataBits01` is interpreted as a base value. `ParameterType` and `ParameterValue` are interpreted as coefficient pairs for caster stats, caster power, target/caster vitals, level, and item budget. | Exact retail rounding order, variance source, item budget semantics, weapon and weapon DPS semantics, combat-result side effects, distance/distribution/shield family deltas. | Central damage path now reads through `SpellEffectInterpreter`; diagnostics emit raw and decoded inputs. | Partially implemented from existing server behavior; needs sniff validation. |
| `Transference` | Fill from sniff/local cast cases. | `DataBits02` is interpreted as a float type multiplier. `DataBits03` is interpreted as a base value. Parameter vectors share damage-family interpretation. | Whether transfer source/destination vitals require separate effect result packets or combat logs. | Central damage-family decoder has a transference branch; handler support still needs family validation. | Decoder seeded; behavior incomplete. |
| `UnitPropertyModifier` | Fill from buff, debuff, reward-buff, and item-like examples. | `DataBits00` maps to `Property`. `DataBits01` is preserved as priority and exposed as a possible `ModType` hint. `DataBits02` is interpreted as percentage value, `DataBits03` as flat value, `DataBits04` as level-scale value. | Whether `DataBits01` is always priority, mod type, or family-dependent ordering; stack behavior; duration removal; prerequisite persistence. | Existing property modifier handler now reads named semantics through the interpreter. | Partially implemented; duration/removal remains open. |
| `Proxy`, `ProxyLinearAE`, `ProxyChannel`, `ProxyChannelVariableTime`, `ProxyRandomExclusive` | Fill from chained spell examples and sniffs. | `DataBits00` maps to chained `Spell4Id` for proxy-style effects. | Proxy target forwarding rules, delay/channel timing, random-exclusive selection rules, chained spell packet parent/root behavior. | `Proxy` handler now uses decoded `Spell4Id`; other proxy families are decoded but not all have handlers. | Decoder seeded; only `Proxy` is behavior-backed locally. |
| `Teleport` | Fill from taxi, bind, zone transition, and scripted teleport examples. | `DataBits00` maps to `WorldLocation2Id` for `Teleport`. | Family differences for `HousingTeleport`, `WarplotTeleport`, `GoMap`, `ReturnMap`, and phase/facing semantics. | `Teleport` handler now uses decoded `WorldLocation2Id`; rapid transport remains separate. | Partially implemented; non-`Teleport` movement families not decoded. |

## Runtime Evidence Loop

1. Use `/spell inspect <baseId> [tier]` to capture base metadata, effects, raw `DataBits`, parameter vectors, and current decoded semantics.
2. Use `/spell cast <baseId> [tier]` locally with trace logging enabled.
3. Compare `SpellDiagnostics effect-dispatch`, `damage-input`, `damage-output`, `spell-go`, and `packet-boundary` trace rows against sniff packets and combat logs.
4. Update this matrix with representative spell IDs and promote a field mapping from hypothesis to known only when local behavior, sniff-visible behavior, and client structure agree.

## Regression Fixtures To Add

- One direct damage spell with non-zero stat or power coefficients.
- One damage-family spell with non-zero target/caster vital coefficient.
- One `UnitPropertyModifier` buff or debuff with duration and visible stat change.
- One direct `Proxy` chain where parent/root spell IDs are visible in outgoing spell packets.
- One `Teleport` spell with known `WorldLocation2` destination.
