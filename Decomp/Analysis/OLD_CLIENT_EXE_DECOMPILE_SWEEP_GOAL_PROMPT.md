# Goal Prompt For Old Client EXE Decompile Sweep

Created: 2026-06-19

This file contains a paste-ready Codex Goal prompt for using older WildStar
client binaries as comparative decompile evidence for current NexusForever
blockers. The sweep treats build 16042 as the compatibility and verification
gate; older EXEs are search accelerants, not alternate retail authority.

## Recommended Goal Prompt

Paste this into Codex from the NexusForever repository root:

```text
/goal Sweep older WildStar client EXE decompilation evidence to unblock current NexusForever blockers only where old binaries can safely accelerate build 16042 evidence discovery. Work from I:\GIT\NexusForever and follow AGENTS.md, CURRENT_STATUS.md, Decomp/Analysis/README.md, Decomp/Analysis/CONTINUATION_GUIDE.md, Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md, Decomp/Analysis/WILDSTAR_CLIENT_DATA_REUSE_PLAN.md, Decomp/Analysis/MISSING_FEATURE_MATRIX.md, Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md, Decomp/Analysis/function_labels.csv, and the generated content-retail CSVs. Use the downloaded archive under I:\WildStar\ClientArchives\ArctiumClientData, especially near-neighbor binaries 16029, 16028, 16005, 15996, 15884, and 15866 before older beta/alpha builds. Treat older client EXEs as comparative archaeology: use them to find stable packet writers/readers, UI/apply consumers, callback tables, constants, strings, table consumers, and function families that identify sharper search targets in the build 16042 client. Do not implement or rename runtime behavior from an older EXE alone.

For each iteration, pick exactly one narrow blocker-shaped question from the current queue or blocker plan, such as one unresolved opcode family, one public-event/matching/reward/quest-direction/map-tracking/housing/marketplace apply path, one spell/entity-stat aux cluster, or one stale table-provenance candidate. First inspect existing 16042 cached exports, labels, tracker rows, owning NexusForever source, and generated CSV evidence. Then compare the closest older binary that is likely to contain the same behavior. Record binary version, function address, proposed label, anchors, constants, strings, table/opcode links, callers/callees, confidence level, and the exact 16042 search target the old binary revealed. Search or refresh 16042 evidence next; only promote a finding when 16042 decompile, table data, packet shape, live client/server capture, or focused tests confirm it.

Use the evidence ladder strictly. Older-only observations may be Observed or Correlated. A durable label requires Mapped evidence and must be conservative. Server behavior changes require Verified evidence against build 16042 or another accepted current-runtime proof source. Keep unknown packet fields diagnostic-only, keep blocked placeholder names neutral, and do not widen packet, spell, crypto, token, anti-tamper, reward, public-event, matching, entity-stat, or runtime state semantics beyond verified evidence. Do not copy decompiled client source into the repository. Do not query wildstar_client, jabbithole, nf_map_*, or other staging/reference sources from runtime code. Preserve unrelated dirty-worktree changes.

Prefer durable, source-controlled evidence updates over broad code churn. If a pass maps behavior, update the appropriate tracker/finding artifacts, such as Decomp/Analysis/INITIAL_FINDINGS.md, Decomp/Analysis/function_labels.csv, Decomp/Analysis/mapping_artifacts/*.csv, Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md, Decomp/Analysis/MISSING_FEATURE_MATRIX.md, or the focused status tracker. If a pass proves an old-client-only stale source row, record it as stale provenance or rejected/mapped-only evidence without claiming retail completion. If a pass enables a safe implementation, make the smallest owning-source change, add or update focused tests, run the narrowest useful verification, and keep tracker claims aligned with generated CSV gates.

Loop until the old-client EXE sweep has either: mapped useful 16042 search targets for each prioritized blocker family, reclassified old-only evidence as stale/rejected provenance, implemented verified behavior with tests, or recorded a named blocker for each remaining target. Stop a slice as blocked instead of guessing when needed inputs are unavailable, including Ghidra analysis, export cache, debugger/MCP access, WildStar build 16042 client evidence, packet captures, local MySQL/RabbitMQ, reference table extraction, or a reliable 16042 confirmation path. For every blocked slice, record the exact attempted binary/build, path, command or cache searched, evidence gathered, missing input, and next command or artifact that would unlock progress.
```

## First Targets

Start with these highest-value comparisons:

1. Near-neighbor EXEs `16029`, `16028`, `16005`, `15996`, `15884`, `15866`
   against current 16042 blocked packet/UI/apply families.
2. Spell aux and entity-stat aux producer/apply paths.
3. Public event objective/vote/scoreboard/reward and instance producer paths.
4. Matching, raid queue, group queue, housing, marketplace, and map-tracking
   placeholder families.
5. Stale provenance checks for old-only IDs already found in the downloaded
   data sweep, including `WorldZone` `1`, `2`, `6`, and `Quest2Reward` `3668`.

## Completion Audit

Before marking the Goal complete, verify:

1. Every swept target has one of these states: Mapped 16042 target, Implemented
   with verification, Rejected as old-only/stale, or Blocked with a named missing
   evidence source.
2. Any labels added to `function_labels.csv` are at least Mapped and re-exported.
3. Any runtime behavior changes have focused tests and do not depend on
   reference/staging databases at runtime.
4. Tracker and finding updates do not claim retail completion from old EXE
   evidence alone.
5. Generated/local artifacts remain uncommitted unless the task explicitly
   requires their promotion.
