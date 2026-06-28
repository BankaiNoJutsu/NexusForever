# Goal Prompt For Implementing The Unblock Plan

Created: 2026-06-17

This file contains a paste-ready Codex Goal prompt for implementing
`Decomp/Analysis/UNBLOCK_AND_IMPLEMENT_ALL_PLAN.md`.

It follows the OpenAI Goals guidance pattern: define the desired outcome,
verification surface, constraints, boundaries, iteration policy, and blocked
stop condition.

Reference:

- https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex

## Recommended Goal Prompt

Paste this into Codex from the NexusForever repository root:

```text
/goal Implement Decomp/Analysis/UNBLOCK_AND_IMPLEMENT_ALL_PLAN.md until every currently tracked blocked, diagnostic-only, partial, and not_retail_complete NexusForever surface is either evidence-backed implemented and verified, explicitly rejected with recorded evidence, or still blocked with a named external/runtime evidence source that cannot be obtained in this environment. Verify completion against CURRENT_STATUS.md, Decomp/Analysis/MISSING_FEATURE_MATRIX.md, Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md, Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md, focused status trackers, generated content-retail CSVs, focused xUnit/Python tests, DataMapping safe-import verification when data changes, and live-client/server evidence bundles where automation cannot prove gameplay. Preserve the evidence ladder, runtime-data boundaries, and repo conventions in AGENTS.md: do not widen packet, spell, crypto, token, anti-tamper, runtime state, DataMapping, or WIP/GUESSED behavior beyond mapped and verified evidence; do not query wildstar_client, jabbithole, or nf_map_* from runtime code; do not edit generated/local artifacts except through documented generator workflows; do not revert unrelated dirty worktree changes. Work one narrow row, packet cluster, subsystem, spell family, quest chain, or instance flow per iteration. Between iterations, inspect the relevant tracker rows and source, choose the highest-leverage unblocker from the plan, gather native/decompile/source/table/log/client evidence first, implement only verified behavior, add or update focused tests, run the narrowest useful verification, update durable trackers, and then re-evaluate the remaining queue before continuing. If a needed dependency is unavailable, such as a WildStar build 16042 client, local MySQL/RabbitMQ, Ghidra/MCP access, reference databases, official world database checkout, retail packet capture, or storefront catalog evidence, stop that slice as blocked with the exact attempted paths, evidence gathered, missing input, and next command or artifact that would unlock progress rather than guessing.
```

## Why This Is The Right Shape

This is intentionally a broad, persistent goal rather than a one-turn task.
The implementation path depends on what each evidence pass discovers, so the
Goal gives Codex enough room to choose the next useful action while keeping the
finish line auditable.

The success condition is deliberately strict:

- A row is not complete just because code was added.
- A row is not complete just because packet shape is mapped.
- A row is not complete just because generated tracker output changed.
- A row is complete only when source, tests, tracker state, and required
  runtime/client evidence agree.

## First Continuation Instruction

After activating the Goal, start with this instruction if Codex asks where to
begin:

```text
Start by rebuilding the baseline from Decomp/Analysis/UNBLOCK_AND_IMPLEMENT_ALL_PLAN.md Phase 0. Then choose the first narrow slice from Phase 1, preferring the crafting current-craft/aux gap unless the current repo state shows a higher-priority blocker. Do not implement until the exact tracker row, owning source files, existing tests, and evidence gate are identified.
```

## Completion Audit

Before marking the Goal complete, Codex must verify:

1. `CURRENT_STATUS.md` no longer lists unresolved partial, diagnostic-only, or
   structural-only feature areas unless each remaining item is explicitly
   blocked or rejected with named evidence.
2. `Decomp/Analysis/MISSING_FEATURE_MATRIX.md` and focused status trackers agree
   with the final state.
3. `Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md` and generated CSVs
   no longer contain unsafe completion claims.
4. The five consolidated runtime producer/emit gaps are implemented, rejected,
   or blocked on named external evidence.
5. All changed runtime behavior has focused tests.
6. DataMapping or SQL changes have documented safe-import verification.
7. Live-client/manual-smoke-dependent behavior has evidence bundles or remains
   explicitly blocked.
8. No runtime code depends on authoring-only reference databases or staging
   tables.
