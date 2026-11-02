# Assessment continuation Goal prompt

Updated 2 November 2026 for roadmap-driven execution, proportionate validation
and the user-authorized regular commit/push workflow.
Paste the following instructions into the main
implementation chat. Continue its existing Goal; create a Goal only if no
unfinished Goal exists. This file does not itself change or restart a Goal.

```text
Continue the existing NexusForever assessment and implementation Goal, applying
the execution policy below. The objective remains: finish every remaining
actionable assessment and upstream reconciliation requirement, then continue
evidence-backed reverse engineering and implementation using
reverse-engineer-anything. Progress means completed, integrated behavior and
resolved defects; test totals and new helper APIs are supporting evidence.

Workspace and continuity
- Use C:/GIT/NexusForever/continuation, branch legacy-di-storefront-hardening,
  repository https://github.com/BankaiNoJutsu/NexusForever. Checkpoint
  293842289f50ea03e17386663da0d59ac9d3576a is historical, not a reset target.
- Preserve all existing work. Inspect current changes and in-flight validation
  before starting anything; do not restart completed work or duplicate a run.
- Read AGENTS.md, CONTINUE_ON_ANOTHER_MACHINE.md,
  Decomp/Analysis/assessment-snapshot/ASSESSMENT.md,
  Decomp/Analysis/assessment-snapshot/PR-TRIAGE.md,
  Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md and
  Decomp/Analysis/CONTINUATION_GUIDE.md. Use the latest verified records, not
  stale test counts or old-machine runtime results. Re-read only relevant
  sections as work changes.

Follow the roadmap
- Use Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md as the canonical roadmap and
  CURRENT_STATUS.md as the feature-area overview. The roadmap's current queue,
  completion gates and linked feature records define the remaining scope. Historical
  entries retain their evidence boundaries; later verified results supersede
  their old next-step instructions. This prompt sets execution policy, not a
  fixed list of individual tasks.
- Resume from the actual in-flight milestone and reuse valid completed checks.
  At each milestone boundary, select the next actionable roadmap outcome by
  user-visible impact, correctness risk, dependencies and available evidence.
  Keep assessment closure, upstream reconciliation, recovery/publication,
  rewards/progression and broader gameplay moving as their prerequisites allow.
  Do not remain in one subsystem merely because more speculative edge cases
  or additional infrastructure could be invented there.
- Deliver coherent behavior through its real producer or public flow. Keep
  necessary prerequisites within that outcome and preserve existing ownership,
  recovery and compatibility guarantees. A new internal API alone does not
  close a feature. Avoid unrelated refactors and general-purpose frameworks.
- State the observable acceptance boundary and necessary evidence briefly in
  the existing roadmap entry or progress update. A separate acceptance document
  is optional; do not create one for every small step.
- Leave behavior requiring missing evidence explicitly blocked; do not guess
  retail semantics or enable incomplete public reward delivery. Record the
  specific missing input, then continue independent actionable roadmap work.
  Revisit blocked evidence only when a relevant input or capability changes.

Use a short feedback loop
- Develop with the owning project build and meaningful focused regressions.
  Reproduce the relevant fault where feasible and retain useful controls.
- Select affected existing integration coverage from the actual dependency
  surface. Shared persistence/model changes need their existing cross-feature
  controls; a local change does not require every historical feature suite.
  Add regressions for distinct observable failures and material boundaries,
  rather than mirroring helpers or multiplying equivalent permutations.
- Batch related implementation, fixture corrections and review fixes into one
  coherent candidate. Run the full Source/NexusForever.slnx build/test suite
  after that candidate passes focused and affected integration checks.
- Do not run a full suite after every helper, API, comment, tracker or small
  intermediate edit. Do not repeat an unchanged passing full run without a new
  material concern. When a full gate fails, diagnose and fix narrowly, then run
  one corrected full gate when required to validate the changed candidate.
  Never weaken assertions merely to obtain a pass. If an unrelated intermittent
  failure remains unresolved, record the failed gate and investigate it as a
  separate issue; do not repeatedly run the whole suite just to obtain green.
  Continue independent work without claiming the failed candidate qualified.
- Consolidate review at the milestone boundary and re-review material fixes.
  Do not restart complete reviews for unchanged code or documentation edits.
- Use normal incremental builds unless there is evidence of stale output.
  Ensure tests use the changed owning DLLs. Force a rebuild or refresh test
  copies when dependency/timestamp evidence requires it, rather than rebuilding
  everything repeatedly. Do not overlap builds that mutate the same outputs or
  claim results against a different source/binary candidate.

Keep evidence proportionate
- Retain meaningful failing reproductions, final source/binary identity, test
  results and the final review. Preserve existing historical evidence.
- One final milestone record should link these results. Intermediate failures
  need enough context to interpret them, not a new whole-tree archive, review
  cycle or bespoke verification framework for each correction.
- Use Tools/Handoff/Verify-Continuation.py as the historical checkpoint check.
  Retain its expected Source-divergence result after implementation changes;
  do not repeatedly rediscover that same failure or alter old records to pass.
- Update the canonical remaining-work ledger at completed milestones or material
  blockers. Other trackers should link to the result rather than accumulate
  repeated copies of every experiment. Keep implementation, source/SQLite,
  configured-provider, socket/native and retail qualification distinct.
- Refresh upstream on resumption and at meaningful reconciliation boundaries.
  Include #608 and later PRs; reuse reviewed unchanged heads, and inspect actual
  changes. Do not repeat complete inventories during routine test iterations.
- Use REA for shipped-artifact questions, not ordinary source architecture.
  Follow the evidence ladder. Record missing external evidence precisely and
  avoid repeating the same unavailable search/tool unless an input changes.

Accountability and constraints
- Report milestone progress as: behavior completed; focused/full results;
  material failures or remaining evidence; next integrated outcome. Include a
  brief observed time breakdown when validation dominates a milestone. Do not
  invent completion percentages or treat test growth as feature completion.
- If validation or evidence administration dominates successive iterations,
  reassess the scope of the checks and group related changes before the next
  full gate. Keep required correctness checks; remove redundant work and
  report completed roadmap behavior instead of using test counts as progress.
- If repeated iterations produce only infrastructure, reassess the critical
  path and complete its next required integration instead of expanding scope.
- Preserve entitlement defaults15:1/17:1. Do not auto-grant44 or the seven
  baseline character entitlements. Preserve existing functionality.
- Computer Use and native client launch remain stopped until explicitly resumed.
  After coherent verified milestones, commit the explicitly reviewed task files
  and push `origin/legacy-di-storefront-hardening` in
  `BankaiNoJutsu/NexusForever`, as authorized by the user on2 November. Verify
  the destination, scope, secrets and local/remote HEAD parity; use normal
  fast-forward pushes. This does not authorize merging or creating upstream PRs.
  Preserve unrelated files, configuration,
  original databases and other workloads; use authorized disposable fixtures.
- Do not claim retail completeness from source tests. Keep unavailable provider,
  actual-table, native and gameplay evidence explicit. Continue independent
  authorized work while gates are blocked. Mark the overall Goal complete only
  when its required outcomes are actually achieved; follow the Goal tool's
  status rules for unresolved blocking conditions.

Resume from the latest actual state now. Do not launch another baseline/full
run merely because these instructions were updated.
```
