# Learned passives and housing recall upstream fixes

Implemented on 2026-10-29 in `I:\GIT\NexusForever-assessment`, after taxi/mount
commit `d2d9c85a394e69ec879ceac616cccf118efcea07`. Upstream was refreshed and
`game_rework` remains `1f643f6374ec017ae46ac3bd72ad99d42c902d46`.

This slice reconciles the next remaining gaps from the assessment:

| Source | Reviewed head | Fork change |
| --- | --- | --- |
| [PR #605](https://github.com/NexusForever/NexusForever/pull/605) | `972ad3b4f9c7bce0a460ad0e5b5410f311b8573c` | Preserve AddSpell's actual tier, apply learned permanent property passives and restore them after world entry. |
| [PR #603](https://github.com/NexusForever/NexusForever/pull/603) | `6faa74d029f95b5e29684690ee2628c10dc80b59` | Teach Recall House to existing residence owners and after successful residence creation. |

## Learned passive behavior

The fork already implements AddSpell and Disembark, with richer spell
interpretation, diagnostics, combat logs and effect lifetime handling than the
upstream patch. Those paths are retained. AddSpell now learns the target entry's
tier, normalizing zero to one and rejecting an invalid base id or a tier that
cannot fit the existing byte API. A known spell is not inserted again or
silently upgraded.

After AddSpell, the spell manager can apply the learned passive to its owner.
World-entry packets also trigger replay of known active learned passives.
Classification requires a nonempty set of UnitPropertyModifier effects and an
AddSpell teaching row. Spells/effects with finite durations, delayed effects,
periodic effects and mixed effect families are excluded from automatic replay.
This narrower classification preserves the fork's timed spell semantics.

The classification cache belongs to the actual effects-table instance, using a
weak reference key. Missing tables do not create a process-wide empty cache;
another table load cannot inherit a previous runtime's classification.

Application goes through the existing TryCastSpell path with explicit self
targeting, UserInitiatedSpellCast=false and IgnoreGlobalCooldown=true. Other
cast checks remain in force. Automatic application does not cancel trading.
The caller must own the active spell at the corresponding tier. Inactive,
unowned or unsupported spells are not replayed.

Dead/out-of-world login and blocked casts leave the eligible passive pending.
Spell-manager updates retry pending spells at bounded intervals after the
player is alive and in-world. A successful cast clears its retry. Disabling a
learned passive removes its owned property modifiers and cancels its retry;
re-enabling applies it again.

Repeated world entry uses the existing property modifier ownership replacement
by Spell4 id and effect-entry id. No second speed bonus accumulates, and an old
effect id cannot remove the newer replacement. No new property persistence or
alternative spell engine is introduced.

## Housing recall

ResidenceManager receives the player's existing global spell manager. It
teaches Recall House (base 22919) on loading an owned residence and after
GetOrCreateResidence succeeds. It verifies ownership, available base/tier data
and whether the spell is already known. Missing spell data does not break
housing initialization. Successful repeated residence access does not grant a
duplicate spell.

The existing retail housing unlock check runs before creation. Existing
HousingTeleport/HousingEscape, parent-community map locks, entrance positions
and facing, pending-transfer checks, and stuck-window recovery are retained.
Regression cases exercise the existing teleport path as well as the new
teaching hooks.

## Preserved prerequisite and entitlement policy

[PR #600](https://github.com/NexusForever/NexusForever/pull/600), reviewed at
`1bf8de1caba3baa70620998b82317d65c96b07c8`, is not imported. Its type-269
UnhealthyTime assumption conflicts with this fork's client-evidence-backed
RapidTransport cast-context check. Its Dueling implementation assumes duels do
not exist, whereas the fork has pending/countdown/active duel sessions. Adding
an always-nondueling result would discard that runtime behavior; a compatible
type-204 check requires its exact table/comparison contract to be established
separately. No prerequisite enum or comparison mapping is changed here.

Account/character entitlement defaults, scope rules, reward modifiers and
storefront/account-item grant behavior are unchanged. No database model,
migration or entitlement row is added in this slice.

## Verification

Forty additional regression cases extend the 5,924-case taxi/mount baseline:

- Twenty-four learned-passive cases cover classification, missing/reloaded
  tables, public world-entry replay against real property-modifier handling,
  effect ownership, self/cooldown flags, inactive/unowned spells, recovery and
  bounded retries, activation/deactivation, and SQLite spell persistence.
- Six AddSpell cases cover tier normalization, tier preservation, duplicate
  ownership, and invalid base/tier rejection.
- Seven residence-manager cases cover owner-only teaching, known spells,
  missing base/tier data, new residence creation and the existing unlock guard.
- Three housing-effect cases preserve entrance/parent-instance routing,
  pending-transfer rejection and missing-entrance handling.

The first full run exposed a cooldown-test fixture that constructs an
uninitialized SpellManager. Its pending-passive set is now initialized alongside
the existing cooldown/spell collections; production guards were not weakened.
The focused housing fixture was also corrected to use the actual residence
map-lock interface.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
git diff --check
```

The final focused slice passes 320 tests, including the existing duel,
prerequisite, cooldown, stuck recovery and spell-grant tests. The full solution
build passes with zero errors and 114 existing warnings. All **5,964 tests
pass**, with zero failures and zero skips. Full build/test
logs and TRX results are saved outside the checkout at
`I:\GIT\NexusForever-assessment-report\learned-passives-housing`.

Tests exercise managed behavior and relational SQLite spell persistence. Live
client gameplay, MySQL integration and broker traffic were not run. No new
migration is required for this slice.
