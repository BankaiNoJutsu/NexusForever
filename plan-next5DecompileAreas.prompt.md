## Plan: Next 5 Decompile Areas

Focus the next cycle on five active areas now that the export/evidence loop and the target-helper recovery are no longer the main blockers: spell broadcast/message decoding, cast-context and prerequisite boundaries, high-volume spell-family validation, receiver/callback graph recovery, and NavMap/LoS artifact search. This ordering keeps the next pass centered on durable client/server boundaries instead of reworking areas that are already mapped.

**Steps**
1. Area 1 - Decode the spell broadcast and result opcode family around `0x07F5` through `0x0818`. Treat this as the active next decomp slice. Center the pass on `i:\GIT\NexusForever\Decomp\Analysis\SPELL_BROADCAST_ROADMAP.md`, `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\Server07F8.cs`, and the adjacent unknown models such as `Server07FB`, `Server0811`, `Server0814`, and `Server0816` through `Server0818`. The expected output is a lifecycle map that explains which follow-up packets correspond to hit, miss, partial-target, and chained spell outcomes without enabling any unproven runtime send paths.
2. Area 2 - Close the cast-context, service-token, and prerequisite/action-set boundary. Depends on area 1's evidence loop and can run in parallel once the packet comparison pass is stable. Recover the cast-context token lifecycle, align it with `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Spell\ClientSpellCastWithServiceTokenHandler.cs`, and extend the same pass through `PrerequisiteManager` plus action-set legality checks. Keep token reuse, rapid-transport payment, and failed prerequisite behavior diagnostic-only until the client path and runtime evidence agree.
3. Area 3 - Promote the biggest high-volume runtime families from conservative to verified. Use the existing fixture loop to validate and document the highest-leverage partially implemented families already wired in the server: `Proc`, `Damage` / `DistanceDependentDamage` / `DistributedDamage`, `UnitStateSet`, `SetBusy`, `UnitPropertyModifier`, and shield/absorb/transference families. The goal is to convert thousands of rows from partial coverage into verified behavior once the broadcast and transaction boundaries stop masking their outcomes.
4. Area 4 - Decode content receiver and callback graphs before broader content implementation. Trace the `RavelSignal` receiver/script-consumer graph and the CSI callback-table/current-object wrapper family together so interaction-driven content can move from structural diagnostics to safe narrow hooks. Limit this area to receiver identities, payload semantics, and script entry surfaces; do not jump to full encounter scripting until those consumers are proven.
5. Area 5 - Start the NavMap and LoS artifact search plus a minimal extractor prototype. Keep this as a parallel research lane while the higher-ROI spell and interaction boundaries advance. The expected output is either a usable artifact source or a smallest-viable extraction path that can be validated against one narrow obstruction or pathing fixture before any broader spatial work.

**Relevant files**
- `i:\GIT\NexusForever\plan-decompileThroughputRoadmap.prompt.md` — master sequencing reference for the decompile workflow.
- `i:\GIT\NexusForever\Spell System Progress Tracker.md` — current implementation state, row counts, and highest-priority remaining families.
- `i:\GIT\NexusForever\Spell Runtime Restoration Plan.md` — validation-loop strategy and spell-core milestone boundaries.
- `i:\GIT\NexusForever\Spell Reverse Engineering Runtime Candidates.md` — reusable fixture spells for step 3 validation.
- `i:\GIT\NexusForever\Global Spell System Reverse Engineering.md` — structural spell model and handler coverage baseline.
- `i:\GIT\NexusForever\Decomp\Analysis\SPELL_BROADCAST_ROADMAP.md` — focused procedure and evidence boundary for the `0x07F5..0x0818` spell follow-up family.
- `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1` — focused export entry point for the broadcast and token/prerequisite passes.
- `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java` — selection/export logic and manifest reuse behavior.
- `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv` — durable native labels to promote after each verified pass.
- `i:\GIT\NexusForever\Decomp\Analysis\INITIAL_FINDINGS.md` — durable client/decompile findings ledger.
- `i:\GIT\NexusForever\Tools\SpellReverseEngineering\world_context_queries.sql` — fixture SQL for repeatable spell and object witnesses.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Spell.cs` — `Cast()`, `Execute()`, `SelectTargets()`, `ExecuteEffects()`, and `SendSpellGo()` anchor the runtime side of steps 1 through 3.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Spell\SpellEffectHandler.cs` — `HandleProc()`, world-target families, `Activate`, `SetBusy`, and `RavelSignal` anchors.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Combat\DamageCalculator.cs` — damage, shield, and mitigation order-of-operations anchor for step 3.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Entity\UnitEntity.cs` — `ProbeProcEvent()` and spell-owned combat/state surfaces used in step 3.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Prerequisite\PrerequisiteManager.cs` — `Check()` anchor for spell/action-set/interact legality in step 2.
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Spell\ClientSpellCastWithServiceTokenHandler.cs` — service-token cast boundary for step 2.
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Spell\ClientRequestActionSetChangesHandler.cs` — action-set legality and client-transaction boundary for step 2.
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Entity\ClientEntityInteractionHandler.cs` — interaction routing and CSI anchor for step 4.
- `i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs` — opcode inventory for steps 1 and 2.
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\Server07F8.cs` — known spell broadcast model adjacent to the unresolved packet family.
- `i:\GIT\NexusForever\Source\NexusForever.Script\ScriptManager.cs` — script-consumer boundary reference for step 4.
- `i:\GIT\NexusForever\deep-research-report.md` — current blocker synthesis tying spell broadcasts, prerequisites, CSI, and spatial gaps together.

**Verification**
1. After any export or label change, rerun the focused Ghidra export and confirm the intended anchor functions still appear in `selected_decompiled.c` with stable labels.
2. Promote a mapping only when at least two independent witnesses agree: client decompile call shape plus fixture, packet, or runtime evidence.
3. For step 1, compare actual cast, fail, partial-target, and follow-up spell lifecycle behavior against the `0x07F5..0x0818` placeholder models before touching runtime send paths.
4. For step 2, verify cast-context tokens, service-token flows, rapid-transport cost handling, and failed prerequisite branches against both client parse/send paths and runtime evidence before touching handlers or packet models.
5. For step 3, run fixture batches for proc, damage, shield, absorb, state, and busy/property families and update the tracker only when SQL shape, runtime mutation, and client-facing output agree.
6. For step 4, keep `RavelSignal` and CSI work diagnostic-only until receiver identities and payload consumers are documented in `INITIAL_FINDINGS.md` and the durable label set.
7. After each verified pass, update `INITIAL_FINDINGS.md`, `function_labels.csv`, `Spell System Progress Tracker.md`, and the focused evidence note that owns the area.

**Decisions**
- Included scope: decompile throughput, reverse-engineering, mapping, durable documentation, packet/model cleanup, and conservative runtime changes once evidence is verified.
- Excluded scope: full encounter scripting, broad expedition/NPE-specific slice implementation, and any speculative runtime behavior beyond the currently mapped packet and spell boundaries.
- Reason for exclusion: the five areas above still offer better evidence yield and safer verification than broad gameplay work, even though NavMap/LoS has now been pulled into the active research queue.
- Ranking rationale: the chosen order removes the strongest current blockers first, then converts existing conservative spell/runtime coverage into the largest verified progress.

**Further Considerations**
1. If usable Rawaho/NavMap or richer client artifact sources surface, pull spatial research forward immediately; otherwise keep it as a parallel research track outside the top five.
2. If the next handoff optimizes for near-term gameplay parity instead of decompile throughput, move step 3 ahead of step 2 while keeping step 1 first.