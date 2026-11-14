# Restoration handoff (2026-11-14)

Branch: `feature/spell-effects-remaining`, starting published head `c96f4c3a`.
This supersedes the 2026-11-07 handoff's next-step priority for the slice below;
the older handoff's wider unfinished outcomes remain open. Static mode was used
because the resume request retained an unresolved mode placeholder.

## Changed this pass

Evidence tier: **Verified native subject selection; Implemented / Source**.
Types 38, 59 and 129 now retain the primary player at their player entry point,
matching the native dispatcher and their explicit-unit entry points. They no
longer substitute the relationship Target. See
[the evidence and validation record](PREREQUISITE_PRIMARY_SUBJECT_20261114.md).
Commit `ff3cf41f` contains the three wrappers, 18 new
regressions, three rewritten tests, ledger and focused-tracker corrections.

The subsequent [type-179 accessor record](PREREQUISITE_179_NATIVE_ACCESSOR_20261114.md)
maps virtual `+0x10` to `14045ba40` through the constructor-installed base vtable.
Evidence tier: Mapped native flow, no additional Source change.

Source checks: owning baseline build passed; new regressions were red first
(15 failed / 3 passed); final Prerequisite/Spell rebuild passed 2,882 cases.
Initial full run: 15,044 passed / 19 failed / 2 skipped. Two stale assertions
were corrected; all nine other failing methods passed isolated reruns (40 cases,
including all 17 previously failing cases). No green final full-suite claim.

## Inferred, not verified

The active-spell state inventory, stack-count and group-membership models retain
their earlier limitations. The native selector correction does not qualify those
models. No new spell-effect data word or retail earning/reset policy was inferred.

## Next work and exact open inputs

Continue the named type-179 discrepancy in the linked evidence record: the
player wrapper, manager helper and pure-179 spell shortcut disagree on subject
selection/default handling. Raw reach: 41 slots, 504 direct spell-column
references, 517 slot-weighted references. Dispatcher case `0xb3` and direct
disassembly of `14049eeb0` establish the primary selector and zero when no
creature record exists. The base accessor now resolves mechanically: unit
`+0x30` below 25, otherwise record `+0x14` (or accessor default 25). Recover the
writers/field meanings and relevant derived overrides, then qualify non-unit
world-object caller policy before unifying the three paths. Do not
present this pending native read as an unavailable asset or a completed fix.

Lower remaining target-substituting wrappers require individual dispatcher/body
and caller checks; the previous audit's reference counts must be re-queried.
For example type 102 now gives 102 direct / 121 weighted references in this pass,
where the older audit recorded 352 spell references; that difference is not yet
explained. Type 44 gives 278 direct / 281 weighted references. Native type 44
`14049ee40` was disassembled and accepts the primary unit as its group-evaluation
subject. Neither type was changed here.

## Not claimed

No retail parity, native client run, Computer Use, deployed runtime/provider
qualification, broad restoration completion or green full-suite result. Original
runtime data, entitlements, generated exports and unrelated work were preserved.
Continue normal scoped commits/pushes on the branch named above; no force push.
