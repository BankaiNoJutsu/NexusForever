# Proc System Unblock Path

Date: 2026-05-14

The proc system is structurally decoded and now has a conservative holder-side runtime for the dominant event and routing shapes. Unsupported tails still stay diagnostic-only until we have stronger evidence.

## Goal

Validate and widen proc event dispatch without guessing:

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

In game, the WorldServer command prefix is `!`, not `/`. These commands should
show decoded proc semantics and `SpellDiagnostics proc ... applied=True` when
cast:

Prerequisite: these commands require RBAC access. The setup scripts now create
`gm` / `gm` with the `GameMaster` role and `admin` / `admin` with the
`Administrator` role for command-driven testing. If you log in with
`player` / `player`, chat still returns `Unable to invoke command, it's either
an invalid command or you don't have permission to access it!` because the
`Player` role does not include `Permission.Spell` / `Permission.SpellCast`.
Fresh accounts created outside the setup scripts also fall back to `Player`
until you add the right role or explicit permissions in `nexus_forever_auth`,
then relog before retrying.

```text
!spell inspect4 7116
!spell cast4 7116
!spell inspect4 4046
!spell cast4 4046
```

When the question depends on the real client-originated trigger spell rather than the command-driven proc holder application, arm the next live request first:

```text
!spell capturenext
```

Then cast the actual hotbar ability, activate-unit interaction, item use, or rapid-transport request that should trigger the proc. This captures the real client spell entry path and preserves the client context token plus request source in the exported runtime artifact while the proc traces still land in `SpellDiagnostics`.

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

If the trigger action itself needs a structured JSON artifact, use `!spell capturenext` immediately before the real client action instead of relying only on `!spell cast4`, because `!spell cast4` bypasses the client packet handlers and will not exercise the preserved client context fields.

## Current Runtime

Implemented on 2026-05-15: proc dispatch now fires conservative holder-side trigger casts when these observed events match the stored proc trigger event:

- spell cast
- damage dealt
- damage received
- heal cast/applied
- target killed
- enter combat

Current conservative routing rules are:

- trigger events `1`, `6`, `10`, `12`, `16`, and `20` are supported
- `targetData` `1`, `2`, and `9` target the proc holder
- `targetData` `4` and `12` target the counterpart unit in the observed event
- cooldown from `DataBits04` is consumed only when the trigger cast queues successfully
- same-chain reentry is blocked per proc effect id

Unsupported trigger events and unsupported `targetData` tails still do not cast trigger spells.

Current candidate labels are source-controlled in:

- `Source/NexusForever.Game.Static/Spell/ProcTriggerEventCandidate.cs`

Current probe hook points:

- `spell-cast` before local cast execution
- `enter-combat` after combat state flips on
- `damage-dealt` / `damage-received` after damage calculation and before health application
- `shield-damage-dealt` / `shield-damage-received` after shield-damage calculation
- `heal-other` / `heal-self` and shield-heal equivalents after heal calculation and before application
- `target-killed` after normal damage, `Kill`, or support-stuck death application

Each `SpellDiagnostics proc-probe` row includes the proc holder, source/target, observed event candidate, whether it matches the active proc state's trigger event, trigger spell/casting/effect context, damage or heal amounts when available, and the raw proc fields. `SpellDiagnostics proc-dispatch` now records the resolved target, cooldown state, cast action, and skipped reason for matched proc states.

## Next Evidence Step

Capture `proc` registration plus `proc-probe` and `proc-dispatch` rows for:

```text
!spell inspect4 4046
!spell cast4 4046
!spell capturenext
```

Then deal damage with a simple known hotbar hit and compare whether event `12` rows line up with source, target, timing, and trigger spell routing. Keep the exported JSON path alongside the trace rows when the real client trigger spell is part of the question. Repeat with `7116` for event `1` kill evidence, `4876` for enter-combat, and one heal-other fixture for event `20`. Unsupported target-data tails such as `20`, `14`, `18`, `33`, `34`, and `36` should stay trace-only until their routing is proven.
