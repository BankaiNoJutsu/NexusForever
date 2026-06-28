# Spell And Creature Remaining Implementation Plan

Last updated: 2026-06-17

## Purpose

This plan describes how to unblock and implement the remaining spell and creature
behavior work without broad guessing. It is scoped to retail-parity gaps around
spell effects, spell auxiliary packets, creature AI, creature data promotion,
public-event creatures, quest creatures, and instance entity behavior.

The plan follows the repository evidence rules:

- Runtime code must not query `wildstar_client`, `jabbithole`, or `nf_map_*`
  staging/reference tables.
- DataMapping output is authoring evidence until reviewed and promoted into
  runtime-owned tables, seed SQL, scripts, or code-owned assets.
- Decompiled client evidence may guide behavior, but decompiled source must not
  be copied into the repository.
- Unknown packet fields, spell data bits, and state flags remain diagnostic-only
  until their producer/consumer semantics are proven.
- Every closure pass must end as implemented with verification, mapped-only with
  an explicit blocker, or rejected with the reason recorded.

## Current Baseline

The current repo snapshot shows that spell runtime and creature behavior are
partially implemented, but not retail-complete.

### Spell Effects

- `SpellEffectType` currently has 150 enum values.
- 89 unique effect families have direct `[SpellEffectHandler]` handlers.
- 61 enum names have no direct handler.
- 14 of the missing names are `UNUSED*`.
- Excluding `UNUSED*`, 47 of 136 named non-unused effect families lack direct
  handlers, which is approximately 34.6 percent.

Important missing named effect families include:

- `Script`
- `NPCForceAIMovement`
- `PathMissionIncrement`
- `HazardEnable`
- `HazardModify`
- `HazardSuspend`
- `ChangePhase`
- `ModifyCreatureFlags`
- `SharedHealthPool`
- `SummonPet`
- `PetCastSpell`
- `ProxyRandomExclusive`
- `ModifySpell`
- `ModifySpellEffect`
- `SuppressSpellEffect`
- `UnitPropertyConversion`
- `VectorSlide`
- `VacuumLoot`
- `WarplotTeleport`

### Spell Protocol And Aux Packets

The following spell/cast-result/cooldown auxiliary packets are mapped at the
read-shape level but remain blocked until apply/consumer semantics or live row
semantics prove runtime emitters and field names:

- `ServerSpellCastResult (0x07FC)`
- `ServerSpellUInt32TripletList (0x080F)`
- `ServerSpellUInt32TripletListVariant (0x0810)`
- `ServerSpellFourUInt32 (0x0812)`

### Creature And Entity Behavior

Creature behavior has a working framework, including idle aggro scan, chase,
auto attacks, target selection, special attacks, cooldowns, and range checks.
However, retail completeness is mostly blocked by content data review, script
semantics, encounter mechanics, and unproven entity/stat auxiliary state.

Current observed content coverage:

- Quest creature coverage has 13,412 rows marked `not_retail_complete`.
- Public-event creature coverage has 2,645 rows marked `not_retail_complete`.
- Instance entity coverage has 271 rows marked `not_retail_complete`.
- Instance dependency coverage has 1,495 rows marked `not_retail_complete`.
- Script runtime action evidence has 205 rows marked `not_retail_complete`.

Combat data currently has:

- 91 reviewed combat kits.
- 2 manual combat profiles.
- 2 manual creature overrides.
- 0 action rules in `CombatActionRules.json`.

### Entity/Stat Auxiliary Packets

The following unit combat/entity stat auxiliary packets and visual flags are
mapped but blocked pending consumer proof, live sniffs, or verified emit
conditions:

- `0x0889`
- `0x08CC`
- `0x08F4`
- `0x0939`
- `0x093D`
- `0x093E`
- `ServerEntityVisualInfoUpdate (0x08A8)`

## Current Slice Notes

### 2026-06-17 Q3479 Creature Relation Reclassification

- `content_retail_completeness_quest_creatures.csv` now keeps Q3479 relation
  `source_relation_id=3449` / Creature2 `36331` Fierce Yeti Icefang as
  mapped-only spawn/context evidence: `NorthernWildsMapScript` has focused
  spawn coverage for the reviewed placements, but build 16042 Q3479 objective
  `4564` credits KillTargetGroup `7288`, which expands through TargetGroup
  `1463` to Creature2 `11945` and `11948` only.
- Q3479 starter relation `source_relation_id=150` / Creature2 `11062` Bosun
  Redmark is runtime-spawn-tested pending client-visible accept dialog,
  prerequisite/visibility, reward/achievement side effects, and end-to-end smoke.
- Verification: `python -m unittest
  test_content_retail_completeness_audit.ContentRetailCompletenessAuditTests.test_quest_creature_rows_cover_current_unmatched_and_noncurrent_relations`;
  `python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py`;
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v
  minimal --nologo --filter
  "FullyQualifiedName~NorthernWildsMapScript_OnLoad_SpawnsQ3486PanelGuardiansQ3479Q3480SurvivorsYetiKillTargetsQ3797DurekTargetsQ3741CratesQ3963ControlsDeadeyeReceiversQ3668ReceiversQ3673SignalFlaresQ4526Q3781AndQ3886Fallbacks"`.

### 2026-06-17 Q3479 Validation Recheck

- Q3479 From the Wreckage remains implemented pending manual/client smoke:
  Bosun Redmark starter spawn, Deadeye Brightland finisher/receiver cache,
  Trapped Survivor CSI placement/credit, TargetGroup `7288` yeti kill credit,
  fixed cash/selectable item rewards, achievement hooks, and Q3667 follow-up are
  covered by focused server tests and generated tracker rows.
- The Fierce Yeti Icefang relation `source_relation_id=3449` stays mapped-only
  contextual spawn evidence for Q3479: build 16042 objective `4564` credits
  TargetGroup `7288`, which expands through TargetGroup `1463` to Creature2
  `11945` and `11948`, not Creature2 `36331`.
- Validation worksheet:
  `artifacts\blocker_evidence\20260617-214306-quest-3479-from-the-wreckage`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter "FullyQualifiedName~Q3479"` passed `38/38` on
  2026-06-17.
- Remaining blocker: live client smoke for Bosun accept dialog, Deadeye
  completion dialog, Trapped Survivor CSI activation/UI, yeti kill credit,
  reward-choice UI and item/cash persistence, achievement UI, mutual exclusion
  with Q3480, prerequisite/visibility behavior, and end-to-end Q3479 -> Q3667
  flow. Zone rows `quest_call_zones` `264`/`1188`/`2546` and `quest_zones`
  `150` remain mapped-only/blocked because current `quest_zone_map.csv` marks
  them `partial` with blank build 16042 WorldZone names, so a reviewed current
  WorldZone bridge plus runtime visibility smoke is still missing.

### 2026-06-17 P0 Quest Validation Batch

- The remaining generated P0 quest queue rows `3480`, `3486`, `3668`, `3673`,
  `3886`, `3963`, `5573`, `5575`, `5580`, and `3797` were rechecked as
  validation-only slices. No runtime behavior changed: each row already has
  focused server/runtime evidence for its current quest, creature, objective,
  reward, achievement, or script surface, but retail completion remains blocked
  on the row-specific client smoke and review evidence below.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter
  "FullyQualifiedName~Q5597|FullyQualifiedName~Q3479|FullyQualifiedName~Q3480|FullyQualifiedName~Q3486|FullyQualifiedName~Q3668|FullyQualifiedName~Q3673|FullyQualifiedName~Q3886|FullyQualifiedName~Q3963|FullyQualifiedName~Q5573|FullyQualifiedName~Q5575|FullyQualifiedName~Q5580|FullyQualifiedName~Q3797"`
  passed `185/185` on 2026-06-17.

| Queue row | Worksheet | Current closure |
| --- | --- | --- |
| Q3480 Reporting for Duty | `artifacts\blocker_evidence\20260617-214531-quest-3480-reporting-for-duty` | Implemented pending Commander Durek/Deadeye dialog, survivor CSI, shared-yeti kill, reward, achievement, exclusion, zone-review, and client smoke. |
| Q3486 Empowered Tower | `artifacts\blocker_evidence\20260617-214714-quest-3486-empowered-tower` | Implemented pending Master Control Panel, arrival/story, Loftite/Crystal Guardian/Frostbite, reward provenance collision, partial zone bridge, achievement, and client smoke. |
| Q3668 Indigenous Intelligence | `artifacts\blocker_evidence\20260617-215006-quest-3668-indigenous-intelligence` | Implemented pending Deadeye/Lusk/Bartol/survivor/Skeech dialog and objective smoke; TargetGroup members `14054` and `11913` still need reviewed current Creature2/source-coordinate evidence. |
| Q3673 Contact with Thayd | `artifacts\blocker_evidence\20260617-215214-quest-3673-contact-with-thayd` | Implemented pending Deadeye/signal-flare/hidden-completion/reward/achievement smoke; finisher relation `1842` / Jabbithole creature `23645` remains unmatched. |
| Q3886 Fiery Distraction | `artifacts\blocker_evidence\20260617-215359-quest-3886-fiery-distraction` | Implemented pending Durek, Burning Torch, Skeech Hut, reward/achievement, Q3673 handoff, duplicate relation `8506`, and zone-routing smoke. |
| Q3963 More Important Than Revenge | `artifacts\blocker_evidence\20260617-215544-quest-3963-more-important-than-revenge` | Implemented pending Ship Controls/Galeras Deadeye smoke; the `11063` starter-context caveat and unmatched `starter:2130:23645` relation need client/bridge proof. |
| Q5573 Powering Down | `artifacts\blocker_evidence\20260617-215735-quest-5573-powering-down` | Implemented pending Mondo, Power Regulator, cinematic, reward/achievement, and Q5596-branch smoke; unpromoted Mondo coordinates remain evidence-only. |
| Q5575 Seizing Power | `artifacts\blocker_evidence\20260617-220052-quest-5575-seizing-power` | Implemented pending Kezrek, shield-generator, Power Regulator, reward/achievement, objective-evidence ordering, zone routing, and Q5596-branch smoke. |
| Q5580 Enforced Radio Silence | `artifacts\blocker_evidence\20260617-220251-quest-5580-enforced-radio-silence` | Implemented pending Tower Controls, Kezrek, reward/achievement, objective row `1452`, zone rows `309`/`620`, episode row `2389`, and Q5594-merge smoke. |
| Q3797 Securing the Area | `artifacts\blocker_evidence\20260617-220423-quest-3797-securing-the-area` | Implemented pending Durek/rootbrute-yeti/reward/Q3486-prereq smoke; partial zone rows `96`/`166`, type-9 target branches, and combat placement behavior remain blocked. |

### 2026-06-17 Ultimate Protogames Split Validation

- World `2980` / public event `594` remains split-required and not
  retail-complete. The tracker has zero generic missing-producer rows for this
  slice; start button, Bev-O-Rage, Vend-A-Tron, Sneaky Prison, Prototentiary,
  Warden, Deputy, Mondo, Ruffles, Gilded Fowl, Hut-Hut, Misplaced Mammoth, and
  tank-room producers are runtime-backed where their individual rows cite
  focused tests.
- Validation worksheet:
  `artifacts\blocker_evidence\20260617-220641-instance-2980-ultimate-protogames-split-validation`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter
  "FullyQualifiedName~NexusForever.Game.Tests.Instances.UltimateProtogamesEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.PublicEventObjectiveCreditEntityScriptTests"`
  passed `620/620` on 2026-06-17.
- Remaining blocker: room randomization/route selection, start-button client
  smoke, Gate Console alias and door choreography, Prototentiary stealth and
  no-alarm semantics, Deputy density/pathing, Ruffles hunt route/pathing, Power
  Plunge scoring/qualification, Hut-Hut football scoring/fumble/shutout/deathless
  and timer semantics, Mondo crate timer/row selection, tank-room timers/failure
  semantics, scoreboard/teleporter/hidden-holder/rookie rows, detailed boss
  mechanics, rewards, achievements, cleanup/replay behavior, and full dungeon
  smoke.

### 2026-06-17 Expedition Tracker Regression Alignment

- The content-retail audit tests now match the generated runtime-tested statuses
  for Fragment Zero search trigger rows `4420`/`4421`, Fragment Zero gold timer
  `4690`, Infestation gold timer `4703`, Outpost M-13 gold timer `4702`, and
  Space Madness gold timer `4705`.
- This was tracker/test alignment only. No instance behavior was widened; the
  rows remain pending exact trigger replay, timer reward/presentation, client
  smoke, achievements, and full expedition smoke where named in
  `CONTENT_RETAIL_COMPLETENESS_TRACKER.md`.
- Verification: `python -m unittest
  test_content_retail_completeness_audit.ContentRetailCompletenessAuditTests`;
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v
  minimal --nologo --filter
  "FullyQualifiedName~FragmentZeroEventScriptTests|FullyQualifiedName~InfestationEventScriptTests|FullyQualifiedName~OutpostM13EventScriptTests|FullyQualifiedName~SpaceMadnessEventScriptTests"`.

### 2026-06-17 Q5597 Validation Bundle

- Q5597 Dregs and Thieves remains implemented pending manual/client smoke:
  Mondo Zax starter/finisher spawn evidence, Chua Explosives virtual-collect
  activation credit, reward grants, achievement checklist update, and Q5604
  follow-up are covered by focused server tests and generated tracker rows.
- Local evidence scaffold created intentionally without launching server/client:
  `artifacts\blocker_evidence\20260617-214211-quest-5597-dregs-and-thieves`;
  the corrected harness-path worksheet with the richer server-start notes remains
  `artifacts\blocker_evidence\20260617-213642-quest-5597-dregs-and-thieves`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter "FullyQualifiedName~Q5597"` passed `15/15` on
  2026-06-17.
- Remaining blocker: live client smoke for Mondo accept/completion dialog, Chua
  Explosives activation/UI, exact retail density and respawn/despawn behavior,
  reward UI/inventory persistence, achievement UI, zone/episode visibility, and
  end-to-end Q5596 -> Q5597 -> Q5604 flow.

### 2026-06-17 Ultimate Protogames Downsizer Validation Bundle

- World `3041` / public event `642` remains validation-ready but not
  retail-complete: objective `3197` activation, Creature2 `61420` death credit,
  event finish boundary, challenge objective activation rows `3266`/`3267`/`3268`/`3269`,
  map binding, and shared instance-settings packet handling are focused-test-backed.
- Local evidence scaffold created intentionally without launching server/client:
  `artifacts\blocker_evidence\20260617-214438-instance-3041-ultimate-protogames-downsizer`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter
  "FullyQualifiedName~UltimateProtogamesDownsizerEventScriptTests|FullyQualifiedName~InstanceMapBindingTests|FullyQualifiedName~InstanceSettingsHandlerTests|FullyQualifiedName~PublicEventObjectiveCreditEntityScriptTests"`.
- Remaining blocker: live client smoke for exact portal entry routing, Downsizer
  challenge ability mechanics/scoring/failure semantics, boss mechanics,
  rewards, achievements, difficulty/settings edge cases, and full dungeon smoke.

### 2026-06-17 Vault Of The Archon Validation Bundle

- World `3009` Vault of the Archon / Hall of the Hundred remains heavily
  runtime-backed but not retail-complete: opening, Varegor, bridge, Kel Havik,
  courtyard, vault-door, Harizog, optional side-event, trigger, key, and direct
  objective producer rows are covered by focused server tests where the tracker
  marks them `runtime_vault_archon_*`.
- Local evidence scaffold created intentionally without launching server/client:
  `artifacts\blocker_evidence\20260617-214558-instance-3009-vault-of-the-archon`;
  the validation-gate worksheet is
  `artifacts\blocker_evidence\20260617-221050-instance-3009-vault-of-the-archon-validation`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter
  "FullyQualifiedName~HallOfTheHundredEventScriptTests|FullyQualifiedName~HallOfTheHundredOptionalEventScriptTests|FullyQualifiedName~InstanceMapBindingTests|FullyQualifiedName~PublicEventObjectiveCreditEntityScriptTests"`
  passed `848/848` on 2026-06-17.
- Remaining blocker: live client smoke for route/choreography timing, trigger
  placement, key/socket/door behavior, wave and boss mechanics, mapped-only
  target-group mismatches, cold/hazard mechanics, rewards, achievements, and full
  world-story instance smoke.

### 2026-06-17 Remaining Instance Validation Batch

- The remaining generated instance queue rows `1271`, `3180`, `1263`, `2183`,
  `3404`, `2149`, `3041`, and `1336` were rechecked as validation-only slices.
  No runtime behavior changed in this batch; the rows remain not retail-complete
  until their exact client, placement, route, reward, achievement, and mechanics
  evidence is captured.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter
  "FullyQualifiedName~NexusForever.Game.Tests.Instances.SanctuaryOfTheSwordmaidenEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.SanctuaryOfTheSwordmaidenTriggerScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.PublicEventObjectiveCreditEntityScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.FragmentZeroEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.BranchCinematicHookMapScriptTests.FragmentZero_|FullyQualifiedName~NexusForever.Game.Tests.Instances.SkullcanoEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.SkullcanoTriggerScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.GauntletEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.PublicEvents.EvilFromTheEtherEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.PublicEvents.EvilFromTheEtherTriggerScriptTests|FullyQualifiedName~spacemadness|FullyQualifiedName~NexusForever.Game.Tests.Instances.UltimateProtogamesDownsizerEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.InstanceMapBindingTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.InstanceSettingsHandlerTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.RuinsOfKelVorethEventScriptTests|FullyQualifiedName~NexusForever.Game.Tests.Instances.RuinsOfKelVorethTriggerScriptTests"`
  passed `974/974` on 2026-06-17.

| Queue row | Worksheet | Current closure |
| --- | --- | --- |
| Sanctuary of the Swordmaiden `1271` | `artifacts\blocker_evidence\20260617-221308-instance-1271-sanctuary-of-the-swordmaiden-validation` | Mapped/runtime WIP pending route weights, boss/miniboss mechanics, Selene escort/challenge proof, residual ritual/teleporter/controller producers, placement/visual/despawn, rewards, achievements, and dungeon smoke. |
| Fragment Zero `3180` | `artifacts\blocker_evidence\20260617-221438-instance-3180-fragment-zero-validation` | Mapped/runtime WIP pending Skeech ambush wave proof, trigger lifecycle, real cinematic payload, communicator/door choreography, entity presentation/cleanup, medal/reward semantics, and expedition smoke. |
| Skullcano `1263` | `artifacts\blocker_evidence\20260617-221559-instance-1263-skullcano-validation` | Mapped/runtime WIP pending boss mechanics, Tugga failure, escort/choreography, jailor/release and essence proof, route weights, terraformer timing, residual bonus/challenge rows, placement state, rewards, achievements, and dungeon smoke. |
| Gauntlet `2183` | `artifacts\blocker_evidence\20260617-221728-instance-2183-gauntlet-validation` | Mapped/runtime WIP pending score-token, gold-skull, Splorg, main-event, arena/score/gold-timer, door/product, cinematic/announcer, route-selection, encounter, reward, achievement, and expedition smoke. |
| Evil from the Ether `3404` | `artifacts\blocker_evidence\20260617-221850-instance-3404-evil-from-the-ether-validation` | Mapped/runtime WIP pending exploding-portal avoidance, drive-spark interaction/visual/despawn, medbay timing, organism waves, Ether-Charged Ravenous state, medal/reward semantics, placement/import, rewards, achievements, and expedition smoke. |
| Space Madness `2149` | `artifacts\blocker_evidence\20260617-222012-instance-2149-space-madness-validation` | Mapped/runtime WIP pending hallucination wave counter, air-scrubbing wave/timer, Rowsdower explosion failure, NPC/console/control placement, door/trigger/teleport proof, communicator/cinematic timing, rewards, achievements, and expedition smoke. |
| Ultimate Protogames Downsizer `3041` | `artifacts\blocker_evidence\20260617-222140-instance-3041-map-ultimateprotogamesraid-validation` | Validation-ready but blocked on exact portal entry routing, challenge ability/scoring/failure semantics, boss mechanics, rewards, achievements, difficulty/settings edge cases, and dungeon smoke. |
| Ruins of Kel Voreth `1336` | `artifacts\blocker_evidence\20260617-222305-instance-1336-ruins-of-kel-voreth-validation` | Mapped/runtime WIP pending Drokk exo-defense proof, health-station, jump puzzle, challenge/timer/rookie/kill-count rows, boss mechanics, trigger/door/optional-route timing, spawn cadence, visual state, rewards, achievements, and dungeon smoke. |

### 2026-06-17 Spell Aux And Creature AI Evidence Gate Recheck

- The remaining high-impact spell-effect families were rechecked against the
  current plan baseline. No additional handler was implemented because the
  remaining candidates still lack enough effect-row semantics, native
  apply/producer ownership, or live quest/encounter observations to satisfy the
  evidence ladder.
- Spell auxiliary packets `0x07FC`, `0x080F`, `0x0810`, and `0x0812` remain
  mapped-only/blocked. The next required evidence is a native producer/apply
  owner, callback/table ownership proof, or accepted live capture that proves
  field meanings, emit timing, and negative cases.
- Entity/stat auxiliary packets `0x0889`, `0x08CC`, `0x08F4`, `0x0939`,
  `0x093D`, `0x093E`, and compact visual-info `0x08A8` remain
  mapped-only/blocked. `Decomp\Analysis\MISSING_FEATURE_MATRIX.md` and
  `Decomp\Analysis\BLOCKER_EVIDENCE_PLAN.md` record the 2026-06-17 F-025
  entity-stat aux recheck and the worksheet
  `artifacts\blocker_evidence\20260617-224723-F025-entity-stat-aux-recheck`;
  there was no running Ghidra MCP instance or accepted live capture to prove
  emit conditions or semantic field names.
- Creature combat AI remains mapped-only/blocked beyond the already covered
  framework behavior because `CombatActionRules.json` still has no proven action
  rules. The next required evidence is reviewed combat-kit action semantics tied
  to a creature slice, a native action producer/consumer witness, or local client
  combat smoke that proves timing, target selection, range, cooldown, and failure
  cases.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter "FullyQualifiedName~Spell"` passed `336/336` on
  2026-06-17.

### 2026-06-17 Headless Ghidra Unblock Attempt

- `mcp__ghidra_mcp.list_instances` returned no running Ghidra instances, so
  interactive MCP/CodeBrowser inspection was unavailable. The local
  `NexusForeverClient64_WildStar64` project, build 16042 `WildStar64.exe`, and
  pinned Ghidra 12.0.4 toolchain were present, so a headless pass was run.
- Entity/stat aux probe: `FindDataReferences.java` checked
  `ServerSpellUInt32TripletListRow_ReadPayload` (`140080bf0`),
  `ServerEntityStatUInt32UInt5UInt32_ReadPayload` (`140097620`),
  `ServerEntityStatUInt32UInt5Pair_ReadPayload` (`140097690`),
  `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` (`140097ee0`),
  `ServerEntityStatTwoUInt32UInt64_ReadPayload` (`140097f70`),
  `ServerUInt32WideString_ReadPayload` (`1400980f0`), and
  `ServerEntityVisualInfoUpdate_ReadPayload` (`140098460`). Ghidra found
  registration references under `Network_RegisterServerOpcode_0351`, the shared
  row-reader call, and raw metadata/table cells only; no producer, apply owner,
  callback/table owner, or semantic field consumer surfaced.
- Raw-slot follow-up: `FindDataReferences.java` found zero code references to
  the raw data cells reported for the reader addresses, and
  `DumpNearbyData.java` at `140dcf8b0` identified the region as
  `_IMAGE_RUNTIME_FUNCTION_ENTRY` unwind metadata rather than packet dispatch
  state.
- Spell aux probe: `FindDataReferences.java` checked
  `ServerSpellCastResult_ReadPayload` (`140094fb0`),
  `ServerSpellUInt32TripletList_ReadPayload` (`140095da0`),
  `ServerSpellFourUInt32_ReadPayload` (`14007fef0`), and the shared triplet-row
  reader (`140080bf0`). The result again showed only
  `Network_RegisterServerOpcode_0351`, the known nested row-reader call, and
  metadata/table refs; no `0x07FC`, `0x080F`, `0x0810`, or `0x0812`
  apply/consumer owner was found.
- Result: headless Ghidra did not unblock implementation for these packets. The
  next smallest unlock is either an interactive MCP pass with the
  `NexusForeverClient64_WildStar64` project open in CodeBrowser, a native
  debugger breakpoint/trace on the registration dispatch path, or accepted live
  packet captures around the relevant spell/entity state transitions.

### 2026-06-17 Interactive Ghidra MCP Unblock Attempt

- `mcp__ghidra_mcp.connect_instance` connected to
  `NexusForeverClient64_WildStar64` over `http://127.0.0.1:8089`; the active
  program was switched to `/WildStar64.exe`. The debugger bridge at
  `127.0.0.1:8099` was not running, so this was a static MCP pass rather than a
  live runtime trace.
- MCP xrefs for `Network_RegisterServerOpcode_0351` (`14006c290`) and the
  blocked reader functions matched the headless result. The readers were
  referenced by opcode registration instructions, nested row-reader calls, and
  data/unwind metadata only:
  `ServerSpellCastResult_ReadPayload` (`140094fb0`),
  `ServerSpellUInt32TripletList_ReadPayload` (`140095da0`),
  `ServerSpellFourUInt32_ReadPayload` (`14007fef0`),
  `ServerSpellUInt32TripletListRow_ReadPayload` (`140080bf0`),
  `ServerEntityStatUInt32UInt5UInt32_ReadPayload` (`140097620`),
  `ServerEntityStatUInt32UInt5Pair_ReadPayload` (`140097690`),
  `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` (`140097ee0`),
  `ServerEntityStatTwoUInt32UInt64_ReadPayload` (`140097f70`),
  `ServerUInt32WideString_ReadPayload` (`1400980f0`), and
  `ServerEntityVisualInfoUpdate_ReadPayload` (`140098460`).
- The `140c1e858`/`140c1e900` pointer region was inspected because it contains
  reader pointers such as `140080bf0` and `140097620`. Neighboring functions
  decompiled as generic bit-reader helpers that call `FUN_14006c090` with field
  widths; they did not reveal packet apply, producer, or semantic consumer
  ownership.
- The live MCP script runner executed `FindOpcodeComparisons.java` for
  `0x07FC`, `0x080F`, `0x0810`, `0x0812`, `0x0889`, `0x08CC`, `0x08F4`,
  `0x0939`, `0x093D`, `0x093E`, and `0x08A8`. The true opcode immediates were
  still confined to `Network_RegisterServerOpcode_0351`; additional hits
  decompiled as GameFormula IDs (`0x0939`, `0x093D`, `0x093E`), UI/object
  offsets, allocation sizes, or float/object-field constants rather than packet
  apply owners.
- Result: interactive static Ghidra MCP did not unblock implementation for the
  spell/entity aux packets. The remaining unlock is a live debugger trace on
  the world-packet dispatch/apply path, or accepted packet captures proving
  field semantics and emit timing.

### 2026-06-17 Stormtalon's Lair Validation Bundle

- World `382` / public event `145` remains split-required and not
  retail-complete: Blade-Wind, Aethros, Stormtalon, Thundercall Pell, High Priest,
  cage/data-altar/storm-totem/launch-pad/optional-miniboss objective producers,
  and reviewed placement slices are focused-test-backed where tracker rows are
  marked `runtime_stormtalon_*`.
- Public-event creature relation `15` now has a reviewed bridge from Jabbithole
  creature `1046` Launch Pad to Creature2 `31587`; relation `57` now has a
  reviewed bridge from Jabbithole creature `1345` Axis Pheydra to Creature2
  `37434`; relation `199` now has a reviewed bridge from Jabbithole creature
  `5045` Belle Walker to Creature2 `47063`; relation `275` now has a reviewed
  bridge from Jabbithole creature `760` Improvement Construction Platform to
  Creature2 `27244`. These remain blocked on runtime spawn/import or script
  ownership, objective-credit behavior, rewards/medals, and client smoke where
  not already covered by the focused WIP Stormtalon producer tests. Belle Walker
  also remains blocked on blank `TalkTo`/TargetGroup `6522` repeat/dialog
  semantics, and Corrupted Flower Stem relation `14` remains blocked with no
  build 16042 Creature2 bridge.
- Local evidence scaffold created intentionally without launching server/client:
  `artifacts\blocker_evidence\20260617-214707-instance-382-stormtalons-lair`.
- Verification: `dotnet test
  Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal
  --nologo --filter
  "FullyQualifiedName~StormtalonsLairEventScriptTests|FullyQualifiedName~StormtalonsLairTriggerScriptTests|FullyQualifiedName~PublicEventObjectiveCreditEntityScriptTests|FullyQualifiedName~InstanceMapBindingTests"`.
- DataMapping verification: `python
  Tools\DataMapping\map_wildstar_data.py --limit-creatures 200
  --limit-relation-rows 500 --limit-spawns 500 --output-dir
  Tools\DataMapping\output_test` completed successfully; `python
  Tools\DataMapping\map_wildstar_data.py` completed successfully with the longer
  verification window and refreshed `creature_map.csv`,
  `creature_public_event_map.csv`, and override audit output; the full mapper was
  rerun after correcting the Belle Walker override CSV reason field. The refreshed
  content-retail tracker validates with `python
  Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py`
  (`31` CSVs / `168101` rows, all `not_retail_complete`), and Python audit
  tests passed `98/98`.
- Remaining blocker: runtime spawn/phase smoke for reviewed bridges, blank
  `TalkTo`/TargetGroup `6522` repeat/dialog semantics, exact boss/challenge
  mechanics, tornado/lightning/gust/deathless/rookie/timer producers, route
  visual state and cleanup, rewards, achievements, full dungeon smoke, and a
  missing build 16042 Creature2 bridge for Corrupted Flower Stem relation `14`.

## Workstreams

The remaining work should be closed through six parallel but coordinated
workstreams.

## 1. Freeze And Rank The Backlog

Goal: turn the broad missing-feature surface into a ranked, evidence-backed
implementation queue.

Actions:

1. Build a working backlog from:
   - `CURRENT_STATUS.md`
   - `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`
   - `Decomp/Analysis/coverage/*.csv`
   - `Tools/DataMapping/output/*.csv`
   - `Tools/DataMapping/review/*.csv`
2. For each row, capture:
   - subsystem
   - source tracker
   - current evidence state
   - blocker
   - required next evidence
   - implementation target
   - test target
   - smoke target
   - final tracker update target
3. Classify each row as:
   - `implementable_now`
   - `mapped_only`
   - `needs_live_evidence`
   - `needs_data_review`
   - `rejected_or_not_current_retail_row`
4. Rank work by:
   - starter-zone and NPE impact
   - quest progression impact
   - public-event progression impact
   - instance progression impact
   - number of dependent rows unblocked
   - strength of existing evidence

Deliverable:

- A top-25 closure queue for the next implementation slices.

Done when:

- Every selected row has a single named blocker and a next action.
- Rows with no current retail/client source are marked explicitly instead of
  remaining in the active implementation queue.

## 2. Close High-Impact Spell Effect Families

Goal: implement missing spell handlers that unblock actual creature, quest,
public-event, or instance behavior.

Do not implement handlers in enum order. Rank missing handlers by observed use in
client data, DataMapping output, scripts, live logs, and blocked content rows.

Priority group A:

- `Script`
- `NPCForceAIMovement`
- `PathMissionIncrement`
- `HazardEnable`
- `HazardModify`
- `HazardSuspend`
- `ChangePhase`
- `ModifyCreatureFlags`
- `SharedHealthPool`
- `SummonPet`
- `PetCastSpell`
- `ProxyRandomExclusive`

Priority group B:

- `ModifySpell`
- `ModifySpellEffect`
- `SuppressSpellEffect`
- `UnitPropertyConversion`
- `VectorSlide`
- `RewardBuffModifier`
- `SpellCounter`
- `ForcedAction`

Priority group C:

- housing, warplot, vendor, trade-skill, cosmetic, icon, display-name, and
  economy-adjacent effect families.

Per-family process:

1. Identify all client rows using the effect family.
2. Record spell IDs, spell4 IDs, spell effect IDs, data bits, and target
   filtering.
3. Check existing decompile labels, cached exports, findings, and blocker
   notes.
4. Capture live/client evidence only when table data and cached evidence are not
   enough.
5. Implement the smallest verified runtime handler.
6. Add focused tests for:
   - handler dispatch
   - target selection
   - data-bit interpretation
   - failure/no-op behavior
   - packet or state emission, if applicable
7. Update the tracker with evidence source, implementation path, tests, and any
   remaining unknowns.

Done when:

- The handler is implemented only for proven semantics.
- Unsupported data-bit combinations are guarded or diagnostic-only.
- Focused tests pass.
- The related tracker row is moved out of `blocked` or `partial` with evidence.

## 3. Resolve Spell Protocol And Aux Packet Emitters

Goal: turn mapped spell auxiliary packet shapes into verified runtime producers
where needed.

Blocked packet families:

- `ServerSpellCastResult (0x07FC)`
- `ServerSpellUInt32TripletList (0x080F)`
- `ServerSpellUInt32TripletListVariant (0x0810)`
- `ServerSpellFourUInt32 (0x0812)`

Actions:

1. Search cached decompile exports and labels for native producers and
   consumers.
2. Use targeted Ghidra passes only for the unresolved call sites.
3. Capture live evidence for:
   - failed cast result paths
   - cooldown updates
   - LAS changes
   - proc changes
   - spell state transitions
4. Compare live packet timing with current emulator behavior.
5. Implement emitters only where trigger conditions and field meanings are
   proven.
6. Keep uncertain fields neutral, diagnostic-only, or blocked.

Done when:

- Producer path is verified.
- Field names are evidence-backed.
- Tests cover both emit and non-emit cases.
- Tracker rows cite packet ID, evidence bundle, implementation, and tests.

## 4. Resolve Entity, Stat, Visual, And Creature State Aux Packets

Goal: unblock creature combat and entity visual/state behavior that depends on
currently mapped-but-unemitted auxiliary packets.

Blocked packet families:

- `0x0889`
- `0x08CC`
- `0x08F4`
- `0x0939`
- `0x093D`
- `0x093E`
- `ServerEntityVisualInfoUpdate (0x08A8)`

Actions:

1. Identify known native consumers and likely producers.
2. Capture live state transitions around:
   - combat start/end
   - stat changes
   - creature death/despawn
   - phase changes
   - visibility changes
   - scripted encounter state changes
3. Compare against current entity create/update behavior.
4. Implement producer paths only for proven conditions.
5. Add tests for the state transition that causes the packet.

Done when:

- The packet is emitted only for verified state transitions.
- Unknown flags remain blocked or diagnostic.
- Visibility/stat behavior is covered by focused tests.

## 5. Promote Creature Data Safely

Goal: convert reviewed client/Jabbithole/DataMapping evidence into runtime-owned
creature content without unsafe bulk imports.

Rules:

- Do not query authoring/reference tables from runtime code.
- Do not import ambiguous creature bridge rows without review.
- Do not re-enable unsafe Northern Wilds bulk spawn import unless a focused task
  explicitly requires it.
- Prefer additive, reviewed, deterministic imports.

Quest creature order:

1. Convert the 118 runtime/tested-or-present rows into smoke-verified closure
   rows.
2. Promote and smoke the 1,140 reviewed bridge/spawn-pending rows.
3. Review high-impact rows from the 3,157 ambiguous bridge rows.
4. Investigate the 210 unmatched/blocked rows only after checking whether the
   source row is current.

Public-event creature order:

1. Close the 31 reviewed bridge/spawn-pending rows.
2. Review the 511 ambiguous bridge rows.
3. Investigate the 89 unmatched rows.
4. Mark non-current event rows explicitly.

DataMapping process:

1. Use prioritization helpers to select high-impact review rows.
2. Add global creature bridge overrides only when the creature mapping is
   broadly correct.
3. Add relation-scoped overrides when the mapping is only correct for one quest,
   objective, target group, public event, world location, or script context.
4. Re-run the mapper.
5. Load staging tables in an authoring environment.
6. Apply safe world imports only for reviewed/approved rows.
7. Verify safe runtime tables.
8. Export refreshed runtime seed SQL only when the promoted data should be
   checked in.

Done when:

- Runtime data is promoted into runtime-owned tables/scripts/assets.
- SQL verification passes.
- Coverage rows move from pending/reviewed to smoke-verified or closed.

## 6. Expand Creature Combat AI From Proven Kits

Goal: improve retail creature behavior using reviewed combat kit and action
evidence rather than hand-authored guesses.

Actions:

1. Use existing combat framework as the base:
   - profile resolution
   - aggro scan
   - chase/leash
   - auto attack
   - target selection
   - special attack cooldowns
   - range checks
2. Promote reviewed combat kit evidence into runtime combat profiles.
3. Add action rules only for proven action semantics.
4. Keep manual overrides narrow and evidence-backed.
5. Prioritize:
   - starter-zone creatures
   - quest blockers
   - public-event blockers
   - instance encounter blockers
6. Add tests for:
   - combat profile resolution precedence
   - special attack selection
   - cooldown behavior
   - target switching
   - out-of-range handling
   - idle aggro behavior

Done when:

- A creature's behavior is backed by reviewed data or live evidence.
- Tests cover the runtime selection path.
- Related quest/public-event/instance smoke confirms the behavior in context.

## 7. Close Instance And Encounter Rows

Goal: turn mapped instance rows into playable, smoke-verified behavior.

Current incomplete instance entity groups:

- 80 reviewed stat/combat smoke pending rows.
- 75 reviewed spawn/mechanics smoke pending rows.
- 67 reviewed event/phase smoke pending rows.
- 35 reviewed script/mechanics smoke pending rows.
- 14 official reconciliation smoke pending rows.

Actions:

1. Pick one instance or encounter lane at a time.
2. Promote reviewed spawn/stat/entity rows.
3. Implement the smallest needed script hooks.
4. Add spell handlers or AI action rules only when the encounter requires them.
5. Run local client smoke.
6. Record evidence bundle paths and tracker updates.

Done when:

- Instance entities spawn correctly.
- Combat/stat/script behavior is verified in local smoke.
- Tracker rows cite implementation, evidence, and tests.

## Suggested Execution Sequence

### Sprint 0: Baseline And Queue

- Freeze current coverage metrics.
- Produce the top-25 ranked closure queue.
- Run a narrow build/test baseline.
- Identify the first spell, creature data, and AI slices to close.

### Sprint 1: Spell Handler Closures

- Close 3 to 5 high-impact spell effect families.
- Prefer spell effects that unblock quest creatures, public events, or instance
  mechanics.
- Add focused tests for every handler.

### Sprint 2: Spell Aux Packet Semantics

- Resolve at least one blocked spell auxiliary packet family.
- Implement producer semantics only if field meanings and emit conditions are
  proven.
- Otherwise update the tracker with the exact remaining evidence blocker.

### Sprint 3: Creature Data Promotion

- Promote one reviewed creature data slice into runtime-owned tables/assets.
- Run DataMapping smoke, staging verification, and safe-world verification.
- Smoke the related quest or public-event behavior.

### Sprint 4: Combat AI Expansion

- Promote reviewed combat kit behavior for the selected slice.
- Add action rules where semantics are proven.
- Add unit tests for profile/action selection.

### Sprint 5: Instance/Public Event Closure

- Close one instance or public-event lane end to end.
- Include spawn, stat, script, spell, AI, and smoke evidence as needed.
- Update all affected trackers and coverage files.

## Verification Commands

Use the narrowest useful verification for each slice.

Build the game project:

```powershell
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
```

Run game tests:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

Run a DataMapping smoke pass:

```powershell
python Tools\DataMapping\map_wildstar_data.py --limit-creatures 200 --limit-relation-rows 500 --limit-spawns 500 --output-dir Tools\DataMapping\output_test
```

Run the default DataMapping pass:

```powershell
python Tools\DataMapping\map_wildstar_data.py
```

Verify safe world imports:

```powershell
Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

Verify authoring staging metrics when staging is involved:

```powershell
Get-Content -Raw Tools\DataMapping\sql\verify_authoring_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

## Slice Definition Of Done

A spell, creature behavior, public event, quest creature, or instance row is done
only when all of the following are true:

- The evidence source is recorded.
- The implementation is limited to proven behavior.
- Focused tests or local client smoke verify the behavior.
- Runtime code consumes only runtime-owned data.
- Unknown fields and unproven data bits remain blocked or diagnostic-only.
- Generated/local artifacts are not changed unless intentionally regenerated.
- The relevant tracker row is updated with implementation, verification, and any
  remaining limitations.
