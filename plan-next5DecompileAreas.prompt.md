## Plan: Next 5 Decompile Areas

Focus the next cycle on one throughput foundation area plus four high-ROI client/server boundaries: evidence-loop hardening, unresolved targeting, spell transaction/message decoding, high-volume spell-family validation, and receiver/callback graph recovery. This ordering removes the largest blockers first and then converts existing conservative runtime work into broad verified progress.

**Steps**
1. Area 1 - Tighten decompile throughput and the normalized evidence loop. Start by hardening `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1`, `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java`, and `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv`, then formalize one repeatable fixture pass across `i:\GIT\NexusForever\Tools\SpellReverseEngineering\world_context_queries.sql`, `/spell inspect4`, `/spell cast4`, diagnostics, and packet comparison. Everything else depends on having a stable export target set, durable labels, and one reusable proof loop.
2. Area 2 - Recover the unresolved target-helper chain and broader target routing. Depends on step 1. Treat this as a function-and-fixture workflow, not a generic targeting pass: decode how `FUN_1403acd90`, `FUN_1403b4ec0`, and the spell-id keyed tree at `DAT_140c65b70 + 0x788` participate in `Game.Spell:IsSelfSpell` and related targeting helpers, then validate the concrete witnesses for target types `0`, `6`, and `7` (`Spell4` `305`, `11820`, `339`, `26813`) before widening any runtime routing. Use `nexus_spell_re` for the fast focused spell mirror and `wildstar_client` as the canonical extracted-client-table schema whenever the pass needs client-side support tables that are not mirrored locally. Type `6` is currently the single mechanic-`17` test row `11820`, backed by `ItemSpecial=3777` but still lacking a concrete `item2` owner in the extracted client tables. The expected output of the area is a documented target-type decision table, explicit valid-target mask boundaries, and a proven next slice for world-target family expansion beyond the current interactable/object path.
3. Area 3 - Close the client/server spell transaction boundary. Depends on step 1 and can run parallel with step 2 once the evidence loop is stable. Decode the spell broadcast/result opcode family around `0x07F5` through `0x0818`, correlate it with `Server07F8` and the adjacent unknown models, and in the same pass map cast-context tokens, service-token flow, and the client-side transaction/prerequisite gates that affect spell and action-set legality. Keep any unproven in-flight transaction logic diagnostic-only rather than inventing server equivalents.
4. Area 4 - Promote the biggest high-volume runtime families from conservative to verified. Depends on step 1 and can run parallel with steps 2 and 3. Use the fixture loop to validate and document the highest-leverage partially implemented families already wired in the server: `Damage` / `DistanceDependentDamage` / `DistributedDamage`, `Proc`, `UnitStateSet`, `SetBusy`, `UnitPropertyModifier`, and shield/absorb/transference families. The goal is to convert thousands of rows from partial coverage into verified behavior and isolate the small formula or routing deltas that unlock the most visible gameplay parity.
5. Area 5 - Decode content receiver and callback graphs before broader content implementation. Depends on steps 2 and 3. Trace the `RavelSignal` receiver/script-consumer graph and the CSI callback-table/current-object wrapper family together so interaction-driven content can move from structural diagnostics to safe narrow hooks. Limit this area to receiver identities, payload semantics, and script entry surfaces; do not jump to full encounter scripting until those consumers are proven.

**Relevant files**
- `i:\GIT\NexusForever\plan-decompileThroughputRoadmap.prompt.md` — master sequencing reference for the decompile workflow.
- `i:\GIT\NexusForever\Spell System Progress Tracker.md` — current implementation state, row counts, and highest-priority remaining families.
- `i:\GIT\NexusForever\Spell Runtime Restoration Plan.md` — validation-loop strategy and spell-core milestone boundaries.
- `i:\GIT\NexusForever\Spell Reverse Engineering Runtime Candidates.md` — reusable fixture spells for step 4 validation.
- `i:\GIT\NexusForever\Global Spell System Reverse Engineering.md` — structural spell model and handler coverage baseline.
- `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1` — focused export entry point for step 1.
- `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java` — selection/export logic and manifest reuse behavior.
- `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv` — durable native labels to promote after each verified pass.
- `i:\GIT\NexusForever\Decomp\Analysis\INITIAL_FINDINGS.md` — durable client/decompile findings ledger.
- `i:\GIT\NexusForever\Decomp\Analysis\AREA2_TARGET_DECODE_WORKFLOW.md` — focused procedure for the unresolved target-helper chain and witness spells.
- `i:\GIT\NexusForever\Tools\SpellReverseEngineering\world_context_queries.sql` — fixture SQL for repeatable spell and object witnesses.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Spell\Spell.cs` — `Cast()`, `Execute()`, `SelectTargets()`, `ExecuteEffects()`, and `SendSpellGo()` anchor the runtime side of steps 2 through 4.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Spell\SpellEffectHandler.cs` — `HandleProc()`, world-target families, `Activate`, `SetBusy`, and `RavelSignal` anchors.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Combat\DamageCalculator.cs` — damage, shield, and mitigation order-of-operations anchor for step 4.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Entity\UnitEntity.cs` — `ProbeProcEvent()` and spell-owned combat/state surfaces used in step 4.
- `i:\GIT\NexusForever\Source\NexusForever.Game\Prerequisite\PrerequisiteManager.cs` — `Check()` anchor for spell/action-set/interact legality in step 3.
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Spell\ClientSpellCastWithServiceTokenHandler.cs` — service-token cast boundary for step 3.
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Spell\ClientRequestActionSetChangesHandler.cs` — action-set legality and client-transaction boundary for step 3.
- `i:\GIT\NexusForever\Source\NexusForever.WorldServer\Network\Message\Handler\Entity\ClientEntityInteractionHandler.cs` — interaction routing and CSI anchor for step 5.
- `i:\GIT\NexusForever\Source\NexusForever.Network\Message\GameMessageOpcode.cs` — opcode inventory for step 3.
- `i:\GIT\NexusForever\Source\NexusForever.Network.World\Message\Model\Server07F8.cs` — known spell broadcast model adjacent to the unresolved packet family.
- `i:\GIT\NexusForever\Source\NexusForever.Script\ScriptManager.cs` — script-consumer boundary reference for step 5.

**Verification**
1. After any export or label change, rerun the focused Ghidra export and confirm the intended anchor functions still appear in `selected_decompiled.c` with stable labels.
2. Promote a mapping only when at least two independent witnesses agree: client decompile call shape plus fixture, packet, or runtime evidence.
3. For step 2, validate the current target-type witnesses (`Spell4` `305`, `11820`, `339`, `26813`) before widening runtime target behavior.
4. For step 3, compare actual cast, fail, partial-target, and service-token flows against packet/model output before touching network handlers or message models.
5. For step 4, run fixture batches for proc, damage, shield, absorb, state, and busy/property families and update the tracker only when SQL shape, runtime mutation, and client-facing output agree.
6. For step 5, keep `RavelSignal` and CSI work diagnostic-only until receiver identities and payload consumers are documented in `INITIAL_FINDINGS.md` and the durable label set.
7. After each verified pass, update `INITIAL_FINDINGS.md`, `function_labels.csv`, `Spell System Progress Tracker.md`, and the focused evidence note that owns the area.

**Decisions**
- Included scope: decompile throughput, reverse-engineering, mapping, durable documentation, packet/model cleanup, and conservative runtime changes once evidence is verified.
- Excluded scope: full NavMap/LoS extraction, broad instance scripting, and expedition/NPE-specific slice implementation during this planning window.
- Reason for exclusion: NavMap/LoS remains a major long-term blocker, but without confirmed artifacts it is lower short-term incremental ROI than the five areas above.
- Ranking rationale: the chosen order removes the strongest current blockers first, then converts existing conservative spell/runtime coverage into the largest verified progress.

**Further Considerations**
1. If usable Rawaho/NavMap or richer client artifact sources surface, pull spatial research forward immediately; otherwise keep it as a parallel research track outside the top five.
2. If the next handoff optimizes for near-term gameplay parity instead of decompile throughput, move step 4 ahead of step 3 while keeping step 1 first.