# Evil from the Ether Expedition Port Plan

## Goal
Port the working Evil from the Ether expedition slice from
`origin/expedition-evil-from-the-ether` into the current `dev` branch without
wholesale-merging the branch.

Success means a local `dev` build can:

- migrate the world database with the schema needed by the expedition data;
- import `NexusForever.WorldDatabase\Instance\Expedition\Evil from the Ether.sql`;
- load world `3404` and public event `781`;
- attach DB-driven named scripts to expedition entities;
- run the core expedition phases, doors, interactables, teleports, cutscenes,
  public event objectives, and key combat encounters without server exceptions.

This plan targets a working slice, not a full branch transplant. It should keep
unrelated branch work such as telemetry rewrites, service removals, API
restructure, friendship-service churn, tutorial phase work, and broad message
cleanup out of scope unless a compile-time dependency proves otherwise.

## Branch Baseline

- Main repo: `I:\GIT\NexusForever`
- Target branch: `dev`
- Source branch: `origin/expedition-evil-from-the-ether`
- Source branch head inspected: `146e4f0c`
- Merge base after deepening the shallow clone: `091da4e`
- Unique source commits relative to `dev`: 51
- Narrow expedition/script/world-db subset:
  - 82 files changed
  - 7,303 insertions
  - 204 deletions
- Broader runtime dependency footprint across DB, game, map, spell, entity,
  combat, script, public-event, cinematic, and selected world-server handlers:
  - 375 files changed
  - 19,646 insertions
  - 3,000 deletions

The branch is not a clean cherry-pick. The expedition relies on a chain of
runtime infrastructure commits: spell execution changes, entity property
overrides, CreatureInfo, script-name filters, entity spawning, local teleports,
grid triggers, public event improvements, forced movement, combat AI, CC, and
stat update changes.

## Source Truth And Local Data

- Use `origin/expedition-evil-from-the-ether` as the behavioral reference.
- Use `I:\GIT\NexusForever.WorldDatabase` as the official data checkout.
- The relevant world SQL file is:
  `I:\GIT\NexusForever.WorldDatabase\Instance\Expedition\Evil from the Ether.sql`
- The SQL uses world `3404`. The expedition map script uses public event `781`.
- The setup script now skips unsupported world SQL by schema. Once the required
  world schema is ported, this file should stop being skipped and should import
  normally.

## Porting Strategy

Use a surgical port in dependency order. Do not merge the source branch.

Recommended process:

1. Create a temporary integration branch from `dev`.
2. Keep the setup/import-script work separate from the expedition runtime port.
3. Port one layer at a time and keep the solution compiling between layers.
4. Use the source commits as references, not as blind cherry-picks.
5. Prefer current `dev` spell/combat/entity code when it conflicts with the
   older source branch; adapt the expedition code to the current shape.
6. Verify with disposable databases before touching a long-lived local DB.

## Source Commits To Mine

Core expedition commits:

- `0937c24` - initial Evil framework, first phases, two cutscenes, local
  teleport, relocation changes, world-location trigger entity.
- `551d0d56` - phases up to Etheric Entity, script loading changes,
  active-prop filter, public-event checklist support.
- `6a49d226` - remaining phase framework, entity add callbacks, entity summon
  manager, position projectile movement, additional grid trigger types.
- `ccfda5e8` - communicator messages, named entity scripts, local teleport
  option, contravariant owned scripts.
- `3c91a94d` - basic Ravenous Reaper, Security Chief Kondovich, and Katja
  Zarkhov scripts.
- `9c1474ef` - Ravenous Refugees start dormant.

Data and entity model commits:

- `de176b04` - `entity_property` table and entity property overrides.
- `f36e7010` - transient `entity_template` support and dynamic spawns.
- `d88057f6` - CreatureInfo replaces EntityTemplate, creature property/stat
  overrides, entity summon by CreatureInfo.
- `cf2f2cb` - default entity movement mode in DB.

Script/runtime infrastructure commits:

- `becec71c` - first CombatAI.
- `235e7051` - script filter classes.
- `64434c34`, `34964931`, `0fe78196`, `6361e86e` - public event stat/objective
  fixes needed by content events.

High-risk dependency commits:

- `c010636f`, `54cd7380`, `5bda8101`, and related spell-v2 commits - spell
  execution/target/effect architecture used by branch combat scripts.
- `9f6ad36f` - CC, interrupt armor, delayed effects, buff removal behavior.
- `c8a6d887` and `f4128ae9` - forced movement, velocity keys, relocation
  behavior.
- `36f415d8` and `68470210` - stat updater and vital/stat/property behavior.

Only port high-risk dependency commits when a specific expedition script,
combat encounter, or compile error requires them.

## Phase 0: Working Branch And Safety

Tasks:

- Create a branch such as `codex/evil-expedition-port` from `dev`.
- Record the current dirty worktree before making runtime changes.
- Keep current setup/import script changes intact.
- Use disposable MySQL databases for migration/import testing.
- Confirm runtime asset availability:
  - WildStar 16042 `tbl`/`bin` game tables.
  - Generated map assets, including the map for world `3404`.
- Confirm the WorldDatabase checkout is current enough to contain
  `Evil from the Ether.sql`.

Acceptance:

- `git status` is understood and unrelated local work is not touched.
- `dotnet build Source\NexusForever.slnx` baseline status is known before the
  port starts.
- Existing setup script can still create/migrate/import all currently supported
  data.

Estimate: 0.5 day.

## Phase 1: World Database Schema And Import

Required schema:

- Keep the current `dev` migration history; do not rename existing migrations to
  match the source branch.
- Add forward migrations for final required schema:
  - `entity_property`
  - `creature_info_property`
  - `creature_info_stat`
- Confirm existing `dev` schema already covers:
  - `entity_script`
  - `entity.mode`
  - `entity_event`
  - `map_entrance`
  - `version`
- Do not port the transient `entity_template` tables unless a script still needs
  them after the CreatureInfo port. The source branch replaces them with
  CreatureInfo.

Required model changes:

- Add `CreatureInfoPropertyModel`.
- Add `CreatureInfoStatModel`.
- Add `EntityPropertyModel`.
- Update `EntityModel` navigation for `EntityProperty`.
- Update `WorldContext` mappings.
- Update `WorldDatabase.EntitiesInclude(...)` to include entity properties.
- Add query methods for creature info property/stat overrides.

Import tasks:

- Run EF migrations against a disposable `nexus_forever_world`.
- Run the setup importer against the WorldDatabase checkout.
- Verify `Evil from the Ether.sql` is no longer skipped for missing
  `entity_property.value` and `creature_info_property.value`.
- Verify failed partial imports are repaired before retry.

Acceptance:

- `SHOW TABLES` includes `entity_property`, `creature_info_property`, and
  `creature_info_stat`.
- `version` records `Evil from the Ether.sql`.
- `entity` has rows for world `3404`.
- `entity_script` has rows with script names used by the Evil scripts.
- No compatibility-only `alue` columns remain in the world DB.

Estimate: 0.5-1.5 days.

## Phase 2: CreatureInfo And Entity Initialization

Tasks:

- Port the CreatureInfo abstraction:
  - interfaces in `Source\NexusForever.Game.Abstract\Entity\Creature`;
  - implementations in `Source\NexusForever.Game\Entity\Creature`;
  - service registrations.
- Load base creature info from game tables.
- Overlay DB-backed creature property/stat overrides.
- Apply entity-level property overrides from `entity_property`.
- Update entity initialization paths to accept CreatureInfo:
  - DB-spawned static entities;
  - public event entities;
  - dynamically summoned entities;
  - command-spawned test entities, if still present.
- Preserve current `dev` entity/combat improvements when conflicts occur.

Files/modules to inspect first:

- `Source\NexusForever.Game\Entity\WorldEntity.cs`
- `Source\NexusForever.Game\Entity\CreatureEntity.cs`
- `Source\NexusForever.Game\Entity\EntityManager.cs`
- `Source\NexusForever.Game\PublicEvent\PublicEventEntityFactory.cs`
- `Source\NexusForever.Database.World\WorldDatabase.cs`

Acceptance:

- Static world entities still spawn in existing worlds.
- Entities for world `3404` load with correct creature id, display, outfit,
  factions, stats, and property overrides.
- Missing creature info fails loudly with enough context to repair data.

Estimate: 1-2 days.

## Phase 3: Script Infrastructure

Tasks:

- Port named script resolution from `entity_script.scriptName`.
- Port script filters:
  - script name filter;
  - active prop id filter;
  - dynamic filters needed by CombatAI and entity spline scripts.
- Update `IOwnedScript` variance only if needed by current script loader shape.
- Add typed script collection invocation helpers used by the branch.
- Add or update script lifecycle callbacks:
  - `OnAddToMap`;
  - `OnRemoveFromMap`, if required;
  - `OnEnterZone`;
  - `OnActivateSuccess`;
  - `OnActivateFail`;
  - unit threat callbacks.
- Add script event helpers:
  - `EntityCastEvent`;
  - `EntitySummonEvent`;
  - event factory/service registrations.
- Add template interfaces used by Evil scripts:
  - `IDoorEntityScript`;
  - `IUnitScript` changes;
  - `IWorldEntityScript` changes.

Files/modules to inspect first:

- `Source\NexusForever.Script\ScriptManager.cs`
- `Source\NexusForever.Script\Template\Collection`
- `Source\NexusForever.Script\Template\Filter`
- `Source\NexusForever.Script\Template\Event`
- `Source\NexusForever.Script\Template\IWorldEntityScript.cs`
- `Source\NexusForever.Script\Template\IUnitScript.cs`

Acceptance:

- Existing compiled map/entity scripts still load.
- DB named scripts attach only to entities whose names match compiled scripts.
- Unknown script names are logged and skipped, not fatal, unless explicitly
  configured as fatal for test.
- A test entity from `Evil from the Ether.sql` resolves its named script.

Estimate: 1-2 days.

## Phase 4: Public Event, Map, Trigger, And Teleport Support

Public event tasks:

- Port public event checklist objective support.
- Port objective data fixes from `6361e86e`.
- Port enough public event stat tracking to avoid event progress failures.
- Preserve current `dev` public-event behavior where already newer.
- Ensure content public events do not finalise prematurely if Evil depends on
  phase transitions after objective completion.

Map/entity tasks:

- Port local teleport behavior used by the expedition scripts.
- Add map/entity callbacks required by content scripts.
- Add or adapt entity summon support.
- Add required trigger entity types:
  - grid trigger;
  - volume grid trigger;
  - turnstile trigger;
  - world-location volume trigger.
- Add `ActivePropId` and `WorldSocketId` handling if not already present in
  `dev` entity create packets.

Movement tasks:

- Port only required pieces of position projectile / velocity key / forced
  movement support.
- Prefer stubbing optional non-critical movement flair over bringing the whole
  forced-movement stack early.

Acceptance:

- Map script for world `3404` loads.
- Player can be placed into world `3404`.
- Expedition interactables can trigger public event phase transitions.
- Scripted local teleports move the player without requiring a full map reload.
- Doors and active-prop-filtered scripts resolve correctly.

Estimate: 1.5-3 days.

## Phase 5: Cinematics And Communicator Messages

Tasks:

- Port Evil-specific cinematics:
  - `EvilFromTheEtherOnCreate`
  - `EvilFromTheEtherOnEthericOrganisms`
  - `EvilFromTheEtherOnOpenMedbay`
- Add matching interfaces in `NexusForever.Game.Abstract.Cinematic.Cinematics`.
- Port only the cinematic framework changes required by those classes.
- Queue opening cinematic from `EvilFromTheEtherMapScript` when a player is
  added to the map.
- Port communicator message ids and helpers used by the event script.

Acceptance:

- Entering world `3404` does not throw in cinematic creation.
- Opening cinematic or fallback narrative event triggers once per entry.
- Missing/unsupported cinematic actions log cleanly rather than crashing.

Estimate: 0.5-1 day.

## Phase 6: Evil Expedition Scripts

Files to port/adapt:

- `Source\NexusForever.Script.Instance\Expedition\AutomaticDoorEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\CommunicatorMessage.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\EvilFromTheEtherEventScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\EvilFromTheEtherMapScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\PublicEventCreature.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\PublicEventObjective.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\PublicEventPhase.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CaptainWeirTeleportGridTriggerEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CrewLogEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CrewQuatersDoorEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\EthericDriveControlsEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\EthericPortalEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\EthericPortalLargeEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\EthericPortalSmallEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\KatjaZarkhovEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\KatjaZarkhovFloatingEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\PrimaryPowerPlantDoorEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\PrimaryPowerPlantHallwayDoorEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\RavenousReaperEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\RavenousRefugeeEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\SecurityChiefKondovichEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\TeleporterControlDoorEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\TetheredOrganismEntityScript.cs`
- `Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\UpperDeckTeleportGridTriggerEntityScript.cs`

Tasks:

- Add the files and adapt namespaces/usings to current `dev`.
- Compile after every small batch of scripts.
- For each compile error, decide whether to:
  - adapt script to current `dev`;
  - port a missing infrastructure method;
  - stub optional behavior with a TODO and log.
- Keep all script-name strings aligned with `Evil from the Ether.sql`.

Acceptance:

- `NexusForever.Script.Instance` builds.
- Every `entity_script.scriptName` used by world `3404` resolves to a compiled
  script or an explicitly documented skip.
- Event phases can advance through the non-combat interaction path without
  throwing.

Estimate: 1-2 days.

## Phase 7: Combat, Spell, AI, And Encounter Closure

This is the largest uncertainty. The source branch has a sizeable spell system
and combat dependency chain. Current `dev` already has local spell/combat work,
so the port must reconcile behavior rather than blindly replace files.

Minimum tasks:

- Port enough CombatAI for expedition mobs to select targets, chase, and
  auto-attack.
- Verify threat callbacks still fire from current `UnitEntity`/`ThreatManager`.
- Support script-triggered entity spell casts used by:
  - Ravenous Reaper;
  - Security Chief Kondovich;
  - Katja Zarkhov;
  - Etheric portals;
  - Tethered Organism.
- Implement or adapt only the spell effect handlers actually used by those
  encounters.
- Add lightweight fallbacks for non-critical spell flair.
- Avoid replacing the current `dev` spell diagnostics/reverse-engineering work
  unless absolutely required.

Likely dependency areas:

- `Source\NexusForever.Game\Spell`
- `Source\NexusForever.Game\Entity\SpellManager.cs`
- `Source\NexusForever.Game\Combat`
- `Source\NexusForever.Game\Entity\UnitEntity.cs`
- `Source\NexusForever.Game\Entity\Movement`
- selected world-server spell/entity handlers

Acceptance:

- Mobs aggro and attack.
- Player damage and creature death work.
- Public event kill objectives advance.
- Encounter scripts can cast their required spells or degrade gracefully.
- No repeat server exception loop occurs when a spell effect is unsupported.

Estimate: 2-5 days.

## Phase 8: Verification And Hardening

Automated checks:

- `dotnet build Source\NexusForever.slnx`
- EF migration generation/build check for `WorldContext`
- Setup script parse check:
  `[scriptblock]::Create((Get-Content .\Tools\Setup\Initialize-NexusForever.ps1 -Raw))`
- Fresh disposable DB import:
  `.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf -RepairPartialLargeDumpDatabases`

Database checks:

```sql
SELECT COUNT(*) FROM version WHERE fileName = 'Evil from the Ether.sql';
SELECT COUNT(*) FROM entity WHERE world = 3404;
SELECT COUNT(*) FROM entity_event WHERE eventId = 781;
SELECT scriptName, COUNT(*) FROM entity_script GROUP BY scriptName ORDER BY scriptName;
SELECT COUNT(*) FROM entity_property;
SELECT COUNT(*) FROM creature_info_property;
SELECT COUNT(*) FROM creature_info_stat;
```

Manual runtime checks:

- Login and enter the game with the WildStar 16042 client.
- Enter or teleport to world `3404`.
- Confirm opening state loads without a crash.
- Confirm initial cinematic or fallback runs.
- Confirm key interactables respond.
- Confirm doors open/close through scripts.
- Confirm local teleports work.
- Confirm Ravenous Refugees start dormant and awaken at the intended phase.
- Confirm Ravenous Reaper encounter can be fought.
- Confirm Security Chief Kondovich encounter can be fought.
- Confirm Katja Zarkhov encounter can be reached and fought.
- Confirm public event objectives and phases advance.
- Confirm no unsupported script/spell warning becomes an exception loop.

Acceptance:

- Clean build.
- Clean disposable import.
- World `3404` runtime smoke passes.
- Known incomplete mechanics are documented with exact file references and
  player-visible impact.

Estimate: 2-4 days.

## Risk Matrix

High risk:

- Spell system divergence between `dev` and the source branch.
- Combat/CC/forced-movement dependencies expanding the slice.
- Dirty local work in spell/combat files overlapping the port.
- Missing map/game-table runtime assets for world `3404`.

Medium risk:

- EF migration history conflicts due source branch migration names/dates.
- WorldDatabase SQL drifting beyond the source branch runtime.
- Script-name binding accidentally enabling unsupported arbitrary scripts.
- Public event phase/objective behavior differing from source branch.

Low risk:

- Adding final schema tables for `entity_property` and creature info overrides.
- Import-script compatibility once schema exists.
- Copying Evil-specific script files after infrastructure is present.

## Explicit Non-Goals

- Do not merge the entire source branch.
- Do not port telemetry/tracing changes.
- Do not remove or restructure API, character, or friendship services.
- Do not port tutorial phase 1 unless a shared infrastructure piece is needed.
- Do not replace current `dev` spell/combat work wholesale.
- Do not fabricate retail behavior for missing spell effects; log and isolate
  gaps.
- Do not make the setup script force branch-specific world SQL into unsupported
  schemas by default.

## Suggested Milestones

Milestone A: importable data

- Schema port complete.
- `Evil from the Ether.sql` imports and records a `version` marker.
- No runtime behavior required.

Milestone B: loadable world

- World `3404` loads.
- Entities spawn.
- Named scripts attach.
- Map script runs.

Milestone C: interaction path

- Opening state works.
- Doors/interactables/local teleports work.
- Public event phases advance through non-combat objectives.

Milestone D: combat path

- Basic AI works.
- Ravenous Reaper and Security Chief Kondovich are fightable.
- Kill objectives advance.

Milestone E: full branch-equivalent slice

- Katja Zarkhov path works.
- Required cinematic, movement, CC, and spell mechanics are implemented or
  documented as intentionally degraded.
- Full manual smoke run completes without server exceptions.

## Rough Effort Estimate

- Importable DB data: 0.5-1.5 days.
- Loadable world with named scripts attached: 3-5 days total.
- Minimal playable expedition slice: 5-8 focused days total.
- Branch-equivalent working slice: 10-15 focused days total.
- Production-quality integration with regression testing: 3-4+ weeks.

The fastest useful path is Milestones A through C first. That tells us how much
of the source branch's heavy combat/spell machinery is truly necessary before
we spend days porting it.
