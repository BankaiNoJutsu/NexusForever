# Spell-Adjacent Blocker Unblock Plan

Last updated: 2026-06-20 CEST

## Purpose

This plan targets the spell-adjacent blockers currently present in
`Decomp/Analysis/coverage/content_retail_completeness_next_slice_blockers.csv`.
It complements `SPELL_CREATURE_REMAINING_IMPLEMENTATION_PLAN.md` by focusing on
the current next-slice queue, not the full spell backlog.

The main rule is to unblock rows through the smallest evidence path that is
actually needed. Most current rows need table/script/client smoke evidence, not
new spell auxiliary packet semantics.

## Current Queue Facts

The generated next-slice queue has no first-class spell blocker category and no
spell-specific coverage CSV. Filtering the direct blocker fields for spell
language gives 71 spell-adjacent rows:

| Bucket | Count |
| --- | ---: |
| `instance_dependency` | 58 |
| `quest_objective` | 4 |
| `quest_script` | 4 |
| `quest_world_dependency` | 3 |
| `script_objective_producer` | 2 |

Those 71 rows are concentrated in:

| Slice | Direct spell-adjacent rows | Queue state |
| --- | ---: | --- |
| `instance-3009` Vault of the Archon | 48 | split-required, high blocker volume |
| `instance-382` Stormtalon's Lair | 11 | split-required, high blocker volume |
| `quest-3886` Fiery Distraction | 3 | quest validation ready |
| `quest-5573` Powering Down | 3 | quest validation ready |
| `quest-5580` Enforced Radio Silence | 3 | quest validation ready |
| `quest-5575` Seizing Power | 2 | quest validation ready |
| `instance-1263` Skullcano | 1 | split-required, high blocker volume |

Do not count rows as spell implementation work merely because their
`evidence_sources` mention `SpellEffectHandler`. For planning, use direct fields
such as `evidence_status`, `blocker_summary`, `required_validation`, and
`manual_validation`.

## Workstream A: Quest Activation Smoke

Start with the quest rows in generated queue order. These are validation-ready
and already have server-side runtime evidence. The likely unblocker is live
build 16042 smoke proving that the generic interaction or activate-spell path
matches the client-visible objective.

Process one row per pass, then regenerate/re-read the queue.

1. `quest-3886` Fiery Distraction
   - Focus: Skeech Hut `ActivateTargetGroupChecklist` objective `5052`,
     Burning Torch CSI/objective context, Durek dialog, reward and achievement
     UI, Q3886 -> Q3673 handoff.
   - Command:

     ```powershell
     dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q3886"
     ```

2. `quest-5573` Powering Down
   - Focus: Power Regulator `ActivateTargetGroupChecklist` objective `8229`,
     hidden completion objective `12870`, cinematic timing, Q5573 -> Q5596
     branch smoke.
   - Command:

     ```powershell
     dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5573"
     ```

3. `quest-5575` Seizing Power
   - Focus: shared Power Regulator objective `8371`, hidden objective `12871`,
     Q5575 -> Q5596 branch smoke.
   - Command:

     ```powershell
     dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5575"
     ```

4. `quest-5580` Enforced Radio Silence
   - Focus: Tower Controls `ActivateTargetGroupChecklist` objective `8227`,
     active prop visual state, Kezrek dialog, Q5594 merge smoke.
   - Command:

     ```powershell
     dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q5580"
     ```

Evidence bundle requirements for each quest:

- Client screenshot/video of the interact or activate prompt.
- Server Trace logs around activation, objective credit, completion, reward, and
  follow-up.
- Client-visible objective tracker change.
- Visual state check after activation, including despawn/respawn when relevant.
- Negative cases: missing quest, wrong objective/world, repeat interaction after
  completion.
- Reward UI and achievement UI when the row names them.

Stop as blocked if the client uses an unmodeled packet, if the target cannot be
reached in build 16042, or if the live client shows different objective binding
than the current reviewed table/script evidence.

## Workstream B: Instance Spell-Mechanic Sub-Slices

Do not attempt whole instances. Each pass must choose one public-event objective
or one mechanics row from a split-required instance.

### Vault of the Archon

First target:

- `instance-3009`, blocker rank `184`,
  `runtime_vault_archon_forcefield_power_link_direct_script_credit_tested_pending_construct_spell_portal_client_smoke`.
- Objective: prove or block Forcefield Power Link / objective `4327`, construct
  interaction, spell `83784` gating, jump-through or portal behavior, repeated
  and negative cases.
- Start with:

  ```powershell
  dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~NexusForever.Game.Tests.Instances.HallOfTheHundredEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.InstanceMapBindingTests"
  ```

If the existing server tests already prove direct objective credit, the next
unblocker is client smoke for spell/portal/construct behavior. If the client
requires unmodeled spell or portal semantics, stop and route that exact spell or
packet question to Workstream C.

### Stormtalon's Lair

First target:

- `instance-382`, blocker rank `90`,
  `runtime_stormtalon_grenade_disable_objective_activation_tested_pending_grenade_mechanics_and_client_smoke`.

Then handle the mapped challenge rows one by one:

- Aethros tornado bonus.
- Stormchaser challenge.
- Lightning Reflexes / lightning strike hit tracking.

Do not fold the matching-map, achievement, or full dungeon smoke rows into the
spell pass unless the selected mechanics row depends on them.

### Skullcano

First target:

- `instance-1263`, blocker rank `28`,
  `mapped_skullcano_thunderfoot_seismic_challenge_blocked_missing_seismic_tremor_hit_tracking_producer_client_smoke`.

This is not implementation-ready yet. The first pass must identify the
Seismic Tremor Spell4/effect ids, hit-tracking producer, fail-state timing, and
achievement/reward side effects. If those cannot be proven from table/script
data and focused runtime traces, leave it mapped-only and capture the blocker.

## Workstream C: Native Or Protocol Evidence Only When Needed

Use decompile/debugger work only when a selected row proves that table/script
evidence is insufficient. Candidate questions:

- Does the selected objective emit or consume `ServerSpellUInt32TripletList`
  `0x080F`, `ServerSpellUInt32TripletListVariant` `0x0810`, or
  `ServerSpellFourUInt32` `0x0812`?
- Is a missing visual/cooldown/buff/client-state update actually a spell aux
  packet, or just an unimplemented content script state?
- Which Spell4Effects payload fields are consumed by the client for the selected
  mechanic?

Required sequence:

1. Search existing cached exports and labels first.
2. Use focused export refresh only if cache evidence is missing.
3. For live debugger work, confirm the bridge status endpoint before attach.
4. Require an opcode-specific producer/apply owner or live trace witness before
   renaming fields or adding runtime emitters.

Useful commands:

```powershell
rg -n "0x080F|0x0810|0x0812|83784|Seismic|Tremor|Spell4Effects" Decomp\Analysis Source
.\Decomp\Analysis\Get-GhidraMcpWorkflowHints.ps1 -Targets WildStar64.exe
.\Decomp\Analysis\Start-GhidraDebuggerBridge.ps1 -GhidraRoot I:\ghidra_12.1_PUBLIC
Invoke-WebRequest http://127.0.0.1:8099/debugger/status -UseBasicParsing
```

Stop as blocked if the evidence is only a shared reader shape, registration
literal, or table row without a producer/apply path.

## Workstream D: Regeneration And Tracker Closure

After any implementation, evidence-status, or tracker-generator change:

```powershell
python Tools\WikiArchiveAudit\content_retail_completeness_audit.py --only next-slice
python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py
```

If the touched source inventory is not refreshed by `next-slice`, regenerate the
specific output by filename or run `--only all-csv` before validating.

Do not change `completion_status` or claim retail completion unless the
generated outputs and validator allow it. The expected intermediate result for
most of these rows is still `not_retail_complete`, with blocker text narrowed
from generic spell/client smoke to the exact remaining evidence gap.

## Recommended First Pass

Run the first pass on `quest-3886` because it is the earliest queue row with
direct spell-adjacent activation text and it is validation-ready rather than
split-required.

Done when:

- Focused Q3886 tests pass or the failure is recorded.
- A blocker bundle exists for the exact Durek/Torch/Skeech Hut activation smoke
  target.
- The evidence records whether generic interaction/activate-spell behavior
  matches the client-visible objective.
- `next-slice` outputs are regenerated and validated if any source or generator
  evidence changes.
- The final state is implemented, mapped-only, rejected, or blocked with the
  missing evidence named.
