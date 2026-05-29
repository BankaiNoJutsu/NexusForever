# Full Decompile Pass Status (2026-05-27)

This document records the **full-program decompile cache** pass requested against the
default client binaries. It follows `CONTINUATION_GUIDE.md` evidence-ladder language and
does **not** copy decompiled client C into NexusForever source.

## Pass Parameters

| Setting | Value | Rationale |
| --- | --- | --- |
| Targets | `WildStar64.exe`, `Houston64.exe`, `StsConnLib64.MT.dll` | Default analysis set from `README.md` |
| `CacheWarmMode` | `Complete` | Fill canonical per-function cache for every internal function |
| `MaxDecompiledFunctions` | `5000`–`10000` per target | Raise focused `selected_decompiled.c` ceiling without building an unusable single file |
| `ProjectLayout` | `PerTarget` | Avoid shared-project lock contention |
| `GhidraMaxHeap` | `8G` (added to runner) | Default headless `MAXMEM=2G` caused JVM exit `-1` on large WildStar exports |

## Decompile Status (canonical cache)

| Binary | Internal functions | Canonical cache fragments | Cache % | Pass state |
| --- | ---: | ---: | ---: | --- |
| `StsConnLib64.MT.dll` | 4,522 | 4,522 | 100% | **Complete** (`CacheWarmMode Complete`, exit 0) |
| `WildStar64.exe` | 25,001 | 25,001 | 100% | **Complete** (final incremental batch with `-GhidraMaxHeap 8G`, `remainingUncached=0`) |
| `Houston64.exe` | 25,129 | 25,129 | 100% | **Complete** (`CacheWarmMode Complete`, `-GhidraMaxHeap 8G`, exit 0) |

Refresh counts:

```powershell
.\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
```

Or inspect `exports/<binary>/decompile_cache_summary.properties` and
`selected_decompiled_cache/functions/<fingerprint>/*.fragment.properties`.

## Mapping And Documentation (durable layer)

| Artifact | Purpose |
| --- | --- |
| `function_labels.csv` | 1,676 unique durable labels (`program,address,name,comment`); 30 duplicate rows removed this pass |
| `coverage/function_label_inventory.csv` | 54,652 rows — labeled: WildStar 1,613, Sts 24, Houston 25 (evidence-backed only) |
| `coverage/LATEST_COVERAGE_SUMMARY.md` | Human-readable export + **cache %** + opcode inventory |
| `INITIAL_FINDINGS.md` | Subsystem findings at Mapped/Implemented/Blocked (existing) |
| `CONTINUATION_GUIDE.md` | Workflow, evidence ladder, current map |

**Mapping policy:** Only evidence-backed rows belong in `function_labels.csv`. The full
program cache decompiles every internal function for lookup; it does **not** imply all
functions are renamed or server-implemented. Unlabeled functions remain `FUN_*` in
`functions.csv` with decompile bodies in the canonical cache only.

## Evidence Ladder State For This Pass

| Outcome | Status |
| --- | --- |
| Full-program decompile cache (all default targets) | **Mapped** — 54,652/54,652 internal functions in canonical cache (`remainingUncached=0`) |
| Durable `function_labels.csv` rows | **Mapped** — 1,676 unique evidence-backed labels; not exhaustive rename |
| Rename all ~54k functions in Git | **Rejected** — violates conservative labeling; use `function_label_inventory.csv` + targeted passes |
| NexusForever C# implementation from bulk decompile | **Blocked** — no Verified behavior from this pass |

## Commands (resume)

```powershell
# Smallest target already complete; finish WildStar then Houston sequentially:
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -ProjectLayout PerTarget `
  -Targets WildStar64.exe -CacheWarmMode Complete -MaxDecompiledFunctions 5000 `
  -DecompileMode Auto -GhidraMaxHeap 8G

.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -ProjectLayout PerTarget `
  -Targets Houston64.exe -CacheWarmMode Complete -MaxDecompiledFunctions 5000 `
  -DecompileMode Auto -GhidraMaxHeap 8G

.\Decomp\Analysis\Export-FunctionLabelInventory.ps1
.\Decomp\Analysis\Test-DecompileManifest.ps1 -FailOnMismatch
.\Decomp\Analysis\Get-DecompCoverageSnapshot.ps1
```

## Blockers

1. **Ghidra headless memory** — default 2G `MAXMEM` is insufficient for complete WildStar export; use `-GhidraMaxHeap 8G` (or higher if needed).
2. **Project locks** — stale `.lock` files after interrupted runs; clear only when no `java.exe` AnalyzeHeadless process holds the project.
3. **Parallel multi-target** — do not run parallel `Complete` passes that delete/reimport programs in the same project; use `PerTarget` sequentially or one parallel worker per isolated project.
4. **Time** — remaining WildStar/Houston cache fill is tens of minutes per binary at local throughput.

## Next Steps (CONTINUATION_GUIDE)

1. Confirm `cache.remainingUncached=0` for all three targets. **Done.**
2. Run manifest test + coverage snapshot; commit only `Decomp/Analysis/coverage/*`, `function_labels.csv`, and this status doc (not `exports/`, `logs/`, `ghidra_projects/`).
3. Continue **narrow** mapping passes (one subsystem per session) using `function_label_inventory.csv` and `selected_reasons_summary.csv` — not bulk rename.

Follow-up label promotion (2026-05-28): see `FUNCTION_LABEL_MAPPING_PASS.md`.
