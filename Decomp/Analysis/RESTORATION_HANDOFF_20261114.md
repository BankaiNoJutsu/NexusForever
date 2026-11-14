# Restoration handoff (2026-11-14)

Current branch: `feature/spell-effects-remaining`. Latest continuation starts from
published `3d1c6aa0`; it verifies and corrects type 44's player subject. Static/headless analysis
only; no native client execution or Computer Use.

## Latest type-44 continuation

Evidence tier: **Verified native selector; Implemented / Source**.
[Type-44 primary subject](PREREQUISITE_44_PRIMARY_SUBJECT_20261114.md) re-derives
the case `0x2c` dispatcher, handler `14049ee40`, criteria proxy and TargetGroup
getter. The player entry point now retains the player; explicit-unit and spell
paths already retain their supplied subject. One target-substitution expectation
is rewritten with explicit-creature controls. Sixteen new regressions ran red
(14 failed / two passed); rebuilt Prerequisite/Spell gate: **2,934 passed**.
Owning Game build: zero warnings/errors. Three native labels re-exported;
8,332 mapping rows validate. Full suite: **15,100 passed / 15 failed / two
skipped**; all 40 type-44 cases passed. The ten failed methods pass isolated
reruns covering 66 cases. Thirteen full-run failures reported EF provider-cache
warnings; two were assertions. This does not establish their cause or make the
full suite green. Commands and outputs are recorded in the linked note.

Reference-data reach is 214 slots / 201 prerequisite rows / 278 direct spell
columns / 281 weighted references, not affected-cast counts. Group attribute,
recursion, missing-table, non-unit-row and activation-caller policy are unchanged
and are not newly qualified. The native lookup's descriptor identifies
TargetGroup, correcting its old Spell4ValidTargets table label. ObjectId is
ignored but nonzero in one slot; the earlier all-zero claim is corrected.

## Previous type-179 continuation (3d1c6aa0)

Evidence tier: **Verified native field binding/evaluator; Implemented / Source**.
[Type-179 field binding](PREREQUISITE_179_FIELD_BINDING_20261114.md) proves that
raw Creature2 `+0x1c` becomes intermediate CreatureInfo `+0x14`, cached by the
unit at `+0x30`. A present record whose raw ID is zero retains constructor default
25; the evaluator uses zero only with no creature record. The setter's separate
Creature2Difficulty lookup does not gate that raw/cached ID. Raw-zero reference
data reach: 17,990 / 53,137 Creature2 rows; type-179 dependency reach: 41 slots /
504 direct spell-column references / 517 weighted references, not broken-cast counts.

The type-179 player wrapper, explicit-unit handler and pure-179 spell shortcut
now share one comparison helper. Null subjects fail; Target remains relationship
context; the pure shortcut stops at None. One test encoding target substitution
was rewritten with explicit-subject controls. Four native labels were reapplied
and re-exported; the accessor was created as a function in the read-only Ghidra
session. The constructor is not a packet consumer, correcting its old generic label.

Earlier commits remain: `ff3cf41f` corrects primary subjects for 38/59/129;
`b021a3ce` records the then-unmapped type-179 accessor. Their validation is in
[the earlier slice](PREREQUISITE_PRIMARY_SUBJECT_20261114.md); its pending field
gate is superseded by this continuation.

## Previous type-179 verification and limits

Initial expanded red: 36 failures before production changes. Some null tests
reached unsupported generic player-manager null logging. The first wider green
attempt passed 2,914 / failed four null cases at that logger; these were corrected
to exercise the actual type-179 player handler while keeping the real explicit-unit
manager null checks and false assertions. The final four handler-level null cases
were not separately rerun red; the other 32 cases had behavioral baseline failures.
Final owning/dependency rebuild and Prerequisite/Spell gate: **2,918 passed**,
zero failed/skipped. Five CS8632 warnings are in the unmodified MySQL test file.
Explicit owning Game build: zero warnings/errors. Mapping validator: 8,331 rows /
seven files pass. Full suite: 15,069 passed / 30 failed / two skipped / 15,101
total; all 36 type-179 cases passed. All 17 failing methods passed separate
reruns covering 94 cases, including all 30 failed cases. Twenty-eight full-run
failures reported EF provider-cache warnings; two were assertion failures.
Isolation does not establish their root cause or make the full suite green.
See the field-binding note for output paths and qualification limits.

## Open outcomes

Type 179 is implemented for the existing server CreatureInfo/CreatureEntry model.
No independent dynamic difficulty override state was introduced. Type 180's
rank helper and mixed non-unit prerequisite-row policy remain unchanged; native
acceptance, live UI/timing and retail qualification were not performed.

The broader restoration queue remains open. Lower remaining target-substituting
wrappers require their own dispatcher/body and caller checks. Previous counts
must be re-queried:
type 102 gave 102 direct / 121 weighted references where the old audit said 352
spell references. That count discrepancy remains unexplained. Type 102 was not
changed in this continuation. Generic player-manager null logging is outside
type 179; the null-capable explicit-unit manager remains tested.

## Not claimed

No retail parity, native client run, Computer Use, runtime deployment, provider
qualification or broad restoration completion. Original runtime data and
entitlements are preserved. Continue normal scoped commits/pushes on the branch
above; no force push. The commit containing this handoff carries the type-44
correction, regressions, labels and evidence record together.
