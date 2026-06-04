# Goal: Close Every Missing NexusForever System

Work in `I:\GIT\NexusForever`. Unblock, uncover, name, and implement every
missing or incomplete NexusForever feature/system. Do this as an evidence-backed
closure loop, not a broad rewrite.

First read: `AGENTS.md`, `README.md`, `CURRENT_STATUS.md`,
`Decomp/Analysis/MISSING_FEATURE_MATRIX.md`,
`Decomp/Analysis/CONTINUATION_GUIDE.md`,
`Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md`, and the focused tracker for the
target area. Preserve existing user changes in the dirty worktree.

Build a master closure workboard covering all `F-001`..`F-036` rows, the
`Remaining Blocked Items` table, shape-mapped aux/spell packets with no emitters,
diagnostic client opcodes, WIP-guessed content, TODO/FIXME/NotImplemented paths,
placeholder names (`Unknown`, `ValueN`, `DataBits`, `Client0x`, `Server0x`), and
tracker disagreements or false "ghost gap" claims.

For every gap record: stable name, feature row, current behavior, missing
behavior, evidence state, blocker, owning files, implementation gate,
verification path, and final disposition.

Evidence states:
- `Observed`: one string/constant/log/shape. Record only.
- `Correlated`: matches source, table, addon, packet, log, sniff, or tracker.
- `Mapped`: field order, intent, direction, and ownership are explainable.
- `Verified`: repeatable runtime, test, sniff, table, or native evidence proves it.
- `Implemented`: code changed, verification passed, tracker updated.
- `Blocked`: exact missing evidence source is named.
- `Rejected`: evidence disproves the hypothesis or source.

Rules:
- Implement only verified behavior. Keep unknown fields diagnostic-only.
- Do not infer semantics from adjacency, old branch code, names, or one witness.
- Do not query `wildstar_client`, `jabbithole`, or `nf_map_*` from runtime code.
- Do not copy decompiled source. Summarize with binary/address/label/export path.
- Do not widen crypto, token, spell, packet, or runtime state beyond mapped proof.

Closure loop:
1. Inventory gaps with `rg` across `Source`, `Decomp`, `Tools`, and `*.md`.
2. Pick one row/subsystem/packet cluster/spell family/content flow.
3. State the target question, current behavior, evidence level, blocker,
   candidate files, and verification command before editing.
4. Gather evidence from source/tests, packet shapes, tables, addons, decompile
   exports/labels/xrefs, Ghidra helper scripts, server Trace logs, client
   captures, or `Start-BlockerEvidenceHarness.ps1`.
5. Name fields/opcodes/helpers only after semantics are mapped.
6. Implement the smallest proven behavior in the owning subsystem.
7. Add focused xUnit or smoke coverage.
8. Update `CURRENT_STATUS.md`, `MISSING_FEATURE_MATRIX.md`, focused trackers,
   labels, and findings with enough proof for rediscovery.
9. Verify with the narrowest useful build/test, then broader gates if shared
   contracts, database models, or status claims changed.

Initial priority:
1. `F-025` entity-stat aux emitters: `0x0889`, `0x08CC`, `0x08F4`, `0x0939`,
   `0x093D`, `0x093E`.
2. `F-025` map-tracked-unit producer timing and `TrackingSlot` selection.
3. `F-004` housing `0x0501`/`0x0506` neighborhood trigger and emit semantics.
4. `F-008` crafting discovery, service keys, complex stats, rune bridge,
   `0x084B`/`0x0855`.
5. `F-031` Madame Fay retail weights and active rotation.
6. All remaining partial/blocked rows, then diagnostic/structural rows.

Done only when every known gap is `Implemented`, `Mapped only`, `Blocked`, or
`Rejected`; every safe gap is implemented; every implemented gap has verification
and tracker evidence; every blocked gap names the next evidence source; and no
generated/local artifacts or unrelated user changes were included.

Final response each pass: state, targets closed, files changed,
verification results, blockers, and next target.
