## Plan: Expand Export Interesting Strings

Continue the recent exporter-selection work from the actual control point: `ExportNexusForeverAnalysis.java` decides which string xrefs enter the focused export set through `INTERESTING_STRING_KEYWORDS`, `HIGH_VALUE_STRING_PATTERNS`, and `writeStringsAndXrefs()`. The recent session outcome was that some instance table-loader anchors are already selected today, but the focused export still misses high-value instance UI and instance-state strings that are clearly present in the cache and would surface better candidate functions if promoted into the interesting-string path.

**Steps**
1. Start from the selector, not the whole pipeline. Read `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java` around `INTERESTING_STRING_KEYWORDS`, `HIGH_VALUE_STRING_PATTERNS`, and `writeStringsAndXrefs()`.
2. Reconfirm the current misses against live artifacts before editing. Compare `interesting_strings.csv`, `string_xrefs.csv`, and `selected_xrefs.csv` with `strings.csv` and the current `selected_decompiled_cache` fragments so each new pattern is backed by a concrete miss rather than guesswork.
3. Promote only the high-value patterns that the recent cache pass already exposed. Prioritize exact or near-exact instance UI and instance-state anchors such as `ShowInstanceGameModeDialog`, `HideInstanceGameModeDialog`, `bRemovesSingleInstance`, `strSavedInstanceId`, `SetInstanceSettings`, `GetInstanceSettings`, `ResetSingleInstance`, and `OnClosedInstanceSettings`.
4. Keep the widening conservative. Prefer additions to `HIGH_VALUE_STRING_PATTERNS` over broad keyword inflation unless a repeated family genuinely needs a new keyword. Avoid adding generic terms like `instance`, `adventure`, or `dungeon` unless the focused exact strings fail to pull the intended family into the export.
5. Treat current table-loader coverage as baseline, not a new gap. The recent session already confirmed that `InstancePortal`, `Creature2Difficulty`, and `Quest2Difficulty` families are visible enough to inspect; do not reopen those as exporter misses unless the label pass still needs them.
6. Re-export `WildStar64.exe` in export-only mode and inspect the delta in `interesting_strings.csv`, `string_xrefs.csv`, `selected_xrefs.csv`, `selected_reasons_summary.csv`, and `selected_decompiled.c`. Confirm the new patterns surface additional candidate functions instead of just adding noise.
7. Promote conservative labels and findings only when call shape is clear. The last session found table families and helper clusters around instance portal and difficulty tables; continue that labeling pass, but do not overname ambiguous no-argument helpers or accessors whose role is still blocked.
8. Record durable outcomes in `function_labels.csv` and `INITIAL_FINDINGS.md`, including which anchors are now intentionally selected by the exporter so later sessions do not repeat the same cache-vs-export comparison.

**Relevant files**
- `i:\GIT\NexusForever\Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\strings.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\interesting_strings.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\string_xrefs.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_xrefs.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_reasons_summary.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled.c`
- `i:\GIT\NexusForever\Decomp\Analysis\exports\WildStar64.exe\selected_decompiled_cache\functions`
- `i:\GIT\NexusForever\Decomp\Analysis\function_labels.csv`
- `i:\GIT\NexusForever\Decomp\Analysis\INITIAL_FINDINGS.md`

**Verification**
1. Run `i:\GIT\NexusForever\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe` after the pattern changes land.
2. Confirm the targeted instance UI and state strings appear in `interesting_strings.csv` and resolve to concrete entries in `string_xrefs.csv`.
3. Confirm the same anchors now contribute reasons in `selected_reasons_summary.csv` and drive concrete functions into `selected_xrefs.csv` or `selected_decompiled.c`.
4. Compare the before-and-after export size and reason mix so the widened patterns stay focused instead of flooding the selected set.
5. If new labels land, rerun export and confirm the same functions remain discoverable through the focused artifacts.

**Decisions**
- Use the cache as the discriminating check before every keyword or pattern addition.
- Prefer precise high-value strings over broad domain keywords.
- Do not rename helper accessors beyond what call sites and row shape prove.
- Focus this pass on instance portal, instance difficulty, and instance UI/state families before considering broader adventure or expedition strings.

**Further Considerations**
1. If the widened patterns still miss relevant functions, add a second pass that mines `selected_decompiled_cache\functions` for high-frequency string families absent from `interesting_strings.csv`.
2. If output noise grows too much, split patterns into always-on core anchors and optional per-lane bundles rather than bloating one global list.