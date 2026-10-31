# Contract reward modifier ownership

Latest continuation: [Contract track choice through the registered actor queue](CONTRACT_REWARD_TRACK_CHOICES.md)
qualifies checkpoint9122: **9,122 full tests**,32 new/674 focused,1,172 MySQL runs
(1,124 prior +24 choices +24 retry), two model gates and31 compatibility probes,
with zero build warnings/errors. Registered07CF routes Contract0/1 through the
real Quest owner and existing queue. Published/baseline/busy/foreign guards precede
capture; active flags and optional legacy-cycle selections commit together. Choose
provenance records an operation, never fabricated acceptance/history. Other active
types and dirty Quest/Challenge intent survive; lost-ack recovery cannot reroll.
No schema/default changes or runtime staging: the8378 backend,9 identities,
originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain preserved.
Claims/reset/Auth handoff/full-table/socket/deployment/native gates and the pending
reset-policy preference remain. Computer Use stays stopped; the full Goal is active.

Earlier checkpoint8679: [Atomic Contract/ordinary Character save enrollment](CONTRACT_NORMAL_SAVE_TRANSACTION.md)
qualifies checkpoint8679: **8,679 full tests**,116 new/116 focused,853 MySQL cases
(680 prior +116 new +57 configured-retry cases), two model gates and31 compatibility
probes, with zero build warnings/errors. The complete enrolled Character graph,
receipt/points and exact Quest SQL witnesses share one serializable unit. Real
frozen Quest and ordinary Currency saves, partial stubs, later intent, suppressed
writes, rollback and lost acknowledgement are covered. No model/migration changes
or entitlement-default changes are introduced. The live Player completion journal/
FIFO and Auth handoff, then selection/claim/reset/publication/socket qualification,
remain required; no live earning is enabled. The qualified8378 runtime, all9 service
identities, originals/config/schema/defaults15:1/17:1/no44 and6 drained queues remain
preserved. Computer Use stays stopped and the full assessment Goal remains active.

Checkpoint8563 implements source-aware Contract modifier lifetimes, whole-scalar
packet projection and immutable modifier-to-receipt inputs. All12 configured
Contract effects and51 supported Contract source descriptions are verified.
Signature/premium223 and earned entitlement44 remain Challenge-specific; no
entitlement defaults or automatic grants change.

At checkpoint8563 the live completion producer still needed composite ordinary reward/actor ownership
as described in [CONTRACT_COMPLETION_TRANSACTION.md](CONTRACT_COMPLETION_TRANSACTION.md).
The current modifier/completion build is not staged. The isolated runtime remains
on the qualified8378 acceptance build, with its existing data and accepted Contract
intact. The full assessment Goal remains active.

## Exact-build evidence

Client: `I:/Wildstar/Client64/WildStar64.exe`, build16042, SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Evidence root:
`I:/GIT/NexusForever-assessment-report/remaining-goal/contract-modifiers`.

Targeted archive extraction reads13 Contract UI/table files into a new external
directory. The index is hashed unchanged; archive length/write-time are unchanged.
The13GB data archive is not fully hashed. Original archives and client remain
unmodified; no native process or Computer Use is launched.

The shipped Contracts UI reads `GetRewardTrackValue` for its preview, tooltip and
summary. It sums base quotes directly; it has no Challenge multiplier lookup in
that path. Cached native1406a3340 returns the formula1193 base values. This preview
is not itself evidence that active point bonuses should be ignored.

Native scalar getter1403ac060 returns identity1 when absent, and returns the
server aggregate directly for scalar types0/2. Generic Lua getter1403abf30 selects
that scalar path using RewardProperty metadata. Consumer1403ab820 copies the
server aggregate and source rows; it does not add identity locally. Thus mapped
Contract keys need whole values on the wire, while their legacy raw delta API can
remain unchanged. These are cached exact-build observations; no fresh Ghidra
analysis, debugger execution or new CSV label application is claimed.

A read-only audit scans all125 actual premium rows and131,010 Spell4Effects rows.
RewardProperty3/4 are additive scalars. There are no Contract premium rows and no
property3 Contract spell sources. Premium223 is property4/data3/+0.25. Of21
property3/4 spell rows,12 target Contract types0/1, six target Challenge type3 and
three target Zone type4. The Contract rows are:

| Spell | PvP effect (data0) | PvE effect (data1) | Delta |
| --- | --- | --- | --- |
|82014|218043|218045|+0.5|
|82016|218047|218048|+0.5|
|82017|218050|218051|+0.5|
|87221|227800|227801|+0.5|
|87222|227804|227805|+0.5|
|87223|227808|227809|+0.5|

The actual English text, Spell4 and Spell4Base rows are independently hashed and
resolved. Later Reward flask descriptions explicitly mention Contract, Challenge
and Zone points. Older Challenger descriptions mention Challenge points only,
although their typed effect rows also contain the two Contract keys. This pass
preserves existing effect activation/prerequisite processing; it changes ownership
and projection after those handlers run. It does not infer new activation or
offline duration rules from the descriptions.

## Implemented ownership and projection

The existing property4 wrapper now delegates data0/1 to separate Contract owners.
Each retains the bound actor, raw delta, captured runtime effect token, actual
spell id, source effect row, supported shape and revision. Identical apply/removal
is idempotent; independent same-spell instances stack; replacement actors cannot
lose their contributions to late old cleanup. Same-actor premium refresh preserves
active Contract spell state and replaces premium sources rather than duplicating
them. Spell cleanup captures the original source ids before later parameter or
entry changes.

`GetValue`, `UpdateValue` and `SetValue` retain raw delta meaning. Contract packet
rows project identity1 plus that delta, with truthful known source attribution.
Untouched Contract rows may remain absent, whose native default is identity1.
Unrelated keys, including the unmapped Zone projection, retain their earlier
behavior. Challenge Signature/committed-entitlement logic and its source lifetimes
are unchanged.

`IRewardPropertyManager` adds compatible default hooks: old custom implementations
retain their delta updates and do not automatically gain Contract credit authority.
Concrete capture requires the bound actor/account/session, mapped Contract track,
scalar property metadata and available effect data. It declines unknown effect
shapes, anonymous nonneutral deltas, nonneutral specific-track modifiers and
unmapped Contract premiums. Their existing raw/wire operations continue; they are
not silently interpreted as a new credit policy.

`ContractRewardModifierSnapshot` copies multiplier, revision and source scalars.
Mapped base amounts and +50% sources yield exact integral awards. Evaluation is
accepted only where exact arithmetic and float32 agree, with no overflow or
fractional ambiguity. Fractional retail rounding/saturation remains unmapped;
Challenge arithmetic is not substituted for it. `ContractCompletionCommand.FromModifier`
checks track ownership and freezes the evaluated amount plus versioned provenance
inside the existing canonical receipt payload. Later removal or refresh cannot
recompute that queued evaluation. A snapshot is not completion permission.

## Verification

- Full solution build: **0 warnings,0 errors**. Full suite: **8,563/8,563**, zero
  skipped.89 new cases cover12 real handler/cleanup paths, all51 source-to-receipt
  integrations, bonus separation, stacking, actor replacement, refresh, unsupported
  inputs, immutable snapshots, arithmetic bounds and legacy interface defaults.
- 225 focused cases include the40 existing Challenge property and96 Contract
  transaction/schema cases. The earlier sibling-key assertions now check raw
  Contract0.5 versus whole wire1.5; Zone behavior remains unchanged. Initial test
  fixture cleanup metadata and old raw-wire expectation failures are retained.
- **680 exact-source MySQL cases pass**: all629 prior cases rerun plus51 actual
  source modifier-to-receipt integrations. The remaining38 pure modifier/lifetime/
  arithmetic cases run in the full suite, not the provider loop. These are
  surrogate actors and achieved Quest graphs, not live Contract completions.
- `Projection/result.json` runs the real frozen new manager over four actual
  configured tables. All12 complete decoded effects apply, capture and remove;
  48 evaluations cover base50/150/450/500. Both owner types are represented;
  quality4's500 is configured formula evidence, not a newly offered Contract.
  `SourceAudit` records the complete relevant source inventory; `TextAudit` binds
  the six actual spell names/tooltips and English text hash. Private configuration
  is hashed unchanged. No database or native UI execution occurs in those audits.
- Two provider model gates and31 unchanged compatibility probes pass. Final
  inventory binds5,811 Source/tool files,11 changed paths since8474,169 DLLs and29
  copied case/helper files. No model, migration or live reward producer changes
  occur in this checkpoint.
- Read-only preservation verifies all nine existing8378 service identities,
  six drained queues, five closed fixture epoch owners, original77 tables/
  21,989 rows/43 copied legacy characters, the accepted Contract, configuration,
  schema and defaults15:1/17:1/no44. Current code is not staged. The original
  checkout is clean; branch/head are unchanged; no commit/push occurred.
- Content validation remains31 CSVs/167,484 rows, all `not_retail_complete`.
  The unavailable June19 Q5597 bundle remains unavailable. Computer Use remains
  stopped; native UI and walking checks remain open.

## Required connected work

Wire the verified capture/factory into the real completion actor and a composite
Character transaction that owns all ordinary Quest rewards and side effects with
the receipt, points and terminal graphs. Retain FIFO, frozen normal saves, later
dirty intent, quarantine and committed publication; do not wrap an independent
point write around unowned item/currency/XP mutations.

Then finish persistent eligible selections, explicit Contract repeat/reset/carry,
claim guards, composed presentation, all equipment/AccountItem choices, final
credits and idempotent Auth handoff/recovery. Run connected provider and genuine
objective/completion/earn/choose/claim/relog/reset socket qualification before live
enablement. Independently map Zone35 earning and preserve all broader assessment,
upstream, combat, AMP, content and native evidence requirements in the full Goal.
