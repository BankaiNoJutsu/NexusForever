# Combat Profile Audit

Audits committed `CombatAI` profile coverage against client game tables without
querying authoring-only reference databases.

Run directly against a local WorldServer config to infer both game tables and
runtime world spawn context:

```powershell
dotnet run --project Tools\CombatProfileAudit\CombatProfileAudit.csproj -- `
  --world-config Source\NexusForever.WorldServer\bin\Debug\net10.0\WorldServer.json `
  --output-dir artifacts\combat-profile-audit
```

Run with only game tables when no runtime world database is available:

```powershell
dotnet run --project Tools\CombatProfileAudit\CombatProfileAudit.csproj -- `
  "D:\Games\WildStar\Patch\ClientData" `
  --output-dir artifacts\combat-profile-audit
```

The command writes:

- `combat-profile-audit.md`
- `combat-profile-creatures.csv`
- `combat-profile-actions.csv`
- `combat-profile-spawn-priority.csv`
- `combat-kit-group-candidates.csv`
- `combat-action-rule-candidates.csv`
- `combat-signal-blockers.csv`

`combat-profile-spawn-priority.csv` is populated only when `--world-config` or
`--world-connection` supplies runtime-owned `nexus_forever_world` access. Spawn
context is used only to prioritize review of unmapped creatures; it does not
activate combat from unknown `Creature2Action` rows.

The markdown report includes a broad spawned-unmapped queue plus a narrower
spawned combat-signal queue. The combat-signal queue is the best next review
list because it highlights runtime-spawned creatures with action sets, unknown
action rows, visual action rows, aggro sounds, or combat-loop evidence.

When `Tools\DataMapping\output\creature_spell_map.csv` exists, or when
`--creature-spell-map <path>` is supplied, the report also emits
`combat-kit-group-candidates.csv`. That file groups reviewed/unique spell-map
rows by exact Spell4 signature plus `UnitRaceId`, `Creature2FamilyId`,
`Creature2TractId`, `Creature2AffiliationId`, `FactionId`, and
`Creature2ActionSetId`. These are review suggestions only; runtime combat still
requires explicit `CombatKits.json` promotion. The CSV includes full candidate
creature lists plus the narrower `unmapped_combat_signal_*` columns used to
promote runtime-spawned combat-signal rows without relying on truncated report
samples.

`combat-action-rule-candidates.csv` clusters unknown/rejected/missing
`Creature2Action` rows by exact `state,event,action`. It reports runtime spawn
impact, action-data shape, valid `Spell4` collisions, and overlap with reviewed
creature spell-map signatures. These clusters are an evidence funnel for future
`CombatActionRules.json` entries; they never generate or activate rules by
themselves.

`combat-signal-blockers.csv` classifies remaining runtime-spawned combat-signal
unmapped creatures after reviewed kit promotion. A blocked row documents why the
current evidence cannot safely activate runtime combat, such as visual-only
actions, unproven action semantics, sound-only signals, or ambiguous reviewed
spell bridges that look like fixtures or scripted encounter roles.

The output is generated evidence for review and should stay under `artifacts/`
unless a specific snapshot is intentionally promoted.
