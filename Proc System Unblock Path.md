# Proc System Unblock Path

Date: 2026-05-14

The proc system is structurally decoded and registered, but real proc firing is intentionally blocked until we have observable runtime evidence for event routing.

## Goal

Capture enough evidence to safely implement proc event dispatch without guessing:

- trigger event enum meaning from `Proc.DataBits00`
- trigger spell caster and target routing from `Proc.DataBits03`
- chance roll timing from `Proc.DataBits02`
- internal cooldown behavior from `Proc.DataBits04`
- recursion prevention and ordering around damage, heal, kill, and combat logs

## Enable Trace Logging

WorldServer should emit `SpellDiagnostics` trace rows. If trace output is missing, edit:

`Source/NexusForever.WorldServer/nlog.config`

Set the rule to:

```xml
<logger name="*" minlevel="Trace" writeTo="console"/>
```

Then start the server:

```powershell
dotnet run --project Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-build
```

## Validate Proc Registration

These commands should show decoded proc semantics and `SpellDiagnostics proc ... applied=True` when cast:

```text
/spell inspect4 7116
/spell cast4 7116
/spell inspect4 4046
/spell cast4 4046
```

## Minimum Evidence Matrix

| Proc holder | Trigger to observe | Event evidence |
| --- | --- | --- |
| `Spell4=7116` Momentum | Kill target | `DataBits00=1`, likely on kill |
| `Spell4=4876` Readiness | Enter combat | `DataBits00=6`, likely enter combat |
| `Spell4=4046` Brutal | Deal damage | `DataBits00=12`, likely on hit/deal damage |
| `Spell4=1024` Confrontation | Receive damage | `DataBits00=16`, likely get damaged any |
| Heal-other proc from event `20` rows | Heal another unit | `DataBits00=20`, likely on heal other |

For each capture, record:

- proc holder `Spell4`
- triggering action
- trigger spell `Spell4`
- trigger spell caster
- trigger spell primary target
- whether it fired before or after damage/heal/death combat logs
- whether chance rolled per hit, per target, per effect row, or per spell cast
- whether cooldown starts on successful proc or any eligible event
- whether a proc-trigger spell can trigger another proc

## Fastest Unblock

A single reliable capture for either of these is enough to implement the first conservative dispatcher:

- `Spell4=4046` Brutal: damage event `12`, trigger spell `4047`
- `Spell4=7116` Momentum: kill event `1`, trigger spell `7117`

The key evidence needed is trigger spell caster, trigger spell target, and ordering relative to the original event.

## Suggested Next Code Step

Implemented on 2026-05-14: a diagnostic-only `proc-probe` trace now logs active proc states when these events happen:

- spell cast
- damage dealt
- damage received
- heal cast/applied
- target killed
- enter combat

It does not cast trigger spells yet. The purpose is to compare local event context against captured retail/sniff/client behavior before mutating gameplay.

Current candidate labels are source-controlled in:

- `Source/NexusForever.Game.Static/Spell/ProcTriggerEventCandidate.cs`

Current probe hook points:

- `spell-cast` before local cast execution
- `enter-combat` after combat state flips on
- `damage-dealt` / `damage-received` after damage calculation and before health application
- `shield-damage-dealt` / `shield-damage-received` after shield-damage calculation
- `heal-other` / `heal-self` and shield-heal equivalents after heal calculation and before application
- `target-killed` after normal damage, `Kill`, or support-stuck death application

Each `SpellDiagnostics proc-probe` row includes the proc holder, source/target, observed event candidate, whether it matches the active proc state's trigger event, trigger spell/casting/effect context, damage or heal amounts when available, and the raw proc fields.

## Next Evidence Step

Capture `proc` registration plus `proc-probe` rows for:

```text
/spell inspect4 4046
/spell cast4 4046
```

Then deal damage with a simple known hit and compare whether event `12` rows line up with source, target, timing, and trigger spell routing. Repeat with `7116` for event `1` kill evidence.
