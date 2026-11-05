# Assessment continuation Goal prompt

Updated 5 November 2026 with verified pet-flair/creation/LAS milestones and bank contracts,
remaining implementation/evidence priorities and current REA boundary. Retains
roadmap-driven execution, scoped-only validation and the existing main-thread
regular commit/push policy.
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

Verified continuation status - 5 November 2026
- The verified ordinary Guild implementation checkpoint is
  eaad953a0fb898258ef9bd0a96c84dc9c67508d7 (ordinary Guild creation finance).
  Local HEAD and origin/legacy-di-storefront-hardening matched this commit
  during the status check; the research commits below are also published.
  Revalidate the live branch and working tree before resuming; this is a
  checkpoint to recognize, not a reset target or an instruction to rerun checks.
- Decomp/Analysis/GUILD_CREATION_FINANCE.md and its linked portable record
  qualify the public real-Player ordinary Guild credit route in Source/SQLite:
  accepted fee, complete guild/data/three ranks/leader-member graph and Character
  affiliation settle together through the original recoverable save. The draft
  stays private/fenced through ordinary followup; owned Guild achievement effects
  and completion feedback enter the next Character snapshot. Adoption and
  effect/reply cursors prevent replay of the debit or completed publication.
  All 22 focused/202 affected cases, clean owning/test builds and both final
  Source reviews pass. Community creation and cross-owner achievement atomicity
  are not closed by this result. Do not repeat the completed ordinary Guild fix.
- Other completed supported boundaries include Character affiliation recovery
  and committed publication, the tracked-marker first 32-bit TrackingSlot identity
  and unmasked lookup (84481977; 118 affected), and Primal Matrix 0072 decoding /
  corrected 06DF snapshot ownership (a621990e; 16 focused/143 affected). Marker
  producer timing/selection remains open; the Primal handler remains diagnostic
  and allocation gameplay is unfinished. These counts describe separate scopes.
- Published research commits 85efbcc5 and fedf5972 add the all-blocker-family audit and
  ICComm native/authored lifecycle evidence. Read
  Decomp/Analysis/ALL_REMAINING_BLOCKERS_RESEARCH_2026_11_05.md and its linked
  36-area inventory, plus Decomp/Analysis/ICCOMM_NATIVE_LIFECYCLE_2026_11_05.md,
  when selecting work. These are mapped inputs, not completed gameplay or
  row-by-row content closure. Supersede older research next steps with later
  verified implementation records, including the ordinary Guild result above.
  The inventory records11 implementation-COMPLETE,23 partial/emulator-only,
  one diagnostic and one structural area; the five producer/emit gates remain.
  ICComm's typed join/sender, pending type/name key and readiness/callback paths
  are mapped. The later Decomp/Analysis/ICCOMM_JOIN_CORRELATION.md milestone
  reproduces and fixes response-name correlation and collapsed Group/Guild addon
  memberships through public handlers:11 baseline failures fixed,26 selected
  cases, clean owning/test-only builds and both Source reviews. Do not repeat
  this completed Source slice. Native callbacks/socket and circle contexts remain
  open. Use the companion
  Decomp/Analysis/ICCOMM_WEB_CORROBORATION_2026_11_05.md for API-era boundaries;
  entitlement identity, server quota, active formulas and reconnect stay open.
- The latest recorded 20-PR all-state refresh retained reviewed unchanged heads,
  bases and states, including #608 open and #609 closed/unmerged. Refresh on
  resumption as directed below; do not count unchanged PRs as new implementation.

Next actionable outcomes and remaining gates
- The later Decomp/Analysis/PET_FLAIR_PROGRESSION.md milestone completes the
  real spell-effect flair grant's frozen Character rows and confirmed original
  World unlock/initial-bit publication. Three baseline failures are fixed;
  16 new/all299 affected cases, clean owning/test builds and both reviews pass.
  Do not repeat this completed Source slice. F-026 remains partial; broader
  cosmetic timing, public consumption settlement, physical restart, providers,
  actual tables, sockets/deployment and native/retail remain open. Continue the
  wider actionable gameplay/progression queue. Concurrent duel work is separately
  scoped and must be preserved.
- Community credit creation now has the later verified result in
  Decomp/Analysis/COMMUNITY_CREATION_FINANCE.md: the real public credit fee,
  guild/membership/affiliation and full residence/plot graph settle together
  under a dedicated Community receipt. Both registries stay private through
  ordinary followup.15 new Community/22 ordinary finance and278 affected cases,
  clean owning/test-only builds and both Source reviews pass. Do not repeat
  the completed credit integration. Its later Service Token result is recorded
  in Decomp/Analysis/COMMUNITY_TOKEN_CREATION.md: the public alternate route
  now binds durable admitted Auth payment to the complete Character decision,
  compensation before retry and admission recovery.17 new token/278 affected
  cases, clean owning/test builds and both final reviews pass. Do not repeat
  this completed Source integration. Physical restart/autonomous recovery,
  fresh configured-provider/actual-table/deployment/native/retail and cross-owner
  achievement gates remain. Do not reuse an ordinary Guild receipt as authority
  for residence rows or an Auth debit; preserve all direct/admin/free and
  ordinary Guild/Community creation guarantees. The later
  Decomp/Analysis/ACTION_SET_PUBLICATION.md milestone completes the executed LAS
  public save/retry/actor probe for supported replies:16 focused/all252 affected
  cases, clean scoped builds and both final reviews pass. Replies
  use the frozen confirmed book, original actor/account/session/epoch and fresh
  retained packet cursors; no-op and later edits remain owned. Do not repeat
  that completed reply slice. Broader initialized AMP/combat mutation timing,
  native asynchronous ordering and provider/socket/retail stay open. The later
  Decomp/Analysis/GUILD_BANK_NATIVE_CONTRACTS_2026_11_05.md maps29 exact-hash native
  spans:04A8 signs,04B1 bank rank settings, signed-max “all,” six-bit tab permission
  groups and the eight withdrawal selectors (formulas757-762). This is mapped
  only. Financial handlers are log-only and have no transaction to fault-inject.
  The later Decomp/Analysis/GUILD_BANK_RANK_SETTINGS.md implements public rank
  permission/money/repair settings through original guild persistence and
  confirmed World readback.22 new/346 affected cases, clean scoped builds and
  both final reviews pass. Rank256 alias and lost later ordinary notice defects
  are fixed. Separate upstream integration8749af17 changed the parent; bank
  validation was refreshed against it. Do not repeat the settings slice. Continue
  independent progression/gameplay while financial inputs are qualified.
  Keep money/item/tab-open diagnostic until a coherent financial
  service exists; actual formula/perk values, custody, authenticator authority,
  daily reset ownership and provider/native/retail remain open.
- Keep the full broader roadmap active: Contract public claim/reset requires
  remaining unclaimed/final-first/excess policy evidence and complete canonical
  delivery/recovery. Its726 choices are already recovered; retain the remaining
  item-metadata qualification rather than reopening that corpus as missing.
  Primal allocation requires qualified catalogs, ownership,
  validation, essence settlement, persistence/rewards and committed publication.
  Continue Zone earning, unsupported Challenge producers, broader AMP/spell/
  prerequisite/combat work, Hycrest later gameplay and Q5597 qualification as
  their specific inputs allow. Do not request the superseded Q5597 worksheet.
- Use the all-family audit to distinguish unfinished Source work (including
  guild bank settings and broader gameplay probes) from actual missing evidence.
  The LAS public reply probe is completed above. A proposed or unexecuted probe is not a
  reproduced defect. Contract reset edges, Fortune exact weights and other
  retail-policy gaps stay explicit; continue independent implementation.
- Fresh configured-provider/copied-upgrade, actual-table delivery, physical
  failure/restart, broker/socket/deployment and native/retail qualification remain
  required where recorded. Existing Source/SQLite results do not qualify those
  boundaries or the current deployment. Client-dependent checks stay stopped
  under the existing native-client/Computer Use constraint.
- REA is used for exact shipped-artifact identity/import evidence and retained
  bundles. The recorded REA 6.8.0 native-provider failure requires Ghidra 12.1.x;
  installed standalone Ghidra 12.0.4/JDK 21 and LLVM support separate read-only
  static inspection. Keep these evidence types distinct. Reuse exact-hash
  evidence; do not retry unchanged provider failures, install/configure tools
  merely to repeat them, or use REA readiness calls for ordinary Source work.

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
  coherent candidate. Use owning-project builds and scoped tests selected from
  the actual changed behavior and dependency surface. On 3 November the user
  directed: "Don't run the full suite every time, only scoped". This supersedes
  the earlier automatic full-solution milestone gate. Do not run another full
  solution build/test suite unless the user explicitly requests it.
- When a scoped check fails, diagnose and fix narrowly, then run the affected
  scoped checks needed to validate the changed candidate. Never weaken assertions
  merely to obtain a pass. If an unrelated intermittent failure remains unresolved,
  record it and investigate it as a separate issue; do not repeat passing or
  unrelated suites merely to obtain green. Continue independent work without
  claiming an unresolved failed candidate qualified.
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
- Report milestone progress as: behavior completed; owning-build/scoped results;
  material failures or remaining evidence; next integrated outcome. Include a
  brief observed time breakdown when validation dominates a milestone. Do not
  invent completion percentages or treat test growth as feature completion.
- If validation or evidence administration dominates successive iterations,
  reassess the scope of the checks and group related changes before the next
  scoped gate. Keep required correctness checks; remove redundant work and
  report completed roadmap behavior instead of using test counts as progress.
- If repeated iterations produce only infrastructure, reassess the critical
  path and complete its next required integration instead of expanding scope.
- Preserve entitlement defaults15:1/17:1. Do not auto-grant44 or the seven
  baseline character entitlements. Preserve existing functionality.
- Computer Use and native client launch remain stopped until explicitly resumed.
  After coherent verified milestones, commit the explicitly reviewed task files
  and push `origin/legacy-di-storefront-hardening` in
  `BankaiNoJutsu/NexusForever`. On5 November the user explicitly authorized
  this current public origin and named this branch, superseding the earlier
  private-only restriction for this destination and task scope. Do not request
  the same public-origin approval again. Verify the destination, reviewed scope,
  secrets and local/remote HEAD parity; use normal fast-forward pushes and
  preserve other staged work. This does not authorize merging or upstream PRs.
  Preserve unrelated files, configuration,
  original databases and other workloads; use authorized disposable fixtures.
- Do not claim retail completeness from source tests. Keep unavailable provider,
  actual-table, native and gameplay evidence explicit. Continue independent
  authorized work while gates are blocked. Mark the overall Goal complete only
  when its required outcomes are actually achieved; follow the Goal tool's
  status rules for unresolved blocking conditions.

Resume from the latest actual state now. Reuse valid completed checks; do not
launch another baseline or broader run merely because these instructions changed.
```
