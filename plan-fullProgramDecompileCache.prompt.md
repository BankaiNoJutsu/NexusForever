## Plan: Full Program Decompile Cache

Continue the recent decompile-runner planning work from the narrowest verified slice: the exporter already knows how to reuse `selected_decompiled.c` and per-function fragments, while the batch wrapper already knows how to fan out targets. The missing layer is to make `run_ghidra_analysis.ps1` own parallel target orchestration, analysis reuse, and a deduplicated canonical cache that grows across the whole program over repeated runs. Keep `selected_decompiled.c` selective, but make repeated normal runs steadily warm more internal functions until the current binary and label context is fully cached.

**Steps**
1. Reconfirm the current ownership boundary before editing. Read `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1`, `i:\GIT\NexusForever\Decomp\Analysis\Start-DecompileBatch.ps1`, `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java`, `i:\GIT\NexusForever\Decomp\Analysis\Test-DecompileManifest.ps1`, `i:\GIT\NexusForever\Decomp\Analysis\README.md`, and `i:\GIT\NexusForever\Decomp\Analysis\CONTINUATION_GUIDE.md`. Treat the batch wrapper and exporter cache as existing assets, not machinery to re-invent.
2. Add runner-level parallel orchestration to `run_ghidra_analysis.ps1`. Introduce `-MaxParallel` with a conservative default of `1`. When `MaxParallel` is greater than `1` and more than one target is supplied, launch one worker per target, bounded by `MaxParallel`, with per-target summaries and logs and `SkipCoverage` forced on worker runs.
3. Make project layout safe under parallelism. Reject `ProjectLayout Shared` when `MaxParallel > 1`, and resolve `ProjectLayout Auto` to `PerTarget` when parallel workers are requested so Ghidra project locks do not become the new bottleneck.
4. Add analysis-reuse state to the main runner. Create a machine-readable analysis manifest keyed by target path, binary fingerprint, label fingerprint, Ghidra version, project layout, and any script-version inputs that should invalidate reuse. When the target is unchanged and no force flag is set, rerun with `-process -noanalysis` instead of reimporting and reanalyzing.
5. Promote the Java fragment cache into the canonical all-functions cache. Keep `selected_decompiled.c` selective, but let `ExportNexusForeverAnalysis.java` warm cached fragments for all internal functions, not just the selected export set. Preserve the recent decision to support `off`, `incremental`, and `complete` warm modes plus a per-run warm budget.
6. Treat deduplication as a hard invariant. Use one canonical fragment per function per cache context. Reuse or migrate legacy selected-only cache data without leaving parallel duplicate fragment sets behind under multiple cache roots.
7. Surface convergence and warm-state data explicitly. Write a stable summary that reports `totalInternalFunctions`, `canonicalCachedFragments`, `warmedThisRun`, `reusedExistingFragments`, `remainingUncached`, and any dedup or migration decisions so later sessions can see whether the cache is actually converging.
8. Collapse wrapper duplication after the main runner lands. Keep `Start-DecompileBatch.ps1` as a compatibility shim that delegates to the main runner rather than maintaining a second orchestration path with its own rules.
9. Update the docs and validation path. `README.md` and `CONTINUATION_GUIDE.md` should distinguish analysis reuse, selected-output reuse, and full-program cache warming, while `Test-DecompileManifest.ps1` and `Get-DecompCoverageSnapshot.ps1` should remain valid under the upgraded flow.

**Relevant files**
- `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1`
- `i:\GIT\NexusForever\Decomp\Analysis\Start-DecompileBatch.ps1`
- `i:\GIT\NexusForever\Decomp\Analysis\Test-DecompileManifest.ps1`
- `i:\GIT\NexusForever\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1`
- `i:\GIT\NexusForever\Decomp\Analysis\README.md`
- `i:\GIT\NexusForever\Decomp\Analysis\CONTINUATION_GUIDE.md`
- `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java`

**Verification**
1. Run one export-only pass on a prepared target and confirm the runner writes fresh analysis and cache-warm summaries.
2. Run the same target again without changing the binary or labels and confirm the runner chooses analysis reuse instead of a full reanalysis.
3. Run repeated incremental warm passes and confirm `canonicalCachedFragments` rises while `remainingUncached` falls monotonically.
4. Spot-check that unchanged functions are read from the canonical cache and that no duplicate fragment sets are left behind for the same function and cache context.
5. Run a bounded parallel pass across multiple targets and confirm per-target workers succeed under `PerTarget` layout, then run coverage once after all workers finish.
6. Re-run `i:\GIT\NexusForever\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch` after the cache changes land.

**Decisions**
- Default operating mode: incremental full-program warming with a per-run budget.
- Keep `selected_decompiled.c` selective; do not turn normal runs into all-functions dumps.
- Treat deduplication as required behavior, not optional cleanup.
- Make `run_ghidra_analysis.ps1` the authoritative entry point and keep the batch wrapper as a compatibility shim.

**Further Considerations**
1. If the canonical cache directory name changes, add a one-time import or migration path so existing selected-only fragments are reused instead of stranded.
2. If the main binary still warms too slowly, add a target-priority or per-binary warm budget before broadening the default budget.