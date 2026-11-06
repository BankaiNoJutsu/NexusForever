# Unified assessment and retail restoration goal prompt

Created 6 November 2026 for continuation on the asset-owning machine. This merges
the [retail restoration goal](RETAIL_RESTORATION_GOAL_PROMPT.md), its
[machine-transfer checkpoint](RETAIL_RESTORATION_RESUME_PROMPT.md), and
[Continue assessment goal](codex://threads/01a1acb9-1fad-79f1-afb3-9cef6881385f)
with its [execution policy](ASSESSMENT_CONTINUATION_GOAL_PROMPT.md).

Paste the block below when ready to resume. Creating this document does not
activate a Goal or start implementation. Use one combined Goal when resumed;
preserve both chats' checkpoints and existing pause instructions in the meantime.
The latest fetched publication observed while preparing this prompt is
`f9fd614f7eef1af8b0afea61dd0e645d668aedde`, which includes `19d830a8` and the
other-machine hosted results. The dirty local implementation checkout remains
based on `19d830a8`; do not mistake those states for one tested candidate or use
either hash as a reset target.

```text
Resume one combined NexusForever assessment, upstream reconciliation and retail
restoration Goal for WildStar final client build 16042. Finish the remaining
required assessment actions and accepted upstream corrections, then continue
evidence-backed reverse engineering and implementation across F-001 through
F-036 and the complete content-restoration inventory. Deliver integrated public
gameplay and server behavior with the required persistence, recovery and
publication. Research notes, helper APIs, packet models and test totals support
that outcome; they do not by themselves complete a feature or prove retail parity.

Continuity and source of truth
- Locate the actual checkout and verified runtime assets. The prior checkout was
  C:/GIT/NexusForever/continuation, branch legacy-di-storefront-hardening, with
  origin https://github.com/BankaiNoJutsu/NexusForever. Historical paths, hashes,
  drive letters, PIDs and ports are observations, not current ownership or reset
  instructions. Inspect the current branch, dirty/staged/untracked work, existing
  Goal state, paused candidates and live validation before changing anything.
- Continue an existing matching Goal when available. Create a combined Goal only
  when none exists; do not run duplicate restoration workloads across chats.
  Respect current user pause/resume instructions and preserve unrelated work.
- Read AGENTS.md and relevant nested guidance, CURRENT_STATUS.md,
  Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md,
  Decomp/Analysis/FULL_MISSING_SYSTEM_RESTORATION_PLAN.md,
  Decomp/Analysis/CONTINUATION_GUIDE.md and CONTINUE_ON_ANOTHER_MACHINE.md.
  For assessment/upstream work, read
  Decomp/Analysis/assessment-snapshot/ASSESSMENT.md and PR-TRIAGE.md.
- Use ASSESSMENT_REMAINING_WORK.md as the canonical action ledger, with
  MISSING_FEATURE_MATRIX.md and focused records for feature acceptance. Use
  CONTENT_RETAIL_COMPLETENESS_TRACKER.md and
  coverage/content_retail_completeness_next_slice_queue.csv and its blockers for
  content. These filenames are under Decomp/Analysis unless otherwise stated.
- Consult ALL_REMAINING_BLOCKERS_RESEARCH_2026_11_05.md and
  DEEPER_ROADMAP_CONTRACTS_2026_11_05.md for resolved premises and reusable leads.
  Later verified results and current source supersede historical next steps.
  Re-read relevant sections as the work changes, not every historical report
  during each edit. Keep completed evidence intact, with its original boundaries.

Reconcile both checkpoints before choosing new implementation
- Published 19d830a8 contains the physical BOP loot feedback correction and saved
  handoff. Whole quantity and collect state precede fallible UI; retained notice
  retries do not repeat quantity or cross stale map admission. Its 19 focused and
  239 related Source/SQLite cases and eight compiler-source bindings pass; both
  review axes have zero findings. Read PHYSICAL_LOOT_DELIVERY_FEEDBACK.md and
  coverage/physical-loot-delivery-feedback-20261106.json for isolation and limits.
- Preserve the published Q3777, Q4526 and Q5597 milestones and all other completed
  ledger entries. Reuse applicable checks rather than reimplementing their
  completed slices. Do not turn their remaining native/provider gates into
  claims that their Source work is still missing.
- The assessment chat paused an uncommitted transport/location save-retry fix.
  The saved checkpoint is
  artifacts/continuation-20261106/taxi-actual-catalog/PAUSED.md on the source
  machine. Player.Save cleared the location dirty flag before SQL settled, so
  failed-save retry could persist the fare/node but lose the admitted destination.
  The candidate acknowledges location only after flushed transaction settlement
  and preserves later movement. Other scalar save masks are outside that slice.
- Its six owned Source paths are Entity/Player.cs, Entity/Player.MapAdmission.cs,
  Entity/Player.Taxi.cs in NexusForever.Game, and Transport/TransportHandlerTests.cs,
  Transport/TransportHandlerTests.ActualCatalog.cs and
  Transport/PlayerMapLocationPersistenceTests.cs in NexusForever.Game.Tests.
  These projects are under Source. The shared Player.cs has an unrelated PvPFlag
  declaration: preserve it in the worktree and exclude it from this candidate.
- Old-parent transport checks passed 142 scoped cases, 1,338 actual-catalog
  quotes and 12 real-Player admission scenarios. Those binaries used parent
  1429c24a, not 19d830a8. Standards review completed with zero actionable findings;
  Spec review was interrupted. Neither final-parent qualification nor both final
  reviews are complete. Do not commit or relabel this candidate as qualified yet.
- When resuming that candidate, reconcile the committed loot Source blobs and
  later relevant changes against the latest publication parent, then run only the
  affected transport/location and relevant loot cross-controls plus its actual
  runner. Reuse unchanged review; complete Spec review and review material fixes.
  The previous candidate is under
  artifacts/continuation-20261106/northern-wilds-rescue/candidate. Inspect before
  reuse; do not use the older shared incoming-chat build directory.
- Git transfers committed source and portable summaries, not dirty transport
  edits, ignored checkpoints, raw logs, assets, private configuration or frozen
  binaries. On another machine, locate a scoped transfer of the pending candidate
  before assuming it exists. If unavailable, retain the precise missing input
  and reconstruct/reproduce narrowly when useful; do not claim the old passes
  qualify a different candidate. Do not copy or stage unrelated dirty work.

First runtime checkpoint: timestamps, then hosted follow-through
- Read PROVIDER_PACKET_HOSTED_RESTART_HANDOFF.md and its latest checkpoint.
  The later other-machine milestone, published in 3fae8296 and integrated by
  eb3addb6/f9fd614f, completes 16 SQLite and 16 MySQL 8.0.46 wire timestamp probes
  and 118 scoped Guild checks. Committed Guild/Character updates, fresh initial
  packets and legacy Build match stored SQL dates, including null, seasonal dates,
  both sides of clock changes and fractional rounding. Production conversion is
  unchanged. Missing Guild packets were fixture registration/draining omissions;
  do not revive the obsolete draft or infer an offset from Unspecified Kind.
- Reconcile this later evidence with ae873085's separate 12 MySQL/166 SQLite
  milestone rather than adding their totals or replacing their provenance.
  Check source/provider/profile applicability; re-run only for meaningful drift
  or an unresolved concern. Earlier achievement MySQL 51, SQLite 240+104 and
  producer/database-restart proof also retains its separate candidate limits.
- The same other-machine record qualifies frozen hosted World/Auth replacement,
  complete socket/bootstrap/disconnect/closed-presence cycles and prepared actual
  7194 pending/delivered/duplicate recovery. Realm-heartbeat readiness matters;
  Hosting started alone is insufficient. This is prepared recovery, not live raid
  earning. Read coverage/provider-packet-hosted-restart-20261106.json; do not treat
  hosted restart as wholly unperformed or as proof of every later combined build.
- Subsequent ICComm membership and hosted explicit-reconnect milestones are in
  ICCOMM_MEMBERSHIP_OWNERSHIP.md, ICCOMM_SOCKET_RECONNECT_QUALIFICATION.md and their
  portable coverage. Bounded Global/prepared-Guild runs pass seven sessions,
  55 decoded frames and 18 attempts per scope, with 66 scoped tests. Preserve
  limits: explicit rejoin is not native automatic reconnect; requested abortive
  close is not a captured TCP RST; positive Group/live scope changes remain open.
- The current hosted follow-through is CHARACTER_LIST_COSTUME_PREVIEW.md:
  the Source fix passes 93 scoped cases, but its changed World DLL needs a fresh
  hosted witness. Earlier ICComm runs could select a known SQL Character ID even
  though the character-list catch path omitted its row. On new private copies,
  decode ServerCharacterList wire rows and require the owned selected Character
  to be present before selection, then rerun the relevant ICComm scenarios.
  Do not relabel the old socket runs as qualified character-list presentation.
- Locate and verify current build-16042 table/map assets, private routes/configs,
  broker and owned service generations. The other machine found an installation
  and a validated 1,211-file table/map cache; historical paths/hashes remain in
  its record, not assumptions for this host. Reported service/schema/broker cleanup
  is historical evidence: inventory live ownership anew. Preserve original data,
  entitlements, other services and queues. Read Tools/Setup/README.md before
  runtime changes and Tools/NexusForever.LoadTest/README.md before scenarios.
- Prepare the changed frozen candidate with incremental owning World/Auth builds
  as needed. Use complete readiness, admission, disconnect and cleanup controls;
  restart only validated owned generations through the documented mechanism.
  Retain exact source/DLL, row/receipt, wire and lifecycle witnesses, including
  pending recovery when relevant to the changed candidate. Keep hosted startup,
  socket cycles, achievement recovery, native and retail outcomes separate.
  Never fabricate assets or weaken admission/character-list checks.
- Keep actual raid eligibility, all-member fanout and durable global-first open.
  The later exact-hash native pass already resolves Unknown290's dispatcher alias
  and Unknown203's client no-op; neither proves a server predicate or permits a
  bypass. Reopen203 only with a non-stub server/retail witness. Do not repeatedly
  rediscover the same client stub while independent gameplay remains actionable.
- If assets or another required witness are unavailable, record the exact gate
  and continue independent authorized assessment/gameplay work. Preserve pending
  implementation before starting another candidate; do not overlap its outputs.

Use REA, online search and deeper investigation where they resolve a question
- Apply reverse-engineer-anything for shipped-artifact questions. Establish exact
  target build/hash identity, reuse retained evidence and decompile caches, and
  trace focused functions, registrations, callers, packet consumers and table
  joins. Use ordinary repository tools for complete-source analysis; do not run
  REA readiness/provider probes for ordinary Source work.
- Use the connected REA tools and their current schemas. Historical records
  describe a native-provider mismatch and a separate standalone Ghidra/LLVM
  path. Recheck only a capability needed for a concrete question. Reuse supported
  REA identity/import/evidence operations and established static fallbacks while
  recording distinct provenance. Do not repeat unchanged failed calls or install
  tools merely to repeat them. Java is only a prerequisite when the chosen
  analysis tool requires it; it is not an application or MySQL prerequisite.
- Search online actively for specific unanswered questions. Follow primary
  publisher/archived official records, authored Lua/Apollo APIs, original upstream
  code/PRs/commits and attributable captures. Use community recollections, wiki
  summaries and other emulators as leads needing corroboration. Record direct
  URLs, revisions/dates, build applicability, contradictions and unknowns; do not
  apply an older game's policy to final build 16042 without evidence.
- Define each missing witness as a concrete question with target functions,
  packet IDs, table rows or runtime observations. Follow references, vary useful
  search terms and cross-check independent evidence. Advance through Observed,
  Correlated, Mapped and Verified. Implement only corroborated meaning; keep
  unknown fields and unproven producers diagnostic. Record addresses and concise
  findings rather than copying decompiled client source.
- Refresh upstream on resumption and at meaningful reconciliation boundaries,
  including PR #608 and later changes. Compare actual heads, bases, states and
  diffs. Reuse reviewed unchanged PRs; inspect closed/unmerged proposals as
  proposals, not accepted upstream behavior. Do not repeat full inventories
  during routine test iterations. Implement accepted corrections in coherent
  slices; this goal does not authorize merges or upstream PR publication.

Deliver the broader roadmap in coherent public-flow outcomes
- Select the next actionable outcome by player impact, correctness risk,
  dependencies and available evidence. Keep assessment closure, upstream fixes,
  rewards/progression, combat/spells/AMP/prerequisites, loot, crafting/economy,
  social systems, transport, quests/instances and content restoration moving.
  Use the current ledger rather than freezing a stale list of individual tasks.
- Keep remaining Contract claim/reset policy and delivery, Primal Matrix
  allocation/currency ownership, unsupported Challenge/Zone producers, broader
  guild-bank/social systems and later quest/instance gameplay explicit until
  their recorded inputs and public flows are qualified. Recovered catalogs do
  not alone close settlement, earning, delivery or native-policy gates.
- State the observable acceptance boundary, inspect owning source and existing
  checks, and reproduce relevant failures where feasible. Complete the real
  producer or parsed public request flow, including validation, persistence,
  rollback/retry, recovery and committed publication. Avoid isolated helpers,
  speculative edge-case expansion and unrelated general-purpose frameworks.
- Preserve original actor/account/session/epoch/realm/map ownership, transaction
  and retry guarantees, compatibility and existing functionality. Preserve
  entitlement defaults 15:1 and 17:1; do not auto-grant 44 or the seven baseline
  character entitlements. Protect original databases and other workloads; use
  authorized disposable fixtures for destructive failure qualification.
- Runtime code must not query wildstar_client, jabbithole or nf_map_* authoring
  databases/tables. Promote reviewed data through runtime-owned tables, scripts
  or assets. Read DataMapping and setup instructions before changing those flows.

Keep validation and evidence proportionate
- Use normal incremental owning-project builds and meaningful focused tests
  selected from changed behavior and real dependencies. Cover material rejection,
  rollback, retry, ownership and save/reload boundaries. Shared persistence/model
  changes need relevant cross-feature controls, not every historical suite.
  Do not run full-solution builds or full-suite tests unless explicitly requested.
- Batch related implementation, fixture and review fixes into one candidate.
  Diagnose scoped failures narrowly without weakening assertions or rerunning
  passing/unrelated suites merely to obtain green. Track unrelated intermittent
  failures separately. Broaden checks only when a change or concern justifies it.
- Ensure tests use the changed owning DLLs. Isolate outputs when concurrent work
  requires it; never overlap builds writing the same outputs. Rebuild/refresh
  copies when stale-output evidence requires it. Record adaptations and frozen
  dependencies; an isolated assembly-based run is not a normal combined-root
  graph build or exact qualification of a later publication parent.
- Consolidate review at the milestone boundary and re-review material fixes.
  Retain meaningful failing controls, final source/binary identity, test results
  and review evidence in one focused milestone record. Avoid whole-tree archives,
  bespoke verification infrastructure or complete reviews for unchanged code.
- Use Tools/Handoff/Verify-Continuation.py only for its historical checkpoint.
  Preserve expected Source-divergence after changes; do not repeatedly rediscover
  that result or edit historical records to make them pass.
- Update the canonical remaining-work ledger at completed milestones or material
  blockers; other trackers should link to the focused record. Distinguish
  implementation, Source/SQLite, actual-catalog, configured-provider, copied
  upgrade, physical failure/restart, broker/socket/deployment, native and retail
  qualification. Never add test totals across incompatible scopes as coverage.

Publication, reporting and completion
- After coherent verified milestones, commit only explicitly reviewed task files
  and push origin/legacy-di-storefront-hardening in BankaiNoJutsu/NexusForever.
  The user's existing authorization includes this public origin and branch; do
  not request it again. Verify destination, exact staged scope, secrets and
  local/remote HEAD parity. Use normal non-force pushes, preserve other staged
  work and shared-file hunks, and reconcile concurrent commits without resets.
  This does not authorize unrelated publication, merges or upstream PRs.
- Keep credentials, private settings, raw client assets and generated local
  outputs out of commits. Promote concise portable evidence and a current resume
  checkpoint so another machine can distinguish what Git contains from what
  must be supplied separately.
- Report behavior delivered, owning-build/scoped results, material failures or
  missing evidence, and the next integrated outcome. Include observed timing
  briefly when validation dominates. If successive iterations mainly produce
  validation administration or infrastructure, reassess the critical path,
  remove redundant work and complete the next required gameplay integration.
- Computer Use and native-client launch remain stopped until explicitly resumed.
  Do not interpret this combined prompt as lifting that constraint.
- Continue independent authorized work when a slice lacks external evidence.
  Record what was attempted, the exact missing input, why it matters and the next
  artifact/command. Revisit exhausted avenues when inputs change. If no meaningful
  independent progress remains, follow the Goal tool's blocking rules. A blocked
  gate, proposed probe, test increase or research document is not completion.
- Mark the combined Goal complete only when every required assessment/restoration
  outcome is implemented and verified, or explicitly excluded by the user or
  rejected as inapplicable with evidence. Required unresolved retail gates keep
  the Goal unfinished. Do not invent completion percentages.

Resume from the latest actual state when instructed. Reuse applicable completed
checks; do not launch a baseline, full build or broader run just because this
prompt was created or updated.
```
