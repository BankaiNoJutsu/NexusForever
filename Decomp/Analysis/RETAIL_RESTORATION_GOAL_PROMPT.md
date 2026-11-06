# Retail restoration continuation goal

Created: 5 November 2026.

Use this prompt in the active checkout at `C:/GIT/NexusForever/continuation`.
It defines ongoing restoration work; creating the prompt does not qualify any
implementation or retail behavior. Resume an unfinished matching Goal instead
of creating a duplicate.

## Goal prompt

```text
Continue the NexusForever retail restoration for WildStar final client build 16042 in C:/GIT/NexusForever/continuation. Pursue the remaining required outcomes across F-001 through F-036 and the content inventory through evidence-backed investigation, implementation and verification. Deliver working public gameplay and server behavior; research notes, packet models, helper APIs and test totals support that outcome but do not alone complete it.

Start from the actual current state
- Read AGENTS.md, CURRENT_STATUS.md, Decomp/Analysis/ASSESSMENT_REMAINING_WORK.md, Decomp/Analysis/FULL_MISSING_SYSTEM_RESTORATION_PLAN.md and the evidence ladder in Decomp/Analysis/CONTINUATION_GUIDE.md. Use MISSING_FEATURE_MATRIX.md and focused subsystem trackers when selecting a slice. For content, use CONTENT_RETAIL_COMPLETENESS_TRACKER.md and coverage/content_retail_completeness_next_slice_queue.csv plus its blockers.
- Consult ALL_REMAINING_BLOCKERS_RESEARCH_2026_11_05.md and DEEPER_ROADMAP_CONTRACTS_2026_11_05.md for resolved research premises and reusable evidence. Later verified implementation and current source supersede older next-step instructions. Read CONTINUE_ON_ANOTHER_MACHINE.md when resolving missing tools, datasets or historical paths.
- Inspect branch, worktree changes, active work and existing validation before acting. Preserve unrelated changes and databases. Recognize completed slices, reuse checks valid for the same candidate, and resume in-flight work without duplicating another workload.

Use REA and targeted digging
- Apply reverse-engineer-anything for shipped-artifact questions. Establish exact binary/build/hash identity, reuse retained evidence and decompile caches, and trace focused functions, registrations, callers, packet consumers and table joins. Ordinary source analysis uses repository tools.
- Repository records describe a REA native-provider limitation and an existing standalone Ghidra/LLVM inspection path. Check relevant current capability only when needed; do not repeat unchanged failed calls or install/configure tools merely to repeat them. Use supported REA identity/import/evidence operations and the existing static-analysis fallback where necessary, recording their distinct provenance and limitations.
- Use online search actively for a named unanswered question. Follow leads to primary sources: publisher records and archived official pages, authored WildStar Lua/Apollo APIs, original upstream code/PRs/commits, and attributable captures. Treat other emulators, community recollections and wiki summaries as leads requiring corroboration. Record direct URLs, revision or archive dates, applicable game version and contradictions. Do not mistake historical behavior for final-build policy.
- Frame each evidence gap as a concrete question with target functions, packet IDs, table rows or runtime observations. Change search terms, follow references and cross-check independent sources when useful. Revisit an exhausted avenue when an input or capability changes; record the missing witness and move to independent work otherwise.
- Advance through Observed, Correlated, Mapped and Verified. Implement behavior only when the mapped meaning has independent corroboration required by the repository. Keep unknown packet fields and unproven producers diagnostic. Summarize native behavior with addresses and evidence references rather than copying decompiled client source.

Implement and verify in coherent slices
- Choose the next actionable roadmap outcome by player impact, correctness risk, dependencies and available evidence. Begin with one concrete gameplay/progression or other public-flow slice identified from current source and trackers; do not lock the goal to a stale task list. Keep rewards, progression, combat/spells, crafting/economy, social systems and quest/instance restoration moving as inputs permit.
- State the observable acceptance boundary, inspect the owning source and existing tests, and reproduce relevant failures where feasible. Complete behavior through its real producer or public request flow, including required validation, persistence, recovery and committed publication. Avoid isolated helpers and unrelated frameworks.
- Preserve original actor/session ownership, transaction and retry guarantees, compatibility, entitlement defaults and existing functionality. Runtime code must not query wildstar_client, jabbithole or nf_map_* authoring/reference tables; promote reviewed data through the documented runtime-owned path. Read DataMapping/setup instructions before changing those flows.
- Use incremental owning-project builds and meaningful focused tests selected from changed behavior and dependencies. Cover material rejection, rollback, retry, ownership and reload boundaries where applicable. Do not run full-solution builds or the full test suite unless explicitly requested. Do not overlap builds that write the same outputs or claim results from stale DLLs. Broaden checks only when changes or failures justify it.
- Perform configured-provider, actual-row, safe-import, restart/broker/socket and client-visible qualification when required and available within current authorization. Preserve the recorded stop on Computer Use and native-client launch until explicitly resumed. Record unavailable live evidence as an open gate and continue independent work.
- After each coherent milestone, review the diff, resolve material findings, and update the canonical remaining-work ledger and relevant focused evidence record. Link build/test results and exact source/artifact identity. Keep generated reports tied to their documented generators. Do not reset, merge or publish merely because this prompt was created; use current task authorization for external writes.

Continue until the outcome is achieved
- Re-evaluate the remaining queue after each milestone and keep implementing without requiring repeated direction for routine choices. Do not remain in one subsystem inventing additional edge cases while actionable roadmap outcomes remain elsewhere.
- Keep implementation, Source/SQLite, configured-provider, physical failure/restart, native/socket and retail qualification separate. A local pass does not prove retail equivalence. Do not invent completion percentages or close content rows without their row-specific evidence.
- For each blocked slice, retain what was attempted, the exact missing input, why it matters and the next artifact or command that would unblock it. A proposed probe is not a reproduced defect, and an external blocker is not completion. If no independent work remains possible, report the specific dependency and follow the goal tool's blocking rules.
- Mark the overall goal complete only when all required restoration outcomes are implemented and verified, or an item has been excluded by the user or rejected as inapplicable with supporting evidence. Unresolved required retail gates remain unfinished. Report milestones concisely as behavior delivered, scoped verification, material limits and the next integrated outcome.

Online starting points: https://github.com/NexusForever/NexusForever (upstream build-16042 baseline), https://github.com/NexusForever/NexusForever.WorldDatabase (upstream world data). Follow the repository's focused research records to the sources appropriate to each question.
```
