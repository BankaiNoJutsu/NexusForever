## Plan: Evidence-Backed Restoration Epic

## Execution Workboard - Updated 2026-05-20

This file is now both the plan and the current execution ledger. The epic is
not considered complete by deleting every placeholder name; it is complete only
when all implementation-ready items have either landed with verification or
been explicitly blocked by the evidence ladder.

### Phase 0 Baseline

| Area | Current status | Evidence / gate | Next action |
| --- | --- | --- | --- |
| Source placeholder inventory | Complete for this pass | Source scan currently finds 145 distinct `Unknown*` tokens, 21 numbered `Data*`/`DataBits*` tokens, 17 `Client0x*` opcode names, and 120 `Server0x*` opcode names. | Keep the names unless a row below marks the exact item safe. |
| Opcode placeholders | Blocked | `Decomp/Analysis/coverage/LATEST_COVERAGE_SUMMARY.md` lists all missing client/server opcodes as semantics unresolved and no placeholder models to decode. | Do not rename `Client0x*` or `Server0x*` from payload size alone. |
| Quest objective `Unknown10` | Implemented | Existing runtime already used it as both target-group-backed and checklist-backed; `Q3487Shellshock` updates it by activated entity and checklist index. | Renamed to `ScriptedTargetGroupChecklist`; build passed. |
| Tutorial quest direction tables | Implemented and tested | `QuestDirection` and `QuestDirectionEntry` are loaded as `[GameData]` and `Quest.SendObjectiveWorldLocationUpdates()` resolves direction-derived world locations before indicator fallback. | Covered by `GameTableManagerGameDataContractTests`. |
| NPE kill loop | Runtime-smoked, pending manual quest playthrough | `UnitEntity` kill/respawn/reward support was already present; `NewPlayerExperience.nfmap` is staged. A local stack using `I:\WildStar` entered world `3460` through the load-test login-to-world path with no fresh `ERROR`/`FATAL` log entries. | Manual client smoke still needs quest acceptance/progression, first kill objectives, rewards, and respawn observation. |
| Evil from the Ether expedition | Implemented and staged, pending manual expedition playthrough | Branch notes record schema/model support, script filters, public-event phase controller, and expedition scripts. World `3404` DB import is verified, and map generation produced `ShiphandHungerFromtheVoid.nfmap`. | Start local stack with the staged assets, then manually enter/teleport to world `3404` and verify public event `781`, doors, interactables, teleports, phase transitions, and encounters. |
| Spell families marked conservative | Implemented conservative boundary | `Spell System Progress Tracker.md` and `Spell Effect Evidence Matrix.md` list many families as wired conservatively but needing fixture/sniff validation. | Add regression/fixture coverage or live evidence before widening semantics. |
| DataBits and EF placeholders | Mostly blocked; current migration blockers resolved | Spell `DataBits*` require stable family semantics across fixtures; generic EF/store/account placeholder renames still require packet contract proof. The current Character/World migration metadata and snapshots now converge and local DB validation passed. | Rename only after the owning family reaches mapped/verified. |

### Completed In This Execution

- Renamed `QuestObjectiveType.Unknown10` to
  `QuestObjectiveType.ScriptedTargetGroupChecklist` and updated all runtime and
  script callers.
- Recorded the evidence in `Decomp/Analysis/INITIAL_FINDINGS.md`.
- Verified the script/game compile path with
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj --no-restore -v minimal --nologo`.
- Added focused regression coverage for the `QuestDirection` and
  `QuestDirectionEntry` default game-table loading contract.
- Fixed `MapGenerator` runtime extraction by letting `Nexus.Archive` use its
  compatible `SharpCompress` dependency, then generated the world `3404`
  `ShiphandHungerFromtheVoid.nfmap` asset from `I:\WildStar\Patch`.
- Fixed portable RabbitMQ startup for reused local volumes by repairing
  `.erlang.cookie` and RabbitMQ data ownership before readiness checks.
- Added WorldServer EF design-time package coverage plus aligned Roslyn
  workspace package pins so `dotnet ef` can run under the local SDK.
- Completed Character EF migration metadata for `ItemSoulbound`,
  `CharacterTradeskill`, and `CharacterSchematicArchive`, refreshed the
  Character snapshot, and refreshed the World snapshot for already-applied loot
  schema migrations.
- Added runtime game-table contracts for `ItemRandomStat`,
  `ItemRandomStatGroup`, `ArchiveArticle`, `ArchiveEntry`, and
  `ArchiveEntryUnlockRule`.
- Fixed item static-info startup by accumulating duplicate property budgets
  instead of throwing on repeated random-stat properties.
- Fixed the login-world load-test Auth packet by writing GUID `Data2` and
  `Data3` as 16-bit fields, and added explicit `ServerAuthDenied` reporting.
- Applied the local Character migrations and verified `item.soulbound`,
  `character_tradeskill`, `character_schematic`, and
  `character_galactic_archive` exist in `nexus_forever_character`.
- Verified world import state locally: `Evil from the Ether.sql` is recorded in
  `version`, world `3404` has `237` `entity` rows and `12` `entity_script`
  rows, and world `3460` has `431` `entity` rows.
- Started the full local standalone stack with staged table/map assets,
  confirmed ports `23115`, `6600`, `24000`, and `5672`, then ran
  `dotnet run --project Tools\NexusForever.LoadTest -- run login-world --user loadtest0001@example.local --password loadtest --samples 1 --warmup 0`
  successfully. The final post-fix run wrote
  `artifacts\load-tests\login-world-20260520-181053.json` with `1/1`
  success, entered world `3460`, and a fresh log scan found no `ERROR` or
  `FATAL` entries.
- Verified the focused game-table contract with
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter GameTableManagerGameDataContractTests -v minimal --nologo`.
- Verified the full game test project with
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  (`316` tests passed).
- Verified convergence with
  `dotnet build Source\NexusForever.sln -v minimal --nologo`
  (`0` warnings, `0` errors).

### Active Implementation Slice

The current code-backed slice is complete and verified through automated
login-to-world runtime smoke. Remaining gameplay work is manual client
playthrough validation for tutorial quest steps in world `3460` and expedition
mechanics in world `3404`, plus evidence-blocked renames/runtime widenings that
still require new captures, sniffs, table fixtures, or spell-family evidence.

### Local Runtime Paths

- WildStar client root: `I:\WildStar`
- Client executable: `I:\WildStar\Client64\WildStar64.exe`
- Patch/data source: `I:\WildStar\Patch`
- Default generated table assets: `I:\GIT\NexusForever\.nexusforever-runtime\assets\tbl`
- Default generated map assets: `I:\GIT\NexusForever\.nexusforever-runtime\assets\map`
- Official world database checkout: `I:\GIT\NexusForever.WorldDatabase`

### Completion Boundary

The epic is fully implemented to the current evidence ladder: every
implementation-ready backend, setup, migration, asset, and automated runtime
item has landed or was already present and verified. All remaining work is
explicitly blocked on manual client playthrough evidence, packet/sniff
evidence, stable spell-family fixtures, or future placeholder-specific EF
contract proof.

### Blocker Unblock Plan

Use `I:\WildStar` as the canonical local client path for all smoke commands.

1. Runtime asset unblock:
   - Confirm `I:\WildStar\Patch` is readable.
   - Regenerate or refresh staged tables/maps from the local client patch when
     an asset is missing or stale:

     ```powershell
     .\Tools\Setup\Start-NexusForeverLocal.ps1 `
       -ClientDirectory "I:\WildStar" `
       -WorldDatabasePath "I:\GIT\NexusForever.WorldDatabase" `
       -RegenerateAssets `
       -SkipClientLaunch
     ```

   - If a generated asset set is already known-good, reuse it explicitly:

     ```powershell
     .\Tools\Setup\Start-NexusForeverLocal.ps1 `
       -ClientDirectory "I:\WildStar" `
       -ExistingTableDirectory "I:\GIT\NexusForever\.nexusforever-runtime\assets\tbl" `
       -ExistingMapDirectory "I:\GIT\NexusForever\.nexusforever-runtime\assets\map" `
       -WorldDatabasePath "I:\GIT\NexusForever.WorldDatabase" `
       -SkipClientLaunch
     ```

2. World `3460` NPE smoke unblock:
   - `NewPlayerExperience.nfmap` is present in the staged runtime assets.
   - Start the local stack with the real client path:

     ```powershell
     .\Tools\Setup\Start-NexusForeverLocal.ps1 `
       -ClientDirectory "I:\WildStar" `
       -ExistingTableDirectory "I:\GIT\NexusForever\.nexusforever-runtime\assets\tbl" `
       -ExistingMapDirectory "I:\GIT\NexusForever\.nexusforever-runtime\assets\map" `
       -WorldDatabasePath "I:\GIT\NexusForever.WorldDatabase"
     ```

   - Verify a fresh character can enter world `3460`, accept the tutorial
     quests, advance the first movement/kill objectives, receive rewards, and
     observe non-player respawn.

3. World `3404` Evil from the Ether smoke unblock:
   - World `3404` map generation produced
     `I:\GIT\NexusForever\.nexusforever-runtime\assets\map\ShiphandHungerFromtheVoid.nfmap`.
   - Confirm `I:\GIT\NexusForever.WorldDatabase\Instance\Expedition\Evil from the Ether.sql`
     imports, `version` records it, and world `3404` has nonzero `entity` and
     `entity_script` rows.
   - Start the local stack with `I:\WildStar`, enter or teleport to world
     `3404`, and verify public event `781`, script bindings, doors,
     interactables, teleports, phase transitions, and core encounters run
     without server exceptions.

4. Spell/runtime validation unblock:
   - Use `gm` or `admin` with the `I:\WildStar` client path.
   - Follow the tracker-specific commands in `Proc System Unblock Path.md` and
     `Spell Effect Evidence Matrix.md`: `!spell inspect4`, `!spell cast4`,
     `!spell capturenext`, `!spell diag4`, `!spell procreport`, and focused
     fixture captures.
   - Promote a conservative family only when the captured JSON/log evidence
     proves the wider behavior; keep unsupported proc targetData tails,
     non-zero `ShieldOverload` payloads, RavelSignal receiver graphs, and
     stack-group arbitration blocked until then.

5. Opcode and packet placeholder unblock:
   - For each `Client0x*` or `Server0x*`, require handler registration,
     client reader/writer tracing, sniff corroboration, or direct decompile
     consumer evidence.
   - After a rename, rerun
     `Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs`
     plus the relevant serialization tests.

6. EF/data placeholder unblock:
   - For EF placeholders such as `AccountInventoryModel.Unknown1`, require a
     mapped packet contract plus migration proof: model rename, configuration
     or context update, migration, designer, snapshot, and disposable database
     validation.
   - For spell `DataBits*`/`DataXX` names, require stable semantics across a
     family fixture set, not one witness row.

Build a single master epic that first converts already-mapped gameplay and runtime work into verified implementations, then renames only the placeholders whose semantics are proven, and finally mines the remaining codebase for additional implementation-ready gaps. This keeps the effort aligned with the repo’s evidence ladder and avoids turning unknown names into misleading names.

**Steps**
1. Phase 0 — Baseline the backlog and evidence gates.
   Capture a canonical placeholder inventory from Source plus tests and bucket every UnknownXXX, DataXXX, Client0x and Server0x, and EF placeholder into one of: safe-to-rename now, blocked on evidence, or blocked on migration/runtime contract.
   Use i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs, the packet model folders under Source\NexusForever.Network.World\Message\Model, i:\GIT\NexusForever\Source\NexusForever.Game.Static\Quest\QuestObjectiveType.cs, i:\GIT\NexusForever\Source\NexusForever.Game.Static\Prerequisite\PrerequisiteType.cs, i:\GIT\NexusForever\Source\NexusForever.Game.Static\Entity\EntityCreateFlag.cs, i:\GIT\NexusForever\Source\NexusForever.Database.Auth\Model\AccountInventoryModel.cs, and i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs as the initial inventory seed.
   In parallel with the placeholder inventory, build a master missing-feature backlog from i:\GIT\NexusForever\Spell System Progress Tracker.md, i:\GIT\NexusForever\Spell Effect Evidence Matrix.md, i:\GIT\NexusForever\Proc System Unblock Path.md, i:\GIT\NexusForever\Decomp\Analysis\coverage\LATEST_COVERAGE_SUMMARY.md, i:\GIT\NexusForever\Plan Playable Final-Live NPE PvE Slice.md, i:\GIT\NexusForever\Plan Riders Reef Tutorial Recovery via Compare and Decomp.md, and i:\GIT\NexusForever\Plan Evil From The Ether Expedition Port.md.
   Output of this phase: one workboard matrix with item, evidence source, owning code surface, verification path, and status. All later steps depend on this baseline.
2. Phase 1 — Implement mapped generic runtime behavior first. Depends on 1.
   Prioritize high-leverage spell and runtime families already marked implemented, partial, or conservative with concrete remaining work: Proc validation and dispatch widening, shield and transference validation, SapVital, ClampVital, UnitStateSet, SetBusy, Absorption and HealingAbsorption, cooldown and charge families, immunity families, and small utility families that already have diagnostics and fixtures.
   Use i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Effect\SpellEffectInterpretation.cs as the semantic gate, i:\GIT\NexusForever\Source\NexusForever.Game\Spell\SpellEffectHandler.cs as the mutation surface, and i:\GIT\NexusForever\Source\NexusForever.Game\Spell\SpellEffectDiagnostics.cs plus i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Spell.RuntimeEvidence.cs as the verification surface. Do not widen past the current conservative-boundary notes without fresh evidence.
   Add or update xUnit coverage in i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Spell\ProcRuntimeEvidenceCollectorTests.cs and other affected Spell tests whenever a mapped family moves from conservative or diagnostic to supported behavior.
3. Phase 2 — Implement mapped non-spell gameplay slices. Depends on 1 and can run in parallel with 2 once the baseline is complete.
   NPE world 3460: complete the kill loop by wiring creature respawn, kill objective progression, public-event linkage where applicable, and supported reward-path gaps that are already evidenced.
   Riders Reef tutorial: close the quest-direction and objective-world-location preference gaps, CSI progression gaps, and hoverboard or projector reliability issues where diagnostics or decomp evidence already exist.
   Evil from the Ether expedition: finish runtime smoke validation and close any implementation-ready script or infrastructure gaps found during world 3404 smoke tests; treat unsupported spell or content behaviors as isolated blockers, not as reasons to widen unrelated runtime behavior.
   Use i:\GIT\NexusForever\Source\NexusForever.Game\Quest\Quest.cs, i:\GIT\NexusForever\Source\NexusForever.Game\Entity\QuestManager.cs, i:\GIT\NexusForever\Source\NexusForever.Game\Entity\UnitEntity.cs, i:\GIT\NexusForever\Source\NexusForever.Game\Entity\WorldEntity.cs, and i:\GIT\NexusForever\Source\NexusForever.Game\AssetManager.cs as the main code anchors.
4. Phase 3 — Rename low-risk, evidence-backed placeholders that are already identified by Phases 1 and 2. Depends on the specific item being proven by earlier phases.
   Start with static enums and flags whose meaning is now evidenced and whose blast radius is limited: QuestObjectiveType Unknown values, PrerequisiteType Unknown values, and EntityCreateFlag.Unknown08 only if client or decomp evidence clears the blocker.
   Update every runtime caller, script-facing consumer, and related tests in the same change so reflection or data-driven lookups do not silently drift.
   Keep a blocked list for anything still semantically open; unresolved names do not get normalized just for aesthetics.
5. Phase 4 — Rename network and packet placeholders only where protocol semantics are proven. Depends on 1 and item-level evidence; can run in parallel with late Phase 3 work.
   Rename GameMessageOpcode Client0x and Server0x entries only after handler registration, client consumer tracing, sniff corroboration, or decomp-backed receiver evidence is strong enough to survive future audits.
   Rename packet and model Unknown fields only after read and write order, bit width, and packet intent are proven. Use i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs as a required contract gate for storefront, account, and world packet changes.
   Leave unresolved packet shapes as placeholders; do not replace Unknown with guessed reserved or field names unless the evidence policy explicitly permits it.
6. Phase 5 — Rename GameTable DataXXX and EF placeholders where semantics are proven. Depends on 1, 2, or 4 depending on the data family.
   Spell and effect DataBits or DataXX fields only become rename candidates after i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Effect\SpellEffectInterpretation.cs and runtime fixtures demonstrate a stable semantic meaning across a family, not a single spell witness.
   Database placeholders such as i:\GIT\NexusForever\Source\NexusForever.Database.Auth\Model\AccountInventoryModel.cs or storefront field placeholders require the full EF path: model rename, context or model configuration update, new migration, migration designer, and model snapshot update. Treat immutable migration history as part of the work, not cleanup after the fact.
7. Phase 6 — Mine the remaining codebase for missing features that became implementation-ready during earlier work. Depends on 1 through 5.
   Re-scan i:\GIT\NexusForever\Decomp\Analysis\coverage\LATEST_COVERAGE_SUMMARY.md, repo plans, spell trackers, blocker memories, TODOs, diagnostic-only branches, and missing-model opcodes to find items that moved from blocked to mapped because prerequisites landed earlier in the epic.
   Classify each new gap as generic runtime, content and runtime integration, packet and model gap, or data and migration gap.
   Implement only the items that now satisfy the evidence-backed threshold; record the rest as explicit follow-on blockers instead of stretching this epic into speculative behavior.
8. Phase 7 — Finish convergence, hardening, and regression control. Depends on all prior implementation phases.
   Run project builds at the narrowest useful scope after each tranche, then run a full-solution or broad cross-project build when the rename and feature streams converge.
   Expand regression coverage for every area that changed behavior or public model naming: spell evidence tests, network packet tests, quest or tutorial regression tests, reward, account, and loot evidence collectors, and any migration validation checks needed for EF work.
   Re-run the repo’s documented manual verification loops for spell captures, NPE and tutorial flows, and expedition smoke tests before calling the epic complete.

**Relevant files**
- i:\GIT\NexusForever\Spell System Progress Tracker.md — primary ranked backlog for mapped spell and runtime work, remaining-family notes, and verification loop.
- i:\GIT\NexusForever\Spell Effect Evidence Matrix.md — family-by-family evidence strength, regression fixtures, and diagnostic boundaries.
- i:\GIT\NexusForever\Proc System Unblock Path.md — concrete proc validation workflow and conservative routing rules.
- i:\GIT\NexusForever\Decomp\Analysis\coverage\LATEST_COVERAGE_SUMMARY.md — authoritative missing opcode and model inventory for Client0x and Server0x placeholders.
- i:\GIT\NexusForever\Plan Playable Final-Live NPE PvE Slice.md — implementation-ready NPE world 3460 gaps and non-goals.
- i:\GIT\NexusForever\Plan Riders Reef Tutorial Recovery via Compare and Decomp.md — tutorial-specific blockers around quest direction, CSI, and hoverboard or projector behavior.
- i:\GIT\NexusForever\Plan Evil From The Ether Expedition Port.md — branch-level expedition backlog and smoke-test gates.
- i:\GIT\NexusForever\Source\NexusForever.Game\Spell\SpellEffectHandler.cs — main spell effect mutation surface for mapped families.
- i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Effect\SpellEffectInterpretation.cs — semantic interpretation layer and evidence fence for DataBits and family meaning.
- i:\GIT\NexusForever\Source\NexusForever.Game\Spell\SpellEffectDiagnostics.cs — runtime trace surface used to validate new spell behavior and conservative boundaries.
- i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Spell.RuntimeEvidence.cs — structured evidence export and spell-runtime artifact surface.
- i:\GIT\NexusForever\Source\NexusForever.Game\Quest\Quest.cs — objective-world-location resolution, tutorial guidance, and quest-direction behavior.
- i:\GIT\NexusForever\Source\NexusForever.Game\Entity\QuestManager.cs — quest advancement and reward-path integration surface.
- i:\GIT\NexusForever\Source\NexusForever.Game\Entity\UnitEntity.cs — creature lifecycle, death, and respawn anchor for NPE and world-content closure.
- i:\GIT\NexusForever\Source\NexusForever.Game\Entity\WorldEntity.cs — shared world-object spell and interaction state used by activation-object flows.
- i:\GIT\NexusForever\Source\NexusForever.Game\AssetManager.cs — script and data bridge that currently touches objective and type placeholder values.
- i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs — Client0x and Server0x rename surface.
- i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Network\PacketPlaceholderNamingTests.cs — mandatory packet naming and serialization contract gate.
- i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Spell\ProcRuntimeEvidenceCollectorTests.cs — existing evidence-collector tests to extend for proc and runtime changes.
- i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Account\Inventory\AccountRuntimeEvidenceTests.cs — pattern for evidence-backed account and runtime changes and placeholder-sensitive regressions.
- i:\GIT\NexusForever\Source\NexusForever.Game.Tests\Loot\LootRuntimeEvidenceTests.cs — pattern for gameplay evidence collectors and export validation.
- i:\GIT\NexusForever\Source\NexusForever.Game.Static\Quest\QuestObjectiveType.cs — low-risk Unknown enum rename surface.
- i:\GIT\NexusForever\Source\NexusForever.Game.Static\Prerequisite\PrerequisiteType.cs — low-risk Unknown enum rename surface.
- i:\GIT\NexusForever\Source\NexusForever.Game.Static\Entity\EntityCreateFlag.cs — blocked rename candidate requiring evidence.
- i:\GIT\NexusForever\Source\NexusForever.Database.Auth\Model\AccountInventoryModel.cs — EF rename candidate that requires migration coordination.

**Verification**
1. Rebuild the owning projects after each tranche, at minimum NexusForever.Game.csproj for gameplay changes, the relevant Network or Database project when placeholder families move there, and the full solution once cross-project renames converge.
2. Run the full Game test project after any behavior tranche, plus focused filters for Network, Spell, Reward, Loot, Account, or Quest areas when iterating inside a single stream.
3. For spell and runtime work, use the repo’s live evidence loop: spell inspect, cast, capturenext, and diag commands, proc report export, and the JSON artifacts under artifacts\verify to confirm that runtime behavior and diagnostics match the tracker documents before promoting a family from diagnostic-only or conservative status.
4. For NPE, Riders Reef, and Evil expedition work, run local world and database validation plus manual smoke flows in worlds 3460 and 3404 so quest progression, spawn and reset behavior, scripts, and interaction gates are checked end to end rather than only through unit tests.
5. For packet and opcode renames, rerun placeholder and naming tests and verify that every renamed opcode or field still round-trips the same binary shape; no packet rename is complete until serialization order, size, and related sniff evidence all remain correct.
6. For EF rename work, generate and review the migration, designer, and model snapshot together, then validate against a local database before merging the rename.

**Decisions**
- Included scope: executable code under Source, xUnit tests, and EF migrations or snapshots when a rename touches persisted models.
- Excluded scope: wholesale renaming of decomp labels, markdown docs, repo memories, and root planning files; those remain input evidence unless a later follow-up explicitly widens scope.
- Rename policy: evidence-backed only. Unknown, Data, 0x, and Server placeholders stay in place when semantics are still unresolved.
- Priority policy: implement mapped behavior first, then use the new evidence to unlock safe renames.
- Delivery model: single master epic checklist executed in phases, not an interleaved free-for-all.

**Further Considerations**
1. Treat stack-group arbitration, broad non-player persistence, unsupported proc targetData tails, RavelSignal receiver graphs, and non-zero ShieldOverload payloads as explicit blockers unless new evidence arrives during the epic; they are not safe default implementation targets today.
2. If the epic needs to be subdivided for execution, split it into three parallel streams after Phase 1: generic spell and runtime, non-spell gameplay slices, and evidence-backed low-risk rename work.
